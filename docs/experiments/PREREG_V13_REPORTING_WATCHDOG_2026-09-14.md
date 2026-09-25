# PREREG V13 - reporting live gates (V1) + the C16 progress watchdog ON (V3), one run on the merged build (written 2026-09-14 ~23:55Z, BEFORE the run)

Tier HEAVY (two live gates the demo depends on: STP sees the right codes; a frozen unit is reported instead of hanging).
Docs consulted: RUNBOOK sec 10 (C16 is OFF by default; enable by environment: Vrf__StallDetection=true, Vrf__StallClock=sim),
sec 11 (Q4 refusal; the seven R4 keys), REVIEW_RULINGS_8db033e sec 2.2 (Q4: refused BEFORE MarkDispatched, never a TASKSTRT;
successors cascade), sec 2.5 (one code per outcome; stall = TASKABRT and a later arrival TASKCMPLT is deliberate - the C16
ruling), REPORTING_ASSESSMENT_2026-09-14 (B1-B10), STP_PARSE_CHECK sec 3.6 (STP consumes PositionReportContent only - the
bus CAPTURE is the witness), vrf-stall-detection-by-design memory (the interface's own progress watchdog; the 1-35 freeze at
the P11 point is the known stall, six runs), V2 RESULTS (the vendor-failure cascade already seen).
CORRECTION 2026-09-25: this is a SUPERVISOR reading (2026-09-13), not an owner ruling - RL-20260914-01 covers only the TASKABRT code; see its scope note. The current TEMPORARY completion position is RL-20260921-09.

## The run
scratchpad validation/v13_launch.sh = n2b's line with: --order data/PROBE_V13_REPORTING_WATCHDOG_Order.xml (3 tasks:
T1 = 1-35's ridge leg, verbatim; T23 = 1-1's COA task, verbatim, a chain root that moves normally; T_V1_MALFORMED_1-1 = no
Duration, no geometry), --env Vrf__StallDetection=true Vrf__StallClock=sim Vrf__DurationScale=0.1 (PT2H -> 720 s), --run-secs
900, member consoles 3, AG_S2 fixture, COA-STP1 init, AtOrder + nav-area gate, --stop-when-complete (the FIXED rule, first
live use). Two taskees (1-35, 1-1), three tasks.

## Predictions (a missed HIGH is a STOP)
- **V13a (HIGH, Q4):** the malformed task is refused at order receipt: an ERROR naming both missing elements, exactly one
  TASKABRT for it on the bus (ReportingEntity 1-1, CurrentTask 7c1f0a52...), NO TASKSTRT for it, before any other report of
  1-1's; 1-1's T23 still dispatches (the malformed task is not its predecessor).
- **V13b (HIGH, B1):** TASKSTRT for T1 and T23 at dispatch, once each; position reports for both taskees throughout;
  zero push failures.
- **V13c (HIGH, C16 fires on the freeze):** 1-35 freezes at the P11 point (the seventh time; s ~ 1,970 m, by sim ~300-400 at
  ~7 m/s) and the watchdog reports ONE TASKABRT for T1 with the stall reason (progress below threshold over the window) BEFORE
  T1's timed end at 720 s; the TIMED COMPLETION line for T1 then says its end time was cancelled by the TASKABRT (as in V2's
  T25). MISS (no stall TASKABRT by sim 720, T1 completes by time) -> STOP; read the watchdog's own lines (threshold, window,
  clock) before anything else.
- **V13d (HIGH, C16 silent on a mover):** 1-1's T23 gets NO stall TASKABRT; it completes by TIME at 720 s of sim (its
  route is far longer than 720 s of driving) with one TASKCMPLT.
- **V13e (runner, the fixed stop rule):** StopWhenComplete CLOSES the window: 3 tasks terminal (1 TASKCMPLT + 2 TASKABRT), both
  taskees covered, the 60 s hold, position evidence -> "closed: 1 TASKCMPLT + 2 TASKABRT = 3 terminal of 3 tasks" well before
  the 900 s cap. If it does not close, the line must name the blocking condition with numbers.
- **V13f (RECORDED):** what the sim clock does (SIM CLOCK lines), the watchdog's measured progress numbers at the freeze, and
  the six 1-35 member consoles at level 3 for the freeze (P20d crawl or not).

## STOP conditions
Any HIGH miss; a TASKSTRT for the malformed task; two terminal codes for one task other than the deliberate stall-TASKABRT +
later-arrival-TASKCMPLT pair; a crash; Tick-phase / MissingMethod lines.
CORRECTION 2026-09-25: this is a SUPERVISOR reading (2026-09-13), not an owner ruling - RL-20260914-01 covers only the TASKABRT code; see its scope note. The current TEMPORARY completion position is RL-20260921-09.

## Harvest
validation/v2_harvest.py works unchanged for the codes and counts (the order file argument must point at the V13 order:
edit the ORDER path or pass it) plus a grep of the watchdog lines ("STALL", "progress") and the Q4 ERROR line; the ridge
scripts (n1n2/harvest) for 1-35's track.

