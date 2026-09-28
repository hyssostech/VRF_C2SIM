# AUDIT: which settled decisions the code carries (2026-09-28)

Asked by the owner on 2026-09-28: "How much of the settled decisions are not reflected in code?" One row per decision,
verified by reading the code and by running every offline self-test of the deployed exe; a doc saying something is built
counts for nothing here. Basis: main ff60f6f. Its src/, scripts/, data/, tests/ and tools/ are byte-for-byte the deployed
build 84c4f62 (`git diff --stat 84c4f62 ff60f6f` over those trees is empty). Main moved to 699552c WHILE this audit ran
(the C1d merge); the one row that changes says so. The standard is AUDIT_REPEATED_INSTRUCTIONS_2026-09-28's "IN CODE"
column: EMBEDDED = a code line AND a self-test or suite check that pins it, both cited.

## 1. The count

92 decisions audited: 37 E embedded, 27 P partial, 6 X not embedded, 1 I in progress, 10 S superseded, 11 A
process-only. The 27 P split into 15 with a code limb missing (P-code) and 12 whose code exists with nothing that pins it
(P-pin). Four more rows are listed but not counted: the three RL-UNVERIFIED ids (no owner words - not rulings) and the
parked time multiplier (never ruled).

The answer: 71 of the 92 still bind code (92 less 11 process-only, less 10 superseded). Of those 71, 37 are in the code
and pinned; 12 are in the code but unpinned; 22 are not fully in the code (15 P-code, 6 X, 1 I) - 31 percent.

X - not embedded (6):
- RL-20260914-04 - battalion taskees never fan out to companies (SubordinateFanOut is false, set nowhere); the vendor
  filter test run he approved has no record.
- Y-9 - blockOnAsynchronousOperations ON + a pinned seed for golden runs: 0 hits anywhere.
- 5.d - the requestTasksAndSetsFor startup assertion adopted on 2026-09-03: 0 hits.
- G6 - an unmapped type silently becomes a Tank under RealTemplates, and --translator-selftest pins that default.
- G-guard - no refusal for types that cannot aggregate (crowds, convoys, carrier air wings, animal herds).
- FILE-LOAD (PRE-LEDGER, 2026-09-02, "low pri" in his words) - init + order from files without the server: absent.

P-code - a code limb missing (15):
- RL-20260902-01 - the fidelity table runs only through the Demo overlay and RunScenario.sh; the compiled and base
  default is RealTemplates, where every company is a Tank Company (USA), hostile included.
- RL-20260903-01 - hold-objective verbs (SECURE, OCCUPY, SEIZE, RETAIN, BLOCK, DEFEND, GUARD, CRESRV) and CLRLND fall
  back to bare movement; the aggregate verb set is not started.
- RL-20260906-02 - AtOrder only through the Demo overlay and RunScenario.sh; the base default hydrates the whole ORBAT.
- RL-20260913-03 - the stall window is 240 wall s / 360 sim s, not the 120 simulated s he approved.
- RL-20260920-01 - item 1: both runner entry points still default to Mojave init/order pairs, not Iron Storm.
- RL-20260921-01 - the platoon ring is 350 m against the approved "about 300 m" (the ledger itself holds this open).
- RL-20260927-06 - the tasked-echelon rule lives only in the test runner; the STP-driven demo path runs EntityLevel for
  any order, Iron Storm's above-battalion one included.
- RL-20260928-01 - the "no downward override" refusal likewise exists only in the test runner.
- Y-2 - appsettings.json still names CWIX-2024 and three FOM modules; only the runner and the Demo overlay clear them.
- Y-3 - the --settingsFile delivery path for sim knobs does not exist (--notifyLevel does).
- Y-7 - offline terrain profiles 2, 3 and 4 do not exist; only MAK Earth (online) and the aggregate terrain.
- Y-14 - the SQLite logging evaluation was never started.
- Y-15 - no authored doctrinal Lua unit task exists; the profile choice exists only in the runner.
- R-AUTHORING-IN-SCOPE (PRE-LEDGER, 2026-07-17) - no entity-level template authored; PROXY rows remain.
- 5.0.2-ARCHIVE (PRE-LEDGER, 2026-09-04) - RunC2SimScenario.ps1 -VrfProfile still defaults to 5.0.2.

P-pin - in the code, nothing pins it (12): RL-20260913-02 (reinstall-proof recipes), RL-20260925-01 (Q3: stall
detection ON lives only in the Demo overlay), RL-20260927-02 (the RUS default), Y-6 (facade loop), Y-12 (autonomy left
on), Y-13 (no road preference), MAP-T10 (sessionId agreement), C1/C2 + G1 (compose on by default), C4 (no aggregate
formation), C6 + G3/G4 (no reorganize, no formation wait), C8 (routes addressed by the real uuid), C10 (no direct
create).

I - in progress (1): RL-20260928-02 - identity by uuid (C1d, fix/identity-by-uuid). MERGED to main at 699552c during
this audit, with its pins (CST p15, NameSelfTest); not deployed (the exe is still 84c4f62) and not live-proven (G1-2).
Counted at 699552c it is E: 38 E, 0 I.

## 2. The table

