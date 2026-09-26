namespace VrfC2SimApp;

/// <summary>
/// RL-20260926-01 (owner decisions of 2026-09-26): what an ATTACK or a BREACH does, checked
/// offline. Runs inside `--rulings-selftest` (its own section), no bridge, no MAK.
///
///   ATTACK on a UNIT      -> advance to the objective + rules of engagement fire at will.
///                            NEVER a Fire At to a unit.
///   ATTACK by a PLATFORM  -> Fire At only when the order names a DISTINCT target; otherwise
///                            advance only (unchanged).
///   BREACH on anything    -> advance to the breach location + one observation that the breach
///                            is not simulated (STP-865). NEVER a DtBreachTask.
///
/// The decision is ONE pure function (TaskDispatchPolicy.ForEngage); the service only acts on
/// its answer. The last two groups read the repo on disk: the test orders that aim an ATTACK or
/// BREACH at another unit are retired, and nothing that launches a run still points at them.
/// </summary>
public static class EngageDoctrineSelfTest
{
    /// <summary>The header every retired order carries (RL-20260926-01).</summary>
    public const string RetiredMarker = "NOT DOCTRINAL - retired 2026-09-26 (RL-20260926-01)";

    public static int Run()
    {
        int failures = 0;
        var targets = Enum.GetValues<TargetResolution>();
        string repo = FindRepoRoot();
        string service = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs");

        // (e1) ATTACK ON A UNIT: advance + fire at will, whatever the order names as the target.
        //      STP names the performer itself (SelfIsObjective) on every task; a distinct target
        //      does not change the answer - a unit never takes a Fire At.
        foreach (var t in targets)
        {
            var d = TaskDispatchPolicy.ForEngage(TaskIntent.Attack, performerIsUnit: true, t);
            Check(ref failures, d == EngageDecision.AdvanceFireAtWill,
                  $"(e1) ATTACK on a UNIT, target {t} -> advance + rules of engagement fire at will (got {d})");
        }
        Check(ref failures,
              targets.All(t => !TaskDispatchPolicy.IssuesFireAt(
                  TaskDispatchPolicy.ForEngage(TaskIntent.Attack, performerIsUnit: true, t))),
              "(e1) ... and NO target resolution sends a Fire At to a unit");
        Check(ref failures,
              TaskDispatchPolicy.SetsFireAtWill(EngageDecision.AdvanceFireAtWill)
              && !TaskDispatchPolicy.SetsFireAtWill(EngageDecision.AdvanceThenFireAt)
              && !TaskDispatchPolicy.SetsFireAtWill(EngageDecision.AdvanceOnly)
              && !TaskDispatchPolicy.SetsFireAtWill(EngageDecision.AdvanceBreachNotSimulated)
              && !TaskDispatchPolicy.SetsFireAtWill(EngageDecision.NotEngageVerb),
              "(e1) only the unit ATTACK overrides the rules of engagement to fire at will");

        // (e2) ATTACK BY A PLATFORM: Fire At stays, but only at a DISTINCT target.
        Check(ref failures,
              TaskDispatchPolicy.ForEngage(TaskIntent.Attack, performerIsUnit: false, TargetResolution.DistinctEntity)
                  == EngageDecision.AdvanceThenFireAt
              && TaskDispatchPolicy.IssuesFireAt(EngageDecision.AdvanceThenFireAt),
              "(e2) ATTACK by a PLATFORM with a distinct target -> advance, then Fire At that target");
        foreach (var t in new[] { TargetResolution.SelfIsObjective, TargetResolution.Unresolved, TargetResolution.NoTarget })
        {
            var d = TaskDispatchPolicy.ForEngage(TaskIntent.Attack, performerIsUnit: false, t);
            Check(ref failures, d == EngageDecision.AdvanceOnly,
                  $"(e2) ATTACK by a PLATFORM, target {t} -> advance only, unchanged (got {d})");
        }

        // (e3) BREACH ON ANYTHING: advance + the not-simulated observation; never a breach task.
        foreach (bool unit in new[] { true, false })
            foreach (var t in targets)
            {
                var d = TaskDispatchPolicy.ForEngage(TaskIntent.Breach, unit, t);
                Check(ref failures, d == EngageDecision.AdvanceBreachNotSimulated && !TaskDispatchPolicy.IssuesFireAt(d),
                      $"(e3) BREACH, performer {(unit ? "unit" : "platform")}, target {t} -> advance + " +
                      $"'breach not simulated' observation, no vendor breach and no Fire At (got {d})");
            }

        // (e4) Every other intent is outside this decision.
        foreach (var i in Enum.GetValues<TaskIntent>().Where(i => i != TaskIntent.Attack && i != TaskIntent.Breach
                                                                  && i != TaskIntent.FollowAndSupport
                                                                  && i != TaskIntent.PassageOfLines))
            Check(ref failures,
                  TaskDispatchPolicy.ForEngage(i, true, TargetResolution.DistinctEntity) == EngageDecision.NotEngageVerb
                  && TaskDispatchPolicy.ForEngage(i, false, TargetResolution.DistinctEntity) == EngageDecision.NotEngageVerb,
                  $"(e4) intent {i} is not an ATTACK/BREACH decision");

        // (e5) STP's attack variants are ATTACKs (C2SimTask.cs :618-632 emits ATTMN / ATTSPT;
        //      both are TaskActionCodeType members, C2SIM_SMX_LOX_CWIX2024.xsd:3863, :3865).
        foreach (var code in new[] { "ATTACK", "ATTMN", "ATTSPT" })
        {
            var v = VerbMapping.Classify(code);
            Check(ref failures, v.Recognized && v.Intent == TaskIntent.Attack,
                  $"(e5) {code} classifies as a recognised ATTACK (got {v.Intent}, recognised={v.Recognized})");
        }

        // (e11) FOLLOW AND SUPPORT / FOLLOW AND ASSUME (FOLSPT, FOLASS; xsd:3963-3964; STP emits them,
        //       docs/STP_TASK_VOCABULARY_2026-09-03.md:36). Coordinator addition to RL-20260926-01's unit:
        //       advance along the task's graphic to its end and HOLD there; no engagement; the order's
        //       ROE unchanged; completion by the time rules. STP does not carry the supported unit
        //       (AffectedEntity = the performer), so there is nothing to follow - only the graphic.
        foreach (var code in new[] { "FOLSPT", "FOLASS" })
        {
            var v = VerbMapping.Classify(code);
            Check(ref failures, v.Recognized && v.Implemented && v.Intent == TaskIntent.FollowAndSupport,
                  $"(e11) {code} classifies as a recognised, implemented FollowAndSupport (got {v.Intent}, " +
                  $"recognised={v.Recognized}, implemented={v.Implemented})");
            Check(ref failures, v.Composition.Contains("hold") && v.Composition.Contains("no engagement"),
                  $"(e11) {code}'s composition says advance-and-hold with no engagement ('{v.Composition}')");
        }
        foreach (bool unit in new[] { true, false })
            foreach (var t in targets)
            {
                var d = TaskDispatchPolicy.ForEngage(TaskIntent.FollowAndSupport, unit, t);
                Check(ref failures,
                      d == EngageDecision.AdvanceAndHold && !TaskDispatchPolicy.IssuesFireAt(d)
                      && !TaskDispatchPolicy.SetsFireAtWill(d),
                      $"(e11) FOLSPT/FOLASS, performer {(unit ? "unit" : "platform")}, target {t} -> advance and " +
                      $"hold, never an engage, ROE left as ordered (got {d})");
            }

        // (e12) CNFPSL - conduct forward passage of lines (xsd:3899; STP's own passage-of-lines code,
        //       docs/STP_TASK_VOCABULARY_2026-09-03.md:36). Coordinator's direction 2026-09-26 (lane E2):
        //       move along the task's route/graphic through the passage lanes to its end, then hold; no
        //       engagement; ROE as ordered; completion by the time rules.
        {
            var v = VerbMapping.Classify("CNFPSL");
            Check(ref failures, v.Recognized && v.Implemented && v.Intent == TaskIntent.PassageOfLines,
                  $"(e12) CNFPSL classifies as a recognised, implemented PassageOfLines (got {v.Intent}, " +
                  $"recognised={v.Recognized}, implemented={v.Implemented})");
            Check(ref failures, v.Composition.Contains("hold") && v.Composition.Contains("no engagement"),
                  $"(e12) CNFPSL's composition says advance-and-hold with no engagement ('{v.Composition}')");
            foreach (bool unit in new[] { true, false })
                foreach (var t in targets)
                {
                    var d = TaskDispatchPolicy.ForEngage(TaskIntent.PassageOfLines, unit, t);
                    Check(ref failures, d == EngageDecision.AdvanceAndHold && !TaskDispatchPolicy.IssuesFireAt(d)
                                        && !TaskDispatchPolicy.SetsFireAtWill(d),
                          $"(e12) CNFPSL, performer {(unit ? "unit" : "platform")}, target {t} -> advance and hold, " +
                          $"never an engage, ROE as ordered (got {d})");
                }
        }

        // (e13) ONE ROE PER DISPATCH, FROM ONE FUNCTION (lane E2 S2). Only a unit ATTACK overrides the
        //       order; every other dispatch - including the in-place and hold-in-place ones, which set no
        //       ROE before - gets the order's own ROE, so a unit's in-place follow-on after an ATTACK does
        //       not keep fire at will against its own hold-fire.
        Check(ref failures,
              TaskDispatchPolicy.RoeFor(EngageDecision.AdvanceFireAtWill, "ROEHold") == RoeChoice.FireAtWill
              && TaskDispatchPolicy.RoeFor(EngageDecision.AdvanceFireAtWill, "") == RoeChoice.FireAtWill,
              "(e13) a unit ATTACK is fire at will whatever the order's ROE (RL-20260926-01 A4)");
        foreach (var d in Enum.GetValues<EngageDecision>().Where(x => x != EngageDecision.AdvanceFireAtWill))
            Check(ref failures,
                  TaskDispatchPolicy.RoeFor(d, "ROEHold") == RoeChoice.HoldFire
                  && TaskDispatchPolicy.RoeFor(d, "ROEFree") == RoeChoice.FireAtWill
                  && TaskDispatchPolicy.RoeFor(d, "ROETight") == RoeChoice.FireWhenFiredUpon
                  && TaskDispatchPolicy.RoeFor(d, "") == RoeChoice.FireWhenFiredUpon,
                  $"(e13) {d}: the order's ROE as sent (ROEHold -> hold fire, ROEFree -> fire at will, else " +
                  "fire when fired upon)");
        if (service != null && File.Exists(service))
        {
            string src = File.ReadAllText(service);
            int roeCalls = CountOf(src, "_bridge.SetRulesOfEngagement(");
            int viaPolicy = CountOf(src, "SetRulesOfEngagement(vrfUuid, ToRoe(TaskDispatchPolicy.RoeFor(");
            Check(ref failures, viaPolicy == 3 && roeCalls == 4,
                  $"(e13) the service sets ROE through RoeFor on the in-place, hold-in-place and committed dispatch " +
                  $"paths ({viaPolicy} of 3) and nowhere else but ESCRT ({roeCalls} calls, expected 4)");
        }

        // (e6) THE WORDS. The log line and the STP-facing observation are part of the contract.
        Check(ref failures,
              TaskDispatchPolicy.AttackFireAtWillLine ==
              "ATTACK: advancing to the objective; rules of engagement set to fire at will - members " +
              "engage enemies they encounter (RL-20260926-01)",
              "(e6) the unit ATTACK log line is the ruled plain sentence");
        Check(ref failures,
              TaskDispatchPolicy.BreachNotSimulatedObservation("1-1/2/1_AD") ==
              "BREACH by 1-1/2/1_AD: the breach action is not simulated - the order carries no obstacle, " +
              "lane or breach assets (STP-865); the unit advances to the breach location and the task " +
              "completes by time.",
              "(e6) the BREACH observation names the unit, says the breach is not simulated and cites STP-865");
        {
            string xml = ReportBuilder.BuildTypeSubstitutionReport(
                "de16a337-b2a6-c029-07b5-869191631621", "1-1/2/1_AD", "1-1/2/1_AD",
                TaskDispatchPolicy.BreachNotSimulatedObservation("1-1/2/1_AD"),
                "2026-09-26T00:00:00Z", "11111111-2222-3333-4444-555555555555");
            Check(ref failures,
                  xml.Contains("ObservationReportContent") && xml.Contains("NameObservation")
                  && xml.Contains("the breach action is not simulated") && xml.Contains("STP-865")
                  && xml.Contains("de16a337-b2a6-c029-07b5-869191631621"),
                  "(e6) ... and it travels on the existing observation path (ObservationReport / NameObservation)");
        }

        // (e7) NO GEOMETRY. Only a platform Fire At engages in place; a unit ATTACK and every
        //      BREACH execute in place like any other task with no geometry (R2).
        Check(ref failures,
              TaskDispatchPolicy.ForZeroGeometry(true, TaskDispatchPolicy.IssuesFireAt(EngageDecision.AdvanceThenFireAt))
                  == ZeroGeometryAction.EngageInPlace
              && TaskDispatchPolicy.ForZeroGeometry(true, TaskDispatchPolicy.IssuesFireAt(EngageDecision.AdvanceFireAtWill))
                  == ZeroGeometryAction.ExecuteInPlace
              && TaskDispatchPolicy.ForZeroGeometry(true, TaskDispatchPolicy.IssuesFireAt(EngageDecision.AdvanceBreachNotSimulated))
                  == ZeroGeometryAction.ExecuteInPlace,
              "(e7) no geometry: platform Fire At engages in place; unit ATTACK and BREACH execute in place");
        Check(ref failures,
              !Enum.GetNames<ZeroGeometryAction>().Contains("BreachInPlace"),
              "(e7) there is no 'breach in place' outcome any more");

        // (e8) THE SERVICE ISSUES NO BREACH TASK. A source tripwire: no call to the bridge's Breach
        //      is left in the dispatch code (the entity-level SMS has no breach controller -
        //      research_attack_engage: DtBreachTask is aggregate-level, UG52 35.3.1 p719).
        Check(ref failures, service != null && File.Exists(service), $"(e8) the service source is on disk ({service})");
        if (service != null && File.Exists(service))
        {
            string src = File.ReadAllText(service);
            int breachCalls = CountOf(src, "_bridge.Breach(");
            Check(ref failures, breachCalls == 0,
                  $"(e8) VrfC2SimService.cs calls _bridge.Breach( {breachCalls} time(s) - expected 0");
            Check(ref failures, src.Contains("TaskDispatchPolicy.ForEngage("),
                  "(e8) the service takes its ATTACK/BREACH decision from TaskDispatchPolicy.ForEngage");
        }

        // (e9) THE REAL ORDERS. Every ATTACK-family and BREACH task in the STP exports on disk gets
        //      a decision that sends no Fire At to a unit and no breach at all.
        if (repo != null)
        {
            int attacks = 0, breaches = 0, bad = 0;
            foreach (string name in new[] { "COA-STP1_Order.xml", "IRONSTORM_CUTA_Order.xml", "STP-IRON-STORM-SYNTHETIC_Order.xml" })
            {
                string f = Path.Combine(repo, "data", name);
                if (!File.Exists(f)) { bad++; continue; }
                foreach (var t in OrderParser.Parse(File.ReadAllText(f)).Tasks)
                {
                    var intent = VerbMapping.Classify(t.ActionCode).Intent;
                    if (intent != TaskIntent.Attack && intent != TaskIntent.Breach) continue;
                    if (intent == TaskIntent.Attack) attacks++; else breaches++;
                    bool self = string.Equals(t.AffectedEntity, t.TaskeeUuid, StringComparison.Ordinal);
                    var res = TaskDispatchPolicy.ForTarget(!string.IsNullOrEmpty(t.AffectedEntity), true, self);
                    var d = TaskDispatchPolicy.ForEngage(intent, performerIsUnit: true, res);
                    if (TaskDispatchPolicy.IssuesFireAt(d)
                        || (intent == TaskIntent.Breach && d != EngageDecision.AdvanceBreachNotSimulated)) bad++;
                }
            }
            Check(ref failures, bad == 0 && attacks > 0 && breaches > 0,
                  $"(e9) the STP exports on disk: {attacks} ATTACK-family and {breaches} BREACH task(s), " +
                  $"{bad} would send a Fire At to a unit or a breach task (or a file is missing)");
        }

        // (e10) THE RETIRED TEST ORDERS. Every order in data\ and docs\golden-trace\orders\ that
        //       aims an ATTACK-family or BREACH task at an entity other than its performer carries
        //       the retirement header, and the header holds no "<Task" (the SDK sniffs for it).
        if (repo != null)
        {
            var retired = RetiredOrderPaths(repo);
            var missing = new List<string>();
            var sniffable = new List<string>();
            foreach (string f in UnitTargetedOrders(repo))
            {
                string text = File.ReadAllText(f);
                string rel = Path.GetRelativePath(repo, f);
                if (!text.Contains(RetiredMarker)) missing.Add(rel);
                int at = text.IndexOf(RetiredMarker, StringComparison.Ordinal);
                if (at >= 0)
                {
                    int open = text.LastIndexOf("<!--", at, StringComparison.Ordinal);
                    int close = text.IndexOf("-->", at, StringComparison.Ordinal);
                    if (open < 0 || close < 0 || text.Substring(open, close - open).Contains("<Task"))
                        sniffable.Add(rel);
                }
            }
            Check(ref failures, missing.Count == 0,
                  $"(e10) every unit-targeted ATTACK/BREACH order carries '{RetiredMarker}' " +
                  $"(missing: {(missing.Count == 0 ? "none" : string.Join(", ", missing))})");
            Check(ref failures, sniffable.Count == 0,
                  "(e10) ... inside an XML comment that holds no '<Task' token");
            Check(ref failures, retired.Count >= 2,
                  $"(e10) the two known orders are among them ({retired.Count} found)");

            // Nothing that starts a run points at a retired order.
            var users = new List<string>();
            foreach (string dir in new[] { "scripts", "tools", "tests", "config", Path.Combine("src", "VrfC2SimApp") })
            {
                string root = Path.Combine(repo, dir);
                if (!Directory.Exists(root)) continue;
                foreach (string f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext is not (".ps1" or ".psm1" or ".sh" or ".json" or ".cs" or ".py" or ".cmd" or ".bat")) continue;
                    if (f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                        || f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                        || f.EndsWith("EngageDoctrineSelfTest.cs", StringComparison.Ordinal)) continue;
                    string text = File.ReadAllText(f);
                    foreach (string r in retired)
                        if (text.Contains(Path.GetFileName(r) + "\"") || text.Contains(Path.GetFileName(r) + "'")
                            || text.Contains("\\" + Path.GetFileName(r)) || text.Contains("/" + Path.GetFileName(r)))
                            users.Add($"{Path.GetRelativePath(repo, f)} -> {Path.GetFileName(r)}");
                }
            }
            Check(ref failures, users.Count == 0,
                  $"(e10) no runner, tool, test or setting uses a retired order as a path " +
                  $"({(users.Count == 0 ? "none" : string.Join("; ", users))})");
        }

