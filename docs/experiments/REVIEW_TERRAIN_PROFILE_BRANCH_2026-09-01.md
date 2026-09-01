# REVIEW: GroundWaypointAltitudeMode=TerrainProfile branch (2026-09-01)

Reviewed: worktree branch worktree-agent-a51dbe56992f78330, commit 6539036 (parent
c24248f = main at the time). Read-only review; nothing on the branch was edited, no
VR-Forces launch, no C:\MAK file touched. Design doc under review:
docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md (in the worktree).

## Verdict: MERGE WITH FIXES

**Pass 2 (commit 066f3d2): MERGE. All three fixes verified; see section 8.** The text
below this line is the pass-1 record of commit 6539036 and is kept as-is.

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

## 8. Pass 2 - fixes verified (commit 066f3d2, parent 6539036)

Same rules as pass 1: read-only on the branch, no sim, no C:\MAK writes. Diffs read:
6539036..066f3d2 (5 files, +246/-87: design doc, TerrainSelfTest.cs, TerrainVertexAuthoring.cs,
VrfC2SimService.cs, VrfFacade.cpp) and the full c24248f..066f3d2.

### Verdict: MERGE

### F1 (lazy callback registration) - VERIFIED
- Registration now lives only in VrfFacade::RequestTerrainProfile, behind
  Impl::terrainCallbackRegistered, which Stop() resets right after `p_->controller =
  nullptr`. Start() and RegisterInboundCallbacks() no longer touch the terrain callback.
- My own body comparison against c24248f (awk function-body extraction, byte compare):
  Start() IDENTICAL (71 lines), StartAdopting() IDENTICAL (13), RegisterInboundCallbacks()
  IDENTICAL (16), Tick() IDENTICAL (6); Stop() differs by exactly the one flag-reset line.
  Native diff c24248f..066f3d2 is pure insertion: 172 insertions, 0 deletions
  (VrfBridge.cpp +50, VrfFacade.cpp +89, VrfFacade.h +33).
- A consumer that never calls RequestTerrainProfile makes ZERO new MAK/controller calls.
  The only new operation on its path is bridge-internal: VrfBridge.cpp WireCallbacks
  assigns `_facade->OnTerrainProfile = TerrainProfileThunk{gcroot(this)}` (a std::function
  store, no vendor call). Negligible, but it is why the control row (design sec 7 row 1)
  is still needed: the binary is new even if the default path's vendor calls are not.
- Registering from inside a request: safe on two independent grounds. (a) The request is
  queued through _tickActions and drained by TickLoop BEFORE `_bridge.Tick()` /
  drainInput() on the same thread, so no callback-list iteration is in flight. (b) Even if
  it were, readerWriter/noArgumentCallbackList.h documents DtCallbackList as "safe to add
  and remove callbacks from this table during callback invokation" and "safe to add and
  remove callbacks simultaneously from multiple threads" (tbb::spin_mutex). The
  messageExecutive.h:99 std::map<int, DtCallbackList> insertion of a new type key happens
  on the tick thread outside dispatch. No issue.

### F2 (1 cm echo guard) - VERIFIED; healthy case does NOT false-fallback
The question: if the entity IS terrain-clamped and the request point is at terrain
height, is the legitimate reply rejected as an echo? Answer: NO, because the request
point is never at terrain height under the default settings.
- The echo test compares the sample against v.AltMeters, the REQUEST vertex altitude
  (TerrainVertexAuthoring.cs:64). The request is built from routeGeo, whose ground
  vertices - vertex 0 included - carry `live.AltMeters + GroundWaypointLiveClearanceMeters`
  (VrfC2SimService.cs:737-743, :777; default 50 m, VrfSettings.cs:176).
- A clamped taskee's terrain reply is ~live.AltMeters, i.e. ~50 m below the request
  altitude - 5000x outside the 1 cm window. The echo guard cannot fire on it. A distant
  vertex whose terrain happens to equal live + 50 within 1 cm is a ~1e-4 coincidence per
  vertex and its only effect is that ONE vertex keeps Live (Partial WARN naming the vertex),
  never a route fallback.
- Ordering is right: the horizontal test runs first (:63), so an echo (same lat/lon)
  reaches the echo test; a displaced sample never does.
- Configuration edge (LOW, documented here, no code change requested): with
  GroundWaypointLiveClearanceMeters=0 the request altitude equals live, a clamped taskee's
  vertex-0 terrain sample lands within centimetres of it and IS rejected as an echo ->
  Partial with vertex 0 kept Live at the entity's own altitude. Harmless for movement
  (the entity is already there) but it would read as a spurious WARN. Default is 50, the
  design does not propose changing it; note it beside the setting if that knob is ever
  lowered.
- Self-tests 11/12 encode the guard: 11 (three samples at reqAlt, +0.005, -0.009) ->
  Fallback, Reason "3 echoed ... at vertex 0,1,2", Live altitudes; 12 (only vertex 1 at
  reqAlt, vertex 2 at reqAlt + 0.02) -> Partial KeptLive [1], vertex 2 authored
  reqAlt + 0.02 + 10 - so the 1 cm bound is pinned from both sides.

