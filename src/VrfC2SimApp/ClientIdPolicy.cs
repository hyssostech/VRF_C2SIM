namespace VrfC2SimApp;

/// <summary>
/// THE CLIENTID TRAP, MADE ACTIONABLE (2026-09-20).
///
/// The interface creates a unit only when its SystemName equals Vrf:ClientId
/// (VrfC2SimService.ProcessInitializationLocked; docs/RUNBOOK.md sec 2: "clientId (argv4) MUST
/// equal the init's SystemName (STP init -> STP; COA-STP1 -> C2SIM), or the interface creates 0
/// units"). That filter is CORRECT and is not loosened here: an interface that tasked every unit
/// of every producer on the bus would create another simulator's ORBAT, and the init is explicitly
/// the whole context tree, not the set of things we simulate (C13, user ruling 2026-09-06).
///
/// What was wrong was the EVIDENCE. A mismatch produced exactly one log line naming the ClientId
/// and a comma-joined list of the SystemNames seen - no counts, no ready-made override, and
/// nothing on the C2SIM bus at all, so the producer that pushed the init got silence and a
/// healthy-looking interface. It has cost live-run time more than once: the COA-STP1 init needs
/// ClientId=C2SIM (RUNBOOK sec 2), a 48-unit init arrived with SystemName "[Not Set]" during
/// PREREG_TYPEMAP_LIVE_GATE, and the real STP export characterised on 2026-09-20 carries the
/// literal string "Not Set" for all 40 of its units.
///
/// PURE, so the sentence an operator has to act on is decidable offline and is locked by a
/// self-test rather than by reading a run log.
/// </summary>
public static class ClientIdPolicy
{
    /// <summary>How a blank SystemName is spelled in the diagnostic. An init that carries the
    /// element but leaves it empty and an init that omits it entirely both arrive here as "", and
    /// an operator staring at a list of names needs to see that there IS an entry rather than a
    /// gap in the punctuation.</summary>
    public const string BlankSystemName = "(blank/absent)";

    /// <summary>The marker every clientId-mismatch line and report carries, so a harvest can count
    /// them without matching prose.</summary>
    public const string Marker = "CLIENTID MISMATCH";

    /// <summary>
    /// The distinct SystemName values in an init, with how many units carry each, most common
    /// first and then alphabetically so the line is stable across runs of the same init.
    /// </summary>
    public static List<(string Name, int Count)> SystemNameCounts(IEnumerable<InitUnit> units)
        => (units ?? Array.Empty<InitUnit>())
           .Where(u => u != null)
           .GroupBy(u => string.IsNullOrWhiteSpace(u.SystemName) ? BlankSystemName : u.SystemName.Trim(),
                    StringComparer.Ordinal)
           .Select(g => (Name: g.Key, Count: g.Count()))
           .OrderByDescending(x => x.Count).ThenBy(x => x.Name, StringComparer.Ordinal)
           .ToList();

    /// <summary>"Not Set" x40, STP x3 - the counts, rendered.</summary>
    public static string DescribeSystemNames(IEnumerable<(string Name, int Count)> counts)
        => string.Join(", ", (counts ?? Array.Empty<(string, int)>())
                             .Select(c => $"\"{c.Name}\" x{c.Count}"));

    /// <summary>
    /// The ONE override an operator should try, spelled exactly as it must be typed. It is the
    /// ENVIRONMENT form because that is what both start paths take
    /// (scripts/StartInterface52.ps1 -ClientId sets Vrf__ClientId, and the runner injects the same
    /// variable), and because editing a deployed appsettings.json is the change that produced a
    /// separate false green once already.
    ///
    /// THE MOST COMMON name is proposed, not a guess at intent: on a single-producer init that is
    /// the only candidate, and on a mixed one it is the producer that owns most of the tree. A
    /// blank SystemName has no override that could work, and the caller says so instead of
    /// offering Vrf__ClientId="(blank/absent)".
    /// </summary>
    public static string SuggestedOverride(IEnumerable<(string Name, int Count)> counts)
    {
        var best = (counts ?? Array.Empty<(string Name, int Count)>())
                   .FirstOrDefault(c => c.Name != BlankSystemName);
        return string.IsNullOrEmpty(best.Name) ? "" : $"Vrf__ClientId=\"{best.Name}\"";
    }

    /// <summary>
    /// The whole diagnostic, as one sentence block. Used verbatim for the ERROR line AND for the
    /// ObservationReport body, so the C2 side and the log cannot disagree about what happened.
    /// </summary>
    /// <param name="source">Where the init came from (broadcast, QUERYINIT, file) - a late-join
    /// re-delivery and a pushed file are different problems.</param>
    public static string MismatchMessage(string source, string clientId, int totalUnits,
                                         IReadOnlyList<(string Name, int Count)> counts)
    {
        string names = DescribeSystemNames(counts);
        string fix = SuggestedOverride(counts);
        string action = fix.Length > 0
            ? $"SET THE CLIENT ID TO MATCH, exactly: {fix} in the interface's environment (or " +
              "scripts\\StartInterface52.ps1 -ClientId, or Vrf:ClientId in appsettings.json). " +
              "ALTERNATIVELY fix the producer so it stamps its real SystemName - an init whose " +
              "SystemName is a placeholder like \"Not Set\" is an unset export field, not a chosen " +
              "value, and the producer side is the right fix."
            : "NO OVERRIDE CAN FIX THIS: every unit's SystemName is blank or absent, so there is no " +
              "value Vrf:ClientId could be set to. The PRODUCER must stamp " +
              "SystemEntityList/SystemName before this init can be used.";
        return $"{Marker}: init ({source}) matched 0 of {totalUnits} unit(s) against " +
               $"Vrf:ClientId=\"{clientId}\", so NOTHING will be created and NOTHING will be " +
               $"taskable - every order against this init will be refused with \"TASKEEUUID ... NOT " +
               $"FOUND IN C2SIMINITIALIZATION\". SystemName(s) in this init: [{names}]. {action} " +
               "(RUNBOOK sec 2. The filter is deliberate and is not being loosened: the init is the " +
               "whole ORBAT context and only this client's units are simulated.)";
    }
}
