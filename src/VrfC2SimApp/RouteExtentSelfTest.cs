using System;
using System.Collections.Generic;
using System.Globalization;
using V = VrfC2SimApp.RouteExtentPolicy;

namespace VrfC2SimApp;

/// <summary>
/// STP-833 offline suite (--routeextent-selftest). No bridge, no network, no clock.
///
/// THE FIXTURES ARE THE REAL RUNS, not invented geometry:
///   V6f  data/PROBE_V6F_PLATOON_Order.xml on data/R9_Mojave_Lean_Initialization.xml -
///        taskee 1222.MechPlt at 34.612955587412,-116.600486942341, waypoints
///        58.70295558741188,16.50922870030753 and 58.70295558741188,16.51922870030753.
///        8,768.9 km away; the run allocated the back end from 3 GB to 16 GB at 780 MB/min,
///        lost it 112 s after dispatch and moved 0.0 m.
///   V6g  data/PROBE_V6G_MOJAVE_Order.xml on the SAME init - the same three tasks and taskees
///        on Mojave vertices; flat at 13.4 MB/min, backends=1 on 68/68 samples, all three
///        completed. Those three routes are the POSITIVE control: the gate must pass every one.
///   COA-STP1  the project's reference order (42 tasks, 128 units): its worst vertex-from-taskee
///        and its worst leg are BOTH 45.384 km (task T4_Consolidate...AlongPlBlue leg 0),
///        measured over data/COA-STP1_Order.xml + data/COA-STP1_Initialization.xml. Pinned here
///        as a literal so the suite needs no data file - and so a future tightening of the
///        defaults that would refuse the project's own reference order fails HERE.
/// </summary>
public static class RouteExtentSelfTest
{
    // --- V6f (SWEDEN waypoints on a MOJAVE taskee) -------------------------------------------
    private const double TaskeeLat = 34.612955587412;
    private const double TaskeeLon = -116.600486942341;
    private static readonly (double Lat, double Lon)[] SwedenRoute =
    {
        (TaskeeLat, TaskeeLon),                                   // vertex 0 = the taskee's live position
        (58.70295558741188, 16.50922870030753),                   // vertex 1
        (58.70295558741188, 16.51922870030753),                   // vertex 2
    };

    // --- V6g (the SAME three tasks, on Mojave ground) ----------------------------------------
    private static readonly (string Name, (double Lat, double Lon)[] Route)[] MojaveRoutes =
    {
        ("T_R5_PL1 1222.MechPlt", new[]
        {
            (34.612955587412, -116.600486942341),
            (34.612955587412, -116.594173672085),
            (34.612955587412, -116.600487000000),
        }),
        ("T_R5_CO1 114.MechCoy", new[]
        {
            (34.647628996814, -116.693387536163),
            (34.652628996814, -116.693387536163),
            (34.657628996814, -116.693387536163),
        }),
        ("T_R5_TK1 1.BdeHQ", new[]
        {
            (34.608415817915, -116.712685404877),
            (34.608415817915, -116.706372134621),
            (34.608415817915, -116.700058864366),
        }),
    };

    // The shipped defaults (appsettings.json / VrfSettings).
    private const double DefaultVertexKm = 100.0;
    private const double DefaultLegKm = 50.0;

    // The exact sentence the Sweden case must be refused with.
    private const string SwedenReason =
        "route vertex 1 at 58.70296,16.50923 is 8768.9 km from the taskee at " +
        "34.61296,-116.60049 (bound 100 km)";

    public static int Run()
    {
        int fails = 0;
        void Check(string what, bool cond)
        {
            Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what);
            if (!cond) fails++;
        }
        static string F(double v, int d) => v.ToString("F" + d, CultureInfo.InvariantCulture);

        Console.WriteLine("--- 1. THE METRIC: great-circle, because the approximation is wrong at this scale");
        double gcSweden = V.GreatCircleMeters(TaskeeLat, TaskeeLon, SwedenRoute[1].Lat, SwedenRoute[1].Lon);
        Check($"V6f taskee -> Swedish vertex 1 is {F(gcSweden / 1000.0, 3)} km by haversine (expected 8768.884)",
              Math.Abs(gcSweden / 1000.0 - 8768.884) < 0.01);
        double eqSweden = TerrainVertexAuthoring.DistMeters(TaskeeLat, TaskeeLon,
                                                            SwedenRoute[1].Lat, SwedenRoute[1].Lon);
        Check($"... and TerrainVertexAuthoring's equirectangular form reads {F(eqSweden / 1000.0, 0)} km - " +
              "1,737 km out, which is why this policy carries its own haversine",
              eqSweden / 1000.0 > 10000.0 && Math.Abs(eqSweden - gcSweden) > 1.0e6);
        Check("great-circle of a point to itself is 0 m",
              V.GreatCircleMeters(TaskeeLat, TaskeeLon, TaskeeLat, TaskeeLon) < 1e-6);

