using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// ONE ANCHOR FOR THE END-TIME TIMER AND THE STREND GATE (2026-09-27; RL-20260921-09, RL-20260925-01), AND THE
/// GATE THAT WAITS FOR A MOVER WITHOUT A RACE (RL-20260927-05). Its own section of `--rulings-selftest`: no
/// bridge, no MAK; a hand-driven task-clock axis, the REAL TaskSequencer, the REAL TimedCompletionPolicy and the
/// REAL TaskDispatchPolicy window.
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
///   (t4) RL-20260927-05, FAIL-FIRST (the RL-20260925-01 Q1 finding, answered "Q1 a"): one task-clock step
///        larger than the margin across a mover's end time, seen by the gate before the timed walk, SKIPPED
///        its follow-on although the mover then arrived. The gate now ASKS the timer: a predecessor with a
///        destination and no TASKCMPLT/TASKABRT is treated as OVERDUE, so the late mover's follow-on waits for
///        the arrival and the early-arrived (held) mover's for its end time.
///   (t5) SOURCE TRIPWIRES: MarkDispatched reads the axis ONCE and hands the same value to the gate and the
///        timer; the gate call asks TimedCompletionPolicy.IsUnfinishedMover; the no-Duration warning is true.
///   (t6) WHAT RL-20260927-05 DOES NOT CHANGE, pinned: a HOLD whose timer never fires is still skipped at its
///        window; a mover with NO Duration is still skipped at the floor; a STUCK unit's follow-ons are still
///        abandoned at once (RL-20260925-01 Q2), also mid-extension; the chain backstop bounds the wait; the
///        question's truth table; a question that throws is the old skip, never a faulted gate.
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
    private const double ShippedFloor = 600.0;   // appsettings.json Vrf:TaskPredecessorTimeoutSeconds
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
                            && before.CompletedAt == X + 360.0 && before.Served == 315.0,
              "FAIL-FIRST (t1): the PRE-FIX anchoring (the timer counts from its first walk, 45 SIM s after the " +
              $"stamp) SKIPS the successor at stamp + 360 ({Show(before.Gate)}) and the hold completes only " +
              $"afterwards, \"{before.Served:F0} s of a {Duration:F0} s Duration served\" at stamp + " +
              $"{before.CompletedAt - X:F0} - E2's L19619 and L21569, reproduced");
        var after = ReplayE2(anchorAtDispatch: true, window);
        Check(ref failures, after.Gate == GateResult.Proceed && after.CompletedBeforeGate
                            && after.CompletedAt == X + Duration && after.Served == Duration,
              $"(t1) THE FIX (Register takes the dispatch stamp), same walks: the hold completes at stamp + " +
              $"{after.CompletedAt - X:F0} with {after.Served:F0} served, and the gate PROCEEDS " +
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
            // The OVERDUE FLAG alone (no question asked): the path as built on 2026-09-25.
            var late = LateMover(stepAcrossEndTime: false, Ask.None, window);
            Check(ref failures, late.OverdueAt == X + Duration && late.Gate == GateResult.Proceed
                                && late.Arrival == TimedCompletionPolicy.FinishVerdict.EmitNow,
                  $"(t3) a LATE MOVER is OVERDUE at start + Duration (X+{late.OverdueAt - X:F0}), its follow-on " +
                  "WAITS past the 360 s window, and the arrival at X+500 reports TASKCMPLT at once and releases it " +
                  $"({Show(late.Gate)}) - RL-20260925-01 Q1");
        }

        // ------------------------------------------------------------------------------ (t4) ----
        {
            var oldRace = LateMover(stepAcrossEndTime: true, Ask.None, window);
            Check(ref failures, oldRace.Gate == GateResult.PredecessorTimeout
                                && oldRace.Arrival == TimedCompletionPolicy.FinishVerdict.EmitNow,
                  "FAIL-FIRST (t4): WITHOUT the question (the gate before RL-20260927-05) ONE task-clock step " +
                  "X+285 -> X+360 that the gate sees before the timed walk SKIPS the follow-on " +
                  $"({Show(oldRace.Gate)}) although the mover then ARRIVES and reports TASKCMPLT - the RL-20260925-01 " +
                  "Q1 finding, reproduced");
            var newRace = LateMover(stepAcrossEndTime: true, Ask.Service, window);
            Check(ref failures, newRace.Gate == GateResult.Proceed && newRace.WaitingAtArrival && newRace.Asked
                                && newRace.Arrival == TimedCompletionPolicy.FinishVerdict.EmitNow,
                  "(t4) RL-20260927-05: WITH the question the same step finds a mover with no TASKCMPLT/TASKABRT, " +
                  "treats it as OVERDUE and keeps waiting past the window; the arrival at X+500 reports TASKCMPLT and " +
                  $"releases the follow-on ({Show(newRace.Gate)})");
            var oldHeld = HeldMover(Ask.None, window);
            Check(ref failures, oldHeld.Gate == GateResult.PredecessorTimeout && oldHeld.Arrival ==
                                TimedCompletionPolicy.FinishVerdict.Hold && oldHeld.CompletedAt == X + 360.0
                                && !oldHeld.CompletedBeforeGate,
                  "FAIL-FIRST (t4): a mover that ARRIVED EARLY (X+100, its TASKCMPLT held for its end time) - the same " +
                  $"step SKIPS its follow-on without the question ({Show(oldHeld.Gate)}), and its TASKCMPLT goes out " +
                  "at X+360 behind the skip");
            var newHeld = HeldMover(Ask.Service, window);
            Check(ref failures, newHeld.Gate == GateResult.Proceed && newHeld.CompletedBeforeGate && newHeld.Asked
                                && newHeld.CompletedAt == X + 360.0,
                  "(t4) RL-20260927-05: WITH the question the held mover (a destination, no TASKCMPLT yet) is waited " +
                  $"for, and the timed walk's TASKCMPLT at X+360 releases the follow-on ({Show(newHeld.Gate)})");
        }

        // ------------------------------------------------------------------------------ (t6) ----
        {
            var hold = HoldTimerNeverFires(window);
            Check(ref failures, hold.WaitingBefore && hold.Gate == GateResult.PredecessorTimeout && hold.Asked
                                && hold.TimerStillArmed,
                  "(t6) UNCHANGED: a HOLD (no destination) whose timer never fires is still SKIPPED at its window, " +
                  $"X+360 ({Show(hold.Gate)}), with the question asked and answered NO - a hold ends by its timer, so " +
                  "this skip means the timer had not completed it by end + margin");
            double floorWindow = TaskDispatchPolicy.PredecessorTimeoutSeconds(ShippedFloor, 0.0, Margin);
            var noDur = NoDurationMover(floorWindow);
            Check(ref failures, floorWindow == ShippedFloor && noDur.WaitingBefore
                                && noDur.Gate == GateResult.PredecessorTimeout && noDur.Asked
                                && noDur.Arrival == TimedCompletionPolicy.FinishVerdict.NotTimed,
                  "(t6) UNCHANGED: a mover with NO Duration has no timer and is never OVERDUE - its follow-on is still " +
                  $"SKIPPED at the configured floor, X+{floorWindow:F0} ({Show(noDur.Gate)}), although the unit " +
                  "arrives at X+700 (RL-20260927-05 leaves this case; the dispatch warning now says so)");
            var stuckEarly = Stuck(duringExtension: false, window);
            Check(ref failures, stuckEarly.Gate == GateResult.PredecessorAbandoned && stuckEarly.WaitingBefore
                                && !stuckEarly.Asked,
                  "(t6) UNCHANGED: a mover reported STUCK at X+200 - report-only TASKABRT, follow-ons ABANDONED " +
                  $"(RL-20260925-01 Q2) - fails the gate at once ({Show(stuckEarly.Gate)}), before its window, " +
                  "without the question ever being asked");
            var stuckLate = Stuck(duringExtension: true, window);
            Check(ref failures, stuckLate.Gate == GateResult.PredecessorAbandoned && stuckLate.WaitingBefore
                                && stuckLate.Asked && stuckLate.TimerStillArmed,
                  "(t6) THE QUESTION DOES NOT MASK A STUCK UNIT: a mover the gate is waiting for past its window is " +
                  "reported STUCK at X+400 - its report-only abort leaves the timer armed (it would still answer YES) " +
                  $"- and the ABANDON ends the wait at once ({Show(stuckLate.Gate)})");
            const double shortBackstop = 1200.0;
            var never = NeverArrives(shortBackstop, window);
            Check(ref failures, never.WaitingBefore && never.Asked && never.Gate == GateResult.PredecessorTimeout,
                  "(t6) THE BACKSTOP BOUNDS IT: a mover that never arrives and is never reported stuck is waited for " +
                  $"past its window - still waiting at X+{shortBackstop - 60.0:F0} - and SKIPPED at X+{shortBackstop:F0} " +
                  $"= its dispatch + the chain backstop ({shortBackstop:F0} s here, " +
                  $"Vrf:TaskChainBackstopSeconds {Backstop:F0} s shipped) ({Show(never.Gate)})");
            var thrower = ThrowingQuestion(window);
            Check(ref failures, thrower.Gate == GateResult.PredecessorTimeout && thrower.Asked && !thrower.Faulted,
                  "(t6) a question that THROWS counts as NO - the pre-RL-20260927-05 skip " +
                  $"({Show(thrower.Gate)}) - and never faults the gate");
        }
        {
            var p = new TimedCompletionPolicy();
            bool unknown = !p.IsUnfinishedMover("nope", out _);
            p.Register("H", "tk", "Hold", "U", Duration, hasDestination: false, dispatchClock: X);
            bool hold = !p.IsUnfinishedMover("H", out _);
            p.Register("M", "tk", "Move", "U", Duration, hasDestination: true, dispatchClock: X);
            bool mover = p.IsUnfinishedMover("M", out bool held0) && !held0;
            p.MarkFinished("M");                                          // arrived early: Hold
            bool heldMover = p.IsUnfinishedMover("M", out bool held1) && held1;
            p.Register("D", "tk", "Move", "U", Duration, hasDestination: true, dispatchClock: X);
            p.DropDestination("D");                                       // the engage fallback stopped the move
            bool dropped = !p.IsUnfinishedMover("D", out _);
            p.Register("C", "tk", "Move", "U", Duration, hasDestination: true, dispatchClock: X);
            if (TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT, reportOnlyAbort: false))
                p.Cancel("C");                                            // PushTaskStatus on a terminal TASKABRT
            bool aborted = !p.IsUnfinishedMover("C", out _);
            p.Register("K", "tk", "Move", "U", Duration, hasDestination: true, dispatchClock: X);
            if (TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT, reportOnlyAbort: true))
                p.Cancel("K");                                            // the stall watchdog's report-only abort
            bool stuckStays = p.IsUnfinishedMover("K", out _);
            p.Advance(X + 300.0, usingSim: true);                         // H, M, D complete; K goes OVERDUE
            bool completedGone = !p.IsUnfinishedMover("H", out _) && !p.IsUnfinishedMover("M", out _);
            bool overdueStays = p.IsUnfinishedMover("K", out _);
            var arrival = p.MarkFinished("K");                            // the late unit arrives: EmitNow
            bool emittedGone = arrival == TimedCompletionPolicy.FinishVerdict.EmitNow && !p.IsUnfinishedMover("K", out _);
            Check(ref failures, unknown && hold && mover && heldMover && dropped && aborted && stuckStays
                                && completedGone && overdueStays && emittedGone,
                  "(t6) TimedCompletionPolicy.IsUnfinishedMover: NO for an unknown task, a hold, a dropped " +
                  "destination, a terminal TASKABRT, a completed task and an overdue mover that has arrived (EmitNow); " +
                  "YES for a travelling mover, one whose arrival is HELD for its end time (heldForEndTime), one reported " +
                  "STUCK (the report-only abort leaves the timer) and one OVERDUE");
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
        string gateCall = Between(src, "var gate = await _sequencer.WaitForStartAsync(task.StartAfterTaskUuid,",
                                  "if (gate != GateResult.Proceed)");
        Check(ref failures, gateCall.Contains("() => TreatPredecessorAsOverdue(task, timeoutSeconds));")
                            && gateCall.Contains("_vrf.TaskChainBackstopSeconds,"),
              "(t5) RL-20260927-05: the service's STREND gate call hands WaitForStartAsync the chain backstop AND the " +
              "question (TreatPredecessorAsOverdue)");
        string ask = Between(src, "private bool TreatPredecessorAsOverdue(",
                             "Read the LIVE position of every materialized member");
        Check(ref failures, ask.Contains("if (!_timed.IsUnfinishedMover(pred, out bool heldForEndTime)) return false;")
                            && ask.Contains("return true;") && ask.Contains("catch (Exception ex)"),
              "(t5) RL-20260927-05: the question is answered by TimedCompletionPolicy.IsUnfinishedMover, and a " +
              "failure answers NO (the old skip) with an ERROR line");
        string noDurLine = Between(src, "the order gives NO Duration, so this task has no end time",
                                   "collapses its {D:F0} s Duration");
        Check(ref failures, noDurLine.Contains("With no end time it is never OVERDUE")
                            && noDurLine.Contains("Vrf:TaskPredecessorTimeoutSeconds")
                            && noDurLine.Contains("if the unit arrives later")
                            && !src.Contains("and until it does its STREND successors wait at the gate"),
              "(t5) the NO-Duration dispatch warning says what the gate does - never OVERDUE, the configured floor, " +
              "then Vrf:PredecessorTimeoutPolicy even if the unit arrives later - and no longer promises a wait " +
              "\"until it does\"");
        return failures;
    }

    // ----------------------------------------------------------------------------------------------------
    // THE RIG: one predecessor, one successor gate, the service's own order of calls.

    private enum Ask { None, Service, Throws }

    private sealed class Rig
    {
        public const string Pred = "PRED";
        public readonly AxisClock Clock = new(X);
        public readonly TaskSequencer Seq = new();
        public readonly TimedCompletionPolicy Timed = new();
        public readonly Task<GateResult> Gate;
        public double OverdueAt = double.NaN, CompletedAt = double.NaN, Served = double.NaN;
        public bool CompletedBeforeGate;
        private int _answer;                                    // 0 = not asked, 1 = YES, 2 = NO (or threw)
        public bool Asked => Volatile.Read(ref _answer) != 0;
        public bool AnsweredYes => Volatile.Read(ref _answer) == 1;

        /// <summary>MarkDispatched (one read of the axis: the gate's stamp, then Register), then the
        /// successor's gate as the service opens it.</summary>
        public Rig(bool armTimer, bool hasDestination, double window, double backstop, Ask ask,
                   bool anchorAtDispatch = true)
        {
            double reading = Clock.Now;
            Seq.NotifyDispatched(Pred, reading);
            if (armTimer && anchorAtDispatch)
                Timed.Register(Pred, "taskee", "T-PRED", "UNIT", Duration, hasDestination, dispatchClock: reading);
            else if (armTimer)
                Timed.Register(Pred, "taskee", "T-PRED", "UNIT", Duration, hasDestination);
            Func<bool> question = ask switch
            {
                // what VrfC2SimService.TreatPredecessorAsOverdue answers, minus its log line
                Ask.Service => () =>
                {
                    bool yes = false;
                    try { yes = Timed.IsUnfinishedMover(Pred, out _); return yes; }
                    finally { Volatile.Write(ref _answer, yes ? 1 : 2); }
                },
                Ask.Throws => () =>
                {
                    Volatile.Write(ref _answer, 2);
                    throw new InvalidOperationException("a failing question");
                },
                _ => null,
            };
            Gate = Seq.WaitForStartAsync(Pred, 0, 0, window, Clock.AsTaskClock(), CancellationToken.None,
                                         dispatchTimeoutSeconds: backstop, overdueBackstopSeconds: backstop,
                                         treatPredecessorAsOverdue: question);
        }

        /// <summary>The service's timed walk (MaybeCompleteTimedTasks): OVERDUE is told to the gate,
        /// a completion releases it.</summary>
        public void Walk()
        {
            foreach (var d in Timed.Advance(Clock.Now, usingSim: true))
            {
                if (d.Kind == TimedCompletionPolicy.DueKind.OverdueAwaitingArrival)
                {
                    if (double.IsNaN(OverdueAt)) OverdueAt = Clock.Now;
                    Seq.NotifyOverdue(d.TaskUuid);
                    continue;
                }
                CompletedAt = Clock.Now;
                Served = d.Elapsed;
                CompletedBeforeGate = !Gate.IsCompleted;
                Seq.CompleteTask(d.TaskUuid);
            }
        }

        /// <summary>The axis at X + each offset, one timed walk per step, the gate's poller looking after
        /// each. Stops when the gate is decided (or, if asked, when the timer has completed the task).</summary>
        public void WalkOffsets(double fromOffset, double toOffset, double step, bool stopAtCompletion = false)
        {
            for (int i = 0; ; i++)
            {
                double off = fromOffset + i * step;
                if (off > toOffset + 1e-9 || Gate.IsCompleted) return;
                if (stopAtCompletion && !double.IsNaN(CompletedAt)) return;
                Clock.Set(X + off);
                Walk();
                Settle();
            }
        }

        /// <summary>ONE axis step that the gate's poller sees BEFORE the next timed walk (E2: C187, 0.8 WALL
        /// s before C189): the gate decides - or answers YES to its question and settles into its extension -
        /// and only then does the walk run.</summary>
        public void JumpSeenByGateFirst(double offset, bool walk = true)
        {
            Clock.Set(X + offset);
            SpinWait.SpinUntil(() => Gate.IsCompleted || AnsweredYes, 2000);
            if (AnsweredYes) Thread.Sleep(AxisClock.PollMs * 3);   // let the extension start its wait
            if (walk) Walk();
        }

        /// <summary>The unit ARRIVES: SynthesizeUnitCompletion's MarkFinished, then the release rule.</summary>
        public TimedCompletionPolicy.FinishVerdict Arrive()
        {
            var v = Timed.MarkFinished(Pred);
            if (TimedCompletionPolicy.ReleasesSuccessorsNow(true, v)) Seq.CompleteTask(Pred);
            return v;
        }

        /// <summary>The stall watchdog's ReportStall: a REPORT-ONLY TASKABRT (PushTaskStatus cancels the timer
        /// only for a terminal code) and the follow-ons ABANDONED (RL-20260925-01 Q2).</summary>
        public void ReportStuck()
        {
            if (TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT, reportOnlyAbort: true))
                Timed.Cancel(Pred);
            Seq.NotifyAbandoned(Pred);
        }

        public void Settle()
        {
            if (!Gate.IsCompleted) Thread.Sleep(AxisClock.PollMs * 3);
        }

        public bool Faulted => Gate.IsFaulted;

        public GateResult? Result()
        {
            SpinWait.SpinUntil(() => Gate.IsCompleted, 5000);
            return Gate.IsCompleted && !Gate.IsFaulted && !Gate.IsCanceled ? Gate.Result : null;
        }
    }

    private readonly record struct Outcome(GateResult? Gate, bool CompletedBeforeGate, double CompletedAt,
                                           double Served, double OverdueAt, TimedCompletionPolicy.FinishVerdict Arrival,
                                           bool WaitingAtArrival, bool WaitingBefore, bool Asked, bool TimerStillArmed,
                                           bool Faulted);

    private static Outcome Done(Rig r, TimedCompletionPolicy.FinishVerdict arrival = TimedCompletionPolicy.FinishVerdict.NotTimed,
                                bool waitingAtArrival = false, bool waitingBefore = false, bool timerStillArmed = false)
    {
        var result = r.Result();
        return new Outcome(result, r.CompletedBeforeGate, r.CompletedAt, r.Served, r.OverdueAt, arrival,
                           waitingAtArrival, waitingBefore, r.Asked, timerStillArmed, r.Faulted);
    }

    /// <summary>
    /// E2, on the service's own sequence, with the question wired as the service wires it (a hold answers NO):
    /// the first walk comes 45 SIM s after the stamp (E2's ">= 45"), then one every 15 SIM s up to X+330; the
    /// next axis reading, X+360, is seen by the gate's poller BEFORE the tick thread's next walk (E2: C187, 0.8
    /// WALL s before C189). Both arms run the SAME walks; only Register differs.
    /// </summary>
    private static Outcome ReplayE2(bool anchorAtDispatch, double window)
    {
        var r = new Rig(armTimer: true, hasDestination: false, window, Backstop, Ask.Service, anchorAtDispatch);
        r.WalkOffsets(45.0, 330.0, Step, stopAtCompletion: true);
        r.JumpSeenByGateFirst(360.0);
        return Done(r);
    }

    /// <summary>A MOVER predecessor (it has a destination) whose unit ARRIVES at X+500, after its end time.
    /// stepAcrossEndTime=false: a walk every 15 SIM s to X+480. true: a walk every 15 SIM s to X+285, then ONE
    /// axis step to X+360 that the gate sees before the next walk (the (t4) race), then walks to X+480.</summary>
    private static Outcome LateMover(bool stepAcrossEndTime, Ask ask, double window)
    {
        var r = new Rig(armTimer: true, hasDestination: true, window, Backstop, ask);
        if (stepAcrossEndTime)
        {
            r.WalkOffsets(Step, 285.0, Step);
            r.JumpSeenByGateFirst(360.0);                            // the walk then flags OVERDUE - late
            r.WalkOffsets(375.0, 480.0, Step);
        }
        else r.WalkOffsets(Step, 480.0, Step);
        bool waiting = !r.Gate.IsCompleted;
        r.Clock.Set(X + 500.0);
        r.Walk();
        var arrival = r.Arrive();                                    // arrival evidence
        return Done(r, arrival, waitingAtArrival: waiting);
    }

    /// <summary>A MOVER whose unit ARRIVES EARLY (X+100: its TASKCMPLT is held for its end time), then the
    /// (t4) step X+285 -> X+360 seen by the gate first; the timed walk then reports the held TASKCMPLT.</summary>
    private static Outcome HeldMover(Ask ask, double window)
    {
        var r = new Rig(armTimer: true, hasDestination: true, window, Backstop, ask);
        r.WalkOffsets(Step, 90.0, Step);
        r.Clock.Set(X + 100.0);
        r.Walk();
        var arrival = r.Arrive();                                    // Hold
        r.WalkOffsets(105.0, 285.0, Step);
        r.JumpSeenByGateFirst(360.0);
        return Done(r, arrival);
    }

    /// <summary>A HOLD (no destination) whose timer NEVER fires (no timed walk at all): the gate reaches its
    /// window with the timer still armed.</summary>
    private static Outcome HoldTimerNeverFires(double window)
    {
        var r = new Rig(armTimer: true, hasDestination: false, window, Backstop, Ask.Service);
        r.Clock.Set(X + 345.0);
        r.Settle();
        bool waiting = !r.Gate.IsCompleted;
        r.JumpSeenByGateFirst(360.0, walk: false);
        return Done(r, waitingBefore: waiting, timerStillArmed: r.Timed.Count == 1);
    }

    /// <summary>A MOVER with NO Duration: no timer is armed (MarkDispatched's "no end time" branch), so the
    /// successor's window is the configured floor; the unit arrives at X+700, after it.</summary>
    private static Outcome NoDurationMover(double floorWindow)
    {
        var r = new Rig(armTimer: false, hasDestination: true, floorWindow, Backstop, Ask.Service);
        r.WalkOffsets(50.0, floorWindow - 50.0, 50.0);
        bool waiting = !r.Gate.IsCompleted;
        r.JumpSeenByGateFirst(floorWindow);
        r.Clock.Set(X + 700.0);
        var arrival = r.Arrive();                                    // NotTimed: evidence-only, too late
        return Done(r, arrival, waitingBefore: waiting);
    }

    /// <summary>A MOVER reported STUCK by the watchdog - at X+200, inside its window, or at X+400, after the
    /// (t4) step has put the gate into its RL-20260927-05 extension.</summary>
    private static Outcome Stuck(bool duringExtension, double window)
    {
        var r = new Rig(armTimer: true, hasDestination: true, window, Backstop, Ask.Service);
        if (duringExtension)
        {
            r.WalkOffsets(Step, 285.0, Step);
            r.JumpSeenByGateFirst(360.0);
            r.WalkOffsets(375.0, 390.0, Step);
            r.Clock.Set(X + 400.0);
        }
        else
        {
            r.WalkOffsets(Step, 195.0, Step);
            r.Clock.Set(X + 200.0);
        }
        r.Walk();
        r.Settle();
        bool waiting = !r.Gate.IsCompleted;
        r.ReportStuck();
        bool armed = r.Timed.IsUnfinishedMover(Rig.Pred, out _);
        return Done(r, waitingBefore: waiting, timerStillArmed: armed);
    }

    /// <summary>A MOVER that never arrives and is never reported stuck, under a short chain backstop.</summary>
    private static Outcome NeverArrives(double backstop, double window)
    {
        var r = new Rig(armTimer: true, hasDestination: true, window, backstop, Ask.Service);
        r.WalkOffsets(Step, 285.0, Step);
        r.JumpSeenByGateFirst(360.0);
        r.WalkOffsets(420.0, backstop - 60.0, 60.0);
        bool waiting = !r.Gate.IsCompleted;
        r.Clock.Set(X + backstop);
        return Done(r, waitingBefore: waiting);
    }

    /// <summary>The (t4) step with a question that throws.</summary>
    private static Outcome ThrowingQuestion(double window)
    {
        var r = new Rig(armTimer: true, hasDestination: true, window, Backstop, Ask.Throws);
        r.WalkOffsets(Step, 285.0, Step);
        r.JumpSeenByGateFirst(360.0);
        return Done(r);
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
