# Data-quality feedback for the coa-gpt team (from VR-Forces execution)

From: the C2SIM VR-Forces interface team
Re:   four evidence-backed, data-quality items observed while executing coa-gpt-generated
      C2SIM initializations and orders in VR-Forces 5.0.2

## Framing (read this first)

Please keep coa-gpt emitting RICH semantics. The four items below are DATA-quality fixes,
not a request to reduce what you generate. Our interface deliberately maps each C2SIM verb
to the richest VR-Forces task it can (fires, breach, escort, formation moves, timing), and
we are extending that mapping upward, not collapsing it. The guidance we hold internally is
explicit: keep emitting TaskActionCodes, temporal associations, durations, formations, and
explicit targets - do NOT dumb the output down to the bare interface; we extend the
interface up to meet it (PORT.md sec 10). So nothing here asks you to drop richness. Each
item asks for a small correction to the VALUES in the data so the rich task we already build
has something valid to act on.

## Context for readers new to this interface

This interface is a C2SIM -> VR-Forces bridge (a .NET port of the original C++ interface). It
consumes a coa-gpt C2SIM initialization (units, positions, forces) plus a C2SIM order (tasks
with a performing entity, an affected entity, action verb, route points, and timing) and
drives MAK VR-Forces 5.0.2 to execute them. For each task it resolves the C2SIM entity UUIDs
to VR-Forces entities and issues the matching VR-Forces task. A few facts about VR-Forces
that make the items below bite:

- A task's "affected entity" is what the verb acts ON. VR-Forces needs a REAL, distinct
  target to fire at, breach, or escort. If the affected entity equals the performing entity
  (a unit targeting itself), there is no target and the verb has nothing to do.
- Most coa-gpt units in the scenario we tested are DISAGGREGATED aggregates (a unit shown as
  its individual member vehicles). VR-Forces moves such a unit by planning a LEADER path and
  having members follow in formation. That planning depends on the terrain/road content
  streamed at the unit's map location, and on the members not being jammed on top of one
  another at spawn.
- All claims below were verified against our internal engineering docs and confirmed on live
  VR-Forces runs. Doc + section + run date (and, where relevant, the raw evidence file under
  docs/experiments/) are cited per item so you can trace any finding.

The test scenario referenced throughout is "COA-STP1" (a 42-task coa-gpt order over a
128-unit init at a Mojave, California map location). "Golden" refers to our reference
scenario (a Sweden map location) that the interface reproduces faithfully and that executes
correctly - it is the known-good control we compare against.

---

## Item 1 - Emit a distinct affected entity for engagement verbs

FINDING. In the COA-STP1 order, every task sets its affected entity equal to its performing
entity - the unit is tasked to act on itself. This holds for all 42 tasks, including all 19
tasks in the ATTACK family (ATTACK, DESTRY, FIX, DISRPT, PENTRT) and the BREACH and ESCRT
tasks.

EVIDENCE. SEMANTIC_MAPPING.md sec 5 (Unit 3) records the decisive live finding: on a fresh
VR-Forces federation (live run 2026-07-11, appNo 3261) every COA-STP1 ATTACK-family task had
performing entity == affected entity (all 19 self-target; e.g. d6df3c3d "attacks" d6df3c3d).
SEMANTIC_MAPPING.md sec 5 (Unit 2), corrected 2026-07-12, broadens this to ALL 42 COA-STP1
tasks having affected entity == performing entity, not only the ATTACK-family ones. The
general consequence - a bare-movement projector in which ATTACK/BREACH/SECURE all reduce to
move when there is no distinct target - is described in PORT.md sec 10.

IMPACT ON VR-FORCES EXECUTION. Our interface builds the correct rich task (a
DtFireAtTargetTask for the ATTACK family, a DtBreachTask for BREACH, a DtFollowEntityTask for
ESCRT) and then must resolve the affected entity to aim it. A self-target resolves to the
unit itself, which is a no-op (you cannot fire at, breach, or escort yourself), so the
interface's self-target guard correctly skips the engagement and the task degrades to a plain
advance along its route. Net effect: for this data, ATTACK == advance, BREACH cannot ever
dispatch its breach task, and ESCRT cannot ever dispatch its follow task - at any run length.
The engagement semantics you generated are lost not because the interface drops them, but
because there is no distinct entity to apply them to.

