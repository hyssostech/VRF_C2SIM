# PREREG - TYPE-MAPPING LIVE GATE, R9 lean (task 3 of 3, 2026-09-02)

ONE VARIABLE: **`Vrf:TypeMappingMode` = RealTemplates -> FidelityTable**, set by environment
variable, no rebuild, no tracked-file change. Fixture, init, order, window, binary and bridge are
all held at run 20260902T181203Z's values - that run is this one's CONTROL and was made this
session for exactly this purpose.

THIS IS `docs/NEXT_TYPE_MAPPING_LIVE_GATE.md` **run 2**. Its run 0 (`--typemap-selftest` on this
machine) and run 1 (the RealTemplates control) are DONE, this session, and are quoted below.

WHAT IS ACTUALLY AT STAKE. Every "resolves to" claim in
`docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md` and every `objectType` in
`data/unit-type-map.json` is STATIC best-match arithmetic over the `.entity` files. One live
observation has disagreed with the static picture since 2026-07-15 and has never been reconciled
(`docs/VRF_GROUND_TRUTH.md` 0.1.8 item 1: `114.MechCoy`'s formation list came back all-lowercase,
the `Ground_Aggregate` signature, where the static rule says a real template). Sec 11.5 of the
survey calls it "the one unreconciled falsifier for the whole static method". This run settles
whether the static best-match method predicts LIVE CREATION. **If the gate fails, that is a
finding about the METHOD: I record which rows differ and DO NOT adjust the table to fit.**

## 1. DOCS AND SOURCES CONSULTED (docs first, per the 2026-09-01 standing rule)

1. `docs/NEXT_TYPE_MAPPING_LIVE_GATE.md` - all of it: sec 1 (the configuration), sec 2 (the run
   order), sec 3 (what to read and where), sec 4 (the six expected creation lines), sec 6 (the
   PRC refusal), sec 7 (falsifiers 1-7), sec 8 (the deliverable).
2. `docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md` **sec 7.5** (the four verification steps;
   item 3 is this run) and **sec 11** in full - 11.1 files, 11.2 the three judgement calls
   applied as PROVISIONAL, 11.3 the offline verification actually performed, 11.4 the nine
   findings that differ from the survey's own claims, 11.5 what is outstanding. Also sec 4.3
   (the R9 census) and sec 3.5 (the zero-subordinate empty-abstract trap the gate must not hit).
3. `data/unit-type-map.json` - the three rows this fixture exercises, read verbatim this
   session and reproduced in sec 3 below.
4. **THE VENDOR'S OWN LOG, and its verbosity knob, BEFORE probing** (memory
   `lessons-vendor-diagnostics-first`). Users Guide sec 4.9 p.161 (Windows writes
   `bin64\vrfSim.log` unconditionally) and sec 5.4.3 p.185 (notification levels 0-4, default 2);
   `C:\MAK\vrforces5.0.2\appData\settings\vrfSim\vrfSim.mtl:205` is `notifyLevel 3` = Verbose.
   **The channel was CONFIRMED OPEN BEFORE REGISTRATION, not assumed:** at this level the log
   already prints, for every object it builds,
     `Locally Simulated: <name> (VRF_UUID:<uuid>) using parameters:
      ..\data\simulationModelSets\<set>\vrfSim\<Template>.entity`
   That line is the back end naming the `.entity` file it resolved, which is precisely the
   evidence sec 3 item 1 of the gate asks for. NO notify-level change is needed and NONE is
   made - `vrfSim.mtl` is not edited (that needs USER approval we do not have).
5. `docs/experiments/PREREG_MERGED_BUILD_CONTROL_2026-09-02.md` sec 7 - the gate that makes this
   binary adjudicable, and the source of the control numbers below.
6. `docs/HANDOFF_2026-09-01_R9_COMPLETE.md` - PROBE PROTOCOL, NON-NEGOTIABLES, CLIENTID TRAP.
7. THE SOURCE, read this session: `UnitTranslator.FromTable/FromRow`, `UnitTypeMap.ResolvePath`,
   `VrfC2SimService` (the `UsingFidelityTable` pre-flight, the loud skip, the `~PXY` marking and
   its 34-character bound, `BuildTypeSubstitutionReport`), and the `_vrfUuidByName` /
   `CreatedUnit` correlation chain - see sec 4's marking analysis.

## 2. THE CONTROL - run 20260902T181203Z, measured this session, not quoted