        Console.WriteLine("--- 2. FAIL-FIRST: with Vrf:RouteExtentCheck OFF the Sweden case is DISPATCHED");
        var off = V.Check(false, TaskeeLat, TaskeeLon, SwedenRoute, DefaultVertexKm, DefaultLegKm, null);
        Check("check disabled -> no verdict, the V6f order goes to the back end exactly as it did on 09-15 " +
              "(this arm IS the defect)", !off.Violated && off.Kind == V.Violation.None);

        Console.WriteLine("--- 3. ENABLED: the V6f Sweden case is REFUSED, with the exact reason");
        var on = V.Check(true, TaskeeLat, TaskeeLon, SwedenRoute, DefaultVertexKm, DefaultLegKm, null);
        Check("refused", on.Violated);
        Check("the violation is (a) vertex-from-taskee, not (b) leg-length - both are broken here and " +
              "(a) is the one that names the earliest point the order stopped making sense",
              on.Kind == V.Violation.VertexFromTaskee);
        Check("it names vertex 1 (vertex 0 is the taskee's own position)", on.Index == 1);
        Console.WriteLine("        reason: " + on.Reason);
        Check("reason is verbatim: \"" + SwedenReason + "\"",
              string.Equals(on.Reason, SwedenReason, StringComparison.Ordinal));
        Check($"the measured distance is carried on the verdict ({F(on.Meters / 1000.0, 3)} km) against a " +
              $"{F(on.BoundMeters / 1000.0, 0)} km bound",
              Math.Abs(on.Meters - gcSweden) < 1e-9 && Math.Abs(on.BoundMeters - 100000.0) < 1e-9);
        string marking = V.RefusalMarking("1222.MechPlt", "T_R5_PL1", on);
        Check("the ObservationReport marking names the task, the unit and the reason",
              marking.Contains("T_R5_PL1", StringComparison.Ordinal)
              && marking.Contains("1222.MechPlt", StringComparison.Ordinal)
              && marking.Contains(SwedenReason, StringComparison.Ordinal)
              && marking.Contains("refused, not dispatched", StringComparison.Ordinal));
        Check("the TASKABRT prefix says which two knobs decide it",
              V.MalformedGeometryRefusal.Contains("Vrf:MaxVertexFromTaskeeKm", StringComparison.Ordinal)
              && V.MalformedGeometryRefusal.Contains("Vrf:MaxRouteLegKm", StringComparison.Ordinal)
              && V.MalformedGeometryRefusal.StartsWith("MALFORMED:", StringComparison.Ordinal));

        Console.WriteLine("--- 4. THE V6g POSITIVE CONTROL: every Mojave route PASSES at the defaults");
        foreach (var (name, route) in MojaveRoutes)
        {
            var v = V.Check(true, route[0].Lat, route[0].Lon, route, DefaultVertexKm, DefaultLegKm, null);
            double far = 0.0, longest = 0.0;
            for (int i = 0; i < route.Length; i++)
                far = Math.Max(far, V.GreatCircleMeters(route[0].Lat, route[0].Lon, route[i].Lat, route[i].Lon));
            for (int i = 0; i + 1 < route.Length; i++)
                longest = Math.Max(longest, V.GreatCircleMeters(route[i].Lat, route[i].Lon,
                                                               route[i + 1].Lat, route[i + 1].Lon));
            Check($"{name}: farthest vertex {F(far, 1)} m, longest leg {F(longest, 1)} m -> dispatched" +
                  (v.Violated ? " BUT WAS REFUSED: " + v.Reason : ""), !v.Violated);
        }
        double bdeLen = V.PathLengthMeters(MojaveRoutes[2].Route);
        Check($"PathLengthMeters over 1.BdeHQ's route is {F(bdeLen, 1)} m - the run's own figure was " +
              "\"1,155 m of its 1,156 m route\" (V6_LIVE_JOIN_GATE sec 13)",
              Math.Abs(bdeLen - 1155.6) < 1.0);

