# STP task vocabulary assessment (Opus, 2026-09-14 ~13:15Z; lane L8, Jira STP-797). Supervisor notes: the scripted-task
# channel (VrfFacade::RunScriptedTask -> DtScriptedTaskTask) exists end to end with zero callers; 5.2 has no native
# Attack/Defend/Seize task classes - every tactical task is a Lua scripted task, many shipped in EntityLevel; the init's
# 35 tactical areas are already created in VR-Forces with the C2SIM uuid; the binding constraint is parameter supply
# (9 of 42 COA-STP1 tasks carry no location; 42/42 self-target). Six rulings R1-R6 are the user's (sec 7).
# RULINGS 2026-09-14: R1 STP export defect (fix upstream), R2 who-unit geometry, R3 target = objective, R4 end time = start + Duration; R5/R6 open - see sec 7 and DOCTRINE_FOR_TASKING_RULINGS_2026-09-14.md.

# TASK VOCABULARY ASSESSMENT - STP verbs -> VR-Forces 5.2 tasks (2026-09-14, lane L8)

Lane L8 of docs/PLAN_PARALLEL_LANES_2026-09-14.md. Gate: PLAN - this document IS the
scope statement; nothing is built until the user rules on Section 4. Tier: STANDARD
(analysis; no cause claim). Read-only pass: no file in the repo or under C:\MAK was
modified, no build, no launch.

Anchors used, in the order the repo's own rule requires (vendor sample > vendor docs >
deterministic runs > headers > agents): the 5.2d installed data and headers, the 5.2d
help set, the run captures, then the repo's own settled notes
(docs/SEMANTIC_MAPPING.md, docs/TASK_VOCABULARY_V2.md, docs/STP_TASK_VOCABULARY_2026-09-03.md).
TASK_VOCABULARY_V2 is the 5.0.2-era predecessor of this document; where the two differ,
the difference is called out explicitly below (Section 0) rather than silently restated.

Path conventions in citations:
  REPO   = C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM
  SDK    = C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\Software\Library\CS\C2SIMSDK
  STP    = C:\Users\PauloBarthelmess\Source\Repos\STP\STP-5.11
  MAK    = C:\MAK\vrforces5.2d
  HELP   = MAK\doc\help\Content

---

## 0. WHAT THIS PASS CHANGES IN THE RECORD

Five findings that were not in the record before this pass. Each is evidence-backed
below; three of them refute or narrow a statement in the existing docs.

1. THE OBJECTIVE GEOMETRY IS ALREADY IN THE SIMULATION. COA-STP1_Initialization.xml
   carries 409 TacticalGraphic elements - 35 TacticalAreas (named MADISON, MONROE,
   JEFFERSON, HAMILTON, IRON_FIST, BANDIT/II/III, BP_PL_BLUE, AAR_PAA_25, ...), 41 Lines,
   317 Points, 16 TaskGraphics - and the interface ALREADY creates the 35 areas in
   VR-Forces with their C2SIM UUID as the VRF UUID ("Init dispatched: 128 units + 35 areas
   queued for creation", G6 app log; VrfC2SimService.cs:929-943 -> VrfFacade.cpp:725-737).
   Every high-fidelity VR-Forces task in Section 3 takes exactly such an object as its
   parameter. The objective areas the demo needs are already live objects with known
   UUIDs. The 41 Lines and 317 Points are parsed away and NOT created.

2. THE SCRIPTED-TASK CHANNEL IS ALREADY BUILT AND HAS NEVER BEEN USED. The facade
   already exposes RunScriptedTask(uuid, scriptId, vars) (VrfFacade.h:507,
   VrfFacade.cpp:947-964 -> DtScriptedTaskTask + setScriptId + variables().addVariable +
   sendTaskMsg) and the .NET bridge already marshals it (VrfBridge.cpp:439-441, ScriptVar
   at :69-81). No C# file calls it. That is the single lowest-cost route to the entire
   vendor tactical vocabulary. Its only limit is that ScriptVar covers ONLY ObjectUuid and
   Real; the vendor's DtScriptedTask::setValue also accepts bool, int, string, DtVector
   (location), DtEntityType and orientation (scriptedTaskTask.h:90-106) - several vendor
   tasks need those.

3. "EntityLevel has move/follow/fire only" IS REFUTED. STP_TASK_VOCABULARY_2026-09-03.md
   sec 4 item 3 says the attack/defend/recon behaviours live only in AggregateTacticalLevel.
   MAK\data\simulationModelSets\EntityLevel\scripts (412 scripted-task .xml files) contains
   an Offense menu category (company_seize "Seize Objective", co_clear "Clear",
   squad-assault, team-assault, unit-fire-at-target, entity/unit_request_fire_support) plus
   company_breach, section_breach, platform_breach, plt_attack_by_fire, plt_support_by_fire,
   plt_movement_to_contact, plt_clear, plt_perimeter_defense, occupy_firing_positions,
   co_tactical_march, unit-ground-follow, unit_artillery_provide_indirect_fire. G6 ran
   EntityLevel.sms (runs\20260914T002716Z_run\launchvrf.stdout.log). So most of the
   fidelity gain needs NO profile switch and does not wait for Y-15.

4. NINE OF THE 42 COA-STP1 TASKS CARRY NO GEOMETRY AT ALL. Location count per task:
   0 locations x9, 1 x20, 2 x2, 3 x2, 4 x9. A zero-location task is refused outright
   ("NO LOCATION GIVEN - CAN'T EXECUTE TASK", VrfC2SimService.cs:1920) and its whole STREND
   chain is skipped. In G6 that cost four of the eleven chains (the air-defence chain
   T9-T12) before the simulation did anything. This is a bigger demo hazard than any verb
   mapping: 21% of the order is refused by construction.

5. ALL 42 TASKS SELF-TARGET (PerformingEntity == AffectedEntity, 42/42 re-verified this
   pass). Confirms SEMANTIC_MAPPING sec 2a. Consequence: the two wired target-taking
   compositions (FireAtTarget, Breach) can NEVER fire from this order - G6 logged
   "affected entity is the taskee itself ... advancing only" seven times and "affected
   obstacle ... not resolvable to a distinct VRF unit" once. Targets must come from the
   objective AREA (finding 1), exactly as the vendor's own tasks take them.

---

## 1. WHAT STP PRODUCES

### 1.1 COA-STP1_Order.xml - the full census (re-derived this pass from the XML)

42 Task elements, every one a ManeuverWarfareTask, 17 distinct TaskActionCode values,
11 taskees. The interface prints the same census itself at order receipt
(VrfC2SimService.cs:1622-1627); G6 logged:
"ORDER: 42 task(s) for 11 taskee(s); verbs [ATTACK x10, SECURE x4, FIX x3, OCCUPY x3,
BREACH x3, SCREEN x3, PENTRT x2, BLOCK x2, DESTRY x2, DISRPT x2, DEFEND x2, MOVE, ESCRT,
GUARD, SEIZE, RETAIN, CLRLND]."

| verb   | n  | verb   | n | verb   | n | verb   | n |
|--------|----|--------|---|--------|---|--------|---|
| ATTACK | 10 | SCREEN | 3 | DISRPT | 2 | ESCRT  | 1 |
| SECURE | 4  | BREACH | 3 | DESTRY | 2 | GUARD  | 1 |
| FIX    | 3  | PENTRT | 2 | DEFEND | 2 | SEIZE  | 1 |
| OCCUPY | 3  | BLOCK  | 2 | MOVE   | 1 | RETAIN | 1 |
|        |    |        |   |        |   | CLRLND | 1 |

Other fields, whole-file counts: WeaponRuleOfEngagementCode = ROEHold on all 42;
DesiredEffectCode = TaskSuccess x40, DSTRYK x2 (the two DESTRY tasks);
ActionTemporalRelationship present on 31, association code STREND on all 31,
TimeReferenceCode IntervalEndTime on all 31; MapGraphicID = 0 occurrences;
AffectedEntity == PerformingEntity on 42 of 42.

Per-task table (name truncated; loc = number of Location elements; pred = STREND
predecessor). This is the table the mapping in Section 4 is scored against.

| #   | task                                              | verb   | loc | pred |
|-----|---------------------------------------------------|--------|-----|------|
| T1  | AOA_SE_1-35_AR;_2/1_AD_P1                         | PENTRT | 4   | -    |
| T2  | PL_OBJ_MADISON                                    | FIX    | 2   | T1   |
| T3  | PL_OBJ_MONROE                                     | ATTACK | 2   | T2   |
| T4  | ConsolidateAndPrepareDefensivePositionsAlongPlB.. | BLOCK  | 1   | T3   |
| T5  | ConductCounter-FireAndNeutralizationFires..       | DESTRY | 1   | -    |
| T6  | SuppressEnemyArtilleryAndDirectFires..            | DISRPT | 1   | T5   |
| T7  | SuppressAndNeutralizeEnemyArtillery..             | DISRPT | 1   | T6   |
| T8  | ProvidePriorityFiresInSupportOfBrigadeDefensive.. | ATTACK | 0   | T7   |
| T9  | ProvideAirDefenseCoverageForBrigadeManeuver..     | DEFEND | 0   | -    |
| T10 | ContinueAirDefenseCoverage..                      | DEFEND | 0   | T9   |
| T11 | OccupyAndDefendPositionsAlongPlBlue..             | OCCUPY | 1   | T10  |
| T12 | OccupyAndDefendAdaBattlePositionsAlongPlBlue.     | OCCUPY | 1   | T11  |
| T13 | ConductDeliberateBreachOperationsAtPlGold.        | BREACH | 3   | -    |
| T14 | SupportObstacleEmplacementAndLaneDenial..         | ATTACK | 1   | T13  |
| T15 | AOA_SE_1-6_IN;_2/1_AD_P1                          | ATTACK | 4   | -    |
| T16 | AdvanceToSupportTheBrigadeTransition..            | MOVE   | 0   | T15  |
| T17 | AOA_SE_1-6_IN;_2/1_AD_P3                          | FIX    | 4   | T16  |
| T18 | MaintainFixingFiresAndTransition..                | FIX    | 1   | T17  |
| T19 | AOA_SE_40_EN;_2/1_AD_P1                           | BREACH | 4   | -    |
| T20 | ExpandBreachLanesAndSupportMobility..             | SECURE | 1   | T19  |
| T21 | SupportBreachingAndMobilityOperations..           | BREACH | 0   | T20  |
| T22 | ConstructProtectiveObstaclesAndSurvivability..    | ATTACK | 1   | T21  |
| T23 | AOA_SE_1-1_RECON/2/1_AD_P1                        | PENTRT | 4   | -    |
| T24 | ScreenForwardMovementOfTheBrigadeMainBody..       | SCREEN | 0   | T23  |
| T25 | MaintainReconnaissanceAndScreenOperations..       | SCREEN | 1   | T24  |
| T26 | PL_PL_BLUE                                        | SCREEN | 1   | T25  |
| T27 | SecureMovementCorridorsAndPassesAlongPlYellow.    | SECURE | 1   | -    |
| T28 | CNVY_PL_YELLOW                                    | ESCRT  | 1   | T27  |
| T29 | ContinueRouteSecurityAndTrafficRegulation..       | ATTACK | 1   | T28  |
| T30 | MaintainRearAreaSecurityAndConvoyEscort..         | GUARD  | 1   | T29  |
| T31 | AOA_SE_5-20_IN_(MECH);_2/1_...                    | ATTACK | 4   | -    |
| T32 | AOA_SE_5-20_IN_(MECH);_2/1_...                    | SEIZE  | 4   | T31  |
| T33 | SecureObjMadisonAndSupportTheBrigadeTransition..  | SECURE | 1   | T32  |
| T34 | RetainObjMadisonAndSecureBrigadeRearApproaches..  | RETAIN | 0   | T33  |
| T35 | AOA_SE_B/5-20_IN_(MECH)_P1                        | ATTACK | 4   | -    |
| T36 | ClearEnemyThreatsFromPaa25EAndSecureArtillery..   | CLRLND | 3   | T35  |
| T37 | ProvideFollow-OnSecurityAndSupportForArtillery..  | ATTACK | 0   | T36  |
| T38 | SecureArtillerySupportAreasAndSustainment..       | SECURE | 0   | T37  |
| T39 | AOA_SE_C/1-35_AR_P1                               | ATTACK | 4   | -    |
| T40 | OccupySupportPositionsAndPrepareToDestroyEnemy..  | OCCUPY | 1   | T39  |
| T41 | DestroyEnemyReserveFormationsAndAttackAviation..  | DESTRY | 1   | T40  |
| T42 | MaintainAttack-By-FireCoverageAgainstEnemy..      | BLOCK  | 1   | T41  |

Shape of the order: 11 chains, one per taskee, each 2-5 tasks deep, strictly serial via
STREND. Location-count histogram: 0 x9, 1 x20, 2 x2, 3 x2, 4 x9. The 9 zero-location
tasks are T8, T9, T10, T16, T21, T24, T34, T37, T38.

### 1.2 Every other order on disk

| file                                                          | verbs                              |
|---------------------------------------------------------------|------------------------------------|
| data\R9_Mojave_UnitMove_Order.xml                             | MOVE x3                            |
| data\R9_Mojave_UnitMove_Order_NoComments / _LongCoRouteName   | MOVE x3 each                       |
| data\R5_UnitMove_Order.xml                                    | MOVE x3                            |
| data\E1_Formation_Order.xml                                   | MOVE x7                            |
| data\PC2_EntityFirstWaypoint_Order.xml                        | MOVE x1                            |
| data\PROBE_G7_CrossSector_Order.xml                           | MOVE x1                            |
| data\COA-STP1_Sweden_MinimalOrder.xml                         | MOVE x2                            |
| data\VRF-Approved-5June24_Order.xml (85 tasks)                | MOVE 38, SCOUT 29, DEFEND 9, ATTACK 9 |
| docs\golden-trace\orders\1_ and 2_VRF_Move[_Back]_Order.xml   | MOVE x1 each                       |
| docs\golden-trace\orders\ (11 other probe files)              | MOVE x1 each                       |
| docs\golden-trace\orders\synthetic_semantic_sweden.xml        | BREACH 1, ESCRT 1, SCREEN 1        |

So COA-STP1_Order.xml is the ONLY order with the full verb spread; SCOUT appears only in
VRF-Approved; every live-tested fixture exercises MOVE. Unchanged from
TASK_VOCABULARY_V2 sec 1b; re-verified this pass.

### 1.3 What each STP task carries - the parameters available to a mapping

