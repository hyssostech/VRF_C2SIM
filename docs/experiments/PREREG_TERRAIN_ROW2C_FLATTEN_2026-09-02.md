# PREREG TERRAIN ROW 2c - REPLY FLATTEN: does the back end actually send three samples? - registered 2026-09-02, BEFORE launch

WHAT THIS IS: Row 2R run again with ONE variable changed - the deployed native bridge and
app build. ROW2R (docs/experiments/PREREG_TERRAIN_ROW2R_MODE_2026-09-02.md sec 6, run
20260902T101431Z, bridge 28E993FE) is ADJUDICATED, not open: the back end lived, the three
terrain requests (ids 7/8/9) were sent AND answered inside the 10 s budget, and all three
replies were PARTIAL in the identical shape - "Terrain profile 7 for task 'T_R5_PL1':
Partial - vertices 1,2 had no usable sample - kept Live altitude; 2 vertex(es) keep the Live
altitude." Vertex 0 (the taskee's own position) authored from terrain; vertices 1 and 2 (the
order points, 555 m - 1 km away) not. ROW2R's own adjudication left three competing
hypotheses open and named them INFERRED, because the sample count and per-vertex distances
are only logged at Debug (:1459 in that build) and the back end logs nothing about the
request at notify level 3:

    (i)   the back end returned FEWER samples than requested - e.g. one - so vertices 1
          and 2 had no candidate within the 50 m gate;
    (ii)  it returned three samples but only the first is near its vertex;
    (iii) the samples for 1 and 2 were present but rejected by the usability gate for
          another reason.

Between ROW2R and this run, commit 8e14cd1 (already on main, deployed for this run) makes
the run DISCRIMINATE between them. It does two things, both additive:

1. src/VrfFacade/VrfFacade.cpp terrainProfileTrampoline now walks EVERY entry of EVERY
   response set instead of entry [0] of each set. The old code read `pairs[i][0]` and
   discarded `pairs[i][1..]`; the new code iterates j over each inner vector and pushes one
   TerrainSample per entry, with index = userData when it parses as an int, else a running
   count over the flattened entries. An EMPTY inner vector is still kept as one invalid
   sample at index i.
2. src/VrfC2SimApp/VrfC2SimService.cs OnVrfTerrainProfile gains an INFORMATION-level line
   (now :1466) logging the REPLY SHAPE before the continuation is enqueued:
   `Terrain profile reply {Id}: {N} sample(s) [{Shape}].` where Shape joins one token per
   sample, `#<Index>:<Lat F5>,<Lon F5>,<TerrainAlt F1>` for a valid sample and `#<Index>:none`
   for an invalid one. This is the instrument: it makes "the back end sent one sample"
   distinguishable from "the facade read one sample" from the run directory alone, which is
   exactly what ROW2R could not do.

