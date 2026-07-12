namespace VrfC2SimApp;

/// <summary>
/// VR-Forces side configuration (bound from the "Vrf" section of appsettings.json).
/// Mirrors the fields of the bridge's StartupConfig plus the C2SIM clientId.
/// </summary>
public class VrfSettings
{
    public string Protocol { get; set; } = "Hla1516e"; // "Hla1516e" | "Dis"
    public int ApplicationNumber { get; set; } = 3201;
    public int SiteId { get; set; } = 1;
    public int SessionId { get; set; } = 1;
    public string HostInetAddr { get; set; } = "127.0.0.1";

    // HLA 1516e
    public string Federation { get; set; } = "";       // --execName (e.g. CWIX-2024)
    public string FedFileName { get; set; } = "";       // full path, optional
    public List<string> FomModules { get; set; } = new();

    // The C2SIM SystemName this interface answers to. MUST equal the pushed
    // init's SystemName or 0 units are created (RUNBOOK sec 2).
    public string ClientId { get; set; } = "STP";

    // Aggregate formation set before a move (PORT.md sec 10 enrichment). "" = OFF (golden
    // parity: bare moveAlongRoute, disaggregated aggregates freeze on their unresolvable
    // default formation). A VALID Title-Case name ("Wedge"/"Column"/"Line"/"Vee"/"Echelon")
    // unblocks aggregate movement; no-op on non-aggregate entities. Opt-in - it deliberately
    // diverges from the frozen golden-trace behavior.
    public string AggregateFormation { get; set; } = "";

    // Semantic-map Unit 4 (docs/SEMANTIC_MAPPING.md): the PROPER aggregate maneuver. "" = OFF.
    // A VALID Title-Case formation name ("Wedge"/"Column"/...) makes an AGGREGATE task use
    // DtMoveIntoFormationTask (move the set into formation AT the destination) INSTEAD of
    // CreateRoute + MoveAlongRoute + SetAggregateFormation - the real fix for the stuck-aggregate
    // finding (most COA-STP1 aggregates stayed stuck with Wedge alone; PORT.md sec 10). Opt-in +
    // aggregate-only; entity moves are unaffected (golden parity). Takes precedence over
    // AggregateFormation for aggregates when set. Moves to the route's final point (intermediate
    // waypoints are dropped - this is the diagnostic "does the set move in formation" path).
    public string MoveIntoFormation { get; set; } = "";

    // Simulation time multiple applied on Run (parity: C++ SetTimeMultiplier from the
    // server sim multiple, C2SIMinterface.cpp:1844). 1 = real-time (golden default);
    // higher runs the VR-Forces clock faster (useful to watch/verify scenarios quickly).
    public int TimeMultiplier { get; set; } = 1;

    // How long a task waits for its startAfterTaskUuid predecessor to complete before
    // giving up and dispatching anyway (the fix for the C++ infinite busy-wait, PORT.md
    // sec 6). The golden aggregate completion took ~9 min, so 600 s is a safe default.
    public int TaskPredecessorTimeoutSeconds { get; set; } = 600;

    // On clean stop, delete every VR-Forces object this run created (via the tracked uuids)
    // so they do NOT accumulate across runs - accumulation degrades create/route reflection
    // and is why a manual VR-Forces scenario reload was needed between runs (RUNBOOK sec 7/8).
    // Default true (self-service hygiene); set false to leave created objects in place.
    public bool CleanupCreatedOnStop { get; set; } = true;
}
