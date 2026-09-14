using System.Collections.Concurrent;

namespace VrfC2SimApp;

/// <summary>
/// WHEN to tell C2SIM what a unit is actually represented by (B8, 2026-09-14).
///
/// R-SURFACE-PROXY (user ruling 2026-07-17) says a unit represented by a template that is not its
/// doctrinal identity must be announced - ReportBuilder.BuildTypeSubstitutionReport, an
/// ObservationReport/NameObservation. That announcement was emitted ONCE, at init, from the
/// type-map loop. Under CreationPolicy=AtOrder it is then WRONG for the rest of the run: at init
/// the unit is an EMPTY SHELL, and when an order first references it the unit is re-created - as
/// its full template (case 3) or composed from doctrinal sub-units (case 2). Run G6 shows the gap
/// directly: 144 PLACEMENT lines, 102 NameObservations.
///
/// The rule here: a unit's REPRESENTATION is a short string (template + how it was built), and an
/// announcement goes out when that string is new or has CHANGED. A re-creation as the same thing
/// says nothing - the point is to keep the C2SIM side's picture current, not to repeat it.
/// </summary>
public sealed class SubstitutionAnnouncer
{
    // unit name -> the representation last announced (or recorded) for it.
    private readonly ConcurrentDictionary<string, string> _byUnit = new(StringComparer.Ordinal);

    /// <summary>How this unit is represented in the simulation, as one comparable string.</summary>
    /// <param name="templateName">The VR-Forces template the plan names.</param>
    /// <param name="createSubordinates">False = an EMPTY SHELL (CreationPolicy=AtOrder at init).</param>
    /// <param name="composedFrom">Greater than 0 = built from that many doctrinal sub-units
    /// (EXPAND-to-compose) rather than from the template's own subordinates.</param>
    public static string Representation(string templateName, bool createSubordinates, int composedFrom)
    {
        string t = string.IsNullOrEmpty(templateName) ? "(no template)" : templateName;
        if (composedFrom > 0) return $"{t} composed from {composedFrom} sub-unit(s)";
        return createSubordinates ? t : $"{t} (empty shell)";
    }

    /// <summary>Is there a SUBSTITUTION to report at all? A proxy template carries substitution
    /// text; a unit composed from sub-units is a substitution of representation even when its own
    /// type mapped exactly. A plain re-creation as its own template is neither.</summary>
    public static bool Substituted(string substitution, int composedFrom)
        => composedFrom > 0 || !string.IsNullOrEmpty(substitution);

    /// <summary>True (and remembers it) when this unit has never been announced under this
    /// representation - i.e. it is new, or what represents it has changed.</summary>
    public bool ShouldAnnounce(string unitName, string representation)
    {
        if (string.IsNullOrEmpty(unitName)) return false;
        string rep = representation ?? "";
        bool changed = !_byUnit.TryGetValue(unitName, out var prev) || prev != rep;
        if (changed) _byUnit[unitName] = rep;
        return changed;
    }

    /// <summary>Remember a representation WITHOUT announcing it - for a unit whose type mapped
    /// exactly (nothing to report yet), so that a later change is measured against the truth.</summary>
    public void Record(string unitName, string representation)
    {
        if (!string.IsNullOrEmpty(unitName)) _byUnit[unitName] = representation ?? "";
    }

    /// <summary>What this unit was last announced/recorded as ("" if unknown).</summary>
    public string Current(string unitName)
        => !string.IsNullOrEmpty(unitName) && _byUnit.TryGetValue(unitName, out var r) ? r : "";
}
