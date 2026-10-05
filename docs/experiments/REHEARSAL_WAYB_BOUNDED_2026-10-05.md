# REHEARSAL - WAY B CONTENT, BOUNDED BY THE DEMO EXTENT, GUI ON THE UNATTENDED appData (light registration)

STATUS: REGISTERED 2026-10-05; RUN 20261005T120620Z_run; RESULT WRITTEN (B1-B7 HELD, B8 PARTLY). Light registration under RL-20261005-01 (standing go-live, shared resources
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

## Result

Harvested 2026-10-05 (read-only lane) from runs/20261005T120620Z_run, the runner log
runs/launch52/RunScenario-rehearsal-wayb-bounded-20261005T120619Z.log and the seat's scratchpad rehearsal2\ (holder.txt,
claim.txt, stamp.txt, shot_t02.png). [V] = read from those files by this lane; [A] = assumed or seat-reported, not re-read.

TIMELINE [V]: holder RtiProbe 41292 joined on 5329 at 12:05:56Z (5330-5332 not reached: BURNED); runner launched 12:06:20Z,
block 5333-5343 (marker 5333 -> 5344); its own Stage 2h holder 60332 joined on 5340 in 3 s (5341-5343 BURNED); back end
58348, GUI 49364; the ORDER reached the bus at 12:10:10.986Z (PushOrder returned about 12:10:41Z - the seat's "~12:10:54Z" is
not the order time); 23/23 terminal at 12:12:58Z; window closed early at 200 s of 2700; app exit 0; runner exit 0, end
12:16:39Z. The sim ran fast: SIM/WALL 7.4, 6.0, 5.6, 3.0 per 60 s (app log; G1-6 recorded about 7.85), so 300 SIM s holds
ended in about 40-55 WALL s.

SCORES:
- B1 (HIGH) HELD [V]. One start-up line "DEMO EXTENT ON ... + 2 km margin" (app L68); one per-order summary (L750): 23 tasks,
  16 refused, and the six performers with no accepted task not populated (56 SBCT, 278 ACR, 116 ABCT, 55 MEB, 169 FAB,
  11 CAB). The 16 = the EXT lane's offline list exactly: T3-T9, T12, T15, T16, T18-T23; each its own TASKABRT, all 16
  received 12:10:11.9-12:10:12.1Z (reports-captured.log). Reason: PERFORMER START outside for 13 (T3-T5 56 SBCT 22.6 km;
  T6-T9 278 ACR 10.9 km; T12 116 ABCT 15.9 km; T18-T21 55 MEB 8.3 km; T22 169 FAB 1.4 km); ROUTE VERTEX outside for 3 (T15,
  T16 48 IBCT vertex 1 at 87.0 km; T23 11 CAB vertex 1 at 49.7 km). T17 SKIPPED (predecessor T16 refused; its own TASKABRT
  "SKIPPED", L724). 33 "OUT OF DEMO EXTENT" lines = 16 refusal + 16 TASKABRT + 1 summary. The summary does not print "7
  accepted"; 7 = 23 - 16 (T1, T2, T10, T11, T13, T14 ran; T17 skipped).
- B2 (HIGH) HELD [V]. POPULATE IN PLACE three times only: 28ID 1, 1-112 IN 5, 48 IBCT 17 = 23 members, each "N of N
  created"; 36 empty containers at init; WatchVrf reflected 67 at the end.
