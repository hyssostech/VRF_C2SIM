# REVIEW: GroundWaypointAltitudeMode=TerrainProfile branch (2026-09-01)

Reviewed: worktree branch worktree-agent-a51dbe56992f78330, commit 6539036 (parent
c24248f = main at the time). Read-only review; nothing on the branch was edited, no
VR-Forces launch, no C:\MAK file touched. Design doc under review:
docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md (in the worktree).

## Verdict: MERGE WITH FIXES

Fixes required before the confirming run (F1 is the blocker; F2/F3 are cheap and
protect the run's interpretation):

- F1 (HIGH) Move the DtIntersectionInformationResponseType callback registration OFF
  the default Start()/RegisterInboundCallbacks() path: register lazily on the first
  RequestTerrainProfile() call (a once-flag in Impl), or behind an explicit StartupConfig
  opt-in set by the app only when the mode is TerrainProfile. Then the confirming run's
  prereg must add a CONTROL run: new bridge + mode=Live must reproduce the R9 3/3 before
  the mode is flipped (sec 7 of the design changes two variables at once - a new native
  binary AND the mode).
- F2 (MEDIUM) Reject an ECHOED sample in TerrainVertexAuthoring.Apply: a sample whose
  altitude equals the request vertex's altitude (live + 50) within ~1 cm is the request
  point handed back, not a terrain height; treat it as invalid, and add a self-test case.
  Add the same tripwire to the prereg (sec 7 check 2).
- F3 (MEDIUM) Re-word the vertex-0 fallback reason and prereg check 2: an altitude gap
  with horizontal agreement is NOT a frame falsifier (it also fires for an unclamped or
  aggregate taskee); a frame error shows up as horizontal mismatch / "no usable sample".

## 1. Docs check (every citation re-read against the header text)

Verified verbatim (line numbers correct):
- ifRequestTerrainProfileInformation.h: class :16, setRequestId/requestId :43-44,
  partial-information comment :46-48 (quoted correctly), setSendPartialInformation :49,
  setPoints(const std::vector<DtVector>&) :52, member default comment "Default is true"
  :62, cancel class :72 / setRequestId :99.
- ifIntersectionInformationResponse.h: file comment :20 "Note that all point information
  returned is in geocentric"; DtIntersectionInformation :22, intersectionPoint :37,
  userData :77-81 (DtString), response class :105, typedefs :108-109, responseId :131-135
  with comment :130 "set to the request id of the initial request", empty-list comment
  :137-139 (note: the header's example is "in the case of LineOfSight"; the design's
  reading that a no-data point is an EMPTY inner vector is an extension of that
  sentence, not its literal scope), complete() :147-151, setResponseSetSize :155,
  intersectionPairInformation :157/:162.
- terrainProfileRequestManager.h: thread comment :24-25, terrainClosed :48, request
  callback :54, Result struct :109-117 (soilType, testPoint, terrainHeight), map<int,
  Result> :119.
- vrfMessageInterface.h: createAndDeliverMessage :62, addMessageCallback :164;
  vrfRemoteController.h: vrfMessageInterface() :228, generateRequestId :249 (unsigned
  int), backends() :295; backend.h address() :100; simulationAddress.h:18 DtSimSendToAll;
  simInterfaceMessage.h:23 DtSimInterfaceMessage : DtSimMessage;
  simInterfaceContent.h:65 pure virtual type(); noArgumentCallbackList.h:95 callback
  signature; messageTypes.h:311/312 terrain-profile request/cancel ids; createRoute
  vertices "needs to be in geocentric coordinates" (vrfRemoteController.h:1006-1007).
- docs.mak.com classref pages for DtIfRequestTerrainProfileInformation and
  DtTerrainProfileRequestManager (fetched 2026-09-01): generated from the headers, no
  frame statement. Dev Guide blocking-terrain page: quoted sentence present, terrain
  profile not mentioned.

Citation errors (cosmetic):
- Design sec 1.2 cites messageTypes.h:174/:173 for DtIntersectionInformationResponseType
  = 174 / DtRequestIntersectionInformationType = 173. The VALUES are right; the lines are
  :261 and :260.

### The request-point frame: GENUINELY UNSTATED
Neither the request header, its base class, the classref page, nor the back-end
manager header states the frame of DtIfRequestTerrainProfileInformation::setPoints.
Evidence for geocentric, all circumstantial:
1. The reply is stated geocentric (ifIntersectionInformationResponse.h:20).
2. The sibling request states "all point requests must be supplied in geocentric"
   (ifRequestIntersectionInformation.h:18).
3. NEW, not in the design doc: the vendor's own client of this request,
   DtTerrainProfileWidget (vrfGuiCommonQt/terrainProfileWidget.h), keeps every point it
   works with geocentric - "Points are in geocentric" myLinePoints :325, "The points used
   for terrain intersection" myTerrainDatabasePoints :344-345, terrainHeight(const
   DtVector&) "Returns terrain height at geocentric position" :221, and stores the replies
   as std::map<int, DtVector> myReceivedTerrainProfileInformationFor :408 - one DtVector
   per request index, which is consistent with intersectionPoint carrying the terrain
   point (see sec 3, echo blind spot, for why this matters).
