namespace VrfC2SimApp;

/// <summary>
/// ONE ANCHOR FOR THE END-TIME TIMER AND THE STREND GATE (2026-09-27; RL-20260921-09, RL-20260925-01).
/// Its own section of `--rulings-selftest`: no bridge, no MAK; a hand-driven task-clock axis, the REAL
/// TaskSequencer, the REAL TimedCompletionPolicy and the REAL TaskDispatchPolicy window.
///
/// THE DEFECT (run IRONSTORM_CUTA_E2-2026-09-27-1, docs/experiments/PREREG_IRONSTORM_CUTA_E2_2026-09-27.md
/// Result: C187 against C189, app log L19615-L19621 against L21569). MarkDispatched stamped the successor
/// gate with the task-clock reading at dispatch (TaskSequencer.NotifyDispatched), but
/// TimedCompletionPolicy.Register stored no clock and the timer anchored on its FIRST walk after arming,
/// serving nothing for the time in between. E2's order met a scenario clock of 5.6 s running ~14x; the
/// first walk came >= 45 SIM s after the stamp, so the gate (stamp + 300 + 60) expired at 360 while the
/// holds had served 315 of 300 - both STREND successors SKIPPED 0.8 WALL s before the holds completed.
///
///   (t1) THE E2 REPLAY, FAIL-FIRST: the pre-fix anchoring skips at stamp + 360 and completes afterwards
///        with 315 served; the fix completes the hold at stamp + 300 and the gate PROCEEDS - never a skip.
///   (t2) THE ANCHOR ITSELF: Register's dispatch reading is served from; first-dispatch-wins keeps it; a
///        pause and a rollback add nothing; a clock-mode change still re-anchors.
///   (t3) THE RULED SEMANTICS, UNCHANGED BY THE ANCHOR (RL-20260921-09, as built under RL-20260925-01): an
///        early finish is held to start + Duration; a late mover is OVERDUE at start + Duration, its
///        follow-on WAITS past the margin and is released by the arrival's TASKCMPLT.
///   (t4) THE RL-20260925-01 Q1 FINDING, characterised and NOT changed (the owner's call): a task-clock step
///        larger than the margin across a late mover's end time, seen by the gate before the timed walk,
///        still SKIPS its follow-on although the mover later arrives.
///   (t5) SOURCE TRIPWIRES: MarkDispatched reads the axis ONCE and hands the same value to the gate and the
///        timer.
/// </summary>
public static class TimerAnchorSelfTest
{
    // E2's numbers (the Result block and sec DEVIATION FROM RECORD): the holds were stamped at SIMULATION
    // 5.6 s (L676, L704); X = 5.5 here keeps every reading binary-exact, so "due at X+300" is arithmetic,
    // not a rounding. Duration 1200 s x Vrf:DurationScale 0.25 = 300 s; Vrf:TaskPredecessorTimeoutSeconds
    // 60 (the run's floor) and the shipped 60 s end margin: the window max(60, 300 + 60) = 360 of L19615.
    private const double X = 5.5;
    private const double Duration = 300.0;
    private const double ConfiguredFloor = 60.0;
    private const double Margin = TaskDispatchPolicy.DefaultPredecessorEndMarginSeconds;
    private const double Backstop = TaskDispatchPolicy.DefaultChainBackstopSeconds;
    private const double Step = 15.0;   // one timed walk per ~1 WALL s at E2's 14.3x