`--typemap-selftest` on the DEPLOYED merged exe with the MAK bin PATH prefix:
**`SELF-TEST PASSED (783 checks)`, exit 0**, with parts B and C EXECUTED (composition lines
printed, so `C:\MAK` was found and the rows were checked against THIS machine's catalog). That
is the gate's run 0 and it did not skip.

The gate's run 1, `TypeMappingMode=RealTemplates`, same fixture/init/order as this run. Its
`bin64-vrfSim.log` names, for the six units under test:

  1141.MechPlt -> EntityLevel/Tank Platoon (USA)          1222.MechPlt -> EntityLevel/Tank Platoon (USA)
  1142.MechPlt -> EntityLevel/Tank Platoon (USA)          114.MechCoy  -> EntityLevel/Tank Company (USA)
  1143.MechPlt -> EntityLevel/Tank Platoon (USA)          1.BdeHQ      -> EntityLevel/M1A2_Abrams_MBT

Which is EXACTLY what the gate doc's sec 4 predicted for the control ("all four D units become
Tank Platoon (USA), 114.MechCoy becomes Tank Company (USA), and 1.BdeHQ becomes a single
M1A2_Abrams_MBT"), and it is the first live confirmation of ANY static claim in the survey.
Full control census, 62 distinct locally-simulated objects: 31 M1A2_Abrams_MBT, 13 base/Route,
7 Tank Platoon (USA), 2 M998 HMMWV, 1 each of M3A2_Bradley_CFV, M577A2_Command_Post, Tank
Company (USA), Tank Headquarters Section (USA), Blocking Terrain Page-in Area, Global Dynamic
Terrain, global-environment-parameters, top-level-entity. **`Ground_Aggregate`: ZERO objects.**
`TYPE MAP` lines 0, `NameObservation` reports 0, 3/3 TASKCMPLT, endpoints as in the task-1 record.

A NOTE ON THE INSTRUMENT, and an artifact it must not be allowed to hide: the vendor log
INTERLEAVES writes from several threads, so a minority of lines arrive spliced (the control has
`base/Rout1e` and `M1A2 13's Offset Route (M1A2 15VRF_UUID:...` as visible examples). The census
reports what it parses; a garbled line is a LOGGING artifact, not a template, and any
unexpected template name is checked against `C:\MAK\vrforces5.0.2\data\simulationModelSets\`
before it is believed.

## 3. THE THREE TABLE ROWS THIS FIXTURE EXERCISES (verbatim from data/unit-type-map.json)

| id | functionId/echelon | isAggregate | objectType | templateName | fidelity |
|---|---|---|---|---|---|
| `F-UCIZ-D` | UCIZ / D (PLT) | true | `3:11:1:225:3:4:0:0` | Mechanized Platoon (USA) IFV (Deprecated) | **EXACT** |
| `F-UCIZ-E` | UCIZ / E (COY) | true | `3:11:1:225:3:4:0:0` | Mechanized Platoon (USA) IFV (Deprecated) | **PROXY** |
| `F-UCIZ-H` | UCIZ / H (BDE) | **false** | `1:1:1:225:3:11:0:0` | M577A2_Command_Post | **PROXY** |

`Mechanized Platoon (USA) IFV (Deprecated).entity` and `M577A2_Command_Post.entity` both EXIST
in `C:\MAK\vrforces5.0.2\data\simulationModelSets\EntityLevel\vrfSim\` - verified by directory
listing this session, so a "not created" outcome cannot be blamed on a missing file.

A DISCREPANCY IN THE RECORD, registered so it cannot be quietly resolved afterwards: the gate
doc's sec 4 says three units match on key (a) (the init's own SISOEntityType, JC-1) and three
fall to the key-(b) backstop, while the survey's sec 11.3 dry-run table says **key (b) for all
6**. The two disagree. They may simply be about different files - 11.3's table names
`R9_Mojave_Lean_Initialization.xml`, this run uses `R9_Mojave_Lean_Initialization_NoComments.xml`.
IT DOES NOT CHANGE ANY PREDICTED TEMPLATE (both routes land the same three rows), so it is not a
falsifier; the run's `TYPE MAP` lines print `key=` and will say which is right. Recorded in sec 7.

## 4. THE MARKING TAG - what the source says will happen, before it happens

`Vrf:SurfaceProxySubstitutions` defaults **true**, so each PROXY unit gets `~PXY` appended to its
created-object name when the result stays within `MaxVrfMarkingChars = 34`
(`VrfC2SimService.cs`; the bound exists because the back end resolves marking-text references
through a 35-byte blob at `rwUUID.h:412` - the mechanism behind the 2026-09-02 route-uuid
failure). Arithmetic: `114.MechCoy` (11) + `~PXY` (4) = **15**; `1.BdeHQ` (7) + 4 = **11**. Both
are far inside 34, so BOTH PROXY units are expected to be created under TAGGED names and the
`Proxy marking tag NOT appended` warning is expected ZERO times.

DOES THE TAG BREAK TASKING? Falsifier #4 says it might. Source reading says NO, and here is the
chain, so the prediction is a claim about code and not a hope: the tag is applied to `plan.Name`
BEFORE the plan is used, and every downstream key is `plan.Name` - `_unitByC2SimUuid[uuid] = new
CreatedUnit(plan.Name, ...)`, `_c2SimUuidByName[plan.Name]`, `_pendingAltitude[plan.Name]`. On
the other side `OnVrfObjectCreated` writes `_vrfUuidByName[e.Name]`, and `e.Name` is the created
object's name, i.e. the SAME tagged string. `ExecuteTaskOnTick` then looks up
`_vrfUuidByName[unit.Name]` where `unit` is that `CreatedUnit`. Tagged on both sides, so it
matches. The C2SIM-facing identifiers are uuids and are untouched.

## 5. CONFIGURATION AND INVOCATION

    $env:Vrf__TypeMappingMode = 'FidelityTable'
    Get-ChildItem env:Vrf__*             # echoed into the console log
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 1800 -SampleSecs 2 -StopWhenComplete

`FriendlyNation=USA`, `OpposingNation=RUS`, `TypeMapFile=data/unit-type-map.json` and
`SurfaceProxySubstitutions=true` are already the deployed `appsettings.json` values and are left
alone; the gate doc's sec 1 sets them explicitly, and setting a value to what it already is
would be two variables' worth of noise for none. All six R9 units are friendly (`SF...`), so
`OpposingNation` does not bite here - the PRC refusal (gate run 3) is NOT part of this run.
`Vrf:ClientId` must be **STP** for the R9 init (the CLIENTID TRAP); it is restored to STP after
task 2's COA-STP1 run and re-checked at launch.

WALL BUDGET AND ITS CLOCK. `-RunSecs 1800` is a WALL cap, the control's own value.
`-StopWhenComplete` fired on the control at 97.9 s for a total of 4 min 36 s wall; the same is
expected here. A full 1800 s WALL cap is not a falsifier on its own.

PRE-FLIGHT THAT MUST PASS BEFORE LAUNCH, and would otherwise waste a run: the app runs with
**cwd = `C:\MAK\vrforces5.0.2\bin64`** (`RunC2SimScenario.ps1:1912`), and `UnitTypeMap.ResolvePath`
tries the cwd, then `AppContext.BaseDirectory`, then walks UP from it. `bin64\data\` does not
exist and neither does `...\win-x64\data\`, so the table can only be found by the parent walk
reaching the repo root. If it is not found the service logs `REFUSING TO START` and stops.
So: run `--typemap-selftest` FROM cwd `C:\MAK\vrforces5.0.2\bin64`. If it cannot resolve, the fix
is to pass an ABSOLUTE `Vrf__TypeMapFile`, which is a second variable and would be registered as
such. **DONE BEFORE REGISTRATION, AND IT PASSES:** from that cwd, with the MAK bin PATH prefix,
the app printed
  `table: 123 rows from C:\...\Software\Interfaces\VRF_C2SIM\data\unit-type-map.json`
then `SELF-TEST PASSED (783 checks)`, exit 0. The parent walk from `AppContext.BaseDirectory`
reaches the repo root, as the source said it would. No absolute path is needed and none is set.

APP NUMBERS: from the Appendix B marker at registration; the runner allocates 7 and advances it,
the post-run ResetVrf sweep takes one more by hand, ledgered BEFORE the join, with the RUNBOOK
:1208-1215 environment. Actual values in sec 7.

## 6. PREDICTIONS - registered before launch, with confidence and falsifiers

P1 - **THE SIX CREATION LINES.** HIGH confidence, EXACT template names. `bin64-vrfSim.log` must
     carry `Locally Simulated: <name> ... using parameters: ...\vrfSim\<Template>.entity` with:

     | unit | expected template | row | key | fidelity |
     |---|---|---|---|---|
     | `1141.MechPlt` | Mechanized Platoon (USA) IFV (Deprecated) | F-UCIZ-D | (a) or (b) | EXACT |
     | `1142.MechPlt` | Mechanized Platoon (USA) IFV (Deprecated) | F-UCIZ-D | (a) or (b) | EXACT |
     | `1143.MechPlt` | Mechanized Platoon (USA) IFV (Deprecated) | F-UCIZ-D | (a) or (b) | EXACT |
     | `1222.MechPlt` | Mechanized Platoon (USA) IFV (Deprecated) | F-UCIZ-D | (b), after a Country-71 backstop WARN | EXACT |
     | `114.MechCoy~PXY` | Mechanized Platoon (USA) IFV (Deprecated) | F-UCIZ-E | (b) | PROXY |
     | `1.BdeHQ~PXY` | M577A2_Command_Post | F-UCIZ-H | (b) | PROXY |

     THE KEY COLUMN IS NOT A THRESHOLD (sec 3's registered discrepancy); the TEMPLATE column is.
     FALSIFIER 1: any of the six names a different `.entity` than its row's `templateName`. That
     is gate falsifier #1 - the static best-match rule would then be WRONG and the whole
     `objectType` column has to be re-derived from live evidence. RECORD THE ROW, THE OBJECT
     TYPE WE SENT AND THE MODEL THE BACK END BUILT. DO NOT ADJUST THE TABLE.

P2 - **NO GENERIC, NO EMPTY UNIT.** HIGH.
  (a) `Ground_Aggregate` (gui-label "Ground Unit") objects: **0** (control: 0). Gate falsifier #2.
  (b) Each of the five aggregate units publishes a NON-EMPTY subordinate set - the sec-3.5
      empty-abstract trap. Measured as: the census contains platform templates consistent with
      `Mechanized Platoon (USA) IFV`'s composition (M2A2 Bradley plus dismounted squads) where
      the control had 31 `M1A2_Abrams_MBT`, and the total distinct locally-simulated object
      count is **>= 30** (control: 62). Gate falsifier #3.
      THIS BOUND IS DELIBERATELY LOOSE. The composition changes wholesale - a mech platoon is
      not a tank platoon - so predicting an exact count would be predicting the vendor's
      content, not our mapping. The claim under test is "composed, not empty".
  (c) `TYPE MAP AuthoredPending` / `TYPE MAP Failed` lines: **0**, and units created **6**.
      Gate falsifier #7 (a silent skip). The `Init dispatched: 6 units` line must still read 6.

P3 - **THE MODE ACTUALLY TOOK EFFECT.** HIGH, and it is the cheapest way to void a meaningless
     run (gate falsifier #6).
  (a) The app log's mode line reads `Type-mapping mode = FidelityTable (ground dispatch from
      <path>; friendly=USA, opposing=RUS).` - NOT the control's RealTemplates sentence.
  (b) `TYPE MAP` lines: **exactly 6**, one per unit, each naming a row id and a key.
  (c) The six creation lines DIFFER from the control's. If runs 1 and 2 produce identical
      creation lines the config did not take effect and the run proves nothing - VOID, re-run
      after fixing the configuration, not a miss.

P4 - **THE PROXY SUBSTITUTION SURFACES, AND THE TAG FITS.** HIGH (see sec 4's source reading).
  (a) `NameObservation` reports in `reports-captured.log`: **exactly 2** (control: 0), one for
      `114.MechCoy` and one for `1.BdeHQ`, each carrying its `templateName` substitution text in
      `Marking`. The app log's `2 PROXY substitution(s) surfaced to C2SIM (R-SURFACE-PROXY)`
      line must be present.
  (b) `Proxy marking tag NOT appended` warnings: **0**. Both tagged names are 15 and 11
      characters against the 34-character bound.
  (c) Both PROXY units are created under their TAGGED names and neither name is truncated in
      the vendor log. Gate falsifier #4, first half.

P5 - **TASKING STILL RESOLVES, AND THE UNITS STILL MOVE.** HIGH, on the sec-4 chain.
  (a) **3/3 TASKCMPLT** (control: 3). `DROPPING TASK ... WAS NOT CREATED`: 0.
  (b) `Can't find entity route`: **0** EXACT. The route fix is independent of the mapping and
      must stay fixed.
  (c) All three taskees show non-zero net displacement. NOT predicted to match the control's
      metres: a Mechanized Platoon IFV and a command-post track have different speeds and
      formations than a Tank Platoon and an MBT, so the ENDPOINTS MAY LEGITIMATELY DIFFER.
      Threshold: each taskee net displacement **> 100 m**. CLOCK: a distance, no clock; but it
      accrues over sim time, and `-StopWhenComplete` closes the window on completion, so
      completion (a) is the real gate and (c) is the corroboration.
      Gate falsifier #4, second half ("its tasking stops resolving").

P6 - **THE 2026-07-15 FALSIFIER.** MEDIUM, and it is the single most important thing this run
     can find (gate falsifier #5). PREDICTION: `114.MechCoy~PXY` does NOT come back with the
     `Ground_Aggregate` signature; its creation line names a real composed template.
     WHAT I CAN AND CANNOT MEASURE, stated now rather than after: the app does NOT call
     `RequestAvailableFormations` on any normal run path (the bridge exports it; no C# call site
     exists), so THE LITERAL 2026-07-15 ARTEFACT - a formation LIST - IS NOT REPRODUCIBLE BY
     THIS RUN. What this run substitutes is strictly stronger for the question actually asked:
     the back end's own statement of which `.entity` it built. The control already shows
     `114.MechCoy -> Tank Company (USA)`, a real composed template, with the only lowercase
     formation-name warning (`invalid formation name "column-left"`) attached to `AR HQ Sec 1`,
     a SUBORDINATE of that real company - which is the handoff's known-cosmetic HQ-section
     warning. So the 2026-07-15 reading is already in doubt; this run tests whether it survives
     under FidelityTable too. I will report the `invalid formation name` census either way and
     will NOT claim the 2026-07-15 item closed on formation evidence I did not collect.

P7 - **HYGIENE AND MODE.** HIGH. Runner exit 0, every stage exit 0; FFRTC mode check passes
     (TEST A >= 95% in {0.033, 0.034} AND R >= 0.99); no new .dmp; the fixture still hashes
     D27E540F8BCC...B0B9 and vrfSim.mtl still stamps 2026-09-01 14:32:14 (nothing written under
     C:\MAK); RTI trio PIDs unchanged; ResetVrf sweep clean, exit 0; `env:Vrf__*` empty after.

## 6A. THE MISS RULE

A miss on P1 is **the finding**, not a failure of the run: it means the static best-match method
does not predict live creation, and the response is to record every differing row (id, the
objectType we sent, the model the back end built) and STOP. **The table is NOT adjusted to fit
the observation** - that would destroy the only evidence the gate exists to produce.

A miss on P2, P4, P5 or P7 is a STOP and is written up: each is a safety property (no empty
units, substitutions never silently swallowed, tasking still resolves, nothing damaged).

P3(c) is a VOID condition, not a miss - identical creation lines mean the configuration never
reached the process, and the correct response is to fix the configuration and re-run, recording
that the first attempt proved nothing.

P6 is MEDIUM and its instrument is explicitly partial (sec 6 P6). It cannot be a stop, and it
cannot be reported as closing the 2026-07-15 item on its own.

PRE-AUTHORISED BUT NOT TAKEN THIS SESSION: the gate doc offers a fallback run with
`Vrf:SurfaceProxySubstitutions=false` if a tagged unit breaks. That is a SECOND VARIABLE. If P4
or P5 fires I will record it and stop; the fallback needs its own registration.

VOID CONDITION. An abort before the order is pushed is infrastructure, not a miss: recorded,
retried once. Two consecutive infrastructure failures stop the session for research.

## 6B. WHAT A CLEAN PASS WOULD AND WOULD NOT ESTABLISH

WOULD: that the static best-match method PREDICTS LIVE CREATION on this install, for the three
rows this fixture exercises; that R-SURFACE-PROXY works end to end (marking plus report stream);
and that `FidelityTable` is safe to run. It would make the gate's deliverable list EMPTY for
these rows.

WOULD NOT: it would NOT validate the other 120 rows - three rows are not 123, and the gate's own
run 4 (COA-STP1 at 128 units, 28 EXACT / 100 PROXY) is what would test the rest. It would NOT
make `FidelityTable` the default - that is a user decision on an artifact the user reviews line
by line. It would NOT close the PRC refusal (gate run 3, not run here), the sec 7.4 authoring
queue, or the second-hand function-ID decodes (survey 11.5 R3). And it would NOT close the
2026-07-15 formation observation on formation evidence, for the reason P6 states.

## 7. OUTCOME - run 20260902T193508Z_run, appNos 3775-3781, adjudicated from run-directory artifacts

### VERDICT

**THE GATE PASSES. ALL SIX UNITS LANDED EXACTLY THE TEMPLATE `data/unit-type-map.json` NAMES.**
The static best-match method PREDICTS LIVE CREATION on this install. The gate's deliverable list -
"for every row whose live landing differs from the table, the row id, the objectType we sent, and
the model the back end actually built" - is **EMPTY**. P1 through P7 all pass; no registered
falsifier fired, and none of the gate doc's seven falsifiers fired either.

TWO THINGS THE RUN FOUND THAT WERE NOT REGISTERED, both recorded rather than absorbed:
  **(A) THE `~PXY` MARKING TAG BREAKS THE RUNNER'S RULE-4 EVIDENCE GATE.** Not the app's tasking -
  that is clean (0 `DROPPING TASK`, 3/3 TASKCMPLT, all three taskees moved), exactly as sec 4's
  source reading predicted. It is `scripts/RunC2SimScenario.ps1`'s `-StopWhenComplete` rule 4,
  which matches the C2SIM taskee NAME against the trace's TSK **marking**. The trace records
  `TSK,50.1,"1.BdeHQ~PXY"` and `TSK,115.6,"114.MechCoy~PXY"` while the runner looks for
  `1.BdeHQ` and `114.MechCoy`; `1222.MechPlt` (EXACT, untagged) matched fine. So the window never
  closed early and ran its full 1800 s WALL cap. MY sec-4 ANALYSIS TRACED THE APP'S KEY CHAIN AND
  NOT THE RUNNER'S - that is the gap, and it is mine.
  **(B) A FOREIGN C2SIM INITIALIZATION LANDED ON THE SHARED SERVER MID-WINDOW**, 23 minutes after
  every piece of gate evidence was already collected, and the runner correctly flagged the run
  NOT VALID under 4a.6. See "THE VALIDITY FLAG" below for exactly what it does and does not touch.
  **(A) CAUSED THE EXPOSURE TO (B):** had rule 4 matched, the window would have closed at ~t+126 s
  and this run would have been over long before the foreign init arrived.

### RUN FACTS

Run dir `runs/20260902T193508Z_run`. appNumbers 3775-3781; ledger `wasValue` 3775 -> `newValue`
3782, `advanced` true; 3781 (createOneDiag) UNCONSUMED. `runnerExitCode` **0**; every stage exit 0.
`Get-ChildItem env:Vrf__*` = `Vrf__TypeMappingMode=FidelityTable` before, **0** after. App log 142
lines (control: 103 - the extra are the 6 TYPE MAP lines, the proxy-substitution line, the mode
line, and the foreign-init block). Vendor log 109,378 lines.

**THE PRE-FLIGHT HELD LIVE:** the app's own first log line reads `Type-mapping mode =
FidelityTable (123 rows from C:\...\VRF_C2SIM\data\unit-type-map.json); FriendlyNation=USA,
OpposingNation=RUS; SurfaceProxySubstitutions=True.` - the table resolved from cwd `bin64` by the
parent walk, exactly as sec 5 registered, and the JC-2 pre-flight passed (`REFUSING TO START` 0).

### P1 - THE SIX CREATION LINES. **PASS, all six, EXACT.**

`bin64-vrfSim.log`, the back end naming the `.entity` file it resolved:

| unit (as created) | template the BACK END built | table says | row | key |
|---|---|---|---|---|
| `1141.MechPlt` | EntityLevel/**Mechanized Platoon (USA) IFV (Deprecated)** | same | F-UCIZ-D | **(a)** |
| `1142.MechPlt` | EntityLevel/**Mechanized Platoon (USA) IFV (Deprecated)** | same | F-UCIZ-D | **(a)** |
| `1143.MechPlt` | EntityLevel/**Mechanized Platoon (USA) IFV (Deprecated)** | same | F-UCIZ-D | **(a)** |
| `1222.MechPlt` | EntityLevel/**Mechanized Platoon (USA) IFV (Deprecated)** | same | F-UCIZ-D | **(b)** |
| `114.MechCoy~PXY` | EntityLevel/**Mechanized Platoon (USA) IFV (Deprecated)** | same | F-UCIZ-E | **(b)** |
| `1.BdeHQ~PXY` | EntityLevel/**M577A2_Command_Post** | same | F-UCIZ-H | **(b)** |

Six of six. Against the control's Tank Platoon (USA) x4 / Tank Company (USA) / M1A2_Abrams_MBT -
a wholesale change, so P3(c) is satisfied and the config demonstrably took effect.

**THE SEC-3 DISCREPANCY IS RESOLVED, IN THE GATE DOC'S FAVOUR.** The gate doc said three units
match on key (a) and three on the backstop; the survey's 11.3 dry-run table said key (b) for all
six. The live `TYPE MAP` lines say the GATE DOC is right for `_NoComments`, verbatim:
  `TYPE MAP Exact: 1141.MechPlt -> ... [init declared 3:11:1:225:3:4:0:0; honoured (JC-1).
   row=F-UCIZ-D key=a:initSISOEntityType]`  (same for 1142, 1143)
  `TYPE MAP Exact: 1222.MechPlt -> ... [init declared SISOEntityType 3:11:1:71:3:4:0:0, which NO
   table row covers - coverage backstop engaged, using the SIDC-derived row instead (JC-1). ...]`
  `TYPE MAP Proxy: 114.MechCoy~PXY -> ... [init declared 3:11:1:225:5:4:1:0, which NO table row
   covers - coverage backstop engaged ...]`
  `TYPE MAP Proxy: 1.BdeHQ~PXY -> M577A2_Command_Post (1.1.225.3.11.0.0) [init declared
   3:11:1:153:5:4:0:0, which NO table row covers - coverage backstop engaged ...]`
Note `1.BdeHQ`'s declared Country-153 type is Category **5**, confirming survey 11.4 finding 6's
correction of sec 4.3 against the fixture. JC-1 fired on three units and its coverage BACKSTOP
fired on three - both halves of the judgement call are exercised live for the first time.

### P2 - NO GENERIC, NO EMPTY UNIT. PASS on all three.

(a) Objects built from `Ground_Aggregate`: **0** (control: 0). Gate falsifier #2 did not fire.
(b) The composition is real, and it is arithmetically exact. Full census, 125 distinct
    locally-simulated objects (registered floor 30; control 62):
      **5** Mechanized Platoon (USA) IFV (Deprecated)   **20** M2A2_Bradley_IFV
      **15** Infantry Squad (USA) (Deprecated)          **30** US_Army_M16   **30** US_Army_AT4
      1 M577A2_Command_Post, 13 base/Area, 7 base/Route, plus the four standing scenario objects.
    Five mech platoons x (4 Bradleys + 3 squads) = **20 and 15, exactly**; fifteen squads x
    (M16 + AT4) dismount pair = **30 and 30, exactly**. Not one zero-subordinate abstract. Gate
    falsifier #3 did not fire. (The 13 `base/Area` objects are NOT ours - they are the foreign
    init's objectives, see THE VALIDITY FLAG.)
(c) `TYPE MAP AuthoredPending` / `Failed`: **0**. `Init dispatched: 6 units + 0 areas` - all six
    created, none silently skipped. Gate falsifier #7 did not fire.

### P3 - THE MODE TOOK EFFECT. PASS on all three.

(a) The dispatch mode line reads `Type-mapping mode = FidelityTable (ground dispatch from
    ...\data\unit-type-map.json; friendly=USA, opposing=RUS).` - not the control's sentence.
(b) `TYPE MAP` lines: **exactly 6**, one per unit, each naming its row and key (quoted in P1).
(c) The creation lines DIFFER wholesale from the control's. Gate falsifier #6 did not fire; the
    VOID condition did not arise.

### P4 - THE SUBSTITUTION SURFACES AND THE TAG FITS. PASS on all three.

(a) `NameObservation` reports in `reports-captured.log`: **exactly 2** (control: 0), verbatim:
      `<ActorReference>139aa71b-...</ActorReference> <Marking>114.MechCoy~PXY [Proxy: Mechanized
       Platoon (USA) IFV (Deprecated) - mech-inf company: no composed Country-225 mech-inf COMPANY
       exists; the IFV platoon is substituted (echelon Co -> Plt). ...]</Marking>
       <Name>114.MechCoy</Name>`
      `<ActorReference>670cfdb2-...</ActorReference> <Marking>1.BdeHQ~PXY [Proxy:
       M577A2_Command_Post - brigade HQ: no Category-8 unit template exists; a single command-post
       track is substituted (R9 1.BdeHQ).]</Marking> <Name>1.BdeHQ</Name>`
    The DOCTRINAL name survives in `<Name>`, the simulation's label and the reason ride in
    `<Marking>`. The app log's `2 PROXY substitution(s) surfaced to C2SIM (R-SURFACE-PROXY)` line
    is present. R-SURFACE-PROXY works end to end.
(b) `Proxy marking tag NOT appended`: **0**, as the sec-4 arithmetic said (15 and 11 characters
    against the 34-character bound).
(c) Both PROXY units were created under their TAGGED names and NEITHER is truncated anywhere in
    the vendor log. Gate falsifier #4's first half did not fire.

### P5 - TASKING RESOLVES AND THE UNITS MOVE. PASS on all three.

(a) **3/3 TASKCMPLT** (control: 3), sent at app-log lines 98 / 102 / 106. `DROPPING TASK ... WAS
    NOT CREATED`: **0**. Gate falsifier #4's second half did not fire - the tag does not break
    tasking, which is what sec 4 predicted from the `plan.Name` / `e.Name` / `unit.Name` chain.
    The three CreateRoute lines name the TAGGED units (`for 114.MechCoy~PXY`, `for 1.BdeHQ~PXY`)
    and all three routes were created and moved-along.
(b) `Can't find entity route`: **0** EXACT; ZERO 35-character cuts; 3 new-form route lines, 0 old.
(c) All three taskees moved, net displacement from the trace (registered floor 100 m):
      `1.BdeHQ~PXY` **1162.02 m** (control 1161.56) | `114.MechCoy~PXY` **635.59 m** (control
      698.97) | `1222.MechPlt` **687.14 m** (control 1162.60), 690 POS samples each.
    THE ENDPOINTS LEGITIMATELY DIFFER, as sec 6 P5(c) registered in advance: a Mechanized Platoon
    IFV is not a Tank Platoon and a command-post track is not an MBT, so speeds and formations
    differ. The command post, being a single entity in both configurations, matches the control
    to within 0.5 m - which is the right control-within-a-control.

### P6 - THE 2026-07-15 FALSIFIER. PASS as registered, with the limit I registered.

`114.MechCoy~PXY` was built from `Mechanized Platoon (USA) IFV (Deprecated).entity`, a REAL
COMPOSED template with 4 Bradleys and 3 squads. It is not `Ground_Aggregate` and it is not an
empty abstract. Gate falsifier #5 did not fire in the form this run can test.
AN EXTRA DATUM I DID NOT PREDICT: `invalid formation name` is **0** this run, against **1** in the
control (`AR HQ Sec 1: Aggregate state has invalid formation name "column-left"`). The control's
warning attaches to a Tank Headquarters Section, a SUBORDINATE that the Tank Company composition
has and the Mechanized Platoon composition does not - so under FidelityTable the object that
produced the warning is not created at all. That is consistent with the handoff's "the HQ-section
formation-name warning is COSMETIC" and it further weakens the 2026-07-15 reading.
**BUT I AM NOT CLOSING THE 2026-07-15 ITEM, exactly as sec 6 P6 said in advance.** The app makes
no `RequestAvailableFormations` call on any run path, so the literal artefact - a formation LIST
coming back all-lowercase - was not collected and cannot be claimed. What IS now established is
the stronger and more useful thing: THE BACK END'S OWN CREATION LINE agrees with the static rule,
in BOTH modes, for all six units. `docs/VRF_GROUND_TRUTH.md` 0.1.8 item 1 should be re-read
against that, by someone who can issue the formation query.

### P7 - HYGIENE AND MODE. PASS.

(a) FFRTC mode check passes on the handoff's criterion: TEST A **2/2 = 100.0%** in {0.033, 0.034}
    (>= 95%) AND TEST B **R = 0.9984** (>= 0.99), |resid| <= 0.0005 s 10/10 = 100%. THE SAMPLE IS
    THIN AND I SAY SO: 36 stamped / 10 distinct sim stamps, against the control's 404 / 86,
    because the app died at t+1447 s and most of the window has no moving units to stamp. The
    criterion is met but on weak evidence; the slope, 5.6933, is not a registered threshold here.
(b) Runner exit 0; all stages exit 0; no new .dmp (still ...-70668.dmp, 2026-09-02 06:00); the
    fixture still hashes D27E540F8BCC...B0B9 and vrfSim.mtl still stamps 2026-09-01 14:32:14 -
    **NOTHING WRITTEN UNDER C:\MAK**; RTI trio PIDs UNCHANGED (41336 / 224608 / 76620); no
    VR-Forces process or observer remains; docker all Up.
    POST-RUN SWEEP: `tools/ResetVrf 3782` with the RUNBOOK :1208-1215 environment - joined clean,
    0 reflected, resigned cleanly, **exit 0**. LEDGER 3775 -> 3782 (7, runner) -> 3783 (1,
    hand-taken, ledgered BEFORE the join).

### THE VALIDITY FLAG - what the foreign init touched, and what it did not

`[FAIL] VrfC2SimApp exited DURING the observation window with code 0` - the runner's 4a.6 validity
rule, correctly applied. THE CAUSE IS EXTERNAL AND IT IS IN THE APP LOG. At ~t+1447 s the shared
C2SIM server went `INITIALIZED -> UNINITIALIZED -> INITIALIZING`, and a 286,829-byte
initialization arrived carrying **48 units with SystemName `[Not Set]`** and 13 areas named
`LANCASTER__FRIENDLY_OBJECTIVE_LANCASTER`, `READING__...`, `BUFFALO__FRIENDLY_AREA_OF_OPERATIONS_
BUFFALO`, `ATLANTA__...`. None of it is ours - our init is 12,574 bytes and 6 units, and the app
logged `0 of 48 units matched Vrf:ClientId='STP'`, created nothing from it, ran its own cleanup
(9 deletes, the same 9 as the control) and exited 0. The C2SIM docker stack is SHARED
INFRASTRUCTURE (stp-server, c2sim_server4.8.4.9, stp-lt511 all Up); the objective-graphic names
are STP-flavoured, so the most likely source is a co-tenant STP push. **It is a new class of
contamination for this record: our appNo ledger and the VR-Forces federation are protected, the
C2SIM SERVER IS NOT.**

WHAT IT TOUCHED: the last ~6 minutes of a 30-minute window, the 13 stray `base/Area` objects in
the census, and the run's formal validity.
WHAT IT DID NOT TOUCH - and this is why the gate is adjudicable: every creation line, all six
TYPE MAP lines, both NameObservation reports, all three CreateRoute/MoveAlongRoute lines and all
3 TASKCMPLT were written in the first ~120 seconds, roughly **23 minutes before** the foreign init
arrived. The evidence P1-P6 rest on predates the contamination and is timestamped.
**I am reporting the gate as PASSED ON THAT EVIDENCE while stating the run is formally INVALID.**
If the reviewer wants a formally-valid gate, the re-run is cheap (~5 minutes once rule 4 matches)
and needs finding (A) fixed first.

### ADVERSARIAL REVIEW

THE STRONGEST COMPETING HYPOTHESIS: **the six creation lines agree with the table because the
table was BUILT from the same catalog the back end reads, so this is a tautology, not a test.**
That is a real worry and it is the reason the gate exists at all. It fails on the specifics: the
static method is a best-match RULE (`ObjectTypeResolver`) applied OFFLINE to `.entity` files, and
the vendor's live matcher is a different implementation reading the same data. Agreement is
exactly what was in doubt - `VRF_GROUND_TRUTH` 0.1.8 item 1 is a recorded live observation that
DISAGREED. Two further specifics make it a genuine test: the resolver's answer is not the only
possible one at each objectType (sec 3.5's zero-subordinate abstracts and sec 3.6's defective
templates are live alternatives the back end could have picked), and one row - F-UCIZ-H - sends a
`1:1:...` INDIVIDUAL type where the other five send `3:11:...` aggregates, and the back end built
a single entity for it. A tautology would not have those degrees of freedom.

SECOND HYPOTHESIS: **the config did not take effect and I am reading the control.** Refuted three
ways: the mode line names FidelityTable and the table's path, six TYPE MAP lines exist that the
control does not have, and the creation lines are wholesale different (Mechanized Platoon IFV
where the control has Tank Platoon; 20 Bradleys and 30 M16s where the control has 31 Abrams).

THIRD: **the foreign init corrupted the evidence.** Addressed above on timestamps - 23 minutes of
separation, and the app created nothing from it (`0 of 48 units matched`).

UNEXPLAINED AND CARRIED FORWARD: (1) the SOURCE of the foreign init is inferred, not proven - it
is STP-flavoured and the stack is shared, but I did not identify the pusher. (2) `114.MechCoy`
moved 635.59 m against the control's 698.97 m on the same route; plausible for a different vehicle
mix, not measured. Neither bears on the gate.

### CONSEQUENCE - what is now true, and what is not

TRUE: **the static best-match method predicts live creation** for the three rows this fixture
exercises (F-UCIZ-D EXACT, F-UCIZ-E PROXY, F-UCIZ-H PROXY), across BOTH lookup keys, in both
aggregate and individual form; R-SURFACE-PROXY surfaces substitutions in the marking AND the
report stream without breaking tasking; and `FidelityTable` runs end to end.

NOT TRUE YET, and none of it should be inferred from this run: 3 rows are not 123 - the gate's
**run 4** (COA-STP1, 128 units, 28 EXACT / 100 PROXY) is what would test the rest, and gate
**run 3** (OpposingNation=PRC must refuse to start) was not run. `FidelityTable` is NOT the
default and making it one is a user decision on an artifact the user reviews line by line. The
sec 7.4 authoring queue and the second-hand function-ID decodes (survey 11.5 R3) are untouched.

BEFORE ANY FURTHER FidelityTable RUN, FIX FINDING (A): `-StopWhenComplete` rule 4 must resolve a
taskee whose created-object marking carries the `~PXY` tag - match on the tag-stripped marking, or
key rule 4 off the app log's TASKCMPLT taskee uuid rather than the trace's marking text. Until
then every FidelityTable run burns its full `-RunSecs` cap and sits exposed on a shared bus.

## 8. REGISTRATION

Sections 0-6B written and committed BEFORE launch as **1976ec1**. Sec 7 added after the run, from
the run-directory artifacts only.
