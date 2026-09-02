# PREREG - MERGED-BUILD CONTROL on R9 (task 1 of 3, 2026-09-02)

ONE VARIABLE: **the deployed VrfC2SimApp binary**. Everything else - fixture, init, order, run
parameters, configuration, bridge - is held at run 20260902T153837Z's values. The question is
narrow and it is a GATE: does the merged fidelity type-mapping build behave IDENTICALLY to the
build that verified the route-uuid fix, when TypeMappingMode is left at its compiled default
RealTemplates? Nothing in tasks 2 and 3 is adjudicable until this is answered.

## 0. WHY THIS RUN EXISTS

The supervisor rebuilt src/VrfC2SimApp in Release after merging the fidelity type-mapping branch
(3c5af9a). The runner launches the app straight out of that build directory
(RunC2SimScenario.ps1 `$ExeApp` = src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.exe),
so BUILDING IS DEPLOYING for the app. Every behavioural conclusion in the 2026-09-02 record -
the route-uuid fix, rung 2's nine marching performers, the FFRTC slope - was measured on the
PRE-MERGE binary (SHA-256 3b7b8d2e...c60cea0). The tasking states TypeMappingMode's default is
unchanged, "but that is a claim to test, not assume".

## 1. DOCS AND SOURCES CONSULTED (docs first, per the 2026-09-01 standing rule)

No vendor-documentation question arises for this run: the variable is OUR binary, the vendor's
configuration is byte-identical, and no VR-Forces switch or setting changes. The vendor-doc
work belongs to task 2 (`-q`), where a vendor behaviour IS the question. Sources read for THIS
prereg, all this session:

1. `docs/experiments/PREREG_ROUTE_UUID_FIX_2026-09-02.md` sec 4 (the exact invocation) and
   sec 6 (the outcome this run must reproduce). CONTROL = run 20260902T153837Z.
2. `docs/HANDOFF_2026-09-01_R9_COMPLETE.md` - WORKING CONFIGURATION, OPERATIONAL STATE,
   PROBE PROTOCOL, NON-NEGOTIABLES. The CLIENTID TRAP line (R9 inits declare STP) was
   re-verified against the deployed appsettings.json this session, not inherited.
3. `docs/experiments/PREREG_COASTP1_RUNG2_2026-09-02.md` sec 7 - the FFRTC clock finding and
   the THRESHOLD RULE (every threshold names its clock). Applied below.
4. `docs/NEXT_TYPE_MAPPING_LIVE_GATE.md` sec 2. This run IS that gate's **run 1**, the
   RealTemplates control, which the gate needs before its run 2 A/B. Task 1 and the gate's
   run 1 are the same run and it is registered once.
5. `docs/RUNBOOK.md` :1208-1215 - the ResetVrf launch environment (PATH prefix + Machine-scope
   license + cwd bin64). 3757 was burned by skipping it; this prereg quotes the recipe.
6. THE SOURCE DIFF, read line by line this session: `git diff 8edbfcd 3c5af9a -- src/`. It is
   the basis of P4 below and is summarised in sec 2.

## 2. WHAT THE SOURCE DIFF SAYS SHOULD HAPPEN (the prediction's mechanism)

The merge touched six runtime files. Read for default-mode reachability:

- `VrfSettings.cs` - `TypeMappingMode` default string is still `"RealTemplates"`. Five NEW
  settings added (FriendlyNation, OpposingNation, TypeMapFile, SurfaceProxySubstitutions,
  ProxyMarkingTag); the first four are documented "Ignored unless TypeMappingMode is
  FidelityTable", and the code agrees (next bullet).
- `VrfC2SimService.cs` - every new code path is behind `UsingFidelityTable`, a string compare
  against "FidelityTable": the table LOAD in the constructor, the refuse-to-start pre-flight,
  the `Type-mapping mode = FidelityTable` line, the `TYPE MAP` per-unit lines, the proxy
  marking tag, the `R-SURFACE-PROXY` report push, and the unmapped-unit error. In RealTemplates
  `_typeMap` stays null and none of them can execute. The one unconditional change is the
  `Type-mapping mode = {Mode}` log call moving into an `else` branch with its text UNCHANGED.
