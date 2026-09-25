# TASK COMPLETION RESEARCH 2026-09-21 - one page per verb (RESEARCH ONLY, no recommendation)
# What this is: a per-verb table of the doctrinal end state; whether VR-Forces or this interface can
# ascertain that a desired effect was achieved; what the code on main does today; and how the
# TEMPORARY position of 2026-09-21 (docs/RULINGS.md RL-20260921-09) applies to each verb.
# It RECOMMENDS NOTHING and settles nothing. Produced 2026-09-21 by a read-only executor; nothing was
# run (n/a live); the evidence is the citations on each line. Cited from RL-20260921-08 and -09.

# WHEN IS A TASK FINISHED? ONE PAGE PER VERB

Assembled 2026-09-21 by lane V (read-only). Sources: this repo's own doctrine pass and vendor-task
analysis, the installed VR-Forces 5.2d documentation, headers and shipped scripts, the two orders on
disk, and the code on main (ac1ec58). Nothing was run. NOTHING HERE IS A RECOMMENDATION.

HOW TO READ IT
- [V] = I opened the source named on the line. [A] = carried from a repo record I did not re-verify.
- "RECORD SILENT" = neither doctrine record, vendor documentation nor code answers that cell, and the
  line says what I searched.
- Internal codes are spelled out and the code is put in parentheses, e.g. "arrival evidence (C15)".
- Each verb has eight fields. THE WEIGHT IS ON FIELDS 3 AND 4 - what doctrine says the end state is, and
  whether VR-Forces or this interface can actually tell that it was reached. Those two are the research
  the owner asked for. Field 8 records how the temporary position of 2026-09-21 applies to the verb, and
  it is short on purpose: the position covers every case (see W-TEMP2 below), so there is nothing to
  qualify.

---

## 0. THE OWNER'S OWN WORDS, VERBATIM - the frame for everything below

Quoted, never paraphrased. Typos are his. Ledger ids: docs/RULINGS.md and docs/RULINGS_ARCHIVE.md, unless marked new.

