# VRF MECHANISMS RESEARCH (2026-09-01) - what the vendor material says, what our own
# artifacts say, and the two freezes re-diagnosed from primary sources

User directive (2026-09-01): "hit the books" - identify the ACTUAL mechanisms VR-Forces
expects for creating and tasking units, instead of reverse-engineering behaviour via probes.
Research-only pass; NO live time, NO appNo consumed, NO code changed. ASCII only. Every
load-bearing claim carries a file:line or a run artifact. Tier: HEAVY (cause claims) - the
falsification gate is in sec 4 and the adversarial review in sec 8.

## 0. TL;DR

1. The vendor model of unit tasking is now read from the shipped headers, help pages, model
   set and MAK's own remote-control example (sec 2). The port's call shape (createAggregate
   with createSubordinates -> createRoute -> moveAlongRoute(unitUuid)) matches it. The
   "how do you task a unit" question is NOT open; the open questions are two specific
   failure mechanisms, and both now have concrete, cheap discriminators.
2. NEW EVIDENCE nobody had read: the simulator's own log, C:\MAK\vrforces5.0.2\bin64\
   vrfSim.log, still holds RUN 3 (mtime 2026-07-23 14:14). It shows (a) a stock MAK CONTENT
   DEFECT firing at every Tank Company (USA) creation: the company's own formation files
   assign its HQ section a formation name the HQ section template does not define ->
   "AR HQ Sec 1: Aggregate state has invalid formation name \"column-left\""; (b) ZERO
   working/offset routes ever created for the company tree, versus 4 for the platoon that
   moved; (c) NOTHING logged for the frozen entity. The sim was configured to log
   warnings only (vrfSim.mtl objectConsoleNotifyLevel 1, notifyLevel 2) - so silence is
   NOT evidence of "no diagnostics"; it is the expected output at that level.
3. The ENTITY freeze is a REGRESSION, not a never-worked class (the 2026-07-23 handoff is
   wrong on this). 1.BdeHQ drove its Mojave route to the destination vertex on 2026-07-13
   (telemetry: at the route end, 1.5 m off, by t=23 s wall at 20x) under
   GroundWaypointAltitudeMode=Fixed100. It has been bit-frozen in EVERY run since Live mode
   became the default (2026-07-15 x2, 07-19 x3, 07-23). The Fixed100 control at Mojave that
   docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md explicitly demanded on
   2026-07-15 was NEVER run. It is a one-env-var, one-run probe.
4. The COMPANY (higher-unit) freeze has a live-verified MITIGATION already in the code base
   (Vrf:SubordinateFanOut - 18/18 company members marched at Mojave on 2026-07-13,
   UNIT_MOVEMENT_RESEARCH.md sec 4c) and a doc-grounded ROOT-CAUSE CANDIDATE (item 2a)
   with a config/content-level fix to test. RUN 3 ran with the mitigation OFF by design
   (golden parity) and the 07-23 handoff does not mention that it exists.
5. The Developer's Guide / class reference IS public (user-supplied URLs, same day):
   https://docs.mak.com/api/vrforces5.2/classref/ (also 5.1.1, 4.10). Read this pass
   (sec 1b). Its floor: the 5.x guide DROPPED the aggregate / organization / behavior-
   model chapters (105 narrative pages, none on aggregates; the index still lists the
   titles with empty hrefs); the 4.10 guide has them but they enumerate controllers and
   defer to the class docs, which are the shipped headers already read. The higher-unit
   controller's decision path is documented NOWHERE public - which makes it a legitimate,
   well-formed support question, not a 101 one (sec 7).

## 1. Sources read this pass (and what is NOT available)

