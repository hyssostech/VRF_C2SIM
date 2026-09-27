# FINDING: the vendor's expected practice for ground movement, and why the stalls persist (2026-09-27)

Written by the seat (Fable) after the owner asked, following two Iron Storm stops of the same vehicle and a
flip-flopping discussion: "See what the vendor's expected practice is, and how to best account for the repeated
snags that still persist despite all the effort applied." Tier HEAVY (a cause claim). Every statement below is
either [V] read in the cited source or [A] an inference from it. Nothing here is a ruling; sec 6 lists what the
owner decides. Sources: the vendor text under docs/vendor/mak-5.2/txt (UG = VR-Forces 5.2 Users Guide, page
labels; MG = Migration Guide), the installed SDK headers and Lua scripts under C:\MAK\vrforces5.2d, and the
vendor's shipped sample scenarios under C:\MAK\vrforces5.2d\userData\scenarios\Sample.

## 0. The claim in one paragraph

VR-Forces gives a ground vehicle three movement tasks. Move Along Route is the LITERAL EXECUTOR: it drives
straight at each vertex, plans nothing, and has no recovery - it is what the planner itself uses to execute a
segment it has already planned. Move To is the DESTINATION task: it plans (roads, nav mesh, around feature
obstacles), then follows its own planned route with Move Along Route subtasks, and recovers from blockages
(back up, skirt, one replan). A UNIT's Move Along Route plans a path to each vertex for every member. The
interface gives every LONE PLATFORM the literal executor on STP's straight lines, and 32 of the 36 Iron Storm
units are lone platforms because the entity-level catalogue has no template above battalion. Every stop we
have recorded is a lone platform on a straight line meeting something the avoider cannot get round (a lake
edge, a hamlet, a sand slope); the one unit that planned per vertex (T10) arrived through the same landscape.
The nav mesh, the corridor gate, route shift and the hand-edited waypoints all serve PLANNING - which the
literal task never does. The vendor's expected practice for a brigade/division scenario is the aggregate-level
model set (their own Baltic brigade scenario, Road to Kaunas, is built on it); for entity-level lone vehicles it
is a planning task per destination.

## 1. What the vendor documents [V]

- UG 23.1 p500: three key tasks. "Move To. Moves the vehicle to a destination location, causing the entity to
  first plan a route on roads and around obstacles as appropriate, then following that planned route using Move
  Along Route and road-movement tasks." "Move Along Route. Moves the vehicle from point to point along a route
  tactical graphic, using dynamic obstacle avoidance. ... nor does the entity plan paths before moving."
- UG 23.2 p500: Move To "attempt[s] to use roads, avoid obstacles, use navigation (nav) meshes, and recover
  from failures as much as possible"; "For off-road segments, the vehicle uses a Move Along Route subtask."
- UG 23.2.1 p501: planning = roads (if Prefer Roads) -> nav mesh (soil + slope costs) -> feature-obstacle
  planning (MAK_OBSTACLES in featureconfig.txt; "checks lines between the two locations, and then between
  locations and obstacle corners ... until a clear path is found").
- UG 23.2.2 p502 (recovery, Move To only): "if the vehicle becomes blocked by obstacles, it backs up a short
  distance and attempts to drive around the obstacle. If backing up fails several times, the script makes one
  attempt to restart with planning from its current location." Vendor script ground-vehicle-move-to.lua:
  MAX_REPLANS = 3 (:47), VEH_STOPPED_TIME_ALLOWED = 10 s (:69), reads the subtask status BlockedByWall /
  BlockedByVehicle (:236-246) - a status the remote controller never sees (DtMoveAlongTaskState carries only
  the current vertex, moveAlongTaskState.h).
- UG 23.2.3 p502 (the avoider, both tasks): "choosing a direction to travel at every instant, but is not
  planning a path through the obstacles it sees ... the vehicle could become trapped".
- UG 23.3 p505: "The Move Along Route task takes a route object and performs a sequence of movements directly
  to each vertex of the route. There is no path planning done as it moves toward the next vertex."
- UG 23.5.1 p507: soil acceleration-factor "0.0 means the surface's drag prevents the vehicle from moving at
  all (deep water)"; Table 26 p506: deepLake / deepRiver / ocean = deep-water. 23.5.2 p508: "Ground slope is
  not taken into account by the path planner that plans around feature obstacles" (only the nav mesh costs
  slope).
