# VR-Forces 5.2 - decision evidence for sec G of VRF_5.2_MIGRATION_DIFF.md

Written 2026-09-03. Research only: no build, no run, nothing under C:\MAK touched.
Purpose: give each Y-decision (a) the evidence, (b) the vendor's direction as read from
their own words and their own shipped data, (c) what going against that grain would cost,
(d) the recommendation and whether the evidence moved it. Every claim below was verified
on disk or in a document by the supervisor, not taken from an executor's report.

Sources (short names): UG52 = VR-Forces 5.2 Users Guide; MG = VR-Forces User Migration
Guide (5.2); RN = VR-Forces 5.2 Release Notes; APIMG = Developer's Guide "5.2 API
Migration Guide"; IOG = MAK Interoperability Guide. Header paths are relative to
C:\MAK\vrforces5.2d\include; data paths to C:\MAK\vrforces5.2d\data\simulationModelSets;
terrain paths to C:\MAK\SharedData\19\latest\TerrainData. "5.0.2" = the same paths under
C:\MAK\vrforces5.0.2 / SharedData\16.

## 0. The vendor's direction, in one paragraph

5.2 moves intelligence out of the tasking client and into the entity. A ground move is a
destination; the entity's Lua planner (`ground-vehicle-move-to.lua`: "plans paths on roads
if available, and around feature obstacles when off road. Uses nav meshes off road if
available") decides how. The client shapes behaviour through set-data knobs (Navigation
Preferences, Autonomous Actions, Collision Avoidance Enabled), not through task choice.
Old task IDs survive only as a scenario-loading shim (MG 2.3). Federation identity moves
from per-app flags into one exercise-connection file that both sim and clients load, with
NETN layered over RPR (IOG, MG 3.1). Terrain is streamed, asynchronous by default, with one
documented knob to make it deterministic in fixed-frame modes (UG52 Table 76). The remote
controller is a first-class federate: session id, monitor back-end state, no manual clock
(5.2d examples\remoteControl\main.cxx). Working WITH this means: send the simplest task that
states intent, configure through set data, let the shipped SMS defaults stand, and pin
determinism only where the vendor put the knob.

## Y-1 / Y-4 / Y-5 Launch profile, log path, LaunchVrf.ps1

- Evidence: 5.2d launcher profiles carry only autoconnect/configFile/hostAddress/sessionId
  (rows S1); every 5.0.2 vrfLauncher CLI option is gone; direct bin64 launch is documented
  (UG52 4.1.2); default log path C:\MAK\logs, `--logFileName` overrides (row A5).
- Vendor direction: the launcher is a GUI convenience; batch and headless users run the
  sim engine directly (UG52 7.10 shows `vrfSimDIS --siteId 1 --appNumber 3001 ...`).
- Against the grain: scripting the launcher GUI = GUI automation, which THE GOAL forbids.
- Recommendation UNCHANGED: direct launch from bin64; `--logFileName` to keep the watcher
  path; LaunchVrf.ps1 rewritten around the 5.2 options.

## Y-2 Federation identity (MAK-ONE-2025 config file vs C# FomModules)

- Evidence: `vlpi\exerciseConnConfig.h` :24 `#define DtDefaultConfigFile
  "MAK-ONE-2025-Config.xml"`, :47 `loadFile(.mtl or .xml)`;
  `vl\exerciseConnInitializerHLA.h` :390-401 setMimModule/addFomModule/setFomModules
  still exist, :772-773 `myMultipleConfigFiles` "load in multiple config files merging and
  replacing properties as needed". The shipped file carries 17 modules incl. NETN and
  VRFExt-12 (row S4); `requiredFomClasses.mtl` lists classes the sim refuses to run
  without (row S6). Modules are ADDITIVE: keeping appsettings FomModules submits VRFExt-6
  AND VRFExt-12. A bumped module number is an incompatible change (IOG 2.1 p24).
