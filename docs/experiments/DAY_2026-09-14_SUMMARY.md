# Day 2026-09-14 summary (16:00Z - 2026-09-15 01:15Z)

Records snapshot of one working day on the parallel-lanes push to the demo. Live state lives in
docs/HANDOFF_2026-09-14_PARALLEL_LANES.md sec 1 and docs/PLAN_PARALLEL_LANES_2026-09-14.md - this
file is a fixed historical record, not maintained after today.

## 1. Runs since 16:00Z (11)

| Run dir | Purpose | Verdict |
|---|---|---|
| 20260914T164906Z | appData validation (run A): loadAllNavigationDataOnTerrainLoad=1, AtOrder, no settle | CORRECTED: 8 gate failures, the order reached the bus before the area row; the setting is a NULL RESULT |
| 20260914T165919Z | G8 (run B): gamewareMemorySize=128 | REFUSED 4/4; memory is NOT the lever |
| 20260914T170824Z | G7b (run C): useAbstractGraphs=true via the custom including SMS | THE FIX: 8/8 mesh-planned, 0 refusals |
| 20260914T172134Z | G8b (run D): gamewareQueryTimeBudget=50 ms | REFUSED again; time budget is NOT the lever |
| 20260914T185945Z | G7c: single-variable confirmation rerun | VOID: file cache cold again within the hour, nav area never registered, 31/31 gate-failed |
| 20260914T190751Z | G7c-gate: single-variable confirmation, ready gate first live use | CONFIRMED: 0 gate failures, 8/8 long legs planned, 32/32 mesh plans |
| 20260914T205046Z | N2b: AG override + slope-avoidance-factor 2.0 on the 1-35 ridge lane | MISS -> STOP: 0 points for every AtOrder member's goal at the destack start; 5th freeze |
| 20260914T215944Z | N2c: 300 s task-start delay (load-timing discriminator) | REFUSED identically; load timing FALSIFIED, the refusal is spatial |
| 20260914T224505Z | N2d: start moved 500 m along the leg (spatial discriminator) | MEASURED: short goals plan, long goals refused; drove the route 1.3 km north of the freeze - a LINE property |
| 20260914T230706Z | V2: 42-task COA-STP1 chain at DurationScale 0.05, merged build | PASS: 41/42 dispatched, 40 timed TASKCMPLT, 1 ruled TASKABRT cascade, 0 push failures; runner stop rule found broken |
| 20260915T001159Z | V13: reporting live gates + C16 watchdog in one run | PASS all five: Q4 refusal, TASKSTRT once/task, C16 fires/silent, stop rule closed the window at +184 s of 900 |

## 2. Commits landed on main, by lane

**L1 mesh-query / nav mesh (STP-788, STP-802/803/804/806):** N1 dropped, N2b/N2c/N2d designed and
run (f219d4e, 3a594be, 0f29442, 9373d32, 6f2810e, 871a1ec, d4d1180, dcb50dc, c75c079, f979a72,
0efc26b, fe17e7b, 0432b98, 9cf6eb2, 3786381, 2342357, 913ced0); the abstract-graph research record
(f54a616) and PREREG V7 on the MojaveAO20 terrain (ca2fc69); the pre-flight lateral route-shift
designed and built, OFF by default (e640010, 8652716, merge 96fa396).

**L2 reporting (STP-777):** PREREG V13 (e604e0d), V13 results - all five live gates HOLD (ac99ecd),
B7 heading/speed live PASS off V2's capture (e10d4fc).

**L4/L12 runner robustness (STP-789/794):** the stop-rule fix - per-task terminal-code counting,
the broken per-taskee regex named and fixed (ad4b191, merge ff15a4e); tools/PauseSim built as the
eleventh 5.2 consumer with -PauseAtSec/-ResumeAtSec for the Q5 probe (04c676f, merge f13df1b).

