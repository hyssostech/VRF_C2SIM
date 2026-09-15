using C2SIM;
using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// STP-822: IS THE VR-FORCES BACK END STILL THERE? - read on a TIMER, not on the task clock.
///
/// THE DEFECT THIS EXISTS FOR (docs/experiments/V6_LIVE_JOIN_GATE_2026-09-15.md sec 9.5, run
/// 20260915T130627Z). The back end STOPPED at the first ground move-along of the R5 order: no
/// frames, no status messages, no motion, no terminal reports. (WHY it stops is OPEN - V6e, sec
/// 11, falsified the nav-data account and MEASURED the stopped state as a RUNAWAY ALLOCATION,
/// ~2.2 GB/min at under one core, which exhausts a 32 GB machine in about 30 minutes. Sec 11.6
/// makes this read a SAFETY item, not only a reporting one: whatever the trigger turns out to
/// be, the interface has to notice.) The
/// interface logged "Backend discovered (BackendCount=1)" once at start-up and then delivered
/// 543 position reports off stale reflected attributes with 0 warnings and 0 TASKABRT, because
/// the ONLY place it re-read the back end was the task clock's stale branch
/// (`taskSimStale = heldOnSim &amp;&amp; obs.Stale`, VrfC2SimService.SampleTaskClock) and that branch
/// needs a task HELD ON AN END TIME. The R5 order carries no Duration, so nothing was ever held,
/// the branch never ran, and the interface had no mechanism to notice. The same reading held on
/// the healthy A4 run (8,169 position reports, back end alive the whole time) - the mechanism
/// never ran either way, so this is not a fixture property.
///
/// WHAT THIS CLASS IS. The decision, with no bridge, no clock and no reports in it: given a
/// sequence of readings taken on a timer, say when the back end has been LOST and when it has
/// come back. The service supplies the readings and performs the consequences (TASKABRT,
/// ObservationReport, position-report suppression, telling C16 and the task clock to stand
/// down). Pure, so --liveness-selftest can drive the whole sequence offline.
///
/// THE SIGNALS, in the order they are trusted (same discipline as StallPolicy.TaskClockAction):
///   1. VrfFacade::ActiveBackendCount (STP-809) - how many KNOWN back ends the vendor still calls
///      simulatable or in transition. -1 means NO READING (an older bridge throws
///      MissingMethodException here; see RUNBOOK sec 9).
///   2. VrfFacade::BackendCount - backends().count(). The pre-STP-809 signal, and the one
///      WatchVrf --report-backends samples: it is the column that dropped 1 -> 0 exactly 121 s
///      after dispatch on BOTH quiet runs, so it is known to move on this failure.
///   3. The control state, but only to turn "no reading at all" into a verdict:
///      BackendControlNoBackend is the vendor saying there is nothing there. Any other control
///      value NEVER decides a loss - a PAUSED back end is alive (Q5), and a cached "Running" from
///      a back end that has stopped is exactly what this class is here to see through.
/// A SIGNAL THAT SAYS NOTHING CAN NEVER CHANGE AN OUTCOME: an all-unreadable sample is neither a
/// miss nor a good reading, and the state machine simply keeps what it had.
///
/// TWO THINGS IT DELIBERATELY DOES NOT DO:
///   - It never declares a loss on ONE sample (Vrf:BackendLossConfirmSeconds AND
///     <see cref="ConfirmSamples"/> consecutive misses must both be satisfied).
///   - It never declares a loss before the back end has been seen AT LEAST ONCE. You cannot lose
///     what you never had, and the start-up settle already says "NO BACKEND DISCOVERED" for that
///     case, loudly, with what to check.
///
/// NOT READ HERE, and why: the listener's own age for a back end -
/// `DtBackend::lastResponseTime()` (vrfutil/backend.h:198-199), set from each status message,
/// aged out by `DtVrfBackendListener::doTimeouts()` (vrfBackendListener.h:157-163) - is the
/// cleanest reading of all and is NOT exposed by VrfFacade. Publishing it means a new native
/// getter, which triggers the eleven-consumer redeploy of gate G-A (RUNBOOK sec 9). It is not
/// needed for the defect: the counts above already move, 121 s after the back end goes quiet,
/// measured twice.
/// </summary>
public static class BackendLivenessPolicy
{
    /// <summary>How many CONSECUTIVE samples must agree before the state changes, in either
    /// direction. Two, never one: one missed status message, one tick the vendor list was
    /// momentarily empty, or one throw must not abort an order.</summary>
    public const int ConfirmSamples = 2;

