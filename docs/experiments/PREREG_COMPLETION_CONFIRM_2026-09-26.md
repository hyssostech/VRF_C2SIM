# PREREG - COMPLETION CONFIRM: one live run of the completion code unit (temporary position on completion)

STATUS: REGISTERED 2026-09-26T11:45Z, BEFORE the build, the holder, the ledger claim's use and the run.
Drafted 2026-09-25 from the record (lane P2, read-only); finalised and committed by lane R (session 5fc25950)
on branch run/completion-confirm-2026-09-26. The predictions are lane M4's FINAL live-run assertion list
(items 1-9, item 5 (i)-(v) and the displacement rule), which REPLACES the draft's assertion section.
Marks: [V] = checked while writing this file (file read, grep, ls, sha256); [A] = inferred, not checked.

## Registration

PREREG ID: COMPLETION_CONFIRM-2026-09-26-1
DATE (UTC): 2026-09-26T11:45Z (registration commit; before the ledger claim is used and before the holder)
BINARY / COMMIT: main a9d738f ("Merge feat/completion-temporary-position: completion on the owner's temporary position
+ re-clamp gate/tally fix"), which contains the completion unit f4aa43d and its follow-ups L3 583f660, L4 577fde6,
L5 1d6af35 [V: git log]. The managed consumers are rebuilt from the main checkout at a9d738f with
-p:BridgeConfig=Release-5.2 BEFORE any ledger edit touches that checkout (sec 4 step B). The native bridge is NOT
rebuilt: `git diff --stat 13e8c73 a9d738f -- src/VrfBridge` is EMPTY [V], so the pinned VrfBridge.dll stands -
sha256 90272bc95297e3304066218bd5a53128ef8531cfbd6b4dcec3ce5c65b0f25847 (1,001,472 B; RUNBOOK sec 9 pin line
"NEW PIN 2026-09-15 16:52Z") [V: sha256 of src/VrfBridge/build/Release-5.2/VrfBridge.dll]. All ELEVEN 5.2 consumers
must carry that one hash. The deployed VrfC2SimApp.dll / .exe sha256 and ProductVersion are recorded at build time
in the BUILD RECORD addendum (committed before the run) and again after the run.
TIER AND GATE: HEAVY / PREREG

VENDOR CITATION: VR-Forces 5.2 Users Guide (UG52) sec 23.3 p505, "How the Move Along Route Task Works": "The Move
Along Route task takes a route object and performs a sequence of movements directly to each vertex of the route.
There is no path planning done as it moves toward the next vertex ... it will nevertheless consider that it has
reached the vertex if it moves even with the vertex on a parallel path" [V: docs\vendor\mak-5.2\txt]. UG52 sec
23.2 / 23.2.1 p500: only Move To plans a path, so a missing navigation mesh matters only to Move To [V].
C:\MAK\vrforces5.2d\include\vrfobjcore\singleTaskControllerComponent.h:191-200, decideToGiveUpTask: "The
implementation in this class always returns false." The vendor never reports a unit that stops making progress,
so a stuck unit is reported only by this interface's own progress watchdog [V].

OWN-RECORD CITATION: docs\RULINGS.md RL-20260921-09 (the owner's TEMPORARY position: "completion is based on start
time + duration ... If they arrive after the expected start time + duration, they complete immediatelly. Follow-on
tasks still are permitted to take their whole specified duration"); RL-20260925-01 (D1 scope approved, D2 a stuck
unit's follow-ons are abandoned with their own aborts, D3 stall detection ON in the demo profile, D4 the parked
engage is still issued on a late arrival); RL-20260921-05 ("Arriving late does _not_ imply an abortion");
RL-20260921-07 ("abort in case the unit stays put"); RL-20260914-01 (archive: TASKABRT is the code STP sees for a
stalled unit). docs\CORRECTIONS_LOG.md F-4. DEMO_READINESS rows 19 and 26 (row 26: "Owed: the live confirmation
run (it must show the stall TASKABRT sent, not suppressed)"). docs\experiments\PREREG_D5_WAYB_2026-09-21.md sec 4
and 15: the three R9 legs on R9_Mojave_Empty_52 + the lean init completed at 81.1 / 485.5 / 660.5 SIMULATION s
after dispatch in D8, with D9 and D5c within 1.2% on the two long tasks. HANDOFF_2026-09-14_PARALLEL_LANES.md:
a blank line in a pushed message kills the SDK's STOMP pump (STP-795). PREREG_V6F_FDD_CREATE_2026-09-15.md sec 4.3:
a literal "<Task>" in a comment made the SDK send the order as BML (STP-830). Lane M4 review (session scratch
laneM4_report.md, reproduced in the Predictions below so the record does not depend on it).

RUN KIND: movement

## 0. Purpose, in plain words

The completion code unit changed when this interface tells STP that a task is finished. Before it, every task with
a Duration was reported complete at dispatch + Duration, whether or not the unit had arrived. Now:
- (a) a unit that arrives EARLY is held, and reported complete at its end time;
- (b) a unit still travelling at its end time is logged OVERDUE, and reported complete the moment it arrives;
- (c) its follow-on task waits for it instead of being skipped;
- (d) a task with no destination ends at its end time;
- (e) a task with no Duration completes only on evidence;
- (f) a stuck unit gets the watchdog's abort, and its follow-ons are abandoned;
- (g) an ATTACK/BREACH engage fallback judges a stuck unit stuck (watchdog verdict, else the stays-put test).

Offline tests prove the rule against a mirror of the service, not the service itself (lane M review, Q3). This run
is the one live look. It checks the STRUCTURE of the log and of the reports on the bus, on a small order whose
timings are known from three earlier runs. It does not measure how well units drive.

## 1. Decisions taken from the record

(a) WHICH ORDER. No order in data\ fits [V: census of data\*Order*.xml by the drafting lane]:
- The R9 orders carry 3 MOVE tasks, 0 Durations and no chains.
- COA-STP1_Order.xml carries 42 Durations and STREND chains, but 24-33 km legs, no no-destination task (its hold
  verbs map to HoldObjective, a bare move WITH a destination), and it needs the COA navigation fixture, which is not
  deployed.
- The Iron Storm orders' AO and fixture are not deployed on the rebuilt machine.

DECISION: the smallest authored order, sec 2 (data\COMPLETION_CONFIRM_Order.xml, committed with this file). It uses
the three R9 legs verbatim, whose SIMULATION completion times are on record, plus Durations chosen so that:
- one unit arrives EARLY: T_CC_TK1, arrives about 81 of 480 s;
- one arrives LATE: T_CC_CO1, arrives about 660 against an end of 420 s (240 s past its end, 180 s past end + margin);
- two tasks have no destination: T_CC_TK1H 300 s and T_CC_CO1H 360 s, the follow-ons of the early and late arriver;
- one move has no Duration: T_CC_PL1, the R9 parity control.

DurationScale stays 1.0. The last planned report lands about 1,020 SIM s after the order; at the recorded 2.6-4.7x
SIM/WALL ratio (RUNBOOK 11f, N11) that is roughly 220-420 wall s, well inside the 1,800 s window.

The order MUST be validated with one real push before the timed run (sec 4 step C): no comments at all (STP-830), no
blank line (STP-795), nothing in the XML prolog, Durations in the full C2SIM form P##Y##M##DT##H##M##S
(OrderParser.cs: "PT20M is NOT valid C2SIM"), ASCII + CRLF.

(b) WHICH FIXTURE: R9_Mojave_Empty_52 (vendor model set). Deployed; sha256
ed65c3513f26981311b150e68ce1f8d51f15d9fb48b434e2f864d6438ab83a68 equals the committed
tools\FixtureGen\frame_variants copy [V 2026-09-26]. The wrapper's default R9_Mojave_Empty_52_NavAO is NOT deployed,
so --scenario is mandatory. No navigation area is registered for it (the AO20 regeneration of 2026-09-25 failed its
gate and was not registered). Every authored leg is driven by Move Along Route (ruling Y-10), which plans no path
(UG52 23.3 p505), so a missing mesh does not stop these legs; these exact three legs completed 3/3 on this fixture
in D8, D9 and D5c. Whether members of the platoon and company pass through ground-vehicle-move-to.lua is not
established [A]. Why not _AG: its only difference is useAbstractGraphs=true and it has no live R9 record.

Re-clamp (item 8) is meaningful ONLY if this area is COLD (placements fall to the FALLBACK altitude). The laydown's
MAK Earth pages were streamed by the 2026-09-25 navigation-generator run [A: that the sim's runtime query reads that
cache]. The likely world is WARM, which makes item 8 vacuous. Both worlds are registered (P8); the run's first job
is to say which one it was.

(c) SERVER: the private c2sim-server-vrf, REST http://127.0.0.1:18080/C2SIMServer and STOMP
http://127.0.0.1:61614/topic/C2SIM [V 2026-09-26: REST 200, docker "Up 12 hours"]. The operator's server on
8080/61613 is never touched; StompProbe (hard-wired to 8080/61613) is NOT used.

(d) SETTINGS.
- STALL DETECTION ON: --env Vrf__StallDetection=true (see DEVIATION lines).
- STALL CLOCK: default wall, window 240 WALL s, floor 60 s after dispatch. Unchanged.
- ROUTE SHIFT: default ON (RL-20260920-01 item 3). Nothing passed.
- TIMED COMPLETION true and TASK CLOCK sim: code and json defaults [V: appsettings.json:44,46]. Nothing passed.
- NO -StopWhenComplete: --no-stop-when-complete, window --run-secs 1800.
- SUCCESSOR-GATE FLOOR: --env Vrf__TaskPredecessorTimeoutSeconds=60, overriding the wrapper's 7200
  (scripts/RunScenario.sh:308 [V]); the wait becomes max(60, 420 + 60) = 480 s for T_CC_CO1H.

## 2. The order - data\COMPLETION_CONFIRM_Order.xml (committed with this prereg)

Coordinates and performers are copied verbatim from data\R9_Mojave_UnitMove_Order_NoComments.xml [V: diff of every
Latitude/Longitude/PerformingEntity line, 2026-09-26]: 670cfdb2... = 1.BdeHQ (R9 T_R5_TK1); 139aa71b... =
114.MechCoy (T_R5_CO1); 001aa71b... = 1222.MechPlt (T_R5_PL1). The follow-on pattern (STREND +
RelativeTime/IntervalEndTime) is COA-STP1_Order.xml's. The file is 7,503 bytes, 181 CRLF lines, 0 non-ASCII, 0 blank
lines, 0 comments [V, each gated on a dirty control first]. Its tasks, in file order:

| # | name | uuid | performer | verb | geometry | Duration | start |
|---|---|---|---|---|---|---|---|
| 1 | T_CC_TK1 | c3000000-0000-0000-0000-000000000001 | 1.BdeHQ | MOVE | 2 vertices (R9 TK1 leg) | P00Y00M00DT00H08M00S (480 s) | delay 0 |
| 2 | T_CC_TK1H | c3000000-0000-0000-0000-000000000002 | 1.BdeHQ | DEFEND | none | P00Y00M00DT00H05M00S (300 s) | STREND after #1 |
| 3 | T_CC_CO1 | c3000000-0000-0000-0000-000000000003 | 114.MechCoy | MOVE | 2 vertices (R9 CO1 leg) | P00Y00M00DT00H07M00S (420 s) | delay 0 |
| 4 | T_CC_CO1H | c3000000-0000-0000-0000-000000000004 | 114.MechCoy | DEFEND | none | P00Y00M00DT00H06M00S (360 s) | STREND after #3 |
| 5 | T_CC_PL1 | c3000000-0000-0000-0000-000000000005 | 1222.MechPlt | MOVE | 2 vertices (R9 PL1 leg) | none | none |

Planned timeline (SIM s from each task's own DISPATCHED line; the three roots dispatch within seconds of each other):
- T_CC_TK1: arrives about 81 (D8) -> HELD -> TASKCMPLT at 480 -> T_CC_TK1H dispatched, ends at +300 (about 780).
- T_CC_CO1: OVERDUE at 420 -> arrives about 660 -> TASKCMPLT on arrival -> T_CC_CO1H dispatched, ends +360 (~1,020).
- T_CC_PL1: no end time -> completes on evidence at about 485 (D8 485.5).
- Route shift: a fresh build's tile cache may force HTTP tile fetches of up to 30 wall s before a first ground
  dispatch [A]. That shifts dispatch, not the timer (the timer arms at dispatch).

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: not used - the order is pushed after the runner's own Stage 7 oracle gate and the interface's READY
TO TASK barrier (see DEVIATION FROM RECORD).

DurationScale: 1.0

ARMED ENDS VS STALL WINDOW: DurationScale is 1.0 (no compression). Armed ends in SIMULATION s: T_CC_TK1 480 and
T_CC_CO1 420 (destination tasks, both above the 360 SIM s calibrated sim window; the watchdog in this run is on the
WALL clock, 240 wall s). T_CC_TK1H 300 and T_CC_CO1H 360: no destination, never watched (MarkDispatched records no
destination). T_CC_PL1: none. An unarrived destination task goes OVERDUE and stays watched, so no armed end can
pre-empt a stall verdict.

DEVIATION FROM RECORD: no --pre-order-gate - RUNBOOK 0.5.14 item 12 says "HOLD THE ORDER UNTIL THE NAVIGATION AREA IS READY - `--pre-order-gate nav-area`, not a guessed settle." R9_Mojave_Empty_52 has no registered navigation area on this machine, so the row the gate waits on cannot print and the gate would stop the run at its timeout; the authored legs are Move Along Route, which plans no path (UG52 23.3 p505).

DEVIATION FROM RECORD: DEMO_READINESS row 25 says "Demo rule: every ground order runs on a fixture WITH nav data behind the nav-area READY gate." This is a confirmation run, not a demo, and no fixture with navigation data exists on the rebuilt machine.

DEVIATION FROM RECORD: the successor-gate floor is 60 s, not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:308); with 7200 a follow-on waits two hours whatever the code does, so the late-arriver check "not skipped at end + margin" could not fail.

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay, and loading it would also change the application number, connection config and console levels - the one key is exactly appsettings.Demo.json's "StallDetection": true.

DEVIATION FROM RECORD: no throw-away warm-up run - DEMO_RUNBOOK sec 0.4 says "you must warm the terrain BEFORE the demo initialization"; this is not a demo, the re-clamp checks can only be exercised on a cold area, both worlds are registered in P8, and P0 makes a unit that does not move a VOID run rather than a code verdict.

## 3. What the code emits - log-line shapes, re-verified at a9d738f

All lines are in <rundir>\vrfc2simapp.log (no timestamps; line ORDER is the clock). Line numbers are
src\VrfC2SimApp\VrfC2SimService.cs at a9d738f [V: grep -F of each fixed fragment, 2026-09-26].
- L-DISP (:5070): `DISPATCHED <unit> task '<task>' (<kind>) at WALL <stamp>, SIMULATION clock <s> ...`
- L-STRT (:8178 SENT): `SENT TASK STATUS REPORT (TASKSTRT) taskee=<uuid> task=<uuid> - dispatched to <unit> as '<kind>'.`
- L-ARM-D (:5197-5202): `Task '<task>': end time armed at <S> s from dispatch (...) - it has a destination: ...`
- L-ARM-N (:5205): the same line with `- it has no destination: it completes at this end time ...`
- L-NODUR (:5188): `Task '<task>': the order gives NO Duration, so this task has no end time - ...`
- L-INPLACE (:4524): `Task '<task>' carries NO geometry: executing IN PLACE at <unit>'s own position ...`
- L-GATE (:3945): `Task '<task>': gated on <pred>, which IS a task in this order and is armed to end <E> s after ITS
  dispatch. It has <D> s to DISPATCH (...) and then <T> s to COMPLETE; the configured
  Vrf:TaskPredecessorTimeoutSeconds=<cfg> s (+<m> s margin) is the floor. ...`
- L-ARRIVE (:6436/:6481): `ARRIVAL EVIDENCE: <unit> task '<task>' - ...`
- L-HELD (:7739): `Unit <unit>: task '<task>' FINISHED BEFORE ITS END TIME - the TASKCMPLT and the release of its
  follow-on tasks are HELD until start time + Duration ...`
- L-END (:6876): `TIMED COMPLETION: task '<task>' on <unit> reached its END TIME - <served> s of a <D> s Duration
  served on the simulation clock (...)<suffix>.` - suffix (:6882) ` - the unit had already arrived; its completion
  was held until now` for a held destination task, empty for a no-destination task.
- L-OVERDUE (:6865, WARNING): `TIMED COMPLETION: task '<task>' on <unit> reached its END TIME - ... but the unit has
  NOT ARRIVED: OVERDUE. No TASKCMPLT is sent now; ...`
- L-IDLE (:6893): `TIMED COMPLETION: <unit> is idle again - its in-place task '<task>' issued no VR-Forces task ...`
- L-CMPLT-T (:6897): `SENT TASK STATUS REPORT (TASKCMPLT) ... - task '<task>' reached the end time given by its
  C2SIM Duration (<D> s after dispatch).`
- L-LATE (:7744): `Unit <unit>: task '<task>' was OVERDUE and the unit has now ARRIVED - reported complete now, ...`
- L-CMPLT-L (:7785): `SENT TASK STATUS REPORT (TASKCMPLT) ... - unit <unit> arrived after its task's end time (start
  time + Duration) - complete on arrival[; the deferred engage has been issued].`
- L-CMPLT-E (:7791): `SENT TASK STATUS REPORT (TASKCMPLT) ... - unit <unit> completed its task.`
- L-SKIP (:4007 WARNING, :4015 SENT): `... -> NOT dispatched.` and `SENT ... (TASKABRT) ... - SKIPPED: predecessor
  <uuid> <why>; policy=skip.`
- L-STALL (:7190): `STALL: unit <unit> task <task>: no member moved more than 50 m in the last <W> <clock> s (max
  <m> m); TASKABRT reported.` then `SENT ... (TASKABRT) ... - STALLED (C16 progress watchdog) - report only ...`
- L-CANCEL (:8160): `TIMED COMPLETION: the end time armed for task <uuid> is cancelled - <code> reached ...`
- L-SUPP (:8172): `TASK STATUS <code> for task <uuid> SUPPRESSED by the emission rules ...`
- Engage fallback family (item 5): `via fallback` (:5345), `is DROPPED` (:5317), `is STUCK` (:5332),
  `issuing the deferred` / `the deferred engage has been issued` (:7763/:7786).
- L-WDOG (:612): `PROGRESS WATCHDOG ON (C16, report-only): ...`
- L-CLOCK (:6847): `TIMED COMPLETION: <n> task(s) are timing out against the SIMULATION clock; Vrf:DurationScale=1.`
- L-RATIO (:6608): `SIM/WALL RATIO ...` (per minute).
- L-PLACE (:3251): `PLACEMENT summary: <T> of <N> create altitude(s) came from the TERRAIN QUERY, ...`
- Re-clamp lines (PlacementReclampPolicy.cs, prefix `PLACEMENT RE-CLAMP`): L-RC-PASS `gate: <unit> is ON the
  terrain ...`, L-RC-OFF `gate: <unit> measured OFF the terrain at dispatch ...`, L-RC-SUM `summary: ...`.
- RETIRED lines that must not appear at all: `NOT DISPATCHED YET`, `HELD as`, `REFUSED [`,
  `BOUND-BUT-NOT-ON-THE-GROUND`.

## 4. Sequence and exact command lines

Everything runs from Git Bash at the repo root C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\
Software\Interfaces\VRF_C2SIM (the MAIN checkout, main a9d738f), because the runner resolves its exes, tools and
ledger from its own repo root. The committed record (this file, the order, the ledger) lives on branch
run/completion-confirm-2026-09-26; the main checkout's working-tree OPUS_EXECUTION_PLAN.md receives the SAME ledger
text (copied from the branch) before the holder, so the runner's marker read sees 5107, and the runner's own block
is carried back to the branch after the run. The order is passed to the runner by the absolute path of the branch
worktree's committed file (sha256 recorded in the BUILD RECORD).

A. Read-only checks (done 2026-09-26 11:24-11:35Z, lane R report): process inventory; licence line; vrf-sms; fixture
   hash; REST 200 on 18080. At launch time (step E) also: `echo "$DOTNET_ENVIRONMENT"` empty, `env | grep '^Vrf__'`
   empty, NO vrfNavGenerator / dotnet build / MSBuild / VBCSCompiler / vrfSim / vrfGui / VrfC2SimApp process.

B. Build + deploy (main checkout, clean tree at a9d738f, BEFORE any ledger edit there): the eleven 5.2 consumers
   (src/VrfC2SimApp, src/SmokeTest, tools/CreateOne, tools/RtiProbe, tools/RunSim, tools/SetAlt, tools/WatchVrf,
   tools/CreateTaskAgg, tools/ResetVrf, tools/SetSimRate, tools/PauseSim) with
   `dotnet build <csproj> -c Release -p:BridgeConfig=Release-5.2 -t:Rebuild`, against the pinned bridge (no native
   rebuild - bridge source unchanged since the pin). Check the OUTPUT TREE: eleven bin\Release-5.2 VrfBridge.dll
   copies at 90272bc9...f25847. Then `dotnet build-server shutdown`. Then, under DOTNET_ENVIRONMENT=Demo,
   `VrfC2SimApp.exe --runtime-check` -> "runtime-check: OK". Record VrfC2SimApp.exe/.dll sha256 and ProductVersion.

C. Validate the order (no federate joins, no appNumber):
   - C1 offline parse: `VrfC2SimApp.exe --parse-order <order>`. EXPECT 5 tasks; Durations 480/300/420/360 s and one
     with none; T_CC_TK1H after c3...01 and T_CC_CO1H after c3...03 as STREND; ZERO schema warnings.
   - C2 bytes (above, each gated on a dirty control).
   - C3 ONE real push to the PRIVATE server with no interface running:
         tools/PushInit/bin/Release/net10.0/PushInit.exe data/R9_Mojave_Lean_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
         tools/PushOrder/bin/Release/net10.0/PushOrder.exe <order> 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
     EXPECT both exit 0; PushOrder not exit 4 (BML symptom); its capture shows one `ORDER (<n> chars)` echo. Any failure
     = STOP: fix the FILE (never the parser), re-register, push again.

D. WAIT for lane N5's vrfNavGenerator to exit (one polling script, 60 s sleep inside its own loop, up to 90 min; still
   running after that = STOP and report). Then rtiexec, only if not up (it was DOWN at step A; starting is not a
   restart): `"/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartRtiExec52.ps1`. EXPECT
   `RTIEXEC READY ... tcp=...:4001`.
   The persistent holder FIRST (RUNBOOK 9c; DEMO_RUNBOOK sec 0 step zero), numbers claimed in Appendix B (sec 5):
       "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5103,5104,5105,5106 -SettleSecs 28800 -WhatIf
       "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5103,5104,5105,5106 -SettleSecs 28800 < /dev/null > runs/launch52/cc-holder-<stamp>.log 2>&1
   EXPECT `HOLDER JOINED: pid ... appNo 5103`, exit 0.

E. Dry run of the exact command plus --dry-run (launches nothing). Then THE RUN, once, stdout to a FILE, never piped:

       scripts/RunScenario.sh \
         --scenario R9_Mojave_Empty_52 \
         --init data/R9_Mojave_Lean_Initialization.xml \
         --order <absolute path of the committed order> \
         --client-id STP \
         --no-stop-when-complete \
         --run-secs 1800 \
         --env Vrf__StallDetection=true \
         --env Vrf__TaskPredecessorTimeoutSeconds=60 \
         --sample-threads \
         --log runs/launch52/RunScenario-completion-confirm-<stamp>.log

   Defaults kept: --profile 5.2, --no-gui, --object-console 4, --member-console 4, --backend-notify 3,
   --position-report 10, REST/STOMP 18080/61614, no --duration-scale (app 1.0), the runner's Stage 2h holder on (it
   JOINS the federation the persistent holder keeps).

