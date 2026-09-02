# PREREG TERRAIN ROW 1 - CONTROL: new bridge 28E993FE, mode=Live (default) - registered 2026-09-02 01:05Z, BEFORE launch

Source: docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 7 Row 1 (the control row the
review made mandatory) and sec 6 DEPLOY RECORD; docs/experiments/REVIEW_TERRAIN_PROFILE_BRANCH_
2026-09-01.md (pass 2 verdict MERGE at 066f3d2; sec 8 "Residual for the confirming run":
"Control row ... remains mandatory: the native binary is new even though its default-path
vendor calls are byte-identical in source"); docs/HANDOFF_2026-07-19.md sec 4 (five-run
bridge validation table: a wrong bridge shows as "No backends" / 0 MoveAlongRoute / object
creation FAILED) and sec 5 (native-work rule: additive, opt-in, default Start() path untouched;
redeploy all copies, one hash); docs/RUNBOOK.md 0.5.11 (-StopWhenComplete, CONFIRMED);
docs/HEADLESS_RUN_PLAN.md sec 4a.1 (arrival / settled). Reference run = CONFIRM2
20260902T003710Z (docs/experiments/PREREG_RUNNER_CONFIRM2_2026-09-01.md sec 6) - the same
invocation on the OLD bridge A48ABE6C; endpoint record = P2c 20260901T211310Z. ASCII only.
The C++ repo (c2simVRFinterfacev2.36) is a frozen oracle; nothing under C:\MAK is touched.

## 1. The ONE variable: the deployed native binary

VrfBridge.dll A48ABE6C (2026-07-19 build, source c24248f) -> 28E993FE (2026-09-02 build,
source e1fdbbd = main after the merge of 066f3d2), plus its companion Ijwhost.dll 2DCC3B73
(.NET 10.0.8 host) -> 38255036 (.NET 10.0.10 host, refreshed by MSBuild from the installed
SDK - design sec 6 DEPLOY RECORD). All 10 copies hash 28E993FE (one-hash proof recorded).
The consumers in this run that load the new bridge: VrfC2SimApp, WatchVrf (precheck + trace),
RtiProbe, and CreateOne only on the stage-7 failure path.

Everything else IDENTICAL to CONFIRM2: 1x (no Vrf__TimeMultiplier), init
data/R9_Mojave_Lean_Initialization_NoComments.xml, order data/R9_Mojave_UnitMove_Order_
NoComments.xml, RealTemplates, GroundWaypointAltitudeMode=Live BY DEFAULT (VrfSettings.cs:175;
`Get-ChildItem env:Vrf__*` must be EMPTY before launch - verified at launch), stock templates,
NavArea disabled, vrfSim.mtl notify level 3, `-RunSecs 420 -StopWhenComplete`, SettleHoldSecs
60. The managed app code is ALSO new (merge e1fdbbd) but by the review's default-path
analysis (sec 2) its Live path differs only by IsLiveLikeAltitudeMode() (same truth value for
"Live"), one `_pendingTerrain.IsEmpty` test per tick and one event subscription - no VRF call.

What this row proves if it passes: the new binary is inert at default settings, so Row 2 can
flip the mode as its ONE variable. What it proves if it fails: the native binary is at fault
regardless of the mode code (07-19 record) -> STOP, do not proceed to Row 2.

