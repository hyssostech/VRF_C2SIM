using System.Xml;

namespace VrfC2SimApp;

/// <summary>One echelon code on the runner's ladder. Status: ranked | unranked (a schema value off the ladder) |
/// not-schema | missing. Rank 0 = not ranked (counted below BN).</summary>
public sealed record EchelonRank(string Code, string Canonical, int Rank, string Status, string Note);

/// <summary>
/// THE ECHELON LADDER - THE ONE C# PLACE FOR IT (D2b; RL-20260927-06, RL-20260928-01). It is the runner's table,
/// key for key and note for note: scripts/RunnerLib.ps1 $script:EchelonLadder, $script:EchelonBattalionRank,
/// $script:EchelonAliases and $script:EchelonUnranked, read by Get-EchelonRank (plan row D2). Two tables that must
/// agree are pinned from both sides: `--rulings-selftest` (g1) parses RunnerLib.ps1 and compares it with the tables
/// below, and tests/RunnerTurnaround.Tests.ps1 sec 8z parses THIS file and compares it with the runner's. Change
/// one, change both - either check fails otherwise.
///
/// Provenance (as in RunnerLib): C2SIM EchelonCodeType, the JC3IEDM 3.1 UnitTypeSizeCode enumeration
/// (C2SIM_SMX_LOX_CWIX2024.xsd :4614-4648, mandatory on Unit :4970), ranked in the APP-6 / MIL-STD-2525 position-12
/// order; a grouping code ranks with the unit it is based on (COYG a company, BNG and BATGRP a battalion, BDEGRP a
/// brigade). SQD and SEC are not schema values; they are read as SQUAD and SECT, and the note says so.
/// PURE: text in, records out.
/// </summary>
public static class EchelonLadder
{
    // The ladder. Keep ONE (code, rank) pair per entry in this literal form: RunnerTurnaround 8z reads it by regex.
    private static readonly (string Code, int Rank)[] LadderTable =
    {
        ("TEAM", 1), ("SQUAD", 2), ("SECT", 3), ("PLT", 4),
        ("COY", 5), ("COYG", 5),
        ("BN", 6), ("BNG", 6), ("BATGRP", 6),
        ("RGT", 7),
        ("BDE", 8), ("BDEGRP", 8),
        ("DIV", 9), ("CORPS", 10), ("ARMY", 11), ("AG", 12), ("REGION", 13),
    };

    /// <summary>THE THRESHOLD: a tasked echelon ranked ABOVE this is aggregate-only (RL-20260927-06).</summary>
    public const int BattalionRank = 6;

    private static readonly (string Code, string ReadAs)[] AliasTable =
    {
        ("SQD", "SQUAD"), ("SEC", "SECT"),
    };

    // Schema values OFF the ground ladder: counted below BN, and named. Notes are RunnerLib's, verbatim.
    private static readonly (string Code, string Note)[] UnrankedTable =
    {
        ("NOS", "NOS, \"not otherwise specified\" - no echelon given"),
        ("NKN", "NKN, \"not known\" - no echelon given"),
        ("FLIGHT", "an AIR echelon (flight), not on the ground ladder"),
        ("WING", "an AIR echelon (wing), not on the ground ladder"),
        ("SQDRNA", "an AIR echelon (squadron, air), not on the ground ladder"),
        ("SQDRNM", "a NAVAL echelon (squadron, maritime), not on the ground ladder"),
        ("FLEET", "a NAVAL echelon (fleet), not on the ground ladder"),
        ("NTF", "a NAVAL echelon (task force), not on the ground ladder"),
        ("NTG", "a NAVAL echelon (task group), not on the ground ladder"),
        ("NTU", "a NAVAL echelon (task unit), not on the ground ladder"),
        ("TSKELN", "a NAVAL echelon (task element), not on the ground ladder"),
    };

    private static readonly Dictionary<string, int> RankOf =
        LadderTable.ToDictionary(e => e.Code, e => e.Rank, StringComparer.Ordinal);
    private static readonly Dictionary<string, string> AliasOf =
        AliasTable.ToDictionary(e => e.Code, e => e.ReadAs, StringComparer.Ordinal);
    private static readonly Dictionary<string, string> UnrankedNoteOf =
        UnrankedTable.ToDictionary(e => e.Code, e => e.Note, StringComparer.Ordinal);

