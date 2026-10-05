using System;
using System.Collections.Generic;
using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// STP-833: ROUTE EXTENT AND PLAUSIBILITY. A pure, offline-testable decision
/// (--routeextent-selftest); no bridge, no network, no clock.
///
/// WHY THIS EXISTS - the V6 lane, 2026-09-15 (docs/experiments/V6_LIVE_JOIN_GATE_2026-09-15.md
/// secs 12-13). `R5_UnitMove_Order.xml` and its V6f derivative carry SWEDEN waypoints
/// (58.703 N, 16.509 E) while every run paired them with the MOJAVE init (34.61 N, -116.60 W).
/// The interface authored them without comment - our own log reads "Terrain profile 8 ... all 3
/// vertices authored from terrain", alts [1127.2, 97.2, 95.3] - and the back end duly accepted
/// `ground-vehicle-move-to destination 58.70285 N, 16.50897 E`, 8,768.6 km away. `Calc off road
/// nav path part` then became an unbounded off-road search toward another continent: measured
/// memory growth of 780 MB/min (V6f, one task) to 2,219 MB/min (V6e, three), 3 GB -> 31 GB, the
/// back end silent to every controller 112-121 s after dispatch, and 0.0 m of movement. V6g ran
/// the SAME fixture, init, SMS and composition with MOJAVE vertices and was flat (13.4 MB/min,
/// backends=1 on 68/68 samples, all three tasks completed) - so the defect is the ORDER DATA,
/// and the interface's part in it was dispatching geometry it had every means to recognise as
/// impossible.
///
/// THE RULE. Before anything is sent to the back end, on the same vertices the terrain-profile
/// authoring is about to be handed:
///   (a) every route vertex - and the task's own Location/objective, which is where those
///       vertices come from - must lie within Vrf:MaxVertexFromTaskeeKm of the TASKEE'S CURRENT
///       POSITION;
///   (b) no leg may exceed Vrf:MaxRouteLegKm - the legs are the consecutive vertex pairs, and
///       vertex 0 IS the taskee's live position, so "taskee -> first authored vertex" is leg 0;
///   (c) when a loaded terrain / navigation-area EXTENT is known to the app, every vertex must
///       lie inside it. *** NO SUCH EXTENT IS AVAILABLE TODAY *** - see KnownExtent below - so
///       (c) is SKIPPED, out loud, rather than guessed at.
/// A violation is MALFORMED in exactly Q4's sense (user ruling 2026-09-14): the order is at
/// fault, the interface refuses loudly instead of inventing something, and the task's successors
/// fail fast through the same NotifyAbandoned + TASKABRT path.
///
/// GREAT-CIRCLE, NOT THE EQUIRECTANGULAR APPROXIMATION. TerrainVertexAuthoring.DistMeters is an
/// equirectangular approximation, adequate for the metre-scale thresholds it was written for and
/// WRONG at this scale: on the V6f pair it reads 10,506 km where the haversine reads 8,768.9 km.
/// A refusal has to name a distance an operator can check against a map, so this file carries its
/// own haversine - the same formula and the same R_EARTH the pre-flight tile chain already uses
/// (Preflight/TileSource.DistanceMeters).
/// </summary>
public static class RouteExtentPolicy
{
    /// <summary>The same earth radius Preflight/TileSource and tools/preflight/leg_check.py use.</summary>
    public const double EarthRadiusMeters = 6371000.0;

