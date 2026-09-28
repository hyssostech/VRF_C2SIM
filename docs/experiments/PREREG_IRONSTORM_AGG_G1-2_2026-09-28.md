# PREREG - IRON STORM ON THE AGGREGATE PROFILE, RUN G1-2: G1 re-run on the C1d build (IDENTITY BY UUID) - the ruled cut-A order on POPULATED AGGREGATE CONTAINERS (one unscored pre-warm + one scored run)

STATUS: STOPPED at P12 (HIGH) - scored 2026-09-28 from run 20260928T142731Z_run, holder branch (a) (Result below):
identity by uuid HELD live (P3; RL-20260928-02) - 59 of 59 objects created under the requested uuid and bound by it,
all 23 members attributed, every population published in 1.7 s; T10 arrived and T02 stopped at its river, as
predicted; T14 did not arrive - 11 of 48 IBCT's 12 Mech COs stopped inside an OSM building of the -2 hamlet on the
vendor's slope check and the watchdog closed T14 771 m short (a HIGH miss by the letter; MEDIUM misses P12b, P13,
P23). WAS: REGISTERED - LAUNCH PENDING (seat's go-live). Registered by lane G1-2 (session 5fc25950; the registration
commit's own time is the stamp) on branch run/ironstorm-agg-g1-2-c1d from main 699552c, BEFORE any order push, holder
action or launch. (The branch name run/ironstorm-agg-g1-2 is held by the stopped first attempt's worktree
agent-ab30be5806cc7f1f3 at 84c4f62 - an ancestor of 699552c with no commit of its own, stopped at RL-20260928-02; this lane
did not touch it.) PREPARATION ONLY: no C2SIM push, no holder start, no VR-Forces or runner launch, no appNumber claimed,
no C:\MAK write (the fixture is G1's, already deployed and unchanged). Marks: [V] = checked while writing this file; [A] =
taken from the record or assumed, not re-checked here. This file is G1's registration (PREREG_IRONSTORM_AGG_G1_2026-09-28.md
secs 0-7) with ONLY the changes the G1-2 brief names; every src line number cited is re-verified at 699552c [V].

## Registration

PREREG ID: IRONSTORM_AGG_G1-2026-09-28-2
DATE (UTC): 2026-09-28, before any launch (the registration commit's own timestamp is authoritative; the launch waits
for the seat's go-live)
BINARY / COMMIT: the DEPLOYED main-checkout build 1.0.0+git.699552c.Release-5.2 (not DIRTY) - NEW PIN 2026-09-28 13:34Z
(RUNBOOK sec 9; the deploy lane re-pinned from main 699552c; the previous pin 5198ac45 is superseded): VrfBridge.dll sha256
03226dd0e48dae0869ce2fa5c8f3ac521603f2f9d5206ced04c14ad228dd14c5 (1,005,568 B, 13:34:11Z) in ten of the eleven trees - the
app, SmokeTest, CreateOne, CreateTaskAgg, PauseSim, ResetVrf, RunSim, SetAlt, SetSimRate, WatchVrf [V, all eleven hashed
13:5xZ] - while tools/RtiProbe's held tree still carries 5198ac4585bf4218d4c6048cb6713876f9c464e415ce50ac83afcc2b9da2131a
until holder 33476 resigns (its code is unchanged and it calls neither new overload, so branch (a) below is unaffected;
branch (b)'s new holder is started only from a RtiProbe tree the seat has REBUILT IN PLACE after 33476 resigns, sec 3 D0).
VrfC2SimApp.exe sha256 7824483a169870ba0ec3541f5cbe9ea587f61cc72fa02fe41b856fd6d71c78c3 (162,304 B); VrfC2SimApp.dll
sha256 bb7e6361e63e4cd395e511b71ff5c938bf37fa161d368633d5c62aa16c9b24a5 (1,997,824 B); deployed appsettings.json
0f600f0eb2a205773db5a1bfe5077aba981c3b954e0de5e4e86526683e259210 = G1's, appsettings.Demo.json 12391080...6fb8f6ca [V,
golive_checks_g1_2.ps1 13:56:40Z, 0 FAILED]. Offline suites on that deployed exe, from the deploy lane's record [A]: name
77, populate 189 PASS / 0 FAIL / 4 SKIP with (p15) "the LINKED VrfBridge.dll CARRIES CreateEntity(..., String uuid) and
CreateAggregate(..., Boolean, String uuid)"; RunnerTurnaround 751/0/1. src at 699552c = C1d (fix/identity-by-uuid
fa9736f, merge 699552c; RL-20260928-02) + C1c (84c4f62) + D2 (47b3b7b; RL-20260927-06, RL-20260928-01) on G1's 9027548.
`git diff --stat 9027548 699552c -- src` [V]: VrfBridge.cpp, VrfBridge.vcxproj, VrfFacade.cpp/.h, and in VrfC2SimApp
ContainerCatalogue.cs, ContainerComposition.cs, ContainerSelfTest.cs, IdentityUuid.cs (new), NameRegistry.cs,
NameSelfTest.cs, UnitTranslator.cs, VrfC2SimService.cs, VrfNames.cs (new) - every other src file G1 cites keeps G1's line
numbers. `git diff --stat 1efbbe7 699552c -- scripts tools data config` [V]: RunC2SimScenario.ps1, RunScenario.sh,
RunnerLib.ps1 (D2) and six R9 / L2 / L3 / GA inits (the RL-20260928-01 test data) - no Iron Storm data file, no tool.
main was at df9aca1 at registration: `git diff --stat 699552c main -- src` EMPTY [V] (the NEW PIN record and docs only).
NOT rebuilt by this lane.
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: G1's, unchanged (UG52 72.2.1 p1419; UG52 Table 68 p1470; UG52 18.1 p438; UG52 27.1.4; UG52 30.22 p598 step 5
and 30.24 p599; UG52 35.5.7 p734; AggregateLevelBase\scripts\PA_Move_Along_Route.lua :26-91, :95-149, :152-157;
appData\settings\featureconfig.txt :261-262, :266-267, :413-414, :423; VRFSIM.Aggregate.feature.model.xml :323-352;
mech-aggregated-movement.sysdef :110-131 and motorized-aggregated-movement.sysdef :109-130; Mech CO (USA, M2).entity :96,
:439-440; Stryker  HHT (USA).entity :74, :409-410; Stryker Cavalry SQDN (USA).entity :157, :493-494 - see G1's registration
for what each says), and for the one variable, IDENTITY BY UUID: UG52 13.2 Table 21 p362-363 (the UUID is "Unique" and
"Persists from exercise to exercise"; the Name has limits on its length and does not have to be unique) [A: the C1d record,
RUNBOOK sec 11i and RL-20260928-02's reading]; and [V, read-only] vrfcontrol\vrfRemoteController.h (5.2) :1282-1293 createEntity and :1295-1306 createAggregate - both take
`const DtUUID& startingUUID = DtUUID::nullUUID()` (:1292, :1305); vrfmsgs\ifCreateVrfObject.h:105 "Use this UUID to create
object (if specified). If the UUID exists will be regenerated on creation"; vrfutil\uuid.h:77-84 "If string is a UUID
(VRF_UUID:) then sets a valid UUID from the string, else will have an invalid UUID ... if the string given is an object
marking text (not UUID) ... lookup to map the marking text to the UUID" - the header documents only the VRF_UUID:-prefixed
form, while the facade passes the BARE 8-4-4-4-12 form (VrfFacade.cpp:960, :976, the same conversion as CreateControlArea
:1013), which G1 saw honoured for a CONTROL AREA (BUFFALO under its own C2SIM uuid) and never for an entity or aggregate:
that is the live-owed claim, P3. VR-Link 5.10 vlpi\netStructs.h:43, :51 and vl\aggregateStateRepository.h:33-34 (the 11-
and 31-character marking fields - VrfNames.cs:10-12).

OWN-RECORD CITATION:
- Rulings: G1's, unchanged - RL-20260927-01 (movement approach; the pre-flight per model set), RL-20260927-02 (hostile side
  RUS; populate the containers, not proxies), RL-20260927-03 (every unit an EMPTY container at init; only a tasked unit is
  populated, in place), RL-20260927-04 (D-1..D-8 as recommended, D-2 revised; D-6 the withhold), RL-20260927-05 (Q1a),
  RL-20260927-06 (above battalion = aggregate only), RL-20260921-09 (the temporary completion position), RL-20260913-03 and
  RL-20260914-01 (a stuck unit = report + TASKABRT), RL-20260925-01 (stall detection ON in the demo profile), RL-20260926-01
  (a UNIT's ATTACK is fire at will), RL-20260920-01 (route shift ON) - and, new for G1-2: RL-20260928-01 ("D2 as
  recommended. Go for the server" - the model-set rule with no downward override, and the private validation server for G1
  and its successors) and RL-20260928-02 (object identity is the UUID; every create passes a startingUUID; names are display
  and the secondary key - C1d).
- docs/PLAN_MOVEMENT_2026-09-27.md CLOSED list and rows C1, C1c, C1d, D1, D2, M2, G1, G1-2; docs/experiments/
  PREREG_IRONSTORM_AGG_G1_2026-09-28.md (the registration copied here, its Result and the seat's readings of P2(f), P15 and
  P3c vs S2); RUNBOOK secs 9 (NEW PIN 2026-09-28 13:34Z), 9c, 11i (C1c corrected and the C1d IDENTITY lines), 11j (D2),
  12, 12a; docs/AUDIT_REPEATED_INSTRUCTIONS_2026-09-28.md items 1, 2, 5; docs/DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md.
- IRONSTORM_AGG_G1-2026-09-28-1 (G1, STOPPED at P3, run 20260928T102541Z_run): THE CONTROL - the same order, fixture, init,
  composition and procedure on the 9027548 build; its harness outputs (scratch u3\laneG1) are this run's geometry.
  IRONSTORM_CUTA_E2-2026-09-27-2 (E2-2) stays the entity-level reference for the order's geometry.

## 0. Purpose, in plain words

G1-2 re-runs G1 - the ruled cut-A order on VR-Forces' AGGREGATE model set, every Iron Storm unit an EMPTY Aggregate
Container at init (36, hostile RUS), the three performers populated IN PLACE when the order arrives (28ID 1 member, 1-112
IN 5, 48 IBCT 17, FLAT, on one centroid-preserving ring), each advance the container's own PA_Move_Along_Route - on the
build that fixes what stopped G1. G1 stopped at P3 because VR-Forces returns an aggregate's name over 31 characters cut to
30 and the interface bound created objects BY NAME, so 22 of its 23 members were never attributed. Two fixes are in the
deployed build: C1c plans every member name within 30 characters, unique at 30 (48_IBCT/28ID__FRIENDL.INF1RIF1 ...); and
C1d (RL-20260928-02) creates every entity and aggregate UNDER A UUID - an init unit under its own C2SIM uuid, a container
member under an RFC 4122 v5 uuid of '<container uuid>/<suffix>' - and binds each ObjectCreated BY THAT UUID, the name
being display and the secondary key. THE RUN'S FIRST HIGH PREDICTION IS THE LIVE-OWED CLAIM: VR-Forces returns the
requested uuid for ENTITIES and AGGREGATES as it does for control areas.

ONE VARIABLE: the deployed build (identity by uuid + the C1c names + D2's runner-side model-set choice, whose exports the
app reads). Everything else is G1's: the order, the fixture (no fixture deploy), the init, the composition, the harness
geometry, and the runner sequence C3 / D / W / E / F with G1's command lines - new are only `--model-set auto`, the script
and log names and the appNumbers. THE HEADLINE is still 48 IBCT's 17-member brigade on T14, now with every member
attributed.

WHAT THE PREPARATION FOUND, said before the run: G1's harness geometry stands unchanged (C1c changed the NAMES, not the
ring - PopulatePlanner's geometry, the slot check and PreflightService are untouched 9027548..699552c [V]): T14's and T10's
lines water-clear on the centreline, T02's straight leg crossing OSM river 8011072 at 2.40 km (a named stop-with-report
branch, S6), four 48 IBCT slots moved off water, the far-shore member INF1RIF2, unpack targets in lake 197345447. And, read
from the code at 699552c: because an object bound by its uuid never prints the truncation WARN (VrfC2SimService.cs:6731
`if (bind.Truncated && !bind.ByUuid)`), the 35 init containers whose names overflow the 31-character field show their cut
markings inside the IDENTITY lines and NOT as the G1-style "VRF returned created object ... for the name we requested"
WARN - which prints 35 times only in the branch where the uuid is NOT honoured (S7).

## 1. Decisions taken from the record

(a) THE ORDER - G1's, unchanged: the RULED cut-A order, data/IRONSTORM_CUTA_Order.xml sha256
7a9861372f07702fc136f91f63b8bf971650fcc91c20aa62803d73cfd869f5c7 on init 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d
7614f3d26dc93eec [V]. G1's reasons stand (its sec 1(a)); the five tasks (G1's table, the deployed exe's --parse-order [A: the
parsers and both files are unchanged 9027548..699552c]):

| Task (uuid) | performer (container) | verb -> decision | members | route (pts incl. the live origin) | start | armed end (SIM s) |
|---|---|---|---|---|---|---|
| T01 f7b52ba4 | 28ID__FRIENDLY_INFANTRY_DIVISION (DIV, Mech Infantry - NEAREST branch, PROXY) | CNFPSL -> held in place (STP-866) | 1 | not driven (4 graphics) | delay 0, after 28ID publishes | 300, no destination |
| T02 696fbb33 | 28ID (same) | ATTACK -> a UNIT's ATTACK: advance with fire at will (RL-20260926-01; TaskDispatchPolicy.cs:134) | 1 | 2: origin -> PassagePoint_28ID_SLOT0 54.028874, 23.264401 (5,341 m) | after T01's TASKCMPLT | 300, destination |
| T10 9aab7fe6 | 1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE (BN, Light Infantry - exact) | CRESRV -> bare move | 5 | 4: origin -> (h) 54.029734, 23.305499 -> (j) 54.024, 23.313 -> PassagePoint_48_IBCT_SLOT0 (2,771 m) | delay 300 SIM s (1200 s x 0.25) | 450, destination |
| T13 37677c40 | 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE (BDE - exact) | CNFPSL -> held in place | 17 | not driven | delay 0, after 48 IBCT publishes | 300, no destination |
| T14 1075b583 | 48 IBCT (same) | FOLSPT -> advance along the graphic and hold, ROEHold | 17 | 3: origin -> (i) 54.0146, 23.3315 -> 54.040348, 23.324206 (4,172 m) | after T13's TASKCMPLT | 300, destination |

(b) SETTINGS, each with its reason - G1's, but for the model set.
- FIXTURE IronStorm_Centre_52_Aggregate - G1's, deployed by lane G1 at 2026-09-28T02:57:41Z and unchanged since: sha256
  804e2c393dcf5f4fe4e1c58d7423a343c4e43250cd719d5474e035750c76a0e3 [V 13:56Z]. NO fixture deploy; nothing under C:\MAK is
  written by this registration or its go-live.
- MODEL SET: `--model-set auto` (D2, RL-20260927-06; the rule with no downward override, RL-20260928-01), where G1 passed
  `--model-set AggregateTacticalLevel` explicitly before D2 merged. The runner's Select-ModelSetByEchelon (RunnerLib.ps1
  :2143; its line :2232) reads the order's PerformingEntity values - 28ID (DIV, 2 tasks), 1-112 IN (BN, 1), 48 IBCT (BDE, 2)
  - and the highest, DIV, is above BN: AggregateTacticalLevel, aggregate-only. Verbatim from the seat's dry run
  2026-09-28T11:22:58Z (scratch u3\laneG1-2_dryrun_auto.log :19, :28, :32 [V]; RunC2SimScenario.ps1 :2365, :2376, :2643-2644
  print them): "model set   : AggregateTacticalLevel <- auto (RL-20260927-06): highest TASKED echelon DIV
  (28ID__FRIENDLY_INFANTRY_DIVISION) is ABOVE BN - aggregate-only; exported to the app as Vrf__ModelSet", "fixture
  C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Aggregate.scnx loads AggregateTacticalLevel
  (aggregatetacticallevel.sms) - matches the chosen model set." and "catalogue   : not exported - the app keeps the default
  "" (the model set's own SMS)". The type map follows the choice (data/unit-type-map-52-aggregate.json sha256
  c546edbe...6190, unchanged [V]). D2's NEW EXPORTS, which G1's runner did not make (dry run :123-125 [V]):
  Vrf__Scenario=C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Aggregate.scnx and
  Vrf__CompositionVariant=catalogue (the value appsettings.json already gives). The app reads Vrf__Scenario for the C1b guard
  and SAYS it: L-VARIANT's "SMS:" part now reads "the fixture <scnx> loads $(DATA_DIR)\simulationModelSets\
  AggregateTacticalLevel.sms" (ContainerComposition.cs:359-365, :446-447) where G1's read "Vrf:Scenario is not set." - a
  registered consequence of the variable, not a change of what the app does with the catalogue variant (it needs no
  derived SMS either way, :435-436).
- COMPOSITION: G1's - data/unit-composition-52-aggregate.json sha256 9684e945...3bae [V], variant `catalogue`. How the app
  reads it: ContainerStartupPreflight, VrfC2SimService.cs:2996-3121.
- CLIENT ID "Not Set"; DURATION SCALE 0.25; SUCCESSOR-GATE FLOOR 600 s (Vrf__TaskPredecessorTimeoutSeconds=600; the wrapper
  exports 7200, scripts/RunScenario.sh:338, and applies --env after it) - G1's.
- PRE-ORDER GATE: NONE - G1's reasons: the runner offers NavArea or nothing (RunC2SimScenario.ps1 :2273-2280; RunScenario.sh
  :294-298) [V], the aggregate model loads no nav data, and the app's own barriers hold an early order (D5b; MaterializeUnit
  VrfC2SimService.cs:2773-2780; every C1 task waits on its container's publication gate, :4877-4895).
- CONSOLES --object-console 4 --member-console 4; WINDOW -StopWhenComplete with the 2,700 s cap; ENV Vrf__StallDetection=true,
  Vrf__StallClock=sim, Vrf__TaskPredecessorTimeoutSeconds=600; --sample-threads; --no-gui; SERVER the private 18080 / 61614
  (RL-20260928-01); TEARDOWN GATE StopVrf52 exit 0 or 6 - G1's.
- NOT passed (shipped values): route shift ON (RL-20260920-01), the pre-flight and its cache location,
  Vrf:PlatformMoveToPerVertex, Vrf:CatalogueSms (empty = the installed AggregateTacticalLevel.sms under C:\MAK\vrforces5.2d),
  Vrf:ContainerPopulateTimeoutSeconds (0 = derived 85 s) - G1's.

