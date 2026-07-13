# C2SIM VR-Forces interface -> .NET port: master reference

Single source of truth for the decisions and findings behind porting the GMU
c2simVRFinterface (C++) to .NET on top of the HyssosTech C2SIM .NET SDK.
Read this before re-opening any settled question. ASCII-only per repo policy.

Last updated: 2026-07-12 (deep-review corrections + P0 orchestration fixes landed - see
docs/NEXT_SESSION_GUIDANCE.md, which WINS over older text where they conflict: aggregate ROOT
CAUSE = per-unit-type case-inconsistent formation names (sec 10 update); MoveIntoFormation
"ruled out" RETRACTED (confounded experiment); ALL 42 COA-STP1 tasks self-target; P0.1
completion attribution + P0.2 timeout policy + P0.3 completion-gated engage implemented,
all six selftests green. NEXT: guidance sec 4 ladder, E1 per-matched-type formations, LIVE.
Prior state 2026-07-11: Phases 1-5 DONE, Layer-1 + Unit 3 fires live-verified, Solution A +
ResetVrf done - sec 8 phase status, sec 10 semantic map, RUNBOOK sec 7/8.)

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

On `OpenC2SIM.github.io` branch `dev/sdk-fixes`, committed (`f738edf`, `3b7cd33`,
`ae09fd5`), full detail in that repo's `ReleaseNotes.md` (SDK 1.4.0, ClientLib 4.8.3.3):

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
- P4a (2026-07-13, `ae09fd5`, ClientLib 4.8.3.2 -> 4.8.3.3): ONE shared static
  `HttpClient` replaces the per-call create/dispose in `C2SIMClientRestLib.cs`
  (`ServerStatus` + `SendTrans`). Each disposal stranded a TIME_WAIT socket, so
  heavy report pushes exhausted the ephemeral ports -> SocketException 10048 in
  EVERY 2026-07-13 live run (PLAN_DERISK_NOTES sec 1). Accept headers moved
  PER-REQUEST (a shared client's `DefaultRequestHeaders` are not thread-safe to
  mutate); `SocketsHttpHandler`/`PooledConnectionLifetime` (2 min) is
  `#if NET5_0_OR_GREATER`-guarded so the netstandard2.0 leg still compiles (a
  plain shared client there still pools connections). Live discriminator (ZERO
  10048 / "Connection error:" lines) deferred to the Step 5 scale run.

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
    Vrf:TaskPredecessorTimeoutSeconds (default 600 s). Not reproduced (behavior-neutral,
    golden = 0 timing): the C++ doubled-wait bug + time-multiple scaling.
    P0 UPDATE (2026-07-12, NEXT_SESSION_GUIDANCE sec 3 - fixes two live-confirmed
    orchestration defects that corrupted TASKCMPLT reports + confounded every aggregate
    experiment): (P0.1) completions are attributed via a per-unit IN-FLIGHT record
    (`InFlightTracker`) written at dispatch, not a last-write map - the report names the
    RIGHT task and a superseded task's successor gate stays closed; (P0.2) the completion
    window now runs from the predecessor's DISPATCH (two-phase wait), timeout behavior is
    policy-driven (`Vrf:PredecessorTimeoutPolicy` skip|force|whenIdle, default SKIP - the
    old dispatch-anyway burst is opt-in `force`), and skipped/abandoned tasks fail their
    successors FAST; (P0.3) ATTACK/BREACH engages are issued when the move COMPLETES
    (`Vrf:EngageFallbackSeconds` fallback), not same-tick (VRF replaces the running task).
    Behavior-neutral for the golden orders (single task per unit, no temporal deps).
    `--sequencer-selftest` 12 checks green. Remaining limitation: correlation is still
    one-task-per-unit (the bridge callback lacks the task uuid).
  - REMAINING: report enrichment (health/dedup/bundling); THEN the semantic-mapping layer
    (bare movement projector -> real vrftasks) - see sec 10. Then a LIVE run. docs/APP.md.
