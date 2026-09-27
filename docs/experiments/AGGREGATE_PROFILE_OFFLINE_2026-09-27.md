# AGGREGATE-LEVEL PROFILE - the offline build, plan steps 1-3 (2026-09-27)

Lane: feat/aggregate-profile, STANDARD, offline only (no live run, no VR-Forces launch, nothing written under C:\MAK).
Why: the owner's Go on the decision brief moved Iron Storm to the aggregate-level profile behind the entity-level fix -
RL-20260927-01; the standing rule is Y-15 (docs/VRF_5.2_DECISION_EVIDENCE.md) and RL-20260920-02 ("A and the B sounds
right", B = this profile). Plan: docs/PLAN_AGGREGATE_LEVEL_PROFILE_2026-09-06.md (steps 1-3 here; step 4 is live).
Tools: tools/aggregate/survey_magx.py (catalogue), tools/aggregate/typemap_check.py (map gate). Pinned interpreter:
%LOCALAPPDATA%\Programs\Python\Python312\python.exe (tests/NavGate.Tests.ps1).

## 0. The result in one paragraph

The 5.2d aggregate catalogue holds 453 kind-11 templates, but NOT ONE warfare-model unit at BDE, DIV or CORPS for any
nation: those echelons exist only as Aggregate Containers, 19 of the 25 land ones EMPTY (generic, country wildcard). An
empty container cannot move - its Move Along Route is PA_Move_Along_Route.lua, which ends on its first tick when there
are no subordinates. So the aggregate type map names a warfare-model LEAF for every row (one simulated object that moves
by itself) and every Iron Storm unit is a PROXY, 40 of 40, exactly as on the entity-level map. The hostile side is NOT
thin: RUS is DIS 260 here with 131 ground templates (the plan's "RU 4" counted a label spelling); RUS vs BLR is left to the
owner. The fixture, the map and the runner switch are built and gated offline. Reading the code for G1 found two things
that decide it before it runs: the app's arrival evidence and stall watchdog SKIP an aggregate that publishes no members
(every leaf), so a lake-edge stop would be silent at the task level; and the app's catalogue resolver is rooted at
EntityLevel.sms, so order-time materialization classifies aggregate types against the wrong catalogue (sec 6).

## 1. Catalogue (step 2's survey) - docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.md (+ .csv, generated)

- Chain: AggregateTacticalLevel.sms -> AggregateLevelBase.sms -> base.sms. 453 kind-11 templates: UNIT 280, CONTAINER 115,
  AIR-UNIT 49, AIR-CONTAINER 7, base abstract 1, top-level 1. 401 .magx entries.
- Unit and container templates by nation (air excluded): RUS (260) 131 (107 warfare-model units), POL 66, USA 48 (33),
  generic (-1) 42 (all containers), LTU 34, BLR (246) 31 (24 units), DEU 13, CHN 12, GBR 7, others 1-3. Land warfare-model
  units stop at BN: RUS 26 at BN, BLR 8, POL 19, LTU 5, USA 2; none at RGT, BDE, DIV or CORPS for any nation.
- USA has only two ground BN-echelon units (Stryker Cavalry SQDN, ABM Site); its infantry, mech and armor units stop at CO.
- The .magx is a vrfGui element-definition map (UG52 Table 10 p172, --mergeUnitTypeMap), NOT the sim's template
  selector: 38 of its entries carry a type no template publishes and 15 of those land base-sim-aggregate (an EMPTY unit),
  e.g. 'Rifle CO USMC PA.magx' 11:1:225:5:3:1:75 and 'Reconnaissance BDET (RU).magx' 11:1:260:6:30:0:11. Every map row
  therefore takes the type its .entity PUBLISHES and is resolved by the vendor best-match rule; 'magx' is recorded
  when one names the same type.
- Road to Kaunas (the vendor's brigade scenario): 108 kind-11 objects - 81 warfare-model units saved Aggregated, 17
  containers saved Disaggregated, 10 air units. Its corps, divisions, brigades and one regiment are GENERIC containers
  given nation-specific types (1 AC RUS 11:1:260:10:0:0:0 -> 'Corps, Army'; 4 TD -> 'DIV, Armor'; Wolf BDE (LTU) ->
  'BDE, Mech Infantry') and POPULATED with battalions and companies. That is the vendor's brigade.

## 2. The type map - data/unit-type-map-52-aggregate.json (62 rows; gate: typemap_check.py, PASS)

Same schema and key logic as data/unit-type-map-52.json (keys b, c, d, e of UnitTypeMap.Lookup; isAggregate true; EXACT or
PROXY with proxyNote), plus modelSetKey / role / entityFile / magx, which the app ignores. Rules, each gated:
(1) every row lands, in the installed chain, the template it names, as a warfare-model UNIT - never the base abstract and
never an empty container; (2) no row names a type the app would EXPAND at order time (sec 6.2) - which is why armor uses
TANK PLT (US, M1A2), not Tank CO (USA, M1A2), and field artillery a 155 mm platoon, not a battery; (3) nation-correct
DIS country on every row; (4) all 40 Iron Storm units resolve for friendly USA and for EACH hostile nation.
The gate's own port of the key logic is proved first against TypeMapSelfTest.cs's answers on the entity map, and nine
DIRTY controls each fail their own gate (typemap_check.py --selftest).

Iron Storm coverage (40 units: BDE 19, DIV 7, RGT 5, BN 4, NOS 3, CORPS 2; 36 created - III Corps and the three units that
inherit only its missing coordinates are not). 0 EXACT, 40 PROXY for either hostile nation:

| row (key) | template (echelon it really is) | C2SIM units (* = not created) |
|---|---|---|
| F-UCI-H (b) | Mech CO (USA, M2) (CO) | 48 IBCT (G1's taskee), 48IBCT/28ID, 1st (GE) Airborne BDE * |
| F-UCI-I (b) | Mech CO (USA, M2) (CO) | 28ID (taskee T01/T02), 28ID/III Corps, 4ID, 4ID/III Corps, 29ID * |
| F-UCI-F (b) | Mech CO (USA, M2) (CO) | 1-112 IN (taskee T10), 1st Bn 112th IN |
| F-UCA-H / F-UCATA-H (b) | TANK PLT (US, M1A2) (PLT) | 116ABCT/28ID; 116 ABCT |
| F-UCAW-H (b) | Stryker Cavalry SQDN (USA) (BN) | 56 SBCT, 56SBCT/28ID |
| F-UCRVA-G / F-UCRVA-F (b) | Stryker Cavalry SQDN (USA) (BN) | 278 ACR, 278 ACR/28ID, TF SNAKE; 4/278 ACR, 4th Sqn 278 ACR |
| F-UCF-H (b) | FA PLT (USA, 155mm) (PLT) | 169 FAB, 169th FA BDE/28ID |
| F-UCE-H (b) | Mech CO (USA, M2) (CO) | 55 MEB, 55th MEB/28ID |
| F-US-H (b) | CSS CO (USA) (CO) | 113 SB, 113th Sustainment BDE/28ID |
| F-UCVR-H / F-UCVRA-H (b) | Stryker HHT (USA) (CO; aviation out of ground scope) | 11th CAB/28ID; 11 CAB |
| F-GEN-J / F-GEN-N (d) | Stryker Cavalry SQDN (USA) / Mech CO (USA, M2) | III Corps *; HHB III Corps * |
| H- / B-UCIM-G (b) | MR Battalion (RUS, BTR-82A) / BTG (BLR, Mech) (BN) | 15 MOI REG, 1 MOI REG |
| H- / B-UCIM-J (b) | MR Battalion (RUS, BTR-82A) / BTG (BLR, Mech) (BN) | 12th Corps |
| H- / B-UCIZ-I (b) | BTG (RUS, Mech) / BTG (BLR, Mech) (BN) | 52DTG, 33DTG |
| H- / B-UCAAA-H (b) | AT BN (RUS, MT-12, 9P148) (BN) / ATGM PLT (BLR, 9K111) (PLT) | 105th AT BDE |
| H- / B-UCATA-N (b) | BTG (RUS, T-90) / BTG (BLR, Tank) (BN) | 1 TK RGT, 364th TK BDE |
| H- / B-UCF-H (b) | SP Artillery BN (RUS, 2S19) / Howitzer BN (BLR, 2A65) (BN) | 99 ARTY BDE |
| H- / B-GS-H (b) | Reconnaissance BDET (RUS) (BN) / Recon PLT (BLR, BMP) (PLT) | 14th Commando BDE (SOF) |
| H- / B-UCSGM-H (b) | MR Battalion (RUS, BTR-80) / BTG (BLR, Mech) (BN) | 183 SEC BDE |

NO TEMPLATE AT ALL: CORPS, DIV and BDE have no warfare-model unit for any nation (RGT neither - regiments are containers).
The fallback is the largest movable warfare-model unit of the same branch family and nation (BN where the nation has one,
else CO or PLT), marked PROXY with the echelon it really is. The alternative - the generic empty containers the vendor
uses as ORBAT nodes - is EXACT in echelon but moves nothing; it becomes useful only when the interface can populate a
container (the "Armor CAB Group (USA)"-style populated containers are the natural second step after G1).

## 3. The hostile side - OWNER DECISION (not decided here)

| option | what it gives | cost |
|---|---|---|
| RUS (DIS 260) | a branch-correct BN warfare-model unit for every Iron Storm hostile function (motor rifle, mech BTG, T-90 BTG, AT BN, 2S19 BN, recon detachment for SOF) | none in coverage; RUS is DIS 260 here, not the entity map's 222 |
| BLR (DIS 246) | 31 templates: mech and tank BTGs, a towed howitzer BN | motorized, security and SOF fall to a mech BTG or a recon PLT; anti-armour to an ATGM PLT |
| a config choice | both are in the map; Vrf:OpposingNation selects (RUS or BLR) | the pre-existing default in appsettings.json is RUS, so RUS is what a run gets unless the owner says otherwise |

The vendor's own Baltic scenario uses RUS (260) for the whole opposing force. None of the hostile units is a taskee in the
Iron Storm order (all 23 tasks are friendly), so the choice changes what is displayed and what automated aggregate combat
could engage (UG52 27.1.5), not what moves.

## 4. The fixture - tools/FixtureGen/frame_variants/IronStorm_Centre_52_Aggregate.scnx (built, validated, NOT deployed)

```
python build_fixture.py --profile 5.2 --empty --frame-mode fixed-frame-run-to-complete --frame-time 0.033333 \
       --aoi 53.939723,54.119385,23.108483,23.414360,150 --out-name IronStorm_Centre_52_Aggregate \
       --scenario-name "Iron Storm (5.2, FFRTC, AggregateTacticalLevel on MAK Earth Aggregate (online))" \
       --terrain aggregate --sms aggregate
python validate_fixture.py --empty-52 frame_variants/IronStorm_Centre_52_Aggregate.scnx \
       --aoi 53.939723,54.119385,23.108483,23.414360,150 --terrain aggregate --sms aggregate    # ALL FIXTURES: OK
```
sha256 804e2c393dcf5f4fe4e1c58d7423a343c4e43250cd719d5474e035750c76a0e3 (rebuilds identically). The builder's new
--sms aggregate / --terrain aggregate write the vendor's macro strings, byte-identical to Road to Kaunas, and REFUSE the
aggregate SMS on any other terrain; the validator gates the pairing and 0 navData records on the terrain. The two
negative controls fail only their intended checks. An A/B against main's builder over four argument sets and a rebuild
of IronStorm_Centre_52_Nav_AG_maple (sha256 57465c35...0e32) are byte-identical, so no existing fixture moved.

Against Road to Kaunas's .scn: Terrain-Database, Gui-Terrain-Database and Simulation-Model-Set-Files are IDENTICAL, as are
time-multiplier, auto-reorganize, seed, run-duration, runtime schemes and version. Differences, each on purpose: frame
fixed-frame-run-to-complete 0.033333 vs variable-frame 0.1 (Y-9 determinism, same as every 5.2 fixture); exercise start
2025-10-02 10:00Z vs 2024-09-26 10:00Z (the GroundMovement donor's; daytime in both, UseDayNightModel 1 in both); the
playbox is the Iron Storm AOI vs Road to Kaunas's whole-globe extent (radius 10,709 km); and Road to Kaunas carries a
GUI DefaultObserverView and a description the headless fixture has no use for. Both globals of the donor resolve to the
same base.sms templates in the aggregate chain.

## 5. The switch - one key, Vrf:ModelSet = EntityLevel | AggregateTacticalLevel (default EntityLevel)

- Runner: -ModelSet (ValidateSet, default EntityLevel). It picks the type map (EntityLevel keeps data/unit-type-map-52.json,
  AggregateTacticalLevel -> the aggregate map, -TypeMapFile still wins), exports Vrf__ModelSet to the app on every 5.2
  run, prints the model set, the map's declared model set and the fixture's SMS in Stage 0, records them in the
  manifest (inputs.modelSet), and REFUSES at Stage 0: a map built for the other model set; a fixture that loads the
  other one (the default EntityLevel on an aggregate fixture included); AggregateTacticalLevel on 5.0.2; and, live only,
  an AggregateTacticalLevel fixture that cannot be read (warned in -DryRun). The fixture is read from the .scnx the
  launcher will load; a derived SMS is followed through its include lines. Logic: scripts/RunnerLib.ps1
  (Test-ModelSetPairing and three readers); 31 checks in tests/RunnerTurnaround.Tests.ps1 sec 8y.
- Wrapper: scripts/RunScenario.sh --model-set NAME (passed only when given). Pair AggregateTacticalLevel with
  --scenario IronStorm_Centre_52_Aggregate once that fixture is deployed.
- appsettings.json and appsettings.Demo.json carry ModelSet EntityLevel; the Demo _ModelSet text cites RL-20260927-01.
  The app itself does not read the key yet (the pre-flight lane uses the same name for its rule set): a hand-started
  interface that switches it must also set TypeMapFile.
- Also fixed on the way: the runner's -TypeMapFile existence check appended to $bad BEFORE Stage 0 created it, so a
  missing file died on an unset variable under StrictMode instead of being reported; it is inside Stage 0 now.

## 6. What the reading of the app says about a leaf (details: the G1 draft checklist, session scratch)

Line numbers are those of main b984945 (after the M1 merge; the aggregate dispatch path is unchanged by it:
VertexChainPolicy.FormFor sends every aggregate to CreateRoute + MoveAlongRoute, VrfC2SimService.cs:4957).
6.1 Arrival evidence (C15, MaybeCheckArrivals :6772) and the stall watchdog (C16, MaybeCheckStalls :7667) read positions
through TryReadMemberPositions (:7294), which returns FALSE for an aggregate with no members (:7306); both then skip the
unit (:6807, :7899). A warfare-model leaf has no members, so on main a leaf that stops at water gets no TASKABRT and no
arrival closure - its completion rests on the vendor's task-complete report alone. Position reports are unaffected
(they read the unit's own position, :1225).
6.2 Order-time materialization (MaterializeUnit :2502) resolves types with ObjectTypeResolver.LoadChain(home) (:2365),
whose root defaults to C2simEx -> EntityLevel (ObjectTypeResolver.cs:88-96). An aggregate type is classified by its
ENTITY-level twin: most leaves are case 3 (the shell is deleted and re-created, :2623-2651 - not free), and a type
whose twin is a pure higher unit would be EXPANDED into entity-level sub-unit types that land base-sim-aggregate here
(the map avoids those rows; typemap_check.py fails them).
6.3 Every aggregate is created Disaggregated (VrfC2SimService.cs:2127); the vendor saves its warfare-model units
Aggregated (sec 1). Effect unknown offline - the first suspect if a leaf neither moves nor fights.
6.4 The aggregate model never loads nav data: -PreOrderGate NavArea would wait for a row that cannot print.

## 7. Adversarial review

- What could still stop a brigade that this reading cannot see: the centre-point terrain rule meeting OSM water the
  checks do not read the same way (UG52 27.1.4 is one sentence; "Configuring Aggregate-Level Movement Restrictions" is a
  PDF-only manual not read); the fate of a move-along at speed factor 0 (runs forever, fails, or ends); the Disaggregated
  create (6.3); footprint overlap with co-located or composed units slowing a mover (27.1.4; the Mech CO footprint is
  300 m, 90 m in Travel); automated combat or posture change if a hostile unit comes into sensor and weapon range (the
  nearest is 44 km from G1's line); and the aggregate position update rate. None is settled offline.
- Assumed about the .magx: that it is a GUI element-definition map and not consulted when the SIM picks a template. The
  basis is where it lives (gui\visuals\Unit beside the .leaf visualizers), the documented option (a vrfGui switch), and
  38 entries naming types no template publishes; no vendor sentence says what a remote createAggregate consults. If the
  sim DID consult .magx names, the map's rows would still land (each row's type is the one its .entity publishes).
- Assumed about the catalogue: that the best-match rule ported from ObjectTypeResolver.cs is the sim's; it was validated
  6/6 on the entity chain before (UNIT_TYPE_MAPPING_FIDELITY sec 2.3) and is re-checked here only on self-resolution.
- The competing design, stated so it is not lost: generic containers for DIV/CORPS (the vendor's ORBAT nodes) with leaves
  below them. Rejected for now because Iron Storm's DIV row carries a mover (28ID, T01/T02) and an empty container
  cannot move; it becomes right once the interface can populate containers.

## 8. Next (in order)

1. Owner: RUS or BLR for the hostile side (sec 3). 2. Dispatch lane: a memberless aggregate must count as one position in
TryReadMemberPositions (6.1) - G1's stop is silent without it. 3. Pre-flight lane: the aggregate rule set on
Vrf:ModelSet (water = stop, read from OSM). 4. The sanctioned deploy of the fixture. 5. G1 (plan step 4) with the draft
checklist; prefer CreationPolicy AtInit + ComposeHierarchy false for the first arm (6.2).
