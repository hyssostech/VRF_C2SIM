# The .NET app (src/VrfC2SimApp) - Phase 3/4

Status: SKELETON, compiles green (2026-07-09). The C2SIM<->VR-Forces wiring +
lifecycle are real; the C2SIM XML parse/translate is stubbed (the parity port).
ASCII-only.

## What it is

`VrfC2SimApp` is the .NET port of the C++ c2simVRFinterface's runtime role. One
`BackgroundService` (`VrfC2SimService`) hosts BOTH halves in-process:

- C2SIM half: the HyssosTech C2SIM SDK (`C2SIMSDK`) - STOMP/REST, event-driven.
- VR-Forces half: the native `VrfBridge` (C++/CLI over `vrf::VrfFacade`).

```
Program.cs        Host.CreateApplicationBuilder -> AddHostedService<VrfC2SimService>
VrfC2SimService   the bridge: SDK events <-> VrfBridge commands/events
VrfSettings       "Vrf" config section (protocol, ids, federation, clientId)
appsettings.json  C2SIM endpoints + Vrf settings (127.0.0.1 / CWIX-2024 / STP)
```

References: `VrfBridge.dll` (bare Reference + Ijwhost copy, like SmokeTest) and the
SDK via ProjectReference. NOTE the ProjectReference crosses OUT of the VRF_C2SIM
submodule into the fork's `Software/Library/CS/C2SIMSDK` (branch dev/sdk-fixes,
unpushed) - a known coupling; a later step decouples via a published SDK nuget.

## Data flow (the role it reproduces)

C2SIM in:
- `InitializationReceived`  -> parse -> `bridge.CreateEntity/CreateAggregate/
  CreateRoute/CreateControlArea` (only if init SystemName == `Vrf:ClientId`).
- `ObjectInitializationReceived` -> parse -> routes/graphics after the main init.
- `OrderReceived`          -> parse -> tasking (`MoveAlongRoute`, ...).
- `StatusChangedReceived`  -> on UNINITIALIZED, clean stop (RUNBOOK sec 4).

VRF out:
- `ObjectCreated`  -> correlate name -> VRF uuid (`_vrfUuidByName`).
- `TaskCompleted`  -> C2SIM status report (TASKCMPLT).
- `TextReport`     -> C2SIM position report.
- `ScenarioClosed` -> clean stop.

## Threading (a deliberate improvement over the C++ interface)

The native facade is single-threaded. All bridge COMMAND calls are marshalled onto
the one VRF tick thread via a `ConcurrentQueue<Action>` that the tick loop drains
each iteration; the bridge's own callbacks already fire on that thread. The C++
interface called the controller from the STOMP thread while main ticked (a latent
race). Serializing onto the tick thread yields the SAME command stream in order, so
golden-trace parity holds, and it removes the race.

## What is DONE vs TODO

DONE (this skeleton):
- Host + service + config binding; construct + wire SDK and VrfBridge.
- Lifecycle: Start -> tick thread -> Connect -> idle -> clean stop (disconnect,
  stop tick, Stop + Dispose the bridge).
- All event subscriptions + name->uuid correlation + the tick-action queue.
- Builds green (0 errors).

Init translation core - DONE + VERIFIED (2026-07-09):
- `UnitTranslator` is a faithful, pure port of extractC2simInit's dispatch + all 11
  create* factories (exact DIS 7-tuples, force rules, the two divergent heading
  formulas - RW/MQ1 divide phi, others don't - and the post-create SetAltitude rules).
- `OnInitialization` wired: parse -> guard (systemName/hostility/coords) -> plan ->
  enqueue CreateEntity/CreateAggregate + areas -> CreateControlArea; deferred
  SetAltitude applied on ObjectCreated (async analog of C++ waitForData+SetAltitude).
- Verified by `--translator-selftest` (18 cases, all pass) - no VR-Forces needed.

TODO - the Phase 4 PARITY PORT (the real content; each maps to a C++ source):
1. `InitParser.Parse` <- C2SIMxmlHandler: extract InitUnit/InitArea from the init XML
   (element map documented in InitParser.cs). The last piece before OnInitialization
   runs end-to-end: stable UUID-ordered iteration, superior-unit lat/lon fallback,
   name assignment when empty.
2. `OnOrder` <- executeTask: parse tasks, resolve taskee C2SIM uuid -> VRF uuid,
   enqueue tasking. Bare `MoveAlongRoute` first (parity), THEN the two-layer
   TaskActionCode -> vrftask mapping (PORT.md sec 10 / TASK_EXPANSION_PLAN.md).
3. `OnVrfTaskCompleted` / `OnVrfTextReport` <- reportCallback / reportGenerator:
   build C2SIM status + position reports and `PushReportMessage`.
4. Fix the known C++ bugs in the port (PORT.md sec 6): distinct C2SimUuid/VrfUuid
   types (setTarget), completion futures with timeout (not busy-wait), aggregate
   health/heading. The bridge already exposes the seams for these.
5. Parse StatusChanged via a deserialized SystemState (not a substring test).

## Run (once the parity port lands; needs the live env - RUNBOOK)

The app is a native-x64 host: the MAK bin dirs MUST be on PATH, and VR-Forces (HLA
CWIX-2024) + the C2SIM server must be up.
```
$env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6b\bin;$env:PATH"
dotnet run --project src/VrfC2SimApp -c Release
```
Then push an init/order with tools/PushInit + tools/PushOrder (RUNBOOK sec 3).
Parity check: diff the resulting VR-Forces command stream + reports against
docs/golden-trace/*.