    public static int Run()
    {
        int failures = 0;
        double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(ConfiguredFloor, Duration, Margin);
        Check(ref failures, window == 360.0,
              $"(t0) E2's successor window from the real policy: max({ConfiguredFloor:F0}, {Duration:F0} + {Margin:F0}) = " +
              $"{window:F0} s - the \"within 360s of its dispatch\" of L19615");

        // ------------------------------------------------------------------------------ (t1) ----
        var before = ReplayE2(anchorAtDispatch: false, window);
        Check(ref failures, before.Gate == GateResult.PredecessorTimeout && !before.CompletedBeforeGate
                            && before.CompletedAtClock == X + 360.0 && before.ServedAtCompletion == 315.0,
              "FAIL-FIRST (t1): the PRE-FIX anchoring (the timer counts from its first walk, 45 SIM s after the " +
              $"stamp) SKIPS the successor at stamp + 360 ({Show(before.Gate)}) and the hold completes only " +
              $"afterwards, \"{before.ServedAtCompletion:F0} s of a {Duration:F0} s Duration served\" at stamp + " +
              $"{before.CompletedAtClock - X:F0} - E2's L19619 and L21569, reproduced");
        var after = ReplayE2(anchorAtDispatch: true, window);
        Check(ref failures, after.Gate == GateResult.Proceed && after.CompletedBeforeGate
                            && after.CompletedAtClock == X + Duration && after.ServedAtCompletion == Duration,
              $"(t1) THE FIX (Register takes the dispatch stamp), same walks: the hold completes at stamp + " +
              $"{after.CompletedAtClock - X:F0} with {after.ServedAtCompletion:F0} served, and the gate PROCEEDS " +
              $"({Show(after.Gate)}) - it never skips, even when it later sees stamp + 360");

        // ------------------------------------------------------------------------------ (t2) ----
        {
            var t = new TimedCompletionPolicy();
            t.Register("A", "tk", "Hold", "U", Duration, dispatchClock: X);
            bool none45 = t.Advance(X + 45.0, usingSim: true).Count == 0;
            bool none299 = t.Advance(X + 299.5, usingSim: true).Count == 0;
            var due = t.Advance(X + 300.0, usingSim: true);
            Check(ref failures, none45 && none299 && due.Count == 1 && due[0].Elapsed == 300.0,
                  "(t2) Register(dispatchClock: X): the first walk at X+45 SERVES 45 (not 0) and the task is due at " +
                  "exactly X+300 (not at X+299.5)");
            var old = new TimedCompletionPolicy();
            old.Register("A", "tk", "Hold", "U", Duration);
            bool oldAnchor = old.Advance(X + 45.0, usingSim: true).Count == 0;
            bool oldNotYet = old.Advance(X + 300.0, usingSim: true).Count == 0;
            bool oldDue = old.Advance(X + 345.0, usingSim: true).Count == 1;
            Check(ref failures, oldAnchor && oldNotYet && oldDue,
                  "(t2) ... while a Register with NO dispatch reading keeps the first-sighting anchor: due at X+345 " +
                  "(the pre-fix arithmetic, kept for a caller that has no reading; the service always has one)");
            var fw = new TimedCompletionPolicy();
            bool first = fw.Register("B", "tk", "Hold", "U", Duration, dispatchClock: X);
            bool second = fw.Register("B", "tk", "Hold", "U", Duration, dispatchClock: X + 100.0);
            bool fwDue = fw.Advance(X + 300.0, usingSim: true).Count == 1;
            Check(ref failures, first && !second && fwDue,
                  "(t2) FIRST DISPATCH WINS: a re-entered dispatch (anchor X+100) is refused and the first anchor " +
                  "stands - due at X+300, not X+400");
            var pr = new TimedCompletionPolicy();
            pr.Register("C", "tk", "Hold", "U", Duration, dispatchClock: X);
            pr.Advance(X + 100.0, usingSim: true);                       // 100 served
            pr.Advance(X + 100.0, usingSim: true);                       // a paused scenario: + 0
            pr.Advance(X + 40.0, usingSim: true);                        // a rollback: + 0, re-based at X+40
            bool notYet = pr.Advance(X + 239.0, usingSim: true).Count == 0;   // + 199 = 299
            bool nowDue = pr.Advance(X + 240.0, usingSim: true).Count == 1;   // + 1 = 300
            Check(ref failures, notYet && nowDue,
                  "(t2) from the dispatch anchor a pause adds nothing and a rollback adds nothing (100 + 0 + 0, then " +
                  "199 more = 299 not due, 1 more = 300 due)");
            var mc = new TimedCompletionPolicy();
            mc.Register("D", "tk", "Hold", "U", Duration, dispatchClock: X, dispatchClockUsingSim: true);
            bool reAnchor = mc.Advance(1_000.0, usingSim: false).Count == 0;
            bool stillNot = mc.Advance(1_299.0, usingSim: false).Count == 0;
            bool modeDue = mc.Advance(1_300.0, usingSim: false).Count == 1;
            Check(ref failures, reAnchor && stillNot && modeDue,
                  "(t2) a CLOCK-MODE CHANGE still re-anchors and serves nothing across it (unchanged)");
        }

        // ------------------------------------------------------------------------------ (t3) ----
        {
            var t = new TimedCompletionPolicy();
            t.Register("E", "tk", "Move", "U", Duration, hasDestination: true, dispatchClock: X);
            t.Advance(X + 45.0, usingSim: true);
            var verdict = t.MarkFinished("E");
            bool heldAt299 = t.Advance(X + 299.0, usingSim: true).Count == 0;
            var at300 = t.Advance(X + 300.0, usingSim: true);
            Check(ref failures, verdict == TimedCompletionPolicy.FinishVerdict.Hold && heldAt299 && at300.Count == 1
                                && at300[0].Kind == TimedCompletionPolicy.DueKind.CompleteNow,
                  "(t3) RL-20260921-09: an arrival at X+45 is HELD and reported at start + Duration = X+300, not before");
        }
        {
            var late = ReplayLateMover(stepAcrossEndTime: false, window);
            Check(ref failures, late.OverdueAtClock == X + Duration && late.Gate == GateResult.Proceed
                                && late.ArrivalVerdict == TimedCompletionPolicy.FinishVerdict.EmitNow,
                  $"(t3) a LATE MOVER is OVERDUE at start + Duration (X+{late.OverdueAtClock - X:F0}), its follow-on " +
                  "WAITS past the 360 s window, and the arrival at X+500 reports TASKCMPLT at once and releases it " +
                  $"({Show(late.Gate)}) - RL-20260925-01 Q1");
        }

        // ------------------------------------------------------------------------------ (t4) ----
        {
            var race = ReplayLateMover(stepAcrossEndTime: true, window);
            Check(ref failures, race.Gate == GateResult.PredecessorTimeout
                                && race.ArrivalVerdict == TimedCompletionPolicy.FinishVerdict.EmitNow,
                  "FINDING (t4), RL-20260925-01 Q1, characterised and NOT changed (the owner's call): ONE task-clock " +
                  "step X+285 -> X+360 (75 SIM s: ~5 WALL s without a timed walk at 15x) that the gate sees before " +
                  $"the walk SKIPS the follow-on ({Show(race.Gate)}) although the mover then ARRIVES and reports " +
                  "TASKCMPLT - \"follow-ons wait for a late predecessor instead of being skipped 60 s after its end " +
                  "time\" holds only while the OVERDUE signal reaches the gate inside the margin");
        }

        // ------------------------------------------------------------------------------ (t5) ----
        string repo = FindRepoRoot();
        string service = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs");
        bool onDisk = service != null && File.Exists(service);
        Check(ref failures, onDisk, $"(t5) the service source is on disk ({service ?? "repo root NOT FOUND"})");
        if (!onDisk) return failures;
        string src = File.ReadAllText(service);
        string mark = Between(src, "private void MarkDispatched(",
                              "// ============ MOVE TO PER VERTEX FOR A LONE GROUND PLATFORM");
        int read = mark.IndexOf("double dispatchClock = TaskClockSeconds;", StringComparison.Ordinal);
        int stamp = mark.IndexOf("_sequencer.NotifyDispatched(task.TaskUuid, dispatchClock);", StringComparison.Ordinal);
        int arm = mark.IndexOf("dispatchClock: dispatchClock, dispatchClockUsingSim: true", StringComparison.Ordinal);
        Check(ref failures, read > 0 && stamp > read && arm > stamp && CountOf(mark, "TaskClockSeconds") == 1,
              "(t5) MarkDispatched reads the task-clock axis ONCE and hands the SAME value to the gate's stamp and " +
              "the timer's anchor (read, stamp, arm in that order; one TaskClockSeconds in the method)");
        Check(ref failures, !src.Contains("NotifyDispatched(task.TaskUuid, TaskClockSeconds)"),
              "(t5) no dispatch stamp is taken from a second read of the axis");
        Check(ref failures, src.Contains("foreach (var p in _timed.Advance(clockNow, usingSim: true))"),
              "(t5) the timed walk advances with usingSim: true - the mode the anchor is registered in");
        return failures;
    }

