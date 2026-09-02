# ANALYSIS - COA-STP1 RUNG 1: why four aggregates built ZERO offset routes

Offline forensic pass on run `runs/20260902T125423Z_run` (COA-STP1, real init + real order,
1x, 2700 s, DeStack on, fan-out off, TerrainProfile vertices). Outcome record:
`docs/experiments/PREREG_COASTP1_RUNG1_BOUNDED_2026-09-02.md` sec 6, commit 7963aed.
No live run was performed for this analysis.

## VERDICT (one line)

DISCRIMINATOR FOUND, EXCEPTION-FREE: **route-NAME LENGTH**. Every performer whose route name
is <= 34 characters marched; every performer whose route name is >= 36 characters froze. The
move-along task addresses the route by name through `DtUUID`, whose storage is a fixed
36-byte blob (1 type byte + 35 payload) - so the name arrives at the aggregate truncated to
35 characters, matches no object, and no offset route is ever built. None of H1-H5 survives.

## 1. Docs consulted (read before the logs, per the 2026-09-01 directive)

- `C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h` - **`class DtUUID`, protected member
  `char myData[36];` with the comment "The UUID has been changed to be a memory blob of fixed
  size. The blob's format is the first char is the type, and the rest is the data."**
  `data()` returns `&myData[0]`; `dataSize()` returns `sizeof(myData)` (36);
  `setBuffer(const char*)` copies that fixed blob; the string constructors keep a
  non-VRF_UUID string as object marking text (a STRING-type UUID) inside it. So a string
  UUID has at most 35 payload bytes: 34 characters plus a terminator.
- `include\vrfmodel\disaggregatedMoveAlongController.h` - the controller creates temporary
  working routes for each subordinate, at offsets obtained from the formation state
  repository. `generateFormationRoutes(...)` returns "true if the function completed, false
  if it is still waiting for data"; `buildOffsetRoute(...)` carries a `bool& dataAvailable`
  out-param; `adjustOffsetRouteStart/End` and `clipRouteEnd` return false on an invalid or
  over-clipped route. Member `DtSimObjectReference myRoute` - the task's route is held as a
  REFERENCE that must resolve before anything is generated. Requires a
  vrf-aggregate-state-repository and a command radio.
- `include\vrfmodel\disaggregatedLeadFollowInFormationController.h` - the variant used by the
  movers: creates ONE leader offset route (`createLeaderOffsetRoute`), then issues
  lead-formation + follow-in-formation subtasks. Same `DtSimObjectReference myRoute`.
- `include\vrfmodel\groundFollowInFormationController.h` - `createFollowerOffsetRoute(...)`
  "Returns true when the route is created"; it is fed the LEADER's points, so it cannot run
  at all if the leader route was never created.
