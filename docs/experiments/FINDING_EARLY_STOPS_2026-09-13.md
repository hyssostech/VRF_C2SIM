# FINDING: what the sim says when 1-35 and 1-6 stop early (2026-09-13)

Answers the question PREREG_ASSEMBLY_LAYOUT_2026-09-07 sections 3f/3g were opened for ("silent
whole-unit stops 2-3 km out"), from the three traces already held: P11 (runs/20260907T150643Z_run,
no navigation area), G2 (runs/20260907T174654Z_run, area + a concurrent load), G3
(runs/20260913T174516Z_run, area, idle machine). Read by an Opus executor (console_freeze.py in the
session scratchpad, reusing the decoder and clock of g2_recompute.py); supervised by Fable 5.1. Tier:
HEAVY - a cause claim; sec 3 is held to "pending" until the two checks in sec 4 return.

## 1. What the sim PRINTED (verbatim shapes; identical in P11, G2 and G3 for 1-35)
Unit 1-35/2/1_A~PXY, task T1_AOA_SE_1-35_AR;_2/1_AD_P1, route of 4 points (V0 = the assembly
point, DROPPED by DropOriginVertexMeters because the unit was spread 2,797 m from it; V1
34.651212/-116.811637; V2 34.596351/-116.952329; V3 34.570294/-117.006223). Members: M1A2 1
(formation leader), M1A2 2, M3 1, HMMWV 1, HMMWV 2, M577A2 1.
- sim 52-66: UNIT "move-along-controller beginning to process move-along task (ID=0)" ->
  "maneuver-along-controller beginning to process maneuver-along subtask (ID=1)" -> parameters
  (unitRoute = the route, speed=0, startAtClosest=False) -> "Setting navigation preference to
  ignore-roads" -> "Rules of engagement: hold-fire". Then the unit console prints NOTHING for the
  rest of the run (2,500-6,200 sim-seconds): no "Subs still moving", no Completed, no Failed.
- each MEMBER: "maneuver-in-formation ... leader=M1A2 1; formationLength=60; startingVertex=0" ->
  "script-controller beginning to process ground-vehicle-move-to subtask (ID=1)" with a destination
  at its formation slot near birth -> "Not using roads for move planning" / "Job Plan path success"
  / "Planned path has 1 parts" -> turn-to-heading 263 deg -> "Saving ordered speed 3 mps" ->
  "Task Turn to route success" -> at sim 88-110 a SECOND ground-vehicle-move-to whose destination
  is route vertex V1 plus the member's offset -> "move-along beginning to process move-along
  subtask" on its own generated offset route ("Move-Along Route: Route 2/43/60/...") -> "Planned
  path has 1 parts". That second goal is the LAST goal any member ever receives, in all three runs.
- G3 only, sim 124.8: "New Primary nav area: NavArea-ground-platform MojaveAO20" (every member).
- from sim ~125 to the END of every run, each member repeats ONLY these rows at ~1 Hz:
  "Starting sequence node Maybe plan path" / "Starting condition node Goal new or changed?" /
  "Condition false." / "Starting condition node Is path blocked?" / "Starting sequence node Maybe
  Skirt Blockage" / "Condition false." / Status of task "move-along" is "TaskRunning" (14,624
  such rows per member in G3). Message shapes still emitted after sim 200: P11 7 of 147, G2 7 of
  156, G3 7 of 146 - exactly this list.
- The consoles do NOT contain, for 1-35 in any run: BlockedByVehicle, BlockedByWall, "Movement
  stopped by vehicle", "subtask has Failed", "Global Replan", "Entity not embarked", Stopped,
  Waiting, "Subs still moving", any nav-area transition after sim 129, any slope / impassable /
  obstacle / soil message, any speed change after "Saving ordered speed 3 mps", or any row from
  the unit-level controller. The sim never reports failure and never reports arrival; it reports
  the move as running, unblocked and with an unchanged goal, forever.
