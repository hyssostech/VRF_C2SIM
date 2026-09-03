# PREREG - first OUR-stack join of the live 5.2d sim (RtiProbe Release-5.2)

Date 2026-09-03. Tier STANDARD. Follows PREREG_52_LAUNCH_2026-09-03.md (sim from
that prereg is still up: vrfSimHLA1516e pid 91132 in federation MAK-ONE-2025,
vrfGui pid 73264; assistant-free env).

## 1. What changed since the launch prereg (the variables under test)
tools/{RtiProbe,CreateOne,WatchVrf} gained the BridgeConfig axis (own output tree
per bridge, VrfC2SimApp.csproj pattern) and stack-aware identity
(tools/Shared/StackIdentity.cs): on a 5.2-bound bridge the join is CONFIG-FILE
identity (Federation/FedFileName empty, FomModules cleared -> VrfFacade::Start
pushes no --execName/--fedFileName and MAK-ONE-2025-Config.xml supplies identity,
DIFF rows A2/A9); 5.0.2 keeps CWIX-2024 + 3 modules. Both variants build 0/0.

## 2. Offline gates already run (results)
- WatchVrf --con-selftest: PASS on Release AND Release-5.2 (managed-only path).
- RtiProbe-5.2 no-args under DEFAULT env: crash FileLoadException exit -532462766
  - the PATH name-binding trap reaches even the usage path (top-level-statements
  Main resolves VrfBridge eagerly). INSTRUMENT NOTE: the exit-2 usage contract for
  a 5.2 tool holds ONLY under the 5.2 PATH; the runner must set env before ANY
  invocation, including probes of the tool itself.
- RtiProbe-5.2 no-args under 5.2 PATH: exit 2, usage text - contract intact.

## 3. App number (ledgered BEFORE the join)
- 3803: RtiProbe Release-5.2 join gate (single number covers its internal retries,
  per the tool's design). NEXT FREE advanced to 3804.

## 4. Predictions
P1 HIGH: RtiProbe-5.2 under the full 5.2 env (cwd C:\MAK\vrforces5.2d\bin64, 5.2
   PATH prefix, MAK_RTIDIR/RTI_RID_FILE=config/rid-461-ridconfigured.mtl,
   RTI_ASSISTANT_DISABLE, MAKLMGRD_LICENSE_FILE from Machine) exits 0 on attempt
   1 with banner "stack=5.2 federation=(from connection config...)". Falsifier:
   nonzero exit or a 5.0.2 banner (wrong DLL bound). A miss = STOP.
P2 MEDIUM: the probe joins the SIM'S federation MAK-ONE-2025, not a parallel one
   - observed as new join/resign traffic in the sim's console log after the run
   (notify 3), naming the probe federate or MOM activity. Falsifier: probe exit 0
   with NO trace in the sim log -> shared-federation NOT proven; the discrimination
   then moves to CreateOne back-end discovery under a scenario (later prereg) -
   record, do not claim.
P3 LOW: sim/GUI unaffected (thread counts in band, no dialogs).
NOT claimed here: entity creation, telemetry, behaviour (no scenario is loaded).

## 5. Procedure
1. Snapshot the sim log line count. 2. Run RtiProbe-5.2 3803 (full env, cwd 5.2d
bin64). 3. Diff the sim log tail; record processes. 4. Fill sec 6.

## 6. Result (2026-09-03; runs/launch52/)
GATE PASSED - our stack joins and drives the live 5.2 sim:
- RtiProbe-5.2 3803: exit 0, "stack=5.2 ... created/joined (config-file identity)
  and resigned cleanly" on attempt 1. P1 CONFIRMED. Sim log grew (5211->5529) but
  the new lines are FOM class listings, not a federate-join line for 3803 - P2 not
  decisive from the log alone (the sim logs its own FOM, not each joiner).
- CreateOne-5.2 3807 (after a scenario relaunch, appNos 3805/3806, Sample\
  FirstExperience on offline Ala Moana terrain): BackendCount=1 after 0.1s,
  ObjectCreated uuid=VRF_UUID:fe08...1b00 entityId=1:3805:9. Repeated 3815:
  entityId=1:3805:12. The BACKEND-DISCOVERY + CREATE + control-callback path is
  fully working on 5.2 (site 1:app 3805 = the sim's own federate owns the entity).
- Earlier 3804 (no scenario loaded): BackendCount stayed 0 -> CreateOne correctly
  REFUSED (exit 1). Confirms a scenario/backend must be present; the tool's guard
  works on 5.2.
- Two-federate rtiSimple transport check over the rid-configured connection:
  reciprocal discoverObjectInstance + reflectAttributeValues - the connection
  itself carries object reflection between peers.

OPEN (new, separate from the join gate) - the OBSERVATION channel:
WatchVrf-5.2 (3808 paused, 3810 after RunSim-5.2 3809 started the clock, 3811 after
facade disableRemoteDiscovery=false, 3812 after switching the 5.2 branch to the BASE
init(DtExerciseConn*) that creates the DtRemoteObjectManager) ALL report reflected=0
for 30 s. The vendor RAW observer listenHLA1516e_64 joined with the sim's own
--exConnConfigFile and also produced NO discovery in 40 s. So a late-joining
observer federate does not discover the sim's published entities, even though (a)
the control path discovers the backend and creates entities and (b) two rtiSimple
peers reflect to each other on this connection. This is an INSTRUMENT-REBASELINE
task (plan Phase 2: "re-baseline WatchVrf on the new build"), not a join-gate
failure - the join gate is PASSED. It gets its own prereg with fresh eyes.

Adversarial review: the join-gate claim's strongest competing hypothesis - "exit 0
but not actually in the sim's federation" - is falsified by CreateOne discovering
THE SIM's backend (BackendCount=1) and the sim stamping the created entity with its
own site:app (1:3805). The reflected=0 symptom is NOT swept under that claim: it is
called out as an unexplained, separately-tracked observation-channel defect, not a
footnote. Ruled out for reflected=0 so far: paused clock (3810 post-RunSim),
disableRemoteDiscovery flag (3811), the derived-init-not-creating-the-object-manager
hypothesis (3812 base init, still 0). NOT yet explained - the live falsifier for the
next prereg: whether the vrfGui front-end (already joined, same federation) DOES see
the entities on screen (control-only vs network-reflection discrimination).