(c) THE PRE-WARM - G1's procedure, unchanged: the scored command with `--run-secs 120`, its own appNumber block, the order
pushed, UNSCORED, dry run first. It is kept for single-variable discipline and as THE W GATE (sec 3 W) - no longer as a
first load: the sim cache already holds the aggregate earth's layers G1's pre-warm created (Elevation-MAK-Earth-
b64925878feb653c, 328 files, 83,106,082 B, newest 10:20:57Z; Worldwide-Aggregate-Imagery-c890009d2720aec2, 129 files) -
listing u3\laneG1-2\simcache_prep.txt, 13:59:58Z, 25 layer directories, 21,805 files, 495,598,329 B [V, names and sizes
only]. P23's expectation changes with that fact.

(d) POPULATION HAPPENS AT ORDER RECEIPT, for every performer, in the order file's task order - G1's, with 699552c lines:
OnOrder calls MaterializeUnit for each task's performer before the task orchestration starts (VrfC2SimService.cs:4706-4708),
MaterializeUnit is once per unit (:2759) and on the aggregate set goes to PopulateInPlace (:2786-2790). The POPULATE line's
"why" names the FIRST task when the population runs at once; when it is HELD for the init (:2773-2780) or DEFERRED to the
container's ObjectCreated (DeferPopulate :3226-3234, re-entered from OnVrfObjectCreated :6768-6769 with " (shell
reflected)"), it may name T2 / T14 instead. EVERY task of a container - the holds included - waits on the population gate
before it dispatches (:4877-4895, bound = 85 s + the barrier + 15 s), so "READY FOR TASKING" precedes T01's and T13's
dispatch, and T10 dispatches at its own 300 SIM s start delay. A population HELD for the init's creates
(DispatchReadiness.cs:366-372) runs when the init settles - RECORDED.

(e) ORDER VALIDATION (C1 / C2, offline) - G1's results [A: G1's [V] on 9027548; OrderParser, InitParser and both data files
are unchanged 9027548..699552c]: --parse-order "Tasks: 5" ...; --parse-init "Units: 40", "would create (clientId=Not Set):
36", 5 areas, 19 lines. C3 is the one real push (sec 3).

(f) T02'S FORM - G1's finding, with 699552c lines: CreateRoute (2 pts) + PA_Move_Along_Route (routeGeo from the live origin
first, VrfC2SimService.cs:5328-5331; the task's points after the origin-vertex drop, :5433-5458; VertexChainPolicy.FormFor
returns SinglePointMoveTo only for <= 1 point, VertexChain.cs:91). PA_Move_To_Location_Direct (IssueContainerPointMove,
:3403-3418) is unreachable for any container task with geometry - predicted ZERO times (P7).

(g) M2 ON THE AGGREGATE MODEL SET - G1's harness, unchanged (scratch u3\laneG1\harness; PreflightService, VertexNudgeSearch
and the options are untouched 9027548..699552c [V: no Preflight file in the src diff]; GetPreflight's options are now
VrfC2SimService.cs:7048-7066, ShiftOptions :7577-7587). Route origin = the container's centroid after the slot check (sec
1(i)). Per task, as G1 registered: T14 2 checked, 0 moved, legs 1,270 m / 2,902 m not flagged, EXPECTED SLOW 1,270 of
1,270 / 1,663 of 2,902 m, "no leg flagged", L12 x2, 2 reports; T10 3 checked, legs 1,451 / 804 / 516 m none flagged, EXPECTED
SLOW 805 / 103 / 369 m, "no leg flagged", L12 x3, 3 reports; T02 1 checked, leg 1 5,341 m FLAGGED - OSM 8011072
(waterway=river, MAK_WIDTH 5 m) first wet at 54.0087722, 23.2351231 (2,398.9 m along), EXPECTED SLOW 3,370 of 5,342 m, "NO
ROUTE SHIFT - RIVER CROSSING", dispatched as authored, L12 x1, 3 reports; 8 pre-flight ObservationReports in all; 49 + 12
cache hits, 0 fetches. G1's consequence for T02 stands: a river line IS MAK_WATERWAY, IMPASSABLE for the motorized Stryker
HHT that is 28ID's one member, so 28ID is predicted to stop at the river (P14) and T02 to end on the stall watchdog (S5).

(h) THE CONTAINER SCRIPT - G1's reading of PA_Move_Along_Route.lua, unchanged (ContainerPopulation.cs:50-55 sends
startAtClosestVertex false, :54 [V]): each member drives from its ring slot to the route's FIRST vertex, then along the
route, then to route end + its rotated offset; the task ends when every snapshot member did both.

(i) THE RING AND THE MEMBERS' OWN LEGS - G1's geometry, with the C1c NAMES and the C1d UUIDS. The names are what the
planner makes at 699552c - VrfNames.ChildName (VrfNames.cs:80-88: the container cut to 30 - 1 - suffix length, then
".<suffix>"), cleared by NameRegistry.KeyConflict (NameRegistry.cs:254-281) through PopulatePlanner.Plan
(ContainerComposition.cs:733-786); replicated offline on the 699552c init (u3\laneG1-2\names_check.py -> names_check.txt:
the three populations planned against the init's units and its 24 graphics - 23 names, 23 distinct, max 30, no ~k tag [V])
and pinned by --populate-selftest p14 (ContainerSelfTest.cs:1206-1264, the 36 created units of this init included). The uuids are IdentityUuid.Derive (IdentityUuid.cs:77-82), recomputed with Python's
uuid.uuid5 - the independent oracle NameSelfTest pins (NameSelfTest.cs:293-308: 48 IBCT HQ1 d003da2c..., CAV1 46c65670...,
28ID HQ1 c8d5c7b5... - reproduced [V], u3\laneG1-2\expected_identity.txt):
- 28ID (C2SIM uuid 200d3a3f-8f36-4951-9459-c748527896ba): row C-USA-DIV-UCI, 1 member, radius 0 m, slot clear:
  28ID__FRIENDLY_INFANTRY_DI.HQ1 = c8d5c7b5-89b5-5584-9ae9-ec2e5a958ff4 (Stryker  HHT (USA)).
- 1-112 IN (8d5b2ba6-73c1-6c55-812c-7c8078ea8c97): row C-USA-BN-UCI, 5 members, radius 153 m, 5 slots clear:
  1-112_IN/28ID__FRIENDLY_IN.HQ1 = 0daa745e-886f-5d86-bdf2-30aca5cea357; 1-112_IN/28ID__FRIENDLY_I.RIF1 =
  fe0c3275-93b8-5f63-bb34-688639fa59c5; ...RIF2 = 081d4816-9dbd-5f2f-bb9b-42cbff62b529; ...RIF3 =
  17f86e4f-1cc6-5e47-93ad-ab4251574ba5; 1-112_IN/28ID__FRIENDLY_I.WPN1 = 36393c4e-feea-54d3-9cb1-4275efb99de7.
- 48 IBCT (dd3d21b2-c5e0-d45a-9fba-b4b8bb879e6a): row C-USA-BDE-UCI, 17 members (4 x Stryker  HHT (USA), 12 x Mech CO (USA,
  M2), 1 x Stryker Cavalry SQDN (USA)), radius 490 m, 3 sub-containers flattened, in slot order:
  48_IBCT/28ID__FRIENDLY_INF.HQ1 = d003da2c-d813-5e00-ae78-3fbfbfe7dd71; 48_IBCT/28ID__FRIENDLY.INF1HQ1 = 41972fdb-53f2-5072-
  a418-29c4b6213e6b; 48_IBCT/28ID__FRIENDL.INF1RIF1 = 7ad21e0a-b586-51bb-9b13-6b2d1a92bc65; ...INF1RIF2 = dc54d487-a6d4-584f-
  b3aa-65f7714b1558; ...INF1RIF3 = 11e9a58b-ca44-5267-a9cc-8387608da26f; ...INF1WPN1 = 112f0667-be35-51ef-984f-ef98719c5718;
  48_IBCT/28ID__FRIENDLY.INF2HQ1 = e0322c1a-10ea-5b76-a3bf-d79ac6344769; 48_IBCT/28ID__FRIENDL.INF2RIF1 = 7d3b191e-5af7-5168-
  a8a6-be287eeef16c; ...INF2RIF2 = b1a69cc1-a22e-550b-b62f-6767d9866b6d; ...INF2RIF3 = 91126c4d-c686-5dfc-8897-90e865219eaf;
  ...INF2WPN1 = 9fed08a6-f3e4-57d5-97e9-1c8528326e6f; 48_IBCT/28ID__FRIENDLY.INF3HQ1 = 8a68b22c-25a7-5e06-af02-a24695b7186c;
  48_IBCT/28ID__FRIENDL.INF3RIF1 = bc0a7c9d-4f9f-5c99-b6ea-849f977dd0ff; ...INF3RIF2 = dca189c8-6712-5fd2-8eea-36e88f591ed4;
  ...INF3RIF3 = 2c1b8987-bc29-57c0-ab70-97fc99d80722; ...INF3WPN1 = b6c4756d-e5c1-5d13-a1c9-508222bee540;
  48_IBCT/28ID__FRIENDLY_IN.CAV1 = 46c65670-fcf0-5fe2-b0da-3e90b753db71. SLOTS 4, 5 and 6 (INF1RIF2, INF1RIF3, INF1WPN1) lie
  IN lake 197345448 and SLOT 16 (INF3WPN1) IN pond 16373225: "SLOT MOVED 125 m north-west", "SLOT MOVED 125 m south-west",
  "SLOT MOVED 25 m south", "SLOT MOVED 75 m south-east"; 13 clear (G1's P3b held live, to the metre). The moved slots shift
  the members' centroid 8.1 m, to 54.019341, 23.313809 - T14's route origin.
- THE FAR-SHORE SLOT: 48_IBCT/28ID__FRIENDL.INF1RIF2's moved slot (54.022284, 23.319544) is on the far side of lake
  197345448 from vertex 0; its leg to vertex 0 meets the lake 8 m out - predicted never to reach vertex 0 (P11). The near-
  water first legs (INF1RIF1 4.9 m, INF1RIF3 5.4 m, INF1WPN1 7.5 m, INF3WPN1 1.8 m) and the unpack targets in lake 197345447
  (INF2WPN1, INF3HQ1; heading 0 registered, ContainerCatalogue.cs:209-214, heading 0.0 at :210) are G1's.
- Consequence (G1's): T14's PA_Move_Along_Route cannot end while a snapshot member stays short unless the vendor's blocked
  move ENDS; the centroid then rests about 146 m from the destination (P13).

(j) TIMINGS, estimated, not scored - G1's (T14 route end ~1,800 SIM s after dispatch, the centroid within 500 m ~1,600-1,900
SIM s; T10 ~800-950 SIM s; T02 the river ~300-600 SIM s, then a 360 SIM s stall window).

(k) NAMES, PROXY TAGS AND MARKINGS. The seven NEAREST containers are PROXY (G1's list); each name + "~PXY" still exceeds the
34-character marking-text limit the proxy tag is bounded by (VrfSettings.cs:107; MaxVrfMarkingChars = 34,
VrfC2SimService.cs:9780 - C1c kept that budget, PLAN row C1c), so each gets "Proxy marking tag NOT appended to '<name>' ..."
(:1624-1626) and keeps its name; the 7 go to the report stream (:2260). Every populated container is re-announced once
(AnnounceSubstitution, :2952-2967; the line :2961). C1c at init: every unit name is checked unique within 30 before the
request (:1642-1662) - none of the 36 collides (p14 (g): every unit of the 13 shipped inits unique within 30; the 36
cut markings listed by the scorer's --expected are distinct [V]), so 0 "NAME DISAMBIGUATED (C1c)" and 0 "NAME COLLISION
(C1c)". THE MARKINGS VR-Forces returns (the measured rule, VrfNames.cs:64-68,
G1 58 of 58): 35 of the 36 init names are longer than 31 and come back as their first 30; 4ID__FRIENDLY_INFANTRY_DIVISION
(31) comes back whole; every member name (30 at most) comes back exactly.

(l) THE TO TWINS AT INIT - G1's (ApplyHierarchyComposition, VrfC2SimService.cs:2400-2483; the lines :2479 and :2534):
28ID/III_Corps__TWO_EIGHT_TH_US_INFANTRY_DIVISION gets its 9 created TO children, 278_Armored_Cavalry_Regiment/28ID... gets
4th Sqn 278th; EMPTY containers attached to EMPTY containers (D-7); nothing is deleted. 28ID keeps its authored point
(DeStacker.cs:260). G1's P2b held live.

(m) THE TILE CACHE: the deployed <exe dir>\preflight-cache, NOT re-staged - 479 files, manifest
682bdea48f43b6f33d0c34339c6ec269efd786965d81ccd66e4aa7f30ac85b6d [V 13:56Z], G1's and E2-2's.

(n) THE CONTROL: G1 (run 20260928T102541Z_run) - the same order, fixture, init, composition, settings and procedure; the
build differs (and the runner's model-set source and its two new exports, sec 1(b)). What G1 left untested because of the
naming defect - the 17-member convergence, the lake and river stops, the unpack, D-6's withhold limb - is what this run
reaches. The scorer's control on G1's own files (u3\laneG1-2\control_g1_scored_score.txt, control_g1_prewarm_wgate.txt)
reads G1 as branch MIXED with 0 IDENTITY lines and 36 truncation WARNs, and its W gate FAILS on G1's pre-warm - the fail-
first of this registration's instruments on real data [V].

(o) IDENTITY - WHAT THE CODE AT 699552c DOES (C1d, RL-20260928-02) [V]. The start-up refuses a VrfBridge.dll without the
two uuid overloads (VrfC2SimService.cs:637-645; IdentityBridge.Present, IdentityUuid.cs:120-137) and otherwise says the
rule once (:646-652). Every init unit's plan carries its own C2SIM uuid (:1572-1576; UnitTranslator.cs CreationPlan
StartingUuid, :39-44), every member's plan the derived uuid (PopulatePlanner.Plan's containerUuid, VrfC2SimService.cs
:3164-3168; ContainerCatalogue.cs:213-214); EnqueueCreates registers each name AND its uuid before any create
(:2300-2330; RequestIdentity :2341-2356) and calls the overloads with it (:2323-2327; VrfFacade.cpp:954-981 passes it
bare). OnVrfObjectCreated binds by the uuid first (NameRegistry.BindCreated, NameRegistry.cs:596-602; TryBindByUuid
:553-587) and says per object "IDENTITY: '<requested name>' created as uuid <uuid> (requested) marking '<returned
marking>'" (VrfC2SimService.cs:6722; IdentityLines.CreatedAs, IdentityUuid.cs:144-145 - the uuid printed bare,
lower-case); a callback under a uuid we did not request falls through to the name rule and WARNs "IDENTITY: VR-Forces
returned uuid <uuid> for marking '<m>', not one we requested - bound by name (NameRegistry)" (:6728-6730); the old
truncation WARN prints only when the object was NOT bound by its uuid (:6731-6736). After READY TO TASK the census of the
init's own objects is said at INFO when all are bound by uuid, else WARN (:3752-3761). At plan time each population says
its member uuids once (:3198-3204); when every member has its ObjectCreated, the population's census (:6817-6825); each
member says once how it came back and how it was bound (:6803-6813). A task completion and a POSITION text report carry
only a marking, resolved marking -> uuid -> name first (ResolveReportMarking :9263-9269; OnVrfTaskCompleted :9280;
NameRegistry.ResolveMarking :344-358). The container gate and the PA_* scripted tasks already addressed the container by
its uuid; with C1d that uuid IS the C2SIM uuid (branch A), so G1's VRF-generated container uuids (e.g. 48 IBCT's
VRF_UUID:151c17e5-...) become VRF_UUID:dd3d21b2-c5e0-d45a-9fba-b4b8bb879e6a in L-BIND, L-SCRIPT and the WatchVrf trace.