    /// <summary>What one timed reading of the back end says, before any history is applied.</summary>
    public enum Reading
    {
        /// <summary>Nothing readable: every getter threw, or every one returned "no reading".</summary>
        NoReading = 0,
        /// <summary>At least one back end is there (and, when the vendor can say so, operating).</summary>
        Present = 1,
        /// <summary>The vendor positively reports ZERO: no active back end, or none at all.</summary>
        Gone = 2,
    }

    /// <summary>What the monitor decided this sample.</summary>
    public enum Transition
    {
        None = 0,
        /// <summary>Confirmed loss - the sample that crosses BOTH confirmations.</summary>
        Loss = 1,
        /// <summary>Confirmed recovery after a loss.</summary>
        Recovery = 2,
    }

    /// <summary>
    /// One timed reading. activeBackends and backendCount use -1 for "no reading" exactly as the
    /// facade does; controlState carries VrfFacade::BackendControl (see StallPolicy.BackendControl,
    /// the managed mirror), where -1 is Unreadable and -2 is NoBackend.
    /// </summary>
    public readonly record struct Sample(int ActiveBackends, int BackendCount, int ControlState);

    /// <summary>
    /// The three signals -> one reading. Counts decide; the control state only breaks a tie of
    /// silence. ActiveBackends is preferred over BackendCount because backends().count() keeps a
    /// back end that merely missed its status timeout - that is the whole reason STP-809 exists -
    /// but BackendCount is the signal that was MEASURED to drop on this failure, so it is used
    /// whenever the better one has no reading.
    /// </summary>
    public static Reading Classify(Sample s)
    {
        if (s.ActiveBackends >= 0) return s.ActiveBackends == 0 ? Reading.Gone : Reading.Present;
        if (s.BackendCount >= 0) return s.BackendCount == 0 ? Reading.Gone : Reading.Present;
        if (s.ControlState == (int)StallPolicy.BackendControl.NoBackend) return Reading.Gone;
        return Reading.NoReading;
    }

    /// <summary>The TASKABRT reason (and the ObservationReport's opening clause). The wording is
    /// fixed by STP-822 and is what a harvest greps for.</summary>
    public static string LossReason(double noStatusSeconds)
        => $"VR-Forces back end lost (no status for {Math.Max(0.0, noStatusSeconds):F0} s)";

    /// <summary>The Marking text of the LOSS ObservationReport: the loss, the last good stamp and
    /// how many tasks were running when it happened.</summary>
    public static string LossMarking(double noStatusSeconds, string lastGoodIso, int runningTasks)
        => $"BACK-END LIVENESS: {LossReason(noStatusSeconds)}. Last good reading {lastGoodIso}; " +
           $"{runningTasks} task(s) were running and each is reported TASKABRT. Position reports are " +
           "SUSPENDED until the back end reports again - they would otherwise be read off stale " +
           "reflected attributes (STP-822).";

    /// <summary>The Marking text of the RECOVERY ObservationReport. It says what did NOT happen -
    /// nothing is re-tasked - because that is the question a consumer will have.</summary>
    public static string RecoveryMarking(double lostForSeconds, int activeBackends)
        => "BACK-END LIVENESS: the VR-Forces back end is reporting again after " +
           $"{Math.Max(0.0, lostForSeconds):F0} s ({(activeBackends >= 0 ? activeBackends.ToString() : "an unknown number of")} " +
           "back end(s) reported). Position reports resume. Tasks aborted during the loss are NOT " +
           "restarted - a new order is required (STP-822).";

    /// <summary>
    /// ONE C2SIM ObservationReport carrying a liveness state change. Built the way
    /// PreflightReports builds its warnings: a NameObservation whose Marking carries the wording,
    /// because C2SIM 1.0.2 has no observation type for "the simulator itself". The observation is
    /// about the SIMULATION, not about any one unit, so the actor is the zero uuid rather than a
    /// taskee that happens to be in flight.
    /// </summary>
    public static string BuildStateChangeReport(string marking, string isoDateTime, string reportId)
    {
        const string ZeroUuid = "00000000-0000-0000-0000-000000000000";
        var body = new S.ReportBodyType
        {
            FromSender = ZeroUuid,
            ToReceiver = ZeroUuid,
            ReportContent = new[]
            {
                new S.ReportContentType
                {
                    Item = new S.ObservationReportContentType
                    {
                        TimeOfObservation = new S.TimeInstantType
                        {
                            Item = new S.DateTimeType { IsoDateTime = isoDateTime }
                        },
                        Observation = new[]
                        {
                            new S.ObservationType
                            {
                                Item = new S.NameObservationType
                                {
                                    ActorReference = ZeroUuid,
                                    Name = "VR-Forces back end",
                                    Marking = marking ?? "",
                                }
                            }
                        }
                    }
                }
            },
            ReportID = reportId,
            ReportingEntity = ZeroUuid,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }
}

/// <summary>
/// The liveness state machine itself (STP-822). One instance per run, driven from the tick loop
/// at Vrf:BackendLivenessSeconds. Not thread-safe by design: the tick thread is its only caller,
/// exactly like the stall watchdog's rings. <see cref="Lost"/> IS read from other threads, so it
/// is the one volatile-ish member - the service copies it into its own volatile flag.
/// </summary>
public sealed class BackendLivenessMonitor
{
    private readonly double _lossConfirmSeconds;
    private bool _everPresent;
    private bool _lost;
    private int _consecutiveMisses;
    private int _consecutiveGood;
    private double _firstMissWall = double.NaN;
    private double _lastGoodWall = double.NaN;
    private double _lostSinceWall = double.NaN;

