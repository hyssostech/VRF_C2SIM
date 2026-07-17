# TASK VOCABULARY MAP v2 (Phase 2.3 deliverable)

Groundwork plan docs/VRF_GROUNDWORK_PLAN.md Phase 2.3: "C2SIM verbs -> the NATIVE
task/params the GUI sends, replacing collapse-to-moveAlongRoute where a closer native
task exists." Doc-only, offline. This is DESIGN INPUT for Phase 3 (rebuild) / Phase 4
(truthful execution) - NO code changes are proposed here as done.

Citation convention (same as ground truth):
- `GT 0.2 sec 3a` etc. = docs/VRF_GROUND_TRUTH.md, that section (which itself carries the
  on-disk User's Guide path + verbatim quote for each native-task claim).
- `GT 0.3 sec 4` = the remote-API tasking surface (header file:line inside).
- Doc paths `[Tasks\...\X.htm]` are relative to C:\MAK\vrforces5.0.2\doc\help\Content\ .
- Code citations `file:line` are under
  c:\Users\...\Software\Interfaces\VRF_C2SIM\src\ (the .NET PORT - the shipping codebase).
- Every native-task claim traces to a GT section (which carries the primary doc/header
  cite); every "today's behavior" claim traces to a src/ file:line.

ASCII only. Do NOT treat any "proposed" mapping as implemented - the port ships exactly the
behavior in Section 2 column "today".

---

## 0. TL;DR for the supervisor

1. The port is NOT a pure collapse-to-moveAlongRoute projector any more. It has a wired
   Layer-2 for FOUR intent families (Attack, Breach, Reconnoiter, Escort). But the MOVEMENT
   primitive under every verb is still bare `moveAlongRoute` / `moveToLocation`, and the
   whole HoldObjective family (SECURE/OCCUPY/SEIZE/RETAIN/BLOCK/DEFEND/GUARD) + CLRLND
   collapse to bare movement. That is the "collapse" Phase 2.3 targets.
2. The single biggest native-fidelity blocker is NOT task selection - it is the
   AGGREGATED-vs-DISAGGREGATED constraint: "A unit in the aggregated state only simulates
   movement behavior... Only disaggregated units can engage in combat" (GT 0.2 sec 2). So
   every combat verb (ATTACK-family, DEFEND/BLOCK/GUARD, CLRLND) can only reach a real
   native combat task if the unit is DISAGGREGATED first. Kept aggregated, they can only
   ever be movement.
3. Completions lie in BOTH directions (GT 0.0 item 5; live record). Unit Move Along Route
   reports done at FORMATION LEADING EDGE, not member arrival (premature by design). Entity
   Move to Location has an explicit arrival-radius lever (`setAtDistance`) the port does not
   use. This is why Section 3 recommends KEEP the external sequencer but gate it on
   displacement, not native plans.

---

## 1. INVENTORY - verbs that actually appear in the reference orders

Counted directly from the XML (element `<TaskActionCode>`, the field the parser reads into
`OrderTask.ActionCode` - OrderParser.cs:60, OrderModels.cs:15), NOT taken on trust from
SEMANTIC_MAPPING.md (the counts below were independently re-derived and match it).

### 1a. Which file is "the golden order"?

The Phase 2.3 spec says "the golden order XML - find both in data/". There are two readings;
this doc covers BOTH so the ambiguity is harmless:
- The GOLDEN-TRACE reference orders (the parity oracle the whole port is validated against)
  live in docs/golden-trace/orders/ (NOT data/). The two real ones (`1_VRF_Move_Order.xml`,
  `2_VRF_Move_Back_Order.xml`) are MOVE-only; the rest of that dir are synthetic probes
  authored later for semantic-map testing (BREACH/SCREEN/ESCRT/MOVE).
- The largest ORDER file in data/ is `VRF-Approved-5June24_Order.xml` (the "VRF-Approved"
  order paired with the VRF-All-entities init). It is the closest thing to a "golden order"
  physically in data/.
RUNBOOK sec 2 names the "Golden STP" RUN (clientId STP) but no specific order file, so the
file identity is genuinely underdetermined. Treated below: golden = the golden-trace MOVE
orders; VRF-Approved reported alongside as the data/ candidate.

### 1b. Verb counts (verified)

COA-STP1 order (`data/COA-STP1_Order.xml`, 42 tasks; every task carries both a
PerformingEntity AND an AffectedEntity - SEMANTIC_MAPPING.md sec 2a):

| verb   | count | verb   | count | verb   | count |
|--------|-------|--------|-------|--------|-------|
| ATTACK | 10    | BREACH | 3     | BLOCK  | 2     |
| SECURE | 4     | PENTRT | 2     | SEIZE  | 1     |
| SCREEN | 3     | DISRPT | 2     | RETAIN | 1     |
| OCCUPY | 3     | DESTRY | 2     | MOVE   | 1     |
| FIX    | 3     | DEFEND | 2     | GUARD  | 1     |
|        |       |        |       | ESCRT  | 1     |
|        |       |        |       | CLRLND | 1     |

17 distinct verbs, 42 tasks total.

COA-STP1 sibling orders (same COA, different terrain/lean variants) are all MOVE-only:
`COA-STP1_Sweden_MinimalOrder.xml` 2 MOVE; `R9_Mojave_UnitMove_Order.xml` 3 MOVE;
`R5_UnitMove_Order.xml` 3 MOVE; `E1_Formation_Order.xml` 7 MOVE;
`PC2_EntityFirstWaypoint_Order.xml` 1 MOVE. (So the full-verb spread comes only from
`COA-STP1_Order.xml`; the live-tested variants exercise MOVE.)

GOLDEN-TRACE reference orders (`docs/golden-trace/orders/1_VRF_Move_Order.xml`,
`2_VRF_Move_Back_Order.xml`): MOVE only (1 each). The whole golden-trace parity target is
13 x MOVE, one task per unit (NEXT_SESSION_GUIDANCE.md:240). Synthetic probes in the same
dir add SCREEN 1 / ESCRT 1 / BREACH 1 (authored for the Section-2 Layer-2 live tests, not
capture data).

VRF-Approved order (`data/VRF-Approved-5June24_Order.xml`, the data/ "golden" candidate,
85 tasks): MOVE 38, SCOUT 29, DEFEND 9, ATTACK 9.

### 1c. Dead vocabulary (verbs the code handles that these orders never use)

The port's classifier table (`VerbMapping.Map`, VerbMapping.cs:84-101) has exactly 18 keys:
MOVE, BREACH, ATTACK, DESTRY, FIX, DISRPT, PENTRT, SECURE, OCCUPY, SEIZE, RETAIN, BLOCK,
DEFEND, GUARD, SCREEN, SCOUT, ESCRT, CLRLND. Any other code falls through to `Move`
(VerbMapping.cs:112, unrecognized-but-executed as bare move).

Cross-referencing the table against the reference orders:
- Relative to COA-STP1 + golden-trace MOVE orders: **SCOUT is dead** (mapped, but appears in
  NEITHER; it occurs only in VRF-Approved, 29x). It is a synonym of SCREEN (both ->
  Reconnoiter), so it is covered behaviorally by the SCREEN path even though it never fires
  in COA-STP1/golden.
- Across ALL real orders (COA-STP1 + golden + VRF-Approved): every one of the 18 mapped
  verbs appears at least once. So there is NO fully-dead mapped verb once VRF-Approved is
  included - the table was grounded on exactly these two orders (SEMANTIC_MAPPING.md sec 2a)
  and has no speculative entries.
- The superseded plan docs/TASK_EXPANSION_PLAN.md prioritized EMBARK/DEBARK/FOLLOW/EVACTN;
  those are NOT in the port's table and NOT in any real order (SEMANTIC_MAPPING.md sec 2a) -
  correctly-excluded dead vocabulary (documented, not implemented).

---

## 2. Per-verb mapping table

Grouped by the port's `TaskIntent` (VerbMapping.cs). For each group: the compact
verb/count/today table, then Completion semantics / Phase-4 gate / Caveats notes, because
those differ by ENTITY vs UNIT actor and do not fit one row.

Column "today (port)" cites the exact dispatch. The movement backbone for ALL groups is
`ExecuteTaskOnTick` (VrfC2SimService.cs:601): single route point -> `MoveToLocation`
(:855-860 -> VrfFacade.cpp:493 -> controller->moveToLocation, the `DtMoveToLocationTask`
convenience, GT 0.3 sec 4 :1666); multi-point -> `CreateRoute` + deferred `MoveAlongRoute`
(:872-929 -> VrfFacade.cpp:498 -> controller->moveAlongRoute, the `DtMoveAlongTask`
convenience, GT 0.3 sec 4 :1653). Ground waypoints are altitude-clamped (:699-722).

NATIVE-TASK GROUNDING: entity-level ground movement tasks = GT 0.2 sec 3a; unit-level
tactical behaviors = GT 0.2 sec 3a final bullet ([Tasks\UnitBehaviors\] UnitMovementTasks/
UnitOffensiveTasks/UnitDefenseTasks/MountingDismountingTasks.htm); task classes + params =
GT 0.3 sec 4.

---

### GROUP Move -- verb: MOVE (COA-STP1 1, golden 13+, VRF-Approved 38)

| verb | today (port)                                                              |
|------|---------------------------------------------------------------------------|
| MOVE | multi-pt: CreateRoute + MoveAlongRoute (VrfC2SimService.cs:872-929); single-pt: MoveToLocation (:855-860). Bare movement - the golden-parity path. |

Proposed NATIVE task:
- ENTITY actor: **Move Along Route** (`DtMoveAlongTask`, "move-along") for a multi-vertex
  route; **Move to Location** (`DtMoveToLocationTask`) for a single point. This is ALREADY
  what the port does - MOVE is the one verb where today's task choice is already the closest
  native match. Refinement levers available but unused: `setStartAtClosestPoint`,
  `setTraversalDirection` (GT 0.3 sec 4 moveAlongTasks.h); "Treat Route as Road" / Move to
  Location (Plan Along Roads) for road-following ground vehicles (GT 0.2 sec 3a
  [Tasks\MovementTasks\LocationPlanAlongRoads.htm], ground-vehicle only).
- UNIT (aggregate) actor: same **Move Along Route**, OR **Move Into Formation**
  (`DtMoveIntoFormationTask`) to move a DISAGGREGATED unit to a location in a named formation
  (GT 0.2 sec 3a [Tasks\MovementTasks\FormationMoveInto.htm]; GT 0.3 sec 4). The port already
  has this behind the opt-in `Vrf:MoveIntoFormation` (VrfC2SimService.cs:783-799 ->
  VrfFacade.cpp:603 -> `DtMoveIntoFormationTask`), behavior-verified at Sweden
  (SEMANTIC_MAPPING.md sec 7.3 Run 2: 14.MechBn moved 3990 m, arrived 4 m from dest).
- Key params/defaults: route from nearest vertex to end by default (GT 0.2 sec 3a
  [RouteMoveAlong.htm]); arrival radius override = `DtMoveToTask::setAtDistance` for the
  move-to variant (GT 0.3 sec 4).

Completion semantics of the proposed task:
- ENTITY Move to Location / Move Along Route: completes on ARRIVAL at the point / last vertex
  (GT 0.2 sec 3b). Numeric arrival tolerance is undocumented but `setAtDistance` sets it
  explicitly per task (GT 0.2 sec 3b, GT 0.3 sec 4).
- UNIT Move Along Route: completes when the FORMATION LEADING EDGE reaches the last vertex -
  "premature BY DESIGN"; a trailing/piled/frozen member does NOT delay completion (GT 0.2
  sec 3b, verbatim; GT 0.0 item 5). Pseudo-aggregate move-to completes when the LEAD
  subordinate reaches the waypoint (GT 0.3 sec 4 moveToTask.h:30-33). Both fire early.

Phase-4 truthful-arrival gate MUST for MOVE:
- Do NOT advance the sequencer or emit TASKCMPLT on VRF's own completion for a UNIT move.
  Poll `esr()->location()` on a self-owned reflected list (GT 0.3 sec 5 channel 1) and
  require CENTER (or all-members) displacement within an arrival radius before declaring
  arrival. For an ENTITY move, set `setAtDistance` to the intended radius and cross-check the
  position (GT 0.3 sec 5 arrival-verification recommendation).
- The live record already shows this both ways: aggregate move-complete events sometimes
  never fired (Run 1 breach needed a 300 s fallback) and sometimes fired ~40 s EARLY (Run 2,
  before arrival) - SEMANTIC_MAPPING.md sec 7.3 cross-cutting follow-up.

Caveats / open questions:
- The Mojave/COA-STP1 aggregate FREEZE (empty member offset-route generation) is UNSOLVED
  and is independent of task choice (SEMANTIC_MAPPING.md sec 3 notes; PORT.md sec 10). Native
  task selection will not fix it; the proven interim mover is `Vrf:SubordinateFanOut`
  (task the member entities directly, VrfC2SimService.cs:888-903).
- "Units do not generate subordinate routes until they reach the beginning of the route"
  (GT 0.2 sec 2) - offset routes are lazy, so an arrival gate cannot assume member paths
  exist at dispatch time.

---

### GROUP Attack -- verbs: ATTACK (10/9), DESTRY (2), FIX (3), DISRPT (2), PENTRT (2)

| verbs                        | today (port)                                                                 |
|------------------------------|------------------------------------------------------------------------------|
| ATTACK/DESTRY/FIX/DISRPT/PENTRT | classify -> Attack (VerbMapping.cs:86-90). Resolve AffectedEntity to a VRF uuid; advance via bare MoveAlongRoute, then `FireAtTarget` deferred to move-complete (VrfC2SimService.cs:640-658, 923-926 -> VrfFacade.cpp:661 `DtFireAtTargetTask`). No-points -> engage in place (:735-742). Self-target -> advance only (:648-650). |

Proposed NATIVE task:
- ENTITY actor: today's is reasonable - **Move to Location/Along Route (advance) + Fire At
  Target** (`DtFireAtTargetTask`: `setTarget`, `setAutoSelectWeapon`,
  `setMaxRoundsToFire` - GT 0.3 sec 4; SEMANTIC_MAPPING.md sec 2c). This is entity-level
  combat and works only on a DISAGGREGATED / entity actor.
- UNIT (aggregate) actor: the closer native construct is a **Unit Offensive behavior** -
  Movement To Contact / Attack By Fire / Assault (GT 0.2 sec 3a final bullet,
  [Tasks\UnitBehaviors\UnitOffensiveTasks.htm]). BUT these are combat and therefore require
  the unit to be DISAGGREGATED ("Only disaggregated units can engage in combat" - GT 0.2
  sec 2). An aggregated unit given ATTACK can only MOVE.
- Verb nuance for a future richer map: FIX/DISRPT/PENTRT are distinct tactical effects
  (fix in place / disrupt / penetrate), not literal "fire at self"; COA-STP1 collapses them
  because every task self-targets (below).

Completion semantics: Fire At Target is a Weapon-group task; it does not "arrive". For the
COMPOSITE (advance-then-engage) the movement leg completes per the Move rules above; the
fire leg has no arrival notion. Unit offensive behaviors complete per their own behavior
model (undocumented tolerance).

Phase-4 gate MUST for Attack: gate the ADVANCE leg on displacement (same as MOVE); do not
treat "fire issued" as task completion. If the unit is aggregated, flag that the engage
cannot execute as combat until disaggregated.

Caveats / open questions:
- DATA REALITY (decisive, live-verified): ALL 19 COA-STP1 ATTACK-family tasks self-target
  (PerformingEntity == AffectedEntity), so the fire is a no-op and ATTACK == advance for
  COA-STP1 (SEMANTIC_MAPPING.md sec 2b/5 Unit 3). A real distinct enemy target needs either
  a coa-gpt data fix or a synthetic order.
- Most targets are DISAGGREGATED AGGREGATES; how `DtFireAtTargetTask` behaves against an
  aggregate uuid is a run-only question (SEMANTIC_MAPPING.md sec 2b).
- ACTOR-LEVEL MISMATCH FLAG: the entity-level `DtFireAtTargetTask` is what the port sends
  even when the taskee is a UNIT. Real unit combat = Unit Offensive behavior on a
  disaggregated unit. Sending an entity fire task to an aggregated unit is a probable no-op.

---

### GROUP Breach -- verb: BREACH (COA-STP1 3, golden-synthetic 1)

| verb   | today (port)                                                                       |
|--------|------------------------------------------------------------------------------------|
| BREACH | classify -> Breach (VerbMapping.cs:85). Resolve affected obstacle -> approach via bare move, then `Breach` deferred to move-complete (VrfC2SimService.cs:664-673, 925-926 -> VrfFacade.cpp:613 `DtBreachTask`). No-points -> breach in place (:743-749). Self-target -> advance only (:666-672). |

Proposed NATIVE task:
- ENTITY actor: **DtBreachTask** (`setBreachTarget`, optional `setBreachStPt/EndPt`,
  SEMANTIC_MAPPING.md sec 2c) preceded by an approach Move. Today's mapping IS the closest
  native match - BREACH is "the only native-1:1 verb that is actually present"
  (SEMANTIC_MAPPING.md sec 2a). Engineer breaching is an entity/vehicle capability.
- UNIT actor: no distinct aggregate breach behavior enumerated; a unit breach is executed by
  a (disaggregated) engineer subordinate. Content gap: "no engineer aggregate ... in the
  loaded chain" (GT 0.0 item 4) - so there is no real engineer UNIT type to task anyway.

Completion semantics: DtBreachTask completes on breach completion (behavior-model, no
documented arrival tolerance). The approach leg completes per the Move rules.

Phase-4 gate MUST for BREACH: gate the approach on displacement; the breach itself is a
capability action - judge by effect/state, not by a movement arrival.

Caveats: like ATTACK, all 42 COA-STP1 tasks self-target, so BREACH can NEVER dispatch its
DtBreachTask from COA-STP1 - it degrades to advance-only; a synthetic distinct-obstacle
order is required (SEMANTIC_MAPPING.md sec 5 Unit 2; live-verified at Sweden Run 1: distinct
obstacle 114.MechCoy, marched 5318 m, breach issued).

---

### GROUP Reconnoiter -- verbs: SCREEN (COA-STP1 3), SCOUT (VRF-Approved 29; DEAD in COA-STP1/golden)

| verbs        | today (port)                                                                   |
|--------------|--------------------------------------------------------------------------------|
| SCREEN/SCOUT | classify -> Reconnoiter (VerbMapping.cs:98-99). Multi-point -> CreateRoute + `PatrolRoute` (NOT MoveAlongRoute) deferred to route-created (VrfC2SimService.cs:875, 1088-1096 -> VrfFacade.cpp:622 `DtPatrolRouteTask`). |

Proposed NATIVE task:
- ENTITY actor: **Patrol Route** (`DtPatrolRouteTask`) - patrol back and forth along the
  route (GT 0.2 sec 3a [Tasks\MovementTasks\PatrolRoute.htm]; GT 0.3 sec 4). Today's mapping
  is already this. For screen (perceive/report) add spot reporting via `requestSpotReports`
  (GT 0.3 sec 5 channel 4) - the port's composition string says "+ spot reporting" but only
  the patrol is wired.
- UNIT actor: same Patrol Route, OR a Unit Defense/screen behavior
  ([Tasks\UnitBehaviors\UnitDefenseTasks.htm], GT 0.2 sec 3a). Screen is a security task -
  the unit-level "Screen" behavior is the higher-fidelity match if present.

Completion semantics (CRITICAL): Patrol Route / Patrol Between / Follow NEVER self-complete;
they end only on override or (Follow) target death (GT 0.2 sec 3b). So a Reconnoiter task
has NO natural TASKCMPLT.

Phase-4 gate MUST for SCREEN/SCOUT: do NOT wait for completion - it will never come. Gate on
MOTION (displacement observed) to confirm the patrol is live, and end it by explicit
override/time/area condition, not by a completion signal. Live-verified: SCREEN patrol moved
1222.MechPlt 5634 m with NO TASKCMPLT, correctly (SEMANTIC_MAPPING.md sec 7.3 Run 1).

Caveats: putting a never-completing patrol as a predecessor in a chain STALLS the chain
(GT 0.2 sec 3b; GT 0.2 sec 4 While-block wedge). If a SCREEN task is a STREND predecessor in
COA-STP1, the current external sequencer would time out on it - relevant to Section 3.

---

### GROUP Escort -- verb: ESCRT (COA-STP1 1, golden-synthetic 1)

| verb  | today (port)                                                                        |
|-------|-------------------------------------------------------------------------------------|
| ESCRT | classify -> Escort (VerbMapping.cs:100). Resolve escorted entity -> `FollowEntity` (dynamic, no route) dispatched immediately (VrfC2SimService.cs:679-697 -> VrfFacade.cpp:631 `DtFollowEntityTask`). Unresolved/self -> bare move. |

Proposed NATIVE task:
- ENTITY actor: **Follow Entity** (`DtFollowEntityTask`: target + Behind/Right/Above offsets;
  GT 0.2 sec 3a [FollowEntity.htm]; GT 0.3 sec 4). Today's mapping is this. WART: the port
  passes ZERO offset (VrfFacade.cpp:608 region), so the follower stations ON the leader
  (SEMANTIC_MAPPING.md sec 7.1) - should set a real trail offset.
- UNIT actor: the native group-escort construct is a **Convoy** (Convoy To / Convoy Along) -
  but "The Convoy unit can only be assigned convoy tasks... cannot aggregate or
  disaggregate" (GT 0.2 sec 2, sec 3a). So convoy escort needs a special Convoy unit type we
  do not create. For a normal aggregate, Follow Entity or Follow Along Offset Route
  (GT 0.2 sec 3a [Tasks\UnitBehaviors\FollowAlongOffsetRoute.htm]) is the closest.

Completion semantics: Follow Entity NEVER self-completes while the leader lives (GT 0.2
sec 3b). Same non-completion problem as Reconnoiter.

Phase-4 gate MUST for ESCRT: do not wait for completion; gate on the follower tracking the
leader (relative displacement). Live-verified: 1.BdeHQ followed 14.MechBn 9016 m; it DID emit
a TASKCMPLT (SEMANTIC_MAPPING.md sec 7.3 Run 1) - which per the docs it should not; treat
that completion as untrustworthy.

Caveats: all COA-STP1 tasks self-target, so ESCRT degrades to bare move in COA-STP1 (needs a
synthetic order; SEMANTIC_MAPPING.md sec 5 Unit 5).

---

### GROUP HoldObjective -- verbs: SECURE (4), OCCUPY (3), SEIZE (1), RETAIN (1), BLOCK (2), DEFEND (2/9), GUARD (1)

| verbs                                     | today (port)                                              |
|-------------------------------------------|-----------------------------------------------------------|
| SECURE/OCCUPY/SEIZE/RETAIN/BLOCK/DEFEND/GUARD | classify -> HoldObjective, but `IsImplemented(HoldObjective)==false` (VerbMapping.cs:74-77), so NO Layer-2 dispatch: falls through to BARE MOVEMENT (CreateRoute + MoveAlongRoute / MoveToLocation), same as MOVE, + a log line (VrfC2SimService.cs:627-630). |

THIS IS THE PRIMARY "collapse-to-move" TARGET of Phase 2.3 (7 distinct verbs, 14 tasks in
COA-STP1). Proposed NATIVE task:
- ENTITY actor: **Move to Location** (advance to the objective) then **Come to Stop** /
  **Wait Until** / hold (GT 0.2 sec 3a [ComeToStop.htm]; GT 0.3 sec 4 `DtWaitTask` /
  `DtHoldUntilTask`). The port's intended composition string is "move-to + DtHoldUntilTask
  + scan" but DtHoldUntilTask needs a sim-clock stop time and is not wired
  (SEMANTIC_MAPPING.md sec 5 Unit 5 rationale).
- UNIT (aggregate) actor: these are the verbs with the RICHEST native unit behaviors and the
  BIGGEST fidelity gain:
  - SEIZE/SECURE/OCCUPY -> **Seize/Clear Objective** / occupy-position unit behaviors
    (GT 0.2 sec 3a final bullet, [Tasks\UnitBehaviors\UnitOffensiveTasks.htm] /
    UnitMovementTasks.htm).
  - DEFEND/GUARD/BLOCK/RETAIN -> **Unit Defense behaviors** (defend, guard, block)
    ([Tasks\UnitBehaviors\UnitDefenseTasks.htm], GT 0.2 sec 3a).
  ALL of these are combat/defense tasks -> require the unit DISAGGREGATED (GT 0.2 sec 2).
  Aggregated, they can only be the move-to-objective.

Completion semantics:
- ENTITY move-to-objective completes on arrival (setAtDistance available). A subsequent
  Hold/Wait completes at its sim-clock stop time (`DtHoldUntilTask::setSimTimeToStop`,
  SEMANTIC_MAPPING.md sec 2c) - a TIME contract, not an arrival.
- UNIT defense/objective behaviors complete per their own model (position occupied /
  objective secured) - undocumented tolerance; treat as unreliable.

Phase-4 gate MUST for the HoldObjective group: gate the move-to-objective on displacement
(same as MOVE). "Objective secured/occupied" has no trustworthy native completion - if C2SIM
needs a TASKCMPLT, synthesize it from position-at-objective + dwell, not from the native
task.

Caveats / open questions:
- The dominant real behavior for these verbs IS "move to the objective" (the route already
  ends at the objective), which is why the port leaves them as bare move
  (SEMANTIC_MAPPING.md sec 5 Unit 5). The fidelity question is whether Phase 3 disaggregates
  to run the real defense/seize behavior, or keeps the move + a synthesized hold.
- ACTOR-LEVEL MISMATCH FLAG: every proposed UNIT behavior here is combat/defense and does
  NOT exist for an aggregated actor; and none exist at plain entity level either (they are
  unit-echelon behaviors). So for a lone-entity taskee (e.g. the ~15 COA-STP1 units that map
  to a single M1A2, GT 0.0 item 3) these verbs can ONLY be move + hold, never the unit
  behavior.

---

### GROUP Clear -- verb: CLRLND (COA-STP1 1)

| verb   | today (port)                                                                 |
|--------|------------------------------------------------------------------------------|
| CLRLND | classify -> Clear, `IsImplemented(Clear)==false` (VerbMapping.cs:76) -> BARE MOVEMENT + log. |

Proposed NATIVE task:
- ENTITY actor: move-to + engage sweep (composite); no single native "clear" task.
- UNIT actor: **Clear Objective** unit behavior (disaggregated; GT 0.2 sec 3a final bullet).
- HARD NOTE (verified): `DtClearTask` is a set-data request meaning "CANCEL current task",
  NOT tactical clear. Do NOT map CLRLND to it (SEMANTIC_MAPPING.md sec 3 notes;
  TASK_EXPANSION_PLAN sec 3).

Completion / gate: same as the HoldObjective group - gate the movement on displacement; the
"cleared" state has no trustworthy native completion.

Caveats: composite/undesigned; low occurrence (1 in COA-STP1). Lowest priority.

---

### GROUP MoveInFormation (orthogonal, config-gated, not verb-classified)

Not a C2SIM verb - it is a config lever (`Vrf:MoveIntoFormation`) that REPLACES the movement
primitive for AGGREGATE actors on ANY verb (VrfC2SimService.cs:783-799). Listed for
completeness: `DtMoveIntoFormationTask` (setLocation + setHeading + setFormationName) is for
DISAGGREGATED units per the docs (GT 0.2 sec 3a [FormationMoveInto.htm]); for AGGREGATED
units it is equivalent to a Set-Formation request. Behavior-verified at Sweden
(SEMANTIC_MAPPING.md sec 7.3 Run 2). It collapses intermediate waypoints to the destination -
so it TRADES route-fidelity for formation-fidelity; not a drop-in for multi-leg routes.

---

## 3. Task sequencing: external sequencer vs native plans

### 3a. How the port chains tasks TODAY (external sequencer)

- C2SIM expresses chaining as `ActionTemporalRelationship` / `ActionTemporalAssociationCode`.
  In COA-STP1 that code is `STREND` on 31 of 42 tasks (verified count) - "start after
  predecessor ENDS". Parsed to `OrderTask.StartAfterTaskUuid` (OrderParser.cs:104-119,
  OrderModels.cs:26); a non-STREND or multiple-relationship case is collected as a warning
  (OrderParser.cs:76-83).