W-TEMP (2026-09-21, this session; the seat's new id RL-20260921-09) - THE TEMPORARY POSITION:
  "And the doctrinal and vendor doc research is on you buddy - not me. For now, pending this research,
   let's take the temporary position that completion is based on  start time + duration. Tasks
   involving units may still arrive late. If they arrive after the expected start time + duration,
   they complete immediatelly. Follow-on tasks still are permitted to take their whole specified
   duration, even if they were forced to start late. Mark this is as temporary, as a measure to let us
   move forward on this until we have better data."

W-TEMP2 (2026-09-21, same session, correcting a seat reading that called the position "silent" on three
things) - THE POSITION COVERS ALL THREE:
  ""a unit that never arrives" - isn't there ruling already for units that get stuck? That's the only way
   a unit can "never arrive". "patrol and follow" - yes, there's plenty of tasks that involve no movement,
   as discussed repeatedly. And there is ruling for thoise as well -  they are completed when their
   duration elapses. "an effect that has not been achieved by end time" - it is uncertain whether there
   is a way to ascertain the effect - that is the thrust of the research. For this temporary ruling, we
   ignore that part and complete based on time."

W-FUZZY (2026-09-21, this session, typed answer; transcript c3b364bd line 304, 18:33:20Z) - THE QUESTION:
  "The notion of a hold-off or movement tasks seems to be rather fuzzy as described. Some verbs imply
   that the unit stays in place (e.g. to DEFEND). There are plenty of tasks where there is movement
   preceding the desired effect, like in "ATTACK TO SECURE". How are these distinguished? Seems to me
   that the time applies to the full task, including the movement and the achievement of the desired
   effect, at which point the unit might be tasked to perform a "follow on" task (not a "follower").
   SPeaking of which what do you mean by "late delays followers" - I meant to say "follow ons" - the
   tasks for the same unti that may be set to start after it has completed a preceding task. And I hear
   you about the "eralier" - this assumed that all a unit had to do was complete the movement, but your
   point is valid, that theres action required after that - requires a doctrinal research, and as well
   as of vendor documentation - is the sim able to determine when some of the desired effect has been
   achieved, for example DESTROY (no enemy unit operational in the target area or something like that?)."

W-857 (2026-09-21, this session, same record):
  "As for 857, a unit that is meanto to move to an objective and perform some action, but instead gets
   stuck in the middle of the way certainly did not complete, so not sure why you say the "premisse was
   false".  My caveat was that you should not expect every task to require a movement, and abort in
   case the unit stays put."

W-TIME (2026-09-21, earlier session a7f6a276 line 45457; ledger RL-20260921-05):
  "The rulling based on time is for tasks that do not include movement, such as defend in place and
   similar ones - units that are not expected to reach any other location. That's what you asked. "A
   unit that moved 60 m of a 5 km leg gets a complete" is a new interpretation. If I agreed with that,
   I was tricked. [...] The notion that geting stuck midway is a complete is completelly illogical.
   There is also a ruled nuance - some tasks never report completed, and yet they make it to their
   destination - there is code (or should be as a result of the ruling) to adopt complete as the
   outcome even if the sim misses the notificaiton. Arriving late does _not_ imply an abortion - what
   happens is that follow on tasks are delayed by the slow progress on a leg."

W-ARRIVAL (2026-09-07; ledger RL-20260907-01):
  "report a unit's completion from the unit's own arrival evidence is fine"

W-ENDTIME (2026-09-14, answering a question table whose row 4 was "SECURE, OCCUPY and DEFEND have no
natural completion in VR-Forces. Decide whether they complete on arrival, after a duration, or only
when a later order supersedes them"; ledger RL-20260914-02):
  "4 given by the end time"

W-STALL (2026-09-14; ledger RL-20260914-01, answering "whether TASKABRT is the code STP should see for
a stalled unit"):  "Ok on 2-3"

W-OPEN (2026-09-21, earlier session line 45418; ledger RL-20260921-04) - HIS QUESTION, UNANSWERED:
  "This sounds odd. Tasks should include the expected final location if they are indeed moves,
   shouldn't they? Is this creating a special class of errors for no movement from the get go as
   opposed to movement that falls short of the objective?"

WHAT THE TEMPORARY POSITION NOW COVERS that lane B's ledger had listed as unruled: patrol, follow and
escort, and any task with no destination - W-TEMP2 puts all of them on "completed when their duration
elapses"; and a unit that never arrives - W-TEMP2 points at the stall ruling (W-STALL).

STILL NOT on file for any verb (searched the whole ledger and its archive): any bound on how long a
follow-on waits for a late mover, and whether the progress watchdog (C16) ships switched on.

---

## 1. THE THREE FACTS THAT DECIDE MOST CELLS - read these once

VENDOR-1. VR-FORCES 5.2 HAS NO TACTICAL TASK CLASS AT ALL. [V] I enumerated the 277 headers in
C:\MAK\vrforces5.2d\include\vrftasks: 81 distinct task classes, and NOT ONE of them is named for
attack, defend, seize, occupy, secure, screen, guard, block, retain or clear. The single name that
matches is DtClearTask, and clearTask.h:33 declares it `public DtSetDataRequest` - it is "clear the
current task", a cancel, not a tactical clear. Every tactical verb in 5.2 is a Lua script delivered as
a scripted task. (The repo said this at TASK_VOCABULARY_ASSESSMENT_2026-09-14.md:416-420; confirmed.)

VENDOR-2. THE VENDOR'S SHIPPED TACTICAL SCRIPTS END ON MOVEMENT AND POSTURE, NOT ON AN EFFECT. [V]
 - company_seize (the Seize Objective behaviour): its last two behaviour-tree nodes are a "clear phase"
   subtask and a "seize phase" that issues move-into-formation at the objective area's centre
   (company_seize.lua, last 25 lines). It contains ZERO `vrf:endTask` calls.
 - co_clear (the Clear behaviour): its whole top node is `{calcInitPositions, moveToInit}`
   (co_clear.lua, last 15 lines). It contains ZERO `vrf:endTask` calls and ZERO enemy tests - I grepped
   it for isDestroyed, getHostileForces, countDestroyed and the contacts module: no hits.
 - unit-attack-to-objective (the aggregate-level Attack To Objective): its own header says the whole
   behaviour is "1. If not in an attack posture, change to Hasty-Attack as the minimum. 2. Execute a
   move-along." (unit-attack-to-objective.lua:2-4).
 THE ONE EXCEPTION, and it matters for the owner's DESTROY question: plt_attack_by_fire's own header
 says "When there are no more enemy contacts detected, the tanks move to their forward positions and
 the behavior ends", with parameters `timeout` ("After this many seconds without detecting an enemy,
 give up") and `doSupportForever` (plt_attack_by_fire.lua:1-21). That is the closest any shipped task
 comes to an effect test, and its test is NO ENEMY STILL DETECTED BY THIS PLATOON'S SENSORS - not "no
 operational enemy in the area". A platoon that loses contact because the enemy withdrew behind a ridge
 satisfies it.

VENDOR-3. THE SIM CAN BE ASKED EFFECT QUESTIONS - BUT THROUGH THE PLAN CHANNEL, NOT THE TASK CHANNEL.
 [V] VR-Forces has a condition vocabulary, documented at
 C:\MAK\vrforces5.2d\doc\help\Content\Plans\PlansConcepts\ConditionalTests.htm, and every condition is
 a C++ class under C:\MAK\vrforces5.2d\include\vrfutil (condExprTypes.h numbers them). The ones that
 bear on a desired effect:
 - ENTITY DESTROYED (DtCeEntDestroyed, ceEntDestroyed.h:22). Help, verbatim: "Entity Destroyed returns
   true if the specified simulation object is destroyed. The definition of destruction for a unit is
   implementation specific. For the entity-level models provided with VR-Forces, units are considered
   to be destroyed when all members of the unit are destroyed." The class takes a UUID
   (`setEntity`) and a modifier where "ANY" means any subordinate and anything else means all of them
   (ceEntDestroyed.h:49-62).
 - ENTITY IN AREA (DtCeEntInArea, ceEntInArea.h:22) - takes an entity UUID and an AREA UUID plus the
   same any/all modifier (:51-68). The help adds an "Exclude Destroyed Simulation Objects" option when
   the trigger is re-registered.
 - ENGINEERING OBJECT BREACHED (DtCeBreached, ceBreached.h:23). The help says "(Aggregate-level
   scenarios only.)"
 - plus ENTITY UNDER FIRE, ENTITY HAS TARGET, ENTITY IN RANGE OF TARGET, ENTITY LEFT OF PHASE LINE,
   DETECT ENTITY, SIMULATION TIME, and AND / OR / NOT.
 These are PLAN constructs. A trigger "consists of a condition and a block of statements that are
 executed when the condition becomes true" and "a trigger is always local to its plan"
 (ConditionalTests.htm's sibling, Triggers.htm). They are buildable from an outside controller:
 DtPlanBuilder::addTriggerStatement(const DtSimCondExpr&, DtUUID& triggerUUID, ...) at
 vrfplan/planBuilder.h:52-56, and the vendor's own sample assigns a built plan with assignPlanByName
 [A - assessment :1849-1889, sample not re-read this pass]. THIS REPO USES NONE OF IT: I grepped the
 facade, the bridge and the app for DtPlan / PlanBuilder / assignPlan / Trigger / CondExpr - the only
 hits are DtPlanAndMoveToTask, which is a movement task. [V]

INTERFACE-1. WHAT THE INTERFACE CAN READ ABOUT AN ENEMY TODAY: POSITIONS, AND NOTHING ELSE. [V]
 - Available on the wire and one accessor away: every reflected entity's state repository carries
   damageState() (C:\MAK\vrlink5.10\include\vl\entityStateRepository.h:124) and forceId() (:67), and
   the appearance bit field (:99). Inside the sim, Lua offers getDamageState ("none slight moderate
   destroyed"), isDestroyed, countDestroyedObjects and getHostileForcesWithinSearchRadius(location,
   radius, friendlyForceType) (doc\luadoc\vrfLuaApi.lua:1464, :1799, :357, :379), and point-in-area
   tests isPointInside / isInsidePolygon (:1842, :1006).
 - NOT read by this repo: I grepped src/VrfFacade, src/VrfBridge and src/VrfC2SimApp for damage,
   damageState, appearance and isDestroyed. ZERO hits. The facade's reflected-object structs
   (VrfFacade.h:171-303) carry counts, creations, text reports, task completions, formations, aggregate
   members, console messages and terrain samples - no health, no affiliation.
 - The facade CAN read one position at a time by UUID (TryGetEntityGeodetic, VrfFacade.h:714) and CAN
   enumerate every reflected UUID, but only under BeginTrackingReflectedObjects, which its own header
   marks "Intended for the ResetVrf tool ... Not for the app's normal path" (VrfFacade.h:540-554).
 SO: "is anyone inside this area" is one loop away from data the interface already has. "Is the enemy
 in it destroyed" is not - it needs a new facade read of damageState and forceId.

---

## 2. SUMMARY - one line per verb

Counts: C = COA-STP1_Order.xml (42 tasks), I = IRONSTORM_CUTA_Order.xml (5 tasks). All counts [V],
measured this pass. The full Iron Storm export (STP-IRON-STORM-SYNTHETIC_Order.xml, 23 tasks) is given
in field 1 of each verb where it differs, because cut A is a five-task slice of it.

| verb (code)      | C  | I | movement first? | can the sim tell the effect?          | code today            |
|------------------|----|---|-----------------|---------------------------------------|-----------------------|
| ATTACK           | 10 | 2 | usually         | no shipped test; plan trigger could   | move, then fire       |
| PENTRT penetrate |  2 | 0 | always          | no shipped test                       | move, then fire       |
| SEIZE            |  1 | 0 | always          | position-in-area, not "cleared"       | bare move             |
| DESTRY destroy   |  2 | 0 | usually         | YES if a named enemy exists (none do) | move, then fire       |
| FIX              |  3 | 0 | either          | no; attack-by-fire has a timeout only | move, then fire       |
| DISRPT disrupt   |  2 | 0 | either          | no                                    | move, then fire       |
| BREACH           |  3 | 0 | usually         | aggregate-level condition only        | move, then breach     |
| CLRLND clear     |  1 | 0 | always          | no; vendor clear has no enemy test    | bare move             |
| SECURE           |  4 | 0 | either          | position-in-area only                 | bare move             |
| OCCUPY           |  3 | 0 | usually         | position-in-area only                 | bare move             |
| RETAIN           |  1 | 0 | no              | position-in-area only                 | bare move / in place  |
| BLOCK            |  2 | 0 | either          | no                                    | bare move             |
| DEFEND ground    |  2 | 0 | no              | no                                    | in place              |
| DEFEND air def.  | (2 of the 2 above) | 0 | no  | no; the aggregate task never ends     | in place              |
| GUARD            |  1 | 0 | no              | no                                    | bare move             |
| SCREEN           |  3 | 0 | either          | no; recon task ends on a DURATION     | patrol (never ends)   |
| SCOUT            |  0 | 0 | either          | same as SCREEN                        | patrol (never ends)   |
| MOVE             |  1 | 0 | always          | arrival - YES, two ways               | move                  |
| ESCRT escort     |  1 | 0 | always          | no; follow never ends                 | follow at zero offset |
| ExecutePlanPhase |  0 | 2 | no              | not applicable - a marker             | in place              |
| CRESRV reserve   |  0 | 1 | no              | no                                    | bare move             |

---

## 3. PER-VERB ENTRIES

Fields: 1 counts and shape / 2 movement first? / 3 doctrinal end state / 4 can VR-Forces tell? /
5 what the code does today / 6 what the owner has said / 7 the open question / 8 the temporary position.

---------------------------------------------------------------------------------------------------
### ATTACK - attack

1. COA-STP1 10 (the largest group); Iron Storm cut A 2; full Iron Storm 2. [V] Geometry as exported:
   4 points x4 (a linearised axis of advance), 1 point x3, 2 points x1, NO points x2. [V]
2. MOVEMENT FIRST, usually. What decides it in the export is simply whether the task carries Location
   elements: 8 of the 10 do, and the 4-point ones are an axis of advance, so they are a drive. The two
   with none (T8 fires support, T37 follow-on security) are in-place tasks by their own names. Iron
   Storm cut A is different in kind: its tasks carry MapGraphicID references (11 across 5 tasks), which
   COA-STP1 does not carry at all (0 across 42). [V]
3. DOCTRINE: an attack is not a tactical mission task at all but a type of operation, and it ends ON
   THE OBJECTIVE - "An attack is a type of offensive operation that defeats enemy forces, seizes
   terrain, or secures terrain", with the objective a minimum control measure of every attack
   (DOCTRINE_FOR_TASKING_RULINGS_2026-09-14.md:118 and :344, quoting FM 3-90 5-1, 5-8, 5-23). Doctrine
   therefore reduces ATTACK to SEIZE or SECURE on the named objective. [V for the record; A for the
   manual - FM 3-90 was not opened this pass]
4. CAN VR-FORCES TELL? Entity level: there is no attack task (VENDOR-1); the repo composes a move plus
   a fire-at-target. A move-along reports its own completion but for a unit it fires EARLY BY DESIGN -
   "The pseudo-aggregate considers the Move Along task complete for the pseudo-aggregate as a whole
   when its lead subordinate completes the task" (moveAlongTasks.h:35-37). [V] Aggregate level:
   unit-attack-to-objective is a posture change plus a move-along, so its completion is ARRIVAL, not
   the objective taken. [V] THE EFFECT ITSELF: (iv) not observable from any shipped task; (iii)
   observable by the interface only in the weak sense of "my unit is standing in the objective area",
   from positions it already reads - the area is already created in VR-Forces under the order's own
   identifier, so no new reader is needed for that half. Whether the objective is HELD or the enemy
   beaten needs the plan-trigger channel (VENDOR-3) and a named enemy, which this order does not carry
   (every task names the performing unit as its own affected entity, 42 of 42). [V]
5. CODE TODAY: classified Attack and wired (VerbMapping.cs:104-108, :87). The affected entity resolves
   to "the objective is the target, not an error" (TaskDispatchPolicy.cs:80-85, ForTarget ->
   SelfIsObjective), the unit drives the route, and the fire is parked until the move completes and
   then issued (VrfC2SimService.cs:4537, :5321-5329). The move's own completion reports "in progress",
   not "complete", so the one completion is left for the engage (TaskStatusPolicy.cs:114-115). The task
   also has a timer armed at dispatch for its Duration, and whichever lands first wins
   (TimedCompletionPolicy.cs:76-89, :135-136). [V]
6. OWNER: W-FUZZY names this verb family directly - "There are plenty of tasks where there is movement
   preceding the desired effect, like in "ATTACK TO SECURE". How are these distinguished?" and "the
   time applies to the full task, including the movement and the achievement of the desired effect".
   W-TIME: "The rulling based on time is for tasks that do not include movement". Nothing else on file.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): ends at start time plus Duration; if the unit is still
   travelling then, it completes on arrival; a unit that gets stuck is an abort under the stall
   ruling already on file; whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### PENTRT - penetrate

1. COA-STP1 2; Iron Storm 0. Both carry 4 points, both a linearised axis of advance. [V]
2. MOVEMENT FIRST, always - the geometry is a route in both cases. [V]
3. DOCTRINE: not a tactical mission task in the 2023 edition; a FORM OF MANEUVER, "a force attacks on a
   narrow front", ending on the objective beyond the enemy's forward defence, i.e. the same end state as
   ATTACK (doctrine record :124). [V record / A manual]
4. Same as ATTACK in every respect: no vendor task, no shipped effect test, arrival observable.
5. CODE TODAY: mapped to the same Attack intent (VerbMapping.cs:108). Identical behaviour to ATTACK. [V]
6. OWNER: nothing on file for this verb specifically. W-FUZZY covers it as a movement-then-effect task.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): ends at start time plus Duration; if the unit is still
   travelling then, it completes on arrival; a unit that gets stuck is an abort under the stall
   ruling already on file; whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### SEIZE

1. COA-STP1 1 (T32); full Iron Storm 1; cut A 0. Carries 4 points - an axis. [V]
2. MOVEMENT FIRST, always. [V]
3. DOCTRINE, AND THIS IS THE MOST EVALUABLE VERB IN THE SET: "Seize is a tactical mission task in which
   a unit takes possession of a designated area by using overwhelming force. An enemy force can no
   longer place direct fire on a seized objective", and "Once a friendly force seizes a physical
   objective, it clears the terrain within that objective by killing, capturing, or forcing the
   withdrawal of all enemy forces" (doctrine record :132 and :336, quoting FM 3-90 B-57). So doctrine's
   test has three parts: possession, no enemy direct fire onto it, and the terrain inside it cleared.
   [V record / A manual]
4. CAN VR-FORCES TELL? Partly, and the shipped task does NOT.
   - The vendor's Seize Objective behaviour (company_seize) ends by moving the company into formation
     at the objective area's centre (VENDOR-2). It tests possession by geometry, never by enemy state.
     Its first parameter is literally named enemyArea and it uses it only as a place
     (company_seize.lua:150-178, a point-in-area test on its own route points). [V]
   - Possession: (iii) the interface could evaluate it from data it already reads - member positions
     against the objective polygon, which the initialization already creates in VR-Forces under the
     order's own identifier. No reader exists for the polygon test today; the positions do.
   - "No enemy can place direct fire on it" and "terrain inside cleared": (iv) not observable without
     reading affiliation and health of reflected entities, which the facade does not expose
     (INTERFACE-1), or without the plan-trigger channel (VENDOR-3), which needs a named enemy the order
     does not supply.
   - The vendor's script also rejects our taskee types today: company_seize accepts two DIS types and
     the COA's proxies are platoons and headquarters sections [A - assessment :555-566; whether the
     filter is enforced over the wire is recorded there as a one-run question, still unrun].
