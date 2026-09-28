using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using VrfC2SimApp.Preflight;

namespace VrfC2SimApp;

/// <summary>
/// M3 - THE PLANNED CONTAINER MOVE (RL-20260928-03, owner "AUTO it is"; docs/experiments/FINDING_AGGREGATE_MOVEMENT_
/// OBSTACLES_2026-09-28.md). A section of `--rulings-selftest`, and `--planned-move-selftest [cache-dir]` on its own (with a
/// cache directory holding osm-highways/ tiles it also prints the AUTO decision for T14's, T10's and T02's G1-2 legs).
/// Bridge-free: no MAK, no network. The source guard "no container MOVE issues PA_Move_Along_Route unless Literal" is
/// DoNotRulesSelfTest (d10), beside d0-d9; the bridge binding of the new variables is --populate-selftest p11.
///
///   (m1) PLANNER SELECTION per setting - parse, the start refusals, the predicates, and what each planner ISSUES
///        (script, target, variables), with Literal as the fail-first arm (it still reaches PA_Move_Along_Route).
///   (m2) PER-VERTEX CHAINING - a 2-vertex route under the group planner issues vertex 1, then vertex 2 on vertex 1's
///        completion, never both; a planned chain accepts only its own completion type; its last vertex goes to D-6.
///   (m3) THE COMPLETION MAPPING - the vendor's Move (Group) rule (roleNode.lua), the per-member step, the chain's
///        Advance / Failed and the TASKABRT reason; the log-line shapes the G1-3 registration greps.
///   (m4) LITERAL BYTE-FOR-BYTE - today's PA_Move_Along_Route call, script and variables, as G1-2 issued it.
///   (m5) THE SETTING - compiled default, both json files, the environment round-trip, the start-up line.
///   (m6) THE SERVICE GLUE - source tripwires that pin VrfC2SimService.cs to the (m2)/(m3) replay's shape.
///   (m7) THE VENDOR FILES - the script ids, parameter types and completion rule read from C:\MAK (read-only; SKIP if absent).
///   (m8) THE AUTO ROAD DECISION - the vendor road filter, the tile band, NEAR / FAR / UNKNOWN on synthetic tiles.
///   (m9) optional: the AUTO decision on real tiles for the cut-A legs of G1-2 (printed, informational).
/// </summary>
public static class PlannedMoveSelfTest
{
    // The G1-2 dispatch routes (live origin first) - PREREG_IRONSTORM_AGG_G1-2_2026-09-28.md sec 1(a); the origins are the
    // containers' points at dispatch (the population lines L554/L562 and the Result's P4 T14 origin fix).
    public static readonly (string Task, (double Lat, double Lon)[] Route)[] CutALegs =
    {
        ("T14 48 IBCT", new[] { (54.019341, 23.313809), (54.0146, 23.3315), (54.040348, 23.324206) }),
        ("T10 1-112 IN", new[] { (54.042688, 23.308235), (54.029734, 23.305499), (54.024, 23.313), (54.019389, 23.313902) }),
        ("T02 28ID", new[] { (53.992385, 23.211255), (54.028874, 23.264401) }),
    };

    private static readonly VertexChainTracker.Point O = new(54.019341, 23.313809, 145.4);
    private static readonly VertexChainTracker.Point V1 = new(54.0146, 23.3315, 149.9);
    private static readonly VertexChainTracker.Point V2 = new(54.040348, 23.324206, 142.9);

    public static int Run(string realCacheDir = null)
    {
        int failures = 0;
        Selection(ref failures);
        Chaining(ref failures);
        Completion(ref failures);
        Literal(ref failures);
        Setting(ref failures);
        Glue(ref failures);
        Vendor(ref failures);
        Roads(ref failures);
        if (realCacheDir != null) RealLegs(realCacheDir);
        return failures;
    }

    // ------------------------------------------------------------------------------ (m1) ----
    private static void Selection(ref int failures)
    {
        bool parsed = true;
        foreach (var (raw, want) in new[]
                 {
                     ("Auto", AggregateMovePlanner.Auto), ("auto", AggregateMovePlanner.Auto), (" AUTO ", AggregateMovePlanner.Auto),
                     ("Group", AggregateMovePlanner.Group), ("groupoffroad", AggregateMovePlanner.GroupOffRoad),
                     ("PerMemberOffRoad", AggregateMovePlanner.PerMemberOffRoad), ("LITERAL", AggregateMovePlanner.Literal),
                     ("", AggregateMovePlanner.Auto), (null, AggregateMovePlanner.Auto),
                 })
            parsed &= AggregateMovePolicy.TryParse(raw, out var p) && p == want;
        Check(ref failures, parsed,
              "(m1) Vrf:AggregateMovePlanner parses every NAME case-insensitively; blank = Auto, the default (RL-20260928-03)");
        Check(ref failures, !AggregateMovePolicy.TryParse("1", out _) && !AggregateMovePolicy.TryParse("Roads", out _)
                            && AggregateMovePolicy.StartRefusal("Roads", false)?.Contains("is not one of") == true,
              "(m1) a number or an unknown word is NOT recognised - the aggregate model set REFUSES TO START on it");
        string offroad = AggregateMovePolicy.StartRefusal("GroupOffRoad", allowLiteralMove: false);
        Check(ref failures, offroad != null && offroad.Contains("Vrf:AllowLiteralMove=true") && offroad.Contains("CONTROL")
                            && AggregateMovePolicy.StartRefusal("GroupOffRoad", allowLiteralMove: true) == null
                            && new[] { "Auto", "Group", "PerMemberOffRoad", "Literal", "" }
                                   .All(v => AggregateMovePolicy.StartRefusal(v, false) == null),
              "(m1) GroupOffRoad (a DIRECT move, not a planner) is refused unless Vrf:AllowLiteralMove=true; every other value starts");
        var all = Enum.GetValues<AggregateMovePlanner>();
        Check(ref failures, all.Count(AggregateMovePolicy.IsPlanned) == 4 && !AggregateMovePolicy.IsPlanned(AggregateMovePlanner.Literal)
                            && all.Where(AggregateMovePolicy.IsPerMember).SequenceEqual(new[] { AggregateMovePlanner.Auto, AggregateMovePlanner.PerMemberOffRoad })
                            && all.Where(AggregateMovePolicy.IsGroup).SequenceEqual(new[] { AggregateMovePlanner.Group, AggregateMovePlanner.GroupOffRoad }),
              "(m1) the predicates: planned = all but Literal; per member = Auto, PerMemberOffRoad; Move (Group) = Group, GroupOffRoad");

        var near = new RoadDecision(RoadProximity.Near, AggregateMovePolicy.RoadPathQuery, 120, "r", "highway=track", 500, 1000, 0);
        var far = new RoadDecision(RoadProximity.Far, AggregateMovePolicy.NoPathQuery, 812, "r", "highway=track", 500, 1000, 0);
        var unk = new RoadDecision(RoadProximity.Unknown, AggregateMovePolicy.NoPathQuery, double.NaN, "", "", 500, 1000, 2);
        var na = new RoadDecision(RoadProximity.NotApplicable, "", double.NaN, "", "", 500, 1000, 0);
        string Vars(PlannedVertexTask t) => string.Join(", ", t.Vars);
        var g = AggregateMovePolicy.ForVertex(AggregateMovePlanner.Group, V2.Lat, V2.Lon, na);
        var go = AggregateMovePolicy.ForVertex(AggregateMovePlanner.GroupOffRoad, V2.Lat, V2.Lon, na);
        var pm = AggregateMovePolicy.ForVertex(AggregateMovePlanner.PerMemberOffRoad, V2.Lat, V2.Lon, na);
        var aNear = AggregateMovePolicy.ForVertex(AggregateMovePlanner.Auto, V2.Lat, V2.Lon, near);
        var aFar = AggregateMovePolicy.ForVertex(AggregateMovePlanner.Auto, V2.Lat, V2.Lon, far);
        var aUnk = AggregateMovePolicy.ForVertex(AggregateMovePlanner.Auto, V2.Lat, V2.Lon, unk);
        Check(ref failures, g.ScriptId == "group_movement_simplified" && !g.PerMember && g.UseRoads
                            && Vars(g) == "destination=(54.040348,23.324206), useRoads=true"
                            && go.ScriptId == "group_movement_simplified" && !go.UseRoads
                            && Vars(go) == "destination=(54.040348,23.324206), useRoads=false",
              $"(m1) Group / GroupOffRoad ISSUE Move (Group) to the CONTAINER: group_movement_simplified [{Vars(g)}] / [{Vars(go)}]");
        const string Nav = "destination=(54.040348,23.324206), obstacleQuery=\"MAK_OBSTACLE\", pathQuery=\"{0}\", buffer=10, " +
                           "displayRoute=false, query=\"\"";
        Check(ref failures, pm.ScriptId == "navigate-to-location" && pm.PerMember && !pm.UseRoads
                            && Vars(pm) == string.Format(CultureInfo.InvariantCulture, Nav, "NONE"),
              $"(m1) PerMemberOffRoad ISSUES navigate-to-location to EACH MEMBER: [{Vars(pm)}]");
        Check(ref failures, aNear.PerMember && aNear.UseRoads && Vars(aNear) == string.Format(CultureInfo.InvariantCulture, Nav, "MAK_ROAD")
                            && !aFar.UseRoads && Vars(aFar) == string.Format(CultureInfo.InvariantCulture, Nav, "NONE")
                            && !aUnk.UseRoads && aUnk.PathQuery == "NONE",
              "(m1) Auto ISSUES the same per member, pathQuery MAK_ROAD on a NEAR leg and NONE on a FAR or UNKNOWN one - " +
              "obstacleQuery MAK_OBSTACLE and buffer 10 m always");
        Check(ref failures, g.Vars[0].Alt == 0.0 && pm.Vars[0].Alt == 0.0,
              "(m1) the destination is the vertex at altitude 0 - the vendor's 'location without altitude' (saved at 0.00 m)");
        bool literalThrows = false;
        try { AggregateMovePolicy.ForVertex(AggregateMovePlanner.Literal, V2.Lat, V2.Lon, na); }
        catch (InvalidOperationException) { literalThrows = true; }
        Check(ref failures, literalThrows && ContainerScripts.ForForm(GroundMoveForm.RouteTask, false) == "PA_Move_Along_Route",
              "FAIL-FIRST (m1): Literal issues NO planned vertex - its container route goes on to ForForm = PA_Move_Along_Route, " +
              "so the rows above measure the setting");

        // THE DISPATCH, replayed per planner on the real policy and the real trackers.
        var members = Members(3);
        foreach (var planner in all)
        {
            var r = new PlannedRecorder(planner);
            r.Dispatch("48 IBCT", "task-T14", new[] { O, V1, V2 }, members);
            bool ok = planner switch
            {
                AggregateMovePlanner.Literal => r.Issued.Count == 1 && r.Issued[0] == "CreateRoute(48 IBCT,3) -> PA_Move_Along_Route",
                AggregateMovePlanner.Group or AggregateMovePlanner.GroupOffRoad =>
                    r.Issued.Count == 1 && r.Issued[0] == "RunScriptedTask(VRF_UUID:container,group_movement_simplified,@54.0146)",
                _ => r.Issued.Count == 3 && r.Issued.All(s => s.Contains(",navigate-to-location,@54.0146)"))
                     && r.Issued.Select(s => s.Split(',')[0]).Distinct().Count() == 3,
            };
            Check(ref failures, ok, $"(m1) {planner}: a CONTAINER's 3-point route issues [{string.Join("; ", r.Issued)}]");
        }
    }