    // ----------------------------------------------------------------------------------------------------
    private readonly record struct E2Outcome(GateResult? Gate, bool CompletedBeforeGate, double CompletedAtClock,
                                             double ServedAtCompletion);

    /// <summary>
    /// E2, on the service's own sequence: MarkDispatched (the gate's stamp, then Register), then the timed walk
    /// that feeds TaskSequencer.CompleteTask (MaybeCompleteTimedTasks). The first walk comes 45 SIM s after the
    /// stamp (E2's ">= 45"), then one every 15 SIM s up to X+330; the next axis reading, X+360, is seen by the
    /// gate's poller BEFORE the tick thread's next walk (E2: C187, 0.8 WALL s before C189). Both arms run the
    /// SAME walks; only Register differs.
    /// </summary>
    private static E2Outcome ReplayE2(bool anchorAtDispatch, double window)
    {
        var clock = new AxisClock(X);
        var seq = new TaskSequencer();
        var timed = new TimedCompletionPolicy();
        const string pred = "T13-HOLD";
        double reading = clock.Now;                               // MarkDispatched's one read
        seq.NotifyDispatched(pred, reading);
        if (anchorAtDispatch)
            timed.Register(pred, "taskee", "T13", "48_IBCT", Duration, hasDestination: false, dispatchClock: reading);
        else
            timed.Register(pred, "taskee", "T13", "48_IBCT", Duration, hasDestination: false);
        var gate = seq.WaitForStartAsync(pred, 0, 0, window, clock.AsTaskClock(), CancellationToken.None,
                                         dispatchTimeoutSeconds: Backstop, overdueBackstopSeconds: Backstop);

        double completedAt = double.NaN, served = double.NaN;
        bool completedBeforeGate = false;
        void Walk()
        {
            foreach (var d in timed.Advance(clock.Now, usingSim: true))
            {
                if (d.Kind != TimedCompletionPolicy.DueKind.CompleteNow) continue;
                completedAt = clock.Now;
                served = d.Elapsed;
                completedBeforeGate = !gate.IsCompleted;
                seq.CompleteTask(d.TaskUuid);                     // what MaybeCompleteTimedTasks does
            }
        }
        for (int k = 3; k <= 22 && double.IsNaN(completedAt); k++)   // X+45 (the first walk) .. X+330
        {
            clock.Set(X + Step * k);
            Walk();
            Settle(gate);
        }
        clock.Set(X + 360.0);                                     // the gate's poller sees it first
        gate.Wait(TimeSpan.FromSeconds(2));
        Walk();                                                   // then the tick thread's walk
        GateResult? result = gate.Wait(TimeSpan.FromSeconds(5)) ? gate.Result : null;
        return new E2Outcome(result, completedBeforeGate, completedAt, served);
    }