Schema type ManeuverWarfareTaskType,
SDK\C2SIMSDK\C2SIM_SMX_LOX_CWIX2024.cs:4026-4194 - ActionTemporalRelationship[] (:4058),
Location[] (:4069), MapGraphicID[] (:4080), Name (:4090), UUID (:4100),
AffectedEntity[] (:4111), DesiredEffectCode[] (:4122), Duration (:4132), EndTime (:4142),
PerformingEntity (:4152), StartTime (:4162), TaskActionCode (:4172),
RuleOfEngagement[] (:4183), TaskFunctionalRelation[] (:4194).

What the interface's parser actually lifts (REPO\src\VrfC2SimApp\OrderParser.cs:55-70;
model OrderModels.cs:11-27): TaskUuid, TaskName, TaskeeUuid, AffectedEntity[0],
ActionCode (:60), ROE code (:61), MapGraphicID[0] (:62), Location[] -> Points (:67-69),
StartTime delay, STREND predecessor + relative delay (:104-119). NOT lifted:
DesiredEffectCode, Duration, EndTime, TaskFunctionalRelation.

From the STP side (STP\NallSuite\Agents\C2SimBridge\C2SimXmlBuilder.cs:420-475, read in
the 2026-09-03 pass and NOT re-read here): Location[] is the linearised ROUTE (axis of
advance) when the STP task has routes, else the first tactical graphic's points;
AffectedEntity is set to the performing unit itself (:427-429); DesiredEffectCode is
DSTRYK for destroy, NUTRLD for defeat, else TaskSuccess; Duration = phases x 10 min;
ROE default Hold; MapGraphicID is emitted only when the bridge runs with
IncludeMapGraphicIdInTasks=True (documented default True, but 0 occurrences in both
exports on disk).

### 1.4 What STP CAN emit, and the schema's full enumeration

RE-VERIFIED from source this pass (STP\NallSuite\Agents\C2SimBridge\C2SimTask.cs:126,
property MWTaskCode, 547 lines): STP can emit exactly 51 distinct TaskActionCodeType
values -

AMBUSH, ARASLT, ARMAS, ATTACK, ATTMN, ATTSPT, BLOCK, BREACH, BYPASS, CLRLND, CNFPSL,
COVER, CRESRV, CTRATK, CTRFIR, DEFEAT, DEFEND, DELAY, DESTRY, DISRPT, ESCRT, EVACT,
ExecutePlanPhase, FIX, FOLASS, FOLSPT, GUARD, HARASS, MOVE, NTRCOM, OBSRV, OCCUPY,
PENTRT, PRVCNS, PRVHLT, PRVSCY, PSYCHW, REFUEL, REINF, RESUPL, RETAIN, SCOUT, SCREEN,
SECURE, SEIZE, SERCH, SUPPRS, TCARRC, TURN, UNCONW, WITHDR.

Coverage: our table maps 18 of the 51; every one of the 18 is emittable (no dead entry);
33 emittable codes are unmapped and fall through to bare movement -

  ground manoeuvre, worth mapping (21): ATTMN ATTSPT CTRATK CTRFIR ARMAS DEFEAT SUPPRS
      NTRCOM AMBUSH HARASS SERCH BYPASS COVER TURN DELAY WITHDR FOLSPT FOLASS CNFPSL
      OBSRV CRESRV
  not ours - air / CSS / stability / marker (12): ARASLT TCARRC EVACT REFUEL RESUPL
      REINF PSYCHW UNCONW PRVSCY PRVHLT PRVCNS ExecutePlanPhase

The C2SIM schema itself is far wider: enum TaskActionCodeType,
SDK\C2SIMSDK\C2SIM_SMX_LOX_CWIX2024.cs:4295-5655, has 453 members - 7 simple C2SIM task
names (AssistOtherUnit, HoldInPlace, MoveToLocation, Observe, OrientToLocation,
ReportPosition, UseCapability) followed by 446 MIP codes (ACQUIR ... WLDWSL, including
93 MCM* mine-countermeasure codes). NOTE ON THE BRIEF: the enum is named
TaskActionCodeType, not TaskNameCode; there is no TaskNameCode type in the CWIX2024
schema (a grep for it returns nothing). The schema is not the useful bound - STP's 51 is,
and COA-STP1's 17 is the demo bound.

### 1.5 The geometry STP ships that we do not use

COA-STP1_Initialization.xml element census: 409 TacticalGraphic inside 409 MapGraphic,
decomposing to Point x317, Line x41, TacticalArea x35, TaskGraphic x16; plus 128 Units
and 537 Entities.

- The 35 TacticalAreas are NAMED and are exactly the COA's objectives and battle
  positions: MADISON, MONROE, JEFFERSON, HAMILTON, EAGLES, NORMANS, IRON_FIST,
  BANDIT / BANDIT_II / BANDIT_III, BP_PL_BLUE + BP_PL_B_00/01/02, AAR_PAA_25,
  AT_PAA_25E, AT_PAA_25F, OBJ_BND_OB, OBJ_FIX_OB, OBJ_OBJ_00, ... (1-19 points each).
  Task names reference them directly: T2_PL_OBJ_MADISON, T3_PL_OBJ_MONROE,
  T33_SecureObjMadison..., T36_...FromPaa25E...
- The 41 Lines are unnamed (1-15 points each) - phase lines / LD / limits of advance.
- The 16 TaskGraphics are named __FRIEN_04..__FRIEN_21 (1-3 points) - STP's task arrows.
- The 317 Points are named after units and logistics nodes (1-35_AR_LRP, 1-6_IN_Log, ...).

Today the interface creates the 35 areas and nothing else (VrfC2SimService.cs:929-943;
InitParser collects only S.TacticalAreaType - InitParser.cs:60, :67, :140). The areas are
created with the C2SIM UUID AS the VR-Forces UUID (VrfFacade.cpp:725-737,
createControlArea(..., startingUUID = areaUuid)), so an objective area is addressable by
its C2SIM UUID with no extra mapping table. G6 confirms it live: "Init dispatched:
128 units + 35 areas queued for creation."

---

## 2. WHAT THE INTERFACE DOES TODAY, PER VERB

### 2.1 Layer 1 - the classifier (REPO\src\VrfC2SimApp\VerbMapping.cs)

18 keys, 8 intents, 5 intents wired:

| verb(s)                                   | intent        | wired? | VerbMapping.cs |
|-------------------------------------------|---------------|--------|----------------|
| MOVE                                      | Move          | yes    | :84            |
| BREACH                                    | Breach        | yes    | :85            |
| ATTACK DESTRY FIX DISRPT PENTRT           | Attack        | yes    | :86-90         |
| SECURE OCCUPY SEIZE RETAIN BLOCK DEFEND GUARD | HoldObjective | NO | :91-97         |
| SCREEN SCOUT                              | Reconnoiter   | yes    | :98-99         |
| ESCRT                                     | Escort        | yes    | :100           |
| CLRLND                                    | Clear         | NO     | :101           |
| (anything else)                           | Move          | yes*   | :112-113       |

IsImplemented at :67-77 is what makes HoldObjective and Clear fall through to bare
movement. (*) An unlisted verb is executed as bare movement and logged as a coverage gap
(VrfC2SimService.cs:1788-1791).

By COA-STP1 task count: Attack family 19 tasks, HoldObjective family 14, Breach 3,
Reconnoiter 3, Escort 1, Clear 1, Move 1. So HoldObjective - the ONE family with no
Layer-2 at all - is a third of the order.

### 2.2 Layer 2 - what each intent actually dispatches (REPO\src\VrfC2SimApp\VrfC2SimService.cs)

Common spine, in execution order:

| step | line | behaviour |
|------|------|-----------|
| classify | :1787 | VerbMapping.Classify(task.ActionCode) |
| unwired-verb log | :1793-1795 | "Layer-2 not yet wired - executing bare movement" |
| ATTACK target resolve | :1805-1823 | AffectedEntity -> VRF uuid; SELF-TARGET -> advance only (:1813-1815); unresolved -> warn, advance only (:1820-1822) |
| BREACH target resolve | :1829-1838 | same two-dict resolve + self guard; unresolved -> warn, advance only |
| ESCRT | :1844-1863 | resolve escorted entity -> SetRulesOfEngagement + FollowEntity, RETURN (no route needed); unresolved -> fall through to bare movement |
| live position | :1875-1881 | TryGetEntityGeodetic; failure -> "ABANDONING TASK", NotifyAbandoned |
| NO POINTS | :1899-1923 | resolved attack target -> FireAtTarget in place; resolved breach target -> Breach in place; ELSE "NO LOCATION GIVEN - CAN'T EXECUTE TASK" + NotifyAbandoned (:1920-1922) |
| origin-vertex drop | :1925-1943 | drop leading route points sitting on the authored origin |
| ROE | :1993-1996 | ROEFree->FireAtWill, ROEHold->HoldFire, else FireWhenFiredUpon |
| SetTarget | :2002 | KNOWN PARITY BUG reproduced on purpose: C2SIM uuids passed where VRF uuids are expected - silent no-op |
| aggregate + MoveIntoFormation | :2011-2029 | DtMoveIntoFormationTask to the route's LAST point (collapses waypoints); engage deferred |
| aggregate + AggregatePlanAndMove | :2037-2053 | CreateWaypoint + DtPlanAndMoveToTask (R11 probe) |
| aggregate formation enrichment | :2063-2082 | SetAggregateFormation / RequestAvailableFormations |
| single point | :2085-2099 | MoveToLocation; engage/breach deferred to move-complete |
| multi point | :2103-2161 | CreateRoute, then PatrolRoute if Reconnoiter (:2106) else MoveAlongRoute, deferred to route-created; optional R10 subordinate fan-out (:2119-2133) |

Facade/bridge targets: MoveToLocation VrfFacade.h:427, MoveAlongRoute :428,
PlanAndMoveTo :435, SetAggregateFormation :454, MoveIntoFormation :475, Breach :480,
PatrolRoute :493, FollowEntity :496, FireAtTarget :503.

Two implementation warts confirmed in the 5.2 source this pass:
- FollowEntity sends DtFollowEntityTask with NO offset (VrfFacade.cpp, FollowEntity:
  "Offset left at default (0)"), so an escort stations ON its leader.
- PatrolRoute and Breach are single-parameter (setRoute / setBreachTarget only); the
  vendor's own breach line (start/end points) is never set.

### 2.3 What G6 actually did with the 42 tasks

Run runs\20260914T002716Z_run (COA-STP1_Initialization + COA-STP1_Order, EntityLevel.sms,
FidelityTable types, 5.2 stack). Method: bounded scan of vrfc2simapp.log - the full first
60 MB, then a streamed scan of bytes 60-520 MB (which covers ~1300 s of run by its 130
position-report cycles, i.e. past the 600 s predecessor timeout), then five 12 MB samples
at 300 / 700 / 1400 / 2800 / 4200 MB. Beyond 60 MB the log contains ONLY info-level
object-console relay (2,966,499 lines) and position-report summaries: zero "Task '" lines,
zero warn-level lines.

| outcome | tasks | evidence |
|---------|-------|----------|
| Dispatched, bare CreateRoute + MoveAlongRoute | 9: T1 T5 T15 T19 T23 T27 T31 T35 T39 | 9 "CreateRoute '<task> ROUTE'" + 9 "MoveAlongRoute issued" |
| ATTACK-family degraded to advance-only (self-target) | 7: T1 T5 T15 T23 T31 T35 T39 | "affected entity is the taskee itself (self-target fire-support?); no fire, advancing only" |
| BREACH degraded to advance-only | 1: T19 | "affected obstacle '6977b035-...' not resolvable to a distinct VRF unit; advancing only, no breach" |
| HoldObjective, no Layer 2 | 2 logged: T9 T27 | "verb=SECURE -> intent=HoldObjective (move-to + DtHoldUntilTask + scan); Layer-2 not yet wired"; same for DEFEND |
| REFUSED - no geometry | 1: T9 | "NO LOCATION GIVEN - CAN'T EXECUTE TASK 'T9_ProvideAirDefenseCoverage...'" |
| SKIPPED - predecessor abandoned upstream | 3: T10 T11 T12 | "predecessor <uuid> was skipped/abandoned upstream; policy=skip, unit A/6-56/HHC is idle -> NOT dispatched" |
| NEVER DISPATCHED | the remaining 29 | no dispatch line anywhere in the scanned regions |
| COMPLETED | 0 | no VR-Forces task completion is logged anywhere |
| REPORTED to STP | 0 | reports-captured.log: 14,596 PositionReportContent, 102 ObservationReportContent, 0 TaskStatus / TASKCMPLT / TASKABRT / TASKSTRT |

Per-verb tally for G6: PatrolRoute issued 0 times (the 3 SCREEN tasks were all
successors and never dispatched); FollowEntity 0 (ESCRT T28 is a successor);
FireAtTarget 0; Breach 0. NOT ONE Layer-2 composition executed in the whole run. The
demo-visible behaviour of COA-STP1 today is: nine units start one movement each, and
nothing else ever happens.

UNEXPLAINED SYMPTOM, recorded not rationalised: the 29 never-dispatched tasks should each
have logged a gate failure. Their predecessors dispatched at ~00:30:30 and never
completed, so TaskSequencer.WaitForStartAsync (TaskSequencer.cs:86-135) should return
PredecessorTimeout 600 s later (TaskPredecessorTimeoutSeconds=600, VrfSettings.cs:303)
and VrfC2SimService.cs:1685-1688 should log "policy=skip ... NOT dispatched". Only 3 such
lines exist (the T10/T11/T12 abandon cascade), and no timeout line appears in the ~24
minutes of log scanned. Competing explanations not yet separated: (a) the timer tasks
were starved behind the level-4 console relay and fired outside the scanned bytes;
(b) the warnings are there but past 520 MB; (c) the gate never returned. This is a defect
candidate for the reporting lane (L2) - it is exactly the "a task that will never run
produces no report" gap (REPORTING_ASSESSMENT_2026-09-14 G2/G4) - and it does NOT change
any conclusion here, because the dispatch evidence is positive: only 9 VR-Forces task
messages were ever sent.

### 2.4 Already built, never called: the scripted-task channel

- vrf::ScriptVar (VrfFacade.h:56-70): Kind = ObjectUuid | Real; helpers Object(name,uuid)
  and Number(name,value).
- VrfFacade::RunScriptedTask (VrfFacade.h:507; VrfFacade.cpp:947-964): builds a
  DtScriptedTaskTask, setScriptId(scriptId), adds a DtRwObjectName (with setUUID) or
  DtRwReal per variable, then controller->sendTaskMsg(taskee, &task).
- VrfFacade::SendScriptedSet (VrfFacade.h:511; VrfFacade.cpp:966-983): the same for
  DtScriptedTaskSet via sendSetDataMsg.
- Marshalled to .NET: VrfBridge.cpp:69-81 (ScriptVar), :439-441 (RunScriptedTask),
  :442-444 (SendScriptedSet), :552-560 (ToNativeVars).
- Callers in REPO\src\VrfC2SimApp\*.cs: NONE.

