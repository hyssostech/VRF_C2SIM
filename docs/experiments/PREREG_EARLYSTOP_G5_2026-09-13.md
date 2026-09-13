# PREREG G5: 1-35 alone, member consoles at the vendor's debug level, at the freeze

Registered 2026-09-13 by the supervisor (Fable 5.1) BEFORE launch. Follows FINDING_EARLY_STOPS_2026-09-13
sec 5: the slope reading is refuted as stated; the cause of 1-35's identical freeze in three runs is
OPEN; the cheapest discriminator is the vendor's own debug stream from the six members at the stop.
Tier: HEAVY (diagnostic read on which a cause claim will rest).

## 0. Docs and record consulted
- UG52 21.9.1 p483 (object console notify levels 0-4; 4 = debug; set per object - our
  Vrf:ObjectConsoleNotifyLevel / Vrf:ObjectConsoleMemberNotifyLevel drive setObjectNotifyLevel).
- UG52 23.5.2 p508 (slope effects; "vehicles can just barely move up the max-slope ... may slide
  down slopes"); movingObjectParameters.h:294-310 (max-slope = rise-over-run at which acceleration
  >= 0); ground-tracked.sysdef:787 soil factors; navigationPreferenceDescriptor.h:125-127.
- ground-vehicle-move-to.lua:219-264 ("blocked" = BlockedByWall / BlockedByVehicle only).
- FINDING_EARLY_STOPS_2026-09-13 secs 1-5: the printed rows, the geometry, the refuted slope reading,
  the unmeasured list (soil, metre-scale relief, mesh coverage, commanded vs actual speed).
- lessons-vendor-diagnostics-first: the first instrument for a VRF behaviour question is the
  object's own console at level 4.

## 1. The run (TWO deliberate changes vs P11, both named)
- PROBE ORDER: a copy of data/COA-STP1_Order.xml holding ONLY task T1_AOA_SE_1-35_AR;_2/1_AD_P1
  (performer d6df3c3d-f31b-701a-bfc6-2fb9bc86092a, task uuid cd589832-e19f-44e9-91b0-a81ee68d5666),
  built by the supervisor from the original (scratchpad\COA-STP1_Order_PROBE_T1only.xml; 1 task,
  well-formed, header and footer verbatim). With CreationPolicy=AtOrder only 1-35 materializes; the
  other 127 units stay shells. PROBE, not policy.
- CONSOLE LEVELS: unit 4 (as before) and MEMBER 4 (debug; before: 3). Six members only, so the
  trace stays small.
Everything else = P11: fixture R9_Mojave_Empty_52 (NO navigation area - the freeze is identical with
and without it, and P11 is the reference), build 80daed6, the same env block (AtOrder, DeStack 700,
DropOriginVertexMeters 100, PositionReportSeconds 10, predecessor timeout 7200), the PROBE type
map, -NoGui, -BackendNotifyLevel 3, -RunSecs 600 -WatchSecs 800 (the freeze occurred by sim ~360 in
every run; at ~1.6-2x that is < 4 minutes of wall). NOTHING else runs on the machine.
Command (64-bit pwsh from Bash):
  export Vrf__TypeMappingMode=FidelityTable Vrf__CreationPolicy=AtOrder Vrf__DeStackCreates=true
         Vrf__DeStackSpacingMeters=700 Vrf__DeStackRotationDeg=0 Vrf__DropOriginVertexMeters=100
         Vrf__TaskPredecessorTimeoutSeconds=7200 Vrf__ObjectConsoleNotifyLevel=4
         Vrf__ObjectConsoleMemberNotifyLevel=4 Vrf__PositionReportSeconds=10
  pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\RunC2SimScenario.ps1 -VrfProfile 5.2 -NoGui
       -Scenario R9_Mojave_Empty_52 -Init data\COA-STP1_Initialization.xml
       -Order <scratch>\COA-STP1_Order_PROBE_T1only.xml -ClientId C2SIM
       -TypeMapFile <scratch>\unit-type-map-52-nolifeform.json
       -RunSecs 600 -WatchSecs 800 -BackendNotifyLevel 3

