# START HERE - the C2SIM VR-Forces -> .NET port

If you are picking this up in a fresh session with no prior context, read
this first. It gives you everything to continue with zero loss. ASCII-only.

## What this is

Porting the GMU `c2simVRFinterface` (C++, wraps MAK VR-Forces via
`DtVrfRemoteController`) to .NET on top of the HyssosTech C2SIM .NET SDK.

Three locations are in play:
- THIS repo `VRF_C2SIM` - the .NET port and its HOME. Nested submodule under the
  fork at `Software/Interfaces/VRF_C2SIM`. All port docs + products live here now.
  This is the SINGLE SOURCE OF TRUTH.
- The DEPRECATED C++ interface `c2simVRFinterfacev2.36` (separate repo at
  `C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36`). Ported
  FROM. Kept as the FROZEN parity oracle + the only rig that can regenerate a
  golden trace. Do NOT develop the port there.
- The SDK `OpenC2SIM.github.io` at `Software/Library/CS/C2SIMSDK`, branch
  `dev/sdk-fixes` (NOT merged, NOT pushed). The C2SIM half of the port rides on it.

## Read in this order

1. `docs/PORT.md` - the master reference. Every settled decision WITH its evidence
   (feasibility, architecture, toolchain, environment, golden-trace baseline,
   interface bugs, SDK changes, decisions log). Trust it over any summarized
   recollection. ESP. sec 8 (phase status) + sec 10 (the two-layer semantic map).
2. `docs/APP.md` - the .NET app (`src/VrfC2SimApp`): architecture, data flow, what is
   DONE vs the Phase 4 parity-port TODO. THE CURRENT WORK.
3. `docs/PHASE2_BRIDGE.md` - the C++/CLI bridge in `src/` (DONE): proven build config,
   the native/managed split, the callback mechanism.
4. `docs/RUNBOOK.md` - operational runtime procedure (needed only to run the C++ parity
   rig or, later, the .NET app against live VR-Forces).
5. This file for repo state, build/run commands, and where artifacts live.
6. History/reference as needed: `docs/PHASE1_REWIRE.md`, `docs/TASK_EXPANSION_PLAN.md`.

## Current status (2026-07-11)

- **Latest (2026-07-11)**: two-layer semantic mapping UNDERWAY - Layer-1 verb classifier +
  Unit 3 fires (ATTACK) DONE + live-verified; **Solution A (delete-on-stop) DONE** so runs
  self-clean (no more manual reloads). Details + next steps in the "immediate next task" #4
  below and docs/SEMANTIC_MAPPING.md. HEAD `340d608`.
- **Phase 1** (C++ facade extraction + rewire): DONE + verified in the C++ repo.
- **Migration**: port products (`bridge-spikes/`, `tools/`, `docs/`+`golden-trace/`)
  COPIED into THIS repo. C++ originals retained pending review, then deletion
  (migration "step 1", DEFERRED). `VrfFacade.{h,cpp}` lives in BOTH deliberately:
  the C++ repo keeps its FROZEN parity copy; the port has its own evolving copy in
  `src/VrfFacade/` (they are MEANT to diverge - parity is the golden trace, not source).
- **Phase 2** (managed bridge `VrfBridge`): DONE + verified. `src/VrfFacade/*.cpp`
  compiles NATIVE + `src/VrfBridge/VrfBridge.cpp` compiles `/clr:netcore` -> `VrfBridge.dll`
  under the full HLA1516e MAK set (0/0). FULL facade surface exposed; the 4 inbound
  callbacks -> managed events (via gcroot thunks). RUNTIME-LOAD SMOKE PASSES: the DLL
  + MAK stack load in-process and the native facade constructs/disposes clean (the
  smoke lives in `src/SmokeTest`). See PHASE2_BRIDGE.md.
- **Phase 3** (the .NET app `src/VrfC2SimApp`): host + BackgroundService wiring the
  C2SIM SDK <-> VrfBridge, full lifecycle + a single-threaded tick-command queue. See APP.md.