- UG 30.22 p598 (units): "Each subordinate computes an offset route and then traverses it by planning a path
  to each vertex in sequence at its own pace." UG 30.24 p600: a unit's Move Along Route runs Maneuver Along
  for ground-vehicle subordinates (ground-disaggregated-movement.sysdef).
- MG 2.4 p18-19: "Movement to a point is now performed by a Move To task"; "If there is no navigation mesh
  ... the Move To script accounts for feature obstacles and plans the entity's path to avoid them. The script
  also has some ability to dynamically replan paths around blockages"; "The move-along (route) task has been
  improved to better control the entity's speed and also uses dynamic obstacle avoidance." Table 1 p19: the
  old move-to-location / move-to-waypoint tasks all map to ground-vehicle-move-to; move-along stays move-along.
  moveToTask.h :181-184: "move-to is deprecated for ground vehicles. When a move-to is issued, a
  ground-vehicle-move-to is started as a subtask, which does more intelligent path planning."
- The vendor's own per-vertex example: data\simulationModelSets\EntityLevel\scripts\move_along_route_and_
  continue.lua ("an example of a replacement for the move-along task"; a system script listed under Examples)
  walks the route's vertices and starts a move-to-location subtask for each (:31-58) - i.e. a planned move per
  vertex. The 2026-09-07 withdrawal of "sequenced Move To per STP vertex" (PREREG_ASSEMBLY_LAYOUT :375-400)
  rested on "NO vendor sample tasks a unit per vertex"; that grep covered examples\, not the scripts folder.
- UG 13.7 p368: "Aggregate-level simulations focus on the high-level movement and interaction of large numbers
  of personnel and equipment in hierarchical units ... you are usually creating a company, a brigade, or
  another object representing an entire group"; the model set is fixed per scenario (EntityLevel.sms vs
  AggregateTacticalLevel.sms). UG 27.1.4 p531 (aggregate movement): speed modified by slope, terrain class,
  roads; "restricted areas like forests or cities slow them down"; "Rivers and water bodies may stop a unit
  completely, although bridges allow them to cross"; "the center point of the unit is used"; "Aggregate-level
  scenarios do not support collision avoidance among simulation objects."
- Vendor samples (task-id string counts over the .pln/.oob/.scn entries of every Sample .scnx): move-along 505,
  move-to-location-task 410, move-to 329, maneuver-along 6. Road to Kaunas (VR-TheWorld_Online\
  AggregateTacticalLevel\RoadToKaunas): AggregateTacticalLevel.sms on MAK Earth Aggregate (online); "Brigades of
  battalions in conflict with battalions of companies"; 110 aggregate objects (object-type kind 11), zero
  platforms; move-along 25 / move-to 16. The entity-level ground samples (GroundMovement, Wainwright) are
  platoon/company scale on EntityLevel.sms: 18 platforms + 5 units; 27 platforms + 227 lifeforms + 58 units.

## 2. What the interface does today [V]

- VrfC2SimService.cs :4994 / :5692: every mover gets CreateRoute + MoveAlongRoute (VrfFacade.cpp :1018 ->
  DtVrfRemoteController::moveAlongRoute, i.e. DtMoveAlongTask). A lone platform therefore runs the entity task
  (UG 23.3); a unit runs the unit task (UG 30.24 -> Maneuver Along, per-vertex planning). A single-point route
  already goes to MoveToLocation (:4924, the planning task) - so the facade can send Move To to a location.
- data/unit-type-map-52.json: 123 rows, 115 aggregate (PLT/COY/BN), 8 single-platform rows, all PROXY: the
  BDE-and-above fallbacks (M577A2_Command_Post, 1V14M, ...) and the catch-alls (M1A2, T-80, Type 99).