5. CODE TODAY: classified HoldObjective, which is NOT WIRED (VerbMapping.cs:111, :92-94), so the task
   falls through to a bare drive along its four points, and the log says so ("Layer-2 not yet wired -
   executing bare movement", VrfC2SimService.cs:4315-4318). It can end on arrival evidence (C15) or on
   its Duration timer, whichever is first. [V]
6. OWNER: nothing on file naming SEIZE. W-ENDTIME answered a question that named SECURE, OCCUPY and
   DEFEND, not SEIZE.
7. OPEN QUESTION FOR THE OWNER: none. One item is undecidable from the documentation and is a run
   question, not a question for you: whether VR-Forces enforces a scripted task's own entity-type
   filter for a task arriving over the network is not stated anywhere in the help
   [A - assessment sec 6.3 item 1].
8. TEMPORARY POSITION (RL-20260921-09): ends at start time plus Duration; if the unit is still
   travelling then, it completes on arrival; a unit that gets stuck is an abort under the stall
   ruling already on file; whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### DESTRY - destroy (the one verb that carries a desired-effect code)

1. COA-STP1 2 (T5, T41), both carrying the desired-effect code DSTRYK and 1 point each; full Iron Storm
   2, also DSTRYK, 3 points each; cut A 0. Every other task in both orders carries the effect code
   TaskSuccess. [V, measured this pass]
2. MOVEMENT FIRST, usually - T5 has one point and its own name is a counter-fire mission, T41 has one
   point. A single point is ambiguous by construction: the repo measured that a single exported point
   lands on a CLUSTER of graphics, never on one (doctrine record :452-459). [V record]
3. DOCTRINE: "Destroy is a tactical mission task that physically renders an enemy force
   combat-ineffective until it is reconstituted", and the end state is that NAMED enemy force being
   combat-ineffective - "The amount of damage needed to render a unit combat ineffective depends on the
   unit's type, discipline, and morale" (doctrine record :126, :340, quoting FM 3-90 B-23). Doctrine is
   explicit that this verb needs a designated enemy and that area substitution changes the task
   (doctrine record :295, :314-318). [V record / A manual]
4. CAN VR-FORCES TELL? THIS IS THE OWNER'S OWN EXAMPLE, SO IN FULL:
   - (i) NO shipped task self-completes on destruction. There is no destroy task (VENDOR-1). The
     closest shipped behaviour is plt_attack_by_fire, which ends "When there are no more enemy contacts
     DETECTED" with a give-up timeout (VENDOR-2) - a sensor test, not a ground-truth one. [V]
   - (iii) YES, THE SIM CAN BE ASKED, through the plan channel. The condition DtCeEntDestroyed exists,
     is settable by UUID from an outside controller, and the vendor defines a unit as destroyed "when
     all members of the unit are destroyed" for the entity-level models we run (VENDOR-3). Combined
     with DtCeEntInArea and the AND operator, the vendor's own vocabulary can express something very
     close to the owner's sentence: for a named unit, "in area X" AND NOT "destroyed". What it cannot
     express as shipped is the unquantified form "no enemy unit operational in the area" over unknown
     entities - every condition names a specific object UUID. [V]
   - (iii) alternatively THE INTERFACE could evaluate it: damageState() and forceId() are on the
     reflected state repository of every entity it already discovers (INTERFACE-1), so "no entity of a
     hostile force, inside this polygon, with damage state below destroyed" is computable from the wire
     - but the facade exposes neither field today, and the normal path does not enumerate reflected
     entities at all. That is a new facade read, not a new data source. [V]
   - THE BLOCKER IS THE ORDER, NOT THE SIMULATOR: both DESTRY tasks name the performing unit as their
     own affected entity (the whole order does, 42 of 42), so there is no named enemy for either the
     sim's condition or an interface test to point at. [V]
5. CODE TODAY: classified Attack (VerbMapping.cs:105) - the desired-effect code DSTRYK is NOT READ. I
   grepped the whole application source for DesiredEffect: zero hits. [V] So destroy, defeat and
   neutralise are indistinguishable to the code, and the task runs as move-then-fire, ending on its
   Duration timer or the engage's completion.
6. OWNER: W-FUZZY, his example verbatim - "is the sim able to determine when some of the desired effect
   has been achieved, for example DESTROY (no enemy unit operational in the target area or something
   like that?)". Nothing else on file.
7. OPEN QUESTION FOR THE OWNER - the one place the record contradicts itself. On file is the 2026-09-14
   ruling "the target IS the objective in doctrinal terms. Enemies may happen to be in there"
   (ledger RL-20260914-02), and on file is doctrine's requirement that destroy names an enemy force,
   without which its end state has no subject (doctrine record :295, :314-318). Both the simulator's
   own destroyed-test and any test the interface could make need that name, and no order on disk
   carries one.
8. TEMPORARY POSITION (RL-20260921-09): ends at start time plus Duration; if the unit is still
   travelling then, it completes on arrival; a unit that gets stuck is an abort under the stall
   ruling already on file; whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### FIX

1. COA-STP1 3 (4, 2 and 1 points); full Iron Storm 1 (3 points); cut A 0. [V]
2. EITHER. The 4-point one is a drive along an axis; the 1-point one is in effect a place.
3. DOCTRINE, AND DOCTRINE ITSELF ASKS FOR A TIME: "Fix is a tactical mission task in which a unit
   prevents the enemy from moving from a specific location for a specific period", and "This task
   usually has a time constraint, such as 'fix the enemy reserve force until OBJECTIVE FALON is
   secured.'" (doctrine record :120, :357, quoting FM 3-90 B-35, B-36). The doctrine pass puts FIX in
   its group B - "doctrine says the task is OPEN-ENDED and the ORDER MUST SUPPLY A DURATION" (:346-360).
   [V record / A manual]
4. CAN VR-FORCES TELL? (ii)/(iv). No fix task exists. The nearest shipped behaviour is
   plt_attack_by_fire, whose own end is loss of contact or a timeout (VENDOR-2) - which is a time bound,
   not a fix. Whether the enemy is prevented from moving is not observable: it would need positions of
   named enemy entities over time, and the order names no enemy. [V]
5. CODE TODAY: classified Attack (VerbMapping.cs:106) - move, then fire at the (self) target, i.e. the
   geometry. Ends on the Duration timer. [V]
6. OWNER: nothing on file naming FIX. W-TIME's "tasks that do not include movement" fits the 1-point
   cases and not the 4-point one.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): the instances that travel end at start time plus Duration,
   or on arrival if they are still travelling then; the instances with no destination end at their
   Duration. A unit that gets stuck is an abort under the stall ruling already on file. Whether the
   desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### DISRPT - disrupt