    private readonly record struct LateOutcome(GateResult? Gate, double OverdueAtClock,
                                               TimedCompletionPolicy.FinishVerdict ArrivalVerdict);

    /// <summary>A MOVER predecessor (it has a destination) whose unit ARRIVES at X+500, after its end time.
    /// stepAcrossEndTime=false: a walk every 15 SIM s to X+480. true: a walk every 15 SIM s to X+285, then ONE
    /// axis step to X+360 that the gate sees before the next walk (the (t4) race).</summary>
    private static LateOutcome ReplayLateMover(bool stepAcrossEndTime, double window)
    {
        var clock = new AxisClock(X);
        var seq = new TaskSequencer();
        var timed = new TimedCompletionPolicy();
        const string pred = "T10-MOVE";
        double reading = clock.Now;
        seq.NotifyDispatched(pred, reading);
        timed.Register(pred, "taskee", "T10", "1-112_IN", Duration, hasDestination: true, dispatchClock: reading);
        var gate = seq.WaitForStartAsync(pred, 0, 0, window, clock.AsTaskClock(), CancellationToken.None,
                                         dispatchTimeoutSeconds: Backstop, overdueBackstopSeconds: Backstop);

        double overdueAt = double.NaN;
        void Walk()
        {
            foreach (var d in timed.Advance(clock.Now, usingSim: true))
            {
                if (d.Kind == TimedCompletionPolicy.DueKind.OverdueAwaitingArrival)
                {
                    overdueAt = clock.Now;
                    seq.NotifyOverdue(d.TaskUuid);                // what MaybeCompleteTimedTasks does
                }
                else seq.CompleteTask(d.TaskUuid);
            }
        }
        int lastK = stepAcrossEndTime ? 19 : 32;                  // X+285 or X+480
        for (int k = 1; k <= lastK && !gate.IsCompleted; k++)
        {
            clock.Set(X + Step * k);
            Walk();
            Settle(gate);
        }
        if (stepAcrossEndTime)
        {
            clock.Set(X + 360.0);                                 // the gate's poller sees it first
            gate.Wait(TimeSpan.FromSeconds(2));
            Walk();                                               // OVERDUE, one step too late
        }
        clock.Set(X + 500.0);                                     // the unit ARRIVES (arrival evidence)
        Walk();
        var verdict = timed.MarkFinished(pred);
        if (verdict == TimedCompletionPolicy.FinishVerdict.EmitNow) seq.CompleteTask(pred);
        GateResult? result = gate.Wait(TimeSpan.FromSeconds(5)) ? gate.Result : null;
        return new LateOutcome(result, overdueAt, verdict);
    }

