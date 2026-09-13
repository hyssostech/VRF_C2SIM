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

        Console.WriteLine(fails == 0 ? "stall-selftest: ALL CHECKS PASSED" : $"stall-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