- `UnitTranslator.cs` - `Plan()` gained two optional parameters and the table branch is gated
  `typeMapping == FidelityTable && map != null`. Both conjuncts are false in this run.
  `CreationPlan` gained five trailing fields with defaults; the legacy factories do not set them.
- `InitParser.cs` / `InitModels.cs` - `EchelonCode = u.EchelonCode.ToString()` is the ONLY
  unconditional new work on the hot path. It runs in every mode. It is a non-nullable generated
  enum, so it cannot throw or be null; its only consumer is FidelityTable lookup key (c).
  **This is the one line that could make this run differ, and P1/P2 are what would catch it.**
- `ReportBuilder.cs` - a new static method, called only from the FidelityTable branch.
- `Program.cs` - one new `--typemap-selftest` arg branch, before the host builds.

So the source reading predicts BYTE-IDENTICAL BEHAVIOUR. That is exactly the kind of claim that
looks obviously true and is worth one 3-unit run to hold to evidence.

## 3. THE VARIABLE, AND EVERYTHING HELD

CHANGED (one):
  deployed app DLL SHA-256 `570619630015AC3A9B33C77D54A5F13074F620F3CF17A1D71DE10125ACEB52A6`
  (2026-09-02 14:02:48), the MERGED build - against the control's `3b7b8d2e...c60cea0`.

