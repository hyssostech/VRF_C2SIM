# PREREG - IRON STORM CUT A, RUN E1: lone platforms on Move To per vertex, T14 on its ORIGINAL line (one scored run)

STATUS: REGISTERED - LAUNCH PENDING (seat's go-live). Registered 2026-09-27 (the registration commit's own time is the
stamp; see git log) by lane E1 (session 5fc25950) on branch run/ironstorm-cuta-e1 from main 50a7a62 (docs only after
the build commit 5e8d6f1), BEFORE the holder, the order validation push and the scored run. Nothing in this file has been
launched: no push, no holder, no appNumber claim - the preparation phase stops here and the seat gives the go-live.
Written from the -2 registration
(PREREG_IRONSTORM_CUTA_LIVE2_2026-09-27.md - its settings, sequence and P0-P21, carried where still valid) and the
seat's E1 brief. Marks: [V] = checked while writing this file; [A] = taken from the record, not re-checked.

## Registration

PREREG ID: IRONSTORM_CUTA_E1-2026-09-27-1
DATE (UTC): 2026-09-27, before any launch (the registration commit's own timestamp is authoritative; the launch waits for the
seat's go-live)
BINARY / COMMIT: main 5e8d6f1, whose src, tools and scripts are those of 3e5ab14 - the E1 build target PLAN_MOVEMENT row
E1 names ("main 3e5ab14 (M1 + D1 + A1). M2 merges AFTER E1 on purpose"; `git diff --stat 3e5ab14 5e8d6f1` touches only
docs/PLAN_MOVEMENT_2026-09-27.md [V]): M1 (47da73e, merged d75a8d0, RL-20260927-01), D1 + M1b (b817609) and A1 (3e5ab14).
BUILT AND DEPLOYED BY THIS LANE 2026-09-27T18:00:25-18:00:42Z per RUNBOOK sec 9 - ALL ELEVEN consumers, `dotnet build
<csproj> -c Release -p:BridgeConfig=Release-5.2 -t:Rebuild -m:1`, 0 errors, VrfC2SimApp's documented 4 CA2024 + 2
CS8632 - into the MAIN checkout's own output folders (never C:\MAK) [V]: src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\
VrfC2SimApp.exe sha256 af19d6be5dba39e31584345a19b8658ed32d38ac33569be7e36b091fb84edcb2, VrfC2SimApp.dll sha256
249824129d4980480295d8180e8ddfda2202daf41b708da8ea8dbfefefef5eb4, ProductVersion 1.0.0+git.5e8d6f1.Release-5.2 (exe and
dll, no +DIRTY); appsettings.json 1d068ec7...8196f3 = src (PlatformMoveToPerVertex true, VertexArrivalRadiusMeters 100,
ModelSet EntityLevel); VrfBridge.dll 90272bc9...f25847 (the pin, NOT rebuilt) in all eleven output trees and the build
directory. (An earlier deploy of b984945 by this lane, 17:12Z, is SUPERSEDED: main moved to the named target while the
preparation ran.) Offline suites against the DEPLOYED exe [V 18:01-18:02Z]: 25 of 25 exit 0, the three --disabled
fail-first arms exit 1 as designed; --rulings-selftest 431 PASS / 0 FAIL (its RL-20260927-01 section 73 PASS, its D1
section 42 PASS); --routeshift-selftest 90 ok / 0 fail, 0 network fetches; `DOTNET_ENVIRONMENT=Demo --runtime-check`
"runtime-check: OK". `git diff --stat 5e8d6f1..HEAD -- src` on this branch is EMPTY [V at the registration commit].
Fixture C:\MAK\vrforces5.2d\userData\scenarios\
IronStorm_Centre_52_Nav_AG_maple.scnx sha256 57465c3545e884f0c32d283e945f35ce6694233cc17a0bc14116dd4aa44a0e32 [V].
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: UG52 sec 23.1 p500 (Move To "Moves the vehicle to a destination location, causing the entity to first
plan a route on roads and around obstacles as appropriate, then following that planned route using Move Along Route and
road-movement tasks"; Move Along Route "nor does the entity plan paths before moving"); UG52 23.2 / 23.2.1 p500-501 (Move
To uses roads, nav meshes and feature-obstacle planning); UG52 23.2.2 p502 (recovery - back up, drive around, one replan -
Move To only); UG52 23.3 p505 (Move Along Route: "There is no path planning done as it moves toward the next vertex");
MG 2.4 p18-19 ("Movement to a point is now performed by a Move To task"); vrftasks/moveToTask.h :60 (the task type is
move-to) and :181-184 (a move-to on a ground vehicle starts ground-vehicle-move-to as a subtask, "which does more
intelligent path planning"); vrftasks/moveToLocationTasks.h :12-14 (DtMoveToLocationTask is now DtMoveToTask);
vrfcontrol/vrfRemoteController.h :1651 (moveToLocation, the call VrfFacade.cpp :1015 makes); UG52 30.22 p598 / 30.24 p600
(a unit's Move Along Route plans a path to each vertex for every member); UG52 23.5.1 p507 + Table 26 (deepLake ->
deep-water, acceleration-factor 0.0); ground-vehicle-move-to.lua :1401-1404 (an unplannable vertex aborts visibly,
PathPlanFailure) and :423-460 (the only endpoint adjustment is a road shoulder) [A: FINDING_GROUND_MOVEMENT_PRACTICE sec
5.2]; singleTaskControllerComponent.h :191-200 (decideToGiveUpTask "always returns false" - only this interface's watchdog
reports a stuck mover).

OWN-RECORD CITATION:
- RL-20260927-01 (docs/RULINGS.md, "Go" on the decision brief) and docs/PLAN_MOVEMENT_2026-09-27.md row E1 and sec 3 (the
  E1 prediction and its MISS, written there first: "T14 stalls >= 60 sim s with no OSM feature within ~10 m ahead -> the
  frame is wrong; STOP and ask"). PLAN sec 6: that Move To plans on the maple nav area for a lone M577A2 is "documented,
  not yet observed - E1 is the observation".
- docs/experiments/FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27.md secs 0-6 (the claim, the vendor text, the stops read
  against it; sec 5.2 the per-vertex form and why vertices are checked before dispatch; sec 6 its falsifier).
- M1 (47da73e, merged d75a8d0): src/VrfC2SimApp/VertexChain.cs (VertexChainPolicy.FormFor :88-94, StartupLine :113-132,
  VertexChainTracker) and VrfC2SimService.cs :771 (start-up line), :4975-5000 (the form decision), :5334-5405
  (StartVertexChain; dispatch line :5395), :5414-5493 (completion outcomes and their lines), :5532-5562
  (IssueNextVertex), :8100 (VRF task complete) [V at 5e8d6f1]; RUNBOOK sec 11 bullet "GROUND MOVEMENT SINCE 2026-09-27"
  (the lines to look for; "NOT YET SEEN LIVE (run E1 of the plan): exactly one "move-to" completion per vertex; Move To
  planning on the nav area for a lone M577A2; the stop at each intermediate vertex"). M1b (b817609): the runner's
  report-evidence gate maps a chained platform from its MOVE TO PER VERTEX line and anchors it on its LAST
  task-complete record (RunnerLib.ps1 Get-VertexChainNames); tools/analysis/applog_chain.py parses the per-vertex lines.
  D1 (b817609): on EntityLevel a memberless aggregate is still not judged, with one "TASK JUDGES: ... NO members"
  warning per task (VrfC2SimService.cs :7401), and a start-up line names the model set (:776, UnitPositionPolicy.
  StartupLine); on EntityLevel the ARRIVAL EVIDENCE and STALL lines are unchanged (their D1 suffix is empty) [V].
- IRONSTORM_CUTA_LIVE-2026-09-27-1 (PREREG_IRONSTORM_CUTA_LIVE1_2026-09-27.md): its unscored pre-warm, run
  20260927T003120Z, is THIS RUN'S CONTROL - 48 IBCT on the SAME line (the (e) line of the pre-(i) order) on Move Along
  Route drove ~850 m and stopped dead 0.5 m from the OSM lake (FINDING_IRONSTORM_T14_STOP_2026-09-27.md, H1).
- IRONSTORM_CUTA_LIVE-2026-09-27-2 (PREREG_IRONSTORM_CUTA_LIVE2_2026-09-27.md): SCORED, every HIGH prediction held; 48
  IBCT on Move Along Route on the (i) route stopped mid-leg 2 (P20's named outcome). Its dry-woodland reading is CORRECTED
  on the unmerged branch fix/ironstorm-t14-hamlet (lane IS3): the stop lies among hamlet buildings, 3.2 m from an OSM
  footprint [A: that branch's text; FINDING_GROUND_MOVEMENT_PRACTICE sec 3 row 3 records the same].
- data/IRONSTORM_CUTA_CHANGES.md section "E1" (the variant: changes (a)-(h) and (j), (i) LEFT OUT, T14's destination keeps
  (e)); order sha256 7d5b4034eef0ecf0f33946c6f30b60aaff35e640a86b6b16c9c24f02cb3ddb9a, init
  2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec [V: `derive_ironstorm_cuta.py --check`, exit 0, three
  CHECK lines, and the E1 order proven equal to the ruled order minus (i)'s two insertions].
- HANDOFF sec 1 CLOSED list: "A LONE PLATFORM performer never enters ground-vehicle-move-to.lua (it runs the native
  move-along), so it cannot test the mesh planner - G7 attempt 2". That is true of the native move-along; E1 gives the
  lone platform a Move To, and P3 predicts it now ENTERS that script - a new task, not a reopening. G6/G7 CLOSED (the
  custom including SMS at C:\C2SIM\vrf-sms sets useAbstractGraphs=true; its proof line is L-PROOF). C11 (the object's own
  console at level 4 is the first instrument).
- Rulings kept as they stand: RL-20260921-09 (the temporary completion position); RL-20260913-03 and RL-20260914-01
  (stall = report + TASKABRT); RL-20260920-01 item 3 (route shift ON); RL-20260926-01 (CNFPSL held in place; FOLSPT advance
  and hold); RL-20260921-06 (the placement re-clamp stays; the dispatch gate is measure-and-log only); RL-20260925-01
  (D2/D3/D4); RL-20260921-02 item 3 (the sim clock); STP-846, STP-847, STP-866.
- RUNBOOK 0.5.14 items 2, 5, 12, 18; sec 9 (the eleven consumers, the pin); 9c (the persistent holder); 11 (the clock and
  the M1 bullet); 11f; 11h. StopVrf52 since -2 (merged): 6e7295d, the post-force wait uses the same -Name probe as the
  verdict (StopVrf52.ps1 :84-98, :600-635 [V]).

## 0. Purpose, in plain words

RL-20260927-01 changed what a lone ground platform is given: one Move To per STP vertex (the vendor's planning task)
instead of Move Along Route (the literal executor). E1 is the first live look. It drives cut A with T14's hand waypoint (i)
REMOVED, so 48 IBCT (one M577A2) gets its original line - the line on which the same vehicle, on Move Along Route, stopped
dead at the lake edge in the -1 pre-warm. That line is not clear [V, OSM z14 tiles]: it is inside OSM water from 831 to
935 m (the lake Jezioro Wiersnie) and from 1,901 to 1,957 m (a second water body), and inside two building footprints at
1,091-1,095 and 1,113-1,117 m (a hamlet on the lake's north side). THE HEADLINE: T14 reaches within 100 m of its
destination because the PLANNER takes it round all of that. THE FALSIFIER of the frame: T14 stalls for 60 SIM s or more
with no OSM feature within ~10 m ahead. T02 (one M1A2, 5.3 km of open ground) should arrive on Move To too; T10 (a unit,
unchanged code path) behaves as in -2.

WHAT E1 CAN AND CANNOT PROVE, said before the run: T02's and T14's routes are two points each - the live start and one
destination (`leg_check.py --dump-resolved` [V]) - so each is a chain of ONE Move To ("vertex 1 of 1"). E1 therefore
exercises the PLANNING TASK on a lone platform on a 1-vertex chain. It exercises the per-vertex CONTINUATION (vertex k
COMPLETED -> vertex k+1 issued) only if the lateral route shift inserts points into T02's or T14's leg, which it is
predicted NOT to do (P15: both legs read slope ratio 0.05-0.11 and route shift flags on grade). If no shift fires, the
continuation stays NOT YET SEEN LIVE after E1, and the Result says so rather than claiming it.

## 1. Decisions taken from the record

(a) PERFORMERS - unchanged from -2 sec 1(a) [A]: 28ID = ONE M1A2 PLATFORM; 48 IBCT = one M577A2 PLATFORM; 1-112 IN = an
AGGREGATE of 6 members, created AtOrder. T02 is a platform ATTACK whose AffectedEntity is the performer itself (R3 self)
-> AdvanceOnly with ROEHold. No unit ATTACK in cut A: the fire-at-will line is PREDICTED ABSENT (P8).
(b) DurationScale 0.25 is plan P7 / CHANGES.md "recommended", a plan recommendation, not a ruling on file (as -1, -2).
(c) THE TASKS (E1 order 7d5b4034...ddb9a [V], init 2000e856...3eec [V]; scaled seconds are SIMULATION seconds):

| Task (uuid) | performer | verb -> decision | geometry / distance | start | armed end | form (VertexChainPolicy.FormFor) |
|---|---|---|---|---|---|---|
| T01 f7b52ba4... | 28ID (platform) | CNFPSL -> held in place (STP-866) | 4 graphics, not driven | delay 0 | 300 s, no destination | none - a hold issues no move |
| T13 37677c40... | 48 IBCT (platform) | CNFPSL -> held in place | 4 graphics, not driven | delay 0 | 300 s, no destination | none |
| T10 9aab7fe6... | 1-112 IN (aggregate, 6) | CRESRV -> bare move | start -> (h) wp -> (j) wp -> PassagePoint_48_IBCT_SLOT0, 1,453 + 805 + 517 = 2,775 m | SimulationTime PT20M -> 300 s | 450 s, destination | UNIT: CreateRoute (4 pts) + MoveAlongRoute, unchanged |
| T02 696fbb33... | 28ID | ATTACK -> AdvanceOnly (R3 self), ROEHold | PassagePoint_28ID_SLOT0, 5,341 m straight | after T01's TASKCMPLT | 300 s, destination (17.8 m/s straight -> late path expected) | LONE PLATFORM: Move To, vertex 1 of 1 -> 54.028874,23.264401 |
| T14 1075b583... | 48 IBCT | FOLSPT -> advance along graphics and hold, ROEHold | ORIGINAL line: start -> 54.040348 / 23.324206 (the (e) destination), 2,426 m straight, obstacles as sec 0 | after T13's TASKCMPLT | 300 s, destination (8.1 m/s straight; the planned path is longer) | LONE PLATFORM: Move To, vertex 1 of 1 -> 54.040348,23.324206 |

(d) The FOLSPT dispatch line still says "The supported unit is not in the order" (cut A's change (f)); NOT scored (as
-1, -2).
(e) NO PRE-WARM LAUNCH, for -2 sec 1(e)'s reasons, unchanged [A]: warming has never changed the placement line on this AO
(0 of 36 from the terrain query on every Iron Storm launch, a warm re-launch included), and the maple area is gated anyway
(nav-area gate, 900 s, which covers the measured ~237 s COLD case). A cold area costs wait time, not correctness.
(f) P1 is RECORDED with its expectation written down (as -2 sec 1(f)): "0 of 36 ... TERRAIN QUERY, 36 from the FALLBACK"
and one PLACEMENT RE-CLAMP summary line.
(g) --parse-order does not resolve MapGraphicIDs [A: -1 1(f)]. The resolver [A: TaskGeometryResolver.cs :402-468] drops
every line vertex within 100 m of the taskee, chains lines nearest-end-first, and appends a Point graphic as the single
DESTINATION. T14 names ONE graphic, 7351f662 FollowAndSupport_48_IBCT_SLOT1, whose vertices are [48 IBCT's init
position, the (e) destination] (asserted by the derivation [V]): the first is dropped, the second joined, so T14's
route is [live start, 54.040348 / 23.324206], 2 points. T02 names its Point graphic only: [live start,
PassagePoint_28ID_SLOT0], 2 points (-2's T02 L-ROUTE read "(2 pts)" [V: run 20260927T021020Z, L1617]). T10: as -2, 4
points. `leg_check.py --dump-resolved` on the E1 order resolves T14 to [54.040348, 23.324206] alone, T02 to the
PassagePoint alone, T10 to [(h), (j), PassagePoint] [V].
(h) ONE engage-path line per DISPATCH PASS (P8), as -1 1(g) and -2 1(h).
(i) THE FORM is decided once, on the FINAL route (VrfC2SimService.cs :4975): one point -> MoveToLocation; a
non-aggregate ground mover with two or more points, not a patrol, with Vrf:PlatformMoveToPerVertex on -> Move To per
vertex; anything else -> CreateRoute + MoveAlongRoute. So T02 and T14 take Move To per vertex (one vertex each unless a
route shift inserts points), T10 the unit route task exactly as in -2, and the holds nothing.
(j) HARNESS NOTES carried from the -2 lane [A]: --parse-init needs the MAK PATH prefix
(PATH=/c/MAK/vrforces5.2d/bin64:/c/MAK/makRti5.0.1/bin:$PATH) or VrfBridge.dll is not found; the runner reads its
executables and the ledger from the MAIN checkout; StopVrf52's exit code is the teardown gate. NEW: a chained platform
logs no route line; since M1b the runner's report-evidence gate maps it from its MOVE TO PER VERTEX line and anchors it
on its LAST task-complete record. This run does not use --stop-when-complete, so that gate decides nothing here - its
'via' per taskee is RECORDED. The scorer (sec 4) keys on the console and DISPATCHED lines, and
tools/analysis/applog_chain.py (M1b's parser, `--selftest`) cross-checks the chain lines at the harvest. THE MODEL SET:
--model-set is NOT passed, so the runner's default EntityLevel is exported to the app as Vrf__ModelSet=EntityLevel and
the fixture is checked to load EntityLevel ("C2SIM_EntityLevel_AbstractGraphs.sms includes entitylevel.sms") [V: prep
dry run, sec 3 E].

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: --pre-order-gate nav-area --pre-order-gate-timeout 900

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: the watchdog runs on the SIMULATION clock (--env Vrf__StallClock=sim), window 360 SIM s.
Armed ends in SIM s from dispatch: T01 300 and T13 300 (no destination - never watched); T02 300 and T14 300 (destination
tasks: the armed end is INSIDE the 360 s window, but an unarrived destination task goes OVERDUE and stays watched, and
nothing is SENT at that end, so the end cannot pre-empt or manufacture a stall verdict); T10 450 (outside the window). A
stall verdict is possible from dispatch + 360 SIM s onwards for T02 / T14 / T10 only. The E1 FALSIFIER's 60 SIM s stall
is a separate and finer measure on the WatchVrf trace (sec 4), not the watchdog's.

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels.

DEVIATION FROM RECORD: the successor-gate floor is 60 s, not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:320); the computed wait is max(60, 300 + 60) = 360 SIM s, so T02 and T14 are not held two hours behind a late predecessor for no reason.

DEVIATION FROM RECORD: consoles at level 4, not the demo setting of plan P7 ("--object-console 3 --member-console 3"); the house template requires level 4 for a movement run and the movers here are lone platforms.

DEVIATION FROM RECORD: a FALLBACK placement does not stop this run, where DEMO_RUNBOOK sec 0.4 says of anything but N of N from the terrain query "Stop, warm the area, start again"; the record in -2 sec 1(e)1 shows warming does not change that line on this AO, and the re-clamp that repairs it is scored in P5.

Flag note (not a deviation): `--no-gui` is written explicitly in step E; it is the runner's default (scripts/RunScenario.sh
:41, :107) and -1 and -2 kept it. Vrf:PlatformMoveToPerVertex is NOT passed: the shipped default (ON) is what is under test.
--model-set is NOT passed either: the runner's default EntityLevel (sec 1(j)).

## 2. What the code emits - log-line shapes

src at 5e8d6f1 differs from -2's 80f707f by M1 (47da73e), D1 + M1b (b817609) and A1's model-set plumbing (3e5ab14), and
none of them removed or re-worded an existing log template: `git diff 80f707f 5e8d6f1` of VrfC2SimService.cs and
DeferredDispatch.cs removes only comments, the one-point test, two position reads and two format strings to which D1
appended a suffix that is EMPTY on EntityLevel (ARRIVAL EVIDENCE `{Leaf}`, STALL `onePosition`) [V]. So -2 sec 2's shapes
stand, at shifted line numbers [V at 5e8d6f1]: L-DISP (:5174 `DISPATCHED <name> task '<T>' (<kind>) at WALL <iso>Z,
SIMULATION clock <s> s`; a chained platform's kind is `move-to-per-vertex`), L-ROUTE (:5058 `Task '<T>': CreateRoute
'<route>' (<N> pts) for <name>; move deferred to route-created.`), L-MAR (:6024 `Route '<route>' (<uuid>) created;
MoveAlongRoute issued for <vrf>.`), L-SHIFT-ON (:741), L-SHIFTED (:6659), L-NOFLAG (:6698 `ROUTE SHIFT - no leg flagged;
the route is unchanged.`), L-CENSUS (:1672), L-CLOCK (:862), L-RATIO (:7018), L-WDOG (:7872), L-STALL (:7675), L-REPORTS
(:1081), and the resolver, CNFPSL, R3-self, FOLSPT, bare-move and completion shapes as -2 names them. NEW [V]:
- L-M1-ON (:771 -> VertexChain.cs :119-127): `MOVE TO PER VERTEX ON (Vrf:PlatformMoveToPerVertex, RL-20260927-01;
  FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27 sec 5.2): a LONE ground PLATFORM with a route of two or more points is driven
  as one Move To per route vertex ... A vertex completion farther than 100 m (Vrf:VertexArrivalRadiusMeters) from its
  vertex is logged VACUOUS (R11). ...`; its off twin begins `MOVE TO PER VERTEX off (`.
- L-MODELSET (:776 -> UnitPositionPolicy.StartupLine, D1): `MODEL SET for task judging: Vrf:ModelSet='EntityLevel' -> ...
  (D1, RL-20260927-01). A memberless aggregate is NOT judged ...`, INFO; a WARNING if the value is not recognised.
  L-JUDGES (:7401, D1): `TASK JUDGES: <name> (task '<T>') is an aggregate with NO members <S> WALL s after dispatch ...`.
- L-MOVETO (:5395): `Task '<T>': MOVE TO PER VERTEX for <name> (<vrf>) - vertex 1 of <N>: MoveToLocation (<lat>,<lon>);
  the other <N-1> vertex(es) are issued one at a time, each when the previous Move To COMPLETES. No route object is created
  (RL-20260927-01: ...).`
- L-VRFDONE (:8100): `VRF task complete: <marking> / <taskType> (success=<True|False>)`.
- L-VTX (:5436) `VERTEX CHAIN <marking> task '<T>': vertex k of N COMPLETED - the unit is D m from it and moved M m since
  <dispatch|vertex j>; issuing vertex k+1 (...)`, L-VTX-ISSUE (:5559) `VERTEX CHAIN ... vertex k of N issued -
  MoveToLocation (<lat>,<lon>) (...)`, and L-VTX-LAST (:5443) `VERTEX CHAIN <marking> task '<T>': LAST vertex k of N
  COMPLETED - the unit is D m from it and moved M m since dispatch<suffix>. The chain ends and this completion goes to the
  task's own completion rules ...`.
- THE CHAIN FAILURE FAMILY, all PREDICTED ABSENT: `VERTEX CHAIN` lines carrying VACUOUS (:5428, :5453), FAILED (:5463),
  SWALLOWED (:5471, :5481) or `is NOT issued` (:5541, :5552); `is ALREADY driving this task's MOVE TO PER VERTEX chain`
  (:5345); `the MOVE TO PER VERTEX chain of task ... ends at vertex` (:5147, :5775); `MOVE TO PER VERTEX chain(s) FROZEN`
  (:7566). NOT in that family: the service's own `VRF completion for <unit> after the arrival-evidence report of task
  <T> - swallowed.` (:8116, lower case), the benign path when arrival evidence reported first (RECORDED).
- Vendor console lines, the object's own console at level 4 (`VRF console [n] <name> (VRF_UUID:...): ...`), as counted in
  -2: L-PROOF `C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true`; L-PLAN `Planned path has <N> points.`
  and `Node Is destination in nav area?: <success|failure>`.

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run unless marked PREP)

All from Git Bash at the MAIN checkout F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM, HEAD main
(50a7a62 when this was written; the deployed build is 5e8d6f1, docs only behind it). The runner writes its appNumber block into the MAIN checkout's working-tree
OPUS_EXECUTION_PLAN.md; that block is carried back to this branch afterwards (the IS1 / IS2 procedure).

A0. PRECONDITIONS: (1) the seat's go-live; (2) no other lane building, running a suite or an agent (the two offline lanes
    of PLAN_MOVEMENT row M2 / A1 finished or paused - the quiet period cannot start before); (3) the order the runner reads
    is byte-identical to the registered one - data/IRONSTORM_CUTA_E1_Order.xml of the main checkout once the seat has
    merged this branch, or else this worktree's absolute path - sha256 7d5b4034...ddb9a either way; (4) the deployed build is
    still 5e8d6f1 (exe af19d6be..., dll 24982412...) and `git diff --stat 5e8d6f1..main -- src` is EMPTY. If main's src
    has moved on (M2 merges AFTER E1 by the plan), NOTHING is rebuilt: the registered build is what runs, and the
    difference is recorded. If the DEPLOYED hashes differ (another lane rebuilt the main checkout's output folders), STOP
    before D: the BINARY line above no longer describes what would run, and the seat decides between a rebuild of the
    registered source (a new hash, re-registered) and a new registration.
A.  Read-only checks, re-run immediately before E: DOTNET_ENVIRONMENT empty; 0 Vrf__ variables; processes - rtiexec 47980,
    rtiForwarder 50740 and rtiAssistant 30240 up and never touched; no RtiProbe (before D), vrfNavGenerator, build, vrfSim,
    vrfGui, VrfC2SimApp, WatchVrf or ListenReports; REST 200 on 18080; exe / dll / appsettings / bridge / fixture / order /
    init hashes as registered; `derive_ironstorm_cuta.py --check` exit 0 with three CHECK lines; the maple area as at
    registration [V prep: C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform IRONSTORM-CENTRE_maple,
    4,764 files, 287,120,328 B, newest 2026-09-26T23:05:00Z; its .navRuntimeConfig sha256 40b46032...8cddfb]; the
    marker read.
B.  No build (done in the preparation, above).
C.  Order validation (no federate, no appNumber):
    - C1 `VrfC2SimApp.exe --parse-order <E1 order>` [PREP V with the deployed 5e8d6f1 exe, exit 0; the same output on
      b984945's]: "Tasks: 5"; durations 1200000 ms x 4 and 1800000 ms
      (T10); T10 mapGraphic c8d9cd1a, 51a59f89, cc23071f in that order; T14 mapGraphic 7351f662-f857-e05a-b533-f9a46e0fb095
      ONLY; T02 startAfter f7b52ba4, T14 startAfter 37677c40; T10 simStartMs 1200000; 0 warn / error / schema lines.
    - C2 `VrfC2SimApp.exe --parse-init data/IRONSTORM_CUTA_Initialization.xml "Not Set"` with the MAK PATH prefix [PREP V,
      exit 0]: "Units: 40", "would create (clientId=Not Set): 36".
    - C3 ONE real push to the PRIVATE server, at go-live, after this registration is committed:
          tools/PushInit/bin/Release/net10.0/PushInit.exe data/IRONSTORM_CUTA_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
          tools/PushOrder/bin/Release/net10.0/PushOrder.exe data/IRONSTORM_CUTA_E1_Order.xml 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
      EXPECT both exit 0 and one `ORDER (<n> chars)` echo carrying 5 tasks, 5 Durations and the verbs
      CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT. Any failure = STOP.
D.  THE HOLDER, FIRST: the -2 persistent holder (RtiProbe 42672, appNo 5133) is GONE [V 17:10Z: no RtiProbe]. A NEW
    persistent holder takes four fresh numbers M..M+3 from the marker, hand-claimed in Appendix B and committed on this
    branch BEFORE it joins (sec 5); the main checkout's working-tree OPUS_EXECUTION_PLAN.md then takes the branch copy
    (marker M+4) so the runner's marker read is right. The 64-bit pwsh is pinned (bare `pwsh` in Git Bash here is
    32-bit - RunScenario.sh's own banner), as -1 sec 3 D did; output to a file, never piped:
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers M,M+1,M+2,M+3 -SettleSecs 28800 -WhatIf
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers M,M+1,M+2,M+3 -SettleSecs 28800 < /dev/null > runs/launch52/e1-holder-<stamp>.log 2>&1
    EXPECT -WhatIf exit 0 with every precondition met, then "HOLDER JOINED: pid <p> appNo M" and exit 0; numbers not
    reached are BURNED. Exit 1 (none joined) or 2 (a precondition) = STOP, no blind relaunch (RUNBOOK 9c).
E.  DRY RUN, then THE RUN once, stdout to a FILE, never piped:

        scripts/RunScenario.sh \
          --scenario IronStorm_Centre_52_Nav_AG_maple \
          --init data/IRONSTORM_CUTA_Initialization.xml \
          --order data/IRONSTORM_CUTA_E1_Order.xml \
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
          --log runs/launch52/RunScenario-ironstorm-e1-<stamp>.log

    --order (here and in C3) is data/IRONSTORM_CUTA_E1_Order.xml once the seat has merged this branch; unmerged, it is
    this worktree's absolute path (A0 (3)) - the registered sha256 decides, not the path.
    The dry run is the same line plus --dry-run and its own log name. PREP DRY RUN [V 2026-09-27T18:03:18Z, exit 0; a first
    one at 17:40:23Z on the superseded b984945 build also exit 0], with the order given as this worktree's absolute path:
    order accepted; "deployed VrfC2SimApp BUILD IDENTITY: git 5e8d6f1, written 2026-09-27T18:00:28Z"; "model set :
    EntityLevel <- default; exported to the app as Vrf__ModelSet"; type map data/unit-type-map-52.json declares
    EntityLevel and the fixture loads EntityLevel ("matches -ModelSet"); no holder present then, so it planned its block
    from 5170 (5170-5180, marker -> 5181); nothing launched, no run directory, marker unmoved, the main checkout's tracked
    tree unchanged (runs/launch52/RunScenario-ironstorm-e1-prep-dryrun-20260927T180318Z.log). THE GO-LIVE DRY RUN must
    show: the new holder recognised as a PERSISTENT FEDERATION HOLDER; build identity 5e8d6f1; model set EntityLevel; the
    block M+4..M+14 exactly as sec 5; marker would advance to M+15. A different layout is written into sec 5 BEFORE launch.

    Defaults kept: --profile 5.2; REST/STOMP 18080/61614, the PRIVATE server only; route shift ON; Move To per vertex ON; the
    runner's Stage 2h holder (it JOINS the federation the persistent holder keeps). GATE on teardown: StopVrf52's exit 0
    or 6 (P18); 3, 5 or 7 = STOP.
F.  Post-run, in the FOREGROUND: the post-run inventory (Win32_Process: only rtiexec, rtiForwarder, rtiAssistant, the
    persistent holder and, if still inside its hold, the Stage 2h holder; no vrfSim / VrfC2SimApp / WatchVrf /
    ListenReports) ends the quiet period; the hashes are re-read; then the harvest (sec 6).

QUIET PERIOD (RUNBOOK 0.5.14 item 5): from the launch of E to the post-run inventory (F): no Stop-Process / taskkill of
any kind (StopVrf52's own identity-gated force of the run's own back end is allowed), no build, no suite, no subagent, no
second runner - in this lane or any other. The executor polls the run's own files from the FOREGROUND with bounded waits
and does not end its turn while the run is open (HANDOFF sec 1, method lessons). Never touched: rtiexec, rtiForwarder,
rtiAssistant, any RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES. Every count is PER TASK UUID, read from the scored run's vrfc2simapp.log in line order and cross-checked against
reports-captured.log. DISPLACEMENT SINCE DISPATCH (P6, P6b) is measured on watchvrf-trace.csv POS rows exactly as -2 sec
4 defines it. ARRIVAL (P20, P20b) = the closest POS fix after dispatch to the destination is within 100 m by window end.
THE STALL MEASURE (P20F) - after the platform's first fix > 5 m from its dispatch fix and before its first fix within 100
m of its destination, a maximal run of consecutive fixes all within 5 m of the run's first fix. Its SIM length comes from
the trace's own CON rows (the level-4 console tick lines begin "<sim> <level> .", giving (trace t, SIM) pairs from one
file), and from the app log's L-RATIO chain where no such pair lies within 10 s (flagged). A stall of 30-90 SIM s is
reported BORDERLINE: the 2 s trace sampling is about +-15-30 SIM s at -2's ratios. "No OSM feature within ~10 m ahead" =
no OSM OBSTACLE - a building footprint or a water polygon (the z14 `osm` tiles), a waterway line within 1 m, or a wet
cell of the z14 osm-water raster (P21's instrument) - at any point of the 10 m half-disc in front of the stall point
(heading = the last >= 15 m of track before it). Recorded with it: the nearest feature of each class at any bearing, and
the nearest other trace object within 25 m. PATH SHAPE (P27): the track's offsets from T14's original line (east +), the
side it passes the lake on (800-1,100 m along), and its closest approaches to OSM water, the -1 lake stop, the -2 hamlet
stop and any building. THE SCORER is scratch u3\laneE1\e1_score.py <runDir>. ITS CONTROLS, run before this registration
[V]: on the -1 pre-warm trace it finds 48 IBCT's lake-edge stop (757 SIM s at 54.026779, 23.317195; water raster 0.0 m
and water polygon 1.0 m ahead -> a stall AT a feature), 54 wet fixes (the -2 lane's figure) and 28ID's arrival (1.0 m); on
the -2 trace, 48 IBCT's hamlet stop (32,172 SIM s at 54.030774, 23.327064; a building 5.0 m ahead, 4.4 m at any bearing
-> AT a feature), 0 wet fixes and 28ID's arrival (1.0 m). Its SIM spans agree with the app's own "<W> WALL s after
dispatch = <S> SIMULATION s" lines to 3% (T02's -2 arrival: 502.9 against 488.1). The control also caught one defect in
the scorer (a waterway false positive), fixed before the second pass.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) The NEW persistent holder (appNo M) alive at launch and recognised PERSISTENT by the runner; the back end JOINS (rtiexec count-grep: no create for its appNo); "READY - joined the federation". (ii) L-CENSUS reads 4 EMPTY shells and 32 platforms (36 = C2's in-scope count). (iii) L-WDOG once, naming the SIMULATION clock; L-CLOCK names SIMULATION and "Vrf:DurationScale=0.25"; L-SHIFT-ON once. (iv) The runner's stage 7d gate FIRED (no NOT-READY, no exit 3). (v) Exe / dll / bridge hashes as registered, before and after. (READY TO TASK is RECORDED, not scored.) | HIGH | Any limb = VOID + STOP (launch, terrain or harness failure - no code verdict). | |
| P1 | PLACEMENT ALTITUDE SOURCE: the init's L-PLACE line and the PLACEMENT RE-CLAMP summary line, recorded. Expected (sec 1(f)): "0 of 36 ... TERRAIN QUERY, 36 from the FALLBACK" and one re-clamp summary. | RECORDED | - | |
| P2 | AREA: the FIRST L-AREA row names `NavArea-ground-platform IRONSTORM-CENTRE_maple` and precedes L-ORDER; zero L-AREA rows name any other area (empty-area rows RECORDED); runner "NAV AREA ACQUIRED ... IRONSTORM-CENTRE_maple". WARM/COLD and the gate wait RECORDED. | HIGH | No row before L-ORDER, a gate timeout, or another named area = STOP. | |
| P3 | THE PLANNER RUNS ON THE LONE PLATFORMS: >= 1 L-PROOF line (`C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true`) on 48 IBCT's OWN console and >= 1 on 28ID's (a Move To on a ground vehicle starts ground-vehicle-move-to, moveToTask.h :181-184); and >= 1 for a 1-112 IN member, as -2. (-2 recorded NONE for either lone platform: Move Along Route never enters the script.) | HIGH | Zero on either lone platform's console = STOP (the frame's mechanism did not run). | |
| P3b | PLANNING EVIDENCE: >= 1 `Planned path has <N> points.` (N >= 2) on 48 IBCT's console and on 28ID's; each `Node Is destination in nav area?` result RECORDED. | MEDIUM | None for a lone platform = recorded MEDIUM miss (a destination the mesh does not cover takes the feature planner, whose lines were never seen here). | |
| P4 | DISPATCH STRUCTURE: exactly one SENT TASKSTRT per uuid (5); T02's L-DISP after T01's SENT TASKCMPLT and T14's after T13's; one "Task 'T10...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25) - it will not dispatch before then." line and T10's L-DISP after it; ZERO L-SKIP / "SKIPPED: predecessor". | HIGH | A missing or duplicate TASKSTRT; a successor dispatched before its predecessor's TASKCMPLT; any SKIP = STOP. | |
| P5 | RE-CLAMP: an L-RC-PASS "is ON the terrain" gate line for each of T02, T14, T10; ZERO "measured OFF the terrain" gate lines; ZERO retired lines (`NOT DISPATCHED YET`, `HELD as`, `REFUSED [`, `BOUND-BUT-NOT-ON-THE-GROUND`). A mover whose terrain profile fell back to Live has no gate line: recorded NOT MEASURED, not a miss. | HIGH | An OFF gate line or a retired line; a mover with a profile reply and no gate line = STOP. | |
| P6 | T02's 28ID platform AND T14's 48 IBCT platform are each displaced MORE THAN 50 m after dispatch (displacement since dispatch, WatchVrf trace - NOT the stall line's windowed max), by window end. | HIGH | Either at or under 50 m = STOP. | |
| P6b | T10: at least one 1-112 IN member displaced more than 50 m after T10's L-DISP (trace). | HIGH | No member over 50 m = STOP. | |
| P6c | Per mover: L-PLAN lines, max speed and net displacement from the trace, any mesh "not enough (0) points" lines. | RECORDED | - | |
| P7 | CNFPSL HOLDS T01 and T13: each has exactly one L-CNFPSL + one IN PLACE line; one Observation in the capture naming STP-866; an armed line "it has no destination" at 300; NO L-ROUTE, L-MOVETO or MoveToLocation for it; exactly one SENT TASKCMPLT "(300 s after dispatch)". | HIGH | A move issued for a hold; zero or two TASKCMPLTs; a missing L-CNFPSL; an OVERDUE for a hold = STOP. | |
| P8 | T02 PLATFORM PATH: L-R3SELF for T02 on every dispatch pass and on no other task (passes = 1 + L-SHIFTQ lines for T02 + terrain-profile-request lines for T02); ZERO L-FAW, ZERO `FireAtTarget`, ZERO `deferred until the move COMPLETES` lines in the whole log. | HIGH | Any fire-at-will / FireAt / deferred-engage line; no L-R3SELF for T02; a count unequal to T02's pass count = STOP. | |
| P9 | T14 FOLSPT: exactly one L-FOLSPT naming 'ROEHold'; exactly one L-MOVETO for T14 and NO L-ROUTE for T14; an L-ARM-D (destination) at 300. | HIGH | Missing, duplicated, another ROE, an L-ROUTE for T14, or no move = STOP. | |
| P10 | T10 CRESRV: L-BARE for T10; an L-ROUTE "(4 pts)" for T10 (4 + 2 per L-SHIFTED line for T10) followed by its L-MAR; an L-ARM-D at 450; no IN PLACE line for T10. | HIGH | Any limb failing = STOP. | |
| P11 | COMPLETION PER THE TIME RULES (RL-20260921-09): each mover (T02, T14, T10) takes EXACTLY ONE path - (a) early: arrival, one L-HELD, one TASKCMPLT at L-END "had already arrived"; (b) late: one L-OVERDUE, nothing SENT at the end, then arrival, L-LATE, one L-CMPLT-L; (c) not arrived by window end: L-OVERDUE and no terminal report; (d) stalled: L-STALL + one SENT TASKABRT "STALLED". On a chained platform the arrival is the app's arrival evidence or its LAST vertex's completion, whichever comes first; an early one is HELD to start + Duration (dispatch + 300 SIM s). Which path: RECORDED; T02 late (b). | HIGH (exactly one path each; no TASKCMPLT before start + Duration); MEDIUM (T02 late (b)) | Two paths, a TASKCMPLT before start + Duration, or a TASKCMPLT at an end time for an unarrived mover = STOP. T02 not (b) = recorded MEDIUM miss. | |
| P11b | T14 COMPLETION PATH: late (b) - one L-OVERDUE, then arrival and one TASKCMPLT on arrival (L-CMPLT-L). Arithmetic: 2,426 m straight at the ~9 m/s 48 IBCT averaged in -2 is ~260 SIM s; the planner's way round two water bodies and a hamlet, plus its planning time, is expected to pass the 300 s end. | MEDIUM | Any other path = recorded MEDIUM miss. | |
| P12 | REPORT HYGIENE: ZERO L-SUPP for a terminal code; at most one terminal report per uuid; ZERO `task=(none)` SENT lines; capture and log agree; "Reports this run: ... 0 FAILED". | HIGH | Any of these = STOP. | |
| P13 | STALLS / FALLBACKS: every L-STALL, OVERDUE and arrival is REPORTED with the member displacement over the preceding 360 SIM s and since dispatch. | RECORDED | - | |
| P14 | NO ENGAGEMENT: zero L-ENGAGE / FireAt lines (P8) and zero vendor-console lines with "Fire Weapon" in the app log. | HIGH | Any engagement line = STOP. | |
| P15 | ROUTE SHIFT: L-SHIFT-ON once; ZERO L-SHIFTED lines; an L-NOFLAG line for each of T02, T14 and T10 (`leg_check.py --step 2 --no-chain --osm-water`: T14's original leg ratio 0.05, T02 0.11, T10's three legs ok [V]; the shift flags on grade and reads no OSM water). | MEDIUM | A ROUTE SHIFTED line = recorded; P10, P19b, P23 and P25 then count its inserted vertices, and a shifted lone platform's chain has 2+ vertices - the continuation is then exercised, and the Result says so. | |
| P16 | CLOCK: every L-RATIO line; the SIMULATION stamp of each L-DISP; the sim/wall band. | RECORDED | - | |
| P17 | RUN HEALTH: no `BACK END LOST`; no WS-runaway exit 6 (runner); no new .dmp / .callstack.log for this back-end pid (names only); VrfC2SimApp exits 0; rtiexec, rtiForwarder, rtiAssistant 30240 and every holder untouched. | HIGH | Any limb = VOID. | |
| P18 | TEARDOWN, scored on StopVrf52's exit code inside the runner: 0 or 6 (6 = the run's own back end force-stopped by identity, including one still exiting at the 60 s post-force deadline), and no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left. RECORDED beside it: StopVrf52's post-force wait line (`forced pid N still exiting after S s` or the clean exit-6 line, StopVrf52.ps1 :625-635 since 6e7295d) and the runner's own post-check. | HIGH | Exit 3, 5 or 7 = STOP. | |
| P19 | T10 RESOLVED ROUTE (identical geometry to -2): T10 resolves to [(h) wp 54.029734 / 23.305499, (j) wp 54.024000 / 23.313000, PassagePoint_48_IBCT_SLOT0]. T10's distinct L-RESOLVE lines are exactly: for c8d9cd1a and for 51a59f89 each "1 vertex(es) dropped - they ARE the taskee's own position"; "path from MapGraphicID c8d9cd1a-... (line, 1 vertices): 1 vertex(es) joined" (no gap suffix - first graphic); "path from MapGraphicID 51a59f89-... (line, 1 vertices): 1 vertex(es) joined <d> m after the previous graphic's end" with d in 795-815; cc23071f -> PassagePoint_48_IBCT_SLOT0 "(<kind>, 1 vertex): appended as the route's DESTINATION"; zero resolver WARNINGs for T10. T10's L-ROUTE reads "(4 pts)" (4 + 2 per L-SHIFTED line for T10, if any). | HIGH | Any other vertex list, drop/join count or order, destination, a resolver warning for T10, or another L-ROUTE point count = STOP. | |
| P19b | T14 RESOLVED ROUTE - THE RUN'S ONE VARIABLE, so HIGH (MEDIUM in -2): exactly one "MapGraphicID 7351f662-f857-e05a-b533-f9a46e0fb095 -> FollowAndSupport_48_IBCT_SLOT1__FRIENDLY_FOLLOW_AND_SUPPORT (line): 1 vertex(es) dropped" line and one "path from MapGraphicID 7351f662-... (line, 1 vertices): 1 vertex(es) joined." with NO gap suffix; ZERO resolver lines naming 7ff48b93 (the (i) graphic); zero resolver warnings for T14; T14's L-MOVETO reads "vertex 1 of 1: MoveToLocation (54.040348,23.324206)". With an L-SHIFTED line for T14: vertex 1 of 1 + the inserted count, and the first inserted point - recorded. | HIGH | Any other T14 route (another graphic, another drop / join, another destination) = STOP: the run would not test the registered line. | |
| P20 | HEADLINE: T14's 48 IBCT reaches within 100 m of its destination 54.040348, 23.324206 (closest trace fix after dispatch) by window end - on its ORIGINAL line, round the water and the hamlet, by the planner. | HIGH | Not within 100 m = STOP. NAMED OUTCOMES, each recorded with the stop point, its obstacle report (the STALL measure) and the object's last console lines: (1) the P20F condition; (2) a stall AT an OSM feature (water or building within 10 m ahead); (3) a chain FAILED line (the planner aborted visibly, PathPlanFailure) and its TASKABRT; (4) still moving at window end. Not a named outcome of P20 but of P11 / P12: (5) a watchdog STALL (TASKABRT, report-only by design) followed by recovery and arrival - two terminal reports for one uuid, so P11 and P12 miss and the run STOPS as a recovery after a stall. | |
| P20b | T02's 28ID reaches within 100 m of PassagePoint_28ID_SLOT0 54.028874, 23.264401 by window end (closest trace fix). | HIGH | Not within 100 m = STOP, with the same named outcomes. | |
| P20F | THE FALSIFIER (PLAN_MOVEMENT sec 3): NO stall of T14 of 60 SIM s or more with no OSM feature within ~10 m ahead (the STALL measure above; a BORDERLINE 30-90 s stall is named as such). The same measure is RECORDED for T02. A platform that never moves 5 m is P6's, not P20F's. | HIGH | The condition met = the frame of RL-20260927-01 is wrong for this vehicle and ground: STOP and ask; no re-run under this registration. | |
| P21 | NO WATER ON ANY TRACK: no POS fix of any object that moved > 50 m (28ID, 48 IBCT, 1-112 IN and its members) falls in an OSM water cell (the z14 osm-water raster, leg_check.load_osm_water, scratch u3\laneI2\osm\osm-water); a fix on an absent tile is counted UNKNOWN, never dry. For T14 this now reads whether the PLANNER kept it out of the water its line crosses. | MEDIUM | >= 1 wet fix, or any mover fix on an absent tile = recorded MEDIUM miss (a shore-hugging path can read wet at the raster's ~5.6 m cell). | |
| P22 | M1 AND D1 ARE THE BUILD: L-M1-ON exactly once, naming "100 m (Vrf:VertexArrivalRadiusMeters)"; ZERO `MOVE TO PER VERTEX off (` lines; L-MODELSET exactly once at INFO, naming `Vrf:ModelSet='EntityLevel'`. | HIGH | Missing, duplicated, the off line, or a MODEL SET WARNING / another model set = STOP (not the registered build or setting). | |
| P22b | NO UNJUDGED MOVER: ZERO L-JUDGES ("TASK JUDGES: ... is an aggregate with NO members") lines - 1-112 IN's members reflect as in -2, and the two lone platforms are not aggregates. | MEDIUM | Any L-JUDGES line = recorded MEDIUM miss (that mover's arrival and stall are then unjudged by the app; the trace still scores P6b / P20). | |
| P23 | THE FORM PER MOVER: exactly one L-MOVETO for T02 (28ID, "vertex 1 of 1: MoveToLocation (54.028874,23.264401)") and one for T14 (P19b); ZERO L-ROUTE and ZERO L-MAR for T02 or T14 (no "CreateRoute" naming the T2_ or T14_ task, no MoveAlongRoute for 28ID's or 48 IBCT's VRF_UUID); for T10 exactly one L-ROUTE and one L-MAR (P10) and ZERO L-MOVETO naming 1-112 IN. With an L-SHIFTED line for a lone platform: "vertex 1 of N" with N = 1 + its inserted points. | HIGH | Any other form for any mover = STOP. | |
| P24 | VENDOR COMPLETIONS: for T02 and for T14, if its track comes within 15 m of its destination (the 5.2 near-distance, CLAUDE.md D7 / Y-13): exactly one L-VRFDONE for its marking, success=True, whose type CONTAINS "move-to" (VertexChainPolicy.IsChainMoveToType, the chain's own test); the literal type is RECORDED (expected "move-to", moveToTask.h :60 - never yet seen here: every run log on this machine carries only move-along, patrol-route and fire-at-target [V]). ZERO L-VRFDONE for either marking of another type or with success=False. T10: one "... / move-along (success=True)" if it arrives, as -2. A platform ending 15-100 m from its destination is RECORDED. | HIGH | A second completion, another type, success=False, or none for a platform whose track came within 15 m = STOP. | |
| P25 | THE LAST VERTEX: for each lone platform with an L-VRFDONE, exactly one L-VTX-LAST "LAST vertex 1 of 1 COMPLETED - the unit is D m from it" with D < 100 and no VACUOUS suffix; ZERO lines of the chain failure family (sec 2); ZERO L-VTX and L-VTX-ISSUE lines (there is no vertex 2 without a route shift; with an L-SHIFTED line, one L-VTX + one L-VTX-ISSUE per intermediate vertex, each D < 100 - recorded, and the continuation is then exercised). The benign lower-case "- swallowed." line of :8028 is RECORDED, not scored. | HIGH | A missing, duplicated or D >= 100 LAST line, or any failure-family line = STOP. | |
| P27 | RECORDED (the seat's list): the planner's path shape round the lake, the second water body and the hamlet (PATH SHAPE); route-shift lines per mover; the `Node Is destination in nav area?` results; the placement altitude source (P1); the teardown code and the post-force wait line (P18); the report-evidence 'via' per taskee in the runner log (sec 1(j)); the literal type string of every L-VRFDONE. | RECORDED | - | |

(P26 is not used: "TASKCMPLT held to start + Duration" is scored inside P11.)

WHY P3, P20, P20b AND P20F ARE HIGH: they are the frame's own predictions (FINDING_GROUND_MOVEMENT_PRACTICE secs 0 and 5.2,
PLAN_MOVEMENT sec 3), each backed by vendor text (UG52 23.1-23.2.2, moveToTask.h :181-184) and never observed here - E1
is the observation (PLAN sec 6), and a miss on any of them is a reason to stop and understand, not to adjust. WHY P3b,
P11b, P21 AND P22b ARE MEDIUM: the planning line's wording depends on which planner answers; the late path depends on a
planned-path length nobody has seen; a path that hugs a shore can read wet on a ~5.6 m raster without being in the water;
D1's warning is one day old and has never run live.

STOP RULES:
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P17 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- P20F met is reported as the frame's falsifier: STOP and ask (PLAN_MOVEMENT sec 3), before anything else is run.
- The executor never intervenes in the window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as IRONSTORM_CUTA_E1-<date>-2, with new appNumbers.

ONE VARIABLE: for the headline mover T14, the TASK a lone ground platform gets - Move To per vertex (RL-20260927-01)
instead of Move Along Route - on the SAME line, the (e) line of the pre-(i) order; the CONTROL is run 20260927T003120Z (the
-1 pre-warm, unscored: 48 IBCT on Move Along Route drove ~850 m of that line and stopped dead 0.5 m from the OSM lake).
Other differences from that control, named so that no claim rests on them: the build (5e8d6f1 against 80f707f - M1; D1,
which changes nothing on EntityLevel but one start-up line and a memberless-aggregate warning; M1b and A1's runner
changes; StopVrf52 7ba070a / 6e7295d, which touch teardown only), T10's (j) waypoint, the run length (2,700 s against
120 s) and the persistent holder. Against run -2 (20260927T021020Z) T14 differs in BOTH its route ((i) removed) and its task, so -2 is
a reference, not the control.

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5170 ***` at 5e8d6f1 [V]. NOTHING is claimed by this registration (the
preparation phase's hard stop). At the go-live, from the marker M read then (5170 unless another run has moved it):
- the persistent holder: M, M+1, M+2, M+3, hand-claimed in Appendix B and committed on this branch BEFORE it joins; one
  attempt per number until one joins; numbers not reached are BURNED, never reused; marker -> M+4;
- the runner's SCORED block, written by the runner at its Stage 2 (-2's layout): M+4 back end, M+5 front end (BURNED,
  --no-gui), M+6 WatchVrf pre-check, M+7 WatchVrf trace, M+8 VrfC2SimApp, M+9 RtiProbe 2c, M+10 CreateOne (BURNED unless
  the oracle gate fails), M+11 to M+14 Stage 2h holder attempts (M+11 JOINS; M+12 to M+14 BURNED); marker -> M+15.
At M = 5170: holder 5170-5173, runner block 5174-5184, marker -> 5185. The go-live dry run confirms the block; a different
layout is written here BEFORE launch. A launch that aborts burns its whole block. PushInit / PushOrder / ListenReports /
StopIface are C2SIM clients, not federates.

## 6. Harvest (after the run, read-only) and where results go

From the run directory (runs\launch52\last-run-dir.txt): vrfc2simapp.log, reports-captured.log, c2sim-bus.log, the
manifest, watchvrf-trace.csv, thread-samples.csv, holder logs, stopvrf logs, the wrapper log; the scorer scratch
u3\laneE1\e1_score.py <runDir> (sec 4, controls above). Vendor sim logs dump the environment in cleartext: count-grep only,
never quoted. Results go to: the Result block below (measurement and implication in separate sentences);
PLAN_MOVEMENT_2026-09-27.md row E1; DEMO_READINESS row 16; HANDOFF sec 6 (200 x 160 cap); the RUNBOOK sec 11 M1 bullet's
"NOT YET SEEN LIVE" items that E1 settles; Appendix B annotated from the manifest. ASCII + CRLF throughout.

## 7. What this run does NOT claim

- The per-vertex CONTINUATION (vertex k -> k+1), unless a route shift inserted points into a lone platform's leg (P15
  predicts none): E1 exercises the planning task on a 1-vertex chain (sec 0).
- That the planner clears lakes or hamlets in general: n = 1, one M577A2 on one line, one M1A2 on open ground.
- That per-vertex Move To is better than one Move To to the destination: for a two-point route they coincide (FINDING sec
  5.2), which is all E1 drives.
- Anything new about units: T10's code path is -2's.
- Anything about M2's C# OSM pre-flight (vertex nudges, OSM water / building reports): it is NOT in this build, on purpose
  (PLAN_MOVEMENT row E1: "M2 merges AFTER E1 on purpose"), so the planner is tested alone.
- No unit ATTACK or fire at will; no BREACH; CNFPSL as a real passage (held in place); following 116 ABCT (not read); T02
  as an attack (STP-846's fallthrough for RECEIVE).
- Nothing about vegetation fidelity, the full 23-task order, the aggregate profile (run G1), the demo server
  (8080/61613), the GUI or the demo profile.
- No timing generalisation: n = 1, one host, one fixture, a load-dependent clock.

## Result (written after the harvest, never from a live read)

PENDING - the run has not been launched (REGISTERED - LAUNCH PENDING; the go-live is the seat's).
