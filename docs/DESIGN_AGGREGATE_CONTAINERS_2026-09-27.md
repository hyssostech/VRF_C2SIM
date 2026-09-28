# DESIGN - populated Aggregate Containers on the aggregate-level profile (C1, 2026-09-27)

Lane design/aggregate-containers; HEAVY; gate PLAN (the seat reviews this note, a code lane builds it). Offline, nothing
under C:\MAK written. Binds RL-20260927-01 (movement approach), -02 (hostile RUS; populate the containers, not proxies,
before G1), -03 (every unit an EMPTY container at init at its authored position; ONLY a tasked unit populated, in place,
when its order arrives). With data/unit-composition-52-aggregate.json (DRAFT) and tools/aggregate/composition_check.py,
whose `--tree --init-census --twins --vendor` (GATE PASS) and `--selftest` (PASS) reproduce the census, vendor, twin,
footprint, ring and composition numbers. [V] = read at the cited file:line/page; [A] = assumed. Code lines: main 53215e9.

STATUS 2026-09-27: BUILT OFFLINE on feat/aggregate-containers (from main 0fe0734; not merged) under RL-20260927-02,
RL-20260927-03 and RL-20260927-04 (D-1..D-8) - `--populate-selftest` (121 checks) and composition_check.py pass; NOT yet
run live, G1 is its first run (RUNBOOK sec 11i). Deviations from secs 6-8, each deliberate: (a) a container's TYPE is the
sec 3 rule applied in code (ContainerTypeRule), not rewritten type-map rows - the rows are lane C2's
(feat/aggregate-authored-units); the row id stays the composition key and typemap_check.py is untouched. (b)
PublishedSubordinateCount (sec 8 item 4) is written in VrfFacade/VrfBridge but NOT COMPILED here (no C++ toolset on this
machine); the app binds it by reflection and the aggregate model set REFUSES TO START without it, so G1 needs a native
/t:Rebuild of the bridge and all eleven consumers. (c) Sec 8 item 3's "gate opens ... at CompositionTimeoutSeconds with a
WARN" is replaced by a bound (Vrf:ContainerPopulateTimeoutSeconds, derived 85 s) whose expiry REFUSES the unit's moves
(TASKABRT); members not created by its last CompositionTimeoutSeconds are left out by name. (d) D-6 measures against
Vrf:VertexArrivalRadiusMeters (100 m), the existing last-vertex bar (sec 10 said ArrivalRadiusMeters). (e) The script ids
are sent in their XML case (PA_Move_Along_Route), which the vendor's own Lua also sends (group-attack-to-objective.lua:533;
UG52 36.2.1 p753: ids are lower-cased internally). (f) No Vrf:AggregateNesting: FLAT only (D-8); Nested stays a follow-up.
(g) No runner Stage 0 pairing of Vrf:CompositionFile; the app validates and logs every row at start-up instead.