    // ------------------------------------------------------------------------------ (m2) ----
    private static void Chaining(ref int failures)
    {
        var r = new PlannedRecorder(AggregateMovePlanner.Group);
        r.Dispatch("48 IBCT", "task-T14", new[] { O, V1, V2 }, Array.Empty<PlannedMoveTracker.Member>());
        Check(ref failures, r.Issued.Count == 1 && r.Issued[0].EndsWith("@54.0146)", StringComparison.Ordinal),
              "(m2) a 2-VERTEX route under Move (Group): vertex 1 ONLY is issued at dispatch - never both");
        var stray1 = r.Complete("pa_move_along_route", true, V1);
        var stray2 = r.Complete("move-to", true, V1);
        Check(ref failures, stray1 == VertexChainTracker.Outcome.Stray && stray2 == VertexChainTracker.Outcome.Stray && r.Issued.Count == 1,
              "(m2) a late 'pa_move_along_route' or a 'move-to' completion is a Stray - a planned chain accepts ONLY its own type");
        var d1 = r.Complete("GROUP_MOVEMENT_SIMPLIFIED", true, V1);
        Check(ref failures, d1 == VertexChainTracker.Outcome.Advance && r.Issued.Count == 2 && r.Issued[1].EndsWith("@54.0403)", StringComparison.Ordinal),
              "(m2) vertex 1's 'group_movement_simplified' completion (any case) issues vertex 2 - and only then");
        var d2 = r.Complete("group_movement_simplified", true, V2);
        Check(ref failures, d2 == VertexChainTracker.Outcome.FinalVertex && r.Issued.Count == 2,
              $"(m2) vertex 2's completion is the LAST vertex: FinalVertex, handed to the container's completion rules, nothing " +
              $"more issued (got {d2}, {r.Issued.Count} issues)");
        // NEVER BOTH: between vertex 1's completion and vertex 2's issue a second completion is a Stray, and vertex 2 is
        // released exactly once (the tick continuation's TryBeginIssue).
        var raw = new VertexChainTracker(100.0);
        raw.Start("C", "VRF_UUID:c", "task", "taskee", "T", O, new[] { V1, V2 }, "group_movement_simplified", handLastVertexOn: true);
        var adv = raw.OnCompletion("C", "group_movement_simplified", true, V1, false);
        var early = raw.OnCompletion("C", "group_movement_simplified", true, V1, false);
        bool once = raw.TryBeginIssue("C", adv.Chain.Generation, 2, out var next2, out _);
        bool twice = raw.TryBeginIssue("C", adv.Chain.Generation, 2, out _, out _);
        Check(ref failures, adv.Outcome == VertexChainTracker.Outcome.Advance && early.Outcome == VertexChainTracker.Outcome.Stray
                            && once && next2 == V2 && !twice,
              "(m2) NEVER BOTH: a second completion before vertex 2 is issued is a Stray, and vertex 2 is released EXACTLY once");

        // THE LAST VERTEX GOES TO D-6 even when vacuous (handLastVertexOn) - fail-first: without the flag it is withheld here.
        var t = new VertexChainTracker(100.0);
        t.Start("C", "VRF_UUID:c", "task", "taskee", "T", O, new[] { V1 }, "group_movement_simplified", handLastVertexOn: true);
        var vac = t.OnCompletion("C", "group_movement_simplified", true, O, arrivalAlreadyReported: false);
        var tOff = new VertexChainTracker(100.0);
        tOff.Start("C", "VRF_UUID:c", "task", "taskee", "T", O, new[] { V1 }, "group_movement_simplified", handLastVertexOn: false);
        var vacOff = tOff.OnCompletion("C", "group_movement_simplified", true, O, arrivalAlreadyReported: false);
        Check(ref failures, vac.Outcome == VertexChainTracker.Outcome.FinalVertex && vac.Vacuous
                            && vacOff.Outcome == VertexChainTracker.Outcome.FinalVacuous,
              "(m2) a VACUOUS last vertex of a container chain is FinalVertex (flagged vacuous) - D-6 withholds it with its own " +
              "'completed short:' line; FAIL-FIRST: without handLastVertexOn the chain itself would withhold it (FinalVacuous)");

        // THE COMPLETION TYPE IS PER CHAIN - fail-first: a chain started WITHOUT it keeps M1's move-to rule.
        var m1 = new VertexChainTracker(100.0);
        m1.Start("P", "VRF_UUID:p", "task", "taskee", "T", O, new[] { V1, V2 });
        var planned = new VertexChainTracker(100.0);
        planned.Start("C", "VRF_UUID:c", "task", "taskee", "T", O, new[] { V1, V2 }, "navigate-to-location");
        Check(ref failures, m1.OnCompletion("P", "group_movement_simplified", true, V1, false).Outcome == VertexChainTracker.Outcome.Stray
                            && m1.OnCompletion("P", "move-to", true, V1, false).Outcome == VertexChainTracker.Outcome.Advance
                            && planned.OnCompletion("C", "", true, V1, false).Outcome == VertexChainTracker.Outcome.Stray
                            && planned.OnCompletion("C", "navigate-to-location", true, V1, false).Outcome == VertexChainTracker.Outcome.Advance,
              "FAIL-FIRST (m2): a lone platform's chain (no type) is unchanged - 'move-to' advances it, 'group_movement_simplified' " +
              "does not; a PLANNED chain advances on its own type only - not even an empty type (M1 accepts empty)");
        Check(ref failures, planned.TryGet("C", out var ps) && ps.Planned && ps.CompletionTaskType == "navigate-to-location"
                            && m1.TryGet("P", out var mp) && !mp.Planned,
              "(m2) the snapshot says which kind of chain it is (Planned), so the service routes and issues by it");
    }

