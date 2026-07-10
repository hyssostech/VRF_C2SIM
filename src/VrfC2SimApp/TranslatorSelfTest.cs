using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// Offline parity check of UnitTranslator against the C++ create* factories. Runs
/// with `--translator-selftest` (needs the MAK bin dirs on PATH because it loads the
/// bridge assembly for the value types, but NOT VR-Forces). No network, no Start.
/// </summary>
public static class TranslatorSelfTest
{
    private const double Deg2Rad = 57.2957795131;
    private static int _fail;

    public static int Run()
    {
        Console.WriteLine("=== UnitTranslator parity self-test ===");

        // no SIDC: FR -> Tank, HO -> Truck
        Check("Tank(no-sidc)", U(sidc: "", ho: false), agg: false, Force.Friendly, T(1, 1, 225, 1, 1, 3, 0), alt: 1000.0 + 1);
        Check("Truck(no-sidc,HO)", U(sidc: "", ho: true), agg: false, Force.Opposing, T(1, 1, 225, 7, 12, 30, 1), alt: 1000.0 + 1);

        // air (sidc[2]=='A')
        Check("RW/AH64", U(sidc: "SFA------------"), agg: false, Force.Friendly, T(1, 2, 225, 20, 1, 1, 0), alt: 1000.0 + 1);
        Check("RW/CH47", U(sidc: "SFA------------", type: "1.2.225.23.1.1.0"), agg: false, Force.Friendly, T(1, 2, 225, 23, 1, 1, 0), alt: 1000.0 + 1);
        Check("MQ1", U(sidc: "SFA-----------*"), agg: false, Force.Friendly, T(1, 2, 225, 50, 4, 4, 0), alt: 0.0); // elev 1000.0 -> 0

        // sea + domain
        Check("Boat(sea)", U(sidc: "SFS------------"), agg: false, Force.Friendly, T(1, 3, 0, 61, 11, 0, 1), alt: 1000.0 + 1);
        Check("Boat(HO)", U(sidc: "SFS------------", ho: true), agg: false, Force.Opposing, T(1, 3, 0, 84, 1, 0, 0), alt: 1000.0 + 1);
        Check("Boat(domain3)", U(sidc: "SFG------------", domain: 3), agg: false, Force.Friendly, T(1, 3, 0, 61, 11, 0, 1), alt: 1000.0 + 1);

        // neutral civilian (sidc[1]=='N')
        Check("Civilian", U(sidc: "SNG------------"), agg: false, Force.Neutral, T(3, 1, 225, 3, 0, 1, 0), alt: null);

        // echelon aggregates (sidc[11])
        Check("Scout(B)", U(sidc: "SFG--------B---"), agg: true, Force.Friendly, T(11, 1, 225, 2, 1, 1, 0), alt: null);
        Check("MobIrreg(B,HO)", U(sidc: "SFG--------B---", ho: true), agg: true, Force.Opposing, T(11, 1, 0, 13, 34, 0, 1), alt: null);
        Check("ArmorPlatoon(D)", U(sidc: "SFG--------D---"), agg: true, Force.Friendly, T(11, 1, 225, 1, 1, 3, 0), alt: null);
        Check("ArmorCompany(E)", U(sidc: "SFG--------E---"), agg: true, Force.Friendly, T(11, 1, 225, 5, 2, 0, 0), alt: null);
        Check("ArmorCoHQ(F)", U(sidc: "SFG--------F---"), agg: true, Force.Friendly, T(11, 1, 225, 5, 20, 0, 0), alt: null);
        Check("Tank(sidc default)", U(sidc: "SFG--------X---"), agg: false, Force.Friendly, T(1, 1, 225, 1, 1, 3, 0), alt: 1000.0 + 1);

        // heading formulas: RW divides phi by the factor (headingDeg == phi); Tank does
        // not (headingDeg == phi * factor). phi = 45.
        CheckHeading("RW heading (divide)", U(sidc: "SFA------------", phi: "45"), 45.0);
        CheckHeading("Tank heading (no divide)", U(sidc: "", phi: "45"), 45.0 * Deg2Rad);
        CheckHeading("Tank heading (empty phi)", U(sidc: "", phi: ""), 90.0 * Deg2Rad);

        Console.WriteLine(_fail == 0 ? "\nSELF-TEST PASSED" : $"\nSELF-TEST FAILED ({_fail})");
        return _fail == 0 ? 0 : 1;
    }

    private static InitUnit U(string sidc, bool ho = false, string type = "", int domain = 0, string phi = "")
        => new()
        {
            Name = "u",
            Uuid = "uuid",
            SystemName = "STP",
            HostilityCode = ho ? "HO" : "FR",
            Latitude = "50.0",
            Longitude = "7.0",
            ElevationAgl = "1000.0",
            SymbolId = sidc,
            DisEntityType = type,
            DisDomain = domain,
            DirectionPhi = phi
        };

    private static (int, int, int, int, int, int, int) T(int k, int d, int c, int cat, int sub, int sp, int ex)
        => (k, d, c, cat, sub, sp, ex);

    private static void Check(string label, InitUnit u, bool agg, Force force,
                              (int, int, int, int, int, int, int) type, double? alt)
    {
        var p = UnitTranslator.Plan(u);
        var t = (p.Type.Kind, p.Type.Domain, p.Type.Country, p.Type.Category,
                 p.Type.Subcategory, p.Type.Specific, p.Type.Extra);
        bool ok = p.IsAggregate == agg && p.Force == force && t == type
                  && Nullable.Equals(p.PostCreateAltitude, alt);
        Report(label, ok, $"agg={p.IsAggregate} force={p.Force} type={t} alt={p.PostCreateAltitude}");
    }

    private static void CheckHeading(string label, InitUnit u, double expected)
    {
        var p = UnitTranslator.Plan(u);
        bool ok = Math.Abs(p.HeadingDeg - expected) < 1e-6;
        Report(label, ok, $"headingDeg={p.HeadingDeg} expected={expected}");
    }

    private static void Report(string label, bool ok, string detail)
    {
        if (!ok) _fail++;
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {label}  {(ok ? "" : detail)}");
    }
}
