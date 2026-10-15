namespace VrfC2SimApp;

/// <summary>
/// `--navigate-gate-selftest [--disabled]` - OFFLINE proof of the bounded navigate gate (NavigateGate; D36, STP-884).
/// No bridge, no server, no network, no clock: the hold loop is replayed on a timeline with the SAME
/// NavigateGate.Step the service calls once a second, and PlannedMoveTracker is the real class.
///
/// FAIL-FIRST BY CONSTRUCTION. `--disabled` runs the SAME assertions with Vrf:NavigateGateMaxWaitSeconds = 0 - today's
/// build - and MUST fail: the D36 arms F and H (a navigate onto 13 members still moving) and STP-884 (a successor onto
/// a platform's engage in flight) are then dispatched at once, which is the shape that stopped the back end.
///
/// The timelines are D36's own (docs/experiments/D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md, the interface logs):
/// arm F re-tasked at +93.4 s with 4 of 17 ended; arm (ii) at +117.2 s with 10 of 17; arm (i) at +495.3 s with 7 of 17
/// and the other 10 NEVER ended.
/// </summary>
public static class NavigateGateSelfTest
{
    /// <summary>The hold loop, exactly as the service runs it: one Step a second until it is not Hold. Returns the
    /// release and the wall seconds held. pendingAt(t) = members still out t seconds into the hold; lostAt = when the
    /// back end is declared lost (NaN = never).</summary>
    public static (NavigateGate.Release Release, double Held) ReplayHold(
        Func<double, int> pendingAt, double maxWait, double lostAt = double.NaN, int stepSeconds = 1)
    {
        for (double t = 0; ; t += stepSeconds)
        {
            int pending = pendingAt(t);
            var work = NavigateGate.Classify(pending, null, null, "T-new");
            var r = NavigateGate.Step(work, t, maxWait, !double.IsNaN(lostAt) && t >= lostAt);
            if (r != NavigateGate.Release.Hold) return (r, t);
            if (t > 100000) return (NavigateGate.Release.Hold, t);   // never: the bound ends every hold
        }
    }

    public static int Run(bool featureEnabled = true)
    {
        int fails = 0;
        void Check(string what, bool cond)
        { Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what); if (!cond) fails++; }

        double maxWait = featureEnabled ? NavigateGate.DefaultMaxWaitSeconds : 0.0;
        Console.WriteLine(featureEnabled
            ? $"navigate-gate-selftest: {NavigateGate.SettingKey}={maxWait} (the gate ON)"
            : $"navigate-gate-selftest: --disabled - {NavigateGate.SettingKey}=0, TODAY'S BUILD. The hold assertions MUST " +
              "fail in this arm; that failure is the shape D36 arms F and H stopped the back end with.");

        // ===== 1. D36 ARM F: ENGAGE onto 48 IBCT with 13 of 17 members still moving (+93.4 s) =====
        {
            var work = NavigateGate.Classify(13, null, null, "PROBE_48IBCT_ENGAGE");
            bool moves = NavigateGate.TaskMoves(TaskIntent.Attack, hasGeometry: true);
            Check("F: 13 members out -> the work is MemberMovesOutstanding", work == NavigateGate.Work.MemberMovesOutstanding);
            Check("F: an ATTACK with geometry MOVES its unit", moves);
            Check("F: the ENGAGE is HELD instead of being dispatched onto 13 members still moving",
                  NavigateGate.ShouldHold(work, moves, maxWait, backendLost: false));
            // Members end over the next minutes (arm F's CONSTR: 4 ended by +93 s; the rest assumed to end by +300 s).
            var (rel, held) = ReplayHold(t => t < 207 ? 13 - (int)(t / 16) : 0, maxWait);
            Check("F: the hold ends CLEARED when the last member's navigate ends, and the task DISPATCHES",
                  rel == NavigateGate.Release.Cleared && NavigateGate.Dispatches(rel));
            Check("F: ... after the work ended, not at the bound", held < maxWait);
        }

