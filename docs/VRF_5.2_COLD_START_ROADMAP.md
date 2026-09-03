<!-- Verbatim output of a clean-room Opus agent (2026-09-03) given only the requirements, the 5.2d install, the vendor docs and the C2SIM SDK; blind to this repo. Adjudicated in VRF_5.2_COLD_START_MAP.md; do not treat its claims as settled. -->
# C2SIM to MAK VR-Forces 5.2d Headless Interface: Cold-Start Roadmap

Author: senior simulation-integration engineer, clean-room pass. Date: 2026-09-03. Platform:
Windows 11 Pro.

## Citation conventions and evidence classes

Citations are one of:
  * UG  = VR-Forces User Guide 5.2, plain-text extract at
    ./scratchpad/docs52/VRFUsersGuide52.txt. Cited as "UG sec, p PDFpage (extract line N)".
  * MG  = VR-Forces User Migration Guide, ./scratchpad/docs52/VRFMigrationGuide.txt
  * AMG = VR-Forces 5.2 API Migration Guide, ./scratchpad/docs52/vrf_migration52.txt
  * RN  = VR-Forces 5.2 Release Notes, ./scratchpad/docs52/VRF52ReleaseNotes.txt
  * IG  = MAK ONE Interoperability Guide, ./scratchpad/docs52/MAKInteroperabilityGuide.txt
  * DISK = a file I opened or listed under C:\MAK (read-only, this session)

Evidence class is marked inline:
  [V] VERIFIED - I read the quoted text or listed the file myself this session.
  [I] INFERRED - a conclusion I drew; the vendor does not state it.
  [U] UNKNOWN  - only a run can settle it (see section 5).

--------------------------------------------------------------------------

## 1. The vendor's grain: what MAK built 5.2 to do

### 1.a Tasking ground entities and units

MAK rewrote ground movement in 5.2 and moved the primitive from a C++ controller into a
Lua-scripted task. This is the single most important fact in this document, because it
invalidates the naive port of any pre-5.2 tasking code.

> "Movement to a point is now performed by a Move To task, implemented as Lua script. The
> old tasks move-to-waypoint, move-to-location, move-to-waypoint (plan along roads), and
> move-to-location (plan along roads) are no longer available on the Task menu or in the
> Command Panel." [V]
> -- MG 2.4, p 18 (extract line 632ff)

> "Existing Lua scripts, plugins, and remote-control applications that issue movement tasks
> to ground vehicles may need to be updated." [V]
> -- AMG, section "Ground Vehicle Movement" (vrf_migration52.txt)

