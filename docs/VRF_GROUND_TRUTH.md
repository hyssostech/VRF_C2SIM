# VRF GROUND TRUTH (Phase 0 deliverable)

Purpose: the things we should have known on day one about how VR-Forces actually
works, captured from primary sources (installed SDK headers, examples, on-disk docs
and content) with file+line citations. This is the shared deliverable for all of
Phase 0 in docs/VRF_GROUNDWORK_PLAN.md. Each sub-phase (0.1 content catalog, 0.2 docs
curriculum, 0.3 remote-API surface, 0.4 self-launch, 0.5 scnx trick) appends its own
section; do not rewrite another section.

Environment audited: C:\MAK\vrforces5.0.2 (VR-Forces 5.0.2) and its VR-Link at
C:\MAK\vrlink5.8. ASCII only.

## 0.0 Supervisor cross-findings (synthesis across 0.1/0.2/0.3 + the live record)

1. THE UNREAD-WARNINGS CHANNEL: the yellow badge = Object Console warning icon (0.2
   topic 7); addObjectConsoleMessageCallback delivers uuid+level+message remotely (0.3;
   header:112/:1970, supervisor-verified). Most units in our runs carried the badge.
   ACTION: build the console-capture tool (groundwork plan 0.6) BEFORE any other live
   work - VRF may have been explaining the pile split and runaways all along.
2. CONTROLLER-CLASS SPLIT MATCHES THE BEHAVIOR SPLIT (hypothesis, pre-registrable):
   0.1.3a shows platoon-echelon templates wire aggregate-lead-follow-in-formation-
   controller while company/battalion wire aggregate-move-along-controller. The live
   record's stable per-class split (P-C1: platoon marches+publishes / company never
   publishes; FIX-ACCEPT-1: company halts on leading-edge semantics) tracks that
   controller boundary. Candidate probe: create the SAME force as platoon-class vs
   company-class real templates, task identically, compare.
   REFINEMENT (2026-07-16, supervisor, found gating E4): the boundary is per-template
   WIRING, not echelon - `Stryker Rifle Platoon (USA Army).entity` is PLT-echelon yet
   wires ground-higherUnit-disaggregated-movement.sysdef (HU; verified in the file,
   componentSystem line 18). So the cleaner single-variable probe pair is Tank Platoon
   (USA) (LF) vs Stryker Rifle Platoon (USA Army) (HU): SAME echelon, different
   controller, tasked identically - de-confounds echelon from controller entirely.
3. TYPE-FIDELITY QUANTIFIED (0.1.7): today ~64/128 COA-STP1 units map to Tank Company
   (USA) (wrong for ~51), ~49 fall to the generic Ground_Aggregate, ~15 become lone
   M1A2 entities. ArmorCoHQ misses a real template by ONE matchType field. This is the
   Phase 2.1/3 work, now concrete.
4. CONTENT GAPS NEEDING USER ADJUDICATION: no engineer aggregate, no composed USA
   mech-infantry company, no mortar/rocket aggregate in the loaded chain - nearest-type
   choices are military-semantics calls, not code calls.
5. SETTLED FOR THE ARRIVAL GATE: unit route completion = formation LEADING EDGE at last
   vertex (0.2, verbatim doc quote) - VRF unit completions are premature BY DESIGN;
   entity at-distance tasks accept an explicit arrival radius (0.3 setAtDistance).
6. WARP DECOMPOSITION (2026-07-17, supervisor, found gating the E7 census - see
   docs/experiments/RUNAWAY_WARP_CENSUS_2026-07-17.md secs 6 and 11): the archived
   "warp" events split into TWO phenomena. (a) TRANSIENT out-and-back mega-jumps -
   lockstep across co-located member entities (one displacement vector per formation
   per sample step), present at BOTH 1x and 20x, altitude spiking off-terrain then
   snapping back to the marching track - leading candidate: OBSERVER-SIDE
   dead-reckoning artifact in the reflected read (VR-Link DR-extrapolates from last
   received state; a corrupt/thrashing member velocity extrapolates absurdly).
   Member-entity warp telemetry is observation-suspect until the raw-vs-DR live
   discriminator runs (WatchVrf enhancement candidate). (b) PERSISTENT displaced
   end-states (port 20x tasked LF aggregates constant at -1306/-1681 m underground,
   41-83 km out) - a persistent position IS the reflected state; this is the real
   runaway/termination class, port-20x-specific in the archived data. Aggregate-icon
   telemetry, the 18.1-18.4 km stall band, and the E7 controller-split verdict
   (LF 3/5 moved, HU 0/4, entity 0/2 - both codebases; echelon-confounded) are
   UNAFFECTED.

---

## 0.2 VR-Forces semantics curriculum

Source: systematic read of the VR-Forces 5.0.2 User's Guide HTML
(C:\MAK\vrforces5.0.2\doc\help\Content) plus two installed config files
(fastForwardSettings.mtl, default_GuiSettings.grsx) that the Guide points to.
Read date 2026-07-16. Every load-bearing claim carries a file-path citation and,
where it decides an open question, a verbatim quote.

Citation convention for THIS section: doc paths are relative to the on-disk
User's Guide root C:\MAK\vrforces5.0.2\doc\help\Content\ . So a citation like
[Tasks\MovementTasks\RouteMoveAlong.htm] means
C:\MAK\vrforces5.0.2\doc\help\Content\Tasks\MovementTasks\RouteMoveAlong.htm .
Quotations are transcribed verbatim except that non-ASCII punctuation (curly
quotes, en/em dashes) has been normalized to ASCII; a normalized apostrophe is
shown as a plain '.

The nine required subjects are answered below in order. Subjects 3, 6, 7, 8 are
treated exhaustively because they decide our open questions. A running
"WHAT THE GUIDE DOES NOT COVER" list at the end collects the gaps that need MAK
support rather than a doc lookup.

### 1. Scenario / organization authoring

**Two modeling worlds.** VR-Forces has "two, quite different, ways to model
simulation objects - entity-level modeling and aggregate-level modeling"
[Modeling\Modeling_Units.htm]. Our C2simEx/EntityLevel chain is entity-level;
the distinction matters for every claim below (aggregate-level units cannot
disaggregate - doc-verbatim "In aggregate-level scenarios, most preconfigured units
cannot be disaggregated" [Modeling\UnitCreation\vrf_createAggregates.htm]; and every
EntityLevel.sms unit CAN [Modeling\EntityLevel\vrf_entityLevelAggregateConcepts.htm].
Supervisor wording correction 2026-07-17: this line originally said "can never" -
the doc says "most ... cannot").

**Placing an object (palette + click-to-create).** After you select a template
on the Simulation Objects Palette, a "Create object" tab is added below the
palettes [SimObjectsSection\ObjectCreation\vrf_createObject.htm]. There are two
placement modes [SimObjectsSection\ObjectCreation\vrf_placeObjectsIntro.htm]:
- "Click to Create. Each mouse click creates an instance of the selected
  object. ... Click to Create is best for rapidly creating objects using the
  default values for the object's properties."
- "Click to Locate. Mouse clicks specify the coordinates, but do not cause the
  simulation object to be created." Required "if you want to set the altitude
  relative to sea level."
By default the object is attached to the mouse and repeated left-clicks create
multiple instances [SimObjectsSection\ObjectCreation\vrf_createOnLeftClick.htm].
Editable properties at creation are enumerated in Table 20 (Force, Heading,
Label, Location, Name, Visual Markings, Overlay, Publish Object)
[SimObjectsSection\ObjectCreation\vrf_createObject.htm].

**What a well-formed unit consists of.** A unit is a simulation object that owns
subordinates organized by echelon ID:
- "When you create a unit, it is created as a subordinate to the force level.
  You cannot create units that are subordinates of an existing unit. Once you
  create a unit, you can subordinate it to another unit."
  [Modeling\UnitCreation\vrf_createAggregates.htm]
- Echelon IDs are assigned automatically: "Assignment of echelon IDs is handled
  automatically by VR-Forces when you create a unit." The org "is based on the
  echelon ID of each member" e.g. "1 M1A2, 2 Plt, 1 Force"
  [Modeling\EntityLevel\vrf_aggregateReorganization.htm].
- Subordinate ORDER determines the leader: "you can specify the order of
  subordinates in the unit. This determines which subordinate is considered the
  unit leader and affects the assignment of echelon IDs. (It also affects the
  icon used to represent the unit.) You cannot change the subordinate order
  after you create the unit."
  [Modeling\UnitCreation\vrf_aggregateLevelAggregatesConcepts.htm]

**Two ways to create a unit** [Modeling\UnitCreation\vrf_createAggregates.htm]:
1. Place a PRECONFIGURED unit from the palette (EntityLevel.sms /
   AggregateLevel.sms ship preconfigured units at several echelons)
   [Modeling\UnitCreation\vrf_createPreconfiguredAgg.htm].
2. Select existing objects and combine them via Objects > Unit > Aggregate As
   [Modeling\UnitCreation\vrf_aggregateLevelAggregatesConcepts.htm]. Note:
   "VR-Forces has many unit types configured in the Simulation Object Editor
   that are not available to create from the Simulation Objects Palette, but
   which are available as options on the Aggregate As dialog box." (Relevant to
   0.1: the palette is NOT the full type set.)

**Authoring superior/subordinate relations.** Three GUI mechanisms:
- Drag in the Objects List View: "select the simulation object you want to add
  and drag it onto the unit. ... its echelon ID changes."
  [Modeling\UnitCreation\vrf_addEntityToAggregate.htm]
- The Superior set data request: "lets you add a simulation object (individual
  or unit) to a unit or move a simulation object from a unit to the force level.
  The superior unit must already exist. For entity-level scenarios, the superior
  unit must be disaggregated."
  [DataRequests\EntityAndAggregate\vrf_setSuperior.htm]
- Constraint (both paths): "You can add simulation objects to a unit in an
  entity-level scenario only if it is in the disaggregated state."
  [Modeling\UnitCreation\vrf_addEntityToAggregate.htm]

**Order of Battle (OOB) - the authored org tree.** The OOB "represents a
hierarchy of simulation objects that can be instantiated (created) in a
scenario" and is saved with the scenario
[SimObjectsSection\BattleOrder\vrf_introToOrderOfBattle.htm]. OOB members can be
"Organiz[ed] ... into units", and each OOB member has its own UUID (unlike the
palette, where a template spawns many). Subordination rules when adding directly
[SimObjectsSection\BattleOrder\vrf_addingObjectsDIrectlyToOob.htm]:
- "If you add to a force, it becomes a top level subordinate of the force."
- "If you add to a unit, it becomes a subordinate of that unit."
- "If you add it to an entity, it becomes a sibling of the entity. If the entity
  is part of a unit, the new member becomes part of the unit."
OOBs can be exported/imported across scenarios (feeds the 0.5 scnx work and a
possible init path). Gray icon = OOB member not instantiated; colored = present
in scenario [SimObjectsSection\BattleOrder\vrf_orderOfBattleTab.htm].

Implication for the port: a native unit is force-subordinate, has auto-assigned
echelon IDs, an ordered subordinate list whose first element is the leader, and
(for entity-level) must be disaggregated before children can be attached. Our
generic-fallback units with 4 anonymous subordinates violate the "real
subordinate composition" that the palette/SOE templates encode.

### 2. Aggregates: aggregated vs disaggregated, movement, reorganization, taskability

**The two states** [Modeling\EntityLevel\vrf_entityLevelAggregateConcepts.htm]:
- "Disaggregated: Subordinates are simulated and published as individual
  entities. ... if you give a disaggregated unit a movement task, each member of
  the unit plots a path and follows it."
- "Aggregated: Subordinates are not simulated separately. Tasks and plans are
  executed by the unit. ... if you give an aggregated unit a movement task, the
  object representing the unit moves."
- Aggregated/disaggregated (simulation state) is NOT the same as
  expanded/collapsed (a display choice): "Expanded and collapsed units are
  simply different visual representations."
- "A unit in the aggregated state only simulates movement behavior. It does not
  model combat behavior." and "Only disaggregated units can engage in combat."
  [Modeling\UnitCreation\vrf_aggregateStateAndEntityPlansAndTasks.htm]

**How aggregate movement works** [Modeling\EntityLevel\vrf_closeFormationVsReorganization.htm]:
- Aggregated: "When an aggregated unit receives a movement command, VR-Forces
  calculates the path it must follow. Then it moves to the location similarly to
  any individual entity. The unit icon is placed at the center of the bounding
  box of the area that would be encompassed by the unit if it was
  disaggregated."
- Disaggregated (leader path + member offset routes): "VR-Forces calculates the
  path the unit leader must follow for movement. Then it calculates parallel
  paths (taking into account the formation) for each member of the unit. The
  unit members are responsible for following these paths until the unit
  completes the movement task."
- Task delegation is via radio: "When a disaggregated unit is given a task, it
  sends radio messages to its subordinates directing them to carry out their
  role in the task, for example, getting into formation and moving to a
  waypoint." [Modeling\EntityLevel\vrf_aggregateTaskBehavior.htm]
- "Units do not generate subordinate routes until they reach the beginning of
  the route." [Tasks\MovementTasks\RouteMoveAlong.htm] (subordinate offset
  routes are computed lazily, at route start, not at task assignment.)
- Aggregate-level (as opposed to entity-level aggregated) unit movement is
  additionally modulated by terrain/slope/roads/footprint overlap; ordered speed
  is capped by these modifiers, and "the center point of the unit is used to
  determine what type of terrain the unit is on"
  [Modeling\AggregateLevel\vrf_aggregateMovement.htm].

**State transitions preserve tasks** [Modeling\UnitCreation\vrf_aggregateStateAndEntityPlansAndTasks.htm]:
"All movement tasks can be executed in either aggregated or disaggregated state.
Units can transition between states at any time, without interrupting movement
tasks." and "Units preserve their formation across unit state transitions." A
disaggregation area on the path triggers auto-disaggregation mid-move and the
unit "calculates routes for its subordinates, and sends them Move Along Route
tasks"; on exit it re-aggregates and "the tasks for its subordinates are
abandoned" [Modeling\EntityLevel\vrf_aggregateTaskBehavior.htm].

**Reorganization vs closing formation** (two distinct mechanisms):
- Reorganization = reassign echelon IDs when a member dies. "Reorganization is
  the reassignment of echelon IDs to the members of a unit when a member gets
  destroyed." "Reorganization only applies to disaggregated units." Auto vs
  manual (Reorganize command); default is set by the auto-reorganize scenario
  parameter [Modeling\EntityLevel\vrf_aggregateReorganization.htm].
- Closing formation = fill formation-position holes; "internal to a unit ... You
  do not need to do anything to put it into effect and you cannot turn it off."
  It does NOT change echelon IDs
  [Modeling\EntityLevel\vrf_closeFormationVsReorganization.htm].

**What makes an aggregate taskable.** Any unit (aggregated or disaggregated)
"has a plan and you can assign independent tasks to units"
[Modeling\EntityLevel\vrf_aggregateTaskBehavior.htm]. The unit must exist as a
force-level (or higher-unit) subordinate with valid echelon IDs (Section 1).
Caveats:
- Aggregated units execute movement only; to model attrition/combat they must be
  disaggregated [Modeling\EntityLevel\vrf_entityLevelAggregateConcepts.htm].
- Convoy is special: "The Convoy unit can only be assigned convoy tasks. ... You
  cannot aggregate or disaggregate a convoy."
  [Modeling\UnitCreation\vrf_aggregateLevelAggregatesConcepts.htm];
  "The Convoy unit does not support aggregation and disaggregation."
  [Modeling\UnitCreation\vrf_entityLevelAggregatesAggregateState.htm]
- Aggregation state is published via "the DIS/RPR FOM aggregate state
  enumeration" and readable in DtVrfAggregateStateRepository
  [Modeling\UnitCreation\vrf_disaggregatedState.htm] (a reflected-attribute hook
  for 2.2 structure-diff; cross-refs 0.3 section 5 channel 1).

Unit posture (aggregate-level) matters for movement: units are created in Travel
posture; posture transitions "may take up to several hours" of sim time and
affect movement speed [Modeling\AggregateLevel\vrf_aggregatePosture.htm].

### 3. Ground-movement task vocabulary (EXHAUSTIVE)

The complete movement-task set lives under [Tasks\MovementTasks\] plus unit-level
tasks under [Tasks\UnitBehaviors\]. The chapter scope note:
"This chapter describes the movement tasks supplied with VR-Forces that apply to
entity-level scenarios." [Tasks\MovementTasks\MovementTasks.htm]. Aggregate-level
scenarios reuse the same tasks - e.g. [Tasks\AggregateLevelScenarios\RouteMoveAlong1.htm]
and [Tasks\AggregateLevelScenarios\LocationMoveTo1.htm] both simply say "For
details, please see 'Move Along Route' / 'Move to Location'".

#### 3a. The ground-relevant tasks and their parameters

- **Move to Location** [Tasks\MovementTasks\LocationMoveTo.htm]. Params: target
  location (click or coords), optional altitude. "Moving to a location causes a
  simulation object to travel through the terrain." "If navigation data is
  present, simulation objects take it into account in planning their path."
  Distinct from Set Location, which is instantaneous.
- **Move to Waypoint** [Tasks\MovementTasks\WaypointMoveTo.htm]. Target = a point
  or a simulation object (can be moving). In a plan, a "Continue On" checkbox:
  "The entity will maintain speed, instead of slowing as it approaches the
  waypoint." Rotary-wing caveat: "will approach a given waypoint ... but will
  never completely reach the goal."
