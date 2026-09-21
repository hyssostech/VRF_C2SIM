namespace VrfC2SimApp;

/// <summary>
/// THE SIMULATION CLOCK, OBSERVED ONCE FOR EVERYONE (M3 + M4 of the cold-start review of 5c67d41).
/// Pure state machine over <see cref="StallPolicy"/>'s predicates - no bridge, no log, no clock of
/// its own - so the whole rule is decidable offline (`--rulings-selftest`).
///
/// WHY IT EXISTS. Two consumers read DtVrfRemoteController::simTime() through
/// VrfFacade::SimTimeSeconds: the progress watchdog (C16) and the R4 timed completion + task-clock
/// axis. The watchdog had been hardened against the two ways that reader misbehaves; the timed
/// walk was written later and inherited neither guard, with these consequences:
///
///   M3 - AN ALTERNATING READER STARVES EVERY TASK, FOREVER. The watchdog runs its observation
///        through <see cref="StallPolicy.NextClockMode"/> with ModeSwitchConfirmations precisely
///        because a reader that alternates -1 / &gt;= 0 at the check cadence is a documented case
///        ("the back end briefly out of the list", vrfBackendListener.h:154-163). The timed walk
///        computed its mode from a single RAW read, and a mode flip RE-ANCHORS and serves zero -
///        so at the 1 s cadence Elapsed never grew: no task ever completed, no warning, and every
///        successor was then skipped at the predecessor gate. The same is true of the task-clock
///        axis, which contributes nothing across a mode change by design.
///
///   M4 - A STALE CLOCK FREEZES EVERY END TIME, SILENTLY. SimTimeSeconds gates on
///        backends().count() &gt; 0, and a back end that stops answering is DEACTIVATED, not
///        removed (vrfBackendListener.h:161-163 against :154-155), so the reader keeps returning
///        its last cached value. Read as a pause, that is a scenario in which no Duration ever
///        elapses - the exact silent stop R4 exists to remove, reintroduced one layer up.
///
/// ONE OBSERVER, TWO CONSUMERS. Every consumer sees the SAME sample, the SAME hysteresis-confirmed
/// answer to "is the sim clock readable", and the SAME staleness verdict, so the watchdog and the
/// task clock can never disagree about whether the scenario is running. What stays per-consumer is
/// the PREFERENCE (Vrf:StallClock, Vrf:TaskClock) and the reaction: the watchdog suspends judging,
/// the task clock falls back to wall seconds and keeps serving.
/// </summary>
public sealed class SimClockTracker
{
    // ONE OBSERVATION OF THE SIMULATION CLOCK, as every consumer sees it. Plain comments, not
    // <param> docs: a positional record's members are documented where they are read, and the
    // fields below each carry a rule rather than a description.
    //   Readable           - this sample's raw value is a reading at all (finite and >= 0).
    //   ReadableConfirmed  - ... and that has held for ModeSwitchConfirmations consecutive
    //                        samples. THIS is what a consumer switches its clock mode on;
    //                        Readable alone flaps, and a flapping mode re-anchors forever (M3).
    //   ModeChanged        - the confirmed answer changed on this sample. A consumer that keeps
    //                        per-mode state (the watchdog's sample rings) drops it here, and
    //                        only here.
    //   SimSeconds         - the raw reading (-1 when there was none).
    //   PreviousSimSeconds - the last reading that was DIFFERENT from this one: the "was" of a
    //                        rollback line. Equal to SimSeconds on the first readable sample.
    //   Step               - where this reading sits against the previous one. Flat on an
    //                        unreadable sample, because there is nothing to compare.
    //   Stale              - readable, FLAT, and flat for staleAfterSeconds of WALL time: the
    //                        deactivated back end, not a pause the operator chose (M4). A
    //                        consumer that keeps serving must stop serving on this clock while
    //                        it is true.
    //   FlatForWallSeconds - how long (WALL seconds) the reading has been flat; 0 when it moved.
    public readonly record struct Observation(
        bool Readable,
        bool ReadableConfirmed,
        bool ModeChanged,
        double SimSeconds,
        double PreviousSimSeconds,
        StallPolicy.SimClockStep Step,
        bool Stale,
        double FlatForWallSeconds);

