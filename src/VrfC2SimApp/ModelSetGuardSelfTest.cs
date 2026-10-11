using System.Text.RegularExpressions;

namespace VrfC2SimApp;

/// <summary>
/// D2b - THE MODEL-SET RULE INSIDE THE APP (RL-20260927-06, RL-20260928-01). A section of `--rulings-selftest`; no
/// bridge, no MAK, no clock. Every order and init below is a COMMITTED data file, and the expected outcomes are the
/// ones tests/RunnerTurnaround.Tests.ps1 sec 8z pins for the runner's rule on the same files - so the two rules are
/// held to the same answers, not only to the same table.
///
///   (g1) THE TABLE: EchelonLadder equals scripts/RunnerLib.ps1's ladder, threshold, aliases and unranked codes (read
///        from the script itself), with a DIRTY control that proves the comparison can fail.
///   (g2) THE RANK: Get-EchelonRank's semantics (order, groupings, case, SQD/SEC, NOS/NKN/air/naval, non-schema, missing).
///   (g3) THE INIT READ: the raw EchelonCode, and why it is raw - the typed parse turns an absent code into "AG".
///   (g4)-(g11) THE BRANCHES: REFUSED (above BN on EntityLevel), allowed on the aggregate set, the OVERRIDE UP (BN and
///        below on aggregate), BN and below on EntityLevel; only the TASKED units count; NOS/missing count below BN and
///        are named; a hostile formation counts like a friendly one.
///   (g12) THE START-UP LINE. (g13) THE SOURCE GUARD: the check sits on the order path before anything of the order is
///        registered or dispatched, and the refusal sends TASKABRT and returns.
/// </summary>
public static class ModelSetGuardSelfTest
{
    private const string CutAOrder = "IRONSTORM_CUTA_Order.xml";
    private const string CutAInit = "IRONSTORM_CUTA_Initialization.xml";
    private const string IronStormInit = "STP-IRON-STORM-SYNTHETIC_Initialization.xml";
    private const string R9Order = "R9_Mojave_UnitMove_Order.xml";
    private const string R9Init = "R9_Mojave_Lean_Initialization.xml";
    private const string Div28 = "28ID__FRIENDLY_INFANTRY_DIVISION";
    private const string Bde48 = "48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE";
    private const string Bn1112Uuid = "8d5b2ba6-73c1-6c55-812c-7c8078ea8c97";
    private const string Bde105Uuid = "a70d50f9-1151-275d-a298-e006f2519715";
    private const string Bde364NosUuid = "43cc164f-9c49-875b-90b4-d86a206606d6";

    public static int Run()
    {
        int failures = 0;
        string repo = FindRepoRoot();
        Check(ref failures, repo != null, "(g0) the repository is found above the executable (data/COA-STP1_Order.xml)");
        if (repo == null) return failures;
        Table(ref failures, repo);
        RankSemantics(ref failures);
        InitRead(ref failures, repo);
        Branches(ref failures, repo);
        Wiring(ref failures, repo);
        return failures;
    }

    // ------------------------------------------------------------------------------ (g1) ----
    private sealed record RunnerLadder(List<(string Code, int Rank)> Ladder, int Threshold,
                                       List<(string Code, string ReadAs)> Aliases, List<(string Code, string Note)> Unranked);