    public BackendLivenessMonitor(double lossConfirmSeconds)
        => _lossConfirmSeconds = Math.Max(0.0, lossConfirmSeconds);

    /// <summary>True between a confirmed loss and its recovery. The position-report suppression,
    /// the C16 stand-down and the task clock's forced WALL fall back all read this.</summary>
    public bool Lost => _lost;

    /// <summary>Wall seconds of the last sample that positively reported a back end, NaN if the
    /// back end has never been seen.</summary>
    public double LastGoodWall => _lastGoodWall;

    /// <summary>Wall seconds at which the loss was CONFIRMED, NaN while not lost.</summary>
    public double LostSinceWall => _lostSinceWall;

    /// <summary>Seconds since the last good reading (0 when there has never been one).</summary>
    public double NoStatusSeconds(double wallNow)
        => double.IsNaN(_lastGoodWall) ? 0.0 : Math.Max(0.0, wallNow - _lastGoodWall);

    /// <summary>
    /// Admit one timed reading and say whether the state CHANGED. Only a change is ever acted on
    /// or logged: this runs every Vrf:BackendLivenessSeconds for the life of the run and a line
    /// per sample would bury the one line that matters.
    /// </summary>
    public BackendLivenessPolicy.Transition Observe(double wallSeconds, BackendLivenessPolicy.Sample sample)
    {
        var reading = BackendLivenessPolicy.Classify(sample);
        switch (reading)
        {
            case BackendLivenessPolicy.Reading.NoReading:
                // Nothing was legible this sample. Neither a miss nor a good reading: keep the
                // state and the counters exactly as they were.
                return BackendLivenessPolicy.Transition.None;

            case BackendLivenessPolicy.Reading.Present:
                _everPresent = true;
                _consecutiveMisses = 0;
                _firstMissWall = double.NaN;
                _lastGoodWall = wallSeconds;
                if (!_lost) { _consecutiveGood = 0; return BackendLivenessPolicy.Transition.None; }
                // RECOVERY needs the same two consecutive samples a loss does. The extra
                // Vrf:BackendLivenessSeconds of suppressed position reports costs nothing; a
                // flapping reader that re-opened the channel on every stray sample would cost a
                // pair of ObservationReports each time.
                if (++_consecutiveGood < BackendLivenessPolicy.ConfirmSamples)
                    return BackendLivenessPolicy.Transition.None;
                _lost = false;
                _consecutiveGood = 0;
                return BackendLivenessPolicy.Transition.Recovery;

            default:   // Gone
                _consecutiveGood = 0;
                if (_lost) return BackendLivenessPolicy.Transition.None;   // already said so
                if (double.IsNaN(_firstMissWall)) _firstMissWall = wallSeconds;
                _consecutiveMisses++;
                // NEVER on one sample, and never before the back end was ever there.
                if (!_everPresent) return BackendLivenessPolicy.Transition.None;
                if (_consecutiveMisses < BackendLivenessPolicy.ConfirmSamples)
                    return BackendLivenessPolicy.Transition.None;
                if ((wallSeconds - _firstMissWall) < _lossConfirmSeconds)
                    return BackendLivenessPolicy.Transition.None;
                _lost = true;
                _lostSinceWall = wallSeconds;
                return BackendLivenessPolicy.Transition.Loss;
        }
    }

    /// <summary>Seconds the current (or most recent) loss has lasted at wallNow; 0 if never lost.</summary>
    public double LostForSeconds(double wallNow)
        => double.IsNaN(_lostSinceWall) ? 0.0 : Math.Max(0.0, wallNow - _lostSinceWall);
}
