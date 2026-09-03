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
    // 5.2 bridge only (Release-5.2* builds): the VR-Link connection config file loaded before the
    // command line (docs/VRF_5.2_MIGRATION_DIFF.md Y-2: shipped MAK-ONE-2025-Config.xml; overrides
    // go in a SECOND file, FomModules above stays EMPTY because modules are additive). "" = let the
    // bridge resolve the shipped file from the VR-Forces settings tree. Ignored by the 5.0.2 bridge.
    public string ConnectionConfigFile { get; set; } = "";

    // The C2SIM SystemName this interface answers to. MUST equal the pushed
    // init's SystemName or 0 units are created (RUNBOOK sec 2).
    public string ClientId { get; set; } = "STP";

    // R9 TYPE-MAPPING fix (docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md; Cell C proof
    // in docs/experiments/PREREG_PLAN_ASSIGNMENT_SPIKE.md Outcome record + docs/VRF_GROUND_TRUTH.md
    // 0.1.7). The echelon-only dispatch (UnitTranslator) emitted, for an ArmorPlatoon-class unit
    // (SIDC echelon char 'D'), the DIS objectType 11.1.225.1.1.3.0 - which has NO Kind-11 aggregate
    // leaf and falls back to the generic Ground_Aggregate template. Ground_Aggregate's 4 anonymous
    // Cat-4 members have EMPTY function handles and no vehicle-platoon script, so a disaggregated
    // move-along hands them EMPTY offset routes ("moveAlong() - empty route -- not sending move
    // along to subordinate") and the unit freezes (R9 1222.MechPlt, run 20260719T144109Z). Cell C
    // proved the REAL Tank Platoon (USA) template (objectType 3:11:1:225:3:2:0:0, matchType
    // 3:11:1:225:3:2:-1:-1) - createSubordinates=true -> 4 M1A2 members with named handles
    // (PL/PSG/PLWM/PSGWM) + vehiclePlatoonScriptEnable - MOVES end to end on the same remote-create
    // + bare MoveAlongRoute path (reflected 8->13 member offset-route transients, settled ~1165 m E,
    // POS==RPT). "RealTemplates" (THE DEFAULT) makes ArmorPlatoon emit Tank Platoon (USA);
    // "GoldenParity" keeps the byte-for-byte golden-trace objectType (the escape hatch, exactly like
    // GroundWaypointAltitudeMode="Fixed100"). Scope today: ArmorPlatoon ONLY - the one remapping
    // whose target template is verified installed AND proven to move. ArmorCompany already resolves
    // to the real Tank Company (USA); ArmorCoHQ's correct target is a pending USER decision
    // (docs/TYPE_GAP_ADJUDICATION.md Decision-4) and is deliberately NOT changed here.
    // FIDELITY PASS 2026-09-02 (docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md sec 7.1/7.2). A THIRD
    // value, "FidelityTable", replaces the echelon-letter if-chain for GROUND units with a lookup in
    // TypeMapFile keyed on (functionId, echelon, nationRole) - the artifact the user reviews line by
    // line. "GoldenParity" and "RealTemplates" are UNCHANGED, so the new mapping is A/B-able against
    // the R9 evidence without a rebuild; RealTemplates stays the default until the live gate
    // (docs/NEXT_TYPE_MAPPING_LIVE_GATE.md) passes.
    public string TypeMappingMode { get; set; } = "RealTemplates"; // "GoldenParity" | "RealTemplates" | "FidelityTable"

    // R-HOSTILE-NATION (user ruling 2026-09-02): the hostile force nation is a CONFIGURATION option,
    // not a fixed choice - European customers pick RUS, INDOPACOM picks PRC. Friendly stays USA
    // unless the init says otherwise. Both are names in TypeMapFile's "nations" block (USA=DIS 225,
    // RUS=222, PRC=45). A per-unit SISOEntityType/DISCountry in the init OVERRIDES these with a log
    // line (sec 7.3 channel 1) - the C2SIM standard's own per-unit nationality channel.
    // JC-2 (supervisor PROVISIONAL, 2026-09-02): the default is RUS because it is the only value
    // with installed content on 5.0.2; PRC REFUSES TO START until the authored PRC SMS exists,
    // because every PRC aggregate request today lands a zero-subordinate Country-0 abstract
    // (an EMPTY unit) - a loud failure beats that silent one. Ignored unless TypeMappingMode
    // is FidelityTable.
    public string FriendlyNation { get; set; } = "USA";   // DIS country 225
    public string OpposingNation { get; set; } = "RUS";   // "RUS" (222) | "PRC" (45)

    // The fidelity mapping table. Resolved against the working directory, then the app directory,
    // then by walking UP from the app directory (the app runs both from the repo root and from
    // its bin folder). Only read when TypeMappingMode=FidelityTable.
    public string TypeMapFile { get; set; } = "data/unit-type-map.json";

    // R-SURFACE-PROXY (user ruling 2026-07-17): proxy substitutions must be visible to downstream
    // C2SIM consumers, never silently swallowed. When ON, every non-EXACT row (a) appends a compact
    // tag to the created object's MARKING/name and (b) emits one ObservationReport/NameObservation
    // naming the substituted template and why, at creation time.
    // MARKING CAP: the back end resolves marking-text references through a 35-byte blob
    // (C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h:412), so a name over 34 characters is CUT and
    // stops resolving - the 2026-09-02 route-uuid finding. The tag is therefore appended only when
    // the result still fits; when it does not, the name is left alone and the substitution is still
    // reported and logged (never dropped).
    public bool SurfaceProxySubstitutions { get; set; } = true;
    public string ProxyMarkingTag { get; set; } = "~PXY";   // <= 34 chars total with the unit name

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

    // Mojave root-cause probe/fix (docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md;
    // create-time terrain-clamp fix in docs/SUPERVISED_RECOVERY_PLAN.md sec 3b). Governs the
    // altitude of BOTH ground-unit route waypoints AND (as of the create-time fix) the CREATE
    // position of ground units. Under "Fixed100" route waypoints are handed to VRF at a FIXED
    // 100 m MSL (a sea-level assumption that works where terrain < 100 m, e.g. Sweden) and ground
    // units are created at their plan altitude (ElevationAgl MSL) with the deferred SetAltitude -
    // byte-for-byte today's path (the golden-parity escape hatch). At a high-elevation region
    // (Mojave terrain ~1100 m) a 100 m waypoint sits ~1000 m UNDERGROUND, so the aggregate member
    // offset-route GROUND CLAMP (which entity move-along tolerates but the disaggregated move-along
    // controller does not - Thread A: closestIntersection/dataAvailable) yields EMPTY offset routes
    // and the unit freezes; and a ground unit BORN below terrain never executes movement at all
    // (parts 13/13c). "Live" instead puts each ground waypoint at the unit's OWN live ground
    // altitude (read from the sim) + LiveClearanceMeters, and creates ground units at
    // CreateAltitudeSafeMslMeters so VRF's create ground clamp drops them onto the surface.
    // "TerrainProfile" (docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md) creates like Live,
    // then authors each GROUND route vertex from the back end's OWN terrain height
    // (DtIfRequestTerrainProfileInformation) + TerrainClearanceMeters; a vertex the back end does
    // not answer for (or a reply that never arrives within TerrainProfileTimeoutSeconds) keeps its
    // Live altitude with a WARN - the order is never blocked on the query.
    // "TerrainProfile" IS THE DEFAULT since 2026-09-02 (design sec 7, Rows 2c and 2cR: two
    // consecutive live runs authored all 3 vertices of all 3 routes from the back end's own
    // terrain, zero warnings, movement at Row 1 timings). It is the DOCUMENTED frame - the
    // Users Guide makes vertex altitude the author's responsibility (contract C5) and the
    // simulator's own terrain height is the authoritative answer. "Live" (live entity altitude +
    // GroundWaypointLiveClearanceMeters) was the previous default and remains available by config
    // as the fallback when the terrain query is not wanted; "Fixed100" is the byte-for-byte
    // golden-parity escape hatch. Override either way with Vrf__GroundWaypointAltitudeMode.
    public string GroundWaypointAltitudeMode { get; set; } = "TerrainProfile"; // "Fixed100" | "Live" | "TerrainProfile"
    public double GroundWaypointLiveClearanceMeters { get; set; } = 50.0;
    public double TerrainClearanceMeters { get; set; } = 10.0;
    public int TerrainProfileTimeoutSeconds { get; set; } = 10;

    // Live-like ground-unit CREATE altitude in meters MSL (create-time terrain-clamp fix,
    // docs/SUPERVISED_RECOVERY_PLAN.md sec 3b). Under GroundWaypointAltitudeMode="TerrainProfile"
    // (the default) or "Live" - the two modes share the create path - a ground
    // unit is created at THIS altitude instead of its plan altitude (ElevationAgl MSL). It must be
    // guaranteed ABOVE all Earth terrain (highest ground ~8849 m at Everest) so that VRF's
    // createEntity ground clamp (default on) can only DROP the birth onto the local surface - a
    // clamp cannot RAISE a below-terrain birth, which is why fixed-MSL births bury units at high
    // elevation. Default 10000 m clears every land surface with margin. Ignored under "Fixed100"
    // and for non-ground (air/sea) units, which keep parity behavior.
    public double CreateAltitudeSafeMslMeters { get; set; } = 10000.0;
}
