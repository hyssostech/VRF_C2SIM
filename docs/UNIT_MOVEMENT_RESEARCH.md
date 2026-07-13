# How VR-Forces 5.0.2 moves UNITS - research synthesis (2026-07-12)

Produced after E1 falsified the formation-name hypothesis (PORT.md sec 10 "E1 RUN"),
per the 2-strike research rule: three parallel sweeps of the VENDOR's own material on
this machine - (A) doc/help + PDFs, (B) include/ headers + examples/, (C)
data/simulationModelSets content + sysdefs + Lua - plus targeted local verification.
Every load-bearing claim below carries a file citation; quotes are verbatim.
ASCII-only per repo policy.

## 1. The documented model (what we now KNOW)

1. There is NO unit-specific move task. The GUI's unit "Move Along Route" is the same
   `move-along` task (DtMoveAlongTask) addressed to the AGGREGATE's UUID; the receiving
   unit's CONTROLLER produces unit behavior. "To assign a task to a unit, select the
   unit, then assign it a task as you would any individual entity."
   [doc/help/Content/Tasks/TasksAssign/UnitTasksAssign.htm; include/vrftasks/
   moveAlongTasks.h:22-43; include/vrfmodel/disaggregatedMoveAlongController.h]
   => our moveAlongRoute(aggUuid, route) call shape is CORRECT. The task was never
   the problem.

2. Disaggregated movement mechanics: "VR-Forces calculates the path the unit LEADER
   must follow ... then calculates parallel paths (taking into account the formation)
   for each member. The unit members are responsible for following these paths."
   [doc/help/Content/Modeling/EntityLevel/vrf_closeFormationVsReorganization.htm:185]
   The aggregate distributes these via its COMMAND RADIO: "Aggregates must be
   configured with a command radio for this controller to work properly"
   [disaggregatedMoveAlongController.h:444-447] - both shipped aggregate platforms
   (Aggregate.ope, HigherAggregate.ope) carry a main radio, so this precondition is
   plausibly met by our creates (verified: (radios (main-radio ...)) in both).

3. THE LEADER IS LOAD-BEARING. Subordinate ORDER fixes the leader (echelon designator
   1) and CANNOT be changed after creation [vrf_echelonIDAssignment.htm:185;
   vrf_aggregateLevelAggregatesConcepts.htm:184]. Ground_Aggregate's move-along
   controller is the LEAD-FOLLOW variant ("aggregate-lead-follow-in-formation-
   controller"): it "forwards the move-along task to its LEAD subordinate. All other
   subordinates ... follow in formation. The ... Move Along task [is] complete ...
   when its lead subordinate completes" [moveAlongTasks.h:32-37;
   ground-disaggregated-movement.sysdef]. Shipped formation controllers have
   `auto-promote-in-formation False` [DtFormationControllerDescriptor.xml], and the
   remote API exposes `reorganizeAggregate(leader,...)`: "only useful when automatic
   reorganization is not enabled" [vrfRemoteController.h:1569-1573].

