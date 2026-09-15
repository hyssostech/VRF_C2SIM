using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// STP-822 FIXTURE-LEVEL SELF-TEST: drive a tick loop through BackendCount 1 -> 0 -> 1 and assert
/// the whole sequence the interface owes - TASKABRT for every running task, ONE ObservationReport
/// naming the loss, position reports SUPPRESSED until recovery, the progress watchdog and the
/// task clock told to stand down, ONE ObservationReport on recovery, and NOTHING re-tasked.
///
/// FAIL-FIRST BY CONSTRUCTION. `--liveness-selftest --disabled` runs the SAME fixture and the
/// SAME assertions with Vrf:BackendLivenessSeconds = 0, i.e. the interface as it was on the V6d
/// run: the liveness phase never executes, NO back-end reading is taken after start-up, and the
/// R1 poll keeps delivering positions off stale reflected attributes. Every assertion about the
/// loss then fails. That arm is the defect, in one command; the enabled arm is the fix.
///
/// Offline: no bridge, no federation, no server, no clock. The fixture is the tick loop's
/// PHASES, in the order VrfC2SimService.TickLoop runs them, over a scripted sequence of
/// readings. It uses the REAL <see cref="TaskStatusPolicy"/>, the REAL
/// <see cref="BackendLivenessMonitor"/>, the REAL <see cref="NavAreaEvidence"/> and the REAL
/// <see cref="StallPolicy.TaskClockAction"/>, so it can only pass if those agree.
/// </summary>
public static class LivenessSelfTest
{
    // The shipped defaults, so the test measures what a deployment will actually do.
    private const int LivenessSeconds = 10;
    private const int LossConfirmSeconds = 30;

    /// <summary>
    /// The parts of the tick loop this feature touches, with everything else stubbed. One
    /// instance per scenario; Tick() applies the phases in TickLoop's order.
    /// </summary>
    private sealed class TickFixture
    {
        private readonly int _livenessSeconds;            // 0 = the feature is OFF (fail-first arm)
        private readonly BackendLivenessMonitor _mon;
        private readonly TaskStatusPolicy _status = new();
        private readonly List<(string Unit, string TaskUuid, string TaskeeUuid)> _inFlight = new();
        private double _nextLivenessCheck;
        private double _nextPositionReport;
        private bool _suppressionSaid;

        public readonly List<(S.TaskStatusCodeType Code, string TaskUuid, string Why)> Statuses = new();
        public readonly List<string> Observations = new();
        public readonly List<string> Abandoned = new();
        public int PositionReportsSent;
        public int PositionReportsSuppressed;
        public int SuppressionLines;                      // the "said once" line
        public int StallChecksJudged;                     // C16 checks that actually judged a unit
        public int StallChecksStoodDown;                  // C16 checks skipped because the back end is gone
        public int BackendReads;                          // EVERY reading of the back end, from any phase
        public readonly List<StallPolicy.TaskClockOnFlat> TaskClockOutcomes = new();
        public double LossWall = double.NaN;
        public double RecoveryWall = double.NaN;

        public TickFixture(int livenessSeconds, int lossConfirmSeconds,
                           IEnumerable<(string Unit, string TaskUuid, string TaskeeUuid)> inFlight)
        {
            _livenessSeconds = livenessSeconds;
            _mon = livenessSeconds > 0 ? new BackendLivenessMonitor(lossConfirmSeconds) : null;
            _inFlight.AddRange(inFlight);
            foreach (var t in _inFlight)
                if (_status.ShouldEmit(S.TaskStatusCodeType.TASKSTRT, t.TaskUuid))
                    Statuses.Add((S.TaskStatusCodeType.TASKSTRT, t.TaskUuid, "dispatched"));
        }

        public bool Lost => _mon is { Lost: true };

