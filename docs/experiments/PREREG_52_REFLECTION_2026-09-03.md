# PREREG - 5.2 observation channel: why does a late-joining observer reflect 0?

Date 2026-09-03 (evening). Tier HEAVY (cause claim pending). Predecessors:
PREREG_52_TOOLJOIN_2026-09-03.md sec 6 (relabelled), COLDSTART_REVIEW_2026-09-03.md
(R5, R6, R2, D1-D5), RESEARCH_52_OBSERVER_DISCOVERY_2026-09-03.md (ranked hypotheses).
Docs consulted are the ones those two reports cite; this prereg adds none.

## 1. Frame
The 5.2 sim engine (independent mode, assistant-free rid connection, federation
MAK-ONE-2025, scenario Sample\FirstExperience\firstexperience) is controllable by our
bridge (BackendCount=1, ObjectCreated) but NO observer has reflected an entity. Two
things are unknown and must be separated: (a) do entities EXIST on the sim (creation
lines are absent from the 5.2 log; only the controller's echo says so); (b) if they
exist, why does our observer count 0. Instruments (executor deliverable, additive,
opt-in): WatchVrf --diag = NativeStackInfo + HaveRtiLicense/HaveVrLinkLicense at
join, and per sample ReflectedCounts() read DIRECTLY from the controller's reflected
lists next to the UUID-callback "reflected="; --no-wait-ext = waitForVrfExtendedData
(false) lever. Every live run is TEE'd to runs/launch52/<tool>_<appNo>.txt (the
F1 evidence defect).

## 2. Hypotheses (from the research report, ranked) and their single falsifier
H1 Counter, not channel: entities ARE in the reflected lists; UUID-change callbacks
   never fire on 5.2 (ext-data handshake). Falsifier: ReflectedCounts() entity count
   == 0 while the sim holds entities (then H1 is dead).
H2 Ext-data withholding: DtReflectedExtEntityList hides entities until VRF extended
   data arrives. Falsifier: --no-wait-ext run still 0 with counts 0.
H3 Silent unlicensed federate (2-federate cap; no exchange with licensed peers).
   Falsifier: HaveRtiLicense()==true at join in the observer AND 3 concurrent
   federates (sim, gui, observer) all exchanging - then H3 is dead.
H4 Entities never existed on the sim (ObjectCreated is a controller echo). Falsifier:
   the user saw them in vrfGui (Q3), OR a creation/"Locally Simulated" line appears at
   a higher --notifyLevel, OR the sim's own object count (GUI status) is > 0.
H5 Subscription/class mismatch (NETN-Physical vs RPR subscription in our reflected
   lists). Falsifier: a vendor observer that provably JOINED MAK-ONE-2025 (join line
   captured) reflects the entity while ours does not.
H6 Publisher silently skipping (RN VRF-8063: missing FOM class -> warning + skip).
   Falsifier: no such warning in the sim log at notify 4.

## 3. Predictions (written before the run; a missed HIGH = STOP)
P1 HIGH: HaveRtiLicense()==true in every observer at join (three federates already
   coexisted today: sim + gui + CreateOne). If false -> H3 wins, STOP, licence first.
P2 MEDIUM: ReflectedCounts().entities >= 1 within 10 s of join while the UUID-callback
   "reflected=" stays 0 -> H1 confirmed; the fix is the counter (Phase 2 re-baseline).
P3 MEDIUM: with --no-wait-ext the UUID-callback count also rises -> H2 is the
   mechanism behind H1.
P4 LOW (record): the user's answer to Q3 (did vrfGui show ORACLETEST / NETDUMPTEST).
Success = any ONE observer channel reflects >= 1 entity with a real POS line AND
the licence is proven. Otherwise the phase stays STOPPED at the observation gate.

## 4. Procedure (single variable per step; app numbers ledgered BEFORE each join)
0. Executor diagnostics land; both bridge configs + WatchVrf build 0/0; --con-selftest
   and --capabilities (diag, no-wait-ext) pass offline. 5.0.2 default path unchanged.
1. LaunchVrf52 -Scenario Sample\FirstExperience\firstexperience (fresh backend/gui
   numbers), READY, join line captured in the sim log.
