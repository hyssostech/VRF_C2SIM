using System;
using System.Collections.Generic;
using System.Linq;

namespace VrfC2SimApp;

/// <summary>
/// PROGRESS WATCHDOG (C16, report-only; the sibling of ArrivalPolicy/C15). Pure decision, no
/// bridge, testable offline (--stall-selftest).
///
/// Why the interface has to do this itself: in VR-Forces 5.2 a ground unit that stops making
/// progress while its move task is still running is UNDETECTED BY DESIGN
/// (docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md sec 6a). The base contract
/// vrfobjcore/singleTaskControllerComponent.h:192-205 ("Override this function to provide the
/// test for determining if the task controller should give up ... The implementation in this
/// class always returns false") hands the test to the integrator; the shipped sample
/// examples/decideToGiveUpTask is a sim-side PLUGIN, not something an HLA client can install;
/// ground-vehicle-move-to.lua has no progress test at all (its only speed check is the
/// stop-before-replan precondition at :1350-1353, and MAX_REPLANS = 3 at :47 counts blockage
/// replans, not lack of progress). So the unit's task reports "running" forever and nothing
/// reaches the interface. Observed: 1-35 and 1-6 froze 2-3 km into 24-33 km legs in four runs
/// and never reported anything (FINDING sec 7).
///
/// THE RULE. The unit is STALLED when EVERY member that has a readable position moved LESS THAN
/// moveMeters over the window, and at least minMembersWithData members had readable positions.
///
/// Two cases the rule must NOT call a stall, both drawn from real traces:
///   - a unit whose LEADER is moving while its followers stand (1-6 on 2026-09-07: the leader
///     drove the whole leg alone) - some member is making progress, so the unit is not stalled;
///   - a unit with ONE runaway member and five still ones - that is the STRAGGLER case
///     ArrivalPolicy/C15 exists for, not a stall.
/// Both are the same test from opposite ends: ANY member moving means NOT stalled.
///
/// Displacement is NET (start of window -> now), not path length: a vehicle in a limit cycle at
/// the toe of a slope (the 1-35 signature - a persistent ~2 m oscillation held for 480 s) covers
/// distance without getting anywhere, and only net displacement sees that.
/// </summary>
public static class StallPolicy
{
    public readonly record struct Decision(bool Stalled, int Moved, int Total, double MaxMeters);

    /// <summary>
    /// memberDisplacementMeters = each member's NET displacement (meters) between the oldest
    /// sample in the window and now; members whose position could not be read at BOTH ends are
    /// NOT in the list (they neither trigger nor block by themselves - they only fail to count
    /// toward minMembersWithData). totalMembers reports the unit's full member count for the log.
    /// moveMeters is a strict floor: a member at EXACTLY moveMeters counts as having MOVED (so a
    /// threshold of 0 can never call anything stalled, which is the safe degenerate case).
    /// </summary>
    public static Decision Decide(IReadOnlyList<double> memberDisplacementMeters, int totalMembers,
                                  double moveMeters, int minMembersWithData)
    {
        int withData = memberDisplacementMeters?.Count ?? 0;
        if (withData == 0 || totalMembers <= 0)
            return new Decision(false, 0, Math.Max(0, totalMembers), double.NaN);
        double max = memberDisplacementMeters.Max();
        int moved = memberDisplacementMeters.Count(d => d >= moveMeters);
        // Not enough readable members to judge: report the numbers, never the stall. A unit whose
        // members have not reflected yet must not be aborted for the reflection gap.
        if (withData < Math.Max(1, minMembersWithData))
            return new Decision(false, moved, totalMembers, max);
        return new Decision(moved == 0, moved, totalMembers, max);
    }

    // ---------------------------------------------------------------------------------------
    // WHICH CLOCK THE WINDOW RUNS ON. Both helpers are PURE - every time value is injected, no
    // time source is read in here - so the paused-sim and reader-failure cases can be exercised
    // offline by --stall-selftest without a simulation.
    //
    // The sim reading comes from VrfBridge.SimTimeSeconds() -> VrfFacade::SimTimeSeconds() ->
    // DtVrfRemoteController::simTime() (vrfcontrol/vrfRemoteController.h:356 on 5.2d, :352 on
    // 5.0.2): the BACK END's scenario clock, which runs fast under fixed-frame-run-to-complete
    // and stops when the scenario is paused. -1.0 from that reader means "no reading" (no
    // controller, or no back end discovered yet) and is NOT a time.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The window's time base, in seconds. ONE predicate decides which clock that is -
    /// UsingSimClock - so the time base, the log line and the caller's tick gate can never
    /// disagree about the same reading (pass-2 review N1/F2, pass-3 review P1).
    /// </summary>
    public static double SelectClock(bool preferSim, double simSeconds, double wallSeconds)
        => UsingSimClock(preferSim, simSeconds) ? simSeconds : wallSeconds;

    /// <summary>
    /// True when SelectClock would take the sim reading: the sim clock is used only when it was
    /// ASKED FOR (Vrf:StallClock = "sim") AND actually READ, so a build that cannot see the sim
    /// clock behaves exactly as the wall-clock watchdog did. A reading of exactly 0.0 IS a
    /// reading (a scenario at t = 0), not a failure; -1.0 is the reader's "no reading".
    ///
    /// AND THE READING MUST BE FINITE (pass-3 review P1). "simSeconds >= 0.0" alone is TRUE for
    /// +Infinity - the one non-finite value the pass-2 F2 fix left reachable. +Inf passed this
    /// gate, became clockNow, and then Admit - which pass 2 correctly taught to REFUSE a
    /// non-finite clock - returned WITHOUT APPENDING. Two outcomes, both bad: on a unit's FIRST
    /// check that left a zero-length ring for the caller to index (an unhandled
    /// IndexOutOfRangeException on the vrf-tick thread, which terminates the interface process);
    /// and on a ring that already had entries, (clockNow - oldest) >= window and
    /// (clockNow - startClock) >= grace are both trivially true against +Inf, so the gate opened
    /// on WHATEVER depth happened to exist, however little clock time it spanned - measured on
    /// the RECAL doc's own tightest true negative, a 0.23 m/s-of-sim crawler at 1.5x: a TASKABRT
    /// on the SAME TICK at wall 20 / 40 / 60, reporting 6.9 / 13.8 / 20.7 m "in the last 360
    /// SIM s" over 30-90 sim s of history. Whether DtVrfRemoteController::simTime() can return
    /// +Infinity is unproven either way and is part of C16's LIVE UNKNOWNS
    /// (VrfFacade::SimTimeSeconds, VrfFacade.cpp:600-612, manufactures only -1.0 and passes the
    /// vendor value through unfiltered); the guard costs nothing if it cannot, and is the
    /// difference between a crashed process and a wall-clock run if it can. NaN and -Infinity
    /// were already refused (NaN compares false, -Inf fails >= 0.0).
    /// </summary>
    public static bool UsingSimClock(bool preferSim, double simSeconds)
        => preferSim && double.IsFinite(simSeconds) && simSeconds >= 0.0;

    /// <summary>
    /// THE WINDOW GATE. All five arguments are seconds on the SAME clock (whichever SelectClock
    /// chose). A unit may be judged only when (a) its watch has been open for
    /// minSecondsSinceStart - the post-dispatch grace - and (b) the sample ring actually SPANS
    /// the full window: a unit 60 s into a 240 s window holds 60 s of history, and "50 m in
    /// 60 s" is a far stricter test than "50 m in 240 s".
    ///
    /// On the sim clock this is what makes a PAUSED scenario safe: simTime stops, so neither
    /// difference grows and the gate cannot open however long wall time runs.
    /// </summary>
    public static bool WindowReady(double clockNow, double oldestClock, double startClock,
                                   double windowSeconds, double minSecondsSinceStart)
        => (clockNow - startClock) >= minSecondsSinceStart
        && (clockNow - oldestClock) >= windowSeconds;

    /// <summary>
    /// SLIDING-WINDOW PRUNE. Drop the oldest sample when EITHER
    ///   (a) the next one is already at or beyond the window edge - the ordinary case, which
    ///       leaves exactly one sample at or before the edge, or
    ///   (b) the clock did not ADVANCE between the two.
    /// (b) exists only because of the sim clock. A wall clock always advances, so the old
    /// watchdog needed only (a); a scenario clock does NOT - it stops dead while the scenario is
    /// paused and it can jump BACKWARDS (DtVrfRemoteController::rollbackToSnapshot). With (a)
    /// alone a paused scenario would add a sample every StallCheckSeconds of wall time forever
    /// and prune none of them (each carrying a full member-position dictionary), and a
    /// rolled-back scenario would keep pre-rollback samples that belong to no window. Rule (b)
    /// collapses both: samples that carry no new clock information are redundant, and dropping
    /// them cannot shrink the measured window because they do not extend it.
    /// </summary>
    public static bool ShouldDropOldest(double clockNow, double oldestClock, double nextClock,
                                        double windowSeconds)
        => (clockNow - nextClock) >= windowSeconds || nextClock <= oldestClock;

    // ---------------------------------------------------------------------------------------
    // COLD-START REVIEW OF 1616614 (findings 1, 2, 4, 7, 8, 9). Everything below is PURE in the
    // same sense as the helpers above: the ring is passed in, every time value is injected, no
    // clock is read here, so --stall-selftest can drive the exact sequences the review measured.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Samples that must be inside the window before ANY verdict (review finding 4). On the SIM
    /// clock a single wall-cadence step can cover the whole window at a high sim/wall ratio, and
    /// the gate would then open on TWO position reads a few wall seconds apart - where a member
    /// whose reflection has not refreshed contributes 0 m and is indistinguishable from a frozen
    /// one. NextCheckSeconds keeps this floor reachable by shortening the cadence as the ratio
    /// rises; above ratio windowSeconds / ((MinRingDepth - 1) x 1 s) - 120x at the 360 s sim
    /// window - the 1 s cadence floor binds and the watchdog cannot judge at all. MinRingDepth
    /// entries span MinRingDepth - 1 INTERVALS, so the ceiling is 120x, not the 90x 1805ee3's
    /// docstring claimed (pass-2 review F4b; the offline model puts the measured boundary between
    /// 150x and 200x, above the analytic ceiling because the reader is stepped on a finer grid
    /// than the cadence). And it is no longer only documented: MaybeCheckStalls logs one
    /// rate-limited line when the cadence is already at its 1 s floor and the ring has stayed
    /// below this depth for a whole window - a comment is not documentation to an operator
    /// reading a run log.
    /// </summary>
    public const int MinRingDepth = 4;

