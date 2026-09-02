# PREREG TERRAIN ROW 3 - THE DEFAULT FLIP: does the app take the TerrainProfile path with NO env override? - registered 2026-09-02, BEFORE launch

WHAT THIS IS: Row 2cR (docs/experiments/PREREG_TERRAIN_ROW2CR_REPEAT_2026-09-02.md sec 6, run
20260902T111116Z, bridge A7504441) run again with EXACTLY ONE VARIABLE MOVED: the environment
override `Vrf__GroundWaypointAltitudeMode=TerrainProfile` is REMOVED, and the app is expected
to reach the same mode through its own compiled DEFAULT. That default was flipped from "Live"
to "TerrainProfile" in commit 5b82e5f (docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md
sec 7 DEFAULT FLIP), a one-literal change with no code path touched.

WHY IT NEEDS A RUN AT ALL. Every TerrainProfile run in this branch - Row 2, ROW2R, Row 2c,
Row 2cR - reached the mode through the env override. The DEFAULT has never been exercised
live. A flip that does not actually reach the running binary is a classic false green
(docs/HANDOFF_2026-07-19.md sec 4/5; memory note "Lessons: false greens"), and the failure is
SILENT: with the mode string unmatched the app runs the Live path and produces a clean,
successful, 3/3-arrival run with no terrain lines at all. This run is the check that the
config the product ships with is the config that was tested.

## Sources read for this prereg (docs first, per the 2026-09-01 directive)

No NEW vendor question is opened by this run - the mechanism was settled in Rows 2c/2cR and
the vendor sources for it are cited in docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md
sec 1 (ifRequestTerrainProfileInformation.h, ifIntersectionInformationResponse.h,
terrainProfileRequestManager.h, vrfMessageInterface.h, the docs.mak.com class pages, and the
Users Guide vrf_setRouteVertexAltitude.htm contract C5) and in
docs/experiments/PREREG_TERRAIN_ROW2CR_REPEAT_2026-09-02.md sources section (the aggregate
move-along mechanism). Neither is re-litigated here.

What WAS read for THIS run, because the question is "which settings object does the app
actually load":
- src/VrfC2SimApp/VrfSettings.cs:175 (post-flip) - the compiled default is now
  `= "TerrainProfile"`. VERIFIED in the working tree and in commit 5b82e5f.
- src/VrfC2SimApp/appsettings.json - the `Vrf` section carries Protocol, ApplicationNumber,
  SiteId, SessionId, HostInetAddr, Federation, FedFileName, FomModules, ClientId and NOTHING
  ELSE. There is NO `GroundWaypointAltitudeMode` pin, so the code default is the effective
  value when no env var is set. VERIFIED by reading the whole file.
- src/VrfC2SimApp/VrfC2SimService.cs:1487-1491 - IsTerrainProfileMode() is an
  OrdinalIgnoreCase compare against "TerrainProfile"; IsLiveLikeAltitudeMode() is that OR a
  compare against "Live". Both read `_vrf.GroundWaypointAltitudeMode`. VERIFIED.
- scripts/RunC2SimScenario.ps1:1904 - the app is launched with `--contentRoot=<exe dir>` so
  appsettings.json still loads, and only `Vrf__ApplicationNumber` is injected by the runner.
  VERIFIED (no other Vrf__ variable is set by the runner).
- VrfC2SimService.cs:439 - the create-altitude log line hard-codes the text "mode=Live" for
  the whole live-like family. It is NOT a mode readout and is NOT evidence of anything in
  this run (defect named in Row 2cR sec 6 P4 and in design sec 7 ROW 2R note). The app logs
  the mode's EFFECT (the terrain request/reply/authoring lines), never the mode string, which
  is why P1 below is the only available proof that the default took.

## 1. The ONE variable: the env override is GONE; the default must carry the mode