1. COA-STP1 2, both 1 point; Iron Storm 0. [V]
2. EITHER - one point is a place, not a route.
3. DOCTRINE, AND IT SAYS THE VERB IS NOT DIRECTLY EVALUABLE: "Disrupt is a tactical mission task in
   which a unit upsets an enemy's formation or tempo", and the record's own summary line is
   "EFFECT-BASED, NOT DIRECTLY EVALUABLE ... 'Disruption is not an end; it is the means to an end.'"
   (doctrine record :127, and the group-C row :371, quoting FM 3-90 B-29 to B-31). [V record / A manual]
4. CAN VR-FORCES TELL? (iv) NOT OBSERVABLE. Nothing in the condition vocabulary (VENDOR-3) expresses
   formation or tempo. There is no disrupt task. [V]
5. CODE TODAY: classified Attack (VerbMapping.cs:107); move-then-fire; ends on the Duration timer. [V]
6. OWNER: nothing on file.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### BREACH

1. COA-STP1 3 (4, 3 and NO points); full Iron Storm 1 (3 points); cut A 0. [V]
2. USUALLY MOVEMENT FIRST - two carry a route or a lane; T21 carries no geometry at all and is executed
   where the unit stands.
3. DOCTRINE: "Breach is a tactical mission task in which a unit breaks through or establishes a passage
   through an enemy obstacle", and the end state is that a passage or lane exists through the obstacle
   (doctrine record :122, :339, quoting FM 3-90 B-7, B-8). [V record / A manual]
4. CAN VR-FORCES TELL? PARTLY, AND ONLY IN THE OTHER PROFILE.
   - (i) DtBreachTask is real - "a Breach task for an entity to go to said object and breach it"
     (breachTask.h:20-21) - and it reports completion like any task, with the success flag
     (taskCompleteReport.h:84-90). So the vendor's own primitive does self-complete. [V]
   - (iii) the EFFECT has a dedicated condition, DtCeBreached ("an Engineering Object breached",
     ceBreached.h:23) - but the help marks Engineering Object Breached "(Aggregate-level scenarios
     only.)" and we run entity level, so on the profile we run it is not available. [V]
   - The obstacle must be a simulation object the interface can name. In COA-STP1 it is the taskee
     itself, so there is nothing to breach [A - assessment :330, G6's log line].
5. CODE TODAY: classified Breach and wired (VerbMapping.cs:103, :88); approach move, then a breach
   against the resolved obstacle (VrfC2SimService.cs:4547, :5321), with only the target set and never
   the vendor's start and end points [A - assessment :313-314]. Ends on the Duration timer or the
   vendor completion. [V for the code lines]
6. OWNER: nothing on file naming BREACH.
7. OPEN QUESTION FOR THE OWNER: none. Recorded instead: the vendor's own "has it been breached"
   test exists only in the aggregate-level profile, which this build does not run - a consequence
   of the 2026-09-14 model-set answer (RL-20260914-03, R5: "R5 proceed as recommended"; that the
   recommendation was EntityLevel first is recorded at TASK_VOCABULARY_ASSESSMENT R5, not in the ledger
   quote), not a new question.
8. TEMPORARY POSITION (RL-20260921-09): the instances that travel end at start time plus Duration,
   or on arrival if they are still travelling then; the instances with no destination end at their
   Duration. A unit that gets stuck is an abort under the stall ruling already on file. Whether the
   desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### CLRLND - clear

1. COA-STP1 1 (T36, 3 points); full Iron Storm 1 (3 points); cut A 0. [V]
2. MOVEMENT FIRST, always: doctrine's own graphic gives a start and a limit of advance, and the
   exported three points supply both.
3. DOCTRINE: "Clear is a tactical mission task in which a unit eliminates all enemy forces within an
   assigned area", ending when no enemy remains in the area AND the limit of advance is reached; the bar
   on the symbol "designates the desired limit of advance" and "establishes the width of the area to
   clear" (doctrine record :134, :338, quoting FM 3-90 B-16 to B-18). Doctrine even allows a size
   threshold, clearing only enemy larger than a stated size. [V record / A manual]
4. CAN VR-FORCES TELL? NO, AND THIS IS THE SHARPEST MISMATCH IN THE TABLE. The vendor ships a Clear
   behaviour (co_clear) whose help says the unit "lines up at a starting position and then moves to a
   line, attempting to remove all opposing forces along the way"
   (help\Content\Tasks\UnitBehaviors\OffenseClear.htm) - but the script's entire top node is
   `{calcInitPositions, moveToInit}` and it contains NO test of any kind for enemy presence or
   destruction (VENDOR-2). So the shipped behaviour is a formed advance to a line: (i) it ends on
   reaching the line, never on the area being clear. The effect would be (iii) computable by the
   interface, from hostile affiliation and damage state inside the polygon - the same new facade read
   DESTRY needs (INTERFACE-1). [V]
5. CODE TODAY: classified Clear, NOT WIRED (VerbMapping.cs:119, :92-94) - the task drives its three
   points as a bare route. Ends on arrival evidence or the Duration timer. [V]
6. OWNER: nothing on file naming CLEAR.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): ends at start time plus Duration; if the unit is still
   travelling then, it completes on arrival; a unit that gets stuck is an abort under the stall
   ruling already on file; whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### SECURE

