using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of the four tasking rulings of 2026-09-14 (no bridge, no MAK, no VR-Forces):
/// `VrfC2SimApp --rulings-selftest`. One switch, four sections, because the rulings are one
/// decision about how a C2SIM task becomes a VR-Forces task:
///
///   R4 "completion is given by the end time" - TimedCompletionPolicy on a FAKE clock.
///   R2 "a task without geometry uses the geometry of the performing (who) unit" -
///      TaskDispatchPolicy.ZeroGeometry.
///   R3 "the target IS the objective" - TaskDispatchPolicy.ResolveTarget (no verb is refused
///      for self-targeting).
///   R1 (transition) MapGraphicID -> the init graphic created under the same C2SIM uuid -
///      TaskGeometryResolver.
///
/// Every policy under test is PURE: no clock is read, no bridge is called, no report is sent,
/// so the checks are decidable offline and a live run has nothing to prove about them.
/// </summary>
public static class RulingsSelfTest
{
    public static int Run()
    {
        int failures = 0;
        Console.WriteLine("=== R4: completion is given by the end time ===");
        R4(ref failures);
        Console.WriteLine("=== R2: a task without geometry uses the performing unit's position ===");
        R2(ref failures);
        Console.WriteLine("=== R3: the target IS the objective ===");
        R3(ref failures);
        Console.WriteLine("=== R1 (transition): MapGraphicID -> the graphic created at init ===");
        R1(ref failures);
        Console.WriteLine("=== The TASK CLOCK: an unsteady or frozen sim reader must not stop the order ===");
        TaskClockChecks(ref failures);
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------- R4 ----
    private static void R4(ref int failures)
    {
        const double dur = 4800.0;   // COA-STP1's PT1H20M, in seconds

        // (b1) A task with Duration X completes with EXACTLY ONE TASKCMPLT at X.
        {
            var p = new TimedCompletionPolicy();
            Check(ref failures, p.Register("T1", "taskee-1", "T1_Secure", "1-35 AR", dur),
                  "a task with a Duration registers a timed end");
            // Anchor, then walk the clock in 600 s steps. Nothing is due before the deadline.
            p.Advance(1000.0, usingSim: true);
            int dueBefore = 0;
            for (double t = 1600.0; t < 1000.0 + dur; t += 600.0)
                dueBefore += p.Advance(t, usingSim: true).Count;
            Check(ref failures, dueBefore == 0, $"nothing completes before the end time ({dueBefore} early)");

            var due = p.Advance(1000.0 + dur, usingSim: true);
            Check(ref failures, due.Count == 1 && due[0].TaskUuid == "T1",
                  $"exactly one completion AT the end time (got {due.Count})");
            var again = p.Advance(1000.0 + dur + 10000.0, usingSim: true);
            Check(ref failures, again.Count == 0 && p.Count == 0,
                  "the timer is consumed - no second completion, ever");
        }

        // (b2) An EARLIER arrival-evidence completion cancels the timer.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T2", "taskee-2", "T2_Move", "1-35 AR", dur);
            p.Advance(0.0, usingSim: false);
            p.Advance(100.0, usingSim: false);
            Check(ref failures, TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKCMPLT),
                  "a TASKCMPLT cancels the timed end (arrival evidence wins)");
            Check(ref failures, p.Cancel("T2"), "the arrival completion removes the pending timer");
            Check(ref failures, p.Advance(100.0 + dur * 2, usingSim: false).Count == 0,
                  "a cancelled timer never fires a second TASKCMPLT");
        }