Gap: the vendor accepts far more variable types than ScriptVar models -
DtScriptedTask::setValue overloads (scriptedTaskTask.h:90-106) take bool
(DtScriptedTaskCheckBoxVariable), int, double, std::string, DtUUID, DtVector
(DtScriptedTaskLocationVariable), DtEntityType, DtTaitBryan, vectors of UUID/DtVector,
and string->number maps. Several vendor tasks in Section 3 need Bool / Integer /
Location3D, so ScriptVar must grow three kinds before those tasks can be issued.

---

## 3. WHAT THE VENDOR OFFERS (VR-Forces 5.2d, installed)

### 3.1 C++ task classes - MAK\include\vrftasks\*.h (279 headers)

Extracted this pass: 70 distinct Dt*Task classes. The ground-relevant ones, with the
header's own one-line purpose:

| class | header:line | purpose (vendor doxygen) |
|-------|-------------|--------------------------|
| DtMoveAlongTask | moveAlongTasks.h:46 | Move Along Route; complete when the entity reaches the last vertex. A pseudo-aggregate forwards it to its LEAD subordinate and completes when the lead does. |
| DtMoveAlongWithActionsTask | moveAlongTasks.h:192 | move-along carrying per-vertex route actions |
| DtMoveAlongRetrogradeTask | moveAlongTasks.h:157 | move-along in reverse |
| DtMoveToTask | moveToTask.h:38 | move to a control point |
| DtMoveToDirectTask | moveToTask.h:185 | move to a point without path planning |
| DtPlanAndMoveToTask | planAndMoveToTask.h:35 | DtMoveToTask + path planning |
| DtMoveIntoFormationTask | moveIntoFormationTask.h:29 | get an aggregate into formation at a location and heading |
| DtPatrolRouteTask | patrolRouteTask.h:45 | patrol back and forth along a route (never self-completes) |
| DtPatrolTwoPointsTask | patrolTwoPointsTask.h:44 | patrol between two points |
| DtFollowEntityTask | followEntityTask.h:57 | follow an entity at an offset |
| DtKeepStationTask | keepStationTask.h:61 | keep-station mode of follow |
| DtConvoyToTask / DtConvoyAlongTask | convoyToTask.h:35 / convoyAlongTask.h:34 | convoy task for an AGGREGATE |
| DtBreachTask | breachTask.h:24 | go to the given object and breach it |
| DtFireAtTargetTask | fireAtTargetTask.h:25 | fire a manually or automatically chosen weapon at a target |
| DtFireBallisticGunTask | fireBallisticGunTask.h:25 | fire a ballistic gun at a target |
| DtFireForEffectOnTargetTask | fireForEffectOnTargetTask.h:18 | artillery fire for effect on an entity |
| DtFireForEffectOnLocationTask / ...OnEntityTask | fireForEffect*.h | fire for effect on a location / entity |
| DtProvideIndirectFireTask | provideIndirectFireTask.h:14 | provide indirect fire |
| DtStopMovingTask | stopMovingTask.h:29 | stop linear and rotational motion |
| DtTurnToHeadingTask | turnToHeadingTask.h:42 | turn to a heading |
| DtHoldUntilTask | holdUntilTask.h:20 | hold until a simulation time |
| DtWaitDurationTask / DtWaitElapsedTask / DtWaitTask | waitDurationTask.h:45 / waitElapsedTask.h:47 / waitTask.h | wait N seconds / until elapsed sim time / indefinitely |
| DtEmbarkTask / DtDisembarkTask / DtDisembarkAllTask | embarkTask.h:25 / disembarkTask.h:22 / disembarkAllTask.h:19 | mount / dismount |
| DtScriptedTaskTask | scriptedTaskTask.h:277 | run a named Lua scripted task with named variables |
| DtUserTask | userTask.h:53 | a user-defined task |
| DtClearTask | clearTask.h:33 | a SetDataRequest meaning "clear the current task" - NOT tactical clear |

DECISIVE NEGATIVE RESULT: there is NO DtAttackTask, DtDefendTask, DtSeizeTask,
DtOccupyTask, DtSecureTask, DtScreenTask, DtGuardTask or DtBlockTask anywhere in
vrftasks. In 5.2 every tactical (as opposed to kinematic) task is a Lua SCRIPTED task
delivered as a DtScriptedTaskTask. Any mapping of the HoldObjective family to a "native
VR-Forces task" must go through the scripted-task channel; there is no C++ shortcut.
(This narrows TASK_VOCABULARY_V2's "Unit Offensive/Defense behaviors" wording, which
implied dedicated task classes.)

### 3.2 EntityLevel scripted tasks - MAK\data\simulationModelSets\EntityLevel\scripts

412 scripted-task metadata files. Menu categories as parsed from myMenuLocations -
Movement 77, Embarkation 20, Engagement 14, Offense 7, Action 4, Disposition 4, Terrain 3,
others 1-2, and 272 with no parseable menu location. Those counts are a LOWER BOUND:
some tasks declare a menu text without a menu-location entry my parse could read (checked
case: company_breach has myMenuText "Breach" and myShowInMenu=1 yet no parseable location).
Also note that myShowInMenu=0 hides a task from the GUI but does not make it
unassignable over the wire. The tactically relevant set:

| scriptId | menu | purpose / taskParameters found in the .lua header |
|----------|------|---------------------------------------------------|
| company_seize | Offense / Seize Objective | company splits into support + assault platoons and seizes the target area. Params: enemyArea, firePositions, direction, twoAssault, departLine (Line of Departure), supportPlts, assaultPlts, assaultRoute, battery. Valid entity types 11:1:-1:5:2 and 11:1:-1:5:6. |
| co_clear | Offense / Clear | line up at a start point and advance to a limit of advance, clearing. Valid types 11:1:-1:5:{1,2,4,6}. |
| company_breach | Offense / Breach (myShowInMenu=1) | breach a minefield with plow/roller platoon + assault + support elements. Params: objective, lane1, lane2, enemyArea, supportFirePositions, battery. Valid types 11:1:-1:5:{2,4,17,19}. |
| section_breach / platform_breach | (no menu) | section / single-platform breach of a lane (param: lane / breachingLane) |
| squad-assault, team-assault | Offense | assault an objective in phases (approach, support, assault) |
| unit-fire-at-target | Offense | unit-level fire at a target |
| entity_request_fire_support / unit_request_fire_support | Offense | request fires |
| plt_attack_by_fire | (no menu) | platoon moves to a line of fire, takes hull-down position, fires into an area |
| plt_support_by_fire | (no menu) | as above, supporting role |
| plt_movement_to_contact | (no menu) | move toward an objective, act on contact |
| plt_clear | (no menu) | platoon clear |
| plt_perimeter_defense | "Perimeter Defense", myShowInMenu=0 (file PLT_Perim_Def.lua) | platoon perimeter defense |
| occupy_firing_positions / find_firing_positions / engage_from_firing_position | "Fire Team Occupy Fire Position" (file find_firing_positions_from_point_1.lua) / (hidden) | occupy and fight from prepared positions; find_firing_positions params Threat, ThreatRadius, Range, DistanceFromThreat |
| co_tactical_march | (no menu) | road march; params startPoint, releasePoint, marchRoute, marchSpeed |
| unit-ground-follow | (no menu) | unit follow that sizes the follow offset by unit extent; params followSubject, followOffset |
| follow_along_offset_route | (no menu) | follow a leader along an offset route; params route, leader, offset |
| unit_travel_offset_routes | (no menu) | travel subordinate offset routes |
| patrol_route_script_with_options | (no menu) | patrol with move-along options; params Route, treatRouteAsRoad, reverseDirection, startAtClosestVertex |
| unit_artillery_indirect_fire_at_location | (no menu) | full call-for-fire parameter set (location, weaponSystem, rounds, sheaf, fuse, ...) |
| unit_artillery_provide_indirect_fire | (no menu) | standing indirect-fire support |
| unit-mortar-fire-at-location | (no menu) | mortar fire mission |
| provide_suppressive_fire / provide_suppressive_fire_loc | Engagement | suppressive fire on an object or location |
| fire_for_effect_on_location / fire_for_effect_on_target | Engagement | artillery FFE |
| mechanized-assault-tank-led, rifle-squad-attack-by-fire, fire_team_* | (no menu) | sub-unit assault patterns |
| unit-set-posture | (no menu) | set unit posture |

Help pages for the ones with GUI dialogs (parameters are authoritative there):
HELP\Tasks\UnitBehaviors\OffenseSeizeObjective.htm (Objective Area, Fire Positions, one
Line of Departure, Seize Direction Direct/Left/Right, Two Platoon Assault, Artillery,
optional Assault Route); OffenseClear.htm (Limit of Advance line, Starting Point);
OffenseMovementToContact.htm (Axis of Attack = a line, Objective = ellipse or circle,
Action on Contact Engage/Ignore); OffenseAttackByFire.htm (Line of Fire, Enemy Area,
Time Out seconds, Do Fires Indefinitely); OffenseAssault.htm (Enemy Area, Support By Fire
line, Assault Position, Limit of Advance); OverwatchStationaryDefense.htm and
OverwatchMobileDefense.htm (Overwatch location, Maneuver Elements, Enemy area);
DefensePlatoonDefensiveCoil.htm (Radius); FollowAlongOffsetRoute.htm (Leader Route,
Leader, offsets); MovementTasks\PatrolRoute.htm; MovementTasks\RouteMoveAlong.htm;
MovementTasks\FormationMoveInto.htm.

Every one of those parameters is a TACTICAL GRAPHIC (area, line, point, route) - i.e.
exactly the object class our init already ships (Section 1.5) and partly creates.

BINDING KEY: a scripted task is addressed by its myScriptId, which frequently DIFFERS
from the .lua/.xml filename (plt_perimeter_defense lives in PLT_Perim_Def.*;
occupy_firing_positions lives in find_firing_positions_from_point_1.*). Any build item
below must bind on the scriptId read from the metadata, never on a filename.

### 3.3 AggregateTacticalLevel scripted tasks (the Y-15 profile)

MAK\data\simulationModelSets\AggregateTacticalLevel\scripts - 54 scripted tasks. The
ground-manoeuvre ones:

| scriptId | menu | taskParameters (from the .lua header) |
|----------|------|----------------------------------------|
| unit-attack-to-objective | Offense / Attack To Objective | objective (SimObject), route (SimObject, the axis), fireSupportUnit (SimObject), leadElement (SimObject). "Given an objective and an axis, conduct an attack. 1. If not in an attack posture, change to Hasty-Attack as the minimum. 2. Execute a move-along." |
| group-attack-to-objective | Offense | objective, route, fireSupportUnit, leadingElements (list) |
| unit-defend | Defense / Defend | fireSupportUnit. "Given an objective and an axis, conduct a defense. If not in a defend posture, change to Hasty-Defense..." |
| unit-trailing-march-to-objective | Movement | objective, route, leadElements (table), trailingDistance (Real) |
| unit_movement_simplified | Movement | destination (Location3D), destinationPoint (SimObject), useRoads (Bool) |
| group_movement_simplified | Movement | simplified group movement |
| reconnoiter-route | (Reconnoiter Route) | route (SimObject), duration (Integer s), patrol (Bool), deployDrones (Bool), useRoads (Bool) |
| reconnoiter-location | | location (Location3D), duration, deployDrones, useRoads |
| reconnoiter-waypoint | | waypoint (SimObject), duration, deployDrones, useRoads |
| perform-ground-reconnaissance | (called by the above) | route, waypoint, location, patrol, duration, deployDrones, useRoads |
| organic-fire-support / automatic-fire-support / manage-fire-support / supervise-organic-artillery | | fire support behaviours |
| artillery-automatic-engagement, mrl-automatic-engagement, ballistic-missile-automatic-engagement | | automatic engagement background behaviours |
| mount/dismount-company, mount/dismount-platoon, set-mounted/dismounted-* | Embarkation | mount and dismount |
| group-set-ordered-speed | Action | |

AggregateTacticalLevel.sms includes AggregateLevelBase, which adds automatic_air_defense
(AggregateLevelBase\scripts\Automatic_Air_Defense.lua / .xml) - "fires at enemy aircraft
that enter the specified area" (HELP\Tasks\AggregateLevelScenarios\AutomaticAirDefense.htm,
parameter: the area). That is the only native home for the four COA-STP1 air-defence
tasks.

Aggregate-level posture is a first-class model
(HELP\ConceptsAggregateLevel\AggregateLevel\vrf_aggregatePosture.htm): Travel,
Reconnaissance, Hasty-Attack, Deliberate-Attack, Hasty-Defense, Deliberate-Defense, Rout;
posture changes take time (up to hours for deliberate defense) and alter footprint,
sensing, signature, speed, sector sizes, combat power and vulnerability. Change Posture is
a task (HELP\Tasks\AggregateLevelScenarios\PostureChange.htm); Set Posture changes it
immediately. This is the single cheapest fidelity lever for a verb map: it turns ATTACK,
DEFEND, SCREEN and MOVE into four visibly different unit states even when the underlying
motion is the same move-along.

CONSTRAINT: "Aggregate-level modeling only works with HLA Evolved, HLA 4, and MAK FOM
extensions" and "simulation objects that use aggregate-level modeling cannot interact with
simulation objects that use entity-level modeling... Combining these SMSs is not
supported" (HELP\SimObjectsSection\Introduction\vrf_entityLevelModelingAndAggregateLevelModeling.htm).
We run HLA 1516e, so the protocol is fine; the SMS is all-or-nothing per scenario.

### 3.4 How a scripted task is issued remotely

- Generic channel: DtVrfRemoteController::sendTaskMsg(recipient, DtSimTask*, addr)
  (MAK\include\vrfcontrol\vrfRemoteController.h:1632-1634). Every task class below it in
  that header (moveAlongRoute :1638, moveToWaypoint :1644, moveToLocation :1651,
  patrolBetweenWaypoints :1657, patrolAlongRoute :1664, followEntity :1673, wait :1681,
  waitDuration :1687, waitElapsed :1693, skipTask :1697) is a convenience wrapper over it.
- DtScriptedTaskTask carries setScriptId (scriptedTaskTask.h:54-55, :236-237) and a
  DtRwVariableBindings of named variables with setValue overloads for bool / int / double
  / string / DtUUID / DtVector / DtEntityType / orientation / vectors / maps
  (scriptedTaskTask.h:90-106). That is the whole contract.
- Control objects to bind the variables to: createControlArea
  (vrfRemoteController.h:1091-1108, note "the vertices should form a convex polygon"),
  createPhaseLine (:1054-1072, a TWO-POINT line, with an optional startingUUID),
  createRoute (:1023-1039), createWaypoint (:991-1011). All accept a caller-supplied UUID
  and an object-created callback.