    // ------------------------------------------------------------------------------ (m3) ----
    private static void Completion(ref int failures)
    {
        bool? O2(params bool?[] r) => AggregateMovePolicy.GroupOutcome(r);
        Check(ref failures, O2() == false && O2((bool?)null) == null && O2(true) == true && O2(true, false) == true
                            && O2(false, false) == false && O2(true, null) == null && O2(false, null) == null,
              "(m3) THE VENDOR'S Move (Group) RULE (roleNode.lua :61-67, :169-282): ends when EVERY member ended; SUCCESS when at " +
              "least ONE succeeded; FAIL when none did, or with no member at all (critical role)");
        bool naiveAnyFailFails = !new[] { true, false }.All(x => x);
        Check(ref failures, naiveAnyFailFails && O2(true, false) == true,
              "FAIL-FIRST (m3): the brief's 'one member's plan failure fails the command' would FAIL [success, failure]; the " +
              "vendor's code SUCCEEDS it - the vendor's rule is the one pinned (the contradiction is reported, not adopted)");

        // THE PER-MEMBER STEP (Auto, PerMemberOffRoad).
        var tr = new PlannedMoveTracker();
        var ms = Members(3);
        tr.Begin(new PlannedMoveTracker.Context("C", "VRF_UUID:c", 7, AggregateMovePlanner.Auto, ms,
                                                new[] { default(RoadDecision), default }, 2));
        var noStep = tr.OnMemberCompleted("C", ms[0].Name, "navigate-to-location", true);
        tr.BeginVertex("C", 7, 1);
        var a = tr.OnMemberCompleted("C", ms[0].Name, "navigate-to-location", true);
        var aDup = tr.OnMemberCompleted("C", ms[0].Name, "navigate-to-location", true);
        var alien = tr.OnMemberCompleted("C", "someone-else", "navigate-to-location", true);
        var sub = tr.OnMemberCompleted("C", ms[1].Name, "move-along", true);
        var b = tr.OnMemberCompleted("C", ms[1].Name, "navigate-to-location", false);
        var c = tr.OnMemberCompleted("C", ms[2].Name, "Navigate-To-Location", true);
        var late = tr.OnMemberCompleted("C", ms[1].Name, "navigate-to-location", true);
        Check(ref failures, !noStep.Consumed && a.Consumed && !a.StepDone && aDup.Consumed && !aDup.StepDone
                            && !alien.Consumed && !sub.Consumed && b.Consumed && !b.StepDone
                            && c.Consumed && c.StepDone && c.Success && c.Succeeded == 2 && c.Failed == 1 && c.Vertex == 1
                            && !late.Consumed,
              "(m3) a per-member step: nothing counts before the step opens; each member's own navigate-to-location counts ONCE; " +
              "another object, a 'move-along' subtask and a report after the step closed are not consumed; the LAST member " +
              "closes it - 2 succeeded, 1 failed -> SUCCESS by the vendor's rule");
        tr.BeginVertex("C", 7, 2);
        foreach (var m in ms) tr.OnMemberCompleted("C", m.Name, "navigate-to-location", false);
        var allBad = tr.TryGet("C", out _);
        var tr2 = new PlannedMoveTracker();
        tr2.Begin(new PlannedMoveTracker.Context("C", "VRF_UUID:c", 1, AggregateMovePlanner.PerMemberOffRoad, ms, null, 1));
        tr2.BeginVertex("C", 1, 1);
        PlannedMoveTracker.MemberVerdict last = default;
        foreach (var m in ms) last = tr2.OnMemberCompleted("C", m.Name, "navigate-to-location", false);
        Check(ref failures, allBad && last.StepDone && !last.Success && last.Failed == 3,
              "(m3) every member FAILED -> the vertex FAILED (no member succeeded)");
        var grp = new PlannedMoveTracker();
        grp.Begin(new PlannedMoveTracker.Context("C", "VRF_UUID:c", 1, AggregateMovePlanner.Group, Array.Empty<PlannedMoveTracker.Member>(), null, 1));
        Check(ref failures, !grp.OnMemberCompleted("C", "m", "move_to_location_plan_path", true).Consumed,
              "(m3) under Move (Group) the members' own completions are NOT aggregated here - the vendor's behaviour engine does it");

        // THE CHAIN MAPPING + THE REASON, replayed: vertex 1 of 2 fails for every member -> Failed -> TASKABRT with the reason.
        var r = new PlannedRecorder(AggregateMovePlanner.PerMemberOffRoad);
        r.Dispatch("48 IBCT", "task-T14", new[] { O, V1, V2 }, ms);
        var o1 = r.CompleteMember(ms[0].Name, false, V1);
        var o2 = r.CompleteMember(ms[1].Name, false, V1);
        var o3 = r.CompleteMember(ms[2].Name, false, V1);
        Check(ref failures, o1 == null && o2 == null && o3 == VertexChainTracker.Outcome.Failed && r.Issued.Count == 3,
              "(m3) no member's navigate-to-location succeeded on vertex 1 -> the chain's Failed: nothing more is issued");
        string why = AggregateMovePolicy.FailureReason("48 IBCT", 1, 2, AggregateMovePlanner.PerMemberOffRoad, 3, 3);
        string whyG = AggregateMovePolicy.FailureReason("48 IBCT", 1, 2, AggregateMovePlanner.Group, 0, 0);
        Check(ref failures, why.StartsWith("PLANNED MOVE 48 IBCT vertex 1 of 2: navigate-to-location FAILED for 3 of 3 member(s)", StringComparison.Ordinal)
                            && why.Contains("succeeded for none") && why.Contains("roleNode.lua :246-280")
                            && whyG.Contains("success=false") && whyG.Contains("NO member's move succeeded"),
              "(m3) the TASKABRT carries the reason, naming the vendor's rule (per member, and for Move (Group))");
        var r2 = new PlannedRecorder(AggregateMovePlanner.Auto);
        r2.Dispatch("48 IBCT", "task-T14", new[] { O, V1, V2 }, ms);
        r2.CompleteMember(ms[0].Name, false, V1);
        r2.CompleteMember(ms[1].Name, true, V1);
        var adv = r2.CompleteMember(ms[2].Name, true, V1);
        Check(ref failures, adv == VertexChainTracker.Outcome.Advance && r2.Issued.Count == 6
                            && r2.Issued.Skip(3).All(s => s.Contains("@54.0403")),
              "(m3) one member failed, two succeeded on vertex 1 -> Advance: vertex 2 goes to ALL THREE members");

        // THE LINE SHAPES (the G1-3 registration greps these).
        var shape = new Regex(@"^PLANNED MOVE .+ vertex \d+ of \d+: \S+ issued \(useRoads (true|false)\) -> [A-Z][A-Z ]*[A-Z]( .*)?$");
        string l1 = AggregateMovePolicy.VertexLine("48 IBCT", 1, 2, "group_movement_simplified", true, "OUTSTANDING", "to (54.014600,23.331500)");
        string l2 = AggregateMovePolicy.VertexLine("48 IBCT", 1, 2, "navigate-to-location", false, "COMPLETED");
        string l3 = AggregateMovePolicy.VertexLine("48 IBCT", 2, 2, "navigate-to-location", true, "LAST VERTEX COMPLETED", "- x");
        string l4 = AggregateMovePolicy.VertexLine("48 IBCT", 2, 2, "group_movement_simplified", true, "FAILED", "- y");
        Check(ref failures, new[] { l1, l2, l3, l4 }.All(l => shape.IsMatch(l))
                            && l1 == "PLANNED MOVE 48 IBCT vertex 1 of 2: group_movement_simplified issued (useRoads true) -> OUTSTANDING to (54.014600,23.331500)"
                            && l2 == "PLANNED MOVE 48 IBCT vertex 1 of 2: navigate-to-location issued (useRoads false) -> COMPLETED",
              "(m3) 'PLANNED MOVE <container> vertex k of N: <planner> issued (useRoads <bool>) -> <outcome>' - OUTSTANDING, COMPLETED, " +
              "LAST VERTEX COMPLETED, FAILED", l1);
        string rn = AggregateMovePolicy.RoadLine("48 IBCT", 2, 2, new RoadDecision(RoadProximity.Near, "MAK_ROAD", 118.6, "720453203", "highway=primary", 500, 1000, 0));
        string rf = AggregateMovePolicy.RoadLine("48 IBCT", 1, 2, new RoadDecision(RoadProximity.Far, "NONE", 812.4, "9", "highway=track", 500, 1000, 0));
        string rfx = AggregateMovePolicy.RoadLine("48 IBCT", 1, 2, new RoadDecision(RoadProximity.Far, "NONE", double.PositiveInfinity, "", "", 500, 1000, 0));
        string ru = AggregateMovePolicy.RoadLine("48 IBCT", 1, 2, new RoadDecision(RoadProximity.Unknown, "NONE", double.NaN, "", "", 500, 1000, 2));
        Check(ref failures, rn.StartsWith("PLANNED MOVE 48 IBCT vertex 2 of 2: roads NEAR (119 m) -> MAK_ROAD; obstacleQuery MAK_OBSTACLE buffer 10 m", StringComparison.Ordinal)
                            && rf.StartsWith("PLANNED MOVE 48 IBCT vertex 1 of 2: roads FAR (812 m) -> NONE; obstacleQuery MAK_OBSTACLE buffer 10 m", StringComparison.Ordinal)
                            && rfx.StartsWith("PLANNED MOVE 48 IBCT vertex 1 of 2: roads FAR (> 1000 m) -> NONE; obstacleQuery MAK_OBSTACLE buffer 10 m", StringComparison.Ordinal)
                            && ru.StartsWith("PLANNED MOVE 48 IBCT vertex 1 of 2: roads UNKNOWN (2 osm-highways tile(s) within 500 m NOT readable", StringComparison.Ordinal)
                            && ru.Contains("-> NONE; obstacleQuery MAK_OBSTACLE buffer 10 m"),
              "(m3) the AUTO line per leg: 'roads NEAR (<d> m) -> MAK_ROAD | roads FAR (<d> m) -> NONE; obstacleQuery MAK_OBSTACLE " +
              "buffer 10 m', and UNKNOWN -> NONE", rn);
        Check(ref failures, new[] { l1, l2, l3, l4, rn, rf, rfx, ru, why, whyG }.All(s => s.All(ch => ch < 128)),
              "(m3) every line is ASCII");
    }

