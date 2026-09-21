using System.Collections.Generic;

namespace VrfC2SimApp;

/// <summary>
/// WHAT AN INITIALIZATION IS ABOUT TO CREATE, COUNTED ONCE (N15, run D9 2026-09-21).
///
/// THE DEFECT THIS EXISTS TO END. Two lines in the same init used to count the same plan list
/// with two different rules and print two different answers:
///   - the C13 line counted only the aggregates IT had just flipped to CreateSubordinates=false
///     and then printed `toCreate.Count - shells` as "platform(s) created in full". A COMPOSED
///     PARENT was already false (ApplyHierarchyComposition set it), so it was not in `shells` and
///     fell into the "platforms" term. On R9 lean that printed "4 unit(s) created as EMPTY shells
///     ... 2 platform(s) created in full" for an init with FIVE aggregates and ONE platform.
///   - the INIT CREATION BARRIER line counted `IsAggregate and not CreateSubordinates` over the
///     same final list and correctly printed 5.
/// The C13 line's 4 is what the D9 prereg registered as its expected string, so a CORRECT run
/// scored a MISS on a HIGH limb (d9_harvest_report.md sec 2.2). Both lines now read their numbers
/// from this one class, so they cannot disagree again.
///
/// COUNTED OVER THE FINAL PLAN LIST - after ApplyHierarchyComposition and after the AtOrder flip -
/// which is the same list RecordInitCreationBarrier is handed (VrfC2SimService: the C13 block and
/// RecordInitCreationBarrier(toCreate) act on the same `toCreate`).
///
/// PURE: no logging, no state. Locked by --routeorigin-selftest sec 5.
/// </summary>
public readonly record struct CreationCensus(int Total, int Aggregates, int Platforms, int EmptyShells)
{
    /// <summary>Aggregates that WILL carry their template's members out of the create.</summary>
    public int AggregatesWithMembers => Aggregates - EmptyShells;

    public static CreationCensus Of(IReadOnlyList<CreationPlan> plans)
    {
        if (plans == null) return new CreationCensus(0, 0, 0, 0);
        int aggregates = 0, shells = 0;
        for (int i = 0; i < plans.Count; i++)
        {
            if (!plans[i].IsAggregate) continue;
            aggregates++;
            if (!plans[i].CreateSubordinates) shells++;
        }
        return new CreationCensus(plans.Count, aggregates, plans.Count - aggregates, shells);
    }
}
