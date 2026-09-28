# PREREG - IRON STORM ON THE AGGREGATE PROFILE, RUN G1-3: G1-2 re-run on the M3 build - a tasked container moves by the vendor's PLANNING task per STP vertex, AUTO per leg (RL-20260928-03) (one unscored pre-warm + one scored run)

STATUS: STOPPED AT THE W GATE 2026-09-28 - NO SCORED RUN (see Result: the pre-warm 20260928T190047Z_run scored "W GATE:
FAIL (branch A; M10, M12, W-DEST)"; on T10 the planner worked and the executor did not - the vendor's move-along refused
the planned route, "route does not exist"; the back end crashed 2.9 s after the first planned dispatch; E not launched,
block 5270-5280 never claimed). Registered by lane G1-3 (session 5fc25950; the registration
commit's own time is the stamp) on branch run/ironstorm-agg-g1-3 from main abe7cdf (= fde3ff2 + the deploy lane R4's
record, RUNBOOK secs 9 and 11o only: `git diff --stat fde3ff2 abe7cdf` is docs/RUNBOOK.md [V]; the build's source is
fde3ff2), BEFORE any order push, holder action or launch. PREPARATION ONLY: no C2SIM push, no holder start, no VR-Forces
or runner launch, no appNumber claimed, no C:\MAK write, no build (the deployed build is lane R4's). Marks: [V] = checked
while writing this file; [A] = taken from the record or another lane's output, not re-checked here. This file is G1-2's registration
(PREREG_IRONSTORM_AGG_G1-2_2026-09-28.md secs 0-7) with ONE intended variable - the container's move task - and the
changes the G1-3 brief names; every src line number cited is re-verified at fde3ff2 [V: a 699552c -> fde3ff2 line map over
`git diff -U0` (scratch u3\laneG1-3\linemap.py), each mapped line read at its new number].

## Registration

PREREG ID: IRONSTORM_AGG_G1-2026-09-28-3
DATE (UTC): 2026-09-28, before any launch (the registration commit's own timestamp is authoritative; the launch waits
for the seat's go-live; the G1 holder 33476 resigned cleanly at 18:14:59Z [A: lane R4], at the end of its 28800 s hold)
BINARY / COMMIT: the DEPLOYED main-checkout build 1.0.0+git.fde3ff2.Release-5.2 (not DIRTY), rebuilt IN PLACE by lane R4
from main fde3ff2 at 17:30:29-17:30:46Z - managed code only, the ten managed consumers; no native change, so the bridge
pin 03226dd0 stands (RUNBOOK sec 9, POST-PIN MANAGED REBUILD 2026-09-28 17:30Z, on main at cc0b2a7 / 18f6c57 [V]).
VrfC2SimApp.exe sha256 73d4dd82eb2b6a8dbb9ece58f43a65868c70754a73e9957a12f2c949c0fdcdf6 (162,304 B); VrfC2SimApp.dll
sha256 8a434def41986a4288fde6b8fcbdae84336277ab8454e6f55c2e18b9878933d9 (2,212,864 B) [V 18:26:28Z, golive_checks_g1_3.ps1,
0 FAILED; the values of lane R4's record]; appsettings.json a2a9aa0bc9d9e4c8697aa0830fb596bc6ec683fa09e64bbf943f36003c5e9f96
= src/VrfC2SimApp/appsettings.json at fde3ff2 [V, the source and the deployed copy]; appsettings.Demo.json
1239108016acf23923122ba4b233120cd7c09baecde632321bd2a2be6fb8f6ca (unchanged since G1-2) [V]. VrfBridge.dll
03226dd0e48dae0869ce2fa5c8f3ac521603f2f9d5206ced04c14ad228dd14c5 in ALL ELEVEN trees [A: lane R4; V for the app, WatchVrf,
CreateOne and RtiProbe trees]: tools/RtiProbe was REBUILT IN PLACE at 18:16:15-18:16:17Z, after holder 33476 (appNo 5207)
resigned cleanly at 18:14:59Z [A: lane R4] - step D0 is DONE: RtiProbe.exe sha256
9f960c398802188653ce8ced86ea54cbbec7fe8b713d3ed43acaa3ef80efefda, RtiProbe.dll sha256
517510d893bff6fbc865e8427a4b9f274cbd2d095aa38656cc2722f0458574c7 (written 18:16:17Z, ProductVersion
1.0.0+fde3ff2ad01127e47945f210f95c75464a9db8fc) [V 18:26Z]. THE ROAD LAYER the AUTO planner reads: the 41 osm-highways
tiles lane M3 fetched (41 x HTTP 200, scratch m3\hwy_fetch.log [A]) staged by lane R4 at 17:33:32Z into
<exe dir>\preflight-cache\osm-highways - 41 .pbf, 38,924 B, x 9247-9254 / TMS y 11122-11127, road-layer manifest
(sorted "<name> <sha256>" lines, LF) 213bae2063276bdae535fac6f2b45de7161e8148ae4a3e4c069c0d2039b96432 [V 18:13Z and
18:26Z; = lane R4's]; the whole cache 520 files = G1-2's 479 + 41, manifest (lane E2's cache_manifest.ps1)
27c2117e619c5140c465f071f792c9ccbd49f589113cbabd3e2af78ca63c0bc5 [V 18:13Z and 18:26Z]. src at fde3ff2 = M3
(feat/aggregate-planned-move 9945014, merge fde3ff2; RL-20260928-03) + D2b and audit fixes 1-3 (fix/d2b-and-audit-fixes, merge c72c5df; RL-20260927-06, RL-20260928-01) on
G1-2's 699552c. `git diff --stat 699552c fde3ff2 -- src` [V]: AggregateMovePlanner.cs (new), ContainerBridgeGlue.cs,
ContainerPopulation.cs, ContainerSelfTest.cs, DoNotRulesSelfTest.cs, ModelSetGuard.cs (new), ModelSetGuardSelfTest.cs (new),
OsmSelfTest.cs, PlannedMoveSelfTest.cs (new), Preflight\ModelSetRules.cs, OsmFeatures.cs, PreflightReports.cs,
PreflightService.cs, RouteShift.cs, TileSource.cs, Program.cs, RulingsSelfTest.cs, ShippedProfileSelfTest.cs,
VertexChain.cs, VrfC2SimService.cs, VrfSettings.cs, appsettings.json. `git diff --stat 699552c fde3ff2 -- scripts tools
data config` [V]: RunC2SimScenario.ps1 and RunScenario.sh (audit fix 3: the Iron Storm defaults and a clientId that
follows the init - G1-3 names scenario, init, order and clientId, so its command line resolves exactly as G1-2's) and
data/unit-type-map-52-aggregate.json (one header note now names RL-20260927-02 for the hostile nation; no row changed; sha256
cc41f833eca818b5fc7b1f09545e44d3f3257b6f6f408ec4e9523618e0e85f37 [V], G1-2's c546edbe). `git diff --stat fde3ff2 abe7cdf
-- src` is EMPTY [V]. Offline suites on the deployed exe, from lane R4's record [A: scratch r4\suites_summary.txt, 17:35Z]:
28 switches; planned-move 72/0/0 (and 72/0/0 with the deployed cache, its (m9) table = sec 1(q)), populate 191/0/5 (4 SKIP
by variant), rulings 604/0/0, osm 142/0/0 (146 on the real tiles, 0 fetched), name 77/0/0; RunnerTurnaround 770/0/1 on the
main checkout. NOT built by this lane.
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: G1-2's, unchanged (UG52 72.2.1 p1419; UG52 Table 68 p1470; UG52 18.1 p438; UG52 27.1.4; UG52 13.2 Table
21 p362-363; vrfRemoteController.h (5.2) :1282-1306; see G1-2's registration for what each says), and for the ONE
VARIABLE, THE PLANNED CONTAINER MOVE [V, read-only]: UG52 35.5.11 p735 (Move to Location / Waypoint (Plan Along Roads),
"Used by aggregate level units") and 30.5 p587 (roads are looked for "over a fairly wide area") - RL-20260928-03's basis;
SMS\base\scripts\navigate-to-location.xml :5 (myScriptId navigate-to-location), :34-35 (destination is a
LOCATIONREFERENCE), :55-56 (obstacleQuery string), :76-77 (pathQuery string), :97-98 (buffer, range), :118-119
(displayRoute), :139-140 (query); navigate-to-location.lua :26-27 (defaults MAK_OBSTACLE / MAK_ROAD), :33-43 (buffer <= 0
-> 0), :46-57 (the old "query" overrides obstacleQuery unless it is ""; empty -> "NONE"), :60-63 (an empty pathQuery ->
MAK_ROAD), :65-66 (printDebug "Navigate-to: pathQuery=" / "obstacleQuery="), :75 (the destination is read when the script
loads), :102-118 (init: the async navigateThroughFeatures job), :127 (printDebug "Navigation: Initializing"), :134-137
("Destination object has become invalid" -> endTask false), :141-147 ("Reinitializing task due to movement of target
waypoint"), :163-171 (the member is stopped while the plan computes), :177-186 ("Could not compute path - %1" / "Invalid
path computed" -> endTask false), :195-199 (path altitudes set to 0), :222-235 (unpublished route objects, displayRoute
false), :244-261 (a move-along subtask per route; endTask true after the last); makLua\behaviorEngine\roleNode.lua :61
(successPolicy defaults to "one"), :64-67 (letAllFinish defaults to true), :127-134 (a critical role with no subordinate
fails), :246-280 (all done: success if one succeeded, else "fail --Children completed without a success" :266; a critical
role's failure fails the command :274-280); AggregateTacticalLevel\scripts\group_movement_simplified.lua :201 (isCritical),
:223, :226; appData\settings\featureconfig.txt :88 (NONE matches no feature), :102-103 (RIVER, LAKE), :178 (MAK_ROAD),
:189 (MAK_ROAD_VEHICULAR_TRAIL includes highway=track), :405 (MAK_OBSTACLE = MAK_BUILDING, MAK_VEGETATION,
MAK_INFRASTRUCTURE, MAK_WATERWAY and the dynamic obstacles), :413-414 (MAK_WATERWAY includes RIVER and LAKE);
SharedData\19\latest\TerrainData\TerrainConfiguration\osm.roads.model.xml :9-10 (data:vehicle-roads-linear is built from
data:osm-highways), :55 (mak_vrf_layer Roads); osm.features.xml :35-36 (data:osm-highways); makLua\vrfutil.lua :87-98
(printDebug - relayed at console level [4], as G1-2 saw for PA_Move_Along_Route.lua :28, :80).

OWN-RECORD CITATION:
- Rulings: G1-2's list, unchanged - RL-20260927-01 (movement approach; the pre-flight per model set), RL-20260927-02
  (hostile side RUS; populate the containers), RL-20260927-03 (every unit an EMPTY container at init; only a tasked unit
  populated, in place), RL-20260927-04 (D-1..D-8, D-6 the withhold), RL-20260927-05 (Q1a), RL-20260927-06 (above battalion
  = aggregate only), RL-20260921-09 (the temporary completion position), RL-20260913-03 and RL-20260914-01 (a stuck unit =
  report + TASKABRT), RL-20260925-01 (stall detection ON in the demo profile), RL-20260926-01 (a UNIT's ATTACK is fire at
  will), RL-20260920-01 (route shift ON), RL-20260928-01 ("D2 as recommended. Go for the server") and RL-20260928-02
  (object identity is the UUID) - and, NEW for G1-3, RL-20260928-03 (the owner's "AUTO it is": a tasked container moves by
  the vendor's PLANNING tasks per STP vertex; per member navigate-to-location, obstacleQuery MAK_OBSTACLE, buffer 10 m,
  pathQuery MAK_ROAD where roads lie within Vrf:RoadProximityMeters (500) of the leg, else NONE; Auto is the default; the
  pre-flight is report + fallback; "the first registered run is G1-3 on T14's line").
- docs/PLAN_MOVEMENT_2026-09-27.md CLOSED list and rows M3, D2b, AF1-AF3, C1d, G1-2, G1-3;
  docs/experiments/FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md (whole: the planning moves, the building stop, the
  remedy of its sec 5); RUNBOOK secs 9, 11i, 11j, 11k (D2b), 11l, 11m, 11n (audit fixes), 11o (M3 - its line shapes are
  grepped here), 12, 12a.
- IRONSTORM_AGG_G1-2026-09-28-2 (G1-2, STOPPED at P12, run 20260928T142731Z_run; the pre-warm 20260928T141734Z_run): THE
  CONTROL - the same order, fixture, init, composition, settings and procedure on the 699552c build, the container's route
  driven by PA_Move_Along_Route (the literal executor). Its Result (N1: 11 Mech COs held at one OSM building by the
  vendor's slope check) is what this run's variable answers.

## 0. Purpose, in plain words

G1-3 re-runs G1-2 - the ruled cut-A order on VR-Forces' AGGREGATE model set, 36 EMPTY Aggregate Containers at init, the
three performers populated IN PLACE when the order arrives (28ID 1 member, 1-112 IN 5, 48 IBCT 17) - with ONE change: the
container's MOVE. In G1-2 each container ran PA_Move_Along_Route, which puts every member on STP's straight line and plans
nothing; 11 of 48 IBCT's 12 Mech COs stopped together inside an OSM building of the -2 hamlet on the vendor's slope check
and T14 never arrived. On the M3 build (RL-20260928-03) a container's route is driven ONE STP VERTEX AT A TIME by the
vendor's planning task: at each vertex, every member is sent navigate-to-location to the vertex with obstacleQuery
MAK_OBSTACLE (buildings, vegetation, infrastructure, waterways - lakes and rivers included) and a 10 m buffer, and pathQuery
MAK_ROAD because the sim's road layer lies within 500 m of every cut-A leg (sec 1(q)); the next vertex is issued only when
every member's move has ended, the vertex succeeding when at least one member's did (the vendor's own Move (Group) rule).

THE RUN'S NEW HIGH PREDICTION (P7-P7c, P12): on T14 every member's plan succeeds on each of its two vertices - no "Could
not compute path", no "Terrain too steep" for a Mech CO - the 17 members reach the last vertex, the arrival evidence fires
and T14 closes TASKCMPLT; T10 arrives again (regression); T02, whose straight line crosses river 8011072, is an OBSERVATION
with three branches (planned round the river and arrived; the plan failed and T02 closes TASKABRT with the reason; still
held at the water, the watchdog's S5/S6). P3 (identity by uuid, 59 of 59) stays HIGH as C1d's regression guard.

WHAT THE PREPARATION FOUND, said before the run [V unless marked]: (1) every cut-A leg decides NEAR -> MAK_ROAD, 5 of the 6
on a highway=track (sec 1(q)) - so the ruled default's NONE branch is not exercised (sec 7); (2) the pre-flight's
report-only line "NO ROUTE SHIFT - the leg is driven by the vendor's planning task" prints for a FLAGGED leg that is not a
river crossing only (PreflightService.cs :354, :371), and cut A has no such leg - it is predicted ZERO times (P6), where the
brief expected it for the container legs; (3) the ROAD LAYER line is per TASK - 14 / 10 / 16 tiles readable for T14 / T10 /
T02 - while "41" is the staged set the start-up line counts (P0 (vi)); (4) under Auto the parameter the facade's "location"
must reach is navigate-to-location's destination, a LOCATIONREFERENCE (navigate-to-location.xml :34-35) - the
"locationwithoutaltitude" the brief names is Move (Group)'s, which Auto does not run (sec 1(s)); (5) the drivable road
bridges over river 8011072 in the cached osm tiles are 3.74 km and 10.2 km from T02's straight-line crossing (sec 1(t)).

## 1. Decisions taken from the record

(a) THE ORDER - G1-2's, unchanged: the RULED cut-A order, data/IRONSTORM_CUTA_Order.xml sha256
7a9861372f07702fc136f91f63b8bf971650fcc91c20aa62803d73cfd869f5c7 on init 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d
7614f3d26dc93eec [V, main checkout and branch]. The five tasks, as G1-2 (its sec 1(a) table), with what M3 makes of each:

| Task (uuid) | performer (container) | verb -> decision | members | route (pts incl. the live origin) | M3 form | armed end (SIM s) |
|---|---|---|---|---|---|---|
| T01 f7b52ba4 | 28ID__FRIENDLY_INFANTRY_DIVISION (DIV, PROXY) | CNFPSL -> held in place (STP-866) | 1 | not driven | hold, unchanged | 300, no destination |
| T02 696fbb33 | 28ID (same) | ATTACK -> a UNIT's ATTACK: advance with fire at will (RL-20260926-01) | 1 | 2: origin -> PassagePoint_28ID_SLOT0 54.028874, 23.264401 (5,341 m) | PLANNED, 1 vertex | 300, destination |
| T10 9aab7fe6 | 1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE (BN) | CRESRV -> bare move | 5 | 4: origin -> (h) 54.029734, 23.305499 -> (j) 54.024, 23.313 -> PassagePoint_48_IBCT_SLOT0 54.019389, 23.313902 (2,771 m) | PLANNED, 3 vertices | 450, destination |
| T13 37677c40 | 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE (BDE) | CNFPSL -> held in place | 17 | not driven | hold, unchanged | 300, no destination |
| T14 1075b583 | 48 IBCT (same) | FOLSPT -> advance along the graphic and hold, ROEHold | 17 | 3: origin -> (i) 54.0146, 23.3315 -> 54.040348, 23.324206 (4,172 m) | PLANNED, 2 vertices | 300, destination |

(b) SETTINGS - G1-2's, each with its reason, and THE ONE VARIABLE:
- THE MOVE PLANNER: NOT passed. The deployed appsettings.json ships Vrf:AggregateMovePlanner "Auto" (:121),
  Vrf:AllowLiteralMove false (:124), Vrf:RoadProximityMeters 500 (:127) [V, the deployed copy read by
  golive_checks_g1_3.ps1 at 18:13Z], the default RL-20260928-03 rules; VrfSettings.cs compiles the same
  (AggregateMovePolicy.Default). Passing --env Vrf__AggregateMovePlanner=Auto would test the environment override, not the
  ruled default as shipped, so the command line stays G1-2's byte for byte (sec 3 E). Vrf:PreflightOffline stays false
  (:85): the route-shift worker may fetch a road tile it lacks - none is lacking (sec 1(q)), and P19 scores a fetch HIGH.
- FIXTURE IronStorm_Centre_52_Aggregate - G1-2's, deployed 2026-09-28T02:57:41Z and unchanged: sha256
  804e2c393dcf5f4fe4e1c58d7423a343c4e43250cd719d5474e035750c76a0e3 [V 18:13Z]. NO fixture deploy; nothing under C:\MAK is
  written by this registration or its go-live.
- MODEL SET: `--model-set auto` (D2, RL-20260927-06, RL-20260928-01) - G1-2's: RunnerLib.ps1 Select-ModelSetByEchelon
  (:2143; the choice line :2232, the pairing note :1981 - both unchanged 699552c..fde3ff2 [V]) chooses AggregateTacticalLevel
  from the DIV performer; RunC2SimScenario.ps1 prints it at :2394, :2396-2401, :2404-2405, :2409-2410, :2665-2673 [V at
  fde3ff2]. NEW inside the app (D2b, RUNBOOK 11k): the same rule is applied again at order receipt (ModelSetGuard, P1a).
- COMPOSITION, CLIENT ID "Not Set", DURATION SCALE 0.25, SUCCESSOR-GATE FLOOR 600 s, PRE-ORDER GATE none, CONSOLES
  --object-console 4 --member-console 4, WINDOW -StopWhenComplete with the 2,700 s cap, ENV Vrf__StallDetection=true /
  Vrf__StallClock=sim / Vrf__TaskPredecessorTimeoutSeconds=600, --sample-threads, --no-gui, SERVER the private 18080 / 61614
  (RL-20260928-01), TEARDOWN GATE StopVrf52 exit 0 or 6 - G1-2's, unchanged. The type map follows the model set
  (data/unit-type-map-52-aggregate.json, now cc41f833 - a header note only, sec Registration).
- NOT passed (shipped values): route shift ON (RL-20260920-01), the pre-flight and its cache location,
  Vrf:PlatformMoveToPerVertex, Vrf:CatalogueSms, Vrf:ContainerPopulateTimeoutSeconds - G1-2's; and now the planner (above).

(c) THE PRE-WARM - G1-2's procedure: the scored command with `--run-secs 120`, its own appNumber block, the order pushed,
UNSCORED, dry run first; it is THE W GATE (sec 3 W), which now also asks the first live questions of M3: whether the
planner's parameters reach the vendor script (the members head for their vertex 1: W-DEST; the members' consoles relay
"Navigation: Initializing" and "obstacleQuery=MAK_OBSTACLE": P9) and whether a vertex's completion arrives (W-DONE). G1-2's
pre-warm dispatched all three movers inside its 120 s window and T10 even arrived [V: u3\laneG1-2\score_prewarm_full_
20260928T141734Z_run.txt, T10 dispatched SIM 330.1, its arrival evidence inside the window], so T10's vertex 1 (1,451 m on
the straight line) is expected to be reached and closed there.

(d) POPULATION HAPPENS AT ORDER RECEIPT - G1-2's, with fde3ff2 lines: OnOrder calls MaterializeUnit for each task's
performer (VrfC2SimService.cs:5047-5049), once per unit (:2792), on the aggregate set PopulateInPlace (:2819-2823); HELD
for the init (:2806-2813) or DEFERRED to the container's ObjectCreated (DeferPopulate :3273-3281, re-entered from
OnVrfObjectCreated :7129-7130); every task of a container waits on the population gate before it dispatches (:5218-5236).
NEW: the D2b guard runs in OnOrder BEFORE any of this (:4818-4832).

(e) ORDER VALIDATION (C1 / C2, offline) - G1-2's results [A: the parsers and both data files unchanged 699552c..fde3ff2;
the deploy lane's parse-init-IS-cutA / parse-order-IS-cutA exit 0 on the deployed exe, r4\suites_summary.txt]. C3 is the
one real push (sec 3).

(f) THE FORM - M3's: a container's route task (moveForm RouteTask, VertexChain.cs:91: SinglePointMoveTo only for <= 1
point, so T02's 2-point route is a RouteTask, as in G1-2) that is not a patrol goes to StartContainerPlannedMove
(VrfC2SimService.cs:6181-6184) whenever the planner is not Literal: T10 3 vertices, T14 2, T02 1 (routeGeo[1..n]; point 0 is
the live origin and is never driven to). MarkDispatched first (:3597; kind "navigate-to-location"), then the chain (M1's
VertexChainTracker with a completion type, :3598-3600 - a PLANNED chain accepts only "navigate-to-location", VertexChain.cs
:474-479, and hands its LAST vertex to D-6 even when vacuous, :363-369), then vertex 1 (:3609), then the dispatch line
(:3619-3626). CreateRoute, PA_Move_Along_Route (:6186 on) and PA_Move_To_Location_Direct (:3450-3465) are unreachable for
these three tasks - predicted ZERO (P7).

(g) M2 ON THE AGGREGATE MODEL SET - REPORT + FALLBACK (RL-20260928-03; FINDING sec 5 item 4). The pre-flight scores and
reports every container route exactly as G1-2's harness predicted (PreflightService, VertexNudgeSearch and ModelSetRules'
rules unchanged - ModelSetRules.cs changed a comment only [V]): T14 2 vertices checked, 0 moved, legs 1,270 / 2,902 m not
flagged, EXPECTED SLOW 1,270 of 1,270 / 1,663 of 2,902 m, "no leg flagged"; T10 3 checked, legs 1,451 / 804 / 516 m none
flagged, EXPECTED SLOW 805 / 103 / 369 m; T02 1 checked, leg 1 FLAGGED - OSM 8011072 (waterway=river, MAK_WIDTH 5 m) first
wet at 54.0087722, 23.2351231 (2,398.9 m along), "NO ROUTE SHIFT - RIVER CROSSING", EXPECTED SLOW 3,370 of 5,342 m; 8
pre-flight ObservationReports. NEW: RouteShiftOptions.ReportOnlyReason is set for a planned container (VrfC2SimService.cs
:8096-8104), so a flagged leg would be REPORTED and never detoured (PreflightService.cs :354, :371; RouteShift.cs
ReportOnlyShift :631-644; its line :8186-8187 and its marking "ROUTE SHIFT NOT APPLIED - PLANNED LEG", PreflightReports.cs
:276-280). The river test comes first (:365-369), so T02's river crossing is reported as in G1-2, and no cut-A leg reaches
the report-only branch: ZERO report-only lines and ZERO splices (P6). NEW: the worker warms the ROAD LAYER off the tick
thread - PreflightService.WarmOsm (:551-563) over every tile within 2 x Vrf:RoadProximityMeters = 1,000 m of each checked
leg (:8107-8108, :8146), one "ROAD LAYER for the AUTO planner" line per task (:8148-8152).

(h) THE PLANNED MOVE - WHAT IS ISSUED (AggregateMovePlanner.cs ForVertex :160-178, NavigateVars :180-188): for vertex k,
RunScriptedTask(<member uuid>, "navigate-to-location", [destination=(lat,lon) at altitude 0, obstacleQuery="MAK_OBSTACLE",
pathQuery="MAK_ROAD" (the leg decided NEAR) or "NONE", buffer=10, displayRoute=false, query=""]) to EVERY member the
population attached, by uuid (ContainerPopulation.cs TryIssueMemberMoves :455-463, through the one gate MoveVerdict; the
members listed by AttachedMembersOf :216-230), after the step is opened (PlannedMoveTracker.BeginVertex :393-402;
VrfC2SimService.cs :3643-3646). The vertices, as the OUTSTANDING lines will print them (F6): T14 (54.014600,23.331500),
(54.040348,23.324206); T10 (54.029734,23.305499), (54.024000,23.313000), (54.019389,23.313902); T02 (54.028874,23.264401).
query="" on purpose: the script lets the old "query" override obstacleQuery unless it is "" (navigate-to-location.lua
:49-51). EVERY MEMBER IS SENT TO THE VERTEX ITSELF - there is no per-member offset under Auto - so the 17 members of 48
IBCT converge on one point at each vertex (O1).

(i) THE RING AND THE MEMBERS - G1-2's names, uuids and slots, unchanged (the ring, the slot check and the names are
untouched 699552c..fde3ff2 [V: no such file in the src diff but ContainerPopulation.cs, whose change is the two M3 methods
and the Text / Number variable kinds]): 28ID 1 member (28ID__FRIENDLY_INFANTRY_DI.HQ1 = c8d5c7b5-89b5-5584-9ae9-ec2e5a958ff4),
1-112 IN 5 (HQ1 0daa745e..., RIF1 fe0c3275..., RIF2 081d4816..., RIF3 17f86e4f..., WPN1 36393c4e...), 48 IBCT 17 (4 x
Stryker HHT (USA), 12 x Mech CO (USA, M2), 1 x Stryker Cavalry SQDN (USA); HQ1 d003da2c..., CAV1 46c65670..., the full list
in G1-2 sec 1(i) and g1_3_score.py --expected); slots 4, 5, 6, 16 moved off water (125 m NW, 125 m SW, 25 m S, 75 m SE),
13 clear; the members' centroid 54.019341, 23.313809 = T14's route origin. WHAT CHANGES: under PA_Move_Along_Route each
member drove from its slot to the route's FIRST vertex (the origin); under Auto each member plans from wherever it stands to
VERTEX 1 itself. The FAR-SHORE member 48_IBCT/28ID__FRIENDL.INF1RIF2 (slot 54.022284, 23.319544, across lake 197345448 from
the origin) no longer has a leg that meets the lake: it plans round MAK_OBSTACLE, which includes LAKE (featureconfig.txt
:103, :413-414) - it is predicted to reach vertex 1 (P11), where in G1-2 it drove 13 m, rested 4.2 m inside the lake
polygon's edge and printed "Movement constrained by features" [V: g1_3_score.py on the G1-2 pre-warm, sec 4 MEASURES].

(j) TIMINGS, estimated, not scored: T10 ~700-1,300 SIM s from dispatch to its arrival evidence (G1-2 571.5 on the
straight line; road paths, three planning pauses and the per-vertex waits for the slowest member add); T14 ~900-1,800 SIM
s; T02 by branch - a planned detour by the nearest cached drivable road bridge (sec 1(t)) would be ~9 km, some 600-900 SIM
s for the Stryker HHT; a failed plan within seconds of the dispatch.

(k) NAMES, PROXY TAGS AND MARKINGS - G1-2's (seven NEAREST containers keep their names, "Proxy marking tag NOT appended",
VrfC2SimService.cs:1657-1659; 0 NAME DISAMBIGUATED / NAME COLLISION (C1c), :1675-1695; every member's name (30 at most)
comes back exactly; 35 of the 36 init names come back as their first 30).

(l) THE TO TWINS AT INIT - G1-2's (ApplyHierarchyComposition :2433-2516; the lines :2512, :2567).

(m) THE TILE CACHE: the deployed <exe dir>\preflight-cache - G1-2's 479 files (osm-water 225, osm 225, the raster tiles)
PLUS the 41 osm-highways tiles staged by lane R4 at 17:33:32Z: 520 files, manifest 27c2117e...0bc5, road layer 213bae20...6432
[V 18:13Z and 18:26Z].
The pre-flight reads osm-water and osm as in G1-2 (49 hits); the road warm reads osm-highways (sec 1(q)); the dispatch reads
osm-highways through a cache-only reader that never fetches (TileSource.cs OsmCacheReader :851-907).

(n) THE CONTROL: G1-2 (run 20260928T142731Z_run, pre-warm 20260928T141734Z_run) - the same order, fixture, init,
composition, settings, sequence and harness geometry; the build differs by M3 (the variable), D2b (one guard at order
receipt, one start-up line) and audit fixes 1-3 (self-tests and the runner's defaults; nothing in G1-3's command line
resolves differently). g1_3_score.py on G1-2's own files reads identity branch A with I1-I18 and W0-W7 PASS, and FAILS
every M3 limb (M1-M7, M11, M16) and M13 (the 11 "Terrain too steep" Mech COs, all 11 stationary inside a building at
54.030655, 23.326944) - the fail-first of this registration's instruments on real data [V, scratch
u3\laneG1-3\control_g1_2_scored_score.txt, control_g1_2_prewarm_wgate.txt].

(o) IDENTITY - G1-2's sec 1(o), unchanged code, fde3ff2 lines: the refusal without the uuid overloads :656-664, the rule
said once :665-671; each init unit's plan carries its C2SIM uuid (:1605-1609), each member's the derived uuid (:3211-3215);
EnqueueCreates registers name and uuid (:2333-2363; RequestIdentity :2374-2389) and calls the overloads (:2356-2360);
OnVrfObjectCreated binds by the uuid first and says IDENTITY created-as (:7083), the not-requested WARN (:7089-7091), the old
truncation WARN only when NOT bound by uuid (:7092-7097); the READY TO TASK census (:4069-4078); the population's plan line
(:3245-3251), census (:7178-7186) and member lines (:7164-7174); a completion's marking resolved marking -> uuid -> name
(ResolveReportMarking :9652-9658; OnVrfTaskCompleted :9669). The container gate and the planned moves address the
container and the members by uuid.

(p) WHOSE POSITION THE JUDGES READ - G1-2's sec 1(p), fde3ff2 lines: one position per container, its own reflected fix
(TryReadUnitPositions :8902-8926; the member list empty for AGGREGATED members, :5380-5385; UnitPositionPolicy.cs:96-101);
ARRIVAL EVIDENCE "1/1 member(s)" with the ONE POSITION suffix (:8409-8428, suffix :8425-8427), the stall watchdog the same
(:9548-9562, suffix :9228-9230), D-6 the same fix against the route end (:3478-3479). NEW: the vertex bar - each COMPLETED /
LAST line states the container's fix distance to the vertex when the step closed ("the unit is <d> m from it", DescribeVertexFix
:6715-6721; VACUOUS beyond Vrf:VertexArrivalRadiusMeters = 100 m, VertexChain.cs :359); an intermediate vertex advances
either way (:381-384).

(q) THE AUTO ROAD TABLE, OFFLINE [V: scratch u3\laneG1-3\road_table_g1_3.py - an independent Python transcription of
OsmQuery.TilesNear :1542-1567, NearestRoad :1575-1605, OsmVendor.IsVehicleRoad :445-451 and AggregateMovePolicy.DecideRoads
:198-215 / RoadLine :271-289, on lane M3's 41 tiles = the staged set; its selftest PASS], AGREEING line for line with the
deployed exe's own `--planned-move-selftest <cache>` (m9) on the deployed cache [A: scratch r4\suites\planned-move-CACHE.log
:74-80; its control on a cache without osm-highways reads UNKNOWN -> NONE on every leg, planned-move-CONTROL-no-highways.log
:75-80]. On the G1-2 dispatch routes (live origin first), Vrf:RoadProximityMeters 500:

| leg | length | decision | nearest OSM way | 500 m band |
|---|---|---|---|---|
| T14 vertex 1 of 2 | 1,270 m | roads NEAR (0 m) -> MAK_ROAD | 783013873 highway=track | 5 tiles, 0 unreadable |
| T14 vertex 2 of 2 | 2,902 m | roads NEAR (0 m) -> MAK_ROAD | 312326007 highway=track | 5 tiles, 0 unreadable |
| T10 vertex 1 of 3 | 1,451 m | roads NEAR (0 m) -> MAK_ROAD | 372319246 highway=track | 5 tiles, 0 unreadable |
| T10 vertex 2 of 3 | 804 m | roads NEAR (0 m) -> MAK_ROAD | 367165904 highway=track | 5 tiles, 0 unreadable |
| T10 vertex 3 of 3 | 516 m | roads NEAR (1 m) -> MAK_ROAD | 1262103420 highway=unclassified | 4 tiles, 0 unreadable |
| T02 vertex 1 of 1 | 5,341 m | roads NEAR (0 m) -> MAK_ROAD | 384833185 highway=track | 10 tiles, 0 unreadable |

THE ROAD LAYER LINES: WarmOsm reads, per task, every tile within 1,000 m of its legs, distinct - T14 14, T10 10, T02 16
(the deploy lane's probe on the deployed cache reads the same, r4\prewarm_probe.txt [A]); all 30 are among the 41 staged
(11 staged tiles lie outside every band). The nearest tile NOT in each band lies 72 m (T14), 28 m (T10) and 191 m (T02)
beyond the band's reach, so a live origin within 28 m of G1-2's adds no tile; G1-2's origins were identical in both runs
[V: the pre-warm and scored traces - 54.019341,23.313809 / 54.042688,23.308235 / 53.992385,23.211255].

(r) THE COMPLETION MAPPING (AggregateMovePlanner.cs GroupOutcome :232-237, PlannedMoveTracker :338-443; the vendor's rule
read [V] in roleNode.lua :61, :64-67, :127-134, :246-280): a member's own "navigate-to-location" completion arrives through
OnVrfTaskCompleted's member branch (VrfC2SimService.cs :9689-9693) and is consumed by the open step (RoutePlannedMemberCompletion
:3719-3743); anything else - a move-along subtask, a second report from the same member, a report after the step closed -
is not consumed and stays C1's Debug line (:9694-9698). The step closes when EVERY member's move has ended; the vertex
SUCCEEDS when at least one did and FAILS when none did. A succeeded intermediate vertex issues the next one
(ConsumePlannedVertexCompletion :3664-3712; QueueNextVertex; IssueNextVertex -> IssuePlannedVertex :6776-6780); the LAST
vertex and a FAILED vertex go to the container's own completion tail (CompletePlannedStep :3750-3763): D-6 for a success not
yet arrived (:3754-3755), the arrival swallow (:3757-3760), SynthesizeUnitCompletion (:3762), which reports a failure as
TASKABRT with the reason the failed vertex left (:3689-3690, :9876-9880; FailureReason AggregateMovePlanner.cs :240-249).
NOTE (the M3 lane's correction, applied to FINDING_AGGREGATE_MOVEMENT_OBSTACLES sec 2 in this commit): one member's failed
plan does NOT fail a vertex - it fails only when no member succeeded. There is NO per-vertex timeout: a vertex whose
completion never arrives is caught by the time rules (OVERDUE at the end time) and the stall watchdog (sec Conditions; S9).

(s) THE DESTINATION AND THE VENDOR SCRIPT'S OWN LINES. navigate-to-location.xml declares destination as a LOCATIONREFERENCE
(:34-35). The facade sends a "location" (a DtRwVector, VrfFacade.cpp :184-185); M3's reading is that the script's Lua table
is built from the script's metadata and the vendor's own saved navigate-to-location task carries the same value class
(AggregateMovePlanner.cs :146-151; RoadToKaunasPhaseTwo.oob :109481-109500 [A]); --populate-selftest p11 binds it as a
DtRwVector offline. What the script does with it: it reads destination:getLocation3D() when it LOADS (:75), again in init()
(:103), and checks destination:isValid() every tick (:134). So: (1) a destination that did not bind is a script error at
load and a member task that ends at once (member FAILED lines; S8's or S9's signature); (2) one bound to another point sends
the members elsewhere (W-DEST); (3) one bound right prints, at level 4 on the MEMBER's console, "Navigate-to: pathQuery=
MAK_ROAD", "Navigate-to: obstacleQuery=MAK_OBSTACLE" (:65-66) and "Navigation: Initializing" (:127), relayed as
`VRF console [4] <member> (VRF_UUID:<uuid>): <text>` (VrfC2SimService.cs:10027) because --member-console 4 asks for it (the
member relay is live: G1-2 relayed member lines at [4] and [1]). "obstacleQuery=NONE" would mean the obstacle query never
reached the script (:46-57).

(t) T02 AND ITS RIVER [V: scratch u3\laneG1-3\river_probe_g1_3.py on lane G1's copy of the deployed osm set, the vendor
filters as OsmFeatures.cs transcribes them - IsRiverLine and IsRoadBridge :416-424]: river 8011072 (27 parts, 847 vertices
in the cached tiles) passes 0.6 m from T02's straight-line crossing point; the drivable road bridges over it in the cached
tiles are OSM 307511217 (highway=tertiary, bridge=yes) 3,743 m from that point (at 54.036598, 23.202873) and OSM 307510446
10,238 m away. The sim streams more of the world than the cache holds, so a nearer bridge outside the cached tiles is not
excluded. T02's leg decides NEAR on a track (sec 1(q)), so its member plans along the road network round MAK_WATERWAY; whether
the planner will take a bridge across a line obstacle it must also keep 10 m from is exactly what O2 observes.

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: none (sec 1(b)); the order is pushed right after the runner's stage-7 oracle gate.

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: the watchdog runs on the SIMULATION clock (--env Vrf__StallClock=sim), window 360 SIM s, 50 m,
on each container's ONE position. Armed ends in SIM s from dispatch: T01 and T13 300 (no destination - never watched); T02
and T14 300 and T10 450 (destination tasks: an unarrived one goes OVERDUE and stays watched; nothing is SENT at that end).
UNDER M3 the container's fix also pauses while its members PLAN (navigate-to-location stops a member while its path
computes, navigate-to-location.lua :163-171) and while the early members WAIT at a vertex for the last one; a pause that
reaches 360 SIM s is a stall verdict (TASKABRT, report only) - not predicted for T14 or T10 (P12), recorded as O12.

DEVIATION FROM RECORD: the template's "PRE-ORDER GATE: --pre-order-gate nav-area" ("gate PushOrder on the first "New Primary nav area" row; warm the area first") is not used - the aggregate model set loads no nav data, so that row can never print and the gate could only time out (AGGREGATE_PROFILE_OFFLINE sec 6.4); the app's own D5b hold and the C1 publication gate cover an early order. (G1's and G1-2's, unchanged.)

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels. (G1's and G1-2's, unchanged.)

DEVIATION FROM RECORD: the successor-gate floor is 600 s - appsettings.json's shipped value - not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:357); as G1, G1-2 and E2-2.

DEVIATION FROM RECORD: the window closes EARLY under -StopWhenComplete, where E1 registered "--no-stop-when-complete --run-secs 2700"; as G1 and G1-2, the 2700 s cap is kept.

DEVIATION FROM RECORD: the pre-warm is a whole runner launch that also pushes the order and runs 120 s, where DEMO_RUNBOOK sec 0.4 says "let it reach the initialization, then stop it"; there is no init-only runner mode (LIVE1 sec 1(e)). (G1's and G1-2's, unchanged; here it is the W gate, sec 1(c).)

DEVIATION FROM RECORD: the design's G1 outline ("Order: cut A reduced to T13 -> T14 on T14's ORIGINAL line", DESIGN_AGGREGATE_CONTAINERS sec 9) is replaced by the ruled cut-A order at the seat's direction; the original line is G2's. (G1's and G1-2's, unchanged.)

DEVIATION FROM RECORD: the brief asked for the pre-flight line "leg k: NO ROUTE SHIFT - the leg is driven by the vendor's planning task" on the container legs; the code prints it for a FLAGGED leg that is not a river crossing only (PreflightService.cs :354, :371) and cut A has none, so P6 predicts it ZERO times and scores the absence of splices instead (sec 0 item 2).

EFFECT OF -StopWhenComplete ON THE WINDOW - G1-2's: the runner closes it once all 3 taskees and all 5 tasks have a
TERMINAL report, 60 s have passed and every taskee has post-completion position evidence (Test-EarlyExit, RunnerLib.ps1:328
[V unchanged]). T14's and T10's terminal reports are predicted to be their arrival-evidence TASKCMPLTs and T02's one of
its three branches; the window closes about a minute after the last of them. The last vertex's LAST VERTEX COMPLETED line
and its swallow may fall after the window (P7b, P16 RECORDED).

## 2. What the code emits - log-line shapes (src at fde3ff2 = the deployed build; VrfC2SimService.cs unless named) [V]

G1-2's sec 2 applies shape for shape; its line numbers at fde3ff2 are: L-BUILD :613; L-FIDELITY :631; L-CAT :2633;
L-VARIANT :3116 (ContainerComposition.cs :359-365, :429-448 unchanged); L-ROW :3135, its REFUSED twin :3128; L-CON-ON
:3141-3162 and its refusal twin :3099-3100 (ContainerPopulation.cs :542-546 "AGGREGATE CONTAINERS off (" must not print);
L-IDENTITY-ON :665-671, its refusal :656-664; L-WDOG-ON :716; L-CACHE :773; L-SHIFT-ON :822; L-M1-ON :854-855
(VertexChain.cs :113-131); L-MODELSET :859-861; L-DESTACK :874; L-APPROACH :890; L-CLOCK :948; the Q1a gate line :8859;
L-OSMSET :7436 (at the first population's slot check, CheckPopulateSlots :3342-3345). INIT: L-TYPEMAP :1662; L-PXYTAG
:1657-1659; L-NOCONT :1638; NAME DISAMBIGUATED :1689 / NAME COLLISION :1680; L-CENSUS-AGG :1861 (entity twin :1871);
L-COMPOSE :2512, :2567; L-BARRIER :3937; L-READY DispatchReadiness.cs :322-347; L-PLACE :4392; L-INIT-PXY :2293; L-INIT
:2296; L-ID-CREATED :7083; L-ID-NOTREQ :7089-7091; L-TRUNC :7092-7097; L-AMBIG :7098-7101; L-REBIND :7110-7115; L-BIND
:7144-7145; L-ID-INIT :4069-4078. POPULATION: L-POP :3234-3244 (twins REFUSED :3222, deferred :3279, HELD logged :2810);
L-ID-POP :3245-3251 (member NAME DISAMBIGUATED :3254-3256 must not print); L-SLOT :3399-3401; L-ISSUED :3403-3405 (late
twin :3386-3387); L-MEMBER :7164-7170, its WARN twin :7171-7174; L-ID-POPSUM :7178-7186; L-ATTACHED ContainerPopulation.cs
:495-498 (partial :489-493, late member :349-351); L-PUB ContainerPopulation.cs :387-391 ("(expected N)" = what attached,
:485; TIMED OUT :507-509, reasons :372, :377, :394-396); L-REANNOUNCE :2994; L-D5 :5056. TASKING: L-DISP :6369;
L-CONSOLE-MEMBERS :5409; L-CNFPSL :5640-5642 and L-INPLACE :5643-5648; L-FAW :7025-7029; L-R3SELF :6302; L-FOLSPT :6026;
L-BARE :5438; L-SHIFTQ :8110-8112; L-VCHECK :8021-8024; L-ELEV :7532; L-OSMWATER :7616-7625; L-SLOW :7637-7639; L-RIVER
:8181-8183 (the note Preflight\RouteShift.cs :623); L-NOFLAG :8219-8220; the vendor console relay :10027. JUDGING: L-TIMED
:8797 (WARN) / :8808; L-ARRIVE :8409-8428; L-CANNOT :8337; L-STALL :9224-9231 and its TASKABRT :9241-9244; L-VRFDONE
:9699; a member's completion at Debug :9694-9698; L-D6 :3483-3488 (WARN) and its hand-on :3492-3497; L-SWALLOW :9721;
L-OVERDUE-ARR :9839; L-HELD :9834; L-ID-MARK :9656; "no C2SIM uuid known" :9801; L-SENT :10282. SHUTDOWN: "Cleanup: deleting
N created VR-Forces objects before resign..." :1138 and "Cleanup: N deletes dispatched" :1146. THE RUNNER: RunC2SimScenario.ps1
:2394 (model set), :2396-2401 (tasked), :2404 (type map), :2405 (fixture SMS), :2409-2410 (the pairing note, RunnerLib.ps1
:1981), :2665-2671 (composition), :2672-2673 (catalogue); manifest inputs.modelSet :3039-3091.

GONE for a container under Auto (G1-2 printed them; G1-3 predicts ZERO): L-ROUTE-C :6249 "Task '<T>': CreateRoute '<T>
ROUTE' (<n> pts) for CONTAINER ..."; L-SCRIPT :7290 "Route '<T> ROUTE' (<VRF_UUID>) created; RunScriptedTask
PA_Move_Along_Route issued for CONTAINER ..."; the point move :3462; the vendor's PA script lines "Init of aggregate move
along." / "Sub. <m> starting move-along." / "has completed move task." / "has completed final move." (PA_Move_Along_Route.lua
:28, :80, :110, :140).

NEW START-UP LINES (in this order around G1-2's):
- L-M3-ON (:3163-3166, right after L-CON-ON; AggregateMovePlanner.cs StartupLine :292-327) `AGGREGATE MOVE PLANNER Auto
  (Vrf:AggregateMovePlanner; RL-20260928-03, the default is Auto): a CONTAINER's route is driven per STP vertex, per MEMBER,
  navigate-to-location with obstacleQuery MAK_OBSTACLE and buffer 10 m always, pathQuery MAK_ROAD where the sim's road layer
  (osm-highways) lies within 500 m (Vrf:RoadProximityMeters) of the leg's line, else NONE - decided and logged per leg. Road
  layer cache: <exe dir>\preflight-cache\osm-highways (41 tile file(s), 0 of them 0 bytes); the dispatch reads it and never
  fetches; the route-shift worker fetches missing tiles online first. The next vertex is issued only when the previous one
  COMPLETES (...); arrival evidence, D-6 and the watchdog judge the LAST vertex; the pre-flight REPORTS a planned route and
  does not detour it (FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28 sec 5 item 4). Point moves (PA_Move_To_Location_Direct),
  patrols (PA_Patrol_Route) and holds are unchanged.` Its twins must not print: `AGGREGATE MOVE PLANNER not in use ...`
  (:3034-3035) and a REFUSING TO START list carrying "(M3, RL-20260928-03)" (:3041-3042, :3099-3100).
- L-CON-ON's route clause, CHANGED (:3158-3160): `Moves are the container's own scripted tasks: the vendor's planning task per
  vertex (Vrf:AggregateMovePlanner=Auto, RL-20260928-03) on a route, PA_Move_To_Location_Direct to a single point,
  PA_Patrol_Route for a patrol.`
- L-D2B-ON (:864, after L-MODELSET; ModelSetGuard.cs StartupLine :314-327) `MODEL SET RULE (D2b; RL-20260927-06,
  RL-20260928-01): Vrf:ModelSet='AggregateTacticalLevel' -> AggregateTacticalLevel. At every order the TASKED units (each
  task's PerformingEntity, never the init's untasked units) are ranked by their init EchelonCode on the runner's ladder
  (scripts/RunnerLib.ps1 Get-EchelonRank; ...): every order runs here - above BN is aggregate-only and this IS the aggregate
  model set, and a BN-and-below order runs as the OVERRIDE UP (allowed, RL-20260927-06).`
- The init read (:1523-1525, WARN) `MODEL SET RULE (D2b): the initialization's EchelonCodes could not be read (...)` must not
  print.

NEW PER-ORDER LINE: L-D2B-ORDER (:4818-4832, before the order's graphics and populations; ModelSetGuard.cs Decide :218-311,
the allowed-aggregate text :285-288) at INFO `MODEL SET RULE (D2b): allowed - this order's highest TASKED echelon DIV
(28ID__FRIENDLY_INFANTRY_DIVISION) is ABOVE BN, aggregate-only, and this interface runs Vrf:ModelSet=AggregateTacticalLevel
(RL-20260927-06); 3 tasked unit(s), 5 task(s).` Its REFUSED twin (ERROR, :4821-4829) `MODEL SET RULE (D2b): ORDER REFUSED -
ABOVE BATTALION IS AGGREGATE-ONLY ...` with a TASKABRT per task reading `REFUSED (D2b): ABOVE BATTALION IS AGGREGATE-ONLY ...`.

NEW PER MOVER (M3; each RL-20260928-03), in log order:
- L-ROADLAYER (:8148-8152, from the route-shift worker, before the dispatch) `Task '<T>' (<c>): ROAD LAYER for the AUTO
  planner (RL-20260928-03) - <K> osm-highways tile(s) within 1000 m of the legs readable, <U> not; the dispatch decides each
  leg's pathQuery from the cache.` (with " (Vrf:PreflightOffline: nothing was fetched)" before ";" when offline - not here).
- L-DISP (:6369) with kind `navigate-to-location` (MarkDispatched :3597).
- L-ROAD for vertex 1 (:3639; RoadLine AggregateMovePlanner.cs :271-289) `PLANNED MOVE <c> vertex 1 of <N>: roads NEAR (<d>
  m) -> MAK_ROAD; obstacleQuery MAK_OBSTACLE buffer 10 m [nearest OSM <way> highway=<class>; within
  Vrf:RoadProximityMeters=500]`; its twins `roads FAR (<d> m) -> NONE; ...`, `roads FAR (> 1000 m) -> NONE; ...`, `roads UNKNOWN
  (<u> osm-highways tile(s) within 500 m NOT readable, no road within 500 m on the rest) -> NONE; ...` and `roads not decided
  per leg ...` must not print.
- L-OUT (:3652-3655; VertexLine :258-262) `PLANNED MOVE <c> vertex 1 of <N>: navigate-to-location issued (useRoads true) ->
  OUTSTANDING to (<lat F6>,<lon F6>) - to each of <M> member(s) [destination=(<lat F6>,<lon F6>),
  obstacleQuery="MAK_OBSTACLE", pathQuery="MAK_ROAD", buffer=10, displayRoute=false, query=""] (RL-20260928-03)`.
- L-PLANNED (:3619-3626) `Task '<T>': PLANNED MOVE for CONTAINER <c> (VRF_UUID:<its C2SIM uuid>) - Vrf:AggregateMovePlanner=Auto
  (RL-20260928-03): <N> vertex(es), one vendor planning task each, the next only when the previous one COMPLETES; each vertex
  goes to every one of its <M> attached member(s); no route object is created. Arrival evidence, the time rules, the watchdog
  and D-6 judge the LAST vertex.` Twins that must not print: `... is ALREADY driving this task's PLANNED MOVE ...` (:3560-3562,
  WARN) and `Task '<T>' REFUSED: PLANNED MOVE for CONTAINER <c> under Vrf:AggregateMovePlanner=Auto - <why> (RL-20260928-03).`
  (:3580-3584, ERROR, with its TASKABRT).
- The members' consoles (relay :10027): `VRF console [4] <member> (VRF_UUID:<uuid>): Navigate-to: pathQuery=MAK_ROAD`, `...:
  Navigate-to: obstacleQuery=MAK_OBSTACLE`, `...: Navigation: Initializing` (navigate-to-location.lua :65-66, :127); failure
  lines `... Could not compute path - <message>` (:179), `... Invalid path computed` (:181), `... Invalid buffer (<b>)` (:96),
  `... Could not start async job` (:122), `... Destination object has become invalid` (:135), `... Cound not create route`
  (:231, the vendor's spelling); `... Reinitializing task due to movement of target waypoint` (:143).
- L-MSTEP (:3730-3732) `PLANNED MOVE <c> vertex <k> of <N>: member <m> navigate-to-location COMPLETED|FAILED - <i> of <M>
  member(s) ended (<s> succeeded, <f> failed); waiting for the rest.` - and for the last member L-MLAST (:3735-3739) `PLANNED
  MOVE <c> vertex <k> of <N>: member <m> navigate-to-location COMPLETED|FAILED - the LAST of <M>: <s> succeeded, <f> failed ->
  the vertex SUCCEEDED|FAILED by the vendor's Move (Group) rule (done when every member's move ended; a success when at least
  one succeeded - makLua behaviorEngine roleNode.lua :169-282; RL-20260928-03).` (A member report that is not consumed stays
  at Debug, :3728.)
- L-VDONE (:3677-3678) `PLANNED MOVE <c> vertex <k> of <N>: navigate-to-location issued (useRoads true) -> COMPLETED - the unit
  is <d> m from it and moved <m> m since dispatch|vertex <k-1>[ (VACUOUS by the vertex bar - R11)]; members <s> succeeded /
  <f> failed of <M>; vertex <k+1> is issued next (RL-20260928-03)` - then vertex k+1's L-ROAD and L-OUT.
- L-VLAST (:3682-3684) `... -> LAST VERTEX COMPLETED - <where>; members <s> succeeded / <f> failed of <M>; handed to the task's
  completion rules: D-6 (RL-20260927-04), arrival evidence and start time + Duration (RL-20260921-09)` - then L-SWALLOW
  (:3758-3759) `VRF completion for <c> after the arrival-evidence report of task <T> - swallowed.` when the arrival came first,
  else D-6's line (:3483-3497) and SynthesizeUnitCompletion.
- L-VFAIL (:3691-3693, WARN) `... -> FAILED - <where>; members 0 succeeded / <M> failed of <M>. PLANNED MOVE <c> vertex <k> of
  <N>: navigate-to-location FAILED for <M> of <M> member(s) and succeeded for none - by the vendor's Move (Group) rule (a critical
  role fails only when no member succeeded, roleNode.lua :246-280) the vertex FAILED; the chain ends and no further vertex is
  issued; the task takes the failure path (TASKABRT, follow-ons abandoned)`, then one TASKABRT carrying that reason
  (:9876-9880).
- L-STRAY / L-RETIRED (:3697-3701, :3704-3706, WARN) `PLANNED MOVE <c>: a VR-Forces completion ('<type>', success=<b>) arrived
  while ... - it is not this chain's navigate-to-location and is SWALLOWED ...` / `... for a chain FROZEN by the back-end loss
  ...` must not print.
- The pre-flight's report-only leg line (:8186-8187, WARN) `Task '<T>' (<c>) leg <k>: NO ROUTE SHIFT - the leg is driven by
  the vendor's planning task (Vrf:AggregateMovePlanner=Auto, RL-20260928-03), which plans round MAK_OBSTACLE itself - the
  pre-flight REPORTS a planned leg and does not detour it (...). The task is dispatched on the line as authored.` and its
  ObservationReport marking `ROUTE SHIFT NOT APPLIED - PLANNED LEG: ...` - predicted ZERO on cut A (sec 1(g)).

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run unless marked PREP)

All from Git Bash at the MAIN checkout F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM (HEAD abe7cdf at
registration, tracked tree clean, `git diff --stat fde3ff2 main -- src` EMPTY [V 18:26Z]). The order, init, type map and
composition are the main checkout's own data/ files (hashes = this branch's [V]). The runner writes its appNumber blocks into
the MAIN checkout's working-tree docs/OPUS_EXECUTION_PLAN.md; they are carried back to this branch afterwards (the G1 and
G1-2 procedure). Scripts (scratch <u3> = H:\claude\F--Repos-C2SIM-c2simVRFinterfacev2-36\5fc25950-1a10-4ade-9a7b-68cb5c1daf05\
scratchpad\u3, in laneG1-3): golive_checks_g1_3.ps1, claim_holder_g1_3.ps1, c3_push_g1_3.sh, g1_3_runner.sh (one command
line for every runner call: prewarm-dryrun | prewarm | scored-dryrun | scored), g1_3_score.py, road_table_g1_3.py; lane G1's
simcache_listing.ps1; lane E2's cache_manifest.ps1. Python = /c/Users/PauloBarthelmess/AppData/Local/Programs/Python/
Python312/python.exe (the bare "python" on this machine is the Store alias). THE SEAT RUNS C3, D, W, E AND F under
RL-20260928-01, as in G1 and G1-2; D0 was lane R4's and is done.

THE HOLDER BRANCH is (b), the only one: the G1 holder 33476 (appNo 5207) resigned cleanly at 18:14:59Z [A: lane R4] and
every step below runs after it. No branch (a) exists in this registration.

A0. PRECONDITIONS: (1) the seat's go-live; (2) lane R4 has FINISHED (the ten trees, the road tiles staged, the RtiProbe
    tree rebuilt in place - D0, done) and no lane is building, running a suite or an agent for the quiet period; (3) `git diff
    --stat fde3ff2 main -- src` is still EMPTY - if the managed source moved on, NOTHING is rebuilt: the registered build is
    what runs and the difference is recorded; if the DEPLOYED hashes differ from the registered ones (another lane rebuilt),
    STOP before W and the seat decides; (4) the deployed values as registered (sec Registration; the script's defaults).
A.  "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File <u3>/laneG1-3/golive_checks_g1_3.ps1 -Phase preholder
    -MarkerWant 5255   EXPECT 0 checks FAILED. [PREP V 2026-09-28T18:26:28Z, after holder 33476 resigned and after D0: 0
    FAILED - exe 73d4dd82, dll 8a434def, ProductVersion 1.0.0+git.fde3ff2.Release-5.2, appsettings a2a9aa0b, Demo 12391080,
    deployed Auto / 500 / false / PreflightOffline false, bridge 03226dd0 in the app, WatchVrf, CreateOne and RtiProbe
    trees, RtiProbe.exe 9f960c39 / RtiProbe.dll 517510d8, fixture 804e2c39, order 7a986137, init 2000e856, type map cc41f833,
    composition 9684e945, osm-highways 41 / 0 empty / manifest 213bae20, cache 520 / manifest 27c2117e, REST 200, rtiexec
    47980 / rtiForwarder 50740 / rtiAssistant 30240 up, holder 33476 gone, no RtiProbe, no sim/app/observer, marker 5255.
    The first PREP reading, 18:13:11Z, before the resignation and D0, had failed exactly the then-open limbs.]
A2. THE BUILD, OFFLINE - "the REFUSING line = STOP before W": the deployed exe's `--populate-selftest` reads 0 FAIL with
    (p15) "the LINKED VrfBridge.dll CARRIES CreateEntity(..., String uuid) and CreateAggregate(..., Boolean, String uuid)"
    (ContainerSelfTest.cs :1633-1637), and its `--planned-move-selftest <exe dir>\preflight-cache` reads 72 PASS with the (m9)
    table of sec 1(q) - from the deploy lane's record [A: r4\suites\populate.log 191/0/5, planned-move-CACHE.log 72/0/0] or
    re-run by the seat with the 5.2 PATH prefix, DOTNET_ENVIRONMENT empty and no Vrf__ variable:
        src/VrfC2SimApp/bin/Release-5.2/net10.0/win-x64/VrfC2SimApp.exe --populate-selftest
        src/VrfC2SimApp/bin/Release-5.2/net10.0/win-x64/VrfC2SimApp.exe --planned-move-selftest "F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM\src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\preflight-cache"
    A FAIL, a missing line or an (m9) leg other than NEAR = STOP before W.
B.  No build by this lane or the seat (D0, lane R4's in-place rebuild of the RtiProbe tree, is done).
C.  Order validation: C1 / C2 [A: sec 1(e)]; C3 ONE real push to the PRIVATE server (RL-20260928-01), at go-live, after this
    registration is committed - the seat runs `sh <u3>/laneG1-3/c3_push_g1_3.sh`, i.e. G1's two lines verbatim:
        tools/PushInit/bin/Release/net10.0/PushInit.exe data/IRONSTORM_CUTA_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
        tools/PushOrder/bin/Release/net10.0/PushOrder.exe data/IRONSTORM_CUTA_Order.xml 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
    EXPECT both exit 0, "QUERYINIT : 40 Units" and one `ORDER (69670 chars)` echo carrying 5 tasks, 5 Durations and
    CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT (G1's and G1-2's). Any failure = STOP.
D0. THE RTIPROBE TREE, REBUILT IN PLACE - DONE by lane R4 at 18:16:15-18:16:17Z, after holder 33476 (appNo 5207) resigned
    cleanly at 18:14:59Z [A: lane R4], with the pin's own command (RUNBOOK sec 9 step 3; its script r4\rebuild_rtiprobe.ps1
    refuses while any RtiProbe runs or a tree binary is locked):
        dotnet build tools/RtiProbe/RtiProbe.csproj -c Release -p:BridgeConfig=Release-5.2 -t:Rebuild -m:1 -nodeReuse:false
    [V 18:26Z: VrfBridge.dll 03226dd0...14c5, RtiProbe.exe 9f960c398802188653ce8ced86ea54cbbec7fe8b713d3ed43acaa3ef80efefda,
    RtiProbe.dll 517510d893bff6fbc865e8427a4b9f274cbd2d095aa38656cc2722f0458574c7, written 18:16:17Z.] Step A re-verifies all
    three (the script checks them); a mismatch = STOP, no holder is claimed on another tree.
D.  THE NEW HOLDER. First HAND-CLAIM 5255-5258 in Appendix B of the main checkout's working-tree OPUS_EXECUTION_PLAN.md
    (from the marker - 5255 at registration [V]; if it reads otherwise at go-live, every number below shifts with it and the
    difference is recorded) BEFORE the holder joins:
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File <u3>/laneG1-3/claim_holder_g1_3.ps1 -Path docs/OPUS_EXECUTION_PLAN.md -From 5255
    (marker 5255 -> 5259; the same claim is committed on this branch with the Result) [PREP V: on a scratch copy of the file
    the claim inserted its entry above the marker, moved it to 5259, kept ASCII + CRLF, and refused a second application];
    then the holder, -WhatIf first:
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5255,5256,5257,5258 -SettleSecs 28800 -WhatIf
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5255,5256,5257,5258 -SettleSecs 28800 < /dev/null > runs/launch52/g1-3-holder-<stamp>.log 2>&1
    EXPECT `HOLDER JOINED: pid ... appNo 5255`, exit 0. Exit 1 (none joined) or 2 = STOP, no blind relaunch (RUNBOOK 9c).
W.  THE PRE-WARM (unscored) = THE W GATE: golive_checks_g1_3.ps1 -Phase prelaunch -HolderPid <the new pid> -MarkerWant 5259
    (0 FAILED); "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File <u3>/laneG1/simcache_listing.ps1
    -Out <u3>/laneG1-3/simcache_before_prewarm.txt; `sh <u3>/laneG1-3/g1_3_runner.sh prewarm-dryrun` (EXPECT the new holder
    recognised PERSISTENT, block 5259-5269, "marker would advance to: 5270", the three D2 lines of G1-2's P0 (iii) verbatim,
    window 120 s CAP); then `sh <u3>/laneG1-3/g1_3_runner.sh prewarm` once. GATE for E - ANY miss = STOP before E: StopVrf52
    exit 0 or 6; the post-W inventory shows only rtiexec, rtiForwarder, rtiAssistant, the persistent holder and the
    pre-warm's Stage 2h holder; the cache manifest unchanged; AND on the pre-warm's run directory
        /c/Users/PauloBarthelmess/AppData/Local/Programs/Python/Python312/python.exe <u3>/laneG1-3/g1_3_score.py <prewarm run dir> --wgate
    prints "W GATE: PASS" (exit 0): G1-2's IDENTITY + POPULATION limbs (W0-W7, I1-I17, branch A) AND THE M3 LIMBS - M1 (L-M3-ON,
    41 / 0), M2 (L-D2B-ON), M3 (L-D2B-ORDER), M4 (one L-ROADLAYER per dispatched mover, at least k readable - 14 / 10 / 16 for
    T14 / T10 / T02 - and "0 not"), M5 (one L-PLANNED per dispatched mover with N / M as registered, L-DISP kind
    navigate-to-location), M6 (the chain in order, NEVER BOTH), M7 (the OUTSTANDING variables), M8 (one NEAR -> MAK_ROAD road
    line per issued vertex), M9 (every CLOSED step consistent), M10 (0 FAILED member or vertex on T14 / T10), M12 (0 failure
    line from a T14 / T10 member), M13 (0 "Terrain too steep" from a T14 Mech CO), M14 (0 obstacleQuery other than
    MAK_OBSTACLE), M15 (0 stray, retired, refused or already-driving line), M16 (the literal route gone), M17 (0 report-only /
    splice) -
    and from the pre-warm's trace W-DEST (each dispatched mover's container got at least 50 m closer to its vertex 1: the
    destination reached the script), W-DONE (a vertex whose members all came within 50 m of it at least 20 s before the
    window's end CLOSED - "the LAST of M ... -> the vertex SUCCEEDED"; reached but not closed = S9 = FAIL; none reached = NOT
    REACHED, the seat's call) and the water falsifiers (P-FALS (e)). A W showing S7 or S10 is ALSO a STOP before E. Then
    simcache_listing.ps1 -Out <u3>/laneG1-3/simcache_after_prewarm.txt.
E.  THE RUN: golive_checks_g1_3.ps1 -Phase prelaunch (as W) -MarkerWant 5270 (0 FAILED); `sh <u3>/laneG1-3/g1_3_runner.sh
    scored-dryrun` (EXPECT block 5270-5280, marker -> 5281, the D2 lines, window 2700 s CAP with -StopWhenComplete); then `sh
    <u3>/laneG1-3/g1_3_runner.sh scored` ONCE, stdout to a file, never piped. The command g1_3_runner.sh runs, verbatim -
    G1-2's, the log name aside (the planner is the deployed default and is NOT passed, sec 1(b)):

        scripts/RunScenario.sh \
          --scenario IronStorm_Centre_52_Aggregate \
          --init data/IRONSTORM_CUTA_Initialization.xml \
          --order data/IRONSTORM_CUTA_Order.xml \
          --client-id "Not Set" \
          --model-set auto \
          --duration-scale 0.25 \
          --object-console 4 --member-console 4 \
          --stop-when-complete --run-secs 2700 \
          --env Vrf__StallDetection=true \
          --env Vrf__StallClock=sim \
          --env Vrf__TaskPredecessorTimeoutSeconds=600 \
          --sample-threads \
          --no-gui \
          --log runs/launch52/RunScenario-ironstorm-agg-g1-3-<stamp>.log

    (the pre-warm: the same line with --run-secs 120 and its own log name; the dry runs add --dry-run). GATE on teardown:
    StopVrf52 exit 0 or 6 (P21); 3, 5 or 7 = STOP.
F.  Post-run, in the FOREGROUND: the inventory (only the RTI trio, the persistent holder and, inside its 900 s hold, the
    Stage 2h holder); hashes and both manifests re-read (golive_checks_g1_3.ps1 -Phase prelaunch -HolderPid <the new pid>
    -MarkerWant 5281); simcache_listing.ps1 -Out <u3>/laneG1-3/simcache_after_scored.txt; then the harvest (sec 6):
        /c/Users/PauloBarthelmess/AppData/Local/Programs/Python/Python312/python.exe <u3>/laneG1-3/g1_3_score.py <scored run dir> > <u3>/laneG1-3/score_<run>.txt

QUIET PERIOD (RUNBOOK 0.5.14 item 5) - G1-2's: from the launch of W to the post-run inventory of E: no Stop-Process /
taskkill of any kind (StopVrf52's own identity-gated force of the run's own back end is allowed), no build, no suite, no
subagent, no second runner - in this lane or any other. Never touched: rtiexec 47980, rtiForwarder 50740, rtiAssistant
30240, any RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES - G1-2's: per TASK UUID from the scored run's vrfc2simapp.log in line order, cross-checked against
reports-captured.log; positions from watchvrf-trace.csv POS rows. THE SCORER scratch u3\laneG1-3\g1_3_score.py <runDir>
(derived from G1-2's g1_2_score.py - the identity limbs I1-I18 and S7, the population limbs W0-W7, P2(f), P15, O7 and the
trace measures KEPT - plus the M3 / D2b line families of sec 2, the chain and step checks M1-M17, the per-leg road decisions
against sec 1(q), the per-member completion counts, the convergence spread per vertex, the water falsifiers (a fix more than
25 m inside an OSM water feature, even-odd over the feature's rings as OsmGeometry.Inside does; a river crossing more than 15 m
from a drivable road bridge after which the member drove on 50 m) and the stationary runs away from the vertices with their
obstacle classes. Its --selftest: 41 checks PASS, 0 FAIL - the uuid5 oracle, the chain checker's controls, a CLEAN control
(every identity, population and M3 limb, the W gate, the convergence, the water falsifiers), S7 as its own pattern, a
MUST-NOT-FIRE control (a member resting 2 m inside a lake's edge) and 26 DIRTY controls (mixed binding, the IDENTITY refusal,
a wrong derivation, no publication, a timed-out population, a member TaskStatus, a DELETE, an early cleanup, a D-6 that
closes, a point move, a FAR and an UNKNOWN road decision, NEVER BOTH broken, a member FAILED, a vertex FAILED, the literal
route, obstacleQuery=NONE, "Could not compute path", a road layer "1 not", a road layer one tile short of its k, a D2b
refusal, "Terrain too steep", S9 (reached, no completion), the destination ignored, a member deep in a lake, a river crossed
off a bridge) each caught [V,
u3\laneG1-3\g1_3_score_selftest.txt]. One inherited defect fixed on the way: G1-2's P15 limb (ii) returned a list, not a
bool, when no arrival followed a WITHHELD line - the very case it exists for - and would have crashed the scorer there.
Names bind to uuids through L-BIND and L-CONSOLE-MEMBERS; members belong to the container whose L-POP lists them; Mech COs
by their L-POP template. THE CLOCKS: the ~21 s app/trace offset (O10) - every comparison is anchored on the APP clock (the
L-DISP WALL stamps) and trace instants are placed with the WatchVrf join stamp, as G1 and G1-2 did.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) HOLDER BRANCH (b): the G1 holder 33476 gone, the RtiProbe tree rebuilt at the pin 03226dd0 (D0), the new holder claimed 5255-5258 JOINED ("HOLDER JOINED: pid ... appNo 5255") and recognised PERSISTENT by the runner; the back end JOINS (rtiexec count-grep: no create for its appNo); "READY - joined the federation". (ii) L-BUILD names git fde3ff2, ProductVersion 1.0.0+git.fde3ff2.Release-5.2; the exe 73d4dd82 / dll 8a434def, appsettings a2a9aa0b / Demo 12391080, the bridge 03226dd0 in the app, WatchVrf, CreateOne and RtiProbe trees (RtiProbe.exe 9f960c39 / .dll 517510d8), fixture 804e2c39, order 7a986137, init 2000e856, type map cc41f833, composition 9684e945, the road layer 41 tiles (manifest 213bae20) and the cache 520 files (manifest 27c2117e) - identical before and after each run. (iii) D2: the runner's Stage 0 prints, verbatim, the three lines of G1-2's P0 (iii) ("model set   : AggregateTacticalLevel <- auto (RL-20260927-06): highest TASKED echelon DIV (28ID__FRIENDLY_INFANTRY_DIVISION) is ABOVE BN - aggregate-only; exported to the app as Vrf__ModelSet", the pairing line "fixture C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Aggregate.scnx loads AggregateTacticalLevel (aggregatetacticallevel.sms) - matches the chosen model set." and "catalogue   : not exported - the app keeps the default "" (the model set's own SMS)"), the tasked block DIV / BN / BDE; manifest inputs.modelSet.modelSet AggregateTacticalLevel, selection.highestEchelon DIV. (iv) 0 REFUSING TO START of any kind - the IDENTITY refusal and the M3 planner refusal ("(M3, RL-20260928-03)", :3041-3042) included - and no "AGGREGATE CONTAINERS off" or "AGGREGATE MOVE PLANNER not in use" line; L-IDENTITY-ON once. (v) THE FLOOR ARRIVED: L-CLOCK reads `Vrf:TaskPredecessorTimeoutSeconds=600 s` and both gate lines (T02, T14) read `and then 600 s to COMPLETE`. (vi) THE ROAD LAYER (RL-20260928-03): L-M3-ON's start-up census reads "Road layer cache: ...\preflight-cache\osm-highways (41 tile file(s), 0 of them 0 bytes)" (41 = the staged set), and each mover's L-ROADLAYER reads "ROAD LAYER for the AUTO planner (RL-20260928-03) - k osm-highways tile(s) within 1000 m of the legs readable, 0 not" with k = 14 for T14, 10 for T10, 16 for T02 (30 distinct tiles; the other 11 staged are margin) - NOT 41 per task. (vii) The W gate (sec 3 W) held. | HIGH | Any limb = VOID + STOP (launch, harness or settings failure - no code verdict). A D2 refusal, another model-set choice or another fixture in the DRY RUN = STOP before launch; the IDENTITY REFUSING line = STOP before W (step A2). In the PRE-WARM, FEWER READABLE THAN THOSE k (or any "not" above 0) = STOP before E: the AUTO decision would fall to UNKNOWN -> NONE and the run would not test the ruled default (RL-20260928-03). | |
| P1 | START-UP LINES, each exactly once, at INFO - G1-2's with fde3ff2 lines: L-FIDELITY 62 rows; L-CAT (705 templates, 3 model-set dirs, root AggregateTacticalLevel, C:\MAK\vrforces5.2d); L-VARIANT catalogue with "5 of the 8 row(s)", the fixture's SMS clause and "7 authoredRows type(s) ... NOT in use"; FIVE L-ROW 1/0, 17/3, 5/0, 26/6, 8/1 and ZERO REFUSED rows; L-CON-ON with "hostile nation RUS", "(5 row(s) resolve, 0 refused)", "85 s", "100 m" and - CHANGED BY M3 - the route clause "the vendor's planning task per vertex (Vrf:AggregateMovePlanner=Auto, RL-20260928-03) on a route, PA_Move_To_Location_Direct to a single point, PA_Patrol_Route for a patrol"; NEW L-M3-ON right after it, "AGGREGATE MOVE PLANNER Auto (Vrf:AggregateMovePlanner; RL-20260928-03, the default is Auto): a CONTAINER's route is driven per STP vertex, per MEMBER, navigate-to-location with obstacleQuery MAK_OBSTACLE and buffer 10 m always, pathQuery MAK_ROAD where the sim's road layer (osm-highways) lies within 500 m (Vrf:RoadProximityMeters) of the leg's line, else NONE - decided and logged per leg. Road layer cache: ...\osm-highways (41 tile file(s), 0 of them 0 bytes); ..."; L-IDENTITY-ON (namespace 485b28e7-1cc7-534b-b6a6-9beddf07a1b1); L-MODELSET (AggregateTacticalLevel); NEW L-D2B-ON "MODEL SET RULE (D2b; RL-20260927-06, RL-20260928-01): Vrf:ModelSet='AggregateTacticalLevel' -> AggregateTacticalLevel. ... every order runs here - above BN is aggregate-only and this IS the aggregate model set, ..."; L-M1-ON; L-WDOG-ON (360 s, SIMULATION, 50 m); L-CLOCK (SIMULATION, 0.25, 600, 60, 86400); L-SHIFT-ON; L-OSMSET once (osm-water 225 / 0 empty, \osm 225 / 0), after the first L-POP and before the first L-DISP. ZERO "is not EntityLevel or AggregateTacticalLevel"; ZERO "the initialization's EchelonCodes could not be read". | HIGH | A missing, duplicated or differently-numbered line = STOP (the build or the settings are not what was registered). | |
| P1a | D2b AT ORDER RECEIPT (RL-20260927-06, RL-20260928-01): exactly one L-D2B-ORDER at INFO, "MODEL SET RULE (D2b): allowed - this order's highest TASKED echelon DIV (28ID__FRIENDLY_INFANTRY_DIVISION) is ABOVE BN, aggregate-only, and this interface runs Vrf:ModelSet=AggregateTacticalLevel (RL-20260927-06); 3 tasked unit(s), 5 task(s).", before the first L-POP; ZERO "ORDER REFUSED", ZERO "OVERRIDE UP", ZERO "is BN or below", ZERO TASKABRT reading "REFUSED (D2b)". | HIGH | A REFUSED line = STOP (every task TASKABRT and nothing dispatched: the model set the runner chose and the one the app runs disagree); any other shape or count = STOP. | |
| P2 | INIT - 36 EMPTY CONTAINERS - G1-2's P2, limbs (a)-(g) as registered there, with fde3ff2 lines: 36 L-TYPEMAP "CONTAINER (RL-20260927-03)" lines (29 Exact, 7 Proxy "NEAREST branch"; 48 IBCT -> BDE (11.1.225.8.3.1.1), 1-112 IN -> BN, Light Infantry (11.1.225.6.3.1.0), 28ID -> DIV, Mech Infantry (11.1.225.9.4.1.0) Proxy; hostile nation 260); ZERO L-NOCONT and init NAME DISAMBIGUATED / NAME COLLISION (C1c); 7 L-PXYTAG; L-CENSUS-AGG 36 / 0 / 0, no entity census; L-BARRIER 36 objects (36 empty shells); L-INIT "36 units + 5 areas"; L-INIT-PXY 7; the 36 container names bound by L-BIND before the order under VRF_UUID:<own C2SIM uuid>, 0 member names before it; ZERO DELETE-family lines before "Application is shutting down" (the shutdown cleanup is excluded by name; its count RECORDED - 64 expected = 36 + 23 + 5 areas, no route object, where G1-2's 67 counted three routes); ZERO L-TRUNC (VrfC2SimService.cs:7092). READY TO TASK, the init L-PLACE counts and "ORDER BEFORE READY TO TASK" RECORDED. | HIGH | Any limb = STOP, except limb (g) under S7. | |
| P2b | THE TO TWINS - G1-2's: two L-COMPOSE parents, 28ID/III_Corps__TWO_EIGHT_TH_US_INFANTRY_DIVISION with 9 and 278_Armored_Cavalry_Regiment/28ID__TWO_SEVEN_EIGHT_ARMORED_CAVA... with 1, each "composed - n/N" with n = N. | MEDIUM | Recorded MEDIUM miss. | |
| P3 | IDENTITY BY UUID - C1d's REGRESSION GUARD (RL-20260928-02): G1-2's I1-I18, unchanged - L-IDENTITY-ON once and 0 IDENTITY REFUSING; EXACTLY 59 L-ID-CREATED at INFO (the 36 init containers under their own C2SIM uuids, 35 with the cut marking and 4ID__FRIENDLY_INFANTRY_DIVISION whole; the 23 members under their derived v5 uuids with their own names as markings); ZERO L-ID-NOTREQ; L-ID-INIT "36 of 36 objects bound by uuid, 0 by name" at INFO after READY TO TASK; per population one L-ID-POP naming exactly the derivation, one L-ID-POPSUM "1 of 1" / "5 of 5" / "17 of 17 objects bound by uuid, 0 by name"; 23 L-MEMBER "...; bound by its uuid (C1d)."; ZERO of every identity WARN / ERROR family; L-BIND for all 59 under the requested uuid; the WatchVrf trace carries POS rows under the requested uuids of the 3 performers and the 23 members. | HIGH | The falsifier: any L-ID-NOTREQ, or L-ID-INIT "0 of 36 ... by uuid" - branch S7, recorded, the run continues, P3 a MISS (HIGH) and the verdict STOPPED at P3; a MIXED outcome or any other I-limb failing = STOP. | |
| P3a | POPULATION, per performer, at ORDER RECEIPT - G1-2's P3a, unchanged: one L-POP each (28ID source 3 row C-USA-DIV-UCI, 1 member, radius 0 m; 1-112 IN C-USA-BN-UCI, 5, 153 m; 48 IBCT C-USA-BDE-UCI, 17, 490 m, "3 sub-container(s) ... flattened"), the member lists in slot order, N L-SLOT, one L-ISSUED, one L-ATTACHED "N of N ... (<HQ1> first)", one L-PUB "(expected N)" with n >= N, one L-REANNOUNCE; all 23 member names bound; ZERO REFUSED, TIMED OUT, partial, late-member and slot-check-late lines. | HIGH | A missing or duplicated line, another source / row / count / radius / name, a member unbound, or any refusal / timeout = STOP, except S2-S4. | |
| P3b | SLOT VERDICTS - G1-2's: 28ID 1 clear; 1-112 IN 5 clear; 48 IBCT 13 clear + slots 4, 5, 6, 16 "SLOT MOVED 125 m north-west / 125 m south-west / 25 m south / 75 m south-east"; ZERO KEPT ON BAD GROUND, ZERO UNVERIFIED. | HIGH | Another verdict = STOP. | |
| P3c | PUBLICATION BEFORE DISPATCH - G1-2's: each container's L-PUB precedes the L-DISP of every task it performs; "(expected N)" = N; ZERO memberless-move refusals other than S2's, and ZERO "REFUSED: PLANNED MOVE for CONTAINER" (the population attached no member, a nested container, a member without a uuid - VrfC2SimService.cs:3570-3587). | HIGH | Any dispatch before its container's L-PUB, or a refusal that is not S2's registered consequence = STOP. | |
| P3e | D-5 - G1-2's: ZERO "is NOT populated - D-5" (:5050-5063 never entered); only the three performers populated. | HIGH | A D-5 line or another population = STOP. | |
| P4 | CNFPSL HOLDS T01 and T13 - G1-2's: one L-DISP (hold-in-place) each after its container's L-PUB, one L-CNFPSL + one L-INPLACE, one STP-866 observation, "no destination" armed at 300, NO route, NO scripted or planned task, exactly one TASKCMPLT "(300 s after dispatch)". The served figure RECORDED (O9). | HIGH | A move, a script or a planned move for a hold; zero or two TASKCMPLTs; an OVERDUE for a hold = STOP. | |
| P5 | DISPATCH STRUCTURE - G1-2's: exactly one SENT TASKSTRT per task (5); T02's L-DISP after T01's TASKCMPLT and T14's after T13's; one "start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25)" line for T10 and its L-DISP after it; ZERO "SKIPPED: predecessor"; ZERO gate-expiry lines (:8859). The dispatch order RECORDED. | HIGH | A missing or duplicate TASKSTRT, a successor before its predecessor's TASKCMPLT, or a SKIP = STOP. | |
| P6 | M2, REPORT + FALLBACK (RL-20260928-03), per mover - G1-2's numbers (sec 1(g)): L-SHIFTQ "(2 vertices)" T02, "(4 vertices)" T10, "(3 vertices)" T14; L-VCHECK 1 / 3 / 2 checked, 0 moved, 0 kept, 0 unverified; L-ELEV L12 x1 / x3 / x2; L-SLOW T02 3370 of 5342 m, T10 805/1451, 103/804, 369/516 m, T14 1270/1270, 1663/2902 m; T02 one L-OSMWATER "OSM 8011072 (waterway=river (MAK_WIDTH 5 m))" first at 54.0087..., 23.2351... ~2.40 km along and one L-RIVER; T10 and T14 one L-NOFLAG each; NEW: one L-ROADLAYER per mover, "T14 14, T10 10, T02 16 readable, 0 not" (HIGH, with P0 (vi): fewer readable = STOP); ZERO L-REPORTONLY "NO ROUTE SHIFT - the leg is driven by the vendor's planning task" and ZERO "ROUTE SHIFT NOT APPLIED - PLANNED LEG" (no cut-A leg reaches that branch, sec 1(g)); ZERO ROUTE SHIFTED and ZERO PRE-DISPATCH applied (0 splices on a container leg); ZERO VERTEX MOVED / NOT MOVED / UNVERIFIED, "could NOT be read", "NO CLEARED LINE", land-cover "WATER ON THE LINE"; ZERO "the ROUTE SHIFT search finished AFTER the dispatch" (a worker discarded by the 30 s timeout would discard its ROAD LAYER line too - the dispatch's decision reads the staged cache either way); exactly 8 pre-flight ObservationReports in the capture. | HIGH | Any limb = STOP (an L-ROADLAYER count ABOVE its k with "0 not" - a moved live origin, sec 1(q) - is recorded, not a stop). | |
| P7 | THE PLANNED MOVE (RL-20260928-03): per mover exactly one L-PLANNED "Task '<T>': PLANNED MOVE for CONTAINER <c> (VRF_UUID:<its C2SIM uuid>) - Vrf:AggregateMovePlanner=Auto (RL-20260928-03): N vertex(es), ... each vertex goes to every one of its M attached member(s); no route object is created. ..." with (N, M) = T10 (3, 5), T14 (2, 17), T02 (1, 1), printed after its vertex 1's L-ROAD and L-OUT; L-DISP kind "navigate-to-location"; ZERO L-ROUTE-C, L-SCRIPT, "RunScriptedTask PA_Move_To_Location_Direct", "created; MoveAlongRoute issued", "MOVE TO PER VERTEX for", "VERTEX CHAIN ... MoveToLocation", "each vertex goes to the container", "is ALREADY driving this task's PLANNED MOVE" and "REFUSED: PLANNED MOVE" (DoNotRulesSelfTest d10, live: no container move issues PA_Move_Along_Route unless Literal); L-CONSOLE-MEMBERS names 1 / 5 / 17 distinct members under their derived uuids on each pass (the pass count RECORDED). | HIGH | Any other form, count, uuid or kind = STOP (a container uuid other than its C2SIM uuid is S7, not a new stop). | |
| P7a | THE AUTO DECISION PER LEG (RL-20260928-03): exactly one L-ROAD per ISSUED vertex, before its L-OUT (T14 2, T10 3, T02 1 once every vertex is issued), each "PLANNED MOVE <c> vertex k of N: roads NEAR (<d> m) -> MAK_ROAD; obstacleQuery MAK_OBSTACLE buffer 10 m [nearest OSM <way> highway=<class>; within Vrf:RoadProximityMeters=500]"; ZERO "roads FAR", "roads UNKNOWN", "roads not decided per leg". The distance and the way as sec 1(q)'s table: T14 v1 0 m 783013873 track, v2 0 m 312326007 track; T10 v1 0 m 372319246 track, v2 0 m 367165904 track, v3 1 m 1262103420 unclassified; T02 v1 0 m 384833185 track. | HIGH (NEAR -> MAK_ROAD on every leg); MEDIUM (the distance within 5 m and the way) | A NONE or UNKNOWN decision on cut A = MISS (HIGH; S10): the run would not test the ruled default. Another way or a distance above 5 m = recorded MEDIUM miss (a moved live origin, sec 1(q)). | |
| P7b | THE CHAIN, PER VERTEX: per mover, in log order, L-OUT 1, then for each k < N one L-VDONE "-> COMPLETED - the unit is <d> m from it ...; members M succeeded / 0 failed of M; vertex k+1 is issued next (RL-20260928-03)" followed by vertex k+1's L-ROAD and L-OUT, and for k = N one L-VLAST "-> LAST VERTEX COMPLETED - ...; handed to the task's completion rules: D-6 (RL-20260927-04), arrival evidence and start time + Duration (RL-20260921-09)"; NEVER BOTH (no L-OUT while another vertex of the container is outstanding) and none skipped; each L-OUT "navigate-to-location issued (useRoads true) -> OUTSTANDING to (<lat>,<lon>) - to each of M member(s) [destination=(<lat>,<lon>), obstacleQuery="MAK_OBSTACLE", pathQuery="MAK_ROAD", buffer=10, displayRoute=false, query=""] (RL-20260928-03)" at the registered vertex (sec 1(h)). For T14 and T10 every vertex is ISSUED and every intermediate vertex COMPLETED inside the window; ZERO "-> FAILED" for T14 and T10; ZERO L-STRAY / L-RETIRED. RECORDED (MEDIUM): each L-VDONE / L-VLAST "the unit is <d> m from it" within 100 m and never "(VACUOUS by the vertex bar - R11)"; T14's and T10's L-VLAST inside the window. | HIGH | An order break, a skipped vertex, a FAILED vertex on T14 or T10, or an intermediate vertex of T14 / T10 not COMPLETED = STOP (a FAILED vertex of T02 is S8, O2(b)). The MEDIUM limbs: recorded MEDIUM misses. | |
| P7c | THE MEMBER STEP, THE VENDOR'S RULE (roleNode.lua :61, :64-67, :246-280; RL-20260928-03): per closed vertex exactly M member lines between its L-OUT and its L-VDONE / L-VLAST, one per attached member by name - M-1 L-MSTEP "member <m> navigate-to-location COMPLETED - i of M member(s) ended (s succeeded, f failed); waiting for the rest." (i = 1..M-1 in order) and one L-MLAST "the LAST of M: s succeeded, f failed -> the vertex SUCCEEDED by the vendor's Move (Group) rule" - with s + f = M, SUCCEEDED iff s >= 1, and the vertex line's "members s succeeded / f failed of M" equal to it. T14 AND T10: EVERY MEMBER'S PLAN SUCCEEDS ON EVERY VERTEX (f = 0) - ZERO member "navigate-to-location FAILED"; ZERO relayed "Could not compute path", "Invalid path computed", "Invalid buffer", "Could not start async job", "Destination object has become invalid", "Cound not create route" or script error from any of their members; ZERO "Terrain too steep" from a T14 Mech CO (the G1-2 falsifier, N1). | HIGH | A FAILED member of T14 or T10, a failure line from one of their members, or an inconsistent step = STOP. A T14 Mech CO stopped by a building again ("Terrain too steep" and held short of its vertex) = STOP, and FINDING_AGGREGATE_MOVEMENT_OBSTACLES' remedy (its sec 5 item 1) is REFUTED for that leg. | |
| P8 | VERB LINES - G1-2's: T02 exactly one L-FAW (a UNIT's ATTACK, RL-20260926-01) and one L-R3SELF per dispatch pass; T14 exactly one L-FOLSPT naming 'ROEHold'; T10 one L-BARE per pass; ZERO L-FAW for another task. | HIGH | A missing, duplicated or misplaced line = STOP. | |
| P9 | THE VENDOR SCRIPT SPEAKS (the MEMBERS' consoles at level 4; printDebug relays at [4], as G1-2 saw): per member per issued vertex "VRF console [4] <member> (VRF_UUID:<its derived uuid>): Navigate-to: pathQuery=MAK_ROAD", "...: Navigate-to: obstacleQuery=MAK_OBSTACLE" and "...: Navigation: Initializing" (navigate-to-location.lua :65-66, :127) - the obstacle query bound and init() read the destination (sec 1(s)); ZERO "Init of aggregate move along.", "starting move-along." and "starting pa_move_along_route" (the PA script is gone). The counts per member and any "Reinitializing task due to movement of target waypoint" (:143) RECORDED. | MEDIUM (the three lines per member); HIGH (ZERO obstacleQuery other than MAK_OBSTACLE; ZERO PA script lines) | Fewer lines = recorded MEDIUM miss. A relayed "Navigate-to: obstacleQuery=NONE" (the obstacle query never reached the script, :46-57) = STOP: the run would not test the ruled mechanism. A PA script line = STOP. | |
| P10 | MOVEMENT: each moving container's own fix displaced > 50 m after its L-DISP (T02, T10, T14); members displaced > 50 m: 28ID 1 of 1, 1-112 IN 5 of 5, 48 IBCT 17 of 17 - the far-shore member included (sec 1(i)). | HIGH | Fewer = STOP (named outcomes recorded). | |
| P10a | EVERY MEMBER AT EVERY VERTEX: each T14 and T10 member comes within 50 m of each of its task's vertices, in order (the trace), inside the window (a last vertex the window closed on first is RECORDED). | MEDIUM | Recorded MEDIUM miss, naming the member and the vertex. | |
| P10b | THE CONTAINER IS ITS MEMBERS' CENTROID - G1-2's: at the last common trace instant the container's own fix lies within 25 m of its members' unweighted mean, for each of the three. | MEDIUM | Recorded MEDIUM miss. | |
| P11 | THE FAR-SHORE MEMBER: 48_IBCT/28ID__FRIENDL.INF1RIF2, across lake 197345448 from the origin, reaches T14's vertex 1 (its L-MSTEP COMPLETED) with NO trace fix more than 25 m inside an OSM water feature; its path RECORDED (the side of the lake it passed, its nearest approach, when it reached vertex 1 against the others). | HIGH | A fix more than 25 m inside OSM water = STOP and ask: the planner or the aggregate model crossed water that MAK_OBSTACLE covers (featureconfig.txt :405, :413-414, :103). A rest a few metres inside the polygon's edge is not this (G1-2's INF1RIF2 stood 4.2 m inside it on the vendor's feature stop - S6). | |
| P12 | COMPLETION PER THE TIME RULES (RL-20260921-09), each mover EXACTLY ONE path: T14 AND T10 - the members reach the last vertex, the arrival evidence fires: L-ARRIVE with 1/1 and the ONE POSITION suffix, then one TASKCMPLT; T02 closes on exactly one O2 branch: (a) L-ARRIVE, then TASKCMPLT; (b) S8 - the vertex FAILED and one TASKABRT carrying "PLANNED MOVE 28ID__FRIENDLY_INFANTRY_DIVISION vertex 1 of 1: navigate-to-location FAILED for 1 of 1 member(s) and succeeded for none - by the vendor's Move (Group) rule ..." (:9876-9880); (c) S5 / S6 - L-STALL (ONE POSITION), then one TASKABRT "STALLED (C16 progress watchdog) ...". WHOSE POSITION (sec 1(p)): every judge reads the container's own fix, "1/1" that one position. ZERO L-CANNOT; ZERO VACUOUS; exactly one terminal report per task (5 in all). | HIGH | T14 or T10 not closing on L-ARRIVE + TASKCMPLT (a STALL or a FAILED instead), two paths, a TASKCMPLT before start + Duration for an unarrived mover, an arrival without the ONE POSITION suffix or with a total other than 1, a T02 terminal report outside (a)-(c), or a mover with no terminal report = STOP. | |
| P12b | WHICH PATH - G1-2's: T10 and T14 late (b): L-TIMED OVERDUE at their end time (450 / 300 SIM s), then L-ARRIVE, L-OVERDUE-ARR and TASKCMPLT "arrived after its task's end time - complete on arrival"; T02 OVERDUE at 300 s, then its branch. | MEDIUM | Another path = recorded MEDIUM miss. | |
| P12c | THE ARRIVAL FOLLOWS THE LAST VERTEX'S ISSUE: T10's and T14's L-ARRIVE come after their last vertex's L-OUT (T10's vertex 2 lies 516 m from its last vertex, outside the 500 m radius, so members parked there do not trigger it). | MEDIUM | An earlier L-ARRIVE = recorded MEDIUM miss (the rule is honoured; the chain goes on and its last step is swallowed). | |
| P13 | T14's REST: T14's container comes within 100 m of its destination inside the window (every member is sent to the destination itself; G1-2's 100-250 m far-shore pull no longer applies). | MEDIUM | Recorded MEDIUM miss, with P10a read. | |
| P13b | T10's REST - G1-2's: the container comes within 100 m of PassagePoint_48_IBCT_SLOT0. | MEDIUM | Recorded MEDIUM miss. | |
| P14 | T02 AND THE RIVER: 28ID's member never crosses river 8011072 more than 15 m from a drivable road bridge (OsmFeatures.cs IsRoadBridge :416-424) and then drives on 50 m or more; which O2 branch it takes RECORDED. | HIGH | Such a crossing = STOP and ask (the aggregate model crossed MAK_WATERWAY off a bridge). | |
| P15 | D-6 (RL-20260927-04), IN TWO LIMBS - G1-2's, scored on every D-6 line that prints: (i) a completion with the container's fix within 100 m of the route end takes the hand-on line; (ii) one more than 100 m short is WITHHELD and never closes the task by itself (only a later L-ARRIVE or the time rules and the watchdog). Expected: NOT EXERCISED for all three - T14 and T10 arrive first and their last step is swallowed (:3757-3760); a FAILED step skips D-6 by design (:3754-3755). A limb that does not fire is NOT EXERCISED, not a miss. | HIGH | A hand-on beyond 100 m, a WITHHELD completion within 100 m, or a TASKCMPLT after a WITHHELD line with no L-ARRIVE between = STOP. | |
| P16 | VENDOR COMPLETIONS: ZERO "VRF task complete: <container> / ..." - no container runs a vendor task under Auto; the members' own navigate-to-location completions are consumed by the step (:9693) and never reported as a container's; ZERO "attribution anomaly"; after an arrival, the last vertex's aggregated completion takes L-SWALLOW "VRF completion for <c> after the arrival-evidence report of task <T> - swallowed." (:3758-3759) - once for T10 and once for T14 (and for T02 on branch (a)) when their L-VLAST falls inside the window. | MEDIUM | Another count = recorded MEDIUM miss. | |
| P17 | REPORT HYGIENE - G1-2's: ZERO SUPPRESSED; ZERO `task=(none)`; every SENT line's taskee one of the three performers' C2SIM uuids (ZERO member TaskStatus); capture and log agree; "Reports this run: ... 0 FAILED"; ObservationReports RECORDED against 20 = 7 init PROXY + 3 re-announcements + 2 STP-866 + 8 pre-flight. | HIGH | Any limb = STOP. | |
| P18 | NO ENGAGEMENT - G1-2's: zero "Fire Weapon" / FireAtTarget lines. | HIGH | Any = STOP. | |
| P19 | TILE READS: the run's tile total reads 0 HTTP FETCH(es), 0 given up, 0 undecodable; the cache manifest (27c2117e, 520 files) and the road-layer manifest (213bae20, 41 files) unchanged after the pre-warm and after the run. Hits RECORDED - expected 79 = G1-2's 49 + the 30 distinct osm-highways tiles WarmOsm reads (sec 1(q)). | HIGH | A fetch or a changed manifest = STOP (a road tile missing from the staged 41 would be fetched here - Vrf:PreflightOffline is false). | |
| P20 | RUN HEALTH - G1-2's: no BACK END LOST, no "Tick phase ... FAILED", no NO PERFORMING UNIT, no WS-runaway exit 6, no new .dmp / .callstack.log for this back-end pid (names only); VrfC2SimApp exits 0; rtiexec, rtiForwarder, rtiAssistant and every holder untouched. The raw "No creator found" count benign (O6). | HIGH | Any limb = VOID. | |
| P21 | TEARDOWN - G1-2's: StopVrf52 exit 0 or 6; no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left. | HIGH | Exit 3, 5 or 7 = STOP. | |
| P22 | CLOCK AND TIMINGS, RECORDED - G1-2's (every SIM/WALL RATIO line; each L-DISP SIMULATION stamp; each population's L-POP -> L-PUB span; each mover's dispatch -> L-ARRIVE / its terminal report in SIM s against sec 1(j); the holds' served figures) and, per vertex, the trace instants of the first and the last member within 50 m and the planning pause after each L-OUT (O12). | RECORDED | - | |
| P23 | THE LOADS, RECORDED - G1-2's: both runs' Stage 3 READY and "scenario LOAD CONFIRMED", the oracle-gate wait, READY TO TASK, the init L-PLACE counts, every L-PUB time, the sim cache listing before W, after W and after E. Expectation (MEDIUM): the scored run's init L-PLACE "36 of 36 create altitude(s) came from the TERRAIN QUERY" and no new layer directory; the pre-warm's L-PLACE RECORDED as O8. | MEDIUM | Recorded MEDIUM miss. | |
| P-FALS | THE FALSIFIERS - any one = STOP and ask, before anything else is run: (a) a SILENT stop of a populated container - G1-2's (360 SIM s or more short of its destination with NO L-STALL, NO L-D6, NO refusal, while the watchdog was judging); (b) a container's MOVE task "completing" (a vendor completion, L-D6 or TASKCMPLT) within 60 SIM s of its dispatch with its centroid displaced 50 m or less (a TASKABRT on a FAILED vertex is S8, not this); (c) ANY delete of a container or member during the run; (d) ANY TaskStatus for a member; NEW (e) WATER CROSSED - a member's trace fix more than 25 m inside an OSM water feature, or a river crossing more than 15 m from any drivable road bridge after which the member drove on 50 m; (f) NEVER BOTH broken - a container with two vertices OUTSTANDING at once, or vertex k+1 issued before vertex k's L-VDONE; (g) THE LITERAL ROUTE BACK - a CreateRoute or PA_Move_Along_Route for a container under Auto; (h) THE OBSTACLE QUERY LOST - a relayed "Navigate-to: obstacleQuery=" other than MAK_OBSTACLE. | HIGH | - | |

OBSERVATIONS - with branches, not HIGH; each RECORDED with its lines and trace measures:
- O1 CONVERGENCE AT EACH VERTEX: under Auto every member is sent to the vertex itself (sec 1(h)), so the 17 members of 48
  IBCT (and the 5 of 1-112 IN) converge on ONE point at each vertex; aggregate-level scenarios do not support collision
  avoidance and footprint overlap lowers only MAXIMUM speed (UG52 27.1.4) - the vendor's mutual-speed control. RECORDED per
  vertex from the trace: the first and the last member within 50 m, the wait between them, the SPREAD when the last one
  arrived (the largest member distance from the vertex and the largest pairwise distance), the container's fix distance at
  the step's close (the L-VDONE / L-VLAST "the unit is <d> m from it"), and the longest interval the container's fix moved
  < 50 m. Branches: (a) the members stack on the point (spread < 25 m) and the step closes; (b) an OVERLAP SLOWDOWN - the
  last members slow near the point (the last member's final 200 m speed against its own median), the step still closing;
  (c) a member that never ends its move at the point - the step never closes (S9).
- O2 T02 AT THE RIVER (sec 1(t)): (a) PLANNED ROUND THE RIVER AND ARRIVED - a path over a drivable road bridge (the nearest
  cached one 3.74 km north-west of the straight-line crossing), arrival evidence, TASKCMPLT; (b) "Could not compute path"
  relayed from 28ID__FRIENDLY_INFANTRY_DI.HQ1 - the step FAILED, the vertex FAILED, TASKABRT with the planned reason (S8);
  (c) STILL HELD AT THE WATER - the member at a bank ("Movement constrained by features"), no completion, the watchdog's
  STALL + TASKABRT (S5, S6). RECORDED: the relayed console lines, the path, any crossing point and its bridge distance (P14),
  the time to the terminal report.
- O3 1-112 IN ON T10 (three vertices, every leg NEAR; the ordinary case): the per-vertex timings, the waits, the arrival.
- O4 THE CONTAINER'S FIX WHILE MOVING (G1-2 O4 and UNEXPLAINED 3, carried): the container's fix against its members'
  unweighted mean at every common instant after dispatch - max, mean, at rest (G1-2: 432.3 / 76.3 / 132.5 m max, 0.0 m at
  rest); it is the position every judge reads (sec 1(p)).
- O5 A MEMBER'S OWN TASKSTATUS: none (P17 scores it HIGH); RECORDED as the count of SENT lines whose taskee is not a
  performer.
- O6 THE RAW "No creator found" COUNT (vendor SDK text in the app's stdout; G1-2 18, one of them for "pa_move_along_route"):
  benign, counted; a navigate-to-location twin RECORDED if it appears.
- O7 COMPLETIONS BY MARKING: a member's completion carries its marking = its exact C1c name, resolved to its container by
  _containers.IsMember (:9689-9693); COUNTED: member completions consumed by a step (= the L-MSTEP / L-MLAST lines), 0
  container completions, 0 L-ID-MARK, 0 "no C2SIM uuid known".
- O8 THE PRE-WARM'S FIRST TERRAIN QUERY (G1-2 P23 MEDIUM miss, N3, UNEXPLAINED 2, carried): whether the pre-warm's init takes
  its altitudes from the FALLBACK again with a warm sim cache - RECORDED.
- O9 THE HOLDS' SERVED FIGURE (G1-2 UNEXPLAINED 4, carried: 330 of 300 s): RECORDED from the L-TIMED lines.
- O10 THE CONSOLE / APP CLOCK OFFSET (G1-2 UNEXPLAINED 5, carried: the relayed vendor stamps led the app's SIM clock by 0-21
  SIM s): RECORDED; every comparison stays anchored on the app clock.
- O11 THE PATHS: per member, the track against its leg line, against the -2 hamlet footprints where G1-2's Mech COs stopped
  (OSM 857519447, 857519777 - the nearest approach), and against the roads (how much of the track runs along a staged
  vehicle road); every stationary run away from the vertices with the obstacle classes at its point (g1_3_score.py).
- O12 THE PLANNING LATENCY: per vertex, from L-OUT to the first member moving (navigate-to-location halts a member while its
  path computes, navigate-to-location.lua :113-118, :163-171). A stall verdict during a plan or a wait would be S5
  unpredicted, and a MISS of P12 for T14 or T10.

NAMED STOP-WITH-REPORT BRANCHES - recorded with their lines, the run continues (a branch on T14 or T10 is also a miss of the
row it contradicts) - G1-2's S1-S7, and S8-S10:
- S1 BRIDGE REFUSAL, S2 POPULATION TIMED OUT, S3 CREATES LOST, S4 ROW REFUSED - G1-2's, unchanged.
- S5 STALL WATCHDOG: L-STALL + TASKABRT "STALLED ..." on a container that stops (predicted only as T02's O2(c)).
- S6 WATER STOP: a container or a member held at OSM water with the pre-flight's report on the bus (predicted only as T02's
  O2(c)); a member resting a few metres inside a water polygon's edge on "Movement constrained by features" is S6, not P-FALS (e).
- S7 UUID NOT HONOURED - G1-2's full pattern; in the pre-warm a STOP before E.
- S8 PLANNED VERTEX FAILED: every member of a vertex FAILED (a relayed failure line, sec 2) -> L-MLAST "... 0 succeeded, M
  failed -> the vertex FAILED" -> L-VFAIL (WARN) -> one TASKABRT carrying the reason (the vendor's rule named); nothing more is
  issued and the task's follow-ons are abandoned. Predicted only as T02's O2(b); on T14 or T10 it is a P7b / P7c miss.
- S9 THE COMPLETION NEVER ARRIVES: the members reach vertex k and stop there, and the step never closes (fewer than M member
  lines) - the vertex stays OUTSTANDING and nothing issues k+1. THE TIMEOUT THAT CATCHES IT: there is no per-vertex timeout;
  the time rules make the mover OVERDUE at its end time and the stall watchdog reports STALL + TASKABRT 360 SIM s after the
  container's fix stops (for the LAST vertex the arrival evidence closes the task anyway, and S9 shows only as a missing
  L-VLAST and L-SWALLOW). Its two readings: (i) the destination never reached the script (a script error relayed, no
  "Navigation: Initializing", the members going elsewhere or nowhere - W-DEST); (ii) the members' completions never reach the
  interface, or arrive under another task type - which the INFO log cannot tell apart (a member report that is not consumed
  stays at Debug, :9694-9698). In the pre-warm: W-DONE FAIL = STOP before E.
- S10 ROAD LAYER UNKNOWN: an L-ROADLAYER with "not" above 0, or an L-ROAD reading UNKNOWN or FAR -> NONE: the AUTO decision
  fell to NONE; in the pre-warm a STOP before E (P0 (vi)), in E a P7a miss.

WHY THE NEW HIGH ROWS ARE HIGH - the mechanism is the vendor's own and documented (UG52 35.5.11 p735; the scripts and
roleNode.lua read, sec VENDOR CITATION), M3's glue is pinned offline (--planned-move-selftest 72 on the deployed exe, the
(m9) table on the deployed cache, --rulings-selftest d10, --populate-selftest p11 [A: the deploy lane's record]), and every
cut-A leg decides NEAR on the staged road layer (sec 1(q), two independent readings). What no offline test can reach, and
what the pre-warm asks first: the vendor planning round buildings and water for aggregate units on the streamed MAK Earth
Aggregate terrain, and the members' completions coming back.

STOP RULES - G1-2's:
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P20 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- P-FALS met, or P11 / P14's HIGH limb missed: STOP and ask, before anything else is run.
- The executor never intervenes in a window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as IRONSTORM_AGG_G1-<date>-4, with new appNumbers.

ONE VARIABLE: the container's move task - Vrf:AggregateMovePlanner=Auto as shipped (RL-20260928-03), per member
navigate-to-location per STP vertex, where G1-2 ran PA_Move_Along_Route. G1-2 (20260928T142731Z) is the control: the same
order, fixture, init, composition, settings, sequence and harness geometry. Differences named, not intended: D2b's guard
and its two lines (P1, P1a); the type map's header note (no row); the road tiles staged into the cache (P19's manifest);
branch (b)'s new holder on a RtiProbe tree rebuilt at the pin (G1-2 ran branch (a) on the superseded tree).

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5255 ***` in the main checkout's working tree [V 18:13Z and 18:26Z; the file
is identical to the committed one at fde3ff2 and at abe7cdf]. NOTHING is claimed by this registration. At the go-live, from the marker M read then
(5255 unless another run has moved it; the layout shifts with it), HOLDER BRANCH (b), the holder's own claim first:
- 5255-5258: the NEW persistent holder (step D), HAND-CLAIMED in Appendix B BEFORE it joins by claim_holder_g1_3.ps1 -From
  5255 (lane G1's claim_holder.ps1 form, applied by the seat to the main checkout's working tree and committed on this
  branch with the Result); marker -> 5259. Expected: 5255 CONSUMED on a first-attempt join; 5256-5258 BURNED.
- 5259-5269: the PRE-WARM runner block (step W), written by the runner at its Stage 2 - 5259 back end, 5260 front end
  (BURNED, --no-gui), 5261 WatchVrf pre-check, 5262 WatchVrf trace, 5263 VrfC2SimApp, 5264 RtiProbe 2c, 5265 CreateOne
  (BURNED unless the oracle gate fails), 5266-5269 Stage 2h holder attempts (5266 JOINS; 5267-5269 BURNED); marker -> 5270.
- 5270-5280: the SCORED runner block (step E), same layout - 5270 back end ... 5274 VrfC2SimApp ... 5277-5280 Stage 2h;
  marker -> 5281.
The layout is the runner's (block M..M+10, marker -> M+11; RunC2SimScenario.ps1 Stage 2 - unchanged 699552c..fde3ff2, whose
runner changes are the defaults, the clientId and help text [V]). A launch that aborts burns its whole block. Never reuse a
number. PushInit / PushOrder / ListenReports / StopIface are C2SIM clients, not federates.

## 6. Harvest (after the runs, read-only) and where results go

From each run directory (runs\launch52\last-run-dir.txt): vrfc2simapp.log, reports-captured.log, c2sim-bus.log, the
manifest, watchvrf-trace.csv, thread-samples.csv, holder logs, stopvrf and launchvrf logs, the wrapper log; g1_3_score.py
<runDir> (score_<run>.txt; the pre-warm's with --wgate, then in full as an observation); `g1_3_score.py --expected` for the
expected identity lines, road lines and vertex lines; lane G1's harness outputs (g1_predictions_h0.json) for the slots;
simcache listings. Vendor sim logs (runs\*\vendor) dump the environment in cleartext: count-grep only, never quoted or
attached; the app log is ours and may be quoted. Results go to: the Result block below (measurement and implication in
separate sentences); PLAN_MOVEMENT_2026-09-27.md rows G1-3 and M3 (live), sec 2's G1-3 row and sec 5 (the open questions
G1-3 answers); RUNBOOK sec 11o ("OFFLINE-PROVEN ONLY" -> what was seen); FINDING_AGGREGATE_MOVEMENT_OBSTACLES sec 6 (STILL
UNKNOWN -> what the run settled); Appendix B annotated from the manifests. ASCII + CRLF.

## 7. What this run does NOT claim

- G1-2's list, unchanged: the map display (--no-gui); the authored variant, the derived SMS, the seven authored US types
  (G1b); nested containers (M3 refuses a nested container under a per-member planner, :3570-3587, and cut A has none); the
  STP TO precedence; a patrol or a point move; combat, posture, supply or any warfare-model outcome; anything about the
  entity-level profile, the full 23-task order, the demo server or the demo profile; the sim/wall ratio of the aggregate
  profile in general; that 17 members converging on one point is how a doctrinal brigade moves (O1 observes it).
- THE NONE BRANCH IS UNTESTED on cut A: every cut-A leg decides NEAR (sec 1(q)), so pathQuery NONE - the off-road frame's
  variant (FINDING sec 5 item 2, sec 6 dissent line) - and the FAR and UNKNOWN decisions are not exercised live (UNKNOWN
  only as the pre-warm STOP, S10).
- MOVE (GROUP) ON OUR GENERIC CONTAINERS IS UNTESTED: Auto does not use it (Group and GroupOffRoad do); whether
  group_movement_simplified runs on a container the interface creates, which carries no systems, stays open
  (PlannedMoveSelfTest (m7): "not yet seen live").
- HIGHWAY=TRACK COUNTS AS A ROAD in the vendor's filter: osm.roads.model.xml's vehicle-road filter drops only path,
  footway, bridleway, steps, busway, via_ferrata, pedestrian, cycleway and raceway (OsmVendor.RoadModelSkippedHighways,
  OsmFeatures.cs :434-451) and featureconfig.txt :189 classes a track as MAK_ROAD_VEHICULAR_TRAIL - so a NEAR decision can
  rest on a farm track, and 5 of the 6 cut-A legs do (sec 1(q)). A path that follows a track is the vendor's road plan, not a
  claim that the track is a good road.
- THE DESTINATION BINDING is offline-proven only (sec 1(s)): whether the facade's "location" reaches navigate-to-location's
  destination - a LOCATIONREFERENCE (navigate-to-location.xml :34-35; the "locationwithoutaltitude" is Move (Group)'s, which
  Auto does not run) - is the first thing the pre-warm shows (W-DEST, W-DONE, P9); its falsifier and the timeout that
  catches it are S9.
- Rivers solved: T02's river is OBSERVED (O2) - one river, one member, one cached bridge 3.7 km away.
- The members' completions reaching the interface is first seen live here (S9 names what it would look like if not).
- Timing: n = 1 - one host, one fixture, one run; no timing figure generalises.
- The uuid mechanism for a PLATFORM, a synthesized sub-unit and a template re-create: G1-2's not-claimed item, unchanged.

## Result (written after the harvest, never from a live read)

RESULT 2026-09-28: STOPPED AT THE W GATE - NO SCORED RUN. The pre-warm (run 20260928T190047Z_run, 19:00:46-19:07:59Z,
runner exit 0) scored "W GATE: FAIL (branch A; M10, M12, W-DEST) - STOP before E" (g1_3_score.py --wgate; scratch
u3\laneG1-3\W_gate_20260928T190047Z_run.txt). E was not launched: P0-P23 are NOT SCORED and the scored block 5270-5280 was
never claimed. Independently of the gate, the pre-warm's back end CRASHED 2.9 s after the first planned dispatch (N2). By
the stop rules (sec 4) nothing is patched or re-run under this registration; a successor is IRONSTORM_AGG_G1-2026-09-28-4 or
later, with new numbers. Marks: [V] = checked by lane G1-3 after the run (the run directory, the vendor script, the vendor
headers, the vendor's own saved scenario); [A] = the seat's record or an inference.

THE SEQUENCE AS RUN (by the seat under RL-20260928-01) [A unless marked]:
- A, 18:46:19Z: 1 check FAILED - 16 MSBuild.exe processes started 18:43:07Z, idle build nodes of the STP session (not a
  federation participant, not this lane's), left to time out; A again at 18:59:01Z: 0 FAILED (u3\laneG1-3\A_preholder.txt,
  A_preholder2.txt).
- C3, 18:46:26-18:47:23Z: PushInit exit 0 ("QUERYINIT : 40 Units"), PushOrder exit 0, one "ORDER (69670 chars)" echo
  (C3_stdout.txt, c3_pushinit.txt, c3_pushorder.txt) - as registered.
- D: the claim 5255-5258 applied to the main checkout's Appendix B before the join (marker 5255 -> 5259); RtiProbe pid
  56380 "HOLDER JOINED: pid 56380 appNo 5255, holding 28800 s" at 19:00:02Z on the first attempt [V:
  runs/launch52/g1-3-holder-20260928T185943Z.log] - up to about 03:00Z 2026-09-29, kept for the successor.
- W: the prelaunch check at 19:00:23Z 0 FAILED (W_prelaunch.txt); the sim cache listed at 18:59:56Z; the dry run 19:00:24Z
  exit 0; the pre-warm at 19:00:46Z [V: its runner log and run directory]: holder 56380 recognised PERSISTENT, block
  5259-5269 claimed (marker 5259 -> 5270), Stage 2h holder 5266 (pid 31660) joined in 3 s, back end 5259 (vrfSimHLA1516e pid
  3344), the oracle gate passed (72 real-coordinate POS lines, 36 uuids), the order on the bus at 19:04:51.4Z, the app on
  5263 (pid 26024, exit 0, a clean resign), the 120 s window ran to its cap (about 19:05:19-19:07:21Z), StopVrf exit 0
  ("VR-Forces 5.2d is down (graceful; nothing was killed)"), the teardown ran.

WHAT HELD [V, the W gate output]: W0-W7 - the three populations attached and published 1 / 5 / 17 of N; IDENTITY branch A,
I1-I17 - 59 of 59 created under the requested uuid and bound by it (C1d's regression guard, RL-20260928-02); M1 (L-M3-ON,
road layer 41 / 0 empty), M2 and M3 (both D2b lines, "allowed"), M4 (T10's ROAD LAYER: 10 readable, 0 not), M5-M8 (one
PLANNED MOVE line, 3 vertices / 5 members, kind navigate-to-location; the chain in order, never two vertices outstanding;
both OUTSTANDING lines carry the registered variables and vertices; NEAR -> MAK_ROAD at 0 m on the offline table's ways
372319246 and 367165904), M9 (each closed step consistent with the vendor's rule), M13-M17 (M13 vacuous - T14 never
ran; 0 obstacleQuery other than MAK_OBSTACLE; 0 stray, retired, refused or already-driving lines; the literal route gone; 0
report-only lines, 0 splices), the water falsifiers. T14 and T02 were never dispatched: their predecessors' 300 SIM s
holds had not run out when the back end stopped (N2).
WHAT FAILED: M10 - two member FAILED lines at vertex 2 (L10211, L10213); M12 - two relayed "Cound not create route"
(L10199, L10201); W-DEST - T10's container came 0 m closer to vertex 1 and all five members were displaced 0.0 m. W-DONE:
NOT REACHED.

N1 - WHAT THE FIVE T10 MEMBERS DID (app log L9608-L10213 against navigate-to-location.lua, read line by line) [V]:
(a) Vertex 1 went to all five (L9617) with the registered variables. On each member the vendor's script controller began
    navigate-to-location and echoed its parameters at console level 3 (L9681-L9713): `destination={3448198.452440,
    1485421.208769, 5138688.441927}; obstacleQuery=MAK_OBSTACLE; pathQuery=MAK_ROAD; buffer=10; displayRoute=False;
    query=""` - that vector is (54.029734, 23.305499) at ellipsoid height 0 to 0.000 m, the registered vertex 1 (vertex 2's
    echo, L10119-L10151, is (54.024000, 23.313000) to 0.000 m). The independent observer recorded the same five echoes. The
    script then printed "Navigate-to: pathQuery=MAK_ROAD", "Navigate-to: obstacleQuery=MAK_OBSTACLE" (our variable: without
    it, and with query="", the script prints NONE, :46-57) and "Navigation: Initializing" (:127).
(b) Within 0.7 SIM s each member started a move-along SUBTASK on base-system.aggregated-movement.aggregated-move-along-
    controller, route "<member> Pathr" (L9809-L9865), and that controller refused it at once: `Warning:
    DtAggregatedMoveAlongController::setupRoute -- %1 route does not exist. | 1-112_IN/28ID__FRIENDLY_I.RIF2 Pathr`; the
    subtask Failed, 5 of 5 (L9813-L9875). The script starts that subtask only after its path job returned at least two
    points and createRoute returned a route that passed its own validity check (:177-186, :213-246).
(c) Each member's navigate-to-location then ended Completed (L9915-L9971): the script ends with SUCCESS whenever its last
    move-along subtask stops, whatever that subtask's result (:252-259). "5 succeeded, 0 failed -> the vertex SUCCEEDED"
    (L9977) therefore counts five moves that never started, and the interface - by design, an intermediate vertex advances
    either way (VertexChain.cs :359, :381-384) - closed vertex 1 with the unit "1451 m from it and moved 0 m since dispatch
    (VACUOUS by the vertex bar - R11)" (L9979) and issued vertex 2 (L10021).
(d) Vertex 2: the same echo and debug lines on all five; HQ1 and RIF2 relayed "Cound not create route" (:230-233: createRoute
    returned no valid route for a path of at least two points) and FAILED (L10211, L10213); RIF1, RIF3 and WPN1 had not
    returned from planning (:163-171) when the back end crashed.

N2 - THE BACK END CRASHED at 19:05:16.935Z, 2.9 s after T10's dispatch (L9608, 19:05:14.067Z) and about 2 s after the
vertex-2 failures [V]. C:\MAK\logs\vrfSimHLA1516e5.2d-20260928-150104-Legatus-282607-3344.callstack.log (1,506 B) and the
back end's own log were last written at that instant, the .dmp (1,175,055 B) at 19:05:17.492Z (names, sizes and times
only). The back end's CPU fell from 2.14 cores (19:05:17Z) to 0.03 (19:05:22Z) and read 0.00-0.01 to the end, working set
flat at 3,668 MB (thread-samples.csv); no console line was relayed after L10209 and the independent observer's last console
row is at t=77 s (19:05:14.9Z); the app's sim-clock reader ran at exactly 1.000 x wall (L10235) and then "could not be read
(no back end reporting)" (L10241, L10245); the observer's back-end count fell 1 -> 0 between t=192.9 and 194.9 s
(19:07:11-13Z). StopVrf's inventory named the process by its window, "Error vrfSimHLA1516e.exe" - the dialog of the
2026-09-21 D5b crash (RUNBOOK, STP-854) - and its taskkill without /F closed it; the process was gone by 19:07:58Z. By
count-grep only (the vendor-log rule of this lane's brief) the 36-line callstack carries 0xC0000005 once, "Lua" / "lua" on
6 / 7 lines, StateData once, and none of route, navigat, AreaCollector, addGeometry, FeatureSet, NameGenerator, MoveAlong,
isDestroyed or DamageActuator; its
content was not read - that is the owner's call, as it was for D5b's callstack (RL-20260921-02 item 4). Neither the runner
("RUN COMPLETE") nor the app named the crash, and the whole 120 s observation window ran after it. Had the W gate carried
P20's crash limb it would have failed on it too.

THE SEAT'S READING, TESTED - "the destination did not reach the script; a path of fewer than 2 points ends the task as a
success; createRoute on an empty point list gives 'Cound not create route'" - REFUTED on four counts [V]: (1) the vendor's
own parameter echo carries the vertex exactly (N1 (a)); (2) a path of fewer than two points ends the task FAILED, printing
"Could not compute path - <message>" or "Invalid path computed" (:177-185) - 0 of either in the app log and in the back
end's own log, and the vertex-1 tasks ended Completed; (3) the failed move-along subtask is started only after a path of at
least two points and a valid route (:213-246); (4) "Cound not create route" lies inside the loop that runs only while at
least two points remain (:213-235) - never on an empty list. THE DESTINATION BINDING (sec 7) IS SEEN LIVE: the facade's
"location" reaches navigate-to-location's LOCATIONREFERENCE as the exact vertex and the script plans with it; the vendor's
own saved navigate-to-location task carries the same value class, a DtRwVector destination (RoadToKaunasPhaseTwo.oob
:72084-72085 [V, lane m3b's extract of the vendor's .scnx]).

THE NAMED COMPETITORS [V]:
- ALTITUDE 0 REJECTED - EXCLUDED: with the destination at ellipsoid height 0 the path job returned at least two points on
  all five members at vertex 1 and on HQ1 and RIF2 at vertex 2; the script itself sets every path point to altitude 0
  before createRoute (:195-199).
- NO ROAD PATH FROM THE START POSITION - EXCLUDED wherever a job returned: "no path" is the fewer-than-two-points branch,
  printed and FAILED (:177-185), 0 lines. Undecided for RIF1, RIF3 and WPN1 at vertex 2 (no return before N2).
- THE TASK REFUSED BY THE MODEL SET - EXCLUDED: the back end's script controller ran navigate-to-location on each member,
  the script's load section and init() ran (the three debug lines), and its move-along subtask went to the aggregate model
  set's own controller. The client-side "Can't create data of type navigate-to-location. No creator found." (L9615, our
  process's vendor SDK) is the twin of G1-2's pa_move_along_route line - 18 such lines in both runs, the same task types
  but that one (O6) - and PA_Move_Along_Route ran in G1-2 beside its twin.

THE READING THIS LANE SETTLES ON: THE PLANNER WORKED AND THE EXECUTOR DID NOT [V]. On each T10 member the destination
arrived, a path of at least two points was planned (pathQuery MAK_ROAD; its points are not printed - DEBUG_DETAIL is false,
:17) and a route was created, then the aggregate model's move-along controller could not find that route, so no member
moved; navigate-to-location reports that as a success by its own code, the vendor's Move (Group)
rule counts it (Move_To_Location_Plan_Path.lua :50-52 passes the same result up), and the interface advanced past a vacuous
vertex. WHY the controller could not find the route is NOT settled by this run [A]:
 (i) THE ROUTE'S NAME - the lead. navigate-to-location names its route this:getName() .. " Path part N" (:223): 42
     characters on our 30-character member names (C1c), "1-112_IN/28ID__FRIENDLY_I.RIF2 Path part 1". The back end calls
     it "1-112_IN/28ID__FRIENDLY_I.RIF2 Pathr" (36 characters, no hidden byte) in 20 of 20 mentions, in its own log and in
     ours, and "Path part" 0 times. VR-Forces documents a per-type maximum name length and a generator that makes a unique
     name within it (vrfNameGenerator.h :38-53; simObjectNetInterface.h :202-205), as C1c measured for aggregate names, and
     this controller "looks up the route by name" (aggregatedMoveAlongController.h :70-77). In the vendor's own Road to
     Kaunas save the same controller follows a navigate-to-location route of the short-named aggregate JAM-137: route
     "JAM-137 Path part 1", uuid "VRF_UUID:JAM-137 Path part 1_8" (the name plus a counter), publish-flag 0,
     route-is-discovered True at vertex 51, planned with our queries and query "" but buffer 0 (RoadToKaunasPhaseTwo.oob
     :71967-72137, :72195-72201, :103560-103565 [V]). A second route asking for the same capped name would also explain
     vertex 2's "Cound not create route".
 (ii) THE ROUTE IS UNPUBLISHED (displayRoute false, :225) - NOT SUFFICIENT: the vendor's followed route is unpublished too,
     and the vendor's aggregate Move to Location (Plan Along Roads) defaults displayRoute to 0 (Move_To_Location_Plan_Path.xml;
     .lua :39-45).
 (iii) A SAME-TICK RACE (the subtask starts in the tick the route is created) - WEAKENED, not excluded: the vendor script
     always does this, and the vendor save shows it working.
 WHAT SEPARATES THEM: one aggregate unit with a short name running navigate-to-location with these variables, buffer 10
 included - it moves under (i) and fails the same way under (ii) or (iii). CONTROL [V]: in G1-2 the same five members ran
 move-along on the same controller on an app-created route with a 150-character name (G1-2's run, L21495-L21513) and T10
 arrived - the controller moves these members; what differs here is the script-created route.

UNEXPLAINED - each a falsifier of the reading until explained:
- N2's crash. Its timing (2 s after two failed createRoute calls, with three planning jobs outstanding and the failed tasks'
  shutdown deleting their routes, :300-302) and the Lua frames point at the navigate-to-location path; nothing on the stack
  was read. G1 and G1-2 ran the same fixture, the same aggregate reactive Lua tasks (the same 17 client-side task types) and
  a higher peak working set (3,801 and 3,876 MB, against 3,668 here) without a crash. The vendor's own comment on a non-zero
  buffer ("DtAreaFeatureSetAdapter::AreaCollector::addGeometry does not deal correctly with large obstacle features",
  :39-40; we pass 10, the vendor save 0) is a candidate the count-grep does not support (0 frames). n = 1.
- "Pathr": the rule that turned "... Path part 1" into "... Pathr" is not in the installed headers, and the Lua and class
  references are not installed on this machine (C:\MAK\vrforces5.2d\doc\luadoc and classdoc are placeholder pages).
- Why HQ1 and RIF2, and not the other three, returned from planning first at vertex 2.
- O8, carried: the pre-warm's init took 36 of 36 create altitudes from the FALLBACK again (L355) with the sim cache warm.

RECORDED [V]: O6 - 18 "No creator found" lines, the navigate-to-location twin at L9615. O10 - the relayed console led the
app's SIM clock by about 13.7 s (dispatch at app SIM 312.3, the task's first console stamp 326.030). P22 - the populations
published 11.1 s after they began (L1088-L1092; G1-2: 1.7 s). P19 - 28 cache HITs, 0 HTTP FETCH (L10265); at 19:27Z the
cache manifest read 520 files / 27c2117e and the road layer 41 / 213bae20 - unchanged. P23 - the sim cache listing was
identical before (18:59:56Z) and after (19:26:09Z): 21,805 files, 495,598,329 bytes, newest write 10:20:57Z
(u3\laneG1-3\simcache_before_prewarm.txt, simcache_after_prewarm.txt) - the pre-warm added nothing. The shutdown cleanup
dispatched 64 deletes (the registered 36 + 23 + 5 areas, no route object) to a back end that had crashed. Reports: 594
delivered, 0 FAILED; three TASKSTRT (T01, T13, T10), no terminal report. The WS-runaway alert at 19:02:11Z (857 MB/min,
2,143 MB) is the load-time alert both G1-2 runs also raised (746 and 1,045 MB/min) without a crash.

WHAT IT MEANS (implication, not measurement): the vendor's success bit for navigate-to-location means "its move-along
stopped", not "arrived" (:252-259), so a step that closes VACUOUS is a move that did not happen, and M3 advances past it by
design. The W gate did its job - it stopped before E - but two of its labels over-claimed: W-DEST ("the destination reached
the script") failed with the destination reached, and M9 checks the bookkeeping, not the move. The planner of RL-20260928-03
is not refuted: its planning ran on the aggregate model set; its execution on our members did not.

NUMBERS: 5255 CONSUMED (holder 56380), 5256-5258 BURNED; pre-warm block - 5259 (back end, pid 3344, crashed, N2), 5261
(WatchVrf pre-check), 5262 (WatchVrf trace), 5263 (VrfC2SimApp, pid 26024, exit 0), 5264 (Stage 2c RtiProbe), 5266 (Stage 2h
holder attempt 1, pid 31660) CONSUMED; 5260 (--no-gui), 5265 (oracle gate passed), 5267-5269 BURNED; marker 5270. The
scored block 5270-5280 was never claimed. A successor takes new numbers from 5270: with holder 56380 still up (to about
03:00Z 2026-09-29), pre-warm 5270-5280, scored 5281-5291, marker -> 5292; after it resigns, a holder claim first - holder
5270-5273, pre-warm 5274-5284, scored 5285-5295, marker -> 5296.

NEXT, in order: (1) RULE - the owner decides whether pid 3344's callstack is read (the D5b precedent, RL-20260921-02 item 4).
(2) PREREG - the one-variable probe of (i): one short-named aggregate unit, these navigate-to-location variables. (3) M3's
design - an intermediate vertex that closes VACUOUS is not a success. (4) The harness - a back-end crash (crash artefacts
for its pid, the "Error vrfSimHLA1516e.exe" window, a sim clock that stops while the process lives) ends the window as VOID.
(5) Only then a successor registration.

ADVERSARIAL REVIEW: the strongest competitor to the settled reading (the planner worked, the executor did not) was the
seat's (the destination never bound); the vendor's parameter echo, exact to 0.000 m and seen by two observers, and the
script's own branches exclude it. The strongest competitor to the lead cause (the route's name) is a defect in the
aggregate move-along path that does not depend on names - (iii), or something unseen; the vendor save of JAM-137 weakens it,
but it is one save and not a run on this machine, and it differs from ours in the buffer (0 against 10) as well as in the
name's length, so the probe must hold the buffer at 10. The crash could be unrelated to M3 (a D5b-class vendor race);
against that stand its timing and the clean G1 and G1-2 runs, for it n = 1 and an unread stack. Verified: the log lines, the
vector conversion, the script and XML lines, the headers, the save's lines, the crash stamps, the manifests and the sim
cache listing. Assumed: that the name mismatch is what the controller trips on, and anything about the crash's frames.
