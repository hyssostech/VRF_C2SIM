# HANDOFF 2026-09-14 pm - archive

Verbatim material collapsed out of docs/HANDOFF_2026-09-14_PARALLEL_LANES.md on 2026-09-14 pm to
hold that file under its 200-line cap. Nothing here is a new claim; every block is an exact copy of
text that used to live in the entry doc, kept so no settlement was lost in the collapse. This file
has no line cap of its own. ASCII + CRLF, per repo convention.

## sec 1 - Early stops sec 6-7d, full recap (was HANDOFF section 1, "Early stops")

Early stops (docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md secs 6-7d - point at it, not restate):
- sec 6 VENDOR PASS: a vehicle that stops while its task still runs is UNDETECTED BY DESIGN in 5.2
  (the base give-up test "always returns false"; the sample hands that test to the integrator).
- sec 7 CAUSE, SUPPORTED for the units that freeze ON a scored face (1-35, 4-27): seven consecutive
  8 m postings rising at 0.70-0.95 over 55 m on sand ahead of the freeze; the sampler reproduces the
  sim's own reported altitudes to ~3 cm.
- sec 7b SCOPE: this is a claim about those units, NOT about "the early stops". A SECOND mechanism is
  active (1-6 stopped on DESCENDING ground; 856/HHC on FLAT ground). Pre-flight calibration as
  corrected: 40 m window, threshold 0.92, margin 0.097, 3/3 caught, 0/6 false alarms - one order,
  one terrain, three positives.
- sec 7c it is the LEADER that freezes; followers crawl past it. The console excludes blockage,
  replan, give-up and goal change - it does NOT exclude formation speed control.
