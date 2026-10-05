# REHEARSAL - WAY B CONTENT ON THE AGGREGATE PROFILE: STP's RAW IRON STORM EXPORT, GUI ON (light registration)

STATUS: REGISTERED 2026-10-05, LAUNCH PENDING. Light registration under RL-20261005-01 ("Lighter rehearsal go now"):
expectations written before launch, no Fable pre-review, the seat's go-live (standing authority, shared resources agreed
with the other sessions first). Demo-first frame: RL-20261004-04. Way B: RL-20261004-05.

## Registration

PREREG ID: REHEARSAL_WAYB-2026-10-05-1
DATE (UTC): 2026-10-05, before any launch (the commit stamp is authoritative)
BINARY / COMMIT: the main-checkout deploy of the DUR+T2A merge (main b22f899: ISO short-form durations RL-20261004-06,
tier-2a compositions) on the NEW PIN 9e8c96b5 (labels, RUNBOOK sec 9 "NEW PIN (LBL)"), as recorded by the RUNBOOK sec 9
"DUR+T2A DEPLOY" line; the Label decoration deployed 2026-10-04 23:13Z (RUNBOOK sec 9). If the deploy line is absent or
any self-test failed, NO launch.
TIER AND GATE: STANDARD / PREREG (light)
RUN KIND: movement

VENDOR CITATION: G1-6's (PREREG_IRONSTORM_AGG_G1-6_2026-10-04) list, unchanged; UG52 13.2 Table 21 p363 and 21.2.3
p473-474 (the Label and its symbol decoration); MAK ONE 2025 Adding Content 7.8 p234-236 (aggregate mobility).

OWN-RECORD CITATION: RL-20261004-04, -05, -06, RL-20261005-01; PREREG_IRONSTORM_AGG_G1-6_2026-10-04 (the last passing run,
cut A); data/IRONSTORM_FULL_CHANGES.md (the per-task outcome table, tier 2a note); the survey laneDR1 dr1_table.txt.

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: none (G1-6's)

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: G1-6's - the watchdog on the SIMULATION clock, 360 SIM s, 50 m; Vrf__StallDetection=true.

DEVIATION FROM RECORD: this rehearses Way B's ORDER CONTENT, not Way B's launch path. The order is pushed by the runner's
PushOrder, not by a live STP, and the app runs under the runner (Way A's harness), not the hand-started demo path
(StartInterface52 + the Demo overlay). The bytes pushed are STP's own export, unmodified.

DEVIATION FROM RECORD: the GUI is ON (--gui), unlike every G1 run (--no-gui): the front-end number is consumed, not burned.

DEVIATION FROM RECORD: the window is the 2700 s cap; -StopWhenComplete is kept but is not expected to fire (T12 drives
52.6 km; DurationScale does not scale movement).

## Sequence and command line

S/A: golive checks as G1-6 (scratch laneG1-6\g1_6_golive_checks.ps1 -Phase prelaunch -RtiexecPid 65540 -HolderPid 67984
-MarkerWant 5318) with the build hashes of the DUR+T2A deploy passed explicitly; quiet window and desktop agreed with the
other sessions. Holder branch (a): 67984 (appNo 5281) with >= 60 min left. Then ONE launch:
    scripts/RunScenario.sh --scenario IronStorm_Centre_52_Aggregate
      --init data/STP-IRON-STORM-SYNTHETIC_Initialization.xml --order data/STP-IRON-STORM-SYNTHETIC_Order.xml
      --client-id "Not Set" --model-set auto --duration-scale 0.25 --object-console 4 --member-console 4
      --stop-when-complete --run-secs 2700 --env Vrf__StallDetection=true --env Vrf__StallClock=sim
      --env Vrf__TaskPredecessorTimeoutSeconds=600 --sample-threads --gui
      --log runs/launch52/RunScenario-rehearsal-wayb-<stamp>.log
During the window: screenshots of the vrfGui window only (PrintWindow on its handle, no input sent) at about order + 3,
+ 10 and + 30 min. After teardown: tools/display/Enable-LabelDecoration.ps1 -Verify.
NUMBERS: from the marker read at launch (5318 expected): the runner's 11-number block 5318-5328, marker -> 5329.

## Expectations (written before the run; RECORDED, not gated - a rehearsal has no verdict on a mechanism)

| # | Expectation | Confidence | Measured |
|---|---|---|---|
| X1 | Parse: 23 tasks; 46 of 46 IsoTimeDuration values decoded; exactly ONE short-form WARN naming STP-848 and RL-20261004-06 | HIGH | |
| X2 | Population at order receipt: the performers with a composition are populated (28ID, 1-112 IN, 48 IBCT, 116 ABCT, 278 ACR, 56 SBCT, 169 FAB); 55 MEB and 11 CAB refused with the no-composition reason; total members about 99 (G1-6's 23 + tier 2a + 116 ABCT's 26) | MEDIUM | |
| X3 | Refusals named, not silent: the leg-limit tasks (T15, T23) and the MEB/CAB tasks report TASKABRT with their reason; their successors are skipped with a reason | MEDIUM | |
| X4 | Cut A's movers (T02, T10, T14) move on STP's ORIGINAL lines (no cut-A hand fixes in this order) under the Auto planner; arrival is RECORDED, not predicted | MEDIUM (move) | |
| X5 | The map shows the full C2SIM designation as the Label under each unit symbol (screenshot) | MEDIUM (never observed live) | |
| X6 | After the GUI's normal exit, Enable-LabelDecoration -Verify exits 0 (the GUI's own save kept the Label ON); exit 1 = the falsifier | MEDIUM | |
| X7 | No back-end crash; StopVrf exit 0 or 6; the app exits 0 | MEDIUM (load 4x G1-6) | |
| X8 | The pre-flight fetches the OSM tiles the new routes need (cache count rises from 520; 0-byte tiles counted) | MEDIUM | |
| X9 | The 14 StartTime/RelativeTime delays of the raw export are still unread: the dispatch times follow the predecessor chains and Durations only - RECORDED with the per-task dispatch times | HIGH | |
| X10 | Peak working set and CPU of the back end RECORDED against G1-6's (thread-samples) | - | |

STOP RULE: none gates the window except the crash-void (V, G1-6's). The harvest is read-only; a failed expectation becomes a
demo-readiness item, not a re-run under this registration.

## Result (written after the harvest, never from a live read)

(empty - not launched)
