using Microsoft.Extensions.Configuration;

namespace VrfC2SimApp;

/// <summary>
/// RL-20260927-01 - a LONE ground PLATFORM drives its route as one Move To per vertex; UNITS keep Move Along
/// Route. Checked offline as its own section of `--rulings-selftest` (no bridge, no MAK, no clock):
///
///   (v1) THE FORM - VertexChainPolicy.FormFor, the whole truth table, with the switch OFF as the
///        fail-first arm (the pre-change behaviour: a lone platform gets CreateRoute).
///   (v2) THE CHAIN - VertexChainTracker: next vertex, ends at the last, a two-point route is exactly one
///        Move To, the vacuous flag, never two Move Tos in flight, no double start, cleared on abort, frozen
///        (never re-tasked) by a back-end loss.
///   (v3) THE DISPATCH - the three movement arms of ExecuteTaskOnTick replayed in the service's order on
///        the REAL FormFor and the REAL tracker: a lone platform with a 3-point route issues MoveToLocation
///        x1 first and CreateRoute x0, a unit still issues CreateRoute; then source tripwires that pin the
///        service to exactly that shape (the EngageDoctrineSelfTest e8/e13 style).
///   (v4) THE ENGAGE - a platform's parked Fire At is released on the LAST vertex's completion only.
///   (v5) THE SETTING - default ON in code and in both shipped json files, OFF by the environment variable,
///        and the start-up line names RL-20260927-01 in both states.
/// </summary>
public static class VertexChainSelfTest
{
    // A north-running line near the Mojave AO: 0.01 deg of latitude is about 1,112 m.
    private static readonly VertexChainTracker.Point Origin = new(35.000, -116.000, 900.0);
    private static readonly VertexChainTracker.Point V1 = new(35.010, -116.000, 910.0);
    private static readonly VertexChainTracker.Point V2 = new(35.020, -116.000, 920.0);
    private static readonly VertexChainTracker.Point V3 = new(35.030, -116.000, 930.0);

    public static int Run()
    {
        int failures = 0;
        FormTable(ref failures);
        Chain(ref failures);
        Dispatch(ref failures);
        Engage(ref failures);
        Setting(ref failures);
        return failures;
    }

    // ------------------------------------------------------------------------------ (v1) ----
    private static void FormTable(ref int failures)
    {
        Check(ref failures,
              VertexChainPolicy.FormFor(isAggregate: false, isGround: true, patrol: false, routePoints: 3, perVertexEnabled: true)
                  == GroundMoveForm.MoveToPerVertex,
              "(v1) a LONE ground PLATFORM with a 3-point route -> Move To per vertex");
        Check(ref failures,
              VertexChainPolicy.FormFor(false, true, false, 2, true) == GroundMoveForm.MoveToPerVertex,
              "(v1) ... and with a 2-point route (live start + one vertex, T02/T14 in cut A) too");
        Check(ref failures,
              VertexChainPolicy.FormFor(isAggregate: true, isGround: true, patrol: false, routePoints: 3, perVertexEnabled: true)
                  == GroundMoveForm.RouteTask,
              "(v1) a UNIT is unchanged: CreateRoute + MoveAlongRoute (its unit task plans per vertex, UG52 30.22/30.24)");
        Check(ref failures,
              VertexChainPolicy.FormFor(false, true, patrol: true, routePoints: 3, perVertexEnabled: true) == GroundMoveForm.RouteTask,
              "(v1) a PATROL (Reconnoiter) is unchanged: CreateRoute + PatrolRoute");
        Check(ref failures,
              VertexChainPolicy.FormFor(false, isGround: false, false, 3, true) == GroundMoveForm.RouteTask,
              "(v1) a NON-GROUND platform is unchanged: Move To's planner is the ground planner");
        Check(ref failures,
              VertexChainPolicy.FormFor(false, true, false, 1, true) == GroundMoveForm.SinglePointMoveTo
              && VertexChainPolicy.FormFor(true, true, false, 1, true) == GroundMoveForm.SinglePointMoveTo,
              "(v1) a ONE-point route is the single MoveToLocation it always was, platform or unit");
        Check(ref failures,
              VertexChainPolicy.FormFor(false, true, false, 3, perVertexEnabled: false) == GroundMoveForm.RouteTask,
              "FAIL-FIRST (v1): with Vrf:PlatformMoveToPerVertex=false the same lone platform gets CreateRoute + " +
              "MoveAlongRoute - the pre-2026-09-27 behaviour, so the ON rows above measure the switch");
        // Exhaustive: ONLY switched-on + not-aggregate + ground + not-patrol + 2 or more points chains.
        int chained = 0, cells = 0;
        foreach (bool agg in new[] { false, true })
            foreach (bool ground in new[] { false, true })
                foreach (bool patrol in new[] { false, true })
                    foreach (bool on in new[] { false, true })
                        foreach (int pts in new[] { 1, 2, 3, 7 })
                        {
                            cells++;
                            var f = VertexChainPolicy.FormFor(agg, ground, patrol, pts, on);
                            bool expect = on && !agg && ground && !patrol && pts >= 2;
                            if ((f == GroundMoveForm.MoveToPerVertex) == expect) { if (expect) chained++; }
                            else chained = int.MinValue;
                        }
        Check(ref failures, chained == 3,
              $"(v1) exhaustive table ({cells} cells): exactly the 3 lone-ground-platform, switched-on, non-patrol, " +
              $"2+-point cells chain (got {(chained < 0 ? "a mismatch" : chained.ToString())})");
    }