## 2. Invocation (from the main checkout, VRF_C2SIM, pwsh)

    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_NoComments.xml `
        -RunSecs 420 -StopWhenComplete

Console log to the session scratchpad; adjudication from the run directory artifacts only
(scratchpad adjudicate.py, calibrated on CONFIRM2: reproduces +117.1 / +129.1 / +182.1 s,
endpoints 0.27 / 0.00 / 0.09 m from P2c, POS==RPT 0.0 m x3, plateau onsets 147.6 / 214.7 /
159.8, 7 min 17 s, vrfSim counts 0/0/0/1, and 0 terrain lines by the exact strings below).
Ledger: marker `*** NEXT FREE: 3676 ***` before the run (docs/OPUS_EXECUTION_PLAN.md:1525,
CRLF 1819 / bare LF 0); 7 numbers -> expected wasValue 3676 / newValue 3683 (appNos 3676-3682).

## 3. Pre-launch inventory (must hold, else STOP and report - never kill)

VR-Forces DOWN (no vrfLauncher / vrfSimHLA1516e / vrfGui); no WatchVrf / ListenReports /
VrfC2SimApp; RTI trio resident: rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620
(inventoried 2026-09-02 00:5xZ at session start: exactly those three, plus VS Code's
dotnet build host 237788 which is not ours); docker stp-server + c2sim_server Up; main
checkout at the commit that carries this prereg; `env:Vrf__*` empty.

## 4. Predictions and what counts as a miss (ANY miss = STOP; this is a control)

A. COMPLETION (HIGH). 3/3 TASKCMPLT (1.BdeHQ / 1222.MechPlt / 114.MechCoy); order-push-
   relative times (reports-captured.log stamps minus clocks.orderPushedUtc) within +/-10 s of
   CONFIRM2's +117.1 / +129.1 / +182.1 s. 3 TASKCMPLT lines in vrfc2simapp.log and 3 in
   reports-captured.log.
B. ENDPOINTS (HIGH). Each taskee's final POS plateau within 2 m of P2c (1.BdeHQ
   34.608416,-116.699993; 114.MechCoy 34.653915,-116.693388; 1222.MechPlt
   34.612956,-116.587784); ALL THREE POS==RPT <= 2 m (last RPT POSITION vs POS final);
   settled per 4a.1 (<10 m over the last 3 POS samples). Plateau onsets and rest altitudes
   REPORTED (CONFIRM2 147.6 / 214.7 / 159.8; rest alts 1121.1 / 1116.8 / 1026.6 m, start
   alts 1131.4 / 1116.7 / 1040.6 m - Row 2 needs these).
C. INERT BRIDGE (HIGH - the point of the row). ZERO lines in vrfc2simapp.log containing
   `Terrain profile ` or `terrain profile request` - the only two substrings the branch's
   eight log templates share (VrfC2SimService.cs :793 "terrain profile request not sent",
   :802 "Terrain profile {Id} for task ... all {N} vertices authored", :807 "Terrain profile
   ... keep the Live altitude", :810 "Terrain profile ... {Note}", :813 "terrain profile
   request {Id} sent", :1453 "Terrain profile reply {Id}: partial", :1459 "Terrain profile
   reply ... dropped" (Debug), :1474 "Terrain profile request {Id} ... got no reply"). The
   baseline create line "born-above-terrain" does not match either substring (verified on
   CONFIRM2: 0 hits). The app log's WARN/ERROR census identical to CONFIRM2: exactly 3
   `fail: C2SIM.C2SIMSDK[0]` deserialize lines (pre-existing SDK noise) + the 3 "Can't
   create data of type" VRF lines; no other warn/fail/Exception line. Route lines: 3 x
   "CreateRoute ... (3 pts)" + 3 x "MoveAlongRoute issued". Create-altitude lines: 6 x
   "mode=Live". `timeMult=1`.
D. WALL TIME (MEDIUM). startUtc -> savedUtc <= 9 min (CONFIRM2 7 min 17 s). Miss = report.
E. HYGIENE (HIGH). StopVrf exit 0, VR-Forces down after teardown, RTI trio PIDs unchanged
   (41336 / 224608 / 76620), no observer left; both observers took the stop-file path
   (`# STOP requested via stop-file` then `[OK] resigned cleanly.`; ListenReports "stop
   requested via stop-file"); bin64-vrfSim.log 0 "Waiting for nav data", 0 "empty route",
   0 "Can't find entity route", 1 "invalid formation name" (baseline), 0 SocketException /
   "Only one usage" / "Connection error"; 0 lines mentioning TerrainProfile /
   IntersectionInformation in bin64-vrfSim.log. Runner exit 0, 0 [WARN]/[FAIL] flags beyond
   the advisory pre-init INFO. 07-19 table row shape: "No backends" 0, MoveAlongRoute 3.
F. EARLY EXIT (HIGH). run-manifest oracle.earlyExit.fired true, reportEvidence satisfied x3
   with distanceM <= 2, closedUtc - allCompleteUtc in [60, 90] s (CONFIRM2 64.6 s).

Falsifiers (any one = STOP, report with the artifact lines, do NOT retune and do NOT run
Row 2): <3 TASKCMPLT or an offset outside the band; an endpoint > 2 m or POS==RPT > 2 m;
ANY line matching the two terrain substrings; any WARN/ERROR line not in CONFIRM2's census;
"No backends" > 0 or object creation failing (the 07-19 signature); WatchVrf crash
(0xC0000005 was the 61FE865C signature); RTI PID change; observers not on the stop-file
path.

## 5. Adjudication method

scratchpad adjudicate.py (sec 2) against the run directory, then manual quote of the
load-bearing lines into sec 6. Ledger line-ending check after the run (bare LF must be 0).

## 6. Outcome (to be written from the artifacts, after the run)

Run 20260902T010704Z_run, launched 2026-09-02 01:06:49Z from main at 7b7115f (this prereg
committed), env:Vrf__* EMPTY at launch. appNos 3676-3682 (vrfBackend 3676, vrfFrontend
3677, oraclePre 3678, oracleTrace 3679, app 3680, rtiProbe 3681, createOneDiag 3682);
ledger wasValue 3676 / newValue 3683 / advanced true; ledger file after the run CRLF 1832 /
bare LF 0. Runner exit 0; validityFlags = the single advisory pre-init INFO only; console
[WARN]/[FAIL] count 0. VERDICT: ALL SIX PREDICTIONS MET - the new bridge 28E993FE is inert
at default settings. Row 2 is cleared.

