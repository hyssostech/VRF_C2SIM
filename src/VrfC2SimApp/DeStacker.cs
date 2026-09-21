using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// R8 create-time de-stacking (docs/UNIT_MOVEMENT_RESEARCH.md sec 4). Scenario data
/// that spawns many units at LITERALLY identical coordinates (COA-STP1) gridlocks
/// disaggregated-unit geometry: members must form up inside a pile of co-located
/// vehicles and never escape (the R5c finding - dispersed golden 3/3 marched vs
/// stacked COA-STP1 0/6, identical code). This helper spreads each stacked group
/// onto deterministic hexagonal rings BEFORE the creates are issued: the FIRST unit
/// of a group keeps the original position; each subsequent unit takes the next slot
/// on ring k (6k slots at radius k*spacing), so adjacent ring-1 slots sit exactly
/// `spacing` apart. Deterministic (init order in, same offsets out), PURE (no
/// bridge calls - offline-testable via --destack-selftest), and OPT-IN via
/// Vrf:DeStackCreates (it moves units off their source-data positions, so it is
/// deliberately parity-breaking; default off).
///
/// SCOPE, narrowed 2026-09-20 (cold-start review SF2): only units created as INDEPENDENT objects
/// are spread by <see cref="Apply"/>. A unit Vrf:ComposeHierarchy attaches INTO a parent aggregate
/// takes its place from the parent's ORGANIZATION and never takes a 700 m ring slot - see
/// <see cref="CompositionPlan"/> for the rule and its grounds. Before this, R9 lean's three declared
/// platoons of taskee 114.MechCoy were composed into the company FROM 700 m AWAY, on a
/// "co-location" that existed only because InitParser cascades a superior's coordinate onto a unit
/// that has none.
///
/// SCOPE, RE-OPENED 2026-09-21 (user ruling, option C of the D6 harvest): "held with the parent"
/// turned out to mean STACKED ON the parent, because the parent's formation does not separate
/// composed sub-aggregates. D6 measured R9 lean's three MechPlt aggregates 24-56 m apart at every
/// sample from creation to the destination, 3 of 3 footprints overlapping, 137 sub-7 m member pairs
/// against D3's 44 - the 2026-09-07 criterion ("no two units' default formations overlap") violated
/// 3 pairs out of 3. So composed SIBLINGS that share a coordinate are co-located units by the
/// ruling's plain words and <see cref="ApplyComposedSiblings"/> spreads them - around their parent,
/// which never moves, at THEIR echelon's spacing (<see cref="EchelonSpacing"/>), not the company's
/// 700 m. Switchable at Vrf:DeStackComposedSiblings for run-to-run comparability.
/// </summary>
public static class DeStacker
{
    public sealed record StackGroup(double LatDeg, double LonDeg, int Count);

    /// <summary>One composed-sibling group that was spread: whose children they are, where the
    /// shared coordinate was, how many moved, at which echelon's spacing, and - for the start-up
    /// line the ruling asks for - each child's name and how far it went.</summary>
    public sealed record SiblingGroup(string ParentName, double LatDeg, double LonDeg, int Count,
                                      double SpacingMeters, string EchelonKey,
                                      IReadOnlyList<(string Name, double Meters)> Moved);

    /// <summary>A composed-sibling group that was NOT spread, and why - so "nothing happened" is
    /// never silent. Reason is one of: the echelon is not in the table and there is no fallback,
    /// or the siblings do not share a coordinate at all.</summary>
    public sealed record SiblingGroupSkipped(string ParentName, int Count, string Reason);

    private const double MetersPerDegLat = 111_320.0;

    /// <summary>
    /// The plans a de-stack is ENTITLED to move: those created as INDEPENDENT VR-Forces objects.
    ///
    /// C14 spreads units so that no two units' default FORMATIONS overlap (700 m against the 630 m
    /// longest shipped company formation, PREREG_ASSEMBLY_LAYOUT sec 1). That reasoning is about
    /// SIBLINGS. A unit that Vrf:ComposeHierarchy attaches into a parent aggregate has no formation
    /// of its own to keep clear - the PARENT lays it out (UG52 25.2.1) - and displacing it only
    /// separates it from the aggregate whose members STP-837 arrival evidence and the C15/C16 stall
    /// and progress checks sample. A coordinate a unit holds only because InitParser cascaded its
    /// superior's onto it (InitParser.cs:144-153) is not an authored co-location at all.
    ///
    /// A composed child is therefore skipped ENTIRELY: it neither moves, nor anchors a group, nor
    /// consumes a ring slot. <paramref name="composedChildIndices"/> is null (or empty) whenever
    /// Vrf:ComposeHierarchy is off, and then every plan is independent - exactly the pre-2026-09-20
    /// behaviour.
    /// </summary>
    private static bool Independent(int index, IReadOnlySet<int> composedChildIndices)
        => composedChildIndices == null || !composedChildIndices.Contains(index);