- The vendor's own remote-control sample
  (MAK\examples\remoteControl\commandLineRemoteController.cxx, 2242 lines) exercises only
  moveToPoint (:1000), moveToWaypoint (:1771), a DtPlanBuilder plan of
  DtWaitDurationTask + DtMoveToTask + DtSetHeadingRequest + DtSetSpeedRequest +
  DtSetDestroyRequest assigned with assignPlanByName (:1849-1889), the plan callbacks
  (:2036-2038), and a DtTaskFireAtEntityWMInter interaction that only works if the
  addTaskInteraction plugin is loaded (:1217-1228). It does NOT demonstrate a scripted
  task. So the sample cannot be the anchor here; the header contract and the shipped
  .lua/.xml pairs are.

### 3.5 Vendor constraints that shape any mapping

1. TYPE FILTER. Each scripted task's metadata carries myEntityTypes, e.g. company_seize
   accepts 11:1:-1:5:2 and 11:1:-1:5:6. Our Tank Company (USA) is objectType
   3:11:1:225:5:2:0:0 (data\unit-type-map-52-nolifeform.json) - the 11:1:*:5:2 tail
   matches. Our Tank Platoon (USA) is 11:1:225:3:2 and our battalion CP proxy is
   11:1:225:14:2:1 - NEITHER matches company_seize. In G6 the COA-STP1 taskees were
   created as Tank Headquarters Section (the five BN CPs) and Tank Platoon (the companies,
   via PROXY rows), so the company-level scripted tasks would find no valid taskee today.
   DOCS ARE SILENT on whether the sim engine enforces myEntityTypes for a task arriving
   over the network: the help only says the list controls GUI availability and script
   filtering (HELP\Tasks\ScriptedTasksSets\ScriptAvailabilityConfigure.htm,
   ScriptListFilter.htm), and that the default is "available to all simulation objects".
   This is a one-run question, not a doc question.
2. CONVEX AREAS. createControlArea's header says the vertices "should form a convex
   polygon". STP objective areas are arbitrary polygons (up to 19 points). Untested.
3. MUTUAL EXCLUSION. "if the new task is mutually exclusive with the current task, the
   simulation object immediately stops the current task and begins the new one";
   "If a simulation object has a plan, when you give it an independent task, it abandons
   the rest of the plan" (HELP\Tasks\TasksIntro.htm). Any advance-then-engage composition
   must therefore stay completion-gated, as the port already does
   (DeferEngageUntilMoveCompletes).
4. TERRAIN CAVEAT, verbatim: "MAK has designed these behaviors using open source United
   States doctrine to work in an open terrain such as the U.S. National Training Center at
   Fort Irwin... While the tasks may work well in other similar terrains, your results may
   vary. We do not support all tasks in all terrains."
   (HELP\Tasks\UnitBehaviors\vrf_overviewUnitTasksBehaviors.htm). Our terrain IS the
   Mojave/NTC box, which is the best case the vendor claims.
5. VEHICLE CREW / FUNCTIONS. Several unit behaviours require the unit to have subordinates
   with declared functions ("Vehicle Crew", "maneuver elements", "base of fire") - same
   source. Our composed units declare no functions.

---

## 4. MAPPING PROPOSAL

Three premises the whole proposal rests on, each evidenced above:

P1. The vendor's tactical tasks take TACTICAL GRAPHICS, not entity UUIDs (Section 3.2/3.3).
P2. STP already ships those graphics in the Initialization, and the 35 areas are already
    created in VR-Forces under their C2SIM UUID (Section 1.5). Lines and points are not.
P3. The task-to-graphic LINK is missing (0 MapGraphicID in both exports), so it must come
    from (a) STP re-exporting with IncludeMapGraphicIdInTasks=True - the clean fix, a
    question for the STP owner - or (b) a NAME/GEOMETRY match we compute: the task name
    carries the objective ("T2_PL_OBJ_MADISON" -> area MADISON; "T33_SecureObjMadison..."),
    and the task's last Location lies inside or nearest one of the 35 areas. (b) is a
    fallback, and it must be reported, never silent.

Ranked by demo value = COA-STP1 task count. "EL" = EntityLevel.sms (what we run today),
"ATL" = AggregateTacticalLevel.sms (the Y-15 profile).

### Rank 1 - ATTACK family: ATTACK 10, FIX 3, DESTRY 2, DISRPT 2, PENTRT 2 = 19 tasks

| | |
|---|---|
| Vendor task, ATL | `unit-attack-to-objective` (scripted). Params objective (SimObject), route (SimObject axis), fireSupportUnit, leadElement. Sets an attack posture then executes a move-along. |
| Vendor task, EL | Composite today (move-along + DtFireAtTargetTask) OR `plt_movement_to_contact` / `plt_attack_by_fire` / `mechanized-assault-tank-led` for platoon-sized taskees; `unit-fire-at-target` for the fires. |
| C2SIM inputs needed | Location[] -> the ROUTE (already have); the OBJECTIVE area (P3); ROE -> SetRulesOfEngagement (already wired, :1993); DesiredEffectCode DSTRYK/NUTRLD -> not parsed today, would distinguish DESTRY/DEFEAT from ATTACK. |
| Verb nuance | ATTACK/PENTRT/ARMAS/ATTMN/ATTSPT -> attack-to-objective. DESTRY (+DSTRYK) -> attack-to-objective with a destroy end-state. FIX/DISRPT/SUPPRS/HARASS -> attack-BY-fire semantics (`plt_attack_by_fire` / `provide_suppressive_fire`), not an assault: the unit takes a firing line and engages an area, which is exactly what "fix" and "disrupt" mean. |
| Completion evidence | Scripted tasks end with `vrf:endTask(bool)` (284 true / 260 false call sites across the EntityLevel scripts) and the vendor report carries `DtTaskCompleteReport::success()` (taskCompleteReport.h:84-90). That is a REAL pass/fail, better than move-along's leading-edge completion. Our facade currently DROPS it: struct TaskCompleted has only unitMarking + taskType (VrfFacade.h:196-199). |
| Gaps | (i) targets are areas, not AffectedEntity - today's FireAtTarget path is dead for STP data by construction; (ii) type filter (Section 3.5 item 1); (iii) ATL profile needed for the true unit-attack behaviour; (iv) the interface must stop treating self-target as an error case and start treating it as the NORM. |

### Rank 2 - HoldObjective family: SECURE 4, OCCUPY 3, DEFEND 2, BLOCK 2, SEIZE 1, RETAIN 1, GUARD 1 = 14 tasks

This is the family with NO Layer 2 at all today (VerbMapping.cs:74-77) and the largest
single fidelity gain available.

| verb | EL vendor task | ATL vendor task | notes |
|------|----------------|-----------------|-------|
| SEIZE | `company_seize` (Seize Objective): enemyArea, departLine, firePositions, direction, twoAssault, assaultRoute, battery | `unit-attack-to-objective` then hold | needs an area + a Line of Departure + optionally a route; all three derivable from the init (P2/P3) |
| SECURE | `company_seize` (seize then hold) or move-to + hold | `unit-attack-to-objective` + posture Deliberate-Defense | "secure" = take and hold; the hold half has no dedicated vendor task |
| OCCUPY | move-to + `occupy_firing_positions` | `unit-defend` + Change Posture Deliberate-Defense | occupy = move into prepared positions |
| DEFEND / BLOCK / RETAIN / GUARD | `plt_perimeter_defense`, `occupy_firing_positions`, `plt_support_by_fire`; Provide Stationary / Mobile Overwatch (params: overwatch location, maneuver elements, enemy area) | `unit-defend` (param fireSupportUnit) + Change Posture | BLOCK/RETAIN are defensive end-states, not distinct vendor tasks |
| DEFEND (air defence: T9, T10) | none at EL | `automatic_air_defense` (AggregateLevelBase, param = the area) | the ONLY native home for the 4 ADA tasks; ATL-only |

| | |
|---|---|
| C2SIM inputs needed | the objective/battle-position AREA (P3); for SEIZE a Line of Departure (one of the 41 unnamed Lines); Duration -> how long to hold (NOT parsed today); ROE. |
| Completion evidence | `vrf:endTask` for the scripted ones. "Objective held" has no native completion at all - a hold is open-ended. Synthesize: position-inside-area + dwell >= Duration, then emit TASKCMPLT ourselves. |
| Gaps | (i) 4 of the 14 carry ZERO geometry and are refused before any of this matters (T9, T10, T34, T38); (ii) Duration is not parsed; (iii) posture change can take hours of sim time (vrf_aggregatePosture.htm) - at demo speed that is a visible stall unless Set Posture (immediate) is used instead of Change Posture. |

### Rank 3 - BREACH: 3 tasks (T13, T19, T21)

| | |
|---|---|
| Today | DtBreachTask with only setBreachTarget, after an approach move; degraded to advance-only in G6 because the "obstacle" is the taskee itself. |
| Vendor, EL | `company_breach` (objective, lane1, lane2, enemyArea, supportFirePositions, battery) - a real combined-arms breach; `section_breach` (lane); `platform_breach` (breachingLane). DtBreachTask remains the primitive and also accepts setBreachStPt/EndPt. |
| Vendor, ATL | Breach Obstacles - "a breach line is specified with two points" (HELP\Tasks\AggregateLevelScenarios\BreachObstacles.htm); engineering unit required. |
| C2SIM inputs | a breach LANE = a two-point line -> `createPhaseLine` (vrfRemoteController.h:1054). STP ships 41 Lines; the task's 3-4 Locations also give a lane if the last leg is the lane. |
| Completion | DtBreachTask completes on breach completion; `company_breach` via endTask. |
| Gaps | our init creates no lines; no engineer template exists in the chain (the 40_EN battalion is proxied to a Tank HQ Section - unit-type-map row F-UCEC-F), so the breaching systems are absent; T21 has zero geometry. |

### Rank 4 - SCREEN 3 (+ SCOUT 29 in VRF-Approved)

| | |
|---|---|
| Today | DtPatrolRouteTask along the created route (VrfC2SimService.cs:2106) - correct primitive, already wired. |
| Vendor, EL | `patrol_route_script_with_options` (Route, treatRouteAsRoad, reverseDirection, startAtClosestVertex) - strictly better than the bare patrol; plus `requestSpotReports` for the reporting half. |
| Vendor, ATL | `reconnoiter-route` (route, duration, patrol, deployDrones, useRoads), `reconnoiter-location`, `reconnoiter-waypoint`, `perform-ground-reconnaissance`; plus posture Reconnaissance. |
| C2SIM inputs | Location[] -> route (have); Duration -> the `duration` parameter (not parsed); patrol=true. |
| Completion | Patrol NEVER self-completes (HELP\Tasks\MovementTasks\PatrolRoute.htm: "continues going back and forth until given another command"). `reconnoiter-route` DOES complete - it has a `duration`. That alone is a reason to prefer the ATL task: it turns a never-ending task into a bounded one, which is what an STREND chain needs. |
| Gaps | ATL-only; the duration must come from C2SIM Duration; a never-completing SCREEN as a chain predecessor stalls everything behind it (T24 -> T25 -> T26). |

### Rank 5 - MOVE 1 (but 38 in VRF-Approved and every live fixture)

| | |
|---|---|
| Today | CreateRoute + MoveAlongRoute, or MoveToLocation for a single point - already the closest native match. |
| Refinements available and unused | setStartAtClosestPoint / setTraversalDirection (moveAlongTasks.h); DtMoveToTask::setAtDistance (arrival radius); "treat route as road"; DtMoveIntoFormationTask (wired, opt-in); ATL `unit_movement_simplified` (destination, destinationPoint, useRoads) and `unit-trailing-march-to-objective` (objective, route, leadElements, trailingDistance) for follow-on echelons. |
| Completion | Unit move-along completes when the LEAD subordinate completes (moveAlongTasks.h:46 verbatim: "The pseudo-aggregate considers the Move Along task complete for the pseudo-aggregate as a whole when its lead subordinate completes the task") - premature by design, as the record already holds. |
| Gaps | none new; the open movement problem is the early-stop finding, not the task choice. |

### Rank 6 - ESCRT 1 (T28)

| | |
|---|---|
| Today | DtFollowEntityTask with ZERO offset - the follower stations on top of its leader. |
| Vendor, EL | `unit-ground-follow` (followSubject, followOffset) - sizes the offset by unit extent, which is exactly the fix for the zero-offset wart; `follow_along_offset_route` (route, leader, offset); DtConvoyToTask / DtConvoyAlongTask for a true convoy unit. |
| C2SIM inputs | the escorted unit (AffectedEntity - self-targeted in COA-STP1, so unusable) plus a trail offset (a policy constant, not in C2SIM). |
| Completion | Follow never self-completes. |
| Gaps | self-target makes ESCRT bare movement in COA-STP1; the convoy classes need a dedicated Convoy unit type we do not create. |

### Rank 7 - CLRLND 1 (T36)

| | |
|---|---|
| Today | classified Clear, not implemented -> bare movement. |
| Vendor, EL | `co_clear` (Limit of Advance line, Starting Point) and `plt_clear` - a REAL clear behaviour, contrary to the older note that no native clear exists. |
| C2SIM inputs | a Limit-of-Advance line and a start point - both derivable from the task's 3 Locations (first = start, last = limit) with no init lookup at all. |
| Completion | `vrf:endTask` when the survivors reach the limit. |
| Gaps | type filter (co_clear wants a company type); "Clear" must never be mapped to DtClearTask, which is a task-CANCEL SetDataRequest (clearTask.h:33) - that trap stands. |

### Rank 8 - the 33 unmapped STP codes

Cheapest correct treatment, in one edit each, once the families above exist: ATTMN,
ATTSPT, ARMAS, CTRATK -> Attack family. CTRFIR, SUPPRS, NTRCOM, HARASS -> attack-by-fire /
suppressive fire. DEFEAT -> Attack with a destroy end-state (DesiredEffectCode NUTRLD).
COVER, TURN, DELAY, WITHDR -> HoldObjective/defence family (TURN/DELAY/WITHDR have NO
vendor equivalent and stay logged gaps). FOLSPT, FOLASS -> Escort (`unit-ground-follow`).
OBSRV -> Reconnoiter. BYPASS, AMBUSH, SERCH, CNFPSL, CRESRV -> no vendor task; keep the
"unrecognised verb -> bare movement + warning" path but make the warning a reported
ObservationReport, not just a log line (L2 lane).

### 4.9 The one structural decision the mapping needs

Every richer mapping above is gated on ONE choice: does a COA-STP1 taskee run as an
EntityLevel unit of real vehicles (today) or as an AggregateTacticalLevel unit
(the Y-15 plan)? Both paths exist and both are real:

- STAY ON EntityLevel: reachable NOW, no profile switch. Gets SEIZE (company_seize),
  CLEAR (co_clear), BREACH (company_breach), attack-by-fire, occupy-firing-positions,
  a proper follow, and a better patrol. Blocked on the type filter (our proxies are
  platoons and HQ sections, not companies) and on units having declared functions.
- GO TO AggregateTacticalLevel: gets attack-to-objective, defend, reconnoiter-with-duration,
  automatic air defence and the whole posture model - the closest thing to STP's own
  semantics - at the cost of the profile switch, no entity-level interaction, and a
  re-validated type map (PLAN_AGGREGATE_LEVEL_PROFILE_2026-09-06).

Recommendation: BOTH, in that order. The EntityLevel items are demo-reachable before the
licence date; the ATL items are the Y-15 lane and land after. The verb table should carry
BOTH columns per verb and the profile switch should select the column, so neither path
forks the classifier.

---

## 5. BUILD LIST

Ordered by (demo value) / (cost), with the smallest enabling items first. Every item
names its vendor anchor, its C2SIM anchor, an OFFLINE test (a self-test in the existing
`--*-selftest` style, so the item is verified without a run) and the ONE live gate that
proves it. "User?" marks an item that needs a ruling before it is built.

### V1. Surface the task result (`success`) through the facade
- Vendor anchor: DtTaskCompleteReport::success() (MAK\include\vrftasks\taskCompleteReport.h:84-90).
- C2SIM anchor: TaskStatusCodeType (SDK\...\C2SIM_SMX_LOX_CWIX2024.cs:11077) - TASKCMPLT vs TASKABRT.
- Change: add `bool success` to vrf::TaskCompleted (VrfFacade.h:196-199), fill it in the
  report callback, marshal in VrfBridge, and let the service branch on it.
- Offline test: extend ReportSelfTest with a synthetic failed completion -> TASKABRT body.
- Live gate: any run where a scripted task returns endTask(false).
- Why first: without it EVERY scripted task below reports "completed" even when it failed,
  which is worse than today. Dovetails with L2 B1 (TASKSTRT/TASKABRT) - build once, there.
- User? no. Requires a native rebuild (standing authorisation, memory: native fixes).

### V2. Extend ScriptVar to the vendor's full variable contract
- Vendor anchor: DtScriptedTask::setValue overloads (scriptedTaskTask.h:90-106) - bool
  (DtScriptedTaskCheckBoxVariable), int, double, string, DtUUID, DtVector
  (DtScriptedTaskLocationVariable), DtEntityType.
- C2SIM anchor: none (a transport capability).
- Change: add Kind::Bool, Kind::Integer, Kind::Location (Geodetic), Kind::String to
  vrf::ScriptVar (VrfFacade.h:56-70) + VrfFacade.cpp:947-983 + VrfBridge.cpp:69-81,552-560.
- Offline test: a pure round-trip test of the ScriptVar -> DtRw* mapping.
- Live gate: issue `reconnoiter-route` with patrol=true, duration=300 and see the unit
  patrol and then STOP at 300 s.
- Why: without Bool/Integer/Location, reconnoiter-*, company_seize and co_clear cannot be
  parameterised at all. Cheap: one struct, three branches.
- User? no.

### V3. Create the init's LINES and POINTS as VR-Forces control objects
- Vendor anchor: createPhaseLine (vrfRemoteController.h:1054-1072, two points + optional
  startingUUID), createRoute (:1023-1039), createWaypoint (:991-1011); createControlArea
  already used (VrfFacade.cpp:725).
- C2SIM anchor: COA-STP1_Initialization.xml TacticalGraphic/{Line x41, Point x317,
  TaskGraphic x16}; parser today collects only S.TacticalAreaType (InitParser.cs:60,67,140).
- Change: parse Line / Point / Route / TaskGraphic into InitData; create each with its
  C2SIM UUID as the VRF UUID (same trick as areas); log a creation census.
- Offline test: `--parse-init` on COA-STP1_Initialization asserts 35 areas + 41 lines +
  317 points + 16 task graphics.
