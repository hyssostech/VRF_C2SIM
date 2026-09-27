using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace VrfC2SimApp.Preflight;

// ================================================================================================
// OSM FEATURES FOR THE PRE-FLIGHT (RL-20260927-01; docs/PLAN_MOVEMENT_2026-09-27.md M2).
//
// WHY: the sim reads OSM features the raster pre-flight never saw. 48 IBCT stopped twice on lines the
// CLCplus-only pre-flight scored clear - 0.5 m outside an OSM lake (FINDING_IRONSTORM_T14_STOP sec 3)
// and 3.2 m from an OSM building in a hamlet (the -2 run). tools/preflight/leg_check.py reads both
// sets (--osm-water / --osm-buildings); this file is the C# port of those readers, with the same tile
// source, the same vendor filters and the same UNKNOWN-is-never-clear rule.
//
// EVERYTHING IN THIS FILE IS PURE: bytes -> features, features -> distances. No file, no socket, no
// clock. The I/O lives in TileSource (OsmTile), exactly as the raster readers are split.
// ================================================================================================

/// <summary>
/// A Mapbox Vector Tile 2.1 decoder (the format of vr-theworld's mbtiles sets). Transcribes
/// tools/navdata/osm_sector_map.py's decode_mvt, including how it builds parts (a MoveTo starts a
/// part, a ClosePath appends the part's first point), so both tools see the same rings.
///
/// STRICTER THAN THE PYTHON IN ONE PLACE, ON PURPOSE: a body whose top level is anything but layer
/// messages (field 3) is REFUSED with a FormatException. That is how a captive-portal page or an
/// HTML error body is told apart from a tile (SF3's rule, carried over: a body that is not a tile is
/// FAILED, never cached and never read as "no features here").
/// </summary>
public static class Mvt
{
    public const int GeomUnknown = 0, GeomPoint = 1, GeomLine = 2, GeomPolygon = 3;

    public sealed record Feature(int GeomType, IReadOnlyDictionary<string, object> Props,
                                 List<List<(int X, int Y)>> Parts);

    public sealed record Layer(string Name, int Extent, List<Feature> Features);

    /// <summary>The one layer name the interface writes when the SERVER said "no tile" (404): an
    /// explicit, self-describing empty tile, so a later OFFLINE run can tell "the server has nothing
    /// here" from "nobody ever asked" (a missing or 0-byte file, which stays UNKNOWN).</summary>
    public const string AbsentMarkerLayer = "c2sim-absent-http-404";

    public static List<Layer> Decode(byte[] data)
    {
        if (data == null) throw new FormatException("no bytes");
        if (data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b) data = Gunzip(data);
        var layers = new List<Layer>();
        int i = 0;
        while (i < data.Length)
        {
            int at = i;
            ulong key = Varint(data, ref i);
            int field = (int)(key >> 3), wire = (int)(key & 7);
            if (field != 3 || wire != 2)
                throw new FormatException(FormattableString.Invariant(
                    $"not a vector tile: top-level field {field} wire {wire} at byte {at}"));
            var (s, n) = Delimited(data, ref i);
            layers.Add(DecodeLayer(data, s, n));
        }
        return layers;
    }

    /// <summary>True for the interface's own "server said 404" marker tile.</summary>
    public static bool IsAbsentMarker(IReadOnlyList<Layer> layers)
        => layers != null && layers.Count == 1 && layers[0].Name == AbsentMarkerLayer
           && layers[0].Features.Count == 0;

    /// <summary>The marker bytes: one layer (version 2, the marker name, extent 4096), no features.
    /// python's decode_mvt reads it as a layer with no features - "known, nothing here" - in both
    /// leg_check readers, which is what a 404 means.</summary>
    public static byte[] AbsentMarkerTile()
    {
        var layer = new List<byte> { 0x78, 0x02 };                // field 15 (version) = 2
        byte[] name = Encoding.ASCII.GetBytes(AbsentMarkerLayer);
        layer.Add(0x0a);                                          // field 1 (name), length-delimited
        layer.Add((byte)name.Length);
        layer.AddRange(name);
        layer.AddRange(new byte[] { 0x28, 0x80, 0x20 });          // field 5 (extent) = 4096
        var tile = new List<byte> { 0x1a, (byte)layer.Count };    // field 3 (layer)
        tile.AddRange(layer);
        return tile.ToArray();
    }

    /// <summary>python's str() of an MVT value: strings as-is, integers invariant, bools
    /// "True"/"False". Every vendor filter compares strings, so this is the one conversion.</summary>
    public static string Str(object v) => v switch
    {
        null => "",
        string s => s,
        bool b => b ? "True" : "False",
        long l => l.ToString(CultureInfo.InvariantCulture),
        ulong u => u.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        float f => f.ToString("R", CultureInfo.InvariantCulture),
        _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? "",
    };

    /// <summary>The property's string value, or null when the feature does not carry it.</summary>
    public static string Prop(IReadOnlyDictionary<string, object> props, string key)
        => props != null && props.TryGetValue(key, out var v) ? Str(v) : null;

    private static byte[] Gunzip(byte[] data)
    {
        try
        {
            using var src = new MemoryStream(data);
            using var gz = new GZipStream(src, CompressionMode.Decompress);
            using var dst = new MemoryStream();
            gz.CopyTo(dst);
            return dst.ToArray();
        }
        catch (Exception e) when (e is InvalidDataException || e is IOException)
        {
            throw new FormatException("gzip body does not decompress: " + e.Message);
        }
    }

    private static ulong Varint(byte[] b, ref int i)
    {
        ulong r = 0;
        for (int shift = 0; shift < 64; shift += 7)
        {
            if (i >= b.Length) throw new FormatException("truncated varint");
            byte c = b[i++];
            r |= (ulong)(c & 0x7F) << shift;
            if (c < 0x80) return r;
        }
        throw new FormatException("varint longer than 10 bytes");
    }

    private static (int Start, int Length) Delimited(byte[] b, ref int i)
    {
        ulong n = Varint(b, ref i);
        if (n > (ulong)(b.Length - i)) throw new FormatException("length-delimited field runs past the end");
        int s = i;
        i += (int)n;
        return (s, (int)n);
    }

    private static void Skip(byte[] b, ref int i, int wire)
    {
        switch (wire)
        {
            case 0: Varint(b, ref i); break;
            case 1: if (i + 8 > b.Length) throw new FormatException("truncated fixed64"); i += 8; break;
            case 2: Delimited(b, ref i); break;
            case 5: if (i + 4 > b.Length) throw new FormatException("truncated fixed32"); i += 4; break;
            default: throw new FormatException(FormattableString.Invariant($"unsupported wire type {wire}"));
        }
    }

    private static long ZigZag(ulong n) => (long)(n >> 1) ^ -(long)(n & 1);

    private static Layer DecodeLayer(byte[] b, int start, int length)
    {
        string name = "";
        int extent = 4096;
        var featureRanges = new List<(int S, int N)>();
        var keys = new List<string>();
        var vals = new List<object>();
        int i = start, end = start + length;
        while (i < end)
        {
            ulong key = Varint(b, ref i);
            int f = (int)(key >> 3), w = (int)(key & 7);
            if (f == 1 && w == 2) { var (s, n) = Delimited(b, ref i); name = Encoding.UTF8.GetString(b, s, n); }
            else if (f == 2 && w == 2) featureRanges.Add(Delimited(b, ref i));
            else if (f == 3 && w == 2) { var (s, n) = Delimited(b, ref i); keys.Add(Encoding.UTF8.GetString(b, s, n)); }
            else if (f == 4 && w == 2) { var (s, n) = Delimited(b, ref i); vals.Add(DecodeValue(b, s, n)); }
            else if (f == 5 && w == 0) extent = (int)Varint(b, ref i);
            else Skip(b, ref i, w);
            if (i > end) throw new FormatException("layer field runs past the layer");
        }
        if (extent <= 0) throw new FormatException("layer extent must be positive");
        var feats = new List<Feature>(featureRanges.Count);
        foreach (var (s, n) in featureRanges) feats.Add(DecodeFeature(b, s, n, keys, vals));
        return new Layer(name, extent, feats);
    }

    private static object DecodeValue(byte[] b, int start, int length)
    {
        int i = start, end = start + length;
        while (i < end)
        {
            ulong key = Varint(b, ref i);
            int f = (int)(key >> 3), w = (int)(key & 7);
            switch (f)
            {
                case 1 when w == 2: { var (s, n) = Delimited(b, ref i); return Encoding.UTF8.GetString(b, s, n); }
                case 2 when w == 5:
                    if (i + 4 > b.Length) throw new FormatException("truncated float");
                    return BitConverter.ToSingle(b, i);
                case 3 when w == 1:
                    if (i + 8 > b.Length) throw new FormatException("truncated double");
                    return BitConverter.ToDouble(b, i);
                case 4 when w == 0: return (long)Varint(b, ref i);
                case 5 when w == 0: return Varint(b, ref i);
                case 6 when w == 0: return ZigZag(Varint(b, ref i));
                case 7 when w == 0: return Varint(b, ref i) != 0;
                default: Skip(b, ref i, w); break;
            }
        }
        return null;
    }