    /// <summary>The ladder, in its declared order (the self-test compares it with RunnerLib's).</summary>
    public static IReadOnlyList<(string Code, int Rank)> Ladder => LadderTable;
    public static IReadOnlyList<(string Code, string ReadAs)> Aliases => AliasTable;
    public static IReadOnlyList<(string Code, string Note)> Unranked => UnrankedTable;

    /// <summary>One echelon code -> its place on the ladder: the port of RunnerLib Get-EchelonRank, message for
    /// message. Case does not matter; surrounding blanks are ignored; null and "" are MISSING.</summary>
    public static EchelonRank Rank(string code)
    {
        string raw = (code ?? "").Trim();
        string up = raw.ToUpperInvariant();
        if (up.Length == 0)
            return new EchelonRank(raw, "", 0, "missing",
                                   "NO EchelonCode (the schema makes it mandatory on a Unit) - counted below BN");
        if (AliasOf.TryGetValue(up, out string readAs))
            return new EchelonRank(raw, readAs, RankOf[readAs], "ranked",
                                   $"\"{raw}\" is not a schema value; read as {readAs}");
        if (RankOf.TryGetValue(up, out int rank))
            return new EchelonRank(raw, up, rank, "ranked", "");
        if (UnrankedNoteOf.TryGetValue(up, out string note))
            return new EchelonRank(raw, up, 0, "unranked", note + " - counted below BN");
        return new EchelonRank(raw, "", 0, "not-schema",
                               $"\"{raw}\" is not a C2SIM EchelonCodeType value - counted below BN");
    }
}

/// <summary>One Unit of the initialization as AUTHORED: its UUID, Name and EchelonCode text (empty when absent).</summary>
public sealed record InitEchelonUnit(string Uuid, string Name, string EchelonCode);

