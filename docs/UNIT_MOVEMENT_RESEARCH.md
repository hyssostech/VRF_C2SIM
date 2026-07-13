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
- R8 (interface-side mitigation - APPROVED 2026-07-12; **IMPLEMENTED + OFFLINE-VERIFIED
  2026-07-12, late night**): opt-in create-time de-stacking. `Vrf:DeStackCreates`
  (default off) + `Vrf:DeStackSpacingMeters` (default 50): units sharing identical init
  coordinates (CoordKey = lat/lon rounded to 1e-6 deg ~ 0.11 m) are spread onto
  deterministic hex rings BEFORE CreateEntity/CreateAggregate - first unit keeps its
  spot, displaced unit n takes ring k's next slot (6k slots at radius k*spacing; the
  54-unit COA-STP1 pile fits within ring 4 = 200 m). Pure helper `DeStacker.cs`
  (entities and aggregates alike; only lat/lon change), applied in ProcessInitialization
  between planning and enqueue; `--destack-selftest` (20 checks: grouping, ring
  geometry, determinism, lon scaling, no-op guards) + a stacked-groups stat in
  `--parse-init`. Attacks the root without touching coa-gpt; parity-breaking, so
  opt-in ("auto" formation mode pairs naturally with it).
  VERIFY by re-running the R5c probe (COA-STP1 + data/E1_Formation_Order.xml) with
  de-stack on - if aggregates then march, the stack hypothesis is definitively closed;
  if not, the terrain-region caveat is back in play.

R8 OFFLINE FINDING (2026-07-12 late night - REFINES the R5c verdict BEFORE the live
A/B): the new `--parse-init` stacked-groups stat shows the GOLDEN init is ALSO stacked
at create time - 10 groups covering 48 of its 49 creatable units, max pile 13 (e.g.
114.MechCoy inside a 13-unit pile; 1222.MechPlt inside a 4-unit pile - BOTH R5
marchers). Cause: most golden subunits carry no own lat/lon and INHERIT the superior's
exact coordinates (InitParser superior-cascade, a faithful port of the C++
C2SIMinterface.cpp:1421-1441 behavior - so every C++ golden run did this too). So
"stacked coordinates" per se is NOT a binary blocker: R5 marched 3/3 out of 4-13-unit
piles. What distinguishes COA-STP1 is pile SIZE/density: ONE 54-unit mega-pile at
34.679985,-116.724799 (over half its creatable units, aggregates AND tank entities,
incl. the E1 probe's control A/4-27) vs golden's max 13. The surviving hypothesis is
therefore "the 54-unit pile gridlocks member form-up", and the R8 live A/B - SAME
scenario, SAME terrain, only de-stack toggled - is now an even cleaner discriminator
than the cross-scenario golden-vs-COA-STP1 comparison.

## 4b. R8 VERIFY RUN (2026-07-12/13 night, LIVE) - STACK HYPOTHESIS FALSIFIED;
## the runaway was GRIDLOCK-SUPPRESSED, not fixed; terrain/region caveat REOPENS

Setup: EXACTLY the R5c probe with only de-stack toggled ON. ResetVrf pre-sweep (1
orphan; appNo 3330 dry + 3331 real) -> PushInit COA-STP1 (128 units, C2SIM) -> app
appNo 3332 (Vrf__AggregateFormation=auto + Vrf__DeStackCreates=true + 20x) -> de-stack
fired on 10 groups INCL. the 54-unit mega-pile (log: "54 units at (34.679985,
-116.724799) spread onto 50 m rings") -> formation repair 113/113 (all lists lowercase,
set 'column' + reorganize) -> WatchVrf appNo 3333 (20 s samples, 20 min) -> pushed
data/E1_Formation_Order.xml (same 7 tasks: 2 CO, 2 CoHQ, 2 PL aggregates + tank entity
control) -> all 7 resolved, routes created, MoveAlongRoute issued -> clean stop
(Solution A deleted 170/170; no stale federate). appNos 3330-3333 consumed.
Raw evidence (de-stack/dispatch/completion log lines + the 7 probe units' full
WatchVrf telemetry): docs/experiments/R8_verify_run_2026-07-13.txt.

RESULTS (WatchVrf displacement from spawn, 59 samples/unit):
- CONTROL 4x FASTER: tank A/4-27 marched 1.85 km and COMPLETED in ~3.5 min (E1/R5c:
  ~13 min stack-escape). Correct TASKCMPLT attribution. De-stacking REMOVED the
  pile-escape delay - the mega-pile gridlock was real and is fixed by R8.