HYPOTHESIS UNDER TEST (H-A, from 8e14cd1's message and the VrfFacade.cpp comment): the back
end packs ALL point results into ONE response set, so the old entry-[0]-only read yielded
exactly one sample - vertex 0 - and hypothesis (i) above was an artefact of the FACADE, not
of the back end. If H-A holds, the flattened read produces three samples and the three
routes author all three vertices from terrain.

## Sources read for this prereg (docs first, per the 2026-09-01 directive)

- `git show 8e14cd1` in full (both hunks quoted in substance above); the VrfFacade.cpp
  comment it installs, which cites vrfobjcore/terrainProfileRequestManager.h:107-121
  ("the back end keeps ONE result per request POINT, Results = map<int, Result>") and
  vrfmsgs/ifRequestTerrainProfileInformation.h:46-48 ("the user data of each information
  response is the index of the terrain profile request satisfied with the response"), and
  states that HOW those results are packed into sets is NOT documented.
- C:\MAK\vrforces5.0.2\include\vrfmsgs\ifIntersectionInformationResponse.h (read-only), the
  passage the old code leaned on: for the general intersection request "there will be one
  set of response for each pair. If there is no intersection ... then an empty list will be
  returned at the given response index" (:136-138), and the file comment "Note that all
  point information returned is in geocentric" (:20). The header says nothing about how a
  PROFILE request's per-point results map onto sets - that silence is what H-A fills.
- docs/experiments/PREREG_TERRAIN_ROW2R_MODE_2026-09-02.md, ALL sections, especially sec 6
  (verdict, the three verbatim :807 lines, the movement/hygiene numbers reused below as
  comparators, the "WHAT THE BACK-END LOG SAYS ABOUT THE REQUEST" paragraph: zero level-3
  lines matching IfRequest / TerrainProfile / IntersectionInformation, and the correction
  that the 1,100 "Segment endpoints are identical" lines are collision-avoidance noise, not
  request traffic).
- docs/experiments/PREREG_TERRAIN_ROW1_CONTROL_2026-09-02.md sec 6 (the Row 1 comparators:
  offsets +117.3 / +129.2 / +183.8 s, endpoints within 0.09 m of P2c, POS==RPT 0.0 x3, start
  altitudes 1131.4 / 1116.7 / 1040.6 m, WARN census, wall 7 min 15 s, vrfSim counts 0/0/0/1).
- docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 3.3 (the decision rules: 50 m
  MaxHorizontalMismatchMeters gate, the 1 cm echo guard, Terrain/Partial/Fallback modes,
  TerrainClearanceMeters 10), sec 6 DEPLOY RECORD 2 (this run's bridge A7504441, one-hash
  proof over 10 copies), sec 7 (the Row 2 checks 1-3, unchanged and still the standard).
- src/VrfC2SimApp/VrfC2SimService.cs as built for this run: :793 WARN "request not sent",
  :802 INFO "all N vertices authored", :807 WARN "Partial|Fallback", :810 INFO Note,
  :813 INFO "request sent", :1453 INFO "partial (Complete=false)", :1459 DEBUG "matches no
  pending request", :1466 INFO "reply {Id}: {N} sample(s) [{Shape}]" (NEW), :1480 WARN "got
  no reply within {T} s". Note the ORDER at :1453/:1459/:1466: a Complete=false reply and a
  reply matching no pending request both return BEFORE :1466, so the shape line appears only
  for a matched, complete reply.
- data/R9_Mojave_UnitMove_Order_NoComments.xml - the request vertices 1 and 2 of each route,
  used verbatim by F3 below.
- docs/RUNBOOK.md sec 0.5.0 (pre-flight inventory; never kill; the -AllowExistingVrf
  false-READY trap), 0.5.9 (StopVrf exit codes), 0.5.11 (-RunSecs / -StopWhenComplete /
  -SettleHoldSecs, the stop-file observer path), 0.5.12 (the MAK dump-prompt signature
  `^vrfSim.*\.dmp$`, ALWAYS-YES ruling, scripts/AnswerCrashDumpDialog.ps1).

ASCII only. The C++ repo at c2simVRFinterfacev2.36 is a frozen oracle and is untouched;
nothing under C:\MAK is written.

## 1. The ONE variable

THE BRIDGE AND APP BUILD FROM 8e14cd1 (reply flatten + reply-shape Info line), deployed as
DEPLOY RECORD 2 in docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 6:
VrfBridge.dll 28E993FE33032505A999E508877832459450E0568E7E25FFD72BC80D59257FD5 (867840 B)
-> A7504441F421B668D10F5AFD8B4FD71110002D13FE6ABAE0DB576C7C209236F5 (868352 B), 10/10
main-checkout copies re-hashed to the new value, Ijwhost.dll UNCHANGED at
382550362C68297E253EDF796173B8DB8C43709D902E88C94E48BE7D1D435FD2 (so, unlike DEPLOY RECORD
1, the .NET host is not a second moving part this time).

EVERYTHING ELSE IS ROW2R's: env `Vrf__GroundWaypointAltitudeMode=TerrainProfile` set in the
pwsh session that invokes the runner (the runner's Stage 6b Start-External inherits the
process environment; the runner saves/restores only Vrf__ApplicationNumber);
`Vrf__TimeMultiplier` NOT set (1x); the same init and order files; RealTemplates; stock
templates; NavArea disabled; notify level 3; `-RunSecs 420 -StopWhenComplete`;
SettleHoldSecs 60; TerrainClearanceMeters 10; TerrainProfileTimeoutSeconds 10;
GroundWaypointLiveClearanceMeters 50; MaxHorizontalMismatchMeters 50. No code was edited for
this run - 8e14cd1 was authored before it and only the binary changed state.

## 2. Invocation (main checkout, VRF_C2SIM, pwsh)

    $env:Vrf__GroundWaypointAltitudeMode = 'TerrainProfile'
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_NoComments.xml `
        -RunSecs 420 -StopWhenComplete
    Remove-Item env:Vrf__GroundWaypointAltitudeMode

Adjudication from the run directory artifacts ONLY (vrfc2simapp.log, reports-captured.log,
run-manifest.json, watchvrf-trace.csv, bin64-vrfSim.log). Ledger: marker
`*** NEXT FREE: 3697 ***` before the run (verified as the single authoritative marker in
docs/OPUS_EXECUTION_PLAN.md Appendix B); the runner consumes 7 numbers and advances the
marker itself, so expected wasValue 3697 / newValue 3704, appNos 3697-3703.

## 3. Pre-launch inventory (must hold, else STOP - never kill)

VR-Forces DOWN, no vrfSim* / vrfGui / WatchVrf / ListenReports / VrfC2SimApp; RTI trio
rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 resident and unchanged since ROW2R;
docker stp-server + c2sim_server Up; main checkout at the commit carrying this prereg;
10/10 main-checkout VrfBridge.dll copies hashing A7504441; `Get-ChildItem env:Vrf__*` shows
EXACTLY GroundWaypointAltitudeMode=TerrainProfile and nothing else.

VERIFIED AT 10:40Z, before the deploy: no vrfSim*/vrfGui/WatchVrf/ListenReports/VrfC2SimApp
process; RTI trio 41336 (9 threads) / 224608 / 76620 resident; docker stp-server "Up 18 hours
(healthy)" + c2sim_server4.8.4.9 "Up 18 hours"; `Get-ChildItem env:Vrf__*` count 0.

## 4. Predictions with numbers

Comparators. ROW2R/Row 1 taskee live altitudes at task time (POS alt at first sample):
1.BdeHQ 1131.4 m, 114.MechCoy 1116.7 m, 1222.MechPlt 1040.6 m. Task -> taskee -> request id
(from ROW2R's log, deterministic ordering): T_R5_PL1 -> 1222.MechPlt -> id 7;
T_R5_CO1 -> 114.MechCoy -> id 8; T_R5_TK1 -> 1.BdeHQ -> id 9. Request vertices 1 and 2 per
route, from the order file (vertex 0 is the taskee's live position, read at task time):

    id 7 / T_R5_PL1 / 1222.MechPlt: v1 34.612956,-116.594174   v2 34.612956,-116.587860
    id 8 / T_R5_CO1 / 114.MechCoy:  v1 34.652629,-116.693388   v2 34.657629,-116.693388
    id 9 / T_R5_TK1 / 1.BdeHQ:      v1 34.608416,-116.706372   v2 34.608416,-116.700059

P1 - THE DISCRIMINATING PREDICTION (HIGH). Exactly THREE Information lines of the new
    :1466 form, one per request id 7 / 8 / 9:
        Terrain profile reply 7: 3 sample(s) [#0:...,...,... #1:...,...,... #2:...,...,...]
    Each carries N = 3, three tokens, three DISTINCT indices {0,1,2}, and NO `#k:none`
    token (all three samples valid). A count other than 3, a repeated index, or a `none`
    token is a MISS and selects a falsifier branch below.

P2 - ALL THREE VERTICES AUTHORED (HIGH). Exactly THREE :802 Information lines
    "Terrain profile <Id> for task '<T>': all 3 vertices authored from terrain + 10 m
    clearance; alts [a0, a1, a2]" - one per id 7/8/9. ZERO :807 "Partial" or "Fallback"
    WARN (ROW2R had three, all Partial); ZERO :1480 "got no reply within 10 s" WARN; ZERO
    :793 "request not sent" WARN; ZERO :1453 "partial (Complete=false)" INFO; ZERO Reason
    containing "echoed request point" (F2 echo tripwire) or "no usable sample" (F3
    horizontal-frame tripwire).
    Numbers on the `alts` list (authored altitude = terrain sample + 10 m clearance):
    - a0, the taskee's own position, has a comparator: within +/-20 m of (live alt + 10) =
      1050.6 m for id 7, 1126.7 m for id 8, 1141.4 m for id 9.
    - a1 and a2 have NO comparator - no run has ever produced a terrain height at the order
      points, and the Row 1/ROW2R records give altitudes only for the taskee positions. So
      they are held only to: a plausible Mojave terrain altitude, 500-1500 m (i.e. the
      authored value in [510, 1510]); AND horizontally within 50 m of the request vertex
      above, which the app's MaxHorizontalMismatchMeters gate already enforces (a sample
      outside it is rejected and would show up as a :807 Partial, so this half is tested by
      the absence of :807 plus the P1 shape-line coordinates). Their actual values are
      REPORTED, not adjudicated.
    - NO route whose three alts are all equal within 1 m to live + 60 (1100.6 / 1176.7 /
      1191.4) - that is the echo signature leaking past the 1 cm guard.
    - A :810 Note "taskee altitude not terrain-clamped" is not expected (ROW2R fired none);
      if it fires it is recorded with X, Y and the taskee type and does not by itself fail
      P2, but a Note ALONGSIDE an a0 outside the +/-20 m band is a MISS.

P3 - MOVEMENT UNAFFECTED (HIGH). This is the regression guard: the flatten changes what the
    waypoints' altitudes are, and must not change whether the units get there. 3/3 TASKCMPLT
    in both vrfc2simapp.log and reports-captured.log, with order-relative offsets within
    +/-10 s of Row 1's 117.3 (1.BdeHQ) / 129.2 (1222.MechPlt) / 183.8 (114.MechCoy) s
    (ROW2R: 118.0 / 130.1 / 185.2). Endpoints from the trace final POS within 1 m of P2c:
    1.BdeHQ 34.608416,-116.699996; 114.MechCoy 34.653915,-116.693388; 1222.MechPlt
    34.612956,-116.587783. POS==RPT <= 1 m x3 (run-manifest reportEvidence distanceM).
    settled true x3. 3 x "CreateRoute ... (3 pts)" + 3 x "MoveAlongRoute issued".
    bin64-vrfSim.log "empty route" 0. Wall 7 min 15 s +/- 45 s. earlyExit.fired true with
    closedUtc - allCompleteUtc in [60, 90] s.

P4 - THE BACK END SURVIVES AND TEARDOWN IS CLEAN (HIGH). No vrfSim* process with a
    MainWindowTitle matching `^vrfSim.*\.dmp$` at any point during or after the run (polled
    while the run is in flight and once after teardown); bin64-vrfSim.log carries lines
    stamped after clocks.orderPushedUtc (ROW2R: 3,635 of 11,283); no new .dmp file in
    C:\MAK\vrforces5.0.2\bin64; runner exit 0; StopVrf exit 0 and "VR-Forces is DOWN";
    RTI trio PIDs 41336 / 224608 / 76620 unchanged before and after; both observers on the
    stop-file path. WARN census in the app log: Row 1's 3 `fail:` lines (2 deserialize +
    1 "STOMP block reading cancelled") and 3 "Can't create data of type", 0 Exception, and -
    if P2 holds - ZERO `warn:` lines (ROW2R had three, all :807).

