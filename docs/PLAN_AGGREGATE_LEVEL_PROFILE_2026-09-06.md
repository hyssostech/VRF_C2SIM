# PLAN: the aggregate-level profile (Y-15 realized) - added to the plan 2026-09-06 (user: "Add to the plan as suggested")

Position in the sequence (project-demo-ready-milestone): (1) COA-STP1 = 11 units on EntityLevel
with order-time materialization (C13; PREREG_ORDER_TIME_MATERIALIZATION) -> (2) DEMO-READY
(DEMO_READINESS_2026-09-06) -> (3) THIS profile, built so that (4) the STP task vocabulary
(goal-full-stp-task-vocabulary; STP_TASK_VOCABULARY_2026-09-03) targets BOTH model sets.
Y-15 (VRF_5.2_MIGRATION_DIFF sec G, ruled 2026-09-03) already says: EntityLevel for company-
and-below COAs, AggregateTacticalLevel for battalion+ - a PROFILE (fixture + type map) per run.

## What the vendor material says (read 2026-09-06 22:1xZ; nothing from samples or community)
- UG52 13 p368: the model set is a PER-SCENARIO choice ("you must consider which type of
  modeling you want to use ... and remember which type you are using when you populate the
  scenario"); no mixing (13.7). "Aggregate-level modeling only works with HLA Evolved, HLA 4,
  and MAK FOM extensions" - our MAK-ONE-2025 connection config loads VRFAggregate-7 + NETN
  (DIFF A2), so the protocol requirement is met on paper.
- UG52 27.1: a unit is ONE simulated object with combat power / health (attrition), a four-
  sector footprint, posture, logistics, engineering objects, reports. 27.1.4 movement: ordered
  speed modified by slope, terrain class, roads, rivers, MOPP, posture, and footprint overlap
  ("Max Speed Modifier When Overlapping Another Unit"); "Aggregate-level scenarios do not
  support collision avoidance among simulation objects" - the entity jam of 2026-09-06 cannot
  occur there.
- UG52 35 (Tasks for Aggregate-Level Scenarios): Move, Move Along Route (+Retrograde), Move To
  (+Retrograde, Plan Along Roads), Patrol Route/Between, Halt, Turn To Heading, Change Posture,
  Change MOPP, Escort/Wait for Escort; Engagement: Attack by Fire, Indirect Fire, air/missile/
  torpedo/NBC families, Automatic Air Defense; Combat Engineering; Air Base; Embarkation;
  Radio; Other (FASCAM, reports, User Task). "Some tasks are identical to tasks supported by
  simulation objects in entity-level scenarios."
- Catalog (C:\MAK\vrforces5.2d\data\simulationModelSets\AggregateTacticalLevel\vrfSim): 209
  unit types (418 files: .entity + .magx); USA 43 (Tank CO / Tank CO PA (M1A2), Tank BN HQ,
  Mech CO (M2 / ACV), HHC PA (CAB), CSS CO/PLT, FA BTY/PLT (155mm), ADA BTY (Patriot), Recon CO,
  Rifle CO, Armor CAB Group, BN GCE (USMC), ...); BLR 31; RU 4 (hostile side is THIN - BLR or
  a config choice). Echelons: PLT 78, BN 78, CO 56, BDE 11, Group 10. Each type ships a .magx
  = the vendor's own object-type -> template mapping used by its ORBAT/MSDL importer
  (vrfSim --help: "Merges all unit type mapping files (*.magx)") - the same job as our
  fidelity table; the same aggregate code (11:1:225:5:2:0:0) is Tank Company (USA) with 6
  subordinate types in EntityLevel and Tank CO (USA, M1A2) with 0 in AggregateTacticalLevel.
  [CORRECTED 2026-09-27 by the survey, docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.md: the chain
  (AggregateTacticalLevel -> AggregateLevelBase -> base) holds 453 kind-11 templates and 401 .magx entries.
  RUS is NOT thin: it is DIS 260 in this catalogue (137 templates deployable "RU"), 131 ground templates, 107
  warfare-model units; "RU 4" counted the label spelling "(RU,". No nation has a warfare-model UNIT at BDE, DIV,
  CORPS or RGT - the "BDE 11" are Aggregate Containers, most of them EMPTY generic nodes. The .magx is listed
  only among vrfGui options (UG52 Table 10 p172, --mergeUnitTypeMap) and maps a type to a GUI element definition
  beside the .leaf visualizers; 38 entries name types no template publishes, 15 of which land base-sim-aggregate.
  The sim picks templates from the .entity files, so object types come from there and are resolved.]
- API: model-set agnostic (createAggregate(objectType, ...), moveAlongRoute(uuid, route),
  addToOrganization) - the same calls select a leaf unit or an entity container depending on
  the scenario's SMS. The shipped remote-control sample is entity-level but uses nothing that
  differs. No aggregate-level sample ships (examples/ grep). Web: nothing; MAK community =
  licensee portal (login).

## What to build (order)
1. Fixture on AggregateTacticalLevel.sms: tools/FixtureGen/build_fixture.py SMS_52 constant ->
   `--sms` parameter; same Mojave terrain, same frame mode; validate_fixture.py accordingly.
2. Type map for the aggregate catalog: data/unit-type-map-52-aggregate.json, SAME schema and key
   logic (function ID + echelon + side; isAggregate true; objectType from the .magx files;
   fidelity EXACT/PROXY). Survey first (a script over the .magx files -> table). Hostile side:
   BLR (31 types) or a config choice; RU has 4.
3. Profile switch: runner `-SimulationModel EntityLevel|AggregateTacticalLevel` selecting fixture
   + map together (the SMS is per scenario); the same key in appsettings.Demo.json.
4. Gated runs: (a) one Tank CO (USA, M1A2) leaf on the R9 route (create, reflect, Move Along
   Route, TASKCMPLT, position reports); (b) COA-STP1's 11 units; (c) the verb mapping (STP ->
   UG52 35 tasks) - Attack by Fire / Indirect Fire / Move families first.

