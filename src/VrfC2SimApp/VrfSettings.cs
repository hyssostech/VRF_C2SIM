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

    // The network card the UDP / best-effort leg binds - the 5.0.2 Launcher's "Network
    // Interface Address" (RESEARCH_52_HLA_CONNECTION_CONFIG_2026-09-04 P3). On the 5.2
    // bridge VrfFacade::Start pushes it as --deviceAddress on the HLA argv too, not only on
    // DIS (UG52 Table 11 p180-181); on 5.0.2 it reaches only the DIS argv, exactly as before.
    // The default equals the bridge's own StartupConfig default AND the value the 5.0.2
    // configuration fixed, so binding this key changes nothing until a caller sets it - what
    // it buys is that the address a run used is CONFIGURABLE and RECORDED instead of resting
    // on a C++ constructor default. WHETHER 5.2 REQUIRES IT IS UNTESTED: PREREG_52_RTIEXEC_
    // 2026-09-04 changed it together with the RTI connection mode and never separated them.
    // An explicit EMPTY value suppresses --deviceAddress (the unpinned arm of that test).
    public string DeviceAddress { get; set; } = "127.0.0.1";

    // HLA 1516e
    public string Federation { get; set; } = "";       // --execName (e.g. CWIX-2024)
    public string FedFileName { get; set; } = "";       // full path, optional
    public List<string> FomModules { get; set; } = new();
    // 5.2 bridge only (Release-5.2* builds): the VR-Link connection config file loaded before the
    // command line (docs/VRF_5.2_MIGRATION_DIFF.md Y-2: shipped MAK-ONE-2025-Config.xml; overrides
    // go in a SECOND file, FomModules above stays EMPTY because modules are additive). "" = let the
    // bridge resolve the shipped file from the VR-Forces settings tree. Ignored by the 5.0.2 bridge.
    public string ConnectionConfigFile { get; set; } = "";

    // 5.2 CONFIG-FILE JOIN (2026-09-03; DIFF rows A2/A9, the same rule tools/Shared/StackIdentity.cs
    // applies to the bridge tools). When true, BuildStartupConfig submits NO federation identity:
    // Federation and FedFileName empty and the FomModules list CLEARED, so MAK-ONE-2025-Config.xml
    // is the only source of execName/FOM (config modules are ADDITIVE - submitting the 5.0.2 list
    // would join with VRFExt-6 AND VRFExt-12, or fail on files that do not exist on 5.2d).
    // WHY A SWITCH AND NOT A SECOND SETTINGS FILE: a later configuration provider can override a
    // key but cannot REMOVE one, so no appsettings overlay and no Vrf__FomModules__N environment
    // variable can empty the three-element list appsettings.json declares. The runner sets
    // Vrf__ConfigFileIdentity=true from -VrfProfile 5.2; the default false leaves the 5.0.2 path
    // byte-for-byte unchanged. Ignored by the 5.0.2 bridge build in the sense that nothing sets it.
    public bool ConfigFileIdentity { get; set; } = false;

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

    // COMPOSE-FROM-CHILDREN hierarchy-aware create (docs/experiments/PREREG_COMPOSE_A_2026-09-05).
    // ROOT-CAUSE fix for the broken company move: today the app passes createSubordinates=true for
    // EVERY unit, so a company (a unit that has declared C2SIM children) instantiates a generic
    // ~48-vehicle "Tank Company (USA)" TEMPLATE and we ALSO create its declared child platoons as
    // orphan aggregates - tasking the company moves the phantom template while the declared platoons
    // sit idle (PREREG_F_DIVERGE ROOT CAUSE, verified). When ON, a PARENT unit (whose uuid is another
    // survivor's Superior) is created as an EMPTY shell (createSubordinates=false) and its declared
    // child units are attached via AddToOrganization once created - the MAK vendor-sample recipe
    // (commandLineRemoteController.cxx:717-775 build, :1520-1554 attach). Leaves/entities are
    // unchanged. DEFAULT ON since 2026-09-06 (N1, DESIGN_ORBAT_TO_VRF C2): compose is the design -
    // 3/3 deterministic (PREREG_COMPOSE_A) and the template higher-unit path is closed by C1b/C11
    // (its HQ-section HMMWV never reaches its formation slot, so the unit never moves). OFF =
    // explicit legacy (Vrf__ComposeHierarchy=false): hierarchy-blind template create, kept only as
    // the regression control (N3) - never for real runs.
    public bool ComposeHierarchy { get; set; } = true;

    // ---- V3: create the init's LINE and POINT tactical graphics ------------------------------
    // The C2SIM init carries 409 TacticalGraphic elements; the interface has only ever created the
    // 35 TacticalAreas (with the C2SIM uuid AS the VRF uuid, VrfC2SimService -> VrfFacade
    // CreateControlArea). COA-STP1 also ships 41 Lines and 317 Points, and every vendor tactical
    // task that is not a bare move takes exactly such an object as a parameter - a line of
    // departure, a limit of advance, a breach lane, a control point
    // (docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md sec 3.2/3.3, build item V3).
    // Parsing them is unconditional and free (InitParser); CREATING them is behind these two.
    //
    // BOTH DEFAULT FALSE, and the reason is the absence of evidence, not a preference:
    //  1. NOT SHOWN TO BE CHEAP. The only vendor sample for these calls,
    //     examples/remoteControl/commandLineRemoteController.cxx:1563-1636, creates ONE waypoint or
    //     ONE route per typed command. Nothing in the sample or the help measures a bulk create.
    //     Turning both on adds 348 creates to an init that today issues 163 (128 units + 35 areas)
    //     - a 3x increase in creation traffic that has never been timed on this stack.
    //  2. NOT SHOWN TO BE IDEMPOTENT. createWaypoint/createRoute document the opposite of an
    //     idempotent create: "the name must be unique (if specified)" (vrfRemoteController.h:987,
    //     :1019). Nothing states what a second create with the same name or startingUUID does. The
    //     interface already sees duplicate init deliveries (the _createdAreaKeys guard exists for
    //     exactly that), so "re-create is harmless" would be an assumption, not a finding.
    //  3. THEY MULTIPLY THE CONSOLE CHANNEL. OnVrfObjectCreated raises EVERY created object's
    //     console to Vrf:ObjectConsoleNotifyLevel, graphics included. At level 4 - the level the
    //     movement investigations run at - 348 more objects is 348 more open consoles.
    // With both false the init issues exactly the commands it issued before V3, in the same order,
    // so no existing fixture or trace comparison moves. Flip either to true for the live gate that
    // measures 1, 2 and 3; see the V3 notes in VrfC2SimService.DispatchInit.
    public bool CreateInitLines { get; set; } = false;
    public bool CreateInitPoints { get; set; } = false;

    // CREATION POLICY (C13, user ruling 2026-09-06): the C2SIM init carries the WHOLE ORBAT (corps-
    // level context); only the units the ORDERS reference are the COA proper. COA-STP1 = 128 units
    // in the init, 11 referenced by its 42 tasks.
    //   "AtInit"  = every matched unit is created IN FULL at initialization (the C++ oracle's
    //               behaviour; COA-STP1 -> 1,333-1,732 objects, the self-inflicted crawl recorded in
    //               PREREG_COASTP1_52_RUN1 sec 12). Kept as the regression control.
    //   "AtOrder" = every matched aggregate is created at initialization as an EMPTY organizational
    //               shell at its authored position (it displays and reflects its position - a member-
    //               less shell did so for a whole run, 174427Z), shells attach to their declared
    //               superiors (the organization tree exists), and a unit's MEMBERS are created and
    //               attached (the same compose / expand / template recipes as AtInit) only when an
    //               order first references it as PerformingEntity or AffectedEntity. Units no order
    //               touches stay shells for the run. Platforms (single-entity units) are created in
    //               full either way. Needs ComposeHierarchy. PREREG_ORDER_TIME_MATERIALIZATION.
    public string CreationPolicy { get; set; } = "AtInit";
    public bool MaterializeAtOrder =>
        string.Equals((CreationPolicy ?? "").Trim(), "AtOrder", System.StringComparison.OrdinalIgnoreCase);

    // VR-Forces install root whose data/simulationModelSets the live expand-to-compose path reads to
    // learn a coarse leaf's DOCTRINAL sub-units (ObjectTypeResolver, .entity <subordinate> lists).
    // "" -> fall back to the MAK_VRFDIR env (set by the 5.2 runner) then ObjectTypeResolver's default.
    // Only consulted when ComposeHierarchy is ON and a coarse leaf (company+) needs expanding.
    public string VrfHome { get; set; } = "";

    // MAK RUNTIME BOOTSTRAP (MakRuntime.cs, 2026-09-07): with these set, the executable prepares its
    // own process (PATH, MAK_VRLDIR/MAK_RTIDIR, RTI_RID_FILE, RTI_ASSISTANT_DISABLE) and no start
    // script is needed. Empty = the process environment must already be right (harness runs).
    public string VrLinkHome { get; set; } = "";
    public string RtiHome { get; set; } = "";
    public string RidFile { get; set; } = "";              // relative to the app directory when not rooted
    public bool RtiAssistantDisable { get; set; } = false;

    // Seconds to wait for all of a composed parent's declared children to be created (ObjectCreated)
    // before the parent shell is created from the children that DID arrive (a warning is logged for
    // any missing child). Mirrors TerrainProfileTimeoutSeconds; guards against a child whose create
    // never round-trips hanging the parent (and its tasks) forever.
    public double CompositionTimeoutSeconds { get; set; } = 15.0;

    // ARRIVAL-EVIDENCE COMPLETION (user ruling 2026-09-07; ArrivalPolicy.cs). A move task is
    // reported complete (TASKCMPLT) when MORE THAN ArrivalMemberFraction of the unit's members (or
    // the entity itself) are within ArrivalRadiusMeters of the task's last vertex, checked every
    // ArrivalCheckSeconds on the tick thread, no earlier than ArrivalMinSecondsSinceDispatch after
    // dispatch (so a genuine vendor completion for a short move gets there first). The vendor's own
    // completion, when it does arrive, is then swallowed for that task. Why: on 5.2 a unit's Move
    // Along Route holds its completion until EVERY member reports arrival, and one straggling
    // member (an M3 7 km back, an M577 1.3 km back) held units at the ends of their legs for
    // eight sim-hours (PREREG_ASSEMBLY_LAYOUT 2026-09-07). Radius 500 m = the shipped Armor-Co
    // formations' half-length (members sit up to +/-430 m from the last vertex).
    public bool ArrivalCompletion { get; set; } = true;
    public double ArrivalRadiusMeters { get; set; } = 500.0;
    public double ArrivalMemberFraction { get; set; } = 0.5;   // 1.0 = every member must be within the radius
    public double ArrivalCheckSeconds { get; set; } = 5.0;
    public double ArrivalMinSecondsSinceDispatch { get; set; } = 30.0;

    // PROGRESS WATCHDOG (C16, report-only; StallPolicy.cs). VR-Forces 5.2 NEVER reports a unit
    // that stops making progress while its move task runs: the base give-up test "always returns
    // false" (vrfobjcore/singleTaskControllerComponent.h:192-205) and ground-vehicle-move-to.lua
    // has no progress test - docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md sec 6a. So the
    // interface detects it itself: when NO member of a moving unit has covered StallMoveMeters of
    // NET displacement over the last StallWindowSeconds, ONE C2SIM TaskStatus with TASKABRT is
    // reported for that task and nothing else happens - no re-task, no VRF command, no state
    // change (the task stays in flight; the vendor's own completion, if it ever comes, still
    // flows normally). DEFAULT OFF: the deployed behaviour is unchanged until a preregistered
    // run turns it on.
    //
    // WHICH CLOCK THE WINDOW RUNS ON (StallClock). "wall" is the DEFAULT because it is the mode
    // this interface has actually MEASURED LIVE so far: BOTH windows are calibrated (240 wall s,
    // 360 sim s, from the same three replayed traces - see CALIBRATION below), but only the wall
    // clock has been exercised in a run. A build that selects "sim" says so at startup.
    // "sim" measures the window on the back
    // end's own scenario time, read through VrfBridge.SimTimeSeconds() ->
    // VrfFacade::SimTimeSeconds() -> DtVrfRemoteController::simTime()
    // (vrfcontrol/vrfRemoteController.h:356 on 5.2d, :352 on 5.0.2 - the clock the vendor's
    // remote-control sample prints as "Sim time from sim engine status",
    // examples/remoteControl/commandLineRemoteController.cxx:1247-1252). Two consequences:
    // a PAUSED scenario can no longer trip the watchdog (its clock stops while wall time runs),
    // and under fixed-frame-run-to-complete the abort lands after StallWindowSeconds of SIM
    // seconds instead of ratio x StallWindowSeconds (at the 6.21x measured on the 2026-09-13 G5
    // run the wall-clock window's 240 s were ~1,490 sim s). StallClock = "wall" is the
    // pre-2026-09-13 behaviour; the watchdog ALSO falls back to wall seconds by itself whenever
    // the reader answers -1.0 (no controller, no back end yet), logging one line when it does,
    // so it is never left without a clock, and it needs THREE consecutive readings of a new
    // mode before it switches (a reader flapping at the check cadence would otherwise clear
    // every window on every tick). The VALUE is validated: anything that is not exactly "sim"
    // or "wall" (trimmed, case-insensitive) logs one line and runs on WALL, so a typo can never
    // pick a mode nobody chose. The check CADENCE (StallCheckSeconds) is a wall-time sampling
    // rate, but in "sim" mode it is also the window's RESOLUTION, so it FOLLOWS the clock: the
    // watchdog samples often enough (down to a 1 s floor) that one step advances the sim clock
    // by at most StallWindowSeconds / StallPolicy.MinRingDepth, and it never judges on fewer
    // than MinRingDepth samples. A cadence COARSER than StallWindowSeconds / (MinRingDepth - 1)
    // could never fill that ring at all, so StallCheckSeconds is CLAMPED to that ceiling - 80 s
    // on the wall window, 120 s on the sim window - with one line at startup, rather than left to
    // go dormant in silence (pass-2 review F4a). StallMinSecondsSinceDispatch is a WALL floor and
    // is applied on the WALL clock ONLY: 360 sim s is ~58 wall s at 6.21x, so ANDing 60 wall s
    // onto the sim clock would let the floor, not the calibrated window, set the detection time
    // (measured at 60x: wall 60 s / sim 3,600 s - pass-2 review F8). A sim clock that STOPS
    // ADVANCING for 60 wall seconds while a move task is in flight - a paused scenario, or a back
    // end that stopped answering and was deactivated rather than removed - warns once and suspends
    // judging until it moves again; a clock that steps BACKWARDS is a rollback to a snapshot, not
    // a stopped clock, and gets its own line without suspending anything (pass-2 review F3).
    //
    // *** CALIBRATION - THE WINDOW BELONGS TO THE CLOCK ***
    // StallWindowSeconds = 0, the shipped default, means "the window calibrated for whichever
    // clock is in use"; any positive value is used exactly as configured. Both numbers come from
    // the SAME three replayed traces - G5 20260913T185936Z, G3 20260913T174516Z, P11
    // 20260907T150643Z - and they are NOT a conversion of one another: P11's sim/wall ratio swings
    // 1.10x-1.99x WITHIN that one run, so 240 wall s covers 264-478 sim s depending on the load.
    //   WALL, 240 s (tools/analysis/stall_replay.py; numbers in C16 of
    //     docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md). NOT the 120 s first tried: at 120 s the rule
    //     fires on units that are CRAWLING rather than stopped (P11's 4-27, 40, 856/HHC and
    //     C/1-35 creep at 0.4-0.5 m/s for thousands of seconds and each covered 200-1,200 m AFTER
    //     the 120 s rule would have aborted them). The last false alarm disappears between a 160 s
    //     and a 170 s window, so 240 s keeps a 1.41x margin, and the clean threshold band there is
    //     35-70 m with 50 m mid-band. A larger THRESHOLD cannot do this job in place of a longer
    //     window - at 120 s the crawlers' per-window minima (41-50 m) and the frozen units'
    //     (22-49 m) overlap; only persistence separates them.
    //   SIM, 360 s (docs/experiments/RECAL_STALL_SIMSECONDS_2026-09-13.md). The re-calibration
    //     re-stamped the wall-stamped POS rows onto the sim axis by piecewise-linear interpolation
    //     over the (wall, sim) pairs the object console's own line prefixes carry - not one
    //     least-squares slope, which hides the load variation - and REPRODUCED EVERY DOCUMENTED
    //     WALL NUMBER first (the 120 s quartet and the 160 s triple to the second, the 160/170
    //     boundary, the 240 s fires, the 74/78 m true-negative minima, the 35-70 m band). The
    //     sweep over 100-500 sim s then put the pooled false-alarm boundary at 50 m at 250 sim s
    //     (P11 250, G3 200, G5 none), and 250 x 1.41 = 353 -> 360. At 360 sim s the clean
    //     threshold band is 35-75 m and all five true positives fire EARLIER in wall time than the
    //     240 wall s window does (135 s against 385 s in G5), because the freezes happen while the
    //     sim runs fastest.
    // TWO LIVE UNKNOWNS, both settled by one instrumented run: whether
    // DtVrfRemoteController::simTime() reports the same clock the object console prints as its own
    // sim prefix - that IS the clock the 360 was calibrated on - and whether DtBackend::simTime()
    // EXTRAPOLATES between back-end status messages (its member layout,
    // mySimTimeToRealTimeRatio / myLastSimTimeUpdated at vrfutil/backend.h:410-419, suggests it
    // may, which would make a paused reading a sawtooth rather than a flat line).
    // REINTERPRETATION (pass-2 review F9): StallWindowSeconds 0 - and any NEGATIVE value - now
    // mean "the clock's calibrated default". Before this branch the window was
    // Math.Max(1, StallWindowSeconds), so 0 meant a ONE-SECOND window. StallDetection ships OFF
    // and no deployed appsettings sets either, so the blast radius is nil, but a config file
    // carrying an explicit 0 behaves completely differently here than it did.
    public bool StallDetection { get; set; } = false;
    // NOTE (M2 of the cold-start review of 5c67d41): StallClock governs THE PROGRESS WATCHDOG AND
    // NOTHING ELSE. It used to pick the clock the R4 timed completion served its Durations on too,
    // so an operator turning it on for C16 silently changed when every task in the order completed.
    // R4 has its own knob now - Vrf:TaskClock, below. The two READ THE SAME SIM CLOCK through the
    // same shared sample and the same hysteresis, so they can never disagree about whether the
    // scenario is running; they may still be configured to different preferences.
    public string StallClock { get; set; } = "wall";            // "wall" = measured live (default) | "sim" = scenario clock
    public int StallWindowSeconds { get; set; } = 0;            // 0 or negative = the clock's calibrated window (240 wall / 360 sim); else as given
    public double StallMoveMeters { get; set; } = 50.0;         // net displacement per member over the window
    public int StallMinSecondsSinceDispatch { get; set; } = 60; // grace after dispatch before the watchdog may fire
    public int StallCheckSeconds { get; set; } = 5;             // how often the tick thread samples
    public int StallMinMembersWithData { get; set; } = 1;       // readable members needed before a stall may be called

    // OBSERVATION CHANNEL (UG52 21.9 p483): every VR-Forces object has its own console that
    // carries "messages sent from the simulation engine, from a simulation object's plan, from
    // other simulation objects, and from scripts", filtered by a PER-OBJECT notify level
    // (0 fatal, 1 warn, 2 diag, 3 verbose, 4 debug). The installed vrfSim.mtl leaves objects at
    // objectConsoleNotifyLevel 1 (warnings only), so nothing a unit's movement/formation
    // controllers say ever reached us. >= 0: after each of OUR objects is created (ObjectCreated)
    // its level is set to this value (vrfRemoteController.h:1953 setObjectNotifyLevel) and every
    // console message is logged as "VRF console". -1 (default): leave the vendor default alone.
    public int ObjectConsoleNotifyLevel { get; set; } = -1;

    // The same, for the MEMBERS of a tasked aggregate (sim-created entities we never saw in
    // ObjectCreated). -1 (default) = leave them at the vendor default; a unit's own console at 4 is
    // ~60 rows per task (its subtask gate, "task complete msg rcvd from <sub>", offset routes), while
    // every member at 3-4 is tens of thousands of rows per unit (PREREG_CONSOLE_CHANNEL 6.2) - at scale
    // that is the difference between a usable trace and gigabytes. Set >= 0 only for a per-member
    // diagnostic on a small init.
    public int ObjectConsoleMemberNotifyLevel { get; set; } = -1;

    // R1 (REBASELINE_52_INSTRUMENTS sec 6 item 4): PERIODIC C2SIM POSITION REPORTS, the C++ oracle's
    // own mechanism (C2SIMinterface.cpp:388-460: every reportInterval, for every unit we simulate,
    // getUnitGeodeticFromSim -> position report, bundled). The text-report POSITION path
    // (OnVrfTextReport) needs the 5.0.2 fixture's Lua tracking script, which the 5.2 fixtures do not
    // carry, so on 5.2 the C2SIM bus saw no positions at all. 0 (default) = off, exactly the oracle's
    // "runs only if reportInterval > 0"; the 5.0.2 scale runs are equivalent to 10 s.
    public int PositionReportSeconds { get; set; } = 0;
    // Which sides are reported: "both" (the oracle's sendTrackingCode 1, what the 2026-09-02 scale
    // runs used - 128 units reported), "blue" (code 0, blue only), "red" (code 2).
    public string PositionReportSides { get; set; } = "both";

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

    // Rotation of the hex-ring layout about the anchor, degrees clockwise from north (default 0).
    // Same neighbours and spacing, different ground under every displaced unit - the terrain
    // test lever (user 2026-09-07: "Rotate the unit placements to verify your terrain theory";
    // PREREG_ASSEMBLY_LAYOUT 3e). The anchor (first unit of a stack) never moves.
    public double DeStackRotationDeg { get; set; } = 0.0;

    // ORIGIN VERTEX DROP (2026-09-07, PREREG_ASSEMBLY_LAYOUT 3f): STP writes a unit's own position
    // as the FIRST vertex of its route ("from here"). Once the unit has been spread away from that
    // coordinate (DeStackCreates), a route that still starts there drags every unit back to the one
    // point and the start-of-run pile re-forms at vertex 1 (three spaced runs: the early-stalled
    // members all stopped at 0.0-0.2 km from the STP point). A task's LEADING points that lie within
    // this distance of the unit's AUTHORED init position are dropped when the unit's live position is
    // farther than this from it; the unit's live position is point 0 of the route regardless
    // (moveAlongTasks.h: "move to the first vertex, then move successively"). 0 disables.
    public double DropOriginVertexMeters { get; set; } = 100.0;

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

    // M1 (cold-start review of 5c67d41): the gate above is now a FLOOR, not the whole window. A
    // task whose predecessor carries a C2SIM Duration waits at least
    // (that Duration x Vrf:DurationScale) + this margin, because R4 makes the predecessor's
    // completion its END TIME and a gate shorter than the end time it waits for skips the
    // successor by construction (COA-STP1: 31 of 42 tasks, at every shipped setting).
    // The margin covers the 1 s timed-walk cadence and the dispatch-then-arm ordering inside
    // MarkDispatched; 60 s is two orders of magnitude of slack on a 4,800 s hold and costs
    // nothing but 60 s of patience when a predecessor genuinely never completes.
    public int TaskPredecessorEndMarginSeconds { get; set; } = 60;

    // A1 (cold-start review of 0c96f50): THE ABSOLUTE BACKSTOP ON A CHAINED GATE'S PHASE 1.
    // Phase 1 asks "has the predecessor DISPATCHED at all", and for a predecessor that is a task
    // in the same order the honest answer is "wait - something will dispatch it, complete it or
    // ABANDON it", because every dispatch dead end calls TaskSequencer.NotifyAbandoned and a
    // successor therefore fails fast on any real one. Measuring that wait with
    // Vrf:TaskPredecessorTimeoutSeconds instead skipped 21 of COA-STP1's 42 tasks at every
    // shipped setting (the window covered the predecessor's Duration but not its LEAD TIME).
    // This is the only bound left on it: one day, longer than any authored chain
    // (COA-STP1's deepest lead is 26,400 s) and short enough that a wedged interface does not
    // hold a gate for the life of the process. A DANGLING startAfterTaskUuid - a predecessor no
    // task in the order carries - is NOT covered by it and still expires at
    // Vrf:TaskPredecessorTimeoutSeconds, because nothing will ever abandon a task that does not
    // exist. 0 or negative falls back to the 86400 default rather than skipping every chain.
    public int TaskChainBackstopSeconds { get; set; } = 86400;

    // P0.2 (NEXT_SESSION_GUIDANCE.md sec 3, DEFECT B): what to do when a task's predecessor
    // times out or was abandoned.
    //   "skip"     (default) log + do NOT dispatch; the task's own successors then fail fast.
    //   "force"    dispatch anyway (the pre-P0 behavior: retasks a unit whose in-flight task
    //              gets REPLACED mid-route - kept for compatibility/experiments).
    //   "whenIdle" dispatch only if the unit has no in-flight task at that moment.
    // Golden orders carry no temporal deps, so this never fires there (parity-neutral).
    public string PredecessorTimeoutPolicy { get; set; } = "skip";

    // R4 (user ruling 2026-09-14): "completion is given by the end time". A dispatched task whose
    // C2SIM Duration has elapsed is reported TASKCMPLT, once, through the single emit point, and
    // its STREND successors dispatch (TimedCompletionPolicy). ON by default: without it the
    // hold-type half of a real order - SECURE/OCCUPY/DEFEND/RETAIN/BLOCK/FIX/SCREEN/GUARD, fires
    // and air defence - has no completion at all and every successor chain dies at the
    // predecessor timeout (run G6: 9 of 42 tasks dispatched, 0 completed).
    // Set false to go back to evidence-only completion (arrival / the vendor's own report).
    public bool TimedCompletion { get; set; } = true;

    // R4 scale factor on the ORDER'S AUTHORED TIME. 1.0 = as written (the default, and the only
    // value that reproduces the order). A DEMO compresses it: COA-STP1's tasks are PT1H20M and
    // PT2H, so 0.01 turns a 1h20m hold into 48 s and the whole 42-task chain into minutes.
    // It scales BOTH halves of the order's clock - the Duration that ends a task AND the
    // StartTime delay that holds one back (T13's 3h20m) - because compressing one without the
    // other would leave a "compressed" demo waiting 3h20m for its breach.
    // Applied where the value is USED, never in the parser: --parse-order always prints the
    // order as written.
    public double DurationScale { get; set; } = 1.0;

    // WHICH CLOCK A C2SIM TASK TIME IS MEASURED ON (M2 of the cold-start review of 5c67d41;
    // supervisor ruling 2026-09-14, Q2 default - the user may flip it).
    //   "sim"  (DEFAULT) the VR-Forces scenario clock, with an automatic WALL fallback whenever it
    //          cannot be read or has gone stale. A Duration in an order is a statement about
    //          SIMULATED time - "hold this objective for one hour twenty" is an hour twenty of the
    //          scenario, not of the operator's afternoon - and at the measured COA-STP1 sim ratios
    //          (0.27x-0.73x) the two differ by a factor of three or four.
    //   "wall" real seconds. Choose this to reproduce the pre-R4 behaviour, or when the scenario
    //          clock is not trustworthy for a particular run.
    // THREE THINGS RIDE ON IT, and they must ride on the SAME one or the chain breaks: the
    // Duration that ends a task (R4), the StartTime/DelayTimeAmount delay that holds one back, and
    // the STREND predecessor gate. Before this branch the first was on Vrf:StallClock and the
    // other two were pure wall seconds, so a run configured for sim had a gate that expired
    // 3-4x too early and skipped every successor.
    // The axis they are all served on accumulates FORWARD movement only (VrfC2SimService
    // .SampleTaskClock), so a PAUSED scenario adds nothing, a rollbackToSnapshot adds nothing, and
    // a fall back to the wall clock mid-run does not restart anybody's wait.
    // A PAUSED SCENARIO DOES NOT AGE A TASK (Q5, USER RULING 2026-09-14): when the sim clock has
    // been flat for StallPolicy.StaleClockWarnSeconds the axis HOLDS while a VR-Forces back end is
    // still present, and falls back to WALL seconds only when there is none. The signal is
    // VrfFacade::BackendCount, which cannot tell a live back end from one DEACTIVATED for missing
    // its status timeout - so the hold line repeats rather than being said once. See
    // StallPolicy.TaskClockAction.
    // AFTER A ROLLBACK (Q7, USER RULING 2026-09-14, ACCEPTED as recorded): because the axis adds
    // forward movement ONLY, a rollbackToSnapshot adds nothing and the re-simulated stretch is
    // served TWICE - once before the rollback and once after - so a task ends LATER in scenario
    // time than the order says. That is the intended trade: a deadline STAMP would instead fire
    // the moment a rollback happened to land past it.
    public string TaskClock { get; set; } = "sim";              // "sim" (default) | "wall"

    // WHAT A SUPERSEDED TASK REPORTS (m1 of the cold-start review of 5c67d41; supervisor ruling
    // 2026-09-14, Q1 default pending the user's - the user may flip it).
    // VR-Forces runs ONE task per unit: dispatching a new one REPLACES the running one, and the
    // interface's own log already says "the old task will not complete". It did not act on that -
    // the old task's R4 timer stayed armed and duly reported TASKCMPLT at its authored end time,
    // so STP saw TASKSTRT(old), TASKSTRT(new), TASKCMPLT(old), and the old task's successors then
    // dispatched onto a unit doing something else. The interface contradicted itself on the wire.
    //   "TASKABRT"  (DEFAULT) cancel the superseded task's end time and report TASKABRT AT THE
    //               SUPERSEDE POINT. The taskee is demonstrably not performing it.
    //   "TASKCMPLT" leave the timer armed: the end time is the ORDER'S statement about the task
    //               and it ends when the order says it ends, whatever the simulator did. This is
    //               the pre-fix behaviour, kept selectable because R4 read literally supports it.
    // NOT REACHABLE on COA-STP1 under the default PredecessorTimeoutPolicy=skip (measured: 0
    // taskees with more than one ungated task - every taskee is a serial chain); reachable with
    // "force" or "whenIdle", and on any order with concurrent tasks per taskee.
    public string SupersededTaskCode { get; set; } = "TASKABRT";

    // A TASK WITH NO DURATION AND NO GEOMETRY has NO KNOB (Q4, USER RULING 2026-09-14). It is
    // MALFORMED: R2 gives a task without geometry the unit's own position and R4 gives a task its
    // Duration as an end, and a task with neither has no vendor task to evidence it and no
    // authored time to end it. The supervisor default invented Vrf:DefaultHoldSeconds (60 s) so
    // the chain would proceed; the user ruled that a number which is not in the order is not ours
    // to invent, and that such a task is refused - ERROR naming both missing elements, TASKABRT,
    // and NotifyAbandoned so the successors fail fast. The knob is DELETED, not defaulted off:
    // TaskDispatchPolicy.IsMalformedZeroGeometryTask is the whole rule. None of COA-STP1's 42
    // tasks is affected - all 42 carry a Duration.

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
    // (Mojave terrain ~1100 m) a 100 m waypoint sits ~1000 m UNDERGROUND.
    //   *** THE MECHANISM THAT USED TO FOLLOW HERE IS ALSO REFUTED (removed 2026-09-04, caught by
    //   an adversarial audit that found the 2026-09-04 retraction had deleted only half of it):
    //   "so the aggregate member offset-route GROUND CLAMP ... yields EMPTY offset routes". The
    //   EMPTY offset routes were TYPE MAPPING - no member set for buildOffsetRoute to build routes
    //   for (CORRECTIONS_LOG "empty offset routes") - and the R9 freeze was route-by-NAME
    //   addressing (fixed 726f762). Waypoint altitude is FALSIFIED as a freeze cause; do not
    //   restore a below-terrain-waypoint causal chain here. ***
    //   *** CORRECTED 2026-09-04: a clause here used to add "and the unit freezes; and a ground
    //   unit BORN below terrain never executes movement at all". BOTH ARE FALSIFIED. Birth
    //   altitude is NOT the freeze discriminator (the 10000 m fix was already active in the
    //   07-19 scored runs and units froze anyway; three taskees at the SAME altitude split
    //   one-mover/two-frozen), and WAYPOINT altitude is falsified too - the below-terrain
    //   fixture variant MOVED. See docs/VRF_ALTITUDE_FRAMES.md sec 5 and CORRECTIONS_LOG
    //   "Birth altitude". Do not restore a "buried therefore frozen" claim here. ***
    // "Live" instead puts each ground waypoint at the unit's OWN live ground
    // altitude (read from the sim) + LiveClearanceMeters, and creates ground units at
    // [the CREATE half of this mode was RETIRED 2026-09-05: units are created at their authored
    // lat/lon and placed by an AGL setAltitude - PlacementPolicy.cs. The WAYPOINT half stands.]
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

    // CREATE-TIME clearance (2026-09-05). In the Live/TerrainProfile modes the app asks the back end
    // for the terrain height under EVERY create position of an init - one
    // DtIfRequestTerrainProfileInformation for the whole init (ifRequestTerrainProfileInformation.h
    // :45-51), the same query the route path uses - and creates each LAND object at
    // terrainHeight + this clearance instead of at 0. That is MAK's own documented pattern: the
    // shipped remoteControl sample creates a Tank_Plt and its M1A2 members at a point that is
    // ALREADY at the terrain ("Points are from Ala Moana terrain",
    // commandLineRemoteController.cxx:710-772; the point decodes to 1.0 m above the ellipsoid on
    // that near-sea-level terrain - docs/VRF_ALTITUDE_FRAMES.md sec 1a), and never calls
    // setAltitude. 1.0 is that number. The vendor rule the create relies on is UG52 14.3.3 /
    // vrf_newEntityPlacement.htm ("ground ... entities are placed on the ground ... at the highest
    // possible terrain intersection") plus the default clamp (ifCreateVrfObject.h:210-212).
    // If the query is not answered within TerrainProfileTimeoutSeconds the creates go out with the
    // PREVIOUS altitude (C2SIM MSL if the init gave one, else 0) and a WARN naming the fallback -
    // creation is never blocked on the query. Surface/subsurface are untouched (UG52 14.3.3:
    // "Surface and subsurface entities are created at sea level"). Fixed100 never queries.
    public double CreateClearanceMeters { get; set; } = 1.0;

    // AIR units only: the altitude ABOVE GROUND to give an air platform whose C2SIM init carries
    // NEITHER AltitudeAGL nor AltitudeMSL (xsd :2716-2717, both optional; every init in data/ omits
    // both). C2SIM defines no default, so this number is ARBITRARY and is logged as such on use.
    // 1000 is kept only because it is what the oracle assumed (C2SIMinterface.cpp:1378-1379).
    // It is delivered through setAltitude(uuid, m, aboveGroundLevel=TRUE)
    // (vrfRemoteController.h:1372-1374; VrfFacade.cpp:739), so it is AGL by construction.
    //
    // RETIRED 2026-09-05 (user direction): CreateAltitudeSafeMslMeters = 10000 - the "birth every
    // ground unit 10 km up so the create clamp drops it" workaround. Ground units are now created
    // at their AUTHORED lat/lon and placed with an AGL set (VrfC2SimService, "PLACEMENT"). The
    // oracle's folk belief that "1000.0 triggers VRForces Gound Clamping" (C2SIMinterface.cpp:685)
    // was never a VR-Forces behaviour; it was just above the ground at sea-level Bogaland.
    public double AirDefaultAltitudeAglMeters { get; set; } = 1000.0;

    // The post-create setAltitude(...,aboveGroundLevel=TRUE) is BELT-AND-BRACES: since 2026-09-05
    // the create itself is at terrain+clearance (the documented placement, UG52 14.3.3), so the set
    // only insures against the create landing wrong. It is documented as ignored for a non-air
    // vehicle (setAltitudeRequest.h:24-25) yet was observed to lift a buried ground M1A2
    // (PREREG_CLAMP_DIRECTION sec 8a), so on the create path it CONFOUNDS a test of the create:
    // an object could read on-terrain because the create placed it OR because the set rescued it.
    // Set false to ISOLATE the create (the confirming-run control, PREREG_PLACEMENT_R9_52 A1):
    // no post-create set is registered for the placement path, so where an object ends up is the
    // create alone. Default TRUE = production safety. This gates ONLY the placement path's set;
    // the Fixed100 parity branch and any air-unit set are unaffected (they do not read this).
    public bool PlacementAglSet { get; set; } = true;

    // B2 (2026-09-14): a TaskStatus report is emitted ONCE per task per outcome and nothing
    // re-sends it, so a push that fails is information lost for the whole run - 129 pushes failed
    // in G6 with "The response ended prematurely" and the run log said nothing. TASK-STATUS pushes
    // (not position pushes: the next poll carries the same fix seconds later) are retried this many
    // times in total, backing off TaskStatusPushBackoffMs, doubling (1 / 2 / 4 s by default).
    // 1 disables the retry; the failure is still counted and still says so loudly.
    public int TaskStatusPushTries { get; set; } = 3;
    public int TaskStatusPushBackoffMs { get; set; } = 1000;

    // ================= ROUTE PRE-FLIGHT (DEMO_READINESS row 20) ===============================
    // Before a move is dispatched, walk its route against the SAME terrain the sim streams and
    // warn about legs whose sustained climb reaches the performing unit's own derated limit.
    // Ported from tools/preflight/leg_check.py; the numbers below ARE the calibration
    // (docs/experiments/PREFLIGHT_CALIBRATION_2026-09-13.md).
    //
    // SHIPS OFF. A flag is a PREDICTION off terrain tiles, never a vendor verdict: DEMO_READINESS
    // row 20 rules that the warning channel is all this earns until the calibration shows zero
    // false alarms on more than the one run behind it (nine legs, one order, one terrain, three
    // positives). Turning it on emits ObservationReports only - it never refuses or alters a task.
    public bool PreflightWarnings { get; set; } = false;

    // Flag a leg when sustained / (min max-slope x soil factor) reaches this. 0.92 is the
    // MIDPOINT of the 0.096-wide gap between P11's frozen legs (0.966-1.098) and its clean
    // movers (0.438-0.870): three flags, three units that froze, no misses, no false alarms.
    public double PreflightThreshold { get; set; } = 0.92;

    // The sustained window (m). AN OPERATING POINT, NOT A CONSTANT: at 80 m the separation dies
    // (margin -0.003) and 55 m goes to -0.008 in the same sampling cell; only 40 m holds its
    // margin across all six sampling variants (leg_check.py --sensitivity).
    public double PreflightWindowMeters { get; set; } = 40.0;

    // Sample spacing along a leg (m), and the SHORT window that is reported but never decides.
    public double PreflightStepMeters { get; set; } = 8.0;
    public double PreflightShortWindowMeters { get; set; } = 20.0;

    // Where the streamed terrain tiles are cached. Empty = "preflight-cache" beside the
    // executable. The tool's own cache (tools/preflight/preflight_cache) uses the SAME file
    // naming, so it can be copied in to run the pre-flight with no network at all.
    public string PreflightCacheDir { get; set; } = "";

    // Never fetch a tile; score only what the cache already holds. A leg whose tiles are missing
    // gets NO VERDICT - it is never quietly passed.
    public bool PreflightOffline { get; set; } = false;

    // The MAK SharedData root the land-cover CLASS -> soiltype catalogues are read from
    // (osgEarthCatalogs/coverage/layer.*.online.xml). The vehicle limits and the soil
    // acceleration-factors come from Vrf:VrfHome instead. Read-only, both of them.
    public string PreflightSharedDataDir { get; set; } = @"C:\MAK\SharedData\19\latest";
}