- The `TaskSequencer` (TaskSequencer.cs) gates each task at a start gate: it waits for the
  predecessor to COMPLETE, WITH A TIMEOUT (the fix for the C++ infinite busy-wait), in two
  phases - predecessor must DISPATCH within the window, then gets a fresh window to COMPLETE
  (P0.2, TaskSequencer.cs:86-135). Completion is signalled by `CompleteTask` off
  `OnVrfTaskCompleted` (i.e. off VR-Forces' own completion notice).
- So today's chaining is EXTERNAL and keyed on VRF's reported completion (+ timeouts +
  abandon fast-fail). It is strictly sequential per unit.

### 3b. How VR-Forces chains NATIVELY (plans)

- The native primitive is the PLAN, not a DAG (GT 0.2 sec 4). An INDIVIDUAL plan is strictly
  sequential: "A plan always waits for its current task to complete before it starts the next
  task" (GT 0.2 sec 4, verbatim) - this is native "task follows task."
- GOTCHA: a GLOBAL plan sends a same-object series "without regard to completion of the
  previous task... only the last command sent is likely to be completed" (GT 0.2 sec 4,
  verbatim) - a prime suspect for "chain collapses to the final leg." The port must NOT chain
  via a global plan.
- CRITICAL: the native condition vocabulary has NO "task complete" predicate (GT 0.2 sec 4).
  The ONLY native movement-completion sequencing is the implicit "individual plan waits for
  the current task to complete" - i.e. it rides the SAME leading-edge completion that lies
  for units (GT 0.2 sec 3b). Remotely, plans are settable via `assignPlanByName` /
  `DtPlanBuilder` (GT 0.3 sec 4), monitored via `addPlanStatementCallback` /
  `DtPlanStatus.isComplete` (GT 0.3 sec 5 channel 3) - but that isComplete is a
  control-channel notification, not a position observation (GT 0.3 sec 5 closing note).
