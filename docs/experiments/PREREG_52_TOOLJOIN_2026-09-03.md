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

RELABELLED after the cold-start review (COLDSTART_REVIEW_2026-09-03.md R5/F1-F3):
CONTROL CHANNEL PASSED; OBSERVATION CHANNEL FAILED; ENTITY EXISTENCE UNVERIFIED.
- The "vendor observer also blind" leg is WITHDRAWN as evidence: listen_3813.txt has no
  federation/join line (it accepted --exConnConfigFile per its own -h text, but whether
  it joined MAK-ONE-2025 is unproven); netdump_3814 was a PARSE ERROR (never joined).
- Assistant pid 54616 (5.0.1, -K, non-elevated) was spawned at 19:45:48 by the
  supervisor's OWN stray `rtiSimple1516e_64 --help` run from a shell WITHOUT the env
  (its capture reads "Attempt to create and connect to Assistant ... shutdown
  message"); listen 3813 ran afterwards WITH the env (its capture loads the repo rid).
- Sim log 3805 carries NO creation line for ORACLETEST/NETDUMPTEST at notify 3: the
  5.0.2 "Locally Simulated" creation-line oracle is silent on 5.2 too. So the only
  evidence the entities exist is the controller's ObjectCreated echo (entityId
  1:3805:N). Competing hypothesis kept OPEN: the entities never existed on the sim.
- Evidence-discipline defect: 3803/3804/3807/3809/3815 have NO file capture (console
  only). From now on every live tool run tees to runs/launch52/<tool>_<appNo>.txt.
- Falsified for reflected=0: paused clock (3810), disableRemoteDiscovery flag (3811),
  derived-vs-base init (3812). NOT falsified, now ranked first (RESEARCH_52_OBSERVER_
  DISCOVERY): our counter hooks UUID-CHANGE callbacks only, while 5.2's
  DtReflectedExtEntityList withholds entities under waitForVrfExtendedData=true; also
  unverified licensing (assistant-free mode hides the License Not Found dialog; RTI UG
  8.2/8.3: unlicensed = 2-federate cap and no exchange with licensed peers).
Adversarial review: the control-channel claim's strongest competitor - "exit 0 but a
parallel federation" - is countered by BackendCount=1 (the sim's vrfExt backend-state
interactions reach the tool; interactions do not cross federation executions) - but
that observation is prose-only (no capture), so it stays ASSUMED until re-run with
tee. Next prereg gates: (1) WatchVrf --diag ReflectedCounts + HaveRtiLicense at join;
(2) --no-wait-ext as the single variable; (3) the user's answer on whether vrfGui
showed the two entities; (4) listen re-run capturing its join line.