- The interface's own log for 1-35 ends at dispatch (proxy type -> PLACEMENT -> MATERIALIZE ->
  console levels -> "dropped 1 leading route point(s)" -> "terrain profile 199 ... alts [1248.3,
  1341.1, 964.4, 1045.8]" (heights at the FOUR ROUTE VERTICES only) -> CreateRoute (4 pts) ->
  MoveAlongRoute issued). No arrival check fires, no second task, no refusal, no error.

## 2. Where they stop (POS rows; altitude present and varying, not a placeholder)
| run | member | birth | frozen at | net m | to V1 m | altitude |
|---|---|---|---|---|---|---|
| P11 | M1A2 1 | 34.65844,-116.74009 | 34.65608,-116.76142 | 1,968 | 4,625 | ~1,590 |
| G2 | M1A2 1 | same | 34.65616,-116.76002 | 1,840 | 4,754 | ~1,590 |
| G3 | M1A2 1 | same | 34.65608,-116.76142 | 1,969 | 4,625 | ~1,590 |
| G3 | other five | - | 34.6556-34.6566, -116.7610 to -116.7621 | 1,898-2,226 | 4,386-4,664 | - |
Mid-leg, not at a vertex (the nearest vertex is 4.2 km away); all three runs stop within ~150 m of
the same ground. Displacement grows at ~9-10 m/s of sim to ~1,950 m by sim 300-360 while altitude
climbs 1,238 -> ~1,590 m (+350 m over 2.0 km; ~25 % over the last leg), then stops. After the stop
the LEADER M1A2 1 has net movement 15 m but an integrated path of 861 m in G3 (1,735 m over 6,000
sim-seconds in P11) inside a 23 m box, altitude cycling 1,579-1,596 with a ~25 s period; M1A2 2,
M3 1 and M577A2 1 are stationary (0-2 m). POS sampling continues to the end (679 / 2,025 samples
after the stop) - not a telemetry dropout.
1-6/2/1_AD in G3 (task T15; V1 34.609047/-116.803322): the identical pattern - two goals per
member, the last at sim 72-116 pointing at V1, then only the seven-row loop; frozen 3,131-3,485 m
from birth, 5,276-5,596 m short of V1, altitude 1,570-1,643 m.

## 3. The two units that DID enter the vendor's failure path (contrast, same traces)
- P11's 1-6: only HMMWV 8 cycles Status "BlockedByVehicle" (76 rows, sim 89-795) -> "Movement
  stopped by vehicle; will be blocked at time 752.69" -> "move-along's subtask has Failed" (6
  rows) -> "Task Move along route fail; sub. HMMWV 8" -> "Attempt Global Replanning Once" ->
  "Global Replan" -> "Entity not embarked on same object as target [Route 28]. Ending task" -> a
  new ground-vehicle-move-to -> "Planned path has 1 parts" -> blocked again; once
  "pathUtilMakePointsClampedToTerrain(): waiting for terrain page." (sim 784). The UNIT printed one
  row after the start: ".  Subs still moving: 5" at sim 796. HMMWV 8 received 4 goals; the other
  five 1-2 (last at sim 53-95) and stopped at 2,403-3,272 m.
- G2's 1-6 (read by wall because sim froze): at sim 317-320 M3 4 and M1A2 19 "move-along's subtask
  has Failed" + "Entity not embarked ... [Route 54/55]"; sim 320.43 "M1A2 19: maneuver-in-formation's
  task has Failed (ID=0)" - the original leader; the other five are re-tasked "maneuver-in-formation:
  unitRoute=M1A2 20's Offset Route; leader=M1A2 20" and reach 6.2-6.4 km; M1A2 19 stays at 2,868 m;
  M3 4 gets 6 goals (one "Planned path has 970 points") and still never passes 2,900 m.
- A healthy unit, G3's 1-1/2/1_AD: its members cycle a CLOSED loop every 10-20 sim-seconds -
  "move-along's subtask has Completed" -> "script-controller's subtask has Completed" -> a new
  ground-vehicle-move-to (18 goals total, last at sim 2202-2222) -> "Planned path has 1 parts" ->
  "move-along beginning" - while displacement grows monotonically to 21,575 m by sim 2563 and
  altitude falls 1,281 -> 930. Its unit console is alive too (weapon-list heartbeat ~every 100 s);
  in P11 it completes: "maneuver-along-controller's subtask has Completed" at sim 2754.6, then
  "Subs still moving: 3/2/1/0".