## 5. Falsifier branches - PRE-NAMED so nothing is retuned afterwards

Any branch below = STOP. Record it, write sec 6, report. Do NOT retune, do NOT re-run, do
NOT change code in this run. Exactly one variable moved and its answer is whatever the
artifacts say.

F1 - THE SHAPE LINE SAYS `1 sample(s)`. H-A IS DEAD: the back end genuinely returns one
     entry for a three-point profile request, and the old facade was reading it faithfully.
     Reading every entry of every set cannot manufacture samples that were never sent. This
     becomes a MAK question (how a DtIfRequestTerrainProfileInformation with three points
     and sendPartialInformation=false is answered), not a code change. Record the three
     shape lines VERBATIM with their coordinates, and record whether the one sample's index
     is 0 or something else. STOP.

F2 - THREE SAMPLES BUT THE INDICES REPEAT (e.g. `#0 #0 #0`, or `#7 #7 #7`). userData is NOT
     the point index for this reply - it is most likely the REQUEST ID or the SET index, and
     the trampoline's "index = userData when it parses" rule is mis-assigning every sample
     to the same vertex (which the authoring code then reduces to one usable vertex, exactly
     the ROW2R symptom). The fix is a one-line change to prefer the running index over
     userData - IT IS NOT TO BE MADE IN THIS RUN. Record the shape lines verbatim, note
     which value the repeated index equals (0? the request id 7/8/9? the set index?). STOP.

