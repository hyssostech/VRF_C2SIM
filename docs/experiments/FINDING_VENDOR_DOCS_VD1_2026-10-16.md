# REPORT VD-1 - VR-Forces 5.2 DOCS READ (docs-only) - 2026-10-16
Reading gaps: the L2 RESULT files in m1m2 docs/experiments and the 10-15 VRF census record were not read; the HTML help grep for C2SIM timed out.

Worker: fable, effort high. Read-only: no process started, nothing under C:\MAK or the VRF_C2SIM repo written; this file
is the only write. Sources read: the nine text extracts under V/docs/vendor/mak-5.2/txt (UG = VR-Forces_5.2_Users_Guide,
RN = Release_Notes, MG = Migration_Guide, FEG = First_Experience_Guide, AC = MAK_ONE_2025_Adding_Content, MC =
MAK_ONE_2025_Model_Catalog, IOG = Interoperability_Guide; page = PDF page label), two vendor config/data files under
C:\MAK\vrforces5.2d (terrainInterfaceConfig.mtl, AggregateLevelBase default-task-rules.tsk), and the own records named
in the brief. Own-record citations are file:line on the main checkout as of today. [V] = read at the citation; [A] =
inference. Measurements and design implications are kept in separate sentences.

Brief items NOT FOUND under V/docs: no file matching L2_RESULT_* and no "10-15 VRF census" record (Glob + grep for
"census" over docs/experiments/*2026-10-1*: 0 hits). The 10-15 records that exist and were read: D36_BACKEND_STOP_AND_
WARM_CACHE, REPRO_BACKEND_STOP, D47_NAVGATE_LIVE_AND_D16_PRO, PRERUN_STP976, SCOPE_DEMO_EXTENT (10-14), and the vendor
case docs/vendor-cases/MAK_BACKEND_HANG_2026-10-15.md.

## Q1 TASKING

VENDOR
- Tasks are assigned "in plans, through the Task menu, or in the Command Panel" (UG p554). "if the new task is mutually
  exclusive with the current task, the simulation object immediately stops the current task and begins the new one"
  (UG p554); "If a simulation object has a plan, when you give it an independent task, it abandons the rest of the plan."
- UG p561 (29.1.4): "If you assign a simulation object a task that is mutually exclusive with its current task, the
  current task is abandoned." Groups Weapon / Movement / Radio / Depth control: "All tasks within a group are mutually
  exclusive among themselves. For example, all movement tasks are mutually exclusive."
- UG p861: set data requests "take effect immediately. Assignment of a set data request does not override the task ...
  By contrast, if you assign a simulation object a task, it stops its current task and begins the new task."
- UG p580-581 (29.7): the rules file is default-task-rules.tsk per model set; "It does not include scripted tasks.
  Concurrency for scripted tasks is set when they are created." UG p766: a scripted task's Action Categories default to
  Movement, "which means that if you do not change the setting, the new task will interrupt movement tasks."
- Disk [V]: AggregateLevelBase default-task-rules.tsk :2 all-tasks-exclusive False; :7 the Movement group lists move-to,
  move-along, plan-and-move-to, move-to-location-task, stop-moving, scripted-movement-task, wait.
- UG p566 (29.1.10 Skipping a Task): "Skip Task does not apply to members of a unit that are executing tasks assigned
  to them by the unit leader."
- User-level vs system subtasks: UG p500 (23.2) Move To "is implemented as a Lua script using a behavior tree"; "For
  off-road segments, the vehicle uses a Move Along Route subtask. For road segments, the vehicle uses a rail-move-along
  subtask". The string navigate-to-location has 0 hits in the nine PDFs; the GUI-level aggregate movement tasks are
  Move (p734, "Optionally ... Use Roads"), Move Along Route (35.5.7 -> 30.24), Move To (35.5.9 -> 30.28), and Move to
  Location / Waypoint (Plan Along Roads) (p735): the unit "moves directly to the nearest point on the road network and
  then moves along the roads until it reaches the point on the road that is closest to the destination. At that point,
  the unit moves off the road and proceeds directly to the destination."
- (a) Containers / groups: UG p551 (28.3.2): "Groups of ground units can move directly to the destination or they can
  plan along roads. Group planned movement along roads first generates a route for the group. Next, it calculates
  destinations for the subordinates based on their relative positions at the time of tasking. After that, the
  subordinates queue along the route until they reach the end of the route."
- (b) Units: UG p550 (28.2.2) "Land unit movement is based on move-to, which is designed to take a direct path to the
  destination. The unit remains on the ground and does not automatically avoid features." UG p604 (30.28, unit Move
  To): "Each subordinate plans and traverses its own path to an 'offset destination', without consideration of the
  others." UG p549: at aggregate level "there is no formation movement; that exists only at the entity level."
- Re-task mid-move: REPLACE, never queue. Queuing exists only inside a plan (UG p554, p561). RN p27 VRF-8934 "Added logic
  to update ETA when move-to destination changes" shows a destination change on a running move is an expected path.
  No page describes re-tasking subordinates that are executing a superior's task, beyond the Skip Task note (p566).

OUR DESIGN
- A tasked container is driven per STP vertex by navigate-to-location sent to EACH member (PLAN_MOVEMENT_2026-09-27.md:81
  row M3, RL-20260928-03; CLOSED line :19); the vendor's user-level wrappers are Move (Group) / Move_To_Location_Plan_Path
  (FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:81-104). A successor is dispatched on the predecessor's completion
  (STREND), onto members that may still be moving (D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md:77-83).
- Our records already held the task-concurrency fact in part: CORRECTIONS_LOG.md:612 (a container has no move
  controller; every task the vendor's plans give one is scripted) and FINDING_AGGREGATE L112-114 (RunScriptedTask).

GAP
- The vendor's documented operator path tasks the CONTAINER (Move with Use Roads, or the Echelon Group scripts) and lets
  it compute member destinations; we task 17 members directly with a sub-script the PDFs do not name. Measurement: every
  stop of the back end followed a second navigate-to-location to the same 17 members (REPRO_BACKEND_STOP:36-42). Design
  implication: a re-task should be sent to the container through its own group task, or members should be stopped by
  the vendor's documented means before the new task, and that choice is untested on both sides.
- The docs give no "re-task while a member still plans" caveat; the interruption rule (replace) is documented only for
  the tasked object itself. This is a question for MAK (below).

## Q2 MODEL SETS / MODE

VENDOR
- UG p368 (13.7): aggregate-level simulations "focus on the high-level movement and interaction of large numbers of
  personnel and equipment in hierarchical units ... When you create a simulation object, you are usually creating a
  company, a brigade, or another object representing an entire group." p369: mixing the two SMS kinds "is not
  supported and not recommended."
- UG p1309 (68.3): "AggregateTacticalLevel.sms. Defines aggregate-level warfare units and other objects suitable for
  tactical-level scenarios using battalion-size simulation objects or smaller." MG p16 (2.2.1): "This SMS was designed
  specifically for tactical-level aggregate simulations." AggregateLevel.sms is "Legacy" (UG p1309, p119).
- UG p1419 (72.2.1): warfare-model objects "do not have the configuration options for specifying subordinates.
  Therefore, AggregateLevelBase.sms has a platform, Aggregate Container, for use when aggregating simulation objects
  ... If you want to create additional hierarchical units for aggregation, it is recommended that you use the
  Aggregate Container platform." It lives in the AGGREGATE base SMS: it is an aggregate-level construct, not entity-level.
- MC p613-614 (5.4 Echelon Group, AggregateTacticalLevel): the brigade/regiment entries are containers tagged PA (BDE
  Airborne PA, BDE Reconnaissance PA, RGT Tank PA, Artillery BDE PA (POL), Armor CAB Group (USA)); the simulated warfare
  units are BN/CO/BTY sized (5.2-5.3, 5.6-5.7). The catalog does not define "PA".
- How aggregates move: UG p531 (27.1.4) speed is scaled by slope, terrain class, roads, overlap; "Rivers and water
  bodies may stop a unit completely, although bridges allow them to cross"; "Aggregate-level scenarios do not support
  collision avoidance among simulation objects." UG p550: "the land unit simulation object does not avoid any 3D models
  of buildings. Aggregate-level movement is abstract, moving through the region. The MAK Earth Aggregate (online)
  terrain does not extrude buildings from their footprints." Per-entity path planning exists only through the Plan
  Along Roads tasks (p735) and group planned movement (p551).
- Road to Kaunas: FEG p20: "RoadToKaunas examples. Aggregate-level scenarios where the opposing forces attack toward
  Kaunas, Lithuania, and the friendly forces defend. There are in-scenario descriptions and quick launch events to
  demonstrate the aggregate-level warfare model." Nothing else in the nine PDFs names its units or tasks. RN p69
  VRF-8815: the Bakersfield aggregate samples moved to "MAK Earth Aggregate (Online)".

OUR DESIGN
- Two model sets by highest TASKED echelon, above BN aggregate-only (RL-20260927-06, RL-20260928-01; PLAN_MOVEMENT:9,
  :69-70); every unit an empty Aggregate Container at init, tasked ones populated in place (RL-20260927-02/-03;
  PLAN_MOVEMENT:7-8; DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md:62-70). Our records already held the Road to Kaunas
  facts from the shipped scenario files, not the PDFs: terrain + SMS (VRF_5.2_DECISION_EVIDENCE.md:125-128), 51 of 81
  simulated units inside 17 containers (PLAN_MOVEMENT:39), 0 of 335 warfare-model units with a child (DESIGN_AGG:69-70),
  its tasks move_to_waypoint_plan_path / group-attack-to-objective and routes on roads (FINDING_AGGREGATE:105-108,
  :133-143).

GAP
- The container-of-battalions shape matches the vendor's (UG p1419, MC p613-614): no design gap there.
- Measurement: in G1-2 eleven Mech COs stopped on a building footprint 9-11 m above ground (FINDING_AGGREGATE:32-39,
  :63-66). UG p550 says the Aggregate terrain does not extrude buildings and movement ignores building models; our
  record reads the Aggregate .earth as handing VRFSIM simplified building geometry (FINDING_AGGREGATE:52-62). The two
  disagree; this is a MAK question, not a design change.
- The vendor's planned aggregate movement is road-first (p551, p735); our Auto planner adds a MAK_OBSTACLE query and a
  NONE path query off roads (PLAN_MOVEMENT:19) that no PDF page describes. The off-road frame is a dissent-logged
  ruling (FINDING_AGGREGATE:175-180), not reopened here.

## Q3 TERRAIN

VENDOR
- UG p1246-1247 (63.6): "VR-Forces cannot arbitrarily page or stream terrains"; "Even if you use terrain paging or
  streaming terrain, it is still possible that a large terrain could exceed the memory limitations of your operating
  system." p1247: "The simulation engine always loads terrain at the highest level of detail" while the GUI loads at
  the zoom level; "the simulation engine does not load terrain until it needs the terrain to support the objects";
  predictive paging as objects move; page-out "based on the polygon count of the terrain, not on its memory usage".
  Priorities: a moving object's need > predictive; blocking calls > non-blocking; page-out to recover memory highest.
- UG p1248 (63.6.2): a terrain server "can stream elevation data, imagery, map data, and feature data." AC p155:
  "Only the tiles for the current view ... are accessed. As tiles are streamed from the terrain server, they are
  cached locally."
- AC p156 (3.3 Manually Paging In Terrain): predictive paging can fail "when there are many simulation objects over a
  wide area or many fast-moving simulation objects"; page-in areas may block or not; "Terrain page-in areas are
  supported for paged IVE files and supported versions of the MetaFlight format"; "only affect the sim engine."
  RN p9: the (no vegetation) variants. RN p15 VRF-8054: the application itself "creates a terrain page-in area around
  the unit" when a unit on paged terrain changes heading.
- AC p239 (8.1.2 Caching earth File Terrain Data): "You can generate cached imagery and elevation and data through the
  GUI. (Feature data is not cached.)" "It can take a very long time to generate the cache for large areas."
  VRFSIM_OSGEARTH_CACHE_PATH names the sim engine's cache (p239). AC p241 (8.2): OSG_MAX_PAGEDLOD default 200, lower
  "to reduce memory consumption when using paged terrains. (This variable only affects the GUI.)"; GDAL_CACHEMAX
  default 200 MB. AC p269-270 Table 36: sim console commands features info / features pageout / pageout / terrain save.
- UG p1668-1671 Table 76 (vrfSim.mtl): assertOnBlockingTerrainCalls ("Blocking terrain calls can cause long ticks");
  blockOnAsynchronousOperations (fixed-frame only: "block on asynchronous operations (for example, terrain, feature,
  path planning, and navigation operations)"); maxAsynchronousTerrainThreads default 7; loadAllNavigationDataOnTerrainLoad;
  gamewareMemorySize. UG p181: --fullyLoadTerrain "For paged terrains, load all pages." RN p21 VRF-8490: new
  terrainInterfaceConfig.mtl entry min-dynamic-elevation-lod.
- Disk [V] C:\MAK\vrforces5.2d\appData\settings\vrfSim\terrainInterfaceConfig.mtl (no PDF page lists these):
  max-unused-Features 500000 (:11), max-feature-network-threads 4 (:15), area-page-table-threads 4 (:19),
  features-lowest/highest-load-LOD 9/15 (:25,:31), pathfinder-max-features 50000 (:69), pathfinder-load-timeout 20 s
  (:81, "the path finder will wait for a grid cell to load"), pathfinder-max-disjoint-networks 3 (:121, default 0;
  comment: disjoint networks can make "the planner thrash around the area"), num-triangles-threshold 1e8 (:127),
  page-in/out priorities (:144-156), features-min-unload-time 15 (:188, commented), sim-cache-path default
  $(APP_DIR)/cache/vrfsim (:244), per-query subconfigurations example MAK_OBSTACLE features-lowest-load-LOD 12
  (:251-255), nb-terrain-thread 1 (:289), procedural-sim-vegetation-enabled True (:306), terrain-category-mask-override
  (:315).
- UG p197-198: thread counts should sum to ~2 less than the logical CPUs; "The terrain paging thread and the main
  thread are in addition"; with the GUI on the same hardware "leave at least 2 CPUs"; "run only one simulation engine
  per computer". UG p221 (6.17): per-platform terrain category masks let VR-Forces "avoid paging in vegetation data"
  and make "terrain paging much faster".
- Hardware / memory: RN p2 points to the website for hardware requirements; no RAM figure exists in the nine PDFs.
  UG p199: GUI performance depends on "Available memory" and "Size of the terrain database". UG p1661 A.4 is Linux only.
- Does aggregate movement need feature data: yes. UG p550 (28.1): "Aggregate-level movement systems use modifiers based
  on vector data in the terrain. These are area features from geospatial information, such as land use or municipal
  boundaries." Plan Along Roads needs the road features (p735). A plain Move does not avoid features (p550).

OUR DESIGN
- MAK Earth (online) / Aggregate (online) streaming terrain, offline profiles ruled (Y-7, VRF_5.2_DECISION_EVIDENCE.md:
  107-147, :315-327 - "FEATURES ARE NOT CACHED" already recorded); the demo extent bound (RL-20261005-02;
  SCOPE_DEMO_EXTENT_2026-10-14.md:33-42, which already cites AC p155-156 and notes page-in areas are documented for
  IVE/MetaFlight only); blockOnAsynchronousOperations as the FFRTC knob (Y-9; VRF_5.2_MIGRATION_DIFF.md:67).
  Measurement: the unbounded order backed feature_source jobs up to minutes (median 71-114 s) and the working set
  climbed 2.6 -> 14.3 GB (D36:87-117). Design implication: the extent bound stays the documented lever.

GAP
- Nothing in the PDFs bounds the FEATURE pager's memory except the terrainInterfaceConfig.mtl knobs above
  (max-unused-Features, pathfinder-max-features, per-query LOD subconfigurations), which are undocumented in the PDF
  set and untouched by us. Measurement: the backlog lines were feature_source / MAK_OBSTACLE queue waits (D36:92-93).
  Design implication: these knobs are the first documented-on-disk candidates to raise with MAK before any probe.
- The warm cache cannot remove the feature backlog by the vendor's own statement (AC p239 "Feature data is not
  cached"); D36 (b) measured exactly that (D36:113-116). Our record had already drawn the conclusion.

## Q4 HEADLESS / REMOTE VIEW

VENDOR
- UG p114 (3.1): "VR-Forces allows you to run multiple GUIs, sim engines, or both ... Using session IDs, you can specify
  which GUIs control which sim engines." UG p130: "Start vrfSim only: vrfLauncher --sim". UG p133 (4.1.2): command line
  "vrfGui --siteId 1 --appNumber 3000 {--dis | --hla1516e | --hla4}" and "vrfSimprotocol --siteId 1 --appNumber 3001".
- UG p115 (3.2.3): "Sim engines are always part of a session. GUIs can join a session or operate independently of one.
  When a GUI is joined to a session, it can control the sim engines in that session ... [not joined] it can open a
  terrain and view exercises running on its port. In this case, it is simply a viewer." UG p139 (4.3): "By default,
  when you start a GUI, it automatically tries to join a session. If there is no session running, it polls the network
  until one becomes available and then automatically joins it." p116: "When a GUI joins a session that is running a
  scenario, it automatically loads the correct terrain for the scenario." p133: "The default session ID is 1. You
  cannot change the session ID during runtime."
- Another machine: UG p115 "If you decide to run multiple sim engines, you should run them on separate machines";
  p255 "Start multiple sim engines on separate machines"; UG p1674 reportedHostAddressString covers "a GUI or remote
  controller on one side of a router and a simulation engine on the other". UG p116: sessions "allow you to load
  different terrain databases in different VR-Forces executables that are part of the same exercise ... Loading
  databases that cover a subset of the total exercise zone could improve the performance of your computer".
- Back-end-only limits: tasks come from plans, the Task menu or the Command Panel (p554), i.e. the GUI, or from a
  remote controller (UG p1669 cgfDispatcherReceivePort "DtVrfRemoteController"); the osgEarth cache is generated in
  the GUI (AC p239); page-in areas are created from the palette or plans (AC p156); nav data is generated in the GUI
  (UG p1281-1287, INDEX.md:46-58). Sim engine options: --doNotUseConsole quiet mode (p180, "sending output to the
  console can degrade performance"), --daemon is Linux only (p180), startMinimized (p1675), --frontEndPID is for
  combined mode (p181). UG p118: all sim engines "must be started and ready when a scenario is loaded"; a dropped
  engine has "no good way ... to rejoin".
- RN p40 VRF-9853: a front end replaying an aggregate-level tape "must be started with the --defaultSimulationModeSet
  ..\data\simulationModelSets\AggregateLevel (or AggregateTacticalLevel).sms in order to view the data correctly".

OUR DESIGN
- Headless mandate and direct bin64 launch (HEADLESS_RUN_PLAN.md:7-13; VRF_5.2_DECISION_EVIDENCE.md:34-41;
  VRF_5.2_MIGRATION_DIFF.md:126-129 "INDEPENDENT mode ... combined mode needs a GUI-saved connection"). The demo adds a
  local vrfGui that "simply JOINS" (DEMO_RUNBOOK.md:166-171); Way A headless PASSED, with the GUI UNVERIFIED
  (DEMO_RUNBOOK.md:21-22); the GUI clock reads 1x while the sim runs 3-7x (DEMO_RUNBOOK.md:353).

GAP
- Measurement (today's run, per the brief): vrfGui ~4.1 GiB and the back end ~3.2 GiB working set before a free-memory
  stop. Vendor fact: the GUI's terrain LOD follows the zoom level (UG p1247) and OSG_MAX_PAGEDLOD is a GUI-only memory
  lever (AC p241). Design implication: the GUI is the larger, separately tunable consumer, and a viewer GUI on a second
  machine joining the session is a documented topology (UG p115-116, p139, p1674) that our runbook does not cover.
  Requirements the docs state: same exercise connection / federation (MAK-ONE config, Y-2), the session ID (default 1),
  the terrain available to that machine (MAK Earth needs internet, AC p154), and the aggregate SMS for correct display.

## Q5 C2SIM

VENDOR: "C2SIM" has 0 hits in all nine text extracts (UG, RN, MG, FEG, QRC, Entity Catalog, Model Catalog, IOG, AC)
and 0 files under C:\MAK\vrforces5.2d\include. What IS documented: MSDL import (UG p1672 msdlSIDCMappingFile "when it
imports MSDL files"), the NETN FOM (IOG lines 796, 990: "The object model used in MAK ONE products is based on the NATO
Education and Training" FOM), and the remote-controller API (UG p1669). A grep of the HTML help under
C:\MAK\vrforces5.2d\doc\help\Content timed out and was NOT completed.
OUR DESIGN: the interface is a DtVrfRemoteController federate (MAK_BACKEND_HANG_2026-10-15.md:22); IOG lists
NETN_Aggregate for Callsign/UniqueId only, no MRM interaction (VRF_5.2_DECISION_EVIDENCE.md:352-354).
GAP: none. No built-in C2SIM support or plug-in is documented for 5.2 / MAK ONE 2025.

## Q6 KNOWN ISSUES (RN + MG)

- RN p84 "Known Problems": no list; it points to https://www.mak.com/knowledge and the UG Troubleshooting appendix.
- Back end stopping/hanging on a re-task: NO item. Nearest: RN p35 VRF-9288 "Sim engine crash when using nonexistent
  route in Patrol Route task" (fixed: route validity check); RN p42 VRF-10051 "Crash when disabling move-to
  controller"; RN p40 VRF-9814 "Debug Simulation Terrain Tool can hang the sim engine when using draw areas larger than
  the default 1000 m"; RN p32 VRF-9147 "Issue in Aggregate-Level scripts for higher-echelon units" (entityDestroyed()
  check). None matches the measured signature (log stops within 0.5 s, threads spin, flat memory - REPRO:36-42).
- Memory growth: RN p23 VRF-8783 "Large memory consumption over time - Improved intersections with trees to resolve
  this problem" (fixed in 5.2). No open item.
- Terrain paging: RN p21 VRF-8490 (min-dynamic-elevation-lod); RN p24 VRF-8809 GUI hangs creating entities on CDB
  terrains (fixed); RN p9 page-in areas without vegetation; RN p15 VRF-8054 (internal page-in area on heading change).
- Aggregate movement: RN p27 VRF-8934 (ETA on destination change); RN p16 VRF-8199 aggregate detection with larger
  footprints; RN p69 VRF-8815 samples moved to MAK Earth Aggregate. MG: nothing beyond the AggregateTacticalLevel
  introduction (p16) and the ground movement redesign (p18-20, already in our DIFF rows D1-D7).

## RANKED DESIGN CHANGES THE DOCS SUPPORT (each with a vendor page and an own-record line)

1. Re-task the CONTAINER through a vendor group task instead of re-sending navigate-to-location to 17 members.
   Vendor: UG p551 (group planned movement computes member destinations itself), UG p561 (a new movement task replaces
   the current one). Own: PLAN_MOVEMENT:81 (M3 sends per-member navigate-to-location), REPRO_BACKEND_STOP:36-42 (every
   stop is a second per-member navigate). Measurement: 5 of 6 re-tasks at that place stopped the back end. Implication:
   the group-task path is the vendor's documented operator path and has not been tried on the stop recipe.
2. Hold the demo extent and raise the FEATURE pager knobs with MAK rather than widening. Vendor: AC p239 "Feature data
   is not cached"; terrainInterfaceConfig.mtl :11, :69, :251-255 (max-unused-Features, pathfinder-max-features,
   per-query LOD). Own: SCOPE_DEMO_EXTENT:33-42, D36:113-117 (warm cache did not remove the backlog).
3. Move the audience GUI to a second machine as a session viewer, or cap its paging. Vendor: UG p115-116, p139
   (auto-join, viewer mode), AC p241 (OSG_MAX_PAGEDLOD, GUI only), UG p1247 (GUI LOD follows zoom). Own:
   DEMO_RUNBOOK.md:21-22, :166-171 (GUI-on path UNVERIFIED, GUI only joins). Measurement: GUI 4.1 GiB vs back end 3.2
   GiB today. Implication: the host's free memory is governed more by the GUI than by the sim.
4. Record the terrainInterfaceConfig.mtl defaults in VRF_5.2_MIGRATION_DIFF row C5 / DECISION_EVIDENCE Y-9 as the
   documented-on-disk paging bounds. Vendor: the file's own comments (:7-11, :66-69, :78-81, :123-127); RN p21.
   Own: VRF_5.2_MIGRATION_DIFF.md:67 (row C5 names only blockOnAsynchronousOperations and maxAsynchronousTerrainThreads).
5. Add the vendor's own stop rule to any re-task: a new movement task to an object abandons its current one (UG p561),
   so a "stop then re-task" sequence is two interruptions, not one. Own: D36:159-160 already warns against stopping
   first; keep that warning, now cited.
No probe is proposed here; items 1-3 each need an owner RULE and a PREREG with the citations above before any run.

## WHAT THE DOCS DO NOT ANSWER (for MAK)

1. Whether navigate-to-location is a supported task for a remote controller to send directly to an aggregate's
   subordinates, and what happens when it is re-sent while a member's planner (graph search / obstacle avoidance
   phase) is still running. The stop signature (REPRO:36-42) has no match in RN or MG.
2. What the "PA" suffix on the Echelon Group container types means (MC p613-614) and whether a PA container is the
   intended target of Move (Group) / group planned movement for brigade echelons.
3. Whether the MAK Earth Aggregate (online) terrain gives the SIM ENGINE building geometry. UG p550 says buildings are
   not extruded and not avoided; our trace shows aggregates standing 9-11 m above ground at a footprint
   (FINDING_AGGREGATE:32-39).
4. Memory and hardware: no RAM figure in the PDFs (RN p2 defers to the website); what bounds the feature pager's memory
   on a streaming terrain (max-unused-Features, pathfinder-max-features), and whether page-in areas or any pre-caching
   apply to streamed FEATURE data (AC p156 names IVE / MetaFlight only; AC p239 excludes features from the cache).
5. Whether a sim engine can run with the GUI on another host for an HLA 4 / MAK RTI 5.0.1 federation with the terrain
   streamed to both, and what the session needs when the GUI's machine has a different cache (UG p116 allows different
   databases per executable; the correlation warning on p141).
6. Why terrain-profile (elevation) queries time out at init on the online terrain (our measurement, CORRECTIONS_LOG
   .md:142); the only documented timeout is pathfinder-load-timeout 20 s (terrainInterfaceConfig.mtl :81), which is
   the planner's, not the elevation query's.

Adversarial review: competing reading for Q1 - that the per-member navigate IS the vendor's intended API path (the
Plan Along Roads task wraps it, FINDING_AGGREGATE:95-101); not falsified by the PDFs, which never name the script, so
item 1 is ranked as "documented operator path untried", not as a cause claim. For Q3 the competing reading is that the
backlog is network fetch, not processing; AC p239 (features not cached) plus D36's warm-vs-warmer result weigh against
fetch but do not settle it (D36:113-116 says so). Unexplained and left so: the building-geometry disagreement (Q2 GAP)
and the init elevation timeouts (MAK item 6). Verified = every [V] citation above was read at the page/line given;
assumed = the GUI/back-end memory split comes from the brief's context line, not from a file I read.