2. CreateOne-5.2 (fresh number, TEE'd): ObjectCreated captured to file.
3. WatchVrf-5.2 --diag 60 s (fresh number, TEE'd): licence + counts + reflected=.
4. WatchVrf-5.2 --diag --no-wait-ext 60 s (fresh number, TEE'd) - the ONE variable.
5. Only if 3-4 are both 0/0: listen re-run with the FULL env, capture its join
   line; then the sim relaunched at --notifyLevel 4 for VRF-8063 warnings.
6. Fill sec 5; relabel the tool gate if an observer channel now works.

## 5. Result (2026-09-03 late; every run tee'd in runs/launch52/)
Steps 1-4 (3816-3820, Probe52Reflection.ps1, EMPTY-ish scenario firstexperience): P1 HELD -
licence rti=1 vrlink=1 (H3 DEAD). P2 MISSED - the reflected lists themselves are empty
(ent=0 agg=0 env=0 ctl=0, discovered=0; the vendor's own printReflectedObjectCounts says
Total 0) -> H1 DEAD (counter is not the problem). P3 MISSED - --no-wait-ext (waitext=0)
changed nothing -> H2 DEAD. Control channel captured: BackendCount=1, ObjectCreated
REFLTEST 1:3816:9 (createone_3818.txt).
RTI notify-4 traces (rid copy config/rid-461-ridconfigured-notify4.mtl, observer only):
- createone_3824_notify4.txt (complete, 4.5 MB): our federate performs full declaration
  management - 113 subscribeObjectClassAttributes (RPR + NETN + MAK trees incl.
  Platform.GroundVehicle), 26 subscribeInteractionClass, 251 requestClassAttributeValue-
  Update, 3 sendInteraction - IDENTICAL to the vendor remoteControlHLA1516e.exe profile
  (remotecontrol_3825_notify4.txt). It discovered exactly ONE object from the sim:
  "Time and Date-1:3816" (MAK_TimeAndDate). No entity, ever, on either federate.
- watchvrf_3822_notify4.txt is TRUNCATED at 0.6 s (11 KB, no trailer): the supervisor's
  pipeline `| Select-Object -First 40` stopped the pipeline and KILLED the federate (a
  joined federate killed - a rule breach by instrument). The "WatchVrf subscribes to
  nothing" inference drawn from it is WITHDRAWN (executor caught it). Never put -First
  on a live pipeline; tee to file, filter afterwards.
- listen_3823_notify4.txt: the vendor listener subscribes 42 object classes; discovered 0.
Vendor listener, joined (join line captured), explicit classic flags: listen_3821 (30 s,
empty scenario) 0 entities; listen_3829/3834 (30 s each, first_experience_advanced, 741
OOB uuids, "Successfully loaded scenario", sim 3826) 0 entities, "[0, 0]" throughout.
Scenario relaunch 3826/3827 (first_experience_advanced): NO federate discovers the
back-end any more - RunSim 3830 and 3832 BackendCount=0 after 15 s (exit 1, refused
Run), WatchVrf 3833 --diag --report-backends backends=0 for 90 s. The clock was never
started on that scenario, so "running + entity-bearing" remains UNOBSERVED.
Corrections: ".scnx has 0 entities" (earlier) was a grep on a ZIP - .scnx is a zip
(.scn/.oob/.pln/...); entity/unit records live in the .oob (firstexperience 11 uuids,
first_experience_advanced 741, Raid 273, Traffic 487).
STANDING (verified): H1 H2 H3 dead; H4 (created entities never existed) NOT excluded -
no sim-side evidence exists in any log; H5 (class mismatch) weakened - our federate
subscribes the whole Platform tree and still sees nothing; H6 untested (no notify-4 sim
run). NEW LEAD H7 (transport): the assistant-free rid is a pure LIGHTWEIGHT connection
(useRtiExec 0, mcastDiscoveryEnabled 0, discovery from data updates only) whereas the
5.0.2 golden path ran on the assistant's rtiexec LOOPBACK connection; a paused sim sends
few updates, HLAreliable traffic has no forwarder to ride. Research lane opened:
RESEARCH_RTI_CONNECTION_TRANSPORT_2026-09-03.md. Falsifier for H7 = the same observer
against the same sim on a rid-configured rtiexec connection reflecting > 0.
CORRECTION (same night): sim 3826 had CRASHED on its first tick (0xC0000005 in
DtDiGuyController::determineInitialHandItem, first_experience_advanced's DIGuy
characters) - its "back-end discovery lost" observations are VOID, not transport.
USER RULING (2026-09-03 21:20): no old bits - the 5.2 stack runs on MAK RTI 5.0.1.
Applied (Traffic scenario, vehicle-only; runs/launch52/*3840-3853*):
- Release-5.2 bridge LOADS and JOINS on RTI 5.0.1 DLLs unchanged (watchvrf_3845: "Using
  MAK RTI 5.0.1", licence rti=1 vrlink=1, backends=1); RunSim-5.2 starts the clock (3846).
- Assistant mode: the first sim launch (3840) blocked on a "Choose RTI Connection" dialog
  (present in the window list, missed by the screenshot) and died "RTI Connection failed"
  when AnswerRtiDialog clicked; the stored choice (%APPDATA%\MAK\RTI\5.0\Legatus\
  connections.xml: lightweight 229.7.7.7:4000 on 10.5.0.2, chosen=1) then let 3844 join
  with no dialog. Every assistant-mode PROBE that found no responsive assistant SPAWNED
  one (-K): four 5.0.1 assistants now run (19612/42396/49336/54616) - never killed.
- Observer on the 5.0.1 lightweight sim: reflected 0 / lists 0 / discovered 0 while
  PAUSED (3845, 40 s complete); RUNNING (3847) cut at t=3 s by a tool timeout - NOT a
  valid observation.
- rid-configured RTIEXEC connection on 5.0.1 (config/rid-501-rtiexec.mtl, headless
  rtiexec 15720 + its forwarder 43728 up on 127.0.0.1:4001/5000 - LEFT RUNNING, never
  kill): the 5.2 sim CRASHES at start in makVrf::DtVrfSimOptions::parseCmdLine ->
  vlHLA1516e -> vl.dll (C:\MAK\logs\...38180.callstack.log). Dead end as configured.
- Sim-side RTI trace (does the sim registerObjectInstance ANY entity?): NOT captured -
  RTI=> lines never reach --logFileName, -q's log (3851/3852), and a direct launch with
  stdout redirected (3853) stalls the sim at 4 threads before joining. OPEN.
STANDING: H1 (publisher silence) and H2 (no FOM-module merging in lightweight mode,
RM 6.3.1/14.3) remain the top two; H4 (created entities never existed) still not
excluded; H7 transport untested (the rtiexec rid crashes the sim). Screenshot
gui_traffic_rti461_running.png: the GUI showed Traffic loaded, clock RUNNING, but a
blank white view - it did not settle H4 either.
RESOLVED 2026-09-04 (PREREG_52_RTIEXEC_2026-09-04.md): the observation channel is
REPAIRED under the DOCUMENTED posture - MAK RTI in rtiexec mode (UG52 5.5.1 p190
prohibits lightweight mode with VR-Forces; every 2026-09-03 run was lightweight) with
the interface pinned to 127.0.0.1 (the 5.0.2 Launcher's fixed value). WatchVrf 3856:
62 reflected entities, real POS lines. H1/H2/H4/H7 are all subsumed: nothing was wrong
with the sim or the bridge; the RTI connection posture was unsupported. The record
below stands as the trail of a docs-first breach.
NEXT (frame per the user's rulings 2026-09-03/04: vendor-managed 5.2 stack, no 4.6.1
anywhere, and NO questions to MAK - docs and our own runs only): (0) DOCS FIRST, the
part skipped all night: the 5.2d equivalent of the 5.0.2 HLA connection configuration
(UG52 4.7 Launcher connections, 5.3.1 "launch a predefined connection at least once",
IOG connection chapters) - RESEARCH_52_HLA_CONNECTION_CONFIG_2026-09-04.md. Read
2026-09-04 (user: "read the C2SIM setup instructions"): c2simVRFinterfacev2.36 README.txt,
VRFadditionalFiles/README.txt, C2SIM-VRForcesv2.26.pdf, the .bat launchers and
vrfLauncher.pdf - the 5.0.2 HLA setup WAS the Launcher's Simulation Connections
Configuration dialog: NETWORK INTERFACE ADDRESS 127.0.0.1, federation, FED file, session
ID, back-end/front-end site+app numbers, RPR 2.0, the 3 FOM modules; the interface .bat
matches session/site/app 3201/federation, VRF address 127.0.0.1. Our 5.2 launches never
set the interface address (the 5.0.1 assistant stored udpInterface 10.5.0.2 - a
non-loopback adapter; our rid copies 0.0.0.0; the 4.6 golden used 127.0.0.1 + loopback
broadcast) - the single parameter that could explain "reliable crosses, best-effort
(entity updates) never"; it is a documented setting, so the docs decide before any
probe; (1) perform
that documented one-time configuration (headlessly if the files allow, else once via
the Launcher) and re-run observer + create on it; (2) the sim's RTI trace via the RTI's
own log file (rid RTI_logFileName) instead of stdout. Runner default stays -VrfProfile
5.2 on 4.6.1 until (1) lands - flip RtiDir/rid then.