Legend. Svc = src/VrfC2SimApp/VrfC2SimService.cs; VS = VrfSettings.cs; AJ = appsettings.json; AD = appsettings.Demo.json;
TCP = TimedCompletionPolicy.cs; TDP = TaskDispatchPolicy.cs; CPop = ContainerPopulation.cs; CComp = ContainerComposition.cs;
UTM = UnitTypeMap.cs; RST = RulingsSelfTest.cs; TAS = TimerAnchorSelfTest.cs; EDS = EngageDoctrineSelfTest.cs;
VCS = VertexChainSelfTest.cs; CST = ContainerSelfTest.cs; TMS = TypeMapSelfTest.cs; ROS = RouteShiftSelfTest.cs;
OSS = OsmSelfTest.cs; PRS = PlacementReclampSelfTest.cs (all in src/VrfC2SimApp/, like every other bare .cs name);
Fac = src/VrfFacade/VrfFacade.cpp; RTT = tests/RunnerTurnaround.Tests.ps1; RLib = scripts/RunnerLib.ps1;
RCS = scripts/RunC2SimScenario.ps1; RSh = scripts/RunScenario.sh; LV52 = scripts/LaunchVrf52.ps1;
SI52 = scripts/StartInterface52.ps1. "--x" = a VrfC2SimApp self-test switch, run today (sec 6). "+n" = the rest of
the evidence is in that decision's note under its table.

### 2a. The live ledger, docs/RULINGS.md (8)

| id | date | what the code must do | where | STATUS | evidence |
|---|---|---|---|---|---|
| RL-20260921-09 | 09-21 | TEMPORARY: end at start + Duration; a late unit completes on arrival | TCP | E | TCP:75-350; RST:600 (t1-t21); TAS:141-237 |
| RL-20260927-02 | 09-27 | (1) hostile side RUS; (2) populate containers, not proxies | VS, CPop | P-pin CLOSED 2026-09-28 by 9999b35 | CPop:138 + CST; RUS only VS:90, AJ:30 +n |
| RL-20260927-03 | 09-27 | init: every unit an empty container on the map; tasked ones filled | Svc | E | Svc:2906-2908, :1763-1797; CST:361-390, :649-655 +n |
| RL-20260927-04 | 09-27 | D-1..D-8 as recommended; D-2: author the missing US types (C2) | CComp, CPop, data | E | CST:222, :575-589, :645, :780-796 +n |
| RL-20260927-05 | 09-27 | "Q1 a": an unfinished mover is OVERDUE when the gate window expires | TCP, TDP | E | TCP:253, TDP:458; TAS:141-166 (t4) |
| RL-20260927-06 | 09-27 | model set by the highest TASKED echelon; above BN aggregate | RLib, app | P-code CLOSED 2026-09-28 by b5c6d02 | RLib:2143, RTT:2123 (8z); not in app/SI52 +n |
| RL-20260928-01 | 09-28 | D2 as recommended: no downward override; test HQ re-coded COY | RLib, data | P-code CLOSED 2026-09-28 by b5c6d02 | RLib:2143, RTT:2123-2160; demo path none +n |
| RL-20260928-02 | 09-28 | identity = uuid: create under startingUUID, bind ObjectCreated by uuid | Fac, Svc | I | branch fix/identity-by-uuid (C1d) +n |

Notes:
- RL-20260927-02: (2) ContainerPopulator CPop:138, pinned by CST p1-p15. (1) RUS is the compiled and base default (VS:90,
  AJ:30) and no test reads it; data/unit-type-map-52-aggregate.json holds 19 RUS and 19 BLR hostile rows and its note
  (:12) still says the hostile nation is "not decided here".
- RL-20260927-03: on the aggregate set the start refuses without AtOrder (Svc:2906-2908); at entity level the same rule
  is RL-20260906-02's, which only the overlays apply - counted there, not twice.
- RL-20260927-04: CComp:186 (variants and the C1b guard), CComp:499 (resolver); D-6 CPop:517; D-1 in
  data/unit-composition-52-aggregate.json:30-38. Pins: CST:222 (D-8), :575-589 (D-6), :645 (D-5), :780-796 (p13). The
  authored variant (C2) is off by default (AJ:115) and not live (G1b queued).
- RL-20260927-06: the app has no echelon rule (0 hits in src), SI52 sets no model set and AD:12 says EntityLevel, so an
  STP-driven Iron Storm order (brigades, a division) is simulated at entity level. Since b5c6d02 (D2b): the app refuses
  that order at receipt (RUNBOOK sec 11k).
- RL-20260928-01: RTT:2123 (8z) pins the refusal and RTT:2156-2160 the re-coded 1.BdeHQ; nothing refuses on the demo
  path (AD:12). "Go for the server" is an authorisation (process). Since b5c6d02 (D2b): OnOrder refuses it.
- RL-20260928-02: at ff60f6f units are created without a startingUUID (Fac:928-944) and bound by name. The lane merged to main at
  699552c during this audit with its pins (CST p15, NameSelfTest); not deployed (the exe is 84c4f62); G1-2 owed.

### 2b. The archive, docs/RULINGS_ARCHIVE.md (29, plus 3 that are not rulings)