        Console.WriteLine("--- 5. THE BOUNDS ARE INCLUSIVE, AND THEY BITE ONE METRE PAST THEMSELVES");
        (double Lat, double Lon)[] far2 = { (TaskeeLat, TaskeeLon), (35.512955587412, -116.600486942341) };  // ~100 km north
        double d2 = V.GreatCircleMeters(far2[0].Lat, far2[0].Lon, far2[1].Lat, far2[1].Lon);
        // AT the bound means the bound rounded UP by one micrometre: (d/1000)*1000 is not bit-for-bit
        // d, and a test whose verdict turns on the last ulp tests the FPU, not the policy.
        double atBoundKm = d2 / 1000.0 + 1e-9;
        Check($"a vertex EXACTLY at the bound ({F(d2 / 1000.0, 3)} km, to the micrometre) is allowed - " +
              "the rule is 'may not EXCEED'",
              !V.Check(true, TaskeeLat, TaskeeLon, far2, atBoundKm, 1e9, null).Violated);
        var justOver = V.Check(true, TaskeeLat, TaskeeLon, far2, (d2 - 1.0) / 1000.0, 1e9, null);
        Check("one metre tighter and the same vertex is refused", justOver.Violated
              && justOver.Kind == V.Violation.VertexFromTaskee && justOver.Index == 1);
        Check($"a leg EXACTLY at the leg bound ({F(d2 / 1000.0, 3)} km) is allowed",
              !V.Check(true, TaskeeLat, TaskeeLon, far2, 1e9, atBoundKm, null).Violated);
        var legOver = V.Check(true, TaskeeLat, TaskeeLon, far2, 1e9, (d2 - 1.0) / 1000.0, null);
        Check("one metre tighter and the same LEG is refused, named as leg 0 - the taskee -> first vertex",
              legOver.Violated && legOver.Kind == V.Violation.LegLength && legOver.Index == 0
              && legOver.Reason.Contains("route leg 0 (the taskee -> vertex 1)", StringComparison.Ordinal));
        Console.WriteLine("        reason: " + legOver.Reason);

        Console.WriteLine("--- 6. LEG 0 IS THE TASKEE'S OWN LEG: a near start with a far first vertex");
        // (a) passes at 100 km, (b) must still catch the 60 km jump off the taskee's position.
        (double Lat, double Lon)[] jump = { (TaskeeLat, TaskeeLon), (35.152955587412, -116.600486942341) };   // ~60 km
        Check("60 km from the taskee passes (a) at the 100 km default",
              !V.Check(true, TaskeeLat, TaskeeLon, jump, DefaultVertexKm, 1e9, null).Violated);
        var jumpLeg = V.Check(true, TaskeeLat, TaskeeLon, jump, DefaultVertexKm, DefaultLegKm, null);
        Check("... and is refused by (b) at the 50 km leg default", jumpLeg.Violated
              && jumpLeg.Kind == V.Violation.LegLength && jumpLeg.Index == 0);

        Console.WriteLine("--- 7. RULE (c): no extent means SKIPPED, never guessed");
        Check("KnownExtent() is null on this build - there is no remote-controller nav/terrain query",
              V.KnownExtent() == null);
        Check("the skip is SAID, and says why", V.ExtentUnavailableNote.Contains("SKIPPED", StringComparison.Ordinal)
              && V.ExtentUnavailableNote.Contains("NavAreaEvidence", StringComparison.Ordinal));
        var mojaveBox = new V.Extent(34.40, 34.90, -116.90, -116.40);
        Check("with NO extent the Sweden route is judged by (a) alone and still refused",
              V.Check(true, TaskeeLat, TaskeeLon, SwedenRoute, DefaultVertexKm, DefaultLegKm, null).Violated);
        // (a) and (b) disarmed, so only (c) can speak.
        var outside = V.Check(true, TaskeeLat, TaskeeLon, SwedenRoute, 0, 0, mojaveBox);
        Check("with (a)/(b) off and a Mojave extent supplied, (c) refuses the Swedish vertex",
              outside.Violated && outside.Kind == V.Violation.OutsideExtent && outside.Index == 1);
        Console.WriteLine("        reason: " + outside.Reason);
        foreach (var (name, route) in MojaveRoutes)
            Check($"{name} lies INSIDE that extent",
                  !V.Check(true, route[0].Lat, route[0].Lon, route, 0, 0, mojaveBox).Violated);

