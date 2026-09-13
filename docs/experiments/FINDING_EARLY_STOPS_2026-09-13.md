# FINDING: what the sim says when 1-35 and 1-6 stop early (2026-09-13)

Answers the question PREREG_ASSEMBLY_LAYOUT_2026-09-07 sections 3f/3g were opened for ("silent
whole-unit stops 2-3 km out"), from the three traces already held: P11 (runs/20260907T150643Z_run,
no navigation area), G2 (runs/20260907T174654Z_run, area + a concurrent load), G3
(runs/20260913T174516Z_run, area, idle machine). Read by an Opus executor (console_freeze.py in the
session scratchpad, reusing the decoder and clock of g2_recompute.py); supervised by Fable 5.1.
Tier: HEAVY - a cause claim. Status: the cause claim of sec 7 STANDS WITH FIXES after an independent
cold-start re-derivation (2026-09-13 ~21:00Z); secs 4-6 are kept as the audit trail with their
superseded sentences marked.

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
  obstacle / soil message, any speed change after "Saving ordered speed 3 mps", or any row from the
  unit-level controller. The sim never reports failure and never reports arrival; it reports the
  move as running, unblocked and with an unchanged goal, forever. [SUPERSEDED for G5: at the vendor
  DEBUG level the leader's ordered-speed sequence continues 3 -> 8 -> 3 -> 10 m/s, the last at sim
  ~38-56; sec 1 describes the level-3 captures of P11/G2/G3 only.]
- The interface's own log for 1-35 ends at dispatch (proxy type -> PLACEMENT -> MATERIALIZE ->
  console levels -> "dropped 1 leading route point(s)" -> "terrain profile 199 ... alts [1248.3,
  1341.1, 964.4, 1045.8]" (heights at the FOUR ROUTE VERTICES only) -> CreateRoute (4 pts) ->
  MoveAlongRoute issued). No arrival check fires, no second task, no refusal, no error. [NOTE
  2026-09-13 review: these logged heights are uniformly +10.0 m above the surface the sim clamps to
  (L13 bilinear: birth 1239.2, V1 1331.1, V2 954.4, V3 1035.8) - an unexplained systematic in the
  route-authoring path, recorded.]

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
sim-seconds in P11) [WITHDRAWN - aliased series; see sec 5] inside a 23 m box, reported altitude
spanning 1,579-1,596 m (the span REPRODUCED in G5 with damping; the "~25 s period" of this first
read is NOT confirmed - G5 could not resolve it); M1A2 2, M3 1 and M577A2 1 are stationary (0-2 m).
POS sampling continues to the end (679 / 2,025 samples after the stop) - not a telemetry dropout.
1-6/2/1_AD in G3 (task T15; V1 34.609047/-116.803322): the identical pattern - two goals per member,
the last at sim 72-116 pointing at V1, then only the seven-row loop; frozen 3,131-3,485 m from
birth, 5,276-5,596 m short of V1, altitude 1,570-1,643 m.

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
The sim printed no cause. The reading that fits every printed row and every position is: the members
are physically unable to make headway on the climb they reach ~2 km out, while the movement layer
still believes the path is clear and the goal unchanged, so no failure branch and no re-plan is ever
entered - which is why the run has no diagnostic to show. Vendor text this rests on: UG52 23.5.2
p508 "On the best soil surface (dry pavement), vehicles can just barely move up the max-slope
defined in the entity parameters by using maximum throttle. It is possible that vehicles may slide
down slopes"; ground-vehicle-move-to.lua 219-264 - "blocked" is exactly the subtask status
BlockedByWall or BlockedByVehicle (10 s), nothing else, so a slope stall triggers none of the
skirt/backup/replan machinery (DOCSWEEP item, verified); UG52 23.5 - the feature-obstacle planner
ignores slope. The leader's 861 m of back-and-forth inside 23 m with the altitude cycling is the
"barely move / slide down" signature. [The 861 m is WITHDRAWN - aliased series; see sec 5.]
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
(b) a formation or speed-control wait - no wait state is printed and the leader itself is the one at
the face; but 30.22's speed control is MUTUAL, so this is NOT EXCLUDED (sec 7 adversarial review
item 5);
(c) a task-chain or predecessor wait in the interface - REFUTED: the app log shows one task, no
predecessor, no arrival check, nothing after dispatch; (d) a terrain-paging wall - one "waiting for
terrain page" row exists in P11 for HMMWV 8 (a different unit, a different fault path); none for
1-35 in any run.
UNEXPLAINED, recorded: G3's HMMWV 2 crept 271 m after the freeze while four peers moved 0-2 m; G2's
M3 4 received six goals and still did not move; G2's 1-6 leader had its maneuver-in-formation task
Failed at sim 320.4 when the same unit in P11 and G3 never failed at all.

