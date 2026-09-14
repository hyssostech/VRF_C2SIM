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
        Console.WriteLine("=== The STREND CHAIN: a gate is a GRAPH, and its predecessor has a lead time ===");
        ChainTopology(ref failures);
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

            // A COMPRESSED demo must keep the relation: the gate still outlives the scaled end
            // time, because both sides are computed from the SAME scaled number.
            double demoEnd = TaskDispatchPolicy.ScaleOrderMs(4800000L, 0.01) / 1000.0;
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, demoEnd, margin) == configured
                  && demoEnd < configured,
                  $"a compressed demo (scale 0.01 -> {demoEnd:F0} s) stays under the configured floor, so the " +
                  $"floor is what applies - the relation holds at both ends of the scale");

            // A NEGATIVE or absurd margin cannot shorten the gate below the predecessor's end time.
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, predDuration, -500.0) == predDuration
                  && TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, predDuration, double.NaN) == predDuration,
                  "a negative or non-finite margin is treated as zero - never as a reason to expire early");
        }

        // (b9) Vrf:DurationScale ARITHMETIC (review item 8). One function scales both halves of the
        //      order's clock; if it ever disagreed with itself, M1's relation would break silently.
        Check(ref failures,
              TaskDispatchPolicy.ScaleOrderMs(4800000L, 1.0) == 4800000L
              && TaskDispatchPolicy.ScaleOrderMs(4800000L, 0.01) == 48000L
              && TaskDispatchPolicy.ScaleOrderMs(7200000L, 0.5) == 3600000L,
              "Vrf:DurationScale scales an authored time as written (1.0, 0.01, 0.5)");
        Check(ref failures,
              TaskDispatchPolicy.ScaleOrderMs(0L, 0.5) == 0L
              && TaskDispatchPolicy.ScaleOrderMs(-5L, 1.0) == 0L,
              "a zero or negative authored time is not a time: 0 in, 0 out, at any scale");

        // (b10) ... AND ITS BOUNDS (m8). Zero, negative, NaN and the infinities are configuration
        //       errors, not instructions - one scale must not mean "no end time" on one half of the
        //       order's clock and "dispatch now" on the other.
        Check(ref failures,
              !TaskDispatchPolicy.IsUsableDurationScale(0.0)
              && !TaskDispatchPolicy.IsUsableDurationScale(-1.0)
              && !TaskDispatchPolicy.IsUsableDurationScale(double.NaN)
              && !TaskDispatchPolicy.IsUsableDurationScale(double.PositiveInfinity)
              && !TaskDispatchPolicy.IsUsableDurationScale(double.NegativeInfinity)
              && TaskDispatchPolicy.IsUsableDurationScale(1.0)
              && TaskDispatchPolicy.IsUsableDurationScale(0.001),
              "Vrf:DurationScale must be finite and greater than zero (0, negative, NaN, +/-Inf are refused)");
        Check(ref failures,
              TaskDispatchPolicy.ScaleOrderMs(4800000L, 0.0) == 4800000L
              && TaskDispatchPolicy.ScaleOrderMs(4800000L, double.NaN) == 4800000L,
              "... and a refused scale leaves the authored time UNCHANGED - the order as written, " +
              "never a task that ends at once");

        // (b11) THE ORDER'S DURATION FORMATS (review item 8). findTotalIsoMs is a strict
        //       fixed-shape decoder, and R4 now decides completion on its output, so what it
        //       ACCEPTS and what it REFUSES both matter.
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M00DT01H20M00S") == 4800000L
                         && OrderParser.FindTotalIsoMs("P00Y00M00DT02H00M00S") == 7200000L,
              "the two COA-STP1 Durations decode to 4,800 s and 7,200 s");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y01M00DT00H00M00S") == 2592000000L,
              $"m4: ONE MONTH is 30 DAYS ({OrderParser.FindTotalIsoMs("P00Y01M00DT00H00M00S") / 3600000.0:F0} h), " +
              "not the C++'s 30 hours - R4 decides completion on this number now");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M01DT00H00M00S") == 86400000L
                         && OrderParser.FindTotalIsoMs("P01Y00M00DT00H00M00S") == 31536000000L,
              "the day and year terms are unchanged (nominal 24 h and 365 d)");
        Check(ref failures, OrderParser.FindTotalIsoMs("PT1H20M") == -1L,
              "the SHORT ISO-8601 form PT1H20M is REFUSED (-1), not silently read as zero");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M00D01H20M00S") == -1L,
              "a Duration with no 'T' separator is REFUSED (-1)");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M00DT01H20M00.5S") == -1L,
              "FRACTIONAL seconds are REFUSED (-1) rather than truncated");
        Check(ref failures, OrderParser.FindTotalIsoMs(null) == -1L
                         && OrderParser.FindTotalIsoMs("") == -1L
                         && OrderParser.FindTotalIsoMs("1H20M") == -1L,
              "null, empty and a string that does not start with 'P' are REFUSED (-1)");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M00DT-01H00M00S") < 0L,
              "a NEGATIVE term yields a negative total, which the parser clamps and warns about");

        // (b12) ... and the PARSER's contract on top of it: an unreadable Duration must not pass
        //       as "no Duration", because those two have different consequences (a warning and no
        //       end time, vs a task that is supposed to have one).
        {
            var bad = OrderParser.Parse(TimedOrderXml(duration: "PT1H20M", startIso: null));
            Check(ref failures, bad.Tasks.Count == 1 && bad.Tasks[0].DurationMs == 0
                             && bad.Warnings.Any(w => w.Contains("PT1H20M")),
                  $"a Duration that is PRESENT but unreadable gives DurationMs=0 AND a warning naming it " +
                  $"({bad.Warnings.Count} warning(s))");
            var good = OrderParser.Parse(TimedOrderXml(duration: "P00Y00M00DT01H20M00S", startIso: null));
            Check(ref failures, good.Tasks[0].DurationMs == 4800000L && good.Warnings.Count == 0,
                  "... and a readable one gives the authored milliseconds with no warning");
        }

        // (b13) THE ABSOLUTE StartTime (review item 8). STP exports the SimulationTime delay form,
        //       so this path has never run on a real order - which is exactly why it is checked
        //       here: an order from another producer that DATES its tasks must not be dispatched
        //       immediately.
        {
            var parsed = OrderParser.Parse(TimedOrderXml(duration: "P00Y00M00DT01H20M00S",
                                                         startIso: "2026-09-14T12:00:00Z"));
            var t = parsed.Tasks[0];
            Check(ref failures,
                  t.AbsoluteStartUtc is DateTime abs
                  && abs == new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc)
                  && abs.Kind == DateTimeKind.Utc
                  && t.SimulationStartMs == 0,
                  $"an ABSOLUTE StartTime is lifted as a UTC instant (got {t.AbsoluteStartUtc:O}) and leaves " +
                  $"the relative delay at {t.SimulationStartMs} ms");
            var noStart = OrderParser.Parse(TimedOrderXml(duration: "P00Y00M00DT01H20M00S", startIso: null));
            Check(ref failures, noStart.Tasks[0].AbsoluteStartUtc is null,
                  "... and a task with no StartTime has none (it dispatches when its gate opens)");
        }
    }

    /// <summary>A minimal, schema-shaped order carrying a Duration and, optionally, an ABSOLUTE
    /// StartTime (TimeInstantType/DateTime/IsoDateTime) - the form STP does not export.</summary>
    private static string TimedOrderXml(string duration, string startIso) =>
        "<OrderBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">"
        + "<OrderID>rulings-selftest-timing</OrderID>"
        + "<Task><ManeuverWarfareTask>"
        + "<Name>T_Timed</Name>"
        + "<UUID>77777777-7777-7777-7777-777777777777</UUID>"
        + "<PerformingEntity>88888888-8888-8888-8888-888888888888</PerformingEntity>"
        + "<TaskActionCode>SECURE</TaskActionCode>"
        + "<Duration><IsoTimeDuration>" + duration + "</IsoTimeDuration></Duration>"
        + (startIso == null ? ""
           : "<StartTime><DateTime><IsoDateTime>" + startIso + "</IsoDateTime></DateTime></StartTime>")
        + "</ManeuverWarfareTask></Task>"
        + "</OrderBody>";

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

        var fixedFrozen = WalkTaskClock(frozen, useConfirmedMode: true, honourStale: true, dur, 400,
                                        backEndPresent: false);
        Check(ref failures, fixedFrozen.Completed && fixedFrozen.StaleTransitions == 1,
              $"a frozen sim clock with NO back end is detected ONCE ({fixedFrozen.StaleTransitions} " +
              $"transition(s)) and the task completes on the WALL fallback ({fixedFrozen.Samples} samples)");

        // (e2b) Q5 (USER RULING 2026-09-14): A PAUSED SCENARIO DOES NOT AGE A TASK. The same frozen
        //       reader, but a back end is still present - which is what a PAUSE looks like from
        //       here. M4's wall fallback burned a coffee break off every armed Duration; the axis
        //       must now hold instead. (The limit of the signal is documented on
        //       StallPolicy.TaskClockAction: BackendCount cannot tell a paused back end from a
        //       deactivated one, which is why the service repeats the hold line.)
        {
            var paused = WalkTaskClock(frozen, useConfirmedMode: true, honourStale: true, dur, 600,
                                       backEndPresent: true);
            Check(ref failures, !paused.Completed && paused.AxisSeconds == 0.0 && paused.Samples == 600,
                  $"(Q5) a flat sim clock with a back end still present ages a {dur:F0} s task by NOTHING " +
                  $"across 600 wall seconds (axis {paused.AxisSeconds:F0} s, completed={paused.Completed})");
            Check(ref failures,
                  StallPolicy.TaskClockAction(heldOnSim: true, stale: true, backEndPresent: true)
                      == StallPolicy.TaskClockOnFlat.HoldOnSim
                  && StallPolicy.TaskClockAction(heldOnSim: true, stale: true, backEndPresent: false)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall
                  && StallPolicy.TaskClockAction(heldOnSim: true, stale: false, backEndPresent: false)
                      == StallPolicy.TaskClockOnFlat.ServeSim
                  && StallPolicy.TaskClockAction(heldOnSim: false, stale: false, backEndPresent: true)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall,
                  "(Q5) HOLD only while a back end is there; no back end falls to WALL, a running clock is " +
                  "served, and Vrf:TaskClock=wall is unaffected");
        }

        // (e2c) ... and the HOLD is not a freeze: once the scenario runs again the task completes
        //       on the SIM clock, having aged by nothing in between.
        {
            // Flat for 200 s (well past the 60 s stale window), then advancing again.
            Func<int, double> pausedThenRunning = i => i < 200 ? 5000.0 : 5000.0 + (i - 199);
            var resumed = WalkTaskClock(pausedThenRunning, useConfirmedMode: true, honourStale: true,
                                        dur, 600, backEndPresent: true);
            Check(ref failures, resumed.Completed && resumed.Samples == 200 + (int)dur,
                  $"(Q5) ... and when the scenario runs again the task completes after its {dur:F0} SIM " +
                  $"seconds, not counting the pause (completed at sample {resumed.Samples})");
        }

        // (e2d) E1 (pass-3 review): THE WAY OUT OF A HOLD IS NOT ALWAYS A RECOVERY, and the service
        //       said it was. Its "the simulation clock is readable and advancing again" branch fires
        //       on (already warned) && !taskSimStale, and taskSimStale is heldOnSim && obs.Stale -
        //       so it fires just as surely when heldOnSim goes FALSE, which is the hysteresis path
        //       ONTO the wall clock. Those are opposite events. The sequence that reaches it is
        //       below, on the real SimClockTracker and the real StallPolicy; what the service says
        //       at each of the two exits is a log line and is checked by reading.
        {
            var tracker = new SimClockTracker();
            double wall = 1.7e9;
            SimClockTracker.Observation obs = default;
            for (int i = 0; i < 120; i++, wall += 1.0)          // readable, FLAT, back end present
                obs = tracker.Observe(5000.0, wall, StallPolicy.ModeSwitchConfirmations,
                                      StallPolicy.StaleClockWarnSeconds);
            bool heldOnSim = obs.ReadableConfirmed;
            Check(ref failures,
                  heldOnSim && obs.Stale
                  && StallPolicy.TaskClockAction(heldOnSim, heldOnSim && obs.Stale, backEndPresent: true)
                         == StallPolicy.TaskClockOnFlat.HoldOnSim,
                  $"(e2d) E1: {obs.FlatForWallSeconds:F0} wall seconds of a readable-but-flat clock with a " +
                  $"back end present is a HOLD - the state the service warns about and then has to get OUT of");

            for (int i = 0; i < StallPolicy.ModeSwitchConfirmations; i++, wall += 1.0)   // the reader goes
                obs = tracker.Observe(-1.0, wall, StallPolicy.ModeSwitchConfirmations,
                                      StallPolicy.StaleClockWarnSeconds);
            heldOnSim = obs.ReadableConfirmed;
            bool taskSimStale = heldOnSim && obs.Stale;
            Check(ref failures,
                  !heldOnSim && !taskSimStale
                  && StallPolicy.TaskClockAction(heldOnSim, taskSimStale, backEndPresent: true)
                         == StallPolicy.TaskClockOnFlat.FallBackToWall,
                  "(e2d) E1: ... and LOSING THE READER clears taskSimStale exactly as a recovery does, while " +
                  "the axis goes to the WALL clock - so one sentence for both announced \"readable and " +
                  "advancing again\" at the tick every task time left the sim clock. The service now branches " +
                  "on heldOnSim and says which of the two happened");
        }

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
    /// <param name="backEndPresent">Q5: what VrfFacade::BackendCount would say. True = a back end
    /// is still there, so a flat clock is a PAUSE and the axis holds.</param>
    private static (bool Completed, int Samples, double AxisSeconds, int ModeFlips, int StaleTransitions)
        WalkTaskClock(Func<int, double> reader, bool useConfirmedMode, bool honourStale,
                      double durationSeconds, int maxSamples, bool backEndPresent = false)
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
            // The SERVICE's own decision (Q5), not a copy of it: SampleTaskClock calls this.
            var action = StallPolicy.TaskClockAction(heldOnSim, stale, backEndPresent);
            bool usingSim = action != StallPolicy.TaskClockOnFlat.FallBackToWall;
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

        // (c4b) Q4 (USER RULING 2026-09-14): NO DURATION **AND** NO GEOMETRY IS MALFORMED, AND IS
        //       REFUSED. The supervisor default invented Vrf:DefaultHoldSeconds (60 s) so the chain
        //       would proceed; the user ruled that a number which is not in the order is not ours
        //       to invent. The rule is scoped to the kind that issues NO vendor task at all.
        {
            Check(ref failures,
                  TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.ExecuteInPlace, 0L)
                  && TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.ExecuteInPlace, -1L)
                  && !TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.ExecuteInPlace, 4800_000L),
                  "Q4: a zero-geometry task with no Duration is MALFORMED; the same task WITH a Duration is not");
            Check(ref failures,
                  !TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.EngageInPlace, 0L)
                  && !TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.BreachInPlace, 0L)
                  && !TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.Refuse, 0L),
                  "Q4: an ENGAGE or BREACH in place is NOT malformed without a Duration - it has a resolved " +
                  "target, so the vendor reports when it is done");
            Check(ref failures,
                  TaskDispatchPolicy.MalformedZeroGeometryRefusal
                      == "MALFORMED: the order gives this task NEITHER a Duration NOR any geometry (no " +
                         "MapGraphicID and no Location), so nothing in the order could ever end it and " +
                         "nothing in the simulation could ever evidence it",
                  "Q4: the refusal names BOTH missing elements, so the order can be fixed from the report");

            // FAIL-FIRST + the new behaviour, on the real sequencer: the OLD code armed an invented
            // 60 s hold and released the successor at it; the NEW code abandons the task, and the
            // successor is skipped at once with its own TASKABRT.
            var seqOld = new TaskSequencer();
            var timedOld = new TimedCompletionPolicy();
            const string malformed = "T_MALFORMED";
            seqOld.NotifyDispatched(malformed, TaskClock.Wall.Now());   // the axis the gate below reads
            Check(ref failures,
                  timedOld.Register(malformed, "taskee", malformed, "A/6-56 ADA", 60.0),
                  "Q4 FAIL-FIRST: the pre-ruling code armed an INVENTED 60 s end time for a malformed task");
            var oldSuccessor = seqOld.WaitForStartAsync(malformed, 0, 0, 600.0, TaskClock.Wall,
                                                        CancellationToken.None, 86400.0);
            timedOld.Advance(0.0, usingSim: false);
            foreach (var d in timedOld.Advance(60.0, usingSim: false)) seqOld.CompleteTask(d.TaskUuid);
            Check(ref failures,
                  oldSuccessor.Wait(TimeSpan.FromSeconds(2)) && oldSuccessor.Result == GateResult.Proceed,
                  "Q4 FAIL-FIRST: ... and its successor then dispatched on a completion the ORDER never " +
                  "authorised - the chain ran on an invented number");

            var seq = new TaskSequencer();
            var successor = seq.WaitForStartAsync(malformed, 0, 0, 600.0, TaskClock.Wall,
                                                  CancellationToken.None, 86400.0);
            Thread.Sleep(50);
            Check(ref failures, !successor.IsCompleted, "Q4: the successor waits while nothing has happened");
            seq.NotifyAbandoned(malformed);          // what the refusal now does, beside the TASKABRT
            Check(ref failures,
                  successor.Wait(TimeSpan.FromSeconds(2))
                  && successor.Result == GateResult.PredecessorAbandoned,
                  "Q4: the refusal ABANDONS the malformed task, so its successor is skipped immediately " +
                  "with its own TASKABRT rather than waiting out the gate");
        }

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

        // (d2) WHERE EACH RESOLUTION SENDS THE TASK. The old check here asserted that
        //      RefusesForTarget is false for every enum value while the method body IS
        //      "return false" - it could not fail, and n8 of the cold-start review of 5c67d41
        //      called it out. What is worth checking is the MAPPING, which has four arms and can
        //      be got wrong: exactly ONE resolution names an entity to VR-Forces, and the other
        //      three route the task to its own geometry.
        var toGeometry = Enum.GetValues<TargetResolution>()
                             .Where(TaskDispatchPolicy.FallsBackToGeometry).ToArray();
        Check(ref failures,
              toGeometry.Length == 3
              && toGeometry.Contains(TargetResolution.SelfIsObjective)
              && toGeometry.Contains(TargetResolution.Unresolved)
              && toGeometry.Contains(TargetResolution.NoTarget)
              && !toGeometry.Contains(TargetResolution.DistinctEntity),
              $"exactly one target resolution (DistinctEntity) is named to VR-Forces; the other " +
              $"{toGeometry.Length} route the task to its own geometry");

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
        const string cpTango = "12121212-3434-5656-7878-909090909090";
        var graphics = new Dictionary<string, TaskGraphic>(StringComparer.Ordinal)
        {
            [objMadison] = new TaskGraphic(objMadison, "OBJ_MADISON", TaskGraphic.KindArea,
                new[] { (34.4, -116.6, (double?)null), (34.6, -116.6, (double?)null),
                        (34.6, -116.4, (double?)null), (34.4, -116.4, (double?)null) }),
            // M5: the init's 41 LINEs and 317 POINTs are registered for R1 resolution too, and
            // INDEPENDENTLY of Vrf:CreateInitLines / Vrf:CreateInitPoints - this map holds
            // authored points, not VR-Forces objects.
            [plBlue] = new TaskGraphic(plBlue, "PL_BLUE", TaskGraphic.KindLine,
                new[] { (35.0, -117.0, (double?)null), (35.1, -117.1, (double?)null) }),
            [cpTango] = new TaskGraphic(cpTango, "CP_TANGO", TaskGraphic.KindPoint,
                new[] { (36.25, -118.75, (double?)null) }),
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
            Check(ref failures, r.Warnings.Any(l => l.Contains("matched NO graphic in the initialization"))
                             && r.Log.Any(l => l.Contains("geometry from embedded Location")
                                            && l.Contains("STP-801")),
                  "... and WARNS that the id matched nothing (M5), with the STP-801 marker on the fallback");
        }

        // (e7) M5 - A MapGraphicID NAMING A LINE resolves to that line's vertices, in order, with
        //      no area centroid collapse: a phase line or an axis of advance is a PATH.
        {
            var task = new OrderTask
            {
                TaskName = "T_Move_Along_PL_BLUE",
                MapGraphicUuids = new[] { plBlue },
                Points = new List<(double, double, double?)>(),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.MapGraphic && r.Points.Count == 2
                             && Math.Abs(r.Points[0].Lat - 35.0) < 1e-9
                             && Math.Abs(r.Points[1].Lat - 35.1) < 1e-9
                             && r.Warnings.Count == 0,
                  $"a MapGraphicID naming a LINE resolves to its {r.Points.Count} vertices in order " +
                  $"(source {r.Source})");
            Check(ref failures, r.Log.Any(l => l.Contains("PL_BLUE") && l.Contains(TaskGraphic.KindLine)),
                  "... and names the line and its kind in the log");
        }

        // (e8) M5 - A MapGraphicID NAMING A POINT resolves to that one authored position.
        {
            var task = new OrderTask
            {
                TaskName = "T_Occupy_CP_TANGO",
                MapGraphicUuids = new[] { cpTango },
                Points = new List<(double, double, double?)>(),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.MapGraphic && r.Points.Count == 1
                             && Math.Abs(r.Points[0].Lat - 36.25) < 1e-9
                             && Math.Abs(r.Points[0].Lon + 118.75) < 1e-9,
                  $"a MapGraphicID naming a POINT resolves to its authored position (source {r.Source}, " +
                  $"{r.Points.Count} point(s))");
        }

        // (e9) M5 - THE SILENT CASE THE WARNING EXISTS FOR: an unmatched id and NO embedded
        //      Location. Before M5 this fell through to R2 in place with an Information line; the
        //      unit holds still for the whole Duration and the order looks executed.
        {
            var task = new OrderTask
            {
                TaskName = "T_Only_An_Unknown_Graphic",
                MapGraphicUuids = new[] { "00000000-0000-0000-0000-000000000000" },
                Points = new List<(double, double, double?)>(),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.None
                             && r.Warnings.Any(l => l.Contains("matched NO graphic"))
                             && r.Warnings.Any(l => l.Contains("executed IN PLACE")),
                  $"an unmatched MapGraphicID with no embedded Location WARNS that the task will be " +
                  $"executed in place ({r.Warnings.Count} warning(s))");
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


    // ------------------------------------------- THE STREND CHAIN TOPOLOGY ----
    // A1 (cold-start review of `0c96f50`, pass 2). A GATE IS A GRAPH, NOT A PAIR. Every other
    // check in this file looks at ONE gate whose predecessor is ALREADY DISPATCHED, so phase 1
    // of the gate - "the predecessor must dispatch at all" - was never exercised and a defect
    // worth half the order survived a green suite. COA-STP1's 42 tasks are 11 SERIAL CHAINS up
    // to four tasks deep; `HandleOrder` fires every task's orchestration in ONE loop, so all 42
    // gates start waiting at ORDER RECEIPT. Phase 1's window was the same window phase 2 uses -
    // derived from the predecessor's own DURATION - and it therefore knew nothing about the
    // predecessor's LEAD TIME: a d2 task demanded that its predecessor dispatch within 4,860 s
    // while that predecessor was itself waiting out a 7,200 s root, so 21 of the 42 tasks were
    // skipped with TASKABRT at every shipped setting.
    //
    // THE RULE THESE CHECKS LOCK: when the predecessor NAMES A TASK IN THIS ORDER, phase 1 has
    // no timeout of its own - every dispatch dead end calls NotifyAbandoned, so a successor
    // still fails FAST on a real one - and only a generous absolute backstop
    // (Vrf:TaskChainBackstopSeconds) bounds it. A DANGLING predecessor reference keeps the
    // configured window, because nothing will ever abandon a task that does not exist.
    //
    // Everything below runs the REAL TaskSequencer, TimedCompletionPolicy and TaskDispatchPolicy
    // over the WHOLE graph on one monotone clock, reproducing MarkDispatched's order
    // (NotifyDispatched, then Register, then the anchoring walk). The PRE-FIX rule - phase 1
    // measured from order receipt on the completion window - is kept beside each case as the
    // FAIL-FIRST control, because it is exactly what the sequencer does when the caller hands it
    // the same number twice.
    private static void ChainTopology(ref int failures)
    {
        const double configured = 600.0;       // the shipped Vrf:TaskPredecessorTimeoutSeconds
        const double demoConfigured = 7200.0;  // appsettings.Demo.json's overlay
        const double margin = 60.0;            // the shipped Vrf:TaskPredecessorEndMarginSeconds
        const double backstop = 86400.0;       // the shipped Vrf:TaskChainBackstopSeconds
        // Every authored time in COA-STP1 (4,800 s, 7,200 s, 12,000 s) is a whole multiple of
        // 1,200 s, and of 120 s once scaled by 0.05, so a walk in those steps lands EXACTLY on
        // every end time instead of observing it late - the walk's granularity never eats the
        // gate's margin.
        const double step = 1200.0;
        const double compressedStep = 120.0;

        // (f1) A FOUR-DEEP CHAIN, every gate started at order receipt. This is COA-STP1's shape:
        //      a PT2H root and three PT1H20M successors on one taskee.
        {
            var chain = SerialChain(7200_000L, 4800_000L, 4800_000L, 4800_000L);
            var preFix = WalkChain(chain, configured, margin, 1.0, backstop, step, preFixPhase1: true);
            Check(ref failures,
                  preFix.Dispatched == 2 && preFix.SkippedCount == 2
                  && preFix.Result("T3") == GateResult.PredecessorNeverDispatched,
                  $"FAIL-FIRST (A1): with phase 1 measured from order receipt on the COMPLETION window, " +
                  $"the same chain dispatches only {preFix.Dispatched} of 4 - T3's 4,860 s window expires " +
                  $"2,340 s before T2 dispatches, and T4 dies on T3's abandon " +
                  $"({preFix.Times("T1", "T2", "T3", "T4")})");

            var run = WalkChain(chain, configured, margin, 1.0, backstop, step);
            Check(ref failures,
                  run.Dispatched == 4 && run.SkippedCount == 0
                  && run.At("T1") == 0.0 && run.At("T2") == 7200.0
                  && run.At("T3") == 12000.0 && run.At("T4") == 16800.0,
                  $"(i) A1: a 4-deep chain whose gates all start at ORDER RECEIPT dispatches all four " +
                  $"at their predecessors' TIMED completions - expected 0 / 7200 / 12000 / 16800, got " +
                  $"{run.Times("T1", "T2", "T3", "T4")} ({run.SkippedCount} skipped)");

            var demo = WalkChain(chain, demoConfigured, margin, 1.0, backstop, step);
            Check(ref failures, demo.Dispatched == 4 && demo.SkippedCount == 0,
                  $"(i) ... and the same chain under the Demo overlay's 7200 s floor: " +
                  $"{demo.Dispatched} of 4 dispatched, {demo.SkippedCount} skipped");
        }

        // (f2) A DELAYED ROOT. COA-STP1's T13 carries a PT3H20M start delay (12,000 s) and one
        //      successor, T14, whose window is derived from T13's 4,800 s Duration - 7,140 s
        //      before T13 dispatches at all.
        {
            var pair = new[] { new ChainTask("T13", "", 4800_000L, 12000_000L),
                               new ChainTask("T14", "T13", 4800_000L, 0L) };
            var preFix = WalkChain(pair, configured, margin, 1.0, backstop, step, preFixPhase1: true);
            Check(ref failures,
                  preFix.Dispatched == 1 && preFix.Result("T14") == GateResult.PredecessorNeverDispatched,
                  $"FAIL-FIRST (A1): with the pre-fix rule T14's 4,860 s window expires 7,140 s before T13 " +
                  $"dispatches ({preFix.Times("T13", "T14")})");

            var run = WalkChain(pair, configured, margin, 1.0, backstop, step);
            Check(ref failures,
                  run.Dispatched == 2 && run.SkippedCount == 0
                  && run.At("T13") == 12000.0 && run.At("T14") == 16800.0,
                  $"(ii) A1: T13's 12,000 s start delay does not skip T14 - expected 12000 / 16800, got " +
                  $"{run.Times("T13", "T14")} ({run.SkippedCount} skipped)");
        }

        // (f3) A DANGLING predecessor reference - a uuid no task in this order carries. NOTHING
        //      will ever dispatch or abandon it, so this is the ONE case the configured window
        //      still has to bound, and it must still expire at exactly that value.
        {
            var dangling = new[] { new ChainTask("T1", "no-such-task-uuid", 4800_000L, 0L) };
            var run = WalkChain(dangling, configured, margin, 1.0, backstop, 60.0);
            Check(ref failures,
                  run.Dispatched == 0 && run.SkippedCount == 1
                  && run.Result("T1") == GateResult.PredecessorNeverDispatched
                  && run.SkippedAtSeconds("T1") == configured,
                  $"(iii) a DANGLING predecessor still times out at the configured " +
                  $"{configured:F0} s (got {run.Times("T1")} at {run.SkippedAtSeconds("T1"):F0} s)");
        }

        // (f4) AN ABANDONED predecessor must still fail its successor FAST. This is what pays for
        //      dropping phase 1's timeout: every dispatch dead end in the service calls
        //      NotifyAbandoned, so the backstop is never what ends a chain that really died.
        {
            var clock = new StepClock();
            var seq = new TaskSequencer();
            var gate = seq.WaitForStartAsync("PRED-A1", 0, 0, backstop, clock.AsTaskClock(),
                                             CancellationToken.None);
            Thread.Sleep(50);
            Check(ref failures, !gate.IsCompleted,
                  "(iv) a gate whose predecessor has NOT dispatched waits (it does not proceed on its own)");
            seq.NotifyAbandoned("PRED-A1");
            bool done = gate.Wait(TimeSpan.FromSeconds(5));
            Check(ref failures, done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                  $"(iv) ... and an ABANDONED predecessor fails it FAST - 0 s of clock spent, not the " +
                  $"{backstop:F0} s backstop (got {(done ? gate.Result.ToString() : "still waiting")} at " +
                  $"{clock.Now:F0} s)");
        }

        // (f6) A SUPERSEDED predecessor kills its successors AT THE SUPERSEDE POINT (B1 of the
        //      pass-2 review; Q1, USER RULING 2026-09-14). VR-Forces runs one task per unit, so a
        //      new dispatch REPLACES the running one and the interface reports TASKABRT for it -
        //      and until this fix said nothing to the gate, so the successors of a task it had
        //      just declared NOT PERFORMED waited out the whole derived window (up to 7,260 s)
        //      before being skipped anyway.
        {
            Check(ref failures,
                  TaskDispatchPolicy.SupersedeAbandonsSuccessors("TASKABRT")
                  && TaskDispatchPolicy.SupersedeAbandonsSuccessors(null)
                  && TaskDispatchPolicy.SupersedeAbandonsSuccessors("")
                  && !TaskDispatchPolicy.SupersedeAbandonsSuccessors("TASKCMPLT")
                  && !TaskDispatchPolicy.SupersedeAbandonsSuccessors("  taskcmplt  "),
                  "(vi) the DEFAULT supersede code abandons the successors; TASKCMPLT - the reading that " +
                  "keeps the armed end time - does not, at any casing or spacing");

            var clock = new StepClock();
            var seq = new TaskSequencer();
            var timed = new TimedCompletionPolicy();
            const string pred = "PRED-SUPERSEDED";
            seq.NotifyDispatched(pred, clock.Now);
            timed.Register(pred, "taskee", pred, "1-35 AR", 4800.0);
            timed.Advance(clock.Now, usingSim: true);
            double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, 4800.0, margin);
            var gate = seq.WaitForStartAsync(pred, 0, 0, window, clock.AsTaskClock(),
                                             CancellationToken.None, backstop);
            Thread.Sleep(50);
            Check(ref failures, !gate.IsCompleted && window >= 4860.0,
                  $"(vi) FAIL-FIRST: while the supersede says nothing to the gate, the successor is still " +
                  $"waiting out its {window:F0} s window (the pre-B1 behaviour)");

            // What MarkDispatched now does at the supersede point, in order: report TASKABRT for
            // the old task (which cancels its armed end time) and tell the sequencer it is dead.
            Check(ref failures, TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT)
                             && timed.Cancel(pred),
                  "(vi) the supersede TASKABRT cancels the old task's armed end time");
            seq.NotifyAbandoned(pred);
            bool done = gate.Wait(TimeSpan.FromSeconds(5));
            Check(ref failures, done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                  $"(vi) ... and the successor is skipped IMMEDIATELY - PredecessorAbandoned with 0 s of " +
                  $"clock spent, which the service reports as its own TASKABRT (got " +
                  $"{(done ? gate.Result.ToString() : "still waiting")} at {clock.Now:F0} s)");
            Check(ref failures, timed.Advance(clock.Now + 100000.0, usingSim: true).Count == 0,
                  "(vi) ... and the superseded task never reports a later timed TASKCMPLT");
        }

        // (f7) B4: THE LONG WINDOW IS DERIVED FROM AN END TIME THAT MUST ACTUALLY EXIST. With
        //      Vrf:TimedCompletion OFF - the documented evidence-only escape hatch - nothing is
        //      ever armed, so a window derived from the predecessor's Duration only made the
        //      eventual skip eight times slower and quieter than the operator had configured.
        {
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorEndSeconds(false, true, 4800_000L, 1.0) == 0.0
                  && TaskDispatchPolicy.PredecessorEndSeconds(true, true, 4800_000L, 1.0) == 4800.0
                  && TaskDispatchPolicy.PredecessorTimeoutSeconds(
                         configured, TaskDispatchPolicy.PredecessorEndSeconds(false, true, 4800_000L, 1.0),
                         margin) == configured,
                  "(vii) B4: with Vrf:TimedCompletion OFF the predecessor has no armed end time, so the gate " +
                  "is the configured floor; with it ON the Duration raises it");

            var chain = SerialChain(7200_000L, 4800_000L, 4800_000L, 4800_000L);
            var preB4 = WalkChain(chain, configured, margin, 1.0, backstop, 60.0,
                                  timedCompletion: false, derivesFromDuration: true);
            Check(ref failures,
                  preB4.Dispatched == 1 && preB4.SkippedAtSeconds("T2") == 7260.0,
                  $"(vii) FAIL-FIRST (B4): deriving the window anyway makes the skip wait {preB4.SkippedAtSeconds("T2"):F0} s " +
                  $"for a completion that can never come ({preB4.Dispatched} of 4 dispatched)");

            var run = WalkChain(chain, configured, margin, 1.0, backstop, 60.0, timedCompletion: false);
            Check(ref failures,
                  run.Dispatched == 1 && run.SkippedAtSeconds("T2") == configured
                  && run.Result("T2") == GateResult.PredecessorTimeout
                  && run.Result("T3") == GateResult.PredecessorAbandoned,
                  $"(vii) ... and with the fix the same run gives up at the CONFIGURED " +
                  $"{configured:F0} s (got {run.SkippedAtSeconds("T2"):F0} s), which is what the operator asked for");
        }

        // (f8) B7: THE LOG HAS TO SAY WHICH TIMEOUT IT WAS. Live gate 5 is specified as a log
        //      check, so the wording is part of the contract - and one sentence for both timeouts
        //      reported every A1 skip against a dispatch that had never happened, quoting a window
        //      that was not the one that expired.
        {
            Check(ref failures,
                  TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorNeverDispatched, 86400.0, 4860.0)
                      == "never dispatched within 86400s of order receipt",
                  "(viii) a PHASE-1 timeout says the predecessor NEVER DISPATCHED, and quotes the DISPATCH " +
                  "window measured from order receipt");
            Check(ref failures,
                  TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorTimeout, 86400.0, 4860.0)
                      == "did not complete within 4860s of its dispatch",
                  "(viii) a PHASE-2 timeout says it did not COMPLETE, and quotes the COMPLETION window " +
                  "measured from its dispatch");
            Check(ref failures,
                  TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorAbandoned, 86400.0, 4860.0)
                      == "was skipped/abandoned upstream",
                  "(viii) an ABANDONED predecessor is neither, and names no window at all");
            Check(ref failures,
                  TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorNeverDispatched, 86400.0, 4860.0)
                      != TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorTimeout, 86400.0, 4860.0),
                  "(viii) ... and the two timeouts can never print the same sentence");
        }

        // (f9) D1 (pass-3 review of `8db033e`): A DISPATCH THAT DIES ON THE TICK THREAD MUST STILL
        //      END ITS TASK. The bridge work is enqueued onto the VR-Forces tick thread, and the
        //      drain's only handler logs and returns - so a throw there told the sequencer nothing
        //      and STP nothing, and since A1 the successors then sat on the 86,400 s chain
        //      backstop rather than the 4,860 s window. `b76c9c7` gave the FIRST pass the proper
        //      ending; the re-entry that really dispatches a ground move in the DEFAULT
        //      TerrainProfile mode did not have it.
        //
        //      WHAT IS DRIVEN HERE: the production DeferredDispatch.Run, the production
        //      TaskSequencer and the production TaskStatusPolicy. The one stand-in is
        //      PushTaskStatus, which needs a C2SIM server - so `Abort` below is its ONE deciding
        //      line (consult TaskStatusPolicy.ShouldEmit, emit only when it says yes) and nothing
        //      else. The service glue that assembles them cannot be driven offline; the runner it
        //      calls was extracted so that it can be.
        {
            const string pred = "PRED-D1";
            Action throwing = () => throw new InvalidOperationException("the bridge call failed");

            // (a) THE THROW HAPPENS BEFORE MarkDispatched - the ordinary case, and the one where
            //     STP has been told nothing at all about this task.
            {
                var clock = new StepClock();
                var seq = new TaskSequencer();
                var status = new TaskStatusPolicy();
                var aborts = new List<string>();
                int starts = 0;
                void Emit(S.TaskStatusCodeType code, string uuid, string why)
                {
                    if (!status.ShouldEmit(code, uuid)) return;
                    if (code == S.TaskStatusCodeType.TASKABRT) aborts.Add(why);
                    else if (code == S.TaskStatusCodeType.TASKSTRT) starts++;
                }

                // The successor's gate, opened at t = 0 with NOTHING dispatched - HandleOrder's own
                // topology, and the phase-1 window A1 gives an in-order predecessor.
                double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, 4800.0, margin);
                double phase1 = TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds(true, window, backstop);
                var gate = seq.WaitForStartAsync(pred, 0, 0, window, clock.AsTaskClock(),
                                                 CancellationToken.None, phase1);

                // FAIL-FIRST (D1): the PRE-FIX ending. The bare lambda's only handler was the tick
                // drain's catch, which logs and tells NOBODY - the same runner with no sequencer and
                // no report action is exactly that.
                var preFix = DeferredDispatch.Run(throwing, pred, "T_D1", DeferredDispatch.TerrainContinuation,
                                                  sequencer: null, reportAbort: null, logError: _ => { });
                Thread.Sleep(50);
                Check(ref failures,
                      preFix is InvalidOperationException && aborts.Count == 0 && !gate.IsCompleted,
                      $"FAIL-FIRST (D1): with the pre-fix ending - the tick drain logs the throw and tells " +
                      $"nobody - the task reports NO TASKABRT (got {aborts.Count}) and its successor is STILL " +
                      $"at the gate with {phase1:F0} s of backstop left to wait");

                var thrown = DeferredDispatch.Run(throwing, pred, "T_D1", DeferredDispatch.TerrainContinuation,
                                                  seq, why => Emit(S.TaskStatusCodeType.TASKABRT, pred, why),
                                                  logError: _ => { });
                bool done = gate.Wait(TimeSpan.FromSeconds(5));
                Check(ref failures,
                      thrown is InvalidOperationException && aborts.Count == 1 && starts == 0,
                      $"(ix) D1: a continuation that THROWS reports exactly ONE TASKABRT (got {aborts.Count}) " +
                      $"and NO TASKSTRT (got {starts}) - a task that never started must not report started");
                Check(ref failures,
                      done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                      $"(ix) ... and its successor fails FAST - PredecessorAbandoned at 0 s of clock, not the " +
                      $"{phase1:F0} s backstop (got {(done ? gate.Result.ToString() : "still waiting")} at " +
                      $"{clock.Now:F0} s)");
                Check(ref failures,
                      aborts.Count == 1 && aborts[0].Contains("InvalidOperationException")
                      && aborts[0].Contains(DeferredDispatch.TerrainContinuation),
                      $"(ix) ... and the one TASKABRT names the exception TYPE and WHERE it died " +
                      $"(got \"{(aborts.Count == 1 ? aborts[0] : "-")}\")");

                // The reply and the timeout sweep can both reach a continuation; a second ending for
                // the same task must not produce a second report.
                DeferredDispatch.Run(throwing, pred, "T_D1", DeferredDispatch.TerrainContinuation,
                                     seq, why => Emit(S.TaskStatusCodeType.TASKABRT, pred, why),
                                     logError: _ => { });
                Check(ref failures, aborts.Count == 1,
                      $"(ix) ... and a SECOND failure of the same task adds no second TASKABRT (got {aborts.Count})");
            }

            // (b) THE THROW HAPPENS AFTER MarkDispatched - the route was created, the bridge was
            //     called, TASKSTRT went out, and then something threw. The ending must not announce
            //     a second start, and must still be exactly one TASKABRT.
            {
                const string half = "PRED-D1-HALF";
                var clock = new StepClock();
                var seq = new TaskSequencer();
                var status = new TaskStatusPolicy();
                var aborts = new List<string>();
                int starts = 0;
                void Emit(S.TaskStatusCodeType code, string uuid, string why)
                {
                    if (!status.ShouldEmit(code, uuid)) return;
                    if (code == S.TaskStatusCodeType.TASKABRT) aborts.Add(why);
                    else if (code == S.TaskStatusCodeType.TASKSTRT) starts++;
                }
                double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, 4800.0, margin);
                double phase1 = TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds(true, window, backstop);
                var gate = seq.WaitForStartAsync(half, 0, 0, window, clock.AsTaskClock(),
                                                 CancellationToken.None, phase1);

                // MarkDispatched's own order: NotifyDispatched, then TASKSTRT - then the throw.
                Action halfDispatched = () =>
                {
                    seq.NotifyDispatched(half, clock.Now);
                    Emit(S.TaskStatusCodeType.TASKSTRT, half, "dispatched");
                    throw new InvalidOperationException("the bridge call failed after MarkDispatched");
                };
                DeferredDispatch.Run(halfDispatched, half, "T_D1_HALF", DeferredDispatch.TerrainContinuation,
                                     seq, why => Emit(S.TaskStatusCodeType.TASKABRT, half, why),
                                     logError: _ => { });
                bool done = gate.Wait(TimeSpan.FromSeconds(5));
                Check(ref failures, starts == 1 && aborts.Count == 1,
                      $"(ix) D1: a throw AFTER MarkDispatched leaves the one TASKSTRT it had already sent " +
                      $"(got {starts}) and adds exactly one TASKABRT (got {aborts.Count}) - never a second start");
                Check(ref failures, !status.ShouldEmit(S.TaskStatusCodeType.TASKSTRT, half),
                      "(ix) ... and the task cannot announce a second start for the same execution: an ABORT " +
                      "does not re-arm TASKSTRT, only a completion does");
                Check(ref failures,
                      done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                      $"(ix) ... and the successor of a HALF-dispatched task is abandoned at 0 s of clock too " +
                      $"(got {(done ? gate.Result.ToString() : "still waiting")} at {clock.Now:F0} s)");
            }
        }

        // (f5) THE WHOLE COA-STP1 GRAPH, end to end, from the order on disk. This is the branch's
        //      own live gate 2 ("42 dispatches, not 9") decided OFFLINE.
        {
            string file = FindCoaStp1Order();
            var order = file == null ? null : OrderParser.Parse(File.ReadAllText(file));
            Check(ref failures, order != null && order.Tasks.Count == 42,
                  $"(v) data/COA-STP1_Order.xml parses to 42 tasks (got " +
                  $"{(order == null ? "NOT FOUND" : order.Tasks.Count.ToString())})");
            if (order != null && order.Tasks.Count == 42)
            {
                var graph = new List<ChainTask>();
                foreach (var t in order.Tasks)
                    graph.Add(new ChainTask(t.TaskUuid, t.StartAfterTaskUuid, t.DurationMs,
                                            Math.Max(t.SimulationStartMs, t.RelativeDelayMs)));

                var preFix = WalkChain(graph, configured, margin, 1.0, backstop, step, preFixPhase1: true);
                Check(ref failures, preFix.Dispatched == 21 && preFix.SkippedCount == 21,
                      $"FAIL-FIRST (A1): the pre-fix rule dispatches {preFix.Dispatched} of the order's 42 tasks " +
                      $"and skips {preFix.SkippedCount} with TASKABRT - the roots and their first successors run, " +
                      $"the two deeper levels and T14 do not");

                var asWritten = WalkChain(graph, configured, margin, 1.0, backstop, step);
                Check(ref failures, asWritten.Dispatched == 42 && asWritten.SkippedCount == 0,
                      $"(v) the whole COA-STP1 graph at Vrf:DurationScale=1.0 and the SHIPPED " +
                      $"{configured:F0} s floor: {asWritten.Dispatched} dispatches, " +
                      $"{asWritten.SkippedCount} skipped (must be 42 / 0)");

                var demo = WalkChain(graph, demoConfigured, margin, 1.0, backstop, step);
                Check(ref failures, demo.Dispatched == 42 && demo.SkippedCount == 0,
                      $"(v) ... and under the Demo overlay's {demoConfigured:F0} s floor: " +
                      $"{demo.Dispatched} dispatches, {demo.SkippedCount} skipped");

                // DETERMINISM. The compressed profile is where the pre-fix rule put a task on the
                // exact boundary between its window and its predecessor's dispatch, and a demo
                // that fails one run in seven is worse than one that fails every time.
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < 20; i++)
                {
                    var r = WalkChain(graph, configured, margin, 0.05, backstop, compressedStep);
                    seen.Add($"{r.Dispatched}/{r.SkippedCount}");
                }
                Check(ref failures, seen.Count == 1 && seen.Contains("42/0"),
                      $"(v) 20 repetitions at Vrf:DurationScale=0.05 all give 42 dispatches and 0 skips " +
                      $"(distinct outcomes seen: {string.Join(", ", seen)})");
            }
        }
    }

    /// <summary>One task as the gate graph sees it: its uuid, its STREND predecessor (empty for a
    /// root), its authored Duration and its authored start delay, both in milliseconds.</summary>
    private sealed record ChainTask(string Uuid, string Pred, long DurationMs, long StartDelayMs);

    /// <summary>A serial chain T1 -> T2 -> ... with the given authored Durations.</summary>
    private static List<ChainTask> SerialChain(params long[] durationsMs)
    {
        var chain = new List<ChainTask>();
        for (int i = 0; i < durationsMs.Length; i++)
            chain.Add(new ChainTask("T" + (i + 1), i == 0 ? "" : "T" + i, durationsMs[i], 0L));
        return chain;
    }

    /// <summary>What a walk of the graph produced: when each task dispatched, or why it did not.</summary>
    private sealed class ChainOutcome
    {
        public readonly Dictionary<string, double> DispatchedAt = new(StringComparer.Ordinal);
        public readonly Dictionary<string, (GateResult Result, double At)> Skipped = new(StringComparer.Ordinal);
        public int Dispatched => DispatchedAt.Count;
        public int SkippedCount => Skipped.Count;
        public double At(string uuid) => DispatchedAt.TryGetValue(uuid, out var t) ? t : double.NaN;
        public GateResult Result(string uuid) => Skipped.TryGetValue(uuid, out var s) ? s.Result : GateResult.Proceed;
        public double SkippedAtSeconds(string uuid) => Skipped.TryGetValue(uuid, out var s) ? s.At : double.NaN;
        public string Times(params string[] uuids)
        {
            var parts = new List<string>();
            foreach (var u in uuids)
                parts.Add(DispatchedAt.TryGetValue(u, out var t) ? t.ToString("F0")
                        : Skipped.TryGetValue(u, out var s) ? s.Result.ToString() : "-");
            return string.Join(" / ", parts);
        }
    }

    /// <summary>
    /// Walk a whole gate graph on one monotone clock, exactly as the service orchestrates one:
    /// every task's gate starts at t = 0 (HandleOrder's single foreach), a gate that opens is
    /// dispatched with MarkDispatched's own ordering, and a gate that does not open abandons its
    /// task so its successors fail fast. Returns when every task has dispatched or been skipped.
    /// </summary>
    /// <param name="preFixPhase1">The FAIL-FIRST control for A1: give phase 1 the same window
    /// phase 2 gets, which is what the branch did before A1.</param>
    /// <param name="timedCompletion">Vrf:TimedCompletion. OFF means MarkDispatched arms no end
    /// time at all, so nothing in the walk ever completes - the documented evidence-only mode.</param>
    /// <param name="derivesFromDuration">The FAIL-FIRST control for B4: derive the long completion
    /// window from the predecessor's Duration even though no timer will arm it. Null follows
    /// <paramref name="timedCompletion"/>, which is what the service now does.</param>
    private static ChainOutcome WalkChain(IReadOnlyList<ChainTask> tasks, double configured,
                                          double margin, double scale, double backstop,
                                          double stepSeconds, bool preFixPhase1 = false,
                                          bool timedCompletion = true, bool? derivesFromDuration = null)
    {
        var clock = new StepClock();
        var seq = new TaskSequencer();
        var timed = new TimedCompletionPolicy();
        var byUuid = new Dictionary<string, ChainTask>(StringComparer.Ordinal);
        foreach (var t in tasks) byUuid[t.Uuid] = t;
        var gates = new Dictionary<string, Task<GateResult>>(StringComparer.Ordinal);
        var outcome = new ChainOutcome();

        foreach (var t in tasks)
        {
            bool predFound = !string.IsNullOrEmpty(t.Pred) && byUuid.ContainsKey(t.Pred);
            double predEnd = TaskDispatchPolicy.PredecessorEndSeconds(
                derivesFromDuration ?? timedCompletion, predFound,
                predFound ? byUuid[t.Pred].DurationMs : 0L, scale);
            double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, predEnd, margin);
            double phase1 = preFixPhase1 ? window
                          : TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds(predFound, window, backstop);
            gates[t.Uuid] = seq.WaitForStartAsync(t.Pred,
                                                  TaskDispatchPolicy.ScaleOrderMs(t.StartDelayMs, scale),
                                                  0L, window, clock.AsTaskClock(), CancellationToken.None,
                                                  phase1);
        }

        double horizon = LongestLeadSeconds(tasks, scale) + configured + margin + 4.0 * stepSeconds;
        bool signalled = true;      // t = 0: every root's gate is open before the walk starts
        while (true)
        {
            Settle(tasks, gates, outcome, seq, timed, clock, scale, timedCompletion, signalled);
            if (outcome.Dispatched + outcome.SkippedCount >= tasks.Count) break;
            if (clock.Now >= horizon) break;
            int released = clock.AdvanceTo(clock.Now + stepSeconds);
            var due = timed.Advance(clock.Now, usingSim: true);
            foreach (var d in due) seq.CompleteTask(d.TaskUuid);
            signalled = released > 0 || due.Count > 0;
        }
        return outcome;
    }

    /// <summary>
    /// Let every gate that can move at THIS clock reading move, and do not return until nothing
    /// has moved for a while. The clock never advances inside a settle, so a continuation the
    /// thread pool runs late cannot change WHEN a task dispatched - only how long this loop takes
    /// to notice it (and a phase-2 wait registered late is self-correcting: its window is measured
    /// from the predecessor's DISPATCH, an absolute anchor, not from the registration). A step at
    /// which nothing was signalled costs yields only; a step that released a waiter or completed a
    /// task also buys coarse ticks, because that is when the thread pool has something to run.
    /// </summary>
    /// <param name="signalled">The walk released a clock waiter or completed a task at this
    /// reading, so a continuation IS expected.</param>
    private static void Settle(IReadOnlyList<ChainTask> tasks, Dictionary<string, Task<GateResult>> gates,
                               ChainOutcome outcome, TaskSequencer seq, TimedCompletionPolicy timed,
                               StepClock clock, double scale, bool timedCompletion, bool signalled)
    {
        bool moved = SpinDrain(tasks, gates, outcome, seq, timed, clock, scale, timedCompletion);
        if (!signalled && !moved) return;
        for (int round = 0; round < 8; round++)
        {
            Thread.Sleep(1);
            if (!SpinDrain(tasks, gates, outcome, seq, timed, clock, scale, timedCompletion)) return;
        }
    }

    /// <summary>Drain until nothing has moved for 128 yields; true when anything moved at all. A
    /// yield covers a thread-pool continuation that is already queued - what it cannot cover is a
    /// pool that has to inject a worker, which is what Settle's coarse ticks are for.</summary>
    private static bool SpinDrain(IReadOnlyList<ChainTask> tasks, Dictionary<string, Task<GateResult>> gates,
                                  ChainOutcome outcome, TaskSequencer seq, TimedCompletionPolicy timed,
                                  StepClock clock, double scale, bool timedCompletion)
    {
        bool movedEver = false;
        long lastRegistrations = -1;
        for (int quiet = 0; quiet < 128; quiet++)
        {
            long registrations = Volatile.Read(ref clock.Registrations);
            bool moved = Drain(tasks, gates, outcome, seq, timed, clock, scale, timedCompletion);
            if (moved || registrations != lastRegistrations)
            {
                lastRegistrations = registrations;
                movedEver |= moved;
                quiet = -1;
                continue;
            }
            Thread.Yield();
        }
        return movedEver;
    }

    /// <summary>Dispatch every gate that has opened and abandon every gate that has not. Returns
    /// true when anything changed. The dispatch reproduces MarkDispatched: NotifyDispatched with
    /// the task-clock reading, THEN the end-time Register, THEN the anchoring walk.</summary>
    private static bool Drain(IReadOnlyList<ChainTask> tasks, Dictionary<string, Task<GateResult>> gates,
                              ChainOutcome outcome, TaskSequencer seq, TimedCompletionPolicy timed,
                              StepClock clock, double scale, bool timedCompletion)
    {
        bool moved = false;
        foreach (var t in tasks)
        {
            if (outcome.DispatchedAt.ContainsKey(t.Uuid) || outcome.Skipped.ContainsKey(t.Uuid)) continue;
            var gate = gates[t.Uuid];
            if (!gate.IsCompleted) continue;
            moved = true;
            double now = clock.Now;
            if (gate.Result == GateResult.Proceed)
            {
                outcome.DispatchedAt[t.Uuid] = now;
                seq.NotifyDispatched(t.Uuid, now);
                // MarkDispatched arms the end time only under Vrf:TimedCompletion (B4).
                if (timedCompletion)
                {
                    timed.Register(t.Uuid, "taskee-" + t.Uuid, t.Uuid, "unit-" + t.Uuid,
                                   TaskDispatchPolicy.ScaleOrderMs(t.DurationMs, scale) / 1000.0);
                    foreach (var d in timed.Advance(now, usingSim: true)) seq.CompleteTask(d.TaskUuid);
                }
            }
            else
            {
                outcome.Skipped[t.Uuid] = (gate.Result, now);
                seq.NotifyAbandoned(t.Uuid);
            }
        }
        return moved;
    }

    /// <summary>The deepest chain's LEAD, in clock seconds: how long the last task of the longest
    /// chain waits before it can dispatch at all. Bounds the walk; cycle-guarded, so a malformed
    /// order cannot hang the suite.</summary>
    private static double LongestLeadSeconds(IReadOnlyList<ChainTask> tasks, double scale)
    {
        var byUuid = new Dictionary<string, ChainTask>(StringComparer.Ordinal);
        foreach (var t in tasks) byUuid[t.Uuid] = t;
        var memo = new Dictionary<string, double>(StringComparer.Ordinal);
        double Lead(string uuid, int depth)
        {
            if (depth > 64 || !byUuid.TryGetValue(uuid, out var t)) return 0.0;
            if (memo.TryGetValue(uuid, out double cached)) return cached;
            memo[uuid] = 0.0;      // cycle guard: a task that reaches itself contributes nothing
            double lead = TaskDispatchPolicy.ScaleOrderMs(t.StartDelayMs, scale) / 1000.0;
            if (!string.IsNullOrEmpty(t.Pred) && byUuid.TryGetValue(t.Pred, out var pred))
                lead += Lead(t.Pred, depth + 1)
                      + TaskDispatchPolicy.ScaleOrderMs(pred.DurationMs, scale) / 1000.0;
            memo[uuid] = lead;
            return lead;
        }
        double max = 0.0;
        foreach (var t in tasks) max = Math.Max(max, Lead(t.Uuid, 0));
        return max;
    }

    /// <summary>Walk up from the executable and from the working directory until COA-STP1's order
    /// is in sight - the same search --preflight-selftest and --initgraphics-selftest use.</summary>
    private static string FindCoaStp1Order()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
            {
                string candidate = Path.Combine(d.FullName, "data", "COA-STP1_Order.xml");
                if (File.Exists(candidate)) return candidate;
            }
        return null;
    }

    /// <summary>
    /// The chain walk's clock. Unlike <see cref="FakeClock"/> it is SIGNALLED rather than polled:
    /// a waiter registers the reading it is due at and is released the instant the walk reaches
    /// it, so a gate expires at EXACTLY its window and a 42-gate graph can be walked in whole
    /// minutes without buying a poll interval per step. Monotone, in seconds, exactly what the
    /// service's own task-clock axis is.
    /// </summary>
    private sealed class StepClock
    {
        private readonly object _lock = new();
        private readonly List<(double Due, TaskCompletionSource Tcs)> _waiters = new();
        private double _seconds;
        /// <summary>Every DelayAsync bumps this, so a settle loop can tell "a gate moved on to its
        /// next wait" from "nothing happened".</summary>
        public long Registrations;

        public double Now { get { lock (_lock) return _seconds; } }
        public TaskClock AsTaskClock() => new(() => Now, DelayAsync);

        private Task DelayAsync(double seconds, CancellationToken ct)
        {
            Interlocked.Increment(ref Registrations);
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!(seconds > 0.0)) { tcs.TrySetResult(); return tcs.Task; }
            lock (_lock) _waiters.Add((_seconds + seconds, tcs));
            if (ct.CanBeCanceled)
                ct.Register(() =>
                {
                    lock (_lock) _waiters.RemoveAll(w => ReferenceEquals(w.Tcs, tcs));
                    tcs.TrySetCanceled();
                });
            return tcs.Task;
        }

        /// <summary>Move the clock forward to this reading and release everything it reaches.
        /// Returns how many waiters were released, so the walk knows whether to expect a
        /// continuation at all.</summary>
        public int AdvanceTo(double seconds)
        {
            List<TaskCompletionSource> due = null;
            lock (_lock)
            {
                _seconds = Math.Max(_seconds, seconds);
                for (int i = _waiters.Count - 1; i >= 0; i--)
                    if (_waiters[i].Due <= _seconds)
                    {
                        (due ??= new List<TaskCompletionSource>()).Add(_waiters[i].Tcs);
                        _waiters.RemoveAt(i);
                    }
            }
            if (due == null) return 0;
            foreach (var tcs in due) tcs.TrySetResult();
            return due.Count;
        }
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