    private int _mode;                                  // 0 = unresolved, 1 = readable, 2 = not
    private int _candidate;                             // the mode an unsteady reader proposes
    private int _streak;                                // consecutive samples of that candidate
    private double _lastValue = double.NegativeInfinity; // the last reading that CHANGED
    private double _lastChangeWall;                      // wall seconds when it last changed
    private bool _seen;                                  // a readable sample has been taken

    /// <summary>The hysteresis-confirmed answer to "is the simulation clock readable".</summary>
    public bool ReadableConfirmed => _mode == 1;

    /// <summary>
    /// Fold one reading in. <paramref name="rawSimSeconds"/> is VrfFacade::SimTimeSeconds' value
    /// (-1 = no reading, and any non-finite value is treated as none - see
    /// <see cref="StallPolicy.UsingSimClock"/> for why +Infinity has to be refused explicitly).
    /// <paramref name="wallSeconds"/> is the wall clock at the same instant, used ONLY to age a
    /// flat reading.
    /// </summary>
    public Observation Observe(double rawSimSeconds, double wallSeconds, int confirmations,
                               double staleAfterSeconds)
    {
        bool readable = StallPolicy.UsingSimClock(true, rawSimSeconds);
        int held = _mode;
        (_mode, _candidate, _streak) = StallPolicy.NextClockMode(
            _mode, _candidate, _streak, readable ? 1 : 2, confirmations);

        var step = StallPolicy.SimClockStep.Flat;
        double previous = _lastValue;
        bool stale = false;
        double flatFor = 0.0;
        if (readable)
        {
            if (!_seen)
            {
                // The first reading is an ADVANCE from nothing: it anchors, it is not stale, and
                // it has no meaningful predecessor to name in a rollback line.
                _seen = true;
                step = StallPolicy.SimClockStep.Advanced;
                previous = rawSimSeconds;
            }
            else
            {
                // A BACKWARDS STEP IS A CHANGE, NOT A NON-ADVANCE (StallPolicy pass-2 review F3):
                // after rollbackToSnapshot the clock is genuinely advancing, below its old mark,
                // and treating that as "not advancing" suspends every consumer for as long as it
                // takes to climb back.
                step = StallPolicy.ClassifyClockStep(rawSimSeconds, _lastValue);
            }
            if (step != StallPolicy.SimClockStep.Flat)
            {
                _lastValue = rawSimSeconds;
                _lastChangeWall = wallSeconds;
            }
            else
            {
                flatFor = wallSeconds - _lastChangeWall;
                stale = StallPolicy.SimClockStale(rawSimSeconds, _lastValue, wallSeconds,
                                                  _lastChangeWall, staleAfterSeconds);
            }
        }
        return new Observation(readable, _mode == 1, _mode != held, rawSimSeconds, previous,
                               step, stale, flatFor);
    }
}

/// <summary>
/// HOW FAST THE SCENARIO IS RUNNING AGAINST THE WALL (N7, D7 harvest 2026-09-21). Pure: fed two
/// readings, keeps two marks, answers a ratio - no clock of its own, so the rule is decidable
/// offline (`--rulings-selftest`).
///
/// WHY IT EXISTS. Every elapsed figure this interface printed was wall-minus-wall and none of them
/// said so. The D6 harvest recorded "sim/wall 1.00" by comparing one of them against wall capture
/// stamps - the same clock on both sides, so the check was circular - and D7 then had to REGRESS
/// the level-3 behaviour-tree console rows against the observer's trace clock to discover the
/// scenario was running at 3.00x. The ratio is a number the interface can simply state: it holds
/// the sim reading (VrfBridge.SimTimeSeconds() -&gt; DtVrfRemoteController::simTime()) and the wall
/// reading at the same instant, once a second, and nothing else is needed.
///
/// THE RULE: the ratio is (sim now - sim at the window's start) / (wall now - wall at the window's
/// start) over a window of at least <see cref="MinWindowWallSeconds"/> WALL seconds. It is
/// reported when the window is full, and the window then RE-ANCHORS on the reading just reported,
/// so each line describes a DISJOINT stretch of the run rather than a running average that hides a
/// change. A window is ABANDONED (re-anchored, nothing reported) whenever the sim reading is
/// unreadable or went BACKWARDS - an unreadable stretch has no ratio, and a rollback would
/// manufacture a negative one. A FLAT sim clock is NOT abandoned: 0.00 is the true ratio of a
/// paused scenario and is exactly what an operator needs to see.
/// </summary>
public sealed class SimWallRatio
{
    /// <summary>The window, in WALL seconds. One minute: long enough that a 1 Hz sampler's
    /// quantisation is under 2% and short enough to show a rate change inside a demo.</summary>
    public const double MinWindowWallSeconds = 60.0;

