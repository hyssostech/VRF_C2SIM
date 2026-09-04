# VR-Forces 5.0.2 -> 5.2d migration diff (Phase 0 deliverable, 2026-09-02)

Purpose: every difference that touches this federate, cited, with its effect and
whether a user decision is needed - BEFORE any build, run, or C:\MAK edit.
Sources (5.2d install = C:\MAK\vrforces5.2d): MG = VRFMigrationGuide.pdf; RN = release
notes; UG52/UG502 = Users Guides; IOG = MAKInteroperabilityGuide.pdf; API52 = classref
vrf_migration52.html; VRL = VR-Link 5.10 release notes; DISK = installed files; HDR =
headers diffed vs 5.0.2. Verbatim quotes live in the scratchpad row files; this file
keeps verdicts. Where sources disagree the INSTALLED file beats the manual (IOG names
VRFExt-9/VRFAggregate-6; the shipped config carries VRFExt-12/VRFAggregate-7).

## CLOSED - settled from disk or code, no decision owed
- HLA 4 needs MAK RTI 5.0 (IOG; vrfSimHLA4.exe -> librti1516_2025vc141.dll); 5.0.1 INSTALLED
  2026-09-03 (Y-16). Migration gates stay HLA 1516e on makRti4.6.1 (librti1516e64.dll).
- Our tank company is an ENTITY-LEVEL unit (DtDisaggregated + createSubordinates,
  UnitTranslator.cs :156). UG52 ch 28 aggregate-level distribution does NOT govern us;
  the entity-level unit rules (rows D1-D4) do.
- VrfFacade.cpp has no predicted source break (B1-B9, HDR); the ONE predicted break is
  remoteControlInit.cxx :19-20 (myHlaConnection deleted in VR-Link 5.10, A8). Facade uses
  none of the removed controllers/setVisibility/DtObjectType/rewind/etc (grep 2026-09-02).
- appNo ledger SURVIVES: -a/-s are VR-Link base options for HLA too (VRL-758; UG52 5.4.1);
  --sessionId is orthogonal (control group, default 1, UG52 4.1.3 = UG502 4.1.3). NEXT FREE
  marker stays in force. vrLinkSharp has no remote-control classes; the C++ facade stays.
- Compiler: v145 (VS 18), NOT v143 (VS 2022 left 2026-09-02; RN p2 binary-compat covers it;
  v143 5.0.2 baseline kept in src/VrfBridge/build/baseline-502-v143-20260902/). Sec H.
- FFRTC survives: scenario keys frame-mode/frame-time unchanged (UG52 12.2.1 Table 20
  p353-354 = UG502 Table 17); exerciseClock.h keeps FmRunToComplete (HDR).