- **Phase 5 - live run**: DONE (2026-07-10) - the .NET port runs the FULL golden-trace
  pipeline live against VR-Forces HLA + c2sim-server 4.8.4.9: deploy -> HLA join (RTI 4.6.1)
  -> late-join (49 units + 4 areas) -> order over STOMP -> parse -> taskee resolve ->
  CreateRoute + MoveAlongRoute (entity 1.BdeHQ AND disaggregated aggregate 14.MechBn) -> sim
  runs -> unit moves -> completes -> TASKCMPLT report pushed (+ position reports) -> clean
  stop (no stale federate). Bugs found + fixed live: no late-join (JoinSession); parsers
  assumed <MessageBody> root vs the SDK's bare-body live events; empty status body (GetStatus
  trigger); missing Run() (sim clock never started); disaggregated-aggregate geodetic
  (static_cast fallback). Full runtime recipe + findings in docs/RUNBOOK.md sec 7. Remaining:
  a formal message-stream DIFF vs the C++ golden trace, and the report-volume optimization
  (dedup/bundling, docs/APP.md).

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

Target architecture (Phase 4+): two-layer mapping. PORT-GROUNDED PLAN + status:
**docs/SEMANTIC_MAPPING.md** (supersedes TASK_EXPANSION_PLAN.md). Unit 1 DONE
(2026-07-11): Layer-1 verb classifier (VerbMapping.cs, `--verb-selftest`), grounded on the
real COA-STP1 / VRF-Approved verbs; executor consults+logs intent but still runs bare
movement (zero behavior change). Layer 2 (Breach, fires, moveIntoFormationTask) is next and
is LIVE-GATED. Note the port already resolves C2SIM-uuid -> VRF-uuid (the plan's feared
blocker) via `_unitByC2SimUuid` -> `_vrfUuidByName`.
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
The coa-gpt data-quality findings are assembled as an outward-facing memo:
docs/COA_GPT_FEEDBACK.md (2026-07-13; distinct AffectedEntity, timing hygiene, dispersion
nuanced by R8, region validation).

Status (2026-07-10): FIX LIVE-VERIFIED in the .NET port. The disaggregated aggregate
14.MechBn - the canonical "frozen" unit - MOVED. With `Vrf:AggregateFormation=Wedge` the
app set the formation before the move and 14.MechBn tracked its route from lon 16.3907 to
16.4792 (30 position samples, ~the full route to 16.483; visually confirmed in the GUI),
then was killed by an enemy en route (realistic combat, not a defect - which is why it never
reported task-complete). So the sec-5 "aggregates freeze" limitation is a bare-implementation
artifact, SOLVED by setting a valid formation - exactly as hypothesized. The fix is committed
as an OPT-IN enrichment (`Vrf:AggregateFormation`, default "" = golden parity); enabling it is
the deliberate step past the frozen golden behavior.

COA-STP1 live run (2026-07-10, clientId C2SIM, Wedge on, 20x): answers the sec-5 sub-question.
The PIPELINE scaled flawlessly - 128 units + 35 areas created, the 42-task / 76KB order parsed,
32 aggregates formation-set + routed + moved, the TaskSequencer gated the 32 temporal deps live
(30 predecessor-timeouts, the fail-safe), 0 taskee-not-found, 0 ABANDON (the static_cast geodetic
fallback held for ALL aggregates), 0 errors; 9 tasks had no location points (order data). BUT of
the 32 tasked aggregates only ~3 completed a route - "some move, most stuck" (visually confirmed).
So `Wedge` is NECESSARY but NOT SUFFICIENT for the COA-STP1 aggregate TYPES: a global constant
formation moves some (14.MechBn, ~3 here) but most need more. Deeper condition (as sec 5
hypothesized): likely disaggregation/subordinate state ("2 of 4"), a per-unit-type formation
(Wedge may be inapplicable to a scout section / arty battery / company), and/or the PROPER
vrftask - `moveIntoFormationTask + requestAvailableFormationsAdmin` or `planAndMoveToTask` (sec-10
table), not bare moveAlongRoute + setAggregateFormation. i.e. the two-layer mapping is the real
fix for these; our formation enrichment is the first rung. NOT a port/pipeline defect - a VRF
aggregate-maneuver characteristic, now precisely localized.

