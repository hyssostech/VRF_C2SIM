using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VrfC2SimApp;

/// <summary>One simObject read off disk from a VR-Forces .entity file.</summary>
public sealed class SimObjectTemplate
{
    public string File { get; init; } = "";        // full path
    public string Name { get; init; } = "";        // file base name - the survey's naming
    public string GuiLabel { get; init; } = "";
    public string EchelonLevel { get; init; } = "";
    public string CanCreate { get; init; } = "";
    public string Countries { get; init; } = "";
    public int[] ObjectType { get; init; }         // 8 fields, published
    public TypeField[] MatchType { get; init; }    // 8 fields, may wildcard (-1) or range
    public IReadOnlyList<int[]> Subordinates { get; init; } = Array.Empty<int[]>();

    public bool IsUnit => ObjectType is { Length: 8 } && ObjectType[0] == 3;
    public override string ToString() => Name;
}

/// <summary>One matchType field: a wildcard, an exact value, or an inclusive range.</summary>
public readonly record struct TypeField(int Lo, int Hi)
{
    public static readonly TypeField Wild = new(-1, -1);
    public bool IsWild => Lo == -1;
    public bool Accepts(int v) => IsWild || (v >= Lo && v <= Hi);
}

/// <summary>
/// Offline index of an installed VR-Forces simulation-model-set chain plus the vendor's
/// documented BEST-MATCH object-type resolution
/// (C:\MAK\vrforces5.0.2\doc\help\Content\SimulationModels\ObjectParameterDatabase\ObjectTypes.htm:
/// "it finds the best match among matching object types... working its way down until no better
/// matches are found"). Read-only; used by --typemap-selftest to prove every
/// data/unit-type-map.json row lands the template it names. Nothing here runs in the live path.
///
/// Validated 6/6 against the resolutions docs/VRF_GROUND_TRUTH.md sec 0.1.5 records as VERIFIED
/// (the self-test re-runs that validation before it trusts any row).
/// </summary>
public sealed class ObjectTypeResolver
{
    public IReadOnlyList<SimObjectTemplate> Templates { get; }
    public IReadOnlyList<string> ModelSetDirs { get; }

    private ObjectTypeResolver(List<SimObjectTemplate> templates, List<string> dirs)
    {
        Templates = templates;
        ModelSetDirs = dirs;
    }

    /// <summary>The default install root; override with the VRF_HOME environment variable.</summary>
    public static string DefaultVrfHome
        => Environment.GetEnvironmentVariable("VRF_HOME") is { Length: > 0 } h ? h : @"C:\MAK\vrforces5.0.2";

    public static string ModelSetsDir(string vrfHome)
        => Path.Combine(vrfHome, "data", "simulationModelSets");

    /// <summary>
    /// Load a .sms and everything it (transitively) includes. The include lines carry a path
    /// that is not relative to the .sms file itself (`(include "..\data\simulationModelSets\
    /// EntityLevel.sms")` sits IN that directory), so the file NAME is what is followed - the
    /// only interpretation that resolves on the shipped 5.0.2 tree.
    /// </summary>
    public static ObjectTypeResolver LoadChain(string vrfHome, string topSms = "C2simEx")
    {
        string setsDir = ModelSetsDir(vrfHome);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();
        Follow(topSms);

        var templates = new List<SimObjectTemplate>();
        var dirs = new List<string>();
        foreach (string set in order)
        {
            string vrfSim = Path.Combine(setsDir, set, "vrfSim");
            if (!Directory.Exists(vrfSim)) continue;
            dirs.Add(vrfSim);
            foreach (string f in Directory.EnumerateFiles(vrfSim, "*.entity").OrderBy(x => x, StringComparer.Ordinal))
                templates.AddRange(ReadEntityFile(f));
        }
        return new ObjectTypeResolver(templates, dirs);

        void Follow(string smsName)
        {
            string sms = Path.Combine(setsDir, smsName + ".sms");
            if (!File.Exists(sms) || !seen.Add(smsName)) return;
            string text = File.ReadAllText(sms);
            var dir = Regex.Match(text, @"\(model-set-directory\s+""([^""]*)""\)");
            order.Add(dir.Success && dir.Groups[1].Value.Length > 0 ? dir.Groups[1].Value : smsName);
            foreach (Match m in Regex.Matches(text, @"\(include\s+""([^""]*)""\)"))
            {
                string inc = Path.GetFileNameWithoutExtension(m.Groups[1].Value);
                if (inc.Length > 0) Follow(inc);
            }
        }
    }

