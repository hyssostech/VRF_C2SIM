# RUN 20260719T144109Z - first headless end-to-end C2SIM -> VR-Forces run

Run dir: runs/20260719T144109Z_run
Command: pwsh -File scripts\RunC2SimScenario.ps1 -RunSecs 120
Inputs:  data/R9_Mojave_Lean_Initialization.xml + data/R9_Mojave_UnitMove_Order.xml
appNos:  3510 backend, 3511 frontend, 3512 pre-check, 3513 trace, 3514 app, 3515 unused
Scored against: HEADLESS_RUN_PLAN.md sec 4a, RATIFIED 2026-07-19 BEFORE this run existed.

## 0. Headline

THE CHAIN WORKS. One command took a C2SIM init and order all the way through VR-Forces
launch, interface join, unit creation, task issue, telemetry capture and teardown, with
ZERO human interaction. That is the mandate's one-button loop, demonstrated.

*** PARTIALLY OVERTURNED 2026-07-19 by run 20260719T161438Z - READ
docs/experiments/PREREG_TSK_DELIVERY_2026-07-19.md BEFORE RELYING ON ANYTHING BELOW.
That run added VR-Forces' OWN position reports (RPT lines) as a SECOND, INDEPENDENT
oracle. The two oracles AGREE EXACTLY on the two frozen units and CONTRADICT on the third:
1222.MechPlt is reported by VR-Forces as moving steadily EAST toward its objective at
~1.4 m/s, while the WatchVrf POS stream below shows it 65 m WEST and stationary. The
"63.4 m of oscillation" analysis below is therefore an artifact of the POS oracle, not a
description of the unit. 114.MechCoy and 1.BdeHQ being frozen STANDS - confirmed by both
channels. ***

THE UNITS DID NOT MOVE. All three tasks were issued correctly by the interface and none
of the three taskees made meaningful progress toward its objective. This is the known
frozen-pile class, now reproduced HEADLESSLY with a clean trace and a pre-registered
criterion - which is the first time it can be measured rather than argued about.

## 1. What ran (stage by stage, all verified from the run artifacts)

| Stage | Result |
|-------|--------|
| LaunchVrf | EXIT=0 READY, ~50 s, back-end healthy |
| Oracle pre-check (advisory) | 28 POS lines, ALL degenerate. EXPECTED. (NOTE: the readings are CAST-CORRUPTED, not "positionless" - see CORRECTIONS_LOG.md. Degenerate rows are the observed pre-init state, not a health certificate.) |
| PushInit | EXIT=0, server to RUNNING, QUERYINIT reports 6 units for a late joiner |
| VrfC2SimApp | connected to C2SIM, dispatched 6 units + 0 areas |
| ORACLE GATE | PASSED - 44 real-coordinate POS lines across 44 uuids. First: 34.612956,-116.600460,1040.6 (ground clamp working: 1040.6 m, not the requested spawn altitude) |
| PushOrder | EXIT=0, order accepted |
| Observation | 120 s, reflected=54 readable=53, stable |
| StopIface | EXIT=0, server to UNINITIALIZED |
| VrfC2SimApp exit | code 0, CLEAN RESIGN - no stale federate |
| WatchVrf / ListenReports | both exited 0 on their own timers, never killed |
| StopVrf | **EXIT=3 - FAILED**, see sec 4 |

## 2. THE RESULT - per taskee, scored against ratified 4a

The interface issued all three tasks correctly (from vrfc2simapp.log):

    Task 'T_R5_PL1': CreateRoute (3 pts) for 1222.MechPlt -> MoveAlongRoute ec196214
    Task 'T_R5_CO1': CreateRoute (3 pts) for 114.MechCoy  -> MoveAlongRoute dc38cb31
    Task 'T_R5_TK1': CreateRoute (3 pts) for 1.BdeHQ      -> MoveAlongRoute 7052894d