    /// <summary>Let the gate's poller look at the axis before the next step.</summary>
    private static void Settle(Task gate)
    {
        if (!gate.IsCompleted) Thread.Sleep(AxisClock.PollMs * 3);
    }

    private static string Show(GateResult? r) => r?.ToString() ?? "the gate never returned";

    /// <summary>A task-clock axis the test drives by hand - monotone seconds, as the service's TaskClockAxis
    /// is - with the service's own delay shape (TaskClockDelayAsync: poll the axis).</summary>
    private sealed class AxisClock
    {
        public const int PollMs = 5;
        private readonly object _lock = new();
        private double _seconds;
        public AxisClock(double start) => _seconds = start;
        public double Now { get { lock (_lock) return _seconds; } }
        public void Set(double seconds) { lock (_lock) _seconds = Math.Max(_seconds, seconds); }
        public TaskClock AsTaskClock() => new(() => Now, DelayAsync);
        private async Task DelayAsync(double seconds, CancellationToken ct)
        {
            if (!(seconds > 0.0)) return;
            double start = Now;
            while (Now - start < seconds)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(PollMs, ct).ConfigureAwait(false);
            }
        }
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
        for (int i = s.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = s.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            n++;
        return n;
    }

    private static string FindRepoRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "data", "COA-STP1_Order.xml"))) return d.FullName;
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "data", "COA-STP1_Order.xml"))) return d.FullName;
        return null;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