## 4. Reading - PENDING two checks (an executor is running them; sec 5 records the outcome)
The sim printed no cause. The reading that fits every printed row and every position is: the
members are physically unable to make headway on the climb they reach ~2 km out, while the movement
layer still believes the path is clear and the goal unchanged, so no failure branch and no re-plan
is ever entered - which is why the run has no diagnostic to show. Vendor text this rests on:
UG52 23.5.2 p508 "vehicles can just barely move up the max-slope defined in the entity parameters
by using maximum throttle. It is possible that vehicles may slide down slopes"; ground-vehicle-
move-to.lua 219-264 - "blocked" is exactly the subtask status BlockedByWall or BlockedByVehicle
(10 s), nothing else, so a slope stall triggers none of the skirt/backup/replan machinery (DOCSWEEP
item, verified); UG52 23.5 - the feature-obstacle planner ignores slope. The leader's 861 m of
back-and-forth inside 23 m with the altitude cycling is the "barely move / slide down" signature.
CHECKS THAT DECIDE IT (registered before their result is known):
- C1: the vehicles' OWN max-slope parameter (entity parameter files) vs the grade under the stop.
  Grade >= max-slope -> SUPPORTS; grade well below -> REFUTES slope as sufficient (soil or something
  else), and soil is UNMEASURED (the terrain-profile reply carries soilType but the facade drops it).
- C2: why the mesh did not route around it in G3: 1-35's V1 (-116.8116) lies ~230 m OUTSIDE the
  area's west edge (-116.80914), and Migration Guide 2.4 p18 makes the mesh planner conditional on a
  mesh "from the start of the entity's movement path to the end"; for 1-6 (V1 inside) the profile's
  slope-max is 46 deg (navigationProfiles.mtl:246), far above any tank's climbing limit, so a
  mesh-legal path can still exceed the vehicle's max-slope (competing hypothesis H2 of
  PREREG_NAVDATA_2026-09-07 sec 5, now the live candidate). To be quoted and measured.
Competing hypotheses, named: (a) soil/traction rather than slope (UG52 23.5 Table 26; unmeasured);
(b) a formation or speed-control wait - REFUTED by the consoles: no wait state is printed, the
leader itself is the one grinding, and the followers are 0-2 m still because they follow it;
(c) a task-chain or predecessor wait in the interface - REFUTED: the app log shows one task, no
predecessor, no arrival check, nothing after dispatch; (d) a terrain-paging wall - one "waiting for
terrain page" row exists in P11 for HMMWV 8 (a different unit, a different fault path); none for
1-35 in any run.
UNEXPLAINED, recorded: G3's HMMWV 2 crept 271 m after the freeze while four peers moved 0-2 m; G2's
M3 4 received six goals and still did not move; G2's 1-6 leader had its maneuver-in-formation task
Failed at sim 320.4 when the same unit in P11 and G3 never failed at all.