    // ------------------------------------------------------------------------------ (m4) ----
    private static void Literal(ref int failures)
    {
        // G1-2 L29311: "RunScriptedTask PA_Move_Along_Route issued for CONTAINER <c> (VRF_UUID:<uuid>) with
        // [route=VRF_UUID:..., reverseDirection=false, startAtClosestVertex=false] - 17 published member(s)".
        string script = ContainerScripts.ForForm(GroundMoveForm.RouteTask, patrol: false);
        string vars = string.Join(", ", ContainerScripts.ForRoute(script, "VRF_UUID:route-uuid"));
        Check(ref failures, script == "PA_Move_Along_Route"
                            && vars == "route=VRF_UUID:route-uuid, reverseDirection=false, startAtClosestVertex=false",
              $"(m4) LITERAL, byte for byte: ForForm(RouteTask) = {script}, ForRoute = [{vars}] - G1-2's L29311 call (the baseline)");
        Check(ref failures, string.Join(", ", ContainerScripts.ToLocation(54.0268, 23.3172, 0.0)) == "location=(54.026800,23.317200), retrograde=false"
                            && ContainerScripts.ForForm(GroundMoveForm.RouteTask, patrol: true) == "PA_Patrol_Route"
                            && ContainerScripts.ForForm(GroundMoveForm.SinglePointMoveTo, false) == "PA_Move_To_Location_Direct",
              "(m4) the point move (PA_Move_To_Location_Direct) and the patrol (PA_Patrol_Route) are unchanged under every planner");
        var obj = new ContainerTaskVar("route", ContainerTaskVarKind.Object, "VRF_UUID:x");
        var flag = new ContainerTaskVar("reverseDirection", ContainerTaskVarKind.Flag, Flag: false);
        var loc = new ContainerTaskVar("location", ContainerTaskVarKind.Location, Lat: 1.5, Lon: 2.25);
        Check(ref failures, obj.ToString() == "route=VRF_UUID:x" && flag.ToString() == "reverseDirection=false"
                            && loc.ToString() == "location=(1.500000,2.250000)",
              "(m4) adding the Text and Number kinds changed nothing in the three kinds C1 logs (Object, Flag, Location)");
        string svc = ServiceSource();
        Check(ref failures, svc.Contains("var vars = ContainerScripts.ForRoute(pending.ScriptId, e.Uuid);")
                            && svc.Contains("var verdict = _containers.TryIssueScriptedMove(pending.ContainerName, pending.TaskeeVrfUuid,")
                            && svc.Contains("string containerScript = unit.IsContainer ? ContainerScripts.ForForm(moveForm, patrol) : null;")
                            && svc.Contains("\"Route '{Route}' ({RouteUuid}) created; RunScriptedTask {Script} issued for CONTAINER \" +"),
              "(m4) the Literal path in the service is C1's, verbatim: ForForm at the route arm, ForRoute + TryIssueScriptedMove in " +
              "the route-created callback, the same log line");
    }