UPDATE 2026-07-11 - `moveIntoFormationTask` tried, negative run (commit faa4398;
docs/SEMANTIC_MAPPING.md sec 5 Unit 4). Wired `DtMoveIntoFormationTask` (opt-in
`Vrf:MoveIntoFormation`, aggregate move to the route's final point in-formation) and ran COA-STP1
live: it dispatched cleanly ~35 times (0 crash / 0 abandon) but moved NO aggregate (only the
move-along ENTITIES moved; USER visually confirmed no aggregate movement on the GUI).

UPDATE 2026-07-12 - the 2026-07-11 verdict is RETRACTED and the deep-dive REFRAMED
(NEXT_SESSION_GUIDANCE.md sec 2.1/2.2, verified evidence):
- ROOT CAUSE of the stuck aggregates: formation names are defined PER UNIT TYPE in the .entity
  files with INCONSISTENT case - the Ground_Aggregate catch-all (which our Scout/ArmorPlatoon
  types fall back to; no .entity matches their DIS types) lists LOWERCASE "wedge"/"column";
  Tank Company (USA) lists Title-Case; Infantry/Artillery Battalion have EMPTY formation lists.
  Every created aggregate arrives with invalid "column-left" (128 hits of 'Aggregate state has
  invalid formation name' in vrfSim.log). Title-Case "Wedge" could only resolve for the
  company-matched types - which exactly predicts the observed ~3/32 movers.
- The MoveIntoFormation experiment was CONFOUNDED: MAK help (FormationMoveInto.htm) says the task
  IS for DISAGGREGATED units (opposite of the "needs AGGREGATED sets" hypothesis); the targets
  still held the unrepaired invalid "column-left" formation (the path early-returns before the
  Wedge enrichment) in the wrong case for most types; and the pre-P0 orchestration defects
  (retask bursts + misattribution; ~35 dispatches were to only 11 distinct performers, each
  retasked up to 4x) corrupted the run. "35 aggregates dispatched" in older text = ~35 DISPATCHES.
- NEXT: the guidance sec 4 experiment ladder IN ORDER - E1 per-matched-type formation names
  (highest confidence; decision rule = do scout/platoon units move with lowercase names where
  Title-Case failed), E2 re-test MoveIntoFormation after E1, E3 runtime formation discovery
  (DtRequestAvailableFormationsAdmin), E4 fallbacks (subordinate tasking / aggregated-create /
  C2simEx re-key). Record every outcome here as it lands.

E1 RUN (2026-07-12, LIVE; the guidance sec 4 E1 experiment, executed autonomously). Setup:
`Vrf:AggregateFormation=auto` (per-created-type names, AutoFormationFor in VrfC2SimService),
de-confounded synthetic order data/E1_Formation_Order.xml (ONE MOVE per unit, no temporal deps:
2x ArmorCompany + 2x ArmorCoHQ + 2x ArmorPlatoon aggregates + 1 tank ENTITY control - the
COA-STP1 init creates NO Scout/MobileIrregular), COA-STP1 init (128 units + 35 areas
late-joined), fresh federation (ResetVrf), appNo 3315, 20x, P0 fixes in effect. Command path
was flawless: per-type formations set exactly as designed ("Column" x2 CO, "Wedge" x2 HQ,
lowercase "column" x2 PL, none on the entity), 7 routes created, 7 MoveAlongRoute issued,
and NONE of the six set names was rejected (vrfSim.log 'invalid formation' count froze at
210 across all sets - the only invalid lines are the creation-time "column-left" defaults).
RESULTS (position samples t1/t2/t3 via bus captures at ~+9/+13/+17 min; GUI hung, so no
visual channel this run):
- CONTROL PASSED: tank A/4-27 moved and COMPLETED; TASKCMPLT attributed to the correct task
  uuid (P0.1 verified live; second time this session). The run itself was healthy.
- ArmorCompany (Title-Case "Column"): both units DEPARTED at speed but did NOT execute their
  1.1 km routes - by t3 they were 150-170 km away and still going, 0 completions. RUNAWAY,
  not route-march. CONSEQUENCE: the older "Wedge moved ~3/32" reading is now SUSPECT - the
  company-matched "movers" may have been this runaway artifact, not route execution.
- ArmorPlatoon (lowercase "column" - THE discriminator): set ACCEPTED, but units only
  shuffled locally (tens of meters, oscillating around their start). NO route march, no
  completion. Per the guidance decision rule this FALSIFIES formation-name resolution alone
  as the sufficient fix for the Ground_Aggregate-matched types.
- ArmorCoHQ (Title-Case "Wedge"): set accepted, but the aggregates' reported positions were
  ALREADY 60-90 km displaced at creation and never moved. Cause visible in vrfSim.log: their
  "AR HQ Sec" subordinate sections log "Column-Left is an invalid or malformed formation" at
  create - subordinate SCATTER from the unresolvable creation formation.
SURVIVING HYPOTHESES (for the next discriminating test):
(a) COA-STP1 scenario-DATA pathology: dozens of units share IDENTICAL coordinates (e.g.
    34.67998,-116.72480 hosts B/40 + several tank entities + more), and a disaggregated
    aggregate's subordinates spawn stacked when the creation formation is invalid - collision
    /formation-geometry blowup when a valid formation is later set -> scatter/runaway. This
    would be a THIRD coa-gpt data-quality item (after self-targets + timing): DISPERSE unit
    positions.
(b) The formation-REPAIR transition itself (invalid column-left -> valid name post-create)
    misbehaves for these types/scenario, where the golden STP scenario's 14.MechBn (dispersed
    units, 4 subordinates) repaired cleanly with Wedge and genuinely route-marched.
DISCRIMINATING NEXT TEST (E1b): re-run per-type formations on the GOLDEN STP init (dispersed
positions; its MechBn aggregates marched with Wedge) and/or a synthetic dispersed init; plus a
VISUAL check of the CO runaway (set-travel vs subordinate scatter) once the GUI is usable.
E2 (MoveIntoFormation re-test) stays PARKED per its gate ("only after E1 moves units").

RESEARCH SYNTHESIS (2026-07-12, post-E1; the 2-strike rule fired): three parallel sweeps of
the MAK 5.0.2 docs/headers/content produced **docs/UNIT_MOVEMENT_RESEARCH.md** - the
documented unit-movement model with citations, the grounded diagnosis (units born with an
UNINITIALIZED formation on Aggregate.ope platforms; the lead-follow controller needs an
established LEAD subordinate with auto-promote OFF; set-formation SNAPS a disaggregated
unit; stacked spawns + working-formation-from-current-positions explain scatter/runaway),
and the REVISED experiment plan R1-R7 (set formation at CREATE time; ReorganizeAggregate;
member-level telemetry via the reflection machinery; DtRequestAvailableFormationsAdmin;
golden-scenario-first micro-experiments; coa-gpt position-dispersion feedback; subordinate
tasking as last fallback). That doc supersedes the guidance sec 4 ladder from E2 down.

**R5 BREAKTHROUGH (2026-07-12 evening, LIVE - UNIT_MOVEMENT_RESEARCH.md sec 4): THE
STUCK-AGGREGATE PROBLEM IS SOLVED for dispersed scenarios.** The research-derived sequence
- on aggregate creation, QUERY the unit's own formation list (new facade/bridge
`RequestAvailableFormations`, DtRequestAvailableFormationsAdmin round-trip), then on the
reply SET a valid name from that list (lowercase 'column' here; snap) + `ReorganizeAggregate`
(establish the lead subordinate), all BEFORE any tasking - made BOTH tested aggregate types
route-march and COMPLETE on the golden STP init: 3/3 TASKCMPLT (1222.MechPlt ArmorPlatoon-
type - a type that had NEVER moved in any experiment - 114.MechCoy ArmorCompany-type, and
the 1.BdeHQ entity control), with WatchVrf member telemetry showing clean on-axis marches
ending ON the route's final point. KEY GROUND-TRUTH FINDING: the live formation lists are
ALL lowercase - including company-typed units whose .entity file lists Title-Case - so any
STATIC name map is unreliable; query the unit (the app's `Vrf:AggregateFormation=auto` is
now query-driven). The read-back's currentFormation field returns '' even when the set
provably took - trust the list, not current. R5c RAN the same evening on COA-STP1
(stacked coordinates; UNIT_MOVEMENT_RESEARCH.md sec 4): repair applied 113/113 and the
E1 company RUNAWAY is ELIMINATED (units hold instead of flying 150 km), but 0/6
aggregates marched (control-only 1/7, same as E1) - the same-day A/B (dispersed golden
3/3 vs stacked COA-STP1 1/7, identical code) makes COA-STP1's stacked/identical unit
coordinates the evidence-backed blocking data pathology (R6 coa-gpt feedback: DISPERSE
positions). CoHQ creation-time subordinate scatter is a separate open failure mode.
Candidate mitigation R8 (approved by the user same day; record below). Then: E2
MoveIntoFormation re-test with sane preconditions.

