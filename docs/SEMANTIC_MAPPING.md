# Two-layer semantic mapping: C2SIM TaskActionCode -> VR-Forces vrftasks

Status: IN PROGRESS (port-grounded plan, started 2026-07-11). This is the port's
authoritative plan for START_HERE "immediate next task" #4 / PORT.md sec 10. It
SUPERSEDES docs/TASK_EXPANSION_PLAN.md, which was written against the deprecated C++
interface and prioritized verbs the real orders do not use (see re-grounding below).
ASCII-only per repo policy.

## 1. Goal (unchanged from PORT.md sec 10)

The port today is a BARE MOVEMENT PROJECTOR: `ExecuteTaskOnTick` (VrfC2SimService.cs)
ignores the C2SIM `TaskActionCode` and collapses EVERY task to
SetRulesOfEngagement + SetTarget(no-op) + CreateRoute + MoveAlongRoute. Target: a
two-layer mapping so each C2SIM verb drives the appropriate VR-Forces task composition.

- Layer 1 (C2SIM semantics): parse the ManeuverWarfareTask -> verb + params + affected
  entity + graphics. ALREADY DONE by OrderParser (ActionCode, AffectedEntity, Points,
  Roe, timing are all on OrderTask). The missing piece is a VERB CLASSIFIER.
- Layer 2 (VRF task composition): select/compose the right Dt*Task per verb, exposed as
  intent-level facade verbs (Breach, Attack, HoldObjective, ...) instead of only
  MoveAlongRoute.

## 2. Re-grounding on the port + real order data (2026-07-11, evidence)

Read the ACTUAL current port code and the ACTUAL order files before planning. Findings
that change TASK_EXPANSION_PLAN's priorities:

### 2a. Real verb inventory (grep of the order files)