    private static Feature DecodeFeature(byte[] b, int start, int length, List<string> keys, List<object> vals)
    {
        var tags = new List<ulong>();
        var geom = new List<ulong>();
        int type = GeomUnknown;
        int i = start, end = start + length;
        while (i < end)
        {
            ulong key = Varint(b, ref i);
            int f = (int)(key >> 3), w = (int)(key & 7);
            if (f == 2 && w == 2) Packed(b, ref i, tags);
            else if (f == 3 && w == 0) type = (int)Varint(b, ref i);
            else if (f == 4 && w == 2) Packed(b, ref i, geom);
            else Skip(b, ref i, w);
        }
        var props = new Dictionary<string, object>(StringComparer.Ordinal);
        for (int k = 0; k + 1 < tags.Count; k += 2)
        {
            if (tags[k] >= (ulong)keys.Count || tags[k + 1] >= (ulong)vals.Count)
                throw new FormatException("feature tag index out of range");
            props[keys[(int)tags[k]]] = vals[(int)tags[k + 1]];
        }
        // decode_mvt's geometry walk, transcribed: a MoveTo starts a part, a LineTo extends it, a
        // ClosePath appends the part's own first point.
        var parts = new List<List<(int X, int Y)>>();
        List<(int X, int Y)> cur = null;
        long x = 0, y = 0;
        int g = 0;
        while (g < geom.Count)
        {
            int cmd = (int)(geom[g] & 7);
            int cnt = (int)(geom[g] >> 3);
            g++;
            if (cmd == 7)
            {
                if (cur != null && cur.Count > 0) cur.Add(cur[0]);
                continue;
            }
            for (int c = 0; c < cnt; c++)
            {
                if (g + 1 >= geom.Count) throw new FormatException("truncated geometry");
                x += ZigZag(geom[g]);
                y += ZigZag(geom[g + 1]);
                g += 2;
                if (cmd == 1) { cur = new List<(int X, int Y)> { ((int)x, (int)y) }; parts.Add(cur); }
                else
                {
                    if (cur == null) throw new FormatException("LineTo before any MoveTo");
                    cur.Add(((int)x, (int)y));
                }
            }
        }
        return new Feature(type, props, parts);
    }

    private static void Packed(byte[] b, ref int i, List<ulong> into)
    {
        var (s, n) = Delimited(b, ref i);
        int j = s, end = s + n;
        while (j < end) into.Add(Varint(b, ref j));
    }
}

/// <summary>
/// The z14 spherical-mercator tile arithmetic of the mbtiles sets, with vr-theworld's TMS rows
/// (y counted from the SOUTH: tms_y = 2^z - 1 - xyz_y; osm_sector_map.py's header records the
/// HTTP-status check that settled it). Transcribes leg_check.py's _z14_key and osm_sector_map.py's
/// tile_to_latlon.
/// </summary>
public static class OsmTileMath
{
    public const int Zoom = 14;

    public static (int X, int TmsY) TileOf(double latDeg, double lonDeg, int z = Zoom)
    {
        double n = Math.Pow(2, z);
        double la = latDeg * Math.PI / 180.0;
        int x = (int)Math.Floor((lonDeg + 180.0) / 360.0 * n);
        int yx = (int)Math.Floor((1.0 - Math.Log(Math.Tan(la) + 1.0 / Math.Cos(la)) / Math.PI) / 2.0 * n);
        return (x, (int)n - 1 - yx);
    }

    /// <summary>Tile pixel (px, py; py DOWN from the tile's north edge) -> lat/lon.</summary>
    public static (double Lat, double Lon) ToLatLon(int z, int x, int tmsY, double px, double py, int extent)
    {
        double n = Math.Pow(2, z);
        double X = (x + px / extent) / n;
        double Y = (n - 1 - tmsY + py / extent) / n;
        double lat = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * Y))) * 180.0 / Math.PI;
        return (lat, X * 360.0 - 180.0);
    }

    /// <summary>lat/lon -> tile pixel of a given tile (the inverse of <see cref="ToLatLon"/>);
    /// what leg_check's selftest encoder does before rounding.</summary>
    public static (double Px, double Py) ToPixel(int z, int x, int tmsY, double latDeg, double lonDeg, int extent)
    {
        double n = Math.Pow(2, z);
        double la = latDeg * Math.PI / 180.0;
        double X = (lonDeg + 180.0) / 360.0 * n - x;
        double Y = (1.0 - Math.Log(Math.Tan(la) + 1.0 / Math.Cos(la)) / Math.PI) / 2.0 * n - (n - 1 - tmsY);
        return (X * extent, Y * extent);
    }

    public static string FileName(int x, int tmsY) => FormattableString.Invariant($"{Zoom}_{x}_{tmsY}.pbf");
}

/// <summary>The two mbtiles sets the pre-flight reads, by their vr-theworld names.</summary>
public enum OsmSet
{
    /// <summary>mbtiles/osm-water - the VRFSIM "Lake" layer (osm.features.water.xml:5-27) and the top
    /// layer of the land-cover composite (biomes.landcover.coverage.online.xml:58).</summary>
    Water,
    /// <summary>mbtiles/osm - "data:osm-all-features" (osm.features.xml:20-32): the buildings
    /// (buildings.worldwide.osm.online.xml), the aggregate River lines and land use
    /// (VRFSIM.Aggregate.feature.model.xml).</summary>
    All,
}

public static class OsmSets
{
    public static string Name(OsmSet s) => s == OsmSet.Water ? "osm-water" : "osm";
}

/// <summary>Aggregate RESTRICTED_L2 land use (speed-factor 0.25) the osm set carries.</summary>
public enum AggregateLandUse { None, Forest, Swamp, Municipal }

/// <summary>
/// THE VENDOR'S FILTERS, TRANSCRIBED. Each one cites the installed file it copies; where leg_check.py
/// already copied it, the transcription is leg_check's so the two tools keep the same features.
/// </summary>
public static class OsmVendor
{
    /// <summary>featureconfig.txt:423 "Transform MAK_WATERWAY: double MAK_WIDTH = 5" - the width a
    /// waterway line has when its data carries none.</summary>
    public const double DefaultWaterwayWidthMeters = 5.0;

    /// <summary>
    /// The coverage value of an osm-water feature, or null when the vendor drops it:
    /// osm.features.water.xml:15-19 filter() (islet/island, natural=sand/scrub) then
    /// layer.OSM.water.LOD14.online.xml:14-45 selectStyle(). natural=water with NO water= tag takes
    /// the default, 80 (preset Water -> BM_WATER -> deeplake -> deep-water) - the T14 lake.
    /// Same as tools/navdata/landcover_sector_map.py water_value.
    /// </summary>
    public static int? WaterValue(IReadOnlyDictionary<string, object> props)
    {
        string place = Mvt.Prop(props, "place");
        string natural = Mvt.Prop(props, "natural");
        if (place == "islet" || place == "island" || natural == "sand" || natural == "scrub") return null;
        switch (Mvt.Prop(props, "water"))
        {
            case "lake": case "pond": case "reservoir": case "lock": case "basin": case "stream_pool":
            case "oxbow": case "cenote": case "reflecting_pool":
                return 81;
            case "river": case "stream": case "canal": case "rapids": case "motorway_link":
            case "trunk_link": case "primary_link": case "secondary_link":
                return 82;
            case "harbour": case "lagoon":
                return 200;
            case "swamp":
                return 90;
            default:
                return 80;
        }
    }

    /// <summary>The vendor's own description of a coverage value (layer.OSM.water.LOD14.online.xml
    /// :82-86 mappings) and the soil it resolves to (presets.xml + landCoverDataSurfChar.map).</summary>
    public static string WaterDescription(int value) => value switch
    {
        80 => "80 BM_WATER, deep-water: permanent water bodies",
        81 => "81 BM_WATER-FRESH-STANDING, shallow-water: lakes and reservoirs",
        82 => "82 BM_WATER-FRESH-FLOWING, deep-water: rivers and streams",
        90 => "90 Wetland (BM_LAND-MARSH, swamp)",
        200 => "200 BM_WATER-OCEAN, deep-water: oceans, seas",
        _ => value.ToString(CultureInfo.InvariantCulture),
    };

    /// <summary>featureconfig.txt:211 "Transform MAK_ALL_ROADS: double MAK_WIDTH = 8" - a road's
    /// width when its data carries none.</summary>
    public const double DefaultRoadWidthMeters = 8.0;

    private static readonly HashSet<string> NoDriveHighway = new(StringComparer.Ordinal)
    {
        "path", "footway", "bridleway", "steps", "busway", "via_ferrata", "pedestrian", "cycleway", "raceway",
        "construction",
    };