STATUS 2026-09-27 - steps 1-3 BUILT OFFLINE on feat/aggregate-profile (RL-20260927-01 moved Iron Storm to this
profile; record docs/experiments/AGGREGATE_PROFILE_OFFLINE_2026-09-27.md). 1: fixture
IronStorm_Centre_52_Aggregate (Iron Storm AOI, not Mojave; `--sms aggregate --terrain aggregate`; not deployed).
2: data/unit-type-map-52-aggregate.json, object types from the .entity files (see the correction above), every row
a movable warfare-model leaf, 40 of 40 Iron Storm units PROXY; RUS and BLR hostile rows both present - which one is
an owner decision. 3: the key is `Vrf:ModelSet` / runner `-ModelSet` (the name the pre-flight lane uses; not
`-SimulationModel`); it selects the map and the runner refuses a fixture/type-map mismatch. Step 4 corrections:
4(a) must not use Tank CO (USA, M1A2) under CreationPolicy=AtOrder - the app's resolver is rooted at EntityLevel and
would EXPAND it (record sec 6.2); the Iron Storm G1 draft uses Mech CO (USA, M2) for 48 IBCT.

## What is reused (verified against the code 2026-09-06)
Bridge + facade untouched; C2SIM parsing, placement (terrain query), route authoring, task
sequencing, reports (position + TASKCMPLT), runner, observers (WatchVrf reflects aggregates),
tools/analysis (taskee_displacement.py works on the unit's own uuid), demo start script,
CreationPolicy (a leaf shell IS the unit - materialization is ~free). Optional there: the
compose/expand machinery (N1-N4) - only for battalions-of-companies via addToOrganization.
Not reused: the entity-level type map; the entity movement diagnosis (C1b, S4/S6).
[CORRECTED 2026-09-27 by reading main b984945: materialization of a leaf is NOT free - MaterializeUnit deletes the
shell and re-creates it (case 3, VrfC2SimService.cs:2623-2651), classifying the type with a resolver rooted at
EntityLevel.sms (:2365). And "reports" is only half reused: position reports read the unit's own position (:1225),
but arrival evidence (C15) and the stall watchdog (C16) read MEMBER positions and skip an aggregate with no members
(TryReadMemberPositions, :7306) - i.e. every warfare-model leaf. Record sec 6.]

## Unknowns (each settled by run 4a)
TASKCMPLT via DtTaskCompleteReport for aggregate-level tasks (assumed same path); a leaf unit's
reflected position (AggregateEntity FOM) and movement speed on this terrain; whether the
aggregate SMS needs settings we have not seen. Falsifier for the whole plan: the first leaf
create does not reflect or does not move.

## Effort
About a day to the first moving run (most of it the catalog survey); the verb mapping is the
vocabulary milestone's own work.