1. COA-STP1 4 (three with 1 point, one with none); full Iron Storm 3 (1 point each); cut A 0. [V]
2. EITHER. A single exported point is a place, not a journey, so nothing in the order says these units
   must go anywhere; T38 carries no geometry at all.
3. DOCTRINE, AND DOCTRINE ITSELF DEMANDS A STATED DURATION: "Secure is a tactical mission task in which
   a unit prevents the enemy from damaging or destroying a force, facility, or geographical location",
   and "The commander states the mission duration in terms of time or event when assigning a mission to
   secure a given unit, facility, or geographic location" (doctrine record :119, :354, quoting FM 3-90
   B-56). The substantive end state is that no enemy direct or observed indirect fires impact the
   secured location. [V record / A manual]
4. CAN VR-FORCES TELL? (ii)/(iii). There is no secure task (VENDOR-1). The repo's own synthesis for
   this family is "position-inside-area + dwell >= Duration, then emit TASKCMPLT ourselves"
   (assessment :631), and it says plainly that "'Objective held' has no native completion at all - a
   hold is open-ended". The position-inside-area half is (iii) - computable by the interface from
   positions it already reads. The "no enemy fires impact it" half is (iv) on the data the facade
   exposes today; the sim has an ENTITY UNDER FIRE condition (VENDOR-3) that could carry it in the plan
   channel. [V]
5. CODE TODAY: classified HoldObjective, NOT WIRED (VerbMapping.cs:109, :92-94) - a bare move to the
   single point, then nothing. The only thing that ends it is the Duration timer (or arrival evidence
   for the ones with a route). [V]
6. OWNER: W-ENDTIME is the ruling most directly about this verb - the question as put named "SECURE,
   OCCUPY and DEFEND" and his answer was "4 given by the end time" (ledger RL-20260914-02). W-TIME then
   narrowed it: "The rulling based on time is for tasks that do not include movement, such as defend in
   place and similar ones - units that are not expected to reach any other location."
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): the instances that travel end at start time plus Duration,
   or on arrival if they are still travelling then; the instances with no destination end at their
   Duration. A unit that gets stuck is an abort under the stall ruling already on file. Whether the
   desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### OCCUPY

1. COA-STP1 3, all with 1 point; full Iron Storm 1 (5 points); cut A 0. [V]
2. USUALLY MOVEMENT FIRST by doctrine ("moves into an area"), but the COA's three carry one point each,
   so the order itself does not describe a journey. The Iron Storm one carries five points.
3. DOCTRINE, AND THIS ONE IS EVALUABLE: "Occupy is a tactical mission task in which a unit moves into an
   area to control it WITHOUT ENEMY OPPOSITION. Both the friendly force's movement to and occupation of
   the area occur without enemy opposition", so the end state is arrival inside the area plus control,
   and "A unit can control an area without occupying it, but not vice versa" (doctrine record :121,
   :337, quoting FM 3-90 B-53, B-20). Because there is no enemy by definition, area substitution is
   trivially correct here (doctrine record :282). [V record / A manual]
4. CAN VR-FORCES TELL? (iii) YES, in the only sense doctrine asks for. Occupation is position-in-area,
   and the simulator has the test as a first-class condition (DtCeEntInArea, VENDOR-3) while the
   interface could compute it from positions it already reads against an area the initialization
   already created in VR-Forces (INTERFACE-1). No shipped task self-completes on it: the nearest, the
   vendor's occupy-firing-positions behaviour, is a posture, not an area test [A - assessment :448].
5. CODE TODAY: classified HoldObjective, NOT WIRED (VerbMapping.cs:110) - bare move to the point, ends
   on the Duration timer. [V]
6. OWNER: W-ENDTIME names OCCUPY explicitly in the question as put ("SECURE, OCCUPY and DEFEND have no
   natural completion in VR-Forces"), answered "4 given by the end time".
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): ends at start time plus Duration; if the unit is still
   travelling then, it completes on arrival; a unit that gets stuck is an abort under the stall
   ruling already on file; whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### RETAIN

1. COA-STP1 1 (T34, NO geometry); full Iron Storm 3 (all with no geometry); cut A 0. [V]
2. NO MOVEMENT. The task carries no Location at all in all four instances across both orders, and
   doctrine says a unit tasked to retain "does not necessarily have to occupy" the ground.
3. DOCTRINE, AND DOCTRINE DEMANDS A DURATION: "Retain is a tactical mission task in which a unit
   prevents enemy occupation or use of terrain", and "A commander assigning this task specifies the area
   to retain and the duration of the retention, which is time or event driven" (doctrine record :133,
   :355, quoting FM 3-90 B-55). [V record / A manual]