    // ------------------------------------------------------------------------------ (m5) ----
    private static void Setting(ref int failures)
    {
        var d = new VrfSettings();
        Check(ref failures, d.AggregateMovePlanner == "Auto" && !d.AllowLiteralMove && d.RoadProximityMeters == 500.0,
              "(m5) compiled defaults: AggregateMovePlanner Auto (RL-20260928-03), AllowLiteralMove false, RoadProximityMeters 500");
        string repo = FindRepoRoot();
        string app = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
        string demo = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.Demo.json");
        Check(ref failures, app != null && File.Exists(app) && File.Exists(demo), $"(m5) both shipped settings files are on disk ({app})");
        if (app == null || !File.Exists(app) || !File.Exists(demo)) return;
        string text = File.ReadAllText(app);
        Check(ref failures, text.Contains("\"AggregateMovePlanner\": \"Auto\"") && text.Contains("\"AllowLiteralMove\": false")
                            && text.Contains("\"RoadProximityMeters\": 500"),
              "(m5) appsettings.json PINS the three keys explicitly (Auto, false, 500)");
        var doc = Regex.Match(text, "\"_AggregateMovePlanner\": \"([^\"]*)\"");
        Check(ref failures, doc.Success && doc.Groups[1].Value.Contains("RL-20260928-03")
                            && doc.Groups[1].Value.Contains("FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28")
                            && doc.Groups[1].Value.Contains("G1-2") && doc.Groups[1].Value.All(ch => ch < 128),
              "(m5) its _AggregateMovePlanner text names the ruling, the finding and the G1-2 evidence, in ASCII");
        var shipped = new ConfigurationBuilder().AddJsonFile(app, optional: false).AddJsonFile(demo, optional: false)
                          .Build().GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        Check(ref failures, shipped.AggregateMovePlanner == "Auto" && AggregateMovePolicy.TryParse(shipped.AggregateMovePlanner, out var sp)
                            && sp == AggregateMovePlanner.Auto,
              "(m5) the two files layered as the Host layers them bind Auto");
        const string Env = "Vrf__AggregateMovePlanner";
        string saved = Environment.GetEnvironmentVariable(Env);
        try
        {
            foreach (var want in new[] { "Literal", "PerMemberOffRoad", "Group" })
            {
                Environment.SetEnvironmentVariable(Env, want);
                var byEnv = new ConfigurationBuilder().AddJsonFile(app, optional: false).AddJsonFile(demo, optional: false)
                                .AddEnvironmentVariables().Build().GetSection("Vrf").Get<VrfSettings>();
                Check(ref failures, byEnv != null && byEnv.AggregateMovePlanner == want && AggregateMovePolicy.TryParse(byEnv.AggregateMovePlanner, out var p)
                                    && p.ToString() == want,
                      $"(m5) ROUND TRIP: {Env}={want} over both json files binds and parses as {want}");
            }
        }
        finally { Environment.SetEnvironmentVariable(Env, saved); }
        string auto = AggregateMovePolicy.StartupLine(AggregateMovePlanner.Auto, true, false, 500, 12, 0, @"C:\cache");
        string lit = AggregateMovePolicy.StartupLine(AggregateMovePlanner.Literal, true, false, 500, 0, 0, @"C:\cache");
        string off = AggregateMovePolicy.StartupLine(AggregateMovePlanner.Auto, false, false, 500, 0, 0, "");
        Check(ref failures, auto.StartsWith("AGGREGATE MOVE PLANNER Auto (Vrf:AggregateMovePlanner; RL-20260928-03", StringComparison.Ordinal)
                            && auto.Contains("navigate-to-location") && auto.Contains("osm-highways (12 tile file(s)")
                            && lit.Contains("PA_Move_Along_Route") && lit.Contains("still detours")
                            && off.StartsWith("AGGREGATE MOVE PLANNER not in use", StringComparison.Ordinal)
                            && new[] { auto, lit, off }.All(s => s.All(ch => ch < 128)),
              "(m5) the start-up line, said in every state, names the ruling, what a route gets, the road-layer cache - ASCII");
    }

    // ------------------------------------------------------------------------------ (m6) ----
    private static void Glue(ref int failures)
    {
        string src = ServiceSource();
        Check(ref failures, src.Length > 0, "(m6) the service source is on disk");
        if (src.Length == 0) return;
        string consume = Between(src, "private bool ConsumeVertexChainCompletion(", "string where = DescribeVertexFix(d);");
        Check(ref failures, consume.Contains("if (d.Chain.Planned) return ConsumePlannedVertexCompletion(marking, d, vrfTaskType, success);"),
              "(m6) a PLANNED chain's completion is routed to ConsumePlannedVertexCompletion before M1's Move To lines");
        string issue = Between(src, "private void IssueNextVertex(", "/// P0.3: park a PLATFORM's ATTACK Fire At");
        int planned = issue.IndexOf("if (chain.Planned)", StringComparison.Ordinal);
        int moveTo = issue.IndexOf("_bridge.MoveToLocation(chain.VrfUuid", StringComparison.Ordinal);
        Check(ref failures, planned > 0 && moveTo > planned && issue.Substring(planned, moveTo - planned).Contains("IssuePlannedVertex(unitName, chain, vertex, v);")
                            && issue.Substring(planned, moveTo - planned).Contains("return;"),
              "(m6) IssueNextVertex issues a PLANNED chain's vertex with its planner's task and returns BEFORE M1's MoveToLocation");
        string member = Between(src, "if (_containerMode && !string.IsNullOrEmpty(marking) && _containers.IsMember(marking, out var memberOf))",
                                "_log.LogInformation(\"VRF task complete: {Unit} / {Task} (success={Ok})\"");
        Check(ref failures, member.IndexOf("if (RoutePlannedMemberCompletion(memberOf, marking, e.TaskType, success)) return;", StringComparison.Ordinal) > 0
                            && member.IndexOf("RoutePlannedMemberCompletion(", StringComparison.Ordinal)
                               < member.IndexOf("_log.LogDebug(", StringComparison.Ordinal),
              "(m6) a member's completion is offered to the per-member step FIRST; anything else stays C1's Debug line");
        string step = Between(src, "private void CompletePlannedStep(", "private string PlannedFailureText(");
        int s1 = step.IndexOf("if (ConsumeVertexChainCompletion(container, AggregateMovePolicy.NavigateScript, success)) return;", StringComparison.Ordinal);
        int s2 = step.IndexOf("WithholdShortContainerCompletion(container, AggregateMovePolicy.NavigateScript)", StringComparison.Ordinal);
        int s3 = step.IndexOf("ClearStallState(container);", StringComparison.Ordinal);
        int s4 = step.IndexOf("_arrivalReported.TryRemove(container, out var reportedTask)", StringComparison.Ordinal);
        int s5 = step.IndexOf("SynthesizeUnitCompletion(container, AggregateMovePolicy.NavigateScript, success);", StringComparison.Ordinal);
        Check(ref failures, s1 > 0 && s2 > s1 && s3 > s2 && s4 > s3 && s5 > s4,
              "(m6) an aggregated step takes the container's own path in OnVrfTaskCompleted's order: the chain, D-6, the watchdog " +
              "window, the arrival swallow, SynthesizeUnitCompletion");
        string cp = Between(src, "private bool ConsumePlannedVertexCompletion(", "private bool RoutePlannedMemberCompletion(");
        string routing = string.Join(", ", new[] { "Advance", "FinalVertex", "Failed", "Stray", "Retired" }
                                           .Select(o => o + "=" + ReturnAfter(cp, "case VertexChainTracker.Outcome." + o + ":")));
        Check(ref failures, routing == "Advance=true, FinalVertex=false, Failed=false, Stray=true, Retired=true"
                            && cp.Contains("QueueNextVertex(container, d);") && cp.Contains("_plannedFailReason[container] = (d.Chain.TaskUuid, reason);")
                            && !cp.Contains("SynthesizeUnitCompletion("),
              $"(m6) M1's routing for a planned chain - intermediate consumed (the next vertex queued), last vertex and failure handed " +
              $"on (the failure's reason left for its TASKABRT), stray and retired consumed ({routing})");
        Check(ref failures, Between(src, "private void SynthesizeUnitCompletion(", "private void OnVrfTextReport(")
                                .Contains("string plannedWhy = !success ? PlannedFailureText(name, taskUuid) : null;"),
              "(m6) SynthesizeUnitCompletion's TASKABRT carries a planned vertex's reason - only for the task it belongs to");
        string start = Between(src, "private void StartContainerPlannedMove(", "private void IssuePlannedVertex(");
        int sameTask = start.IndexOf("_vertexChains.TryGet(unit.Name, out var running)", StringComparison.Ordinal);
        int mark = start.IndexOf("MarkDispatched(task, unit, AggregateMovePolicy.DispatchKind(_movePlanner), routeGeo[^1], routeGeo);", StringComparison.Ordinal);
        int st = start.IndexOf("handLastVertexOn: true", StringComparison.Ordinal);
        int iss = start.IndexOf("IssuePlannedVertex(unit.Name, start.Chain, 1, start.First);", StringComparison.Ordinal);
        Check(ref failures, sameTask > 0 && mark > sameTask && st > mark && iss > st
                            && start.Contains("completionTaskType: AggregateMovePolicy.CompletionTaskType(_movePlanner)"),
              "(m6) StartContainerPlannedMove: the same-task check, MarkDispatched (the LAST vertex, the whole route), a PLANNED " +
              "chain (its one type, last vertex to D-6), then vertex 1 - M1's order");
        string iv = Between(src, "private void IssuePlannedVertex(", "private bool ConsumePlannedVertexCompletion(");
        Check(ref failures, iv.Contains("_containers.TryIssueMemberMoves(") && iv.Contains("_containers.TryIssueScriptedMove(")
                            && iv.IndexOf("_plannedMoves.BeginVertex(", StringComparison.Ordinal) < iv.IndexOf("_containers.TryIssueMemberMoves(", StringComparison.Ordinal)
                            && !iv.Contains("_bridge."),
              "(m6) IssuePlannedVertex issues ONLY through the population's gate (never _bridge directly), and opens a per-member " +
              "step before it tasks the members");
        string shift = Between(src, "private void QueueRouteShift(", "private bool ContinueShift(");
        Check(ref failures, shift.Contains("bool plannedContainer = unit.IsContainer && AggregateMovePolicy.IsPlanned(_movePlanner)")
                            && shift.Contains("VerbMapping.Classify(task.ActionCode).Intent != TaskIntent.Reconnoiter")
                            && shift.Contains("ReportOnlyReason =")
                            && shift.Contains("svc.WarmOsm(Preflight.OsmSet.Highways, pre.CheckedRoute, roadBand)"),
              "(m6) the pre-flight is REPORT + FALLBACK: report-only for a planned container route (not a patrol), the road layer " +
              "warmed off the tick thread for Auto; Literal and every entity mover keep the lateral shift");
        Check(ref failures, src.Contains("string plannerRefusal = AggregateMovePolicy.StartRefusal(_vrf.AggregateMovePlanner, _vrf.AllowLiteralMove);")
                            && src.Contains("AggregateMovePolicy.StartupLine(_movePlanner, true,")
                            && src.Contains("AggregateMovePolicy.StartupLine(_movePlanner, false,"),
              "(m6) start-up: the planner refusal joins C1's REFUSING TO START list; the planner line is said in both model sets");
    }