Telemetry over 76 samples each, t=27.5 s to t=180.1 s:

| Taskee | uuid | net displacement | leg | % of leg | verdict |
|--------|------|------------------|-----|----------|---------|
| 1222.MechPlt (unit) | ec196214 | 63.4 m | 577.8 m | 11.0% | did not progress - see below |
| 114.MechCoy (unit) | dc38cb31 | **0.0 m** | 556.0 m | 0.0% | FROZEN |
| 1.BdeHQ (entity) | 7052894d | **0.0 m** | 577.8 m | 0.0% | FROZEN |

4a.1 ARRIVAL:    none arrived. Not close.
4a.3 RUNAWAY:    none. Nothing approached 5x leg or the 5 km AO radius.
4a.5 COMPLETION: NO TASKCMPLT was emitted for any task. Under 4a.5 that is
                 "not ARRIVED + no TASKCMPLT" = HONEST FAILURE for all three. The
                 interface did NOT lie in either direction, which is the least-bad
                 failure mode and is worth recording as a positive.

### 1222.MechPlt is NOT a slow march - it is oscillation in place

This is the subtle one and it must not be mis-read as partial success. Distance from its
start position over time:

    t= 39.7 s ->   0.2 m
    t= 49.9 s ->  66.6 m      <- one displacement
    t= 60.1 s ->  64.0 m
    t= 70.3 s ->  65.6 m
    ... oscillates 62.5 - 66.6 m for the remaining 130 s, never progressing ...
    t=172.0 s ->  64.9 m

Total path length summed over all steps: 199.8 m. Net displacement: 63.4 m. Median step
1.46 m per 2 s sample. It moved once, then jittered back and forth around a fixed point
for over two minutes. It is not en route.

### *** CORRECTED 2026-07-19: the four "untasked movers" are IDENTIFIED, and the finding
### is much stronger than the artifact reading it replaces ***

THIS SECTION ORIGINALLY CALLED THEM unexplained candidate artifacts showing "the census's
lockstep signature". THAT WAS WRONG, and the correction matters because the truth is worse.

The four - 82fe0939, 0279a70c, fbab58c5, ceffff34 - are the MEMBER ENTITIES of tasked unit
1222.MechPlt. They first appear 2.5 to 7.5 m from the taskee aggregate ec196214: a platoon
and its vehicles. Clustering ALL real uuids by first fix accounts for every object with no
orphans: 5 in the 1222.MechPlt cluster (aggregate + 4 members), 38 in the 114.MechCoy
cluster (company aggregate + its three subordinate platoon aggregates, which the app log
confirms were created, plus their members, spread 0-414 m), and 1 for 1.BdeHQ, which is a
lone tank ENTITY with no members at all. The interface created 6 objects; the other ~38 are
VR-Forces-side subordinate fan-out. (Cluster membership is INFERRED from co-location, not
from any logged id - the interface logs subordinate creation by NAME and never logs child
uuids. A cluster of exactly 5 at 2.5-7.5 m separation is not ambiguous, but the linkage is
geometric, not authoritative. See sec 6 item 6.)

THE DECISIVE ARITHMETIC - THEY MOVED THE WRONG WAY. 1222.MechPlt's destination lies 578 m
DUE EAST, bearing 090. Every one of the five travelled WEST:

| object | separation from taskee at first fix | bearing travelled | distance to destination |
|--------|-------------------------------------|-------------------|-------------------------|
| ec196214 (the taskee) | 0.0 m | **270 deg** | 1155.5 -> 1218.9 m (**+63.4**) |
| 82fe0939 | 7.5 m | **264 deg** | 1148.0 -> 1217.6 m (**+69.6**) |
| 0279a70c | 2.5 m | **268 deg** | 1153.0 -> 1217.6 m (**+64.5**) |
| fbab58c5 | 2.5 m | **272 deg** | 1158.0 -> 1217.9 m (**+59.9**) |
| ceffff34 | 7.5 m | **278 deg** | 1163.0 -> 1217.9 m (**+54.9**) |

