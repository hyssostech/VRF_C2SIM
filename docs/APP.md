# The .NET app (src/VrfC2SimApp) - Phase 3/4/5

Status: LIVE-VERIFIED end to end (2026-07-10). The app runs the full C2SIM<->VR-Forces
loop against live VR-Forces: init -> create -> order -> task -> move -> complete ->
TASKCMPLT + position reports -> clean stop; and moves aggregates (opt-in formation). See
docs/RUNBOOK.md sec 7 for the live recipe + the six bugs fixed live, and PORT.md sec 8/10
for phase status + the aggregate finding. What remains is parity polish + the two-layer
semantic-mapping arc (docs/START_HERE.md "immediate next task"). ASCII-only.

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
  (PORT.md sec 10), the formation spike, and the report/TASKCMPLT path. (delay/predecessor
  SEQUENCING landed later - see "Task sequencing" below.)
- LIVE-RUN RISKS: (a) RESOLVED (2026-07-10) - the facade's `TryGetEntityGeodetic` now
  resolves the location from EITHER a DtReflectedEntity (entityStateRep) OR a
  DtReflectedAggregate (aggregateStateRep), both sharing DtBaseEntityStateRepository::
  location(), so a disaggregated aggregate's point 0 resolves and the golden 11.MechBn
  aggregate-move no longer abandons. This is numerically identical to the C++ oracle
  (its UB static_cast and this dynamic_cast read the SAME myStateRep->location()), just
  without the undefined behavior. Builds 0/0 under the HLA MAK set; runtime (live
  aggregate) confirmation still pends the live run. (b) taskee resolution shares
  `_vrfUuidByName` with the create correlation - if VRF truncates markings to 10 chars
  while plan.Name is the full name, names >10 chars would miss (the STP scenario pins
  max-name-length to 10, so golden is safe).

Reports out - DONE + VERIFIED offline (2026-07-10):
- `ReportBuilder` (pure) CONSTRUCTS the SDK schema types (ReportBodyType with a TaskStatus
  or PositionReportContent) and SERIALIZES via C2SIMSDK.FromC2SIMObject - the output analog
  of the schema-typed parse path. It deliberately does NOT reproduce the C++ hand-assembled
  report strings (textIf.cxx): that assembly emits MALFORMED xml for the task-status report
  (stray/duplicated ReportID/ReportingEntity fragments) and EMPTY enum-valued health; the
  schema-typed build is well-formed + schema-valid with the same semantic content.
- `OnVrfTaskCompleted` -> TaskStatus (TASKCMPLT) report: resolve the marking -> taskee
  C2SIM uuid (new `_c2SimUuidByName`) + current task uuid (new `_currentTaskUuidByName`, set
  when OnOrder dispatches) -> build + PushReportMessage. This is executeTask's :2435 emit,
  triggered by the completion callback instead of a busy-wait.
- `OnVrfTextReport` -> PositionReport: parse `POSITION "name" <lat> <lon>` (faithful to the
  C++ strtok parse), resolve name -> C2SIM uuid, build + push.
- Verified offline (`--report-selftest`): builds a TASKCMPLT + a position report, prints
  them (match the golden capture structure), and round-trips each through ToC2SIMObject -
  9/9 field checks pass.
- DEFERRED (documented): EntityHealthStatus enrichment (this slice has no health data from
  the bridge; the golden's empty health was the sec-6 bug, so health is OMITTED not emitted
  empty); aggregate-component report de-dup + multi-content BUNDLING (each report is emitted
  singly - semantically equivalent to the consumer).

Task sequencing - DONE + VERIFIED offline (2026-07-10):
- `TaskSequencer` (pure) replaces executeTask's busy-waits (C2SIMinterface.cpp:2087-2154)
  with async gating: a task with `startAfterTaskUuid` awaits that predecessor's completion,
  then any absolute (SimulationTime) or relative start delay, before dispatching. OnOrder
  now runs each task via `RunTaskAsync` (off-thread await of the gate) and only marshals the
  bridge work onto the tick thread once the gate opens - so nothing blocks the tick loop.
- Completion signal: `OnVrfTaskCompleted` calls `_sequencer.CompleteTask(currentTaskUuid)`,
  releasing any successor (parity: setTaskIsComplete unblocking getTaskIsComplete).
- THE FIX for the C++ infinite busy-wait (PORT.md sec 6): the predecessor wait is bounded by
  `Vrf:TaskPredecessorTimeoutSeconds` (default 600 s ~= the golden completion time); on
  timeout the task dispatches anyway with a warning instead of hanging forever.
- NOT reproduced (C++ quirks, behavior-neutral for the 0-timing golden orders): the delay is
  applied ONCE (the C++ doubled-wait loop is a bug) and UNSCALED (sim time-multiple scaling
  is a later refinement).
- Verified offline (`--sequencer-selftest`, 5/5): waits while a predecessor is pending;
  proceeds on completion; proceeds immediately if the predecessor already completed
  (completer-first race); the start delay elapses; the predecessor timeout fires (no infinite
  wait).
- KNOWN LIMITATION (inherited, not a regression): one current-task-per-unit correlation - the
  bridge's TaskCompleted carries only the marking + task type, not the C2SIM task uuid, so if
  a unit is re-tasked before its prior task completes, the completion is attributed to the
  latest task (same as the C++ getCurrentTaskUuid). Fine for one-task-at-a-time golden orders.

Semantic mapping Layer 1 (verb classifier) - DONE + VERIFIED offline (2026-07-11):
- `VerbMapping.Classify(actionCode) -> VerbPlan` (pure): maps a C2SIM TaskActionCode to a
  `TaskIntent` (Move / Breach / Attack / HoldObjective / Reconnoiter / Escort / Clear /
  MoveInFormation) + the intended VR-Forces composition + Implemented/Recognized flags. The
  table is grounded on the ACTUAL verbs in data/COA-STP1_Order + VRF-Approved (not the
  deprecated plan's assumed EMBARK/FOLLOW). `OnOrder`/ExecuteTaskOnTick CONSULTS it and logs
  the mapped intent, but STILL runs bare movement for every verb (Layer 2 not wired) - ZERO
  behavior/golden-trace change. Verified: `--verb-selftest` 28/28; confirmed the parser emits
  the exact table keys for all 17 COA-STP1 verbs. Plan + status: docs/SEMANTIC_MAPPING.md.
- NEXT (Layer 2, LIVE-GATED): facade Dt*Task methods (Breach, fires, moveIntoFormation) +
  dispatch keyed on TaskIntent. A build proves compile/link only; VRF behavior needs a live run.

TODO - the Phase 4 PARITY PORT (the real content; each maps to a C++ source):
1. `InitParser` refinements: parse `DirectionPhi` if a schema instance carries it;
   handle schema versions beyond 1.0.2 (select by the SDK ProtocolVersion) if servers
   send them; empty-name assignment. None block the golden-trace scenario.
2. Fix the known C++ bugs in the port (PORT.md sec 6): distinct C2SimUuid/VrfUuid
   types (setTarget), completion futures with timeout (not busy-wait), aggregate
   health/heading. The bridge already exposes the seams for these.
3. Parse StatusChanged via a deserialized SystemState (not a substring test).
4. Report enrichment: EntityHealthStatus (needs health from the bridge), aggregate-component
   de-dup, and multi-content bundling - all deferred from the reports-out slice above.
5. DONE (2026-07-10): facade `TryGetEntityGeodetic` now handles aggregates (see the
   Order-translation risk (a) note above).

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