    /// <summary>
    /// A DRIVABLE ROAD BRIDGE: an OSM line of the osm set with a bridge tag the vendor renders as a
    /// bridge (osm.bridges.xml filter: not a node or polygon, not bridge=boardwalk, not highway
    /// construction/footway/path) that is also a road a vehicle can use (a highway outside
    /// tools/navdata/osm_sector_map.py's NODRIVE list, the MAK_ROAD volume filter). Railway bridges are
    /// left out on purpose - no ground vehicle is routed over them.
    /// </summary>
    public static bool IsRoadBridge(int geomType, IReadOnlyDictionary<string, object> props)
    {
        if (geomType != Mvt.GeomLine) return false;
        if (Mvt.Prop(props, "@type") == "node") return false;
        string bridge = Mvt.Prop(props, "bridge");
        if (bridge == null || bridge == "no" || bridge == "boardwalk") return false;
        string hw = Mvt.Prop(props, "highway");
        return hw != null && !NoDriveHighway.Contains(hw);
    }

    /// <summary>A road's MAK_WIDTH: the data's width if it has one (featureconfig.txt:165), else
    /// 8 m (:211).</summary>
    public static double RoadWidthMeters(IReadOnlyDictionary<string, object> props)
        => DataWidthMeters(props) ?? DefaultRoadWidthMeters;

    /// <summary>
    /// A River LINE as the AGGREGATE terrain loads it: VRFSIM.Aggregate.feature.model.xml "RiverL"
    /// (not an OSM node, not a polygon, waterway river / canal / tidal_channel; NO tunnel exclusion
    /// in that file, unlike the entity-level osm.features.rivers.xml - which MAK Earth (online).earth
    /// does not include at all). MVT lines only.
    /// </summary>
    public static bool IsRiverLine(int geomType, IReadOnlyDictionary<string, object> props)
    {
        if (geomType != Mvt.GeomLine) return false;
        if (Mvt.Prop(props, "@type") == "node") return false;
        string w = Mvt.Prop(props, "waterway");
        return w == "river" || w == "canal" || w == "tidal_channel";
    }

    /// <summary>MAK_WIDTH of a waterway: featureconfig.txt:165 takes the first of
    /// width | w | wdt | gdb_width | dtwidth | wid2d the data carries; :423 defaults a MAK_WATERWAY
    /// with none to 5 m. A value that does not start with a positive number is ignored.</summary>
    public static double WaterwayWidthMeters(IReadOnlyDictionary<string, object> props)
        => DataWidthMeters(props) ?? DefaultWaterwayWidthMeters;

    /// <summary>The first of featureconfig.txt:165's width attributes that starts with a positive
    /// number, or null when the data carries none.</summary>
    private static double? DataWidthMeters(IReadOnlyDictionary<string, object> props)
    {
        foreach (string k in new[] { "width", "w", "wdt", "gdb_width", "dtwidth", "wid2d" })
        {
            string s = Mvt.Prop(props, k);
            if (string.IsNullOrWhiteSpace(s)) continue;
            int n = 0;
            string t = s.Trim();
            while (n < t.Length && (char.IsDigit(t[n]) || t[n] == '.')) n++;
            if (n > 0 && double.TryParse(t.Substring(0, n), NumberStyles.Float, CultureInfo.InvariantCulture,
                                         out double v) && v > 0)
                return v;
        }
        return null;
    }

    private static readonly HashSet<string> BuildingSkip =
        new(StringComparer.Ordinal) { "bridge", "ship", "bunker", "roof", "no" };
    private static readonly HashSet<string> AerowayKeep =
        new(StringComparer.Ordinal) { "hangar", "terminal", "tower", "control_tower" };
    private static readonly HashSet<string> ManMadeKeep =
        new(StringComparer.Ordinal) { "storage_tank", "tower" };

    /// <summary>
    /// buildings.worldwide.osm.base.online.filter.js filter(), as tools/preflight/leg_check.py's
    /// osm_building_kept copied it: polygons only; aeroway hangar/terminal/tower/control_tower;
    /// building except bridge/ship/bunker/roof/no and location=underground; man_made storage_tank/
    /// tower. The vendor's list of individually skipped OSM ids is NOT mirrored - keeping a building
    /// the vendor drops can only over-flag, never hide one.
    /// </summary>
    public static bool IsBuildingKept(int geomType, IReadOnlyDictionary<string, object> props)
    {
        if (geomType != Mvt.GeomPolygon) return false;
        if (props.ContainsKey("aeroway")) return AerowayKeep.Contains(Mvt.Prop(props, "aeroway"));
        if (props.ContainsKey("building"))
        {
            if (BuildingSkip.Contains(Mvt.Prop(props, "building"))) return false;
            return Mvt.Prop(props, "location") != "underground";
        }
        if (props.ContainsKey("man_made")) return ManMadeKeep.Contains(Mvt.Prop(props, "man_made"));
        return false;
    }

    /// <summary>
    /// The aggregate model's RESTRICTED_L2 land use in the osm set (VRFSIM.Aggregate.feature.model.xml
    /// ForestA landuse forest/orchard, SwampA natural=wetland, MunicipalA landuse commercial/
    /// industrial/residential/construction; featureconfig.txt:91-93 makes Swamp a FOREST and Municipal
    /// URBAN, :271 puts both in MAK_TANK_RESTRICTED_L2_TERRAIN, speed-factor 0.25 in
    /// tank-aggregated-movement.sysdef:124-127). Polygons only.
    /// </summary>
    public static AggregateLandUse LandUse(int geomType, IReadOnlyDictionary<string, object> props)
    {
        if (geomType != Mvt.GeomPolygon) return AggregateLandUse.None;
        string lu = Mvt.Prop(props, "landuse");
        if (lu == "forest" || lu == "orchard") return AggregateLandUse.Forest;
        if (lu == "commercial" || lu == "industrial" || lu == "residential" || lu == "construction")
            return AggregateLandUse.Municipal;
        if (Mvt.Prop(props, "natural") == "wetland") return AggregateLandUse.Swamp;
        return AggregateLandUse.None;
    }
}

/// <summary>An axis-aligned lat/lon box.</summary>
public readonly record struct GeoBox(double S, double W, double N, double E)
{
    public static GeoBox Of(IEnumerable<(double Lat, double Lon)> pts)
    {
        double s = double.MaxValue, w = double.MaxValue, n = double.MinValue, e = double.MinValue;
        foreach (var (la, lo) in pts)
        {
            if (la < s) s = la;
            if (la > n) n = la;
            if (lo < w) w = lo;
            if (lo > e) e = lo;
        }
        return new GeoBox(s, w, n, e);
    }

    /// <summary>Whether this box comes within <paramref name="marginM"/> of the other.</summary>
    public bool Near(GeoBox o, double marginM)
    {
        double dLat = marginM / OsmGeometry.MetresPerDegree;
        double cos = Math.Max(0.01, Math.Cos(((S + N) / 2.0) * Math.PI / 180.0));
        double dLon = marginM / (OsmGeometry.MetresPerDegree * cos);
        return !(N + dLat < o.S || S - dLat > o.N || E + dLon < o.W || W - dLon > o.E);
    }
}

/// <summary>One area feature (water, building footprint, land use): every ring of it, closed.</summary>
public sealed record OsmArea(string Id, string Kind, int Value,
                             IReadOnlyList<IReadOnlyList<(double Lat, double Lon)>> Rings, GeoBox Box)
{
    /// <summary>The OSM name tag, "" when none. IDENTITY ONLY (the river test: is the water at a
    /// band end the SAME water as on the leg?) - never printed: OSM names are not ASCII.</summary>
    public string Name { get; init; } = "";
}

/// <summary>One line feature (an aggregate River line) and its MAK_WIDTH.</summary>
public sealed record OsmLine(string Id, string Kind, double WidthM,
                             IReadOnlyList<(double Lat, double Lon)> Points, GeoBox Box)
{
    /// <summary>As <see cref="OsmArea.Name"/>: a river is several OSM ways, and its name is what
    /// ties them together.</summary>
    public string Name { get; init; } = "";
}

/// <summary>
/// ONE z14 TILE OF ONE SET, decoded into the features the vendor keeps. <see cref="Known"/> is the
/// load-bearing flag: a tile that is missing, 0 bytes, undecodable or unanswered is UNKNOWN, and
/// unknown ground is never clear - a query that touches it says so instead of reading it as empty.
/// A tile the SERVER answered 404 for is KNOWN and empty: that is the server's statement that it has
/// no features there, and the sim streams nothing there either.
/// </summary>
public sealed class OsmTile
{
    public OsmSet Set { get; init; }
    public int X { get; init; }
    public int TmsY { get; init; }
    public bool Known { get; init; }
    /// <summary>What was found: "cached", "fetched", "absent (server 404)", or "UNKNOWN: why".</summary>
    public string State { get; init; } = "";
    public IReadOnlyList<OsmArea> Water { get; init; } = Array.Empty<OsmArea>();
    public IReadOnlyList<OsmArea> Buildings { get; init; } = Array.Empty<OsmArea>();
    public IReadOnlyList<OsmArea> LandUse { get; init; } = Array.Empty<OsmArea>();
    public IReadOnlyList<OsmLine> Rivers { get; init; } = Array.Empty<OsmLine>();
    /// <summary>Drivable road bridges (<see cref="OsmVendor.IsRoadBridge"/>), WidthM = MAK_WIDTH.</summary>
    public IReadOnlyList<OsmLine> Bridges { get; init; } = Array.Empty<OsmLine>();

