# PREREG - IRON STORM ON THE AGGREGATE PROFILE, RUN G1: the ruled cut-A order on POPULATED AGGREGATE CONTAINERS (C1) - the container mechanism's first live run (one unscored pre-warm + one scored run)

STATUS: STOPPED at P3 - scored 2026-09-28 from run 20260928T102541Z_run (Result below): the container mechanism ran end
to end with ONE member; every aggregate-level name over 31 characters comes back from VR-Forces cut to 30, which left 22
of the 23 members unattributed (28ID and 1-112 IN took branch S2, 48 IBCT branch S3). WAS: REGISTERED - LAUNCH PENDING
(seat's go-live). Registered by lane G1 (session 5fc25950; the registration commit's
own time is the stamp) on branch run/ironstorm-agg-g1 from main 1efbbe7, BEFORE any order push, holder action or launch.
PREPARATION ONLY at registration: no C2SIM push, no holder start, no VR-Forces or runner launch (two runner PREP DRY RUNS
and one holder -WhatIf, sec 3), no appNumber claimed. The one sanctioned C:\MAK write was made: the fixture deploy (sec 1
(b)). Marks: [V] = checked while writing this file; [A] = taken from the record or assumed, not re-checked here.

## Registration

PREREG ID: IRONSTORM_AGG_G1-2026-09-28-1
DATE (UTC): 2026-09-28, before any launch (the registration commit's own timestamp is authoritative; the launch waits
for the seat's go-live)
BINARY / COMMIT: the DEPLOYED main-checkout build 1.0.0+git.9027548.Release-5.2 - VrfC2SimApp.exe sha256
31be659b6c405a20f5e57be8e02f28738cab4f7d68dcdc9bb7630953999fcf08, VrfC2SimApp.dll sha256
969c82f88391811252b281e15de7918fa13809b6f7eee748d90b4b6fa82fdeda, VrfBridge.dll sha256
5198ac4585bf4218d4c6048cb6713876f9c464e415ce50ac83afcc2b9da2131a (RUNBOOK sec 9, NEW PIN 2026-09-28) in the app tree AND
the RtiProbe tree, deployed appsettings.json 0f600f0eb2a205773db5a1bfe5077aba981c3b954e0de5e4e86526683e259210 = src,
appsettings.Demo.json 12391080...f8f6ca [V, golive_checks_g1.ps1 03:11Z]. src at 9027548 = C1 (5561d90) + C1b (9027548) +
Q1a (28c5ec0) on D1/M1b (b817609), M2 (0849334), A1 (3e5ab14), M1 (47da73e). `git diff --stat 9027548 1efbbe7 -- src` is
src/VrfBridge/VrfBridge.vcxproj ONLY (the Release-5.2 RtiDir default and its comment; no managed or native source line)
[V]; `git diff --stat 1efbbe7..HEAD -- src` on this branch is EMPTY [V at the registration commit]. NOT rebuilt by this lane.
Offline on the deployed exe (5.2 PATH prefix, DOTNET_ENVIRONMENT empty, 0 Vrf__) [V]: --populate-selftest 148 PASS / 0
FAIL / 4 SKIP ("the linked VrfBridge.dll CARRIES PublishedSubordinateCount" among them), --scripted-task-selftest PASS,
--parse-order and --parse-init as sec 1(e). Gates on this branch [V]: tools/aggregate/composition_check.py --tree
--init-census --twins --vendor COMPOSITION GATE PASS and --selftest PASS; tools/aggregate/typemap_check.py TYPEMAP GATE
PASS and --selftest PASS.
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: UG52 72.2.1 p1419 (the Aggregate Container is the platform that aggregates simulation objects; warfare-
model units carry no subordinates); UG52 Table 68 p1470 (Aggregated / Disaggregated); UG52 18.1 p438 (a unit is
subordinated after it is created); UG52 27.1.4 (the unit's centre point is what the terrain reads); UG52 30.22 p598 step 5
and 30.24 p599 (with Start at Closest Vertex cleared the object first moves to the BEGINNING of the route); UG52 35.5.7
p734 (the aggregate Move Along Route is 30.24's). Vendor data [V, read-only]: AggregateLevelBase\scripts\
PA_Move_Along_Route.lua :26-91 (init(): snapshot of the subordinates, "move-along" to each warfare-model unit with
start_at_closest_point = startAtClosestVertex, "pa_move_along_route" only to a disaggregated sub-container; printDebug
"Init of aggregate move along." :28, "Sub. <name> starting move-along." :80), :95-149 (each finished subordinate is sent
"move-to-location-retrograde-task" to the route end + its offset rotated by the final-leg bearing minus the container's
initial heading; "has completed move task." :110, "has completed final move." :140), :152-157 (the task ends when every
snapshot subordinate did both); appData\settings\featureconfig.txt :261-262 and :266-267 (MAK_MECH / MAK_MOTOR RESTRICTED_L2
= FOREST OR URBAN OR MOUNTAIN; IMPASSABLE = MAK_WATERWAY OR ALPINE), :413-414 (MAK_WATERWAY includes RIVER and LAKE), :423
(MAK_WIDTH 5 m); VRFSIM.Aggregate.feature.model.xml :323-352 (the aggregate terrain loads waterway=river/canal lines as the
River layer); mech-aggregated-movement.sysdef :110-131 and motorized-aggregated-movement.sysdef :109-130 (speed-factor 0 /
0.25 / 0.65 / 1); Mech CO (USA, M2).entity :96, :439-440 (mech movement, max 11.1 / ordered 8.33 m/s); Stryker  HHT (USA)
.entity :74, :409-410 and Stryker Cavalry SQDN (USA).entity :157, :493-494 (motorized, max 26.9 / ordered 15.3 m/s).

OWN-RECORD CITATION:
- Rulings: RL-20260927-01 (movement approach; the pre-flight per model set), RL-20260927-02 (hostile side RUS; populate
  the containers, not proxies), RL-20260927-03 (every unit an EMPTY container at init at its authored position; only a
  tasked unit is populated, in place), RL-20260927-04 (D-1..D-8 as recommended, D-2 revised), RL-20260927-05 (Q1a - the
  late-predecessor gate), RL-20260927-06 (above battalion = aggregate only); kept as they are: RL-20260921-09 (the
  temporary completion position), RL-20260913-03 and RL-20260914-01 (a stuck unit = report + TASKABRT), RL-20260925-01
  (stall detection ON in the demo profile; a stuck unit's follow-ons abandoned), RL-20260926-01 (a UNIT's ATTACK is fire
  at will), RL-20260920-01 (route shift ON for any run).
- docs/PLAN_MOVEMENT_2026-09-27.md rows C1, C1b, C2, D1, M2, G1, G1b, G2, D2; docs/DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md
  (secs 1-11; sec 9 is the G1 outline this registration supersedes for the order, sec 1(a)); docs/experiments/
  AGGREGATE_PROFILE_OFFLINE_2026-09-27.md (the fixture, -ModelSet, sec 6.4 no nav data); docs/experiments/
  AGGREGATE_AUTHORED_UNITS_2026-09-27.md (the authored variant is G1b); RUNBOOK secs 9, 11, 11i, 12, 12a;
  FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27 sec 8 (the aggregate mobility table).
- IRONSTORM_CUTA_E2-2026-09-27-2 (PREREG_IRONSTORM_CUTA_E2-2_2026-09-27.md, SCORED): the SAME ruled order on EntityLevel -
  the harness template of this file and the entity-level reference (sec 4 ONE VARIABLE). IRONSTORM_CUTA_LIVE-2026-09-27-1
  (PREREG_IRONSTORM_CUTA_LIVE1_2026-09-27.md sec 3 W): the pre-warm procedure copied here.

## 0. Purpose, in plain words

G1 runs the ruled cut-A order - the order E2-2 drove on EntityLevel - on VR-Forces' AGGREGATE model set. Every Iron Storm
unit is created at init as an EMPTY Aggregate Container at its authored position (36 containers, hostile RUS). When the
order arrives, the three performers - 28ID (T01 hold, T02 advance), 1-112 IN (T10 advance) and 48 IBCT (T13 hold, T14
advance) - are populated IN PLACE with catalogue warfare-model units (1, 5 and 17 members, FLAT, on one centroid-
preserving ring), attached, and published before any of their tasks runs; each advance is the container's own vendor
script, PA_Move_Along_Route, on a route object; arrival and stall are judged on the container's own centroid (D1). THE
HEADLINE is 48 IBCT's 17-member brigade on T14: the population, the publication gate, the scripted move and the centroid
judge, all live for the first time.

WHAT THE PREPARATION FOUND, said before the run (sec 1(f)-(j); scratch harness u3\laneG1\harness on the deployed dll):
T14's and T10's lines are water-clear on the centreline under the aggregate leg rule (M2), so ARRIVAL is the prediction
for both. T02's straight leg, which no cut-A change re-routed, crosses OSM river 8011072 at 2.40 km: the aggregate rule
flags it as a RIVER CROSSING and the container's single member is predicted to STOP there - a named stop-with-report
branch (S6), not an arrival. And the 490 m ring round 48 IBCT touches two water bodies: four member slots are moved off
water, one moved slot sits on the far shore of lake 197345448 so that member's first leg (slot -> the route's first
vertex, PA_Move_Along_Route.lua :75-78 with start_at_closest_point false) runs into the lake, and one to three unpack
targets at the destination lie in lake 197345447. So T14 is predicted to close on ARRIVAL EVIDENCE (500 m, one position)
with its centroid resting about 150 m short, and the container's own vendor completion is predicted not to come inside
the window. None of this is a reason the ruled order cannot serve (sec 1(a)): the mechanism is tested on all three
containers, and the water findings are exactly the vendor behaviour the PLAN's open question asks about (PLAN sec 5:
"What a Move Along Route does at speed factor 0 (runs forever, fails, or ends) ... G1 observes").

## 1. Decisions taken from the record

