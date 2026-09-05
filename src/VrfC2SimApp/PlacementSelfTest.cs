namespace VrfC2SimApp;

/// <summary>
/// Offline check of PlacementPolicy (<c>--placement-selftest</c>). No bridge, no VR-Forces.
/// Exists because the 8 pre-existing self-tests all passed the moment the placement branch was
/// rewritten on 2026-09-05 - i.e. none of them exercised it (lessons-false-greens: a green that
/// cannot fail is not a gate). Every case below encodes a documented rule from PlacementPolicy
/// and at least one would FAIL under each retired behaviour:
///   - the 10000 m MSL birth (create altitude must be 0 when C2SIM gives no MSL),
///   - the skipped SetAltitude for ground units (a land unit with no altitude MUST get AGL 0),
///   - the oracle's ElevationAgl+1 (no "+1" anywhere),
///   - the SIDC 'G' test (the decision keys on DIS domain only).
/// </summary>
public static class PlacementSelfTest
{
    private static int _fail;
    private const double AirDefault = 1000.0;

    public static int Run()
    {
        Console.WriteLine("=== PlacementPolicy self-test ===");

        // LAND - the case every unit in data/ actually hits (no altitude element at all)
        Case("land, no C2SIM altitude -> create 0, AGL 0 (on the ground)",
             PlacementPolicy.DomainLand, null, null, createAlt: 0.0, setAgl: 0.0);
        Case("land, AGL 5 -> create 0, AGL 5",
             PlacementPolicy.DomainLand, 5.0, null, createAlt: 0.0, setAgl: 5.0);
        Case("land, MSL 1200 -> create 1200, no AGL set",
             PlacementPolicy.DomainLand, null, 1200.0, createAlt: 1200.0, setAgl: null);
        Case("land, AGL 2 AND MSL 1200 -> AGL wins for the set, MSL kept at create",
             PlacementPolicy.DomainLand, 2.0, 1200.0, createAlt: 1200.0, setAgl: 2.0);

        // AIR - the only domain that gets an invented default, and it must be the named knob
        Case("air, no C2SIM altitude -> create 0, AGL = AirDefaultAltitudeAglMeters",
             PlacementPolicy.DomainAir, null, null, createAlt: 0.0, setAgl: AirDefault);
        Case("air, AGL 300 -> AGL 300 (not the default)",
             PlacementPolicy.DomainAir, 300.0, null, createAlt: 0.0, setAgl: 300.0);
        Case("air, MSL 2500 -> create 2500, no AGL set",
             PlacementPolicy.DomainAir, null, 2500.0, createAlt: 2500.0, setAgl: null);

        // SURFACE / SUBSURFACE - sim-side water handling; never an invented AGL
        Case("surface, no altitude -> create 0, no set",
             PlacementPolicy.DomainSurface, null, null, createAlt: 0.0, setAgl: null);
        Case("subsurface, MSL -50 -> create -50, no set",
             PlacementPolicy.DomainSubsurface, null, -50.0, createAlt: -50.0, setAgl: null);
        Case("unknown domain 0, no altitude -> create 0, no set (never invents)",
             0, null, null, createAlt: 0.0, setAgl: null);

        // NEGATIVE CONTROLS - the retired behaviours must be ABSENT
        {
            var d = PlacementPolicy.Decide(PlacementPolicy.DomainLand, null, null, AirDefault);
            Check("no 10000 m birth anywhere", d.CreateAltMeters != 10000.0 && d.SetAglMeters != 10000.0);
            Check("no oracle +1 for land", d.SetAglMeters is double s && Math.Abs(s - 1.0) > 1e-9);
            Check("land with no altitude is NOT left unset (the skipped-SetAltitude bug)", d.SetAglMeters.HasValue);
            var a = PlacementPolicy.Decide(PlacementPolicy.DomainAir, null, null, 42.0);
            Check("air default is the KNOB, not a literal", a.SetAglMeters == 42.0);
        }

        Console.WriteLine(_fail == 0 ? "PlacementPolicy self-test PASSED" : $"PlacementPolicy self-test FAILED ({_fail})");
        return _fail == 0 ? 0 : 1;
    }

    private static void Case(string label, int domain, double? agl, double? msl, double createAlt, double? setAgl)
    {
        var d = PlacementPolicy.Decide(domain, agl, msl, AirDefault);
        bool ok = Math.Abs(d.CreateAltMeters - createAlt) < 1e-9
               && (setAgl is null ? d.SetAglMeters is null : d.SetAglMeters is double s && Math.Abs(s - setAgl.Value) < 1e-9)
               && !string.IsNullOrWhiteSpace(d.Why);
        Report(label, ok, $"create={d.CreateAltMeters} setAgl={(d.SetAglMeters?.ToString() ?? "null")} why=\"{d.Why}\"");
    }

    private static void Check(string label, bool ok) => Report(label, ok, "");

    private static void Report(string label, bool ok, string detail)
    {
        if (!ok) _fail++;
        Console.WriteLine($"  [{(ok ? "OK" : "FAIL")}] {label}{(detail.Length > 0 ? "  (" + detail + ")" : "")}");
    }
}