- Vendor direction: one connection file for all federates ("far simpler than specifying
  each FOM", RN VRF-9054); NETN over RPR is now required (MG 3.1 p24).
- Against the grain: a repo-authored module list must be a SUPERSET of the sim's
  (requiredFomClasses) or the federation the federate creates first will reject the sim;
  drift between two lists is the failure class we already paid for with CWIX-2024.
- Recommendation UNCHANGED, sharpened: resolve the shipped file exactly as the sample does
  (`DtRemoteControlInitializer(argc, argv, defaultConfigFilePath)`), empty the C# FomModules
  list, retire CWIX-2024. Repo-controlled overrides go in a SECOND config file layered via
  myMultipleConfigFiles, never a replacement list.

## Y-3 / Y-9 vrfSim.mtl edits and repeatability knobs

- Evidence: factory notifyLevel 2/1/0 at the same keys (row S3);
  `blockOnAsynchronousOperations 0` :445 "Applies only to scenarios that are set to run in
  a fixed frame clock mode (Fixed-Frame Best-Effort or Fixed-Frame Run-To-Complete) ...
  block on asynchronous operations (terrain, feature, path planning, navigation)" (UG52
  Table 76 p1669); `maxAsynchronousTerrainThreads 7`; thread budget advice "sum ... about
  2 less than logical CPUs" (UG52 p197); `assertOnBlockingTerrainCalls` exists to HUNT
  blocking calls, i.e. blocking is the exception the vendor engineered around.
- Vendor direction: async streaming terrain is the default; determinism in fixed-frame
  modes is provided by exactly one knob. Every vendor ground scenario ships
  `frame-mode "variable-frame"` (WainwrightMechanizedAttack.scnx) - FFRTC is a batch
  posture, not their demo posture, and the 0.27x COA-STP1 ratio (memory) is consistent
  with "load-bound by design".
- Against the grain: leaving block=0 under FFRTC makes path-planning results depend on
  wall-clock arrival of tiles - the non-determinism class we adjudicated on 5.0.2 without
  this knob existing.