## 2. Predictions (before launch; a missed HIGH prediction is a stop)
- P15a (HIGH): 1-35 ALONE freezes at the same place - its leader's net displacement stops at
  1,900-2,050 m within 200 m of 34.6561/-116.7614 by sim ~400, and stays there to the cap. HOLDS ->
  the freeze is intrinsic to this unit / route / ground, not an interaction with the other 8 tasked
  units (blocking, formation traffic). MISS (1-35 passes 3 km) -> the other units are part of the
  mechanism; the three earlier traces are re-read for cross-unit proximity at the stop; STOP here.
- P15b (MEDIUM, the discriminator): at level 4 the members' consoles at the stop show the movement
  controller's commanded (ordered / desired) speed or throttle AND the vehicle's actual speed; the
  commanded value is NON-ZERO while the actual speed is ~0 for the leader. HOLDS -> the vehicle
  cannot move (physical: slope-at-metre-scale, soil derating, terrain contact / clamp) and the
  controller does not know it. MISS (commanded speed 0, or an explicit stop/hold/wait row) -> the
  controller decided to stop; its reason is the next read.
- P15c (RECORDED, not predicted): any level-4 row naming slope, slide, traction, soil, terrain,
  contact, clamp, "waiting for terrain page", stuck, or a physics/dynamics state, in the 60 s
  before and after the freeze; and the leader's altitude trace at the stop (amplitude and period of
  the oscillation reported by the console read: 1,579-1,596 m, ~25 s).
- P15d (instrument sanity, HIGH): the six members' consoles emit level-4 rows (more distinct message
  shapes than the seven of level 3), the unit is created and tasked, and the run reaches sim >= 500.

## 3. What counts as a stop
P15a or P15d missed = stop, report. Whatever P15b shows, the next step (a terrain/soil grid probe,
or a controller read) is a NEW prereg.

