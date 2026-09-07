# Doc sweep: what the vendor sources say about ground units stalling mid-route

Answers the user's question of 2026-09-07: "If not task split, what do the docs say
about the issue you were trying to solve with that?" Produced by a workflow of eight
readers (Sonnet) over the Users Guide, Migration Guide, Developer's Guide help, the
EntityLevel Lua/task rules, the headers and examples, and the public web, with every
statement re-opened by an independent adversarial verifier (Opus).

STATE OF THIS DOCUMENT - READ BEFORE USING IT:
- 203 candidate statements were produced; 120 were adversarially verified before the
  session limit stopped the run; 84 verifiers and the SYNTHESIS never ran.
- ONLY THE 19 STATEMENTS BELOW passed verification (the quote exists at the stated
  location AND the stated meaning follows without inference). 101 were REJECTED and
  are deliberately absent; 83 remain unverified and are NOT recorded here.
- No synthesis, no mechanism claim and no lever recommendation is made here. That
  step is owed and is the user's / Fable's call. Raw material, including the
  rejections and their objections, is in the session scratchpad
  (docsweep-verified.json, docsweep-findings.json) and in the workflow journal
  wf_30a3ee28-12e; it is NOT durable storage.

## Route authoring and what the move tasks do

- **VR-Forces 5.2 Users Guide** - docs/vendor/mak-5.2/VR-Forces_5.2_Users_Guide.pdf, printed p.500, sec 23.1 Overview (location as stated is correct); corroborating detail at p.505 sec 23.3, qualifier at p.503 sec 23.2.4
  > MoveAlongRoute.Movesthevehiclefrompointtopointalongaroutetacticalgraphic,usingdynamicobstacleavoidance.MoveAlongRoutedoesnotuseroadmovement(thatis,drivinginlanesandfollowingtraffic),nordoestheentityplanpathsbeforemoving. [verbatim, spaces stripped by pypdf]
  Directly on point for the user's question: MoveAlongRoute (the task the interface issues per unit) is explicitly documented as NOT path-planning at all -- it only reacts to obstacles moment-to-moment. This is the vendor's own confirmation of the prereg's 3g conclusion that 'mid-route legs are go-straight between offset-route vertices.'
  QUALIFIER FOUND BY THE VERIFIER: Two neighbouring qualifiers. (a) p.503 sec 23.2.4: "Ifyougiveamovementtaskwitharoaddrivingelement(includingTreatRouteasRoadinaMoveAlongRoutetask,orPreferRoadsinaspawnpattern'sdefaultplan)toanyothertypeofentity,itwillfail." -- so "does not use road movement" describes the DEFAULT; MoveAlongRoute has 

## Replanning

- **ground-vehicle-move-to.lua** - C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-vehicle-move-to.lua lines 43-44 (constant), 1314-1342 (isDestChanged), 1583-1592 (maybePlanPath gate), 1266-1300 (attemptGlobalReplanningOnce -> setStateVar("replan",true)), 789-801 and 1166-1170 (the only two writers of blockageSkirtFail), 1380 (the only place MultiPartPath is set to nil)
  > -- If the destination moves more than this, then the path is replanned LOCATION_CHANGE_THRESHOLD = 1.0
  Outside of a blockage, the ONLY other trigger for replanning the whole route is the destination location itself moving more than 1.0 m (isDestChanged, lines 1320-1342) - there is no periodic or distance-along-route replan; a static destination with an already-accepted plan is never revisited unless blocked.
  QUALIFIER FOUND BY THE VERIFIER: The code enumerates THREE replan triggers, not one. Lines 1314-1316: "-- Replanning is required if there is no path, if the global \"replan\" variable is set, -- or if the destination location has changed." And lines 1324-1339: "if getStateVar(\"MultiPartPath\") == nil then ... replanNow = true else

## Obstacles, blockage and giving up