- The multi-unit native construct is the Synchronization Matrix (phase-gated, "intended for
  a linear series of actions"), where a Phase End Condition would host an "advance when all
  arrived" gate (GT 0.2 sec 4).

### 3c. Interruption semantics (relevant to any chain that retasks mid-move)

- Movement tasks are MUTUALLY EXCLUSIVE: "if the new task is mutually exclusive with the
  current task, the simulation object immediately stops the current task and begins the new
  one"; "If a simulation object has a plan, when you give it an independent task, it abandons
  the rest of the plan" (GT 0.2 sec 3c, verbatim). So a next-leg movement issued before the
  current leg finishes SILENTLY ABANDONS the current leg. The port already hit this: the
  pre-P0.2 sequencer's simultaneous timeouts burst-retasked units mid-route
  (TaskSequencer.cs class doc; SEMANTIC_MAPPING.md sec 4). A truthful chain must not issue
  the next movement until the current one is genuinely done (arrived), or it corrupts both.
- From the remote side a new `sendTaskMsg` supersedes the current top-level task; `skipTask`
  / clear-task requests exist (GT 0.3 sec 4). The port models supersession explicitly
  (MarkDispatched / in-flight record, VrfC2SimService.cs:932+).

### 3d. RECOMMENDATION

KEEP the external sequencer, but re-key its advance from VRF's completion notice to a
DISPLACEMENT-verified arrival gate (Phase 4). Do NOT adopt native individual plans for
chaining. Two sentences of rationale: native plans advance on the SAME unit leading-edge
completion that the live record proves fires both early and never (GT 0.2 sec 3b; GT 0.0
item 5; SEMANTIC_MAPPING.md sec 7.3), and the native condition set has no position-based
"task complete" predicate to substitute (GT 0.2 sec 4) - so moving chaining into VRF would
bake in the exact untrustworthy signal we are trying to escape, while losing the external
timeout/abandon fast-fail the port already needs. The external sequencer is the only place we
can insert a position-truth gate; keep chaining there and make `CompleteTask` fire on
observed center-displacement arrival (GT 0.3 sec 5 arrival-verification recommendation), not
on `OnVrfTaskCompleted`.

(The Synchronization Matrix Phase End Condition remains the ONE native construct worth
revisiting later for multi-unit COA phase gating - but only once the arrival gate exists to
define "phase complete", and it is out of scope for the single-unit STREND chains COA-STP1
actually uses.)

---

## 4. Adversarial review notes

- VERB TODAY-MAPPINGS VERIFIED IN CODE, not assumed: each row cites VerbMapping.cs (the
  classify table) AND the ExecuteTaskOnTick dispatch branch AND the VrfFacade.cpp task method
  it calls. The non-obvious findings that survived checking: (a) the port is NOT pure
  collapse - Attack/Breach/Reconnoiter/Escort have real distinct Layer-2 tasks wired; (b) the
  HoldObjective + Clear groups DO still collapse to bare move (IsImplemented==false,
  VerbMapping.cs:74-77) - that is where "replace collapse-to-move" bites; (c) single-point
  MOVE already uses MoveToLocation, not MoveAlongRoute, so "collapse-to-moveAlongRoute" is
  loosely stated - it is collapse-to-bare-MOVEMENT.
- ACTOR-LEVEL MISMATCHES FLAGGED (the review's main catch): the richest proposed native tasks
  (Unit Offensive/Defense/Seize behaviors, Move Into Formation) are UNIT-echelon and/or
  require DISAGGREGATION for combat (GT 0.2 sec 2). They do NOT exist for (i) an AGGREGATED
  unit, or (ii) a lone-entity taskee (the ~15 COA-STP1 single-M1A2 units, GT 0.0 item 3).
  Conversely FireAtTarget/Breach/FollowEntity are ENTITY-level tasks the port sends even when
  the taskee is a unit - a probable no-op against an aggregated unit. Every group note calls
  this out explicitly.
- 3c INTERRUPTION checked for preempt-capable verbs: movement mutual-exclusivity means any
  mid-route retask (STREND chain firing early, or a fresh order) silently abandons the
  current leg (GT 0.2 sec 3c) - folded into Section 3c and the Phase-4 gate.
- FALSIFICATION of the sequencing recommendation: the competing hypothesis is "adopt native
  individual plans (task-follows-task) and drop the external sequencer." The single
  observation that would favor it is a trustworthy native completion signal; the live record
  falsifies that (completions fire early AND never - SEMANTIC_MAPPING.md sec 7.3; GT 0.0
  item 5), and the native condition set has no position-based completion predicate (GT 0.2
  sec 4). So external-sequencing-on-native-completions is falsified in BOTH directions -
  hence "keep the sequencer, re-key it to displacement."
- Residual UNVERIFIED (assumed, not checked here - flagged honestly): the exact on-disk
  filenames of the Unit Offensive/Defense/Seize task .htm pages under [Tasks\UnitBehaviors\]
  are cited via GT 0.2 sec 3a's summary, not re-opened this pass; GT 0.2 sec 3a explicitly
  lists them but marks the entity-level chapter as its exhaustive scope, so the unit-behavior
  parameter details are a Phase-3 read-back item, not settled here. The "golden order" file
  identity is underdetermined (Section 1a) - reported for both candidates.

## 5. Open questions for the supervisor / user

1. AGGREGATED vs DISAGGREGATED policy: do Phase-3 units run DISAGGREGATED (so combat/defense
   verbs can map to real unit behaviors) or aggregated (movement-only, every combat verb
   reduces to move)? This decides whether Section 2's UNIT-column proposals are even
   reachable. (Military-semantics call - user adjudicates.)
2. coa-gpt DATA: ATTACK/BREACH/ESCRT self-target in COA-STP1 (Affected==Performing), so the
   distinct-target native tasks can never fire from this data. Fix coa-gpt to emit distinct
   AffectedEntity, or accept that COA-STP1 exercises movement only.
3. Unit-behavior parameter read-back: Phase 3 needs the actual [Tasks\UnitBehaviors\] task
   classes + params (Seize/Clear Objective, Defend, Movement To Contact) from the headers/
   docs before wiring them - not covered exhaustively by GT 0.2 (entity-scope).
4. Which order is canonically "golden" for scoring (Section 1a) - so Phase 5 scores the right
   file.
