namespace VrfC2SimApp;

/// <summary>
/// Offline check of PlacementPolicy (<c>--placement-selftest</c>). No bridge, no VR-Forces.
/// Exists because the 8 pre-existing self-tests all passed the moment the placement branch was
/// rewritten on 2026-09-05 - i.e. none of them exercised it (lessons-false-greens: a green that
/// cannot fail is not a gate). Every case below encodes a documented rule from PlacementPolicy
/// and at least one would FAIL under each retired behaviour:
///   - the 10000 m MSL birth (a create altitude must never be 10000),
///   - the skipped SetAltitude for ground units (a land unit with no altitude MUST get AGL 0),
///   - the oracle's ElevationAgl+1 (no "+1" anywhere),
///   - the SIDC 'G' test (the decision keys on DIS domain only),
///   - "create at 0 and let the clamp sort it out" (with a terrain height in hand a LAND create
///     must be at terrain + clearance and must NOT be 0 - the 2026-09-05 terrain-anchored change).
/// The FALLBACK block is a REGRESSION LOCK: with no terrain height every row must still produce
/// exactly the values the pre-terrain-query code produced, because that is what the app falls back
/// to when the query is not sent or not answered.
/// </summary>
public static class PlacementSelfTest
{
    private static int _fail;
    private const double AirDefault = 1000.0;
    private const double Clearance = 1.0;     // VrfSettings.CreateClearanceMeters default
    private const double Terrain = 1150.0;    // the R9 Mojave AOI terrain height, near enough