- Recommendation UNCHANGED: edit the three notify keys (needs the user's OK, C:\MAK);
  `blockOnAsynchronousOperations 1` for FFRTC fixture runs; thread count pinned and
  recorded in the prereg. Cost is measured, not guessed, in Phase 2.

## Y-6 Facade start/tick re-authored on the 5.2d sample

- Evidence: 5.2d main.cxx :26-66: controller constructed BEFORE exConn ("to avoid
  requiring a VR-Link license" - our order :395/:396 already complies);
  `init(exConn, nullptr x4, "entity-identifier", disableRemoteDiscovery=false)`;
  `setMonitorBackendState(true)`; `vrfMessageInterface()->setSessionId(...)`;
  `communicationManager()->run()`; loop is `tick()` only. 5.0.2 sample :50-53 and our
  `VrfFacade::Tick` :480-482 do `setSimTime(elapsedRealTime()); tick()`.
  vrfRemoteController.h :2045-2054: tick() "will also drain input";
  setMonitorBackendState: "Normally the remote controller will advance time on wall clock
  time without listening to play/pause state ... Set this flag to true to pause/resume the
  connection with changes in play/pause state". Only compile break: remoteControlInit.cxx
  :19-20 `myHlaConnection` (deleted in VR-Link 5.10). -a/-s survive on the base parser.
- Vendor direction: the controller owns its clock; the client does not push wall time.
- Against the grain: keeping `setSimTime(elapsedRealTime())` under FFRTC (sim time
  decoupled from wall time by design) pushes a wall clock into a run-to-complete
  federation - the two clocks fight, and our frame_gaps instrument reads that fight as
  sim behaviour.
- Recommendation STRENGTHENED: adopt the sample loop verbatim (drop setSimTime and the
  redundant drainInput; monitorBackendState true; session id from config). Keep
  disableRemoteDiscovery=true only if Phase 1 confirms nothing reads reflected state;
  otherwise the sample's false. Ledger/appNo kept.

## Y-7 Terrain successor

- Evidence, what we are leaving: 5.0.2 "MAK Earth Space (online).earth" =
  `{% include elevation.worldwide.lowres.xml %}` + imagery; lowres = LOCAL
  `WorldElevation.dem`, "extremely low resolution ... very low-res (LOD 3)" upsampled to
  LOD 7; NO roads, OSM, features, surfChar or nav data. Inherited from C2SIM
  TropicTortoise.scnx, never chosen.
- Evidence, candidates (SharedData\19, .earth files are authoritative; the UG52 Table 52
  row claiming Base has "streaming feature data" is contradicted by Base's own header
  "Basically just imagery and elevation"):
  | terrain | vrfsim:max_data_level | roads/features to sim | surfChar | nav data |
  | MAK Earth (online) | 15 | OSM roads (`osm.roads.model.xml` vrfsim:enabled="true", layer Roads), buildings, water, vegetation, CA insets | yes | 4 areas |
  | MAK Earth Aggregate (online) | 12 | roads + `VRFSIM.Aggregate.feature.model.xml` land-cover polygons for map_vrf_layers | no | none |
  | MAK Earth Base (online) | 10 (~90 m) | none | no | none |
  | MAK Earth Air and Space (online) | 10 | all feature layers vrfsim:enabled=false | no | none |
  | MAK Earth (online) - Simple for High Fliers | 15 | water only; SAME map name as MAK Earth (online) inside the file | no | none |
  All stream elevation from vr-theworld.com ("minimum global 30 m", "10m for all of
  California"); every (online) terrain requires an internet connection.
- Evidence, vendor pairing (census of every shipped .scnx): MAK Earth (online) +
  EntityLevel.sms = GroundMovement, BehaviorGroundAttack*, WainwrightMechanizedAttack,
  AirDrop, HawaiiNaval; MAK Earth Aggregate + AggregateTacticalLevel.sms = RoadToKaunas,
  MiamiAmphibiousAssault, VanillaRepublic; Air and Space + EntityLevel = air vignettes.
  Header of MAK Earth (online).earth :3: "the primary terrain for use in ground or air
  applications".
- Evidence, our AOI: fixtures span 34.33-34.68N / -117.01..-116.39W (R9, COA order) -
  Southern California, inside the 10 m coverage. Shipped nav areas (Ala Moana, Kilo2,
  Range220 at 34.42N/-116.28W, Thun) do NOT cover it; Range220 is the nearest, just east.
  Nav data is not required for Move To (UG52 23.2.1: roads -> nav mesh -> feature-obstacle
  planning fallback); generating it needs the GUI plus an extra Gameware licence (UG52 3.6
  p123) - out of scope by THE GOAL.
- Vendor direction: entity-level ground work on MAK Earth (online); Aggregate is for
  AggregateTacticalLevel units, whose movement is abstract and reads land-cover modifiers
  (UG52 28.2.2), which our ENTITY-level company does not use.
- Against the grain: Aggregate gives our tanks a coarser DEM and no surfChar (soil
  effects UG52 23.5.1 silently default); Base/Air-and-Space give no roads at all, so the
  redesigned planner degrades to feature-obstacle-free straight lines - which would make
  5.2 LOOK like 5.0.2 and hide the whole point of the redesign.
- Recommendation CONFIRMED: MAK Earth (online). Consequence to prereg: the 5.2 runs will
  differ from 5.0.2 traces because roads, slope and soil now exist - that is the terrain
  changing, not a defect. TerrainProfile altitude tolerance is set from LOD 15 vs LOD 7.

## Y-8 SMS root and type map on 5.2

- Evidence, SMS purpose (UG52 68.3 p1309): EntityLevel "Defines entities, units, and
  other objects used for entity-level scenarios"; AggregateLevel "Legacy SMS preserved for
  backward compatibility"; AggregateTacticalLevel "battalion-size simulation objects or
  smaller" on the HLA aggregate connection. Disk census: EntityLevel is the ONLY SMS
  holding both platform leaves (1530) and disaggregatable units (172, 133 on
  Aggregate.ope); AggregateTacticalLevel is unit-only (401/10) on
  AggregateLevelAggregate.ope. C2simEx.sms is an include-and-extend of EntityLevel.sms
  (MG 1.2.1/1.2.2: .entity files copy across; .sysdef/.ope must be re-edited from the new
  version's files) - it is NOT a copied SMS.
- Evidence, BREAK: 5.2d EntityLevel and AggregateTacticalLevel dropped the leading
  superType field - `Tank Company (USA).entity` objectType "3:11:1:225:5:2:0:0" ->
  "11:1:225:5:2:0:0", `M1A2_Abrams_MBT.entity` "1:1:1:225:1:1:3:0" -> "1:1:225:1:1:3:0"
  (verified both installs). APIMG: "DtObjectType ... has been removed ... replace with
  DtEntityType"; "superType() ... can be substituted for kind() == 11".
  `src\VrfC2SimApp\ObjectTypeResolver.cs` :136 and :150 `if (parts.Length != 8) return
  null;` -> aimed at 5.2d the offline resolver indexes ZERO templates. The C++ path is
  unaffected (`VrfFacade.cpp` :89-92 builds 7-field DtEntityType). AggregateLevel and
  AggregateLevelBase were NOT converted (135/135 and 84/90 still 8-field) - a vendor
  inconsistency that a two-format parser must tolerate.
- Evidence, resolution: re-running the resolver's best-match rule over the 5.2d
  EntityLevel chain with the superType stripped lands the identically named template for
  30 of 31 types (Tank Company/Platoon/HQ USA and RUS, M1A2, T-80, Type 99, artillery,
  ADA, CSS, squads...). The one miss: AR Scout (3:11:1:225:14:30:0:1, oracle-only) falls
  to Ground_Aggregate. Mobile Irregular (3:11:1:-1:13:34:0:1) and Mobile Light Infantry
  (3:11:1:225:13:3:0:200) have NO type equivalent in 5.2d; Skiff was never oracle-only.
  All 9 Ar_Plt_US_*.frm ship natively (byte-identical 5.0.2/5.2d); the oracle's 4
  overrides differ in geometry (and carry the typo "Wegde-Formation"). Formation names
  inside Tank Platoon (USA).entity are now lower-case (column-left ...), agreeing with
  the runtime list our C# already observed (VrfC2SimService.cs :1484-1491).
- Vendor direction: include-and-extend SMSs; entity-level units live in EntityLevel;
  supertype is gone from the data model.
- Against the grain: carrying C2simEx forward means re-editing 4 .sysdef against the
  redesigned movement (MG 2.4.2 says you MUST copy the new system and re-apply edits)
  and reviving oracle custom types the fidelity ruling (2026-09-02) already retired.
- Recommendation CONFIRMED + NEW WORK: EntityLevel.sms; drop C2simEx. Phase 1 gets a
  predicted break row: resolver accepts 7- or 8-field types (kind==11 replaces superType);
  `--typemap-selftest` re-gated with the expected 30/31 + Fire Support Team tie; AR Scout,
  Mobile Irregular, Mobile Light Infantry re-adjudicated from the 5.2 catalog; oracle
  formation geometries dropped (stock .frm).

- OUTCOME 2026-09-03 (Phase 1): prediction confirmed exactly - 2182/2190 5.2d EntityLevel
  simObjects 7-field (8 vendor stragglers 8-field); resolver normalises 7 -> 8 by kind==11
  (superType 3 <=> kind 11 held in all 1713 5.0.2 files); a matchType whose field count
  differs from its own objectType (5.0.2 EC-135 Eurocopter.entity, 7 vs 8) is a vendor typo
  and falls back to exact match as before. Substitutes (user: "pick substitutes"): 5.2d
  EntityLevel has NO USA scout/recon unit (Armored Cavalry Platoon/Troop/Squadron .entity =
  Country-0, gui-can-create False, zero subordinates; Recon Vehicle Platoons are RUS only).
  AR Scout rows -> `Mechanized Platoon (USA Army M2)` 11:1:225:3:4:0:126 (2x M2A2 + 2x M2A3
  Bradley IFV + 3 mech rifle squads + HQ section; same chassis family as the M3A2 CFV, wrong
  branch) for N/D, `Mechanized Company (US Army M2)` 11:1:225:5:4:0:126 for E - both PROXY,
  both NEW in 5.2d (absent from 5.0.2), recorded in data/unit-type-map-52.json (the 5.2 table;
  data/unit-type-map.json stays the 5.0.2 table). Mobile Irregular / Mobile Light Infantry
  were never map rows (ground-truth check only) - no substitute owed. Self-test: 783 checks on
  5.0.2, 784 on 5.2d, 5.2 table on 5.0.2 catalog FAILS 5 (negative control).

## Y-10 Ground MOVE mapping (MoveAlongRoute vs Move To)

- Evidence: `moveAlongRoute` on the controller is byte-identical 5.0.2/5.2d
  (vrfRemoteController.h :1638); `DtMoveAlongTask` carries no deprecation; MG Table 1
  maps move-along to move-along; MG 2.4 p19 "improved to better control the entity's
  speed and also uses dynamic obstacle avoidance". For a UNIT:
  `vrfmodel\disaggregatedMoveAlongAdapterController.h` :9 "repackages 'move-along' as
  'maneuver-along'"; `ground-disaggregated-movement.sysdef` :176-196 wires
  move-along-controller = `aggregate-move-along-adapter-controller` and a new
  `aggregate-maneuver-along-controller` (catchup 1.5, slowdown 0.5) - so the stale
  moveAlongTasks.h comment (lead + follow) loses to the UG and the sysdef. Route offsets
  per subordinate (UG52 30.22/30.24) are computed at task time. `DtMoveToLocationTask` is
  DELETED (moveToLocationTasks.h is a 15-line `#error` shim); `DtMoveToTask` absorbed it
  (DtRwLocationReference control point); `DtPlanAndMoveToTask` still derives from it and
  our :575-579 usage compiles; `DtPlanAndMoveToLocationTask` header is gone. moveToTask.h
  :181-184: "move-to is deprecated for ground vehicles. When a move-to is issued, a
  ground-vehicle-move-to is started as a subtask". RN VRF-8977: unit Move Along Route
  failed unless saved-and-rewound; 5.2 "waits until a unit's formation is considered
  valid before initiating the movement".
- Vendor direction: their own 5.2 ground scenarios task `move-along`,
  `move-to-location-task`, `convoy-along-task`; route-following remains THE
  non-planning task; destination tasks go to the planner.
- Against the grain: replacing an authored route with Move To hands route choice to the
  planner (roads/obstacles) - a different product from what C2SIM's MoveAlongRoute
  expresses; probing MoveToLocation buys nothing since the class it named no longer
  exists.
- Recommendation CHANGED: keep MoveAlongRoute; RETIRE the MoveToLocation/PlanAndMoveTo
  headless probe from the queue (no consumer, no class). Phase 3 prereg carries VRF-8977
  as a competing hypothesis for the 5.0.2 company non-determinism (formation-validity
  race at task time) with its falsifier (stable sub-routes on 5.2 across 2 runs).

## Y-11 Unit Move To -> Maneuver To

- Evidence: RN VRF-9245 / MG 2.4: ground units given Move To default to Maneuver To,
  which "always uses off-road navigation preferences". Maneuver To is a C++ SCRIPTED task
  (`EntityLevel\scripts\maneuver-to.xml`, engine c++, no vrftasks header) with one
  variable `destination` (locationreferencewithoutaltitude); issued via
  `DtScriptedTaskTask`, which the facade already includes.
- Vendor direction: cohesive off-road unit movement is the default; the client states
  the destination only.
- Against the grain: forcing per-entity move-to on a unit reintroduces the lead/follow
  behaviour the vendor just replaced (VRF-9243 unified formation controller).
- Recommendation UNCHANGED: accept. Only relevant if a destination verb is ever mapped;
  MOVE stays on MoveAlongRoute (Y-10).

## Y-12 Autonomous Actions

- Evidence: `platforms\Ground_Vehicle.ope` :436 `(DtRwBoolean AutonomousActionsEnabled
  True publish)` (5.0.2 :447 `AIEnabled True`); MG 2.6 p21 rename only, "performs the
  same functions"; UG52 23.6 p508: disabled = no planning, straight line through
  obstacles.
- Vendor direction: planning is on by default; the disable switch exists for direct
  external control (with `Collision Avoidance Enabled` for the avoider).
- Against the grain: disabling to "get repeatable traces" produces traces of a mode the
  vendor calls dumb driving, not of the simulation we are integrating.
- Recommendation UNCHANGED: enabled. Repeatability comes from Y-9, not from this switch.

## Y-13 Navigation Preferences / SMS defaults

- Evidence, THE FLIP: `systems\movement\ground-tracked.sysdef` 5.0.2 :71
  `(default-preference "Prefer Roads")` hard-coded; 5.2d :132 `(default-preference
  $road-preference (default "Ignore Roads"))` parameterised, and M1A2_Abrams_MBT.entity
  :218 pins `<string paramName="road-preference">Ignore Roads</string>`; T-80_MBT sets
  nothing and inherits Ignore Roads. MG 2.4 p18: "By default, vehicles that use the
  tracked or wheels-off-road movement systems ignore roads". Other changed keys in the
  same file: obstruction-sensor, collision-avoidance, move-to-location controllers
  REMOVED; move-to-adapter, maneuver-in-formation (near 15 / at 2 / obstacle-buffer 7 m),
  react-to-collision-event-actuator ADDED; near-distance 25 -> 15 m on every movement
  controller (wait-at-waypoint 35 -> 15); ALL per-soil max-speed-factor entries removed
  (stopping/acceleration factors remain); pathfinder-weight-factors populated (primary
  1.0 ... dirt/trail 2.25, off-road-path-weight 3.0); fuel-amount -> fuel-efficiency +
  SISO resource type (MG 2.2.3). Ground_Vehicle.ope: nav-interface "active-bot" ->
  "dynamic-obstacle"; disaggregation-range removed; target-selection -> weapon-management.
  Vendor scenarios set `selected-navigation-preference "Ignore Roads"` per military
  entity and "Prefer Roads" for civilians - i.e. they state it explicitly.
- Vendor direction: tracked vehicles ignore roads unless told; preference is a set-data
  knob on the entity, not a task parameter.
- Against the grain: sending "Prefer Roads" to reproduce 5.0.2 traces re-creates a
  default the vendor deliberately abandoned for armour; conversely, relying on defaults
  while ANY fixture entity is wheels-road silently mixes behaviours.
- Recommendation UNCHANGED in substance, made explicit: rely on SMS defaults (fidelity
  ruling), record in the prereg that tanks now ignore roads, arrive within 15 m not 25 m
  (TASKCMPLT timing shifts), and lose per-soil speed caps. No per-entity send unless a
  C2SIM order carries a road-use intent.

## Y-14 SQLite logging and batch mode as instruments

- Evidence: UG52 7.10 p269-273 "Batch mode is read-only. You cannot save a scenario,
  create simulation objects or objects, pause the scenario, or otherwise change it";
  SQLite state logging via `appData\settings\databaseConfig.mtl` (RN 5.2).
- Vendor direction: batch = replay a saved scenario to completion; logging = passive
  state capture alongside any run.
- Against the grain: batch mode cannot host remote-control creation or tasking, so it
  cannot run a C2SIM order at all.
- Recommendation CHANGED: batch mode REJECTED for interface runs (usable only to replay a
  saved .scnx as a control); SQLite logging still worth a Phase 2 evaluation as a
  cross-check of the census.

## Y-7 ruling text (moved from the DIFF ledger 2026-09-03 to keep it under its cap)

Y-7 RULED as recommended (2026-09-03 pm): online default; offline-authored (3) for the
  AOIs that matter; cached (2) as the cheap fallback. MAK Earth (online) (vendor: primary
  ground/air terrain; streams
  worldwide elevation max_data_level 15 + OSM features/roads from vr-theworld.com). User:
  OFFLINE IS A REQUIREMENT in some settings -> a per-fixture PROFILE, both kept:
  (1) online; (2) offline-cached: same .mtf with an osgEarth cache generated once per AOI
  (AddingContent 8.1.2, GUI Generate Cache; sim reads VRFSIM_OSGEARTH_CACHE_PATH) -
  imagery+elevation only, FEATURES ARE NOT CACHED so roads/land-use vanish offline and
  ground traces differ from (1); (3) offline-authored: local .earth for the AOI with the
  elevation tile + OSM shapefile extract (AddingContent, features as shapefiles) = full
  parity, content work per AOI; (4) shipped USGS N34W117 (R9 box only, 5.a). Each profile
  is its own baseline; never compare traces across profiles. The aggregate profile (Y-15)
  uses "MAK Earth Aggregate (online)" (UG52 Table 52).

## Y-15 Unit representation level (ruling text, moved from the DIFF ledger)

Y-15 UNIT REPRESENTATION LEVEL (user goal: best sim ability within what STP hands us;
  STP vocabulary in docs/STP_TASK_VOCABULARY_2026-09-03.md - 51 codes, AffectedEntity
  is always the performer, targets = objective TG). Options, UG52 13.7/27/28/35 read:
  (a) EntityLevel: individual vehicles, weapons, sensors, LOS, road/obstacle following,
  formations with spacing (ground-disaggregated-movement controllers); unit tasks on disk
  = move/maneuver, follow, fire-at-target, mortar/artillery, posture, formation;
  attack-to-objective/defend/screen/recon must be AUTHORED as Lua unit tasks composed
  from those primitives (vendor path examples\addTask + luadoc; ~10-15 tasks).
  (b) AggregateTacticalLevel: 54 doctrinal tasks native (unit-attack-to-objective,
  unit-defend, reconnoiter-*, manage-fire-support, attack-by-fire with sub-unit
  placement) but the SIM is abstract: attrition per second from strength/vulnerability
  tables, footprints, direct-line movement, no collision/building/feature avoidance,
  roads only by task option (UG52 27.1, 27.1.4, 28.2.2); telemetry = centroid + health.
  Runtime hybrid does NOT exist: modeling type is fixed by the scenario's SMS (UG52
  13.7); an aggregate SMS may hold entities but they run aggregate models; UG52 has no
  aggregate/disaggregate-at-runtime feature (NETN-MRM module ships in bin64 unused).
  RULED as recommended (2026-09-03 pm): hybrid = TWO PROFILES selected by the order's
  echelon/scale - EntityLevel + authored doctrinal Lua for company-and-below COAs (the
  fidelity is in the physics), AggregateTacticalLevel for battalion+ or entity counts that
  make FFRTC crawl (COA-STP1 0.27x). Authoring order: attack-to-objective family first
  (covers ~12 codes). Profile = a fixture/runner setting (SMS + terrain + type map).

## Y-16 Protocol: HLA 4 on MAK RTI 5.0.1 (ruling text, moved from the DIFF ledger)

Y-16 PROTOCOL (NEW): HLA 4 (IEEE 1516-2025) on MAK RTI 5.0 vs HLA 1516e on 4.6.1.
  MAK RTI 5.0.1 INSTALLED by the user 2026-09-03 (DISK: C:\MAK\makRti5.0.1, branch
  makRti5-0 rev 281993 built 2025-12-03; bin has librti1516_2025vc141.dll AND
  librti1516e64.dll, rtiexec.exe, rtiForwarder.exe; doc has RTI5.0.1ReleaseNotes.pdf,
  RTIUsersGuide.pdf). Env vars UNTOUCHED (MAK_RTIDIR/RTI_RID_FILE still 4.6.1) - the
  runner's HLA 4 profile sets them per process. Unlicensed mode = 2 federates (vrfSim +
  controller); DEMO .lic PACKAGEs carry rti1-rti7 + makrti_counted (version 2026.258) -
  licensing to be VERIFIED in the first HLA 4 join. On disk already: vrfSimHLA4.exe,
  remoteControlHLA4.exe, vlHLA4.lib, vrfExtObjectsHLA4.lib, vrfHla4.lib. Sequencing
  unchanged: bridge gets a protocol build axis (HLA1516e | HLA4), migration gates run
  on 1516e/makRti4.6.1 (one variable), HLA 4 = its own phase with prototype zero on
  remoteControlHLA4.exe first; 5.0.1 also serving 1516e is a SECOND variable, not a shortcut.

## Notes that feed later phases (not decisions)

- RN "Known Problems" is a bare KB pointer; the notes publish no items.
- RN VRF frame-overrun fix on Windows (1 ms timer request) - frame_gaps re-baseline.
- RN: a VR-Forces based executable run OUTSIDE bin64 uses default Legion double-buffer
  config unless `vrfLegion.lua` is copied beside it - Phase 1 deploy note for our
  out-of-tree host.
- Remote-control create calls can specify the global ID (RN VRF-9961) - optional later.
- `DtTaskCompleteReport::success()` exists on 5.2d (taskCompleteReport.h :84-90,
  "Defaults to True"), unchanged - Phase 4 stands as planned.
- Simple-for-High-Fliers declares the same internal map name as MAK Earth (online); the
  runner must key on the .mtf file name, never the map name.
- `DtMaintainFormationMonitor` now reads a state property for subordinate speed control
  (RN); any sub-route/speed observation is downstream of that change.
