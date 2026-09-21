using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// WHICH PLANNED UNITS ARE CREATED AS INDEPENDENT OBJECTS, AND WHICH ARE COMPOSED INTO A PARENT.
///
/// Extracted 2026-09-20 from VrfC2SimService.ApplyHierarchyComposition so that exactly ONE
/// definition of "composed child" exists. Two callers need the same answer and must not be able
/// to drift apart:
///   1. ApplyHierarchyComposition, which flips each parent aggregate to an EMPTY shell and
///      registers a PendingComposition so every declared child is attached with AddToOrganization
///      once created (the vendor-sample recipe).
///   2. The create-time DE-STACK (C14), which must NOT displace a unit whose place in the world
///      comes from its parent's formation rather than from its own coordinate.
///
/// WHY THE DE-STACK CARES (the rule, and its grounds).
/// C14 (user ruling 2026-09-07, docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md:131-138) spreads CO-LOCATED
/// units so that "no two units' default formations can overlap whatever their heading" - the value
/// 700 m is chosen against the longest shipped company formation, 630 m
/// (docs/experiments/PREREG_ASSEMBLY_LAYOUT_2026-09-07.md sec 1). That argument is about SIBLING
/// units that each lay their own members out at their own coordinate. It says nothing about a unit
/// and its own parent, and the vendor's own rule is the opposite one: "a unit created from the
/// panel gets its members laid out in its default formation" (UG52 25.2.1, quoted in the same
/// prereg sec 0) - i.e. a composed member's place is the PARENT'S to decide, not the init file's.
///
/// So the rule this class encodes:
///   DE-STACK APPLIES TO UNITS CREATED AS INDEPENDENT OBJECTS THAT SHARE A COORDINATE WITH ANOTHER
///   INDEPENDENT OBJECT. A unit that ComposeHierarchy composes into a parent aggregate takes its
///   place FROM the parent (formation), so it is never displaced, and it never occupies a ring slot.
///
/// A position that exists only because of the SUPERIOR CASCADE is not an authored co-location.
/// InitParser gives a unit with no coordinates its superior's (InitParser.cs:144-153, the C++
/// parity behaviour), so a company whose platoons carry no position of their own arrives here as a
/// "pile" of four units at one point that the export never authored as a pile. Spreading those
/// three platoons 700 m off their company is not C14 - it separates an aggregate from the members
/// STP-837 arrival evidence and the C15/C16 member-position checks sample
/// (VrfC2SimService.TryReadMemberPositions).
///
/// PURE: no bridge call, no logging, no state. Locked by --destack-selftest.
/// </summary>
public sealed class CompositionPlan
{
    private CompositionPlan(HashSet<string> parentUuids, List<int> nonAggregateParentIndices,
                            HashSet<int> composedChildIndices,
                            List<(int ParentIndex, IReadOnlyList<int> ChildIndices)> composedGroups)
    {
        ParentUuids = parentUuids;
        NonAggregateParentIndices = nonAggregateParentIndices;
        ComposedChildIndices = composedChildIndices;
        ComposedGroups = composedGroups;
    }

    /// <summary>The uuids of SURVIVING AGGREGATE units that some other surviving unit names as its
    /// Superior. Each becomes an empty shell and takes its declared children by
    /// AddToOrganization.</summary>
    public IReadOnlySet<string> ParentUuids { get; }

    /// <summary>Indices of plans that have declared children but are NOT aggregates: they cannot
    /// compose, are created as-is, and their children become standalone (the caller warns). They
    /// are NOT in <see cref="ParentUuids"/>, so their children are INDEPENDENT objects and the
    /// de-stack treats them as such.</summary>
    public IReadOnlyList<int> NonAggregateParentIndices { get; }

    /// <summary>Indices of plans that will be ATTACHED INTO a parent aggregate. These are never
    /// spread by the INDEPENDENT-object de-stack (their place is inside their parent's
    /// organization, not at an authored coordinate of their own).</summary>
    public IReadOnlySet<int> ComposedChildIndices { get; }

