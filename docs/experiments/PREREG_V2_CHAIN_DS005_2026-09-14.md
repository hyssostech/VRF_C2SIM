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

## RESULTS - run 20260914T230706Z (launched 23:07Z; order pushed 23:09:49Z at the ready gate; window ran to its 1,500 s cap; teardown clean, RTI preserved)

Harvest: scratchpad validation/v2_harvest.py over vrfc2simapp.log (4,257 lines) and reports-captured.log (19,917 reports:
19,712 PositionReport, 121 Observation, 84 TaskStatus). No vendor log opened.

| prediction | verdict | evidence |
|---|---|---|
| V2a CHAIN DEPTH 840 / 1,080 s, no WARNING | **HOLD** | one line: "dispatches its last task 840 s of TASK CLOCK after order receipt and is armed to end at 1080 s (Vrf:DurationScale=0.05); backstop 86400" |
| V2b 42 dispatches / 0 skips; 31 gate lines; no "never dispatched within" | **HOLD for the gate, 41 of 42 dispatched** | 31 A1 gate lines, "never dispatched within" 0 times, 41 TASKSTRT. The one non-dispatch is T26 (1-1), skipped because its predecessor T25 was ABORTED by VR-Forces itself: "VRF task complete: 1-1/2/1_AD~PXY / patrol-route (success=False)" -> TASKABRT (B7's success flag, live for the first time) -> "predecessor ... was skipped/abandoned upstream; policy=skip ... NOT dispatched" -> T26 TASKABRT. That is the ruled cascade, not a gate failure. Cause of the vendor failure unknown (member consoles at 1 for this run); recorded. |
| V2c one TASKCMPLT per task at Duration x 0.05 of SIM time | **HOLD (40 of 40 completable)** | 40 TIMED COMPLETION lines, each "360 s of a 360 s Duration" or "240/241 s of a 240 s Duration served on the simulation clock"; 40 TASKCMPLT on the bus, none doubled; the roots' wall span 180 s for 360 sim s and 120-167 s for 240 sim s = a 1.4-2.1x sim ratio under 11 taskees; 0 push failures |
| dispatch times 0 / 360 / 600 / 840 s of task clock; T13 after its 600 s delay | **HOLD** | roots at 23:09:49-50; second wave at 23:12:49-50 (+180 wall = 360 sim); third at 23:14:50-52; fourth at 23:17:20-21; T13 "start delay 600 s (order says 12000 s; Vrf:DurationScale=0.05)" dispatched 23:14:32 (+282 wall), T14 at 23:16:57 |
| V2d 9 in-place dispatches, 0 MALFORMED | **HOLD** | 9 hold-in-place dispatches, 0 MALFORMED |
| V2e clock behaviour (recorded) | RECORDED | TASK CLOCK line names the SIMULATION clock; "SIM CLOCK: stepped BACKWARDS" 3x: 2.6 -> 0.2 s at the start (the scenario clock reset) and two ~2 s steps (754.4 -> 752.4, 970.9 -> 969.2), each classified RolledBack, "no task is completed by it and no wait is restarted" - the FIRST live observation of the assessment's item-8 hazard; it is small and Q7's forward-only axis absorbed it. 24 "SUPERSEDES in-flight task" lines: each chained successor replaced the still-driving VR-Forces move of its timed-completed predecessor - expected for units that had not arrived; no TASKABRT from them (the old C2SIM task had already TASKCMPLT'd; B1's abandon is on the TASKABRT branch only) |
| V2f B7 heading/speed | OWED | harvest-only, from this capture (19,712 PositionReports) |
| G-A tripwire | **HOLD** | 0 "Tick phase FAILED", 0 MissingMethodException |
| Runner StopWhenComplete fires | **MISS - RUNNER DEFECT, not the build** | it reported "Taskees without TASKCMPLT: <all eleven>" although every taskee has TASKCMPLT lines in both sources, and its criterion "TASKCMPLT lines >= task count" cannot be met when a task legitimately ends in TASKABRT. Fix dispatched 23:35Z (fix/runner-stop-rule: count terminal codes per task; fix the per-taskee detection; name the failing condition) |

**Adversarial review.** Strongest competing reading of V2b: "the chain only worked because every predecessor completed by TIME, so nothing tested the gate against a real completion" - true and intended (R4: completion is given by the end time); the gate's job is to release on the predecessor's end, and 31 of 31 gated tasks released at their predecessor's scaled end (+0-2 s). Competing reading of V2c: "the completions are wall-clock timers at a 2x ratio, not sim-clock" - refuted by the lines themselves ("served on the simulation clock") and by the wall spread: identical 360-sim-s Durations took 180 wall s in the first wave but the 240-sim-s ones took 120 to 167 wall s across waves as the sim ratio drifted; wall timers would not drift with the ratio. Unexplained and carried: WHY VR-Forces failed 1-1's patrol-route (T25) - the vendor's own failure, reported correctly; a level-3 member-console rerun of 1-1 alone would show it. VERIFIED: everything in the table from the run's own files. ASSUMED: nothing beyond the sim ratio, read from wall spans not from the trace (object console 3 emitted no sim-prefixed controller rows).

**Verdict:** the rulings build does on the bus what the 136-check suite says it does. The chain, the timed completions, the in-place dispatches, the vendor-failure cascade and the report path all behaved as ruled; the only defect found is the runner's stop rule.