- `include\vrfmodel\organizationController.h` - echelon IDs are assigned by
  `generateEchelonId` / `processSetDesignator`; the "Setting new designator: N" console lines
  are this component. The descriptor lives at `include\vrfobjparam\` (not `vrfmodel\`).
- `include\vrftasks\taskCompleteReport.h` - `DtTaskCompleteReport` DOES carry a status:
  `setSuccess(bool)` / `success()`, "A task complete report with success being false indicates
  that the task has failed and is no longer being processed. Defaults to True." Also
  `taskId()`, `subtaskId()` (deprecated), `taskTrackingNumber()`.
- `include\vrftasks\moveAlongTasks.h` - "The string name of the route is specified as a
  parameter of the task. The route name refers to a DtSimObject";
  `virtual void setRoute(const DtUUID&)` and `virtual const DtUUID& route() const`
  ("Was: routeName"). This is where the 36-byte blob is applied to a route NAME.
- `VRFUsersGuide.pdf` 13.2.2 "Simulation Object Names" (p.363): "Simulation object names have
  the following restrictions on their length: Entities - 11 characters. Units - 31
  characters." p.988: "A graphical object's name can be up to 255 characters long." NO stated
  limit on the route name carried in a task - the doc gap that let this through.
  21.1.1 "How Units Are Organized" (p.499): organization is by echelon ID, "handled
  automatically by VR-Forces when you create a unit"; reorganization applies only on member
  death and only to disaggregated units; the auto-reorganize scenario parameter (p.353)
  points at that section. 21.1.2 "How Units Move" (p.500): "When a
  disaggregated unit receives a movement command, VR-Forces calculates the path the unit
  leader must follow ... Then it calculates parallel paths (taking into account the
  formation) for each member." Neither chapter conditions movement on organization state at
  task-arrival time, and the guide states no navigation-data prerequisite for this path (the
  run logged zero "Waiting for nav data" with NavArea disabled - prereg P3.1).
- docs.mak.com 4.10 Developer's Guide: UNREACHABLE. /vrforces/4.10/ and
  /vrforces/4.10/developersguide/index.html both 404; the docs.mak.com root serves a bare
  directory index with no VR-Forces set. The class-doc copy surfaced by search
  (ftp.mak.com/out/classdocs/vrforces4.4.1/classref/entitymodels_aggregates.html) fails DNS,
  matching the memory note that ftp.mak.com is dead.
- In-repo prior art: `docs/UNIT_MOVEMENT_RESEARCH.md:321-330` - the "moveAlong() - empty
  route" grep oracle and the (since RETRACTED) terrain-region verdict.
  `docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md:1192-1215` - part 12 declared
  "name length" among the exhausted/falsified candidates and asserted "Route names pass
  through VR-Forces at up to 99 chars UNTRUNCATED". That test looked at the CREATION path and
  at 10-char entity-marking collisions; it never inspected the route name inside the
  move-along TASK. See sec 5, dissent.
  `docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md` - the region cause was
  falsified there, leaving "partial movers" unexplained.

## 2. The 9-row comparison table

Route name = task.TaskName + " ROUTE" (`src/VrfC2SimApp/VrfC2SimService.cs:929`). "in-task"
is the name as the BACK END received it (bin64-vrfSim.log:52949-52987, all at sim t=16.358,
wall 08:57:16).

```
  perf unit          tmpl / controller  created (line)   nameLen  in-task  offRt  moved
  ---- ------------- ------------------ --------------   -------  -------  -----  -------
  T1   1-35/2/1_A    GndAgg / lead-foll 08:56:52 (11257)    34       34       4   13.4 km
  T15  1-6/2/1_AD    GndAgg / lead-foll 08:56:49  (8744)    34       34       4   26.7 km
  T19  40/2/1_AD     GndAgg / lead-foll 08:56:48  (8098)    33       33       4   13.2 km
  T39  C/1-35        TankCo / move-alng 08:56:51 (11031)    29       29      18   24.2 km
  T5   4-27/2/1_A    GndAgg / move-alng 08:56:46  (6471)    99       35       0    0.09 km
  T27  856/HHC       TankCo / move-alng 08:56:48  (8191)    56    35+junk     0    0.00 km
  T31  5-20/2/1_A    GndAgg / move-alng 08:56:48  (7480)    40       35       0    0.00 km
  T35  B/5-20        TankCo / move-alng 08:56:46  (5725)    36       35       0    0.00 km
  T23  1-1/2/1_AD    Tank ENTITY        08:56:52 (11281)    36       35     n/a    0.18 km
```

Verbatim in-task names, sorted by length (deduped from the Move-Along Route parameter):

```
  29  T39_AOA_SE_C/1-35_AR_P1 ROUTE          <- mover, intact
  33  T19_AOA_SE_40_EN;_2/1_AD_P1 ROUTE      <- mover, intact
  34  T15_AOA_SE_1-6_IN;_2/1_AD_P1 ROUTE     <- mover, intact
  34  T1_AOA_SE_1-35_AR;_2/1_AD_P1 ROUTE     <- mover, intact
  35  T23_AOA_SE_1-1_RECON/2/1_AD_P1 ROUT    <- freezer, cut from 36
  35  T31_AOA_SE_5-20_IN_(MECH);_2/1_...     <- freezer, cut from 40 (trailing space)
  35  T35_AOA_SE_B/5-20_IN_(MECH)_P1 ROUT    <- freezer, cut from 36
  35  T5_ConductCounter-FireAndNeutraliza    <- freezer, cut from 99
      T27_SecureMovementCorridorsAndPasse then "UUIDx" + non-ASCII garbage, no closing
      quote                                  <- freezer, cut from 56, UNTERMINATED
