using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of TerrainVertexAuthoring (GroundWaypointAltitudeMode="TerrainProfile"
/// vertex decision; no bridge start, no MAK runtime, no VR-Forces):
/// `VrfC2SimApp --terrain-selftest`. Asserts terrain replacement, per-vertex fallback,
/// the no-reply / empty-reply fallbacks, the vertex-0 "not terrain-clamped" note, the
/// horizontal mismatch rejection, the echoed-request-point rejection and index handling
/// (docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 3.3; review 2026-09-01 F2/F3).
/// </summary>
public static class TerrainSelfTest
{
    private const double Clearance = 10.0;
    private const double EntityAlt = 1100.0;          // Mojave-like live altitude
    private const double LiveClearance = 50.0;

    public static int Run()
    {
        int failures = 0;
        // Live-mode route: point 0 = live location + 50, two task points at the same alt.
        var live = new List<Geodetic>
        {
            V(34.60, -116.55, EntityAlt + LiveClearance),
            V(34.61, -116.54, EntityAlt + LiveClearance),
            V(34.62, -116.53, EntityAlt + LiveClearance),
        };

        // 1. Full reply -> every vertex = terrain + clearance, lat/lon untouched.
        {
            var samples = new List<TerrainHeightSample> { S(0, 34.60, -116.55, 1098.0), S(1, 34.61, -116.54, 1120.5), S(2, 34.62, -116.53, 1140.0) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Terrain, "full reply -> Mode.Terrain");
            Check(ref failures, r.KeptLive.Count == 0, "full reply -> nothing kept Live");
            Check(ref failures, Near(r.Vertices[0].AltMeters, 1108.0) && Near(r.Vertices[1].AltMeters, 1130.5) && Near(r.Vertices[2].AltMeters, 1150.0),
                  $"full reply -> terrain + clearance (got {Alts(r)})");
            Check(ref failures, r.Vertices[1].LatDeg == 34.61 && r.Vertices[1].LonDeg == -116.54, "lat/lon never changed");
            Check(ref failures, live[0].AltMeters == EntityAlt + LiveClearance, "input list not mutated");
        }

        // 2. No reply (timeout) -> Fallback, identical to Live.
        {
            var r = TerrainVertexAuthoring.Apply(live, null, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Fallback, "null samples -> Fallback");
            Check(ref failures, r.Vertices.Select(v => v.AltMeters).SequenceEqual(live.Select(v => v.AltMeters)), "null samples -> Live altitudes");
            Check(ref failures, r.KeptLive.Count == 3, "null samples -> all indices kept Live");
        }

        // 3. Empty reply -> Fallback.
        {
            var r = TerrainVertexAuthoring.Apply(live, new List<TerrainHeightSample>(), Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Fallback, "empty samples -> Fallback");
        }

        // 4. One vertex invalid (no terrain data) -> Partial; that vertex keeps Live.
        {
            var samples = new List<TerrainHeightSample> { S(0, 34.60, -116.55, 1098.0), Invalid(1), S(2, 34.62, -116.53, 1140.0) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Partial, "one invalid -> Partial");
            Check(ref failures, r.KeptLive.SequenceEqual(new[] { 1 }), "one invalid -> index 1 kept Live");
            Check(ref failures, Near(r.Vertices[1].AltMeters, EntityAlt + LiveClearance) && Near(r.Vertices[2].AltMeters, 1150.0),
                  $"one invalid -> others authored (got {Alts(r)})");
        }

        // 5. Vertex-0 terrain far below the entity's live altitude (unclamped taskee, e.g. born
        //    high, or an aggregate's published altitude): still authored from terrain; the gap is a Note,
        //    not a fallback and not a frame claim (review F3).
        {
            var samples = new List<TerrainHeightSample> { S(0, 34.60, -116.55, 100.0), S(1, 34.61, -116.54, 120.0), S(2, 34.62, -116.53, 140.0) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Terrain, "vertex-0 1000 m off -> still Mode.Terrain");
            Check(ref failures, Near(r.Vertices[0].AltMeters, 110.0) && Near(r.Vertices[2].AltMeters, 150.0), $"vertex-0 1000 m off -> terrain + clearance (got {Alts(r)})");
            Check(ref failures, r.Note != null && r.Note.Contains("not terrain-clamped") && !r.Note.Contains("frame"),
                  "vertex-0 gap -> Note says 'not terrain-clamped', never 'frame'");
        }

        // 6. Vertex-0 within the note threshold (entity 1100, terrain 1040 -> 60 m) -> no Note.
        {
            var samples = new List<TerrainHeightSample> { S(0, 34.60, -116.55, 1040.0), S(1, 34.61, -116.54, 1120.0), S(2, 34.62, -116.53, 1140.0) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Terrain && r.Note == null, "vertex-0 60 m off -> accepted, no Note");
        }

        // 7. A sample horizontally displaced from its vertex (> 50 m) is not an answer for it.
        {
            var samples = new List<TerrainHeightSample> { S(0, 34.60, -116.55, 1098.0), S(1, 34.61 + 0.002, -116.54, 1120.0), S(2, 34.62, -116.53, 1140.0) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Partial && r.KeptLive.SequenceEqual(new[] { 1 }),
                  "displaced sample -> its vertex kept Live");
        }

        // 8. Index handling: out-of-range ignored, order irrelevant, duplicate first-wins.
        {
            var samples = new List<TerrainHeightSample> { S(2, 34.62, -116.53, 1140.0), S(7, 34.62, -116.53, 999.0), S(0, 34.60, -116.55, 1098.0), S(0, 34.60, -116.55, 1000.0), S(1, 34.61, -116.54, 1120.0) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Terrain, "unordered + out-of-range -> Terrain");
            Check(ref failures, Near(r.Vertices[0].AltMeters, 1108.0), $"duplicate index -> first sample wins (got {r.Vertices[0].AltMeters:F1})");
        }

        // 9. Single-vertex route (MoveToLocation path) works the same.
        {
            var one = new List<Geodetic> { V(34.60, -116.55, EntityAlt + LiveClearance) };
            var r = TerrainVertexAuthoring.Apply(one, new List<TerrainHeightSample> { S(0, 34.60, -116.55, 1098.0) }, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Terrain && Near(r.Vertices[0].AltMeters, 1108.0), "single vertex authored");
        }

        // 10. All samples invalid -> Fallback (not Partial).
        {
            var r = TerrainVertexAuthoring.Apply(live, new List<TerrainHeightSample> { Invalid(0), Invalid(1), Invalid(2) }, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Fallback, "all invalid -> Fallback");
        }

        // 11. Echo (review F2): every sample equals its request vertex altitude (live + 50) ->
        //     not terrain; whole route falls back and the reason names the echo.
        {
            double reqAlt = EntityAlt + LiveClearance;
            var samples = new List<TerrainHeightSample> { S(0, 34.60, -116.55, reqAlt), S(1, 34.61, -116.54, reqAlt + 0.005), S(2, 34.62, -116.53, reqAlt - 0.009) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Fallback, "all echoed -> Fallback");
            Check(ref failures, r.Reason.Contains("3 echoed"), $"all echoed -> reason counts 3 echoes (got '{r.Reason}')");
            Check(ref failures, r.Vertices.Select(v => v.AltMeters).SequenceEqual(live.Select(v => v.AltMeters)), "all echoed -> Live altitudes (never live + 60)");
        }

        // 12. Echo on one vertex only -> that vertex kept Live (Partial); a 2 cm difference is NOT an echo.
        {
            double reqAlt = EntityAlt + LiveClearance;
            var samples = new List<TerrainHeightSample> { S(0, 34.60, -116.55, 1098.0), S(1, 34.61, -116.54, reqAlt), S(2, 34.62, -116.53, reqAlt + 0.02) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Partial && r.KeptLive.SequenceEqual(new[] { 1 }), "one echoed -> Partial, index 1 kept Live");
            Check(ref failures, Near(r.Vertices[2].AltMeters, reqAlt + 0.02 + Clearance), "2 cm off the request altitude -> accepted as terrain");
        }

        // 13. Vertex 0 invalid, others valid -> Partial with index 0 kept Live, no Note (nothing to compare).
        {
            var samples = new List<TerrainHeightSample> { Invalid(0), S(1, 34.61, -116.54, 1120.5), S(2, 34.62, -116.53, 1140.0) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Partial && r.KeptLive.SequenceEqual(new[] { 0 }), "vertex 0 invalid -> Partial, index 0 kept Live");
            Check(ref failures, Near(r.Vertices[0].AltMeters, EntityAlt + LiveClearance) && Near(r.Vertices[1].AltMeters, 1130.5) && r.Note == null,
                  $"vertex 0 invalid -> vertex 0 Live, others terrain, no Note (got {Alts(r)})");
        }

        // 14. Wrong-frame reply (every point far from its vertex) -> Fallback "no usable sample":
        //     this, not a vertical gap, is the frame falsifier (review F3).
        {
            var samples = new List<TerrainHeightSample> { S(0, 35.60, -117.55, 1098.0), S(1, 35.61, -117.54, 1120.0), S(2, 35.62, -117.53, 1140.0) };
            var r = TerrainVertexAuthoring.Apply(live, samples, Clearance, EntityAlt);
            Check(ref failures, r.Mode == TerrainVertexAuthoring.Mode.Fallback && r.Reason.StartsWith("no usable sample"), "all displaced -> Fallback 'no usable sample'");
        }

        Console.WriteLine(failures == 0 ? "terrain-selftest: PASS" : $"terrain-selftest: {failures} FAILURE(S)");
        return failures;
    }

    private static Geodetic V(double lat, double lon, double alt) => new() { LatDeg = lat, LonDeg = lon, AltMeters = alt };
    private static TerrainHeightSample S(int idx, double lat, double lon, double terrain) =>
        new() { Index = idx, Valid = true, LatDeg = lat, LonDeg = lon, TerrainAltMeters = terrain };
    private static TerrainHeightSample Invalid(int idx) => new() { Index = idx, Valid = false };
    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-6;
    private static string Alts(TerrainVertexAuthoring.Result r) => string.Join(", ", r.Vertices.Select(v => v.AltMeters.ToString("F1")));

    private static void Check(ref int failures, bool ok, string what)
    {
        Console.WriteLine($"  [{(ok ? "ok" : "FAIL")}] {what}");
        if (!ok) failures++;
    }
}