    /// <summary>Consecutive readings of a new clock mode before the watchdog switches (finding 8).</summary>
    public const int ModeSwitchConfirmations = 3;

    /// <summary>Wall seconds of a motionless sim clock that earn the stale-clock warning (finding 9).</summary>
    public const double StaleClockWarnSeconds = 60.0;

    /// <summary>
    /// Rate limit, in wall seconds, for the watchdog's own STATUS lines - the clock-mode line, the
    /// rollback line and the dormancy line. 1805ee3 reused StaleClockWarnSeconds for the mode line
    /// (pass-2 review N2), which tied an unrelated log rate to the stale-clock THRESHOLD: changing
    /// one silently changed the other. They carry the same number today and mean different things.
    /// </summary>
    public const double LogRateLimitSeconds = 60.0;

    /// <summary>What the TASK clock does with a simulation clock that has gone FLAT past the stale
    /// window (Q5). Not the watchdog's decision - that one SUSPENDS judging either way.</summary>
    public enum TaskClockOnFlat
    {
        /// <summary>The clock is readable and not stale: serve it.</summary>
        ServeSim,
        /// <summary>Stale, but a VR-Forces back end is still there: the scenario is PAUSED, so the
        /// axis stays on the sim clock and adds NOTHING until it moves again.</summary>
        HoldOnSim,
        /// <summary>Stale with no back end at all, or the wall clock was asked for: serve WALL
        /// seconds, keeping everything already served.</summary>
        FallBackToWall,
    }

    /// <summary>
    /// Q5 (USER RULING 2026-09-14): DOES A PAUSED SCENARIO AGE A TASK? NO - while the back end is
    /// still there.
    ///
    /// M4 made the task clock fall back to WALL seconds after 60 wall seconds of a flat sim clock,
    /// because a back end that stops answering is DEACTIVATED rather than removed and its cached
    /// reading would otherwise freeze every end time forever, silently. The cost of that cure was
    /// that an operator who PAUSES for ten minutes burned 600 s off every armed Duration - and R4's
    /// own rule says a paused scenario does not age a task. The user ruled: HOLD while the back end
    /// is still reporting, fall back to wall only when it is gone.
    ///
    /// THE LIMIT OF THE SIGNAL, stated because it decides how much this rule is worth. The only
    /// back-end liveness the facade exposes is VrfFacade::BackendCount ->
    /// DtVrfRemoteController::backends().count(), and that list keeps a back end that has missed
    /// its status timeout: DtVrfBackendListener::doTimeouts() DEACTIVATES such an entry rather than
    /// removing it (vrfBackendListener.h; only the explicit remove() takes one out). So a non-zero
    /// count proves the back end was DISCOVERED and never removed - NOT that it is still answering.
    /// The consequence is deliberate and must be read with the log: on this signal a DEAD back end
    /// holds task time exactly as a paused one does, so the hold line repeats rather than being
    /// said once. Serving WALL seconds against a dead back end was not better - it aged tasks
    /// against a simulation that was not running - but it was at least noisy in a different way.
    /// OWED (needs a C++ facade change, hence not done here): expose the vendor's real answer -
    /// DtVrfBackendListener::lookupBackend(addr)-&gt;status() gives DtBackend::Paused vs Playing, and
    /// getControlState(addr) gives DtPauseControlType vs DtRunControlType - and decide on THAT.
    /// </summary>
    public static TaskClockOnFlat TaskClockAction(bool heldOnSim, bool stale, bool backEndPresent)
        => !heldOnSim ? TaskClockOnFlat.FallBackToWall
         : !stale ? TaskClockOnFlat.ServeSim
         : backEndPresent ? TaskClockOnFlat.HoldOnSim
         : TaskClockOnFlat.FallBackToWall;

    /// <summary>
    /// THE COARSEST CHECK CADENCE THE WINDOW CAN CARRY. MinRingDepth entries span MinRingDepth - 1
    /// intervals, so any Vrf:StallCheckSeconds above windowSeconds / (MinRingDepth - 1) leaves the
    /// front prune holding fewer than MinRingDepth samples and NOTHING is ever judged - silently,
    /// and on the DEFAULT wall path (pass-2 review F4a; measured at the shipped 240 s wall window:
    /// StallCheckSeconds 120 and 240 both went dormant, where 51d78a5 fired at 240 s in both
    /// cases). 80 s at the 240 s wall window, 120 s at the 360 s sim window.
    /// </summary>
    public static int MaxCheckSeconds(double windowSeconds)
        => Math.Max(1, (int)Math.Floor(windowSeconds / Math.Max(1, MinRingDepth - 1)));

    /// <summary>
    /// Vrf:StallCheckSeconds held against that ceiling. CLAMPED, not refused: the watchdog is
    /// report-only and ships OFF, so a mis-set knob must never stop a run that is otherwise fine,
    /// and clamping restores exactly the 51d78a5 outcome instead of silence. The caller logs one
    /// line at start-up when the clamp bites.
    /// </summary>
    public static int ClampCheckSeconds(int configuredSeconds, double windowSeconds)
        => Math.Min(Math.Max(1, configuredSeconds), MaxCheckSeconds(windowSeconds));

    /// <summary>
    /// THE CALIBRATED WINDOW, PER CLOCK. Vrf:StallWindowSeconds = 0 (the shipped default) means
    /// "use the window calibrated for whichever clock the watchdog ends up measuring on"; any
    /// positive value is used exactly as configured.
    ///   240 WALL seconds - the 2026-09-13 wall calibration over three replayed traces (G5, G3,
    ///     P11): the last false alarm disappears between a 160 s and a 170 s window, so 240/170 is
    ///     a 1.41x margin, and the clean move-threshold band there is 35-70 m.
    ///   360 SIM seconds - the 2026-09-13 sim-second RE-CALIBRATION
    ///     (docs/experiments/RECAL_STALL_SIMSECONDS_2026-09-13.md), which first reproduced EVERY
    ///     documented wall number from the same replay (the 120 s quartet and the 160 s triple to
    ///     the second, the 160/170 boundary, the 240 s fires, the 74/78 m true-negative minima,
    ///     the 35-70 m band) and then swept 100-500 sim s: the pooled false-alarm boundary at 50 m
    ///     is 250 sim s (P11 250, G3 200, G5 none), and 250 x 1.41 = 353 -> 360 on the sweep grid.
    ///     At 360 sim s the clean threshold band is 35-75 m (50 m mid-band, as at 240 wall s) and
    ///     all five true positives fire EARLIER in wall time than the 240 wall s window does -
    ///     135 s against 385 s in G5 - because the freezes happen while the sim runs fastest.
    /// The two are NOT one ratio apart: P11's sim/wall ratio varies 1.10x-1.99x WITHIN that single
    /// run, so 240 wall s covers anywhere from 264 to 478 sim s depending on the load. That is why
    /// the default is per-clock rather than a conversion.
    /// </summary>
    public const int CalibratedWindowWallSeconds = 240;
    public const int CalibratedWindowSimSeconds = 360;

    /// <summary>Vrf:StallWindowSeconds resolved against the clock actually in use (see above).</summary>
    public static double ResolveWindowSeconds(int configuredSeconds, bool usingSimClock)
        => configuredSeconds > 0 ? configuredSeconds
         : (usingSimClock ? CalibratedWindowSimSeconds : CalibratedWindowWallSeconds);

