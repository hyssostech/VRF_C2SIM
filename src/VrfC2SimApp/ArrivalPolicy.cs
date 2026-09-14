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
        // with fraction 0.5 and 6 members, 4 within arrive, 3 do not. fraction >= 1.0 means ALL
        // members (a strictly-greater test could never hold there - review wf_62e5bdf7).
        bool arrived = fraction >= 1.0 ? within >= totalMembers : within > fraction * totalMembers;
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
        d = ArrivalPolicy.Decide(new double[] { 100, 100, 100, 100 }, 4, 500, 1.0);
        Check("fraction 1.0 (ALL) with 4 of 4 -> arrived", d.Arrived);
        d = ArrivalPolicy.Decide(new double[] { 100, 100, 100, 900 }, 4, 500, 1.0);
        Check("fraction 1.0 (ALL) with 3 of 4 -> NOT arrived", !d.Arrived);
        // ---- C16 SAMPLER REGRESSION GUARD (cold-start review D1, 2026-09-13) -----------------
        // VrfFacade::collectMembers recurses to depth 3 WITHOUT de-duplicating, so a member
        // published under two sub-aggregates appears TWICE in the member list. Three samplers over
        // the SAME list, all feeding this same Decide:
        //   MAIN   (pre-C16) distances is a LIST - the duplicate contributes TWO distances - and
        //          total = members.Count - the duplicate counts TWICE. Weighted consistently on
        //          both sides of the fraction.
        //   BROKEN (C16 as first written) the sample is a DICTIONARY keyed by uuid (ONE distance)
        //          but total is still members.Count (TWO): the duplicate weighs against arrival
        //          while contributing one position - strictly HARDER than main. That is the bug.
        //   FIXED  (this build) dictionary sample AND total = DISTINCT uuids, with an EMPTY-uuid
        //          member still counted against arrival exactly as main did: one physical vehicle
        //          counted once on both sides; an unreadable one keeps its vote.
        static (List<double> D, int Total) SampleMain((string Uuid, double Dist)[] ms)
        {
            var d = new List<double>();
            foreach (var m in ms) if (!string.IsNullOrEmpty(m.Uuid)) d.Add(m.Dist);
            return (d, ms.Length);
        }
        static (List<double> D, int Total) SampleBroken((string Uuid, double Dist)[] ms)
        {
            var p = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var m in ms) if (!string.IsNullOrEmpty(m.Uuid)) p[m.Uuid] = m.Dist;
            return (p.Values.ToList(), ms.Length);
        }
        static (List<double> D, int Total) SampleFixed((string Uuid, double Dist)[] ms)
        {
            var p = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var m in ms) if (!string.IsNullOrEmpty(m.Uuid)) p[m.Uuid] = m.Dist;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int total = ms.Count(m => string.IsNullOrEmpty(m.Uuid) || seen.Add(m.Uuid));
            return (p.Values.ToList(), total);
        }
        static bool Arrives((List<double> D, int Total) s) => ArrivalPolicy.Decide(s.D, s.Total, 500, 0.5).Arrived;

        // (a) the real case - the duplicate is a member that HAS arrived. main 4 of 6 -> arrived;
        //     fixed 3 of 5 -> arrived (SAME); broken 3 of 6 -> NOT arrived (the regression).
        var dupNear = new[] { ("A", 10.0), ("A", 10.0), ("B", 20.0), ("C", 30.0), ("D", 30000.0), ("E", 30000.0) };
        var mMain = SampleMain(dupNear); var mBroken = SampleBroken(dupNear); var mFixed = SampleFixed(dupNear);
        Check($"duplicated ARRIVED member: FIXED ({mFixed.D.Count} dist/{mFixed.Total} total) decides as MAIN " +
              $"({mMain.D.Count}/{mMain.Total}) - both arrive",
              Arrives(mFixed) == Arrives(mMain) && Arrives(mFixed));
        Check($"... and the UN-deduplicated total ({mBroken.D.Count}/{mBroken.Total}) would NOT arrive - " +
              "the D1 regression is real", !Arrives(mBroken));

        // (b) the documented DIVERGENCE from main: the duplicate is a member that has NOT arrived.
        //     main double-counted one vehicle against arrival (3 of 6 -> no); fixed counts it once
        //     (3 of 5 -> yes). Intended: there are five vehicles, three of them are there.
        var dupFar = new[] { ("A", 30000.0), ("A", 30000.0), ("B", 10.0), ("C", 10.0), ("D", 10.0), ("E", 30000.0) };
        Check("duplicated ABSENT member: FIXED arrives (3 of 5 real vehicles), MAIN did not (3 of 6 " +
              "counted) - a DELIBERATE divergence: one vehicle is one vote",
              Arrives(SampleFixed(dupFar)) && !Arrives(SampleMain(dupFar)));

        // (c) a member whose uuid is EMPTY cannot be sampled or de-duplicated, but it is still a
        //     vehicle: it counts against arrival exactly as on main (supervisor ruling 2026-09-13,
        //     'unreadable members must still count'). No such member has ever been observed.
        var withEmpty = new[] { ("A", 10.0), ("B", 10.0), ("", 30000.0), ("C", 30000.0) };
        Check("EMPTY-uuid member: FIXED counts it against arrival as MAIN does (2 of 4 -> not " +
              "arrived on both) - no divergence",
              Arrives(SampleFixed(withEmpty)) == Arrives(SampleMain(withEmpty)) && !Arrives(SampleFixed(withEmpty)));

        // (d) the property that matters: over EVERY near/far arrangement of five members with one
        //     duplicated, the fixed sampler is never STRICTER than main - and the broken one is.
        bool fixedNeverStricter = true, brokenStricterSomewhere = false;
        for (int mask = 0; mask < 32; mask++)
        {
            var ms = new List<(string, double)>();
            const string ids = "ABCDE";
            for (int i = 0; i < 5; i++)
            {
                double dist = (mask & (1 << i)) != 0 ? 10.0 : 30000.0;
                ms.Add((ids[i].ToString(), dist));
                if (i == 0) ms.Add((ids[i].ToString(), dist));   // A is published under two sub-aggregates
            }
            var arr = ms.ToArray();
            bool am = Arrives(SampleMain(arr));
            if (am && !Arrives(SampleFixed(arr))) fixedNeverStricter = false;
            if (am && !Arrives(SampleBroken(arr))) brokenStricterSomewhere = true;
        }
        Check("over all 32 near/far arrangements of a duplicated member the FIXED sampler is NEVER " +
              "stricter than main", fixedNeverStricter);
        Check("... and the BROKEN one is stricter somewhere (so the guard above has teeth)",
              brokenStricterSomewhere);
        Console.WriteLine(fails == 0 ? "arrival-selftest: ALL CHECKS PASSED" : $"arrival-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
