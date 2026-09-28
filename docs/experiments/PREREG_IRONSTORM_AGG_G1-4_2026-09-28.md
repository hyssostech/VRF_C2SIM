# PREREG - IRON STORM ON THE AGGREGATE PROFILE, RUN G1-4: G1-3 re-run on the M3b build - member names within 16 characters so the vendor script's route reference is carried whole, a vacuous planned vertex is EXECUTOR REFUSED, a crashed back end voids the window (one unscored pre-warm + one scored run)

STATUS: REGISTERED 2026-09-28, LAUNCH PENDING - the seat's go-live under RL-20260928-01, after Fable's cold review of this
registration's cause claims (RL-20260928-04; HANDOFF_SEAT_2026-09-28 sec 3 item 3). Registered by lane G1-4 (session
b25cd950; the registration commit's own time is the stamp) on branch prereg/g1-4 from main 9c15ebe, BEFORE any order push,
holder action or launch. PREPARATION ONLY: no C2SIM push, no holder start, no VR-Forces or runner launch, no appNumber
claimed, no edit of docs/OPUS_EXECUTION_PLAN.md, no C:\MAK write, no build (the deployed build is the M3b deploy lane's).
Marks: [V] = checked while writing this file (read-only hashes, source reads, the scorer on real run directories); [A] =
taken from the record or another lane's output, not re-checked here. This file is G1-3's registration
(PREREG_IRONSTORM_AGG_G1-3_2026-09-28.md, "G1-3" below) with ONE intended variable - M3b as deployed - and it says only
what differs: every section of G1-3 not restated here applies unchanged, with its src line numbers moved by the map of
sec 2 [V: `git diff -U0 fde3ff2 9c15ebe` hunk headers].

## Registration

PREREG ID: IRONSTORM_AGG_G1-2026-09-28-4
DATE (UTC): 2026-09-28, about 21:45Z, before any launch (the registration commit's timestamp is authoritative)
BINARY / COMMIT: the DEPLOYED main-checkout build 1.0.0+git.b0bad53.Release-5.2 (not DIRTY), rebuilt IN PLACE by the M3b
deploy lane from main b0bad53 at 21:19:57-21:21:07Z - managed code only, the ten managed consumers; no native change, so the
bridge pin 03226dd0 stands (RUNBOOK sec 9, "M3b DEPLOY 2026-09-28 21:20Z", :2415 [V]). b0bad53 = the M3b merge 739cce2 +
three docs-only HANDOFF_SEAT commits [A: the deploy line]; `git diff --stat b0bad53 9c15ebe -- src scripts tools data` is
EMPTY [V]. Read-only hashes at 2026-09-28T21:44Z [V]: VrfC2SimApp.exe 012ce272096d42614853255ea5832f93345e1a628027bd1c41d7b299c9fd1811,
VrfC2SimApp.dll 04fac5642f50e3023efa3d470e87f11f71dcba0303ed2db1fcac8486bd542dc0 (= the deploy line), ProductVersion
1.0.0+git.b0bad53.Release-5.2, exe written 21:19:57Z; deployed appsettings.json
4d737da54204b6395f60b784af230b675070f3d842f6a7f90e4855866672c79a = src/VrfC2SimApp/appsettings.json at 9c15ebe; appsettings.Demo.json
1239108016acf23923122ba4b233120cd7c09baecde632321bd2a2be6fb8f6ca; VrfBridge.dll in the app tree 03226dd0e48dae0869ce2fa5c8f3ac521603f2f9d5206ced04c14ad228dd14c5
(all eleven trees [A: the deploy line]); RtiProbe.exe 9f960c39... / RtiProbe.dll 517510d8... (lane R4's 18:16Z tree, NOT rebuilt
by the M3b deploy - held by holder 56380; its code unchanged [A: the deploy line]). Deployed settings: Vrf:AggregateMovePlanner
Auto, RoadProximityMeters 500, AllowLiteralMove false, PreflightOffline false, VertexArrivalRadiusMeters 100 [V]. Fixture
IronStorm_Centre_52_Aggregate.scnx 804e2c393dcf5f4fe4e1c58d7423a343c4e43250cd719d5474e035750c76a0e3 (read-only hash; G1-3's),
order 7a986137..., init 2000e856..., type map cc41f833..., composition 9684e945... [V; all G1-3's; `git diff --stat fde3ff2
9c15ebe -- data config tools` EMPTY]. The road layer (41 tiles, 213bae20...) and the cache (520 files, 27c2117e...) [A: the
deploy line, "the deployed preflight-cache untouched (520 files ...)"; re-read at step A]. Offline suites on the deployed
exe [A: the deploy line]: 28 switches 0 FAIL - rulings 615, planned-move 81, populate 199 PASS / 0 FAIL / 4 SKIP;
RunnerTurnaround 779/0/1. src at b0bad53 = G1-3's fde3ff2 + M3b (fix/planned-move-destination 5ae2193, merge 739cce2):
`git diff --stat fde3ff2 9c15ebe -- src scripts` [V] = AggregateMovePlanner.cs, ContainerComposition.cs, ContainerSelfTest.cs,
PlannedMoveSelfTest.cs, ShippedProfileSelfTest.cs, VertexChain.cs, VrfC2SimService.cs, VrfNames.cs, appsettings.json (one
comment); RunC2SimScenario.ps1, RunnerLib.ps1, RunnerWatchdog.ps1, StopVrf52.ps1. NOT built by this lane.
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: G1-3's list, unchanged (its sec VENDOR CITATION: UG52 72.2.1 p1419, Table 68 p1470, 35.5.11 p735, 30.5
p587; navigate-to-location.xml / .lua; roleNode.lua; featureconfig.txt; osm.roads.model.xml). For THE ONE VARIABLE:
- vrfutil/uuid.h :247-249 (C:\MAK\vrforces5.2d\include, read-only): "The UUID has been changed to be a memory blob of fixed
  size. The blob's format is the first char is the type, and the rest is the data" / `char myData[36];` [V] - 35 data bytes.
- vrftasks/moveAlongTasks.h :79-82 (the move-along carries its route as a DtUUID), vrfmodel/aggregatedMoveAlongController.h
  :70-77 (the controller looks the route up by it), navigate-to-location.lua :223 (the route is named "<taskee> Path part
  <n>"), :31 (MAX_POINTS_PER_ROUTE 100), :252-259 (the script ends SUCCESS once its move-along subtask stops, whatever that
  subtask's result) [A: RUNBOOK sec 11o and VrfNames.cs :62-88; --populate-selftest p16 (a) re-reads the lua and the three
  headers on the deployed exe, ContainerSelfTest.cs :1680-1696, 199 PASS per the deploy line].
- RoadToKaunasPhaseTwo.oob :103563-103565 (the vendor's own script route uuid "JAM-137 Path part 1_8"), :72127 (its
  move-along references "VRF_UUID:JAM-137 Path part 1_8"), :72076-72130 (planned with buffer 0) [A: RUNBOOK 11o].
- UG52: SILENT on the route-reference length and on "Path part" - 0 hits for "Path part", "route does not exist" and
  "navigate-to-location" in docs/vendor/mak-5.2/txt/*.txt [V]; "A graphical object's name can be up to 255 characters long"
  (VR-Forces_5.2_Users_Guide.txt :32795) [V] - the ROUTE keeps its whole name; the cut is the move-along's copy of it.

OWN-RECORD CITATION:
- Rulings: G1-3's list, unchanged (RL-20260927-01..-06, RL-20260928-01..-03, RL-20260921-09, RL-20260913-03,
  RL-20260914-01, RL-20260925-01, RL-20260926-01, RL-20260920-01), and RL-20260928-04 (Opus runs the seat; Fable reviews cause
  claims cold). RL-20260928-03's operative text names "buffer 10 m" (docs/RULINGS.md :98, :103) [V].
- docs/PLAN_MOVEMENT_2026-09-27.md CLOSED list :4-5 (identity is the UUID; names are display only) and :12-13 ("G1-3's
  planned move: the DESTINATION BOUND ...; the executor refused the script's route, whose name-built reference a move-along
  cuts at 35 - do not reopen the binding") [V]; rows M3b (:79), G1-3 (:80), step table "M3b -> G1-4" (:95) [V].
- docs/HANDOFF_SEAT_2026-09-28.md sec 2 (the M3b merge and deploy, holder 56380, the numbers) and sec 3 item 3 [V].
- G1-3 and its Result (N1, N2, the reading (i)-(iii), UNEXPLAINED, NEXT) [V, read in full].
- RUNBOOK sec 9 "M3b 2026-09-28" (:2414) and "M3b DEPLOY" (:2415); sec 11o (:4182-4250); sec 0.5.9's M3b paragraph (exit 8,
  :656-665); the names paragraph (:3965-3970) [V].
- PREREG_ROUTE_NAME_LENGTH_2026-09-02: route names <= 34 marched, >= 36 froze, the task's copy cut at 35, 35 never bisected
  [A: cited by VrfNames.cs :81-86 and RUNBOOK 11o].
- THE CONTROL: IRONSTORM_AGG_G1-2026-09-28-3 (G1-3; pre-warm 20260928T190047Z_run, STOPPED at its W gate).

## 0. Purpose, in plain words

G1-4 re-runs G1-3 - the ruled cut-A order on the AGGREGATE model set, 36 empty containers at init, the three performers
populated in place at order receipt (28ID 1 member, 1-112 IN 5, 48 IBCT 17), each container's route driven per STP vertex by
the vendor's navigate-to-location per member (Auto, RL-20260928-03) - with ONE change: M3b as deployed. In G1-3 the planner
worked and the executor did not: every T10 member's move-along asked for route "<member> Pathr" and the aggregated move-along
controller answered "route does not exist" (5 of 5), the script still reported SUCCESS, the interface advanced past a
vertex it had not left, and the back end crashed 2.9 s after the first planned dispatch (G1-3 Result N1, N2). M3b (1) names
every container member within 16 characters (`1-112_IN.RIF2`, not `1-112_IN/28ID__FRIENDLY_I.RIF2`), so the reference the
script builds - "<member> Path part <n>_<counter>" - fits the 34 characters a move-along has carried intact; (2) turns an
intermediate planned vertex that "succeeds" without moving into EXECUTOR REFUSED -> the vertex FAILS -> TASKABRT; (3) makes a
crashed back end VOID the window (runner line, manifest, exit 3; StopVrf52 exit 8). Buffer 10 is NOT changed: it is the next
hypothesis if G1-4 still fails (HANDOFF_SEAT sec 2), and it is in RL-20260928-03's operative text.

THE RUN'S HIGH PREDICTIONS (sec 4, rows H1-H6): 0 "route does not exist"; every tasked member moves at every vertex it is
sent to; T14 and T10 ARRIVE (in the pre-warm: T10's vertex 1 reached and CLOSED - W-DONE); 0 EXECUTOR REFUSED; the
destination echo exact, as in G1-3; the member names exactly the M3b table with the uuids unchanged. THE FALSIFIER OF THE
WINDOW: a crash of the back end (a crash record for its pid, the "Error vrfSimHLA1516e.exe" window, the trace's backends 1 ->
0, the sim clock lost) makes the window VOID - neither a pass nor a fail of M3b.

## 1. Decisions taken from the record

(a)-(t) - G1-3's, unchanged: the order and its five tasks, the settings (nothing passed for the planner or for M3b), the
pre-warm as the W gate, population at order receipt, order validation, the form, M2 report + fallback, what is issued
(navigate-to-location per member, obstacleQuery MAK_OBSTACLE, buffer 10, pathQuery MAK_ROAD on every cut-A leg, query ""),
the ring and slots, timings, container names and proxy tags, the TO twins, the tile cache, the control, identity, whose
position the judges read, the offline AUTO road table (every leg NEAR -> MAK_ROAD; ROAD LAYER 14 / 10 / 16), the completion
mapping, the destination, T02 and its river. Their src line numbers move by sec 2's map. Differences:
- (b) M3b adds NO setting: the EXECUTOR REFUSED bar is Vrf:VertexArrivalRadiusMeters = 100, already shipped; M3b changed only
  its comment [V: `git diff fde3ff2 9c15ebe -- src/VrfC2SimApp/appsettings.json`]. The command line stays G1-3's byte for byte.
- (h) WHAT IS ISSUED is G1-3's: AggregateMovePlanner.cs changed a comment block and added ExecutorRefusedReason only [V: the
  diff's two hunks, :159-166 and :259-281]; buffer=10 stays (RL-20260928-03).
- (i) THE UUIDS AND SLOTS are G1-3's: a member's uuid derives from its container's uuid and its SUFFIX, never its name
  (IdentityUuid.Derive; ContainerSelfTest p16 (e), :1734-1738) [V: source]; only the display NAME changes (sec 1(u)).
- (k) CONTAINER names unchanged (the 30-character cut, C1c); MEMBER names per (u).
- (n) THE CONTROL is G1-3: the same order, fixture, init, composition, settings, sequence, harness geometry; the build
  differs by M3b only (sec Registration).

(u) THE ONE VARIABLE, PART 1 - THE MEMBER NAMES AND THE ROUTE REFERENCES. VrfNames.MemberName (:152-153) = ChildName
(:177-185) on the container's Designator (:139-150: the name before its first "__", then before that part's last "/") within
PlannedMemberNameChars = 34 - 11 (" Path part ") - 2 (part digits) - 5 ("_" + a 4-digit counter) = 16 (:111-112); 34 =
UuidReferenceChars (:91), the longest reference a move-along has carried intact; 35 = UuidReferenceCutChars (:95), the blob's
data bytes. ContainerComposition.cs :705-735 routes every member name through it (PopulatePlanner.MaxNameChars). COMPUTED
[V: scratch laneG1-4\route_ref_lengths.py, a transcription of those lines, whose controls reproduce p16 (d)'s five 1-112 IN
names (:1726) and G1-3's live C1c names (vrfc2simapp.log L632, L640), and the scorer's --expected]:

| container (designator) | member names (M3b) | name length | "<member> Path part 1_8" (the vendor save's counter) | counter 1 / 2 / 3 / 4 digits | worst case: part 99 + 4-digit counter |
|---|---|---|---|---|---|
| 28ID__FRIENDLY_INFANTRY_DIVISION (28ID) | 28ID.HQ1 | 8 | 22 | 22 / 23 / 24 / 25 | 26 |
| 1-112_IN/... (1-112_IN) | 1-112_IN.HQ1 | 12 | 26 | 26 / 27 / 28 / 29 | 30 |
| 1-112_IN/... | 1-112_IN.RIF1, .RIF2, .RIF3, .WPN1 | 13 | 27 | 27 / 28 / 29 / 30 | 31 |
| 48_IBCT/... (48_IBCT) | 48_IBCT.HQ1, 48_IBCT.CAV1 | 11, 12 | 25, 26 | 25-28, 26-29 | 29, 30 |
| 48_IBCT/... | 48_IBCT.INF1HQ1, .INF2HQ1, .INF3HQ1 | 15 | 29 | 29 / 30 / 31 / 32 | 33 |
| 48_IBCT/... | 48_IBCT.INF<1-3>RIF<1-3>, 48_IBCT.INF<1-3>WPN1 (12 Mech COs) | 16 | 30 | 30 / 31 / 32 / 33 | 34 |

Every reference fits the 34 intact characters: the longest in the run's likely range (part 1, a counter of up to 4 digits)
is 33, the worst case 34 = the measured-safe maximum; none reaches 35 (the blob). THE CONTROL LINE (G1-3): the C1c name
`1-112_IN/28ID__FRIENDLY_I.RIF2` (30) gave "1-112_IN/28ID__FRIENDLY_I.RIF2 Path part 1" (42, + the counter), carried as its
first 35, "1-112_IN/28ID__FRIENDLY_I.RIF2 Path" - what the back end printed as "...Pathr" (35 + one byte) [V: the script and
G1-3 L9809]. No designator is cut on cut A (48_IBCT + ".INF1RIF1" = 16 exactly). NOT known: the counter's range (the vendor
documents it nowhere; its saves show 1..73) - a counter of 5 digits on a 16-character member at part 1 is 34, of 6 digits 35
[A: the record, VrfNames.cs :73, :104-106].

(v) PART 2 - EXECUTOR REFUSED (VertexChain.cs IsExecutorRefusal :121-123: a success farther than the bar from the vertex AND
the unit moved less than the bar since its last fix; applied only to a PLANNED chain's INTERMEDIATE vertex, :402-412). The
service logs at WARN (VrfC2SimService.cs :3711-3713) `PLANNED MOVE <c> vertex <k> of <N>: navigate-to-location issued
(useRoads true) -> FAILED - EXECUTOR REFUSED - the unit is <d> m from it and moved <m> m since dispatch|vertex <k-1> (VACUOUS
by the vertex bar - R11); members <s> succeeded / <f> failed of <M>. <reason>; the task takes the failure path (TASKABRT,
follow-ons abandoned)`, the reason (AggregateMovePlanner.cs :267-280) `PLANNED MOVE <c> vertex <k> of <N>: navigate-to-location
EXECUTOR REFUSED - moved <m> m since ... and is <d> m from the vertex (the vacuous bar is 100 m), although <s> of <M>
member(s) reported success: ...` carried by one TASKABRT (SynthesizeUnitCompletion(..., false), :3722); the stall state is
cleared (:3715). The LAST vertex is unchanged (D-6 withholds a vacuous one) - so T02 (one vertex, the last) can never read
EXECUTOR REFUSED; a refused T02 would show as D-6 WITHHELD and the watchdog [V: source].

(w) PART 3 - CRASH-VOID. The runner checks the back end every 10 s of the window, read-only (RunC2SimScenario.ps1 :5575-5587,
Test-LiveBackendCrash :1946-1988): crash records `<prefix>-<pid>.callstack.log` / `.dmp` in C:\MAK\logs by NAME AND MTIME
(never opened; the vendor .log never matched), the window title "Error vrfSim...", the process gone, WatchVrf's backends= 1 ->
0; on the first signal it prints once `BACK END CRASHED at <t> - the window is VOID (<evidence>)` (RunnerLib.ps1 :2507-2511),
writes the manifest's backendCrash block (Register-BackendCrash :1990-2008), RUNS THE WINDOW OUT (it is not cut short) and
ends with exit 3 instead of 0. StopVrf52 reads the same signals before the close, prints `CRASHED BEFORE THE CLOSE: ...`
(:558) and exits 8, "down, but NOT graceful" (:591-593); the runner reads 8 as down and voids the window unless the crash
is dated after the window closed (then WARN, the window stands, :5969-5986); the watchdog reads 8 as down (RunnerWatchdog.ps1
:380-382) [V: source]. Offline-proven only (RunnerTurnaround section 14 [A]); G1-4 is its first live use.

(x) THE SCORER: scratch laneG1-4\g1_4_score.py (sec 4 MEASURES). Its CONTROL on G1-3's own run is sec 4's fail-first.

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: none (G1-3's); the order is pushed right after the runner's stage-7 oracle gate.

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: G1-3's, unchanged - the watchdog on the SIMULATION clock, window 360 SIM s, 50 m, on each
container's one position; T01 / T13 300 (no destination, never watched); T02 / T14 300 and T10 450 (destination tasks: an
unarrived one goes OVERDUE and stays watched). Under M3b an EXECUTOR REFUSED vertex ends its task at once with TASKABRT and
clears its stall state (:3715), so the watchdog does not also report it.

DEVIATION FROM RECORD: the template's "PRE-ORDER GATE: --pre-order-gate nav-area" ("gate PushOrder on the first "New Primary nav area" row; warm the area first") is not used - the aggregate model set loads no nav data (G1's, G1-2's and G1-3's, unchanged).

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3) (G1-3's, unchanged).

DEVIATION FROM RECORD: the successor-gate floor is 600 s - appsettings.json's shipped value - not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:357) (G1-3's, unchanged).

DEVIATION FROM RECORD: the window closes EARLY under -StopWhenComplete, where E1 registered "--no-stop-when-complete --run-secs 2700"; the 2700 s cap is kept (G1-3's, unchanged).

DEVIATION FROM RECORD: the pre-warm is a whole runner launch that also pushes the order and runs 120 s, where DEMO_RUNBOOK sec 0.4 says "let it reach the initialization, then stop it"; there is no init-only runner mode (G1-3's, unchanged; it is the W gate).

DEVIATION FROM RECORD: the design's G1 outline ("Order: cut A reduced to T13 -> T14 on T14's ORIGINAL line") is replaced by the ruled cut-A order at the seat's direction (G1-3's, unchanged).

EFFECT OF -StopWhenComplete ON THE WINDOW: G1-3's (Test-EarlyExit, RunnerLib.ps1). NEW: a crash does not close the window
early; the runner runs it out and voids it (sec 1(w)).

## 2. What the code emits - log-line shapes (src at 9c15ebe = the deployed build's b0bad53 source)

G1-3's sec 2 applies shape for shape. ITS LINE NUMBERS MOVE by M3b's hunks [V: `git diff -U0 fde3ff2 9c15ebe`, spot-checked
by reading L-DISP :6397, L-ROADLAYER :8176, L-ARRIVE :8437, the relay :10055, L-SENT :10310, L-PLANNED :3623, L-MSTEP :3759,
L-MLAST :3763, L-VDONE :3683, L-VLAST :3688, L-VFAIL :3697 at their new numbers]: VrfC2SimService.cs - unchanged below
:3205; +2 for :3207-3242; +3 for :3244-3250; +4 for :3252-3661; +6 for :3663-3695; +28 above :3695. AggregateMovePlanner.cs -
+8 for :159-250, +31 above :250. VertexChain.cs - +13 for :112-146, +15 for :147-210, +21 for :211-380, +32 above :380.
ContainerComposition.cs - +5 around :705-735, +11 above :735.

CHANGED by M3b (G1-3 predicted their old form):
- L-POP (:3236-3247): the member list reads `Members: [<short> = <container>.<suffix> (<template>), ...]` (:3244-3246), e.g.
  `1-112_IN.RIF2 = 1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE.RIF2 (Mech CO (USA, M2))`.
- L-ID-POP (:3248-3256): each item `<short> = <uuid> (<container uuid>/<suffix>; <container>.<suffix>)` (:3254).
- L-MEMBER (:7196-7198) UNCHANGED in text: `member '<short>' (<n> chars) came back as EXACTLY that name (VRF_UUID:<uuid>) -
  at most 30 characters, it fits the aggregate marking (C1c); bound by its uuid (C1d).` - n is now 8-16.
- L-ID-CREATED / L-BIND / L-CONSOLE-MEMBERS / every PLANNED MOVE member line / the console relay: the SHORT names.
NEW (M3b):
- L-REFUSED (WARN, :3711-3713) and its TASKABRT reason (AggregateMovePlanner.cs :267-280) - sec 1(v); predicted ZERO.
- THE RUNNER (RunScenario log + manifest): `BACK END CRASHED at <t> - the window is VOID (<evidence>)` and the manifest block
  backendCrash {void, atUtc, evidence, line}; exit 3. StopVrf52: `CRASHED BEFORE THE CLOSE: <name> pid=<pid> - <evidence>`
  and exit 8; RunnerWatchdog `StopVrf exit 8: ... NOT graceful; the run's window is VOID` - all predicted ZERO (sec 4 V).
THE VENDOR'S OWN LINES the scorer now greps (relayed from a MEMBER's console; G1-3's text) [V: G1-3 vrfc2simapp.log]:
- level 3 `...Task 0 name and parameters: navigate-to-location: destination={<x>, <y>, <z>}; obstacleQuery=MAK_OBSTACLE;
  pathQuery=MAK_ROAD; buffer=10; displayRoute=False; query=""` (L9681) - THE ECHO (ECEF metres).
- level 3 `...Subtask 1 name and parameters: Move-Along Route: "<ref>"` (L9809) - the reference the move-along carries.
- level 1 `Warning:  DtAggregatedMoveAlongController::setupRoute -- %1 route does not exist. | <ref>` and `%1 route does not
  exist | <ref>` (L9811, L9813) - THE REFUSAL; predicted ZERO.

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run)

G1-3's sequence and command lines, with this lane's scripts (scratch <L4> = H:\claude\F--Repos-C2SIM-c2simVRFinterfacev2-36\
b25cd950-e1a5-4e5b-97e2-4b1267e629d2\scratchpad\laneG1-4, NOT RUN): golive_checks_g1_4.ps1, c3_push_g1_4.sh,
g1_4_runner.sh (prewarm-dryrun | prewarm | scored-dryrun | scored - G1-3's command line byte for byte, the log names aside),
claim_holder_g1_4.ps1 (branch (b') only), g1_4_score.py, route_ref_lengths.py; lane G1's simcache_listing.ps1 and lane E2's
cache_manifest.ps1 stay in G1-3's scratch u3. Python = /c/Users/PauloBarthelmess/AppData/Local/Programs/Python/Python312/
python.exe (the bare "python" is the Store alias [V]). THE SEAT RUNS C3, D', W, E AND F under RL-20260928-01.

TWO HOLDER BRANCHES, chosen at the go-live by the clock [V: RtiProbe 56380 up at 21:44Z, started 18:59:46Z, so its 28800 s
hold ends about 02:59:46Z 2026-09-29]:
- (a) HOLDER 56380 (appNo 5255) STILL UP with at least 60 min of hold left before W AND before E (golive_checks -HolderPid
  56380 -MinHoldLeftMin 60): no holder step; the runner recognises it PERSISTENT (G1-3's W did).
- (b') 56380 HAS RESIGNED (or would resign inside E): step D' - golive_checks -Phase preholder (56380 gone, no RtiProbe),
  claim_holder_g1_4.ps1 -From 5270 on the main checkout's Appendix B (marker 5270 -> 5274), then
  `scripts/StartFederationHolder52.ps1 -AppNumbers 5270,5271,5272,5273 -SettleSecs 28800` (-WhatIf first), EXPECT
  "HOLDER JOINED: pid ... appNo 5270", exit 0; exit 1 or 2 = STOP, no blind relaunch (RUNBOOK 9c). The RtiProbe tree is R4's
  18:16Z build at the pin (not rebuilt; optional, code unchanged [A: the deploy line]).
W and E start only with at least 60 min of hold left (golive_checks -MinHoldLeftMin 60, G1-3's default). The record is SILENT
on a persistent holder that resigns INSIDE a window (the pre-warm's own Stage 2h holder is also joined): if it happens it is
RECORDED with the federation's state and the seat decides VOID or not - this lane does not decide it.

A0. PRECONDITIONS: the seat's go-live; Fable's cold review of sec 4's cause rows done; no lane building, running a suite or an
    agent for the quiet period (ask the STP session to hold builds first; idle MSBuild workers fail the check - let them time
    out, never kill, HANDOFF_SEAT sec 3 item 4); `git diff --stat b0bad53 main -- src scripts` still EMPTY - if it moved,
    NOTHING is rebuilt and the difference is recorded; deployed hashes as registered, else STOP before W.
A.  "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File <L4>/golive_checks_g1_4.ps1 -Phase prelaunch -HolderPid 56380
    -MarkerWant 5270   EXPECT 0 checks FAILED (branch (a)); branch (b'): -Phase preholder -MarkerWant 5270 first.
A2. THE BUILD, OFFLINE: the deployed exe's --populate-selftest (p16 names the five 1-112 IN members '1-112_IN.HQ1' ..
    '1-112_IN.WPN1' and every shipped row within 16) and --planned-move-selftest <exe dir>\preflight-cache (81 PASS; m9 NEAR on
    every cut-A leg; m10 EXECUTOR REFUSED) - from the deploy line [A] or re-run by the seat from PowerShell with the 5.2 PATH
    prefix (the deploy line's SHELL NOTE: Git Bash's PATH form failed every bridge-loading switch). A FAIL = STOP before W.
C.  C3 - `sh <L4>/c3_push_g1_4.sh` (G1-3's two lines verbatim, the private server 18080 / 61614, RL-20260928-01). EXPECT both
    exit 0, "QUERYINIT : 40 Units", one `ORDER (69670 chars)` echo carrying 5 tasks. Any failure = STOP.
W.  THE PRE-WARM (unscored) = THE W GATE: golive_checks -Phase prelaunch (0 FAILED); simcache_listing.ps1 -Out
    <L4>/simcache_before_prewarm.txt; `sh <L4>/g1_4_runner.sh prewarm-dryrun` (EXPECT the holder PERSISTENT, block 5270-5280
    under (a) / 5274-5284 under (b'), "marker would advance to: 5281" / "5285", G1-3's D2 lines, 120 s CAP); then `sh
    <L4>/g1_4_runner.sh prewarm` ONCE. GATE for E - ANY miss = STOP before E: StopVrf52 exit 0 or 6 (8 = the crash branch:
    VOID); the runner exit 0 and NO `BACK END CRASHED` line; the post-W inventory shows only the RTI trio, the persistent holder
    and the pre-warm's Stage 2h holder; the cache manifest unchanged; AND
        <python> <L4>/g1_4_score.py <prewarm run dir> --wgate > <L4>/W_gate_<run>.txt
    prints "W GATE: PASS" (exit 0): G1-3's population + identity limbs (branch A) with W1n and I6 on the M3b names; G1-3's M3
    limbs M1-M17 (M9 now labelled the BOOKKEEPING of the vendor's rule, not the move); H1, H2, H3 (sec 4); W-MOVE (G1-3's W-DEST,
    RELABELLED - the Result's own critique: it "failed with the destination reached"; the binding is H3's); W-DONE (HIGH here:
    T10's vertex 1 reached by all 5 members and CLOSED SUCCEEDED inside the window - NOT REACHED is a FAIL, no longer "the
    seat's call"); H5; the water falsifiers; V-CRASH (a crash = "VOID", STOP before E). Then simcache_listing.ps1 -Out
    <L4>/simcache_after_prewarm.txt.
E.  THE RUN: golive_checks -Phase prelaunch -MarkerWant 5281 (a) / 5285 (b'); `sh <L4>/g1_4_runner.sh scored-dryrun` (EXPECT
    block 5281-5291 / 5285-5295, marker -> 5292 / 5296, 2700 s CAP with -StopWhenComplete); then `sh <L4>/g1_4_runner.sh
    scored` ONCE, stdout to a file. The command, verbatim G1-3's (sec 3 E there), the log name aside:
        scripts/RunScenario.sh --scenario IronStorm_Centre_52_Aggregate --init data/IRONSTORM_CUTA_Initialization.xml
          --order data/IRONSTORM_CUTA_Order.xml --client-id "Not Set" --model-set auto --duration-scale 0.25
          --object-console 4 --member-console 4 --stop-when-complete --run-secs 2700 --env Vrf__StallDetection=true
          --env Vrf__StallClock=sim --env Vrf__TaskPredecessorTimeoutSeconds=600 --sample-threads --no-gui
          --log runs/launch52/RunScenario-ironstorm-agg-g1-4-<stamp>.log
    GATE on teardown: StopVrf52 exit 0 or 6 (P21); 8 = VOID (sec 4 V); 3, 5 or 7 = STOP.
F.  Post-run, in the FOREGROUND: the inventory; golive_checks -Phase prelaunch -MarkerWant 5292 / 5296; simcache listing;
        <python> <L4>/g1_4_score.py <scored run dir> > <L4>/score_<run>.txt
QUIET PERIOD: G1-3's (no Stop-Process / taskkill of any kind, no build, suite, subagent or second runner from W to F's
inventory). Never touched: rtiexec 47980, rtiForwarder 50740, rtiAssistant 30240, any RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES: G1-3's, per TASK UUID from the run's vrfc2simapp.log in line order, positions from watchvrf-trace.csv POS rows.
THE SCORER scratch laneG1-4\g1_4_score.py (g1_3_score.py + these changes, all [V]): --member-names m3b (default) | c1c (G1-3's
rule, for the control); the M3b L-POP / L-ID-POP forms parsed (the old form too); H1 (the relayed refusal), H2 (EXECUTOR
REFUSED lines, the TASKABRT reason, and the pre-M3b equivalent - an intermediate vertex COMPLETED VACUOUS with the unit moved
< 100 m, G1-3 L9979), H3 (the level-3 echo converted from ECEF to WGS84 and compared with the vertex the member's container
last issued: <= 0.5 m horizontally and |height| <= 0.5 m), H5 (per member per vertex, on the trace: displacement > 1.0 m from
its fix at the dispatch for vertex 1, from its first fix within 50 m of vertex k-1 for vertex k), R-REF (the printed
move-along reference against the whole "<member> Path part <n>[_<counter>]"), R-VEND (the vendor log's "route does not
exist" COUNT - never a line), V-CRASH (the run manifest's backendCrash; stopvrf.stdout.log's "Error vrfSim..." window and
"CRASHED BEFORE THE CLOSE"; the watchdog's "StopVrf exit 8"; crash records `vrfSim*-<pid>.callstack.log|.dmp` LISTED in
C:\MAK\logs by name and mtime between the back end's start and the window's close + 5 s; the trace's backends 1 -> 0; the
app's "(no back end reporting)" before shutdown); W-DEST relabelled W-MOVE; W-DONE's NOT REACHED a FAIL. Its --selftest: 50
checks PASS, 0 FAIL - G1-3's 41 (the uuid oracle, the chain checker, the CLEAN control, S7, the must-not-fire wet edge, 26
dirty controls) on synthetic runs in the M3b line forms, plus an ECEF oracle (G1-3's echo reads (54.029734, 23.305499, 0)),
the M3b name oracle, the clean M3b rows, and SIX NEW DIRTY controls each caught: route-not-exist (H1), executor-refused (H2),
vacuous-advance (H2), echo-off (H3, a member's echo 111 m off), no-move (H5, one T14 member parked), crash (V-CRASH VOID; the
vendor .log beside the callstack never matched) [V: scratch laneG1-4\g1_4_score_selftest.txt].
THE CONTROL, FAIL-FIRST ON REAL DATA [V]: `g1_4_score.py runs\20260928T190047Z_run --wgate --member-names c1c` (G1-3's own
pre-warm, read with its own naming rule) -> exit 1, "W GATE: FAIL + VOID (branch A; H1, H2, H5, M10, M12, V-CRASH(VOID),
W-DONE(NOT REACHED), W-MOVE)": H1 15 relayed "route does not exist" (first L9811); H2 the pre-M3b equivalent L9979 (vertex 1
"1451 m from it, moved 0 m"); H5 0.0 m for all five members; V-CRASH the "Error vrfSimHLA1516e.exe" window, the callstack
record (19:05:16Z) and .dmp (19:05:17Z) for pid 3344 (names and mtimes only), the trace's backends 1 -> 0 at t=194.9 s and
the app's L10245; and, as it must, H3 PASSES there (10 echoes exact, vertices 1 and 2 - the binding G1-3 saw) and every
identity / population limb passes (scratch control_g1_3_wgate_c1c.txt). With the default M3b names the same run fails more
(the names do not match: I2, I6, I8, W1n ...; control_g1_3_wgate_m3b.txt). A MUST-NOT-FIRE control on G1-2's clean pre-warm
(20260928T141734Z_run, the Literal build): V-CRASH "no signal", H1 0, H2 0 (control_g1_2_prewarm_wgate_c1c.txt).

THE NEW ROWS (G1-4's one variable). G1-3's P0-P23, P-FALS and O1-O12 apply as registered there, their line numbers per sec 2,
with the changes named after this table.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| H1 | THE EXECUTOR TAKES THE ROUTE: ZERO relayed "route does not exist" from any member of any task, in the pre-warm and in the scored run (scorer H1); the vendor sim log's count of the same text is 0 too (R-VEND, count only). | HIGH | Any = STOP. With R-REF showing the reference WHOLE (sec 4 COMPETITORS), the name reading of G1-3 (i) is REFUTED; with it cut, the budget of sec 1(u) is wrong. | |
| H2 | NO EXECUTOR REFUSED: ZERO L-REFUSED lines and ZERO "EXECUTOR REFUSED" TASKABRT reasons; ZERO intermediate vertices COMPLETED VACUOUS with the unit moved < 100 m (the pre-M3b form). | HIGH | Any = STOP (the executor refused again; the TASKABRT is M3b working as designed, the move is not). | |
| H3 | THE DESTINATION ECHO EXACT, AS IN G1-3: every member of T10 and T14 (and T02's one) prints the level-3 echo for every vertex its container issued, and each converts to that vertex at ellipsoid height 0 within 0.5 m (G1-3: exact to 0.000 m on 10 of 10). | HIGH | Any echo off or missing = STOP; the binding the CLOSED list settles is not reopened - the miss is recorded and the seat decides. | |
| H4 | THE NAMES AS sec 1(u): each L-POP lists the registered short names in slot order, each "<short> = <container>.<suffix>"; L-ID-POP names exactly the v5 uuids of G1-3 (unchanged: the derivation takes the suffix) beside the full names; EXACTLY 59 L-ID-CREATED (the 23 members under their G1-3 uuids with their short names as markings); 23 L-MEMBER "(8..16 chars) came back as EXACTLY that name ... bound by its uuid (C1d)"; ZERO NAME DISAMBIGUATED / NAME COLLISION (scorer W1n, I2, I3, I6, I8, I12, I13). | HIGH | Any limb = STOP (C1d's regression guard, RL-20260928-02, as G1-3's P3). | |
| H5 | EVERY TASKED MEMBER MOVES at every vertex it is sent to: displacement > 1.0 m on the trace (scorer H5; G1-3: 0.0 m for 5 of 5); and G1-3's P10 (each mover's members displaced > 50 m: 1 of 1, 5 of 5, 17 of 17). | HIGH | A member that does not move at a vertex = STOP. | |
| H6 | T14 AND T10 ARRIVE (G1-3's P12, unchanged): L-ARRIVE with 1/1 and the ONE POSITION suffix, then one TASKCMPLT each; in the PRE-WARM, T10's vertex 1 reached by all 5 members within 50 m and CLOSED "the LAST of 5 ... -> the vertex SUCCEEDED" inside the window (W-DONE). | HIGH | T14 or T10 not arriving = STOP; in the pre-warm, W-DONE NOT REACHED or reached-not-closed (S9) = STOP before E. | |
| R-REF | THE CARRIED REFERENCE: every relayed "Move-Along Route: \"<ref>\"" is the WHOLE "<member> Path part <n>" or "<member> Path part <n>_<counter>", <= 34 characters (G1-3: 5 of 5 "<30 chars> Pathr"). | MEDIUM | Recorded; read with H1 (sec COMPETITORS). | |
| V | THE WINDOW IS VALID - NO BACK-END CRASH: no crash record for the back end's pid dated before the window closed, no "Error vrfSimHLA1516e.exe" window, no `BACK END CRASHED` line or manifest backendCrash, StopVrf52 exit 0 or 6 (not 8), the trace's backends never 1 -> 0, the sim clock never lost before shutdown (scorer V-CRASH). IF a crash happens, the harness NAMES it (the runner line + exit 3, StopVrf52 exit 8) - M3b (3)'s first live test. | MEDIUM (no crash: G1 and G1-2 ran clean, G1-3 crashed once, n = 1, its stack unread); HIGH (a crash is named by the runner and StopVrf52) | A crash = the window is VOID: neither a pass nor a fail of M3b, no row is scored, the successor is G1-5 with new numbers. A crash the scorer sees and the runner did NOT name = STOP (M3b (3) failed live). | |

CHANGES TO G1-3's ROWS: P0 (ii) - the build is b0bad53's (sec Registration), (i) holder branch (a) or (b') (sec 3); P1 - the
L-CON-ON / L-M3-ON lines as G1-3's; P2 - the 36 init containers as G1-3's; P3 - the 23 members under their G1-3 uuids with the
M3b short names (H4); P3a - the member lists per H4; P7c - ZERO "Cound not create route" (as G1-3); P12 - = H6; P20 - a crash
limb is V (VOID); P21 - StopVrf52 exit 8 is V's branch, not a teardown STOP. Everything else as registered in G1-3.

THE CARRIED UNEXPLAINED ITEMS OF G1-3, each with its expectation here:
- VERTEX 2's "Cound not create route" (HQ1, RIF2): expected ZERO (P7c HIGH, scorer M12). The Result's own reading - "a second
  route asking for the same capped name" (its (i)) - predicts it goes with the name. If it recurs with H1 at 0 and R-REF whole,
  that reading of it is refuted and it is a separate mechanism.
- THE CRASH (pid 3344, 19:05:16Z, stack unread - reading it is the owner's decision, RL-20260921-02 item 4 precedent):
  expected NOT to recur (V, MEDIUM). If it recurs, the window is VOID; its timing against the first planned dispatch and the
  count-grep of its frames are RECORDED the way G1-3's were (names, sizes, mtimes; never the content).
- "Pathr": expected to be replaced by whole references (R-REF). Whole references would fit the record's reading (35 characters
  cut + one byte that follows the buffer, VrfNames.cs :80-82); the rule that printed the 36th byte stays unread (not claimed).
- O8, THE FALLBACK ALTITUDES (the pre-warm's init took 36 of 36 create altitudes from the FALLBACK in G1-2 and G1-3): RECORDED
  again (G1-3's P23, MEDIUM for the scored run's TERRAIN QUERY). Nothing in M3b touches it; it is not expected to change.
- WHY HQ1 AND RIF2 RETURNED FIRST AT VERTEX 2: RECORDED (the planning latency per member, G1-3's O12); not predicted.

COMPETITORS TO "the name cut caused G1-3's refusal" (the Result's (i); this row is for Fable's cold review):
- THE STRONGEST: a defect of the aggregate move-along path that does not depend on the name - the Result's (iii) (a same-tick
  race: the subtask starts in the tick the route is created) or something unseen. The Result WEAKENED (iii) (the vendor script
  always does this, and the vendor's JAM-137 save shows it working) but did not exclude it; its adversarial note says the
  save "differs from ours in the buffer (0 against 10) as well as in the name's length".
- BUFFER 10 (the vendor save used 0): the handoff's next hypothesis if short names still fail. Nothing in the record ties the
  buffer to the ROUTE LOOKUP; its one vendor comment concerns obstacle-area collection ("AreaCollector::addGeometry does not
  deal correctly with large obstacle features", navigate-to-location.lua :39-40 [A: the G1-3 Result]), which is the crash's
  candidate, not the refusal's.
- THE OBSERVATION THAT FALSIFIES THE NAME READING IN G1-4: R-REF prints the reference WHOLE ("1-112_IN.RIF2 Path part 1_<n>")
  AND the controller still answers "route does not exist" for it (H1 > 0). The reference then arrived intact and was still not
  found, which the name reading cannot produce. H1 = 0 with members moving is CONSISTENT with the name reading but, at n = 1,
  does not exclude an intermittent (iii); it is not claimed as proof of cause.

NAMED STOP-WITH-REPORT BRANCHES: G1-3's S1-S10, and S11 THE EXECUTOR REFUSED AGAIN - L-REFUSED + TASKABRT on T10 or T14 (H2
missed): recorded with the relayed lines, R-REF and R-VEND; STOP.
STOP RULES: G1-3's - a missed HIGH row is a STOP (record it, no patch, no re-run under this registration); P0 / P20 VOID; P-FALS
or P11 / P14's HIGH limb = STOP and ask; the executor never intervenes in a window. NEW: a crash (V) VOIDS the window whatever
else it shows. A VOID or STOPPED run is re-registered as IRONSTORM_AGG_G1-<date>-5 with new numbers. Buffer 10 is NOT changed
under this registration or its successor without a ruling: it is in RL-20260928-03's operative text.

ONE VARIABLE: M3b as deployed - the member-name budget, EXECUTOR REFUSED, the crash-void - on G1-3's order, fixture, init,
composition, settings, sequence and harness geometry. Differences named, not intended: the holder branch (a) reuses G1-3's
holder 56380 (G1-3 claimed and started it); branch (b') starts a new one on the same RtiProbe tree.

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5270 ***` (docs/OPUS_EXECUTION_PLAN.md :3610 in the main checkout's working tree
[V 21:44Z]). NOTHING is claimed by this registration. The runner's block layout M..M+10 (M back end, M+1 front end - BURNED
under --no-gui, M+2 WatchVrf pre-check, M+3 WatchVrf trace, M+4 VrfC2SimApp, M+5 RtiProbe 2c, M+6 CreateOne - BURNED unless the
oracle gate fails, M+7..M+10 Stage 2h holder attempts; marker -> M+11) is G1-3's: M3b's runner change adds no appNumber [V: 0
appNumber / block / marker lines in `git diff fde3ff2 9c15ebe -- scripts/RunC2SimScenario.ps1`]. From the marker M read at
the go-live (5270 unless another run moved it; every number shifts with it and the difference is recorded):
- BRANCH (a), holder 56380 (appNo 5255) reused: PRE-WARM 5270-5280 (marker -> 5281); SCORED 5281-5291 (marker -> 5292).
- BRANCH (b'), after 56380 resigned: the NEW HOLDER 5270-5273, HAND-CLAIMED by claim_holder_g1_4.ps1 -From 5270 BEFORE it joins
  (marker -> 5274); PRE-WARM 5274-5284 (marker -> 5285); SCORED 5285-5295 (marker -> 5296).
A launch that aborts burns its whole block. Never reuse a number. The claims are carried back to this branch with the Result.

## 6. Harvest (after the runs, read-only) and where results go

G1-3's sec 6, with g1_4_score.py (<run> --wgate for the pre-warm, then in full; `--expected` for the 59 identity lines, the
M3b names with their route references, the road and vertex lines). Vendor sim logs (runs\*\vendor) and C:\MAK\logs: COUNT
and NAME / MTIME only, never quoted, never attached. Results go to: the Result block below (measurement and implication in
separate sentences); PLAN_MOVEMENT_2026-09-27.md rows M3b and G1-4 and the step table; RUNBOOK sec 11o ("OFFLINE-PROVEN ONLY"
-> what was seen) and sec 0.5.9 (exit 8 seen live or not); HANDOFF_SEAT sec 2; Appendix B annotated from the manifests.
ASCII + CRLF.

## 7. What this run does NOT claim

- G1-3's list, unchanged (the map display; the authored variant; nested containers; the STP TO precedence; a patrol or a
  point move; combat; the entity-level profile; the full order; the NONE branch of Auto; Move (Group); the timing).
- THE CAUSE OF G1-3's REFUSAL is not proven by a pass: n = 1, and H1 = 0 does not exclude an intermittent defect (sec 4
  COMPETITORS); a pass says the M3b build moved these members on this order once.
- THE COUNTER'S RANGE: the "_<counter>" digits are budgeted at 4 and the vendor documents none; a run that prints a counter
  is RECORDED (R-REF), not generalised.
- BUFFER 10 is not tested against 0 here (the one variable is M3b); the crash's cause is not read (the owner's decision).
- THE LABEL (HANDOFF_SEAT sec 3 item 5): the map shows the 16-character member names; the full designation in the vendor's
  Label is a follow-up after this Result, not part of G1-4.

## 8. Where the record was silent or disagreed - the choices this lane made, for the seat to decide

1. THE CAP, 34 OR 35: HANDOFF_SEAT sec 2 says the reference "fits the 35-byte DtUUID payload"; the CLOSED list says a
   move-along "cuts at 35"; the code budgets 34 (VrfNames.UuidReferenceChars :91, "35 was never bisected"). This file
   measures against 34 - every cut-A reference fits either way (sec 1(u)).
2. W-DONE IN THE PRE-WARM made HIGH (NOT REACHED = STOP before E), where G1-3 left NOT REACHED to the seat - per this lane's
   brief (T10 ARRIVES, W-DONE reached). It assumes T10's vertex 1 is reachable inside the 120 s window [A: G1-2's pre-warm T10
   arrived on the straight line; under Auto the path is a road path with a planning pause].
3. H5's "moves" is > 1.0 m, not > 0 m: 1 m of fix jitter is allowed (G1-3's refused members read exactly 0.0 m).
4. BUFFER 10 AS "THE NEXT HYPOTHESIS" (HANDOFF_SEAT sec 2, RUNBOOK 11o) is inside RL-20260928-03's operative text ("buffer
   10 m"); testing 0 needs a ruling or a registered comparison the ruling allows - not decided here.
5. THE ORDER OF WORK: the G1-3 Result's NEXT list put a one-variable short-name probe (2) and the callstack decision (1) before
   a successor registration (5); HANDOFF_SEAT sec 3 (later) goes to G1-4 on M3b directly. This lane followed the handoff; the
   callstack decision (RL-20260921-02 item 4 precedent) is still open.
6. A PERSISTENT HOLDER RESIGNING INSIDE A WINDOW: silent in the record (sec 3); recorded, not decided.
7. A CRASH RECORD DATED IN TEARDOWN: the runner lets the window stand (WARN, :5980-5982); the scorer counts records up to the
   window's close + 5 s (its own slack, stated here).
8. T02 CANNOT READ "EXECUTOR REFUSED": its one vertex is the last, which M3b leaves to D-6 (sec 1(v)); a refused T02 shows as
   D-6 WITHHELD and the watchdog, and H2 cannot see it - H1 and H5 do.

## Result (written after the harvest, never from a live read)

(pending)