- CoHQ CREATION IS CLEAN NOW (progress on the separate failure mode): both CoHQs sat
  INTACT at their spawn points after create+repair (samples t=23-43 s, order already
  arriving) - no E1/R5c-style creation scatter. The "AR HQ Sec ... Column-Left"
  create warning still logs (repair lands after create), but members stayed put.
- BUT STILL 0/6 AGGREGATES MARCH, and the failure mode FLIPPED BACK to E1's:
  - Companies RUNAWAY re-expressed: 3/7159 drove ~124 km out (still driving at window
    end, then partial return); B/40 (the mega-pile's kept-in-place first unit) idled
    ~3 min post-dispatch then moved ~31 km and froze. Sustained rates 28-49 km/h for
    3/7159 (plausible ground driving); B/40's initial burst reached ~110 km/h before
    freezing - hot, but well below the CoHQ-style >130 km/h sustained warp. Either
    way they move far past their 1.1 km routes instead of executing them.
  - CoHQs SCATTER ON TASKING (not creation): intact until the move dispatch, then
    76-93 km displacement within ~1-2 min wall at 20x (>130 km/h = member scatter/warp
    dragging the aggregate position, not driving), then drift/hold.
  - Platoons: ~60 m local shuffle, no march (unchanged across E1/R5c/R8).

VERDICT (per the pre-registered decision rule): stacked coordinates - including the
pile-size refinement - are FALSIFIED as the sufficient blocker for COA-STP1 aggregate
marching. R8 is a genuine improvement (entity ops 4x faster, clean CoHQ creation,
recommended ON for stacked scenarios) but does NOT unlock aggregate marching.
REINTERPRETATION of R5c: its "runaway eliminated - units hold" was the mega-pile
GRIDLOCK physically suppressing the runaway, not the create-time repair fixing it;
de-stacking released it. (The repair itself remains necessary + verified - golden R5.)

SURVIVING HYPOTHESIS - GEOGRAPHY/TERRAIN CONTENT at the scenario region: identical
code + repair + sane geometry marches at the golden region (Sweden 58.7,16.4) and
runs away / scatters / stalls at the COA-STP1 region (Mojave 34.7,-116.7). Terrain
fact established this run (vrfSim.log): the backend scenario runs whole-earth "MAK
Earth Space (online).mtf" - the SAME terrain system for both regions - so the
discriminator is the streamed CONTENT at each location (elevation/roads/pathfinding),
not a different terrain file. RESIDUAL ALTERNATIVES (not yet excluded): (a) the 20x
TimeMultiplier interacting with the lead-follow/catchup controllers (golden R5's
multiplier is not recorded; its ~3-min completions suggest it was also fast); (b)
init-content differences beyond DIS type (hierarchy/echelon/superior fields - the
created DIS types are identical in both scenarios).

R9 (NEXT - the region-swap discriminator, cheap): synthesize an init that places the
EXACT golden unit set that marched in R5 (1222.MechPlt, 114.MechCoy, 1.BdeHQ + their
subordinate context) at the COA-STP1 Mojave coordinates (dispersed, de-stack
irrelevant), run the R5-style one-move-per-unit probe at 20x. If they fail in Mojave
-> geography CONFIRMED as the blocker (then: coa-gpt feedback "pick regions with road
content", or test planAndMoveToTask/off-road settings). If they march in Mojave ->
geography falsified too; next suspects are init content (b) and multiplier (a) - hold
20x constant and vary one at a time. Optionally also the mirror swap (COA-STP1 units
at the golden Sweden coordinates).

## 4c. R9 REGION SWAP (2026-07-13 morning, LIVE) - GEOGRAPHY CONFIRMED; MECHANISM
## FOUND: the leader path plan is EMPTY at the Mojave region

Files (tracked): data/R9_Mojave_Initialization.xml + data/R9_Mojave_UnitMove_Order.xml
- the golden init + R5 order transplanted Sweden (~58.69,16.5) -> Mojave (~34.6,
-116.6) by a pure coordinate transform that PRESERVES ground geometry (dLat 1:1; lon
offsets scaled by cos(58.69)/cos(34.6)=0.6313; spot check: a 9344 m inter-pile
distance maps to 9346 m). Offline: --parse-init 80/49/4 + the same 10 stacked groups
as golden; --parse-order 3 tasks with routes adjacent to their units (~578 m PL leg,
same as golden). Raw run evidence: docs/experiments/R9_region_swap_2026-07-13.txt.

Run A - MOJAVE (app 3336 / watch 3337; ClientId=STP, auto formation 40/40, 20x,
NO de-stack - identical to golden R5 except location): all 3 tasks dispatched;
**1/3 completed** - the entity control 1.BdeHQ drove its full ~1.16 km route and
completed within ~1 min; 1222.MechPlt moved 8 m TOTAL and froze for 18 min;
114.MechCoy moved 410 m the WRONG direction in ~2 min and froze. No runaway.

Run B - SWEDEN CONTROL (same day, same code, same settings, original golden init +
R5 order; app 3339 / watch 3340): **3/3 completed within ~4 min** - both aggregates
physically marched (telemetry 0.7-1.1 km) and reported TASKCMPLT. This one run
excludes BOTH residual alternatives in sec 4b: today's code (incl. the R8 two-pass
ProcessInitialization restructure) is good, and 20x is compatible with marching.

MECHANISM (vrfSim.log, decisive): in the Mojave run window the backend logged, 3x
per aggregate, `<unit>: moveAlong() - empty route -- not sending move along to
subordinate`, and created **ZERO** member "Offset Route" objects; the Sweden control
window created 45 (original golden R5 window: 34). So at Mojave the lead-follow
controller's LEADER PATH PLAN comes back EMPTY - nothing is ever forwarded to the
lead subordinate and the unit never marches. Entity moves complete at both regions
because entity move-along does not go through unit leader-path planning. The
`moveAlong() - empty route` line is THE grep oracle for this failure mode.

VERDICT: aggregate movement is blocked by TERRAIN CONTENT AT THE SCENARIO LOCATION
- the whole-earth "MAK Earth Space (online)" scenario supports unit ground path
planning at the golden Sweden site but returns empty plans at the COA-STP1 Mojave
site. This is a VR-Forces/terrain characteristic, NOT an interface defect: the
interface's command stream is identical in both runs. It also retro-explains the
COA-STP1 platoon freezes (and plausibly the CoHQ move-time scatter; the company
RUNAWAY there may be a second-order expression - COA-STP1 companies got *some*
plan where the golden-derived R9 companies got none; unproven, low priority).

NEXT (in order):
- R10 (the practical COA-STP1 unlock, live-gated): SUBORDINATE FAN-OUT fallback (the
  original plan's R7) - when a unit's move is wanted at a region where leader-path
  planning fails, task the unit's member ENTITIES directly (entity moves are PROVEN
  at Mojave - A/4-27 completed twice, 1.BdeHQ once). Members revert to unit control
  on completion (UnitMembersTaskIndependently.htm). Trigger options: opt-in setting
  first (Vrf:SubordinateFanOut); auto-detect via the empty-route backend line is not
  visible to the interface, so a completion-timeout heuristic would be the auto lever.
- R11 (cheap probe, same session as R10): DtPlanAndMoveToTask on ONE aggregate at
  Mojave - does the pathfinding point-move task plan where moveAlongRoute's leader
  path does not?

R10 + R11 IMPLEMENTED + OFFLINE-VERIFIED (2026-07-13; live verify pending):
- Facade/bridge (one VS18 rebuild, links vrfExtObjectsHLA1516e.lib): `GetAggregateMembers`
  (reads the reflected aggregate's PUBLISHED entities designator list; resolves each
  via entityList()->lookupEE -> uuid + marking; same static_cast caveat as
  TryGetEntityGeodetic - pass aggregate uuids only) and `PlanAndMoveTo(uuid,
  controlPointUuid)` (DtPlanAndMoveToTask; NOTE DtMoveToTask addresses a control-point
  OBJECT, not raw coordinates - create a waypoint first).
- App: `Vrf:SubordinateFanOut` (default off) fans an aggregate's along-route move out
  to its members (route path only; 0 published members -> loud log + normal aggregate
  move); the unit-level TASKCMPLT is synthesized when ALL fanned members complete
  (pure FanOutTracker + `--fanout-selftest`, 16 checks; supersession cancels the
  fan-out). `Vrf:AggregatePlanAndMove` (default off, experiment-only; takes precedence)
  makes an aggregate move create "<task> WPT" at the route's final point and issue
  PlanAndMoveTo on waypoint-created.
- All eight offline selftests green. LIVE decision rules - R10: platoon/company members
  march their R9-probe routes at Mojave and ONE unit-level TASKCMPLT per task arrives
  when the last member finishes; R11: any member/unit movement after PlanAndMoveTo
  where move-along planned empty.

**R10 LIVE-VERIFIED AT MOJAVE (2026-07-13 morning) - THE FAN-OUT UNLOCKS AGGREGATE
TASKING WHERE LEADER-PATH PLANNING FAILS. 3/3 completions on the R9 probe.**
- Run 1 (app 3342/watch 3343): the PLATOON fan-out worked first try - 1222.MechPlt
  published 4 member entities (R 1, AT4 1, AT4 2, R 2), all 4 got MoveAlongRoute on
  the probe route, ALL 4 marched and completed at Mojave (~3 min at 20x), and the
  unit-level TASKCMPLT was synthesized with the CORRECT task uuid + taskee. The
  COMPANY published NO entities() members -> clean fallback to the aggregate move
  (froze, as R9 predicts). LIVE FINDING: company-type units publish their elements
  as SUB-AGGREGATES, not entities.
- Facade fix (same session): GetAggregateMembers now RECURSES into the published
  subAggregates() designators (lookupEA -> extAggregateStateRep, depth-capped at 3),
  collecting entity members at every level.
- Run 2 (app 3345, recursion in): **3/3 COMPLETIONS** - 1222.MechPlt 4/4 members,
  114.MechCoy **18/18 members** (16x M1A2 + M3 + HMMWV/AUV - the recursion surfaced
  the full company), 1.BdeHQ entity control; every unit-level TASKCMPLT synthesized
  with correct attribution; clean stop (Solution A 56/56). The SAME units at the SAME
  Mojave location scored 1/3 in R9 with unit-level tasking.
VERDICT: Vrf:SubordinateFanOut is the working mitigation for path-plan-dead regions
- COA-STP1 is now functionally UNBLOCKABLE at its own location (re-run pending).
Residual semantics note: fanned members move as INDEPENDENT entities (no formation
keeping; members revert to unit control on completion), and a patrol/never-completing
member leaves the unit task open - acceptable for the movement-projector use case.

**R11 NEGATIVE - AND A TRAP (2026-07-13, LIVE at Mojave, app 3347): DtPlanAndMoveToTask
completes VACUOUSLY at the path-dead region.** Both aggregates' plan-and-move-to tasks
"completed" (~60-90 s after dispatch) and TASKCMPLT reports fired - but a WatchVrf
position check (appNo 3348) showed BOTH units sitting EXACTLY at their spawn points
(platoon at -116.600487 vs destination -116.587860; company at 34.647629 vs
destination 34.657629). ZERO movement. So at a region where the path planner fails,
DtPlanAndMoveToTask reports FALSE SUCCESS - strictly worse than moveAlongRoute's
silent freeze, because it corrupts the C2SIM report stream with phantom completions.
DO NOT use Vrf:AggregatePlanAndMove as a fix; it stays an experiment-only knob with
this caveat. (It also means: any future "did it move" claim needs TELEMETRY, not just
a completion event - completions can lie. The R10 verdicts below were telemetry-
verified for exactly this reason: the R10 watch window shows the platoon + company
member cohorts physically marching 1.1-1.3 km, per-object displacement analysis.)

**COA-STP1 UNBLOCKED (2026-07-13 morning, LIVE, app 3350): de-stack + fan-out on the
scenario's OWN units at its OWN location - 5/7 unit completions where R5c scored
0/6+control.** Setup: COA-STP1 init (128 units), Vrf:DeStackCreates=true (10 groups
spread incl. the 54-pile) + Vrf:AggregateFormation=auto (repair 113/113) +
Vrf:SubordinateFanOut=true, the E1 7-task probe order, 20x. ALL 6 aggregates fanned
out (recursion surfaced every roster): companies B/40 + 3/7159 18 members each,
CoHQs 7913/HQ_71 + 7159/HQ_71 4 GndV each (the historically scatter-doomed units
enumerate cleanly), platoons AD/7152 + 1/1/8072 4 each - 52 members + the control.
RESULTS: BOTH platoons COMPLETED (~4 min), BOTH companies COMPLETED (18/18 members
each - B/40 spawned at the mega-pile center and had failed in every prior
configuration), control COMPLETED; the CoHQs each finished 3 of 4 members with ONE
GndV straggler still driving/stuck at the ~35-min window end (unit task left open -
the documented fan-out caveat). 46/52 members marched. Clean stop 170/170.
Port-exhaustion errors recurred throughout (P4 bundling is overdue).
FOLLOW-UP (fan-out robustness): a straggler policy - completion QUORUM (e.g.
Vrf:FanOutCompletionFraction) and/or a per-member timeout that synthesizes the unit
completion with a warning - so one stuck vehicle cannot hold a unit task open. Also
consider fanning the single-point MoveToLocation path (fan-out currently covers the
multi-point route path only).
- coa-gpt feedback item #4 (evidence-backed): scenario REGION determines whether
  disaggregated units can maneuver at all in VR-Forces' online-earth scenario;
  validate a region with a 1-unit probe before generating COAs there, or pick
  regions with known-good ground content (the golden Sweden site works).
- CoHQ subordinate scatter needs its own investigation (member telemetry on ONE CoHQ
  through create->repair) - it is a distinct failure mode from the stack.
- Then: make query-driven auto the recommended default for aggregate-bearing scenarios
  and re-test MoveIntoFormation (E2) now that preconditions are sane.

**COA-STP1 FULL 42-TASK SCALE RUN (2026-07-13 afternoon, LIVE, apps 3355-3359):
PIPELINE HOLDS AT SCALE; P4a + STEP-2 PASS THEIR LIVE GATES; MOVEMENT MIXED -
F1 RUNAWAY UNDER FAN-OUT, F2/F2b VACUOUS-COMPLETION CLASSES, F3 TIMEOUT RACE.**
Setup: the FULL COA-STP1 order (42 tasks, 11 performers, 32 temporal deps) on the
128-unit init at its own Mojave region. Vrf: DeStackCreates=true,
AggregateFormation=auto, SubordinateFanOut=true, TimeMultiplier=20,
FanOutStragglerSeconds=600, FanOutCompletionFraction=1.0,
TaskPredecessorTimeoutSeconds=600 (explicit); P4b NOT in the build (deliberate,
Step 3 ordering note). App carried SDK ClientLib 4.8.3.3 (P4a) + the Step-2
FanOutTracker. Procedure per RUNBOOK sec 7 + OPUS plan 5.2; ~53 min observation
at 20x; WatchVrf 3600 s x 20 s sampled 1785 objects. Raw evidence:
docs/experiments/COA-STP1_scale_2026-07-13.txt.

PIPELINE AT SCALE - PASS: 128 units + 35 areas dispatched; the mega-pile de-stack
fired verbatim ("DeStack (R8): 54 units at (34.67998497486787,-116.72479854165415)
spread onto 50 m rings", + 9 more groups); formation repair 113/113; 14 fan-out
registrations + 1 aggregate-move fallback (1-1 RECON publishes no members); the
order fully DRAINED: 15 dispatched-and-completed / 5 no-location / 21 skip-gated /
1 never-dispatched (T13, the 3h20m SimulationTime-delay task) = 42. Clean stop
(Solution A "178 deletes dispatched (1623 ms)" + resign), post-run ResetVrf found
only 1 leftover; no stale federate. AppNos 3355-3359 consumed.

P4a - PASS LIVE (the discriminator): grep over the full ~50-min 20x log with heavy
position reporting: ZERO "Only one usage of each socket address", ZERO "Connection
error:". Every pre-P4a live run had them; the shared-HttpClient fix is proven.

STEP-2 MECHANISM - PASS: 9 quorum syntheses + 5 straggler-timeout syntheses (the
warnings fired: 1-35/2/1_A 0/4, 1-6/2/1_AD 0/4, C/1-35 0/18, 40/2/1_AD 0/4,
856/HHC 7/18); ZERO "NO in-flight task recorded" (the swallow gate held); no
double TASKCMPLT. 15 unit TASKCMPLTs total (R5c-era scored 0/6; the 7-task probe
5/7). BUT see F3: the lever's mechanics worked while its successor-unblocking
value was nullified by configuration.

MOVEMENT (telemetry-verified ONLY - the R11 rule): 4 units genuinely marched
their 22-40 km routes: 856/HHC 24.1 km FULL 18/18 quorum, cohort n=23 ending at
the corridor endpoint (min 36 m) - the showcase; 1-35/2/1_A and 40/2/1_AD 28.5 km
with cohort arrivals at the objective (synthesized EARLY at 0/4 - members arrived
after the 600 s synthesis); C/1-35 40.2 km, 15 movers near the T39 endpoint,
aggregate overshooting 6.2 km past; 5-20/2/1_A's first leg (T31) real. Census:
130/1785 objects moved >50 m, 61 >500 m, 0 objects >50 m in the final 600 s (the
order had drained, not stalled). Roughly HALF the 15 completions are NOT
displacement-backed:

- F1 RUNAWAY UNDER FAN-OUT: 1-6/2/1_AD drove 53.8 km on its correct 218-deg axis,
  18.4 km PAST its 35.5 km route end, never stopping in-window; member monsters
  on-axis at 166.7 km and 67.2 km. The R8-era overshoot pathology re-expressed
  under fan-out for ONE unit; its 0/4 straggler synthesis reported completion for
  a unit that was actually running away.
- F2 R11 VACUOUS COMPLETION REPRODUCED on the run's ONE unfanned aggregate move:
  1-1/2/1_AD (T23) sat at EXACTLY (34.678715,-116.726343) for the entire window -
  zero displacement over 166 samples - yet "VRF task complete: 1-1/2/1_AD /
  move-along" fired and its TASKCMPLT was the SECOND sent. moveAlongRoute itself
  can complete vacuously at this region, not just DtPlanAndMoveToTask.
- F2b NEW CLASS - MEMBER-LEVEL VACUOUS COMPLETIONS: full member quorums with ZERO
  telemetry arrivals at the route ends: 4-27/2/1_A (all 3 tasks; aggregate moved
  0 m; zero >500 m movers within 3 km of any of its three endpoints), B/5-20
  (T35 largely, T36 zero arrivals), 5-20/2/1_A legs 2-3 (T32/T33 zero arrivals).
  Entity-level MoveAlongRoute completions can ALSO lie on some routes at this
  region - the R11 trap is not confined to unit-level or PlanAndMove tasking.
- F4 UNEXPLAINED: one 148.0 km mover due NORTH (bearing 8 deg) matching NO task
  axis - reported as unexplained, not folded into a category. (All six >60 km
  movers began moving in the same t=1025-1125 s sample band, the successor-
  dispatch wave.)
- F3 THE 600==600 TIMEOUT RACE (config lesson, no code defect): with
  FanOutStragglerSeconds == TaskPredecessorTimeoutSeconds == 600, every straggler
  synthesis fired in a dead heat with its successors' predecessor windows - and
  the SKIP won every time (verified: T2's skip names T1's uuid the same instant
  T1's synthesis fired). Straggler-synthesized completions unblocked NOTHING;
  only fast quorum completions opened successor chains (4-27, 5-20, 856, B/5-20
  ran 2-3-task chains). ALSO: 600 s was sized for R10's 1.1-1.3 km routes; this
  order's 13-40 km routes take far longer at the same speeds, so the timeout
  fired at 0/N for units MID-MARCH (a route-length artifact, not a stuck-member
  detector) - premature completions for units that later arrived (1-35, 40,
  C/1-35) or never would (1-6). RULE FOR NEXT RUNS: scale the straggler timeout
  with route length and set it meaningfully BELOW the predecessor timeout.

NEXT (in order): (1) re-run tuning - straggler timeout sized to route length
(and/or the Step-2.3 idle-timeout refinement) and < predecessor timeout, to see
the synthesis lever actually unblock successors; (2) F2b/F1 investigation - a
per-route 1-member probe matrix at this region (which routes vacuous-complete,
which overshoot; vrfSim.log oracle) before trusting member completions there;
(3) P4b gets its own SHORT live pass (per the Step-3 ordering note) now that the
P4a verdict is clean.

## 5. What the research could NOT settle (open)

- What exactly VR-Forces does at creation when the formation is unresolvable
  (undocumented; observed: warnings + stacked-or-scattered members).
- Whether setAggregateFormation emits the snap-flavored DtSetFormationRequest or the
  name-only DtSetFormationTypeRequest (compiled; docs imply snap for the GUI path).
- Why exactly the companies runaway (member telemetry R3 will answer).
- Where our creates' "column-left" default state string originates (VRF default vs
  create-message default) - cosmetic once R1 lands, but worth one look in VrfFacade.
