using System.Globalization;
using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// What to create in VR-Forces for one C2SIM unit. Pure data - the service turns it
/// into bridge.CreateEntity / CreateAggregate + a deferred SetAltitude.
/// </summary>
public readonly record struct CreationPlan(
    bool IsAggregate,
    EntityTypeSpec Type,
    Force Force,
    double HeadingDeg,
    string Name,
    Geodetic Pos,
    double? PostCreateAltitude); // meters; null = do not set altitude after create

/// <summary>
/// Faithful port of the C++ extractC2simInit dispatch + the create* factories
/// (C2SIMinterface.cpp). Parity-first: reproduces the exact DIS entity types, force
/// mapping, per-factory heading formulas and post-create altitude rules, so the
/// resulting VR-Forces command stream matches the golden trace. This is PURE (no
/// bridge / MAK dependency) so it can be reviewed and tested in isolation.
/// </summary>
public static class UnitTranslator
{
    // C2SIMinterface.cpp:82 - const double degreesToRadians = 57.2957795131.
    // (It is actually a radians<->degrees factor; the mixed-unit use below is a
    // deliberate quirk reproduced for parity - the facade divides by the same factor.)
    private const double DegreesToRadians = 57.2957795131;

    /// <summary>
    /// Map one unit to its creation plan, reproducing extractC2simInit's dispatch
    /// (C2SIMinterface.cpp:1452-1501). Assumes coordinates + hostility are present
    /// (the caller guards those); ElevationAgl should already be defaulted to
    /// "1000.0" when the source was empty (parity with :1445-1446).
    /// </summary>
    public static CreationPlan Plan(InitUnit u)
    {
        var pos = new Geodetic { LatDeg = D(u.Latitude), LonDeg = D(u.Longitude), AltMeters = D(u.ElevationAgl) };
        bool ho = u.HostilityCode == "HO";
        string sidc = u.SymbolId ?? "";

        if (sidc.Length > 0)
        {
            char echelon = At(sidc, 11);
            if (At(sidc, 2) == 'A') // air (:1457)
            {
                if (u.DisEntityType is "1.2.225.50.0.1.0" or "1.2.71.50.1.1.0" or "1.2.78.50.1.0.0")
                    return Rw(u, pos, ho);          // :1458-1462
                if (At(sidc, 14) == '*')
                    return Mq1(u, pos, ho);         // :1463-1464 Predator
                return Rw(u, pos, ho);              // :1465-1466
            }
            if (At(sidc, 2) == 'S') return Boat(u, pos, ho);      // :1468 sea surface
            if (u.DisDomain == 3) return Boat(u, pos, ho);        // :1470
            if (At(sidc, 1) == 'N') return Civilian(u, pos);      // :1472 neutral
            if (echelon == 'B') return ho ? MobileIrregular(u, pos) : ScoutUnit(u, pos); // :1474-1478
            if (echelon == 'D') return ArmorPlatoon(u, pos, ho);  // :1480
            if (echelon == 'E') return ArmorCompany(u, pos, ho);  // :1482
            if (echelon == 'F') return ArmorCoHQ(u, pos, ho);     // :1484 battalion -> Co HQ
            return Tank(u, pos, ho);                              // :1486 default
        }
        // no SIDC (:1491-1500)
        return ho ? Truck(u, pos) : Tank(u, pos, ho);
    }

    // ---- entity factories -------------------------------------------------

    private static CreationPlan Rw(InitUnit u, Geodetic pos, bool ho) // createRW
    {
        var type = u.DisEntityType == "1.2.225.23.1.1.0"
            ? Spec(1, 2, 225, 23, 1, 1, 0)   // CH-47 Chinook
            : Spec(1, 2, 225, 20, 1, 1, 0);  // AH-64 (default)
        return new(false, type, ForceOf(ho), HeadingDeg(u.DirectionPhi, divide: true),
                   u.Name, pos, D(u.ElevationAgl) + 1.0);
    }