STATUS 2026-09-28: C1 merged (5561d90) and the bridge rebuilt with PublishedSubordinateCount (pin 5198ac45, 2094c56) -
deviation (b) is closed. G1 ran (runs/20260928T102541Z_run; merge bee3753): the container mechanism worked live on ONE
member (48 IBCT's CAV1: published, PA_Move_Along_Route, TASKCMPLT), and 22 of the 23 members were lost to their NAMES.
VR-Forces returns an aggregate name that overflows its 31-character marking field (DtMaxAggregateMarkingLength, VR-Link
vlpi/netStructs.h:51) as its first 30 (107 of 107 cut names over 132 runs; one that fits comes back whole), so sec 8
item 2's "unique names within 34 chars" was the wrong width: siblings and their own container shared one 30-character
name. C1c (fix/container-member-names,
RL-20260927-03): every member and synthesized sub-unit name is at most 30 (VrfNames.ChildName), every requested name is
unique within 30 before the request (NameRegistry.KeyConflict, a deterministic ~k tag on collision), and routes,
waypoints and graphics register whole; `--populate-selftest` p14 replays G1's three populations - 23 of 23 bound, where
the 34-character names reproduce G1's 1 of 23. Offline only; RUNBOOK sec 11i.

## 1. The five questions

(1) ONLY A CONTAINER HOLDS SUBORDINATES. UG52 72.2.1 p1419: "the aggregate-level simulation objects that are configured
to use the aggregate warfare model do not have the configuration options for specifying subordinates. Therefore,
AggregateLevelBase.sms has a platform, Aggregate Container, for use when aggregating simulation objects." [V] The switch is
AggregateLevelAggregate.ope:19 `(can-have-subordinates False)`; PseudoAggregate.ope sets none, default True
(vrfobjparam/aggregateObjectParameters.h:110-114); vrfobjcore/vrfObjectStateRepository.inl:148-156 adds a subordinate only
`if (... canHaveSubordinates())`, dropping it SILENTLY otherwise [V; A that addToOrganization takes this path]. Vendor data:
0 of 335 ground warfare-model units in the 9 sample scenarios has a child - only the 83 containers do [V --vendor].
CORRECTION: Road to Kaunas has 51 of its 81 units inside its 17 containers, 30 at force level - not "81 inside".

(2) A CONTAINER RUNS ITS OWN SCRIPTED TASKS, WHICH TASK ITS SUBORDINATES. PseudoAggregate.ope has no movement system (:40)
or move controller; :115-119 enables PA_Move_Along_Route, PA_Move_To_Location_Direct, PA_Patrol_Route ... [V]. The vendor's
plans give containers only scripted tasks (pa_move_along_route 6, group-attack-to-objective 41, 3 others) plus the built-in
wait-duration its wait controller serves (:122-131); never move-along or move-to [V --vendor]. PA_Move_Along_Route.lua:
init() snapshots the IMMEDIATE subordinates (:37), sends each warfare-model unit the built-in "move-along" on the SAME route
(:75-78) and each disaggregated sub-container "pa_move_along_route" (:65-70), recording each offset (:88-89); every 0.5 s
(:33) a subordinate whose move ended is sent "move-to-location-retrograde-task" to route end + its offset rotated by
(final-leg bearing - initial heading) (:108-137); the container ends (:152-155) when every SNAPSHOT subordinate did both.
One attached after init() is never tasked, one that disappears drops out (:96-106), an empty container ends at once [V].
PA_Move_To_Location_Direct.lua is the same with one direct move (:24-53, :58-82) [V]. MG 2.4.1 Table 1 p19-20 ("works for
both lower-level and higher-level units") is the ENTITY-level ground-vehicle table (MG sec 2.4) - PLAN C1 cites it for the
wrong object. POSITION: the aggregate-actuator (:133-144, min tick 3.0 s) sets "location, bounding volume, velocity, and
orientation" from the immediate subordinates (vrfmodel/disaggregatedActuator.h:9-26); every fully placed vendor container
sits at the UNWEIGHTED MEAN of its immediate subordinates to 0.0 m, 61 of 61 [V --vendor]. FOOTPRINT: none of its own
(bounding volume 1 m, :7-10); combat state is rolled up from the members (Update_Combat_Effectiveness.lua:284-303) [V].

(3) YES - CREATE FLAT, THEN ATTACH TO THE EXISTING CONTAINER. UG52 18.1 p438: "You cannot create units that are
subordinates of an existing unit. Once you create a unit, you can subordinate it to another unit."; 40.80 p902: "For
aggregate-level scenarios, the superior must be a higher echelon unit." [V] addToOrganization has no timing precondition
(vrfRemoteController.h:1334-1339); the container has organization and set controllers for it (PseudoAggregate.ope:73-92;
pseudoAggregatedSetController.h:55-56) [V]. The attach lands in the NEXT-FRAME subordinate manager
(vrfObjectStateRepository.inl:154) and the script snapshots at init(), so the task waits until the container PUBLISHES its
subordinates (simPseudoAggregateLocalEntityNetInterface.h:47-50) [V header; A prompt]. In place matters: "If you delete a
unit, all of its members are deleted" (UG52 18.5 p442) [V]; TODAY's MaterializeUnit (VrfC2SimService.cs:2522-2682)
resolves in the EntityLevel chain (:2385), where all 19 container types of the census and draft land the MIXED
"Ground_Aggregate" (not expanded, :2451-2458), so case 3 DELETES the container (:2664) and re-creates it [V --twins, code].
Order-time attach into an existing shell is proven at entity level only (PREREG_ORDER_TIME_MATERIALIZATION 3.2) [A here].

(4) AGGREGATED = NO SUBORDINATE OBJECTS; DISAGGREGATED = MADE OF OTHER OBJECTS (UG52 Table 68 p1470; the mode may change
in a run, vrfAggregateStateRepository.h:120-127) [V]. Vendor: 335 of 335 ground warfare-model units saved Aggregated, 83 of
83 containers Disaggregated [V --vendor]. The scripts branch on it - PA_Move_Along_Route.lua:46/:65 (recurse vs
move-along), Maximum_Speed_Modifier.lua:223 (overlap counts only non-disaggregated neighbours),
Update_Combat_Effectiveness.lua:74/:254 - and a VR-Link viewer gives an Aggregated unit no children
(DtVrlinkAggregateElementManager.inl:168-172) [V]. The interface creates EVERY aggregate Disaggregated
(VrfC2SimService.cs:2148) [V], wrong for a warfare-model unit by the vendor's data; whether isDisaggregatedUnit() reads the
requested state or the platform is [A] - hence parity, not theory.

(5) ROAD TO KAUNAS PLACES BY HAND; THE CONTAINER IS THE CENTROID. "there is no formation movement; that exists only at the
entity level" (UG52 ch 28 p549); "Subordinate destinations are calculated based on relative position within the group at
the time of tasking" (28.3.3 p551) [V]. Of its 10 battalion containers one is deployed beyond 2 km and one holds a single
co-located company; the other 8 keep their members 26-1,533 m away (median 374 m over 27); multi-unit brigades/divisions
spread 2.8-40 km (deployed); 13 of 17 containers nest; an HQ, where there is one, is designator 1 [V --vendor, .oob]. No US
brigade; the one US Army battalion is Armor CAB Group (USA) - TF 2/5/CAV (2 Tank CO + 1 Mech CO), CMD 2/5/CAV (HQ,
mortar, medical, CSS platoons) - tasked with pa_move_along_route (reverseDirection False, startAtClosestVertex False); all 6
vendor pa_move_along_route tasks go to these two, whose members are all units [V .orb/.pln].

## 2. The frame
INIT one empty container per unit; ORDER populate the performer's IN PLACE (Aggregated units, attached, publication
confirmed, then tasked; nothing deleted); TASK its own PA_* script; JUDGE its centroid (D1). All behind
Vrf:ModelSet=AggregateTacticalLevel; the entity-level path does not move.

## 3. INIT (a) - 36 empty containers, hostile RUS

Rule (`--init-census`: all 36 created units land a container): type = (11, 1, nation, echelon category from SIDC position
12 [D3 E5 F6 G7 H8 I9 J10, else EchelonCode], branch subcategory from the function ID, specific/extra from the landing
container's matchType); the first candidate branch that lands a CONTAINER wins. A nation-typed generic container is the
vendor's practice (Wolf BDE 11:1:255:8:4:1:0 -> "BDE, Mech Infantry"; 1 AC RUS 11:1:260:10:0:0:0 -> "Corps, Army") [V].
E.g. 48 IBCT "BDE" (light infantry), 116 ABCT "BDE, Armor", 1-112 IN "BN, Light Infantry", MOI regiments "RGT Motorized
Rifle PA"; NEAREST branch where the catalogue has none: infantry divisions -> "DIV, Mech Infantry", aviation brigades ->
"BDE", 105th AT -> "BDE, Armor", the NOS tank units -> "BN, Armor". Created as today's AtOrder shells -
createAggregate(createSubordinates=false, Disaggregated) at the authored position (VrfC2SimService.cs:1649-1677, :2148);
the TO's declared children attach at init as today (ApplyHierarchyComposition :2197). An empty container keeps its created
position: the same aggregate-actuator (EntityLevel Aggregate.ope:234-259) held entity-level shells still for 274 POS rows
(PREREG_ORDER_TIME_MATERIALIZATION sec 0) [V entity level; A PseudoAggregate]. MAP: symbols come from the GUI's
.magx/.leaf definitions, and 92 templates, mostly these generic containers, have none naming their exact type
(AGGREGATE_CATALOGUE sec 4) - how the echelon renders is [A]; that a remote create ignores their gui-can-create False is
[A]. G1 checks both (P1).

## 4. ORDER TIME (b) - populate in place

4.1 SOURCES, in precedence. (1) The STP TO: declared C2SIM subordinates are ATTACHED (they exist as containers; never
re-created; identity and reports stay theirs) and each is populated by these rules - a container moves only through its
members. (2) The catalogue: a sub-container whose template configures subordinates (Armor CAB Group (USA), HHC PA (USA,
CAB), Stryker Cavalry SQDN PA (USA) ...) is composed from its .entity list ("CATALOGUE") by us - never
createSubordinates=true (sim-assigned names at one point, DESIGN_ORBAT C10). (3) The authored table, keyed like the type
map and looked up by CreationPlan.MapRowId (UnitTranslator.cs:25); its doctrine is FM 3-96 - the MAK ONE 2025 Model
Catalog lists echelon groups by name and type only (sec 5.4 p613-614), no compositions [V].
FINDING - (1) is EMPTY for cut A. The tasked units are COA task-force units (28ID__FRIENDLY_INFANTRY_DIVISION,
48_IBCT/28ID, 1-112_IN/28ID) with NO Superior and NO Subordinate; the 15 relations (III Corps -> 3 divisions + 1st (GE)
Abn Bde + HHB; 28ID/III_Corps -> 8 brigades/regiments + 1st Bn 112th IN; 278 ACR -> 4/278) sit on their TO TWINS,
separate units at other positions [V init]. The owner's premise "the TO providing the composition" (RL-20260927-02) does
not reach a cut-A taskee - D-7.
4.2 DEPTH: recurse until the catalogue simulates the echelon - USA: company (plus one BN-level unit, the Stryker cavalry
squadron; no US Army infantry unit exists at any echelon); RUS: battalion (26 land BN warfare-model units).
4.3 THE DRAFT (gate PASS, counts pinned by --selftest, provenance per entry; FM3-96 = FM 3-96, HQDA, January 2021):

| row (map rows) | serves | container | members: leaves / sub-containers | provenance |
|---|---|---|---|---|
| C-USA-DIV-UCI (F-UCI-I) | 28ID: T01 hold, T02 5.3 km | DIV, Mech Infantry (PROXY) | 1 HQ = Stryker HHT (PROXY): 1/0 | T02 "28ID HQ transitions"; FM3-96 p1-1 |
| C-USA-BDE-UCI (F-UCI-H) | 48 IBCT: T13, T14 (G1) | BDE | HQ; 3 x C-USA-BN-UCI; CAV Stryker Cav SQDN (PROXY): 17/3 | FM3-96 1-6 |
| C-USA-BN-UCI (F-UCI-F) | 1-112 IN: T10 2.6 km | BN, Light Infantry | HQ; 3 RIF + 1 WPN = Mech CO (USA, M2) (PROXY): 5/0 | FM3-96 1-7 |
| C-USA-BDE-UCA (F-UCATA-H, F-UCA-H) | 116 ABCT (T14 affected) | BDE, Armor | HQ; 2 Armor CAB Group CATALOGUE; C-USA-BN-UCIZ; CAV: 26/6 | FM3-96 1-39/1-41 |
| C-USA-BN-UCIZ (none) | ABCT mech-heavy CAB | BN, Mech Infantry | HHC PA (USA, CAB) CATALOGUE; 2 Mech CO; 1 Tank CO: 8/1 | FM3-96 1-41 |

Five tasks, four units (116 ABCT only named in T14); omissions with doctrine and reason in the file (D-1, D-2). No RUS row
is owed (no hostile unit is tasked); Road to Kaunas's 79 MRB / 275 MRR / 4 TD ORBATs are the source when one is.
4.4 FLAT FOR G1 (D-8). PA_Move_Along_Route.lua:133 sends the built-in "move-to-location-retrograde-task" to EVERY finished
subordinate, a sub-container included, and a container has no controller registered for it (PseudoAggregate.ope:42-131;
simTaskManager.h:123-135: a task goes to "any task controller that has registered to process" its type) - the outcome is
[A] and no vendor plan exercises it (Q5). So the planner FLATTENS for G1: every simulated leaf attaches directly to the
tasked container, the vendor-exercised shape (TF 2/5/CAV). The table keeps the nested ORBAT; Nested is one setting and a
registered follow-up. Nested also ends off the last vertex (48 IBCT ~134 m: sub-container members unpack round the route
end, not round their slot); Flat ends on it (mean offset 0).

## 5. PLACEMENT (c)

Members are born on ONE ring round the container's current point: equal bearings, centroid-preserving radius
r = s / (2 sin(pi/N)) (DeStacker.CentroidPreservingRadius/EqualBearingOffset, DeStacker.cs:480-500, --destack-selftest).
The container IS its members' centroid (Q2), so it stays where the map showed it. Spacing s = 2 x the largest member reach:
a unit's Travel-posture footprint radius (UG52 27.1.2-27.1.3; Mech CO (USA, M2).entity:230/:297 300 m x 0.3 = 90 m), a
sub-container's ring radius + its reach; a leaf above company echelon takes a slot but does not size the ring (the Stryker
squadron: 2,000 m x 0.3 = 600 m; overlap only lowers MAXIMUM speed, x0.9 here, UG52 27.1.4). --tree: 48 IBCT flat = 17 on
one ring, s 180 m, r 490 m, reach 1.09 km (nested: BN rings r 153 m, BDE ring r 414 m); the HQ takes slot 0 (designator
1). Each slot is tested with the M2 point test and moved by VertexNudgeSearch (Preflight/VertexNudge.cs:83-165) when wet or
on a building, reported like a moved vertex. Road to Kaunas's median 374 m is looser - one number if the owner prefers it.

## 6. TASKING, ARRIVAL, STALL, REPORTS (d)

TASK: routeGeo = [live position, STP points] as today (:4514-4644), so a container keeps 2+ points and FormFor (:4977)
sends it to CreateRoute; in the route-created callback a CONTAINER gets RunScriptedTask(uuid, "pa_move_along_route",
route=ObjectUuid, reverseDirection=false, startAtClosestVertex=false) - the vendor plan's spelling and values - instead of
MoveAlongRoute (:6025); one point gets "PA_Move_To_Location_Direct" (location, retrograde=false) for MoveToLocation
(:4984); a patrol "PA_Patrol_Route" (:6004). RunScriptedTask exists (VrfFacade.cpp:1191-1198; ObjectUuid ->
"simulationobject", Bool -> "checkbox", :169-192) with NO caller yet - G1 is its first live use [A]. A MOVE on a
memberless container is REFUSED (TASKABRT + text): it would end at once (Q2). Holds are unaffected.
POSITION SOURCE: the container's own centroid (D1 as merged). GetAggregateMembers keeps ENTITY members only
(VrfFacade.cpp:1036-1070) and aggregate units "do not simulate them individually" (AggregateLevelAggregate.ope:5), so
UnitPositionPolicy.SourceFor gives AggregateLeaf (:96-101) and TryReadUnitPositions reads the centroid (:7529-7581) - no
code change [V code; A no entity members, D1's premise]. For: it is the vendor's place for the unit (Q2), what R1 reports
(:1197), where the script ends it. Against: C15's majority rule becomes 1-of-1 (a straggler 4 km back shifts a 17-member
centroid ~235 m, inside 500 m) and the watchdog sees 1/N of one mover's speed. Member positions would change the ONE
sampler C15/C16 share - only if G1 shows >50% of members within 500 m of the last vertex while the centroid is not.
COMPLETION: once, for the container (SynthesizeUnitCompletion :8377, RL-20260921-09; C15's swallow takes the late vendor
report); each member's two completions (move-along, unpack) go to Debug via a member registry, not the "no C2SIM uuid
known" warning (:8390). REPORTS: the container only; the population is announced once (AnnounceSubstitution :2695).

## 7. Plumbing (e)

CHANGES: the aggregate type map's rows name the census CONTAINER types and typemap_check.py's "no EMPTY container" gate
inverts for it (the PROXY map stays a file for A/B; -TypeMapFile wins); new Vrf:CompositionFile (default
data/unit-composition-52-aggregate.json), validated at start-up on the aggregate model set and paired in the runner's
Stage 0 (RunnerLib.ps1 Test-ModelSetPairing); Vrf:AggregateNesting Flat|Nested (D-8); Vrf:CreationPolicy=AtOrder REQUIRED
there (RL-20260927-03; AtOrder needs ComposeHierarchy=true, :584) - the PLAN's G1 prerequisite "AtInit with
ComposeHierarchy=false" was the proxy arm's and is superseded; GetResolver rooted at the model set (:2385). UNCHANGED: the
entity-level path bit for bit; D1, C15, C16; the M2 pre-flight; the sequencer and RL-20260921-09; R1; NameRegistry;
DeStacker; the fixture IronStorm_Centre_52_Aggregate (deploy = the one sanctioned C:\MAK file); -ModelSet.

## 8. Code changes (f) and tests

1. UnitTranslator.cs:15-33 CreationPlan + UnitTypeMap carry the row role (IsContainer); EnqueueCreates (:2131-2153)
   creates a container Disaggregated and a warfare-model unit AGGREGATED on the aggregate model set (:2148: always Dis).
2. New pure ContainerPopulatePlan.cs: composition -> ordered creates (MakeChildName :3144; type, state, parent, ring
   slot), attach order, expected published counts; Flat/Nested; cycle/depth guards; CATALOGUE from SubordinateSpecs
   (ObjectTypeResolver.cs:23). --populate-selftest: the gate's pinned counts (48 IBCT 17/3), unique names within 34
   chars, ring centroid on the anchor (<0.01 m), states by role, NO delete in any plan.
3. MaterializeUnit (:2522), aggregate model set, first branch PopulateInPlace: declared children -> populate each; else
   (2) -> StartPlacementTerrainQuery -> creates -> PendingComposition with ParentVrfUuid pre-resolved (as :2606) ->
   AddToOrganization (FinishComposition :2308) -> the gate opens when the container publishes the attached count (4) or
   at CompositionTimeoutSeconds with a WARN. Cases 2/3 unreachable; no DeleteObject.
4. VrfFacade/VrfBridge PublishedSubordinateCount(uuid) = subAggregates + entities (beside VrfFacade.cpp:1073-1094).
5. Tasking (sec 6) at :4984/:6004/:6025 via a CreatedUnit IsContainer flag (:357). 6. OnOrder (:3929-3933): performer
   only (D-5). 7. OnVrfTaskCompleted (:8272): members at Debug; D-6's guard. 8. VrfSettings, appsettings, runner pairing.
TESTS: composition_check.py --selftest (13 DIRTY controls, 5 pinned expansions, ring/footprint/rank/twin ports, this
commit); --populate-selftest; AggregateLeafSelfTest + "a populated container publishes no entity members ->
AggregateLeaf"; the full suite and build (RUNBOOK sec 9) before merge.

## 9. G1 registration outline (g) - one populated brigade on T14

Build C1 + D1 + M2 (0849334); fixture deployed; OSM tiles warmed by an ONLINE run first (M2). Settings: ModelSet
AggregateTacticalLevel with sec 7's keys (Nesting Flat, AtOrder), StallDetection true, OpposingNation RUS,
ObjectConsoleNotifyLevel 4 on 48_IBCT (C11), no --pre-order-gate nav-area.
Order: cut A reduced to T13 -> T14 on T14's ORIGINAL line (E1's derivation without (i)). P1 init: 36 ObjectCreated, 36
containers, 0 warfare-model units, each within 5 m of its authored or de-stacked point; 48_IBCT drawn as a brigade
(MEDIUM). P2 at T13: 17 creates, all Aggregated; 48_IBCT publishes 17 subordinates before any dispatch; ZERO deletes. P3 at
T14, the M2 lines (PLAN sec 3): no "VERTEX MOVED" (PreflightReports.cs:462), "ROUTE PRE-FLIGHT - OSM WATER ON THE LINE"
(lake 197345448 from ~0.84 km, :487), "EXPECTED SLOW" (MEDIUM, :509), "NO CLEARED LINE within +/-" (RouteShift.cs:675), 3
ObservationReports. P4: 48_IBCT's console "Init of aggregate move along." (PA_Move_Along_Route.lua:28), then 17 "starting
move-along." (:80). P5, branch 2 (M2's reading): the members stop at the lake shore (MAK_MECH_IMPASSABLE_TERRAIN =
MAK_WATERWAY OR ALPINE, featureconfig.txt:262), the centroid rests within 150 m of 54.0268, 23.3172 (MEDIUM); then 2a
TASKABRT + stall report, or 2b a vendor completion short, reported only under D-6. P6: no TaskStatus for any member; one
terminal status for T14. MISS: a silent stop; no water within 150 m of the centroid; a member crossing the lake; the
container ending seconds after dispatch; a member sent pa_move_along_route; any deletion.

