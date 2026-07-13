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
        }

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
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