    // ------------------------------------------------------------------------------ (m7) ----
    private static void Vendor(ref int failures)
    {
        // The 5.2 install, found as --populate-selftest finds it (VRF_HOME, then MAK_VRFDIR, then C:\MAK\vrforces5.2d).
        string home = Environment.GetEnvironmentVariable("VRF_HOME") is { Length: > 0 } h ? h
                    : Environment.GetEnvironmentVariable("MAK_VRFDIR") is { Length: > 0 } m ? m
                    : @"C:\MAK\vrforces5.2d";
        string sms = Path.Combine(home, "data", "simulationModelSets");
        string gxml = Path.Combine(sms, "AggregateTacticalLevel", "scripts", "group_movement_simplified.xml");
        string glua = Path.Combine(sms, "AggregateTacticalLevel", "scripts", "group_movement_simplified.lua");
        string nxml = Path.Combine(sms, "base", "scripts", "navigate-to-location.xml");
        string nlua = Path.Combine(sms, "base", "scripts", "navigate-to-location.lua");
        string role = Path.Combine(home, "makLua", "behaviorEngine", "roleNode.lua");
        string enabler = Path.Combine(sms, "AggregateTacticalLevel", "vrfSim", "systems", "other", "ground-echelon-group-tactics-enabler.sysdef");
        if (!File.Exists(gxml) || !File.Exists(nxml))
        {
            Console.WriteLine($"  [SKIP] (m7) the vendor scripts are not installed under {sms} - the vendor-file checks are not evaluated");
            return;
        }
        string g = File.ReadAllText(gxml), gl = SafeRead(glua), n = File.ReadAllText(nxml), nl = SafeRead(nlua), rl = SafeRead(role);
        Check(ref failures, Regex.Match(g, "<myScriptId>([^<]*)</myScriptId>").Groups[1].Value == AggregateMovePolicy.GroupScript
                            && VarType(g, "destination") == "locationwithoutaltitude" && VarType(g, "useRoads") == "checkbox"
                            && VarDefault(g, "useRoads") == "1" && EntityTypeCount(g) == "0",
              "(m7) group_movement_simplified.xml: id, destination = locationwithoutaltitude, useRoads = checkbox default 1 (the vendor's " +
              "default ON), no entity-type restriction (valid for all types, scriptedTaskMetaData.h :276-277)");
        Check(ref failures, gl.Contains("\"Move_To_Location_Plan_Path\"") && gl.Contains("\"move-to-location\"")
                            && gl.Contains("isCritical = true") && !gl.Contains("successPolicy") && !gl.Contains("letAllFinish"),
              "(m7) group_movement_simplified.lua: useRoads -> Move_To_Location_Plan_Path per member, off -> a plain move-to-location; " +
              "the role is critical and sets NO successPolicy / letAllFinish (the engine's defaults apply)");
        Check(ref failures, rl.Contains("local successPolicy = args.successPolicy or \"one\"") && rl.Contains("local letAllFinish = true")
                            && rl.Contains("node.myStatus = \"fail\" --Children completed without a success"),
              "(m7) roleNode.lua: successPolicy defaults to \"one\", letAllFinish to true, and all-done-without-a-success = fail - the " +
              "rule GroupOutcome pins");
        var declared = Regex.Matches(n, "&lt;myVariableName&gt;([A-Za-z_]+)&lt;/myVariableName&gt;").Select(m => m.Groups[1].Value).ToHashSet();
        var sent = AggregateMovePolicy.NavigateVars(0, 0, "NONE").Select(v => v.Name).ToList();
        Check(ref failures, Regex.Match(n, "<myScriptId>([^<]*)</myScriptId>").Groups[1].Value == AggregateMovePolicy.NavigateScript
                            && VarType(n, "destination") == "locationreference" && VarType(n, "obstacleQuery") == "string"
                            && VarType(n, "pathQuery") == "string" && VarType(n, "buffer") == "range" && VarType(n, "displayRoute") == "checkbox"
                            && VarType(n, "query") == "string" && sent.All(declared.Contains) && sent.Count == 6
                            && n.Contains("<item>-1:-1:-1:-1:-1:-1:-1</item>"),
              "(m7) navigate-to-location.xml: every variable the per-member planners send is DECLARED, with the type the vendor gives " +
              "it (destination a locationreference, fed a location vector as the vendor's own subtask does); valid for every type");
        Check(ref failures, nl.Contains("if (taskParameters.query ~= \"\") then") && nl.Contains("obstacleQuery = taskParameters.query")
                            && nl.Contains("defaultPathQuery = \"MAK_ROAD\""),
              "(m7) navigate-to-location.lua lets the old 'query' OVERRIDE obstacleQuery unless it is \"\" - so query=\"\" is sent; " +
              "its own default path query is MAK_ROAD - so pathQuery is always sent");
        string en = SafeRead(enabler);
        Check(ref failures, en.Contains("\"group_movement_simplified\"") && en.Contains("\"group-navigate-route-to-location\""),
              "(m7) the Ground Echelon Group Tactics Enabler lists Move (Group) (INFO: the generic containers the interface creates " +
              "carry no systems; Move (Group) runs on them by its metadata - not yet seen live)");
        string roads = SafeRead(Path.Combine(Environment.GetEnvironmentVariable("MAK_SHAREDDATA") is { Length: > 0 } s ? s
                                             : @"C:\MAK\SharedData\19\latest", "TerrainData", "TerrainConfiguration", "osm.roads.model.xml"));
        if (roads.Length == 0)
            Console.WriteLine("  [SKIP] (m7) osm.roads.model.xml not found - the road-filter parity is not evaluated");
        else
        {
            var filtered = Regex.Matches(roads.Substring(0, Math.Max(0, roads.IndexOf("Pedestrian Features", StringComparison.Ordinal))),
                                         "hwy === \"([a-z_]+)\"").Select(m => m.Groups[1].Value).ToHashSet();
            Check(ref failures, roads.Contains("<features>data:osm-highways</features>") && filtered.SetEquals(OsmVendor.RoadModelSkippedHighways),
                  "(m7) osm.roads.model.xml builds the sim's Roads layer from data:osm-highways and drops exactly the highway values " +
                  $"OsmVendor.IsVehicleRoad drops ({string.Join(",", filtered.OrderBy(x => x))})");
        }
    }