## 5. Check results (Opus executor, 2026-09-13 ~19:30Z) - THE SLOPE READING OF SEC 4 IS REFUTED AS STATED
C1 - the vehicles' own limits (movingObjectParameters.h:294-310: max-slope is rise-over-run, "the
maximum slope at which the entity can still have acceleration >= 0"): M1A2_Abrams_MBT.entity:300
0.94 (43.2 deg); M3 via parent M2A2_Bradley_IFV.entity:358 0.94; HMMWV (M998:168 / M1025:227) 1.0
(45 deg); M577A2_Command_Post.entity:47 1.0. Soil derates it (navigationPreferenceDescriptor.h:
125-127: multiplied by the soil acceleration-factor; ground-tracked.sysdef:787 rocks/sand 0.80,
muck 0.40, deep-water 0). The nav-mesh profile slope-max is 46 deg (navigationProfiles.mtl:246,
units degrees per lines 50-52) - STEEPER than any of these vehicles can climb: the shipped mesh
marks as traversable ground an Abrams cannot drive (a real vendor-configuration gap, recorded, but
not what stopped these units - see C2).
C2 - the grade under the stop, from the POS altitude (live, not a placeholder), resampled along
path: 1-35's leader (G3) birth 1239 m -> freeze 1586 m, path 2,449 m, climb +346 m; max 100 m
grade in the last 500 m +0.674 (34 deg) at the CREST (alt 1604, sim 331); grade AT the stop -0.010
(G3) / +0.030 (P11); steepest 20 m surmounted +0.896 (41.9 deg) = 95 % of the limit; after the
stop the leader sits in a 3 m ball (alt 1583.7-1586.6) for the rest of the run. P11 freezes at the
same point to ~1 m. 1-6's leader (G3): climb +408 m, max grade last 500 m +0.292, grade at the
stop +0.101, steepest surmounted +0.693. CONTRAST 1-1's leaders (same run, same type) passed
361-410 m from 1-35's freeze point at 1574-1600 m and drove 24 km, surmounting +0.713. The
remaining leg ahead of each freeze is net DOWNHILL (-0.055 for 1-35 to V1; -0.086 for 1-6).
VERDICT: the stop is not on a grade the vehicles cannot climb; they froze on near-level ground just
past a crest after climbing steeper pitches. Slope as the sufficient cause is REFUTED.
C3 - the area edge: 1-35's V1 is 229 m OUTSIDE the area, 1-6's V1 533 m INSIDE, 1-1's V1 272 m
OUTSIDE (further than 1-35's) - and 1-1 ran 24 km while 1-6 froze. Inside/outside does not
partition frozen from moving units. The mesh gate is isPathPartOutsideNavArea (ground-vehicle-
move-to.lua:478-483: either endpoint outside -> no mesh planning for that part); "Planned path has N
parts" (lua:1398) = parts of the feature path (on-feature/off-feature), a different message from
the mesh's "Planned path has N points" (lua:511); neither survives at the captured console level.
Discrepancy between the two executors, recorded: the console read gave the leader "max excursion
23 m, integrated path 861 m" after the stop; the grade check gives "a 3 m ball". Same member, same
run; the metric or the window differs; not resolved here.
REVISED STATE: CAUSE OPEN. What is now VERIFIED: (i) all three runs freeze on the same near-level
ground just past a crest at ~1,590 m, 2 km out; (ii) the movement layer reports running /
unblocked / goal unchanged forever and enters no failure branch; (iii) the vehicles had just
climbed 41-42 deg pitches, 95 % of their limit; (iv) the leader keeps shuffling inside ~23 m
horizontally with its reported altitude spanning 1,579-1,596 m (G3 console read; REPRODUCED in
G5: span 16.6 m, damping over time; the ~25 s period is NOT measurable in G5 - aliasing at 12.4
sim-s per sample); the altitude tracks the longitude across the shuffle (~0.78 rise-over-run over
16 m, an executor's inference from POS rows) - a steep local face metres from where its
companions sit still, which a 100 m grade resample cannot see; (v) a same-type unit crossed the ridge 400 m away without stopping. UNMEASURED:
soil class at the stop (the facade drops soilType); terrain relief at metre scale around and
ahead of the stop (the 90 m worldwide elevation of MAK Earth (online) can hold a stair-step at a
crest); whether the mesh's polygons cover the freeze point; the controller's COMMANDED speed and
throttle at the stop, which is what separates "cannot move" from "chose to stop".
NEXT DISCRIMINATOR (cheapest first; registered as PREREG_EARLYSTOP_G5_2026-09-13.md): a 10-minute
run of the same scenario with a PROBE order holding only 1-35's first task, so only 1-35
materializes, with its six member consoles at the vendor's DEBUG level 4 (UG52 21.9.1) - the
freeze happens by sim ~360, i.e. ~4 minutes of wall at 1.6x. [G5 RAN 19:00Z: the unit ALONE froze
at the same point to ~1 m - interaction refuted; the debug stream is the BT tick trace parked in
move-along; harvest in progress.]
USER DIRECTIVE 2026-09-13 ~19:40Z, before any further probe: "there is nothing special in what you
are trying to achieve. No random walks - read the vendor documentation, samples, community
insights instead." Accordingly the path-shift run and the terrain-grid probe tool I had proposed
are PARKED; the next executor is a docs / sample / community reader on the ordinary question "a
VR-Forces ground vehicle stops mid-route while its task runs; what stops a vehicle on a patch of
terrain" - the vendor's decideToGiveUpTask sample, UG52 ch 23 in full (soil Table 26, slope,
paging, clamping), the terrain's surface-characteristics mapping files (landCoverDataSurfChar.map,
smc_fid_soiltype_map.csv) read at the freeze point offline, and the public record. Sec 6 holds
its result.