- **ground-vehicle-move-to.lua** - C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-vehicle-move-to.lua lines 67-69 (constant), mechanism at lines 219-264 (isMovementBlocked), consumers at lines 756-758, 1007, 1020
  > VEH_STOPPED_TIME_ALLOWED = 10.0
  A vehicle that is not moving is not immediately treated as blocked; the comment two lines above (line 67-68, 'Amount of time a blocking vehicle has to be stopped before it is considered a blockage and will trigger a backup and skirt maneuver') says the sim waits 10 simulated seconds of continuous BlockedByVehicle status before it acts.
  QUALIFIER FOUND BY THE VERIFIER: Lines 67-69 verbatim as stated: "-- Amount of time a blocking vehicle has to be stopped before it is / -- considered a blockage and will trigger a backup and skirt maneuver. / VEH_STOPPED_TIME_ALLOWED = 10.0". The mechanism is in isMovementBlocked() (lines 219-264): line 254 "setStateVar(\"timeWhenB

- **ground-vehicle-move-to.lua** - C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-vehicle-move-to.lua lines 219-264 (isMovementBlocked); consumers at lines 756-758 (isPathBlocked), 1200-1207 (maybeSkirtABlock), 1007 and 1020 (backup abort checks); constant at 67-69 (VEH_STOPPED_TIME_ALLOWED = 10.0)
  > local blocked = false if taskInfo.Status == "BlockedByWall" then if not isBlockedByWall then setStateVar("isBlockedByWall", true) printVerbose("Movement is blocked by a wall") end blocked = true else setStateVar("isBlockedByWall", false) end if taskInfo.Status == "BlockedByVehicle" then if wasStoppedByVeh then local timeWhenBlocked = getStateVar("timeWhenBlocked") or 0 if vrf:getSimulationTime() > timeWhenBlocked ...
  "Blocked" is defined ONLY as the running subtask's own status field reading exactly "BlockedByWall" or "BlockedByVehicle" (after the 10s grace timer for the vehicle case). Any other reason a vehicle stops moving - including, on the stated facts, a grade the planner never modeled - does not set blocked=true and triggers none of the skirt/backup/replan machinery below.
  QUALIFIER FOUND BY THE VERIFIER: Stated location is exact; the quote matches lines 235-250 word for word (only elisions in the submitted quote). taskType comes from getStateVar("currentTask") (line 220), described at line 112 as "blockable tasks' name which is tracked in isMovementBlocked()", and taskInfo = this:getInternalStatePro

- **ground-vehicle-move-to.lua** - C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-vehicle-move-to.lua lines 71-76 (as stated; no correction needed)
  > -- Amount of vehicle lengths to use when attempting to back up and drive around an obstacle SKIRT_OBSTACLE_DISTANCE_BACK = 2 SKIRT_OBSTACLE_DISTANCE_FORWARD = 5 -- Speed to travel while backing up REVERSE_SPEED = 3
  The vendor's obstacle remedy for a wall/vehicle blockage is a short local back-up (2 vehicle lengths) and forward skirt (5 vehicle lengths) at 3 m/s, then a local replan of just that path segment - a small, local maneuver, not a route-level detour.
  QUALIFIER FOUND BY THE VERIFIER: Lines 67-69 (10 s dwell before a vehicle blockage counts; wall blockage is immediate); lines 46-47 MAX_REPLANS = 3 then abort via blockageSkirtFail; lines 999-1002 supply the m/s unit for REVERSE_SPEED; line 1035 reuses SKIRT_OBSTACLE_DISTANCE_FORWARD as a "close enough" threshold; header lines 5-9 

- **ground-vehicle-move-to.lua** - C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-vehicle-move-to.lua:46-47,67-69,219-264,776-802 (plus the tree wiring that the 'meaning' depends on: 1158-1216 maybeSkirtABlock/testBackupAttemptLimit, 1220-1264 loopOverPathParts failWhen, 1266-1301 attemptGlobalReplanningOnce, 1320-1342 isDestChanged)
  > L46-47: "-- Attempts to replan and move before deciding it is permanently stuck and aborting\nMAX_REPLANS = 3"; L67-69: "-- Amount of time a blocking vehicle has to be stopped before it is\n-- considered a blockage and will trigger a backup and skirt maneuver.\nVEH_STOPPED_TIME_ALLOWED = 10.0"; L246-258: "if taskInfo.Status == \"BlockedByVehicle\" then / if wasStoppedByVeh then / local timeWhenBlocked = getStateVa...
  Confirms the exact mechanism the prereg summarized: a vehicle whose Status is BlockedByVehicle for 10 s (VEH_STOPPED_TIME_ALLOWED) is treated as a blockage; the tree then backs up and skirts, incrementing replanAttempts each time; once attempts exceed MAX_REPLANS=3 it sets blockageSkirtFail, which forces exactly one global replan (see globalReplanAttempted below) before the stall loop.
  QUALIFIER FOUND BY THE VERIFIER: Verified but qualified. (a) Scope is correct: EntityLevel path, file header L1-2 "This is the scripted task that ground vehicles use for all movement to a specified location." Not aggregate, not air/human. (b) The 10 s timer applies ONLY to Status=="BlockedByVehicle"; Status=="BlockedByWall" sets bl

