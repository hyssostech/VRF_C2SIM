# ROADS / ROUTES: does VR-Forces 5.2d need roads or densely authored routes for ground navigation? (docs-only, 2026-10-20; STP-1296 deliverable 1)

Read-only; no run, no probe. Fable cold review 2026-10-20: SURVIVES NO as first written; its 8 findings are applied (REQUIRED* cells -> NOT DOCUMENTED + [A]; the 200 m deviation box added; F6 -> dissent line; cites fixed). SOURCE CAVEAT: docs/vendor/mak-5.2/txt is git-ignored and ABSENT in this
checkout (only INDEX.md + README.txt exist), so no vendor PDF was opened here. Every [V] below is a vendor page / script
line AS RECORDED in an own-record file, cited as "[V] <vendor cite> via <record>:<line>" - the record line was opened; the
vendor page was not re-opened in this session. [O] = our measurement of a vendor artefact. [A] = inference. The vendor
sample guide PDF is Company Confidential: cited by nothing here. Measurement and implication are kept in separate sentences.

## 1. Table - level x task -> roads needed? / densely authored (obstacle-clear) route needed?

| level | task | ROADS | DENSE/CLEAR ROUTE | citation |
|---|---|---|---|---|
| ENTITY | Move To (ground-vehicle-move-to; = Move to Location/Waypoint since 5.2, MG Table 1) | NOT REQUIRED | NOT REQUIRED | [V] UG52 23.2.1 p500-501: roads if road driving desired, else nav mesh, else "planned around obstacles using feature path planning" via NAVMESH_QUERY_DOCS_2026-09-14.md:182-195; [V] MG 2.4 p18 "If there is no navigation mesh ... accounts for feature obstacles and plans" via VRF_5.2_COLD_START_ROADMAP.md:135-138; [V] help EntityMovement.htm "generic Move To ... moves directly ... If no navigation data is available, the entity avoids obstacles" via VRF_GROUND_TRUTH.md:845-850; armour default Ignore Roads [V] ground-tracked.sysdef:132, MG 2.4 p18 via VRF_5.2_DECISION_EVIDENCE.md:274-279. Roads at all: UG 23.2.4 p503 "first use the Navigation Preferences set data request ... Prefer Roads" via DOCSWEEP_GROUND_STALLS_2026-09-07.md:140-141; with the Y-13 Ignore Roads default, armour on Move To uses no roads unless that set-data is sent. Caveat: an UNREACHABLE destination (in water) aborts with PathPlanFailure [V] ground-vehicle-move-to.lua:1401-1404, :1423 via FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27.md:156-159 |
| ENTITY | Move Along Route (entity, UG52 30.24 / 23.3) | NOT REQUIRED ("does not use road movement") | NOT DOCUMENTED. [A] Because the task plans nothing between vertices, each straight leg must clear MAK_OBSTACLE and water. | [V] UG52 23.1 p500 "does not use road movement" via DOCSWEEP_GROUND_STALLS_2026-09-07.md:48, VRF_5.2_MIGRATION_DIFF.md:74; [V] UG52 23.3 p505 "There is no path planning done as it moves toward the next vertex", 23.2.3 p502 "could become trapped" via FINDING_GROUND:44-47. No spacing figure is documented. Counter-case [O]: T02 arrived on the literal entity task over 5.3 km of open ground (FINDING_GROUND:101). Option "Treat Route as Road" needs the entity within 10 m [V] RouteMoveAlong.htm via VRF_GROUND_TRUTH.md:294-300 |
| ENTITY | Move Along Route (UNIT -> Maneuver Along) | NOT REQUIRED (off-road preference) | NOT REQUIRED (members plan to each vertex) | [V] UG52 30.22 p598 "traverses it by planning a path to each vertex in sequence", 30.24 p600 via FINDING_GROUND:52-54; members' movement systems Ignore Roads [V] Y-13 via VRF_5.2_DECISION_EVIDENCE.md:274-279; route cap 2000 points [V] RN p14 via VRF_5.2_MIGRATION_DIFF.md:69 (a ceiling, not a density) |
| ENTITY | Move to Location / Waypoint (Plan Along Roads) | REQUIRED (and the network must be edge-connected) | NOT REQUIRED (destination task) | [V] help EntityMovement.htm, LocationPlanAlongRoads.htm via VRF_GROUND_TRUTH.md:838-844, :851-854, :861-865 |
| ENTITY | Move to Location (Plan Path) | n/a | n/a | [V] surface/subsurface only, S-57 depth data via VRF_GROUND_TRUTH.md:313-315 |
| AGG | Move Along Route (35.5.7 -> 30.24; PA_Move_Along_Route for a container) | NOT REQUIRED (road = speed factor 1, "not a router") | NOT DOCUMENTED. [A] No planning and no feature avoidance, so legs must clear water and building models. |  [V] UG52 27.1.4 p531 "Rivers and water bodies may stop a unit completely", "do not support collision avoidance" via FINDING_GROUND:70-73; [V] sysdef :112-131 mobility table, "There is no feature-avoidance setting for aggregates" via FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:118-124, FINDING_GROUND:183-194; spacing undocumented. Counter-case [O]: T10 arrived on literal PA_Move_Along_Route in G1-2 (PLAN_MOVEMENT_2026-09-27.md:78) |
| AGG | Move To (35.5.9 -> 30.28) / unit Move To | NOT REQUIRED | n/a (straight line) | [V] UG p550 28.2.2 "take a direct path ... does not automatically avoid features" via FINDING_VENDOR_DOCS_VD1_2026-10-16.md:46-48; what it does at impassable cells: NOT DOCUMENTED [V] FINDING_AGG:109-111 |
| AGG | Move to Location / Waypoint (Plan Along Roads) (35.5.11-12 p735; Move_To_Location_Plan_Path) | [A] REQUIRED by the task's description (p735 describes travel to the nearest road, along the network, off-road only at the ends); what it does with NO road network is NOT DOCUMENTED | NOT REQUIRED | [V] UG p735 via VD-1:38-41; script passes obstacleQuery MAK_OBSTACLE + pathQuery MAK_ROAD, buffer 0 [V] .lua:20-45 via FINDING_AGG:95-98, PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md:70-71 |
| AGG | navigate-to-location (base system subtask; our Auto/PerMemberOffRoad) | NOT REQUIRED (pathQuery may be NONE; empty -> MAK_ROAD) | NOT REQUIRED (plans round MAK_OBSTACLE with a buffer) | [V] navigate-to-location.lua:60-63, .xml, featureconfig.txt:88 via FINDING_AGG:98-101, :115-117; "not designed for end user use" [V] .xml:17-18 via PREREG_L1_L2:21. NONE branch: documented parameter, behaviour UNOBSERVED (FINDING_AGG:117, :191) |
| AGG | Move (Group) (group_movement_simplified) | NOT REQUIRED (useRoads default ON; OFF = straight move-to-location per member, no planning) | n/a | [V] .xml:70-74, .lua:28-77 via FINDING_AGG:81-85; [V] UG p551 28.3.2 "can move directly ... or they can plan along roads" via VD-1:42-45; needs ground-echelon-group-tactics-enabler, absent on the bare Aggregate Container [V] PseudoAggregate.ope:40, :115-119; enabler.sysdef:16-22 via PREREG_STP1296_GROUP_TASK_2026-10-19.md:61-65, :122-125 |
| AGG | Group Navigate Route To Location | REQUIRED as scripted (pathQuery MAK_ROAD, no obstacleQuery) | n/a (makes its own route, drives it with PA_Move_Along_Route) | [V] .lua:55-108 via FINDING_AGG:91-94 |
| AGG | group-attack-to-objective (container) | NOT DOCUMENTED (:533 sends subordinates PA_Move_Along_Route; that it drives the AUTHORED route is [A] from the task's "route (simulationobject)" parameter) | NOT DOCUMENTED. [A] As AGG Move Along Route. | [V] group-attack-to-objective.lua:533 via DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md:21; parameters via LD-1296-R1_BOX_READ_2026-10-20.md:32-33 |
| AGG | unit-attack-to-objective / unit-trailing-march-to-objective | NOT DOCUMENTED (approach to a route >500 m away uses the planner; its pathQuery is not in our record) | NOT DOCUMENTED. [A] As AGG Move Along Route, for the route leg (a move-along). | [V] unit-attack-to-objective.lua:2-4 "Execute a move-along" via TASK_COMPLETION_RESEARCH_2026-09-21.md:127-129; :54, :510-548 and unit-trailing-march:263-300 via FINDING_AGG:106-108 |
| AGG | unit Move (Use Roads option; unit_movement_simplified) | NOT REQUIRED (Use Roads is an option) | n/a | [V] UG p734 via FINDING_VENDOR_DOCS_VD1_2026-10-16.md:38; unit_movement_simplified.lua:74-80 via FINDING_AGG:104; TASK_VOCABULARY_ASSESSMENT_2026-09-14.md:494 |
| AGG | PA_Move_To_Location_Direct | NOT REQUIRED | n/a (straight move-to-location per member) | [V] .lua:34-50 via FINDING_AGG:79-80 |
| BOTH | nav mesh (navigation data) | not a road question; NOT REQUIRED for Move To (fallback above); nav area cap 20 x 20 km at default precision, mesh does not span areas | | [V] UG52 66.2 p1276, Table 54 p1279, 66.3 p1281 via PREREG_NAVDATA_G6_2026-09-13.md:18-26 |
| BOTH | max leg length / planner search radius | No LENGTH bound is documented. A DEVIATION bound is: the nav-mesh flat A* query searches a 200 m box around the straight start-to-goal line, "no path will deviate more than 200 meters" [V] Autodesk AStarQuery doc, DtNavBot myPropagationBoxExtent(200.), Ground_Vehicle.ope propagation-box-extent, via NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE_2026-09-14.md:41-49, :789; NOT a knob we hold: ground vehicles build no NavBot, so the .ope line is never read (NAVDOCS:53-58); when the mesh query fails, UG 23.2.1 falls back to the feature planner (NAVMESH_QUERY_DOCS:189-195). Feature planner: terrainInterfaceConfig.mtl pathfinder-min-search-radius 1000 m, pathfinder-load-timeout 20 s, pathfinder-max-features 50000 (file comments only) | | [V] terrainInterfaceConfig.mtl:69, :81, :98-101 via KNOB_PREP_2026-10-20.md:22-26, LD-1296-R4:44-45, VD-1:145-149 |

Aggregate movement as a whole: "Aggregate-level movement systems use modifiers based on vector data in the terrain" [V] UG p550 28.1 via
VD-1:160-162 - FEATURE data is required for the mobility model; roads enter it only as the speed-factor-1 class (AC 7.8 p234-236 via
INDEX.md:39-42, FINDING_AGG:66, :141). Vendor sample [O]: 57 ground routes, median vertex spacing 52 m, maneuver routes 100% within 10 m of OSM roads,
0 footprint crossings (FINDING_AGG:133-143). That is what the vendor DID on roads-first tasks; no page says it is REQUIRED.

## 2. What each finding KILLS and ENABLES

F1 Entity Move To needs neither roads nor a nav mesh (table row 1).
  KILLS: "generate nav data / road-trace STP routes before an entity-level run" (already dead: VRF_5.2_PLAN_RECONCILIATION_2026-09-04.md:53-64). KILLS "a Move To on an STP vertex hands route choice to the planner" as an objection (DECISION_EVIDENCE Y-10:235-241).
  ENABLES: sparse STP vertices as Move To destinations (M1, live E1/E2-2: lone M577A2 arrived 6.9 m, PLAN_MOVEMENT_2026-09-27.md:58, :60).
  Separate implication: a vertex in water must be checked BEFORE dispatch, because the planner aborts rather than re-sites it (M2).
F2 Entity Move Along Route plans nothing and may trap (row 2).
  KILLS: "better pre-flight data makes the literal task safe at scale" (the competing hypothesis of FINDING_GROUND:167-172); KILLS
  "dense routes are a vendor requirement" - the doc gives no spacing; it gives no planning, which is a different fact.
  ENABLES: route shift / corridor only as the fallback for legs still driven literally (PLAN_MOVEMENT:128-131).
F3 Unit Move Along Route plans per vertex off-road (row 3).
  KILLS: "units need roads or dense vertices". ENABLES: units stay on Move Along Route with STP's vertices (T10 arrived through lakes and
  forest, FINDING_GROUND:102; Y-11/Y-13 untouched, RULINGS_ARCHIVE.md:569).