## 4. Results - run 20260913T185936Z (launched 18:59:36Z, 600 s cap, runner exit 0, clean teardown)
SUPERVISOR QUICK LOOK (19:12Z; the executor's full harvest follows):
- P15a HOLDS: 1-35 ALONE froze at the same place. Leader M1A2 1 net 1,970 m at 34.65607/-116.76144,
  alt 1,585.5 m - the P11/G3 freeze point (34.65608/-116.76142, 1,969-1,970 m) to ~1 m. The other
  five: HMMWV 1 1,981 m, HMMWV 2 2,047 m, M1A2 2 1,941 m, M3 1 2,042 m, M577A2 1 1,900 m, all
  within the same ~120 m patch (alts 1,560-1,624). The freeze is intrinsic to this unit / route /
  ground; interaction with the other eight tasked units is REFUTED (only 1-35 existed as
  vehicles; 127 shells).
- P15d HOLDS: 443 distinct console message shapes from the six members (level 3 gave 7); ~93,500
  console rows per member; the unit console printed 7 rows (unchanged). Instrument live.
- The level-4 stream is the ground-vehicle-move-to BEHAVIOUR TREE tick trace: at the freeze, every
  tick reads "Primitive or Planned Move: running -> Plan Then Move: running -> loop until no more
  path parts or block skirt fail -> Maybe plan path: FAIL (Goal new or changed? fail) -> Move on
  unblocked path or replan: running -> Move while checking blockage: running -> Maybe Skirt
  Blockage: FAIL (Is path blocked? fail) -> Select move technique: running -> Maybe Move Off Road:
  running -> Move along offroad path: running -> Move along route: RUNNING -> Status of task
  move-along is TaskRunning" plus "Checking status of task for <member>". The tree is parked in
  its move-along task node and sees nothing wrong. Whether the stream also carries commanded vs
  actual speed (P15b) is the executor's read.
- The leader's altitude over the last 240 s of wall: 1,584.0-1,585.5 m (1.5 m band), position
  spread 1.3 m. The "17 m altitude cycling with a 25 s period" reported by the 2026-09-13 console
  read of G3 is NOT reproduced here; the grade check's "3 m ball" is. The FINDING doc's item (iv)
  is to be corrected once the executor has compared the runs.
EXECUTOR HARVEST (Opus, g5_harvest.py in the session scratchpad):
- Clock: sim 37-3,954 s over wall 35-666 s = 6.21x (six vehicles instead of ~1,700). Leader stops
  between sim 300 and 330 (gap to its final position 135 m at sim 280, 34 m at 300, 17 m at 320,
  8 m at 360) and never moves again through sim 3,954 - about 3,600 sim-seconds standing still.
  Stops: M1A2 1 1,970 m (5 m from the reference point), M1A2 2 1,941, M3 1 2,042, HMMWV 1 1,981,
  HMMWV 2 2,047 (creeps ~90 m more until sim ~1,270), M577A2 1 1,900; all 4.57-4.66 km short of V1.
  P15a HOLDS.
- P15d HOLDS: 561,831 member console rows; 434 distinct shapes vs 139 at level 3 (299 new). But
  280 of the 299 new shapes are one-shot, at sim <= 93.7 (the behaviour tree being BUILT -
  "Creating selector, Primitive or Planned Move", "Creating loop, Loop to stall for replanning",
  ... - and the initial turn-to-route). After the freeze the members emit 26 shapes: the 7 of
  level 3 plus 19 indented renderings of the SAME tick. The shape set while MOVING (sim 240-300)
  is byte-identical to the shape set while FROZEN (330-450 and 3,800-3,900).
- P15b NOT CONFIRMED - AND NOT MISSED: the instrument does not carry it. Every speed row in the
  run lies in sim 40.6-93.7: "Setting ordered speed: | 3mps" -> 8 -> "10mps" (leader last at sim
  56.1, M3 1 at 50.2), "maneuver-in-formation-controller::processSetSpeed: Using ordered speed |
  #mps instead of task speed", "Saving ordered speed 3 mps". No actual / current speed row exists
  at level 4 anywhere; no throttle row; no stop / hold / halt / wait row; no "Goal new or changed?
  true". The last commanded value, 10 m/s, is never re-stated and never rescinded. The vendor's
  object console at its highest level is NOT an instrument for commanded-vs-actual speed.
- P15c: zero rows naming slope, slide, traction, soil, terrain contact, clamp, ground contact,
  "waiting for terrain page", stuck, physics, dynamics, accel, brake, gear, or any movement-system
  component (ground-tracked, ground-wheels, ground-auto-controller) at any level. Six incidental
  name matches only (the task name ground-vehicle-move-to; "Loop to stall for replanning").
- Leader altitude at the freeze (sim >= 330, 287 POS samples at 12.4 sim-s per sample): 1,579.2-
  1,595.8 m (span 16.6 m, mean 1,585.0, sd 1.24), horizontal shuffle 22.8 m E-W, integrated path
  318.7 m over 3,339 sim-s, DAMPING (a 22.9 m swing at sim 330-343, then 2-6 m between consecutive
  samples, 7 of 286 pairs above 5 m). The span REPRODUCES the G3 console read's 1,579-1,596 m; the
  ~25 s period is NOT measurable here (12.4 s per sample = the Nyquist limit; the apparent 38 s
  is aliasing). M3 1 beside it: 0.4 m of altitude span, 0.6 m E-W - genuinely still. The
  supervisor's "1.5 m band over the last 240 s of wall" was the damped tail, not the freeze.
- Unit console: 7 rows, silent from sim 37.6 to 3,953.6. Its maneuver-along subtask prints
  "speed=0" (the default); the members then take 3 -> 8 -> 10 m/s from the maneuver-in-formation
  controller.
EXECUTOR'S INFERENCE, flagged as such (not printed by the sim): the leader's reported altitude
tracks its longitude monotonically across the 22.8 m shuffle - 1,583.2 m at lon -116.761401 to
1,595.8 m at -116.761577, i.e. 12.6 m of altitude over 16.1 m of ground, a local rise-over-run of
~0.78 - so the leader appears parked on a steep local face, shuffling along the fall line, while
its still companions sit on level ground metres away. A 100 m resample (the C2 grade check) cannot
see a 16 m face; 0.78 is below the M1A2's 0.94 limit but ABOVE the soil-derated 0.752 (rocks/sand).
This is geometry from POS rows only; it does not by itself re-open the refuted slope reading and
is recorded for the docs pass to weigh.
UNEXPLAINED (flagged): HMMWV 2 kept creeping to sim ~1,270 while the other five stopped by ~400;
the leader alone oscillates while M3 1 is still. No console row attaches to either.
CONSEQUENCE for instruments: "cannot move" vs "commanded to stop" needs the vehicle's own
velocity (the reflected entity state - WatchVrf can carry it) or the vendor's own progress test
(the decideToGiveUpTask sample, being read), not the object console.
