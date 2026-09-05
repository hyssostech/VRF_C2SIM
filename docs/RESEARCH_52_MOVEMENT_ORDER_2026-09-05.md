# 5.2 order/movement research - the gaps PREREG_R9_52 rests on (docs/samples/code, refuter-corrected)

2026-09-05. Read-only sweep (UG52, help, headers, MAK examples, public web, our code) + three
adversarial passes. Every load-bearing line re-verified by the seat; where a refuter corrected the
sweep, the CORRECTED value is what stands here. This closes the four gaps a 5.2 C2SIM ORDER run
faces before the probe is written; it does not run anything.

## G1 - FORMATION-VALIDITY WAIT (a real 5.2 behaviour, in our favour)
A disaggregated unit's formation is VALID iff its promotion map is non-empty - an ASSIGNMENT test,
not geometry: "Will be considered valid if myFormPromIdToEntityMap size is not zero"
(vrfutil/formationState.h:52-53, verified). 5.2 WAITS for validity before starting a unit's move
rather than rejecting it, and REISSUES the move on formation change
(disaggregatedManeuverAlongController.h myRestartTaskOnFormationUpdate + formationChangedCallback).
Release-note key is **VRF-8968** - SETTLED FROM THE RN PDF BY THE SEAT (VRF5.2ReleaseNotes.pdf,
extracted 2026-09-05): "VRF-8968 Tasking a mechanized platoon to Move Along Route fails unless the
scenario is saved and rewound -> The application now waits until a unit's formation is considered
valid before initiating the movement for that unit. VRF-5.2, VRF-5.1.1". VRF-8977 is the fixed-wing
exception. The movement-sweep readers that claimed 8977 were WRONG on the number (the behaviour was
never in dispute); the prior citations across the record already say 8968 and are CORRECT - do not
flip them. Validity arrives ~one
non-zero advance frame after creation (INFERRED from the init/first-tick callbacks), duration NOT
DOCUMENTED - so prereg the OUTCOME (movement eventually starts), never a numeric wait bound.
Our interface additionally queries accepted formations at ObjectCreated and SET+REORGANIZEs via the
async OnVrfAvailableFormations reply (VrfC2SimService.cs:1567-1586 query, :1902-1903 the actual
set/reorganize) - this is INTENT WITH A LIVE RACE, not a barrier: tasking gates only on
_vrfUuidByName (:1070), which is set at :1559 in the same callback, before the reorganize reply
lands (refuter A1/Defect-2; the "BEFORE any tasking" phrasing was not in the source). We lean on
the VRF-side wait, not on our own ordering.

## G2 - MoveAlongRoute ON A UNIT: authored route is CONSUMED (route-by-uuid still works)
A ground-vehicle unit move-along is repackaged as maneuver-along and the authored route is the
"original route" from which per-subordinate OFFSET routes are built - read, not ignored
(disaggregatedMoveAlongController.h:58-60 "working routes parallel to the original route";
disaggregatedMoveAlongAdapterController.h "repackage move-along as maneuver-along"; UG52 30.22
p598/30.24 p599). Route-by-uuid contract unchanged: DtMoveAlongTask route()/setRoute(DtUUID)
(moveAlongTasks.h:79-82), createRoute + moveAlongRoute(entity,route) by uuid
(vrfRemoteController.h:1023-1039,:1636-1640); MAK's own commandLineRemoteController.cxx uses the
same create-then-task-by-uuid pattern; our projector matches (VrfC2SimService.cs:1433,:1636). An
ENTITY (1.BdeHQ) is a plain vertex-by-vertex follow, no planning between vertices
(moveAlongTasks.h:26-30). NOT DOCUMENTED: the unit LEADER's offset magnitude - whether the leader
coincides with the authored centerline. Do NOT prereg "leader tracks the authored vertices
exactly"; prereg "the authored route is consumed and the unit advances along it".

