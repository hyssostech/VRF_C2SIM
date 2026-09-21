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
///
/// *** N4 (D7 harvest, 2026-09-21): THE CHILDREN'S CENTROID IS THE PARENT'S PUBLISHED POSITION. ***
/// The first cut of the sibling pass reused the HEX <see cref="RingOffset"/> of the independent
/// lane, which puts N children on the FIRST N hex slots - bearings 0/60/120 deg for N = 3. Those
/// three bearings do not sum to zero, so their centroid sits 2/3 x spacing x |sum of unit vectors|
/// off the shared coordinate: 233 m for three platoons on a 350 m ring. VR-Forces publishes a
/// COMPOSED AGGREGATE AT ITS MEMBERS' CENTROID, so run D7 measured 114.MechCoy~PXY's published
/// position 262 m from the coordinate its own shell was created at, its route 1,039.3 m instead of
/// 1,112 m, its arrival radius 260 m instead of 278 and its traversal bar 520 m instead of 556.
/// The parent's SHELL never moved - the object the federation sees did.
/// THE FIX IS THE GEOMETRY, not the anchor: N siblings go on ONE ring at EQUAL bearings 360/N
/// apart (<see cref="EqualBearingOffset"/>), whose unit vectors sum to zero for every N >= 2, so
/// the centroid coincides with the shared coordinate to floating-point noise. The RADIUS is then
/// derived from the echelon spacing rather than equal to it - see
/// <see cref="CentroidPreservingRadius"/>. The INDEPENDENT lane (<see cref="Apply"/>) is UNTOUCHED:
/// there the anchor is a real unit that keeps the centre, nothing is published at a centroid, and
/// the hex packing is what keeps the radius bounded as a group grows.
/// </summary>
public static class DeStacker
{
    public sealed record StackGroup(double LatDeg, double LonDeg, int Count);

    /// <summary>One composed-sibling group that was spread: whose children they are, where the
    /// shared coordinate was, how many moved, at which echelon's spacing, the RING RADIUS that
    /// spacing produced (N4: the two are no longer the same number) and - for the start-up line
    /// the ruling asks for - each child's name and how far it went.
    /// <para><paramref name="AnchoredOnParent"/> is true when the anchor is the PARENT's own
    /// current position rather than the children's shared coordinate - the SF-4 case, see
    /// <see cref="ApplyComposedSiblings"/>.</para></summary>
    public sealed record SiblingGroup(string ParentName, double LatDeg, double LonDeg, int Count,
                                      double SpacingMeters, string EchelonKey,
                                      IReadOnlyList<(string Name, double Meters)> Moved,
                                      double RadiusMeters = 0.0,
                                      bool AnchoredOnParent = false);

    /// <summary>A composed-sibling group that was NOT spread, and why - so "nothing happened" is
    /// never silent. Reason is one of: the echelon is not in the table and there is no fallback,
    /// or the siblings do not share a coordinate at all.</summary>
    public sealed record SiblingGroupSkipped(string ParentName, int Count, string Reason);

    /// <summary>
    /// SF-B: TWO SPREAD GROUPS WHOSE RINGS COME TOO CLOSE TO EACH OTHER. <paramref name="Clearance"/>
    /// is the NEAREST APPROACH THE TWO RINGS CAN HAVE - the anchor separation minus both radii - so
    /// it is a LOWER BOUND on the distance between any child of one group and any child of the
    /// other, whatever the rotation. Negative means the rings interpenetrate: some rotation puts
    /// two children on top of each other. <paramref name="Required"/> is the larger of the two
    /// groups' echelon spacings, which is the separation the 2026-09-07 ruling asks for between
    /// units whose formations must not overlap.
    /// </summary>
    public sealed record RingProximity(string ParentA, string ParentB, double AnchorSeparationMeters,
                                       double RadiusAMeters, double RadiusBMeters,
                                       double Clearance, double Required)
    {
        public bool Interpenetrating => Clearance < 0.0;
    }