4. CAN VR-FORCES TELL? (ii). No retain task; nothing self-completes; the effect ("the enemy did not
   occupy or use it") is the absence of an event, which none of the vendor's conditions expresses
   directly - the closest is a negated Entity In Area over a NAMED enemy (VENDOR-3), and the order names
   none. [V]
5. CODE TODAY: classified HoldObjective and NOT WIRED, but because it carries no geometry it is taken by
   the zero-geometry path instead: the unit is dispatched where it stands, no vendor task is sent, a
   note goes out saying so, and the Duration timer is the only thing that ends it
   (TaskDispatchPolicy.cs:49-68; VerbMapping.cs:112). [V]
6. OWNER: nothing naming RETAIN. W-TIME's description - "units that are not expected to reach any other
   location" - fits these four tasks as exported.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### BLOCK

1. COA-STP1 2, both with 1 point; Iron Storm 0. [V]
2. EITHER - one point, so the order describes a place.
3. DOCTRINE, AND DOCTRINE DEMANDS A TIME OR AN EVENT: "Block is a tactical mission task that denies the
   enemy access to an area or an avenue of approach", and "A blocking task normally requires the
   friendly force to block the enemy force for a certain time, or until a specific event has occurred"
   (doctrine record :125, :356, quoting FM 3-90 B-5, B-6). [V record / A manual]
4. CAN VR-FORCES TELL? (ii)/(iv). No block task. The effect is the enemy's non-arrival, which is the
   absence of an event over entities the order does not name. [V]
5. CODE TODAY: classified HoldObjective, NOT WIRED (VerbMapping.cs:113) - bare move to the point, ends
   on the Duration timer. [V]
6. OWNER: nothing naming BLOCK.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### DEFEND (a) ground defence

1. COA-STP1 2 (T9, T10) - BOTH WITH NO GEOMETRY, and both are in fact air-defence tasks by their names;
   Iron Storm 0. So the COA contains no ground-defence instance. [V]
2. NO MOVEMENT - the verb itself implies staying, which is the case the owner names in W-FUZZY.
3. DOCTRINE: not a tactical mission task but a TYPE OF OPERATION, and OPEN-ENDED: it "Ends on the higher
   commander's transition decision, not on any observable the defending unit produces", and a battle
   position is explicitly "not an assigned area", so "arrived at the BP is not completion, it is
   occupation" (doctrine record :128, :367, quoting FM 3-90 A-63, A-64 and the glossary).
   [V record / A manual]
4. CAN VR-FORCES TELL? (ii) NO. There is no defend task at entity level (VENDOR-1). At aggregate level
   unit-defend exists and is a posture change [A - assessment :492]. Doctrine says there is no
   observable to look for, so this is the one verb where "the sim cannot tell" is not a gap in the
   simulator. [V for the vendor absence]
5. CODE TODAY: classified HoldObjective, NOT WIRED (VerbMapping.cs:114); with no geometry it takes the
   in-place path and ends only on its Duration timer (TaskDispatchPolicy.cs:61-68). [V]
6. OWNER: W-FUZZY names this verb as his example of the stay-in-place kind - "Some verbs imply that the
   unit stays in place (e.g. to DEFEND)". W-TIME names it too - "such as defend in place and similar
   ones". W-ENDTIME's question named DEFEND and he answered "4 given by the end time".
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
### DEFEND (b) air-defence coverage - the same code, a different task

1. Both COA-STP1 DEFEND tasks are this kind (T9 "ProvideAirDefenseCoverageForBrigadeManeuver...",
   T10 "ContinueAirDefenseCoverage...SouthOfPlBronze"), neither carries geometry. [V]
2. NO MOVEMENT, but doctrine derives a POSITION from the defended asset rather than from the order
   (doctrine record :223-235, citing FM 3-01.44). [A record's citation, manual not opened]
3. DOCTRINE: continuous protection of the supported force; RECORD SILENT on a completion condition - the
   doctrine record gives this case a geometry rule and no end state. Searched the doctrine record's
   sections 1, 2.2 and 4 for an air-defence end state; there is none.
4. CAN VR-FORCES TELL? (ii) NEVER SELF-COMPLETES. The only native home is the aggregate-level background
   behaviour automatic_air_defense, whose own header says it "checks the contact list each tick ... finds
   the closest such target. Then it starts a Attack_with_AntiAir_Missile task"
   (AggregateLevelBase\scripts\Automatic_Air_Defense.lua:1-8) - a perpetual loop with no terminating
   branch. It is also aggregate-profile only, and we run entity level. [V]
5. CODE TODAY: as (a) - in place, ends on the Duration timer. This chain is the one run G6 lost entirely
   when zero-geometry tasks were refused [A - assessment :332-333]; that refusal is gone.
6. OWNER: as (a).
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### GUARD

1. COA-STP1 1 (T30, 1 point); Iron Storm 0. [V]
2. NO MOVEMENT as exported (one point), though doctrine orients a guard on the moving main body.
3. DOCTRINE, AND IT IS EXPLICIT: guard is a TYPE OF SECURITY OPERATION, "NEVER SELF-COMPLETING - same
   relief/handover logic as screen", ending on relief or battle handover, not on arrival (doctrine
   record :131, :366, quoting FM 3-90 13-72 to 13-75 and 13-54). [V record / A manual]
4. CAN VR-FORCES TELL? (iv). No guard task; battle handover and relief are not concepts the simulator
   models, and nothing in the condition vocabulary expresses them (VENDOR-3). [V]
5. CODE TODAY: classified HoldObjective, NOT WIRED (VerbMapping.cs:115) - bare move to the point, ends
   on the Duration timer. [V]
6. OWNER: nothing naming GUARD.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### SCREEN

1. COA-STP1 3 (one with no geometry, two with 1 point); full Iron Storm 3 (none, 3 points, 7 points);
   cut A 0. [V] They form one serial chain, T24 -> T25 -> T26.
2. EITHER - and this is the verb where the export and the behaviour disagree most: two of the three
   carry a single point, yet the code turns any multi-point screen into a patrol that drives forever.
3. DOCTRINE, AND IT IS EXPLICIT: screen is a TYPE OF SECURITY OPERATION and NEVER SELF-COMPLETES -
   "Displacement to these subsequent PLs is event driven (enemy or friendly) or time driven. The approach
   of an enemy force, relief of a friendly unit, or movement of the protected force dictates the movement
   of security forces", with the rear boundary the battle handover line (doctrine record :123, :366,
   quoting FM 3-90 13-52, 13-54). The record adds the consequence in its own words: "Chaining a
   never-completing task as a STREND predecessor stalls everything behind it - which is the T24 -> T25
   -> T26 screen chain exactly" (:397-401). [V record / A manual]
4. CAN VR-FORCES TELL? (ii) FOR THE PROFILE WE RUN, AND (i) FOR THE OTHER ONE.
   - Entity level: the primitive is a patrol, and the vendor's own header says it never ends - "An entity
     executing a DtPatrolRouteTask will not advance to another task unless one of the following things
     happens: A trigger for the entity fires ... The entity is retasked or set Tasked by Superior"
     (patrolRouteTask.h:31-37). [V]
   - Aggregate level: reconnoiter-route DOES end, and it ends ON A DURATION. Its parameters are route,
     duration ("Duration to perform task"), patrol, deployDrones, useRoads, and it ends the task with the
     subtask's result (reconnoiter-route.lua:24-53). The subtask it delegates to says the rule outright:
     "if there is no duration, at the end of the move-along, quit" and it burns a duration down with a
     wait (perform-ground-reconnaissance.lua:250-253, :630, :648, :671). So the VENDOR'S OWN ANSWER for
     a screen is the same shape as the owner's: a time. [V]
   - The effect (early warning given) is (iv) not observable.
5. CODE TODAY: classified Reconnoiter and WIRED (VerbMapping.cs:116, :89) - a multi-point screen becomes
   a patrol along the created route (VrfC2SimService.cs:4983, :5514), which by the header above never
   reports completion. The ONLY thing that ends a screen today is its Duration timer
   (TimedCompletionPolicy.cs:76-89). Arrival evidence cannot end it either: a patrol returns to its start,
   and a route whose last vertex is near the dispatch position is refused for arrival evidence outright
   (ArrivalPolicy.cs header, the ClosableByArrival rule; RUNBOOK sec 11d). [V]
6. OWNER: nothing on file naming SCREEN. On patrol his 2026-09-21 words are "there's plenty of tasks
   that involve no movement, as discussed repeatedly. And there is ruling for thoise as well -  they are
   completed when their duration elapses" (W-TEMP2), which supersedes lane B's older "patrol NOT RULED".
7. OPEN QUESTION FOR THE OWNER: none. Recorded instead: the vendor's own reconnaissance task ends
   on a duration, exactly the shape of the position on file, and it exists only in the
   aggregate-level profile (reconnoiter-route.lua:24-26; perform-ground-reconnaissance.lua:250-253).
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### SCOUT

1. COA-STP1 0; Iron Storm 0. It appears 29 times in data\VRF-Approved-5June24_Order.xml [A - assessment
   :171], which is a vendor-supplied fixture, not an STP export.
2. EITHER.
3. DOCTRINE: RECORD SILENT. The doctrine pass covers the 17 COA verbs and a further list of Appendix B
   tasks; SCOUT is in neither table. Searched sections 1.1 and 1.2 of the doctrine record.
4. CAN VR-FORCES TELL? Identical to SCREEN - patrol never ends, aggregate-level reconnoiter ends on a
   duration. [V]
5. CODE TODAY: identical to SCREEN - Reconnoiter, wired, patrol (VerbMapping.cs:117). [V]
6. OWNER: nothing on file.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### MOVE

1. COA-STP1 1 (T16, and it carries NO geometry); Iron Storm 0. But MOVE is what every live fixture
   exercises - three of the R9 fixtures, seven in the formation fixture and 38 in the vendor order
   [A - assessment :164-172]. [V for the COA count]
2. MOVEMENT, always - except that the single COA instance carries no geometry, which is a contradiction
   between the verb and the export and is recorded as an order-side issue (doctrine record :253). [V]
3. DOCTRINE: "Arrival." That is the doctrine record's entire entry, and MOVE is the first row of its
   group A, the group where "doctrine gives a state the simulation can evaluate" (doctrine record :129,
   :332, :343). [V record]
4. CAN VR-FORCES TELL? (i) YES, TWICE OVER, AND BOTH ARE ALREADY BUILT.
   - The vendor's move-along reports its own completion - but for a UNIT it is premature by design:
     "The pseudo-aggregate considers the Move Along task complete for the pseudo-aggregate as a whole
     when its lead subordinate completes the task" (moveAlongTasks.h:35-37). [V]
   - The interface's own arrival evidence (C15) is the second, and it exists because the opposite failure
     was also observed - the vendor holding a unit's completion behind one straggler for eight simulated
     hours (ArrivalPolicy.cs:14-27; DESIGN_ORBAT_TO_VRF_2026-09-06.md:139-148). The rule is a majority of
     members within a radius of the last vertex AND each having actually travelled, with a route that
     ends where it began refused outright (ArrivalPolicy.cs:28-113; RUNBOOK sec 11d). [V]
5. CODE TODAY: classified Move and wired (VerbMapping.cs:102, :86). A route is created and driven; the
   task ends on the vendor's completion, on arrival evidence, or on the Duration timer - whichever
   arrives first, and whichever it is cancels the others (TimedCompletionPolicy.cs:135-136;
   TaskStatusPolicy.cs:70-94). THE DIFFERENCE FROM THE TEMPORARY POSITION IS HERE: the timer is armed for
   EVERY task including moves - the arming site (VrfC2SimService.cs:5246) never looks at the verb - so a
   unit that has not arrived when its Duration runs out is reported
   COMPLETE, and because a completion suppresses any later abort (TaskStatusPolicy.cs:84-94) the stall
   watchdog's verdict on that unit can no longer be reported. [V]