    public static int Run()
    {
        Console.WriteLine("=== PlacementPolicy self-test ===");

        // ---- FALLBACK: terrain height UNKNOWN. These are the pre-2026-09-05 values, unchanged. ----
        Console.WriteLine("  -- terrain height UNKNOWN (the fallback path: no reply / timeout / not sent) --");

        // LAND - the case every unit in data/ actually hits (no altitude element at all)
        Case("land, no C2SIM altitude -> create 0, AGL 0 (on the ground)",
             PlacementPolicy.DomainLand, null, null, null, createAlt: 0.0, setAgl: 0.0, fromTerrain: false);
        Case("land, AGL 5 -> create 0, AGL 5",
             PlacementPolicy.DomainLand, 5.0, null, null, createAlt: 0.0, setAgl: 5.0, fromTerrain: false);
        Case("land, MSL 1200 -> create 1200, no AGL set",
             PlacementPolicy.DomainLand, null, 1200.0, null, createAlt: 1200.0, setAgl: null, fromTerrain: false);
        Case("land, AGL 2 AND MSL 1200 -> AGL wins for the set, MSL kept at create",
             PlacementPolicy.DomainLand, 2.0, 1200.0, null, createAlt: 1200.0, setAgl: 2.0, fromTerrain: false);

        // AIR - the only domain that gets an invented default, and it must be the named knob
        Case("air, no C2SIM altitude -> create 0, AGL = AirDefaultAltitudeAglMeters",
             PlacementPolicy.DomainAir, null, null, null, createAlt: 0.0, setAgl: AirDefault, fromTerrain: false);
        Case("air, AGL 300 -> AGL 300 (not the default)",
             PlacementPolicy.DomainAir, 300.0, null, null, createAlt: 0.0, setAgl: 300.0, fromTerrain: false);
        Case("air, MSL 2500 -> create 2500, no AGL set",
             PlacementPolicy.DomainAir, null, 2500.0, null, createAlt: 2500.0, setAgl: null, fromTerrain: false);

        // SURFACE / SUBSURFACE - sim-side water handling; never an invented AGL
        Case("surface, no altitude -> create 0, no set",
             PlacementPolicy.DomainSurface, null, null, null, createAlt: 0.0, setAgl: null, fromTerrain: false);
        Case("subsurface, MSL -50 -> create -50, no set",
             PlacementPolicy.DomainSubsurface, null, -50.0, null, createAlt: -50.0, setAgl: null, fromTerrain: false);
        Case("unknown domain 0, no altitude -> create 0, no set (never invents)",
             0, null, null, null, createAlt: 0.0, setAgl: null, fromTerrain: false);

        // ---- TERRAIN KNOWN: create AT the surface (UG52 14.3.3; MAK's own sample). ----
        Console.WriteLine("  -- terrain height KNOWN (1150 m, the terrain-anchored create) --");

        Case("land, no C2SIM altitude, terrain 1150 -> create 1151 (terrain + clearance), AGL 0",
             PlacementPolicy.DomainLand, null, null, Terrain, createAlt: Terrain + Clearance, setAgl: 0.0, fromTerrain: true);
        Case("land, AGL 5, terrain 1150 -> create 1151 (the AGL drives the SET, not the create), AGL 5",
             PlacementPolicy.DomainLand, 5.0, null, Terrain, createAlt: Terrain + Clearance, setAgl: 5.0, fromTerrain: true);
        Case("land, MSL 1200, terrain 1150 -> create 1200: C2SIM MSL WINS over the terrain query",
             PlacementPolicy.DomainLand, null, 1200.0, Terrain, createAlt: 1200.0, setAgl: null, fromTerrain: false);
        Case("land, AGL 2 AND MSL 1200, terrain 1150 -> create 1200 (MSL wins), AGL 2",
             PlacementPolicy.DomainLand, 2.0, 1200.0, Terrain, createAlt: 1200.0, setAgl: 2.0, fromTerrain: false);

        Case("air, no C2SIM altitude, terrain 1150 -> create terrain + the AGL knob, AGL = knob",
             PlacementPolicy.DomainAir, null, null, Terrain, createAlt: Terrain + AirDefault, setAgl: AirDefault, fromTerrain: true);
        Case("air, AGL 300, terrain 1150 -> create 1450 (terrain + AGL), AGL 300",
             PlacementPolicy.DomainAir, 300.0, null, Terrain, createAlt: Terrain + 300.0, setAgl: 300.0, fromTerrain: true);
        Case("air, MSL 2500, terrain 1150 -> create 2500 (MSL wins), no AGL set",
             PlacementPolicy.DomainAir, null, 2500.0, Terrain, createAlt: 2500.0, setAgl: null, fromTerrain: false);

        Case("surface, no altitude, terrain 1150 -> create 0: sea level, NOT the terrain (UG52 14.3.3)",
             PlacementPolicy.DomainSurface, null, null, Terrain, createAlt: 0.0, setAgl: null, fromTerrain: false);
        Case("subsurface, no altitude, terrain 1150 -> create 0 (sea level), no set",
             PlacementPolicy.DomainSubsurface, null, null, Terrain, createAlt: 0.0, setAgl: null, fromTerrain: false);
        Case("subsurface, MSL -50, terrain 1150 -> create -50, no set",
             PlacementPolicy.DomainSubsurface, null, -50.0, Terrain, createAlt: -50.0, setAgl: null, fromTerrain: false);
        Case("unknown domain 0, no altitude, terrain 1150 -> create 0 (never invents, even with terrain)",
             0, null, null, Terrain, createAlt: 0.0, setAgl: null, fromTerrain: false);

        // ---- NEGATIVE CONTROLS - the retired behaviours must be ABSENT ----
        {
            var d = Decide(PlacementPolicy.DomainLand, null, null, null);
            Check("no 10000 m birth anywhere", d.CreateAltMeters != 10000.0 && d.SetAglMeters != 10000.0);
            Check("no oracle +1 for land", d.SetAglMeters is double s && Math.Abs(s - 1.0) > 1e-9);
            Check("land with no altitude is NOT left unset (the skipped-SetAltitude bug)", d.SetAglMeters.HasValue);
            Check("fallback land create is EXACTLY the pre-terrain-query value (0)", d.CreateAltMeters == 0.0);
            Check("fallback is never flagged as coming from the terrain", !d.CreateAltFromTerrain);

            var a = PlacementPolicy.Decide(PlacementPolicy.DomainAir, null, null, 42.0, null, Clearance);
            Check("air default is the KNOB, not a literal", a.SetAglMeters == 42.0);

            // The 2026-09-05 change itself: with a terrain height in hand a LAND create is at the
            // surface, and 0 - the value the create clamp had to rescue - is gone.
            var t = Decide(PlacementPolicy.DomainLand, null, null, Terrain);
            Check("land with terrain known does NOT create at 0", t.CreateAltMeters != 0.0);
            Check("land with terrain known creates at terrain + clearance",
                  Math.Abs(t.CreateAltMeters - (Terrain + Clearance)) < 1e-9);
            Check("land with terrain known is flagged as coming from the TERRAIN QUERY", t.CreateAltFromTerrain);
            Check("terrain-anchored land create is still not 10000", t.CreateAltMeters != 10000.0);

            var k = PlacementPolicy.Decide(PlacementPolicy.DomainLand, null, null, AirDefault, 100.0, 7.0);
            Check("the create clearance is the KNOB, not a literal 1.0", Math.Abs(k.CreateAltMeters - 107.0) < 1e-9);

            // No 10000 anywhere, swept: every domain x every altitude combination x terrain
            // known/unknown. Nothing in this table may reintroduce the retired birth altitude.
            bool sawTenThousand = false;
            foreach (int dom in new[] { 0, PlacementPolicy.DomainLand, PlacementPolicy.DomainAir,
                                        PlacementPolicy.DomainSurface, PlacementPolicy.DomainSubsurface })
                foreach (double? agl in new double?[] { null, 0.0, 5.0, 300.0 })
                    foreach (double? msl in new double?[] { null, -50.0, 1200.0 })
                        foreach (double? th in new double?[] { null, 0.0, Terrain })
                        {
                            var r = PlacementPolicy.Decide(dom, agl, msl, AirDefault, th, Clearance);
                            if (r.CreateAltMeters == 10000.0 || r.SetAglMeters == 10000.0) sawTenThousand = true;
                        }
            Check("no 10000 in ANY domain/altitude/terrain combination (5x4x3x3 = 180 rows)", !sawTenThousand);
        }

        Console.WriteLine(_fail == 0 ? "PlacementPolicy self-test PASSED" : $"PlacementPolicy self-test FAILED ({_fail})");
        return _fail == 0 ? 0 : 1;
    }