    /// <summary>scripts/RunnerLib.ps1's four tables, read from the script text; null when a block is not found.</summary>
    private static RunnerLadder ParseRunnerLadder(string lib)
    {
        string ladder = Block(lib, "$script:EchelonLadder = [ordered]@{");
        string unranked = Block(lib, "$script:EchelonUnranked = [ordered]@{");
        var th = Regex.Match(lib, @"\$script:EchelonBattalionRank\s*=\s*(\d+)");
        var al = Regex.Match(lib, @"\$script:EchelonAliases\s*=\s*@\{([^\r\n}]*)\}");
        if (ladder == null || unranked == null || !th.Success || !al.Success) return null;
        return new RunnerLadder(
            Regex.Matches(ladder, @"'([A-Z]+)'\s*=\s*(\d+)").Select(m => (m.Groups[1].Value, int.Parse(m.Groups[2].Value))).ToList(),
            int.Parse(th.Groups[1].Value),
            Regex.Matches(al.Groups[1].Value, @"'([A-Z]+)'\s*=\s*'([A-Z]+)'").Select(m => (m.Groups[1].Value, m.Groups[2].Value)).ToList(),
            Regex.Matches(unranked, @"'([A-Z]+)'\s*=\s*'((?:[^']|'')*)'")
                 .Select(m => (m.Groups[1].Value, m.Groups[2].Value.Replace("''", "'"))).ToList());
    }

    /// <summary>The body of a PowerShell hashtable literal that opens at <paramref name="head"/> and closes at a line that
    /// is only "}".</summary>
    private static string Block(string text, string head)
    {
        int a = text.IndexOf(head, StringComparison.Ordinal);
        if (a < 0) return null;
        var close = Regex.Match(text.Substring(a), @"\r?\n\}\s*(\r?\n|$)");
        return close.Success ? text.Substring(a, close.Index) : null;
    }

    private static List<string> Mismatches(RunnerLadder ps)
    {
        var bad = new List<string>();
        if (ps == null) { bad.Add("RunnerLib's tables were not found"); return bad; }
        string Pairs<T>(IEnumerable<(string, T)> xs) => string.Join(",", xs.OrderBy(x => x.Item1, StringComparer.Ordinal)
                                                                              .Select(x => x.Item1 + "=" + x.Item2));
        if (Pairs(ps.Ladder) != Pairs(EchelonLadder.Ladder.Select(e => (e.Code, e.Rank))))
            bad.Add($"ladder: RunnerLib [{Pairs(ps.Ladder)}] vs C# [{Pairs(EchelonLadder.Ladder.Select(e => (e.Code, e.Rank)))}]");
        if (ps.Threshold != EchelonLadder.BattalionRank) bad.Add($"threshold: RunnerLib {ps.Threshold} vs C# {EchelonLadder.BattalionRank}");
        if (Pairs(ps.Aliases) != Pairs(EchelonLadder.Aliases.Select(e => (e.Code, e.ReadAs))))
            bad.Add($"aliases: RunnerLib [{Pairs(ps.Aliases)}] vs C# [{Pairs(EchelonLadder.Aliases.Select(e => (e.Code, e.ReadAs)))}]");
        if (Pairs(ps.Unranked) != Pairs(EchelonLadder.Unranked.Select(e => (e.Code, e.Note))))
            bad.Add("unranked codes or notes differ");
        return bad;
    }

    private static void Table(ref int failures, string repo)
    {
        string libPath = Path.Combine(repo, "scripts", "RunnerLib.ps1");
        string lib = File.Exists(libPath) ? File.ReadAllText(libPath) : "";
        var ps = ParseRunnerLadder(lib);
        Check(ref failures, ps != null && ps.Ladder.Count == 17 && ps.Unranked.Count == 11 && ps.Aliases.Count == 2,
              "(g1) scripts/RunnerLib.ps1's four tables are read from the script: 17 ladder codes, the threshold, 2 aliases, " +
              "11 unranked codes", ps == null ? "not found" : $"{ps.Ladder.Count} / {ps.Threshold} / {ps.Aliases.Count} / {ps.Unranked.Count}");
        var bad = Mismatches(ps);
        Check(ref failures, bad.Count == 0,
              "(g1) THE SAME TABLE: EchelonLadder (ModelSetGuard.cs) equals RunnerLib's $script:EchelonLadder, " +
              "$script:EchelonBattalionRank (BN = 6), $script:EchelonAliases and $script:EchelonUnranked - codes, ranks and " +
              "notes (RunnerTurnaround 8z holds the same from the other side)", string.Join(" | ", bad));
        // DIRTY CONTROLS: the same comparison on RunnerLib text with ONE rank moved, and with ONE code added, must fail.
        var movedBn = ParseRunnerLadder(Regex.Replace(lib, @"'BN' = 6;", "'BN' = 7;"));
        var extra = ParseRunnerLadder(lib.Replace("'RGT' = 7", "'RGT' = 7; 'XYZ' = 14"));
        var movedTh = ParseRunnerLadder(Regex.Replace(lib, @"\$script:EchelonBattalionRank = 6", "$script:EchelonBattalionRank = 7"));
        Check(ref failures, Mismatches(movedBn).Count > 0 && Mismatches(extra).Count > 0 && Mismatches(movedTh).Count > 0,
              "(g1) DIRTY CONTROL: RunnerLib text with 'BN' = 7, with an extra code, or with the threshold at 7 is REPORTED " +
              "as a mismatch - the equality check can fail");
    }