/// <summary>The init's Units keyed by LOWER-CASE uuid (the first one wins, as in RunnerLib), plus every other element
/// that carries a UUID child (so a performer that is not a Unit is told apart from one that is not in the init).</summary>
public sealed class InitEchelons
{
    public bool Parsed { get; init; }
    public string Error { get; init; } = "";
    public Dictionary<string, InitEchelonUnit> Units { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Other { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Duplicates { get; } = new(StringComparer.Ordinal);
}

/// <summary>How the model-set rule judged one order.</summary>
public enum ModelSetCheckKind
{
    /// <summary>BN and below, on EntityLevel - the default the rule chooses.</summary>
    AllowedEntity,
    /// <summary>Above BN, on AggregateTacticalLevel - the only model set the rule allows for it.</summary>
    AllowedAggregate,
    /// <summary>BN and below, on AggregateTacticalLevel - the upward override (RL-20260927-06).</summary>
    AllowedUp,
    /// <summary>Above BN, on EntityLevel - REFUSED: no downward override (RL-20260928-01).</summary>
    Refused,
}

/// <summary>One distinct performer of the order, ranked.</summary>
public sealed record PerformerEchelon(string Uuid, int Tasks, string Name, string EchelonCode, string Canonical,
                                      int Rank, string Status, string Note);

/// <summary>The verdict for one order: the kind, the evidence, and the ONE line the service logs for it.</summary>
public sealed record ModelSetVerdict(
    ModelSetCheckKind Kind,
    int TaskCount,
    IReadOnlyList<PerformerEchelon> Performers,
    string Highest,
    int HighestRank,
    IReadOnlyList<string> HighestUnits,
    IReadOnlyList<string> AboveBattalionUnits,
    IReadOnlyList<PerformerEchelon> Unranked,
    string Line,
    bool Warn,
    string AbortReason);

/// <summary>
/// D2b - THE MODEL-SET RULE INSIDE THE APP (RL-20260927-06, RL-20260928-01; plan row D2b of
/// docs/PLAN_MOVEMENT_2026-09-27.md; audit AUDIT_RULINGS_IN_CODE_2026-09-28 sec 4, "the most consequential gap").
///
/// The runner CHOOSES the model set from the order at Stage 0 (RunnerLib Select-ModelSetByEchelon, RUNBOOK sec 11j).
/// An interface started by hand - scripts/StartInterface52.ps1 with the Demo overlay, Vrf:ModelSet=EntityLevel -
/// chooses nothing, and VR-Forces has loaded its model set before any order arrives, so the app cannot change it.
/// The app's twin of the rule is therefore a GUARD AT ORDER RECEIPT, on the runner's own ladder (EchelonLadder):
///   * the TASKED units are the order's PerformingEntity values - never the init's untasked units;
///   * a tasked unit's echelon is its init Unit/EchelonCode AS AUTHORED (ReadInit reads the raw text: the typed parse
///     turns an ABSENT code into the enum's first member "AG", rank 12, see InitParser.cs CAVEAT);
///   * the HIGHEST decides. Above BN on EntityLevel -> REFUSED: one ERROR line, every task of the order TASKABRT, and
///     nothing of it registered or dispatched - above battalion is aggregate-only, with no downward override. Above BN
///     on AggregateTacticalLevel -> allowed. BN and below on AggregateTacticalLevel -> allowed, the OVERRIDE UP. BN and
///     below on EntityLevel -> allowed;
///   * NOS, NKN, air/naval, a non-schema or missing code, a performer that is not a Unit or not in the init: counted
///     BELOW BN and NAMED - on EntityLevel the order's line is then a WARNING (it cannot be lifted by what it lacks).
/// PURE: no clock, no bridge, no configuration - `--rulings-selftest` checks it offline.
/// </summary>
public static class ModelSetGuard
{
    public const string RuleIds = "RL-20260927-06, RL-20260928-01";

    /// <summary>The port of RunnerLib Get-InitUnitEchelons: every Unit's UUID / Name / EchelonCode (DIRECT children, as
    /// the schema puts them), keyed by lower-case uuid, first wins; other elements that carry a UUID child are kept by
    /// element name. DTDs are ignored and nothing external is resolved. Never throws: Parsed=false + Error instead.</summary>
    public static InitEchelons ReadInit(string initXml)
    {
        if (string.IsNullOrWhiteSpace(initXml)) return new InitEchelons { Parsed = false, Error = "the init text is empty" };
        var doc = new XmlDocument { XmlResolver = null };
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
            using var sr = new StringReader(initXml);
            using var xr = XmlReader.Create(sr, settings);
            doc.Load(xr);
        }
        catch (Exception ex) { return new InitEchelons { Parsed = false, Error = ex.Message }; }

        var outp = new InitEchelons { Parsed = true };
        foreach (XmlNode node in doc.SelectNodes("//*[local-name()='Unit']"))
        {
            string uuid = null, name = null, ech = null;
            foreach (XmlNode c in node.ChildNodes)
            {
                if (c.NodeType != XmlNodeType.Element) continue;
                if (c.LocalName == "UUID" && uuid == null) uuid = c.InnerText.Trim();
                if (c.LocalName == "Name" && name == null) name = c.InnerText.Trim();
                if (c.LocalName == "EchelonCode" && ech == null) ech = c.InnerText.Trim();
            }
            if (string.IsNullOrWhiteSpace(uuid)) continue;
            string key = uuid.ToLowerInvariant();
            if (outp.Units.TryGetValue(key, out var first))
            {
                if (!string.Equals(first.EchelonCode, ech ?? "", StringComparison.Ordinal) && !outp.Duplicates.ContainsKey(key))
                    outp.Duplicates[key] = $"{uuid}: EchelonCode \"{first.EchelonCode}\" and \"{ech ?? ""}\" (the first is used)";
                continue;
            }
            outp.Units[key] = new InitEchelonUnit(uuid, string.IsNullOrEmpty(name) ? "(no Name)" : name, ech ?? "");
        }
        foreach (XmlNode node in doc.SelectNodes("//*[*[local-name()='UUID']]"))
        {
            if (node.LocalName == "Unit") continue;
            foreach (XmlNode c in node.ChildNodes)
            {
                if (c.NodeType == XmlNodeType.Element && c.LocalName == "UUID")
                {
                    string k = c.InnerText.Trim().ToLowerInvariant();
                    if (k.Length > 0) outp.Other.TryAdd(k, node.LocalName);
                    break;
                }
            }
        }
        return outp;
    }

    /// <summary>
    /// THE DECISION for one order. <paramref name="taskPerformers"/> has ONE entry per task (its PerformingEntity,
    /// empty when it has none); <paramref name="modelSetShown"/> is the raw Vrf:ModelSet for the line (null = not set).
    /// </summary>
    public static ModelSetVerdict Decide(bool aggregateModelSet, string modelSetShown, IReadOnlyList<string> taskPerformers,
                                         IReadOnlyDictionary<string, InitEchelonUnit> units,
                                         IReadOnlyDictionary<string, string> other)
    {
        taskPerformers ??= Array.Empty<string>();
        // ONE RECORD PER DISTINCT PERFORMER, in the order's own order, with how many tasks name it.
        var order = new List<string>();
        var tasksOf = new Dictionary<string, (string Uuid, int Tasks)>(StringComparer.Ordinal);
        foreach (string p in taskPerformers)
        {
            string raw = (p ?? "").Trim();
            if (raw.Length == 0) continue;
            string k = raw.ToLowerInvariant();
            if (tasksOf.TryGetValue(k, out var t)) tasksOf[k] = (t.Uuid, t.Tasks + 1);
            else { tasksOf[k] = (raw, 1); order.Add(k); }
        }
        var performers = new List<PerformerEchelon>();
        foreach (string k in order)
        {
            var (uuid, n) = tasksOf[k];
            if (units != null && units.TryGetValue(k, out var u))
            {
                var r = EchelonLadder.Rank(u.EchelonCode);
                performers.Add(new PerformerEchelon(uuid, n, u.Name, r.Code, r.Canonical, r.Rank, r.Status, r.Note));
            }
            else if (other != null && other.TryGetValue(k, out string element))
                performers.Add(new PerformerEchelon(uuid, n, $"(a {element}, not a Unit)", "", "", 0, "not-a-unit",
                               $"the performer is a {element} in the init, not a Unit - it has no echelon; counted below BN"));
            else
                performers.Add(new PerformerEchelon(uuid, n, "(not in the init)", "", "", 0, "not-in-init",
                               "no Unit or other entity with this UUID in the init - counted below BN"));
        }

        int highestRank = performers.Count == 0 ? 0 : performers.Max(p => p.Rank);
        string highest = highestRank > 0 ? performers.First(p => p.Rank == highestRank).Canonical : "";
        var highestUnits = highestRank > 0 ? performers.Where(p => p.Rank == highestRank).Select(p => p.Name).ToList()
                                           : new List<string>();
        var above = performers.Where(p => p.Rank > EchelonLadder.BattalionRank)
                              .Select(p => $"{p.Name} ({p.Canonical})").ToList();
        var unranked = performers.Where(p => p.Status != "ranked").ToList();
        var unrankedNamed = unranked.Where(p => p.Status != "not-in-init").ToList();

        ModelSetCheckKind kind = above.Count > 0
            ? (aggregateModelSet ? ModelSetCheckKind.AllowedAggregate : ModelSetCheckKind.Refused)
            : (aggregateModelSet ? ModelSetCheckKind.AllowedUp : ModelSetCheckKind.AllowedEntity);

        string shown = string.IsNullOrWhiteSpace(modelSetShown) ? "(not set)" : modelSetShown.Trim();
        string set = aggregateModelSet ? UnitPositionPolicy.AggregateModelSet : UnitPositionPolicy.EntityModelSet;
        string model = shown == set ? $"Vrf:ModelSet={set}" : $"Vrf:ModelSet={shown} -> {set}";
        string top = highestRank > 0
            ? $"highest TASKED echelon {highest} ({string.Join(", ", highestUnits)})"
            : "no tasked unit has a ranked echelon";
        string census = $"{performers.Count} tasked unit(s), {taskPerformers.Count} task(s)";
        string line, abort = "";
        bool warn = false;
        switch (kind)
        {
            case ModelSetCheckKind.Refused:
                line = "MODEL SET RULE (D2b): ORDER REFUSED - ABOVE BATTALION IS AGGREGATE-ONLY (RL-20260927-06; no downward " +
                       $"override, RL-20260928-01). This order's {top} is ABOVE BN - it tasks {string.Join(", ", above)} - and " +
                       $"this interface runs {model}. NONE of its {taskPerformers.Count} task(s) is executed: each is reported " +
                       "TASKABRT. Run the order on the aggregate profile - Vrf:ModelSet=AggregateTacticalLevel with " +
                       "data/unit-type-map-52-aggregate.json and a fixture that loads AggregateTacticalLevel.sms " +
                       "(IronStorm_Centre_52_Aggregate); the runner's -ModelSet auto chooses it by itself.";
                abort = $"REFUSED (D2b): ABOVE BATTALION IS AGGREGATE-ONLY - the order tasks {string.Join(", ", above)} and " +
                        $"this interface runs {model} ({RuleIds}); the order was not executed";
                break;
            case ModelSetCheckKind.AllowedAggregate:
                line = $"MODEL SET RULE (D2b): allowed - this order's {top} is ABOVE BN, aggregate-only, and this interface " +
                       $"runs {model} (RL-20260927-06); {census}.";
                break;
            case ModelSetCheckKind.AllowedUp:
                line = $"MODEL SET RULE (D2b): OVERRIDE UP - allowed. This order's {top} is BN or below (EntityLevel by " +
                       $"default) and this interface runs {model}: the setting may lift battalion-and-below to aggregate " +
                       "(RL-20260927-06: \"battalion and b[el]ow can be simulated at an aggregate level rather than " +
                       $"entity\"); {census}.";
                break;
            default:
                line = $"MODEL SET RULE (D2b): allowed - this order's {top} is BN or below and this interface runs {model} " +
                       $"(RL-20260927-06); {census}.";
                if (unrankedNamed.Count > 0)
                {
                    warn = true;
                    line += $" BUT {unrankedNamed.Count} tasked unit(s) carry no ranked echelon and are counted BELOW BN " +
                            "(the D2 rule): " + string.Join(", ", unrankedNamed.Select(p =>
                                $"{p.Name} [{(p.EchelonCode.Length > 0 ? p.EchelonCode : p.Status)}]")) +
                            ". If one of them is really a formation above BN, this order must not run at entity level - " +
                            "use the aggregate profile (Vrf:ModelSet=AggregateTacticalLevel).";
                }
                break;
        }
        return new ModelSetVerdict(kind, taskPerformers.Count, performers, highest, highestRank, highestUnits, above,
                                   unranked, line, warn, abort);
    }

    /// <summary>The start-up line: the model set as the app read it, and what the rule does under it.</summary>
    public static string StartupLine(string rawValue, bool aggregateModelSet)
    {
        string shown = string.IsNullOrWhiteSpace(rawValue) ? "(not set)" : "'" + rawValue.Trim() + "'";
        string set = aggregateModelSet ? UnitPositionPolicy.AggregateModelSet : UnitPositionPolicy.EntityModelSet;
        string head = $"MODEL SET RULE (D2b; {RuleIds}): Vrf:ModelSet={shown} -> {set}. At every order the TASKED units " +
                      "(each task's PerformingEntity, never the init's untasked units) are ranked by their init EchelonCode " +
                      "on the runner's ladder (scripts/RunnerLib.ps1 Get-EchelonRank; NOS, NKN, air/naval, unknown and " +
                      "missing codes count below BN and are named): ";
        return head + (aggregateModelSet
            ? "every order runs here - above BN is aggregate-only and this IS the aggregate model set, and a BN-and-below " +
              "order runs as the OVERRIDE UP (allowed, RL-20260927-06)."
            : "an order that tasks a unit ABOVE BN is REFUSED here - above battalion is aggregate-only, no downward " +
              "override (RL-20260928-01) - with one ERROR line and a TASKABRT for each of its tasks; BN-and-below orders run.");
    }
}