## Slope and soil

- **VR-Forces 5.2 Users Guide** - VR-Forces 5.2 Users Guide, printed p.1688 (PDF page 1688 of 1806), first lines of the page; section D.2.3 "Moving Object Parameters" begins on p.1687 and continues onto p.1688. Table 80 "Parameter types for simulation objects" is on the same p.1688. Supporting ground-vehicle text: p.508, sec 23.5.2 "Slope Effects on Movement"; also p.512 (fixed-wing takeoff, sec 24.1.5); parameter-file structure: p.1682, sec D.1.
  > "Parametersincludesupportpoints,maximumspeed,maximumreversespeed,turning radius,maximumslope,orderedspeed,maximumacceleration,maximumdeceleration, andfuelefficiency.Unitsaremovingobjectsandhavethisparametertype.Allofthe platform-specificparametertypesderivefromthisone." (p.1688; extraction drops inter-word spaces)
  Confirms max-slope is a configurable per-entity-type parameter (ground-vehicle-param derives from moving-object-param); the Guide names the parameter but gives no per-vehicle (M1A2/M3/M577) numeric defaults anywhere in the text.
  QUALIFIER FOUND BY THE VERIFIER: Immediately after the quote, same paragraph, p.1688: "Unitsaremovingobjectsandhavethisparametertype.Allofthe platform-specificparametertypesderivefromthisone." Table 80 on the same page lists human-param, fixed-wing-entity-param, rotary-wing-entity-param, missile-param and aggregate-param as also in

- **VR-Forces 5.2 Users Guide** - VR-Forces 5.2 Users Guide, printed page 512 (pypdf page index 512), sec 24.1.5 "How Fixed-Wing Entities Take Off" (the section heading itself begins on printed p.511; the quoted sentence is on p.512). Related ground-vehicle text: printed p.508, sec 23.5.2 "Slope Effects on Movement".
  > "Whenafixed-wingentitytakesoff,itdoesnottakeintoaccountthesoiltypeorterrain,exceptthatiftheslopeoftheterrainalongwhichtheentitymustmovetotakeoffisgreaterthanthemax-slopeparameter,theentityhalts." (p.512; spaces lost in extraction)
  The ONLY sentence in the whole Guide that explicitly says an entity 'halts' because of exceeding max-slope, and it is written for FIXED-WING ground roll, not ground vehicles. No equivalent 'halts' sentence exists for ground vehicles anywhere in ch.23; for them the Guide only describes throttle/traction degradation and possible sliding (see other slope findings), never a named halt state.
  QUALIFIER FOUND BY THE VERIFIER: Two neighbouring/related items a reader should see, neither of which refutes the finding. (a) Same section, immediately preceding paragraph, p.512: "Whenyoutaskafixed-wingentitytomovetoawaypointthatisabovegroundlevel,theentityorientsitselfdirectlytowardsthatpointandacceleratesinthedirectionofthepoin

- **VR-Forces 5.2 Users Guide** - docs/vendor/mak-5.2/VR-Forces_5.2_Users_Guide.pdf, printed page 1282 (= pypdf page index 1282), chapter 66 "Generating Navigation Data", sec 66.3.1 "Navigation Profiles", final paragraph before sec 66.3.2 - as stated, no correction needed
  > Ifrequiredfortheterrainyouwilluseinyoursimulation,youcanspecifywhichsoiltypesaretaggedwithsurfacecharacteristics.Thereisaconfigurationoptioninnavigation Profiles.mtl,soil-types-to-tag-with-surface-char, whichlistssoiltypes.Onlysoiltypesincludedinthelistwillgeneratenavigationtagsspecifictothatsoiltype.Othersoiltypesreceivethedefaultnavigationtag.Fordetails,seethatsectionofthefile.
  The only soil-related lever in this chapter is a generation-time TAGGING option (soil-types-to-tag-with-surface-char) that marks graph nodes with a surface characteristic for a listed soil type; untagged soil types get a generic default navigation tag. Nothing here states what the tag DOES (e.g., a speed penalty or impassability threshold) or ties it to slope.
  QUALIFIER FOUND BY THE VERIFIER: The finding's quote truncates the paragraph. The two omitted sentences are "Othersoiltypesreceivethedefaultnavigationtag.Fordetails,seethatsectionofthefile." Neither qualifies the finding - the first directly supports the "untagged soil types get a default tag" half of the meaning, and the second co