    // ------------------------------------------------------------------------------ (g2) ----
    private static void RankSemantics(ref int failures)
    {
        var ladder = new[] { "TEAM", "SQUAD", "SECT", "PLT", "COY", "BN", "RGT", "BDE", "DIV", "CORPS", "ARMY", "AG", "REGION" };
        var ranks = ladder.Select(c => EchelonLadder.Rank(c).Rank).ToArray();
        bool increasing = ranks[0] > 0;
        for (int i = 1; i < ranks.Length; i++) increasing &= ranks[i] > ranks[i - 1];
        Check(ref failures, increasing, "(g2) TEAM<SQUAD<SECT<PLT<COY<BN<RGT<BDE<DIV<CORPS<ARMY<AG<REGION, strictly increasing",
              string.Join(",", ranks));
        Check(ref failures, EchelonLadder.Rank("COYG").Rank == EchelonLadder.Rank("COY").Rank
                            && EchelonLadder.Rank("BNG").Rank == EchelonLadder.BattalionRank
                            && EchelonLadder.Rank("BATGRP").Rank == EchelonLadder.BattalionRank
                            && EchelonLadder.Rank("BDEGRP").Rank == EchelonLadder.Rank("BDE").Rank
                            && EchelonLadder.Rank("BN").Rank == EchelonLadder.BattalionRank,
              "(g2) the grouping codes rank with their base unit (COYG=COY, BNG=BATGRP=BN, BDEGRP=BDE); BN is the threshold");
        Check(ref failures, EchelonLadder.Rank("bde").Canonical == "BDE" && EchelonLadder.Rank(" div ").Rank == 9
                            && EchelonLadder.Rank("SQD").Canonical == "SQUAD" && EchelonLadder.Rank("SEC").Canonical == "SECT"
                            && EchelonLadder.Rank("SQD").Note.Contains("not a schema value"),
              "(g2) case and blanks do not matter; SQD/SEC (not schema values) read as SQUAD/SECT and SAY so");
        Check(ref failures, new[] { "NOS", "NKN", "WING", "FLEET" }.All(c => EchelonLadder.Rank(c) is { Status: "unranked", Rank: 0 })
                            && EchelonLadder.Rank("XYZ").Status == "not-schema" && EchelonLadder.Rank("").Status == "missing"
                            && EchelonLadder.Rank(null).Status == "missing" && EchelonLadder.Rank("NOS").Note.EndsWith("counted below BN"),
              "(g2) NOS, NKN, air (WING) and naval (FLEET) are UNRANKED; a non-schema code and a missing one are named as such");
    }

