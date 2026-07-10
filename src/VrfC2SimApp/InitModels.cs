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
    public string ElevationAgl { get; init; } = "";
    public string SymbolId { get; init; } = "";         // APP6C SIDC string
    public string DisEntityType { get; init; } = "";    // "k.d.c.cat.sub.spec.extra"
    public int DisDomain { get; init; }
    public string DirectionPhi { get; init; } = "";     // heading source (may be empty)
    public string SuperiorUuid { get; init; } = "";     // for the missing-coords fallback
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