    // ------------------------------------------------------------------------------ (m8) ----
    private static void Roads(ref int failures)
    {
        IReadOnlyDictionary<string, object> P(params (string K, object V)[] kv) => kv.ToDictionary(x => x.K, x => x.V);
        Check(ref failures, OsmVendor.IsVehicleRoad(Mvt.GeomLine, P(("highway", "track"))) && OsmVendor.IsVehicleRoad(Mvt.GeomLine, P(("highway", "construction")))
                            && !OsmVendor.IsVehicleRoad(Mvt.GeomLine, P(("highway", "footway"))) && !OsmVendor.IsVehicleRoad(Mvt.GeomPolygon, P(("highway", "primary")))
                            && !OsmVendor.IsVehicleRoad(Mvt.GeomLine, P(("highway", "primary"), ("@type", "node")))
                            && !OsmVendor.IsVehicleRoad(Mvt.GeomLine, P(("waterway", "river"))),
              "(m8) the vendor's vehicle-road filter: a track and a construction way are roads (the road model, unlike the bridge " +
              "filter, keeps construction), a footway, a polygon, a node and a non-highway are not");
        var a = (Lat: 54.0146, Lon: 23.3315);
        var b = (Lat: 54.040348, Lon: 23.324206);
        var f = OsmGeometry.Frame.At(a.Lat, a.Lon);
        var B = f.Xy(b);
        double len = Math.Sqrt(B.X * B.X + B.Y * B.Y);
        (double X, double Y) nrm = (B.Y / len, -B.X / len);              // the leg's right-hand normal
        List<(double Lat, double Lon)> Parallel(double off) => new() { f.LatLon((nrm.X * off, nrm.Y * off)), f.LatLon((B.X + nrm.X * off, B.Y + nrm.Y * off)) };
        OsmLine Road(string id, double off) { var pts = Parallel(off); return new OsmLine(id, "highway=track", 8, pts, GeoBox.Of(pts)); }
        OsmTileProvider World(IReadOnlyList<OsmLine> roads, Func<int, int, bool> unknown = null)
            => (set, x, y) => set != OsmSet.Highways ? OsmTile.Unknown(set, x, y, "not this set")
                              : unknown != null && unknown(x, y) ? OsmTile.Unknown(set, x, y, "synthetic hole")
                              : new OsmTile { Set = set, X = x, TmsY = y, Known = true, State = "synthetic", Roads = roads };
        double prox = AggregateMovePolicy.DefaultRoadProximityMeters;
        RoadDecision Decide(OsmTileProvider w)
        {
            var inBand = OsmQuery.NearestRoad(w, a, b, prox);
            var wide = inBand.Found ? inBand : OsmQuery.NearestRoad(w, a, b, 2 * prox);
            return AggregateMovePolicy.DecideRoads(AggregateMovePlanner.Auto, inBand, wide, prox);
        }
        var near = Decide(World(new[] { Road("near-120", 120.0), Road("far-800", 800.0) }));
        var far = Decide(World(new[] { Road("far-800", 800.0) }));
        var none = Decide(World(Array.Empty<OsmLine>()));
        var tiles = OsmQuery.TilesNear(a, b, prox);
        var hole = tiles.Last();
        var unknown = Decide(World(Array.Empty<OsmLine>(), (x, y) => (x, y) == hole));
        var nearWithHole = Decide(World(new[] { Road("near-120", 120.0) }, (x, y) => (x, y) == hole));
        Check(ref failures, near.Proximity == RoadProximity.Near && Math.Abs(near.DistanceM - 120.0) < 1.0 && near.PathQuery == "MAK_ROAD"
                            && near.RoadId == "near-120",
              FormattableString.Invariant($"(m8) a road 120 m beside the leg -> NEAR ({near.DistanceM:F1} m) -> MAK_ROAD, the nearest named"));
        Check(ref failures, far.Proximity == RoadProximity.Far && Math.Abs(far.DistanceM - 800.0) < 1.0 && far.PathQuery == "NONE",
              FormattableString.Invariant($"(m8) the nearest road 800 m away -> FAR ({far.DistanceM:F1} m, measured within twice the proximity) -> NONE"));
        Check(ref failures, none.Proximity == RoadProximity.Far && double.IsPositiveInfinity(none.DistanceM) && none.PathQuery == "NONE"
                            && AggregateMovePolicy.RoadLine("c", 1, 1, none).Contains("roads FAR (> 1000 m) -> NONE"),
              "(m8) no road within twice the proximity and every tile read -> FAR (> 1000 m) -> NONE");
        Check(ref failures, unknown.Proximity == RoadProximity.Unknown && unknown.PathQuery == "NONE" && unknown.UnknownTiles == 1,
              "(m8) no road on the readable tiles but ONE tile of the band unreadable -> UNKNOWN -> NONE (never 'no roads')");
        Check(ref failures, nearWithHole.Proximity == RoadProximity.Near,
              "(m8) a road found within the proximity decides NEAR even with an unreadable tile elsewhere in the band");
        Check(ref failures, AggregateMovePolicy.DecideRoads(AggregateMovePlanner.PerMemberOffRoad, null, null, prox).PathQuery == "NONE"
                            && AggregateMovePolicy.DecideRoads(AggregateMovePlanner.Group, null, null, prox).Proximity == RoadProximity.NotApplicable,
              "(m8) only Auto decides per leg: PerMemberOffRoad is NONE everywhere, Move (Group) carries useRoads instead");
        // THE BAND'S TILES ARE EXACT: every tile returned is within the band, a far tile of the box is not.
        var fT = OsmGeometry.Frame.At(a.Lat, a.Lon);
        bool allWithin = tiles.All(t =>
        {
            var nw = OsmTileMath.ToLatLon(OsmTileMath.Zoom, t.X, t.TmsY, 0, 0, 4096);
            var se = OsmTileMath.ToLatLon(OsmTileMath.Zoom, t.X, t.TmsY, 4096, 4096, 4096);
            var ring = new[] { fT.Xy(nw), fT.Xy((nw.Lat, se.Lon)), fT.Xy(se), fT.Xy((se.Lat, nw.Lon)), fT.Xy(nw) };
            return OsmGeometry.SegmentPolygon(fT.Xy(a), fT.Xy(b), new[] { ring }).D <= prox + OsmQuery.TileMarginM;
        });
        var mid = (Lat: (a.Lat + b.Lat) / 2, Lon: (a.Lon + b.Lon) / 2);
        var inTile = OsmQuery.TilesNear(mid, mid, 0.0);
        Check(ref failures, tiles.Count >= 2 && allWithin && inTile.Count >= 1 && inTile.Count <= 4
                            && tiles.Contains(OsmTileMath.TileOf(a.Lat, a.Lon)) && tiles.Contains(OsmTileMath.TileOf(b.Lat, b.Lon)),
              $"(m8) TilesNear: {tiles.Count} tile(s) for T14's leg 2 within 500 m, each one within the band, both end tiles included");
    }

