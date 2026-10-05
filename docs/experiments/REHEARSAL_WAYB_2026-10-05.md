# REHEARSAL - WAY B CONTENT ON THE AGGREGATE PROFILE: STP's RAW IRON STORM EXPORT, GUI ON (light registration)

STATUS: REGISTERED 2026-10-05; LAUNCHED 00:24:23Z; HARVESTED (Result below; window VOID). Light registration under RL-20261005-01 ("Lighter rehearsal go now"):
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

RESULT 2026-10-05: WINDOW VOID by the registered crash rule (WatchVrf backends= 1 -> 0 at trace t=339.9 s = 00:33:19Z), but
the back-end PROCESS DID NOT DIE (see CRASH, HANG OR LOST RTI). Run 20261005T002424Z_run; runner exit 4; app exit 0 (clean
resign); StopVrf EXIT 7. Marks: [V] = checked by this harvest lane (read-only: the run directory, the runner log, the rtiexec
log's join/resign lines, the seat's scratch rehearsal\ artefacts, G1-6's scored run directory); [A] = the seat's record or an
inference. Vendor logs: names, sizes, mtimes only. Lane files: scratch 7207877b...\harvestWayB (h_timeline.py, h_warn.py,
h_movers.py).
CORRECTION TO THE SEAT'S NOTE [V]: the ORDER reached the bus at 00:28:28.939Z (runner :266; PushOrder's 30 s listen ended about
00:28:58Z); "pushed 00:29:04Z" is not in the record. "t+306 s" is the runner's poll after PushOrder returned; the last 18
terminal reports were received 00:34:01.36-.45Z. Of the 19 TASKABRT, 10 are SKIPPED, 8 "back end lost", 1 MALFORMED.

THE ROWS [V]:
- X1 (HIGH) HELD: "ORDER: 23 task(s) for 9 taskee(s)"; exactly ONE short-form WARN (app L640: 32 values in the short form,
  naming STP-848 and RL-20261004-06); 0 decode-failure lines. The order holds 46 IsoTimeDuration (23 Duration + 23
  DelayTimeAmount); the app prints no "46 of 46" count, so that figure is [A] from the 0 failures.
- X2 (MEDIUM) HELD: populated 28ID 1, 56 SBCT 5, 278 ACR 26, 1-112 IN 5, 116 ABCT 26, 48 IBCT 17, 169 FAB 19 = 99 members;
  55 MEB and 11 CAB "REFUSED - NO COMPOSITION" (fail lines, type-map rows F-UCE-H, F-UCVRA-H).
- X3 (MEDIUM) PARTLY: no refusal was silent - all 19 TASKABRT carry a reason, all 10 skips name their predecessor. But T23's
  refusal is the ROUTE EXTENT REFUSAL (STP-833, MALFORMED, 00:29:48.670Z), not the no-composition reason; T18 (55 MEB) was NOT
  refused - it DISPATCHED hold-in-place on the empty shell (TASKSTRT, 00:28:29.464Z); T15's extent check never ran (skipped).
- X4 (MEDIUM) NOT MET: in the raw export T2 (696fbb33, cut A's T02) and T10 (9aab7fe6) carry no geometry and ran
  hold-in-place; T14 (1075b583) dispatched navigate-to-location at 00:30:54.644Z and 0 of 48 IBCT's 17 members moved > 50 m in
  the 145 s to the 1 -> 0. No arrival. The only unit that moved > 50 m before the 1 -> 0: 169 FAB (T22; 20 uuids, to 1,845 m).
- X5 (MEDIUM) HELD: shot_t03 / shot_t10 show the full designation under the 30-char name (e.g. "55_MEB/28ID__FRIENDLY_ENGINEER"
  over "55_MEB/28ID__FRIENDLY_ENGINEER_BRIGADE"); where units stack (48 IBCT, 1-112 IN, 28ID) the labels overprint illegibly.
- X6 (MEDIUM) UNTESTED: -Verify exit 0 at 01:17:12Z (label_verify_after_gui.txt), but GUI 680 never exited (modal up), so the
  GUI's own save on a normal exit did not happen.
- X7 (MEDIUM) FAILED: the crash-void signal fired (runner :283); StopVrf EXIT 7 (close refused with the "Session Status" modal
  on 680; 6724 force-stopped; 680 left up, not killed); the app exited 0.
- X8 (MEDIUM) HELD: preflight-cache now 1,181 files = 520 + 661, 0 zero-byte, newest 00:34:15Z; the app's TILE TOTAL: 33 HITs,
  661 HTTP FETCH, 0 given up, 0 undecodable. No before-listing of preflight-cache this run: the 520 is G1-6's count [A].
- X9 (HIGH) HELD: 14 RelativeTime StartTimes; 13 carry PT0S and only T2's carries PT20M (300 s scaled) - T2 dispatched
  00:30:43.920Z, 44 ms after T1's TASKCMPLT (00:30:43.876Z): unread. The 9 SimulationTime delays ARE read (T6 600 -> 150 s;
  T10, T12, T23 1200 -> 300 s; app L674-L730). Dispatch times: the timeline.