F3 - THREE DISTINCT SAMPLES AND STILL :807 Partial. The samples arrived and were rejected -
     by the 50 m horizontal gate or by the 1 cm echo guard. Read the shape line's per-sample
     lat/lon against the request vertices tabulated in sec 4 and record the horizontal
     offsets; record whether the Reason contains "echoed request point". A large horizontal
     offset on vertices 1-2 with a small one on vertex 0 would point at how the request's
     LATER points are packed or converted (the request frame is INFERRED geocentric, never
     stated - design sec 0), which is the next docs-first question. STOP.

F4 - ANY CRASH, TIMEOUT, OR INFRASTRUCTURE FAILURE. A :1480 "got no reply within 10 s" WARN,
     a :793 "request not sent" WARN, a back end on the MAK dump prompt, a runner non-zero
     exit, a StopVrf non-zero exit, an observer that never reached the stop-file path, or a
     P3 movement miss. Treat as infrastructure, not as an answer about H-A. If the dump
     prompt is up: `pwsh -File scripts\AnswerCrashDumpDialog.ps1` then
     `pwsh -File scripts\StopVrf.ps1` per RUNBOOK 0.5.12 (ruling: ALWAYS Yes), record the
     last back-end log line and its stamp relative to clocks.orderPushedUtc, the .dmp title
     and pid. STOP. After two infrastructure failures in this session, stop entirely.