6. OWNER: W-ARRIVAL ("report a unit's completion from the unit's own arrival evidence is fine", ledger
   RL-20260907-01); W-TIME ("The notion that geting stuck midway is a complete is completelly illogical"
   and "Arriving late does _not_ imply an abortion - what happens is that follow on tasks are delayed by
   the slow progress on a leg"); W-857 ("a unit that is meanto to move to an objective and perform some
   action, but instead gets stuck in the middle of the way certainly did not complete"); W-TEMP.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): ends at start time plus Duration; if the unit is still
   travelling then, it completes on arrival; a unit that gets stuck is an abort under the stall
   ruling already on file; whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### ESCRT - escort

1. COA-STP1 1 (T28, 1 point, named for the convoy graphic); Iron Storm 0. [V]
2. MOVEMENT, always - an escort moves with what it escorts.
3. DOCTRINE: convoy security, "A specialized area security task conducted to protect convoys", terrain
   oriented, ending "when the convoy completes its move / the route-security period expires. Not
   self-completing as a 'follow' task" (doctrine record :130, :372, quoting FM 3-90 and FM 1-02.1).
   [V record / A manual]
4. CAN VR-FORCES TELL? (ii) NO. The vendor's own header is explicit: an entity following another "will
   not advance to another task" unless it is retasked, set tasked by superior, a trigger fires, or "The
   entity being followed is destroyed" (followEntityTask.h:35-42). So a follow ends only on an external
   event, and one of the three is the death of the escorted unit. [V]
5. CODE TODAY: classified Escort and wired (VerbMapping.cs:118, :90) - a follow is issued against the
   resolved escorted entity and the task returns without any route (VrfC2SimService.cs:4351). Two known
   warts: the follow offset is left at zero so the escort stations on top of its leader, and in this
   order the escorted entity resolves to the taskee itself, so the task falls through to a bare drive
   [A - assessment :311-312, :671-673]. Ends on the Duration timer. [V for the code line]
6. OWNER: nothing on file naming ESCRT. W-TEMP2's "tasks that involve no movement ... are completed when
   their duration elapses" covers a follow, which has no destination of its own; it supersedes lane B's
   older "follow and escort NOT RULED".
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### ExecutePlanPhase - plan-phase marker

1. COA-STP1 0; Iron Storm cut A 2 (1 point each); full Iron Storm 3. [V] Until 2026-09-20 this verb was
   unrecognised and ran as a bare drive (VerbMapping.cs:121-139).
2. NO MOVEMENT. The schema's only sentence about it is on the TRIGGER that consumes it: "A trigger for
   the execution of a plan phase that is based on the start time of a task. Typically this task will be
   defined in an order and will have a TaskActionCode of ExecutePlanPhase" [A - quoted in
   VerbMapping.cs:128-131 from the schema; the xsd was not opened this pass].
3. DOCTRINE: RECORD SILENT - this is a C2SIM plumbing code, not a tactical mission task, and the doctrine
   pass does not list it. Searched both doctrine tables.
4. CAN VR-FORCES TELL? Not applicable: there is no activity for the simulator to perform or evaluate.
5. CODE TODAY: classified HoldInPlace and wired (VerbMapping.cs:140-142) - no vendor task is sent, the
   task is executed where the unit stands, the geometry it carries is named in the log as not driven, and
   it ends at its Duration. [V]
6. OWNER: nothing on file.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------
### CRESRV - constitute reserve

1. COA-STP1 0; Iron Storm cut A 1 (no geometry); full Iron Storm 2 (no geometry). [V]
2. NO MOVEMENT as exported - neither instance carries a Location.
3. DOCTRINE: RECORD SILENT. Not in either doctrine table; "constitute reserve" is a JC3IEDM code, and
   FM 3-90's Appendix B list of 27 tactical mission tasks does not include it (doctrine record :159-163).
   The nearest doctrinal statement is the record's note that commanders are not limited to Appendix B
   (:168-173). [V record]
4. CAN VR-FORCES TELL? (iv). The repo's own survey says there is no vendor task for it at all
   [A - assessment :692-694]. Nothing evaluates "is this unit still uncommitted".
5. CODE TODAY: classified HoldObjective and therefore NOT WIRED (VerbMapping.cs:155), deliberately - the
   comment says recognising it is the whole change, so the gap stays loud instead of the verb reading as
   unknown. With no geometry it takes the in-place path and ends on its Duration. [V]
6. OWNER: nothing on file.
7. OPEN QUESTION FOR THE OWNER: none.
8. TEMPORARY POSITION (RL-20260921-09): this task has no destination, so it ends at start time plus
   Duration. Whether the desired effect was achieved is ignored for now.
---------------------------------------------------------------------------------------------------

## 4. THE CROSS-CUTTING QUESTIONS - not per verb

Q-A. WHAT DOES A DURATION MEAN FOR A TASK THAT INCLUDES MOVEMENT?
  On file, his words: "Seems to me that the time applies to the full task, including the movement and the
  achievement of the desired effect" (W-FUZZY) and "The rulling based on time is for tasks that do not
  include movement" (W-TIME) and today's "completion is based on  start time + duration. Tasks involving
  units may still arrive late. If they arrive after the expected start time + duration, they complete
  immediatelly" (W-TEMP). Measurement: every task in both orders carries a Duration - 42 of 42 in
  COA-STP1 (32 at one hour twenty, 10 at two hours) and 5 of 5 in Iron Storm cut A (4 at twenty minutes,
  1 at thirty); neither order carries an end time. [V, measured this pass] Separately: the REAL Iron
  Storm export writes its durations in the short form "PT20M", which the C2SIM 1.1 schema pattern does
  not allow, so the parser refuses all 23 of them and those tasks have no end time at all
  (OrderParser.cs:87-105). [V] Cut A's derived file uses the long form and parses.

Q-B. WHAT IS REPORTED IF THE END TIME ARRIVES AND THE UNIT HAS NOT REACHED THE OBJECTIVE?
  On file: "The notion that geting stuck midway is a complete is completelly illogical" (W-TIME); "a unit
  that is meanto to move to an objective and perform some action, but instead gets stuck in the middle of
  the way certainly did not complete" (W-857); "My caveat was that you should not expect every task to
  require a movement, and abort in case the unit stays put" (W-857). Measurement of the code on main: the
  Duration timer is armed for every dispatched task including moves - the arming site
  (VrfC2SimService.cs:5246) does not branch on the verb at all, only on whether a Duration is present and
  non-zero - and when it fires the task is reported COMPLETE; a completion then
  suppresses any later abort for that task (TaskStatusPolicy.cs:84-94). [V] The position on file says a
  late unit completes WHEN IT ARRIVES; the code completes it when the clock runs out. On the case of a
  unit that never arrives at all, his words of 2026-09-21 are: ""a unit that never arrives" - isn't
  there ruling already for units that get stuck? That's the only way a unit can "never arrive"."
  (W-TEMP2), and the stall ruling he points at is W-STALL (RL-20260914-01: abort is the code STP sees for
  a stalled unit). Whether an earlier timed completion may suppress that abort, and whether a later
  arrival still reports complete, are NOT in his answer - they are supervisor positions (that entry's
  scope note). This doc settles neither; it records only that the code on main does suppress the abort.

