# DESIGN: route vertices authored from the back-end's own terrain (2026-09-01)

Handoff NEXT item 1 (docs/HANDOFF_2026-09-01_R9_COMPLETE.md): add a THIRD
GroundWaypointAltitudeMode, "TerrainProfile", that asks the simulating back end for
the terrain height under each ground route vertex (DtIfRequestTerrainProfileInformation)
and authors the vertex at terrain + TerrainClearanceMeters. AS FIRST WRITTEN (2026-09-01):
"Live" (live entity altitude + 50 m) stays THE DEFAULT and its code path is byte-identical;
"Fixed100" is untouched. SUPERSEDED 2026-09-02: after Rows 2c and 2cR both passed,
"TerrainProfile" BECAME THE DEFAULT - see sec 7 DEFAULT FLIP. "Live" remains available by
config (Vrf__GroundWaypointAltitudeMode=Live) and its code path is still byte-identical;
"Fixed100" is still untouched.
Written docs-first, before any code (STANDING RULE). ASCII only. Offline gates only in
this pass - the confirming live run is sec 7.

Review 2026-09-01 (docs/experiments/REVIEW_TERRAIN_PROFILE_BRANCH_2026-09-01.md on main):
MERGE WITH FIXES. F1 (lazy callback registration + control row), F2 (echo guard), F3
(vertex-0 wording) and the LOW items are applied in this document's second revision; the
"(review Fn)" tags below mark what changed.

## 0. Answers the brief asked for (verified vs. assumed)

| Question | Answer | Status |
|---|---|---|
| Frame/datum of the REPLY points | GEOCENTRIC DtVector (earth-centred metres); converted to geodetic with DtGeodeticCoord::setGeocentric, so the height is metres above the WGS-84 ELLIPSOID - the same datum every other altitude in this app uses (TryGetEntityGeodetic, toGeocentric) | VERIFIED - header comment, sec 1.2 |
| Frame of the REQUEST points | Not stated in the request header. Geocentric by protocol convention: the sibling DtIfRequestIntersectionInformation says "all point requests must be supplied in geocentric", the back-end manager stores them as plain DtVector, and the app's own controller calls (createRoute, setLocation) take geocentric. The reviewer adds the vendor's own client, DtTerrainProfileWidget (vrfGuiCommonQt/terrainProfileWidget.h:221, :325, :344-345, :408), which keeps every request/reply point geocentric. A WRONG request frame shows up live as HORIZONTAL mismatch (samples nowhere near the vertices -> "no usable sample" fallback), NOT as a vertical gap (review F3) | INFERRED, strongly corroborated - verify live (sec 7 check 2) |
| Async model + correlation | Fire-and-forget sim-interface message carrying an int requestId (controller->generateRequestId()); the reply is a DtIfIntersectionInformationResponse delivered to a callback registered by message TYPE on the controller's DtVrfMessageInterface; correlate on responseId() == requestId, per point on userData() == request point index; complete() marks the last (or only) reply | VERIFIED - sec 1.1-1.4 |
| Unpaged-terrain behaviour | Non-blocking terrain queries on unpaged terrain return immediately with dataAvailable=false and "no terrain intersections" (Dev Guide, contract C1). The terrain-profile manager runs its requests in its own thread (header), so it MAY block/page instead - undocumented. Either way the app must not depend on it: a point with no intersection arrives as an EMPTY response set; a request that never answers is covered by the app timeout. Both fall back to the Live altitude for that vertex | VERIFIED (Dev Guide) + handled defensively |
| Oracle parity | NONE. The frozen C++ oracle never queries terrain height: grep of c2simVRFinterfacev2.36 for TerrainProfile / IntersectionInformation returns 0 hits; ground vertices are `loc->elevation = "100"` (C2SIMinterface.cpp:2191); the only AGL use is setPointAltitudeAgl(waypoint, 0) for evacuate waypoints (:1148-1149). This mode is a deliberate departure from the oracle - opt-in and off by default when written 2026-09-01; THE DEFAULT since 2026-09-02 (sec 7 DEFAULT FLIP), with "Live" and "Fixed100" still reachable by config | VERIFIED |

## 1. Documentation read (citations)

All paths are read-only vendor headers under C:\MAK (never modified).

### 1.1 The request: vrfmsgs/ifRequestTerrainProfileInformation.h
- Class DtIfRequestTerrainProfileInformation : DtSimInterfaceContent (:16).
- setRequestId(int) / requestId() (:43-44).
- setSendPartialInformation(bool) (:49), header comment :46-48: "When partial
  information is requested, a series of responses will be sent containing the
  information that has been discovered since the last check. The response will be in
  the form of a DtIfIntersectionInformationResponse where the user data of each
  information response is the index of the terrain profile request satisfied with the
  response". Default true (back-end struct default sendPartial=true,
  terrainProfileRequestManager.h:91). We send FALSE: one complete reply per request.
- setPoints(const std::vector<DtVector>&) (:52). Frame NOT stated (see sec 0).
- Type id: vrfmsgs/messageTypes.h:311 DtTerrainProfileInformationRequestType =
  MAX_USER_INTERFACE_MESSAGE_TYPE + 13; cancel = +14 (:312, class
  DtIfCancelTerrainProfileInformationRequest at ifRequestTerrainProfileInformation.h:72,
  setRequestId :99). Cancel is not used: a late reply after the app timeout is simply
  dropped (no pending entry matches).

### 1.2 The reply: vrfmsgs/ifIntersectionInformationResponse.h
- File comment :20: "Note that all point information returned is in geocentric".
- DtIntersectionInformation (:22): intersectionPoint() (:37), soilType(), userData()
  (:78, DtString), startPoint/endPoint/entityIntersected members (:93-101).
- DtIfIntersectionInformationResponse : DtSimInterfaceContent (:105); typedefs
  IntersectionInformation = vector<DtIntersectionInformation>,
  IntersectionPairInformation = vector<IntersectionInformation> (:108-109).
- responseId() (:132) "set to the request id of the initial request" (:129).
- Comment :136-138: for each request pair "there will be one set of response for each
  pair. If there is no intersection ... then an empty list will be returned at the
  given response index" - i.e. a vertex with no terrain data is an EMPTY inner vector.
- complete() (:149): "Set to false to indicate this is a partial message and there is
  more information to come. Default is true" (:148). setResponseSetSize (:155).
