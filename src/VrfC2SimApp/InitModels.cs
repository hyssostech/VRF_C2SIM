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
    // The AUTHORED <Subordinate> uuid order from UnitType.Subordinate[] - first = the unit leader
    // (UG52 18.1.1). Drives the compose attach order (N2, ComposeOrder.ByDeclared). Empty when the
    // init declares none (the child->Superior links alone still define the tree).
    public IReadOnlyList<string> DeclaredSubordinates { get; init; } = Array.Empty<string>();
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

/// <summary>
/// A LINE tactical graphic: an ordered polyline of geodetic points - phase line, line of
/// departure, limit of advance, boundary, breach lane. C2SIM wraps two different element
/// types in one: LineType.Item is a RouteType or a BoundaryType
/// (C2SIMSDK\C2SIM_SMX_LOX_CWIX2024.cs:8645-8658), both carrying Name / UUID / CurrentState.
/// <see cref="Kind"/> keeps which one it was, because the two mean different things to a
/// planner even though the geometry is identical. COA-STP1 ships 37 Route + 4 Boundary.
/// </summary>
public record InitLine
{
    public string Name { get; init; } = "";
    public string Uuid { get; init; } = "";
    public string Kind { get; init; } = "";   // "Route" | "Boundary"
    public List<(double Lat, double Lon, double Elev)> Points { get; init; } = new();
}

/// <summary>
/// A POINT tactical graphic: ONE geodetic position (C2SIM PointType,
/// C2SIMSDK\C2SIM_SMX_LOX_CWIX2024.cs:8990). COA-STP1's 317 points are unit and logistics
/// nodes (1-35_AR_LRP, DP1, ...). Every one of the 317 carries exactly one Location
/// (measured 2026-09-14), so the extra coordinates of a malformed point are kept in
/// <see cref="Points"/> rather than silently dropped, and the FIRST is the position.
/// </summary>
public record InitPoint
{
    public string Name { get; init; } = "";
    public string Uuid { get; init; } = "";
    public List<(double Lat, double Lon, double Elev)> Points { get; init; } = new();
    public bool HasPosition => Points.Count > 0;
    public (double Lat, double Lon, double Elev) Position => Points.Count > 0 ? Points[0] : (0, 0, 0);
}

/// <summary>The parsed contents of a C2SIM Initialization message.</summary>
public class InitData
{
    public string SystemName { get; set; } = "";
    public List<InitUnit> Units { get; set; } = new();
    public List<InitArea> Areas { get; set; } = new();
    // V3 (docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md): the line and point
    // tactical graphics the parser used to discard. They are the parameter supply for every
    // vendor tactical task that takes a line (line of departure, limit of advance, breach
    // lane) or a control point. Created only when Vrf:CreateInitLines / Vrf:CreateInitPoints
    // are on - parsing them is unconditional and free.
    public List<InitLine> Lines { get; set; } = new();
    public List<InitPoint> Points { get; set; } = new();
}
