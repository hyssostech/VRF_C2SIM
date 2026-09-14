using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace VrfC2SimApp.Preflight;

/// <summary>What the ground under one sample resolves to, and how sure we are of the hop.</summary>
public sealed record SoilSample(
    string Source,        // the land-cover tileset that answered, or "none"
    int? Value,           // its raw class value
    string Description,   // the catalogue's own description of that class
    string SoilType,      // BM_* - the vendor catalogue's soiltype
    string SurfChar,      // the surface characteristic landCoverDataSurfChar.map gives it
    string Soil,          // the ground-tracked.sysdef soil name
    double Factor,        // that soil's acceleration-factor
    bool Assumed);        // true when the DtSoilType -> DtRoughnessSoilType hop is a guess

/// <summary>
/// The vendor chain the pre-flight derates a vehicle's max-slope with:
///   land-cover class value -> soiltype (osgEarth catalogue) -> surface characteristic
///   (landCoverDataSurfChar.map) -> soil -> acceleration-factor (ground-tracked.sysdef).
///
/// I/O happens ONCE, in the constructor, reading four read-only vendor files. Everything
/// afterwards is a table lookup; <see cref="FromClass"/> is pure and <see cref="Classify"/>
/// only adds the tile read that says which class value applies at a point.
///
/// Ported from tools/preflight/leg_check.py's SoilChain, including its fallbacks: when a
/// vendor file cannot be read the chain keeps working on the tabulated factors and SAYS SO
/// through <see cref="Sources"/>, because a silently different factor would move every ratio.
/// </summary>
public sealed class SoilChain
{
    /// <summary>
    /// DtSoilType (geometry/surface.h:19-44, the right-hand column of
    /// landCoverDataSurfChar.map) -> DtRoughnessSoilType (surface.h:75-88, the names in
    /// ground-tracked.sysdef's soil-list). ASSUMED except the "sand" and "pavedroad" rows:
    /// the implementation (DtMapSurfaceToRoughness::operator()) ships only in the geometry
    /// DLL, the header declares it and nothing more. "sand" is the row FINDING sec 7
    /// confirms end to end for this AO, and it is the row every calibration flag rests on.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Soil, bool Assumed)> SoilBridge =
        new Dictionary<string, (string, bool)>(StringComparer.Ordinal)
        {
            ["pavedroad"] = ("paved-road", false),
            ["asphaltorotherhardsurface"] = ("paved-road", true),
            ["usrailroad"] = ("hard-packed", true),
            ["eurorailroad"] = ("hard-packed", true),
            ["gravelroad"] = ("gravel", true),
            ["dirtroad"] = ("hard-packed", true),
            ["dryground"] = ("hard-packed", true),
            ["cultivatedfields"] = ("hard-packed", true),
            ["grass"] = ("hard-packed", true),
            ["orchards"] = ("hard-packed", true),
            ["forest"] = ("hard-packed", true),
            ["softsoil"] = ("sand", true),
            ["flimsy"] = ("hard-packed", true),
            ["rock"] = ("rocks", true),
            ["boulder"] = ("rocks", true),
            ["sand"] = ("sand", false),
            ["mud"] = ("muck", true),
            ["muddyroad"] = ("muck", true),
            ["swamp"] = ("muck", true),
            ["bodyofwater"] = ("deep-water", true),
            ["ocean"] = ("deep-water", true),
            ["deeplake"] = ("deep-water", true),
            ["deepriver"] = ("deep-water", true),
            ["shallowlake"] = ("shallow-water", true),
            ["shallowriver"] = ("shallow-water", true),
            ["tree"] = ("hard-packed", true),
            ["building"] = ("hard-packed", true),
            ["undefinedsoiltype"] = ("hard-packed", true),
        };