F4 Aggregate movement is abstract: no feature avoidance, water stops, speed by terrain class (rows 6-7).
  KILLS: "aggregates ignore buildings so the literal route is safe" (FINDING_GROUND 5.1, REFUTED by G1-2: 11 Mech COs stopped on a building
  model, FINDING_AGG:41-66). Measurement: UG p550 says the Aggregate terrain does not extrude buildings; the trace shows units 9-11 m up
  (VD-1:110-113). Implication: that disagreement is a MAK question, not a design input. ENABLES: a per-vertex PLANNING task for containers
  (M3, RL-20260928-03) and a pre-flight reading the same OSM classes the sim reads (water/building; M2, W1).
F5 The aggregate planning tasks are road-first by default, but off-road planning is a documented parameter (rows 8-10).
  KILLS: "roads are REQUIRED for aggregate planned movement" - Plan Along Roads needs them by definition, navigate-to-location does not
  (pathQuery NONE), Move (Group) useRoads is an option. KILLS "snap aggregate routes to roads" as a remedy (CORRECTIONS_LOG.md:646-657, owner).
  ENABLES: Auto's per-leg road decision (MAK_ROAD within 500 m else NONE). Not enabled: a NONE-branch claim - no run has exercised it
  (every cut-A leg decided NEAR -> MAK_ROAD, PLAN_MOVEMENT:84-85; G1-6 arrived on that branch, PREREG_IRONSTORM_AGG_G1-6_2026-10-04.md:274-279).