(a) THE ORDER - the RULED cut-A order, data/IRONSTORM_CUTA_Order.xml sha256
7a9861372f07702fc136f91f63b8bf971650fcc91c20aa62803d73cfd869f5c7 on init 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d
7614f3d26dc93eec [V], the seat's preference. Why it serves: its (i) and (j) waypoints make T14's and T10's lines clear of
OSM water ON the centreline, which is the aggregate model's only stop (M2's leg rule; the harness: 0 flagged legs for T14
and T10, sec 1(g)), so ARRIVAL is the prediction and the MECHANISM is what is tested. The design's sec 9 outline (T13 ->
T14 on T14's ORIGINAL line, the lake branch) is the E1 variant and is G2's. Checked for a reason the ruled order cannot
serve, and none found: T02's river (sec 1(g)) and the ring's water (sec 1(i)) change what is predicted for T02 and for
T14's rest point, not whether the containers can be populated, published, tasked and judged - and T02 was never re-routed
by any cut-A change because on EntityLevel a river line is not water (RUNBOOK sec 12a table), which is why E1 and E2-2's
28ID platform reached PassagePoint_28ID_SLOT0 at 0.9 m. Re-routing T02 would be a new cut-A change and needs a ruling; it
is not made here. The five tasks (per the deployed exe's --parse-order [V]):

| Task (uuid) | performer (container) | verb -> decision | members | route (pts incl. the live origin) | start | armed end (SIM s) |
|---|---|---|---|---|---|---|
| T01 f7b52ba4 | 28ID__FRIENDLY_INFANTRY_DIVISION (DIV, Mech Infantry - NEAREST branch, PROXY) | CNFPSL -> held in place (STP-866) | 1 | not driven (4 graphics) | delay 0, after 28ID publishes | 300, no destination |
| T02 696fbb33 | 28ID (same) | ATTACK -> a UNIT's ATTACK: advance with fire at will (RL-20260926-01; TaskDispatchPolicy.cs:134) | 1 | 2: origin -> PassagePoint_28ID_SLOT0 54.028874, 23.264401 (5,341 m) | after T01's TASKCMPLT | 300, destination |
| T10 9aab7fe6 | 1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE (BN, Light Infantry - exact) | CRESRV -> bare move | 5 | 4: origin -> (h) 54.029734, 23.305499 -> (j) 54.024, 23.313 -> PassagePoint_48_IBCT_SLOT0 (2,771 m) | delay 300 SIM s (1200 s x 0.25) | 450, destination |
| T13 37677c40 | 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE (BDE - exact) | CNFPSL -> held in place | 17 | not driven | delay 0, after 48 IBCT publishes | 300, no destination |
| T14 1075b583 | 48 IBCT (same) | FOLSPT -> advance along the graphic and hold, ROEHold | 17 | 3: origin -> (i) 54.0146, 23.3315 -> 54.040348, 23.324206 (4,172 m) | after T13's TASKCMPLT | 300, destination |

(b) SETTINGS, each with its reason.
- FIXTURE IronStorm_Centre_52_Aggregate - the aggregate SMS on MAK Earth Aggregate (online), the Iron Storm AOI, FFRTC
  0.033333 s, no nav data. DEPLOYED BY THIS LANE 2026-09-28T02:57:41Z, the one sanctioned C:\MAK write (tools/FixtureGen/
  README.md): the committed tools/FixtureGen/frame_variants/IronStorm_Centre_52_Aggregate.scnx copied (no overwrite; the
  file did not exist) to C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Aggregate.scnx; sha256
  804e2c393dcf5f4fe4e1c58d7423a343c4e43250cd719d5474e035750c76a0e3, 6,540 B, `fc /b` "no differences"; a scratch REBUILD
  with the README's command is byte-identical; validate_fixture.py --empty-52 on the committed AND the deployed copy:
  ALL FIXTURES: OK (aggregate terrain on disk, 0 navData records, SMS AggregateTacticalLevel paired with it) [V]. Nothing
  else under C:\MAK was written.
- MODEL SET: `--model-set AggregateTacticalLevel` passed EXPLICITLY. The D2 automatic selector (RL-20260927-06) is NOT
  merged (PLAN row D2 "NEXT"); under it this order's tasked echelons (a division and a brigade) would select the aggregate
  set anyway. The runner then picks data/unit-type-map-52-aggregate.json (sha256 c546edbe...6190, 62 rows + 7 authoredRows,
  modelSetKey AggregateTacticalLevel [V]) and exports Vrf__ModelSet; its Stage 0 read the deployed fixture: "fixture ...
  loads AggregateTacticalLevel (aggregatetacticallevel.sms) - matches -ModelSet" [V, PREP dry runs].
- COMPOSITION: data/unit-composition-52-aggregate.json sha256 9684e945...3bae [V], variant `catalogue` - the default
  (Vrf:CompositionVariant "catalogue" in appsettings.json and the file's defaultVariant). How the app reads it
  (ContainerStartupPreflight, VrfC2SimService.cs:2839-2964): CompositionTable.Load, CompositionVariants.Select narrows it to
  ONE variant (the variant's rows plus the `all` rows), the derived-SMS guard applies only to authored content (none in
  `catalogue`), and every row is resolved against the catalogue and logged. Vrf__Scenario is NOT passed (the runner does
  not export it; RUNBOOK sec 11i): for the catalogue variant that is "UNKNOWN: logged and harmless" - the start-up line
  says "SMS: Vrf:Scenario is not set." (the harness reproduces it, sec 2). The authored variant is G1b's.
- CLIENT ID "Not Set" (the init's SystemName; the runner's Stage 0 "init SystemName [Not Set] matches app clientId").
  DURATION SCALE 0.25 (as E2-2). SUCCESSOR-GATE FLOOR 600 s (Vrf__TaskPredecessorTimeoutSeconds=600, as E2-2; the wrapper
  exports 7200 and applies --env after it).
- PRE-ORDER GATE: NONE. The runner offers exactly two stage-7d gates - NavArea, or nothing (with an optional fixed
  -PreOrderSettleSecs hold) - and no ready-to-task gate (RunC2SimScenario.ps1 :2231-2238; RunScenario.sh :276-280) [V].
  NavArea waits for a "New Primary nav area" row that the aggregate model can never print (it loads no nav data,
  AGGREGATE_PROFILE_OFFLINE sec 6.4), so it would time out and stop the run (exit 3). A fixed settle guesses; the app's
  own barrier does not: a task whose taskee is not taskable yet is HELD, not dropped (RUNBOOK sec 11g, D5b), a population
  waits for the init's creates to settle (MaterializeUnit, VrfC2SimService.cs:2628-2635), and every C1 task waits on its
  container's publication gate (:4660-4678). The order is pushed right after the oracle gate (a real POS line), as the
  PREP dry run shows ("no hold and no gate"). RECORDED: whether "ORDER BEFORE READY TO TASK" prints.
- CONSOLES --object-console 4 --member-console 4 (every container and every member console opens at level 4 at its
  ObjectCreated; the members' task-time console line re-requests it). WINDOW -StopWhenComplete with the 2,700 s cap (as
  E2-2). ENV Vrf__StallDetection=true, Vrf__StallClock=sim, Vrf__TaskPredecessorTimeoutSeconds=600. --sample-threads.
  --no-gui. SERVER the private 18080 / 61614. TEARDOWN GATE StopVrf52 exit 0 or 6.
- NOT passed (shipped values): route shift ON (RL-20260920-01), the pre-flight and its cache location, Vrf:PlatformMoveTo
  PerVertex (ON; no platform moves here), Vrf:CompositionVariant, Vrf:CatalogueSms (empty = the installed
  AggregateTacticalLevel.sms under MAK_VRFDIR C:\MAK\vrforces5.2d), Vrf:ContainerPopulateTimeoutSeconds (0 = derived 85 s).

(c) THE PRE-WARM (the seat's item 2; LIVE1 sec 3 W, copied). MAK Earth Aggregate (online) has never been loaded on this
machine: the sim's terrain cache (C:\MAK\vrforces5.2d\appData\cache\vrfsim, the default sim-cache-path, LaunchVrf52.ps1
:199-202) holds 23 layer directories, 21,346 files, 405.8 MB, newest write 2026-09-27T00:34:38Z - the entity terrain's
layers only [V, simcache_listing.ps1, names and sizes only]. The aggregate earth adds its own layers (elevation at
vrfsim:max_data_level 12, the OSM ocean/water/feature sets, the VRFSIM.Aggregate feature model: Land, Coast, Hills,
Mountain, Alpine, Forest, Cultivated, Municipal, Grass, River ... [V, the .earth and its include]). A whole runner launch is
the only way to load it (there is no init-only mode, LIVE1 sec 1(e)): the scored command with `--run-secs 120`, its own
appNumber block, the order pushed, UNSCORED, dry run first. Its first-load cost is RECORDED (P23). GATE for E, as LIVE1's
step W: the pre-warm's StopVrf52 exit is 0 or 6 and the post-W inventory shows no vrfSim / vrfGui / VrfC2SimApp / WatchVrf
/ ListenReports. ADDED (sec 3 W): the pre-warm's app log carries no "AGGREGATE CONTAINERS (...) - REFUSING TO START" (the
scored run could not differ) and the preflight-cache manifest is unchanged; either failing is a STOP before E.

(d) POPULATION HAPPENS AT ORDER RECEIPT, for every performer, in the order file's task order - 28ID (T1's performer), then
1-112 IN (T10's), then 48 IBCT (T13's) - not at dispatch: OnOrder calls MaterializeUnit for each task's performer before
the task orchestration starts (VrfC2SimService.cs:4489-4491), MaterializeUnit is once per unit (:2614) and on the aggregate
set goes to PopulateInPlace (:2641-2645). The POPULATE line's "why" names the FIRST task ("task 'T1_...' performer",
"task 'T10_...' performer", "task 'T13_...' performer") when the population runs at once; when it is HELD for the init
(:2628-2635) or DEFERRED to the container's ObjectCreated (:3046-3054), the pending entry is re-written by the performer's
next task, so it may name T2 / T14 instead (with " (shell reflected)" appended on the deferred path). EVERY task of a
container - the HOLDS included - waits on the
population gate before it dispatches (:4660-4678, bound = 85 s + the barrier + 15 s), so "READY FOR TASKING" precedes
T01's and T13's dispatch, and T10 dispatches at its own 300 SIM s start delay, which may be BEFORE T02 and T14 (they wait
for the holds' end, which is counted from the holds' dispatch, after the population). If the init's creates are still
outstanding when the order lands, a population is HELD first (DispatchReadiness.cs:366-372) and runs when the init
settles - RECORDED.

(e) ORDER VALIDATION (C1 / C2, offline, deployed exe) [V]: --parse-order: "Tasks: 5"; durations 4 x 1200000 ms + 1 x
1800000 ms; T14 mapGraphic 7ff48b93 THEN 7351f662; T10 c8d9cd1a, 51a59f89, cc23071f; T02 startAfter f7b52ba4, T14
startAfter 37677c40; T10 simStartMs 1200000; T14 "embedded shape: Route (span 2428 m ...)". --parse-init ... "Not Set":
"Units: 40", "would create (clientId=Not Set): 36", 5 areas, 19 lines. Both identical to E2-2's but the vendor's RDTSCP
timing line.

(f) T02'S FORM - CHANGED FROM THE BRIEF, with the evidence. The brief expected T02 as a point move (PA_Move_To_Location_
Direct). The code sends it CreateRoute (2 pts) + PA_Move_Along_Route: ExecuteTaskOnTick builds routeGeo from the live
ORIGIN first (VrfC2SimService.cs:5111-5114) and appends the task's points after the origin-vertex drop (:5221-5241), so a
task with one STP point has 2 route points, and VertexChainPolicy.FormFor returns SinglePointMoveTo only for <= 1 point
(VertexChain.cs:91). PA_Move_To_Location_Direct (:3223-3238) is therefore unreachable for any container task that has
geometry - predicted ZERO times (P7). (The same held for platforms before M1: LIVE1's pre-warm T02 was "CreateRoute (2
pts)".)

(g) M2 ON THE AGGREGATE MODEL SET - THE HARNESS (scratch u3\laneG1\harness, Harness.exe built against the deployed dll
969c82f8; it calls PreflightService with GetPreflight's options (VrfC2SimService.cs:6780-6799, ModelSet
AggregateTacticalLevel) and ShiftOptions (:7309-7319), on a scratch COPY of the deployed cache, OFFLINE; the copy's
manifest is unchanged afterwards, 0 fetches) [V]. Route origin = the container's centroid after the slot check (sec
1(i)). Per task (VERTEX CHECK / legs / reports):
- T14 [54.019341,23.313809 -> (i) -> 54.040348,23.324206]: VERTEX CHECK (AggregateTacticalLevel) 2 checked, 0 moved, 0
  kept, 0 unverified; leg 1 1,270 m, not flagged, nearest OSM water 64.7 m (197345448), EXPECTED SLOW 1,270 of 1,270 m;
  leg 2 2,902 m, not flagged, nearest water 62.2 m (505603429), EXPECTED SLOW 1,663 of 2,902 m; "ROUTE SHIFT - no leg
  flagged"; L12 x2; 2 ObservationReports (EXPECTED SLOW x2).
- T10 [54.042688,23.308235 -> (h) -> (j) -> PassagePoint_48_IBCT_SLOT0]: 3 checked, 0/0/0; legs 1,451 / 804 / 516 m, none
  flagged (nearest water 109.0 / 156.7 m / none), EXPECTED SLOW 805 / 103 / 369 m; "no leg flagged"; L12 x3; 3 reports.
- T02 [53.992385,23.211255 -> 54.028874,23.264401]: 1 checked, 0/0/0; leg 1 5,341 m FLAGGED - "impassable OSM water ON
  the centreline at 2.40 km (waterway=river (MAK_WIDTH 5 m), OSM 8011072)", first wet at 54.0087722, 23.2351231 (2,398.9 m
  along); EXPECTED SLOW 3,370 of 5,342 m; the shift finds the SAME water at both ends of the +/-600 m band -> "NO ROUTE
  SHIFT - RIVER CROSSING ... needs a road/bridge; STP authoring", dispatched as authored; no road bridge on the line;
  L12 x1; 3 reports (RIVER CROSSING, OSM WATER ON THE LINE, EXPECTED SLOW).
- 8 pre-flight ObservationReports in all; the tile reads 49 cache hits, 0 fetches (and 12 hits, 0 fetches for the three
  slot checks).
Consequence for T02, from the vendor's own data: a river line IS MAK_WATERWAY (featureconfig.txt :413-414, MAK_WIDTH 5 m at
:423), MAK_WATERWAY is IMPASSABLE for the motorized Stryker HHT that is 28ID's one member (:266-267;
motorized-aggregated-movement.sysdef :115-119, speed-factor 0), and the aggregate terrain loads that River layer
(VRFSIM.Aggregate.feature.model.xml :323-352). The unit's centre point moves at most 15.3 m/s x 0.2 s = 3 m per actuator tick
(the motorized min-tick-period 0.2 s, :99), less than the 5 m width, so it cannot step over the line [A: that the actuator
tests the centre point each tick]. Predicted: 28ID stops at the river (P14), and T02 ends on the stall watchdog (S5).

(h) THE CONTAINER SCRIPT, READ FROM THE VENDOR'S FILE (PA_Move_Along_Route.lua, VENDOR CITATION). init() snapshots the
container's subordinates and sends each warfare-model member the built-in "move-along" on the SAME route, with
start_at_closest_point = false (C1 sends startAtClosestVertex false, ContainerPopulation.cs:50-55) - so each member first
drives from its ring slot to the route's FIRST vertex, the container's own position at dispatch (UG52 30.22 p598 step 5,
30.24 p599) - then along the route; each finished member is sent "move-to-location-retrograde-task" to route end + its
offset from the container at init, rotated by (final-leg bearing - the container's initial heading); the task ends when
every snapshot member did both. So the members converge on vertex 0, drive the route in file, and unpack round its end,
where their centroid is the route end when every one of them gets there.

(i) THE RING AND THE MEMBERS' OWN LEGS (the harness, `populate` and `members` modes; the same CompositionResolver,
PopulatePlanner and slot check - VertexNudgeSearch with the M2 point test, VrfC2SimService.cs:3115-3148 - as the service)
[V]:
- 28ID: row C-USA-DIV-UCI (map row F-UCI-I), 1 member "28ID__FRIENDLY_INFANTRY_DIVISI.HQ1" (Stryker  HHT (USA)), radius 0
  m (spacing 180 m, reach 90 m); slot clear.
- 1-112 IN: row C-USA-BN-UCI (F-UCI-F), 5 members HQ1 + RIF1-3 + WPN1 ("1-112_IN/28ID__FRIENDLY_INFANT.HQ1",
  "1-112_IN/28ID__FRIENDLY_INFAN.RIF1" ...), radius 153 m; 5 slots clear.
- 48 IBCT: row C-USA-BDE-UCI (F-UCI-H), 17 members (HQ1, INF1-3 x {HQ1, RIF1-3, WPN1}, CAV1; 4 x Stryker  HHT (USA), 12 x
  Mech CO (USA, M2), 1 x Stryker Cavalry SQDN (USA)), radius 490 m (spacing 180 m, reach 1,090 m; 3 sub-containers
  flattened). SLOTS 4, 5 and 6 (INF1RIF2, INF1RIF3, INF1WPN1) lie IN lake 197345448 and SLOT 16 (INF3WPN1) IN pond
  16373225: "SLOT MOVED 125 m north-west", "SLOT MOVED 125 m south-west", "SLOT MOVED 25 m south", "SLOT MOVED 75 m
  south-east"; 13 clear. The moved slots shift the members' centroid 8.1 m, to 54.019341, 23.313809 - T14's route origin.
- THE FAR-SHORE SLOT: INF1RIF2's moved slot (54.022284, 23.319544) is on the far side of lake 197345448 from vertex 0:
  its leg to vertex 0 meets the lake 8 m out. It is predicted never to reach vertex 0 (P11). Three more first legs pass
  within 1.8-7.5 m of water (INF1RIF1 4.9 m, INF1RIF3 5.4 m, INF1WPN1 7.5 m, INF3WPN1 1.8 m) - clear on a centre-point
  model, tight against the sim's own rendering of the same polygons [A].
- THE UNPACK TARGETS at T14's end (final leg bearing 350.6 deg; the container's initial heading is [A] - its members are
  created at heading 0 (ContainerCatalogue.cs:209-213, HeadingDeg 0.0), so 0 is registered): INF2WPN1 and INF3HQ1's
  targets lie IN lake 197345447 (1 to 3 targets for headings 0 / 112 / 180 / 270 - never 0). T10's 5 targets and T02's 1
  are dry for every heading, and every T10 / T02 first leg is dry.