QUIET PERIOD (RUNBOOK 0.5.14 item 5): from step E until the post-run inventory, no Stop-Process / taskkill of any
kind, no build, no subagent or other agent session started by this lane, no second runner. The executor polls the
run's own markers from the foreground with bounded waits. The WS-runaway tripwire (3 alerts) aborts on its own.

## 5. Application numbers

The Appendix B marker read `*** NEXT FREE: 5103 ***` at a9d738f and on every local branch [V 2026-09-26].
This run claims 15 numbers, 5103-5117, and leaves the marker at 5118.
- Persistent holder (hand claim, step D; written into Appendix B and the marker advanced to 5107 IN THIS COMMIT):
  5103-5106. Expected: 5103 CONSUMED on a first-attempt join; 5104-5106 BURNED.
- Runner block (the runner writes it itself at Stage 2), in its own allocation order:

| appNo | what | expected |
|---|---|---|
| 5107 | back end (vrfSimHLA1516e) | CONSUMED |
| 5108 | front end (vrfGui) | BURNED - --no-gui |
| 5109 | WatchVrf pre-init oracle pre-check | CONSUMED |
| 5110 | WatchVrf main run trace | CONSUMED |
| 5111 | VrfC2SimApp - the interface | CONSUMED |
| 5112 | RtiProbe Stage 2c readiness gate | CONSUMED |
| 5113 | CreateOne Stage 7b diagnostic | BURNED unless the oracle gate fails |
| 5114-5117 | RtiProbe Stage 2h holder attempts 1-4 | attempt 1 JOINS (5114); 5115-5117 BURNED |

