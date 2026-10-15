using System.Globalization;

namespace VrfC2SimApp.Preflight;

/// <summary>
/// STP-806 / STP-803 (AO intake): THE AREA OF OPERATIONS OF AN INITIALIZATION - the box that holds every unit and
/// every tactical graphic it carries, widened by a margin. The input every preparation step keys on: the tile warm-up
/// (STP-805, --warm-preflight-cache), the navigation-data generation box (STP-803) and the fixture extent (STP-804).
/// PURE: it reads a parsed InitData and returns numbers; no file, no network.
/// </summary>
public static class AoExtent
{
    /// <summary>Edges names what sets the south, west, north and east edge of the raw box ("unit X", "area Y"): one
    /// far-away item (a rear airbase, a strategic unit) can make the AO hundreds of km wide, and the operator must see
    /// which one before warming or generating anything for it.</summary>
    public sealed record Result(TileWarmup.Box Box, TileWarmup.Box Raw, double MarginKm, int Units, int UnitsSkipped,
                                int GraphicPoints, (string S, string W, string N, string E) Edges);

    /// <summary>The AO of an initialization: units (their Latitude/Longitude strings, invariant culture; an empty or
    /// unparseable one is SKIPPED and counted, never read as 0,0) plus every vertex of every area, line, point and task
    /// graphic, widened by <paramref name="marginKm"/> on every side. Null when nothing has a position.</summary>
    public static Result FromInit(InitData d, double marginKm)
    {
        var pts = new List<(double Lat, double Lon, string What)>();
        int units = 0, skipped = 0, graphic = 0;
        foreach (var u in d?.Units ?? new List<InitUnit>())
        {
            if (double.TryParse(u.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var la)
                && double.TryParse(u.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lo)
                && la is >= -90 and <= 90 && lo is >= -180 and <= 180)
            { pts.Add((la, lo, "unit " + u.Name)); units++; }
            else skipped++;
        }
        void AddAll(IEnumerable<(double Lat, double Lon, double Elev)> vs, string what)
        { foreach (var v in vs) { pts.Add((v.Lat, v.Lon, what)); graphic++; } }
        foreach (var a in d?.Areas ?? new List<InitArea>()) AddAll(a.Points, "area " + a.Name);
        foreach (var l in d?.Lines ?? new List<InitLine>()) AddAll(l.Points, "line " + l.Name);
        foreach (var p in d?.Points ?? new List<InitPoint>()) AddAll(p.Points, "point " + p.Name);
        foreach (var t in d?.TaskGraphics ?? new List<InitTaskGraphic>()) AddAll(t.Points, "task graphic " + t.Name);
        if (pts.Count == 0) return null;
        var raw = new TileWarmup.Box(pts.Min(p => p.Lat), pts.Min(p => p.Lon), pts.Max(p => p.Lat), pts.Max(p => p.Lon));
        var edges = (pts.MinBy(p => p.Lat).What, pts.MinBy(p => p.Lon).What, pts.MaxBy(p => p.Lat).What,
                     pts.MaxBy(p => p.Lon).What);
        return new Result(Widen(raw, marginKm), raw, marginKm, units, skipped, graphic, edges);
    }

    /// <summary>Widen a box by km on every side (the same arithmetic as RouteExtentPolicy's demo-extent margin:
    /// 111.32 km per degree of latitude, scaled by cos(mid-latitude) for longitude), clamped to the warm-up's limits.</summary>
    public static TileWarmup.Box Widen(TileWarmup.Box b, double km)
    {
        double dLat = km / 111.32;
        double mid = (b.South + b.North) / 2.0 * Math.PI / 180.0;
        double dLon = km / (111.32 * Math.Max(0.01, Math.Cos(mid)));
        return new TileWarmup.Box(Math.Max(-85, b.South - dLat), Math.Max(-180, b.West - dLon),
                                  Math.Min(85, b.North + dLat), Math.Min(180, b.East + dLon));
    }

    public static string BoxText(TileWarmup.Box b)
        => string.Create(CultureInfo.InvariantCulture, $"{b.South:F5},{b.West:F5},{b.North:F5},{b.East:F5}");

    public static double WidthKm(TileWarmup.Box b)
        => (b.East - b.West) * 111.32 * Math.Cos((b.South + b.North) / 2.0 * Math.PI / 180.0);

    public static double HeightKm(TileWarmup.Box b) => (b.North - b.South) * 111.32;

    public static string SummaryLine(Result r)
    {
        var ic = CultureInfo.InvariantCulture;
        return $"AO {BoxText(r.Box)} ({WidthKm(r.Box).ToString("F1", ic)} x {HeightKm(r.Box).ToString("F1", ic)} km) = " +
               $"the extent of {r.Units} unit(s) and {r.GraphicPoints} graphic vertex(es) ({BoxText(r.Raw)}) + " +
               $"{r.MarginKm.ToString("F1", ic)} km margin" +
               (r.UnitsSkipped > 0 ? $"; {r.UnitsSkipped} unit(s) with no readable position SKIPPED" : "") + ". " +
               $"Edges set by: S {r.Edges.S}; W {r.Edges.W}; N {r.Edges.N}; E {r.Edges.E}.";
    }
}
