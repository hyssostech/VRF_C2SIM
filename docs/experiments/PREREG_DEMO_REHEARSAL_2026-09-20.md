# PREREG - DEMO REHEARSALS (2026-09-20, seat). Written BEFORE any rehearsal runs.

Goal (user 2026-09-20): get to the demoable deliverable. DEMO_RUNBOOK sec 10 lists what was never rehearsed;
each rehearsal below closes one of those lines. One run per claim; a missed HIGH prediction is a STOP.

## D1 - Way A with the GUI, verbatim from DEMO_RUNBOOK sec 1

Command: scripts/RunScenario.sh --gui --scenario R9_Mojave_Empty_52 --init data/R9_Mojave_Lean_Initialization.xml
--order data/R9_Mojave_UnitMove_Order.xml --client-id STP --object-console -1 --member-console -1 (plus --log).
Main 259082d or later: Stage 2h federation holder (STP-825), bridge pin 90272BC9 (STP-832), STP-833 route-extent
check ON, STP-837 traversal-based arrival, STP-822 liveness ON, WS tripwire abort at 3 alerts.

Predictions:
- HIGH: the holder joins, vrfLauncher + the GUI front end + the back end all JOIN (no create failure), READY.
- HIGH: 6 units created from the lean init and visible; 3 TASKSTRT; nothing refused by STP-833 (legs 557-578 m).
- HIGH: the three tasks complete with VENDOR completions; under STP-837 no task closes on arrival evidence before
  its members have travelled >= half the route (the order's routes are out-and-away, so arrival evidence may still
  close the company/entity tasks - but only after traversal). First live exercise of STP-837.
- HIGH: no BACK END LOST, no WS RUNAWAY abort, teardown clean (GUI and back end closed, rtiexec + holder left).
- MEDIUM: the fixture's frame mode makes the move visible to an audience (if it is fixed-frame-run-to-complete
  the 580 m legs finish in ~20-30 wall seconds - too fast to watch; then readiness row 10's REAL-TIME fixture is
  the next work item, not a failure of D1).
- UNKNOWN (observe, do not predict): whether the GUI raises any modal (licence, terrain download, session
  dialog) that blocks an unattended start. Any modal = a finding for the runbook, and a STOP if it blocks READY.
MISS = any startup failure, any task not terminal, any leftover process after teardown.

## D2 - reset between runs without restarting VR-Forces (DEMO_RUNBOOK sec 6) - registered after D1's harvest.
## D3 - Way B, hand-started and STP-driven, with the LaunchVrf52 holder (lane feat/demo-federation-holder).
## D4 - the audience scenario (COA-STP1's 11 taskees, GUI, real-time, route shift) - needs the user's rulings first.
