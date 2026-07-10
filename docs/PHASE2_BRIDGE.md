# Phase 2 - the managed bridge (VrfBridge)

Status: STARTED 2026-07-09. Slice 1 (outbound path) BUILDS GREEN. ASCII-only.

The .NET port's C++/CLI bridge that wraps the pure-native `vrf::VrfFacade` so a
.NET app can drive VR-Forces in-process. This is the real bridge that Phase 1's
`bridge-spikes/` proved the toolchain for (see PORT.md sec 2-3). Read PORT.md
first for the settled architecture and toolchain decisions.

## Layout

```
src/
  VrfFacade/            port-owned copy of the native facade (evolves independently
    VrfFacade.h         of the C++ interface's frozen parity copy)
    VrfFacade.cpp       includes the SetAggregateFormation fix (PORT.md sec 10)
    remoteControlInit.{h,cxx}   verbatim-MAK init helper the facade constructs
  VrfBridge/
    VrfBridge.vcxproj   /clr:netcore DLL, v143, net10.0, x64
    VrfBridge.cpp       the ONLY managed TU: ref class VrfBridge + POD mirrors
  SmokeTest/
    SmokeTest.csproj    net10 console referencing VrfBridge.dll (+ copies Ijwhost.dll)
    Program.cs          construct + dispose the bridge; the runtime-load proof
```

## Build (verified 2026-07-09, 0 warn / 0 err)

Build with the VS18 (VS "2026"/net10-capable) MSBuild, NOT VS2019 BuildTools
(which lacks the .NET SDK resolver the netcore C++/CLI path needs - PORT.md sec 3):

```
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
    src\VrfBridge\VrfBridge.vcxproj /p:Configuration=Release /p:Platform=x64 /m
```

Output: `src/VrfBridge/build/Release/VrfBridge.dll` (+ `.lib`, `.exp`, `.pdb`),
and MSBuild auto-copies `Ijwhost.dll` beside it (the IJW bootstrap a .NET
consumer needs - PORT.md sec 3).

### The proven compile/link configuration

Extracted from the C++ interface's `c2simVRFHLA1516e.vcxproj` (Release), which is
the config that compiles `VrfFacade.cpp` successfully, adapted for /clr:netcore:

- MAK roots: VrfDir=`C:/MAK/vrforces5.0.2`, VrlDir=`C:/MAK/vrlink5.8`,
  RtiDir=`C:/MAK/makRti4.6b` (the HLA interface uses 4.6b, NOT 4.6.1).
- Includes: `$(VrfDir)/include;$(VrlDir)/include;$(RtiDir)/include/HLA1516E;$(RtiDir)/include`.
- Defines (HLA, not DIS): `DtHLA=1;DtHLA_1516=1;DtHLA_1516_EVOLVED=1;RTI_USES_STD_FSTREAM=1;
  DT_DLL_BUILD;DT_USE_DLL;NO_DFD_SUPPORT;NOMINMAX;BOOST_NO_RVALUE_REFERENCES;
  IS_64BIT;RELEASE_PLUGIN_SUPPORT;...`.
- Libs (facade subset - no xerces / C2SIMClientLib, which are interface-only):
  `vrfcontrol vrlinkNetworkInterfaceHLA1516e vrfMsgTransport vrfExtProtocol
  readerWriter vrfmsgs vrfplan vrftasks vrfutil vlHLA1516e vl vlutil matrix mtl`.
- `/bigobj`; disable warnings `4793;4835;4564;4652;4691;4949;4005;4251;4267;4275;4675;4250`.

### Native vs managed split (the key structural decision)

The project is `<CLRSupport>NetCore</CLRSupport>` (all TUs default to /clr), but the
two native TUs override per-file: `<CompileAsManaged>false</CompileAsManaged>` +
`<ExceptionHandling>Sync</ExceptionHandling>`. So:

- `VrfFacade.cpp`, `remoteControlInit.cxx` -> native (`/EHsc`), no /clr overhead on
  the boost-heavy MAK code; matches how the interface compiles them.
- `VrfBridge.cpp` -> managed (`/clr:netcore /EHa`); the ONLY TU that sees .NET.

This is the standard IJW mixed-mode pattern and it links cleanly - no MAK/boost type
crosses the managed boundary; only POD marshalled via `msclr/marshal_cppstd.h`.

## What slice 1 covers (outbound path)

`ref class VrfBridge` with managed POD mirrors (`VrfProtocol`, `Force`, `Geodetic`,
`EntityTypeSpec`, `StartupConfig`) and:
- lifecycle: ctor/`~`/`!` (native teardown; `~VrfFacade()` calls `Stop()`),
  `Start(StartupConfig^)`, `Stop`, `Tick`, `BackendCount`, `AllBackendsReady`
- sim control: `Run`, `Pause`, `SetTimeMultiplier`
- a representative create + task: `CreateEntity`, `MoveAlongRoute`,
  `SetAggregateFormation`

Deliberately partial - the point of slice 1 was to retire the "does the real facade
compile+link as a /clr:netcore DLL under the HLA set" risk. It does.

## Next (ordered)

1. RUNTIME SMOKE - DONE (2026-07-09). `src/SmokeTest` constructs + disposes VrfBridge
   with the MAK bin dirs on PATH; EXITCODE 0. The MAK static init ran in-process (the
   "RDTSCP Timing Probe" line - the spike-#2 signature, now via the REAL facade),
   `new VrfBridge()` built vrf::VrfFacade over IJW, `BackendCount()` marshalled
   managed->native->managed (returned 0 pre-Start), and dispose ran ~VrfFacade->Stop()
   null-safe. Run: `dotnet build src/SmokeTest -c Release` then run the exe with
   `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6b\bin` on PATH.
   The seam (net10 -> C++/CLI -> native facade -> MAK DLLs, in-process) is PROVEN.
2. CALLBACKS slice: wire the facade's 4 `std::function` members
   (OnObjectCreated/OnTextReport/OnTaskCompleted/OnScenarioClosed) to managed events
   via a native lambda capturing `gcroot<VrfBridge^>`. Phase-1 parity keeps the
   dispatch synchronous (on the VRF tick thread); a later step marshals off-thread.
3. Fill out the remaining facade surface (aggregates, waypoint/route/control area,
   the other setters, MoveToLocation, scripted task/set, TryGetEntityGeodetic).
4. The .NET app: host the C2SIM SDK (event-driven: InitializationReceived /
   ObjectInitializationReceived / OrderReceived / ReportReceived in; PushReportMessage
   / PushCommand out) and drive VrfBridge. This is Phase 3-4 - the two-layer
   C2SIM-semantics -> vrftasks mapping (PORT.md sec 10, TASK_EXPANSION_PLAN.md) lives
   in the app + grows the facade toward intent-level verbs.

## Notes / risks

- Slice 1 proves COMPILE+LINK, not runtime load - see next step 1.
- The DIS path is present in StartupConfig but the port targets HLA1516e first
  (PORT.md sec 8); DIS is untested here.
- The port's `VrfFacade` is now a SEPARATE copy from the C++ interface's. They are
  meant to diverge (C++ parity-frozen; .NET semantically enriched). Do not "sync"
  them - the golden trace is the parity contract, not source identity.