    public string Name => $"{OsmSets.Name(Set)}/{OsmTileMath.FileName(X, TmsY)}";

    public static OsmTile Unknown(OsmSet set, int x, int tmsY, string why)
        => new() { Set = set, X = x, TmsY = tmsY, Known = false, State = "UNKNOWN: " + why };

    /// <summary>PURE: decoded layers -> the features the vendor keeps from this set.</summary>
    public static OsmTile FromLayers(OsmSet set, int x, int tmsY, IReadOnlyList<Mvt.Layer> layers, string state)
    {
        var water = new List<OsmArea>();
        var buildings = new List<OsmArea>();
        var landUse = new List<OsmArea>();
        var rivers = new List<OsmLine>();
        var bridges = new List<OsmLine>();
        foreach (var layer in layers ?? Array.Empty<Mvt.Layer>())
            foreach (var f in layer.Features)
            {
                string id = Mvt.Prop(f.Props, "@id") ?? "?";
                string name = Mvt.Prop(f.Props, "name") ?? "";
                if (set == OsmSet.Water)
                {
                    // Polygons only: the osm-water set carries no lines (measured 2026-09-27 on the 128
                    // non-empty Suwalki tiles: 368 features, every one a polygon), and leg_check's
                    // classifier reads areas only.
                    if (f.GeomType != Mvt.GeomPolygon) continue;
                    int? v = OsmVendor.WaterValue(f.Props);
                    if (v == null) continue;
                    var rings = Rings(f, x, tmsY, layer.Extent);
                    if (rings.Count == 0) continue;
                    string kind = "natural=" + (Mvt.Prop(f.Props, "natural") ?? "-")
                                + (Mvt.Prop(f.Props, "water") is string w ? " water=" + w : "");
                    water.Add(new OsmArea(id, kind, v.Value, rings, GeoBox.Of(rings.SelectMany(r => r))) { Name = name });
                    continue;
                }
                if (OsmVendor.IsBuildingKept(f.GeomType, f.Props))
                {
                    var rings = Rings(f, x, tmsY, layer.Extent);
                    if (rings.Count > 0)
                        buildings.Add(new OsmArea(id, "building=" + (Mvt.Prop(f.Props, "building")
                                                                     ?? Mvt.Prop(f.Props, "aeroway")
                                                                     ?? Mvt.Prop(f.Props, "man_made") ?? "-"),
                                                  0, rings, GeoBox.Of(rings.SelectMany(r => r))));
                }
                if (OsmVendor.IsRiverLine(f.GeomType, f.Props))
                {
                    double width = OsmVendor.WaterwayWidthMeters(f.Props);
                    foreach (var part in f.Parts)
                    {
                        if (part.Count < 2) continue;
                        var pts = part.Select(p => OsmTileMath.ToLatLon(OsmTileMath.Zoom, x, tmsY, p.X, p.Y, layer.Extent))
                                      .ToList();
                        rivers.Add(new OsmLine(id, "waterway=" + Mvt.Prop(f.Props, "waterway"), width, pts,
                                               GeoBox.Of(pts)) { Name = name });
                    }
                }
                if (OsmVendor.IsRoadBridge(f.GeomType, f.Props))
                {
                    double width = OsmVendor.RoadWidthMeters(f.Props);
                    foreach (var part in f.Parts)
                    {
                        if (part.Count < 2) continue;
                        var pts = part.Select(p => OsmTileMath.ToLatLon(OsmTileMath.Zoom, x, tmsY, p.X, p.Y, layer.Extent))
                                      .ToList();
                        bridges.Add(new OsmLine(id, "highway=" + Mvt.Prop(f.Props, "highway") + " bridge", width, pts,
                                                GeoBox.Of(pts)));
                    }
                }
                var lu = OsmVendor.LandUse(f.GeomType, f.Props);
                if (lu != AggregateLandUse.None)
                {
                    var rings = Rings(f, x, tmsY, layer.Extent);
                    if (rings.Count > 0)
                        landUse.Add(new OsmArea(id, lu.ToString(), (int)lu, rings, GeoBox.Of(rings.SelectMany(r => r))));
                }
            }
        return new OsmTile
        {
            Set = set, X = x, TmsY = tmsY, Known = true, State = state,
            Water = water, Buildings = buildings, LandUse = landUse, Rivers = rivers, Bridges = bridges,
        };
    }

    private static List<IReadOnlyList<(double Lat, double Lon)>> Rings(Mvt.Feature f, int x, int tmsY, int extent)
    {
        var rings = new List<IReadOnlyList<(double Lat, double Lon)>>();
        foreach (var part in f.Parts)
        {
            if (part.Count < 3) continue;
            var ring = part.Select(p => OsmTileMath.ToLatLon(OsmTileMath.Zoom, x, tmsY, p.X, p.Y, extent)).ToList();
            if (ring[0] != ring[^1]) ring.Add(ring[0]);
            rings.Add(ring);
        }
        return rings;
    }
}

/// <summary>Where the pre-flight gets a tile from - the TileSource in service, a dictionary of
/// synthetic tiles in the self-test.</summary>
public delegate OsmTile OsmTileProvider(OsmSet set, int x, int tmsY);

/// <summary>
/// PURE PLANAR GEOMETRY in a local east/north metre frame, the construction leg_check.py's
/// OsmBuildings.leg uses (1 deg = rad(1) x 6,371 km north, x cos(lat0) east). Exact enough over the
/// few km a leg spans; the distances it produces are compared against 10-25 m clearances.
/// </summary>
public static class OsmGeometry
{
    public const double EarthRadiusM = 6371000.0;
    public static readonly double MetresPerDegree = Math.PI / 180.0 * EarthRadiusM;

    public readonly record struct Frame(double Lat0, double Lon0, double Kx, double Ky)
    {
        public static Frame At(double lat0, double lon0)
            => new(lat0, lon0, MetresPerDegree * Math.Cos(lat0 * Math.PI / 180.0), MetresPerDegree);
        public (double X, double Y) Xy((double Lat, double Lon) p) => ((p.Lon - Lon0) * Kx, (p.Lat - Lat0) * Ky);
        public (double Lat, double Lon) LatLon((double X, double Y) q) => (Lat0 + q.Y / Ky, Lon0 + q.X / Kx);
    }

    /// <summary>Even-odd over EVERY ring of a feature, so a hole (an island in a lake) is dry -
    /// the same rule as landcover_sector_map.py's fill.</summary>
    public static bool Inside((double X, double Y) p, IReadOnlyList<(double X, double Y)[]> rings)
    {
        bool c = false;
        foreach (var r in rings)
            for (int i = 0; i + 1 < r.Length; i++)
            {
                var (x1, y1) = r[i];
                var (x2, y2) = r[i + 1];
                if ((y1 > p.Y) != (y2 > p.Y) && p.X < (x2 - x1) * (p.Y - y1) / (y2 - y1) + x1) c = !c;
            }
        return c;
    }

    /// <summary>Distance from p to segment a-b, and the parameter of the closest point on a-b.</summary>
    public static (double D, double T) PointSegment((double X, double Y) p, (double X, double Y) a, (double X, double Y) b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double l = dx * dx + dy * dy;
        double t = l == 0 ? 0.0 : Math.Max(0.0, Math.Min(1.0, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l));
        return (Math.Sqrt(Math.Pow(p.X - a.X - t * dx, 2) + Math.Pow(p.Y - a.Y - t * dy, 2)), t);
    }

