# Phase 1 rewire plan: put the interface on VrfFacade

Actionable working doc for the remaining Phase 1 work. See `PORT.md` for the
why. ASCII-only.

State entering the rewire:
- `VrfFacade.h` / `VrfFacade.cpp` exist, compile-verified standalone (DIS + HLA),
  and are in `c2simVRFHLA1516e.vcxproj`. Nothing calls the facade yet.
- Baseline commit `191933a`; facade commits `2d0b1c1`, `01431ea`, `7806ffd`.
- Every commit builds; use `git bisect`/`reset` freely.

---

## The coupling problem and the chosen approach

The facade owns the controller (one network connection), so main.cxx, textIf, and
C2SIMinterface must flip together - a naive rewire will not build until it is mostly
done, which prevents checkpointing.

**Chosen approach: an "adopt" transition scaffold** so the rewire is incrementally
buildable and testable.

1. Add a transition-only entry to the facade that ADOPTS an existing controller/exConn
   instead of creating its own:
   `bool StartAdopting(void* controllerPtr, void* exConnPtr, void* uuidMgrPtr);`
   (void* keeps the header POD-clean; cast inside VrfFacade.cpp; add an `owns` flag so
   `Stop()` does NOT delete an adopted controller). Do NOT register the facade's inbound
   callbacks in adopt mode (they would double-fire alongside textIf's).
2. main.cxx still creates controller+exConn+textIf as today, then also creates the
   facade and `StartAdopting(...)`. Give C2SIMinterface access to the facade
   (simplest: `textIf->facade()` returns it, mirroring the existing
   `textIf->controller()` access pattern to minimize churn).
3. Move COMMAND call sites in small batches (create/attr/task/state). Each batch builds
   and runs green because both `textIf->controller()->X` and `facade->X` hit the same
   controller. Diff-check each batch against golden-trace-02.
4. FINAL atomic flip: relocate the three inbound callbacks from textIf to the facade,
   remove the direct controller creation, switch `StartAdopting` -> `Start`, delete the
   `MyDtVrlinkVrfRemoteController` subclass from main.cxx (it now lives in VrfFacade.cpp)
   and textIf's callback bodies.

Discipline: RE-READ each file's current lines before editing; do not trust context
memory of signatures. Commit per batch.

---

## Call-site catalogue (live paths only; dead code excluded)

49 `controller()->` sites in C2SIMinterface.cpp; the live/portable ones:

### Object creation (in the create* factories, ~C2SIMinterface.cpp 691-1109, 1127-1270)
- `createEntity(cb, usr, DtEntityType, DtVector vec, DtForceType, DtReal heading, DtString name)`
  x9 -> `facade->CreateEntity(EntityTypeSpec, Geodetic, Force, headingDeg, name)`.
  Factories: createRW (708/713), createMQ1 (751), createRQ7 (791), createBoat (829/834),
  createTruck (868), createTank (902), createCivilian (1100). Each builds `vec` via
  `convertCoordinates` (geocentric) - the facade takes Geodetic and converts internally,
  so pass lat/lon/alt in degrees/m and drop the local DtVector/DtEntityType.
- `createAggregate(...)` x5 (977/1003/1038/1072/1254): createScoutUnit, createArmorPlatoon,
  createArmorCompany, createArmorCoHQ, createMobileIrregular ->
  `facade->CreateAggregate(EntityTypeSpec, Geodetic, Force, headingDeg, name,
  AggregateState::Disaggregated, true)`. NOTE parity: keep Disaggregated+true AND note
  the heading bug (they pass 0 today; the facade divides headingDeg by the deg/rad
  factor - to reproduce EXACTLY, pass headingDeg=0 as today, i.e. do not "fix" it in
  Phase 1).
- `createWaypoint(cb, usr, DtVector, DtString)` (1138) -> `CreateWaypoint(Geodetic, name)`.
- `createRoute(cb, usr, DtList points, DtString)` (2320) ->
  `CreateRoute(vector<Geodetic>, name)`.
- `createControlArea(cb, usr, DtList perimeter, DtString name, DtString label,
  DtSimSendToAll, DtUUID)` (1475) -> `CreateControlArea(vector<Geodetic>, name, label)`.
- Each factory: after the create call, keep `waitForData(unitData)`, then
  `setAltitude(DtUUID(vrfUuid), alt, TRUE)` -> `facade->SetAltitude(vrfUuid, alt)`.

### Attributes
- `setAltitude(DtUUID, double, TRUE)` x6 (720/761/801/841/875/909) ->
  `SetAltitude(uuid, meters)`.
- `setLocation(DtUUID, DtVector)` (687, magicMove) -> `SetLocation(uuid, Geodetic)`.
- `setTarget(DtUUID, DtUUID)` (2301) -> `SetTarget(uuid, targetUuid)`. PARITY: pass the
  SAME (buggy) taskee/affected uuids the code passes today.
- `setRulesOfEngagement(DtUUID, const char*, DtSimSendToAll)` (2291/2294/2297) ->
  `SetRulesOfEngagement(uuid, Roe)`.

### Tasking
- `moveToLocation(DtUUID, DtVector64, DtSimSendToAll)` (2308) ->
  `MoveToLocation(uuid, Geodetic)`.
- `moveAlongRoute(DtUUID, DtUUID, DtSimSendToAll)` (2334) ->
  `MoveAlongRoute(uuid, routeUuid)`.
- `sendTaskMsg(DtUUID, DtScriptedTaskTask*)` (1225, runEvacuate) ->
  `RunScriptedTask(uuid, "evacuate_civilians", {Object("pickupPoint",..),
  Object("dropoffPoint",..), Object("returnPoint",..)})`.
- `sendSetDataMsg(DtUUID, DtScriptedTaskSet*, DtSimSendToAll)` (1123, setPointAltitudeAgl)
  -> `SendScriptedSet(uuid, "set_point_agl", {Number("altitudeAgl", v)})`.

### Scenario / lifecycle / state
- `setExerciseStartTime(DtScenario*)` (616) -> `SetExerciseStartTime(y,mo,d,h,mi,s)`
  (facade builds the DtScenario).
- `run()` (1747/1845) -> `Run()`; `pause()` (1752) -> `Pause()`;
  `setTimeMultiplier(int)` (1772) -> `SetTimeMultiplier(int)`.
- `backends().count()` / `allBackendsReady()` (1575/1578/1580) -> `BackendCount()` /
  `AllBackendsReady()`.
- `uuidNetworkManager()` (132) -> gone; facade owns uuidMgr; state read via
  `TryGetEntityGeodetic`.
- `getUnitGeodeticFromSim` (296-334) -> DO NOT replace with `facade->TryGetEntityGeodetic`
  in Phase 1. CORRECTION (this plan's original claim was wrong; the code disproves it):
  the current code uses `static_cast<DtReflectedEntity*>` and DOES return a valid location
  for the disaggregated aggregate `11.MechBn` (PORT.md sec 5 confirms it moved, so
  executeTask did not abandon its task). `TryGetEntityGeodetic` uses `dynamic_cast`, which
  returns null for an aggregate reflected as `DtReflectedAggregate` -> executeTask would
  "ABANDONING TASK" and 11.MechBn would NOT move (golden-trace break); it also drops the
  "UNABLE TO GET REFLECTED OBJECT" / "...ENTITY STATE REPOSITORY" couts. So Phase 1 keeps
  getUnitGeodeticFromSim verbatim (static_cast + couts). The final flip only repoints its
  `uuidMgr` source (line 133) from `textIf->controller()->uuidNetworkManager()` to a facade
  `void* GetUuidManager()` accessor. The dynamic_cast fix (and the health-report fix) land
  in the .NET port (Phase 4).

### main.cxx construction + tick
- Replace lines ~338-412 (vrfArgv build, DtRemoteControlInitializer, controller/exConn
  creation, init, setSessionId, setHostInetAddr) with building a `vrf::StartupConfig`
  from the parsed args and `facade.Start(cfg)` (or `StartAdopting` during transition).
- Tick loop (441-443): `exConn->clock()->setSimTime(...); exConn->drainInput();
  controller->tick();` -> `facade.Tick()`. Keep `DtSleep(0.1)` and the `readCommand()`
  no-op in the caller.
- Delete the `MyDtVrlinkVrfRemoteController` subclass (now in VrfFacade.cpp).

### Callback relocation (the atomic final step)
- `vrfObjectCreatedCb` (textIf.cxx 2069): its body (store `vrfUuid`, set
  `createdObject`, TacticalArea handling, Task route handling) becomes the
  `facade.OnObjectCreated` handler (a C2SIMinterface static; wire in main).
- `reportCallback` (textIf.cxx 683-960): SPLIT. The facade already extracts raw fields
  and fires `OnTaskCompleted{marking,taskType}` / `OnTextReport{text}`. Move the C2SIM
  interpretation - task-complete: `setTaskRouteIsComplete`/`setTaskIsComplete`; text:
  the POSITION/OBSERVATION strtok parse + bundling + sendRest - into
  `OnTaskCompleted`/`OnTextReport` handlers. The report-building helpers
  (`sendC2simReport`, `sendRest`, bundling) stay put (pure C2SIM).
- `scenarioCloseCallback` (672) -> `facade.OnScenarioClosed` handler sets the quit flag.
- After relocation: remove textIf's callback registration block (1912-1944) and the
  controller/exConn members; `Start` replaces `StartAdopting`.

---

## Verification (after the rewire)

1. Build `c2simVRFHLA1516e.sln` Release x64 (VS2019 v142). Must be error-clean.
2. Ensure VR-Forces HLA (CWIX-2024, session 1) is running and the C2SIM container is up;
   free IPv4 8080 (stop the tileserver).
3. Push the STP init (RESET/INITIALIZE/push/SHARE/START) so QUERYINIT returns 80 units,
   SystemName=STP. Run the interface:
   `... STP 0 0 3 127.0.0.1 0 0 0 1 3201 1 3 0 0 CWIX-2024 0` (debug=0).
4. Diff the new trace against `golden-trace-02.log`: same 49 creates + 4 areas, same
   order->route->move->TASKCMPLT. Push `1_VRF_Move_Order.xml` and confirm the tasking
   trace matches.
5. Report path: restart with reportInterval>0, tracking=0; confirm PositionReport output
   matches `reports-captured.log`.
6. THEN do a FRESH-context review of the full diff vs baseline `191933a` (an unanchored
   pass catches what the author rationalizes - see PORT.md notes).

Parity rule: the rewire must NOT change behavior. Bugs in PORT.md section 6 are
reproduced, not fixed, in Phase 1. Fixes land in the .NET port (Phase 4).
