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

Init parse - DONE + VERIFIED (2026-07-09):
- `InitParser` DESERIALIZES the init into the SDK's XSD-generated schema types
  (C2SIM.Schema102 via ToC2SIMObject) - it follows the C2SIM schema, NOT the shape of
  any one sample - then collects Unit/ForceSide/TacticalArea from the typed graph and
  reads typed properties. Domain behavior mirrors the C++: first-ForceSide-is-blue
  hostility, SystemEntityList -> SystemName, and the order-dependent superior-unit
  coordinate cascade for units missing their own lat/lon.
- Verified offline against the golden-trace STP init (`--parse-init`): 80 units,
  49 creatable (clientId STP), 4 areas - matching the golden trace's 49 creates + 4 areas.
- OnInitialization now runs end-to-end (parse -> translate -> enqueue Create*), pending
  a LIVE run for the final visual/parity confirmation.

Order translation (bare movement) - DONE + VERIFIED offline (2026-07-10):
- `OrderParser` DESERIALIZES the order into the SDK schema types (C2SIM.Schema102 via
  ToC2SIMObject, same approach as InitParser): MessageBody -> DomainMessageBody ->
  OrderBody -> Task[] -> ManeuverWarfareTask. Field mapping mirrors the C++ SAX handler
  (C2SIMxmlHandler.cpp): taskeeUuid=PerformingEntity, taskUuid=UUID, taskName=Name,
  mapGraphicUuid=MapGraphicID, roe=WeaponRuleOfEngagementCode; delays via a faithful
  port of findTotalIsoMs (INCLUDING its 30*60*60-sec/month quirk - parity, not fixed).
- `OnOrder` wired: parse -> resolve each task's PerformingEntity (C2SIM uuid) to the
  unit created at init (new `_unitByC2SimUuid` retained from OnInitialization) -> enqueue
  `ExecuteTaskOnTick` on the tick thread. The executor is the bare-movement body of
  executeTask: point 0 = the taskee's live sim location (`TryGetEntityGeodetic`),
  ground-clamp to 100 for `SIDC[2]=='G'`, append the task's INLINE route points, apply
  ROE (ROEFree->FireAtWill / ROEHold->HoldFire / else->FireWhenFiredUpon) and the parity
  no-op `SetTarget(taskeeC2SimUuid, affectedEntity)`, then CreateRoute + a MoveAlongRoute
  DEFERRED to the route's ObjectCreated (mirrors the C++ wait-for-route-then-move).
- Verified offline (`--parse-order`) against ALL golden-trace orders: the parse matches
  each order (e.g. 1_VRF_Move_Order -> 1 MOVE task T1_1_4_A, taskee 670cfe3a..., ROE
  ROETight, 2 inline points; E_cohq_noaffected -> affectedEntity empty; C_agg_selftarget
  -> taskee==affected). Execution wiring is live-run-pending (see risks below).
- NOT in this slice (deliberate): the two-layer TaskActionCode -> vrftask mapping
  (PORT.md sec 10), the formation spike, the report/TASKCMPLT path, and delay/predecessor
  SEQUENCING (parsed + warned, but executed immediately; the golden orders carry 0 timing).
- LIVE-RUN RISKS to resolve before the parity run: (a) `TryGetEntityGeodetic` uses
  dynamic_cast in the port facade -> returns null for a DISAGGREGATED AGGREGATE, so the
  golden aggregate-move (11.MechBn) would hit ABANDON TASK until the facade is reconciled
  (PORT.md sec 8); (b) taskee resolution shares `_vrfUuidByName` with the create
  correlation - if VRF truncates markings to 10 chars while plan.Name is the full name,
  names >10 chars would miss (the STP scenario pins max-name-length to 10, so golden is safe).

TODO - the Phase 4 PARITY PORT (the real content; each maps to a C++ source):
1. `InitParser` refinements: parse `DirectionPhi` if a schema instance carries it;
   handle schema versions beyond 1.0.2 (select by the SDK ProtocolVersion) if servers
   send them; empty-name assignment. None block the golden-trace scenario.
2. `OnVrfTaskCompleted` / `OnVrfTextReport` <- reportCallback / reportGenerator:
   build C2SIM status + position reports and `PushReportMessage`. (Pairs with converting
   the C++ busy-waits to TaskCompletionSource + timeout, and re-homes task-delay/predecessor
   SEQUENCING here - OnOrder currently executes immediately.)
3. Fix the known C++ bugs in the port (PORT.md sec 6): distinct C2SimUuid/VrfUuid
   types (setTarget), completion futures with timeout (not busy-wait), aggregate
   health/heading. The bridge already exposes the seams for these.
4. Parse StatusChanged via a deserialized SystemState (not a substring test).
5. Reconcile the facade's `TryGetEntityGeodetic` aggregate handling (dynamic_cast ->
   null for DtReflectedAggregate) so a disaggregated aggregate's live location resolves
   for OnOrder point 0 (else the golden aggregate-move abandons; PORT.md sec 8).

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