    /// <summary>
    /// THE SAME CHILDREN, GROUPED BY THEIR PARENT - (parent plan index, its child plan indices),
    /// ordered by parent index and then by child index so the answer is identical run to run.
    ///
    /// This is what the COMPOSED-SIBLING de-stack acts on (user ruling 2026-09-21, option C):
    /// siblings that share a coordinate are co-located units by C14's plain words - the parent's
    /// formation does NOT separate them (D6 measured the three MechPlt aggregates 24-56 m apart
    /// at every sample, 3 of 3 footprints overlapping) - so they are spread around the PARENT,
    /// which stays where it is, at a spacing taken from THEIR echelon (<see cref="EchelonSpacing"/>).
    /// One classifier, two lanes: nothing outside this class decides who is a composed child of whom.
    /// </summary>
    public IReadOnlyList<(int ParentIndex, IReadOnlyList<int> ChildIndices)> ComposedGroups { get; }

    /// <summary>True when this plan index is created as an INDEPENDENT VR-Forces object - the set
    /// the de-stack acts on.</summary>
    public bool IsIndependent(int planIndex) => !ComposedChildIndices.Contains(planIndex);

    /// <summary>
    /// Classify <paramref name="plans"/> against the declared C2SIM Superior chain in
    /// <paramref name="hierarchy"/> (index-parallel: (this unit's uuid, its Superior's uuid)).
    /// An empty plan is returned when the lists disagree in length or the init is FLAT (nobody
    /// names a survivor as Superior) - in both cases every unit is independent, which is the
    /// pre-2026-09-20 behaviour and the safe direction.
    /// </summary>
    public static CompositionPlan Classify(IReadOnlyList<CreationPlan> plans,
                                           IReadOnlyList<(string Uuid, string SuperiorUuid)> hierarchy)
    {
        var empty = new CompositionPlan(new HashSet<string>(StringComparer.Ordinal),
                                        new List<int>(), new HashSet<int>(),
                                        new List<(int, IReadOnlyList<int>)>());
        if (plans == null || hierarchy == null || plans.Count != hierarchy.Count || plans.Count == 0)
            return empty;

        var survivorUuids = new HashSet<string>(
            hierarchy.Select(h => h.Uuid).Where(u => !string.IsNullOrEmpty(u)), StringComparer.Ordinal);
        // A unit is a PARENT CANDIDATE iff some SURVIVING unit names it as Superior. A Superior
        // outside the file (R9 lean's 1.BdeHQ names 271aa71b, which is not there) composes nothing.
        var parentUuids = new HashSet<string>(
            hierarchy.Where(h => !string.IsNullOrEmpty(h.SuperiorUuid) && survivorUuids.Contains(h.SuperiorUuid))
                     .Select(h => h.SuperiorUuid), StringComparer.Ordinal);
        if (parentUuids.Count == 0) return empty;

        // Only an AGGREGATE can take children. A platform with declared children is created as-is
        // and its children stand alone - so they ARE independent objects and the de-stack owns them.
        var nonAggregate = new List<int>();
        for (int i = 0; i < plans.Count; i++)
        {
            string uuid = hierarchy[i].Uuid;
            if (string.IsNullOrEmpty(uuid) || !parentUuids.Contains(uuid)) continue;
            if (plans[i].IsAggregate) continue;
            nonAggregate.Add(i);
            parentUuids.Remove(uuid);
        }

        // The index of every surviving PARENT aggregate, so a child can be grouped under the plan
        // it will be attached to (not merely under its superior's uuid).
        var parentIndexByUuid = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < plans.Count; i++)
        {
            string uuid = hierarchy[i].Uuid;
            if (!string.IsNullOrEmpty(uuid) && parentUuids.Contains(uuid) && !parentIndexByUuid.ContainsKey(uuid))
                parentIndexByUuid[uuid] = i;
        }

        var children = new HashSet<int>();
        var byParent = new Dictionary<int, List<int>>();
        for (int i = 0; i < plans.Count; i++)
        {
            string sup = hierarchy[i].SuperiorUuid;
            if (string.IsNullOrEmpty(sup) || !parentUuids.Contains(sup)) continue;
            children.Add(i);
            if (!parentIndexByUuid.TryGetValue(sup, out int pi)) continue;
            if (!byParent.TryGetValue(pi, out var list)) byParent[pi] = list = new List<int>();
            list.Add(i);
        }
        var groups = byParent.OrderBy(kv => kv.Key)
                             .Select(kv => (kv.Key, (IReadOnlyList<int>)kv.Value.OrderBy(i => i).ToList()))
                             .ToList();
        return new CompositionPlan(parentUuids, nonAggregate, children, groups);
    }
}