- B3 (MEDIUM) HELD [V]. T14 navigate-to-location at 12:11:03.4Z on STP's ORIGINAL line (MapGraphicID
  FollowAndSupport_48_IBCT_SLOT1; its first vertex = the unit's position, dropped; 2768 m), Vrf:AggregateMovePlanner=Auto,
  one vertex to all 17 members. ARRIVAL EVIDENCE 114.5 WALL s / 640.6 SIM s after dispatch, nearest 497 m; TASKCMPLT
  12:12:57.9Z. At the trace's last fix (t=274 s) the container is 153 m from the vertex; members moved 1.6-3.2 km and end
  0-9 m (11 of 17) or 262-790 m (6) from it. T2 and T10 (and T1, T11, T13) ran as holds; their 6 members moved 0 m; all
  TASKCMPLT on their scaled durations (T10's 450 s, the rest 300 s).
- B4 (MEDIUM) HELD [V]. WatchVrf backends=1 in all 148 status lines, t=3.0-302.7 s - never 1 -> 0. Vendor sim log read from
  line 4,991 on (after the environment block; nothing quoted from it): 205,946 lines, ZERO "queued for <s> seconds" lines,
  zero feature_source paging jobs, 18 MAK_OBSTACLE mentions (the 17 T14 member navigate tasks + 1). Predecessor, same parser
  (control, it reproduces the ADDENDUM): 39,806 parsed, median 71.3 s, p95 191.5 s, max about 3,188 s (interleaved lines
  dropped). Bounded run: count 0, so no median/p95/max exist.
- B5 (HIGH) HELD [V]. shot_t02.png (12:12:57Z, GUI clock 0:00:19:31): the full designation drawn under the 30-char name for
  every visible unit (e.g. 169_FAB/28ID__FRIENDLY_FIELD_ARTILLERY_BRIGADE); the 48 IBCT / 1-112 IN and 28ID stacks overprint.
- B6 (MEDIUM) HELD [V]. StopVrf: CloseMainWindow on vrfGui returned TRUE; the 120 s window diagnostic lists only the BACK
  END's two invisible windows - no "Session Status", no "Are You Sure?", no vrfGui; runner "no VR-Forces processes remain";
  no vrfGui now. EXIT 6 MEANS the BACK END did not leave on taskkill without /F within 120 s and this run's own pid 58348 was
  forced - not a GUI modal (the vendor log ends in the back end's object removals). Label after: Test-LabelDecorationFile on
  the unattended tree = on (5/Ground, 5/Default), file mtime 12:06:51Z (GUI start; not rewritten at exit).
- B7 (MEDIUM) HELD [V]. No crash-void line in the runner log; app exit 0; runner exit 0; the watchdog stood down.
- B8 (MEDIUM) PARTLY HELD [V]. Back end 58348 (119 samples, 12:06:40-12:16:33Z): peak WS 3,856 MB (the last sample, in
  teardown; 3,157 MB at the order), peak CPU 4.38 cores (12:13:27Z), mean 1.66 (2.33 after the order); 1 WS-RUNAWAY alert at
  12:07:45Z (pre-order load). WS below G1-6's 4,172 MB and far below the predecessor's 8,740 MB; CPU peak above G1-6's 3.37,
  far below the predecessor's 6.85.

HOLDER LEDGER (OPUS_EXECUTION_PLAN.md Appendix B, the 5329-5332 entry at about L3701): it was written by the G1-6 helper and
names IRONSTORM_AGG_G1-2026-10-04-6, branch (b'), and "runner blocks PRE-WARM 5333-5343, then SCORED 5344-5354". It should
say: "2026-10-05 REHEARSAL_WAYB-2026-10-05-2 PERSISTENT HOLDER (docs/experiments/REHEARSAL_WAYB_BOUNDED_2026-10-05.md step
R/D'): StartFederationHolder52 -AppNumbers 5329,5330,5331,5332 -SettleSecs 28800; JOINED on 5329 (pid 41292, 12:05:56Z);
5330-5332 not reached, BURNED. The rehearsal's one runner block is 5333-5343 (run 20261005T120620Z_run; its Stage 2h holder
60332 on 5340; 5339 and 5341-5343 unconsumed, BURNED); no 5344-5354 block." The seat fixes it.

WHAT IT MEANS (implication, separate from the measurements): bounding the raw Way B order by the demo extent removed what
broke the predecessor. The 16 out-of-extent tasks were refused within a second with readable reasons, only 23 members were
populated, the back end never stopped publishing and paged no terrain features (0 queued waits against a 71 s median), and
the GUI left unattended. What remains to watch on the map is one mover (48 IBCT, about 2.8 km, done in about 2 min of wall
time) and five holds, all finished about 2 min 47 s after the order. That is a stable, short show of a thin slice of the
order, not the order's operational content (13 of 16 refusals are units whose start lies outside the extent).

DEMO-READINESS ITEMS:
- D1 Content inside the extent is thin: one mover, five holds. More motion needs STP geometry inside the box, or a larger
  box (re-test B4 then) - the owner's call.
- D2 Pace: the GUI shows 1x but the sim runs 3-7x (fixed-frame run-to-complete); every task ends within about 3 min.
  Decide the show clock (real time, or longer durations).
- D3 Refused units stay on the map as unpopulated containers with labels; decide whether the show hides or explains them.
- D4 Labels overprint where units stack (48 IBCT / 1-112 IN, 28ID); RL-20261005-03's clean designator shortens them, the
  stacking stays (zoom or placement).
- D5 6 of 17 T14 members end 262-790 m off the vertex; the vendor log carries 21 "Failed to navigate off road: Start point is
  within obstacle" lines. Unattributed here; check before the show if the scatter is visible.
- D6 StopVrf exit 6 (back end forced after a refused no-/F taskkill) is the remaining teardown roughness; the GUI side is
  clean. A stale-federate check on the next join is RUNBOOK sec 0's.
- D7 Ledger fix for the 5329-5332 holder entry (above).

VERIFIED vs ASSUMED: all scores [V]. [A]: the seat's "+8 min screenshot found no vrfGui window" (no shot_t08 file exists;
consistent with teardown ending 12:16:39Z); the 18 MAK_OBSTACLE mentions mapped to T14's members by name shape only;
predecessor max from a parse that drops interleaved lines. n = 1.
