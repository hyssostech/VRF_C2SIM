# PREREG TERRAIN ROW 2 - MODE: same bridge 28E993FE, Vrf__GroundWaypointAltitudeMode=TerrainProfile - registered 2026-09-02 01:17Z, BEFORE launch

Sources read for this prereg (docs first, per the 2026-09-01 directive):
- docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 7 "Row 2 - MODE" (checks 1-3 quoted
  verbatim in sec 4 below), sec 3 (design), sec 8 (offline gate results), sec 6 DEPLOY RECORD.
- docs/experiments/REVIEW_TERRAIN_PROFILE_BRANCH_2026-09-01.md: "The request-point frame:
  GENUINELY UNSTATED" (no MAK header states the frame of
  DtIfRequestTerrainProfileInformation::setPoints; the reply is documented geocentric and the
  request is INFERRED geocentric because createRoute vertices are geocentric,
  vrfRemoteController.h:1006-1007); F2 echo blind spot (1 cm guard: a sample within 0.01 m of
  the REQUEST vertex altitude is rejected as "echoed request point"); F3 (a vertex-0 vertical
  gap > 100 m with horizontal agreement is an INFO Note, NOT a frame falsifier; a real frame
  error shows as HORIZONTAL displacement > 50 m = "no usable sample"). Competing hypotheses
  named there: (i) the request expects geodetic/topographic points -> samples land nowhere
  near the vertices -> Fallback "no usable sample for any vertex"; (ii) the back end echoes
  the request points -> Fallback with "echoed request point(s)" OR a success line whose alts
  all equal live + 60.
- C:\MAK\vrforces5.0.2\include\vrfmsgs\ifRequestTerrainProfileInformation.h (read-only):
  "Once the request is made, either a full or partial responses will be give back (depending
  on the setting)"; setSendPartialInformation "When partial information is requested, a
  series of responses will be sent ... The response will be in the form of a
  DtIfIntersectionInformationResponse where the user data of each information response is
  the index of the terrain profile request"; `bool mySendPartialInformation; // Default is
  true` (the app sets it false); `std::vector<DtVector> myPoints` - no frame stated.
  vrfmsgs\ifIntersectionInformationResponse.h: "Note that all point information returned is
  in geocentric".
- Code paths that produce the lines adjudicated below: src/VrfC2SimApp/VrfC2SimService.cs
  :787-816 (request, INFO :802 success / WARN :807 Partial|Fallback / INFO :810 Note / INFO
  :813 "request sent"), :1453 partial reply INFO, :1474 timeout WARN;
  src/VrfC2SimApp/TerrainVertexAuthoring.cs (Mode Terrain|Partial|Fallback; Reasons "no reply
  (timeout)", "empty reply", "no usable sample for any vertex", "vertices ... had no usable
  sample - kept Live altitude", suffix "(N echoed request point(s) at vertex ... rejected)";
  Note "taskee altitude not terrain-clamped: live X m vs terrain Y"); VrfSettings.cs:175-178,
  188 (TerrainClearanceMeters 10, TerrainProfileTimeoutSeconds 10,
  GroundWaypointLiveClearanceMeters 50). Live vertex altitude = taskee live alt + 50
  (VrfC2SimService.cs:737-738), so an echo would author live + 60.
- Reference runs: Row 1 CONTROL 20260902T010704Z (docs/experiments/PREREG_TERRAIN_ROW1_CONTROL_
  2026-09-02.md sec 6, ALL MET) and CONFIRM2 20260902T003710Z. ASCII only. The C++ repo is a
  frozen oracle; nothing under C:\MAK is written.

## 1. The ONE variable: the altitude mode

Environment `Vrf__GroundWaypointAltitudeMode=TerrainProfile` set in the pwsh session that
invokes the runner (runner Stage 6b Start-External inherits the process environment - the
same passthrough P1 used for Fixed100 and P3 used for Vrf__TimeMultiplier=5; the runner
saves/restores only Vrf__ApplicationNumber). `Vrf__TimeMultiplier` NOT set (1x). Everything
else IDENTICAL to Row 1: bridge 28E993FE (10/10 copies, unchanged since the deploy record -
re-hashed before launch), Ijwhost 38255036, app build from e1fdbbd, init/order files,
RealTemplates, stock templates, NavArea disabled, notify level 3, `-RunSecs 420
-StopWhenComplete`, SettleHoldSecs 60. Unit creation is identical in Live and TerrainProfile
(VrfC2SimService.cs:431 "Live or TerrainProfile (identical creation)"), so the 6 create lines
still read mode=TerrainProfile but with the same altitudes as Row 1.

## 2. Invocation (main checkout, VRF_C2SIM, pwsh)

    $env:Vrf__GroundWaypointAltitudeMode = 'TerrainProfile'
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_NoComments.xml `
        -RunSecs 420 -StopWhenComplete

Adjudication from the run directory only (scratchpad adjudicate.py, same version that
adjudicated Row 1). Ledger: marker `*** NEXT FREE: 3683 ***` before the run; 7 numbers ->
expected wasValue 3683 / newValue 3690.

## 3. Pre-launch inventory (must hold, else STOP - never kill)