ASK. Emit a real, distinct affected entity per engagement verb:
- ATTACK / DESTRY / FIX / DISRPT / PENTRT: a valid OPFOR (enemy) unit UUID as the target,
  force-tagged so VR-Forces rules of engagement permit the engagement.
- BREACH: a distinct obstacle-like affected entity (the thing to breach), not the breaching
  unit.
- ESCRT: the escorted unit as the affected entity, distinct from the escorting unit.
A synthetic 1-task order we built with a distinct OPFOR target (taskee friendly, affected a
different OPFOR unit, ROE permitting) drove the full fire path end to end in VR-Forces
(SEMANTIC_MAPPING.md sec 5, Unit 3, 2026-07-11) - so the interface is ready; it only needs a
distinct target in the data.

---

## Item 2 - Timing hygiene (start delays and a watchable time scale)

FINDING. The COA-STP1 order carries at least one task (T13) with a very large start delay -
12,000,000 ms of SimulationTime, i.e. about 3 hours 20 minutes - before the task begins. Task
durations across the order range from about 1 hour 20 minutes to 3 hours 20 minutes.

EVIDENCE. PORT.md sec 5 records, from the 2026-07-09 COA-STP1 examination: "durations
(1h20m-3h20m) are IGNORED ... only task T13 has a real 12,000,000 ms (3h20m) SimulationTime
start delay." PORT.md sec 10 lists the same 12,000,000 ms T13 delay explicitly as a coa-gpt
timing-hygiene item (order-data, not an interface artifact).

IMPACT ON VR-FORCES EXECUTION. A start delay of ~3h20m means that task does not begin until
the simulation clock reaches that mark. At an ordinary real-time rate the scenario sits idle
for hours before that task fires, which makes it effectively unwatchable in a live review.
(For accuracy: task DURATIONS are currently IGNORED by the interface - our move task passes no
speed - so the long durations do not themselves stretch execution today. We flag them because
they signal the same implausible time scale as the start delay, and a future timing-aware task
path would honor them.)

ASK. Apply timing hygiene: use sane, plausibly small start delays, and set a
SimulationRealtimeMultiple appropriate to the scenario so it is watchable rather than idling
for hours (PORT.md sec 10). Keep the temporal associations and durations you already emit -
those are rich semantics we want; it is the magnitudes that need to be realistic.

---

## Item 3 - Disperse unit positions (good hygiene; NOT the movement fix)

FINDING. The COA-STP1 init co-locates a large number of units at a single identical
coordinate. One "mega-pile" places 54 units - over half the scenario's creatable units, both
aggregates and entities - at exactly 34.679985, -116.724799.

EVIDENCE. UNIT_MOVEMENT_RESEARCH.md sec 4 (R8 offline finding, 2026-07-12) identifies the
one 54-unit mega-pile at 34.679985, -116.724799. The de-stacking spread was then confirmed on
a live run (UNIT_MOVEMENT_RESEARCH.md sec 4b, the "R8 verify" run 2026-07-12/13, appNo 3332;
the backend logged "54 units at (34.679985, -116.724799) spread onto 50 m rings"; raw
evidence docs/experiments/R8_verify_run_2026-07-13.txt). "R8" is our internal name for the
interface-side create-time DE-STACKING mitigation - an opt-in feature that spreads units
sharing an identical spawn coordinate onto deterministic rings before they are created.

