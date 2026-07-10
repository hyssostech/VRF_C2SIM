# Task Expansion Plan: mapping C2SIM tasks to VR-Forces behaviors

Status: PLAN ONLY (not yet implemented). Captured 2026-07-09.
Owner note: another session is actively editing the interface files
(VrfFacade.*, C2SIMinterface.cpp). Do NOT hand-merge this against their
work blind - re-read those files before implementing any snippet below.

## 1. Goal

Today the interface collapses almost every C2SIM `actionTaskActivityCode`
to a route-follow. VR-Forces exposes a large native task catalog through
the SAME channel the facade already uses (`controller->sendTaskMsg` for
`DtSimTask`, `controller->sendSetDataMsg` for `DtSetDataRequest`). The
plan: add facade methods that build specific native tasks, and add
dispatch branches keyed on `actionTaskActivityCode`, so C2SIM tasks drive
realistic simulated execution beyond moves.

## 2. Current tasking surface (baseline)

Facade (`VrfFacade.cpp`) exposes only:
- `MoveToLocation` / `MoveAlongRoute`  -> `moveToLocation` / `moveAlongRoute`
- `SetRulesOfEngagement`, `SetTarget` (SetTarget is the known no-op, PORT.md sec 6)
- `RunScriptedTask` (`sendTaskMsg` + `DtScriptedTaskTask`)
- `SendScriptedSet` (`sendSetDataMsg` + `DtScriptedTaskSet`)

Dispatch (`C2SIMinterface.cpp`, the `startTask`/run path ~L2150-2430):
- `EVACTN` -> `RunScriptedTask("evacuate_civilians", ...)`
- `EMBARK` / `DEBARK` -> written but COMMENTED OUT (L2360-2371)
- everything else -> ROE + SetTarget + Move(To|Along)

Only one scripted task is actually shipped and wired: `evacuate_civilians.xml`
(under `VRFadditionalFiles/.../C2simEx/scripts/`).

## 3. VR-Forces 5.0.2 native task catalog (grounded)

Enumerated from `C:\MAK\vrforces5.0.2\include\vrftasks\`. All `*Task` classes
below are `DtSimTask` subclasses reachable via `sendTaskMsg` UNLESS noted as
`DtSetDataRequest` (which go via `sendSetDataMsg`).

- Maneuver: `followEntityTask`, `convoyToTask`, `convoyAlongTask`,
  `moveIntoFormationTask`, `patrolRouteTask`, `patrolTwoPointsTask`,
  `orbitTask`, `moveToAltitudeTask`, `turnToHeadingTask`, `stopMovingTask`,
  `planAndMoveToTask`, `planAndMoveToLocationTask`, `flyToLocationTask`,
  `flyAlongTask`
- Fires (direct/indirect): `targetEntityTask`, `targetPointTask`,
  `fireAtTargetTask`, `crashIntoTask`, `provideIndirectFireTask`,
  `fireForEffectOnLocation/OnEntity/OnTarget`, `fireForEffectTask`,
  `fireCruiseMissileTask`, `releaseBombOn{Location,Target,LaserSpot}`,
  `laseTargetTask`, `laseLocationTask`, `launchSmokeTask`
- Engineering / dismounted: `breachTask`, `embarkTask`, `disembarkTask`,
  `disembarkAllTask`, `diGuyAnimationTask`, `diGuyGestureTask`
- Air ops: `takeoffTask`, `landingTask`, `verticalLandingTask`,
  flight-command tasks, `internalTransferFuelTask`
- Logistics: `transferSuppliesTask`
- Synchronization: `holdUntilTask`, `waitDurationTask`, `waitElapsedTask`,
  `waitTask` (honor C2SIM start-conditions / triggers)
- Reporting: `spotReport`, `engagementReport`, `taskCompleteReport`,
  `textReport`, plus `setSpotReportingRequest` / `setEngagementReportingRequest`
- Reactive behaviors (SetData): `setReactiveTaskTable`,
  `setReactiveTaskEnabled/Priority/Cancel/Disabled` - built-in
  react-to-fire / return-fire / avoid-obstacle behaviors

CORRECTION (verified by reading the header): `DtClearTask` (`clearTask.h`)
is a `DtSetDataRequest`, and it means "CANCEL the current task", NOT the
tactical "clear an area of enemy". Do NOT map C2SIM CLEAR to it. The C2SIM
CLEAR verb is composite (move + engage) and needs a script or facade-composed
sequence. `DtClearTask` is still worth exposing as an honest task-cancel
utility (maps to C2SIM task cancellation / retasking), sent via
`sendSetDataMsg`.

## 4. Verified header signatures for the first wins

Read directly from the 5.0.2 headers.

- `DtFollowEntityTask` (`followEntityTask.h`): `init()`,
  `setEntityToFollow(const DtUUID&)`, `setOffsetVector(const DtVector&)`.
  Offset is BODY coords in meters: x out the front, y right, z down
  (z ignored for ground). To follow 25 m behind, 5 m right: (-25, 5, 0).
  If the followed entity is destroyed, follower stops and sends taskComplete.
- `DtEmbarkTask` (`embarkTask.h`): `init()`, `setParent(const DtUUID&)`,
  plus optional slot controls (`setEmbarkInSlot(int)`, `setSlotName`,
  `setSlotType`, `setChooseAnyAvailableSlotIfTaken(bool)`,
  `setLoadOverride/Position`). Minimum viable = setParent only.
- `DtDisembarkTask` (`disembarkTask.h`): `init()` only, no params
  (entity disembarks from its current parent).
  `DtDisembarkAllTask` (`disembarkAllTask.h`): `init()` only, sent to the
  PARENT to eject all embarked units.
- `DtBreachTask` (`breachTask.h`): `init()`, `setBreachTarget(const DtUUID&)`,
  optional `setBreachStPt(const DtVector&)` / `setBreachEndPt(const DtVector&)`
  (geocentric world points). Minimum viable = setBreachTarget only.

All four are sent with `controller->sendTaskMsg(DtUUID(unitUuid), &task)`,
exactly like `RunScriptedTask`.

## 5. Proposed facade additions

Header (`VrfFacade.h`, under the `-- tasking --` block):

```cpp
// Native VR-Forces tasks (sent via sendTaskMsg, like RunScriptedTask).
// Follow another entity, holding standoff distanceBehindMeters behind it
// (body-coordinate offset; the facade builds (-distance, 0, 0)).
void FollowEntity(const std::string& uuid, const std::string& leaderUuid,
                  double distanceBehindMeters = 25.0);

