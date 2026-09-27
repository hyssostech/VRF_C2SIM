# PREREG - IRON STORM CUT A, RUN E2: the ruled order on Move To per vertex with M2's OSM pre-flight - T14 as a 2-vertex chain through the (i) waypoint (one scored run)

STATUS: REGISTERED - LAUNCH PENDING (seat's go-live). Registered by lane E2 (session 5fc25950; the registration
commit's own time is the stamp) on branch run/ironstorm-cuta-e2 from main 53215e9, BEFORE any order push, holder
action or launch. PREPARATION ONLY so far: no C2SIM push, no holder start, no VR-Forces or runner launch (one runner
DRY RUN, sec 3 E), no appNumber claimed. The quiet period cannot start before the offline design lane C1 has
reported. Written from E1's registration (PREREG_IRONSTORM_CUTA_E1_2026-09-27.md - its settings, sequence, P0-P27
and its scorer, carried where still valid, with its lessons: P19b's "distinct" wording and the unexplained nav-area
verdict on T14's destination) and the seat's E2 brief. Marks: [V] = checked while writing this file; [A] = taken
from the record, not re-checked.

## Registration

PREREG ID: IRONSTORM_CUTA_E2-2026-09-27-1
DATE (UTC): 2026-09-27, before any launch (the registration commit's own timestamp is authoritative; the launch waits
for the seat's go-live)
BINARY / COMMIT: main 53215e9, whose src, tools and scripts are those of 0849334 (`git diff --stat 0849334 53215e9 --
src tools scripts data config` is EMPTY [V]; 53215e9 is the seat's one-line PLAN_MOVEMENT commit). 0849334 = M1
(47da73e, merged d75a8d0) + D1/M1b (b817609) + A1 (3e5ab14) + the E1 records (41ae9fa) + M2 (6bc0493 + c37290e, merged
0849334: the C# OSM pre-flight, the vertex check, the per-model-set leg rule). BUILT AND DEPLOYED BY THIS LANE
2026-09-27T20:06:51-20:07:13Z per RUNBOOK sec 9 into the MAIN checkout's own output folders (never C:\MAK), `dotnet
build <csproj> -c Release -p:BridgeConfig=Release-5.2 -t:Rebuild -m:1 -nodeReuse:false`, 0 errors, VrfC2SimApp's
documented 4 CA2024 + 2 CS8632, the others 0 [V]: VrfC2SimApp.exe sha256
d12ca223a43909e53f6224875e8caf0a82b92a4e53dd41802dcdc89ba65bff45 (162,304 B), VrfC2SimApp.dll sha256
37e2adeef7f5378bf875bb900dd33d14e0c1968cb4d1f29877f9edb1e9f46302 (1,664,512 B), ProductVersion
1.0.0+git.53215e9.Release-5.2 (exe and dll, no +DIRTY; NOT git.0849334 - main moved to 53215e9 before the build, docs
only, as E1's 5e8d6f1 against 3e5ab14); deployed appsettings.json 5e85e4b5ba24202442c9903ada6ea7974f5abb714a5781fdfed1feda5d08daa2
= src (PlatformMoveToPerVertex true, VertexArrivalRadiusMeters 100, ModelSet EntityLevel, PreflightRouteShift true,
PreflightCacheDir "" -> the exe's own preflight-cache, PreflightOffline false, PreflightBuildingClearanceMeters 10,
PreflightVertexNudgeMaxMeters 300); appsettings.Demo.json 12391080...f8f6ca = src; VrfBridge.dll
90272bc95297e3304066218bd5a53128ef8531cfbd6b4dcec3ce5c65b0f25847 (the pin, NOT rebuilt) in all eleven output trees
and the build directory. TEN consumers rebuilt in place; the ELEVENTH, tools/RtiProbe, NOT: its bin\Release-5.2 tree
is held open by the persistent holder RtiProbe 45600 (the RUNBOOK sec 9 precedent of 2026-09-26 19:58Z); `git diff
5e8d6f1 53215e9 -- tools/RtiProbe tools/Shared` is EMPTY, so its tree (RtiProbe.dll 070d3e34..., 18:00:32Z) is E1's
build of unchanged code, and a -t:Rebuild into a scratch output compiled 0 errors / 0 warnings at 53215e9 [V].
Offline suites against the DEPLOYED exe [V 20:08:42-20:09:29Z, MAK PATH prefix, DOTNET_ENVIRONMENT empty, 0 Vrf__]:
26 of 26 exit 0 (E1's 25 + --osm-selftest, 139 PASS); the three --disabled fail-first arms exit 1 as designed;
--rulings-selftest 431 PASS / 0 FAIL; --routeshift-selftest 90 ok / 0 fail, 0 network fetches; --preflight-selftest
72 PASS; --osm-selftest on the lane-I2 real tiles 143 PASS (the (e) line flags for OSM water and ends NO CLEARED LINE
on both model sets - M2's record reproduced - 0 fetched); `DOTNET_ENVIRONMENT=Demo --runtime-check` "runtime-check:
OK". `git diff --stat 0849334..HEAD -- src` on this branch is EMPTY [V at the registration commit]. Fixture
C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Nav_AG_maple.scnx sha256
57465c3545e884f0c32d283e945f35ce6694233cc17a0bc14116dd4aa44a0e32 [V]. The tile cache the pre-flight reads: sec 1(k).
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: UG52 sec 23.1 p500 (Move To "Moves the vehicle to a destination location, causing the entity to first
plan a route on roads and around obstacles as appropriate, then following that planned route using Move Along Route and
road-movement tasks"); UG52 23.2 / 23.2.1 p500-501 (roads, nav meshes and feature-obstacle planning); UG52 23.2.2 p502
(recovery - back up, drive around, one replan - Move To only); UG52 23.3 p505 (Move Along Route: "There is no path
planning done as it moves toward the next vertex"); MG 2.4 p18-19; vrftasks/moveToTask.h :60 (the task type is
move-to) and :181-184 (a move-to on a ground vehicle starts ground-vehicle-move-to, "which does more intelligent path
planning"); vrfcontrol/vrfRemoteController.h :1651 (moveToLocation); the vendor's own per-vertex shape,
move_along_route_and_continue.lua :31-58 (a move-to-location subtask per route vertex) [A: FINDING_GROUND_MOVEMENT_
PRACTICE sec 5.2]; ground-vehicle-move-to.lua :1401-1404 (an unplannable vertex aborts visibly) and :423-460 (the only
endpoint adjustment is a road shoulder) [A: the same]; UG52 30.22 p598 / 30.24 p600 (a unit's Move Along Route plans a
path to each vertex for every member); UG52 23.5.1 p507 + Table 26 (deepLake -> deep-water, acceleration-factor 0.0);
M2's vendor chain for the OSM sets - osm.features.water.xml :6 and osm.features.xml :21 (the z14 tile sets the sim
streams), featureconfig.txt :423 (MAK_WIDTH 5 m) and :211 (road width 8 m) [A: RUNBOOK sec 12a];
singleTaskControllerComponent.h :191-200 (decideToGiveUpTask "always returns false").

OWN-RECORD CITATION:
- RL-20260927-01 (docs/RULINGS.md, "Go" on the decision brief: (b) Move To per vertex for lone platforms, (c) the
  pre-flight port with per-profile rules); RL-20260927-02 / -03 (containers; nothing in this run). docs/PLAN_MOVEMENT_
  2026-09-27.md rows M1, M1b, M2 (MERGED 0849334) and E1 (SCORED); sec 6 "Not claimed: ... the per-vertex continuation
  (vertex k -> k+1: E1 drove 1-vertex chains)". PLAN_MOVEMENT has no E2 row yet: the seat's brief is this run's plan
  entry (the seat's commit 53215e9 names "E2 prep lane running").
- IRONSTORM_CUTA_E1-2026-09-27-1 (PREREG_IRONSTORM_CUTA_E1_2026-09-27.md, SCORED: every HIGH held; T14 on Move To
  ARRIVED 6.9 m from its destination on its ORIGINAL line, no stall; M1b's gate not exercised, P27). Its Result's
  UNEXPLAINED item: the vendor judged T14's destination point outside the nav area; both platforms received their
  Move-To destination 10 m above the terrain (TerrainClearanceMeters 10, VrfSettings.cs :833-848) [A].
- IRONSTORM_CUTA_LIVE-2026-09-27-2 (PREREG_IRONSTORM_CUTA_LIVE2_2026-09-27.md, run 20260927T021020Z): THIS RUN'S
  CONTROL - the SAME ruled order (7a986137) with T14 on Move Along Route: 48 IBCT passed its (i) waypoint (closest fix
  25.8 m) and stopped dead mid-leg 2 at 54.030807, 23.327063, 1,077 m short, among the hamlet's buildings (3.2 m from an
  OSM footprint; the correction is on the unmerged branch fix/ironstorm-t14-hamlet) [V: this lane's scorer control,
  sec 4 MEASURES].
- RUNBOOK sec 11, bullet "GROUND MOVEMENT SINCE 2026-09-27": "NOT YET SEEN LIVE: the stop at each intermediate vertex
  ... and the runner's report-evidence gate for a chained platform (it runs only under -StopWhenComplete, which E1 did
  not pass)" - E2 is that look. RUNBOOK sec 12a (M2: the vertex check, the leg rule per model set, UNKNOWN is never
  clear, 404 markers, the lines to look for). RUNBOOK sec 9 (the eleven consumers, the pin), 9c (the persistent holder).
- data/IRONSTORM_CUTA_CHANGES.md (i) and (j): the ruled order is (a)-(j); (k) is on the unmerged branch and NOT in it.
- M1 source [V at 0849334]: VertexChain.cs (FormFor :88-94, StartupLine :113-132, VertexChainTracker :165-455);
  VrfC2SimService.cs StartVertexChain :5336-5407 (L-MOVETO :5397), ConsumeVertexChainCompletion :5416-5495, IssueNext
  Vertex :5534-5564. M1b: RunnerLib.ps1 Get-VertexChainNames :460-469, Get-VrfUuidByName :471-504, Test-ReportEvidence
  :797-927, Resolve-MarkingKey :575-581; suite section 9b. M2: Preflight/PreflightService.cs PreDispatch :523-536,
  CheckVertices :475-515; TileSource.cs LoadOsmTile :703-788, CensusOsmCache :805-837; VrfC2SimService.cs GetPreflight
  :6096-6196, QueueRouteShift :6777-6940, ReportVertexFindings :6685-6725, ReportOsmLegFindings :6303-6347.
- Rulings kept as they stand (as E1): RL-20260921-09; RL-20260913-03 and RL-20260914-01; RL-20260920-01 item 3 (route
  shift ON); RL-20260926-01; RL-20260921-06; RL-20260925-01; RL-20260921-02 item 3; STP-846, STP-847, STP-866.

## 0. Purpose, in plain words

E1 showed a lone M577A2 on Move To reach its destination round a lake and a hamlet that stopped the literal executor.
But E1 drove 1-vertex chains only, and M2's pre-flight was held out on purpose. E2 runs the RULED cut-A order - (i)
included - on main with M2: T14's route resolves to TWO vertices, [(i) waypoint, destination], so 48 IBCT gets Move To
to the (i) waypoint, then, when that completes, Move To to its destination. THE HEADLINE: the per-vertex CONTINUATION
works live - exactly two move-to completions for 48 IBCT, in order, the second at its destination - and on leg 2 the
planner takes the vehicle past the hamlet where the SAME route on Move Along Route stopped dead in run -2. THE
FALSIFIER stays E1's: T14 stalls for 60 SIM s or more with no OSM feature within ~10 m ahead and no vehicle within
25 m ahead. And M2's pre-dispatch stage runs live for the first time; on these routes it is predicted to CHANGE
NOTHING (no vertex moved, no leg flagged, no tile fetched), which is itself the prediction.

WHAT E2 CAN AND CANNOT PROVE, said before the run: one continuation (vertex 1 -> 2) on one vehicle; M2's stage on
three routes it is predicted to leave alone (so M2's nudge, its water detour and its river report are NOT exercised
live - they stay offline-proven, --osm-selftest); the runner's report-evidence gate under -StopWhenComplete, whose
chain-line mapping IS exercised and whose last-completion anchor is predicted NOT to engage (sec 1(l)).

## 1. Decisions taken from the record

(a) PERFORMERS - unchanged from E1 sec 1(a) [A]: 28ID = ONE M1A2 PLATFORM (template M1A2_Abrams_MBT); 48 IBCT = one
M577A2 PLATFORM (M577A2_Command_Post; max-speed 15 m/s, M577A2_Command_Post.entity :48 [V]); 1-112 IN = an AGGREGATE of
6 members (Tank Headquarters Section (USA)), created AtOrder. T02 is a platform ATTACK on itself (R3 self) ->
AdvanceOnly with ROEHold. The fire-at-will line is PREDICTED ABSENT (P8).
(b) DurationScale 0.25 as E1 (plan P7 / CHANGES.md "recommended", not a ruling on file).
(c) THE TASKS (the RULED order 7a9861372f07702fc136f91f63b8bf971650fcc91c20aa62803d73cfd869f5c7 [V: `derive_ironstorm_
cuta.py --check` exit 0, three CHECK lines], init 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec
[V]; scaled seconds are SIMULATION seconds):

| Task (uuid) | performer | verb -> decision | geometry / distance | start | armed end | form (VertexChainPolicy.FormFor) |
|---|---|---|---|---|---|---|
| T01 f7b52ba4... | 28ID (platform) | CNFPSL -> held in place (STP-866) | 4 graphics, not driven | delay 0 | 300 s, no destination | none - a hold issues no move |
| T13 37677c40... | 48 IBCT (platform) | CNFPSL -> held in place | 4 graphics, not driven | delay 0 | 300 s, no destination | none |
| T10 9aab7fe6... | 1-112 IN (aggregate, 6) | CRESRV -> bare move | start -> (h) 54.029734/23.305499 -> (j) 54.024000/23.313000 -> PassagePoint_48_IBCT_SLOT0 54.019389/23.313902, 1,451 + 804 + 516 m | SimulationTime PT20M -> 300 s | 450 s, destination | UNIT: CreateRoute (4 pts) + MoveAlongRoute, as E1 |
| T02 696fbb33... | 28ID | ATTACK -> AdvanceOnly (R3 self), ROEHold | PassagePoint_28ID_SLOT0, 5,341 m straight | after T01's TASKCMPLT | 300 s, destination | LONE PLATFORM: Move To, vertex 1 of 1 -> 54.028874,23.264401 (as E1) |
| T14 1075b583... | 48 IBCT | FOLSPT -> advance along graphics and hold, ROEHold | THE (i) ROUTE: start 54.019389/23.313902 -> (i) 54.014600/23.331500 (leg 1, 1,267 m, SE) -> the (e) destination 54.040348/23.324206 (leg 2, 2,902 m, N); 4,169 m | after T13's TASKCMPLT | 300 s, destination | LONE PLATFORM: Move To per vertex - vertex 1 of 2 -> (i), vertex 2 of 2 -> the destination |

(d) The FOLSPT dispatch line still says "The supported unit is not in the order" (cut A's change (f)); NOT scored.
(e) NO PRE-WARM LAUNCH, E1 sec 1(e)'s reasons unchanged [A]; the maple area is gated (nav-area gate, 900 s).
(f) P1 is RECORDED with its expectation: "0 of 36 ... TERRAIN QUERY, 36 from the FALLBACK" and one RE-CLAMP summary.
(g) THE RESOLVER, logged once per DISPATCH PASS (E1 P19b's lesson: the same lines recur on the route-shift re-entry,
so every resolver prediction below counts DISTINCT lines). `leg_check.py --dump-resolved` on the ruled order [V]:
T14 -> [(54.0146, 23.3315), (54.040348, 23.324206)] from graphics 7ff48b93 (the (i) Route) then 7351f662 (the
FollowAndSupport graphic), each first vertex dropped as the taskee's own position; T02 -> [PassagePoint_28ID_SLOT0];
T10 -> [(h), (j), PassagePoint_48_IBCT_SLOT0]. `VrfC2SimApp --parse-order` with the DEPLOYED exe [V, exit 0]: "Tasks:
5"; durations 4 x 1200000 ms and 1 x 1800000 ms (T10); T14 mapGraphic 7ff48b93-5a1e-5a9a-813f-8dda1df7e5dd THEN
7351f662-f857-e05a-b533-f9a46e0fb095; T02 startAfter f7b52ba4; T14 startAfter 37677c40; T10 simStartMs 1200000; no
warn / error line. `--parse-init ... "Not Set"` (MAK PATH prefix) [V, exit 0]: "Units: 40", "would create (clientId=
Not Set): 36". The same distinct T14 resolver lines as run -2 (same order) [V: -2 app log L1337-L1343]: "MapGraphicID
7ff48b93-... -> T14_Waypoint_48_IBCT__CUT_A_I_ROUTE_ROUND_LAKE_SE (line): 1 vertex(es) dropped", "MapGraphicID
7351f662-... -> FollowAndSupport_48_IBCT_SLOT1__FRIENDLY_FOLLOW_AND_SUPPORT (line): 1 vertex(es) dropped", "path from
MapGraphicID 7ff48b93-... (line, 1 vertices): 1 vertex(es) joined." (no gap suffix) and "path from MapGraphicID
7351f662-... (line, 1 vertices): 1 vertex(es) joined 2906 m after the previous graphic's end."
(h) ONE engage-path line per DISPATCH PASS (P8), as E1.
(i) THE FORM is decided once, on the FINAL route (VrfC2SimService.cs FormFor at the committed dispatch): T14's final
route is 3 points (live start + 2 vertices) -> MoveToPerVertex, N = 2; T02's 2 points -> N = 1; T10 a unit ->
CreateRoute + MoveAlongRoute. M2's stage runs BEFORE that decision (QueueRouteShift -> PreDispatch), so a moved vertex
or a shifted leg would change N - predicted not to happen (P26).
(j) HARNESS NOTES carried from E1 [A]: the MAK PATH prefix for --parse-init; the runner reads its executables, the
order and the ledger from the MAIN checkout; StopVrf52's exit code is the teardown gate; --model-set not passed (the
runner's default EntityLevel, exported as Vrf__ModelSet; the prep dry run confirms "matches -ModelSet").
(k) THE OSM TILE CACHE - DECIDED AND DONE IN PREPARATION [V]. The app reads z14 OSM tiles from
<exe dir>\preflight-cache\osm-water and \osm (Vrf:PreflightCacheDir empty -> the shipped fallback) and, online (the
shipped PreflightOffline=false), fetches what is missing; a 0-byte file is re-fetched online and is UNKNOWN offline.
The lane-I2 cache (scratch u3\laneI2\osm, fetched 2026-09-26) holds 225 osm tiles (0 empty) and 225 osm-water tiles,
97 of them 0-BYTE (the python fetcher's mark for any non-200). CHOSEN: option (a) with a gap-fill - the lane-I2 tiles
minus the 97 zero-byte files were staged, and the 97 were then fetched ONCE with the interface's OWN reader (a scratch
harness calling TileSource.GetOsmTile online): vr-theworld answered 404 for all 97 (0 carried features, 0 UNKNOWN),
each written as the interface's 30-byte absent MARKER, 96 fresh fetches in 3.3 s (31-125 ms each, median 33 ms). The
staged sets were copied into the DEPLOYED preflight-cache (osm-water 225 files = 128 tiles + 97 markers, 58,315 B; osm
225, 1,648,995 B; the 29 raster files untouched). CensusOsmCache on the deployed dir: osm-water 225, 0 zero-byte; osm
225, 0 zero-byte; 0 undecodable. REGISTERED CACHE MANIFEST (path:sha256 per file, sorted, 479 files) sha256
df8ede714d6d18120e126495578fb35afe1acd77c296a7e7032dd6967b3a4cd2 (scratch u3\laneE2\deployed_cache_manifest.txt).
WHY NOT (b): the cut-A legs need 4 of the 97 (T02's leg: osm-water 14_9248_11122, 14_9248_11123, 14_9250_11124,
14_9250_11125 - all "no water", now markers); fetched at dispatch they would cost ~0.2-0.6 s against the 30 s
route-shift timeout (measured above), but a fetch that hung would orphan the worker's lines at the timeout (the
VERTEX CHECK and ROUTE SHIFT predictions of P26 would then be discarded unsaid) - so the network is taken off the
dispatch path. WHY NOT offline: PreflightOffline=false is the shipped value; E2 changes no setting.
THE PREDICTION INSTRUMENT: the same scratch harness references the DEPLOYED VrfC2SimApp.dll and calls
PreflightService.PreDispatch per mover with GetPreflight's options (EntityLevel, OsmFeatures on, clearance 10 m, nudge
300 m, step 8, window 40, threshold 0.92, L13 -> L11) and ShiftOptions()'s defaults - the stage the live run executes,
on the live routes (the init positions stand in for the live starts: E1's trace had 28ID and 48 IBCT at them to 1e-6
deg). Its output on the deployed cache, offline and online alike (online: 0 fetched): sec 4 P26.
(l) THE REPORT-EVIDENCE GATE (M1b) UNDER -StopWhenComplete - REPLAYED OFFLINE ON E1'S OWN FILES [V: scratch
u3\laneE2\replay_evidence.ps1 dot-sources RunnerLib.ps1 and calls Get-VertexChainNames, Get-VrfUuidByName,
Get-TraceEvidence, Get-ReportCaptureEvidence, Get-AppLogPositionEvidence and Test-ReportEvidence exactly as the runner
does]: the chain lines name both platforms (full names) and map their VRF uuids (48 IBCT -> a4cbe933..., 28ID ->
83a522fb...; 1-112 IN through the route join -> a950852e...); but the trace's TSK rows are keyed by the TRUNCATED DIS
marking ('48_IBCT/28', '28ID__FRIE', '1-112_IN/28ID__FRIENDLY_INFANT'), which Resolve-MarkingKey does not map to the
init names (exact or '<name>~<tag>' only), so completionT and the anchor stay EMPTY for all three taskees and the RPT
route cannot satisfy them (and the traces carry 0 RPT rows); each is satisfied 'via C2SIM-capture' (a PositionReport
after the capture's own first TASKCMPLT), AllSatisfied true. Suite section 9b uses one untruncated name in the init,
the app log and the trace, which is why it passes. So under E2 the gate's MAPPING is exercised and its "LAST of N TSK"
anchor is predicted NOT to appear (P30). This is recorded as a finding for the seat, not repaired here.

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: --pre-order-gate nav-area --pre-order-gate-timeout 900

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: the watchdog runs on the SIMULATION clock (--env Vrf__StallClock=sim), window 360 SIM s.
Armed ends in SIM s from dispatch: T01 300 and T13 300 (no destination - never watched); T02 300 and T14 300
(destination tasks: inside the 360 s window, but an unarrived destination task goes OVERDUE and stays watched and
nothing is SENT at that end, so the end cannot pre-empt or manufacture a stall verdict); T10 450. A stall verdict is
possible from dispatch + 360 SIM s onwards for T02 / T14 / T10 only. T14's chain HANDOFF at vertex 1 (sec 4 MEASURES)
is predicted to last well under the window; an intermediate vertex's completion resets nothing (ConsumeVertexChain
Completion returns before the watchdog reset), so a long handoff WOULD be judged. The falsifier's 60 SIM s stall is a
separate, finer measure on the WatchVrf trace (sec 4).

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels.

DEVIATION FROM RECORD: the successor-gate floor is 60 s, not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:320); the computed wait is max(60, 300 + 60) = 360 SIM s, so T02 and T14 are not held two hours behind a late predecessor for no reason.

DEVIATION FROM RECORD: consoles at level 4, not the demo setting of plan P7 ("--object-console 3 --member-console 3"); the house template requires level 4 for a movement run and the movers here are lone platforms.

DEVIATION FROM RECORD: a FALLBACK placement does not stop this run, where DEMO_RUNBOOK sec 0.4 says of anything but N of N from the terrain query "Stop, warm the area, start again"; E1 and -2 record that warming does not change that line on this AO, and the re-clamp that repairs it is scored in P5.

DEVIATION FROM RECORD: the window closes EARLY under -StopWhenComplete, where E1 registered "--no-stop-when-complete --run-secs 2700" (E1 sec 3 E); the reason is to run the runner's report-evidence gate (M1b) live for the first time (RUNBOOK sec 11 lists it NOT YET SEEN LIVE), and the 2700 s cap is kept so the run ends even if a task never reaches a terminal report.

EFFECT OF THAT DEVIATION ON THE WINDOW, stated before the run: the runner closes the observation window once all 3
taskees and all 5 tasks have a TERMINAL report, 60 s (-SettleHoldSecs) have passed since ALL-COMPLETE, and every
taskee has post-completion position evidence (RunC2SimScenario.ps1 :5308-5378, Test-EarlyExit). On E1's timeline the
last terminal report (T10, 1,322 SIM s = 184 WALL s after dispatch) came ~4 min after the order, so the E2 window is
expected to close ~5-6 min after the order instead of 45 min: the trace and the app log end ~1-2 min after T10's
TASKCMPLT. What that costs: anything after the close is unobservable (a late second terminal report for a uuid, a late
vendor completion, T10's members' last convergence); the pre-arrival measures (P6, P20, P20b, P20F, P21, P26, P28, P29)
are all inside the window by construction, because a mover's own TASKCMPLT precedes the close. A task that never
reaches a terminal report leaves the window at its 2700 s cap.

Flag notes (not deviations): `--no-gui` explicit (the runner's default); Vrf:PlatformMoveToPerVertex NOT passed (the
shipped default ON is under test); --model-set NOT passed (the runner's default EntityLevel); route shift, the
pre-flight and the tile cache location are the shipped settings (nothing passed).

## 2. What the code emits - log-line shapes (src at 0849334 [V])

E1 sec 2's shapes stand; M2 added lines and shifted the line numbers of VrfC2SimService.cs. At 0849334 [V]: L-DISP
(:5176 `DISPATCHED <name> task '<T>' (<kind>) at WALL <iso>Z, SIMULATION clock <s> s`; a chained platform's kind is
`move-to-per-vertex`); L-ROUTE (`Task '<T>': CreateRoute '<route>' (<N> pts) for <name>; move deferred to
route-created.`); L-MAR (:6026 `Route '<route>' (<uuid>) created; MoveAlongRoute issued for <vrf>.`); L-RATIO (:7212);
L-WDOG (:8066); L-REPORTS (:1083); L-JUDGES (:7595); ARRIVAL EVIDENCE (:7081); OVERDUE / late completion (:7471,
:8428); L-M1-ON (:773 -> VertexChain.cs :119-127); L-MODELSET (:778-780, UnitPositionPolicy.StartupLine); L-MOVETO
(:5397); L-VTX (:5438) `VERTEX CHAIN <name> task '<T>': vertex k of N COMPLETED - the unit is D m from it and moved M m
since <dispatch|vertex j>; issuing vertex k+1 (RL-20260927-01).`; L-VTX-ISSUE (:5561) `VERTEX CHAIN <name> task '<T>':
vertex k of N issued - MoveToLocation (<lat>,<lon>) (RL-20260927-01).`; L-VTX-LAST (:5445) `... LAST vertex k of N
COMPLETED - the unit is D m from it and moved M m since <dispatch|vertex j><suffix>. The chain ends ...`; L-VRFDONE
(:8294) `VRF task complete: <name> / <taskType> (success=<True|False>)`; the benign swallow (:8310) `VRF completion
for <name> after the arrival-evidence report of task <T> - swallowed.`. THE CHAIN FAILURE FAMILY (all PREDICTED
ABSENT): `VERTEX CHAIN` lines carrying VACUOUS (:5430, :5455), FAILED (:5465), SWALLOWED (:5473, :5483) or `is NOT
issued` (:5543, :5554); `is ALREADY driving this task's MOVE TO PER VERTEX chain` (:5347); `the MOVE TO PER VERTEX
chain of task ... ends at vertex` (:5149, :5777); `... is REPLACED by task` (:5374); `MOVE TO PER VERTEX chain(s)
FROZEN` (:7760); `THE VERTEX-CHAIN CONTINUATION THREW` (:5524).
M2 [V]:
- L-CACHE (start-up, :692) `ROUTE PRE-FLIGHT TILE CACHE: <dir> - <N> file(s), the SHIPPED FALLBACK because
  Vrf:PreflightCacheDir is empty ... Vrf:PreflightOffline=False. ...` - N counts the TOP-LEVEL (raster) files only
  (CountCacheFiles :6601, non-recursive); L-SCRUB (:726) `ROUTE PRE-FLIGHT TILE CACHE SCRUB: <N> cached tile file(s)
  checked, all carry a valid TIFF/PNG signature (SF3).`
- L-OSMSET (the pre-flight's FIRST USE, :6136 - lazily, at the first ground-move dispatch, not at start-up) `ROUTE
  PRE-FLIGHT MODEL SET EntityLevel (Vrf:ModelSet; RL-20260927-01) - EntityLevel: a leg is FLAGGED by the slope ratio OR
  by OSM water (osm-water Lake areas) within +/-25 m of the line; river LINES are not water here (...). OSM features
  read from <dir>\osm-water (<Wf> tile file(s), <We> of them 0 bytes = UNKNOWN) and \osm (<Af>, <Ae> 0 bytes); a
  missing or empty tile is UNKNOWN, never clear. VERTEX CHECK: an authored vertex in OSM water or within 10 m of an
  OSM building is moved to the nearest clear ground within 300 m and REPORTED ...`; its warning twins: `Vrf:ModelSet
  '<v>' is not EntityLevel or AggregateTacticalLevel` (:6131) and `cached OSM tile file(s) ... are not vector tiles`
  (:6147).
- L-VCHECK (:6721) `Task '<T>' (<name>): VERTEX CHECK (EntityLevel) - <N> authored vertex(es) checked against OSM water
  and buildings: <M> moved, <K> kept on bad ground, <U> unverified, the rest clear.`; its per-vertex WARNINGs
  `VERTEX MOVED` (:6693), `VERTEX NOT MOVED` (:6701), `VERTEX UNVERIFIED` (:6710).
- L-OSMWATER (:6316) `ROUTE PRE-FLIGHT task '<T>' (<name>) leg <k>: OSM WATER WITHIN 25 m OF THE LINE - ...`;
  L-OSMUNK (:6328) `... leg <k>: <n> OSM tile(s) under this leg could NOT be read [...]`; L-BRIDGE (:6342).
- The shift family (:6792 `ROUTE SHIFT check queued for <name> (<n> vertices)`, :6837 `ROUTE SHIFTED`, :6853 `NO ROUTE
  SHIFT - ... RIVER CROSSING ...`, :6858 `NO ROUTE SHIFT - <note>`, :6884 `PRE-DISPATCH applied`, L-NOFLAG :6891 `Task
  '<T>' (<name>): ROUTE SHIFT - no leg flagged; the route is unchanged.`); the land-cover `WATER ON THE LINE` (:6284)
  and `ELEVATION LEVEL ACTUALLY USED` (:6232).
- L-CENSUS-TILE (:6476) `ROUTE PRE-FLIGHT TILE CENSUS, batch <b> of this order (E6/N8): <H> cache HIT(s), <F> HTTP
  FETCH(es) since the previous census line. ...`; L-TILETOTAL (:6520, at shutdown) `ROUTE PRE-FLIGHT TILE TOTAL for
  this RUN (...): <H> cache HIT(s), <F> HTTP FETCH(es), <X> tile(s) given up on after 3 attempts, <U> undecodable
  body(ies). ...`.
Vendor console lines on the object's own console at level 4 (`VRF console [n] <name> (VRF_UUID:...): ...`), as E1:
L-PROOF `C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true`; L-PLAN `Planned path has <N> points.`
(and the feature planner's `Planned path has <N> parts.`); `Node Is destination in nav area?: <success|failure>`.

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run unless marked PREP)

All from Git Bash at the MAIN checkout F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM (HEAD 53215e9
when this was written). The order is the main checkout's own data/IRONSTORM_CUTA_Order.xml, already byte-identical to
the registered one (7a986137 [V]) - no merge is needed for it. The runner writes its appNumber block into the MAIN
checkout's working-tree OPUS_EXECUTION_PLAN.md; that block is carried back to this branch afterwards (the E1 procedure).
Scripts (scratch u3\laneE2): golive_dryrun.sh, golive_run.sh (the lines below, verbatim), prep_dryrun.sh (PREP).

A0. PRECONDITIONS: (1) the seat's go-live; (2) the offline design lane C1 has REPORTED and no other lane is building,
    running a suite or an agent - the quiet period cannot start before; (3) the main checkout's
    data/IRONSTORM_CUTA_Order.xml sha256 7a986137...; (4) the deployed build is still 53215e9 (exe d12ca223..., dll
    37e2adee...) and `git diff --stat 0849334..main -- src` is EMPTY - if main's src has moved on, NOTHING is rebuilt:
    the registered build is what runs and the difference is recorded; if the DEPLOYED hashes differ (another lane
    rebuilt the main checkout's output folders), STOP before E and the seat decides; (5) the DEPLOYED preflight-cache
    manifest still hashes df8ede71... (479 files) - a changed cache is a changed input: STOP before E; (6) the
    persistent holder RtiProbe 45600 (appNo 5170, started 18:20:45Z with a 28,800 s hold -> resigns ~02:20:45Z
    2026-09-28) is ALIVE with at least 60 min of hold left - if it is gone or nearly done, use D-NEW below.
A.  Read-only checks, re-run immediately before E: DOTNET_ENVIRONMENT empty; 0 Vrf__ variables; processes - rtiexec
    47980, rtiForwarder 50740, rtiAssistant 30240 and RtiProbe 45600 up and never touched; no vrfNavGenerator, build,
    vrfSim, vrfGui, VrfC2SimApp, WatchVrf or ListenReports; REST 200 on 18080 [PREP V 20:29Z]; exe / dll / appsettings
    / bridge / fixture / order / init hashes as registered; `derive_ironstorm_cuta.py --check` exit 0 with three CHECK
    lines [PREP V]; the maple area as at registration [PREP V 20:29Z: C:\C2SIM\vrf-nav\navData\MAK Earth (online)\
    NavArea-ground-platform IRONSTORM-CENTRE_maple, 4,764 files, 287,120,328 B, newest 2026-09-26T23:05:00Z; its
    .navRuntimeConfig sha256 40b46032631b5f22229945c5a1e008cf5ef4d4ce131728b9016fbd99fb8cddfb]; the cache manifest
    (A0 (5)); the marker read (5185 at registration).
B.  No build (done in the preparation, above).
C.  Order validation (no federate, no appNumber): C1 `--parse-order` and C2 `--parse-init` [PREP V, sec 1(g)]; C3 ONE
    real push to the PRIVATE server, at go-live, after this registration is committed:
        tools/PushInit/bin/Release/net10.0/PushInit.exe data/IRONSTORM_CUTA_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
        tools/PushOrder/bin/Release/net10.0/PushOrder.exe data/IRONSTORM_CUTA_Order.xml 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
    EXPECT both exit 0 and one `ORDER (<n> chars)` echo carrying 5 tasks, 5 Durations and the verbs
    CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT. Any failure = STOP.
D.  THE HOLDER: REUSE the persistent holder RtiProbe 45600 (appNo 5170, E1's). No new holder, no new claim. The PREP
    dry run recognised it: "RtiProbe pid=45600 started=2026-09-27T18:20:45.000Z is ALREADY RUNNING - a PERSISTENT
    FEDERATION HOLDER from outside this run" [V]. D-NEW (only if A0 (6) fails): E1 sec 3 D verbatim with four fresh
    numbers M..M+3 from the marker, hand-claimed in Appendix B and committed on this branch BEFORE the holder joins,
    -WhatIf first; exit 1 or 2 = STOP, no blind relaunch (RUNBOOK 9c); sec 5 then shifts by four.
E.  DRY RUN, then THE RUN once, stdout to a FILE, never piped:

        scripts/RunScenario.sh \
          --scenario IronStorm_Centre_52_Nav_AG_maple \
          --init data/IRONSTORM_CUTA_Initialization.xml \
          --order data/IRONSTORM_CUTA_Order.xml \
          --client-id "Not Set" \
          --duration-scale 0.25 \
          --pre-order-gate nav-area --pre-order-gate-timeout 900 \
          --object-console 4 --member-console 4 \
          --stop-when-complete --run-secs 2700 \
          --env Vrf__StallDetection=true \
          --env Vrf__StallClock=sim \
          --env Vrf__TaskPredecessorTimeoutSeconds=60 \
          --sample-threads \
          --no-gui \
          --log runs/launch52/RunScenario-ironstorm-e2-<stamp>.log

    The dry run is the same line plus --dry-run and its own log name. PREP DRY RUN [V 2026-09-27T20:17:54Z, exit 0,
    runs/launch52/RunScenario-ironstorm-e2-prep-dryrun-20260927T201754Z.log]: order data/IRONSTORM_CUTA_Order.xml
    accepted; "deployed VrfC2SimApp BUILD IDENTITY: git 53215e9, written 2026-09-27T20:06:59Z"; "model set :
    EntityLevel <- default; exported to the app as Vrf__ModelSet"; the type map declares EntityLevel and the fixture
    loads EntityLevel ("matches -ModelSet"); route shift PREDICTED ON; RtiProbe 45600 a PERSISTENT FEDERATION HOLDER;
    block 5185-5195, "marker would advance to: 5196"; "window : 2700s CAP; -StopWhenComplete closes it once all 3
    taskee(s) and all 5 task(s) have a TERMINAL report ..., 60s have passed AND every taskee has post-completion
    position evidence"; nothing launched, marker unmoved (5185), the main checkout's git status unchanged. THE GO-LIVE
    DRY RUN must show: the persistent holder recognised; build identity 53215e9; model set EntityLevel; the block of
    sec 5 exactly; the window line above. A different layout is written into sec 5 BEFORE launch.
    Defaults kept: --profile 5.2; REST/STOMP 18080/61614, the PRIVATE server only; route shift ON; Move To per vertex
    ON; the runner's Stage 2h holder (it JOINS the federation the persistent holder keeps). GATE on teardown:
    StopVrf52's exit 0 or 6 (P18); 3, 5 or 7 = STOP.
F.  Post-run, in the FOREGROUND: the post-run inventory (Win32_Process: only rtiexec, rtiForwarder, rtiAssistant, the
    persistent holder and, inside its 900 s hold, the Stage 2h holder; no vrfSim / VrfC2SimApp / WatchVrf /
    ListenReports) ends the quiet period; the hashes and the cache manifest are re-read; then the harvest (sec 6).

QUIET PERIOD (RUNBOOK 0.5.14 item 5): from the launch of E to the post-run inventory (F): no Stop-Process / taskkill of
any kind (StopVrf52's own identity-gated force of the run's own back end is allowed), no build, no suite, no subagent,
no second runner - in this lane or any other. The executor polls the run's own files from the FOREGROUND with bounded
waits and does not end its turn while the run is open. Never touched: rtiexec, rtiForwarder, rtiAssistant, any
RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES. As E1 sec 4 [A], per TASK UUID, from the scored run's vrfc2simapp.log in line order, cross-checked against
reports-captured.log; displacement, ARRIVAL (the closest POS fix after dispatch within 100 m of the destination) and THE
STALL MEASURE (P20F: after the platform's first fix > 5 m from its dispatch fix and before its first fix within 100 m of
its destination, a maximal run of consecutive fixes all within 5 m of the run's first fix; SIM length from the trace's
own CON rows, else the L-RATIO chain, flagged; 30-90 SIM s = BORDERLINE; "no OSM feature within ~10 m ahead" and "a
vehicle within 25 m ahead" exactly as E1's AMENDMENT) on watchvrf-trace.csv. NEW FOR E2: VERTEX 1 REACH - the closest
trace fix to the (i) waypoint and the app's own L-VTX distance D; THE TSK ROWS - the trace's `TSK,<t>,"<marking>",
"<type>"` records per marking (48 IBCT's is '48_IBCT/28'), one per vendor task completion; THE HANDOFF - the stationary
run (fixes within 5 m of its first) that contains the fix nearest 48 IBCT's FIRST TSK row, when that fix is within 100 m
of the (i) waypoint, with its SIM length. THE HANDOFF IS NOT EXEMPT FROM P20F: a handoff of >= 60 SIM s with nothing
ahead is the falsifier's condition like any other stall. PATH SHAPE per leg of the (i) route (lateral offsets, right of
travel +), the side the track passes the -2 stop on and its closest approach to the hamlet's footprints. THE SCORER is
scratch u3\laneE2\e2_score.py <runDir> - E1's e1_score.py copied, its OSM source switched to this run's staged cache
(identical to the deployed one; no 0-byte tile), plus the E2 measures. ITS CONTROLS, run before this registration
[V]: on run -2 (the SAME order on Move Along Route) it finds 48 IBCT's stop AT a feature (32,172 SIM s at 54.030774,
23.327064; a building 5.0 m ahead, 4.4 m at any bearing - E1's control reading reproduced), NOT arrived (1,077 m), the
(i) waypoint passed at 25.8 m (trace t 89.7, 129.4 SIM s after dispatch), 0 TSK rows for '48_IBCT/28', the stop 1,815 m
along leg 2 and 9.9 m right (east) of the line, 3.2 m from a building; 0 wet fixes, 0 on unknown tiles. On E1's run it
finds 48 IBCT arrived 6.9 m / 257.6 SIM s after dispatch with 0 stationary runs (E1's P20 / P20F reproduced), 1 TSK row
'48_IBCT/28' move-to, never within 1.29 km of the (i) waypoint, and it REFUSES to call that TSK a vertex-1 handoff
("2904.5 m from the (i) waypoint - NOT a vertex-1 completion there") - the guard added after its first control pass
measured E1's final rest as a "handoff". 28ID arrived 0.9 m (E1) / 1.0 m (-2) in both.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) The persistent holder 45600 alive at launch and recognised PERSISTENT by the runner; the back end JOINS (rtiexec count-grep: no create for its appNo); "READY - joined the federation". (ii) L-CENSUS reads 4 EMPTY shells and 32 platforms (36 = C2's in-scope count). (iii) L-WDOG once, naming the SIMULATION clock; L-CLOCK names SIMULATION and "Vrf:DurationScale=0.25"; the route-shift start-up line once. (iv) The runner's stage 7d gate FIRED (no NOT-READY, no exit 3). (v) Exe / dll / bridge hashes and the cache manifest as registered, before and after. (READY TO TASK is RECORDED.) | HIGH | Any limb = VOID + STOP (launch, terrain or harness failure - no code verdict). | |
| P1 | PLACEMENT ALTITUDE SOURCE, recorded: expected "0 of 36 ... TERRAIN QUERY, 36 from the FALLBACK" and one PLACEMENT RE-CLAMP summary line. | RECORDED | - | |
| P2 | AREA: the FIRST L-AREA row names `NavArea-ground-platform IRONSTORM-CENTRE_maple` and precedes L-ORDER; zero L-AREA rows name any other area; runner "NAV AREA ACQUIRED ... IRONSTORM-CENTRE_maple". WARM/COLD and the gate wait RECORDED. | HIGH | No row before L-ORDER, a gate timeout, or another named area = STOP. | |
| P3 | THE PLANNER RUNS ON THE LONE PLATFORMS: >= 1 L-PROOF line on 48 IBCT's OWN console and >= 1 on 28ID's; >= 1 for a 1-112 IN member. The count on 48 IBCT's console RECORDED (expected 2: one ground-vehicle-move-to per vertex). | HIGH | Zero on either lone platform's console = STOP. | |
| P3b | PLANNING EVIDENCE: >= 2 `Planned path has <N> points.` (N >= 2) on 48 IBCT's console (one per vertex) and >= 1 on 28ID's; every `Node Is destination in nav area?` result RECORDED per Move To. | MEDIUM | Fewer = recorded MEDIUM miss. | |
| P4 | DISPATCH STRUCTURE: exactly one SENT TASKSTRT per uuid (5); T02's L-DISP after T01's SENT TASKCMPLT and T14's after T13's; one "Task 'T10...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25) - it will not dispatch before then." line and T10's L-DISP after it; ZERO "SKIPPED: predecessor" lines. | HIGH | A missing or duplicate TASKSTRT; a successor before its predecessor's TASKCMPLT; any predecessor SKIP = STOP. | |
| P5 | RE-CLAMP: an "is ON the terrain" gate line for each of T02, T14, T10; ZERO "measured OFF the terrain" gate lines; ZERO retired lines (`NOT DISPATCHED YET`, `HELD as`, `REFUSED [`, `BOUND-BUT-NOT-ON-THE-GROUND`). A mover whose terrain profile fell back to Live has no gate line: RECORDED NOT MEASURED. | HIGH | An OFF gate line or a retired line; a mover with a profile reply and no gate line = STOP. | |
| P6 | T02's 28ID and T14's 48 IBCT are each displaced MORE THAN 50 m after dispatch (trace, displacement since dispatch). | HIGH | Either at or under 50 m = STOP. | |
| P6b | T10: at least one 1-112 IN member displaced more than 50 m after T10's L-DISP. | HIGH | None = STOP. | |
| P6c | Per mover: L-PLAN lines, max speed, net displacement, any mesh "not enough (0) points" lines. | RECORDED | - | |
| P7 | CNFPSL HOLDS T01 and T13: each one L-CNFPSL + one IN PLACE line; one STP-866 Observation each in the capture; "it has no destination" armed at 300; NO L-ROUTE, L-MOVETO or MoveToLocation; exactly one SENT TASKCMPLT "(300 s after dispatch)". | HIGH | A move issued for a hold; zero or two TASKCMPLTs; a missing L-CNFPSL; an OVERDUE for a hold = STOP. | |
| P8 | T02 PLATFORM PATH: L-R3SELF for T02 on every dispatch pass and on no other task; ZERO L-FAW, ZERO `FireAtTarget`, ZERO `deferred until the move COMPLETES` lines. | HIGH | Any fire-at-will / FireAt / deferred-engage line; no L-R3SELF for T02; a count unequal to T02's pass count = STOP. | |
| P9 | T14 FOLSPT: exactly one DISTINCT L-FOLSPT naming 'ROEHold'; exactly one L-MOVETO for T14; NO L-ROUTE for T14; an L-ARM-D (destination) at 300. | HIGH | Missing, another ROE, an L-ROUTE for T14, two L-MOVETO for T14, or no move = STOP. | |
| P10 | T10 CRESRV: L-BARE for T10; an L-ROUTE "(4 pts)" for T10 (4 + 4 per ROUTE SHIFTED line for T10 - a shift splices four points, RouteShift.cs :642 - predicted none) followed by its L-MAR; an L-ARM-D at 450; no IN PLACE line for T10. | HIGH | Any limb failing = STOP. | |
| P11 | COMPLETION PER THE TIME RULES (RL-20260921-09): each mover (T02, T14, T10) takes EXACTLY ONE path - (a) early; (b) late: one L-OVERDUE, nothing SENT at the end, then arrival and one TASKCMPLT on arrival; (c) not arrived by window end; (d) stalled: L-STALL + one TASKABRT "STALLED". On a chained platform the arrival is the app's arrival evidence or its LAST vertex's completion, whichever comes first - never an intermediate vertex's. | HIGH | Two paths, a TASKCMPLT before start + Duration, a TASKCMPLT at an end time for an unarrived mover, or any completion taken from T14's vertex 1 = STOP. | |
| P11b | WHICH PATH: T14 late (b) - arrival evidence fires within min(500, 0.25 x 4,169) = 500 m of the destination (ArrivalPolicy), i.e. after >= 3,669 m of travel incl. the vertex-1 stop: at E1's 9-10 m/s ~370-410 SIM s, past its 300 s end; early (a) would need >= 12.2 m/s sustained (max-speed 15, E1's observed max 11.3). T02 late (b) as E1. T10 late (b) as E1. | MEDIUM | Another path for any = recorded MEDIUM miss. | |
| P12 | REPORT HYGIENE: ZERO suppressed terminal reports; at most one terminal report per uuid; ZERO `task=(none)` SENT lines; capture and log agree; "Reports this run: ... 0 FAILED". | HIGH | Any of these = STOP. | |
| P13 | STALLS / FALLBACKS: every L-STALL, OVERDUE and arrival RECORDED with its displacements. | RECORDED | - | |
| P14 | NO ENGAGEMENT: zero L-ENGAGE / FireAt lines and zero vendor-console "Fire Weapon" lines. | HIGH | Any = STOP. | |
| P16 | CLOCK: every L-RATIO line; each L-DISP's SIMULATION stamp; the sim/wall band. | RECORDED | - | |
| P17 | RUN HEALTH: no `BACK END LOST`; no WS-runaway exit 6 (runner); no new .dmp / .callstack.log for this back-end pid (names only); VrfC2SimApp exits 0; rtiexec, rtiForwarder, rtiAssistant and every holder untouched. | HIGH | Any limb = VOID. | |
| P18 | TEARDOWN on StopVrf52's exit code inside the runner: 0 or 6, and no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left; the post-force wait line and the runner's post-check RECORDED. | HIGH | Exit 3, 5 or 7 = STOP. | |
| P19 | T10 RESOLVED ROUTE (as E1): T10's DISTINCT L-RESOLVE lines are exactly E1 P19's (c8d9cd1a and 51a59f89 each "1 vertex(es) dropped"; c8d9cd1a joined with no suffix; 51a59f89 "joined <d> m after the previous graphic's end", d in 795-815; cc23071f -> PassagePoint_48_IBCT_SLOT0 appended as the DESTINATION), each recurring once per dispatch pass; zero resolver WARNINGs for T10; L-ROUTE "(4 pts)". | HIGH | Any other distinct line set, a warning, or another point count = STOP. | |
| P19b | T14 RESOLVED ROUTE: T14's DISTINCT resolver lines are exactly the four of sec 1(g) - 7ff48b93 and 7351f662 each "1 vertex(es) dropped"; "path from MapGraphicID 7ff48b93-... (line, 1 vertices): 1 vertex(es) joined." with NO gap suffix; "path from MapGraphicID 7351f662-... (line, 1 vertices): 1 vertex(es) joined <d> m after the previous graphic's end." with d in 2890-2915 - each recurring once per dispatch pass (the count of passes RECORDED); zero resolver warnings for T14; the ROUTE SHIFT queue line "(3 vertices)". | HIGH | Another graphic, another drop / join, another destination, a warning, or another vertex count = STOP: the run would not test the registered route. | |
| P20 | HEADLINE: T14's 48 IBCT reaches within 100 m of its destination 54.040348, 23.324206 (closest trace fix after dispatch) by window end - via the (i) waypoint and past the -2 hamlet stop on leg 2. | HIGH | Not within 100 m = STOP. NAMED OUTCOMES, each recorded with the stop point, the STALL measure and the object's last console lines: (1) the P20F condition; (1b) a TRAFFIC STALL (a vehicle within 25 m ahead); (2) a stall AT an OSM feature - above all the -2 outcome recurring, a stop among the hamlet's buildings; (3) a chain FAILED line (PathPlanFailure) and its TASKABRT; (4) still moving at window end; (5) stuck at the (i) waypoint with vertex 2 issued. | |
| P20b | T02's 28ID reaches within 100 m of PassagePoint_28ID_SLOT0 54.028874, 23.264401 by window end. | HIGH | Not within 100 m = STOP, with the same named outcomes. | |
| P20F | THE FALSIFIER (E1 P20F as AMENDED): NO stall of T14 of >= 60 SIM s with no OSM feature within ~10 m ahead AND no vehicle within 25 m ahead - the vertex-1 handoff included. The same measure RECORDED for T02. | HIGH | The condition met = the frame of RL-20260927-01 is wrong for this vehicle and ground: STOP and ask; no re-run under this registration. | |
| P21 | NO WATER ON ANY TRACK: no POS fix of any object that moved > 50 m falls in an OSM water cell (the z14 osm-water raster of this run's staged cache, leg_check.load_osm_water); a fix on an absent tile counts UNKNOWN. | MEDIUM | >= 1 wet fix or any fix on an absent tile = recorded MEDIUM miss. | |
| P22 | M1 AND D1 ARE THE BUILD: L-M1-ON exactly once, naming "100 m (Vrf:VertexArrivalRadiusMeters)"; ZERO `MOVE TO PER VERTEX off (` lines; L-MODELSET exactly once at INFO, naming `Vrf:ModelSet='EntityLevel'`. | HIGH | Missing, duplicated, the off line, or a MODEL SET WARNING / another model set = STOP. | |
| P22b | NO UNJUDGED MOVER: ZERO L-JUDGES lines. | MEDIUM | Any = recorded MEDIUM miss. | |
| P23 | THE FORM PER MOVER: T14 exactly one L-MOVETO "MOVE TO PER VERTEX for 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE (VRF_UUID:<u>) - vertex 1 of 2: MoveToLocation (54.014600,23.331500); the other 1 vertex(es) are issued one at a time ..."; T02 exactly one "... 28ID__FRIENDLY_INFANTRY_DIVISION ... - vertex 1 of 1: MoveToLocation (54.028874,23.264401); the other 0 vertex(es) ..."; ZERO L-ROUTE and ZERO L-MAR for T02 or T14; T10 exactly one L-ROUTE and one L-MAR and ZERO L-MOVETO naming 1-112 IN. | HIGH | Any other form, vertex count or first vertex = STOP. | |
| P24 | VENDOR COMPLETIONS: for 48 IBCT exactly TWO L-VRFDONE "VRF task complete: 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE / move-to (success=True)", the first before its L-VTX-ISSUE and the second after it; ZERO of another type or success=False; the trace carries two '48_IBCT/28' "move-to" TSK rows in the same order. For 28ID exactly one "move-to (success=True)" if its track comes within 15 m of its destination (as E1). T10: one "... / move-along (success=True)" if it arrives. A platform ending 15-100 m short is RECORDED. | HIGH | For 48 IBCT: not exactly two, another type, success=False, or the second missing while its track came within 15 m of the destination = STOP. | |
| P25 | THE CHAIN LINES, in this order and exactly once each: L-VTX "VERTEX CHAIN 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE task 'T14_...': vertex 1 of 2 COMPLETED - the unit is D1 m from it and moved M1 m since dispatch; issuing vertex 2 (RL-20260927-01)." with D1 < 100; L-VTX-ISSUE "... vertex 2 of 2 issued - MoveToLocation (54.040348,23.324206) (RL-20260927-01)."; L-VTX-LAST "... LAST vertex 2 of 2 COMPLETED - the unit is D2 m from it and moved M2 m since vertex 1." with D2 < 100 and NO VACUOUS suffix. For 28ID: one L-VTX-LAST "LAST vertex 1 of 1 COMPLETED ... since dispatch", D < 100 (as E1). ZERO lines of the chain failure family (sec 2). The benign lower-case "- swallowed." lines RECORDED. M1 and M2 values RECORDED. | HIGH | A missing, duplicated or out-of-order chain line, a D >= 100, a VACUOUS suffix, or any failure-family line = STOP. | |
| P26 | M2's PRE-DISPATCH STAGE, predicted by the harness (sec 1(k)) on the deployed cache: (a) at start-up one L-CACHE "... preflight-cache - 29 file(s), the SHIPPED FALLBACK ... Vrf:PreflightOffline=False" and one L-SCRUB "29 cached tile file(s) checked, all carry a valid TIFF/PNG signature"; (b) exactly one L-OSMSET, at the pre-flight's first use, naming EntityLevel and "osm-water (225 tile file(s), 0 of them 0 bytes = UNKNOWN) and \osm (225, 0 0 bytes)", with ZERO model-set and not-vector-tile warnings; (c) per mover exactly one DISTINCT L-VCHECK: T14 "2 authored vertex(es) checked ...: 0 moved, 0 kept on bad ground, 0 unverified, the rest clear.", T02 "1 ... 0 moved, 0 kept on bad ground, 0 unverified", T10 "3 ... 0 moved, 0 kept on bad ground, 0 unverified"; ZERO VERTEX MOVED / NOT MOVED / UNVERIFIED lines; (d) per mover one DISTINCT L-NOFLAG "ROUTE SHIFT - no leg flagged; the route is unchanged."; ZERO L-OSMWATER, L-OSMUNK, L-BRIDGE, `ROUTE SHIFTED`, `NO ROUTE SHIFT` (incl. RIVER CROSSING), `PRE-DISPATCH applied` and land-cover `WATER ON THE LINE` lines for T02 / T10 / T14; (e) ELEVATION LEVEL ACTUALLY USED L12 on every leg (T14 x2, T02 x1, T10 x3); (f) ZERO pre-flight ObservationReports: no captured ObservationReport whose Marking begins "ROUTE PRE-FLIGHT", "ROUTE SHIFT", "VERTEX MOVED" or "VERTEX NOT MOVED" (the capture's total RECORDED against E1's 39 = 37 init-time type-map proxy observations + the two STP-866 hold observations, all before the movers' dispatch [V: E1's reports-captured.log]). The harness's numbers behind it: T14 leg 1 slope ratio 0.140, nearest OSM water 60.1 m (197345448), leg 2 0.060, 62.2 m (505603429); T02 0.105, nearest water 141.3 m (a river polygon); T10 0.114 / 0.011 / 0.079, 109.0 / 156.7 m / none; no vertex within 25 m of OSM water or 10 m of a building. | HIGH | Any limb (a moved or unverified vertex, a flagged leg, an OSM finding, a report, another count) = STOP: the stage did not do on these routes what its own code does offline on the same tiles. | |
| P26b | TILE READS: every L-CENSUS-TILE line and L-TILETOTAL say 0 HTTP FETCH(es), 0 given up, 0 undecodable; the deployed cache manifest is unchanged after the run (df8ede71...). Cache HITS RECORDED (harness, sequential: 38; concurrent workers may read a tile twice). | HIGH | A fetch, a changed manifest, an exhausted or undecodable tile = STOP (the registered input changed under the run). | |
| P27 | RECORDED (the seat's list): per Move To (T14 x2, T02 x1) the destination altitude the console reports against the terrain-profile reply for that vertex (E1: +10 m, TerrainClearanceMeters) and its `Node Is destination in nav area?` result - E1 saw T14's destination judged OUTSIDE the nav area; vertex 2 is that same point at the same +10 m, so the same verdict is EXPECTED (MEDIUM), vertex 1 has no expectation; the planner's branch per Move To; PATH SHAPE per leg; the side of the -2 stop the track passes and its closest approach to the hamlet's footprints; the teardown code; the literal type string of every L-VRFDONE. | RECORDED | - | |
| P28 | VERTEX 1 IS REACHED: the L-VTX distance D1 < 100 m (Vrf:VertexArrivalRadiusMeters) AND the closest trace fix to the (i) waypoint is < 100 m, before T14's first fix within 100 m of its destination. | HIGH | Either >= 100 m, or the destination reached first = STOP. | |
| P29 | THE HANDOFF at vertex 1 (the stop the vendor's move-to leaves at an intermediate vertex, RUNBOOK sec 11 NOT YET SEEN LIVE): the stationary run around 48 IBCT's first TSK row lasts < 60 SIM s; its SIM length, the gap between the two TSK rows and the plan lines between them RECORDED. | MEDIUM | >= 60 SIM s = recorded MEDIUM miss here, and P20F then decides whether it is the falsifier. | |
| P30 | THE REPORT-EVIDENCE GATE (-StopWhenComplete, M1b), per sec 1(l)'s replay: the runner's per-taskee evidence (manifest earlyExit.reportEvidence) names 48 IBCT's and 28ID's VRF uuid from their MOVE TO PER VERTEX lines and 1-112 IN's from its route join; the anchor is EMPTY for all three (the trace's TSK rows carry truncated markings) - "LAST of 2" does NOT appear; each satisfied 'via C2SIM-capture'; the window closes EARLY ("closing the observation window EARLY ... of the 2700s cap") after the settle hold. | MEDIUM | Any limb = recorded MEDIUM miss (the gate is harness, not the frame); a "LAST of 2 TSK" anchor would mean the marking resolution differs from the replay - recorded, the reason looked for. | |

(P15 is retired into P26: E1's single route-shift row is now M2's stage.)

WHY P20, P20F, P24, P25, P26 AND P28 ARE HIGH: P20 / P20F are the frame's own predictions (FINDING_GROUND_MOVEMENT_
PRACTICE secs 0 and 5.2, UG52 23.1-23.2.2), E1 held them on a 1-vertex chain, and leg 2 of this route is the ground
that stopped the literal executor in -2; P24 / P25 / P28 are VertexChainTracker's deterministic sequence, pinned
offline by the RL-20260927-01 section of --rulings-selftest (431 PASS); P26 is the production code's own output on the
deployed tiles (the harness). WHY P3b, P11b, P21, P22b, P29 AND P30 ARE MEDIUM: plan-line wording depends on which
planner answers; the late path rests on arithmetic E1 already missed once; a shore-hugging track can read wet on a
~5.6 m raster; D1's warning has one live run; the handoff's length has never been measured and the trace samples every
~2 WALL s (15-30 SIM s); the gate is harness behaviour replayed on one run's files.

STOP RULES (as E1):
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P17 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- P20F met is reported as the frame's falsifier: STOP and ask, before anything else is run.
- The executor never intervenes in the window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as IRONSTORM_CUTA_E2-<date>-2, with new appNumbers.

ONE VARIABLE: for the headline mover T14, the TASK a lone ground platform gets - Move To per vertex (RL-20260927-01)
instead of Move Along Route - on the SAME route, the ruled (i) route of the SAME order; the CONTROL is run -2
(20260927T021020Z: 48 IBCT on Move Along Route passed the (i) waypoint and stopped dead among the hamlet's buildings
mid-leg 2). Other differences from that control, named so that no claim rests on them: the build (80f707f against
53215e9 - M1, D1/M1b, A1 and M2; M2's stage is predicted to change no route here, P26), T02's task (Move To), the
window (-StopWhenComplete with a 2700 s cap, against -2's fixed "--no-stop-when-complete --run-secs 2700" [V: its
registration's step E]), the persistent holder, StopVrf52's post-force wait. Against E1 (20260927T182140Z, the same build family without M2), T14 differs in its ROUTE ((i)
included) and M2 is present, so E1 is a reference, not the control.

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5185 ***` at 53215e9 [V]. NOTHING is claimed by this registration (the
preparation phase's hard stop). At the go-live, from the marker M read then (5185 unless another run has moved it),
with the persistent holder 45600 REUSED (no holder numbers), the runner's SCORED block is written by the runner at its
Stage 2 (the PREP dry run's layout [V]): M back end (5185), M+1 front end (5186, BURNED, --no-gui), M+2 WatchVrf
pre-check (5187), M+3 WatchVrf trace (5188), M+4 VrfC2SimApp (5189), M+5 RtiProbe 2c (5190), M+6 CreateOne (5191,
BURNED unless the oracle gate fails), M+7 to M+10 Stage 2h holder attempts (5192 JOINS; 5193-5195 BURNED); marker ->
M+11 (5196). Under D-NEW the holder takes M..M+3 first and the block starts at M+4. A launch that aborts burns its whole
block. PushInit / PushOrder / ListenReports / StopIface are C2SIM clients, not federates.

## 6. Harvest (after the run, read-only) and where results go

From the run directory (runs\launch52\last-run-dir.txt): vrfc2simapp.log, reports-captured.log, c2sim-bus.log, the
manifest (earlyExit.reportEvidence for P30), watchvrf-trace.csv, thread-samples.csv, holder logs, stopvrf logs, the
wrapper log; scratch u3\laneE2\e2_score.py <runDir> (sec 4, controls above); tools/analysis/applog_chain.py
(`--selftest`) for the chain lines; the harness's P26 lines against the app log's; the cache manifest re-hashed.
Vendor sim logs dump the environment in cleartext: count-grep only, never quoted. Results go to: the Result block below
(measurement and implication in separate sentences); PLAN_MOVEMENT_2026-09-27.md (an E2 row, the seat's to place); the
RUNBOOK sec 11 M1 bullet's "NOT YET SEEN LIVE" items E2 settles; Appendix B annotated from the manifest. ASCII + CRLF.

## 7. What this run does NOT claim

- M2's vertex nudge, its water detour, its river report or its bridge rule working live: on these routes none of them
  is predicted to fire (P26); they stay offline-proven (--osm-selftest 139 PASS, real tiles 143 PASS).
- That the continuation generalises past one intermediate vertex, one vehicle, one waypoint (n = 1).
- That the planner clears hamlets in general, or that per-vertex Move To beats one Move To to the destination.
- That M1b's last-completion anchor works on live markings (sec 1(l) predicts it does not engage).
- Anything new about units (T10's path is E1's), the aggregate profile (G1), the full 23-task order, the demo server,
  the GUI or the demo profile; no timing generalisation (n = 1, one host, one fixture, a load-dependent clock).

## Result (written after the harvest, never from a live read)

(pending - the run has not been launched)