## RESULTS - run 20260915T001159Z (launched 00:12Z; order on the bus 00:14:43.6Z; window CLOSED EARLY by the stop rule at +184 s of 900; teardown clean, RTI preserved)

Build: the G-A pin (bridge 99B7B235; main 165e04c's binaries) - STP-809 was merged to main during the run but is not in the deployed
tree; the runner was main's, with the fixed stop rule (ff15a4e). Harvest: app log (97k lines, member consoles 3), reports-captured.log
(2,944 PositionReports, 104 Observations, 5 TaskStatus).

| prediction | verdict | evidence |
|---|---|---|
| V13a Q4 refusal at receipt, one TASKABRT, no TASKSTRT | **HOLD** | "Task 'T_V1_MALFORMED_1-1' is MALFORMED and will NOT be executed: the order gives it NO Duration ... AND NO geometry"; bus: TASKABRT 7c1f0a52 at 00:14:44.490, BEFORE both TASKSTRTs (00:14:44.808/.809); no TASKSTRT for it; 1-1's T23 dispatched anyway |
| V13b TASKSTRT once per dispatched task, position reports, no push failures | **HOLD** | TASKSTRT cd589832 (1-35) and 468c0325 (1-1) once each; 2,944 position reports for both taskees; 0 push failures; 0 Tick-phase / MissingMethod |
| V13c C16 fires on the freeze before the timed end | **HOLD** | start-up: "PROGRESS WATCHDOG ON (C16, report-only): a 360 s no-progress window on the SIMULATION clock, 50 m of net displacement per member, sampled every 5 s"; then "STALL: unit 1-35/2/1_A~PXY task T1: no member moved more than 50 m in the last 360 SIM s (max 40.4 m); TASKABRT reported" -> bus TASKABRT cd589832 at 00:16:41.8 (+117 s wall after dispatch, ~sim 690 at the ~6x ratio); "TIMED COMPLETION: the end time armed for task cd589832 is cancelled - TASKABRT reached the reporting point first" - the stall beat the 720 s end by a few sim seconds (the seventh freeze at the P11 point, by design of this order) |
| V13d C16 silent on the mover; TASKCMPLT by time | **HOLD** | 1-1's T23: no STALL line; "TIMED COMPLETION: task 'T23...' reached its END TIME - 740 s of a 720 s Duration served on the simulation clock (x 0.1)"; one TASKCMPLT 468c0325 at 00:16:55.4. The 20 s overshoot = the 1 wall-s sampling staircase at a ~6x sim ratio (up to ~6 sim s per sample, three samples) - recorded, within the C3 margin |
| V13e the fixed stop rule closes | **HOLD - FIRST LIVE CLOSURE EVER** | progress lines "1 TASKABRT = 1 terminal of 3 tasks" (+6 s) -> "2 TASKABRT" (+90 s) -> "1 TASKCMPLT + 2 TASKABRT = 3 terminal of 3 tasks" (+106 s); "ALL taskees and ALL tasks have a TERMINAL report at t+116s"; "settle hold of 60s elapsed (64.6s) and position evidence in - closing the observation window EARLY at t+184s ... of the 900s cap" |
| V13f recorded | RECORDED | TASK CLOCK on the SIMULATION clock; no SIM CLOCK backwards step this run; the watchdog's numbers above; 1-35's six members at level 3 (freeze track not re-harvested - the seventh repeat adds nothing) |

**Minor finding, recorded as debt (D-V13-1):** the malformed task's performer was MATERIALIZED at order receipt ("MATERIALIZE 1-1/2/1_AD~PXY
(task 'T_V1_MALFORMED_1-1' performer): shell deleted; re-creating as the TEMPLATE Tank Platoon with its members") BEFORE the Q4
refusal - harmless here (1-1 had T23), but an order whose only task for a unit is malformed would still create that unit's members.
Fix: run the Q4/E3 refusal walk before MaterializeUnit, or skip materialisation for a task the walk refuses. Small; not blocking.

**Adversarial review.** "The stall abort is just the timed end arriving early" - no: the STALL line names the measured 40.4 m max
displacement over 360 sim s and the cancel line says the abort reached the emit point first; a timed end would have printed the
TIMED COMPLETION task line, which exists only for T23. "The stop rule closed on the malformed abort alone" - no: it reported 1/3,
2/3, 3/3 in order and waited for the third. "The refusal happened after a TASKSTRT" - refuted by the bus timestamps (0.3 s apart,
ABRT first). Unexplained: nothing; the seventh freeze is the known lane. VERIFIED: all from the run's files. ASSUMED: the sim ratio
(~6x) from the wall spans; not read from the trace.

**Verdict:** reporting live gates (B1 start, Q4 refusal, one code per outcome, position stream, no push loss) PASS; the C16
watchdog PASSES both halves (fires on the frozen unit, silent on the mover) with its calibrated sim-clock defaults; the runner's
stop rule is live. Remaining live gates: V5 (Q5 pause + kill) after the pause tool and the G-A rerun.