    // ------------------------------------------------------------------------------ (m9) ----
    /// <summary>The AUTO decision for the cut-A legs of G1-2 on a real tile cache (printed; no pass/fail).</summary>
    public static void RealLegs(string cacheDir)
    {
        Console.WriteLine($"--- (m9) the AUTO decision on the tiles in {cacheDir} (osm-highways/), Vrf:RoadProximityMeters 500 ---");
        var reader = new OsmCacheReader(cacheDir);
        var (files, empty) = reader.Census(OsmSet.Highways);
        Console.WriteLine($"     osm-highways: {files} tile file(s), {empty} of them 0 bytes");
        double prox = AggregateMovePolicy.DefaultRoadProximityMeters;
        foreach (var (task, route) in CutALegs)
            for (int k = 1; k < route.Length; k++)
            {
                var inBand = OsmQuery.NearestRoad(reader.Provider, route[k - 1], route[k], prox);
                var wide = inBand.Found ? inBand : OsmQuery.NearestRoad(reader.Provider, route[k - 1], route[k], 2 * prox);
                var dcs = AggregateMovePolicy.DecideRoads(AggregateMovePlanner.Auto, inBand, wide, prox);
                double lenM = RouteExtentPolicy.GreatCircleMeters(route[k - 1].Lat, route[k - 1].Lon, route[k].Lat, route[k].Lon);
                Console.WriteLine(FormattableString.Invariant($"     {task} leg {k} ({lenM:F0} m): ") +
                                  AggregateMovePolicy.RoadLine(task, k, route.Length - 1, dcs) +
                                  FormattableString.Invariant($" {{tiles in the 500 m band: {inBand.KnownTiles} read, {inBand.UnknownTiles} unreadable}}"));
            }
    }

    // --------------------------------------------------------------------------- helpers ----
    private static List<PlannedMoveTracker.Member> Members(int n)
        => Enumerable.Range(1, n).Select(i => new PlannedMoveTracker.Member($"48_IBCT/28ID__FRIENDL.M{i}", $"VRF_UUID:m{i}")).ToList();

    /// <summary>The service's planned-move glue, replayed on the REAL policy, chain and step tracker: what ExecuteTaskOnTick's
    /// planned arm, IssuePlannedVertex, RoutePlannedMemberCompletion / CompletePlannedStep and M1's continuation issue, in
    /// their order. (m6) pins the service to this shape.</summary>
    private sealed class PlannedRecorder
    {
        private readonly AggregateMovePlanner _p;
        private readonly VertexChainTracker _chains = new(VertexChainPolicy.DefaultVertexArrivalRadiusMeters);
        private readonly PlannedMoveTracker _moves = new();
        private string _c = "";
        public readonly List<string> Issued = new();
        public VertexChainTracker.Outcome LastOutcome;

        public PlannedRecorder(AggregateMovePlanner p) => _p = p;

        public void Dispatch(string container, string taskUuid, IReadOnlyList<VertexChainTracker.Point> route,
                             IReadOnlyList<PlannedMoveTracker.Member> members)
        {
            _c = container;
            if (!AggregateMovePolicy.IsPlanned(_p))
            {
                Issued.Add($"CreateRoute({container},{route.Count}) -> {ContainerScripts.ForForm(GroundMoveForm.RouteTask, false)}");
                return;
            }
            var s = _chains.Start(container, "VRF_UUID:container", taskUuid, "taskee", "task", route[0], route.Skip(1).ToList(),
                                  AggregateMovePolicy.CompletionTaskType(_p), handLastVertexOn: true);
            var roads = Enumerable.Range(0, route.Count - 1)
                                  .Select(_ => new RoadDecision(_p == AggregateMovePlanner.Auto ? RoadProximity.Far : RoadProximity.NotApplicable,
                                                                "NONE", double.NaN, "", "", 500, 1000, 0)).ToList();
            _moves.Begin(new PlannedMoveTracker.Context(container, "VRF_UUID:container", s.Chain.Generation, _p,
                                                        AggregateMovePolicy.IsPerMember(_p) ? members : Array.Empty<PlannedMoveTracker.Member>(),
                                                        roads, route.Count - 1));
            Issue(s.Chain, 1, s.First);
        }

        private void Issue(VertexChainTracker.Snapshot chain, int vertex, VertexChainTracker.Point v)
        {
            _moves.TryGet(_c, out var ctx);
            var t = AggregateMovePolicy.ForVertex(_p, v.Lat, v.Lon, _moves.RoadFor(_c, vertex));
            string at = v.Lat.ToString("F4", CultureInfo.InvariantCulture);
            if (t.PerMember)
            {
                _moves.BeginVertex(_c, chain.Generation, vertex);
                foreach (var m in ctx.Members) Issued.Add($"RunScriptedTask({m.Uuid},{t.ScriptId},@{at})");
            }
            else Issued.Add($"RunScriptedTask({chain.VrfUuid},{t.ScriptId},@{at})");
        }

        /// <summary>A completion of the CONTAINER's chain (Move (Group)'s own, or a closed per-member step's).</summary>
        public VertexChainTracker.Outcome Complete(string type, bool success, VertexChainTracker.Point at)
        {
            var d = _chains.OnCompletion(_c, type, success, at, false);
            if (d.Outcome == VertexChainTracker.Outcome.Advance
                && _chains.TryBeginIssue(_c, d.Chain.Generation, d.NextVertex, out var next, out var snap))
                Issue(snap, d.NextVertex, next);
            if (d.Outcome is VertexChainTracker.Outcome.FinalVertex or VertexChainTracker.Outcome.Failed) _moves.End(_c);
            LastOutcome = d.Outcome;
            return d.Outcome;
        }

        /// <summary>A MEMBER's own completion: null until its step closes, then the chain's outcome.</summary>
        public VertexChainTracker.Outcome? CompleteMember(string member, bool success, VertexChainTracker.Point at)
        {
            var v = _moves.OnMemberCompleted(_c, member, AggregateMovePolicy.NavigateScript, success);
            if (!v.Consumed || !v.StepDone) return null;
            return Complete(AggregateMovePolicy.NavigateScript, v.Success, at);
        }
    }

    private static string VarType(string xml, string name)
    {
        var m = Regex.Match(xml, "&lt;myVariableName&gt;" + Regex.Escape(name) + "&lt;/myVariableName&gt;\\s*&lt;myType&gt;([^&]*)&lt;/myType&gt;");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static string VarDefault(string xml, string name)
    {
        var m = Regex.Match(xml, "&lt;myVariableName&gt;" + Regex.Escape(name) + "&lt;/myVariableName&gt;[\\s\\S]*?&lt;myDefaultValue&gt;([^&]*)&lt;/myDefaultValue&gt;");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static string EntityTypeCount(string xml)
    {
        var m = Regex.Match(xml, "<myEntityTypes>\\s*<count>(\\d+)</count>");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static string SafeRead(string p) { try { return File.Exists(p) ? File.ReadAllText(p) : ""; } catch { return ""; } }

    private static string ServiceSource()
    {
        string repo = FindRepoRoot();
        string p = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs");
        return p != null && File.Exists(p) ? File.ReadAllText(p) : "";
    }

    private static string ReturnAfter(string s, string label)
    {
        int at = s.IndexOf(label, StringComparison.Ordinal);
        if (at < 0) return "?";
        int t = s.IndexOf("return true;", at, StringComparison.Ordinal);
        int f = s.IndexOf("return false;", at, StringComparison.Ordinal);
        if (t < 0 && f < 0) return "?";
        return f < 0 || (t >= 0 && t < f) ? "true" : "false";
    }

    private static string Between(string s, string from, string to)
    {
        int a = s.IndexOf(from, StringComparison.Ordinal);
        if (a < 0) return "";
        int b = s.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        return b < 0 ? s.Substring(a) : s.Substring(a, b - a);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "data", "COA-STP1_Order.xml"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static void Check(ref int failures, bool ok, string label, string detail = "")
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}" + (ok || string.IsNullOrEmpty(detail) ? "" : " -- " + detail));
        if (!ok) failures++;
    }
}
