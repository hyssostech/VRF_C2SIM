# PREREG - IRON STORM ON THE AGGREGATE PROFILE, RUN G1-4: G1-3 re-run on the M3b build - member names within 16 characters so the vendor script's route reference is carried whole, a vacuous planned vertex is EXECUTOR REFUSED, a crashed back end voids the window (one unscored pre-warm + one scored run)

STATUS: STOPPED AT THE W GATE 2026-09-28 - FAIL (H5, P-FALS(e)) BINDS AS SCORED (RL-20261004-01; the Result below). Was:
REGISTERED 2026-09-28, LAUNCH PENDING - the seat's go-live under RL-20260928-01, after Fable's cold review of this
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
  size. The blob's format is the first char is the type, and the rest is the data" / `char myData[36];` [V: the lines read].
  THE CUT AT 35 IS OBSERVED, WHERE IT HAPPENS IS NOT [Fable review]: G1-3's back end printed the reference cut at exactly 35
  characters (vrfc2simapp.log L9809; 20 of 20 mentions per the G1-3 Result) [V]; that the 36-byte blob is the cutter is [A] -
  uuid.h :251-253 keeps a string uuid that is not a boost uuid in an unbounded `DtString myUUIDString` [V: read], and
  PREREG_ROUTE_NAME_LENGTH_2026-09-02 :512-518 calls the blob its own INFERRED candidate [V: read]. The CLOSED line (the cut at
  35) stands; only its location is [A].
- vrftasks/moveAlongTasks.h :79-82 (the move-along carries its route as a DtUUID), vrfmodel/aggregatedMoveAlongController.h
  :70-77 (the controller looks the route up by it), navigate-to-location.lua :223 (the route is named "<taskee> Path part
  <n>"), :31 (MAX_POINTS_PER_ROUTE 100), :252-259 (the script ends SUCCESS once its move-along subtask stops, whatever that
  subtask's result) [A: RUNBOOK sec 11o and VrfNames.cs :62-88; --populate-selftest p16 (a) re-reads the lua and the three
  headers on the deployed exe, ContainerSelfTest.cs :1680-1696, 199 PASS per the deploy line].