        // (b3) A TASKABRT (stall watchdog, refusal, skipped successor) also cancels it.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T3", "taskee-3", "T3_Attack", "1-6 IN", dur);
            p.Advance(0.0, usingSim: false);
            Check(ref failures, TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT),
                  "a TASKABRT cancels the timed end (the task is not going to run)");
            Check(ref failures, !TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKSTRT)
                             && !TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKINPRG),
                  "TASKSTRT / TASKINPRG do NOT cancel it (a start and a progress note are not an end)");
            p.Cancel("T3");
            Check(ref failures, p.Advance(dur * 2, usingSim: false).Count == 0,
                  "an aborted task produces no later timed TASKCMPLT");
        }

        // (b4) A PAUSED clock does not age the task, and a ROLLBACK does not complete it early.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T4", "taskee-4", "T4_Defend", "A/6-56", 100.0);
            p.Advance(1000.0, usingSim: true);
            p.Advance(1050.0, usingSim: true);              // 50 s served
            for (int i = 0; i < 20; i++) p.Advance(1050.0, usingSim: true);   // scenario paused
            Check(ref failures, p.Count == 1, "a paused sim clock does not age a timed task");
            p.Advance(900.0, usingSim: true);               // rollbackToSnapshot: clock steps BACK
            Check(ref failures, p.Count == 1, "a backwards clock step does not complete the task");
            Check(ref failures, p.Advance(950.0, usingSim: true).Count == 1,
                  "the remaining 50 s still complete it once the clock advances again");
        }

        // (b5) A clock-MODE change (sim reader lost -> wall fallback) re-anchors instead of
        //      completing the task on the difference between two unrelated time bases.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T5", "taskee-5", "T5_Screen", "B/6-56", 100.0);
            p.Advance(500.0, usingSim: true);
            p.Advance(540.0, usingSim: true);                    // 40 s served on the sim clock
            var onSwitch = p.Advance(1.7e9, usingSim: false);    // wall seconds: a huge step
            Check(ref failures, onSwitch.Count == 0 && p.Count == 1,
                  "a clock-mode change re-anchors (no completion on the mode step itself)");
            Check(ref failures, p.Advance(1.7e9 + 59.0, usingSim: false).Count == 0
                             && p.Advance(1.7e9 + 61.0, usingSim: false).Count == 1,
                  "the 60 s it still owes are served on the new clock");
        }

        // (b6) A task with NO Duration has no timed end (it completes on its own evidence only).
        {
            var p = new TimedCompletionPolicy();
            Check(ref failures, !p.Register("T6", "taskee-6", "T6_Move", "C/6-56", 0.0),
                  "a task with no Duration registers no timer");
            Check(ref failures, p.Advance(1e6, usingSim: false).Count == 0 && p.Count == 0,
                  "... and never completes on time");
        }

        // (b7) THE STREND CHAIN: the timed completion releases the successor's gate exactly as a
        //      vendor completion does. This is the service's rule expressed here - the due entry's
        //      task uuid is handed to TaskSequencer.CompleteTask - so the chain cannot be broken
        //      by a change to either side without this check failing.
        {
            var p = new TimedCompletionPolicy();
            var seq = new TaskSequencer();
            p.Register("PRED", "taskee-7", "T7_Secure", "1-35 AR", 100.0);
            seq.NotifyDispatched("PRED", TaskClock.Wall.Now());
            var successor = seq.WaitForStartAsync("PRED", 0, 0, 5.0, TaskClock.Wall,
                                                  CancellationToken.None);
            p.Advance(0.0, usingSim: false);
            Thread.Sleep(120);
            Check(ref failures, !successor.IsCompleted, "the successor waits while the task is running");
            var due = p.Advance(100.0, usingSim: false);
            foreach (var d in due) seq.CompleteTask(d.TaskUuid);   // exactly what the service does
            bool released = successor.Wait(TimeSpan.FromSeconds(2));
            Check(ref failures, released && successor.Result == GateResult.Proceed,
                  "the STREND successor dispatches after the TIMED completion");
        }

        // (b8) M1 - THE GATE MUST OUTLIVE THE END TIME IT WAITS FOR. This is the one check in this
        //      file that runs the REAL TaskSequencer and the REAL TimedCompletionPolicy against a
        //      common clock, because the defect it covers is an interaction between the two and
        //      not a property of either: the gate expired at dispatch + configured timeout while
        //      the completion fires at dispatch + Duration, so on COA-STP1 (600 s configured,
        //      4,800 s Duration) all 31 gated tasks were SKIPPED with TASKABRT.
        {
            const double predDuration = 4800.0;   // COA-STP1's PT1H20M, in seconds
            const double configured = 600.0;      // the shipped Vrf:TaskPredecessorTimeoutSeconds
            const double margin = 60.0;           // the shipped Vrf:TaskPredecessorEndMarginSeconds

            double preFixWindow = configured;     // what the branch used before M1
            double derivedWindow = TaskDispatchPolicy.PredecessorTimeoutSeconds(
                configured, predDuration, margin);
            Check(ref failures, preFixWindow < predDuration && derivedWindow >= predDuration + margin,
                  $"the PRE-FIX window ({preFixWindow:F0} s) is shorter than the predecessor's end time " +
                  $"({predDuration:F0} s); the derived one ({derivedWindow:F0} s) is not");

            // FAIL-FIRST CONTROL: the pre-fix window, on the same clock, still SKIPS the successor.
            Check(ref failures, GateOutcome(preFixWindow, predDuration) == GateResult.PredecessorTimeout,
                  "FAIL-FIRST: with the flat configured window the successor times out while its " +
                  "predecessor is still running (the behaviour M1 replaces)");

            // FIXED: the derived window outlives the end time, and the timed completion releases it.
            Check(ref failures, GateOutcome(derivedWindow, predDuration) == GateResult.Proceed,
                  "with the window derived from the predecessor's ARMED END TIME the successor " +
                  "dispatches AT the timed completion, and is not skipped");

            // A predecessor with NO Duration leaves the configured floor exactly as it was.
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, 0.0, margin) == configured
                  && TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, double.NaN, margin) == configured,
                  "a predecessor with no armed end time leaves Vrf:TaskPredecessorTimeoutSeconds alone");
        }
    }

    /// <summary>
    /// Run ONE STREND gate against the real TaskSequencer and the real TimedCompletionPolicy on a
    /// fake clock, and report what the gate decided. The predecessor is dispatched, armed with
    /// <paramref name="predDurationSeconds"/>, and the clock is walked forward in 60 s steps; the
    /// timed completion is fed to CompleteTask exactly as MaybeCompleteTimedTasks does.
    /// </summary>
    private static GateResult GateOutcome(double windowSeconds, double predDurationSeconds)
    {
        var clock = new FakeClock();
        var seq = new TaskSequencer();
        var timed = new TimedCompletionPolicy();
        const string pred = "PRED-M1";
        seq.NotifyDispatched(pred, clock.Now);
        timed.Register(pred, "taskee-m1", "T_Secure", "1-35 AR", predDurationSeconds);
        timed.Advance(clock.Now, usingSim: true);     // the anchoring walk MarkDispatched leaves behind
        var gate = seq.WaitForStartAsync(pred, 0, 0, windowSeconds, clock.AsTaskClock(),
                                         CancellationToken.None);
        for (double served = 0.0; served <= predDurationSeconds + 600.0 && !gate.IsCompleted; served += 60.0)
        {
            clock.Advance(60.0);
            foreach (var d in timed.Advance(clock.Now, usingSim: true)) seq.CompleteTask(d.TaskUuid);
            Thread.Sleep(FakeClock.PollMs * 2);       // let the gate's poller observe the step
        }
        return gate.Wait(TimeSpan.FromSeconds(5)) ? gate.Result : GateResult.PredecessorTimeout;
    }

    /// <summary>A clock the test drives by hand. Monotone, in seconds, exactly what the service's
    /// own task-clock axis is - so a delay taken on it is a delay in SIMULATED time.</summary>
    private sealed class FakeClock
    {
        public const int PollMs = 20;
        private readonly object _lock = new();
        private double _seconds;
        public double Now { get { lock (_lock) return _seconds; } }
        public void Advance(double seconds) { lock (_lock) _seconds += seconds; }
        public TaskClock AsTaskClock() => new(() => Now, DelayAsync);
        private async Task DelayAsync(double seconds, CancellationToken ct)
        {
            double start = Now;
            while (Now - start < seconds)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(PollMs, ct).ConfigureAwait(false);
            }
        }
    }

    // ------------------------------------------------------- THE TASK CLOCK ----
    // M3 + M4 of the cold-start review of 5c67d41. Both defects are the SAME failure mode from two
    // directions: the R4 walk stops serving time, no task ever completes, nothing is logged, and
    // every successor is then skipped at the predecessor gate. Both are checked here against the
    // REAL SimClockTracker, the REAL TaskClockAxis and the REAL TimedCompletionPolicy, with the
    // pre-fix behaviour kept beside each as the fail-first control.
    private static void TaskClockChecks(ref int failures)
    {
        const double dur = 100.0;

        // (e1) M3 - A READER ALTERNATING -1 / >= 0 AT THE SAMPLE CADENCE. This is the documented
        //      "back end briefly out of the list" case (vrfBackendListener.h:154-163) the watchdog
        //      was hardened against and the timed walk was not.
        Func<int, double> flapping = i => (i % 2 == 0) ? 1000.0 + i : -1.0;

        var preFix = WalkTaskClock(flapping, useConfirmedMode: false, honourStale: true, dur, 400);
        Check(ref failures, !preFix.Completed && preFix.AxisSeconds == 0.0,
              $"FAIL-FIRST (M3): on the RAW mode a flapping reader leaves the axis at " +
              $"{preFix.AxisSeconds:F0} s after 400 samples - the task never completes and nothing is logged");

        var fixedFlap = WalkTaskClock(flapping, useConfirmedMode: true, honourStale: true, dur, 400);
        Check(ref failures, fixedFlap.Completed && fixedFlap.ModeFlips == 0,
              $"with the HYSTERESIS-CONFIRMED mode the same reader completes the task " +
              $"({fixedFlap.Samples} samples) and never flips the clock mode ({fixedFlap.ModeFlips} flips)");

        // (e2) M4 - A FROZEN READER. VrfFacade::SimTimeSeconds gates on backends().count() > 0 and
        //      a deactivated back end is NOT removed, so the reader returns its last value for the
        //      rest of the run. Read as a pause, no Duration ever elapses again.
        Func<int, double> frozen = _ => 5000.0;

        var preFixFrozen = WalkTaskClock(frozen, useConfirmedMode: true, honourStale: false, dur, 400);
        Check(ref failures, !preFixFrozen.Completed && preFixFrozen.AxisSeconds == 0.0,
              "FAIL-FIRST (M4): with no stale detection a frozen sim clock freezes every end time - " +
              "no TASKCMPLT is ever emitted, and nothing says so");

        var fixedFrozen = WalkTaskClock(frozen, useConfirmedMode: true, honourStale: true, dur, 400);
        Check(ref failures, fixedFrozen.Completed && fixedFrozen.StaleTransitions == 1,
              $"a frozen sim clock is detected ONCE ({fixedFrozen.StaleTransitions} transition(s)) and the " +
              $"task completes on the WALL fallback ({fixedFrozen.Samples} samples)");

        // (e3) The axis itself: it never invents time, whatever the reader does.
        {
            var axis = new TaskClockAxis();
            axis.Advance(1000.0, usingSim: true);          // anchor
            axis.Advance(1060.0, usingSim: true);          // 60 s of scenario
            double afterRun = axis.Seconds;
            axis.Advance(1060.0, usingSim: true);          // paused
            axis.Advance(900.0, usingSim: true);           // rollbackToSnapshot
            Check(ref failures, axis.Seconds == afterRun && afterRun == 60.0,
                  $"the axis adds a pause and a rollback as ZERO (60 s served, still {axis.Seconds:F0} s)");
            axis.Advance(1.7e9, usingSim: false);          // fall back to the wall clock
            Check(ref failures, axis.Seconds == afterRun,
                  "a fall back to the WALL clock adds nothing across the change - the two epochs " +
                  "are not comparable, and the task keeps the time it has served");
            axis.Advance(1.7e9 + 30.0, usingSim: false);
            Check(ref failures, axis.Seconds == afterRun + 30.0,
                  "... and the rest of the Duration is then served in wall seconds");
        }

        // (e4) A SUSTAINED change is still adopted - the hysteresis must not blind the interface to
        //      a back end that really has gone.
        {
            var tracker = new SimClockTracker();
            double wall = 1.7e9;
            tracker.Observe(1000.0, wall, StallPolicy.ModeSwitchConfirmations, StallPolicy.StaleClockWarnSeconds);
            int adoptedAt = -1;
            for (int i = 1; i <= 6 && adoptedAt < 0; i++)
            {
                var o = tracker.Observe(-1.0, wall += 1.0, StallPolicy.ModeSwitchConfirmations,
                                        StallPolicy.StaleClockWarnSeconds);
                if (o.ModeChanged) adoptedAt = i;
            }
            Check(ref failures, adoptedAt == StallPolicy.ModeSwitchConfirmations && !tracker.ReadableConfirmed,
                  $"a SUSTAINED loss of the sim reader IS adopted, on reading " +
                  $"{StallPolicy.ModeSwitchConfirmations} (got {adoptedAt})");
        }

        // (e5) A backwards step is reported as a ROLLBACK and never as a stale clock - the two
        //      have opposite remedies (re-anchor and carry on, vs stop serving on this clock).
        {
            var tracker = new SimClockTracker();
            double wall = 1.7e9;
            tracker.Observe(1000.0, wall, StallPolicy.ModeSwitchConfirmations, StallPolicy.StaleClockWarnSeconds);
            var back = tracker.Observe(500.0, wall + 1.0, StallPolicy.ModeSwitchConfirmations,
                                       StallPolicy.StaleClockWarnSeconds);
            Check(ref failures,
                  back.Step == StallPolicy.SimClockStep.RolledBack && !back.Stale
                  && back.PreviousSimSeconds == 1000.0,
                  $"a backwards step is a ROLLBACK (1000 s -> 500 s), not a stale clock (got {back.Step})");
            var flat = tracker.Observe(500.0, wall + 1.0 + StallPolicy.StaleClockWarnSeconds,
                                       StallPolicy.ModeSwitchConfirmations, StallPolicy.StaleClockWarnSeconds);
            Check(ref failures, flat.Stale && flat.Step == StallPolicy.SimClockStep.Flat,
                  "... and the stale clock is only what stays FLAT for the whole stale window");
        }
    }

    /// <summary>
    /// One run of the service's OWN timed-completion loop, on a scripted sim reader: the sampler
    /// (SimClockTracker), the axis (TaskClockAxis) and the walk (TimedCompletionPolicy), sampled
    /// once per WALL second exactly as SampleTaskClock does. <paramref name="useConfirmedMode"/>
    /// false and <paramref name="honourStale"/> false reproduce the PRE-FIX behaviours of M3 and M4.
    /// </summary>
    private static (bool Completed, int Samples, double AxisSeconds, int ModeFlips, int StaleTransitions)
        WalkTaskClock(Func<int, double> reader, bool useConfirmedMode, bool honourStale,
                      double durationSeconds, int maxSamples)
    {
        var tracker = new SimClockTracker();
        var axis = new TaskClockAxis();
        var timed = new TimedCompletionPolicy();
        timed.Register("T-CLOCK", "taskee-clock", "T_Secure", "1-35 AR", durationSeconds);
        double wall = 1.7e9;            // a plausible wall epoch, in seconds
        int modeFlips = 0, staleTransitions = 0, samples = 0;
        bool lastStale = false, completed = false;
        for (; samples < maxSamples && !completed; samples++, wall += 1.0)
        {
            var obs = tracker.Observe(reader(samples), wall, StallPolicy.ModeSwitchConfirmations,
                                      StallPolicy.StaleClockWarnSeconds);
            // The FIRST resolution (mode 0 -> readable/not) is adopted at once and is not a
            // flip - nothing has been served yet. Only later changes can starve the axis.
            if (obs.ModeChanged && samples > 0) modeFlips++;
            bool stale = honourStale && obs.Stale;
            if (stale != lastStale) { staleTransitions++; lastStale = stale; }
            bool heldOnSim = useConfirmedMode ? obs.ReadableConfirmed : obs.Readable;
            if (heldOnSim && !obs.Readable) continue;      // nothing to read this sample
            bool usingSim = heldOnSim && !stale;
            axis.Advance(usingSim ? obs.SimSeconds : wall, usingSim);
            if (timed.Advance(axis.Seconds, usingSim: true).Count > 0) completed = true;
        }
        return (completed, samples, axis.Seconds, modeFlips, staleTransitions);
    }

    // ---------------------------------------------------------------- R2 ----
    private static void R2(ref int failures)
    {
        // (c1) A T9-SHAPED TASK - "T9_ProvideAirDefenseCoverage...", zero Locations, no distinct
        //      affected entity - is DISPATCHED IN PLACE, not refused. This is the exact task run
        //      G6 logged as "NO LOCATION GIVEN - CAN'T EXECUTE TASK".
        var t9 = TaskDispatchPolicy.ForZeroGeometry(performerResolved: true, hasAttackTarget: false,
                                                    hasBreachTarget: false);
        Check(ref failures, t9 == ZeroGeometryAction.ExecuteInPlace,
              $"a zero-geometry task executes at the performing unit's position (got {t9})");
        Check(ref failures, !TaskDispatchPolicy.Refuses(t9),
              "... and is NOT refused, so its STREND chain is not abandoned");

        // (c2) The ONLY refusal left is the one that was never about geometry.
        var noUnit = TaskDispatchPolicy.ForZeroGeometry(performerResolved: false, hasAttackTarget: false,
                                                        hasBreachTarget: false);
        Check(ref failures, noUnit == ZeroGeometryAction.Refuse && TaskDispatchPolicy.Refuses(noUnit),
              "a task whose PERFORMER cannot be resolved is still refused");

        // (c3) A resolved distinct target still engages in place (unchanged behaviour).
        Check(ref failures,
              TaskDispatchPolicy.ForZeroGeometry(true, hasAttackTarget: true, hasBreachTarget: false)
                  == ZeroGeometryAction.EngageInPlace
              && TaskDispatchPolicy.ForZeroGeometry(true, hasAttackTarget: false, hasBreachTarget: true)
                  == ZeroGeometryAction.BreachInPlace,
              "a resolved attack / breach target still engages in place");

        // (c4) The derivation is REPORTED, in the ruling's own words.
        Check(ref failures,
              TaskDispatchPolicy.ZeroGeometryObservation
                  == "no geometry in the order: executing at the performing unit's position",
              "the in-place dispatch announces the derivation verbatim");

        // (c5) THE SUCCESSOR IS NOT SKIPPED. The in-place task is dispatched (not abandoned) and
        //      closed by R4's end time, so the STREND gate releases exactly as for a moving task.
        {
            var seq = new TaskSequencer();
            var timed = new TimedCompletionPolicy();
            const string inPlace = "T9";
            seq.NotifyDispatched(inPlace, TaskClock.Wall.Now());  // what MarkDispatched does
            timed.Register(inPlace, "taskee-9", "T9_ProvideAirDefenseCoverage", "A/6-56 ADA", 300.0);
            var successor = seq.WaitForStartAsync(inPlace, 0, 0, 5.0, TaskClock.Wall,
                                                  CancellationToken.None);
            timed.Advance(0.0, usingSim: false);
            Thread.Sleep(120);
            Check(ref failures, !successor.IsCompleted,
                  "the successor of an in-place task waits (it was not abandoned)");
            foreach (var d in timed.Advance(300.0, usingSim: false)) seq.CompleteTask(d.TaskUuid);
            bool released = successor.Wait(TimeSpan.FromSeconds(2));
            Check(ref failures, released && successor.Result == GateResult.Proceed,
                  "the successor of an in-place task DISPATCHES at its predecessor's end time");
        }
    }

    // ---------------------------------------------------------------- R3 ----
    private static void R3(ref int failures)
    {
        // (d1) THE DISCRIMINATING CHECK. STP sets AffectedEntity to the performing unit on all 42
        //      COA-STP1 tasks (C2SimXmlBuilder.cs:427-429). That is not an error and not "no
        //      target": the task's geometry is the objective.
        var self = TaskDispatchPolicy.ForTarget(hasAffectedEntity: true, resolved: true, isSelf: true);
        Check(ref failures, self == TargetResolution.SelfIsObjective,
              $"an ATTACK whose AffectedEntity IS the taskee resolves to the OBJECTIVE (got {self})");
        Check(ref failures, !TaskDispatchPolicy.RefusesForTarget(self)
                         && TaskDispatchPolicy.FallsBackToGeometry(self),
              "... is dispatched (never refused) and routed to the task's own geometry");

        // (d2) NO target resolution refuses a verb - R3 in one line.
        bool anyRefuses = false;
        foreach (TargetResolution r in Enum.GetValues<TargetResolution>())
            anyRefuses |= TaskDispatchPolicy.RefusesForTarget(r);
        Check(ref failures, !anyRefuses, "NO verb is refused for its target resolution, self included");

        // (d3) A self-targeted ATTACK that ALSO carries no geometry executes in place (R2 + R3
        //      together) - the T9-T12 shape, which used to be a refusal AND a chain abandon.
        Check(ref failures,
              TaskDispatchPolicy.ForZeroGeometry(performerResolved: true,
                                                 hasAttackTarget: false, hasBreachTarget: false)
                  == ZeroGeometryAction.ExecuteInPlace,
              "a self-targeted ATTACK with no geometry executes in place, not refused");

        // (d4) A genuinely distinct target is unchanged: it is still named to VR-Forces.
        Check(ref failures,
              TaskDispatchPolicy.ForTarget(true, resolved: true, isSelf: false) == TargetResolution.DistinctEntity
              && !TaskDispatchPolicy.FallsBackToGeometry(TargetResolution.DistinctEntity),
              "a DISTINCT resolved target is still engaged as an entity (FireAtTarget / Breach)");

        // (d5) An out-of-scope or absent target routes to the location form at the objective
        //      rather than producing a degraded-capability warning about a missing entity.
        Check(ref failures,
              TaskDispatchPolicy.ForTarget(true, resolved: false, isSelf: false) == TargetResolution.Unresolved
              && TaskDispatchPolicy.ForTarget(false, resolved: false, isSelf: false) == TargetResolution.NoTarget
              && TaskDispatchPolicy.FallsBackToGeometry(TargetResolution.Unresolved)
              && TaskDispatchPolicy.FallsBackToGeometry(TargetResolution.NoTarget),
              "an unresolved or absent target falls back to the task's geometry, not to a refusal");
    }

    // ---------------------------------------------------------------- R1 ----
    private static void R1(ref int failures)
    {
        // The init's graphics, as the service registers them: one AREA (a square around
        // 34.5 / -116.5, centroid exactly at its centre) and one LINE.
        const string objMadison = "11111111-2222-3333-4444-555555555555";
        const string plBlue = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        var graphics = new Dictionary<string, TaskGraphic>(StringComparer.Ordinal)
        {
            [objMadison] = new TaskGraphic(objMadison, "OBJ_MADISON", TaskGraphic.KindArea,
                new[] { (34.4, -116.6, (double?)null), (34.6, -116.6, (double?)null),
                        (34.6, -116.4, (double?)null), (34.4, -116.4, (double?)null) }),
            [plBlue] = new TaskGraphic(plBlue, "PL_BLUE", "line",
                new[] { (35.0, -117.0, (double?)null), (35.1, -117.1, (double?)null) }),
        };
        var embedded = new List<(double Lat, double Lon, double? Elev)>
            { (33.0, -115.0, null), (33.1, -115.1, null) };

        // (e1) A MapGraphicID that matches an init graphic resolves to THAT graphic's geometry -
        //      an area to its centroid - and says so.
        {
            var task = new OrderTask
            {
                TaskName = "T2_PL_OBJ_MADISON",
                MapGraphicUuid = objMadison,
                MapGraphicUuids = new[] { objMadison },
                Points = new List<(double, double, double?)>(embedded),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            bool centroid = r.Points.Count == 1
                            && Math.Abs(r.Points[0].Lat - 34.5) < 1e-9
                            && Math.Abs(r.Points[0].Lon + 116.5) < 1e-9;
            Check(ref failures, r.Source == GeometrySource.MapGraphic && centroid,
                  $"a MapGraphicID matching an init AREA resolves to its centroid " +
                  $"(source {r.Source}, {r.Points.Count} point(s))");
            Check(ref failures, r.Log.Any(l => l.Contains("geometry from MapGraphicID")
                                            && l.Contains(objMadison) && l.Contains("OBJ_MADISON")),
                  "... and logs \"geometry from MapGraphicID <uuid> -> <name>\"");
        }

        // (e2) Several MapGraphicIDs become the sequence of their geometries - a LINE contributes
        //      its vertices, in order. No verb-typed interpretation of the points (V4b, separate).
        {
            var task = new OrderTask
            {
                TaskName = "T_Multi",
                MapGraphicUuids = new[] { objMadison, plBlue },
                Points = new List<(double, double, double?)>(),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.MapGraphic && r.Points.Count == 3
                             && Math.Abs(r.Points[1].Lat - 35.0) < 1e-9
                             && Math.Abs(r.Points[2].Lat - 35.1) < 1e-9,
                  $"several MapGraphicIDs resolve to route vertices in order (got {r.Points.Count})");
        }

        // (e3) NO MapGraphicID - every COA-STP1 task today - uses the embedded Location, which is
        //      valid C2SIM and stays supported, and the line says which path was taken.
        {
            var task = new OrderTask
            {
                TaskName = "T1_AOA_SE_1-35_AR",
                Points = new List<(double, double, double?)>(embedded),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.EmbeddedLocation && r.Points.Count == 2
                             && Math.Abs(r.Points[0].Lat - 33.0) < 1e-9,
                  $"no MapGraphicID -> the embedded Location, unchanged (source {r.Source})");
            Check(ref failures, r.Log.Any(l => l.Contains("geometry from embedded Location")
                                            && l.Contains("STP-801")),
                  "... and logs \"geometry from embedded Location (no MapGraphicID - STP-801)\"");
        }

        // (e4) A MapGraphicID that matches NOTHING falls back to the embedded Location, says the
        //      id matched nothing, and still carries the STP-801 marker.
        {
            var task = new OrderTask
            {
                TaskName = "T_Unknown_Graphic",
                MapGraphicUuids = new[] { "00000000-0000-0000-0000-000000000000" },
                Points = new List<(double, double, double?)>(embedded),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.EmbeddedLocation && r.Points.Count == 2,
                  $"an unmatched MapGraphicID falls back to the embedded Location (source {r.Source})");
            Check(ref failures, r.Log.Any(l => l.Contains("matched no object created at init"))
                             && r.Log.Any(l => l.Contains("geometry from embedded Location")
                                            && l.Contains("STP-801")),
                  "... and says the id matched nothing, with the STP-801 marker");
        }

        // (e5) NO geometry at all is still no geometry - R2 executes it in place.
        {
            var task = new OrderTask { TaskName = "T9_ProvideAirDefenseCoverage" };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.None && r.Points.Count == 0,
                  "a task with neither a MapGraphicID nor a Location has no geometry (R2 takes it)");
        }

        // (e6) THE PARSER lifts every MapGraphicID the schema allows, not just the first.
        {
            var parsed = OrderParser.Parse(TwoGraphicOrderXml(objMadison, plBlue));
            Check(ref failures, parsed.Tasks.Count == 1 && parsed.Tasks[0].MapGraphicUuids.Count == 2
                             && parsed.Tasks[0].MapGraphicUuids[0] == objMadison
                             && parsed.Tasks[0].MapGraphicUuids[1] == plBlue
                             && parsed.Tasks[0].MapGraphicUuid == objMadison,
                  "OrderParser lifts ALL MapGraphicIDs in document order (and keeps the first)");
        }
    }

    /// <summary>A minimal, schema-shaped order carrying two MapGraphicIDs on one task.</summary>
    private static string TwoGraphicOrderXml(string firstUuid, string secondUuid) =>
        "<OrderBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">"
        + "<OrderID>rulings-selftest</OrderID>"
        + "<Task><ManeuverWarfareTask>"
        + "<MapGraphicID>" + firstUuid + "</MapGraphicID>"
        + "<MapGraphicID>" + secondUuid + "</MapGraphicID>"
        + "<Name>T_TwoGraphics</Name>"
        + "<UUID>99999999-9999-9999-9999-999999999999</UUID>"
        + "<PerformingEntity>88888888-8888-8888-8888-888888888888</PerformingEntity>"
        + "<TaskActionCode>ATTACK</TaskActionCode>"
        + "</ManeuverWarfareTask></Task>"
        + "</OrderBody>";

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