- Iron Storm init (data/STP-IRON-STORM-SYNTHETIC_Initialization.xml): echelons BDE 19, DIV 7, RGT 5, BN 4, NOS 3,
  CORPS 2. Run 20260927T021020Z: "4 unit(s) created as EMPTY shells ... 32 platform(s) created in full";
  "36 PROXY substitution(s) surfaced to C2SIM". T02 (28ID = one M1A2) and T14 (48 IBCT = one M577A2) are
  lone platforms; T10 (1-112 IN) is the one unit with members.

## 3. The recorded stops, read against sec 1 [V unless marked]

| stop | mover | task the sim ran | what it met | outcome |
|---|---|---|---|---|
| Mojave 1-35 leader (FINDING_EARLY_STOPS 09-13) | platform | entity Move Along Route | 0.70-0.95 rise/run on sand, no nav mesh | stopped; route shift cured it |
| Iron Storm -1 pre-warm, T14 | platform | entity Move Along Route | OSM lake edge, deep-water (FINDING_IRONSTORM_T14_STOP) | stopped 0.5 m from the polygon; silent |
| Iron Storm -2, T14 | platform | entity Move Along Route | hamlet: line through 2 OSM buildings, every forward arc blocked | stopped 3.2 m from a building; silent |
| Iron Storm -2, T02 | platform | entity Move Along Route | 5.3 km of open ground | arrived (nothing to trap it) |
| Iron Storm -2, T10 | unit, 6 members | unit Move Along Route -> per-vertex planning on the maple nav area | same AO, lakes and forest | arrived |

[A] Reading: the stops are one mechanism - the literal executor driving STP's straight line into a feature the
avoider cannot pass, with no recovery and no status the interface can read (sec 1, UG 23.2.3 + 23.3 + the Lua
status). The interface-side remedies each re-implement one slice of the vendor's planner for the one task that
does not use it: route shift (a lateral line, elevation + land cover only), the corridor gate (nav-mesh sectors,
which the entity task never consults), leg_check --osm-water / --osm-buildings and the hand-edited waypoints
(i) (j) (k) (obstacle avoidance by hand, one leg at a time). They cannot scale to a 23-task order across villages
and lakes, and they do nothing for the next feature class nobody has mapped yet.

## 4. Record touched (so nothing is reopened by accident)

- Y-10 (VRF_5.2_DECISION_EVIDENCE.md :205) is a seat recommendation from the 5.2 API assessment (moveAlongRoute
  unchanged; DtMoveToLocationTask deleted), not a ledger ruling; later docs cite it as "ruling Y-10". Its "hands
  route choice to the planner" objection does not survive sec 1: Move To per STP vertex keeps STP's vertices and
  order; the planner only chooses the path between two of them, as the unit task already does for every member.
- Y-15 (DECISION_EVIDENCE, ruled 2026-09-03): EntityLevel for company-and-below, AggregateTacticalLevel for
  battalion-and-above; PLAN_AGGREGATE_LEVEL_PROFILE_2026-09-06 (four steps; "Add to the plan as suggested").
  RL-20260920-02: Iron Storm representation "A and the B sounds right" (A = demo-scale cut on today's
  representation, B = the aggregate-level profile). Iron Storm at BDE/DIV/CORPS scale is squarely B.
- RL-20260913-03 (stall = report only, no automatic re-tasking) and RL-20260914-01 (TASKABRT) stand; the -2 run
  behaved as ruled. RL-20260920-01 item 3 (route shift ON for any run) stands; sec 5 leaves it on as the
  no-nav-mesh fallback. The 2026-09-07 withdrawal (sec 1, last bullet) was on evidence that has changed.
- R11 (UNIT_MOVEMENT_RESEARCH :394): DtPlanAndMoveToTask on an AGGREGATE at Mojave on 5.0.2, no nav data,
  completed vacuously. It is why arrival is scored on telemetry (C15), not on the sim's completion; it is not
  evidence about Move To for a lone platform on 5.2 with a nav mesh.

