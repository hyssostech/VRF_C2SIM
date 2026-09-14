# PREREG V2 - COA-STP1 42-task STREND chain at Vrf:DurationScale = 0.05 on the merged build (written 2026-09-14 ~23:00Z, BEFORE the run)

Tier HEAVY (it decides whether the rulings build does on the bus what the 136-check suite says it does). Gate: PREREG (this).
Build under test: main >= 6f91feb (gate G-A pin: bridge 99B7B235..., six 5.2 consumers one hash, 18/18 suites, rulings 136/0).
Docs consulted: TASK_VOCABULARY_ASSESSMENT_2026-09-14.md sec 7.1a items 1-2, 5-6, 10 (what a live run must prove), sec 7.1b (A1
closed offline: 42/0 at scale 0.05 in 20 repetitions; E4 CHAIN DEPTH line; E6 figures 16,800 / 21,600 s), RUNBOOK sec 11 (the
seven R4 keys; gate windows), REVIEW_RULINGS_8db033e sec 2.1 (the windows at 0.05: dispatch at 0 / 360 / 600 / 840 s; T13 at 600,
T14 at 840), STP_PARSE_CHECK sec 3.6 (STP consumes only PositionReportContent - our bus CAPTURE, not STP, is the witness).

## The run
Launch line scratchpad validation/v2_launch.sh = n2b's line with: --order data/COA-STP1_Order.xml (42 tasks, 11 taskees),
--env Vrf__DurationScale=0.05, --run-secs 1500, --object-console 3 --member-console 1 (the vehicle consoles are not the
instrument here; the app log and reports-captured.log are), the AG_S2 fixture, COA-STP1 init, AtOrder + nav-area gate. Timed
completion ON (default), TaskClock sim (default), TaskPredecessorTimeoutSeconds 7200 (the demo floor), StallDetection OFF.
Scaled durations: PT2H -> 360 s, PT1H20M -> 240 s; T13 delay PT3H20M -> 600 s. Longest dispatch lead 840 s; last END 1,080 s
of TASK clock (= sim clock; N2b-N2d ran at ~1.0x, so ~1,080 wall s + the dispatch overhead). Window 1,500 s covers it.
ONE VARIABLE vs the N2 series: the order (42 tasks instead of 1) and the scale - everything else identical; this is a proof
run, not a comparison.

## Predictions (a missed HIGH is a STOP)
- **V2a (HIGH):** the app log prints ONE `CHAIN DEPTH:` INFO line for the order with lead 840 s and end 1,080 s (scale 0.05,
  backstop 86,400) and NO chain WARNING. MISS -> the E4 arithmetic differs live from the suite; STOP and read.
- **V2b (HIGH, THE POINT):** 42 dispatches, 0 skips: each of the 31 gated tasks logs its A1 gate line ("gated on ..., which IS
  a task in this order ... N s to DISPATCH ... then M s to COMPLETE") and the string "never dispatched within" appears NOWHERE;
  every task reaches MarkDispatched (42 TASKSTRT pushes). The 10 chain roots + T13 dispatch at 0 / 600 s; successors at
  ~360 / 600 / 840 s of task clock (+ the observation staircase, <= 3 s at 1.0x). MISS -> A1 fails live; STOP.
- **V2c (HIGH):** exactly one TASKCMPLT per task (42), each at its dispatch + Duration x 0.05 of SIM time (360 or 240 s),
  none before; no task ends on arrival before its time except where the vehicle reaches the goal first (record which). The
  bus capture (reports-captured.log; ListenReports incremental) shows 42 TASKSTRT and 42 TASKCMPLT for the 11 taskees; zero
  "push failed" lines. MISS on the count -> the single emit point or the capture is wrong; STOP.
- **V2d (MEDIUM):** the 9 zero-geometry tasks (assessment sec 4) dispatch IN PLACE (TASKSTRT, NameObservation, no vendor
  move), and NONE of the 42 is refused as malformed (all 42 carry a Duration) - the Q4 refusal count is 0.
- **V2e (RECORDED):** the TASK CLOCK start-up line names the SIMULATION clock; the SIM CLOCK lines report whether the reader
  alternated / went flat / stepped back (assessment item 8 - unobserved so far); how many taskees froze on ridge faces (the
  nav problem is not this run's question; timed completion must close them anyway).
- **V2f (RECORDED, B7):** the PositionReports carry HeadingAngle and Speed consistent with the trace-derived course/speed for
  a moving platform (sign and units) - harvest-only.
- **Runner:** StopWhenComplete fires when all 11 taskees / 42 tasks report TASKCMPLT (+ 60 s + position evidence) - the first
  live firing of the stop rule; if it does not fire, the window cap (1,500 s) closes the run and the reason is recorded.

## What counts as a STOP
Any HIGH miss; a crash of the interface or the sim; a "Tick phase ... FAILED (MissingMethodException)" line (a partial deploy -
gate G-A's own tripwire); more than one TASKCMPLT for a task; a TASKABRT for a task that had no cause line.

## Harvest
scratchpad validation/v2_harvest.py (to write after the run, read-only): per task - dispatch clock, TASKSTRT time, TASKCMPLT
time, expected end, delta; chain lines; CHAIN DEPTH line; refusal count; bus capture counts by code; the SIM CLOCK lines.
