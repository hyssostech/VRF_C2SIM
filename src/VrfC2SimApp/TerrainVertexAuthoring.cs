using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// Pure decision for GroundWaypointAltitudeMode="TerrainProfile"
/// (docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 3.3): given the Live-mode route
/// vertices and the back end's terrain-profile reply, produce the vertices to author. Every
/// rule degrades to the Live altitude - the order is never blocked or dropped by the query.
/// No bridge, no logging (the caller logs Reason/Note); offline-tested by TerrainSelfTest.
/// </summary>
public static class TerrainVertexAuthoring
{
    public enum Mode { Terrain, Partial, Fallback }

    /// <param name="Note">diagnostic that does not change the decision (null when nothing to say);
    /// today only the vertex-0 "taskee altitude not terrain-clamped" observation</param>
    public sealed record Result(List<Geodetic> Vertices, Mode Mode, List<int> KeptLive, string Reason, string Note);

    // A sample whose returned lat/lon is not under the requested vertex is not an answer for it.
    // This is the FRAME check (review 2026-09-01 F3): a request or reply in the wrong frame
    // lands nowhere near the vertices, every sample fails here, and the route falls back to
    // Live with "no usable sample" - a vertical-only gap says nothing about the frame.
    public const double DefaultMaxHorizontalMismatchMeters = 50.0;

    // Echo guard (review F2): a "terrain height" equal to the REQUEST vertex altitude (live +
    // GroundWaypointLiveClearanceMeters) to within 1 cm is the request point handed back, not
    // a terrain intersection. Authoring it would add the clearance on top of the Live
    // altitude and read as a success.
    public const double EchoToleranceMeters = 0.01;

    // Vertex-0 diagnostic (F3): the taskee should sit ON the terrain (VRF ground clamp), so a
    // terrain height under vertex 0 far from its live altitude means the published altitude is
    // not the surface (unclamped at CreateAltitudeSafeMslMeters, or an aggregate). That is
    // exactly the case the mode exists for, so it is REPORTED, never a fallback trigger.
    public const double DefaultVertex0NoteThresholdMeters = 100.0;

    /// <param name="liveVertices">the route as Live mode would author it (point 0 = live location)</param>
    /// <param name="samples">the reply; null = no reply (timeout)</param>
    /// <param name="clearanceMeters">TerrainClearanceMeters, added to each terrain height</param>
    /// <param name="entityAltMeters">the taskee's live altitude (vertex-0 diagnostic reference)</param>
    public static Result Apply(IReadOnlyList<Geodetic> liveVertices,
                               IReadOnlyList<TerrainHeightSample> samples,
                               double clearanceMeters,
                               double entityAltMeters,
                               double vertex0NoteThresholdMeters = DefaultVertex0NoteThresholdMeters,
                               double maxHorizontalMismatchMeters = DefaultMaxHorizontalMismatchMeters)
    {
        var vertices = liveVertices.ToList();
        var allIndices = Enumerable.Range(0, vertices.Count).ToList();
        if (samples == null)
            return new Result(vertices, Mode.Fallback, allIndices, "no reply (timeout)", null);
        if (samples.Count == 0)
            return new Result(vertices, Mode.Fallback, allIndices, "empty reply", null);

        // Index the usable samples: in range, valid, horizontally under their vertex, not an echo.
        var terrainByIndex = new Dictionary<int, double>();
        var echoed = new SortedSet<int>();
        foreach (var s in samples)
        {
            if (!s.Valid || s.Index < 0 || s.Index >= vertices.Count) continue;
            var v = vertices[s.Index];
            if (DistMeters(v.LatDeg, v.LonDeg, s.LatDeg, s.LonDeg) > maxHorizontalMismatchMeters) continue;
            if (Math.Abs(s.TerrainAltMeters - v.AltMeters) < EchoToleranceMeters) { echoed.Add(s.Index); continue; }
            terrainByIndex.TryAdd(s.Index, s.TerrainAltMeters);   // first answer per vertex wins
        }

        string note = null;
        if (terrainByIndex.TryGetValue(0, out double terrain0)
            && Math.Abs(terrain0 - entityAltMeters) > vertex0NoteThresholdMeters)
            note = $"taskee altitude not terrain-clamped: live {entityAltMeters:F1} m vs terrain " +
                   $"{terrain0:F1} m under vertex 0 (gap {Math.Abs(terrain0 - entityAltMeters):F0} m) - " +
                   "authoring from terrain anyway";

        var keptLive = new List<int>();
        for (int i = 0; i < vertices.Count; i++)
        {
            if (terrainByIndex.TryGetValue(i, out double terrain))
                vertices[i] = new Geodetic { LatDeg = vertices[i].LatDeg, LonDeg = vertices[i].LonDeg,
                                             AltMeters = terrain + clearanceMeters };
            else
                keptLive.Add(i);
        }

        string echoText = echoed.Count == 0 ? ""
            : $" ({echoed.Count} echoed request point(s) at vertex {string.Join(",", echoed)} rejected)";
        if (keptLive.Count == vertices.Count)
            return new Result(vertices, Mode.Fallback, keptLive, "no usable sample for any vertex" + echoText, note);
        if (keptLive.Count > 0)
            return new Result(vertices, Mode.Partial, keptLive,
                $"vertices {string.Join(",", keptLive)} had no usable sample - kept Live altitude" + echoText, note);
        return new Result(vertices, Mode.Terrain, keptLive, "all vertices authored from terrain", note);
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