    /// <summary>
    /// ADMIT ONE SAMPLE into a unit's window, and return the watch's start clock.
    ///
    /// 1616614 pruned only at the FRONT, and the cold-start review showed that rule (b) there
    /// (nextClock &lt;= oldestClock) fires only when the two OLDEST stamps are equal - i.e. only
    /// when the pause or the rollback happens to a ring that is empty or degenerate, which is the
    /// shape the self-test used and NOT the shape a run produces. A pause that begins AFTER the
    /// ring has filled leaves ring[0] &lt; ring[1], so nothing is ever dropped (measured offline:
    /// 249 entries after 400 s of run plus 1,000 wall s paused, 1,489 after a two-hour pause,
    /// each entry a full member-position dictionary), and a rollback leaves PRE-rollback samples
    /// that the window then compares against POST-rollback positions - the cross-discontinuity
    /// false TASKABRT that rule (b) is documented to prevent.
    ///
    /// So the ring is pruned at BOTH ends:
    ///   NO CLOCK AT ALL (pass-2 review F2): a NON-FINITE reading is not a time and is refused
    ///     outright. NaN compares false against every stamp, so 1805ee3 APPENDED it (neither the
    ///     rollback nor the back branch fires against NaN) and then BOTH disjuncts of
    ///     ShouldDropOldest went false, so the front prune stopped permanently at the first NaN
    ///     that reached index 1: measured with a NaN on every 7th read at 1.5x, 1,195 entries and
    ///     still climbing, 171 of them NaN, and a measured window of 8,955 sim s against a
    ///     configured 360. The caller guards this too, with the same predicate UsingSimClock uses;
    ///     this is the belt to that pair of braces. A REFUSAL RETURNS A RING THE CALLER MAY NOT
    ///     INDEX: on a unit's FIRST check the ring is still EMPTY afterwards, so the caller must
    ///     test Count before reading ring[0] (pass-3 review P1 - 08146a2 did not, and the throw
    ///     terminates the tick thread and with it the interface process).
    ///   ROLLBACK (clockNow is BEFORE the newest stamp - DtVrfRemoteController::rollbackToSnapshot,
    ///     vrfRemoteController.h:605): every entry stamped after clockNow belongs to an abandoned
    ///     timeline. Drop them all and re-anchor the grace (startClock = clockNow): a rollback is
    ///     a NEW watch, not a continuation of the old one. The sample that ARRIVES with the
    ///     rollback replaces whatever is left at the back, because that stored payload really is
    ///     from the timeline just abandoned.
    ///   BACK (clockNow EQUALS the newest stamp - a paused scenario, or a reader whose back-end
    ///     status period is coarser than the check cadence): the sample carries no new clock
    ///     information and is DISCARDED (pass-2 review F1). 1805ee3 replaced the newest entry's
    ///     PAYLOAD under its old STAMP, arguing that the verdict has to be computed from the
    ///     newest positions - which is not so: the verdict's NOW end is the caller's own fresh
    ///     position dictionary, never ring[^1]. Nothing needs the newest payload in the ring
    ///     except when that entry later becomes ring[0] - the harmful case. A ring of ONE entry is
    ///     at both ends at once, so after a replace the window was measured from an OLD stamp
    ///     against positions read up to a whole flat-clock interval LATER: the true elapsed
    ///     interval is SHORTER than the window it is judged against, which is the FALSE-POSITIVE
    ///     direction. Measured on 1805ee3: a unit crawling at 0.23 m/s of SIM time - the RECAL
    ///     doc's own tightest true negative - was reported STALLED after a 40-wall-second flat
    ///     reading at 6.21x, on 35.7 m over a nominal 360 sim s window whose true displacement was
    ///     82.8 m. Discarding costs the ring bound nothing: nothing is added, so a pause of any
    ///     length still leaves the ring at its filled size.
    ///   FRONT (rule (a), unchanged): keep exactly one sample at or before the window edge.
    /// </summary>
    public static double Admit<T>(List<(double Clock, T P)> ring, double clockNow, T sample,
                                  double windowSeconds, double startClock)
    {
        if (ring is null || !double.IsFinite(clockNow)) return startClock;
        bool rolledBack = false;
        if (ring.Count > 0 && clockNow < ring[^1].Clock)
        {
            while (ring.Count > 0 && ring[^1].Clock > clockNow) ring.RemoveAt(ring.Count - 1);
            startClock = clockNow;
            rolledBack = true;
        }
        if (double.IsNaN(startClock)) startClock = clockNow;
        if (ring.Count > 0 && clockNow <= ring[^1].Clock)
        {
            if (rolledBack) ring[^1] = (clockNow, sample);
        }
        else ring.Add((clockNow, sample));
        while (ring.Count >= 2 && ShouldDropOldest(clockNow, ring[0].Clock, ring[1].Clock, windowSeconds))
            ring.RemoveAt(0);
        return startClock;
    }

    /// <summary>
    /// THE FULL GATE the product judges on. WindowReady measures the window on the SELECTED clock;
    /// the floors that clock window cannot supply are ANDed onto it (review finding 4):
    ///   RING DEPTH - see MinRingDepth. A verdict on two position reads is not a measurement.
    ///     Applies on BOTH clocks, always.
    ///   WALL FLOOR - 51d78a5 gated on wall seconds since the in-flight record's own DispatchedUtc
    ///     and 1616614 dropped that floor entirely when it moved the anchor to the watch's first
    ///     sample. The same StallMinSecondsSinceDispatch is kept here as a WALL floor on the WALL
    ///     path, where it IS the 51d78a5 behaviour. It is NOT applied on the SIM path
    ///     (applyWallFloor false - pass-2 review F8): 1805ee3 ANDed the same 60 s onto both clocks,
    ///     and since 360 sim s is only ~58 wall s at the 6.21x measured on G5, above ratio ~6x the
    ///     FLOOR - not the calibrated window - decided when the watchdog fires. Measured at 60x:
    ///     the first verdict landed at wall 60 s / sim 3,600 s, ten times the calibrated window,
    ///     silently negating the "fires EARLIER in wall time" property the 360 s default was
    ///     chosen for, at exactly the high ratios that motivated the sim clock. The two-sample
    ///     case the floor incidentally covered is covered by MinRingDepth, which 51d78a5 did not
    ///     have; the floor's other stated purpose - "no task is ever judged inside its first
    ///     minute of real time" - is a policy choice nothing measures.
    /// </summary>
    public static bool JudgeReady(double clockNow, double oldestClock, double startClock,
                                  double windowSeconds, double minSecondsSinceStart,
                                  int ringDepth, double wallSecondsSinceDispatch,
                                  bool applyWallFloor)
        => ringDepth >= MinRingDepth
        && (!applyWallFloor || wallSecondsSinceDispatch >= minSecondsSinceStart)
        && WindowReady(clockNow, oldestClock, startClock, windowSeconds, minSecondsSinceStart);

    /// <summary>
    /// SAMPLING CADENCE when the window is not on wall time (review finding 4). The cadence is a
    /// wall-time sampling rate but it sets the ring's RESOLUTION in sim seconds
    /// (windowSeconds / (cadence x ratio) samples), so at a high ratio the front prune collapses
    /// the ring to the two entries it must keep and MinRingDepth would become a permanent OFF
    /// switch. Sample often enough that one step advances the sim clock by at most
    /// windowSeconds / MinRingDepth; never slower than configured, never faster than 1 s (the
    /// pre-existing floor). A non-positive or non-finite ratio (first check, frozen clock) leaves
    /// the configured cadence alone.
    /// </summary>
    public static double NextCheckSeconds(double configuredSeconds, double windowSeconds,
                                          double simPerWallRatio)
    {
        double slowest = Math.Max(1.0, configuredSeconds);
        if (double.IsNaN(simPerWallRatio) || double.IsInfinity(simPerWallRatio) || simPerWallRatio <= 0.0)
            return slowest;
        double wanted = windowSeconds / Math.Max(1, MinRingDepth) / simPerWallRatio;
        return Math.Clamp(wanted, 1.0, slowest);
    }

    /// <summary>
    /// Vrf:StallClock -> preferSim. EXACTLY "sim" or "wall" (trimmed, case-insensitive); anything
    /// else - "", "Wal", "walltime", "WALL " with a trailing space, "true" - is a CONFIGURATION
    /// ERROR and resolves to WALL, the mode measured live so far (review findings 5 and 7, as
    /// narrowed by pass-2 F7 and pass-3 P2: BOTH windows are calibrated now - this same branch
    /// derived the 360 sim s default - so the reason a typo must resolve to wall is NOT that the
    /// sim window is un-derived; it is that only the wall clock has been exercised in a run).
    /// valid=false tells the caller to log one line.
    /// </summary>
    public static bool ParseClockPreference(string configured, out bool valid)
    {
        string s = (configured ?? string.Empty).Trim();
        if (string.Equals(s, "sim", StringComparison.OrdinalIgnoreCase)) { valid = true; return true; }
        valid = string.Equals(s, "wall", StringComparison.OrdinalIgnoreCase);
        return false;
    }

    /// <summary>
    /// CLOCK-MODE HYSTERESIS (review finding 8). Every mode change drops every ring, so a reader
    /// that alternates -1 / &gt;= 0 at the check cadence - the "back end briefly out of the list"
    /// case the watchdog says it cares about - would clear the watchdog's whole memory on every
    /// tick, judge nothing ever, and log a mode line each time instead of the promised one. A
    /// change is adopted only after confirmations CONSECUTIVE readings of the new mode; the
    /// first resolution (mode 0) is adopted at once, because nothing has been sampled yet.
    /// Modes: 1 = simulation clock, 2 = wall clock.
    /// </summary>
    public static (int Mode, int Candidate, int Streak) NextClockMode(
        int mode, int candidate, int streak, int observed, int confirmations)
    {
        if (observed == mode) return (mode, 0, 0);
        if (mode == 0) return (observed, 0, 0);
        int s = (observed == candidate) ? streak + 1 : 1;
        return s >= Math.Max(1, confirmations) ? (observed, 0, 0) : (mode, observed, s);
    }

    /// <summary>
    /// STALE (as opposed to PAUSED) SIM CLOCK (review finding 9). VrfFacade::SimTimeSeconds gates
    /// on backends().count() &gt; 0, and a back end that stops answering is DEACTIVATED, not
    /// removed - vrfBackendListener.h:161-163 "deactivates any status objects which have not
    /// responded within the timeout interval", while :154-155 documents remove() as "Normally,
    /// this should not need to get called!". So count() stays &gt; 0, the reader keeps returning
    /// the last cached value, and the watchdog reads it as a paused scenario and reports nothing
    /// for the rest of the run, silently - a new silence in front of the one C16 exists to catch.
    /// True here means the clock has not advanced for staleAfterSeconds of WALL time; the caller
    /// warns ONCE and stops judging until it moves again.
    /// </summary>
    public static bool SimClockStale(double clockNow, double lastAdvancedValue,
                                     double wallNowSeconds, double wallLastAdvancedSeconds,
                                     double staleAfterSeconds)
        => clockNow <= lastAdvancedValue
        && (wallNowSeconds - wallLastAdvancedSeconds) >= staleAfterSeconds;

    /// <summary>Where a new sim reading sits against the PREVIOUS one.</summary>
    public enum SimClockStep { Flat = 0, Advanced = 1, RolledBack = 2 }

    /// <summary>
    /// A BACKWARDS STEP IS A CHANGE, NOT A NON-ADVANCE (pass-2 review F3). 1805ee3 held the
    /// caller's _stallSimClockLast as a HIGH-WATER mark and refreshed the "last advanced" wall
    /// time only on clockNow &gt; that mark. After DtVrfRemoteController::rollbackToSnapshot the
    /// clock is genuinely ADVANCING, but below the mark, so SimClockStale went true 60 wall
    /// seconds later and the watchdog stopped judging EVERY unit until the clock climbed back past
    /// its pre-rollback value - measured: 45, 420 and 945 wall seconds of suspended judging for
    /// rollbacks of 100, 475 and 1,000 sim s - under the warning text "the scenario is PAUSED, or
    /// the back end has stopped answering", which is not what happened and which the operator has
    /// no way to see through. Admit already handles a backwards step correctly and explicitly;
    /// this is the stale detector agreeing with it. Only a FLAT reading is a non-advance, and only
    /// a flat reading can be stale - so the caller reaches SimClockStale on Flat alone.
    /// </summary>
    public static SimClockStep ClassifyClockStep(double clockNow, double lastSeen)
        => clockNow > lastSeen ? SimClockStep.Advanced
         : clockNow < lastSeen ? SimClockStep.RolledBack
         : SimClockStep.Flat;