Bearing to objective 090; bearings travelled 264-278. Essentially 180 degrees opposed.
Distance to the objective INCREASED for all five. Independently re-verified by the
supervisor from the trace.

TIMING TIES IT TO THE TASK. *** CLOCK CONVENTION, ADDED 2026-07-20 - REQUIRED BEFORE ANY EVENT ALIGNMENT:
trace t = wall-clock MINUS stage start MINUS a 5.89 s trace-t0 offset. Omitting the offset
bakes a systematic +5.9 s error into every alignment built on this document.
Applying it: the order published at trace t=27.7 s (NOT 33.6), and the westward snap at
t=41.7 is 14.0 s after the order (NOT "about 8 s"). ***
The order was published at 14:43:43.053 (c2sim-bus.log) = trace t=27.7 s. At t=41.7 s - 14.0 s later - the cluster snapped 63 m west in a SINGLE
2.0 s sample (~31 m/s). From t=43.8 s to t=180.1 s, 136 seconds, all four members are
BIT-IDENTICAL in every fix. The cluster's geometry rotated from an east-west line to a
north-south line while preserving its ~15 m spread.

BEST OFFLINE READING (INFERRED, not established): this is a FORMATION REORGANIZE - the
platoon shuffling into a march formation - and then nothing. The 90-degree rotation of a
preserved spread supports it. What it is NOT is a road march: 63 m of a 1155 m route,
executed as one snap, followed by 136 s of bit-frozen silence.

THE AGGREGATE-REFLECTION EXCUSE IS DEAD. The obvious defence of the interface would be
that tasked aggregates read stale while their members really drive (RUNBOOK sec 7 documents
a real dynamic_cast<DtReflectedAggregate*> failure). Three independent facts kill it:
  1. 114.MechCoy and ALL 37 of its co-located members hold ONE distinct lat/lon string
     across all 76 fixes. There are no moving members to hide behind.
  2. 1.BdeHQ is an ENTITY, not an aggregate. The cast problem cannot apply to it and it has
     no members. It received MoveAlongRoute and moved zero bits.
  3. WatchVrf's read path is demonstrably fine - it resolved sub-metre changes on 5 objects
     in the same trace.
Of the real-coordinate uuids, all but the 1222.MechPlt cluster are bit-exactly static.

## 3. *** CRITERION GAP FOUND BY THIS RUN - SINCE RULED AND APPLIED (see sec 7) ***

Per the sec 4a ratification protocol, this was raised as a DATED AMENDMENT with its
reason, and DATA ALREADY EXISTED when it was found. It was NOT silently applied: it was
put to the user, RULED, and is now AMENDMENT 1 in HEADLESS_RUN_PLAN.md sec 4a.2. Sec 7
below carries the re-scoring under the amended rule. This section is kept as written so
the sequence - gap found, amendment proposed, ruling obtained, then re-scored - stays
visible rather than being flattened into a tidy result.

4a.2 defines MOVED as ">= 25 m net displacement, sustained across at least 3 consecutive
samples". 1222.MechPlt satisfies BOTH clauses - 63.4 m net, dozens of consecutive
non-zero steps - while making no progress at all toward its waypoint. The rule cannot
distinguish PROGRESS from OSCILLATION, because it only ever compares against the start
position and never asks whether the distance to the DESTINATION is decreasing.

PROPOSED AMENDMENT (needs a user ruling; do not treat as in force):
  add to 4a.2 - MOVED additionally requires that the distance to the task's final
  waypoint DECREASE by at least the same 25 m threshold between first and last fix.
  A unit that displaces 63 m sideways, or oscillates around a point, is not moving.

This is exactly why the criterion was written before the data. The rule was wrong in a
way that would have been invisible if it had been written afterwards - a 63.4 m "MOVED"
result would have looked like partial success and been reported as such.