### F3 (vertex-0 gap is a Note, not a falsifier) - VERIFIED
- TerrainVertexAuthoring.cs:75-79: |terrain0 - entityAlt| > 100 m sets Result.Note
  ("taskee altitude not terrain-clamped ... authoring from terrain anyway"); Mode is
  unaffected. VrfC2SimService.cs:809-810 logs the Note at INFO.
- Design sec 7 check 2 now reads the two signals separately: 2a HORIZONTAL ("no usable
  sample for any vertex") is the ONLY frame falsifier; 2b VERTICAL is explicitly "NOT a
  frame signal". Design decision rules (lines 223-241) say the same. I grepped the design
  doc for every "falsif"/"frame"/"vertex 0" occurrence: no residual text treats the vertical
  gap as a frame falsifier. Sec 0 row "Frame of the REQUEST points" was updated to match.
- 50 m horizontal threshold: sole frame signal, and sound in kind - a frame error (geodetic
  degrees read as geocentric metres, or vice versa) displaces samples by thousands of km,
  while the legitimate offset between a request point and its vertical intersection is
  sub-metre (geocentric-radial vs ellipsoidal-normal drop over 50-60 m of altitude is
  < 0.2 m). LOW: the design doc states the number but not this derivation; one sentence
  next to DefaultMaxHorizontalMismatchMeters (TerrainVertexAuthoring.cs:24) would close it.
  The value also has to stay below the closest R9 inter-vertex spacing for the
  positional-index fallback argument (design line 248) to hold - the doc asserts "no such
  pair" without a number.
- Self-test 5 flip is non-vacuous: entityAlt 1100, terrain 100/120/140 -> Mode.Terrain
  with concrete authored altitudes 110.0 and 150.0 (= terrain + 10, depends on the
  authoring path running) plus Note text asserted to contain "not terrain-clamped" and not
  "frame". Case 6 (60 m gap) asserts Note == null. The pass-1 code (Fallback on gap) fails
  case 5.

### LOW items and new cases 13/14 - VERIFIED
- Complete=false: OnVrfTerrainProfile (VrfC2SimService.cs:1446-1453) logs INFO and returns
  WITHOUT removing the pending entry when a partial arrives for a live request; the entry
  is consumed by the complete reply or by ExpireTerrainRequests (:307, :1468) -> Live
  fallback "no reply". complete() defaults true per ifIntersectionInformationResponse.h:147
  ("Default is true"), and the app requests sendPartialInformation=false, so this path is
  defensive only. Safe direction on every branch.
- Case 13 (vertex 0 Invalid, 1 and 2 valid): Partial, KeptLive [0], vertex 0 = 1150 (live +
  50), vertex 1 = 1130.5 (1120.5 + 10), Note null - tests exactly the "vertex 0 unusable does
  not block the others" behaviour claimed. Case 14 (all samples ~1 deg away): Fallback with
  Reason starting "no usable sample" - tests the horizontal frame signal.
- Echo count text and Partial reason text asserted by content (case 11), not just by Mode.

### Build state, tests, hygiene
- Binaries: src/VrfBridge/build/Release/VrfBridge.dll 4286B64D...AAC37 built 19:02:31,
  AFTER the last VrfFacade.cpp edit (19:02:11) - fresh, matches the hash the design doc
  names. The app bin's copy was STALE at review start (3F68CFF1..., built 19:00:22, i.e.
  before that last facade edit - the executor rebuilt the bridge after the app). Pure
  managed self-tests were unaffected, but it is the exact stale-bin trap design sec 6 step 5
  warns about, inside the worktree itself. I rebuilt the app in the worktree (dotnet build
  -c Release with the untracked worktree-sdk.targets): Build succeeded, 0 errors, 6
  pre-existing warnings; the bin copy is now 4286B64D. Nothing else on the branch touched.
- Self-tests from the refreshed worktree bin (MAK bin64 dirs on PATH), all exit 0:
  terrain ("terrain-selftest: PASS", incl. the 7 new [ok] lines for cases 5/11/12/13),
  translator, report, sequencer, verb, destack, fanout.
- git diff --check 6539036..066f3d2 clean; rg -n -P "[^\x00-\x7F]" matched the dirty
  control and nothing in the 5 touched files; all 5 CRLF in the working tree.

### Residual for the confirming run (no code change)
- Control row (design sec 7 row 1, new bridge + mode=Live) remains mandatory: the native
  binary is new even though its default-path vendor calls are byte-identical in source.
- Deploy per design sec 6 step 5 (one hash across build output and every consumer bin) -
  the worktree just demonstrated how a stale copy arises.

### Adversarial review note (pass 2)
Competing hypothesis for F2: "the echo guard rejects the healthy clamped case". Falsified
by reading the request construction (live + 50 for every ground vertex) - the only way it
holds is GroundWaypointLiveClearanceMeters=0, recorded above as a configuration edge.
Competing hypothesis for F1: "Start() still differs from c24248f in some way the diff
stat hides". Falsified by my own extracted-body byte comparison, not the executor's claim.
Unexplained symptom: the stale app-bin bridge copy - explained by timestamps (facade edit
19:02:11 after the app build 19:00:42), resolved by the rebuild, hash now uniform.