    private double _simAnchor = double.NaN;
    private double _wallAnchor = double.NaN;
    private double _lastSim = double.NaN;

    /// <summary>
    /// Fold in one pair of readings. <paramref name="simSeconds"/> is NaN (or negative) when the
    /// sim clock is not readable. Returns the ratio to report, or NaN for "nothing to report yet".
    /// </summary>
    public double Observe(double simSeconds, double wallSeconds)
    {
        bool readable = double.IsFinite(simSeconds) && simSeconds >= 0.0;
        if (!readable || (double.IsFinite(_lastSim) && simSeconds < _lastSim))
        {
            // No reading, or a rollback: this window cannot be priced. Start a new one.
            _simAnchor = _wallAnchor = _lastSim = double.NaN;
            if (readable) { _simAnchor = _lastSim = simSeconds; _wallAnchor = wallSeconds; }
            return double.NaN;
        }
        _lastSim = simSeconds;
        if (double.IsNaN(_simAnchor)) { _simAnchor = simSeconds; _wallAnchor = wallSeconds; return double.NaN; }
        double wallSpan = wallSeconds - _wallAnchor;
        if (wallSpan < MinWindowWallSeconds || wallSpan <= 0.0) return double.NaN;
        double ratio = (simSeconds - _simAnchor) / wallSpan;
        _simAnchor = simSeconds;
        _wallAnchor = wallSeconds;
        return ratio;
    }
}

/// <summary>
/// THE TASK-CLOCK AXIS (M2 of the cold-start review of 5c67d41): one monotone seconds axis on
/// which every C2SIM task time is served - the Duration that ends a task (R4), the StartTime delay
/// that holds one back, and the STREND predecessor gate. Pure: it is fed readings, it reads no
/// clock of its own, so the whole rule is decidable offline (`--rulings-selftest`).
///
/// THE RULE is TimedCompletionPolicy's, applied once for every consumer instead of once per
/// consumer: add the FORWARD movement of whichever clock is in effect, and NOTHING ELSE.
///   - a PAUSED scenario adds nothing (the reading is flat);
///   - DtVrfRemoteController::rollbackToSnapshot adds nothing (the reading went backwards) - a
///     deadline stamp would instead fire the moment a rollback landed past it;
///   - A CLOCK-MODE CHANGE ADDS NOTHING. The two clocks are not comparable at all: a scenario
///     clock starts near zero and the wall clock's epoch is ~1.7e9 s, so subtracting one from the
///     other is not a small error but a nonsensical one. Falling back to the wall clock mid-task
///     therefore keeps every task the time it has already served and serves the rest on the new
///     base.
/// Advanced on the tick thread; READ from the SDK callback threads and the thread pool (the gate
/// pollers), hence Volatile.
/// </summary>
public sealed class TaskClockAxis
{
    private double _seconds;
    private double _lastReading = double.NaN;
    private bool _lastUsingSim;

    /// <summary>Seconds served since start-up. Safe from any thread.</summary>
    public double Seconds => Volatile.Read(ref _seconds);

    /// <summary>Which clock the last accepted reading came from (for the log line only).</summary>
    public bool UsingSim { get; private set; }

    /// <summary>Fold in one reading of the clock in effect. Tick thread only.</summary>
    public void Advance(double reading, bool usingSim)
    {
        if (!double.IsFinite(reading)) return;
        if (!double.IsNaN(_lastReading) && usingSim == _lastUsingSim && reading > _lastReading)
            Volatile.Write(ref _seconds, _seconds + (reading - _lastReading));
        _lastReading = reading;
        _lastUsingSim = usingSim;
        UsingSim = usingSim;
    }
}