    private static double Orient((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    /// <summary>A PROPER crossing of p-q and r-s (leg_check.py _seg_seg_cross), with the parameter
    /// along p-q where they meet.</summary>
    public static bool Cross((double X, double Y) p, (double X, double Y) q, (double X, double Y) r,
                             (double X, double Y) s, out double t)
    {
        t = double.NaN;
        double d1 = Orient(r, s, p), d2 = Orient(r, s, q), d3 = Orient(p, q, r), d4 = Orient(p, q, s);
        if (((d1 > 0) != (d2 > 0)) && ((d3 > 0) != (d4 > 0)))
        {
            double den = d1 - d2;
            t = den == 0 ? 0.0 : d1 / den;
            return true;
        }
        return false;
    }

    /// <summary>Distance between segments a-b and c-d (0 when they cross), and the parameter along
    /// a-b of the closest approach.</summary>
    public static (double D, double T) SegmentSegment((double X, double Y) a, (double X, double Y) b,
                                                      (double X, double Y) c, (double X, double Y) d)
    {
        if (Cross(a, b, c, d, out double tc)) return (0.0, tc);
        var best = (D: PointSegment(a, c, d).D, T: 0.0);
        double d2 = PointSegment(b, c, d).D;
        if (d2 < best.D) best = (d2, 1.0);
        var (d3, t3) = PointSegment(c, a, b);
        if (d3 < best.D) best = (d3, t3);
        var (d4, t4) = PointSegment(d, a, b);
        if (d4 < best.D) best = (d4, t4);
        return best;
    }

    /// <summary>
    /// Segment a-b to a polygon (all rings, even-odd): 0 when an end is inside or the segment
    /// crosses an edge (leg_check.py seg_poly_dist, extended to rings with holes), else the
    /// smallest edge distance. The parameter is where along a-b that happens.
    /// </summary>
    public static (double D, double T) SegmentPolygon((double X, double Y) a, (double X, double Y) b,
                                                      IReadOnlyList<(double X, double Y)[]> rings)
    {
        if (Inside(a, rings)) return (0.0, 0.0);
        double firstCross = double.PositiveInfinity;
        var best = (D: double.PositiveInfinity, T: 0.0);
        foreach (var r in rings)
            for (int i = 0; i + 1 < r.Length; i++)
            {
                if (Cross(a, b, r[i], r[i + 1], out double tc))
                {
                    if (tc < firstCross) firstCross = tc;
                    continue;
                }
                var (d, t) = SegmentSegment(a, b, r[i], r[i + 1]);
                if (d < best.D) best = (d, t);
            }
        if (!double.IsPositiveInfinity(firstCross)) return (0.0, firstCross);
        if (Inside(b, rings)) return (0.0, 1.0);
        return best;
    }

    /// <summary>Point to polygon: 0 inside, else the nearest edge.</summary>
    public static double PointPolygon((double X, double Y) p, IReadOnlyList<(double X, double Y)[]> rings)
    {
        if (Inside(p, rings)) return 0.0;
        double best = double.PositiveInfinity;
        foreach (var r in rings)
            for (int i = 0; i + 1 < r.Length; i++)
            {
                double d = PointSegment(p, r[i], r[i + 1]).D;
                if (d < best) best = d;
            }
        return best;
    }

    /// <summary>Segment a-b to a polyline (0 when they cross), with the parameter along a-b.</summary>
    public static (double D, double T) SegmentPolyline((double X, double Y) a, (double X, double Y) b,
                                                       IReadOnlyList<(double X, double Y)> line)
    {
        var best = (D: double.PositiveInfinity, T: 0.0);
        for (int i = 0; i + 1 < line.Count; i++)
        {
            var (d, t) = SegmentSegment(a, b, line[i], line[i + 1]);
            if (d < best.D || (d == 0.0 && best.D == 0.0 && t < best.T)) best = (d, t);
        }
        return best;
    }

    public static double PointPolyline((double X, double Y) p, IReadOnlyList<(double X, double Y)> line)
    {
        double best = double.PositiveInfinity;
        for (int i = 0; i + 1 < line.Count; i++)
        {
            double d = PointSegment(p, line[i], line[i + 1]).D;
            if (d < best) best = d;
        }
        return best;
    }
}

/// <summary>What the OSM features say about ONE POINT (a route vertex, or a nudge candidate).</summary>
public sealed record OsmPointCheck
{
    public bool WaterKnown { get; init; }
    public bool Water { get; init; }
    public string WaterId { get; init; } = "";
    public string WaterKind { get; init; } = "";
    public double WaterDistanceM { get; init; } = double.PositiveInfinity;
    public double WaterCorridorM { get; init; }
    public bool BuildingsKnown { get; init; }
    public bool Building { get; init; }
    public string BuildingId { get; init; } = "";
    public double BuildingDistanceM { get; init; } = double.PositiveInfinity;
    public double BuildingClearanceM { get; init; }
    public int UnknownTiles { get; init; }
    public string UnknownWhy { get; init; } = "";
    /// <summary>Water was found, but the point is on (or, near water, beside) a drivable road
    /// bridge, so it is NOT water for this check - see OsmQuery.BridgeExempt.</summary>
    public bool OnBridge { get; init; }
    public string BridgeId { get; init; } = "";

    public bool Known => WaterKnown && BuildingsKnown;
    public bool Bad => Water || Building;
    public bool Clear => Known && !Bad;

    /// <summary>One clause naming what is wrong here, for the log and the report.</summary>
    public string Describe()
    {
        var parts = new List<string>();
        if (Water)
            parts.Add(WaterDistanceM <= 0.0
                ? $"IN OSM water ({WaterKind}, OSM {WaterId})"
                : FormattableString.Invariant($"{WaterDistanceM:F1} m from OSM water ({WaterKind}, OSM {WaterId}), inside the {WaterCorridorM:F0} m water clearance"));
        if (Building)
            parts.Add(BuildingDistanceM <= 0.0
                ? $"INSIDE the footprint of OSM building {BuildingId}"
                : FormattableString.Invariant($"{BuildingDistanceM:F1} m from OSM building {BuildingId}, inside the {BuildingClearanceM:F0} m building clearance"));
        if (parts.Count == 0 && !Known) parts.Add($"UNKNOWN - {UnknownTiles} OSM tile(s) not readable ({UnknownWhy})");
        if (parts.Count == 0 && OnBridge)
            return $"clear - on OSM road bridge {BridgeId} (the water under or beside its deck does not count)";
        return parts.Count == 0 ? "clear" : string.Join(" and ", parts);
    }
}

/// <summary>What the OSM features say about ONE LEG, under one model set's rules.</summary>
public sealed record OsmLegFeatures
{
    public double CorridorM { get; init; }
    public int Samples { get; init; }
    /// <summary>Samples with water within the corridor (the extent; the verdict is <see cref="Water"/>).</summary>
    public int WaterSamples { get; init; }
    public double WaterFirstSM { get; init; } = double.NaN;
    public double WaterLastSM { get; init; } = double.NaN;
    public (double Lat, double Lon) WaterFirst { get; init; }
    /// <summary>EXACT: the leg's nearest approach to water (0 = it crosses or ends in it).</summary>
    public double WaterMinDistanceM { get; init; } = double.PositiveInfinity;
    public string WaterId { get; init; } = "";
    public string WaterKind { get; init; } = "";
    public int WaterValue { get; init; }
    public bool WaterIsRiverLine { get; init; }
    /// <summary>THE VERDICT: water within the corridor of the line (exact geometry, not samples).</summary>
    public bool Water { get; init; }
    public int UnknownTiles { get; init; }
    public IReadOnlyList<string> UnknownTileNames { get; init; } = Array.Empty<string>();
    /// <summary>Aggregate RESTRICTED_L2 length along the centreline, metres (sampled).</summary>
    public double ForestM { get; init; }
    public double SwampM { get; init; }
    public double MunicipalM { get; init; }
    public bool LandUseRead { get; init; }
    public double LengthM { get; init; }
    /// <summary>OSM road bridge(s) whose deck (or, near water, approach zone) made some of this leg's
    /// water contact NOT count - OsmQuery.BridgeExempt. Empty when no bridge was involved.</summary>
    public IReadOnlyList<string> BridgeIds { get; init; } = Array.Empty<string>();

    public double SlowM => ForestM + SwampM + MunicipalM;
    public bool Known => UnknownTiles == 0;
}

/// <summary>leg_check.py's --osm-buildings answer for one leg: the parity instrument, and an INFO line
/// in the service. Buildings are NOT a leg flag on either profile (RL-20260927-01).</summary>
public sealed record OsmLegBuildings(double ClearanceM, double? MinM, string MinId,
                                     IReadOnlyList<(string Id, double DistanceM)> Within, bool Flagged,
                                     int UnknownTiles);

/// <summary>
/// THE QUERIES, pure given a tile provider. Which tiles a question touches is decided here, the same
/// way leg_check.py's OsmBuildings.leg decides it (the line and both edges of its buffer, sampled every
/// step), and every tile touched that is not KNOWN is counted - never skipped as empty.
/// </summary>
public static class OsmQuery
{
    /// <summary>Metres added to a buffer when choosing tiles, so a feature just outside the buffer on
    /// a neighbouring tile is still loaded before it is ruled out.</summary>
    public const double TileMarginM = 10.0;

    /// <summary>Metres beyond a road bridge's half MAK_WIDTH that still count as ON it (OSM line
    /// geometry against a rendered deck).</summary>
    public const double BridgeToleranceM = 3.0;

    /// <summary>
    /// THE BRIDGE RULE, pure. The plan's remedy for a river on a leg is STP re-authoring the route over a
    /// BRIDGE (PLAN_MOVEMENT sec 0; RL-20260927-01), so the checks must not undo it: a vertex on a
    /// bridge deck is not "in water" (moving it would put the unit on one bank), and a leg crossing on a
    /// bridge is not "water on the line" (flagging it would ask STP for the bridge it already used).
    /// A point IN water (distance 0) is exempt only ON the deck: within half the bridge's MAK_WIDTH +
    /// <see cref="BridgeToleranceM"/> of its line. A point merely NEAR water (inside the corridor, not in
    /// it) is exempt anywhere in the bridge's approach zone: within corridor + half width + tolerance.
    /// Nothing else is exempt - water 20 m beside a bridge, in the river, is still water.
    /// </summary>
    public static bool BridgeExempt(double waterDistanceM, double bridgeDistanceM, double bridgeWidthM, double corridorM)
    {
        double deck = bridgeWidthM / 2.0 + BridgeToleranceM;
        return waterDistanceM <= 0.0 ? bridgeDistanceM <= deck : bridgeDistanceM <= corridorM + deck;
    }

    /// <summary>The nearest road bridge to a point (local frame), or +infinity.</summary>
    private static (double D, double Width, string Id) NearestBridge((double X, double Y) p, IEnumerable<OsmTile> tiles,
                                                                    OsmGeometry.Frame f, GeoBox near, double reachM)
    {
        var best = (D: double.PositiveInfinity, Width: OsmVendor.DefaultRoadWidthMeters, Id: "");
        foreach (var t in tiles)
            foreach (var br in t.Bridges)
            {
                if (!br.Box.Near(near, reachM + br.WidthM)) continue;
                double d = OsmGeometry.PointPolyline(p, br.Points.Select(f.Xy).ToList());
                // Compare on the EXEMPTION margin, not the raw distance: a wide bridge a little
                // further away can still carry the point.
                if (d - br.WidthM / 2.0 < best.D - best.Width / 2.0) best = (d, br.WidthM, br.Id);
            }
        return best;
    }

    private static readonly (double X, double Y)[] Compass =
    {
        (0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1),
    };

    /// <summary>The z14 tiles a point's neighbourhood of <paramref name="radiusM"/> touches (the
    /// point and the eight corners/edges of the square around it - a tile is ~1.4 km, the square a
    /// few tens of metres, so no tile the square overlaps is missed).</summary>
    public static List<(int X, int TmsY)> TilesAround((double Lat, double Lon) p, double radiusM)
    {
        var f = OsmGeometry.Frame.At(p.Lat, p.Lon);
        var keys = new List<(int, int)>();
        foreach (var (dx, dy) in Compass)
        {
            var q = f.LatLon((dx * radiusM, dy * radiusM));
            var k = OsmTileMath.TileOf(q.Lat, q.Lon);
            if (!keys.Contains(k)) keys.Add(k);
        }
        return keys;
    }

    /// <summary>The z14 tiles a leg's buffer of <paramref name="halfWidthM"/> touches: the line and both
    /// buffer edges, sampled every step (leg_check.py OsmBuildings.leg).</summary>
    public static List<(int X, int TmsY)> TilesAlong((double Lat, double Lon) a, (double Lat, double Lon) b,
                                                     double halfWidthM, double stepM)
    {
        var f = OsmGeometry.Frame.At(a.Lat, a.Lon);
        var B = f.Xy(b);
        double len = Math.Sqrt(B.X * B.X + B.Y * B.Y);
        int n = Math.Max(1, (int)Math.Ceiling(len / Math.Max(1.0, stepM)));
        double ux = len > 0 ? B.X / len : 1.0, uy = len > 0 ? B.Y / len : 0.0;
        var keys = new HashSet<(int, int)>();
        var order = new List<(int, int)>();
        for (int i = 0; i <= n; i++)
        {
            double t = (double)i / n;
            double px = B.X * t, py = B.Y * t;
            foreach (double off in new[] { -halfWidthM, 0.0, halfWidthM })
            {
                var q = f.LatLon((px + off * uy, py - off * ux));
                var k = OsmTileMath.TileOf(q.Lat, q.Lon);
                if (keys.Add(k)) order.Add(k);
            }
        }
        return order;
    }

    private static (double X, double Y)[][] LocalRings(OsmGeometry.Frame f, OsmArea a)
        => a.Rings.Select(r => r.Select(p => f.Xy(p)).ToArray()).ToArray();

    /// <summary>
    /// ONE POINT against the rules' water (osm-water areas; the aggregate also counts River lines at
    /// their MAK_WIDTH) and against building footprints. Water is a hit when it lies within the
    /// rules' water corridor of the point (0 = the point is in it); a building when its footprint is
    /// strictly closer than <paramref name="buildingClearanceM"/> (leg_check's "within").
    /// </summary>
    public static OsmPointCheck Point(OsmTileProvider tiles, (double Lat, double Lon) p, ModelSetRules rules,
                                      double buildingClearanceM)
    {
        var f = OsmGeometry.Frame.At(p.Lat, p.Lon);
        var here = (0.0, 0.0);
        int unknown = 0;
        var why = new List<string>();

        // ---- water
        double corridor = rules.WaterCorridorMeters;
        bool waterKnown = true;
        double wd = double.PositiveInfinity;
        string wid = "", wkind = "";
        var waterTiles = new List<OsmTile>();
        foreach (var (x, y) in TilesAround(p, corridor + TileMarginM))
        {
            var t = tiles(OsmSet.Water, x, y);
            if (t == null || !t.Known) { waterKnown = false; unknown++; why.Add(t?.State ?? "no tile"); continue; }
            waterTiles.Add(t);
        }
        var near = new GeoBox(p.Lat, p.Lon, p.Lat, p.Lon);
        foreach (var t in waterTiles)
            foreach (var a in t.Water)
            {
                if (!a.Box.Near(near, corridor + 1.0)) continue;
                double d = OsmGeometry.PointPolygon(here, LocalRings(f, a));
                if (d < wd) { wd = d; wid = a.Id; wkind = a.Kind; }
            }
        var allTiles = new List<OsmTile>();
        bool allKnown = true;
        double clearance = Math.Max(0.0, buildingClearanceM);
        double bridgeReach = corridor + OsmVendor.DefaultRoadWidthMeters / 2.0 + BridgeToleranceM;
        double allRadius = Math.Max(Math.Max(clearance, bridgeReach),
                                    rules.RiverLinesAreWater ? corridor + OsmVendor.DefaultWaterwayWidthMeters : 0.0)
                         + TileMarginM;
        foreach (var (x, y) in TilesAround(p, allRadius))
        {
            var t = tiles(OsmSet.All, x, y);
            if (t == null || !t.Known) { allKnown = false; unknown++; why.Add(t?.State ?? "no tile"); continue; }
            allTiles.Add(t);
        }
        if (rules.RiverLinesAreWater)
        {
            if (!allKnown) waterKnown = false;
            foreach (var t in allTiles)
                foreach (var r in t.Rivers)
                {
                    if (!r.Box.Near(near, corridor + r.WidthM)) continue;
                    double d = Math.Max(0.0, OsmGeometry.PointPolyline(here, r.Points.Select(f.Xy).ToList()) - r.WidthM / 2.0);
                    if (d < wd) { wd = d; wid = r.Id; wkind = r.Kind + FormattableString.Invariant($" (MAK_WIDTH {r.WidthM:F0} m)"); }
                }
        }

        // ---- buildings
        double bd = double.PositiveInfinity;
        string bid = "";
        foreach (var t in allTiles)
            foreach (var a in t.Buildings)
            {
                if (!a.Box.Near(near, clearance + 1.0)) continue;
                double d = OsmGeometry.PointPolygon(here, LocalRings(f, a));
                if (d < bd) { bd = d; bid = a.Id; }
            }

        // ---- the bridge rule: water under (or beside) a drivable road bridge's deck is not water here
        bool water = wd <= corridor;
        bool onBridge = false;
        string bridgeId = "";
        if (water)
        {
            var (bd0, bw0, bid0) = NearestBridge(here, allTiles, f, near, bridgeReach);
            if (BridgeExempt(wd, bd0, bw0, corridor)) { water = false; onBridge = true; bridgeId = bid0; }
        }

        return new OsmPointCheck
        {
            WaterKnown = waterKnown,
            Water = water,
            OnBridge = onBridge,
            BridgeId = bridgeId,
            WaterId = wid, WaterKind = wkind, WaterDistanceM = wd, WaterCorridorM = corridor,
            BuildingsKnown = allKnown,
            Building = bd < clearance,
            BuildingId = bid, BuildingDistanceM = bd, BuildingClearanceM = clearance,
            UnknownTiles = unknown,
            UnknownWhy = string.Join("; ", why.Distinct().Take(3)),
        };
    }

    private sealed record WaterShape(string Id, string Kind, int Value, bool Line, double HalfWidth,
                                     (double X, double Y)[][] Rings, List<(double X, double Y)> Points)
    {
        public string Name { get; init; } = "";
    }

    /// <summary>
    /// How far a water DISTANCE is measured beyond the corridor. The VERDICT only ever needs the
    /// corridor; the distance past it is what lets the chooser prefer, between two equally clear
    /// candidates (every water-clear candidate ties at ratio 0 on the aggregate profile), the one that
    /// stays FURTHER from water, and what lets a log say "nearest water 60 m" instead of nothing.
    /// Tiles beyond the corridor are read for the distance only: an unreadable one there makes a
    /// line's distance an overestimate, never its verdict UNKNOWN.
    /// </summary>
    public const double WaterMeasureHorizonM = 100.0;

    private static List<WaterShape> WaterShapes(OsmTileProvider tiles, (double Lat, double Lon) a,
                                                (double Lat, double Lon) b, ModelSetRules rules, double stepM,
                                                OsmGeometry.Frame f, List<string> unknownNames)
    {
        double corridor = rules.WaterCorridorMeters;
        double reach = Math.Max(corridor, WaterMeasureHorizonM);
        var box = GeoBox.Of(new[] { a, b });
        var shapes = new List<WaterShape>();
        var seen = new HashSet<(OsmSet, int, int)>();
        void AddWater(OsmTile t)
        {
            foreach (var w in t.Water)
                if (w.Box.Near(box, reach + 1.0))
                    shapes.Add(new WaterShape(w.Id, w.Kind, w.Value, false, 0.0, LocalRings(f, w), null) { Name = w.Name });
        }
        void AddRivers(OsmTile t)
        {
            foreach (var r in t.Rivers)
                if (r.Box.Near(box, reach + r.WidthM + 1.0))
                    shapes.Add(new WaterShape(r.Id, r.Kind + FormattableString.Invariant($" (MAK_WIDTH {r.WidthM:F0} m)"),
                                              0, true, r.WidthM / 2.0, null, r.Points.Select(f.Xy).ToList())
                               { Name = r.Name });
        }
        // The CORRIDOR's tiles decide UNKNOWN; the horizon's are read for the distance only.
        foreach (var (x, y) in TilesAlong(a, b, corridor + TileMarginM, stepM))
        {
            if (!seen.Add((OsmSet.Water, x, y))) continue;
            var t = tiles(OsmSet.Water, x, y);
            if (t == null || !t.Known) { unknownNames.Add(t?.Name ?? $"osm-water/{OsmTileMath.FileName(x, y)}"); continue; }
            AddWater(t);
        }
        foreach (var (x, y) in TilesAlong(a, b, reach + TileMarginM, Math.Max(stepM, 25.0)))
        {
            if (!seen.Add((OsmSet.Water, x, y))) continue;
            var t = tiles(OsmSet.Water, x, y);
            if (t != null && t.Known) AddWater(t);
        }
        if (rules.RiverLinesAreWater)
        {
            foreach (var (x, y) in TilesAlong(a, b, corridor + OsmVendor.DefaultWaterwayWidthMeters + TileMarginM, stepM))
            {
                if (!seen.Add((OsmSet.All, x, y))) continue;
                var t = tiles(OsmSet.All, x, y);
                if (t == null || !t.Known) { unknownNames.Add(t?.Name ?? $"osm/{OsmTileMath.FileName(x, y)}"); continue; }
                AddRivers(t);
            }
            foreach (var (x, y) in TilesAlong(a, b, reach + OsmVendor.DefaultWaterwayWidthMeters + TileMarginM,
                                              Math.Max(stepM, 25.0)))
            {
                if (!seen.Add((OsmSet.All, x, y))) continue;
                var t = tiles(OsmSet.All, x, y);
                if (t != null && t.Known) AddRivers(t);
            }
        }
        return shapes;
    }

    private static (double D, double T) ShapeSegment(WaterShape s, (double X, double Y) a, (double X, double Y) b)
    {
        if (!s.Line) return OsmGeometry.SegmentPolygon(a, b, s.Rings);
        var (d, t) = OsmGeometry.SegmentPolyline(a, b, s.Points);
        return (Math.Max(0.0, d - s.HalfWidth), t);
    }

    private static double ShapePoint(WaterShape s, (double X, double Y) p)
        => s.Line ? Math.Max(0.0, OsmGeometry.PointPolyline(p, s.Points) - s.HalfWidth)
                  : OsmGeometry.PointPolygon(p, s.Rings);

    /// <summary>
    /// ONE LEG against the rules' water, and - on the aggregate profile - its RESTRICTED_L2 land use.
    /// The VERDICT is exact (segment-to-feature distance within the corridor); the samples, spaced as
    /// LegScorer spaces them, only give the along-leg extent the detour window and the report use.
    /// </summary>
    public static OsmLegFeatures Leg(OsmTileProvider tiles, (double Lat, double Lon) a, (double Lat, double Lon) b,
                                     ModelSetRules rules, double stepM)
    {
        var f = OsmGeometry.Frame.At(a.Lat, a.Lon);
        var A = f.Xy(a);
        var B = f.Xy(b);
        double len = Math.Sqrt(B.X * B.X + B.Y * B.Y);
        double corridor = rules.WaterCorridorMeters;
        var unknownNames = new List<string>();
        var shapes = WaterShapes(tiles, a, b, rules, stepM, f, unknownNames);

        double minD = double.PositiveInfinity, minT = 0.0;
        WaterShape gov = null;
        foreach (var s in shapes)
        {
            var (d, t) = ShapeSegment(s, A, B);
            if (d < minD || (d == minD && d == 0.0 && t < minT)) { minD = d; minT = t; gov = s; }
        }

        bool water = minD <= corridor;

        // THE BRIDGE RULE (BridgeExempt): when the exact test finds water and a drivable road bridge
        // lies within its exemption reach of the line, the verdict is re-taken on 1 m samples with the
        // bridge's deck / approach zone excluded - a leg STP routed over a bridge is not "water on the
        // line". The osm tiles for this are read only here; an unreadable one exempts nothing.
        var legBridges = new List<(string Id, double W, List<(double X, double Y)> Pts)>();
        var bridgeUsed = new HashSet<string>(StringComparer.Ordinal);
        if (water && len > 0)
        {
            double reach = corridor + OsmVendor.DefaultRoadWidthMeters / 2.0 + BridgeToleranceM;
            var legBox = GeoBox.Of(new[] { a, b });
            var seenB = new HashSet<(int, int)>();
            foreach (var (x, y) in TilesAlong(a, b, reach + TileMarginM, stepM))
            {
                if (!seenB.Add((x, y))) continue;
                var t = tiles(OsmSet.All, x, y);
                if (t == null || !t.Known) continue;
                foreach (var br in t.Bridges)
                {
                    if (!br.Box.Near(legBox, reach + br.WidthM)) continue;
                    var pts = br.Points.Select(f.Xy).ToList();
                    if (OsmGeometry.SegmentPolyline(A, B, pts).D <= corridor + br.WidthM / 2.0 + BridgeToleranceM)
                        legBridges.Add((br.Id, br.WidthM, pts));
                }
            }
        }
        double WaterAt((double X, double Y) p)
        {
            double dw = double.PositiveInfinity;
            foreach (var sh in shapes) dw = Math.Min(dw, ShapePoint(sh, p));
            return dw;
        }
        bool Exempt((double X, double Y) p, double dw)
        {
            foreach (var br in legBridges)
                if (BridgeExempt(dw, OsmGeometry.PointPolyline(p, br.Pts), br.W, corridor))
                { bridgeUsed.Add(br.Id); return true; }
            return false;
        }

        int n = LegScorer.SampleCount(len, Math.Max(0.5, stepM));
        int wet = 0;
        double first = double.NaN, last = double.NaN;
        (double Lat, double Lon) firstLL = default;
        if (shapes.Count > 0)
            for (int i = 0; i < n; i++)
            {
                double s = LegScorer.SampleDistance(len, i, n);
                double t = len > 0 ? s / len : 0.0;
                var p = (A.X + (B.X - A.X) * t, A.Y + (B.Y - A.Y) * t);
                double dw = WaterAt(p);
                if (dw > corridor) continue;
                if (legBridges.Count > 0 && Exempt(p, dw)) continue;
                wet++;
                if (double.IsNaN(first)) { first = s; firstLL = f.LatLon(p); }
                last = s;
            }
        if (water && legBridges.Count > 0)
        {
            // The verdict with bridges involved: any 1 m sample in water's corridor that no bridge
            // carries. (The step samples above only give the extent a report prints.)
            int nf = LegScorer.SampleCount(len, 1.0);
            bool any = false;
            double f0 = double.NaN, f1 = double.NaN;
            (double Lat, double Lon) fLL = default;
            for (int i = 0; i < nf; i++)
            {
                double s = LegScorer.SampleDistance(len, i, nf);
                double t = s / len;
                var p = (A.X + (B.X - A.X) * t, A.Y + (B.Y - A.Y) * t);
                double dw = WaterAt(p);
                if (dw > corridor || Exempt(p, dw)) continue;
                any = true;
                if (double.IsNaN(f0)) { f0 = s; fLL = f.LatLon(p); }
                f1 = s;
            }
            water = any;
            if (any) { first = f0; last = f1; firstLL = fLL; }
            else { wet = 0; first = last = double.NaN; firstLL = default; }
        }
        else if (water && double.IsNaN(first))
        {
            // A crossing narrower than the sample spacing: the exact test found it, the samples
            // stepped over it. The extent is the exact point of closest approach.
            first = last = minT * len;
            firstLL = f.LatLon((A.X + (B.X - A.X) * minT, A.Y + (B.Y - A.Y) * minT));
        }

        // ---- aggregate RESTRICTED_L2 land use along the centreline (REPORT only)
        double forest = 0, swamp = 0, municipal = 0;
        bool landUseRead = false;
        if (rules.ReportSlowTerrain && len > 0)
        {
            landUseRead = true;
            var areas = new List<(AggregateLandUse Kind, (double X, double Y)[][] Rings)>();
            var box = GeoBox.Of(new[] { a, b });
            var seenLu = new HashSet<(int, int)>();
            foreach (var (x, y) in TilesAlong(a, b, TileMarginM, stepM))
            {
                if (!seenLu.Add((x, y))) continue;
                var t = tiles(OsmSet.All, x, y);
                if (t == null || !t.Known)
                {
                    string nm = t?.Name ?? $"osm/{OsmTileMath.FileName(x, y)}";
                    if (!unknownNames.Contains(nm)) unknownNames.Add(nm);
                    continue;
                }
                foreach (var lu in t.LandUse)
                    if (lu.Box.Near(box, 1.0)) areas.Add(((AggregateLandUse)lu.Value, LocalRings(f, lu)));
            }
            double ds = n > 1 ? len / (n - 1) : 0.0;
            for (int i = 0; i < n && areas.Count > 0; i++)
            {
                double s = LegScorer.SampleDistance(len, i, n);
                double t = s / len;
                var p = (A.X + (B.X - A.X) * t, A.Y + (B.Y - A.Y) * t);
                double w = (i == 0 || i == n - 1) ? ds / 2.0 : ds;
                foreach (var (kind, rings) in areas)
                {
                    if (!OsmGeometry.Inside(p, rings)) continue;
                    if (kind == AggregateLandUse.Forest) forest += w;
                    else if (kind == AggregateLandUse.Swamp) swamp += w;
                    else municipal += w;
                    break;
                }
            }
        }

        return new OsmLegFeatures
        {
            CorridorM = corridor,
            Samples = n,
            WaterSamples = wet,
            WaterFirstSM = first,
            WaterLastSM = last,
            WaterFirst = firstLL,
            WaterMinDistanceM = minD,
            WaterId = gov?.Id ?? "",
            WaterKind = gov?.Kind ?? "",
            WaterValue = gov?.Value ?? 0,
            WaterIsRiverLine = gov?.Line ?? false,
            Water = water,
            UnknownTiles = unknownNames.Distinct().Count(),
            UnknownTileNames = unknownNames.Distinct().ToList(),
            ForestM = forest, SwampM = swamp, MunicipalM = municipal,
            LandUseRead = landUseRead,
            LengthM = len,
            BridgeIds = bridgeUsed.OrderBy(x => x, StringComparer.Ordinal).ToList(),
        };
    }

    /// <summary>
    /// WHICH water lies within the rules' corridor of a polyline: (id, name) per feature, distinct, and
    /// how many tiles it touched were UNKNOWN. The river test's instrument - it asks whether the water at
    /// a band end is the SAME water as on the leg.
    /// </summary>
    public static (List<(string Id, string Name)> Hits, int UnknownTiles) WaterHits(
        OsmTileProvider tiles, IReadOnlyList<(double Lat, double Lon)> poly, ModelSetRules rules, double stepM)
    {
        var hits = new List<(string Id, string Name)>();
        var unknown = new HashSet<string>();
        for (int i = 0; i + 1 < (poly?.Count ?? 0); i++)
        {
            var a = poly[i];
            var b = poly[i + 1];
            if (TileMath.DistanceMeters(a.Lat, a.Lon, b.Lat, b.Lon) < LegScorer.MinLegM) continue;
            var f = OsmGeometry.Frame.At(a.Lat, a.Lon);
            var names = new List<string>();
            var shapes = WaterShapes(tiles, a, b, rules, stepM, f, names);
            foreach (var nm in names) unknown.Add(nm);
            var A = f.Xy(a);
            var B = f.Xy(b);
            foreach (var s in shapes)
                if (ShapeSegment(s, A, B).D <= rules.WaterCorridorMeters && !hits.Contains((s.Id, s.Name)))
                    hits.Add((s.Id, s.Name));
        }
        return (hits, unknown.Count);
    }

    /// <summary>
    /// A CANDIDATE polyline (a detour, a band end): does any of its segments come within the rules'
    /// corridor of water, how close does it get, and how many tiles it touched were UNKNOWN. Exact per
    /// segment, no samples - this is the acceptance test, not the report.
    /// </summary>
    public static (int WetSegments, double MinDistanceM, int UnknownTiles) Polyline(
        OsmTileProvider tiles, IReadOnlyList<(double Lat, double Lon)> poly, ModelSetRules rules, double stepM)
    {
        int wetSegs = 0;
        double minD = double.PositiveInfinity;
        var unknown = new HashSet<string>();
        for (int i = 0; i + 1 < (poly?.Count ?? 0); i++)
        {
            var a = poly[i];
            var b = poly[i + 1];
            if (TileMath.DistanceMeters(a.Lat, a.Lon, b.Lat, b.Lon) < LegScorer.MinLegM) continue;
            var f = OsmGeometry.Frame.At(a.Lat, a.Lon);
            var names = new List<string>();
            var shapes = WaterShapes(tiles, a, b, rules, stepM, f, names);
            foreach (var nm in names) unknown.Add(nm);
            var A = f.Xy(a);
            var B = f.Xy(b);
            double segMin = double.PositiveInfinity;
            foreach (var s in shapes)
            {
                double d = ShapeSegment(s, A, B).D;
                if (d < segMin) segMin = d;
            }
            if (segMin < minD) minD = segMin;
            if (segMin <= rules.WaterCorridorMeters) wetSegs++;
        }
        return (wetSegs, minD, unknown.Count);
    }

    /// <summary>
    /// leg_check.py OsmBuildings.leg, transcribed - THE PARITY INSTRUMENT for --osm-buildings: every
    /// footprint piece within max(250 m, clearance) of the leg's box, its segment-to-ring distance,
    /// the nearest per OSM id, and every id strictly closer than the clearance. Tiles touched by the
    /// line and both clearance edges (sampled every step) that are absent or empty are UNKNOWN.
    /// </summary>
    public static OsmLegBuildings LegBuildings(OsmTileProvider tiles, (double Lat, double Lon) a,
                                               (double Lat, double Lon) b, double clearanceM, double stepM)
    {
        const double NearCapM = 250.0;
        var f = OsmGeometry.Frame.At(a.Lat, a.Lon);
        var A = f.Xy(a);
        var B = f.Xy(b);
        int unknown = 0;
        var known = new List<OsmTile>();
        foreach (var (x, y) in TilesAlong(a, b, clearanceM, stepM))
        {
            var t = tiles(OsmSet.All, x, y);
            if (t == null || !t.Known) { unknown++; continue; }
            known.Add(t);
        }
        double cap = Math.Max(NearCapM, clearanceM);
        var box = GeoBox.Of(new[] { a, b });
        var best = new Dictionary<string, double>(StringComparer.Ordinal);
        // The neighbourhood within the cap is wider than the clearance buffer, so load the tiles the
        // cap reaches too - read for the NEAREST distance only; they do not decide UNKNOWN (leg_check
        // counts unknown over the clearance buffer, and so does this).
        foreach (var (x, y) in TilesAlong(a, b, cap, Math.Max(stepM, 50.0)))
        {
            var t = tiles(OsmSet.All, x, y);
            if (t != null && t.Known && !known.Contains(t)) known.Add(t);
        }
        foreach (var t in known)
            foreach (var bld in t.Buildings)
            {
                if (!bld.Box.Near(box, cap)) continue;
                foreach (var ring in bld.Rings)
                {
                    var pts = ring.Select(p => f.Xy(p)).ToArray();
                    double d = SegPolyLegCheck(A, B, pts);
                    if (d <= cap && (!best.TryGetValue(bld.Id, out double prev) || d < prev)) best[bld.Id] = d;
                }
            }
        var near = best.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList();
        var within = near.Where(kv => kv.Value < clearanceM)
                         .Select(kv => (kv.Key, Math.Round(kv.Value, 1, MidpointRounding.ToEven))).ToList();
        return new OsmLegBuildings(clearanceM, near.Count > 0 ? near[0].Value : null,
                                   near.Count > 0 ? near[0].Key : "", within, within.Count > 0, unknown);
    }

    /// <summary>leg_check.py seg_poly_dist, one ring, exactly (its inside test needs >= 4 points).</summary>
    private static double SegPolyLegCheck((double X, double Y) a, (double X, double Y) b, (double X, double Y)[] ring)
    {
        var rings = new[] { ring };
        if (ring.Length >= 4 && (OsmGeometry.Inside(a, rings) || OsmGeometry.Inside(b, rings))) return 0.0;
        double best = double.PositiveInfinity;
        for (int i = 0; i + 1 < ring.Length; i++)
        {
            var r = ring[i];
            var s = ring[i + 1];
            if (OsmGeometry.Cross(a, b, r, s, out _)) return 0.0;
            best = Math.Min(best, Math.Min(Math.Min(OsmGeometry.PointSegment(a, r, s).D, OsmGeometry.PointSegment(b, r, s).D),
                                           Math.Min(OsmGeometry.PointSegment(r, a, b).D, OsmGeometry.PointSegment(s, a, b).D)));
        }
        return best;
    }
}
