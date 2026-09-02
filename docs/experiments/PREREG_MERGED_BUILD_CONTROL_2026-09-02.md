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

## 7. OUTCOME

(written after the run, from the run-directory artifacts only)

## 8. REGISTRATION

Sections 0-6B written and committed BEFORE launch. Commit hash stamped in sec 7.
