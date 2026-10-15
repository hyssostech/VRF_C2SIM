using System.Globalization;

namespace VrfC2SimApp.Preflight;

/// <summary>
/// STP-805 (Prep 3 of STP-802): WARM THE ROUTE PRE-FLIGHT TILE CACHE FOR AN AO, ONCE, BEFORE THE RUN.
///
/// WHY. The route pre-flight scores legs from cached elevation GeoTIFFs, land-cover PNGs and OSM feature tiles
/// (TileSource). On a fresh AO the cache is empty, every leg is "no verdict", and a run with the pre-flight on issues
/// hundreds of live HTTP fetches from worker threads during the first order. This walks every tile the pre-flight can
/// ask for over a bounding box and fetches it through the SAME TileSource the interface uses - same servers, same cache
/// layout, same absent/failed rules - so the run profile can then set Vrf:PreflightOffline=true and never depend on the
/// internet.
///
/// WHAT IT ENUMERATES (Plan): the elevation tiles at the cascade's START level (Vrf:PreflightElevationLevel; the
/// cascade itself fetches coarser levels where the server has none - Elevation() at each tile centre), the tiles of
/// every land-cover source at that source's level (TileMath.LandCoverSources), and the z14 OSM tiles of every OsmSet
/// (water, all features, highways). Geodetic TMS tiles are 180 / 2^level degrees square; OSM tiles are web-mercator z14.
///
/// SAFETY. The cache directory is REQUIRED (no default): the warm-up never writes into a run's configured cache by
/// accident. A box larger than <see cref="MaxTiles"/> elevation tiles is refused unless forced. --dry-run fetches
/// nothing and only prints the plan.
/// </summary>
public static class TileWarmup
{
    /// <summary>Refuse a plan with more elevation tiles than this unless forced (~1 degree square at L13 is 2,048).</summary>
    public const int MaxTiles = 4096;

    public readonly record struct Box(double South, double West, double North, double East);

    public sealed record Plan(Box Box, int ElevationLevel,
                              IReadOnlyList<(double Lat, double Lon)> ElevationCentres,
                              IReadOnlyList<(int Ds, int Level, string Label, double Lat, double Lon)> CoverCentres,
                              IReadOnlyList<(OsmSet Set, int X, int TmsY)> OsmTiles);

    /// <summary>Parse "S,W,N,E" (decimal degrees, invariant culture). Null + reason on anything malformed.</summary>
    public static Box? ParseBox(string text, out string error)
    {
        error = null;
        var parts = (text ?? "").Split(',');
        if (parts.Length != 4) { error = "expected S,W,N,E (four comma-separated decimal degrees)"; return null; }
        var v = new double[4];
        for (int i = 0; i < 4; i++)
            if (!double.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]))
            { error = $"'{parts[i].Trim()}' is not a decimal number"; return null; }
        if (v[0] < -85 || v[2] > 85 || v[0] >= v[2]) { error = "need -85 <= S < N <= 85"; return null; }
        if (v[1] < -180 || v[3] > 180 || v[1] >= v[3]) { error = "need -180 <= W < E <= 180 (no antimeridian crossing)"; return null; }
        return new Box(v[0], v[1], v[2], v[3]);
    }

    /// <summary>The geodetic TMS tiles (x, y) covering the box at a level, and their centres.</summary>
    public static List<(int X, int Y, double Lat, double Lon)> GeodeticTiles(Box b, int level)
    {
        double size = 180.0 / Math.Pow(2, level);
        int x0 = (int)Math.Floor((b.West + 180.0) / size), x1 = (int)Math.Floor((b.East + 180.0) / size);
        int y0 = (int)Math.Floor((b.South + 90.0) / size), y1 = (int)Math.Floor((b.North + 90.0) / size);
        var list = new List<(int, int, double, double)>();
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                list.Add((x, y, (y + 0.5) * size - 90.0, (x + 0.5) * size - 180.0));
        return list;
    }

    /// <summary>The web-mercator z14 OSM tiles (x, TMS y) covering the box.</summary>
    public static List<(int X, int TmsY)> OsmTilesOf(Box b)
    {
        var (xa, ya) = OsmTileMath.TileOf(b.South, b.West);
        var (xb, yb) = OsmTileMath.TileOf(b.North, b.East);
        var list = new List<(int, int)>();
        for (int y = Math.Min(ya, yb); y <= Math.Max(ya, yb); y++)
            for (int x = Math.Min(xa, xb); x <= Math.Max(xa, xb); x++)
                list.Add((x, y));
        return list;
    }

    public static Plan Build(Box b, int elevationLevel = TileMath.DefaultElevationLevel)
    {
        var elev = GeodeticTiles(b, elevationLevel).Select(t => (t.Lat, t.Lon)).ToList();
        var cover = new List<(int, int, string, double, double)>();
        foreach (var (ds, level, label) in TileMath.LandCoverSources)
            foreach (var t in GeodeticTiles(b, level)) cover.Add((ds, level, label, t.Lat, t.Lon));
        var osm = new List<(OsmSet, int, int)>();
        foreach (var set in Enum.GetValues<OsmSet>())
            foreach (var (x, y) in OsmTilesOf(b)) osm.Add((set, x, y));
        return new Plan(b, elevationLevel, elev, cover, osm);
    }

    /// <summary>Refuse an oversized plan unless forced. Null = go.</summary>
    public static string Refusal(Plan p, bool force)
        => !force && p.ElevationCentres.Count > MaxTiles
            ? $"REFUSED: {p.ElevationCentres.Count} elevation tiles at L{p.ElevationLevel} is more than {MaxTiles} - a box " +
              "this big is hours of fetching; pass --force to do it anyway, or warm it in parts"
            : null;

    public static string PlanLine(Plan p)
        => string.Create(CultureInfo.InvariantCulture,
            $"WARM-UP PLAN for S {p.Box.South:F4} W {p.Box.West:F4} N {p.Box.North:F4} E {p.Box.East:F4}: " +
            $"{p.ElevationCentres.Count} elevation tile(s) at L{p.ElevationLevel} (coarser levels fetched by the cascade " +
            $"where L{p.ElevationLevel} is absent), {p.CoverCentres.Count} land-cover tile(s) over " +
            $"{TileMath.LandCoverSources.Length} source(s), {p.OsmTiles.Count} OSM z{OsmTileMath.Zoom} tile(s) over " +
            $"{Enum.GetValues<OsmSet>().Length} set(s).");

    /// <summary>Fetch every planned tile through the given TileSource. Returns the per-kind counts the caller prints.
    /// Absent tiles are a property of the ground (no data there) and are not failures.</summary>
    public static (int Elev, int Cover, int Osm, int OsmUnknown) Run(Plan p, TileSource ts, Action<string> progress = null)
    {
        int e = 0, c = 0, o = 0, unknown = 0, done = 0;
        int total = p.ElevationCentres.Count + p.CoverCentres.Count + p.OsmTiles.Count;
        void Tick() { done++; if (progress != null && done % 250 == 0) progress($"... {done} of {total} tile request(s)"); }
        foreach (var (lat, lon) in p.ElevationCentres) { ts.Elevation(lat, lon, out _, out _); e++; Tick(); }
        foreach (var (ds, level, _, lat, lon) in p.CoverCentres) { ts.LandCoverClass(ds, level, lat, lon); c++; Tick(); }
        foreach (var (set, x, y) in p.OsmTiles) { var t = ts.GetOsmTile(set, x, y); o++; if (!t.Known) unknown++; Tick(); }
        return (e, c, o, unknown);
    }
}