    // ------------------------------------------------------------------------------ (g3) ----
    private static void InitRead(ref int failures, string repo)
    {
        string cutAText = ReadData(repo, CutAInit);
        var cutA = ModelSetGuard.ReadInit(cutAText);
        string Code(InitEchelons e, string name) => e.Units.Values.FirstOrDefault(u => u.Name == name)?.EchelonCode;
        Check(ref failures, cutA.Parsed && cutA.Units.Count == 40 && Code(cutA, Div28) == "DIV" && Code(cutA, Bde48) == "BDE"
                            && Code(cutA, "1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE") == "BN",
              "(g3) the raw read of the cut-A init: 40 Units, 28ID DIV, 48 IBCT BDE, 1-112 IN BN",
              $"parsed {cutA.Parsed}, units {cutA.Units.Count}, 28ID {Code(cutA, Div28)}, 48 IBCT {Code(cutA, Bde48)}");
        string stripped = Regex.Replace(cutAText, "<EchelonCode>[^<]*</EchelonCode>", "");
        var raw = ModelSetGuard.ReadInit(stripped);
        var typed = InitParser.Parse(stripped);
        Check(ref failures, raw.Units.Count == 40 && raw.Units.Values.All(u => u.EchelonCode == "")
                            && typed.Units.Count == 40 && typed.Units.All(u => u.EchelonCode == "AG"),
              "(g3) WHY THE READ IS RAW: with every EchelonCode removed the raw read says MISSING for all 40 (counted below BN, " +
              "the runner's rule) while the typed parse says \"AG\" for all 40 - the enum's first member, rank 12, which " +
              "would REFUSE a BN-and-below order (InitParser.cs CAVEAT)",
              $"raw '' x{raw.Units.Values.Count(u => u.EchelonCode == "")}, typed AG x{typed.Units.Count(u => u.EchelonCode == "AG")}");
        var bad = ModelSetGuard.ReadInit("<MessageBody><Unit>");
        var empty = ModelSetGuard.ReadInit("  ");
        Check(ref failures, !bad.Parsed && bad.Error.Length > 0 && !empty.Parsed && bad.Units.Count == 0,
              "(g3) an unparseable or empty init is Parsed=false with a reason - never a throw");
    }

