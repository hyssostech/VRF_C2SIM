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

        DemoExtentSection(Check);

        Console.WriteLine(fails == 0 ? "routeextent-selftest: ALL CHECKS PASSED"
                                     : $"routeextent-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }

    // ===================== RL-20261005-02: THE DEMO TERRAIN EXTENT ==============================
    // The Demo overlay's value (appsettings.Demo.json; --rulings-selftest (s14) pins the file to it).
    public const string DemoOverlayExtent = "53.939723,23.108483,54.119385,23.414360";
    public const double DemoOverlayMarginKm = 2.0;

    private const string IronInit = "STP-IRON-STORM-SYNTHETIC_Initialization.xml";
    private const string CutAOrder = "IRONSTORM_CUTA_Order.xml";
    private const string FullOrder = "IRONSTORM_FULL_Order.xml";
    private const string RawOrder = "STP-IRON-STORM-SYNTHETIC_Order.xml";

    private sealed record Judged(OrderTask Task, string Unit, V.Verdict Verdict, int Points, bool InInit);

    /// <summary>The order-receipt check exactly as OnOrder runs it: the graphics map built as the
    /// service builds it (init first, then the order's own; the first publisher keeps a uuid), the
    /// performer's AUTHORED init coordinate as its start, and DemoExtentPoints + CheckDemoExtent.</summary>
    private static List<Judged> JudgeOrder(InitData init, OrderData order, V.DemoBound? bound)
    {
        static List<(double, double, double?)> Pts(IEnumerable<(double Lat, double Lon, double Elev)> src)
        {
            var l = new List<(double, double, double?)>();
            foreach (var p in src) l.Add((p.Lat, p.Lon, p.Elev));
            return l;
        }
        var map = new Dictionary<string, TaskGraphic>(StringComparer.Ordinal);
        foreach (var a in init.Areas)
            if (a.Uuid.Length > 0) map[a.Uuid] = new TaskGraphic(a.Uuid, a.Name, TaskGraphic.KindArea, Pts(a.Points));
        foreach (var l in init.Lines)
            if (l.Uuid.Length > 0 && l.Points.Count > 0)
                map[l.Uuid] = new TaskGraphic(l.Uuid, l.Name, TaskGraphic.KindLine, Pts(l.Points));
        foreach (var p in init.Points)
            if (p.Uuid.Length > 0 && p.HasPosition)
                map[p.Uuid] = new TaskGraphic(p.Uuid, p.Name, TaskGraphic.KindPoint,
                                              new List<(double, double, double?)> { (p.Position.Lat, p.Position.Lon, p.Position.Elev) });
        foreach (var t in init.TaskGraphics)
            if (t.Uuid.Length > 0 && t.Points.Count > 0)
                map[t.Uuid] = new TaskGraphic(t.Uuid, t.Name,
                                              t.Points.Count >= 2 ? TaskGraphic.KindLine : TaskGraphic.KindPoint, Pts(t.Points));
        foreach (var g in order.Graphics)
            if (!map.ContainsKey(g.Uuid)) map[g.Uuid] = new TaskGraphic(g.Uuid, g.Name, g.Kind, Pts(g.Points));

        var byUuid = new Dictionary<string, InitUnit>(StringComparer.Ordinal);
        foreach (var u in init.Units) byUuid[u.Uuid] = u;
        var rows = new List<Judged>();
        foreach (var t in order.Tasks)
        {
            if (!byUuid.TryGetValue(t.TaskeeUuid ?? "", out var unit))
            {
                rows.Add(new Judged(t, "", V.Verdict.Ok, 0, false));
                continue;
            }
            (double Lat, double Lon)? start = null;
            if (double.TryParse(unit.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double la)
                && double.TryParse(unit.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double lo))
                start = (la, lo);
            var pts = V.DemoExtentPoints(t, map, start);
            rows.Add(new Judged(t, unit.Name, V.CheckDemoExtent(bound, pts), pts.Count, true));
        }
        return rows;
    }

    private static string FindData(string name)
    {
        foreach (var start in new[] { AppContext.BaseDirectory, System.IO.Directory.GetCurrentDirectory() })
        {
            var dir = new System.IO.DirectoryInfo(start);
            for (int i = 0; dir != null && i < 10; i++, dir = dir.Parent)
            {
                string candidate = System.IO.Path.Combine(dir.FullName, "data", name);
                if (System.IO.File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    /// <summary>Short task label: the leading "T&lt;n&gt;" of an Iron Storm task name, else the name.</summary>
    private static string Short(string taskName)
    {
        var m = System.Text.RegularExpressions.Regex.Match(taskName ?? "", @"^T0*(\d+)");
        return m.Success ? "T" + m.Groups[1].Value : taskName;
    }

    private static void DemoExtentSection(Action<string, bool> Check)
    {
        static string F(double v, int d) => v.ToString("F" + d, CultureInfo.InvariantCulture);

        Console.WriteLine("--- 11. RL-20261005-02: THE DEMO TERRAIN EXTENT (Vrf:DemoExtent), at ORDER RECEIPT");
        string initPath = FindData(IronInit), cutPath = FindData(CutAOrder),
               fullPath = FindData(FullOrder), rawPath = FindData(RawOrder);
        Check($"the Iron Storm fixtures are on disk (data/{IronInit}, {CutAOrder}, {FullOrder}, {RawOrder})",
              initPath != null && cutPath != null && fullPath != null && rawPath != null);
        if (initPath == null || cutPath == null || fullPath == null || rawPath == null) return;
        var init = InitParser.Parse(System.IO.File.ReadAllText(initPath));
        var orders = new (string Name, OrderData Order)[]
        {
            ("cut A", OrderParser.Parse(System.IO.File.ReadAllText(cutPath))),
            ("FULL", OrderParser.Parse(System.IO.File.ReadAllText(fullPath))),
            ("RAW export", OrderParser.Parse(System.IO.File.ReadAllText(rawPath))),
        };

        Check("the Demo overlay's value parses", V.TryParseDemoExtent(DemoOverlayExtent, DemoOverlayMarginKm, out var demo, out _)
                                                  && demo != null);
        var b = demo.Value;
        Console.WriteLine($"        {b.Describe()}; tested box lat {F(b.Bounded.MinLat, 6)}..{F(b.Bounded.MaxLat, 6)}, " +
                          $"lon {F(b.Bounded.MinLon, 6)}..{F(b.Bounded.MaxLon, 6)}");

        // ---- 11a. OFF CHANGES NOTHING: no extent -> every task of all three orders is accepted ----
        Check("OFF: an empty Vrf:DemoExtent parses as OFF (no bound, no error)",
              V.TryParseDemoExtent("", 0, out var offB, out var offE) && offB == null && offE == null
              && V.TryParseDemoExtent("   ", 2, out var offB2, out _) && offB2 == null);
        foreach (var (name, order) in orders)
        {
            var rows = JudgeOrder(init, order, null);
            Check($"OFF: {name} - all {order.Tasks.Count} task(s) accepted exactly as before (no verdict with the bound null)",
                  rows.TrueForAll(r => !r.Verdict.Violated) && rows.TrueForAll(r => r.InInit));
        }

        // ---- 11b. ON, the Demo overlay's extent: the pinned refusal lists ----
        string[] none = Array.Empty<string>();
        string[] fullOut = { "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T15", "T16", "T18", "T19", "T20", "T21", "T22", "T23" };
        string[] rawOut = { "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T12", "T15", "T16", "T18", "T19", "T20", "T21", "T22", "T23" };
        string[] fullUnpop = { "11_CAB", "169_FAB", "278_ACR", "55_MEB", "56_SBCT" };
        string[] rawUnpop = { "116_ABCT", "11_CAB", "169_FAB", "278_ACR", "55_MEB", "56_SBCT" };
        var expect = new (string[] Out, string[] Unpop)[] { (none, none), (fullOut, fullUnpop), (rawOut, rawUnpop) };
        for (int k = 0; k < orders.Length; k++)
        {
            var (name, order) = orders[k];
            var rows = JudgeOrder(init, order, demo);
            var refused = new List<string>();
            var accepted = new HashSet<string>(StringComparer.Ordinal);
            var performers = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var r in rows)
            {
                string unit = r.Unit.Split('/')[0];
                performers.Add(unit);
                if (r.Verdict.Violated) refused.Add(Short(r.Task.TaskName)); else accepted.Add(unit);
                Console.WriteLine($"        {name,-10} {Short(r.Task.TaskName),-4} {unit,-9} " +
                                  (r.Verdict.Violated ? "REFUSED  " + r.Verdict.Reason : "accepted (" + r.Points + " point(s) inside)"));
            }
            var unpop = new SortedSet<string>(performers, StringComparer.Ordinal);
            unpop.ExceptWith(accepted);
            Check($"ON: {name} - {order.Tasks.Count} task(s) in, {refused.Count} refused [{string.Join(", ", refused)}] " +
                  $"(expected [{string.Join(", ", expect[k].Out)}])",
                  string.Join(",", refused) == string.Join(",", expect[k].Out));
            Check($"ON: {name} - performer(s) NOT populated (every task out): [{string.Join(", ", unpop)}] " +
                  $"(expected [{string.Join(", ", expect[k].Unpop)}]); populated: [{string.Join(", ", accepted.OrderBy(s => s, StringComparer.Ordinal))}]",
                  string.Join(",", unpop) == string.Join(",", expect[k].Unpop));
        }
        Check("ON: cut A's 5 tasks are all IN - the margin covers its one point outside the authored box (T13's route " +
              "vertex 53.97205,23.44140, 1.769 km east of it)",
              orders[0].Order.Tasks.Count == 5 && JudgeOrder(init, orders[0].Order, demo).TrueForAll(r => !r.Verdict.Violated)
              && JudgeOrder(init, orders[0].Order, new V.DemoBound(b.Authored, 0, b.Authored)).FindAll(r => r.Verdict.Violated).Count == 1);

        // ---- 11c. THE EDGE: 1 m inside is kept, 1 m outside is refused, on the north and the east edge ----
        double mPerDegLat = V.EarthRadiusMeters * Math.PI / 180.0;
        double midLat = (b.Bounded.MinLat + b.Bounded.MaxLat) / 2, midLon = (b.Bounded.MinLon + b.Bounded.MaxLon) / 2;
        double mPerDegLonEast = mPerDegLat * Math.Cos(midLat * Math.PI / 180.0);
        var pIn = new[] { new V.ExtentPoint("route vertex 1 of 1", b.Bounded.MaxLat - 1.0 / mPerDegLat, midLon) };
        var pOut = new[] { new V.ExtentPoint("route vertex 1 of 1", b.Bounded.MaxLat + 1.0 / mPerDegLat, midLon) };
        var eIn = new[] { new V.ExtentPoint("route vertex 1 of 1", midLat, b.Bounded.MaxLon - 1.0 / mPerDegLonEast) };
        var eOut = new[] { new V.ExtentPoint("route vertex 1 of 1", midLat, b.Bounded.MaxLon + 1.0 / mPerDegLonEast) };
        var vOut = V.CheckDemoExtent(demo, pOut);
        var veOut = V.CheckDemoExtent(demo, eOut);
        Check("EDGE: a vertex 1 m INSIDE the north edge of the tested box is kept", !V.CheckDemoExtent(demo, pIn).Violated);
        Check($"EDGE: a vertex 1 m OUTSIDE the north edge is refused, {F(vOut.Meters, 3)} m out (expected 1.000 +/- 0.001)",
              vOut.Violated && vOut.Kind == V.Violation.OutsideExtent && Math.Abs(vOut.Meters - 1.0) < 1e-3);
        Check("EDGE: a vertex 1 m INSIDE the east edge is kept", !V.CheckDemoExtent(demo, eIn).Violated);
        Check($"EDGE: a vertex 1 m OUTSIDE the east edge is refused, {F(veOut.Meters, 3)} m out (expected 1.000 +/- 0.001)",
              veOut.Violated && Math.Abs(veOut.Meters - 1.0) < 1e-3);
        Check("EDGE: the margin is at least the stated distance - the authored north edge + margin is inside, and the " +
              "authored east edge + margin (measured at the box's northern edge) is inside",
              !V.CheckDemoExtent(demo, new[] { new V.ExtentPoint("p", b.Authored.MaxLat + (DemoOverlayMarginKm * 1000.0 - 0.5) / mPerDegLat, midLon) }).Violated
              && !V.CheckDemoExtent(demo, new[] { new V.ExtentPoint("p", b.Authored.MaxLat,
                    b.Authored.MaxLon + (DemoOverlayMarginKm * 1000.0 - 0.5) / (mPerDegLat * Math.Cos(b.Authored.MaxLat * Math.PI / 180.0))) }).Violated);

        // ---- 11d. THE TASKABRT TEXT ----
        string abort = V.DemoExtentAbort("T22_Occupy", vOut);
        Console.WriteLine("        TASKABRT: " + abort);
        Check("the TASKABRT reads 'OUT OF DEMO EXTENT: <what> at <lat,lon> is <km> km outside <extent> ... " +
              "(Vrf:DemoExtent, RL-20261005-02) - task ... refused, not executed'",
              abort.StartsWith("OUT OF DEMO EXTENT: route vertex 1 of 1 at ", StringComparison.Ordinal)
              && abort.Contains(" is 0.001 km outside the demo extent S 53.93972 W 23.10848 N 54.11939 E 23.41436 + 2 km margin",
                                StringComparison.Ordinal)
              && abort.Contains("(Vrf:DemoExtent, RL-20261005-02)", StringComparison.Ordinal)
              && abort.Contains("task 'T22_Occupy' refused, not executed", StringComparison.Ordinal));
        var fab = JudgeOrder(init, orders[2].Order, demo).Find(r => Short(r.Task.TaskName) == "T22");
        Check("a performer created outside is named FIRST: T22 is refused on 169 FAB's start position (54.15,23.10)",
              fab != null && fab.Verdict.Reason.StartsWith("the performer's start position at 54.15000,23.10000 is ", StringComparison.Ordinal));

        // ---- 11e. SUCCESSORS CHAIN AS TODAY: a refused task is ABANDONED, so its successor's gate fails at once ----
        var t16 = orders[1].Order.Tasks.Find(t => Short(t.TaskName) == "T16");
        var t17 = orders[1].Order.Tasks.Find(t => Short(t.TaskName) == "T17");
        Check("FULL: T17 (48 IBCT, accepted - its one point is inside) is gated on T16 (refused)",
              t16 != null && t17 != null && t17.StartAfterTaskUuid == t16.TaskUuid);
        if (t16 != null && t17 != null)
        {
            var seq = new TaskSequencer();
            seq.NotifyAbandoned(t16.TaskUuid);                       // what the order-receipt refusal does
            var gate = seq.WaitForStartAsync(t17.StartAfterTaskUuid, 0, 0, 5.0, TaskClock.Wall, System.Threading.CancellationToken.None);
            bool done = gate.Wait(TimeSpan.FromSeconds(2));
            Check("... and T17's gate fails at once with PredecessorAbandoned -> \"SKIPPED: predecessor <uuid> was " +
                  "skipped/abandoned upstream\" (the existing cascade)",
                  done && gate.Result == GateResult.PredecessorAbandoned
                  && TaskDispatchPolicy.GateFailureReason(gate.Result, 0, 0) == "was skipped/abandoned upstream");
        }

        // ---- 11f. A MALFORMED SETTING IS AN ERROR, NEVER A GUESS ----
        Check("malformed: three fields, a box with south >= north, a non-number, and a negative margin are all REJECTED",
              !V.TryParseDemoExtent("53.9,23.1,54.1", 0, out _, out var e1) && e1 != null
              && !V.TryParseDemoExtent("54.2,23.1,54.1,23.4", 0, out _, out _)
              && !V.TryParseDemoExtent("53.9,abc,54.1,23.4", 0, out _, out _)
              && !V.TryParseDemoExtent(DemoOverlayExtent, -1, out _, out _));
    }
}
