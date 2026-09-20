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

    // R1 (transition): EVERY MapGraphicID the task carries, in order. The schema allows a list
    // (ManeuverWarfareTask/MapGraphicID[], :4080) and a task may name several graphics - an
    // attack position and then an objective, say - so the resolved geometry is their sequence.
    // COA-STP1 carries NONE: STP emits MapGraphicID only when IncludeMapGraphicIdInTasks is set
    // (an STP-side export defect, STP-801), which is why the embedded Location path stays.
    public IReadOnlyList<string> MapGraphicUuids { get; init; } = Array.Empty<string>();

    // Inline task Location points (used when MapGraphicUuid is empty). Elev is null when
    // the point carries no altitude (the executor ground-clamps to 100 for ground units).
    public List<(double Lat, double Lon, double? Elev)> Points { get; init; } = new();

    // Timing. SimulationStartMs/StartAfterTaskUuid/RelativeDelayMs gate the DISPATCH
    // (TaskSequencer.WaitForStartAsync); DurationMs closes the task (R4, below).
    public long SimulationStartMs { get; init; }             // StartTime/SimulationTime/DelayTimeAmount relative delay
    public string StartAfterTaskUuid { get; init; } = "";    // ActionTemporalRelationship predecessor
    public long RelativeDelayMs { get; init; }               // ActionTemporalRelationship delay

    // R4 (user ruling 2026-09-14, "completion is given by the end time"): the task's own
    // Duration (ManeuverWarfareTask/Duration/IsoTimeDuration, schema :4132). The end time is
    // dispatch + Duration; a task still running then is reported TASKCMPLT
    // (TimedCompletionPolicy). 0 = absent or unparseable (the parser warns) - such a task has
    // no timed end and completes only on its own evidence.
    // COA-STP1_Order.xml carries one on all 42 tasks: 32 x P00Y00M00DT01H20M00S (4,800,000 ms)
    // and 10 x P00Y00M00DT02H00M00S (7,200,000 ms).
    public long DurationMs { get; init; }

    // R4: the ABSOLUTE form of StartTime (TimeInstantType/DateTime/IsoDateTime, schema :3863).
    // STP exports the SimulationTime delay form instead, so this is null for every task on
    // disk; it is honoured (converted to a delay against order receipt) so an order from a
    // different producer is not silently dispatched early.
    public DateTime? AbsoluteStartUtc { get; init; }
}

/// <summary>
/// ONE TACTICAL GRAPHIC CARRIED BY THE ORDER ITSELF, at
/// OrderBody/Entity/PhysicalEntity/MapGraphic/TacticalGraphic
/// (C2SIM_SMX_LOX_CWIX2024.xsd:2960-2977 -> :3121-3133 -> :2814-2825 -> :4934-4947).
///
/// WHY THIS EXISTS. The schema is explicit that an order may define its own objects rather than
/// reference ones defined elsewhere - "This message may define tasks, or it may refer to tasks
/// defined elsewhere (such as in an initialization message)" (xsd:2963) - and `MapGraphicID` is a
/// plain UUID-patterned string with NO xs:keyref behind it, so nothing requires the graphic a task
/// names to live in the initialization. The real STP export takes the other branch: 34 of its 35
/// MapGraphicID references name a graphic carried IN THE ORDER and 0 name an init graphic, so an
/// interface that registers only init graphics resolves NOTHING.
///
/// <see cref="Kind"/> uses the <see cref="TaskGraphic"/> Kind constants, so the registration is a
/// straight hand-over to the same map the init fills and the same resolver reads.
/// </summary>
public record OrderGraphic
{
    public string Name { get; init; } = "";
    public string Uuid { get; init; } = "";
    public string Kind { get; init; } = "";   // TaskGraphic.KindArea | KindLine | KindPoint
    /// <summary>The C2SIM element this came from - "Route", "Boundary", "Point", "TacticalArea",
    /// "TaskGraphic" - kept because a mission SYMBOL and a control measure are different things to
    /// a planner even where the geometry is read the same way (DESIGN_V4B sec on task symbols).</summary>
    public string Element { get; init; } = "";
    public List<(double Lat, double Lon, double Elev)> Points { get; init; } = new();
}

/// <summary>The parsed contents of a C2SIM Order message.</summary>
public class OrderData
{
    public string OrderId { get; set; } = "";
    public List<OrderTask> Tasks { get; set; } = new();

    /// <summary>The graphics the ORDER defines for itself, in document order. Parsing them is
    /// unconditional and free; what the service does with them is register them for R1 resolution
    /// BEFORE any task of the order is translated.</summary>
    public List<OrderGraphic> Graphics { get; set; } = new();

    // Non-fatal parse findings the executor should surface (the parser is pure, so it
    // collects rather than logs): e.g. multiple ActionTemporalRelationships (only the
    // first is honored) or a non-STREND association code (the sequencer assumes
    // start-after-predecessor-END).
    public List<string> Warnings { get; } = new();
}
