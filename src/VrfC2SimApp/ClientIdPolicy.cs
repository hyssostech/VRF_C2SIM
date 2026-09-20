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

    /// <summary>How loudly an all-foreign initialization is reported.</summary>
    public enum MismatchSeverity
    {
        /// <summary>Nothing to say: some unit matched, or the init is empty.</summary>
        None,
        /// <summary>An initialization this app has nothing to do with, arriving while it already
        /// holds its OWN units. Normal on a shared server. One INFO line, NOTHING on the bus.</summary>
        Foreign,
        /// <summary>The case that matters: this app holds NO units of its own and an init just
        /// matched none either, so the run will do nothing. ERROR + an ObservationReport.</summary>
        Loud,
    }

    /// <summary>
    /// SF6 (cold-start review of 9d67f97). THE LOUD DIAGNOSTIC MUST NOT CRY WOLF.
    ///
    /// The trigger was `matched == 0 &amp;&amp; init.Units.Count > 0` alone, which is true of ANY
    /// all-foreign initialization - and on a shared C2SIM server another system publishing its own
    /// ORBAT is the normal case, not a fault. Every one of those produced an ERROR *and* an
    /// ObservationReport pushed onto the bus, which is how a real diagnostic gets trained out of an
    /// operator's attention.
    ///
    /// The discriminator is whether THIS APP HAS ANY UNITS AT ALL. If it does, an init that matches
    /// none of them is somebody else's message and the filter did its job (C13: the init is the
    /// whole ORBAT context; only this client's units are simulated). If it does not, then nothing
    /// has ever been created, nothing is taskable, and the run is dead - which is exactly the
    /// failure that has cost live-run time, and is worth an ERROR on every delivery until it is
    /// fixed, including on the first and only initialization the app ever sees.
    ///
    /// PURE, so both arms are decidable offline and are locked by --stpexport-selftest.
    /// </summary>
    /// <param name="matched">Units in THIS init whose SystemName equalled Vrf:ClientId.</param>
    /// <param name="initUnitCount">Units in this init, of any SystemName.</param>
    /// <param name="unitsAlreadyHeld">Units of OUR OWN system this app has planned or created so
    /// far, across every initialization in this run.</param>
    public static MismatchSeverity Severity(int matched, int initUnitCount, int unitsAlreadyHeld)
    {
        if (matched > 0 || initUnitCount <= 0) return MismatchSeverity.None;
        return unitsAlreadyHeld > 0 ? MismatchSeverity.Foreign : MismatchSeverity.Loud;
    }

    /// <summary>
    /// The quiet arm's sentence: one INFO line, no bus report. It still names the counts, because
    /// "which systems are on this bus" is the first thing anyone asks when a run looks idle.
    /// </summary>
    public static string ForeignInitMessage(string source, string clientId, int totalUnits,
                                            int unitsAlreadyHeld,
                                            IReadOnlyList<(string Name, int Count)> counts)
        => $"Init ({source}): {totalUnits} unit(s), none with SystemName \"{clientId}\" - " +
           $"SystemName(s): [{DescribeSystemNames(counts)}]. This initialization belongs to another " +
           $"system on the bus and is IGNORED, which is normal on a shared C2SIM server. Not an " +
           $"error here: this interface already holds {unitsAlreadyHeld} unit(s) of its own, so it " +
           $"is not idle. (C13 / RUNBOOK sec 2 - the init is the whole ORBAT context and only this " +
           $"client's units are simulated. Nothing is pushed to the bus for this.)";

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