- sec 7d a THIRD stop kind: a move-along subtask FAILED ("Entity not embarked on same object as
  target"), VR-Forces re-formed the unit, and the task-less hull was PUSHED 1,994 m by the vehicle
  behind it. Two consequences that bind: (a) a vendor task FAILURE produces NO C2SIM report today
  (reporting item B1); (b) in a collapsed-ratio capture the observer's dead reckoning draws sawtooth
  motion - CHECK THE ALTITUDE RESIDUAL before reading any excursion as movement (G2's post-collapse
  tail is withdrawn as motion).

## sec 2 - Navigation mesh, full recap (was HANDOFF section 1, "Navigation mesh")

Navigation mesh (PREREG_NAVDATA_G6 sec 5 = STOP; MESH_QUERY_VS_DISTANCE_2026-09-14 sec 0):
- G6 STOPPED on its instrument gate: the 41 x 54 km area loaded, BOTH nav-area gates returned
  success, and vrf:findPathToLocation returned "not enough (0) points" for every multi-kilometre
  goal. G6 is therefore a FOURTH reproduction of the stops, not a discriminator.
- The dependence is on the AREA, not on goal distance: 10,131.6 m planned twice on the 1,600-sector
  MojaveAO20, 0 points on the 8,856-sector MojaveCOA for BYTE-IDENTICAL destinations; 22 pairs flip
  that way, none the other way. Refusals return in 0.4-1.3 s, FASTER than the 2.4-4.2 s long
  successes - a refusal, not an exhausted search. Frame correction: G1 ran on MojaveAO20; there
  never was a 3 x 3 km COA area.
- The generator accepted a 41 x 54 km area: UG52's 20 x 20 km is a GUI default, not a generator limit.
- G6/G7 CLOSED 2026-09-14 17:15Z: the refusal was the vendor script's hard-coded
  useAbstractGraphs = false (ground-vehicle-move-to.lua:488), not an area-size or memory/budget
  limit. The custom including SMS (C:\C2SIM\vrf-sms) planned the 10-seam, 4,989 m leg 8/8 with 0
  refusals (G7b); gamewareMemorySize and gamewareQueryTimeBudget are NOT the lever (G8/G8b still
  refused on the stock SMS). Record: docs/experiments/G7B_G8_RESULTS_2026-09-14.md (in progress);
  OPEN: whether abstract-graph paths avoid the ridge faces (FINDING_EARLY_STOPS).

## sec 3 - Harness / runner-exit-127, full recap (was HANDOFF section 1, "Harness")

Harness (docs/experiments/RUNNER_EXIT127_2026-09-14.md; RUNBOOK 0.5.14):
- "runner exit: 127" does NOT mean "command not found". On this MSYS bash it is a high-bit Windows
  exit code; the only SILENT one is 0xFFFFFFFF = TerminateProcess(-1) = Stop-Process / Process.Kill.
  127 + a silent log + no WER event = THE RUNNER WAS KILLED FROM OUTSIDE. The killer is unidentified
  (no process auditing on this machine). The G6 capture was COMPLETE; "trace: (no samples)" was a
  separate tail-read defect, fixed. NO Stop-Process / taskkill sweeps while a run window is open.

## sec 4 - Lane table, full per-lane state as of 2026-09-14 ~13:00Z

(was HANDOFF section 2's "State 2026-09-14 ~13:00Z" column; superseded by the live
docs/PLAN_PARALLEL_LANES_2026-09-14.md and, within the entry doc, by section 1 where later.)

| Lane | What it is | State 2026-09-14 ~13:00Z | Jira |
|---|---|---|---|
| L1 | Mesh-query refusal | attempts 1 and 2 VOID (tripwires above); ATTEMPT 3 (tank platoon 1222.MechPlt) launched 13:04Z as runs/20260914T130439Z_run - verdict PENDING, PREREG_MESHQUERY_G7 sec 5 | STP-788 epic; STP-790/791/792/793 |
| L2 | Reporting | assessment done (11 gaps, B1-B10); B5/B3/B1/B2/B8 built on feat/reporting | STP-777 epic; STP-778..787 |
| L3 | Watchdog + sim clock | feat/sim-clock f052ea7, reviewed to pass 4, supervisor-verified; MERGE AFTER G7 | STP-783 |
| L4 | Runner robustness | landed on main (f6e68d9); first live use = the G7 attempts | STP-789 epic; STP-794 |
| L5/L10 | Records | this file + docs/DEMO_RUNBOOK.md (readiness row 13) | - |
| L6 | User-owned | licence renewal; DI-Guy package; TASKABRT decided; C:\MAK sanctions given | - |
| L8 | Task vocabulary beyond MOVE | assessment running (docs-first) | epic created 2026-09-14 |
| L9 | STP-side strict parse check | running (offline console test) | STP-615 |
| L11 | 4-27 second read + offset-line scoring | running | - |
| L12 | A2 detached run watchdog | running (scripts only) | STP-794 |

## sec 5 - Licence, superseded 2026-09-15 lapse warning (was HANDOFF section 4)

(Superseded 2026-09-14 pm: the licence was renewed to 2026-10-31, RUNBOOK 0.5.15, c8730e7. The
text below is kept verbatim as the record of what this doc said before that.)

- LICENCE: the node-locked DEMO licence LAPSES 2026-09-15. It gates the whole toolchain, including
  vrfNavGenerator. The renewal is user-owned and the download was still owed at 13:00Z.

## sec 6 - Branches and what each needs to land, as of 2026-09-14 ~17:30Z (was HANDOFF section 2)

(Superseded 2026-09-14 20:30Z: sim-clock, reporting, heading-speed and preflight-port all
landed via feat/integration @ 26efe0c; only feat/tasking-rulings remains to land, per
section 1's state paragraph. Kept verbatim as the record of what this doc said before that.)

BRANCHES AND WHAT EACH NEEDS TO LAND (worktrees under .claude\worktrees\; `git worktree list`):
- feat/sim-clock @ f052ea7 - the C16 progress watchdog (report-only, DEFAULT OFF) measured on the
  back end's scenario clock. Reviewed until clean (pass 3 = REVIEW3_SIMCLOCK_08146a2; pass-4 fixes
  verified: 66/66 stall checks, wall path identical to 51d78a5 on ten feeds, Decide byte-identical).
  Needs: the merge, then the validation run below.
- feat/reporting @ f0d1c68 - B5 (sniff the inbound root: no more false SDK deserialize ERROR), B3
  (find the created object when VR-Forces truncates its name), B1 (TASKSTRT at dispatch, TASKABRT
  for tasks that will never run), B2 (never lose a report silently), B8 (re-announce a substitution),
  plus one review fix. Branched off 08146a2, so it carries part of the sim-clock series with it.
  Needs: a cold review pass, the merge, then its live gates.
- feat/heading-speed (B7, heading + speed in the reports; native) and feat/preflight-port @ 7672957
  (B4, the calibrated leg scorer ported into the interface at order receipt - the Python tool stays
  test-harness only; the product contains no Python). Both branched off f052ea7; both worktrees are
  locked while their executors run.
- MERGE ORDER: sim-clock, reporting, heading-speed, preflight-port.
- AFTER THE SIM-CLOCK MERGE THE NATIVE DLL MUST BE REBUILT AND REDEPLOYED. main's VrfFacade /
  VrfBridge have NO SimTimeSeconds (verified: the symbol exists only on the branch), so the merged
  managed code cannot read the scenario clock until the C++ is rebuilt (/t:Rebuild always; back up
  the DLLs; redeploy all 7 copies). Re-pin the deployed build for the next run afterwards.

## sec 7 - Next steps items 1-2, as of 2026-09-14 17:30Z (was HANDOFF section 5)

(Superseded 2026-09-14 20:30Z: the nav-mesh question these items awaited is now CLOSED
(section 1's CLOSED list) and the four branches in item 2 all landed via feat/integration.
Kept verbatim as the record of what this doc said before that.)

1. G7 ATTEMPT 3 VERDICT (L1). Harvest against PREREG_MESHQUERY_G7 sec 3 and fill sec 5: per leg -
   seams / gate current / gate destination / mesh outcome / N points / gate-to-outcome seconds /
   vertex reached. Points on leg 1 AND 0 on leg 3 -> the graph IS connected across seams and the
   failure is length or budget -> G7b (the abstract-graph SMS, fixture _AG), then G8
   (gamewareMemorySize via --appDataDir). 0 points on leg 1 -> the generated data is unusable across
   seams, the "area size" reading is WITHDRAWN and the generation becomes the suspect.
2. MERGES + REBUILD + DEPLOY: sim-clock, then reporting (after its cold review), then heading-speed,
   then preflight-port; rebuild and redeploy the native VrfBridge / VrfFacade after the sim-clock
   merge (section 2), then re-pin the deployed build.

## sec 8 - State at 2026-09-14 20:30Z, superseded (was HANDOFF section 1, State paragraph)

(Superseded 2026-09-15 01:00Z by the new State paragraph in HANDOFF section 1. Kept verbatim
as the record of what this doc said before the 01:00Z refresh; nothing here is a new claim.)

**State at 2026-09-14 20:30Z:** main is at 5047114 (ledger: appNo claims for the G7c and G7c-gate
runs, 185945Z/190751Z). feat/integration sits at 26efe0c (tasking foundation V2/V3 merged, 10
suites green, native rebuilt in the worktree, gate G-B PASS) and feat/tasking-rulings at 8db033e
(fix pass 2 LANDED: A1 chain gate - COA-STP1 42 dispatches / 0 skips offline, deterministic; A2,
B1, B4, B6, B7; Q4 and Q5 built; 18 suites, rulings suite 112 checks; pass-3 cold-start review
RUNNING before merge; follow-up STP-809 filed - the facade must expose back-end status
Paused/Playing/gone so a dead back end does not freeze task time under Q5). The day's 11 runs,
one line each: G7 attempt 1 VOID (a blank line killed the STOMP pump, STP-795); attempt 2 VOID (a
lone platform never enters the Lua planner); attempt 3 VOID (AtOrder tasked members ~175 s before
the area was recognised); attempt 4 MEASURED 15:42Z (legs 1-2 mesh-planned 4/4, the 10-seam leg 3
refused 4/4 - a length ceiling, AREA-dependent); appData validation (164906Z) CORRECTED (8
current-point gate failures; loadAllNavigationDataOnTerrainLoad=1 is a NULL RESULT); G8 (165919Z)
refused 4/4 at gamewareMemorySize=128 - memory is NOT the lever; G7b (170824Z) planned the 4,989
m / 10-seam leg 8/8 with 0 refusals via the custom including SMS - THE FIX; G8b (172134Z) refused
again at gamewareQueryTimeBudget=50ms - time budget is NOT the lever; G7c (185945Z) VOID (cache
cold again within the hour, nav area never registered, 31/31 gate-failed); G7c-gate (190751Z)
CONFIRMED the custom SMS single-variable (0 gate failures, 8/8 long legs planned, 32/32 mesh
plans). MERGED 22:05Z: rulings 1ffb4ee (pass-3 fixes, suite 136/0) -> integration -> main 0f4d09e (46 src
files). G-A 22:45Z (6f91feb) built+pinned; V2 LIVE 23:09Z proved the chain (PREREG_V2 RESULTS); runner stop rule FIXED+merged ff15a4e; STP-809 merged 4cd84d7 (pin STALE, G-A rerun owed); V13 00:15Z PASSED all five (Q4 refusal, TASKSTRT, C16 fires/silent, stop rule closed at +184 s); tools52 MERGED b4fcf58 (join gate owed); V4b MERGED 16995b3 (suite 176/0); lanes out: pausesim, route-shift, AG research; N2b (abstract-graph SMS +
slope-avoidance-factor 2.0, fixture _AG_S2, 1-35 lane) is next on L1, replacing the paused ridge
test (N1 dropped 20:40Z: propagation-box-extent is a dead parameter for vehicles)
(NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE_2026-09-14.md). The user's rulings are ALL IN: task-vocabulary
Q1-Q7 (20:00Z) alongside the earlier R1-R6, TASKABRT (row 19), pre-flight-as-warnings (row 20),
and the C:\C2SIM homes (row 21). The MAK licence is RENEWED to 2026-10-31 (RUNBOOK 0.5.15;
c8730e7). METHOD LESSON for memory: a docs-first relapse - L1 drifted back to probing (the paused
ridge test) before the Gameware Navigation docs were read; the user's 2026-09-14 correction
("drift back to probing") produced the 20:00Z docs pass that redirected L1 to N2b. The
2026-09-15 lapse warning stays superseded (archived, section 4).