Everything else is Row 2cR's exactly: bridge VrfBridge.dll
A7504441F421B668D10F5AFD8B4FD71110002D13FE6ABAE0DB576C7C209236F5 on 10/10 main-checkout
copies (re-verified before launch, NOT rebuilt for this run - the native bridge is untouched
by the flip); Ijwhost.dll unchanged; the same init and order files; RealTemplates; stock
templates; NavArea disabled; notify level 3; `-RunSecs 420 -StopWhenComplete`; SettleHoldSecs
60; TerrainClearanceMeters 10; TerrainProfileTimeoutSeconds 10;
GroundWaypointLiveClearanceMeters 50; MaxHorizontalMismatchMeters 50;
`Vrf__TimeMultiplier` NOT set (1x). The app assembly is rebuilt (Release, 0 errors) because
the flip is a source change; that rebuild is PART of the one variable, not a second one.

`Get-ChildItem env:Vrf__*` must be EMPTY at launch and is echoed into the run console log.

## 2. Invocation (main checkout, VRF_C2SIM, pwsh) - NO env line at all

    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_NoComments.xml `
        -RunSecs 420 -StopWhenComplete

Foreground, 15-minute timeout (Row 2cR wall was 7 min 13 s). Adjudication from the run
directory artifacts ONLY (vrfc2simapp.log, reports-captured.log, run-manifest.json,
watchvrf-trace.csv, bin64-vrfSim.log). Ledger: marker `*** NEXT FREE: 3711 ***` before the
run (docs/OPUS_EXECUTION_PLAN.md:1590, verified as the single value-bearing marker); the
runner consumes 7 numbers and advances the marker itself, so expected wasValue 3711 /
newValue 3718, appNos 3711-3717.

PREREG COMMIT: the predictions below were registered in commit 4682063 BEFORE launch. This
line is the only content added afterwards (in the immediately following commit); nothing in
sections 1-5 changed after 4682063, which is what the hash attests.

## 3. Pre-launch inventory (must hold, else STOP - never kill)

VERIFIED 2026-09-02 before launch: no vrfSim* / vrfGui / vrfLauncher / WatchVrf /
ListenReports / VrfC2SimApp process of any kind; RTI trio exactly rtiAssistant 41336
(start 2026-09-01 14:34:28) / rtiexec 224608 (15:08:59) / rtiForwarder 76620 (15:09:02),
the same PIDs as Rows 1 / 2 / 2R / 2c / 2cR; docker stp-server "Up 19 hours (healthy)" +
c2sim_server4.8.4.9 "Up 19 hours"; `Get-ChildItem env:Vrf__*` count 0 - NO Vrf__ variable
was present and none had to be removed; 10/10 main-checkout VrfBridge.dll copies hash
A7504441 with a single distinct SHA-256 over the ten enumerated paths (the only other copies
on disk are the two labelled backup directories and the .claude worktrees, none of which
this run loads). Offline gates for the flip, all green before this prereg was written:
`dotnet build src/VrfC2SimApp/VrfC2SimApp.csproj -c Release` 0 errors / 6 pre-existing
warnings; 7/7 app self-tests exit 0 (translator SELF-TEST PASSED; report, sequencer, verb,
destack, fanout ALL CHECKS PASSED; terrain "terrain-selftest: PASS");
tests/RunnerTurnaround.Tests.ps1 96 passed / 0 failed, exit 0.

## 4. Predictions with numbers

Comparator throughout is Row 2cR (run 20260902T111116Z), with Row 1 (20260902T010704Z) for
the movement baseline.

P1 - THE DEFAULT PATH IS THE TERRAINPROFILE PATH (HIGH). This is the whole point of the run.
    In vrfc2simapp.log:
    (a) Exactly THREE :813 Information lines "Task '<name>': terrain profile request {Id}
        sent for 3 vertices; dispatch deferred until the reply or the timeout." - ids 7, 8, 9.
        ZERO of these is the falsifier that matters: it means the mode string did not match
        and the app silently ran the Live path.
    (b) Exactly THREE :1466 Information lines "Terrain profile reply {Id}: 3 sample(s)
        [...]", each with N = 3, three tokens, DISTINCT indices {0,1,2}, no `#k:none`, and
        the SAME coordinates and terrain altitudes as Row 2cR - character for character:

          Terrain profile reply 7: 3 sample(s) [#0:34.61296,-116.60049,1040.6 #1:34.61296,-116.59417,1033.9 #2:34.61296,-116.58786,1026.7].
          Terrain profile reply 8: 3 sample(s) [#0:34.64763,-116.69339,1116.7 #1:34.65263,-116.69339,1116.8 #2:34.65763,-116.69339,1116.9].
          Terrain profile reply 9: 3 sample(s) [#0:34.60842,-116.71269,1131.4 #1:34.60842,-116.70637,1126.3 #2:34.60842,-116.70006,1121.1].

    (c) Exactly THREE :802 Information lines "all 3 vertices authored from terrain + 10 m
        clearance", with these alts lists, identical to Row 2cR and Row 2c to 0.1 m:

          Terrain profile 7 for task 'T_R5_PL1': all 3 vertices authored from terrain + 10 m clearance; alts [1050.6, 1043.9, 1036.7].
          Terrain profile 8 for task 'T_R5_CO1': all 3 vertices authored from terrain + 10 m clearance; alts [1126.7, 1126.8, 1126.9].
          Terrain profile 9 for task 'T_R5_TK1': all 3 vertices authored from terrain + 10 m clearance; alts [1141.4, 1136.3, 1131.1].

    (d) ZERO :807 Partial/Fallback, ZERO :1480 timeout, ZERO :793 "request not sent", ZERO
        :1453 partial-series, ZERO :810 Note, and ZERO `warn:` lines in the whole app log.
    (e) The run console log records `Get-ChildItem env:Vrf__*` as EMPTY before the runner
        started, so (a)-(d) are attributable to the compiled default and to nothing else.

P2 - MOVEMENT IS UNCHANGED (HIGH). TASKCMPLT offsets from clocks.orderPushedUtc, measured off
    the reports-captured.log `[hh:mm:ss.fff]` stamps - the same measure every row used:
      1.BdeHQ       117.3 s +/- 5 s   (Row 1 117.3, Row 2c 117.5, Row 2cR 117.45, ROW2R 118.0)
      1222.MechPlt  129.2 s +/- 5 s   (Row 1 129.2, Row 2c 129.6, Row 2cR 129.67, ROW2R 130.1)
      114.MechCoy   within the OBSERVED 1x spread [178.2, 198.1] (Row 2cR sec 6 table). Both
        sub-branches are stated NOW, and neither is a "miss":
          - inside [178.8, 188.8] = TYPICAL, the band Row 2cR's branch (b) used. Expected.
          - 193 to 203 = A SECOND EXCURSION. This would REOPEN the residual named in Row 2cR
            sec 6 ("only two runs have ever moved this aggregate on terrain-authored
            waypoints... a RARE or INTERMITTENT altitude-triggered effect is NOT excluded by
            n=2; the falsifier that would reopen H-ALT: further TerrainProfile runs drawing
            ~198 s while Live runs stay at or below ~185 s"). Record it as exactly that -
            a second draw at ~198 on terrain-authored waypoints - and STOP for the
            supervisor. Do NOT retune, do NOT re-run inside this prereg.
          - anything else inside [178.2, 198.1] but outside both sub-bands: UNDECIDED,
            recorded, no band widened.
    Endpoints from the trace final POS within 1 m of Row 2cR (1.BdeHQ 34.608416,-116.699996
    alt 1121.1; 114.MechCoy 34.653915,-116.693388 alt 1116.8; 1222.MechPlt
    34.612956,-116.587783 alt 1026.6). POS==RPT <= 1 m x3; settled true x3. 3/3 TASKCMPLT in
    both vrfc2simapp.log and reports-captured.log. Corroboration required as in Row 2cR: the
    trace plateau onset (first POS within 1 m of the final POS) must move the same way as the
    report offset (Row 2cR 147.9 / 219.2 / 160.1). Report offset and plateau onset disagreeing
    is itself a finding - it would mean report lag, not movement.

P3 - THE BACK END SURVIVES AND TEARDOWN IS CLEAN (HIGH). No vrfSim* process with
    MainWindowTitle matching `^vrfSim.*\.dmp$` at any point; no new .dmp in
    C:\MAK\vrforces5.0.2\bin64 (newest stays the ROW2R-era
    vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp). Runner exit 0; StopVrf exit 0 with
    "VR-Forces is DOWN"; RTI trio PIDs 41336 / 224608 / 76620 unchanged; both observers on the
    stop-file path. 3 x "CreateRoute ... (3 pts)" + 3 x "MoveAlongRoute issued".
    earlyExit.fired true with closedUtc - allCompleteUtc in [60, 90] s. bin64-vrfSim.log
    censuses "Waiting for nav data" 0 / "empty route" 0 / "Can't find entity route" 0 /
    "invalid formation name" 1. App-log census: 3 `fail:` + 3 "Can't create data of type",
    0 Exception, 0 `warn:`. Six "Create-altitude mode=Live" create lines (the hard-coded
    template - EXPECTED, not a mode readout). Wall 7 min 15 s +/- 45 s. Ledger wasValue 3711 /
    newValue 3718 / advanced true; marker line after the run reads `*** NEXT FREE: 3718 ***`.

## 5. Falsifier branches - PRE-NAMED

F1 - ZERO :813 request lines (or zero :1466 replies, or no terrain line of any kind). THE
     DEFAULT DID NOT FLIP IN THE DEPLOYED BIN. This is the SILENT failure the run exists to
     catch: the app would still complete 3/3 under Live and look successful. STOP. Do not
     re-run and do not touch the mode. Diagnose which VrfSettings the app actually loaded, in
     this order: (i) `Get-ChildItem env:Vrf__*` in the run console log - was a variable set
     after all; (ii) the appsettings.json NEXT TO THE RUNNING EXE
     (src/VrfC2SimApp/bin/Release/net10.0/win-x64/appsettings.json, which is what
     --contentRoot points at) for a `Vrf:GroundWaypointAltitudeMode` pin that the source
     appsettings.json does not have; (iii) the timestamp and content of the deployed
     VrfC2SimApp.dll against the 5b82e5f build - a stale bin is the 07-19 false-green trap.
F2 - A :807 Partial or Fallback, a :1480 timeout, a :793 "request not sent", or any `warn:`
     line. The mode was entered but did not deliver, which is a REGRESSION against Rows 2c and
     2cR on an unchanged bridge and unchanged data. Record verbatim and STOP - the flip is not
     provably safe and the supervisor decides whether to revert it.
F3 - P1 (b) or (c) differs from Row 2cR by more than 0.1 m, or an index repeats, or a
     `#k:none` appears. The terrain query is not deterministic after all. Record and STOP;
     P2 becomes uninterpretable as a comparison.
F4 - P2's individual entities (1.BdeHQ, 1222.MechPlt) fall outside +/-5 s of Row 1. The whole
     run is slower or faster and no per-unit reading is attributable. Record all three offsets
     and STOP.
F5 - ANY CRASH OR INFRASTRUCTURE FAILURE - the MAK dump prompt, a non-zero runner or StopVrf
     exit, an observer that never reached the stop-file path, an RTI PID change, a killed
     federate. Infrastructure, not an answer. Dump prompt: `pwsh -File
     scripts\AnswerCrashDumpDialog.ps1` then `pwsh -File scripts\StopVrf.ps1` per RUNBOOK
     0.5.12 (ALWAYS Yes). STOP; after two infrastructure failures this session, stop entirely.

NOTE, carried forward from Row 2cR sec 5: a movement-timing result on 114.MechCoy is NOT an
infrastructure failure and NOT a falsifier. P2 states its sub-branches in advance and each
has its own verdict.

## 6. Outcome (written from the run directory artifacts, after the run)

VERDICT: THE FLIP TOOK. With NO env override of any kind, the app took the TerrainProfile
path and reproduced Row 2cR: three requests sent, three three-sample replies
character-for-character identical to Row 2cR, three routes with all 3 vertices authored from
terrain + 10 m, ZERO warn: lines, 3/3 TASKCMPLT, endpoints and resting altitudes unchanged,
back end alive, clean teardown. P1 MET, P2 MET (114.MechCoy in the TYPICAL sub-branch - no
second excursion, so the Row 2cR residual is NOT reopened), P3 MET. No falsifier fired.
Nothing was retuned, re-run, killed, or code-changed after launch.

Run 20260902T113613Z_run, launched 2026-09-02 11:36:13.302Z from main at b2ceeb1 (this
prereg registered at 4682063; the flip itself at 5b82e5f). `Get-ChildItem env:Vrf__*` was
EMPTY (count 0) both before the runner started and after it finished, echoed into
runs/20260902T113613Z_run/console-row3.log (its first three lines). appNos 3711-3717
(vrfBackend 3711, vrfFrontend 3712, oraclePre 3713, oracleTrace 3714, app 3715, rtiProbe
3716, createOneDiag 3717 - UNCONSUMED and BURNED, the oracle gate passed); ledger advanced
3711 -> 3718 by the runner; marker line after the run reads `*** NEXT FREE: 3718 ***`
(docs/OPUS_EXECUTION_PLAN.md:1603, still the only value-bearing marker); ledger file CRLF
1897 / bare LF 0 / non-ASCII 0. Every stage exit code 0 (RtiProbe, LaunchVrf,
WatchVrf-precheck, WatchVrf-trace, ListenReports, PushInit, VrfC2SimApp, PushOrder,
StopIface, StopVrf). Wall 11:36:13.302Z -> 11:43:27.429Z = 434.1 s = 7 min 14 s (band was
7 min 15 s +/- 45 s; Row 2cR 7 min 13 s, Row 1 7 min 15 s). validityFlags: the single
advisory pre-init INFO only; console [WARN]/[FAIL] 0.

P1 - THE DEFAULT PATH IS THE TERRAINPROFILE PATH - MET. Verbatim from vrfc2simapp.log
   (lines 47/49/51, 53/55/57, 59/63/67):

     Task 'T_R5_PL1': terrain profile request 7 sent for 3 vertices; dispatch deferred to the reply (timeout 10 s -> Live fallback).
     Task 'T_R5_CO1': terrain profile request 8 sent for 3 vertices; dispatch deferred to the reply (timeout 10 s -> Live fallback).
     Task 'T_R5_TK1': terrain profile request 9 sent for 3 vertices; dispatch deferred to the reply (timeout 10 s -> Live fallback).

     Terrain profile reply 7: 3 sample(s) [#0:34.61296,-116.60049,1040.6 #1:34.61296,-116.59417,1033.9 #2:34.61296,-116.58786,1026.7].
     Terrain profile reply 8: 3 sample(s) [#0:34.64763,-116.69339,1116.7 #1:34.65263,-116.69339,1116.8 #2:34.65763,-116.69339,1116.9].
     Terrain profile reply 9: 3 sample(s) [#0:34.60842,-116.71269,1131.4 #1:34.60842,-116.70637,1126.3 #2:34.60842,-116.70006,1121.1].

     Terrain profile 7 for task 'T_R5_PL1': all 3 vertices authored from terrain + 10 m clearance; alts [1050.6, 1043.9, 1036.7].
     Terrain profile 8 for task 'T_R5_CO1': all 3 vertices authored from terrain + 10 m clearance; alts [1126.7, 1126.8, 1126.9].
     Terrain profile 9 for task 'T_R5_TK1': all 3 vertices authored from terrain + 10 m clearance; alts [1141.4, 1136.3, 1131.1].

   The six reply and authoring lines are CHARACTER-FOR-CHARACTER the lines Row 2cR sec 6
   quotes - a third consecutive identical terrain query. N = 3 on all three replies, indices
   {0,1,2} distinct, no `#k:none`. ZERO :807 Partial/Fallback, ZERO :1480 timeout, ZERO :793
   "request not sent", ZERO :1453 partial-series, ZERO :810 Note, and ZERO `warn:` lines in
   the whole app log.
   PREREG DEFECT, recorded rather than quietly absorbed: sec 4 P1 (a) quoted the :813 template
   as "...dispatch deferred until the reply or the timeout." The actual template
   (VrfC2SimService.cs:813) reads "...dispatch deferred to the reply (timeout 10 s -> Live
   fallback)." The prediction's SUBSTANCE - exactly three :813 lines, ids 7/8/9, "sent for 3
   vertices" each - is met exactly; the transcription of the template text into the prereg was
   wrong. Nothing was adjusted after the fact: the quoted line above is the log's, not the
   prereg's.
   WHY THIS IS ATTRIBUTABLE TO THE COMPILED DEFAULT AND NOTHING ELSE (the adversarial pass,
   because a false positive here is as bad as a false negative):
     - `Get-ChildItem env:Vrf__*` count 0 before AND after, in the console log. No override.
     - The deployed appsettings.json NEXT TO THE RUNNING EXE
       (src/VrfC2SimApp/bin/Release/net10.0/win-x64/appsettings.json, which is exactly what
       the runner's `--contentRoot` points at - see the VrfC2SimApp commandLine in
       run-manifest.json) has a `Vrf` section with NO GroundWaypointAltitudeMode key. Read
       before launch.
     - The runner injects exactly ONE Vrf__ variable into the child, Vrf__ApplicationNumber
       (manifest note on the VrfC2SimApp stage), which is not the mode.
     - A STALE binary is excluded in the same breath: a pre-flip VrfC2SimApp carries "Live"
       and would have produced ZERO terrain lines. Terrain lines exist, so the settings object
       the app loaded said "TerrainProfile", and the only remaining source for that string is
       VrfSettings.cs:175 as compiled in 5b82e5f. VrfC2SimApp.dll 2026-09-02 07:31:11 local,
       newer than the VrfSettings.cs edit at 07:28:43.

P2 - MOVEMENT IS UNCHANGED - MET. Offsets of the TASKCMPLT report receipts
   (reports-captured.log `[hh:mm:ss.fff]` stamps, attributed by the block's own
   <ReportingEntity>) from clocks.orderPushedUtc 2026-09-02T11:38:37.739Z:
     1.BdeHQ      (task ...0003, entity 670cfdb2) receipt 11:40:35.214 -> +117.47 s
                  Row 1 +117.3, Row 2cR +117.45, Row 2c +117.5. Delta vs Row 1 +0.17. OK
     1222.MechPlt (task ...0001, entity 001aa71b) receipt 11:40:47.368 -> +129.63 s
                  Row 1 +129.2, Row 2cR +129.67, Row 2c +129.6. Delta vs Row 1 +0.43. OK
     114.MechCoy  (task ...0002, entity 139aa71b) receipt 11:41:40.074 -> +182.34 s
                  INSIDE the TYPICAL sub-band [178.8, 188.8]; 10.8 s below the excursion
                  sub-band [193, 203]. The Row 2cR residual is NOT reopened: this is a third
                  run on terrain-authored waypoints and it drew a Live-era value, which moves
                  the evidence further AWAY from a rare altitude-triggered effect rather than
                  toward it. It is also the lowest terrain-mode draw so far and the second
                  lowest of all nine 1x runs.
   Corroborated by the trace, and the two measures move together, so this is arrival and not
   report lag - trace plateau onset (first POS within 1 m of that entity's final POS):
     1.BdeHQ      147.9 s  (Row 2cR 147.9, Row 1 148.0, Row 2c 148.0) - identical
     1222.MechPlt 160.1 s  (Row 2cR 160.1, Row 1 160.1, Row 2c 162.3) - identical
     114.MechCoy  215.3 s  (Row 2cR 219.2, Row 1 219.2, Row 2c 233.3) - 3.9 s EARLIER than
                  Row 2cR, in the same direction and of the same order as its report offset
                  (2.7 s earlier). Trace TSK completionT 210.4 (Row 2cR 213.2, Row 1 212.0,
                  Row 2c 226.4).
   Endpoints from the trace final POS (t=278.5): 1.BdeHQ 34.608416,-116.699994 alt 1121.1;
   114.MechCoy 34.653915,-116.693388 alt 1116.8; 1222.MechPlt 34.612956,-116.587783 alt
   1026.6 - identical to Row 2c to all six decimals and within 0.18 m of Row 2cR (whose
   1.BdeHQ longitude read -116.699996). Resting altitudes unchanged. POS==RPT 0.0 / 0.0 /
   0.0 m, satisfied x3, reason "post-completion RPT agrees with POS". 3 TASKCMPLT in
   vrfc2simapp.log and 3 in reports-captured.log.

   114.MechCoy COMPLETION OFFSET ACROSS EVERY RUN IN runs/ THAT PUSHED THIS ORDER AND GOT A
   TASKCMPLT FOR TASKEE 139aa71b-75df-4888-4a5a-6056bae66242 - Row 2cR sec 6's table with
   this run appended. Offset = the report-receipt stamp in reports-captured.log minus
   clocks.orderPushedUtc from that run's own manifest; traceTSK = the same run's
   oracle.earlyExit.reportEvidence completionT:

     run                    offset_s  traceTSK  timeMult  mode            bridge     note
     20260901T203702Z_run    178.2     n/r        1       Live            A48ABE6C   R9 baseline
     20260901T211310Z_run    184.6     n/r        1       Live            A48ABE6C   P2c endpoint record
     20260901T230326Z_run     37.0     n/r        5       Live            A48ABE6C   5x multiplier - NOT comparable
     20260901T235823Z_run    183.7     n/r        1       Live            A48ABE6C   CONFIRM1
     20260902T003710Z_run    182.1    210.3       1       Live            A48ABE6C   CONFIRM2
     20260902T010704Z_run    183.8    212.0       1       Live            28E993FE   ROW 1 control
     20260902T101431Z_run    185.2    214.1       1       TerrainProfile  28E993FE   ROW 2R (Partial - Live alts used)
     20260902T104832Z_run    198.1    226.4       1       TerrainProfile  A7504441   ROW 2c
     20260902T111116Z_run    185.0    213.2       1       TerrainProfile  A7504441   ROW 2cR
     20260902T113613Z_run    182.3    210.4       1       TerrainProfile  A7504441   ROW 3 (this run - mode from the
                                                                                     COMPILED DEFAULT, no env override)

     Exclusions unchanged from Row 2cR sec 6 (20260902T011908Z_run has no TASKCMPLT for this
     taskee; the freeze-era and pre-fix runs have none either; the 5x run is not like-for-like).
     STATISTICS over the nine comparable 1x runs: min 178.2, max 198.1, range 19.9 s - the
     range is UNCHANGED by this run, which landed inside it. Over the eight excluding Row 2c:
     178.2 to 185.2, range 7.0 s - also unchanged; 182.3 sits mid-band. Terrain-authored
     waypoints have now produced 198.1 / 185.0 / 182.3 against Live-style 178.2 / 184.6 /
     183.7 / 182.1 / 183.8 / 185.2: the two sets now overlap on both sides, which is a
     stronger position for H-V than n=2 gave. THE BAND TO USE GOING FORWARD is unchanged
     (~185 +/- 10 s at 1x, with a demonstrated excursion to ~198), and so is the rule requiring
     n>=2 before calling any shift on this taskee an effect.

P3 - THE BACK END SURVIVES AND TEARDOWN IS CLEAN - MET. Back end alive the whole run:
   bin64-vrfSim.log 10,600 lines, 5,880 of them stamped 07:38 local or later, i.e. at or after
   the order push at 07:38:37 local (11:38:37.739Z); last line "Exception in
   destroyFederationExecution: Federation Execution Already Exists.[Wed Sep  2 07:43:23 2026]"
   - the normal teardown tail; 0 lines matching FATAL. NO new .dmp in
   C:\MAK\vrforces5.0.2\bin64: the newest is still the ROW2R-era
   vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp, 598,441 B, 2026-09-02 06:00:18 local.
   AnswerCrashDumpDialog.ps1 was never needed and never run; the post-run process sweep found
   no vrfSim* at all. Runner exit 0; StopVrf exit 0 with "VR-Forces is DOWN (graceful quit; no
   process was force-killed)."; post-run Get-Process shows exactly rtiAssistant 41336 /
   rtiexec 224608 / rtiForwarder 76620 and nothing else of ours - RTI PIDs unchanged and
   explicitly reported preserved by StopVrf. Both observers took the stop-file path: trace
   "# STOP requested via stop-file at t=310.3s" -> "[OK] resigned cleanly."; ListenReports
   "stop requested via stop-file at t=313.4s ... - disconnecting", "captured 30 reports".
   Three "CreateRoute ... (3 pts)" + three "MoveAlongRoute issued". earlyExit.fired true;
   allCompleteUtc 11:41:43.798Z, closedUtc 11:42:48.411Z -> 64.6 s (band [60, 90]);
   windowSecsUsed 220.3 of the 420 cap (Row 2cR 220.5); completionLinesSeen 3.
   Censuses: bin64-vrfSim.log "Waiting for nav data" 0 / "empty route" 0 / "Can't find entity
   route" 0 / "invalid formation name" 1 (baseline) = 0/0/0/1, SocketException 0, and ZERO
   lines matching IfRequest / TerrainProfile / terrain profile / IntersectionInformation (the
   back end still logs nothing about the request at notify level 3 - unchanged from ROW2R,
   Row 2c and Row 2cR). App log: 3 `fail:` (the C2SIMSDK deserialize noise), 3 "Can't create
   data of type", 0 Exception, 0 `warn:` - identical to every prior row's census. Six
   "Create-altitude mode=Live" create lines, as pre-stated: the template at
   VrfC2SimService.cs:439 is hard-coded for the whole live-like family and is NOT a mode
   readout.

FALSIFIER BRANCH TAKEN: none. F1, F2, F3, F4, F5 all silent.

ADJUDICATION (verified vs. inferred):
- VERIFIED: the compiled default now carries the mode. Terrain requests were issued, answered
  and applied on a run whose environment contained no Vrf__ override and whose deployed
  appsettings.json contains no mode pin. The competing hypothesis - that some OTHER
  configuration source supplied "TerrainProfile" - was checked against all three candidate
  sources (env, deployed appsettings, runner-injected env) and each was read directly rather
  than assumed. The reverse competing hypothesis - a stale bin still running "Live" - is
  falsified by the terrain lines themselves, which that binary cannot emit.
- VERIFIED: the flip costs nothing. Three requests, three complete three-sample replies,
  three fully authored routes, zero warnings; movement, endpoints, resting altitudes,
  POS==RPT, settle, early exit, back-end liveness and every hygiene census are Row 2cR's.
- VERIFIED: the terrain query is deterministic across three consecutive runs on this data
  (Row 2c, Row 2cR, Row 3 - the reply and authoring lines are character-identical).
- VERIFIED, strengthening a prior ruling without reopening it: 114.MechCoy's third
  terrain-authored draw is 182.3 s, below every Live-era draw except the R9 baseline. The
  Row 2cR residual named the reopening falsifier as "further TerrainProfile runs drawing
  ~198 s while Live runs stay at or below ~185 s"; this run is the opposite observation, so
  the H-V ruling stands and the residual narrows.
- NOT CLAIMED: that the default is right for every deployment. This run shows the default is
  reachable and safe on THIS data at THIS site. A back end that cannot answer the terrain
  query still falls back to Live per vertex with a WARN (design sec 3.3), and that path has
  been exercised live exactly once, in ROW 2 (the crashed run) - it is untested on a healthy
  back end and stays a known gap.
- NOT CLAIMED: anything about Fixed100. It was not exercised in this run and is unchanged.