        Console.WriteLine("--- 8. THE PROJECT'S OWN REFERENCE ORDER MUST NOT BE REFUSED (COA-STP1)");
        // Measured over data/COA-STP1_Order.xml + data/COA-STP1_Initialization.xml on 2026-09-15:
        // 42 tasks, 128 positioned units; the WORST vertex-from-taskee and the WORST leg are the
        // same 45.384 km (T4_ConsolidateAndPrepareDefensivePositionsAlongPlBlue, leg 0).
        const double CoaWorstKm = 45.384;
        (double Lat, double Lon)[] coa = { (TaskeeLat, TaskeeLon), (35.021, -116.600486942341) };
        double coaD = V.GreatCircleMeters(coa[0].Lat, coa[0].Lon, coa[1].Lat, coa[1].Lon);
        Check($"the synthetic stand-in leg is {F(coaD / 1000.0, 3)} km, i.e. COA-STP1's worst " +
              $"({F(CoaWorstKm, 3)} km) to within 100 m", Math.Abs(coaD / 1000.0 - CoaWorstKm) < 0.1);
        Check("COA-STP1's worst leg is DISPATCHED at the shipped defaults",
              !V.Check(true, coa[0].Lat, coa[0].Lon, coa, DefaultVertexKm, DefaultLegKm, null).Violated);
        Check($"HEADROOM, RECORDED: the leg default leaves only {F(DefaultLegKm - CoaWorstKm, 3)} km " +
              "over the reference order's worst leg - a tighter default would refuse a legitimate move",
              DefaultLegKm - CoaWorstKm > 0 && DefaultLegKm - CoaWorstKm < 10.0);
        Check("a default of 45 km WOULD refuse it (so this guard has teeth)",
              V.Check(true, coa[0].Lat, coa[0].Lon, coa, DefaultVertexKm, 45.0, null).Violated);

        Console.WriteLine("--- 9. DEGENERATE INPUT NEVER REFUSES");
        Check("a zero bound turns its rule OFF",
              !V.Check(true, TaskeeLat, TaskeeLon, SwedenRoute, 0, 0, null).Violated);
        Check("a negative bound turns its rule OFF",
              !V.Check(true, TaskeeLat, TaskeeLon, SwedenRoute, -1, -1, null).Violated);
        Check("an empty vertex list is Ok (the zero-geometry case is Q4's, not this one)",
              !V.Check(true, TaskeeLat, TaskeeLon, Array.Empty<(double, double)>(),
                       DefaultVertexKm, DefaultLegKm, null).Violated);
        Check("a null vertex list is Ok",
              !V.Check(true, TaskeeLat, TaskeeLon, null, DefaultVertexKm, DefaultLegKm, null).Violated);
        Check("a single vertex (the taskee alone) has no leg and no distance to itself",
              !V.Check(true, TaskeeLat, TaskeeLon, new[] { (TaskeeLat, TaskeeLon) },
                       DefaultVertexKm, DefaultLegKm, null).Violated);
        Check("PathLengthMeters of 0 or 1 vertex is 0 m",
              V.PathLengthMeters(Array.Empty<(double, double)>()) == 0.0
              && V.PathLengthMeters(new[] { (TaskeeLat, TaskeeLon) }) == 0.0);

        Console.WriteLine("--- 10. EVERY ORDER VERTEX IS JUDGED, NOT ONLY THE LAST");
        // The V6f route's SECOND Swedish vertex is further still; a check that looked only at the
        // destination would also have caught V6f, but not an order whose MIDDLE vertex is the
        // impossible one. This is that order.
        var midBad = new List<(double Lat, double Lon)>
        {
            (TaskeeLat, TaskeeLon),
            (58.70295558741188, 16.50922870030753),
            (34.612955587412, -116.594173672085),
        };
        var mid = V.Check(true, TaskeeLat, TaskeeLon, midBad, DefaultVertexKm, DefaultLegKm, null);
        Check("a route that starts and ENDS on the Mojave but detours to Sweden in the middle is refused, " +
              "naming vertex 1", mid.Violated && mid.Index == 1);

        Console.WriteLine(fails == 0 ? "routeextent-selftest: ALL CHECKS PASSED"
                                     : $"routeextent-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