**R8 IMPLEMENTED + OFFLINE-VERIFIED (2026-07-12 late night; live A/B pending)** - opt-in
create-time de-stacking (`Vrf:DeStackCreates`, default false; `Vrf:DeStackSpacingMeters`,
default 50): in ProcessInitialization, after planning and before the creates are enqueued,
units sharing identical coordinates (lat/lon rounded to 1e-6 deg) are spread onto
deterministic hex rings - the first unit keeps its exact position, displaced unit n takes
the next slot on ring k (6k slots at radius k*spacing; adjacent ring-1 slots exactly one
spacing apart; the 54-unit COA-STP1 pile fits inside ring 4 = 200 m). Pure helper
`DeStacker.cs` (entities + aggregates alike; only lat/lon change; deterministic in init
order); new `--destack-selftest` (20 checks) + a stacked-groups stat in `--parse-init`;
all offline selftests green (translator 18/18, parse-init 80/49/4, parse-order,
report 9/9, sequencer 12, verb 28/28, destack 20). Parity-breaking by design, hence
opt-in; pairs with `Vrf:AggregateFormation=auto`.
OFFLINE FINDING that REFINES the R5c verdict (full detail UNIT_MOVEMENT_RESEARCH.md
sec 4): the GOLDEN init is ALSO stacked at create - 10 groups, 48/49 creatable units,
max pile 13, produced mostly by InitParser's superior-coordinate inheritance (faithful
C++ parity, so every golden C++ run stacked the same way) - and R5 marched 3/3 OUT of
those piles (1222.MechPlt from a 4-pile, 114.MechCoy from a 13-pile). COA-STP1's
distinguishing pathology is therefore pile SIZE - ONE 54-unit mega-pile (over half its
creatable units incl. tank entities and the E1 control A/4-27) vs golden's max 13 - not
stacking per se. The R8 live A/B (same COA-STP1 scenario, only de-stack toggled) is the
clean discriminator; the cross-scenario golden-vs-COA-STP1 A/B was confounded by
scenario/terrain after all.