A P1 MET + P2 MET result is the only PASS. A P1 MET + P2 MISSED result routes to F3. A
missed HIGH-confidence prediction is a STOP and is recorded as such, never retuned.

## 6. Outcome (written from the run directory artifacts, after the run)

VERDICT: H-A IS CONFIRMED AND THE MODE NOW DELIVERS ITS PURPOSE - but the run STOPS on a
missed HIGH prediction that is NOT infrastructure. The back end sends THREE samples for a
three-point profile request; ROW2R's "vertex 0 only" was a FACADE defect (entry [0] of each
set), not a back-end limitation. All three routes authored all three vertices from terrain,
zero WARN of any kind in the app log, and the sample coordinates match the request vertices
to the fifth decimal. P1 MET, P2 MET, P4 MET. P3 MISSED on ONE of its clauses: the aggregate
company 114.MechCoy completed at +198.1 s against Row 1's +183.8 s, a delta of +14.3 s and
outside P3's +/-10 s band; the other two taskees are inside 0.5 s. The delta is corroborated
by physical arrival, not just report lag (plateau onset 233.3 s vs Row 1's 219.2 s), so it is
a real movement change and the ONE variable is the only thing that moved. Per this prereg's
rules that is a STOP: nothing was retuned, re-run, killed, or code-changed.

Run 20260902T104832Z_run, launched 2026-09-02 10:48:32.197Z from main at e2e78d9 (this
prereg committed; the bridge deploy is 8410588). `Get-ChildItem env:Vrf__*` was exactly
`Vrf__GroundWaypointAltitudeMode=TerrainProfile` at launch (echoed before the runner started)
and empty again after. appNos 3697-3703 (vrfBackend 3697, vrfFrontend 3698, oraclePre 3699,
oracleTrace 3700, app 3701, rtiProbe 3702, createOneDiag 3703 - UNCONSUMED and BURNED, the
oracle gate passed); ledger wasValue 3697 / newValue 3704 / advanced true; ledger file after
the run CRLF 1871 / bare LF 0 / non-ASCII 0. Pre-launch inventory held in full (sec 3):
no vrfSim* / vrfGui / vrfLauncher / WatchVrf / ListenReports / VrfC2SimApp; RTI trio exactly
rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620; docker stp-server + c2sim_server Up;
env:Vrf__* empty; 10/10 main-checkout VrfBridge.dll copies A7504441. Runner exit 0; StopVrf
exit 0; wall 10:48:32.197Z -> 10:56:01.842Z = 449.6 s = 7 min 30 s (Row 1 / ROW2R: 7 min 15 s;
band was 7 min 15 s +/- 45 s). validityFlags: the single advisory pre-init INFO only; console
[WARN]/[FAIL] 0.

P1 - THE DISCRIMINATING PREDICTION - MET, and it is the finding. Exactly three :1466
   Information lines, ids 7/8/9, each with N = 3, three tokens, indices {0,1,2} DISTINCT, and
   no `#k:none`. Verbatim from vrfc2simapp.log lines 53/55/57:

     Terrain profile reply 7: 3 sample(s) [#0:34.61296,-116.60049,1040.6 #1:34.61296,-116.59417,1033.9 #2:34.61296,-116.58786,1026.7].
     Terrain profile reply 8: 3 sample(s) [#0:34.64763,-116.69339,1116.7 #1:34.65263,-116.69339,1116.8 #2:34.65763,-116.69339,1116.9].
     Terrain profile reply 9: 3 sample(s) [#0:34.60842,-116.71269,1131.4 #1:34.60842,-116.70637,1126.3 #2:34.60842,-116.70006,1121.1].

   Against sec 4's request-vertex table the horizontal agreement is exact to the logged 5
   decimals: id 7 #1 34.61296,-116.59417 = v1 34.612956,-116.594174 and #2 -116.58786 =
   -116.587860; id 8 #1 34.65263 = 34.652629 and #2 34.65763 = 34.657629; id 9 #1 -116.70637
   = -116.706372 and #2 -116.70006 = -116.700059. Each #0 sits at the taskee's own live
   position. So the samples arrive IN REQUEST ORDER, one per point, and userData parsed to a
   distinct index for every one of them - F2 (repeated indices) did not fire.
   WHAT THIS SETTLES about ROW2R's three open hypotheses: (i) "the back end returned fewer
   samples than requested" is REFUTED for the back end and TRUE OF THE OLD FACADE - the wire
   carries three, the old `pairs[i][0]` read exposed one. (ii) and (iii) are refuted: all
   three samples are near their own vertices and none was rejected.

P2 - ALL THREE VERTICES AUTHORED - MET. Exactly three :802 Information lines, verbatim
   (vrfc2simapp.log 59/63/67):

     Terrain profile 7 for task 'T_R5_PL1': all 3 vertices authored from terrain + 10 m clearance; alts [1050.6, 1043.9, 1036.7].
     Terrain profile 8 for task 'T_R5_CO1': all 3 vertices authored from terrain + 10 m clearance; alts [1126.7, 1126.8, 1126.9].
     Terrain profile 9 for task 'T_R5_TK1': all 3 vertices authored from terrain + 10 m clearance; alts [1141.4, 1136.3, 1131.1].

   ZERO :807 Partial/Fallback (ROW2R had three), ZERO :1480 timeout, ZERO :793 "not sent",
   ZERO :1453 "partial (Complete=false)", ZERO :810 Note, ZERO "echoed request point", ZERO
   "no usable sample". The app log contains ZERO `warn:` lines in total.
   Numbers. a0 hits its band EXACTLY, not merely within +/-20 m: id 7 1050.6 vs predicted
   1050.6 (delta 0.0); id 8 1126.7 vs 1126.7 (0.0); id 9 1141.4 vs 1141.4 (0.0). The reason
   it is exact is itself corroborating: each #0 terrain sample (1040.6 / 1116.7 / 1131.4)
   equals the taskee's Row 1 live altitude to 0.1 m, which is what a GROUND-CLAMPED entity
   standing on the surface should read - the back end is reporting the surface, not the
   request. Echo signature ABSENT: no route's alts are all equal, and none equals live + 60
   (1100.6 / 1176.7 / 1191.4); the request points carried live + 50, and the returned
   samples sit 50 m below them.
   a1 / a2, REPORTED not adjudicated (no comparator exists): 1043.9 / 1036.7 (id 7),
   1126.8 / 1126.9 (id 8), 1136.3 / 1131.1 (id 9). All inside the [510, 1510] plausibility
   band. The relief they describe is coherent - id 7's route runs downhill 1040.6 -> 1033.9
   -> 1026.7 over 1.1 km, and 1026.7 + 0 is within 0.1 m of where 1222.MechPlt actually
   comes to rest (final POS alt 1026.6); id 9 runs downhill 1131.4 -> 1126.3 -> 1121.1 and
   1.BdeHQ rests at 1121.1; id 8 is essentially flat at 1116.7 -> 1116.9 and 114.MechCoy
   rests at 1116.8. Three independent agreements between a terrain sample at a route
   endpoint and the entity's own clamped resting altitude at that endpoint.

P3 - MOVEMENT UNAFFECTED - MISSED (one clause of five). Everything except the completion
   offsets is identical to Row 1:
   - TASKCMPLT counts 3 (vrfc2simapp.log) and 3 (reports-captured.log).
   - Endpoints from the trace final POS (t=294.4): 1.BdeHQ 34.608416,-116.699994 alt 1121.1;
     114.MechCoy 34.653915,-116.693388 alt 1116.8; 1222.MechPlt 34.612956,-116.587783 alt
     1026.6 - identical to Row 1's final POS to all six logged decimals, i.e. 0.00 m apart,
     and the resting ALTITUDES are unchanged too (terrain-authored waypoints did not move
     where the entities end up: VRF ground-clamps the entity regardless).
   - POS==RPT 0.0 / 0.0 / 0.0 m (run-manifest oracle.earlyExit.reportEvidence distanceM),
     satisfied x3, reason "post-completion RPT agrees with POS". settled true x3.
   - 3 x "CreateRoute '<T> ROUTE' (3 pts)" + 3 x "Route '<T> ROUTE' created; MoveAlongRoute
     issued" (VRF_UUIDs 41f1fdf1 / a6436ffc / 10f03d88). bin64-vrfSim.log "empty route" 0.
   - earlyExit.fired true; allCompleteUtc 10:54:17.371Z, closedUtc 10:55:22.934Z -> 65.6 s
     (band [60, 90]); windowSecsUsed 236.2 of the 420 cap; completionLinesSeen 3.
   - Wall 7 min 30 s, inside the +/- 45 s band.
   THE MISS. Offsets of the TASKCMPLT report receipts (reports-captured.log `[hh:mm:ss.fff]`
   stamps) from clocks.orderPushedUtc 2026-09-02T10:50:56.392Z - the same measure Row 1 sec 6
   A used:
     1.BdeHQ      (task ...0003)  +117.5 s   Row 1 +117.3   ROW2R +118.0   delta +0.2  OK
     1222.MechPlt (task ...0001)  +129.6 s   Row 1 +129.2   ROW2R +130.1   delta +0.4  OK
     114.MechCoy  (task ...0002)  +198.1 s   Row 1 +183.8   ROW2R +185.2   delta +14.3 MISS
   The band was +/-10 s. The company is 14.3 s late against Row 1 and 12.9 s late against
   ROW2R. This is NOT report lag: the trace's own plateau onset (first POS within 1 m of the
   final POS) moves the same way - 114.MechCoy 233.3 s vs Row 1 219.2 and ROW2R 221.6
   (+14.1 / +11.7), while 1.BdeHQ is 148.0 vs 148.0 / 150.1 and 1222.MechPlt 162.3 vs
   160.1 / 162.4. The entity physically arrived later. Trace TSK completionT, REPORTED:
   145.7 / 157.9 / 226.4 (Row 1: 145.5 / 157.4 / 212.0; ROW2R: 146.9 / 159.0 / 214.1) - the
   same +14 s on the company alone.
   The four-run spread for 114.MechCoy before this run was 182.1 (CONFIRM2) / 183.8 (Row 1) /
   185.2 (ROW2R), i.e. about 3 s; +198.1 sits far outside it. n=1, so "run-to-run variance"
   is not excluded by arithmetic alone, but it is not a comfortable reading.
   WHAT CHANGED FOR THIS TAKEE SPECIFICALLY (verified from the artifacts, cause NOT claimed):
   114.MechCoy is the only AGGREGATE among the three taskees (it has subordinates
   1141/1142/1143.MechPlt). Between ROW2R and this run its vertices 1 and 2 dropped from the
   Live altitude 1166.7 m to the terrain-authored 1126.8 / 1126.9 m - a 40 m drop - and its
   route is the near-flat one. The two non-aggregate taskees had comparable 45-50 m drops on
   their vertices 1-2 (1181.4 -> 1136.3/1131.1 and 1090.6 -> 1043.9/1036.7) and did NOT
   change their timing at all. So the artifacts associate the delay with the AGGREGATE, not
   with the size of the altitude change. Deciding whether that is aggregate path-planning
   responding to lower waypoints, an aggregate-formation effect, or variance is the
   supervisor's docs-first call - no probe, no retune, no re-run was made here.

P4 - BACK END SURVIVES, TEARDOWN CLEAN - MET. bin64-vrfSim.log 14,158 lines / 1,274,790
   bytes, of which 5,464 are stamped 06:51 local or later, i.e. after the order push at
   06:50:56 local (10:50:56.392Z); last line "Exception in destroyFederationExecution:
   Federation Execution Already Exists.[Wed Sep  2 06:55:58 2026]" - the normal teardown
   tail, same as ROW2R's. NO new .dmp in C:\MAK\vrforces5.0.2\bin64: the newest is still
   ROW2R-era vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp, 598,441 B, 2026-09-02 06:00:18
   local. AnswerCrashDumpDialog.ps1 was never needed and never run. Runner exit 0; StopVrf
   exit 0 with "VR-Forces is DOWN (graceful quit; no process was force-killed)."; post-run
   sweep found 0 vrfSim* / vrfGui / WatchVrf / ListenReports / VrfC2SimApp processes; RTI
   trio 41336 / 224608 / 76620 unchanged before and after and explicitly reported preserved
   by StopVrf. Both observers exited 0 on the stop-file path. bin64-vrfSim.log counts
   "Waiting for nav data" 0 / "empty route" 0 / "Can't find entity route" 0 / "invalid
   formation name" 1 (baseline) = 0/0/0/1, SocketException 0. App-log census: 3 `fail:`
   (C2SIMSDK, the Row 1 three), 3 "Can't create data of type", 0 Exception, and ZERO `warn:`
   - strictly better than ROW2R's three :807 WARNs and identical to Row 1's census.
   The 6 create lines read "Create-altitude mode=Live: GROUND unit <name> created at safe
   MSL 10000 m (original create alt 1000 m); parity post-create SetAltitude SKIPPED" -
   expected, per the ROW2R correction (VrfC2SimService.cs:439 hard-codes "mode=Live" for the
   whole live-like family).
   METHOD DEVIATION, recorded: P4 asked for window-title polling for `^vrfSim.*\.dmp$` while
   the run was in flight. The runner was executed in the FOREGROUND with a 15-minute
   timeout, so no in-flight polling happened. The substitute evidence is stronger on the
   question that matters (did the back end die?): the back-end log runs continuously from
   scenario load through teardown with 5,464 post-order lines, the units moved and completed,
   no dump file was created, and StopVrf quit a live front end gracefully. A crashed federate
   produces none of those.

WHAT THE BACK-END LOG SAYS ABOUT THE REQUEST (recorded, not investigated): unchanged from
ROW2R - at notify level 3, bin64-vrfSim.log contains ZERO lines matching `IfRequest`,
`TerrainProfile`, `terrain profile` or `IntersectionInformation`. The back end answers the
request correctly and logs nothing about it at this verbosity.

FALSIFIER BRANCH TAKEN: none of F1 / F2 / F3. F1 is refuted (3 samples, not 1); F2 is refuted
(indices 0,1,2 distinct on all three replies); F3 is refuted (three distinct samples AND no
:807). The P3 miss is listed under F4 by the letter of sec 5, and that is a PREREG DEFECT
worth naming: F4 bundles "a P3 movement miss" with crashes and timeouts under the heading
"treat as infrastructure, not as an answer about H-A". Here the infrastructure was clean on
every measure and the movement delta is a substantive result about the mode's effect on an
aggregate. The correct disposition is the one taken: STOP, record, report, do not retune -
but the delta is EVIDENCE, not noise, and it should be adjudicated as such rather than filed
as an infrastructure failure. A future prereg should give the movement-regression clause its
own branch.

ADJUDICATION AGAINST THE VARIABLE (verified vs. inferred):
- VERIFIED: the back end returns one sample per requested point, in request order, with
  userData parsing to the point index. ROW2R's Partial replies were caused by the facade
  reading only entry [0] of each response set. H-A ("all points land in one set") is
  CONFIRMED insofar as the flattened walk recovers all three; the artifacts do not
  distinguish "one set of three" from "three sets of one", because the trampoline flattens
  both identically. That distinction is unresolved and does not matter to the app.
- VERIFIED: the terrain samples are real surface heights, not echoes of the request points -
  they sit 50 m below the request altitudes and agree to 0.1 m with the clamped resting
  altitude of an entity standing at the same place (three independent agreements).
- VERIFIED: the request-point frame inference (geocentric) now has much stronger support
  than ROW2R's single vertex-0 hit: six order-point samples, all landing on their own
  request vertex to five decimals.
- VERIFIED: the mode delivers its purpose - the order-point waypoints now carry terrain +
  10 m instead of live + 50 m. Design sec 7's Row 2 check 1 is MET for the first time.
- VERIFIED: 114.MechCoy, the aggregate, arrives and completes about 14 s later than in every
  prior run, by two independent measures (report receipt and trace plateau).
- INFERRED, NOT CLAIMED: why. Candidate readings, none tested here - (a) aggregate path
  planning or formation keeping responds to the lowered waypoint altitudes; (b) an
  aggregate-specific interaction with waypoint altitude that the two individual entities do
  not have; (c) run-to-run variance larger for the aggregate than the 3 s seen in three
  prior runs. Nothing in this run's artifacts separates them.
- CONSEQUENCE: the mode is now FUNCTIONAL and its remaining question is a movement-timing
  regression on the aggregate, not a reply-plumbing question.

STATE LEFT BEHIND (for the next session):
- Processes: VR-Forces DOWN, nothing force-killed; no observers; RTI trio rtiAssistant 41336
  / rtiexec 224608 / rtiForwarder 76620 resident and untouched; docker stp-server +
  c2sim_server Up.
- Binaries: VrfBridge.dll A7504441F421B668D10F5AFD8B4FD71110002D13FE6ABAE0DB576C7C209236F5
  deployed to 10/10 main-checkout copies; the 28E993FE set is backed up in
  src/VrfBridge/build/Release/bak-20260902-28e993fe/.
- Ledger: marker is now 3704 (this run consumed 3697-3703; 3703 unconsumed and BURNED).
- Dumps: none created by this run. No file under C:\MAK was written by this session.
