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

(to be filled in after the run)
