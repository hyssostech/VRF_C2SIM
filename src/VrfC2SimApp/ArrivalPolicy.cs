using System;
using System.Collections.Generic;
using System.Linq;

namespace VrfC2SimApp;

/// <summary>
/// ARRIVAL-EVIDENCE COMPLETION (user ruling 2026-09-07: "report a unit's completion from the
/// unit's own arrival evidence is fine"). Pure decision, no bridge, testable offline
/// (--arrival-selftest).
///
/// Why: on 5.2 a unit's Move Along Route holds its vendor completion until EVERY member reports
/// arrival ("Subs still moving: N" on the unit's console); in the spaced COA-STP1 run of
/// 2026-09-07 seven of nine units stood at the ends of their 24-33 km legs for eight sim-hours
/// while one member each oscillated in place kilometres behind (an M3, an M577), and one
/// unit's leader drove the whole leg alone while its followers never left. STP sequences the
/// plan on TASKCMPLT, so the interface reports completion from the unit's OWN evidence:
///
///   the task is complete when MORE THAN a configured fraction of the unit's members (or the
///   entity itself for a platform) are within ArrivalRadiusMeters of the task's destination.
///
/// A majority rule, not a leader rule: the 1-6/2/1_AD case (leader at the end, five followers
/// 30 km back) must NOT report complete. The radius is sized for a unit in formation at the
/// route end: the shipped Armor-Co formations span up to 630 m, members sit up to +/-430 m
/// from the unit's last vertex, so the default is 500 m (an entity's own vendor tolerance is
/// 1-15 m, far inside it).
/// </summary>
public static class ArrivalPolicy
{
    public readonly record struct Decision(bool Arrived, int Within, int Total, double NearestMeters);

    /// <summary>
    /// distances = each member's (or the lone entity's) distance to the destination in meters;
    /// members without a readable position are NOT in the list (they count as absent, which
    /// is conservative: they neither help nor block by themselves - the fraction is of the
    /// members that COULD be read, but Total below reports all members for the log).
    /// </summary>
    public static Decision Decide(IReadOnlyList<double> distances, int totalMembers, double radiusMeters, double fraction)
    {
        if (distances == null || distances.Count == 0 || totalMembers <= 0)
            return new Decision(false, 0, Math.Max(0, totalMembers), double.NaN);
        int within = distances.Count(d => d <= radiusMeters);
        double nearest = distances.Min();
        // "more than the fraction" of ALL members (unreadable members count against arrival):
        // with fraction 0.5 and 6 members, 4 within arrive, 3 do not.
        bool arrived = within > fraction * totalMembers;
        return new Decision(arrived, within, totalMembers, nearest);
    }
}

public static class ArrivalSelfTest
{
    public static int Run()
    {
        int fails = 0;
        void Check(string what, bool cond) { Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what); if (!cond) fails++; }
        var d = ArrivalPolicy.Decide(new double[] { 10, 20, 30, 40, 50, 7000 }, 6, 500, 0.5);
        Check("5 of 6 within 500 m (one M3 7 km back) -> arrived", d.Arrived && d.Within == 5);
        d = ArrivalPolicy.Decide(new double[] { 10, 30000, 30100, 29900, 30050, 30200 }, 6, 500, 0.5);
        Check("leader alone at the end, five followers 30 km back -> NOT arrived", !d.Arrived && d.Within == 1);
        d = ArrivalPolicy.Decide(new double[] { 12 }, 1, 500, 0.5);
        Check("a lone entity within radius -> arrived", d.Arrived);
        d = ArrivalPolicy.Decide(new double[] { 10, 20, 30 }, 6, 500, 0.5);
        Check("3 of 6 readable and within, 3 unreadable -> NOT arrived (strictly more than half of ALL)", !d.Arrived);
        d = ArrivalPolicy.Decide(new double[] { 10, 20, 30, 40 }, 6, 500, 0.5);
        Check("4 of 6 within -> arrived", d.Arrived);
        d = ArrivalPolicy.Decide(new double[] { 501, 600, 700, 800, 900, 1000 }, 6, 500, 0.5);
        Check("all just outside the radius -> NOT arrived, nearest reported", !d.Arrived && Math.Abs(d.NearestMeters - 501) < 1e-9);
        d = ArrivalPolicy.Decide(Array.Empty<double>(), 6, 500, 0.5);
        Check("no readable member -> NOT arrived", !d.Arrived);
        d = ArrivalPolicy.Decide(new double[] { 100, 100, 100, 100 }, 4, 500, 0.75);
        Check("fraction 0.75 with 4 of 4 -> arrived (4 > 3)", d.Arrived);
        d = ArrivalPolicy.Decide(new double[] { 100, 100, 100, 900 }, 4, 500, 0.75);
        Check("fraction 0.75 with 3 of 4 -> NOT arrived (3 is not > 3)", !d.Arrived);
        Console.WriteLine(fails == 0 ? "arrival-selftest: ALL CHECKS PASSED" : $"arrival-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
