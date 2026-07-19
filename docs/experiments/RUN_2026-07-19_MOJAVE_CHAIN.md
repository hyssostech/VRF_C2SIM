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

THE UNITS DID NOT MOVE. All three tasks were issued correctly by the interface and none
of the three taskees made meaningful progress toward its objective. This is the known
frozen-pile class, now reproduced HEADLESSLY with a clean trace and a pre-registered
criterion - which is the first time it can be measured rather than argued about.

## 1. What ran (stage by stage, all verified from the run artifacts)

| Stage | Result |
|-------|--------|
| LaunchVrf | EXIT=0 READY, ~50 s, back-end healthy |
| Oracle pre-check (advisory) | 28 POS lines, ALL degenerate. EXPECTED - stock TropicTortoise baseline objects are positionless (RUNBOOK 0.5.7) |
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

### A finding that inverts the naive reading

Five objects in the trace showed displacement. ONLY ONE of them (ec196214) is a taskee.
The other four - 82fe0939, 0279a70c, fbab58c5, ceffff34 - were NOT tasked. Meanwhile two
of the three objects that WERE tasked sat at exactly 0.0 m.

Those four non-taskees show the census's lockstep signature: motion beginning at the
identical instant (t=32 s for four of them) and identical max step speeds (111.7 km/h for
three of them). 82fe0939's entire "displacement" is two discrete jumps - 49.6 m at t=31.6
and 113.2 m at t=39.7 (**204 km/h, above the census 200 km/h warp threshold**) - followed
by exactly 0.0 m for 140 consecutive seconds. Under 4a.4 these are candidate artifacts;
they do not return to the pre-jump track, so by the letter of 4a.4 they classify as
PERSISTENT, but at 60-70 m they are nowhere near the runaway class.

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
Ground clamp also works: altitude resolved to ~1040 m terrain height rather than the
requested spawn MSL. So the failure is NOT misplacement and NOT burial. The units are put
in exactly the right place, given the right route, and then do not drive it.

This tightens the diagnosis considerably. Ruled OUT by this run: wrong spawn position,
buried spawn, missing route, missing task issue, lying completion. What remains is
specifically the EXECUTION of an issued MoveAlongRoute by a created unit.