    /// <summary>
    /// Whole windows of un-judged clock time that must pass, WHILE SAMPLES ARE BEING TAKEN,
    /// before the watchdog admits it is dormant. One window is the minimum any ring must span
    /// before a first verdict is even possible, so silence for one window is ordinary start-up;
    /// a SECOND full window on top of that is silence that has no innocent reading.
    /// </summary>
    public const int DormancyWindows = 2;

    /// <summary>
    /// IS THE WATCHDOG JUDGING ANYTHING AT ALL? (pass-3 review P4.) 1805ee3 said nothing in this
    /// state; 08146a2 armed a line on ONE CAUSE - "the cadence is already at its 1 s floor and
    /// the deepest ring is below MinRingDepth" - which is the HIGH-RATIO cause only. It misses
    /// every other way the watchdog can fall silent, and the measured one is MODE THRASH: a
    /// reader that is out for ModeSwitchConfirmations or more CONSECUTIVE checks (the
    /// deactivated-back-end shape this watchdog exists to survive) flips the clock mode for
    /// real, and every flip DROPS EVERY RING and swaps the window 360 &lt;-&gt; 240. Measured: 3
    /// consecutive misses in every 10 reads at 1.5x, frozen unit, 3,000 wall s - 0 verdicts, 0
    /// judgeable checks, 40 mode lines, and not one word that no unit was being watched. The
    /// cadence never leaves 5 s at 1.5x, so 08146a2's arming condition is never even evaluated
    /// true. A reader that keeps going out, and units re-tasked faster than one window, are two
    /// more causes with the same signature.
    ///
    /// So the watch is armed on the OBSERVABLE CONDITION instead of on any one cause: NO UNIT HAS
    /// SATISFIED JudgeReady while samples were being taken. clockNow and lastJudgeableClock are
    /// on the caller's own MONOTONE UN-JUDGED AXIS - the sum of the per-check advances of
    /// whichever clock was in effect, accumulated only across checks that actually sampled and
    /// never across a mode change (the two clocks are not comparable, and the wall clock's epoch
    /// is astronomically larger than a scenario clock's). That axis is what makes this test
    /// survive the thrash it is meant to catch, and it is why the caller may not simply subtract
    /// two readings of the selected clock.
    ///
    /// samplingActive is the caller's "at least one unit was sampled on this check": with no move
    /// task in flight, during a stale-clock hold (which announces itself) or through a sim-clock
    /// blackout (ditto), dormancy is not a question and the axis does not advance.
    /// </summary>
    public static bool DormancyDue(double lastJudgeableClock, double clockNow,
                                   double windowSeconds, bool samplingActive)
        => samplingActive
        && windowSeconds > 0.0
        && double.IsFinite(lastJudgeableClock) && double.IsFinite(clockNow)
        && (clockNow - lastJudgeableClock) >= DormancyWindows * windowSeconds;
}

public static class StallSelfTest
{
    public static int Run()
    {
        int fails = 0;
        void Check(string what, bool cond) { Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what); if (!cond) fails++; }

        var d = StallPolicy.Decide(Array.Empty<double>(), 6, 50, 1);
        Check("no readable member -> NOT stalled (nothing to judge)", !d.Stalled && d.Moved == 0 && d.Total == 6);

        d = StallPolicy.Decide(null, 6, 50, 1);
        Check("null displacement list -> NOT stalled (no throw)", !d.Stalled);

        d = StallPolicy.Decide(new double[] { 1.9 }, 1, 50, 1);
        Check("lone entity, 1.9 m in the window -> STALLED (the 1-35 limit-cycle signature)", d.Stalled && d.Total == 1);

        d = StallPolicy.Decide(new double[] { 620 }, 1, 50, 1);
        Check("lone entity, 620 m in the window -> NOT stalled", !d.Stalled && d.Moved == 1);

        d = StallPolicy.Decide(new double[] { 2.0, 0.4, 1.1, 0.0, 3.3, 0.8 }, 6, 50, 1);
        Check("all six members still -> STALLED, max reported", d.Stalled && Math.Abs(d.MaxMeters - 3.3) < 1e-9);

        d = StallPolicy.Decide(new double[] { 1240, 0.4, 1.1, 0.0, 3.3, 0.8 }, 6, 50, 1);
        Check("LEADER moving 1240 m, five followers still -> NOT stalled (the 1-6 leader-alone case)",
              !d.Stalled && d.Moved == 1);

        d = StallPolicy.Decide(new double[] { 0.4, 1.1, 0.0, 3.3, 0.8, 7300 }, 6, 50, 1);
        Check("ONE runaway member, five still -> NOT stalled (straggler case, C15's job not C16's)", !d.Stalled);

        d = StallPolicy.Decide(new double[] { 50.0, 2.0, 1.0 }, 3, 50, 1);
        Check("a member EXACTLY at the threshold counts as MOVED -> NOT stalled", !d.Stalled && d.Moved == 1);

        d = StallPolicy.Decide(new double[] { 49.999, 2.0, 1.0 }, 3, 50, 1);
        Check("the same member just BELOW the threshold -> STALLED", d.Stalled && d.Moved == 0);

        d = StallPolicy.Decide(new double[] { 2.0, 1.0 }, 6, 50, 3);
        Check("only 2 of 6 members readable with minMembersWithData=3 -> NOT stalled (insufficient data)", !d.Stalled);

        d = StallPolicy.Decide(new double[] { 2.0, 1.0, 0.5 }, 6, 50, 3);
        Check("3 readable, all still, minMembersWithData=3 -> STALLED", d.Stalled);

        d = StallPolicy.Decide(new double[] { 2.0, 1.0, 0.5 }, 0, 50, 1);
        Check("totalMembers 0 (nothing materialized) -> NOT stalled", !d.Stalled);

        d = StallPolicy.Decide(new double[] { 0.0, 0.0 }, 2, 0, 1);
        Check("moveMeters 0 can never call a stall (degenerate config is safe)", !d.Stalled && d.Moved == 2);

        d = StallPolicy.Decide(new double[] { 12.0, 9.0, 11.0, 10.0 }, 6, 50, 1);
        Check("4 readable of 6, all under the threshold -> STALLED (unreadable members do not block)",
              d.Stalled && d.Total == 6 && Math.Abs(d.MaxMeters - 12.0) < 1e-9);

        // -- CLOCK SELECTION (Vrf:StallClock x VrfBridge.SimTimeSeconds) ------------------------

        Check("StallClock=sim with a readable clock -> the SIM reading is the window's time base",
              StallPolicy.SelectClock(true, 1480.0, 12345.0) == 1480.0 && StallPolicy.UsingSimClock(true, 1480.0));

        Check("a sim clock of exactly 0 (scenario just loaded) is a READING, not a failure",
              StallPolicy.SelectClock(true, 0.0, 12345.0) == 0.0 && StallPolicy.UsingSimClock(true, 0.0));

        Check("reader returns -1 (no controller / no back end yet) -> FALL BACK to wall seconds",
              StallPolicy.SelectClock(true, -1.0, 12345.0) == 12345.0 && !StallPolicy.UsingSimClock(true, -1.0));

        Check("StallClock=wall ignores a perfectly good sim reading",
              StallPolicy.SelectClock(false, 1480.0, 12345.0) == 12345.0 && !StallPolicy.UsingSimClock(false, 1480.0));

        // -- THE WINDOW GATE, on whichever clock was selected -----------------------------------

        Check("full window + grace served -> the gate OPENS",
              StallPolicy.WindowReady(300.0, 60.0, 0.0, 240.0, 60.0));

        Check("window full but still inside the post-dispatch grace -> gate SHUT",
              !StallPolicy.WindowReady(250.0, 0.0, 200.0, 240.0, 60.0));

        Check("grace served but the ring spans only 239 s of a 240 s window -> gate SHUT",
              !StallPolicy.WindowReady(300.0, 61.0, 0.0, 240.0, 60.0));

        // A PAUSED SIMULATION. Wall time runs, the scenario clock does not: 100 checks x 5 wall
        // seconds against a simTime frozen at 1,480.0. The window must NEVER open - even though
        // every member is motionless, which on the wall clock is exactly the pattern that aborts
        // the task. This is the whole reason the reader exists.
        {
            double simFrozen = 1480.0, wall = 0.0;
            double start = StallPolicy.SelectClock(true, simFrozen, wall);
            double oldest = start;
            bool everReadyOnSim = false, everReadyOnWall = false;
            for (int i = 0; i < 100; i++)
            {
                wall += 5.0;
                everReadyOnSim |= StallPolicy.WindowReady(
                    StallPolicy.SelectClock(true, simFrozen, wall), oldest, start, 240.0, 60.0);
                everReadyOnWall |= StallPolicy.WindowReady(wall, 0.0, 0.0, 240.0, 60.0);
            }
            Check("PAUSED SIM: 500 s of wall time at a frozen sim clock NEVER opens the window", !everReadyOnSim);
            Check("...while the same 500 s measured on the WALL clock does (the behaviour this replaces)",
                  everReadyOnWall);
        }

        // READER DEAD FOR THE WHOLE RUN. -1.0 every time: the watchdog must revert to wall
        // seconds and keep working, not go silent.
        {
            double wall = 0.0;
            bool ready = false;
            for (int i = 0; i < 100; i++)
            {
                wall += 5.0;
                ready |= StallPolicy.WindowReady(StallPolicy.SelectClock(true, -1.0, wall), 0.0, 0.0, 240.0, 60.0);
            }
            Check("reader returns -1 for the whole run -> the WALL window still opens (graceful fallback)", ready);
        }

