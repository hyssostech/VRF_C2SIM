# PREREG: open the vendor's object-console channel and let the company explain itself

Date: 2026-09-06. Tier: HEAVY (the outcome feeds a cause claim about C1b, DESIGN_ORBAT_TO_VRF
CLOSED list). Status: REGISTERED - predictions written before the runs (sec 4). Results: sec 6.

## 0. Why this run exists (the knowledge gap, stated once)

C1b (docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md): a template higher-unit created by the remote
`createAggregate(..., createSubordinates=true)` does not move as a unit when tasked - members
scatter - with the formation settle OFF (run 20260906T112702Z, "G-A") and ON (20260906T131748Z).
A composed company (empty shell + `addToOrganization` of declared children, the vendor-sample
recipe) moves and completes 3/3 (20260906T104042Z, 105344Z, 110323Z). The MECHANISM of C1b is
open. Every mechanism proposed so far came from inference (headers, agents), none from the sim
saying what it did. This run asks the sim.

## 1. Documents consulted (Step 0 of the gap plan; all installed 5.2d docs, cited by page)

- UG52 25.2 "How Units Move" (p517): on a movement command VR-Forces computes the LEADER path,
  then "offset destinations for each subordinate ... and the routes to those destinations, which
  are offset from the leader's route ... based on the unit's current formation".
- UG52 25.2.1 "Unit Formations" (p517): a unit created from the Create Object panel is placed in
  the default formation of its unit type (-> 71.5.4 p1413, leftmost tab of the Formation
  Editor). Three ways to set a formation: Move Into Formation task, Transition Into Formation
  task, Formation set-data. **"Formation position is not reported as part of simulation object
  information. The only way to determine a simulation object's formation position is to observe
  the formation as the unit moves."** -> there is NO vendor query for formation validity or the
  promotion map; the gap plan's "read back designators/promotion state via queries" is not a
  thing the product offers. Closing formation vs reorganization: only reorganization changes
  echelon designators.
- UG52 40.33 "Formation" set-data (p879-880): "When you set the formation for a unit, the
  simulation objects that make up the unit snap to formation immediately."
- UG52 13.3.1 (p365): designators are assigned by VR-Forces sequentially from 1; on aggregation
  the unit leader gets designator 1. "You have no direct control over them."
- UG52 21.9 "Configuring Object Console Messages" (p483-484): each simulation object has a
  console "that displays messages sent from the simulation engine, from a simulation object's
  plan, from other simulation objects, and from scripts". 21.9.1: PER-OBJECT notification level
  0 fatal / 1 warn / 2 diag / 3 verbose / 4 debug, "default notification level is 2", settable
  in the Information dialog, by the Notify Level set-data request (40.55 p890), or by the
  `objectConsoleNotifyLevel` parameter in vrfSim.mtl. The messages "are also sent to the Object
  Console Summary Panel" - i.e. they leave the sim engine over the network.
