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
///
/// *** STP-837 (2026-09-15): PROXIMITY IS NOT ARRIVAL. TRAVERSAL IS. ***
/// The rule above asks only WHERE the members are, never whether they went anywhere - and on
/// run V6g (20260915T171152Z, V6_LIVE_JOIN_GATE sec 13) that closed two of three tasks EARLY,
/// 116 s and 516 s before their (swallowed) vendor completions. T_R5_PL1 is the proof and it is
/// degenerate BY CONSTRUCTION: the order mirrors the platoon's LAST vertex onto its own start,
/// so "4/4 within 500 m of the last vertex (nearest 26 m)" held from the first check while
/// M1A2 1 had moved 20 m and M1A2 4 had moved 133 m - only two of the four ever drove the
/// 578 m leg. A 500 m radius against a 578 m leg is a false-green surface, and it closed the
/// observation window at t+68 s of 720.
///
/// THE RULE IS NOW, for a member to be counted:
///   (i)  it is within the ARRIVAL RADIUS of the last vertex, where that radius is
///        min(Vrf:ArrivalRadiusMeters, 0.25 x the route's authored length) - a tolerance may
///        never be a quarter of the journey; and
///   (ii) it has MOVED at least max(0.5 x the route's authored length,
///        Vrf:ArrivalMinTravelMeters) from ITS OWN position at dispatch.
/// And a route whose LAST VERTEX lies within the arrival radius of the taskee's dispatch
/// position is NOT CLOSABLE BY ARRIVAL EVIDENCE AT ALL - standing at the end of an out-and-back
/// is indistinguishable from never having left, so that task waits for the vendor's own
/// completion or for its C2SIM Duration (R4). That is the ONLY case where this feature now
/// gives up a completion it used to report, and giving it up is the point.
///
/// WHAT IT COSTS A LEGITIMATE LONG MOVE: nothing. On a straight 1,155 m route a member within
/// 289 m of the last vertex has necessarily covered at least 866 m, well past the 578 m the
/// traversal test asks for - the test can only bite where proximity was never evidence.
/// </summary>
public static class ArrivalPolicy
{
    /// <param name="RadiusMeters">The EFFECTIVE radius this decision used - the configured one
    /// shrunk to a quarter of the route (STP-837). The log prints this, not the setting.</param>
    /// <param name="RequiredTravelMeters">The traversal each counted member had to show.</param>
    /// <param name="FarthestTravelMeters">The best travel any sampled member managed, for the
    /// log: it is what tells an operator whether the unit is short of the bar or short of the
    /// destination.</param>
    public readonly record struct Decision(bool Arrived, int Within, int Total, double NearestMeters,
                                           double RadiusMeters = double.NaN,
                                           double RequiredTravelMeters = double.NaN,
                                           double FarthestTravelMeters = double.NaN);

    /// <summary>One member's two measurements against the task: how far it is from the route's
    /// last vertex, and how far it has come from where it stood when the task was dispatched.
    /// A member whose dispatch position is unknown carries NaN travel and is therefore NEVER
    /// counted - conservative by choice: the failure this rule exists to stop is closing early.</summary>
    public readonly record struct MemberSample(double DistanceToLastVertexMeters, double TravelledMeters);

    /// <summary>
    /// THE ARRIVAL RADIUS ACTUALLY USED: min(configured, 0.25 x route length). An unknown or
    /// zero-length route (NaN / &lt;= 0) leaves the configured radius standing - this must
    /// degrade to the old tolerance, never to a tighter one nobody asked for.
    /// </summary>
    public static double RadiusFor(double configuredRadiusMeters, double routeLengthMeters)
        => double.IsNaN(routeLengthMeters) || routeLengthMeters <= 0.0
           ? configuredRadiusMeters
           : Math.Min(configuredRadiusMeters, 0.25 * routeLengthMeters);

    /// <summary>
    /// THE TRAVERSAL EACH COUNTED MEMBER MUST SHOW: max(0.5 x route length, the configured
    /// floor). The floor is what covers a short move, where half the route is less than the
    /// noise a stationary vehicle's published position can wander through.
    /// </summary>
    public static double RequiredTravelFor(double routeLengthMeters, double minTravelMeters)
        => double.IsNaN(routeLengthMeters) || routeLengthMeters <= 0.0
           ? minTravelMeters
           : Math.Max(0.5 * routeLengthMeters, minTravelMeters);

