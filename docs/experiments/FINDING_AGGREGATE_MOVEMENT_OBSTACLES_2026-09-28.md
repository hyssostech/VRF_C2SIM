# FINDING: aggregate units, buildings and steep terrain on VR-Forces 5.2 - what stops them, what the vendor provides (2026-09-28)

Lane RS1 (research, read-only; session 5fc25950) on main c4536eb. Tier HEAVY (a cause claim). Nothing here is a ruling;
nothing was launched (no VR-Forces run, no probe, no write under C:\MAK). [V] = read in the cited source or measured from
the cited file; [A] = inferred. UG52 = VR-Forces 5.2 Users Guide (docs/vendor/mak-5.2/txt, page labels). AC25 = "Adding
Content to MAK ONE Applications Reference Manual", MAK ONE 2025 rev MAK-25.0-1-251009, the manual UG52 27.1.4 points to for
aggregate movement restrictions (public: https://docs.mak.com/support/MAK_ONE_2025_Adding_Content.pdf, sha256 3b556c51
8356a1d26f6a5e2bb5d2b39a6d6de78d396c324302c5477bb0ceacd4; not yet in docs/vendor). RN52 = 5.2 Release Notes. Vendor paths:
SMS = C:\MAK\vrforces5.2d\data\simulationModelSets (AggregateTacticalLevel includes AggregateLevelBase, which includes base,
AggregateLevelBase.sms :81); TC = C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration; FC = C:\MAK\vrforces5.2d\
appData\settings\featureconfig.txt; AMA.h = C:\MAK\vrforces5.2d\include\vrfmodel\aggregatedMovementActuator.h (the SDK's
public header); "sysdef" = SMS\AggregateLevelBase\vrfSim\systems\movement\mech-aggregated-movement.sysdef unless named.
Instruments: scratch H:\claude\F--Repos-C2SIM-c2simVRFinterfacev2-36\5fc25950-1a10-4ade-9a7b-68cb5c1daf05\scratchpad\rs1
(alt_profile.py, alt_spikes.py, stop_building.py, leg2_offsets.py, task_inventory.py, rtk_routes.py, rtk_onroad.py,
rtk_tasktypes.py). Two corrections to the brief, from the coordinator, are applied: snapping routes to roads is off the
table (sec 6, the settled frame), and whether a PLANNING move exists at aggregate level is answered first.

## 0. The question and the run evidence