        /// <summary>One tick: the liveness phase, then R1, then C16, then the task clock's
        /// stale branch - the same order and the same gates TickLoop applies.</summary>
        public void Tick(double wall, BackendLivenessPolicy.Sample sample)
        {
            // --- TickPhase("MaybeCheckBackendLiveness", Vrf:BackendLivenessSeconds > 0) --------
            if (_livenessSeconds > 0 && wall >= _nextLivenessCheck)
            {
                _nextLivenessCheck = wall + _livenessSeconds;
                BackendReads++;
                var t = _mon.Observe(wall, sample);
                if (t == BackendLivenessPolicy.Transition.Loss)
                {
                    LossWall = wall;
                    string why = BackendLivenessPolicy.LossReason(_mon.NoStatusSeconds(wall));
                    foreach (var f in _inFlight)
                    {
                        if (_status.ShouldEmit(S.TaskStatusCodeType.TASKABRT, f.TaskUuid))
                            Statuses.Add((S.TaskStatusCodeType.TASKABRT, f.TaskUuid, why));
                        Abandoned.Add(f.TaskUuid);
                    }
                    Observations.Add(BackendLivenessPolicy.LossMarking(
                        _mon.NoStatusSeconds(wall), "2026-09-15T13:11:51Z", _inFlight.Count));
                }
                else if (t == BackendLivenessPolicy.Transition.Recovery)
                {
                    RecoveryWall = wall;
                    Observations.Add(BackendLivenessPolicy.RecoveryMarking(
                        _mon.LostForSeconds(wall), sample.ActiveBackends));
                    _suppressionSaid = false;
                }
            }

            // --- TickPhase("MaybeSendPositionReports", Vrf:PositionReportSeconds > 0) ----------
            if (wall >= _nextPositionReport)
            {
                _nextPositionReport = wall + 10.0;
                if (Lost)
                {
                    PositionReportsSuppressed++;
                    if (!_suppressionSaid) { _suppressionSaid = true; SuppressionLines++; }
                }
                else PositionReportsSent++;
            }

            // --- TickPhase("MaybeCheckStalls", Vrf:StallDetection) ----------------------------
            // C16 must not call a unit stalled because the SIMULATOR stopped: a back-end loss is
            // not a unit stall, and the two verdicts would contradict each other in the report
            // stream.
            if (Lost) StallChecksStoodDown++; else StallChecksJudged++;

            // --- SampleTaskClock ---------------------------------------------------------------
            // THE PRE-STP-822 STATE OF THE WORLD, modelled exactly: the ONLY place the interface
            // re-read the back end was this stale branch, and it is reached only while the sim
            // clock is readable-confirmed AND flat. VrfFacade::SimTimeSeconds gates on
            // backends().count() > 0, so a back end that goes away takes the reader with it and
            // the branch stops running instead of reporting anything. While the back end is up
            // and the scenario is RUNNING the clock is not flat, so the branch does not run
            // either. Net effect on the V6d run: no back-end reading was EVER taken after
            // start-up (V6_LIVE_JOIN_GATE sec 9.5, "the reading is never taken again").
            bool simReadable = sample.BackendCount > 0;
            bool clockFlat = false;                  // the scenario is running whenever it can be read
            bool taskSimStale = simReadable && clockFlat;
            var control = (StallPolicy.BackendControl)sample.ControlState;
            int active = sample.ActiveBackends;
            bool present = sample.BackendCount > 0;
            if (taskSimStale) BackendReads++;
            if (Lost)
            {
                // STP-822: with the back end CONFIRMED gone, the stale branch's three reads are
                // short-circuited to that verdict, so the task clock can never print "the back end
                // REPORTS PAUSED" about a back end this feature has already declared lost.
                control = StallPolicy.BackendControl.NoBackend;
                active = 0;
                present = false;
            }
            TaskClockOutcomes.Add(StallPolicy.TaskClockAction(
                heldOnSim: simReadable, stale: taskSimStale, backEndPresent: present, control, active));
        }

        public int Count(S.TaskStatusCodeType code) => Statuses.Count(s => s.Code == code);
    }

    public static int Run(bool featureEnabled = true)
    {
        int fails = 0;
        void Check(string what, bool cond) { Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what); if (!cond) fails++; }

        Console.WriteLine(featureEnabled
            ? "liveness-selftest: Vrf:BackendLivenessSeconds=" + LivenessSeconds
              + ", Vrf:BackendLossConfirmSeconds=" + LossConfirmSeconds + " (the feature ON)"
            : "liveness-selftest: --disabled - Vrf:BackendLivenessSeconds=0, THE V6d BUILD. The "
              + "assertions about the loss MUST fail in this arm; that failure is the defect.");