- Installed `C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl` :176 `(setqb notifyLevel 2)`,
  :179 `(setqb objectConsoleNotifyLevel 1)` (factory copy identical). So every prior run had
  object consoles at WARN only: the controllers' diag/verbose/debug messages were never emitted.
  (`sendBackendLogToNetwork` :281-284 concerns the sim LOG, "does not block Object Console
  messages".) The vendor sim log at --notifyLevel 3 carries NOTHING per task: the keyword-line
  multiset (formation/leader/offset/aggregate-move/promot/designator) of the G-A scatter log
  (runs/launch52/vrfSim_3996_...) and the compose V log (vrfSim_3975_...) is IDENTICAL - all 48
  hits are FOM boilerplate. Lesson memory "vendor diagnostics first": a silent channel at a low
  notify level is not evidence.
- Remote API: `DtVrfRemoteController::setObjectNotifyLevel(const DtUUID&, DtNotifyLevelType,
  addr)` vrfRemoteController.h:1951-1954 ("Sends a request to the sim engine to change the
  notification level of the specified object"); `addObjectConsoleMessageCallback` :1946-1949
  (our facade already registers it, VrfFacade.cpp:512); `logObjectConsoleToFile` :1956-1960
  (sim-side file; not used). `DtNotifyLevelType` vlutil/vlPrint.h:39-46 (DtNlFatal 0 ..
  DtNlDebug 4). Classref: `DtSetNotifyLevel` = "Admin content which changes the notification
  level of the object console for a VRF object"; `DtBackendConsoleNetworkPrinter` = "sends the
  console data to the network using the Comment PDU/Interaction".
- Classref `DtVrfRemoteController::createAggregate` (callback overload): `createSubordinates =
  false` is the DEFAULT; `addToOrganization`: "Adds the specified object as a subordinate to the
  specified superior. Object will be detached from any superior it currently belongs to first."
  `sendVrfObjectCreateMsg` is the only remote call carrying `initialFormation`.
  `DtVrfLocalObjectCreateInformation` (GUI create-info): `setCreateSubObjects/createSubObjects`,
  `setOrganized/organized`, `addSubordinate(s)`; NO initialFormation member documented.
- Vendor sample commandLineRemoteController.cxx, full handler inventory (:130-2186): the only
  aggregate creates are `createAggregate` :717-775 (EMPTY shell + `createEntity` members +
  `addToOrganization` in the created-callback :1520-1554) and `aggregateCommand` :899-925 (the
  entityNames-list overload: aggregate EXISTING named entities). NO handler passes
  `createSubordinates=true`; NO handler tasks a unit as such - `moveToPoint` :1757 / `follow`
  :1780 / `patrolRoute` :1734 task by name (`DtUUID(name)`, marking-text lookup, C8 applies).
  So the sample never exercises the template-higher-unit path at all.

## 2. The instrument (one commit, no behavior change with the setting at its default)

- VrfFacade/VrfBridge: `SetObjectNotifyLevel(uuid, level)` -> `setObjectNotifyLevel` (clamped
  0..4). Native Release-5.2 rebuilt (backup src/VrfBridge/build/backup-Release-5.2-*-console).
- App: subscribes `ObjectConsoleMessage` (the bridge raised it since 5.0.2; NO subscriber
  existed) and logs every message as `VRF console [<level>] <name> (<uuid>): <text>`. Setting
  `Vrf:ObjectConsoleNotifyLevel` (default -1 = vendor default untouched): when >= 0 the level is
  requested for every object we create at ObjectCreated, and for the tasked aggregate's
  published members at task time (template members are sim-created; ObjectCreated never saw
  them). `_nameByVrfUuid` now filled on all paths (was B1-only).
- Runner: `-BackendNotifyLevel` (default 3 = unchanged) passed to LaunchVrf52 `-NotifyLevel`,
  recorded in the manifest as `inputs.backendNotifyLevel`.

## 3. Runs (one variable vs its baseline: the instrument; nothing else changes)

Common: `RunC2SimScenario.ps1 -VrfProfile 5.2 -NoGui -Scenario R9_Mojave_Empty_52 -Order
data\R9_Mojave_UnitMove_Order.xml -RunSecs 360`, env `Vrf__ObjectConsoleNotifyLevel=4`,
`Vrf__AggregateFormation` UNSET (auto OFF), rtiexec 15720 / forwarder 43728 preserved.

- Run T (template higher-unit, the C1b reproduction): init `data\GA_LeafCompany_Initialization.xml`
  (114.MechCoy childless -> template "Tank Company (USA)"-class unit, createSubordinates=true),
  `Vrf__ComposeHierarchy` UNSET, `-BackendNotifyLevel 4` (sim-wide debug too, once).
  Baseline: 20260906T112702Z (scatter, 0 TASKCMPLT).
- Run C (composed company, the working control): init `data\R9_Mojave_Lean_Initialization.xml`,
  `Vrf__ComposeHierarchy=true`, `-BackendNotifyLevel 3`. Baseline: 20260906T104042Z (moves,
  3/3 complete).

## 4. Predictions (written BEFORE the runs; a missed high-confidence prediction is a STOP)

- P1 (instrument live) HIGH: both app logs carry `VRF console level 4 requested for ...` for
  every created object, and at least one `VRF console [n] ...` line with n >= 2 from the
  tasked company or a member during the task window. FALSIFIER: zero `VRF console` lines
  after the level request -> the engine does not emit object-console content to a remote
  controller under our posture (or the set-data is ignored) -> next step is
  `logObjectConsoleToFile` (vrfRemoteController.h:1956), NOT another mechanism guess.
- P2 (instrument inert) HIGH: outcomes reproduce their baselines - T scatters (members >
  0.5 km apart, 0 TASKCMPLT), C moves and completes 3/3. FALSIFIER: T moves as a unit or C
  fails -> the notify level itself perturbs behavior; the observation is confounded; stop.
- P3 (content, MEDIUM - this is the actual question, stated as what would DECIDE it, not as a
  guess): T's console during the move names the reason its members do not follow offset
  routes. Decision table, in advance:
  (a) messages about formation (no/invalid formation, no leader/designator, "not organized")
      -> C1b mechanism = unit-state at creation; the fix candidates are the vendor's own
      (initialFormation at create via sendVrfObjectCreateMsg, or compose).
  (b) messages about per-member path planning / route failure (plan failed, no path, terrain)
      -> C1b is a per-member movement failure, NOT formation; compose works because its members
      are the DECLARED platoons (aggregates), not ~48 raw vehicles.
  (c) messages showing the task delegated to subordinates that each plan their own route to
      the DESTINATION (not offset routes) -> C1b = the template unit's controller runs the
      aggregate-move-along controller without the maneuver-along/formation adapter (sysdef
      :177-203 finding), i.e. the unit type's sysdef, not our create call.
  (d) silence at level 4 from the aggregate but chatter from members -> the higher-unit
      controller has no console instrumentation; fall back to the sim-wide debug log
      (BackendNotifyLevel 4) for the same window.
  Anything outside (a)-(d) is recorded verbatim and NOT interpreted in this document.

## 5. Scoring (deterministic; same instruments as PREREG_COMPOSE_A)

- Movement: tools scorer over `watchvrf-trace.csv` (POS rows; haversine displacement and
  bearing of every MechCoy-cluster entity at (34.64763,-116.69339) +/- 0.02 deg); "scatter" =
  members' final pairwise spread > 500 m OR bearings spanning > 90 deg; "unit move" = all
  members displaced > 1 km within 30 deg of the route bearing.
- Completion: app log `VRF task complete:` count and `SENT TASK STATUS REPORT (TASKCMPLT)`.
- Channel: app log `VRF console` line count by level and by object name; the messages of the
  tasked company and its members between the task issue line and +120 s are quoted VERBATIM in
  sec 6 (the log holds no secrets; the vendor sim log does and is not quoted).

## 6. Results

### 6.0 Found BEFORE run T, in the record: the channel was already on the wire, unread
WatchVrf has captured `CON,<t>,<uuid>,<level>,<msg>` object-console rows since 5.0.2
(tools/WatchVrf/ConFormat.cs). At the vendor default level 1 they carried only warnings, and
nobody decoded them. G-A (112702Z) holds exactly one: t=29.7, a sim-created member,
`DtGroundAutoControllerComponent: "Entity not embarked on same object as target [Route 2].
Ending task"`. The compose V run (104042Z) holds 24: the three tasked units' path-start
chatter ("Making pivot geometry / Pivot sweeps full circle") at t=30.0-30.2. The template
run's unit said less at task time than the composed one - consistent with, and explained by,
6.1.

### 6.1 Run T - 20260906T160640Z (template company, console level 4, backend 4)
P1 LIVE (HIGH, confirmed): level-4 requests honored for all 3 units, 22 members and the 3
routes ("Setting to notify level 4" echoed on each object's console); 94k CON rows by t=232,
127k by t=380; levels 0-4 present (plus rows whose level field carries a sim-time prefix,
e.g. 492/4180 - instrument caveat, content intact).

THE COMPANY'S OWN ACCOUNT (114.MechCoy, e1b1e6b1; 12 console rows in the whole run):
```
t=28.8 [2] Controller base-system.disaggregated-movement.move-along-controller beginning to
           process move-along task (ID=0)
t=28.8 [3] Task 0 name and parameters: Move-Along Route: "T_R5_CO1 ROUTE"
t=28.8     Task 0 starting subtask move-into-formation
t=28.8 [2] Controller ...move-into-formation-controller beginning to process
           move-into-formation subtask (ID=1)
t=28.8 [3] Subtask 1: Move into formation: formation: keep-existing-formation loc: {geocentric
           of the current position} heading: ~0
t=50.7     Disagg mv into form: task complete msg rcvd from AR Plt 1
t=50.8     Disagg mv into form: task complete msg rcvd from AR Plt 3
t=51.1     Disagg mv into form: task complete msg rcvd from AR Plt 2
           (nothing further from the company for the rest of the run)
```
So the higher-unit's move-along is GATED on a move-into-formation subtask at the current
location (this is VRF-8977, VRF5.2ReleaseNotes: "The application now waits until a unit's
formation is considered valid before initiating the movement for that unit"). That subtask
fans a formation-slot Move-To to every direct member (move-to-adapter task ID=0 on each) and a
move-into-formation to each sub-unit. The three sub-platoons (sim-created "AR Plt 1/2/3")
report complete by t=51.1. Of the six direct HQ-section members (template "Tank Headquarters
Section (USA)": CDR M1A2, XO M1A2, FSO M3, AUX M998 HMMWV x2, AUX M577), five complete their
move-to-adapter task (M1A2 5 t=50.0, M1A2 6 t=52.0, M3 1 t=49.9, M577A2 1 t=54.9, HMMWV 1
t=46.0 after three Failed attempts incl. the "not embarked on same object as target [Route 2]"
warning). ONE never does: HMMWV 2 receives the slot Move-To (t=28.8), a second Move-To
(t=31.7), a Move-To Direct (t=34.0, Completed 34.7), the slot Move-To again (t=34.7) and at
t=35.6 an internally generated `move-along` (subtask 13) that stays "TaskRunning" (3022 status
lines) while the vehicle drives ENE in a straight line: 713 m from birth at t=65, 2.5 km at
t=106, 7 km at t=207, 12.1 km at t=329 - the "runaway". The company's formation is therefore
never valid, the gate never opens, the route move never starts, no TASKCMPLT.

Decision table (sec 4 P3): (b)/(c) hybrid, vendor-narrated: the unit-level controller is
fine (it processes the task exactly as designed) and the formation is not "invalid at
creation" - the gate is a per-member movement failure of ONE template-created HQ-section
vehicle. (a) is REFUTED (no formation/leader/organization complaint anywhere on the company's
console). The birth geometry is not stacked (23 members, pairwise distance at birth min 0.0 /
median 257 / max 787 m; 1 pair closer than 15 m) - the R8 stacking pathology is not this.

Why compose works (now explained, not inferred): a composed company's members are the three
DECLARED platoons (created at their own authored positions) - and in expand-to-compose one HQ
entity we create - none of which is the template's HQ-section HMMWV pair. Their
move-into-formation completes, the gate opens, the route move runs (V: 3/3 complete).

Not explained and NOT claimed: WHY HMMWV 2's generated move-along runs off the map (its
console shows "Is path blocked? Condition false" 1801x - it is not skirting a blockage; it
is following a route it never finishes). The M998 template has `can-embark`/`can-be-embarked-
upon` True and moves on `ground-wheels-off-road.sysdef`; HMMWV 1's "not embarked on same
object as target" (decideToGiveUpTask, groundAutoControllerComponent.h:317: "If the entity is
attached and the target of the current task is not, give up") says these HMMWVs consider
themselves attached. That is the vendor template's business; it is recorded, not pursued.

P2 (instrument inert) and the movement/completion scoring: see 6.2 (filled after teardown).
Instrument caveat: the notify-level requests do not alter behavior on the evidence so far
(same gate, same runaway class as G-A); confirmed numerically in 6.2.

### 6.2 Scoring
Run T (20260906T160640Z), runner exit 0, RUN COMPLETE. Threshold correction, stated before
run C is scored: the pre-registered "unit move = all members displaced > 1 km" was mis-sized -
the company's route is ~600-900 m long (the V baseline's members moved 467-926 m at bearing
357-3 deg and completed 3/3). Read "unit move" as: members displaced 400-1500 m within 30 deg
of the route bearing (~0 deg) AND the company's TASKCMPLT; "scatter" as: any member > 2 km OR
final spread > 2 km. The qualitative verdicts below do not depend on the correction.

| run | company TASKCMPLT | members snapped 412 m @ 180 deg | runaways (> 2 km) | final spread | verdict |
|---|---|---|---|---|---|
| G-A 112702Z (baseline, console 1) | 0 (2/3 tasks) | 19 of 23 | 3 (4.7 / 13.6 / 16.7 km, brg 358/346/8) | 17.6 km | scatter |
| T 160640Z (console 4, backend 4) | 0 (2/3 tasks) | 20 of 23 | 2 (HMMWV 2 15.5 km brg 50; unnamed 2.4 km brg 56) | 16.1 km | scatter |
| V 104042Z (compose baseline) | 3/3 | 0 | 0 | 0.56 km | unit move |
| C 161714Z (compose, console 4) | 3/3 (by t=78) | 0 | 0 (16 of 16 moved 467-924 m, brg 357-3) | 0.56 km | unit move |

P2 CONFIRMED: the console level is inert on behavior (same gate, same snap, same runaway
class). P1 CONFIRMED. P3 answered by 6.1: (b)/(c), (a) refuted.

### 6.3 Run C - 20260906T161714Z (composed company, console level 4, backend 3) - THE CONTROL
Same controller, same first subtask, different ending - the composed 114.MechCoy (e317ca80),
58 console rows, verbatim skeleton:
```
t=29.0 [2] move-along-controller beginning to process move-along task (ID=0)
t=29.0 [3] Task 0: Move-Along Route: "T_R5_CO1 ROUTE"
t=29.0     Task 0 starting subtask move-into-formation
t=29.0 [3] Subtask 1: Move into formation: formation: keep-existing-formation loc: {current}
t=42.1     Disagg mv into form: task complete msg rcvd from 1141.MechPlt
t=45.9     Disagg mv into form: task complete msg rcvd from 1143.MechPlt
t=51.3     Disagg mv into form: task complete msg rcvd from 1142.MechPlt
t=51.3 [2] move-into-formation-controller's subtask has Completed (ID=1)
t=51.3     Disagg mv alng: Move into formation complete.
t=51.3 [4] Subordinate 1141/1142/1143.MechPlt: maintain-speed 10  (then speed-up 15 / slow-down 1
           adjustments for the rest of the move - the unit pacing its subordinates)
t=75.7     Disagg mv alng: sub 1142.MechPlt completed move along task (tracking num: 4)
t=77.1     Disagg mv alng: sub 1141.MechPlt completed move along task (tracking num: 3)
t=77.7     Disagg mv alng: sub 1143.MechPlt completed move along task (tracking num: 2)
t=77.7 [2] move-along-controller's task has Completed (ID=0)
```
Each declared platoon's console shows the vendor's UG52 25.2 mechanism exactly: at t=29.0 a
move-into-formation (its own formation: Wedge-Right / Column-Left / column) which Completes
(42.1 / 51.3 / 45.9); at t=51.3 a move-along on a company-generated OFFSET ROUTE named
`114.MechCoy_R0` / `_R1` / `_R2` (the "routes ... offset from the leader's route" of 25.2),
executed by the platoon's maneuver-along-controller (unitRoute=114.MechCoy_Rn,
startAtClosest=True), Completed at 77.1 / 75.7 / 77.7. App: 3/3 `VRF task complete` + 3/3
TASKCMPLT by t=78 (V baseline reproduced; instrument inert on the control as well). The only
level-1 rows in the whole run are the members' "Making pivot geometry" path-start chatter.

VERDICT (both directions of the gate hypothesis observed on the vendor's own channel):
- The higher-unit move-along = [move-into-formation at the current location, gated on a
  completion message from EVERY subordinate] -> [offset route per subordinate, leader-relative]
  -> [complete when every subordinate completes]. Not our code, not our create call.
- Template company (T): one HQ-section HMMWV never completes its slot move -> gate never opens
  -> no route move (C1b). Composed company (C): all declared subordinates complete -> gate opens
  -> offset routes -> complete.
- The remaining unknown (why the M998's generated move-along never ends) is the vendor
  template's behavior and is not on our path: compose/expand never instantiate that HQ section.

Instrument caveats found in T (recorded for the next slice, not fixed in this one):
- The app's `ObjectConsoleMessage` callback is PARTIAL by an unknown rule: in T it delivered
  53 lines, all for the three objects this controller created (1222.MechPlt 38, 114.MechCoy 9,
  1.BdeHQ 6) and none of the members' 170k; in C 279 lines (the three units + the three
  declared platoons); in D (no level requests) 24 level-1 lines FROM MEMBER ENTITIES the app
  did not create. So it is not "own objects only" and not a level filter. WatchVrf's `CON` rows
  are the complete channel; the app log is a convenience. Not pursued.
- The app logs the raw DtRwTranslatableStringObject XML (first line only visible per log
  line). Decode `<string>` parts before logging (LIGHT follow-up).
- Some rows' level field carries a sim-time prefix (e.g. 4180 = level 4 at 180.7 s of the
  message text); treat level > 4 as 4.
- `-BackendNotifyLevel 4` added nothing decision-relevant to the vendor log for this question;
  the object console is the channel. (The vendor log still holds the environment in
  cleartext - not quoted.)
