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

## 4. Results
(to be filled after the run; nothing here was written before it)
