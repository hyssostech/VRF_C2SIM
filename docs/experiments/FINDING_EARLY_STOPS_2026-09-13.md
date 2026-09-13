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

## 5. Check results
(filled when the executor returns)