// Embark `uuid` onto carrier `parentUuid` (first available slot).
void Embark(const std::string& uuid, const std::string& parentUuid);

// Disembark `uuid` from its current carrier.
void Disembark(const std::string& uuid);

// Eject all units embarked on carrier `parentUuid`.
void DisembarkAll(const std::string& parentUuid);

// Breach the obstacle identified by `targetUuid`.
void Breach(const std::string& uuid, const std::string& targetUuid);
```

Implementation (`VrfFacade.cpp`), mirroring `RunScriptedTask` exactly.
Add includes near the existing `#include <vrftasks/scriptedTaskTask.h>`:

```cpp
#include <vrftasks/followEntityTask.h>
#include <vrftasks/embarkTask.h>
#include <vrftasks/disembarkTask.h>
#include <vrftasks/disembarkAllTask.h>
#include <vrftasks/breachTask.h>
```

```cpp
void VrfFacade::FollowEntity(const std::string& uuid,
                             const std::string& leaderUuid,
                             double distanceBehindMeters) {
    DtFollowEntityTask task;
    task.init();
    task.setEntityToFollow(DtUUID(leaderUuid));
    // body coords: -x is behind, y right, z down (z ignored on ground)
    task.setOffsetVector(DtVector(-distanceBehindMeters, 0.0, 0.0));
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::Embark(const std::string& uuid,
                       const std::string& parentUuid) {
    DtEmbarkTask task;
    task.init();
    task.setParent(DtUUID(parentUuid));
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::Disembark(const std::string& uuid) {
    DtDisembarkTask task;
    task.init();
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::DisembarkAll(const std::string& parentUuid) {
    DtDisembarkAllTask task;
    task.init();
    p_->controller->sendTaskMsg(DtUUID(parentUuid), &task);
}

void VrfFacade::Breach(const std::string& uuid,
                       const std::string& targetUuid) {
    DtBreachTask task;
    task.init();
    task.setBreachTarget(DtUUID(targetUuid));
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}
```

Note: `DtVector` and `DtUUID` are already used throughout `VrfFacade.cpp`,
so no new low-level includes beyond the task headers. Confirm `init()` is
the right lifecycle call - `RunScriptedTask` calls it, and `DtSimTask`
declares `virtual bool init()`, so this matches the established pattern.

## 6. Proposed dispatch wiring

In `C2SIMinterface.cpp`, alongside the existing `EVACTN` branch and the
commented `EMBARK`/`DEBARK` block (~L2356-2371), replace the special-case
block with real calls. Sketch (verb codes are the C2SIM
`actionTaskActivityCode` controlled vocabulary - confirm exact spellings
against the C2SIM ontology / incoming orders before wiring):

