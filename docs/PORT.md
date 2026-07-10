# C2SIM VR-Forces interface -> .NET port: master reference

Single source of truth for the decisions and findings behind porting the GMU
c2simVRFinterface (C++) to .NET on top of the HyssosTech C2SIM .NET SDK.
Read this before re-opening any settled question. ASCII-only per repo policy.

Last updated: 2026-07 (initial port design + Phase 1 facade extraction + full rewire +
runtime golden-trace verify; only the mechanical lifecycle Start-switch remains).

Two repos are in play:
- This one: `c2simVRFinterfacev2.36` (the C++ interface being ported; now git-tracked).
- The SDK: `OpenC2SIM.github.io` at `Software/Library/CS/C2SIMSDK`, work on branch
  `dev/sdk-fixes` (commits `f738edf` static-state fixes + tests, `3b7cd33` net10).

---

## 1. Feasibility verdict (SETTLED - do not re-litigate)

**A native bridge is mandatory. A pure-.NET interface is impossible.** Evidence,
gathered from the actual MAK install, not docs:

| Question | Evidence | Answer |
|---|---|---|
| Does VR-Link have a C# API? | `C:\MAK\vrlink5.8\C-Sharp\`, `vrLinkSharp.dll` (408 types, ns `makVrl`) | Yes |
| Does that C# API expose VR-Forces control? | reflected `vrLinkSharp.dll`: only `ExerciseConnection`, `EntityPublisher`, `ReflectedEntityList`... grep `Vrf\|RemoteControl\|Task\|Controller` -> 1 false positive (`PowerPlantStatus`) | **No** |
| Does VR-Forces ship any managed assembly? | `find C:\MAK\vrforces5.0.2 -iname "*sharp*"` -> only the copied `vrLinkSharp.dll` | **No** |
| Can .NET P/Invoke `vrfcontrol.dll`? | `dumpbin /exports`: 607 exports, ALL C++-mangled, ZERO `extern "C"` | **No** |
| Is VR-Forces control expressed in HLA (so a pure-HLA client could drive it)? | `MAK-VRFExt-6_evolved.xml` defines exactly one control interaction, `MtlCommand`, whose single parameter `MtlCommandPdu` is an opaque blob; `vrfcontrol.dll` exports `boost::archive::text_oarchive` symbols | **No** |

So `DtVrfRemoteController` (the class the whole interface is built on) is reachable
ONLY from C++. Observation (entity/aggregate state) COULD be pure .NET via RPR-FOM,
but **commands cannot**, and task-completion notifications live on VRF's internal bus
(no FOM interaction), so a pure-HLA client cannot even see "the task you assigned
finished." Verdict: commands go through a native bridge; observation may go over HLA
later as an optimization.

The interface is a fork of MAK's own `examples\remoteControl` (same filenames), with
C2SIM grafted on. `remoteControlInit.cxx/.h` is verbatim MAK.

---

## 2. Architecture decision (SETTLED)

**In-process C++/CLI bridge over `DtVrfRemoteController`. Out-of-process not needed.**

Design: a pure-native `VrfFacade` C++ layer exposes ONLY POD structs + callbacks (no
`Dt*` types) behind a pimpl. A `/clr:netcore` C++/CLI wrapper includes only
`VrfFacade.h`; a `.NET` app drives it. This seam keeps in-process vs out-of-process a
LATE, cheap decision (the POD boundary is what an out-of-process transport would
serialize).

Validated by two spikes (in scratchpad `VrfBridgeSpike/`, `VrfControlSpike/`,
`NativeProbe/`):

- **Spike #1**: net10 process -> `/clr:netcore` dll -> native `matrix.lib`. Geodetic
  round-trip correct to 15 sig figs. Proves the toolchain mechanism.
- **Spike #2 (decisive)**: linked the full boost-heavy DIS set (`vrfcontrol`, `vl`,
  `vlutil`, `vrfmsgs`, ...). Under net10 the DLLs LOAD, run static init (`RDTSCP`
  probe fired), `vrfmsgs` is callable, and a real `DtAutomaticBackendSelector`
  (vrfcontrol) CONSTRUCTS in-process. **The boost-heavy stack works under /clr:netcore.**

Adversarial check that saved the architecture: spike #2 first hit `0xC0000005` on the
object's DESTRUCTOR. Competing hypothesis: "boost can't work under /clr -> pivot to
out-of-process." Falsified by building a PURE-NATIVE exe (no /clr) that crashed
IDENTICALLY on `delete` - so the crash is a context-dependent object's destructor, NOT
a /clr limitation. Construct-only passes. Lesson: always run the native isolation test
before blaming the managed layer.

NOT yet proven (next risks, not blockers): full `DtVrfRemoteController` lifecycle WITH
a live `DtExerciseConn`; callbacks marshalled off the tick thread into .NET; the DLL
fan-out at deploy.

---

## 3. Toolchain facts (SETTLED - hard-won, do not rediscover)

- **Target runtime: net10.0 (LTS to Nov 2028).** net6 is EOL (Nov 2024, the
  `NETSDK1138` warning); net8 LTS expires Nov 2026; net9 STS already EOL. SDK libs are
  `net10.0;netstandard2.0` (netstandard leg keeps older consumers working).