(p) WHOSE POSITION THE JUDGES READ (P12, read at 699552c) [V]. The arrival evidence, the stall watchdog and D-6's distance
all read ONE position for a container: its OWN reflected centre point. TryReadUnitPositions (VrfC2SimService.cs:8513-8537)
asks the bridge for the aggregate's MEMBERS, and VrfBridge.GetAggregateMembers lists ENTITIES only (VrfFacade.cpp:1068-1131:
the aggregate state's entity designators, recursing into published sub-aggregates for THEIR entities, never returning a
sub-aggregate itself); a container's members are warfare-model UNITS created AGGREGATED, so the list is empty
(VrfC2SimService.cs:5039-5044 says so, and takes the console members from the population's record instead) and
UnitPositionPolicy.SourceFor returns AggregateLeaf on the aggregate model set (UnitPositionPolicy.cs:96-101): "total = 1"
and the position is TryGetEntityGeodetic(<the container's uuid>) (VrfC2SimService.cs:8527-8536) - the position VR-Forces
publishes for the container, the unweighted mean of its immediate subordinates (design Q2). ARRIVAL EVIDENCE therefore
reads "1/1 member(s)" with the ONE POSITION suffix (:8020-8039, suffix :8036-8038); the stall watchdog the same
(:9159-9173, suffix :8839-8841); D-6 measures the same fix against the route end (:3431-3432). No member's own position is
read by any judge. Consequence: while the container moves its fix TRAILS its members - G1 measured up to 150.2 m behind its
one member (2 s fixes, mean 15.8 m) - so an arrival is scored when the CONTAINER's fix, not the leading members, is within
500 m of the last vertex and past its traversal bar.

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: none (sec 1(b)); the order is pushed right after the runner's stage-7 oracle gate.

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: the watchdog runs on the SIMULATION clock (--env Vrf__StallClock=sim), window 360 SIM s,
50 m. Armed ends in SIM s from dispatch: T01 and T13 300 (no destination - never watched); T02 and T14 300 and T10 450
(destination tasks: an unarrived one goes OVERDUE and stays watched; nothing is SENT at that end). A stall verdict is
possible from dispatch + 360 SIM s on, for T02 / T10 / T14 only, on each container's ONE position (D1; sec 1(p)). T14's
convergence phase (members closing on vertex 0 from a 490 m ring) leaves the centroid nearly still for ~130 SIM s, not 360
(the Strykers pass vertex 0 first) - G1's sec 1(j).

DEVIATION FROM RECORD: the template's "PRE-ORDER GATE: --pre-order-gate nav-area" ("gate PushOrder on the first "New Primary nav area" row; warm the area first") is not used - the aggregate model set loads no nav data, so that row can never print and the gate could only time out (AGGREGATE_PROFILE_OFFLINE sec 6.4); the app's own D5b hold and the C1 publication gate cover an early order. (G1's, unchanged.)

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels. (G1's, unchanged.)

DEVIATION FROM RECORD: the successor-gate floor is 600 s - appsettings.json's shipped value - not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:338); as G1 and E2-2.

DEVIATION FROM RECORD: the window closes EARLY under -StopWhenComplete, where E1 registered "--no-stop-when-complete --run-secs 2700"; as G1 and E2-2, the 2700 s cap is kept.

DEVIATION FROM RECORD: the pre-warm is a whole runner launch that also pushes the order and runs 120 s, where DEMO_RUNBOOK sec 0.4 says "let it reach the initialization, then stop it"; there is no init-only runner mode (LIVE1 sec 1(e)). (G1's, unchanged; here it is the W gate, sec 1(c).)

DEVIATION FROM RECORD: the design's G1 outline ("Order: cut A reduced to T13 -> T14 on T14's ORIGINAL line", DESIGN_AGGREGATE_CONTAINERS sec 9) is replaced by the ruled cut-A order at the seat's direction; the original line is G2's. (G1's, unchanged.)

DEVIATION FROM RECORD: in holder branch (a) the RtiProbe tree the runner's Stage 2c / 2h use keeps the superseded bridge 5198ac45 while every other tree carries the NEW PIN 03226dd0 - RUNBOOK sec 9 calls a partial deploy "the trap" ("A PARTIAL redeploy is the trap: the tools and the app then disagree about what the bridge can do"). Accepted here because RtiProbe only joins, holds and resigns - its code is unchanged and it calls neither new overload (the deploy lane's reading) - and because its tree cannot be rebuilt while holder 33476 holds it open. Branch (b) has no such deviation (sec 3 D0).

DEVIATION FROM RECORD: G1's go-live check demanded 60 minutes of hold left on the persistent holder at each pre-launch check; in branch (a) this registration demands 20 (golive_checks_g1_2.ps1 -MinHoldLeftMin 20) - the persistent holder only has to outlive each run's Stage 2h join (RUNBOOK 9c: the run's own holder then holds MAK-ONE-2025 for 900 s and the sim and app keep it for the rest of the run), and 60 would make the ~17:40Z branch-(a) cut-off of sec 3 impossible for E (60 minutes before 18:14:49Z is 17:14:49Z). Branch (b) keeps 60.

EFFECT OF -StopWhenComplete ON THE WINDOW - G1's: the runner closes it once all 3 taskees and all 5 tasks have a TERMINAL
report, 60 s have passed and every taskee has post-completion position evidence (Test-EarlyExit, RunnerLib.ps1:328).
T02's terminal report is predicted to be its stall TASKABRT and T14's its arrival-evidence TASKCMPLT (~1,600-1,900 SIM s
after its dispatch), so the window closes about a minute after the later of the two; a member still unpacking then, a
vendor completion that comes later, and T14's final rest are unobservable; P13's "closest approach" is read inside the
window.

## 2. What the code emits - log-line shapes (src at 699552c = the deployed build; VrfC2SimService.cs unless named) [V]

START-UP, in this order (ExecuteAsync :587-1155):
- L-BUILD (:594) `BUILD IDENTITY: git 699552c, ...`.
- L-FIDELITY (:612) `Type-mapping mode = FidelityTable (62 rows from <...>unit-type-map-52-aggregate.json); FriendlyNation=USA, OpposingNation=RUS; SurfaceProxySubstitutions=True.`
- L-CAT (:2600, from GetResolver inside ContainerStartupPreflight) `AGGREGATE CONTAINERS: catalogue loaded from C:\MAK\vrforces5.2d (root AggregateTacticalLevel, 705 templates, 3 model-set dir(s)) - the init rule and the composition resolve against it (RL-20260927-03).`
- L-VARIANT (:3077; ContainerComposition.cs:429-448) `COMPOSITION VARIANT catalogue (Vrf:CompositionVariant; RL-20260927-04): 5 of the 8 row(s) of <...>unit-composition-52-aggregate.json (its own and the 'all' rows: C-USA-DIV-UCI, C-USA-BDE-UCI, C-USA-BN-UCI, C-USA-BDE-UCA, C-USA-BN-UCIZ). SMS: the fixture C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Aggregate.scnx loads $(DATA_DIR)\simulationModelSets\AggregateTacticalLevel.sms. Catalogue: AggregateTacticalLevel. It needs no derived SMS: catalogue units only, which resolve alike on the shipped and the derived set. 7 authoredRows type(s) in the type map, NOT in use (they serve the 'authored' variant only).` - CHANGED FROM G1 by D2's Vrf__Scenario export (sec 1(b)): the "SMS:" part (Describe, ContainerComposition.cs:359-365).
- L-ROW (:3096) five lines `COMPOSITION ROW <row> (map rows [<maps>]): <N> simulated leaf unit(s), <K> sub-container(s) flattened (D-8, RL-20260927-04): [...]` - G1's: C-USA-DIV-UCI [F-UCI-I] 1/0; C-USA-BDE-UCI [F-UCI-H] 17/3; C-USA-BN-UCI [F-UCI-F] 5/0; C-USA-BDE-UCA 26/6; C-USA-BN-UCIZ 8/1. REFUSED twin (:3089) - none.
- L-CON-ON (:3102-3119) `AGGREGATE CONTAINERS ON (Vrf:ModelSet=AggregateTacticalLevel; ...) ... (hostile nation RUS; ...) ... (5 row(s) resolve, 0 refused) ... for up to 85 s (Vrf:ContainerPopulateTimeoutSeconds, derived) ... A vendor completion farther than 100 m (Vrf:VertexArrivalRadiusMeters) from the route end is WITHHELD (D-6). ...` Its refusal twin (:3060-3061) `AGGREGATE CONTAINERS (...) - REFUSING TO START: ...` and ContainerPopulation.cs:499-503 `AGGREGATE CONTAINERS off (` must not print.
- L-IDENTITY-ON (:646-652) `IDENTITY (C1d, RL-20260928-02): every entity and aggregate is CREATED UNDER A UUID and BOUND BY IT - an init unit (and its ~PXY proxy) under its own C2SIM uuid; a container member, a synthesized sub-unit and a template re-create under an RFC 4122 v5 uuid of '<parent uuid>/<suffix>' in namespace 485b28e7-1cc7-534b-b6a6-9beddf07a1b1 (the same every run). The name is display and the SECONDARY key: ...` at INFO, after L-CON-ON (ContainerStartupPreflight runs first, :633). Its refusal twin (:637-645) `IDENTITY (C1d, RL-20260928-02) - REFUSING TO START: the loaded VrfBridge.dll has no CreateEntity/CreateAggregate taking a uuid, ...` at CRITICAL must not print (P0).
- L-WDOG-ON (:697) `PROGRESS WATCHDOG ON (C16, report-only): a 360 s no-progress window on the SIMULATION clock, 50 m of net displacement per member, ...`; L-CACHE (:754) `ROUTE PRE-FLIGHT TILE CACHE: <exe dir>\preflight-cache - ...`; L-SHIFT-ON (:803); L-M1-ON (:835-836; VertexChain.cs:113-131); L-MODELSET (:840-842; UnitPositionPolicy.cs:109-125) `MODEL SET for task judging: Vrf:ModelSet='AggregateTacticalLevel' -> AggregateTacticalLevel (D1, RL-20260927-01). A MEMBERLESS aggregate ... counts as ONE position ...` at INFO; L-DESTACK (:852); L-APPROACH (:868) `ARRIVAL APPROACH FRACTION 0.50 ... Radius 500 m ...`; L-CLOCK (:926) `TASK CLOCK (R4): C2SIM task times are measured on the SIMULATION clock ... Vrf:DurationScale=0.25; a successor waits max(Vrf:TaskPredecessorTimeoutSeconds=600 s, ... Vrf:TaskPredecessorEndMarginSeconds=60 s) ... Vrf:TaskChainBackstopSeconds=86400 s ...`. Q1a (RL-20260927-05) adds no start-up line; its gate line (:8470) prints only when a gate window expires.
- L-OSMSET (:7075) at the pre-flight's FIRST USE - the first population's slot check (CheckPopulateSlots :3295 calls GetPreflight at :3298), i.e. at order receipt, before any dispatch: `ROUTE PRE-FLIGHT MODEL SET AggregateTacticalLevel (Vrf:ModelSet; RL-20260927-01) - ... OSM features read from <cache>\osm-water (225 tile file(s), 0 of them 0 bytes = UNKNOWN) and \osm (225, 0 0 bytes); ...`.
INIT:
- L-TYPEMAP (:1629) `TYPE MAP <Exact|Proxy>: <name> -> <container template> (<type>) [<map note> CONTAINER (RL-20260927-03): ...]` (ContainerCatalogue.cs:223-243); a NEAREST line ends `PROXY: <template> - an Aggregate Container of the NEAREST branch ...`. L-PXYTAG (:1624-1626, WARN). L-NOCONT (:1605) must not print. C1c's init rename (:1642-1662): `NAME DISAMBIGUATED (C1c): unit ...` (:1656) and `NAME COLLISION (C1c): unit ...` (:1647) must not print.
- L-CENSUS-AGG (:1828) `CreationPolicy=AtOrder (C13) on the AGGREGATE model set: <C> container(s) created EMPTY ...`; its entity twin (:1838) must not print.
- L-COMPOSE (:2479) `ComposeHierarchy: <parent> -> EMPTY shell; will attach <N> declared child unit(s) [...]` and (:2534) `ComposeHierarchy: <parent> composed - <n>/<N> declared children attached.`
- L-BARRIER (:3620) `INIT CREATION BARRIER: <N> object(s) planned by this initialization (<S> empty shell(s)), ...`; L-READY (DispatchReadiness.cs:322-337 / :339-347) `READY TO TASK - <b> of <N> init unit(s) bound ...`; L-PLACE (:4075) `PLACEMENT summary: <T> of <N> create altitude(s) came from the TERRAIN QUERY, <F> from the FALLBACK ...`; L-INIT-PXY (:2260) `Init (<source>): <N> PROXY substitution(s) surfaced to C2SIM (R-SURFACE-PROXY).`; L-INIT (:2263) `Init dispatched: <U> units + <A> areas + <L> lines + <P> points queued for creation.`
- L-ID-CREATED (:6722; IdentityUuid.cs:144-145) `IDENTITY: '<requested name>' created as uuid <uuid> (requested) marking '<returned marking>'` at INFO - one per object bound by the uuid it was created under; <uuid> bare, lower-case.
- L-ID-NOTREQ (:6728-6730; IdentityUuid.cs:149-150) `IDENTITY: VR-Forces returned uuid <uuid> for marking '<returned marking>', not one we requested - bound by name (NameRegistry)` at WARN.
- L-TRUNC (:6731-6736, WARN, ONLY when NOT bound by uuid) `VRF returned created object '<returned>' for the name we requested, '<requested>' (<VRF_UUID>) - a name that overflows its DIS marking field ...`; L-AMBIG (:6737-6740) `... which is the truncation of MORE THAN ONE name we requested ...`; L-REBIND (:6749-6754) `NAME REBIND REFUSED: ...`.
- L-BIND (:6783-6784) `VRF console level 4 requested for <name> (<VRF_UUID>).` - one per created object at its ObjectCreated, after its L-ID line: the scorer's name -> uuid binding.
- L-ID-INIT (:3752-3761; IdentityUuid.cs:153-155) after READY TO TASK: `IDENTITY: <k> of <n> objects bound by uuid, <j> by name[, <u> not bound] (the initialization's own objects, at READY TO TASK[ - NOT REACHED]; RL-20260928-02)` - INFO when k = n, else WARN.
POPULATION (order receipt; C1):
- L-POP (:3187-3197) `POPULATE <container> IN PLACE (task '<first task>' performer; RL-20260927-03): source 3 - the authored table, row <row> (map row <map>): <N> member(s) FLAT (...) on ONE ring round its point (<lat>,<lon>): spacing <s> m (...), radius <r> m (centroid-preserving), reach <R> m. ... NOTHING IS DELETED. Members: [<name> (<template>), ...].` Twins: REFUSED (:3175), deferred (:3232), HELD (DispatchReadiness.cs:366-372, logged at :2777).
- L-ID-POP (:3198-3204) `IDENTITY: POPULATE <container> (<container uuid>): <N> member uuid(s) DERIVED - RFC 4122 v5 in namespace 485b28e7-1cc7-534b-b6a6-9beddf07a1b1 of '<container uuid>/<suffix>', the same every run (C1d, RL-20260928-02): [<member> = <uuid> (<container uuid>/<suffix>); ...].` - once per population, right after its L-POP. `NAME DISAMBIGUATED (C1c): member ...` (:3207-3209) must not print.
- L-SLOT (:3352-3354) `POPULATE <container> slot <k> of <N>: <member> (<template>, <path>) at (<lat>,<lon>), bearing <b> deg from the container point - <clear | SLOT MOVED <d> m <dir> - ... | KEPT ON BAD GROUND ... | UNVERIFIED ...>.`
- L-ISSUED (:3356-3358) `POPULATE <container>: <N> member create(s) issued AGGREGATED (UG52 Table 68 p1470), ...`; its late twin (:3339-3340).
- L-MEMBER (:6803-6809) `POPULATE <container>: member '<name>' (<n> chars) came back as EXACTLY that name (<VRF_UUID>) - at most 30 characters, it fits the aggregate marking (C1c); bound by its uuid (C1d).` - once per member at its ObjectCreated; the tail reads `; bound by the name registry, NOT by its uuid (C1d).` when the uuid was not honoured; its WARN twin (:6810-6813) `... came back as '<returned>' (<m> chars, <VRF_UUID>) - NOT the name we asked for ...`.
- L-ID-POPSUM (:6817-6825) `IDENTITY: <k> of <n> objects bound by uuid, <j> by name (the members of <container>; RL-20260928-02)` - once per population, when every member has its ObjectCreated.
- L-ATTACHED (ContainerPopulation.cs:452-455) `POPULATE <container>: <N> of <N> member(s) created; AddToOrganization issued for all of them in planned order (<HQ member> first) - waiting for the container to PUBLISH them (gate ends <t> s from now).`; partial twin (:446-450); late member (:321-323).
- L-PUB (ContainerPopulation.cs:359-363) `POPULATE <container>: the container PUBLISHES <n> subordinate(s) (expected <N>) <a> s after the population began, <b> s after the attach - READY FOR TASKING (RL-20260927-03).` - "(expected N)" is the count that ATTACHED (ContainerPopulation.cs:442), so it is read with L-ATTACHED; its TIMED OUT twin (:464-466, reasons :344, :349, :366-368).
- L-REANNOUNCE (:2961) `R-SURFACE-PROXY: <container> is now represented by '<template> composed from <N> sub-unit(s)' (...) - re-announcing the substitution to C2SIM.`
- L-D5 (:4715) `Task '<T>': its affected entity <name> is NOT populated - D-5 ...` - see P3e.
TASKING:
- L-DISP (:6016) `DISPATCHED <container> task '<T>' (<kind>) at WALL <iso>Z, SIMULATION clock <s> s. ...` - kind `hold-in-place` for T01/T13, `PA_Move_Along_Route` for T02/T10/T14 (MarkDispatched with the container script, :5875).
- L-CONSOLE-MEMBERS (:5068) `VRF console level 4 requested for <N> DISTINCT member(s) of <container> (<N> published, ...): <name> [<uuid>], ...` - on every ExecuteTaskOnTick pass of a container task (the members come from the population's record, :5039-5044).
- L-CNFPSL (:5299-5301; TaskDispatchPolicy.cs:144-146) and L-INPLACE (:5302-5307) `Task '<T>': verb CNFPSL -> intent=... Executing IN PLACE at <container>'s own position (<lat F5>,<lon F5>); NO VR-Forces task is issued ...`.
- L-FAW (:6664-6668; TaskDispatchPolicy.cs:172-174) `Task '<T2>' (verb ATTACK, 28ID__FRIENDLY_INFANTRY_DIVISION): ATTACK: advancing to the objective; rules of engagement set to fire at will - members engage enemies they encounter (RL-20260926-01). The order's own rules of engagement ('ROEHold') are overridden for this task.`; L-R3SELF (:5949) per pass; L-FOLSPT (:5685) `Task '<T14>' (verb FOLSPT, <48 IBCT>): advancing along the task's graphic to its end and holding there - ... ('ROEHold'). ...`; L-BARE (:5097) `Task '<T10>' verb=CRESRV -> intent=HoldObjective (...); Layer-2 not yet wired - executing bare movement.`
- L-SHIFTQ (:7731-7733); L-VCHECK (:7660-7663) `Task '<T>' (<container>): VERTEX CHECK (AggregateTacticalLevel) - <n> authored vertex(es) checked against OSM water and buildings: <m> moved, <k> kept on bad ground, <u> unverified, the rest clear.`; L-ELEV (:7171); L-OSMWATER (:7255-7264, WARN) `ROUTE PRE-FLIGHT task '<T>' (<container>) leg <i>: OSM WATER ON THE LINE - OSM <id> (<kind>), nearest <d> m, first at (<lat>,<lon>) <km> km along, ...`; L-SLOW (:7276-7278) `... leg <i>: EXPECTED SLOW - <s> m of <len> m in OSM forest/swamp/municipal land use (speed-factor 0.25 on the aggregate model set); reported only.`; L-RIVER (:7792-7794, WARN; the note from Preflight\RouteShift.cs:610) `Task '<T>' (<container>) leg <i>: NO ROUTE SHIFT - RIVER CROSSING - the same OSM water lies on the line at BOTH ends of the +/-600 m lateral band ... The task is dispatched on the line as authored and the C2 side is told it needs STP authoring (RL-20260927-01).`; L-NOFLAG (:7830-7831) `Task '<T>' (<container>): ROUTE SHIFT - no leg flagged; the route is unchanged.`
- L-ROUTE-C (:5896) `Task '<T>': CreateRoute '<T> ROUTE' (<n> pts) for CONTAINER <container> - <N> published member(s); RunScriptedTask PA_Move_Along_Route deferred to route-created (RL-20260927-03).`; L-SCRIPT (:6929) `Route '<T> ROUTE' (<VRF_UUID>) created; RunScriptedTask PA_Move_Along_Route issued for CONTAINER <container> (<VRF_UUID>) with [route=<VRF_UUID>, reverseDirection=false, startAtClosestVertex=false] - <N> published member(s) (RL-20260927-03).`; its failure twin `... was NOT issued for CONTAINER ...`; the memberless refusal (ContainerMayMove :3389-3401, the reason :3394-3397) `... REFUSED: container <name> has no published members to move ...`; the point move (:3415) `RunScriptedTask PA_Move_To_Location_Direct ...`.
- The vendor script's own console lines, relayed as (:9631) `VRF console [<level>] <container> (<uuid>): ...Init of aggregate move along.` and `... Sub. <member> starting move-along.` / `has completed move task.` / `has completed final move.` (PA_Move_Along_Route.lua :28, :80, :110, :140).
JUDGING AND COMPLETION:
- L-TIMED (:8408 WARN / :8419) `TIMED COMPLETION: task '<T>' on <container> reached its END TIME - <S> s of a <D> s Duration served ... [- but the unit has NOT ARRIVED: OVERDUE.]`.
- L-ARRIVE (:8020-8039) `ARRIVAL EVIDENCE: <container> task '<T>' - 1/1 member(s) within 500 m of the last vertex (nearest <d> m) AND past their OWN traversal bar ... ONE POSITION: an aggregate-level unit with no members, judged on its own reflected centre point (D1, RL-20260927-01).` (the suffix :8036-8038); L-CANNOT (:7948) must not print.
- L-STALL (:8835-8842) `STALL: unit <container> task <T>: no member moved more than 50 m in the last 360 SIM s (max <m> m); TASKABRT reported. ONE POSITION: ...` and its TASKABRT text (:8852-8855) `STALLED (C16 progress watchdog) - report only: the task stays in flight, ...`.
- L-VRFDONE (:9306) `VRF task complete: <container> / <type> (success=<b>)` - <container> is the name RESOLVED from the completion's marking (:9280); a MEMBER's completion is at Debug (:9299-9305) and never reaches a TaskStatus; L-D6 (:3436-3441, WARN) `CONTAINER <container> task '<T>': VR-Forces reported '<type>' COMPLETE, but completed short: <d> m from the route end (...) - TASKCMPLT is WITHHELD (D-6, RL-20260927-04). ...` (ContainerPopulation.cs:526-527) and its hand-on twin (:3445-3450) `... COMPLETE with the container <d> m from the route end - handed on to the task's completion rules (D-6, RL-20260927-04).`; L-SWALLOW (:9328) `VRF completion for <container> after the arrival-evidence report of task <T> - swallowed.`; L-OVERDUE-ARR (:9446); L-HELD (:9441); L-ID-MARK (:9267; IdentityUuid.cs:159-162) `IDENTITY: the task-completion marking '<m>' is carried by <n> objects bound by uuid [...] ...` (WARN, must not print); `Task-complete for '<name>' but no C2SIM uuid known - no report sent.` (:9408) must not print.
- L-SENT (:9886) `SENT TASK STATUS REPORT (<code>) taskee=<C2SIM uuid> task=<task uuid> - <why>.` - the taskee is always a PERFORMER's C2SIM uuid (members have none).
SHUTDOWN (after the host's "Application is shutting down..."): `Cleanup: deleting <N> created VR-Forces objects before resign...` (:1116-1117) and `Cleanup: <N> deletes dispatched (<ms> ms).` (:1124) - the resign-time teardown, excluded by name from P2(f).
THE RUNNER (Stage 0, RunC2SimScenario.ps1; the D2 lines of sec 1(b)): `model set   : ...` (:2365), `tasked      : ...` and one line per tasked unit (:2367-2372), `type map    : ...` (:2375), `fixture SMS : ...` and the pairing note (:2376-2380), `composition : variant "catalogue" <- ...` (:2636-2642), `catalogue   : ...` (:2643-2644); manifest inputs.modelSet (:2997-3049).

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run unless marked PREP)

All from Git Bash at the MAIN checkout F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM (HEAD df9aca1 at
registration, tracked tree clean, `git diff --stat 699552c main -- src` EMPTY [V]). The order, init, type map and
composition are the main checkout's own data/ files (hashes = this branch's [V]). The runner writes its appNumber blocks
into the MAIN checkout's working-tree docs/OPUS_EXECUTION_PLAN.md; they are carried back to this branch afterwards (the
G1 procedure). Scripts (scratch u3\laneG1-2): golive_checks_g1_2.ps1, g1_2_runner.sh (one command line for every runner
call: prewarm-dryrun | prewarm | scored-dryrun | scored), c3_push_g1_2.sh, claim_holder_g1_2.ps1 (branch (b) only),
g1_2_score.py, names_check.py; lane G1's simcache_listing.ps1 and harness; lane E2's cache_manifest.ps1. THE SEAT RUNS C3,
D, W, E AND F under RL-20260928-01, as in G1 (the permission classifier refused G1's lane its push and its Appendix B
write).

THE HOLDER BRANCH is fixed ONCE, at step A, before W:
- BRANCH (a) - REUSE: RtiProbe pid 33476 (appNo 5207, started 10:14:49.722Z, hold 28800 s -> resigns 18:14:49Z [V
  13:41:53Z]) is alive AND the pre-warm W is launched before ~17:40:00Z. Nothing is claimed for the holder; its tree keeps
  5198ac45 (DEVIATION above); each pre-launch check uses -Branch a -MinHoldLeftMin 20, so E must launch before ~17:54:49Z.
  If E's check then fails that limb, E is NOT launched on 33476: the seat waits for its resignation and continues with D0-D
  of branch (b) from the marker read then; W stands.
- BRANCH (b) - NEW HOLDER: otherwise (33476 gone, or W not launched before ~17:40:00Z). Steps D0 and D below.

A0. PRECONDITIONS: (1) the seat's go-live; (2) no other lane is building, running a suite or an agent for the quiet
    period; (3) `git diff --stat 699552c main -- src` is still EMPTY - if the MANAGED source has moved on, NOTHING is
    rebuilt: the registered build is what runs and the difference is recorded; if the DEPLOYED hashes differ (another lane
    rebuilt), STOP before W and the seat decides; (4) the deployed fixture, the order, init, type map and composition hashes
    and the cache manifest as registered.
A.  "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File <u3>/laneG1-2/golive_checks_g1_2.ps1 -Phase prelaunch
    -HolderPid 33476 -MarkerWant 5233 -Branch a -MinHoldLeftMin 20   (branch (a)); in branch (b) before D:
    ... -Phase preholder -MarkerWant 5233 -Branch b. EXPECT 0 checks FAILED. [PREP V 2026-09-28T13:56:40Z, branch (a):
    0 FAILED - exe 7824483a, dll bb7e6361, appsettings 0f600f0e, bridge 03226dd0 in the app, WatchVrf and CreateOne trees,
    5198ac45 in the RtiProbe tree, PV 1.0.0+git.699552c.Release-5.2, fixture 804e2c39, order 7a986137, init 2000e856, type
    map c546edbe, composition 9684e945, cache 682bdea4 / 479 files, REST 200, rtiexec 47980 / rtiForwarder 50740 /
    rtiAssistant 30240 up, holder 33476 up with 258 min left, no sim/app/observer, marker 5233.]
A2. THE BRIDGE, OFFLINE - "the REFUSING line = STOP before W": the deployed exe's `--populate-selftest` reads 0 FAIL and its
    (p15) "the LINKED VrfBridge.dll CARRIES CreateEntity(..., String uuid) and CreateAggregate(..., Boolean, String uuid)"
    PASS (ContainerSelfTest.cs:1610-1614) - from the NEW PIN record [A: the deploy lane, 189 PASS / 0 FAIL / 4 SKIP], or
    re-run by the seat with the 5.2 PATH prefix, DOTNET_ENVIRONMENT empty and no Vrf__ variable:
        src/VrfC2SimApp/bin/Release-5.2/net10.0/win-x64/VrfC2SimApp.exe --populate-selftest
    A FAIL or a missing line = STOP before W. (W is the app's first live start; the IDENTITY refusal there is W gate I1.)
B.  No build (branch (b) D0 is a rebuild of the RtiProbe tree only, outside the quiet period).
C.  Order validation: C1 / C2 [A: G1's, sec 1(e)]; C3 ONE real push to the PRIVATE server (RL-20260928-01), at go-live,
    after this registration is committed - the seat runs `sh <u3>/laneG1-2/c3_push_g1_2.sh`, i.e. G1's two lines verbatim:
        tools/PushInit/bin/Release/net10.0/PushInit.exe data/IRONSTORM_CUTA_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
        tools/PushOrder/bin/Release/net10.0/PushOrder.exe data/IRONSTORM_CUTA_Order.xml 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
    EXPECT both exit 0, "QUERYINIT : 40 Units" and one `ORDER (69670 chars)` echo carrying 5 tasks, 5 Durations and
    CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT (G1's). Any failure = STOP.
D0. BRANCH (b) ONLY - after 33476 has resigned (18:14:49Z; confirm with tasklist: no RtiProbe.exe), REBUILD THE RTIPROBE
    TREE IN PLACE at the NEW PIN (RUNBOOK sec 9 step 3, the pin's own command):
        dotnet build tools/RtiProbe/RtiProbe.csproj -c Release -p:BridgeConfig=Release-5.2 -t:Rebuild -m:1 -nodeReuse:false
    EXPECT 0 errors and tools/RtiProbe/bin/Release-5.2/net10.0/win-x64/VrfBridge.dll = 03226dd0...14c5 (golive
    -Branch b checks it). A failure = STOP; no holder is claimed on a stale tree.
D.  BRANCH (b) ONLY - THE NEW HOLDER. First HAND-CLAIM M..M+3 in Appendix B of the main checkout's working-tree
    OPUS_EXECUTION_PLAN.md (M = the marker, 5233 unless another run has moved it) BEFORE the holder joins:
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File <u3>/laneG1-2/claim_holder_g1_2.ps1 -Path docs/OPUS_EXECUTION_PLAN.md -From 5233
    (marker 5233 -> 5237; the same claim is committed on this branch with the Result); then G1's holder lines with the new
    numbers:
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5233,5234,5235,5236 -SettleSecs 28800 -WhatIf
        "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5233,5234,5235,5236 -SettleSecs 28800 < /dev/null > runs/launch52/g1-2-holder-<stamp>.log 2>&1
    EXPECT `HOLDER JOINED: pid ... appNo 5233`, exit 0. Exit 1 (none joined) or 2 = STOP, no blind relaunch (RUNBOOK 9c).
    Branch (a) runs NEITHER D0 nor D.
W.  THE PRE-WARM (unscored) = THE W GATE: golive_checks_g1_2.ps1 -Phase prelaunch -HolderPid <33476 | the new pid>
    -MarkerWant <5233 | 5237> -Branch <a | b> -MinHoldLeftMin <20 | 60> (0 FAILED); simcache_listing.ps1 -Out
    <u3>/laneG1-2/simcache_before_prewarm.txt; `sh <u3>/laneG1-2/g1_2_runner.sh prewarm-dryrun` (EXPECT the holder
    recognised PERSISTENT, block 5233-5243 (a) / 5237-5247 (b), "marker would advance to: 5244" (a) / "5248" (b), the three
    D2 lines of sec 1(b) verbatim, window 120 s CAP); then `sh <u3>/laneG1-2/g1_2_runner.sh prewarm` once. GATE for E - ANY
    miss = STOP before E: StopVrf52 exit 0 or 6; the post-W inventory shows only rtiexec, rtiForwarder, rtiAssistant, the
    persistent holder and the pre-warm's Stage 2h holder; the cache manifest still 682bdea4; AND on the pre-warm's app log
    `python <u3>/laneG1-2/g1_2_score.py <prewarm run dir> --wgate` prints "W GATE: PASS" (exit 0) - IDENTITY + POPULATION
    PUBLISHED (P3, P3a): 0 REFUSING TO START; the L-IDENTITY-ON line once; 59 L-ID-CREATED lines (the 36 init containers
    under their C2SIM uuids, 35 of them with the CUT marking and 4ID__FRIENDLY_INFANTRY_DIVISION whole; the 23 members under
    their derived uuids with their own names as markings); 0 L-ID-NOTREQ; L-ID-INIT "36 of 36 objects bound by uuid, 0 by
    name" at INFO after READY TO TASK; per performer one L-POP, one L-ID-POP naming exactly the derived uuids, one
    L-ID-POPSUM "1 of 1" / "5 of 5" / "17 of 17 objects bound by uuid, 0 by name", the full L-ATTACHED "POPULATE <c>: <N> of
    <N> member(s) created; AddToOrganization issued for all of them in planned order (<HQ> first) ..." with N = 1 / 5 / 17
    and the HQs 28ID__FRIENDLY_INFANTRY_DI.HQ1, 1-112_IN/28ID__FRIENDLY_IN.HQ1, 48_IBCT/28ID__FRIENDLY_INF.HQ1, and one
    L-PUB "(expected N)" = N with PUBLISHES >= N; 23 L-MEMBER lines "...; bound by its uuid (C1d)." with the member's derived
    VRF_UUID; 0 L-TRUNC (the init's 35 cut markings are INSIDE the L-ID-CREATED lines: the G1-style WARN prints only for
    an object NOT bound by its uuid, :6731, so "35 init truncation lines" is the S7 prediction, not this gate's), 0 NAME
    DISAMBIGUATED (C1c), 0 NAME COLLISION (C1c), 0 "truncation of MORE THAN ONE name", 0 NAME REBIND
    REFUSED, 0 POPULATE TIMED OUT, 0 "only <n> of <N>"; L-BIND for all 59 under VRF_UUID:<the requested uuid>; and the
    pre-warm's WatchVrf trace carries POS rows under the requested uuids of the three performers and the 23 members. A W
    showing S7 in full (sec 4) is ALSO a STOP before E: the live-owed claim is then answered, and whether E runs under S7 is
    the seat's call. Then simcache_listing.ps1 -Out <u3>/laneG1-2/simcache_after_prewarm.txt.
E.  THE RUN: golive_checks_g1_2.ps1 -Phase prelaunch (as W) -MarkerWant <5244 | 5248> (0 FAILED); `sh
    <u3>/laneG1-2/g1_2_runner.sh scored-dryrun` (EXPECT block 5244-5254 (a) / 5248-5258 (b), marker -> 5255 / 5259, the D2
    lines, window 2700 s CAP with -StopWhenComplete); then `sh <u3>/laneG1-2/g1_2_runner.sh scored` ONCE, stdout to a file,
    never piped. The command g1_2_runner.sh runs, verbatim - G1's but for `--model-set auto` and the log name:

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
          --log runs/launch52/RunScenario-ironstorm-agg-g1-2-<stamp>.log

    (the pre-warm: the same line with --run-secs 120 and its own log name; the dry runs add --dry-run). GATE on teardown:
    StopVrf52 exit 0 or 6 (P21); 3, 5 or 7 = STOP.
F.  Post-run, in the FOREGROUND: the inventory (only the RTI trio, the persistent holder and, inside its 900 s hold, the
    Stage 2h holder); hashes and the cache manifest re-read (golive_checks_g1_2.ps1 as E, -MarkerWant <5255 | 5259>);
    simcache_listing.ps1 -Out <u3>/laneG1-2/simcache_after_scored.txt; then the harvest (sec 6).

QUIET PERIOD (RUNBOOK 0.5.14 item 5) - G1's: from the launch of W to the post-run inventory of E: no Stop-Process / taskkill
of any kind (StopVrf52's own identity-gated force of the run's own back end is allowed), no build, no suite, no subagent,
no second runner - in this lane or any other. Never touched: rtiexec 47980, rtiForwarder 50740, rtiAssistant 30240, any
RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES - G1's: per TASK UUID from the scored run's vrfc2simapp.log in line order, cross-checked against
reports-captured.log; positions from watchvrf-trace.csv POS rows. THE SCORER scratch u3\laneG1-2\g1_2_score.py <runDir>
(derived from G1's g1_score.py through the stopped first attempt's; imports lane E2's e2_score.py - the trace parser, the
SIM clock, the OSM reader on lane G1's copy of the deployed cache, manifest 682bdea4): the line-family census of sec 2, the
per-task SENT codes, the population per container, the IDENTITY limbs I1-I18 and the S7 pattern, P2(f), P15, the
completions by marking (O7), and per mover the CONTAINER's own POS track and each MEMBER's track (member names mapped to
G1's harness targets by suffix). Its --selftest: 33 checks PASS, 0 FAIL - the uuid5 oracle against NameSelfTest's pinned values,
a CLEAN control (every identity and population limb, the W gate, P2(f), P15, O7, the trace measures), S7 as its own full
pattern, and DIRTY controls (mixed binding, the IDENTITY refusal, a wrong derivation, a member TaskStatus, a DELETE, a point
move, a missing publication, a timed-out population, an early cleanup, a D-6 that closes) each caught [V,
u3\laneG1-2\g1_2_score_selftest.txt]. Names bind to uuids through L-BIND and L-CONSOLE-MEMBERS; members belong to the
container whose L-POP lists them. "Before X" is log-line order. THE CLOCKS: the ~21 s app/trace clock offset is the seat's
known one - every comparison is anchored on the APP clock (the L-DISP WALL stamps) and trace instants are placed with the
WatchVrf join stamp, as G1 did.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) The persistent holder of the branch fixed at step A alive at launch and recognised PERSISTENT by the runner; the back end JOINS (rtiexec count-grep: no create for its appNo); "READY - joined the federation". (ii) L-BUILD names git 699552c; the exe / dll / bridge / appsettings / fixture / order / init / type map / composition hashes and the cache manifest as registered, before and after. (iii) D2: the runner's Stage 0 prints, verbatim, "model set   : AggregateTacticalLevel <- auto (RL-20260927-06): highest TASKED echelon DIV (28ID__FRIENDLY_INFANTRY_DIVISION) is ABOVE BN - aggregate-only; exported to the app as Vrf__ModelSet", the pairing line "fixture C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Aggregate.scnx loads AggregateTacticalLevel (aggregatetacticallevel.sms) - matches the chosen model set." and "catalogue   : not exported - the app keeps the default "" (the model set's own SMS)", with the tasked block DIV / BN / BDE of sec 1(b); its manifest records inputs.modelSet.modelSet AggregateTacticalLevel and inputs.modelSet.selection.highestEchelon DIV. (iv) 0 REFUSING TO START of any kind - the IDENTITY refusal included - and no "AGGREGATE CONTAINERS off" line; L-IDENTITY-ON once. (v) THE FLOOR ARRIVED: L-CLOCK reads `Vrf:TaskPredecessorTimeoutSeconds=600 s` and both gate lines (T02, T14) read `and then 600 s to COMPLETE`. (vi) The W gate (sec 3 W) held. | HIGH | Any limb = VOID + STOP (launch, harness or settings failure - no code verdict). A D2 refusal, another model-set choice or another fixture in the DRY RUN = STOP before launch; the IDENTITY REFUSING line = STOP before W (step A2). | |
| P1 | START-UP LINES, each exactly once, at INFO, with the numbers of sec 2 - G1's: L-FIDELITY 62 rows; L-CAT (705 templates, 3 model-set dirs, root AggregateTacticalLevel, home C:\MAK\vrforces5.2d); L-VARIANT catalogue with "5 of the 8 row(s)", "7 authoredRows type(s) ... NOT in use" and - CHANGED FROM G1 by D2 - "SMS: the fixture C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Aggregate.scnx loads $(DATA_DIR)\simulationModelSets\AggregateTacticalLevel.sms."; FIVE L-ROW lines 1/0, 17/3, 5/0, 26/6, 8/1 with ZERO REFUSED rows; L-CON-ON with "hostile nation RUS", "(5 row(s) resolve, 0 refused)", "85 s" and "100 m"; L-IDENTITY-ON naming namespace 485b28e7-1cc7-534b-b6a6-9beddf07a1b1; L-MODELSET naming AggregateTacticalLevel; L-M1-ON; L-WDOG-ON (360 s, SIMULATION, 50 m); L-CLOCK (SIMULATION, 0.25, 600, 60, 86400); L-SHIFT-ON; and L-OSMSET once, naming AggregateTacticalLevel and "osm-water (225 tile file(s), 0 of them 0 bytes = UNKNOWN) and \osm (225, 0 0 bytes)", printed after the first L-POP and before the first L-DISP. ZERO "is not EntityLevel or AggregateTacticalLevel" warnings. | HIGH | A missing, duplicated or differently-numbered line = STOP (the build or the settings are not what was registered). | |
| P2 | INIT - 36 EMPTY CONTAINERS - G1's. (a) 36 L-TYPEMAP lines carrying "CONTAINER (RL-20260927-03)": 29 `TYPE MAP Exact` and 7 `TYPE MAP Proxy` whose note says "NEAREST branch"; 48 IBCT -> `BDE (11.1.225.8.3.1.1)`, 1-112 IN -> `BN, Light Infantry (11.1.225.6.3.1.0)`, 28ID -> `DIV, Mech Infantry (11.1.225.9.4.1.0)` (Proxy); every hostile container nation 260. (b) ZERO L-NOCONT; ZERO init-time NAME DISAMBIGUATED (C1c) / NAME COLLISION (C1c). (c) 7 L-PXYTAG warnings, one per NEAREST container. (d) L-CENSUS-AGG reads 36 / 0 / 0 and the entity census twin is absent; L-BARRIER reads 36 objects (36 empty shells); L-INIT reads "36 units + 5 areas"; L-INIT-PXY reads 7. (e) 36 container names bound by L-BIND before the order, each under VRF_UUID:<its own C2SIM uuid> (P3 limb I17), and ZERO member names bound before it. (f) ZERO DELETE-family lines from init to the last scored line; the shutdown "Cleanup: deleting N created VR-Forces objects before resign" line after "Application is shutting down" is excluded by name (and its "Cleanup: N deletes dispatched" tail). (g) ZERO L-TRUNC at init: every init container is bound by its uuid (P3), so its cut marking shows in its L-ID-CREATED line, not in the G1-style WARN (VrfC2SimService.cs:6731). READY TO TASK, the init L-PLACE counts and "ORDER BEFORE READY TO TASK" RECORDED. | HIGH | Any limb = STOP, except limb (g) under the named branch S7, where 35 L-TRUNC are the S7 prediction. | |
| P2b | THE TO TWINS - G1's: exactly two L-COMPOSE parents - 28ID/III_Corps__TWO_EIGHT_TH_US_INFANTRY_DIVISION with 9 declared children and 278_Armored_Cavalry_Regiment/28ID__TWO_SEVEN_EIGHT_ARMORED_CAVA... with 1 - each "composed - n/N declared children attached" with n = N. | MEDIUM | Another parent set, a count, or n < N = recorded MEDIUM miss. | |
| P3 | IDENTITY BY UUID - THE LIVE-OWED CLAIM (C1d, RL-20260928-02): VR-Forces returns the REQUESTED uuid for ENTITIES and AGGREGATES as it does for control areas. (I1) L-IDENTITY-ON once, 0 IDENTITY REFUSING. (I2) EXACTLY 59 L-ID-CREATED lines at INFO: the 36 init containers, each under its OWN C2SIM uuid (the init's UUID element; e.g. 48 IBCT dd3d21b2-c5e0-d45a-9fba-b4b8bb879e6a, 28ID 200d3a3f-8f36-4951-9459-c748527896ba, 1-112 IN 8d5b2ba6-73c1-6c55-812c-7c8078ea8c97), and the 23 members, each under the derived uuid of sec 1(i). (I3) Their markings: 35 of the 36 init lines carry the name's first 30 characters, 4ID__FRIENDLY_INFANTRY_DIVISION (31) its whole name, every member line its own name (30 at most). (I4) ZERO L-ID-NOTREQ ("... not one we requested - bound by name (NameRegistry)"). (I5) L-ID-INIT "IDENTITY: 36 of 36 objects bound by uuid, 0 by name (the initialization's own objects, at READY TO TASK; RL-20260928-02)" once, at INFO, after READY TO TASK. (I6) Per population, right after its L-POP, one L-ID-POP "IDENTITY: POPULATE <c> (<its C2SIM uuid>): N member uuid(s) DERIVED ..." whose list is exactly the sec 1(i) names, uuids and '<container uuid>/<suffix>' in slot order (N = 1 / 5 / 17). (I7) One L-ID-POPSUM each: "1 of 1", "5 of 5", "17 of 17 objects bound by uuid, 0 by name (the members of <c>; ...)". (I8) 23 L-MEMBER lines "came back as EXACTLY that name (VRF_UUID:<its derived uuid>) - ... (C1c); bound by its uuid (C1d).", one per member. (I9-I16) ZERO: L-MEMBER "bound by the name registry, NOT by its uuid", its WARN twin, L-TRUNC, NAME DISAMBIGUATED (C1c), NAME COLLISION (C1c), "truncation of MORE THAN ONE name", NAME REBIND REFUSED, and every other IDENTITY WARN/ERROR family (ambiguous marking, displaced, uuid conflict, not 8-4-4-4-12, WITHOUT a uuid, EXPAND, re-create). (I17) L-BIND for all 59 under VRF_UUID:<the requested uuid>. (I18) The WatchVrf trace - an independent federate - carries POS rows under the requested uuids of the three performers and the 23 members. | HIGH (the run's first HIGH prediction) | The falsifier: any L-ID-NOTREQ, or L-ID-INIT reading "0 of 36 ... by uuid". If it fires in full the run takes the named branch S7 ("uuid not honoured") and CONTINUES to score the rest - C1c's names still bind by exact match; P3 is then a MISS (HIGH), recorded, the verdict STOPPED at P3. A MIXED outcome (some objects by uuid, some not), an object bound to a uuid it was not created under, or any I-limb failing otherwise = STOP. The strongest competing hypothesis, named now: VR-Forces treats the BARE uuid string on an entity/aggregate create the way uuid.h:77-84 documents for a non-VRF_UUID: string (a marking-text lookup, an invalid uuid) and generates its own - which the S7 pattern would show. | |
| P3a | POPULATION, per performer, at ORDER RECEIPT (sec 1(d)) - G1's P3 with the C1c names: exactly one L-POP each, its "why" naming one of that performer's tasks - 28ID source 3 row C-USA-DIV-UCI (map row F-UCI-I), 1 member, radius 0 m; 1-112 IN row C-USA-BN-UCI (F-UCI-F), 5 members, radius 153 m; 48 IBCT row C-USA-BDE-UCI (F-UCI-H), 17 members, radius 490 m, "3 sub-container(s) ... flattened"; member lists = the names of sec 1(i) in slot order; and per container, after its L-POP, exactly N L-SLOT lines, one L-ISSUED (N AGGREGATED), one L-ATTACHED "N of N member(s) created; AddToOrganization issued for all of them in planned order (<the HQ1 member> first)" - 28ID__FRIENDLY_INFANTRY_DI.HQ1, 1-112_IN/28ID__FRIENDLY_IN.HQ1, 48_IBCT/28ID__FRIENDLY_INF.HQ1 - one L-PUB "PUBLISHES n subordinate(s) (expected N) ... READY FOR TASKING" with n >= N (read with L-ATTACHED: "(expected N)" counts what attached) and one L-REANNOUNCE "... composed from N sub-unit(s)". All 23 member names bound by L-BIND. ZERO REFUSED, TIMED OUT, partial ("only <n> of <N>"), late-member and slot-check-late lines. The order of the three L-POP lines, which task each "why" names, a HELD / deferred line and the publication times RECORDED. | HIGH | A missing or duplicated line, another source / row / count / radius / name, a member unbound, or any refusal / timeout = STOP, except the named branches S2 (TIMED OUT) and S3 / S4 below, which are recorded and the run continues. | |
| P3b | SLOT VERDICTS as the harness - G1's (held live in G1): 28ID 1 clear; 1-112 IN 5 clear; 48 IBCT 13 clear + slots 4, 5, 6, 16 "SLOT MOVED 125 m north-west / 125 m south-west / 25 m south / 75 m south-east" (lake 197345448 x3, pond 16373225 x1); ZERO KEPT ON BAD GROUND and ZERO UNVERIFIED. | HIGH | Another verdict for any slot = STOP. | |
| P3c | PUBLICATION BEFORE DISPATCH: each container's L-PUB precedes the L-DISP of every task it performs (28ID before T01, 1-112 IN before T10, 48 IBCT before T13); its "(expected N)" equals N; ZERO memberless-move refusals other than S2's. | HIGH | Any dispatch before its container's L-PUB = STOP (the publication gate did not hold); a memberless-move refusal that is not the registered consequence of an S2 timeout = STOP. | |
| P3e | D-5 - G1's: ZERO "is NOT populated - D-5" lines, and 116_ABCT/28ID__FRIENDLY_AIRBORNE_TRACKED_ARMORED_BRIGADE gets NO L-POP - OrderParser keeps only the FIRST AffectedEntity (OrderParser.cs:76), which for every task is its performer, so the D-5 branch (VrfC2SimService.cs:4709-4722) is never entered. | HIGH | A D-5 line or a population of any unit other than the three performers = STOP. | |
| P4 | CNFPSL HOLDS T01 and T13 - G1's: each one L-DISP (hold-in-place) after its container's L-PUB, one L-CNFPSL + one L-INPLACE naming the container, one STP-866 observation, "it has no destination" armed at 300, NO route and NO scripted task, exactly one TASKCMPLT "(300 s after dispatch)". The L-INPLACE position of 48 IBCT RECORDED (the harness centroid 54.01934, 23.31381). | HIGH | A move or script for a hold; zero or two TASKCMPLTs; an OVERDUE for a hold = STOP. | |
| P5 | DISPATCH STRUCTURE - G1's: exactly one SENT TASKSTRT per task (5); T02's L-DISP after T01's TASKCMPLT and T14's after T13's; one "Task 'T10...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25)" line and T10's L-DISP after it; ZERO "SKIPPED: predecessor"; ZERO RL-20260927-05 gate-expiry lines (VrfC2SimService.cs:8470). The dispatch order of T10 against T02 / T14 RECORDED. | HIGH | A missing or duplicate TASKSTRT, a successor before its predecessor's TASKCMPLT, or a SKIP = STOP. | |
| P6 | M2 ON THE AGGREGATE MODEL SET, per mover exactly as the harness (sec 1(g)) - G1's: L-SHIFTQ "(2 vertices)" T02, "(4 vertices)" T10, "(3 vertices)" T14; L-VCHECK (AggregateTacticalLevel) 1 / 3 / 2 checked, 0 moved, 0 kept, 0 unverified; L-ELEV L12 x1 / x3 / x2; L-SLOW T02 leg 1 3370 of 5342 m, T10 legs 805/1451, 103/804, 369/516 m, T14 legs 1270/1270, 1663/2902 m; T02: one L-OSMWATER "OSM 8011072 (waterway=river (MAK_WIDTH 5 m))" first at 54.0087..., 23.2351... ~2.40 km along, and one L-RIVER; T10 and T14: one L-NOFLAG each; ZERO VERTEX MOVED / NOT MOVED / UNVERIFIED, ZERO "could NOT be read", ZERO ROUTE SHIFTED, ZERO "NO CLEARED LINE", ZERO PRE-DISPATCH applied, ZERO land-cover "WATER ON THE LINE"; exactly 8 pre-flight ObservationReports in the capture. | HIGH | Any limb = STOP. | |
| P7 | THE CONTAINER'S OWN SCRIPT - G1's: per mover one L-ROUTE-C - T02 "(2 pts) for CONTAINER 28ID__FRIENDLY_INFANTRY_DIVISION - 1 published member(s)", T10 "(4 pts) ... - 5", T14 "(3 pts) ... - 17", each "RunScriptedTask PA_Move_Along_Route deferred to route-created" - then one L-SCRIPT "RunScriptedTask PA_Move_Along_Route issued for CONTAINER <c> (VRF_UUID:<its C2SIM uuid>) with [route=<the route's uuid>, reverseDirection=false, startAtClosestVertex=false] - N published member(s)"; L-DISP kind "PA_Move_Along_Route". ZERO "was NOT issued", ZERO "RunScriptedTask PA_Move_To_Location_Direct" (sec 1(f)), ZERO "created; MoveAlongRoute issued", ZERO "MOVE TO PER VERTEX for", ZERO MoveIntoFormation / R11 / fan-out / formation lines for a container. L-CONSOLE-MEMBERS names 1 / 5 / 17 distinct members, each [VRF_UUID:<its derived uuid>], on each pass (pass count RECORDED). | HIGH | Any other form, point count, member count, uuid or variable value = STOP (a container uuid other than its C2SIM uuid is the S7 branch, not a new stop). | |
| P8 | VERB LINES - G1's: T02 exactly one L-FAW (a UNIT's ATTACK - RL-20260926-01; TaskDispatchPolicy.cs:134) and one L-R3SELF per dispatch pass; T14 exactly one L-FOLSPT naming 'ROEHold'; T10 one L-BARE per pass; ZERO L-FAW for any other task. | HIGH | A missing, duplicated or misplaced line = STOP. | |
| P9 | THE VENDOR SCRIPT SPEAKS (the container's console at level 4; the relay SEEN in G1): per mover one "Init of aggregate move along." on the container's console, then one "Sub. <m> starting move-along." per member (1 / 5 / 17) - the vendor now names each member by its WHOLE C1c name, where G1 saw a 30-character cut - and ZERO "starting pa_move_along_route" (members are AGGREGATED, PA_Move_Along_Route.lua :46/:65). The "has completed move task." / "has completed final move." lines per member RECORDED. | MEDIUM | Fewer or no such lines = recorded MEDIUM miss; a "starting pa_move_along_route" line = STOP (a member was created Disaggregated). | |
| P10 | MOVEMENT - G1's: each moving container's OWN published position (the centroid) is displaced MORE THAN 50 m after its L-DISP (T02, T10, T14); members displaced > 50 m after it: 28ID 1 of 1, 1-112 IN 5 of 5, 48 IBCT at least 12 of 17. | HIGH | A container at or under 50 m, or fewer members moving = STOP (named outcomes recorded). | |
| P10a | 48 IBCT: EXACTLY 16 of 17 members displaced > 50 m - all but 48_IBCT/28ID__FRIENDL.INF1RIF2 (P11); the near-water first legs pass (G1's sec 1(i)). | MEDIUM | 12-15 = recorded MEDIUM miss, naming the members that stopped and the water within 10 m ahead of each. | |
| P10b | THE CONTAINER IS ITS MEMBERS' CENTROID: at the last common trace instant the container's own fix lies within 25 m of its members' unweighted mean (design Q2; G1 0.0 m with one member), for each of the three. | MEDIUM | Beyond 25 m = recorded MEDIUM miss (the D1 judge would be reading something other than the members' mean). | |
| P11 | THE FAR-SHORE MEMBER: 48_IBCT/28ID__FRIENDL.INF1RIF2 never comes within 50 m of T14's vertex 0 (54.019341, 23.313809) - lake 197345448 lies between (sec 1(i)). Its outcome RECORDED as one of: (a) it stays within ~10 m of its slot (the blocked move runs forever); (b) its move ends or fails at the shore and its retrograde move takes it toward its unpack target (54.043804, 23.329044); (c) anything else. | HIGH | Within 50 m of vertex 0 = the aggregate model crossed or skirted OSM water that M2 reads as a stop: STOP and ask. | |
| P12 | COMPLETION PER THE TIME RULES (RL-20260921-09), each mover EXACTLY ONE path - G1's: T10 and T14 close on L-ARRIVE with 1/1 and the ONE POSITION suffix, then one TASKCMPLT; T02 does NOT arrive and closes on L-STALL (ONE POSITION) with one TASKABRT "STALLED (C16 progress watchdog) ..." (S5; RL-20260913-03, RL-20260914-01). WHOSE POSITION (sec 1(p)): every one of these judges reads the CONTAINER's own reflected position - VR-Forces' published fix for the container, the mean of its members - and no member's; "1/1" is that one position. ZERO L-CANNOT; ZERO VACUOUS; exactly one terminal report per task (5 in all). T14's arrival limb is scored HIGH when P10a holds; if P10a misses (more members held by water), T14 closing on L-STALL instead is RECORDED as S5 with the held members named. The lag of the container's fix behind its members while moving (G1: up to 150 m) is RECORDED as O4 and does not change the rule: the arrival fires on the container's fix. | HIGH | Two paths, a TASKCMPLT before start + Duration for an unarrived mover, an arrival without the ONE POSITION suffix or with a total other than 1, T02 arriving, or T02 stopping with NO stall report (the P-FALS silent stop) = STOP. | |
| P12b | WHICH PATH - G1's: T10 and T14 late (b): L-TIMED OVERDUE at their end time (450 / 300 s), then L-ARRIVE, L-OVERDUE-ARR and TASKCMPLT "arrived after its task's end time - complete on arrival"; T02 OVERDUE at 300 s, then the stall. | MEDIUM | Another path = recorded MEDIUM miss. | |
| P13 | T14's REST (sec 1(i)): the container's closest approach to its destination inside the window is between 100 and 250 m (predicted ~146 m: the far-shore member's 1/17 pull), under outcome P11(a). | MEDIUM | Within 100 m or beyond 250 m = recorded MEDIUM miss; the P11 outcome read with it. | |
| P13b | T10's REST: the container comes within 100 m of PassagePoint_48_IBCT_SLOT0. | MEDIUM | Not within 100 m = recorded MEDIUM miss. | |
| P14 | T02 AT THE RIVER - G1's: 28ID's container never comes within 500 m of its destination; its last fix lies within 150 m of the river point 54.0087722, 23.2351231 and short of it along the line (<= 2,450 m along); the stall's "max" RECORDED. | HIGH (never within 500 m); MEDIUM (the stop point) | Within 500 m of the destination = STOP (the aggregate model crossed a MAK_WATERWAY river). A stop elsewhere = recorded MEDIUM miss. | |
| P15 | D-6 (RL-20260927-04), IN TWO LIMBS, scored on every D-6 line that prints: (i) a vendor completion of a container MOVE with the container's own fix within 100 m of the route end takes the hand-on line ("... COMPLETE with the container <d> m from the route end - handed on to the task's completion rules", d <= 100) and the completion rules then close the task (SEEN in G1: T14, 9 m); (ii) a completion more than 100 m short is WITHHELD ("... completed short: <d> m from the route end ... TASKCMPLT is WITHHELD") and the task is then closed only by its arrival evidence (L-ARRIVE, then TASKCMPLT) or by the time rules and the watchdog (OVERDUE, then TASKABRT STALLED; RL-20260921-09) - never by that completion (UNSEEN before G1-2). Expected here: T10 and T14 print neither (their arrival evidence comes first; the later vendor completion is swallowed, P16); T02 prints limb (ii) only if its blocked move at the river ENDS with success (O2). A limb that does not fire is NOT EXERCISED, not a miss. | HIGH | A hand-on beyond 100 m, a WITHHELD completion within 100 m, or a TASKCMPLT that follows a WITHHELD line with no L-ARRIVE between = STOP (read with P-FALS (b)). | |
| P16 | VENDOR COMPLETIONS inside the window - G1's: T14's container reports none (a snapshot member never finishes); T10's container reports one "VRF task complete: 1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE / <type> (success=True)" - the FULL name, resolved from the container's 30-character marking (O7) - and L-SWALLOW after its arrival evidence. The literal <type> RECORDED (expected "pa_move_along_route"; ZERO "attribution anomaly" lines, InFlightTracker.cs:96-101). | MEDIUM | Another count = recorded MEDIUM miss. | |
| P17 | REPORT HYGIENE - G1's: ZERO SUPPRESSED; ZERO `task=(none)`; every SENT line's taskee is one of the three performers' C2SIM uuids (ZERO member TaskStatus - P-FALS (d)); capture and log agree; "Reports this run: ... 0 FAILED". The capture's ObservationReports RECORDED against the expectation 20 = 7 init R-SURFACE-PROXY + 3 re-announcements + 2 STP-866 holds + 8 pre-flight. | HIGH | Any limb = STOP. | |
| P18 | NO ENGAGEMENT - G1's: zero "Fire Weapon" / FireAtTarget lines. | HIGH | Any = STOP. | |
| P19 | TILE READS - G1's: the run's tile total reads 0 HTTP FETCH(es), 0 given up, 0 undecodable; the cache manifest is unchanged after the pre-warm and after the run (682bdea4, 479). Hits RECORDED (harness: 12 slot + 49 route reads). | HIGH | A fetch or a changed manifest = STOP. | |
| P20 | RUN HEALTH - G1's: no BACK END LOST, no "Tick phase ... FAILED", no NO PERFORMING UNIT line, no WS-runaway exit 6, no new .dmp / .callstack.log for this back-end pid (names only); VrfC2SimApp exits 0; rtiexec, rtiForwarder, rtiAssistant and every holder untouched. The raw vendor "No creator found" text in the app's stdout (G1: 18) is benign - COUNTED, never a stop. | HIGH | Any limb = VOID. | |
| P21 | TEARDOWN - G1's: StopVrf52 exit 0 or 6 inside the runner; no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left. | HIGH | Exit 3, 5 or 7 = STOP. | |
| P22 | CLOCK AND TIMINGS, RECORDED - G1's: every SIM/WALL RATIO line; each L-DISP SIMULATION stamp; each population's L-POP -> L-PUB WALL span (G1: 70.5 s, set by the create deadline - with every member attributed it is predicted to be seconds); each mover's dispatch -> L-ARRIVE / L-STALL in SIM s against sec 1(j); the holds' L-TIMED served figures; the watchdog's dormancy warnings, if any. | RECORDED | - | |
| P23 | THE LOADS, RECORDED (sec 1(c): the cache is warm): the pre-warm's and the scored run's Stage 3 LaunchVrf READY and "scenario LOAD CONFIRMED", the oracle-gate wait, READY TO TASK, the init L-PLACE counts and every L-PUB time; the sim cache listing before W, after W and after E. Expectation (MEDIUM): BOTH runs' init L-PLACE read "36 of 36 create altitude(s) came from the TERRAIN QUERY" (G1's pre-warm read 36 FALLBACK on the cold cache, its scored run 36 TERRAIN QUERY), and neither run adds a layer directory. | MEDIUM | Recorded MEDIUM miss. | |
| P-FALS | THE FALSIFIERS - any one = STOP and ask, before anything else is run - G1's: (a) a SILENT stop of a populated container: a mover whose container stops for 360 SIM s or more short of its destination with NO L-STALL, NO L-D6 and NO refusal, while the watchdog was judging; (b) a container's MOVE task (T02, T10, T14 - not a hold) "completing" (a vendor completion, L-D6 or TASKCMPLT) within 60 SIM s of its dispatch with its centroid displaced 50 m or less; (c) ANY delete of a container or member during the run (a DELETE-family line before "Application is shutting down", or a container uuid that changes); (d) ANY TaskStatus for a member. | HIGH | - | |

OBSERVATIONS - with branches, not HIGH; each RECORDED with its lines and trace measures:
- O1 48 IBCT'S CONVERGENCE: the 17 members, born on the 490 m ring, each given its own "move-along" on the same route by
  the PA script (PA_Move_Along_Route.lua :26-91), close on vertex 0 and share the road - footprint overlap lowers only
  MAXIMUM speed (UG52 27.1.4). RECORDED per member: first fix within 50 m of vertex 0, the order of arrival there, the
  longest interval the container's centroid moved < 50 m (branches: a queue at vertex 0; members that never reach it;
  members that take another way).
- O2 T02 AT SPEED FACTOR 0: 28ID's one member meets river 8011072 (MAK_WATERWAY, speed-factor 0 in the aggregate mobility
  table; M2's pre-flight REPORTS rivers only, RL-20260927-01). What the vendor's move-along does there - (a) runs forever
  (no completion; OVERDUE at 300 s, the watchdog's TASKABRT from ~360 SIM s later: S5), (b) ENDS with success short of the
  route end (P15 limb (ii): WITHHELD, then the time rules RL-20260921-09 and the watchdog close T02), (c) FAILS (the vendor
  failure path, TASKABRT) - and how the interface closes T02. Observed, not solved.
- O3 1-112 IN'S FIVE MEMBERS ON T10 (a dry line, every slot and unpack target dry): the ordinary case - convergence on
  vertex 0, the route in file, the unpack; the vendor completion after the arrival evidence (P16). 1-112 IN performs T10
  only; T02's performer is 28ID with one member (O2).
- O4 THE CONTAINER'S PUBLISHED POSITION WHILE MOVING, per mover: the container's fix against its members' unweighted mean
  at every common instant after dispatch - max, mean, at the last instant (G1: up to 150.2 m behind its one member, mean
  15.8 m, 0.0 m at rest). It is the position every judge reads (sec 1(p)).
- O5 A MEMBER'S OWN TASKSTATUS: none is predicted (a member's completion is at Debug, VrfC2SimService.cs:9299-9305; P17's
  hygiene limb scores it HIGH); RECORDED as the count of SENT lines whose taskee is not a performer.
- O6 THE RAW "No creator found" COUNT (vendor SDK text interleaved into the app's stdout; G1 18): benign, counted.
- O7 COMPLETIONS BY MARKING: TaskCompletedEventArgs carries UnitMarking only; it is resolved marking -> uuid -> name
  (RUNBOOK 11i; VrfC2SimService.cs:9263-9280). COUNTED: the INFO "VRF task complete: <name>" lines that name a performer
  by its FULL requested name (the container's marking is its first 30), those that name anything else, the L-ID-MARK
  WARNs and the "no C2SIM uuid known" lines (both predicted 0). Member completions are at Debug and not in the log.

NAMED STOP-WITH-REPORT BRANCHES - recorded with their lines, the run continues (a branch other than S5 / S6 on T02 is
not predicted, so it is also a miss of the row it contradicts) - G1's S1-S6, and S7:
- S1 BRIDGE REFUSAL: "the loaded VrfBridge.dll has no PublishedSubordinateCount ..." (VrfC2SimService.cs:3054-3057) - should
  NOT happen on 03226dd0 (it carries PublishedSubordinateCount - p12, the NEW PIN record [A]); it would stop the start (P0).
- S2 POPULATION TIMED OUT: "POPULATE <c>: POPULATE TIMED OUT after <s> s: <why> - its MOVE tasks are REFUSED (TASKABRT) ..."
  (G1 saw the why "NONE of its N member(s) was created", ContainerPopulation.cs:349) and that container's move tasks REFUSED
  with TASKABRT.
- S3 CREATES LOST: "only n of N member(s) were created ... the rest are NOT part of this unit: [...]" (the rest attached and
  the move goes on with n).
- S4 ROW REFUSED: "COMPOSITION ROW ... is REFUSED" / "POPULATE <c> (...) REFUSED - ..." and the move refused with TASKABRT.
- S5 STALL WATCHDOG: L-STALL + TASKABRT "STALLED ..." on a container that stops (predicted for T02).
- S6 WATER STOP: a container (or a member) stopped at OSM water, with the pre-flight's report on the bus and, if its script
  then ends short, an L-D6 WARN "completed short: <d> m from the route end" (predicted for T02 at the river and for the
  INF1RIF2 member at lake 197345448).
- S7 UUID NOT HONOURED (the P3 falsifier in full): 59 L-ID-NOTREQ WARNs, one per requested object's returned marking; L-ID-INIT
  "IDENTITY: 0 of 36 objects bound by uuid, 36 by name (...)" at WARN; 35 L-TRUNC at init; 23 L-MEMBER lines ending "; bound
  by the name registry, NOT by its uuid (C1d)."; the three L-ID-POPSUM "0 of N ..., N by name"; 0 "MORE THAN ONE" and 0
  NAME REBIND REFUSED (C1c's names keep every marking distinct); the containers' VRF uuids (L-BIND, L-SCRIPT, the trace)
  VR-Forces-generated. Every other row is scored as registered. In the pre-warm, S7 is a STOP before E (sec 3 W).

WHY THE HIGH ROWS ARE HIGH - G1's reasons, and for P3: C1d's code is pinned offline (--populate-selftest p15: G1's 23
members with their OLD 34-character names bind 23 of 23 by uuid through BindCreated; --name-selftest the derivation, the
fallback and the line shapes) and the deployed bridge carries the overloads (p15 on the deployed exe [A]); what no offline
test can reach is VR-Forces honouring a caller's startingUUID for an entity or aggregate - the evidence is one control
area (BUFFALO, G1) passed the same way (VrfFacade.cpp:1013 vs :960 / :976) and the vendor's own signatures (sec VENDOR
CITATION). It is HIGH because the mechanism is documented and the conversion is the one that worked; it is the run's
first prediction because everything downstream reads through it.

STOP RULES - G1's:
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P20 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- P-FALS met, or P11 / P14's HIGH limb missed: STOP and ask, before anything else is run.
- The executor never intervenes in a window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as IRONSTORM_AGG_G1-<date>-3, with new appNumbers.

ONE VARIABLE: the deployed build - C1d's identity by uuid, C1c's names and D2's runner-side choice (whose Vrf__Scenario
export changes one clause of L-VARIANT, P1). G1 (20260928T102541Z) is the control: the same order, fixture, init,
composition, settings, sequence and harness geometry. Environmental differences named, not intended: the sim cache is warm
(P23); in branch (a) the RtiProbe tree keeps the superseded bridge (DEVIATION).

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5233 ***` in the main checkout's working tree [V 13:56:40Z and at the
registration commit] - the seat's dry run 11:22:58Z read the same ("marker currently reads : 5233 ... would advance to:
5244"). NOTHING is claimed by this registration. At the go-live, from the marker M read then (5233 unless another run has
moved it; the layout shifts with it), per the branch fixed at step A:
- BRANCH (a), holder 33476 REUSED (appNo 5207, nothing claimed for it):
  - 5233-5243: the PRE-WARM runner block (step W), written by the runner at its Stage 2 - 5233 back end, 5234 front end
    (BURNED, --no-gui), 5235 WatchVrf pre-check, 5236 WatchVrf trace, 5237 VrfC2SimApp, 5238 RtiProbe 2c, 5239 CreateOne
    (BURNED unless the oracle gate fails), 5240-5243 Stage 2h holder attempts (5240 JOINS; 5241-5243 BURNED); marker -> 5244.
  - 5244-5254: the SCORED runner block (step E), same layout - 5244 back end ... 5248 VrfC2SimApp ... 5251-5254 Stage 2h;
    marker -> 5255.
- BRANCH (b), a NEW persistent holder - the holder's own claim first, so every block shifts by 4:
  - 5233-5236: the new holder (step D), HAND-CLAIMED in Appendix B BEFORE it joins by claim_holder_g1_2.ps1 -From 5233
    (the G1 procedure: claim_holder.ps1's form, applied by the seat to the main checkout's working tree and committed on this
    branch with the Result); marker -> 5237. Expected: 5233 CONSUMED on a first-attempt join; 5234-5236 BURNED.
  - 5237-5247: the PRE-WARM block (5237 back end ... 5241 VrfC2SimApp ... 5244-5247 Stage 2h); marker -> 5248.
  - 5248-5258: the SCORED block (5248 back end ... 5252 VrfC2SimApp ... 5255-5258 Stage 2h); marker -> 5259.
- BRANCH (a) FOR W THEN (b) FOR E (E's check failed the hold limb, sec 3): W 5233-5243 as (a); the holder claims 5244-5247
  (marker -> 5248); E 5248-5258; marker -> 5259.
The layout is the runner's (block M..M+10, marker -> M+11; RunC2SimScenario.ps1 Stage 2, the dry run [V]). A launch that
aborts burns its whole block. Never reuse a number. PushInit / PushOrder / ListenReports / StopIface are C2SIM clients, not
federates.

## 6. Harvest (after the runs, read-only) and where results go

From each run directory (runs\launch52\last-run-dir.txt): vrfc2simapp.log, reports-captured.log, c2sim-bus.log, the
manifest, watchvrf-trace.csv, thread-samples.csv, holder logs, stopvrf and launchvrf logs, the wrapper log; scratch
u3\laneG1-2\g1_2_score.py <runDir> (score_g1_2.txt; the pre-warm's with --wgate, then in full as an observation); lane G1's
harness outputs and g1_predictions_h0.json for the expected geometry (the member names mapped by suffix); simcache listings.
Vendor sim logs (runs\*\vendor) dump the environment in cleartext: count-grep only, never quoted or attached; the app log
is ours and may be quoted. Results go to: the Result block below (measurement and implication in separate sentences);
PLAN_MOVEMENT_2026-09-27.md rows G1-2, C1d (the live-owed claim) and C1, and sec 5 (the open questions G1-2 answers);
RUNBOOK sec 11i ("LIVE-OWED" -> what was seen); Appendix B annotated from the manifests. ASCII + CRLF.

## 7. What this run does NOT claim

- G1's: the map display (--no-gui); the authored variant, the derived SMS, the seven authored US types (G1b); nested
  containers; the STP TO precedence; a patrol or a point move; combat, posture, supply or any warfare-model outcome (no
  hostile within 40 km); anything about the entity-level profile, the full 23-task order, the demo server or the demo
  profile; the sim/wall ratio of the aggregate profile in general; that the members' convergence to vertex 0 is how a
  doctrinal brigade moves.
- Rivers solved: T02's river branch (O2) is OBSERVED, not solved - the pre-flight reports it, nothing routes round it.
- Timing: n = 1 - one host, one fixture, one run; no timing figure generalises.
- D2 is verified by its CHOICE LINE only (P0 (iii)): one order above BN, one automatic choice; its BN-and-below and override
  paths are not exercised.
- The uuid mechanism (C1d) is proven for UNITS only if P3 holds - 36 containers and 23 aggregated members; a PLATFORM
  (createEntity) is not created on this profile and stays untested live; so do a synthesized sub-unit and a template
  re-create (their derived uuids: 0 EXPAND / re-create lines predicted).
- That a completion's marking can never be ambiguous: C1c's names keep markings apart for the names the interface requests;
  a marking two uuid-bound objects share would still be unattributable (the RUNBOOK 11i residual).

## Result (written after the harvest, never from a live read)

Written 2026-09-28 ~15:15Z by lane G1-2 (session 5fc25950) from the harvested files of the scored run 20260928T142731Z_run
and the pre-warm 20260928T141734Z_run (main checkout runs\): vrfc2simapp.log (232,949 lines; L = its line numbers; PL =
the pre-warm's), reports-captured.log (C = record number; 1,326 records), run-manifest.json, watchvrf-trace.csv (t =
trace seconds from the WatchVrf join at 14:30:04.515Z), the runner logs runs\launch52\RunScenario-ironstorm-agg-g1-2-
20260928T142730Z.log (R) and ...-prewarm-20260928T141733Z.log (RW), and the rtiexec log (count-grep only). Instruments,
scratch u3\laneG1-2: g1_2_score.py (score_20260928T142731Z_run.txt and W_gate_20260928T141734Z_run.txt, run by the seat;
score_prewarm_full_20260928T141734Z_run.txt), g1_2_harvest.py (lane G1's g1_harvest.py plus a G1-2 tail;
harvest_scored.txt, harvest_prewarm.txt), lane G1's capture_census.py (capture_scored.txt), t14_members.py,
mech_stop_check.py, member_console_census.py, console_members_check.py, o1_intervals.py, sim_at_line.py (the SIM clock at
a log line, bracketed by the relayed vendor console stamps, which lead the app's own clock reading by 0-21 SIM s here),
rtiexec_count_g1_2.py (counts only) and appendixb_diff.py; the seat's go-live outputs (D_prelaunch_a.txt 14:16:12Z,
D_prelaunch_E.txt 14:26:53Z, F_check.txt 14:39:51Z, the C3 files, simcache_after_prewarm.txt, simcache_after_scored.txt).
Vendor sim logs (runs\*\vendor, runs\launch52\vrfSim_*) were not opened; every vendor console text quoted below is the
app's own relay of it in vrfc2simapp.log. "reg. L" = the row's line in the registration as committed (7918ab2), before the
STATUS edit and this Result. [V] = checked in the harvest; [A] = taken from the seat or inferred.

GO-LIVE, as sec 3 wrote it, HOLDER BRANCH (a), with these differences [V unless marked]:
- A: golive_checks_g1_2 -Phase prelaunch -HolderPid 33476 -MarkerWant 5233 -Branch a -MinHoldLeftMin 20, run by the seat
  at 14:16:12Z: 0 FAILED - exe 7824483a, dll bb7e6361, appsettings 0f600f0e, bridge 03226dd0 in the app, WatchVrf and
  CreateOne trees, 5198ac45 in the RtiProbe tree (the branch (a) DEVIATION), PV 1.0.0+git.699552c.Release-5.2, fixture
  804e2c39, order 7a986137, init 2000e856, type map c546edbe, composition 9684e945, cache 682bdea4 / 479 files, REST 200,
  the RTI trio and holder 33476 up (239 min of hold left), marker 5233; main at 7c13441 (this registration merged), `git
  diff 699552c main -- src` empty. A2 was not re-run: the NEW PIN record's p15 PASS stands [A], and the live start printed
  0 IDENTITY REFUSING (P3 I1).
- C3, run by the seat 14:15:41-14:16:12Z under RL-20260928-01 (c3_push_g1_2.sh unchanged): PushInit "QUERYINIT : 40
  Units, SystemName=[Not Set]", PushOrder one "[14:15:42.223] ORDER (69670 chars)"; the bus log carries 5
  ManeuverWarfareTask, 5 Duration and CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT; exit 0 / 0.
- D0 and D not run (branch (a)); no hand claim.
- W, run by the seat under RL-20260928-01: dry run 14:17:16Z exit 0 (block 5233-5243, "marker would advance to: 5244",
  holder 33476 PERSISTENT, the D2 lines); the pre-warm launched 14:17:33Z, its ORDER on the bus 14:21:01.653Z (RW260),
  window 120.6 s of its 120 s cap (RW273), app exit 0 (RW281), StopVrf exit 6 (RW290), end 14:26:08Z (RW304). THE W GATE
  HELD: W_gate_20260928T141734Z_run.txt "W GATE: PASS" (identity branch A; W0-W7 and I1-I17 PASS; the full score adds I18,
  26 of 26); the E check at 14:26:53Z (0 FAILED) saw only the RTI trio, holder 33476 and the pre-warm's Stage 2h holder
  34140, cache 682bdea4 / 479, marker 5244. DEVIATION: the "before W" sim-cache listing was not taken; the prep listing
  (13:59:58Z) stands for it - nothing was launched in between, and the three listings are identical (P23).
- E, run by the seat: dry run 14:27:14Z exit 0 (block 5244-5254, marker -> 5255, holders 33476 and 34140 PERSISTENT); THE
  RUN launched 14:27:30Z (runner start 14:27:31.3Z), its ORDER on the bus 14:30:38.923Z (R260), the window closed EARLY at
  14:36:33.8Z (R280; 324.8 s of the 2,700 s cap, R281), runner exit 0, end 14:39:09Z (R312) - why so short: THE 12-MINUTE
  RUN below.
- F, by the seat at 14:39:51Z: 0 FAILED - every hash and the cache manifest unchanged; up: the RTI trio, holder 33476 (215
  min left) and the run's own Stage 2h holder 51272 (inside its 900 s hold); 34140 had left after its own hold; marker
  5255. Sim cache listed 14:39:57Z.

### VERDICT: STOPPED at P12 (HIGH), by the letter. T14 did not arrive: the watchdog closed it with TASKABRT (L170139 / L170141) while 48 IBCT's own fix rested 770.9 m short of its destination - 11 of its 12 Mech CO members stopped together inside an OSM building of the -2 hamlet on the vendor's slope check ("Terrain too steep, slope: 408.844% max: 78.5398%", once per member, N1) and the far-shore member never left its slot, while P10a held (16 of 17 moved), so T14's arrival limb is scored HIGH and the S5 escape of P12 does not apply. The mechanism proved out around that miss: IDENTITY BY UUID HELD LIVE (P3; RL-20260928-02) - 59 of 59 objects created under the requested uuid and bound by it in both runs, all 23 members attributed; every population published N of N 1.7 s after it began; the three container scripts ran; T10 ARRIVED on its arrival evidence; T02 stopped at its river and the watchdog closed it, as predicted; the time rules held T14 OVERDUE with no premature TASKCMPLT. MEDIUM misses: P12b (T14's path), P13 (771 m, not 100-250 m), P23 (the pre-warm's init took its altitudes from the FALLBACK with the sim cache warm). P-FALS did not fire. Every other HIGH row held; P15 was NOT EXERCISED (no D-6 line), which its letter says is not a miss.

| # | Reg. | Verdict | Evidence |
|---|---|---|---|
| P0 | L515 | PASS | (i) R89 holder 33476 "a PERSISTENT FEDERATION HOLDER" (RW89; R90 also the pre-warm's 2h holder 34140); rtiexec count-grep 10:27:30-10:39:15 local: 12 federation creates refused as already existing, 0 created, 12 JoinConfirm (pre-warm 10:17:33-10:26:14: 12 / 0 / 12); L67 "READY - joined the federation" once. (ii) L12 "BUILD IDENTITY: git 699552c"; every hash and the cache manifest identical at 14:16:12Z, 14:26:53Z and 14:39:51Z. (iii) R19 "model set   : AggregateTacticalLevel <- auto (RL-20260927-06): highest TASKED echelon DIV (28ID__FRIENDLY_INFANTRY_DIVISION) is ABOVE BN - aggregate-only; exported to the app as Vrf__ModelSet", R28 the pairing line "... matches the chosen model set.", R32 "catalogue   : not exported - the app keeps the default "" (the model set's own SMS)", R21-R23 DIV / BN / BDE (RW19-RW32 the same); manifest inputs.modelSet.modelSet AggregateTacticalLevel, selection.highestEchelon DIV. (iv) 0 REFUSING TO START, 0 "AGGREGATE CONTAINERS off", L32 L-IDENTITY-ON once. (v) L50 "Vrf:TaskPredecessorTimeoutSeconds=600 s"; L560 (T02) and L576 (T14) "and then 600 s to COMPLETE". (vi) the W gate held (GO-LIVE above). |
| P1 | L516 | PASS | L14 L-FIDELITY (62 rows); L16 L-CAT (705 templates, 3 model-set dirs, root AggregateTacticalLevel, C:\MAK\vrforces5.2d); L18 L-VARIANT ("5 of the 8 row(s)", "SMS: the fixture C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Aggregate.scnx loads $(DATA_DIR)\simulationModelSets\AggregateTacticalLevel.sms.", "7 authoredRows type(s) ... NOT in use") - the D2 clause, where G1 read "Vrf:Scenario is not set."; L20-L28 five L-ROW 1/0, 17/3, 5/0, 26/6, 8/1, 0 REFUSED; L30 L-CON-ON ("hostile nation RUS", "(5 row(s) resolve, 0 refused)", "85 s", "100 m"); L32 L-IDENTITY-ON (namespace 485b28e7-1cc7-534b-b6a6-9beddf07a1b1); L44 L-MODELSET; L42 L-M1-ON; L34 L-WDOG-ON (360 s, SIMULATION, 50 m); L50 L-CLOCK (SIMULATION, 0.25, 600, 60, 86400); L40 L-SHIFT-ON; L578 L-OSMSET once (osm-water 225 tile files, 0 empty; \osm 225, 0), after the first L-POP (L554) and before the first L-DISP (L1036); 0 "is not EntityLevel or AggregateTacticalLevel". L79 and L580 are other lines (the init's type-mapping line; "ROUTE PRE-FLIGHT enabled"), not repeats. |
| P2 | L517 | PASS | (a) 36 TYPE MAP ... CONTAINER lines L83-L173: 29 Exact, 7 Proxy "NEAREST branch"; L167 48 IBCT -> BDE (11.1.225.8.3.1.1), L125 1-112 IN -> BN, Light Infantry (11.1.225.6.3.1.0), L91 28ID -> DIV, Mech Infantry (11.1.225.9.4.1.0) Proxy; 11 of 11 hostile containers nation 260. (b) 0 L-NOCONT, 0 NAME DISAMBIGUATED (C1c), 0 NAME COLLISION (C1c). (c) 7 L-PXYTAG (L81-L169). (d) L181 "36 container(s) ... 0 other aggregate shell(s); 0 platform(s)", no entity census; L191 "36 object(s) ... (36 empty shell(s))"; L199 "36 units + 5 areas"; L197 "7 PROXY substitution(s)". (e) the 36 container names bound by L-BIND before the order (L546) - L301-L447, the two "(res)" names at L325 and L386 included - each under VRF_UUID:<its own C2SIM uuid> (I17); 0 member names before it (the 5 areas are bound too, L203-L211). (f) 0 DELETE-family lines before L232865 "Application is shutting down..."; the shutdown's L232869 "Cleanup: deleting 67 created VR-Forces objects before resign..." and L232943 "Cleanup: 67 deletes dispatched" are excluded by name. (g) 0 L-TRUNC. RECORDED: L452 READY TO TASK "36 of 36 ... after 7.8 s"; L287 "36 of 36 create altitude(s) came from the TERRAIN QUERY"; 0 ORDER BEFORE READY TO TASK. |
| P2b | L518 | PASS | L177 / L426 28ID/III_Corps... "will attach 9 declared child unit(s)" / "composed - 9/9"; L179 / L404 278_Armored_Cavalry_Regiment... 1 / "1/1". |
| P3 | L519 | PASS - THE LIVE-OWED CLAIM HOLDS for aggregates | Branch A, I1-I18 all PASS (score lines 152-174): (I1) L32 once, 0 IDENTITY REFUSING; (I2) 59 L-ID-CREATED at INFO - the 36 init containers L299-L444 under their C2SIM uuids, the 23 members L714-L868 under the derived uuids of sec 1(i); (I3) 35 cut markings, L444 4ID__FRIENDLY_INFANTRY_DIVISION whole, every member its own name; (I4) 0 L-ID-NOTREQ; (I5) L454 "IDENTITY: 36 of 36 objects bound by uuid, 0 by name (the initialization's own objects, at READY TO TASK; RL-20260928-02)" at INFO after L452; (I6) L556 / L564 / L572 exactly the derivation; (I7) L722 "1 of 1", L756 "5 of 5", L876 "17 of 17 objects bound by uuid, 0 by name"; (I8) 23 L-MEMBER L718-L872 "...; bound by its uuid (C1d)."; (I9-I16) 0 of each; (I17) all 59 L-BIND under VRF_UUID:<the requested uuid>; (I18) the WatchVrf trace has POS rows under the requested uuids of the 3 performers and the 23 members, 26 of 26. The pre-warm read the same (W gate; I18 26 of 26). S7 did not fire. The strongest competing hypothesis the row named (the bare string treated as a marking lookup) is falsified by I2, I4 and I18. |
| P3a | L520 | PASS | One L-POP each at order receipt: L554 28ID (task 'T1_...' performer; source 3, C-USA-DIV-UCI / F-UCI-I, 1, radius 0 m), L562 1-112 IN ('T10_...'; C-USA-BN-UCI / F-UCI-F, 5, 153 m), L570 48 IBCT ('T13_...'; C-USA-BDE-UCI / F-UCI-H, 17, 490 m, 3 sub-containers flattened); the member lists are sec 1(i)'s names in slot order; N L-SLOT lines each (L598; L602-L610; L614-L646); one L-ISSUED each (L600, L612, L648); one L-ATTACHED each - L720 "1 of 1 ... (28ID__FRIENDLY_INFANTRY_DI.HQ1 first)", L754 "5 of 5 ... (1-112_IN/28ID__FRIENDLY_IN.HQ1 first)", L874 "17 of 17 ... (48_IBCT/28ID__FRIENDLY_INF.HQ1 first)"; one L-PUB each - L1016 "PUBLISHES 1 subordinate(s) (expected 1)", L1018 5 (5), L1020 17 (17); one L-REANNOUNCE each (L558, L566, L574 "composed from 1 / 5 / 17 sub-unit(s)"); all 23 member names bound (L716-L870); 0 REFUSED, TIMED OUT, partial, late-member or slot-check-late lines. RECORDED: L-POP order 28ID, 1-112 IN, 48 IBCT; no HELD or deferred population (L650-L654 are the members' terrain-profile requests); L-PUB 1.7 s after each population began (1.0 / 1.0 / 0.3 s after the attach; G1 70.5 s); member placements L662 (1 of 1), L674 (5 of 5), L712 (17 of 17) from the TERRAIN QUERY. |
| P3b | L521 | PASS | 28ID L598 clear; 1-112 IN L602-L610 5 clear; 48 IBCT 13 clear + slot 4 L620 "SLOT MOVED 125 m north-west" (OSM 197345448; INF1RIF2 at 54.022284, 23.319544), slot 5 L622 "125 m south-west", slot 6 L624 "25 m south" (both 197345448), slot 16 L644 "75 m south-east" (OSM 16373225); 0 KEPT, 0 UNVERIFIED. |
| P3c | L522 | PASS | L1016 (28ID) before T01 L1036 and T02 L29395; L1018 (1-112 IN) before T10 L21364; L1020 (48 IBCT) before T13 L1064 and T14 L29255; "(expected N)" = N; 0 memberless-move refusals. |
| P3e | L523 | PASS | 0 "is NOT populated - D-5"; three L-POP only - 116_ABCT/... has none. |
| P4 | L524 | PASS | T01: L-DISP L1036 (hold-in-place) after L-PUB L1016; L1042 L-CNFPSL; L1044 L-INPLACE; L1040 armed 300 "no destination"; C49 STP-866; L29021 TASKCMPLT "(300 s after dispatch)". T13: L1064 after L1020; L1070; L1072; L1068; C48; L29015. 0 routes or scripts for a hold, 0 OVERDUE for a hold, one TASKCMPLT each. RECORDED: L1072 48 IBCT in place at (54.01939,23.31390) - the container's own point (L570 "round its point (54.019389,23.313902)"), 8.1 m from its members' centroid: T13 dispatched before the container's fix moved onto its members' mean (it had by T14's dispatch, origin fix 54.019341, 23.313809). The holds served 330 of 300 s (L29011, L29017; G1 300) - UNEXPLAINED item 4. |
| P5 | L525 | PASS | 5 SENT TASKSTRT (L1038 T01, L1066 T13, L21366 T10, L29257 T14, L29397 T02); T02's L-DISP L29395 after T01's TASKCMPLT L29021; T14's L29255 after T13's L29015; L568 "Task 'T10_...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25)" and T10's L-DISP L21364 after it; 0 SKIPPED; 0 gate-expiry lines. RECORDED: T10 dispatched first (14:31:06.612Z, SIM 317.5), then T14 (14:31:17.952Z, SIM 410.5) and T02 (14:31:18.199Z, SIM 410.5). |
| P6 | L526 | PASS | L29049 "(2 vertices)" T02, L20970 "(4 vertices)" T10, L29039 "(3 vertices)" T14; L-VCHECK L29231 1 / L21224 3 / L29179 2 checked, "0 moved, 0 kept on bad ground, 0 unverified"; L-ELEV L29233 L12 x1, L21226 x3, L29181 x2; L-SLOW L29239 3370 of 5342 m; L21234-L21238 805/1451, 103/804, 369/516 m; L29187-L29189 1270/1270, 1663/2902 m; L29237 "OSM 8011072 (waterway=river (MAK_WIDTH 5 m))", first at (54.00877,23.23512) 2.40 km along; L29241 RIVER CROSSING; L21240 (T10) and L29191 (T14) L-NOFLAG; 0 VERTEX MOVED / NOT MOVED / UNVERIFIED, 0 "could NOT be read", 0 ROUTE SHIFTED, 0 "NO CLEARED LINE", 0 PRE-DISPATCH, 0 land-cover "WATER ON THE LINE"; the capture holds exactly 8 pre-flight ObservationReports (C123-C125, C201-C205). |
| P7 | L527 | PASS | L21370 "(4 pts) for CONTAINER 1-112_IN/... - 5 published member(s); RunScriptedTask PA_Move_Along_Route deferred to route-created", L29261 "(3 pts) ... 48_IBCT/... - 17", L29401 "(2 pts) ... 28ID__FRIENDLY_INFANTRY_DIVISION - 1"; L21465, L29311, L29593 "RunScriptedTask PA_Move_Along_Route issued for CONTAINER <c> (VRF_UUID:<its C2SIM uuid>) with [route=VRF_UUID:..., reverseDirection=false, startAtClosestVertex=false] - 5 / 17 / 1 published member(s)"; L-DISP kind PA_Move_Along_Route (L21364, L29255, L29395); 0 "was NOT issued", 0 PA_Move_To_Location_Direct, 0 MoveAlongRoute, 0 MOVE TO PER VERTEX, 0 formation lines for a container. L-CONSOLE-MEMBERS: 11 lines, each 1 / 5 / 17 distinct members under their derived uuids (console_members_scored.txt: 0 mismatches); passes T01 1, T13 1, T10 3, T14 3, T02 3. |
| P8 | L528 | PASS | T02: one L-FAW (L29393, on its last pass) and the R3 line once per pass (L29047, L29269, L29391 - passes L29045, L29267, L29389); T14: L29253 L-FOLSPT "('ROEHold')"; T10: L-BARE L20966, L21256, L21362, one per pass; 0 L-FAW for another task. |
| P9 | L529 | PASS | L21483 (T10, 328.663), L29409 (T14, 420.129), L29603 (T02, 420.729) "Init of aggregate move along." on the container's console; 23 "Sub. <m> starting move-along." (L21485-L21493, L29415-L29463, L29605), each under the member's WHOLE C1c name; 0 "starting pa_move_along_route". RECORDED: "has completed move task." x10 - 1-112 IN 5 (HQ1 644.127, RIF1 and RIF2 1072.223, WPN1 1073.756, RIF3 1080.989), 48 IBCT 5 (INF1HQ1 1049.723, HQ1 1058.623, INF3HQ1 1061.689, CAV1 1064.789, INF2HQ1 1067.323); "has completed final move." x9 - 1-112 IN 5 (HQ1 696.326, WPN1 1196.988, RIF2 1199.055, RIF1 1200.621, RIF3 1202.121), 48 IBCT 4 (INF1HQ1 1103.722, HQ1 1119.755, CAV1 1133.022, INF2HQ1 1139.722); none from 28ID's member or from any 48 IBCT Mech CO. |
| P10 | L530 | PASS | Containers displaced after their L-DISP: 28ID 2,396.4 m, 1-112 IN 2,646.1 m, 48 IBCT 1,750.0 m; members > 50 m: 1 of 1, 5 of 5, 16 of 17. |
| P10a | L531 | PASS | Exactly 16 of 17, all but 48_IBCT/28ID__FRIENDL.INF1RIF2 (6.7 m); the near-water first legs passed (first within 50 m of vertex 0: INF1RIF1 t=98.5, INF1RIF3 t=100.5, INF3WPN1 t=100.5, INF1WPN1 t=104.6). |
| P10b | L532 | PASS | At the last common instant (t=387.1) each container's fix lies 0.0 m from its members' unweighted mean (all three). |
| P11 | L533 | PASS | INF1RIF2 never nearer than 490.8 m to (54.019341, 23.313809); outcome (a): it stayed within 6.7 m of its slot, and L29881 relays its vendor console "Movement constrained by features" at SIM ~423, 12 SIM s after T14's dispatch. |
| P12 | L534 | MISS (HIGH) - STOP | T14 closed on L-STALL, not L-ARRIVE: L170139 "STALL: unit 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE task T14_...: no member moved more than 50 m in the last 360 SIM s (max 39.6 m); TASKABRT reported. ONE POSITION: ..." and L170141 TASKABRT "STALLED (C16 progress watchdog) - report only: ..." (C1002, 14:35:03.830Z; SIM ~2017), 0 ARRIVAL EVIDENCE for T14. P10a held, so by the row's own words T14's arrival limb is scored HIGH and "closing on L-STALL instead is RECORDED as S5" does not apply: that clause was written for members held on their first legs by water; here 11 members moved > 50 m and were held later, on the line, by a building (N1) - a case the registration did not name. Held: T10 L74355 ARRIVAL EVIDENCE "1/1 member(s) within 500 m of the last vertex (nearest 319 m) AND past their OWN traversal bar ... ONE POSITION: ...", then L74359 TASKCMPLT; T02 L94613 L-STALL (max 0.0 m; ONE POSITION) and L94615 TASKABRT "STALLED (C16 progress watchdog) ..." (S5, as predicted); every judge read the container's own reflected position ("1/1", the ONE POSITION suffix on all three lines); 0 L-CANNOT; 0 VACUOUS; exactly one terminal report per task, 5 in all. |
| P12b | L535 | MISS (MEDIUM) | T14: L57139 OVERDUE ("307 s of a 300 s Duration served ... NOT ARRIVED: OVERDUE"), then the stall (L170139), not an arrival. Held: T10 L62991 OVERDUE ("467 s of a 450 s"), L74355 L-ARRIVE, L74357 "was OVERDUE and the unit has now ARRIVED", L74359 TASKCMPLT "arrived after its task's end time (start time + Duration) - complete on arrival."; T02 L57137 OVERDUE ("307 s of a 300 s"), then the stall (L94613). |
| P13 | L536 | MISS (MEDIUM) | T14's container came no nearer than 770.8 m (final 770.9 m, from t=263.2), not 100-250 m: its fix is the mean of 11 Mech COs 1,092.5 m short, 5 Strykers 446.6-496.5 m from the destination and INF1RIF2 2,036.7 m away (t14_members_scored.txt). The P11 outcome, (a), is the far-shore member's 1/17 pull the row assumed; the stop of 11 of 17 (N1) is what it did not. |
| P13b | L537 | PASS | T10's container came within 100 m of PassagePoint_48_IBCT_SLOT0 at t=163.4 and rests 0.1 m from it. |
| P14 | L538 | PASS (HIGH); PASS (MEDIUM) | 28ID's container never nearer than 2,944.2 m to its destination; its last fix (54.008761, 23.235090) is 2.5 m from the river point, 2,399 m along (<= 2,450); the stall's max 0.0 m (L94613). |
| P15 | L539 | NOT EXERCISED (not a miss, by its letter) | 0 L-D6 lines, either limb: T10's vendor completion came after its arrival evidence and was swallowed (L100817) as predicted; T14's and T02's scripts never ended, so there was no vendor completion to judge. The withhold limb (ii) stays UNSEEN. |
| P16 | L540 | PASS | T14's container reported no completion; T10's one: L100815 "VRF task complete: 1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE / pa_move_along_route (success=True)" - the full name, resolved from the 30-character marking - and L100817 "VRF completion for 1-112_IN/... after the arrival-evidence report of task 9aab7fe6-... - swallowed." after L74355. RECORDED: the literal type "pa_move_along_route"; 0 "attribution anomaly". |
| P17 | L541 | PASS | 0 SUPPRESSED; 0 task=(none); all 10 SENT lines' taskees are the three performers (200d3a3f x4, dd3d21b2 x4, 8d5b2ba6 x2); 0 member TaskStatus; capture = log (10 TaskStatus, C47-C1002; 1,326 records) and L232945 "Reports this run: 1326 delivered, 0 FAILED". RECORDED: exactly 20 ObservationReports, the expectation - 7 init PROXY (C1-C7), 3 re-announcements (C44-C46), 2 STP-866 (C48, C49), 8 pre-flight (C123-C125, C201-C205). |
| P18 | L542 | PASS | 0 Fire Weapon / FireAtTarget lines. |
| P19 | L543 | PASS | L232947 "49 cache HIT(s), 0 HTTP FETCH(es), 0 tile(s) given up on after 3 attempts, 0 undecodable body(ies)" (the pre-warm PL104999 the same 49); the cache manifest 682bdea4 / 479 at 14:16:12Z, 14:26:53Z and 14:39:51Z. RECORDED: 49 hits = the harness's 49 route reads; its 12 slot reads are not in the total [A: whether the slot check's reads are counted there is not read]. |
| P20 | L544 | PASS | 0 BACK END LOST, 0 "Tick phase ... FAILED", 0 NO PERFORMING UNIT; runner exit 0; R289 "VrfC2SimApp exited with code 0 (clean resign)"; 0 .dmp / .callstack.log under C:\MAK\logs or C:\MAK\vrforces5.2d newer than 14:10Z (names only); the RTI trio (30240, 47980, 50740) and holder 33476 untouched at every check. RECORDED: the thread sampler raised BACK-END WS RUNAWAY in both runs during the load (pre-warm 1045.4 MB/min at 14:18:59Z, 2417 MB, pid 40344; scored 746.3 MB/min at 14:28:57Z, 2561 MB, pid 39208; manifest backendWsRunaway=true) - an alert, not an exit, as in G1; the one fail-level line is L232948-L232949 "STOMP block reading cancelled", after the shutdown, as in G1 and the pre-warm. |
| P21 | L545 | PASS | R298 StopVrf EXIT=6: FORCED by identity (pid 39208) after the graceful close was refused; R303 "no VR-Forces processes remain", R304 "no WatchVrf / ListenReports observer remains"; F 14:39:51Z: no sim, app or observer. |
| P22 | L546 | RECORDED | L-RATIO 7.555 (L35045, sim 458.0 s), 6.630 (L71323, 856.1), 6.414 (L106169, 1241.0), 7.088 (L144319, 1673.3), 9.021 (L187267, 2221.3), 8.352 (L229923, 2730.5); L-DISP SIM stamps T01 / T13 80.6 s, T10 317.5 s, T14 / T02 410.5 s; every population 1.7 s from L-POP to L-PUB (the seconds predicted; G1 70.5 s). Dispatch to judge: T10 571.5 SIM s to L-ARRIVE (317.5 -> 889.0; sec 1(j) guessed 800-950); T02 ~306 SIM s to its river stop (console SIM ~717, L56719) and ~722 to L-STALL (SIM ~1132); T14 ~1,245 SIM s to the first Mech CO stop (SIM ~1655, L140151) and ~1,607 to L-STALL (SIM ~2017; sec 1(j) guessed ~1,800 to its route end). The holds served 330 of 300 s (L29011, L29017), the movers' OVERDUE 307 of 300 (T02, T14) and 467 of 450 (T10). No dormancy warning. |
| P23 | L547 | RECORDED; MEDIUM MISS on the pre-warm's L-PLACE | Pre-warm / scored: LaunchVrf (Stage 3) 70.4 s / 55.4 s to READY and "scenario LOAD CONFIRMED"; app start to order push 21.3 s / 16.3 s; oracle gate 72 / 72 real-coordinate POS lines across 36 uuids; READY TO TASK after 12.9 s (PL590) / 7.8 s (L452); init L-PLACE PL351 "0 of 36 create altitude(s) came from the TERRAIN QUERY, 36 from the FALLBACK" - PL277 "Terrain profile request 1 for task 'INIT PLACEMENT' got no reply within 10 s" - / L287 36 of 36 from the TERRAIN QUERY (the MISS: both were predicted TERRAIN QUERY); every L-PUB 1.8 / 1.7 / 1.7 s (pre-warm) and 1.7 / 1.7 / 1.7 s. Sim cache: 25 layer directories, 21,805 files, 495,598,329 B at 13:59:58Z, 14:26:53Z and 14:39:57Z - no layer directory and no file added (that limb held). N3. |
| P-FALS | L548 | NOT FIRED | (a) no silent stop: T14's and T02's containers stopped and the watchdog reported each (L170139, L94613); no dormancy. (b) no move "completed" within 60 SIM s of dispatch: T10's TASKCMPLT came 571.5 SIM s after it, 2,646 m displaced; T02 and T14 had no vendor completion. (c) 0 DELETE-family lines before L232865; the three performers' uuids are their C2SIM uuids throughout (L-BIND, L-SCRIPT, the trace). (d) 0 member TaskStatus. |

BRANCHES (reg. L575-L593): S1 not seen. S2, S3 and S4 not seen - every population published N of N. S5 SEEN twice: T02
(predicted; L94613 / L94615, max 0.0 m) and T14 (NOT predicted; L170139 / L170141, max 39.6 m), the held members named -
the 11 Mech COs 48_IBCT/28ID__FRIENDL.INF1RIF1, .INF1RIF3, .INF1WPN1, .INF2RIF1, .INF2RIF2, .INF2RIF3, .INF2WPN1,
.INF3RIF1, .INF3RIF2, .INF3RIF3 and .INF3WPN1 at one point 1,092 m short (N1), and .INF1RIF2 on its slot across lake
197345448; per the branch rule a branch other than S5 / S6 on T02 is also a miss of the row it contradicts - P12. S6 SEEN
as predicted: T02's member at river 8011072 (last fix 2.5 m from the river point; L56719 relays "Movement constrained by
features" at SIM ~717; the pre-flight's RIVER CROSSING and OSM WATER reports on the bus, C203-C204; its script never ended,
so no L-D6) and INF1RIF2 at lake 197345448 (L29881, the same line); and a third, which sec 1(i) foresaw but S6 did not
list: INF3HQ1 short of its unpack target in lake 197345447 (L93535, SIM ~1121; 34.6 m from the target, no "has completed
final move."). S7 not seen.

OBSERVATIONS (reg. L550-L573):
- O1 48 IBCT'S CONVERGENCE: three Stryker HHTs and the Stryker Cavalry SQDN came within 50 m of vertex 0 first (t=84.3-86.4;
  INF3HQ1 passed it at 53.7 m), the 11 moving Mech COs next (t=98.5-104.6); INF1RIF2 never (490.8 m). The container's fix
  moved less than 50 m for at most 18.3 s at the convergence (t=74.1-92.4) and 22.5 s on the Mech COs' last stretch
  (t=222.4-244.9), then not again from t=257.1. Branches: no queue at vertex 0; one member never reached it (the lake);
  none took another way (the 11 Mech COs ended on the line, offset 0-1 m).
- O2 T02 AT SPEED FACTOR 0: branch (a) - the move ran on: no vendor completion and no failure; OVERDUE at 307 of 300 s
  (L57137); the member stopped 2.5 m from the river point at SIM ~717 (L56719); the watchdog's TASKABRT at SIM ~1132
  (L94613 / L94615); the script was still running at the shutdown.
- O3 1-112 IN ON T10: 4 of 5 members within 50 m of vertex 0 at t=64.0-66.1 (HQ1 nearest 85.4 m); arrival evidence 571.5
  SIM s after dispatch (L74355, the per-member relaxation APPLIED); the unpack - HQ1's final move at SIM 696.3, the other
  four at 1197.0-1202.1 - each member 0.0-0.5 m from its unpack target, 152.5-153.0 m from the destination, the container
  0.1 m from it; the vendor completion (L100815) right after the last final move (L100809), swallowed (L100817).
- O4 THE CONTAINER'S FIX WHILE MOVING, against its members' unweighted mean: T02 max 432.3 m at t=116.8 (its one member
  reaching the river; the fix caught up within ~2 s), mean 18.4 m; T10 max 76.3 m at t=161.4, mean 9.1 m; T14 max 132.5 m
  at t=246.9, mean 23.3 m; 0.0 m at rest for all three (G1: 150.2 m, one member).
- O5 A MEMBER'S OWN TASKSTATUS: 0.
- O6 "No creator found": 18 (G1 18), L380-L770 and L21463 "Can't create data of type pa_move_along_route.  No creator
  found.  Only time this warning will be issued." (as G1's L81507); benign, counted.
- O7 COMPLETIONS BY MARKING: 1 INFO "VRF task complete" naming a performer by its full name (L100815), 0 naming anything
  else, 0 L-ID-MARK, 0 "no C2SIM uuid known".

N1 - WHY T14 DID NOT ARRIVE [V]. Measurement: of 48 IBCT's 17 members (t14_members_scored.txt, mech_stop_check.txt), the
four Stryker HHTs and the Stryker Cavalry SQDN drove the route and unpacked 446.6-496.5 m from the destination (last moves
t=179.7-183.7); INF1RIF2 never left its far-shore slot; and the other 11 Mech COs all stopped on the route line at ONE
point, (54.030655, 23.326944) - spread 0.1 m, 3,084 of 4,177 m along, 1,092.5 m from the destination, 18.6 m short of
E2's -2 hamlet stop (54.030807, 23.327063) and INSIDE an OSM building footprint (lane E2's reader on lane G1's copy of the
deployed tile cache: hit 'building'; no water or waterway within 200 m). Each of the 11 relayed exactly one vendor console
line, in the order they stopped - L140151 (INF1RIF1, SIM ~1655, last move t=257.1) to L143597 (INF2RIF2, SIM ~1695,
t=263.2): "Terrain too steep, slope: 408.844% max: 78.5398%". Two Strykers were sampled 3.6 m and 4.4 m from that point
(INF1HQ1 t=151.3, INF3HQ1 t=153.3) and went on. The three member types carry the same max-slope, 0.7853981633974 (Mech CO
(USA, M2).entity:438, Stryker  HHT (USA).entity:408, Stryker Cavalry SQDN (USA).entity:492 [V, read-only]); max-speed
differs (11.1 vs 26.9 m/s, :439 / :409 / :493) and so does the movement system (mech- vs motorized-aggregated-
movement.sysdef, :96 / :74 / :157). No command reached 48 IBCT after its script (L29311). The container's fix - the mean of
its members - made its last 50 m of progress by t=257.1 (SIM ~1655) and rested 770.9 m from the destination from t=263.2;
the watchdog reported 360 SIM s after that last progress (L170139, SIM ~2017, max 39.6 m).
Implication: the container mechanism, the publication gate, the time rules and the watchdog did what they were built to
do; the arrival was lost to the ground, not to the interface. On the aggregate model set an OSM building on a leg can stop
Mech CO units on the vendor's slope check - FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27 sec 8's "buildings never trap it"
does not hold for them - and the aggregate pre-flight flags a leg for OSM water only, with the slope ratio OFF (L578), so
nothing reported this leg before dispatch. The same hamlet stopped the entity-level literal executor in -2, 18.6 m away.
How G1-3 treats T14's line (a pre-flight rule for buildings on aggregate legs, a different line, or the stop accepted) is
the owner's call.

N2 - IDENTITY BY UUID, LIVE [V, P3]. Measurement: in both runs VR-Forces created all 36 init containers and all 23
members under the uuid the interface requested - the containers under their C2SIM uuids, the members under the v5 uuids
derived offline (expected_identity.txt) - and an independent federate saw them under those uuids. Implication: C1d's
live-owed claim is answered for AGGREGATES (RL-20260928-02); an ENTITY (platform) create, a synthesized sub-unit and a
template re-create stay unproven live (sec 7).

N3 - THE FIRST LOAD, AGAIN [V, P23]. Measurement: with the sim cache warm and unchanged (G1's pre-warm made its layers), the
pre-warm's init terrain query still got no reply within 10 s and its 36 containers were created at the FALLBACK altitudes;
the scored run, 10 minutes later, got its reply. Implication: G1's N4 reading - the cold sim cache - does not explain the
first launch's fallback; a pre-warm is still needed before a scored run (UNEXPLAINED item 2).

THE 12-MINUTE RUN [V]: launch 14:27:31Z to the ORDER on the bus 14:30:38.923Z (3 min 8 s: LaunchVrf 55.4 s, the 45 s
settle, the oracle pre-check, the observers' 20 s pre-roll, PushInit, the app start 16.3 s before the push); the window,
5 min 55 s; teardown to 14:39:09Z, 2 min 35 s (StopIface, the app's resign, a 22 s trail, the observers, StopVrf's forced
exit 6). The window closed on -StopWhenComplete once every task had a terminal report: T01 and T13 TASKCMPLT at +38.7 s
(C199 / C200, 14:31:17.59Z), T10 TASKCMPLT at +113.5 s (C460, 14:32:32.449Z), T02 TASKABRT at +148.8 s (C605,
14:33:07.759Z) and - the last - T14 TASKABRT at +264.9 s (C1002, 14:35:03.830Z); the runner saw them at +41 / +116 / +152 /
+269 s (R265-R275), had post-completion position evidence for all three taskees at +291 s (R276), held its 60 s settle
(63.9 s) and closed at +355 s (R280). It was short because every end time, delay and the stall window run on the
SIMULATION clock, which ran 6.4-9.0 times real time (P22): T14's ~1,607 SIM s from dispatch to its stall verdict took 226
wall s. The registration expected the close about a minute after the later of T02's stall and T14's arrival; with T14
stalled instead, it came 90 s after T14's TASKABRT.

UNEXPLAINED OR ASSUMED:
1. Why the terrain reads a 408.8% slope inside that building, and why five Strykers with the same max-slope passed within
   a few metres of the point where all eleven Mech COs were refused [A: the vendor's slope sampling and how the aggregate
   terrain represents a building are not read; the speed or the movement system may matter - untested].
2. Why the pre-warm's first terrain query timed out with the sim cache warm (N3) [A: the back end's first terrain load
   after its start, or the OS file cache - neither measured].
3. T02's one-member container trailed its member by 432.3 m before catching up in ~2 s (O4; G1 150.2 m) [A: the vendor's
   aggregate publication or dead-reckoning rate].
4. The holds served 330 s of 300 (G1 300; the pre-warm 304): the end-time check runs every 1.0 wall s
   (VrfC2SimService.cs:229, TimedCheckSeconds), yet the TIMED lines came 30 SIM s after the end time - ~3.4 wall s at
   the 8.9x of that minute [A: the tick or the sim-clock reading lagged; not measured]. Reports only; no row depends on
   it.
5. The relayed vendor console stamps lead the app's own SIM clock reading by 0-21 SIM s (L74355: app 889.0, console
   910.5-911.3), so the console-bracketed SIM figures above are +/- 21 SIM s.

ADVERSARIAL REVIEW (N1 is a cause claim). The strongest competing hypothesis: the Mech COs were held by a terrain FEATURE
(the mobility table's speed-factor 0, the way the lake, the river and the in-water unpack target held three others) and
the slope line is incidental. Falsifier checked: the vendor's feature line, "Movement constrained by features", printed
exactly three times - INF1RIF2 (lake), 28ID's member (river), INF3HQ1 (lake) - and never for a Mech CO; no OSM water or
waterway lies within 200 m of the stop; each Mech CO printed exactly one "Terrain too steep" line, in the order and within
the SIM seconds of its own stop. Second hypothesis, congestion: footprint overlap lowers only maximum speed (UG52 27.1.4),
and the first Mech CO to stop (INF1RIF1) had no Mech CO ahead of it. Third, the interface stopped them: no command reached
48 IBCT after L29311, and the watchdog is report-only and fired after the stops. Verified: the stop point, its building,
the eleven slope lines, their order and timing, the equal max-slope in the three entity files. Assumed: why the slope is
408.8% there and why the Strykers passed (UNEXPLAINED 1). No symptom is left without a named reading.

RECORDS: this Result and the STATUS line; PLAN_MOVEMENT_2026-09-27 rows C1, C1d and G1-2 (sec 1), G1-2 (sec 2), sec 3's G1
bullet and sec 5; RUNBOOK sec 11i (LIVE-OWED -> SEEN for aggregates); DESIGN_AGGREGATE_CONTAINERS' C1d status (the owed
sentence); Appendix B - the runner's blocks 5233-5243 and 5244-5254 carried from the main checkout's working tree with two
RESULT lines, marker 5255. Nothing was patched or re-run (STOP RULES): the next aggregate run is IRONSTORM_AGG_G1-<date>-3,
a new registration with new numbers from 5255.