    /// <summary>
    /// Grouping key: lat/lon rounded to 1e-6 deg (~0.11 m) - literal identity plus
    /// string-formatting noise. Shared with InitParseCheck so the offline stat and
    /// the runtime behavior always agree.
    /// </summary>
    public static (double Lat, double Lon) CoordKey(double latDeg, double lonDeg)
        => (Math.Round(latDeg, 6), Math.Round(lonDeg, 6));

    /// <summary>
    /// De-stack <paramref name="plans"/> IN PLACE and return the groups that were
    /// spread (2+ INDEPENDENT units at the same CoordKey). Entities and aggregates are treated
    /// alike (both pile up - the R5c entity control needed ~13 min to escape the
    /// stack). Altitude, name, type, force and heading are untouched.
    /// </summary>
    /// <param name="composedChildIndices">Indices of plans that Vrf:ComposeHierarchy will attach
    /// INTO a parent aggregate (CompositionPlan.ComposedChildIndices). They are excluded from the
    /// whole operation - see <see cref="Independent"/>. Null = every plan is independent.</param>
    public static List<StackGroup> Apply(IList<CreationPlan> plans, double spacingMeters, double rotationDeg = 0.0,
                                         IReadOnlySet<int> composedChildIndices = null)
    {
        var groups = new List<StackGroup>();
        if (plans.Count < 2 || spacingMeters <= 0)
            return groups;

        var byCoord = new Dictionary<(double, double), List<int>>();
        for (int i = 0; i < plans.Count; i++)
        {
            if (!Independent(i, composedChildIndices)) continue;   // its place is its parent's
            var key = CoordKey(plans[i].Pos.LatDeg, plans[i].Pos.LonDeg);
            if (!byCoord.TryGetValue(key, out var members))
                byCoord[key] = members = new List<int>();
            members.Add(i);
        }

        // Order groups by first occurrence so log output is deterministic too.
        foreach (var members in byCoord.Values.Where(m => m.Count > 1).OrderBy(m => m[0]))
        {
            var anchor = plans[members[0]].Pos;   // first unit keeps its spot
            double latRad = anchor.LatDeg * Math.PI / 180.0;
            // Clamp the lon scale near the poles; irrelevant for real scenarios but
            // keeps the math finite everywhere.
            double metersPerDegLon = MetersPerDegLat * Math.Max(Math.Cos(latRad), 0.01);

            for (int n = 1; n < members.Count; n++)
            {
                var (north, east) = RingOffset(n, spacingMeters, rotationDeg);
                int idx = members[n];
                var p = plans[idx];
                plans[idx] = p with
                {
                    Pos = new Geodetic
                    {
                        LatDeg = anchor.LatDeg + north / MetersPerDegLat,
                        LonDeg = anchor.LonDeg + east / metersPerDegLon,
                        AltMeters = p.Pos.AltMeters,
                    }
                };
            }
            groups.Add(new StackGroup(anchor.LatDeg, anchor.LonDeg, members.Count));
        }
        return groups;
    }