        // ===== 2. D36 ARM H: OCCUPY (ROEHold) - same shape, same hold: the ROE plays no part =====
        {
            var work = NavigateGate.Classify(13, null, null, "PROBE_48IBCT_OCCUPY");
            Check("H: an OCCUPY with geometry onto 13 members still moving is HELD as well",
                  NavigateGate.ShouldHold(work, NavigateGate.TaskMoves(TaskIntent.HoldObjective, true), maxWait, false));
        }

        // ===== 3. D36 ARM (i): 10 members NEVER end - the bound dispatches the task (it ran on) =====
        {
            var (rel, held) = ReplayHold(_ => 10, maxWait);
            Check("(i): members that never end do not hold the task forever - it is released at the BOUND",
                  rel == NavigateGate.Release.Bound && Math.Abs(held - maxWait) < 1.5);
            Check("(i): the bound DISPATCHES (not an abort): a late re-task ran on in arm (i)", NavigateGate.Dispatches(rel));
            Check($"(i): the bound is {NavigateGate.DefaultMaxWaitSeconds:F0} s WALL - at arm (i)'s measured safe re-task " +
                  "(+495 s), above F/H's +93/+110 s", featureEnabled ? Math.Abs(maxWait - 480) < 1e-9 : maxWait == 0);
        }

        // ===== 4. The back end is declared LOST mid-hold: the ordinary path, which refuses =====
        {
            var (rel, held) = ReplayHold(_ => 13, maxWait, lostAt: 30);
            Check("LOST: a loss mid-hold ends it at once and the task is NOT dispatched (the service abandons it)",
                  rel == NavigateGate.Release.BackendLost && !NavigateGate.Dispatches(rel) && held <= 31);
            Check("LOST: a back end already lost is never waited on",
                  !NavigateGate.ShouldHold(NavigateGate.Work.MemberMovesOutstanding, true, maxWait, backendLost: true));
        }

        // ===== 5. Tasks that do not move are never held (they issue no movement) =====
        {
            var w = NavigateGate.Work.MemberMovesOutstanding;
            Check("IN PLACE: a HoldInPlace intent is not held",
                  !NavigateGate.ShouldHold(w, NavigateGate.TaskMoves(TaskIntent.HoldInPlace, true), maxWait, false));
            Check("IN PLACE: a passage of lines (held in place, RL-20260926-01 A6) is not held",
                  !NavigateGate.ShouldHold(w, NavigateGate.TaskMoves(TaskIntent.PassageOfLines, true), maxWait, false));
            Check("IN PLACE: a task with NO geometry (R2, in place) is not held",
                  !NavigateGate.ShouldHold(w, NavigateGate.TaskMoves(TaskIntent.Move, false), maxWait, false));
            Check("IDLE: a moving task onto a unit with nothing in flight dispatches at once",
                  !NavigateGate.ShouldHold(NavigateGate.Classify(0, null, null, "T"), true, maxWait, false));
        }

        // ===== 6. STP-884: a successor released while a PLATFORM's deferred engage is in flight =====
        {
            var w = NavigateGate.Classify(0, NavigateGate.EngageKind, "T-attack", "T-successor");
            Check("STP-884: a platform whose in-flight record is the ATTACK's engage -> EngageInFlight",
                  w == NavigateGate.Work.EngageInFlight);
            Check("STP-884: a MOVING successor is HELD instead of superseding the engage",
                  NavigateGate.ShouldHold(w, NavigateGate.TaskMoves(TaskIntent.Move, true), maxWait, false));
            Check("STP-884: a task never waits on its OWN engage",
                  NavigateGate.Classify(0, NavigateGate.EngageKind, "T-attack", "T-attack") == NavigateGate.Work.None);
            Check("PLATFORM MOVE: a platform busy with a plain move is NOT held (a new order supersedes it, as today)",
                  NavigateGate.Classify(0, "navigate-to-location", "T-old", "T-new") == NavigateGate.Work.None);
            Check("STP-884: in-place successors are still not held (SF4's hold refusal is a separate matter)",
                  !NavigateGate.ShouldHold(w, NavigateGate.TaskMoves(TaskIntent.HoldInPlace, true), maxWait, false));
        }