**R8 VERIFY RUN (2026-07-12/13 night, LIVE - full record UNIT_MOVEMENT_RESEARCH.md
sec 4b): the STACK HYPOTHESIS IS FALSIFIED as the sufficient blocker.** The exact R5c
probe with only de-stack toggled ON (appNo 3332/3333, auto repair 113/113, de-stack
spread 10 groups incl. the 54-unit mega-pile, E1 probe order, 20x, WatchVrf 20 min):
the entity control completed 4x FASTER (~3.5 min vs ~13 - the pile gridlock was real
and R8 fixes it) and CoHQ CREATION is now clean (no creation scatter), but STILL 0/6
aggregates marched and the failure mode FLIPPED BACK to E1's: companies DRIVE away far
past their 1.1 km routes (31-124 km; 28-49 km/h sustained), CoHQs scatter 76-93 km ON TASKING
(>130 km/h displacement = member warp, not driving), platoons shuffle ~60 m.
REINTERPRETATION: R5c's "runaway eliminated" was mega-pile GRIDLOCK suppressing the
runaway, not the repair fixing it. R8 stays a genuine improvement (recommended ON for
stacked scenarios) but the blocker is elsewhere: the surviving hypothesis is
GEOGRAPHY/terrain content at the COA-STP1 Mojave region (the backend runs whole-earth
"MAK Earth Space (online).mtf" for BOTH regions - vrfSim.log - so it is the streamed
content at the location, not a different terrain file; residual alternatives: the 20x
multiplier, init-content differences). NEXT: R9 region swap - the golden R5 unit set
placed at the Mojave coordinates, same probe (UNIT_MOVEMENT_RESEARCH.md sec 4b).
Operational: report-push ephemeral-port exhaustion recurred (7 errors) - P4 bundling
stays urgent; Solution A deleted 170/170; clean stop; appNos 3330-3333 consumed.