F6 The vendor sample authors routes on roads at ~52 m spacing [O].
  KILLS nothing on its own (practice, not a requirement). ENABLES nothing new: road-tracing STP routes was struck by the owner
  (CORRECTIONS_LOG.md:646-657) and the road question is settled per leg by RL-20260928-03 (RULINGS_ARCHIVE.md:558-570).
  Dissent line only: would reopen if the owner admits the aggregate planners with Use Roads (FINDING_AGG:178-180).
F7 Move (Group) / group-attack need the enabler sysdef the bare container lacks (row 9).
  KILLS: "Group mode on our brigade containers as built" (PREREG_L1_L2:98-100). ENABLES: an enabler-carrying PA type (G0a, 41 of 79 types).
F8 No documented max leg LENGTH (NAVDOCS:789: the vendor documents a deviation limit, not a length limit). The documented numbers are the
  nav-mesh 200 m deviation box (entity Move To on a nav mesh), pathfinder-min-search-radius 1000 m and the 20 km nav-area cap (rows 15-16).
  KILLS: "a documented leg-length cap explains the stops". ENABLES: nothing for aggregates - a gap (sec 3); for entity Move To on a nav
  mesh, the 200 m box is a documented reason a far-from-chord path is not found by the mesh query.

## 3. Gaps - not documented, and what our box measured