        // ============ THE MAIN FIXTURE: BackendCount 1 -> 0 -> 1 on the tick loop ==============
        // 3 running tasks, exactly like V6d's three move-alongs. Ticks every 2 s for 400 s; the
        // back end answers until t=100 s, is GONE from t=100 to t=250, and answers again after.
        // (V6d measured 121 +/- 2 s from dispatch to the observers' drop, twice; the shape that
        // matters here is the drop itself, not when it arrives.)
        var fx = new TickFixture(featureEnabled ? LivenessSeconds : 0, LossConfirmSeconds,
                                 new[]
                                 {
                                     ("1-35", "T_R5_TK1", "u-tk1"),
                                     ("1-6",  "T_R5_CO1", "u-co1"),
                                     ("1-1",  "T_R5_PL1", "u-pl1"),
                                 });
        for (double t = 0.0; t <= 400.0; t += 2.0)
        {
            var s = (t < 100.0 || t >= 250.0)
                  ? new BackendLivenessPolicy.Sample(1, 1, (int)StallPolicy.BackendControl.Running)
                  // A back end that STOPPED: the vendor's counts drop, but its last cached status
                  // still says RUNNING (VrfFacade.h: "a back end that dies while running keeps
                  // reporting Running here"). That is the V6d reading exactly.
                  : new BackendLivenessPolicy.Sample(0, 0, (int)StallPolicy.BackendControl.Running);
            fx.Tick(t, s);
        }

        Check("THE READING IS TAKEN ON A TIMER, independent of the task clock: >= 40 back-end reads in "
              + "400 s at Vrf:BackendLivenessSeconds=" + LivenessSeconds
              + " (V6d took NONE after start-up - its order had no Durations, so the task-clock stale "
              + "branch never ran)",
              fx.BackendReads >= 40);

        Check("LOSS: one TASKABRT per running task, and not one more (3 tasks -> 3 aborts)",
              fx.Count(S.TaskStatusCodeType.TASKABRT) == 3);

        Check("LOSS: every TASKABRT carries the STP-822 reason \"VR-Forces back end lost (no status for N s)\"",
              fx.Count(S.TaskStatusCodeType.TASKABRT) == 3
              && fx.Statuses.Where(x => x.Code == S.TaskStatusCodeType.TASKABRT)
                            .All(x => x.Why.StartsWith("VR-Forces back end lost (no status for", StringComparison.Ordinal)
                                      && x.Why.EndsWith(" s)", StringComparison.Ordinal)));

        Check("LOSS: the successors are ABANDONED too, so a gate cannot contradict the abort we just sent",
              fx.Abandoned.Count == 3);

        Check("LOSS: EXACTLY ONE ObservationReport for the loss, naming the last good stamp and the running-task count",
              fx.Observations.Count(o => o.Contains("back end lost", StringComparison.Ordinal)) == 1
              && fx.Observations.Any(o => o.Contains("Last good reading 2026-09-15T13:11:51Z", StringComparison.Ordinal)
                                          && o.Contains("3 task(s) were running", StringComparison.Ordinal)));

        Check("LOSS lands within one cadence of the confirm window (first zero sample t=100 s -> abort by "
              + "t <= " + (100 + LossConfirmSeconds + LivenessSeconds) + " s)",
              !double.IsNaN(fx.LossWall) && fx.LossWall >= 100.0
              && fx.LossWall <= 100.0 + LossConfirmSeconds + LivenessSeconds);

        Check("SUPPRESSION: position reports STOP at the loss - none is sent off stale reflected "
              + "attributes - and the suppression is SAID ONCE, not once per cycle",
              fx.PositionReportsSuppressed >= 10 && fx.SuppressionLines == 1);

        Check("SUPPRESSION is bounded by the outage: reports flow before it and after it",
              fx.PositionReportsSent > 0 && !fx.Lost);

        Check("C16 STANDS DOWN while the back end is gone - a back-end loss is not a unit stall",
              fx.StallChecksStoodDown > 0 && fx.StallChecksJudged > 0);

