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
/// </summary>
public static class DeStacker
{
    public sealed record StackGroup(double LatDeg, double LonDeg, int Count);

    private const double MetersPerDegLat = 111_320.0;

    /// <summary>
    /// Grouping key: lat/lon rounded to 1e-6 deg (~0.11 m) - literal identity plus
    /// string-formatting noise. Shared with InitParseCheck so the offline stat and
    /// the runtime behavior always agree.
    /// </summary>
    public static (double Lat, double Lon) CoordKey(double latDeg, double lonDeg)
        => (Math.Round(latDeg, 6), Math.Round(lonDeg, 6));

    /// <summary>
    /// De-stack <paramref name="plans"/> IN PLACE and return the groups that were
    /// spread (2+ units at the same CoordKey). Entities and aggregates are treated
    /// alike (both pile up - the R5c entity control needed ~13 min to escape the
    /// stack). Altitude, name, type, force and heading are untouched.
    /// </summary>
    public static List<StackGroup> Apply(IList<CreationPlan> plans, double spacingMeters)
    {
        var groups = new List<StackGroup>();
        if (plans.Count < 2 || spacingMeters <= 0)
            return groups;

        var byCoord = new Dictionary<(double, double), List<int>>();
        for (int i = 0; i < plans.Count; i++)
        {
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
                var (north, east) = RingOffset(n, spacingMeters);
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
    /// Slot for the n-th DISPLACED unit of a group (n is 1-based; n=0 is the anchor
    /// and never moves). Hex ring k = 1, 2, ... holds 6k slots at radius k*spacing;
    /// cumulative capacity of rings 1..k is 3k(k+1).
    /// </summary>
    public static (double NorthMeters, double EastMeters) RingOffset(int n, double spacingMeters)
    {
        int k = 1;
        while (3 * k * (k + 1) < n)
            k++;
        int j = n - 3 * (k - 1) * k - 1;          // 0-based slot index on ring k
        double angle = 2.0 * Math.PI * j / (6 * k);
        double r = k * spacingMeters;
        return (r * Math.Cos(angle), r * Math.Sin(angle));
    }
}
