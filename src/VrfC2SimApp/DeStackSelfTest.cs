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

        // 10. THE REAL INITS AT THE BASE PROFILE'S SPACING (2026-09-20).
        //
        // WHY: C14 (user ruling 2026-09-07) says co-located units are spread at init on 5.2, and
        // appsettings.Demo.json has said so since. A RUNNER-LAUNCHED app never loads the Demo
        // overlay - scripts\RunC2SimScenario.ps1 sets no DOTNET_ENVIRONMENT - so the base
        // appsettings.json now carries DeStackCreates/700 too. That is a change to WHERE UNITS ARE
        // on every runner run, so it owes a measurement on the actual fixtures rather than an
        // argument. The numbers below are COUNTED FROM THE FILES, not assumed.
        {
            Console.WriteLine("  --- the shipped inits at the base profile's 700 m spacing (C14) ---");
            const double Base = 700.0;   // appsettings.json DeStackSpacingMeters
            // MEASURED, NOT ASSUMED - and the measurement CORRECTED a belief. The R9 inits look
            // un-stacked in the raw XML (every unit with an authored position has a distinct one),
            // but InitParser's SUPERIOR CASCADE gives a unit with no coordinates its superior's
            // (C2SIMinterface.cpp:1421-1441), and that is what builds the piles: R9 lean ends with
            // ONE stack of four (114.MechCoy and its three platoons), R9 full with ten. So "R9 is
            // not co-located" is true of the file and false of the parse, and the de-stack DOES
            // touch it. What matters for a run is the next check, not this count.
            CheckInit(ref failures, "R9_Mojave_Lean_Initialization.xml", Base,
                      expectGroups: 1, expectMoved: 3);
            CheckInit(ref failures, "R9_Mojave_Initialization.xml", Base,
                      expectGroups: 10, expectMoved: 38);
            // COA-STP1 IS co-located, heavily - it is the pathology C14 was ruled against ("STP
            // puts a whole COA on its assembly point"): 10 shared coordinates carrying 72 of its
            // 128 units. The other 66 do not move at all and every group anchor keeps its exact
            // coordinate. Anyone who expected this init to be untouched should read the ruling,
            // not weaken the check.
            CheckInit(ref failures, "COA-STP1_Initialization.xml", Base, expectGroups: 10, expectMoved: 62);
            // The real STP export: 40 units, 36 placeable, and the superior cascade piles the 28ID
            // subtree onto one coordinate (the characterisation counted 12 there).
            CheckInit(ref failures, "STP-IRON-STORM-SYNTHETIC_Initialization.xml", Base,
                      expectGroups: 2, expectMoved: 12);

            // *** THE CHECK THAT DECIDES WHETHER A RUN MOVES: DO THE ORDER'S TASKEES SHIFT? ***
            // A context unit spread onto a ring changes the picture but nothing that is measured;
            // a TASKEE spread 700 m changes where its route starts, which changes the route the
            // pre-flight scores, the arrival radius and the traversal bar. R9's three taskees
            // (R9_Mojave_UnitMove_Order.xml) are the D3 control's whole population.
            CheckTaskeesUnmoved(ref failures, "R9_Mojave_Lean_Initialization.xml",
                                "R9_Mojave_UnitMove_Order.xml", Base);
            CheckTaskeesUnmoved(ref failures, "R9_Mojave_Initialization.xml",
                                "R9_Mojave_UnitMove_Order.xml", Base);
        }

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// Parse a real init, build one plan per unit that HAS a position (which is what
    /// ProcessInitializationLocked plans), de-stack at the given spacing, and assert the group
    /// count, how many units actually moved, and that every unmoved unit is byte-identical.
    ///
    /// "MOVED" is measured against the parsed coordinate, not inferred from the group sizes: a
    /// group of n contributes n-1 moved units only if the anchor really stays put, and that is the
    /// property worth locking.
    /// </summary>
    private static void CheckInit(ref int failures, string fixture, double spacing,
                                  int expectGroups, int expectMoved)
    {
        string path = FindData(fixture);
        if (path == null)
        {
            Check(ref failures, false, $"{fixture}: NOT FOUND under data/ - cannot measure");
            return;
        }
        var init = InitParser.Parse(File.ReadAllText(path));
        var plans = new List<CreationPlan>();
        var authored = new List<(double Lat, double Lon)>();
        foreach (var u in init.Units)
        {
            if (!double.TryParse(u.Latitude, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out double la)
                || !double.TryParse(u.Longitude, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out double lo))
                continue;
            plans.Add(Plan(u.Uuid, la, lo));
            authored.Add((la, lo));
        }
        var groups = DeStacker.Apply(plans, spacing);
        int moved = 0;
        for (int i = 0; i < plans.Count; i++)
            if (plans[i].Pos.LatDeg != authored[i].Lat || plans[i].Pos.LonDeg != authored[i].Lon)
                moved++;
        bool anchorsKept = groups.All(g =>
            plans.Any(p => p.Pos.LatDeg == g.LatDeg && p.Pos.LonDeg == g.LonDeg));
        Check(ref failures,
              groups.Count == expectGroups && moved == expectMoved && anchorsKept,
              $"{fixture}: {plans.Count} placeable unit(s), {groups.Count} co-located group(s) " +
              $"(expected {expectGroups}), {moved} unit(s) moved (expected {expectMoved}), " +
              $"{plans.Count - moved} untouched, every group anchor kept: {anchorsKept}");
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
        var init = InitParser.Parse(File.ReadAllText(ip));
        var order = OrderParser.Parse(File.ReadAllText(op));
        var taskees = order.Tasks.Select(t => t.TaskeeUuid).Where(u => !string.IsNullOrEmpty(u))
                           .ToHashSet(StringComparer.Ordinal);
        var plans = new List<CreationPlan>();
        var authored = new List<(string Uuid, string Name, double Lat, double Lon)>();
        foreach (var u in init.Units)
        {
            if (!double.TryParse(u.Latitude, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out double la)
                || !double.TryParse(u.Longitude, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out double lo))
                continue;
            plans.Add(Plan(u.Uuid, la, lo));
            authored.Add((u.Uuid, u.Name, la, lo));
        }
        DeStacker.Apply(plans, spacing);
        var moved = new List<string>();
        int seen = 0;
        for (int i = 0; i < plans.Count; i++)
        {
            if (!taskees.Contains(authored[i].Uuid)) continue;
            seen++;
            double d = DistMeters(new Geodetic { LatDeg = authored[i].Lat, LonDeg = authored[i].Lon },
                                  plans[i].Pos);
            if (d > 1e-6) moved.Add($"{authored[i].Name} {d:F0} m");
        }
        Check(ref failures, seen == taskees.Count && moved.Count == 0,
              $"{initFixture} + {orderFixture}: {seen} of {taskees.Count} taskee(s) found in the init, " +
              (moved.Count == 0
                  ? "and NONE of them is moved by the 700 m de-stack - every taskee is the anchor of " +
                    "its own coordinate, so the order's routes start exactly where they did"
                  : "and " + moved.Count + " IS MOVED: [" + string.Join("; ", moved) + "] - the route " +
                    "this taskee is given will start somewhere else"));
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