        // ===== 7. The tracker reading the gate rests on (the real PlannedMoveTracker) =====
        {
            var tr = new PlannedMoveTracker();
            var members = Enumerable.Range(1, 17).Select(i => new PlannedMoveTracker.Member($"48_IBCT.M{i}", $"u{i}")).ToList();
            tr.Begin(new PlannedMoveTracker.Context("48_IBCT/28ID", "c", 7, AggregateMovePlanner.Auto, members,
                                                    Array.Empty<RoadDecision>(), 1));
            Check("TRACKER: a context with no step open yet (the first vertex is about to go) -> all 17 BUSY",
                  tr.PendingMembers("48_IBCT/28ID", out long gen0) == 17 && gen0 == 7);
            tr.BeginVertex("48_IBCT/28ID", 7, 1);
            Check("TRACKER: a fresh step -> all 17 pending", tr.PendingMembers("48_IBCT/28ID", out _) == 17);
            for (int i = 1; i <= 4; i++) tr.OnMemberCompleted("48_IBCT/28ID", $"48_IBCT.M{i}", AggregateMovePolicy.NavigateScript, true);
            Check("TRACKER: arm F at the re-task - 4 ended -> 13 pending", tr.PendingMembers("48_IBCT/28ID", out _) == 13);
            tr.OnMemberCompleted("48_IBCT/28ID", "48_IBCT.M5", "move-to", true);
            Check("TRACKER: a completion that is not the step's navigate does not count", tr.PendingMembers("48_IBCT/28ID", out _) == 13);
            for (int i = 5; i <= 17; i++) tr.OnMemberCompleted("48_IBCT/28ID", $"48_IBCT.M{i}", AggregateMovePolicy.NavigateScript, i % 2 == 0);
            Check("TRACKER: the step closed but the context lives (BETWEEN VERTICES: the next one is queued) -> all 17 " +
                  "BUSY, never idle (cold review of ada1bf0)", tr.PendingMembers("48_IBCT/28ID", out _) == 17);
            tr.End("48_IBCT/28ID");
            Check("TRACKER: the planned move ENDED (last vertex completed / failed) -> 0", tr.PendingMembers("48_IBCT/28ID", out _) == 0);
            var g = new PlannedMoveTracker();
            g.Begin(new PlannedMoveTracker.Context("X", "x", 1, AggregateMovePlanner.Group, members, Array.Empty<RoadDecision>(), 1));
            g.BeginVertex("X", 1, 1);
            Check("TRACKER: a Group planner reports 0 (the vendor aggregates its members; no per-member step)",
                  g.PendingMembers("X", out _) == 0);
            Check("TRACKER: an unknown container -> 0", tr.PendingMembers("nobody", out _) == 0 && tr.PendingMembers(null, out _) == 0);
        }

        // ===== 8. The sentences =====
        {
            string hold = NavigateGate.HoldLine("PROBE_48IBCT_ENGAGE", "48_IBCT/28ID", NavigateGate.Work.MemberMovesOutstanding, 13, 480);
            Check("LINES: the hold line names the prefix, the members out, the bound and the setting",
                  hold.StartsWith(NavigateGate.Prefix, StringComparison.Ordinal) && hold.Contains("13 member(s)")
                  && hold.Contains("480 s") && hold.Contains(NavigateGate.SettingKey));
            Check("LINES: the bound line says the task is DISPATCHED, not aborted",
                  NavigateGate.BoundLine("T", "U", NavigateGate.Work.MemberMovesOutstanding, 10, 480).Contains("DISPATCHING ANYWAY"));
            Check("LINES: the startup line says OFF at 0 and ON otherwise",
                  NavigateGate.StartupLine(0).Contains(" OFF ") && NavigateGate.StartupLine(480).Contains(" ON "));
            Check("LINES: every sentence is ASCII",
                  new[] { hold, NavigateGate.ClearedLine("T", "U", 12.3), NavigateGate.BackendLostLine("T", "U", 4),
                          NavigateGate.StartupLine(480) }.All(s => s.All(c => c < 128)));
        }

        Console.WriteLine(fails == 0 ? "navigate-gate-selftest: ALL CHECKS PASSED"
                                     : $"navigate-gate-selftest: {fails} CHECK(S) FAILED");
        return fails == 0 ? 0 : 1;
    }
}