        Check("THE TASK CLOCK cannot HOLD while the back end is declared LOST: every outcome inside "
              + "the outage is FallBackToWall, never HoldOnSim (Q5 holds a PAUSE, not a death)",
              fx.TaskClockOutcomes.Count > 0
              && fx.TaskClockOutcomes.All(o => o != StallPolicy.TaskClockOnFlat.HoldOnSim)
              && StallPolicy.TaskClockAction(true, true, false,
                                             StallPolicy.BackendControl.NoBackend, 0)
                 == StallPolicy.TaskClockOnFlat.FallBackToWall);

        Check("RECOVERY: EXACTLY ONE ObservationReport, and it says the tasks are NOT restarted",
              fx.Observations.Count(o => o.Contains("reporting again", StringComparison.Ordinal)) == 1
              && fx.Observations.Any(o => o.Contains("are NOT restarted", StringComparison.Ordinal)));

        Check("RECOVERY re-tasks NOTHING: no second TASKSTRT, no second TASKABRT",
              fx.Count(S.TaskStatusCodeType.TASKSTRT) == 3 && fx.Count(S.TaskStatusCodeType.TASKABRT) == 3);

        Check("TWO reports in the whole outage - one loss, one recovery - not one per tick",
              fx.Observations.Count == 2);

        // ============ THE MONITOR'S OWN RULES ==================================================
        // These exercise the policy directly, so they hold in BOTH arms: the class behaves either
        // way - the V6d defect is that nothing ever CALLS it.
        {
            var m = new BackendLivenessMonitor(LossConfirmSeconds);
            var up = new BackendLivenessPolicy.Sample(1, 1, (int)StallPolicy.BackendControl.Running);
            var down = new BackendLivenessPolicy.Sample(0, 0, (int)StallPolicy.BackendControl.Running);
            m.Observe(0, up);
            var t1 = m.Observe(10, down);
            var t2 = m.Observe(20, up);
            Check("NEVER ON ONE SAMPLE: a single zero between two good readings declares nothing",
                  t1 == BackendLivenessPolicy.Transition.None
                  && t2 == BackendLivenessPolicy.Transition.None && !m.Lost);
        }
        {
            var m = new BackendLivenessMonitor(LossConfirmSeconds);
            var down = new BackendLivenessPolicy.Sample(0, 0, (int)StallPolicy.BackendControl.NoBackend);
            for (double t = 0; t <= 300; t += 10) m.Observe(t, down);
            Check("YOU CANNOT LOSE WHAT YOU NEVER HAD: a run that never discovers a back end reports no "
                  + "LOSS (the start-up settle already says NO BACKEND DISCOVERED)", !m.Lost);
        }
        {
            var m = new BackendLivenessMonitor(LossConfirmSeconds);
            m.Observe(0, new BackendLivenessPolicy.Sample(1, 1, (int)StallPolicy.BackendControl.Running));
            // Every getter unreadable: an older VrfBridge (MissingMethodException on the STP-809
            // members) with BackendCount itself throwing too.
            var blind = new BackendLivenessPolicy.Sample(-1, -1, (int)StallPolicy.BackendControl.Unreadable);
            for (double t = 10; t <= 300; t += 10) m.Observe(t, blind);
            Check("A SIGNAL THAT SAYS NOTHING CHANGES NOTHING: an all-unreadable reader never declares a loss",
                  !m.Lost);
        }
        {
            var m = new BackendLivenessMonitor(LossConfirmSeconds);
            m.Observe(0, new BackendLivenessPolicy.Sample(1, 1, (int)StallPolicy.BackendControl.Running));
            // The STP-809 reader is absent (an older bridge) but BackendCount still answers - the
            // WatchVrf --report-backends column that was MEASURED to drop 1 -> 0 at 121 s on both
            // quiet runs.
            var down = new BackendLivenessPolicy.Sample(-1, 0, (int)StallPolicy.BackendControl.Unreadable);
            var last = BackendLivenessPolicy.Transition.None;
            for (double t = 10; t <= 60; t += 10)
            { var r = m.Observe(t, down); if (r != BackendLivenessPolicy.Transition.None) last = r; }
            Check("BackendCount ALONE is enough: a deployment carrying a bridge that predates STP-809 "
                  + "still detects the loss", last == BackendLivenessPolicy.Transition.Loss && m.Lost);
        }
        Check("A PAUSED back end is ALIVE (Q5): active=1 with DtPauseControlType is never a loss",
              BackendLivenessPolicy.Classify(
                  new BackendLivenessPolicy.Sample(1, 1, (int)StallPolicy.BackendControl.Paused))
              == BackendLivenessPolicy.Reading.Present);