IMPORTANT NUANCE - this is hygiene, not the cure. Stacked coordinates are NOT, by themselves,
what blocks movement. Our known-good golden init is ALSO stacked (10 groups, up to 13 units
in one pile), yet those units marched correctly. What distinguishes COA-STP1 is pile SIZE: one
54-unit mega-pile versus the golden maximum of 13 (UNIT_MOVEMENT_RESEARCH.md sec 4). We then
ran a controlled A/B - the same scenario with only de-stacking toggled - and it FALSIFIED
stacking as the sufficient blocker: de-stacking made entity operations about 4x faster and
cleaned up unit creation, but the aggregates still did not march (UNIT_MOVEMENT_RESEARCH.md
sec 4b, verdict). The dominant cause turned out to be geography, which is Item 4. So please
read this item as: dispersing positions removes real gridlock and creation problems and is
worth doing, but it will not by itself make disaggregated units maneuver.

IMPACT ON VR-FORCES EXECUTION. Dozens of vehicles spawned on the same point must physically
un-jam before anyone can move; a member entity needed about 13 minutes just to escape the
54-unit pile in one run, versus roughly 3.5 minutes once de-stacked. Members of a disaggregated
unit that must first form up inside such a pile are impeded. De-stacking removes this specific
gridlock, but the underlying aggregate-maneuver block (Item 4) remains.

ASK. Disperse unit positions in the init - do not co-locate dozens of units at one identical
coordinate. Give units realistic, spread-out spawn positions (UNIT_MOVEMENT_RESEARCH.md sec 4,
the R6 feedback item: dispersed positions are the preferred, source-side fix). Treat this as
good hygiene that removes a gridlock failure mode, and pair it with Item 4, which is the
governing constraint.

---

## Item 4 - Validate the scenario region before generating COAs there (strongest item)

FINDING. Whether disaggregated units can maneuver as units at all depends on the map REGION
you place them in. At the COA-STP1 Mojave location, VR-Forces cannot plan unit movement paths;
at our golden Sweden location the identical units and code plan and march fine. This is the
governing blocker behind the COA-STP1 movement failures.

EVIDENCE. This was isolated by a region-swap experiment we call "R9" (UNIT_MOVEMENT_RESEARCH.md
sec 4c, live 2026-07-13; raw evidence docs/experiments/R9_region_swap_2026-07-13.txt). We took
the exact golden unit set that marched correctly, transplanted it by a geometry-preserving
coordinate transform from Sweden (~58.69, 16.5) to the COA-STP1 Mojave area (~34.6, -116.6),
and ran the same one-move-per-unit probe:
- Mojave run (app 3336 / watch 3337): 1 of 3 tasks completed - only the single entity control
  drove its route; the two aggregates moved a few meters or the wrong way and froze.
- Sweden control run (app 3339 / watch 3340, same code and settings): 3 of 3 completed, both
  aggregates physically marched (0.7-1.1 km telemetry) and reported completion.

MECHANISM (decisive, from the VR-Forces backend log). In the Mojave window the backend logged,
three times per aggregate, "<unit>: moveAlong() - empty route -- not sending move along to
subordinate" and created ZERO member "Offset Route" objects; the Sweden control window created
45. In other words, at Mojave the lead-follow controller's LEADER path plan comes back EMPTY,
so nothing is ever forwarded to the following members and the unit never marches. Individual
ENTITY moves complete at both regions because entity movement does not go through unit
leader-path planning. VR-Forces runs the same whole-earth terrain system for both locations,
so the discriminator is the streamed ground CONTENT (elevation/roads/pathfinding) at each
location, not a different terrain file (UNIT_MOVEMENT_RESEARCH.md sec 4b/4c).

INTERFACE-SIDE MITIGATION AND ITS COST. We have a working mitigation we call "R10" -
SUBORDINATE FAN-OUT: when a unit's leader-path plan is empty, the interface tasks the unit's
member entities directly (entity moves ARE proven at Mojave). This unlocked aggregate tasking
at Mojave - the same units that scored 1/3 with unit-level tasking scored 3/3 fanned out, and
a later COA-STP1 run reached 5 of 7 unit completions where the prior best was 0
(UNIT_MOVEMENT_RESEARCH.md sec 4c, live 2026-07-13, apps 3342/3345/3350). BUT the fan-out
forfeits formation keeping: fanned members move as INDEPENDENT entities rather than as a unit
in formation. So the interface can get vehicles to their destinations at a path-dead region,
but it cannot make them maneuver AS a formed unit there. The region is still the real
constraint.