    private static IEnumerable<SimObjectTemplate> ReadEntityFile(string path)
    {
        XDocument doc;
        try { doc = XDocument.Load(path); }
        catch { yield break; }                      // a malformed vendor file is a finding, not a crash
        string name = Path.GetFileNameWithoutExtension(path);
        foreach (var so in doc.Descendants("simObject"))
        {
            int[] obj = ParseType((string)so.Attribute("objectType"));
            if (obj == null) continue;
            var match = ParseMatch((string)so.Attribute("matchType")) ?? obj.Select(v => new TypeField(v, v)).ToArray();
            yield return new SimObjectTemplate
            {
                File = path,
                Name = name,
                GuiLabel = StringParam(so, "gui-label"),
                EchelonLevel = StringParam(so, "echelon-level"),
                Countries = StringParam(so, "gui-deployable-countries"),
                CanCreate = so.Elements("bool").FirstOrDefault(b => (string)b.Attribute("paramName") == "gui-can-create")?.Value?.Trim() ?? "",
                ObjectType = obj,
                MatchType = match,
                Subordinates = so.Descendants("subordinate")
                                 .Select(s => ParseType((string)s.Attribute("objectType")))
                                 .Where(t => t != null).ToList(),
            };
        }
    }

    private static string StringParam(XElement so, string paramName)
        => so.Elements("string").FirstOrDefault(s => (string)s.Attribute("paramName") == paramName)?.Value?.Trim() ?? "";

    private static int[] ParseType(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var parts = s.Split(':');
        if (parts.Length != 8) return null;
        var f = new int[8];
        for (int i = 0; i < 8; i++)
            if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out f[i]))
                return null;
        return f;
    }

    // matchType fields may be -1 (wildcard) or an inclusive range "12-22" (four civil-aircraft
    // templates on 5.0.2 use ranges; no ground unit does).
    private static TypeField[] ParseMatch(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var parts = s.Split(':');
        if (parts.Length != 8) return null;
        var f = new TypeField[8];
        for (int i = 0; i < 8; i++)
        {
            string p = parts[i].Trim();
            int dash = p.IndexOf('-', 1);
            if (dash > 0)
            {
                if (!int.TryParse(p[..dash], out int lo) || !int.TryParse(p[(dash + 1)..], out int hi)) return null;
                f[i] = new TypeField(lo, hi);
            }
            else
            {
                if (!int.TryParse(p, out int v)) return null;
                f[i] = v == -1 ? TypeField.Wild : new TypeField(v, v);
            }
        }
        return f;
    }

    // ---- the best-match rule ---------------------------------------------

    private static bool Matches(int[] q, TypeField[] m)
    {
        for (int i = 0; i < 8; i++) if (!m[i].Accepts(q[i])) return false;
        return true;
    }

    // "working its way down until no better matches are found": prefer the deepest run of
    // leading SPECIFIC fields (the A-10 example turns on field 6 differing), then the most
    // specific fields overall.
    private static (int Lead, int Total) Score(TypeField[] m)
    {
        int lead = 0;
        while (lead < 8 && !m[lead].IsWild) lead++;
        int total = m.Count(f => !f.IsWild);
        return (lead, total);
    }

    /// <summary>Every template tied for best match. Empty when nothing matches at all.</summary>
    public IReadOnlyList<SimObjectTemplate> ResolveAll(int[] query)
    {
        var cands = Templates.Where(t => Matches(query, t.MatchType)).ToList();
        if (cands.Count == 0) return Array.Empty<SimObjectTemplate>();
        var best = cands.Select(t => Score(t.MatchType)).Max();
        return cands.Where(t => Score(t.MatchType) == best).ToList();
    }

    /// <summary>The winning template, or null when nothing matches. Ties break on chain order.</summary>
    public SimObjectTemplate Resolve(int[] query) => ResolveAll(query).FirstOrDefault();

    public SimObjectTemplate Resolve(string objectType)
    {
        var q = UnitTypeMap.ParseObjectType(objectType);
        return q == null ? null : Resolve(q);
    }

    /// <summary>
    /// Transitive subordinate expansion of a landed template, depth-first, cycle-guarded.
    /// Each entry is (depth, requested type, what it resolves to - null when nothing does).
    /// This is what catches the survey sec 3.5/3.6 traps: a template's own subordinate list
    /// is subject to the SAME best-match rule, so "it is a real composed unit" must be
    /// verified transitively, never read at face value.
    /// </summary>
    public List<(int Depth, int[] Query, SimObjectTemplate Landed)> Expand(int[] query, int maxDepth = 4)
    {
        var outp = new List<(int, int[], SimObjectTemplate)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Walk(query, 0);
        return outp;

        void Walk(int[] q, int depth)
        {
            var t = Resolve(q);
            outp.Add((depth, q, t));
            if (t == null || depth >= maxDepth) return;
            if (!seen.Add(t.File + "|" + string.Join(':', t.ObjectType))) return;
            foreach (var s in t.Subordinates) Walk(s, depth + 1);
        }
    }
}