        // ============ THE NAV-AREA GATE (Vrf:RequireNavAreaForGroundTasks) =====================
        {
            var ev = new NavAreaEvidence();
            ev.ConsoleOpened("u-tk1", 4);
            var v = ev.Decide(new[] { "u-tk1" }, 100.0, 300.0);
            Check("NAV GATE: console open at level 4 and NO area row ever -> REFUSE (the V6d fixture)",
                  NavAreaEvidence.ShouldRefuse(v) && !v.AnyEvidence && v.CanSee);

            ev.Observe("u-bde", "New Primary nav area: | MojaveAO20", 120.0);
            v = ev.Decide(new[] { "u-tk1" }, 130.0, 300.0);
            Check("NAV GATE: another object's area row inside the window -> DISPATCH, and the verdict "
                  + "records that it was not the taskee's own row",
                  !NavAreaEvidence.ShouldRefuse(v) && v.AnyEvidence && !v.TaskeeEvidence);

            v = ev.Decide(new[] { "u-tk1" }, 600.0, 300.0);
            Check("NAV GATE: the evidence EXPIRES (Vrf:NavAreaEvidenceSeconds) - a 480 s old row does not "
                  + "clear a task now", NavAreaEvidence.ShouldRefuse(v));

            ev.Observe("u-tk1", "New Primary nav area: | MojaveAO20", 590.0);
            v = ev.Decide(new[] { "u-tk1" }, 600.0, 300.0);
            Check("NAV GATE: the TASKEE's own row clears it, and the verdict says so",
                  !NavAreaEvidence.ShouldRefuse(v) && v.TaskeeEvidence && v.Area == "MojaveAO20");

            var blind = new NavAreaEvidence();
            var bv = blind.Decide(new[] { "u-tk1" }, 100.0, 300.0);
            Check("NAV GATE: consoles BELOW level 3 -> the gate cannot judge, so it WARNS and dispatches "
                  + "rather than refusing on ignorance",
                  !NavAreaEvidence.ShouldRefuse(bv) && !bv.CanSee);

            var neg = new NavAreaEvidence();
            neg.ConsoleOpened("u-tk1", 4);
            neg.Observe("u-tk1", "fail in action Is current point in nav area?", 50.0);
            var nv = neg.Decide(new[] { "u-tk1" }, 60.0, 300.0);
            Check("NAV GATE: the behaviour tree's own FAILED condition is recorded and named in the refusal",
                  nv.NotInAreaSeen
                  && NavAreaEvidence.RefusalMarking("1-35", 300.0, nv)
                        .Contains("FAILED \"Is current point in nav area?\"", StringComparison.Ordinal));
        }

        // The XML is built the way every other report is - construct the SDK types and serialize.
        {
            string xml = BackendLivenessPolicy.BuildStateChangeReport(
                BackendLivenessPolicy.LossMarking(31.0, "2026-09-15T13:11:51Z", 3),
                "2026-09-15T13:13:52Z", "rep-1");
            Check("the loss ObservationReport serializes as a C2SIM ReportBody with a NameObservation",
                  !string.IsNullOrEmpty(xml)
                  && xml.Contains("NameObservation", StringComparison.Ordinal)
                  && xml.Contains("VR-Forces back end lost", StringComparison.Ordinal));
        }

        Console.WriteLine(fails == 0 ? "liveness-selftest: ALL CHECKS PASSED" : $"liveness-selftest: {fails} FAILED");
        if (!featureEnabled)
            Console.WriteLine("liveness-selftest: the --disabled arm is EXPECTED to fail - it is the V6d "
                            + "behaviour (543 position reports off a stopped back end, 0 warnings, 0 TASKABRT).");
        return fails == 0 ? 0 : 1;
    }
}
