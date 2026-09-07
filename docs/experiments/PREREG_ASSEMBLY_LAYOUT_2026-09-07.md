# PREREG: spacing co-located units at startup (assembly-area layout) - 2026-09-07

Tier: HEAVY (a cause claim rides on it: "the residual stall is the start-point crowding").
Gate: PREREG (this file, before the run). User ruling 2026-09-07 ~00:20Z: "I need to bite my
tongue and accept trying to space out the entities at startup, given the evidence of the
triangular position you now cite." This SUPERSEDES the R8 ruling of 2026-07-13 for 5.2: that
ruling was made on 5.0.2 (no vehicle-vehicle avoidance) and against a different symptom.

## 0. Vendor anchors (sample > docs > our runs)
- SAMPLE: the shipped remote-control sample never stacks objects - it offsets even one
  platoon's four tanks by 10 m from each other, "to create an initial triangle formation"
  (examples/remoteControl/commandLineRemoteController.cxx:756-770).
- DOCS: UG52 23.2.3 - avoidance "is not planning a path through the obstacles it sees ...
  the vehicle could become trapped ... by moving entities that close off its path"; UG52
  25.2.1 - a unit created from the panel gets its members laid out in its default formation
  (one unit per point, never two). Shipped formation files
  (data/simulationModelSets/EntityLevel/vrfSim/formation): Formation-Column-Armor-Co(US)
  spans x -430..+200 m (630 m long, platoon columns +/-50 m wide); Wedge/Line/Vee-Armor-Co
  span 0..300 x, +/-200 y; Ar_Plt_US_* +/-50 m; Ar_Co_HQ_* -100..0 x, +/-50 y.
- SCRIPT: ground-vehicle-move-to.lua - a vehicle stopped by another for 10 s is a blockage
  (back up 2 lengths, skirt 5), MAX_REPLANS = 3, one global replan, then "Loop to stall for
  replanning" (doWhile = true) - a stalled vehicle never untangles by itself.
- OUR RUNS (PREREG_ORDER_TIME_MATERIALIZATION 3.3/3.4): 4,828 "BlockedByVehicle" messages in
  the first 300 s after tasking; 5/9, 8/9, 8/9 tasked units beyond 1 km; the stuck composed
  company waited on move-into-formation from one sub-unit that had stalled in the pile.

## 1. The lever
The existing DeStacker (hex rings, first unit kept in place, adjacent slots `spacing` apart;
`--destack-selftest` PASS on the deployed build 2026-09-07) applied at init to every group of
units sharing a coordinate, with Vrf:DeStackSpacingMeters = 700: larger than the longest
shipped company formation (630 m) so no two units' default formations can overlap whatever
their heading. 54 units at STP's point -> rings 1..4 (60 slots), outer radius 2.8 km - the
size of a brigade assembly area. The stored order-time plans carry the spread positions
(review fix H, 13ac3c3), so members materialize where their shell is. Context shells (no
vehicles) take slots too; harmless, and the GUI shows a laid-out ORBAT.
Env for the run: Vrf__DeStackCreates=true, Vrf__DeStackSpacingMeters=700 (no runner switch;
the app logs "DeStack (R8): N units at (lat,lon) spread onto 700 m rings").

## 2. The run
Same as the completion run (234525Z): AtOrder, FidelityTable + scratch no-lifeform map,
R9_Mojave_Empty_52, -NoGui, RunSecs 2700, -StopWhenComplete, unit consoles at 4 AND member
consoles at 3 (needed for the blocking count; the 1 M-row cost was 1.84x in 3.3). ONE variable
against 234525Z's configuration: the spacing (plus member consoles, which 3.3 showed do not
change movement).

## 3. Predictions (written before launch)
- P1 "BlockedByVehicle" messages over the whole run < 500 (co-located: 4,828 in 300 s).
- P2 9 of 9 tasked units beyond 1 km (co-located: 5/9, 8/9, 8/9).
- P3 the composed tank company C/1-35 logs "task complete msg rcvd from" ALL FOUR sub-units
  and advances to maneuver-along (in 231401Z it never received TANK2's).
- P4 at least one TASKCMPLT within 2700 s (first legs 24-45 km; at >= 1.8x a 28 km leg is
  ~1,500 s wall). Medium confidence: the exact leg speed is not measured.
- P5 the app log shows the DeStack line with 54 units spread onto 700 m rings, and the
  materialized members are created at the spread positions (PLACEMENT lines).
FALSIFIERS: P1 miss with P5 met -> the crowding is not (only) at the start point, or 700 m is
not enough - read the members' consoles, no theory first. P3 miss with P1 met -> the
composed unit's stall has another cause; that unit's console decides. P2/P4 misses with P1
and P3 met -> movement is limited elsewhere (route, terrain, speed) - measure before
claiming.

## 4. Results
Run 20260907T003457Z (launched 00:35Z on the deployed build 7ac70c9). RUNNER INCIDENT: the
runner process exited with code 9 at ~t = 590 s of the window with no Stop-Runner message;
the sim, the interface and WatchVrf kept running (the runner's teardown never ran - manual
teardown after the window). Scored from the live trace. MECHANISM (code + instrument
facts, not a run): the window loop's status line calls Read-LiveText, which reads the WHOLE
trace with StreamReader.ReadToEnd (RunC2SimScenario.ps1:976-990) every 30 s; with unit
consoles at 4 AND member consoles at 3 the trace grew ~17 MB/min (176 MB at t = 590 s), and
the launching PowerShell host is 32-bit ([Environment]::Is64BitProcess False, found
2026-09-06) - a 350 MB UTF-16 string plus copies inside a 32-bit process is an out-of-memory
death. The diagnostic run 224326Z (member consoles only, ~150 MB by its END) survived the
same host; this one crossed the line at 10 minutes. Competing explanation - the tool's
10-minute background timeout - is refuted by the 45-minute run bxe7sxsdb from the same host.
FIX OWED (runner, not product): Read-LiveText for the status line reads a tail, not the file;
launch long runs from a 64-bit host. Falsifier of the mechanism: the same configuration
dying under a 64-bit host with a tail read.
MID-RUN, t = 646 s (order at ~t = 40 s):
- P5 HOLDS: "DeStack (R8): 54 units ... spread onto 700 m rings" (+ a second group of 2);
  341 reflected objects on 308 distinct ~10 m cells, largest co-located group 5 (co-located
  runs: one cell holding every taskee's vehicles).
- P1 HOLDS: "BlockedByVehicle" 63 messages in 10 minutes, all runs' worst minute 30 (the
  co-located diagnostic: 4,828 in the first 5 minutes).
- P3 HOLDS: C/1-35's console logs "task complete msg rcvd from" TANK2 (sim 89 s), HQ1 (111 s),
  TANK3 (141 s), TANK4 (163 s) - all four - then issues maneuver-along on its four offset
  routes C/1-35_R0..R3 at wall t = 103 s. The gate that stayed shut in 231401Z opened in under
  three sim minutes.
- P2 in progress: 8 of 9 beyond 1 km at t = 646 s (2.8 / 5.0 / 5.2 / 8.8 / 9.0 / 9.6 / 10.4 /
  10.6 km); the ninth is C/1-35 at 415 m, which began maneuvering at t = 103 s. At the same
  wall time the co-located diagnostic (224326Z) had 0.14-5.0 km.
- P4 pending (first legs 24-45 km).
(final numbers after the manual teardown)
