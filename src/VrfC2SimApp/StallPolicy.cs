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
    /// The window's time base, in seconds. The sim clock is used only when it was ASKED FOR
    /// (Vrf:StallClock = "sim") AND actually READ (>= 0); every other case is wall seconds, so a
    /// build that cannot see the sim clock behaves exactly as the wall-clock watchdog did.
    /// A sim reading of exactly 0.0 is a reading (a scenario at t = 0), not a failure.
    /// </summary>
    public static double SelectClock(bool preferSim, double simSeconds, double wallSeconds)
        => (preferSim && simSeconds >= 0.0) ? simSeconds : wallSeconds;

    /// <summary>True when SelectClock would take the sim reading. Drives the one log line.</summary>
    public static bool UsingSimClock(bool preferSim, double simSeconds)
        => preferSim && simSeconds >= 0.0;

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
    /// rises; above ratio windowSeconds / (MinRingDepth x 1 s) - 90x at the 360 s sim window - the
    /// 1 s cadence floor binds and the watchdog cannot judge at all. Documented, not silent.
    /// </summary>
    public const int MinRingDepth = 4;

    /// <summary>Consecutive readings of a new clock mode before the watchdog switches (finding 8).</summary>
    public const int ModeSwitchConfirmations = 3;

    /// <summary>Wall seconds of a motionless sim clock that earn the stale-clock warning (finding 9).</summary>
    public const double StaleClockWarnSeconds = 60.0;

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
    ///   ROLLBACK (clockNow is BEFORE the newest stamp - DtVrfRemoteController::rollbackToSnapshot,
    ///     vrfRemoteController.h:605): every entry stamped after clockNow belongs to an abandoned
    ///     timeline. Drop them all and re-anchor the grace (startClock = clockNow): a rollback is
    ///     a NEW watch, not a continuation of the old one.
    ///   BACK (clockNow EQUALS the newest stamp - a paused scenario, or a reader whose back-end
    ///     status period is coarser than the check cadence): the sample carries no new clock
    ///     information, so it REPLACES its predecessor instead of being appended. That bounds the
    ///     ring at one entry over its filled size for a pause of any length, and keeps the NEWEST
    ///     positions, which is what the verdict has to be computed from.
    ///   FRONT (rule (a), unchanged): keep exactly one sample at or before the window edge.
    /// </summary>
    public static double Admit<T>(List<(double Clock, T P)> ring, double clockNow, T sample,
                                  double windowSeconds, double startClock)
    {
        if (ring is null) return startClock;
        if (ring.Count > 0 && clockNow < ring[^1].Clock)
        {
            while (ring.Count > 0 && ring[^1].Clock > clockNow) ring.RemoveAt(ring.Count - 1);
            startClock = clockNow;
        }
        if (double.IsNaN(startClock)) startClock = clockNow;
        if (ring.Count > 0 && clockNow <= ring[^1].Clock) ring[^1] = (clockNow, sample);
        else ring.Add((clockNow, sample));
        while (ring.Count >= 2 && ShouldDropOldest(clockNow, ring[0].Clock, ring[1].Clock, windowSeconds))
            ring.RemoveAt(0);
        return startClock;
    }

    /// <summary>
    /// THE FULL GATE the product judges on. WindowReady measures the window on the SELECTED clock;
    /// two floors the clock window cannot supply are ANDed onto it (review finding 4):
    ///   RING DEPTH - see MinRingDepth. A verdict on two position reads is not a measurement.
    ///   WALL FLOOR - 51d78a5 gated on wall seconds since the in-flight record's own DispatchedUtc
    ///     and 1616614 dropped that floor entirely when it moved the anchor to the watch's first
    ///     sample. The same StallMinSecondsSinceDispatch is kept here as a WALL floor, so however
    ///     fast the sim clock runs no task is ever judged inside its first minute of real time.
    /// </summary>
    public static bool JudgeReady(double clockNow, double oldestClock, double startClock,
                                  double windowSeconds, double minSecondsSinceStart,
                                  int ringDepth, double wallSecondsSinceDispatch)
        => ringDepth >= MinRingDepth
        && wallSecondsSinceDispatch >= minSecondsSinceStart
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
    /// ERROR and resolves to WALL, the calibrated mode (review findings 5 and 7: the 240 s window
    /// was derived in wall seconds, so a typo must never silently select the un-derived one).
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
                if (StallPolicy.JudgeReady(clock, old.Clock, start, 240.0, 60.0, ring.Count, wall)
                    && StallPolicy.Decide(new[] { Math.Abs(pos - old.P) }, 1, 50.0, 1).Stalled)
                    verdicts++;
            }
            Check("PAUSE AFTER THE RING FILLED: 1,000 wall s of samples at a frozen clock leave the "
                  + "ring at its filled size, not 200 entries longer",
                  filled >= 40 && maxDuringPause <= filled + 1 && ring.Count <= filled + 1);
            Check("... and the ring still holds the whole pre-pause window, so nothing is manufactured "
                  + "during the pause", verdicts == 0 && (clock - ring[0].Clock) >= 240.0);
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
                if (StallPolicy.JudgeReady(clock, old.Clock, start, 240.0, 60.0, ring.Count, wall)
                    && StallPolicy.Decide(new[] { Math.Abs(pos - old.P) }, 1, 50.0, 1).Stalled)
                { verdicts++; oldestAtFirstVerdict = old.Clock; }
            }
            Check("... so the first verdict after the rollback is computed from a sample stamped AFTER "
                  + "it, never across the discontinuity",
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
            bool readyBefore = StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count, 9999.0);
            ring.Clear(); start = double.NaN;               // <- MarkDispatched: a new move task
            start = StallPolicy.Admit(ring, clock, 0.0, 240.0, start);
            bool readyAfter = StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count, 9999.0);
            Check("FRESH DISPATCH re-arms the watch: StartClock becomes the clock at the first sample "
                  + "after the clear and the previous task's window can no longer be judged",
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
                    StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count, wall))
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
            for (int i = 1; i <= 120 && firstJudgeable < 0; i++)
            {
                clock += 60.0; wall += cadence;             // 60x sampled at the adapted cadence
                start = StallPolicy.Admit(ring, clock, 0.0, 240.0, start);
                if (StallPolicy.JudgeReady(clock, ring[0].Clock, start, 240.0, 60.0, ring.Count, wall))
                    firstJudgeable = i;
            }
            Check("... and at that cadence the FIRST judgeable check is the 60 wall-second floor, on a "
                  + "ring 5 deep - not sample 2 at 10 wall s",
                  firstJudgeable == 60 && ring.Count >= StallPolicy.MinRingDepth);
        }

        // FINDING 7 - Vrf:StallClock is validated; a typo must not select the un-calibrated mode.
        Check("StallClock accepts exactly \"sim\", trimmed and case-insensitive",
              StallPolicy.ParseClockPreference("  SiM ", out bool okSim) && okSim);
        Check("StallClock accepts exactly \"wall\", trimmed and case-insensitive",
              !StallPolicy.ParseClockPreference("WALL ", out bool okWall) && okWall);
        Check("every other value is a CONFIGURATION ERROR and falls back to WALL, the calibrated mode",
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

        Console.WriteLine(fails == 0 ? "stall-selftest: ALL CHECKS PASSED" : $"stall-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
