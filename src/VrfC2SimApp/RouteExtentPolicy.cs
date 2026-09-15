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