    /// <summary>
    /// CAN THIS TASK BE CLOSED FROM ARRIVAL EVIDENCE AT ALL? Not when its last vertex sits
    /// inside the arrival radius of the position the taskee was dispatched from: the evidence
    /// would be satisfied by a unit that never moved (V6g's T_R5_PL1). An UNKNOWN separation
    /// (NaN) keeps the task closable - this rule refuses evidence it can disprove, it does not
    /// refuse on ignorance.
    /// </summary>
    public static bool ClosableByArrival(double lastVertexFromDispatchMeters, double radiusMeters)
        => double.IsNaN(lastVertexFromDispatchMeters) || lastVertexFromDispatchMeters > radiusMeters;

    /// <summary>
    /// THE DECISION (STP-837). A member counts only if it is BOTH inside the effective radius
    /// and past the required travel; the majority rule over <paramref name="totalMembers"/> is
    /// unchanged, so every property the old rule had against a straggler still holds.
    /// The caller must have asked <see cref="ClosableByArrival"/> first - this function cannot
    /// see the dispatch position and will happily count members on a degenerate route.
    /// </summary>
    public static Decision DecideWithTraversal(IReadOnlyList<MemberSample> samples, int totalMembers,
                                               double configuredRadiusMeters, double fraction,
                                               double routeLengthMeters, double minTravelMeters)
    {
        double radius = RadiusFor(configuredRadiusMeters, routeLengthMeters);
        double required = RequiredTravelFor(routeLengthMeters, minTravelMeters);
        if (samples == null || samples.Count == 0 || totalMembers <= 0)
            return new Decision(false, 0, Math.Max(0, totalMembers), double.NaN,
                                radius, required, double.NaN);
        int within = 0;
        double nearest = double.NaN, farthest = double.NaN;
        foreach (var s in samples)
        {
            if (double.IsNaN(nearest) || s.DistanceToLastVertexMeters < nearest)
                nearest = s.DistanceToLastVertexMeters;
            if (!double.IsNaN(s.TravelledMeters) && (double.IsNaN(farthest) || s.TravelledMeters > farthest))
                farthest = s.TravelledMeters;
            // NaN fails both comparisons, which is the intended answer for an unknown baseline.
            if (s.DistanceToLastVertexMeters <= radius && s.TravelledMeters >= required) within++;
        }
        bool arrived = fraction >= 1.0 ? within >= totalMembers : within > fraction * totalMembers;
        return new Decision(arrived, within, totalMembers, nearest, radius, required, farthest);
    }

    /// <summary>
    /// *** THE SUPERSEDED PROXIMITY-ONLY RULE (pre-STP-837). NOT CALLED BY THE SERVICE. ***
    /// It is kept, and kept exercised, as the FAIL-FIRST ARM of --arrival-selftest: the V6g
    /// platoon case must close here - wrongly - and must not close under DecideWithTraversal.
    /// A rule that is only described as wrong is not tested; this one is run.
    ///
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
        // ================= STP-837: TRAVERSAL, NOT PROXIMITY ==================================
        // THE FIXTURES ARE RUN V6g (20260915T171152Z), measured from its own order and init:
        //   T_R5_PL1 1222.MechPlt  start 34.612955587412,-116.600486942341; vertices
        //       ...,-116.594173672085 and ...,-116.600487000000. Legs 577.75 + 577.76 m =
        //       1,155.51 m, and the LAST VERTEX IS 0.0053 m FROM THE START (mirrored onto it).
        //       The run reported "4/4 within 500 m of the last vertex (nearest 26 m)" at sim
        //       ~256 - 516 s before the vendor's own completion - while M1A2 1 had moved 20 m
        //       and M1A2 4 had moved 133 m.
        //   T_R5_TK1 1.BdeHQ  start ...,-116.712685404877 -> ...,-116.706372134621 ->
        //       ...,-116.700058864366: 577.79 + 577.79 = 1,155.57 m out and away. Its TASKCMPLT
        //       was the one that DID pair to a vendor move-along completion.
        const double PlatoonRouteM = 1155.51, PlatoonLastFromStartM = 0.0053;
        const double BdeRouteM = 1155.57, BdeLastFromStartM = 1155.57;
        const double CfgRadius = 500.0, MinTravel = 100.0;