- **groundAutoControllerComponent.h** - C:\MAK\vrforces5.2d\include\vrfmodel\groundAutoControllerComponent.h:190-199 (confirmed exact; the doc block is lines 192-198, attached to tractionFactorFromSoilAndWeather() on line 199)
  > virtual double decelerationSoilFactor() const; //! Gets the soil type from current location, then looks up the stopping (traction) //! factor. Then looks up weather and makes some guesses as to the effects //! (does not replicate the computation that is used for the actuator; //! that would be more perfect, but maybe not realistic. And it should be //! put in a common place like the SR then, not the actuator). //!...
  Soil (and weather) affect vehicle deceleration/traction estimates used for speed targeting in the controller, and the comment itself flags that this estimate 'does not replicate the computation that is used for the actuator' -- i.e. the vendor's own docs admit the controller-level soil model and the physical actuator's soil model are two different, not-fully-reconciled computations. No slope term 
  QUALIFIER FOUND BY THE VERIFIER: Quote is verbatim, line for line, including the ellipsis-truncated tail the finding omitted ("And it should be put in a common place like the SR then, not the actuator"). Entity-level ground vehicle scope is confirmed by the class header, lines 26-34: "DtGroundAutoControllerComponent is a base contr

## Units and stalled members

- **ground-vehicle-unit-move-to.lua** - C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-vehicle-unit-move-to.lua lines 269-307 (exact, as stated). Supporting: same file lines 309-320 (moveAround), 322-327 (maybeStopped), 329-334 (determineStopped selector), 336-355 (leadUnit/followLeader/role nodes), 357-372 (setup: subTimes init per subordinate); dispatch at C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-unit-move-to.lua lines 169-174; task metadata C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-vehicle-unit-move-to.xml lines 5-6, 18.
  > Verbatim at 269-307: "-- Determines if current subordinate is stuck behind another entity on the road near the final destination / -- by checking how long the subordinate has stopped moving, how close the subordinate is to the unit's end area, / -- and how far the subordinate is from it's personal destination location" ... "if (node.subordinate():getSpeed() == 0 and distToEnd < 20 and distFromGoal > 10) then" ... ...
  For the UNIT-level "Move To" task (ground-vehicle-unit-move-to, dispatched from ground-unit-move-to.lua for an all-vehicle unit, NOT the Maneuver Along/Move Along Route task the interface actually issues), the vendor codes an explicit per-member stuck check: speed 0, within 20 m of the unit's destination area, more than 10 m short of its own assigned spot, sustained 10 simulated seconds.
  QUALIFIER FOUND BY THE VERIFIER: Three neighbouring qualifiers. (1) Early return, lines 280-282: "if (not endArea:isValid()) then return false end" - with no valid endAreaGeometry the stuck check never fires. (2) Scope: the check only fires INSIDE the end area proximity (distToEnd < 20); a member stalled far from the unit's destina

- **moveToTask.h** - C:\MAK\vrforces5.2d\include\vrftasks\moveToTask.h:20-23 (confirmed exact, no correction needed). Corroborating second instance: C:\MAK\vrforces5.2d\include\vrftasks\moveAlongTasks.h:33-37
  > //! Both individual and pseudo-aggregate entities can receive this \n//! task. Individual vehicles stop when they reach the waypoint. For a \n//! pseudo-aggregate, the task is considered to be complete when the lead \n//! subordinate reaches the waypoint.
  Same lead-subordinate-only completion rule documented for the simpler Move To task, reinforcing that this is the vendor's stated general design intent for pseudo-aggregate move tasks, not a one-off comment.
  QUALIFIER FOUND BY THE VERIFIER: Three scope limits a reader must keep. (1) The quote states only a COMPLETION CRITERION for the pseudo-aggregate as a whole; moveToTask.h says NOTHING about what the non-lead subordinates do, whether they move, lag, or stop - not stated. It is therefore not evidence of a member stall. (2) The neares