- **C++/CLI target-framework property is plain `<TargetFramework>net10.0</TargetFramework>`.**
  `DotNetCoreTargetFramework` is OBSOLETE and appears nowhere in the VS2022+ Cpp
  targets. `CLRSupport=NetCore` imports `Microsoft.NET.Sdk`, which reads
  `$(TargetFramework)`. (Burned two builds on the wrong property before checking the
  targets - the 2-strike research rule applies.)
- **Build C++/CLI netcore with VS2022+ MSBuild, NOT VS2019 BuildTools' MSBuild** -
  the netcore path needs the .NET SDK resolver, which BuildTools lacks. VS "18"
  Community (MSVC 14.51) MSBuild is what built net10. VS2022 (14.44) MSBuild defaulted
  to SDK 9.0.304 and could not target net10; VS18's MSBuild could.
- **Toolset: v143** works for the bridge (MAK libs are `/MD`; MSVC is ABI-stable
  across v14x, so v143 links libs built with v140/v141/v142 fine). The interface
  itself pins **v142** (VS2019 BuildTools) because that is what it was shipped against.
- **`ijwhost.dll` must sit next to the .NET consumer exe** (the IJW bootstrap for a
  `/clr:netcore` assembly). A `ProjectReference` copies it automatically; a bare
  `<Reference HintPath=...>` does NOT - copy it from
  `dotnet\packs\Microsoft.NETCore.App.Host.win-x64\<ver>\runtimes\win-x64\native\`.
- **Interface itself builds with VS2019 BuildTools v142** (only place v142 lives here):
  `MSBuild.exe c2simVRFHLA1516e.sln /p:Configuration=Release /p:Platform=x64`.

---

## 4. Environment and operational facts (time-sensitive)

- **MAK license renewed**: `C:\MAK\MAKLicenseManager\SALES-TEMP-9-15-26-MAK-node-locked-DEMO_1-dec-2025.lic`,
  **expires 15-sep-2026**. `MAKLMGRD_LICENSE_FILE` (machine scope) was updated to point
  at it. Node-locked; `lmdiag` confirms this is the correct node.
- **Qt**: the interface needs `QT_QPA_PLATFORM_PLUGIN_PATH=C:\MAK\vrforces5.0.2\bin64\platforms`.
  Qt resolves `platforms\qwindows.dll` relative to the EXE dir, not the working dir, so
  the `cd` in the .bat files is not enough. Without it: "no Qt platform plugin" popup
  then `abort()` (exit `0xC0000409`). The three `.bat` files were fixed to set it (via
  `%MAK_VRFDIR%`), preserve `%PATH%` (the HLA ones were clobbering it), and use a
  `%~dp0`-relative exe path.
- **C2SIM server**: docker container `c2sim-server`, publishes 8080 (REST) + 61613
  (STOMP), server version 4.8.4.9. **IPv4 `127.0.0.1:8080` is shadowed by a COA-GPT
  `tileserver.py`** (owns `0.0.0.0:8080`); Docker bound `::`, so from a browser use
  `http://[::1]:8080`. The C++ interface takes an IP string and cannot express `[::1]`,
  so for the interface the tileserver must be stopped (it was, this session) to free
  IPv4 8080.
- **VR-Forces runs HLA 1516e** (`vrfSimHLA1516e.exe`, `--execName CWIX-2024`,
  siteId 1, sessionId 1, appNumber 3001/3101) against MAK `rtiexec`. So the port
  targets **HLA1516e first**, NOT DIS (README concedes DIS is not load-tested). Build
  `c2simVRFHLA1516e.sln`.
- **STP C2SIM system name is "STP"**; a real init file is at
  `C:\Users\PauloBarthelmess\Downloads\STP-TC-small-6-12-24_Initialization.xml`
  (80 units, 3 ForceSides, 8 Routes). STP has a bridge parameter to control entity-name
  length (relevant to the 10-char truncation below).
- Interface command-line params are documented in `README.txt` and `main.cxx`. For a
  clean init+task trace: `... STP 0 0 3 127.0.0.1 0 0 0 1 3201 1 3 0 0 CWIX-2024 0`
  (tracking=3, obs=3, reportInterval=0, **debug=0**). For a report trace use
  reportInterval>0 and tracking=0.

---

## 5. Golden trace (behavior baseline for the port)

Captured live against VR-Forces HLA + the C2SIM container. Logs are in the repo at
`docs/golden-trace/` (relocated from the volatile session scratchpad):
`golden-trace-02_init-and-tasking.log`, `golden-trace-report_generation.log`,
`reports-captured_wire-xml.log`, plus the `STP-TC-small-6-12-24_Initialization.xml`
init and `orders/` used to produce them. These are the parity oracle: the ported code
must reproduce them. See `START_HERE.md` for the full artifact map and reproduce steps.

Covered and working end-to-end:
- RTI join, backend discovery, `allBackendsReady()`.
- Late-join `QUERYINIT` -> SAX parse -> 80 units, 49 created + 4 control areas, with
  DIS types, forces, lat/lon, returned VRF UUIDs.
- `getUnitGeodeticFromSim` -> `DtEntityStateRepository::location()` (3 live samples).
- Order -> `executeTask` -> `createRoute` -> `moveAlongRoute` -> task-complete ->
  `TASKCMPLT` status report. Confirmed for BOTH an entity-level unit (`1.BdeHQ`) and an
  aggregate (`11.MechBn`): both moved and reported completion.
- Report generation: `reportGenerator` polled 36 entities, emitted 72 messages, 288
  `PositionReportContent`, deterministic 2 contents/message bundling.