- intersectionPairInformation() (:157/:162).
- Type id: messageTypes.h:261 DtIntersectionInformationResponseType = 174 (request
  sibling DtRequestIntersectionInformationType = 173 at :260; its header
  ifRequestIntersectionInformation.h states the geocentric input convention and has a
  TerrainHeight flag - it is the general-purpose form; the terrain-profile request is
  the purpose-built per-point form and is what the handoff names).

### 1.3 The back-end handler: vrfobjcore/terrainProfileRequestManager.h
- Header comment :24-25: "will handle requests (in a thread) for terrain profile
  requests and either send them back when they are all done or when results become
  available (as per the request)".
- Registered as terrainProfileInformationRequestCallback (:54) - a message callback,
  so any sender on the session can issue one (the remote controller is such a sender).
- Result struct per point (:109-117): soilType, testPoint (DtVector), terrainHeight
  (double). Results are keyed by int point index (:119) - this is what becomes each
  DtIntersectionInformation's userData. Requests are cleared on terrainClosed() (:48) -
  a request in flight across a scenario close never answers -> app timeout.

### 1.4 Sending and receiving from the remote controller
- vrfcontrol/vrfRemoteController.h: vrfMessageInterface() (:228), generateRequestId()
  (:249, unsigned int), backends() (:295, DtList of DtBackend*; DtBackend::address()
  at vrfutil/backend.h:100).
- vrfMsgTransport/vrfMessageInterface.h: createAndDeliverMessage(const
  DtSimulationAddress& recipient, const DtSimInterfaceContent& content, bool
  overrideTimestampOrder=true, bool sendImmediately=false) (:62-65);
  addMessageCallback(int type, DtMessageCallbackFcn fcn, void* usrData) (:164-165);
  sessionId semantics (:83-99; the facade already sets it in Start()).
- vrfmsgs/messageExecutive.h:21: DtMessageCallbackFcn =
  DtCallbackList<DtSimMessage*>::DtCallbackFunctionType, which is
  `void (*)(DtSimMessage*, void*)` (readerWriter/noArgumentCallbackList.h:95).
- vrfmsgs/simInterfaceMessage.h: DtSimInterfaceMessage : DtSimMessage (:23),
  interfaceContent() (:44); vrfExtProtocol/simInterfaceContent.h:65 `virtual int
  type() const = 0` (used to double-check the content type in the trampoline).
- Recipient: DtSimSendToAll = DtSimulationAddress(0xFFFF, 0xFFFF)
  (vrlink5.8/include/vlpi/simulationAddress.h:18) - the address the facade already
  uses for every task/set/reorganize message; every back end on the session answers.
  With more than one back end the app keeps the FIRST complete reply and ignores the
  rest (the pending entry is removed on first use).

### 1.5 Developer's Guide (public, docs.mak.com; read 2026-09-01)
- https://docs.mak.com/api/vrforces5.2/classref/vrf_object_operationsand_blocking_terrain_calls.html
  and https://docs.mak.com/api/vrforces5.2/classref/vrf_queryingthe_terrain_interface.html
  - the C1-C6 contract recorded in docs/RESEARCH_MECHANISMS_2026-09-01.md sec 9:
  non-blocking (dataAvailable) form "will return immediately and DataAvailable will
  be set to false" on unpaged terrain and "no terrain intersections will be returned";
  clampToGround/place have requireAllData; object creation and Set Location are
  QUEUED until terrain data is available. Named API: intersect(), preloadTerrainAreas(),
  terrainHeightAboveSeaLevel(), setAssertOnBlockingTerrainCalls().
- Class pages https://docs.mak.com/api/vrforces5.2/classref/class_dt_if_request_terrain_profile_information.html
  and .../class_dt_if_intersection_information_response.html: generated from the
  same headers, add nothing (no frame statement beyond "geocentric" on the response).
- Remote-control chapter (vrf_usingthe_remote_control_a_p_i.html, 5.2 and 4.10): how
  the controller sends DtIfRequest* is through the DtVrfMessageInterface; no
  narrative page covers the terrain-profile request specifically (5.x dropped several
  narrative chapters - handoff item 3b).
- Route-vertex frames + the underground warning: Users Guide
  vrf_setRouteVertexAltitude.htm (contract C5): vertex altitude is the AUTHOR's
  responsibility; above-sea-level vertices can be underground. This design replaces the
  author's approximation (live + 50) with the simulator's own terrain height.

## 2. Existing pattern this follows (project code, read this pass)
- Facade trampolines: static free functions with usr = VrfFacade*, null-guarded string
  extraction, std::function event (VrfFacade.cpp:236-273 availableFormations /
  objectConsoleMessage). The EXISTING callbacks are registered in Start() (:333-346) AND
  RegisterInboundCallbacks() (:389-404) - the new one deliberately is not, see next item.
