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

/// <summary>
/// A TASKGRAPHIC: the FM 3-90 Appendix B tactical-mission-task symbol STP draws for a task
/// (a breach's two arms, a clear's limit-of-advance bar, a screen's front, a follow-and-support
/// arrow). Schema-wise it is a plain sibling of Line / Point / TacticalArea inside the
/// TacticalGraphic choice (C2SIM_SMX_LOX_CWIX2024.xsd:4934-4959) with the same three groups and
/// the same geometry slot - CurrentState/PhysicalState/Location, 1..n - so there is nothing
/// exotic to parse; it was simply skipped.
///
/// WHAT ITS POINTS MEAN, AND WHY THEY ARE STILL READ AS A PATH. The points are the SYMBOL's
/// anchors, not a route and not a polygon (docs/experiments/DESIGN_V4B_EMBEDDED_LOCATION_2026-09-14.md
/// :141-151, quoting FM 3-90 B-8 for BREACH and B-17 for CLEAR). But the interface ALREADY drives
/// exactly these coordinates whenever STP linearises the same symbol into embedded Locations - V4b
/// settled that reading and measured the cost as "a 629 m drive around the symbol" - so
/// registering the graphic makes the MapGraphicID path agree with the embedded path instead of
/// disagreeing with it. Binding the arms to the vendor parameters they really are
/// (company_breach lane1/lane2, co_clear limit of advance) stays V5/V6 work, and the resolver's
/// log says "task symbol" so that work does not have to re-derive this from the XML.
/// </summary>
public record InitTaskGraphic
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
    // V3 (docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md): the line and point
    // tactical graphics the parser used to discard. They are the parameter supply for every
    // vendor tactical task that takes a line (line of departure, limit of advance, breach
    // lane) or a control point. Created only when Vrf:CreateInitLines / Vrf:CreateInitPoints
    // are on - parsing them is unconditional and free.
    public List<InitLine> Lines { get; set; } = new();
    public List<InitPoint> Points { get; set; } = new();
    // The TaskGraphic wrappers the parser used to skip on the grounds that "nothing in the build
    // list consumes one yet". Something does now: 11 of Iron Storm's 35 MapGraphicID references
    // name one, and every one of them resolved to NOTHING. Parsed unconditionally and free, like
    // the lines and points; NEVER created as a VR-Forces object (a mission symbol is not a control
    // measure), only registered so a MapGraphicID naming one can resolve.
    public List<InitTaskGraphic> TaskGraphics { get; set; } = new();
}
