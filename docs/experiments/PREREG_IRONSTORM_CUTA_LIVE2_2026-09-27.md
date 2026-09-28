# PREREG - IRON STORM CUT A, SECOND LIVE RUN (maple nav area, T14 round the lake, T10 off the pond, one scored run)

STATUS: REGISTERED 2026-09-27 (the registration commit's own time is the stamp; see git log), BEFORE the order
validation push, the holder check and the scored run. Written by lane IS2run (session 5fc25950) on branch
run/ironstorm-cuta-live2 from main 6ab78f2, from the -1 registration (PREREG_IRONSTORM_CUTA_LIVE1_2026-09-27.md, whose
structure and settings are carried over) and the seat's -2 revision list. Marks: [V] = checked while writing this file;
[A] = taken from the record, not re-checked.

## Registration

PREREG ID: IRONSTORM_CUTA_LIVE-2026-09-27-2
DATE (UTC): 2026-09-27, before any launch (the registration commit's own timestamp is authoritative)
BINARY / COMMIT: main 6ab78f2, deployed build 80f707f. NO REBUILD: `git diff --stat 80f707f..6ab78f2 -- src` is EMPTY
[V]. Deployed src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64: VrfC2SimApp.exe sha256
493a07a89e43daaa8410bc489f0ba12816bec0a342219ff2e52f7829f739f31d and VrfC2SimApp.dll
c86901be4e1f5f4effabd6a9022f0dbad8c4e4088777c1186e066ff4e802d74c, ProductVersion 1.0.0+git.80f707f.Release-5.2 (no
+DIRTY); appsettings.json 2ddbaba3...aae45a = src; VrfBridge.dll 90272bc95297e330...f25847 (the pin) [V 2026-09-27 01:59Z].
Fixture C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Nav_AG_maple.scnx sha256 57465c35...0e32 [V].
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: UG52 sec 23.3 p505 (Move Along Route: "There is no path planning done as it moves toward the next
vertex"; it "does avoid obstacles as it moves") and sec 23.2 / 23.2.1 p500 (only Move To plans a path); UG52 23.5.1
p505-507 and Table 26 (soil drives mobility; deepLake -> deep-water; acceleration-factor 0.0 = "the surface's drag
prevents the vehicle from moving at all"), ground-tracked.sysdef:813-815; UG52 68.3.3 p1312 / 68.3.4 p1313 (a derived
SMS's scripts supersede the included set's - the proof line); UG52 Appendix C p1671 (loadAllNavigationDataOnTerrainLoad -
navigation data otherwise loads lazily, after placement); UG52 23.6 p508 (autonomous actions gate path planning; ON by
default); UG52 40.69 p896-897 Rules of Engagement ("Hold Fire. Do not fire at targets."); singleTaskControllerComponent.h
:191-200 decideToGiveUpTask "always returns false" (only this interface's watchdog ever reports a stuck unit).

OWN-RECORD CITATION:
- IRONSTORM_CUTA_LIVE-2026-09-27-1 (PREREG_IRONSTORM_CUTA_LIVE1_2026-09-27.md): STOPPED at its step-W gate on StopVrf
  exit 7, nothing scored; its Result block records the unscored pre-warm (run 20260927T003120Z) and its dated CORRECTION:
  48 IBCT drove ~850 m and stopped dead at 54.026779, 23.317195, and the STALL line's "(max 0.0 m)" is the 360-SIM-s
  sliding window, not the displacement since dispatch.
- FINDING_IRONSTORM_T14_STOP_2026-09-27.md: H1 (OSM natural=water, way 197345448, the lake Jezioro Wiersnie, top layer
  of the vendor land-cover composite -> deep-water, acceleration 0.0) CONFIRMED on geometry + config with one [A] (the
  sim's soil query resolves the composite as its XML reads - not observed in a sim query); H1b FALSIFIED; H2 (obstacle
  avoidance deadlock) NOT FALSIFIED as a contributor. Its sec 5 names this run as H1's falsification test.
- data/IRONSTORM_CUTA_CHANGES.md changes (h), (i), (j): T10 via 54.029734 / 23.305499 (h) and 54.024000 / 23.313000 (j) to
  PassagePoint_48_IBCT_SLOT0, 2,775 m; T14 via 54.014600 / 23.331500 (i) to the unchanged (e) destination 54.040348 /
  23.324206, 4,169 m. Order sha256 7a9861372f07702fc136f91f63b8bf971650fcc91c20aa62803d73cfd869f5c7, init
  2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec [V: derive_ironstorm_cuta.py --check, 02:03Z, gates ALL
  PASS, both CHECK lines match]. `corridor_gate.py --preset ironstorm-cuta-j` PASS, 33 sectors, min 0.9000 [A].
- StopVrf52 since -1 (merged): a forced own back end still exiting at the deadline is exit 6, not 7; post-force wait
  -ForcedExitWaitSec default 60 s (scripts/StopVrf52.ps1:84-110, :582-609 [V]).
- RUNBOOK 0.5.14 items 2, 5, 12, 18; 9c (the persistent holder); 11f (the sim clock); 11h (re-clamp); DEMO_RUNBOOK sec
  0.4 (pre-warm).
- Rulings: RL-20260926-01 (A6 CNFPSL held in place, STP-866; FOLSPT advance and hold); RL-20260925-01 (D2/D3/D4);
  RL-20260921-09 (the temporary completion position); RL-20260921-06 (the placement re-clamp stays; the dispatch gate is
  measure-and-log only); RL-20260921-02 item 3 "3. Fast." (the sim clock); RL-20260920-01 item 3 (route shift ON by
  default); STP-846, STP-847, STP-866.

## 0. Purpose, in plain words

The -1 registration never reached its scored run (the pre-warm's teardown gate read exit 7). Its unscored pre-warm showed
28ID driving its 5.3 km leg and arriving, and 48 IBCT driving 850 m and then stopping dead at the edge of an OSM lake the
pre-flight had not read. The order now routes T14 round the lake and T10 off a pond. This run is the scored look: the
maple area before the order, the three movers displacing, T14 passing its new waypoint and reaching its destination (the
test of the T14 finding's H1), no mover's track entering OSM water, and every task ending by the time rules with clean
reports. A demo-readiness confirmation at n = 1, not an experiment.

## 1. Decisions taken from the record

(a) PERFORMERS - unchanged from -1 sec 1(a) [A]: 28ID = ONE M1A2 PLATFORM; 48 IBCT = one M577A2 PLATFORM; 1-112 IN = an
AGGREGATE of 6 members, created AtOrder. T02 is a platform ATTACK whose AffectedEntity is the performer itself (R3 self)
-> AdvanceOnly with ROEHold. No unit ATTACK in cut A: the fire-at-will line is PREDICTED ABSENT (P8).
(b) DurationScale 0.25 is plan P7 / CHANGES.md "recommended", a plan recommendation, not a ruling on file (as -1).
(c) THE TASKS (order 7a986137...69f5c7 [V], init 2000e856...3eec [V]; scaled seconds are SIMULATION seconds):

| Task (uuid) | performer | verb -> decision | geometry / distance | start | armed end |
|---|---|---|---|---|---|
| T01 f7b52ba4... | 28ID (platform) | CNFPSL -> held in place (STP-866) | 4 graphics, not driven | delay 0 | 300 s, no destination |
| T13 37677c40... | 48 IBCT (platform) | CNFPSL -> held in place | 4 graphics, not driven | delay 0 | 300 s, no destination |
| T10 9aab7fe6... | 1-112 IN (aggregate, 6) | CRESRV -> bare move | start -> (h) wp -> (j) wp -> PassagePoint_48_IBCT_SLOT0, 1,453 + 805 + 517 = 2,775 m | SimulationTime PT20M -> 300 s | 450 s, destination (6.2 m/s needed) |
| T02 696fbb33... | 28ID | ATTACK -> AdvanceOnly (R3 self), ROEHold | PassagePoint_28ID_SLOT0, 5,341 m | after T01's TASKCMPLT | 300 s, destination (17.8 m/s needed -> late path expected) |
| T14 1075b583... | 48 IBCT | FOLSPT -> advance along graphics and hold, ROEHold | start -> (i) wp 54.014600 / 23.331500 -> 54.040348 / 23.324206, 1,267 + 2,902 = 4,169 m | after T13's TASKCMPLT | 300 s, destination (13.9 m/s needed -> late path expected) |

(d) The FOLSPT dispatch line still says "The supported unit is not in the order" (cut A's change (f)); NOT scored (as -1).
(e) PRE-WARM DECISION: NO PRE-WARM LAUNCH. Reasons, from the gate code and the record [V]:
  1. What a pre-warm is for (DEMO_RUNBOOK 0.4) is the init's placement terrain query ("N of N ... from the TERRAIN
     QUERY"). The record says warming does not deliver that on this AO: every Iron Storm launch on this machine read "0 of
     36 create altitude(s) came from the TERRAIN QUERY, 36 from the FALLBACK" - runs 20260921T114910Z (cold),
     20260921T143243Z (the SAME fixture re-launched 2 h 43 min later) and 20260927T003120Z (the -1 pre-warm, after the AO had
     been streamed twice) [V: grep of every runs\*\vrfc2simapp.log]. The query is ONE DtIfRequestTerrainProfileInformation
     issued right after the de-stack with a 10 s timeout (VrfC2SimService.cs:3140-3188 [V]); on 2026-09-21 its reply came
     ~32 s late (PlacementReclampSelfTest.cs:12-15 [V]). A freshly launched back end has not paged the terrain in memory,
     whatever the disk cache holds.
  2. The other thing a pre-warm buys - the maple nav area's file cache - was bought at ~00:31Z by the -1 pre-warm (area row
     25.2 s after placement, "NAV AREA ACQUIRED after 0s of gate", WARM [A: -1 Result]); the fixture is unchanged
     (57465c35 [V]); and the order is gated on the area row anyway (--pre-order-gate nav-area, timeout 900 s, which covers
     the measured ~237 s COLD case, scripts/RunScenario.sh:129-137 [V]). A cold area costs wait time, not correctness; a gate
     timeout is a STOP (P2).
  3. A pre-warm is a whole runner launch with its own teardown gate and appNumber block; the -1 pre-warm is where that
     gate stopped the lane.
(f) P1 DECISION (placement altitude source): P1 as -1 wrote it ("36 of 36 ... TERRAIN QUERY", HIGH, any FALLBACK = STOP)
would, on the record in (e)1, fail with near certainty on a mechanism the project already handles: the placement re-clamp
(RL-20260921-06) repairs FALLBACK-created land platforms, and the dispatch gate logs "is ON the terrain" per mover (P5
scores that). Registering a prediction the record says is false would be dishonest. P1 is therefore RECORDED, with the
expectation written down before the run: "0 of 36 ... TERRAIN QUERY, 36 from the FALLBACK", followed by one PLACEMENT
RE-CLAMP summary line (the -1 pre-warm's L385 read "32 of 36 object(s) - LAND PLATFORMS ONLY - were created at the
FALLBACK altitude").
(g) --parse-order does NOT resolve MapGraphicIDs [V: -1 1(f)]; T10's and T14's resolved vertex lists are visible only in
the live app's resolver lines and CreateRoute point counts (P19, P19b). Resolver chaining [V TaskGeometryResolver.cs
:402-468]: every line vertex within 100 m of the taskee is dropped; lines are chained nearest-end-first from the taskee
(ChainGapMeters 5000); a Point/Area graphic becomes the single DESTINATION appended last. T10: (h) wp 1,453 m is nearer
than (j) wp 2,104 m, so (h) then (j) (805 m apart), then the PassagePoint. T14: (i) wp 1,267 m is nearer than the
FollowAndSupport end 2,426 m, so (i) then FollowAndSupport (2,902 m apart); T14 names no Point graphic.
(h) ONE engage-path line per DISPATCH PASS (P8), as -1 1(g).

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: --pre-order-gate nav-area --pre-order-gate-timeout 900

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: the watchdog runs on the SIMULATION clock (--env Vrf__StallClock=sim), window 360 SIM s.
Armed ends in SIM s from dispatch: T01 300 and T13 300 (no destination - never watched); T02 300 and T14 300 (destination
tasks: the armed end is INSIDE the 360 s window, but an unarrived destination task goes OVERDUE and stays watched, and
nothing is SENT at that end, so the end cannot pre-empt or manufacture a stall verdict); T10 450 (outside the window). A
stall verdict is possible from dispatch + 360 SIM s onwards for T02 / T14 / T10 only.

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels.

DEVIATION FROM RECORD: the successor-gate floor is 60 s, not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:308); the computed wait is max(60, 300 + 60) = 360 SIM s, so T02 and T14 are not held two hours behind a late predecessor for no reason.

DEVIATION FROM RECORD: consoles at level 4, not the demo setting of plan P7 ("--object-console 3 --member-console 3"); the house template requires level 4 for a movement run and the movers here are lone platforms.

DEVIATION FROM RECORD: a FALLBACK placement does not stop this run, where DEMO_RUNBOOK sec 0.4 says of anything but N of N from the terrain query "Stop, warm the area, start again"; the record in sec 1(e)1 shows warming does not change that line on this AO, and the re-clamp that repairs it is scored in P5.

Flag note (not a deviation): `--no-gui` is written explicitly in step E; it is the runner's default (scripts/RunScenario.sh
:102 [V]) and -1 kept it by default.

## 2. What the code emits - log-line shapes

src is byte-identical to -1's (80f707f = a019918 = 6ab78f2 for src [V]), so -1 sec 2 stands verbatim: L-DISP (:5102),
L-CNFPSL (:4447) + IN PLACE (:4448), L-R3SELF (:5047), L-FAW (:5471, PREDICTED ABSENT), L-FOLSPT (:4829), L-BARE (:4248),
L-RESOLVE (TaskGeometryResolver.cs:424 dropped / :467 path joined / :489 already ends / :496 DESTINATION; warnings :473
NOT continuous, :500 extra destinations, :516 RETURNS ... TRUNCATED), L-ROUTE (:4996 `CreateRoute '<route>' (<N> pts)`),
L-SHIFT-ON (:720), L-SHIFTED (:6328), L-SHIFTQ (:6290), L-CENSUS (:1635), and the COMPLETION_CONFIRM / NAV_STALL shapes
(L-AREA, L-ORDER, L-PROOF, L-PLAN, L-ENGAGE, L-ARM-D, L-ARM-N, L-OVERDUE, L-ARRIVE, L-HELD, L-END, L-LATE, L-CMPLT-L,
L-STALL, L-SUPP, L-WDOG, L-CLOCK, L-RATIO, L-PLACE, L-RC-*). The second line of a chained path reads `... <k> vertex(es)
joined <d> m after the previous graphic's end` (:467-468 [V]).

## 3. Sequence and exact command lines

All from Git Bash at the MAIN checkout F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM (HEAD main
6ab78f2 [V: submodule gitdir refs/heads/main]; OPUS_EXECUTION_PLAN.md, order, init, RunScenario.sh, StopVrf52.ps1
byte-identical to this branch [V]). The runner writes its appNumber block into the MAIN checkout's working-tree
OPUS_EXECUTION_PLAN.md; that block is carried back to this branch afterwards (lane R / IS1 procedure).

A. Read-only checks [V 01:58-02:03Z]: DOTNET_ENVIRONMENT empty; 0 Vrf__ vars; no vrfNavGenerator / build / vrfSim /
   vrfGui / VrfC2SimApp / WatchVrf / ListenReports process; rtiexec 47980 + rtiForwarder 50740 alive; rtiAssistant 30240
   never touched; REST 200 on 18080; hashes as registered; `derive_ironstorm_cuta.py --check` ALL PASS. Re-run immediately
   before E.
B. No build. Version string and hashes re-read after E.
C. Order validation (no federate, no appNumber):
   - C1 `VrfC2SimApp.exe --parse-order data/IRONSTORM_CUTA_Order.xml` [V 02:04Z, exit 0]: "Tasks: 5"; durations 1200000
     ms x 4 and 1800000 ms (T10); T10 lists mapGraphic c8d9cd1a-b808-5b8d-97be-7beb98393a62, 51a59f89-799e-5ed0-8c05-
     1e63b01e8069, cc23071f-aa60-9f52-884c-11505051cc99 in that order (3 graphics before resolution); T14 lists
     7ff48b93-5a1e-5a9a-813f-8dda1df7e5dd then 7351f662-f857-e05a-b533-f9a46e0fb095; T02 startAfter f7b52ba4, T14
     startAfter 37677c40; 0 warn / error / schema lines.
   - C2 `--parse-init data/IRONSTORM_CUTA_Initialization.xml "Not Set"` with the MAK PATH prefix
     (PATH=/c/MAK/vrforces5.2d/bin64:/c/MAK/makRti5.0.1/bin:$PATH; without it VrfBridge.dll is not found - a harness note)
     [V 02:05Z, exit 0]: "Units: 40", "would create (clientId=Not Set): 36".
   - C3 ONE real push to the PRIVATE server, after this registration is committed:
         tools/PushInit/bin/Release/net10.0/PushInit.exe data/IRONSTORM_CUTA_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
         tools/PushOrder/bin/Release/net10.0/PushOrder.exe data/IRONSTORM_CUTA_Order.xml 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
     EXPECT both exit 0, one `ORDER (<n> chars)` echo carrying 5 tasks. Any failure = STOP.
D. THE HOLDER: the -1 persistent holder RtiProbe pid 42672, appNo 5133, SettleSecs 28800 from 00:29:58Z (to ~08:30Z), is
   alive [V 01:58Z]. It is NOT restarted. The runner's dry run must recognise it as a PERSISTENT FEDERATION HOLDER; if it
   is gone at launch time, a new one is started with StartFederationHolder52.ps1 (-WhatIf first) on fresh numbers from the
   marker, recorded in Appendix B before it joins.
E. THE RUN, once, dry run first (same flags plus --dry-run), stdout to a FILE, never piped:

       scripts/RunScenario.sh \
         --scenario IronStorm_Centre_52_Nav_AG_maple \
         --init data/IRONSTORM_CUTA_Initialization.xml \
         --order data/IRONSTORM_CUTA_Order.xml \
         --client-id "Not Set" \
         --duration-scale 0.25 \
         --pre-order-gate nav-area --pre-order-gate-timeout 900 \
         --object-console 4 --member-console 4 \
         --no-stop-when-complete --run-secs 2700 \
         --env Vrf__StallDetection=true \
         --env Vrf__StallClock=sim \
         --env Vrf__TaskPredecessorTimeoutSeconds=60 \
         --sample-threads \
         --no-gui \
         --log runs/launch52/RunScenario-ironstorm-live2-<stamp>.log

   Defaults kept: --profile 5.2, REST/STOMP 18080/61614, route shift ON, the runner's Stage 2h holder (it JOINS the
   federation the persistent holder keeps). GATE on teardown: StopVrf exit 0 or 6 (P18).

QUIET PERIOD (RUNBOOK 0.5.14 item 5): from the launch of E to the post-run inventory: no Stop-Process / taskkill of any
kind (StopVrf52's own identity-gated force of the run's own back end is allowed), no build, no suite, no subagent, no
second runner. Foreground polling of the run's own files with bounded waits. Never touched: rtiexec, rtiForwarder,
rtiAssistant, any RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

Every count is PER TASK UUID, read from the scored run's vrfc2simapp.log in line order and cross-checked against
reports-captured.log. DISPLACEMENT SINCE DISPATCH (P6, P6b, P20) is measured on watchvrf-trace.csv POS rows: the reference
is the object's first POS fix at or after its L-DISP WALL time (trace t mapped to wall by the WatchVrf join stamp in the
trace header, +- a few s), and the figure is the maximum distance from that reference over every later fix; the same
figure against the last PRE-dispatch fix is reported alongside. It is NOT the STALL line's 360-SIM-s windowed max. The
scorer is scratch laneIS2\trace_score.py, run on the -1 pre-warm trace as its control before this registration (it
reproduced 48 IBCT's stop at 0.3 m from 54.02678, 23.31720 and 835 m from its last pre-dispatch fix, 28ID's arrival
1.0 m from its destination, and 54 wet fixes for 48 IBCT at the lake edge [V]).

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) Persistent holder 42672 (appNo 5133) alive at launch and recognised PERSISTENT by the runner; the back end JOINS (rtiexec count-grep: no create for its appNo); "READY - joined the federation". (ii) L-CENSUS reads 4 EMPTY shells and 32 platforms (36 = C2's in-scope count). (iii) L-WDOG once, naming the SIMULATION clock; L-CLOCK names SIMULATION and "Vrf:DurationScale=0.25"; L-SHIFT-ON once. (iv) The runner's stage 7d gate FIRED (no NOT-READY, no exit 3). (v) Exe / dll / bridge hashes as registered, before and after. (READY TO TASK is RECORDED, not scored.) | HIGH | Any limb = VOID + STOP (launch, terrain or harness failure - no code verdict). | |
| P1 | PLACEMENT ALTITUDE SOURCE: the init's L-PLACE line and the PLACEMENT RE-CLAMP summary line, recorded. Expected (sec 1(f)): "0 of 36 ... TERRAIN QUERY, 36 from the FALLBACK" and one re-clamp summary. | RECORDED | - | |
| P2 | AREA: the FIRST L-AREA row names `NavArea-ground-platform IRONSTORM-CENTRE_maple` and precedes L-ORDER; zero L-AREA rows name any other area (empty-area rows RECORDED); runner "NAV AREA ACQUIRED ... IRONSTORM-CENTRE_maple". WARM/COLD and the gate wait RECORDED. | HIGH | No row before L-ORDER, a gate timeout, or another named area = STOP. | |
| P3 | SMS: >= 1 L-PROOF line (`C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true`) in the app log. Per mover (28ID, 48 IBCT, a 1-112 IN member by L-MEMBERS) >= 1 each. | HIGH (>= 1); MEDIUM (per mover) | Zero = STOP; a mover with none = recorded MEDIUM miss. | |
| P4 | DISPATCH STRUCTURE: exactly one SENT TASKSTRT per uuid (5); T02's L-DISP after T01's SENT TASKCMPLT and T14's after T13's; one "Task 'T10...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25) - it will not dispatch before then." line (:3988) and T10's L-DISP after it; ZERO L-SKIP / "SKIPPED: predecessor". | HIGH | A missing or duplicate TASKSTRT; a successor dispatched before its predecessor's TASKCMPLT; any SKIP = STOP. | |
| P5 | RE-CLAMP: an L-RC-PASS "is ON the terrain" gate line (VrfC2SimService.cs:3517) for each of T02, T14, T10; ZERO "measured OFF the terrain" gate lines; ZERO retired lines (`NOT DISPATCHED YET`, `HELD as`, `REFUSED [`, `BOUND-BUT-NOT-ON-THE-GROUND`). A mover whose terrain profile fell back to Live has no gate line: recorded NOT MEASURED, not a miss. | HIGH | An OFF gate line or a retired line; a mover with a profile reply and no gate line = STOP. | |
| P6 | HEADLINE: T02's 28ID platform AND T14's 48 IBCT platform are each displaced MORE THAN 50 m after dispatch (displacement since dispatch as defined above, WatchVrf trace - NOT the stall line's windowed max), by window end. | HIGH | Either at or under 50 m = STOP. | |
| P6b | T10: at least one 1-112 IN member displaced more than 50 m after T10's L-DISP (trace). | HIGH | No member over 50 m = STOP. | |
| P6c | Per mover: L-PLAN lines, max speed and net displacement from the trace, any mesh "not enough (0) points" lines. | RECORDED | - | |
| P7 | CNFPSL HOLDS T01 and T13: each has exactly one L-CNFPSL + one IN PLACE line; one Observation in the capture naming STP-866; an armed line "it has no destination" at 300; NO L-ROUTE / MoveToLocation for it; exactly one SENT TASKCMPLT "(300 s after dispatch)". | HIGH | A move issued for a hold; zero or two TASKCMPLTs; a missing L-CNFPSL; an OVERDUE for a hold = STOP. | |
| P8 | T02 PLATFORM PATH: L-R3SELF for T02 on every dispatch pass and on no other task (passes = 1 + L-SHIFTQ lines for T02 + terrain-profile-request lines for T02); ZERO L-FAW, ZERO `FireAtTarget`, ZERO `deferred until the move COMPLETES` lines in the whole log. | HIGH | Any fire-at-will / FireAt / deferred-engage line; no L-R3SELF for T02; a count unequal to T02's pass count = STOP. | |
| P9 | T14 FOLSPT: exactly one L-FOLSPT naming 'ROEHold'; an L-ROUTE for T14 and an L-ARM-D (destination) at 300. | HIGH | Missing, duplicated, another ROE, or no route move = STOP. | |
| P10 | T10 CRESRV: L-BARE for T10; an L-ROUTE for T10 and an L-ARM-D at 450; no IN PLACE line for T10. | HIGH | Any limb failing = STOP. | |
| P11 | COMPLETION PER THE TIME RULES: each mover (T02, T14, T10) takes EXACTLY ONE path - (a) early: arrival, one L-HELD, one TASKCMPLT at L-END "had already arrived"; (b) late: one L-OVERDUE, nothing SENT at the end, then arrival, L-LATE, one L-CMPLT-L; (c) not arrived by window end: L-OVERDUE and no terminal report; (d) stalled: L-STALL + one SENT TASKABRT "STALLED". Which path: RECORDED; T02 late (b). | HIGH (exactly one path each); MEDIUM (T02 late (b)) | Two paths, or a TASKCMPLT at an end time for an unarrived mover = STOP. T02 not (b) = recorded MEDIUM miss. | |
| P11b | T14 COMPLETION PATH: late (b) - one L-OVERDUE (4,169 m against a 300 SIM s scaled duration), then arrival and one TASKCMPLT on arrival (L-CMPLT-L). | MEDIUM | Any other path (early, not arrived, stalled) = recorded MEDIUM miss. | |
| P12 | REPORT HYGIENE: ZERO L-SUPP for a terminal code; at most one terminal report per uuid; ZERO `task=(none)` SENT lines; capture and log agree; "Reports this run: ... 0 FAILED". | HIGH | Any of these = STOP. | |
| P13 | STALLS / FALLBACKS: every L-STALL, OVERDUE and arrival is REPORTED with the member displacement over the preceding 360 SIM s and since dispatch. | RECORDED | - | |
| P14 | NO ENGAGEMENT: zero L-ENGAGE / FireAt lines (P8) and zero vendor-console lines with "Fire Weapon" in the app log. | HIGH | Any engagement line = STOP. | |
| P15 | ROUTE SHIFT: L-SHIFT-ON once; ZERO L-SHIFTED lines (leg_check --osm-water: all three movers' legs ok, no water, CHANGES (i)/(j)). | MEDIUM | A ROUTE SHIFTED line (recorded; P19/P19b then count its inserted vertices). | |
| P16 | CLOCK: every L-RATIO line; the SIMULATION stamp of each L-DISP; the sim/wall band. | RECORDED | - | |
| P17 | RUN HEALTH: no `BACK END LOST`; no WS-runaway exit 6 (runner); no new .dmp / .callstack.log for this back-end pid (names only); VrfC2SimApp exits 0; rtiexec, rtiForwarder, rtiAssistant 30240 and every holder untouched. | HIGH | Any limb = VOID. | |
| P18 | TEARDOWN, scored on StopVrf52's exit code inside the runner: 0 or 6 (6 = the run's own back end force-stopped by identity, including one still exiting at the 60 s post-force deadline), and no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left. | HIGH | Exit 3, 5 or 7 = STOP. | |
| P19 | T10 RESOLVED ROUTE: T10 resolves to [(h) wp 54.029734 / 23.305499, (j) wp 54.024000 / 23.313000, PassagePoint_48_IBCT_SLOT0]. T10's distinct L-RESOLVE lines are exactly: for c8d9cd1a and for 51a59f89 each "1 vertex(es) dropped - they ARE the taskee's own position"; "path from MapGraphicID c8d9cd1a-... (line, 1 vertices): 1 vertex(es) joined" (no gap suffix - first graphic); "path from MapGraphicID 51a59f89-... (line, 1 vertices): 1 vertex(es) joined <d> m after the previous graphic's end" with d in 795-815; cc23071f -> PassagePoint_48_IBCT_SLOT0 "(<kind>, 1 vertex): appended as the route's DESTINATION"; zero resolver WARNINGs for T10. T10's L-ROUTE reads "(4 pts)" (4 + 2 per L-SHIFTED line for T10, if any). | HIGH | Any other vertex list: a different drop/join count or order, a missing or different destination, a resolver warning for T10, or an L-ROUTE point count other than 4 + 2 x shifts = STOP. | |
| P19b | T14 RESOLVED ROUTE: 7ff48b93 and 7351f662 each "1 vertex(es) dropped"; 7ff48b93 joined first (no gap suffix), 7351f662 joined second "... 1 vertex(es) joined <d> m after the previous graphic's end" with d in 2890-2915; zero resolver warnings for T14; T14's L-ROUTE "(3 pts)" (+ 2 per shift). | MEDIUM | Any other list or count = recorded MEDIUM miss (P9 still scores the route move). | |
| P20 | H1 FALSIFIER (FINDING_IRONSTORM_T14_STOP sec 5): T14's 48 IBCT does NOT halt within 50 m of 54.02678, 23.31720; it passes its (i) waypoint 54.014600 / 23.331500 (closest trace fix within ~50 m) and reaches within 100 m of its destination 54.040348 / 23.324206 by window end. NAMED OUTCOME: a stop (no >5 m step for the rest of the window) on dry ground >= 50 m from any water (OSM water raster and CLCplus water) on the (i) route REFUTES H1's sufficiency (H2, obstacle avoidance) - it is recorded as that, not explained away. | MEDIUM | Any limb failing = recorded MEDIUM miss, with the stop point, its nearest water distance and the object's last console lines. | |
| P21 | NO WATER ON ANY TRACK: no POS fix of any object that moved > 50 m (28ID, 48 IBCT, 1-112 IN and its members) falls in an OSM water cell - the z14 osm-water raster (~5.6 m cells at 54 N) read by tools/preflight/leg_check.load_osm_water from scratch u3\laneI2\osm\osm-water; a fix on an absent tile is counted UNKNOWN, never dry. Control [V]: the -1 pre-warm trace gives 54 wet fixes, all 48 IBCT's, at the lake-edge stop (the polygon is 0.5 m away; the raster cell holds it). | MEDIUM | >= 1 wet fix, or any mover fix on an absent tile = recorded MEDIUM miss. | |

WHY P20 IS MEDIUM, NOT HIGH: the finding confirmed H1 on geometry and config but carries one [A] (the sim's soil query
resolves the composite as its XML reads - never observed in a sim query) and leaves H2 not falsified as a contributor;
the (i) route has never been driven, its water clearance is 60 m on a 5 m lattice, and the last 46 m left turn before
the -1 stop is unexplained. P6 (displacement > 50 m) stays HIGH: the first (i) leg runs 1,267 m south-east over dry
ground, and 48 IBCT drove 850 m of its -1 leg before the water.

STOP RULES:
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P17 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- The executor never intervenes in the window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as IRONSTORM_CUTA_LIVE-<date>-3, with new appNumbers.

ONE VARIABLE: T14's route (change (i)) is the variable P20 tests, against the -1 pre-warm run 20260927T003120Z (the
control, unscored: 48 IBCT stopped at the lake edge on the (e) leg). The -2 run also differs in T10's route (change (j)),
the StopVrf52 exit-6 reading and the run length (2,700 s against 120 s), so no other single-variable claim is made.

## 5. Application numbers

The Appendix B marker read `*** NEXT FREE: 5159 ***` at 6ab78f2 [V]. No hand claim: the persistent holder keeps 5133
(-1's). The runner writes the SCORED block at its Stage 2, expected layout (as -1's pre-warm block): 5159 back end, 5160
front end (BURNED, --no-gui), 5161 WatchVrf pre-check, 5162 WatchVrf trace, 5163 VrfC2SimApp, 5164 RtiProbe 2c, 5165
CreateOne (BURNED unless the oracle gate fails), 5166-5169 Stage 2h holder attempts (5166 JOINS; 5167-5169 BURNED);
marker -> 5170. The dry run confirms the block; a different layout is written here BEFORE launch. A launch that aborts
burns its whole block. PushInit / PushOrder / ListenReports / StopIface are C2SIM clients, not federates.

## 6. Harvest (after the run, read-only) and where results go

From the run directory (runs\launch52\last-run-dir.txt): vrfc2simapp.log, reports-captured.log, c2sim-bus.log, the
manifest, watchvrf-trace.csv, thread-samples.csv, holder logs, stopvrf logs, the wrapper log. Vendor sim logs dump the
environment in cleartext: count-grep only, never quoted. Results go to: the Result block below (measurement and
implication in separate sentences); DEMO_READINESS row 16; HANDOFF sec 6 (200 x 160 cap); Appendix B annotated from the
manifest. ASCII + CRLF throughout.

## 7. What this run does NOT claim

- No unit ATTACK or fire at will; no BREACH; CNFPSL as a real passage (held in place); T10 as a reserve posture (a bare
  move); following 116 ABCT (not read); T02 as an attack (STP-846's fallthrough for RECEIVE).
- Nothing about vegetation fidelity, the full 23-task order, the demo server (8080/61613), the GUI or the demo profile.
- A T14 arrival supports H1 but does not prove the sim's soil query path (the [A] above); a T14 stop away from water
  refutes H1's sufficiency without identifying H2's mechanism.
- No timing generalisation: n = 1, one host, one fixture, a load-dependent clock.

## Result (written after the harvest, never from a live read)

Written 2026-09-27 ~03:30Z by lane IS2run (session 5fc25950) from the harvested files of the scored run
20260927T021020Z_run (main checkout runs\): vrfc2simapp.log (752,596 lines, streamed grep; L = its line numbers),
reports-captured.log, run-manifest.json, watchvrf-trace.csv (scratch u3\laneIS2\trace_score.py), stopvrf.stdout.log,
thread-samples.csv, holder.1.stdout.log, runner log runs\launch52\RunScenario-ironstorm-live2-20260927T021001Z.log
(R = its line numbers). Vendor sim logs were not opened. Launch 02:10Z, order pushed after a 2 s gate, window 2,700 s,
runner exit 03:01:21Z.

### VERDICT: SCORED. Every HIGH prediction held. Three MEDIUM misses: P3 (per mover), P11b, P20. P20's miss is its NAMED OUTCOME: 48 IBCT stopped on dry ground 290 m from any water, which refutes H1's sufficiency.

| # | Verdict | Evidence |
|---|---|---|
| P0 | PASS | (i) R74 "RtiProbe pid=42672 ... PERSISTENT FEDERATION HOLDER"; rtiexec log "Could not create federation MAK-ONE-2025, because it already exists" (creates became joins); L45 "READY - joined the federation". (ii) L217 "4 unit(s) created as EMPTY shells ... 32 platform(s) created in full". (iii) L51 "STALL WATCHDOG: ... on the SIMULATION clock" (once); L28 TASK CLOCK SIMULATION; L989 "... SIMULATION clock; Vrf:DurationScale=0.25"; L22 L-SHIFT-ON (once). (iv) R242 "NAV AREA ACQUIRED after 2s of gate". (v) exe 493a07a8 / dll c86901be / bridge 90272bc9 identical before (01:59Z) and after (03:05Z). READY TO TASK L552 "36 of 36 ... after 13.4 s" (recorded). |
| P1 | RECORDED | L393 "0 of 36 create altitude(s) came from the TERRAIN QUERY, 36 from the FALLBACK" - as written down in sec 1(f); L395 PLACEMENT RE-CLAMP "32 of 36 object(s) - LAND PLATFORMS ONLY"; L319 the init query "got no reply within 10 s". |
| P2 | PASS | First L-AREA L738 `NavArea-ground-platform IRONSTORM-CENTRE_maple` before L-ORDER L830; all 45 L-AREA rows name the maple area. R244 placement -> area row 7.2 s, WARM. |
| P3 | PASS (HIGH); MEDIUM MISS per mover | 27 L-PROOF lines (first L3411). Per mover: all 6 1-112 IN members yes; 28ID and 48 IBCT none (both drove Move Along Route, which plans no path, UG52 23.3). |
| P4 | PASS | 5 TASKSTRT, one per uuid (L862, L890, L1559, L1573, L1613); T02 L-DISP L1611 after T01 TASKCMPLT L1335; T14 L-DISP L1571 after T13 TASKCMPLT L1329; L844 T10 "start delay 300 s (order says 1200 s ...)" before T10 L-DISP L1557; zero predecessor SKIPs. |
| P5 | PASS | L1551 (1-112 IN, gap 0.3 m), L1567 (48 IBCT, 0.0 m), L1607 (28ID, 0.0 m) "is ON the terrain"; 0 OFF gate lines; 0 retired lines. The 32 "MEASURED OFF THE TERRAIN" lines are the init re-clamp sweep, not the dispatch gate. |
| P6 | PASS | Measured since dispatch: T02 28ID 5,173 m (5,340 m from the last pre-dispatch fix); T14 48 IBCT 1,515 m (1,533 m). |
| P6b | PASS | All six 1-112 IN members 2,543-2,677 m since dispatch. |
| P6c | RECORDED | L-PLAN only for T10 members (27 "Planned path has N points", 27 "Is destination in nav area?: success"); none for 28ID / 48 IBCT; 0 "not enough (0) points". Net: 28ID 5,340 m to 1.0 m from its destination; 48 IBCT see P20; T10 members end 1.7-51.5 m from the PassagePoint. |
| P7 | PASS | T01 L866 + L868, T13 L894 + L896; L864 / L892 "armed at 300 s ... it has no destination"; TASKCMPLT L1335 / L1329 "(300 s after dispatch)"; 2 STP-866 Observations in the capture; no L-ROUTE and no OVERDUE for either. |
| P8 | PASS | L-R3SELF L1359, L1541, L1609, T02 only; T02 passes = 1 + L1361 (route-shift check queued) + L1543 (terrain profile request) = 3; 0 L-FAW, 0 FireAt, 0 deferred-engage. |
| P9 | PASS | One L-FOLSPT L1569 "rules of engagement as ordered ('ROEHold')"; L-ROUTE L1577; L1575 armed at 300 s, "a destination". |
| P10 | PASS | L-BARE L1377 / L1509 / L1555 (one per pass); L-ROUTE L1563; L1561 armed at 450 s, "a destination"; no IN PLACE for T10. |
| P11 | PASS (HIGH and MEDIUM) | T02 path (b): OVERDUE L107399 (325 of 300 s), ARRIVAL EVIDENCE L155255, L155257, TASKCMPLT L155259 "arrived after its task's end time". T10 path (b): OVERDUE L149157 (472 of 450 s), L712007-L712011 TASKCMPLT on arrival. T14 path (d): OVERDUE L107401 (the registered shape: a 300 s armed end inside the 360 s window), then STALL L208413 and one TASKABRT L208415; no TASKCMPLT. One terminal path each. |
| P11b | MEDIUM MISS | T14 took path (d), stalled, not (b). |
| P12 | PASS | 0 SUPPRESSED; one terminal report per uuid (4 TASKCMPLT + 1 TASKABRT); 0 `task=(none)`; capture = log (5 / 4 / 1); L752593 "9840 delivered, 0 FAILED". |
| P13 | RECORDED | T14 STALL L208413 "(max 0.4 m)" in 360 SIM s, sim clock ~1,057 s; since dispatch 1,515 m (trace). OVERDUEs: T02 325 s, T14 325 s, T10 472 s. T02 arrived ~73 wall s after dispatch (trace); T10 reported 331.6 WALL s = 2,403.3 SIM s after dispatch (L712007). |
| P14 | PASS | 0 L-ENGAGE / FireAt / "Fire Weapon" lines. |
| P15 | PASS | 0 L-SHIFTED; "no leg flagged" for T10 L1485, T14 L1493, T02 L1533. |
| P16 | RECORDED | L-DISP SIM stamps: T01 / T13 72.3 s; T10 / T14 / T02 419.9 s. 45 L-RATIO lines, SIM/WALL 6.993-13.917. |
| P17 | PASS | 0 BACK END LOST; runner exit 4 (not the WS-runaway 6); no .dmp / .callstack.log under C:\MAK\vrforces5.2d written after 02:00Z; VrfC2SimApp exit 0 (manifest); rtiexec 47980, rtiForwarder 50740, rtiAssistant 30240 and holder 42672 up at 03:01:35Z. |
| P18 | PASS | StopVrf exit 6 (R364; the graceful close was refused and the own back end pid 6980 forced by identity). The registered miss is exit 3 / 5 / 7. Post-run inventory 03:01:35Z: no vrfSim / VrfC2SimApp / WatchVrf / ListenReports. RECORDED beside it: the runner's own post-check (R369-R382) still listed the forced pid 6980, so runner exit 4 "TEARDOWN INCOMPLETE"; thread-samples.csv has 6980 "process gone" at 03:01:24.169Z. |
| P19 | PASS | L1363 / L1365 "1 vertex(es) dropped" (c8d9cd1a, 51a59f89); L1367 c8d9cd1a "(line, 1 vertices): 1 vertex(es) joined." (no suffix); L1369 51a59f89 "... joined 805 m after the previous graphic's end"; L1371 cc23071f -> PassagePoint_48_IBCT_SLOT0 "(point, 1 vertex): appended as the route's DESTINATION"; 0 resolver warnings for T10; L1563 "(4 pts)". |
| P19b | PASS | L1337 / L1339 "1 vertex(es) dropped" (7ff48b93, 7351f662); L1341 7ff48b93 joined (no suffix); L1343 7351f662 "joined 2906 m after the previous graphic's end"; 0 warnings for T14; L1577 "(3 pts)". [NOTE 2026-09-27, from E2-2's Result: "0 warnings" means none of the three REGISTERED warning kinds; the resolver also logs at WARN "the MapGraphicID geometry and the embedded Location on this task END 2428 m apart ... the MapGraphicID was used" (L1347, L1523) - an STP data trait present in every run, not a route defect; the seat's reading (RL-free, scoring only): PASS stands.] |
| P20 | MEDIUM MISS - NAMED OUTCOME | Limbs: did NOT halt near 54.02678, 23.31720 (closest 702.5 m) - held; passed the (i) waypoint (closest fix 25.8 m, trace t 89.7) - held; reached within 100 m of its destination - FAILED: it stopped at 54.030807, 23.327063 (alt 133.6, last >5 m step at trace t 114.3, about 43 wall s after dispatch) and stayed there to the last fix (t 2773.6), 1,077 m short, 1,825 m along the 2,902 m leg 2, about 10 m east of the leg's line. The stop is DRY: nearest water of any source (the OSM z14 water raster over the CLCplus chain, 5 m rings) is 290 m away; the steepest 10 m window within +-100 m on the leg heading is 0.065; no other entity came within 400 m. That is the registered named outcome. |
| P21 | PASS | 0 wet fixes over the 9 objects that moved > 50 m (28ID, 48 IBCT, the 1-112 IN proxy and its 6 members); 0 fixes on absent tiles. |

MEASUREMENTS BEYOND THE PREDICTIONS [V]:
- Under the T14 stop, CLCplus reads class 31 ("Woodland - broadleaved trees", BM_VEGETATION) at all 81 lattice points
  within 25 m, and at 262 of 317 within 50 m. The last two fixes before the stop turned from heading -9.5 deg (the leg) to
  -6.9 deg, then one 3.7 m step. The object's own console at level 4 printed nothing after "Controller ... beginning to
  process move-along task" (L1635/L1637). This is the same silence as the -1 stop.
- Across the whole run, the movers drove through needle-leaved woodland (CLCplus 21) for kilometres without stopping:
  28ID ~3.4 km, 48 IBCT before its stop ~2.2 km, and T10 members 0.7-1.25 km each. Class 31 is rare on every track. T10
  members crossed 10-80 m of class 31 and kept moving. 48 IBCT's only class-31 samples are at its stop.
- StopVrf52 printed "forced pid 6980 still exiting after 0 s", although -ForcedExitWaitSec defaults to 60 s and the
  runner did not pass it. The post-force wait loop (scripts/StopVrf52.ps1:582-584) apparently ended at once while the
  name-based re-read still listed the pid. This is UNEXPLAINED here [A: no reproduction].
- T13's resolver warned "RETURNS to the taskee's own start ... TRUNCATED" (L882). T13 is a hold and its geometry is not
  driven.

Measurement: in one scored run on the maple area, all five tasks dispatched in their registered order and all reports
were clean. 28ID drove 5.3 km and T10's aggregate drove 2.7 km through both of its new waypoints; both arrived late and
completed on arrival. No mover's track touched OSM water. 48 IBCT drove the new (i) route past its waypoint, then
stopped dead on dry ground in broadleaved woodland, 290 m from any water and 1,077 m short of its destination. The
watchdog reported the stop as a stall and sent a TASKABRT.

Design implication, stated separately: the T14 finding's H1 (OSM deep water) does not explain this stop, so water
cannot be the whole story of 48 IBCT's two stops. H2 (the vehicle's own obstacle avoidance deadlocking) is the
registered alternative. The measurements above name one candidate for it: the woodland at the stop (CLCplus 31). On
this fixture, biome-04 forest is red maples over the whole AO (tools/FixtureGen/README.md:171). The movers crossed
kilometres of other woodland without stopping, so if trees are the cause, the difference must be in tree density or
layout at this spot. Tree density per land-cover class was not measured, and the candidate is NOT verified. This run cannot tell whether trees stop a lone M577A2 on Move
Along Route, or whether the -1 stop was the same mechanism. Iron Storm is not demo-ready while one of its three movers
stops mid-leg.
