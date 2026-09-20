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

        // 10. COMPOSED CHILDREN ARE NOT CO-LOCATED UNITS (SF2, 2026-09-20). The RULE, on synthetic
        //     plans, before the fixtures exercise it.
        {
            Console.WriteLine("  --- C14 scope: a composed child takes its place from its parent ---");
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
            var groups = DeStacker.Apply(plans, Spacing, 0.0, comp.ComposedChildIndices);
            Check(ref failures, groups.Count == 1 && groups[0].Count == 2,
                  $"the group is the 2 INDEPENDENT units, not all 5 (got {groups.Count} group(s) of " +
                  $"{(groups.Count > 0 ? groups[0].Count : 0)})");
            Check(ref failures, plans[1].Pos.LatDeg == 34.5 && plans[2].Pos.LatDeg == 34.5
                             && plans[3].Pos.LatDeg == 34.5 && plans[1].Pos.LonDeg == -116.5,
                  "NO composed child is displaced - it is laid out by its parent's formation");
            Check(ref failures, Math.Abs(DistMeters(plans[0].Pos, plans[4].Pos) - Spacing) < Spacing * 0.01,
                  "the co-located INDEPENDENT unit IS still spread (C14 is not weakened)");

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
                      expectGroups: 0, expectMoved: 0);
            CheckInit(ref failures, "R9_Mojave_Initialization.xml", Base,
                      expectGroups: 0, expectMoved: 0);
            // COA-STP1 IS co-located, heavily - it is the pathology C14 was ruled against ("STP
            // puts a whole COA on its assembly point"). Anyone who expected this init to be
            // untouched should read the ruling, not weaken the check. UNCHANGED by the SF2 scope
            // rule in the RUNNER configuration, which is what makes PREREG_ASSEMBLY_LAYOUT's
            // confirmed 2026-09-07 result still the result of this build.
            CheckInit(ref failures, "COA-STP1_Initialization.xml", Base, expectGroups: 10, expectMoved: 62);
            // The real STP export: 40 units, 36 placeable, the superior cascade piles the 28ID
            // subtree onto one coordinate.
            CheckInit(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml", Base,
                      expectGroups: 2, expectMoved: 12);

            // THE SAME FOUR FIXTURES IN THE OTHER SHIPPED MODE. FidelityTable maps a brigade or a
            // division to a REAL aggregate template where RealTemplates' 5.0.2 parity dispatch
            // falls through to a single Tank (a PLATFORM, which can compose nothing), so WHICH
            // units are composed children - and therefore which the de-stack may touch - is
            // different. Both modes ship; both are measured rather than reasoned about.
            Console.WriteLine("  --- the same fixtures under Vrf:TypeMappingMode=FidelityTable (Demo) ---");
            CheckInit(ref failures, "R9_Mojave_Lean_Initialization.xml", Base,
                      expectGroups: 0, expectMoved: 0, mode: TypeMapping.FidelityTable);
            CheckInit(ref failures, "R9_Mojave_Initialization.xml", Base,
                      expectGroups: 0, expectMoved: 0, mode: TypeMapping.FidelityTable);
            CheckInit(ref failures, "COA-STP1_Initialization.xml", Base,
                      expectGroups: 10, expectMoved: 62, mode: TypeMapping.FidelityTable);
            CheckInit(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml", Base,
                      expectGroups: 2, expectMoved: 12, mode: TypeMapping.FidelityTable);

            // *** THE CHECK THAT DECIDES WHETHER A RUN MOVES: DO THE ORDER'S TASKEES SHIFT? ***
            // A context unit spread onto a ring changes the picture but nothing that is measured;
            // a TASKEE spread 700 m changes where its route starts, which changes the route the
            // pre-flight scores, the arrival radius and the traversal bar. R9's three taskees
            // (R9_Mojave_UnitMove_Order.xml) are the D3 control's whole population.
            CheckTaskeesUnmoved(ref failures, "R9_Mojave_Lean_Initialization.xml",
                                "R9_Mojave_UnitMove_Order.xml", Base);
            CheckTaskeesUnmoved(ref failures, "R9_Mojave_Initialization.xml",
                                "R9_Mojave_UnitMove_Order.xml", Base);
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
        }
        return new Fixture(plans, hier, authored, CompositionPlan.Classify(plans, hier));
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
    /// THE ARM THE COLD-START REVIEW OF 9d67f97 ADDED (SF2). The taskee check above is necessary
    /// and is NOT sufficient: with Vrf:ComposeHierarchy on, a taskee's DECLARED SUBORDINATES become
    /// the members of its aggregate, and members are exactly what STP-837 arrival evidence and the
    /// C15/C16 stall and progress checks sample (VrfC2SimService.TryReadMemberPositions). A run in
    /// which the taskee stood still while its three platoons were born 700 m out on a hex ring is a
    /// different experiment from the D1/D1b/D3 controls, whatever the taskee's own coordinate did.
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
        DeStacker.Apply(f.Plans, spacing, 0.0, f.Comp.ComposedChildIndices);
        var moved = new List<string>();
        int members = 0;
        for (int i = 0; i < f.Plans.Count; i++)
        {
            if (!taskees.Contains(f.Hierarchy[i].SuperiorUuid)) continue;   // a member of some taskee
            members++;
            double d = DistMeters(new Geodetic { LatDeg = f.Authored[i].Lat, LonDeg = f.Authored[i].Lon },
                                  f.Plans[i].Pos);
            if (d > 1e-6) moved.Add($"{f.Authored[i].Name} {d:F0} m");
        }
        Check(ref failures, moved.Count == 0,
              $"{initFixture} + {orderFixture}: {members} declared subordinate(s) of a TASKEE, " +
              (moved.Count == 0
                  ? "and NONE is displaced - every member is created on its parent's coordinate and " +
                    "laid out by the parent's formation, so the member positions STP-837 and C15/C16 " +
                    "sample are the ones the previous runs sampled"
                  : moved.Count + " MOVED: [" + string.Join("; ", moved) + "] - the arrival evidence " +
                    "and stall checks of this run are not comparable with a run that had de-stack off"));
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