    // --------------------------------------------------------------------------- (g4)-(g11) ----
    private static void Branches(ref int failures, string repo)
    {
        var cutA = ModelSetGuard.ReadInit(ReadData(repo, CutAInit));
        var ironStorm = ModelSetGuard.ReadInit(ReadData(repo, IronStormInit));
        var cutATasks = Performers(repo, CutAOrder);

        // (g4) FAIL-FIRST ARM 1 - the downward case the owner ruled out: cut A on EntityLevel.
        var refused = ModelSetGuard.Decide(false, "EntityLevel", cutATasks, cutA.Units, cutA.Other);
        Check(ref failures, refused.Kind == ModelSetCheckKind.Refused && refused.Highest == "DIV" && refused.TaskCount == 5
                            && refused.Performers.Count == 3,
              "(g4) REFUSED: cut A (28ID DIV, 48 IBCT BDE, 1-112 IN BN tasked; 5 tasks) on Vrf:ModelSet=EntityLevel - " +
              "above battalion is aggregate-only (RL-20260927-06), no downward override (RL-20260928-01)",
              $"kind {refused.Kind}, highest {refused.Highest}, tasks {refused.TaskCount}, performers {refused.Performers.Count}");
        Check(ref failures, string.Join(" | ", refused.AboveBattalionUnits) == $"{Div28} (DIV) | {Bde48} (BDE)",
              "(g4) ... the units above BN are exactly 28ID (DIV) and 48 IBCT (BDE) - the BN is not one of them",
              string.Join(" | ", refused.AboveBattalionUnits));
        Check(ref failures, refused.Line.StartsWith("MODEL SET RULE (D2b): ORDER REFUSED - ABOVE BATTALION IS AGGREGATE-ONLY", StringComparison.Ordinal)
                            && refused.Line.Contains("RL-20260927-06") && refused.Line.Contains("RL-20260928-01")
                            && refused.Line.Contains("highest TASKED echelon DIV (" + Div28 + ")")
                            && refused.Line.Contains(Bde48 + " (BDE)") && refused.Line.Contains("Vrf:ModelSet=EntityLevel")
                            && refused.Line.Contains("TASKABRT") && !refused.Line.Contains("1-112_IN") && !refused.Warn,
              "(g4) ... ONE line names the rule, the echelon, the performers and both ruling ids", refused.Line);
        Check(ref failures, refused.AbortReason.StartsWith("REFUSED (D2b): ABOVE BATTALION IS AGGREGATE-ONLY", StringComparison.Ordinal)
                            && refused.AbortReason.Contains(ModelSetGuard.RuleIds) && refused.AbortReason.Contains(Div28),
              "(g4) ... and the TASKABRT reason every task of the order carries says the same", refused.AbortReason);
        Console.WriteLine("        the ERROR line, verbatim: " + refused.Line);
        Console.WriteLine("        the TASKABRT reason, verbatim: " + refused.AbortReason);
        var refusedUnset = ModelSetGuard.Decide(false, null, cutATasks, cutA.Units, cutA.Other);
        var refusedTypo = ModelSetGuard.Decide(false, "Aggregate", cutATasks, cutA.Units, cutA.Other);
        Check(ref failures, refusedUnset.Kind == ModelSetCheckKind.Refused && refusedUnset.Line.Contains("Vrf:ModelSet=(not set) -> EntityLevel")
                            && refusedTypo.Kind == ModelSetCheckKind.Refused && refusedTypo.Line.Contains("Vrf:ModelSet=Aggregate -> EntityLevel"),
              "(g4) an UNSET or unrecognised Vrf:ModelSet is EntityLevel (UnitPositionPolicy) - refused too, and the line says " +
              "what was configured");
        var refusedIs = ModelSetGuard.Decide(false, "EntityLevel", cutATasks, ironStorm.Units, ironStorm.Other);
        Check(ref failures, refusedIs.Kind == ModelSetCheckKind.Refused && refusedIs.Highest == "DIV",
              "(g4) the same order on the full Iron Storm init (STP-IRON-STORM-SYNTHETIC) is refused the same way");

        // (g5) The same order on the aggregate model set: the only set the rule allows for it.
        var agg = ModelSetGuard.Decide(true, "AggregateTacticalLevel", cutATasks, cutA.Units, cutA.Other);
        Check(ref failures, agg.Kind == ModelSetCheckKind.AllowedAggregate && agg.AbortReason == "" && !agg.Warn
                            && agg.Line.Contains("ABOVE BN, aggregate-only") && agg.Line.Contains("Vrf:ModelSet=AggregateTacticalLevel"),
              "(g5) cut A on Vrf:ModelSet=AggregateTacticalLevel is ALLOWED - one INFO line", agg.Line);

        // (g6) FAIL-FIRST ARM 2 - the upward override: a company-only order on the aggregate set.
        var r9 = ModelSetGuard.ReadInit(ReadData(repo, R9Init));
        var r9Tasks = Performers(repo, R9Order);
        var up = ModelSetGuard.Decide(true, "AggregateTacticalLevel", r9Tasks, r9.Units, r9.Other);
        Check(ref failures, up.Kind == ModelSetCheckKind.AllowedUp && up.Highest == "COY"
                            && string.Join(",", up.HighestUnits) == "114.MechCoy,1.BdeHQ" && up.AbortReason == "" && !up.Warn
                            && up.Line.Contains("OVERRIDE UP - allowed") && up.Line.Contains("RL-20260927-06"),
              "(g6) ALLOWED UP: the R9 order (1222.MechPlt PLT, 114.MechCoy COY, 1.BdeHQ COY) on AggregateTacticalLevel - " +
              "the setting may lift battalion-and-below to aggregate (RL-20260927-06); one INFO line", up.Line);

        // (g7) BN and below on EntityLevel: allowed. Only the TASKED units count.
        var entity = ModelSetGuard.Decide(false, "EntityLevel", r9Tasks, r9.Units, r9.Other);
        var bnOnly = ModelSetGuard.Decide(false, "EntityLevel", new[] { Bn1112Uuid }, ironStorm.Units, ironStorm.Other);
        Check(ref failures, entity.Kind == ModelSetCheckKind.AllowedEntity && entity.Highest == "COY" && !entity.Warn
                            && bnOnly.Kind == ModelSetCheckKind.AllowedEntity && bnOnly.Highest == "BN" && !bnOnly.Warn,
              "(g7) ALLOWED on EntityLevel: the R9 order (highest COY) and 1-112 IN (BN) tasked alone on the Iron Storm init, " +
              "whose CORPS/DIV/BDE units are not tasked - only the TASKED units count",
              $"R9 {entity.Kind}/{entity.Highest}, 1-112 IN {bnOnly.Kind}/{bnOnly.Highest}");

        // (g8) NOS in real data: it cannot lift the choice, and it is NAMED - a WARNING on EntityLevel.
        var coaInit = ModelSetGuard.ReadInit(ReadData(repo, "COA-STP1_Initialization.xml"));
        var coa = ModelSetGuard.Decide(false, "EntityLevel", Performers(repo, "COA-STP1_Order.xml"), coaInit.Units, coaInit.Other);
        var nosBde = ModelSetGuard.Decide(false, "EntityLevel", new[] { Bde364NosUuid }, ironStorm.Units, ironStorm.Other);
        Check(ref failures, coa.Kind == ModelSetCheckKind.AllowedEntity && coa.Highest == "BN" && coa.TaskCount == 42 && coa.Warn
                            && coa.Line.Contains("A/6-56/HHC [NOS]") && coa.Line.Contains("1-1/2/1_AD [NOS]")
                            && coa.Line.Contains("counted BELOW BN"),
              "(g8) NOS: COA-STP1 (42 tasks) is allowed on EntityLevel (highest BN) with a WARNING that NAMES both NOS units",
              coa.Line);
        Check(ref failures, nosBde.Kind == ModelSetCheckKind.AllowedEntity && nosBde.Warn && nosBde.Line.Contains("364th_TK_BDE"),
              "(g8) a NOS-coded brigade (364th TK BDE (res)) tasked alone cannot lift the choice - allowed, WARNED by name",
              nosBde.Line);

        // (g9) MISSING codes: the runner's rule (below BN, named) - not the typed parse's "AG" (which would refuse).
        string stripped = Regex.Replace(ReadData(repo, CutAInit), "<EchelonCode>[^<]*</EchelonCode>", "");
        var rawStripped = ModelSetGuard.ReadInit(stripped);
        var missing = ModelSetGuard.Decide(false, "EntityLevel", cutATasks, rawStripped.Units, rawStripped.Other);
        var typedUnits = InitParser.Parse(stripped).Units
            .GroupBy(u => u.Uuid.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => new InitEchelonUnit(g.First().Uuid, g.First().Name, g.First().EchelonCode), StringComparer.Ordinal);
        var typedVerdict = ModelSetGuard.Decide(false, "EntityLevel", cutATasks, typedUnits, new Dictionary<string, string>());
        Check(ref failures, missing.Kind == ModelSetCheckKind.AllowedEntity && missing.Warn
                            && missing.Performers.All(p => p.Status == "missing")
                            && missing.Line.Contains(Div28 + " [missing]") && typedVerdict.Kind == ModelSetCheckKind.Refused,
              "(g9) MISSING echelons: cut A on an init without EchelonCodes is allowed on EntityLevel with all 3 NAMED as " +
              "missing (the D2 rule) - FAIL-FIRST: fed the typed parse's codes (\"AG\" x3) the same order would be REFUSED",
              $"raw {missing.Kind}, typed {typedVerdict.Kind}");

        // (g10) A performer that is not a Unit, and one not in the init at all: below BN, named, never a throw.
        var units = new Dictionary<string, InitEchelonUnit>(StringComparer.Ordinal)
        {
            ["aaaaaaaa-0000-0000-0000-00000000000a"] = new("AAAAAAAA-0000-0000-0000-00000000000A", "U-bn", "BN"),
        };
        var other = new Dictionary<string, string>(StringComparer.Ordinal) { ["aaaaaaaa-0000-0000-0000-000000000020"] = "Platform" };
        var strangers = ModelSetGuard.Decide(false, "EntityLevel",
            new[] { "AAAAAAAA-0000-0000-0000-00000000000A", "aaaaaaaa-0000-0000-0000-000000000020",
                    "bbbbbbbb-0000-0000-0000-00000000dead", "", "aaaaaaaa-0000-0000-0000-00000000000a" }, units, other);
        Check(ref failures, strangers.Kind == ModelSetCheckKind.AllowedEntity && strangers.Highest == "BN" && strangers.TaskCount == 5
                            && strangers.Performers.Count == 3 && strangers.Performers[0].Tasks == 2
                            && strangers.Performers[1].Status == "not-a-unit" && strangers.Performers[2].Status == "not-in-init"
                            && strangers.Warn && strangers.Line.Contains("(a Platform, not a Unit) [not-a-unit]")
                            && !strangers.Line.Contains("dead"),
              "(g10) uuids match case-insensitively (2 tasks, 1 performer); a Platform performer and a stranger count below " +
              "BN; the Platform is NAMED, the stranger is left to the service's own NOT FOUND refusal", strangers.Line);
        var none = ModelSetGuard.Decide(false, "EntityLevel", new[] { "", "" }, units, other);
        Check(ref failures, none.Kind == ModelSetCheckKind.AllowedEntity && none.TaskCount == 2 && none.Performers.Count == 0
                            && none.Line.Contains("no tasked unit has a ranked echelon"),
              "(g10) an order that tasks nobody is allowed and says so (nothing to rank)", none.Line);

        // (g11) MIXED SIDES: a hostile formation that is tasked counts like a friendly one (the ruling does not split by side).
        var mixed = ModelSetGuard.Decide(false, "EntityLevel", new[] { Bn1112Uuid, Bde105Uuid }, ironStorm.Units, ironStorm.Other);
        Check(ref failures, mixed.Kind == ModelSetCheckKind.Refused && mixed.Highest == "BDE"
                            && mixed.Line.Contains("105th_AT_BDE_(res)__ENEMY_ARMORED_ANTI_ARMORED_BRIGADE (BDE)"),
              "(g11) MIXED SIDES: friendly 1-112 IN (BN) + HOSTILE 105th AT BDE (BDE) on EntityLevel is REFUSED", mixed.Line);
    }