    private static PlacementPolicy.Decision Decide(int domain, double? agl, double? msl, double? terrain)
        => PlacementPolicy.Decide(domain, agl, msl, AirDefault, terrain, Clearance);

    private static void Case(string label, int domain, double? agl, double? msl, double? terrain,
                             double createAlt, double? setAgl, bool fromTerrain)
    {
        var d = Decide(domain, agl, msl, terrain);
        bool ok = Math.Abs(d.CreateAltMeters - createAlt) < 1e-9
               && (setAgl is null ? d.SetAglMeters is null : d.SetAglMeters is double s && Math.Abs(s - setAgl.Value) < 1e-9)
               && d.CreateAltFromTerrain == fromTerrain
               && !string.IsNullOrWhiteSpace(d.Why);
        Report(label, ok, $"create={d.CreateAltMeters} setAgl={(d.SetAglMeters?.ToString() ?? "null")} " +
                          $"fromTerrain={d.CreateAltFromTerrain} why=\"{d.Why}\"");
    }

    private static void Check(string label, bool ok) => Report(label, ok, "");

    private static void Report(string label, bool ok, string detail)
    {
        if (!ok) _fail++;
        Console.WriteLine($"  [{(ok ? "OK" : "FAIL")}] {label}{(detail.Length > 0 ? "  (" + detail + ")" : "")}");
    }
}
