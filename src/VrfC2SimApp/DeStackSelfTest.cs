using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of DeStacker (R8 create-time de-stacking; no bridge start, no MAK
/// runtime, no VR-Forces): `VrfC2SimApp --destack-selftest`. Asserts the grouping,
/// the deterministic ring geometry, and the no-op paths
/// (docs/UNIT_MOVEMENT_RESEARCH.md sec 4).
/// </summary>
public static class DeStackSelfTest
{
    private const double Spacing = 50.0;

    public static int Run()
    {
        int failures = 0;

        // 1. All-distinct coordinates: nothing grouped, nothing moved.
        {
            var plans = new List<CreationPlan> { Plan("a", 34.0, -116.0), Plan("b", 34.1, -116.1), Plan("c", 35.0, -117.0) };
            var before = plans.ToList();
            var groups = DeStacker.Apply(plans, Spacing);
            Check(ref failures, groups.Count == 0, "distinct coords -> no stack groups");
            Check(ref failures, plans.SequenceEqual(before), "distinct coords -> plans untouched");
        }

        // 2. Two units at one spot: first keeps its position, second lands one spacing away.
        {
            var plans = new List<CreationPlan> { Plan("a", 34.5, -116.5), Plan("b", 34.5, -116.5) };
            var groups = DeStacker.Apply(plans, Spacing);
            Check(ref failures, groups.Count == 1 && groups[0].Count == 2, "2 stacked -> 1 group of 2");
            Check(ref failures, plans[0].Pos.LatDeg == 34.5 && plans[0].Pos.LonDeg == -116.5,
                  "first unit of a group keeps its exact position");
            double d = DistMeters(plans[0].Pos, plans[1].Pos);
            Check(ref failures, Math.Abs(d - Spacing) < Spacing * 0.01,
                  $"second unit is one spacing away (got {d:F2} m)");
        }

        // 3. Eight units at one spot: center + 6 ring-1 slots + 1 ring-2 slot; all
        //    positions distinct; ring-1 slots one spacing from center AND from each
        //    neighbor (hex geometry: chord == radius at 6 slots).
        {
            var plans = Enumerable.Range(0, 8).Select(i => Plan($"u{i}", 34.68, -116.72)).ToList();
            var groups = DeStacker.Apply(plans, Spacing);
            Check(ref failures, groups.Count == 1 && groups[0].Count == 8, "8 stacked -> 1 group of 8");

            var keys = plans.Select(p => DeStacker.CoordKey(p.Pos.LatDeg, p.Pos.LonDeg)).Distinct().Count();
            Check(ref failures, keys == 8, $"all 8 de-stacked positions are distinct (got {keys})");

            bool ring1Radii = Enumerable.Range(1, 6).All(i =>
                Math.Abs(DistMeters(plans[0].Pos, plans[i].Pos) - Spacing) < Spacing * 0.01);
            Check(ref failures, ring1Radii, "ring-1 slots (units 1-6) sit one spacing from the anchor");

            bool ring1Chords = Enumerable.Range(1, 6).All(i =>
            {
                int next = i == 6 ? 1 : i + 1;
                return Math.Abs(DistMeters(plans[i].Pos, plans[next].Pos) - Spacing) < Spacing * 0.02;
            });
            Check(ref failures, ring1Chords, "adjacent ring-1 slots sit one spacing apart");

            double d7 = DistMeters(plans[0].Pos, plans[7].Pos);
            Check(ref failures, Math.Abs(d7 - 2 * Spacing) < Spacing * 0.02,
                  $"unit 7 overflows to ring 2 at two spacings (got {d7:F2} m)");
        }

        // 4. Deterministic: the same input yields the same output, twice.
        {
            List<CreationPlan> Make() => new()
            {
                Plan("a", 34.68, -116.72), Plan("b", 34.68, -116.72), Plan("c", 34.9, -116.9),
                Plan("d", 34.68, -116.72), Plan("e", 34.9, -116.9),
            };
            var run1 = Make(); DeStacker.Apply(run1, Spacing);
            var run2 = Make(); DeStacker.Apply(run2, Spacing);
            Check(ref failures, run1.SequenceEqual(run2), "same input -> identical de-stacked output");
        }

        // 5. Independent groups: each stack gets its own rings; the un-stacked unit is untouched.
        {
            var plans = new List<CreationPlan>
            {
                Plan("a1", 34.0, -116.0), Plan("b1", 35.0, -117.0), Plan("solo", 36.0, -118.0),
                Plan("a2", 34.0, -116.0), Plan("b2", 35.0, -117.0),
            };
            var groups = DeStacker.Apply(plans, Spacing);
            Check(ref failures, groups.Count == 2 && groups.All(g => g.Count == 2),
                  "two separate stacks -> two groups of 2");
            Check(ref failures, plans[2].Pos.LatDeg == 36.0 && plans[2].Pos.LonDeg == -118.0,
                  "un-stacked unit is untouched");
            Check(ref failures, Math.Abs(DistMeters(plans[0].Pos, plans[3].Pos) - Spacing) < Spacing * 0.01
                             && Math.Abs(DistMeters(plans[1].Pos, plans[4].Pos) - Spacing) < Spacing * 0.01,
                  "each group is de-stacked around its own anchor");
        }

        // 6. Longitude scaling: at lat 60 the east-west degree shrinks (cos 60 = 0.5);
        //    ground distance must still be one spacing.
        {
            var plans = new List<CreationPlan> { Plan("a", 60.0, 20.0), Plan("b", 60.0, 20.0) };
            DeStacker.Apply(plans, Spacing);
            double d = DistMeters(plans[0].Pos, plans[1].Pos);
            Check(ref failures, Math.Abs(d - Spacing) < Spacing * 0.01,
                  $"lat-60 ground distance is one spacing (got {d:F2} m)");
        }

        // 7. Grouping tolerance: within 1e-6 deg rounding -> same group; 100 m apart -> not grouped.
        {
            var near = new List<CreationPlan> { Plan("a", 34.500000, -116.500000), Plan("b", 34.5000004, -116.5000004) };
            Check(ref failures, DeStacker.Apply(near, Spacing).Count == 1,
                  "coords equal after 1e-6 rounding are grouped");
            var far = new List<CreationPlan> { Plan("a", 34.5, -116.5), Plan("b", 34.5009, -116.5) }; // ~100 m north
            Check(ref failures, DeStacker.Apply(far, Spacing).Count == 0,
                  "coords ~100 m apart are NOT grouped");
        }

        // 8. Only Pos.Lat/Lon change: altitude, name, type, force, heading survive the move.
        {
            var moved = new CreationPlan(true, new EntityTypeSpec { Kind = 11, Domain = 1, Country = 225, Category = 5, Subcategory = 2, Specific = 0, Extra = 0 },
                                         Force.Opposing, 42.0, "keeper",
                                         new Geodetic { LatDeg = 34.5, LonDeg = -116.5, AltMeters = 123.0 }, 7.0);
            var plans = new List<CreationPlan> { Plan("anchor", 34.5, -116.5), moved };
            DeStacker.Apply(plans, Spacing);
            var m = plans[1];
            Check(ref failures, m.Pos.AltMeters == 123.0 && m.Name == "keeper" && m.IsAggregate
                             && m.Force == Force.Opposing && m.HeadingDeg == 42.0 && m.PostCreateAltitude == 7.0,
                  "de-stacking changes only lat/lon (alt/name/type/force/heading kept)");
        }

        // 9. No-op guards: spacing <= 0 and single-plan lists change nothing.
        {
            var plans = new List<CreationPlan> { Plan("a", 34.5, -116.5), Plan("b", 34.5, -116.5) };
            var before = plans.ToList();
            Check(ref failures, DeStacker.Apply(plans, 0).Count == 0 && plans.SequenceEqual(before),
                  "spacing 0 -> no-op");
            var one = new List<CreationPlan> { Plan("a", 34.5, -116.5) };
            Check(ref failures, DeStacker.Apply(one, Spacing).Count == 0, "single plan -> no-op");

            // Rotation (2026-09-07 terrain test lever): 0 deg = the old geometry; 90 deg maps the
            // first ring-1 slot from due north to due east; 360 deg = identity; the anchor never moves.
            var (n0, e0) = DeStacker.RingOffset(1, Spacing);
            var (n90, e90) = DeStacker.RingOffset(1, Spacing, 90.0);
            var (n360, e360) = DeStacker.RingOffset(1, Spacing, 360.0);
            Check(ref failures, Math.Abs(n0 - Spacing) < 1e-9 && Math.Abs(e0) < 1e-9, "rotation 0: slot 1 due north at one spacing");
            Check(ref failures, Math.Abs(n90) < 1e-9 && Math.Abs(e90 - Spacing) < 1e-9, "rotation 90: slot 1 due east at one spacing");
            Check(ref failures, Math.Abs(n360 - n0) < 1e-9 && Math.Abs(e360 - e0) < 1e-9, "rotation 360 = identity");
            List<CreationPlan> Stack8() => Enumerable.Range(0, 8).Select(i => Plan("u" + i, 34.5, -116.5)).ToList();
            var rotA = Stack8(); DeStacker.Apply(rotA, Spacing, 0.0);
            var rotB = Stack8(); DeStacker.Apply(rotB, Spacing, 45.0);
            Check(ref failures, rotA[0].Pos.LatDeg == rotB[0].Pos.LatDeg && rotA[0].Pos.LonDeg == rotB[0].Pos.LonDeg,
                  "rotation keeps the anchor in place");
            Check(ref failures, Enumerable.Range(1, rotA.Count - 1).All(i => rotA[i].Pos.LatDeg != rotB[i].Pos.LatDeg || rotA[i].Pos.LonDeg != rotB[i].Pos.LonDeg),
                  "rotation 45 moves every displaced unit");
        }

        // 10. COMPOSED CHILDREN TAKE NO 700 m RING SLOT (SF2, 2026-09-20) - AND, SINCE THE USER
        //     RULING OF 2026-09-21, SIBLINGS THAT SHARE A COORDINATE ARE SPREAD AT THEIR OWN
        //     ECHELON'S SCALE AROUND THE PARENT (option C of the D6 harvest). The RULE, on
        //     synthetic plans, before the fixtures exercise it.
        {
            Console.WriteLine("  --- C14 scope: a composed child takes no INDEPENDENT ring slot ---");
            // A company at X with three declared platoons that the superior cascade also put at X,
            // plus ONE unrelated independent unit at X. Only the company and the stranger are
            // independent, so exactly ONE unit moves: the stranger.
            var plans = new List<CreationPlan>
            {
                Agg("coy",  34.5, -116.5),   // 0 parent aggregate
                Agg("plt1", 34.5, -116.5),   // 1 declared child
                Agg("plt2", 34.5, -116.5),   // 2 declared child
                Agg("plt3", 34.5, -116.5),   // 3 declared child
                Agg("other",34.5, -116.5),   // 4 an INDEPENDENT unit on the same coordinate
            };
            var hier = new List<(string, string)>
            { ("u0", ""), ("u1", "u0"), ("u2", "u0"), ("u3", "u0"), ("u4", "") };
            var comp = CompositionPlan.Classify(plans, hier);
            Check(ref failures, comp.ComposedChildIndices.Count == 3
                             && comp.ComposedChildIndices.Contains(1)
                             && comp.ComposedChildIndices.Contains(2)
                             && comp.ComposedChildIndices.Contains(3),
                  $"the three declared children of an aggregate parent are classified as COMPOSED " +
                  $"(got {comp.ComposedChildIndices.Count})");
            Check(ref failures, !comp.ComposedChildIndices.Contains(0) && !comp.ComposedChildIndices.Contains(4),
                  "the parent shell and an unrelated unit are INDEPENDENT");
            Check(ref failures, comp.ComposedGroups.Count == 1
                             && comp.ComposedGroups[0].ParentIndex == 0
                             && comp.ComposedGroups[0].ChildIndices.Count == 3,
                  $"the SAME classifier groups them by parent: {comp.ComposedGroups.Count} group(s), " +
                  $"parent index {(comp.ComposedGroups.Count > 0 ? comp.ComposedGroups[0].ParentIndex : -1)}, " +
                  $"{(comp.ComposedGroups.Count > 0 ? comp.ComposedGroups[0].ChildIndices.Count : 0)} child(ren)");
            var groups = DeStacker.Apply(plans, Spacing, 0.0, comp.ComposedChildIndices);
            Check(ref failures, groups.Count == 1 && groups[0].Count == 2,
                  $"the INDEPENDENT group is the 2 independent units, not all 5 (got {groups.Count} group(s) of " +
                  $"{(groups.Count > 0 ? groups[0].Count : 0)})");
            Check(ref failures, plans[1].Pos.LatDeg == 34.5 && plans[2].Pos.LatDeg == 34.5
                             && plans[3].Pos.LatDeg == 34.5 && plans[1].Pos.LonDeg == -116.5,
                  "no composed child took a 700 m INDEPENDENT ring slot (the SF2 rule, unchanged: the " +
                  "independent pass does not see them at all)");
            Check(ref failures, Math.Abs(DistMeters(plans[0].Pos, plans[4].Pos) - Spacing) < Spacing * 0.01,
                  "the co-located INDEPENDENT unit IS still spread (C14 is not weakened)");

            // *** THE 2026-09-21 RULING. This block REPLACES the assertion that used to stand here,
            // "NO composed child is displaced - it is laid out by its parent's formation". That
            // assertion encoded SF2's reading of C14 and D6 refuted its premise: the parent's
            // formation does NOT separate composed sub-aggregates (three platoon aggregates 24-56 m
            // apart at every sample, 3 of 3 footprints overlapping, 137 sub-7 m member pairs vs
            // D3's 44). The children are now spread AT THEIR OWN ECHELON'S SPACING, around a parent
            // that does not move. ***
            Console.WriteLine("  --- C14 echelon scope (user ruling 2026-09-21): siblings spread around the parent ---");
            var ech = new List<string> { "", EchelonSpacing.Platoon, EchelonSpacing.Platoon,
                                         EchelonSpacing.Platoon, "" };
            var parentBefore = plans[0].Pos;
            double pltSpacing = EchelonSpacing.TableMeters[EchelonSpacing.Platoon];
            var sib = DeStacker.ApplyComposedSiblings(plans, comp.ComposedGroups, ech,
                                                      k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                      out var skipped);
            Check(ref failures, sib.Count == 1 && sib[0].Count == 3
                             && Math.Abs(sib[0].SpacingMeters - pltSpacing) < 1e-9
                             && sib[0].EchelonKey == EchelonSpacing.Platoon && skipped.Count == 0,
                  $"3 composed siblings at one coordinate -> ONE group spread at the PLATOON spacing " +
                  $"{pltSpacing:F0} m (got {sib.Count} group(s), spacing " +
                  $"{(sib.Count > 0 ? sib[0].SpacingMeters : 0):F0} m, echelon " +
                  $"'{(sib.Count > 0 ? sib[0].EchelonKey : "")}', {skipped.Count} skipped)");
            Check(ref failures, plans[0].Pos.LatDeg == parentBefore.LatDeg
                             && plans[0].Pos.LonDeg == parentBefore.LonDeg,
                  "THE PARENT DID NOT MOVE - it keeps the centre slot, so a taskee's route still " +
                  "starts where the init put it");
            // *** N4 (D7 harvest). THIS BLOCK REPLACES THE ASSERTION THAT USED TO STAND HERE,
            // "every one of the 3 children is exactly one platoon spacing (350 m) from the parent".
            // That assertion pinned the HEX geometry, and D7 refuted the premise behind it: the
            // ring radius was never the quantity C14 rules on - the SEPARATION is - and putting
            // three children on hex bearings 0/60/120 moved the parent's PUBLISHED position (its
            // members' centroid) 233 m, costing R9 lean 74 m of route. What is pinned instead is
            // strictly stronger: the separations are still exactly one spacing, AND the centroid
            // stays on the parent. The radius is now 202.1 m, and that is a derived number, not
            // a ruled one. ***
            double pltRadius = DeStacker.CentroidPreservingRadius(3, pltSpacing);
            bool allOnRing = Enumerable.Range(1, 3).All(i =>
                Math.Abs(DistMeters(parentBefore, plans[i].Pos) - pltRadius) < pltRadius * 0.01);
            Check(ref failures, allOnRing,
                  $"every one of the 3 children sits on ONE ring of the derived radius " +
                  $"{pltRadius:F1} m = {pltSpacing:F0} / (2 sin 60 deg) - none is left stacked on the " +
                  "parent, and the radius is derived from the spacing rather than equal to it (N4)");
            // SF-D4: THE LINE `--parse-init` PRINTS, asserted as text. It used to say "350 m
            // rings" - the SPACING, not the radius - so an operator was told 350 m and measured
            // 202.1 m. Both numbers are now named, and this arm is what stops the label drifting
            // back onto the wrong quantity. The FIRST assertion is the one that fails on the old
            // line; the second is the one that fails if the radius is dropped for brevity.
            string desc3 = DeStacker.DescribeSiblingGroup(sib[0]);
            Console.WriteLine($"    --parse-init would print: {desc3}");
            Check(ref failures,
                  desc3.Contains($"ring RADIUS {pltRadius:F1} m", StringComparison.Ordinal)
                  && desc3.Contains($"min sibling SEPARATION {pltSpacing:F0} m", StringComparison.Ordinal)
                  && !desc3.Contains($"{pltSpacing:F0} m rings", StringComparison.Ordinal),
                  $"SF-D4: the --parse-init sibling line names BOTH numbers - ring RADIUS " +
                  $"{pltRadius:F1} m AND min sibling SEPARATION {pltSpacing:F0} m - and never calls " +
                  $"the spacing a ring (got: {desc3})");
            Check(ref failures,
                  sib[0].Moved.All(m => desc3.Contains($"{m.Name} {m.Meters:F1} m", StringComparison.Ordinal))
                  && sib[0].Moved.All(m => Math.Abs(m.Meters - pltRadius) < 0.05),
                  "SF-D4: and each child's printed displacement is the one the pass APPLIED, which " +
                  "is the radius, not a third derivation of it");
            // N = 2 is the case where reading the spacing as a radius is most wrong (175.0 m
            // against 350 m), so the formatter is exercised there too rather than at N = 3 alone.
            var twoMoved = new List<(string Name, double Meters)> { ("A", 175.0), ("B", 175.0) };
            var g2 = new DeStacker.SiblingGroup("P", 1.0, 2.0, 2, pltSpacing,
                                                EchelonSpacing.Platoon, twoMoved,
                                                DeStacker.CentroidPreservingRadius(2, pltSpacing));
            string desc2 = DeStacker.DescribeSiblingGroup(g2);
            Check(ref failures,
                  desc2.Contains("ring RADIUS 175.0 m", StringComparison.Ordinal)
                  && desc2.Contains("min sibling SEPARATION 350 m", StringComparison.Ordinal),
                  $"SF-D4: at N = 2 the line reads 175.0 m radius against a 350 m separation - the " +
                  $"two numbers are half an order apart and must not share a label (got: {desc2})");
            bool pairsClear = true;
            for (int a = 1; a <= 3; a++)
                for (int b = a + 1; b <= 3; b++)
                    if (DistMeters(plans[a].Pos, plans[b].Pos) < pltSpacing * 0.99) pairsClear = false;
            Check(ref failures, pairsClear,
                  $"and every sibling PAIR is at least {pltSpacing:F0} m apart - which is what C14's " +
                  $"criterion asks for at this echelon (longest shipped platoon formation " +
                  $"{EchelonSpacing.SpanMeters[EchelonSpacing.Platoon]:F1} m). THE SEPARATION IS THE " +
                  "RULED QUANTITY; the radius is derived from it");
            // THE N4 PROPERTY ITSELF: the children's CENTROID - which is what VR-Forces publishes
            // for the composed aggregate - is the parent's own coordinate.
            var centroid = Centroid(plans.Skip(1).Take(3).Select(p => p.Pos));
            double centroidOff = DistMeters(parentBefore, centroid);
            Check(ref failures, centroidOff < 1.0,
                  $"*** N4: the three children's CENTROID is {centroidOff:F3} m from the parent's own " +
                  "coordinate. VR-Forces publishes a composed aggregate AT its members' centroid, so " +
                  "this is the position the taskee's route, arrival radius and traversal bar are all " +
                  "built from. The hex layout this replaces put it 233 m away (run D7 measured 262 m " +
                  "live and a route 74 m short) ***");
            Check(ref failures, Math.Abs(DistMeters(plans[0].Pos, plans[4].Pos) - Spacing) < Spacing * 0.01,
                  "the INDEPENDENT unit's 700 m-lane position is untouched by the sibling pass");

            // Deterministic, and the OFF switch is the old behaviour byte for byte.
            {
                var again = new List<CreationPlan>
                {
                    Agg("coy", 34.5, -116.5), Agg("plt1", 34.5, -116.5),
                    Agg("plt2", 34.5, -116.5), Agg("plt3", 34.5, -116.5), Agg("other", 34.5, -116.5),
                };
                var c2 = CompositionPlan.Classify(again, hier);
                DeStacker.Apply(again, Spacing, 0.0, c2.ComposedChildIndices);
                DeStacker.ApplyComposedSiblings(again, c2.ComposedGroups, ech,
                                                k => EchelonSpacing.SpacingFor(k, 0.0), 0.0, out _);
                Check(ref failures, again.SequenceEqual(plans),
                      "the sibling spread is DETERMINISTIC - same input, identical output");

                var held = new List<CreationPlan>
                {
                    Agg("coy", 34.5, -116.5), Agg("plt1", 34.5, -116.5),
                    Agg("plt2", 34.5, -116.5), Agg("plt3", 34.5, -116.5), Agg("other", 34.5, -116.5),
                };
                var c3 = CompositionPlan.Classify(held, hier);
                DeStacker.Apply(held, Spacing, 0.0, c3.ComposedChildIndices);
                Check(ref failures, held[1].Pos.LatDeg == 34.5 && held[2].Pos.LatDeg == 34.5
                                 && held[3].Pos.LatDeg == 34.5,
                      "Vrf:DeStackComposedSiblings OFF (the sibling pass simply not called) reproduces " +
                      "the 2026-09-20 SF2 behaviour exactly - the children stay on the parent");

                // THE DOCUMENTED FALLBACK: an echelon the table cannot size is NOT spread at a
                // number nobody derived. This is the ExpandCoarseLeaves case (synthesized sub-units
                // carry no C2SIM echelon at all).
                var unknown = new List<CreationPlan>
                {
                    Agg("coy", 34.5, -116.5), Agg("k1", 34.5, -116.5), Agg("k2", 34.5, -116.5),
                    Agg("k3", 34.5, -116.5), Agg("other", 34.5, -116.5),
                };
                var c4 = CompositionPlan.Classify(unknown, hier);
                var noEch = new List<string> { "", "", "", "", "" };
                var none = DeStacker.ApplyComposedSiblings(unknown, c4.ComposedGroups, noEch,
                                                           k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                           out var skipped2);
                Check(ref failures, none.Count == 0 && skipped2.Count == 1 && skipped2[0].Count == 3
                                 && unknown[1].Pos.LatDeg == 34.5,
                      "FALLBACK 0 (the default): siblings whose echelon the table cannot size are NOT " +
                      "moved, and the skip is REPORTED so the log says why nothing happened");
                var withFallback = DeStacker.ApplyComposedSiblings(unknown, c4.ComposedGroups, noEch,
                                                                   k => EchelonSpacing.SpacingFor(k, 500.0),
                                                                   0.0, out _);
                Check(ref failures, withFallback.Count == 1
                                 && Math.Abs(withFallback[0].SpacingMeters - 500.0) < 1e-9,
                      "Vrf:DeStackEchelonFallbackMeters=500 spreads that same group at 500 m - the " +
                      "fallback is a documented lever, not a hidden default");
            }

            // A parent that is NOT an aggregate cannot compose: its children are created standalone
            // and ARE therefore independent objects the de-stack owns. The arm that must not rot.
            var flat = new List<CreationPlan> { Plat("veh", 34.5, -116.5), Agg("kid", 34.5, -116.5) };
            var flatComp = CompositionPlan.Classify(flat, new List<(string, string)> { ("u0", ""), ("u1", "u0") });
            Check(ref failures, flatComp.ComposedChildIndices.Count == 0
                             && flatComp.NonAggregateParentIndices.Count == 1,
                  "a NON-aggregate parent composes nothing - its child stays an independent object");
            Check(ref failures, DeStacker.Apply(flat, Spacing, 0.0, flatComp.ComposedChildIndices).Count == 1,
                  "and that child IS de-stacked");

            // ComposeHierarchy OFF (null set) = the pre-2026-09-20 behaviour, unchanged.
            var off = new List<CreationPlan>
            { Agg("coy", 34.5, -116.5), Agg("plt1", 34.5, -116.5), Agg("plt2", 34.5, -116.5) };
            Check(ref failures, DeStacker.Apply(off, Spacing, 0.0, null) is { Count: 1 } g0 && g0[0].Count == 3,
                  "with ComposeHierarchy off every plan is independent (null exclusion set = old behaviour)");
        }

        // 10b. THE N4 RING GEOMETRY, AS ARITHMETIC (user ruling 2026-09-21 + the D7 harvest).
        {
            Console.WriteLine("  --- N4: the centroid-preserving ring, N = 1..8 ---");
            const double S = 350.0;   // the PLATOON spacing, the one that matters live
            // THE TABLE THE REPORT CARRIES. r = spacing / (2 sin(pi/N)), so the CHORD between
            // adjacent slots is exactly the spacing at every N. Re-derived here from the function,
            // never copied from the comment.
            var expected = new (int N, double Ratio)[]
            {
                (2, 0.5), (3, 0.5773502692), (4, 0.7071067812), (5, 0.8506508084),
                (6, 1.0), (7, 1.1523824354), (8, 1.3065629649),
            };
            foreach (var (n, ratio) in expected)
            {
                double r = DeStacker.CentroidPreservingRadius(n, S);
                Check(ref failures, Math.Abs(r - ratio * S) < 0.01,
                      $"N = {n}: radius {r:F1} m = {ratio:F6} x the {S:F0} m spacing " +
                      $"(expected {ratio * S:F1} m)");
            }
            Check(ref failures, DeStacker.CentroidPreservingRadius(1, S) == 0.0
                             && DeStacker.CentroidPreservingRadius(0, S) == 0.0
                             && DeStacker.CentroidPreservingRadius(3, 0.0) == 0.0,
                  "N = 1 (and N = 0, and a non-positive spacing) gives radius 0 - a lone child is not " +
                  "a stack and MUST NOT be displaced; its own centroid already is the anchor");
            // The two properties that matter, checked for every N from 2 to 12 at once:
            //   (i)  the centroid of the N slots is the anchor, to floating-point noise;
            //   (ii) the MINIMUM pairwise separation is exactly the spacing.
            bool centroidHolds = true, minSepHolds = true;
            var sepAtN = new List<string>();
            for (int n = 2; n <= 12; n++)
            {
                double r = DeStacker.CentroidPreservingRadius(n, S);
                double sumN = 0, sumE = 0, minSep = double.MaxValue;
                var pts = new List<(double N, double E)>();
                for (int i = 0; i < n; i++)
                {
                    var (north, east) = DeStacker.EqualBearingOffset(i, n, r, 0.0);
                    pts.Add((north, east));
                    sumN += north; sumE += east;
                }
                if (Math.Sqrt(sumN * sumN + sumE * sumE) / n > 1e-9) centroidHolds = false;
                for (int a = 0; a < n; a++)
                    for (int b = a + 1; b < n; b++)
                        minSep = Math.Min(minSep, Math.Sqrt(Math.Pow(pts[a].N - pts[b].N, 2)
                                                            + Math.Pow(pts[a].E - pts[b].E, 2)));
                if (Math.Abs(minSep - S) > 1e-6) minSepHolds = false;
                sepAtN.Add($"N={n}: r {r:F1} m");
            }
            Check(ref failures, centroidHolds,
                  "for EVERY N from 2 to 12 the N slots' centroid is the anchor to under 1e-9 m - the " +
                  "N-th roots of unity sum to zero, which is the whole reason the bearings are equal");
            Check(ref failures, minSepHolds,
                  $"...and the MINIMUM pairwise separation is exactly the {S:F0} m spacing at every one " +
                  $"of them. Radii: [{string.Join(", ", sepAtN)}] - ONE ring, growing with N " +
                  "(asymptotically spacing x N / 2 pi); there is deliberately no second ring, because " +
                  "two unequally-filled rings have no centroid at the centre");
            // Rotation is the START BEARING, exactly as Vrf:DeStackRotationDeg documents.
            var (n0, e0) = DeStacker.EqualBearingOffset(0, 3, 100.0, 0.0);
            var (n90, e90) = DeStacker.EqualBearingOffset(0, 3, 100.0, 90.0);
            Check(ref failures, Math.Abs(n0 - 100.0) < 1e-9 && Math.Abs(e0) < 1e-9
                             && Math.Abs(n90) < 1e-9 && Math.Abs(e90 - 100.0) < 1e-9,
                  "slot 0 at rotation 0 is due north and at rotation 90 due east - Vrf:DeStackRotationDeg " +
                  "is the START BEARING of the ring, the same meaning it has in the independent lane");
            // N = 2 is opposite, by construction rather than by luck.
            {
                var two = new List<CreationPlan>
                { Agg("coy", 34.5, -116.5), Agg("p1", 34.5, -116.5), Agg("p2", 34.5, -116.5) };
                var hier2 = new List<(string, string)> { ("u0", ""), ("u1", "u0"), ("u2", "u0") };
                var c = CompositionPlan.Classify(two, hier2);
                var before2 = two[0].Pos;
                DeStacker.ApplyComposedSiblings(two, c.ComposedGroups,
                    new List<string> { "", EchelonSpacing.Platoon, EchelonSpacing.Platoon },
                    k => EchelonSpacing.SpacingFor(k, 0.0), 0.0, out _);
                double sep = DistMeters(two[1].Pos, two[2].Pos);
                double off = DistMeters(before2, Centroid(new[] { two[1].Pos, two[2].Pos }));
                Check(ref failures, Math.Abs(sep - S) < S * 0.01 && off < 1.0
                                 && two[0].Pos.LatDeg == before2.LatDeg,
                      $"N = 2: the two children sit DIAMETRICALLY OPPOSITE, {sep:F1} m apart (one " +
                      $"spacing) with their centroid {off:F3} m from the parent, which has not moved");
            }
        }

        // 10c. SF-4: WHICH POINT THE RING IS BUILT ABOUT, when the independent pass moved the parent.
        {
            Console.WriteLine("  --- SF-4: a parent the INDEPENDENT pass displaced ---");
            // The latent case no shipped fixture reaches: a parent aggregate co-located with
            // another INDEPENDENT unit, and not first in that group - so the independent pass moves
            // the PARENT. Its children must then ring where the parent ENDED, which is what the
            // call site's comment has always claimed and what the code did not do (it anchored on
            // the children's shared CoordKey and never read plans[parentIndex]).
            var plans = new List<CreationPlan>
            {
                Agg("stranger", 34.5, -116.5),   // 0 independent, FIRST in the group -> keeps its spot
                Agg("coy",      34.5, -116.5),   // 1 the parent, independent, SECOND -> is displaced
                Agg("plt1",     34.5, -116.5),   // 2..4 its composed children, on the same coordinate
                Agg("plt2",     34.5, -116.5),
                Agg("plt3",     34.5, -116.5),
            };
            var hier = new List<(string, string)>
            { ("u0", ""), ("u1", ""), ("u2", "u1"), ("u3", "u1"), ("u4", "u1") };
            var comp = CompositionPlan.Classify(plans, hier);
            var ech = new List<string> { "", "", EchelonSpacing.Platoon, EchelonSpacing.Platoon,
                                         EchelonSpacing.Platoon };
            var snapshot = plans.Select(p => p.Pos).ToList();     // BEFORE the independent pass
            DeStacker.Apply(plans, 700.0, 0.0, comp.ComposedChildIndices);
            var parentAfter = plans[1].Pos;
            Check(ref failures, DistMeters(snapshot[1], parentAfter) > 690.0,
                  $"the independent pass DID move the parent ({DistMeters(snapshot[1], parentAfter):F0} m) - " +
                  "without that this arm would prove nothing");
            var sib = DeStacker.ApplyComposedSiblings(plans, comp.ComposedGroups, ech,
                                                      k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                      out _, snapshot);
            var kids = Centroid(plans.Skip(2).Take(3).Select(p => p.Pos));
            Check(ref failures, sib.Count == 1 && sib[0].AnchoredOnParent
                             && DistMeters(parentAfter, kids) < 1.0,
                  $"SF-4 FIXED: the children ring the parent's POST-de-stack position - their centroid is " +
                  $"{DistMeters(parentAfter, kids):F3} m from it and {DistMeters(snapshot[1], kids):F0} m " +
                  "from the coordinate the init authored. Before this the comment said one thing and the " +
                  "code did the other, which N4 turned from cosmetic into a shell published 700 m from " +
                  "its own members' centroid");
            // ...and the OTHER reading is preserved: siblings authored on a common point that is NOT
            // their parent's keep ringing THAT point. Moving them onto a parent they were never
            // co-located with is a different change, and no evidence asks for it.
            var apart = new List<CreationPlan>
            {
                Agg("coy",  34.5,   -116.5),     // 0 the parent, somewhere else entirely
                Agg("plt1", 34.60,  -116.5),     // 1..3 the children, on a common point of their own
                Agg("plt2", 34.60,  -116.5),
                Agg("plt3", 34.60,  -116.5),
            };
            var hierA = new List<(string, string)> { ("u0", ""), ("u1", "u0"), ("u2", "u0"), ("u3", "u0") };
            var compA = CompositionPlan.Classify(apart, hierA);
            var snapA = apart.Select(p => p.Pos).ToList();
            var sharedA = snapA[1];
            var sibA = DeStacker.ApplyComposedSiblings(apart, compA.ComposedGroups, ech.Take(4).Select(
                                                           (_, i) => i == 0 ? "" : EchelonSpacing.Platoon).ToList(),
                                                       k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                       out _, snapA);
            var kidsA = Centroid(apart.Skip(1).Take(3).Select(p => p.Pos));
            Check(ref failures, sibA.Count == 1 && !sibA[0].AnchoredOnParent
                             && DistMeters(sharedA, kidsA) < 1.0
                             && apart[0].Pos.LatDeg == 34.5,
                  "and siblings authored on a COMMON POINT AWAY FROM THEIR PARENT still ring that point " +
                  $"(centroid {DistMeters(sharedA, kidsA):F3} m from it), not the parent - the group " +
                  "reports AnchoredOnParent=false and the log says which it was");
        }

        // 10d. THE REVIEW NOTE (NOTE-3 / the N4 brief): can a parent's OWN formation overlap its
        //      child ring? ANSWERED FROM THE CODE, not invented.
        {
            Console.WriteLine("  --- the review note: a parent with ORGANIC members AND composed children ---");
            // ApplyHierarchyComposition (VrfC2SimService.cs:1930) flips EVERY parent that takes
            // composed children to CreateSubordinates = false - an EMPTY SHELL - and C13
            // (MaterializeAtOrder) does the same to every aggregate. So under the only
            // configuration in which composed groups exist at all, a composed parent HAS NO ORGANIC
            // MEMBERS: its members ARE its children, and there is no second formation to overlap
            // the ring. The case in the review note is unreachable by construction, and this arm is
            // what would notice if that ever changed.
            var plans = new List<CreationPlan>
            { Agg("coy", 34.5, -116.5), Agg("plt1", 34.5, -116.5), Agg("plt2", 34.5, -116.5) };
            var hier = new List<(string, string)> { ("u0", ""), ("u1", "u0"), ("u2", "u0") };
            var comp = CompositionPlan.Classify(plans, hier);
            Check(ref failures, comp.ComposedGroups.Count == 1 && plans[0].CreateSubordinates,
                  "the CLASSIFIER itself does not flip the shell - that is the service's job " +
                  "(ApplyHierarchyComposition), and this arm only records where the property lives");
            // The property as the service establishes it, applied here the same way.
            for (int i = 0; i < plans.Count; i++)
                if (comp.ComposedGroups.Any(g => g.ParentIndex == i))
                    plans[i] = plans[i] with { CreateSubordinates = false };
            Check(ref failures, !plans[0].CreateSubordinates,
                  "EVERY parent of a composed group is created as an EMPTY SHELL (CreateSubordinates " +
                  "= false), so it has NO organic members of its own and no formation that could " +
                  "overlap its children's ring. The review's 'a parent with organic members plus " +
                  "composed children' is UNREACHABLE in this configuration - reported, not designed for");
        }

        // 11. THE REAL INITS AT THE BASE PROFILE'S SPACING (2026-09-20).
        //
        // WHY: C14 (user ruling 2026-09-07) says co-located units are spread at init on 5.2, and
        // appsettings.Demo.json has said so since. A RUNNER-LAUNCHED app never loads the Demo
        // overlay - scripts\RunC2SimScenario.ps1 sets no DOTNET_ENVIRONMENT - so the base
        // appsettings.json now carries DeStackCreates/700 too. That is a change to WHERE UNITS ARE
        // on every runner run, so it owes a measurement on the actual fixtures rather than an
        // argument. The numbers below are COUNTED FROM THE FILES, not assumed.
        //
        // EVERY COUNT IS MEASURED WITH Vrf:ComposeHierarchy ON (appsettings.json:34, the default),
        // which is the configuration every runner run uses, and through the SAME CompositionPlan
        // the service calls - not a second copy of the classification.
        {
            Console.WriteLine("  --- the shipped inits at the base profile's 700 m spacing (C14) ---");
            const double Base = 700.0;   // appsettings.json DeStackSpacingMeters
            // R9 LEAN: ZERO UNITS MOVE. The raw XML has no duplicate coordinates; InitParser's
            // SUPERIOR CASCADE gives a unit with no coordinates its superior's
            // (InitParser.cs:144-153, C++ parity), which used to build one "pile" of four -
            // 114.MechCoy and its three DECLARED platoons. Those three are the company's own
            // composed members (ComposeHierarchy on by default), so under C14 as ruled they are
            // laid out by the company's formation and are never displaced. Nothing else in this
            // init shares a coordinate, so the de-stack is a NO-OP on the R9 rehearsal - which is
            // what makes the next run comparable to the D1/D1b/D3 controls that ran without it.
            CheckInit(ref failures, "R9_Mojave_Lean_Initialization.xml", Base,
                      expectGroups: 0, expectMoved: 0, expectSiblingGroups: 1, expectSiblingMoved: 3);
            CheckInit(ref failures, "R9_Mojave_Initialization.xml", Base,
                      expectGroups: 0, expectMoved: 0, expectSiblingGroups: 4, expectSiblingMoved: 11);
            // COA-STP1 IS co-located, heavily - it is the pathology C14 was ruled against ("STP
            // puts a whole COA on its assembly point"). Anyone who expected this init to be
            // untouched should read the ruling, not weaken the check. UNCHANGED by the SF2 scope
            // rule in the RUNNER configuration, which is what makes PREREG_ASSEMBLY_LAYOUT's
            // confirmed 2026-09-07 result still the result of this build.
            CheckInit(ref failures, "COA-STP1_Initialization.xml", Base, expectGroups: 10, expectMoved: 62,
                      expectSiblingGroups: 0, expectSiblingMoved: 0);
            // The real STP export: 40 units, 36 placeable, the superior cascade piles the 28ID
            // subtree onto one coordinate.
            CheckInit(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml", Base,
                      expectGroups: 2, expectMoved: 12, expectSiblingGroups: 0, expectSiblingMoved: 0);

            // THE SAME FOUR FIXTURES IN THE OTHER SHIPPED MODE. FidelityTable maps a brigade or a
            // division to a REAL aggregate template where RealTemplates' 5.0.2 parity dispatch
            // falls through to a single Tank (a PLATFORM, which can compose nothing), so WHICH
            // units are composed children - and therefore which the de-stack may touch - is
            // different. Both modes ship; both are measured rather than reasoned about.
            Console.WriteLine("  --- the same fixtures under Vrf:TypeMappingMode=FidelityTable (Demo) ---");
            CheckInit(ref failures, "R9_Mojave_Lean_Initialization.xml", Base,
                      expectGroups: 0, expectMoved: 0, expectSiblingGroups: 1, expectSiblingMoved: 3,
                      mode: TypeMapping.FidelityTable);
            CheckInit(ref failures, "R9_Mojave_Initialization.xml", Base,
                      expectGroups: 0, expectMoved: 0, expectSiblingGroups: 4, expectSiblingMoved: 11,
                      mode: TypeMapping.FidelityTable);
            CheckInit(ref failures, "COA-STP1_Initialization.xml", Base,
                      expectGroups: 10, expectMoved: 62, expectSiblingGroups: 0, expectSiblingMoved: 0,
                      mode: TypeMapping.FidelityTable);
            CheckInit(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml", Base,
                      expectGroups: 2, expectMoved: 12, expectSiblingGroups: 0, expectSiblingMoved: 0,
                      mode: TypeMapping.FidelityTable);

            // *** THE CHECK THAT DECIDES WHETHER A RUN MOVES: DO THE ORDER'S TASKEES SHIFT? ***
            // A context unit spread onto a ring changes the picture but nothing that is measured;
            // a TASKEE spread 700 m changes where its route starts, which changes the route the
            // pre-flight scores, the arrival radius and the traversal bar. R9's three taskees
            // (R9_Mojave_UnitMove_Order.xml) are the D3 control's whole population.
            CheckTaskeesUnmoved(ref failures, "R9_Mojave_Lean_Initialization.xml",
                                "R9_Mojave_UnitMove_Order.xml", Base);
            CheckTaskeesUnmoved(ref failures, "R9_Mojave_Initialization.xml",
                                "R9_Mojave_UnitMove_Order.xml", Base);
            // ... and what the 2026-09-21 SIBLING pass does to the same taskees. R9 LEAN - the
            // fixture of the D6 control and of the confirming run - moves NONE. R9 FULL moves ONE,
            // 1222.MechPlt, because in that file it is a declared child of 122.MechCoy.
            CheckTaskeesAfterSiblingSpread(ref failures, "R9_Mojave_Lean_Initialization.xml",
                                           "R9_Mojave_UnitMove_Order.xml", Base, expectMovedTaskees: 0);
            CheckTaskeesAfterSiblingSpread(ref failures, "R9_Mojave_Initialization.xml",
                                           "R9_Mojave_UnitMove_Order.xml", Base, expectMovedTaskees: 1);
            CheckTaskeesAfterSiblingSpread(ref failures, "COA-STP1_Initialization.xml",
                                           "COA-STP1_Order.xml", Base, expectMovedTaskees: 0);
            // *** AND THE ARM THE COLD-START REVIEW ADDED: NO MEMBER OF A TASKEE MOVES EITHER. ***
            // STP-837 arrival evidence and the C15/C16 stall/progress checks all sample MEMBER
            // positions (VrfC2SimService.TryReadMemberPositions), so a taskee whose own members
            // were spread 700 m away is a different experiment even though the taskee itself sat
            // still. The taskee check above is necessary and was never sufficient.
            CheckTaskeeMembersUnmoved(ref failures, "R9_Mojave_Lean_Initialization.xml",
                                      "R9_Mojave_UnitMove_Order.xml", Base);
            CheckTaskeeMembersUnmoved(ref failures, "R9_Mojave_Initialization.xml",
                                      "R9_Mojave_UnitMove_Order.xml", Base);
            CheckTaskeeMembersUnmoved(ref failures, "COA-STP1_Initialization.xml",
                                      "COA-STP1_Order.xml", Base);
            // *** SF-5 (cold-start review of 35a13f2): IRON STORM HAD NO TASKEE CHECK AT ALL. ***
            // It was the only shipped fixture with neither arm, while the build report claimed
            // "0 taskees move" for it and PREREG_IRONSTORM_CUTA_DRAFT.md:106 claimed the opposite
            // ("de-stack can move 28ID's own taskee instance by up to 700 m, so every distance
            // below is +/- 700 m"). The two cannot both be right and the run at stake was the Iron
            // Storm one. The order used is the FULL EXPORT that ships on main; the derived cut-A
            // order lives on feat/ironstorm-cut-a and no selftest reads a fixture off another
            // branch. 28ID sorts FIRST by uuid ordinal in its co-location group, so it is that
            // group's ANCHOR and keeps its coordinate - measured here rather than argued.
            CheckTaskeesUnmoved(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml",
                                "STP-IRON-STORM-SYNTHETIC_Order.xml", Base);
            CheckTaskeesAfterSiblingSpread(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml",
                                           "STP-IRON-STORM-SYNTHETIC_Order.xml", Base,
                                           expectMovedTaskees: 0);
            CheckTaskeeMembersUnmoved(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml",
                                      "STP-IRON-STORM-SYNTHETIC_Order.xml", Base);

            // *** N4: THE INDEPENDENT LANE IS UNTOUCHED, PROVED PER FIXTURE. ***
            // COA-STP1 (10 groups / 62 moved) and Iron Storm (2 / 12) have NO composed sibling
            // groups at all, so the whole N4 change must be a literal no-op on them: every plan
            // position after BOTH passes is bit-for-bit the position the INDEPENDENT pass left.
            // That is a stronger statement than "the group counts did not change" and it is the
            // one the ruling's confirmed 2026-09-07 result depends on.
            CheckSiblingPassIsNoOp(ref failures, "COA-STP1_Initialization.xml", Base);
            CheckSiblingPassIsNoOp(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml", Base);

            // *** N4: R9 LEAN, THE NEXT LIVE RUN'S FIXTURE - THE CENTROID AND THE ROUTE LENGTH. ***
            CheckComposedCentroidAndRoute(ref failures, "R9_Mojave_Lean_Initialization.xml",
                                          "R9_Mojave_UnitMove_Order.xml", Base);
        }

        // 12. THE ECHELON TABLE (user ruling 2026-09-21, option C). The ruling's own arithmetic is
        //     "spacing > the longest shipped formation span for that echelon"; these checks re-run
        //     that arithmetic on the constants instead of trusting them, and pin the echelon
        //     vocabulary the C2SIM inits actually use.
        {
            Console.WriteLine("  --- the echelon spacing table and its arithmetic ---");
            foreach (var kv in EchelonSpacing.SpanMeters)
            {
                double table = EchelonSpacing.TableMeters[kv.Key];
                Check(ref failures, table > kv.Value && Math.Abs(table - EchelonSpacing.StepAbove(kv.Value)) < 1e-9,
                      $"{kv.Key}: the table's {table:F0} m is the next 50 m step strictly above the " +
                      $"longest shipped {kv.Key.ToLowerInvariant()} formation span {kv.Value:F1} m " +
                      $"(re-derived here, not copied: StepAbove = {EchelonSpacing.StepAbove(kv.Value):F0} m)");
            }
            Check(ref failures, EchelonSpacing.TableMeters[EchelonSpacing.Platoon] == 350.0,
                  "PLATOON is 350 m - the D6 harvest's '~300 m, VERIFY it' resolved against the vendor " +
                  "data: Formation-Column-US-Army-Mech-Plt-w-IFV.frm spans 320.9 m with its leader " +
                  "chain resolved, so 300 m would NOT clear it and 350 m does");
            Check(ref failures, EchelonSpacing.StepAbove(660.0) == 700.0,
                  "the same rule applied to the longest shipped COMPANY formation " +
                  "(Formation-Column-Armor-Co(US), 660 m resolved - the ruling's 630 m read the raw " +
                  "offsets, not the chain) returns exactly the 700 m the user ruled on 2026-09-07");
            Check(ref failures, EchelonSpacing.StepAbove(250.0) == 300.0 && EchelonSpacing.StepAbove(1.0) == 100.0,
                  "an exact 50 m multiple is still CLEARED (250 -> 300, never 250) and the floor is 100 m");
            // The echelon vocabulary of the shipped inits: EchelonCode first, SIDC as the fallback.
            Check(ref failures, EchelonSpacing.KeyOf("PLT", "") == EchelonSpacing.Platoon
                             && EchelonSpacing.KeyOf("SECT", "") == EchelonSpacing.Section
                             && EchelonSpacing.KeyOf("SQUAD", "") == EchelonSpacing.Squad
                             && EchelonSpacing.KeyOf("TEAM", "") == EchelonSpacing.Team,
                  "the C2SIM EchelonCode values PLT/SECT/SQUAD/TEAM map to their table rows");
            Check(ref failures, EchelonSpacing.KeyOf("COY", "").Length == 0
                             && EchelonSpacing.KeyOf("BN", "").Length == 0
                             && EchelonSpacing.KeyOf("BDE", "").Length == 0
                             && EchelonSpacing.KeyOf("NOS", "").Length == 0
                             && EchelonSpacing.KeyOf("", "").Length == 0,
                  "COY/BN/BDE/NOS and a missing code are NOT in the table - company and above keep the " +
                  "RULED Vrf:DeStackSpacingMeters, and this lane does not re-rule it");
            // SIDC position 12 (0-based 11) is the echelon character - the same index
            // UnitTypeMap.EchelonCharOf reads (TypeMapSelfTest: "SFGPUCIZ--EH---" is echelon H).
            Check(ref failures, EchelonSpacing.KeyOf("", "SFGPUCIZ--ED---") == EchelonSpacing.Platoon
                             && EchelonSpacing.KeyOf("", "SFGPUCIZ--EE---").Length == 0
                             && EchelonSpacing.KeyOf("", "SFGPUCIZ--EH---").Length == 0,
                  "with no EchelonCode the SIDC echelon character decides: D = platoon (in the table), " +
                  "E = company and H = brigade (not)");
            Check(ref failures, EchelonSpacing.SpacingFor("", 0.0) == 0.0
                             && EchelonSpacing.SpacingFor("", 777.0) == 777.0
                             && EchelonSpacing.SpacingFor(EchelonSpacing.Platoon, 777.0) == 350.0,
                  "SpacingFor: an uncovered echelon returns the caller's FALLBACK (0 = do not spread), " +
                  "a covered one ignores it");
            var over = EchelonSpacing.WithOverrides("PLT=400, SECT=250", out string note);
            Check(ref failures, over[EchelonSpacing.Platoon] == 400.0 && over[EchelonSpacing.Section] == 250.0
                             && EchelonSpacing.TableMeters[EchelonSpacing.Platoon] == 350.0
                             && note.Contains("PLATOON=400"),
                  $"Vrf:DeStackEchelonSpacingMeters overrides a row without mutating the derived table " +
                  $"({note})");
            var bad = EchelonSpacing.WithOverrides("COY=900,PLT=nonsense,=5", out string badNote);
            Check(ref failures, bad[EchelonSpacing.Platoon] == 350.0 && badNote.Contains("IGNORED"),
                  $"an override the table cannot take is IGNORED AND NAMED, never silently applied " +
                  $"({badNote})");

            // *** SF-3 (cold-start review of 35a13f2): THE TABLE IS THE GROUND SUBSET, AND KeyOf
            // MUST NOT HAND AN AIR UNIT A GROUND ROW IN SILENCE. *** The builder's own
            // frm_by_template.py returns SECTION 3,000 m (Air Section) and SQUAD 12,649 m (Fighter
            // Squadron) when the ground restriction is lifted, so the shipped rows are an order of
            // magnitude too small for an air echelon - and before this the SIDC echelon character
            // alone decided, whatever the battle dimension said.
            Console.WriteLine("  --- SF-3: the GROUND subset, the domain gate and the provenance ---");
            Check(ref failures, EchelonSpacing.BattleDimensionOf("SFGPUCIZ--ED---") == 'G'
                             && EchelonSpacing.BattleDimensionOf("SFAPMFQM--ED---") == 'A'
                             && EchelonSpacing.BattleDimensionOf("SFSPCLCV--ED---") == 'S'
                             && EchelonSpacing.BattleDimensionOf("SF") == '\0',
                  "the SIDC BATTLE DIMENSION is position 3 (index 2) - G ground, A air, S sea surface - " +
                  "and a too-short symbol id has none. (The ECHELON is position 12; two different fields)");
            string airKey = EchelonSpacing.KeyOf("PLT", "SFAPMFQM--ED---", out string airWhy);
            Check(ref failures, airKey.Length == 0 && airWhy.Contains("BATTLE DIMENSION")
                             && airWhy.Contains("GROUND subset"),
                  $"an AIR platoon gets NO ROW and the provenance says why: \"{airWhy}\". Before SF-3 it " +
                  "took the 350 m ground PLATOON row in silence");
            string seaKey = EchelonSpacing.KeyOf("SECT", "SFSPCLCV--EC---", out _);
            string subKey = EchelonSpacing.KeyOf("", "SFUPSCF---EC---", out _);
            string spaceKey = EchelonSpacing.KeyOf("SQUAD", "SFPPT-----EB---", out _);
            Check(ref failures, seaKey.Length == 0 && subKey.Length == 0 && spaceKey.Length == 0,
                  "sea surface (S), subsurface (U) and space (P) are refused the same way - only the " +
                  "GROUND rows exist and only ground units may take them");
            string groundKey = EchelonSpacing.KeyOf("PLT", "SFGPUCIZ--ED---", out string groundWhy);
            Check(ref failures, groundKey == EchelonSpacing.Platoon && groundWhy.Contains("GROUND PLATOON row")
                             && groundWhy.Contains("350 m") && groundWhy.Contains("is ground"),
                  $"a GROUND platoon takes the 350 m row and says so: \"{groundWhy}\"");
            string sofKey = EchelonSpacing.KeyOf("PLT", "SFFPN-----ED---", out _);
            Check(ref failures, sofKey == EchelonSpacing.Platoon,
                  "SOF (F) takes the ground row - the SOF templates in this catalogue ARE ground");
            string noSidcKey = EchelonSpacing.KeyOf("PLT", "", out string noSidcWhy);
            Check(ref failures, noSidcKey == EchelonSpacing.Platoon && noSidcWhy.Contains("ground is assumed"),
                  $"NO SIDC at all keeps the pre-SF-3 answer - the ground row, with the assumption " +
                  $"STATED rather than hidden: \"{noSidcWhy}\". Every shipped fixture is ground, so " +
                  "this is the path they all take and none of their counts move");
            string coyKey = EchelonSpacing.KeyOf("COY", "SFGPUCA---EE---", out string coyWhy);
            Check(ref failures, coyKey.Length == 0 && coyWhy.Contains("NO ROW"),
                  $"company and above still get NO ROW, for the old reason, and say which field decided: " +
                  $"\"{coyWhy}\"");
            Check(ref failures, EchelonSpacing.KeyOf("PLT", "SFGPUCIZ--ED---")
                                == EchelonSpacing.KeyOf("PLT", "SFGPUCIZ--ED---", out _),
                  "the two-argument overload is the three-argument one - one rule, not two copies");
        }

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// One real init, parsed and planned THE WAY ProcessInitializationLocked plans it: one
    /// CreationPlan per unit that has a uuid, a hostility and a position, through the same
    /// UnitTranslator.Plan (default TypeMapping.RealTemplates = appsettings.json's
    /// Vrf:TypeMappingMode), with the index-parallel (uuid, superiorUuid) hierarchy and the SAME
    /// CompositionPlan the service uses. Nothing is re-derived here, which is what stops the test
    /// and the service drifting apart.
    /// </summary>
    private sealed record Fixture(List<CreationPlan> Plans,
                                  List<(string Uuid, string SuperiorUuid)> Hierarchy,
                                  List<(string Uuid, string Name, double Lat, double Lon)> Authored,
                                  List<string> Echelons,
                                  CompositionPlan Comp);

    /// <summary>
    /// WHICH TYPE-MAPPING MODE. Whether a unit is an AGGREGATE - and therefore whether it can take
    /// composed children at all - is decided by UnitTranslator.Plan, which dispatches differently
    /// in the two shipped modes. Both are live configurations and the de-stack scope must be
    /// measured on both:
    ///   RealTemplates - appsettings.json:28, and what a RUNNER-LAUNCHED app uses
    ///     (scripts\RunC2SimScenario.ps1 sets Vrf__TypeMapFile but never Vrf__TypeMappingMode, and
    ///     sets no DOTNET_ENVIRONMENT). This is the configuration the base-profile de-stack default
    ///     was added for.
    ///   FidelityTable - appsettings.Demo.json:9, i.e. scripts\StartInterface52.ps1 -Environment
    ///     Demo. The table (data/unit-type-map-52.json) is read ONLY in this mode.
    /// </summary>
    private static UnitTypeMap _table;
    private static bool _tableTried;

    private static UnitTypeMap Table()
    {
        if (_tableTried) return _table;
        _tableTried = true;
        string p = FindData("unit-type-map-52.json");
        if (p != null) { try { _table = UnitTypeMap.Load(p); } catch { _table = null; } }
        return _table;
    }

    private static Fixture BuildFixture(string path, TypeMapping mode = TypeMapping.RealTemplates)
    {
        var init = InitParser.Parse(File.ReadAllText(path));
        var map = mode == TypeMapping.FidelityTable ? Table() : null;
        var nations = new NationRoles("USA", "RUS");   // appsettings.json:29-30
        var plans = new List<CreationPlan>();
        var hier = new List<(string, string)>();
        var authored = new List<(string, string, double, double)>();
        var echelons = new List<string>();
        foreach (var u in init.Units)
        {
            if (string.IsNullOrEmpty(u.Uuid) || string.IsNullOrEmpty(u.HostilityCode)) continue;
            if (!double.TryParse(u.Latitude, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out double la)
                || !double.TryParse(u.Longitude, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out double lo))
                continue;
            var unit = string.IsNullOrEmpty(u.ElevationAgl) ? u with { ElevationAgl = "1000.0" } : u;
            var plan = UnitTranslator.Plan(unit, mode, map, nations);
            // The service does NOT create a unit whose type mapping failed (the TYPE MAP
            // AuthoredPending/Failed branch), so such a unit is not a plan and cannot be de-stacked.
            if (plan.Fidelity is TypeFidelity.AuthoredPending or TypeFidelity.Failed) continue;
            plans.Add(plan);
            hier.Add((u.Uuid, (u.SuperiorUuid ?? "").Trim()));
            authored.Add((u.Uuid, u.Name, la, lo));
            echelons.Add(EchelonSpacing.KeyOf(u.EchelonCode, u.SymbolId));
        }
        return new Fixture(plans, hier, authored, echelons, CompositionPlan.Classify(plans, hier));
    }

    /// <summary>
    /// De-stack a real init at the given spacing and assert the group count, how many units
    /// actually moved, and that every group anchor kept its exact coordinate.
    ///
    /// "MOVED" is measured against the parsed coordinate, not inferred from the group sizes: a
    /// group of n contributes n-1 moved units only if the anchor really stays put, and that is the
    /// property worth locking.
    ///
    /// The line ALSO reports what the SAME fixture would have done with the pre-2026-09-20 scope
    /// (every plan independent), so the effect of the C14 scope rule is on the record in the test's
    /// own output rather than in a report someone has to find.
    /// </summary>
    private static void CheckInit(ref int failures, string fixture, double spacing,
                                  int expectGroups, int expectMoved,
                                  int expectSiblingGroups, int expectSiblingMoved,
                                  TypeMapping mode = TypeMapping.RealTemplates)
    {
        string path = FindData(fixture);
        if (path == null)
        {
            Check(ref failures, false, $"{fixture}: NOT FOUND under data/ - cannot measure");
            return;
        }
        var f = BuildFixture(path, mode);
        // The OLD scope, on an independent copy, purely to print the contrast.
        var oldPlans = f.Plans.ToList();
        var oldGroups = DeStacker.Apply(oldPlans, spacing, 0.0, null);
        int oldMoved = CountMoved(oldPlans, f.Authored);

        // THE PARSE-INIT DIAGNOSTIC MUST AGREE WITH THE RUNTIME DE-STACK (cold-start review,
        // 2026-09-20): `--parse-init` now calls InitParseCheck.ComputeStackedGroups, the SAME
        // CompositionPlan/DeStacker.CoordKey classification DeStacker.Apply uses below, on a
        // PRE-destack copy of the same plans/hierarchy - so the two can never again print
        // different answers for the same file (the defect this replaces: the diagnostic used
        // to group by raw coordinate and call a company's own composed platoons "affected"
        // while the runtime de-stack held them with their parent and moved nothing).
        var preDestack = f.Plans.ToList();
        var (parseGroups, parseAffected, parseComposedChildren) =
            InitParseCheck.ComputeStackedGroups(preDestack, f.Hierarchy);

        var groups = DeStacker.Apply(f.Plans, spacing, 0.0, f.Comp.ComposedChildIndices);
        int moved = CountMoved(f.Plans, f.Authored);
        bool anchorsKept = groups.All(g =>
            f.Plans.Any(p => p.Pos.LatDeg == g.LatDeg && p.Pos.LonDeg == g.LonDeg));
        var movedNames = new List<string>();
        for (int i = 0; i < f.Plans.Count && movedNames.Count < 12; i++)
            if (f.Plans[i].Pos.LatDeg != f.Authored[i].Lat || f.Plans[i].Pos.LonDeg != f.Authored[i].Lon)
                movedNames.Add(f.Authored[i].Name);
        Check(ref failures,
              groups.Count == expectGroups && moved == expectMoved && anchorsKept,
              $"{fixture} [{mode}]: {f.Plans.Count} placeable unit(s), " +
              $"{f.Comp.NonAggregateParentIndices.Count} declared parent(s) that are NOT aggregates " +
              $"[{string.Join(", ", f.Comp.NonAggregateParentIndices.Select(i => f.Authored[i].Name))}] " +
              $"(cannot compose - their children stand alone), {f.Comp.ComposedChildIndices.Count} " +
              $"composed child(ren) held with their parent; {groups.Count} co-located group(s) of " +
              $"INDEPENDENT units (expected {expectGroups}), {moved} unit(s) moved (expected " +
              $"{expectMoved}) [{string.Join(", ", movedNames)}], every group anchor kept: " +
              $"{anchorsKept}. Pre-SF2 scope on the same file: {oldGroups.Count} group(s), " +
              $"{oldMoved} moved.");

        // The affected-unit count from ComputeStackedGroups counts every member of a stacked
        // group (anchors included); DeStacker.Apply's "moved" excludes the one anchor per group
        // that keeps its position - so the two agree exactly when parseAffected == moved +
        // groups.Count. R9 lean/full must both come out (0 groups, 0 affected) here too.
        Check(ref failures,
              parseGroups == groups.Count && parseAffected == moved + groups.Count &&
              parseComposedChildren.Count == f.Comp.ComposedChildIndices.Count,
              $"{fixture} [{mode}]: --parse-init's ComputeStackedGroups agrees with the runtime " +
              $"de-stack - {parseGroups} group(s) (destack: {groups.Count}), {parseAffected} " +
              $"unit(s) affected (destack anchors {groups.Count} + moved {moved} = " +
              $"{groups.Count + moved}), {parseComposedChildren.Count} composed child(ren) held " +
              $"(destack: {f.Comp.ComposedChildIndices.Count})");

        // *** THE SECOND PASS (user ruling 2026-09-21): composed siblings at their echelon's own
        // scale, around a parent that does not move. Run on the SAME plans the independent pass
        // just rewrote, which is the order the service uses. ***
        var beforeSiblings = f.Plans.ToList();
        var sib = DeStacker.ApplyComposedSiblings(f.Plans, f.Comp.ComposedGroups, f.Echelons,
                                                  k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                  out var skipped, preDestack.Select(x => x.Pos).ToList());
        int sibMoved = 0;
        var parentsMoved = new List<string>();
        for (int i = 0; i < f.Plans.Count; i++)
            if (f.Plans[i].Pos.LatDeg != beforeSiblings[i].Pos.LatDeg
                || f.Plans[i].Pos.LonDeg != beforeSiblings[i].Pos.LonDeg) sibMoved++;
        foreach (var (pi, _) in f.Comp.ComposedGroups)
            if (pi >= 0 && pi < f.Plans.Count
                && (f.Plans[pi].Pos.LatDeg != beforeSiblings[pi].Pos.LatDeg
                    || f.Plans[pi].Pos.LonDeg != beforeSiblings[pi].Pos.LonDeg))
                parentsMoved.Add(f.Authored[pi].Name);
        Check(ref failures,
              sib.Count == expectSiblingGroups && sibMoved == expectSiblingMoved && parentsMoved.Count == 0,
              $"{fixture} [{mode}]: COMPOSED SIBLINGS - {f.Comp.ComposedGroups.Count} parent(s) with " +
              $"composed children; {sib.Count} co-located sibling group(s) spread (expected " +
              $"{expectSiblingGroups}), {sibMoved} child(ren) moved (expected {expectSiblingMoved}), " +
              $"{skipped.Count} group(s) skipped for want of an echelon; NO PARENT MOVED " +
              $"(moved: [{string.Join(", ", parentsMoved)}]). Spread: [" +
              string.Join("; ", sib.Take(6).Select(g =>
                  $"{g.ParentName} x{g.Count} @ {g.SpacingMeters:F0} m ({g.EchelonKey})")) +
              (sib.Count > 6 ? "; ..." : "") + "]");
    }

    private static int CountMoved(IReadOnlyList<CreationPlan> plans,
                                  IReadOnlyList<(string Uuid, string Name, double Lat, double Lon)> authored)
    {
        int moved = 0;
        for (int i = 0; i < plans.Count; i++)
            if (plans[i].Pos.LatDeg != authored[i].Lat || plans[i].Pos.LonDeg != authored[i].Lon)
                moved++;
        return moved;
    }

    /// <summary>
    /// Does de-stacking move any unit THIS ORDER ACTUALLY TASKS? That is the question a run cares
    /// about: a spread context shell is invisible, a spread TASKEE changes where its route starts
    /// and therefore what the pre-flight scores, what the arrival radius is and what the traversal
    /// bar is. Reports the displacement per taskee so a non-zero answer is actionable rather than
    /// just red.
    /// </summary>
    private static void CheckTaskeesUnmoved(ref int failures, string initFixture, string orderFixture,
                                            double spacing)
    {
        string ip = FindData(initFixture), op = FindData(orderFixture);
        if (ip == null || op == null)
        {
            Check(ref failures, false, $"{initFixture} + {orderFixture}: NOT FOUND under data/");
            return;
        }
        var f = BuildFixture(ip);
        var order = OrderParser.Parse(File.ReadAllText(op));
        var taskees = order.Tasks.Select(t => t.TaskeeUuid).Where(u => !string.IsNullOrEmpty(u))
                           .ToHashSet(StringComparer.Ordinal);
        DeStacker.Apply(f.Plans, spacing, 0.0, f.Comp.ComposedChildIndices);
        var moved = new List<string>();
        int seen = 0;
        for (int i = 0; i < f.Plans.Count; i++)
        {
            if (!taskees.Contains(f.Authored[i].Uuid)) continue;
            seen++;
            double d = DistMeters(new Geodetic { LatDeg = f.Authored[i].Lat, LonDeg = f.Authored[i].Lon },
                                  f.Plans[i].Pos);
            if (d > 1e-6) moved.Add($"{f.Authored[i].Name} {d:F0} m");
        }
        Check(ref failures, seen == taskees.Count && moved.Count == 0,
              $"{initFixture} + {orderFixture}: {seen} of {taskees.Count} taskee(s) found in the init, " +
              (moved.Count == 0
                  ? "and NONE of them is moved by the 700 m de-stack - every taskee is the anchor of " +
                    "its own coordinate, so the order's routes start exactly where they did"
                  : "and " + moved.Count + " IS MOVED: [" + string.Join("; ", moved) + "] - the route " +
                    "this taskee is given will start somewhere else"));
    }

    /// <summary>
    /// AND THE SAME QUESTION OF THE 2026-09-21 SIBLING PASS, WHICH IS NOT THE SAME ANSWER.
    ///
    /// The parent of a composed group never moves, so a taskee that is a PARENT keeps its route
    /// start. A taskee that is itself a COMPOSED CHILD does move - that is what the ruling asks
    /// for, and no init-time rule can avoid it, because the de-stack runs before any order exists
    /// and cannot know which units will be tasked. So the property is MEASURED per fixture and
    /// pinned by name rather than asserted away:
    ///   R9 LEAN (the D6 / confirming-run fixture): 0 taskees move. 1222.MechPlt's Superior is not
    ///     in that file, so it is an INDEPENDENT unit, and 114.MechCoy is a parent.
    ///   R9 FULL: 1 taskee moves - 1222.MechPlt, which in the full init IS a declared child of
    ///     122.MechCoy, one platoon ring (350 m). Its route then starts 350 m from the authored
    ///     point; Vrf:DropOriginVertexMeters (default 100 m) is what keeps the order's leading
    ///     "from here" vertex from dragging it back (PREREG_ASSEMBLY_LAYOUT 3f).
    /// A moved taskee must sit exactly on ITS OWN GROUP'S ring - and since N4 that radius is
    /// DERIVED from the echelon spacing (spacing / (2 sin(pi/N)) for a group of N), not equal to
    /// it. The check reads the radius off the group the pass itself reports, so the test cannot
    /// drift from the geometry; a taskee displaced by anything else is a defect and this says so.
    /// </summary>
    private static void CheckTaskeesAfterSiblingSpread(ref int failures, string initFixture,
                                                       string orderFixture, double spacing,
                                                       int expectMovedTaskees)
    {
        string ip = FindData(initFixture), op = FindData(orderFixture);
        if (ip == null || op == null)
        {
            Check(ref failures, false, $"{initFixture} + {orderFixture}: NOT FOUND under data/");
            return;
        }
        var f = BuildFixture(ip);
        var order = OrderParser.Parse(File.ReadAllText(op));
        var taskees = order.Tasks.Select(t => t.TaskeeUuid).Where(u => !string.IsNullOrEmpty(u))
                           .ToHashSet(StringComparer.Ordinal);
        var snapshot = f.Plans.Select(p0 => p0.Pos).ToList();
        DeStacker.Apply(f.Plans, spacing, 0.0, f.Comp.ComposedChildIndices);
        var beforeSiblings = f.Plans.ToList();
        var spread = DeStacker.ApplyComposedSiblings(f.Plans, f.Comp.ComposedGroups, f.Echelons,
                                                     k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                     out _, snapshot);
        var radiusByName = RadiusByName(spread);
        var moved = new List<string>();
        var offRing = new List<string>();
        for (int i = 0; i < f.Plans.Count; i++)
        {
            if (!taskees.Contains(f.Authored[i].Uuid)) continue;
            double d = DistMeters(beforeSiblings[i].Pos, f.Plans[i].Pos);
            if (d <= 1e-6) continue;
            moved.Add($"{f.Authored[i].Name} {d:F0} m ({f.Echelons[i]})");
            // N4: THE EXPECTED DISPLACEMENT IS ITS GROUP'S DERIVED RADIUS, NOT THE SPACING. This
            // REPLACES the pre-N4 assertion "a whole number of its own echelon's rings", which
            // pinned the superseded hex geometry (displacement = k x spacing). What it pins now is
            // stronger, because the radius is a function of the group's SIZE as well as its
            // echelon and the test reads it from the pass's own report rather than re-deriving it.
            double own = EchelonSpacing.SpacingFor(f.Echelons[i], 0.0);
            if (!radiusByName.TryGetValue(f.Authored[i].Name, out double r) || !(Math.Abs(d - r) < 1.0))
                offRing.Add($"{f.Authored[i].Name} {d:F1} m vs its group's {r:F1} m ring " +
                            $"(echelon spacing {own:F0} m)");
        }
        Check(ref failures, moved.Count == expectMovedTaskees && offRing.Count == 0,
              $"{initFixture} + {orderFixture}: the SIBLING pass moves {moved.Count} taskee(s) " +
              $"(expected {expectMovedTaskees}) [{string.Join("; ", moved)}]" +
              (offRing.Count == 0
                  ? " - each of them a composed child displaced exactly its GROUP'S derived ring " +
                    "radius (N4: spacing / (2 sin(pi/N)), not the spacing), and every taskee that is " +
                    "a PARENT keeps its position, centroid included"
                  : " - OFF-RING: [" + string.Join("; ", offRing) + "]"));
    }

    /// <summary>
    /// A TASKEE'S OWN DECLARED SUBORDINATES: HOW FAR DO THEY MOVE, AND IS IT THEIR ECHELON'S RING?
    ///
    /// THIS CHECK REPLACES THE 2026-09-20 ASSERTION "NONE is displaced" (the SF2 arm the cold-start
    /// review of 9d67f97 added). That assertion pinned the behaviour the user ruling of 2026-09-21
    /// SUPERSEDES: D6 measured what "held with the parent" actually produced - three platoon
    /// aggregates 24-56 m apart at every sample, 3 of 3 footprints overlapping, 137 member pairs
    /// under 7 m against D3's 44 - so the children ARE spread now. What the check pins instead is
    /// the property the ruling actually asks for, which is stronger than "nothing moved":
    ///   - the TASKEE itself does not move (its route still starts where it started);
    ///   - every displaced member sits on ITS OWN ECHELON's ring, at the spacing the table derives
    ///     from the shipped formations - not at the 700 m company spacing, which is what put a
    ///     whole platoon under the STP-837 traversal bar in D3;
    ///   - the displacement is a whole number of rings, so the geometry is the tested one.
    /// The COMPARABILITY consequence is real and is the reason Vrf:DeStackComposedSiblings exists:
    /// a run with it ON is NOT comparable member-for-member with D1/D1b/D3/D6 (RUNBOOK 11e).
    /// </summary>
    private static void CheckTaskeeMembersUnmoved(ref int failures, string initFixture,
                                                  string orderFixture, double spacing)
    {
        string ip = FindData(initFixture), op = FindData(orderFixture);
        if (ip == null || op == null)
        {
            Check(ref failures, false, $"{initFixture} + {orderFixture}: NOT FOUND under data/");
            return;
        }
        var f = BuildFixture(ip);
        var order = OrderParser.Parse(File.ReadAllText(op));
        var taskees = order.Tasks.Select(t => t.TaskeeUuid).Where(u => !string.IsNullOrEmpty(u))
                           .ToHashSet(StringComparer.Ordinal);
        var snapshot = f.Plans.Select(p0 => p0.Pos).ToList();
        DeStacker.Apply(f.Plans, spacing, 0.0, f.Comp.ComposedChildIndices);
        var spread = DeStacker.ApplyComposedSiblings(f.Plans, f.Comp.ComposedGroups, f.Echelons,
                                                     k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                     out _, snapshot);
        var radiusByName = RadiusByName(spread);
        var moved = new List<string>();
        var offRing = new List<string>();
        int members = 0;
        for (int i = 0; i < f.Plans.Count; i++)
        {
            if (!taskees.Contains(f.Hierarchy[i].SuperiorUuid)) continue;   // a member of some taskee
            members++;
            double d = DistMeters(new Geodetic { LatDeg = f.Authored[i].Lat, LonDeg = f.Authored[i].Lon },
                                  f.Plans[i].Pos);
            if (d <= 1e-6) continue;
            moved.Add($"{f.Authored[i].Name} {d:F0} m ({f.Echelons[i]})");
            double own = EchelonSpacing.SpacingFor(f.Echelons[i], 0.0);
            // N4: ON ITS GROUP'S DERIVED RING. This REPLACES "a whole number of ITS OWN ECHELON's
            // rings", which pinned the superseded hex geometry. The radius comes from the pass's
            // own report of the group this unit is in, so the assertion tracks the rule.
            bool onRing = radiusByName.TryGetValue(f.Authored[i].Name, out double r) && Math.Abs(d - r) < 1.0;
            if (!onRing)
                offRing.Add($"{f.Authored[i].Name} {d:F1} m vs its group's {r:F1} m ring " +
                            $"(echelon spacing {own:F0} m)");
        }
        Check(ref failures, offRing.Count == 0,
              $"{initFixture} + {orderFixture}: {members} declared subordinate(s) of a TASKEE, " +
              $"{moved.Count} displaced by the 2026-09-21 sibling spread [" +
              string.Join("; ", moved.Take(8)) + (moved.Count > 8 ? "; ..." : "") + "] - " +
              (offRing.Count == 0
                  ? "and EVERY one of them sits on ITS OWN GROUP'S derived ring radius, never the 700 m " +
                    "company spacing and (since N4) never the echelon spacing either: the radius is " +
                    "spacing / (2 sin(pi/N)), chosen so the SEPARATION is the spacing and the group's " +
                    "CENTROID stays on the parent. (The pre-2026-09-21 assertion here was 'NONE is " +
                    "displaced' and the pre-N4 one was 'a whole number of echelon rings'; " +
                    "Vrf:DeStackComposedSiblings=false restores the original for a comparability run.)"
                  : offRing.Count + " is NOT on its group's ring: [" + string.Join("; ", offRing) + "]"));
    }

    /// <summary>
    /// N4: IS THE SIBLING PASS A LITERAL NO-OP ON THIS FIXTURE? For a fixture with no composed
    /// sibling groups - COA-STP1 and Iron Storm, the two the C14 confirmation of 2026-09-07 rests
    /// on - the whole 2026-09-21 lane, N4 included, must leave every coordinate exactly as the
    /// INDEPENDENT pass left it. Compared as doubles, bit for bit, not to a tolerance: the claim
    /// is "byte-identical", and a tolerance would not test it.
    /// </summary>
    private static void CheckSiblingPassIsNoOp(ref int failures, string fixture, double spacing)
    {
        string path = FindData(fixture);
        if (path == null) { Check(ref failures, false, $"{fixture}: NOT FOUND under data/"); return; }
        var f = BuildFixture(path);
        var snapshot = f.Plans.Select(p => p.Pos).ToList();
        DeStacker.Apply(f.Plans, spacing, 0.0, f.Comp.ComposedChildIndices);
        var afterIndependent = f.Plans.ToList();
        var sib = DeStacker.ApplyComposedSiblings(f.Plans, f.Comp.ComposedGroups, f.Echelons,
                                                  k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                  out var skipped, snapshot);
        int differing = 0;
        for (int i = 0; i < f.Plans.Count; i++)
            if (f.Plans[i].Pos.LatDeg != afterIndependent[i].Pos.LatDeg
                || f.Plans[i].Pos.LonDeg != afterIndependent[i].Pos.LonDeg) differing++;
        Check(ref failures, sib.Count == 0 && skipped.Count == 0 && differing == 0
                         && f.Plans.SequenceEqual(afterIndependent),
              $"{fixture}: the 2026-09-21 sibling pass (N4 geometry included) is a LITERAL NO-OP - " +
              $"{f.Comp.ComposedGroups.Count} composed group(s), {sib.Count} spread, {skipped.Count} " +
              $"skipped, {differing} of {f.Plans.Count} plan(s) differ from what the INDEPENDENT pass " +
              "left. The independent lane's own result (the 2026-09-07 confirmation) is therefore " +
              "byte-identical to what it was before N4");
    }

    /// <summary>
    /// *** THE N4 PROPERTY ON THE REAL FIXTURE, AND WHAT IT IS WORTH IN METRES OF ROUTE. ***
    ///
    /// VR-Forces publishes a composed aggregate at its MEMBERS' CENTROID (run D7 measured
    /// 114.MechCoy~PXY's published position 262 m from the coordinate its shell was created at),
    /// and the taskee's route is built from the position it is PUBLISHED at - so the centroid, not
    /// the shell, is what sets the route length, the arrival radius and the traversal bar. This
    /// check therefore asks the question the live run asks:
    ///   1. is the children's centroid the parent's own coordinate?
    ///   2. is the route measured FROM THAT CENTROID the same length as the route from the parent?
    ///   3. is that length back at D3/D6's 1,112-1,113 m, and not D7's 1,039 m?
    /// The route is the great-circle path from the taskee's position through the task's authored
    /// vertices - the same construction ExecuteTaskOnTick uses when the first authored vertex is
    /// more than Vrf:DropOriginVertexMeters from the unit (R9 lean's first vertex is 557 m out, so
    /// nothing is dropped and the route is start -&gt; v1 -&gt; v2).
    /// </summary>
    private static void CheckComposedCentroidAndRoute(ref int failures, string initFixture,
                                                      string orderFixture, double spacing)
    {
        string ip = FindData(initFixture), op = FindData(orderFixture);
        if (ip == null || op == null)
        {
            Check(ref failures, false, $"{initFixture} + {orderFixture}: NOT FOUND under data/");
            return;
        }
        var f = BuildFixture(ip);
        var order = OrderParser.Parse(File.ReadAllText(op));
        var snapshot = f.Plans.Select(p => p.Pos).ToList();
        DeStacker.Apply(f.Plans, spacing, 0.0, f.Comp.ComposedChildIndices);
        var beforeSiblings = f.Plans.ToList();
        var sib = DeStacker.ApplyComposedSiblings(f.Plans, f.Comp.ComposedGroups, f.Echelons,
                                                  k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                  out _, snapshot);
        Check(ref failures, sib.Count > 0,
              $"{initFixture}: {sib.Count} composed sibling group(s) spread - the fixture this check " +
              "is about");
        foreach (var (parentIndex, childIndices) in f.Comp.ComposedGroups)
        {
            if (parentIndex < 0 || parentIndex >= f.Plans.Count) continue;
            var kids = childIndices.Where(i => i >= 0 && i < f.Plans.Count && i != parentIndex).ToList();
            if (kids.Count < 2) continue;
            var parentPos = f.Plans[parentIndex].Pos;
            string parentName = f.Authored[parentIndex].Name;
            // (1) the parent's shell did not move, and (2) neither did its published position.
            double shellMoved = DistMeters(beforeSiblings[parentIndex].Pos, parentPos);
            var centroid = Centroid(kids.Select(i => f.Plans[i].Pos));
            double centroidOff = DistMeters(parentPos, centroid);
            double minSep = double.MaxValue;
            for (int a = 0; a < kids.Count; a++)
                for (int b = a + 1; b < kids.Count; b++)
                    minSep = Math.Min(minSep, DistMeters(f.Plans[kids[a]].Pos, f.Plans[kids[b]].Pos));
            double own = EchelonSpacing.SpacingFor(f.Echelons[kids[0]], 0.0);
            Check(ref failures, shellMoved == 0.0 && centroidOff < 1.0 && minSep >= own * 0.99,
                  $"{initFixture}: {parentName}'s {kids.Count} composed children - the PARENT'S SHELL " +
                  $"moved {shellMoved:F3} m, their CENTROID (= the position VR-Forces publishes for the " +
                  $"aggregate) is {centroidOff:F3} m from it, and the tightest sibling pair is " +
                  $"{minSep:F1} m apart against the {own:F0} m echelon spacing. Positions: [" +
                  string.Join("; ", kids.Select(i =>
                      $"{f.Authored[i].Name} {DistMeters(parentPos, f.Plans[i].Pos):F1} m at " +
                      $"{BearingDeg(parentPos, f.Plans[i].Pos):F1} deg")) + "]");
            // (3) the route the taskee is given, measured from the CENTROID and from the SHELL.
            var task = order.Tasks.FirstOrDefault(t => t.TaskeeUuid == f.Authored[parentIndex].Uuid
                                                       && t.Points is { Count: > 0 });
            if (task == null) continue;
            double fromShell = PathMeters(parentPos, task.Points);
            double fromCentroid = PathMeters(centroid, task.Points);
            Check(ref failures, Math.Abs(fromShell - fromCentroid) < 1.0
                             && fromCentroid >= 1110.0 && fromCentroid <= 1116.0,
                  $"{initFixture} + {orderFixture}: {parentName}'s task '{task.TaskName}' is " +
                  $"{fromCentroid:F1} m long measured from the PUBLISHED (centroid) position and " +
                  $"{fromShell:F1} m from the shell - the same route, and back in D3/D6's " +
                  "1,112-1,113 m band. Run D7 measured 1,039.3 m from a centroid the hex layout had " +
                  "moved 262 m, with the arrival radius at 260 m instead of 278 and the traversal bar " +
                  "at 520 m instead of 556 (N4). Radius now " +
                  $"{ArrivalPolicy.RadiusFor(500.0, fromCentroid):F1} m, route bar " +
                  $"{ArrivalPolicy.RequiredTravelFor(fromCentroid, 100.0):F1} m");
            // ...and what the SUPERSEDED hex layout would have produced on the same fixture, so the
            // 74 m is a measured contrast in the test's own output rather than a claim in a report.
            double hexRadius = own;
            double sumN = 0, sumE = 0;
            for (int n = 1; n <= kids.Count; n++)
            {
                var (north, east) = DeStacker.RingOffset(n, hexRadius, 0.0);
                sumN += north; sumE += east;
            }
            var hexCentroid = Offset(parentPos, sumN / kids.Count, sumE / kids.Count);
            double hexRoute = PathMeters(hexCentroid, task.Points);
            Check(ref failures, DistMeters(parentPos, hexCentroid) > 200.0 && hexRoute < fromCentroid - 50.0,
                  $"CONTRAST (the superseded hex layout, re-derived here): its centroid would sit " +
                  $"{DistMeters(parentPos, hexCentroid):F0} m from the parent and the same task would " +
                  $"measure {hexRoute:F1} m - {fromCentroid - hexRoute:F1} m short. That is the D7 " +
                  "defect, reproduced offline");
        }
    }

    /// <summary>Unit name -> the ring radius the sibling pass reports for the group it is in (N4).
    /// Taken from the pass's own SiblingGroup records, so the assertion cannot drift from the
    /// geometry by re-deriving it from a formula the test keeps its own copy of.</summary>
    private static Dictionary<string, double> RadiusByName(IReadOnlyList<DeStacker.SiblingGroup> spread)
    {
        var map = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var g in spread)
            foreach (var (name, _) in g.Moved)
                map[name] = g.RadiusMeters;
        return map;
    }

    /// <summary>Flat-earth centroid of a set of positions - adequate at ring scale.</summary>
    private static Geodetic Centroid(IEnumerable<Geodetic> points)
    {
        double lat = 0, lon = 0; int n = 0;
        foreach (var p in points) { lat += p.LatDeg; lon += p.LonDeg; n++; }
        return n == 0 ? new Geodetic() : new Geodetic { LatDeg = lat / n, LonDeg = lon / n, AltMeters = 0.0 };
    }

    private static Geodetic Offset(Geodetic from, double northM, double eastM)
        => new()
        {
            LatDeg = from.LatDeg + northM / 111_320.0,
            LonDeg = from.LonDeg + eastM / (111_320.0 * Math.Cos(from.LatDeg * Math.PI / 180.0)),
            AltMeters = from.AltMeters,
        };

    private static double BearingDeg(Geodetic from, Geodetic to)
    {
        double north = (to.LatDeg - from.LatDeg) * 111_320.0;
        double east = (to.LonDeg - from.LonDeg) * 111_320.0 * Math.Cos(from.LatDeg * Math.PI / 180.0);
        double deg = Math.Atan2(east, north) * 180.0 / Math.PI;
        return deg < 0 ? deg + 360.0 : deg;
    }

    /// <summary>Great-circle path length from a start position through the task's authored
    /// vertices - RouteExtentPolicy's own measure, so the number is the one the service records
    /// in InFlight.RouteLengthMeters.</summary>
    private static double PathMeters(Geodetic start, IReadOnlyList<(double Lat, double Lon, double? Elev)> pts)
    {
        var path = new List<(double Lat, double Lon)> { (start.LatDeg, start.LonDeg) };
        foreach (var p in pts) path.Add((p.Lat, p.Lon));
        return RouteExtentPolicy.PathLengthMeters(path);
    }

    /// <summary>data/&lt;name&gt;, found by walking up from the exe and the working directory - the
    /// same search InitGraphicsSelfTest uses, and for the same reason (the exe sits five or six
    /// levels below the repo root depending on the configuration).</summary>
    private static string FindData(string name)
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            for (int i = 0; dir != null && i < 10; i++, dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "data", name);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    private static CreationPlan Plan(string name, double lat, double lon)
        => new(false, new EntityTypeSpec { Kind = 1, Domain = 1, Country = 225, Category = 1, Subcategory = 1, Specific = 3, Extra = 0 },
               Force.Friendly, 90.0, name, new Geodetic { LatDeg = lat, LonDeg = lon, AltMeters = 0.0 }, null);

    /// <summary>An AGGREGATE plan - only an aggregate can take composed children
    /// (CompositionPlan.Classify / ApplyHierarchyComposition).</summary>
    private static CreationPlan Agg(string name, double lat, double lon)
        => new(true, new EntityTypeSpec { Kind = 11, Domain = 1, Country = 225, Category = 3, Subcategory = 2, Specific = 0, Extra = 0 },
               Force.Friendly, 90.0, name, new Geodetic { LatDeg = lat, LonDeg = lon, AltMeters = 0.0 }, null);

    /// <summary>A PLATFORM plan - declared children under one of these compose nothing.</summary>
    private static CreationPlan Plat(string name, double lat, double lon) => Plan(name, lat, lon);

    // Local flat-earth ground distance - adequate at ring scale (tens of meters).
    private static double DistMeters(Geodetic a, Geodetic b)
    {
        double latRad = a.LatDeg * Math.PI / 180.0;
        double north = (b.LatDeg - a.LatDeg) * 111_320.0;
        double east = (b.LonDeg - a.LonDeg) * 111_320.0 * Math.Cos(latRad);
        return Math.Sqrt(north * north + east * east);
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