- Live gate: object-created callbacks for all of them in one init; count matches.
- Why: this is the parameter supply for every task in Section 4. It is also the cheapest
  item with visible demo value on its own (the objectives and phase lines appear on the
  operator's map).
- Risk: a 41-line, 317-point creation burst on top of 128 units; and createControlArea
  wants CONVEX polygons - STP areas may not be. Measure before assuming.
- User? no.

### V4. Resolve a task to its objective graphic - UUID LINKAGE ONLY (BUILT, 2026-09-14)
REVISED UNDER R1. The name/geometry heuristic this item originally proposed is WITHDRAWN:
the user ruled the missing MapGraphicID an STP export defect (STP-801), to be fixed by
re-exporting with IncludeMapGraphicIdInTasks=True, and the embedded Location is valid
C2SIM that stays supported alongside it. So the item is the LINKAGE and nothing else.
- Vendor anchor: none (interface logic).
- C2SIM anchor: ManeuverWarfareTask/MapGraphicID (schema :4080) - a LIST, now parsed whole
  into OrderTask.MapGraphicUuids; 0 present in both exports on disk.
- Change (BUILT, `TaskGeometryResolver.cs`): precedence is (1) each MapGraphicID resolved
  against the graphic created at init UNDER THE SAME C2SIM UUID - an area to its centroid,
  a line/point to its vertices, several ids to a route; (2) the embedded Location; (3)
  nothing, which is V9's in-place case. Which path was taken is LOGGED on every task. No
  name matching, no geometry-proximity matching, no verb-typed reading of the points
  (that is V4b, below).
- Offline test: `--rulings-selftest` section R1 (9 checks) + the `--parse-order` R1
  geometry census (COA-STP1: 0 with a MapGraphicID, 33 embedded, 9 with none).
- Live gate: an order carrying a MapGraphicID (an STP re-export, or a hand-edited fixture)
  dispatches to the init area's centroid and logs the uuid -> name line.
- User? RULED (R1).

### V4b. What the points MEAN, per verb (NOT BUILT - deliberately deferred)
The embedded Location is one list of points for every verb today: a route to drive. For a
HoldObjective verb the same list is really an objective to occupy, and for a fires verb a
target area. Splitting that reading per verb is a separate item from the linkage and was
explicitly held back on 2026-09-14; it belongs with V5/V6, which need the objective as a
task PARAMETER rather than as a route.

### V5. Wire the HoldObjective family (14 of 42 tasks) - EntityLevel first
- Vendor anchor: company_seize / co_clear / occupy_firing_positions / plt_perimeter_defense
  (EntityLevel\scripts\*.xml + .lua headers); help pages OffenseSeizeObjective.htm,
  OffenseClear.htm, OverwatchStationaryDefense.htm.
- C2SIM anchor: TaskActionCode SECURE/OCCUPY/SEIZE/RETAIN/BLOCK/DEFEND/GUARD; Duration
  (schema :4132, NOT parsed today - add it).
- Change: IsImplemented(HoldObjective) = true; dispatch = move-to-objective (unchanged)
  THEN a scripted task with the resolved area; hold synthesised from
  position-in-area + dwell >= Duration.
- Offline test: VerbMappingSelfTest gains a per-verb expected-composition assertion;
  a sequencer test for the synthesised hold completion.
- Live gate: ONE fixture, ONE taskee, SECURE with a resolved area: the unit moves to the
  area, stays, and a TASKCMPLT is emitted at the dwell deadline.
- User? RULED on both counts, and PART OF IT IS ALREADY BUILT.
  R4 settles the hold semantics this item was blocked on: SECURE completes AT THE END TIME
  (dispatch + Duration), and that half shipped on 2026-09-14 - a hold-type task already
  reports exactly one TASKCMPLT and releases its STREND successors WITHOUT any vendor
  scripted task. R3 settles the parameter: the objective, not an enemy entity. What is
  left in V5 is the VR-Forces composition itself (company_seize / co_clear /
  occupy_firing_positions / plt_perimeter_defense) so the units DO something at the
  objective instead of only standing on it; the reporting no longer waits on it.

### V6. Wire the ATTACK family onto the objective (19 of 42 tasks)
- Vendor anchor: unit-attack-to-objective (ATL) / plt_movement_to_contact +
  plt_attack_by_fire (EL); help OffenseMovementToContact.htm, OffenseAttackByFire.htm.
- C2SIM anchor: ATTACK/DESTRY/FIX/DISRPT/PENTRT + DesiredEffectCode (:4122, add to parser)
  + RuleOfEngagement (already parsed).
- Change: stop routing ATTACK through AffectedEntity (dead by construction); route it to
  the resolved objective area. Split the family: assault verbs -> attack-to-objective;
  FIX/DISRPT/SUPPRS -> attack-by-fire onto the area.
- Offline test: classifier + composition assertions for all five verbs; a test that a
  self-targeted task no longer takes the "advance only" branch (BUILT - R3 section of
  `--rulings-selftest`).
- Live gate: ONE fixture with a hostile unit inside the objective area: the tasked unit
  advances and engages it.
- User? RULED (R3): the target IS the objective; enemies may happen to be inside it. The
  DEFECT half shipped on 2026-09-14 - no verb is refused or degraded for self-targeting
  any more, and an ATTACK-family task routes to its own geometry. R4 also gives every one
  of these tasks an end time, so a fires task that can never be "achieved" still closes.
  What is left in V6 is the vendor composition (unit-attack-to-objective /
  plt_movement_to_contact / plt_attack_by_fire) onto the resolved objective.

### V7. Reconnoiter with a duration (3 SCREEN + 29 SCOUT)
- Vendor anchor: reconnoiter-route (ATL; route, duration, patrol, deployDrones, useRoads);
  patrol_route_script_with_options (EL).
- C2SIM anchor: SCREEN/SCOUT + Duration.
- Change: replace bare DtPatrolRouteTask with the scripted task when the profile is ATL,
  or with patrol_route_script_with_options on EL; pass Duration.
- Offline test: parser test that Duration reaches the task; classifier test.
- Live gate: the patrol stops at the duration and emits a completion - the thing bare
  PatrolRoute can never do.
- User? no.

### V8. Air defence (4 tasks, and the whole chain that G6 lost)
- Vendor anchor: automatic_air_defense (AggregateLevelBase\scripts; help
  AutomaticAirDefense.htm - parameter is an AREA).
- C2SIM anchor: DEFEND/OCCUPY on an ADA unit with ZERO Locations (T9, T10) or one
  (T11, T12).
- Change: for a zero-geometry task whose taskee is an air-defence unit, resolve the area
  from the init (V4) and issue automatic_air_defense instead of refusing.
- Offline test: the refusal path must become "resolve, then refuse only if unresolved".
- Live gate: ATL profile only.
- User? YES - this is ATL-only, so it is an explicit Y-15 item, not a demo item.

### V9. Zero-geometry policy (9 of 42 tasks) - WHO-UNIT GEOMETRY (BUILT, 2026-09-14)
REVISED UNDER R2: a task without geometry uses the geometry of the PERFORMING (who) unit.
- Vendor anchor: n/a.
- C2SIM anchor: the 9 tasks with 0 Location elements (confirmed offline by the
  `--parse-order` R1 census: 9 of 42).
- Change (BUILT): the refusal + chain-abandon is gone. A zero-geometry task now dispatches
  IN PLACE - TASKSTRT at dispatch, no vendor move issued (the unit holds where it is), a
  NameObservation on the existing R-SURFACE-PROXY observation channel carrying the
  ruling's own sentence ("no geometry in the order: executing at the performing unit's
  position"), completion at R4's end time, and the STREND chain continues. NO destination
  is recorded, which is what keeps the progress watchdog off a unit that is correctly
  standing still. The only refusal left is the one that was never about geometry: there is
  no performing unit to task.
- Offline test: `--rulings-selftest` section R2 (7 checks), including the successor of an
  in-place task dispatching at its predecessor's end time; the `--parse-order` R1 census
  counts the 9.
- Live gate: the ADA chain T9-T12 dispatches instead of collapsing, and T10-T12 are no
  longer skipped as "predecessor abandoned upstream".
- User? RULED (R2). Remaining zero-geometry tasks whose own NAME references a graphic
  (T16 OBJ MONROE, T34 OBJ MADISON, T21 PL GOLD, T10 PL BRONZE) are STP-side issues to
  raise with the exporter - deliberately NOT name heuristics here.

### V10. Follow with a real offset (ESCRT, FOLSPT, FOLASS)
- Vendor anchor: unit-ground-follow (followSubject, followOffset) or
  DtFollowEntityTask offset (vrfRemoteController.h:1673-1677: "To specify an offset in
  front, to the right, or below, use a negative sign for x, y, or z").
- C2SIM anchor: ESCRT + AffectedEntity.
- Change: pass a configurable trail offset instead of zero (VrfFacade.cpp FollowEntity).
- Offline test: none meaningful; a facade argument test.
- Live gate: the follower trails rather than stacking.
- User? no. Small, and it removes a visibly wrong behaviour in the demo.

### V11. Map the remaining 33 STP codes
- Vendor anchor: as in Section 4.8.
- C2SIM anchor: STP C2SimTask.cs:126 MWTaskCode.
- Change: table rows only (VerbMapping.cs:84-101), plus "no vendor equivalent" rows that
  log and report a gap rather than silently moving.
- Offline test: VerbMappingSelfTest asserts all 51 STP codes classify, and that the
  no-equivalent ones are flagged.
- Live gate: none (pure).
- User? no.

### Suggested order
REVISED 2026-09-14 after the rulings. V4 (uuid linkage) and V9 (who-unit geometry) are
BUILT, and R4's timed completion - which was not an item in this list at all - turned out
to be the thing that made a COA-STP1 chain run to its end at all. What remains:
V1, V2, V3 (enablers, all offline-testable) -> V5, V6 (the vendor compositions; their
reporting and parameter questions are now settled) -> V4b (what the points mean per verb)
-> V7, V10, V11 -> V8 with the Y-15 profile.
Superseded order, for the record: V1, V2, V3 -> V4 (+user ruling) -> V5, V6 -> V9 ->
V7, V10, V11 -> V8.

Cross-lane: V1 is L2's B1; V3 and V4 produce the objective overlay the operator view
(row 15) wants; V9 changes what the reporting lane has to report. Build V1 once, in L2.

---

## 6. VERIFIED vs ASSUMED, and where the docs are silent

### 6.1 VERIFIED this pass (primary source read or measured, not recalled)

- COA-STP1 verb counts, per-task locations, self-target 42/42, STREND 31, ROEHold 42,
  DesiredEffectCode split: parsed from data\COA-STP1_Order.xml.
- Verb counts of all 24 other order files on disk: greped.
- COA-STP1_Initialization graphic census (409 / 317 / 41 / 35 / 16) and the area NAMES:
  parsed from the XML.
- 35 areas created live, with C2SIM UUID as VRF UUID: G6 log line + VrfFacade.cpp:725-737.
- STP's 51 emittable codes: re-derived from STP\...\C2SimTask.cs:126 this pass.
- The C2SIM enum has 453 members and is named TaskActionCodeType: SDK file, lines counted.
- The interface's verb table, dispatch branches and refusal path: read in
  VerbMapping.cs and VrfC2SimService.cs at the lines cited.
- RunScriptedTask / SendScriptedSet exist end-to-end and have no C# caller: read in
  VrfFacade.h/.cpp and VrfBridge.cpp; greped the app for callers.
- FollowEntity sends a zero offset; PatrolRoute sets only the route; Breach sets only the
  target: read in VrfFacade.cpp.
- TaskCompleted drops success(): read VrfFacade.h:196-199 against taskCompleteReport.h:84-90.
- The 70 Dt*Task classes and the ABSENCE of any attack/defend/seize/secure/screen task
  class: extracted from all 279 headers in MAK\include\vrftasks.
- sendTaskMsg is the generic channel and the convenience movers sit on it:
  vrfRemoteController.h:1632-1697.
- createPhaseLine / createRoute / createWaypoint / createControlArea signatures, including
  the convex-polygon note and the startingUUID parameter: same header, :991-1115.
- The vendor sample issues only moveToPoint / moveToWaypoint / assignPlanByName / a
  plugin-gated fire interaction: read commandLineRemoteController.cxx.
- The EntityLevel scripted-task inventory (412 files, menu categories) and the
  AggregateTacticalLevel inventory (54): enumerated from the shipped .xml metadata.
- taskParameters for unit-attack-to-objective, unit-defend, unit-trailing-march-to-objective,
  group-attack-to-objective, reconnoiter-{route,location,waypoint},
  perform-ground-reconnaissance, unit_movement_simplified, company_seize, company_breach,
  section_breach, platform_breach, unit-ground-follow, follow_along_offset_route,
  co_tactical_march, patrol_route_script_with_options, find_firing_positions,
  unit_artillery_indirect_fire_at_location: read from the .lua headers.
- Scripted tasks end with vrf:endTask(bool): 544 call sites counted in the EntityLevel set.
- Vendor help text quoted for seize / clear / movement-to-contact / attack-by-fire /
  assault / overwatch / coil / follow-along-offset-route / patrol-route / move-along-route /
  move-into-formation / posture / automatic air defence / tasks-intro mutual exclusion /
  aggregate-vs-entity SMS incompatibility.
- G6 outcomes: 9 dispatched (all bare move-along), 1 refused for no location, 3 skipped,
  0 completions, 0 task-status reports, and the exact log lines for each; plus the type
  templates the taskees were created as.
- Our aggregate object types vs the scripts' myEntityTypes filters:
  data\unit-type-map-52-nolifeform.json rows against the .xml metadata.
- G6 ran EntityLevel.sms: runs\20260914T002716Z_run\launchvrf.stdout.log.

### 6.2 ASSUMED (carried from the record, NOT re-verified this pass)

- The STP-side export semantics (C2SimXmlBuilder.cs:420-475): that Location[] is the
  linearised route, that AffectedEntity is deliberately the performing unit, and the
  IncludeMapGraphicIdInTasks / PlaceAllTgInInitialization defaults. Taken from
  STP_TASK_VOCABULARY_2026-09-03.md; only C2SimTask.cs was re-read this pass.
- The 266 STP activity signatures and the HOW x WHAT x WHO model: same source, unread here.
- That unit move-along completions fire both early and never (SEMANTIC_MAPPING sec 7.3,
  GT 0.0 item 5) - carried, and consistent with moveAlongTasks.h:46 which I did read.
- That the aggregated/disaggregated combat constraint of the 5.0.2 docs still holds in 5.2
  in the same words. I did not re-find that sentence in the 5.2 help this pass; the 5.2
  aggregate model I did read (vrf_aggregateCombat.htm) describes ATTRITION-based unit
  combat, which is a different mechanism from "only disaggregated units can engage". Treat
  TASK_VOCABULARY_V2 sec 0 item 2 as UNCONFIRMED for 5.2 until someone re-reads it.

### 6.3 Where the docs are SILENT (run-only questions)

1. Whether the sim engine enforces a scripted task's myEntityTypes list for a task
   arriving over the network, or only in the GUI menu. The help describes it only as menu
   availability and states the default is "available to all simulation objects".
2. Whether createControlArea accepts a non-convex STP objective polygon, and what it does
   if not ("the vertices should form a convex polygon" is advice, not a documented error).
3. What a scripted task does when a required SimObject parameter is nil - several .lua
   files guard for it (unit-attack-to-objective:186, :386, :513) but the behaviour is
   per-script, not a documented contract.
4. Whether a scripted task assigned remotely reports completion through the same
   DtTaskCompleteReport channel our facade already listens on. The header contract implies
   yes; nothing states it.
5. Arrival tolerances for any of the unit behaviours; and any numeric contract for "the
   objective is secured".
6. Whether an EntityLevel unit whose type is a Tank Platoon can be given a company-level
   script at all (a consequence of 1).

### 6.4 Adversarial notes on this document

- Competing frame considered and rejected: "the fix is a richer verb table". It is not.
  Nine of 42 tasks are refused for missing geometry and 42 of 42 self-target, so a richer
  table alone changes the behaviour of zero G6 tasks. The binding constraint is the
  PARAMETER SUPPLY (graphics), which is why V3/V4 precede V5/V6.
- Competing frame considered and rejected: "wait for Y-15 / AggregateTacticalLevel". Half
  the value (seize, clear, breach, attack-by-fire, occupy, follow, better patrol) is in
  EntityLevel, which is what we already run - so gating all of it on the profile switch
  would delay the demo-visible half for nothing.
- The claim that "all this is reachable cheaply" would be falsified by a live gate showing
  the sim ignoring a scripted task sent to a type outside its myEntityTypes list (silent 6.3
  item 1) - in that case V5/V6 need the type map changed too (companies must be created as
  company types), which is a bigger, separate change. That gate is the first thing to run.
- One symptom is unexplained and is recorded as such in Section 2.3: the 29 never-dispatched
  G6 tasks produced no gate-timeout warning in the ~24 minutes of log scanned. It does not
  change any conclusion here (the positive dispatch evidence is complete) but it is a real
  defect candidate and belongs to the reporting lane.

---

## 7. WHAT NEEDS A USER RULING BEFORE ANY BUILD

| # | question | why it blocks |
|---|----------|---------------|
| R1 | Is task-to-objective resolution by NAME/GEOMETRY acceptable, or must STP re-export with MapGraphicID? | decides V4, and therefore V5-V9 |
| R2 | What does a zero-geometry task mean - in-place continuation, or a refusal? | 9 of 42 tasks; today it silently kills whole chains |
| R3 | Is "the enemy is whatever is inside the objective area" the intended semantics for the ATTACK family? | decides V6; it is what both STP and VR-Forces assume |
| R4 | When is SECURE / OCCUPY / DEFEND complete - arrival, Duration, or never? | decides the synthesised completion in V5, and what STP sees |
| R5 | EntityLevel first, AggregateTacticalLevel later (the recommendation), or straight to ATL? | decides which column of the verb table gets built first |
| R6 | Do we change the type map so COA-STP1 companies are created as COMPANY types (needed for company_seize / co_clear / company_breach)? | interacts with the settled fidelity-table rulings |

R1, R2 and R4 are also the natural additions to the five STP questions already drafted in
docs\DRAFT_STP_QUESTIONS_2026-09-14.md (lane L2, item B10).

---

### 7.1 Rulings received 2026-09-14

Doctrine research pass (docs/experiments/DOCTRINE_FOR_TASKING_RULINGS_2026-09-14.md,
Opus executor, ~14:10Z) read FM 3-90 / ADP 3-90 / FM 1-02.1 and re-parsed the init:
every one of the 409 init MapGraphic elements carries an APP6C-SIDC (409/409), and 16
of them ARE FM 3-90 Appendix B tactical-mission-task symbols. Separately, the exported
TaskActionCode disagrees with the drawn task SYMBOL in at least 4 places (three FOLLOW
AND SUPPORT `GFTPAS`, one FOLLOW AND ASSUME `GFTPA`, one NEUTRALIZE `GFTPN` symbol
exist in the init, but FOLSPT/FOLASS/NTRCOM appear in none of the 42 task codes) - an
unexplained STP-side symptom, not resolved here. The user ruled on R1-R4 the same day;
R5 and R6 remain open. Citations below are to that doctrine record unless noted.

- **R1 RULED - STP export defect, not an interface problem.** The missing MapGraphicID
  is not ours to fix. Verified: COA-STP1_Order.xml carries 0 MapGraphicID elements and
  66 embedded GeodeticCoordinates; STP's C2SimXmlBuilder emits MapGraphicID only when
  IncludeMapGraphicIdInTasks is set (C2SimBridgeAgentParams.cs:128; C2SimXmlBuilder.cs
  ~383/460), which was OFF for this export; absent that flag, Location is the FIRST
  tactical graphic linearised (a live `// TODO: multiple TGs` in the builder), and the
  task.Objective emission branch is commented out entirely. Fix belongs on the STP side
  (re-export with the flag on); the interface links task -> graphic by UUID, since the
  init already creates every area under its own C2SIM UUID. V4's name/geometry
  heuristic as originally written is WITHDRAWN; keep a reported fallback only for the
  transition period before STP re-exports.

  USER 2026-09-14: the embedded Location is valid C2SIM and stays supported
  alongside MapGraphicID; gap = verb-typed interpretation of the points (route /
  area polygon-line-point-with-radius / line), on-the-fly creation of the
  VR-Forces control object from them, precedence MapGraphicID > embedded,
  consistency check when both; inherent limits reported not guessed (first graphic
  only, single point cannot identify the graphic in a cluster - STP-801);
  RelativeLocation unsupported (STP does not emit it). Follow-up build item V4b.

- **R2 RULED - a task without geometry uses the geometry of the performing (who) unit.**
  Execute in place and report the derivation; do not refuse and do not silently invent a
  location. Zero-geometry tasks whose own statement names a graphic (T16 OBJ MONROE,
  T34 OBJ MADISON, T21 PL GOLD obstacle belts, T10 PL BRONZE) are STP-side issues to
  raise with the exporter, not interface heuristics to build around.

- **R3 RULED - the target IS the objective (doctrinal); enemies may happen to be inside
  it.** VR-Forces' own tactical tasks agree: company_seize, co_clear, company_breach,
  plt_attack_by_fire and unit-attack-to-objective all take the objective graphic as
  their parameter, never a named enemy entity. The interface's self-target-is-an-error
  logic (VrfC2SimService.cs :1813-1815, :1820-1822) is the defect to remove, not the
  order to fix.

  USER 2026-09-14: confirmed aligned with STP's own task model (C2SimTask:
  Who/What/How/Objective-by-SIDC/Routes/Tgs/Start/End; no enemy or target-entity
  field; task_generation_tables.pl defines seize/clear/breach/block/destroy by their
  task graphic and the objective by the objective-area graphic); destroy vs defeat
  by DesiredEffectCode.

- **R4 RULED - completion is given by the END TIME = StartTime + Duration.** Verified
  in the order itself: Duration is present on all 42 tasks (32 x PT1H20M, 10 x PT2H),
  StartTime is a relative delay (0 on 41, 3h20m on T13), EndTime is absent. The
  interface must parse Duration (dropped today by OrderParser) and close hold-type
  tasks (SECURE/OCCUPY/DEFEND/RETAIN/BLOCK/FIX/SCREEN/GUARD) with TASKCMPLT at that sim
  time; evaluable tasks (SEIZE/OCCUPY arrival, BREACH, MOVE) may still complete earlier
  on their own evidence.

  **R4's CLOCK IS THE SIMULATION CLOCK** (Q2, **RULED 2026-09-14 (user)**: the
  supervisor default stands). `Vrf:TaskClock`, default
  `"sim"`, with an automatic WALL fallback whenever `DtVrfRemoteController::simTime()`
  cannot be read or has gone stale. The ruling above already says "at that sim time";
  what the setting adds is that the SAME clock carries all THREE task times - the
  Duration that ends a task, the StartTime/DelayTimeAmount delay that holds one back,
  and the STREND predecessor gate. Before the review fixes the Duration was served on
  `Vrf:StallClock` (the progress watchdog's knob, default wall) while the other two were
  pure wall `Task.Delay`, so at COA-STP1's measured sim ratios (0.27x-0.73x) a gate
  expired three to four times too early and every successor was skipped.
  `Vrf:StallClock` now governs the progress watchdog ONLY.

  **THE OTHER SIX REVIEW QUESTIONS - ALL RULED 2026-09-14 (user).** Q1-Q3 confirm the
  supervisor defaults; Q4 REPLACES one; Q5-Q7 were raised by the pass-2 review of
  `0c96f50` and are answered here for the first time. Nothing below is "pending".
  - **Q1, a SUPERSEDED task - RULED (user)**: `Vrf:SupersededTaskCode`, default
    `TASKABRT` AT THE SUPERSEDE POINT - the taskee is demonstrably not performing it, and
    the interface's own log already said so while its report stream said the opposite -
    **AND its successors are abandoned immediately** (`821b359`), so each reports its own
    TASKABRT at once instead of waiting out a gate of up to 7,260 s. `TASKCMPLT` (the
    literal reading: the order says when the task ends, whatever the simulator did) is
    selectable and deliberately does NOT abandon. Not reachable on COA-STP1 under
    `PredecessorTimeoutPolicy=skip`.
  - **Q3, the WIRE AMBIGUITY is ACCEPTED - RULED (user)**: STP sees TASKSTRT + TASKCMPLT
    for a zero-geometry in-place task exactly as for a performed one; the derivation goes
    out as an ObservationReport, which STP discards (STP-800). Recorded, not worked around.
  - **Q4, a task with NO Duration AND no geometry - RULED 2026-09-14 (user),
    REPLACING the supervisor default**: such a task is **MALFORMED** and is REFUSED.
    No hold is invented and `Vrf:DefaultHoldSeconds` is DELETED. The task gets an
    ERROR naming both missing elements, a TASKABRT through the single emit point and
    a `NotifyAbandoned`, so its successors fail fast like every other refusal - a
    number that is not in the order is not this interface's to invent, and a chain
    built on one is worse than a chain that stops with a named cause. None of
    COA-STP1's 42 tasks is affected: all 42 carry a Duration. Built in `168207f`.
  - **Q5, does a PAUSED scenario age a task? NO - RULED 2026-09-14 (user)**, as the
    pass-2 review recommended. M4's cure for a frozen reader served WALL seconds after
    60 s of flatness, so a ten-minute pause burned 600 s off every armed Duration. The
    task clock now HOLDS on the sim axis while a VR-Forces back end is still present and
    falls back to wall only when there is none (`f2794d7`, `StallPolicy.TaskClockAction`).
    **THE LIMIT, recorded rather than papered over**: the only liveness the facade exposes
    is `BackendCount` -> `backends().count()`, and that list KEEPS a back end deactivated
    for missing its status timeout, so on this signal a DEAD back end holds task time
    exactly as a paused one does. The hold line therefore REPEATS instead of being said
    once, and says so. OWED (needs a C++ facade change): expose
    `DtVrfBackendListener::lookupBackend(addr)->status()` (Paused vs Playing) or
    `getControlState(addr)` and decide on that.
  - **Q6, the interim demo setting - RULED 2026-09-14 (user)**: NO stop-gap. A1
    (`b6471a3`) is the answer; `Vrf:TaskPredecessorTimeoutSeconds` is NOT raised to
    20,000 anywhere, and `appsettings.Demo.json` keeps its 7,200 as the floor it always
    was.
  - **Q7, a task after a `rollbackToSnapshot` - RULED 2026-09-14 (user), ACCEPTED as
    recorded**: the axis adds FORWARD movement only, so the re-simulated stretch is served
    TWICE and the task ends LATER in scenario time than the order says. That is the
    intended trade against a deadline STAMP, which would fire the moment a rollback landed
    past it. Stated where the clock is configured (`VrfSettings.TaskClock`).

- **R5 OPEN - EntityLevel first vs straight to AggregateTacticalLevel.** Explained to
  the user 2026-09-14; doctrine does not settle it directly (an engineering/schedule
  call).

  RULED 2026-09-14 (user): proceed as recommended (EntityLevel first, aggregate
  profile second) BUT the product must offer the user the OPTION of entity-level or
  aggregate-level mode with the limitations of each documented; a scenario is one
  mode, never mixed.

- **R6 OPEN - whether to change the type map so COA-STP1 companies are created as
  COMPANY types.** Explained to the user 2026-09-14; doctrine note: the type should
  follow the taskee's ECHELON, not a global switch (company-typed vendor tasks
  implement company-level doctrine, and COA-STP1's taskees are a mix of battalions and
  companies) - a shape constraint on the answer, not a decision.

  CLARIFIED 2026-09-14: R6 is NOT a mixed entity/aggregate mode (user: 'a bridge too
  far'); it is the DIS type of the EntityLevel aggregate object that the vendor
  script's myEntityTypes filter checks. Options restated: (a) fan out to composed
  companies for battalion taskees (SubordinateFanOut exists, default off) -
  doctrinally right; (b) widen the filter in our copy of the script (custom
  including SMS) - transition only; (c) one run to test whether the filter is
  enforced over the remote-control channel at all. Recommendation: (c), then (a),
  (b) as transition. Ruling still owed.

---

### 7.1a STATUS 2026-09-14 (branch `feat/tasking-rulings`) - R1-R4 BUILT AND REVIEW-FIXED, LIVE CONFIRMATION OWED

R4 `746c091`, R2 `1f65f55`, R3 `0cd8905`, R1 `0193379`; merged with `feat/integration` at
`182bd51`; then the cold-start review of `5c67d41` (verdict FIX FIRST, in-repo at
`docs/experiments/REVIEW_RULINGS_5c67d41_2026-09-14.md`) fixed in `075c0b7` (M1),
`3fe69fa` (M2), `48e7c5d` (M3+M4), `1ddb9a7` (M5), `06f8cf0` (m1-m9 + the Q1/Q4 defaults)
and `6d46921` (tests); then the PASS-2 review of `0c96f50` (verdict FIX FIRST again, on
one item) fixed in `b6471a3` and the commits listed in 7.1b; then the PASS-3 review of
`8db033e` (verdict FIX FIRST again, on two items - and A1 independently re-derived and
CLOSED at 42/0) fixed in `5f661f5` (D1), `fc23f14` (D2), `2cbd722` (E1), `846de1d` (E3),
`c4f7785` (E4+E6), `9a0a928` (E5) and the docs commit carrying 7.1b's pass-3 table.
Offline only: every check is a `--rulings-selftest` (136 checks across SIX sections - the
count 7.1b quotes, and the one the suite prints) or a `--parse-order` census, and
all 18 self-tests stay green (typemap 783, scripted-task, initgraphics, preflight
included). NOTHING here has been run against VR-Forces yet.

- **R4 BUILT** - `OrderParser` lifts Duration (and the absolute StartTime form);
  `TimedCompletionPolicy` arms one timer per task at dispatch and measures ELAPSED clock,
  not a deadline stamp, so a paused scenario does not age a task and a
  `rollbackToSnapshot` does not complete one early; the service walks it on the tick
  thread against the SAME clock the progress watchdog uses (`Vrf:StallClock`), pushes ONE
  TASKCMPLT through `PushTaskStatus` and releases the STREND gate. Any TASKCMPLT/TASKABRT
  cancels the timer; TASKSTRT/TASKINPRG do not. Config: `Vrf:TimedCompletion` (default ON),
  `Vrf:DurationScale` (default 1.0) - the scale applies to BOTH the Duration and the
  StartTime delay, so a compressed demo does not still wait 3h20m for T13's successor. The
  VR-Forces task is deliberately NOT cancelled at the end time. SINCE THE REVIEW FIXES the
  walk runs on R4's OWN clock (`Vrf:TaskClock`, default `sim`), not the watchdog's, and the
  predecessor gate is derived from the predecessor's armed end time - see 7.1b.
- **R2 BUILT** - `TaskDispatchPolicy.ForZeroGeometry`. A zero-geometry task dispatches in
  place with TASKSTRT, no vendor move, a NameObservation carrying the ruling's sentence,
  R4's end time and an unbroken chain. The only refusal left is "no performing unit".
- **R3 BUILT** - `TaskDispatchPolicy.ForTarget`. The three self-target guards (ATTACK,
  BREACH, ESCRT) are one resolver; self is `SelfIsObjective`, not an error and not "no
  target"; nothing is refused for its target resolution; a DISTINCT resolved entity is
  still engaged as an entity. No new vendor tactical task (V5/V6 still to come).
- **R1 BUILT (transition)** - `TaskGeometryResolver`. MapGraphicID(s) -> the graphic
  created at init under the same C2SIM uuid (area -> centroid, line/point -> vertices,
  several ids -> a route), else the embedded Location, which the user ruled is valid C2SIM
  and stays supported; the path taken is logged on every task. No name heuristics, and no
  verb-typed reading of the points (V4b).

### 7.1b THE THREE COLD-START REVIEWS OF THIS BRANCH - ITEM TABLE WITH STATUSES

Three reviews. The second found that the first one's headline fix had been MOVED rather than
closed; the third found that the second one's own follow-up fix guarded the dispatch path the
demo does NOT take. Nothing here is "all fixed": the tables below are the status of every item.

#### PASS 1 - review of `5c67d41` (in-repo, `docs/experiments/REVIEW_RULINGS_5c67d41_2026-09-14.md`)

Five MAJOR items, one of which meant the branch did not achieve its own stated purpose on
the order it was built for. What changed:

| item | what was wrong | fix | sha |
|------|----------------|-----|-----|
| M1 | The STREND gate expired at dispatch + `TaskPredecessorTimeoutSeconds` (600 s) while the timed completion fires at dispatch + Duration (4,800 / 7,200 s) - an ordering proof, not a race. All 31 gated COA-STP1 tasks were SKIPPED with TASKABRT: 11 dispatches, not 42. | The gate is `max(configured, predecessor Duration x DurationScale + Vrf:TaskPredecessorEndMarginSeconds)`. `TaskSequencer` takes the window in seconds and an INJECTED clock. | `075c0b7` |
| M2 | Two clocks: the Duration on `Vrf:StallClock`, the start delay and the gate on pure wall `Task.Delay`. At 0.27x-0.73x sim ratio the gate expired 3-4x too early. R4's semantics were also controlled by a knob named for the watchdog. | `Vrf:TaskClock` (default `sim`) + ONE MONOTONE AXIS that adds forward movement only, carrying all three task times. `Vrf:StallClock` governs the watchdog only. | `3fe69fa` |
| M3 | The timed walk took the RAW clock mode where the watchdog takes the hysteresis-CONFIRMED one, so an alternating -1 / >= 0 reader re-anchored every walk: no task ever completed, silently, forever. | `SimClockTracker` - the sim clock is read ONCE per sample and judged once for both consumers. | `48e7c5d` |
| M4 | No stale-clock detection in the timed walk. A deactivated back end is not removed, so the reader returns its last value for the rest of the run and every end time froze. | The shared tracker's `Stale` verdict; the task clock falls back to WALL seconds and keeps serving, warning once each way. | `48e7c5d` |
| M5 | `_graphicsByC2SimUuid` was written only from `init.Areas`: 35 of the init's 409 graphics. A MapGraphicID naming a phase line or an axis of advance matched nothing and the task fell through to the embedded Location - or, on an export that drops it, to R2 IN PLACE. | Lines and points are registered too, UNCONDITIONALLY (the map holds authored points, not VR-Forces objects, so the create flags are irrelevant to it). An unmatched id is now a WARNING. | `1ddb9a7` |
| m1-m9 | The superseded task still reported TASKCMPLT; the in-place dispatch cleared state for a command it never issued; `Unresolved`/`NoTarget` were relabelled as R3; the month term was 30 HOURS; the timer survived the empty-taskee guard; the real "no performing unit" path was silent; a discarded embedded Location said nothing; `DurationScale` had two opposite readings; an in-place task stayed `IsBusy` forever. | All fixed; Q1 and Q4 answered by the supervisor defaults recorded in 7.1. | `06f8cf0` |
| n8 | The 41-check suite was pure-function checks of 3-line methods; `(d2)` could not fail. | 78 checks (the brief said 77; the pass-2 review counted them), including service-level ones on real objects and FAIL-FIRST controls for M1, M3 and M4. | `6d46921` |

NEW CONFIG KEYS (all documented in `VrfSettings.cs` and `docs/RUNBOOK.md` sec 11):
`Vrf:TaskClock` (`sim`), `Vrf:TaskPredecessorEndMarginSeconds` (60),
`Vrf:SupersededTaskCode` (`TASKABRT`), `Vrf:TaskChainBackstopSeconds` (86400, added by
pass 2's A1). `Vrf:DefaultHoldSeconds` (60) was added by pass 1 and DELETED again by the
user's Q4 ruling of 2026-09-14 - a malformed task is refused, not held.

#### PASS 2 - review of `0c96f50` (in-repo, `docs/experiments/REVIEW2_RULINGS_0c96f50_2026-09-14.md`)

Verdict **FIX FIRST**, on M1: it was fixed one level down and re-created one level up. All
FIX-FIRST items are now closed; at the end of pass 2 the suite was **112 checks** (it is **136**
after pass 3 - see below) and all 18 offline suites exit 0.
NOTHING below has been run against VR-Forces.

| item | what was wrong | status |
|------|----------------|--------|
| **A1** | **M1 WAS MOVED, NOT CLOSED.** The derived window covers the predecessor's DURATION but not its LEAD TIME, and phase 1 of the gate ("has it dispatched at all?") runs from ORDER RECEIPT - `HandleOrder` starts all 42 orchestrations in one loop. MEASURED on the real classes over the whole order graph: **21 dispatches, 21 TASKABRT(SKIPPED)** at the 600 s default AND at the Demo overlay's 7,200, and NOT DETERMINISTIC at `DurationScale=0.05`. | **FIXED `b6471a3`** - phase 1 takes its own window: `Vrf:TaskChainBackstopSeconds` when the predecessor is a task in this order (a real dead end ABANDONS it), the configured value for a DANGLING reference. 15 new checks walk WHOLE graphs, including COA-STP1 off disk: 42 / 0, deterministic over 20 repetitions at 0.05. |
| **A2** | `docs/RUNBOOK.md` sec 11 told the operator the OPPOSITE of what the code did ("you no longer need to raise `TaskPredecessorTimeoutSeconds`"), sec 11 sat ABOVE sec 10, and this section claimed ALL ITEMS FIXED. | **FIXED `4ca7afa`** - sec 11 states both windows and what was true before A1 (>= 20,000 s, measured), sec 11 now follows sec 10, and this table replaces the claim. |
| B1 | The Q1 supersede TASKABRT does not call `_sequencer.NotifyAbandoned`, so the successors of a task the interface has just told STP is NOT being performed still wait out the full derived gate. | **FIXED `821b359`** - and the user's Q1 ruling says the same. The branch decision is a named predicate; `TASKCMPLT` deliberately does not abandon. |
| B3 | `Vrf:DefaultHoldSeconds` is invisible to the gate derivation, so the two knobs can be set to contradict each other with no warning. | **MOOT** - the user's Q4 ruling of 2026-09-14 removes the invented hold entirely (a task with no Duration AND no geometry is MALFORMED and is refused), so there is no second knob to cross-check. |
| B4 | The derivation ignores `Vrf:TimedCompletion`: with timed completion OFF every gated successor still waits `predDuration + 60` instead of the configured window. | **FIXED `df7f7e2`** - measured: the same chain gave up after 7,260 s and now gives up at the configured 600 s. |
| B6 | None of the R4 keys is in either settings file, on a branch whose next milestone is a STANDALONE demo deployment. | **FIXED `d792901`** - seven keys in `appsettings.json` at their defaults, six in `appsettings.Demo.json` with a `_Key` line each; RUNBOOK sec 11 rewritten. |
| B7 | The gate-timeout message cannot tell a phase-1 timeout ("never dispatched") from a phase-2 one ("did not complete"), so the log mis-states the cause. | **FIXED `ba44d7e`** - `GateResult.PredecessorNeverDispatched` is its own outcome and `TaskDispatchPolicy.GateFailureReason` is a pure function whose two sentences the suite locks verbatim. |
| B2 | A superseded move's LATE vendor completion is attributed to the NEW task (TASKCMPLT for a task that has not finished). Needs a supersede, which COA-STP1 does not reach under `policy=skip`. | RECORDED DEBT |
| B5 | An ABSOLUTE `StartTime` is a WALL instant served on the SIM axis, and scaled. No order on disk reaches it (STP exports the relative form). | RECORDED DEBT |
| Q4, Q5 | Not review items but USER RULINGS taken in the same pass, and both change code the review had accepted: a zero-geometry task with no Duration is MALFORMED and is REFUSED (`Vrf:DefaultHoldSeconds` deleted), and a flat sim clock HOLDS task time while a back end is present instead of falling to wall after 60 s. | **BUILT `168207f`, `f2794d7`** - see 7.1 |
| C1-C15 | Doc misattachment (C1), the dead mode-change re-anchor (C2), the quantified staircase error (C3), the new per-second bridge read (C4), PAUSED vs DEAD (C5, and Q5 to the user), flat-time accounting (C6), a non-volatile carrier struct (C7), the uncovered `SampleTaskClock` glue (C8), a stale comment (C9), section order (C10, fixed by A2), a registration-window ordering nit (C11), the area "centroid" (C12, pass-1 n1), pre-existing CS8632 (C13), the 5 Hz gate poller (C14), the clean ASCII/CRLF audit (C15). | RECORDED DEBT |

#### PASS 3 - review of `8db033e` (in-repo, `docs/experiments/REVIEW_RULINGS_8db033e_2026-09-14.md`)

Verdict **FIX FIRST**, on two items. The reviewer re-derived A1's outcome independently - its
own parse of `COA-STP1_Order.xml` plus a hand re-implementation of the three window formulas -
and got 42 dispatches / 0 skips at `DurationScale` 1.0 AND 0.05, at the shipped 600 s floor and
at the Demo overlay's 7,200, so **A1 IS CLOSED** by two independent derivations. The two
FIX-FIRST items were not in that work. The suite is now **136 checks**, 0 failures, and all 18
offline suites exit 0. NOTHING below has been run against VR-Forces.

| item | what was wrong | status |
|------|----------------|--------|
| **D1** | **`b76c9c7` guarded the enqueue the demo does NOT take.** `Vrf:GroundWaypointAltitudeMode` defaults to `"TerrainProfile"` and neither settings file overrides it, so for a ground unit with route points the guarded first pass of `ExecuteTaskOnTick` only asks the back end for terrain heights and RETURNS with nothing marked. The REAL dispatch is the re-entry from the terrain reply (or the timeout sweep) - it creates the route, calls the bridge and runs `MarkDispatched` - and it was a bare lambda. A throw there reached only the tick drain's catch, which logs and returns: no TASKSTRT, no TASKABRT, no abandon, and successors parked on the 86,400 s chain backstop because A1 removed the configured bound for an in-order predecessor. | **FIXED `5f661f5`** - `DeferredDispatch.Run` is the one ending, and BOTH enqueues go through it (ERROR + `NotifyAbandoned` + ONE TASKABRT via the single emit point; never a second TASKSTRT). 8 new checks drive a THROWING continuation through the production runner with the real `TaskSequencer` and `TaskStatusPolicy`; FAIL-FIRST, with the runner reduced to the pre-fix ending, 6 of the 8 fail. |
| **D2** | **RUNBOOK sec 11 stated the rule Q5 REPLACED** - "falls back to WALL ... or has been flat for 60 wall seconds", and "the TASK CLOCK lines say each way, ONCE". Since `f2794d7` a flat clock with a back end present HOLDS and the line REPEATS. Sec 11 carried no Q5 bullet at all, so a demo operator was not told what a pause costs (nothing) or what the one symptom of a dead back end is. | **FIXED `fc23f14`** - four bullets: the HOLD and that all three task times freeze together; wall only when the READER is gone; the once-a-minute repeat and why; and the `BackendCount` LIMIT stated as the symptom, with the vendor citations and STP-809. The start-up `TASK CLOCK (R4):` line carried the same stale clause and is fixed too. |
| E1 | The hysteresis exit printed **"the simulation clock is readable and advancing again" on the way TO the wall clock**: the branch fires on `!taskSimStale`, which also goes false when `heldOnSim` does - i.e. when the reader is confirmed GONE. | **FIXED `2cbd722`** - the two exits are said apart, and 2 checks lock the reachability of the branch and the opposite meaning of the two (a HOLD, then losing the reader clears `taskSimStale` exactly as a recovery does while `TaskClockAction` turns to `FallBackToWall`). |
| E3 | **No cycle or self-reference guard**, and A1 made a cycle 144x more expensive: `predecessorInThisOrder` is TRUE, so phase 1 takes the backstop, and a cycle is the one dead end nothing ever abandons. | **FIXED `846de1d`** - SUPERVISOR DECISION, an EXTENSION of Q4 rather than an implementation of it. The graph is walked once at order receipt and every task on a loop gets Q4's shape (ERROR naming the loop, abandon, one TASKABRT) before any TASKSTRT. 8 checks incl. NO FALSE POSITIVE on the real 42-task graph; FAIL-FIRST measures the cost (0 dispatched, skipped only after 86,400 s). |
| E4 | Nothing compared an order's own longest chain lead against the backstop that truncates it. | **FIXED `c4f7785`** - `TaskDispatchPolicy.LongestChainLeadSeconds` / `LongestChainEndSeconds`; one `CHAIN DEPTH:` INFO line per order and a WARNING when the lead meets the backstop. The suite's private copy of that arithmetic is gone - `WalkChain`'s horizon calls the production function. |
| E5 | `Vrf:TaskPredecessorEndMarginSeconds = 0` makes the phase-2 window EQUAL the predecessor's scaled Duration, which races the <= 3 x (sim ratio) s observation lag - and the suite's label called zero safe. | **FIXED `9a0a928`** - DECISION: refuse, do not clamp. A non-positive value is an ERROR at start-up and the run proceeds at the shipped 60 s; the validated value is resolved into a field so the refused one cannot be read again. RUNBOOK sec 11 documents it. The label is now three checks that say which half is which. |
| E6 | The backstop's justification said COA-STP1's deepest chain is 26,400 s, in `TaskDispatchPolicy` and again in `VrfSettings`. | **FIXED `c4f7785`** - 16,800 s to the last DISPATCH, 21,600 s to the last END, in both places, and now MEASURED by the suite off the order on disk rather than asserted in a comment. |
| E7 | 7.1a said "(90 checks across SIX sections)" while 7.1b and the suite said 112. | **FIXED** (this table's commit) - both places say 136, the count the suite prints after pass 3. |
| **E2** | **Q5's wall-fallback branch is DEAD CODE.** `VrfFacade::SimTimeSeconds` returns -1 exactly when `backends().count() <= 0`, which is the same expression `VrfFacade::BackendCount` returns, and `SampleTaskClock` returns early unless the sample was readable - so `taskSimStale && !backEndPresent` is unreachable and the "NO VR-Forces back end is present" line never prints. The ruling is still honoured in OUTCOME (a genuinely removed back end makes the reader unreadable and the axis falls to wall through the hysteresis path, which E1 now labels correctly). | **RECORDED DEBT** - the fix is a facade change: `DtVrfRemoteController::backendsControlState()` (`vrfRemoteController.h:321-323`) returns Paused vs Running, the discriminator the predicate actually wants. **STP-809.** Live gate 11's probe is what says which of the three readings the vendor really gives. |
| N1 | Pass-2's C1: `TaskDispatchPolicy`'s M1 doc block and its `<param>` tags bind to `ScaleOrderMs`, not to the method they describe. | RECORDED DEBT |
| N2 | `_taskByUuid` is never pruned and spans ORDERS, so `predecessorInThisOrder` - and its log line "IS a task in this order" - really mean "in any order this process has seen". No unsafe case constructed; the words are wrong, and a cross-order reference gets the backstop rather than the configured window. (E3's cycle walk deliberately uses the SAME map, so what is checked is the graph that would take the backstop.) | RECORDED DEBT |
| N3 | The start-delay LOG reports `Math.Max(scaledStartMs, scaledRelativeMs)` while `TaskSequencer` uses sim-first-else-relative. Differs only for an order carrying BOTH forms; COA-STP1 does not. The suite's graph builder copies the log's rule, not the sequencer's. | RECORDED DEBT |
| N4 | The `(e2)` check drives a READABLE frozen reader with `backEndPresent: false` - a state E2 shows the facade cannot produce. Sound as a unit test of the policy; not evidence that Q5's "gone" half is exercised. | RECORDED DEBT |
| N5 | `WalkTaskClock` and `WalkChain` re-implement the service glue rather than driving it. Verified faithful argument by argument as of `8db033e`, but a future drift in the service would not fail a check. (Pass 3 removed ONE of these copies - the chain-lead arithmetic, E4 - and D1's runner was extracted precisely so the new checks drive production code.) | RECORDED DEBT |
| N6 | `IssueEngage` enqueues a bare lambda too. A throw leaves the parked engage unissued and unreported - but the task was already `MarkDispatched`'d as the move, so its armed end time still closes it. Same family as D1, far lower stakes. | RECORDED DEBT |
| N7-N9 | Record only: the pass-2 debt below is unchanged (N7); ASCII/CRLF clean across every changed file with a dirty control proving the instrument (N8); and `-t:Rebuild` is required - the incremental build's "up-to-date / 0 warnings" is a no-op result, the real number is 6 pre-existing warnings (N9). | RECORDED |

**RECORDED DEBT CARRIED FORWARD, unchanged by pass 3:** pass-2's **B2** (a superseded move's
late vendor completion attributed to the NEW task) and **B5** (an absolute `StartTime` served as
a scaled wall delta on the sim axis), and **C2-C15** - including C3 (the quantified staircase
error, still unstated in `VrfSettings`), C8 (the uncovered `SampleTaskClock` glue, = N5) and C12
(the area "centroid" is the vertex arithmetic mean, 149-1,130 m off on 12 multi-vertex areas
against a 500 m arrival radius). Pass-1's doctrine verb-vs-symbol mismatch is on the STP side
and is still open there.

**WHAT A LIVE RUN MUST PROVE** (none of it is offline-decidable):
1. A COA-STP1 run emits TASKSTRT for every dispatched task and exactly one TASKCMPLT per
   task at its end time, and `reports-captured.log` shows them (G6 captured 0 TaskStatus).
2. The STREND chain runs past its first link: T1 -> T2 -> ... dispatches on timed
   completions instead of dying at the predecessor gate. 42 dispatches, not 9. SINCE A1
   THE GRAPH ITSELF IS DECIDED OFFLINE - `--rulings-selftest` walks the real sequencer
   over the whole order and gets 42 / 0 at scale 1.0, under the Demo overlay and in 20
   repetitions at 0.05 - so what the live run adds is that VR-Forces, the bridge and the
   report path do not break what the graph already proves.
3. The air-defence chain T9-T12 dispatches - T9 in place with its NameObservation, T10-T12
   as successors - instead of collapsing on the refusal.
4. No ATTACK/BREACH/ESCRT task logs a self-target degradation; the ATTACK family routes to
   its geometry and the 42-task order reads as 42 executions.
5. The end-time clock behaves at scale: on the DEFAULT `Vrf:TaskClock=sim` the start-up
   TASK CLOCK line names the SIMULATION clock, the TIMED COMPLETION line agrees, and the
   completions land at Duration x `Vrf:DurationScale` of SIM time - the first live exercise
   of the sim-clock path (the watchdog has only ever been measured on the wall clock).
   With it, that the STREND gate SURVIVES the whole Duration: the M1/A1 line "gated on
   ..., which IS a task in this order and is armed to end ... s after ITS dispatch. It has
   ... s to DISPATCH ... and then ... s to COMPLETE" must appear for each of the 31 gated
   tasks, and none of them may report the skip TASKABRT.
6. A demo run with a compressed `Vrf:DurationScale` completes the whole order in minutes
   and T13 still dispatches after its (scaled) 3h20m delay, not before.
7. An order carrying a MapGraphicID (an STP re-export with IncludeMapGraphicIdInTasks=True,
   or a hand-edited fixture) logs "geometry from MapGraphicID <uuid> -> <name>" and drives
   the init area's centroid - and, since M5, a MapGraphicID naming one of the init's 41
   LINEs or 317 POINTs resolves too, with the init line reporting how many graphics are
   addressable ("R1 RESOLUTION (M5) is independent of those flags: N graphic(s) ...").
8. THE CLOCK HAZARDS ARE STILL UNOBSERVED. M3 and M4 are hardening against two reader
   behaviours the neighbouring watchdog code judged real (`vrfBackendListener.h:154-163`,
   `vrfRemoteController.h:605`) but which this project has NEVER seen live. A run must show
   whether `DtVrfRemoteController::simTime()` alternates, goes flat, or steps backwards at
   all: if it does, the TASK CLOCK / SIM CLOCK lines say so; if it never does, they stay
   silent and the fallback path remains unexercised.
9. Q1's default is UNREACHED on COA-STP1 (0 taskees with more than one ungated task under
   `PredecessorTimeoutPolicy=skip`): a supersede TASKABRT needs an order with concurrent
   tasks per taskee, or a run with `force`/`whenIdle`, before it is anything but a
   self-test.
10. **A1's gate lines appear, and no task reports the phase-1 skip.** Each of the 31 gated
    tasks logs "gated on ..., which IS a task in this order ... It has 86400 s to DISPATCH
    ... and then N s to COMPLETE", and the string "never dispatched within" appears
    NOWHERE in the run log. The graph itself is decided offline; what the run adds is that
    the live clock, the bridge and the report path do not break it.
11. **Q5's hold behaves on a real pause - and then, as a PROBE, what a killed back end does.**
    Two halves, and only the first is a pass/fail.
    - **PASS/FAIL.** Pause the scenario for more than 60 s: the "TASK CLOCK: ... a VR-Forces
      back end IS still present ... C2SIM task times are HELD" line must appear and REPEAT
      (once a wall minute), no task may complete during the pause, and every task must
      complete its full remaining Duration after the resume.
    - **PROBE, THREE OUTCOMES, NO PREDICTION** (re-worded by the pass-3 review; the previous
      wording predicted the "NO VR-Forces back end is present" line, and E2 shows that branch
      CANNOT print: `VrfFacade::SimTimeSeconds` returns -1 exactly when
      `backends().count() <= 0`, which is the same expression `BackendCount` returns, and
      `SampleTaskClock` returns early unless the sample was readable - so by the time the
      back-end count is read, the reader has already proved the count is above zero). Kill a
      back end and RECORD which of these happens, because what
      `DtVrfRemoteController::simTime()` returns for an entry that is in the list but
      DEACTIVATED is unknown and all three are defensible:
      (a) **the last cached value** -> readable, flat -> `Stale` after 60 s -> the HOLD line,
          repeating, forever;
      (b) **0.0** -> a large BACKWARDS step, classified `RolledBack` rather than `Stale`,
          re-anchored at 0, then flat -> HOLD by a different route, with a misleading
          "SIM CLOCK: stepped BACKWARDS" WARNING first;
      (c) **a throw** -> the facade returns -1 -> unreadable -> after
          `ModeSwitchConfirmations` samples the axis falls to the WALL clock through the
          hysteresis path, and says so in the transition line E1 corrected.
      The gate records WHICH occurred and WHAT THE LOG SAID, verbatim. All three end in a
      defensible state; which one occurs decides what an operator sees, and nobody has seen
      it. Outcome (a) or (b) is the STP-809 case: task time frozen with one WARNING a minute
      as the only symptom, and the facade's `backendsControlState()` is the fix.