## G3 - COMPLETION + ARRIVAL (retire the 5.0.2 leading-edge/250 m rule)
Unit completion on the 5.2 ground config = ALL tasked subordinates complete, backstopped by a
stuck-subordinate fail-safe (disaggregatedManeuverAlongController.h:173-178 processTaskComplete,
:285-292 isUnitMovementExhausted; disaggregatedMoveAlongController.h:46-49 "complete when all
subordinates have reached the end of the route"). The legacy "lead subordinate completes"
(moveAlongTasks.h:32-37) is superseded FOR THIS CONFIG. Caveat (refuter A2): the all-subordinates
text is byte-identical in 5.0.2 headers, so this is the CONFIG'S model, not a 5.2 code change - but
it IS a change from how THIS PROJECT scored (5.0.2 runs used leading-edge). Entity completion =
last vertex reached.
**ARRIVAL TOLERANCE IS DOCUMENTED (refuter Defect-1, seat-verified in the DEPLOYED SMS):**
EntityLevel/vrfSim/systems/movement/ground-tracked.sysdef - ground-auto-move-along-controller
`(near-distance 15.0)(at-distance 1.0)` (:311-312); maneuver-in-formation-controller
`(near-distance 15.0)(at-distance 2.0)` (:383-384); follow-in-formation at-distance 1.5. Read by
the LIVE atDestination() (groundMoveToDestinationControllerComponent.h:157-165, ABOVE the
Deprecated block; only nearDestination/passedDestination are deprecated). So the vendor arrival
number for 5.2 ground move-along is **at-distance 1.0 m** (2.0 m for maneuver-in-formation), NOT
250 m and NOT "no number". Other numerics: 10 m "Treat Route as Road" (task FAILS if outside),
10 s "Abandon if Time-on-Target" (UG52 30.24/30.25). DtTaskCompleteReport still carries
success()/taskId()/taskTrackingNumber(), fired per task (taskCompleteReport.h:68-102). MoveAlongRoute
is NOT a NETN-ETR class (IOG 2.3), so completion is via VR-Forces' own task path. ETA removal is
DISPLAY-only for entity-level ground (RN, exact key not primary-verifiable - state as display-only,
not with a ticket number).

## G4 - CREATE-vs-ORDER TIMING (our code; the run MUST guard this)
An order whose sequencer gate opens before the taskee's ObjectCreated has populated _vrfUuidByName
is DROPPED, never queued or retried ("DROPPING TASK ... WAS NOT CREATED" + NotifyAbandoned,
VrfC2SimService.cs:1069-1076, verified verbatim; drop-not-queue is oracle parity, C++ :2046-2050).
Tasking gates only on the sequencer (predecessor + start delay, TaskSequencer.cs:86-134); nothing
gates on creation. The terrain deferral I added (def8a5c) WIDENS this window: creates are enqueued
only from FinalizePlacement->EnqueueCreates (:916) after the terrain reply or the 10 s timeout
(TerrainProfileTimeoutSeconds, VrfSettings.cs:261), then each still round-trips to ObjectCreated.
The PLACEMENT summary line (:914) means creates were ENQUEUED, not that any object EXISTS -
necessary but NOT sufficient. There is NO Info-level "all taskees created" signal (:1560 is Debug).
=> the run must push the ORDER only after every taskee's creation is CONFIRMED (reflected-object
plateau in the WatchVrf trace, or app Debug log, or a margin >= 10 s + a create round-trip), and
gate health on `DROPPING TASK == 0` AND `ABANDONING TASK == 0`. (ABANDONING also fires if
TryGetEntityGeodetic returns null for a disaggregated aggregate, :1174-1180 - a separate known
risk to watch.)

## WHAT THIS MEANS FOR PREREG_R9_52 (all documented/verified above)
1. Push order = R9_Mojave_UnitMove_Order.xml, ONLY after all taskees confirmed created (G4).
2. Expect the formation-validity wait; score the OUTCOME (moves start, completes arrive), not a
   wait duration (G1). Expect areas-before-units create order in the querying modes.
3. Authored route is consumed; units advance along it via offset routes; do not score leader-exact
   vertex tracking (G2).
4. Score completion = all-subordinates (NOT leading-edge); arrival against the SHIPPED at-distance
   1.0 m / 2.0 m + near-distance 15 m (NOT 250 m); TASKCMPLT via DtTaskCompleteReport per task,
   rule-4 pairing as before (G3).
5. Instrument health: DROPPING TASK == 0, ABANDONING TASK == 0 (G4).
6. Still off-road by default (Y-13), autonomy on (Y-12), no frame claim (REBASELINE_52).

## NOT DOCUMENTED (carry as run-closable, do not assert)
- formation-validity wait duration/timeout; leader offset magnitude vs the authored centerline;
  the aggregate's own published Z rule; whether AggregateTacticalLevel sysdefs share these numbers
  (EntityLevel verified only).