Experiment matrix run (settles the "unit destroyed" question): the one unit that froze
(`14.MechBn`) was NOT killed by the interface. Its twin `11.MechBn` (same factory, type,
route) completed fine. `14.MechBn` is a disaggregated aggregate down to 2 of 4
subordinates with a failing formation ("invalid formation name column-left"); its
Subsystems tab shows only "Disaggregated Set Maneuver: Enabled" (no damage). Movement-
blocked, not destroyed. This does NOT affect the port (the facade forwards
createSubordinates/aggregateState; VRF's formation behavior is downstream).

COA-STP1 adversarial test (2026-07-09; the order a user reported "broke" an older
C++ build). Findings, all orthogonal to the Phase 1 rewire:
- Init loaded cleanly through the facade: 128 units (grep shows 113 disaggregated
  aggregates + 15 entities) + 35 tactical areas.
- Parse divergence is NOT the rewire. The older binary `bin64/c2simVRFHLA1516ed.exe`
  (2024-06-06, pre-git) dies parsing the 42-task order; the current build parses all
  42. But the parser (`C2SIMxmlHandler`) is UNCHANGED across the entire git history
  (only the baseline commit touches it), so the parse difference is binary-age /
  older-source, not the facade work.
- Movement stall is VRF formation behavior, same as `14.MechBn` above. Only the lone
  TANK entity (T23) moved + completed; every aggregate-tasked unit froze. Frozen
  units never complete routes, so the 31 temporally-dependent tasks (`startAfterTaskUuid`,
  0 ms delay) busy-wait forever (sec 6 thread-leak) on predecessors that never finish
  -> total stall (process alive, ~55 threads parked in DtSleep, ~0 CPU).
- Time values are a minor factor: durations (1h20m-3h20m) are IGNORED (`moveAlongRoute`
  passes no speed); only task T13 has a real 12,000,000 ms (3h20m) SimulationTime
  start delay.
- Net: the rewire reproduces interface behavior faithfully; neither failure is a port
  regression. CONFIRMED via VR-Forces GUI (2026-07-09): stuck unit `1-6/2/1_AD` shows
  Type: Ground Unit, the Move-Along Route task ASSIGNED on the unit ("T15_AOA_SE...",
  so the facade tasking + route-by-name resolution worked), and Subsystems =
  "Disaggregated Set Maneuver: Enabled" with no damage - identical to 14.MechBn. The
  interface does every step correctly; VRF does not move the disaggregated aggregate.
  This is the product gap COA-STP1 surfaces (aggregate-heavy orders don't execute
  movement in VRF), NOT a rewire or port defect. Open sub-question: why ALL COA-STP1
  aggregates froze here when golden `11.MechBn` moved - likely a shared formation/
  subordinate condition across these unit types; deferred, does not affect parity.

---

## 6. Bugs in the C++ interface (found via the golden trace; do NOT reproduce in the port)

- **`setTarget` passes a C2SIM UUID where VRF expects a VRF UUID**
  (`C2SIMinterface.cpp:2301`). Every other call uses `taskUnit->vrfUuid`; this one uses
  `taskeeUuid` (the C2SIM uuid). VR-Forces has no such object -> silent no-op. Target
  assignment has NEVER worked. `DtUUID` accepts any string so C++ can't catch it ->
  in the port, `C2SimUuid` and `VrfUuid` must be DISTINCT types.
- **`debugFlag=1` discards the server's initialization** and reads a hardcoded path
  `C:/Users/c2sim/Desktop/.../test-initialize.xml` (`C2SIMinterface.cpp:1563`, also
  `:1690`, `:1797`). On any other machine that read fails -> empty XML -> "DID NOT
  RECEIVE EXPECTED LATE JOINER". Debug output is unusable as shipped.
- **`executeTask` busy-waits forever** on a task that never completes (spins in
  `DtSleep(.1)` on `getTaskRouteIsComplete`), one detached thread per task -> thread
  leak. Port must use a completion future WITH a timeout.
- **Entity names truncate to exactly 10 chars** (DIS/RPR marking-text limit;
  `C2SIMxmlHandler.cpp:2365`). Routes and tactical graphics are NOT limited
  (`T1_1_4_A ROUTE` = 14 chars round-tripped). On a truncation COLLISION,
  `addUnit()` silently rewrites the last char (`modDigit` 0-9), which then breaks
  position-report name matching. Set STP's max-name-length to 10 to make names
  deliberate.
- **Aggregate position reports carry empty health** - `getUnitGeodeticFromSim` casts
  `reflectedObjectFor(aggregateUuid)` to `DtReflectedEntity*` which is null for
  aggregates, so 120 of 144 report contents went out with empty
  `OperationalStatusCode`/`Strength` (only 24 entity-level units had real values).
- **Aggregate heading is dropped**: all four aggregate factories compute
  `DtReal heading` from `directionPhi` then pass the literal `0` in the heading slot of
  a 12-positional-arg `createAggregate`. The facade exposes heading as a named param.
- `~C2SIMinterface` calls `stompLib->disconnect()` with no null check; `"/n"` typo
  (should be `\n`) at `C2SIMinterface.cpp:414`.

---

## 7. SDK-side changes (other repo; captured here for cross-reference)

On `OpenC2SIM.github.io` branch `dev/sdk-fixes`, committed (`f738edf`, `3b7cd33`),
full detail in that repo's `ReleaseNotes.md` (SDK 1.4.0, ClientLib 4.8.3.2):

- Fixed: `Dispose()`-before-`Connect()` NRE; `Error` event never fired; STOMP pump
  now exits cleanly on cancel; `Disconnect()` set `IsConnected=true` (now false).
- Added: `ObjectInitializationReceived` (was dropped - needed for Routes-after-init);
  `OrderReceived` (misspelled `OderReceived` now `[Obsolete]`, still raised).
- Concurrency: `C2SIMClientSTOMPLib._queue` was `static` (one queue shared by all
  instances -> two SDK instances could not both connect: 2nd `Connect()` failed with
  "Expected 'CONNECTED' but received MESSAGE"). Now per-instance.
  `C2SIMClientRESTLib._protocol/_protocolVersion` were static while instances are
  per-request -> a concurrent BML push could strip a C2SIM instance's header. Now
  instance. Removed the `GetHashCode()`-keyed static XDoc cache.
- Added a real `C2SIMSDK.Tests` project (39 tests, fake STOMP broker + fake REST
  server on real sockets; live-server tests gated on env vars). Bumped to net10.

**NEAR-MISS to NOT repeat**: the missing STOMP `selector` was NOT a bug. Commit
`04c0131` (Feb 2024) removed it deliberately - "won't work with recent ActiveMQ".
Do not re-add it.

---

## 8. Phased plan and current status

- **Phase 0 - unblock**: DONE. License renewed, Qt fixed, .bat files fixed, golden
  trace captured.
- **Phase 1 - VrfFacade extraction**: DONE except the mechanical lifecycle Start-switch
  follow-up. The interface runs 100% through VrfFacade and reproduces golden-trace-02
  live (verified). Facade contract
  (`VrfFacade.h`) + implementation (`VrfFacade.cpp`) written and COMPILE-VERIFIED
  standalone for both DtDIS and DtHLA; added to the HLA vcxproj. Commits `2d0b1c1`,
  `01431ea`, `7806ffd`. Rewire progress (plan in `PHASE1_REWIRE.md`):
  - [DONE] Step 1 - `StartAdopting` transition scaffold. `VrfFacade` gains
    `StartAdopting(void*,void*,void*)` (adopts an externally-owned controller/exConn/
    uuidMgr, registers NO inbound callbacks, `owns=false` so `Stop()` frees nothing).
    `Impl::controller` retyped to the base `makVrf::DtVrlinkVrfRemoteController*` -
    only the derived `init(exConn,...)` overload needs the subclass, now called via a
    local in `Start()`. `main.cxx` builds the facade and `StartAdopting`s the
    controller it still owns; `textIf->facade()`/`setFacade()` added (mirrors
    `controller()`). ZERO behavior change: the facade is constructed and adopts, but
    no call site uses it, it is never ticked, and it registers no callbacks. HLA sln
    builds clean (0 warn / 0 err). NOTE: the DIS vcxproj now fails to LINK (it shares
    `main.cxx`, which references `VrfFacade`, but does not compile `VrfFacade.cpp`);
    DIS is not in the HLA sln and is an untested fallback, so this is deferred - mirror
    `7806ffd` for the DIS vcxproj to restore it if/when DIS is needed.
  - [DONE] Batch 2a - entity creation. The 9 `controller->createEntity` calls in the
    create* factories (createRW/MQ1/RQ7/Boat/Truck/Tank/Civilian) now call
    `facade->CreateEntity`; a new `C2SIMinterface::onVrfObjectCreated` static
    reproduces `vrfObjectCreatedCb`'s C2SIM path (correlate-by-name via
    `c2simXmlHandler`; the two golden-trace cout lines reproduced verbatim; the DtInfo
    log lines and the textIf-demo Tank_Plt/Plt_Sub branch omitted as golden-trace-
    neutral) and is wired to `facade->OnObjectCreated` in main. PARITY: Geodetic built
    from the same unit lat/lon/elevationAgl the dispatcher fed `convertCoordinates`
    (facade `toGeocentric` matches, and the dispatcher STILL calls convertCoordinates
    so its negative-elevation cerr warning is preserved); heading passed as
    `heading * degreesToRadians` so the facade's internal `/kDegRadFactor` delivers the
    exact (mixed-unit, quirk-preserving) createEntity value. HLA builds 0/0.
  - [DONE] Batch 2b - aggregate factories (createScoutUnit/ArmorPlatoon/ArmorCompany/
    ArmorCoHQ/MobileIrregular) now call `facade->CreateAggregate` with headingDeg=0.0
    (reproduces the dropped-heading quirk; 0/kDegRadFactor == 0) and
    Disaggregated+createSubordinates=true. createScoutUnit gained a (pure) getUnitByName
    to build the Geodetic; createMobileIrregular's lookup was relocated above the create.
    Force literals preserved (Scout=Friendly, MobIrregular=Opposing, Armor=forceType).
    HLA builds 0/0.
  - [DONE] Batch 2c - waypoint / route / control area. makeWaypoint ->
    `facade->CreateWaypoint`; route (executeTask) and control area (extractC2simInit)
    build a parallel `std::vector<vrf::Geodetic>` alongside the geocentric DtList
    (from the same per-point lat/lon/elev strings) and pass it to
    `facade->CreateRoute` / `CreateControlArea`; the facade's toGeocentric matches
    convertCoordinates, so points are sub-ULP identical. The DtList is kept where its
    count()/free are still needed. FACADE CHANGE: `CreateControlArea` gained a trailing
    `uuid=""` param so the interface still assigns the area's C2SIM uuid (the plan's
    3-arg form would have dropped it to nullUUID - a behavior change I could not prove
    unobservable). All 17 creation call sites (9 entity + 5 aggregate + waypoint + route
    + control area) now go through VrfFacade; ZERO `controller()->create*` remain in
    C2SIMinterface.cpp. HLA builds 0/0. Runtime golden-trace diff deferred to the
    end-of-rewire verify (env not set up per-batch; batches are compile-green + reviewed).
  - [DONE] Batch 3: attribute setters. 6 setAltitude (entity factories) ->
    `facade->SetAltitude` (facade hardcodes the TRUE clamp flag, as today); magicMove
    setLocation -> `facade->SetLocation` (Geodetic lat/lon, alt 0.0); executeTask's 3
    setRulesOfEngagement -> `facade->SetRulesOfEngagement` with Roe enum
    (Free/Hold/else -> FireAtWill/HoldFire/FireWhenFiredUpon, same strings); setTarget ->
    `facade->SetTarget` PRESERVING the known uuid bug (passes C2SIM taskeeUuid, a VRF
    no-op - PORT.md sec 6, not fixed). HLA builds 0/0.
  - [DONE] Batch 4: tasking. moveAlongRoute -> `facade->MoveAlongRoute(vrfUuid,
    routeName)`; setPointAltitudeAgl's sendSetDataMsg -> `facade->SendScriptedSet
    ("set_point_agl", {Number("altitudeAgl", v)})`; runEvacuate's sendTaskMsg ->
    `facade->RunScriptedTask("evacuate_civilians", {Object(pickup/dropoff/return)})`
    (variable order preserved). moveToLocation -> `facade->MoveToLocation(vrfUuid,
    routeGeodetic.back())`: NOTE the original single-point branch reads uninitialized
    nextX/Y/Z (only reachable when numberOfPoints==0; latent UB, not in the golden
    trace), so this uses the last captured Geodetic (point 0) - a defined analog.
    HLA builds 0/0.
  - [DONE] Batch 5 (partial - clean lifecycle calls): run x2 -> `facade->Run`,
    pause -> `facade->Pause`, setTimeMultiplier -> `facade->SetTimeMultiplier`,
    setScenarioStart's setExerciseStartTime -> `facade->SetExerciseStartTime`
    (facade builds the same DtScenario("DATE-TIME")), allBackendsReady x2 ->
    `facade->AllBackendsReady`. HLA builds 0/0.
    DEFERRED to the final flip (need the facade's controller/uuidMgr, and/or are
    parity-sensitive):
    * getUnitGeodeticFromSim (line 133 uuidMgr + line 313 reflectedObjectFor) is
      KEPT AS-IS, NOT replaced by `facade->TryGetEntityGeodetic`. PARITY FINDING
      (code contradicts PHASE1_REWIRE): the original uses `static_cast` and DOES
      return a location for the disaggregated aggregate 11.MechBn (PORT.md sec 5
      shows it moved, so executeTask did not abandon). The facade's
      TryGetEntityGeodetic uses `dynamic_cast` (the sec-6 "fix"), which would
      return null for an aggregate reflected as DtReflectedAggregate -> executeTask
      "ABANDONING TASK" -> 11.MechBn would NOT move -> golden-trace break. It also
      drops the "UNABLE TO GET REFLECTED OBJECT"/"...ENTITY STATE REPOSITORY" couts.
      So Phase 1 keeps the static_cast getUnitGeodeticFromSim; the dynamic_cast fix
      lands in Phase 4. (Update: the final flip kept `StartAdopting`, so line 133's
      `textIf->controller()` still returns main's controller and needs NO repoint - no
      GetUuidManager accessor was needed. Repointing waits for the lifecycle Start
      follow-up, which routes it via GetController.)
    * The backends() DtList (count print + the debug-only iteration) stays on the
      controller (the facade only exposes BackendCount()); handled in the flip.
    * Dead/commented controller() users remain: the never-called free
      createAggregate (sendVrfObjectCreateMsg), unused generateRequestId locals in
      createArmorCompany/CoHQ, and the commented-out embark/disembark. The flip
      keeps textIf->controller() returning the facade-owned controller so these
      compile unchanged (Phase 4 removes them).
  - [DONE] Final flip - callback relocation (the parity-critical part). reportCallback's
    task-completed and text-report interpretation is relocated verbatim (extracted
    programmatically + diff-confirmed byte-identical) into
    `DtTextInterface::onTaskCompleted` / `onTextReport`, and scenarioCloseCallback into
    `onScenarioClosed`, driven by the facade's `ev` fields (markingText / taskCompleted /
    text). textIf's own report + scenario-close registration is removed; the facade
    registers its trampolines via `RegisterInboundCallbacks()` (called from main), so it
    fires OnTaskCompleted / OnTextReport / OnScenarioClosed -> the handlers. Object-
    created already routes via the facade (2a). All THREE inbound callbacks now flow
    through VrfFacade; no double-firing (textIf keeps only the backend callbacks). The
    reportCallback/scenarioCloseCallback DEFINITIONS remain (dead, unregistered; Phase 4
    removes them). Flip-A `e628c14` (handlers dead-wired) + Flip-B. HLA builds 0/0.
    DEVIATION FROM THE PLAN (parity-driven): kept `StartAdopting` + main creating the
    controller, rather than switching to `facade.Start` + deleting the subclass +
    `facade.Tick`. Reason: main's controller setup carries golden-trace couts (VR-Forces
    arguments / SessionId set to / HostInetAddr set to / BACKEND COUNT); the full Start
    switch would have to reproduce them (parity risk) yet has ZERO effect on the trace
    (same controller, same behavior). So the callback relocation was done + verified in
    isolation; the pure-lifecycle Start switch (facade owns the controller, delete
    `MyDtVrlinkVrfRemoteController`, `facade.Tick`, wire via the staged-but-unused
    GetController/GetExConn) is a clean follow-up. getUnitGeodeticFromSim stays on
    `textIf->controller()` (main's controller); line 133 unchanged, static_cast preserved.
  - [DONE] Lifecycle Start switch. main now builds a `vrf::StartupConfig` from the parsed
    args and calls `facade.Start(cfg)`: the facade OWNS + creates the exConn + controller
    (its own MyDtVrlinkVrfRemoteController - the duplicate subclass is DELETED from
    main.cxx, ending the ODR overlap), runs init / setSessionId / setHostInetAddr, and
    registers the inbound callbacks. main borrows the controller/exConn back via
    GetController/GetExConn (for textIf + the couts), ticks via `facade.Tick()`, and
    shuts down with `delete textIf; delete facade` (facade.Stop frees controller/exConn,
    owns=true). StartAdopting + RegisterInboundCallbacks removed; handlers wired before
    Start. Builds 0/0. VERIFIED: `facade.Start()` connects and its startup couts match
    golden-trace-02 EXACTLY (VR-Forces arguments / SessionId set to:1 / HostInetAddr set
    to:127.0.0.1 / BACKEND COUNT:0). The full create+task flow after the switch could NOT
    be re-run end-to-end because the C2SIM STOMP broker (ActiveMQ) degraded under the many
    verify runs - later interface starts hang at "connecting STOMP stream" (a broker-state
    issue, NOT the switch: the STOMP/readStomp path is untouched by it, and iface2 fully
    verified creates+tasking through the facade before the switch; facade.Tick() runs the
    same exConn->drainInput()/controller->tick() as the old inline loop). Needs an env
    reset (restart the c2sim-server container; reload the VR-Forces scenario) for the next
    clean run.
  - [DONE] RUNTIME VERIFY (live vs golden-trace-02). Ran the rebuilt interface against
    VR-Forces HLA / CWIX-2024 / MAK rtiexec / the C2SIM container (STP init, 80 units).
    Confirmed EVERYTHING runs through VrfFacade and reproduces the golden trace:
    49 unit creates + 4 tactical areas (EXACT match: `controller created object` x49,
    `CREATING TACTICAL AREA` x4), the object-created handler (onVrfObjectCreated) output,
    order -> facade->CreateRoute -> facade->MoveAlongRoute, and the task-complete flow -
    the relocated `onTaskCompleted` handler fired with byte-identical output
    ("TaskComplete received.../Task complete message.../TASK COMPLETE:/SENT TASK STATUS
    REPORT") and the status report echoed on the C2SIM bus. (Only 2 orders pushed vs the
    full experiment matrix, so route/report counts are a proportional subset; units move
    slowly - a completion took ~9 min.)
  - [BUG FOUND+FIXED during verify] `8ee4b0b`: VrfFacade CreateRoute/CreateControlArea
    freed their DtList with `delete (DtVector*)it; it = it->next()` - deleted the list
    NODE (not its data) AND used `it` after freeing it. Use-after-free crashed the
    readStomp thread on the FIRST control-area/route creation (interface hung: printed
    the 1_3 callback, never reached 1_1). Replaced with `freeVectorList()` mirroring the
    interface's proven `deleteDtList` (save next; delete `list.remove(item)`). Pre-existing
    facade bug (never runtime-tested until the rewire routed area/route creation through
    the facade), not a parity regression.
- **Phase 2 - managed bridge**: DONE + verified (2026-07-09). The real bridge lives in
  `src/` (VrfFacade native + VrfBridge /clr:netcore DLL). VrfFacade.cpp compiles native +
  VrfBridge.cpp compiles /clr:netcore + they LINK into VrfBridge.dll under the full
  HLA1516e MAK set (0/0, VS18 MSBuild). FULL facade surface exposed; the 4 inbound
  callbacks -> managed events via gcroot thunks. RUNTIME-LOAD SMOKE PASSES (the DLL + MAK
  stack load in-process; native facade constructs/disposes clean). Ijwhost.dll auto-copies.
  Full detail: docs/PHASE2_BRIDGE.md.
- **Phase 3 - C2SIM half on the .NET SDK**: DONE (skeleton, 2026-07-09). `src/VrfC2SimApp`
  (a Host + BackgroundService) constructs + wires the C2SIM SDK and VrfBridge with full
  lifecycle (Start -> single-threaded tick loop -> Connect -> clean stop) and all event
  subscriptions + name->uuid correlation. See docs/APP.md.
- **Phase 4 - glue** (extractC2simInit, executeTask, reportCallback ported to .NET;
  busy-waits -> TaskCompletionSource + timeout): IN PROGRESS.
  - `OnInitialization` DONE + verified (2026-07-10): `InitParser` deserializes the init
    into the SDK's XSD-generated schema types (C2SIM.Schema102 via ToC2SIMObject) -
    schema-driven, not hand-parsed - and `UnitTranslator` faithfully ports
    extractC2simInit's dispatch + all 11 create* factories. Offline-verified against the
    STP init: 80 units, 49 creatable, 4 areas (matches the golden trace's 49 + 4).
  - `OnOrder` (bare movement) DONE + offline-verified (2026-07-10): `OrderParser`
    deserializes the order via the SDK schema types (C2SIM.Schema102, same as InitParser;
    MessageBody->DomainMessageBody->OrderBody->Task->ManeuverWarfareTask) with the C++
    field mapping (taskee=PerformingEntity, roe=WeaponRuleOfEngagementCode, delays via a
    faithful findTotalIsoMs incl. its 30h/month quirk). `OnOrder` resolves the taskee
    (new `_unitByC2SimUuid` from init) and enqueues the bare-movement body of executeTask
    on the tick thread: live point 0 (TryGetEntityGeodetic), ground-clamp, inline route
    points, ROE, the parity-no-op SetTarget, CreateRoute + a MoveAlongRoute deferred to
    the route's ObjectCreated (mirrors the C++ wait-for-route). Verified offline with
    `--parse-order` against ALL golden orders. NOT included (deferred): the two-layer
    TaskActionCode->vrftask map (sec 10), the formation spike, reports, and delay/
    predecessor SEQUENCING (parsed+warned, executed immediately - golden orders are 0-timing).
  - Facade aggregate-geodetic reconcile DONE (2026-07-10): `TryGetEntityGeodetic` now
    resolves the location from a DtReflectedEntity (entityStateRep) OR a
    DtReflectedAggregate (aggregateStateRep) - both share DtBaseEntityStateRepository::
    location(), so a disaggregated aggregate's point 0 resolves and 11.MechBn no longer
    abandons. Numerically identical to the oracle's UB static_cast (same myStateRep->
    location()) but type-safe. Builds 0/0; live-aggregate confirmation pends the live run.
  - Reports out DONE + offline-verified (2026-07-10): `ReportBuilder` CONSTRUCTS the SDK
    schema types (ReportBodyType with TaskStatus / PositionReportContent) and SERIALIZES
    via FromC2SIMObject (the output analog of the schema-typed parse), NOT the C++
    hand-assembled strings - which emit MALFORMED task-status xml + empty enum health.
    `OnVrfTaskCompleted` -> TASKCMPLT report (correlate marking -> taskee uuid + current
    task uuid via new `_c2SimUuidByName`/`_currentTaskUuidByName`); `OnVrfTextReport` parses
    `POSITION "name" lat lon` -> PositionReport. `--report-selftest` builds + round-trips
    both (9/9 checks). Deferred: health enrichment (no bridge health this slice; golden's
    empty health was the sec-6 bug, so OMITTED not emitted-empty), aggregate-component
    de-dup + bundling, and TaskCompletionSource/timeout + delay/predecessor sequencing.
  - Task sequencing DONE + offline-verified (2026-07-10): `TaskSequencer` replaces
    executeTask's busy-waits with async gating - a task awaits its startAfterTaskUuid
    predecessor (completed off `OnVrfTaskCompleted`), then its start delay, before the
    bridge work is marshalled onto the tick thread (OnOrder -> RunTaskAsync). THE FIX for
    the sec-6 infinite busy-wait: the predecessor wait is bounded by
    Vrf:TaskPredecessorTimeoutSeconds (default 600 s), dispatching anyway on timeout. Not
    reproduced (behavior-neutral, golden = 0 timing): the C++ doubled-wait bug + time-
    multiple scaling. `--sequencer-selftest` 5/5. Inherited limitation: one current-task-
    per-unit correlation (the bridge callback lacks the task uuid).
  - REMAINING: report enrichment (health/dedup/bundling); THEN the semantic-mapping layer
    (bare movement projector -> real vrftasks) - see sec 10. Then a LIVE run. docs/APP.md.
- **Phase 5 - parity**: diff .NET vs C++ message streams against the golden trace (LIVE run).

---

## 9. Decisions log (terse; each is SETTLED with the evidence above)

1. Native bridge mandatory; pure .NET impossible. (S1)
2. Pure-HLA rejected: VRF control is an opaque `MtlCommand` blob, not typed FOM. (S1)
3. In-process C++/CLI chosen over out-of-process; POD facade keeps the pivot cheap. (S2)
4. Facade exposes POD only, no `Dt*`; MAK types behind a pimpl. (S2)
5. Facade is PARITY-FIRST: known bugs (setTarget uuid class, aggregate heading, etc.)
   are EXPOSED via the API (named params, distinct-uuid intent) but the Phase-1 rewire
   reproduces current behavior so the golden-trace diff stays clean. Fixes land later.
6. Target net10 LTS everywhere; SDK libs keep `netstandard2.0`. (S3)
7. Toolchain: `<TargetFramework>` (not `DotNetCoreTargetFramework`); build /clr:netcore
   with VS2022+ MSBuild; v143 for the bridge, v142 for the interface; ship `ijwhost.dll`. (S3)
8. Port targets HLA1516e first (that is what VR-Forces runs); DIS is a fallback. (S4)
9. Creates are async (fire-and-forget); `OnObjectCreated` is the completion signal;
   callers keep their `waitForData` spin. (Phase 1 design)
10. Interface is a BARE movement projector: it uses ~4 of 263 `vrftasks` and does NOT
    dispatch on C2SIM `TaskActionCode` - every maneuver collapses to createRoute+
    moveAlongRoute. Most observed limitations (incl. the aggregate movement-block) are
    bare-implementation artifacts, not fundamental VRF constraints. Phase 4+ target is a
    two-layer mapping (C2SIM semantics -> VRF task composition), NOT a port of the bare
    model. See sec 10. (2026-07-09, COA-STP1 exploration)

---

## 10. Semantic gap: bare movement projector vs the vrftasks model (Phase 4+ scope)

Finding (2026-07-09, from the COA-STP1 exploration). The C++ interface provides BARE
task support; it must NOT be enshrined as the port's target design.

Evidence:
- MAK ships 263 headers in `include/vrftasks` (~60 semantic `*Task` types: fireAtTargetTask,
  breachTask, clearTask, moveIntoFormationTask, planAndMoveToTask, patrolRouteTask,
  provideIndirectFireTask, holdUntilTask, ...). The interface uses ~4: moveToTask,
  moveAlongRoute, waitDurationTask, plus two narrow Lua scripted tasks
  (evacuate_civilians, set_point_agl via RunScriptedTask/SendScriptedSet).
- `executeTask` does NOT branch on `TaskActionCode`. ATTACK/BLOCK/BREACH/SECURE/SEIZE/
  PENTRT all collapse to createRoute + moveAlongRoute + setTarget(no-op). The action verb
  is parsed by C2SIMxmlHandler and discarded.
- Net: a movement projector - every maneuver becomes "walk these waypoints," dropping
  fires, breaching, securing, targeting, formations, timing.

Concern separation (what to fix by using vrftasks vs what is real VRF behavior):

| Problem observed | Bare-impl artifact (fixable; don't overfit) | Fundamental VRF (must handle) | Proper vrftask |
|---|---|---|---|
| Aggregates freeze on move | Mostly - no formation ever set | Partly - disagg sets move via formations | moveIntoFormationTask + requestAvailableFormationsAdmin, or planAndMoveToTask |
| ATTACK/BREACH/SECURE all == move | Yes - no dispatch | No | fireAtTargetTask / breachTask / clearTask |
| Literal waypoints, no pathfinding | Yes | No | planAndMoveToTask |
| setTarget no-op | Yes (uuid bug, sec 6) | No | fix uuid + targetEntityTask |
| Task Duration ignored | Yes | No | setSpeedRequest / task timing |
| Thread-per-task busy-wait stall | Yes (control flow, sec 6) | No | event-driven task state |
| T13 12,000,000 ms delay | No - order data | No | coa-gpt timing hygiene |

Reclassification of the sec-5 aggregate movement-block: MOSTLY a bare-implementation
artifact, not "aggregates can't move." Disaggregated sets move via formations; the
interface never sets one, so VRF falls back to an unresolvable default (golden 11.MechBn
resolved by luck; COA-STP1 unit types get "column-left" = invalid). Proper fix keeps
disaggregation (subordinate fidelity) and sets a valid formation.

Target architecture (Phase 4+): two-layer mapping.
- Layer 1 (C2SIM semantics): parse ManeuverWarfareTask + TaskActionCode + params/graphics.
- Layer 2 (VRF task composition): select/compose the right Dt*Task per action code.
The facade surface should evolve from `MoveAlongRoute(uuid, route)` toward intent-level
verbs (Attack, Breach, SecureArea, MoveInFormation, ...) that each compose the appropriate
vrftask. Parity-first stays for Phases 1-3 (reproduce bare behavior, keep the golden-trace
diff clean); semantic enrichment is a deliberate Phase 4+ step.

coa-gpt guidance: keep emitting rich semantics (TaskActionCodes, temporal associations,
durations; add formations + explicit targets). Do NOT dumb coa-gpt down to the bare
interface - extend the interface up. One real coa-gpt-side fix: timing hygiene (the
12,000,000 ms / 3.3-hr delay; set SimulationRealtimeMultiple so scenarios are watchable).

Status (2026-07-09): fix IDENTIFIED + prototyped, NOT yet runtime-verified.
- Fix: `controller->setAggregateFormation(leaderUuid, formationName)` BEFORE moveAlongRoute
  (no-op on non-aggregates). moveAlongRoute carries no formation; the aggregate holds a
  formation state whose model-set default is unresolvable ("column-left") -> frozen set
  maneuver. Valid formation names (from VRF data literals): Title-Case "Wedge"/"Column"/
  "Line"/"Vee"/"Echelon". The createAggregate overload the facade uses has NO initialFormation
  arg (its nullString is the label), so formation must be set post-create / pre-move.
- Prototype (uncommitted spike, parity-breaking): `VrfFacade::SetAggregateFormation` +
  an executeTask `SetAggregateFormation(vrfUuid,"Wedge"); DtSleep(.5)` before MoveAlongRoute.
  Builds clean (0 err/0 warn).
- NOT runtime-verified. Runtime env is flaky: (a) ~one clean RTI join per VR-Forces scenario
  reload - force-killing a joined federate dirties the federation, so the next join hangs at
  1 thread; (b) the interface QUERYINITs only at STARTUP, so the init must reach the C2SIM
  server BEFORE the interface starts. Pushing it after connect (ResetToInitializing) resets
  the interface into a stuck waiting state (1 thread, no units). Correct order: reload VRF ->
  PushInit -> start interface -> PushOrder.
- NOTE: this fix belongs in the .NET port (VRF_C2SIM), NOT the deprecated C++ interface; the
  C++ change here is only a spike to validate the mechanism.
