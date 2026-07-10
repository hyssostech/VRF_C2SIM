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

## Current status (2026-07-10)

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
  - REMAINING: `OnOrder` <- executeTask; reports <- reportCallback; then a LIVE run.

The aggregate-movement fix (`SetAggregateFormation(uuid,"Wedge")` before move; PORT.md
sec 10) lives in the port's `src/VrfFacade/`. The C++ live proof was never landed and
is NOT worth more time (RUNBOOK sec 6); its real validation is the .NET port.

## Repo state (git log is authoritative)

- THIS repo `VRF_C2SIM` (branch `main`), newest first (key commits):
  ```
  378c71c Phase 4: InitParser via SDK schema types - OnInitialization end-to-end
  3a9c28b Phase 4: init translation core (UnitTranslator, verified)
  834c62b Phase 2: complete the VrfBridge facade surface
  9b8061d Phase 2 slice 2: inbound callbacks -> managed events
  a39e25f Phase 2: runtime-load smoke PASSES
  b24c380 Phase 2 slice 1: VrfBridge builds green under the HLA MAK set
  7c6c5a6 Port products: import bridge-spikes, tools, docs+golden-trace
  0462a79 Initial commit
  ```
- The fork `OpenC2SIM.github.io` (`dev/sdk-fixes`) tracks the submodule pointer; latest
  `baaec08` (-> 378c71c). Local only, NOT pushed.
- The C++ repo `c2simVRFinterfacev2.36`: HEAD `b87fc9b`. Working tree still holds the
  UNCOMMITTED formation spike (deliberately not committed there).
- The SDK (`dev/sdk-fixes`): `f738edf` (static-state fixes + tests), `3b7cd33` (net10).

`build/` `bin/` `obj/` are gitignored (rebuild them); `docs/golden-trace/*.log` is
force-tracked (parity oracle).

## Where everything lives (all in THIS repo)

- `src/VrfFacade/` - port-owned native facade (`VrfFacade.{h,cpp}` + verbatim-MAK
  `remoteControlInit.{h,cxx}`). Carries the SetAggregateFormation fix.
- `src/VrfBridge/` - the `/clr:netcore` managed bridge (wraps VrfFacade; the only managed TU).
- `src/VrfC2SimApp/` - the .NET app (net10 host):
  - `VrfC2SimService.cs` - the interface: SDK events <-> bridge commands/events.
  - `InitParser.cs` - schema-typed init deserialization (C2SIM.Schema102).
  - `UnitTranslator.cs` - the create* dispatch/factories (pure, verified).
  - `InitModels.cs`, `VrfSettings.cs`, `appsettings.json`.
  - `TranslatorSelfTest.cs` / `InitParseCheck.cs` - offline checks (see Run below).
- `bridge-spikes/` - the proven C++/CLI spikes + native probe (the bridge's templates).
- `docs/golden-trace/` - the PARITY ORACLE.
- `tools/` - .NET SDK helpers: `PushInit`, `PushOrder`, `ListenReports`, `SdkVerify`.

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
PATH for the exe: `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6b\bin`.

LIVE run (RUNBOOK first): needs VR-Forces (HLA CWIX-2024) + the C2SIM container up, MAK
on PATH. `dotnet run --project src/VrfC2SimApp -c Release`; push init/order with
`tools/PushInit` + `tools/PushOrder`; diff against `docs/golden-trace/*`.

## The immediate next task

Continue the Phase 4 parity port in `VrfC2SimService` (docs/APP.md "What is DONE vs TODO"):
1. `OnOrder` <- executeTask: deserialize the order (SDK schema types, like InitParser),
   resolve the taskee C2SIM-uuid -> VRF-uuid via `_vrfUuidByName`, enqueue the tasking
   (bare `CreateRoute` + `MoveAlongRoute` FIRST for parity; the two-layer TaskActionCode
   -> vrftask mapping is the Phase 4+ enrichment - PORT.md sec 10 / TASK_EXPANSION_PLAN.md).
2. `OnVrfTaskCompleted` / `OnVrfTextReport` <- reportCallback: build C2SIM status +
   position reports and `PushReportMessage`.
3. LIVE run + golden-trace parity diff (needs the runtime env - RUNBOOK).

Keep `docs/PORT.md` + `docs/APP.md` current AS you work; after any context compaction
re-read them before deciding anything.