**L8 task vocabulary + rulings (task-vocab epic, STP-809):** Q5 ruling - a paused scenario holds
task time (f2794d7); review-pass-2 fix items B4/B6/B7 (df7f7e2, d792901, ba44d7e) and the A1/Q5
follow-ups (b76c9c7, 8db033e); pass-3 cold-start review (75bc1dc) and its fixes D1/D2/E1/E3-E7
(5f661f5, fc23f14, 2cbd722, 846de1d, c4f7785, 9a0a928, 1ffb4ee); STP-809 back-end control state
built and reviewed (c9576a0, f8aebc5, merge 4cd84d7); E3 (cyclic chains malformed) confirmed by the
user (fde99f9); the four 5.0.2-only tools converted to 5.2 consumers (20850f4, 61c2047, merge
b4fcf58); V4b embedded-location read per verb and per shape (4b9ad7c, 1cdafb6, merge 16995b3).

**L13 integration:** the sim-clock/reporting/heading-speed/preflight-port/tasking series merged to
main (0f4d09e); gate G-A - native bridge rebuilt, six 5.2 consumers at one hash (6f91feb, 4d5f59c).

**L5/L10 records + ledger:** 3c78533, e51e84f, 54558ba, 165e04c, 172e0ca, eadf91d, 33b3d85, dd99f09,
7afd9bf, 2bc8022, 70a2e40 (this task's HANDOFF/PLAN refresh).

## 3. Rulings received today (TASK_VOCABULARY_ASSESSMENT_2026-09-14.md sec 7.1)

- R1 RULED: the missing MapGraphicID is an STP export defect (IncludeMapGraphicIdInTasks was off),
  not ours to fix; the embedded Location stays supported alongside it (follow-up build V4b).
- R2 RULED: a task without geometry uses the geometry of the performing (who) unit - execute in
  place and report the derivation; never refuse, never silently invent a location.
- R3 RULED: the target IS the objective (doctrinal); VR-Forces' own tactical tasks agree.
- R4 RULED: completion = StartTime + Duration, on the SIMULATION clock (Q2); Q1 superseded ->
  TASKABRT + successors abandoned; Q3 wire ambiguity accepted; Q4 no Duration + no geometry =
  MALFORMED, refused with error + TASKABRT; Q5 a paused scenario holds task time while the back
  end reports; Q6 no interim stop-gap; Q7 rollback forward-only.
- User ruling 00:15Z: convert the four 5.0.2-only tools; keep probing the western abstract graph
  docs-first (the research record above is that probe).

## 4. Lessons recorded today

- LIVE READS ARE PROVISIONAL: two live mid-run conclusions were reversed by the harvest (a
  placement-vs-planning trigger; a Jira PASS posted on a grep for ": fail" while the sim logs
  "fail in action"); only the harvest reader's verdict goes to Jira/records.
- DOCS-FIRST RELAPSE: ten runs (two low-prior probes plus one wasted) happened before anyone read
  what a Gameware "abstract graph" is; vendor docs come before any probe, one confirming run per
  claim (feedback-docs-first.md).
- EXECUTOR REPORTS TO FILE: a reviewer's first TaskOutput arrived truncated with an empty task
  transcript; re-written to a file on request (REVIEW_RULINGS_8db033e_2026-09-14.md).
- WALL-DERIVED SPEED MISLEADS: the reported Speed is m/s of true SIM motion, not a wall-clock
  estimate - the reported/derived ratio tracks the sim ratio exactly (PREREG_V2_CHAIN_DS005, B7).
- A PARSER OF YOUR OWN LOGS NEEDS A FIXTURE TEST: the runner's completion regex anchored to the
  end of a log line; an unrelated app-side change appended text after it, and the parser matched
  NOTHING for eleven taskees before anyone noticed live (ad4b191).
- CHECK THE OUTPUT TREE, NOT THE EXIT CODE: four bridge-consumer tools built and exited 0 under
  -p:BridgeConfig=Release-5.2 while silently linking the 5.0.2 DLL, because their csproj lacked
  the BridgeConfig property (20850f4).