- The 2026-07-19 native-change rule (commit 5d14eda "REVERT the native bridge change - it
  broke object creation"; docs/HANDOFF_2026-07-19.md sec 5): "WHEN THIS IS REDONE: additive
  only, opt-in only, and it MUST NOT touch the default Start() path. Verify against this
  exact four-run table before trusting any new build". Callback registration in Start()
  was the PRIME SUSPECT of that failure (not proven). This design therefore registers its
  callback lazily (sec 3.1) and the confirming run carries a control row (sec 7). No
  dissent: the rule costs one bool and one control run.
- Bridge: managed EventArgs + `event EventHandler<...>` + Raise* internal + namespace-
  scope thunk holding msclr::gcroot + WireCallbacks (VrfBridge.cpp:136-176, 385-389,
  496-526).
- App: `_tickActions` ConcurrentQueue<Action> drained on the tick thread before
  `_bridge.Tick()` every 50 ms (VrfC2SimService.cs:118, 289-302); ExecuteTaskOnTick
  builds routeGeo from the live location + task points (:723-768) then dispatches
  MoveToLocation / CreateRoute (:861-937).
- Offline self-tests: `--*-selftest` switches in Program.cs (no test project);
  DeStackSelfTest.cs is the template (Check(ref failures, ...), returns failure count,
  uses the bridge's Geodetic value type offline).

## 3. Design

### 3.1 Facade (C++, src/VrfFacade)
POD reply (VrfFacade.h):
```
struct TerrainSample { int index; bool valid; Geodetic point; }; // point.altMeters = terrain height (ellipsoid)
struct TerrainProfile { unsigned int requestId; bool complete; std::vector<TerrainSample> samples; };
```
API: `unsigned int RequestTerrainProfile(const std::vector<Geodetic>& points);` returns
the request id (0 = not sent). Event: `std::function<void(const TerrainProfile&)>
OnTerrainProfile;`.
Implementation: generateRequestId(); DtIfRequestTerrainProfileInformation on the stack
(setRequestId, setSendPartialInformation(false), setPoints(geocentric via
toGeocentric)); `vrfMessageInterface()->createAndDeliverMessage(DtSimSendToAll, req)`.
Trampoline `terrainProfileTrampoline(DtSimMessage*, void*)`: static_cast to
DtSimInterfaceMessage (the callback is registered by content type, so the message is a
sim-interface message), check `content->type() == DtIntersectionInformationResponseType`,
then for each inner vector i: empty -> sample{i,false}; else userData -> index (falls
back to i when unparsable), intersectionPoint -> geodetic via DtGeodeticCoord (same
conversion as TryGetEntityGeodetic). Registered LAZILY (review F1): the first
RequestTerrainProfile() on a controller calls
`vrfMessageInterface()->addMessageCallback(DtIntersectionInformationResponseType,
terrainProfileTrampoline, this)` once (Impl::terrainCallbackRegistered, reset in Stop()).
Start(), StartAdopting(), RegisterInboundCallbacks() and Tick() are byte-identical to
main (c24248f) - a consumer that never requests terrain (8 of the 9, and this app in
Live mode) runs the pre-feature native path; only Stop() gains the flag reset.
NOTE: this callback also fires for replies to the
general DtIfRequestIntersectionInformation (same response type) - nothing else in this
process sends those, and the app drops unmatched responseIds anyway.

### 3.2 Bridge (C++/CLI, src/VrfBridge/VrfBridge.cpp)
`public value struct TerrainHeightSample { int Index; bool Valid; double LatDeg, LonDeg,
TerrainAltMeters; }`, `TerrainProfileEventArgs { uint RequestId; bool Complete;
List<TerrainHeightSample>^ Samples; }`, `event EventHandler<TerrainProfileEventArgs^>^
TerrainProfile`, `unsigned int RequestTerrainProfile(IEnumerable<Geodetic>^ points)`,
RaiseTerrainProfile internal, TerrainProfileThunk, wired in WireCallbacks.

### 3.3 App (C#, src/VrfC2SimApp)
Config (VrfSettings.cs): GroundWaypointAltitudeMode gains the value "TerrainProfile";
new `TerrainClearanceMeters` (default 10.0 - the vertex is placed slightly ABOVE the
reported terrain so a clamp only ever drops it; 50 m stays the Live approximation's
margin because Live has no terrain knowledge) and `TerrainProfileTimeoutSeconds`
(default 10 - the reply is one message round trip on a warm back end; the terrain
manager thread may page tiles, so allow seconds, not ticks).

Flow (VrfC2SimService.cs), all on the tick thread:
1. Create-altitude: `liveMode` becomes true for "Live" OR "TerrainProfile" (a ground
   unit is still born at CreateAltitudeSafeMslMeters and clamped by VRF; the terrain
   query is only for ROUTE vertices).
   *** The MSL birth is DEPRECATED (wrong frame - an AGL setAltitude places a unit on the
   ground in one call, verified 2026-09-04; docs/VRF_ALTITUDE_FRAMES.md). THIS DOC'S OWN
   SUBJECT IS UNAFFECTED: route vertices have NO AGL frame in the API, so the terrain query
   below is correct and stays. Only the CREATE half is retiring. ***
2. ExecuteTaskOnTick(task, unit, terrainRoute = null): groundWpAlt is computed as
   today for "Live" or "TerrainProfile" (live + GroundWaypointLiveClearanceMeters),
   100 m otherwise - so Live and Fixed100 produce the same numbers as before. After
   routeGeo is complete (point 0 + task points), IF mode is TerrainProfile AND isGround
   AND terrainRoute == null: `id = _bridge.RequestTerrainProfile(routeGeo)`, store
   `_pendingTerrain[id] = (deadline, live vertices, entity live altitude, continuation)`
   and RETURN before ROE/SetTarget/dispatch. Nothing is marked dispatched yet, so a
   timeout/fallback re-entry leaves all bookkeeping (MarkDispatched, routeQueue,
   fan-out, DeferEngage) exactly once, at dispatch time.
3. OnVrfTerrainProfile (bridge event, VRF tick thread): look up
   `_pendingTerrain[e.RequestId]`; if absent (late/unknown) -> debug log, drop. Else
   remove it and `_tickActions.Enqueue(() => continuation(e.Samples))`. A reply with
   Complete=false for a pending id is logged and LEFT pending (review LOW): we asked for
   complete replies, and consuming a partial would make the completing message stale;
   the complete message or the timeout finishes the request.
4. TickLoop: once per iteration, expire pending entries past their deadline ->
   WARN + `continuation(null)`.
5. continuation(samples): `TerrainVertexAuthoring.Apply(liveVertices, samples,
   clearance, entityAltMeters, ...)` -> authored vertices + a diagnostic; log INFO
   (terrain heights used) or WARN (fallback: which indices, why); then
   `ExecuteTaskOnTick(task, unit, authored)` - the re-entry takes the authored route in
   place of the freshly computed one (point 0's lat/lon is re-read live but the unit
   has not been tasked, so it is stationary) and proceeds to the normal dispatch. The
   re-entry re-runs the verb classification and the ATTACK/BREACH/ESCRT resolution logs
   (idempotent reads), so in TerrainProfile mode each ground task prints those lines
   twice; accepted for the smallest diff on the default path (review LOW) - the log
   reader keys on the "terrain profile request N sent" / "Terrain profile N" pair.

Decision rules (pure, in TerrainVertexAuthoring.Apply; unit-tested offline):
- samples == null (timeout) or empty -> every vertex keeps its Live altitude
  ("fallback: no reply").
- Per-sample validity: Valid flag AND the index is in range AND horizontal distance
  between the sample's lat/lon and the vertex's lat/lon <= MaxHorizontalMismatchMeters
  (50 m) AND not an echo. Invalid -> that vertex keeps Live ("partial: vertices i,j
  kept Live"). The horizontal test is the FRAME check: a request or reply in the wrong
  frame puts every sample far from its vertex -> Fallback "no usable sample for any
  vertex" (self-test 14).
- Echo guard (review F2): |sample.TerrainAltMeters - liveVertex.AltMeters| < 1 cm means
  the back end handed the REQUEST point back (live + 50), not a terrain intersection;
  the sample is rejected and the Reason counts the echoes ("N echoed request point(s) at
  vertex i,j rejected"). Without it an echo would author live + 60 and read as success.
- Vertex-0 diagnostic (review F3 - was a fallback trigger, now a NOTE): if sample 0 is
  usable and |terrainAlt0 - entityAltMeters| > 100 m the Result carries
  Note = "taskee altitude not terrain-clamped: live X m vs terrain Y m under vertex 0
  (gap N m) - authoring from terrain anyway", logged INFO. Rationale: horizontal
  agreement with a vertical gap is what an UNCLAMPED taskee (born at
  CreateAltitudeSafeMslMeters, or an aggregate whose published altitude is not the
  surface) looks like, and in exactly that case terrain authoring is the point of the
  mode; a frame error cannot produce this signature (it fails the horizontal test).
  So the gap never stops the run and never names the frame.
- Valid -> AltMeters = terrainAlt + clearance. Lat/lon are never changed.
- Result Mode: Terrain (all replaced) / Partial / Fallback, with KeptLive + Reason (+
  Note) for the log lines.
- Positional-index fallback (review LOW, documented not guarded): when userData is
  missing the trampoline uses the pair's position i. If a back end OMITTED no-data
  entries instead of sending empty inner vectors, later samples would shift onto earlier
  vertices; the 50 m horizontal test rejects the shifted ones unless two route vertices
  lie within 50 m of each other, in which case the error is bounded by the terrain
  difference between adjacent vertices. The R9 routes have no such pair.

Non-ground units, routes with zero points, ESCRT (dispatched before the route section),
ATTACK/BREACH with no points: unchanged - they never reach the request.

### 3.4 What is NOT changed
Live and Fixed100 code paths (create altitude, groundWpAlt arithmetic, dispatch order,
log lines) - the only touch on the shared lines is the mode predicate widening
("Live" -> "Live" or "TerrainProfile") and the new `terrainRoute` optional parameter,
which is null on the default path. The frozen C++ oracle is untouched. No deployed
binary, no C:\MAK file, no launch.
NOTE 2026-09-02: "the default path" in this section means the pre-flip default, "Live".
Those code paths are unchanged by the DEFAULT FLIP (sec 7) - the flip changes ONE literal
in VrfSettings.cs, not any code path; Live is still byte-identical and still selectable
with Vrf__GroundWaypointAltitudeMode=Live.

## 4. Files changed (this pass)
- src/VrfFacade/VrfFacade.h, src/VrfFacade/VrfFacade.cpp - POD types, request, trampoline, event.
- src/VrfBridge/VrfBridge.cpp - managed mirrors, RequestTerrainProfile, event, thunk.
- src/VrfC2SimApp/VrfSettings.cs - mode value + TerrainClearanceMeters + TerrainProfileTimeoutSeconds.
- src/VrfC2SimApp/TerrainVertexAuthoring.cs (new) - pure decision.
- src/VrfC2SimApp/TerrainSelfTest.cs (new) + Program.cs `--terrain-selftest`.
- src/VrfC2SimApp/VrfC2SimService.cs - pending-request table, event handler, tick expiry, mode wiring.
- this document.

## 5. Offline gates (results recorded in sec 8 when run)
1. MSBuild /t:Rebuild src/VrfBridge/VrfBridge.vcxproj Release x64 (output ONLY to
   src/VrfBridge/build/Release in the worktree - the vcxproj has no post-build copy).
2. dotnet build src/VrfC2SimApp -c Release.
3. All existing self-tests + the new one, run from the worktree's bin with the MAK bin
   dirs on PATH (the bridge assembly loads the native MAK DLLs; no federation is
   joined by a self-test).
4. ASCII: `rg -n -P "[^\x00-\x7F]"` on every touched file, after proving the check on
   a known non-ASCII file.
5. `git diff` of VrfC2SimService.cs shows the Live/Fixed100 lines unchanged except the
   predicate widening + optional parameter.
6. (review F1) Start(), StartAdopting(), RegisterInboundCallbacks() and Tick() in
   VrfFacade.cpp are byte-identical to c24248f (function-body comparison script).

## 6. Deploy steps for the confirming run (NOT done in this pass)
The nine csproj consumers reference src\VrfBridge\build\Release\VrfBridge.dll by
HintPath and copy it into their own bin output at build time - "redeploy all copies"
means REBUILDING every consumer after the bridge rebuild (a stale bin copy is the
false-green trap, docs/HANDOFF_2026-07-19.md sec 4/5):
1. Merge the worktree branch to main (supervisor decision), then on main:
2. Back up the current binaries: copy src/VrfBridge/build/Release/VrfBridge.dll (+
   Ijwhost.dll) to a dated .bak alongside; record its SHA-256.
3. MSBuild /t:Rebuild src/VrfBridge/VrfBridge.vcxproj /p:Configuration=Release
   /p:Platform=x64 (ALWAYS /t:Rebuild - a plain build has false-greened before).
4. dotnet build -c Release each consumer: src/SmokeTest, src/VrfC2SimApp,
   tools/CreateOne, tools/CreateTaskAgg, tools/ResetVrf, tools/RtiProbe, tools/RunSim,
   tools/SetSimRate, tools/WatchVrf (the bin copies are the deployed copies).
5. Confirm ONE hash: the SHA-256 of VrfBridge.dll in src/VrfBridge/build/Release equals
   the copy in src/VrfC2SimApp/bin/Release/net*/ (and spot-check tools/WatchVrf).
6. No file under C:\MAK changes for this feature.

DEPLOY RECORD (2026-09-02 01:00Z, main @ e1fdbbd = merge of 066f3d2; executor, not the
author): steps 1-6 done exactly as written. Backup: src/VrfBridge/build/Release/
bak-20260902-a48abe6c/ (VrfBridge.dll A48ABE6CBC6EA8E9B6B391B4FCBEBA2E0A5D7DE356A50475D6F77AB431FE766A,
839680 B, 2026-07-19 08:01:27; Ijwhost.dll 2DCC3B73...0B7E = .NET 10.0.8 host; VrfBridge.pdb
E8FEDA10...). Before the rebuild all 10 copies (9 consumer bins + build/Release) hashed
A48ABE6C. MSBuild 18 Community amd64 /t:Rebuild Release x64 from PowerShell: exit 0, 0
warnings -> VrfBridge.dll 28E993FE33032505A999E508877832459450E0568E7E25FFD72BC80D59257FD5
(867840 B). SIDE EFFECT recorded, not chosen: MSBuild also refreshed Ijwhost.dll from the
installed SDK (10.0.302): 2DCC3B73 (10.0.8 host, 137040 B) -> 38255036...5FD2 (10.0.10 host,
137000 B); every consumer's `None Include Ijwhost.dll` PreserveNewest picked it up. It is part
of "the new native binary" that Row 1 controls for. dotnet build -c Release of all 9
consumers from MAIN (no worktree-sdk.targets): 9 x "Build succeeded", 0 errors (the app's 6
pre-existing warnings). ONE-HASH PROOF: 10/10 VrfBridge.dll copies = 28E993FE..., 10/10
Ijwhost.dll = 38255036... All 7 app self-tests exit 0 from the main bin (terrain 29 checks);
WatchVrf --capabilities exit 0 (stop-file advertised), --con-selftest exit 0; ListenReports
--capabilities stop-file. Function bodies Start/StartAdopting/RegisterInboundCallbacks/Tick
byte-identical pre- vs post-merge (71/13/16/6 lines); Stop() +1 line. No C:\MAK write.

DEPLOY RECORD 2 (2026-09-02 10:43Z, main @ 8e14cd1 "Terrain reply: read every entry of
every response set; log reply shape at Info"; executor, not the author). SECOND deploy of
this design, for Row 2c. Steps 2-6 repeated exactly as written; step 1 (merge) not needed -
8e14cd1 was already on main, only the binary was stale. Pre-flight: main, working tree clean
apart from untracked .claude/ and the .code-workspace; no vrfSim*/vrfGui/WatchVrf/
ListenReports/VrfC2SimApp process; RTI trio resident (rtiAssistant 41336 / rtiexec 224608 /
rtiForwarder 76620); docker stp-server Up 18 h (healthy) + c2sim_server4.8.4.9 Up 18 h; no
Vrf__* env var. Backup: src/VrfBridge/build/Release/bak-20260902-28e993fe/ (VrfBridge.dll
28E993FE33032505A999E508877832459450E0568E7E25FFD72BC80D59257FD5, 867840 B, 2026-09-02
00:56:24Z; Ijwhost.dll 382550362C68297E253EDF796173B8DB8C43709D902E88C94E48BE7D1D435FD2,
137000 B; VrfBridge.pdb 281DC6CC...4E97, 12529664 B). Before the rebuild all 10 copies (9
consumer bins + build/Release) hashed 28E993FE - verified by enumeration, worktrees and the
bak dirs excluded. MSBuild 18.8.2.30814 Community amd64 (vswhere -find MSBuild\**\Bin\amd64\
MSBuild.exe under C:\Program Files\Microsoft Visual Studio\18\Community) /t:Rebuild
/p:Configuration=Release /p:Platform=x64 from PowerShell: exit 0, 0 warnings (the usual
harmless "Unknown compiler version" from a vendor header and the MSIL incremental-link
notice) -> VrfBridge.dll A7504441F421B668D10F5AFD8B4FD71110002D13FE6ABAE0DB576C7C209236F5
(868352 B, +512 B over 28E993FE). NO Ijwhost side effect this time: it stayed
38255036...5FD2 / 137000 B / 2026-06-27 (the SDK that refreshed it on 2026-09-02 01:00Z is
still the installed one). dotnet build -c Release of all 9 consumers: 9 x "Build succeeded",
9 x exit 0, 0 errors; 0 warnings except the app's 6 pre-existing. ONE-HASH PROOF: 10/10
VrfBridge.dll copies = A7504441... (single distinct hash over the 10 enumerated paths),
10/10 Ijwhost.dll = 38255036... All 7 app self-tests from the main bin with the MAK bin64
dirs on PATH: --translator-selftest exit 0 (SELF-TEST PASSED), --report-selftest exit 0,
--sequencer-selftest exit 0, --verb-selftest exit 0, --destack-selftest exit 0,
--fanout-selftest exit 0, --terrain-selftest exit 0 ("terrain-selftest: PASS", 29 [ok]
checks - unchanged from DEPLOY RECORD 1, as expected: 8e14cd1 touches the facade and the
service log line, not TerrainVertexAuthoring). WatchVrf --capabilities exit 0 (advertises
con-selftest + stop-file), --con-selftest exit 0 (ALL CHECKS PASSED). Runner offline gate
tests/RunnerTurnaround.Tests.ps1: 96 passed, 0 failed (the script prints its own tally; it
is not Pester-discovered, so Invoke-Pester's own counters read 0). No file under C:\MAK
changed; no launch in this step.

## 7. The confirming live run: TWO rows, one variable each (review F1)
Prereg per the standing rule; baseline = the R9 order under Live on the A48ABE6C bridge
(run 20260901T203702Z, 3/3 arrivals). A single run that flips the mode on the NEW bridge
would move two variables (native binary AND mode), which the 2026-07-19 rule forbids.

Row 1 - CONTROL (new bridge, mode=Live). Deploy per sec 6; leave
Vrf__GroundWaypointAltitudeMode=Live (the default). Expected: the R9 result reproduced -
3/3 units static -> moving -> settled, 3/3 TASKCMPLT, POS/RPT agreement, NO terrain log
line of any kind (the request path is never entered; the callback is never registered).
Miss = any deviation from the R9 table -> STOP; the new native binary is at fault
regardless of the mode code (07-19 record), do not proceed to row 2.
ROW 1 RESULT (2026-09-02, run 20260902T010704Z, appNos 3676-3682): ALL SIX predictions
MET - 3/3 TASKCMPLT at +117.3 / +129.2 / +183.8 s (CONFIRM2 +117.1 / +129.1 / +182.1),
endpoints 0.09 / 0.00 / 0.09 m from P2c, POS==RPT 0.0 x3, 0 terrain log lines, WARN census
identical, vrfSim counts 0/0/0/1, RTI PIDs unchanged, 7 min 15 s. Record:
docs/experiments/PREREG_TERRAIN_ROW1_CONTROL_2026-09-02.md sec 6. Row 2 cleared.

Row 2 - MODE (same bridge, Vrf__GroundWaypointAltitudeMode=TerrainProfile, env). Checks:
1. The reply arrives: log line "Terrain profile <id> for task '<name>': all N vertices
   authored from terrain + 10 m clearance; alts [...]" within TerrainProfileTimeoutSeconds
   of each ground task; NO "fallback" / "Partial" WARN. A WARN with "no reply" means the
   back end did not answer (check bin64/vrfSim.log at notify level 3 for the request) -
   the order still executes under Live numbers, by design. A "partial (Complete=false)"
   INFO line means the back end ignored sendPartialInformation=false - note it; the run
   is still valid if the complete reply follows.
2. Frame check (the sec 0 inference) - read the TWO signals separately (review F3):
   a. HORIZONTAL: a WARN "no usable sample for any vertex" (samples displaced > 50 m from
      every vertex) FALSIFIES the geocentric-request inference -> stop, read the request's
      setToNet/back-end handling, do not tune.
   b. VERTICAL: an INFO "taskee altitude not terrain-clamped: live X vs terrain Y" is NOT
      a frame signal - it says the taskee's published altitude is above the surface
      (unclamped birth altitude or aggregate). The route is still authored from terrain;
      record X, Y and the taskee type for the handoff.
   Echo tripwire (review F2): a WARN whose Reason contains "echoed request point" means
   the back end returned the request points, not terrain heights -> the mode cannot work
   as designed; stop and read how the manager packs Result.terrainHeight into
   DtIntersectionInformation. A "success" line with every authored altitude equal to the
   Live altitude + 10 m (i.e. live + 60) would be the same defect leaking past the 1 cm
   guard - compare the alts list with row 1's Live vertex altitudes.
3. Movement gate unchanged: static -> moving -> settled + POS/RPT agreement, 3/3
   TASKCMPLT, and the company's working offset routes non-empty (vrfSim.log).
Prediction: row 1 identical to R9; row 2 identical arrivals, terrain heights at the
Mojave vertices ~1000-1200 m ellipsoid within ~20 m of each clamped taskee's live altitude,
authored vertices = terrain + 10 m. Miss = any row-1 deviation, any fallback/Partial
WARN in row 2, an echo Reason, or a unit that moved under Live but not under
TerrainProfile.
ROW 2 RESULT (2026-09-02, run 20260902T011908Z, appNos 3683-3689): STOP - FALSIFIED BUT
UNADJUDICATED. The 3 requests (ids 7/8/9) were sent; all 3 hit the 10 s timeout and fell
back to Live (:1474 + :807 x3); no route-created callback followed, 0/3 moved. Cause: the
VR-Forces back end (pid 70668) took a FATAL ERROR at 21:21:26 local - 6 s BEFORE the order
push and before RequestTerrainProfile was first called - and sat on the MAK crash-dump
prompt ("A fatal error has occurred. Would you like to save a diagnostic file?"). The mode
variable was never exercised against a live back end. Full record + state left behind:
docs/experiments/PREREG_TERRAIN_ROW2_MODE_2026-09-02.md sec 6. Next: user decides on the
70668 dump; then repeat Row 2 on a clean boot (same prereg, new run id) - NO retune.

ROW 2R RESULT - THE REPEAT, AND THE ROW 2 ADJUDICATION (2026-09-02, run
20260902T101431Z, appNos 3690-3696): the 70668 dump was saved and VR-Forces torn down; the
repeat ran the identical experiment. The back end lived the whole run (11,283 log lines,
3,635 of them after the order push; no crash prompt, no new dump). The three requests
(ids 7/8/9) were sent AND ANSWERED inside the 10 s budget - no :1474 timeout, no Fallback,
no :1453 partial-series line - but all three replies were PARTIAL in the same shape:
"Terrain profile 7 for task 'T_R5_PL1': Partial - vertices 1,2 had no usable sample - kept
Live altitude; 2 vertex(es) keep the Live altitude." Vertex 0 (the taskee's own position)
authored from terrain; vertices 1 and 2 (the order points, 555 m - 1 km away) did not.
Neither tripwire fired: no "echoed request point" (F2), no "no usable sample for any
vertex" (F3 horizontal frame falsifier) - vertex 0's sample was inside the 50 m gate, which
is evidence FOR the geocentric-request inference. Check 1 is therefore NOT met (a Partial
WARN fired); check 2 is clean; check 3 is met - movement was untouched (3/3 TASKCMPLT at
+118.0 / +130.1 / +185.2 s vs Row 1's +117.3 / +129.2 / +183.8, endpoints within 0.27 m of
P2c, POS==RPT 0.0 x3, 7 min 15 s, StopVrf exit 0), because the Partial path keeps Live
altitudes. The mode is alive but does not yet deliver its purpose: the order-point waypoints
still carry Live altitudes. The sample count and per-vertex distances are only logged at
Debug (:1459) and the back end logs nothing about the request at notify level 3, so WHY
vertices 1-2 have no usable sample is undecided - docs-first, supervisor's call, NO retune
was attempted. Full record: docs/experiments/PREREG_TERRAIN_ROW2R_MODE_2026-09-02.md sec 6.
NOTE a defect in Row 2's sec 6: it quotes the create lines as "mode=TerrainProfile"; the log
template at VrfC2SimService.cs:439 hard-codes "mode=Live" for the whole live-like family and
Row 2's own log reads mode=Live, same as Row 1 and Row 2R.

ROW 2c RESULT - THE FLATTEN, AND ROW 2 CHECK 1 FINALLY MET (2026-09-02, run
20260902T104832Z, appNos 3697-3703, bridge A7504441 from 8e14cd1): ROW2R's Partial replies
were a FACADE defect, not a back-end limitation. With the trampoline walking every entry of
every response set, all three requests came back with THREE samples, one per requested point,
in request order, each carrying a distinct userData index 0/1/2:
"Terrain profile reply 9: 3 sample(s) [#0:34.60842,-116.71269,1131.4
#1:34.60842,-116.70637,1126.3 #2:34.60842,-116.70006,1121.1]." (ids 7 and 8 likewise). All
three routes then authored all three vertices - "Terrain profile 9 for task 'T_R5_TK1': all 3
vertices authored from terrain + 10 m clearance; alts [1141.4, 1136.3, 1131.1]." - with ZERO
`warn:` lines of any kind in the app log (ROW2R had three :807 Partials). Check 1 of Row 2 is
MET for the first time; check 2 is clean and its geocentric-request inference is now backed by
six order-point samples landing on their own request vertices to five decimals rather than one;
the echo tripwire stayed silent and the samples sit 50 m BELOW the request points (which
carried live + 50), agreeing to 0.1 m with each entity's clamped resting altitude at that
place. Check 3 is MET except for one number: 114.MechCoy, THE ONLY AGGREGATE taskee,
completed at +198.1 s against Row 1's +183.8 (band was +/-10 s), and the trace agrees it
physically arrived later (plateau onset 233.3 s vs 219.2). The two individual entities are
unchanged to within 0.5 s despite comparable 45-50 m drops in their waypoint altitudes, so
the artifacts associate the delay with the aggregate rather than with the size of the
altitude change; cause is UNDECIDED and NO retune or re-run was made. Endpoints, resting
altitudes, POS==RPT, settle, early exit and every hygiene measure are Row 1's exactly; back
end alive (14,158 log lines, 5,464 after the order push), no dump, runner and StopVrf exit 0,
RTI PIDs unchanged. Full record: docs/experiments/PREREG_TERRAIN_ROW2C_FLATTEN_2026-09-02.md
sec 6. NEXT QUESTION for the supervisor: the aggregate's +14 s, docs-first.

ROW 2cR RESULT - THE UNCHANGED REPEAT, AND THE +14 s QUESTION CLOSED (2026-09-02, run
20260902T111116Z, appNos 3704-3710, bridge A7504441, nothing changed from Row 2c): Row 2c's
aggregate delay DID NOT REPRODUCE. 114.MechCoy completed at +185.0 s against Row 2c's +198.1
and Row 1's +183.8, and the trace agrees it physically arrived on Row 1's schedule - plateau
onset 219.2 s, the SAME value as Row 1's 219.2, against Row 2c's 233.3 (trace TSK completionT
213.2 vs Row 2c 226.4 vs Row 1 212.0). The terrain authoring was character-for-character
identical to Row 2c - the same three ":1466 3 sample(s)" reply lines and the same three ":802
all 3 vertices authored ... alts [1050.6, 1043.9, 1036.7] / [1126.7, 1126.8, 1126.9] /
[1141.4, 1136.3, 1131.1]" lines, zero warn: lines - so the aggregate ran on exactly the
lowered waypoint altitudes twice and finished 13 s apart. H-ALT (the ~40 m lower waypoints
systematically change the aggregate's move-along) is REFUTED as a reproducible effect; H-V
(run-to-run variance of the aggregate) STANDS. The two individual taskees were inside 0.5 s
of Row 1 as always (+117.5 / +129.7).
A CORRECTION Row 2cR forces on Row 2c's write-up: the "about 3 s" prior spread quoted there
counted only three runs. Every run in runs/ that pushed this order and got a TASKCMPLT for
taskee 139aa71b at 1x gives 178.2 (R9 baseline) / 184.6 (P2c) / 183.7 (CONFIRM1) / 182.1
(CONFIRM2) / 183.8 (Row 1) / 185.2 (ROW2R) / 198.1 (Row 2c) / 185.0 (Row 2cR) - a
pre-Row-2c spread of 7.0 s, not 3 s, and a full observed range of 19.9 s. The aggregate is
about twenty times noisier than either individual taskee (117.1-118.0 and 129.1-130.1 over
the same set), which is consistent with the vendor's documented mechanism: an aggregate does
not follow the ordered route at all - DtDisaggregatedMoveAlongController builds a temporary
offset route per subordinate and the task completes only when ALL subordinates report
complete (disaggregatedMoveAlongController.h:34-63), under a 1 Hz formation monitor whose
slowdown factor is 0.1x ordered speed (aggregateMoveAlongDescriptor.h:111-139). GOING FORWARD:
require n>=2 before calling any timing shift on 114.MechCoy an effect.
DOCS ANSWER recorded with the prereg (docs/experiments/PREREG_TERRAIN_ROW2CR_REPEAT_
2026-09-02.md, sources section): NO vendor source read - C:\MAK\vrforces5.0.2\include, the
5.0.2 doc/ set (which has no Developer's Guide PDF), or docs.mak.com - states that route-vertex
altitude affects an aggregate's move-along timing, and none states that it does not. What IS
documented and cuts against H-ALT: DtAggregateMoveAlongDescriptor's `ground-clamp` parameter
defaults to True with the note "Ground-vehicles and human aggregates should set this value to
true" (aggregateMoveAlongDescriptor.h:159-164), i.e. the GENERATED subordinate route vertices
are ground clamped and the authored altitude is discarded before any subordinate chases it -
which the 2026-07-22 below-terrain confound already showed empirically (a disaggregated Tank
Platoon with waypoints 941 m BELOW terrain moved identically, clamped up to the surface;
docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md sec 6a). Row 2cR is the
seconds-resolution confirmation that comparison lacked.
Hygiene all Row 1's: 3/3 TASKCMPLT, endpoints within 0.2 m, POS==RPT 0.0 x3, settled x3,
0 warn:, vrfSim counts 0/0/0/1, no dump, back end alive (11,700 lines, 6,465 after the order
push), runner and StopVrf exit 0, RTI PIDs 41336/224608/76620 unchanged, wall 7 min 13 s.
Full record: docs/experiments/PREREG_TERRAIN_ROW2CR_REPEAT_2026-09-02.md sec 6. THE ROW 2
QUESTION IS NOW CLOSED: checks 1, 2 and 3 all MET, mode functional, no movement cost.

DEFAULT FLIP (2026-09-02, supervisor decision, COMMIT 5b82e5f "TerrainProfile is the
default GroundWaypointAltitudeMode (Rows 2c/2cR)"; this hash line is the only content
added to this paragraph after that commit). WHAT CHANGED: exactly one literal -
`VrfSettings.GroundWaypointAltitudeMode` default `"Live"` -> `"TerrainProfile"`, plus the
comment blocks and the docs that asserted the old default. NO code path changed: Live's
create-altitude arithmetic, groundWpAlt arithmetic, dispatch order and log lines are
untouched, "Fixed100" is untouched, the native bridge is NOT rebuilt (A7504441 stays on
10/10 copies), and `Vrf__GroundWaypointAltitudeMode` still selects any of the three modes.
RATIONALE: TerrainProfile is the DOCUMENTED frame. The Users Guide makes route-vertex
altitude the author's responsibility and warns that above-sea-level vertices can be
underground (sec 1.5, contract C5); "Live" (live entity altitude + 50 m) was an
APPROXIMATION of terrain height with no terrain knowledge behind it, while TerrainProfile
asks the simulating back end for the height under each vertex and authors terrain + 10 m.
Two consecutive live runs (Row 2c 20260902T104832Z and Row 2cR 20260902T111116Z, both on
bridge A7504441) authored all 3 vertices of all 3 routes from terrain with ZERO warn: lines
and character-for-character identical reply and authoring lines, and cost NOTHING in
movement: 3/3 TASKCMPLT at Row 1 timings, endpoints within 0.2 m, POS==RPT 0.0 x3. The
Row 2c aggregate excursion (+14 s on 114.MechCoy) did NOT reproduce and is adjudicated as
run-to-run variance of the aggregate (Row 2cR sec 6; H-ALT refuted as a systematic effect).
Live stays in the product as the fallback for a back end that cannot or should not be asked
for terrain, and Fixed100 stays as the golden-parity relic.
WHAT ROW 3 TESTS: that the FLIP ITSELF took - i.e. that the app with NO env override at all
(`Get-ChildItem env:Vrf__*` empty) takes the TerrainProfile path and reproduces Row 2cR.
Every prior TerrainProfile run in this branch reached the mode through the env override, so
until Row 3 the DEFAULT has never been exercised. One variable vs Row 2cR: the removal of
`Vrf__GroundWaypointAltitudeMode=TerrainProfile`. Prereg:
docs/experiments/PREREG_TERRAIN_ROW3_DEFAULT_2026-09-02.md. The falsifier that matters is
zero terrain request lines, which would mean the deployed bin did not pick the new default
up (a stale binary, or an appsettings/env override) - appsettings.json carries no `Vrf:`
mode pin, which is what makes the code default the effective one.

ROW 3 RESULT - THE DEFAULT FLIP IS LIVE-CONFIRMED (2026-09-02, run 20260902T113613Z, appNos
3711-3717, bridge A7504441 unchanged and NOT rebuilt, main at b2ceeb1): the app took the
TerrainProfile path with NO environment override at all. `Get-ChildItem env:Vrf__*` was count
0 both before and after the runner (echoed into runs/20260902T113613Z_run/console-row3.log),
the deployed appsettings.json beside the running exe carries no mode pin, and the runner
injects only Vrf__ApplicationNumber - so the mode came from VrfSettings.cs:175 as compiled in
5b82e5f and from nowhere else. A stale binary is excluded by the same evidence: a pre-flip
build carries "Live" and cannot emit a terrain line at all, which is precisely why F1 (zero
terrain lines) was the falsifier that mattered. All three predictions MET, no falsifier fired.
The terrain query was character-for-character Row 2cR's for the third consecutive run - three
":813 request sent for 3 vertices" (ids 7/8/9), three ":1466 3 sample(s)" replies, three
":802 all 3 vertices authored ... alts [1050.6, 1043.9, 1036.7] / [1126.7, 1126.8, 1126.9] /
[1141.4, 1136.3, 1131.1]", zero `warn:` lines. Movement is Row 1's: 3/3 TASKCMPLT at +117.47
(1.BdeHQ) / +129.63 (1222.MechPlt) / +182.34 s (114.MechCoy), plateau onsets 147.9 / 160.1 /
215.3, endpoints identical to Row 2c to six decimals, POS==RPT 0.0 x3, satisfied x3, early
exit 64.6 s, wall 7 min 14 s, every stage exit 0, back end alive (10,600 log lines, 5,880
after the order push), no dump, RTI PIDs 41336/224608/76620 unchanged. 114.MechCoy's 182.3 s
is a THIRD terrain-authored draw and it landed BELOW every Live-era draw but the R9 baseline,
which is the opposite of the observation Row 2cR named as the falsifier that would reopen
H-ALT; the H-V ruling therefore stands and that residual narrows. Full record:
docs/experiments/PREREG_TERRAIN_ROW3_DEFAULT_2026-09-02.md sec 6, which also records one
prereg defect (the :813 template text was mis-transcribed into the prediction; the
prediction's substance was met and the log's own wording is quoted in the outcome) and the
remaining known gap (the per-vertex Live fallback path has never run on a healthy back end).

## 8. Gate results (2026-09-01, offline, worktree only)
First revision (commit 6539036): VrfBridge /t:Rebuild exit 0 / 0 warnings; dotnet build 0
errors; 7 self-tests exit 0 (terrain: 21 checks); ASCII clean vs a dirty control.

Second revision (review fixes F1/F2/F3 + LOW):
- VrfBridge.vcxproj /t:Rebuild Release x64 (MSBuild via PowerShell - Git Bash mangles the
  /m and /t: switches into paths, MSB1008): exit 0, 0 warnings ->
  src/VrfBridge/build/Release/VrfBridge.dll (SHA-256 4286B64D...AAC37; VrfFacade is
  built as part of it).
- dotnet build src/VrfC2SimApp -c Release: "Build succeeded. 0 Error(s)" (6 warnings,
  all pre-existing: CA2024 in the SDK, CS8632 at VrfC2SimService.cs lines 52/944).
  Build aid: the csproj's relative SDK ProjectReference does not resolve from the deeper
  worktree path, so the worktree build passes an untracked worktree-sdk.targets via
  -p:CustomBeforeMicrosoftCommonTargets that re-points the reference to the absolute SDK
  path. Not needed from the main checkout.
- Self-tests (MAK bin64 dirs on PATH): --translator-selftest exit 0 (SELF-TEST PASSED),
  --report-selftest exit 0 (ALL CHECKS PASSED), --sequencer-selftest exit 0 (ALL CHECKS
  PASSED), --verb-selftest exit 0, --destack-selftest exit 0, --fanout-selftest exit 0,
  --terrain-selftest exit 0 ("terrain-selftest: PASS", 29 checks, 14 scenarios; new:
  5 vertex-0 gap -> Terrain + Note, 11 all echoed -> Fallback, 12 one echoed -> Partial
  and 2 cm is not an echo, 13 vertex 0 invalid + others valid -> Partial, 14 all
  displaced -> "no usable sample"). Case 5's expectation flipped from Fallback to Terrain
  with the F3 change - the previous code fails the new case, so it is not vacuous.
- Native default path: Start(), StartAdopting(), RegisterInboundCallbacks(), Tick() in
  VrfFacade.cpp byte-identical to c24248f; Stop() differs by the one-line flag reset.
- ASCII: rg -n -P "[^\x00-\x7F]" matched the known-dirty control (exit 0) and matched
  nothing in the 9 touched files (exit 1). Every touched file CRLF in the working tree.
- Default-path diff: git diff of VrfC2SimService.cs vs c24248f removes only 4 lines - the
  two "Live" predicates (replaced by IsLiveLikeAltitudeMode(), which is true for "Live"),
  the ExecuteTaskOnTick signature (new optional parameter, default null) and one
  doc-comment line.
- Untested offline (review sec 5, unchanged): the service-level lifecycle - pending
  insert, expiry sweep, stale drop, partial-reply wait, continuation re-entry with
  terrainRoute, exactly-once MarkDispatched - and the native trampoline (userData parse,
  empty inner vector, type guard). The self-test template has no bridge seam; row 2 of
  sec 7 is the check.
- Oracle parity: none to check (sec 0) - the oracle never queries terrain.