        Console.WriteLine("  -- STP-837 (a): the effective radius and the traversal bar");
        double platoonRadius = ArrivalPolicy.RadiusFor(CfgRadius, PlatoonRouteM);
        Check($"a 1,155.5 m route shrinks the 500 m radius to 0.25 x route = {platoonRadius:F1} m",
              Math.Abs(platoonRadius - 288.8775) < 0.01);
        Check("a 3 km route keeps the configured 500 m (0.25 x 3000 = 750 is the looser of the two)",
              Math.Abs(ArrivalPolicy.RadiusFor(CfgRadius, 3000.0) - 500.0) < 1e-9);
        Check("an UNKNOWN route length (NaN) leaves the configured radius exactly as it was",
              Math.Abs(ArrivalPolicy.RadiusFor(CfgRadius, double.NaN) - 500.0) < 1e-9
              && Math.Abs(ArrivalPolicy.RadiusFor(CfgRadius, 0.0) - 500.0) < 1e-9);
        Check($"the traversal bar on a 1,155.5 m route is half of it ({ArrivalPolicy.RequiredTravelFor(PlatoonRouteM, MinTravel):F1} m)",
              Math.Abs(ArrivalPolicy.RequiredTravelFor(PlatoonRouteM, MinTravel) - 577.755) < 0.01);
        Check("on a SHORT (120 m) route the Vrf:ArrivalMinTravelMeters floor wins: 100 m, not 60",
              Math.Abs(ArrivalPolicy.RequiredTravelFor(120.0, MinTravel) - 100.0) < 1e-9);
        Check("an UNKNOWN route length falls back to the floor alone",
              Math.Abs(ArrivalPolicy.RequiredTravelFor(double.NaN, MinTravel) - 100.0) < 1e-9);

        Console.WriteLine("  -- STP-837 (b): FAIL-FIRST - the old rule closes V6g's platoon, the new one refuses it");
        var v6gPlatoonDistances = new double[] { 26, 120, 200, 300 };     // "4/4 within 500 m (nearest 26 m)"
        var v6gPlatoonTravel = new double[] { 20, 578, 578, 133 };        // only 2 of 4 drove the leg
        var oldRule = ArrivalPolicy.Decide(v6gPlatoonDistances, 4, CfgRadius, 0.5);
        Check("DISABLED ARM (proximity only, pre-STP-837): 4/4 within 500 m -> ARRIVED. That is the " +
              "V6g early TASKCMPLT, 516 s before the vendor's own completion", oldRule.Arrived && oldRule.Within == 4);
        Check("ENABLED: the task is NOT CLOSABLE BY ARRIVAL AT ALL - its last vertex is 0.005 m from " +
              $"the dispatch position, inside the {platoonRadius:F0} m radius",
              !ArrivalPolicy.ClosableByArrival(PlatoonLastFromStartM, platoonRadius));
        var platoonSamples = new List<ArrivalPolicy.MemberSample>();
        for (int i = 0; i < 4; i++)
            platoonSamples.Add(new ArrivalPolicy.MemberSample(v6gPlatoonDistances[i], v6gPlatoonTravel[i]));
        var newRule = ArrivalPolicy.DecideWithTraversal(platoonSamples, 4, CfgRadius, 0.5,
                                                        PlatoonRouteM, MinTravel);
        Check($"... and even if the closability gate were removed, only {newRule.Within} of 4 count " +
              "(M1A2 1 moved 20 m, M1A2 4 moved 133 m, one sits 300 m out) -> NOT arrived",
              !newRule.Arrived && newRule.Within == 2);
        Check("the decision carries the numbers the log line needs: effective radius, the bar, and " +
              "the best travel any member managed",
              Math.Abs(newRule.RadiusMeters - platoonRadius) < 1e-9
              && Math.Abs(newRule.RequiredTravelMeters - 577.755) < 0.01
              && Math.Abs(newRule.FarthestTravelMeters - 578.0) < 1e-9
              && Math.Abs(newRule.NearestMeters - 26.0) < 1e-9);