| id | date | what the code must do | where | STATUS | evidence |
|---|---|---|---|---|---|
| RL-20260902-01 | 09-02 | best fidelity: every unit as its correct VR-Forces type | UTM, VS, AJ | P-code (overlay pinned 9999b35; base open) | UTM:77, TMS 610/0; base default RealTemplates +n |
| RL-20260903-01 | 09-03 | all the tasking STP can represent, not only move | VerbMapping.cs | P-code | :110-122, --verb 51/0; hold family bare-moves +n |
| RL-20260906-01 | 09-06 | a demo app that runs without the test harness | MakRuntime.cs, SI52 | E | MakRuntime.cs:29; Program.cs:210-218; RTT:3425-3440 |
| RL-20260906-02, C13 | 09-06 | simulate only the taskees; the init is ORBAT context | VS, AD, RSh | P-code (overlay pinned 9999b35; base open) | VS:205 AtInit; AtOrder only AD:28, RSh:333 +n |
| RL-20260907-01, C15 | 09-07 | a unit's completion from its own arrival evidence | ArrivalPolicy.cs | E | :230-280; pin :294 (--arrival 74/0) |
| RL-20260907-02, C14 | 09-07 | spread co-located units at startup, 700 m rings | DeStacker.cs, AJ | E | :54, AJ:38-39; RST:3001-3004; --destack 141/0 |
| RL-20260913-01 | 09-13 | leg checks reach users as C2 reports, not logs | Preflight/ | E | PreflightReports.cs; ROS:383-389; OSS:594 +n |
| RL-20260913-02 | 09-13 | customisations survive a VR-Forces reinstall | tools/sms | P-pin | Deploy-C2SimSms.ps1:1-14 (C:\C2SIM\vrf-sms); no pin |
| RL-20260913-03, C16 | 09-13 | stall: report + abort, N = 120 SIMULATED s, no re-tasking | StallPolicy.cs | P-code (shipped window pinned 9999b35; number open) | :50, --stall 66/0; window 240 wall s +n |
| RL-20260914-01 | 09-14 | TASKABRT is what STP sees for a stalled unit | Svc | E | RST:670-685 (t4), :704-715 (t6) |
| RL-20260914-02 | 09-14 | R1 MapGraphicID; R2 performer's position; R3 objective; item 4 | TaskGeometryResolver.cs | E | :167; RST:1957, :1799, :1903, :296 |
| RL-20260914-03 | 09-14 | R1 embedded Location (V4b); R5 entity first + user option | TaskGeometryInterpretation.cs | E | :58, RST:68; VS:1166, RTT:1979 |
| RL-20260914-04 | 09-14 | R6: test the vendor filter, then fan battalions out to companies | Svc, VS | X (OFF pinned 4d488ab; retire or build: owner) | VS:617 SubordinateFanOut=false, set nowhere +n |
| RL-20260914-05 | 09-14 | Q1 supersede aborts; Q2 sim clock; Q4 malformed; Q5; Q7 | TDP, SimClockTracker.cs | E | TDP:331, :437; RST:2294, :1836, :1412 +n |
| RL-20260914-06 | 09-14 | STP-809 back-end state; E3 task cycles refused with an abort | VrfFacade.h, TDP | E | VrfFacade.h:406-415, TDP:368; RST:1366, :2553 |
| RL-20260915-01 | 09-15 | convert the four 5.0.2-only tools to 5.2 | tools/*.csproj | E | BridgeConfig; tools/ResetVrf/Program.cs:113 |
| RL-20260920-01 | 09-20 | 1 Iron Storm; 2 8080/61613; 3 route shift; 4 extent; 6 quit | runner, SI52, VS | P-code | item 1: Mojave defaults; 2-6 pinned +n |
| RL-20260920-02 | 09-20 | Iron Storm: A, a demo-scale cut, then B, the aggregate profile | data, Svc | E | IRONSTORM_CUTA_*.xml, C1; --stpexport, --populate |
| RL-20260921-01 | 09-21 | de-stack A + C; T14 800 m nudge (not the 0.9 nav bar) | DeStacker.cs, data | P-code | RST:2960-3045; ring 350 m vs "about 300 m" +n |
| RL-20260921-02 | 09-21 | close rtiAssistant; path A; demo clock FAST; callstack; MAK case | ops, fixture | A | actions +n |
| RL-20260921-03 | 09-21 | abort a move that never moved (label withdrawn by RL-20260921-05) | - | S | by RL-20260921-05, -09; caveat pinned RST:961-981 |
| RL-20260921-04 | 09-21 | selected "Abandon them" for that abort's successors | - | S | carried by RL-20260925-01 Q2 (RST:704-715) +n |
| RL-20260921-05 | 09-21 | timed completion only without movement; stuck is not done; late is no abort | TCP | E | RST:600-725 (t1-t7); TAS |
| RL-20260921-06 | 09-21 | the placement re-clamp stays; its gate and tally were the defect | PlacementReclampPolicy.cs | E | :75, :207; PRS:291-474; RTT:1443 |
| RL-20260921-07 | 09-21 | a movement task whose unit stays put aborts; not every task moves | Svc | E | Svc:6366-6456; RST:961-981 (t21) +n |
| RL-20260921-08 | 09-21 | no rule: plain words; read the record before asking | - | A | process |
| RL-20260925-01 | 09-25 | Q1 scope; Q2 abandon a stuck unit's follow-ons; Q3 stall ON in demo; Q4 | TCP, AD | P-pin CLOSED 2026-09-28 by 9999b35 | RST:600-767, TAS; Q3 only AD:49 +n |
| RL-20260926-01 | 09-26 | unit ATTACK = advance + fire at will; BREACH not simulated; CNFPSL held | TDP | E | TDP:128, :165; EDS:30-71, :117-166 |
| RL-20260927-01 | 09-27 | lone platform Move To per vertex; one OSM pre-flight per set | VertexChain.cs, Preflight/ | E | VCS:41-64, :451-466; OSS:148-155 +n |
| RL-UNVERIFIED-DIGUY01 | - | claimed: dropping DI-Guy rows is a fidelity regression | - | not a ruling | no owner words |
| RL-UNVERIFIED-MAK01 | - | claimed: no questions to MAK | - | not a ruling | RL-20260921-02 item 5 opened a case +n |
| RL-UNVERIFIED-NAV09 | - | claimed: nav connectivity >= 0.9 is the launch gate | - | not a ruling | tools/navdata/nav_gate.py +n |

Notes:
- RL-20260902-01: the table path is UTM:77, pinned by TMS (610/0). But VS:77 and AJ:28 default to RealTemplates, the
  5.0.2 parity dispatch in which every company is a Tank Company (USA), hostile included (AD:8), with the 5.0.2 map
  (AJ:31). FidelityTable runs only through AD:9-10 and the RSh:332 export; RCS:3971-3979 never sets the mode; neither
  overlay is pinned. The 2026-07-22 choice of RealTemplates as the compiled default (HANDOFF_2026-09-01_R9_COMPLETE
  CLOSED list) is what still stands in the code.
- RL-20260903-01: VerbMapping.cs:110-122 builds Move, Attack, Breach, Reconnoiter, Escort, HoldInPlace,
  FollowAndSupport and PassageOfLines; SECURE, OCCUPY, SEIZE, RETAIN, BLOCK, DEFEND, GUARD, CRESRV and CLRLND fall back
  to a bare move; the aggregate verb set (PLAN_MOVEMENT row V1) is not started.
- RL-20260906-02: no CreationPolicy key in AJ, so a base run creates every unit in full; AtOrder only through AD:28 and
  RSh:333; the aggregate set forces it (Svc:2906-2908). The policy is pinned by --dispatch-readiness (52/0); the
  entity-level default is not.
- RL-20260913-01: Preflight/PreflightReports.cs and RouteShift.cs send the findings as C2SIM reports; pinned by
  ROS:383-389 and OSS:594. STP discards them on receipt (STP-800, an STP-side ticket).
- RL-20260913-03: the window is 240 WALL s by default, 360 SIM s on the sim clock (VS:418-419; StallPolicy.cs:357-360,
  the 2026-09-13 calibration), not the 120 simulated s he approved; no ledger line records his acceptance.
- RL-20260914-04: VS:617 SubordinateFanOut=false and no json, script or export sets it; the filter test run has no
  record past TASK_VOCABULARY_ASSESSMENT_2026-09-14.md:1271. Probably overtaken by RL-20260926-01 and RL-20260927-06.
- RL-20260914-05: SimClockTracker.cs:206 (TaskClockAxis); VS:757 sim clock, :774 TASKABRT; Q7 rollback pinned RST:1623.
- RL-20260920-01: item 1 - RSh:61-66 defaults to R9_Mojave_Empty_52_NavAO with the COA-STP1 Mojave pair; RCS:918,
  :1065, :1074 to firstexperience with the R9 Mojave pair. Items 2-6 pinned: RTT:3609-3618 (11i), ROS:478-498,
  RouteExtentSelfTest.cs:61, RTT:2813 (10).
- RL-20260921-01: A + C pinned by RST:2960-3045 and --destack; the nudge is data/IRONSTORM_CUTA_CHANGES.md:150 (kept at
  :494). The platoon ring is 350 m (EchelonSpacing.cs:97, derived from a 320.9 m formation span) against the approved
  "about 300 m"; the ledger holds that gap open.
- RL-20260921-02: all five items are actions; item 3 lives in the fixture's frame mode (DEMO_RUNBOOK.md:77-86) - no
  interface code sets the clock.
- RL-20260921-04: his typed question back (a special class of error for no movement, against movement that falls short)
  is still unanswered.
- RL-20260921-07: per the ledger, the Jira STP-857 correction is drafted and not posted (process, awaiting him).
- RL-20260925-01: Q1 RST:600-725 and TAS; Q2 RST:704-715; Q4 RST:760-767; the re-clamp limb PRS. Q3 lives only in AD:49
  (VS:411 is false) and no test reads it.
- RL-20260927-01: VertexChain.cs:88, VS:600, AJ:77; Preflight/OsmFeatures.cs, VertexNudge.cs, ModelSetRules.cs. Pins:
  VCS:41-64 (v1), :451-466 (v5 reads AJ and AD), OSS:148-155 (shipped defaults), RTT:2715 (9b).
- RL-UNVERIFIED-MAK01: HANDOFF_2026-09-01_R9_COMPLETE.md:160-161 still states the prohibition.
- RL-UNVERIFIED-NAV09: the bar lives in tools/navdata/nav_gate.py and corridor_gate.py:60 (tests/NavGate.Tests.ps1); PLAN_MOVEMENT sec 4
  retires it as a launch bar after G1.

### 2c. The 2026-09-03 Y items, VRF_5.2_DECISION_EVIDENCE.md and DIFF sec G (17 ids, 16 rows)

| id | date | what the code must do | where | STATUS | evidence |
|---|---|---|---|---|---|
| Y-1, Y-5 | 09-03 | launch vrfSimHLA1516e from bin64; LaunchVrf52 replaces the launcher | LV52 | E | LV52:1-11, :1045-1046; RTT:960-975 (8g) |
| Y-2 | 09-03 | join via the MAK-ONE-2025 config; C# FomModules emptied | Svc, AJ | P-code (overlay pinned 9999b35; base open) | Svc:1388-1403 only if ConfigFileIdentity +n |
| Y-3 | 09-03 | no C:\MAK edit: --notifyLevel and a repo-held --settingsFile | LV52 | P-code | LV52:1046, RTT:966; --settingsFile 0 hits |
| Y-4 | 09-03 | keep the bin64 sim log via --logFileName | - | S | never pass it: LV52:1047-1052; RTT:766-800 (8f) |
| Y-6 | 09-03 | facade loop per the sample: no setSimTime; monitor back-end state | Fac | P-pin | Fac:659-662, :756-763; native, no offline check |
| Y-7 | 09-03 | MAK Earth (online) by default; offline: four per-fixture profiles | tools/FixtureGen | P-code | build_fixture.py:574-606; profiles 2-4: 0 hits |
| Y-8 | 09-03 | EntityLevel.sms root; 7-field types; catalogue substitutes | ObjectTypeResolver.cs | E | :270-280; TMS 610/0 (TypeMapSelfTest.cs:75-119) |
| Y-9 | 09-03 | golden runs: blockOnAsynchronousOperations ON + a pinned seed | LV52, fixture | X | 0 hits in scripts, src, tools/FixtureGen, config |
| Y-10 | 09-03 | keep MoveAlongRoute for every MOVE - a SEAT RECOMMENDATION | - | S | by RL-20260927-01; unit limb VCS:54 (v1) +n |
| Y-11 | 09-03 | accept unit Move To -> Maneuver To; no per-entity moves on a unit | VertexChain.cs | E | :88; VCS:41-64 (v1) |
| Y-12 | 09-03 | Autonomous Actions left ON | Svc | P-pin CLOSED 2026-09-28 by 4d488ab | 0 hits for AutonomousActions in src; no guard |
| Y-13 | 09-03 | SMS road defaults; no road preference unless an order asks | Svc | P-pin CLOSED 2026-09-28 by 4d488ab | 0 hits for a navigation-preference send; no guard |
| Y-14 | 09-03 | no batch mode for interface runs; evaluate SQLite logging | - | P-code | batch 0 hits; SQLite 0 hits (never evaluated) |
| Y-15 | 09-03 | two profiles by echelon; authored doctrinal Lua for company and below | runner, Lua | P-code | RLib:2143 (8y, 8z); 0 .lua files tracked +n |
| Y-16 | 09-03 | HLA 4 is its own phase; gates run on HLA 1516e | VrfBridge.vcxproj | E | vcxproj:21-22, :56-62; RTT:960-975, :487 |
| Y-17 | 09-04 | authored lat/lon + AGL; no 10000 m birth; TerrainProfile vertex | PlacementPolicy.cs | E | :59-82, VS:846; PlacementSelfTest.cs:90, :111-123 |

Notes:
- Y-2: Svc:1388-1403 clears the federation, FED file and modules only when Vrf:ConfigFileIdentity is true, which RCS:3971
  and AD:4 set; AJ:23-25 still carries CWIX-2024 and three FOM modules and VS:47 defaults the switch to false. No pin.
- Y-10: never put to him; CLAUDE.md sec 2 now records it as a seat recommendation.
- Y-15: the ATTACK family went the RL-20260926-01 way (advance + fire at will) instead of an authored task;
  RL-20260927-06 refines the echelon rule.

### 2d. The T items of 2026-09-03 (6 rows)

DECISION_EVIDENCE carries no T- items. The T-numbered items of that date are the cold-start roadmap's 18 traps
(VRF_5.2_COLD_START_ROADMAP.md:651-741): vendor facts, not decisions. COLD_START_MAP sec 4 carried four of them under
its OWN numbering - its T10, T14 and T18 are the sessionId, log-copy and requestTasksAndSetsFor points, which are not the
roadmap's T10, T14 and T18 - and DIFF sec G adopted two instruments (5.b, 5.d). Those, plus the NETN-ETR line, are here.

| id | date | what the code must do | where | STATUS | evidence |
|---|---|---|---|---|---|
| 5.b | 09-03 | prototype zero: the vendor's remoteControl binary first | - | S | demoted, VRF_5.2_MIGRATION_DIFF.md:146 |
| 5.d, MAP-T18 | 09-03 | startup: every mapped task/set name advertised (bounded poll) | Fac, Svc | X | adopted DIFF:162; requestTasksAndSetsFor 0 hits |
| MAP-T5 | 09-03 | batch recording refuses a second run | - | A | moot: no batch mode (Y-14) |
| MAP-T10 | 09-03 | the controller's sessionId equals the sim's; a mismatch is silent | LV52, VS | P-pin | LV52:166 = VS:12 = 1; no cross-check |
| MAP-T14 | 09-03 | the sim log is per launch: copy it before a relaunch | LV52 | E | LV52:498; RTT:792 (8f) |
| NETN-ETR | 09-03 | record only, build nothing | - | A | nothing built |

### 2e. DESIGN_ORBAT_TO_VRF_2026-09-06 CLOSED list (C1-C12; C13-C16 are rows in 2b) (10 rows)

| id | date | what the code must do | where | STATUS | evidence |
|---|---|---|---|---|---|
| C1, C2, G1 | 09-06 | compose per the vendor sample, ON by default | Svc, VS | P-pin (default pinned 9999b35; flow unpinned) | Svc:2439-2471; VS:136, AJ:34; order pinned only +n |
| C3, C5 | 09-06 | no template higher-unit for company+; expand pure higher-units | ComposeOrder.cs | E | :11; :44 (--compose 9/0) |
| C4 | 09-06 | AggregateFormation stays OFF | VS | P-pin CLOSED 2026-09-28 by 9999b35 | VS:120; used only when set (Svc:5645, :9497) |
| C6, G3, G4 | 09-06 | no post-attach reorganize; no client formation wait | Svc | P-pin CLOSED 2026-09-28 by 4d488ab | Svc:2439-2471 attaches only; opt-in :9497-9512 |
| C7, G2 | 09-06 | attach in the declared Subordinate order | ComposeOrder.cs | E | --compose "declared order wins" |
| C8 | 09-02 | address by the real VRF_UUID, never a name as a DtUUID | Svc | P-pin CLOSED 2026-09-28 by 4d488ab | Svc:6740-6758; no self-test; RTT:2680 parses logs |
| C9 | 09-06 | no vendor importer sample exists | - | A | a fact |
| C10 | 09-06 | never build sendVrfObjectCreateMsg + initialFormation | Svc, Fac | P-pin CLOSED 2026-09-28 by 4d488ab | 0 hits in src; no guard |
| C11 | 09-06 | the object console at level 4 is the first instrument | VS, lint | E | VS:505; tests/RecordChecks.ps1:675-677 (13d) |
| C12 | 09-06 | lifeforms need the DI-Guy data installed | env | A | installed by him |

Notes:
- C1, C2, G1: --compose pins the attach order and the expand choice (ComposeOrder.cs:44); the ComposeHierarchy default
  and the service's shell + attach flow have no check.

### 2f. ORBAT_LOADING_REQUIREMENTS_2026-09-06 G-items (G1 is row C1, G2 C7, G3/G4 C6, G5 RL-20260928-02) (2 rows)

| id | date | what the code must do | where | STATUS | evidence |
|---|---|---|---|---|---|
| G6 | 09-06 | reject or flag an unmapped type under RealTemplates (low priority) | UnitTranslator.cs | X | :109 silent Tank; TranslatorSelfTest.cs:47 pins it |
| G-guard | 09-06 | refuse to compose types that cannot aggregate (UG52 71.4) | compose path | X | 0 hits for crowd, convoy, air wing, herd |

The refuter's name-cap item (11 / 31 characters, renamed on import) is part of RL-20260928-02 (C1c names, then C1d).

### 2g. PLAN_MOVEMENT_2026-09-27 CLOSED lines not covered above (2 rows)

Its other nine lines map to RL-20260928-02, RL-20260927-01, RL-20260927-02/-04, RL-20260927-03, RL-20260927-06,
RL-20260927-05, C8, R-HOSTILE-NATION and RL-20260920-01.

| id | date | what the code must do | where | STATUS | evidence |
|---|---|---|---|---|---|
| DOCS-FIRST | 09-01 | vendor docs and samples before any probe; cite both records | CLAUDE.md | A | prereg form: RecordChecks.ps1:650-651 (13d) |
| NOT-BROKEN | 09-01 | a suspected VR-Forces bug means we misuse it | - | A | process |

### 2h. Pre-ledger decisions cited in src/ or docs/ (PRE-LEDGER; his words unless marked) (19 rows + 1 not counted)

| id | date | what the code must do | where | STATUS | evidence |
|---|---|---|---|---|---|
| R8 | 07-13 | 5.0.2: stacked spawns are not the blocker; no de-stack | - | S | for 5.2 by RL-20260907-02 (AJ:37) |
| R-ENTITY-LEVEL | 07-17 | stay entity level | - | S | by Y-15, RL-20260914-03, RL-20260927-06 |
| R-SURFACE-PROXY | 07-17 | announce every proxy substitution downstream | SubstitutionAnnouncer.cs | E | :20, VS:106; ReportSelfTest.cs:275, :290 |
| R-AUTHORING-IN-SCOPE | 07-17 | author missing templates; proxies only as interim | tools/sms, data | P-code | aggregate C2 only; entity level none +n |
| MULT-20X | 07-18 | set 20x through the remote setTimeMultiplier | - | S | withdrawn 2026-09-02; VS:653 is 1 |
| AMEND-4a2 | 07-19 | a 5.0.2 run's MOVED score needs 25 m closer | - | A | an archived run's scoring rule |
| HARDEN-FIRST | 07-22 | stop live runs, harden the launch first | - | A | process |
| RTI-UNTOUCHED | 07-22 | never stop rtiexec, rtiForwarder or rtiAssistant | StopVrf52.ps1 | E | :425-426; RTT:522-524, :3035 (10f) |
| NAVAREA-OFF | 09-01 | keep the project-made 5.0.2 NavArea disabled | env | A | SharedData 16 only |
| R-HOSTILE-NATION | 09-02 | hostile nation is a setting; Chinese preferred, RUS acceptable | VS, UTM | E | VS:79-90; TMS:253, :319 +n |
| MOVE-TO-5.2 | 09-02 | migrate to VR-Forces 5.2d, docs first | RSh, vcxproj | E | RSh:40; RTT:485-520 (8b), :960-975 (8g) |
| MIN-DATA | 09-02 | install the minimal data set, no DI-Guy | - | S | DI-Guy installed (C12) |
| TERRAIN-0902 | 09-02 | terrain MAK Earth (online) | - | S | by Y-7 |
| FILE-LOAD | 09-02 | init + order from files, no server (low priority) | Program.cs, Svc | X | 0 hits; --parse-init/--parse-order only parse |
| SETTLE-EVIDENCE | 09-02 | early exit needs a post-completion report per taskee | RLib | E | RLib:379-395; RTT:237 (4b) +n |
| JC-1 | 09-02 | the init's type wins only where the table covers it | UTM | E | UTM:66-69; TypeMapSelfTest.cs:259-287 +n |
| JC-2 | 09-02 | a nation with no content refuses to start | UTM | E | UTM:301-342; TMS:319 +n |
| NO-OLD-BITS | 09-03 | 5.2 runs on MAK RTI 5.0.1 | RCS, vcxproj | E | RCS:906, vcxproj:50; RTT:487, :520 (8b) |
| 5.0.2-ARCHIVE | 09-04 | nothing on the live path launches 5.0.2 | RCS | P-code CLOSED 2026-09-28 by 9999b35 | RCS:400-401 defaults to 5.0.2 +n |
| MULTIPLIER | 09-02 | NOT COUNTED - parked, "ok on the multipliers for now" | VS | - | VS:653 = 1; no runner sets it +n |

Notes:
- R-AUTHORING-IN-SCOPE: the aggregate US types were authored offline (package C2, variant off by default, AJ:115); at
  entity level nothing was authored - data/unit-type-map-52.json keeps PROXY rows, PRC is AUTHORED_PENDING and refuses
  to start (TMS:319), and 5.g PRC authoring is deferred.
- R-HOSTILE-NATION: OpposingNation (VS:79-90, default RUS) is pinned by TMS:253; PRC refuses to start for want of
  content (TMS:319) - the Chinese preference waits on 5.g.
- SETTLE-EVIDENCE: author not recorded. JC-1, JC-2: the supervisor's provisional calls, never put to him.
- 5.0.2-ARCHIVE: RCS:400-401 -VrfProfile still defaults to 5.0.2 (RSh:40 is 5.2) and nothing refuses a live 5.0.2 run;
  the 5.0.2 install is gone from this machine.
- MULTIPLIER: VS:653 is 1 and applied only above 1 (Svc:1050-1051). PREREG_COASTP1_RUNG1_BOUNDED_2026-09-02:194 and
  RUNG2:163 record 1x as settled; AUDIT_REPEATED item 13 records it parked. Not counted.

## 3. The pattern: code with no pin is where the record rots

1. OVERLAY-ONLY decisions. The value the owner chose lives in appsettings.Demo.json, a RunScenario.sh export or
   RunnerLib, while VrfSettings.cs and appsettings.json carry the 5.0.2 value: RL-20260902-01 (RealTemplates, VS:77),
   RL-20260906-02 (AtInit, VS:205), Y-2 (CWIX-2024 + modules, AJ:23-25), RL-20260925-01 Q3 (VS:411 false),
   RL-20260927-06 and RL-20260928-01 (AD:12 EntityLevel, no echelon rule in the app). So a bare exe or a direct
   RunC2SimScenario.ps1 call runs RealTemplates + AtInit, while RunScenario.sh and StartInterface52 run what was chosen;
   DeStackSelfTest.cs:910 even records RealTemplates as "what a RUNNER-LAUNCHED app uses". The repo already has the
   cure and uses it four times - a self-test that loads the shipped json files and asserts the chosen value
   (RST:2990-3045, VCS:451-466, ROS:478-498, OSS:148-155) - and none of the values above has one.
2. "DO NOT" decisions kept by absence: C4, C6 + G3/G4, C10, Y-12, Y-13. Nothing fails if a lane puts the call back. The
   model is PlacementSelfTest.cs:90-123 ("no 10000 m birth anywhere") - the one negative rule that is pinned, and the one
   the owner had to repeat about twelve times before it was (AUDIT_REPEATED item 3).
3. WIRING OUTSIDE THE PURE POLICIES. The self-tests pin pure functions. The service's own wiring - C8's route addressed
   by the real uuid (Svc:6740-6758), C2's compose flow (Svc:2439-2471) - and the native facade (Y-6) are proven only by
   live runs.
4. THE PINS ARE NOT IN THE SUITE. RTT runs no C# self-test; the 27 switches run only at a deploy (RUNBOOK sec 9), so a
   pin catches nothing between deploys. --populate-selftest's source checks read the CHECKOUT, not the build: today's
   one FAIL (CST:646-648) came from a lane's C1d edits in main's working tree, not from the deployed code.
5. TESTS THAT PIN THE OPPOSITE. --translator-selftest pins G6's silent Tank default (TranslatorSelfTest.cs:47);
   DeStackSelfTest's measured counts are taken under RealTemplates.
6. NAMED NUMBERS THAT DRIFTED. RL-20260913-03 (120 sim s became 240 wall s) and RL-20260921-01 (about 300 m became
   350 m) were changed for sound, measured reasons, but his approved figure was never replaced by his word.

## 4. The three cheapest fixes

1. ONE SHIPPED-PROFILE SELF-TEST, beside RST:2990-3045 (about 40 lines, no behaviour change): load AJ + AD and parse
   RSh's exports; assert TypeMappingMode=FidelityTable, CreationPolicy=AtOrder, ConfigFileIdentity=true,
   StallDetection=true (demo), OpposingNation=RUS, ComposeHierarchy=true, AggregateFormation empty. Moves RL-20260925-01,
   RL-20260927-02 and C4 to E, and pins the overlay limb of RL-20260902-01, RL-20260906-02, Y-2 and C1/C2.
2. ONE SOURCE-GUARD CHECK (the CST:632-700 style, or an RTT scan): src sends no AutonomousActions or navigation
   preference, never calls sendVrfObjectCreateMsg, and calls ReorganizeAggregate only inside the opt-in
   AggregateFormation=auto handler. Moves C6 + G3/G4, C10, Y-12 and Y-13 to E.
3. IRON STORM AS THE RUNNER DEFAULT (RL-20260920-01 item 1, asked four times - AUDIT_REPEATED item 12): RSh:61-66 and
   RCS:918, :1065, :1074 name the cut-A pair (data/IRONSTORM_CUTA_Initialization.xml, data/IRONSTORM_CUTA_Order.xml)
   and its fixture, plus an RTT check on the defaults; D2 (8z) then picks the aggregate set on its own. Needs him to
   name the default fixture.

Not cheap, and the most consequential gap: RL-20260927-06 and RL-20260928-01 on the demo path. The app itself should
refuse (or warn loudly) when a tasked unit is above battalion and Vrf:ModelSet is EntityLevel - a C# port of RLib's
Get-EchelonRank and refusal, pinned like 8z.

## 5. Where the record and the code disagree - for the owner

- RL-20260902-01: base default RealTemplates (the 5.0.2 parity dispatch) vs the fidelity table. Flip VS:77, or keep the
  overlays and pin them (fix 1)?
- RL-20260927-06 and RL-20260928-01: the STP-driven demo path has no echelon rule; Iron Storm there runs EntityLevel.
- RL-20260913-03: stall window 240 wall s / 360 sim s (calibrated 2026-09-13: 3 of 3 freezes caught, 0 of 6 false
  alarms) vs his "120 simulated seconds". Ratify or change.
- RL-20260921-01: platoon ring 350 m (derived from the 320.9 m formation span) vs "about 300 m". Ratify or change.
- RL-20260914-04: battalion fan-out never switched on; probably overtaken by RL-20260926-01 (attack = advance) and
  RL-20260927-06 (battalions at aggregate level). Retire it or keep it?
- RL-20260921-04: his typed question (why a special class of error for no movement at all, against movement that falls
  short) is still unanswered; RL-20260921-07: the ledger says the STP-857 correction is drafted, not posted.
- MULTIPLIER: two preregs (PREREG_COASTP1_RUNG1_BOUNDED_2026-09-02:194, RUNG2:163) record 1x as settled; AUDIT_REPEATED
  item 13 records it parked; he has not been asked since 5.2d.
- Seat-fixable, no owner needed: the aggregate map's "not decided here" note (data/unit-type-map-52-aggregate.json:12,
  answered by RL-20260927-02); RCS:401 -VrfProfile default 5.0.2; HANDOFF_2026-09-01_R9_COMPLETE.md:160-161 still
  states the unverified MAK prohibition (RL-UNVERIFIED-MAK01).

## 6. Method and instruments

POPULATION: every RL id (40; the 3 RL-UNVERIFIED are listed, not counted); Y-1..Y-17 (Y-1 and Y-5 share a row); the T
items and 5.x instruments above; DESIGN_ORBAT C1-C12 (C13-C16 share the rows of RL-20260906-02, RL-20260907-02,
RL-20260907-01 and RL-20260913-03); the G-items; the PLAN_MOVEMENT CLOSED lines; every pre-ledger decision found cited
in src/ or docs/ (a grep for the named R- entries, for dated owner-direction lines in the handoffs and preregs, and the
HANDOFF_2026-09-01 CLOSED list). One row per decision; a row that carries several ids is counted once.
P-PIN vs P-CODE: P-pin = the code does what was decided and nothing fails if it stops; P-code = a limb of the decision
is absent or differs (a default, a path, a number).
SELF-TESTS, run 2026-09-28 against the deployed exe (src/VrfC2SimApp/bin/Release-5.2/net10.0/win-x64, ProductVersion
1.0.0+git.84c4f62, VrfC2SimApp.exe sha256 ad9ff865..., VrfBridge.dll 5198ac45...), 5.2 PATH prefix, offline: 26 of 27
switches exit 0 (rulings 459, typemap 610, destack 141, osm 139, report 90, arrival 74, preflight 72, routeorigin 70,
stall 66, stpexport 64, dispatch-readiness 52, verb 51, name 49, fanout 43, routeextent 39, liveness 25, translator 22,
parse 19, sequencer 12, compose 9, and terrain, placement, placement-reclamp, routeshift, scripted-task, initgraphics
PASS); --populate-selftest 173 PASS / 1 FAIL / 4 SKIP, the FAIL being the checkout-reading source check of sec 3 item 4
(the committed ff60f6f source holds the string it looks for); the three --disabled arms exit 1 as designed.
Nothing joined a federation; no VR-Forces process was started; nothing under C:\MAK was written.