- **Phase 4** (parity port of the C++ glue): IN PROGRESS.
  - `OnInitialization` DONE + verified: `InitParser` DESERIALIZES the init into the
    SDK's XSD-generated schema types (C2SIM.Schema102 via ToC2SIMObject) - schema-driven,
    not hand-parsed - and `UnitTranslator` faithfully ports extractC2simInit's dispatch +
    all 11 create* factories. Offline-verified: the STP init -> 80 units, 49 creatable,
    4 areas (matches the golden trace's 49 + 4).
  - `OnOrder` (bare movement) DONE + offline-verified (2026-07-10): `OrderParser`
    deserializes the order via C2SIM.Schema102 (same as InitParser) and `OnOrder` ports
    the bare-movement body of executeTask - resolve taskee (PerformingEntity) -> live
    point 0 -> inline route points -> ROE + parity-no-op SetTarget -> CreateRoute +
    deferred MoveAlongRoute. `--parse-order` matches ALL golden orders. Deferred: reports,
    the two-layer vrftask map, the formation spike, delay/predecessor sequencing.
  - Facade aggregate `TryGetEntityGeodetic` reconcile DONE (2026-07-10): resolves point 0
    from an entity OR an aggregate (aggregateStateRep), so the golden 11.MechBn aggregate-
    move no longer abandons. Builds 0/0; live confirmation pends the run.
  - Reports out DONE + offline-verified (2026-07-10): `ReportBuilder` builds C2SIM
    TaskStatus (TASKCMPLT) + PositionReport bodies via the SDK schema types (serialize,
    not the C++ malformed strings). `OnVrfTaskCompleted`/`OnVrfTextReport` correlate +
    PushReportMessage. `--report-selftest` builds + round-trips both (9/9). Deferred:
    health/dedup/bundling, TaskCompletionSource/timeout + delay sequencing.
  - Task sequencing DONE + offline-verified (2026-07-10): `TaskSequencer` replaces the C++
    busy-waits with async gating - a task awaits its predecessor (via OnVrfTaskCompleted)
    + start delay before dispatch, with a timeout (fixes the sec-6 infinite wait).
    `--sequencer-selftest` 5/5.
- **Phase 5** (LIVE run vs VR-Forces): DONE + verified (2026-07-10). The .NET port runs the
  FULL golden-trace pipeline live against VR-Forces HLA + a freshly-redeployed c2sim-server
  4.8.4.9: deploy -> HLA join (RTI 4.6.1) -> late-join (49 units + 4 areas) -> order over
  STOMP -> parse -> taskee resolve -> CreateRoute + MoveAlongRoute (entity 1.BdeHQ AND
  disaggregated aggregate 14.MechBn) -> sim runs -> unit MOVES -> COMPLETES -> TASKCMPLT
  report pushed (+ position reports) -> clean stop (no stale federate). Six bugs found+fixed
  LIVE (all in RUNBOOK sec 7): no late-join (JoinSession); parsers assumed <MessageBody>
  root vs the SDK's bare-body live events; empty status body (GetStatus trigger); missing
  Run() (sim clock never started); disaggregated-aggregate geodetic (static_cast fallback);
  RTI-4.6.1/license/cwd/FOM launch env. New tools: `StopIface` (clean stop), `StompProbe`.
- **Aggregate movement (Phase 4+ enrichment)**: `Vrf:AggregateFormation` (opt-in, default
  "" = golden parity) sets a valid formation ("Wedge") before the move. LIVE-VERIFIED:
  14.MechBn - the canonical frozen aggregate - MOVED its full route. COA-STP1 live run
  (128 units, 42-task order, clientId C2SIM) validated the pipeline AT SCALE (0 abandons,
  sequencer gated 32 temporal deps, 32 aggregates formation-set+moved) BUT only ~3 of 32
  aggregates completed - "some move, most stuck": Wedge is NECESSARY but NOT SUFFICIENT for
  the COA-STP1 aggregate types (deeper subordinate/per-type/vrftask condition; PORT.md sec 10).

Net: the port reproduces the full C2SIM<->VR-Forces loop live and moves aggregates. What
remains is quality/parity polish + the two-layer semantic-mapping arc (see "next task" below).

## Repo state (git log is authoritative)

- THIS repo `VRF_C2SIM` (branch `main`), HEAD `340d608` (29 ahead of origin, UNPUSHED),
  newest first (key commits, the 2026-07-11 semantic-map + Solution A work at top):
  ```
  340d608 docs: ResetVrf turnkey plan + Unit3/Solution-A status for a fresh session
  7ee0fa8 Solution A: delete created VR-Forces objects on stop (no more manual reloads)
  5f34d5b Phase 4+ semantic map: Layer-1 verb classifier + Unit 3 fires (ATTACK)
  480af81 docs: session handoff - Phases 1-5 DONE
  fcba5f4 docs: COA-STP1 live run - pipeline flawless at scale; Wedge necessary-not-sufficient
  80e4b15 Phase 4+ enrichment: opt-in SetAggregateFormation before move (unblock aggregates)
  8ed890e Phase 5: sim Run() + TimeMultiplier - FULL golden-trace pipeline live-verified
  ff4705c Phase 5: port runs live end-to-end - late-join, bare-body parse, GetStatus clean-stop
  03e3a09 Phase 4: OnOrder <- executeTask (bare movement), schema-typed OrderParser
  ```
- The fork `OpenC2SIM.github.io` (`dev/sdk-fixes`) tracks the submodule pointer; needs a
  bump to `340d608` (last recorded bump was `-> 480af81`). Local only, NOT pushed.
- The C++ repo `c2simVRFinterfacev2.36`: HEAD `b87fc9b`. Working tree still holds the
  UNCOMMITTED formation spike (deliberately not committed there).
- The SDK (`dev/sdk-fixes`): `f738edf` (static-state fixes + tests), `3b7cd33` (net10).

`build/` `bin/` `obj/` are gitignored (rebuild them); `docs/golden-trace/*.log` is
force-tracked (parity oracle). `data/` (user-provided post-gold scenarios: COA-STP1,
VRF-All-entities) is UNTRACKED - decide whether to track it.

## Where everything lives (all in THIS repo)

- `src/VrfFacade/` - port-owned native facade (`VrfFacade.{h,cpp}` + verbatim-MAK
  `remoteControlInit.{h,cxx}`). Carries SetAggregateFormation, `FireAtTarget` (Unit 3),
  and `DeleteObject` (Solution A).
- `src/VrfBridge/` - the `/clr:netcore` managed bridge (wraps VrfFacade; the only managed TU).
- `src/VrfC2SimApp/` - the .NET app (net10 host):
  - `VrfC2SimService.cs` - the interface: SDK events <-> bridge commands/events (init,
    order, reports, late-join, Run, clean-stop, aggregate formation, ATTACK dispatch,
    delete-on-stop cleanup).
  - `InitParser.cs` / `OrderParser.cs` - schema-typed init/order deserialization
    (C2SIM.Schema102; root-robust: envelope OR bare live-event body).
  - `UnitTranslator.cs` - the create* dispatch/factories (pure, verified).
  - `VerbMapping.cs` - Layer-1 semantic-map verb->intent classifier (pure).
  - `ReportBuilder.cs` - schema-typed TASKCMPLT + PositionReport builder (serialize).
  - `TaskSequencer.cs` - async predecessor/delay gating (replaces the C++ busy-waits).
  - `InitModels.cs`, `OrderModels.cs`, `VrfSettings.cs`, `appsettings.json`.
  - offline selftests: `TranslatorSelfTest.cs`, `InitParseCheck.cs`, `OrderParseCheck.cs`,
    `ReportSelfTest.cs`, `SequencerSelfTest.cs`, `VerbMappingSelfTest.cs` (see Run below).
- `bridge-spikes/` - the proven C++/CLI spikes + native probe (the bridge's templates).
- `docs/golden-trace/` - the PARITY ORACLE. `data/` (untracked) - post-gold scenarios.
- `tools/` - .NET SDK helpers: `PushInit`, `PushOrder`, `ListenReports`, `SdkVerify`,
  `StopIface` (clean stop = STOP+RESET -> UNINITIALIZED), `StompProbe` (subscribe + log
  every inbound event - the STOMP-receive diagnostic).

## Build

Bridge (HLA1516e) with the VS18 (net10-capable) MSBuild - NOT VS2019 BuildTools
(PORT.md sec 3):
```
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
    src\VrfBridge\VrfBridge.vcxproj /p:Configuration=Release /p:Platform=x64 /m
```
-> `src/VrfBridge/build/Release/VrfBridge.dll` (+ Ijwhost.dll).

App (net10): `dotnet build src/VrfC2SimApp -c Release`.

## Run / verify

OFFLINE checks (no VR-Forces; the app exe needs the MAK bin dirs on PATH only because
it loads the bridge assembly for value types):
- `VrfC2SimApp.exe --translator-selftest` - 18-case parity check of UnitTranslator.
- `VrfC2SimApp.exe --parse-init docs\golden-trace\STP-...Initialization.xml STP`
  - expect 80 units, 49 creatable, 4 areas.
- `VrfC2SimApp.exe --parse-order docs\golden-trace\orders\1_VRF_Move_Order.xml`
  - expect 1 MOVE task T1_1_4_A, taskee 670cfe3a..., ROE ROETight, 2 inline points.
- `VrfC2SimApp.exe --report-selftest` - builds + round-trips a TASKCMPLT + a position
  report via the SDK schema types; expect 9/9 checks pass.
- `VrfC2SimApp.exe --sequencer-selftest` - task-start gating (predecessor / delay /
  timeout); expect 5/5 checks pass.
- `VrfC2SimApp.exe --verb-selftest` - Layer-1 semantic-map verb->intent classification
  (VerbMapping); expect ALL CHECKS PASSED (28+). Build with
  `DOTNET_CLI_USE_MSBUILD_SERVER=false ... --disable-build-servers` (concurrent dotnet
  builds deadlock the shared build server).
PATH for the exe: `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6b\bin`.

NOTE the offline PATH above uses `makRti4.6b` (fine - it only LOADS the bridge DLLs). A
LIVE run MUST use `makRti4.6.1` (match VR-Forces' federation RTI) - see RUNBOOK sec 7.

LIVE run - the FULL recipe is RUNBOOK sec 7 (read it; the golden run + COA-STP1 followed
it exactly). In short: redeploy c2sim-server if gone (from Downloads/Docker.zip); launch env
= RTI 4.6.1 on PATH + `MAKLMGRD_LICENSE_FILE` from Machine scope + cwd=`C:\MAK\vrforces5.0.2\bin64`
+ `--contentRoot=<exe dir>` + a FRESH `Vrf__ApplicationNumber`; PushInit FIRST then start the
app (it late-joins); clean-stop with `tools/StopIface`. Useful env knobs: `Vrf__ClientId`
(STP / C2SIM / VRF per the init's SystemName), `Vrf__TimeMultiplier` (e.g. 20 = fast clock),
`Vrf__AggregateFormation` (e.g. Wedge = move aggregates; "" = golden parity),
`Vrf__TaskPredecessorTimeoutSeconds`. Reload the VR-Forces scenario between heavy runs
(entities accumulate -> creates stop reflecting after ~2-3 runs).

## The immediate next task

Phase 1-5 are DONE (the port runs the full C2SIM<->VR-Forces loop live + moves aggregates).
Remaining work, roughly by priority (details: docs/APP.md TODO, PORT.md sec 6/10):

1. **Aggregate deep-dive** (the live-open question): most COA-STP1 aggregates stay stuck even
   with `Wedge` (necessary-not-sufficient; PORT.md sec 10). Diagnose - compare a MOVING vs a
   STUCK aggregate's VR-Forces Subsystems tab (formation valid for the type? subordinates
   present "2 of 4"? damage?) - then per-unit-type formation and/or the PROPER vrftask
   (`planAndMoveToTask` / `moveIntoFormationTask`), which is the first real slice of #4.
2. **Report-stream parity polish**: EntityHealthStatus (needs the bridge to surface health),
   aggregate-component de-dup, multi-content bundling. Position reports work but are chatty.
3. **Deferred C++-bug fixes** (PORT.md sec 6): distinct C2SimUuid/VrfUuid types (setTarget
   no-op); aggregate health/heading. And the `OnObjectInitialization` STUB (needed only for
   orders that task via a named map-graphic Route, not inline points).
4. **Two-layer semantic mapping** (the big value-add, PORT.md sec 10). Plan +
   re-grounding: **docs/SEMANTIC_MAPPING.md** (the port-grounded plan; supersedes
   TASK_EXPANSION_PLAN.md, whose "first wins" - EMBARK/DEBARK/FOLLOW - are NOT in the real
   orders). Map C2SIM `TaskActionCode` -> the right VR-Forces vrftask (breachTask,
   fireAtTargetTask, moveIntoFormationTask, ...) instead of collapsing every verb to
   moveAlongRoute.
   - **Unit 1 DONE + offline-verified (2026-07-11)**: Layer-1 verb classifier
     `VerbMapping` (VerbMapping.cs) + `--verb-selftest` (28/28). Table grounded on the ACTUAL
     COA-STP1 / VRF-Approved verbs (ATTACK is the most common; only BREACH is native-1:1).
     Confirmed the parser's `TaskActionCode.ToString()` emits the exact table keys (all 17
     COA-STP1 verbs recognized). The executor now CONSULTS the classifier and logs the mapped
     intent + intended composition, but STILL executes bare movement for every verb - ZERO
     behavior/golden-trace change. The port already dissolves the plan's "uuid-resolution
     blocker" (via `_unitByC2SimUuid` -> `_vrfUuidByName`).
   - **Unit 3 (fires/ATTACK) DONE - build + offline + FULL LIVE (2026-07-11), commit 5f34d5b**:
     facade `FireAtTarget` (DtFireAtTargetTask) + bridge + dispatch for ATTACK/DESTRY/FIX/DISRPT/
     PENTRT (resolve affected via TryResolveVrfUuid -> advance -> fire deferred after
     MoveAlongRoute; self-target guard). Live-verified end to end via a SYNTHETIC distinct-target
     order (scratchpad/synthetic_attack_order.xml): "FireAtTarget A -> B after MoveAlongRoute".
     KEY DATA FINDING: COA-STP1 self-targets EVERY attack verb (AffectedEntity==PerformingEntity),
     so the real orders give the fires path no target - a coa-gpt data-quality issue to feed back.
   - **Solution A (delete-on-stop) DONE + LIVE-VERIFIED (2026-07-11), commit 7ee0fa8**: the app
     deletes every object it created on clean-stop (VrfFacade::DeleteObject -> controller->
     deleteObject), so runs SELF-CLEAN - no more manual VR-Forces reloads between runs. Verified:
     164 objects deleted, GUI empty after. Opt-out Vrf:CleanupCreatedOnStop=false.
   - PENDING: **ResetVrf** (hard reset for orphans from crashes/force-kills) - TURNKEY PLAN in
     RUNBOOK sec 8 (Option 1 delete-all-reflected, file-free). Then Layer-2 units: **Unit 2 Breach**,
     **Unit 4 moveIntoFormationTask** (the real fix for the stuck-aggregate finding - serves #1),
     Unit 5+ HoldObjective/Reconnoiter. Each needs a bridge rebuild (VS18) + a live run.
5. **Formal golden-trace message diff** (byte-level parity, not just behavioral).
6. **Housekeeping**: PUSH the branches (port main / fork / SDK are all local-only);
   delete the retained C++ originals (migration step 1); decouple the SDK ProjectReference
   (published nuget); decide whether to track `data/`.

Keep `docs/PORT.md` + `docs/APP.md` current AS you work; after any context compaction
re-read them before deciding anything.