        // FIXED-FRAME-RUN-TO-COMPLETE, 6.21x (the 2026-09-13 G5 run). The 240 s window now fills
        // after ~38.6 s of WALL time - the abort lands at 240 sim seconds of no progress instead
        // of 240 x 6.21 = 1,490.
        Check("at 6.21x a 240 s SIM window is full while only 38.6 wall s have passed",
              StallPolicy.WindowReady(240.0, 0.0, 0.0, 240.0, 60.0)
              && !StallPolicy.WindowReady(38.6, 0.0, 0.0, 240.0, 60.0));

        // -- SLIDING-WINDOW PRUNE (the ring must stay bounded on a clock that can stop) ---------

        Check("ordinary prune: the next sample is past the window edge -> drop the oldest",
              StallPolicy.ShouldDropOldest(300.0, 0.0, 55.0, 240.0));

        Check("ordinary keep: dropping would leave the ring spanning only 239 s -> keep the oldest",
              !StallPolicy.ShouldDropOldest(300.0, 0.0, 61.0, 240.0));

        Check("FROZEN clock (paused scenario): a sample that did not advance the clock is dropped",
              StallPolicy.ShouldDropOldest(1480.0, 1480.0, 1480.0, 240.0));

        Check("clock jumped BACKWARDS (rollback to snapshot): the stale sample is dropped",
              StallPolicy.ShouldDropOldest(1000.0, 1480.0, 1000.0, 240.0));

        // The buffer-growth guard, run as the tick thread would: a paused scenario, sampled every
        // 5 wall seconds for 100 checks. Without rule (b) this ring reaches 100 entries, each a
        // member-position dictionary, and never prunes - for as long as the pause lasts.
        {
            var ring = new List<double>();
            double simFrozen = 1480.0;
            for (int i = 0; i < 100; i++)
            {
                ring.Add(simFrozen);
                while (ring.Count >= 2 && StallPolicy.ShouldDropOldest(simFrozen, ring[0], ring[1], 240.0))
                    ring.RemoveAt(0);
            }
            // It collapses to ONE: each new sample prunes its predecessor, and a one-sample ring
            // spans zero seconds, so the window stays shut for the whole pause and rebuilds from
            // the moment the scenario resumes - which is the correct window to measure.
            Check("PAUSED SIM: 100 samples at a frozen clock leave the ring at 1 entry, not 100",
                  ring.Count == 1);
        }

        // ... and the same loop on a clock that IS advancing keeps a full window of history:
        // 5 s per sample over a 240 s window is 48 intervals, so 49 or 50 samples.
        {
            var ring = new List<double>();
            double c = 0.0;
            for (int i = 0; i < 100; i++)
            {
                c += 5.0;
                ring.Add(c);
                while (ring.Count >= 2 && StallPolicy.ShouldDropOldest(c, ring[0], ring[1], 240.0))
                    ring.RemoveAt(0);
            }
            Check("RUNNING clock: the ring holds one full 240 s window and no more",
                  ring.Count >= 2 && (c - ring[0]) >= 240.0 && (c - ring[1]) < 240.0);
        }

        // =====================================================================================
        // COLD-START REVIEW OF 1616614. Every block below drives the PRODUCT helpers
        // (StallPolicy.Admit -> StallPolicy.JudgeReady -> StallPolicy.Decide) with the exact
        // sequence the review measured, NOT with the degenerate shape the cases above use: the
        // 1616614 ring cases seed a frozen or 2-entry ring FROM EMPTY, which is the one shape the
        // old front-only prune collapses, so they passed while the product case failed. The
        // payload here is a scalar position in metres along the leg, so one member's net
        // displacement over the window is |now - oldest|.
        // =====================================================================================