    /// <summary>Used ONLY when ground-tracked.sysdef cannot be read (its lines 787-820).</summary>
    public static readonly IReadOnlyDictionary<string, double> FactorsFallback =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["paved-road"] = 1.00, ["hard-packed"] = 0.98, ["gravel"] = 0.95,
            ["rocks"] = 0.80, ["sand"] = 0.80, ["shallow-water"] = 0.70,
            ["deep-water"] = 0.00, ["muck"] = 0.40, ["snow"] = 0.60, ["ice"] = 1.00,
        };

    private readonly Dictionary<int, Dictionary<int, (string SoilType, string Desc)>> _maps = new();
    private readonly Dictionary<string, string> _surfChar = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> _factors;
    private readonly List<string> _sources = new();
    private readonly ConcurrentDictionary<(double, double), SoilSample> _cache = new();

    /// <summary>One line per vendor file that was actually read, for the operator log.</summary>
    public IReadOnlyList<string> Sources => _sources;

    public SoilChain(string sharedData, string vrfHome)
    {
        _factors = new Dictionary<string, double>(FactorsFallback, StringComparer.Ordinal);
        LoadLayers(sharedData);
        LoadSurfChar(vrfHome);
        LoadFactors(vrfHome);
    }

    // ---- vendor files (the only I/O in this class) ------------------------

    private void LoadLayers(string sharedData)
    {
        string baseDir = Path.Combine(sharedData, "TerrainData", "TerrainConfiguration",
                                      "osgEarthCatalogs", "coverage");
        if (!Directory.Exists(baseDir)) return;

        var presets = new Dictionary<string, string>(StringComparer.Ordinal);
        string presetFile = Path.Combine(baseDir, "presets.xml");
        if (File.Exists(presetFile))
            foreach (Match m in Regex.Matches(ReadText(presetFile),
                                              "<preset\\s+name=\"([^\"]+)\"[^>]*?soiltype=\"([^\"]+)\""))
                presets[m.Groups[1].Value] = m.Groups[2].Value;

        var candidates = Directory.GetFiles(baseDir, "layer.*.online.xml")
                                  .OrderBy(p => p, StringComparer.Ordinal).ToList();
        foreach (var (ds, _, _) in TileMath.LandCoverSources)
        {
            string chosen = null, text = null;
            foreach (var cand in candidates)
            {
                string body = ReadText(cand);
                if (body.Replace("\\", "/").Contains($"/{ds}/", StringComparison.Ordinal))
                { chosen = cand; text = body; break; }
            }
            if (chosen == null) continue;

            // Commented-out rows are IGNORED - which is exactly why water resolves to no soil:
            // the vendor catalogues map no water class at all.
            text = Regex.Replace(text, "<!--.*?-->", "", RegexOptions.Singleline);
            var table = new Dictionary<int, (string, string)>();
            foreach (Match m in Regex.Matches(text, "<mapping\\s+([^>]*?)/>"))
            {
                var attrs = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (Match a in Regex.Matches(m.Groups[1].Value, "(\\w+)=\"([^\"]*)\""))
                    attrs[a.Groups[1].Value] = a.Groups[2].Value;
                if (!attrs.TryGetValue("value", out var vs)) continue;
                attrs.TryGetValue("soiltype", out var soilType);
                if (string.IsNullOrEmpty(soilType) && attrs.TryGetValue("preset", out var pre))
                    presets.TryGetValue(pre, out soilType);
                if (string.IsNullOrEmpty(soilType)) continue;
                if (!int.TryParse(vs, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) continue;
                table[v] = (soilType, attrs.TryGetValue("desc", out var d) ? d : "");
            }
            if (table.Count > 0)
            {
                _maps[ds] = table;
                _sources.Add($"{Path.GetFileName(chosen)} -> {table.Count} class rows");
            }
        }
    }

    private void LoadSurfChar(string vrfHome)
    {
        string fn = Path.Combine(vrfHome, "appData", "settings", "vrfSim", "landCoverDataSurfChar.map");
        if (!File.Exists(fn)) return;
        foreach (var line in File.ReadAllLines(fn))
        {
            var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && parts[0] == "Match") _surfChar[parts[1]] = parts[2];
            else if (parts.Length == 2 && parts[0].StartsWith("BM_", StringComparison.OrdinalIgnoreCase))
                _surfChar[parts[0]] = parts[1];     // the file's one unindented row
        }
        _sources.Add($"{Path.GetFileName(fn)} -> {_surfChar.Count} soiltype rows");
    }

    private void LoadFactors(string vrfHome)
    {
        string fn = Path.Combine(vrfHome, "data", "simulationModelSets", "EntityLevel", "vrfSim",
                                 "systems", "movement", "ground-tracked.sysdef");
        if (!File.Exists(fn)) return;
        string txt = ReadText(fn);
        var block = Regex.Match(txt, "\\(soil-list(.*?)\\n\\s*\\)\\s*\\n\\s*\\)\\s*\\n\\s*\\)",
                                RegexOptions.Singleline);
        if (!block.Success) return;
        int n = 0;
        foreach (Match m in Regex.Matches(block.Groups[1].Value,
                                          "\\(([a-z\\-]+)\\s*\\n\\s*\\(acceleration-factor\\s+([0-9.]+)\\)"))
        {
            if (double.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double f))
            { _factors[m.Groups[1].Value] = f; n++; }
        }
        if (n > 0) _sources.Add($"ground-tracked.sysdef -> {n} soil acceleration factors");
    }

    private static string ReadText(string path)
    {
        try { return File.ReadAllText(path, System.Text.Encoding.UTF8); }
        catch { return ""; }
    }

    // ---- lookup (pure) ----------------------------------------------------

    /// <summary>The default ground when no tileset has data: hard-packed, and marked assumed.</summary>
    public SoilSample NoData => new("none", null, "", "", "", "hard-packed",
                                    _factors.TryGetValue("hard-packed", out var f) ? f : 0.98, true);

    /// <summary>
    /// PURE: one tileset's raw class value -> the whole chain. Null when this tileset has no
    /// row for that value (the caller falls through to the next, coarser tileset).
    /// </summary>
    public SoilSample FromClass(int ds, int value, string label)
    {
        if (!_maps.TryGetValue(ds, out var table) || !table.TryGetValue(value, out var hit)) return null;
        string sc = _surfChar.TryGetValue(hit.SoilType, out var s) ? s : "undefinedsoiltype";
        var (soil, assumed) = SoilBridge.TryGetValue(sc.ToLowerInvariant(), out var b)
                            ? b : ("hard-packed", true);
        return new SoilSample(label, value, hit.Desc, hit.SoilType, sc, soil,
                              _factors.TryGetValue(soil, out var f) ? f : 0.98, assumed);
    }

    /// <summary>
    /// The ground at a point: the highest-resolution tileset WITH DATA wins. A class value of
    /// 0, a missing tile, or a class the catalogue does not map all mean "not this tileset".
    /// </summary>
    public SoilSample Classify(TileSource tiles, double latDeg, double lonDeg)
    {
        var key = (Math.Round(latDeg, 5), Math.Round(lonDeg, 5));
        if (_cache.TryGetValue(key, out var cached)) return cached;
        SoilSample outp = NoData;
        foreach (var (ds, level, label) in TileMath.LandCoverSources)
        {
            int? v = tiles.LandCoverClass(ds, level, latDeg, lonDeg);
            if (v == null || v.Value == 0) continue;
            var s = FromClass(ds, v.Value, label);
            if (s == null) continue;
            outp = s;
            break;
        }
        _cache[key] = outp;
        return outp;
    }

    /// <summary>The acceleration-factor in force for a soil name (after the vendor load).</summary>
    public double FactorOf(string soil)
        => _factors.TryGetValue(soil ?? "", out var f) ? f : 0.98;
}