- **Move Along Route** [Tasks\MovementTasks\RouteMoveAlong.htm]. Params: route
  (or corridor/line/pedestrian path/supply route), Reverse Direction, Treat
  Route as Road, Start at Closest Vertex. "By default, a simulation object moves
  along a route from its nearest vertex to its end point." Treat Route as Road:
  "the route is treated as a two-lane bidirectional road" and "the entity must be
  within 10 meters of the route or the task will fail." For circular reuse, clear
  Start at Closest Vertex or the object "keeps going to the end vertex."
- **Move Along Route with Actions** [Tasks\MovementTasks\RouteActionsMoveAlong.htm].
  Move Along Route plus point-anchored actions (Insert Task / Insert Set at
  vertices). Extra params: Specify Time on Target (with a hold location HL to
  burn extra time), Ordered Speed (Enforced vs Suggested), "Abandon if Time on
  Target Cannot Be Achieved" ("the tasked entity abandons the task if it
  determines that it cannot reach the target location within 10 seconds of the
  specified time"). Default here is start-at-beginning, not closest vertex.
- **Move to Location (Plan Along Roads)** [Tasks\MovementTasks\LocationPlanAlongRoads.htm]
  and **Move to Waypoint (Plan Along Roads)** [Tasks\MovementTasks\WaypointAlongRoads.htm].
  Ground-vehicle only. Route themselves onto the nearest road, drive the network
  to the point nearest the goal, then off-road to the goal "unless you select
  Stay On Road When Complete."
- **Move to Location (Plan Path)** / **Move to Waypoint (Plan Path)**
  [Tasks\MovementTasks\LocationPlanPath.htm], [Tasks\MovementTasks\WaypointPlanPath.htm].
  Surface/subsurface (water) only; needs S-57 depth data. Not ground-relevant.
- **Patrol Route** [Tasks\MovementTasks\PatrolRoute.htm]. "moves to the nearest
  vertex ... to the end point, back to the beginning ... continues going back and
  forth ... until given another command." (Never self-completes.)
- **Patrol Between** [Tasks\MovementTasks\PatrolBetween.htm]. Ping-pongs between
  two points/objects; never self-completes.
- **Patrol Area** [Tasks\MovementTasks\PatrolArea.htm]. Surface/subsurface only
  (ships); zig-zag across an area. Not ground-relevant.
- **Follow Entity** [Tasks\MovementTasks\FollowEntity.htm]. Params: target, and
  Behind/Right/Above offsets (negatives allow front/left/below). Terminates only
  when "The task is overridden" or "The followed entity is destroyed" - i.e. it
  never self-completes while the leader lives.
- **Follow Along Offset Route** [Tasks\UnitBehaviors\FollowAlongOffsetRoute.htm].
  Ground entity follows another along a leader route at an offset; "The leader is
  expected to be tasked to move."
- **Move Into Formation** [Tasks\MovementTasks\FormationMoveInto.htm]. Orders "a
  disaggregated ground unit to move to a formation at a specified location."
  Params: formation, location, heading. "For aggregated units, Move Into
  Formation has the same effect as a Formation set data request." Distinct from
  Set Formation (instantaneous).
- **Turn to Heading** [Tasks\MovementTasks\HeadingTurnTo.htm]. Ground vehicles
  pivot/K-turn to a heading; controlled by the can-pivot OPD parameter. Params:
  heading + Relative To (True / Host / Current Heading / Magnetic).
- **Come to Stop** [Tasks\MovementTasks\ComeToStop.htm]. Surface entity; normal
  stop, emergency stop, or slow drift.
- **Halt Movement** [Tasks\AggregateLevelScenarios\MovementHalt.htm]. "Movement
  stops immediately."
- **Adding Multiple Move to Location tasks** [Tasks\MovementTasks\LocationTasksMultipleMove.htm].
  In a plan, "Each Click Generates A Task" adds one Move to Location per click.
- **Convoy tasks** (Convoy To / Convoy Along) [Tasks\MovementTasks\ConvoyTo.htm],
  [Tasks\MovementTasks\ConvoyAlong.htm], [Tasks\TasksAssign\ConvoyTasks.htm].
  Ground-vehicle group movement; only the special Convoy unit takes them (see
  Section 2). Distinct from generic move tasks (spacing/lead-vehicle behavior).
- **Animated Movement** [Tasks\MovementTasks\AnimatedMovement.htm]. Assigns a
  scripted movement (CSV/ASE animation file) with an optional time scale;
  bypasses the normal path/terrain movement model. Generalization of ballistic-
  missile firing. Relevant only if we ever script exact trajectories.
- Unit-level movement/offense/defense behaviors (bounding overwatch, assault,
  attack-by-fire, movement-to-contact, seize/clear objective, mount/dismount,
  etc.) are catalogued under [Tasks\UnitBehaviors\] (UnitMovementTasks.htm,
  UnitOffensiveTasks.htm, UnitDefenseTasks.htm, MountingDismountingTasks.htm).
  These are higher-level than the C2SIM move verbs but exist if we need closer
  mappings in 2.3.

Air/naval-only movement tasks (Fly*, Orbit*, Sail*, patterns, landings,
airdrops, refuel, minesweep) are present under [Tasks\MovementTasks\] but out of
scope for ground movement; listed here only so the census is complete.

#### 3b. Completion semantics (this is the crux for false chain-advancement)