Golden-trace orders (docs/golden-trace/orders/*.xml): 13 orders, ALL `MOVE`. So the
golden trace exercises MOVE only - every Layer-2 addition is ADDITIVE to it and cannot
regress golden parity (re-run the golden move order after any dispatch edit to confirm).

Post-gold real orders (data/, coa-gpt output):
- `COA-STP1_Order.xml` (42 tasks): ATTACK 10, SECURE 4, SCREEN 3, OCCUPY 3, FIX 3,
  BREACH 3, PENTRT 2, DISRPT 2, DESTRY 2, DEFEND 2, BLOCK 2, SEIZE 1, RETAIN 1, MOVE 1,
  GUARD 1, ESCRT 1, CLRLND 1. Every task (42/42) carries a PerformingEntity AND an
  AffectedEntity.
- `VRF-Approved-5June24_Order.xml`: MOVE 38, SCOUT 29, DEFEND 9, ATTACK 9.

CONSEQUENCE: TASK_EXPANSION_PLAN's "first wins" (EMBARK, DEBARK, FOLLOW, EVACTN) do NOT
appear in either real order. Do NOT implement them first. The real value order is
combat maneuver (ATTACK is the single most common verb: 19 across both orders) + BREACH
(the only native-1:1 verb that is actually present).

### 2b. The "load-bearing blocker" is already solved in the port

TASK_EXPANSION_PLAN sec 6/8 called C2SIM-uuid -> VRF-uuid resolution the main blocker for
every multi-entity verb (it shares the SetTarget no-op bug's root cause). In the PORT this
is already available: `_unitByC2SimUuid` (C2SIM uuid -> CreatedUnit.Name) chained with
`_vrfUuidByName` (Name -> VRF uuid) resolves any init-created entity's VRF uuid. So the
affected-entity resolution for ATTACK/BREACH/... is a two-dictionary lookup, not new
infrastructure. (This also IS the real fix for the SetTarget no-op, PORT.md sec 6.)

Caveat to verify at a live run: an AffectedEntity may be an OPFOR unit that our clientId
did NOT create at init (ProcessInitialization only creates units whose SystemName ==
Vrf:ClientId). If the target is not in our maps, the verb degrades to bare move + a warn
(no silent drop).

RESOLVED for COA-STP1 (2026-07-11, offline): all 19 ATTACK-family tasks carry a present
AffectedEntity, and every COA-STP1 unit is under the SINGLE SystemName `C2SIM` (one
SystemEntityList in the init). So with Vrf:ClientId=C2SIM our app creates ALL 128 units
including the targets, and TryResolveVrfUuid resolves them - the fire targets are in-scope.
(Spot-checked: the ATTACK target d6df3c3d "1-35/2/1_AD" is a BN Unit in the init.) Remaining
live unknown: most targets are DISAGGREGATED AGGREGATES, so how DtFireAtTargetTask behaves
against an aggregate uuid is a run-only question.

### 2c. Header signatures verified against C:\MAK\vrforces5.0.2\include\vrftasks

- `DtBreachTask`: init(); setBreachTarget(const DtUUID&); optional setBreachStPt/EndPt(DtVector).
- `DtTargetEntityTask`: init(); setTargetEntity(const DtUUID&) - persistent target assignment.
- `DtFireAtTargetTask`: init(); setTarget(const DtUUID&); setAutoSelectWeapon(bool);
  setWeaponToFire(DtString); setMaxRoundsToFire(int).
- `DtMoveIntoFormationTask`: init(); setLocation(const DtVector&); setHeading(double);
  setFormationName(const DtString&) - the PROPER aggregate-in-formation move (the real fix
  for the sec-5/sec-10 stuck-aggregate problem; START_HERE next-task #1).
- `DtPlanAndMoveToTask` : public DtMoveToTask (pathfinding move to a point; base setLocation).
- `DtHoldUntilTask`: init(); setSimTimeToStop(double) - for OCCUPY/SECURE hold-in-place.

All are DtSimTask subclasses sent via `controller->sendTaskMsg(DtUUID(uuid), &task)`,
exactly like the facade's existing RunScriptedTask.

## 3. Verb -> intent -> Layer-2 composition (the grounded map)

Intent is the port's TaskIntent enum (VerbMapping.cs). "Impl" tracks what is wired in
Layer 2 today; unimplemented intents fall back to bare movement + a log line (no silent
degrade). This table is the single source of truth for the mapping.

| C2SIM verb(s)                                   | Intent          | Layer-2 composition (VRF)                                   | Impl |
|-------------------------------------------------|-----------------|-------------------------------------------------------------|------|
| MOVE                                            | Move            | CreateRoute + MoveAlongRoute (today's bare path)            | yes  |
| BREACH                                          | Breach          | (approach move) + DtBreachTask(setBreachTarget=affected)    | unit2|
| ATTACK, DESTRY, FIX, DISRPT, PENTRT             | Attack          | advance (move) + DtFireAtTargetTask(target=affected)        | UNIT3 code done, LIVE-pending |
| SECURE, OCCUPY, SEIZE, RETAIN, BLOCK, DEFEND, GUARD | HoldObjective | move-to + DtHoldUntilTask (+ scan sector)                   | no   |
| SCREEN, SCOUT                                   | Reconnoiter     | DtPatrolRouteTask + spot reporting                          | no   |
| ESCRT                                           | Escort          | DtFollowEntityTask / convoy (needs the escorted entity)     | no   |
| CLRLND                                          | Clear           | composite move + engage sweep (NOT DtClearTask; see below)  | no   |
| (aggregate move, any verb, opt-in)              | MoveInFormation | DtMoveIntoFormationTask(setFormationName)                   | no   |

Notes:
- `DtClearTask` is a DtSetDataRequest meaning "CANCEL current task", NOT tactical clear
  (verified header read, TASK_EXPANSION_PLAN sec 3). Do NOT map CLRLND to it.
- MoveInFormation is orthogonal to the verb: it is the proper replacement for the current
  moveAlongRoute + `Vrf:AggregateFormation=Wedge` enrichment, and is the real fix for the
  COA-STP1 stuck-aggregate finding (PORT.md sec 10). It stays opt-in until live-verified.

## 4. Architecture as realized in the port

- Layer 1: `OrderParser` (done) + `VerbMapping.Classify(actionCode) -> VerbPlan`
  (new; pure; offline-testable via `--verb-selftest`). VerbPlan carries the intent, a
  human-readable composition string, and an Implemented flag.
- Layer 2: new `VrfFacade` task methods (Breach, then FireAtTarget/TargetEntity,
  MoveIntoFormation, HoldUntil, PatrolRoute) -> `VrfBridge` managed wrappers -> a switch
  in `ExecuteTaskOnTick` keyed on VerbPlan.Intent. Affected-entity resolution via the two
  dicts (sec 2b).

## 5. Implementation order (units; each with its verification method)

1. [Unit 1 - THIS SLICE] Layer-1 classifier + design doc. `VerbMapping` + `VerbPlan` +
   `VerbMappingSelfTest` (`--verb-selftest`). Executor CONSULTS the classifier and logs
   the mapped intent + composition, but still executes bare movement for every verb -
   ZERO behavior/parity change. Verify: `--verb-selftest` passes offline; app still builds.
2. [Unit 2 - DONE + BUILD + LIVE-DISPATCHED 2026-07-11, commit faa4398] Facade + bridge
   `Breach` (DtBreachTask); dispatch BREACH -> resolve affected obstacle -> Breach deferred AFTER
   the approach MoveAlongRoute (parallel to the ATTACK fire), plus breach-in-place (no points) and
   breach-after-MoveToLocation (single point). Bridge + app build 0/0; `--verb-selftest` green
   (BREACH now Implemented). LIVE (COA-STP1): the BREACH task DISPATCHED without crashing, but its
   affected OBSTACLE (a map graphic, uuid 6977b035...) did NOT resolve to a distinct VRF unit we
   created -> degraded to advance-only (correct fallback). So the breach ENGAGEMENT itself is not
   yet exercised: COA-STP1 breach obstacles are map graphics, not init-created entities, so
   TryResolveVrfUuid misses them - a synthetic order (or resolving obstacle graphics) is needed to
   drive a real DtBreachTask, same shape as the Unit-3 synthetic-target gap.
3. [Unit 3 - CODE DONE + BUILD-VERIFIED + PARTIAL LIVE 2026-07-11] Fires: facade
   `FireAtTarget` (DtFireAtTargetTask, autoSelectWeapon) -> bridge -> dispatch
   ATTACK/DESTRY/FIX/DISRPT/PENTRT. Resolves the affected entity via TryResolveVrfUuid; issues
   the move, then FireAtTarget deferred to AFTER MoveAlongRoute (advance then engage). No-points
   ATTACK fires in place; unresolved OR self-target degrades to advance-only + a log. Bridge +
   app build 0/0; `--verb-selftest` green (ATTACK now Implemented).
   LIVE RUN (COA-STP1, clientId C2SIM, Wedge, 20x): app joined RTI 4.6.1, late-joined 128 units
   + 35 areas, order parsed (42 tasks), clean-stopped (no stale federate).
   - VERIFIED LIVE: `FireAtTarget` executes end-to-end without crashing (2 no-points ATTACK
     tasks issued it via the engage-in-place path); target resolution + the two-dict chain work.
   - FOUND + FIXED LIVE: some coa-gpt fire-support tasks ("ProvidePriorityFires"...) set
     AffectedEntity == PerformingEntity -> FireAtTarget(self), a no-op. Added a self-target guard
     (skip the fire, advance only). The richer fix is provideIndirectFireTask (a later unit).
   - NO REGRESSION: the 7 "NO LOCATION GIVEN" errors are pre-existing order-data (no-point
     non-ATTACK tasks), not from this change.
   RE-RUN on a FRESH federation (2026-07-11, appNo 3261): decisive results.
   - ROUTES/MOVES WORK on a fresh federation: MoveAlongRoute issued 32, TASKCMPLT 3. So the
     prior run's "CreateRoute 32 / MoveAlongRoute 0" WAS the accumulated-federation degradation
     (RUNBOOK sec 7), NOT the code - CONFIRMED. Reload/clean between heavy runs.
   - DATA FINDING (decisive): EVERY COA-STP1 ATTACK-family task has PerformingEntity ==
     AffectedEntity (all 19 self-target: d6df3c3d attacks d6df3c3d, 3ac081eb attacks 3ac081eb,
     ...). So COA-STP1 provides NO real enemy target for fires - the earlier "d6df3c3d is a
     distinct enemy" read was WRONG (only `affected` was compared, not the taskee). The self-
     target guard correctly skipped all 19 (advance-only). i.e. ATTACK == advance for this data.
   - CONSEQUENCE: the fires feature (FireAtTarget vs a DISTINCT enemy) CANNOT be exercised by
     COA-STP1 - it needs a SYNTHETIC order (taskee=friendly unit, AffectedEntity=a distinct
     OPFOR unit uuid, force-aware so ROE permits the engagement). That is the definitive test.
   - COA-GPT DATA-QUALITY finding to feed back (like the timing hygiene, PORT.md sec 10):
     coa-gpt emits AffectedEntity == PerformingEntity for attack verbs, so there is no target to
     attack. For the semantic map to add value, coa-gpt must emit a real distinct AffectedEntity.
   SYNTHETIC ATTACK test - FULL FIRES PATH LIVE-VERIFIED (2026-07-11, fresh federation): a
   1-task order (taskee d6df3c3d, DISTINCT affected 3ac081eb, ATTACK, ROEFree, 4 pts;
   scratchpad/synthetic_attack_order.xml) drove the complete Layer-2 path: taskee -> fb516baa,
   target -> 92a47a93 (distinct), CreateRoute (5 pts) -> MoveAlongRoute -> "FireAtTarget fb516baa
   -> 92a47a93 after MoveAlongRoute". Self-target guard correctly NOT triggered; advance-then-
   engage confirmed; no crash. UNIT 3 IS DONE (build + offline + full live). The remaining
   move-vs-fire nuance (does the unit visibly close+destroy the target, ROE/force-permitting) is
   a VRF-behavior observation, not a code question.
4. [Unit 4 - CODE DONE + BUILD + LIVE-TESTED 2026-07-11, commit faa4398. RESULT: NEGATIVE -
   MoveIntoFormation is NOT the stuck-aggregate fix.] Facade `MoveIntoFormation`
   (DtMoveIntoFormationTask: setLocation + setHeading + setFormationName) -> bridge -> dispatch:
   opt-in via `Vrf:MoveIntoFormation` (formation name; "" = off), AGGREGATE-only; an aggregate move
   issues it to the route's FINAL point (heading = bearing to destination) INSTEAD of moveAlongRoute
   + SetAggregateFormation. Entity moves unchanged (golden parity). Build 0/0; --verb-selftest green.
   LIVE RUN (COA-STP1, clientId C2SIM, Vrf:MoveIntoFormation=Wedge, 20x): the app late-joined 128
   units, and dispatched MoveIntoFormation to **35 aggregate tasks** with valid destinations,
   headings, and formation 'Wedge' - **0 tick failures / 0 SEH / 0 abandon** (the command path is
   sound). BUT: only 1 task COMPLETED the whole run (a move-along ENTITY, not a formation move), a
   position diff (t=0 vs t+5min) showed only 2 of 128 units moved >50m (the move-along entities),
   and the USER CONFIRMED VISUALLY: **no aggregate movement on the GUI.** So DtMoveIntoFormationTask
   dispatches perfectly but does NOT move the DISAGGREGATED COA-STP1 aggregates - it is actually
   WORSE than Wedge+moveAlongRoute (which moved ~3/32; sec 10). STRONG HYPOTHESIS: MoveIntoFormation
   is for AGGREGATED aggregates; a disaggregated set (createSubordinates=true) does not respond to
   it. CONCLUSION: Unit 4 does not solve the stuck-aggregate problem. Keep it opt-in (default off)
   as a tested-but-ineffective lever; the deep-dive continues with a DIFFERENT approach - candidates
   (sec 10 table): `planAndMoveToTask` (pathfinding move-to, may move a disaggregated set), tasking
   the SUBORDINATES individually, aggregating the set first (createSubordinates=false) then moving,
   or a per-unit-type formation. Requires the next live experiment (user-directed).
5. [Unit 5 - Reconnoiter + Escort DONE + BUILD 2026-07-11, commit faa4398; HoldObjective + Clear
   are documented bare-move fallbacks] Reconnoiter (SCREEN/SCOUT) -> DtPatrolRouteTask (patrol the
   created route instead of moving along once, deferred to route-created). Escort (ESCRT) ->
   DtFollowEntityTask on the resolved escorted entity (dynamic, no route). Build 0/0; --verb-selftest
   green (SCREEN/ESCRT Implemented). NOT behaviorally live-exercised in the COA-STP1 run: the SCREEN
   (3) / ESCRT (1) tasks are temporally GATED (startAfterTaskUuid) and did not dispatch within the
   run window (PatrolRoute issued 0, FollowEntity 0) - needs a longer run or an ungated order.
   HoldObjective (SECURE/OCCUPY/SEIZE/RETAIN/BLOCK/DEFEND/GUARD) and Clear (CLRLND) STAY bare-move
   fallbacks: DtHoldUntilTask needs a sim-clock stop time (marginal over a route that already ends
   at the objective), and Clear is an undesigned composite (NOT DtClearTask, which is task-cancel).
   The move-to-objective is the dominant behavior; wiring the hold/scan is a later refinement.

## 6. Risks / open questions

- LAYER-2 BEHAVIOR IS LIVE-GATED. A green build proves the facade/bridge compile+link under
  the MAK set; it does NOT prove VRF executes the task as intended. Every Layer-2 unit needs
  a live run (RUNBOOK sec 7) to confirm real behavior. Offline we can only verify Layer 1
  (classification) and that the command stream is well-formed.
- Affected-entity scope (sec 2b): targets may be OPFOR units not created by our clientId.
  Degrade-to-move + warn is the safe fallback; confirm real coa-gpt target scoping live.
- Task interaction: issuing move + a second task (breach/fire) in the same dispatch may
  race in VRF (last-task-wins) - the right sequencing (task queue vs. deferred-on-arrival,
  like the existing route-created deferral) must be validated live per verb.
- Exact verb spellings are now GROUNDED for COA-STP1 + VRF-Approved (sec 2a); a new order
  could introduce an unlisted code -> it classifies as Move (fallback) + logs. Re-grep new
  orders and extend the table.
- init() lifecycle assumed correct by parity with RunScriptedTask (DtSimTask::init); low risk.