        Console.WriteLine("  -- STP-837 (c): the V6g ENTITY route still closes - but only after traversal");
        double bdeRadius = ArrivalPolicy.RadiusFor(CfgRadius, BdeRouteM);
        double bdeBar = ArrivalPolicy.RequiredTravelFor(BdeRouteM, MinTravel);
        Check($"1.BdeHQ's 1,155.6 m route IS closable by arrival (last vertex {BdeLastFromStartM:F0} m " +
              $"from the start, radius {bdeRadius:F0} m)",
              ArrivalPolicy.ClosableByArrival(BdeLastFromStartM, bdeRadius));
        Check($"the bar is {bdeBar:F1} m - the brief's \"~578 m\"", Math.Abs(bdeBar - 577.785) < 0.01);
        var atEndShortTravel = new[] { new ArrivalPolicy.MemberSample(100.0, 500.0) };
        Check("100 m from the last vertex but only 500 m travelled -> NOT arrived (short of the bar)",
              !ArrivalPolicy.DecideWithTraversal(atEndShortTravel, 1, CfgRadius, 0.5, BdeRouteM, MinTravel).Arrived);
        var atEndFullTravel = new[] { new ArrivalPolicy.MemberSample(100.0, 578.0) };
        Check("100 m from the last vertex and 578 m travelled -> ARRIVED",
              ArrivalPolicy.DecideWithTraversal(atEndFullTravel, 1, CfgRadius, 0.5, BdeRouteM, MinTravel).Arrived);
        var travelledButShort = new[] { new ArrivalPolicy.MemberSample(400.0, 1200.0) };
        Check("1,200 m travelled but 400 m from the last vertex - outside the 288.9 m effective " +
              "radius (the OLD 500 m would have counted it) -> NOT arrived",
              !ArrivalPolicy.DecideWithTraversal(travelledButShort, 1, CfgRadius, 0.5, BdeRouteM, MinTravel).Arrived
              && ArrivalPolicy.Decide(new double[] { 400.0 }, 1, CfgRadius, 0.5).Arrived);

        Console.WriteLine("  -- STP-837 (d): the straggler property of 2026-09-07 is UNCHANGED");
        // The case the feature was built for: 5 of 6 at the end of a 30 km leg, one M3 7 km back.
        const double LongRouteM = 30000.0;
        var longRoute = new List<ArrivalPolicy.MemberSample>();
        for (int i = 0; i < 5; i++) longRoute.Add(new ArrivalPolicy.MemberSample(10.0 + i * 10, 29900.0));
        longRoute.Add(new ArrivalPolicy.MemberSample(7000.0, 23000.0));
        Check("5 of 6 at the end of a 30 km leg, one M3 7 km back -> still ARRIVED (the traversal " +
              "test is satisfied by anything that actually drove the leg)",
              ArrivalPolicy.ClosableByArrival(LongRouteM, ArrivalPolicy.RadiusFor(CfgRadius, LongRouteM))
              && ArrivalPolicy.DecideWithTraversal(longRoute, 6, CfgRadius, 0.5, LongRouteM, MinTravel).Arrived);
        var leaderAlone = new List<ArrivalPolicy.MemberSample> { new(10.0, 30000.0) };
        for (int i = 0; i < 5; i++) leaderAlone.Add(new ArrivalPolicy.MemberSample(30000.0, 5.0));
        Check("leader alone at the end, five followers 30 km back -> still NOT arrived",
              !ArrivalPolicy.DecideWithTraversal(leaderAlone, 6, CfgRadius, 0.5, LongRouteM, MinTravel).Arrived);

        Console.WriteLine("  -- STP-837 (e): degenerate inputs");
        Check("an UNREADABLE dispatch baseline (NaN travel) is never counted - the conservative " +
              "direction is 'do not close'",
              !ArrivalPolicy.DecideWithTraversal(new[] { new ArrivalPolicy.MemberSample(10.0, double.NaN) },
                                                 1, CfgRadius, 0.5, BdeRouteM, MinTravel).Arrived);
        Check("no samples at all -> NOT arrived, and the radius/bar are still reported for the log",
              !ArrivalPolicy.DecideWithTraversal(Array.Empty<ArrivalPolicy.MemberSample>(), 6, CfgRadius,
                                                 0.5, BdeRouteM, MinTravel).Arrived
              && Math.Abs(ArrivalPolicy.DecideWithTraversal(null, 6, CfgRadius, 0.5, BdeRouteM, MinTravel)
                          .RadiusMeters - bdeRadius) < 1e-9);
        Check("an UNKNOWN route length degrades to the old radius + the floor, and stays closable",
              ArrivalPolicy.ClosableByArrival(double.NaN, 500.0)
              && ArrivalPolicy.DecideWithTraversal(new[] { new ArrivalPolicy.MemberSample(400.0, 150.0) },
                                                   1, CfgRadius, 0.5, double.NaN, MinTravel).Arrived);
        Check("fraction 1.0 (ALL members) still means all of them under the new rule",
              !ArrivalPolicy.DecideWithTraversal(
                  new[] { new ArrivalPolicy.MemberSample(10.0, 900.0), new ArrivalPolicy.MemberSample(10.0, 10.0) },
                  2, CfgRadius, 1.0, BdeRouteM, MinTravel).Arrived);

        Console.WriteLine(fails == 0 ? "arrival-selftest: ALL CHECKS PASSED" : $"arrival-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
