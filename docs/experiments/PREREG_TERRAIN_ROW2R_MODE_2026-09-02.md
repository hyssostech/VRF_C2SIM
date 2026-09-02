# PREREG TERRAIN ROW 2R - MODE REPEAT: the UNCHANGED repeat of Row 2 after the back-end crash - registered 2026-09-02, BEFORE launch

WHAT THIS IS: Row 2 run again, byte-for-byte the same experiment, with a new run id. Row 2
(docs/experiments/PREREG_TERRAIN_ROW2_MODE_2026-09-02.md, run 20260902T011908Z) is
UNADJUDICATED, not refuted: its sec 6 records that the VR-Forces back end (pid 70668) took
a FATAL ERROR at 21:21:26 local - 6 s BEFORE the order push at 01:21:32.24Z and therefore
before RequestTerrainProfile was ever called - and parked on the MAK crash-dump prompt, so
the three terrain requests (ids 7/8/9) went to a dead federate, timed out at 10 s, and fell
back to Live. The mode variable was never exercised against a live back end. Sections 1-5
below are Row 2's sections 1-5 VERBATIM (the ONE variable is unchanged: the same
Vrf__GroundWaypointAltitudeMode=TerrainProfile, the same invocation, the same inventory, the
same predictions A-F with the same numbers, the same falsifiers); the only additions are the
ledger numbers for this run's appNos and a new prediction G that the back end survives.
NOTHING was retuned, rebuilt or reconfigured between Row 2 and this run - same bridge
28E993FE, same app build, same data files, same switches.

Since Row 2 the machine state changed in exactly two ways, neither of them an experimental
variable: (1) the crashed federate 70668 was answered Yes on the dump prompt (dump
C:\MAK\vrforces5.0.2\bin64\vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp, 598441 bytes) and
vrfGui was quit via StopVrf (exit 0), so VR-Forces is DOWN; (2) scripts/AnswerCrashDumpDialog.ps1
and RUNBOOK sec 0.5.12 were added (tooling for the dump prompt; not loaded by the app or the
bridge). The RTI trio rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 is resident and
untouched.

Additional sources read for this repeat (docs first, per the 2026-09-01 directive):
- docs/experiments/PREREG_TERRAIN_ROW2_MODE_2026-09-02.md, ALL sections, esp. sec 6 Outcome
  (the crash sequence, the state left behind, the verified-vs-inferred adjudication).
- docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 6 DEPLOY RECORD (bridge 28E993FE,
  one-hash proof over 10 copies) and sec 7 (Row 2 checks 1-3, quoted verbatim in sec 4
  below; the ROW 2 RESULT paragraph: "repeat Row 2 on a clean boot (same prereg, new run
  id) - NO retune").
- docs/experiments/PREREG_TERRAIN_ROW1_CONTROL_2026-09-02.md sec 6 (the Row 1 comparators
  used by predictions C/D/E/F: offsets +117.3 / +129.2 / +183.8 s, endpoints within 0.09 m
  of P2c, POS==RPT 0.0 x3, start alts 1131.4 / 1116.7 / 1040.6 m, WARN census 3+3+0, wall
  7 min 15 s, vrfSim counts 0/0/0/1).
- docs/RUNBOOK.md sec 0.5.0 (pre-flight inventory; never kill; the -AllowExistingVrf
  false-READY trap), sec 0.5.9 (StopVrf exit codes 0/2/3/4/5), sec 0.5.11 (the runner
  switches -RunSecs / -StopWhenComplete / -SettleHoldSecs and the stop-file observer path),
  sec 0.5.12 (NEW: a crashed back end parked on the MAK dump prompt - the `.dmp` window
  title signature, the ALWAYS-YES ruling, scripts/AnswerCrashDumpDialog.ps1 exit codes).

