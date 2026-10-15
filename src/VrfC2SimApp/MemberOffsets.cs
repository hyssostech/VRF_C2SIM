namespace VrfC2SimApp;

/// <summary>
/// M2 (demo-readiness census 2026-10-15): EACH MEMBER'S OWN DESTINATION for a per-member planned vertex, computed the way
/// the vendor's Move (Group) computes its subordinates' destinations before it sends each one Move_To_Location_Plan_Path
/// (C:\MAK\vrforces5.2d\data\simulationModelSets\AggregateTacticalLevel\scripts\group_movement_simplified.lua):
///   - the GROUP BEARING is the bearing of the sum of the vectors from the unit's centre to its first
///     max(1, floor(n * 0.34)) located subordinates, in subordinate order (determineGroupLocation, :83-176);
///   - each subordinate keeps its offset from the centre, WHEELED by (bearing to the destination - group bearing) and
///     scaled by the unit's compression factor (calcSubordinateDestination :28-55);
///   - the compression factor is limit / maxDim when the unit's extent exceeds the limit its DtAggregateCategory allows
///     (_UnitTypeExtentLimitTable :20, determineGeometryCompression :59-78); 1 otherwise.
/// Geometry: a local east/north plane at the centre (an inference that is exact enough at a few km - the vendor works
/// with geocentric vectors; the offsets here are at most the unit's extent). PURE: no bridge, no clock.
/// </summary>
public static class MemberOffsets
{
    /// <summary>group_movement_simplified.lua :20 - the extent limit (m) per DtAggregateCategory 1..14.</summary>
    public static readonly double[] ExtentLimitByCategory = { 200, 200, 500, 1500, 1500, 4000, 6000, 6000, 10000, 10000, 100000, 200, 100, 200 };

    public readonly record struct MemberFix(string Name, double Lat, double Lon, bool Located);

    public readonly record struct Result(IReadOnlyDictionary<string, (double Lat, double Lon)> Destinations,
                                         double GroupBearingDeg, double BearingToDestinationDeg, double Scale,
                                         int Unlocated);

    private const double MetersPerDegree = 111320.0;

    /// <summary>
    /// The members' destinations for one vertex, in the given (subordinate) order. <paramref name="aggregateCategory"/> is
    /// the unit's DtAggregateCategory (the 5th field of its object type; 0 or out of range = no compression, as the
    /// vendor's own fallback limit = maxDim). maxDim is twice the larger of the members' greatest north and east offset
    /// (an axis-aligned stand-in for the vendor's bounding volume, 2 x max(forward, right) - an INFERENCE), at least 10 m.
    /// A member with no readable position goes to the vertex itself (counted in Unlocated).
    /// </summary>
    public static Result For(double centreLat, double centreLon, IReadOnlyList<MemberFix> members, double destLat,
                             double destLon, int aggregateCategory)
    {
        members ??= Array.Empty<MemberFix>();
        double cos = Math.Cos(centreLat * Math.PI / 180.0);
        (double N, double E) Local(double lat, double lon) => ((lat - centreLat) * MetersPerDegree, (lon - centreLon) * MetersPerDegree * cos);
        double Bearing((double N, double E) v) => (Math.Atan2(v.E, v.N) * 180.0 / Math.PI + 360.0) % 360.0;

        var located = members.Where(m => m.Located).ToList();
        // determineGroupLocation: the first max(1, floor(n * 0.34)) located subordinates set the group bearing.
        int samples = Math.Max(1, (int)Math.Floor(members.Count * 0.34));
        double sumN = 0, sumE = 0;
        foreach (var m in located.Take(samples)) { var v = Local(m.Lat, m.Lon); sumN += v.N; sumE += v.E; }
        double groupBearing = sumN == 0 && sumE == 0 ? 0.0 : Bearing((sumN, sumE));
        double toDest = Bearing(Local(destLat, destLon));
        double wheel = toDest - groupBearing;

        // determineGeometryCompression.
        double maxN = 0, maxE = 0;
        foreach (var m in located) { var v = Local(m.Lat, m.Lon); maxN = Math.Max(maxN, Math.Abs(v.N)); maxE = Math.Max(maxE, Math.Abs(v.E)); }
        double maxDim = Math.Max(Math.Max(maxN * 2.0, maxE * 2.0), 10.0);
        double limit = aggregateCategory >= 1 && aggregateCategory <= ExtentLimitByCategory.Length
            ? ExtentLimitByCategory[aggregateCategory - 1] : maxDim;
        double scale = maxDim > limit ? limit / maxDim : 1.0;

        double destCos = Math.Cos(destLat * Math.PI / 180.0);
        var d = new Dictionary<string, (double, double)>(StringComparer.Ordinal);
        int unlocated = 0;
        foreach (var m in members)
        {
            if (!m.Located) { d[m.Name] = (destLat, destLon); unlocated++; continue; }
            var v = Local(m.Lat, m.Lon);
            double dist = Math.Sqrt(v.N * v.N + v.E * v.E) * scale;
            double heading = (Bearing(v) + wheel) * Math.PI / 180.0;   // calcSubordinateDestination: wheel, then scale
            double n = dist * Math.Cos(heading), e = dist * Math.Sin(heading);
            d[m.Name] = (destLat + n / MetersPerDegree, destLon + e / (MetersPerDegree * destCos));
        }
        return new Result(d, groupBearing, toDest, scale, unlocated);
    }
}