## 5. Recommendation (the seat's; not acted on)

1. Iron Storm on the aggregate-level profile, as already planned (Y-15, RL-20260920-02 "B"): the vendor's model
   set for brigades and divisions, with BDE (11) and Group (10) templates in the catalogue and Move Along Route
   at 35.5.7. Buildings and vehicles cannot trap an aggregate (27.1.4); water still stops one, so leg_check
   --osm-water stays as the pre-flight report and a route through a lake is an STP/CoaRenderer authoring defect
   to report, not something to hide. First gated run per the plan: one leaf unit on cut A's T14 line (the
   original, no (i)/(j)/(k)); prediction: arrives unless the line crosses water.
2. Entity-level profile (company-and-below COAs, Mojave): a lone platform gets the planning task per STP vertex -
   Move To to each vertex in sequence (facade MoveToLocation exists; the vendor's move_along_route_and_continue
   .lua is the pattern); units keep the unit Move Along Route (they already plan per vertex). Arrival stays on
   telemetry. Registered A/B on the original -2 T14 geometry: prediction, arrives on Move To per vertex.
   Owner's question (same day): is the vendor's model one Move To to the destination rather than segments? Yes for a
   POINT (MG 2.4 "Movement to a point is now performed by a Move To task"); each per-vertex segment is that same
   planner run with a nearer destination, so segments lose nothing of the planner - they keep STP's intermediate
   points (passage/release points), which one Move To discards. Neither form makes a BAD VERTEX safe: the script
   checks "Is destination in nav area?" (ground-vehicle-move-to.lua :1423), falls back to the feature planner, and
   on failure sets PathPlanFailure and aborts (:1401-1404) - a visible abort, not a silent stop; its only endpoint
   adjustment is to a road shoulder (:423-460), never out of water. So STP vertices must be checked BEFORE dispatch
   (water / building -> nudge to the nearest clear ground and report, as change (e) did by hand for T14's lake
   destination). For a two-point route (T02, T14 in cut A) per-vertex and single Move To coincide.
3. Keep: the nav mesh (the planner's slope/soil input, UG 23.2.1), the stall watchdog as ruled, route shift ON as
   the fallback where no nav area exists. Retire after 1-2 are live-proven: the hand-edited waypoints (i)(j)(k)
   and the corridor gate as a launch bar (it scores a mesh the entity task never used).

## 6. Adversarial review

- Strongest competing hypothesis: the stops are a DATA problem (water/buildings our checks did not see) fixable
  by a better pre-flight, with the task choice incidental. Against it: the vendor's planner already consumes the
  same feature classes (MAK_OBSTACLES) and adds recovery; the literal task gets neither however good the
  pre-flight is; T10 crossed the same AO by planning per vertex; UG 23.2.3 says the avoider alone "could become
  trapped". What would falsify sec 0: a lone platform on Move To per vertex, nav mesh present, stalling on the
  -2 T14 line with no feature within ~10 m ahead; or an aggregate-level unit stalling short of water.
- Unexplained, kept open: T14's 32 deg left turn before the -1 stop; the -2 right-hand jog into the pocket; the
  three refused graceful closes (separate finding).
- Assumed: that Move To on 5.2 plans on the maple nav area for a lone M577A2 (documented, not yet observed
  here); that the OSM tiles the checks read are what the sim streamed; that an aggregate unit's centre-point
  soil rule treats a lake edge as the docs say (27.1.4 is one sentence; the Adding Content manual section
  "Configuring Aggregate-Level Movement Restrictions" was not read - it is a PDF only).

## 8. Addendum (owner's question, same day): can the route-shift machinery serve aggregates, and does it solve the lake?

What the AGGREGATE model does with terrain [V]: AggregateLevelBase\vrfSim\systems\movement\tank-aggregated-movement.sysdef
:112-131 (mech/motor/infantry alike) - terrain-mobility by feature query at the unit's centre point (UG 27.1.4):
MAK_TANK_UNRESTRICTED (MAK_ROAD) speed-factor 1; RESTRICTED_L1 (HILLS, CULTIVATED, DESERT) 0.65; RESTRICTED_L2 (FOREST,
URBAN, MOUNTAIN) 0.25; IMPASSABLE (MAK_WATERWAY OR ALPINE) 0. featureconfig.txt :254-272, :413-414: MAK_WATERWAY = Waterway
features OR OCEAN OR COAST OR RIVER OR LAKE. The aggregate terrain (MAK Earth Aggregate (online).earth) loads OSM oceans, OSM
water and OSM land-use features; VRFSIM.Aggregate.feature.model.xml maps them to the Lake / River / Forest (landuse forest,
orchard) / Municipal (residential, commercial, industrial) / Cultivated layers. So on the aggregate profile: a lake or river on
the line STOPS a brigade (factor 0); a hamlet or forest slows it to a quarter speed; buildings never trap it; the nav mesh plays
no part; Halt_Movement_Before_Obstacles is a reactive task that is OFF by default and asks the GUI user (headless: leave off).
The vendor's aggregate planning task is Move to Location (Plan Along Roads) (UG 35.5.11): AggregateLevelBase\scripts\
Move_To_Location_Plan_Path.lua starts navigate-to-location with pathQuery MAK_ROAD and obstacleQuery MAK_OBSTACLE (which
includes MAK_WATERWAY) - [A] so it plans on roads and round water; it is a destination task, not a route task.