    private static CreationPlan Mq1(InitUnit u, Geodetic pos, bool ho) // createMQ1
    {
        // override the ground-clamping default so it does not start orbiting (:840)
        double alt = u.ElevationAgl == "1000.0" ? 0.0 : D(u.ElevationAgl);
        return new(false, Spec(1, 2, 225, 50, 4, 4, 0), ForceOf(ho),
                   HeadingDeg(u.DirectionPhi, divide: true), u.Name, pos, alt);
    }

    private static CreationPlan Boat(InitUnit u, Geodetic pos, bool ho) // createBoat
    {
        var type = ho ? Spec(1, 3, 0, 84, 1, 0, 0) : Spec(1, 3, 0, 61, 11, 0, 1);
        return new(false, type, ForceOf(ho), HeadingDeg(u.DirectionPhi, divide: false),
                   u.Name, pos, D(u.ElevationAgl) + 1.0);
    }

    private static CreationPlan Truck(InitUnit u, Geodetic pos) // createTruck (HO only)
        => new(false, Spec(1, 1, 225, 7, 12, 30, 1), Force.Opposing,
               HeadingDeg(u.DirectionPhi, divide: false), u.Name, pos, D(u.ElevationAgl) + 1.0);

    private static CreationPlan Tank(InitUnit u, Geodetic pos, bool ho) // createTank
        => new(false, Spec(1, 1, 225, 1, 1, 3, 0), ForceOf(ho),
               HeadingDeg(u.DirectionPhi, divide: false), u.Name, pos, D(u.ElevationAgl) + 1.0);

    private static CreationPlan Civilian(InitUnit u, Geodetic pos) // createCivilian (no SetAltitude)
        => new(false, Spec(3, 1, 225, 3, 0, 1, 0), Force.Neutral,
               HeadingDeg(u.DirectionPhi, divide: false), u.Name, pos, null);

    // ---- aggregate factories (Disaggregated + subordinates; heading dropped to 0) ----

    private static CreationPlan ScoutUnit(InitUnit u, Geodetic pos) // createScoutUnit (Friendly always)
        => new(true, Spec(11, 1, 225, 2, 1, 1, 0), Force.Friendly, 0.0, u.Name, pos, null);

    private static CreationPlan ArmorPlatoon(InitUnit u, Geodetic pos, bool ho)
        => new(true, Spec(11, 1, 225, 1, 1, 3, 0), ForceOf(ho), 0.0, u.Name, pos, null);

    private static CreationPlan ArmorCompany(InitUnit u, Geodetic pos, bool ho)
        => new(true, Spec(11, 1, 225, 5, 2, 0, 0), ForceOf(ho), 0.0, u.Name, pos, null);

    private static CreationPlan ArmorCoHQ(InitUnit u, Geodetic pos, bool ho)
        => new(true, Spec(11, 1, 225, 5, 20, 0, 0), ForceOf(ho), 0.0, u.Name, pos, null);

    private static CreationPlan MobileIrregular(InitUnit u, Geodetic pos) // Opposing always
        => new(true, Spec(11, 1, 0, 13, 34, 0, 1), Force.Opposing, 0.0, u.Name, pos, null);

    // ---- helpers ----------------------------------------------------------

    // RW/MQ1 pre-divide phi by the factor; Boat/Truck/Tank/Civilian do not. Both
    // then multiply by it (the facade divides once more internally). Empty phi -> 90.
    private static double HeadingDeg(string phi, bool divide)
    {
        double h = string.IsNullOrEmpty(phi) ? 90.0 : (divide ? D(phi) / DegreesToRadians : D(phi));
        return h * DegreesToRadians;
    }

    private static Force ForceOf(bool ho) => ho ? Force.Opposing : Force.Friendly;

    private static EntityTypeSpec Spec(int k, int d, int c, int cat, int sub, int spec, int extra)
        => new() { Kind = k, Domain = d, Country = c, Category = cat, Subcategory = sub, Specific = spec, Extra = extra };

    private static char At(string s, int i) => i < s.Length ? s[i] : '\0';

    private static double D(string s)
        => string.IsNullOrEmpty(s) ? 0.0 : double.Parse(s, CultureInfo.InvariantCulture);
}