## 10. Decisions owed (owner)

D-1 Division depth: 28ID = its HQ only (draft) or its TO brigades attached (needs D-7; hydrates nine subtrees).
D-2 Brigade depth: MANEUVER (draft: HQ + 3 BN + cavalry = 17 units) or FULL TO&E (+ FA, BEB, BSB; US catalogue gaps).
D-3 Battalion HQ: one CO-level HQ unit (draft) or the vendor's HHC PA (USA, CAB) container (+4 objects per battalion).
D-4 US rifle company: Mech CO (USA, M2) PROXY (draft; tracked) or USMC Rifle CO PA (dismounted; platoon resolution).
D-5 AffectedEntity: populate the performer only (draft; RL-20260927-03 read literally) or also 116 ABCT (26 idle units).
D-6 Vacuous container completion (D1's open item): withhold TASKCMPLT, report "completed short", when the centroid is
    beyond Vrf:ArrivalRadiusMeters from the last vertex at a vendor completion - recommended before G1.
D-7 The TO/COA double export: TO twins display-only (recommended) or a COA unit linked to its twin by name; ask STP why
    the init carries each formation twice.
D-8 Flat (draft, G1) or nested sub-containers (the vendor's ORBAT shape; its unpack step is unexercised - 4.4).

## 11. Adversarial review

What could keep a populated container from moving or reporting, strongest first: (i) the scripted task never reaches it -
RunScriptedTask is unexercised live, the id's case ("pa_move_along_route" in plans and script, "PA_Move_Along_Route" in
its .xml) is [A]; P4 and the object console (C11) check it. (ii) The attach not in effect at init(): no member tasked,
the container ends at once - the publication gate (items 3-4). (iii) Members created Disaggregated (:2148) look like
containers to the script and get pa_move_along_route, which a warfare-model unit does not enable
(AggregateLevelAggregate.ope:195-197) - item 1, on 335/335. (iv) One member stuck at water holds the container forever:
only the watchdog (ON) or D-6 reports it; whether a blocked move-along ends or runs is G1's open question. (v) Unpack
targets in water at the destination never complete; smaller rings reduce it. (vi) A memberless container's task ending
at once reads as success under RL-20260921-09 - the MOVE refusal. (vii) The nested retrograde step (4.4) - Flat.
Assumed about the SDK: every [A] above. Competing designs, rejected: PROXY leaves (A1), superseded by RL-20260927-02,
whose condition "if this is real" is met (Q1); createSubordinates=true on templates with subordinates - sim-assigned
names, no placement (C10), three US Army templates at all. The G1 lines are registered expectations, not results.