What the route-shift machinery is [V]: RouteShift.cs inserts FOUR points per flagged leg (two on the authored line, two
offset), keeps STP's vertices in order, re-scores the shifted line, reports the detour to C2; offsets 25..600 m either side
(Vrf:PreflightRouteShiftMaxMeters 600), formation band +/-50 m; on "no cleared line" it dispatches the authored line and says
so. It runs for aggregates too (VrfC2SimService.cs :4678-4686; skipped only when an opt-in aggregate branch collapses the
route). Its FLAG rule is the entity-level slope ratio (LegScorer.cs :220: ratio >= 0.92 over a 40 m window); water flags a leg
only when it falls inside that worst window (:60-74). Its ground truth is elevation + the CLCplus 10 m raster (TileSource.cs
:49-58); it does not read the OSM water or building features the sim uses at either level.

Answer 1 - reusable for aggregates: yes in shape (insert vertices, keep STP's, report the detour), and the aggregate profile
is where such a pre-flight matters most, because the aggregate model neither avoids nor plans round anything on a route
task. But its flag rule must become the aggregate mobility table (water / river / alpine = stop; forest / urban / mountain =
0.25) read from the same OSM feature classes the sim reads, instead of the Mojave slope calibration. leg_check.py already
reads OSM water and buildings (opt-in, tested); the C# port is the missing piece the T14 lane proposed.
Answer 2 - the lake as things stand: NO. T14's line was dry on CLCplus and wet on OSM (FINDING_IRONSTORM_T14_STOP), so the
pre-flight would not flag it, insert nothing, and a brigade would stop at the same edge. Even flagged, the fix is a lateral
band: the detour that worked stood about 1.25 km off the line (waypoint (i)); a smaller westward shift may clear the lake for
an aggregate (buildings and nav sectors no longer matter there) - not measured. A RIVER across a leg cannot be shifted round at
all; it needs a bridge, i.e. a road crossing, which is what the vendor's road planner provides and a lateral shift never will.

## 7. Decisions owed to the owner

(a) Confirm sec 5.1 as the Iron Storm path now (it is the ruled "B", pulled forward). (b) Sec 5.2: allow Move To
per vertex for lone platforms at entity level (reverses the 2026-09-07 withdrawal on the new evidence). (c) Whether
Y-10's wording is amended to "entity-level MOVE mapping: unit -> Move Along Route; lone platform -> Move To per
vertex" once 5.2 is live-proven.
