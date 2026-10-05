# REHEARSAL - WAY B CONTENT, BOUNDED BY THE DEMO EXTENT, GUI ON THE UNATTENDED appData (light registration)

STATUS: REGISTERED 2026-10-05, LAUNCH PENDING. Light registration under RL-20261005-01 (standing go-live, shared resources
agreed with the other sessions first) and RL-20261005-02 (bound the demo by a terrain extent). Demo-first: RL-20261004-04.
Predecessor: REHEARSAL_WAYB_2026-10-05.md (VOID: the back end fell minutes behind paging terrain for the far-east routes;
the GUI was left on its modals because the run-owned unattended appData was not passed - RUNBOOK AMENDMENT 2026-10-05).

## Registration

PREREG ID: REHEARSAL_WAYB-2026-10-05-2
DATE (UTC): 2026-10-05, before any launch (the commit stamp is authoritative)
BINARY / COMMIT: the main-checkout deploy recorded by RUNBOOK sec 9 "DEMOEXTENT DEPLOY" (1.0.0+git.fe57e33.Release-5.2, pin
9e8c96b5 in all eleven trees); the unattended appData C:\C2SIM\vrf-appdata-unattended (RUNBOOK sec 9 "APPDATA + LABEL +
RTIPROBE": quit prompt off, session dialogs off, Label decoration ON). If the deploy line is absent or a self-test failed,
NO launch.
TIER AND GATE: STANDARD / PREREG (light)
RUN KIND: movement

VENDOR CITATION: as REHEARSAL_WAYB_2026-10-05 (UG52 13.2 Table 21 p363, 21.2.3 p473-474; MAK ONE 2025 Adding Content 7.8
p234-236); UG52 4.6.1 "Disabling the Quit Prompt" and 4.3.1 "Show Session Terrain Change Prompts" (the unattended appData).

OWN-RECORD CITATION: RL-20261004-04, -05, -06, RL-20261005-01, -02; REHEARSAL_WAYB_2026-10-05 Result and its ADDENDUM; RUNBOOK
sec 0.5.x ADOPTED 2026-09-20 (STP-844) and its AMENDMENT 2026-10-05; PREREG_IRONSTORM_AGG_G1-6_2026-10-04 (cut A passing).

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: none (G1-6's)

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: G1-6's - the watchdog on the SIMULATION clock, 360 SIM s, 50 m; Vrf__StallDetection=true.

DEVIATION FROM RECORD: as the predecessor - Way B's ORDER CONTENT (STP's raw export, unmodified) pushed by the runner's
PushOrder, the app under the runner, not the hand-started demo path.

DEVIATION FROM RECORD: the demo extent is passed by environment (--env Vrf__DemoExtent=53.939723,23.108483,54.119385,23.414360
--env Vrf__DemoExtentMarginKm=2, the Demo overlay's values) because the runner loads appsettings.json, not the Demo overlay.

DEVIATION FROM RECORD: GUI ON (--gui) with --vrf-appdata-dir C:\C2SIM\vrf-appdata-unattended\appData (RUNBOOK AMENDMENT
2026-10-05).

## Sequence and command line

R/D': rtiexec 65540 is still up (StartRtiExec52 ensure-up starts nothing); no holder is live (67984 resigned 04:22Z) - claim
the holder block from the marker (5329 expected: 5329-5332, marker -> 5333) and start it
(scripts/StartFederationHolder52.ps1 -AppNumbers <4 numbers> -SettleSecs 28800; exit 0 = joined; 1 or 2 = STOP). S: REST
http://127.0.0.1:18080/C2SIMServer -> 200 (docker start c2sim-server-vrf if not). Quiet window agreed. Then ONE launch:
    scripts/RunScenario.sh --scenario IronStorm_Centre_52_Aggregate
      --init data/STP-IRON-STORM-SYNTHETIC_Initialization.xml --order data/STP-IRON-STORM-SYNTHETIC_Order.xml
      --client-id "Not Set" --model-set auto --duration-scale 0.25 --object-console 4 --member-console 4
      --stop-when-complete --run-secs 2700 --env Vrf__StallDetection=true --env Vrf__StallClock=sim
      --env Vrf__TaskPredecessorTimeoutSeconds=600
      --env Vrf__DemoExtent=53.939723,23.108483,54.119385,23.414360 --env Vrf__DemoExtentMarginKm=2
      --sample-threads --gui --vrf-appdata-dir C:\C2SIM\vrf-appdata-unattended\appData
      --log runs/launch52/RunScenario-rehearsal-wayb-bounded-<stamp>.log
Screenshots of the vrfGui window only (PrintWindow, no input) at about order + 2 and + 8 min. After teardown:
Enable-LabelDecoration -AppDataDir C:\C2SIM\vrf-appdata-unattended\appData -Verify. Runner block: 11 numbers from the marker.

## Expectations (RECORDED, not gated)

| # | Expectation | Confidence | Measured |
|---|---|---|---|
| B1 | One startup line naming the demo extent; one per-order summary: 23 tasks, 7 accepted, 16 refused OUT OF DEMO EXTENT (T3-T9, T12, T15, T16, T18-T23), each with its own TASKABRT reason; T17 skipped (predecessor T16 refused) | HIGH | |
| B2 | Population: 28ID 1, 1-112 IN 5, 48 IBCT 17 = 23 members; nothing else populated | HIGH | |
| B3 | T14 (48 IBCT) moves under the Auto planner on STP's ORIGINAL line; T2 and T10 run as holds (no geometry in the raw export); T11 and the holds terminal as their durations say | MEDIUM | |
| B4 | THE BACK END KEEPS PUBLISHING to the end of the window: WatchVrf backends never 1 -> 0; the vendor log shows no minutes-long feature-paging backlog (median queued wait far below the predecessor's 71 s - RECORDED as a count) | MEDIUM - the claim this run tests | |
| B5 | The Label (full designation) drawn on the map (screenshot) | HIGH (seen in the predecessor) | |
| B6 | THE GUI EXITS UNATTENDED: StopVrf52 exit 0 or 6, no "Session Status" / "Are You Sure?" modal, no vrfGui left; the unattended tree's Label still ON after | MEDIUM (STP-844's lever; its ASSUMED mapping of modal 2 is tested here) | |
| B7 | No crash-void; the app exits 0 | MEDIUM | |
| B8 | Load: peak working set and CPU near G1-6's (4.2 GB, 3.4 cores), far below the predecessor's 8.7 GB / 6.9 cores | MEDIUM | |

STOP RULE: none gates the window except the crash-void. A failed expectation becomes a demo-readiness item. The harvest is
read-only.

## Result (written after the harvest, never from a live read)

(empty - not launched)