## 5. Check results (Opus executor, 2026-09-13 ~19:30Z) - the 100 m-resample refutation, SUPERSEDED by sec 7
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
path: 1-35's leader (G3) birth 1239 m -> freeze 1586 m, path 2,449 m, climb +346 m; max 100 m grade
in the last 500 m +0.674 (34 deg) at the CREST (alt 1604, sim 331); grade AT the stop -0.010 (G3) /
+0.030 (P11); steepest 20 m surmounted +0.896 (41.9 deg) = 95 % of the limit; after the stop the
leader sits in a 3 m ball (alt 1583.7-1586.6) for the rest of the run. P11 freezes at the same point
within 3 m. 1-6's leader (G3): climb +408 m, max grade last 500 m +0.292, grade at the stop +0.101,
steepest surmounted +0.693. CONTRAST 1-1's leaders (same run, same type) passed 361-410 m from
1-35's freeze point at 1574-1600 m and drove 24 km, surmounting +0.713. The remaining leg ahead of
each freeze is net DOWNHILL (-0.055 for 1-35 to V1; -0.086 for 1-6).
[SUPERSEDED by sec 7: this refutation was of a 100 m resample, which cannot see a 55 m face; the
grade AHEAD of the stop, not AT it, is the quantity that matters - the leader sits at the TOE of the
face, on the last level postings before it.]
C3 - the area edge: 1-35's V1 is 229 m OUTSIDE the area, 1-6's V1 533 m INSIDE, 1-1's V1 272 m
OUTSIDE (further than 1-35's) - and 1-1 ran 24 km while 1-6 froze. Inside/outside does not
partition frozen from moving units. The mesh gate is isPathPartOutsideNavArea (ground-vehicle-
move-to.lua:478-483: either endpoint outside -> no mesh planning for that part); "Planned path has N
parts" (lua:1398) = parts of the feature path (on-feature/off-feature), a different message from
the mesh's "Planned path has N points" (lua:511); neither survives at the captured console level.
Discrepancy between the two executors, recorded: the console read gave the leader "max excursion
23 m, integrated path 861 m" after the stop; the grade check gives "a 3 m ball". Same member, same
run; the metric or the window differs; not resolved here. RESOLVED 2026-09-13 review: the POS series
after the stop is ALIASED (12.4 sim-s per sample in G5, sign alternating), so no path length can be
integrated from it; the figures 861 m, '3 m ball' and 318 m are artefacts of three windows and are
WITHDRAWN. What stands: the leader never comes to rest - a ~2 m limit cycle persists for 480 s.
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
materializes, with its six member consoles at the vendor's DEBUG level 4 (UG52 21.9.1) - the freeze
happens by sim ~360, i.e. ~4 minutes of wall at 1.6x. [G5 RAN 19:00Z: the unit ALONE froze at the
same point within 3 m - interaction refuted; the debug stream is the BT tick trace parked in
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
entity is unable to carry out a given task behavior ... determine when it should 'give up' trying to
reach its waypoint." The shipped test is a 10 s timer (.cxx:48-62); giveUpTask() calls
taskComplete() "which results in a task complete report being sent and the controller put into a
non-tasked state" (.h:35-36); it is installed as a sim-side PLUGIN replacing the stock creator
(plugin.cxx:29-30, addCreatorFcn(DtGroundAutoMoveAlongType, ...)). The BASE CONTRACT
(vrfobjcore/singleTaskControllerComponent.h:192-205): "Override this function to provide the test
for determining if the task controller should give up. For example, a derived move-to-waypoint
controller tasked to reach an unreachable waypoint might decide to give up after a certain amount of
time, or after circling the point and failing to reach it. ... The implementation in this class
always returns false." DtGroundMoveAlongControllerComponent declares an override
(groundMoveAlongControllerComponent.h:160) with no doc and no shipped source - what it tests is NOT
STATED. ground-vehicle-move-to.lua has no progress watchdog: its only speed test is the
stop-before-replan precondition ('Is Vehicle Stopped?', this:getSpeed() == 0.0, :1350-1353) and its
only abort counter is MAX_REPLANS = 3 (:47, 'Attempts to replan and move before deciding it is
permanently stuck and aborting'), which counts blockage- or destination-driven replans; neither
fires on lack of progress. Its exits are BlockedByWall, BlockedByVehicle held 10 s (:219-264,
:756-760), or completion. "stuck" appears ZERO times in UG52. VERDICT: a ground vehicle that stops
making progress while its task runs is UNDETECTED BY DESIGN in VR-Forces 5.2; the vendor's sample
hands the detection to the integrator.
(b) WHAT PHYSICALLY STOPS A VEHICLE (UG52 23.5.1 p507 = vrf_movementAndSoilType.htm): the soil
acceleration-factor "0.0 means the surface's drag prevents the vehicle from moving at all (deep
water)"; the only roughness rated 0 in ground-tracked.sysdef is deep-water (:813-816), muck is
0.4/0.6 (:817-820), sand 0.80/0.75 (:805-808), rocks 0.80/0.90. Slope (23.5.2 p508): "On the best
soil surface (dry pavement), vehicles can just barely move up the max-slope defined in the entity
parameters by using maximum throttle. It is possible that vehicles may slide down slopes, especially
if the soil is slippery" - and navigationPreferenceDescriptor.h:125-127: the effective max-slope "is
reduced by the soil modifier (i.e. multiplied by the acceleration-factor)". [NOTE per the 2026-09-13
review: this sentence belongs to the nav-mesh path-cost formula and calls itself an estimate; it is
used here as an analogy for the dynamics, not as their specification] Trapping (23.2.3 p502): "the
vehicle could become trapped either by a very large alley or by moving entities that close off its
path". Paging (63.6.1 p1247): terrain loads on demand, moving objects' pages are high priority; what
a moving entity does on terrain not yet streamed is NOT STATED anywhere; vrfSim.mtl has no paging
keys (they are in terrainInterfaceConfig.mtl:19,76,144-156). Clamping (63.7 p1248) is never tied to
movement failure.
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
0.94 x 0.80 = 0.752 (navigationPreferenceDescriptor.h:125-127). The G5 leader's own track across its
23 m shuffle reports 12.6 m of altitude over 16.1 m of ground - a local face of ~0.78, above 0.752 -
while a 100 m resample of the same track reads the stop as level (sec 5 C2). UG52 23.5.2 predicts
exactly the observed behaviour at that limit: "just barely move up ... may slide down slopes" = the
leader's persistent limit cycle at the toe; the four still followers are the formation's speed
control holding station on their leader (UG52 30.22 p598); the movement layer prints nothing because
nothing in it tests progress (sec 6a). [REFUTED in sec 7: the face is not a 90 m cell edge. The
surface here is the streamed 10 m California inset (8 m posting); the face is coherent relief
present at L11 (0.856), L12 (0.888) and L13 (0.858) and across 60 m of ridge frontage; the 90 m
index is only a line-of-sight pre-check (max_lod 7). The determinism and the 400 m contrast are
explained by each unit's straight line meeting a different part of the ridge.] FALSIFIER: the
elevation data at the freeze point, read at its native posting and interpolated as the sim does,
shows NO local gradient >= 0.752 along the leader's heading within its 23 m shuffle line - then the
face is not in the terrain and the mechanism fails; a second test at 1-6's G3 stop
(34.64236/-116.75907, sand?) and P11's 2.85 km stop must show the same signature or the hypothesis
is weakened.
Adversarial review: competing account (1) "commanded to stop" - weakened, not excluded, by the
leader's continuous shuffle (a commanded stop sits still, as M3 1 does) and by the ordered speed of
10 m/s never rescinded; the reflected entity velocity would settle it. (2) A terrain-streaming gap
under the vehicle - not excluded by any doc (silent), but the freeze reproduces within 3 m in four
runs at very different wall clocks (1.5x and 6.2x), which a transient page would not do. (3) An
obstacle trap (23.2.3) - the avoider's trap prints BlockedBy statuses; none appear.
Unexplained, still: HMMWV 2's 90 m creep to sim 1,270; G2's 1-6 leader task failure at sim 320.

## 7. The elevation test (registered in sec 6 before its result; Opus executor, elev_face.py, ~20:40Z)
THE SURFACE THE SIM READS: MAK Earth (online).earth line 20 includes ONE elevation layer,
elevation.worldwide.online.xml:24-35 - TMSElevation "Elevation MAK Earth", tiles from
http://vr-theworld.com/vr-theworld/tiles/1.0.0/149/ (257x257 float32 TIFF, EPSG:4326, orders 0-19),
with the local 90 m "VRTW-Worldwide-90m ... index.bin" only as a max_lod 7 line-of-sight
pre-check (+/-500 m tolerance) - NOT the surface under the vehicles. Over this area the tileset's
best DataExtent is maxlevel 13 (the California 10 m inset, bbox -117.0006..-115.9994 /
33.9994..35.0006): posting 7.86 m E-W x 9.56 m N-S at 34.66 N. Interpolation: the .earth loads
options.terrain-rex-default.xml, which sets no elevation_interpolation -> osgEarth default
bilinear (triangulate is set only in the sibling options.terrain-default.xml, not included).
INSTRUMENT VALIDATION: the L13 sampler reproduces the sim's reported altitudes - 1-35 freeze
1585.61 vs 1585.50; 1-6 birth 1208.45 vs 1208.50; 1-1 ridge 1574.14 vs 1574.10; over 129 POS
samples on three vehicles the residual median is +0.03 m (range -0.06 / +0.31). A Pacific control
gives -4,251.9 m (bathymetry). Tile seams match 257/257. The sim is reading this dataset at this
level, and the vehicles are clamped to it.
NATIVE POSTINGS through the 1-35 freeze (E-W row, walking WEST = the direction of travel, heading
263.2 deg): 1582.85, 1588.38, 1595.07, 1602.19, 1609.17, 1615.84, 1622.62, 1630.07 m at 7.86 m steps
= rise-over-run 0.70, 0.85, 0.91, 0.89, 0.85, 0.86, 0.95 - seven consecutive postings, 55 m of face
at 35-43 deg. Max along-heading over 5 / 10 / 20 m windows: 1.003 / 0.960 / 0.933. The leader's
post-arrival excursion decays from 22.8 m (sim 80-180) to a persistent ~2.0 m limit cycle by sim 380
(altitude span 16.6 m, then 1.4 m); its westernmost reach, -116.761689 at 1,605.0 m (a first
overshoot at t=76.9), is 4.5 native postings up the face and the furthest it ever gets; the approach
sample -116.761066 / 1,564.2 m (t=74.9) is BELOW the toe. Both are reproduced by the sampler to
~1 m. The "12.6 m over 16.1 m" of sec 6 is exactly the native pair 1582.85 -> 1595.07 (0.777) - a
real face in the source data, not an interpolation artefact.
| point | run | heading | native postings ahead (rise/run) | max 5/10/20 m | limit (0.94 x sand 0.80) | verdict |
|---|---|---|---|---|---|---|
| A 1-35 M1A2 1 freeze 34.656073/-116.761444 | G5 | 263.2 | 0.70 0.85 0.91 0.89 0.85 0.86 0.95 | 1.003 / 0.960 / 0.933 | 0.752 | SUPPORTS |
| B1 1-6 M1A2 19 freeze 34.642372/-116.759057 | G3 | 227.5 | 0.882 0.929 0.874 0.693 | 0.856 / 0.851 / 0.841 | 0.752 | SUPPORTS |
| B2 1-6 P11 stop 34.645071/-116.755305 | P11 | 227.3 | 0.698 0.456 0.363 | 0.809 / 0.782 / 0.724 | 0.752 | SUPPORTS on short windows only (0.809/0.782/0.724); the same vehicle type climbed such pitches; weakest row |
| C 1-1 M1A2 23 ridge crossing 34.659681/-116.762350 (CONTROL) | G3 | 258.2 | 0.194 0.192 0.222 0.218 0.194 | 0.154 / 0.120 / 0.102 | 0.752 | REFUTES a block there; see C' |
| C' 1-1's own STEEPEST crossing 34.659460/-116.763633 (its POS at t=164.3) | G3 | 258 | short pitches 0.764 / 0.755 / 0.732 (5/10/20 m); 55 m window 0.661 | above 0.752 on short windows | 0.752 | PASSED - which is why the verdict rests on sustained extent, not the 0.752 threshold |
Cut used per row: A = the native E-W posting row through the point (walking west); B1, B2, C, C' =
the terrain sampled along the vehicle's last-300 m heading at native spacing (elev_face.py); the E-W
row at B1 reads +0.31 +0.26 +0.18 +0.19 +0.30 +0.40 +0.34 and at C descends then climbs (-0.18 -0.16
-0.11 +0.01 +0.17 +0.28) - the along-heading cut is the one that bears on the vehicle.
SURMOUNTED BY THE LEADERS THEMSELVES (their own POS polylines resampled at 1 m against the same
tiles): 1-35 max 5/10/20/55 m = 0.909 / 0.904 / 0.863 / 0.714; 1-6 (G3) 0.986 / 0.879 / 0.736 /
0.588; 1-1 over 5 km of ridge 55 m max 0.661. The face at A: 55 m at mean 0.858.
Land cover at all four points: CA FVEG 15 m = 30 "Sagebrush" -> BM_SAND -> sand (0.80); the
derated limit is identical at the freezes and at the control, so soil does not separate them; the
GEOMETRY of each unit's straight line across the ridge does.

VERDICT (HEAVY; the cause claim of this document; rewritten after the cold-start review of
2026-09-13 ~21:00Z): 1-35 stops because its straight leg toward route vertex V1 runs into 55 m of
SUSTAINED face - seven consecutive native postings at 0.70-0.95 rise-over-run (mean 0.858,
35-43 deg) - on sand, in the sim's 8 m elevation data at 34.6561/-116.7614. The discriminator is
sustained extent, NOT a single window against a threshold: every leader surmounted short pitches
above 0.752 on its way (1-35's leader up to 0.909 / 0.904 / 0.863 over 5 / 10 / 20 m; 1-6's 0.986 /
0.879 / 0.736), but no leader surmounted a 55 m window above 0.714, and 1-1's line over the same
ridge never exceeds 0.661 over 55 m even where it crosses 0.764 / 0.755 / 0.732 short pitches (its
own steepest crossing, 34.659460/-116.763633, passed). The stop LOCATION selects the vehicle's
effective limit: at its raw max-slope 0.94 the M1A2 should have climbed ~40 m of the face to
~1,630 m before stalling; it sits at the toe, 1,585 m, where the next posting steps are 0.70 then
0.85 - a limit near 0.75, which is what the dynamics text predicts for a surface poorer than dry
pavement: UG52 23.5.2 p508 "On the best soil surface (dry pavement), vehicles can just barely move
up the max-slope defined in the entity parameters by using maximum throttle. It is possible that
vehicles may slide down slopes, especially if the soil is slippery";
movingObjectParameters.h:294-310 (max-slope = "the maximum slope at which the entity can still have
acceleration >= 0 in its direction of motion"). CONFIRMING, from the same G5 rows: HMMWV 2
(max-slope 1.0) stops 61 m further west and 39 m HIGHER (1,624.3 m) on the same face, at a point
whose own face ahead reads 0.640 / 0.757 / 0.671 - two vehicles with different limits stop near
their own limits. The leader never comes to rest (its excursion decays from 22.8 m to a persistent
~2 m limit cycle held for 480 s); the followers hold station on their leader (UG52 30.22 p598); no
vendor component tests progress (sec 6a - by design), so the task reports running forever and
nothing reaches the unit controller or the interface. The interface's route authoring sampled
terrain heights only at the four route vertices, 4-5 km apart (sec 1), and never saw the ridge.
1-6's stops show the same signature (B1: 0.84 sustained over 20 m; B2: narrow); the three different
1-6 outcomes across runs follow from the per-run offset-route geometry meeting different parts of
the face (G2's leader failed its formation task at the face at sim 320 and the rest were re-led
around it).
WHAT IS MEASURED: the elevation data and its validation; the postings and gradients; the soil class
and factor; the entity max-slope values; the headings and stop points; the console silence and its
cause in the shipped code. WHAT IS ASSUMED: (i) the 0.752 figure is an ANALOGY, not a cited dynamics
limit: its only source, navigationPreferenceDescriptor.h:111-132, is a nav-mesh PATH-COST formula
(inherent cost x slope-avoidance-factor x (slope/max-slope)^2, no hard gate) that explicitly calls
the derating 'an estimate of the reduction in max-slope caused by terrain drag', and which sec 6c
shows was inactive in these runs (no soil tags in the profile; 1-35's goal outside the area). The
dynamics-side warrant is separate and qualitative: UG52 23.5.2 p508 (full sentence above) and
movingObjectParameters.h:294-310; the stop LOCATION at the toe is what selects a limit near 0.75
over the raw 0.94; (ii) bilinear reconstruction between postings (the posting differences carry the
verdict without it).
Adversarial review: (1) "commanded to stop" - the toe of a measured 35-43 deg face under a vehicle
that holds a ~2 m limit cycle at the toe of the face while its ordered speed stays 10 m/s is not a
commanded stop; a still follower (M3 1) is what a commanded stop looks like, and the followers are
explained by formation speed control. Not excluded by a velocity reading (never logged), but no
longer competitive. (2) A terrain-streaming gap - REFUTED: the sim's reported altitudes at the
freeze match the streamed dataset to 3 cm, so the sim had the surface. (3) An obstacle trap
(23.2.3) - no BlockedBy status in any run. (4) Soil - equal at freezes and control; not the
discriminator. (5) The leader held by its own formation's speed control - NOT EXCLUDED: UG52 30.22
p598 makes speed control mutual ('leader and followers, which use speed control to remain within
some distance of each other'), and in G5 the members straddle the leader (M577A2 1 ends 42 m east
and 25 m below at 1,560.6 m; HMMWV 2 61 m west and 39 m above at 1,624.3 m). Against it: the
leader's furthest reach is the highest point on the face any M1A2 attains, and HMMWV 2 passes it; a
formation hold would not put the leader at the face's toe. A reflected-velocity or set-speed reading
would settle it, and so would the confirming test of sec 8. STILL UNEXPLAINED, recorded: HMMWV 2's
extra creep is now CONFIRMING evidence (see the verdict), not unexplained; G2's 1-6 leader task
failure at sim 320.4 (a different exit, seen once); B2's narrow margin. Falsifier that would reopen
this: a run in which 1-35 is released from the toe by a route that avoids the face (or by Prefer
Roads) and then completes its leg - the confirming test - or a run in which it freezes at the same
point with the face removed from the data.

## 7b. Scope of the cause claim, narrowed by the pre-flight calibration review (2026-09-13 ~23:15Z)
The route pre-flight tool (tools/preflight/leg_check.py; DEMO_READINESS row 20) scores every first leg
of the COA against the sim's own elevation and soil, and its cold-start review re-derived P11's
outcomes from the traces instead of from net displacement. Findings that bear on THIS document:
- A THIRD slope stop: 4-27/2/1_A drove 22,697 m along its 32 km leg (the committed tool's plane,
  80831b9; 22,723 m on the equal-scale plane used earlier in this section - a projection constant,
  not a disagreement) and then froze for the remaining 2,870 s (+/-2 m), INSIDE the tool's own worst
  40 m window (centred at 22,702 m; scored 0.966 of the vehicle limit). It had been labelled "moved"
  from its net displacement. CORRECTED by the console read (sec 7c): the LEADER froze; three of its
  five members crawled 1.1-1.25 km past it. 1-35's freeze is 38 m before its window centre (18 m before its near
  edge). Two independent freezes ON scored faces support the mechanism of sec 7.
- The mechanism does NOT cover every early stop: in P11, 1-6/2/1_AD stopped at 2,840 m on ground
  DESCENDING at -0.14 over 55 m, 480 m SHORT of the face the tool flags (in G3 the same unit froze
  ON that face - sec 7 B1); and 856/HHC~PXY in P11 stopped 699 m short of its vertex on FLAT ground
  (-0.04), crawling at 0.23 m/s. Every stuck unit degrades into a 0.2-0.8 m/s crawl and all units
  slow after sim ~1,400 regardless of terrain (NOT 4-27's leader - sec 7c: 10.0 m/s in every 200 s
  bin to sim 2,200 and 9.92 m/s in its last 60 s; the crawl there is post-stop and in the followers). A SECOND stop mechanism is therefore active in these
  runs - the candidate already in the record is the formation / scale-crawl stop (DESIGN_ORBAT C1b:
  the move-into-formation gate; the S4 crawl of 2026-09-06), unmeasured here.
- Consequently the VERDICT of sec 7 is a claim about 1-35 (and 4-27), not about "the early stops":
  it stands for units that freeze ON a scored face, and it is silent on the stops on benign ground.
  The pre-flight's correct wording is "this leg crosses a sustained face your vehicles probably
  cannot climb", never "this is why your unit will stop".
- Calibration as corrected: at a 40 m window, frozen legs score 1.098 / 0.990 / 0.966 and the best
  clean pass 0.870 (margin 0.097 as the committed tool prints it; threshold 0.92); at 55 m the margin was an artefact of the wrong
  label (0.025 corrected); at 80 m the separation dies. One order, one terrain, three positives.
Adversarial review of 7b: the alternative reading of 4-27 - "it stopped for the second mechanism and
happened to be near a steep window" - is weakened by the stop falling inside the scored 40 m window on a 32 km leg and by the
identical +/-2 m signature to 1-35's; the console has now been read (sec 7c) and excludes blockage,
replan, give-up, goal change and any formation-wait print, but not speed control. The
alternative reading of 1-6/P11 and 856 - "they are slope stops the metric mis-scores" - is refuted
by the ground itself (descending / flat at the stop). What remains unexplained: the second
mechanism's identity; G3's 856/HHC stop (PREREG_NAVDATA_G3 sec 6 addition).

## 7c. 4-27's console, read (Opus reader, 2026-09-13 ~23:35Z; docs/experiments/READ_4-27_CONSOLE_2026-09-13.md; spot-checked)
P11 (runs/20260907T150643Z_run), 4-27/2/1_A re-created as the template Tank HQ Section: leader M1A2 3
(uuid ac191858, verified from the five followers' own leader=M1A2 3 task rows, vrfc2simapp.log:2077-2091),
unit object at level 4, six members at level 3 (their own "Setting to notify level 3." rows,
watchvrf-trace.csv:926-931). No member row at level 4 exists (configured outcome, not evidence) and
WatchVrf carries position only (tools/WatchVrf/WatchRunner.cs:14) - no velocity in the capture.
- THE LEADER: 10.0 m/s net in every 200-sim-s bin from sim 200 to 2,200, 9.92 m/s in its last 60 s, then
  at sim 2,366.0 (wall 1,396.4; 34.479042/-116.819676) net progress ends. Its own sim-reported altitudes
  over its own last fixes: +14.9 m over 26.7 m (0.558), then +10.9 m over 12.4 m (0.879) to a peak of
  1,111.8 m at 22,708.6 m along; it then fell back 22.5 m along and 16.6 m of height and held a limit
  cycle decaying from 22.5 m to 3.2 m by sim ~2,620 (0.78 m/s of shuffle, zero net progress, 3,922 sim s
  to the end of the capture) - the shape 1-35 shows (22.8 m -> ~2.0 m). Its oscillation band
  22,694.8-22,708.6 m straddles the pre-flight's worst 40 m (22,702) and 55 m (22,709.9) window centres.
  Console, every ~1 s cycle from sim 105 to the end (258,631 rows, seven shapes only): "Goal new or
  changed? -> Condition false", "Is path blocked? -> Condition false", Status move-along TaskRunning.
  Zero BlockedBy / stuck / give-up / replan / task-complete / formation-wait / speed rows after wall 90.
  Supervisor spot-check from the trace reproduced: peak 22,708.6 m at wall 1,398.4 alt 1,111.8; final
  22,697.2 m; the two pre-stop pitches 0.558 and 0.879.
- VERDICT: CONFIRMED slope stop for the LEADER - same signature as 1-35 on every listed point.
  Competing hypothesis, formation mutual speed control holding the leader (UG52 30.22 p598): weakened -
  at the stop the leader was the MOST ADVANCED of the six, three followers then passed it and walked
  1.1-1.25 km away while it stayed, and a commanded hold does not slide back 16.6 m of height. NOT
  EXCLUDED: the leader's commanded speed / reflected velocity inside the window is not in the capture.
  The falsifier is unchanged from sec 7: a level-4 leader console row setting speed, reflected velocity,
  or the leader completing the leg when routed around the window (G6 tests the router).
- THE UNIT DID NOT FREEZE: HMMWV 3 and M577A2 2 converged onto the leader (6.5 m / 47.9 m) and stopped;
  M1A2 4, M3 2 and HMMWV 4 went past it and were still creeping at 0.301 / 0.299 / 0.301 m/s (sim) when
  the capture ended, 1,246 / 1,202 / 1,078 m further along and 130-142 m LOWER (descending - not a slope
  stop). The pre-flight's "the formation leader stands for the unit" assumption (PREFLIGHT_CALIBRATION
  sec 4 item 6) is doing real work: the unit-level statement "4-27 froze" is true of M1A2 3 only.
- THE SECOND MECHANISM, seen inside the same unit: a constant ~0.30 m/s crawl in followers that overran
  their slots relative to a stopped leader. The vendor documents follower speed control for this task -
  UG52 30.22 p598 Maneuver Along: "a leader and followers, which use speed control to remain within some
  distance of each other ... speed control can override ordered speed" - but no numeric floor; the
  EntityLevel Lua scripts and vrfobjcore/groundManeuverInFormationPSR.h carry no speed parameter (grep
  2026-09-13). CANDIDATE, not a finding: follower speed control at its floor. It does NOT explain the
  LEADER crawls of 1-6 (P11) and 856/HHC, which remain open. Discriminator for both: member consoles at
  level 4 (a set-speed row) or reflected velocity - G6 runs member consoles at 4.
- Other runs carrying 4-27 member consoles at level 3, unread: runs/20260913T174516Z_run (G3) and
  runs/20260907T174654Z_run - a second read would test whether the 3-of-5 crawl-past reproduces.
Adversarial review of 7c: the strongest alternative to "slope stop" for the leader is the same speed-
control hold sec 7 could not exclude, and this read does not close it either - it repeats the gap
(position-only capture, members at 3). What this read DOES add: obstacle blockage, paging and the generic
crawl are refuted for the leader by rows, and the followers' behaviour is inconsistent with a hold that
pins the leader. Unexplained and recorded as a falsifier of any single-mechanism story: the leaders of
1-6/P11 and 856/HHC crawl without a slope.

## 8. What follows (decisions for the user; none taken here)
- The interface needs its OWN progress watchdog: no net movement for N sim-seconds while the
  task reports running -> report to STP (a TaskStatus the demo audience can see) and/or re-task.
  The vendor's own sample (decideToGiveUpTask) is this test, delivered as a sim-side plugin; on
  our side the reflected entity position/velocity we already track carries it. The sibling of
  the arrival-evidence rule C15.
- Route authoring must respect the vehicles' derated climbing limit ALONG the leg, not the
  heights at the vertices. Doc-backed options, cheapest first: (a) Prefer Roads set-data for
  the tasked units (UG52 30.28 p604, 40.54) - roads avoid such faces by construction; needs a
  facade method (native change, standing authorization); (b) the vendor's mesh, configured to
  the vehicles: a ground-platform profile slope-max near atan(0.752) = 37 deg instead of 46 and
  sand in soil-types-to-tag-with-surface-char (navigationProfiles.mtl:246, :261 - a C:\MAK
  settings edit, the user's call), areas that CONTAIN the goals (1-35's V1 is 229 m outside),
  regeneration (~25 min per 20x20 km); (c) interface-side intermediate vertices from the same
  public elevation tiles the sim reads (validated identical) - building a planner the vendor
  already ships, last resort.
- The G4 mesh repeat as registered is now known to be unable to help 1-35 (profile 46 deg, no
  sand tag, goal outside the area); it stays deferred unless option (b) is chosen, in which case
  a new prereg with the corrected profile replaces it.
