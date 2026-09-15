# PREREG V13 - reporting live gates (V1) + the C16 progress watchdog ON (V3), one run on the merged build (written 2026-09-14 ~23:55Z, BEFORE the run)

Tier HEAVY (two live gates the demo depends on: STP sees the right codes; a frozen unit is reported instead of hanging).
Docs consulted: RUNBOOK sec 10 (C16 is OFF by default; enable by environment: Vrf__StallDetection=true, Vrf__StallClock=sim),
sec 11 (Q4 refusal; the seven R4 keys), REVIEW_RULINGS_8db033e sec 2.2 (Q4: refused BEFORE MarkDispatched, never a TASKSTRT;
successors cascade), sec 2.5 (one code per outcome; stall = TASKABRT and a later arrival TASKCMPLT is deliberate - the C16
ruling), REPORTING_ASSESSMENT_2026-09-14 (B1-B10), STP_PARSE_CHECK sec 3.6 (STP consumes PositionReportContent only - the
bus CAPTURE is the witness), vrf-stall-detection-by-design memory (the interface's own progress watchdog; the 1-35 freeze at
the P11 point is the known stall, six runs), V2 RESULTS (the vendor-failure cascade already seen).

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

## Harvest
validation/v2_harvest.py works unchanged for the codes and counts (the order file argument must point at the V13 order:
edit the ORDER path or pass it) plus a grep of the watchdog lines ("STALL", "progress") and the Q4 ERROR line; the ridge
scripts (n1n2/harvest) for 1-35's track.