Row 2's own source list, unchanged, follows.

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
adjudicated Row 1). Ledger: marker `*** NEXT FREE: 3690 ***` before the run (verified in
docs/OPUS_EXECUTION_PLAN.md, the single authoritative marker); 7 numbers -> expected
wasValue 3690 / newValue 3697, appNos 3690-3696.

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

G. THE BACK END SURVIVES THE RUN (HIGH - NEW for this repeat; Row 2's back end took a
   fatal error at 21:21:26 local, 6 s BEFORE the order push, and never answered). The
   VR-Forces back end is still writing to its log after the terrain requests were sent:
   the captured copy of C:\MAK\vrforces5.0.2\bin64\vrfSim.log in the run directory
   (bin64-vrfSim.log) carries lines stamped AFTER clocks.orderPushedUtc (local stamps are
   UTC-4; Row 1's back end logged hundreds of lines per second through the whole run,
   11,099 lines total, while Row 2's log stopped dead at 5,318 lines with creation), AND
   no vrfSim* process shows a MainWindowTitle ending in `.dmp` at any point during or
   after the run (polled about every 60 s while the run is in flight, and once after
   teardown; RUNBOOK 0.5.12 is the signature: `vrfSim5.0.2-MSVC++15.0_64-249613-<pid>.dmp`
   = MAK's crash handler = the federate is already dead).

Falsifiers (any one = STOP and report with the exact log lines; do NOT retune, do NOT re-run):
any :793 / :807 / :1474 WARN; a Reason containing "no usable sample" (frame) or "echoed
request point" (echo); a success line with alts == live + 60 (echo past the guard); a
vertex-0 alt outside +/-20 m of live + 10; fewer than 3 request or 3 success lines; any
Row-1 movement/hygiene prediction missed. A `:1453` partial INFO followed by a complete
reply is NOT a stop but is reported as unexplained.

ALSO A STOP (new for this repeat): if G is MISSED - the back end crashes again with the
mode set - THAT IS THE FINDING OF THIS RUN. Do NOT retune, do NOT re-run, do NOT change
code. Record the crash (last back-end log line and its stamp relative to
clocks.orderPushedUtc, the .dmp window title and pid), answer the dump prompt per RUNBOOK
0.5.12 (ruling: ALWAYS Yes), tear down with StopVrf, and report. Two runs crashing at the
same phase with the mode set - where Row 1 under Live did not - is itself the evidence the
supervisor takes docs-first; one more run would not add to it and is not authorized.

## 6. Outcome (written from the run directory artifacts, after the run)

VERDICT: STOP - FALSIFIED, AND THIS TIME IT IS ABOUT THE MODE. The back end lived through
the whole run (prediction G MET), the three terrain requests were sent AND ANSWERED - no
timeout, no Fallback - but every reply was PARTIAL: vertex 0 got a usable terrain sample,
vertices 1 and 2 did not. Prediction B MISSED (3 x :807 Partial WARN, the named falsifier);
prediction C is UNADJUDICABLE on numbers (no :802 success line, so no `alts [...]` list
exists in any artifact); predictions A, D, F MET; E MET except its WARN-census clause, which
the 3 Partial WARNs break by construction. Movement was UNAFFECTED - 3/3 TASKCMPLT at Row 1's
offsets, endpoints within 0.27 m of P2c - because the Partial path keeps the Live altitude for
the vertices it could not author. Nothing was retuned, re-run, killed, or code-inspected
beyond attributing log strings; per the prereg's rules this run stops here.

Run 20260902T101431Z_run, launched 2026-09-02 10:14:31.952Z from main at 4fc7e4d (this
prereg committed), `Get-ChildItem env:Vrf__*` = exactly `Vrf__GroundWaypointAltitudeMode=
TerrainProfile` (echoed into the run console log before the runner started). appNos 3690-3696
(vrfBackend 3690, vrfFrontend 3691, oraclePre 3692, oracleTrace 3693, app 3694, rtiProbe 3695,
createOneDiag 3696); ledger wasValue 3690 / newValue 3697 / advanced true; ledger file after
the run CRLF 1858 / bare LF 0. Pre-launch inventory held: no vrfSim* / vrfGui / WatchVrf /
ListenReports / VrfC2SimApp, RTI trio exactly rtiAssistant 41336 / rtiexec 224608 /
rtiForwarder 76620, docker stp-server + c2sim_server Up, env:Vrf__* empty, 10/10 main-checkout
VrfBridge.dll copies re-hashed 28E993FE (the only other copies on disk are inside
.claude/worktrees/, which this run does not load). Runner exit 0; StopVrf exit 0; wall
10:14:31.952Z -> 10:21:46.589Z = 434.6 s = 7 min 15 s (Row 1: 7 min 15 s). validityFlags: the
single advisory pre-init INFO only.

A. REQUEST PATH ENTERED - MET. Exactly 3, ids 7/8/9, one per task, no :793 WARN:
   "Task 'T_R5_PL1': terrain profile request 7 sent for 3 vertices; dispatch deferred to the
   reply (timeout 10 s -> Live fallback)." (and 8 / T_R5_CO1, 9 / T_R5_TK1).
B. REPLY, COMPLETE, IN TIME - MISSED (the falsifier). ZERO :802 "all 3 vertices authored"
   lines. ZERO :1474 "got no reply within 10 s" WARN - so the replies DID arrive inside the
   10 s window, unlike Row 2. ZERO :1453 "partial (Complete=false)" INFO - the reply was a
   single complete response, not a partial-information series. What fired instead was :807,
   three times, verbatim:
     warn: VrfC2Sim[0]
           Terrain profile 7 for task 'T_R5_PL1': Partial - vertices 1,2 had no usable
           sample - kept Live altitude; 2 vertex(es) keep the Live altitude.
     warn: VrfC2Sim[0]
           Terrain profile 8 for task 'T_R5_CO1': Partial - vertices 1,2 had no usable
           sample - kept Live altitude; 2 vertex(es) keep the Live altitude.
     warn: VrfC2Sim[0]
           Terrain profile 9 for task 'T_R5_TK1': Partial - vertices 1,2 had no usable
           sample - kept Live altitude; 2 vertex(es) keep the Live altitude.
   Same shape for all three routes: vertex 0 authored from terrain, vertices 1 and 2 not.
C. ALTITUDES ARE TERRAIN, NOT ECHO - UNADJUDICABLE, recorded as a MISS by absence. The
   `alts [a0, a1, a2]` list only exists on the :802 success line, which never fired, so the
   authored vertex-0 altitude is in no artifact and the +/-20 m band cannot be tested. What
   the artifacts DO settle, negatively: NO Reason anywhere contains "echoed request point"
   (the F2 echo tripwire did not fire) and NO Reason contains "no usable sample for any
   vertex" (the F3 HORIZONTAL frame falsifier did not fire) - vertex 0's sample was accepted,
   i.e. it landed within 50 m of the requested vertex, which is evidence FOR the
   geocentric-request inference rather than against it. No :810 "taskee altitude not
   terrain-clamped" Note fired either.
D. MOVEMENT IDENTICAL TO ROW 1 - MET. 3 TASKCMPLT in vrfc2simapp.log and 3 in
   reports-captured.log. Offsets from clocks.orderPushedUtc 2026-09-02T10:16:55.043Z:
   1.BdeHQ +118.0 s (Row 1 +117.3, delta +0.7), 1222.MechPlt +130.1 s (+129.2, +0.9),
   114.MechCoy +185.2 s (+183.8, +1.4) - all inside +/-10 s. Endpoints from the trace final
   POS (t=280.9): 1.BdeHQ 34.608416,-116.699996 (0.27 m from P2c), 114.MechCoy
   34.653915,-116.693388 (0.00 m), 1222.MechPlt 34.612956,-116.587783 (0.09 m). POS==RPT
   0.0 / 0.0 / 0.0 m (run-manifest reportEvidence distanceM). settled true x3 (last-3-POS
   spread 0.00 m each). Plateau onsets 150.1 / 221.6 / 162.4 s (Row 1: 148.0 / 219.2 /
   160.1, same order 1.BdeHQ / 114.MechCoy / 1222.MechPlt). 3 x "CreateRoute ... (3 pts)"
   and 3 x "Route '...' created; MoveAlongRoute issued" (9bd6676f / e5f6ff0f / 7d6ce5ba).
   bin64-vrfSim.log "empty route" 0. Trace TSK lines (REPORTED, not adjudicated): 146.9 /
   159 / 214.1 s.
E. HYGIENE - MET EXCEPT THE WARN CENSUS, which B's Partials break. StopVrf exit 0 and
   "VR-Forces is DOWN (graceful quit; no process was force-killed)."; RTI trio 41336 /
   224608 / 76620 unchanged before and after (start times 14:34 / 15:08 / 15:09 local, same
   as Row 1's); both observers on the stop-file path ("# STOP requested via stop-file at
   t=313s (duration cap was 980s)" -> "[OK] resigned cleanly."; ListenReports "stop requested
   via stop-file at t=316.1s ... - disconnecting", "captured 31 reports" - Row 1 also 31).
   bin64-vrfSim.log counts: "Waiting for nav data" 0, "empty route" 0, "Can't find entity
   route" 0, "invalid formation name" 1 (baseline) = 0/0/0/1; SocketException / "Only one
   usage" / "Connection error" 0/0/0. App-log census: 3 `fail:` lines - 2 deserialize + 1
   "STOMP block reading cancelled" - byte-for-byte the same three Row 1 has; 3 "Can't create
   data of type"; 0 Exception; and THREE `warn:` lines, all :807, where Row 1 had ZERO. That
   is the only census delta and it is the finding, not noise. Runner exit 0, console
   [WARN]/[FAIL] 0. Wall 7 min 15 s (<= 9 min).
   PREREG DEFECT recorded, not a run miss: E asked for "6 x mode=TerrainProfile" create
   lines. That string cannot occur - VrfC2SimService.cs:439 hard-codes the literal
   "Create-altitude mode=Live" for the whole live-like family, and Row 2's own log (which
   its sec 6 quoted as "mode=TerrainProfile") in fact reads mode=Live too. This run logged
   6 x "Create-altitude mode=Live: GROUND unit <name> created at safe MSL 10000 m (original
   create alt 1000 m); parity post-create SetAltitude SKIPPED" - identical to Row 1's six,
   which is what the clause was actually testing. Row 2 sec 6's quotation of that line is
   wrong and should be corrected there.
F. EARLY EXIT - MET. earlyExit.fired true; allCompleteUtc 10:20:01.500Z, closedUtc
   10:21:07.915Z -> 66.4 s (band [60, 90]); windowSecsUsed 222.1; reportEvidence satisfied
   x3 with distanceM 0.0 / 0.0 / 0.0.
G. THE BACK END SURVIVES THE RUN - MET, both halves. bin64-vrfSim.log (captured copy) is
   11,283 lines / 996,829 bytes and runs from scenario load to teardown: 3,635 lines are
   stamped 06:17:00 local or later, i.e. AFTER the order push at 06:16:55 local
   (10:16:55.043Z), first one "M3 1: [Wed Sep  2 06:17:00 2026] avoidCollision() - Entity
   location: {-2359998.939429, -4693684.575701, 3606516.701742}", last one "Exception in
   destroyFederationExecution: Federation Execution Already Exists.[Wed Sep  2 06:21:43
   2026]" (the normal teardown tail). Row 2's log stopped dead at 5,318 lines / 21:21:26,
   6 s before its order push; Row 1's was 11,099 lines. Window-title polling every 60 s
   through the run (7 samples 10:15:43Z - 10:21:43Z) saw only "vrfSimHLA1516e pid=133672
   title='C:\MAK\vrforces5.0.2\bin64\vrfSimHLA1516e.exe'" and "vrfGui pid=238252
   title='VR-Forces - ..\userData\scenarios\TropicTortoise.scnx'", never a `.dmp` title;
   the post-run sweep found no vrfSim*/vrfGui process at all, and C:\MAK\vrforces5.0.2\
   bin64 holds NO new dump (newest is still Row 2's vrfSim5.0.2-MSVC++15.0_64-249613-
   70668.dmp, 598,441 B, 2026-09-02 06:00:18 local). AnswerCrashDumpDialog.ps1 was never
   needed and never run.

WHAT THE BACK-END LOG SAYS ABOUT THE REQUEST (recorded per the prereg's STOP rule, not
investigated): at notify level 3 bin64-vrfSim.log contains ZERO lines matching
`IfRequest`, `TerrainProfile`, `terrain profile`, or `IntersectionInformation`. Every hit
for `terrain` is scenario-load/plugin noise from 06:14:52-06:15:31 (terrain config, vantage
plugin, dynamic-terrain entity), all of it before the order. A first read mistook the 1,100
"Segment/Bounding-Volume Intersection:  Segment endpoints are identical." lines (06:16:56 -
06:17:53) for request traffic; they are NOT - Row 1, which never sent a request, has 1,050
of the same lines interleaved with `avoidCollision()` / `avoidNonMovingObstruction()`
entries, and Row 2, where nothing moved, has 0. They are collision-avoidance noise
proportional to movement. So the back end answered the request without logging anything
about it at level 3.

ADJUDICATION AGAINST THE VARIABLE (verified vs. inferred):
- VERIFIED: with a live back end, the mode's request/reply round trip WORKS end to end
  inside the 10 s budget - Row 2's timeout was the crash, not the design. The request is
  sent (non-zero ids), a complete reply comes back, and the authoring code consumes it.
- VERIFIED: the reply is usable for exactly one vertex per route - vertex 0, the taskee's
  own position - and unusable for vertices 1 and 2, the two order points 555 m - 1 km away.
  All three routes behave identically. No echo, no all-vertex frame failure.
- VERIFIED: the failure is inert for movement. The Partial path keeps Live altitudes for the
  unauthored vertices and the run reproduces Row 1 to within 1.4 s and 0.27 m.
- INFERRED (not proven by this run's artifacts): the natural competing hypotheses for
  "vertex 0 usable, vertices 1-2 not" are (i) the back end returned FEWER samples than
  requested - e.g. one - so vertices 1 and 2 had no candidate within the 50 m gate; (ii) it
  returned three samples but only the first is near its vertex, which would point at how the
  request's later points are packed or interpreted; (iii) the samples for 1 and 2 were
  present but rejected by the usability gate for another reason. NOTHING in the app log at
  Info level distinguishes these - the sample count and the per-vertex distances are only
  visible at Debug (:1459) - and the back end logs nothing about the request at level 3.
  Deciding between them is the supervisor's docs-first call, not this run's.
- CONSEQUENCE FOR THE MODE: the mode is NOT dead and NOT frame-broken, but it does not yet
  deliver its purpose - the route waypoints that matter (the order points) still carry Live
  altitudes. Row 2 is now ADJUDICATED: the design's Row 2 check 1 ("NO fallback / Partial
  WARN") is not met.

STATE LEFT BEHIND (for the next session):
- Processes: VR-Forces DOWN, nothing force-killed; no WatchVrf / ListenReports /
  VrfC2SimApp; RTI trio rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 resident
  and untouched; docker stp-server + c2sim_server Up.
- Ledger: marker is now 3697 (this run consumed 3690-3696; 3696 unconsumed and BURNED).
- Dumps: none created by this run; bin64 still holds Row 2's 70668 dump (598,441 B) and the
  older 2023-12 / 2026-07 ones. No file under C:\MAK was written by this session.