- RoadToKaunasPhaseTwo.oob :103563-103565 (the route object: marking-text "JAM-137 Path part 1", uuid "VRF_UUID:JAM-137 Path
  part 1_8"), :72127 and :72196 (its move-along references "VRF_UUID:JAM-137 Path part 1_8"), :72034 and :72082 (buffer
  0.000000), :72084-72085 (the DtRwVector destination), :103561 (publish-flag 0) [V: docs/experiments/EXTRACT_RoadToKaunasPhaseTwo
  _oob_2026-09-28.txt - the cited ranges only, from the vendor's .scnx (sha256 22e06582...), whose .oob member equals the m3b
  lane's extract, sha256 891a5a2f...]. So the "<name> Path part <n>_<counter>" FORM is the vendor's own [V]; that a
  move-along carries that string through a 36-byte blob is [A] (above).
- UG52: SILENT on the route-reference length and on "Path part" - 0 hits for "Path part", "route does not exist" and
  "navigate-to-location" in docs/vendor/mak-5.2/txt/*.txt [V]; "A graphical object's name can be up to 255 characters long"
  (VR-Forces_5.2_Users_Guide.txt :32795) [V]; that the ROUTE keeps its whole name and only the move-along's copy is cut is
  [A]. R-REF printed WHOLE in G1-4 is the observation that would show the cut was the reference's; H1 = 0 alone does not.

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

THE RUN'S HIGH PREDICTIONS (sec 4, rows H1-H5): 0 "route does not exist"; every tasked member moves at every vertex it is
sent to; 0 EXECUTOR REFUSED; the destination echo exact, as in G1-3; the member names exactly the M3b table with the uuids
unchanged. MEDIUM (H6, after Fable's review): T14 and T10 ARRIVE in the SCORED run - never yet observed under Auto; the
pre-warm keeps G1-3's W-DONE rule (sec 3 W). THE FALSIFIER OF THE
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
UuidReferenceChars (:91), the longest reference a move-along has carried intact; 35 = UuidReferenceCutChars (:95), the length
the reference was OBSERVED cut to (that the blob's 35 data bytes do it is [A], VENDOR CITATION). ContainerComposition.cs :705-735 routes every member name through it (PopulatePlanner.MaxNameChars). COMPUTED
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
is 33, the worst case 34 = the measured-safe maximum; none reaches 35 (the observed cut); all fit under 34 and under 35. THE CONTROL LINE (G1-3): the C1c name
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

THE HOLDER - BRANCH (a), SEAT DECISION 2026-09-28 [V: RtiProbe 56380 up at 21:44Z, started 18:59:46Z, so its 28800 s hold
ends about 02:59:46Z 2026-09-29]: the go-live runs under branch (a) and STARTS E NO LATER THAN 01:55Z 2026-09-29 (SEAT
DECISION after Fable's review, replacing 02:15Z): the whole scored window - launch about 4 min + the 2700 s cap - then closes
before 56380's hold ends. IF E CANNOT START BY 01:55Z, THE GO-LIVE MOVES TO BRANCH (b') below.
- (a) HOLDER 56380 (appNo 5255) STILL UP with at least 60 min of hold left before W AND before E (golive_checks -HolderPid
  56380 -MinHoldLeftMin 60): no holder step; the runner recognises it PERSISTENT (G1-3's W did).
- (b') 56380 HAS RESIGNED (or would resign inside E): step D' - golive_checks -Phase preholder (56380 gone, no RtiProbe),
  claim_holder_g1_4.ps1 -From 5270 on the main checkout's Appendix B (marker 5270 -> 5274), then
  `scripts/StartFederationHolder52.ps1 -AppNumbers 5270,5271,5272,5273 -SettleSecs 28800` (-WhatIf first), EXPECT
  "HOLDER JOINED: pid ... appNo 5270", exit 0; exit 1 or 2 = STOP, no blind relaunch (RUNBOOK 9c). The RtiProbe tree is R4's
  18:16Z build at the pin (not rebuilt; optional, code unchanged [A: the deploy line]).
W and E start only with at least 60 min of hold left (golive_checks -MinHoldLeftMin 60, G1-3's default) and E by 01:55Z. A
persistent holder that resigns INSIDE a window is RECORDED with the federation's state; it does NOT void the window by itself
(SEAT DECISION 2026-09-28: the crash-void limb V is about the BACK END only).

A0. PRECONDITIONS: the seat's go-live; Fable's cold review of sec 4's cause rows done; no lane building, running a suite or an
    agent for the quiet period (ask the STP session to hold builds first; idle MSBuild workers fail the check - let them time
    out, never kill, HANDOFF_SEAT sec 3 item 4); `git diff --stat b0bad53 main -- src scripts` still EMPTY - if it moved,
    NOTHING is rebuilt and the difference is recorded; deployed hashes as registered, else STOP before W.
A.  "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File <L4>/golive_checks_g1_4.ps1 -Phase prelaunch -HolderPid 56380
    -MarkerWant 5270   EXPECT 0 checks FAILED (branch (a)); branch (b'): -Phase preholder -MarkerWant 5270 first. THEN THE
    FEDERATE LIST, read-only: `<python> <L4>/rti_federates.py` (the long-lived rtiexec log's join / resign lines, names and
    handles only; RUNBOOK :31, :1455, :2445 read joins from that log) - EXPECT exactly one joined federate, remoteControl
    56380 (branch (a)); any other = STOP before W and the seat decides (RUNBOOK sec 0's ghost-federate check).
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
    RELABELLED - the Result's own critique: it "failed with the destination reached"; the binding is H3's); W-DONE (G1-3's
    rule, SEAT DECISION 2026-09-28: a vertex all members reached but that never CLOSED - S9 - is a FAIL = STOP before E; NOT
    REACHED is RECORDED, the seat's call, and NOT a stop by itself PROVIDED W-MOVE passes - every dispatched T10 member displaced
    > 50 m (T10 ONLY, SEAT DECISION after Fable's review; T14's and T02's member displacements are RECORDED in the W output,
    not gated - a far-shore member stuck at water, G1-2's INF1RIF2 at 13 m, is not M3b's question, and the scored run's H5
    covers T14) and each mover's container at least 50 m closer to vertex 1; the arrival, H6, is MEDIUM and for the scored run only); H5; the water falsifiers; V-CRASH (a crash = "VOID", STOP before E). Then simcache_listing.ps1 -Out
    <L4>/simcache_after_prewarm.txt.
E.  THE RUN: golive_checks -Phase prelaunch -MarkerWant 5281 -StartBy 2026-09-29T01:55:00Z (a) / -MarkerWant 5285 (b'); `sh <L4>/g1_4_runner.sh scored-dryrun` (EXPECT
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
app's "(no back end reporting)" before shutdown); W-DEST relabelled W-MOVE, which now also needs every dispatched T10 member displaced > 50 m (T14 / T02 members RECORDED);
W-DONE per G1-3's rule (S9 = FAIL; NOT REACHED = the seat's call provided W-MOVE passes); in BOTH modes a vertex issued
< 30 trace-s before the last fix is NOT EVALUATED by H3 (a missing echo; an OFF echo still fails) and H5 (Fable review item 4;
W-MOVE's 30 s rule) - the app log carries no wall stamp per line, so a vertex's issue instant on the trace clock is its
mover's dispatch for vertex 1 and the instant the last member first came within 50 m of vertex k-1 for vertex k. Its --selftest: 53
checks PASS, 0 FAIL - G1-3's 41 (the uuid oracle, the chain checker, the CLEAN control, S7, the must-not-fire wet edge, 26
dirty controls) on synthetic runs in the M3b line forms, plus an ECEF oracle (G1-3's echo reads (54.029734, 23.305499, 0)),
the M3b name oracle, the clean M3b rows, and SIX NEW DIRTY controls each caught: route-not-exist (H1), executor-refused (H2),
vacuous-advance (H2), echo-off (H3, a member's echo 111 m off), no-move (H5, one T14 member parked), crash (V-CRASH VOID; the
vendor .log beside the callstack never matched), SEVENTH echo-missing (H3, one member's vertex-1 echo absent), and two
MUST-NOT controls: not-reached (T10 half-way to vertex 1 at the window's end - W-DONE "NOT REACHED - the seat's call", the W
gate PASSES) and late-issue (vertex 2 issued 10 trace-s before the last fix, no echo and no move yet - H3 and H5 read it NOT
EVALUATED and pass) [V: scratch laneG1-4\g1_4_score_selftest.txt].
THE CONTROL, FAIL-FIRST ON REAL DATA [V]: `g1_4_score.py runs\20260928T190047Z_run --wgate --member-names c1c` (G1-3's own
pre-warm, read with its own naming rule) -> exit 1, "W GATE: FAIL + VOID (branch A; H1, H2, H5, M10, M12, V-CRASH(VOID),
W-DONE(NOT REACHED, W-MOVE failed), W-MOVE)" - W-DONE's NOT REACHED counts only because W-MOVE failed: H1 15 relayed "route does not exist" (first L9811); H2 the pre-M3b equivalent L9979 (vertex 1
"1451 m from it, moved 0 m"); H5 0.0 m for all five members; V-CRASH the "Error vrfSimHLA1516e.exe" window, the callstack
record (19:05:16Z) and .dmp (19:05:17Z) for pid 3344 (names and mtimes only), the trace's backends 1 -> 0 at t=194.9 s and
the app's L10245; and, as it must, H3 PASSES there (10 echoes exact, vertices 1 and 2 - the binding G1-3 saw) and every
identity / population limb passes (scratch control_g1_3_wgate_c1c.txt). With the default M3b names the same run fails more
(the names do not match: I2, I6, I8, W1n ...; control_g1_3_wgate_m3b.txt). THE MUST-NOT-FIRE CONTROL on G1-2's clean pre-warm
(20260928T141734Z_run, the Literal build), re-run on the FINAL scorer (Fable review item 5) [V: scratch laneG1-4\control_g1_2
_prewarm_wgate_c1c.txt, sha256 dcb6ce0e...; the scorer g1_4_score.py sha256 8f7f5fd3...]: V-CRASH "no signal" (pid 40344), H1
0, H2 0; its W gate FAILS, as it must on that build, on the M3 limbs (M1-M5, M7, M16: the Literal route), H3 / H5 (no planned
vertex) and W-DONE (S9: the Literal path closes no planned step). W-MOVE PASSES there on the final scorer: G1-2's far-shore
member 48_IBCT/28ID__FRIENDL.INF1RIF2 (13 m, G1-3 sec 1(i)) is a T14 member, now RECORDED, not gated.

THE NEW ROWS (G1-4's one variable). G1-3's P0-P23, P-FALS and O1-O12 apply as registered there, their line numbers per sec 2,
with the changes named after this table.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| H1 | THE EXECUTOR TAKES THE ROUTE: ZERO relayed "route does not exist" from any member of any task, in the pre-warm and in the scored run (scorer H1); the vendor sim log's count of the same text is 0 too (R-VEND, count only). | HIGH | Any = STOP. With R-REF showing the reference WHOLE (sec 4 COMPETITORS), the name reading of G1-3 (i) is REFUTED; with it cut, the budget of sec 1(u) is wrong. | |
| H2 | NO EXECUTOR REFUSED: ZERO L-REFUSED lines and ZERO "EXECUTOR REFUSED" TASKABRT reasons; ZERO intermediate vertices COMPLETED VACUOUS with the unit moved < 100 m (the pre-M3b form). | HIGH | Any = STOP. Two branches: (a) with H1 > 0 or H5 failing - the executor refused again (the TASKABRT is M3b working as designed, the move is not); (b) H2 fires with H1 = 0 and H5 PASS - M3b (2)'s refusal test fired on a GENUINE move (it judges the container's centroid fix, evidenced live with ONE member only - PLAN_MOVEMENT :133, RUNBOOK :3959): STOP, M3b (2) is reviewed, and it is NOT a name verdict. | |
| H3 | THE DESTINATION ECHO EXACT, AS IN G1-3: every member of T10 and T14 (and T02's one) prints the level-3 echo for every vertex its container issued, and each converts to that vertex at ellipsoid height 0 within 0.5 m (G1-3: exact to 0.000 m on 10 of 10). | HIGH | Any echo off, or missing for a vertex issued 30 trace-s or more before the last fix, = STOP (a vertex issued later is NOT EVALUATED for a missing echo); the binding the CLOSED list settles is not reopened - the miss is recorded and the seat decides. | |
| H4 | THE NAMES AS sec 1(u): each L-POP lists the registered short names in slot order, each "<short> = <container>.<suffix>"; L-ID-POP names exactly the v5 uuids of G1-3 (unchanged: the derivation takes the suffix) beside the full names; EXACTLY 59 L-ID-CREATED (the 23 members under their G1-3 uuids with their short names as markings); 23 L-MEMBER "(8..16 chars) came back as EXACTLY that name ... bound by its uuid (C1d)"; ZERO NAME DISAMBIGUATED / NAME COLLISION (scorer W1n, I2, I3, I6, I8, I12, I13). | HIGH | Any limb = STOP (C1d's regression guard, RL-20260928-02, as G1-3's P3). | |
| H5 | EVERY TASKED MEMBER MOVES at every vertex it is sent to: displacement > 1.0 m on the trace (scorer H5; G1-3: 0.0 m for 5 of 5); and G1-3's P10 (each mover's members displaced > 50 m: 1 of 1, 5 of 5, 17 of 17). | HIGH | A member that does not move at a vertex issued 30 trace-s or more before the last fix = STOP (a later vertex is NOT EVALUATED). A member whose plan FAILED (a member FAILED line, M10, or a relayed failure, M12) also reads 0 m: that miss is M10 / M12's - a PLAN failure - not the executor's. | |
| H6 | T14 AND T10 ARRIVE IN THE SCORED RUN (G1-3's P12): L-ARRIVE with 1/1 and the ONE POSITION suffix, then one TASKCMPLT each. The PRE-WARM is judged by G1-3's W-DONE rule (sec 3 W), not by this row (SEAT DECISION 2026-09-28). Still scored. | MEDIUM (Fable review item 3: an arrival under Auto has never been observed; G1-2's T10 arrived on the LITERAL path) | A miss is RECORDED as "not an M3b verdict", with its mode: a plan failure (M10 / M12), the stall watchdog on the container's centroid fix, T10's armed end (450 SIM s) or the window's 2700 s cap. | |
| R-REF | THE CARRIED REFERENCE: every relayed "Move-Along Route: \"<ref>\"" is the WHOLE "<member> Path part <n>" or "<member> Path part <n>_<counter>", <= 34 characters (G1-3: 5 of 5 "<30 chars> Pathr"). | MEDIUM | Recorded; read with H1 (sec COMPETITORS). | |
| V | THE WINDOW IS VALID - NO BACK-END CRASH (the BACK END only; a persistent holder's resignation is recorded, not a void): no crash record for the back end's pid dated before the window closed, no "Error vrfSimHLA1516e.exe" window, no `BACK END CRASHED` line or manifest backendCrash, StopVrf52 exit 0 or 6 (not 8), the trace's backends never 1 -> 0, the sim clock never lost before shutdown (scorer V-CRASH). IF a crash happens, the harness NAMES it (the runner line + exit 3, StopVrf52 exit 8) - M3b (3)'s first live test. | MEDIUM (no crash: G1 and G1-2 ran clean, G1-3 crashed once, n = 1, its stack unread); HIGH (a crash is named by the runner and StopVrf52) | A crash = the window is VOID: neither a pass nor a fail of M3b, no row is scored, the successor is G1-5 with new numbers. A crash the scorer sees and the runner did NOT name = STOP (M3b (3) failed live). | |

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
composition, settings, sequence and harness geometry (the differences not intended: sec 4a).

## 4a. Differences named, not intended (Fable review item 6)

- M3b IS THREE CHANGES, NOT ONE: (1) the names - the variable the cause claim is about; (2) EXECUTOR REFUSED and (3) the
  crash-void ship beside it. (2) changes what a vacuous intermediate vertex DOES (it now fails the task instead of advancing),
  (3) only what the harness SAYS about a crash. Vertex 2's "Cound not create route" is observable only if vertex 1 moves:
  under (2) a refused vertex 1 ends the task before vertex 2 is issued, where G1-3 advanced to it.
- THE FEDERATION IS 3-6 HOURS OLDER: the same long-lived rtiexec 47980 (up since 2026-09-26) and the same federation instance
  held by 56380 since 19:00:02Z. G1-3's crashed back end (pid 3344) was closed by StopVrf's taskkill without a clean resign of
  its own [A: the G1-3 Result]; the rtiexec log nevertheless records a "VR-Forces Sim Engine 5.2d" federate (handle 5)
  "has resigned" at its L582339, after G1-3's app (remoteControl 26024) resigned at L581979 - the only Sim Engine joined since
  56380 joined (L535605), so that resign is taken to be pid 3344's [A: attribution by order; the log names no pid for a Sim
  Engine]. READ-ONLY FEDERATE LIST at 2026-09-28T22:2xZ [V: scratch laneG1-4\rti_federates.py over runs\launch52\rtiexec_20260926T115802Z5.0.1-...-47980.log,
  join / resign lines only, names and handles printed]: joined now = remoteControl 56380 (handle 2) ONLY. So pid 3344's federate does not appear to linger in the federation
  G1-4 joins; the tie of that handle-5 resign to pid 3344 stays [A]. Step A repeats the list.
- THE SCORED RUN HAS NO G1-3 CONTROL: G1-3 stopped at its W gate, so the scored rows' control is G1-2's scored run
  (20260928T142731Z) on another build (699552c, the Literal path) - every scored comparison names that difference.
- THE HOLDER: branch (a) reuses G1-3's holder 56380 (G1-3 claimed and started it); branch (b') would start a new one on the
  same RtiProbe tree.

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

## 8. Where the record was silent or disagreed - SEAT DECISIONS 2026-09-28 (on the lane's eight open points)

1. THE CAP: the budget stays 34 (the code's conservative VrfNames.UuidReferenceChars :91, "35 was never bisected"); every
   cut-A reference fits under both 34 and 35 (sec 1(u); the handoff's "35-byte payload" and the CLOSED list's "cuts at 35"
   agree with that).
2. W-DONE IN THE PRE-WARM: G1-3's rule (sec 3 W) - reached-not-closed (S9) = FAIL = STOP; NOT REACHED = recorded, the seat's
   call, not a stop by itself provided W-MOVE passes. "T10 arrives" is H6, the SCORED run only; G1-2's straight-line pre-warm
   arrival [A] does not carry to a road path with a planning pause.
3. H5's "moves" is > 1.0 m (1 m of fix jitter allowed; G1-3's refused members read exactly 0.0 m) - accepted.
4. BUFFER stays 10, untouched (RL-20260928-03's operative text); testing 0 is for the owner after a G1-4 failure.
5. THE ORDER: the handoff (written after the G1-3 Result, under the seat switch RL-20260928-04) governs - G1-4 now; the
   callstack read stays the owner's open call (HANDOFF_SEAT sec 4) and does not block G1-4.
6. THE HOLDER: branch (a); E starts no later than 01:55Z 2026-09-29 (replacing 02:15Z after Fable's review; else branch
   (b')); a holder that resigns inside a window is recorded and
   does not void it by itself - V is about the back end only (sec 3).
7. THE TEARDOWN SLACK: the scorer counts crash records up to the window's close + 5 s [A: the lane's choice, approved by the
   seat]; the runner itself lets a window stand for a crash dated after it closed (WARN, RunC2SimScenario.ps1 :5980-5982).
8. T02: H2 cannot see a refused T02 - its one vertex is the last, which M3b leaves to D-6 (sec 1(v)); its D-6 path is
   covered by H6 / the arrival rules (and H1 / H5 see the refusal and the missing move).

## Result (written after the harvest, never from a live read)

RESULT 2026-09-28: STOPPED AT THE W GATE - NO SCORED RUN. The pre-warm (run 20260928T225800Z_run, launched 22:57:59Z, runner
exit 0 at about 23:07:14Z) scored "W GATE: FAIL (branch A; H5, P-FALS(e)) - STOP before E" (g1_4_score.py --wgate, sha256
8f7f5fd3...; scratch laneG1-4\W_gate_20260928T225800Z_run.txt). E was not launched: P0-P23 and H6 are NOT SCORED and the
scored block 5281-5291 was never claimed. M3b's question is answered at n = 1 (below): 22 of 23 tasked members moved on the script's routes; the 23rd is read as
a terrain hold (N1, [A]). The two misses are not read as M3b's: one T14 member never moved off a lake shore (N1) and T02's member crossed its river 16 m from a road
bridge by the scorer's chord (N2); the H5 limb that stopped the gate governs as written
(N3). By the stop rules (sec 4) nothing is patched or re-run under this registration; the successor is IRONSTORM_AGG_G1-2026-
09-28-5 or later, with new numbers. Marks: [V] = checked by this lane after the run (the run directory, the runner log, the
trace, the reports capture, the vendor script and settings, the OSM tile copy the scorer read, the machine - all read-only);
[A] = the seat's record or an inference. The vendor sim log and C:\MAK\logs: counts, names, sizes and mtimes only.

THE SEQUENCE AS RUN (by the seat under RL-20260928-01) [V: scratch laneG1-4 artefacts and the runner log
runs/launch52/RunScenario-ironstorm-agg-g1-4-prewarm-20260928T225759Z.log, unless marked]:
- A, 22:32:08Z: 1 check FAILED - 16 MSBuild.exe started 22:28:32Z (A_prelaunch.txt; idle build nodes of VS Code's C# Dev Kit,
  /nodeReuse:true [A: the seat]), left to time out (HANDOFF_SEAT sec 3 item 4); gone by 22:56:28Z [A]. The federate list:
  remoteControl 56380 (handle 2) the only join (A_federates.txt). A2 offline on the deployed exe: populate 199 PASS,
  planned-move ALL PASS (A2_populate.txt, A2_plannedmove.txt).
- C3, 22:32:58-22:33:29Z: PushInit exit 0, PushOrder exit 0 (C3_stdout.txt).
- W: prelaunch at 22:56:42Z 0 FAILED (W_prelaunch.txt: the registered hashes and settings, 41 / 0 road tiles, cache 520 /
  27c2117e, 56380 up with 243 min left, marker 5270); the sim cache listed 22:57:04Z; the dry run 22:57:26Z exit 0; the
  pre-warm launched 22:57:59Z (g1_4_runner.stamps.txt): holder 56380 recognised PERSISTENT (runner log :89), block 5270-5280
  claimed, marker 5270 -> 5281 (:131), Stage 2h holder pid 61064 on 5277 joined in 3 s (:162), back end pid 40228 on 5270
  started 22:58:14.617Z (:187), the app pid 16272 on 5274 (:227), the oracle gate passed, the order on the bus 23:01:51.345Z
  (:261), the 120 s window, every task terminal at t+120 s (4 TASKCMPLT + 1 TASKABRT, :273), the window ran 134.8 s (:277),
  the app exit 0 - a clean resign (:285); StopVrf EXIT 6: the graceful close (taskkill without /F) was REFUSED for 120 s and
  the run's own back end force-stopped - its windows were "NVOGLDC invisible" and "Default IME" only, no "Error
  vrfSimHLA1516e.exe" modal (stopvrf.stdout.log); the registered gate accepts 0 or 6 (sec 3 W). The teardown ran; the
  watchdog stood down 23:07:18Z (runner-watchdog.log). The sim cache listed 23:08:18Z. Scored by the seat [A].
- The owner rebooted the machine afterwards: LastBootUpTime 2026-09-28T23:16:45Z [V]; at 23:30Z no rtiexec, rtiForwarder,
  rtiAssistant, RtiProbe, vrfSim or MSBuild process exists [V]. Holder 56380 (appNo 5255) is gone. Not part of the run.
THE CLOCKS [V]: trace t0 = WatchVrf join 23:00:56.973Z; T10 dispatched t=88.5 (SIM 404.1), T14 t=92.5 and T02 t=92.7 (SIM
437.2); the last fix t=219.0 (23:04:36Z); the sim ran about 6.2 x wall (L64753: 70.6 WALL s = 438.7 SIM s). Reports
(reports-captured.log): TASKSTRT T13 and T01 23:01:54.3Z, T10 23:02:25.5Z, T14 23:02:29.5Z, T02 23:02:29.7Z; TASKCMPLT T01
and T13 23:02:29.0Z (the 300 s Duration, L26847, L26853), T10 23:03:39.4Z, T02 23:03:51.2Z; TASKABRT T14 23:04:21.5Z.

WHAT HELD [V: the W gate output]: W0-W7 (1 / 5 / 17 of N attached and published); IDENTITY branch A, I1-I17 - 59 of 59
created under the requested uuid and bound by it, the M3b short names as markings (C1d's guard, RL-20260928-02); H4 by those
limbs (W1n, I2, I6, I8, I12, I13); M1-M9 and M12-M17 on all three movers (road layer 16 / 10 / 14 readable, 0 not; every leg
NEAR -> MAK_ROAD on the offline table's ways 384833185, 372319246 / 367165904 / 1262103420, 783013873; the chains in
order); M10 0 FAILED; H1 0 relayed "route does not exist" (G1-3: 15) and R-VEND 0 in the vendor log (count); H2 0 EXECUTOR
REFUSED and 0 pre-M3b equivalent; H3 33 of 33 echoes exact (T10 5 x 3, T14 17, T02 1); R-REF 35 of 35 WHOLE - "<member>
Path part <n>", n = 1 on every T10 and T14 line and n = 1, 2, 3 on T02's (L27575, L52031, L73145: its 5.3 km road path split
at MAX_POINTS_PER_ROUTE 100, navigate-to-location.lua :31, :210-233); the move-along prints no "_<counter>" (the vendor
save's counter is the route uuid's, not the printed name's); V-CRASH no signal for pid 40228 (no crash record in C:\MAK\logs
by name [V: listing - only pid 3344's of 19:05Z], no modal, backends never 1 -> 0, the sim clock read to the end); W-MOVE PASS
(T10 5 of 5 > 50 m; T02 1 of 1 and T14 16 of 17 RECORDED); the fixes-inside-water limb (0 fixes deeper than 25 m).
THE MOVES [V: the app log, the trace]: T10 - vertex 1 closed 7 m from it, 1,453 m moved (L48635); vertex 2 10 m, 797 m
(L61201); vertex 3 55 m (scorer R12); ARRIVAL EVIDENCE 70.6 WALL s after dispatch (L64753), held to the 450 s end time,
TASKCMPLT (L66861); the container's final fix 1.7 m from the destination. T02 - its one vertex closed 6 m from it; OVERDUE at
the end time (L57321, 331 of 300 s); ARRIVED 81.5 WALL s = 524.4 SIM s after dispatch, 5,036 m of a 5,341 m route (L74995),
TASKCMPLT on arrival (L74999); final fix 0.5 m. T14 - 16 of 17 members' navigate-to-location COMPLETED at vertex 1 (L35555
INF2HQ1 first, SIM 528.7, to L69777 INF3RIF3, SIM 917.8: "16 of 17 member(s) ended ... waiting for the rest"); the vertex never
closed and vertex 2 was never issued; OVERDUE at the end time (L57323); STALL "no member moved more than 50 m in the last 360
SIM s (max 19.9 m)" on the container's ONE POSITION (L94977), TASKABRT (L94979) at 23:04:21.5Z, about 36 wall s after the
last mover came to rest. W-DONE: NOT REACHED - the seat's call.
WHAT FAILED [V]: H5 - 48_IBCT.INF3WPN1 0.0 m at T14 vertex 1 (33 member-vertex pairs judged, 1 not moved; W gate output line
135). P-FALS (e) / P11 / P14 - 28ID.HQ1 "crossed river 8011072 at 54.007560,23.236494 16 m from any drivable road bridge and
drove on 2990 m" (line 136).

N1 - 48_IBCT.INF3WPN1 NEVER MOVED [V]:
(a) WHAT IT IS: slot 16 of 17 of 48_IBCT, template Mech CO (USA, M2), composition row C-USA-BDE-UCI/INF3/WPN1 (L616, L690) -
    the same template as the eleven other Mech COs, every one of which moved (773.8-1,754.9 m); its mobility is the aggregate
    sysdef's, mech / motor / tank alike ((d) below). Created under uuid b6c4756d-e5c1-5d13-a1c9-508222bee540 (L908), marking
    exact (L912), altitude 135.96 m from the TERRAIN QUERY (L754).
(b) WHERE IT WAS BORN: its ring slot lay IN OSM water 16373225, and the app moved it 75 m south-east to (54.022048, 23.309402),
    "the nearest ground clear of OSM water and of OSM buildings within 10 m" (L690; VrfC2SimService.cs :3355-3369 - the 10 m
    is the BUILDING clearance, Vrf:PreflightBuildingClearanceMeters; the water rule is "not inside the polygon", clearance 0,
    ring step 25 m). On lane G1's copy of the deployed osm set - the tiles the scorer read - that point is 1.7 m OUTSIDE the
    shoreline of 16373225 (natural=water, unnamed) and 1.7 m INSIDE landuse=forest 232776542; vertex 1 lies 1,664 m away at
    bearing 120 deg, away from the lake (north-west of it); the container point 417 m at 135 deg [scratch laneG1-4\
    harvest_geom.py, harvest_geom2.py]. THE CONTROL: three siblings were nudged the same way out of lake 197345448 (Jezioro
    Wiersnie) - INF1RIF2 to 2.3 m from its shoreline (L666), INF1RIF3 to 5.5 m, inside forest 232776590 (L668), INF1WPN1 to
    7.5 m, in that forest (L670) - and ALL THREE MOVED (1,156.0 / 870.5 / 773.8 m). OSM shoreline distance alone does not
    predict the stop.
(c) ITS CONSOLE, LINE FOR LINE AGAINST A SIBLING (INF3RIF3, the twin Mech CO): identical through the move-along's start -
    "Navigate-to: pathQuery=MAK_ROAD" (L27371 / L27363), "obstacleQuery=MAK_OBSTACLE" (L27373 / L27365), the script
    controller began navigate-to-location at SIM 440.662 (L27375 / L27367; all 17 at that stamp), the echo exact to the
    vertex (L27377 / L27369), "Navigation: Initializing" (L27419 / L27387), ROE hold-fire (L27453 / L27421), the
    aggregated-move-along-controller began the move-along subtask at SIM 441.196 / 441.229 (L27533 / L27537) on the WHOLE
    reference "48_IBCT.INF3WPN1 Path part 1" / "48_IBCT.INF3RIF3 Path part 1" (L27535 / L27545). THEN THEY DIVERGE. INF3WPN1
    printed ONE level-1 line, "Movement constrained by features" (L27577, unstamped, between the SIM 441.262 and 441.862 stamps; the subtask began at 441.196), and
    nothing else to the end: no "Can't receive supplies while moving" (0; INF3RIF3 printed it 92 times from L27869 - the sim's
    own sign of a unit in motion), only the stationary "Use Supplies ... No nearby supply units" (179 pairs after L27577, 241 in the whole log), no subtask end,
    no task end (INF3RIF3: subtask Completed SIM 917.524, task Completed 917.757; the 16 movers ended between 528.7 and
    917.8). It is the run's only "Movement constrained" line (app log 1; vendor log count 1). No warning, failure or refusal
    anywhere: 0 "route does not exist", 0 "Could not compute path" / "Invalid path computed" / "Cound not create route",
    0 "Terrain too steep" (our log and the vendor's, by count). Its 81 trace fixes (t=56.3-219.0) are one point,
    (54.022048, 23.309402, 135.0).
(d) WHAT THE VENDOR SAYS: the aggregated-movement actuator sets speed by a terrain-mobility query at the unit's centre point -
    AggregateLevelBase\vrfSim\systems\movement\tank-aggregated-movement.sysdef :112-131: MAK_TANK_UNRESTRICTED_TERRAIN
    speed-factor 1, IMPASSABLE 0 (priority 200), RESTRICTED_L2 0.25, RESTRICTED_L1 0.65; appData\settings\featureconfig.txt
    :269-272: MAK_TANK_IMPASSABLE_TERRAIN = MAK_WATERWAY OR ALPINE, RESTRICTED_L2 = FOREST OR URBAN OR MOUNTAIN (the MECH
    and MOTOR rows :259-267 read the same); :412-413: MAK_WATERWAY = Waterway OR OCEAN OR COAST OR RIVER OR LAKE. UG52 27.1.4
    p531: "Rivers and water bodies may stop a unit completely, although bridges allow them to cross"; "the center point of
    the unit is used to determine what type of terrain the unit is on"; the detail is in "Configuring Aggregate-Level
    Movement Restrictions" of the Adding Content manual, PDF only, unread (FINDING_GROUND_MOVEMENT_PRACTICE :178-179). The
    text "Movement constrained by features" is in no installed script or header (0 hits in data\simulationModelSets and
    include) and in none of the eight vendor txt files; G1-2 relayed it exactly three times, each for a member at water and
    never for a Mech CO stopped by a building (G1-2 Result :851-853). The script side is silent: navigate-to-location.lua ends
    only when its move-along stops (:252-259) and prints nothing while it runs.
(e) THE READING MOST CONSISTENT WITH THE EVIDENCE [V for the observation; A for the mechanism and the feature]: INF3WPN1 was held at speed factor 0 from its first tick by a terrain
    feature at its birth point - the mobility table's IMPASSABLE class, which in this run's data means MAK_WATERWAY (the sim's
    Lake layer; ALPINE is not here). The route was planned, created and TAKEN - the subtask ran on the whole reference; the
    executor refused nothing; the unit could not move. WHICH feature, and why the sim's water covers a point 1.7 m outside
    one OSM polygon while INF1RIF2's point 2.3 m outside another was dry, is NOT settled: the actuator queries the sim's own
    streamed features (VRFSIM.Aggregate.feature.model.xml maps OSM water to its Lake layer - FINDING_GROUND_MOVEMENT_PRACTICE
    :184-190 [A]), not our cached tiles, and no feature query at that point is in the evidence. What the app did is settled:
    its nudge stops at the OSM polygon's edge, so a member can be born a metre from a shoreline.
THE COMPETITORS [V]: (i) the executor refused the route (G1-3's mechanism) - EXCLUDED: 0 refusals, the subtask began on the
whole reference, 16 siblings moved on identical references. (ii) a planning failure - EXCLUDED: the script starts the
subtask only after a path of two or more points and a route that passed its validity check (lua :177-186, :213-246), and 0
failure lines. (iii) the building slope stop of G1-2 - EXCLUDED: 0 "Terrain too steep"; the nudge's own check found no OSM
building within 10 m. (iv) the forest (factor 0.25) - not a stop by the table, and INF1RIF3 / INF1WPN1 sat in forest and
moved. (v) the planned path's first leg led INTO the lake - not excluded (the path is not printed: DEBUG_DETAIL false, lua
:17; the sysdef's debug-detail false), but it collapses into (e): at 0.0 m over 81 fixes the unit never left its birth cell,
so the 0-factor terrain is AT the birth point. (vi) NOT WEIGHED BY THE HARVEST, ADDED BY THE COLD REVIEW (2026-10-04): the
planner's obstacle constraint - navigate-to-location with obstacleQuery MAK_OBSTACLE and buffer 10 m; a unit born 1.7 m
from a water polygon starts inside that buffer, and "constrained by features" fits that emitter as well as the speed-factor
table (the string's emitter is unknown: in no script, header or manual). The siblings at 2.3-7.5 m, also inside 10 m,
moved - this weakens (vi) but does not exclude it; (e) and (vi) call for different fixes. What would falsify (e): the same
slot moving with obstacleQuery none or buffer 0; a sim-side feature query at (54.022048, 23.309402)
returning no impassable feature, or the same line printed for a unit clear of water - neither seen.
THE CONSEQUENCE FOR T14 (a design fact, not the vendor's): M3 waits for "the LAST of M" (M9); a member whose planning task
never ends keeps the vertex open, and only the stall watchdog closes the task (an S5-shaped close). M3b (2) cannot see it:
EXECUTOR REFUSED judges a vertex that CLOSES without movement, not one that never closes.

N2 - T02's RIVER: CONSISTENT WITH THE BRIDGE AT EVERY FIX (NO FIX IS ON IT); THE 16 m IS THE CHORD [V: scratch laneG1-4\harvest_geom.py on the scorer's
tiles and filters]:
(a) THE RULE (g1_4_score.py): each chord between consecutive trace fixes is intersected with every river line (:1405-1421); the
    crossing point's distance to the nearest DRIVABLE BRIDGE WAY (bridge not no / boardwalk, highway not in NoDriveHighway -
    OsmFeatures.cs IsRoadBridge :416-424 as transcribed at :1357-1360) is measured (:1423-1432); a crossing is "OFF A BRIDGE"
    when that distance exceeds BRIDGE_TOL_M = 15.0 (:1317) and the member then drove 50 m or more (:1604-1606); P-FALS (e) /
    P11 / P14 fail on it (:1644-1663).
(b) THE GROUND: river 8011072 (Czarna Hancza, waterway=river) passes 0.1 m from the scorer's crossing point. OSM 218414262 -
    highway=unclassified, bridge=yes, layer=1, surface=wood, a 2-point way 57 m long from (54.007245, 23.236304) to
    (54.007605, 23.236926) - CROSSES river 8011072 and lies 16.2 m from that point; it is the ONLY bridge-tagged way within
    300 m. The approach is 218414261 (unclassified, unpaved), the far side 178824546 (track, gravel). T02's leg decided NEAR
    (0 m) -> MAK_ROAD on 384833185 (scorer R8), so the member planned on the road network; its path is not printed.
(c) THE FIXES (28ID.HQ1's 2-s trace fixes, 135-207 m apart on this leg): t=127.4 0.1 m from road 218414261; t=129.4 0.3 m from
    it, 84 m short of the crossing point and 51.6 m from the bridge's south-west end; t=131.4 9.4 m from track 178824546,
    61 m past the crossing and 42.3 m from the bridge's north-east end; from t=133.5 up the track. The chord t=129.4 -> 131.4
    is 144.9 m long across a bend (road -> bridge -> track) and cuts the river at 54.007560, 23.236494 - 16.3 m from the
    bridge way, 1.3 m over the tolerance. A member that entered the water would have stopped (MAK_WATERWAY is IMPASSABLE,
    N1 (d)); this one drove on 2,990 m to its destination (0.5 m) with no "Movement constrained" line (0 for HQ1), and G1-2's
    T02 member, on the straight line 162 m upstream, was held at this very river ("Movement constrained by features", G1-2
    Result :761). READING [V for the fixes; A for the path]: the member most likely crossed on bridge 218414262 - no fix is on it (51.6 m
and 42.3 m from its ends; the chord passes 13.7 m from it at its nearest); the scorer's 16 m is the
    fix chord's corner cut - inside fix-spacing error, outside the 15 m rule. The competitor, a ford beside the bridge, has no
    fix and no line for it and would need the sim's river layer to have a gap where OSM has none; the fixes alone (145 m
    apart) cannot exclude it, the mobility table does if the sim's river is continuous there.
(d) THE RECORD ON THIS SPOT: G1-2 - T02's member stopped AT river 8011072 on the literal straight line (last fix 2.5 m from the
    river point 54.0087722, 23.2351231; S5 / S6; G1-2 Result :758-762). G1-3 - T02 was never dispatched (the crash); its
    registration sec 1(t) (:322-325) put the nearest drivable bridge 3,743 m away (OSM 307511217): its probe (scratch
    u3\laneG1-3\river_probe_g1_3.py :76-79) counted a bridge only if one of its VERTICES lies within 15 m of the river line,
    and bridge 218414262's two vertices sit about 28 m either side of the river, so it was missed [V: the probe read, the same
    tile copy]. On the scorer's index that bridge is 175 m from G1-3's straight-line point. G1-4 is the first run in which
    T02's member crossed the river and arrived.

N3 - H5 IN THE W GATE (recorded, not re-scored) [V: this file and commit 5a400d3]: sec 3 W (:274-278) confines W-MOVE's
per-member clause to T10 - "T14's and T02's member displacements are RECORDED in the W output, not gated - a far-shore member
stuck at water, G1-2's INF1RIF2 at 13 m, is not M3b's question, and the scored run's H5 covers T14" - and the same sentence
keeps "H5; the water falsifiers; V-CRASH" in the W gate's list (:278). H5's row (:343) reads "EVERY TASKED MEMBER MOVES at
every vertex it is sent to ... = STOP" with no pre-warm exemption (sec 4 MEASURES :301-302; sec 8 item 3, :451), and the
scorer's wgate() gates H5 over every tasked member (g1_4_score.py :1705-1707; W gate output line 135). So the pre-warm
failed on H5 for a T14 member that the seat's W-MOVE decision had declared not M3b's question. Commit 5a400d3 changed
:275-278 and :307 and left H5's row and the gate list as they were. The verdict as scored stands (RULING below).
COLD REVIEW 2026-10-04: NOT A CONTRADICTION - W-MOVE (> 50 m) and H5 (> 1.0 m) are different limbs (G1-2's INF1RIF2 at 13 m,
the example behind the W-MOVE decision, passes H5 and fails only W-MOVE), so confining W-MOVE did not confine H5; the gate
list's "H5; the water falsifiers" was in the first registration (c47df77 :259). The W-MOVE rationale ("the scored run's H5
covers T14") sits uneasily with H5 in the gate list, but AS WRITTEN H5 GOVERNS; "meant to confine" was an inference about
intent. Independently of H5 the gate FAILS on P-FALS (e): 16.24 m > BRIDGE_TOL_M 15 under the registered rule.

M3b'S QUESTION AT n = 1 (measurement, then implication): 0 "route does not exist" against G1-3's 15 on the same order, fixture
and settings; 35 of 35 references carried whole (G1-3: 5 of 5 cut at 35 + "r"); 22 of 23 tasked members moved on the
script's routes; four planned vertices closed with the unit 6-55 m from them; two containers arrived. The name reading of
G1-3 (i) is CONSISTENT with this run, and its falsifier - a whole reference still refused - did not fire. Sec 7 stands: n = 1
does not exclude an intermittent (iii), and a pass says the M3b build moved these members on this order once. H2's branch (b)
did not fire: no EXECUTOR REFUSED on a genuine move.

THE CARRIED UNEXPLAINED ITEMS OF G1-3 [V]:
- THE CRASH: did not recur (V MEDIUM held; 1 crash in 2 runs on this path). Peak working set 4,030 MB at 23:04:06Z
  (thread-samples.csv; G1-3 3,668, G1-2 3,801 / 3,876), peak 3.60 cores at 23:02:56Z, WS-runaway alerts 930.6 MB/min at
  22:59:25Z and 1,536.1 at 23:01:45Z (load-time, as before). The vendor's addGeometry comment (buffer > 0) was exercised with
  buffer 10 on 35 routes without a crash. pid 3344's callstack stays unread (the owner's call).
- "Pathr": gone - every reference whole (R-REF). WHERE the cut at 35 happens stays [A] (sec VENDOR CITATION).
- VERTEX 2's "Cound not create route": 0 (P7c held). Consistent with the name reading of it; not proven.
- O8: the init's 36 create altitudes came from the FALLBACK again (L281: "Terrain profile request 1 for task 'INIT PLACEMENT'
  got no reply within 10 s"; L355: "0 of 36 ... 36 from the FALLBACK", 23:01:40.5Z) while the 23 members' came from the
  TERRAIN QUERY (23 of 23; L708, L720, L754). Unchanged; nothing in M3b touches it.
- HQ1 / RIF2 FIRST: on T10 the Stryker HHT (HQ1) returned first at all three vertices (L42597, L56125, L66005); the last was
  WPN1, RIF2, RIF1 in turn. On T14 the four Stryker HHTs and CAV1 completed first (SIM 528.7-617.3) and the Mech COs from
  629.0 to 917.8. Recorded; the two templates' speeds and slots are the obvious reading, not claimed.

RECORDED [V]: O6 - 18 "No creator found" lines, the navigate-to-location twin at L25055 (as G1-3). P19 - 79 cache HITs,
0 HTTP FETCH (L103983). THE SIM CACHE - ONE URI-cache entry added: __default\uri\26\a0927b0f7e311c767aa5fab66caa661644a25a
.meta (700 B) and .osgb (151 B), both written 23:02:31.94Z (during T10's move); TOTAL 21,805 -> 21,807 files, 495,598,329 ->
495,599,180 B (simcache_before / after_prewarm.txt; G1-3's pre-warm added nothing). StopVrf's exit 6 - the forced branch,
first seen on this path [A]; the stale-federate check the runner names for it is moot after the reboot. The back end's own log
(C:\MAK\logs\...-185817-...-40228.log, 15,148,205 B, last written 23:04:37Z) is the vendor copy's twin - names, sizes and
counts only. Vendor log counts: "Path part" 35, "48_IBCT.INF3WPN1" 13, "Movement constrained" 1, every failure text 0.
The 5.2 command line was G1-3's byte for byte (the log name aside).

WHAT IT MEANS (implication, not measurement): M3b (1) did what it was built for - every member's move-along took its
whole reference (35 of 35, 0 refusals) and 22 of 23 members moved, the 23rd read as a terrain hold (N1, [A]); M3b (2) had nothing to refuse; M3b (3) had nothing to void. The pre-warm's two misses are a birth slot
at a shoreline and a scorer chord across a bridge - neither is read as the M3 planner's, the executor's or M3b's ([A] for N1).
The W gate's FAIL rests on an H5 limb that governs as written (N3) and a P-FALS (e) reading the fixes can neither confirm nor exclude (N2);
both are recorded here and neither is re-scored. A member born a metre from water and a vertex held open by a member that
never ends are the two mechanisms this run adds to the record.

NUMBERS [V: runner log :94-106, :131, :307; run-manifest.json]: block 5270-5280 claimed by the pre-warm, marker 5270 -> 5281
(17b0859). 5270 (back end, pid 40228), 5272 (WatchVrf pre-check), 5273 (WatchVrf trace), 5274 (VrfC2SimApp, pid 16272,
exit 0), 5275 (Stage 2c RtiProbe), 5277 (Stage 2h holder attempt 1, pid 61064) CONSUMED; 5271 (--no-gui), 5276 (the oracle
gate passed), 5278-5280 BURNED. The scored block 5281-5291 was never claimed. Holder 56380 (appNo 5255) ended with the reboot
at 23:16:45Z. A successor takes new numbers from 5281 under branch (b'): the holder claim 5281-5284 (marker -> 5285), the
pre-warm 5285-5295 (-> 5296), the scored run 5296-5306 (-> 5307); Stage 2r starts a fresh rtiexec.

NEXT, in order: (1) Fable's cold review of this Result (RL-20260928-04), then the owner. (2) RULE - the owner: whether the W
gate's FAIL binds as scored (H5 on a T14 member, N3; P-FALS (e) as the chord read it, N2) or the successor's gate is
corrected; pid 3344's callstack stays the owner's open call. (3) PREREG G1-5 on the same build and order, with: the scorer's
bridge rule reading the fixes against the road and bridge WAYS (or interpolating along the road), not the chord; H5 confined
as W-MOVE was, or gated by the seat's explicit choice; INF3WPN1's birth as a registered observation. (4) DESIGN - the seat's
call, not under this registration: a WATER clearance for nudged slots (the building clearance is 10 m, the water clearance 0;
VrfC2SimService.cs :3355-3369); a per-member hold on "the LAST of M" (a member whose task never ends holds the vertex open
until the stall watchdog). (5) The doc updates of sec 6 - PLAN_MOVEMENT rows M3b and G1-4 and the step table, RUNBOOK 11o
and 0.5.9, HANDOFF_SEAT sec 2, Appendix B annotated - after the review. (6) The Label follow-up (HANDOFF_SEAT sec 3 item 5)
is unblocked.

COLD REVIEW AND RULING (2026-10-04): Fable, cold, on the evidence only (RL-20260928-04): ACCEPT WITH FIXES - every number it
could reproduce held; its fixes are applied above (N1 (c) stamps, (e) to [A] and competitor (vi); N2's headline and
reading; N3 not a contradiction; M3b's question and WHAT IT MEANS to 22 of 23; the runner's exit time; exit 6 [A]).
RULING RL-20261004-01 ("As recommended"): (2) the W FAIL BINDS AS SCORED - G1-4 is STOPPED at its W gate, nothing is
re-scored. (3) G1-5 gates H5 on EVERY tasked member, after a build that gives nudged member slots a water clearance of at
least the planner's 10 m buffer; the scorer's bridge rule reads the fixes against the road and bridge ways, fail-first on
this pre-warm. NEXT (4)'s water clearance is thereby ruled; its per-member hold on "the LAST of M" stays the seat's.
ADVERSARIAL REVIEW: (N1) the strongest competitor to "held by an impassable feature at its birth point" is a defect of the
planned route itself - a first leg into the lake, or a move-along that never started its motion for a reason the console
does not print; against it stand the one vendor line that exists, which names features, 0.0 m over 81 fixes (the 0-factor
cell is the birth point), and sixteen siblings that moved on identical references. Unexplained and standing: WHICH feature
(the sim's own layer, unread), and why 1.7 m from one lake stops a unit while 2.3 m from another does not - a falsifier of
any rule that reads OSM distance alone, so none is claimed. (N2) the strongest competitor to "on the bridge" is a ford 16 m
beside it; against it the fixes on the road network on both banks, the mobility table and G1-2's stop at this river; the
planned path is unread, so the crossing is inferred from fixes and the model, not observed. (M3b) the pass is consistent
with the name reading, not proof of it (n = 1, sec 7). Verified: the app-log lines, the trace, the runner log and manifest,
the reports capture, the thread samples, the vendor log and C:\MAK\logs by count / name / mtime, the sim-cache listings and
the URI entry, the OSM geometry on the scorer's tiles, the vendor sysdef, featureconfig and script lines, UG52 27.1.4, the
machine's boot time and process table, this file's lines and the 5a400d3 diff. Assumed: the MSBuild source, that the sim's
Lake layer is what held INF3WPN1, that the planned path followed the bridge.