HELD, each verified by a command run this session:
  - fixture `TropicTortoise_FFRTC.scnx`, repo copy and the C:\MAK deploy BOTH hashing
    `D27E540F8BCCAA2EBDD33C63CF062CB4842DBE1937B7AA3DAC1C20F9C990B0B9` (i.e. nothing was
    written under C:\MAK since rung 2).
  - init `data/R9_Mojave_Lean_Initialization_NoComments.xml`; order
    `data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml` (the 44-character route name).
  - `-RunSecs 1800 -SampleSecs 2 -StopWhenComplete`, the control's own values.
  - `Get-ChildItem env:Vrf__*` count **0** - no environment override of any kind, as the
    control had none. Config comes entirely from the deployed appsettings.json, read this
    session: TypeMappingMode RealTemplates, FriendlyNation USA, OpposingNation RUS,
    TypeMapFile data/unit-type-map.json, SurfaceProxySubstitutions true, ClientId **STP**
    (matches the R9 init's SystemName - the CLIENTID TRAP is checked, not assumed).
  - bridge A7504441, NOT rebuilt and not to be rebuilt this session.
  - `vrfSim.mtl` untouched (notifyLevel 3 / objectConsoleNotifyLevel 3).

## 4. INVOCATION

    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 1800 -SampleSecs 2 -StopWhenComplete

APP NUMBERS. The Appendix B marker reads `*** NEXT FREE: 3759 ***` at registration (verified as
the only value-bearing marker line). The runner allocates 7 and advances the marker itself:
expected 3759-3765, marker -> 3766, with 3765 (createOneDiag) consumed only if the stage-7
oracle gate fails. The post-run ResetVrf sweep then takes 3766 by hand, ledgered BEFORE the
join, marker -> 3767. Actuals recorded in sec 7.

WALL BUDGET AND ITS CLOCK. `-RunSecs 1800` is a WALL cap (the runner's observation window).
`-StopWhenComplete` is expected to FIRE this time - the control's failure to fire was the
`$rxB` regex defect, fixed and gated at 105 passed - so the expected wall is ~5-8 minutes of
stages plus ~60-120 s of window, not the control's forced 33 min 38 s. A full 1800 s cap is
NOT a falsifier by itself (P5 covers it); it is a wall ceiling of ~40 minutes either way.

AFTER the run: `tools/ResetVrf <fresh appNo>` with the RUNBOOK :1208-1215 environment -
PATH prefixed with `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin`,
`MAKLMGRD_LICENSE_FILE` from Machine scope, cwd `C:\MAK\vrforces5.0.2\bin64`.

## 5. PRE-LAUNCH INVENTORY (measured this session; must still hold at launch, else STOP)

- NO vrfSim* / vrfGui / vrfLauncher / WatchVrf / ListenReports / VrfC2SimApp process. The only
  matching processes are the RTI trio.
- RTI trio RESIDENT, PIDs unchanged from the whole 2026-09-02 record: rtiAssistant 41336 /
  rtiexec 224608 / rtiForwarder 76620. NEVER killed.
- docker: stp-server Up 25 hours (healthy), c2sim_server4.8.4.9 Up 25 hours, stp-lt511 Up
  3 hours (healthy).
- `Get-ChildItem env:Vrf__*` count 0.
- Newest bin64 dump is still vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp (2026-09-02 06:00).
- MAK license `SALES-TEMP-9-15-26-...lic`, expires 2026-09-15; today 2026-09-02.
- git: branch main at 3c5af9a; working tree clean apart from untracked `.claude/`, a workspace
  file and `tools/analysis/__pycache__/`.
- `--typemap-selftest` on the DEPLOYED merged exe, with the MAK bin PATH prefix:
  **SELF-TEST PASSED (783 checks), exit 0**, parts B and C RAN (composition checks printed), so
  this machine's own C:\MAK catalog matches the table. That is the live gate's run 0, done.

INSTRUMENT CONTROL (the false-greens rule: prove the instrument reproduces the KNOWN result
before trusting it on a new one). `scratchpad/r9_compare.py` and `tools/analysis/frame_gaps.py`
were run over the CONTROL run 20260902T153837Z FIRST, this session, and reproduce its published
numbers exactly: 103 app-log lines, 3 TASKCMPLT, 3 new-form route lines, 0 old-form, 0
`Can't find entity route`, 0 `TYPE MAP` lines, endpoints 34.653915,-116.693388 / 34.612956,
-116.587783 / 34.608416,-116.699993 and net displacements 698.97 / 1162.60 / 1161.56 m, 905
real-coordinate POS samples each; frame_gaps TEST A 32/32 = 100.0%, TEST B R = 0.9983,
|resid| <= 0.0005 s 83/86 = 96.5%, LS slope 7.4281 sim-s per wall-s. Every control number
below is one of those, measured, not quoted from prose. (One deliberate divergence from the
prose: the vendor log is 167,010 lines by `wc -l`, not the outcome section's 167,011 - a
final-line-without-newline artifact. The MEASURED value is the comparator.)

## 6. PREDICTIONS - registered before launch, with confidence, clock, and falsifiers

P1 - THE FIX STILL WORKS (HIGH). The merged build reproduces the route-uuid fix.
  (a) `Can't find entity route` in bin64-vrfSim.log = **0** (control 0; the freezing run
      20260902T143638Z had 67,590). EXACT, no band.
  (b) NEW-form route lines (`Route '<name>' (VRF_UUID:<guid>) created; MoveAlongRoute issued
      for VRF_UUID:<guid>.`) = **3**; OLD form (no parenthetical) = **0**. EXACT.
  (c) ZERO occurrences of the 35-character cut form `T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3"` in
      the vendor log. EXACT.
  FALSIFIER: any of the three off its exact value. That would mean the merge disturbed the
  route addressing, and tasks 2 and 3 do not run.

P2 - THE BEHAVIOUR IS THE CONTROL'S (HIGH).
  (a) **3/3 TASKCMPLT** in vrfc2simapp.log (control 3). EXACT.
  (b) All three taskee endpoints within **0.10 m** of the control's, measured on the
      watchvrf-trace.csv final real-coordinate POS sample per taskee:
        114.MechCoy   34.653915,-116.693388 alt 1116.8
        1222.MechPlt  34.612956,-116.587783 alt 1026.6
        1.BdeHQ       34.608416,-116.699993 alt 1121.1
      This threshold is a DISTANCE, not a rate; no clock applies. It is deliberately looser
      than the control's own 0.00 m against CONTROL-B, because the sampling window will differ.
  (c) Net displacements within **1.0 m** of 698.97 / 1162.60 / 1161.56 m. Distance, no clock.
  FALSIFIER: any endpoint > 0.10 m from the control's, or a TASKCMPLT count != 3.

P3 - THE CLOCK IS IN THE R9 BAND (MEDIUM). frame_gaps.py LS clock slope in
  **[7.0, 11.0] sim-s per WALL-s** - the band the tasking names, which brackets the control's
  7.4281 and CONTROL-B's 10.18 but NOT the frozen run's 13.11 (a frozen company is cheap per
  frame). CLOCK: the slope is by construction sim-seconds per WALL-second; both units are named.
  Mode check unchanged: TEST A >= 95% in {0.033, 0.034} AND TEST B R >= 0.99.
  CONFIDENCE IS ONLY MEDIUM AND THE BAND IS NOT A GATE ON ITS OWN: the slope is load-dependent
  by the FFRTC finding, and the ~3 percentage points of extra work this build does per init
  (one enum ToString per unit) is far below its run-to-run spread (7.43 vs 10.18 on identical
  binaries and near-identical orders). A slope outside the band with P1 and P2 clean is a
  RECORDED ANOMALY to explain, not a build-identity failure - see sec 6A.

P4 - NO NEW LOG FORMS FROM THE TYPE-MAP CODE (HIGH). In vrfc2simapp.log:
  (a) `TYPE MAP` lines = **0**; `R-SURFACE-PROXY` lines = **0**; `REFUSING TO START` = 0.
  (b) The mode line reads EXACTLY, character for character, the control's:
      `Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA) (11.1.225.3.2.0.0)).`
  (c) App-log line count **103**, the control's exactly. This is the sharpest single check that
      no line was added or dropped anywhere in the init/task path.
  FALSIFIER: any FidelityTable-only string present, a different mode line, or a line count
  other than 103 whose cause is not identified.

P5 - HYGIENE (MEDIUM on the first clause, HIGH on the rest). Runner exit 0; every stage exit 0;
  no new .dmp; RTI trio PIDs unchanged; fixture hash unchanged after the run (nothing written
  under C:\MAK); ResetVrf sweep joins clean and reports 0 reflected.
  `-StopWhenComplete` is PREDICTED TO FIRE (the $rxB fix, gated at 105 passed) - MEDIUM, because
  it has never been exercised live on this binary. If it does not fire, the window runs its
  1800 s WALL cap; that costs half an hour and degrades no measurement above (the control proves
  exactly that), so it is recorded, NOT treated as a build-identity failure.

## 6A. THE MISS RULE

P1, P2 and P4 are BUILD-IDENTITY predictions at HIGH confidence with EXACT values. **A miss on
any clause of P1, P2 or P4 is a STOP**: it is written up here, tasks 2 and 3 do NOT run, nothing
is retuned and nothing is re-run, and the finding is that the merge changed default-mode
behaviour - which is a material defect, not a nuisance.

P3 and P5's first clause are MEDIUM and are NOT stops on their own; a miss on either is recorded
with its explanation and the work continues, because neither can be true-or-false about whether
the binary behaves identically. This asymmetry is registered BEFORE the run precisely so it
cannot be invented afterwards to rescue a bad result.

VOID CONDITION. If the run aborts before the order is pushed (a launch, RTI or license failure),
it is an infrastructure event, not a miss; it is recorded and retried once. Two consecutive
infrastructure failures stop the session for research.

## 6B. WHAT A CLEAN PASS WOULD AND WOULD NOT ESTABLISH

WOULD: that the merged binary, at default configuration, reproduces the R9 lean order's
mechanism, behaviour and log surface exactly - so every 2026-09-02 conclusion measured on the
pre-merge binary still stands on this one, and tasks 2 and 3 are adjudicable.

WOULD NOT: anything about FidelityTable mode (task 3 is the only thing that tests it), anything
at SCALE (this is 3 units; rung 2's 128-unit behaviour is NOT re-verified here), and anything
about the native completion-status gap, which is untouched source.

## 7. OUTCOME - run 20260902T181203Z_run, appNos 3759-3765, adjudicated from run-directory artifacts

### VERDICT

**THE MERGED BUILD IS BEHAVIOURALLY IDENTICAL TO THE PRE-MERGE BUILD AT DEFAULT CONFIGURATION.
GATE PASSED. P1, P2, P3, P4 and P5 all PASS; NO registered falsifier fired.** The result is
stronger than the prereg asked for: the two app logs are IDENTICAL LINE FOR LINE, and all three
endpoints match the control TO SIX DECIMAL PLACES - a measured 0.00 m, not the registered 0.10 m.
`-StopWhenComplete` FIRED (its first live exercise on this binary), closing the window at 97.9 s
instead of the control's forced 1800 s cap and cutting the run from 33 min 38 s to 4 min 35.6 s.

### RUN FACTS (all from the run directory)

Run dir `runs/20260902T181203Z_run`. Started 2026-09-02T18:12:03.608Z, order pushed
18:14:22.309Z, observation window closed 18:16:00.178Z (**97.9 s** against its 1800 s WALL cap),
trace stop requested 18:16:30.740Z, manifest saved 18:16:39.199Z - **4 min 35.6 s** wall, against
the control's 33 min 38 s. appNumbers 3759-3765; ledger `wasValue` 3759 -> `newValue` 3766,
`advanced` true, taken BEFORE any join; 3765 (createOneDiag) UNCONSUMED, the stage-7 oracle gate
having passed (44 real-coordinate POS lines across 44 distinct uuids). `runnerExitCode` **0** and
every one of the ten stages exit 0 (RtiProbe, LaunchVrf, WatchVrf-precheck, WatchVrf-trace,
ListenReports, PushInit, VrfC2SimApp, PushOrder, StopIface, StopVrf). One `validityFlags` entry,
severity INFO, the standing "Pre-init oracle pre-check saw NO real-coordinate POS line. EXPECTED
on a stock TropicTortoise" advisory. `inputs.scenario` = `TropicTortoise_FFRTC`.
`Get-ChildItem env:Vrf__*` = 0 before the run and 0 after.

### P1 - THE FIX STILL WORKS. PASS on all three clauses, all EXACT.

(a) `buildEntityRouteFollowingMap() : Can't find entity route` in bin64-vrfSim.log: **0**
    (predicted 0; control 0; the freezing run 20260902T143638Z 67,590).
(b) NEW-form route lines **3**, OLD form **0** (predicted 3 / 0). Verbatim from
    vrfc2simapp.log, the 44-character name intact and carrying the route's own uuid:
      `Route 'T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE'
      (VRF_UUID:...) created; MoveAlongRoute issued for VRF_UUID:...`
(c) The 35-character cut form `T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3"` in the vendor log: **0**.

### P2 - THE BEHAVIOUR IS THE CONTROL'S. PASS on all three, at ZERO.

(a) **3/3 TASKCMPLT** (predicted 3).
(b)+(c) The endpoints are not "within 0.10 m" - they are the control's DIGITS:

    | taskee | this run, final trace POS | control 20260902T153837Z | delta | net displacement |
    |---|---|---|---|---|
    | 114.MechCoy  | 34.653915,-116.693388 alt 1116.8 | identical | **0.00 m** | 698.97 m (control 698.97) |
    | 1222.MechPlt | 34.612956,-116.587783 alt 1026.6 | identical | **0.00 m** | 1162.60 m (control 1162.60) |
    | 1.BdeHQ      | 34.608416,-116.699993 alt 1121.1 | identical | **0.00 m** | 1161.56 m (control 1161.56) |

    Registered threshold 0.10 m on the endpoint and 1.0 m on the net displacement; measured
    0.00 m and 0.00 m. 49 real-coordinate POS samples per taskee (control 905) - fewer only
    because -StopWhenComplete closed the window at 97.9 s instead of running the 1800 s cap.
    The three taskee VRF uuids DIFFER from the control's, as they must: uuids are per-join.

### P3 - THE CLOCK IS IN THE R9 BAND. PASS.

`python tools/analysis/frame_gaps.py . 20260902T181203Z_run`:

| statistic | THIS RUN | CONTROL 153837Z | registered |
|---|---|---|---|
| lines / stamped / distinct sim stamps | 21450 / 393 / 86 | 167010 / 404 / 86 | - |
| TEST A in {0.033, 0.034} | 30/30 = 100.0% | 32/32 = 100.0% | >= 95% |
| TEST B resultant length R | **0.9985** | 0.9983 | >= 0.99 |
| LS slope sim-s per WALL-s | **9.7687** | 7.4281 | in [7.0, 11.0] |
| TEST B \|residual\| <= 0.0005 s | 94.2% (81/86) | 96.5% (83/86) | not a registered criterion |

Both registered mode criteria pass; the slope is inside the band. RECORDED, NOT REGISTERED AND
NOT A MISS: the |residual| statistic came in at 94.2% against the control's 96.5%. The handoff's
mode check is TEST A >= 95% AND R >= 0.99 (both met, and frame_gaps.py prints no verdict of its
own), so this is an observation, not a threshold. It is 81/86 vs 83/86 - two stamps - on a
sample a fifth the size of the control's vendor log, which is the expected behaviour of a
proportion measured over a 98-second window instead of a 30-minute one. It is not evidence of a
mode change: R rose, and TEST A is at 100%.

The slope rose from 7.4281 to 9.7687 on an identical order and fixture. That is INSIDE the
FFRTC block's documented load-dependence and inside the R9 family's own spread (7.43 / 10.18 /
13.11 on three runs of two binaries), and the mechanism is visible in this run: the observation
window closed at 97.9 s, so the measured interval is dominated by the movement phase and does
not include the control's 28 extra minutes of three parked units with the window still open.
The prereg registered P3 at MEDIUM for exactly this reason and it is not load-bearing.

### P4 - NO NEW LOG FORMS. PASS, and the check is stronger than registered.

(a) `TYPE MAP` **0**; `R-SURFACE-PROXY` **0**; `REFUSING TO START` **0**; the case-insensitive
    union of `fidelitytable|R-SURFACE-PROXY|AuthoredPending` **0**. None of the merged code's
    log surface is reachable at default configuration, as the source reading said.
(b) The mode line is the control's, character for character:
    `Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA) (11.1.225.3.2.0.0)).`
(c) App-log line count **103**, the control's exactly.
(d) STRONGER, AND NOT REGISTERED IN ADVANCE (recorded as a bonus check, not as a passed
    prediction): the two app logs were normalised - every digit to `#`, every uuid to `<UUID>` -
    and diffed. **Zero diff hunks over 103 lines.** Not merely the same count: the same lines,
    in the same order, with the same text. That is the sharpest available statement that the
    merge changed nothing on the init/task path at default configuration.

### P5 - HYGIENE. PASS, including the MEDIUM clause.

`-StopWhenComplete` **FIRED**, the clause registered at MEDIUM because it had never run live on
this binary. The console shows the full rule-4 evidence chain: `TASKCMPLT seen for 3/3
taskee(s) ... (t+5s)`, then `report evidence IN for all 3 taskee(s) at t+5s: 1222.MechPlt RPT
t=57.2 vs POS 0 m; 114.MechCoy RPT t=57.2 vs POS 0 m; 1.BdeHQ RPT t=57.5 vs POS 0 m`, then the
60 s hold floor, then close at 97.9 s. The `$rxB` regex fix from the route-uuid run is confirmed
LIVE against the new log form - the defect that cost that run half an hour is closed on evidence,
not on a unit test alone.

EVERYTHING ELSE CLEAN. Vendor-log censuses THIS / CONTROL: `invalid formation name` 1/1 (the
standing cosmetic baseline), `moveAlong() - empty route` 0/0, `Waiting for nav data` 0/0, FATAL
0/0, SocketException 0/0. No new .dmp - newest is still
vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp (2026-09-02 06:00). The FFRTC fixture at
C:\MAK\...\userData\scenarios\ still hashes D27E540F8BCC...B0B9 and vrfSim.mtl still stamps
2026-09-01 14:32:14, i.e. NOTHING WAS WRITTEN UNDER C:\MAK. RTI trio PIDs UNCHANGED and never
touched (rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620). No VR-Forces process and no
observer remains. StopVrf exit 0, "graceful; RTI infrastructure preserved".

POST-RUN SWEEP: `tools/ResetVrf 3766`, run WITH the RUNBOOK :1208-1215 environment (PATH
prefixed with the VR-Forces / VR-Link / makRti bin dirs, MAKLMGRD_LICENSE_FILE from Machine
scope, cwd C:\MAK\vrforces5.0.2\bin64) - the recipe 3757 was burned for skipping. Result: joined
clean (BackendCount=0), discovered 0 reflected (0 deletable, 0 nil), resigned cleanly, **exit 0**.
ZERO LEFTOVERS. Standing caveat (rung-1 finding D): the sweep runs AFTER StopVrf, so it proves
NO STALE FEDERATE and nothing about scenario contents.
LEDGER: marker 3759 -> 3766 (7, by the runner) -> 3767 (1, hand-taken and ledgered BEFORE the
join, for the sweep). Exactly 7 + 1, as predicted.

### ADVERSARIAL REVIEW

THE STRONGEST COMPETING HYPOTHESIS: **the merged build was never actually the binary under
test.** A run that reproduces its control to six decimals is exactly what you would see if the
runner had launched a stale copy - and there is a real mechanism for it, because the runner
starts `VrfC2SimApp.exe`, not the `.dll` whose hash the tasking named. FALSIFYING OBSERVATION,
checked: the runner logged the launch by ABSOLUTE PATH into
`src\VrfC2SimApp\bin\Release\net10.0\win-x64\`, and that directory's `VrfC2SimApp.dll` hashes
`570619630015AC3A9B33C77D54A5F13074F620F3CF17A1D71DE10125ACEB52A6` - the merged build - with the
`.exe` and the `.dll` carrying the SAME write stamp, 2026-09-02 14:02:48, i.e. one build. The
.NET apphost `.exe` loads the `.dll` from beside itself and that directory holds exactly one.
(Other `VrfC2SimApp.dll` copies exist on this machine, under `obj\` and under
`.claude\worktrees\`; neither is on the launched path.) Independently: `--typemap-selftest` is
a MERGE-ONLY code path
(Program.cs gained it in 3c5af9a), and it ran to `SELF-TEST PASSED (783 checks)` on that same
`.exe` this session. A pre-merge binary would have rejected the argument. The merged build ran.

SECOND HYPOTHESIS: **the endpoints agree because they are terrain-clamped attractors, not
because the code agrees** - i.e. any build that moves at all would land there, so P2 has no
discriminating power. Partly TRUE and worth stating: the endpoints are route termini, so they
are not sensitive to small behavioural differences. That is why P2 was never the whole gate.
The discriminating checks are P4(c) and P4(d): a 103-line app log that diffs to ZERO HUNKS after
normalisation is not an attractor - it is a statement about every line the init and task paths
emitted. Between them the two are enough; either alone would not be.

UNEXPLAINED, AND CARRIED FORWARD: nothing from this run. The one statistic that moved
unfavourably (TEST B |residual| 96.5% -> 94.2%) has a stated mechanism - a 97.9 s window instead
of a 1800 s one - and it moved two stamps on a non-criterion while the actual criterion (R)
moved the other way. It is recorded here rather than dropped, but it is not a loose end.

WHAT THIS DOES NOT ESTABLISH, restated from sec 6B because it matters for what comes next: this
is 3 units. Rung 2's 128-unit behaviour is NOT re-verified on the merged binary by this run, so
task 2 carries "the DLL differs from rung 2" as a known second difference and must say so.

### CONSEQUENCE

The gate is open. Task 2 (`-q` at scale) and task 3 (the FidelityTable live gate) are
adjudicable on this binary, and every 2026-09-02 conclusion measured on 3b7b8d2e stands on
570619630015 as well. Separately, this run IS `docs/NEXT_TYPE_MAPPING_LIVE_GATE.md`'s **run 1**,
the RealTemplates control, complete with its vendor log - so that gate's run 2 has its A/B
partner, and its run 0 (`--typemap-selftest` on this machine: 783 checks, exit 0, parts B and C
executed) is done.

## 8. REGISTRATION

Sections 0-6B written and committed BEFORE launch as **0f75f29**. Sec 7 added after the run,
from the run-directory artifacts only.