    /// <summary>
    /// SF-B (cold-start review of 1d0fb69, 2026-09-21): DETECT - AND ONLY DETECT - CROSS-GROUP
    /// RING OVERLAP.
    ///
    /// THE GAP. <see cref="ApplyComposedSiblings"/> sizes each ring so that the minimum separation
    /// WITHIN a group is exactly that group's echelon spacing. Nothing looks ACROSS groups. Two
    /// independent parents 700 m apart, each ringing three platoon children at r = 202.1 m, leave
    /// 700 - 202.1 - 202.1 = 295.8 m between the two rings - under the 350 m the ruling asks for -
    /// and at N &gt;= 6 (r = 350 m at the platoon spacing) the two rings touch or interpenetrate.
    ///
    /// *** NOT LATENT. R9 FULL HAS IT TODAY. *** The first review of this called it "latent - no
    /// shipped fixture has both lanes active on the same init"; measured on the files, that is
    /// false. InitParser's superior cascade puts 113.MechCoy and 114.MechCoy on the SAME
    /// 11.MechBn coordinate - their own group, the 6 companies under 11.MechBn, is skipped for
    /// want of a company echelon row - so each company rings its own platoons about that one
    /// point: two CONCENTRIC rings at 202.1 m and 175.0 m, clearance -377.1 m, children of
    /// different parents 27 m apart radially. R9 lean, COA-STP1 and Iron Storm are clean.
    /// DeStackSelfTest.CheckRingOverlap asserts all four counts (0/1/0/0), so this paragraph is
    /// held by a test rather than by memory. R9 full is off every current runbook/demo path, so
    /// nothing running today is affected; if it is ever put on one, this is the first thing to
    /// settle.
    ///
    /// THIS DOES NOT MOVE ANYTHING. Placement is not redesigned here - a cross-group solve is a
    /// ruling, not a patch, and it would change every shipped fixture's geometry. What it does is
    /// make the condition VISIBLE: a loud WARN naming both parents and the number, at init, and
    /// the same rows in `--parse-init` so it can be seen BEFORE a run rather than derived from one
    /// afterwards.
    ///
    /// PURE. O(n^2) over SPREAD GROUPS - four on the largest shipped init, so the pairs are free.
    /// Groups with a non-positive radius (N &lt;= 1, or a skipped group that never made it here)
    /// are ignored: nothing was moved, so there is no ring.
    /// </summary>
    public static List<RingProximity> FindRingOverlaps(IReadOnlyList<SiblingGroup> groups)
    {
        var hits = new List<RingProximity>();
        if (groups == null) return hits;
        var real = groups.Where(g => g != null && g.RadiusMeters > 0.0).ToList();
        for (int i = 0; i < real.Count; i++)
            for (int j = i + 1; j < real.Count; j++)
            {
                var a = real[i];
                var b = real[j];
                double dLat = (a.LatDeg - b.LatDeg) * MetersPerDegLat;
                double dLon = (a.LonDeg - b.LonDeg) * MetersPerDegLat
                              * Math.Max(Math.Cos(a.LatDeg * Math.PI / 180.0), 0.01);
                double sep = Math.Sqrt(dLat * dLat + dLon * dLon);
                double clearance = sep - a.RadiusMeters - b.RadiusMeters;
                double required = Math.Max(a.SpacingMeters, b.SpacingMeters);
                if (clearance < required)
                    hits.Add(new RingProximity(a.ParentName, b.ParentName, sep,
                                               a.RadiusMeters, b.RadiusMeters, clearance, required));
            }
        return hits;
    }

    /// <summary>
    /// SF-B: the one loud line per offending pair, shared by the service's WARN and `--parse-init`
    /// so the two can never word it differently.
    ///
    /// *** SF-R3 (cold-start review of 2df59ba): TWO HEADLINES, BECAUSE THERE ARE TWO FACTS. ***
    /// This always opened with "CROSS-GROUP RING OVERLAP", including for the 295.9 m clearance of
    /// the branch's own worked example - where nothing overlaps at all. The check is a SEPARATION
    /// rule (clearance below the echelon spacing), not an overlap test, and an operator scanning
    /// WARN headlines at a demo must be able to tell the two apart without reading the body:
    ///   clearance &lt;= 0  -&gt; CROSS-GROUP RINGS INTERPENETRATE (children can land on each other)
    ///   0 &lt; clearance   -&gt; CROSS-GROUP RINGS CLOSER THAN THE ECHELON SPACING (a margin, not a hit)
    /// The threshold and the WARN level are unchanged; only the headline stops overstating.
    /// </summary>
    public static string DescribeRingProximity(RingProximity p)
        => p == null ? "" :
           (p.Interpenetrating
                ? "CROSS-GROUP RINGS INTERPENETRATE: "
                : "CROSS-GROUP RINGS CLOSER THAN THE ECHELON SPACING: ") +
           $"{p.ParentA} and {p.ParentB} are {p.AnchorSeparationMeters:F1} m " +
           $"apart and ring their children at {p.RadiusAMeters:F1} m and {p.RadiusBMeters:F1} m, so the " +
           $"two rings come within {p.Clearance:F1} m of each other - " +
           (p.Interpenetrating
                ? "a NEGATIVE clearance, so two children of different parents can land on top of " +
                  "one another"
                : $"a positive clearance, but under the {p.Required:F0} m separation the 2026-09-07 " +
                  "ruling asks for at this echelon - they do NOT overlap") +
           ". The sibling pass sizes each ring WITHIN its own group and does not look across groups " +
           "(SF-B). Nothing is moved to fix this; it is reported so it is not discovered from a run.";