```cpp
if (thisTask->actionTaskActivityCode == "EVACTN") { runEvacuate(thisTask); return; }
if (thisTask->actionTaskActivityCode == "EMBARK") {
    textIf->facade()->Embark(taskUnit->vrfUuid, <carrierVrfUuid>);
    return;
}
if (thisTask->actionTaskActivityCode == "DEBARK") {
    textIf->facade()->Disembark(taskUnit->vrfUuid);
    return;
}
if (thisTask->actionTaskActivityCode == "FOLLOW") {
    textIf->facade()->FollowEntity(taskUnit->vrfUuid, <leaderVrfUuid>);
    return;
}
if (thisTask->actionTaskActivityCode == "BREACH") {
    textIf->facade()->Breach(taskUnit->vrfUuid, <obstacleVrfUuid>);
    return;
}
```

OPEN DEPENDENCY: EMBARK/FOLLOW/BREACH need a SECOND entity resolved to a
VRF uuid (carrier, leader, obstacle). The C2SIM order carries this as an
affected/related entity; `getAffectedEntity(thisTask)` exists but note the
SetTarget bug (PORT.md sec 6) - it returns a C2SIM uuid, and tasking needs
the mapped VRF uuid. The C2SIM-uuid -> VRF-uuid mapping must be resolved
(same fix the SetTarget defect needs). This is the main blocker for the
multi-entity verbs; DEBARK (no second entity) is the cleanest to land first.

## 7. C2SIM verb -> VR-Forces mapping (classification)

Native 1:1 (this plan):
- FOLLOW  -> DtFollowEntityTask
- EMBARK  -> DtEmbarkTask
- DEBARK  -> DtDisembarkTask (/ DtDisembarkAllTask on carrier)
- BREACH  -> DtBreachTask
- (task cancel / retask) -> DtClearTask via sendSetDataMsg

Native, straightforward follow-ons:
- MOVE/ADVANCE with formation -> moveIntoFormation / convoyAlong
- SCREEN/GUARD/DEFEND         -> patrolRoute / patrolTwoPoints + scan sector
- RECCE/SURVEIL              -> patrolRoute + setSpotReporting + laseTarget
- FIRE SUPPORT / indirect    -> provideIndirectFire / fireForEffectOnLocation
- RESUPPLY/SUPPORT           -> transferSuppliesTask
- air LAND/TAKEOFF/ORBIT     -> landing/takeoff/orbit tasks

Composite (need a behavior script or facade-composed sequence, NOT 1:1):
- ATTACK/ASSAULT  -> moveTo + targetEntity/fireAtTarget
- OCCUPY/SEIZE/SECURE -> moveTo + holdUntil + scan sector
- AMBUSH -> holdUntil + ROE hold->free + fireAtTarget
- CLEAR (tactical) -> move + engage sweep (NOT DtClearTask)

Highest realism-per-effort, orthogonal to per-verb wiring:
- Enable REACTIVE tasks on created entities (setReactiveTaskEnabled) so
  units react to contact realistically regardless of the driving C2SIM
  task. Cheapest single lever; consider doing this in the entity-creation
  path independent of the verb dispatch.

## 8. Risks / open questions

- Header signatures verified for the 4 native wins; NOT yet verified for
  the follow-on tasks (patrol, formation, indirect fire, etc.) - read each
  header before wiring those.
- C2SIM-uuid -> VRF-uuid resolution is the load-bearing blocker for every
  multi-entity verb (shares root cause with the SetTarget no-op bug).
- Exact C2SIM `actionTaskActivityCode` spellings must be confirmed against
  the ontology / real orders (EMBARK/DEBARK/EVACTN are known; FOLLOW/BREACH
  spellings assumed).
- `init()` lifecycle assumed correct by parity with RunScriptedTask; low
  risk but confirm no per-task extra init is required.
- Behavior parity: these are ADDITIVE (new verbs previously fell through to
  move). Confirm the golden trace still passes - it exercises MOVE only, so
  it should be unaffected, but re-run it after any dispatch edit.
- Coordination: another session edits VrfFacade.* / C2SIMinterface.cpp now.
  Re-read those files before applying; the facade method block and the
  dispatch block may have moved.

## 9. Next steps (ordered)

1. Land DEBARK first (no second-entity dependency) end to end:
   facade `Disembark` + dispatch branch + a smoke test.
2. Solve C2SIM-uuid -> VRF-uuid resolution (unblocks EMBARK/FOLLOW/BREACH
   and also fixes the SetTarget no-op). Do this as one focused change.
3. Land EMBARK, FOLLOW, BREACH on top of (2).
4. Turn on reactive tasks in the entity-creation path; measure realism.
5. Design the composite verbs (ATTACK/OCCUPY/AMBUSH) as scripts alongside
   evacuate_civilians.xml, or as facade-composed task sequences - decide
   per verb after checking whether a reactive behavior already covers it.
6. Read headers and wire the native follow-ons (patrol, formation,
   indirect fire, logistics) as C2SIM orders demand them.