Q-C. DO A STUCK UNIT'S FOLLOW-ON TASKS WAIT, AND FOR HOW LONG?
  On file: "Arriving late does _not_ imply an abortion - what happens is that follow on tasks are delayed
  by the slow progress on a leg" (W-TIME) and "Follow-on tasks still are permitted to take their whole
  specified duration, even if they were forced to start late" (W-TEMP), and his correction of the word:
  "I meant to say "follow ons" - the tasks for the same unti that may be set to start after it has
  completed a preceding task" (W-FUZZY). Measurement: a follow-on waits for its predecessor to dispatch
  for a backstop of 86,400 seconds and then for it to complete for the predecessor's own scaled duration
  plus a margin (RUNBOOK sec 11, the two-window rule; TaskSequencer.cs:55-70). The progress watchdog's
  abort is REPORT ONLY - it pushes a task status and does NOT tell the follow-ons to stop waiting; its
  emission site (VrfC2SimService.cs:7296-7299, whose own text says "report only: the task stays in
  flight ... nothing is re-tasked") calls no abandon, unlike the twenty other dead ends that do. [V]
  So today a
  stalled unit's follow-ons wait until the stalled task's own timer completes it, and then run.
  NO BOUND ON WAITING FOR A LATE MOVER IS ON FILE - lane B records it as not ruled.

Q-D. HIS OWN EARLIER QUESTION (W-OPEN, ledger RL-20260921-04):
  "Is this creating a special class of errors for no movement from the get go as opposed to movement that
  falls short of the objective?" His 2026-09-21 correction (W-TEMP2) bears on it - a unit that gets stuck
  is covered by the stall ruling whether it moved a little or not at all - but the record does not say
  whether the two cases should read differently on the wire. What can be measured: the code draws no
  such distinction anywhere - the timer treats a unit that never moved and a unit that moved most of the
  way identically, and the arrival rule's own refusal case (a route that ends where it began) is about the
  ROUTE's shape, not about what the unit did (ArrivalPolicy.cs:44-48, :86-102). [V]

Q-E. DOES THE PROGRESS WATCHDOG SHIP SWITCHED ON?
  On file: the question as put to him in 2026-09-13 asked for a yes and for which code STP should see, and
  his answer was "2 as recommended"; the question never mentioned a shipped default (lane B's note on
  ledger RL-20260913-03). W-STALL ("Ok on 2-3") settled the code as TASKABRT. Measurement: it is OFF by
  default, absent from both settings files, with no script switch - it is turned on only by an environment
  override (RUNBOOK sec 10, :2580-2603). [V] NOT RULED either way.

Q-F. THE ORDERS NAME NO ENEMY, so every effect test that needs one has no subject.
  Measurement: in COA-STP1 the affected entity equals the performing unit on all 42 tasks
  [A - assessment :106, not re-counted this pass], and the exported desired-effect code is TaskSuccess on
  40 and DSTRYK on the two destroy tasks [V, measured this pass]. The interface does not read the effect
  code at all (grep for DesiredEffect across the application source: zero hits). [V]

---

## 5. WHERE THE DOCTRINE RECORD, THE VENDOR DOCUMENTATION AND THE CODE DISAGREE

D1. THE REPO SAYS THE VENDOR'S TACTICAL SCRIPTS REPORT A REAL PASS OR FAIL. THE THREE THAT MATTER DO NOT
    CONTAIN THE CALL. The vocabulary assessment states, for the attack family and again for the hold,
    breach and clear families, "Completion evidence: scripted tasks end with vrf:endTask(bool) (284 true /
    260 false call sites)" and "vrf:endTask for the scripted ones" (assessment :612, :631, :642, :682).
    Measured this pass over the shipped entity-level scripts: 284 true and 260 false is right as a total
    (253 of 365 files contain one), but company_seize, co_clear, company_breach and plt_attack_by_fire -
    the four scripts the assessment nominates as the vendor homes for SEIZE, CLEAR, BREACH and the
    attack-by-fire family - contain ZERO. [V] They are behaviour-tree scripts registered with
    defineCommand, so how they end, and whether that ending reaches a remote controller as a task-complete
    report, is a different mechanism. The assessment's own section 6.3 item 4 already records the second
    half as unknown ("Whether a scripted task assigned remotely reports completion through the same
    DtTaskCompleteReport channel our facade already listens on. The header contract implies yes; nothing
    states it."), but the first half - that these scripts self-report success at all - is not supported by
    the files. Consequence for this table: every "the vendor task would tell us" cell for SEIZE, CLEAR and
    BREACH rests on an unverified mechanism.

D2. THE VENDOR'S HELP DESCRIBES AN EFFECT THAT THE VENDOR'S SCRIPT DOES NOT IMPLEMENT (Clear). The help
    page says a unit tasked to Clear "lines up at a starting position and then moves to a line, attempting
    to remove all opposing forces along the way" (OffenseClear.htm), and doctrine's end state is "no enemy
    remaining in the area, and the limit of advance reached" (doctrine record :338). The shipped script's
    whole behaviour is `{calcInitPositions, moveToInit}` with no enemy test of any kind. [V] So a reader
    who takes either the help page or the doctrine record at face value will expect an effect evaluation
    that does not exist.

D3. THE CODE ON MAIN ENDS MOVES ON THE CLOCK; THE OWNER'S WORDS AND THE DOCTRINE RECORD BOTH END THEM ON
    ARRIVAL. The doctrine record puts MOVE in its group A with the end state "Arrival" and keeps the
    duration-required verbs in a separate group B (doctrine record :332-360). The owner's words are
    "geting stuck midway is a complete is completelly illogical" (W-TIME) and today's "Tasks involving
    units may still arrive late. If they arrive after the expected start time + duration, they complete
    immediatelly" (W-TEMP). The code arms a duration timer on every dispatched task including moves, and
    a timer completion then suppresses the later real arrival and any stall abort for that task
    (TimedCompletionPolicy.cs:76-89; TaskStatusPolicy.cs:70-94). [V] This is the difference the seat is
    tracking as a defect, not a ruling.

D4. DOCTRINE PUTS SCREEN, GUARD AND ESCORT IN A NEVER-COMPLETING CLASS; THE VENDOR AGREES IN ITS HEADERS;
    THE CODE ENDS THEM ON A CLOCK ANYWAY. Doctrine: security operations end on relief or battle handover
    (doctrine record :366, :372). Vendor: a patrol "will not advance to another task" and a follow likewise
    (patrolRouteTask.h:31-37, followEntityTask.h:35-42). [V] Code: the only thing that ends them is the
    Duration timer, and the temporary position has no arrival for them to be late for. The vendor's own
    aggregate-level reconnaissance task resolves the same problem the same way - with a duration parameter
    (reconnoiter-route.lua:24-26, perform-ground-reconnaissance.lua:250-253) - which is a point of AGREEMENT
    between the vendor and the owner's position, and it is only available in a profile we do not run.

D5. A MISMATCH THE DOCTRINE RECORD FLAGS AND NOBODY HAS RESOLVED: the exported verb disagrees with the task
    symbol drawn in the initialization in at least four places - three follow-and-support symbols, one
    follow-and-assume and one neutralize exist in the data, but none of those codes appears among the 42
    exported verbs; those tasks went out as ATTACK, SECURE or DESTRY (doctrine record :539-546). The record
    marks it an unexplained order-side symptom. Any per-verb completion rule keyed on the exported code
    inherits it. [A - the record's measurement, not re-derived this pass]

D6. A SMALLER ONE, RECORDED SO IT IS NOT REDISCOVERED: the assessment counts 279 headers and 70 task
    classes in the vendor's task directory; I count 277 headers and 81 declarations matching the exported
    class pattern. [V] Different counting methods, same conclusion - and the conclusion (no tactical task
    class exists) reproduces exactly.

---

## 6. WHAT I COULD NOT FILL, AND WHAT I SEARCHED

Cells marked RECORD SILENT: 4.
 - SCOUT, doctrinal end state. Searched both doctrine tables (sections 1.1 and 1.2); the verb is in
   neither.
 - ExecutePlanPhase, doctrinal end state. Same search; it is a C2SIM plumbing code.
 - CRESRV, doctrinal end state. Same search; not among FM 3-90's 27 tactical mission tasks.
 - DEFEND as air-defence coverage, doctrinal end state. The doctrine record gives this case a GEOMETRY
   rule (section 2.2, rows T9 and T10) and no completion condition; searched sections 1, 2.2 and 4.

Cells I marked [A] rather than [V] because the source was not opened this pass:
 - every citation to FM 3-90, FM 1-02.1 and FM 3-01.44 paragraph numbers - the manuals are not on this
   machine and I read them through the doctrine record;
 - the vendor's remote-control sample's use of assignPlanByName;
 - the scripted-task type filter (which DIS types company_seize accepts) and the G6 run outcomes;
 - the self-target count of 42 of 42 and the zero-offset follow wart;
 - the C2SIM schema line for the ExecutePlanPhase trigger sentence (quoted from the code comment).

One thing I deliberately did not do: I did not open anything under C:\MAK\logs, did not launch anything,
and did not write anything under C:\MAK or in the repository.