HEADLINE [V]: the aggregate model set HAS planning moves - for a container, Move (Group) and Group Navigate Route To Location;
for a unit, Move to Location (Plan Along Roads) over navigate-to-location, whose obstacle query covers the buildings and water the
sim loads. The container was driven with the literal executor (PA_Move_Along_Route), so, as at entity level, the remedy is
the TASK CHOICE (sec 2, sec 5). Their default path network is the road net - a conflict with the settled off-road frame that
is the owner's call (sec 2, sec 6).
Question (brief RS1): what stopped 48 IBCT's Mech COs at a building while its Strykers passed, what the vendor offers for
aggregate movement, how the vendor authors routes, and which remedy is vendor-consistent.
Run evidence [V] (PREREG_IRONSTORM_AGG_G1-2_2026-09-28.md Result N1, run 20260928T142731Z_run): on T14, 11 of 48 IBCT's 12
Mech COs stopped at one point (54.030655, 23.326944), 1,092 m short, inside an OSM building of the -2 hamlet, each relaying
once "Terrain too steep, slope: 408.844% max: 78.5398%" (vrfc2simapp.log L140151 first, SIM ~1655); the four Stryker HHTs and
the Stryker Cavalry SQDN drove through the same point; INF1RIF2 never left its far-shore slot; T02's member stopped at river
8011072; T10 arrived. The pre-flight flagged no T14 leg (L29191): its aggregate leg rule is water-only (L578).
New measurements [V]: ALTITUDES (trace, alt_profile.py) - the stopped Mech COs rest at 144.3-144.6 m on ~135 m ground
(137.7-138.0 m 51-58 m before the point, 132.7-133.4 m 24-34 m after); two Strykers were sampled 3.6-4.4 m past it at
146.4 m. ROOF RIDES (alt_spikes.py, all 59 traced objects) - exactly two fixes stand > 4 m above both neighbours inside a
footprint: those Strykers (+12.9 m), in the stop point's own footprint; G1's run (20260928T102541Z) shows none, and its one
moving member, CAV1 (Stryker Cavalry SQDN), drove the same leg past the hamlet with 0 "Terrain too steep" lines. THE BUILDING
(stop_building.py, lane E2's reader on its staged copy of the deployed tiles) - OSM way 857519447, building=yes, no height
tag, 14.4 x 15.7 m; T14's dispatched centreline (4.2 km) crosses exactly two footprints (it and the adjacent 857519777) and
passes within 10 m of no other.

## 1. What stops an aggregate with "Terrain too steep" (Q1)

THE COMPONENT [V]. The movement system's aggregated-movement-actuator (sysdef :97-136): "This actuator places the aggregate
on the ground ... The aggregate is treated as a point object when moving" (AMA.h :42-45); calculateOrientationAndPosition and
adjustToTerrain(heading, location, newNormal) each "Returns an error string" (AMA.h :121-135), shown once per condition
(AMA.h :318-347). The limit is the platform's (max-slope $max-slope) (SMS\AggregateLevelBase\vrfSim\platforms\
AggregateLevelAggregate.ope :386), filled from the entity file (SMS\AggregateTacticalLevel\vrfSim): 0.7853981633974 for Mech CO
(USA, M2) :438, Stryker  HHT (USA) :408 and Stryker Cavalry SQDN (USA) :492, all on that platform (:3). The console prints it
x 100 ("max: 78.5398%") beside 408.844%, which cannot be an angle, so [A] a rise/run ratio is compared with pi/4 read as a
ratio (~38 deg effective). The move is refused, not ended: no completion or failure reaches the task (G1-2 P12); the stall
watchdog reports it, as RL-20260913-03 and RL-20260914-01 provide.
BUILDINGS IN THIS TERRAIN [V]. The fixture's .scn loads MAK Earth Aggregate (online).mtf, whose .earth includes
buildings.worldwide.osm.online.xml (:50). That gives the SIM ENGINE (1) the OSM footprints as features, mak_vrf_layer Building
(:41) -> MAK_BUILDING (FC :410), inside MAK_OBSTACLE (FC :405, the planners' obstacle query) and in NO aggregate
terrain-mobility query (FC :259-272; URBAN is Municipal land use, FC :91), so a footprint has no speed factor; and (2) 3-D
GEOMETRY: "VRFSIM needs the buildings footprints features as well as the geometry from the <building>" (:11-12), from the sim
catalog (:55; TC\osgEarthCatalogs\default.buildings-catalog-vrfsim.xml :2-6 "simplified parametric buildings"; AC25 2.7.3
p102 "when the VR-Forces sim engine starts, it uses a different catalog ... simplified versions of the visual models"; RN52
p17 VRF-8229), 6-11 m high when OSM gives no height (TC\buildings.worldwide.osm.base.online.style.js :308-310, :388-391).
"Ground-based entities are placed at the highest possible terrain intersection ... If a terrain supports buildings with
multiple levels, the entity is placed at the highest level" (UG52 14.3.3 p386). A building is not extruded into the elevation
mesh and has no mobility factor: it is a sim model the ground clamp intersects - and the trace shows aggregates ON one.
CONCLUSION [V]. The Mech COs were stopped by the building MODEL meeting the actuator's slope check, not by the mobility table:
buildings are in no mobility query, and the vendor's feature stop ("Movement constrained by features") printed 3 times in
G1-2, never for a Mech CO. UG52 27.1.4 p531 (slope and terrain class alter MAXIMUM SPEED; "restricted areas like forests or
cities slow them down") and AC25 7.8 p234-236 (the mobility table) do not describe this interaction.
WHY MECH COs STOP AND STRYKERS PASS. [V] Same platform, actuator class, max-slope, and mobility features (MAK_MECH_* and
MAK_MOTOR_* resolve alike, FC :259-267). Different: the actuator tick - mech sysdef :100-101 min-tick-period -1 (every frame,
1/30 s at the fixture's fixed frame), motorized-aggregated-movement.sysdef :99-100 0.2 s +/- 0.025 (UG52 6.7 p206-207); speed
- ordered 8.33 vs 15.28 m/s, max 11.1 vs 26.9 m/s (entity files :439-440, :409-410, :493-494); movement-type (:134, :133). So
a Mech CO samples the terrain every ~0.1-0.3 m of travel, a Stryker every ~1.3-3.1 m. [A] Leading reading: the model's steep
facets are narrower than a Stryker's step, so its samples fall either side; the split is systematic (16 of 16 G1-2 members by
movement system; G1's CAV1 too), not luck. The sampling itself is undocumented. Tank actuators tick at 0.2 s (tank :101),
infantry every frame (infantry :99). Either way the Strykers "passed" by driving over a roof.

## 2. What the vendor offers for aggregate movement (Q2)

PLANNING MOVES - FIRST [V]. The PA ("Disaggregated Units") family that drives containers has no planned twin: PA_Move_Along_Route,
PA_Move_To_Location_Direct and PA_Move_To_Waypoint_Direct (+Retrograde), PA_Patrol_Route (SMS\AggregateLevelBase\scripts
*.xml); PA_Move_To_Location_Direct sends each member a straight move-to-location (.lua :34-50). The planned moves are elsewhere:
- CONTAINER, "Move (Group)" (SMS\AggregateTacticalLevel\scripts\group_movement_simplified, dir "Echelon Group"): parameters
  destination (location) or destinationPoint (object), useRoads (default ON, .xml :70-74). Each member is sent
  Move_To_Location_Plan_Path to its own offset destination - the group's current arrangement wheeled toward the destination and
  compressed to the echelon's extent (.lua :20, :28-77, :212-231); useRoads off = a straight move-to-location per member.
  Completion: a behaviour-engine role node finishes when every member's move has finished (letAllFinish default true) and
  succeeds if at least one did (successPolicy default "one"); the role is critical, so the command fails when that ROLE node
  fails - which it does only when NO member's move succeeded, or when there is no member at all
  (C:\MAK\vrforces5.2d\makLua\behaviorEngine\roleNode.lua :61-67, :113-137, :246-280; behaviorEngine.lua :35-38, :122-137;
  .lua :196-205). CORRECTED 2026-09-28 (lane G1-3, from the M3 lane's reading of roleNode.lua, AggregateMovePlanner.cs
  :218-237): this line first said "a member's failed plan fails the command" - one member's failure does not fail it.
- CONTAINER, "Group Navigate Route To Location" (group-navigate-route-to-location, same directory; parameter location): plans
  ONE path for the container with vrf:navigateThroughFeatures {start, destination, buffer 10 m, pathQuery MAK_ROAD} (no
  obstacleQuery passed), makes it a route, drives it with PA_Move_Along_Route and deletes it; done when that script ends
  (.lua :55-108).
- UNIT (a container's member), "Move to Location / Waypoint (Plan Along Roads)": "Move to the selected location by planning the
  best path. Used by aggregate level units." (SMS\AggregateLevelBase\scripts\Move_To_Location_Plan_Path.xml; UG52 35.5.11-12
  p735). It starts navigate-to-location with obstacleQuery = the movement system's MAK_OBSTACLE and pathQuery MAK_ROAD (.lua
  :20-45; sysdef :79-92). navigate-to-location (SMS\base\scripts): "Plan a path to the given destination and move there. The
  path is planned around the given non-traversable features ... The path maintains the given buffer distance from the features"
  (.xml); parameters destination, obstacleQuery, pathQuery (empty -> MAK_ROAD, .lua :60-63), buffer (0 when absent, :33-44),
  displayRoute; it drives the plan as routes with move-along (:237-262), ends true after the last, false with "Could not compute
  path" (:195-205), and re-plans if the destination object moves (:140-147). MAK_OBSTACLE = MAK_BUILDING, MAK_VEGETATION,
  MAK_INFRASTRUCTURE, MAK_WATERWAY and dynamic obstacles (FC :405-413): the OSM footprints and water the sim loads; forests are
  land use, not obstacles. The same planner serves "Move" with Use Roads (unit_movement_simplified.lua :74-80).
- ROAD TO KAUNAS uses them (rtk_tasktypes.py, plans / saved task states of its three scenarios): move_to_waypoint_plan_path
  3/23/10 / 25/26/26, move_to_location_plan_path -/2/4 / 11/18/15, navigate-to-location states -/8/5, group-attack-to-objective
  12/17/15, whose units reach an authored route > 500 m away with the planner and then drive it (unit-attack-to-objective.lua
  :54, :510-548; unit-trailing-march-to-objective.lua :263-300).
- UG52 is silent on what an aggregate Move To does with impassable cells, buildings or steep faces: 35.5.9 defers to 30.28
  p604 (entity-level: "If navigation data is present, simulation objects take it into account in planning"), and 27.1.4 p531
  says only that water "may stop a unit completely".
- THE INTERFACE CAN SWITCH: RunScriptedTask already sends simulationobject, checkbox, string, location and double variables
  (src\VrfFacade\VrfFacade.cpp :169-192) and already receives scripted completions (G1-2 P16 "pa_move_along_route
  (success=True)") - no native change [A: that "locationwithoutaltitude" / "locationreference" take the facade's "location"].
- THE FRAME CONFLICT: every one of these plans along ROADS by default (pathQuery MAK_ROAD; Use Roads ON). navigate-to-location
  also takes pathQuery "NONE" (FC :88 "NONE: FALSE -- Matches no features") with obstacleQuery MAK_OBSTACLE and a buffer - a
  plan round buildings and water on no road net: a documented parameter, behaviour unobserved.
OFF-ROAD MOVEMENT, THE REST OF Q2 [V]. Move Along Route (35.5.7 p734 -> 30.24 p599) drives vertex to vertex with no planning
(UG52 23.3 p505), the unit a point (AMA.h :42-45); "Aggregate-level scenarios do not support collision avoidance" (27.1.4). The
mobility table's road entry (speed-factor 1, sysdef :111-115) is a speed multiplier applied where a road happens to lie under
the unit's centre point, not a router. Halt_Movement_Before_Obstacles halts before ENGINEERING-OBJECT obstacles (AMA.h :195-224;
sysdef :133) and asks the GUI user (Halt_Movement_Before_Obstacles.lua :4-26, :248-250); it ignores terrain, buildings and
water, is off on all 90 aggregate actuators Road to Kaunas saves, and relayed 0 lines in G1-2. There is no feature-avoidance
setting for aggregates. FORMATION: PA_Move_Along_Route.lua sends every member the SAME route (:37-83), records each member's
offset at the start (:85-89) and applies it only after that member finishes, at route end + the offset rotated by (last-segment
bearing - initial heading) (:108-137) - no lateral offset en route (G1-2 O1: on the line to 0-1 m); the formation is a fan-in
and a fan-out. FOLLOW THE LEAD: Follow Entity exists for aggregates (SMS\AggregateLevelBase\scripts\aggregate-follow.xml :18;
sysdef :48-58) and heads for a moving station with no path planning (UG52 23.4 p505); the trailing march keeps followers on the
same route and regulates speed (unit-trailing-march-to-objective.lua :1-7). Neither changes the ground a member crosses.

## 3. How the vendor authors aggregate routes (Q3)

[V] Road to Kaunas (C:\MAK\vrforces5.2d\userData\scenarios\Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunas\
RoadToKaunas.scnx; its .scn loads MAK Earth Aggregate (online), AggregateTacticalLevel.sms): 75 routes - 18 two-vertex air routes,
57 ground routes (7,099 vertices, median per-route spacing 52 m) named for the roads and village-to-village segments they trace ("RTE 4717 S 1", "RTE 1826
S 1"). The six ground routes its maneuver tasks name (group-attack-to-objective x5, pa_move_along_route x1; 443 vertices),
against OpenStreetMap (overpass-api.de 2026-09-28; 1,400 highway and 4,016 building ways in 54.770-54.892 N, 24.680-24.820 E -
today's OSM, not necessarily the vendor's snapshot): 100 percent of vertices within 10 m of a road (median 0.0-0.2 m), 0
segments crossing a footprint, 0 vertices in one, though two run through villages (60 and 83 buildings within 50 m, nearest
6.0 and 9.7 m). Within the settled off-road frame (sec 6) what transfers is the CLEARANCE - no vendor route crosses a footprint
- not the roads. No avoidance setting and no scenario-level mobility override exist: the mobility table is SMS data (AC25 7.8)
changeable only in a derived SMS (UG52 68.3 p1309-1314); a terrain can hand the sim different layers (AC25 2.19 p150-151), but
the vendor's aggregate terrain gives VRFSIM the building geometry on purpose (buildings.worldwide.osm.online.xml :11-12).

## 4. Which remedy is vendor-consistent for our case (Q4)

(c) A VENDOR TASK - YES, AND IT COMES FIRST. The planning moves of sec 2 are the aggregate counterpart of the entity-level lesson
(Move To plans; Move Along Route executes literally). Per STP vertex, as M1 does for lone platforms: Move (Group) keeps the
formation and lets each member plan round MAK_OBSTACLE; navigate-to-location per member adds a buffer and the NONE path query.
Costs: plans follow roads unless pathQuery NONE is used; Move_To_Location_Plan_Path passes no buffer, so a plan may hug a
footprint edge [A: an edge may still meet the model]; no aggregate plan has run here (R11's vacuous plan-and-move was 5.0.2,
FINDING_GROUND_MOVEMENT_PRACTICE sec 4).
(a) THE PRE-FLIGHT - now the REPORT and the fallback, and its corridor is not the formation width: PA_Move_Along_Route puts every
member on one centreline (sec 2), so the rule is the centreline plus the existing 10 m clearance (Vrf:PreflightBuildingClearance
Meters), as water already is (ModelSetRules.cs :99-112), plus each member's fan-in and fan-out legs. A +/-490 m corridor would
flag any leg near a village: 56 footprints lie within 150 m of T14's leg 2 alone (leg2_offsets.py). On G1-2's order the centreline
rule flags one leg (857519447, 857519777); a line 125 m east or 175 m west clears the hamlet by 10 m (parallel lines only).
Touch points: ModelSetRules.FlagLeg, RouteShift.FeatureRefusal :512-526, the fans; the reader exists (OsmFeatures.cs
:319-321, :461-468; OsmQuery.LegBuildings :1408-1453, self-tested at OsmSelfTest.cs :426-443, never called by the service).
Premises to retire: ModelSetRules.cs :34-35, OsmFeatures.cs :886-887, FINDING_GROUND_MOVEMENT_PRACTICE secs 5.1 and 8.
(d) MEMBER TYPES OR MOVEMENT SYSTEMS - not consistent: a 0.2 s actuator or a larger max-slope lets Mech COs drive over roofs as
the Strykers did, edits vendor data in a derived SMS, and treats as a VR-Forces fault what is our choice of the literal task
(PLAN_MOVEMENT CLOSED list: a suspected VR-Forces bug means we misuse it).

## 5. Recommendation (the lane's; not acted on)

1. Task choice first: drive a tasked container with a vendor PLANNING move per STP vertex (Move (Group), or navigate-to-location per member) instead of PA_Move_Along_Route on STP's straight legs; arrival stays judged on telemetry.
2. Gate RULE before code: the vendor's aggregate planners plan along roads by default; the owner says whether the settled off-road frame (sec 6) governs aggregates - if yes, navigate-to-location per member with pathQuery NONE, obstacleQuery MAK_OBSTACLE, buffer 10 m; if no, Move (Group) with Use Roads.
3. Register the first run on T14's line (PREREG): the planned move vs G1-2's literal one; falsifier = a planned member stopped by a building ("Terrain too steep") or a "Could not compute path" failure.
4. Keep (a) as the pre-dispatch report and the fallback for any leg still driven literally: a footprint within 10 m of the centreline flags the leg and RouteShift detours it; fan-in/fan-out legs checked per member; retire the refuted premises named in sec 4.
5. Reject (d).

## 6. Documented vs inferred vs still unknown

SETTLED FRAME (not reopened): docs/VRF_5.2_DECISION_EVIDENCE.md L247-292 (Y-11: "cohesive off-road unit movement is the
default"; Y-13: 5.2d tracked and wheels-off-road movement systems ignore roads, "Prefer Roads" is what the vendor states for
civilians, and sending it is against the grain) and FINDING_EARLY_STOPS_2026-09-13.md L367-372 (Prefer Roads named only as one
way to run a confirming test). Snapping routes to roads is therefore not a candidate here. Dissent line: that evidence is entity-level (Maneuver
To, ground-tracked.sysdef); the aggregate model set's own planners default to roads (sec 2) - what would reopen it: the owner
admitting the aggregate planners with Use Roads; until then sec 5 item 2's NONE variant keeps the frame.
DOCUMENTED [V]: the actuator places a point and returns terrain errors; max-slope from the entity file; MAK Earth Aggregate
gives VRFSIM footprints (MAK_BUILDING, no mobility entry) AND simplified 3-D models, 6-11 m untagged; highest-intersection
placement; tick periods; the aggregate planning moves, their parameters and completion; Move Along Route is literal; members
share the route and take offsets only at its end; halt-before-obstacles is engineering objects plus a GUI question.
MEASURED [V]: stop point inside OSM 857519447; Mech COs at 144.3-144.6 m and Strykers at 146.4 m over ~135 m ground; the only
roof rides are those two Strykers; motorized 6 of 6 passes (G1 CAV1 and G1-2's five), mech 11 of 11 refusals; T14's
centreline crosses 2 footprints, T10's none; Road to Kaunas maneuver routes on OSM roads with 0 footprint crossings, and its
aggregates tasked with the planners.
INFERRED [A]: max-slope read as a ratio; the mech/motorized split from the sampling interval (tick x speed); that the
facade's location type satisfies the planners' location parameters.
STILL UNKNOWN (each settles by a registered run, not more reading): whether navigate-to-location with pathQuery NONE plans off
road at aggregate level on 5.2; how Move (Group) completes live and whether 0-buffer plans clip footprint edges; the default
obstacle set of vrf:navigateThroughFeatures when none is passed; the actuator's sampling geometry; the vendor's streamed OSM
snapshot vs the cached tiles.
NOTE 2026-09-28 (lane G1-3): the sim's ROAD layer - what a pathQuery MAK_ROAD plans along - is built from the osm-highways
set (osm.roads.model.xml :9-10, data:vehicle-roads-linear from <features>data:osm-highways</features>, :55 mak_vrf_layer
Roads; osm.features.xml :35-36), NOT from the "osm" set the pre-flight caches beside osm-water; a road read from the cached
osm tiles is therefore not the sim's road network. M3 reads osm-highways (OsmFeatures.cs OsmSet.Highways :323-332).
Adversarial review: competing hypothesis 1 - the mobility table (a speed factor 0) stopped the Mech COs; falsified by FC
(buildings in no mobility query) and by the feature-stop line never printing for a Mech CO. 2 - steep elevation data;
falsified by the ~5 percent ground gradient and the units standing 9-11 m above it. 3 - luck in which member met the building;
against it, 16 of 16 members split by movement system and G1's motorized member passed too, but the sampling mechanism stays
[A]. 4 - "no planning move serves a container's route" (FINDING_GROUND_MOVEMENT_PRACTICE sec 8 set Plan Along Roads aside as "a
destination task, not a route task"); per-vertex use answers that as M1 did, and the Echelon Group tasks and Road to Kaunas's
use of the planners stand against it. Unexplained: how the Mech COs got onto the model at all (144.3 m) yet were
refused on it; the geometry behind 408.844 percent.