Status: inferred with strong corroboration; the live run remains the proof. The design's
own runtime guards (horizontal mismatch <= 50 m, vertex-0 delta) DO catch a wrong request
frame: a request misread as geodetic/topographic returns points nowhere near the vertices,
every sample fails the horizontal check, the route falls back to Live with a WARN.

## 2. Default path (mode=Live) - unchanged at the C# level, CHANGED at the native level

C# (VrfC2SimService.cs): the two "Live" predicates became IsLiveLikeAltitudeMode(), which
is Equals("Live") || Equals("TerrainProfile") - identical truth value for "Live" and
"Fixed100". The new block (:780-816) is guarded by terrainRoute != null (null on the first
pass) and isGround && IsTerrainProfileMode() (false) - no call, no log. Create altitude,
groundWpAlt arithmetic, ROE/SetTarget order, MoveIntoFormation/PlanAndMove/fan-out/
CreateRoute order and every log line are unchanged. Additions on the Live path:
_pendingTerrain.IsEmpty test once per 50 ms tick (:307), one event subscription in the
constructor (:163). Neither issues a VRF call.

Native (VrfFacade.cpp:397-400 and :458-460): Start() and RegisterInboundCallbacks() now
call vrfMessageInterface()->addMessageCallback(DtIntersectionInformationResponseType, ...)
UNCONDITIONALLY, for every consumer of the DLL (all 9). This is precisely the pattern the
2026-07-19 incident record forbids: commit 5d14eda ("REVERT the native bridge change - it
broke object creation") closes with "WHEN THIS IS REDONE: additive only, opt-in only, and
it MUST NOT touch the default Start() path. Verify against this exact four-run table before
trusting any new build", and HANDOFF_2026-07-19.md sec 5 repeats it. The design doc cites
that handoff (sec 6) only for the stale-DLL trap and does not mention the rule - no dissent
line, no justification. Whether a by-type callback on an otherwise unused message type can
disturb creation is unknown (the 07-19 failure was attributed to callback registration as
"PRIME SUSPECT", not proven); the tripwire exists so that this is not re-argued per change.
-> F1. The lazy-registration variant keeps Start() byte-identical for every consumer and
needs no config plumbing.

Consequence for the prereg (design sec 7): as written it flips the mode on a NEW bridge
binary in one step against the R9 Live baseline (old bridge). A control row (new bridge,
mode=Live, 3/3) is required first, per the 07-19 rule.

## 3. Correctness of the new path

Verified:
- Id generation: controller counter (generateRequestId), request setRequestId((int)id),
  reply responseId() unsigned long -> unsigned int -> C# uint; distinct per request, so
  the three R9 ground tasks cannot cross-correlate on id. Replies are keyed by id in a
  ConcurrentDictionary; the reply handler and the expiry sweep both TryRemove - exactly one
  wins.
- Threading: the reply callback fires inside exConn->drainInput() in VrfFacade::Tick(),
  i.e. on the app's tick thread; the handler only enqueues to _tickActions, drained at the
  top of the next iteration. ExpireTerrainRequests runs on the same thread. No cross-thread
  mutation of app state. Send is posted (sendImmediately=false) and goes out on the
  following Tick, so the reply cannot precede the _pendingTerrain insert.
- Timeout: deadline = now + max(1, TerrainProfileTimeoutSeconds); swept every tick;
  continuation(null) -> Apply returns Mode.Fallback with ALL indices kept Live -> the
  re-entry dispatches exactly the Live route (same list contents; point 0 lat/lon is
  re-read but the unit was never tasked). Sequencer impact: the deferral only delays
  NotifyDispatched by <= 10 s against a 600 s predecessor window.
- Stale reply: unknown id -> LogDebug + drop. A late reply after expiry is dropped; a
  second back end's reply to a consumed id is dropped.
- No intersection: EMPTY inner vector -> valid=false -> that vertex keeps Live (Partial).
- Vertex-0 sanity fail -> Mode.Fallback with allIndices -> the WHOLE route is Live (claim
  holds). A mixed route is produced ONLY by Mode.Partial (some vertices no data /
  horizontally displaced) - by design, and each Live vertex is what Live authors today.
- 1-vertex route: not reachable (task.Points.Count == 0 returns before the branch; with
  points the route has >= 2 vertices). Apply handles it anyway (self-test 9).
- Frame for createRoute: vertices geocentric (vrfRemoteController.h:1006-1007). Live
  path: entity geocentric -> DtGeodeticCoord (ellipsoid alt) + 50 -> toGeocentric. Terrain
  path: reply geocentric -> DtGeodeticCoord alt + 10 -> toGeocentric. Same conversion
  functions (VrfFacade.cpp:95-100, :314-318, :816-820); same datum.
- Non-ground units, ESCRT with a resolvable target, in-place ATTACK/BREACH, aggregate
  MoveIntoFormation/PlanAndMove: the request is never issued or the authored final point
  is used as before.

Findings:
- F2 (MEDIUM) Echo blind spot. The back end's Result keeps testPoint and terrainHeight as
  SEPARATE fields (terrainProfileRequestManager.h:114-116); how it packs them into
  DtIntersectionInformation is not documented. If intersectionPoint were the request point
  handed back unchanged, the app would author every vertex at live+50+10, log Mode.Terrain
  ("all vertices authored from terrain"), and pass BOTH runtime guards (vertex-0 delta =
  50 m < 100; horizontal mismatch 0). The confirming run would read as a success. The
  widget evidence (sec 1, item 3) makes this unlikely, but the check is one line:
  |sample.TerrainAltMeters - liveVertices[i].AltMeters| < 0.01 -> invalid ("echoed request
  point"). Add the self-test case and the prereg tripwire.
- F3 (MEDIUM) Vertex-0 reason overclaims. TerrainVertexAuthoring.cs:18-21 and :54-58 name the
  "request/reply frame" as the cause of any > 100 m altitude gap, and design sec 7 check 2
  says such a fallback "falsifies the geocentric-request inference -> stop". A ground unit
  not yet clamped (born at CreateAltitudeSafeMslMeters) or an aggregate whose published
  altitude is not the surface produces the same gap with perfect horizontal agreement; a
  real frame error produces horizontal mismatch. The run's decision rule must separate the
  two signals or it will stop on a false falsifier.
- LOW: complete() is read into the event (Complete) but never consulted by the app. We
  request complete replies; if the back end ignores the flag and sends partials, the first
  partial is consumed (Partial WARN) and the completing message is dropped as stale.
  Degrades to Live per vertex - acceptable - but log Complete=false in the WARN so the run
  can tell the two apart.
- LOW: positional index fallback. If a reply omits entries instead of sending empty lists
  and userData is not set for complete replies, samples shift onto the wrong vertex; the
  50 m horizontal check catches it unless two route vertices are within 50 m of each other
  (bounded error, adjacent terrain).
- LOW: re-entry re-runs the verb classification and the ATTACK/BREACH/ESCRT resolution
  logs - duplicate WARN/INFO lines per ground task in TerrainProfile mode. Accepted in the
  design; noted for the log reader.
- INFO: another sender's DtIfIntersectionInformationResponse (a GUI terrain-profile dialog
  on the same session) with a numerically equal request id would be consumed as ours; the
  horizontal check rejects its samples -> Live fallback WARN, and our real reply is then
  dropped. Not silent; no GUI in the headless configuration.
- INFO: callbacks are never removed (pre-existing pattern for every facade callback);
  Stop() deletes the controller on the owning path, so no dangling pointer there. On the
  adopt path (StartAdopting) RegisterInboundCallbacks() is caller-driven as before.

## 4. Build/deploy surface - consumer list VERIFIED (9)

grep of *.csproj for the HintPath src\VrfBridge\build\Release\VrfBridge.dll in the main
checkout: src/SmokeTest, src/VrfC2SimApp, tools/CreateOne, tools/CreateTaskAgg,
tools/ResetVrf, tools/RtiProbe, tools/RunSim, tools/SetSimRate, tools/WatchVrf - the
design's nine. bridge-spikes/VrfBridgeSpike/SpikeRunner references VrfBridge.Spike.dll
(its own build), not a consumer. No script copies the DLL elsewhere (grep of *.ps1/*.cmd).
All 10 present copies on main (9 bins + build/Release) hash a48abe6c... = the A48ABE6C
build in HANDOFF_2026-07-19 sec 4; the native source at main is byte-identical to the
source that build came from (git diff 50a5c0c..c24248f -- src/VrfFacade src/VrfBridge is
empty). The worktree build is 5c6f5517... (different, as it should be). The design's deploy
step 4 (rebuild every consumer) plus step 5 (one hash) is the right procedure; add: run the
07-19 four-run-table control before the mode flip (F1).

## 5. Tests - run by the reviewer from the worktree bin (MAK bin64 dirs on PATH)

--terrain-selftest exit 0 (21 checks, PASS); --translator/--report/--sequencer/--verb/
--destack/--fanout self-tests all exit 0. The tested exe was built 18:36:32 from source
last edited 18:36:15, committed 18:38:40 with a clean tree.

Coverage of the asked-for behaviours, at the PURE-FUNCTION level only:
- timeout -> fallback: YES (case 2, null samples -> Fallback, Live altitudes)
- no-intersection -> fallback (per vertex): YES (case 4 Partial, case 10 all invalid)
- sanity fail -> WHOLE route fallback: YES (case 5, all three altitudes == Live)
- clearance applied: YES (case 1: 1098 -> 1108 etc.)
- stale reply ignored: NO - lives in OnVrfTerrainProfile (service), untested offline
Gaps:
- No service-level test of the lifecycle: pending insert, expiry sweep, stale drop, the
  continuation re-entering ExecuteTaskOnTick with terrainRoute, exactly-once
  MarkDispatched. (The self-test template has no bridge; a seam would be needed.)
- No echo case (F2).
- No case for "vertex 0 invalid, others valid" - the frame check is skipped exactly then
  and the route goes Partial with an unverified frame (design-conformant, untested).
- No native test of the trampoline (userData parse, empty inner vector, type guard) -
  the live run is the only check.
- Not mutation-checked by the reviewer (branch is read-only); the assertions compare
  concrete values, so they are not vacuous.

## 6. Hygiene

- Line endings: every touched file is CRLF in the working tree; blobs are LF at both
  c24248f and 6539036 (core.autocrlf=true) - no whole-file churn; git diff --check clean.
- ASCII: rg -n -P "[^\x00-\x7F]" flagged a known-dirty control and matched nothing in the
  9 touched files.
- Naming/comment density match the surrounding code (trampoline/thunk/Raise*/self-test
  patterns followed; comments cite header lines like the neighbours). No Console.WriteLine
  outside the self-test, no TODO/debug leftovers in the diff.

## 7. Adversarial review note

Competing hypothesis to the design's frame claim: the request expects geodetic
(lat/lon/alt) DtVectors like some VR-Link topographic APIs. Not falsified by any document
(genuinely unstated); weakened by the vendor widget's all-geocentric point handling; and
the runtime horizontal check turns a wrong guess into a loud Live fallback rather than a
bad route. Competing hypothesis to "Terrain mode worked" in the coming run: the echoed
request point (F2) - currently NOT distinguishable by the app or by the prereg as written.
Unexplained symptom: none observed (no live run in this review).