    /// <summary>
    /// SF-D4 (cold-start review of 1d0fb69, 2026-09-21): THE OPERATOR-FACING DESCRIPTION OF ONE
    /// SPREAD GROUP, with BOTH numbers, each named.
    ///
    /// `--parse-init` used to print "350 m rings". 350 m is the SPACING - the minimum separation
    /// C14 rules on - and it is NOT the distance any child moves. Since N4 the two are different
    /// numbers, <c>r = spacing / (2 sin(pi/N))</c>: 175.0 m at N=2, 202.1 m at N=3, and 350.0 m
    /// only at N=6. An operator who read "350 m rings", ran the file and then measured 202 m was
    /// misled by the one diagnostic whose job is to say what will happen.
    ///
    /// PURE, and factored out of the Console.WriteLine it came from, so the line an operator reads
    /// can be asserted by --destack-selftest without a bridge, a MAK PATH or a federation - the
    /// text itself is what was wrong, so the text is what a test has to be able to see.
    /// </summary>
    public static string DescribeSiblingGroup(SiblingGroup g, int maxNames = 4)
    {
        if (g == null) return "";
        var names = g.Moved ?? Array.Empty<(string Name, double Meters)>();
        string listed = string.Join(", ", names.Take(Math.Max(0, maxNames))
                                               .Select(m => $"{m.Name} {m.Meters:F1} m"));
        return $"{g.Count} child(ren) of {g.ParentName} at {g.LatDeg},{g.LonDeg} -> " +
               $"ring RADIUS {g.RadiusMeters:F1} m (each child moves that far), " +
               $"min sibling SEPARATION {g.SpacingMeters:F0} m ({g.EchelonKey} spacing): " +
               listed + (names.Count > maxNames ? ", ..." : "");
    }

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
    /// FOUR PROPERTIES THIS GUARANTEES, each of them load-bearing:
    ///  1. THE PARENT NEVER MOVES. It is not in its own child list, and no slot is taken from it:
    ///     the children occupy the ring and the shared coordinate - which for the superior
    ///     cascade IS the parent's position - is left to the parent. Every taskee that is a parent
    ///     therefore keeps the position its route is built from (the property --destack-selftest
    ///     asserts on the real fixtures).
    ///  2. *** AND NEITHER DOES ITS PUBLISHED POSITION (N4, D7 harvest). *** The children sit at
    ///     EQUAL bearings 360/N apart, so their CENTROID - which is what VR-Forces publishes for
    ///     the composed aggregate, and therefore what the taskee's route, arrival radius and
    ///     traversal bar are computed from - coincides with the shared coordinate. The hex layout
    ///     this replaces moved it 233 m for three platoons on a 350 m ring and cost R9 lean 74 m
    ///     of route. N = 1 does not move at all (a lone child is not a stack); N = 2 puts the two
    ///     children diametrically opposite.
    ///  3. EVERY child of a spread group moves, so no child is left stacked on the parent's own
    ///     coordinate. That is the difference from <see cref="Apply"/>, where the first unit of a
    ///     group is the anchor and keeps its spot.
    ///  4. DETERMINISTIC: groups in parent-index order, children in plan-index order, one pure
    ///     function of (N, spacing, rotation) for the geometry.
    ///
    /// SPACING is <paramref name="spacingForEchelon"/> applied to the group's children; when they
    /// disagree (a mixed group), the LARGEST of their spacings wins - the conservative direction,
    /// since the criterion is that no two footprints overlap. A group whose echelon the table
    /// cannot size returns a non-positive spacing and is SKIPPED, reported in
    /// <paramref name="skipped"/> rather than silently spread at a number nobody derived. The
    /// spacing is the required MINIMUM SIBLING SEPARATION, and the ring RADIUS is derived from it
    /// by <see cref="CentroidPreservingRadius"/>.
    ///
    /// *** SF-4 (cold-start review of 35a13f2): WHICH POINT THE RING IS BUILT ABOUT. *** The pass
    /// runs AFTER the independent pass so that a parent the independent pass displaced is ringed
    /// at the position it ENDED at - which is what the call site's comment has always claimed and
    /// what the code did NOT do: it anchored on the children's shared CoordKey and never read the
    /// parent's plan at all. With N4 that stopped being cosmetic: the anchor IS the aggregate's
    /// published position, so anchoring away from the parent's shell puts the shell and the
    /// published object in different places. <paramref name="positionsBeforeIndependentPass"/>
    /// closes it WITHOUT changing any shipped fixture: the parent's current position is taken as
    /// the anchor ONLY when the parent SHARED the group's coordinate before the independent pass
    /// ran (the superior-cascade case this feature exists for). Siblings authored on a common
    /// point away from their parent keep ringing THAT point - moving them onto a parent they were
    /// never co-located with is a different change, with no evidence behind it. Null (the
    /// selftest's synthetic calls, and any caller that took no snapshot) = the shared coordinate,
    /// exactly as before.
    /// </summary>
    /// <param name="echelonKeys">Index-parallel with <paramref name="plans"/>:
    /// EchelonSpacing.KeyOf for each planned unit, "" where the init says nothing.</param>
    /// <param name="positionsBeforeIndependentPass">Index-parallel snapshot of
    /// <paramref name="plans"/>' positions taken BEFORE <see cref="Apply"/> ran, or null. See the
    /// SF-4 paragraph above.</param>
    public static List<SiblingGroup> ApplyComposedSiblings(
        IList<CreationPlan> plans,
        IReadOnlyList<(int ParentIndex, IReadOnlyList<int> ChildIndices)> composedGroups,
        IReadOnlyList<string> echelonKeys,
        Func<string, double> spacingForEchelon,
        double rotationDeg,
        out List<SiblingGroupSkipped> skipped,
        IReadOnlyList<Geodetic> positionsBeforeIndependentPass = null)
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
                // SF-4: the parent's CURRENT position is the anchor when the parent shared this
                // group's coordinate before the independent pass ran. See the remarks.
                bool anchoredOnParent = false;
                if (positionsBeforeIndependentPass != null
                    && parentIndex < positionsBeforeIndependentPass.Count
                    && CoordKey(positionsBeforeIndependentPass[parentIndex].LatDeg,
                                positionsBeforeIndependentPass[parentIndex].LonDeg) == kv.Key)
                {
                    anchorLat = plans[parentIndex].Pos.LatDeg;
                    anchorLon = plans[parentIndex].Pos.LonDeg;
                    anchoredOnParent = true;
                }
                double latRad = anchorLat * Math.PI / 180.0;
                double metersPerDegLon = MetersPerDegLat * Math.Max(Math.Cos(latRad), 0.01);
                double radius = CentroidPreservingRadius(members.Count, spacing);
                var moved = new List<(string, double)>(members.Count);
                for (int n = 0; n < members.Count; n++)
                {
                    var (north, east) = EqualBearingOffset(n, members.Count, radius, rotationDeg);
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
                                            spacing, echelon, moved, radius, anchoredOnParent));
            }
        }
        return spread;
    }

    /// <summary>
    /// *** THE RING RADIUS THAT KEEPS THE CENTROID ON THE ANCHOR (N4, D7 harvest). ***
    ///
    /// N points at equal bearings 360/N apart on a circle of radius r have their centroid EXACTLY
    /// at the centre (the unit vectors are the N-th roots of unity and sum to zero for every
    /// N &gt;= 2), and their nearest-neighbour separation is the chord
    /// <c>2 r sin(pi / N)</c>. The ruling's quantity is the SEPARATION - "no two units' default
    /// formations overlap" - so the radius is derived from it rather than set equal to it:
    ///
    ///     r = spacing / (2 sin(pi / N))          minimum sibling separation = spacing, exactly
    ///
    /// | N | r / spacing | r at the 350 m PLATOON spacing | min separation |
    /// |---|-------------|--------------------------------|----------------|
    /// | 1 | -           | not spread at all              | -              |
    /// | 2 | 0.500000    | 175.0 m (opposite each other)  | 350 m          |
    /// | 3 | 0.577350    | 202.1 m                        | 350 m          |
    /// | 4 | 0.707107    | 247.5 m                        | 350 m          |
    /// | 5 | 0.850651    | 297.7 m                        | 350 m          |
    /// | 6 | 1.000000    | 350.0 m                        | 350 m          |
    /// | 7 | 1.152382    | 403.3 m                        | 350 m          |
    /// | 8 | 1.306563    | 457.3 m                        | 350 m          |
    ///
    /// BEYOND ONE RING THERE IS NO SECOND RING, and that is the deliberate difference from the hex
    /// <see cref="RingOffset"/> of the independent lane. A second ring would re-introduce exactly
    /// the defect N4 is about: ring 1 and ring 2 populated to different counts do not have a
    /// centroid at the centre except by coincidence. So every sibling of a group stays on ONE
    /// circle whose radius grows with N - asymptotically <c>r -&gt; spacing x N / (2 pi)</c>, i.e.
    /// about 0.159 x spacing per extra child (a 12-child group is 671 m out at the platoon
    /// spacing, a 20-child group 1,118 m). THE COST IS PAID IN RADIUS, NOT IN OVERLAP. Two
    /// consequences a future session must weigh rather than rediscover: the group's footprint
    /// grows linearly with N, and each child is displaced by r - which is the "phantom travel" a
    /// member materialized after dispatch is credited with (review NOTE-1) and the amount by which
    /// the taskee's own position diverges from its members' (review NOTE-2).
    ///
    /// *** SF-A (cold-start review of 1d0fb69, 2026-09-21). THE REMARK THAT STOOD HERE - "No
    /// shipped fixture has more than 3 composed siblings in one group (R9 full's largest is 3)" -
    /// WAS FALSE, and false in the direction that makes this paragraph's "the footprint grows
    /// linearly with N" sound theoretical. *** MEASURED on the shipped file by
    /// --destack-selftest: R9 full has composed-sibling groups of 4 (Z1.InfCoy), 5 (14.MechBn),
    /// 6 (11.MechBn) and 7 (13.MechBn). They are invisible today only because they are SKIPPED -
    /// COMPANY-and-above have no <see cref="EchelonSpacing"/> row and Vrf:DeStackEchelonFallbackMeters
    /// ships at 0 - not because they are small. ONE key turns them on, and at the ruled 700 m
    /// company spacing those four groups would take rings of 495.0, 595.5, 700.0 and 806.7 m:
    /// the largest child displacement on any shipped fixture would go from 202.1 m to 806.7 m,
    /// and the "phantom travel" and taskee-vs-member divergence above with it. The sizes and the
    /// radii are now ASSERTED against the file (DeStackSelfTest.CheckSkippedSiblingSizes), so
    /// this paragraph cannot go stale again the way it just did; RUNBOOK 11e carries the same
    /// numbers for the operator.
    ///
    /// N &lt;= 1 returns 0: a lone child is not a stack and must not be displaced (and its own
    /// centroid IS the anchor already). A non-positive spacing returns 0 for the same reason
    /// <see cref="Apply"/> no-ops on one.
    /// </summary>
    public static double CentroidPreservingRadius(int count, double spacingMeters)
        => count <= 1 || !(spacingMeters > 0.0)
           ? 0.0
           : spacingMeters / (2.0 * Math.Sin(Math.PI / count));

    /// <summary>
    /// Slot <paramref name="slot"/> of <paramref name="count"/> on ONE ring of radius
    /// <paramref name="radiusMeters"/>, at bearing <c>rotationDeg + 360 x slot / count</c>
    /// clockwise from north (so slot 0 at rotation 0 is due north, matching
    /// <see cref="RingOffset"/>'s first slot and Vrf:DeStackRotationDeg's documented meaning).
    /// Returns (north, east) metres. Pure; the centroid property of
    /// <see cref="CentroidPreservingRadius"/> is a property OF THIS FUNCTION and is asserted
    /// directly by --destack-selftest.
    /// </summary>
    public static (double NorthMeters, double EastMeters) EqualBearingOffset(
        int slot, int count, double radiusMeters, double rotationDeg = 0.0)
    {
        if (count <= 0 || !(radiusMeters > 0.0)) return (0.0, 0.0);
        double angle = 2.0 * Math.PI * slot / count + rotationDeg * Math.PI / 180.0;
        return (radiusMeters * Math.Cos(angle), radiusMeters * Math.Sin(angle));
    }

    /// <summary>
    /// Slot for the n-th DISPLACED unit of a group (n is 1-based; n=0 is the anchor
    /// and never moves). Hex ring k = 1, 2, ... holds 6k slots at radius k*spacing;
    /// cumulative capacity of rings 1..k is 3k(k+1).
    ///
    /// THE INDEPENDENT LANE ONLY since 2026-09-21 (N4). It is correct there and wrong for composed
    /// siblings: the anchor of an independent group is a REAL UNIT that keeps the centre slot and
    /// nothing publishes a centroid over the group, so hex packing's bounded radius is pure gain.
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