- **Move Along Route, UNIT completion = LEADING-EDGE rule** (verbatim, load-
  bearing) [Tasks\MovementTasks\RouteMoveAlong.htm]:
  "A unit starts moving along a route when the leading edge of its formation is
  at the first point of the route. It is considered finished when the leading
  edge reaches the last point of the route."
  This is the documented "leading-edge rule." For a unit/formation, Move Along
  Route reports finished when the FORMATION LEADING EDGE crosses the last
  vertex - NOT when all members arrive, and NOT based on the unit center. A
  trailing / piled / frozen member does not delay completion. This directly
  explains completions that fire while members lag, and it means any truthful-
  arrival gate we build (Phase 4) must NOT rely on VR-Forces' own notion of
  route completion for units. (Consistent with the 0.3 header quote:
  "for a pseudo-aggregate the task completes when the lead subordinate reaches
  the waypoint", moveToTask.h:30-33.)
- **Move to Location / Move to Waypoint**: completion is arrival at the
  location/waypoint (the object "travel[s] through the terrain" to it)
  [Tasks\MovementTasks\LocationMoveTo.htm], [Tasks\MovementTasks\WaypointMoveTo.htm].
  The Guide does not give a numeric arrival tolerance for these; rotary-wing is
  explicitly stated to never fully reach a moving waypoint
  [Tasks\MovementTasks\WaypointMoveTo.htm]. (Arrival tolerance = an undocumented
  gap; the remote API does expose a per-task arrival-radius override,
  DtMoveToTask::setAtDistance - see 0.3 section 4. See NOT-COVERED list.)
- **Move Along Route with Actions** adds an explicit time contract: it can
  "Abandon if Time on Target Cannot Be Achieved" within 10 seconds of the target
  time [Tasks\MovementTasks\RouteActionsMoveAlong.htm].
- **Patrol Route / Patrol Between / Patrol Area / Follow Entity / Follow Along
  Offset Route**: continuous tasks that never self-complete; they end only on
  override or (for Follow) death of the target [Tasks\MovementTasks\PatrolRoute.htm],
  [Tasks\MovementTasks\FollowEntity.htm]. Putting one in a plan statement stalls
  the plan; putting one in a While block can wedge the loop (see Section 4).
- **Viewing status**: current task is shown in the Information dialog summary /
  Tasks page, the Last Selected Object Panel, and highlighted in the Plan window
  [Tasks\TasksAssign\TaskStatusView.htm].

#### 3c. Interruption / replacement semantics

- Tasks are grouped; within a group they are mutually exclusive
  [Tasks\TasksAssign\ConcurrentTaskExecution.htm]: "C++ tasks are organized into
  the following groups: Weapon, Movement, Radio, Depth control ... All tasks
  within a group are mutually exclusive among themselves. For example, all
  movement tasks are mutually exclusive." A new movement task therefore ABANDONS
  the current movement task; a Send/radio task can run concurrently with
  movement.
- "If you assign a simulation object a task that is mutually exclusive with its
  current task, the current task is abandoned."
  [Tasks\TasksAssign\ConcurrentTaskExecution.htm]
- "When you give a task to a simulation object, if the new task is mutually
  exclusive with the current task, the simulation object immediately stops the
  current task and begins the new one." And: "If a simulation object has a plan,
  when you give it an independent task, it abandons the rest of the plan."
  [Tasks\TasksIntro.htm]
- Mutual-exclusion groups are data-driven per OPE file via a task-rules .tsk
  file (default-task-rules.tsk); all-tasks-exclusive=True forces one-at-a-time;
  "Tasks are always considered to be mutually exclusive with themselves."
  [Tasks\TasksAssign\TaskExecutionRulesConfigure.htm]
- Interactive interruption prompts exist (Prompt Before Abandoning Plan / Prompt
  Before Interrupting Task) but are GUI-only confirmations
  [Tasks\TasksAssign\ConcurrentTaskExecution.htm].

### 4. Plans and task chaining (the native "DAG" equivalent)

VR-Forces' native sequencing primitive is the PLAN, not a DAG. Two kinds
[Plans\PlansIntro.htm], [Plans\IndivGlobalPlansIntro.htm]:

- **Individual plan** (owned by one object): "a set of statements that order a
  particular simulation object to complete a sequence of tasks ... The
  simulation object carries out the tasks in order unless it is interrupted by
  an independent task or global command." Crucially for chaining
  [Plans\PlansConcepts\ConcurrentTasksInPlansUse.htm]:
  "A plan always waits for its current task to complete before it starts the
  next task, even if the next task does not conflict with the current one."
  So native chaining = strict sequential, each statement blocks on completion of
  the prior task. This is the native equivalent of "task follows task." It is
  linear, not a parallel DAG.
- **Global plan** (not owned by any object). GOTCHA, verbatim, load-bearing
  [Plans\IndivGlobalPlansIntro.htm]:
  "if a global plan has a series of tasks for the same simulation object, they
  get sent in order without regard to completion of the previous task (as if you
  issued a series of commands from the Task menu, one immediately after
  another). In such a case, only the last command sent is likely to be
  completed."
  If the port pushes a sequence of tasks the way a global plan does (fire-and-
  forget, no wait on completion), only the LAST survives. This is a prime
  suspect for "chain collapses to the final leg."

**Native control-flow statements** (the actual chaining vocabulary)
[Plans\PlansConcepts\ConditionalStatements.htm]:
- **If** - evaluated once on arrival; If/else/endif; nestable
  [Plans\PlansConcepts\IfStatement.htm]. "Do not use an If statement to test for
  receipt of a message ... Use a trigger."
- **While** - re-evaluated only after the whole block finishes
  [Plans\PlansConcepts\WhileStatements.htm]: "If you put a continuous task such
  as Wait or Patrol Between in a While block, it is possible that the condition
  will never be re-evaluated, because these tasks never finish."
- **Do Until Interrupt** - run a (possibly endless) task until a condition or
  external tasking; a trigger interruption returns to the Do-Until task afterward
  [Plans\PlansConcepts\DoUntilInterruptStatement.htm].
- **Wait Until** - wait in place until a condition; trigger with Interrupt
  Current Task selected exits the wait, otherwise resumes it
  [Plans\PlansConcepts\WaitUntilStatements.htm].
- **Trigger** - a condition evaluated CONTINUOUSLY once registered, with a
  statement block; "you must register a trigger before VR-Forces will test it";
  can Interrupt vs Suspend the current task; auto-reregister optional; fires once
  then is removed unless reregistered [Plans\PlansConcepts\Triggers.htm]. This is
  the only way to get concurrent tasking inside a plan
  [Plans\PlansConcepts\ConcurrentTasksInPlansUse.htm] and the only correct way to
  test for events/messages [Plans\PlansConcepts\IfStatement.htm]. Placement
  matters: register at plan top to guarantee evaluation
  [Plans\PlansConcepts\TriggerUseConsiderations.htm].

**Conditions available** to all of the above
[Plans\PlansConcepts\ConditionalStatements.htm], detailed in
[Plans\PlansConcepts\ConditionalTests.htm]: Entity In Area, Entity Left of Phase
Line, Entity Created, Entity Destroyed, Entity Under Fire, Entity Has Target,
Entity Embarked, Entity Altitude, Detect Entity, Receive Text Message,
Engineering Object Breached, Missile Approach Warning, Scenario Event, Lifeform
Surrendered, DI-Guy animation/appearance, Random, Resource, Simulation Object
Independently Tasked, Simulation Time, Simulation Time of Day, Simulation
Date/Time, Tactical Graphic Active. Boolean AND/OR/NOT/True/False. Note for
sequencing on movement: there is a Sim-Time condition and an Entity-In-Area
condition, but the condition list contains NO "task complete" predicate - the
only movement-completion sequencing VR-Forces offers natively is the implicit
"individual plan waits for the current task to complete" behavior above. Also
"Entity Destroyed ... disaggregated units are considered to be destroyed when
all members of the unit are destroyed" [Plans\PlansConcepts\ConditionalTests.htm].

**Subordinate command delegation.** Tasked-By-Superior lets a superior task a
subordinate, but in a plan body it is a no-op unless it is the last statement:
"as soon as it is set, the plan goes to the next task and overrides the
superior." [Plans\PlansConcepts\TaskedBySuperiorRequestInPlans.htm]

**Synchronization Matrix** - the native multi-unit coordination tool
[Plans\SyncMatrix\vrf_SynchronizationMatrixOverview.htm],
[Plans\SyncMatrix\vrf_synchronizationMatrixFunction.htm]: "a simulation object
that uses the Issue Plan command to send the plans in the matrix to the entities
or units. ... Each phase has a Phase End Condition, and when that condition is
reached then the phases ends and the next phase begins. When a phase reaches its
completion, all plan information from that phase is discarded and the plans in
the next phase are used." Explicitly "intended for a linear series of actions"
(phase-gated), not branching. This is the closest native construct to a COA
phase sequence across multiple units, and the Phase End Condition is where a
multi-unit "advance when all arrived" gate would live.

### 5. Ground clamp semantics

There are THREE distinct clamp-related mechanisms; conflating them is a known
source of our confusion. Keep them separate.

**(a) Create-time / placement altitude (NOT clamping).** When you place or edit
an object you choose a reference frame
[SimObjectsSection\ObjectCreation\vrf_setAltitudeInDialogBox.htm]:
- "Above Sea Level. The distance above sea level. If the terrain is higher than
  the altitude entered, the entity will be below the surface of the ground."
- "Above Terrain. The distance above the terrain at this location."
Same hazard for route vertices: "Set Altitude Above Sea Level ... This could
result in some vertices being below ground."
[SimObjectsSection\ObjectCreation\vrf_setRouteVertexAltitude.htm]. Dynamic
altitude (Alt + mouse wheel) is "set relative to the terrain"
[SimObjectsSection\ObjectCreation\vrf_setAltitudeDynamically.htm]. Paste can
apply prior altitude as above-terrain or above-sea-level
[SimObjectsSection\ObjectCreation\vrf_pasteAltitude.htm]. This is exactly the
buried-birth mechanism: an object born "above sea level" at an altitude below
local terrain elevation is created underground. There is no create-time
auto-clamp in the GUI dialog; the reference frame is the author's/caller's
responsibility. (The remote API adds an explicit create-time clamp switch,
createEntity groundClamp=true / DtIfCreateVrfObject::clampToGround - see 0.3
section 1a - which snaps Z to the nearest polygon and is the fix that cured the
buried-birth freeze.)

**(b) Display/visual ground clamping (front-end only, cosmetic).**
[SharedTopics\3Dnovaentities\GroundClamping.htm]: "Because of differences in
terrain databases among exercise participants, entities can sometimes appear to
be underground or hovering above the terrain surface. To compensate, VR-Forces
can clamp land and sea entities to the terrain surface. When ground clamping is
enabled, VR-Forces keeps an entity anchored to the terrain surface regardless of
the altitude data contained in its state update." This changes the VISUAL only;
it does not correct the simulated/published altitude. Configurable cutoff
distance: "The setting for the Ground Clamping Cutoff Distance Scale maximizes at
100 meters. If you set it to the maximum, the distance is infinite and all
entities are ground-clamped." [SharedTopics\3Dnovaentities\GroundClampingConfig.htm].
It is "not useful" in aggregate-level scenarios (no 3D models)
[SharedTopics\3Dnovaentities\GroundClamping.htm]. Because this is display-only,
a buried entity can be visually clamped to the surface while its real state stays
underground - a trap for eyeball verification (WatchVrf displacement, not the 3D
view, remains the oracle).

**(c) Simulation-side runtime clamp (movement model).** Moving ground entities
follow the terrain surface as part of the movement model
[Tasks\TasksAssign\EntityMovement.htm]: "Vehicles follow vector features and
ground clamp to the terrain. If there is a crater in the vehicle's path, the
vehicle drives through it. If a bridge is destroyed, the road vector remains and
the vehicle drives across the terrain that the bridge previously spanned." For
human entities there is a separate fidelity knob
[DataRequests\EntityLevel\vrf_setGroundClampFideli.htm]: high fidelity clamps to
terrain, low fidelity clamps to the navigation mesh ("can appear to move through
the terrain"); low fidelity "only works in navigation areas." Cultural features
have an "Is Gravity Aligned" option
[SimulationModels\EntityLevelScenariosEntities\GroundClampingCulturalFeatures.htm].

Documented behavior above/below terrain: create-time above-sea-level below the
surface = underground (a); the movement model re-clamps moving ground vehicles
to the surface (c). The Guide does NOT state what happens to a STATIONARY entity
created underground before it is tasked (does it get pulled to the surface at
sim start, or stay buried until it moves?), nor whether ground-clamp applies over
un-paged terrain. See NOT-COVERED.

### 6. Terrain paging / playbox (EXHAUSTIVE)

**"Playbox" is not a VR-Forces term.** A full-tree search for playbox / play box
/ area of interest returns nothing in the Guide. The relevant native concept is
terrain PAGING (static local terrains) and terrain STREAMING (from a terrain
server); do not expect the docs to speak of a "playbox radius."

**How paging works** [Terrain\Introduction\vrf_terrainPaging.htm] (verbatim,
load-bearing):
- "Terrain paging breaks up a database into multiple cells that can be loaded
  independently."
- "The simulation engine always loads terrain at the highest level of detail."
- "In general, the simulation engine does not load terrain until it needs the
  terrain to support the objects in the scenario. ... if you create a new
  scenario and specify a paged terrain, the simulation engine does not load
  terrain until you create a simulation object. Then it only loads the terrain
  page required for the object."
- "When you run a scenario, as simulation objects move, the simulation engine
  tries to predict which terrain pages it will need to support the simulation
  objects and tries to page them in before the simulation objects need them. If
  the total polygon count required to page in required terrain exceeds the
  maximum polygon count configured, VR-Forces pages out (removes) unused pages.
  The simulation engine pages out the least recently used data."
- Priorities: "If a simulation object is moving, paging in terrain that it needs
  is a higher priority than predictive paging."; "Blocking terrain calls have a
  higher priority than non-blocking calls."; "Paging out terrain to recover
  memory is a higher priority than paging in terrain."

**Extent / bounds of the paged area.** The Guide does NOT define a fixed paged
extent or radius. Paged coverage is dynamic and object-driven: pages are brought
in around objects and along predicted motion, and least-recently-used pages are
evicted when the polygon budget is exceeded
[Terrain\Introduction\vrf_terrainPaging.htm]. Streaming (VR-TheWorld and other
servers) is the whole-earth mechanism: "Only the data you need at a particular
location is loaded." [Terrain\Introduction\vrf_terrainStreamingConc.htm]. Both
"provid[e] access to large amounts of data without loading all of that data into
memory at any one time" [Terrain\Introduction\vrf_usingLargeTerrains.htm]. The
only "bounds" surface is a debug/console command: on the back-end console, "pbv"
"Prints page bounding information to the console"
[AddingContent\Terrain\Performance\vrf_terrainPagingInfoCommands.htm] - i.e. the
paged bounding info exists but only as runtime console output, with no documented
default extent value.

**When prediction fails, and the remedy** [AddingContent\Terrain\PagedStreaming\vrf_manuallyPageInTerrain.htm]
(verbatim, load-bearing for the 18.4 km stop-radius question):
"VR-Forces pages in terrain based on the location of simulation objects and an
algorithm that predicts where terrain will be needed. In most cases VR-Forces
can predict the need for terrain and page it in without affecting the
performance of the exercise. However there may be cases where it cannot, for
example if there are many simulation objects over a wide area or many
fast-moving simulation objects. Therefore, VR-Forces also allows you to specify
areas where you want the terrain to be paged in."
The remedy is a Terrain Page-in Area tactical graphic (Simulation Effect Object)
that can be attached to a moving object "to ensure that terrain in front of the
simulation object gets paged in." "Terrain page-in areas only affect the
back-end." Paging budget is tuned by OSG_MAX_PAGEDLOD (front-end, default 200)
and GDAL_CACHEMAX (streaming cache, default 200 MB)
[AddingContent\Terrain\Performance\Configuring_Paged_Streaming_Terrains.htm].

**DOCUMENTED behavior of ground movement at/beyond the paged boundary: NONE.**
This is the sharpest gap. A full-tree search (beyond / outside / edge of the
terrain / off the terrain / no elevation / leaves the) in the Terrain section
returned no matches. The Guide never states what a ground mover does when it
reaches un-paged terrain, un-streamed regions, or the edge of available
elevation data. The observed ~18.4 km common stop-radius from the AO and the
runaways terminating underground/offshore are NOT explained by any documented
paging boundary behavior. Best-supported reading from what IS documented:
predictive paging "cannot" keep up "if there are many simulation objects over a
wide area or many fast-moving simulation objects"
[AddingContent\Terrain\PagedStreaming\vrf_manuallyPageInTerrain.htm], and a
moving object's terrain-page-in is a blocking, higher-priority call
[Terrain\Introduction\vrf_terrainPaging.htm] - so an object outrunning its paged
terrain is a documented failure mode, but the resulting motion (stop vs warp vs
sink) is undocumented. => MAK support question, with the Terrain Page-in Area
attach-to-object remedy as the first thing to try in Phase 1.

### 7. Status icons and warning badges (EXHAUSTIVE) - the yellow triangle IDENTIFIED

**The yellow warning triangle = the Object Console Warning Icon (a.k.a. alert
icon / exclamation point).** Primary evidence: the Guide's own figure for this
feature, [graphics\vrf_alerticon.png] (referenced from
[SimObjectsSection\ObjectInfo\vrf_displayMessageIcon.htm] as
vrf_alerticon_thumb_175_0.png), shows a yellow triangle enclosing a black "!"
placed immediately to the LEFT of a unit's 2D icon (an M1A2 in the figure). This
matches exactly the badge seen next to most of our units.

What it is [SimObjectsSection\ObjectInfo\vrf_displayMessageIcon.htm] (verbatim):
"VR-Forces can display a notification icon when an object receives a message on
its object console. This option is enabled by default ... Icons are displayed
next to the entity icon in the display window, next to the entity's listing in
the Objects List Panel, and if the entity has recently been selected, on the
Last Selected Object Panel."

Behavior:
- Enabled by default; toggle at Settings > Display > Entity Display Settings >
  "Show Object Console Warning Icon" [same file].
- Clearing test / how to read it: "The icon is not displayed if the Information
  dialog box is open. If an icon is displayed for an object, when you open its
  Information dialog box, the icon is removed." [same file]. Also
  [SimObjectsSection\ObjectInfo\vrf_objectConsoleSummary.htm]: "When VR-Forces
  sends a message to a simulation object's console, it displays an exclamation
  point next to the entity until you view the message ... The messages in the
  Object Console Summary Panel also display an exclamation point until you open
  the Information dialog box for that entity or click Acknowledge All."
- To see WHAT it means for a given unit: open that object's Information dialog >
  Object Console section, or View > Object Console Summary Panel to see all
  messages at once [SimObjectsSection\ObjectInfo\vrf_objectConsoleSummary.htm],
  [SimObjectsSection\ObjectInfo\vrf_configureconsolemsg.htm]. The console shows
  "messages sent from the simulation engine, from a simulation object's plan,
  from other simulation objects, and from scripts."
- Notification levels [SimObjectsSection\ObjectInfo\vrf_setconsolenotifylevel.htm]:
  0 fatal only; 1 warnings+fatal; 2 some diagnostics+warnings+fatal; 3 verbose;
  4 debug. "The default notification level is 2." Set per object, or globally via
  the objectConsoleNotifyLevel parameter in vrfSim.mtl. (Minor doc
  inconsistency: [vrf_displayMessageIcon.htm] says "the default notification
  level is Warn and objects do not typically get messages at this level," while
  [vrf_setconsolenotifylevel.htm] says the default is 2. Either way the icon
  means a message at or above the object's level was posted.)

Load-bearing takeaway: the yellow triangle is NOT cosmetic and does NOT have a
single fixed meaning. It means "this object has an unviewed console message at or
above its notify level" - almost certainly a warning (level 1) or higher, since
the default does not surface routine info. If MOST units show it, MOST units are
emitting warnings/errors that we have never read. The next live session must open
the Object Console Summary Panel (or each unit's Information > Object Console) and
capture the actual message text - this is likely where VR-Forces has been telling
us what is wrong (path-planning failure, terrain not paged, task failure, etc.).
The badge's specific meaning is only knowable by reading the message; the User's
Guide does not enumerate a fixed set of triangle sub-types. (Remote-side, this
text is capturable via addObjectConsoleMessageCallback / setObjectNotifyLevel -
0.3 section 5 channel 4 - so the port could log every unit's console warnings
without the GUI.)

**Other status decorations (for completeness, so we do not misread them as the
triangle):**
- Ghosted icons = an AGGREGATION display aid (semi-transparent subordinate icons
  under a collapsed unit), configured at Settings > Display > Unit Display
  Settings; not a warning [SharedTopics\3D_aggregates\GhostedIcons.htm].
- Symbol decorations = TEXT labels (name, speed, heading, etc.) toggled per label
  type on the Symbol Decoration Settings page; heading indicator; LOD-out by
  observer altitude (default 100,000 m) [SharedTopics\3Dnovaentities\EntityLabels2DIcons.htm].
  These are text, not a triangle.
- 2D icon color/transparency (Objects > Decorations > Set Icon Color) is a
  session-only cosmetic categorization [SimObjectsSection\ObjectInfo\vrf_configuring2Dicons48904891.htm].
- OOB gray vs colored icon = not-instantiated vs instantiated
  [SimObjectsSection\BattleOrder\vrf_orderOfBattleTab.htm].
- Terrain correlation warning is a modal DIALOG ("a warning message if the
  terrain loaded in the front-end might not correlate well with the terrain
  loaded in the back-end"), NOT a per-unit badge - so it is not the triangle
  [Introduction\Starting\vrf_displayingTerrainCorrelationWarnings.htm]. (Still
  worth heeding: our runs may be raising it.)
- Damage/frozen state is carried in the RPR-FOM appearance bits
  (immobilized/damageState/frozen), read remotely per 0.3 section 5 channel 2 -
  these drive model/icon changes, not the yellow triangle.

### 8. Time management (EXHAUSTIVE) - the 20x question

**Exercise clock + modes.** VR-Forces "maintains an exercise clock" tracking
elapsed sim time, date, time of day [Introduction\Concepts\vrf_timeConcepts.htm].
Three clock modes [Introduction\Concepts\vrf_runFasterThanRealTime.htm]:
- Variable-Frame Run-To-Complete ("variable-frame"): "advances simulation time by
  the amount of time passed since the last time the exercise clock was ticked.
  This mode is typical for distributed, interactive simulations. It does not
  provide repeatable results."
- Fixed-Frame Best-Effort ("fixed-frame"): "advances simulation time by a fixed
  amount each frame, even if a frame takes longer than the fixed amount to
  compute. If the simulation takes less than the fixed frame time to compute, it
  waits ..."
- Fixed-Frame Run-To-Complete ("fixed-frame-run-to-complete"): fixed amount per
  frame, and does not wait if faster; "not suited for interactive use. It ...
  disables the Simulation Time Scale Toolbar."
Set via the frame-mode scenario parameter; frame-time must be non-zero for fixed
modes ("A value of zero for frame time prevents simulation time from advancing")
[Scenarios\Files\vrf_scenarioParams.htm]. Default scenario parameters shown there
are frame-mode "variable-frame", frame-time 0.1, time-multiplier 1.0.

**Faster than real time = a multiplier on tick time**
[Introduction\Concepts\vrf_runFasterThanRealTime.htm]: "VR-Forces supports
simulation at rates faster than real-time by applying a multiplier to the tick
time." Documented caveats (verbatim, all load-bearing for 20x):
- "Do not run your simulation at faster than real-time if you are interacting
  with other real-time simulations."
- "Running a simulation faster than real-time can cause performance of
  simulation object models to degrade."
- "If you increase the speed at which a scenario runs, the frame rate is reduced
  and performance of models may degrade."
  [Scenarios\CreateRun\vrf_automaticallyChangin.htm]
- "When you increase the time scale in scenarios that have many entities,
  particularly if they are fast moving entities such as aircraft, simulation
  quality may degrade." [Scenarios\CreateRun\vrf_automaticallyChangin.htm]

**The scenario parameter** [Scenarios\Files\vrf_scenarioParams.htm]:
"time-multiplier ... Specifies whether a scenario runs in real time or faster
than real time. A value greater than 1 runs the simulation faster than real
time. You can set this value dynamically using the Time Multiplier toolbar. If
you are running a scenario using HLA Time Management, it is strongly recommended
that you set time-multiplier to 1." (Remote-settable via
DtVrfRemoteController::setTimeMultiplier - 0.3 section 6.)

**20x is beyond the GUI's default range.** The Simulation Time Scale Toolbar
"range for the Simulation Time Scale Toolbar is 1 to 15" by default, with default
buttons 1;2;5;10;15 [Scenarios\CreateRun\vrf_automaticallyChangin.htm]; confirmed
in the installed config [C:\MAK\vrforces5.0.2\appData\settings\vrfGui\default_GuiSettings.grsx]:
myTimescaleLow=1, myTimescaleHigh=15, myTimescaleButtons 1;2;5;10;15. Running at
20x (which we set via the time-multiplier scenario parameter / setTimeMultiplier,
not the toolbar) runs ABOVE the value the GUI exposes by default. Note also "If
you are running VR-Forces with time management, the Simulation Time Scale Toolbar
has no effect on simulation speed." [Scenarios\CreateRun\vrf_automaticallyChangin.htm]

**The fast-forward auto-tuning mechanism - EXISTS but is DISABLED on this
install.** [Scenarios\CreateRun\vrf_automaticallyChangin.htm]: at high time
scale, VR-Forces can auto-swap the dead-reckoning algorithm, frame mode, and
frame time via ./appData/settings/vrfSim/fastForwardSettings.mtl; "When you
change the time scale, VR-Forces finds the highest play-speed specified in this
file that is less than the current speed and updates the settings accordingly. If
the current speed is less than the lowest value, default settings are used." The
documented example switches to dead-reckon-algorithm Static, fixed frame, frame
time 1.0-2.0 s at play-speed 3-6. CHECKED the actual installed file
[C:\MAK\vrforces5.0.2\appData\settings\vrfSim\fastForwardSettings.mtl]: every
fast-forward-entry is commented out (the block is empty). Therefore, at 20x on
this machine, NO dead-reckoning/frame-mode override is applied - default settings
are used at all speeds. This falsifies the tidy hypothesis that a Static/large-
frame-time fast-forward profile is auto-warping our movers; it is not enabled
here.

**Dead reckoning vs time advancement** [Introduction\Concepts\vrf_advancingTimeUsingSimulationTimeOrWallClockT.htm]:
by default time advances on simulation time, and "simulation objects dead reckon
correctly in the GUI when you run faster or slower than real time. ... However,
simulation objects published by non-VR-Forces federates will not dead reckon
correctly." There is an option to advance on wall-clock time instead. Time of
day defaults to 10:00 AM and affects sensing.

**HLA/time-managed backend.** Time management is enabled at the application (not
scenario) level; a time-managed back-end in a fixed-frame mode becomes a
regulating+constrained federate; "If the frame-mode is fixed-frame-run-to-
complete, the back-end tries to advance simulation time as fast as possible,
independent of wall-clock time." [Introduction\Starting\vrf_enableTimeMgtBackend.htm].

Assessment for our 20x runs (separates verified from hypothesis):
- VERIFIED (docs + config): the Guide's only stated consequences of high
  multipliers are frame-rate reduction and "model performance / simulation
  quality may degrade," with fast movers over wide areas called out; 20x exceeds
  the GUI's default 1-15 range; the auto-tuning safety net is disabled here.
- HYPOTHESIS (to test in Phase 1.4, NOT a doc claim): with default variable-
  frame mode, sim time advances by (wall time since last tick) x multiplier, so a
  single stalled frame (e.g. a blocking terrain-page-in call, Section 6) advances
  many seconds of sim time in one integration step at 20x, which could produce a
  large one-step position jump for a moving entity (a "warp"). The docs support
  the ingredients (blocking terrain calls; fast movers outrunning paging;
  degraded quality at high scale) but do NOT state this mechanism. Phase 1.4
  (native move at 20x vs real time) is the right falsification test; if native
  units warp only at 20x, the multiplier is implicated; if they warp at 1x too,
  it is not.

### 9. Roads

Ground route-following uses road networks only for the road-specific tasks, and
only when a network exists and is connected.

- Which tasks use roads [Tasks\TasksAssign\EntityMovement.htm]: "The road driving
  tasks are Move to Waypoint (Plan Along Roads) and Move to Location (Plan Along
  Roads). The generic movement tasks are Move to Location and Move to Waypoint.
  Road driving tasks are valid only for ground vehicles. If you give a road
  driving task ... to any other type of entity, it will fail." Plus Move Along
  Route with "Treat Route as Road" selected, and "Use Roads in pattern of life
  default plans."
- Generic (non-road) movement ignores roads: "When you give a simulation object
  a generic Move To task, it moves directly towards its destination. If
  navigation data is available, it plans a path using that data. If no navigation
  data is available, the entity avoids obstacles, but otherwise does not pay
  attention to the terrain except to the extent that it cannot move on certain
  soil types or terrains that are too steep." [same file]
- Road path-planning sequence [same file], [Tasks\MovementTasks\LocationPlanAlongRoads.htm]:
  move to nearest point on the road network, drive the network to the point
  nearest the goal, then off-road to the goal (unless Stay On Road When
  Complete).
- Road driving behavior [Tasks\TasksAssign\RoadDrivingBehavior.htm]: "It clamps
  to the road and does not deviate from it. It goes around corners accurately. It
  ignores any obstacles that are near to the road ... The vehicle only responds
  to obstacles that block the road. This does not include craters. If a vehicle
  is blocking its progress and there is an adjacent lane in the road that is
  clear, the entity passes the blocking vehicle."
- Network requirement [Tasks\TasksAssign\EntityMovement.htm]: "The road driving
  feature in VR-Forces requires that the road vectors making the network be
  connected at their edges. If your terrain's data does not have accurate
  positions for road ends, causing them to be disconnected, VR-Forces might not
  be able to plan a path along those roads."
- Side of road (default right; before load/rewind only)
  [Tasks\TasksAssign\RoadSideToDriveOnSpecify.htm].
Note the distinction between road networks (vector road features, above) and
navigation data (Gameware topological nav meshes for obstacle-avoiding path
planning) [Introduction\Concepts\vrf_advancedTerrainNavigation.htm] - generic Move
To uses nav data if present; road tasks use the road vector network.

### WHAT THE USER'S GUIDE DOES NOT COVER (=> MAK support / probe, not a doc lookup)

1. Movement behavior at/beyond the paged (or streamed) terrain boundary. No
   documented stop / warp / sink behavior; no fixed paged extent or radius; the
   observed ~18.4 km stop-radius and the underground/offshore runaways are
   unexplained by the Guide. (Section 6.) HIGH priority MAK question; try the
   attach-to-object Terrain Page-in Area remedy first.
2. Numeric arrival tolerance for Move to Location / Move to Waypoint (how close
   is "arrived"?). Only the unit leading-edge rule for Move Along Route and the
   10-second window for Time-on-Target are quantified in the Guide. (The remote
   API exposes a per-task override, DtMoveToTask::setAtDistance - 0.3 section 4 -
   which is our lever for a truthful-arrival radius.) (Section 3b.)
3. What happens to a STATIONARY entity created below terrain (buried-birth)
   before it is tasked: does sim start pull it to the surface, or does it stay
   buried until it moves and the movement-model clamp (5c) engages? Display
   clamp (5b) hides it visually either way. (Section 5.) Directly relevant to the
   fixed buried-birth bug's residue.
4. Exact warp/runaway mechanism at 20x. The Guide gives only "quality may
   degrade"; it does not model per-step position jumps vs multiplier. The
   fast-forward auto-tuning that could have explained it is disabled on this
   install. (Section 8.) Falsify via Phase 1.4.
5. Enumerated meanings of the yellow triangle. The Guide documents ONE console
   warning/alert icon whose meaning is "unviewed console message at/above notify
   level"; the actual per-unit meaning is only in the console message text, which
   must be read live. There is no documented catalog of triangle sub-types.
   (Section 7.)
6. The precise radio-message protocol a disaggregated unit uses to task
   subordinates ("part of the modeling ... built into the VR-Forces application"
   [Modeling\EntityLevel\vrf_aggregateTaskBehavior.htm]) - opaque in the Guide;
   relevant if subordinate tasking is where our piles/frozen members originate.

### Cross-references

- 0.1 content catalog: Section 1's "palette is not the full type set" and the
  echelon/leader/subordinate-order facts constrain what a "real type" is.
- 0.3 remote-API audit: Section 1 (Superior / Aggregate As / OOB) maps to
  addToOrganization / setSuperior / createAggregate(createSubordinates) /
  sendVrfObjectCreateMsg(setSuperior); Section 4 (plans, triggers, sync-matrix
  phase-end) maps to assignPlanByName / DtPlanBuilder; Section 3b completion maps
  to the current-tasks subscription + DtMoveAlongTaskState::currentVertex;
  Section 7 maps to addObjectConsoleMessageCallback.
- 0.5 scnx trick: Section 1 (unit = force-subordinate + echelon IDs + ordered
  subordinates + disaggregated-to-attach) and Section 2 (aggregation state,
  DtVrfAggregateStateRepository) are the fields to diff a native .scnx unit
  against our remote creations.
- Phase 1 live session: Section 7 (open the Object Console Summary Panel - read
  the triangles), Section 6 (attach a Terrain Page-in Area to the marcher that
  crosses 18.4 km), Section 8 (repeat one move at 1x vs 20x), Section 3b (watch
  whether unit Move Along Route completes on leading-edge while members lag).

---

## 0.3 Remote controller API surface

Scope: the complete creation / organization / tasking / state / monitoring / scenario
surface of the MAK remote-control interface, class `DtVrfRemoteController`, so the
rebuilt mapping layer can pick the RIGHT calls instead of only the two creates the GMU
interface used.

Primary source read in full: `C:\MAK\vrforces5.0.2\include\vrfcontrol\vrfRemoteController.h`
(2431 lines). Cross-referenced headers and the canonical example
`C:\MAK\vrforces5.0.2\examples\remoteControl\` (main.cxx, textIf.cxx) are cited inline.

### Orientation facts (read these first)

- The class is a pure command/encode/decode front end. Its own doc states it "does NOT
  instantiate reflected lists or maintain lists of the objects in the simulation" and
  needs an active `DtCommunicationManager`, not necessarily a live connection
  (vrfRemoteController.h:195-197). Consequence: reading back simulation state is a
  SEPARATE subsystem (see section 5) - the controller sends, it does not observe.
- Addressing: nearly every call takes a trailing `const DtSimulationAddress& addr =
  DtSimSendToAll`. Multi-backend scenarios need per-backend addressing; single-backend
  (our case) can use the default.
- Object identity is `DtUUID` at the control layer (marking-text based). Creation calls
  optionally accept a caller-chosen `startingUUID`; if omitted a UUID is generated and
  returned via the create callback. "Use this UUID to create object (if specified). If
  the UUID exists it will be regenerated on creation" (ifCreateVrfObject.h:105).
- Two API tiers exist: convenience methods (`createEntity`, `createAggregate`,
  `moveAlongRoute`, `setAltitude`, ...) that fill a fixed subset of fields, and the
  generic message builders (`sendVrfObjectCreateMsg`, `sendSetDataMsg`, `sendTaskMsg`,
  `assignPlanByName`) that expose the FULL underlying message. The convenience methods
  are a strict subset - several creation knobs (superior-at-create, add-to-orbat,
  initial formation, pitch) are reachable ONLY through the generic builders.

### 1. CREATION

#### 1a. Entities

`createEntity` - two overloads (vrfRemoteController.h:1287 and :1301; the second adds a
`DtVrfObjectCreatedCallbackFcn fcn, void* usr` up front):

```
virtual void createEntity(
   const DtEntityType& type,            // DIS/RPR enumeration (kind,dom,country,cat,subcat,spec,extra)
   const DtVector& geocentricPosition,  // geocentric (ECEF) meters - NOT lat/lon
   DtForceType force,                   // DtForceFriendly / DtForceOpposing / DtForceNeutral
   DtReal heading,                      // see "Heading units" note below
   const DtString& uniqueName = null,   // marking text; must be unique if given
   const DtString& label = null,
   const DtSimulationAddress& addr = DtSimSendToAll,
   bool groundClamp = true,             // see groundClamp note
   const DtUUID& startingUUID = nullUUID) const;
```

- `groundClamp` (default TRUE): maps to `DtIfCreateVrfObject::clampToGround`
  (ifCreateVrfObject.h:210-214). "If True (the default) the object will be created and
  placed on the nearest polygon. Otherwise, it will be created and then placed at the
  altitude specified in the position. Subsurface entities will be constrained between
  the water surface and bottom." This is the create-time analogue of the buried-birth
  altitude class: with groundClamp=true the supplied Z is ignored and the entity is
  snapped to terrain; with groundClamp=false the supplied geocentric Z is authoritative.
- `heading` UNITS - RESOLVED to RADIANS at this API level (0=north, clockwise). The wire
  field is radians (ifCreateVrfObject.h:50-51,150-152), and the port's facade explicitly
  converts degrees->radians before calling: `createEntity(..., (DtReal)(headingDeg /
  kDegRadFactor), ...)` (VrfFacade.cpp:411). BEWARE the asymmetry: the post-create setter
  `setHeading` is DEGREES (vrfRemoteController.h:1411) but create-time heading is RADIANS.
  LATENT BUG in the oracle + MAK example: both pass raw `90.0` to `createEntity` heading
  (c2simVRFinterface .../C2SIMinterface.cpp:848; examples textIf.cxx:1352) as if degrees;
  as radians 90.0 wraps to ~2.04 rad (~117 deg), so spawned entities faced a wrong initial
  heading. The port corrected this by converting. Aggregates are typically created with
  heading 0 anyway (the oracle drops the computed heading, passing literal 0 -
  C2SIMinterface.cpp:925), so aggregate spawn orientation was not controlled at all.

#### 1b. Aggregates (units)

Three `createAggregate` overloads:

```
// (A) callback form, template-driven subordinates - vrfRemoteController.h:1313
virtual void createAggregate(
   DtVrfObjectCreatedCallbackFcn fcn, void* usr,
   const DtEntityType& type, const DtVector& geocentricPosition,
   DtForceType force, DtReal heading,
   const DtString& uniqueName, const DtString& label = null,
   const DtSimulationAddress& addr = DtSimSendToAll,
   DtAggregateState initialAggregateState = DtDisaggregated,
   const DtUUID& startingUUID = nullUUID,
   bool createSubordinates = false);      // <-- the "with real structure" switch

// (B) plain form, aggregate-AS an explicit entity list - vrfRemoteController.h:1327
virtual void createAggregate(
   const DtEntityType& type, const DtVector& geocentricPosition,
   DtForceType force, DtReal heading,
   DtList* entityNames = 0,               // names of ALREADY-CREATED entities to enclose
   const DtSimulationAddress& addr = DtSimSendToAll,
   DtAggregateState initialAggregateState = DtDisaggregated,
   unsigned int requestId = 0, const DtUUID& startingUUID = nullUUID) const;

// (C) callback + explicit entity list - vrfRemoteController.h:1341
```

- `initialAggregateState` (`DtAggregateState`, default `DtDisaggregated`). Enum is the
  DIS Aggregate-State PDU enum, disEnums.h:2750-2758:
  `DtOtherAggregateState=0, DtAggregated=1, DtDisaggregated=2, DtFullyDisaggregated=3,
  DtPseudoDisaggregated=4, DtPartiallyDisaggregated=5`.
  DIS semantics: "aggregated" = the unit is represented/simulated as a single aggregate
  marker; "disaggregated" = it is broken down into its individually-simulated member
  entities. The default `DtDisaggregated` means the aggregate is instantiated with its
  member entities present. (Interacts with `createSubordinates` / `entityNames` for where
  those members come from - template vs explicit list. Exact runtime behavior of each
  enum value at Mojave is a probe question, not a header fact.)
- `createSubordinates` (bool, default FALSE, overload A only): maps to
  `DtIfCreateVrfObject::setCreateSubObjects` (ifCreateVrfObject.h:193-199). When TRUE and
  the type is an organized/aggregate template, the OPD/parameter file's subobject list is
  instantiated - i.e. the aggregate is born WITH its real subordinate composition from the
  installed template. When FALSE, a bare aggregate leader is created and the caller must
  populate it (attach entities, see section 2).
- `entityNames` (overloads B/C): the "aggregate-AS" path - enclose an existing set of
  already-created entities. Bottom-up composition.

WITH real org/subordinate structure vs BARE:
- WITH structure: `createAggregate(..., createSubordinates=true)` (template subobjects);
  `createSimulationObjectGroup` (pre-authored .sogx group, section 1d);
  `loadScenario` / `newScenario(baseScenario)` / `importScenario` (full scenario org tree).
- BARE (must post-populate): `createEntity` (single entity); `createAggregate` with
  `createSubordinates=false` or with an explicit `entityNames` list; `createObject`.
- The MAK example demonstrates the BARE bottom-up pattern (createSubordinates left false):
  create the leader, create each subordinate entity separately, and in the create callback
  call `addToOrganization(subUuid, aggUuid)` once the leader exists (textIf.cxx:587-639,
  callback :1236-1270), with an explicit create-order/timing hazard warning.
- CORRECTION to the intuition that "GMU used bare creates": the GMU C2SIM automation does
  NOT use the bottom-up pattern. Both the oracle C2SIM path and the port call
  `createAggregate(..., createSubordinates=true)` (C2SIMinterface.cpp:925,951,986,1020;
  VrfFacade.cpp:421, default true per VrfFacade.h:239) - template-driven subordinates. The
  `addToOrganization` bottom-up pattern appears ONLY in the inherited demo console
  (textIf.cxx), never in the C2SIM flow. So the real defect is NOT "bare aggregates" - it
  is that (a) the entity TYPE (DIS enumeration) did not resolve to a real installed
  template, so `createSubordinates=true` populated the GENERIC fallback
  (`Ground_Aggregate.entity`, ~4 anonymous subordinates - groundwork plan sec 0), and
  (b) no organization calls means the org tree is one flat aggregate deep (no
  company->platoon->vehicle nesting). `.sogx` and `sendVrfObjectCreateMsg(superior=...)`
  remain the unused paths that could build correct multi-echelon structure.

#### 1c. Generic full-control create

`sendVrfObjectCreateMsg` (vrfRemoteController.h:1593-1615) exposes the entire
`DtIfCreateVrfObject` payload, including fields the convenience creates do NOT reach:
`createSubObjects`, `clampToGroundFlag`, `initialFormation` (aggregate formation at
birth), `initialAggregateState`, `overlayParent`, `uuidForCreation`, `extendedData`,
`initialAdminContent`/`initialInterfaceContent`. The message class adds still more
(ifCreateVrfObject.h): `setSuperior(DtUUID)` :258 (set the org parent AT creation),
`setAddToOrbat(bool)` :254, `setAddToOrganization(bool)` :201-208 (default TRUE - an
organized object is auto-placed directly below its force-level aggregate unless cleared),
`setInitialFormation` :216-221, `setPitch` :154-163 (only honored for non-ground-clamped
entities), `setPublishObject` :224-229, `setConnectToRadioNetwork` :231-236. Setting the
superior at creation time (via this path) avoids the create-order timing hazard the
example's bottom-up pattern has to manage manually.

`createObject` (vrfRemoteController.h:1268): minimal create (heading 0, vertices, name)
for an entity or overlay object.

#### 1d. Simulation object group (pre-authored composite unit)

`createSimulationObjectGroup(addr, groupName, createPosition, offsetRotation, createFlags,
force, storedInZipFile)` (vrfRemoteController.h:492-494): instantiates the contents of a
saved simulation-object-group (a `<name>.sogx` file in the object-groups directory of the
loaded simulation model set) at a geocentric position. A .sogx is a pre-authored bundle of
objects WITH their organization, formations and controllers - so this is a first-class
"create a real, structurally-complete unit in one call" path. Its inverse,
`saveSimulationObjectGroup(smsDir, groupName, desc, flags, addr, ...)` (:787-790), saves a
selected set of live objects back to a .sogx. Pairing these two is a way to author a unit
once in the GUI and re-stamp it remotely.

#### 1e. Control objects (route/area/overlay geometry)

All take geocentric vertices, optional unique name/label, optional startingUUID, and have a
plain + callback overload:
- `createWaypoint` :978 / :988
- `createRoute` :1010 / :1020  (vertices = `DtList` of `DtVector`)
- `createPhaseLine` :1042 / :1053
- `createControlArea` :1078 / :1088  (vertices must form a convex polygon)
- `createObstacle` :1111 / :1121
- `createDisaggregationArea` :1145 / :1156  (auto-disaggregates aggregates that enter)
- `createOverlayObject` :1186  (annotation/control object with color, parent, appearance)
- `createIndirectArtillery` :1243
Matching `modify*` methods exist for each (e.g. `modifyRoute` :1029, `modifyControlArea`
:1097). Routes/waypoints created here are what tasking (section 4) references by UUID.

### 2. ORGANIZATION (attach / detach / superiors, post-create)

- `addToOrganization(objectId, newSuperiorId, addr)` :1354 - "Adds the specified object
  as a subordinate to the specified superior. Object will be detached from any superior it
  currently belongs to first." This is the header-line-1352 call the task flagged; it is
  the primary post-create attach.
- `addToForceLevelOrganization(uuid, addr)` :1362 - attach directly under the force-level
  aggregate for the object's force.
- `removeFromOrganization(uuid, addr)` :1368 - detach from whatever aggregate, leaving it
  with no superior.
- `setSuperior(uuid, superior, addr)` :1495 - set-data variant ("set-superior",
  radioMessageTypes.h:213). "If superior is blank will set superior as force of the
  current item."
- `setTaskedBySuperior(uuid, addr)` :1500 - mark the entity as tasked by its superior.
- `setLabel(uuid, label, addr)` :1375 - NOTE on an aggregate LEADER it applies to the
  whole aggregate (the same "leader change propagates to aggregate" note recurs on most
  setters).
- `setOverlayParent(uuid, label, addr)` :1383.
- Aggregate shaping: `setAggregateFormation(leader, formationName, addr)` :1565,
  `reorganizeAggregate(leader, addr)` :1572 (only needed when auto-reorganize is off).
Working attach pattern (from the example, textIf.cxx:1236-1270): create leader ->
create subordinates -> on each subordinate's create callback, `addToOrganization(sub,
leader)`. Alternatively set the superior at creation via `sendVrfObjectCreateMsg`
(section 1c) to avoid the ordering dependency.

### 3. STATE / DATA (set on an existing object)

Convenience setters (all `(uuid, value..., addr=DtSimSendToAll)`; on an aggregate leader
they apply to the whole aggregate):
- `setAltitude(uuid, altitude_meters, aboveGroundLevel=false, addr)` :1390. The
  `aboveGroundLevel` flag: FALSE = MSL, TRUE = AGL (already verified TRUE=AGL). "Set
  altitude (in meters)."
- `setLocation(uuid, DtVector coord, addr)` :1397 - geocentric coordinates.
- `setHeading(uuid, degrees, addr)` :1414 - explicitly DEGREES.
- `setSpeed(uuid, m/s)` :1474 (ordered speed) and `setCurrentSpeed(uuid, m/s)` :1481.
- `setForce` :1421, `setAppearance(uuid, int appearance)` :1402, `setCapabilities` :1407.
- `setResource(uuid, resourceType, value)` :1450 (e.g. "fuel"; example textIf.cxx:1763).
- `setDestroyed(uuid)` :1541, `restore(uuid)` :1535 (restore to start state),
  `setConcealed(uuid, bool)` :1506.
- Others: `setTarget` :1488, `setEmitter` :1509, `setIff` :1519, `setEmergencyBeacon`
  :1514, `setLifeformWeaponState` :1547, `setLifeformPosture` :1559, `setSystemEnabled`
  :1552, `setSectorOfResponsibility` :1467, `setRulesOfEngagement` :1459.
- FREEZE: there is NO dedicated per-entity "setFreezeMovement" remote call. Substitutes:
  scenario-wide `run()`/`pause()` (section 6); the appearance frozen bit via
  `setAppearance` (readable back as `frozen()`, section 5); `wait`/`waitDuration` tasks
  (section 4); or `restore()` to reset to start state. (The "entity-freeze" symptom in the
  groundwork plan refers to buried-birth entities that fail to move, not to this API.)

Generic set-data mechanism (the extensible path underneath the convenience setters):
- `sendSetDataMsg(recipient, DtSetDataRequest* msg, addr)` :1577. `DtSetDataRequest` is an
  abstract, serializable base (setDataRequest.h:41); concrete subclasses map one-to-one to
  the `DtSet___Type` string vocabulary in radioMessageTypes.h:166-323. That vocabulary is
  much wider than the convenience setters, e.g.: `set-resource, set-location, set-vertices,
  set-heading, set-orientation, set_pitch, set-altitude, set-ordered-altitude, set-speed,
  set-current-speed, set-superior, set-formation, set-formation-type, set-target,
  set-concealed, set-destroy, set-appearance, set-capability, set-reorganize, clear-task,
  set-force, set-notify-level, set-spot-reporting-request, snap-into-formation, use-rails,
  set-navigation-preference, set-collision-avoid-types, set-stay-in-lane` and many more.
  Many convenience setters are just wrappers that build the corresponding request and call
  `sendSetDataMsg`. Anything not wrapped is reachable by constructing the request directly
  (e.g. `DtSetHeadingRequest`, `DtSetSpeedRequest`, `DtSetDestroyRequest` used in
  textIf.cxx:1507-1533 and via the factory `setDataFactory()` :2055).

### 4. TASKING

General send:
- `sendTaskMsg(recipient, DtSimTask* task, addr)` :1647 - the universal task send; `task`
  is any concrete `DtSimTask` subclass. This is how you send tasks the convenience methods
  don't cover, and how you set task parameters (speed override, at-distance, traversal
  direction, per-waypoint actions) that the convenience methods can't.

Convenience task methods (build a fixed task and send it):
- `moveAlongRoute(entity, route)` :1653, `moveToWaypoint(entity, waypoint)` :1659,
  `moveToLocation(entity, geocentricLoc)` :1666, `patrolBetweenWaypoints` :1672,
  `patrolAlongRoute` :1679, `followEntity(uuid, leader, offset_meters)` :1688,
  `wait(uuid)` :1695, `waitDuration(uuid, seconds)` :1701, `waitElapsed(uuid, simtime)`
  :1707, `skipTask(uuid)` :1712.

Concrete task classes (constructed then sent via `sendTaskMsg` / built into plans). Base
`DtSimTask` (simTask.h:30). There are ~60 task headers under include\vrftasks\. The
task-type string vocabulary is in radioMessageTypes.h:413-582. Ground-movement relevant:
- `DtMoveToTask` ("move-to", moveToTask.h): `setControlPoint(DtUUID)`, `setContinueFlag`
  (don't decelerate at the point - useful for chaining), `setAtDistance` (arrival-radius
  override for THIS task only - directly relevant to a truthful-arrival gate),
  `setSpeed` (per-task speed override). "Individual vehicles stop when they reach the
  waypoint; for a pseudo-aggregate the task completes when the lead subordinate reaches
  the waypoint" (moveToTask.h:30-33).
- `DtMoveAlongTask` ("move-along", moveAlongTasks.h): `setRoute(DtUUID)`,
  `setTraversalDirection` (forward/reverse), `setStartAtClosestPoint`, `setSpeed`.
  "Considered complete when the entity reaches the last vertex" (moveAlongTasks.h:29-30).
- `DtMoveAlongWithActionsTask` ("move-along-with-actions", moveAlongTasks.h:171): a
  move-along carrying a list of per-waypoint sets/tasks (`routeActions()`), plus
  `timeOnTarget`, `holdPosition`. This is the rich route task the GUI uses when waypoints
  carry actions/speeds - a closer native match for COA routes than plain move-along.
- Also present: `DtMoveToLocationTask`, `DtMoveToAltitudeTask` ("move-to-altitude"),
  `DtMoveIntoFormationTask`, `DtPatrolRouteTask`, `DtPatrolTwoPointsTask`,
  `DtFollowEntityTask`, `DtConvoyToTask`/`DtConvoyAlongTask`, `DtStopMovingTask`,
  `DtTurnToHeadingTask`, `DtScriptedMovementTask`, `DtWaitTask`/`DtWaitDurationTask`/
  `DtWaitElapsedTask`/`DtHoldUntilTask`, plus fire (`DtFireAtTargetTask`,
  `DtFireForEffectOn{Target,Entity,Location}Task`), embark/disembark, and DI-Guy families.
  Retrograde variants (`DtMoveToRetrogradeTask`, `DtMoveAlongRetrogradeTask`) keep the
  entity's orientation fixed regardless of velocity vector (moveToTask.h:132-135).

Task interruption / replacement (from the remote side):
- Sending a new top-level task via `sendTaskMsg` supersedes the current top-level task
  (subtasks are exempt: `DtSimTask::setSubtask` keeps a task from clearing the current
  one, simTask.h:83-87).
- `skipTask(uuid)` :1712 skips the currently executing task.
- The set-data `clear-task` request (`DtSetClearTaskType`, radioMessageTypes.h:251;
  clearTask.h) clears tasking; `clearTaskByTrackingNumAdminContent.h` clears a specific
  tracked task.
- `DtCurrentTaskData::conflictingTaskIds` (section 5) enumerates which task types a given
  task conflicts with - i.e. which get displaced when it starts.

Plans / statements (remotely settable):
- `assignPlanByName(uuid, DtPlan, addr, retiStatus, retsStmt)` :1735,
  `assignMultiplePlanByName(entities, DtPlan, ...)` :1741 (one plan to many entities),
  `restartPlan(uuid)` :1725, `abandonPlan(uuid)` :1837.
- Global plans: `assignGlobalPlan(name, DtPlan)` :1856, `launchGlobalPlan(name)` :1866,
  `deleteGlobalPlan(name)` :1862, `requestGlobalPlansList` :1847.
- Plans are built with `DtPlanBuilder` (example textIf.cxx:1494-1536): statements are
  tasks + set-data-requests + control flow (if/then/else, trigger, while blocks). A plan
  is the mechanism for conditional/sequenced tasking from the remote side.
- DEPRECATION to heed: "As of VR-Forces 3.10, assigning plans by echelon ID is no longer
  supported. Assign all plans by name." (vrfRemoteController.h:1733-1734, 1854-1855).

### 5. MONITORING (what a remote controller can READ)

Key architecture fact (confirmed): `DtVrfRemoteController` does NOT expose reflected
object state - it only forward-declares `DtReflectedEntityList`/`DtReflectedAggregateList`
(vrfRemoteController.h:64-65) and runs on a `DtCommunicationManager`. Reading back live
positions requires a SEPARATE VR-Link `DtExerciseConn` + reflected list that the
application constructs and drains itself, joined to the same DIS exercise / HLA
federation. The control channel (existing `DtCommunicationManager`) carries the plan /
current-task / report / resource channels below with no extra connection.

Three readback channels, most-direct first:

1. Reflected entity/aggregate state (own `DtExerciseConn` required). Construct a
   `DtReflectedEntityList(DtExerciseConn*, ...)` (vl/reflectedEntityListHLA.h:47) and/or
   `DtReflectedAggregateList(DtExerciseConn*, ...)` (vl/reflectedAggregateListHLA.h:31);
   both auto-discover and maintain one reflected object per network object. Per entity:
   `DtReflectedEntity::esr()` -> `DtEntityStateRepository`, whose parent
   `DtBaseEntityStateRepository` carries the TSPI in GEOCENTRIC world coords:
   `location(DtTime)` / `location()` (baseEntityStateRepository.h:108-113),
   `velocity()` :126, `orientation()` (`DtTaitBryan` Euler) :154, `frozen()` :178,
   `entityId()` :410, `entityType()` :415. NOTE there is no `heading()` getter on the
   reflected side - derive heading from `orientation()` yaw or from `velocity()`. Freshness
   via `reflectedObject::lastSimTimeUpdated()` (reflectedObjectHLA.h:105); change-driven
   via `DtReflectedEntity::addPostUpdateCallback` (reflectedEntityHLA.h:75).
   Aggregates: `DtReflectedAggregate::asr()` -> `DtAggregateStateRepository`:
   `aggregateState()` :62, `formation()` :70, member entity list `entities()` :87,
   `subAggregates()` :91 (aggregateStateRepository.h), plus inherited location/velocity.
   (Identity mapping caveat: reflected objects carry HLA object id / DIS entity id /
   marking, NOT the control-layer `DtUUID`; cross-reference via marking text / entityId.)

2. Appearance / frozen / damage bits (status badges). Read directly off the reflected
   entity's `esr()` (same accessors as `DtAppearance`, appearance.h:27, included by the
   controller at :41): `frozen()` (esr:109 / appearance.h:56), `damageState()` returns
   `DtDamageState` (esr:119 / appearance.h:64), `isConcealed()` (esr:213 / appearance.h:125),
   `immobilized()` (esr:154 / appearance.h:80), `firePowerDisabled()` (esr:208 /
   appearance.h:121), `missionKill()` (esr:258 / appearance.h:153), plus `flamesPresent()`,
   `smokePlumePresent()`, `engineSmokeOn()`. These are the frozen/deactivated and
   damage/destroyed badge bits. Caveat: appearance attributes are FOM-dependent and
   optional (entityStateRepository.h:83-87). The unidentified yellow-triangle badge from
   plan 0.2 is most likely one of `immobilized()` / a warning appearance - candidate to
   confirm here.

3. Current-task / plan state over the control channel (no extra connection). Two paths:
   - Current executing tasks (the direct "what is this unit doing now" query):
     request/subscribe with `DtRequestCurrentTasksAdminContent`
     (requestCurrentTasksAdminContent.h:19; `setSubscribeToChanges(bool)` picks one-shot
     vs continuous). Response is `DtCurrentTasksAdminContent` (currentTasksAdminContent.h:113)
     whose `taskList()` :164 is a list of `DtCurrentTaskData` (:25): the executing
     `DtSimTask* task` (its `type()` gives the kind), `taskId`, `controller`, recursive
     `subtasks`, and `status` (1=Active, 2=Suspended, 3=reactive-check; :39-43), plus
     `conflictingTaskIds`. The higher-level `DtGuiThreadVrfRemoteController` wraps this as
     `requestCurrentTasks(uuid)` / `subscribeForTaskInformation(...)`
     (guiThreadVrfRemoteController.h:979-988). Per-task execution detail:
     `DtMoveAlongTaskState::currentVertex()` (moveAlongTaskState.h:54) gives the 0-based
     index of the waypoint the entity is heading to - reaching the last vertex means the
     route is complete.
   - Plan status callbacks: `subscribePlan`/`requestPlan` (:1759/:1750) then
     `addPlanStatementCallback` (:1799) and `addPlanCompleteCallback` (:1823). The
     statement callback delivers a `DtPlanStatus` (plan.h:45) with fields
     `currentStatementId`, `isComplete`, `isExecuting`, `isAbandoned`,
     `executingTriggerNames`. Coarser than the task query and only for movement issued as
     a plan.

4. Reports and resources (secondary channels):
   - Spot reports (PERCEIVED, not ground truth): `requestSpotReports(uuid, onOff, addr)`
     :1430; `DtSpotReport` carries `contactLocation()` (geocentric), `contactType()`,
     `reason()` (spotReport.h).
   - Resource monitor: `requestResources(uuid, resourceNames, cb, usr)` :1950 ->
     `DtIfResourceMonitorResponse` with per-entity `DtResource` (current/full amount,
     name, type) - good for ammo/fuel/health badges (ifResourceMonitorResponse.h,
     ifEntityResourceResponse.h). Example usage: textIf.cxx:654-742.
   - Console/comment channel: `addObjectConsoleMessageCallback` :1970,
     `setObjectNotifyLevel` :1977 - captures object-level console/warning text remotely.
   - Backend/scenario status: `simulationStatus()` :2036 +
     `addSimulationStatusChangedCallback` :2039; `allBackendsReady()` :326;
     backend-loaded/saved callbacks.

Arrival-verification recommendation (feeds the truthful-arrival gate): run a
self-owned `DtReflectedEntityList` and poll `esr()->location()` for a geometric
distance-to-target test (continuous ground truth, independent of how the move was tasked),
cross-checked with the current-tasks subscription (moved off the move task / reached the
last vertex = done) and appearance `frozen()`/`damageState()` for status badges. The
plan-complete callback alone is the weakest signal (plan-scoped, tasking-dependent) - the
groundwork plan already found completions untrustworthy, and this API layout explains why:
completion is a control-channel notification, not a position observation.

### 6. SCENARIO (load / new / save / control) - and the scnx trick

- New: `newScenario(...)` three overloads :451/:462/:476 - terrain db + gui db (+ opd,
  physical world, simulation-model-set files, optional `baseScenario`, start-time-of-day,
  weather). Creating from a `baseScenario` seeds a full org tree.
- Load: `loadScenario(...)` three overloads :528/:534/:546 (by filename, by
  `DtScenarioLoadInformation`, or by `DtScenario`). Carries a backend-remapping fcn, a
  load-balancing fcn, simulation-model-set files, and a `ComponentAttachmentRule`
  (`AttachNone`/`AttachFirst`/`AttachEven`, enum :205). Loading a scenario is the highest-
  fidelity "instantiate real, GUI-authored units with full structure" path.
- Import (MSDL etc.): `importScenario` :603, `importScenarioDescriptionFile(file, addr,
  format, importData)` :610.
- MUST close before load/new: `closeScenario(addr, databaseToOpen, forceCloseAll)` :804;
  header warns you must close and wait for all backends before load/new or the backend is
  left "in an undefined state" (:512-519). `reinitializeScenario()` :808 rebuilds all
  scenario objects.
- Control: `run(useStartStopInteraction, addr)` :819, `pause(...)` :820, `step()` :826,
  `rewind()` :823, `exit(addr)` :825, `setTimeMultiplier(double)` :827,
  `rollbackToSnapshot(simTime)` :599, `takeSnapshot()` :680.

SCNX trick (item 6, CONFIRMED viable via remote control):
`saveScenario(saveName, preserveMappingsFcn, userData, error, saveToZip=false,
deleteExistingZip=true, simTime=-1)` :660 sends a save message to the backends; each
backend returns its order-of-battle (.oob) and plan (.pln) data, and the front end composes
a scenario file at `saveName` (`processSaveMessages` :869, `save(DtString&)` :670). Valid
save extensions are `.scn` AND `.scnx` (vrfSaveScenarioHandler.h:58: "valid save scenario
extension (.scn, .scnx)"); `.scnx` is the zipped form, produced with `saveToZip=true`
(loadOrderOfBattleHandler.h:70 "Deals with the scenario as a zip (scnx) file"). So a remote
controller CAN command the running backend to serialize the CURRENT scenario - whether the
units in it were GUI-authored OR remote-created - to a readable .scnx. That is exactly the
apparatus Phase 0.5 wants: create-via-remote, save-to-scnx, diff against a GUI-authored
.scnx to see field-by-field how our creations differ from native units. Completion notice
via `addScenarioSavedCallback` (new-style with `DtSaveResult`, :729).
Related: `saveScenarioCheckpoint` :676, `saveSimulationObjectGroup` :787 (save selected
objects as a reusable .sogx, section 1d), `saveParameterDb` :778.

### 7. HONEST GAPS - what the GMU interface used vs what exists

Source of the "used" column: audit of the GMU C2SIM automation path. Two codebases exist:
the C++ ORACLE (`.../Interfaces/c2simVRFinterface/c2simVRFinterfacev2.33-patched/
C2SIMinterface.cpp`) and the C++ PORT (`.../Interfaces/VRF_C2SIM/src/VrfFacade/
VrfFacade.cpp`). A third body of calls lives in the inherited MAK demo console
(`textIf.cxx`) - compiled into the oracle binary but NEVER driven by C2SIM; those are the
"exists, demonstrated, but unused-by-automation" calls. The delta below is the design space.

The C2SIM automation actually exercises a SMALL slice:
- CREATE: `createEntity`, `createAggregate(createSubordinates=true)`, `createWaypoint`,
  `createRoute`; port adds `createControlArea`. (Oracle also has a dead
  `sendVrfObjectCreateMsg` helper that the STP aggregate path does not use.)
- ORG: NONE in either C2SIM path (no `addToOrganization`, `setSuperior`, `setLabel`).
- STATE: `setAltitude(...,AGL=TRUE)`, `setLocation`, `setTarget`, `setRulesOfEngagement`,
  `sendSetDataMsg`; port adds `setAggregateFormation`, `reorganizeAggregate`, `deleteObject`.
- TASK: `moveToLocation`, `moveAlongRoute`, `sendTaskMsg` (oracle: scripted "evacuate"
  only; port: a Layer-2 vocabulary via `sendTaskMsg` - `DtPlanAndMoveToTask`,
  `DtMoveIntoFormationTask`, `DtBreachTask`, `DtPatrolRouteTask`, `DtFollowEntityTask`,
  `DtFireAtTargetTask`, `DtScriptedTaskTask`). PORT.md sec 10: "~4 of 263 vrftasks".
- MONITOR: report/close/formation message callbacks + reflected-object/UUID enumeration
  (port: `reflectedObjectFor`, aggregate member enumeration). NO plan subscription, NO
  arrival-position polling for a truthful-arrival gate.
- SCENARIO: `run`, `pause`, `setTimeMultiplier`, `setExerciseStartTime`. NO load/new/save/
  close in the automation.

Delta - capabilities that EXIST in the API but the C2SIM automation does NOT use (ranked
by relevance to the groundwork goals):

| Gap | Exists (cite) | Why it matters for the rebuild |
|-----|---------------|--------------------------------|
| Multi-echelon org build | `addToOrganization` :1354, `setSuperior` :1495, `sendVrfObjectCreateMsg(superior=)` ifCreateVrfObject.h:258 | Build company->platoon->vehicle trees instead of one flat aggregate; make units structurally like GUI units (Phase 2/3 core) |
| Real-template composite create | `createSimulationObjectGroup` :492 (.sogx), `createAggregate(createSubordinates=true)` used but hitting the GENERIC fallback | The fix is right type resolution + possibly .sogx, not a new create verb |
| Arrival readback / truthful-arrival gate | own `DtReflectedEntityList` + `esr()->location()` (sec 5); `DtRequestCurrentTasksAdminContent` :19 -> `taskList()`/`status`; `DtMoveAlongTaskState::currentVertex()` | Programmatic, position-based arrival - the automation currently has NO position-arrival check; completions proved untrustworthy |
| Plan/statement monitoring | `subscribePlan` :1759, `addPlanStatementCallback` :1799, `addPlanCompleteCallback` :1823, `DtPlanStatus.isComplete` | Sequencing signal; used only in the demo console, never in C2SIM |
| Richer native tasks | `moveToWaypoint` :1659, `patrolAlongRoute` :1679, `DtMoveAlongWithActionsTask` (per-waypoint speeds/actions), `DtMoveToTask::setAtDistance` (arrival radius) | Map COA verbs to closer native tasks than collapse-to-move; per-task arrival radius feeds the gate |
| scnx diff apparatus | `saveScenario(...,.scnx,saveToZip=true)` :660, `saveSimulationObjectGroup` :787 | Phase 0.5: save remote-created scenario, diff vs GUI-authored .scnx |
| Scenario bring-up from code | `newScenario` :451, `loadScenario` :528, `closeScenario` :804 | Self-launch / reset without the GUI (Phase 0.4 dependency) |
| Per-entity state not wired | `setHeading` :1414, `setSpeed` :1474, `setResource` :1450, `restore` :1535, `setAppearance` :1402 | Available; only `setAltitude/Location/Target/RoE` are used by C2SIM |
| Superior-at-create | `DtIfCreateVrfObject::setSuperior/setAddToOrbat/setInitialFormation` (via `sendVrfObjectCreateMsg`) | Set org parent + formation atomically at birth, avoiding the create-order timing hazard |

Confirmed ABSENT everywhere (port + oracle + demo): `addToForceLevelOrganization`,
`removeFromOrganization`, `assignGlobalPlan`, `skipTask`, `requestResources`,
`requestPlan`, `createObject`, `createOverlayObject`, `createSimulationObjectGroup`.

Two known carried bugs surfaced by the audit (both preserved into the port):
- `setTarget(taskeeUuid, <C2SIM uuid>)` (C2SIMinterface.cpp:2135; port VrfFacade.cpp:461)
  passes a C2SIM UUID where a VRF UUID is required -> VRF no-op (PORT.md sec 6).
- Create-time heading passed as raw degrees in the oracle/example (section 1a) - a wrong
  initial spawn heading; the port converts, but aggregates still spawn at heading 0.

---

### Residual uncertainties / verify-next

- Exact runtime behavior of each `DtAggregateState` value at Mojave (esp. `DtAggregated`
  vs `DtDisaggregated` re: whether member entities are individually simulated/taskable) is
  a header-documented DIS enum but a live-probe question, not settled here.
- Whether `createSubordinates=true` with a CORRECTLY-resolved installed template yields the
  full real subordinate composition (vs the generic fallback we observed) must be shown
  live once Phase 0.1's type catalog gives real DIS enumerations to test.
- `DtReflectedEntityList` identity is HLA/DIS object id + marking, not the control-layer
  `DtUUID`; the reflected-vs-control cross-reference (marking text / entityId) needs a
  concrete mapping in the rebuilt monitor. The port already enumerates reflected objects
  via `uuidNetworkManager()`/`reflectedObjectFor(DtUUID)` (VrfFacade.cpp:328,531), which
  suggests VRF provides a UUID<->reflected bridge worth reusing - confirm its coverage.

---

## 0.1 Installed content catalog

Owner: Phase 0.1 (executor). Goal: the definitive catalog of REAL, installed VR-Forces
unit content available to our scenarios, so C2SIM units can be mapped to real types instead
of generic fallbacks. All paths below are under `C:\MAK\vrforces5.0.2\`.

### 0.1.0 Method and the one structural fact that governs everything

VR-Forces creation is content-driven. Our app calls `createAggregate`/`createEntity` with an
8-field object type; the back-end resolves that type to a `.entity` template in the loaded
simulation model set (SMS) by a documented best-match rule (0.1.2). The template - not our
code - decides the unit's real composition, formations, echelon, and which movement
controller runs. So "close to their real types" is entirely a question of WHICH template our
type resolves to. Sections 0.1.5-0.1.7 show that today most units resolve to a generic
fallback or to the wrong branch (armor) regardless of what they actually are.

Object-type field order (verified `doc/help/Content/SimulationModels/ObjectParameterDatabase/ObjectTypes.htm`
lines 185-254, and consistent with every `.entity` read):

```
  superType : Kind : Domain : Country : Category : Subcategory : Specific : Extra
     3          11      1       225        5           2            0         0     (an aggregate)
     1           1      1       225        1           1            3         0     (an M1A2 entity)
```

- Field 0 (superType) is a VR-Forces extension to the 7-field DIS/RPR enumeration. Values
  (ObjectTypes.htm Table 63): 0=Other, 1=Individual (platform entity / individual graphic),
  2=Unit, 3=Disaggregated unit (a unit composed of other sim objects), 4=Aggregated unit
  (a unit with NO subordinate sim objects).
- Fields 1-7 are the standard DIS SISOEntityType (Kind, Domain, Country, Category, Subcat,
  Specific, Extra). Kind=11 is VR-Forces' marker for a disaggregated unit; Kind=1 = platform.
- Our C# `EntityTypeSpec` carries only the 7 DIS fields (Kind..Extra); the facade prepends
  superType 3 for aggregates, 1 for entities. So `Spec(11,1,225,5,2,0,0)` is published as
  objectType `3:11:1:225:5:2:0:0` (verified UnitTranslator.cs:115-116 + how it matches
  Tank Company (USA) below).

DIS land Category values observed in the chain (derived from named platform files, 0.1.4):
1=Tank, 2=Armored Fighting Vehicle (Bradley/BMP/M113), 3=APC/command (M577), 4=Artillery
(SP howitzer/MLRS), 6=Utility vehicle (HMMWV), 7=Truck (FMTV), 28=Air-defense/SAM. For
Kind=11 (unit) object types the Category field instead encodes echelon (3=platoon, 4=battery,
5=company, 6=battalion/squadron, 8=brigade, 9=division, 12=team, 13=squad, 14=section/HQ).

### 0.1.1 The confirmed .sms include chain

Confirmed by reading each `.sms` file's `(include ...)` line directly:

```
  data/simulationModelSets/C2simEx.sms          (line 86: include EntityLevel.sms)
    -> data/simulationModelSets/EntityLevel.sms  (line 86: include base.sms)
       -> data/simulationModelSets/base.sms       (no further include; leaf)
```

- `C2simEx.sms:14-95` - the set our scenarios name (both Bogaland2/Sweden and
  TropicTortoise/Mojave `.scnx` declare `C2simEx.sms`; MOJAVE doc verified byte-identical).
  `model-set-directory "C2simEx"`, opd-file `C2simEx\vrfSim.opd`, `(include
  "..\data\simulationModelSets\EntityLevel.sms")` at line 86.
- `EntityLevel.sms:86` - `(include "..\data\simulationModelSets\base.sms")`.
- `base.sms` - no include (43 lines, no `(include ...)`).

Each SMS contributes the `.entity` templates under its own `<dir>/vrfSim/` tree; later
(more specific) sets add to / override earlier ones. NOT in the chain (do NOT catalog as
available to our scenarios): `AggregateLevel.sms`, `MAKTest.sms`, `developer_toolkit_examples`
- sibling model sets our scenarios never load. (The MOJAVE doc "part 9" note that the
overlap-footprint speed modifier lives only under AggregateLevel is the same fact: that
content is out of our chain.) The three in-chain directories:
`C:\MAK\vrforces5.0.2\data\simulationModelSets\{C2simEx,EntityLevel,base}\vrfSim\`.

Where the templates actually live: C2simEx adds 4 custom `.entity` files
(`AR Scout`, `Mobile Irregular`, `Mobile Light Infantry`, `Skiff`); base adds only the
root fallback `base-sim-aggregate.entity`; the LARGE library of real unit templates
(companies, platoons, batteries) is in `EntityLevel/vrfSim/`.

### 0.1.2 How a creation request resolves (the OPD best-match rule) - VERIFIED from the doc

`ObjectTypes.htm` (lines 255-316), quoted/paraphrased:

- Every template has an `objectType` (published, all fields specific) and a `matchType`
  (may contain `-1` wildcards).
- "When the back-end receives a request to create a simulation object and it cannot find an
  exact match for the object type that it is sent, it finds the best match among matching
  object types" (line 270).
- Best-match method (lines 281-316): start at the root of the type tree, walk down replacing
  wildcards with specifics; matching narrows left-to-right. A non-wildcarded field that
  DIFFERS is a non-match and forces fallback to the nearest more-wildcarded ancestor. Worked
  example in the doc: a discovered `1:2:225:2:11:0:0` fails the A-10's `...:2:4:1:-1` at
  field 6 (4 != 11, not wild) and settles on the generic "US attack fixed-wing" ancestor.

Consequence for us: a type whose non-wild fields do not exactly line up with any leaf falls
to whatever wildcarded ancestor still matches - which for our ground aggregates is
`Ground_Aggregate.entity` (0.1.5).

### 0.1.3 Aggregate-capable unit templates in the chain

An aggregate-capable template is any `.entity` with `objectType` superType=3 (disaggregated
unit). The chain has ~95 such templates; the ground (Domain=1) force-relevant ones are
tabled below. First, the movement controllers they wire, because that is where the
ground-clamp / empty-route behavior lives.

#### 0.1.3.a The three ground movement sysdefs (controllers + ground-clamp) - VERIFIED

Every ground aggregate wires exactly one movement sysdef via
`<componentSystem systemName="disaggregated-movement" platform=".../movement/<X>.sysdef"/>`.
Only three `<X>` occur for ground units (paths under
`EntityLevel/vrfSim/systems/movement/`):

| sysdef | wired by | move-along controller (component-type) | ground-clamp | move-into-formation clamp | in-position tol |
|--------|----------|----------------------------------------|--------------|---------------------------|-----------------|
| `ground-disaggregated-movement.sysdef` | PLATOON-level & HQ-section units (members are ENTITIES) | `aggregate-lead-follow-in-formation-controller` (:112-130) | **True** (:129) | **True** (:49) | 0.2 m (:125) |
| `ground-higherUnit-disaggregated-movement.sysdef` | COMPANY/BATTALION units (members are UNITS) | `aggregate-move-along-controller` (:87-105) | **True** (:104) | **True** (:49) | 0.2 m (:100) |
| `human-disaggregated-movement.sysdef` | DISMOUNTED infantry (squads/teams/inf platoons/companies) | `aggregate-move-along-controller` (:95-114) | **True** (:113) | **True** (:31) | 0.2 m (:109) |

Load-bearing facts:
- ALL THREE set `ground-clamp True` on both the move-along controller AND the
  move-into-formation controller. Ground-clamp is a property of disaggregated ground
  movement generally in this SMS, NOT of under-defined units - deliberate, not an oversight
  (corroborates MOJAVE doc part 5). It is the step that rejects the interface's fixed-100 m
  MSL waypoints at ~1100 m Mojave terrain.
- The PLATOON path (`aggregate-lead-follow-in-formation-controller`) is the one that emits
  `moveAlong() - empty route -- not sending move along to subordinate`: it lays out per-member
  OFFSET routes (leader path + formation offsets, then ground-clamps each vertex). The
  COMPANY/HUMAN path (`aggregate-move-along-controller`) moves subordinate UNITS, not member
  entities - a different controller, still ground-clamped. So a company and its platoons run
  DIFFERENT move-along controllers; the empty-offset-route symptom is specifically the
  platoon/lead-follow controller.
- `aggregate-formation-controller` has `auto-promote-in-formation False` in all three - a
  lead subordinate must be established (why R5 `AggregateFormation=auto` reorganize-at-create
  was needed).
- The platoon and human sysdefs enable `script-enable-controller` scripts including
  `set_freeze_movement` / `transition_into_formation` / `take_formation`; the human one adds
  patrol controllers (patrol-between/along) that the ground-vehicle one has commented OUT
  ("Patrol tasks removed for units", ground-disaggregated-movement.sysdef:87-111).

Non-ground movement sysdefs seen (not our mapping targets): aircraft (`fixed-wing-*`,
`rotary-wing-*`), `ground-convoy-movement.sysdef` (Convoy), `surface-*` (boats),
`animal-herd-movement.sysdef`, `pedestrian-crowd-movement.sysdef`.

#### 0.1.3.b Ground force-relevant aggregate roster (Domain=1)

All under `EntityLevel/vrfSim/` unless noted `[C2simEx]`. `create` = `gui-can-create` flag
(True = palette-creatable composed unit; False = abstract/generic parent, typically Country
wild with no explicit subordinates). `mv`: `LF` = ground-disaggregated (lead-follow, platoon),
`HU` = ground-higherUnit (company/bn), `HM` = human-disaggregated. objectType shown 8-field;
matchType wildcards noted. Subordinate detail in 0.1.3.c for the starred (*) rows.

ARMOR (tanks):

| template (.entity) | objectType | matchType | echelon | mv | create | platform.ope |
|--------------------|-----------|-----------|---------|----|--------|--------------|
| Tank Platoon (USA) * | 3:11:1:225:3:2:0:0 | 3:11:1:225:3:2:-1:-1 | Plt | LF | True | VehicleAggregate |
| Tank Platoon (RUS) * | 3:11:1:222:3:2:0:0 | 3:11:1:222:3:2:-1:-1 | Plt | LF | True | VehicleAggregate |
| Tank Platoon (USA) Mine Plows | 3:11:1:225:3:2:0:78 | exact | Plt | LF | (True) | VehicleAggregate |
| Tank Platoon (generic) | 3:11:1:0:3:2:0:0 | 3:11:1:-1:3:2:0:-1 | PLT | LF | (abstract) | VehicleAggregate |
| Tank Headquarters Section (USA) * | 3:11:1:225:14:2:1:0 | 3:11:1:225:14:2:1:-1 | HQ Sec | LF | True | VehicleAggregate |
| Tank Headquarters Section (RUS) | 3:11:1:222:14:2:1:0 | 3:11:1:222:14:2:1:-1 | Co HQ | LF | (True) | VehicleAggregate |
| Tank Company (USA) * | 3:11:1:225:5:2:0:0 | 3:11:1:225:5:2:-1:-1 | Co | HU | True | HigherAggregate |
| Tank Company (RUS) * | 3:11:1:222:5:2:0:0 | 3:11:1:222:5:2:-1:-1 | Co | HU | True | HigherAggregate |
| Tank Company (generic) | 3:11:1:0:5:2:0:0 | 3:11:1:-1:5:2:0:-1 | CO | HU | (abstract) | Aggregate |
| Tank Breach Company (USA) * | 3:11:1:225:5:2:0:78 | exact | Co | HU | True | HigherAggregate |
| aggregate-Plt-M1A2 | 3:11:1:225:3:0:0:1 | exact | Plt | LF | - | Aggregate |
| aggregate-Co-M1A2 | 3:11:1:225:5:0:0:1 | exact | Co | HU | - | Aggregate |
| aggregate-Plt-T80 | 3:11:1:222:3:0:0:1 | exact | Plt | LF | - | Aggregate |
| aggregate-CO-T80 | 3:11:1:222:5:0:0:1 | exact | Co | HU | - | Aggregate |

MECHANIZED INFANTRY (mounted):

| template | objectType | matchType | echelon | mv | create |
|----------|-----------|-----------|---------|----|--------|
| Mechanized Platoon | 3:11:1:0:3:4:0:0 | 3:11:1:-1:3:4:0:-1 | PLT | LF | False (abstract, no subs) |
| Mechanized Platoon IFV | 3:11:1:0:3:4:0:1 | 3:11:1:-1:3:4:0:1 | PLT | LF | False |
| Mechanized Platoon (USA) IFV (Deprecated) | 3:11:1:225:3:4:0:0 | exact | PLT | LF | (dep) |
| Mechanized Company | 3:11:1:0:5:4:0:0 | 3:11:1:-1:5:4:0:-1 | CO | HU | False (abstract, no subs) |
| Mechanized Company IFV | 3:11:1:0:5:4:0:1 | 3:11:1:-1:5:4:0:1 | CO | HU | False |
| Mechanized Battalion | 3:11:1:0:6:4:0:0 | 3:11:1:-1:6:4:0:-1 | BN | HU | False |
| Mechanized Battalion IFV | 3:11:1:0:6:4:0:1 | 3:11:1:-1:6:4:0:1 | BN | HU | False |
| Mechanized Squad (USA Army) | 3:11:1:225:13:3:0:0 | exact | SQD | HM | - |
| Mechanized Fire Team (USA Army) [+Javelin] | 3:11:1:225:12:3:0:1 / :0:0 | exact | FT | HM | - |

INFANTRY (dismounted):

| template | objectType | matchType | echelon | mv | create |
|----------|-----------|-----------|---------|----|--------|
| Infantry Platoon (USA Army) * | 3:11:1:225:3:3:1:0 | exact | PLT | HM | True |
| aggregate-Co-Infantry-Friendly * [225] | 3:11:1:225:5:3:1:0 | exact | Co | HU | (True) |
| aggregate-Co-Infantry-Hostile * [222] | 3:11:1:222:5:3:1:0 | exact | Co | HU | (True) |
| aggregate-Plt-Infantry-Hostile | 3:11:1:222:3:3:1:0 | exact | Plt | LF | - |
| Infantry Company (generic) | 3:11:1:... | - | CO | HM | False |
| Infantry Battalion / Brigade / Division | 3:11:1:... | - | BN/BDE/DIV | HM | False |
| Rifle Squad (USA Army) | 3:11:1:225:13:3:0:2 | exact | SQD | HM | - |
| Rifle Squad (USA Marines) | 3:11:1:225:13:3:0:3 | exact | SQD | HM | - |
| Weapons Squad (USA Army) | 3:11:1:225:13:3:0:58 | exact | SQD | HM | - |
| Infantry/Rifle Fire Team (USA Army/Marines) | 3:11:1:225:12:3:0:x | exact | FT | HM | - |
| Infantry Squad (USA)/(RUS) (Deprecated) | 3:11:1:225:13:3:0:1 / 222:13:3 | wild | SQD | HM | (dep) |

RECON / CAVALRY:

| template | objectType | matchType | echelon | mv | create |
|----------|-----------|-----------|---------|----|--------|
| Armored Cavalry Platoon | 3:11:1:0:3:6:0:0 | exact | PLT | LF | False (no subs found) |
| Armored Cavalry Troop | 3:11:1:0:5:6:0:0 | exact | TRP | HU | False |
| Armored Cavalry Squadron | 3:11:1:0:6:6:0:0 | exact | SQN | HU | False |
| Recon Vehicle Platoon (RUS BMP2) * | 3:11:1:222:3:6:0:49 | exact | Plt | LF | True |

FIELD ARTILLERY / MORTAR / TARGET ACQ:

| template | objectType | matchType | echelon | mv | create |
|----------|-----------|-----------|---------|----|--------|
| Field Artillery Battery (USA) M109 * | 3:11:1:225:4:8:0:0 | 3:11:1:225:4:8:-1:-1 | BTY | HU | True |
| Field Artillery Battery (USA) M777 | 3:11:1:225:4:7:0:0 | 3:11:1:225:4:7:-1:-1 | BTY | HU | True |
| Field Artillery Platoon (USA) M109 * | 3:11:1:225:3:8:0:0 | 3:11:1:225:3:8:-1:-1 | PLT | LF | True |
| Field Artillery Platoon (USA) M777 | 3:11:1:225:3:7:0:0 | 3:11:1:225:3:7:-1:-1 | PLT | LF | True |
| Field Artillery Section (USA) M109 / M777 | 3:11:1:225:14:8:0:0 / :7:0:0 | wild | SEC | LF | - |
| Field Artillery Headquarters Section (USA) | 3:11:1:225:14:7:1:0 | 3:11:1:225:14:7:1:-1 | SEC | LF | - |
| Artillery Battalion / Battery / Brigade (generic) | 3:11:1:... | - | BN/BTY/BDE | - | False |
| COLT Team (USA) / (RUS) | 3:11:1:225:12:27:0:0 / 222 | wild | Team | HM | - |
| Fire Support Team (USA) / Fire_Support_Team | 3:11:1:225:12:27:0:1 | exact | Team | HM | - |

AIR DEFENSE (the UCD family in COA-STP1) + ANTI-ARMOR:

| template | objectType | matchType | echelon | mv | create |
|----------|-----------|-----------|---------|----|--------|
| Air Defense Artillery Platoon (USA) * | 3:11:1:225:3:11:0:0 | 3:11:1:225:3:11:-1:-1 | PLT | LF | True |
| Air Defense Artillery Platoon (RUS) * | 3:11:1:222:3:11:0:0 | 3:11:1:222:3:11:-1:-1 | PLT | LF | True |
| Antitank Team (USA Army) Javelin | 3:11:1:225:14:12:0:0 | exact | FT | HM | - |

HQ / CS / CSS / CONVOY:

| template | objectType | matchType | echelon | mv | create |
|----------|-----------|-----------|---------|----|--------|
| aggregate-Company-HQ-Friendly * [225] | 3:11:1:225:5:20:1:0 | exact | Co | LF | (True) |
| aggregate-Company-HQ-Hostile [222] | 3:11:1:222:5:20:1:0 | exact | Co | LF | (True) |
| aggregate-Plt-HQ-Friendly / -Hostile | 3:11:1:225:3:20:1:0 / 222 | exact | Plt | LF | - |
| Combat Service Support Platoon (USA) / (RUS) | 3:11:1:225:3:31:0:0 / 222 | 3:11:1:225:3:31:-1:-1 | PLT | LF | - |
| Supply Section (USA) | 3:11:1:225:14:31:0:0 | exact | SEC | LF | - |
| Convoy | 3:11:1:0:0:0:0:1 | exact | Convoy | convoy | False |
| aggregate-DI-Sectn-Friendly / -Hostile | 3:11:1:225:14:3:0:0 / 222 | exact | Sectn | LF | - |

The generic FALLBACK templates (0.1.5): `Ground_Aggregate.entity` (3:11:1:-1:-1:-1:-1:-1)
and `base/vrfSim/base-sim-aggregate.entity` (3:11:-1:-1:-1:-1:-1:-1).

C2simEx custom ground aggregates: `AR Scout.entity` (3:11:1:225:14:30:0:1),
`Mobile Irregular.entity` (3:11:1:-1:13:34:0:1), `Mobile Light Infantry.entity`
(3:11:1:225:13:3:0:200).

ENGINEER: there is NO dedicated engineer aggregate template in the chain. `Tank Breach
Company (USA)` (armor + mine-plow, breach-capable) is the closest engineer-flavored unit.
Genuine content gap (flag, 0.1.8).

#### 0.1.3.c Real subordinate compositions (starred templates) - VERIFIED from files

Members shown as `functionHandle: count x objectType (resolved name)`. Names resolved via
0.1.4. "unit member" = the member is itself an aggregate (superType 3); "entity member" =
a platform (superType 1).

- **Tank Company (USA)** (HU / HigherAggregate): unit-of-units. HQ: 1 x 3:11:1:225:14:2:1:0
  (Tank Headquarters Section (USA)); TANK: 3 x 3:11:1:225:3:2:0:0 (Tank Platoon (USA)).
  Formations Title-Case Column/Line/Wedge/Vee (Formation-*-Armor-Co(US).frm).
  **This is what our ArmorCompany factory (echelon E) resolves to.**
- **Tank Platoon (USA)** (LF / VehicleAggregate): entity-of-4. 4 x 1:1:1:225:1:1:3:0
  (M1A2 Abrams), handles PL/PSG/PLWM/PSGWM. Formations Column-Left/Line-Left/Wedge-Left/
  Vee-Left + -Right variants + Column (Ar_Plt_US_*.frm). The long-observed birth default
  "Column-Left" originates here.
- **Tank Headquarters Section (USA)** (LF): 6 entity members - CDR,XO: 2 x M1A2
  (1:1:1:225:1:1:3:0); FSO: 1 x M3A2 Bradley CFV (1:1:1:225:2:1:2:0); AUX: 1 x M577 Command
  Post (1:1:1:225:3:11:0:0) + 2 x M998 HMMWV (1:1:1:225:6:1:1:0).
- **Tank Breach Company (USA)** (HU): HQ: 1 x Tank HQ Section; TANK: 2 x Tank Platoon (USA);
  PLOW: 1 x Tank Platoon (USA) Mine Plows (3:11:1:225:3:2:0:78).
- **Tank Company (RUS)** / **Tank Platoon (RUS)**: RUS mirror - Company = HQ Sec (RUS) + 3 x
  Tank Platoon (RUS); Platoon = 3 x T-80 MBT (1:1:1:222:1:1:1:0).
- **Infantry Platoon (USA Army)** (HM): HQ: 1 x 3:11:1:225:12:3:1:0 (Infantry HQ Team); INF:
  3 x 3:11:1:225:13:3:0:2 (Rifle Squad (USA Army)); WEAP: 1 x 3:11:1:225:13:3:0:58 (Weapons
  Squad (USA Army)). Formations lowercase column/line/wedge/vee/file.
- **aggregate-Co-Infantry-Friendly** [225] (HU): HQ: 1 x 3:11:1:225:14:3:1:127 (Infantry HQ
  Section); INF: 3 x 3:11:1:225:3:3:0:0 (Infantry Platoon). Hostile mirror [222] same shape.
- **Recon Vehicle Platoon (RUS BMP2)** (LF): PL/PSG/IFV: 3 x 1:1:1:222:2:2:1:0 (BMP-2 AFV).
- **Field Artillery Battery (USA) M109** (HU): HQ: 1 x 3:11:1:225:14:7:1:0 (FA HQ Section);
  FIRES: 2 x 3:11:1:225:3:8:0:0 (FA Platoon (USA) M109).
- **Field Artillery Platoon (USA) M109** (LF): HQ: 1 x 1:1:1:225:3:13:0:0; FDC: 1 x
  1:1:1:225:3:13:1:0; FIRES: 4 x 3:11:1:225:14:8:0:0 (FA Section M109, fielding the M109A5
  SP howitzer 1:1:1:225:4:3:6:0).
- **Air Defense Artillery Platoon (USA)** (LF): 4 x 1:1:1:225:28:5:4:0 (HMMWV with Avenger).
  RUS variant: 4 x 1:1:1:222:28:12:2:0 (SA-9 Gaskin SAM System).
- **aggregate-Company-HQ-Friendly** [225] (LF): 4 x 1:3:1:225:1:1:0:0 (US dismounted soldier
  life-form; no dedicated platform file - generic DI-Guy). Hostile mirror = Country 222.

#### 0.1.3.d Non-ground / non-force aggregates (listed, not force-mapping targets)

Present but NOT candidates for armor/mech/infantry mapping (Domain 2=air, 3=surface, or
non-military): aircraft units (`A-*`, Flight, Air Section, Fighter Squadron (USA) F16,
Rotary Wing Unit, Carrier Air Wing, Strike/Sea/Maritime/Electronic squadrons, UAS Flock,
Aviation Assault/Attack Battalion/Company - the last two are Domain=1 land-labeled but
AVIATION); naval (Surface, Sea Combat Squadron, Logistics Support Squadron); civilian
(Civilian Crowd (Small/Medium/Large) + Mideast variants; animal herds Goat/Sheep/Cow/Animal).
Aviation is relevant only to COA-STP1's 2 UCV units (0.1.7).

### 0.1.4 Ground entity (platform) templates relevant to armor/mech/infantry (item 3)

Kind=1 platform `.entity` files our aggregates spawn as members, plus the ones our entity
factories create directly. DIS type shown 7-field (superType 1 prefix omitted). All under
`EntityLevel/vrfSim/` (verified via `grep -rl objectType`).

| platform (.entity) | DIS type (Kind.Dom.Ctry.Cat.Sub.Spec.Extra) | role | used by |
|--------------------|----------------------------------------------|------|---------|
| M1A2_Abrams_MBT | 1.1.225.1.1.3.0 | US MBT (tank) | Tank Platoon (USA) members; Tank HQ; **our Tank() factory + ObjectTypes.htm's own M1A2 example** |
| T-80_MBT | 1.1.222.1.1.1.0 | RUS MBT | Tank Platoon (RUS) members |
| M2A2_Bradley_IFV | 1.1.225.2.1.1.0 | US IFV | mech-infantry mount |
| M3A2_Bradley_CFV | 1.1.225.2.1.2.0 | US cavalry fighting vehicle | Tank HQ Section (FSO) |
| BMP-2_AFV | 1.1.222.2.2.1.0 | RUS IFV | Recon Vehicle Platoon (RUS) members |
| M113_APC | 1.1.225.2.3.1.0 | US APC | APC family |
| M577A2_Command_Post | 1.1.225.3.11.0.0 | US command-post APC | Tank HQ Section (AUX) |
| M1126_Stryker_ICV | 1.1.225.2.5.0.0 | US Stryker ICV | Stryker units |
| M998 HMMWV Utility Vehicle | 1.1.225.6.1.1.0 | US utility (HMMWV) | Tank HQ Section (AUX) |
| M1025 HMMWV with M2 | 1.1.225.6.1.18.0 | US armed HMMWV | gun-truck family |
| HMMWV_with_Avenger | 1.1.225.28.5.4.0 | US SHORAD | Air Defense Artillery Platoon (USA) members |
| SA-9_Gaskin_SAM_System | 1.1.222.28.12.2.0 | RUS SHORAD | Air Defense Artillery Platoon (RUS) members |
| M109A5_SP_Howitzer | 1.1.225.4.3.6.0 | US 155 SP howitzer | FA Section (USA) M109 |
| M1157 FMTV Troop Seat Dump Truck | 1.1.225.7.12.30.1 | US medium truck | **our Truck() factory (HO, no-SIDC)** |
| (generic Cat-4 vehicle) | 1.1.225.4.14.0.0 | no exact platform file (artillery-family, subcat 14) | the 4 anonymous members of Ground_Aggregate |

Dismounted infantry: represented as Kind=3 (life form) DI-Guy characters. A large roster of
soldier `.entity` files exists by nationality (`European Soldier`, `Afghan Soldier AK47`,
`Israeli Soldier IMI Galil`, `Ghillie Suit Soldier`, ...); US dismounts appear as the
1:3:1:225:1:1:0:0 life-form member inside HQ/squad aggregates (no single "US Soldier" file
matched a quick glob - the US dismount is a generic DI-Guy; flag 0.1.8). Weapon carriers
M1134 Stryker ATGM, M1046 HMMWV TOW, M270 MLRS / M142 HIMARS also present.

### 0.1.5 The generic fallback resolution logic - VERIFIED (files + lines)

When a Kind=11 ground aggregate type finds NO leaf match, the best-match walk (0.1.2) stops
at the most specific still-matching wildcard ancestor. Two fallback tiers:

1. `EntityLevel/vrfSim/Ground_Aggregate.entity` (line 3): objectType `3:11:1:0:0:0:0:0`,
   **matchType `3:11:1:-1:-1:-1:-1:-1`** = "any Kind=11, Domain=1 (land) unit; everything else
   wild". Platform `Aggregate.ope`. This is the operative fallback for our ground aggregates.
   NOT content-free (an earlier MOJAVE-doc draft wrongly said so, then corrected):
   - `gui-can-create` = **False**, `gui-label` = "Ground Unit", gui-categories include
     "Empty Unit" (lines 11, 45, 40) - MAK's own naming says internal fallback, not a real
     unit-specific composition.
   - Real formations (lines 20-25): line/column/wedge/vee (lowercase, Ar_Plt_US_*.frm) -
     matches the live "formations are all lowercase" observation.
   - `echelon-level` = "" (empty, line 39) - born with no echelon.
   - Subordinates (lines 56-61): 4 x `1:1:1:225:4:14:0:0` - four ANONYMOUS generic Cat-4
     vehicles (no functionHandle, no real composition; that member type has no exact platform
     file of its own, 0.1.4).
   - Movement: `ground-disaggregated-movement.sysdef` (line 18) = the LF lead-follow
     controller with ground-clamp True (0.1.3.a) - the empty-offset-route path.
   - ordered-speed 10 m/s, max-speed 18.06 m/s (lines 29-30).
2. `base/vrfSim/base-sim-aggregate.entity` (line 3): objectType/matchType
   `3:11:-1:-1:-1:-1:-1:-1` = "any Kind=11 unit at all" (even non-land). Truly minimal
   (short-name, echelon "", combat-range; Aggregate.ope; no formations/subordinates/movement
   of its own). Reached only if Domain != 1; our ground types never fall this far because
   Ground_Aggregate's Domain=1 still matches.

So EXACTLY WHEN a creation gets a generic instead of a real type: whenever our published
`3:11:1:<Cat>:<Sub>:<Spec>:<Extra>` type has no leaf whose non-wild fields all equal ours.
Concretely for our five aggregate factories (verified against the 0.1.3.b roster):

| our factory (UnitTranslator.cs) | published objectType | resolves to | why |
|---------------------------------|----------------------|-------------|-----|
| ArmorCompany (echelon E) :115 | 3:11:1:225:5:2:0:0 | **Tank Company (USA)** (real) | matches matchType 3:11:1:225:5:2:-1:-1 (Spec/Extra wild) |
| ArmorPlatoon (echelon D) :112 | 3:11:1:225:1:1:3:0 | **Ground_Aggregate** (generic) | Category=1 has NO Kind=11 leaf; ancestor only |
| ArmorCoHQ (echelon F) :118 | 3:11:1:225:5:20:0:0 | **Ground_Aggregate** (generic) | aggregate-Company-HQ-Friendly is 3:11:1:225:5:20:**1**:0 - Specific 1 != our 0, not wild -> non-match |
| ScoutUnit (echelon B, friendly) :109 | 3:11:1:225:2:1:1:0 | **Ground_Aggregate** (generic) | Category=2 has no Kind=11 leaf |
| MobileIrregular (echelon B, hostile) :121 | 3:11:1:0:13:34:0:1 | **Mobile Irregular.entity** (real) | matches C2simEx matchType 3:11:1:-1:13:34:0:1 |

CAVEAT (project R5 rule + MOJAVE doc part 5/6 tension): the OPD best-match ALGORITHM is
directly documented and the field arithmetic above is exact, but ONE live cross-check is
still outstanding - 2026-07-15 live-queried 114.MechCoy's formation list came back lowercase
([column,line,wedge,vee]), the Ground_Aggregate signature, NOT the Tank Company (USA)
Title-Case signature this static analysis predicts for echelon E. Either the live match
differed, or the query normalizes case. This is the single observation that would falsify the
"ArmorCompany -> Tank Company (USA)" static claim; confirm with a live vrfSim.log OPD-resolve
line (or the 0.5 scnx save) before treating the E-armor match as settled.

### 0.1.6 What the port sends today (for cross-reference)

Verified UnitTranslator.cs. Dispatch is by SIDC ECHELON CHARACTER ONLY (position 11) plus
air/sea/neutral prefixes and hostility - it NEVER reads the function ID (armor vs infantry vs
artillery vs engineer). Branches (UnitTranslator.cs:39-67):
- SIDC pos 2 = 'A' -> air (Rw/Mq1); pos 2 = 'S' or DisDomain 3 -> Boat; pos 1 = 'N' -> Civilian.
- echelon 'B' -> ScoutUnit (friendly) / MobileIrregular (hostile) [aggregates].
- 'D' -> ArmorPlatoon; 'E' -> ArmorCompany; 'F' -> ArmorCoHQ ("battalion -> Co HQ") [aggregates].
- anything else present (C, H, dash/NOS) -> Tank() = single M1A2 entity (1.1.225.1.1.3.0);
  no SIDC -> Truck() (hostile) / Tank() (friendly).

Force (friendly/hostile) sets only the VR-Forces force side, NOT the DIS Country - so hostile
armor still gets Country 225 (Tank Company USA composition), never the 222 RUS templates.

### 0.1.7 COA-STP1 population -> mapping raw material (item 5)

Population from `data/COA-STP1_Initialization.xml` (128 Unit blocks; extracted SIDC + Name +
EchelonCode per Unit). EchelonCode distribution: COY 64, BN 26, PLT 23, NOS 12, SECT 2,
BDE 1. Function IDs are APP6C/MIL-STD-2525C positions 5-10; meanings verified against
symbol.army's 2525C symbol list and corroborated by the unit NAMES in the init (MTR=mortar,
REC=recon, TA/RDR=target-acq radar, AVN=aviation, HHC/FSC/CTCP=HQ/support).
KEY 2525C decodes: UCA=Armor, UCI=Infantry, UCIZ=Mechanized Infantry, UCR=Reconnaissance,
UCRVA=Cavalry(Armored), UCE=Engineer, UCEC=Combat Engineer, UCF=Field Artillery,
UCFHE=SP Howitzer/Gun, UCFM=Mortar, UCFR=Rocket, UCFT/UCFTR=Target Acquisition(Radar),
UCAA=**Anti-armor** (NOT air defense), UCD=**Air Defense** (NOT unknown; UCDM=Air Defense
Missile), UCV=Aviation, US=Combat Service Support, USX=Maintenance, USXO=Ordnance,
UULM=Military Intelligence, UUAC=Chemical/CBRN.

NOTE a naming trap the MOJAVE doc hit: the hostile "AD/7151..7154" units (prior sessions
treated as air defense by NAME) carry function ID **UCD = Air Defense** - so the semantics
happened to be right, but the true air-defense code is UCD; the similarly-named UCAA is
anti-armor (its units are named WPN = weapons platoon).

Mapping table. "Now ->" = what the echelon-only dispatch produces today (0.1.5/0.1.6).
Candidates are 1-3 CLOSEST real installed templates with evidence. For hostile (WASA) units
prefer the RUS (Country 222) variant; friendly (NATO) the USA (225) variant.

| COA-STP1 class | function ID / ech / count | Now -> (today) | Closest REAL installed candidate(s) | evidence for closeness |
|----------------|---------------------------|----------------|--------------------------------------|------------------------|
| **Armor company** | UCA / E(COY) / 13 (7 SF, 6 SH) | ArmorCompany -> **Tank Company (USA)** (already real) | Tank Company (USA) 3:11:1:225:5:2 [SF]; **Tank Company (RUS)** 3:11:1:222:5:2 [SH] | Cat5(company)/Sub2(armor) exact family; HU move; real HQ+3xTankPlt. Only hostile side is mis-countried (gets USA not RUS). |
| **Armor/mech platoon** | UCIZ / D + (no pure UCA-D) / mech-plt 1 | ArmorPlatoon -> **Ground_Aggregate** (generic) | **Tank Platoon (USA/RUS)** 3:11:1:2xx:3:2 (armor); **Mechanized Platoon (USA) IFV (Dep)** 3:11:1:225:3:4 or generic Mechanized Platoon 3:11:1:-1:3:4 | Cat3(platoon)/Sub2(armor) or Sub4(mech). Real Tank Platoon = 4xM1A2; mech-plt templates abstract (no subs) - IFV-deprecated is the only composed-ish mech platoon. |
| **CoHQ / bn HQ** | (F->CoHQ) all F(BN) / 26 | ArmorCoHQ -> **Ground_Aggregate** (generic; Spec mismatch) | **aggregate-Company-HQ-Friendly/Hostile** 3:11:1:2xx:5:20:**1**:0 (needs Spec 0->1 to match); **Tank Headquarters Section (USA)** 3:11:1:225:14:2 (real CDR/XO/FSO composition) | The intended CoHQ template EXISTS but our factory's Specific=0 misses its Specific=1 leaf by one field - a 1-field fix would land it. Tank HQ Section is the real armor-CoHQ composition. |
| **Mech infantry company** | UCIZ / E(COY) / 13 (9 SH, 4 SF) | ArmorCompany -> **Tank Company (USA)** (wrong branch: armor) | **aggregate-Co-Infantry-Hostile** 3:11:1:222:5:3 [SH] / **-Friendly** [SF] (HQ+3xInfPlt); generic **Mechanized Company** 3:11:1:-1:5:4 (abstract, no subs) | Cat5/Sub3=infantry company (composed) vs Sub4=mech (abstract). No composed USA mech-inf COMPANY with IFV members exists - closest composed is dismounted Co-Infantry; mech would need authoring or Mechanized Squad assembly. |
| **Recon / cavalry** | UCRVA D(3)/E(2)/F(1); UCR NOS(6) / 12 | D->Ground_Aggregate, E->Tank Company, NOS->M1A2 entity | **Recon Vehicle Platoon (RUS BMP2)** 3:11:1:222:3:6:0:49 [SH plt, composed 3xBMP2]; **Armored Cavalry Platoon/Troop** 3:11:1:0:3:6 / 5:6 (abstract) | Sub6=cavalry/recon. Recon Vehicle Platoon is the only COMPOSED recon aggregate (hostile-side, matching these SH units). Cav Platoon/Troop abstract (no subs). |
| **Engineer** | UCE E(6)/F(2); UCEC E(1)/F(1) / 10 | E->Tank Company, F->Ground_Aggregate | **Tank Breach Company (USA)** 3:11:1:225:5:2:0:78 (only engineer-flavored: armor+mine-plow) | GENUINE GAP: no engineer aggregate exists. Breach Company is armor-with-breach, not a sapper/bridging unit. User adjudication needed. |
| **Artillery / mortar** | UCFHE E(3)/F(3); UCFM D(7)/E(1); UCFR E(1)/F(1); UCF NOS(4)/BN(2); UCFTR C-SECT(2)/E(1) | echelon-driven: E->Tank Company, D->Ground_Aggregate, F->Ground_Aggregate, SECT->M1A2 entity | **Field Artillery Battery (USA) M109** 3:11:1:225:4:8 (SP-how battery) for UCFHE-company; **Field Artillery Platoon (USA) M109/M777** 3:11:1:225:3:8/7 for UCFM/D; **COLT/Fire Support Team** 3:11:1:225:12:27 for UCFTR | Cat4(battery)/Sub8=M109 SP; Cat3(platoon)/Sub8. SP-howitzer templates fit UCFHE. NO dedicated MORTAR template (UCFM borrows a FA platoon) or ROCKET/MLRS aggregate (UCFR; M270 exists only as a platform entity). |
| **HHC / HQ / support** | US E(9)/F(3); USX/USXO/UULM/UUAC E(1 each); UCD(AirDef) D(5)/E(4)/F(3); UCV(Avn) F(2) | US-E->Tank Company; UCD-D->Ground_Aggregate, UCD-E->Tank Company; UCV-F->Ground_Aggregate; NOS/dash->M1A2 entity | CSS: **Combat Service Support Platoon (USA)** 3:11:1:225:3:31 / **Supply Section (USA)** 3:11:1:225:14:31. AirDef(UCD): **Air Defense Artillery Platoon (USA/RUS)** 3:11:1:2xx:3:11 (composed SHORAD). Avn(UCV): **Aviation Assault/Attack Company** (air). MI/Chem/Ord HHC: no specific template -> aggregate-Company-HQ or generic | CSS Cat3/Sub31; ADA Cat3/Sub11 composed (4xAvenger/SA-9). MI(UULM)/Chem(UUAC)/Ord(USXO) have no matching unit template - map to a generic HQ. |

Top-line: of 128 units, today ~64 (all COY/E) become **Tank Company (USA)** - correct only
for the ~13 real armor companies, wrong (armor) for the ~51 mech-inf/engineer/artillery/CSS/
HQ/AD companies; ~49 (all BN/F + PLT/D) fall to the generic **Ground_Aggregate**; ~15
(NOS/SECT/BDE) become single M1A2 entities. The raw material above is what a function-aware
(not echelon-only) Phase-2.1 mapping would use.

### 0.1.8 Uncertainties / flags (do not treat as settled)

1. LIVE-MATCH CROSS-CHECK OUTSTANDING (0.1.5 caveat): static best-match predicts
   ArmorCompany(E) -> Tank Company (USA), but a 2026-07-15 live formation query returned the
   lowercase (Ground_Aggregate) signature for 114.MechCoy. Read a live vrfSim.log OPD-resolve
   line before trusting any specific-match claim. Resolvable with no extra appNo (0.5 scnx
   trick or a verbose creation log).
2. ENGINEER GAP is real: no engineer aggregate in the chain (0.1.3.b/0.1.7). Tank Breach
   Company is the only near-thing. User adjudication (author a template vs accept a proxy).
3. NO COMPOSED USA MECH-INFANTRY COMPANY: Mechanized Company/Platoon (Country 0) are abstract
   (gui-can-create False, no subordinates). The only composed mech-infantry is the deprecated
   USA IFV platoon and the dismounted aggregate-Co-Infantry. IFV-mounted mech-inf would need
   authoring or squad-assembly.
4. NO MORTAR / NO ROCKET aggregate: UCFM(mortar) and UCFR(rocket) have no dedicated aggregate;
   FA Platoon/Battery M109/M777 (tube artillery) are the nearest. M270 MLRS / M142 HIMARS
   exist only as platform ENTITIES, not unit aggregates.
5. Function-ID decodes UCD=AirDefense / UCAA=Anti-armor are from symbol.army's 2525C list +
   unit-name corroboration, not the primary MIL-STD-2525C PDF; high confidence but
   counter-intuitive (AA != anti-air here) - re-verify against the standard if a mapping
   decision hinges on them.
6. The 4 anonymous Ground_Aggregate members (1:1:1:225:4:14:0:0, Cat-4 subcat-14) have no
   exact platform file; what they best-match at creation was not chased to a leaf (flagged in
   MOJAVE doc part 3 too) - cheap to confirm from a live member census (WatchVrf member DIS).
7. US dismounted infantry has no single "US Soldier" .entity; it is a generic Kind=3 life
   form (1:3:1:225:1:1:0:0). The national "* Soldier" files are the concrete DI-Guy platforms.
8. Subordinate compositions in 0.1.3.c are from static `.entity` reads. Per R5, a live create
   can differ (`createSubordinates` semantics not found documented locally - MOJAVE doc part
   3). Treat member counts/types as the file's intent, not a live guarantee.
9. Air/naval/civilian aggregates (0.1.3.d) catalogued by name only, not composition - out of
   the armor/mech/infantry mapping scope; revisit only for COA-STP1's 2 UCV aviation units.

### 0.1.9 Primary sources (all read this pass)

- `.sms` chain: C2simEx.sms:86, EntityLevel.sms:86, base.sms (whole).
- OPD rule: doc/help/Content/SimulationModels/ObjectParameterDatabase/ObjectTypes.htm:185-316.
- Aggregate roster: `grep objectType="3:11:` over EntityLevel + C2simEx (all simObject lines).
- Movement: EntityLevel/vrfSim/systems/movement/{ground-disaggregated,ground-higherUnit-
  disaggregated,human-disaggregated}-movement.sysdef (whole).
- Fallback: EntityLevel/vrfSim/Ground_Aggregate.entity (whole);
  base/vrfSim/base-sim-aggregate.entity (whole).
- Compositions: the starred `.entity` files read directly (subordinates/formations/echelon).
- Platforms: `grep -rl objectType="1:..."` in EntityLevel/vrfSim.
- Port dispatch: src/VrfC2SimApp/UnitTranslator.cs (whole).
- Population: data/COA-STP1_Initialization.xml (128 Unit blocks extracted).
- Function IDs: symbol.army MIL-STD-2525C symbol list + init unit-name corroboration.

---

## 0.5 The .scnx container - what a saved scenario actually is

Source: executor E2 build pass over the installed scenarios (all 68 under
C:\MAK\vrforces5.0.2\userData\scenarios), 2026-07-16; harness at tools/ScnxDiff/
(scnx_diff.py + README.md - the README carries the full rules). Supervisor
re-ran all three acceptance checks independently (dump on TropicTortoise.scnx;
self-diff = zero differences; an independently built one-field mutation detected
as exactly that one field).

1. FORMAT CORRECTION (the plan brief said "XML" - only half right): a .scnx is a
   ZIP holding a MIX. The object model (.oob - the units), scenario master
   (.scn), forces (.xtr), and orbat/plan/address-map (.orb/.pln/.omp) are MAK
   Lisp-style S-EXPRESSIONS, not XML; only .osrx/.spt/.sgr/.ovl/.gui_settings
   are (boost) XML. Anything that parses "the scenario XML" for units is parsing
   the wrong members.
2. ECHELON IDS ARE NOT PERSISTED. Zero `echelon` keys across all 68 installed
   scenarios; the static per-controller (subordinates ) lists are EMPTY at rest.
   Echelon IDs and subordinate lists are runtime-only reconstructions.
3. THE ONLY PERSISTENT ORG LINKAGE is (parent-name "VRF_UUID:<guid>") in each
   object's state-repository ("VRF_UUID:<n> Force" = force-level; "" =
   unattached). Subordinate ORDER = .oob document order; first = leader is an
   ASSUMPTION consistent with the User's Guide (0.2 sec 1) but not
   offline-verifiable - confirm against a live reflected echelon ID in
   Phase 1/2.2.
4. TEMPLATE NAMES ARE NOT IN THE .oob - only the DIS 7-tuple. Structure diffs
   resolve human-readable type names by joining that tuple to the 0.1 catalog.
5. Phase-2.2 diff protocol (implemented in tools/ScnxDiff): match units by
   marking-text (the identity that survives GUI-vs-remote creation);
   canonicalize floats (4 dp, -0.0 == 0.0); ignore identity/volatile fields
   (uuid, object-identifier, raw parent-name, clock snapshots) while diffing
   derived name-based #superior/#subordinates so org changes still surface.
   Known gaps: .pln plans and the boost-XML members not structurally parsed
   yet; uuid-valued semantic cross-references (task targets) flag on
   cross-creation diffs until judged and --ignore'd per tag.

