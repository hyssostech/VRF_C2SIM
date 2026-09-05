namespace VrfC2SimApp;

/// <summary>
/// One unit extracted from a C2SIM Initialization message. String fields mirror
/// the C++ Unit struct (values arrive as XML text). Parsed by InitParser; dispatched
/// by UnitTranslator.
/// </summary>
public record InitUnit
{
    public string Name { get; init; } = "";
    public string Uuid { get; init; } = "";
    public string SystemName { get; init; } = "";
    public string HostilityCode { get; init; } = "";   // e.g. "HO" (hostile)
    public string Latitude { get; init; } = "";
    public string Longitude { get; init; } = "";
    // ORACLE-PARITY STRING, FRAME-AMBIGUOUS: the oracle read whichever altitude element it met into
    // one field and used it as an absolute create altitude (C2SIMxmlHandler.cpp:2438-2439,
    // C2SIMinterface.cpp:1384). Kept ONLY for the Fixed100 byte-parity path. Live/TerrainProfile
    // placement reads the TYPED fields below and never this one.
    public string ElevationAgl { get; init; } = "";
    // The two altitude elements C2SIM actually defines - both OPTIONAL, and they mean different
    // things (C2SIM_SMX_LOX_CWIX2024.xsd :2716-2717; :155 "distance vertically above ground level";
    // :163 "distance vertically above mean sea level"). null = the element was absent. Every init
    // in data/ carries NEITHER (checked 2026-09-05), so the absent case is the normal case.
    public double? AltitudeAgl { get; init; }
    public double? AltitudeMsl { get; init; }
    public string SymbolId { get; init; } = "";         // APP6C SIDC string
    public string DisEntityType { get; init; } = "";    // "k.d.c.cat.sub.spec.extra"
    public int DisDomain { get; init; }
    public string DirectionPhi { get; init; } = "";     // heading source (may be empty)
    public string SuperiorUuid { get; init; } = "";     // for the missing-coords fallback
    // C2SIM UnitType/EchelonCode (COY, BN, PLT, SECT, NOS, BDE, ...). Read by the
    // FidelityTable lookup key (c) as a cross-check when the SIDC echelon character has
    // no row (docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md sec 7.1).
    public string EchelonCode { get; init; } = "";
}

/// <summary>A tactical area / control graphic (perimeter of geodetic points).</summary>
public record InitArea
{
    public string Name { get; init; } = "";
    public string Uuid { get; init; } = "";
    public List<(double Lat, double Lon, double Elev)> Points { get; init; } = new();
}

/// <summary>The parsed contents of a C2SIM Initialization message.</summary>
public class InitData
{
    public string SystemName { get; set; } = "";
    public List<InitUnit> Units { get; set; } = new();
    public List<InitArea> Areas { get; set; } = new();
}
