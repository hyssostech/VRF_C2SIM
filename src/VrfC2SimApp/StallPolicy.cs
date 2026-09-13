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

        Console.WriteLine(fails == 0 ? "stall-selftest: ALL CHECKS PASSED" : $"stall-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