The vendor's own 5.1.1 -> 5.2 task-ID mapping [V] (MG 2.4.1 Table 1, p 19-20, extract lines
683-760): move-to-location, move-to-location-path-plan, move-to and
move-to-waypoint-path-plan all collapse to task ID `ground-vehicle-move-to` (GUI name "Move
To"), with the note "Set Navigation Preferences to 'Prefer Roads'" where road following is
wanted. For units the 5.2 answer is "move-to or maneuver-to"; move-along stays move-along.
MG 2.4 also states plainly that "There are extensive changes to the ground vehicle movement
system ... in some cases may produce different results than seen in previous releases" [V].

Cohesion of a company is a task-selection question, not a configuration question. The vendor
states the difference outright:

> "When a unit performs a Move To task: Each subordinate plans and traverses its own path to
> an 'offset destination', without consideration of the others." [V] -- UG 30.28 Move To, p
> 604 (extract line 24651)

> "This task causes a ground vehicle unit to move to a destination in a formation offset
> from each other. The subordinates in the unit move to their offset destinations and
> attempt to stay together. ... There are a leader and followers, which use speed control to
> remain within some distance of each other as they move to the specified destination." [V]
> -- UG 30.23 Maneuver To, p 599 (extract line 24384)

Behind both, VR-Forces models the unit itself: it computes the leader's path and then offset
destinations and routes for each subordinate [V, UG 25.2 How Units Move, p 517, extract line
21095], and a tasked unit "sends radio messages to its subordinates directing them to carry
out their role in the task", a mechanism the vendor describes as "built into the VR-Forces
application" [V, UG 25.3, p 518, extract line 21152].

Grain: task the UNIT, never its members; use Maneuver To / Maneuver Along when the company
must move cohesively, Move To / Move Along Route when independent movement is correct. Do
not re-implement formation keeping outside VR-Forces.

### 1.b HLA / federation configuration

> "all MAK ONE products are link compatible with the MAK RTI and other RTIs that support the
> HLA Evolved (IEEE 1516-2010) or HLA 4 (IEEE 1516-2025) specifications, and which are built
> with the same compilers as the MAK ONE product you are using." [V] -- IG 1.3, p 10
> (extract line 438)

> "VR-Forces now uses the NATO Education and Training Network (NETN) FOM with custom MAK
> extensions. This FOM is based on RPR FOM 2.0 ... However, the NETN FOM is now required by
> VR-Forces for some of the additional attributes it adds on top of RPR." [V] -- MG 3.1 FOM
> Support, p 24 (extract line 802)

> "When you connect to a simulation, the application uses the new exercise connection
> configuration (exConnConfig) file, MAK-ONE-YYYY-Config.xml, which specifies the basic set
> of connection settings." [V] -- RN, Configuration and Startup, p 3 (extract line ~118)

DISK [V]: C:\MAK\vrforces5.2d\appData\settings\connections\MAK-ONE-2025-Config.xml declares
execName "MAK-ONE-2025", fedFileName RPR_FOM_v2.0_1516-2010.xml, rprFomVersion 2.0,
netnFomVersion 3.0, and a 17-module FOM list that already includes NETN-BASE.xml,
NETN-ETR.xml, NETN-Physical.xml, NETN-MRM.xml and MAK-VRFExt-12_evolved.xml.

DISK [V]: C:\MAK\vrforces5.2d\bin64 ships vrfSimDIS.exe, vrfSimHLA1516e.exe and
vrfSimHLA4.exe. C:\MAK\vrforces5.0.2\bin64 ships vrfSimHLA13.exe and vrfSimHLA1516.exe,
which 5.2d does not. MAK deliberately dropped the HLA 1.3 and 1516-2000 sim engines and
added HLA 4.

DISK [V]: C:\MAK\makRti4.6.1\lib and C:\MAK\makRti4.6b\lib each contain librti1516e64.lib,
librti1516_64.lib and libRTI-NG_64.lib - HLA Evolved, 1516-2000 and 1.3 respectively -
and nothing named for 1516-2025 or HLA 4; C:\MAK\vrforces5.2d\bin64 ships no RTI library of
its own. RN adds that "The MAK RTI HLA 4 libraries
are built using the Video Studio 2017 compiler and use the vc141 compiler marker" and that
SISO-REF-075 naming "prevents HLA applications from loading binary files from non-compatible
compilers" [V]. Conclusion: vrfSimHLA4.exe has no RTI to bind to on this machine. [I]

### 1.c Terrain

> "MAK Earth is the primary terrain for use in ground or air applications. ...
> High-resolution elevation includes global 30m, 10m for all of California, and 1m for Camp
> Pendleton area plus high resolution insets for many other areas." [V] -- DISK, header
> comment of C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\MAK Earth
> (online).earth

DISK [V]: that same file pulls elevation and imagery through `{% include
elevation.worldwide.online.xml %}` and `{% include imagery.worldwide.online.xml %}` plus
TMSImage layers at http://vr-theworld.com. "NTC (online).earth" mixes an online worldwide
elevation layer (http://vr-theworld.com/vr-theworld/tiles/1.0.0/149/) with a local
../Terrain/California/Elevation/NTCRazishBerm.tif.

DISK [V]: purely local, offline coverage of the assigned box exists:
Terrain\California\Elevation\N34W117.dem (27,264,000 bytes) and
Terrain\California\Imagery\N34W117.tif. A 1-degree tile named N34W117 covers
34-35 N, 116-117 W, which contains the whole assigned operating area
(34.3-34.7 N, 117.0-116.4 W). [V for the files; I for the tile convention]

DISK [V]: no shipped .earth references N34W117.dem (`grep -l N34W117 *.earth` returned
nothing). Using it offline requires a custom .earth + .mtf pair. [I]

DISK [V]: prebuilt navigation data exists only for Ala Moana, Kilo2, Range220 and Thun under
SharedData\19\latest\TerrainData\navData\"MAK Earth (online)". Nothing covers 34.3-34.7 N /
117.0-116.4 W.

> "If there is no navigation mesh available from the start of the entity's movement path to
> the end, the Move To script accounts for feature obstacles and plans the entity's path to
> avoid them. The script also has some ability to dynamically replan paths around blockages"
> [V] -- MG 2.4, p 18

So 5.2's design intent is that ground movement degrades gracefully without a nav mesh -
which is exactly the condition the operating area puts us in.

### 1.d Headless, batch and remote-control use

Four vendor-blessed mechanisms exist. They are not interchangeable.

(1) Batch mode - unattended, seeded, read-only:

> "Batch mode allows you to run one or more scenarios multiple times, without direct action
> through the graphical user interface. A scenario run in batch mode runs with the
> parameters in the scenario file, except that you can override the random number seed.
> Batch mode is read-only. You cannot save a scenario, create simulation objects or objects,
> pause the scenario, or otherwise change it, while a batch is running." [V]
> -- UG 7.10, p 269 (extract line 12025)

> "To run a batch from the command line, use the (--batchScenarioFileName | -B) parameter,
> with the sim engine in separate mode, for example: vrfSimDIS --siteId 1 --appNumber 3001
> --batchScenarioFileName "../userData/scenarios/Sample/sampleScenarioBatch.bsn"" [V]
> -- UG 7.10.4, p 272 (extract line 12206)

The .bsn file is MTL and carries number-of-runs, random-number-seed (with -1 meaning
computer-generated), scenario-filename, sms-filename and either simulation-run-duration (sim
time) or run-duration (wall time) [V]
-- UG 7.10.3 Table 15, p 269-272.

(2) Independent sim engine from the command line, no GUI, no Launcher:

> "To start independent VR-Forces executables from the command line: ... Enter a command for
> each VR-Forces executable you want to run. You must specify the protocol to use. For
> vrfSim, the protocol is part of the executable name. vrfSimprotocol --siteId 1 --appNumber
> 3001 options where protocol is DIS, HLA1516e, or HLA4." [V]
> -- UG 4.1.2, p 132-133 (extract line 6276)

vrfSim's own option set carries everything a headless run needs [V]
-- UG 5.2, p 178-186 (extract line 8357): `-L/--scenarioFileName`,
`-T/--terrainDatabase`, `-r/--startInRunMode`, `-B/--batchScenarioFileName`,
`--simulationModelSet`, `--exConnConfigFile`, `-q/--doNotUseConsole`, `--logFileName`,
`--fileNotifyLevel`, `-n/--notifyLevel`, `--startMinimized`, `--siteId`, `--appNumber`,
`-i/--sessionId`, `--exitOnTestResultCommand`, `--connectionless`, and HLA-only
`--execName`, `--federateName`, `--fedFileName`, `--fomModules`, `--setFomModuleList`,
`--timeManagement`.

(3) Remote Control API - an external process drives a running sim engine:

> "The Remote Control API allows you to control a VR-Forces simulation engine from a remote
> application, such as a graphical user interface (GUI)." [V]
> -- UG 1.2, p 87-88 (extract line 4348)

> "Users can manage missions with the global and unit plans, through player interaction, or
> through externally developed applications that use the remote control API." [V] -- UG
> 27.6, p 543 (extract line 22215)

DISK [V]: the transport is not a private socket - it is an interaction class in MAK's own
FOM extension. C:\MAK\vrforces5.2d\bin64\MAK-VRFExt-12_evolved.xml declares interactionClass
"MtlCommand" with one parameter "MtlCommandPdu" (fixedRecordData: header, senderID,
receiverID, pduStamp, pduMember, ofTotal, commandLength, command). The same module declares
ETR_Root and ETR_Report. Remote control therefore rides the same federation as entity
traffic. [V]

DISK [V]: the shipped sample is C:\MAK\vrforces5.2d\examples\remoteControl (main.cxx,
commandLineRemoteController.cxx/.h, remoteControlInit.cxx/.h), with prebuilt
remoteControlDIS.exe, remoteControlHLA1516e.exe and remoteControlHLA4.exe under
build64\RelWithDebInfo. Its loop constructs a `makVrf::DtVrlinkVrfRemoteController`, then a
`DtExerciseConn` from a `DtRemoteControlInitializer`, calls `remoteController->init(exConn,
nullptr, nullptr, nullptr, nullptr, "entity-identifier", false)`,
`setMonitorBackendState(true)`, `communicationManager()->run()`, and then polls
`remoteController->tick()`. Note the comment "Instantiate the remote controller before the
DtExerciseConn to avoid requiring a VR-Link license."

(4) NETN ETR over HLA - standards-based tasking, but a narrow subset:

> "VR-Forces uses the NETN FOM by default, but it only uses a small number of these classes
> and attributes. ... Supported classes: ... MagicMove (ETR), MagicResource (ETR),
> MoveToLocation (ETR), FireAtEntity (ETR), FireAtLocation (ETR), Mount (ETR), Dismount
> (ETR), SetOrderedAltitude (ETR), SetOrderedSpeed (ETR), SetRulesOfEngagement (ETR)" [V]
> -- IG 2.3 NETN FOM Support, p 27-28 (extract line 1283)

That list contains no route following, no patrol, no unit-cohesion task and no object
creation. [I]

### 1.e Simulation model sets and extending them

> "If you have a custom Simulation Model Set (SMS), you must upgrade this before using it in
> a newer version of VR-Forces. ... Entity files (.entity) can generally be copied directly
> to a new version of VR-Forces. System files (.sysdef) and Platform files (.ope) cannot be
> used directly in a new [version]" [V] -- MG 1.2.1, p 12 (extract line 396)

> "Resources are now represented by their SISO enumeration rather than a string. Fuel
> resource quantities are now represented by real world units (liters or kilograms). You
> must update any custom entity-level SMSs to the new resource format for scenarios using
> those SMS to work correctly." [V] -- MG 2.2.3, p 16

> "The ground movement systems have changed significantly. The collision avoidance and
> obstruction sensors are no longer used. If you modified any of the ground movement systems
> (tracked, off-road, amphibious), you must inspect the new ground movement systems to see
> what is configured differently" [V]
> -- MG 2.4.2, p 20

DISK [V]: C:\MAK\vrforces5.2d\data\simulationModelSets contains base, EntityLevel,
AggregateLevelBase, AggregateLevel, AggregateTacticalLevel and MAKTest, each as a directory
plus a .sms file. EntityLevel\vrfSim holds 1723 files. Preconfigured US and Russian units
ship by name, e.g. "Tank Company (USA).entity", "Tank Company (RUS).entity", "Mechanized
Company (US Army M2).entity", "Mechanized Company (RUS).entity",
"Field Artillery Platoon (RUS 2S1).entity", "Air Defense Artillery Platoon (RUS).entity" and (USA),
"Motorized Squad (Irregular Technical).entity", "Rifle Squad (Insurgent).entity", plus
patrol boats and a Rigid-Hulled_Inflatable_Boat.

DISK [V]: Chinese PLATFORMS ship - "Type 96 (ZTZ96) MBT.entity", "Type_99_MBT.entity", the
HQ-9 / HQ-16A / HQ-2 SAM families, "Chengdu J-10C", "Type 052D Luyang III Class Destroyer" -
but no Chinese UNIT template: there is no "(CHN)" or "(PRC)" unit .entity in the SMS. A PLA
order of battle must be composed by us. [V]

Grain: MAK expects extension by ADDING .entity files and layering SMSs (EntityLevel layers
on base; AggregateLevel on AggregateLevelBase), not by forking their SMS.

--------------------------------------------------------------------------

## 2. Architecture recommendation

### 2.1 The five candidate control paths, evaluated

**A. Remote Control API in C++ (vrfcontrol / DtVrfRemoteController).** Covers the whole job:
  create objects and aggregates, assign plans, send tasks, run/pause, set time multiplier,
  snapshot/rollback, and - uniquely - read back execution status. DISK [V],
  C:\MAK\vrforces5.2d\include\vrfcontrol\ vrfRemoteController.h (2430 lines) exposes named
  sections "V. Object Management", "VI. Tasks", "VII. Plans - A/B", "IX. Simulation Control
  and Information Request", "XII. Simulation Status". Concretely: createEntity,
  createAggregate, createObject, sendTaskMsg(recipient, DtSimTask*, addr), convenience
  moveToLocation / moveAlongRoute / patrolAlongRoute / followEntity / waitDuration,
  assignPlanByName, subscribePlan, addPlanStatementCallback, addPlanCompleteCallback,
  requestTasksAndSetsFor, monitorResources, loadScenario, run, pause, setTimeMultiplier,
  rollbackToSnapshot, simulationStatus. Cost: C++ only, and it speaks MAK's private MTL
  command language on the wire. Risk: the 5.2 ground-movement rewrite (section 1.a) means
  the convenience moveToLocation may no longer resolve to the ground-vehicle-move-to task.
  [U]

**B. VR-Link C# (vrLinkSharp).** DISK [V]: C:\MAK\vrlink5.10\C-Sharp exists with examples
  f18, listen, talk, laserDesignator; C:\MAK\vrlink5.10\bin64 and C:\MAK\vrforces5.2d\bin64
  both ship vrLinkSharp.dll plus managedInterface{DIS,HLA13,HLA1516,HLA1516e,HLA4}.dll. A
  symbol probe of vrLinkSharp.dll [V] shows EntityPublisher, EntityStateRepository,
  ReflectedEntityList, ReflectedEmitterList, ReflectedTransmitterList and
  UniversalHlaInteraction. What that buys: a fully managed, HLA-native way to (i) reflect
  ground-truth entity state - position, orientation, damage, force - for telemetry, and (ii)
  send and receive arbitrary FOM interactions by class name, which in principle includes
  NETN-ETR tasking and MAK's own MtlCommand. What it does not buy: any VR-Forces object
  model. C:\MAK\vrforces5.2d has no C-Sharp directory and bin64's only managed assemblies are
  the VR-Link ones above [V], so there is no managed binding for vrfcontrol [I]. Creating a
  "Tank
  Company (USA)" unit is a VR-Forces concept, not a VR-Link one.

**C. NETN-ETR tasking over HLA.** Standards-clean and language-neutral, and the default
  federation already loads NETN-ETR.xml [V]. But VR-Forces implements only MagicMove,
  MagicResource, MoveToLocation, FireAtEntity, FireAtLocation, Mount, Dismount,
  SetOrderedAltitude, SetOrderedSpeed and SetRulesOfEngagement (IG 2.3, p 27-28) [V]. DISK
  [V]: NETN-ETR.xml itself also defines Move, MoveToEntity, MoveIntoFormation, patrol move
  types, CancelAllTasks, TaskStatusReport and PositionStatusReport with TaskStatusEnum32
  (Accepted / Completed) - but none of those are on MAK's supported list. So C2SIM
  move-along-route, patrol, and any ETR task-status feedback have no carrier here. [I] ETR
  alone cannot satisfy the requirements; it is a useful secondary channel, not the primary
  one.

**D. Generate scenario files and run batch mode.** Vendor-blessed for exactly our use case:
  seeded, unattended, repeatable, N runs per scenario (UG 7.10, p 269-272) [V]. But "Batch
  mode is read-only. You cannot save a scenario, create simulation objects or objects, pause
  the scenario, or otherwise change it, while a batch is running" [V]. A C2SIM Order
  arriving mid-run cannot be injected. Batch mode is the right engine for replay and A/B
  comparison of a fixed order set, not for live C2SIM.

**E. Lua scripts inside the SMS.** This is where MAK put ground movement in 5.2 [V], so it
  is the natural place to add derived behaviour. But scripts run inside the sim engine and
  are driven by tasks; they are not a network ingress. Right tool for "make
  ground-vehicle-move-to do X", wrong tool for "receive a C2SIM Order".

### 2.2 Recommendation: two planes, C# on top, one thin C++ shim

Pick A as the control plane and B as the telemetry plane, with D as the regression harness.

    C2SIM server (STOMP notifications / REST push)
          |   HyssosTech.Sdk.C2SIM  (C#, .NET 8)
          v
    +-------------------------------+       machine-readable evidence
    |  C2SIM Adapter (C#)           |-----> run manifest + per-order JSON
    |  - parse/validate messages    |       trace: orderId -> taskId ->
    |  - unit and type resolution   |       status -> simTime -> geometry
    |  - Order -> VR-Forces plan    |
    +--------+--------------+-------+
             |              ^
    localhost IPC           |  reflected entity state (position, damage,
    (named pipe or gRPC)    |  force) via vrLinkSharp ReflectedEntityList
             v              |             [plane B, pure C#]
    +---------------------------+     HLA 1516e / MAK RTI 4.6.1
    |  VrfControl shim (C++)    |<=== federation C2SIM-<runid> ===>
    |  wraps DtVrfRemoteController                      vrfSimHLA1516e.exe
    |  create / plan / task / run / status callbacks     (no GUI at all)
    +---------------------------+

Why this split rather than all-C++ or all-C#:
  * The assignment mandates C# where possible. Everything except the DtVrfRemoteController
    binding can be C#.
  * The one thing only C++ can do is speak the VR-Forces object/plan/task model. That shim
    is small and stable - create object, assign plan, send task, run/pause, plus three
    callbacks (plan statement, plan complete, simulation status). It is a direct derivative
    of examples\remoteControl\main.cxx [V].
  * Telemetry does not need the shim. vrLinkSharp reflects the same federation directly, so
    position and health evidence is produced in C#, independently of the control path. Two
    independent observers of one run is what "machine-verifiable" should mean: the control
    plane asserts "plan complete", the telemetry plane asserts "the entity was within R
    metres of the ordered point at sim time T". Neither alone is proof. [I]

### 2.3 Concrete configuration decisions

**Executable and protocol.** vrfSimHLA1516e.exe, no vrfGui, no vrfLauncher, one sim engine
  per run, launched directly per UG 4.1.2 [V]. HLA is preferred by the assignment; HLA 4 is
  unusable here because neither installed MAK RTI ships 1516-2025 libraries [V for the lib
  listing, I for the conclusion; section 1.b]. HLA Evolved on MAK RTI 4.6.1 is the only
  installable HLA option. Fallback: vrfSimDIS.exe. DIS is genuinely simpler (no rtiexec, no
  FED file, no join race) and MAK still ships and documents it fully. I would not choose it
  as primary, because both the remote-control MtlCommand and NETN ETR have first-class HLA
  carriers and the assignment prefers HLA - but keep DIS as a diagnostic instrument: a
  symptom that reproduces on DIS is not an RTI problem.

**Exercise connection.** Ship our own exConnConfig XML and pass --exConnConfigFile
  runs\<runid>\exconn.xml to every federate [V, IG 1.8.2, p 20]. Copy
  MAK-ONE-2025-Config.xml, change only execName to a per-run value (e.g. C2SIM-<runid>), and
  keep the 17-module FOM list verbatim. Vendor warning: "The MAK-ONE-YYYY-Config.xml file is
  always loaded to ensure that no parameters are missing" [V] - our file overrides, it does
  not replace. Run the rtiexec: "If you want to use advanced features such as DDM, Time
  Management, or MOM, you must run the rtiexec. Run only one instance of the rtiexec per
  federation. Always run different federations on different UDP ports." [V, IG 1.3, p 11]

**Terrain.** Build one custom terrain, C2SIM-Mojave.earth plus its .mtf, composed only from
  the LOCAL sources Terrain\California\Elevation\N34W117.dem and
  Terrain\California\Imagery\N34W117.tif [V, both present on disk]. Do not use "MAK Earth
  (online)" or "NTC (online)" for scored runs: both stream tiles from http://vr-theworld.com
  [V], which makes a run depend on a network and on a tile cache whose warm/cold state
  differs between runs. Keep MAK Earth (online) for eyeball checks only.

**Simulation model set.** EntityLevel.sms [V]. "A company is tasked as a unit and its
  subordinates move cohesively" is an entity-level unit requirement: units with real
  subordinates live in EntityLevel, while the aggregate SMSs model a company as a single
  aggregate object. Add our content as a thin SMS that includes EntityLevel rather than
  copying it (MG 1.2.2, "Copying versus Including VR-Forces SMS") [V].

**Unit creation and tasking.**
  * Resolve each C2SIM unit to a VR-Forces object type by SISO/DIS entity type first, with a
    curated name map as fallback. The catalogue is on disk as 1723 .entity files [V]; that
    map is data, not code, and belongs in version control next to the adapter.
  * Create the company by pre-placing it in a generated scenario (.scn) where possible - the
    laydown is then reproducible and diffable - and by createAggregate / createEntity only
    for units that arrive after start.
  * Task the unit object, never the members (UG 25.2 / 25.3) [V].
  * C2SIM move-to-location on a company -> VR-Forces Maneuver To (maneuver-to), not Move To:
    Move To makes each subordinate plan independently (UG 30.28) [V] while Maneuver To keeps
    formation and uses leader/follower speed control (UG 30.23) [V].
  * C2SIM move-along-route on a company -> build a route control object from the C2SIM
    control points, then Maneuver Along (UG 30.22) [V]. On a single entity -> Move Along
    Route (UG 30.24) [V], noting the vendor trap "If you select Treat Route as Road, the
    entity must be within 10 meters of the route or the task will fail" [V].
  * C2SIM patrol -> Patrol Route / Patrol Between (UG 30.36-30.38) [V].
  * Road versus cross-country is a set data request, not a task: "You can use the Navigation
    Preferences set data request to specify whether an entity will prefer to use or ignore
    roads. By default, vehicles that use the tracked or wheels-off-road movement systems
    ignore roads, while vehicles that use the wheels-road movement system prefer roads." [V,
    MG 2.4, p 18]
  * Before sending anything, call requestTasksAndSetsFor(names) [V, vrfRemoteController.h]
    and assert the task ID we are about to send appears in the response. That turns "a 5.2
    rename broke us" from a silent no-op into a startup failure.

**Reading completion and status back.** The Tasks section of DtVrfRemoteController has no
  completion callback - only skipTask; completion callbacks live in the Plans section [V,
  vrfRemoteController.h lines 1624-1826]. Therefore:
  * Wrap every C2SIM Order as a plan, even a one-task order: assignPlanByName(uuid, plan),
    then subscribePlan(uuid, cb, usr), addPlanStatementCallback and addPlanCompleteCallback.
    The statement callback delivers a DtPlanStatus; the complete callback fires once. That
    is the authoritative machine-readable "the order finished" signal.
  * Cross-check with plane B: sample reflected entity position at fixed cadence and assert
    the geometric predicate the order implies - arrived within R of the destination; never
    left the route corridor by more than D; company diameter stayed under C for cohesion.
  * Also capture object console messages (addObjectConsoleMessageCallback,
    logObjectConsoleToFile) [V], monitorResources for ammunition and fuel [V], and the sim
    engine log via --logFileName plus --fileNotifyLevel [V, UG 5.2].
  * Emit C2SIM Position Reports and Task-Status Reports back to the C2SIM server from the C#
    adapter with IC2SIMSDK.PushReportMessage(xml) [V, C2SIMSDK/IC2SIMSDK.cs].

**Time and frame mode for repeatability.** Set in the scenario file [V, UG 12.2.1, p
  351-353]: (frame-mode "fixed-frame-run-to-complete"), (frame-time 0.05) or 0.1,
  (random-number-seed <fixed>), (time-multiplier 1.0). The vendor's own words:
> "Variable-Frame Run-To-Complete mode advances simulation time by the amount of time passed
> since the last time the exercise clock was ticked. ... It does not provide repeatable
> results." [V] -- UG 3.4.3, p 122 (extract line 5847) "Fixed-Frame Run-To-Complete mode ...
> is most useful for situations where you want a simulation to run with internal consistency
> and high fidelity, and want it to run to completion, but do not need to observe the
> simulation. ... It disables the Simulation Time Scale Toolbar." [V] -- same section
Also: "A value of zero for frame time prevents simulation time from advancing in either of
these modes." [V, UG 12.2.1 Table 20]

**Unattended launch and teardown.** No Launcher. Sequence:
  1. Assert the license server is reachable; inventory processes and refuse to start if a
     stale vrfSim* or rtiexec from a previous run survives.
  2. Start rtiexec for this run's federation on a per-run UDP port.
  3. Start vrfSimHLA1516e.exe --exConnConfigFile ... --simulationModelSet ... -T
     <terrain.mtf> --siteId N --appNumber 3001 -i <sessionId> --logFileName
     runs\<id>\vrfSim.log --fileNotifyLevel 3 -q --startMinimized (-q sends output to the
     log rather than the console, which UG 5.2 notes also avoids a performance hit at high
     notify levels [V]).
  4. Start the C++ shim and the C# adapter; wait for the shim's addBackendDiscoveryCallback
     / addBackendLoadedCallback before declaring READY. Do not gate on "process exists" or
     "TCP port open".
  5. Load or push the scenario, then run().
  6. Teardown: pause(), close the scenario, stop the shim, terminate vrfSim, terminate
     rtiexec, then assert zero surviving processes. A failed teardown is a failed run, not a
     warning.
For the fixed-order regression suite, prefer the vendor path outright: vrfSimHLA1516e
--batchScenarioFileName run.bsn with number-of-runs and an explicit random-number-seed [V].
That needs no shim and no GUI at all.

--------------------------------------------------------------------------

## 3. Roadmap

### What to prototype FIRST, and why

Prototype zero writes no code at all. MAK already ships a built remote controller:
C:\MAK\vrforces5.2d\examples\remoteControl\build64\RelWithDebInfo\ remoteControlHLA1516e.exe
[V], whose command surface includes `create tank`, `create route`, `create aggregate`, `task
moveToPoint`, `task patrolRoute`, `task follow`, `set plan`, `run`, `pause`, `simrun`,
`timemultiplier`, `monitor`, `takesnapshot`, `rollback`, `simulationStatus` and, under
DtHLA, `etrfireat` [V, commandLineRemoteController.h].

Run it against a headless vrfSimHLA1516e.exe on a shipped scenario and answer, in one
afternoon and with no build system: does the sim engine join, does an external process
create and task an object, does a ground vehicle actually move, and does the
plan-statement/plan-complete callback fire. Every later phase inherits its de-risking from
that answer. Building our own shim before running MAK's own shim would be building on an
unverified assumption.

### Phase 0 - Environment truth (no VR-Forces yet)

Entry: nothing. Exit: a written, dated inventory that records license server reachability
and expiry, the exact vrf/vrv/RTI binaries present, RTI HLA version coverage, makData
version, and a clean process baseline. Key risk: an expiring or node-locked license silently
gating the whole toolchain. De-risked by: RN "License Manager" p 2 and the MAK ONE
Installation Guide [V for RN].

### Phase 1 - Headless sim engine boots and joins, unattended

Entry: Phase 0. Exit: `vrfSimHLA1516e.exe --exConnConfigFile <ours>
--simulationModelSet EntityLevel.sms -T <terrain> -q --logFileName ...` reaches
a joined, scenario-loaded state with zero GUI processes, and a scripted teardown returns the
machine to the Phase 0 process baseline, twice in a row. Key risk: the RTI join. IG 1.3 p
10-11 is explicit that every federate needs the RTI on PATH, a license per federate, one
rtiexec per federation, and distinct UDP ports per federation [V]. Second risk: the
Launcher's stored network address. UG 5.3.1 warns "You must launch a predefined connection
from the vrfLauncher at least once so that VR-Forces can save the network address
information it requires to launch" [V] - which is why we bypass the Launcher entirely and
use UG 4.1.2 direct invocation [V].

### Phase 2 - External control proves a ground vehicle moves (the prototype)

Entry: Phase 1. Exit: MAK's own remoteControlHLA1516e.exe creates an object, tasks it, the
object's position changes in the log, and the run tears down. Then the same achieved by our
own minimal C++ shim built from examples\remoteControl. Key risk: the 5.2 ground-movement
rewrite. De-risked by MG 2.4 and 2.4.1 Table 1 (p 18-20) [V] and AMG "Ground Vehicle
Movement" [V]; operationally de-risked by calling requestTasksAndSetsFor before tasking [V].

### Phase 3 - Telemetry plane in C#

Entry: Phase 2. Exit: a C# process using vrLinkSharp joins the same federation read-only and
writes a time-stamped position/damage track for every entity, and the track agrees with the
sim engine log on start and end positions. Key risk: whether the managed binding exposes
enough of the reflected entity list and whether it needs the same FOM module list. De-risked
by C:\MAK\vrlink5.10\C-Sharp\examples (listen is the exact shape needed) [V] and IG 1.8 on
exConnConfig [V].

### Phase 4 - Order compiler: C2SIM Order to VR-Forces plan

Entry: Phases 2 and 3. Exit: one hand-written C2SIM Order XML, delivered through the C2SIM
SDK, produces a plan on a single entity, and the run emits a JSON trace containing orderId,
taskId, plan status transitions, sim times and the geometric assertion result. Key risk:
semantic mismatch between C2SIM task vocabulary and the 5.2 task set. De-risked by UG
chapters 29-34 (Assigning Tasks; Movement, Engagement, Unit Behavior, Embarkation/Wait/Radio
tasks) [V, chapter list] and by the C2SIM schemas in C2SIMSDK\schemas [V].

### Phase 5 - Units, companies and fidelity mapping

Entry: Phase 4. Exit: a US tank company and a Russian mechanized company are created from
Initialization messages, tasked as units, and the telemetry plane shows subordinate spread
below a stated cohesion threshold under Maneuver To, and above it under Move To (the control
case that proves the mapping matters). Key risk: no Chinese unit templates ship [V]; the PLA
order of battle must be authored. De-risked by UG chapters 68-72 (Simulation Object Editor
and SMSs; Editing Units for Entity-Level Scenarios) [V, chapter list] and MG 1.2 on SMS
upgrade rules [V].

### Phase 6 - Repeatability and A/B comparison

Entry: Phase 5. Exit: the same order set run twice with the same seed produces traces that
match within a stated tolerance, and run-to-run variation is reported as a number, not an
impression. Both a live path (shim) and a batch path (.bsn with number-of-runs 2 and a fixed
random-number-seed) are exercised. Key risk: hidden nondeterminism - variable-frame clock,
wall-clock-coupled components, network jitter, online terrain tiles. De-risked by UG 3.4.3 p
122 [V], UG 12.2.1 Table 20 [V], UG 7.10.3 Table 15 [V], and by choosing a fully local
terrain (section 2.3).

### Phase 7 - One button

Entry: Phase 6. Exit: a single command takes a directory of C2SIM messages and returns a
pass/fail verdict plus evidence, with no human in the loop, and a deliberately induced
failure (unreachable RTI, missing terrain, unknown unit type) is reported as a failure
rather than as a hang. Key risk: teardown. It is the step most likely to leave the machine
unusable for the next run; treat "processes at exit equals processes at entry" as a
first-class exit criterion.

--------------------------------------------------------------------------

## 4. Decisions the owner must make

1. **HLA Evolved now, or wait for MAK RTI 5.x to get HLA 4?** Recommendation: HLA Evolved
   (1516-2010) on MAK RTI 4.6.1 now. HLA 4 support is the headline 5.2 feature [V, RN p 3]
   but no installed RTI can serve it [V]. Design the exConnConfig and FOM module list so
   switching later is a config change, not a code change.

2. **Live control plane, batch harness, or both?** Recommendation: both, and say which one a
   given result came from. Live (remote control) is required by "receives C2SIM messages ...
   and tasks them". Batch is required by "compare two runs of the same order" and is the
   only path the vendor documents as unattended and seeded [V, UG 7.10].

3. **Custom offline terrain, or MAK Earth (online)?** Recommendation: custom offline .earth
   over N34W117.dem/.tif for all scored runs [V]. Cost: one terrain build and a correlation
   check. Benefit: runs stop depending on vr-theworld.com.

4. **Navigation mesh for the operating area: generate, or rely on the Move To script's
   obstacle planning?** Recommendation: start without one - MG 2.4 says the script plans
   around feature obstacles when no mesh exists [V] - and generate only if Phase 2 shows
   unacceptable paths. Note UG 3.6 states nav-data generation needs a purchased license [V];
   confirm entitlement before planning on it.

5. **Chinese forces: author PLA unit templates, or use Russian units as a stand-in?**
   Recommendation: author them. The platforms exist (Type 96, Type 99, HQ-9, HQ-16A, J-10C)
   [V]; only the unit groupings are missing. A stand-in silently changes the fidelity of
   every result that involves them.

6. **Where the C2SIM-to-VR-Forces type map lives.** Recommendation: a version-controlled
   data file (CSV or JSON) keyed on SISO entity type with a name fallback, loaded at
   startup, validated against the SMS at startup. Not code, and not scattered through the
   adapter.

7. **Report cadence and content pushed back to the C2SIM server.** Recommendation: position
   reports at a fixed sim-time interval plus task-status reports on plan-status transitions
   only. Event-driven status plus periodic position is the least surprising contract for a
   C2 client.

8. **Does the deliverable include the C++ shim as a supported component?** Recommendation:
   yes, and keep it deliberately thin - if it grows business logic it becomes a second place
   where orders are interpreted.

--------------------------------------------------------------------------

## 5. Unknowns that only a run can settle (as testable predictions)

P1. **Sim engine runs headless with no GUI.** Prediction: vrfSimHLA1516e.exe started per UG
  4.1.2 with -T and -L, and no vrfGui, loads the scenario and advances sim time. Falsified
  if it blocks waiting for a GUI/session peer, or if simulation time does not advance
  without -r.

P2. **External control without a GUI.** Prediction: MAK's own remoteControlHLA1516e.exe can
  create and task an object with no vrfGui running. Falsified if any command requires a
  GUI-side session. Confidence high, because the sample's own comment describes avoiding a
  VR-Link licence by construction order, implying it is intended to run alone [V].

P3. **Remote-control convenience movers still work on 5.2 ground vehicles.** Prediction:
  DtVrfRemoteController::moveToLocation on an M1A2 produces motion toward the point.
  Falsified if the entity accepts the task and does not move, or the object console reports
  an unknown/legacy task. This is the highest-value unknown in the document: MG 2.4 and AMG
  both warn remote-control movement code may need updating [V]. Mitigation if falsified:
  build the task explicitly with taskFactory() using task ID ground-vehicle-move-to and send
  via sendTaskMsg.

P4. **Plan callbacks are the completion signal.** Prediction: wrapping a single Move To in a
  plan and subscribing yields a plan-statement callback on start and a plan-complete
  callback on arrival. Falsified if plan-complete never fires for a successfully completed
  movement, or fires immediately on assignment.

P5. **Maneuver To keeps a company together and Move To does not.** Prediction: over an
  identical 8 km leg, maximum subordinate spread under Maneuver To is materially smaller
  than under Move To. Falsified if the two are indistinguishable - which would mean the
  cohesion requirement needs formation tasks (Move Into Formation / Transition Into
  Formation) instead.

P6. **Fixed-frame run-to-complete gives run-to-run repeatability.** Prediction: two runs of
  one scenario, same seed, fixed-frame-run-to-complete, local terrain, produce end positions
  within a small tolerance. Falsified if positions diverge beyond tolerance; then the next
  suspects, in order, are thread-count nondeterminism (--numCallbackThreads,
  --disableParallelTick [V]), network timing, and terrain paging.

P7. **The local N34W117 pair actually covers the operating box.** Prediction: a terrain
  built from N34W117.dem/.tif returns valid elevation across 34.3-34.7 N, 117.0-116.4 W with
  no online layer enabled. Falsified by any no-data region inside the box, or by an entity
  failing to ground-clamp.

P8. **NETN ETR MoveToLocation is accepted from a foreign federate.** Prediction: a
  UniversalHlaInteraction of class ETR_Task.MoveToLocation, sent from C#, moves a VR-Forces
  entity. Falsified if ignored. Even if it succeeds, expect NO TaskStatusReport back,
  because MAK's supported ETR list contains no report classes [V, IG 2.3].

P9. **Teardown is repeatable.** Prediction: launch/teardown ten times in a row leaves no
  surviving vrfSim or RTI process and the eleventh launch succeeds. Falsified by any wedged
  RTI process; that failure mode must be designed for, not discovered late.

--------------------------------------------------------------------------

## 6. Traps found in the docs and the shipped data

T1. **vrfSimHLA4.exe looks like the modern choice and cannot run here.** HLA 4 is the
  headline 5.2 feature [V, RN p 3], but neither installed MAK RTI ships 1516-2025 libraries
  [V], and RN warns that SISO-REF-075 compiler markers "prevents HLA applications from
  loading binary files from non-compatible compilers" [V].

T2. **The remote-control convenience task methods are pre-5.2 vocabulary.** moveToLocation /
  moveToWaypoint survive in vrfRemoteController.h [V] while MG 2.4.1 Table 1 says the
  corresponding task IDs were replaced [V]. A method that still compiles is not evidence
  that the task still exists.

T3. **Deprecated and removed scripted tasks that still appear in the UI.** AMG lists
  move_on_roads_to_waypoint as removed, ship_move_to_location and move_to_waypoint_path_plan
  as deprecated, and notes that "in order to support their editing in current plan dialogs
  the UI still exists" [V].

T4. **Batch mode cannot be used for live C2SIM.** "Batch mode is read-only. You cannot save
  a scenario, create simulation objects or objects, pause the scenario, or otherwise change
  it" [V, UG 7.10]. It is easy to mistake batch mode for the headless answer to everything.

T5. **Batch recording silently refuses the second run.** "The MAK Data Logger does not
  record batch file runs if a previous recording exists. ... once you record a batch run,
  you cannot record a subsequent run of the same batch." The fix requires editing
  lgrConfig.xml to add processRemoteControl / recordRemoteControl / allowDeleteRemoteControl
  and, for DIS, listenWhenIdle false [V, UG 7.10.5, p 272]. Exactly the trap that makes an
  A/B comparison quietly compare a run against itself.

T6. **The "VR-Forces External Communication Server Interface" is not an external control
  interface.** IG chapter 3 sounds like a general command channel; it is a radio
  communications-effects protocol (Process Message, Timeout, Ready To Send Signal,
  ProtocolID 10000) [V].

T7. **NETN ETR gives tasking but no task feedback in VR-Forces.** NETN-ETR.xml defines
  TaskStatusReport with TaskStatusEnum32 including Accepted and Completed [V], but MAK's
  supported ETR class list contains only task classes, no reports [V, IG 2.3]. Reading the
  FOM module and assuming feedback exists is the trap.

T8. **The Launcher is a hidden GUI dependency.** UG 5.3.1: "You must launch a predefined
  connection from the vrfLauncher at least once so that VR-Forces can save the network
  address information it requires to launch" [V]. Any design that uses `vrfLauncher
  --connection` therefore has a one-time human step. Bypass with direct executable
  invocation per UG 4.1.2 [V].

T9. **Gameware navigation: two vendor statements in tension.** UG 3.6 still says "VR-Forces
  supports advanced terrain navigation for entities based on the Autodesk Gameware
  Navigation" and "If you want to generate navigation data, you must purchase an additional
  license" [V], while MG 2.4 says "Ground vehicles no longer use Gameware for steering or
  obstacle avoidance" [V]. Treat the User Guide chapter as possibly stale for 5.2 ground
  vehicles; verify before buying anything.

T10. **Shipped terrain configs reference an older SharedData version.** "NTC (online).mtf"
  carries `<myName>../../SharedData/17/latest/TerrainData/ TerrainConfiguration/NTC
  (online).earth</myName>` while `<myFilename>` uses `$(SHARED_DATA_DIR)` [V]. The installed
  data is SharedData\19. Harmless if the macro wins; a silent wrong-file if anything
  resolves myName.

T11. **The "primary" terrains are network-dependent.** MAK Earth (online) and NTC (online)
  both stream from http://vr-theworld.com [V]. A repeatable, unattended run must not depend
  on them.

T12. **No navigation data anywhere near the operating area.** navData exists only for Ala
  Moana, Kilo2, Range220, Thun [V]. Ground movement in the assigned box will run in the
  no-mesh fallback path.

T13. **Fuel and resource values from older scenarios are wrong by construction.** "As of
  VR-Forces 5.2, the fuel quantities are measured in real-world units ... when you load
  scenarios in VR-Forces 5.2 that were created in previous releases and include entities
  with fuel resources, the application sets the fuel amounts to full." [V, MG 2.2.3]. A
  silently rewritten value is worse than an error.

T14. **Frame time zero freezes fixed-frame modes.** "A value of zero for frame time prevents
  simulation time from advancing in either of these modes" [V, UG 12.2.1]. Setting
  frame-mode without frame-time produces a run that starts, reports healthy, and never
  advances.

T15. **Chinese platforms exist; Chinese units do not.** [V] Assuming the SMS covers PRC
  forces because Type 99 and J-20 are present is the trap.

T16. **AI Enabled was renamed.** The AIEnabled state property is now
  AutonomousActionsEnabled and the set data request is now Autonomous Actions Enabled [V, MG
  2.6 and AMG]. Old scripts referencing the old name fail quietly.

T17. **DIS port range affects object recognition.** "If you specify a DIS port that is less
  than 3000 or greater than 4000, discovered objects might not be seen as VR-Forces objects"
  [V, UG 5.2]. Relevant if DIS is used as the diagnostic fallback.

T18. **State properties must be in the right section to be published.** "Published state
  properties must be in external-state-data" [V, AMG]. A property moved to
  internal-state-data for performance stops appearing on the network - i.e. stops being
  evidence.

--------------------------------------------------------------------------

## Executive summary (15 lines)

1. MAK's 5.2 grain: task the UNIT, let VR-Forces model subordinate behaviour, and accept
   that ground movement was rewritten as a Lua task in 5.2.
2. Cohesion is a task choice: Maneuver To / Maneuver Along keep a company together; Move To
   makes every subordinate plan independently.
3. Use HLA Evolved (1516-2010) on MAK RTI 4.6.1. vrfSimHLA4.exe ships but no installed RTI
   provides 1516-2025 libraries.
4. Run vrfSimHLA1516e.exe directly per UG 4.1.2 - never the Launcher, which has a documented
   one-time GUI step to persist network addresses.
5. Control plane: a thin C++ shim over DtVrfRemoteController, derived from the shipped
   examples\remoteControl.
6. Telemetry plane: pure C# over vrLinkSharp, reflecting the same federation independently.
   Two observers, not one, is what makes evidence credible.
7. Everything else - C2SIM SDK ingress, order compilation, type mapping, verification,
   reporting - is C#.
8. Completion status comes from PLANS, not tasks: the remote-control Tasks API has no
   completion callback; plan-statement and plan-complete callbacks do.
9. NETN ETR is a useful secondary channel but supports only ten task classes and no
   task-status reports in VR-Forces.
10. Repeatability: fixed-frame-run-to-complete, non-zero frame-time, fixed
    random-number-seed, time-multiplier 1.
11. A/B comparison of a fixed order set belongs in vendor batch mode (.bsn), which is seeded
    and unattended but strictly read-only.
12. Terrain: build a local .earth from N34W117.dem and N34W117.tif, which cover the whole
    34.3-34.7 N / 117.0-116.4 W box. Avoid the online terrains.
13. Expect no navigation mesh in the operating area; 5.2's Move To script plans around
    feature obstacles without one.
14. Fidelity gap to close: US and Russian unit templates ship, Chinese platforms ship,
    Chinese unit templates do not and must be authored.
15. Prototype first, with zero code: drive a headless vrfSimHLA1516e with MAK's prebuilt
    remoteControlHLA1516e.exe and find out whether a ground company actually moves and
    reports completion.