        return failures;
    }

    /// <summary>Orders in data\ and docs\golden-trace\orders\ with an ATTACK-family or BREACH task
    /// whose AffectedEntity is set and is not its PerformingEntity.</summary>
    public static List<string> UnitTargetedOrders(string repo)
    {
        var hits = new List<string>();
        foreach (string dir in new[] { Path.Combine(repo, "data"), Path.Combine(repo, "docs", "golden-trace", "orders") })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (string f in Directory.EnumerateFiles(dir, "*.xml").OrderBy(p => p, StringComparer.Ordinal))
            {
                string text = File.ReadAllText(f);
                if (!text.Contains("OrderBody")) continue;
                OrderData order;
                try { order = OrderParser.Parse(text); } catch { continue; }
                bool hit = order.Tasks.Any(t =>
                {
                    var i = VerbMapping.Classify(t.ActionCode).Intent;
                    return (i == TaskIntent.Attack || i == TaskIntent.Breach)
                           && !string.IsNullOrEmpty(t.AffectedEntity)
                           && !string.Equals(t.AffectedEntity, t.TaskeeUuid, StringComparison.Ordinal);
                });
                if (hit) hits.Add(f);
            }
        }
        return hits;
    }

    private static List<string> RetiredOrderPaths(string repo)
        => UnitTargetedOrders(repo).Where(f => File.ReadAllText(f).Contains(RetiredMarker)).ToList();

    private static int CountOf(string s, string needle)
    {
        int n = 0;
        for (int i = s.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = s.IndexOf(needle, i + 1, StringComparison.Ordinal)) n++;
        return n;
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

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