A. COMPLETION - MET. reports-captured.log TASKCMPLT count 3; vrfc2simapp.log "SENT TASK
   STATUS REPORT (TASKCMPLT)" count 3. Offsets from clocks.orderPushedUtc
   2026-09-02T01:09:28.677Z: 1.BdeHQ +117.3 s (CONFIRM2 +117.1, delta +0.2), 1222.MechPlt
   +129.2 s (+129.1, +0.1), 114.MechCoy +183.8 s (+182.1, +1.7). All within +/-10 s.
B. ENDPOINTS - MET. Final POS (trace t=278.2): 1.BdeHQ 34.608416,-116.699994 alt 1121.1
   (0.09 m from P2c); 114.MechCoy 34.653915,-116.693388 alt 1116.8 (0.00 m); 1222.MechPlt
   34.612956,-116.587783 alt 1026.6 (0.09 m). POS==RPT 0.0 / 0.0 / 0.0 m (last RPT
   POSITION at t=278.7 / 218.8 / 220.3). settled true x3. Plateau onsets 148.0 / 219.2 /
   160.1 s (CONFIRM2 147.6 / 214.7 / 159.8). Start alts 1131.4 / 1116.7 / 1040.6 m (POS
   t=23.5) - identical to CONFIRM2 to 0.1 m; these are the live altitudes Row 2's echo
   test uses (Live vertex alt = live + 50 -> 1181.4 / 1166.7 / 1090.6; echo = live + 60 ->
   1191.4 / 1176.7 / 1100.6; expected terrain + 10 at vertex 0 ~ 1141 / 1127 / 1051).
C. INERT BRIDGE - MET. Lines matching `Terrain profile ` or `terrain profile request` in
   vrfc2simapp.log: 0. WARN/ERROR census: exactly 3 `fail: C2SIM.C2SIMSDK[0]` (deserialize
   noise), 3 "Can't create data of type", 0 `warn:` lines - identical to CONFIRM2 (3/3/0).
   Route lines: "Task 'T_R5_PL1': CreateRoute 'T_R5_PL1 ROUTE' (3 pts) for 1222.MechPlt",
   "... 'T_R5_CO1 ROUTE' (3 pts) for 114.MechCoy", "... 'T_R5_TK1 ROUTE' (3 pts) for
   1.BdeHQ"; 3 x "Route '...' created; MoveAlongRoute issued for VRF_UUID:..." (3ea5a109
   / da43adba / f9d719d4). 6 x "Create-altitude mode=Live: GROUND unit ... created at
   safe MSL 10000 m". timeMult=1. "No backends": 0 hits in any run log; WatchVrf trace
   ends "[OK] resigned cleanly." (no 0xC0000005).
D. WALL TIME - MET. startUtc 01:07:04.089Z -> savedUtc 01:14:18.796Z = 434.7 s = 7 min
   15 s (CONFIRM2 7 min 17 s).
E. HYGIENE - MET. stopvrf.stdout.log: "rtiAssistant pid=41336 still running - CORRECT",
   "rtiexec pid=224608 still running - CORRECT", "rtiForwarder pid=76620 still running -
   CORRECT", "VR-Forces is DOWN (graceful quit; no process was force-killed)."; StopVrf
   stage exit 0. Post-run Get-Process: exactly rtiAssistant 41336 / rtiexec 224608 /
   rtiForwarder 76620 (start times 14:34 / 15:08 / 15:09 local, unchanged); no WatchVrf /
   ListenReports / VrfC2SimApp / vrf*. Trace tail: "# STOP requested via stop-file at
   t=310.2s (duration cap was 980s)" -> "[OK] resigned cleanly."; ListenReports "stop
   requested via stop-file at t=313.4s ... - disconnecting", "captured 31 reports".
   bin64-vrfSim.log: "Waiting for nav data" 0, "empty route" 0, "Can't find entity route"
   0, "invalid formation name" 1, SocketException / "Only one usage" / "Connection error"
   0/0/0; TerrainProfile / IntersectionInformation 0 lines. (The 4 "erminate" hits are
   "Found variable reference: $terminate-on-destroy" - same 4 lines at the same log
   positions 3912/4446/4786/4888 in CONFIRM2; not an error.)
F. EARLY EXIT - MET. run-manifest oracle.earlyExit: fired true, allCompleteUtc
   01:12:34.528Z, evidenceSatisfiedUtc 01:12:46.201Z, closedUtc 01:13:39.751Z ->
   closed - allComplete = 65.2 s (CONFIRM2 64.6 s; band [60, 90]); windowSecsUsed 220.7;
   reportEvidence 1222.MechPlt / 114.MechCoy / 1.BdeHQ distanceM 0.0 x3, satisfied x3.

Unexplained: nothing. Adjudication JSON kept in the session scratchpad
(row1_adjudication.json); every number above is reproducible from the run directory with
the adjudicate.py described in sec 2.
