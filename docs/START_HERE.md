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

1. `docs/PORT.md` - the master reference. Every settled decision WITH its
   evidence (feasibility, architecture, toolchain, environment, golden-trace
   baseline, interface bugs, SDK changes, decisions log). Trust it over any
   summarized recollection. ESP. sec 8 (phase status) + sec 10 (the two-layer
   semantic-mapping target).
2. `docs/PHASE2_BRIDGE.md` - the CURRENT actionable work: the C++/CLI bridge in
   `src/`, the proven build config, and the ordered next steps.
3. `docs/RUNBOOK.md` - operational runtime procedure (only needed to run the C++
   parity rig or, later, the .NET app against live VR-Forces).
4. This file for repo state, build commands, and where artifacts live.
5. History/reference as needed: `docs/PHASE1_REWIRE.md` (the completed C++ facade
   rewire), `docs/TASK_EXPANSION_PLAN.md` (verb -> vrftask blueprint).

## Current status (2026-07-09)

- **Phase 1** (C++ facade extraction + full rewire): DONE and verified in the C++
  repo - the interface runs 100% through `VrfFacade` and reproduced golden-trace-02.
- **Migration to the port**: the port-bound products (`bridge-spikes/`, `tools/`,
  `docs/` incl. `golden-trace/`) were COPIED from the C++ repo into THIS repo
  (submodule commit `7c6c5a6`). The C++-repo originals are retained pending review,
  then deletion (migration "step 1", deferred). `VrfFacade.{h,cpp}` intentionally
  stays in BOTH: the C++ repo keeps its FROZEN parity copy; the port has its own
  evolving copy in `src/VrfFacade/` (they are MEANT to diverge - parity is the
  golden trace, not source identity).
- **Phase 2** (the managed bridge): STARTED; slice 1 BUILDS GREEN (submodule
  `b24c380`). `src/VrfFacade/VrfFacade.cpp` compiles NATIVE + `src/VrfBridge/
  VrfBridge.cpp` compiles `/clr:netcore` + they LINK into `VrfBridge.dll` under the
  full HLA1516e MAK set (0 warn/0 err, VS18 MSBuild). Ijwhost.dll auto-copies. This
  retires the central Phase 2 risk against the REAL facade. Slice 1 is the outbound
  path only (lifecycle + CreateEntity + MoveAlongRoute + SetAggregateFormation).
  See PHASE2_BRIDGE.md.

The aggregate-movement fix (`setAggregateFormation(uuid,"Wedge")` before move;
PORT.md sec 10) now lives in the port's `src/VrfFacade/` - its correct home. The
C++ live visual proof was never landed and is explicitly NOT worth more time
(RUNBOOK sec 6); the fix's real validation is in the .NET port.

## Repo state (git log is authoritative)

- THIS repo `VRF_C2SIM` (branch `main`), newest first:
  ```
  b24c380 Phase 2 slice 1: VrfBridge C++/CLI bridge builds green under the HLA MAK set
  7c6c5a6 Port products: import bridge-spikes, tools, docs+golden-trace from C++ repo
  0462a79 Initial commit
  ```
- The fork `OpenC2SIM.github.io` (branch `dev/sdk-fixes`) tracks the submodule
  pointer: `0b902b0` (-> b24c380), `95887aa` (-> 7c6c5a6), `6185848` (add submodule).
  Local only, not pushed.
- The C++ repo `c2simVRFinterfacev2.36`: HEAD `b87fc9b` (Phase 1 complete). Working
  tree still holds the UNCOMMITTED C++ formation spike (VrfFacade.*, C2SIMinterface.cpp)
  - deliberately not committed there; the fix moved to the port.
- The SDK (`dev/sdk-fixes`): commits `f738edf` (static-state fixes + tests), `3b7cd33`
  (net10). Not merged/pushed.

`build/` `bin/` `obj/` are gitignored (rebuild them); `docs/golden-trace/*.log` is
force-tracked (parity oracle).

## Where everything lives (all in THIS repo)

- `src/VrfFacade/` - port-owned native facade (`VrfFacade.{h,cpp}` + the verbatim-MAK
  `remoteControlInit.{h,cxx}` it constructs). Evolves toward the two-layer model.
- `src/VrfBridge/` - the `/clr:netcore` managed bridge (`VrfBridge.vcxproj` +
  `VrfBridge.cpp`). Wraps VrfFacade; the only managed TU.
- `bridge-spikes/` - the proven C++/CLI spikes + native probe. The templates the
  real bridge was built from. `VrfControlSpike` (boost-heavy) is the closest model.
- `docs/golden-trace/` - the PARITY ORACLE the ported code must reproduce
  (golden-trace-02 init+tasking, report generation, wire XML, the STP init, orders/).
- `tools/` - .NET SDK helpers: `PushInit`, `PushOrder`, `ListenReports`, `SdkVerify`
  (net10; reference the SDK csproj by absolute path).

## Build

The bridge (HLA1516e, the target protocol) with the VS18 (net10-capable) MSBuild -
NOT VS2019 BuildTools, which lacks the .NET SDK resolver the netcore C++/CLI path
needs (PORT.md sec 3):
```
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
    src\VrfBridge\VrfBridge.vcxproj /p:Configuration=Release /p:Platform=x64 /m
```
-> `src/VrfBridge/build/Release/VrfBridge.dll` (+ Ijwhost.dll). Full config +
rationale in `docs/PHASE2_BRIDGE.md`.

The C++ parity rig (only to regenerate a golden trace) builds in the C++ repo with
VS2019 BuildTools v142 - see that repo and RUNBOOK.md.

## The immediate next task

Continue Phase 2 per `docs/PHASE2_BRIDGE.md` "Next":
1. Runtime-load smoke (construct + dispose the bridge in-process with MAK on PATH) -
   the honest proof of the seam; build-green does not prove load.
2. Callbacks slice (facade `std::function` -> managed events via `gcroot`).
3. Fill out the remaining facade surface.
4. The .NET app on the C2SIM SDK - where the two-layer C2SIM-semantics -> vrftasks
   mapping (PORT.md sec 10, TASK_EXPANSION_PLAN.md) lives.

Keep `docs/PORT.md` + `docs/PHASE2_BRIDGE.md` current AS you work; after any context
compaction re-read them before deciding anything.