4. FORMATION STATE AT BIRTH is type-dependent and is our smoking gun:
   - Tank Company (USA) uses HigherAggregate.ope, which is BORN initialized:
     `(DtRwString CurrentFormation "Column") (DtRwString OrderedFormation "Column")`.
   - EVERYTHING ELSE we create (Ground_Aggregate fallback = our Scout/ArmorPlatoon/
     CoHQ; Mobile Irregular) uses Aggregate.ope, which defines NO CurrentFormation/
     OrderedFormation - the unit starts UNINITIALIZED, and the backend's compiled
     fallback rebuilds a "working formation" from members' CURRENT POSITIONS
     [disaggregatedFormationController.h:246-262; the vrfmodel.dll "formation is
     uninitialized" string].
   - Subordinate spawn offsets in every shipped .entity are ALL 0,0,0 - members are
     placed BY THE FORMATION at creation. An unresolvable creation formation =>
     UNDOCUMENTED placement (nothing in the docs covers invalid formation at create).
     Observed reality: our creates arrive with state formation "column-left" (invalid
     for these types' lists) -> the 128x "invalid formation name" warnings, stacked or
     SCATTERED members (the CoHQ "AR HQ Sec" sections logged "Column-Left is an
     invalid or malformed formation" and landed 60-90 km away).

5. SET-FORMATION SNAPS; MOVE-INTO-FORMATION DRIVES. "When you set the formation for a
   disaggregated unit, the simulation objects ... SNAP to formation immediately. If
   you want simulation objects to simulate moving into formation ... use the Move
   Into Formation task." [vrf_sets_formation.htm:207] For AGGREGATED units it is pure
   bookkeeping (remembered for next disaggregation). So our SetAggregateFormation at
   move time teleports members into slots computed from whatever geometry the unit
   currently has - it cannot itself cause driving, but bad slot geometry disperses
   members instantly and every SUBSEQUENT move amplifies it.

6. Pathological-geometry levers (documented): formation slot offsets are ABSOLUTE
   meters relative to the leader (column = 25 m steps; company-level .frm files reach
   hundreds of meters and delegate to subordinate-formations like "Column-Left");
   working-formation-from-current-positions inherits whatever spread the members have;
   `catchup-factor 1.5` scales with formation spacing; followers that cannot occupy a
   slot CIRCLE it [SimulationObjectsFollow.htm:184 - matches the observed platoon
   "shuffle"]; and formationUtils.lua's extent math can turn a degenerate spread into
   a huge march leg (transition_into_formation.lua: move distance = max(extent)).

7. Formation NAME hygiene (E1's finding stands, reframed): names are per-type and
   case-sensitive at the C++ set path; the Lua path (formationUtils.
   getValidFormationNameFor) would have sanitized "column-left" -> "column", but the
   remote set/state path does NOT sanitize. Our per-type map (auto mode) picked names
   the backend ACCEPTED - correct and worth keeping - but a valid name is only ONE of
   the preconditions, hence E1's negative.

8. Tasking SUBORDINATES directly is supported ("independent tasking") but REMOVES the
   member from the unit's formation until its task completes
   [UnitMembersTaskIndependently.htm:184-188] - a fallback, not the primary path.

## 2. Diagnosis of the E1 observations (grounded, with confidence)

- Platoons (lowercase "column" set at MOVE time, then move-along): the set SNAPPED
  members into a small local column (the observed tens-of-meters position jumps match
  a 4-member 25m-step column); the unit then never marched. LEADING HYPOTHESIS: no
  established LEAD subordinate (auto-promote False; unit born formation-uninitialized;
  leader/echelon state never repaired), so the lead-follow controller had nobody to
  forward the route to; followers idled/circled. [HYPOTHESIS - test via
  reorganizeAggregate + set-at-create, sec 3]
- Companies (born initialized "Column", higherUnit controller): they DO respond to
  moves - but ran 150+ km past a 1.1 km route. Consistent with parallel member routes
  computed from degenerate member geometry (stacked spawn among DOZENS of co-located
  COA-STP1 units + invalid "column-left" state at registration), then catchup-surging.
  [HYPOTHESIS - needs member-level telemetry or GUI]
- CoHQ: scattered 60-90 km AT CREATION (their auto-created HQ-Section members hit the
  same invalid-formation placement) - broken before any tasking.
- The golden scenario's 14.MechBn Wedge march was REAL (route-tracked): dispersed
  scenario, valid post-create formation set, and (apparently) a workable leader state.
  It is the existence proof that the pipeline CAN march a disaggregated unit.

## 3. The revised plan (replaces the guidance sec 4 ladder from E2 down)

R1 (cheap, app-only, offline-buildable): move the per-type formation set from MOVE
   time to CREATE time - set the valid formation in OnVrfObjectCreated as soon as the
   aggregate's uuid arrives, snapping members into clean geometry at the spawn point
   BEFORE anything else acts on them. Keep `auto` per-type names (E1's map).
R2 (bridge rebuild, VS18): facade + bridge `ReorganizeAggregate(uuid)` ->
   controller->reorganizeAggregate - establish/repair the leader after create (the
   documented lever for auto-promote=False). Sequence: create -> set formation ->
   reorganize -> (later) move.
R3 (bridge rebuild, same batch): member-level TELEMETRY, GUI-independent - reuse the
   ResetVrf reflection machinery to snapshot ALL reflected entity positions
   periodically during an experiment (subordinates included), so runaway-vs-scatter
   is measurable without vrfGui. (Also useful forever.)
R4 (bridge rebuild, same batch): `RequestAvailableFormations(uuid)` via
   DtRequestAvailableFormationsAdmin -> DtAvailableFormationsAdmin (no controller
   helper exists; deliver the admin content per textIf.cxx:705 pattern). Replaces the
   static map with ground truth AND reads back currentFormation - the direct oracle
   for "did my set take".
R5 (experiment, golden scenario FIRST): single platoon-type unit on the STP init
   (dispersed positions): create -> R1 snap -> R2 reorganize -> move-along ->
   expect a real march (14.MechBn-style). Then the SAME sequence on COA-STP1 to
   isolate the stacked-coordinates data pathology.
R6 (data feedback to coa-gpt, third item): unit positions must be DISPERSED -
   dozens of units at literally identical coordinates is pathological for
   disaggregated-unit geometry. (Existing items: distinct AffectedEntity; timing.)
R7 (fallback if R1-R5 fail): task SUBORDINATES directly (supported; members revert
   to unit control on completion) - forfeits unit-level semantics, keep last.

Explicitly NOT the path: a different move task (none exists for units); plans (the
GUI does not wrap unit moves in plans); MoveIntoFormation as the primary fix (it
drives members into slots - useful AFTER geometry/leader are sane, i.e. E2 stays
parked behind R5).

## 4. R1-R5 EXECUTED (2026-07-12, same evening) - THE SEQUENCE WORKS. 3/3 COMPLETIONS.

Implementation (commit history has the detail):
- R2+R4: facade+bridge `ReorganizeAggregate` (controller->reorganizeAggregate) and
  `RequestAvailableFormations` (DtRequestAvailableFormationsAdmin via DtAdminMessage ->
  sendMessageToObject; reply via objectMessageExecutive typed callback ->
  AvailableFormations event). Both PROVEN live: all 40 golden aggregates answered.
- R1 evolved mid-experiment into QUERY-DRIVEN (better than the static map): on aggregate
  creation the app QUERIES the unit's own formation list; the REPLY picks a valid name
  (prefer "column") and issues SetAggregateFormation (snap) + ReorganizeAggregate. First
  reply wins; later replies only log. GROUND-TRUTH FINDING that forced this: the live
  read-backs show ALL 40 golden aggregates accept only LOWERCASE names - including the
  18 company-typed units whose .entity file (Tank Company (USA)) lists Title-Case. The
  runtime formation list does NOT match the static .entity analysis (different effective
  model set / matching than the files suggest), so any static map is unreliable - query
  the unit, always. (This also retro-explains E1: the companies' Title-Case "Column" was
  invalid; their movement was runaway, not marching.)
- R3: tools/WatchVrf - joins as observer, reuses the reflection machinery + geodetic
  reads, samples EVERY reflected object (members included) as CSV. Worked first try.
- CAVEAT on the read-back: currentFormation returns '' even after a set that provably
  took (movement followed); treat the LIST as ground truth and current as unreliable.

R5 RESULT (golden STP init, dispersed positions; appNo 3322; auto mode; P0 fixes on):
create -> query -> set 'column' (from the unit's own list) + reorganize on all 40
aggregates -> data/R5_UnitMove_Order.xml (one MOVE each: 1222.MechPlt ArmorPlatoon-type,
114.MechCoy ArmorCompany-type, 1.BdeHQ entity control) -> **ALL 3 COMPLETED with correct
TASKCMPLT attribution.** WatchVrf telemetry: 1222.MechPlt tracked its route east and
stopped ON the final point (16.5191 vs target 16.5192); 114.MechCoy marched on-axis
north; no runaway, no scatter. The ArmorPlatoon type had NEVER moved in any prior
experiment (E1, Wedge, MoveIntoFormation). THE STUCK-AGGREGATE PROBLEM IS SOLVED for
dispersed scenarios: the missing preconditions were exactly the researched ones -
creation-time VALID formation state (query-driven) + reorganize-established leadership.

R5c RESULT (same evening; COA-STP1, STACKED/identical coordinates; appNo 3325; identical
code + the same 7-task probe order E1 used): the query-driven repair applied cleanly to
ALL 113 aggregates (113/113 replies, all lists lowercase - 64 column-first company-typed,
49 line-first - all set 'column' + reorganize; zero empty lists). Outcome vs the E1
baseline on the identical order:
- RUN HEALTHY: the tank entity control completed at ~13 min (same stack-escape timing
  as E1), so results are interpretable.
- THE RUNAWAY IS ELIMINATED: both company-typed units now HOLD POSITION for the whole
  watch window (E1: 150-170 km runaway). The formation-state repair works here too.
- BUT NO AGGREGATE MARCHES: 0/6 aggregate completions in ~14 min (golden R5: 2/2 within
  ~3 min). Platoons shuffle meters; companies stationary; the CoHQ units STILL fly off
  (163-396 km) - their creation-time "AR HQ Sec" subordinate scatter happens before any
  repair can land and the snap does not gather them.
VERDICT: the create-time repair is NECESSARY (it removes the runaway failure mode
everywhere) but NOT SUFFICIENT on COA-STP1. The same-day A/B - identical code, dispersed
golden 3/3 vs stacked COA-STP1 1/7(control-only) - is strong evidence for the
scenario-DATA pathology (R6): dozens of units spawned at literally identical coordinates
gridlock the members (the control needs ~13 min just to escape the pile; ground
aggregates whose members must form up inside it apparently never do). CAVEAT
(adversarial): region/terrain also differs between the two scenarios; but entities DO
move in the COA-STP1 region, so the stack remains the strongest discriminator - a
definitive isolation would re-run COA-STP1 with de-stacked positions.
NEXT:
- R6 (coa-gpt feedback, now evidence-backed): emit DISPERSED unit positions - stacked
  coordinates are pathological for disaggregated units. THE preferred fix (source data).
- R8 (candidate interface-side mitigation, NOT built): opt-in create-time de-stacking -
  when N units share identical init coordinates, offset each by a few tens of meters
  (deterministic ring/grid) before CreateAggregate. Attacks the root without touching
  coa-gpt; parity-breaking, so opt-in. Decide with the user.
- CoHQ subordinate scatter needs its own investigation (member telemetry on ONE CoHQ
  through create->repair) - it is a distinct failure mode from the stack.
- Then: make query-driven auto the recommended default for aggregate-bearing scenarios
  and re-test MoveIntoFormation (E2) now that preconditions are sane.

## 5. What the research could NOT settle (open)

- What exactly VR-Forces does at creation when the formation is unresolvable
  (undocumented; observed: warnings + stacked-or-scattered members).
- Whether setAggregateFormation emits the snap-flavored DtSetFormationRequest or the
  name-only DtSetFormationTypeRequest (compiled; docs imply snap for the GUI path).
- Why exactly the companies runaway (member telemetry R3 will answer).
- Where our creates' "column-left" default state string originates (VRF default vs
  create-message default) - cosmetic once R1 lands, but worth one look in VrfFacade.
