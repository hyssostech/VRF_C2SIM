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

## 7. OUTCOME

(written after the run, from the run-directory artifacts only)

## 8. REGISTRATION

Sections 0-6B written and committed BEFORE launch. Commit hash stamped in sec 7.
