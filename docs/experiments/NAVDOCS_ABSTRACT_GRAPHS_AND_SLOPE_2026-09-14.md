# NAVDOCS: abstract graphs and slope - the documentation-first pass (2026-09-14, verbatim)
Written by an Opus research executor after the user's drift call and BEFORE any ridge launch: five questions answered read-only from the Autodesk Navigation SDK help, the installed VR-Forces 5.2d tree and web search, with what the sources SAY kept apart from what it IMPLIES. It makes UNNECESSARY the gamewareMemorySize and gamewareQueryTimeBudget ladders, the hunt for a distance-or-sector ceiling, and the three runs section 6 answers from documentation alone; it reorders the work as N1 (propagation-box-extent 200 -> 2000, the flat query's corridor), then N2 (slope-avoidance-factor 1.0 -> 2.0, the cost model's no-go gate), keeping the abstract-graph ridge run as N3 to be spent only if N1 refuses.
CORRECTED THE SAME DAY, read this before acting on section 1.2 / 2.1 / 6: docs/experiments/PREREG_N1_N2_CORRIDOR_SLOPE_2026-09-14.md sec 1 shows that every navigation-parameters line this record quotes out of Ground_Vehicle.ope - use-abstract-graph, min-abstract-graph-replan-distance, propagation-box-extent, radius-factor - is DEAD for a LOCAL GROUND VEHICLE, because the shipped file binds both local-objects and remote-objects to nav-interface "dynamic-obstacle" and only DtActiveBotNavInterface builds a DtNavBot::NavigationConfiguration from an .ope; so sec 2.1's "MAK ships abstract graphs ON for the NavBot's own destination-following" does not hold for vehicles, and N1's lever is inert as this record proposes it. Sections 3 and 4 (slope, soil, cost, stall detection) are untouched by the correction.

NAVDOCS: ABSTRACT GRAPHS AND SLOPE
==================================
A documentation-first pass on the five questions the supervisor put, written
2026-09-14 by an Opus research executor. READ-ONLY: nothing was launched, no
build was run, no file under the repo or under C:\MAK was written; the only
writes are in this scratchpad. Sources are (a) the installed VR-Forces 5.2d
tree at C:\MAK\vrforces5.2d (docs, headers, appData, shipped data files),
(b) the live Autodesk Navigation SDK help, which is still online, and
(c) web search. Every quote below was read in this session at the file, line
or URL given in section 8.

Rule applied throughout: what the sources SAY is separated from what it
IMPLIES for us, and from what is still UNVERIFIED.


0. HEADLINE - the seven things the documentation already settles
---------------------------------------------------------------

H1. The flat A* query has a documented 200 m HARD CORRIDOR around the
    straight start-to-goal line, and nothing outside it is ever explored.
    "By default, the extent of the box ... is set to 200.0f, making the full
    width of the box 400 meters. Therefore, by default, no path will deviate
    more than 200 meters from the straight line between the start position
    and the destination." (Autodesk, AStarQuery Options.) VR-Forces exposes
    that value, at the vendor default, in DtNavBot::NavigationConfiguration
    (myPropagationBoxExtent(200.)) AND as a per-platform parameter
    propagation-box-extent 200.000000 in Ground_Vehicle.ope.
    G7b measured the abstract route's cross-track at 231-292 m - larger than
    the corridor the flat query is allowed to search. This is the leading
    DOCUMENTED mechanism for the G6/G7 "not enough (0) points" refusals, and
    it has never been tested: we varied memory and time budget, never the box.

H2. Abstract graphs are a genuine hierarchical layer, generated at NavData
    generation time, and they are documented as COST-BLIND and STATIC:
    "AbstractGraphs are not dynamic. They do not consider holes punched in
    the NavMesh during runtime, or traversibility of a NavTag and its cost."
    So they are a way to GET a long route, not a way to get a route that
    respects terrain cost. The refinement to a concrete path happens after
    them (query stage order ASTAR_PROCESSING_ABSTRACT_PATH ->
    ASTAR_PROCESSING_REFINING) and, in VR-Forces, again every
    min-abstract-graph-replan-distance = 300 m as the entity advances.

H3. The nav mesh IS slope-aware, by COST, not by prohibition - and with the
    shipped numbers the prohibition threshold is UNREACHABLE. MAK's own
    formula (navigationPreferenceDescriptor.h) is
      cost = inherent cost * slope-avoidance-factor * (slope/max-slope)^2 * 10
    with "if the cost of a path is 100.0 or higher, the triangle is
    considered no-go". With the shipped ground-tracked values that gate does
    not bite until slope >= 1.063 rise/run, and the mesh itself refuses to
    generate anything above slope-max 46 deg = 1.036. The no-go gate is
    therefore dead by construction for every triangle that exists.
    Our 55 m face (0.70-0.95) costs 43-80 - expensive, never forbidden.

H4. "max-slope x soil acceleration-factor" is NOT our analogy. It is MAK's,
    in writing: "max-slope is the max-slope entity parameter; for a vehicle,
    this is reduced by the soil modifier (i.e. multiplied by the
    acceleration-factor) to give an estimate of the reduction in max-slope
    caused by terrain drag." M1A2 max-slope 0.94 (entity file), sand
    acceleration-factor 0.80 (ground-tracked.sysdef) -> 0.752. The memory
    note that called 0.752 "an analogy" can be upgraded: it is the planner's
    own documented term, labelled by MAK as "an estimate".

H5. Soil is INVISIBLE to the ground-vehicle mesh as shipped. The
    ground-platform profile tags only two soil types with surface
    characteristics - road and pavedroad. The lifeform profile tags five,
    including sand. So on our area the sand under the face produced no nav
    tag at all, and the planner's "inherent cost" there is the descriptor's
    default-path-cost 5.0, identical to hard rock. Confirmed by the 5.2
    release note VRF-10203, which introduced soil-types-to-tag-with-surface-
    char precisely to REDUCE tagging.

H6. The feature-obstacle planner - the fallback the Lua takes whenever the
    mesh query fails - is documented as slope-blind: "Ground slope is not
    taken into account by the path planner that plans around feature
    obstacles." (UG52 23.5.2 p508.) That closes the causal chain of the
    early stops from documentation alone:
      long flat query refused (H1) -> Lua falls back to the feature planner
      -> feature planner ignores slope -> straight leg across a 0.86 mean
      face -> vehicle cannot accelerate above max-slope x soil (H4)
      -> nothing in VR-Forces tests progress (H7) -> frozen for the run.
    Every link is a vendor sentence, not an inference.

H7. VR-Forces does not detect a vehicle that stops while its task runs, and
    MAK knows it: the shipped remedy is a PLUGIN EXAMPLE. The Developer's
    Guide ships "Decide to Give Up (decideToGiveUpTask)" whose whole point is
    "installs a new version of the DtGroundMoveAlongControllerComponent to
    give up the task after a certain amount of sim time ... This example can
    be used as a starting point for other type of reasons to give up on a
    task." The two decideToGiveUpTask overrides that DO ship test other
    things (destination inside an obstruction; entity attached while the task
    target is not) - neither is a progress test. There is vendor precedent
    for a burndown timer, in the aggregate path only
    (DtDisaggregatedManeuverAlongController::isUnitMovementExhausted).


1. Q1 - WHAT AN ABSTRACT GRAPH IS
---------------------------------

1.1 What the sources say

Autodesk Navigation SDK help, "Creating AbstractGraphs" (verbatim):

  "AbstractGraphs are another type of NavData. They are an abstract
   lightweight static representation of possible paths in NavMeshes."

  "AbstractGraphs are not dynamic. They do not consider holes punched in the
   NavMesh during runtime, or traversibility of a NavTag and its cost."

  Generation: "set m_doGenerateAbstractGraph to true in
  m_abstractGraphParams, which is the GeneratorAbstractGraphParameters
  member." "GeneratorAbstractGraphParameters::m_extentsInNumberOfCells"
  controls "the cell box length each AbstractGraph covers; default value of 0
  creates one graph covering entire NavData".
  "GeneratorAbstractGraphParameters::m_workingMemorySizeLimit parameter gives
  the size limit (in Bytes) of the WorkingMemory."

  Runtime: "SetAbstractGraphTraversalMode()" with
  PATHFINDER_TRAVERSE_ABSTRACTGRAPHS or
  PATHFINDER_DO_NOT_TRAVERSE_ABSTRACTGRAPHS. On success, "m_abstractPath
  contains the AbstractPath, and m_path contains the first concrete path
  (from the starting point to the first AbstractGraph node)."

The stated PURPOSE is long-distance pathfinding with bounded cost: the page
frames AbstractGraphs as computing paths at start and end points rather than
across the whole terrain, conserving memory and CPU relative to a NavMesh-only
A*.

The query pipeline, from Kaim::AStarQueryResult (namespace reference), in
order: ASTAR_PROCESSING_TRAVERSAL, ..._TRAVERSAL_DONE,
ASTAR_PROCESSING_ABSTRACT_PATH, ..._REFINING_INIT, ..._REFINING_RESETCOST,
ASTAR_PROCESSING_REFINING, ..._PATHCLAMPING, ..._PATHBUILDING,
..._CHANNEL_INIT, ..._CHANNEL_COMPUTE, then the terminal values
ASTAR_DONE_PATH_FOUND, ASTAR_DONE_PATH_NOT_FOUND,
ASTAR_DONE_ERROR_LACK_OF_WORKING_MEMORY, ASTAR_DONE_START_OUTSIDE,
ASTAR_DONE_END_OUTSIDE, ASTAR_DONE_START_NAVTAG_FORBIDDEN,
ASTAR_DONE_END_NAVTAG_FORBIDDEN, ASTAR_DONE_NAVDATA_CHANGED and others.
So the abstract path is produced FIRST and then refined inside the same query;
and "path not found" and "out of working memory" are DISTINCT results.

Why a flat query fails on a large world - the documented reason is NOT search
size, it is the propagation box. AStarQuery Options, verbatim:

  "When you use an A* algorithm to find a path to a destination point that is
   not reachable from the starting point ... the algorithm will eventually
   explore the entire terrain ... To avoid this possibility, the propagation
   of the AStarQuery is constrained within a 2D box whose extents are
   measured on the horizontal (X,Y) plane, in meters, from the straight line
   between the starting position and the destination. ... The area outside of
   this 2D box is never explored. By default, the extent of the box ... is
   set to 200.0f, making the full width of the box 400 meters. Therefore, by
   default, no path will deviate more than 200 meters from the straight line
   between the start position and the destination. Depending on the layout of
   your game level, and the length of the paths you need to calculate, you
   may need to raise the default value in order to successfully complete very
   roundabout or circuitous path calculations with significant deviations
   from the straight-line path."

Class reference confirms the shape: "the propagation is limited to a 2d
oriented bounding box computed by inflating the segment going from startPos to
destPos by this value" (Kaim::BaseAStarQuery::m_propagationBoxExtent), and
there is a debug call whose documented purpose is exactly our symptom:
"DisplayPropagationBounds ... Fill the displayList with display info that may
help to understand a PathFinderFailure (mainly propagation bounds)".

The secondary reason the sectorizing page gives is memory and search size:
"You may need a large amount of runtime memory to hold all the data. The
memory size of the data grows quickly as the area it represents increases. As
your data set increases in size, the performance of the pathfinding framework
may suffer, as it may end up exploring enormous numbers of vertices and edges.
This is particularly true if you try to calculate a path to an unreachable
destination, because all triangles in the NavMesh will necessarily be
explored."

What VR-Forces adds on top (headers, read directly):

  navBot.h:41-67, DtNavBot::NavigationConfiguration defaults -
    myUseAbstractGraph(true)
    myMinAbstractGraphReplanDistance(100.)
    myPropagationBoxExtent(200.)
  with the comments:
    "Indicates whether the abstract graph should be used for planning.
     Abstract graph allows for longer paths, as long as abstract graph data
     has been generated."
    "Only relevant if using abstract graphs. Entity will replan when the
     distance to the current abstract graph node goes below this value."
    "Determines the size of the box used for path planning. Path planning is
     limited to the 2d box created by inflating the segment going from the
     start pos to the destination pos by the propagation box extent."

  navArea.h:398-402 - findPathToLocation(..., bool useAbstractGraphs = true,
  bool useChannels = false, double channelRadius = 4.0, ...). The C++ default
  is true, matching luaNavJob.h:321 (bool useAbstractGraphs = true).

  Our generation DID produce abstract data: navigationProfiles.mtl
  ground-platform has (generate-abstract-data True), and our own
  NavArea-ground-platform MojaveCOA.navGenConfig has (allow-abstract-data
  True), (raster-precision 0.200000), (cell-size 43), tile-count 108 x 82.
  MAK's own profile comment is the clearest plain-language statement of what
  an abstract graph does that exists in the shipped tree:
    "Indicates whether abstract data should be generated. Abstract data can
     be used to help perform long path planning. It allows an entity to
     determine a high level route, and then update that to a more local,
     detailed route as it progresses. Makes path planning long routes much
     faster, but can result in sub-optimal paths."

1.2 What it implies for us

- The measured 28-35 point plans at 5 km (137-177 m spacing, G7B sec 2.3) are
  abstract NODES plus the first refined concrete segment, not a degenerate
  answer. Refinement is real and happens twice: inside the query
  (ASTAR_PROCESSING_REFINING) and again every 300 m of travel
  (min-abstract-graph-replan-distance 300.0 in Ground_Vehicle.ope, above the
  100 m library default). The earlier note in NAVMESH_QUERY_DOCS that "no MAK
  document explains what an abstract graph IS" stands for MAK; it is now
  answered by the upstream vendor.
- The 117-133 m abstract spacing measured on a 1,877-1,997 m / 4-seam leg
  means several abstract nodes per ~500 m sector, so VR-Forces is NOT
  generating one AbstractGraph node per sector. (Inference from our
  measurement plus the sector geometry; MAK does not document the mapping
  from cell-size 43 to m_extentsInNumberOfCells.)
- The 200 m propagation box is the single documented fact that best fits the
  G6/G7 refusal pattern, because G7b MEASURED the abstract route leaving the
  chord by 231-292 m on exactly the legs the flat query refused. A route that
  must deviate more than 200 m is, by the vendor's own sentence, not merely
  slow to find - it is unreachable.
- Corollary that matters more than the flag: raising propagation-box-extent
  would give us a FINE mesh path (7-9 m spacing, per G7B's flat-query column)
  that still carries NavTag and slope costs, whereas the abstract flag gives
  a coarse path whose top layer is documented as cost-blind. If the box is
  the mechanism, it is the better fix.

1.3 Confidence

- What an AbstractGraph is, how it is generated, that it is static and
  cost-blind, that the path is refined after it: HIGH (primary vendor docs,
  quoted).
- That the 200 m propagation box is THE cause of our refusals: MEDIUM. It is
  the only documented mechanism that predicts a deviation-dependent refusal,
  and G7b's cross-track measurement lands on the right side of the threshold.
  It is NOT proven, and one row in our own record argues against a purely
  geometric explanation: run A and run D both planned M1A2 1's outbound
  5,013.7 m query with 718 points, and run B - which differs from A only by
  gamewareMemorySize 128 - refused the identical query. A pure geometry gate
  is deterministic per (start, goal) pair; that row is not. So either the box
  is necessary-but-not-sufficient (box admits the route, working memory or
  query slicing then decides), or something else is also in play. Do not
  write "the propagation box is the cause" anywhere until a run tests it.
- One doc-derived caution on gamewareMemorySize that explains B without
  inventing anything: vrfSim.mtl says "Multiple working memory blocks are
  allocated with this limit, so this is not an overall limit for GameWare."
  Raising the PER-BLOCK size is therefore not a monotone "more is better"
  knob - it can change how many blocks exist. That makes B's single
  regression unsurprising rather than anomalous, and it is a further reason
  not to spend runs on that parameter.


2. Q2 - WHY THE SHIPPED LUA PASSES useAbstractGraphs = false
------------------------------------------------------------

2.1 What the sources say

MAK's Lua API documentation (doc/luadoc/modules/vrf.html, findPathToLocation)
states the trade-off in one sentence, and this is the ONLY vendor rationale
that exists anywhere:

  "useAbstractGraphs (bool) - Uses abstract graphs during path finding. This
   can speed up path finding queries at the expense of finer detail in the
   path. It may also result in an inability to properly avoid the specified
   avoidLocation."

The Developer's Guide class reference says only "useAbstractGraphs, // setting
to true can speed up long path planning queries" and documents the default as
true (struct_dt_lua_nav_find_path_parameters.html: bool useAbstractGraphs =
true).

The shipped scripts, both call sites, read this session:
- EntityLevel/scripts/ground-vehicle-move-to.lua:488
    local params = {useAbstractGraphs = false, useChannels = true,
                    channelRadius = 4.0}
  Note that the same call turns CHANNELS ON. Channels are the turning-radius
  mechanism: "A channel is a simplified representation of the navigable space
  around a path ... The information in a channel is easily accessible for
  computing trajectories that take care of constraints in the bot turning
  ability to anticipate the upcoming turns." The Lua doc describes
  useChannels as "helps to restrict entities with a turning radius from
  trying create paths with overly aggressive turns."
- EntityLevel/scripts/Move_Between_Cover_To_Location.lua:125 uses the
  DEPRECATED positional form, also with false, and passes an avoidLocation
  (a threat) - exactly the case the Lua doc warns abstract graphs cannot
  honour.

There is NO comment in ground-vehicle-move-to.lua explaining the value; the
file's header comment describes the road/off-road/feature-fallback structure
and never mentions abstract graphs. Searching the whole 5.2d data tree,
"useAbstractGraphs" occurs exactly once.

Release notes: the 5.2 release notes mention abstract graphs NOWHERE. The word
"abstract" appears in them only in unrelated contexts. The nearest nav entries
are VRF-8813 ("Entities tasked to wander can get stuck on uneven terrain -
Fixed in third-party product, Gameware Navigation"), VRF-9225/9238/9241
(navigation areas no longer loaded for remote entities), VRF-10203 (the
soil-tagging option), and a data note that MAK regenerated the shipped
terrains' navigation data and split combined areas into separate ground and
lifeform areas "to improve entity movement". The Migration Guide mentions
abstract graphs nowhere. The Users Guide mentions "abstract graph" nowhere -
verified by full-text extraction of all 1,806 pages.

Crucially, the ENTITY's own navigation already uses abstract graphs by
default. platforms/Ground_Vehicle.ope, navigation-parameters block, read this
session:
    (DtRwString  navigation-profile "ground-platform")
    (DtRwString  navigation-method "spline")
    (DtRwBoolean use-abstract-graph True)
    (DtRwReal    min-abstract-graph-replan-distance 300.000000)
    (DtRwReal    propagation-box-extent 200.000000)
    (DtRwReal    radius-factor 1.5)
So MAK ships abstract graphs ON for the NavBot's own destination-following,
and OFF only for the Lua one-shot route query.

2.2 What it implies for us

- Supported / discouraged / unmentioned: the flag itself is SUPPORTED and
  DOCUMENTED, with a documented default of true in both the C++ and the Lua
  API, and MAK ships it enabled at the platform level. What is unmentioned
  anywhere is any guidance about flipping the value in the shipped script.
  So flipping it is "using a documented parameter at its own API default",
  not an unsupported hack - but it is also not a configuration MAK has
  blessed for this script, and no release note or KB article covers it.
- The most defensible reading of MAK's choice is the Lua doc's own reason,
  not a bug: the script wants a FINE path because it immediately hands the
  points to formation and channel logic with a 4 m channel radius, and one of
  its two sibling call sites needs avoidLocation to work. MAK's shipped
  terrains are the ones referenced in the release notes - city and range
  databases, not 41 x 54 km of Mojave - where a 200 m corridor is almost
  always enough and the finer path is worth more.
- Therefore: flipping the flag buys long-range success by giving up the two
  things the script chose it for. That is a real cost, not a free win, and it
  is the strongest argument for testing the propagation box FIRST.

2.3 Confidence

HIGH on the facts (quotes, defaults, call sites, absence from release notes).
MEDIUM on the "why": MAK never states a rationale for the script's value; the
Lua doc's trade-off sentence is the inference's whole basis.


3. Q3 - THE RIDGE QUESTION, ANSWERED FROM DOCUMENTATION
-------------------------------------------------------

3.1 Does NavMesh GENERATION take slope into account? Yes, as a hard cut, and
    we used the shipped value.

appData/settings/vrfSim/navigationProfiles.mtl, header: "These parameters are
used during navigation data generation to create appropriate navigation data
for different kinds of entities."

  ground-platform profile, read verbatim this session:
    (entity-height 3.000000) (entity-radius 3.000000)
    (step-max 0.560000)
    (slope-max 46.000000)          ;; "Maximum slope this type of entity can
                                   ;;  traverse. Unit: degrees"
    (raster-precision 0.200000)
    (generate-abstract-data True)
    (generate-cover-points False)
    (min-navigable-surface 10.0)
    (soil-types-to-tag-with-surface-char (soil-type "road")
                                         (soil-type "pavedroad"))

Our generation used exactly this, unmodified: our navGenConfig records
(raster-precision 0.200000) and (allow-abstract-data True) and names the
profile ground-platform; it carries no slope override, because slope-max is
not a per-area setting - it lives only in the profile.

46 deg is rise/run 1.036. The 55 m face in FINDING_EARLY_STOPS runs
0.70-0.95, mean 0.858, i.e. 35.0-43.5 deg. EVERY posting on that face is
below the generator's cut. The face is legal mesh, by construction, and it was
always going to be.

The file also documents the escape hatch: "New profiles can be added - once
they are, they will show up in the navigation area dialogs as an option for
navigation data generation. Once navigation data exists for a new profile,
update the platform file of any entity that should use the new navigation
data." And: "If a profile is changed, any navigation data using the modified
profile will need to be regenerated to pick up the changes."

3.2 Does generation take SOIL / land cover into account? Yes - and for ground
    vehicles, as shipped, almost none of it.

Two independent mechanisms exist in the profile:

  (a) soil-type-tags: maps a soil type directly to a semantic tag. The
      ground-platform profile uses it for exactly one thing:
        (underwater (soil-types (type "mud")) (semantic-data
         "no-go-exclusive"))
      The lifeform profile adds trees. "no-go-exclusive" is a real
      prohibition: navDataTag.h defines DtNavDataTagSemanticNoGoExclusive = 5
      among the semantic tags, and our navGenConfig has (prune-no-go-areas
      True).
  (b) soil-types-to-tag-with-surface-char: which soils get a soil-specific
      nav tag at all. Profile comment: "For soil types which do not generate
      semantic tags, they are instead tagged with either their surface
      characteristics or just a default tag. Only The soil types listed below
      will be tagged with surface characteristics. Adding more types here can
      allow you to more finely tune path planning, but it also makes nav data
      larger and more complex."
      ground-platform lists road and pavedroad. ONLY.
      lifeform lists road, pavedroad, mud, sand, grass.

UG52 23.5 p505 says the same in prose, and 5.2 release note VRF-10203 records
why the option exists: "Nav generator producing overly detailed nav data ...
Only soil types included in the list will generate navigation tags specific to
that soil type. Other soil types receive the default navigation tag."

So: sand IS a soil type the system understands and the LIFEFORM profile
already tags - it simply is not tagged for ground vehicles. The mesh under our
tanks knows "road / pavedroad / everything else".

3.3 Can a per-cell COST be added so the planner avoids sand? Yes, and the
    machinery is entirely in shipped configuration files - no C++.

The cost model lives in the navigation-preference-controller descriptor. From
systems/movement/ground-tracked.sysdef (the M1A2's movement system), read
verbatim:

    (default-path-cost 5.000000)
    (navigation-preferences-map
       (preference-entry (preference-type "Prefer Roads") (alias
        "prefer-roads") (use-roads True)
          (path-costs
             (cost-entry (soil-name "")        (semantic-tag "road")
                         (path-cost 1.000000))
             (cost-entry (soil-name "pavedroad")(semantic-tag "")
                         (path-cost 1.000000))))
       (preference-entry (preference-type "Ignore Roads") (alias
        "ignore-roads") (use-roads False)
          (path-costs
             (cost-entry (soil-name "")        (semantic-tag "road")
                         (path-cost 5.000000))
             (cost-entry (soil-name "pavedroad")(semantic-tag "")
                         (path-cost 5.000000)))))
    (default-preference $road-preference (default "Ignore Roads"))
    (slope-avoidance-factor 1.000000)
    (min-elevation-change-to-consider-slope 0.300000)

and the descriptor that defines the semantics,
include/vrfobjparam/navigationPreferenceDescriptor.h:100-140, verbatim:

  "Mapping a preference type to path cost list. Note: Path cost values should
   never be less than 1.0!"

  slope-avoidance-factor: "Determines how much slope is taken into
   consideration when path planning. The higher this value, the more the
   steep slopes will be avoided. A value of 0 or less indicates that slope
   should not be factored in to path planning. Otherwise, the path cost for a
   particular triangle is calculated as
        inherent cost * slope-avoidance-factor * (slope/max-slope)^2 *
        MAX_SLOPE_MULTIPLIER
   where
     - inherent cost is the cost of the triangle, which may come from the
       soil type or a semantic tag. Soil type costs may be directly specified
       by the soil-name cost-entry elsewhere in this descriptor, or may be
       calculated from 1.0/acceleration-factor in the soil-factors system
       meta-data.
     - max-slope is the max-slope entity parameter; for a vehicle, this is
       reduced by the soil modifier (i.e. multiplied by the
       acceleration-factor) to give an estimate of the reduction in max-slope
       caused by terrain drag.
     - MAX_SLOPE_MULTIPLIER is a constant, currently 10.0.
   Note if the cost of a path is 100.0 or higher, the triangle is considered
   no-go. Parameter Default Value: 0.0"

  min-elevation-change-to-consider-slope: "Determines how much a triangle has
   to change in elevation before we take its slope into account when path
   planning. Parameter Default Value: 0.0" (shipped value 0.3 m.)

Upstream, the same idea is the TraverseLogic layer, and Autodesk's page
"Forbidding, Avoiding, and Preferring NavTags" shows the three levels
(SimpleTraverseLogic = forbid/allow only;
TraverseLogicWithCostMultiplerPerNavTag = per-tag cost;
TraverseLogicWithCostPerTriangle = per-triangle cost, with the warning "It
must be done carefully because the result depends on the tessellation of the
NavMesh. If the tessellation is not dense enough, the behavior becomes
erratic."). VR-Forces plugs into this with a per-point callback,
navBot.h:130:

    typedef bool (*NavBotPathLocationEvalFcn)(const DtNavTag& navTag,
        float slope, float elevationChange, float* costMultiplier,
        void* userData);

set via DtActiveBotNavInterface::setPathCostEvalFunction and accepted by
DtNavArea::findPathToLocation as costEvalFcn. That is where the formula above
is evaluated: the planner is handed the tag, the slope and the elevation
change for every candidate point.

3.4 THE ARITHMETIC - why nothing on our face is ever refused

Shipped values on the M1A2 over untagged sand:
  inherent cost          = 5.0   (default-path-cost; sand is untagged for
                                  ground-platform, so it is NOT 1/0.8)
  slope-avoidance-factor = 1.0
  max-slope (effective)  = 0.94 x 0.80 = 0.752
  MAX_SLOPE_MULTIPLIER   = 10.0
  => cost(s) = 5 * 1.0 * (s/0.752)^2 * 10 = 88.4 * s^2

    slope s   cost    no-go (>= 100)?
    0.20      3.5     no      (the control line over the same ridge)
    0.70     43.3     no
    0.752    50.0     no      <- the vehicle's own documented limit
    0.858    65.1     no      <- the 55 m face's mean
    0.950    79.8     no      <- the face's worst posting
    1.036    94.9     no      <- slope-max: the steepest triangle that exists
    1.063   100.0     NO-GO   <- unreachable: the mesh contains nothing here

The no-go gate cannot fire on any triangle the generator was willing to
produce. Slope changes route PREFERENCE and nothing else. This is the plain
answer the supervisor asked for, and it is stronger than the brief's framing:

  The face is legal to the mesh, soil is invisible to the mesh, AND the one
  hard gate the cost model has is out of reach with the shipped numbers. No
  planner FLAG will route around that face. Two configuration changes and one
  interface change can.

3.5 The documented remedies, cheapest first

R-a. RAISE slope-avoidance-factor in a copy of ground-tracked.sysdef.
     No nav-data regeneration, no C++, no licence spend - it is read at
     entity creation from the movement system definition.
     Setting it to 2.0 makes cost(s) = 100 exactly at s = max-slope x
     acceleration-factor, i.e. NO-GO begins precisely where MAK's own
     documentation says the vehicle stops being able to accelerate. That is
     not a tuned number; it falls out of the formula.
       saf = 1.25 -> the 0.95 worst posting becomes no-go
       saf = 1.54 -> the 0.858 mean becomes no-go
       saf = 2.00 -> everything at or above 0.752 becomes no-go
     Side effect to state in any prereg: it scales cost everywhere slope > 0,
     so it changes route choice globally, not only at the face.
     CAVEAT, unverified: the header says max-slope is reduced by "the soil
     modifier" but does not say whether that is the soil under the candidate
     triangle or the soil under the entity right now. On our area the answer
     barely matters (sand nearly everywhere), but it matters for the general
     case.

R-b. LOWER slope-max in a new navigation profile and regenerate.
     Documented and supported ("New profiles can be added"), but it costs a
     full regeneration (about 3.3 h for the 41 x 54 km area, per
     PLAN_PARALLEL_LANES L14) and it INTERACTS BADLY with H1: excising the
     face from the mesh forces routes to deviate further, and a flat query
     that already cannot deviate more than 200 m becomes MORE likely to
     refuse, not less. Do not do this before the propagation box is settled.

R-c. TAG SAND and give it a cost. Add (soil-type "sand") to ground-platform's
     soil-types-to-tag-with-surface-char (the lifeform profile already does
     this, so it is a supported value) and add a
     (cost-entry (soil-name "sand") (path-cost N)) to the preference map.
     Needs regeneration, and the release note warns it enlarges the data.
     Note the sign trap: the formula multiplies by inherent cost, so tagging
     sand with the "natural" 1/0.8 = 1.25 would LOWER the face's cost from
     the current 5.0 baseline. Only a deliberately HIGH N helps.

R-d. EXCLUSION VOLUMES. The generator can emit "no-go-exclusive" from a
     feature query or a soil type (feature-tag-volumes / soil-type-tags), and
     DtNavArea has a runtime tag-volume API (newNavTagId,
     associateNavTagIdWithObject, add-tag-volume with a DtInputNavTag). This
     is the surgical option for a known bad patch, and it needs no
     regeneration if done at runtime - but it requires C++ and it requires
     knowing where the bad patches are, which is the same leg-wise slope
     check as R-e.

R-e. OUR OWN LEG CHECK, which no vendor mechanism replaces. Everything above
     biases or forbids inside the mesh; none of it exists off the mesh, and
     the feature-obstacle fallback is documented as slope-blind. The
     interface's route authoring should test max-slope x soil-factor along
     the whole leg, not at the four vertices - which is exactly the extension
     already queued as DEMO_READINESS row 18 / tools/preflight.

3.6 Confidence

HIGH on all of 3.1-3.4: every number is a line in a shipped file or a
sentence in a shipped header, all read this session. The one inference is the
arithmetic, which is just the vendor's formula evaluated.


4. Q4 - VEHICLE DYNAMICS ON SLOPE x SOIL, AND WHETHER THE SIM REPORTS A STALL
-----------------------------------------------------------------------------

4.1 Where the limit lives

movingObjectParameters.h:294-310, max-slope, verbatim:

  "For ground entities, the maximum slope, at which the entity can still have
   acceleration >= 0 in its direction of motion. Therefore, if a ground
   vehicle is heading up a hill, as soon as the slope of the hill is greater
   than the max-slope, it will no longer be able to accelerate up the slope.
   At best, without any friction or other source of negative acceleration, it
   will remain at its current speed. If the slope increases, it will start
   decelerating and even start rolling backwards. ... The units are the
   fraction rise-over-run, i.e. tangent of the angle of inclination.
   Parameter Default Value: 1."

Values, read this session:
  M1A2 SEP V2 Abrams.entity:300   <real paramName="max-slope">0.94</real>
  ground-tracked.sysdef soil-factors, sand:
      (acceleration-factor 0.800000) (stopping-factor 0.750000)
    (paved-road 1.00/0.95; hard-packed 0.98/0.90; gravel 0.95/0.90;
     rocks 0.80/0.90; shallow-water 0.70/0.60; muck 0.40/0.60;
     deep-water 0.00/0.00; snow 0.60/0.60; ice 1.00/0.30)

UG52 23.5.1 p506-507 defines the factors: "The acceleration-factor affects the
surface's drag on a vehicle. Use values ranging from 0 to 1, where lower
values increase drag." Table 26 p506 maps terrain soil types to roughness
types: sand -> sand, softSoil -> sand, dryGround/asphalt/forest/grass/
cultivatedFields/orchards -> hard-packed, rock/boulder -> rocks, mud/swamp ->
muck.

UG52 23.5.2 p508, the dynamics sentence, verbatim:

  "In the vehicle dynamics model, moving up slope requires a higher throttle
   setting than moving over level ground, and moving down slope requires a
   lower setting. On the best soil surface (dry pavement), vehicles can just
   barely move up the max-slope defined in the entity parameters by using
   maximum throttle. It is possible that vehicles may slide down slopes,
   especially if the soil is slippery."

and the two planner sentences, also verbatim:

  "For planning paths in nav meshes, higher slopes cost more (whether the
   slope is up or down). In the movement .sysdef files ... the
   slope-avoidance-factor in the navigation-preference-controller descriptor
   can vary the effect."
  "Ground slope is not taken into account by the path planner that plans
   around feature obstacles."

Status of "effective max-slope = max-slope x soil acceleration-factor": it is
DOCUMENTED, but only in the PLANNER's cost formula
(navigationPreferenceDescriptor.h, quoted in 3.3), where MAK itself calls it
"an estimate". The DYNAMICS model is documented only qualitatively - the
"best soil surface (dry pavement)" sentence above - and no document states the
dynamics law. So 0.94 x 0.80 = 0.752 should be cited as "the planner's own
derating estimate", which is a stronger warrant than the memory note's
"analogy" but still not a statement about the physics.

4.2 Does the MOVE controller report it? No - and this is a design position,
    not a gap we found.

- The only two shipped decideToGiveUpTask overrides on the ground move path
  test something else:
    groundMoveToDestinationControllerComponent.h:281-284 - "Checks to see if
    destination is inside an obstruction. If it is, returns true. Otherwise
    it returns the value of DtGroundAutoControllerComponent::
    decideToGiveUpTask()"
    groundAutoControllerComponent.h:317-319 - "If the entity is attached and
    the target of the current task is not, give up on the task."
  Neither is a progress or stall test. This REFINES rather than contradicts
  the earlier record: the base test is not the only one, but no shipped
  ground override tests progress.
- The mechanism that would run one exists:
  multiTaskControllerComponent.h postTick() - "Override to check if we're
  tasked and we want to give up (only performs the check if we're in play
  mode (dT() > 0)). Calls decideToGiveUpTask() to determine if we should give
  up. If true, calls giveUpTask()."
- The vendor's recommended way to add one is an EXAMPLE PLUGIN, and MAK says
  so in as many words. classdoc "Decide to Give Up (decideToGiveUpTask)":
  "The Decide to Give Up example installs a new version of the
  DtGroundMoveAlongControllerComponent to give up the task after a certain
  amount of sim time. This example can be used as a starting point for other
  type of reasons to give up on a task. Since this example is loaded as a
  plugin, it can be used in conjunction with the released VR-Forces
  application." The walkthrough is literally our scenario: create an M1A2,
  create a waypoint at least 50 m away, move-to, set Object Console Notify
  Level to Info, "After 10 seconds of simulation time, the entity will stop
  moving and there will be messages printed to the entity Information dialog
  and the SIM console indicating that the entity has 'given up on the task'."
- Vendor PRECEDENT for a burndown watchdog exists, but only on the aggregate
  path: disaggregatedManeuverAlongController.h:285-292,
  "Check to see if the unit task should be ended because subordinate movement
  has ended. This is a fail safe, because normally the subordinates should
  finish their moves and send taskComplete reports ... if the leader has
  finished its movement task and the subordinates are all stopped, but
  haven't sent task complete reports, then something is hanging them up. In
  that case this unit behavior can shut itself down and continue." with
  DtTime myStuckSubordinatesGiveUpSimTime. It is NOT armed for an entity-level
  ground move.
- Status IS available in C++ and NOT in Lua. DtNavBot::PathStatus enumerates
  PathStatusInactive, PathStatusComputing, PathStatusHasPath,
  PathStatusArrived, PathStatusNoValidPath, PathStatusLeavingArea,
  PathStatusFindingTransitionPoint, PathStatusNoValidPathDestOutside,
  PathStatusNoValidPathStartOutside; DtActiveBotNavInterface exposes
  pathStatus(double atDistance) and pathStatusString() ("A text status of the
  current path query, if available. Useful for debugging"). No shipped Lua
  script references either, and the Lua job class exposes only results and
  completion - which is why the console prints commands and never the
  vehicle's response, as the record already found. Similarly the query's own
  failure reason (ASTAR_DONE_PATH_NOT_FOUND vs
  ASTAR_DONE_ERROR_LACK_OF_WORKING_MEMORY) is not reachable from Lua, so our
  "not enough (0) points" can never be attributed from the script layer.

4.3 Implication

The interface's own progress watchdog (DEMO_READINESS row 10a's user decision)
is the right answer and is now supported by vendor documentation rather than
merely by our observation: MAK's position is that stall detection belongs to
the integrator, and their published starting point is a plugin. Our watchdog
sits one level further out (telemetry-side, no C++, no plugin load), which is
strictly cheaper and loses only the ability to make the SIM itself abandon the
task.

Confidence: HIGH.


5. Q5 - COMMUNITY AND SAMPLES
------------------------------

State plainly: NOTHING WAS FOUND. Six searches (exact error string; VR-Forces
plus navigation area plus sectors; MAK knowledge base plus abstract graph;
Autodesk forums plus out-of-working-memory / PATH_NOT_FOUND / propagation box;
Kynapse hierarchical pathfinding; Gameware Navigation abstract graph) returned:

- No occurrence anywhere on the public web of "Planned nav path has not enough"
  or of VR-Forces long-distance nav-mesh query failures. VR-Forces is not
  discussed in public forums at a technical level; MAK has no public forum,
  only a support portal behind login and a blog.
- The one MAK technical article in the area is the cover-points tech tip
  (mak.com blog id=178). Its practitioner advice is generic and does not touch
  our problem: configure generate-cover-points per profile in
  navigationProfiles.mtl, regenerate navigation data after any profile change,
  verify with the Navigation Lab debug application, and supplement manually
  with Cover Point tactical graphics "when navigation mesh triangles are
  particularly large". The only transferable item is the workflow: CHANGE
  PROFILE -> REGENERATE -> VERIFY IN NAVIGATION LAB. We have never used the
  Navigation Lab to look at our area; it ships (navigationLab.exe was seen in
  an earlier session's bin64 string dumps) and it is the vendor's own
  verification instrument for exactly the question "what does the mesh think
  of this ground".
- Autodesk's Gameware Navigation was withdrawn from sale in July 2017
  (Wikipedia / Grokipedia, corroborating the product history), which explains
  the absence of any living community. The SDK help itself is still served and
  is, as this document shows, the best source available.
- The academic literature confirms the shape of the technique but adds nothing
  actionable: hierarchical navmesh pathfinding (HNA*, Pelechano et al.) plans
  on a coarse abstraction and refines locally, reporting up to 7.7x speedups -
  the same trade (speed for optimality) MAK's profile comment states.

Confidence: HIGH that nothing public exists. Absence of evidence here is close
to evidence of absence - the product's user base does not publish.


6. WHAT THIS MAKES UNNECESSARY, AND WHAT IT MAKES NECESSARY
------------------------------------------------------------

UNNECESSARY - do not spend runs on these

| planned/possible run | why the docs kill it |
|---|---|
| Any further variation of gamewareMemorySize | Already measured to do nothing (G7B 2.1), and the mtl itself says the value is PER BLOCK with multiple blocks allocated, so it is not a monotone knob. There is nothing more to learn. |
| Any further variation of gamewareQueryTimeBudget | Measured to do nothing (G7B 2.2); documented as a per-frame responsiveness/frame-rate trade, not a success/failure gate. |
| A ladder to find "the distance or sector-count ceiling" | No such ceiling is documented anywhere. The documented limit is a DEVIATION limit (200 m from the chord), not a length limit - which also explains G7B fact (d), a 5,013.7 m / 10-seam query planning while a 4,550.2 m / 9-seam one refused. Searching for a length ceiling is searching for something the vendor says does not exist. |
| A run to discover whether the mesh knows the ground is sand | Answered: it does not. ground-platform tags road and pavedroad only. |
| A run to discover whether the mesh forbids our face | Answered: it cannot. slope-max 46 deg admits it, and the cost no-go threshold (100) is unreachable below slope 1.063 while the mesh contains nothing above 1.036. |
| A run to see whether the sim will report the stall | Answered: no shipped ground controller tests progress, and MAK's published remedy is an integrator plugin. |

NECESSARY - the docs raise these, and only a run can close them

| run | what it tests | why it is now first |
|---|---|---|
| N1. propagation-box-extent raised (e.g. 200 -> 2000) in an SMS-overridden copy of platforms/Ground_Vehicle.ope, stock Lua (flat query), 1-35 ridge lane | Does the long flat query stop refusing? | It is the ONLY documented mechanism that predicts deviation-dependent refusal, it was never varied, and G7b measured the successful abstract route at 231-292 m cross-track - outside the 200 m corridor. If it works it also returns a FINE path (7-9 m) that keeps slope and NavTag costs, which the abstract flag does not. Single variable. |
| N2. N1 plus slope-avoidance-factor 1.0 -> 2.0 in an SMS-overridden copy of systems/movement/ground-tracked.sysdef | With the query succeeding, does the cost model now route around the 55 m face? | This is the first configuration in which the planner is both ABLE to answer and ABLE to refuse the face. 2.0 is not tuned: it puts the no-go threshold exactly at max-slope x acceleration-factor. |
| N3. The ridge-AG run, if and only if N1 refuses | Whether abstract graphs remain the only way to get a long route at all | See section 7. |

Two mechanical notes for whoever writes the prereg:
- Both N1 and N2 are shipped-configuration edits carried by the SAME custom
  including-SMS mechanism that already carries the Lua override at
  C:\C2SIM\vrf-sms. VERIFY FIRST that the include mechanism covers
  platforms\ and systems\movement\ and not only scripts\ - that is a
  read-only check of the .sms file, not a run.
- vrfobjcore.dll's parameter-name string table contains propagation-box-extent,
  min-abstract-graph-replan-distance, radius-factor and
  enable-path-plan-timing alongside the three parameters the header's doc
  comment lists (navigation-method, use-abstract-graph,
  min-abstract-graph-replan-distance). The header comment at
  activeBotNavInterface.h:248-253 is therefore INCOMPLETE, not authoritative,
  and the .ope's propagation-box-extent line is read. Treat the binary string
  as supporting evidence only; the run is the proof.


7. VERDICT ON THE RIDGE TEST
-----------------------------

REDESIGN IT, and reorder it behind one cheaper run. As PREREG_RIDGE_AG stands,
the abstract-graph run on the 1-35 ridge lane is built to falsify "abstract
graphs return something USEFUL" by seeing whether the coarse route avoids the
55 m face. The documentation now answers a large part of that before the sim
starts, and answers it against the run's value: Autodesk states that
AbstractGraphs "do not consider ... traversibility of a NavTag and its cost",
so the coarse layer is cost-blind by design and has no mechanism to avoid the
face; and section 3.4 shows that even a perfectly refined fine path would not
avoid it either, because with the shipped slope-avoidance-factor 1.0, default
path cost 5.0 and untagged sand, the face costs 43-80 against a no-go
threshold of 100 that no legal triangle can reach. A "drove across it and
froze at 1.97 km" outcome would therefore be over-determined by three
independent documented causes - cost-blind coarse layer, unreachable no-go
gate, and (if the query falls back) a feature planner the Users Guide says
ignores slope entirely - and would not distinguish among them, which is
precisely the failure mode the run was designed to avoid. The higher-value
sequence is N1 then N2: first raise propagation-box-extent, the one documented
mechanism that fits the refusal pattern and the one knob we never touched, and
see whether the ordinary fine-grained flat query starts answering long legs
(if it does, it is strictly better than the abstract flag, because it keeps
the cost model the abstract layer discards, and the whole "should we adopt
useAbstractGraphs=true" question may simply dissolve); then, with a query that
answers, raise slope-avoidance-factor to 2.0 so that the no-go boundary sits
exactly where MAK's own documentation says the vehicle stops being able to
accelerate, and watch whether the planned leg bends around the face. Keep the
abstract-graph ridge run as N3, to be spent only if N1 refuses - at which
point it stops being "is the flag useful?" and becomes "is the flag the only
way to get a long route at all?", a question worth a run. And note the
interaction that outranks all of them: regenerating the area with a lower
slope-max (remedy R-b) must NOT be attempted before N1 settles, because
excising the face forces larger deviations and a 200 m corridor would refuse
even more often than it does today.


8. SOURCES
-----------

Autodesk Navigation SDK (Gameware Navigation, formerly Kynapse) - live help
- AStarQuery Options (propagation box, 200.0f default, hooking, TryCanGo)
  https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/files/GUID-4E7030F2-75CD-4992-9FD4-1C4157AA441B.htm
- Creating AbstractGraphs
  https://help.autodesk.com/cloudhelp/ENU/Navigation-SDK-Help/nav-help/navdata/creating_abstractgraphs.html
  (2016 GUID mirror: .../files/GUID-11DD2A34-AD33-4973-B5FB-190F16C1A3C9.htm)
- NavData (structure; sub-topic list)
  https://help.autodesk.com/cloudhelp/ENU/Navigation-SDK-Help/nav-help/navdata.html
  https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/files/GUID-D5F11989-05DF-4703-9C68-8D6628CDC139.htm
- Sectorizing the Terrain (single-sector memory/performance warnings)
  https://help.autodesk.com/cloudhelp/ENU/Navigation-SDK-Help/nav-help/navdata/sectorizing_the_terrain.html
  https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/files/GUID-A581692A-75D6-4071-8439-9C532762AD97.htm
- Forbidding, Avoiding, and Preferring NavTags (TraverseLogic, cost model)
  https://help.autodesk.com/cloudhelp/ENU/Navigation-SDK-Help/nav-help/path_finding_and_path_following/forbidding_avoiding_navtags.html
- Getting a Static Path (SetPropagationBoxExtent in the canonical example)
  https://help.autodesk.com/cloudhelp/ENU/Navigation-SDK-Help/nav-help/path_finding_and_path_following/getting_a_static_path.html
- Channels for Computing and Following Trajectories
  https://help.autodesk.com/cloudhelp/ENU/Navigation-SDK-Help/nav-help/path_finding_and_path_following/channels.html
- Alternative Path Finding Queries
  https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/files/GUID-05D03E87-E74A-4621-A9DB-52601930B2BA.htm
- Kaim::AStarQuery class reference (m_propagationBoxExtent,
  SetAbstractGraphTraversalMode, DisplayPropagationBounds)
  https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/api/class_kaim_1_1_a_star_query.html
- Kaim namespace reference (AStarQueryResult enum, incl.
  ASTAR_PROCESSING_ABSTRACT_PATH, ASTAR_DONE_PATH_NOT_FOUND,
  ASTAR_DONE_ERROR_LACK_OF_WORKING_MEMORY)
  https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/api/namespace_kaim.html
- Overview / product history
  https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/files/GUID-006522C2-43EE-41A9-B305-DA3A8B86E40A.htm
  https://en.wikipedia.org/wiki/Kynapse

MAK, public web
- Tech tip: Configuring VR-Forces path planning to support cover points
  https://www.mak.com/learn/blog?view=article&id=178:blog-tech-tip-configuring-vr-forces-path-planning-to-support-cover-points&catid=17:technical-blog

MAK, installed 5.2d tree (all read this session, read-only)
- C:\MAK\vrforces5.2d\doc\VRFUsersGuide.pdf
    p501 "Path Planning on the Nav Mesh" / "Planning an Off-road Path Between
    Feature Obstacles" / "Navigation Preferences"
    p505-507 23.5, 23.5.1 (Table 26 soil/roughness map; acceleration-factor,
    stopping-factor)
    p508 23.5.2 Slope Effects on Movement
    p1276 66.2 Navigation Areas; p1277 66.2.1 Sectorizing Navigation Areas
    ("abstract graph" occurs NOWHERE in the 1,806 pages)
- C:\MAK\vrforces5.2d\doc\VRF5.2ReleaseNotes.pdf - VRF-8813, VRF-9225/9238/
  9241, VRF-10203, the nav-data regeneration data note ("abstract" occurs in
  no navigation context)
- C:\MAK\vrforces5.2d\doc\VRFMigrationGuide.pdf - no abstract-graph content
- C:\MAK\vrforces5.2d\doc\luadoc\modules\vrf.html - findPathToLocation
  parameter table (the useAbstractGraphs trade-off sentence)
- C:\MAK\vrforces5.2d\doc\classdoc\classref\decidetogiveup.html - the
  Decide to Give Up plugin example
- C:\MAK\vrforces5.2d\appData\settings\vrfSim\navigationProfiles.mtl -
  lifeform / ground-platform / background-lifeform profiles
- C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl:378-389 -
  gamewareMemorySize, gamewareQueryTimeBudget
- C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\
  ground-vehicle-move-to.lua:488, :497, :503-510
- C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\
  Move_Between_Cover_To_Location.lua:122-127
- C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\vrfSim\platforms\
  Ground_Vehicle.ope:16-35 (navigation-parameters), :475 (max-slope binding)
- C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\vrfSim\systems\
  movement\ground-tracked.sysdef:75-134 (navigation-preference controller),
  :787-830 (soil-factors)
- C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\vrfSim\
  M1A2 SEP V2 Abrams.entity:300 (max-slope 0.94)
- C:\MAK\vrforces5.2d\include\vrfNavigation\navBot.h:41-67, :130, :140-180
- C:\MAK\vrforces5.2d\include\vrfNavigation\navArea.h:233-235, :393-402
- C:\MAK\vrforces5.2d\include\vrfNavigation\navDataTag.h:18-82
- C:\MAK\vrforces5.2d\include\vrfLua\luaNavJob.h:319-329
- C:\MAK\vrforces5.2d\include\vrfobjcore\activeBotNavInterface.h:83-85,
  :95-119, :248-253
- C:\MAK\vrforces5.2d\include\vrfobjparam\navigationPreferenceDescriptor.h:
  100-140
- C:\MAK\vrforces5.2d\include\vrfobjparam\movingObjectParameters.h:294-310
- C:\MAK\vrforces5.2d\include\vrfmodel\
  groundMoveToDestinationControllerComponent.h:281-284
- C:\MAK\vrforces5.2d\include\vrfmodel\groundAutoControllerComponent.h:317-319
- C:\MAK\vrforces5.2d\include\vrfmodel\disaggregatedManeuverAlongController.h:
  285-292, :376-381
- C:\MAK\vrforces5.2d\include\vrfobjcore\multiTaskControllerComponent.h:43-47
- C:\MAK\vrforces5.2d\bin64\vrfobjcore.dll - parameter-name string table
  (supporting evidence only)

Our own record (read for grounding, not quoted as authority)
- C:\C2SIM\vrf-nav\navData\MAK Earth (online)\
  NavArea-ground-platform MojaveCOA\...navGenConfig and the sibling
  .navRuntimeConfig
- docs\experiments\NAVMESH_QUERY_DOCS_2026-09-14.md
- docs\experiments\G7B_G8_RESULTS_2026-09-14.md secs 2.1-2.4
- docs\experiments\PREREG_RIDGE_AG_2026-09-14.md
- docs\DEMO_READINESS_2026-09-06.md row 10a