    // ------------------------------------------------------------------------------ (v2) ----
    private static void Chain(ref int failures)
    {
        // THREE-POINT ROUTE: live start + two vertices.
        var t = new VertexChainTracker(VertexChainPolicy.DefaultVertexArrivalRadiusMeters);
        var s = t.Start("T14 M577", "VRF_UUID:a", "task-1", "taskee-1", "T14", Origin, new[] { V1, V2 });
        Check(ref failures, s.Outcome == VertexChainTracker.StartOutcome.Started && s.First == V1
                            && s.Chain.VertexNumber == 1 && s.Chain.VertexCount == 2 && s.Chain.Outstanding,
              "(v2) Start on a 3-point route: vertex 1 of 2 is OUTSTANDING and the first Move To goes to route point 1 " +
              "(point 0, the live start, is never driven to)");

        var d1 = t.OnCompletion("T14 M577", "move-to", true, V1, arrivalAlreadyReported: false);
        Check(ref failures, d1.Outcome == VertexChainTracker.Outcome.Advance && d1.Next == V2 && d1.NextVertex == 2
                            && d1.CompletedVertex == 1 && !d1.Vacuous,
              $"(v2) vertex 1 of 2 completes AT the vertex -> Advance to vertex 2 (got {d1.Outcome}, next {d1.NextVertex})");
        Check(ref failures, Math.Abs(d1.DistanceMeters) < 0.5 && Math.Abs(d1.DisplacementMeters - 1112.0) < 5.0
                            && d1.DisplacementFrom == 0,
              FormattableString.Invariant(
                  $"(v2) ... and the R11 figures are logged: {d1.DistanceMeters:F1} m from the vertex, {d1.DisplacementMeters:F0} m ") +
              "moved since DISPATCH");

        var dup = t.OnCompletion("T14 M577", "move-to", true, V1, false);
        Check(ref failures, dup.Outcome == VertexChainTracker.Outcome.Stray,
              $"(v2) a SECOND completion before vertex 2 is issued is a Stray - no double step, no Move To (got {dup.Outcome})");

        bool issued = t.TryBeginIssue("T14 M577", d1.Chain.Generation, 2, out var next, out var afterIssue);
        bool issuedTwice = t.TryBeginIssue("T14 M577", d1.Chain.Generation, 2, out _, out _);
        Check(ref failures, issued && next == V2 && afterIssue.Outstanding && !issuedTwice,
              "(v2) TryBeginIssue releases vertex 2 EXACTLY ONCE - the next Move To is never issued twice, and never " +
              "before the previous one completed");

        var d2 = t.OnCompletion("T14 M577", "move-to", true, V2, false);
        Check(ref failures, d2.Outcome == VertexChainTracker.Outcome.FinalVertex && d2.CompletedVertex == 2
                            && t.Count == 0 && !t.TryGet("T14 M577", out _),
              $"(v2) vertex 2 of 2 is the LAST: the chain ENDS and the completion is handed on (got {d2.Outcome})");
        Check(ref failures, Math.Abs(d2.DisplacementMeters - 1112.0) < 5.0 && d2.DisplacementFrom == 1,
              "(v2) ... its displacement is measured from vertex 1's completion, not from dispatch");
        Check(ref failures, t.OnCompletion("T14 M577", "move-to", true, V2, false).Outcome == VertexChainTracker.Outcome.NotChained,
              "(v2) a completion after the chain ended is NotChained - the service's own path handles it as before");

        // TWO-POINT ROUTE = EXACTLY ONE MOVE TO.
        var t2 = new VertexChainTracker(100.0);
        var s2 = t2.Start("T02 M1A2", "VRF_UUID:b", "task-2", "taskee-2", "T02", Origin, new[] { V1 });
        var only = t2.OnCompletion("T02 M1A2", "move-to", true, V1, false);
        Check(ref failures, s2.Outcome == VertexChainTracker.StartOutcome.Started && s2.Chain.VertexCount == 1
                            && only.Outcome == VertexChainTracker.Outcome.FinalVertex && t2.Count == 0,
              "(v2) a TWO-point route is exactly ONE Move To: its first completion is the last (no Advance, no second issue)");

        // THE VACUOUS FLAG (R11). The unit "completes" vertex 1 standing on its start: 1,112 m away.
        var tv = new VertexChainTracker(100.0);
        tv.Start("U", "VRF_UUID:c", "task-3", "taskee-3", "T3", Origin, new[] { V1, V2 });
        var vac = tv.OnCompletion("U", "move-to", true, Origin, false);
        Check(ref failures, vac.Outcome == VertexChainTracker.Outcome.Advance && vac.Vacuous && vac.DistanceMeters > 1000.0,
              FormattableString.Invariant($"(v2) a completion {vac.DistanceMeters:F0} m from its vertex is flagged VACUOUS and the ") +
              "chain still CONTINUES (the C2SIM outcome is decided on the last vertex)");
        tv.TryBeginIssue("U", vac.Chain.Generation, 2, out _, out _);
        var vacLast = tv.OnCompletion("U", "move-to", true, Origin, false);
        Check(ref failures, vacLast.Outcome == VertexChainTracker.Outcome.FinalVacuous && vacLast.Vacuous && tv.Count == 0,
              $"(v2) a VACUOUS LAST vertex is NOT handed on as the task's arrival: FinalVacuous, the chain ends and arrival " +
              $"evidence decides (got {vacLast.Outcome})");
        var tvr = new VertexChainTracker(100.0);
        tvr.Start("U", "VRF_UUID:c", "task-3b", "taskee-3", "T3b", Origin, new[] { V1 });
        var vacReported = tvr.OnCompletion("U", "move-to", true, Origin, arrivalAlreadyReported: true);
        Check(ref failures, vacReported.Outcome == VertexChainTracker.Outcome.FinalVertex && vacReported.Vacuous,
              "(v2) ... unless arrival evidence has ALREADY reported the task: then it is handed on to the service's swallow");
        var near = new VertexChainTracker(100.0);
        near.Start("U", "VRF_UUID:c", "task-3c", "taskee-3", "T3c", Origin, new[] { V1 });
        var at80 = near.OnCompletion("U", "move-to", true, new VertexChainTracker.Point(35.010 - 80.0 / 111195.0, -116.0), false);
        Check(ref failures, at80.Outcome == VertexChainTracker.Outcome.FinalVertex && !at80.Vacuous,
              FormattableString.Invariant($"(v2) {at80.DistanceMeters:F0} m from the vertex is inside the 100 m bar: a genuine arrival"));
        var off = new VertexChainTracker(0.0);
        off.Start("U", "VRF_UUID:c", "task-3d", "taskee-3", "T3d", Origin, new[] { V1 });
        Check(ref failures, off.OnCompletion("U", "move-to", true, Origin, false).Outcome == VertexChainTracker.Outcome.FinalVertex,
              "(v2) Vrf:VertexArrivalRadiusMeters=0 switches the test off: every completion is an arrival");
        var blind = new VertexChainTracker(100.0);
        blind.Start("U", "VRF_UUID:c", "task-3e", "taskee-3", "T3e", Origin, new[] { V1, V2 });
        var nofix = blind.OnCompletion("U", "move-to", true, null, false);
        Check(ref failures, nofix.Outcome == VertexChainTracker.Outcome.Advance && !nofix.Vacuous
                            && double.IsNaN(nofix.DistanceMeters) && double.IsNaN(nofix.DisplacementMeters),
              "(v2) an UNREADABLE position is never called vacuous (NaN, said as not known) - the chain claims only what it measured");

        // CLEARED ON ABORT - a vendor FAILURE, a new task, a back-end loss.
        var tf = new VertexChainTracker(100.0);
        tf.Start("U", "VRF_UUID:d", "task-4", "taskee-4", "T4", Origin, new[] { V1, V2, V3 });
        var failed = tf.OnCompletion("U", "move-to", false, Origin, false);
        Check(ref failures, failed.Outcome == VertexChainTracker.Outcome.Failed && failed.CompletedVertex == 1 && tf.Count == 0,
              "(v2) success=false on vertex 1 of 3 -> Failed: the chain ENDS and the failure path (TASKABRT) takes over");
        var tn = new VertexChainTracker(100.0);
        tn.Start("U", "VRF_UUID:d", "task-5", "taskee-5", "T5", Origin, new[] { V1, V2 });
        bool sameKept = !tn.ClearIfOtherTask("U", "task-5", out _);
        bool otherCleared = tn.ClearIfOtherTask("U", "task-6", out var endedByNew);
        Check(ref failures, sameKept && otherCleared && endedByNew.TaskUuid == "task-5" && tn.Count == 0,
              "(v2) a NEW task for the unit ends its chain; a re-dispatch of the SAME task does not");
        // A BACK-END LOSS FREEZES (STP-822: every task aborted, nothing re-tasked on recovery).
        var ta = new VertexChainTracker(100.0);
        ta.Start("A", "VRF_UUID:e", "task-7", "t", "T7", Origin, new[] { V1, V2, V3 });
        ta.Start("B", "VRF_UUID:f", "task-8", "t", "T8", Origin, new[] { V1, V2 });
        var aAdv = ta.OnCompletion("A", "move-to", true, V1, false);          // A: vertex 2 queued, not issued
        int frozen = ta.FreezeAll();
        Check(ref failures, frozen == 2 && ta.TryGet("A", out var fa) && fa.Frozen
                            && !ta.TryBeginIssue("A", aAdv.Chain.Generation, 2, out _, out _),
              "(v2) a back-end loss FREEZES every chain: a vertex queued at the loss is never issued");
        var bLate = ta.OnCompletion("B", "move-to", true, V1, false);          // B was on vertex 1 of 2
        Check(ref failures, bLate.Outcome == VertexChainTracker.Outcome.Retired && !ta.TryGet("B", out _),
              "(v2) ... a frozen chain's INTERMEDIATE vertex that still completes (a hung, not dead, back end) is " +
              $"Retired - swallowed, not the task's arrival, and nothing more is issued (got {bLate.Outcome})");
        Check(ref failures, ta.OnCompletion("A", "move-to", true, V1, false).Outcome == VertexChainTracker.Outcome.Retired
                            && ta.Count == 0,
              "(v2) ... a frozen chain with nothing outstanding ends on whatever arrives next");
        var tl = new VertexChainTracker(100.0);
        tl.Start("C", "VRF_UUID:k", "task-8b", "t", "T8b", Origin, new[] { V1 });
        tl.FreezeAll();
        Check(ref failures, tl.OnCompletion("C", "move-to", true, V1, false).Outcome == VertexChainTracker.Outcome.FinalVertex,
              "(v2) ... but a frozen chain's LAST vertex is handed on as for any chain - the task is still in flight");
        var tr = new VertexChainTracker(100.0);
        tr.Start("D", "VRF_UUID:l", "task-8c", "t", "T8c", Origin, new[] { V1, V2 });
        tr.FreezeAll();
        Check(ref failures, tr.Start("D", "VRF_UUID:l", "task-8c", "t", "T8c", Origin, new[] { V1, V2 }).Outcome
                            == VertexChainTracker.StartOutcome.Replaced,
              "(v2) the SAME task re-pushed after a recovery REPLACES its frozen chain (a new order is required, STP-822)");
        var tc = new VertexChainTracker(100.0);
        tc.Start("U", "VRF_UUID:g", "task-9", "t", "T9", Origin, new[] { V1, V2 });
        var dc = tc.OnCompletion("U", "move-to", true, V1, false);
        tc.Clear("U", out _);
        Check(ref failures, !tc.TryBeginIssue("U", dc.Chain.Generation, 2, out _, out _),
              "(v2) a chain ended while its next Move To was queued issues NOTHING (the queued TryBeginIssue is refused)");

        // NO DOUBLE START, and a replaced chain's queued vertex is dead.
        var td = new VertexChainTracker(100.0);
        var first = td.Start("U", "VRF_UUID:h", "task-10", "t", "T10", Origin, new[] { V1, V2 });
        var again = td.Start("U", "VRF_UUID:h", "task-10", "t", "T10", Origin, new[] { V1, V2 });
        Check(ref failures, again.Outcome == VertexChainTracker.StartOutcome.AlreadyRunning
                            && td.TryGet("U", out var still) && still.Generation == first.Chain.Generation,
              "(v2) NO DOUBLE START: the same task started again on the same unit changes nothing (AlreadyRunning)");
        var adv = td.OnCompletion("U", "move-to", true, V1, false);
        var repl = td.Start("U", "VRF_UUID:h", "task-11", "t", "T11", Origin, new[] { V3 });
        Check(ref failures, repl.Outcome == VertexChainTracker.StartOutcome.Replaced && repl.Replaced.TaskUuid == "task-10"
                            && !td.TryBeginIssue("U", adv.Chain.Generation, 2, out _, out _),
              "(v2) a DIFFERENT task replaces the chain, and the old chain's queued vertex can no longer be issued");

        // ONLY A MOVE-TO ADVANCES THE CHAIN.
        var tt = new VertexChainTracker(100.0);
        tt.Start("U", "VRF_UUID:i", "task-12", "t", "T12", Origin, new[] { V1, V2 });
        var lateAlong = tt.OnCompletion("U", "move-along", true, V1, false);
        var fire = tt.OnCompletion("U", "fire-at-target", true, V1, false);
        var gvmt = tt.OnCompletion("U", "ground-vehicle-move-to", true, V1, false);
        Check(ref failures, lateAlong.Outcome == VertexChainTracker.Outcome.Stray && fire.Outcome == VertexChainTracker.Outcome.Stray
                            && gvmt.Outcome == VertexChainTracker.Outcome.Advance,
              "(v2) a late 'move-along' or a 'fire-at-target' completion is a Stray; a 'ground-vehicle-move-to' one is a vertex");
        Check(ref failures, VertexChainPolicy.IsChainMoveToType("move-to") && VertexChainPolicy.IsChainMoveToType("")
                            && VertexChainPolicy.IsChainMoveToType(null) && !VertexChainPolicy.IsChainMoveToType("move-along"),
              "(v2) type test: 'move-to' (moveToTask.h :60) and an empty type are accepted, 'move-along' is not");
        Check(ref failures, !tt.TryBeginIssue("U", gvmt.Chain.Generation, 3, out _, out _),
              "(v2) TryBeginIssue refuses a vertex number the Advance did not name");
        Check(ref failures,
              t.Start("", "x", "y", "z", "w", Origin, new[] { V1 }).Outcome == VertexChainTracker.StartOutcome.Refused
              && t.Start("U", "x", "y", "z", "w", Origin, Array.Empty<VertexChainTracker.Point>()).Outcome
                 == VertexChainTracker.StartOutcome.Refused,
              "(v2) no unit name or no vertex: Refused, nothing started");
    }