## A. Connection, federation, launch (decision-heavy)
| # | 5.0.2 | 5.2d | Cite | Effect on us | Decision |
|---|---|---|---|---|---|
| A1 | Launcher profile carried federationName CWIX-2024, fedFileName, 3 fomModules, appNumber 3001/3101, siteId, rprFomVersion | Profile "HLA 1516 Evolved.xml" carries only autoconnect, configFile=MAK-ONE-2025-Config.xml, hostAddress, sessionId=1 | DISK appData\settings\vrfLauncher; RN p3 | LaunchVrf.ps1 profile name and autoConnect logic are dead; identity now lives in the connection config | Y-1 |
| A2 | No connection config file | appData\settings\connections\MAK-ONE-2025-Config.xml: execName MAK-ONE-2025, RPR_FOM_v2.0_1516-2010.xml, rprFomVersion 2.0 rev 2, netnFomVersion 3.0 rev 1, 17 modules (NETN-BASE/ETR/Physical/METOC/MRM, MAK-Physical-2, Aerodrome-1, METOC-3, VRFExt-12, DIGuy-7, LgrControl-2, VRFAggregate-7, DynamicTerrain-2, VRLExt-3, DER-1, RPR-Enumerations_Experimental_IFF, RPR-MAK_Experimental_IFF-4); vrfSim --exConnConfigFile overrides | DISK; UG52 4.7.4 p150, 5.2 p181; MG 3.1 p24; IOG 5.3 p84 (module ORDER matters) | appsettings FomModules [VRFExt-6, DIGuy-7, LgrControl-2] + Federation CWIX-2024 no longer match what the sim joins; the sim hard-fails on missing required classes (requiredFomClasses.mtl, RN p71) | Y-2 |
| A3 | vrfSim.mtl held disPort/exerciseId/execName/fedFileName/rprFomVersion; mimModule | Keys moved to the connection xml; mimModules; new maxAsynchronousTerrainThreads 7, loadAllNavigationDataOnTerrainLoad 0, automaticallyCreateGlobalEnvironment 0, blockOnAsynchronousOperations 0 | DISK vrfSim.mtl diff; UG52 App C p1668-1677 | Connection edits go to the xml, not vrfSim.mtl | N |
| A4 | Our notifyLevel 3 / objectConsoleNotifyLevel 3 / enableLogFileTimestamps 1 edits | 5.2d ships factory 2/1/0, same keys (:176/:179/:279) | DISK | Re-apply before any log instrument is believed (C:\MAK edit) | Y-3 |
| A5 | vrfSim.log written to the working dir (bin64) | Written to C:/MAK/logs by default; new vrfSim --logFileName | UG502 4.9 p161 vs UG52 4.10 p162, 5.2 p181 | Our watcher tails bin64\vrfSim.log -> silent channel (the vendor-diagnostics lesson) | Y-4 |
| A6 | vrfLauncher -B/-F, --usePredefinedConnection, --simArgs, -- / B-- | Option set replaced: --sim/-s, --connection/-c, --run "app" additionalargs--, -R now = run group; sim engine still launchable directly from bin64 (UG52 4.1.2 p132) | UG502 5.3 p182 vs UG52 5.3 Table 12 p187-188; MG 2.1 p16 | Every LaunchVrf.ps1 invocation is invalid on 5.2 | Y-5 |
| A7 | vrfSim options --enableMetoc, --mimModule, --selfReflect | Dropped; added --exConnConfigFile, --logFileName, --mimModules, --netnFomVersion/-Revision, --setFomModuleList, --performanceOutput | UG52 5.2 Table 11 p178-186; RN p20 | Audit the sim command line before first launch | N (part of Y-5) |
| A8 | remoteControlInit.cxx hangs backendSiteId/backendAppNum on protected myHlaConnection/mySettings | VR-Link 5.10 deleted both members (only DtExerciseConnConfig myExConnConfig remains; subclasses use exerciseConnectionConfig()->settings()); the 5.2d sample deleted that whole block, ctor is (argc, argv, defaultConfigFilePath), sessionId var only | vl\exerciseConnInitializerHLA.h 5.8 :665-667 vs 5.10 :525/:769; DISK sample diff | HARD BREAK: rewrite remoteControlInit.{h,cxx} from the 5.2d sample; our -a/-s short flags would also collide with the base parser | Y-6 |
| A9 | Argv joins with what we pass | MAK-ONE-2025-Config.xml is ALWAYS loaded (DtDefaultConfigFile, vlpi\exerciseConnConfig.h :24; sample resolves it via DtAppPathResolver::connectionsSettingsDirectory()); a 2-arg ctor looks relative to the process CWD; config-file FOM modules are ADDITIVE (VRL-739), argv applied after the file; without the file the join uses VR-Link defaults (execName "VR-Link", RPR 1.0, NETN 1.0) | VRL release notes; sample main.cxx :27-31; IOG 5.3 | Keeping our 3-module list would submit VRFExt-6 AND VRFExt-12; a hand argv must also pass --netnFomVersion 3.0 --netnFomRevision 1 --rprFomRevision 2; exact list only via --setFomModuleList / clearFomModules() / own --exConnConfigFile | Y-2 |
| A10 | Loop: setSimTime(elapsedRealTime()); drainInput(); tick() (our Tick :478-482) | Sample: init(..., disableRemoteDiscovery=false); setMonitorBackendState(true) (new :2054 - clock follows scenario play/pause); communicationManager()->run(); tick() advances the clock itself | DISK sample main.cxx :47-66; vrfRemoteController.h :2054 | Our pump must stop driving the clock by hand; monitorBackendState is a golden-trace variable (paused scenario = paused controller clock) | Y-6 |
| A11 | FED/module files found from CWD | Unchanged: CWD or RTI_CONFIG; config lists modules by bare filename; MAK_RTIDIR appears in no doc | VLCONN 4.2.2; IOG 5.1.2 | C# host CWD/RTI_CONFIG must resolve RPR_FOM_v2.0_1516-2010.xml + all 17 modules | N (runner sets RTI_CONFIG) |
| A12 | RTI dialog once per reboot, scripted watcher; assistant rtiexec loopback connection (4.6.1) | Assistant-free works (per-process RTI_ASSISTANT_DISABLE + rid RTI_configureConnectionWithRid 1, RTI Ref 5.2.10) BUT the rid MUST be the RTIEXEC posture: UG52 5.5.1 p190 "You cannot use the MAK RTI in lightweight mode with VR-Forces" - the 2026-09-03 lightweight rid gave reflected=0 all night; the documented posture (RTI 5.0.1, config/rid-501-rtiexec-min.mtl = an assistant rtiexec connection's parameters, six keys off stock, headless rtiexec started by us; --deviceAddress NOT required on either side - TESTED, runs 3857/3859) reflects 62 entities (PREREG_52_RTIEXEC_2026-09-04; cause supported by elimination, mechanism open). The parseCmdLine startup crash is BISECTED, not unknown: it is OURS, bound to `--logFileName` (11 crashes/30 passing it, 0/12 omitting it, Fisher p=0.013, 42 launches; PREREG_52_CRASH_BISECT). NEVER PASS `--logFileName`; harvest C:\MAK\logs instead. This supersedes the earlier "any rid, trigger UNKNOWN" reading. 5.2 runs on RTI 5.0.1, not 4.6.1 (user ruling) | UG52 5.5.1 p190, Tables 10/11; RTI UG 5.0.1 7.3; RESEARCH_52_HLA_CONNECTION_CONFIG_2026-09-04 | Dialog watcher retired; rtiexec/forwarder started per profile, never killed; rid-461-ridconfigured.mtl and rid-501-rtiexec.mtl are SUPERSEDED (kept as records) | N (done) |
| A13 | DtExerciseConn failure aborts | Unchanged, but ctor status arg + DtHaveVrLinkLicense()/DtHaveRtiLicense() exist | VLCONN 4.2.5.5; checkLicense.h | Start() can return an error instead of killing the C# host; licence pre-flight for the runner (expiry 2026-09-15) | N (later) |

## B. Native API (facade symbols, HDR-verified)
| # | 5.0.2 | 5.2d | Cite | Effect | Decision |
|---|---|---|---|---|---|
| B1 | controller init(DtExerciseConn*, rel, reel, ral, uuidMarking, disableRemoteDiscovery, eaol) | DtReflectedAerodromeList* ael=NULL inserted before uuidMarking; CommunicationManager overload unchanged | vrlinkVrfRemoteController.h :94-126 | Our subclass defines its own overload and forwards to the unchanged one -> compiles | N |
| B2 | createVrlinkNetworkInterface(DtExerciseConn*) | + int clockMode=0 | vrlinkNetworkInterface.h :86 | Default keeps 5.0.2 behaviour | N |
| B3 | Interface-level setExerciseStartTime/setSimTime/setTimeMultiplier | Moved to DtCommunicationManager; controller setTimeMultiplier(double)/(DtScenario*), setExerciseStartTime(DtScenario*) survive | vrfRemoteController.h :833/:926/:951 | Facade uses controller calls only | N |
| B4 | DtScenario(const DtString&), DtScriptedTaskTask/Set, createCommunicationManager | Survive (defaulted new params; +override) | scenario.h :60; scriptedTaskTask.h; communicationManager.h | Unchanged | N |
| B5 | DtPlanAndMoveToTask::setControlPoint(const DtUUID&) | Survives (moveToTask.h :77); MG's semantic-swap warning applies to the vector overload only | HDR; MG 2.4 | Route-by-uuid path unchanged | N |
| B6 | createControlArea(..., appearance) | appearance defaulted (theDefaultAreaAppearance) | vrfRemoteController.h :1091-1109 | Unchanged | N |
| B7 | DtTaskCompleteReport taskId()/success()/taskTrackingNumber() | Survive | taskCompleteReport.h | Phase 4 truthful completion can ride on success() | N |
| B8 | DtObjectType; messageTypes numbering | Removed -> DtEntityType; renumbered (RequestSpawnData 305->205) | API52; messageTypes.h | Facade already uses DtEntityType; no numeric literals in our code (verify at compile) | N |
| B9 | Controllers DtGroundMoveToLocationController etc.; DtRemoteEnvironmentController::setVisibility | Six controllers removed; setVisibilityAndObscurant | API52 | Not used by the facade | N |
| B10 | Toolchain C++17-ish, Boost smart pointers | RN: C++20 and Boost 1.84 / std smart pointers | RN p2 | COMPILED 2026-09-03 with 0 errors on the existing /std (sec H); no break | N |

## C. Data: terrain, SMS, fixtures, settings
| # | 5.0.2 | 5.2d | Cite | Effect | Decision |
|---|---|---|---|---|---|
| C1 | Fixtures pin "MAK Earth Space (online).mtf" (SharedData\16) | Absent from SharedData\19 and from UG52 Table 52 (undocumented drop). Available: MAK Earth (online), MAK Earth Base (online), MAK Earth Air and Space (online), MAK Earth Aggregate (online) ("designed for use in aggregate-level scenarios"; MAK moved its own aggregate example onto it, RN Fixed Bugs), MAK Earth (online) - Simple For High Fliers | DISK; UG52 Table 52 p1238-1239; RN p73 nav data regenerated | The 2026-09-02 ruling "MAK Earth (online)" was made from an incomplete list. Since our units are entity-level (CLOSED), the aggregate terrain's tailoring is not the deciding factor; the deciding factor is which terrain the golden fixtures should be re-authored on for entity-level ground movement with nav data | Y-7 |
| C2 | Fixtures + C# pin C2simEx.sms (oracle SMS: EntityLevel.sms + AR Scout, Mobile Irregular, Mobile Light Infantry, Skiff, Ar_Plt_US_* formations, 5 Lua scripts, DIS group connection) | Not in 5.2d data; 5.2d has EntityLevel (1702 .entity vs 1388, incl. Tank Company (USA)/(RUS), Tank Platoon; 9 Ar_Plt_US_*.frm shipped natively), AggregateLevelBase, AggregateLevel (legacy), AggregateTacticalLevel (new), MAKTest, base; custom entity SMS needs --upgrade for resources | DISK ls; MG 2.2.1 p16, 2.2.3 p17; UG52 68.3 p1309 | ObjectTypeResolver.LoadChain(topSms="C2simEx") :67, TypeMapSelfTest :140 ("Mobile Irregular"), UnitTypeMap :298-303, VrfC2SimService :1502 all encode the oracle SMS; carrying it forward violates the fidelity ruling | Y-8 |
| C3 | Scenario compat: n/a | Pre-5.2 scenario loads if SMS + terrain migrated; tasks auto-convert on load; save is forward-only; 5.2 never re-saves our goldens | MG 1.1 p12, 2.3 p17, 1.4 p13 | FixtureGen re-authors 5.2 fixtures fresh (terrain + SMS lines); 5.0.2 fixtures immutable | N (policy) |
| C4 | No navigation profiles | navigationProfiles.mtl (lifeform / ground-platform); nav data per profile; nav areas load only for local entities | DISK; RN p34, p73 | Ground planning keys off nav data; online terrain nav coverage is unverified -> Phase 2 gate | N |
| C5 | No block-on-async knob; terrain single-threaded (undocumented) | blockOnAsynchronousOperations (fixed-frame modes only, default 0); maxAsynchronousTerrainThreads 7 | UG52 App C p1669, p1671; RN p72 | Documented determinism knobs for FFRTC runs: terrain paging, path planning, altitude reads | Y-9 |
| C6+C7 | aggregateSpatialModelTick* keys; ad hoc resources | Keys gone from UG52 Table 76 (never in either installed vrfSim.mtl); resources = SISO enums, real fuel units, pre-5.2 scenarios get full fuel | UG502 p1660 vs UG52 p1668; MG 2.2.3 | No action | N |
| C8 | Move to Altitude MSL only; route vectors uncapped | Move to Altitude accepts AGL; routes capped at 2000 points. **AND setAltitude has taken `bool aboveGroundLevel` ALL ALONG** (vrfRemoteController.h:1372; VrfFacade.cpp:739 already passes TRUE) - not a 5.2 change, a capability nobody had exercised: 0 calls to SetAltitude existed in tools/ until 2026-09-04 | RN p64, p14; HDR; help DataRequests/EntityLevel/vrf_sets_setAltitude.htm | **VERIFIED LIVE 2026-09-04** (PREREG_CLAMP_DIRECTION sec 8a): one AGL call lifted a BURIED entity onto the surface. An ENTITY needs NO terrain query and NO birth altitude. A ROUTE VERTEX has no AGL frame at all, so TerrainProfile stays for vertices. The 10000 m MSL birth is the WRONG FRAME, retiring (HANDOFF NEXT 6b). Canonical: docs/VRF_ALTITUDE_FRAMES.md | **Y-17 (was "N (later)" - THAT DEFERRAL COST A DAY: a 2026-09-04 session re-derived this row's content from a stale code comment, wrongly, because nobody opened this file. A row that says "may simplify later" does not stop anyone re-entrenching the old approach. If a row identifies a SHIFT IN APPROACH, it is never "later".)** |

## D. Behaviour: movement, units, completion
| # | 5.0.2 | 5.2d | Cite | Effect | Decision |
|---|---|---|---|---|---|
| D1 | Ground entity move-to-a-point = 4 tasks (move-to-location, -path-plan, move-to-waypoint, -path-plan); road use chosen by task id | One ground-vehicle-move-to with a planning phase, automatic road use, dynamic obstacle avoidance ON, Gameware/collision sensors gone; old ids auto-convert on scenario load, but "remote-control applications that issue movement tasks to ground vehicles may need to be updated" | MG 2.4 p18-20 Table 1; API52 Ground Vehicle Movement; RN p7 | Our MoveToLocation/PlanAndMoveTo remote-control path is exactly what MAK flags; must be probed headless before any mapping change (HEAVY prereg) | Y-10 |
| D2 | Unit Move To = parallel subordinate paths; "units do not generate subordinate routes until they reach the beginning of the route"; leading-edge completion | Unit Move To silently redirected to Maneuver To (always off-road, leader/follower speed control overriding ordered speed); unit move-along -> subordinates maneuver-along with per-subordinate offset routes computed at task time; units wait for a valid formation before moving | RN p7, p28, p75; MG Table 1 p19-20; UG52 30.22-30.24 p598-600 vs UG502 26.17 p594 | The 5.0.2 lazy sub-route mechanism behind Tank-Company non-determinism no longer exists; RN Fixed Bugs "DtMaintainFormationMonitor problematic use of position and orientation of subordinate units" names a 5.0.2 defect in exactly this path. Phase 3 prediction: sub-route table stable across runs | Y-11 |
| D3 | Move Along Route: nearest-vertex start, 10 m road rule | Same options; ground vehicles compute curves around vertices; vertex counts as reached when abeam; speed engine-governed; no planning between vertices | UG52 30.24 p599-600, 23.3 p505; MG 2.4 p19 | Most deterministic route-follow available; arrival tolerance replaces exact-speed timing | N (keep MoveAlongRoute as the MOVE projector) |
| D4 | Continue On applies to ground | Removed for ground platforms; vehicles stop at each waypoint of a chained move | MG 2.4 p19 | We never set Continue On (grep); MOVE chaining is one route per order -> unaffected; leg timing re-baselined in Phase 2 | N |
| D5 | Road driving: ordered speed honoured, obstacles near road ignored | Lane model, road speed limits, curve limits; ordered speed becomes a ceiling | UG52 23.2.4 p503-504 | Golden traces on roads need arrival tolerances, not ETA; ETA display removed for ground (RN Fixed Bugs) - our extractor samples positions, not ETA | N |
| D6 | AI Enabled: firing/collision only | Autonomous Actions Enabled also gates path planning; off = straight line through obstacles | MG 2.6 p21; UG52 40.3 p866, 23.6 p508 | A determinism lever for golden traces, at the cost of realism | Y-12 |
| D7 | Navigation preferences: none | Per-entity Navigation Preferences set request (default per SMS movement system: tracked/wheels-off-road ignore roads, wheels-road prefer) | MG 2.4 p18-19 | Road behaviour on 5.2 depends on SMS defaults unless we send the set request | Y-13 |
| D8 | Task completion via proprietary report demux (TASKCMPLT rule 4) | DtTaskCompleteReport unchanged (B7); NETN-ETR task status exists but limited support | RN p70; IOG 2.3 p28; classref vrf_netn_etr_tasks | Keep the report path; NETN-ETR recorded only | N (deferred) |
| D9+D10 | Renames; doc conflict | set-road-driving-options -> set-road-passing-options, Navigation Enabled lifeforms only, Collision Avoidance Types deprecated (RN p75; unused by us); UG52 79.2 still documents ground obstruction sensors, MG 2.4.2 p20 says unused - MG rules, no probe time on obstruction tuning | RN; MG vs UG52 | None | N (ruled) |

## E. Observability and repeatability instruments
| # | 5.0.2 | 5.2d | Cite | Effect | Decision |
|---|---|---|---|---|---|
| E1 | Log scraping only | SQLite logging built in (databaseConfig.mtl), one file per sim engine, aggregate-aware | UG52 1.2.2 p87-88; RN p9-10 | Candidate machine-readable verification channel (headless goal) | Y-14 |
| E2 | Batch mode .bsn | Same; single sim engine; seed pinned from batch file | UG52 7.10 p269-271 | Seed-pinned repeats for Phase 3 without hand-driven runs | Y-14 |
| E3+E4 | Load distribution by GUI; no profiler | gui-runtime-scheme / remote-attachment-scheme scenario keys (single engine - ignore); MAK Remote Debugger + Tracy new | UG52 Table 20 p354; RN p3, p73 | Read the debugger doc before the next behavioural probe | N |

## F. C# behaviours that encode 5.0.2 semantics (re-verify on 5.2; proving test)
- Type-map template names via the SMS chain: ObjectTypeResolver/UnitTypeMap/TypeMapSelfTest
  -> PREDICTED BREAK: 5.2d EntityLevel objectType is 7-field (superType dropped, APIMG
  DtObjectType removal); ObjectTypeResolver.cs :136/:150 `parts.Length != 8` indexes zero
  templates. Fix: accept 7 or 8 fields (kind==11 replaces superType). Expected result
  30/31 same-named + Fire Support Team tie; AR Scout falls to Ground_Aggregate
  (DECISION_EVIDENCE Y-8). CONFIRMED 2026-09-03 exactly as predicted (sec H). Then the
  creation-line gate.
- Route-by-uuid with 99-char names (PlanAndMoveTo setControlPoint(DtUUID)) -> R9 gate on 5.2.
- TerrainProfile clamping and ElevationAgl defaults (VrfC2SimService :470-531,
  TerrainVertexAuthoring) -> `--terrain-selftest` + Phase 2 altitude tolerance from the
  5.2 terrain (row C1), with blockOnAsynchronousOperations state recorded (row C5).
- TASKCMPLT pairing rule 4 (FanOutTracker/InFlightTracker) -> `--fanout-selftest`,
  `--report-selftest`; live: 3/3 TASKCMPLT on R9-52. Completion semantics change per D2.
- Census placeholder encodings -> tools/analysis/run_census.py re-gated on a 5.2 log.
- FFRTC frame_gaps gate -> tools/analysis/frame_gaps.py re-baselined on 5.2 (C5 knobs).
- vrfSim.log watcher path (RunnerLib/RunC2SimScenario) -> row A5 before any run.
- appsettings Protocol/Federation/FomModules/FedFileName -> row A2; LaunchVrf.ps1 -> A6.

## H. Phase 1 compile record (2026-09-03) - what is proven, what is not
- Build axis VRF_API_52 (VrfBridge.vcxproj configs Release | Release-5.2 | Release-5.2-HLA4,
  v145, OutDir build\<Configuration>\); app `dotnet build src\VrfC2SimApp -c Release
  -p:BridgeConfig=Release-5.2` -> bin\Release-5.2\. 0 errors on all three; the ONE predicted
  break (A8) never fired (remoteControlInit52.{h,cxx} = the 5.2d sample as its own TU).
  Start(): 3-arg initializer + config file as the sample (A9), --execName/--fedFileName only
  when non-empty, setMonitorBackendState(true), no setSimTime (A10); drainInput() kept.
- UNPREDICTED: (1) v143 gone -> v145 (CLOSED list). (2) PATH name-binding trap: MAK DLLs
  resolve by NAME on PATH - a 5.2 process dies with FileLoadException unless PATH is prefixed
  per process (vrforces5.2d\bin64;vrlink5.10\bin64;makRti4.6.1\bin); tripwire = VrfBridge.
  NativeStackInfo() logged after Start(), manifested. (3) aerodrome init param (B1): no break.
- Offline gates: 8/8 C# self-tests on both apps (own PATH each). Type map: sec F CONFIRMED
  (2182/2190 5.2d simObjects 7-field). Y-8: data/unit-type-map-52.json re-points the five AR
  Scout rows to Mechanized Platoon/Company (USA Army M2) (PROXY - 5.2d has NO USA scout unit).
  --typemap-selftest picks the table by catalog root; 783/784 checks; the 5.2 table FAILS 5
  rows on the 5.0.2 catalog (negative control - the instrument discriminates).
- 2026-09-03 pm (PREREG_52_LAUNCH_2026-09-03.md): first 5.2 LAUNCH + JOIN proven - log
  "Joined federation MAK-ONE-2025 ... VR-Forces Sim Engine 5.2d"; healthy back-end = 36
  threads (new baseline). Path: scripts/LaunchVrf52.ps1, UG52 4.1.2 INDEPENDENT mode (every
  5.0.2 vrfLauncher invocation is INVALID on 5.2; combined mode needs a GUI-saved connection).
  BLOCKER found+fixed: the RTI 5.0.1 installer left an ELEVATED 5.0.1 rtiAssistant on port
  6003 that version-rejects EVERY 4.6.1 LRC (both stacks; A12). Fix (RTI Ref Manual 5.2.10):
  per-process RTI_ASSISTANT_DISABLE + repo rid config/rid-461-ridconfigured.mtl - no
  assistant, no boot dialog, headless. Prototype zero NOT automatable (keyboard-only stdin).
- TOOL GATE 2026-09-03 pm, RELABELLED by COLDSTART_REVIEW_2026-09-03 R5: CONTROL CHANNEL
  PASSED (RtiProbe-5.2 joins; CreateOne-5.2 BackendCount=1 in 0.1 s + ObjectCreated entityId
  1:<simApp>:N; RunSim-5.2 runs) - prose-only, no file captures; OBSERVATION CHANNEL FAILED
  (WatchVrf-5.2 reflected=0 x4; the vendor-listen control was INVALID; the 5.2 sim log has NO
  creation lines, so entity existence is UNVERIFIED). Facade 5.2 path calls the BASE
  init(DtExerciseConn*, ..., false) for SAMPLE PARITY only - it did NOT change reflected=0
  (sec H "kept true" above is superseded). Lead hypothesis (RESEARCH_52_OBSERVER_DISCOVERY):
  our counter hooks UUID-change callbacks while DtReflectedExtEntityList withholds entities
  under waitForVrfExtendedData; second: silent UNLICENSED mode (A13 DtHaveRtiLicense is now a
  GATE). Env DRIFT: machine MAK_RTIDIR -> 5.0.1 (PATH holds version-agnostic %MAK_RTIDIR%\bin).
  5.0.2 is ARCHIVE (user direction 2026-09-04) - oracle = the 39 RECORDED run dirs.
  Restored: 5.0.2 Release bridge rebuilt v145 (deployed copy open); --sessionId must equal the
  sim engine's; 5.b prototype zero DEMOTED (sec G/COLD_START_MAP "ADOPT" superseded). NOT proven:
  HLA 4, behaviour.

## G. Decisions ledger (canonical IDs; a reply that uses other numbers is wrong)
Evidence: docs/VRF_5.2_DECISION_EVIDENCE.md. Rulings dated 2026-09-03 unless noted.
FACTS of the version drift - changes required, no ruling (user agreed, Y-2 named):
- Y-1 launch: vrfSimHLA1516e.exe direct from bin64 (UG52 4.1.2); LaunchVrf.ps1 5.2 profile
  (folds Y-5). Y-2 federation identity: resolve the shipped MAK-ONE-2025 config exactly as
  the 5.2d sample; C# FomModules EMPTIED (modules additive); overrides = a SECOND file via
  myMultipleConfigFiles. Y-4 --logFileName keeps bin64\vrfSim.log. Y-6 remoteControlInit
  + Start/Tick re-authored on the 5.2d sample loop (drop setSimTime(elapsedRealTime()),
  setMonitorBackendState(true), session id from config, -a/-s ledger kept). Y-10 keep
  MoveAlongRoute (adapter repackages unit move-along as maneuver-along; MoveToLocation
  probe RETIRED, class deleted); Phase 3 prereg carries RN VRF-8977 as competing
  hypothesis. Y-11 unit Move To -> Maneuver To default accepted. Y-12 golden traces with
  Autonomous Actions enabled. 7-field ObjectTypeResolver fix (sec F). 5.b prototype zero
  and 5.d requestTasksAndSetsFor assertion adopted as instruments.
RULED:
- Y-3 settings under C:\MAK\vrforces5.2d: edit AUTHORIZED by the user, but not needed -
  5.2 vrfSim takes --notifyLevel 0-4 and --settingsFile <file> (UG52 Table 11, 5.4.3), so
  verbosity and Y-9 knobs ride the runner command line / a repo-held settings file.
  Edit C:\MAK only for a knob with no CLI path; back up first.
- Y-7 RULED as recommended (2026-09-03 pm): terrain = MAK Earth (online) by default; offline
  is a REQUIREMENT in some settings -> per-fixture PROFILE (1 online / 2 offline-cached, no
  features / 3 offline-authored .earth, full parity / 4 shipped USGS N34W117); each profile is
  its own baseline, never compare traces across profiles. Full text: DECISION_EVIDENCE Y-7.
- Y-8 SMS root EntityLevel.sms accepted; 7-field fix; the three types with no 5.2d
  equivalent (AR Scout, Mobile Irregular, Mobile Light Infantry): user says PICK
  SUBSTITUTES from the catalog, record them in data/unit-type-map.json with the source
  file cited; oracle formation overrides dropped (stock .frm, lower-case names).
- Y-9 repeatability knobs: blockOnAsynchronousOperations applies ONLY to fixed-frame
  scenarios and makes the frame wait for terrain/feature/path-planning/navigation
  operations (UG52 App. C) - with online terrain that is tile fetch latency at start and
  at each new tile, so golden runs are slower to START, not slower to simulate; the 0.27x
  ratio on COA-STP1 was entity-count load, not this knob. Seed pinning costs nothing.
  Default: knob ON + seed for golden/prereg runs, OFF (vendor default) for exploratory
  probes, always stated in the run header. User asked "probing really slowly?" - no.
- Y-13 accepted: vendor Ignore-Roads default for tracked vehicles, near-distance 15 m,
  no per-soil caps; traces differ from 5.0.2 by design; prereg records all three.
- Y-14 accepted: SQLite logging evaluated in Phase 2; batch mode REJECTED for interface
  runs ("Batch mode is read-only", UG52 7.10).
- NETN-ETR: RECORD ONLY (translator supports 10 interactions, drops StartWhen/Why/Path/
  MoveType, no formation/spacing knobs). 5.c/5.e/5.h recorded. 5.g PRC authoring DEFERRED.
- Y-15 UNIT REPRESENTATION LEVEL RULED as recommended (2026-09-03 pm): hybrid = TWO PROFILES
  selected by the order's echelon/scale - EntityLevel + authored doctrinal Lua unit tasks for
  company-and-below COAs; AggregateTacticalLevel for battalion+ or entity counts that make
  FFRTC crawl. No runtime aggregate/disaggregate exists (UG52 13.7). Authoring order:
  attack-to-objective family first. Profile = fixture/runner setting (SMS + terrain + type
  map). STP vocabulary: docs/STP_TASK_VOCABULARY_2026-09-03.md. Full text: DECISION_EVIDENCE Y-15.
- Y-16 PROTOCOL: HLA 4 (IEEE 1516-2025) needs MAK RTI 5.0; RTI 5.0.1 INSTALLED 2026-09-03
  (C:\MAK\makRti5.0.1; env vars untouched, runner sets them per process; licensing VERIFIED
  only at the first HLA 4 join). Bridge has the protocol build axis (Release-5.2 = 1516e on
  4.6.1, Release-5.2-HLA4 = HLA4 on 5.0.1); migration gates run on 1516e (one variable); HLA 4
  is its own phase with prototype zero on remoteControlHLA4.exe first. Full text: DECISION_EVIDENCE Y-16.
- Carried: MAK KB check; hostile nation option; licence 2026-09-15; 5.g PRC authoring.