    /// <summary>
    /// SPREAD COMPOSED SIBLINGS AT THEIR OWN ECHELON'S SCALE (user ruling 2026-09-21, option C).
    ///
    /// For each parent in <paramref name="composedGroups"/> (CompositionPlan.ComposedGroups - the
    /// single classifier), its children are grouped by CoordKey exactly as <see cref="Apply"/>
    /// groups independent units. A group of TWO OR MORE children sharing one coordinate is a
    /// co-location by C14's plain words, whether that coordinate was authored or cascaded onto
    /// them from their superior, and it is spread onto hex rings about that shared coordinate.
    ///
    /// THREE PROPERTIES THIS GUARANTEES, each of them load-bearing:
    ///  1. THE PARENT NEVER MOVES. It is not in its own child list, and no slot is taken from it:
    ///     the children occupy ring slots 1..N and the shared coordinate - which for the superior
    ///     cascade IS the parent's position - is left to the parent. Every taskee that is a parent
    ///     therefore keeps the position its route is built from (the property --destack-selftest
    ///     asserts on the real fixtures).
    ///  2. EVERY child of a spread group moves, so no child is left stacked on the parent's own
    ///     coordinate. That is the difference from <see cref="Apply"/>, where the first unit of a
    ///     group is the anchor and keeps its spot.
    ///  3. DETERMINISTIC: groups in parent-index order, children in plan-index order, the same hex
    ///     <see cref="RingOffset"/> geometry as the independent lane.
    ///
    /// SPACING is <paramref name="spacingForEchelon"/> applied to the group's children; when they
    /// disagree (a mixed group), the LARGEST of their spacings wins - the conservative direction,
    /// since the criterion is that no two footprints overlap. A group whose echelon the table
    /// cannot size returns a non-positive spacing and is SKIPPED, reported in
    /// <paramref name="skipped"/> rather than silently spread at a number nobody derived.
    /// </summary>
    /// <param name="echelonKeys">Index-parallel with <paramref name="plans"/>:
    /// EchelonSpacing.KeyOf for each planned unit, "" where the init says nothing.</param>
    public static List<SiblingGroup> ApplyComposedSiblings(
        IList<CreationPlan> plans,
        IReadOnlyList<(int ParentIndex, IReadOnlyList<int> ChildIndices)> composedGroups,
        IReadOnlyList<string> echelonKeys,
        Func<string, double> spacingForEchelon,
        double rotationDeg,
        out List<SiblingGroupSkipped> skipped)
    {
        var spread = new List<SiblingGroup>();
        skipped = new List<SiblingGroupSkipped>();
        if (plans == null || composedGroups == null || spacingForEchelon == null) return spread;

        foreach (var (parentIndex, childIndices) in composedGroups)
        {
            if (parentIndex < 0 || parentIndex >= plans.Count || childIndices == null) continue;
            string parentName = plans[parentIndex].Name;
            var byCoord = new Dictionary<(double, double), List<int>>();
            foreach (int ci in childIndices)
            {
                if (ci < 0 || ci >= plans.Count || ci == parentIndex) continue;
                var key = CoordKey(plans[ci].Pos.LatDeg, plans[ci].Pos.LonDeg);
                if (!byCoord.TryGetValue(key, out var list)) byCoord[key] = list = new List<int>();
                list.Add(ci);
            }
            foreach (var kv in byCoord.OrderBy(kv => kv.Value[0]))
            {
                var members = kv.Value;
                if (members.Count < 2) continue;
                double spacing = 0.0;
                string echelon = "";
                foreach (int ci in members)
                {
                    string key = echelonKeys != null && ci < echelonKeys.Count ? echelonKeys[ci] ?? "" : "";
                    double s = spacingForEchelon(key);
                    if (s > spacing) { spacing = s; echelon = key; }
                }
                if (!(spacing > 0.0))
                {
                    skipped.Add(new SiblingGroupSkipped(parentName, members.Count,
                        "no echelon spacing could be derived for these siblings (their C2SIM echelon is " +
                        "not one the table covers and Vrf:DeStackEchelonFallbackMeters is 0) - they stay " +
                        "on their parent's coordinate, as before the 2026-09-21 ruling"));
                    continue;
                }
                double anchorLat = kv.Key.Item1, anchorLon = kv.Key.Item2;
                double latRad = anchorLat * Math.PI / 180.0;
                double metersPerDegLon = MetersPerDegLat * Math.Max(Math.Cos(latRad), 0.01);
                var moved = new List<(string, double)>(members.Count);
                for (int n = 0; n < members.Count; n++)
                {
                    var (north, east) = RingOffset(n + 1, spacing, rotationDeg);   // slot 0 is the parent's
                    int idx = members[n];
                    var p = plans[idx];
                    plans[idx] = p with
                    {
                        Pos = new Geodetic
                        {
                            LatDeg = anchorLat + north / MetersPerDegLat,
                            LonDeg = anchorLon + east / metersPerDegLon,
                            AltMeters = p.Pos.AltMeters,
                        }
                    };
                    moved.Add((p.Name, Math.Sqrt(north * north + east * east)));
                }
                spread.Add(new SiblingGroup(parentName, anchorLat, anchorLon, members.Count,
                                            spacing, echelon, moved));
            }
        }
        return spread;
    }

    /// <summary>
    /// Slot for the n-th DISPLACED unit of a group (n is 1-based; n=0 is the anchor
    /// and never moves). Hex ring k = 1, 2, ... holds 6k slots at radius k*spacing;
    /// cumulative capacity of rings 1..k is 3k(k+1).
    /// </summary>
    /// <param name="rotationDeg">Rotates the whole hex pattern about the anchor (clockwise from
    /// north, degrees; default 0). Every displaced unit then lands on DIFFERENT ground with the
    /// same neighbours and spacing - the lever for the terrain test of 2026-09-07
    /// (PREREG_ASSEMBLY_LAYOUT 3e: "rotate the unit placements to verify your terrain theory").</param>
    public static (double NorthMeters, double EastMeters) RingOffset(int n, double spacingMeters, double rotationDeg = 0.0)
    {
        int k = 1;
        while (3 * k * (k + 1) < n)
            k++;
        int j = n - 3 * (k - 1) * k - 1;          // 0-based slot index on ring k
        double angle = 2.0 * Math.PI * j / (6 * k) + rotationDeg * Math.PI / 180.0;
        double r = k * spacingMeters;
        return (r * Math.Cos(angle), r * Math.Sin(angle));
    }
}
