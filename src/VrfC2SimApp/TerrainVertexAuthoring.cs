using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// Pure decision for GroundWaypointAltitudeMode="TerrainProfile"
/// (docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 3.3): given the Live-mode route
/// vertices and the back end's terrain-profile reply, produce the vertices to author. Every
/// rule degrades to the Live altitude - the order is never blocked or dropped by the query.
/// No bridge, no logging (the caller logs Reason); offline-tested by TerrainSelfTest.
/// </summary>
public static class TerrainVertexAuthoring
{
    public enum Mode { Terrain, Partial, Fallback }

    public sealed record Result(List<Geodetic> Vertices, Mode Mode, List<int> KeptLive, string Reason);

    // Frame sanity: the entity sits ON the terrain (VRF ground clamp), so the terrain height
    // under vertex 0 must be close to its live altitude. A larger gap says the request or
    // reply frame is not what the design inferred -> do not trust ANY sample of this reply.
    public const double DefaultMaxVertex0DeltaMeters = 100.0;

    // A sample whose returned lat/lon is not under the requested vertex is not an answer for it.
    public const double DefaultMaxHorizontalMismatchMeters = 50.0;

    /// <param name="liveVertices">the route as Live mode would author it (point 0 = live location)</param>
    /// <param name="samples">the reply; null = no reply (timeout)</param>
    /// <param name="clearanceMeters">TerrainClearanceMeters, added to each terrain height</param>
    /// <param name="entityAltMeters">the taskee's live altitude (vertex-0 sanity reference)</param>
    public static Result Apply(IReadOnlyList<Geodetic> liveVertices,
                               IReadOnlyList<TerrainHeightSample> samples,
                               double clearanceMeters,
                               double entityAltMeters,
                               double maxVertex0DeltaMeters = DefaultMaxVertex0DeltaMeters,
                               double maxHorizontalMismatchMeters = DefaultMaxHorizontalMismatchMeters)
    {
        var vertices = liveVertices.ToList();
        var allIndices = Enumerable.Range(0, vertices.Count).ToList();
        if (samples == null)
            return new Result(vertices, Mode.Fallback, allIndices, "no reply (timeout)");
        if (samples.Count == 0)
            return new Result(vertices, Mode.Fallback, allIndices, "empty reply");

        // Index the usable samples: in range, valid, and horizontally under their vertex.
        var terrainByIndex = new Dictionary<int, double>();
        foreach (var s in samples)
        {
            if (!s.Valid || s.Index < 0 || s.Index >= vertices.Count) continue;
            var v = vertices[s.Index];
            if (DistMeters(v.LatDeg, v.LonDeg, s.LatDeg, s.LonDeg) > maxHorizontalMismatchMeters) continue;
            terrainByIndex.TryAdd(s.Index, s.TerrainAltMeters);   // first answer per vertex wins
        }

        if (terrainByIndex.TryGetValue(0, out double terrain0)
            && Math.Abs(terrain0 - entityAltMeters) > maxVertex0DeltaMeters)
            return new Result(vertices, Mode.Fallback, allIndices,
                $"vertex-0 terrain height {terrain0:F1} m disagrees with the entity's live altitude " +
                $"{entityAltMeters:F1} m by {Math.Abs(terrain0 - entityAltMeters):F0} m - request/reply frame suspect");

        var keptLive = new List<int>();
        for (int i = 0; i < vertices.Count; i++)
        {
            if (terrainByIndex.TryGetValue(i, out double terrain))
                vertices[i] = new Geodetic { LatDeg = vertices[i].LatDeg, LonDeg = vertices[i].LonDeg,
                                             AltMeters = terrain + clearanceMeters };
            else
                keptLive.Add(i);
        }

        if (keptLive.Count == vertices.Count)
            return new Result(vertices, Mode.Fallback, keptLive, "no usable sample for any vertex");
        if (keptLive.Count > 0)
            return new Result(vertices, Mode.Partial, keptLive,
                $"vertices {string.Join(",", keptLive)} had no usable sample - kept Live altitude");
        return new Result(vertices, Mode.Terrain, keptLive, "all vertices authored from terrain");
    }

    // Equirectangular approximation - adequate for a few-metre threshold at any latitude
    // short of the poles (same formula family as DeStackSelfTest.DistMeters).
    public static double DistMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000.0;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0 * Math.Cos((lat1 + lat2) / 2.0 * Math.PI / 180.0);
        return R * Math.Sqrt(dLat * dLat + dLon * dLon);
    }
}