- Off-road aggregate plan (pathQuery NONE): unobserved on 5.2 (FINDING_AGG:191-192). All G1-3..G1-6 legs ran MAK_ROAD; G1-6 scored
  T02/T10/T14 ARRIVED 0.9-2.1 m, 79 PASS / 0 FAIL (HANDOFF_SEAT_2026-09-28.md:116-119; G1-6 PREREG:226-227, :274-279).
- Aggregate Move To / Move Along Route at speed factor 0: doc silent; G1-2 measured the move runs on with no completion or failure and
  the watchdog closes it (PLAN_MOVEMENT:142-146). Buildings at aggregate level: doc vs trace disagree (VD-1:278-280).
- Mobility priority where road and river overlap (culvert): NOT DOCUMENTED: the manual was read and is silent on priority resolution (G1-5 PREREG:700-703); G1-5/G1-6 measured members crossing on the
  road way (G1-5 PREREG:684-688; G1-6 W gate PASS under the culvert rule).
- Start or goal inside MAK_OBSTACLE: the vendor's off-road planning failure line fires (counts only): L2 Auto 41 vs PlanPath 8; LD-21b put 4
  offset goals in OSM water (L2_RESULT_2026-10-16.md:142-171). Forest as a planner obstacle: consistent with NOT (L2:169-171), ruling owed.