PushInit / PushOrder / ListenReports / StopIface are C2SIM REST/STOMP clients, not federates, and use no number. A
launch that aborts burns its whole block; a relaunch needs a NEW registration and new numbers.

## Predictions (written BEFORE the run; a missed HIGH prediction is a STOP, not an adjustment)

Source: lane M4's FINAL live-run assertion list, items 1-9 with item 5 (i)-(v) and the displacement rule, each as one
row below, plus the run's own precondition / health / clock rows (P0, P10, P11). Every count is PER TASK UUID, read
from vrfc2simapp.log in line order (NOT from the runner's coverage line) and cross-checked against
reports-captured.log. Each falsifier says what the log will look like; it says nothing about what to build.

| # | Prediction | Confidence | FALSIFIER (what counts as a MISS) | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) Holder joined; the back end JOINS (no create); READY. (ii) 6 of 6 init units; L-WDOG once; L-CLOCK names SIMULATION and DurationScale=1. (iii) 3 roots get L-DISP + L-STRT. (iv) The last L-PLACE line is present (it names the P8 world). (v) Each of the 3 movers is displaced more than 50 m within 360 SIM s of its L-DISP (WatchVrf trace). | HIGH | Any limb failing = VOID + STOP (a launch, terrain or harness failure, not a code verdict). | |
| P1 | M4 ITEM 1, EARLY ARRIVER T_CC_TK1: L-ARM-D at 480; then ARRIVAL EVIDENCE or a vendor completion; then exactly one L-HELD; NO SENT TASKCMPLT for c3...01 before its L-END carrying "had already arrived; its completion was held until now"; exactly one SENT TASKCMPLT for c3...01 after that L-END ("(480 s after dispatch)"); T_CC_TK1H's L-DISP and L-STRT only AFTER that TASKCMPLT. | HIGH | A TASKCMPLT for c3...01 before its L-END; no L-HELD; two TASKCMPLTs; T_CC_TK1H dispatched before it; any L-OVERDUE naming T_CC_TK1. | |
| P1t | T_CC_TK1 arrival lands 60-250 SIM s after its L-DISP (D8 81.1 s plus route-shift delay). | MEDIUM | Outside 60-250 (recorded; at or past 480 = P1 VOID, not missed). | |
| P2 | M4 ITEM 2, LATE ARRIVER T_CC_CO1 -> T_CC_CO1H: L-ARM-D at 420; exactly one L-OVERDUE for T_CC_CO1 and NOTHING SENT for c3...03 at its end time or between that L-OVERDUE and the arrival; then arrival evidence or a vendor completion, then L-LATE, then exactly one SENT TASKCMPLT "arrived after its task's end time (start time + Duration) - complete on arrival". T_CC_CO1H: L-GATE reads "Vrf:TaskPredecessorTimeoutSeconds=60 s" and "480 s to COMPLETE"; NO "SKIPPED: predecessor c3000000-0000-0000-0000-000000000003" at end + margin; its L-DISP / L-STRT only AFTER CO1's TASKCMPLT; its own armed line reads "end time armed at 360 s". CONDITION: no L-STALL on 114.MechCoy; if one fires, the follow-on limbs are VOID (D2 abandons them by design) and P4 governs. | HIGH | A TASKCMPLT for c3...03 at its end time; zero or two L-OVERDUE; a SKIPPED for CO1H without a stall; CO1H armed with anything but 360; a second TASKCMPLT for c3...03. | |
| P2t | T_CC_CO1 arrives 580-760 SIM s after its L-DISP (D8 660.5 +/- 12 percent plus route-shift delay). | MEDIUM | Arrival at or before 480 SIM s (then "not skipped at end + margin" was never tested: VOID, not HIT). | |
| P3 | M4 ITEM 3, NO-DESTINATION TASKS T_CC_TK1H (300 s) and T_CC_CO1H (360 s): each prints L-INPLACE and an armed line with "it has no destination"; then one L-END with no suffix; then exactly one SENT TASKCMPLT at its end time ("(300 s after dispatch)" / "(360 s after dispatch)"); ZERO L-OVERDUE naming either. | HIGH | An OVERDUE for a hold task; zero or two TASKCMPLTs; a TASKCMPLT before its end time; a hold dispatched as a move (L-ARM-D for it). | |
| P3b | Each hold is followed by L-IDLE; ZERO L-HELD lines name either hold task (an L-HELD would mean a late vendor completion of the previous move was attributed to the hold - a finding). | MEDIUM | An L-HELD naming a hold task; a missing L-IDLE. | |
| P4 | M4 ITEM 4, STALL ON A PLAIN MOVER: ZERO L-STALL lines expected (240 s wall window + 60 s floor exceed every mover's expected wall travel; holds are not watched). IF any L-STALL fires, these limbs become HIGH: (a) the STALL line and one SENT TASKABRT "STALLED (C16 progress watchdog) - report only", not suppressed (no L-SUPP for it); (b) NO L-CANCEL ("end time ... is cancelled - TASKABRT") for that abort; (c) each waiting follow-on "SKIPPED" with its own SENT TASKABRT (D2); (d) a SENT TASKCMPLT for that task only after a real arrival. Every stall on a unit that later arrives is COUNTED as a false alarm under D2; for every stall the member displacement over the preceding window and since dispatch is recorded from the trace. | MEDIUM on "zero"; HIGH on (a)-(d) if one fires | Zero-limb: any L-STALL (a finding, not a stop). Conditional limbs: any of (a)-(d) failing = STOP. | |
| P5 | M4 ITEM 5, ATTACK/BREACH WITH AN APPROACH MOVE - EVERY one classified into exactly one of (i) move completed before the end time (TASKINPRG "completed the MOVE half", engage issued, one TASKCMPLT at the end time); (ii) move completed after the end time (one TASKCMPLT on arrival with "the deferred engage has been issued"); (iii) engage fallback on a MOVING/MOVED unit ("via fallback", the "replaced its move" WARNING naming the verdict, one TASKCMPLT at the end time, follow-on released after it); (iv) engage fallback on a STUCK unit (a) watchdog first / (b) judged at the fallback / (c) stays-put, each with the STALL line + SENT TASKABRT, no engage, no TASKCMPLT at or after the end time, follow-ons SKIPPED; (v) superseded ("is DROPPED", no engage for the old task). DISPLACEMENT RULE: for EVERY fallback, the member displacement over the preceding watchdog window AND since dispatch is recorded from the WatchVrf trace; a (iii) unit that did not move over the window is the stated residual and is counted, not passed; no engage while the unit's current task is another (M3-1); an arrival within one tick of its fallback with no engage is M4-1 and is counted. THIS ORDER HAS NO ATTACK OR BREACH, so the prediction is: ZERO item-5 instances - zero `via fallback`, `is DROPPED`, `is STUCK`, `issuing the deferred`, `the deferred engage` lines and zero SENT TASKINPRG. Item 5 is NOT EXERCISED. | HIGH | Any such line: the order that ran is not the one registered = STOP. | |
| P6 | M4 ITEM 6: ZERO L-SUPP for a TASKCMPLT or TASKABRT (in particular none after a timer line); exactly one SENT TASKCMPLT per task uuid c3...01-05 (5 in all) and exactly 5 SENT TASKSTRT. reports-captured.log holds the same 5 + 5 task-status bodies, 0 TASKABRT absent a stall, 0 failed pushes. (An L-SUPP for a re-entered TASKSTRT is allowed.) | HIGH | Any duplicate terminal report; any L-SUPP for a terminal code; capture and log disagree; any failed push. | |
| P7 | M4 ITEM 7, NO DURATION T_CC_PL1: L-NODUR once; ZERO "end time armed" lines and zero TIMED COMPLETION lines naming it; completion on evidence only - ARRIVAL EVIDENCE or a vendor completion, then one SENT TASKCMPLT "unit <1222.MechPlt> completed its task". | HIGH | Any end time armed for c3...05; a TASKCMPLT for it with a Duration-based reason. | |
| P7t | T_CC_PL1 completes 427-544 SIM s after its L-DISP (D8 485.5 +/- 12 percent). | MEDIUM | Outside the band (recorded). | |
| P8 | M4 ITEM 8, RE-CLAMP, read from the LAST L-PLACE line. BOTH worlds (HIGH): ZERO `NOT DISPATCHED YET`, `HELD as`, `REFUSED [`, `BOUND-BUT-NOT-ON-THE-GROUND`. WARM (`N of N ... from the TERRAIN QUERY, 0 from the FALLBACK`): the re-clamp never arms; item 8 is VACUOUS, recorded NOT EXERCISED. COLD (any FALLBACK create) (HIGH): every "measured OFF the terrain at dispatch" line is followed by that task's dispatch with no TASKABRT for it; the LAST "PLACEMENT RE-CLAMP summary" has five counts summing to "(<n> enrolled)"; its "CORRECTED, READ-BACK NOT RECEIVED" count equals the correction lines with no RE-CLAMPED AND VERIFIED line for the same object. Which world: WARM expected. | HIGH on the retired-line limb and the cold-world limbs; MEDIUM on which world | A retired line; a TASKABRT after an L-RC-OFF; counts that do not sum or do not agree. | |
| P9 | M4 ITEM 9: the runner's terminal count equals a grep of the FIRST `SENT TASK STATUS REPORT \((TASKCMPLT\|TASKABRT)\)` per task uuid over vrfc2simapp.log: 5 of 5, "5 TASKCMPLT + 0 TASKABRT" absent a stall; the SENT line format is unchanged. Runner count: manifest oracle.earlyExit (terminalByCode / tasksClosed) if written; else RunnerLib's parser replayed offline over the log [A: that the manifest carries it with the early exit off]. | HIGH | The two counts differ. | |
| P10 | RUN HEALTH: exe and bridge sha256 identical before and after; no `BACK END LOST`; no WS-runaway exit 6; no new .dmp / .callstack.log for this back-end pid (names and mtimes only); teardown clean - no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left; rtiexec, rtiForwarder and the holder(s) still up and untouched. | HIGH | Any of these. A back-end fault is VOID + new STP-854 evidence, not a completion verdict. | |
| P11 | Clock: every per-minute `SIM/WALL RATIO` line reads 2.6-4.7x (N11; no magnitude is claimed); T_CC_CO1H's TASKCMPLT lands 1,000-1,200 SIM s after T_CC_CO1's L-DISP; the settle window after it is at least 600 wall s. | MEDIUM | Ratio outside the band, or a settle under 600 wall s (then any "later TASKCMPLT after an abort" limb is CENSORED). | |

STOP RULES:
- A missed HIGH row is a STOP. Record it, no patch, no rerun under this registration, adjust nothing.
- P0 or P10 failing makes the run VOID. Two identical launch failures in a row: do not launch a third (RUNBOOK 9c).
- The operator never intervenes in the window. A live read is for watching only; no verdict is written from it.
- A VOID or STOPPED run is re-registered as COMPLETION_CONFIRM-<date>-2, with new appNumbers.

ONE VARIABLE: none - this is the confirmation run of a code unit, not a single-variable experiment. Its HIGH rows are
log-structure predictions that need no control. The comparator for the MEDIUM timing rows is D8
(runs\20260921T052350Z_run), with D9 and D5c; they differ from this run in the binary, the order's Durations /
follow-ons / holds, stall detection, the successor-gate floor and the rebuilt machine, which is why no timing row is
HIGH.

## 6. The settle window

1,800 wall s from PushOrder returning (the stage-8b t+ clock), with no early exit. Everything after the last planned
terminal report is settle (about 1,380-1,580 wall s at the recorded ratios). It exists to catch a TASKCMPLT that
follows a stall abort, a duplicate or late vendor completion attributed to a hold (P3b), and position reports
continuing after the last completion.

## 7. Run conditions to record (in the Result block)

- SIM CLOCK SOURCE: the task clock as L-CLOCK prints it (expected SIMULATION, DtVrfRemoteController::simTime); the
  watchdog clock as its own line prints it (expected WALL); every SIM/WALL RATIO line; the fixture's frame mode
  (fixed-frame-run-to-complete, 0.033333 s); the SIMULATION stamp of each L-DISP.
- MACHINE LOAD: CPU at launch and the --sample-threads CSV (+ alerts); no agent session started by this lane from
  step E to the post-run inventory; no dotnet build / MSBuild / VBCSCompiler / vrfNavGenerator at launch; no vrfSim /
  vrfGui / VrfC2SimApp at launch. A run started under any other condition is VOID.
- OTHER SESSIONS ON THE BOX: the operator's c2sim-server on 8080/61613 (untouched); `docker ps`; rtiexec and
  rtiForwarder pids and start times; every rtiAssistant pid (30240 seen - never killed); the holder's pid and appNo
  plus any other RtiProbe; other Claude Code / IDE sessions.
- BUILD: main HEAD a9d738f; VrfC2SimApp.exe/.dll ProductVersion and sha256 before AND after; VrfBridge.dll sha256
  against the pin; MAK-ONE-2025-Config.xml sha256 (f445629e...b5e1e at registration [V]).
- ENVIRONMENT: DOTNET_ENVIRONMENT unset for the run; the Vrf__* set the app received (manifest env record + the app's
  start-up lines); internet reachable; the licence-expiry line.
- TERRAIN AND TILES: the LAST L-PLACE line (the world); the preflight tile cache before/after and its TILE CENSUS line.
- The runner's exit code and legend; watchdog.pid and runner.launched contents; the post-run inventory.

## 8. Harvest (after the run, read-only) and where results go

- WHERE: the run directory named in runs\launch52\last-run-dir.txt: vrfc2simapp.log, reports-captured.log,
  c2sim-bus.log, the manifest, the WatchVrf trace, thread-samples.csv + alerts, holder logs, plus the wrapper log.
  Vendor sim logs dump the whole environment in cleartext: never attach or quote one; count-grep only; on a crash,
  only the .callstack.log / .dmp names.
- HOW: one grep pass over vrfc2simapp.log for the sec 3 shapes, then per-uuid tables (c3...01-05): each SENT code in
  line order, the SIMULATION stamp of L-DISP and of the completion evidence; cross-check reports-captured.log; WatchVrf
  displacement per mover (P0 v) and for every stall / fallback (P4, P5). SIM figures are read from the app's own
  lines, never computed from two wall figures (RUNBOOK 11f).
- RESULTS GO TO: (1) the Result block below (verdict per row; measurement and design implication in SEPARATE
  sentences, CLAUDE.md sec 4); (2) DEMO_READINESS rows 19 and 26 (row 26's "Owed" is discharged ONLY if P4's
  conditional limbs ran) and the HANDOFF sec 6 (200 x 160 cap), each with a dated line; CORRECTIONS_LOG only if
  something on record is refuted; (3) Appendix B: 5103-5117 annotated CONSUMED / BURNED from the manifest and the
  holder log. ASCII + CRLF throughout.

## 9. What this run does NOT claim

- Nothing about the attack / breach engage fallback (item 5): the order has none, so P5 is NOT EXERCISED.
- Nothing about a genuinely stuck unit unless one happens; no stuck leg on this laydown exists in the record. DEMO_
  READINESS row 26 ("it must show the stall TASKABRT sent, not suppressed") stays OWED if P4's conditional limbs did
  not run.
- Nothing about re-clamp behaviour in a WARM world (item 8 vacuous), nor about placement beyond the L-PLACE line.
- Nothing about back-end loss aborting held tasks, the supersede-as-TASKCMPLT path (non-default
  Vrf:SupersededTaskCode), or the demo profile as a whole (only its StallDetection key is used).
- Nothing about navigation meshes, abstract graphs, the _AG model set, COA-STP1, long legs or any other AO.
- Nothing about whether a task's desired effect was achieved, nor about the doctrinal correctness of these Durations
  (test timings, not doctrine).
- No timing generalisation: n = 1, one host, one fixture, a load-dependent clock.
- STP itself driving the interface: owed to a session with the owner at the box.
- No claim that the temporary position is final: it is TEMPORARY by the owner's own word.

## BUILD RECORD (steps B and C, 2026-09-26, committed BEFORE the holder and the run)

CORRECTION (same day, before the run): the STATUS and DATE lines above read "2026-09-26T11:45Z"; that stamp was
written ahead of the commit and is wrong. The registration commit b0ab078 is dated 2026-09-26T11:32:53Z [V: git log].
The ordering claim is unchanged: registration preceded the build (below, from 11:34:33Z) and everything after it.

B. BUILD + DEPLOY [V]. Main checkout at a9d738f, tree clean (untracked only: a .code-workspace, grep.exe.stackdump,
tools/analysis/__pycache__). `dotnet build <csproj> -c Release -p:BridgeConfig=Release-5.2 -t:Rebuild` for the eleven
consumers, 11:34:33Z-11:35:42Z: eleven "Build succeeded", 0 errors, VrfC2SimApp 6 warnings (4x CA2024, 2x CS8632 - the documented set), the
other ten 0. OUTPUT TREE: all eleven `bin\Release-5.2\net10.0\win-x64\VrfBridge.dll` = 90272bc95297e330... (the pin),
each consumer's own .dll freshly written 11:34:54Z-11:35:41Z. `dotnet build-server shutdown` done. No native rebuild
(bridge source unchanged since 13e8c73). Deployed app, src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64:
- VrfC2SimApp.dll sha256 7b3554a5fbf4da4c2dbb00d39ea855d527b173df8ede74cbc7de26bae97b8787, ProductVersion
  1.0.0+git.a9d738f.Release-5.2 (no +DIRTY);
- VrfC2SimApp.exe (apphost) sha256 acbb5095330ae565ef58ca3643ba66fa34dd1a3cf6ef65561f007296fd25bbb5, same version;
- appsettings.json byte-identical to src (sha256 2ddbaba3...aae45a);
- `DOTNET_ENVIRONMENT=Demo VrfC2SimApp.exe --runtime-check`: "VrfBridge loaded; native stack =
  5.2|C:\MAK\vrforces5.2d\bin64\vrfcontrol.dll", "runtime-check: OK", exit 0.

C. ORDER VALIDATION [V].
- C1 `--parse-order` on the committed order: exit 0; "Tasks: 5"; durations 480000 / 300000 / 420000 / 360000 ms and
  "(none)" for T_CC_PL1; T_CC_TK1H startAfter=c3...0001 and T_CC_CO1H startAfter=c3...0003; 3 Route shapes (578 / 557 /
  578 m), 2 tasks with no geometry; 0 lines matching warn|schema|error.
- C2 bytes: see sec 2 (each check gated on a dirty control first).
- C3 one real push on the PRIVATE server, no interface running: PushInit exit 0 (RUNNING -> INITIALIZING -> "push
  result : OK Message processed successfully" -> RUNNING; "QUERYINIT : 6 Units, SystemName=[STP]"); PushOrder exit 0
  ("pushing order: ... (7503 chars)", "[11:37:27.069] ORDER (6580 chars)", "push result : OK Message processed
  successfully", 1 bus message captured). The echo is the server's re-serialised OrderBody (no MessageBody wrapper,
  2-space indent), so its character count differs from the file's by construction; it carries all 5 <Task> elements,
  the 4 Durations, 2 STREND relations and ends with </OrderBody>. No BML reply (not exit 4), no truncated echo.

## Result (written after the harvest, never from a live read)

(pending - to be written after the harvest)