- Consequence: T14's PA_Move_Along_Route cannot end while a snapshot member stays short (the lake stops the far-shore
  member; the targets in water stop two more at the edge) unless the vendor's blocked move ENDS - the open question. The
  centroid then rests about 146 m from the destination (1/17 of the far-shore member's ~2.5 km to its target; P13).

(j) TIMINGS, estimated, not scored [A: speeds x the vendor's factors, no overlap or hills modelled]. Speeds: Mech CO
8.33 m/s ordered (2.08 in forest/urban, factor 0.25), Stryker HHT and Cavalry SQDN 15.3 m/s (3.82 in forest). T14: the
12 Mech CO convergence ~236 s, leg 1 (all forest) ~611 s, leg 2 ~949 s -> route end ~1,800 SIM s after dispatch, the
Strykers ~980 s; the centroid within 500 m of the destination ~1,600-1,900 SIM s after dispatch. T10: ~800-950 SIM s. T02:
the river ~300-600 SIM s after dispatch, then a 360 SIM s stall window. The sim/wall ratio of the aggregate profile is
unknown (E2-2's entity-level run: 7.3-14.7x); at 5x or more everything lands well inside the 2,700 s cap.

(k) NAMES AND PROXY TAGS. Seven containers are of the NEAREST branch and therefore PROXY (composition_check.py
--init-census, pinned by --populate-selftest p7 [V]): 11_CAB/28ID..., 28ID__FRIENDLY_INFANTRY_DIVISION, 4ID__FRIENDLY_
INFANTRY_DIVISION, 28ID/III_Corps..., 105th_AT_BDE..., 11th_Combat_Aviation_Brigade/28ID..., 4ID/III_Corps.... Each name +
"~PXY" exceeds the 34-character marking limit (VrfSettings.cs:107; VrfC2SimService.cs:9488; the shortest is 31 + 4 = 35),
so each gets "Proxy marking tag NOT appended to '<name>' ..." (:1555) and keeps its name; the 7 go to the report stream
("Init (...): 7 PROXY substitution(s) surfaced to C2SIM (R-SURFACE-PROXY).", :2160). Every populated container is
re-announced once (AnnounceSubstitution, :2795-2809; composedFrom > 0 is a substitution, SubstitutionAnnouncer.cs): 3
"R-SURFACE-PROXY: <container> is now represented by '<template> composed from N sub-unit(s)' ..." lines.

(l) THE TO TWINS AT INIT. On the aggregate set the TO parents are containers, so the init composes them
(ApplyHierarchyComposition, VrfC2SimService.cs:2269-2352): 28ID/III_Corps__TWO_EIGHT_TH_US_INFANTRY_DIVISION gets its 9
created TO children (113th SB, 116ABCT, 11th CAB, 169th FA BDE, 1st Bn 112th IN, 278 ACR, 48IBCT, 55th MEB, 56SBCT - all
on its own cascaded coordinate) and 278_Armored_Cavalry_Regiment/28ID... gets 4th Sqn 278th; III Corps is not created (no
coordinates). These are EMPTY containers attached to EMPTY containers (D-7 "TO twins display-only"); nothing is deleted.
28ID (the COA unit, uuid 200d3a3f) is the uuid-first unit of its stacked group, so the de-stack keeps it on its authored
point (DeStacker.cs:260) - the harness's T02 origin [A: the de-stack's group membership on the aggregate set was not run
offline].

(m) THE TILE CACHE: the deployed <exe dir>\preflight-cache, NOT re-staged - 479 files, manifest
682bdea48f43b6f33d0c34339c6ec269efd786965d81ccd66e4aa7f30ac85b6d (lane E2's cache_manifest.ps1) [V 02:58Z and 03:11Z], the
cache E2-2 ran on. It covers every tile the harness reads (0 fetches), with Vrf:PreflightOffline=False as shipped.

(n) THE CONTROL / REFERENCE: E2-2 (run 20260927T231937Z) drove this order on EntityLevel: 28ID and 48 IBCT were lone
platforms on Move To per vertex (0.9 m and 6.9 m), 1-112 IN an entity-level aggregate. G1 changes the model set and with
it the representation of every unit, the terrain, the dispatch forms and the judges' position source - so no single-
variable claim is made (sec 4 ONE VARIABLE). The scorer's real-run control on E2-2's files reproduces its registered
28ID 5,341.4 m / 0.9 m and 48 IBCT 2,424.8 m / 6.9 m / first within 100 m at t=120.5 [V].

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: none (sec 1(b)); the order is pushed right after the runner's stage-7 oracle gate.

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: the watchdog runs on the SIMULATION clock (--env Vrf__StallClock=sim), window 360 SIM s,
50 m. Armed ends in SIM s from dispatch: T01 and T13 300 (no destination - never watched); T02 and T14 300 and T10 450
(destination tasks: an unarrived one goes OVERDUE and stays watched; nothing is SENT at that end). A stall verdict is
possible from dispatch + 360 SIM s on, for T02 / T10 / T14 only, on each container's ONE position (D1). T14's convergence
phase (members closing on vertex 0 from a 490 m ring) leaves the centroid nearly still for ~130 SIM s, not 360 (the
Strykers pass vertex 0 first) - sec 1(j).

DEVIATION FROM RECORD: the template's "PRE-ORDER GATE: --pre-order-gate nav-area" ("gate PushOrder on the first "New Primary nav area" row; warm the area first") is not used - the aggregate model set loads no nav data, so that row can never print and the gate could only time out (AGGREGATE_PROFILE_OFFLINE sec 6.4); the app's own D5b hold and the C1 publication gate cover an early order.

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels.

DEVIATION FROM RECORD: the successor-gate floor is 600 s - appsettings.json's shipped value - not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:320); as E2-2.

DEVIATION FROM RECORD: the window closes EARLY under -StopWhenComplete, where E1 registered "--no-stop-when-complete --run-secs 2700"; as E2-2, the 2700 s cap is kept.

DEVIATION FROM RECORD: the pre-warm is a whole runner launch that also pushes the order and runs 120 s, where DEMO_RUNBOOK sec 0.4 says "let it reach the initialization, then stop it"; there is no init-only runner mode (LIVE1 sec 1(e)).

DEVIATION FROM RECORD: the design's G1 outline ("Order: cut A reduced to T13 -> T14 on T14's ORIGINAL line", DESIGN_AGGREGATE_CONTAINERS sec 9) is replaced by the ruled cut-A order at the seat's direction; the original line is G2's.

EFFECT OF -StopWhenComplete ON THE WINDOW: the runner closes it once all 3 taskees and all 5 tasks have a TERMINAL report,
60 s have passed and every taskee has post-completion position evidence (Test-EarlyExit, RunnerLib.ps1:328-378). T02's
terminal report is predicted to be its stall TASKABRT and T14's its arrival-evidence TASKCMPLT (~1,600-1,900 SIM s after
its dispatch), so the window closes about a minute after the later of the two. What that costs: a member still unpacking
then, a vendor completion that comes later, and T14's final rest are unobservable; P13's "closest approach" is read inside
the window.

## 2. What the code emits - log-line shapes (src at 9027548 = the deployed build; VrfC2SimService.cs unless named) [V]

START-UP, in this order (ExecuteAsync :572-916):
- L-BUILD (:579) `BUILD IDENTITY: git 9027548, ...`.
- L-FIDELITY (:597) `Type-mapping mode = FidelityTable (62 rows from <...>unit-type-map-52-aggregate.json); FriendlyNation=USA, OpposingNation=RUS; SurfaceProxySubstitutions=True.`
- L-CAT (:2469, from GetResolver inside ContainerStartupPreflight) `AGGREGATE CONTAINERS: catalogue loaded from C:\MAK\vrforces5.2d (root AggregateTacticalLevel, 705 templates, 3 model-set dir(s)) - the init rule and the composition resolve against it (RL-20260927-03).` (harness, exact)
- L-VARIANT (:2920; ContainerComposition.cs:429-448) `COMPOSITION VARIANT catalogue (Vrf:CompositionVariant; RL-20260927-04): 5 of the 8 row(s) of <...>unit-composition-52-aggregate.json (its own and the 'all' rows: C-USA-DIV-UCI, C-USA-BDE-UCI, C-USA-BN-UCI, C-USA-BDE-UCA, C-USA-BN-UCIZ). SMS: Vrf:Scenario is not set. Catalogue: AggregateTacticalLevel. It needs no derived SMS: catalogue units only, which resolve alike on the shipped and the derived set. 7 authoredRows type(s) in the type map, NOT in use (they serve the 'authored' variant only).` (harness, exact but the path)
- L-ROW (:2939) five lines `COMPOSITION ROW <row> (map rows [<maps>]): <N> simulated leaf unit(s), <K> sub-container(s) flattened (D-8, RL-20260927-04): [...]`: C-USA-DIV-UCI [F-UCI-I] 1/0 [1 x Stryker  HHT (USA)]; C-USA-BDE-UCI [F-UCI-H] 17/3 [4 x Stryker  HHT (USA), 12 x Mech CO (USA, M2), 1 x Stryker Cavalry SQDN (USA)]; C-USA-BN-UCI [F-UCI-F] 5/0 [1 x Stryker  HHT (USA), 4 x Mech CO (USA, M2)]; C-USA-BDE-UCA [F-UCATA-H, F-UCA-H] 26/6; C-USA-BN-UCIZ [] 8/1. REFUSED twin (:2932) - none.
- L-CON-ON (:2945-2962) `AGGREGATE CONTAINERS ON (Vrf:ModelSet=AggregateTacticalLevel; ...) ... (hostile nation RUS; ...) ... the composition table <...> (5 row(s) resolve, 0 refused) ... for up to 85 s (Vrf:ContainerPopulateTimeoutSeconds, derived) ... PA_Move_Along_Route on a route, PA_Move_To_Location_Direct to a single point, PA_Patrol_Route for a patrol. A vendor completion farther than 100 m (Vrf:VertexArrivalRadiusMeters) from the route end is WITHHELD (D-6). Catalogue: root AggregateTacticalLevel, 705 template(s) from 3 model-set dir(s).` Its refusal twin (:2903) `AGGREGATE CONTAINERS (...) - REFUSING TO START: ...` and ContainerPopulation.cs:499-503 `AGGREGATE CONTAINERS off (` must not print.
- L-WDOG-ON (:663) `PROGRESS WATCHDOG ON (C16, report-only): a 360 s no-progress window on the SIMULATION clock, 50 m of net displacement per member, ...`; L-CACHE (:720) `ROUTE PRE-FLIGHT TILE CACHE: <exe dir>\preflight-cache - 29 file(s), the SHIPPED FALLBACK ... Vrf:PreflightOffline=False.`; L-SHIFT-ON (:769); L-M1-ON (:801; VertexChain.cs:113-131) `MOVE TO PER VERTEX ON (Vrf:PlatformMoveToPerVertex, RL-20260927-01; ...) ... UNITS are unchanged ... farther than 100 m (Vrf:VertexArrivalRadiusMeters) ...`; L-MODELSET (:806; UnitPositionPolicy.cs StartupLine) `MODEL SET for task judging: Vrf:ModelSet='AggregateTacticalLevel' -> AggregateTacticalLevel (D1, RL-20260927-01). A MEMBERLESS aggregate ... counts as ONE position ...` at INFO; L-DESTACK (:818); L-APPROACH (:834) `ARRIVAL APPROACH FRACTION 0.50 ... Radius 500 m ...`; L-CLOCK (:892) `TASK CLOCK (R4): C2SIM task times are measured on the SIMULATION clock (Vrf:TaskClock=sim) ... Vrf:DurationScale=0.25; a successor waits max(Vrf:TaskPredecessorTimeoutSeconds=600 s, the predecessor's own scaled Duration + Vrf:TaskPredecessorEndMarginSeconds=60 s) ... Vrf:TaskChainBackstopSeconds=86400 s ...`. Q1a (RL-20260927-05) adds no start-up line; its gate line (:8203) prints only when a gate window expires.
- L-OSMSET (:6807) at the pre-flight's FIRST USE - the first population's slot check (CheckPopulateSlots :3118 calls GetPreflight), i.e. at order receipt, before any dispatch: `ROUTE PRE-FLIGHT MODEL SET AggregateTacticalLevel (Vrf:ModelSet; RL-20260927-01) - AggregateTacticalLevel: slope ratio OFF; a leg is FLAGGED only when OSM water (Lake areas, River lines at MAK_WIDTH 5 m) lies ON the centreline ...; Ocean/Coast/Alpine/Mountain/Hills sets are NOT read. OSM features read from <cache>\osm-water (225 tile file(s), 0 of them 0 bytes = UNKNOWN) and \osm (225, 0 0 bytes); ... VERTEX CHECK: ... within 10 m ... within 300 m ...`.
INIT:
- L-TYPEMAP (:1560) `TYPE MAP <Exact|Proxy>: <name> -> <container template> (<11.1.nation.cat.sub.spec.extra>) [<map note> CONTAINER (RL-20260927-03): <template> (<11:1:...>) by the init rule - echelon <E>, branch <b> for <fid>[; NEAREST branch (...)]; the map row keys its composition]` (ContainerCatalogue.cs:222-241); a NEAREST line ends `PROXY: <template> - an Aggregate Container of the NEAREST branch ...`. L-PXYTAG (:1555) `Proxy marking tag NOT appended to '<name>': ...` (WARN). L-NOCONT (:1536) `CONTAINER: unit ... has NO Aggregate Container ...` must not print.
- L-CENSUS-AGG (:1730) `CreationPolicy=AtOrder (C13) on the AGGREGATE model set: <C> container(s) created EMPTY at their authored positions (RL-20260927-03) ... <O> other aggregate shell(s); <P> platform(s) created in full. ...`; its entity twin (:1740) must not print.
- L-COMPOSE (:2348) `ComposeHierarchy: <parent> -> EMPTY shell; will attach <N> declared child unit(s) [...]` and (:2403) `ComposeHierarchy: <parent> composed - <n>/<N> declared children attached.`
- L-BARRIER (:3440) `INIT CREATION BARRIER: <N> object(s) planned by this initialization (<S> empty shell(s)), ...`; L-READY (DispatchReadiness.cs:322-337 / :339-347) `READY TO TASK - <b> of <N> init unit(s) bound ...` or `READY TO TASK - NOT REACHED within <s> s ...`; L-PLACE (:3858) `PLACEMENT summary: <T> of <N> create altitude(s) came from the TERRAIN QUERY, <F> from the FALLBACK ...`; L-INIT-PXY (:2160) `Init (<source>): <N> PROXY substitution(s) surfaced to C2SIM (R-SURFACE-PROXY).`; L-INIT (:2163) `Init dispatched: <U> units + <A> areas + <L> lines + <P> points queued for creation.`
- L-BIND (:6545) `VRF console level 4 requested for <name> (<VRF_UUID>).` - one per created object at its ObjectCreated: the scorer's name -> uuid binding.
POPULATION (order receipt; C1):
- L-POP (:3019-3029) `POPULATE <container> IN PLACE (task '<first task>' performer; RL-20260927-03): source 3 - the authored table, row <row> (map row <map>): <N> member(s) FLAT (D-8, RL-20260927-04: <K> sub-container(s) ...) on ONE ring round its point (<lat>,<lon>): spacing <s> m (...), radius <r> m (centroid-preserving), reach <R> m. ... NOTHING IS DELETED. Members: [<name> (<template>), ...].` Twins: REFUSED (:3007), deferred (:3052), HELD (DispatchReadiness.cs:366-372).
- L-SLOT (:3172-3174) `POPULATE <container> slot <k> of <N>: <member> (<template>, <path>) at (<lat>,<lon>), bearing <b> deg from the container point - <clear | SLOT MOVED <d> m <dir> - the planned slot lies IN OSM water (...) ... | KEPT ON BAD GROUND ... | UNVERIFIED ...>.`
- L-ISSUED (:3176) `POPULATE <container>: <N> member create(s) issued AGGREGATED (UG52 Table 68 p1470), ...`; its late twin (:3159).
- L-ATTACHED (ContainerPopulation.cs:452-455) `POPULATE <container>: <N> of <N> member(s) created; AddToOrganization issued for all of them in planned order (<HQ member> first) - waiting for the container to PUBLISH them (gate ends <t> s from now).`; partial twin (:446-450); late member (:321-323).
- L-PUB (ContainerPopulation.cs:359-363) `POPULATE <container>: the container PUBLISHES <n> subordinate(s) (expected <N>) <a> s after the population began, <b> s after the attach - READY FOR TASKING (RL-20260927-03).`; its TIMED OUT twin (:464-466) `POPULATE <container>: POPULATE TIMED OUT after <s> s: <why> - its MOVE tasks are REFUSED (TASKABRT), ...`.
- L-REANNOUNCE (:2804) `R-SURFACE-PROXY: <container> is now represented by '<template> composed from <N> sub-unit(s)' (it was announced as '<...>') - re-announcing the substitution to C2SIM.`
- L-D5 (:4498) `Task '<T>': its affected entity <name> is NOT populated - D-5 ...` - see P3e.
TASKING:
- L-DISP (:5799) `DISPATCHED <container> task '<T>' (<kind>) at WALL <iso>Z, SIMULATION clock <s> s. ...` - kind `hold-in-place` for T01/T13, `PA_Move_Along_Route` for T02/T10/T14 (MarkDispatched with the container script, :5658).
- L-CONSOLE-MEMBERS (:4851) `VRF console level 4 requested for <N> DISTINCT member(s) of <container> (<N> published, 0 duplicate uuid(s) ..., 0 with no uuid - N5): <name> [<uuid>], ...` - on every ExecuteTaskOnTick pass of a container task (the members come from the population's record, :4824-4827).
- L-CNFPSL (:5083; TaskDispatchPolicy.cs:144-146) and L-INPLACE (:5085) `Task '<T>': verb CNFPSL -> intent=... Executing IN PLACE at <container>'s own position (<lat F5>,<lon F5>); NO VR-Forces task is issued ...`.
- L-FAW (:6448; TaskDispatchPolicy.cs:172-174) `Task '<T2>' (verb ATTACK, 28ID__FRIENDLY_INFANTRY_DIVISION): ATTACK: advancing to the objective; rules of engagement set to fire at will - members engage enemies they encounter (RL-20260926-01). The order's own rules of engagement ('ROEHold') are overridden for this task.`; L-R3SELF (:5732) per pass; L-FOLSPT (:5468) `Task '<T14>' (verb FOLSPT, <48 IBCT>): advancing along the task's graphic to its end and holding there - no engagement task, rules of engagement as ordered ('ROEHold'). ...`; L-BARE (:4879) `Task '<T10>' verb=CRESRV -> intent=HoldObjective (...); Layer-2 not yet wired - executing bare movement.`
- L-SHIFTQ (:7463) `Task '<T>': ROUTE SHIFT check queued for <container> (<n> vertices); ...`; L-VCHECK (:7392) `Task '<T>' (<container>): VERTEX CHECK (AggregateTacticalLevel) - <n> authored vertex(es) checked against OSM water and buildings: <m> moved, <k> kept on bad ground, <u> unverified, the rest clear.`; L-ELEV (:6903) `... ELEVATION LEVEL ACTUALLY USED - L12 x<n> over <n> leg(s) ...`; L-OSMWATER (:6987, WARN) `ROUTE PRE-FLIGHT task '<T>' (<container>) leg <i>: OSM WATER ON THE LINE - OSM <id> (<kind>), nearest <d> m, first at (<lat>,<lon>) <km> km along, ...`; L-SLOW (:7008) `ROUTE PRE-FLIGHT task '<T>' (<container>) leg <i>: EXPECTED SLOW - <s> m of <len> m in OSM forest/swamp/municipal land use (speed-factor 0.25 on the aggregate model set); reported only.`; L-RIVER (:7524, WARN) `Task '<T>' (<container>) leg <i>: NO ROUTE SHIFT - RIVER CROSSING - the same OSM water lies on the line at BOTH ends of the +/-600 m lateral band ... needs a road/bridge; STP authoring. The task is dispatched on the line as authored and the C2 side is told it needs STP authoring (RL-20260927-01).`; L-NOFLAG (:7562) `Task '<T>' (<container>): ROUTE SHIFT - no leg flagged; the route is unchanged.`
- L-ROUTE-C (:5679) `Task '<T>': CreateRoute '<T> ROUTE' (<n> pts) for CONTAINER <container> - <N> published member(s); RunScriptedTask PA_Move_Along_Route deferred to route-created (RL-20260927-03).`; L-SCRIPT (:6664) `Route '<T> ROUTE' (<VRF_UUID>) created; RunScriptedTask PA_Move_Along_Route issued for CONTAINER <container> (<VRF_UUID>) with [route=<VRF_UUID>, reverseDirection=false, startAtClosestVertex=false] - <N> published member(s) (RL-20260927-03).`; its failure twin (:6669) `... was NOT issued for CONTAINER ...`; the memberless refusal (:3214-3219) `... REFUSED: container <name> has no published members to move ...`; the point move (:3235) `RunScriptedTask PA_Move_To_Location_Direct ...`.
- The vendor script's own console lines, relayed as (:9344) `VRF console [<level>] <container> (<uuid>): ...Init of aggregate move along.` and `... Sub. <member> starting move-along.` / `has completed move task.` / `has completed final move.` (PA_Move_Along_Route.lua :28, :80, :110, :140).
JUDGING AND COMPLETION:
- L-TIMED (:8140 WARN / :8151) `TIMED COMPLETION: task '<T>' on <container> reached its END TIME - <S> s of a <D> s Duration served ... [- but the unit has NOT ARRIVED: OVERDUE.]`.
- L-ARRIVE (:7752) `ARRIVAL EVIDENCE: <container> task '<T>' - 1/1 member(s) within 500 m of the last vertex (nearest <d> m) AND past their OWN traversal bar ... ONE POSITION: an aggregate-level unit with no members, judged on its own reflected centre point (D1, RL-20260927-01).` (the suffix :7768-7770); L-CANNOT (:7680) must not print.
- L-STALL (:8567) `STALL: unit <container> task <T>: no member moved more than 50 m in the last 360 SIM s (max <m> m); TASKABRT reported. ONE POSITION: ...` (:8572) and its TASKABRT text (:8585) `STALLED (C16 progress watchdog) - report only: the task stays in flight, ...`.
- L-VRFDONE (:9020) `VRF task complete: <container> / <type> (success=<b>)`; a MEMBER's completion is at Debug (:9015) and never reaches a TaskStatus; L-D6 (:3256-3261, WARN) `CONTAINER <container> task '<T>': VR-Forces reported '<type>' COMPLETE, but completed short: <d> m from the route end (...) - TASKCMPLT is WITHHELD (D-6, RL-20260927-04). ...` (ContainerPopulation.cs:526-527) and its hand-on twin (:3265-3270); L-SWALLOW (:9042) `VRF completion for <container> after the arrival-evidence report of task <T> - swallowed.`; L-OVERDUE-ARR (:9160); L-HELD (:9155).
- L-SENT (:9594) `SENT TASK STATUS REPORT (<code>) taskee=<C2SIM uuid> task=<task uuid> - <why>.` - the taskee is always a PERFORMER's C2SIM uuid (members have none).

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run unless marked PREP)

All from Git Bash at the MAIN checkout F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM (HEAD 1efbbe7 when
this was written, tracked tree clean [V]). The order, init, type map and composition are the main checkout's own data/
files (hashes equal to this branch's [V]). The runner writes its appNumber blocks into the MAIN checkout's working-tree
docs/OPUS_EXECUTION_PLAN.md; they are carried back to this branch afterwards (the E1 / E2-2 procedure). Scripts (scratch
u3\laneG1): golive_checks_g1.ps1, g1_runner.sh (one command line for every runner call: prewarm-dryrun | prewarm |
scored-dryrun | scored), simcache_listing.ps1, g1_score.py, the harness.

A0. PRECONDITIONS: (1) the seat's go-live; (2) no other lane is building, running a suite or an agent for the quiet
    period; (3) `git diff --stat 9027548 main -- src` is still the vcxproj only - if the MANAGED source has moved on,
    NOTHING is rebuilt: the registered build is what runs and the difference is recorded; if the DEPLOYED hashes differ
    (another lane rebuilt), STOP before W and the seat decides; (4) the deployed fixture, the order, init, type map and
    composition hashes and the cache manifest as registered.
A.  golive_checks_g1.ps1 -Phase preholder -MarkerWant 5207: 0 checks FAILED. [PREP V 2026-09-28T03:11:13Z: 0 FAILED - exe
    31be659b, dll 969c82f8, appsettings 0f600f0e, bridge 5198ac45 in the app and RtiProbe trees, PV 1.0.0+git.9027548,
    fixture 804e2c39, order 7a986137, init 2000e856, type map c546edbe, composition 9684e945, cache 682bdea4 / 479 files,
    REST 200, rtiexec 47980 / rtiForwarder 50740 / rtiAssistant 30240 up, no RtiProbe, no sim/app/observer, marker 5207.
    Recorded: the aggregate terrain files (vendor, read-only) mtf f399dc41..., earth 04f8830b..., feature model 753d2489....]
B.  No build.
C.  Order validation: C1 / C2 [PREP V, sec 1(e)]; C3 ONE real push to the PRIVATE server, at go-live, after this
    registration is committed:
        tools/PushInit/bin/Release/net10.0/PushInit.exe data/IRONSTORM_CUTA_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
        tools/PushOrder/bin/Release/net10.0/PushOrder.exe data/IRONSTORM_CUTA_Order.xml 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
    EXPECT both exit 0, "QUERYINIT : 40 Units" and one `ORDER (69670 chars)` echo carrying 5 tasks, 5 Durations and
    CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT (E2-2's). Any failure = STOP.
D.  THE HOLDER (new; the last one, 45600, resigned at 02:20:59Z). First HAND-CLAIM 5207-5210 in Appendix B of the main
    checkout's working-tree OPUS_EXECUTION_PLAN.md and of this branch (committed here), marker 5207 -> 5211, BEFORE the
    holder joins; then:
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5207,5208,5209,5210 -SettleSecs 28800 -WhatIf
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5207,5208,5209,5210 -SettleSecs 28800 < /dev/null > runs/launch52/g1-holder-<stamp>.log 2>&1
    EXPECT `HOLDER JOINED: pid ... appNo 5207`, exit 0. Exit 1 (none joined) or 2 = STOP, no blind relaunch (RUNBOOK 9c).
    [PREP V: the -WhatIf exit 0 - rtiexec 47980 OK, log found, "nothing was started, no application number was spent".]
W.  THE PRE-WARM (unscored): golive_checks_g1.ps1 -Phase prelaunch -HolderPid <pid> -MarkerWant 5211 (0 FAILED);
    simcache_listing.ps1 -Out simcache_before_prewarm.txt; `sh g1_runner.sh prewarm-dryrun` (EXPECT the holder recognised
    PERSISTENT, block 5211-5221, "marker would advance to: 5222", model set AggregateTacticalLevel, the fixture pairing
    line, window 120 s CAP); then `sh g1_runner.sh prewarm` once. GATE for E: StopVrf52 exit 0 or 6; the post-W inventory
    shows only rtiexec, rtiForwarder, rtiAssistant, the G1 holder and the pre-warm's Stage 2h holder; the pre-warm's app
    log has no "REFUSING TO START"; the cache manifest still 682bdea4. Any of these failing = STOP before E. Then
    simcache_listing.ps1 -Out simcache_after_prewarm.txt. [PREP V: prewarm dry run 20260928T025941Z exit 0 - block
    5207-5217 at marker 5207 (the claim moves it), "no hold and no gate", window 120 s CAP, observers 680 s, nothing
    launched, marker unmoved.]
E.  THE RUN: golive_checks_g1.ps1 -Phase prelaunch -HolderPid <pid> -MarkerWant 5222 (0 FAILED); `sh g1_runner.sh
    scored-dryrun` (EXPECT block 5222-5232, marker -> 5233, the layout of sec 5, window 2700 s CAP with -StopWhenComplete);
    then `sh g1_runner.sh scored` ONCE, stdout to a file, never piped. The command g1_runner.sh runs, verbatim:

        scripts/RunScenario.sh \
          --scenario IronStorm_Centre_52_Aggregate \
          --init data/IRONSTORM_CUTA_Initialization.xml \
          --order data/IRONSTORM_CUTA_Order.xml \
          --client-id "Not Set" \
          --model-set AggregateTacticalLevel \
          --duration-scale 0.25 \
          --object-console 4 --member-console 4 \
          --stop-when-complete --run-secs 2700 \
          --env Vrf__StallDetection=true \
          --env Vrf__StallClock=sim \
          --env Vrf__TaskPredecessorTimeoutSeconds=600 \
          --sample-threads \
          --no-gui \
          --log runs/launch52/RunScenario-ironstorm-agg-g1-<stamp>.log

    (the pre-warm: the same line with --run-secs 120 and its own log name; the dry runs add --dry-run). [PREP V: scored dry
    run 20260928T025909Z exit 0 - "model set : AggregateTacticalLevel <- argument -ModelSet", "type map :
    data/unit-type-map-52-aggregate.json - declares AggregateTacticalLevel", "fixture ... loads AggregateTacticalLevel ...
    matches -ModelSet", "deployed VrfC2SimApp BUILD IDENTITY: git 9027548", block 5207-5217 (marker would -> 5218), "no hold
    and no gate", "window : 2700s CAP; -StopWhenComplete closes it once all 3 taskee(s) and all 5 task(s) have a TERMINAL
    report ...", "NOTHING was launched, NO server was contacted, the Appendix B marker was NOT advanced (it still reads
    5207)"; the main checkout's tracked tree unchanged.] GATE on teardown: StopVrf52 exit 0 or 6 (P21); 3, 5 or 7 = STOP.
F.  Post-run, in the FOREGROUND: the inventory (only the RTI trio, the G1 holder and, inside its 900 s hold, the Stage 2h
    holder); hashes and the cache manifest re-read (golive_checks_g1.ps1); simcache_listing.ps1 -Out
    simcache_after_scored.txt; then the harvest (sec 6).

QUIET PERIOD (RUNBOOK 0.5.14 item 5): from the launch of W to the post-run inventory of E: no Stop-Process / taskkill of
any kind (StopVrf52's own identity-gated force of the run's own back end is allowed), no build, no suite, no subagent, no
second runner - in this lane or any other. The executor polls the runs' own files from the FOREGROUND with bounded waits
and does not end its turn while a run is open. Never touched: rtiexec 47980, rtiForwarder 50740, rtiAssistant 30240, any
RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES: per TASK UUID from the scored run's vrfc2simapp.log in line order, cross-checked against reports-captured.log;
positions from watchvrf-trace.csv POS rows. THE SCORER scratch u3\laneG1\g1_score.py <runDir> (imports lane E2's
e2_score.py: the trace parser, the SIM clock, the OSM feature reader; the OSM tiles are a copy of the deployed cache,
manifest 682bdea4): the family census of sec 2, the per-task SENT codes, the population per container, and per mover the
CONTAINER's own POS track (the vendor's published centroid: displacement since dispatch = the farthest fix from the last
fix before L-DISP, closest approach to the destination, first fix within 100 m / 500 m, final fix) and each MEMBER's track
(displacement, closest approach to route vertex 0, to its predicted unpack target, final distance), the members'
unweighted mean against the container's fix, and T02's stop against the river point. Its --selftest: 14 CLEAN checks + 4
DIRTY controls (a member TaskStatus, a DELETE, a point move, a missing publication) each caught [V]; its real-run control
on E2-2 reproduces E2-2's registered displacements [V]. Names bind to uuids through L-BIND and L-CONSOLE-MEMBERS; members
belong to the container whose L-POP lists them. "Before X" is log-line order.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) The G1 holder alive at launch and recognised PERSISTENT by the runner; the back end JOINS (rtiexec count-grep: no create for its appNo); "READY - joined the federation". (ii) L-BUILD names git 9027548; exe / dll / bridge / appsettings / fixture / order / init / type map / composition hashes and the cache manifest as registered, before and after. (iii) The runner's Stage 0 prints the model-set, type-map and fixture pairing lines of sec 3 E and its manifest records inputs.modelSet AggregateTacticalLevel. (iv) No REFUSING TO START and no "AGGREGATE CONTAINERS off" line. (v) THE FLOOR ARRIVED: L-CLOCK reads `Vrf:TaskPredecessorTimeoutSeconds=600 s` and both L-GATE lines (T02, T14) read `and then 600 s to COMPLETE`. (vi) The pre-warm's gate (sec 3 W) held. | HIGH | Any limb = VOID + STOP (launch, harness or settings failure - no code verdict). | |
| P1 | START-UP LINES, each exactly once, at INFO, with the numbers of sec 2: L-FIDELITY 62 rows; L-CAT (705 templates, 3 model-set dirs, root AggregateTacticalLevel, home C:\MAK\vrforces5.2d); L-VARIANT catalogue with "5 of the 8 row(s)" and "7 authoredRows type(s) ... NOT in use"; FIVE L-ROW lines 1/0, 17/3, 5/0, 26/6, 8/1 with the templates of sec 2 and ZERO REFUSED rows; L-CON-ON with "hostile nation RUS", "(5 row(s) resolve, 0 refused)", "85 s" and "100 m"; L-MODELSET naming AggregateTacticalLevel; L-M1-ON; L-WDOG-ON (360 s, SIMULATION, 50 m); L-CLOCK (SIMULATION, 0.25, 600, 60, 86400); L-SHIFT-ON; and L-OSMSET once, naming AggregateTacticalLevel and "osm-water (225 tile file(s), 0 of them 0 bytes = UNKNOWN) and \osm (225, 0 0 bytes)", printed after the first L-POP and before the first L-DISP. ZERO "is not EntityLevel or AggregateTacticalLevel" warnings. | HIGH | A missing, duplicated or differently-numbered line = STOP (the build or the settings are not what was registered). | |
| P2 | INIT - 36 EMPTY CONTAINERS. (a) 36 L-TYPEMAP lines carrying "CONTAINER (RL-20260927-03)": 29 `TYPE MAP Exact` and 7 `TYPE MAP Proxy` whose note says "NEAREST branch", the 7 of sec 1(k); 48 IBCT -> `BDE (11.1.225.8.3.1.1)`, 1-112 IN -> `BN, Light Infantry (11.1.225.6.3.1.0)`, 28ID -> `DIV, Mech Infantry (11.1.225.9.4.1.0)` (Proxy); every hostile container nation 260. (b) ZERO L-NOCONT. (c) 7 L-PXYTAG warnings, one per NEAREST container. (d) L-CENSUS-AGG reads 36 / 0 / 0 (36 containers, 0 other shells, 0 platforms) and the entity census twin is absent; L-BARRIER reads 36 objects (36 empty shells); L-INIT reads "36 units + 5 areas"; L-INIT-PXY reads 7. (e) 36 container names bound by L-BIND before the order, and ZERO member names bound before it (no warfare-model unit at init). (f) ZERO DELETE-family lines at init or later (P-FALS). READY TO TASK, the init L-PLACE counts and "ORDER BEFORE READY TO TASK" RECORDED. | HIGH | Any limb = STOP. | |
| P2b | THE TO TWINS: exactly two L-COMPOSE parents - 28ID/III_Corps__TWO_EIGHT_TH_US_INFANTRY_DIVISION with 9 declared children and 278_Armored_Cavalry_Regiment/28ID__TWO_SEVEN_EIGHT_ARMORED_CAVA... with 1 - each "composed - n/N declared children attached" with n = N. | MEDIUM | Another parent set, a count, or n < N = recorded MEDIUM miss (the de-stack and the composition classifier on the aggregate set were not run offline, sec 1(l)). | |
| P3 | POPULATION, per performer, at ORDER RECEIPT (sec 1(d)): exactly one L-POP each, its "why" naming one of that performer's tasks (T1 or T2 for 28ID, T10, T13 or T14 for 48 IBCT - sec 1(d)) - 28ID source 3 row C-USA-DIV-UCI (map row F-UCI-I), 1 member, radius 0 m; 1-112 IN row C-USA-BN-UCI (F-UCI-F), 5 members, radius 153 m; 48 IBCT row C-USA-BDE-UCI (F-UCI-H), 17 members, radius 490 m, "3 sub-container(s) ... flattened"; member lists = the names of sec 1(i); and per container, in any order after its L-POP, exactly N L-SLOT lines, one L-ISSUED (N AGGREGATED), one L-ATTACHED "N of N member(s) created; AddToOrganization issued ... (<the HQ1 member> first)", one L-PUB "PUBLISHES N subordinate(s) (expected N) ... READY FOR TASKING" and one L-REANNOUNCE "... composed from N sub-unit(s)". All 23 member names bound by L-BIND. ZERO REFUSED, TIMED OUT, partial, late-member and slot-check-late lines. The order of the three L-POP lines, which task each "why" names, a HELD / deferred line and the publication times RECORDED. | HIGH | A missing or duplicated line, another source / row / count / radius, a member unbound, or any refusal / timeout = STOP, except the named branches S2 (TIMED OUT) and S3 / S4 below, which are recorded and the run continues. | |
| P3b | SLOT VERDICTS as the harness (sec 1(i)): 28ID 1 clear; 1-112 IN 5 clear; 48 IBCT 13 clear + slots 4, 5, 6, 16 "SLOT MOVED 125 m north-west / 125 m south-west / 25 m south / 75 m south-east" (lake 197345448 x3, pond 16373225 x1); ZERO KEPT ON BAD GROUND and ZERO UNVERIFIED. | HIGH | Another verdict for any slot = STOP (the slot check did not do live what its own code does offline on the same tiles). | |
| P3c | PUBLICATION BEFORE DISPATCH: each container's L-PUB precedes the L-DISP of every task it performs (28ID before T01, 1-112 IN before T10, 48 IBCT before T13); its "(expected N)" equals N; ZERO memberless-move refusals. | HIGH | A dispatch before its container's L-PUB, or a refusal = STOP (the publication gate did not hold). | |
| P3e | D-5: ZERO "is NOT populated - D-5" lines, and 116_ABCT/28ID__FRIENDLY_AIRBORNE_TRACKED_ARMORED_BRIGADE gets NO L-POP - CHANGED FROM THE BRIEF: OrderParser keeps only the FIRST AffectedEntity (OrderParser.cs:76), which for every task is its performer (T14's 116 ABCT, cut-A change (f), is the second), so the D-5 branch (:4492-4502) is never entered; 116 ABCT stays empty because it performs nothing. | HIGH | A D-5 line or a population of any unit other than the three performers = STOP. | |
| P4 | CNFPSL HOLDS T01 and T13: each one L-DISP (hold-in-place) after its container's L-PUB, one L-CNFPSL + one L-INPLACE naming the container, one STP-866 observation, "it has no destination" armed at 300, NO route and NO scripted task, exactly one TASKCMPLT "(300 s after dispatch)". The L-INPLACE position of 48 IBCT RECORDED (the harness centroid 54.01934, 23.31381). | HIGH | A move or script for a hold; zero or two TASKCMPLTs; an OVERDUE for a hold = STOP. | |
| P5 | DISPATCH STRUCTURE: exactly one SENT TASKSTRT per task (5); T02's L-DISP after T01's TASKCMPLT and T14's after T13's; one "Task 'T10...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25)" line and T10's L-DISP after it; ZERO "SKIPPED: predecessor"; ZERO RL-20260927-05 gate-expiry lines (:8203). The dispatch order of T10 against T02 / T14 RECORDED (sec 1(d)). | HIGH | A missing or duplicate TASKSTRT, a successor before its predecessor's TASKCMPLT, or a SKIP = STOP. | |
| P6 | M2 ON THE AGGREGATE MODEL SET, per mover exactly as the harness (sec 1(g)): L-SHIFTQ "(2 vertices)" T02, "(4 vertices)" T10, "(3 vertices)" T14; L-VCHECK (AggregateTacticalLevel) 1 / 3 / 2 checked, 0 moved, 0 kept, 0 unverified; L-ELEV L12 x1 / x3 / x2; L-SLOW T02 leg 1 3370 of 5342 m, T10 legs 805/1451, 103/804, 369/516 m, T14 legs 1270/1270, 1663/2902 m; T02: one L-OSMWATER "OSM 8011072 (waterway=river (MAK_WIDTH 5 m))" first at 54.0087..., 23.2351... ~2.40 km along, and one L-RIVER; T10 and T14: one L-NOFLAG each; ZERO VERTEX MOVED / NOT MOVED / UNVERIFIED, ZERO "could NOT be read", ZERO ROUTE SHIFTED, ZERO "NO CLEARED LINE", ZERO PRE-DISPATCH applied, ZERO land-cover "WATER ON THE LINE"; exactly 8 pre-flight ObservationReports in the capture (T02: RIVER CROSSING, OSM WATER ON THE LINE, EXPECTED SLOW; T10: EXPECTED SLOW x3; T14: EXPECTED SLOW x2). | HIGH | Any limb = STOP (the stage did not do on these routes what its code does offline on the same tiles). | |
| P7 | THE CONTAINER'S OWN SCRIPT: per mover one L-ROUTE-C - T02 "(2 pts) for CONTAINER 28ID__FRIENDLY_INFANTRY_DIVISION - 1 published member(s)", T10 "(4 pts) ... - 5", T14 "(3 pts) ... - 17", each "RunScriptedTask PA_Move_Along_Route deferred to route-created" - then one L-SCRIPT "RunScriptedTask PA_Move_Along_Route issued for CONTAINER <c> (<its uuid>) with [route=<the route's uuid>, reverseDirection=false, startAtClosestVertex=false] - N published member(s)"; L-DISP kind "PA_Move_Along_Route". ZERO "was NOT issued", ZERO "RunScriptedTask PA_Move_To_Location_Direct" (sec 1(f) - CHANGED FROM THE BRIEF), ZERO "created; MoveAlongRoute issued", ZERO "MOVE TO PER VERTEX for", ZERO MoveIntoFormation / R11 / fan-out / formation lines for a container. L-CONSOLE-MEMBERS names 1 / 5 / 17 distinct members on each pass (pass count RECORDED). | HIGH | Any other form, point count, member count or variable value = STOP. | |
| P8 | VERB LINES: T02 exactly one L-FAW (a UNIT's ATTACK - RL-20260926-01; TaskDispatchPolicy.cs:134) and one L-R3SELF per dispatch pass; T14 exactly one L-FOLSPT naming 'ROEHold'; T10 one L-BARE per pass; ZERO L-FAW for any other task. | HIGH | A missing, duplicated or misplaced line = STOP. | |
| P9 | THE VENDOR SCRIPT SPEAKS (the container's console at level 4): per mover one "Init of aggregate move along." on the container's console, then one "Sub. <m> starting move-along." per member (1 / 5 / 17) and ZERO "starting pa_move_along_route" (members are AGGREGATED, PA_Move_Along_Route.lua :46/:65). The "has completed move task." / "has completed final move." lines per member RECORDED. | MEDIUM | Fewer or no such lines = recorded MEDIUM miss (the relay of a container's printDebug at level 4 is [A]); a "starting pa_move_along_route" line = STOP (a member was created Disaggregated). | |
| P10 | MOVEMENT: each moving container's OWN published position (the centroid) is displaced MORE THAN 50 m after its L-DISP (T02, T10, T14); members displaced > 50 m after it: 28ID 1 of 1, 1-112 IN 5 of 5, 48 IBCT at least 12 of 17. | HIGH | A container at or under 50 m, or fewer members moving = STOP (named outcomes recorded: the script not received, members idle, a container whose position does not follow its members). | |
| P10a | 48 IBCT: EXACTLY 16 of 17 members displaced > 50 m - all but INF1RIF2 (P11). The near-water first legs (INF3WPN1 1.8 m from pond 16373225; INF1RIF1 4.9 m, INF1RIF3 5.4 m, INF1WPN1 7.5 m from lake 197345448, sec 1(i)) pass on a centre-point model with the polygons as read. | MEDIUM | 12-15 = recorded MEDIUM miss, naming the members that stopped and the water within 10 m ahead of each (the sim's own lake/pond edges are then wider than the OSM polygons M2 reads - a finding for M2 and for C1's slot check). | |
| P10b | THE CONTAINER IS ITS MEMBERS' CENTROID: at the last common trace instant, the container's own fix lies within 25 m of its members' unweighted mean (design Q2: the vendor's 61 of 61 at 0.0 m), for each of the three. | MEDIUM | Beyond 25 m = recorded MEDIUM miss (the D1 judge would be reading something other than the members' mean). | |
| P11 | THE FAR-SHORE MEMBER: 48_IBCT/28ID__FRIENDLY_IN.INF1RIF2 never comes within 50 m of T14's vertex 0 (54.019341, 23.313809) - lake 197345448 lies between (sec 1(i)). Its outcome RECORDED as one of: (a) it stays within ~10 m of its slot (the blocked move runs forever); (b) its move ends or fails at the shore and its retrograde move takes it toward its unpack target (54.043804, 23.329044); (c) anything else. | HIGH | Within 50 m of vertex 0 = the aggregate model crossed or skirted OSM water that M2 reads as a stop: STOP and ask (it indicts M2's aggregate leg rule and G2's lake branch). | |
| P12 | COMPLETION PER THE TIME RULES (RL-20260921-09), each mover EXACTLY ONE path: T10 and T14 close on L-ARRIVE with 1/1 and the ONE POSITION suffix, then one TASKCMPLT; T02 does NOT arrive and closes on L-STALL (ONE POSITION) with one TASKABRT "STALLED (C16 progress watchdog) ..." (S5; RL-20260913-03, RL-20260914-01). ZERO L-CANNOT; ZERO VACUOUS; exactly one terminal report per task (5 in all). T14's arrival limb is scored HIGH when P10a holds; if P10a misses (more members held by water), T14 closing on L-STALL instead is RECORDED as S5 with the held members named - the centroid's 500 m reach then depends on how many are held (sec 1(i)). | HIGH | Two paths, a TASKCMPLT before start + Duration for an unarrived mover, an arrival without the ONE POSITION suffix, T02 arriving, or T02 stopping with NO stall report (the P-FALS silent stop) = STOP. | |
| P12b | WHICH PATH: T10 and T14 late (b): L-TIMED OVERDUE at their end time (450 / 300 s), then L-ARRIVE, L-OVERDUE-ARR and TASKCMPLT "arrived after its task's end time - complete on arrival"; T02 OVERDUE at 300 s, then the stall. | MEDIUM | Another path = recorded MEDIUM miss. | |
| P13 | T14's REST (sec 1(i)): the container's closest approach to its destination inside the window is between 100 and 250 m (predicted ~146 m: the far-shore member's 1/17 pull), under outcome P11(a). | MEDIUM | Within 100 m (P11(b) or an unpredicted route) or beyond 250 m = recorded MEDIUM miss; the P11 outcome read with it. | |
| P13b | T10's REST: the container comes within 100 m of PassagePoint_48_IBCT_SLOT0 (every T10 leg, slot leg and unpack target is dry). | MEDIUM | Not within 100 m = recorded MEDIUM miss. | |
| P14 | T02 AT THE RIVER: 28ID's container never comes within 500 m of its destination; its last fix lies within 150 m of the river point 54.0087722, 23.2351231 and short of it along the line (<= 2,450 m along); the stall's "max" RECORDED. | HIGH (never within 500 m); MEDIUM (the stop point) | Within 500 m of the destination = STOP (the aggregate model crossed a MAK_WATERWAY river). A stop elsewhere = recorded MEDIUM miss, with the OSM features ahead (the scorer's). | |
| P15 | D-6: ZERO L-D6 WITHHELD for T10 and T14 - their arrival evidence (500 m) comes before any vendor completion (a completion before it would be the vacuous falsifier). T02: any L-D6 or hand-on line RECORDED (it prints only if a blocked move ENDS - the open question). | HIGH (T10, T14) | An L-D6 for T10 or T14 = STOP (read with P-FALS (b)). | |
| P16 | VENDOR COMPLETIONS inside the window: T14's container reports none (a snapshot member never finishes, sec 1(i)); T10's container reports one "VRF task complete: 1-112_IN/... / <type> (success=True)" and L-SWALLOW after its arrival evidence. The literal <type> RECORDED (expected "pa_move_along_route", lower case; KindLooksRight passes it, InFlightTracker.cs:96-101 - ZERO "attribution anomaly" lines). | MEDIUM | Another count = recorded MEDIUM miss (the ending of a blocked or retrograde vendor move is the open question). | |
| P17 | REPORT HYGIENE: ZERO SUPPRESSED; ZERO `task=(none)`; every SENT line's taskee is one of the three performers' C2SIM uuids (ZERO member TaskStatus - P-FALS (d)); capture and log agree; "Reports this run: ... 0 FAILED". The capture's ObservationReports RECORDED against the expectation 20 = 7 init R-SURFACE-PROXY + 3 re-announcements + 2 STP-866 holds + 8 pre-flight. | HIGH | Any limb = STOP. | |
| P18 | NO ENGAGEMENT: zero "Fire Weapon" / FireAtTarget lines - the nearest hostile container is over 40 km from every route (28ID's fire at will has nothing in range). | HIGH | Any = STOP. | |
| P19 | TILE READS: the run's tile total reads 0 HTTP FETCH(es), 0 given up, 0 undecodable; the cache manifest is unchanged after the pre-warm and after the run (682bdea4, 479). Hits RECORDED (harness: 12 slot + 49 route reads). | HIGH | A fetch or a changed manifest = STOP. | |
| P20 | RUN HEALTH: no BACK END LOST, no "Tick phase ... FAILED", no NO PERFORMING UNIT line (every container's live position is readable), no WS-runaway exit 6, no new .dmp / .callstack.log for this back-end pid (names only); VrfC2SimApp exits 0; rtiexec, rtiForwarder, rtiAssistant and every holder untouched. | HIGH | Any limb = VOID. | |
| P21 | TEARDOWN: StopVrf52 exit 0 or 6 inside the runner; no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left. | HIGH | Exit 3, 5 or 7 = STOP. | |
| P22 | CLOCK AND TIMINGS, RECORDED: every SIM/WALL RATIO line; each L-DISP SIMULATION stamp; each population's L-POP -> L-PUB WALL span (the gate is 85 s); each mover's dispatch -> L-ARRIVE / L-STALL in SIM s against sec 1(j); the holds' L-TIMED served figures; the watchdog's dormancy warnings, if any (above ~150x the ring cannot fill - then P12's stall limb is NOT SCORED and T02 is judged on P14 alone). | RECORDED | - | |
| P23 | THE FIRST LOAD (the pre-warm), RECORDED: the pre-warm's Stage 3 LaunchVrf52 READY and "scenario LOAD CONFIRMED" (or its 180 s WARN), the oracle-gate wait, READY TO TASK, the init L-PLACE counts and every L-PUB time; the same for the scored run; the sim cache listing before W, after W and after E (new layer directories, files, bytes). Expectation (MEDIUM): the pre-warm is the slower of the two on every one of those spans and adds at least one new layer directory; the scored run adds few or none. | MEDIUM | Recorded MEDIUM miss. | |
| P-FALS | THE FALSIFIERS - any one = STOP and ask, before anything else is run: (a) a SILENT stop of a populated container: a mover whose container stops for 360 SIM s or more short of its destination with NO L-STALL, NO L-D6 and NO refusal, while the watchdog was judging (no dormancy warning); (b) a container's MOVE task (T02, T10, T14 - not a hold) "completing" (a vendor completion, L-D6 or TASKCMPLT) within 60 SIM s of its dispatch with its centroid displaced 50 m or less; (c) ANY delete of a container or member (a DELETE-family line or a container uuid that changes); (d) ANY TaskStatus for a member. | HIGH | - | |

NAMED STOP-WITH-REPORT BRANCHES - recorded with their lines, the run continues (a branch other than S5 / S6 on T02 is
not predicted, so it is also a miss of the row it contradicts):
- S1 BRIDGE REFUSAL: "the loaded VrfBridge.dll has no PublishedSubordinateCount ..." (:2897-2900) - should NOT happen on
  5198ac45 (--populate-selftest p12 "CARRIES"); it would stop the start (P0).
- S2 POPULATION TIMED OUT: "POPULATE <c>: POPULATE TIMED OUT after 85 s: the container published 0 of N attached
  subordinate(s) - its MOVE tasks are REFUSED (TASKABRT) ..." and that container's move tasks REFUSED with TASKABRT.
- S3 CREATES LOST: "only n of N member(s) were created ... the rest are NOT part of this unit: [...]" (the rest attached
  and the move goes on with n).
- S4 ROW REFUSED: "COMPOSITION ROW ... is REFUSED" / "POPULATE <c> (...) REFUSED - ..." and the move refused with TASKABRT.
- S5 STALL WATCHDOG: L-STALL + TASKABRT "STALLED ..." on a container that stops (predicted for T02).
- S6 WATER STOP: a container (or a member) stopped at OSM water, with the pre-flight's report on the bus and, if its
  script then ends short, an L-D6 WARN "completed short: <d> m from the route end" (predicted for T02 at the river and
  for the INF1RIF2 member at lake 197345448).

WHY THE HIGH ROWS ARE HIGH: P1-P3e, P6 and P7 are the deployed code's own output, reproduced offline on the same data by
the harness (the same CompositionResolver, PopulatePlanner, VertexNudgeSearch, PreflightService and format strings) and
pinned by --populate-selftest (148 PASS); P4, P5, P8, P12 and P17 are E2-2's rows carried to the container path, whose
code differs only where sec 2 says; P10, P11, P12 (T02) and P14's first limb are the frame itself - the vendor's script
and mobility data (VENDOR CITATION) - and a miss there is a finding about the frame, which is why they stop the run.
MEDIUM rows are where the vendor's live behaviour is [A]: the console relay (P9), the sim's own water edges against the
OSM polygons M2 reads (P10a), the centroid rule on a live container (P10b), the rest points (P13, P13b, P14 stop point),
the vendor completions (P16) and the timings (P23).

STOP RULES:
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P20 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- P-FALS met, or P11 / P14's HIGH limb missed: STOP and ask, before anything else is run.
- The executor never intervenes in a window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as IRONSTORM_AGG_G1-<date>-2, with new appNumbers.

ONE VARIABLE: none - the first live run of the aggregate profile and of populated containers. E2-2 (20260927T231937Z,
the same ruled order on EntityLevel) is the REFERENCE for the order's geometry and the harness, not a control: the model
set, the unit representation, the terrain, the dispatch forms and the judges' position source all differ.

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5207 ***` in the main checkout and on this branch [V]. NOTHING is claimed by
this registration. At the go-live, from the marker M read then (5207 unless another run has moved it; the layout shifts
with it):
- 5207-5210: the G1 persistent holder (step D), HAND-CLAIMED in Appendix B BEFORE it joins (this branch commits the claim);
  marker -> 5211. Expected: 5207 CONSUMED on a first-attempt join; 5208-5210 BURNED.
- 5211-5221: the PRE-WARM runner block (step W), written by the runner at its Stage 2 - 5211 back end, 5212 front end
  (BURNED, --no-gui), 5213 WatchVrf pre-check, 5214 WatchVrf trace, 5215 VrfC2SimApp, 5216 RtiProbe 2c, 5217 CreateOne
  (BURNED unless the oracle gate fails), 5218-5221 Stage 2h holder attempts (5218 JOINS; 5219-5221 BURNED); marker -> 5222.
- 5222-5232: the SCORED runner block (step E), same layout - 5222 back end ... 5226 VrfC2SimApp ... 5229-5232 Stage 2h;
  marker -> 5233.
The layout is the PREP dry runs' (block M..M+10, marker -> M+11) [V]. A launch that aborts burns its whole block. PushInit
/ PushOrder / ListenReports / StopIface are C2SIM clients, not federates.

## 6. Harvest (after the runs, read-only) and where results go

From each run directory (runs\launch52\last-run-dir.txt): vrfc2simapp.log, reports-captured.log, c2sim-bus.log, the
manifest, watchvrf-trace.csv, thread-samples.csv, holder logs, stopvrf and launchvrf logs, the wrapper log; scratch
u3\laneG1\g1_score.py <runDir> (the pre-warm's too, as an observation); the harness outputs (harness_*.txt,
g1_predictions_h0.json) for the expected numbers; simcache listings. Vendor sim logs dump the environment in cleartext:
count-grep only, never quoted. Results go to: the Result block below (measurement and implication in separate
sentences); PLAN_MOVEMENT_2026-09-27.md rows G1 and C1 and sec 5 (the open questions G1 answers); RUNBOOK sec 11i ("NOT
live" -> what was seen); Appendix B annotated from the manifests. ASCII + CRLF.

## 7. What this run does NOT claim

- The map display RL-20260927-03 asks for ("all units to show on the map"): --no-gui, no GUI looks at the containers or
  their symbols (the design's P1 "drawn as a brigade" stays open).
- The authored variant, the derived SMS, the seven authored US types (G1b); nested containers (D-8 Flat only); the
  STP TO precedence (cut A's performers carry no TO subordinates, design sec 4.1); a patrol (PA_Patrol_Route) or a point
  move (unreachable, sec 1(f)).
- Combat, posture, supply or any aggregate warfare-model outcome (no hostile within 40 km); the members' fire at will.
- Rivers solved (T02's river is reported, not crossed); that the members' convergence to vertex 0 is how a doctrinal
  brigade moves; the timing of anything beyond n = 1, one host, one fixture; the sim/wall ratio of the aggregate profile
  in general.
- Anything about the entity-level profile, the full 23-task order, the demo server or the demo profile.

## Result (written after the harvest, never from a live read)

Written 2026-09-28 ~11:10Z by lane G1 (session 5fc25950) from the harvested files of the scored run 20260928T102541Z_run
and the pre-warm 20260928T101531Z_run (main checkout runs\): vrfc2simapp.log (184,134 lines; L = its line numbers),
reports-captured.log (C = record number; 859 records), run-manifest.json, watchvrf-trace.csv (t = trace seconds from the
WatchVrf join at 10:28:09.792Z), the runner logs runs\launch52\RunScenario-ironstorm-agg-g1-20260928T102540Z.log (R) and
...-prewarm-20260928T101530Z.log (RW), the holder log runs\launch52\g1-holder-20260928T101448Z.log (H) and the rtiexec log
(count-grep only). Instruments, scratch u3\laneG1: g1_score.py (score_g1.txt, run by the seat), g1_harvest.py
(harvest_scored.txt, harvest_prewarm.txt), capture_census.py, trace_g1.py, centroid_check.py, name_collision_check.py,
simcache_windows.ps1, golive_checks_g1.ps1 and the harness (re-run offline on T14's live origin); u3\laneE2-2
rtiexec_count.py. Vendor sim logs were not opened. "reg. L" = the row's line in the registration as committed (3af710a),
before the STATUS edit and this Result. [V] = checked in the harvest; [A] = taken from the seat or inferred.

GO-LIVE, as sec 3 wrote it, with these differences [V unless marked]:
- A (golive_checks_g1 -Phase preholder): 03:42:26Z and again 10:07:35Z, 0 FAILED (main daaf679; every registered hash;
  cache 682bdea4 / 479 files; REST 200; the RTI trio up; no RtiProbe; marker 5207).
- C3 RUN BY THE SEAT. The Claude Code permission classifier refused this lane's c3_push.sh twice ("Production Deploy");
  the seat ran the same script unchanged under the owner's authorisation RL-20260928-01, 10:09:32-10:10:08Z. Evidence
  (u3\laneG1\c3_*): PushInit "before : UNINITIALIZED", "QUERYINIT : 40 Units, SystemName=[Not Set]"; PushOrder one
  "[10:09:38.697] ORDER (69670 chars)"; the bus log carries 5 ManeuverWarfareTask, 5 Duration and TaskActionCode
  CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT. Exit codes 0 / 0 per the seat [A].
- D, W, E AND F RUN BY THE SEAT under RL-20260928-01, the classifier having refused this lane's Appendix B write ("Modify
  Shared Resources"). The hand-claim (this lane's claim_holder.ps1) was applied to both copies at 10:14:26Z, before the
  holder's first attempt (H 10:14:49Z); it is committed on this branch with this Result, not before the join as sec 3 D
  wrote. H "HOLDER JOINED: pid 33476 appNo 5207" at 10:14:58Z; the holder CREATED the federation (the E1 holder had
  resigned; rtiexec count-grep 06:14:45-06:15:05 local: "Sending Create Response = Success"). W: the pre-warm launched
  10:15:30Z, runner exit 0, StopVrf exit 6, no REFUSING TO START, cache manifest unchanged. E: dry run 10:25:04Z exit 0
  (block 5222-5232), THE RUN launched 10:25:40Z, the ORDER on the bus 10:28:44.178Z, the window closed EARLY at
  10:32:27Z (192.8 s of the 2700 s cap, R263-R264), runner exit 0 at 10:35:10Z. F: golive_checks_g1 10:48:36Z: every
  hash and the cache manifest unchanged, only the RTI trio and holder 33476 (resigns 18:14:49Z) up - the run's own Stage
  2h holder 54324 had left after its 900 s hold - and the marker 5233.

### VERDICT: STOPPED at P3 (HIGH). VR-Forces created all 23 members, but the interface attributed one: every aggregate-level name longer than 31 characters comes back cut to 30 (FINDING N1), and the members' 34-character names collide at 30 with their container and with each other. The registered branches then ran exactly: S2 for 28ID and 1-112 IN (TIMED OUT, their moves refused with TASKABRT), S3 for 48 IBCT (1 of 17 attached, the move went on with it). THE MECHANISM RAN END TO END WITH ONE MEMBER (N2): populated in place, attached, published in 0.3 s, T14 issued as the container's own PA_Move_Along_Route through RunScriptedTask, the vendor script drove the member 4.4 km round the lake the pre-flight shifted it past, completed 9 m from the route end, D-6 handed it on, TASKCMPLT. P-FALS (a)-(d) did not fire. HIGH misses: P3, P3c, P4 (one limb), P5, P6 (T14's limbs), P7, P10, P12 - every one downstream of N1 (P6, P7 and P12 through the moved route origin, verified).

| # | Reg. | Verdict | Evidence |
|---|---|---|---|
| P0 | L453 | PASS | (i) R78 holder 33476 "a PERSISTENT FEDERATION HOLDER" (RW78 in the pre-warm); rtiexec count-grep 06:25:40-06:35:10 local: 12 "Could not create federation ... because it already exists.", 0 creates, 12 JoinConfirm; L65 "READY - joined the federation" once. (ii) L12 "BUILD IDENTITY: git 9027548"; every hash identical at A (10:07:35Z) and F (10:48:36Z). (iii) R19-R22 the model-set, type-map and fixture pairing lines; manifest inputs.modelSet AggregateTacticalLevel. (iv) 0 REFUSING TO START, 0 "AGGREGATE CONTAINERS off". (v) L48 "Vrf:TaskPredecessorTimeoutSeconds=600 s"; L544 (T02) and L556 (T14) "and then 600 s to COMPLETE". (vi) the pre-warm's gate held: RW277 StopVrf EXIT=6, its app log 0 REFUSING TO START, the cache manifest unchanged; the scored run's Stage 1 inventory (R75-R79) saw only the RTI trio and holders 33476 / 42016. |
| P1 | L454 | PASS | L14 L-FIDELITY (62 rows); L16 L-CAT (705 templates, 3 model-set dirs, root AggregateTacticalLevel, C:\MAK\vrforces5.2d); L18 L-VARIANT ("5 of the 8 row(s)", "SMS: Vrf:Scenario is not set.", "7 authoredRows type(s) ... NOT in use"); L20-L28 five L-ROW 1/0, 17/3, 5/0, 26/6, 8/1, 0 REFUSED; L30 L-CON-ON ("hostile nation RUS", "(5 row(s) resolve, 0 refused)", "85 s", "100 m"); L42 L-MODELSET; L40 L-M1-ON; L32 L-WDOG-ON (360 s, SIMULATION, 50 m); L48 L-CLOCK (SIMULATION, 0.25, 600, 60, 86400); L38 L-SHIFT-ON; L558 L-OSMSET once (225 tile files, 0 empty; 225, 0), after the first L-POP (L540) and before the first L-DISP (L58346); 0 "is not EntityLevel or AggregateTacticalLevel". L77 and L560 are other lines (the init's type-mapping line; "ROUTE PRE-FLIGHT enabled"), not repeats. |
| P2 | L455 | PASS; limb (f) FLAGGED for the seat | (a) 36 TYPE MAP ... CONTAINER lines L81-L171: 29 Exact, 7 Proxy "NEAREST branch"; L165 48 IBCT -> BDE (11.1.225.8.3.1.1), L123 1-112 IN -> BN, Light Infantry (11.1.225.6.3.1.0), L89 28ID -> DIV, Mech Infantry (11.1.225.9.4.1.0) Proxy; 11 of 11 hostile containers nation 260. (b) 0 L-NOCONT. (c) 7 L-PXYTAG (L79-L167). (d) L179 "36 container(s) ... 0 other aggregate shell(s); 0 platform(s)", no entity census; L189 "36 object(s) ... (36 empty shell(s))"; L197 "36 units + 5 areas"; L195 "7 PROXY substitution(s)". (e) 36 container names bound by L-BIND before the order (L532), 0 member names. (f) 0 DELETE-family lines in the window. FLAG: L183045 "Cleanup: deleting 48 created VR-Forces objects before resign..." after L183041 "Application is shutting down..." - the resign-time teardown every run prints (E2-2 L582395, 42 objects), after the window; by the letter "at init or later" it is a DELETE-family line, and that reading is the seat's. RECORDED: L528 READY TO TASK "36 of 36 ... after 6.9 s"; L285 "36 of 36 create altitude(s) came from the TERRAIN QUERY"; 0 ORDER BEFORE READY TO TASK. |
| P2b | L456 | PASS | L175 / L423 28ID/III_Corps... "will attach 9 declared child unit(s)" / "composed - 9/9"; L177 / L401 278_Armored_Cavalry_Regiment... 1 / "1/1". |
| P3 | L457 | MISS (HIGH) - STOP | Held: one L-POP each at order receipt - L540 28ID ("task 'T1_..." performer"), L546 1-112 IN ("T10_..."), L552 48 IBCT ("T13_...") - source 3, rows, map rows, N and radius as registered (1 / 0 m; 5 / 153 m; 17 / 490 m, "3 sub-container(s) ... flattened"), the member lists of sec 1(i); N L-SLOT lines each (L578; L584-L592; L596-L628); one L-ISSUED each (L580, L594, L630); one L-REANNOUNCE each (L542, L548, L554). Missed: 0 L-ATTACHED; one L-PUB, for 48 IBCT with 1 not 17 (L58486 "PUBLISHES 1 subordinate(s) (expected 1) 70.5 s after the population began, 0.3 s after the attach"); 1 of 23 member names bound (48_IBCT/28ID__FRIENDLY_INFANT.CAV1) - the 22 others hit 22 "truncation of MORE THAN ONE name" lines (L695-L826) and 17 "NAME REBIND REFUSED" (L697-L828); 2 TIMED OUT (L58326, L58328) and 1 partial (L58330), branches S2 and S3 below. RECORDED: L-POP order 28ID, 1-112 IN, 48 IBCT; no HELD or deferred line; member placements L640 (1 of 1), L656 (5 of 5), L692 (17 of 17) from the TERRAIN QUERY. |
| P3b | L458 | PASS | 28ID L578 clear; 1-112 IN L584-L592 5 clear; 48 IBCT 13 clear + slot 4 L602 "SLOT MOVED 125 m north-west", slot 5 L604 "125 m south-west", slot 6 L606 "25 m south" (OSM 197345448), slot 16 L626 "75 m south-east" (OSM 16373225); 0 KEPT, 0 UNVERIFIED; every slot position the harness's. |
| P3c | L459 | MISS (HIGH) by the letter | 48 IBCT's L-PUB L58486 precedes T13 (L58508) and T14 (L81482). 28ID and 1-112 IN printed no L-PUB: T01 dispatched (L58346, hold-in-place) after 28ID's TIMED OUT (L58326), and there were two memberless-move refusals (T10 L58856, T02 L81502). Both are S2's registered consequences; no move reached a container with no published member. |
| P3e | L460 | PASS | 0 "is NOT populated - D-5" lines; 116_ABCT/... has no L-POP. |
| P4 | L461 | MISS (HIGH) on one limb | T13: L-DISP L58508 after L-PUB L58486; L58514 L-CNFPSL; L58516 L-INPLACE; L58512 armed 300 "no destination"; C302 STP-866; L81062 TASKCMPLT "(300 s after dispatch)". T01: the same lines (L58346, L58352, L58354, L58350, C299, L81068) - but after 28ID's TIMED OUT, not after an L-PUB (the S2 path). 0 routes or scripts for a hold, 0 OVERDUE for a hold, one TASKCMPLT each. RECORDED: 48 IBCT's L-INPLACE position is (54.02349,23.31120), its one member's slot, 490 m NNW of the harness centroid - the container had moved onto CAV1 at the attach (P10b). |
| P5 | L462 | MISS (HIGH) | 3 SENT TASKSTRT (T01 L58348, T13 L58510, T14 L81484), not 5: T10 and T02 got TASKABRT instead (L58858, L81504). T14's L-DISP L81482 after T13's TASKCMPLT L81062 and L550 T10 "start delay 300 s" held; no L-DISP for T10 or T02 (refused); 0 SKIPPED; 0 gate-expiry lines. RECORDED: T10 refused 10:29:55.07Z (C306), T14 dispatched 10:30:22.104Z, T02 refused 10:30:22.29Z (C426). |
| P6 | L463 | MISS (HIGH) on T14's limbs; the stage VERIFIED on the live route | T02 and T10 exactly as the harness: L81096 "(2 vertices)", L58374 "(4 vertices)"; L-VCHECK L81408 1 / L58662 3 checked, 0 moved / kept / unverified; L-ELEV L81410 L12 x1, L58664 x3; L-SLOW L81416 3370 of 5342 m, L58672-L58676 805/1451, 103/804, 369/516 m; L81414 "OSM 8011072 (waterway=river (MAK_WIDTH 5 m))" first at (54.00877,23.23512) 2.40 km along; L81418 RIVER CROSSING; L58678 L-NOFLAG (T10). T14 held L81086 "(3 vertices)", L81224 2 checked 0 / 0 / 0, L81226 L12 x2 - and missed: leg 1 was 1654 m, not 1270 m, and FLAGGED (L81234 OSM 197345448 on the line at 0.61 km; L81230 land-cover WATER ON THE LINE; L81240 ROUTE SHIFTED 125 m south; L81244 PRE-DISPATCH applied, "route 3 -> 7 vertices"; L81236 slow 1179 of 1654 m); the capture holds 11 pre-flight ObservationReports, not 8 (C303-C305, C381-C388). Cause [V]: the route origin is the container's live position, which was its one member's slot (54.023492,23.311196), not the 17-member centroid; the harness re-run offline on that origin (harness_predispatch_liveorigin6.txt; the cache copy's manifest unchanged) prints every T14 line above to the metre - leg 1 1654 m flagged at 0.61 km, slow 1179 of 1654 m, shift 125 m south, 5 reports - and the run's own tile total, 53 hits. |
| P7 | L464 | MISS (HIGH) | T14 only: L81488 "CreateRoute '...' (7 pts) for CONTAINER 48_IBCT/... - 1 published member(s); RunScriptedTask PA_Move_Along_Route deferred to route-created"; L81509 "RunScriptedTask PA_Move_Along_Route issued for CONTAINER 48_IBCT/... (VRF_UUID:151c17e5-...) with [route=VRF_UUID:bc8fec43-..., reverseDirection=false, startAtClosestVertex=false] - 1 published member(s)"; L81482 kind "PA_Move_Along_Route". The form and the variables held; the point count (7, not 3 - the shift) and the member count (1, not 17) did not, and T02 / T10 issued none (refused). 0 "was NOT issued", 0 PA_Move_To_Location_Direct, 0 MoveAlongRoute, 0 MOVE TO PER VERTEX, 0 formation lines for a container. L-CONSOLE-MEMBERS: 1 distinct member on each of 4 passes (L58506, L81084, L81260, L81478); 28ID and 1-112 IN "publishes NO members at task time" (L58344-L81496, 7 lines). |
| P8 | L465 | PASS | T02: one L-FAW (L81500, on its last pass, before the refusal L81502) and the R3 line once per pass (L81094, L81430, L81498); T14: L81480 L-FOLSPT "'ROEHold'"; T10: L-BARE L58370, L58694, L58854, one per pass; 0 L-FAW for another task. |
| P9 | L466 | MISS (MEDIUM); the level-4 relay is now [V] | T14: L81555 "Init of aggregate move along." (1073.123) on the container's console; L81557 one "Sub. 48_IBCT/28ID__FRIENDLY_INFANT. starting move-along." - 1, not 17 (the vendor names the member by its 30-character name); T02 / T10 none; 0 "starting pa_move_along_route". RECORDED: L132737 "has completed move task." (1687.383), L132949 "has completed final move." (1689.383). |
| P10 | L467 | MISS (HIGH) | 48 IBCT's container displaced 2,059.8 m after its L-DISP (trace, from t=131.4); 28ID's and 1-112 IN's 0.0 m. Members moved > 50 m: 28ID 0 of 1, 1-112 IN 0 of 5, 48 IBCT 1 of 17 (CAV1, 2,059.8 m). The 22 unattributed members stayed on their planned slots: each first fix 0.0 m from its L-SLOT position, every one displaced 0.0 m. |
| P10a | L468 | MISS (MEDIUM) | 1 of 17, not 16. The near-water first legs were never driven: those members were never attached or tasked. |
| P10b | L469 | PASS for 48 IBCT; NOT MEASURABLE for 28ID and 1-112 IN (MEDIUM miss) | 48 IBCT: 0.0 m between the container and its one member at the last common instant (t=255.9); at the attach (t=105.1) the container's own fix jumped 490 m onto the member's slot. RECORDED: while moving the container's fix trailed the member by up to 150.2 m (t=182.5; mean 15.8 m over 75 common instants). 28ID and 1-112 IN never had an attached member. |
| P11 | L470 | PASS (untested mechanism) | 48_IBCT/28ID__FRIENDLY_IN.INF1RIF2 (VRF_UUID:a3393f30-..., identified by its first fix 0.0 m from slot 4) never came nearer than 498.0 m to (54.019341, 23.313809). Outcome (c): never attached or tasked, it stayed on its slot - the lake case was not exercised. |
| P12 | L471 | MISS (HIGH) | T14 closed on neither L-ARRIVE (0 ARRIVAL EVIDENCE lines) nor L-STALL but on its vendor completion: L132955 "VRF task complete: 48_IBCT/... / pa_move_along_route (success=True)", L132957 "... COMPLETE with the container 9 m from the route end - handed on to the task's completion rules (D-6, ...)", L132959 "was OVERDUE and the unit has now ARRIVED", L132961 TASKCMPLT "complete on arrival". T10 and T02: TASKABRT refusals (L58858, L81504), not arrival or stall. Held: 0 L-CANNOT; 0 VACUOUS-completion lines (the VrfC2SimService.cs:6075 / :6100 family - the refusal text "never dispatched as a vacuous success" is not one, and g1_score.py's "VACUOUS x1 L40" is its own regex matching the M1-ON line); one terminal report per task, 5 in all. WHY NO ARRIVAL EVIDENCE [V: arithmetic and ArrivalPolicy.cs:184-215]: from the live origin the route (terrain profile reply 70, L81434, 7 points) is 4,628 m and its last vertex 2,061 m from the dispatch position - under half the route - so SF-1 refuses the per-member relaxation and the bar is the route's, 2,314 m, more than any straight-line travel to the destination; on the registered origin (2,424 m of a 4,172 m route) the relaxation applies. |
| P12b | L472 | MISS (MEDIUM) | T14: L106647 OVERDUE ("308 s of a 300 s Duration served"), then L132959 / L132961 complete on arrival - through the vendor completion, with no L-ARRIVE. T10, T02: refused. |
| P13 | L473 | MISS (MEDIUM) | T14's container rested 0.5 m from its destination (trace; 9 m at the vendor completion, L132957): with one member its offset from the container is 0, so its unpack target is the route end. |
| P13b | L474 | MISS (MEDIUM) | T10 never dispatched. |
| P14 | L475 | HIGH limb PASS (untested); stop point MISS (MEDIUM) | 28ID's container stayed at its start, 5,346.6 m from its destination and 2,401.4 m short of the river point: never tasked, so neither a river stop nor a detour was exercised. |
| P15 | L476 | PASS on the registered limb; its stated reason did not hold | 0 L-D6 WITHHELD for T10 / T14. T14's vendor completion came first (there was no arrival evidence, P12) and, 9 m from the route end, took the hand-on twin (L132957). T02: no L-D6 line. |
| P16 | L477 | MISS (MEDIUM) | T14's container reported one completion (L132955), T10's none (refused). RECORDED: the literal type "pa_move_along_route", lower case, as expected; 0 "attribution anomaly". |
| P17 | L478 | PASS | 0 SUPPRESSED; 0 task=(none); all 8 SENT lines' taskees are the three performers (200d3a3f x3, dd3d21b2 x4, 8d5b2ba6 x1); 0 member TaskStatus; capture = log (8 TaskStatus, C300-C643; 859 records); L184129 "Reports this run: 859 delivered, 0 FAILED". RECORDED: 23 ObservationReports against the expectation 20 - 7 init PROXY (C1-C7), 3 re-announcements (C44-C46), 2 STP-866 (C299, C302) and 11 pre-flight (T14's moved origin added three). |
| P18 | L479 | PASS | 0 Fire Weapon / FireAtTarget lines. |
| P19 | L480 | PASS | L184131 "53 cache HIT(s), 0 HTTP FETCH(es), 0 tile(s) given up on after 3 attempts, 0 undecodable body(ies)"; the cache manifest 682bdea4 / 479 at F, unchanged since A. RECORDED: 53 hits - the harness on the live origin reads 53. |
| P20 | L481 | PASS | 0 BACK END LOST, 0 "Tick phase ... FAILED", 0 NO PERFORMING UNIT; runner exit 0; R272 "VrfC2SimApp exited with code 0 (clean resign)"; 0 .dmp / .callstack.log under C:\MAK\logs or C:\MAK\vrforces5.2d newer than 10:15Z (names only); the RTI trio and holder 33476 untouched. RECORDED: the thread sampler raised BACK-END WS RUNAWAY in both runs (pre-warm 1468.1 MB/min at 10:16:55Z, scored 666.4 MB/min at 10:27:06Z, both at ~2.57 GB during the load; manifest backendWsRunaway=true) - an alert, not an exit. |
| P21 | L482 | PASS | R281 StopVrf EXIT=6: FORCED by identity after the graceful close was refused (R282); no sim, app or observer left (10:48:36Z). |
| P22 | L483 | RECORDED | L-RATIO 9.144 (L39924, sim 556.7 s), 10.173 (L90069), 9.698 (L139895); L-DISP SIM stamps T01 / T13 759.0 s (L58346, L58508), T14 1059.4 s (L81482); populations: 70 s to TIMED OUT (28ID, 1-112 IN), 70.5 s to L-PUB (48 IBCT); T14 dispatch to vendor completion 630 SIM s (1059.4 -> 1689.4), trace t=132.3 -> first fix within 500 m t=192.6, within 100 m t=194.7; the holds served 300 of 300 s (L81058, L81064); no dormancy warning. |
| P23 | L484 | RECORDED; MEDIUM MISS on one span | Pre-warm / scored: LaunchVrf, launch to READY and "scenario LOAD CONFIRMED", 72.6 s / 51.2 s; app start to order push 35.4 s / 16.3 s; oracle gate 108 / 72 real-coordinate POS lines (both passed); READY TO TASK after 16.0 s / 6.9 s; the init's L-PLACE 0 of 36 from the TERRAIN QUERY, 36 FALLBACK / 36 of 36 from the TERRAIN QUERY; 48 IBCT's L-PUB 70.5 s / 70.5 s - EQUAL, both set by the create deadline, not by the load. Sim cache (simcache_after_scored_windows.txt): the pre-warm created two layer directories, Elevation-MAK-Earth-b64925878feb653c (328 files, 83,106,082 B) and Worldwide-Aggregate-Imagery-c890009d2720aec2 (129 files, 6,678,193 B), and wrote 2 files to __default; the scored run wrote none. |
| P-FALS | L485 | NOT FIRED | (a) no silent stop: from dispatch to arrival 48 IBCT's container moved at least 61.8 m between every two consecutive trace fixes (2.0-2.1 WALL s apart, 31 steps); T02 and T10 were refused and reported; no dormancy. (b) T14's completion came 630 SIM s after dispatch at 2,059.8 m; T02 and T10 closed on refusals, not completions. (c) 0 DELETE-family lines in the window; the three performers' uuids unchanged across the trace; the resign-time cleanup is flagged under P2 (f). (d) 0 member TaskStatus. |

BRANCHES (reg. L487-L499): S1 not seen. S2 SEEN for 28ID and 1-112 IN, at 70 s - the create deadline, the 85 s bound
less its 15 s publication window (ContainerPopulation.cs:422-428) - with "NONE of its N member(s) was created" where the
registration wrote "published 0 of N attached"; both move tasks REFUSED with TASKABRT, as the branch says. S3 SEEN for
48 IBCT: L58330 in the registered wording, and the move went on with 1. The creates were not lost in VR-Forces: all 22
exist, unattributed (N1). S4, S5 and S6 not seen: the one member driven went round lake 197345448 on the shifted route
without stopping.

N1 - THE CAUSE [V]. Measurement: in both runs VR-Forces returned the name of every aggregate-level object whose requested
name is longer than 31 characters cut to exactly 30 - 58 of 58 logged returns (36 correlated to their requested names +
22 unattributed; 75 lines); the one 31-character name (4ID__FRIENDLY_INFANTRY_DIVISION) was bound with no truncation
line, i.e. returned whole (Bind logs every resolved truncation, VrfC2SimService.cs:6493-6498). VR-Link sizes the
aggregate marking at 31 bytes (vl/aggregateStateRepository.h:34 "Aggregate state PDU marking is 31 bytes long";
vlpi/netStructs.h:51). The interface plans names to 34 characters (MaxVrfMarkingChars, VrfC2SimService.cs:9484-9488,
from 5.0.2's UUID blob; PopulatePlanner.MaxNameChars, ContainerComposition.cs:700-716, "<container, trimmed>.<suffix>"
within 34), so a member's name cut to 30 is either its container's own cut name (each HQ1) or shared with its siblings
(48_IBCT/28ID__FRIENDLY_IN.INF1 for INF1RIF1-3 and INF1WPN1): ambiguous, or already bound, and refused
(NameRegistry.cs:203-219, :279-294). From the requested names alone, under that cut, exactly one member is attributable -
CAV1, "48_IBCT/28ID__FRIENDLY_INFANT." (name_collision_check.txt) - and exactly CAV1 was. Competing hypotheses, falsified:
the creates failed (all 22 exist in the trace, each on its planned slot to 0.0 m); the publication read failed (48 IBCT
published its one attached member 0.3 s after the attach). Implication: no container whose members' 30-character names
are not unique can be populated, and the same 34-character assumption governs the proxy tag ("~PXY") and every other name
the interface asks for on the aggregate model set. The fix is C1c: plan aggregate-level names within 30 characters and
unique at 30 (or bind members by uuid), then G1-2.

N2 - THE MECHANISM, LIVE, n = 1 [V]. Measurement: populate in place -> AddToOrganization -> publication (1 of 1 in 0.3 s)
-> RunScriptedTask PA_Move_Along_Route on the container -> the vendor script's four console lines -> the vendor completion
-> D-6 hand-on -> TASKCMPLT; the container's own position followed its member (the 490 m jump at the attach, 0.0 m at
rest, trailing up to 150 m while moving). Implication: the design's [A]s on the remote scripted-task path, the publication
read and the level-4 console relay are closed for one member; the 17-member convergence, the lake and river stops, the
unpack round the route end and D-6's WITHHELD branch are still untested.

N3 - THE ARRIVAL JUDGE ON A ROUTE THAT DOUBLES BACK [V, P12]. Measurement: from the one-member origin, T14's last vertex
lay 2,061 m away on a 4,628 m route, so ARRIVAL EVIDENCE could not fire and the vendor completion closed the task.
Implication: a container that starts nearer its destination than half its route closes only on its vendor completion -
handed on within 100 m, withheld beyond (D-6, RL-20260927-04), then the time rules (RL-20260921-09).

N4 - THE FIRST LOAD [V, P23]. Measurement: the pre-warm's init took every one of its 36 create altitudes from the FALLBACK,
the scored run's from the terrain query, and the pre-warm created the two aggregate-earth cache layers. Implication: the
pre-warm was needed; a cold scored launch would have created the init containers at fallback altitudes.

UNEXPLAINED OR ASSUMED: the container's in-motion lag behind its member (up to 150.2 m) [A: the vendor's aggregate
publication rate or dead reckoning]; why a 31-character name survives whole while 32 and more are cut to 30 [A: the
vendor's truncation code is not read]; L81507 "Can't create data of type pa_move_along_route. No creator found." (raw
vendor SDK output, once; the same benign family E2-2 printed for other types; no effect seen).