## 6. The vendor's answer (docs / sample / community pass, Opus reader, 2026-09-13 ~20:10Z)
(a) SAMPLE - C:\MAK\vrforces5.2d\examples\decideToGiveUpTask (derivedMoveAlongController.h:18-25):
"The purpose of this class is to demonstrate how to check for and handle a situation in which an
entity is unable to carry out a given task behavior ... determine when it should 'give up' trying
to reach its waypoint." The shipped test is a 10 s timer (.cxx:48-62); giveUpTask() calls
taskComplete() "which results in a task complete report being sent and the controller put into a
non-tasked state" (.h:35-36); it is installed as a sim-side PLUGIN replacing the stock creator
(plugin.cxx:29-30, addCreatorFcn(DtGroundAutoMoveAlongType, ...)). The BASE CONTRACT
(vrfobjcore/singleTaskControllerComponent.h:192-205): "Override this function to provide the test
for determining if the task controller should give up. For example, a derived move-to-waypoint
controller tasked to reach an unreachable waypoint might decide to give up after a certain amount
of time, or after circling the point and failing to reach it. ... The implementation in this class
always returns false." DtGroundMoveAlongControllerComponent declares an override
(groundMoveAlongControllerComponent.h:160) with no doc and no shipped source - what it tests is
NOT STATED. ground-vehicle-move-to.lua has NO speed, distance or progress watchdog; its only exits
are BlockedByWall, BlockedByVehicle held 10 s (:219-264, :756-760), or completion. "stuck" appears
ZERO times in UG52. VERDICT: a ground vehicle that stops making progress while its task runs is
UNDETECTED BY DESIGN in VR-Forces 5.2; the vendor's sample hands the detection to the integrator.
(b) WHAT PHYSICALLY STOPS A VEHICLE (UG52 23.5.1 p507 = vrf_movementAndSoilType.htm): the soil
acceleration-factor "0.0 means the surface's drag prevents the vehicle from moving at all (deep
water)"; the only roughness rated 0 in ground-tracked.sysdef is deep-water (:813-816), muck is
0.4/0.6 (:817-820), sand 0.80/0.75 (:805-808), rocks 0.80/0.90. Slope (23.5.2 p508): "vehicles can
just barely move up the max-slope defined in the entity parameters by using maximum throttle. It
is possible that vehicles may slide down slopes, especially if the soil is slippery" - and
navigationPreferenceDescriptor.h:125-127: the effective max-slope "is reduced by the soil
modifier (i.e. multiplied by the acceleration-factor)". Trapping (23.2.3 p502): "the vehicle could
become trapped either by a very large alley or by moving entities that close off its path".
Paging (63.6.1 p1247): terrain loads on demand, moving objects' pages are high priority; what a
moving entity does on terrain not yet streamed is NOT STATED anywhere; vrfSim.mtl has no paging
keys (they are in terrainInterfaceConfig.mtl:19,76,144-156). Clamping (63.7 p1248) is never tied
to movement failure.
(c) THE SOIL UNDER THE STOP, READ OFFLINE WITH CONTROLS: MAK Earth (online) has no local
soil raster; its land cover is a streamed composite (biomes.landcover.coverage.online.xml) whose
tiles on vr-theworld.com are publicly fetchable (EPSG:4326 TMS). At 34.65607/-116.76144 the
highest-resolution layer with data, CA FVEG 15 m (tileset 154), reads 30 = "Sagebrush" ->
soiltype BM_SAND (layer.CA-FVEG.15m.online.xml:38) -> landCoverDataSurfChar.map:317 -> sand ->
acceleration 0.80 / stopping 0.75. NLCD 30 m reads 52 Shrub/Scrub (BM_VEGETATION-BRUSH),
Copernicus 100 m reads 30 Herbaceous (BM_LAND-GRASS). Controls: Pacific -> Ocean/Open Water,
downtown LA -> Urban/Developed, Lake Tahoe -> water/Lacustrine, all correct. Where 1-1 crossed
400 m away: FVEG 60 "Desert Scrub" -> BM_SAND too. SOIL AS AN OUTRIGHT STOP IS REFUTED (sand, not
deep water or muck). The generated nav area carries no soil file, and the ground-platform profile
tags only road/pavedroad (navigationProfiles.mtl:261-264), so the mesh never knew about sand.
(d) COMMUNITY: nothing public on the symptom; docs.mak.com classref 404s for these classes; MAK's
5.2 announcement only says vehicles "use the MAK Behavior Tree System" and "ground path planning
is enhanced with vector-based terrain data". The support portal is the vendor channel (not used).
(e) SILENT: what DtGroundMoveAlongControllerComponent's give-up override tests; entities on
unstreamed terrain; page-in areas on osgEarth terrain; any stall-while-running mechanism; any
progress watchdog; what happens at 95 % of max-slope on lower-traction soil.