    // ------------------------------------------------------------------------------ (v3) ----
    /// <summary>What the three movement arms of ExecuteTaskOnTick's committed dispatch point issue, in the
    /// service's order, on the REAL FormFor and the REAL tracker. The service arms are pinned to this shape
    /// by the source tripwires below.</summary>
    private sealed class DispatchRecorder
    {
        public readonly VertexChainTracker Chains = new(VertexChainPolicy.DefaultVertexArrivalRadiusMeters);
        public readonly List<string> Commands = new();
        public int Count(string verb) => Commands.Count(c => c.StartsWith(verb + "(", StringComparison.Ordinal));

        public GroundMoveForm Dispatch(string unit, string taskUuid, bool isAggregate, bool isGround, bool patrol,
                                       IReadOnlyList<VertexChainTracker.Point> routeGeo, bool enabled)
        {
            var form = VertexChainPolicy.FormFor(isAggregate, isGround, patrol, routeGeo.Count, enabled);
            if (form == GroundMoveForm.SinglePointMoveTo)
                Commands.Add($"MoveToLocation({unit}@{routeGeo[^1].Lat:F3})");
            else if (form == GroundMoveForm.MoveToPerVertex)
            {
                var s = Chains.Start(unit, "VRF_UUID:" + unit, taskUuid, "taskee", "task", routeGeo[0],
                                     routeGeo.Skip(1).ToList());
                if (s.Outcome is VertexChainTracker.StartOutcome.Started or VertexChainTracker.StartOutcome.Replaced)
                    Commands.Add($"MoveToLocation({unit}@{s.First.Lat:F3})");
            }
            else
                Commands.Add($"CreateRoute({unit},{routeGeo.Count})");
            return form;
        }