- X10 RECORDED: back end 6724 peak WS 8,740 MB (5,186 MB before the 1 -> 0), peak CPU 6.85 cores (5.5 before), mean 1.47 cores
  before / 1.72 after; 3 WS-RUNAWAY alerts (00:25:51, 00:31:02, 00:32:23Z). G1-6 scored (52080): peak WS 4,172 MB, peak CPU
  3.37, mean 1.39, 2 alerts. Members 99 vs 23 (4.3x). Back-end vendor log 28,414,123 B (G1-6's 52080: 19,525,643 B).

TASK TIMELINE [V] (dispatch WALL Z / SIM s from the app log; terminal = ListenReports receive time; B/A = before/after the
1 -> 0 at 00:33:19Z; "lost" = "VR-Forces back end lost (no status for 40 s)"):
  T18 55 MEB   hold      00:28:29.5 /   4.9            -> ABRT lost      00:34:01 A
  T1  28ID     hold      00:28:36.6 / 140.7            -> CMPLT          00:30:43.9 B
  T13 48 IBCT  hold      00:28:37.5 / 141.0            -> CMPLT          00:30:43.9 B
  T3  56 SBCT  hold      00:28:42.1 / 140.9            -> CMPLT          00:30:47.0 B
  T22 169 FAB  navigate  00:28:48.2 / 141.3            -> ABRT lost      00:34:01 A
  T6  278 ACR  patrol    00:29:33.1 / 255.9 (150 s)    -> CMPLT          00:32:46.8 B
  T10 1-112 IN hold      00:29:48.6 / 310.0 (300 s)    -> ABRT lost      00:34:01 A
  T23 11 CAB   (none)                       (300 s)    -> ABRT MALFORMED 00:29:48.7 B
  T12 116 ABCT navigate  00:30:28.8 / 420.4 (300 s)    -> ABRT lost      00:34:01 A
  T2  28ID     hold      00:30:43.9 / 422.0            -> ABRT lost      00:34:01 A
  T14 48 IBCT  navigate  00:30:54.6 / 433.5            -> ABRT lost      00:34:01 A
  T4  56 SBCT  navigate  00:31:08.5 / 447.2            -> ABRT lost      00:34:01 A
  T7  278 ACR  hold      00:32:46.9 / 497.8            -> ABRT lost      00:34:01 A
  Never dispatched, SKIPPED at 00:34:01 (A): T5<-T4; T8<-T7, T9<-T8; T11<-T10; T15<-T14, T16<-T15, T17<-T16; T19<-T18,
  T20<-T19, T21<-T20.
SKIP-CHAIN ROOTS: T4, T7, T10, T14, T18 - each TASKABRT "lost" (app L116783-L116815; the BACK END LOST line gives the last good
reading 00:33:21Z and 8 tasks in flight). No skip descends from a refusal: T23, the one refusal, has no successor. Before the
1 -> 0: 4 TASKCMPLT + 1 TASKABRT; after: 8 lost + 10 skipped; 23 of 23.

CRASH, HANG OR LOST RTI CONNECTION - measurements [V]:
(a) The process lived: 6724 was sampled to 01:16:57Z (523 samples after 00:33:19Z, CPU mean 1.72 cores, 6 under 0.05; WS 5,186
    -> 8,740 MB; 68-69 threads); StopVrf's inventory (about 01:15Z) listed it (68 threads); taskkill without /F answered
    SUCCESS, then it was forced. C:\MAK\logs holds its .log (28,414,123 B, last written 01:12:40Z), no .dmp, no .callstack.log.
    Thread structure: four threads (30784, 20692, 63840, 66924) carry about 1.4 cores 00:32:48-00:36 and fall to about 0 at
    about 00:37; five others (40464, 52260, 59184, 62704, 66372) then hold about 1.4-2 cores to the force. WS is not monotonic:
    8.5 GB at 00:37, 6.8 at 00:45, 8.7 at 01:15 (about 63 MB/min 00:45-01:15, against 684-948 MB/min in this run's three
    WS-RUNAWAY alerts). The machine has 31.7 GB of RAM, so memory exhaustion is a weak competitor.
(b) The RTI management link held: in the rtiexec log, Federate23 ("VR-Forces Sim Engine 5.2d", joined L76725; = 6724 by join
    order and time [A]) has no resign or drop until "Dropped connection #218" and a LostConnectionResign (DeleteObjects) at
    21:17:00 local = 01:17:00Z (L93410-L93458) - the force. WatchVrf kept reflected=145 to the end (nothing deleted).
(c) Publishing to the two remote-controller observers (the app, WatchVrf) stopped: 1 -> 0 at 00:33:19Z; the app's last good
    status 00:33:21Z. After it only 2 of 135 trace uuids change position: 169.RKT1BTY1MRL2 (f5ed1104) at a constant 8.31 m/s,
    91.8 deg, for 2,490 s while its altitude climbs linearly 189 -> 494 m, and 169 FAB's aggregate (a4777af0) at 0.88 m/s,
    357 deg, 2,185 m in a straight line - dead reckoning from a last update, not terrain-following movement.
(d) Before it: SIM/WALL 0.56 between T12's and T7's dispatches (77.4 SIM s / 138.0 WALL s); right after T12's dispatch the
    sim clock moved 1.6 s in 15.1 WALL s (420.4 -> 422.0 at T2's dispatch). Five-thread bursts of about 5 cores follow T22's
    (00:28:57-00:29:27), T6's (00:29:37-00:30:17) and T4's (00:31:12-00:31:22) dispatches; after T12's and T14's the back end
    sat at 0.1-0.9 cores (00:30:37-00:32:48) while the sim clock crawled. The GUI clock read 0:00:07:12 at 00:31:41Z and
    0:00:14:28 at 00:38:57Z (+436 s in 436 s); shot_after.png (01:17:25Z) shows it FROZEN at 0:00:16:52 (Play enabled, Pause
    greyed): 1,012 s, which is the app's last reading (497.8 s at 00:32:46.9Z) + 514 s at 1.0x, so the GUI's session ended
    about 00:41:21Z, about 8 min after the app and WatchVrf lost status.
READINGS (no cause claim): a crash in the sense of process death is REFUTED by (a). A lost RTI connection is not supported at
the rtiexec level (b); the data path (rtiForwarder) was not read, so a forwarder-side stall is not excluded. What fits (a)-(c):
the back end alive, busy and growing but publishing neither status nor entity updates for 44 min - a hang/livelock of its sim
loop; the competitor is an extreme slowdown (frames longer than the 40 s status window), which the vendor log's content could
separate and this lane does not read. (d)'s GUI clock is RECORDED only: whether that display is fed by the back end or ticks
locally is unknown. Two readings of the GUI's 8 min, no claim: the GUI free-runs its clock with a longer timeout, or the
back end's time kept reaching the GUI after the two remote controllers lost status. [A] (c)-(d) - the low-CPU crawl after
T12 / T14 and the thread hand-over at 00:37 - favour a stall tied to route planning over a generic livelock; not settled.
The runner's "BACK END CRASHED" means "stopped publishing" here, not a process death.

WHAT IT MEANS (implication, separate from the measurements): the raw Way B order with the GUI on the aggregate profile did not
run to its end on this machine. About 4.8 min after the order, with 99 members populated (4.3x G1-6) and the working set
climbing past 5 GB, the back end went silent; every unfinished task then reported TASKABRT, and on the map units froze while
dead-reckoned icons drifted straight off (169 FAB, east, climbing). Load (members, GUI) and one piece of content (the T4 / T12
/ T14 plans, T6's patrol, the 661-tile pre-flight) are not separable at n = 1. Way B's content itself parsed and dispatched as
designed (X1, X2, X9) and its refusals were named (X3). The full raw order with the GUI is not demo-ready.

DEMO-READINESS ITEMS:
- D1 The back end went silent under the full raw order + GUI. The one-variable leg is the raw order with --no-gui; a 23-member
  cut with the GUI changes members AND content (cut A's hand-fixed geometry), so it is not a one-variable test.
- D2 The "Session Status" modal refused the GUI close (StopVrf 7); 680 is still up and blocks the next launch - the owner's.
- D3 GUI runs need the run-owned appData (NewVrfAppData52 + LaunchVrf52 -AppDataDir) so no modal is raised.
- D4 The runner's "CRASHED" fired on a live pid: add pid liveness and rtiexec state to the void line (silent vs dead).
- D5 Dead-reckoned drift after a silence is visible on the map: a stop/hide rule for the show.
- D6 RelativeTime DelayTimeAmount is unread (T2's PT20M): implement, or STP states it another way (owner's call).
- D7 Raw-export movers: T2 / T10 carry no geometry (cut A's MapGraphicIDs were hand-added); only T4, T12, T14, T22 navigate.
- D8 55 MEB / 11 CAB have no composition: T18-T21 run on an empty shell; T23 is refused by the extent check.
- D9 X6 is still untested: label kept after a NORMAL GUI exit needs a run whose GUI exits cleanly (after D3).
- D10 Labels overprint where units stack (48 IBCT / 1-112 IN / 28ID): demo zoom or placement.
- D11 This run fetched 661 tiles live (cache 1,181): the demo machine needs the cache carried, or network at show time.

NEXT: the cheapest discriminator is the vendor sim log's lines after 00:33:19Z (frame/time lines continuing = slowdown;
repeated planner/route lines = planning stall; nothing = hang). Reading vendor-log CONTENT is the OWNER'S call (this record
treats vendor logs as names/sizes/counts only); then the raw order with --no-gui (D1).

ADVERSARIAL REVIEW: (1) n = 1. (2) Unit attribution of trace uuids rests on the app's IDENTITY marking lines (135 named);
"moved" is a > 50 m / > 5 m displacement test, not an attribute timestamp - a unit that moved < 5 m after 00:33:19Z is not
told apart from a frozen one. (3) 6724 = Federate23 is [A] (join order and time). (4) Hang vs slowdown stays open (NEXT).

ADDENDUM 2026-10-05 ~10:20Z (the seat; the vendor-log CONTENT read and the GUI close authorised by the owner, "Go for 1-2";
lines after the environment block only, nothing quoted from it):
- THE VENDOR SIM LOG (runs/20261005T002424Z_run/vendor/vendor-vrfSim.log, 363,522 lines): 49,434 terrain feature_source paging
  jobs (DtVrfCallbackQueue feature_source job: tfs, mvt, dynamic feature set) and 55 MAK_OBSTACLE area jobs (buffer=10; the first
  at line 159,525; the last ones at 24.26-24.44 E, 54.18-54.26 N - east of every cached tile). 40,737 'queued for <s> seconds'
  lines: median wait 71 s, p95 192 s, pending queues up to about 122; the log's last lines are still such jobs completing.
  READING [A]: the back end did NOT hang - it kept completing work until the force - but its terrain feature paging and the
  planner's obstacle-area collection were backed up by minutes, which fits the observers' 1 -> 0 at 00:33:19Z as an extreme
  slowdown under the full order's much larger terrain extent (out to about 24.4 E). Competitor: the queues are a side effect of
  a different stall; against it, jobs keep completing to the end. Thread 20692 (one of the four hot threads) is a feature_source
  worker by the log's own thread info. Not settled; the demo implication is to bound the order's terrain extent (or pre-page it).
- X6 HELD: the GUI was closed normally at 10:16:30Z (the Session Status dialog answered Yes, then Quit confirmed); the settings
  file was not rewritten on exit (mtime 00:25:00Z, the GUI's start) and Enable-LabelDecoration -Verify exits 0 (ON).
- DEVIATION NOT REGISTERED (found 2026-10-05): the GUI-on launch did NOT pass the run-owned unattended appData
  (RUNBOOK sec 0.5.x, ADOPTED 2026-09-20, STP-844; the tree was gone after the machine rebuild) - so the vendor prompts
  were on, and the leftover GUI's two modals are that procedure's known symptom. RUNBOOK AMENDMENT 2026-10-05 records it.