**R9 REGION SWAP (2026-07-13 morning, LIVE - full record UNIT_MOVEMENT_RESEARCH.md
sec 4c): GEOGRAPHY CONFIRMED as the aggregate blocker, WITH the mechanism.** The
golden unit set transplanted to Mojave (data/R9_Mojave_*.xml, ground geometry
preserved) fails 1/3 (entity only; platoon frozen at 8 m, company 410 m wrong-way
then frozen), while the same-day SWEDEN CONTROL (original golden files, same code,
same 20x) completes 3/3 in ~4 min - excluding code drift and the multiplier.
MECHANISM in vrfSim.log: at Mojave the backend logs `moveAlong() - empty route --
not sending move along to subordinate` and creates ZERO member Offset Routes
(Sweden: 45) - the lead-follow controller's leader path plan is EMPTY at that
location on the whole-earth online terrain. NOT an interface defect (identical
command stream both runs). Practical unlock candidates: R10 subordinate fan-out
(entity moves are PROVEN at Mojave), R11 DtPlanAndMoveToTask probe; coa-gpt feedback
item #4 = validate the scenario region before generating COAs there. appNos
3335-3340 consumed (next fresh: 3341).

**R10 FAN-OUT LIVE-VERIFIED + COA-STP1 UNBLOCKED (2026-07-13 late morning - full
records UNIT_MOVEMENT_RESEARCH.md sec 4c).** `Vrf:SubordinateFanOut` (facade/bridge
GetAggregateMembers - published entities + recursive subAggregates rosters - plus app
fan-out with FanOutTracker completion synthesis) tasks member entities directly:
Mojave R9 probe 3/3 with telemetry-verified 1.1-1.3 km member marches (platoon 4/4,
company 18/18, control), and on COA-STP1's OWN units at its OWN location (de-stack +
auto repair + fan-out, E1 probe): **5/7 unit completions - both platoons, both
companies (incl. B/40 from the 54-pile center), control - where R5c scored 0/6**;
the 2 CoHQs each ended 3/4 members (one straggler GndV per unit; fan-out
quorum/timeout is the robustness follow-up). R11 NEGATIVE AND A TRAP:
DtPlanAndMoveToTask at a path-dead region completes VACUOUSLY (TASKCMPLT fired,
units verified still AT SPAWN) - never adopt it as a fix; telemetry, not completion
events, is the movement oracle. Port exhaustion recurred in every run (P4 overdue).
appNos 3341-3350 consumed (next fresh: 3355).
OPERATIONAL FINDINGS from the run: (1) report pushing hit ephemeral-PORT EXHAUSTION ("Only one
usage of each socket address...") under the un-bundled position-report volume -> the P4
report dedup/bundling item is now OPERATIONALLY URGENT for long runs; (2) a deterministic
~2500-char truncation of certain server broadcasts (the A2/A_entity probe orders truncate at
position 2500 on EVERY push - both the app and a second SDK client see it - while a 1.7 KB and
a 9 KB order pass intact; not size-general, unexplained, does not block work).

Prior status (2026-07-09): fix IDENTIFIED + prototyped, NOT yet runtime-verified.
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