## Navigation data: what it changes and its limits

- **VR-Forces 5.2 Users Guide** - VR-Forces 5.2 Users Guide, p599, sec 30.23 Maneuver To, first paragraph (location as stated - no correction needed)
  > The Maneuver To task always uses off-road navigation preferences.
  Confirms, from inside my assigned chapter (independent of the PREREG's own 40.54 citation), that at least one vendor unit-movement task unconditionally forces ignore-roads navigation - it is not merely a default that can be overridden for this task.
  QUALIFIER FOUND BY THE VERIFIER: p501 sec 23.2, "Navigation Preferences": "Pathplanningonlyusesroadsiftheentity'sNavigationPreferencesaresettoPreferRoads.Thispreferenceissetinthemovementsystemandcanbechangedwithasetdatarequest(see 40.54.NavigationPreferences on page 890).Wheeled-roadvehiclespreferroadsbydefault,whilewheeled-off-roa

- **VR-Forces 5.2 Users Guide** - docs/vendor/mak-5.2/VR-Forces_5.2_Users_Guide.pdf, p.1276, sec 66.2 "Navigation Areas" (confirmed verbatim). Corroborating Table 54 "Navigation area properties" is on p.1279, sec 66.2.2, NOT p.1276. Qualifier on p.1277, sec 66.2.1.
  > p.1276 sec 66.2: "Navigation areas are rectangular areas that are aligned to the North/South axis. The maximum size of the area on which you can generate navigation data depends on the raster precision value used to generate it. If you reduce the precision of navigation data, creating a sparser graph, the size of the maximum area increases. The maximum size for a navigation area using the default values is 20 km b...
  Navigation areas are rectangular, North/South aligned, and capped at 20x20 km at default raster precision (matches the PREREG's own citation of Table 54, sec 3g). Multiple areas can exist per terrain.
  QUALIFIER FOUND BY THE VERIFIER: p.1277, sec 66.2.1 Sectorizing Navigation Areas: "To generate a maximum size navigation area, you must use multiple sectors." and "The navigation data generation process can fail if any single sector is too large, too complex, or both." Also p.1279 Table 54 note: "If you are creating a large navigat

- **VR-Forces 5.2 Users Guide** - docs/vendor/mak-5.2/VR-Forces_5.2_Users_Guide.pdf, printed p.1279 (PDF page index 1279), Table 54 "Navigation area properties", the "Width / Length" row; the table is inside sec 66.2.2 "Creating a Navigation Area" (step 6 on p.1278 points to it). Location as stated is correct.
  > Width Length - "The length and width of the navigation area. Do not try to create an area larger than 20km by 20km unless you have changed the raster precision to support a larger area." (verbatim, spaces lost by extraction)
  Restates the 20x20 km default cap as an explicit user warning in the property-table notes for Width/Length.
  QUALIFIER FOUND BY THE VERIFIER: Two neighbouring passages qualify rather than refute it. (1) Sec 66.2, p.1276: "Themaximumsizeoftheareaonwhichyoucangeneratenavigationdatadependsontherasterprecisionvalueusedtogenerateit.Ifyoureducetheprecisionofnavigationdata,creatingasparsergraph,thesizeofthemaximumareaincreases.Themaximumsizefora

- **C:\MAK\vrforces5.2d\doc\help\Content\ConceptsEntityLevel\GroundVehMove\vrf_groundVehiclePathPlanningNavMesh.htm** - C:\MAK\vrforces5.2d\doc\help\Content\ConceptsEntityLevel\GroundVehMove\vrf_groundVehiclePathPlanningNavMesh.htm ("Path Planning on the Nav Mesh") - quote is verbatim there, entire body of the topic. The complementary half of the meaning lives in the sibling topic C:\MAK\vrforces5.2d\doc\help\Content\ConceptsEntityLevel\GroundVehMove\vrf_entitymovementonslopesVRF2365.htm ("Slope Effects on Movement"), and the planner taxonomy in vrf_groundVehiclePathPlanning.htm ("Path Planning").
  > Nav-mesh topic (verbatim, as cited): "If a navigation mesh is available, each off-road segment is planned using this nav mesh. The planner takes the costs of different types of soils and the slope of the terrain into account as it plans the path." Immediately following sentence: "If navigation mesh planning is not successful-for example, if there is no navigation mesh in the area-then the segment will be planned a...
  The ONLY vendor path planner that considers slope is the NavMesh planner; without generated navigation data the sim falls back to the feature-obstacle planner, which (per the slope-effects topic above) ignores slope entirely. This is the vendor's own statement of why navdata is the remedy.
  QUALIFIER FOUND BY THE VERIFIER: vrf_entitymovementonslopesVRF2365.htm adds that the nav-mesh slope weighting is tunable, not fixed: "In the movement .sysdef files in ./data/simulationModelSets/EntityLevel/vrfSim/systems/movement , the slope-avoidance-factor in the navigation-preference-controller descriptor can vary the effect." I

## Remedies the vendor names

- **VR-Forces 5.2 Users Guide** - docs/vendor/mak-5.2/VR-Forces_5.2_Users_Guide.pdf, printed page 604 (pypdf page index 604), chapter "30. Movement Tasks for Entity-Level Scenarios", sec 30.28 Move To - as stated, no correction needed
  > Ifyouwantagroundplatformentitytousetheroadnetworktomovetoalocation,firstusetheNavigationPreferencessetdatarequesttoensurethatitissettoPreferRoads.Fordetailsaboutroadmovement,see"23.2.4VehicleMovementOnRoads"onpage 503. [pypdf strips inter-word spaces; verbatim match to the finding's quote]
  Names the Navigation Preferences set-data request (Prefer Roads) as the vendor-documented remedy to force road-network use on an entity's Move To task - a second, independent citation (beyond the PREREG's UG52 40.54) inside the assigned chapter for the same lever.
  QUALIFIER FOUND BY THE VERIFIER: Two bounding conditions in the referenced neighbours, neither refuting: (1) UG52 p503 sec 23.2.4 - "Someterrainshavevectorroadnetworks.Groundvehiclescanusethesenetworkstomovepreciselythroughtheterrain." and "Roaddrivingisvalidonlyforgroundvehicles." So the remedy only bites on terrain that actually 

- **VR-Forces 5.2 Users Guide** - docs/vendor/mak-5.2/VR-Forces_5.2_Users_Guide.pdf, p.1282 (running text of sec 66.3.1 Navigation Profiles, which starts on p.1281); target section is p.1023-1025, sec 47.6 Creating Nav Edges
  > You can dynamically add nav edges to a navigation area using nav edge tactical graphics. For details, see "47.6. Creating Nav Edges" on page 1023.
  The scenario-authoring path to the same nav-edge mechanism: placed as tactical graphics rather than edited in the profile file.
  QUALIFIER FOUND BY THE VERIFIER: The reciprocal cross-reference CONFIRMS the "two paths to the same mechanism" reading rather than refuting it: p.1025, sec 47.6 says "You can also add nav edges in a navigation profile. For details, see \"66.3.1 Navigation Profiles\" on page 1281." The neighbouring qualifier on the profile side call

## Other

- **VR-Forces 5.2 Users Guide** - VR-Forces 5.2 Users Guide, printed page 601 (PDF page 601), sec 30.25 "Move Along Route with Actions", last bullet of the bullet list; corroborated at p603, step 9 of "To assign a Move Along Route with Actions task"
  > "TheAbandonifTimeonTargetCannotBeAchievedoptionisavailablewhenyouuseSpecifyTimeonTarget.Ifyouselecttheoption,thetaskedentityabandonsthetaskifitdeterminesthatitcannotreachthetargetlocationwithin10secondsofthespecifiedtime." (PDF text, spaces stripped by extraction; matches the finding's quote word for word)
  A named vendor 'give up' mechanism for a route task, but it is scoped to missing a scheduled Time-on-Target, not to being physically blocked or stalled mid-route by terrain or another vehicle - it does not apply to the plain Move Along Route task the PREREG's units were given.
  QUALIFIER FOUND BY THE VERIFIER: Nothing in the neighbourhood weakens the claim; two notes. (1) p603 step 9 restates it slightly more broadly - "ThistellsthetaskedentitytogiveuponthetaskifitestimatesthatitcannotcompletethetaskbythetimeyouspecifiedinTimeonTarget(within10seconds)" - "complete the task" rather than "reach the target l