VR-Forces DOWN, no WatchVrf / ListenReports / VrfC2SimApp, RTI trio 41336 / 224608 / 76620
resident and unchanged since Row 1, docker stp-server + c2sim_server Up, main checkout at
the commit carrying this prereg, `Get-ChildItem env:Vrf__*` shows EXACTLY
GroundWaypointAltitudeMode=TerrainProfile and nothing else.

## 4. Design sec 7 Row 2 checks (verbatim)

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

## 5. Predictions with numbers, and what counts as a miss

Row 1 reference numbers (its sec 6): taskee live altitudes at task time (POS alt at first
sample) 1.BdeHQ 1131.4 m, 114.MechCoy 1116.7 m, 1222.MechPlt 1040.6 m; Live vertex
altitude = live + 50 -> 1181.4 / 1166.7 / 1090.6; echo signature = live + 60 ->
1191.4 / 1176.7 / 1100.6 (all vertices of one route share the taskee's live
altitude under Live, so an echo shows as three EQUAL alts at that value).

A. REQUEST PATH ENTERED (HIGH). Exactly 3 INFO lines "Task '<T>': terrain profile request
   <Id> sent for 3 vertices; dispatch deferred to the reply (timeout 10 s -> Live fallback)"
   (one per T_R5_PL1 / T_R5_CO1 / T_R5_TK1; the routes are 3-point: origin + 2 order
   points, as in Row 1's "CreateRoute ... (3 pts)"). ZERO "terrain profile request not sent"
   WARN (bridge returned requestId 0 = falsifier: the additive native path failed).
B. REPLY, COMPLETE, IN TIME (HIGH). Exactly 3 INFO lines "Terrain profile <Id> for task
   '<T>': all 3 vertices authored from terrain + 10 m clearance; alts [a0, a1, a2]". The
   app log carries no timestamps, so "within 10 s" is adjudicated indirectly: ZERO WARN
   :1474 "got no reply within 10 s" (the tick sweep logs it the moment the deadline passes)
   AND the TASKCMPLT offsets in prediction D's band (a reply late by seconds would push them
   out). ZERO WARN :807 (Partial or Fallback), ZERO :1453 "partial (Complete=false" (if one
   appears AND the complete reply follows, design check 1 says the run stays valid - it is
   then REPORTED as an unexplained-symptom note, not a pass). Order of the three success
   lines in the log is REPORTED (expected: the same order as the requests).
C. ALTITUDES ARE TERRAIN, NOT ECHO (HIGH). For each route: vertex-0 alt (the taskee's own
   position) within +/-20 m of (taskee live alt + 10) = 1141.4 / 1126.7 / 1050.6 m
   (design sec 7 prediction: terrain within ~20 m of each clamped taskee's live altitude);
   NO route whose three alts are all equal to live + 60 within 1 m (1191.4 /
   1176.7 / 1100.6); vertices 1-2 (555 m - 1 km away) plausibly differ from vertex 0
   by metres to tens of metres (Mojave relief) and are REPORTED. An INFO Note "taskee
   altitude not terrain-clamped: live X m vs terrain Y" is NOT expected for these
   ground-clamped taskees (Row 1 rest alts sit on the surface); if it fires it is recorded
   with X, Y and taskee type per check 2b and does not by itself fail the row - but a Note
   alongside a vertex-0 alt outside the +/-20 m band is a MISS of C.
D. MOVEMENT IDENTICAL TO ROW 1 (HIGH). 3/3 TASKCMPLT with order-relative offsets within
   +/-10 s of Row 1's 117.3 / 129.2 / 183.8 s (the dispatch is deferred to the
   reply, so the sub-second reply latency must not move the offsets out of band; the
   trace's TSK lines are late Object Console observations - CONFIRM2 TSK 145.4 s for 1.BdeHQ
   vs order push at trace t=31.9 s - and are REPORTED, not adjudicated); endpoints
   within 2 m of P2c; POS==RPT <= 2 m x3; settled per 4a.1; 3 CreateRoute (3 pts) + 3
   MoveAlongRoute issued; company working offset routes non-empty (bin64-vrfSim.log 0
   "empty route").
E. HYGIENE (HIGH). As Row 1: StopVrf exit 0, VR-Forces down, RTI PIDs unchanged, both
   observers on the stop-file path, vrfSim.log counts 0/0/0/1, no SocketException. WARN
   census = Row 1's (3 SDK deserialize `fail:` + 3 "Can't create data of type") and NOTHING
   ELSE - in particular no WARN from :793 / :807 / :1474. Wall <= 9 min. Runner exit 0.
   Create lines: 6 x "mode=TerrainProfile" with the same altitudes as Row 1's mode=Live lines.
F. EARLY EXIT (HIGH). earlyExit.fired true, reportEvidence x3 distanceM <= 2, closedUtc -
   allCompleteUtc in [60, 90] s.

Falsifiers (any one = STOP and report with the exact log lines; do NOT retune, do NOT re-run):
any :793 / :807 / :1474 WARN; a Reason containing "no usable sample" (frame) or "echoed
request point" (echo); a success line with alts == live + 60 (echo past the guard); a
vertex-0 alt outside +/-20 m of live + 10; fewer than 3 request or 3 success lines; any
Row-1 movement/hygiene prediction missed. A `:1453` partial INFO followed by a complete
reply is NOT a stop but is reported as unexplained.

## 6. Outcome (to be written from the artifacts, after the run)

(pending)