- Re-task of planning members: NOT DOCUMENTED (VD-1:68-69, :273-275). REPRO_BACKEND_STOP: 5 of 6 Auto re-tasks at one place stopped the back
  end (:36-42); L2 B1/B2 PlanPath clean at the same place, A1 Auto stopped (L2:43-48); D47 G1: 0 members outstanding, 0 in-obstacle lines,
  still stopped (D47:43-56). Place-dependent; cause OPEN; MAK case drafted.
- Move (Group) live on a container: never run (PREREG_STP1296_GROUP_TASK_2026-10-19.md:130-133, G0b; STP-1296 arm G not run).
  group-attack-to-objective from the interface: never issued (no list variable is marshalled, VrfFacade.cpp:169-192 via FINDING_AGG:112-114).
- Entity level with NO nav area in the AO on 5.2 (feature planner only) for a lone platform on Move To: not a scored observation (E1/E2-2 ran on
  the maple nav area, PLAN_MOVEMENT:154; E1 noted the destination judged outside the nav area, :58). Vertex k -> k+1 continuation seen once.
- Max leg length: no vendor number; our longest STP route is 42.37 km (CORRECTIONS_LOG:579); which task it ran on is not recorded.

## 4. Sources (all opened this session; vendor cites are as recorded in them)

docs/vendor/README.txt, docs/vendor/mak-5.2/INDEX.md (txt/ absent); docs/experiments/FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27.md;
FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md; FINDING_VENDOR_DOCS_VD1_2026-10-16.md; stp1296/KNOB_PREP,
LD-1296-R1..R4 (2026-10-20); docs/VRF_5.2_MIGRATION_DIFF.md (C4, C8, D1-D7); VRF_5.2_DECISION_EVIDENCE.md:14-25, :125-140, :205-298, :329-349;
RULINGS.md; RULINGS_ARCHIVE.md:441-453, :558-570; PLAN_MOVEMENT_2026-09-27.md (CLOSED list, rows M1-M3b, G1-6, secs 3-6); CORRECTIONS_LOG.md:
604-657; DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md:196-211; VRF_GROUND_TRUTH.md:272-329, :833-871; VRF_5.2_COLD_START_ROADMAP.md:125-141;
VRF_5.2_PLAN_RECONCILIATION_2026-09-04.md:44-88; experiments/NAVMESH_QUERY_DOCS_2026-09-14.md:1-60, :170-205; NAVDOCS_ABSTRACT_GRAPHS_AND_
SLOPE_2026-09-14.md:1-58, :789; DOCSWEEP_GROUND_STALLS_2026-09-07.md:1-60; PREREG_NAVDATA_G6_2026-09-13.md:18-40; PREREG_L1_L2_DOCUMENTED_MOVE_
2026-10-15.md:14-113; PREREG_L1_L2_L5_DOCS_ADDENDUM_2026-10-15.md:20-48; PREREG_IRONSTORM_AGG_G1-5_2026-10-04.md:50-66, :680-701;
PREREG_IRONSTORM_AGG_G1-6_2026-10-04.md (grep: 163, 224-287); PREREG_STP1296_GROUP_TASK_2026-10-19.md:14-73, :118-143;
TASK_COMPLETION_RESEARCH_2026-09-21.md:118-137; REPRO_BACKEND_STOP_2026-10-15.md; L2_RESULT_2026-10-16.md; D47_NAVGATE_LIVE_AND_D16_PRO_
2026-10-15.md; HANDOFF_PARK_2026-10-16.md; HANDOFF_SEAT_2026-09-28.md:114-124; D36 (grep only, :74-153).