```

The route OBJECTS were created with their FULL names and are present in the back end
(bin64-vrfSim.log:47421-47439) - e.g. T5's 99-character route object, intact, at 08:57:14,
two seconds before dispatch. The asymmetry is in our own two call paths:
`VrfFacade::CreateRoute` passes `DtString(name.c_str())` (unbounded) at
`src/VrfFacade/VrfFacade.cpp:529-534`, while `VrfFacade::MoveAlongRoute` passes
`DtUUID(routeUuid)` (the 36-byte blob) at `:569-571`. The C# side deliberately addresses the
route BY NAME: `src/VrfC2SimApp/VrfC2SimService.cs:1142` and `:1172`.

## 3. Mechanism

1. `MoveAlongRoute(unitUuid, routeName)` constructs `DtUUID(routeName)`. The name is not a
   VRF_UUID string, so it is kept as a STRING-type UUID inside `char myData[36]` (rwUUID.h).
   One byte is the type tag; 35 remain. Names of 34 characters or fewer fit with a
   terminator; longer names are cut at 35 with no terminator - which is exactly what T27's
   unterminated, junk-trailing console line exposes.
2. `DtMoveAlongTask::setRoute(const DtUUID&)` carries that truncated blob to the aggregate.
3. In the aggregate, `DtSimObjectReference myRoute` never resolves: no object is named
   T5_ConductCounter-FireAndNeutraliza. `generateFormationRoutes` / `beginFollowInFormation`
   are never reached, so ZERO member offset routes are created and no subordinate is tasked.
   The controller stays tasked forever - which is why the sequencer logged those four units
   as "BUSY (task in flight)" for the whole window and no TASKCMPLT ever came.
4. Nothing is logged: the July oracle line "moveAlong() - empty route -- not sending move
   along to subordinate" is emitted from a LATER stage that is never reached - which is why
   the failure went silent (prereg P3.1 / P3.2).

## 4. Hypothesis verdicts

- H1 - dispatch beat subordinate creation / organization: REFUTED. All 1,732 objects received
  their echelon designator by 08:57:02 (count of "Setting new designator" = 1732); every
  move-along arrived at 08:57:16 - a 14 s margin for movers and freezers alike. Creation
  order does not separate them either: by log line, B/5-20(F) 5725, 4-27(F) 6471, 5-20(F)
  7480, 40(M) 8098, 856(F) 8191, 1-6(M) 8744, C/1-35(M) 11031, 1-35(M) 11257, entity
  1-1(F) LAST at 11281 - freezers both first and last, 856/HHC breaking the only monotone
  reading.
- H2 - leader selection failed: REFUTED as a cause, CONFIRMED as a consequence. The
  "follow-in-formation: leader=" lines exist only for T1/T15/T19 (GndV 73/61/49) and T39
  (M1A2 851/853/857/861), i.e. only where a route resolved. The freezers never reach leader
  selection: it happens inside beginFollowInFormation, downstream of route resolution. No
  "no leader" or organization-failure line exists for any of the four.
- H3 - physical boxing-in on the de-stacked pile: REFUTED. Local density at the first
  position report does not separate the classes: movers 1-35 / 1-6 / 40 / C/1-35 have
  4 / 6 / 6 / 4 other units within 75 m, freezers 4-27 / 856 / 5-20 / B/5-20 / 1-1 have
  6 / 6 / 8 / 9 / 4. Mover 1-6 ties freezer 4-27, and freezer 1-1/2/1_AD is as uncrowded as
  the two least-crowded movers. Boxing-in would in any case still yield offset routes (they
  are geometric offsets from formation state), so it cannot produce ZERO.
- H4 - aggregated vs disaggregated state differs: REFUTED. All eight aggregates got
  "vfObjMgr - setting <name> to DtDisaggregated" (lines 5634, 6457, 7466, 8084, 8100, 8730,
  10940, 11243) - four movers and four freezers, identical - and all eight then entered
  base-system.disaggregated-movement.move-along-controller.
- H5 - 2-point routes are a separate class from 5-point: REFUTED. Freezers include both
  2-point (T5, T27) and 5-point (T31, T35, T23); movers are all 5-point. Vertex count does not
  separate the freezers from each other, and the split it does produce is fully subsumed by
  name length. Corroborates prereg P3.3: all nine routes were fully terrain-authored.
- H-NEW - route-name length above 35 characters truncates the task's route DtUUID: SUPPORTED,
  no exceptions in 9 of 9. Evidence: the length table in sec 2; intact full-length names at
  creation (47421-47439); the 35-character cut with unterminated junk for T27 (52979); the
  single "(null) destination" warning in 140 MB of log (52994, T23); the vendor's own
  `char myData[36]` with a type byte (rwUUID.h).

## 5. Dissent against the 2026-07-16 ruling

MOJAVE_ROOTCAUSE part 12 (:1197-1205) listed "name length" as falsified and stated "Route
names pass through VR-Forces at up to 99 chars UNTRUNCATED". That statement is true of the
CREATION path and is re-confirmed here (T5's 99-char route object exists intact). It was
never tested on the route name inside the move-along TASK, which is a different transport
(DtUUID, not DtString). This analysis does not reopen the 10-char marking-collision finding,
which stands. Evidence that would reopen THIS finding: the falsifier in sec 8.

## 6. The entity T23 and the task-complete status field

Timeline (bin64-vrfSim.log): 52972 movement-chooser begins the move-along task (ID=0) at sim
t=16.358; 52973 and 52977 the task and its subtask both name the truncated
T23_AOA_SE_1-1_RECON/2/1_AD_P1 ROUT; 52976 move-along begins the vrf-move-along subtask
(ID=1); then 52994 "Warning: DtGroundMoveToDestinationControllerComponent::tick -- Stopping
ground move to goal controller because (null) destinationcould not be setup." - the ONLY
occurrence of that warning in the whole 140,902,719-byte log; then 52995/52996 the subtask
clears and Completes and 53015 the movement-chooser clears task ID=0, all at sim t=17.183,
0.8 s after dispatch. No route or path-planning line appears in between.
watchvrf-trace.csv holds exactly ONE TSK row for the whole run,
TSK,57,"1-1/2/1_AD","move-along"; the unit sat in its de-stack ring (0.18 km net) and its
false completion released T24 (vrfc2simapp.log:447-451).

DOES THE COMPLETION MESSAGE CARRY A STATUS OUR FACADE DROPS? YES. `DtTaskCompleteReport`
exposes `success()` (default true; false means "the task has failed and is no longer being
processed"), `taskId()` and `taskTrackingNumber()`. `src/VrfFacade/VrfFacade.cpp:217-242`
(`reportTrampoline`) populates only `ev.unitMarking = msg->transmitter().markingText()` and
`ev.taskType = tc->taskCompleted().string()`; `struct TaskCompleted`
(`src/VrfFacade/VrfFacade.h:119-123`) has no success / id / tracking field at all. A
success=false failure report and a genuine success are therefore indistinguishable above the
facade. Whether VR-Forces actually set success=false here is NOT determinable from these
artifacts - the console never prints it. Forwarding the flag is a two-line change and would
have turned this run's false TASKCMPLT into a reported failure.

## 7. Verified vs inferred vs unexplained

VERIFIED (directly in the artifacts): the route-name lengths and the 35-character cut; route
objects created at full length; all nine tasks received at t=16.358; the 1:1 correspondence
of offset routes with movement; the single "(null) destination" warning belonging to T23;
identical disaggregated state and completed organization across all performers; pile density
not separating the classes; the facade's dropped success(); docs.mak.com/ftp.mak.com dead.

INFERRED: that `DtUUID::myData[36]` is the buffer doing the cutting (the header's comment
plus the 35-character observed cut make it the only fitting candidate; no instrumentation
proves it); and that the freezers' controllers stayed blocked on an unresolved
`DtSimObjectReference myRoute` (consistent with permanent BUSY and zero diagnostics, never
logged).

UNEXPLAINED: "buildEntityRouteFollowingMap() : Can't find entity route" appears 14,880 times,
starting at 08:57:16 (the dispatch second, line 52991) and running at a flat ~335/min to
09:42:41. The lines carry no object prefix at notify level 3, so they cannot be attributed to
a unit from this log. They are new to this analysis - the prereg did not mention them - and
they are the closest thing to a diagnostic the freeze produced. Whether they belong to the
four freezers, to the movers' formation maintenance, or to both, is not determinable from
these artifacts. This is an unexplained symptom, not a footnote: it does not contradict
H-NEW, but H-NEW does not yet account for it either.

## 8. Falsifier and the one-variable probe

FALSIFIER for H-NEW: a performer whose route name is 36 characters or longer that builds
member offset routes and marches; or a performer whose route name is 34 characters or shorter
that builds zero offset routes while its route object is present. Either observation kills it.

PROBE (R9-sized, one variable, minutes under the fixed-frame mode now being validated):
ROUTE-NAME LENGTH, A/B. Same fixture, region and performer type; two move-along tasks issued
back to back to two identical units, differing ONLY in route name - unit A 30 characters,
unit B the SAME geometry under a 40-character name (pad the task name; touch nothing else).
If H-NEW holds: A builds member offset routes and moves; B builds zero, logs no diagnostic,
never completes, and its in-task name appears cut to 35 characters in bin64-vrfSim.log. If
H-NEW is false, both behave alike. Oracles: the deduped Move-Along Route parameter names and
the count of "Offset Route" object creations.

FIX (not applied here; not before the probe): cap the name passed to MoveAlongRoute /
CreateRoute at 34 characters, or better, name routes with a short synthetic id as VR-Forces
does internally - T39's own generated sub-routes are C/1-35_R0..R3, 9 characters, and they
resolved. The C2SIM task name belongs in the log line, not the object name. Independently,
forward DtTaskCompleteReport::success() / taskId() / taskTrackingNumber() through
VrfFacade::TaskCompleted so a failed completion stops masquerading as a successful one.