## 4. DEFECTS FOUND

1. **StopVrf leaves the back-end running.** EXIT=3. vrfGui closed correctly (the "Are You
   Sure?" dialog was detected and answered with 'Quit All Back-Ends' = On) but
   vrfSimHLA1516e survived 120 s. Evidence it had NOT begun shutting down: CPU rose
   399.8 s -> 408.9 s across an 8 s sample (about one full core), and it still held
   ESTABLISHED connections to port 6003 (rtiAssistant) and 4001 plus its UDP endpoint -
   still JOINED and still simulating. StopVrf correctly refused to force-kill it.
   RESOLVED BY HAND, and this is the fix StopVrf should adopt: a graceful
   CloseMainWindow() (WM_CLOSE) to the back-end brought it down in 5 SECONDS, cleanly,
   with no force-kill and all RTI processes preserved. StopVrf currently closes only the
   GUI and trusts 'Quit All Back-Ends' to carry the engine with it; that worked earlier
   the same day on an idle instance and did NOT work after a full run.
2. **ListenReports captured 0 bytes.** It ran the whole window and exited 0, but the
   capture file is empty. Either nothing was published to the topic it subscribes to, or
   it is not seeing the bus. Unresolved. Note it hardcodes 127.0.0.1 and takes no
   endpoint argument, so it cannot be pointed elsewhere to test.
3. **The CON stream is empty** - zero CON lines. The 0.6 console-capture work exists to
   surface VR-Forces' own per-unit warnings, and those warnings are a prime candidate for
   explaining WHY two units are frozen. Either no warnings were raised, or the callback
   is not wired on this path. Worth settling before the next run, because it may already
   contain the answer.

## 5. What this run does NOT establish

- NOT a 4a-scored acceptance result. sec 4a.6 makes run 1 a MEASUREMENT.
- NOT evidence about the 18.1-18.4 km stall band. Legs are ~556-578 m; the band cannot
  be reached at this scale.
*** SECTIONS 5 AND 6 BELOW ARE SUPERSEDED - RETRACTED 2026-07-19 LATE. The overturn banner
at the top of this file covers the movement reading only and did NOT reach here. "Duration
is not the binding constraint" and "a 600 s run will reproduce this, not fix it" are BOTH
FALSE for 1222.MechPlt: it was still moving and NOT SLOWING when observation
ended, and the route needs ~825 s against the ~145 s ever observed. THE RATIFIED NEXT
ACTION IS THE LONG RUN (-RunSecs 900+). The claim remains TRUE for the two frozen units.
Sec 6 item 1 ("rule on the 4a.2 amendment") is also stale - it was ruled, see sec 7. ***

- NOT evidence that a longer window would help. The two frozen taskees never moved at
  all, and the third stopped progressing after ~20 s and oscillated for 130 s. Duration
  is not the binding constraint. A 600 s run is expected to reproduce this, not fix it.
- NOT a de-confounded controller-class result. Echelon and controller class remain
  confounded exactly as the census said.

## 6. Next actions, in order

1. Rule on the 4a.2 amendment in sec 3 above.
2. Fix StopVrf's back-end teardown (graceful WM_CLOSE fallback, proven to work in 5 s).
3. Settle the empty CON stream - it may already explain the freeze.
4. Investigate the empty ListenReports capture.
5. Only then consider the 600 s run. On this evidence it will reproduce the same result,
   so it should be run to CONFIRM the freeze reproducibly, not in the hope of an arrival.

## 7. POST-RUN RE-SCORING under AMENDED 4a.2, and two corrections (2026-07-19)

AMENDMENT 1 to 4a.2 was ruled by the user and applied: MOVED now additionally requires the
distance to the FINAL WAYPOINT to have DECREASED by >= 25 m. Re-scoring this run:

| Taskee | dist to dest, first fix | dist to dest, last fix | closed | MOVED? |
|--------|------------------------|------------------------|--------|--------|
| 1222.MechPlt | 1155.5 m | 1218.9 m | **-63.4 m** | NO |
| 114.MechCoy  | 1112.0 m | 1112.0 m | 0.0 m | NO |
| 1.BdeHQ      | 1155.5 m | 1155.5 m | 0.0 m | NO |

All three fail, and 1222.MechPlt's only displacement took it 63.4 m FURTHER FROM its
objective. The amended rule reclassifies it from a misleading MOVED to a correct NO. Under
the ORIGINAL rule this run would have reported one unit as having moved.

### CORRECTION 1 - the ordered route is TWICE what sec 4a.0 said

The interface builds a THREE-point route, prepending the unit's current position:
"CreateRoute 'T_R5_PL1 ROUTE' (3 pts)" (vrfc2simapp.log). Measured from telemetry, each
unit starts almost exactly one leg away from waypoint 1 - 577.8 / 556.0 / 577.7 m - so the
FULL ordered route is ~1155 / ~1112 / ~1155 m, not the 556-578 m recorded in 4a.0. That
table has been corrected in place. This does not change any verdict here (all three are
0.0 m or negative progress) but it halves every percentage-of-route figure, and it means a
completion would require roughly twice the travel previously assumed.

### CORRECTION 2 - CREATION FIDELITY IS EXACT, and that is a real positive

The three taskees spawned at the init's coordinates to SIX DECIMAL PLACES:
    1.BdeHQ       init 34.608415817915,-116.712685404877  ->  actual 34.608416,-116.712685
    114.MechCoy   init 34.647628996814,-116.693387536163  ->  actual 34.647629,-116.693388
    1222.MechPlt  init 34.612955587412,-116.600486942341  ->  actual 34.612956,-116.600487
Ground clamp also works, but NOT at one altitude - the taskees sit ~10 km apart so their
terrain heights differ: 1.BdeHQ 1131.4 m, 114.MechCoy 1116.7 m, 1222.MechPlt 1040.6 m.
(The 1040.6 in the gate's firstRealLine is uuid 0279a70c, a 1222.MechPlt MEMBER, not a
taskee - do not generalise one member's clamp height to the group.) So the failure is NOT misplacement and NOT burial. The units are put
in exactly the right place, given the right route, and then do not drive it.

This tightens the diagnosis considerably. Ruled OUT by this run: wrong spawn position,
buried spawn, missing route, missing task issue, lying completion. What remains is
specifically the EXECUTION of an issued MoveAlongRoute by a created unit.

## 8. USER HYPOTHESIS TESTED: does the scenario inject behaviour? - REFUTED (2026-07-19)

The project owner asked whether the Bogaland scenario TropicTortoise was generated from
embeds a script that injects behaviour outside the C2SIM order, or picks entities with AI
capabilities that act independently. Tested by reading the archive bytes. REFUTED.

TropicTortoise.scnx, 11 members, all read:
- **.pln = 36 bytes: "( (Plan-File (version \"2.0\")) )" - ZERO PLANS.** This also CLOSES the
  ".pln plans unparsed" gap that groundwork 0.5 has carried since 2026-07-16. It was never
  a gap with content behind it; the member is an empty header.
- **.spt: <ScenarioScripts><count>0</count> - ZERO SCRIPTS.**
- **.gui_settings: SystemScriptsAvailable count=0.**
- .orb "(orbat )" empty; .ovl NumberOfStates 0; .sgr zero selection groups.
- .scn master carries "(auto-reorganize False)".
- **.oob defines exactly THREE objects** - GlblTerrDmg 1, GlobalEnv 1, Blocking Terrain
  Page-In Area 1 - and every one has an EMPTY task-status-list, an EMPTY
  suspended-task-list, empty engagement-zones, and "independently-tasked False". None is a
  movable platform.
- .xtr holds two stock spawn templates whose entity-plan fields are all USE-DEFAULT with
  EMPTY sink-nodes and spawn-points, so nothing can spawn. Its CRC is byte-identical to
  stock Rope_Demo.scnx - unmodified vendor default, not authored content.

BOGALAND EXISTS AND IS EQUALLY INERT. Bogaland2.scnx is on disk. Its .oob differs from
TropicTortoise in 47 trivial lines: three object-identifier renumberings and the page-in
area relocated from 58.5558N,16.1678E (Sweden) to 34.615N,-116.55W (Fort Irwin / NTC).
Identical uuids; identical CRCs on .ovl/.sgr/.osrx/.spt/.orb/.pln/.xtr. TropicTortoise IS
Bogaland2 relocated, and Bogaland2's own .pln and .spt are empty too.

NO DEFAULT BEHAVIOUR IN THE MODEL SET EITHER. C2simEx.sms -> EntityLevel.sms -> base.sms
has zero matches for doctrine/behavior/plan/reactive/task-set. C2simEx/vrfSim/taskRules/
and C2simEx/scriptedObjectMovement/ are BOTH EMPTY DIRECTORIES.
*** THIS IS TRUE BUT DOES NOT SUPPORT THE CONCLUSION DRAWN FROM IT - RETRACTED 2026-07-19.
C2simEx.sms includes EntityLevel.sms (line 86), and EntityLevel/vrfSim/taskRules/ holds
actionCategories.tsk, default-task-rules.tsk and doctrines.dct while
EntityLevel/scriptedObjectMovement/ holds 19 files. The wrong layer was checked. NOBODY HAS
OPENED THEM. *** The five Lua scripts under
C2simEx/scripts/ are DtScriptedTaskMetaData with myMenuLocations - operator menu-invoked,
no autostart.

CONCLUSION: nothing in the loaded scenario, its Bogaland ancestor, or the simulation model
set can task or move an entity. The four "independent movers" were not independent - two
executors independently identified them as the member entities of tasked unit
1222.MechPlt (sec 2). The hypothesis is refuted, but asking it is what produced the
identification and closed the .pln gap.

### Corrections to VRF_GROUND_TRUTH owed by this pass

1. The 0.4 gate recorded TWO baseline objects. There are **THREE** (GlblTerrDmg, GlobalEnv,
   Blocking Terrain Page-In Area).
2. RUNBOOK 0.5.7 says "the TropicTortoise baseline objects are POSITIONLESS; that is simply
   how they reflect". IMPRECISE. In the .oob, GlblTerrDmg and GlobalEnv sit at ECEF
   (6378137,1,1) - a null-island placeholder in the AUTHORED data. *** BUT NEITHER READABLE OBJECT REFLECTS ITS AUTHORED POSITION: FULL CENSUS (this correction previously named the wrong pair): d39a55ad GlblTerrDmg = 0 samples, NEVER reflects. f864e51f GlobalEnv = 1388 samples, 2 forms (NaN,-90,NaN and 0.0,-90,6.4e72), never 9e-6. cde66adc Page-In Area = 1390 samples, FOUR forms (90/-90/0.0, NaN/-90/NaN, 0.000001/-90/1.02e15, 0.000001/-90/6.4e72). THE TWO READABLE OBJECTS ARE GlobalEnv AND Page-In Area - both cast-corrupted. Corrected 2026-07-20. *** so authored-effectively-positionless as claimed. But
   the Page-In Area carries a REAL authored position (34.615N, -116.55W) and still reflects
   as lat=90/lon=-90, while GlobalEnv reflects NaN. Whether that is a WatchVrf decode fault
   or the correct HLA behaviour for non-entity control objects is NOT settled here - an
   area is not an entity and may legitimately not publish an entity state. Flagged, not
   diagnosed. It does not affect the 0.5.7 pre-check design, which already treats a
   degenerate pre-init result as expected.