    /// <summary>Great-circle metres (haversine). Correct at 500 m and at 8,000 km.</summary>
    public static double GreatCircleMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dla = (lat2 - lat1) * Math.PI / 180.0;
        double dlo = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Pow(Math.Sin(dla / 2.0), 2)
                 + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                 * Math.Pow(Math.Sin(dlo / 2.0), 2);
        return 2.0 * EarthRadiusMeters * Math.Asin(Math.Sqrt(Math.Min(1.0, a)));
    }

    /// <summary>The summed great-circle length of a vertex list (0 or 1 vertex = 0 m).</summary>
    public static double PathLengthMeters(IReadOnlyList<(double Lat, double Lon)> vertices)
    {
        if (vertices == null || vertices.Count < 2) return 0.0;
        double total = 0.0;
        for (int i = 0; i + 1 < vertices.Count; i++)
            total += GreatCircleMeters(vertices[i].Lat, vertices[i].Lon,
                                       vertices[i + 1].Lat, vertices[i + 1].Lon);
        return total;
    }

    /// <summary>A rectangular lat/lon extent. No antimeridian wrap - every terrain this project
    /// has ever loaded is far from it, and a wrap-aware box nobody can test would be worse than
    /// none.</summary>
    public readonly record struct Extent(double MinLat, double MaxLat, double MinLon, double MaxLon)
    {
        public bool Contains(double lat, double lon)
            => lat >= MinLat && lat <= MaxLat && lon >= MinLon && lon <= MaxLon;
    }

    /// <summary>
    /// CHECK (c)'S INPUT, AND WHY IT IS ALWAYS null TODAY.
    ///
    /// There is no in-band source for the loaded terrain's extent:
    ///   - NavAreaEvidence.cs states the finding in full: "There is NO nav-area query a remote
    ///     controller can issue" - vrfcontrol/vrfRemoteController.h has no navigation accessor,
    ///     navigationAreasManager.h lives in vrfGuiCore (the GUI library this process does not
    ///     link), and vrfNavigation/navArea.h is the back-end/generator side. What the console
    ///     rows give is the NAME of an acquired area, never its boundary.
    ///   - The terrain-profile reply carries elevations, not coverage: on the V6f run it answered
    ///     for the Swedish vertices with 97.2 m and 95.3 m and the authoring called all three
    ///     vertices "authored from terrain". An off-database answer is indistinguishable from an
    ///     on-database one there, so it cannot serve as an extent either.
    /// So (c) is not implemented against a guess. The method exists as the single, named seam a
    /// future extent source plugs into, and the caller says out loud (once per run) that (c) was
    /// skipped - an unchecked rule that looks checked is the failure this project keeps meeting.
    /// </summary>
    public static Extent? KnownExtent() => null;

    /// <summary>The one line the log and the ObservationReport both say when (c) cannot run.</summary>
    public const string ExtentUnavailableNote =
        "ROUTE EXTENT CHECK (STP-833): rules (a) distance-from-taskee and (b) leg-length are armed; " +
        "rule (c) inside-the-loaded-terrain is SKIPPED for this run because no terrain or " +
        "navigation-area extent is readable from a VR-Forces remote controller (NavAreaEvidence: " +
        "no nav-area query exists, and the terrain-profile reply answers with elevations for " +
        "off-database points too - V6f authored two Swedish vertices at 97.2 m and 95.3 m).";

    public enum Violation
    {
        None = 0,
        /// <summary>(a) a vertex is further from the taskee than Vrf:MaxVertexFromTaskeeKm.</summary>
        VertexFromTaskee,
        /// <summary>(b) a leg is longer than Vrf:MaxRouteLegKm.</summary>
        LegLength,
        /// <summary>(c) a vertex is outside the loaded terrain extent.</summary>
        OutsideExtent,
    }

    /// <summary>
    /// The verdict for ONE task's geometry. Index/Lat/Lon name the offending vertex (for a leg,
    /// its FIRST vertex), Meters is the measured quantity and BoundMeters the bound it broke.
    /// Reason is the sentence STP and the log both get - it is locked by --routeextent-selftest
    /// because it is what an operator has to fix the order from.
    /// </summary>
    public readonly record struct Verdict(Violation Kind, int Index, double Lat, double Lon,
                                          double Meters, double BoundMeters, string Reason)
    {
        public bool Violated => Kind != Violation.None;
        public static Verdict Ok => new(Violation.None, -1, double.NaN, double.NaN,
                                        double.NaN, double.NaN, "");
    }

    /// <summary>The exact sentence a geometry refusal is prefixed with. Locked by
    /// --routeextent-selftest for the same reason TaskDispatchPolicy.MalformedZeroGeometryRefusal
    /// is: it is what STP will be told, and a refusal has to say what is wrong with the order or
    /// nobody can fix it.</summary>
    public const string MalformedGeometryRefusal =
        "MALFORMED: this task's geometry is not plausible ground for its taskee - a route vertex " +
        "or a leg breaks the configured extent bounds (Vrf:MaxVertexFromTaskeeKm / " +
        "Vrf:MaxRouteLegKm), so no VR-Forces task is issued and nothing is sent to the back end";

    /// <summary>
    /// THE CHECK. <paramref name="enabled"/> is Vrf:RouteExtentCheck, passed IN rather than read
    /// from the settings, so the OFF arm is exercised by the selftest on the same call the service
    /// makes (a feature flag tested only by reading the flag is not tested). The service also
    /// short-circuits on the same setting, so a disabled check costs nothing per dispatch.
    ///
    /// vertices[0] MUST BE THE TASKEE'S LIVE POSITION - that is how VrfC2SimService builds
    /// routeGeo - so (a) is trivially satisfied for vertex 0 and leg 0 is "taskee -> first
    /// authored vertex", which is exactly what (b) is asked to cover.
    /// </summary>
    public static Verdict Check(bool enabled, double taskeeLat, double taskeeLon,
                                IReadOnlyList<(double Lat, double Lon)> vertices,
                                double maxVertexFromTaskeeKm, double maxLegKm, Extent? extent)
    {
        if (!enabled || vertices == null || vertices.Count == 0) return Verdict.Ok;

        // (a) EVERY VERTEX WITHIN REACH OF THE TASKEE. First (lowest-index) offender wins, so the
        // message names the earliest point at which the order stopped making sense.
        if (maxVertexFromTaskeeKm > 0)
        {
            double bound = maxVertexFromTaskeeKm * 1000.0;
            for (int i = 0; i < vertices.Count; i++)
            {
                double d = GreatCircleMeters(taskeeLat, taskeeLon, vertices[i].Lat, vertices[i].Lon);
                if (d <= bound) continue;
                return new Verdict(Violation.VertexFromTaskee, i, vertices[i].Lat, vertices[i].Lon,
                                   d, bound,
                                   $"route vertex {i} at {LL(vertices[i].Lat)},{LL(vertices[i].Lon)} is " +
                                   $"{Km1(d)} km from the taskee at {LL(taskeeLat)},{LL(taskeeLon)} " +
                                   $"(bound {KmB(bound)} km)");
            }
        }

        // (b) NO LEG LONGER THAN THE BOUND. Leg i runs vertex i -> vertex i+1; leg 0 is the
        // taskee's own position -> the first authored vertex.
        if (maxLegKm > 0)
        {
            double bound = maxLegKm * 1000.0;
            for (int i = 0; i + 1 < vertices.Count; i++)
            {
                double d = GreatCircleMeters(vertices[i].Lat, vertices[i].Lon,
                                             vertices[i + 1].Lat, vertices[i + 1].Lon);
                if (d <= bound) continue;
                return new Verdict(Violation.LegLength, i, vertices[i].Lat, vertices[i].Lon,
                                   d, bound,
                                   $"route leg {i} ({(i == 0 ? "the taskee" : "vertex " + i)} -> vertex " +
                                   $"{i + 1}) from {LL(vertices[i].Lat)},{LL(vertices[i].Lon)} to " +
                                   $"{LL(vertices[i + 1].Lat)},{LL(vertices[i + 1].Lon)} is {Km1(d)} km long " +
                                   $"(bound {KmB(bound)} km)");
            }
        }

        // (c) INSIDE THE LOADED TERRAIN. Skipped when no extent is known (KnownExtent).
        if (extent is Extent box)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                if (box.Contains(vertices[i].Lat, vertices[i].Lon)) continue;
                return new Verdict(Violation.OutsideExtent, i, vertices[i].Lat, vertices[i].Lon,
                                   double.NaN, double.NaN,
                                   $"route vertex {i} at {LL(vertices[i].Lat)},{LL(vertices[i].Lon)} lies " +
                                   $"OUTSIDE the loaded terrain extent (lat {LL(box.MinLat)}..{LL(box.MaxLat)}, " +
                                   $"lon {LL(box.MinLon)}..{LL(box.MaxLon)})");
            }
        }

        return Verdict.Ok;
    }

    /// <summary>The line an operator reads when the gate refuses a task, and the Marking of the
    /// ObservationReport that carries it to the C2 side.</summary>
    public static string RefusalMarking(string unitName, string taskName, Verdict v)
        => $"ROUTE EXTENT REFUSAL (STP-833, Vrf:RouteExtentCheck): task {taskName} " +
           $"({(string.IsNullOrEmpty(unitName) ? "unit" : unitName)}) - {v.Reason} - refused, not " +
           "dispatched. FIX THE ORDER: the geometry is not on the ground this taskee is standing " +
           "on (V6f drove a Mojave platoon at Swedish waypoints and the back end allocated itself " +
           "to a standstill). Raise Vrf:MaxVertexFromTaskeeKm / Vrf:MaxRouteLegKm only if the " +
           "move really is that long.";

    // ============= RL-20261005-02: THE DEMO TERRAIN EXTENT (Vrf:DemoExtent) =====================
    // WHY. The Way B rehearsal (docs/experiments/REHEARSAL_WAYB_2026-10-05.md, Result + ADDENDUM
    // 2026-10-05) ran STP's raw Iron Storm export, whose routes reach about 24.4 E; the back end's
    // terrain feature paging and planner obstacle-area collection fell minutes behind and it stopped
    // publishing 4.8 min after the order. The owner's ruling RL-20261005-02 (option (a)): the
    // interface bounds the demo by a TERRAIN EXTENT SETTING - a task whose geometry leaves it is
    // REPORTED out of area (TASKABRT with the reason), not executed; STP's export is unchanged.
    //
    // HOW IT RELATES TO RULE (c) ABOVE. Rule (c) is "inside the LOADED terrain", which no remote
    // controller can read (KnownExtent stays null, and that statement stays true). The demo extent
    // is NOT that: it is an OPERATOR'S BOUND, configured, never inferred. It reuses rule (c)'s box
    // (Extent), its verdict (Violation.OutsideExtent) and the same refusal path (NotifyAbandoned +
    // one TASKABRT through the single emit point), but it runs EARLIER - at ORDER RECEIPT, before
    // any member is populated and before any task is orchestrated - because the cost it exists to
    // avoid (terrain paging around a performer) starts at population, not at dispatch.
    //
    // WHAT IS TESTED, per task: the performer's AUTHORED start position (the init coordinate, the
    // one its members are placed around), every point the interface would drive (the resolved
    // route vertices / point / area centroid - TaskGeometryResolver, the same call dispatch makes),
    // and every vertex of every AREA graphic the task names (the objective ring itself, not only its
    // centroid). An embedded Location that a MapGraphicID overrides is NOT tested: nothing drives it.

    /// <summary>The setting and the ruling, as every demo-extent line names them.</summary>
    public const string DemoExtentRuling = "Vrf:DemoExtent, RL-20261005-02";

    /// <summary>The TASKABRT prefix (locked by --routeextent-selftest).</summary>
    public const string OutOfDemoExtent = "OUT OF DEMO EXTENT";

    /// <summary>The configured demo extent: the authored box, its margin, and the box widened by
    /// that margin, which is what is tested.</summary>
    public readonly record struct DemoBound(Extent Authored, double MarginKm, Extent Bounded)
    {
        /// <summary>"the demo extent S 53.93972 W 23.10848 N 54.11939 E 23.41436 + 2 km margin".</summary>
        public string Describe()
            => $"the demo extent S {LL(Authored.MinLat)} W {LL(Authored.MinLon)} N {LL(Authored.MaxLat)} " +
               $"E {LL(Authored.MaxLon)}" + (MarginKm > 0 ? $" + {KmB(MarginKm * 1000.0)} km margin" : "");
    }

    /// <summary>
    /// Parse Vrf:DemoExtent ("south,west,north,east", decimal degrees, invariant culture) and
    /// Vrf:DemoExtentMarginKm. Empty or whitespace = OFF: returns true with <paramref name="bound"/>
    /// null. Anything else that is not four finite numbers with south &lt; north, west &lt; east,
    /// latitudes in [-90, 90] and longitudes in [-180, 180], or a negative / non-finite margin,
    /// returns false with the reason - the caller logs it as an ERROR and runs with the bound OFF.
    /// </summary>
    public static bool TryParseDemoExtent(string text, double marginKm, out DemoBound? bound, out string error)
    {
        bound = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        var parts = text.Split(',');
        if (parts.Length != 4)
        {
            error = $"Vrf:DemoExtent='{text}' must be four numbers 'south,west,north,east' (got {parts.Length} field(s))";
            return false;
        }
        var v = new double[4];
        for (int i = 0; i < 4; i++)
        {
            if (!double.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v[i])
                || double.IsNaN(v[i]) || double.IsInfinity(v[i]))
            {
                error = $"Vrf:DemoExtent='{text}': field {i + 1} ('{parts[i].Trim()}') is not a finite number";
                return false;
            }
        }
        double s = v[0], w = v[1], n = v[2], e = v[3];
        if (s < -90 || n > 90 || w < -180 || e > 180 || !(s < n) || !(w < e))
        {
            error = $"Vrf:DemoExtent='{text}' is not a box: it needs -90 <= south < north <= 90 and " +
                    "-180 <= west < east <= 180 (no antimeridian wrap)";
            return false;
        }
        if (double.IsNaN(marginKm) || double.IsInfinity(marginKm) || marginKm < 0)
        {
            error = $"Vrf:DemoExtentMarginKm={marginKm.ToString(CultureInfo.InvariantCulture)} must be a finite number >= 0";
            return false;
        }
        var authored = new Extent(s, n, w, e);
        bound = new DemoBound(authored, marginKm, Widen(authored, marginKm * 1000.0));
        return true;
    }

    /// <summary>The box widened by <paramref name="marginMeters"/> on every side. The longitude
    /// widening is computed at the box's edge FURTHEST from the equator (after the latitude
    /// widening), where a degree of longitude is shortest - so the margin is at least the stated
    /// distance everywhere along the east and west edges, never less.</summary>
    public static Extent Widen(Extent e, double marginMeters)
    {
        if (!(marginMeters > 0)) return e;
        double degLat = marginMeters / (EarthRadiusMeters * Math.PI / 180.0);
        double worstLat = Math.Min(89.0, Math.Max(Math.Abs(e.MinLat), Math.Abs(e.MaxLat)) + degLat);
        double degLon = degLat / Math.Cos(worstLat * Math.PI / 180.0);
        return new Extent(Math.Max(-90.0, e.MinLat - degLat), Math.Min(90.0, e.MaxLat + degLat),
                          e.MinLon - degLon, e.MaxLon + degLon);
    }

    /// <summary>Great-circle metres from a point to the nearest point of the box (0 inside or on
    /// the edge). The nearest point is the point clamped into the box - exact due north/south/east/
    /// west of an edge, and within a fraction of a percent at a corner at this project's scale.</summary>
    public static double MetersOutside(Extent box, double lat, double lon)
    {
        if (box.Contains(lat, lon)) return 0.0;
        double cLat = Math.Min(box.MaxLat, Math.Max(box.MinLat, lat));
        double cLon = Math.Min(box.MaxLon, Math.Max(box.MinLon, lon));
        return GreatCircleMeters(lat, lon, cLat, cLon);
    }

    /// <summary>One point the demo-extent check tests, with what it IS (the refusal names it).</summary>
    public readonly record struct ExtentPoint(string What, double Lat, double Lon);

    /// <summary>
    /// The points the demo extent tests for ONE task, in the order the refusal reports them: the
    /// performer's start first, then the driven geometry in route order, then the area rings. PURE:
    /// the same TaskGeometryResolver.Resolve call ExecuteTaskOnTick makes (its log lines discarded -
    /// dispatch logs them), so the order-receipt check judges exactly what dispatch would drive.
    /// </summary>
    public static List<ExtentPoint> DemoExtentPoints(OrderTask task, IReadOnlyDictionary<string, TaskGraphic> graphics,
                                                     (double Lat, double Lon)? performerStart)
    {
        var outp = new List<ExtentPoint>();
        if (performerStart is { } ps)
            outp.Add(new ExtentPoint("the performer's start position", ps.Lat, ps.Lon));
        if (task == null) return outp;
        var geometry = TaskGeometryResolver.Resolve(task, graphics, performerStart);
        var reading = TaskGeometryInterpretation.Interpret(task.ActionCode, geometry.Points, geometry.Source);
        int n = geometry.Points.Count;
        string kind = reading.Kind switch
        {
            GeometryKind.ObjectiveArea => "objective area vertex",
            GeometryKind.Point => "task point",
            _ => "route vertex",
        };
        for (int i = 0; i < n; i++)
            outp.Add(new ExtentPoint(n == 1 ? (reading.Kind == GeometryKind.Route ? "route point" : "task point")
                                            : $"{kind} {i + 1} of {n}",
                                     geometry.Points[i].Lat, geometry.Points[i].Lon));
        // The rings of the AREA graphics the task names. The resolver reduces a MapGraphicID area to
        // its centroid (the destination); the ring is still the task's ground.
        if (geometry.Source == GeometrySource.MapGraphic && graphics != null)
            foreach (var id in task.MapGraphicUuids ?? Array.Empty<string>())
                if (graphics.TryGetValue(id, out var g) && g.Points is { Count: > 0 }
                    && string.Equals(g.Kind, TaskGraphic.KindArea, StringComparison.OrdinalIgnoreCase))
                    for (int j = 0; j < g.Points.Count; j++)
                        outp.Add(new ExtentPoint($"area '{g.Name}' vertex {j + 1} of {g.Points.Count}",
                                                 g.Points[j].Lat, g.Points[j].Lon));
        return outp;
    }

    /// <summary>
    /// THE DEMO-EXTENT VERDICT for one task. <paramref name="bound"/> null = OFF = Ok (the OFF arm
    /// is the same call). The FIRST point outside wins - the performer's start when it is outside,
    /// which is also why that performer is not populated - and the reason adds how many of the
    /// task's points are outside and how far the farthest is. Meters = the named point's distance
    /// outside the widened box; BoundMeters = the margin.
    /// </summary>
    public static Verdict CheckDemoExtent(DemoBound? bound, IReadOnlyList<ExtentPoint> points)
    {
        if (bound is not DemoBound b || points == null || points.Count == 0) return Verdict.Ok;
        int first = -1, outside = 0;
        double farthest = 0.0;
        for (int i = 0; i < points.Count; i++)
        {
            double d = MetersOutside(b.Bounded, points[i].Lat, points[i].Lon);
            if (d <= 0.0) continue;
            outside++;
            if (first < 0) first = i;
            farthest = Math.Max(farthest, d);
        }
        if (first < 0) return Verdict.Ok;
        var p = points[first];
        double dm = MetersOutside(b.Bounded, p.Lat, p.Lon);
        string reason = $"{p.What} at {LL(p.Lat)},{LL(p.Lon)} is {Km3(dm)} km outside {b.Describe()}";
        string tally = outside == 1
            ? $"1 of {points.Count} checked point(s) outside"
            : $"{outside} of {points.Count} checked points outside, the farthest {Km3(farthest)} km out";
        return new Verdict(Violation.OutsideExtent, first, p.Lat, p.Lon, dm, b.MarginKm * 1000.0,
                           reason + " - " + tally);
    }

    /// <summary>The TASKABRT text: "OUT OF DEMO EXTENT: &lt;what&gt; at &lt;lat,lon&gt; is &lt;km&gt; km
    /// outside &lt;extent&gt; - &lt;tally&gt; (Vrf:DemoExtent, RL-20261005-02) - task 'X' refused, not executed".</summary>
    public static string DemoExtentAbort(string taskName, Verdict v)
        => $"{OutOfDemoExtent}: {v.Reason} ({DemoExtentRuling}) - task '{taskName}' refused, not executed; " +
           "STP's order is unchanged";

    /// <summary>A distance outside, in km to the metre ("0.001", "41.234").</summary>
    private static string Km3(double meters)
        => (meters / 1000.0).ToString("F3", CultureInfo.InvariantCulture);

    /// <summary>Latitude/longitude as the refusal prints them - 5 decimals, invariant.</summary>
    private static string LL(double v)
        => v.ToString("F5", CultureInfo.InvariantCulture);

    /// <summary>A measured distance in km, one decimal (8768.884 m -> "8768.9").</summary>
    private static string Km1(double meters)
        => (meters / 1000.0).ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>A BOUND in km, printed without trailing zeros ("100", "0.5").</summary>
    private static string KmB(double meters)
        => (meters / 1000.0).ToString("0.###", CultureInfo.InvariantCulture);
}