REVISED MECHANISM - A HYPOTHESIS, cited, with its falsifier (sec 7 records the test):
SAND-DERATED MAX-SLOPE EXCEEDED BY A METRE-SCALE FACE. The M1A2's max-slope is 0.94; on sand it is
0.94 x 0.80 = 0.752 (navigationPreferenceDescriptor.h:125-127). The G5 leader's own track across
its 23 m shuffle reports 12.6 m of altitude over 16.1 m of ground - a local face of ~0.78, above
0.752 - while a 100 m resample of the same track reads the stop as level (sec 5 C2). UG52 23.5.2
predicts exactly the observed behaviour at that limit: "just barely move up ... may slide down
slopes" = the leader's 318 m of back-and-forth along the fall line; the four still followers are
the formation's speed control holding station on their leader (UG52 30.22 p598); the movement
layer prints nothing because nothing in it tests progress (sec 6a). The 90 m elevation posting of
MAK Earth (online) is where a 12 m face over 16 m plausibly comes from (a cell edge), and a
different cell edge 400 m away lets 1-1 through - consistent with the determinism (data-driven)
and with the contrast. FALSIFIER: the elevation data at the freeze point, read at its native
posting and interpolated as the sim does, shows NO local gradient >= 0.752 along the leader's
heading within its 23 m shuffle line - then the face is not in the terrain and the mechanism
fails; a second test at 1-6's G3 stop (34.64236/-116.75907, sand?) and P11's 2.85 km stop must
show the same signature or the hypothesis is weakened.
Adversarial review: competing account (1) "commanded to stop" - weakened, not excluded, by the
leader's continuous shuffle (a commanded stop sits still, as M3 1 does) and by the ordered speed
of 10 m/s never rescinded; the reflected entity velocity would settle it. (2) A terrain-streaming
gap under the vehicle - not excluded by any doc (silent), but the freeze reproduces to ~1 m in
four runs at very different wall clocks (1.5x and 6.2x), which a transient page would not do.
(3) An obstacle trap (23.2.3) - the avoider's trap prints BlockedBy statuses; none appear.
Unexplained, still: HMMWV 2's 90 m creep to sim 1,270; G2's 1-6 leader task failure at sim 320.

## 7. The elevation test (registered before its result; executor running)
(filled when the executor returns)