        /// <summary>OnVrfTaskCompleted's routing + the tick-thread continuation it enqueues.</summary>
        public VertexChainTracker.Outcome Complete(string unit, VertexChainTracker.Point at)
        {
            var d = Chains.OnCompletion(unit, "move-to", true, at, false);
            if (d.Outcome == VertexChainTracker.Outcome.Advance
                && Chains.TryBeginIssue(unit, d.Chain.Generation, d.NextVertex, out var v, out _))
                Commands.Add($"MoveToLocation({unit}@{v.Lat:F3})");
            return d.Outcome;
        }
    }

    private static void Dispatch(ref int failures)
    {
        var route3 = new[] { Origin, V1, V2 };

        var lone = new DispatchRecorder();
        var form = lone.Dispatch("T14", "task-T14", isAggregate: false, isGround: true, patrol: false, route3, enabled: true);
        Check(ref failures, form == GroundMoveForm.MoveToPerVertex && lone.Commands.Count == 1
                            && lone.Commands[0] == "MoveToLocation(T14@35.010)" && lone.Count("CreateRoute") == 0,
              $"(v3) a LONE platform with a 3-point route issues MoveToLocation x1 FIRST (to vertex 1) and CreateRoute x0 " +
              $"[{string.Join(", ", lone.Commands)}]");
        var o1 = lone.Complete("T14", V1);
        Check(ref failures, o1 == VertexChainTracker.Outcome.Advance && lone.Count("MoveToLocation") == 2
                            && lone.Commands[1] == "MoveToLocation(T14@35.020)" && lone.Count("CreateRoute") == 0,
              "(v3) ... vertex 1's completion issues vertex 2's MoveToLocation - still no route object");
        var o2 = lone.Complete("T14", V2);
        Check(ref failures, o2 == VertexChainTracker.Outcome.FinalVertex && lone.Count("MoveToLocation") == 2,
              "(v3) ... and vertex 2's completion issues NOTHING more: it is the last, handed to the completion rules");
        var redispatch = lone.Dispatch("T14", "task-T14", false, true, false, route3, true);
        Check(ref failures, redispatch == GroundMoveForm.MoveToPerVertex && lone.Count("MoveToLocation") == 3,
              "(v3) a later dispatch of the same task, once its chain has ENDED, is a new dispatch as before");

        var unit = new DispatchRecorder();
        unit.Dispatch("1-112 IN", "task-T10", isAggregate: true, isGround: true, patrol: false, route3, enabled: true);
        Check(ref failures, unit.Count("CreateRoute") == 1 && unit.Count("MoveToLocation") == 0,
              $"(v3) a UNIT with the same route still issues CreateRoute x1 and MoveToLocation x0 [{string.Join(", ", unit.Commands)}]");

        var offArm = new DispatchRecorder();
        offArm.Dispatch("T14", "task-T14", false, true, false, route3, enabled: false);
        Check(ref failures, offArm.Count("CreateRoute") == 1 && offArm.Count("MoveToLocation") == 0,
              "FAIL-FIRST (v3): switched OFF, the same lone platform issues CreateRoute x1 - the assertion above measures the switch");

        var twice = new DispatchRecorder();
        twice.Dispatch("T02", "task-T02", false, true, false, route3, true);
        twice.Dispatch("T02", "task-T02", false, true, false, route3, true);
        Check(ref failures, twice.Count("MoveToLocation") == 1,
              "(v3) NO DOUBLE START: the same task dispatched again while its chain runs issues no second Move To");

        // SOURCE TRIPWIRES - the service arms are the recorder's shape.
        string repo = FindRepoRoot();
        string service = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs");
        Check(ref failures, service != null && File.Exists(service), $"(v3) the service source is on disk ({service})");
        if (service == null || !File.Exists(service)) return;
        string src = File.ReadAllText(service);

        Check(ref failures, CountOf(src, "VertexChainPolicy.FormFor(") == 1,
              $"(v3) the service decides the form in ONE place (VertexChainPolicy.FormFor x{CountOf(src, "VertexChainPolicy.FormFor(")})");
        int single = src.IndexOf("if (moveForm == GroundMoveForm.SinglePointMoveTo)", StringComparison.Ordinal);
        int chainArm = src.IndexOf("if (moveForm == GroundMoveForm.MoveToPerVertex)", StringComparison.Ordinal);
        int startCall = src.IndexOf("StartVertexChain(task, unit, vrfUuid, routeGeo, attackTargetVrf);", StringComparison.Ordinal);
        int createRoute = src.IndexOf("_bridge.CreateRoute(routeGeo, routeName);", StringComparison.Ordinal);
        Check(ref failures, single > 0 && chainArm > single && startCall > chainArm && createRoute > startCall
                            && src.Substring(startCall, createRoute - startCall).Contains("return;"),
              "(v3) ExecuteTaskOnTick: single point, THEN the chain arm (StartVertexChain; return), THEN CreateRoute - a " +
              "chained platform never reaches CreateRoute");
        Check(ref failures, CountOf(src, "_bridge.CreateRoute(routeGeo, routeName);") == 1,
              "(v3) the dispatch CreateRoute exists once, on the route arm");
        Check(ref failures, CountOf(src, "_bridge.MoveToLocation(") == 3,
              $"(v3) _bridge.MoveToLocation is called in exactly 3 places - the single point, a chain's vertex 1, a chain's " +
              $"next vertex (got {CountOf(src, "_bridge.MoveToLocation(")})");
        string startBody = Between(src, "private void StartVertexChain(", "private bool ConsumeVertexChainCompletion(");
        Check(ref failures, startBody.Contains("_vertexChains.TryGet(unit.Name, out var running)")
                            && startBody.IndexOf("_vertexChains.TryGet(", StringComparison.Ordinal)
                               < startBody.IndexOf("MarkDispatched(", StringComparison.Ordinal)
                            && startBody.IndexOf("MarkDispatched(", StringComparison.Ordinal)
                               < startBody.IndexOf("_vertexChains.Start(", StringComparison.Ordinal),
              "(v3) StartVertexChain: the same-task check comes BEFORE MarkDispatched, and MarkDispatched before Start");
        Check(ref failures, startBody.Contains("MarkDispatched(task, unit, VertexChainPolicy.DispatchKind, routeGeo[^1], routeGeo)"),
              "(v3) the chained task's destination is the LAST vertex and its journey the whole route (arrival, time rules, watchdog)");
        string glue = Between(src, "private bool ConsumeVertexChainCompletion(", "/// P0.3: park a PLATFORM's ATTACK Fire At");
        Check(ref failures, glue.Contains("_tickActions.Enqueue(() => DeferredDispatch.Run(")
                            && glue.Contains("DeferredDispatch.VertexChainContinuation"),
              "(v3) the next vertex re-enters the tick thread through _tickActions + DeferredDispatch.Run (the D1 ending)");
        Check(ref failures, !glue.Contains("SynthesizeUnitCompletion(") && !glue.Contains("IssueEngage(")
                            && !startBody.Contains("SynthesizeUnitCompletion(") && !startBody.Contains("IssueEngage("),
              "(v3) the chain glue never completes a task and never fires an engage itself");
        // WHICH OUTCOMES REACH THE COMPLETION PATH: true = consumed by the chain, false = handed on.
        string routing = string.Join(", ", new[] { "Advance", "FinalVertex", "FinalVacuous", "Failed", "Stray", "Retired" }
                                        .Select(o => o + "=" + ReturnAfter(glue, "case VertexChainTracker.Outcome." + o + ":")));
        Check(ref failures, routing == "Advance=true, FinalVertex=false, FinalVacuous=true, Failed=false, Stray=true, Retired=true",
              $"(v3) the service hands on ONLY the last vertex and a failure; an intermediate vertex, a vacuous last vertex, " +
              $"a stray and a retired completion are consumed ({routing})");
        string completed = Between(src, "private void OnVrfTaskCompleted(", "private void SynthesizeUnitCompletion(");
        int hook = completed.IndexOf("ConsumeVertexChainCompletion(marking, e.TaskType, success)) return;", StringComparison.Ordinal);
        int clear = completed.IndexOf("ClearStallState(marking);", StringComparison.Ordinal);
        int swallow = completed.IndexOf("_arrivalReported.TryRemove(marking", StringComparison.Ordinal);
        int synth = completed.IndexOf("SynthesizeUnitCompletion(name, e.TaskType, success);", StringComparison.Ordinal);
        Check(ref failures, hook > 0 && hook < clear && hook < swallow && hook < synth,
              "(v3) OnVrfTaskCompleted routes through the chain FIRST - before the watchdog reset, the arrival swallow " +
              "and the completion");
        Check(ref failures, Between(src, "private void MarkDispatched(", "private void StartVertexChain(")
                                .Contains("_vertexChains.ClearIfOtherTask(unit.Name, task.TaskUuid"),
              "(v3) every dispatch ends a chain ANOTHER task left on the unit (MarkDispatched)");
        Check(ref failures, Between(src, "private void IssueEngage(", "private static Roe ToRoe(").Contains("_vertexChains.Clear(unitName"),
              "(v3) a Fire At replacing the move ends the chain (IssueEngage)");
        Check(ref failures, Between(src, "private void MaybeCheckBackendLiveness()", "private int SampleAndJudgeStall(")
                                .Contains("_vertexChains.FreezeAll()"),
              "(v3) a back-end loss freezes every chain");
        Check(ref failures, startBody.Contains("!running.Frozen"),
              "(v3) a FROZEN chain of the same task does not block its re-push after a recovery");
        Check(ref failures, src.Contains("VertexChainPolicy.StartupLine(_vrf.PlatformMoveToPerVertex"),
              "(v3) the start-up line is logged from the setting");
    }

    // ------------------------------------------------------------------------------ (v4) ----
    /// <summary>A platform ATTACK's Fire At, parked on the move task (P0.3), is released by the service only
    /// where a completion LEAVES the chain with success - SynthesizeUnitCompletion's release. So route the
    /// completions through the REAL tracker and release exactly where the service would.</summary>
    private static bool Released(VertexChainTracker t, string unit, VertexChainTracker.Point at, bool success,
                                 ref bool parked)
    {
        var d = t.OnCompletion(unit, "move-to", success, at, false);
        bool passesThrough = d.Outcome is VertexChainTracker.Outcome.NotChained or VertexChainTracker.Outcome.FinalVertex
                                       or VertexChainTracker.Outcome.Failed;
        if (d.Outcome == VertexChainTracker.Outcome.Advance)
            t.TryBeginIssue(unit, d.Chain.Generation, d.NextVertex, out _, out _);
        if (parked && passesThrough && success) { parked = false; return true; }
        return false;
    }

    private static void Engage(ref int failures)
    {
        var t = new VertexChainTracker(100.0);
        bool parked = true;
        t.Start("P", "VRF_UUID:p", "attack-1", "taskee", "ATT", Origin, new[] { V1, V2, V3 });
        bool r1 = Released(t, "P", V1, true, ref parked);
        bool r2 = Released(t, "P", V2, true, ref parked);
        Check(ref failures, !r1 && !r2 && parked,
              "(v4) a platform's parked Fire At is NOT released by vertex 1 or vertex 2 of 3");
        bool r3 = Released(t, "P", V3, true, ref parked);
        Check(ref failures, r3 && !parked, "(v4) ... it is released by the LAST vertex's completion");

        var tv = new VertexChainTracker(100.0);
        bool parkedV = true;
        tv.Start("P", "VRF_UUID:p", "attack-2", "taskee", "ATT2", Origin, new[] { V1 });
        Check(ref failures, !Released(tv, "P", Origin, true, ref parkedV) && parkedV,
              "(v4) a VACUOUS last vertex does not release it - the engage is not fired from wherever the unit stopped " +
              "(it waits for arrival evidence, or the engage fallback's stuck test)");

        var tf = new VertexChainTracker(100.0);
        bool parkedF = true;
        tf.Start("P", "VRF_UUID:p", "attack-3", "taskee", "ATT3", Origin, new[] { V1, V2 });
        Check(ref failures, !Released(tf, "P", Origin, false, ref parkedF) && parkedF && tf.Count == 0,
              "(v4) a FAILED vertex ends the chain and releases nothing (the failure path cancels the engage)");
    }

    // ------------------------------------------------------------------------------ (v5) ----
    private static void Setting(ref int failures)
    {
        var d = new VrfSettings();
        Check(ref failures, d.PlatformMoveToPerVertex && d.VertexArrivalRadiusMeters == 100.0,
              "(v5) Vrf:PlatformMoveToPerVertex initialises to TRUE and Vrf:VertexArrivalRadiusMeters to 100 - a run with " +
              "no configuration file gets the RL-20260927-01 default");
        string on = VertexChainPolicy.StartupLine(true, 100.0);
        string offLine = VertexChainPolicy.StartupLine(false, 100.0);
        Check(ref failures, on.StartsWith("MOVE TO PER VERTEX ON (", StringComparison.Ordinal) && on.Contains("RL-20260927-01")
                            && on.Contains("100 m") && on.Contains("Vrf__PlatformMoveToPerVertex=false"),
              "(v5) the ON start-up line names RL-20260927-01, the vacuous bar and how to turn it off");
        Check(ref failures, offLine.StartsWith("MOVE TO PER VERTEX off (", StringComparison.Ordinal) && offLine.Contains("RL-20260927-01"),
              "(v5) the OFF state is said too - a log without the ON line is not read as off");
        Check(ref failures, on.All(ch => ch < 128) && offLine.All(ch => ch < 128),
              "(v5) both lines are ASCII");

        string repo = FindRepoRoot();
        string appSettings = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
        string demoSettings = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.Demo.json");
        Check(ref failures, appSettings != null && File.Exists(appSettings) && File.Exists(demoSettings),
              $"(v5) both shipped settings files are on disk ({appSettings})");
        if (appSettings == null || !File.Exists(appSettings) || !File.Exists(demoSettings)) return;
        var shipped = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false)
                          .Build().GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        var demo = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false)
                       .AddJsonFile(demoSettings, optional: false)
                       .Build().GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        Check(ref failures, shipped.PlatformMoveToPerVertex && shipped.VertexArrivalRadiusMeters == 100.0
                            && demo.PlatformMoveToPerVertex,
              "(v5) appsettings.json and the Demo overlay SAY it: ON, bar 100 m - the default is written down, not only compiled in");
        Check(ref failures, File.ReadAllText(appSettings).Contains("\"PlatformMoveToPerVertex\": true")
                            && File.ReadAllText(demoSettings).Contains("\"PlatformMoveToPerVertex\": true"),
              "(v5) ... as an explicit key in both files");

        const string EnvKey = "Vrf__PlatformMoveToPerVertex";
        string saved = Environment.GetEnvironmentVariable(EnvKey);
        try
        {
            Environment.SetEnvironmentVariable(EnvKey, "false");
            var offByEnv = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false)
                               .AddJsonFile(demoSettings, optional: false).AddEnvironmentVariables()
                               .Build().GetSection("Vrf").Get<VrfSettings>();
            Check(ref failures, offByEnv != null && !offByEnv.PlatformMoveToPerVertex,
                  $"(v5) {EnvKey}=false turns it OFF over BOTH json files - the pre-2026-09-27 CreateRoute path for every mover");
            Environment.SetEnvironmentVariable(EnvKey, "true");
            var onByEnv = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false).AddEnvironmentVariables()
                              .Build().GetSection("Vrf").Get<VrfSettings>();
            Check(ref failures, onByEnv != null && onByEnv.PlatformMoveToPerVertex,
                  $"(v5) {EnvKey}=true leaves it on (the key is read at all, not passing by being ignored)");
        }
        finally { Environment.SetEnvironmentVariable(EnvKey, saved); }
    }

    // --------------------------------------------------------------------------- helpers ----
    /// <summary>"true" / "false" for the first `return true;` / `return false;` after a case label, "?" if none.</summary>
    private static string ReturnAfter(string s, string label)
    {
        int at = s.IndexOf(label, StringComparison.Ordinal);
        if (at < 0) return "?";
        int t = s.IndexOf("return true;", at, StringComparison.Ordinal);
        int f = s.IndexOf("return false;", at, StringComparison.Ordinal);
        if (t < 0 && f < 0) return "?";
        return f < 0 || (t >= 0 && t < f) ? "true" : "false";
    }

    private static string Between(string s, string from, string to)
    {
        int a = s.IndexOf(from, StringComparison.Ordinal);
        if (a < 0) return "";
        int b = s.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        return b < 0 ? s.Substring(a) : s.Substring(a, b - a);
    }

    private static int CountOf(string s, string needle)
    {
        int n = 0;
        for (int i = s.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = s.IndexOf(needle, i + 1, StringComparison.Ordinal)) n++;
        return n;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "data", "COA-STP1_Order.xml"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