    // ------------------------------------------------------------------------- (g12)-(g13) ----
    private static void Wiring(ref int failures, string repo)
    {
        string onEntity = ModelSetGuard.StartupLine("EntityLevel", false);
        string onAgg = ModelSetGuard.StartupLine("AggregateTacticalLevel", true);
        string unset = ModelSetGuard.StartupLine(null, false);
        Check(ref failures, onEntity.StartsWith("MODEL SET RULE (D2b; RL-20260927-06, RL-20260928-01): Vrf:ModelSet='EntityLevel' -> EntityLevel.", StringComparison.Ordinal)
                            && onEntity.Contains("ABOVE BN is REFUSED here") && onEntity.Contains("TASKABRT")
                            && onAgg.Contains("-> AggregateTacticalLevel.") && onAgg.Contains("OVERRIDE UP")
                            && unset.Contains("Vrf:ModelSet=(not set) -> EntityLevel"),
              "(g12) the start-up line states the active model set and the rule, in both states", onEntity);
        var all = new[] { onEntity, onAgg, unset };
        Check(ref failures, all.All(s => s.All(ch => ch < 128)), "(g12) the start-up lines are ASCII");

        string svc = SafeRead(Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs"));
        string onOrder = Between(svc, "private void OnOrder(", "private long ScaleOrderMs(");
        int parse = onOrder.IndexOf("try { order = OrderParser.Parse(e.Body); }", StringComparison.Ordinal);
        int guard = onOrder.IndexOf("ModelSetGuard.Decide(_aggregateModelSet, _modelSetRaw,", StringComparison.Ordinal);
        int graphics = onOrder.IndexOf("_graphicsByC2SimUuid[g.Uuid] = incoming;", StringComparison.Ordinal);
        int record = onOrder.IndexOf("_taskByUuid[task.TaskUuid] = task;", StringComparison.Ordinal);
        int materialize = onOrder.IndexOf("MaterializeUnit(", StringComparison.Ordinal);
        // Merge with STP-850: the dispatch call also carries the order's receipt clock and minimum offset.
        int run = onOrder.IndexOf("_ = RunTaskAsync(t, u, orderReceiptClock, orderMinOffsetMs);", StringComparison.Ordinal);
        Check(ref failures, parse > 0 && guard > parse && graphics > guard && record > graphics && materialize > record && run > materialize
                            && CountOf(svc, "ModelSetGuard.Decide(") == 1,
              "(g13) THE SOURCE GUARD: ModelSetGuard.Decide is called ONCE, in OnOrder, right after the parse - BEFORE the " +
              "order's graphics are registered, its tasks recorded, any unit materialized or any task dispatched",
              $"parse {parse}, guard {guard}, graphics {graphics}, record {record}, materialize {materialize}, run {run}");
        string refusal = guard > 0 && graphics > guard ? onOrder.Substring(guard, graphics - guard) : "";
        Check(ref failures, refusal.Contains("if (modelSetCheck.Kind == ModelSetCheckKind.Refused)")
                            && refusal.Contains("_log.LogError(\"{Line}\", modelSetCheck.Line);")
                            && refusal.Contains("foreach (var task in order.Tasks)")
                            && refusal.Contains("_sequencer.NotifyAbandoned(task.TaskUuid);")
                            && refusal.Contains("S.TaskStatusCodeType.TASKABRT, modelSetCheck.AbortReason);")
                            && refusal.Contains("return;")
                            && refusal.IndexOf("return;", StringComparison.Ordinal) > refusal.IndexOf("TASKABRT", StringComparison.Ordinal),
              "(g13) the refusal logs ONE ERROR line, abandons and TASKABRTs EVERY task of the order, then RETURNS - nothing " +
              "of a refused order is dispatched");
        string init = Between(svc, "private void ProcessInitializationLocked(", "// COMPOSE-FROM-CHILDREN (Vrf:ComposeHierarchy)");
        int typed = init.IndexOf("try { init = InitParser.Parse(body); }", StringComparison.Ordinal);
        int rawRead = init.IndexOf("ModelSetGuard.ReadInit(body)", StringComparison.Ordinal);
        Check(ref failures, typed > 0 && rawRead > typed && CountOf(svc, "ModelSetGuard.ReadInit(") == 1,
              "(g13) every initialization's raw EchelonCodes are read once, after its typed parse succeeded");
        Check(ref failures, svc.Contains("_log.LogInformation(\"{Line}\", ModelSetGuard.StartupLine(_modelSetRaw, _aggregateModelSet));"),
              "(g13) the start-up line is logged from the SAME Vrf:ModelSet read the task judges use (D1)");
    }

    // --------------------------------------------------------------------------- helpers ----
    private static IReadOnlyList<string> Performers(string repo, string order)
        => OrderParser.Parse(ReadData(repo, order)).Tasks.Select(t => t.TaskeeUuid).ToList();

    private static string ReadData(string repo, string file)
    {
        string p = Path.Combine(repo, "data", file);
        return File.Exists(p) ? File.ReadAllText(p) : "";
    }

    private static string SafeRead(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

    private static string Between(string s, string from, string to)
    {
        int a = s.IndexOf(from, StringComparison.Ordinal);
        if (a < 0) return "";
        int b = s.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        return b < 0 ? s.Substring(a) : s.Substring(a, b - a);
    }

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

    private static void Check(ref int failures, bool ok, string label, string detail = "")
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}" + (ok || string.IsNullOrEmpty(detail) ? "" : " -- " + detail));
        if (!ok) failures++;
    }
}