IMPACT ON VR-FORCES EXECUTION. If a COA is generated for a region whose streamed content does
not support unit path planning, disaggregated units cannot maneuver as units there regardless
of what the interface does - the best achievable is independent member movement without
formation keeping. Region choice, made at COA-generation time, determines whether the maneuver
you specified is executable at all.

ASK. Validate a candidate region BEFORE generating COAs for it: run a 1-unit move probe at the
intended location and confirm the unit actually plans a path and marches (the empty-leader-path
symptom is the failure signature). Where possible, prefer regions with known-good ground
content - our golden Sweden site works. This is the single highest-leverage change on this
list: it is decided up front, at region selection, and it governs whether the other three items
even get a chance to matter (UNIT_MOVEMENT_RESEARCH.md sec 4c, feedback item #4).

---

## Item 5 - Right-size the entity count, especially OPFOR density (do NOT thin the threat)

Measured 2026-07-14: COA-STP1 emits **128 units** across two ForceSides (NATO Coalition 61,
WASA/OPFOR 67, + neutral), but the order tasks only **11 distinct units (~9%)**. Fully
disaggregated that is **~1785 live VR-Forces objects, ~93% of them idle**, plus a 54-unit
mega-pile stacked at one coordinate.

Why it matters: VR-Forces' own docs name "many simulation objects over a wide area OR many
fast-moving objects" as exactly what DEFEATS predictive terrain paging (empty leader-path ->
frozen aggregates) and it heavily loads the sim/HLA at high time multiples. So entity BLOAT
compounds the maneuver problems (Items 3-4).

What the interface already does (context, not an ask): it now LEAN-CREATES - it spawns only
order-referenced friendly units, and PRESERVES the full threat (OPFOR/neutral is kept, because
it engages autonomously and is part of the COP). Idle *friendly* context is dropped
automatically; the threat is not touched.

The ask (OPFOR density only): keep emitting the FULL friendly ORBAT (the plan needs it), but for
OPFOR emit what is **tactically relevant to the COA** - the threat the force will actually face
along its axis and at its objectives - rather than the entire enemy database fully disaggregated.
Prefer aggregate-level for deep/rear enemy formations that will never engage this COA. This keeps
the threat and the COP intact (do NOT thin the threat the force fights) while shedding deep dead
weight that only slows the sim and starves terrain paging. Evidence: this doc +
docs/SCENARIO_SETUP_GUIDE.md (bloat/lean-creation), measured 2026-07-14.

---

## Summary

| # | Item | Ask | Primary evidence |
|---|------|-----|------------------|
| 1 | Distinct affected entity | Emit a real, distinct target per engagement verb (OPFOR for ATTACK-family, obstacle for BREACH, escorted unit for ESCRT) | SEMANTIC_MAPPING.md sec 5 (Units 2-3); live 2026-07-11 |
| 2 | Timing hygiene | Sane start delays; set a watchable SimulationRealtimeMultiple | PORT.md sec 5 and sec 10 |
| 3 | Disperse positions | Do not co-locate dozens of units at one coordinate (hygiene, not the movement cure) | UNIT_MOVEMENT_RESEARCH.md sec 4 / 4b; live 2026-07-12/13 |
| 4 | Region validation | Probe a region with a 1-unit move before generating COAs there, or pick known-good regions | UNIT_MOVEMENT_RESEARCH.md sec 4c; live 2026-07-13 |
| 5 | Right-size OPFOR density | Full friendly ORBAT; emit tactically-relevant OPFOR (aggregate-level for deep/rear), not the whole enemy DB disaggregated - do NOT thin the threat | SCENARIO_SETUP_GUIDE.md; measured 2026-07-14 |

Items 1-3 are independent, low-cost data corrections. Item 4 is the governing constraint and
is decided at COA-generation time. None of these ask you to reduce the richness of what
coa-gpt emits - they ask for realistic values so the rich tasks the interface already builds
have valid data to execute.