Installed vendor material (C:\MAK\vrforces5.0.2 unless noted):
- doc/help/Content (the Users Guide as HTML): Modeling/UnitCreation/*, Modeling/EntityLevel/*,
  Tasks/MovementTasks/RouteMoveAlong.htm, Tasks/TasksAssign/{UnitTasksAssign,
  WhichSimulationObjectsExecuteTasks,UnitMembersTaskIndependently,TaskExecutionRulesConfigure,
  EntityMovement}.htm, Tasks/UnitBehaviors/vrf_overviewUnitTasksBehaviors.htm,
  Tasks/ScriptedBehaviors/vrf_BehaviorSubordinateCoordination.htm (the ONLY page that
  mentions "higher unit"), SimObjectsSection/ObjectInfo/vrf_*console*.htm,
  Appendixes/vrfSim/vrf_vrfSimMTLParams.htm, Scenarios/CreateRun/vrf_exportMSDL.htm,
  Introduction/VR-ForcesIntro/vrf_remoteControl.htm (is about the OBSERVER, not the API).
- include/vrfcontrol/vrfRemoteController.h (createAggregate overloads :1313-1350;
  addToOrganization :1354; setObjectNotifyLevel :1977; logObjectConsoleToFile :1983;
  addBackendConsoleMessageCallback :1992); include/vrfmsgs/ifCreateVrfObject.h
  (setInitialFormation :216-221, setAddToOrbat :255, setSuperior :259);
  include/vrfmodel/{disaggregatedMoveAlongController,
  disaggregatedLeadFollowInFormationController,disaggregatedFormationController}.h.
- examples/remoteControl/textIf.cxx (MAK's shipped remote-control console; the oracle's
  textIf.cxx descends from it) and examples/guiThreadVrfRemoteController/*.
- data/simulationModelSets/EntityLevel: vrfSim/"Tank Company (USA).entity",
  "Tank Platoon (USA).entity", "Tank Headquarters Section (USA).entity",
  "M1A2_Abrams_MBT.entity"; vrfSim/platforms/{HigherAggregate,VehicleAggregate,
  Aggregate,Ground_Vehicle}.ope; vrfSim/systems/movement/{ground-disaggregated-movement,
  ground-higherUnit-disaggregated-movement,ground-tracked}.sysdef; vrfSim/formation/
  Formation-{Column,Line,Wedge,Vee}-Armor-Co(US).frm, Ar_Plt_US_Column.frm,
  Ar_Co_HQ_Column.frm; vrfSim/taskRules/{default-task-rules.tsk,doctrines.dct,
  actionCategories.tsk} (the files CORRECTIONS_LOG says nobody had opened - now opened);
  scripts/unit_travel_offset_routes.lua, move_along_route_and_continue.lua; C2simEx.sms,
  EntityLevel.sms; doc/luadoc/modules (SimObject/unitUtil/aggregateUtils function lists).
- appData/settings/vrfSim/vrfSim.mtl (live settings) and bin64/vrfSim.log (RUN 3).
- doc/VR-ForcesFirstExperience.pdf (pdftotext; entity-level tutorial + aggregate-LEVEL
  chapter; creates units via "Aggregate As" in the GUI - no remote-control content).
Our own record re-read: UNIT_MOVEMENT_RESEARCH.md (sec 1, 4c, 5), MOJAVE_ROOTCAUSE_
INVESTIGATION_2026-07-14.md (findings log, A/B design, parts 4-5, the 07-15 Live-mode
runs), FREEZE_ROOTCAUSE_AGGREGATE_2026-07-21.md, MOJAVE_FIXTURE_2026-07-21.md,
HANDOFF_2026-07-22_PLAN_ASSIGNMENT.md, RUN_2026-07-19_MOJAVE_CHAIN.md, R9_region_swap_
2026-07-13.txt (raw), runs/20260723T174540Z_run/* (RUN 3), VRF_GROUND_TRUTH.md 0.0/0.2/
0.3/0.5, PRIOR_ART_SURVEY.md Q2/Q3, TYPE_MAPPING_TABLE.md sec 1, CORRECTIONS_LOG.md.

### 1b. The Developer's Guide (public at docs.mak.com; user-supplied URLs, read same day)

Not installed locally (doc/ holds Users Guide, First Experience, Entity Catalog, Adding
Content, Migration, Interoperability, release notes). The old ftp.mak.com host is gone
(NXDOMAIN) and www.mak.com/docs/... 301s to docs.mak.com, which is what resolves.
- Version index: https://docs.mak.com/api/vrforces5.2/classref/index.html (also
  vrforces5.1.1, vrforces4.10). Remote Control API chapter (5.2):
  vrf_the_v_r_forces_remote_control_api.html -> vrf_managing_v_r_forces_objects.html,
  vrf_tasksand_plans.html, vrf_usingthe_remote_control_a_p_i.html; class page
  class_dt_vrf_remote_controller.html (signatures identical to the shipped 5.0.2 header).
- 5.x GAP: the narrative chapters "The Aggregate Entity Behavior Model", "Ground
  Disaggregated Movement System", "The Organization Manager", "Echelon IDs", "The
  Subordinate Manager", "Object Console Messages" exist as index TITLES with EMPTY hrefs
  in the 5.2 keyword index; none of the 105 narrative pages covers aggregates or
  organization. They are present in 4.10 (entitymodels_aggregates.html,
  entity_behaviors_ground_disaggregated_movement.html, vrf_the_organization_manager.html,
  vrf_object_console_messages.html, vrf_the_aggregate_multiresolution_model.html).
- What those pages ADD beyond the shipped headers (everything else in them is the header
  text or a controller list):
  * Organization Manager (4.10): new objects are placed under the force-level unit by
    default; "The echelon ID for a simulation object is not available immediately upon
    creation. The assignment takes place after the simulation object is attached to its
    superior"; setSuperior() requires both objects to exist (queueSetSuperior is the
    load-time variant); DtAggregateOrganizationController "ensures subordinates use
    different designators" and reorganize() reassigns designators "starting with 1, and
    with no gaps" moving destroyed or independently-tasked members to the bottom.
  * Managing VR-Forces Objects (5.2): "createAggregate() creates a new unit ... then calls
    setSuperior() for each of the subordinates"; a create must target ONE back-end
    (DtSimSendToAll warns and routes to the first); a duplicate requested name is silently
    replaced by a default name.
  * Handling Unachievable Tasks (5.2): failure handling is per controller via
    decideToGiveUpTask()/giveUpTask(); the DEFAULT returns false with an empty giveUpTask()
    - "no automatic failure handling occurs without custom code". The Task Manager page
    documents dispatch, conflict clearing ("newer tasks always take precedence") and
    completion reports, and is silent on tasks no controller can execute. => a controller
    that cannot proceed sits tasked forever with no report. That is the documented shape
    of what we observe on 114.MechCoy and 1.BdeHQ.
  * Object Console Messages (4.10): five streams objectConsoleError/Warn/Info/Verbose/
    Debug; messages go to the object's console, the vrfSim console AND the vrfSim log
    file, gated per object by notify level; Info is "important events of an object that
    are part of normal operation". At vrfSim.mtl objectConsoleNotifyLevel 1 only
    Error/Warn survive - Info/Verbose from the movement controllers are dropped before
    they reach WatchVrf or vrfSim.log.
  * Ground Disaggregated Movement System (4.10): lists the ten controllers (formation,
    move-into-formation, turn-to-heading, move-to, move-to-location, patrol-between,
    patrol-along, move-along, wait, convoy) and states the design: the unit "push[es]
    down responsibility ... by forwarding movement tasks to its subordinates". Nothing on
    higher-echelon units or preconditions.
  * DtDisaggregatedMoveIntoFormationController (5.2 class page): the higher-unit mover's
    start subtask; "issues move-to-location tasks to subordinates, followed by
    turn-to-heading tasks upon arrival"; complete when ALL subordinates arrive and turn;
    sends a DtFormationTypeRequest to itself first unless DtKeepExistingFormation. So a
    higher unit's move-along BEGINS with a formation snap/move that depends on the
    formation resolving - consistent with H-CO-1.
  * DtVrfAggregateStateRepository (5.2): formationName()/setFormationName(),
    allowableFormationNames() - the live list the port's formation repair already queries.

## 2. The vendor's model of unit creation and tasking (mechanisms, with citations)

M1. CREATE. Two sanctioned remote paths: (a) createAggregate(type, pos, force, heading,
    name, label, addr, initialAggregateState = DtDisaggregated, startingUUID,
    createSubordinates = false) - the overload the port calls with createSubordinates=true
    and DtDisaggregated (VrfC2SimService.cs:466-467; header :1313-1325); (b) MAK's own
    example creates the aggregate WITHOUT subordinates and then creates member entities and
    calls addToOrganization(memberUuid, aggUuid) in the creation callback, with an explicit
    warning that the aggregate must exist before members are attached (textIf.cxx:587-640,
    1236-1271). GUI rule: "When you create a unit, it is created as a subordinate to the
    force level. You cannot create units that are subordinates of an existing unit. Once you
    create a unit, you can subordinate it" [Modeling/UnitCreation/vrf_createAggregates.htm].
    The create message also carries setInitialFormation / setSuperior / setAddToOrbat
    (ifCreateVrfObject.h:216-259) - none used by the port or the oracle.
M2. TEMPLATE RESOLUTION. Type -> best matchType in the loaded .sms chain (C2simEx ->
    EntityLevel -> base). Tank Company (USA) = 3:11:1:225:5:2:0:0, platform
    HigherAggregate.ope, movement ground-higherUnit-disaggregated-movement.sysdef,
    subordinates HQ Sec (3:11:1:225:14:2:1:0) + 3x Tank Platoon (USA) (3:11:1:225:3:2:0:0).
    Tank Platoon (USA) = platform VehicleAggregate.ope, movement ground-disaggregated-
    movement.sysdef, 4x M1A2. M1A2 = Ground_Vehicle.ope, movement ground-tracked.sysdef.
M3. TASK A UNIT = SAME TASK AS AN ENTITY. "To assign a task to a unit, select the unit, then
    assign it a task as you would any individual entity" [Tasks/TasksAssign/
    UnitTasksAssign.htm]. Which objects can execute a task is decided by which CONTROLLER
    their systems carry [WhichSimulationObjectsExecuteTasks.htm]. The port's
    moveAlongRoute(unitUuid, routeUuid) is therefore the correct call (UNIT_MOVEMENT_
    RESEARCH.md sec 1 item 1 already established this on 2026-07-12).
M4. THREE DIFFERENT MOVE-ALONG CONTROLLERS are in play - this is the axis the freezes split
    on, and it is per-template WIRING, not echelon:
    - Entity (M1A2): ground-auto-move-along-controller, near-distance 25 m, at-distance 1 m,
      approach-speed 2 m/s (ground-tracked.sysdef:240-256). Follows the route vertices
      itself.
    - Leaf unit (Tank Platoon, HQ Sec): aggregate-lead-follow-in-formation-controller
      (ground-disaggregated-movement.sysdef). "creates a temporary working route for the
      LEADER ... other subordinate routes are handled by their subtask ... complete when all
      subordinates have reached the end of the route and have issued task complete reports"
      (disaggregatedLeadFollowInFormationController.h:33-55). These working routes are the
      "<member>'s Offset Route" objects visible in vrfSim.log.
    - Higher unit (Tank Company): aggregate-move-along-controller (ground-higherUnit-
      disaggregated-movement.sysdef) = DtDisaggregatedMoveAlongController: "queries the
      aggregate's DtFormationState repository ... for the current positions of its taskable
      subordinates in the current formation. It then creates a set of 'working' routes
      parallel to the original route, based on the offsets obtained from the formation
      state repository" and "Aggregates must be configured with a command radio for this
      controller to work properly" (disaggregatedMoveAlongController.h:36-62, 444-447).
      => A higher unit's move DEPENDS on a valid formation state to derive per-sub-unit
      offsets. If no offsets can be derived, no working routes are created and the task
      neither executes nor fails loudly (nothing in the header promises a diagnostic).
    Route-start rules for units [Tasks/MovementTasks/RouteMoveAlong.htm]: "By default, a
    simulation object moves along a route from its nearest vertex"; "Units do not generate
    subordinate routes until they reach the beginning of the route"; "A unit starts moving
    along a route when the leading edge of its formation is at the first point of the
    route" (the port prepends the unit's OWN centre position as vertex 1 -
    VrfC2SimService.cs:731-733 - so the leading edge is already PAST vertex 1 at task time).
M5. FORMATION STATE. Names are "regularized" to lower case; "If the formation in the
    parameters is not valid (mal-formed) or missing, the DtWorkingFormationString is used"
    (working formation rebuilt from current member positions; disaggregatedFormation-
    Controller.h:246-262). Platform defaults: HigherAggregate.ope (formation "Column");
    VehicleAggregate.ope and Aggregate.ope (formation "Other"). The company's formation
    files assign a SUB-formation to each sub-unit slot:
      Formation-Column-Armor-Co(US).frm: entry-1 (HQ, offset 0) "Column-Left"; entry-2
      +200 m "Wedge-Right"; entry-3 -430 m "Column-Left"; entry-4 -230 m "Column-Right".
      Line/Wedge/Vee variants: "Line-Left"/"Column-Left" for entry-1, "Wedge-Right"/
      "Wedge-Left" for the platoons (offsets up to 365 m).
    Tank Platoon (USA) defines Column-Left/Line-Left/Wedge-Left/Vee-Left/Column-Right/
    Column/Line-Right/Wedge-Right/Vee-Right - so the platoon slots resolve. Tank
    Headquarters Section (USA) defines ONLY line/column/wedge/column-center - so EVERY stock
    company formation assigns the HQ section a name it does not have. This is a defect in
    MAK's shipped content, reproducible offline, and it is exactly what vrfSim.log reports
    at every Tank Company (USA) creation (sec 3).
M6. AGGREGATED vs DISAGGREGATED. Aggregated: the unit icon moves, movement only, no combat.
    Disaggregated: members are simulated; "each member of the unit plots a path and follows
    it"; tasks are delegated by radio; transitions preserve movement tasks [Modeling/
    UnitCreation/vrf_aggregateStateAndEntityPlansAndTasks.htm; Modeling/EntityLevel/
    vrf_aggregateTaskBehavior.htm]. The port always creates DtDisaggregated.
M7. TASK EXECUTION RULES (taskRules/default-task-rules.tsk, opened this pass): one group
    "Movement" lists move-along, move-to, move-into-formation, follow-entity, patrol-*,
    stop-moving, convoy-*, etc. as MUTUALLY EXCLUSIVE - starting any one interrupts a running
    one; "Tasks are always mutually exclusive with themselves" [TaskExecutionRulesConfigure.
    htm]. doctrines.dct is EMPTY (count 0). Relevance: any SetFormation snap / MoveInto-
    Formation issued around a MoveAlongRoute displaces it; the port's ordering matters.
    Members independently tasked revert to unit control on completion [UnitMembersTask-
    Independently.htm] - the documented basis for the R10 fan-out.
M8. DIAGNOSTICS (the vendor's own channels):
    - bin64/vrfSim.log: written on every launch, OVERWRITTEN on the next; RUN 3's copy is
      still on disk. The runner does NOT copy it into the run directory (verified: no such
      step in RunC2SimScenario.ps1/LaunchVrf.ps1/StopVrf.ps1). It was the "grep oracle" on
      2026-07-13 and has been ignored since.
    - vrfSim.mtl (live values): notifyLevel 2; objectConsoleNotifyLevel 1 ("0 fatal, 1
      warnings, 2 diagnostics, 3 verbose, 4 debug" [vrf_setconsolenotifylevel.htm]);
      sendBackendLogToNetwork 0; enableLogFileTimestamps 0. Object-console messages are
      emitted only at or above the object's notify level, so WatchVrf's CON channel
      receiving zero lines in RUN 3 is the EXPECTED output at level 1 - not evidence that
      VRF had nothing to say.
    - Remote API: setObjectNotifyLevel(uuid, level) and logObjectConsoleToFile(uuid, file)
      (vrfRemoteController.h:1977-1988); addBackendConsoleMessageCallback (:1992) delivers
      the back-end log over the network when sendBackendLogToNetwork=1.
M9. ALTERNATIVE CREATION PATHS. MSDL import exists but is GUI-only (File > Import Scenario
    Objects) and imports objects/locations/forces/formations/tactical graphics; unit types
    must exist in the Simulation Object Editor [Scenarios/CreateRun/vrf_exportMSDL.htm].
    The programmatic equivalent is an authored .scnx + loadScenario - already built and
    live-proven for a Tank Platoon (Sweden, Mojave, Mojave-below-terrain all moved,
    2026-07-22; HANDOFF_2026-07-22_PLAN_ASSIGNMENT.md sec "STRATEGIC PIVOT").
M10. MAK'S OWN CAVEAT on unit behaviours: designed "to work in an open terrain such as the
    U.S. National Training Center at Fort Irwin ... your results may vary. We do not support
    all tasks in all terrains ... MAK recommends developer training" [Tasks/UnitBehaviors/
    vrf_overviewUnitTasksBehaviors.htm]. Region-dependence of unit movement is vendor-
    acknowledged, so a question framed around it is not naive.

## 3. What the simulator's own log says about RUN 3 (bin64/vrfSim.log, 2026-07-23)

- Creation (lines ~106-260): 1222.MechPlt -> Tank Platoon (USA) + M1A2 1-4. 114.MechCoy ->
  Tank Company (USA) + "AR HQ Sec 1" (Tank Headquarters Section (USA): M1A2 5-6, M3 1,
  HMMWV 1-2, AUV 1 = M577) + AR Plt 1-3 (Tank Platoon (USA), 4x M1A2 each) = 18 members
  (matches the 18/18 the R10 fan-out enumerated on 2026-07-13). 1.BdeHQ -> M1A2_Abrams_MBT.
  Immediately after the company and its HQ section are created:
    "Formation does not exist for given aggregate.  Attempting to position based on
     subordinate footprint sizes."
  (the line does not name the aggregate - company or HQ section; flagged in sec 8).
- Just before the routes are created (line ~283):
    "AR HQ Sec 1: Aggregate state has invalid formation name \"column-left\""
  = mechanism M5 firing on stock content. The port's formation repair (Vrf:Aggregate-
  Formation=auto) only touches aggregates the PORT creates (VrfC2SimService.cs:1084 fires
  on our own creation callback); VRF-created sub-aggregates such as the HQ section are
  never repaired - consistent with the company still freezing on 2026-07-13 with auto ON.
- Tasking: the three routes are created; then "M1A2 1's/2's/3's/4's Offset Route" appear
  (the platoon's lead-follow working routes) and are later removed. There are NO working
  routes for AR Plt 1-3 or AR HQ Sec 1, NO "moveAlong() - empty route" line for the
  company (the 2026-07-13 lead-follow failure signature), and NOTHING for 1.BdeHQ. At
  notifyLevel 2 / objectConsoleNotifyLevel 1 this silence is uninformative (M8).
- Other lines: "Could not load physical world file ..\C2simEx\physicalWorldParams.mtl",
  "Could not load comm model file ..\C2simEx\commModelParams.mtl", "No range-id specified
  for base-system.combat-range-controller" (x9). Observed, not interpreted; the comm-model
  one is worth one look because the higher-unit controller needs a command radio (M4).

## 4. The two freezes re-diagnosed (falsification gate: competing hypotheses + falsifiers)

### 4a. 114.MechCoy - higher-unit (company) move-along

H-CO-1 (leading, doc-grounded): stock content defect (M5) -> the HQ section sub-unit has an
  invalid formation; the company's higher-unit controller cannot derive per-sub-unit
  offsets from its formation state -> zero working routes -> silent freeze (M4). Evidence:
  the vrfSim.log lines above; zero offset routes for the company tree in RUN 3; the
  formation-repair scope gap. Competing: H-CO-2 - the 2026-07-13 "empty route"
  region/clamp failure family (MOJAVE_ROOTCAUSE Thread A) also hits the higher-unit
  controller but silently. Against H-CO-2: the R10 fan-out marched all 18 company members at
  the same place, and the 2026-07-22 fixtures moved at Mojave and with below-terrain
  waypoints, so region and altitude are already falsified for the lead-follow path.
  H-CO-3 - route geometry: vertex 1 is at the company centre while the leading edge is
  ~200 m ahead (M4 leading-edge rule + M5 offsets); consistent with the 410 m wrong-way move
  on 2026-07-13 (Fixed100) vs 0 m under Live mode, but not sufficient on its own.
FALSIFIERS: H-CO-1 is dead if, after giving AR HQ Sec 1 a formation it defines ("column",
  via SetAggregateFormation on the sub-aggregate uuid the R10 recursion already enumerates,
  or a C2simEx override of the .frm/.entity), vrfSim.log shows the same "invalid formation"
  line or the company still creates zero working routes. H-CO-3 is dead if starting the
  route ahead of the formation's leading edge changes nothing. Expected if H-CO-1 holds:
  working routes for AR Plt 1-3 / HQ Sec appear in vrfSim.log and the company marches.
Verified vs assumed: the content defect and the log lines are VERIFIED (files + log). That
  the invalid sub-formation is what stops the controller is INFERRED from the header text -
  not yet shown live.

### 4b. 1.BdeHQ - single-entity move-along

> **SUPERSEDED SAME DAY (2026-09-01, P1 RUN 2 - see PREREG_P1_FIXED100_ENTITY_2026-09-01.md
> outcomes): H-ENT-1 IS FALSIFIED** - the entity froze under Fixed100 with all gates met.
> The observed mechanism (12,100 Info-level "Waiting for nav data to load" lines, visible
> once objectConsoleNotifyLevel was raised) is **H-ENT-3: the NavArea this project's own
> 2026-07-14 session generated and left in SharedData** (120k tiles; extent contains all
> taskees; the company's tree waits on it too). The Live-mode default landing the same
> day as the artifact was a coincidental confound. P1c (artifact moved aside) is the
> confirming probe. The history below stands as the state of knowledge when written.

RECORD CORRECTION: the 07-23 handoff's "no entity move has ever been proven through the
interface" is FALSE. R9_region_swap_2026-07-13.txt: 1.BdeHQ (3b905c4e) at spawn
34.608416,-116.712685 (t=3) and at 34.608416,-116.700075 (the route's final vertex, 1.5 m
from the ordered -116.700058) from t=23.1 s onward, static thereafter. A vacuous completion
leaves a unit AT SPAWN (R11, 2026-07-13); this one is at the DESTINATION. Also 9016 m at
Sweden on 2026-07-14 (MOJAVE_ROOTCAUSE sec "Experiment matrix"). Caveat: 20 s sampling, so
the transit itself was not sampled.
Run history of 1.BdeHQ at Mojave, ALL with CreateRoute+MoveAlongRoute confirmed dispatched:
  2026-07-13 Fixed100 waypoints, 20x, GUI-launched, R9 full init ........ MOVED (to route end)
  2026-07-15 Live clearance 0 and 50, 20x, GUI-launched, lean init ...... FROZEN (both runs)
  2026-07-19 Live, 1x, launcher, lean init, born 10000 MSL .............. FROZEN x3 (bit-exact)
  2026-07-23 Live, 1x, launcher, lean init, RUN 3 ...................... FROZEN (bit-exact)
GroundWaypointAltitudeMode DEFAULTED to "Live" from 2026-07-15 to 2026-09-02, so
every run after 07-13 used Live. (The default became "TerrainProfile" on 2026-09-02 -
docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 7 DEFAULT FLIP - which is AFTER
every run in the table above; nothing in this section's reasoning changes.) The Fixed100 control at Mojave that MOJAVE_ROOTCAUSE
(2026-07-15, "THE MISSING CONTROL") called for as THE first thing to do was never run.
H-ENT-1 (leading): Live-mode vertex altitude (live reflected altitude + 50 m; VrfC2Sim-
  Service.cs:727-733, 763-767) stalls the entity's ground-auto-move-along-controller (the
  platoon is insulated because the lead-follow controller regenerates its own ground-
  clamped working routes; the company shows the same Live-vs-Fixed100 symptom change:
  410 m wrong-way vs 0 m). Mechanism is NOT established: the descriptor exposes at-distance
  1 m / near-distance 25 m (ground-tracked.sysdef) but the headers do not say whether
  arrival is tested in 2D or 3D; the Live vertex also depends on the reflected-read
  altitude, which the corrections log shows can be garbage for some object classes.
H-ENT-2: some other 07-13 -> 07-15 change (GUI-launch -> vrfLauncher; full -> lean init;
  20x -> 1x from 07-19; birth altitude 1000 -> 10000 from 07-16). The 07-15 doc leaned this
  way ("environment"), but the platoon moved in the same 07-19/07-23 runs, so the
  environment was not globally frozen.
FALSIFIER for H-ENT-1: RUN 3 verbatim with Vrf__GroundWaypointAltitudeMode=Fixed100 (one
  variable); if 1.BdeHQ stays bit-static through a running clock after MoveAlongRoute,
  H-ENT-1 is dead and H-ENT-2 is next. If it moves, H-ENT-1 is confirmed and the platoon
  result under Fixed100 must be recorded too (Live mode was introduced FOR aggregates).
Verified vs assumed: the run history and the default are VERIFIED (raw files, source). The
  altitude mechanism is a HYPOTHESIS with one clean prior success and six failures on the
  other side of the single variable.

## 5. Record-integration failures found (the corrections-log lesson, again)

- The 07-23 handoff dropped: the entity's proven moves (sec 4b); the R10 fan-out's 18/18
  company result at Mojave (UNIT_MOVEMENT_RESEARCH sec 4c); the existence of Aggregate-
  Formation=auto; and the 07-15 "missing control". Its NEXT-SESSION ORDER proposed bespoke
  probes for questions that had cheaper, already-designed discriminators.
- The Object Console channel was declared "empty" three times (07-19 chain doc, sec 4
  item 3) without checking the notify level it was gated by (M8). Zero CON lines were the
  configured outcome.
- vrfSim.log - the vendor's primary diagnostic and this project's own 2026-07-13 oracle -
  is not captured per run and was not read for RUN 3 until this pass.
- Formation repair was believed to cover "aggregates"; it covers only port-created ones.

## 6. Proposed next actions (all pre-registerable; user go required before any LIVE run)

P0 - OFFLINE / CONFIG ONLY (no license time, no appNo):
  a. Runner: copy C:\MAK\vrforces5.0.2\bin64\vrfSim.log (and vrfGui.log) into the run
     directory at teardown. One PowerShell step.
  b. vrfSim.mtl (back it up first): notifyLevel 3, objectConsoleNotifyLevel 3,
     enableLogFileTimestamps 1, sendBackendLogToNetwork 1. Expect VRF to explain itself on
     the next run; revert if the log volume hurts the tick.
  c. App: log every CreateRoute vertex (lat, lon, alt) at INFO. Today the vertex altitude is
     never recorded, which is why H-ENT-1 cannot be checked from RUN 3's artifacts.
  d. App (small): extend the formation repair to VRF-created sub-aggregates using the
     existing GetAggregateMembers recursion (needed for P2).
P1 - LIVE, 1 run, ONE variable: RUN 3 verbatim + env Vrf__GroundWaypointAltitudeMode=Fixed100
  (the runner already proves Vrf__ env binding via Vrf__ApplicationNumber). PREDICTION:
  1.BdeHQ moves (H-ENT-1); record the platoon and company outcomes as secondary. FALSIFIER:
  entity bit-static through a running clock. Movement gate as always: static-while-paused ->
  moving -> settled + POS/RPT agreement.
P2 - LIVE, 1 run, ONE variable: RUN 3 + the HQ-section formation fix (P0.d, or a C2simEx
  content override). PREDICTION (H-CO-1): vrfSim.log shows working routes for AR Plt 1-3 /
  HQ Sec and 114.MechCoy marches; FALSIFIER: same "invalid formation" line + zero routes.
P3 - if P2 fails: adopt Vrf:SubordinateFanOut as the product path for higher units (live-
  verified 18/18 at Mojave 2026-07-13) with its documented caveats (no formation keeping;
  F1 runaway / F2b vacuous completions under load).
License: MAK license expires 2026-09-15; P1 + P2 are two ~20-minute runs.

## 7. What to ask MAK (well-formed, evidence-backed - not 101 questions)

1. Documentation: the public 5.x Developer's Guide (docs.mak.com) no longer carries the
   aggregate / organization / behavior-model chapters that 4.10 has (their index entries
   remain with empty links). Is there a 5.x source for the higher-unit movement model, or
   is the 4.10 text still authoritative for 5.0.2?
2. Content defect report: in EntityLevel, Formation-{Column,Line,Wedge,Vee}-Armor-Co(US).frm
   assign the HQ slot sub-formations "Column-Left"/"Line-Left" and the platoon slots
   "Wedge-Right"/"Wedge-Left"/"Column-Right"; "Tank Headquarters Section (USA).entity"
   defines only line/column/wedge/column-center. Every Tank Company (USA) creation logs
   "AR HQ Sec 1: Aggregate state has invalid formation name \"column-left\"". What is the
   intended behaviour of aggregate-move-along-controller when a sub-unit's formation is
   invalid, and is the fix to add the missing names to the HQ section template?
3. Higher-unit move-along at a location where entity and leaf-unit moves succeed: the
   controller creates no working routes and emits nothing at notifyLevel 2. Which notify
   level / channel exposes its decision path, and what preconditions (command radio,
   formation state, route start relative to the leading edge) must hold?
4. For ground-auto-move-along-controller: are route-vertex altitudes clamped to terrain or
   used literally (at-distance 1 m arrival)? We observe a lone M1A2 freeze bit-exact at spawn
   when route vertices are placed 50 m above its reflected altitude, and move when they are
   at 100 m MSL (about 1 km below terrain) - pending the P1 control.
Ask 1 first; 2-4 only after P1/P2, so each carries our own falsification result.

## 8. Adversarial review (2026-09-01, this pass)

- Could the vrfSim.log lines belong to the platoons rather than the company? No for the
  invalid-formation line: "AR HQ Sec 1" exists only under 114.MechCoy. Yes possibly for
  "Formation does not exist for given aggregate" - it is unattributed; P0.b/P2 resolve it.
- Could the 07-13 entity result be a vacuous completion? Unlikely: vacuous completions leave
  the unit at spawn (R11); this unit sits at the destination vertex. But the transit was not
  sampled (20 s cadence); the claim is "arrived", not "observed driving".
- Is H-ENT-1 confounded? Yes - launcher, init file, multiplier and birth altitude all
  changed between 07-13 and 07-19. The platoon moving in the same 07-19/07-23 runs argues
  against a global environment freeze; P1 is the single-variable discriminator.
- Zero offset routes for the company might reflect a different naming for higher-unit
  working routes rather than their absence. The header says the controller "creates a set
  of 'working' routes"; on 2026-07-13 at Sweden the company completed and 45 offset routes
  were counted for the whole force - not attributable per unit from the surviving record.
  P0.b + P2 settle it.
- The MAK class docs were not readable; M4/M5 rest on shipped headers and help pages, which
  are primary but terse. Nothing here depends on the unverified search snippet.
- Unexplained and left open: the 2026-07-15 observation that aggregates' positions read as
  degenerate 0,0 at Live clearance 0 but real at clearance 50 (MOJAVE_ROOTCAUSE, altitude
  probe). Not folded into any hypothesis above.

## 9. The clamping / terrain-query contract (2026-09-01 evening; from the user-provided
## Developer's Guide at docs.mak.com + local sysdefs; IT IS DOCUMENTED)

The user's challenge stood: clamping is 101 and must be documented. It is. The contract,
assembled from vrf_object_operationsand_blocking_terrain_calls.html,
vrf_queryingthe_terrain_interface.html, the Users Guide altitude pages, and the sysdefs:

C1. Terrain queries have BLOCKING and NON-BLOCKING (dataAvailable) forms; "all simulation
    models must use the dataAvailable flag". Non-blocking + unpaged terrain => the query
    "will return immediately and DataAvailable will be set to false" and "no terrain
    intersections will be returned". Queries are chord-based and BIDIRECTIONAL (p0->p1,
    e.g. Z +10000 to -10000) - Thread A's 2026-07-14 "load-bearing unknown" is answered
    from the book: direction is not the issue; DATA AVAILABILITY is.
C2. clampToGround()/place() on unpaged terrain "will be placed as specified without
    regard to the terrain" (no error); requireAllData exists to abort-and-restore instead.
C3. Creation and Set-Location are QUEUED until terrain is available; nothing analogous is
    documented for the unit route builders - Thread A's binary finding stands: the offset
    -route builder DROPS a vertex whose clamp fails => "moveAlong() - empty route".
C4. ENTITIES are protected: ground-tracked.sysdef:30-45 carries movement-based-terrain-
    preload-controller (radius 5-500 m by speed) and entity move tolerates failed clamps.
    NEITHER unit movement system (ground-disaggregated, ground-higherUnit-disaggregated)
    carries ANY preload controller (grep: 0/0). Unit route-building therefore depends on
    whatever terrain happens to be paged when the task arrives.
C5. Route-vertex altitude is the AUTHOR'S responsibility, three documented reference
    frames, with the Guide's own warning that above-sea-level vertices can be underground
    [vrf_setRouteVertexAltitude.htm]. There is no documented auto-clamp of route vertices
    at creation.
C6. TropicTortoise's page-in area (read from the .scnx this pass): center ~34.60,-116.55,
    half-extents ~30x61 km - it COVERS all R9 units. Area presence does not by itself
    prove tiles are paged at clamp time (C1/C2 are about actual paged state).
PREDICTION under this contract for P2b (running): if the company's empty working routes
are altitude-sensitive, Live vertices fix them; if they are PAGING-sensitive (C1/C4),
Live changes nothing and the next lever is warming the corridor (page-in wait / preload)
before tasking - a config/sequencing fix, not code.
