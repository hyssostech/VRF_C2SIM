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

TO BE COMPLETED AFTER THE RUN.
