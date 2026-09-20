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

### D1 RESULT (run 20260920T172141Z, harvest 2026-09-20)

Verdicts (sec 0): 1 HIT (holder+back end+GUI joined, no FOM Reader error, READY; vrfLauncher
clause NOT-MEASURABLE - LaunchVrf52 starts no vrfLauncher). 2 HIT (6 units, 3 TASKSTRT, 0
STP-833 refusals). 3 HIT on the traversal guard - all three closed on ARRIVAL EVIDENCE
(STP-837 margins +407/+648/+1040 m over 578/578/548 m thresholds); the 'vendor completions'
half is MISSED (later, swallowed). 4 MISS - vrfGui pid 39652 survived teardown (exit 3).
5 MISS, favourably - sim/wall ratio 1.00 (+/-0.02); terminal reports at wall 38.0 s / 153.4 s /
284.0 s (order-to-last-completion 4 min 44 s). 6 NO SIGN of a startup modal (46.5 s to READY,
inside the headless range 18.0-49.5 s, n=34). 7 HIT (TASKCMPLT label names the real code).

Teardown cause (sec 8d): MOST PROBABLE CAUSE is the GUI's documented exit prompt (UG52
4.6/4.6.1, myShowQuitDialogOnClose=1, ON in this run) going unanswered - StopVrf52.ps1 sends
WM_CLOSE and drives no dialog by design. FIRST 5.2 GUI-on teardown ever (1 of 80 StopVrf52
runs); the 8/8 and 79/79 clean record is headless-only. VERIFIED by window enumeration of pid
39652 (sec 9): TWO modals, both class makVrf::DtNeverAskAgainMessageBox - "Are You Sure?" /
"Quit VR-Forces GUI" with checkbox "Quit All Sim Engines" (underneath, disabled), and "Session
Status" / "The current session has ended. Close current terrain?" with checkbox "Execute
session changes without prompting." (on top, the only enabled window). WM_CLOSE was accepted
(it opened modal 1); no save-scenario prompt exists anywhere. Modal 2 arises because
StopVrf52 stops the back end ~20 s after closing the GUI while modal 1 is still open - our own
teardown order manufactures the stack.

Remedy direction (seat): both never-ask-again settings via documented configuration, no GUI
automation; Jira STP-844. Unpredicted finding: four dismounts gave up on an obstructed
destination - Jira STP-845.

D1 = STOP on prediction 4; D2 NOT RUN; remedy lane fix/gui-quit-prompt-teardown; the
confirming run is D1b, to be registered before it runs.

## D1b - Way A with the GUI and the run-owned appData (STP-844 confirming run)

Registered 2026-09-20 BEFORE the run. Command = D1's exact command (above) plus
`--vrf-appdata-dir C:\C2SIM\vrf-appdata-unattended\appData` (NO trailing backslash - it
makes the quoted argument's closing `\"` read as an escaped quote and vrfGui silently
ignores the option). One-time setup: `scripts\NewVrfAppData52.ps1 -Dest
C:\C2SIM\vrf-appdata-unattended`, run once by the seat, plus a post-seed copy of the
settings directory kept in the scratchpad for a post-run diff.

Predictions (guiquit_report.md sec 8, review-amended):
- P1 (HIGH): LaunchVrf52's [OK] line names the RELOCATED path, not C:\MAK.
- P2 (HIGH): "Are You Sure?" / "Quit VR-Forces GUI" never opens - CloseMainWindow TRUE, the
  front end gone inside the 20 s grace, no post-grace window diagnostic runs.
- P3 (MEDIUM-HIGH): "Session Status" / "The current session has ended. Close current
  terrain?" never opens.
- P4 (HIGH): StopVrf52 exit 0, runner exit 0, post-run VR-Forces inventory empty, nothing
  force-killed.
- P5 (HIGH): everything upstream of teardown repeats D1 (holder joins, READY unattended, 6
  units, 3/3 terminal, real time). D1b changes TWO things vs D1, not one: -VrfAppDataDir
  also relocates the connection-config file the runner reads (RunC2SimScenario.ps1:883-888),
  and the seed captures POST-D1 vendor state, not D1's starting state.
- P6 (LOW): default_Application.apsx and default_SessionSettings.srsx in the run-owned tree
  still hold the patched values after the run.

STOP rule: a P2 miss is a STOP. Falsification note for P3 (review item 8): a P3 miss WITH a
P2 hit does not fail the remedy - three candidates besides the bit mapping: one of the seven
unnamed set bits in mySessionOptions; the flag words in applicationSettings.xml; or session
settings arriving from the session database because DtAlwaysJoinWithSessionDatabase (0x4) is
set. The post-run settings-directory diff discriminates between them.

## D2 - reset between runs, the FULL CYCLE with the GUI (DEMO_RUNBOOK sec 6 item 1: 'run the one command again')

Registered 2026-09-20 BEFORE D1 runs. D2 = D1b's exact command a second time, started after D1b's teardown inventory is
clean (observer process count 0; D1b's holder may still be joined - EXPECTED, it is why D2's sim joins).
- HIGH: D2 reaches READY and completes 3/3 exactly as D1 did; no wedge (the 5.0.2-era teardown-relaunch wedge does
  not reproduce: the 5.2 runner teardown was 8/8 clean on 2026-09-14, headless; the GUI is the new variable).
- HIGH: D2's Stage 2h holder JOINS (fresh ledgered appNo) whether or not D1's holder is still in the federation.
MISS = D2 startup failure, observers blind, or any task not terminal -> STOP; the reset story for the demo is then
'restart from a clean desktop', and row 9 stays open. Sec 6 items 2-3 (GUI reload, ResetVrf) stay UNVERIFIED and
out of the demo - not needed once the full cycle is proven with the GUI.

NOT RUN 2026-09-20 - blocked by D1's STOP.

## D3 - Way B, hand-started and STP-driven, with the LaunchVrf52 holder (lane feat/demo-federation-holder).
## D4 - the audience scenario (COA-STP1's 11 taskees, GUI, real-time, route shift) - needs the user's rulings first.