        // FINDING 1 - A PAUSE THAT BEGINS AFTER THE RING HAS FILLED. 400 s of an advancing clock
        // at the 5 s cadence with the unit MOVING, then the scenario is paused: 1,000 wall s of
        // samples at a frozen clock and frozen positions. Measured on 1616614: the ring reached
        // 249 entries and kept growing for the length of the pause.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, clock = 0.0, pos = 0.0;
            for (int i = 0; i < 80; i++)
            {
                clock += 5.0; pos += 10.0;                  // 2 m/s - an ordinary road speed
                start = StallPolicy.Admit(ring, clock, pos, 240.0, start);
            }
            int filled = ring.Count;
            int maxDuringPause = filled, verdicts = 0;
            double wall = 400.0;
            for (int i = 0; i < 200; i++)                    // 200 x 5 wall s = 1,000 wall s paused
            {
                wall += 5.0;
                start = StallPolicy.Admit(ring, clock, pos, 240.0, start);
                if (ring.Count > maxDuringPause) maxDuringPause = ring.Count;
                var old = ring[0];
                if (StallPolicy.JudgeReady(clock, old.Clock, start, 240.0, 60.0, ring.Count, wall,
                                           applyWallFloor: true)
                    && StallPolicy.Decide(new[] { Math.Abs(pos - old.P) }, 1, 50.0, 1).Stalled)
                    verdicts++;
            }
            Check("PAUSE AFTER THE RING FILLED: 1,000 wall s of samples at a frozen clock leave the "
                  + "ring at its filled size, not 200 entries longer",
                  filled >= 40 && maxDuringPause <= filled + 1 && ring.Count <= filled + 1);
            Check("(INVARIANT - passes against the pre-fix logic too; pass-2 review F5) ... and the ring "
                  + "still holds the whole pre-pause window, so nothing is manufactured during the pause",
                  verdicts == 0 && (clock - ring[0].Clock) >= 240.0);
        }

        // FINDING 2 - ROLLBACK TO SNAPSHOT WITH A FULL RING. The review's sequence: 5 s cadence up
        // to sim 1480, DtVrfRemoteController::rollbackToSnapshot puts the clock (and the units)
        // back to 1005, then the run advances again. Measured on 1616614: the ring reached 145
        // entries and the first window that re-opened compared a POST-rollback position against
        // the sample stamped 1240 - recorded BEFORE the rollback.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, clock = 0.0, pos = 0.0;
            for (int i = 0; i < 296; i++)
            {
                clock += 5.0; pos += 10.0;
                start = StallPolicy.Admit(ring, clock, pos, 240.0, start);
            }
            double beforeRollback = clock;                  // 1480
            clock = 1005.0; pos = 2010.0;                   // the snapshot's clock AND its positions
            start = StallPolicy.Admit(ring, clock, pos, 240.0, start);
            Check("ROLLBACK: no sample from the abandoned timeline survives - the ring's oldest entry "
                  + "is never stamped after the rolled-back clock",
                  beforeRollback > clock && ring.Count > 0 && ring[0].Clock <= clock
                  && !ring.Any(e => e.Clock > clock));
            Check("... and the rollback re-arms the watch (a rollback is a NEW watch, not a continuation)",
                  Math.Abs(start - 1005.0) < 1e-9);
            int verdicts = 0; double oldestAtFirstVerdict = double.NaN;
            double wall = 2000.0;                           // well past every wall floor
            for (int i = 0; i < 200 && verdicts == 0; i++)
            {
                clock += 5.0; wall += 5.0;                  // the unit is FROZEN after the rollback
                start = StallPolicy.Admit(ring, clock, pos, 240.0, start);
                var old = ring[0];
                if (StallPolicy.JudgeReady(clock, old.Clock, start, 240.0, 60.0, ring.Count, wall,
                                           applyWallFloor: true)
                    && StallPolicy.Decide(new[] { Math.Abs(pos - old.P) }, 1, 50.0, 1).Stalled)
                { verdicts++; oldestAtFirstVerdict = old.Clock; }
            }
            Check("(INVARIANT - the pre-fix ring's oldest entry at this point is stamped 1480, which also "
                  + "satisfies >= 1005 while being a PRE-rollback sample; the check above carries the load "
                  + "- pass-2 review F5) ... so the first verdict after the rollback is computed from a "
                  + "sample stamped AFTER it, never across the discontinuity",
                  verdicts == 1 && oldestAtFirstVerdict >= 1005.0);
        }

        // FINDING 3 - A NEW DISPATCH IS A NEW WINDOW. In the product this is
        // VrfC2SimService.MarkDispatched dropping the unit's _stallSamples whenever the new task
        // carries a destination (VrfC2SimService.cs:2186-2190), which is the DEFAULT aggregate
        // move path (MarkDispatched at :2146, dest = routeGeo[^1]) and the R11 plan-move path
        // (:2049) - on both, 51d78a5 re-armed 60 s of grace from the record's own DispatchedUtc
        // and 1616614 left the PREVIOUS task's ring and anchor in place. Policy-level equivalent:
        // after the clear the watch must re-anchor on its next sample and stay shut for a whole
        // grace + window again.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, clock = 1000.0;
            for (int i = 0; i < 80; i++) { clock += 5.0; start = StallPolicy.Admit(ring, clock, 0.0, 240.0, start); }
            bool readyBefore = StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count,
                                                      9999.0, applyWallFloor: true);
            ring.Clear(); start = double.NaN;               // <- MarkDispatched: a new move task
            start = StallPolicy.Admit(ring, clock, 0.0, 240.0, start);
            bool readyAfter = StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count,
                                                     9999.0, applyWallFloor: true);
            Check("(INVARIANT - this block clears the ring ITSELF, so it exercises no product code: the "
                  + "actual finding-3 change is VrfC2SimService.MarkDispatched dropping _stallSamples, "
                  + "which the self-test harness cannot reach - pass-2 review F5) FRESH DISPATCH re-arms "
                  + "the watch: StartClock becomes the clock at the first sample after the clear and the "
                  + "previous task's window can no longer be judged",
                  readyBefore && !readyAfter && Math.Abs(start - clock) < 1e-9 && ring.Count == 1);
        }

        // FINDING 4 - NO VERDICT ON TWO SAMPLES. 60x under fixed-frame-run-to-complete: a 5 s wall
        // cadence steps the SIM clock by 300 s, so the 240 s window is spanned by the SECOND
        // sample and 1616614 would judge there - two position reads, 10 wall seconds into the
        // task, over a measured span 25 % wider than the configured window.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, clock = 0.0, wall = 0.0;
            bool clockWindowOpenAtTwo = false; int firstJudgeable = -1;
            for (int i = 1; i <= 24; i++)
            {
                clock += 300.0; wall += 5.0;
                start = StallPolicy.Admit(ring, clock, 0.0, 240.0, start);
                if (i == 2) clockWindowOpenAtTwo = StallPolicy.WindowReady(clock, ring[0].Clock, start, 240.0, 60.0)
                                                   && ring.Count == 2;
                if (firstJudgeable < 0 &&
                    StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count, wall,
                                           applyWallFloor: true))
                    firstJudgeable = i;
            }
            Check("60x ratio: the clock window ALONE is satisfied on sample 2 - that is the hole",
                  clockWindowOpenAtTwo);
            Check("... and the ring-depth floor keeps the gate shut on a 2-entry ring, whatever the "
                  + "wall time (this is why the cadence has to follow the clock)", firstJudgeable < 0);
            // The cadence that makes the depth floor reachable: at 60x, one step may advance the
            // sim clock by at most 240 / 4 = 60 s, so the check interval drops to the 1 s floor.
            double cadence = StallPolicy.NextCheckSeconds(5.0, 240.0, 60.0);
            Check("... so at 60x the sampling cadence drops from 5 s to the 1 s floor",
                  Math.Abs(cadence - 1.0) < 1e-9
                  && Math.Abs(StallPolicy.NextCheckSeconds(5.0, 240.0, 6.21) - 5.0) < 1e-9
                  && Math.Abs(StallPolicy.NextCheckSeconds(5.0, 240.0, 12.0) - 5.0) < 1e-9
                  && Math.Abs(StallPolicy.NextCheckSeconds(5.0, 240.0, -1.0) - 5.0) < 1e-9);
            ring.Clear(); start = double.NaN; clock = 0.0; wall = 0.0; firstJudgeable = -1;
            int firstWithWallFloor = -1;
            for (int i = 1; i <= 120; i++)
            {
                clock += 60.0; wall += cadence;             // 60x sampled at the adapted cadence
                start = StallPolicy.Admit(ring, clock, 0.0, 240.0, start);
                if (firstJudgeable < 0 &&
                    StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count, wall,
                                           applyWallFloor: false))
                    firstJudgeable = i;
                if (firstWithWallFloor < 0 &&
                    StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count, wall,
                                           applyWallFloor: true))
                    firstWithWallFloor = i;
            }
            Check("... and at that cadence the window opens on a ring 5 deep at sample 5 - not sample 2 "
                  + "at 10 wall s. The 60 WALL-second floor would push it to sample 60 (ten times the "
                  + "calibrated window in sim seconds), which is why it is not applied on the sim clock "
                  + "(pass-2 review F8)",
                  firstJudgeable == 5 && firstWithWallFloor == 60
                  && ring.Count >= StallPolicy.MinRingDepth);
        }

        // FINDING 7 - Vrf:StallClock is validated; a typo must not select the mode that has never
        // been exercised in a run (pass-3 review P2: it is not "un-calibrated" - this branch
        // calibrated it - it is un-MEASURED, which is a different and smaller claim).
        Check("StallClock accepts exactly \"sim\", trimmed and case-insensitive",
              StallPolicy.ParseClockPreference("  SiM ", out bool okSim) && okSim);
        Check("StallClock accepts exactly \"wall\", trimmed and case-insensitive",
              !StallPolicy.ParseClockPreference("WALL ", out bool okWall) && okWall);
        Check("every other value is a CONFIGURATION ERROR and falls back to WALL, the mode measured "
              + "live so far",
              !StallPolicy.ParseClockPreference("walltime", out bool e1) && !e1
              && !StallPolicy.ParseClockPreference("Wal", out bool e2) && !e2
              && !StallPolicy.ParseClockPreference("", out bool e3) && !e3
              && !StallPolicy.ParseClockPreference(null, out bool e4) && !e4
              && !StallPolicy.ParseClockPreference("true", out bool e5) && !e5);

        // FINDING 8 - a flapping reader must not clear every ring on every flip.
        {
            int mode = 0, cand = 0, streak = 0, switches = 0;
            (mode, cand, streak) = StallPolicy.NextClockMode(mode, cand, streak, 1,
                                                            StallPolicy.ModeSwitchConfirmations);
            Check("the FIRST clock resolution is adopted at once (nothing has been sampled yet)",
                  mode == 1 && cand == 0 && streak == 0);
            for (int i = 0; i < 50; i++)
            {
                int before = mode;
                (mode, cand, streak) = StallPolicy.NextClockMode(mode, cand, streak, (i % 2 == 0) ? 2 : 1,
                                                                StallPolicy.ModeSwitchConfirmations);
                if (mode != before) switches++;
            }
            Check("a reader FLAPPING -1 / >= 0 at the check cadence never switches the mode, so no ring "
                  + "is ever cleared and no mode line is ever logged", switches == 0 && mode == 1);
            int adoptedAt = -1;
            for (int i = 1; i <= 5 && adoptedAt < 0; i++)
            {
                int before = mode;
                (mode, cand, streak) = StallPolicy.NextClockMode(mode, cand, streak, 2,
                                                                StallPolicy.ModeSwitchConfirmations);
                if (mode != before) adoptedAt = i;
            }
            Check("... but a SUSTAINED change is adopted, on the third consecutive reading",
                  adoptedAt == StallPolicy.ModeSwitchConfirmations && mode == 2);
        }

        // PER-CLOCK CALIBRATED DEFAULT (supervisor ruling 2026-09-13 on the sim-second
        // re-calibration): Vrf:StallWindowSeconds = 0 takes the window calibrated for the clock in
        // use - 240 WALL s, 360 SIM s - and the two are not a conversion of each other.
        Check("StallWindowSeconds=0 on the WALL clock resolves to the 240 s wall calibration",
              StallPolicy.ResolveWindowSeconds(0, false) == 240.0
              && StallPolicy.ResolveWindowSeconds(0, false) == StallPolicy.CalibratedWindowWallSeconds);
        Check("StallWindowSeconds=0 on the SIM clock resolves to the 360 s sim-second calibration",
              StallPolicy.ResolveWindowSeconds(0, true) == 360.0
              && StallPolicy.ResolveWindowSeconds(0, true) == StallPolicy.CalibratedWindowSimSeconds);
        Check("an EXPLICIT StallWindowSeconds is used as configured, on either clock",
              StallPolicy.ResolveWindowSeconds(240, true) == 240.0
              && StallPolicy.ResolveWindowSeconds(999, false) == 999.0);
        Check("a zero-or-negative window is not a window: it falls back to the clock's calibrated default",
              StallPolicy.ResolveWindowSeconds(-5, true) == 360.0
              && StallPolicy.ResolveWindowSeconds(-5, false) == 240.0);

        // FINDING 9 - a STALE clock (a deactivated back end still in backends()) is not a pause.
        Check("STALE SIM CLOCK: 60 wall s with no advance is stale; 59 s is not, and any advance clears it",
              StallPolicy.SimClockStale(1480.0, 1480.0, 1000.0, 940.0, 60.0)
              && !StallPolicy.SimClockStale(1480.0, 1480.0, 999.0, 940.0, 60.0)
              && !StallPolicy.SimClockStale(1485.0, 1480.0, 1000.0, 940.0, 60.0));

        // =====================================================================================
        // PASS-2 COLD-START REVIEW OF 1805ee3 (F1, F2, F3, F4, F8). Every block drives the PRODUCT
        // helpers with the sequence the reviewer measured, and each FAILS against the logic it
        // replaces - the pre-fix numbers quoted below were reproduced here, from this file, before
        // the fix was applied. All four defects are SIM-clock only except F4a, which is a
        // regression against 51d78a5 on the DEFAULT wall path.
        // =====================================================================================

        // F2 - A NaN SIM READING IS NOT A CLOCK. NaN compares false against every stamp, so 1805ee3
        // appended it and then BOTH disjuncts of ShouldDropOldest went false: the front prune
        // stopped permanently at the first NaN that reached index 1, the ring grew without bound
        // and the measured window silently became "since the first NaN". Measured on 1805ee3 with
        // a NaN on every 7th read at 1.5x: max ring 1,195 (171 NaN stamps), window 8,955 sim s
        // against a configured 360.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN;
            int maxRing = 0;
            for (int i = 1; i <= 1200; i++)                  // 5 wall s cadence, ratio 1.5x
            {
                double sim = 7.5 * i;                        // 7.5 sim s per 5 wall s
                double reading = (i % 7 == 0) ? double.NaN : sim;   // the reader hiccups
                start = StallPolicy.Admit(ring, reading, 2.0 * sim, 360.0, start);
                if (ring.Count > maxRing) maxRing = ring.Count;
            }
            Check("NaN SIM READING: a non-finite clock is REFUSED, so the ring stays bounded (43, not "
                  + "1,195) and the measured window stays the configured 360 s (not 8,955)",
                  maxRing == 43 && ring.Count == 42
                  && !ring.Any(e => double.IsNaN(e.Clock))
                  && Math.Abs((ring[^1].Clock - ring[0].Clock) - 360.0) < 1e-9);
            Check("(INVARIANT - UsingSimClock was already right; F2 was the service's INLINED copy of "
                  + "it, `simSeconds < 0.0`, disagreeing on NaN, so the product now calls these two) "
                  + "... NaN is no reading and the clock falls back to wall",
                  !StallPolicy.UsingSimClock(true, double.NaN)
                  && StallPolicy.SelectClock(true, double.NaN, 12345.0) == 12345.0);
        }

        // F3 - A ROLLBACK IS A CHANGE, NOT A NON-ADVANCE. The stale detector compared against a
        // HIGH-WATER mark, so after rollbackToSnapshot the clock was genuinely running below that
        // mark, the detector fired 60 wall s later, and judging was suspended for EVERY unit until
        // the clock climbed back - measured on 1805ee3: 45 / 420 / 945 wall s for rollbacks of
        // 100 / 475 / 1,000 sim s, under the text "the scenario is PAUSED, or the back end has
        // stopped answering". Neither was true and nothing in the log said rollback.
        {
            double last = double.NegativeInfinity, lastAdvanceWall = 0.0, sim = 0.0, wall = 0.0;
            bool warned = false;
            int warns = 0, heldChecks = 0, rollbacks = 0;
            for (int i = 1; i <= 600; i++)                   // 5 s cadence, ratio 1.0x
            {
                wall += 5.0; sim += 5.0;
                if (i == 300) sim -= 475.0;                  // DtVrfRemoteController::rollbackToSnapshot
                var step = StallPolicy.ClassifyClockStep(sim, last);
                if (step != StallPolicy.SimClockStep.Flat)
                {
                    if (step == StallPolicy.SimClockStep.RolledBack) rollbacks++;
                    last = sim; lastAdvanceWall = wall; warned = false;
                }
                else if (StallPolicy.SimClockStale(sim, last, wall, lastAdvanceWall,
                                                   StallPolicy.StaleClockWarnSeconds))
                {
                    heldChecks++;
                    if (!warned) { warned = true; warns++; }
                }
            }
            Check("ROLLBACK OF 475 SIM s: no STALE warning and not one check of suspended judging "
                  + "(1805ee3: one warning and 84 suspended checks), and the rollback is reported as "
                  + "itself - exactly once",
                  warns == 0 && heldChecks == 0 && rollbacks == 1);
            Check("(NEW API - ClassifyClockStep did not exist at 1805ee3, which inlined a high-water "
                  + "comparison; this is its truth table, not a discriminator) ... and the three steps "
                  + "are classified as they read, with the first reading against an unset mark counting "
                  + "as an advance",
                  StallPolicy.ClassifyClockStep(10.0, 5.0) == StallPolicy.SimClockStep.Advanced
                  && StallPolicy.ClassifyClockStep(5.0, 10.0) == StallPolicy.SimClockStep.RolledBack
                  && StallPolicy.ClassifyClockStep(5.0, 5.0) == StallPolicy.SimClockStep.Flat
                  && StallPolicy.ClassifyClockStep(0.0, double.NegativeInfinity)
                     == StallPolicy.SimClockStep.Advanced);
        }

        // F1 - A FLAT CLOCK MUST NOT SKEW THE OLDEST SAMPLE. 1805ee3 replaced the newest entry's
        // PAYLOAD under its old STAMP when the clock did not advance; a ring of ONE entry has that
        // entry at both ends, so the window was then measured from an old stamp against positions
        // read up to a whole flat-clock interval later - the false-positive direction. The
        // sequence is the RECAL doc's own tightest true negative: a unit crawling at 0.23 m/s of
        // SIM time, dispatched at wall 100 while the reader sits flat for 40 wall s at 6.21x (G5's
        // measured ratio). Measured on 1805ee3: TASKABRT at wall 160 on 35.7 m over a nominal
        // 360 sim s window whose TRUE displacement was 82.8 m. The 60 wall-second stale hold does
        // NOT cover this - the false verdict lands inside its grace.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, held = double.NaN;
            int verdicts = 0;
            for (double wall = 100.0; wall <= 1500.0; wall += 5.0)
            {
                double simTrue = 6.21 * wall;                 // the scenario really is running
                if (wall < 140.0 && double.IsNaN(held)) held = simTrue;
                double reading = (wall < 140.0) ? held : simTrue;   // 40 wall s of a flat reading
                double pos = 0.23 * simTrue;                  // 0.23 m per SIM second
                start = StallPolicy.Admit(ring, reading, pos, 360.0, start);
                var oldest = ring[0];
                if (StallPolicy.JudgeReady(reading, oldest.Clock, start, 360.0, 60.0, ring.Count,
                                           wall - 100.0, applyWallFloor: false)
                    && StallPolicy.Decide(new[] { Math.Abs(pos - oldest.P) }, 1, 50.0, 1).Stalled)
                    verdicts++;
            }
            Check("FLAT CLOCK, CRAWLING UNIT: a sample carrying no new clock information is DISCARDED, "
                  + "so a 40 wall-second flat reading at 6.21x can no longer manufacture a TASKABRT on "
                  + "the calibration's own tightest true negative", verdicts == 0);
        }

        // ... and discarding must not re-open the unbounded-ring hole the 1805ee3 back-prune closed:
        // 3,000 wall seconds of pause AFTER the ring has filled, on the 360 s sim window.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, sim = 0.0, pos = 0.0;
            int filled = 0, maxRing = 0, verdicts = 0;
            for (double wall = 5.0; wall <= 4000.0; wall += 5.0)
            {
                if (wall < 600.0 || wall >= 3600.0) { sim += 7.5; pos += 15.0; }   // 1.5x, 2 m/s
                start = StallPolicy.Admit(ring, sim, pos, 360.0, start);
                if (wall <= 600.0) filled = ring.Count;
                if (ring.Count > maxRing) maxRing = ring.Count;
                var oldest = ring[0];
                if (StallPolicy.JudgeReady(sim, oldest.Clock, start, 360.0, 60.0, ring.Count, wall,
                                           applyWallFloor: false)
                    && StallPolicy.Decide(new[] { Math.Abs(pos - oldest.P) }, 1, 50.0, 1).Stalled)
                    verdicts++;
            }
            Check("(INVARIANT - 1805ee3's back-REPLACE also held the ring at its filled size here and "
                  + "also produced no verdict; this guards the F1 fix against RE-OPENING the hole the "
                  + "replace closed, it does not discriminate against it) ... a 3,000 wall-second pause "
                  + "after the ring filled still leaves it at its filled size, with no verdict "
                  + "manufactured during the pause",
                  filled == 49 && maxRing <= filled + 1 && ring.Count <= filled + 1 && verdicts == 0);
        }

        // F4a - A CADENCE THE WINDOW CANNOT CARRY IS CLAMPED, NOT OBEYED. MinRingDepth entries span
        // MinRingDepth - 1 intervals, so any StallCheckSeconds above window / (MinRingDepth - 1)
        // leaves the front prune holding three entries forever and nothing is EVER judged -
        // silently, and on the DEFAULT wall path. Measured on 1805ee3 at the shipped 240 s wall
        // window: StallCheckSeconds 120 and 240 both went dormant with nothing in the log, where
        // 51d78a5 fired at 240 s in both cases.
        {
            Check("(NEW API - neither MaxCheckSeconds nor ClampCheckSeconds existed at 1805ee3, which "
                  + "held the configured cadence against nothing; this is their table, and the "
                  + "DISCRIMINATING case is the run below it) the cadence ceiling is window / "
                  + "(MinRingDepth - 1): 80 s on the 240 s wall window, 120 s on the 360 s sim window, "
                  + "and a sane cadence is left alone",
                  StallPolicy.ClampCheckSeconds(120, 240.0) == 80
                  && StallPolicy.ClampCheckSeconds(240, 240.0) == 80
                  && StallPolicy.ClampCheckSeconds(200, 360.0) == 120
                  && StallPolicy.ClampCheckSeconds(5, 240.0) == 5
                  && StallPolicy.ClampCheckSeconds(80, 240.0) == 80
                  && StallPolicy.ClampCheckSeconds(0, 240.0) == 1
                  && StallPolicy.MaxCheckSeconds(240.0) == 80 && StallPolicy.MaxCheckSeconds(360.0) == 120);
            int cadence = StallPolicy.ClampCheckSeconds(120, 240.0);
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, firstFire = -1.0;
            for (int i = 0; i < 400 && firstFire < 0.0; i++)
            {
                double wall = cadence * i;                   // WALL clock: the clock IS wall time
                start = StallPolicy.Admit(ring, wall, 0.0, 240.0, start);   // the unit is FROZEN
                var oldest = ring[0];
                if (StallPolicy.JudgeReady(wall, oldest.Clock, start, 240.0, 60.0, ring.Count, wall,
                                           applyWallFloor: true)
                    && StallPolicy.Decide(new[] { Math.Abs(0.0 - oldest.P) }, 1, 50.0, 1).Stalled)
                    firstFire = wall;
            }
            Check("... so Vrf:StallCheckSeconds=120 on the 240 s WALL window fires at 240 s - what "
                  + "51d78a5 did - instead of going dormant with nothing in the log",
                  Math.Abs(firstFire - 240.0) < 1e-9);
        }

        // F8 - IN SIM MODE THE WALL FLOOR MUST NOT SET THE DETECTION TIME. See JudgeReady: 1805ee3
        // ANDed the same 60 s onto both clocks as a WALL floor, and 360 sim s is ~58 wall s at
        // 6.21x, so above ratio ~6x the floor decided when the watchdog fires.
        {
            double cadence = StallPolicy.NextCheckSeconds(5.0, 360.0, 60.0);   // 1.5 s at 60x
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, wallAt = -1.0, simAt = -1.0;
            int firstSample = -1;
            for (int i = 1; i <= 200 && firstSample < 0; i++)
            {
                double wall = cadence * i, sim = 60.0 * wall;
                start = StallPolicy.Admit(ring, sim, 0.0, 360.0, start);
                if (StallPolicy.JudgeReady(sim, ring[0].Clock, start, 360.0, 60.0, ring.Count, wall,
                                           applyWallFloor: false))
                { firstSample = i; wallAt = wall; simAt = sim; }
            }
            Check("60x on the 360 s SIM window: the first verdict is set by the window and the ring depth "
                  + "(sample 5, wall 7.5 s, sim 450 s), not by the 60 wall-second floor (which put it at "
                  + "sample 40, wall 60 s, sim 3,600 s - ten times the calibrated window)",
                  Math.Abs(cadence - 1.5) < 1e-9 && firstSample == 5
                  && Math.Abs(wallAt - 7.5) < 1e-9 && Math.Abs(simAt - 450.0) < 1e-9
                  && ring.Count >= StallPolicy.MinRingDepth);
            Check("(INVARIANT - identical under 1805ee3, which ANDed the floor on BOTH clocks; this is "
                  + "the guard that F8 narrowed the floor to the sim path only and left the shipped wall "
                  + "path alone) ... the WALL path keeps that floor exactly as 51d78a5 had it: a frozen "
                  + "unit is not judged 59 wall s after dispatch, and is 60 s after",
                  !StallPolicy.JudgeReady(300.0, 0.0, 0.0, 240.0, 60.0, 10, 59.0, applyWallFloor: true)
                  && StallPolicy.JudgeReady(300.0, 0.0, 0.0, 240.0, 60.0, 10, 60.0, applyWallFloor: true));
        }

        // =====================================================================================
        // PASS-3 COLD-START REVIEW OF 08146a2 (P1, P4). P1 is a defect this branch INTRODUCED: the
        // F2 fix taught Admit to refuse a non-finite clock, but left the caller's gate at
        // "simSeconds >= 0.0", which is TRUE for +Infinity. P4 is the dormancy line armed on one
        // cause instead of on the condition. Both blocks FAIL against the logic they replace.
        // =====================================================================================

        // P1(a) - THE PREDICATE. +Infinity is not a reading, and SelectClock must route it to wall.
        Check("+INFINITY IS NOT A CLOCK: UsingSimClock refuses it (08146a2: `simSeconds >= 0.0` is "
              + "TRUE for +Inf, the one non-finite value that reached Admit), and SelectClock - which "
              + "now routes through that one predicate - falls back to wall seconds",
              !StallPolicy.UsingSimClock(true, double.PositiveInfinity)
              && StallPolicy.SelectClock(true, double.PositiveInfinity, 4242.0) == 4242.0
              && !StallPolicy.UsingSimClock(true, double.NegativeInfinity)
              && !StallPolicy.UsingSimClock(true, double.NaN)
              && StallPolicy.UsingSimClock(true, 0.0)            // t = 0 IS a reading
              && !StallPolicy.UsingSimClock(true, -1.0)          // the reader's "no reading"
              && !StallPolicy.UsingSimClock(false, 123.0));

        // P1(b) - THE CRASH POINT. A unit's FIRST check with the reader at +Infinity: 08146a2 let
        // the value through, Admit refused it (correctly), and the service's very next statement -
        // `var oldest = ring[0];` - indexed a zero-length list. MaybeCheckStalls is called bare from
        // TickLoop and Program.cs installs no AppDomain.UnhandledException handler, so that throw
        // terminates the interface process. Belt: the predicate above. Braces: the caller's
        // ring.Count guard, modelled here as the service now has it.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, wall = 5000.0;
            bool threw = false, judged = false, ringEmptyAfterAdmit = false;
            try
            {
                double clockNow = StallPolicy.SelectClock(true, double.PositiveInfinity, wall);
                start = StallPolicy.Admit(ring, clockNow, 1.0, 360.0, start);
                ringEmptyAfterAdmit = ring.Count == 0;
                if (ring.Count == 0) _ = ring[0];       // 08146a2's unconditional index - it threw here
                else
                {
                    var oldest = ring[0];
                    judged = StallPolicy.JudgeReady(clockNow, oldest.Clock, start, 360.0, 60.0,
                                                    ring.Count, 9999.0, applyWallFloor: false);
                }
            }
            catch (Exception) { threw = true; }
            Check("+INFINITY ON AN EMPTY RING: the tick does not throw and judges nothing - the sample "
                  + "is admitted on the WALL clock instead, so the ring is never left empty for the "
                  + "caller to index (08146a2: IndexOutOfRangeException on the vrf-tick thread, which "
                  + "takes the process with it)",
                  !threw && !judged && !ringEmptyAfterAdmit
                  && ring.Count == 1 && Math.Abs(ring[0].Clock - wall) < 1e-9);
        }

        // P1(c) - THE FALSE TASKABRT. Same feed as F1 above - the RECAL doc's own tightest true
        // negative, a unit crawling at 0.23 m/s of SIM time - at 1.5x, with the reader returning
        // +Infinity at wall 20, 40 and 60. Against +Inf BOTH window terms are trivially true, so
        // 08146a2 opened the gate on whatever depth existed and reported 6.9 / 13.8 / 20.7 m "in the
        // last 360 SIM s" on 30-90 sim s of history - a TASKABRT to STP on a healthy unit, on the
        // same tick, three times.
        {
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN;
            int verdicts = 0, refusedTicks = 0;
            for (double wall = 0.0; wall <= 200.0; wall += 5.0)
            {
                double sim = 1.5 * wall;                      // the scenario really is running
                double pos = 0.23 * sim;                      // 0.23 m per SIM second
                double reading = (wall == 20.0 || wall == 40.0 || wall == 60.0)
                               ? double.PositiveInfinity : sim;
                if (!StallPolicy.UsingSimClock(true, reading)) { refusedTicks++; continue; }
                double clockNow = StallPolicy.SelectClock(true, reading, wall);
                start = StallPolicy.Admit(ring, clockNow, pos, 360.0, start);
                if (ring.Count == 0) continue;                // the caller's guard
                var oldest = ring[0];
                if (StallPolicy.JudgeReady(clockNow, oldest.Clock, start, 360.0, 60.0, ring.Count,
                                           wall, applyWallFloor: false)
                    && StallPolicy.Decide(new[] { Math.Abs(pos - oldest.P) }, 1, 50.0, 1).Stalled)
                    verdicts++;
            }
            Check("+INFINITY, CRAWLING UNIT: a +Inf reading at wall 20/40/60 produces NO verdict - the "
                  + "tick is simply not taken on the sim clock (08146a2: three TASKABRTs, one per "
                  + "poisoned tick, on 6.9 / 13.8 / 20.7 m over a NOMINAL 360 sim s window)",
                  verdicts == 0 && refusedTicks == 3 && ring.Count >= StallPolicy.MinRingDepth);
        }

        // P4 - MODE THRASH IS DORMANCY TOO, AND IT MUST BE SAID. The reviewer's A8b feed: the sim
        // reader answers on 7 of every 10 checks and is OUT for the other 3 CONSECUTIVE ones, at
        // 1.5x, with a FROZEN unit, for 3,000 wall seconds. Every third consecutive miss flips the
        // clock mode for real; every flip drops every ring and swaps the window; no ring ever spans
        // a window and nothing is ever judged. 08146a2 armed its dormancy line on the CADENCE FLOOR
        // (appliedCadence <= 1 s AND deepest ring < MinRingDepth) and at 1.5x the cadence never
        // leaves 5 s - so that condition is true on ZERO checks here and the operator saw 0 verdicts,
        // a stream of mode lines, and nothing saying no unit was being watched.
        {
            int mode = 0, cand = 0, streak = 0, modeChanges = 0;
            var ring = new List<(double Clock, double P)>();
            double start = double.NaN, sim = 0.0, lastCheckClock = double.NaN;
            double unjudged = 0.0, judgeableAt = 0.0;
            int dormantChecks = 0, cadenceFloorChecks = 0, verdicts = 0, judgeableChecks = 0;
            for (int i = 0; i < 600; i++)                      // 600 checks x 5 wall s = 3,000 wall s
            {
                double wall = 5000.0 + 5.0 * i;
                sim += 7.5;                                    // 1.5x throughout - the sim never stops
                double reading = (i % 10) >= 7 ? -1.0 : sim;   // 3 CONSECUTIVE misses in every 10
                bool simReadable = StallPolicy.UsingSimClock(true, reading);
                int held = mode;
                (mode, cand, streak) = StallPolicy.NextClockMode(mode, cand, streak,
                                                                simReadable ? 1 : 2,
                                                                StallPolicy.ModeSwitchConfirmations);
                if (mode != held)
                {
                    if (held != 0) { ring.Clear(); start = double.NaN; lastCheckClock = double.NaN; }
                    modeChanges++;
                }
                bool usingSim = mode == 1;
                double window = StallPolicy.ResolveWindowSeconds(0, usingSim);
                if (usingSim && !simReadable) continue;        // the service's sim-blackout return
                double clockNow = StallPolicy.SelectClock(usingSim, reading, wall);
                double advance = (!double.IsNaN(lastCheckClock) && clockNow > lastCheckClock)
                               ? clockNow - lastCheckClock : 0.0;   // never across a mode change
                lastCheckClock = clockNow;
                start = StallPolicy.Admit(ring, clockNow, 0.0, window, start);   // the unit is FROZEN
                if (ring.Count == 0) continue;
                bool judgeable = StallPolicy.JudgeReady(clockNow, ring[0].Clock, start, window, 60.0,
                                                        ring.Count, wall - 5000.0,
                                                        applyWallFloor: !usingSim);
                if (judgeable)
                {
                    judgeableChecks++;
                    if (StallPolicy.Decide(new[] { 0.0 }, 1, 50.0, 1).Stalled) verdicts++;
                }
                unjudged += advance;
                if (judgeable) judgeableAt = unjudged;
                else if (StallPolicy.DormancyDue(judgeableAt, unjudged, window, samplingActive: true))
                    dormantChecks++;
                // 08146a2's arming condition, evaluated on the same feed, for the record.
                if (StallPolicy.NextCheckSeconds(5.0, window, 1.5) <= 1.0
                    && ring.Count < StallPolicy.MinRingDepth) cadenceFloorChecks++;
            }
            Check("MODE THRASH GOES DORMANT, AND NOW SAYS SO: a reader out for 3 CONSECUTIVE checks in "
                  + "every 10 at 1.5x flips the clock mode for real, every flip drops every ring, and a "
                  + "FROZEN unit is never judged once in 3,000 wall s - the dormancy watch fires on the "
                  + "OBSERVABLE condition (nothing judgeable for " + StallPolicy.DormancyWindows
                  + " whole windows while sampling), where 08146a2's cadence-floor arming condition is "
                  + "true on NO check here and printed nothing",
                  verdicts == 0 && judgeableChecks == 0 && modeChanges > 1
                  && dormantChecks > 0 && cadenceFloorChecks == 0);
        }

        Console.WriteLine(fails == 0 ? "stall-selftest: ALL CHECKS PASSED" : $"stall-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
