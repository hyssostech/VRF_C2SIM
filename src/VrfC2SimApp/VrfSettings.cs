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

    // Aggregate formation repair (docs/UNIT_MOVEMENT_RESEARCH.md). "" = OFF (golden
    // parity: bare moveAlongRoute; disaggregated aggregates freeze on their unresolvable
    // default formation "column-left"). "auto" = the QUERY-DRIVEN create-time repair -
    // RECOMMENDED for any aggregate-bearing scenario, R5-verified (3/3 route completions
    // on the golden init): on each aggregate creation the app queries the unit's OWN
    // formation list (RequestAvailableFormations) and, on the reply, sets a valid name
    // from that list (prefer "column"; snap) + ReorganizeAggregate (establish the lead
    // subordinate) BEFORE any tasking. Never trust static formation names: live lists
    // are all lowercase even where the .entity files say Title-Case. A literal name
    // (e.g. "Wedge") is the legacy global set at MOVE time - kept for experiments only.
    // Opt-in - deliberately diverges from the frozen golden-trace behavior.
    public string AggregateFormation { get; set; } = "";

    // R8 create-time de-stacking (docs/UNIT_MOVEMENT_RESEARCH.md sec 4). When ON, init
    // units that share IDENTICAL coordinates (the COA-STP1 blocking data pathology:
    // dozens of units at literally the same lat/lon gridlock disaggregated-unit
    // geometry - dispersed golden 3/3 marched vs stacked COA-STP1 0/6, identical code)
    // are spread onto deterministic hex rings BEFORE CreateEntity/CreateAggregate:
    // first unit keeps its spot, the rest take ring slots (6k slots at k*spacing).
    // OPT-IN: it moves units off their source-data positions (parity-breaking).
    // Pairs naturally with AggregateFormation=auto. See DeStacker.cs.
    public bool DeStackCreates { get; set; } = false;

    // Ring spacing in meters for DeStackCreates (adjacent ring-1 slots sit exactly
    // this far apart). "A few tens of meters" per the R8 plan; tune via env
    // (Vrf__DeStackSpacingMeters) if 50 proves too tight for member footprints.
    public double DeStackSpacingMeters { get; set; } = 50.0;

    // R10 subordinate fan-out (docs/UNIT_MOVEMENT_RESEARCH.md sec 4c). When ON, an
    // AGGREGATE'S along-route move is fanned out to its member ENTITIES (each member
    // gets MoveAlongRoute on the same route; the unit-level TASKCMPLT is synthesized
    // when ALL fanned members complete). The practical unlock for regions where VRF's
    // unit leader-path planning returns EMPTY (the R9 Mojave finding) while entity
    // moves work fine. Members revert to unit control on completion (MAK
    // UnitMembersTaskIndependently). Opt-in; falls back to the normal aggregate move
    // when the unit publishes no members. Applies to the multi-point route path only.
    public bool SubordinateFanOut { get; set; } = false;

    // R10 fan-out robustness (UNIT_MOVEMENT_RESEARCH.md sec 4c). Completion QUORUM: synthesize
    // the unit's TASKCMPLT once this FRACTION of fanned members complete (1.0 = today's
    // behavior: ALL must finish). Guards against one stuck member holding the unit task open
    // (the 3/4-CoHQ gap in the COA-STP1 unblock run). Late stragglers after synthesis are
    // swallowed (the tracker's Synthesized state), not re-reported. Range (0,1]; <=0 or >1
    // clamp to 1.0.
    public double FanOutCompletionFraction { get; set; } = 1.0;

    // R10 fan-out robustness: per-fan-out straggler TIMEOUT in seconds. If the quorum has not
    // been reached this long after the fan-out is registered, synthesize the unit completion
    // anyway WITH A WARNING (a member never completing - e.g. a stuck GndV - no longer hangs
    // the unit task). 0 = OFF (no timeout; rely on quorum/all-complete only). Either trigger
    // fires the synthesis at most once (idempotent).
    public int FanOutStragglerSeconds { get; set; } = 0;

    // R11 probe (experiment-only): an AGGREGATE move creates a waypoint at the route's
    // FINAL point and issues DtPlanAndMoveToTask (the PLANNED pathfinding point move)
    // instead of CreateRoute + MoveAlongRoute - does the planner path where the
    // move-along leader plan is empty? Takes precedence over SubordinateFanOut.
    public bool AggregatePlanAndMove { get; set; } = false;

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

    // How long a task waits for its startAfterTaskUuid predecessor before giving up (the
    // fix for the C++ infinite busy-wait, PORT.md sec 6). P0.2: the completion window is
    // measured from the predecessor's DISPATCH, not order arrival (TaskSequencer). The
    // golden aggregate completion took ~9 min, so 600 s is a safe default. NOTE: past live
    // experiments overrode this to 30 s via env - make experiment configs explicit.
    public int TaskPredecessorTimeoutSeconds { get; set; } = 600;

    // P0.2 (NEXT_SESSION_GUIDANCE.md sec 3, DEFECT B): what to do when a task's predecessor
    // times out or was abandoned.
    //   "skip"     (default) log + do NOT dispatch; the task's own successors then fail fast.
    //   "force"    dispatch anyway (the pre-P0 behavior: retasks a unit whose in-flight task
    //              gets REPLACED mid-route - kept for compatibility/experiments).
    //   "whenIdle" dispatch only if the unit has no in-flight task at that moment.
    // Golden orders carry no temporal deps, so this never fires there (parity-neutral).
    public string PredecessorTimeoutPolicy { get; set; } = "skip";

    // P0.3: an ATTACK/BREACH engage is issued when its approach move COMPLETES (previously
    // it was issued in the same tick as the move, which - VRF running one task at a time -
    // would REPLACE the move the moment both are real). If the move never completes, issue
    // the engage anyway after this many seconds (0 = never: engage strictly on completion).
    public int EngageFallbackSeconds { get; set; } = 300;

    // On clean stop, delete every VR-Forces object this run created (via the tracked uuids)
    // so they do NOT accumulate across runs - accumulation degrades create/route reflection
    // and is why a manual VR-Forces scenario reload was needed between runs (RUNBOOK sec 7/8).
    // Default true (self-service hygiene); set false to leave created objects in place.
    public bool CleanupCreatedOnStop { get; set; } = true;

    // P4b position-report bundling (C++ parity, textIf.cxx:435-530). OFF = one PositionReport
    // per POSITION line (today's behavior). ON = accumulate POSITION reports into one envelope
    // (N ReportContent) and flush on count/size/timer. TASKCMPLT is never bundled. Opt-in.
    public bool BundlePositionReports { get; set; } = false;
    public int BundleMaxReports { get; set; } = 10;      // C++ maxReportsPerBundleTextIf
    public int BundleMaxBytes { get; set; } = 10240;     // C++ maxBundleSizeTextIf
    public int BundleFlushMs { get; set; } = 2000;       // C++ ~2 s reminder-thread flush

    // Mojave root-cause probe/fix (docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md).
    // Ground-unit route waypoints are handed to VRF at a FIXED 100 m MSL ("Fixed100" = the golden-
    // parity default; a sea-level assumption that works where terrain < 100 m, e.g. Sweden). At a
    // high-elevation region (Mojave terrain ~1100 m) a 100 m waypoint sits ~1000 m UNDERGROUND, so
    // the aggregate member offset-route GROUND CLAMP (which entity move-along tolerates but the
    // disaggregated move-along controller does not - Thread A: closestIntersection/dataAvailable)
    // yields EMPTY offset routes and the unit freezes. "Live" instead puts each ground waypoint at
    // the unit's OWN live ground altitude (read from the sim) + LiveClearanceMeters, so waypoints
    // sit just ABOVE local terrain at ANY region. Opt-in; "Fixed100" is byte-for-byte today's path.
    public string GroundWaypointAltitudeMode { get; set; } = "Fixed100"; // "Fixed100" | "Live"
    public double GroundWaypointLiveClearanceMeters { get; set; } = 50.0;
}
