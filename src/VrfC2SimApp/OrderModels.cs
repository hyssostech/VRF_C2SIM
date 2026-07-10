namespace VrfC2SimApp;

/// <summary>
/// One task extracted from a C2SIM Order (ManeuverWarfareTask). Field names/semantics
/// mirror the C++ Task struct that executeTask consumes (C2SIMinterface.cpp). Values
/// arrive as XML text; numeric coordinates are kept as doubles (the schema types them).
/// Parsed by <see cref="OrderParser"/>; executed by VrfC2SimService.OnOrder.
/// </summary>
public record OrderTask
{
    public string TaskUuid { get; init; } = "";              // ManeuverWarfareTask/UUID
    public string TaskName { get; init; } = "";              // ManeuverWarfareTask/Name (route name = Name + " ROUTE")
    public string TaskeeUuid { get; init; } = "";            // PerformingEntity (C2SIM uuid of the unit to task)
    public string AffectedEntity { get; init; } = "";        // for the SetTarget no-op (PORT.md sec 6 parity bug)
    public string ActionCode { get; init; } = "";            // TaskActionCode (e.g. "MOVE"); discarded by bare movement
    public string RuleOfEngagementCode { get; init; } = "";  // WeaponRuleOfEngagementCode (e.g. "ROETight")
    public string MapGraphicUuid { get; init; } = "";        // MapGraphicID[0] (route/graphic ref; empty = inline points)

    // Inline task Location points (used when MapGraphicUuid is empty). Elev is null when
    // the point carries no altitude (the executor ground-clamps to 100 for ground units).
    public List<(double Lat, double Lon, double? Elev)> Points { get; init; } = new();

    // Timing (parsed for completeness; delay/sequencing EXECUTION is deferred to the
    // completion-future slice - the golden-trace order carries all-zero timing).
    public long SimulationStartMs { get; init; }             // StartTime/SimulationTime delay
    public string StartAfterTaskUuid { get; init; } = "";    // ActionTemporalRelationship predecessor
    public long RelativeDelayMs { get; init; }               // ActionTemporalRelationship delay
}

/// <summary>The parsed contents of a C2SIM Order message.</summary>
public class OrderData
{
    public string OrderId { get; set; } = "";
    public List<OrderTask> Tasks { get; set; } = new();
}
