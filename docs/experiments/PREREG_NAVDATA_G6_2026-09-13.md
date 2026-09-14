# PREREG G6: the vendor's router applied correctly - ONE navigation area containing every leg

Registered 2026-09-13 ~22:00Z by the supervisor (Fable 5.1) BEFORE generation and before the run.
User direction: "Router exists, but our mesh was too small. Something to fix" and "no random walks -
read the vendor documentation, samples, community insights instead". Tier: HEAVY (the run decides
whether the vendor's default mesh cost routes a tank around a measured 35-43 deg sustained face).

## 0. What the vendor's files say (read 2026-09-13; file:line)
- ground-vehicle-move-to.lua: the mesh branch is entered only if the vehicle's current point AND the
  destination are inside a navigation area (isCurrentPointInNavArea / isDestInNavArea, :1416-1424,
  sequenced at :1464-1465) and, per path part, only if the part's start and end are inside one
  (isPathPartOutsideNavArea, :478-481); "inside" = vrf:findClosestPointInNavigationArea reports the
  point in ANY area (:197-213). The mesh query is ONE call per part, vrf:findPathToLocation(start,
  end, {useAbstractGraphs = false, useChannels = true, channelRadius = 4.0}) (:487-499); on nil or
  < 2 points the tree falls back to feature planning ("Planned nav path is nil." / "... not enough
  points"; the success row is printVerbose "Planned path has N points." at :511 - CAPTURED at console
  level 4, as G5 showed for the feature planner's "Planned path has N parts." at :1398).
- UG52 66.3 p1281: "Entities use advanced navigation within the areas and standard navigation if
  they move between them" - the mesh does not span areas. So a leg whose two ends lie in different
  areas passes the gate and then has nowhere to be planned; the 9-tile layout computed earlier
  today would reproduce the no-mesh result and is WITHDRAWN.
- UG52 66.2 p1276: "The maximum size for a navigation area using the default values is 20 km by
  20 km. ... If you reduce the precision of navigation data, creating a sparser graph, the size of
  the maximum area increases." Table 54 p1279: "Do not try to create an area larger than 20 km by
  20 km unless you have changed the raster precision". 66.2.1 p1277: sectorize large areas; "Path
  planning over long distances may be slower" with sectors.
- navigationProfiles.mtl header: "The default profiles are 'lifeform' - used by humans - and
  'ground-platform' - used by vehicles and animals." (:9-10); "New profiles can be added ... update
  the platform file of any entity that should use the new navigation data" (:17-21). ground-platform
  block (:242-300): slope-max 46, soil-type-tags: mud -> "no-go-exclusive"; soil-types-to-tag-with-
  surface-char: road, pavedroad (sand therefore takes "the default navigation tag",
  EntityMovementTerrain.htm); feature-tag-volumes: MAK_WATERWAY and MAK_VEGETATION no-go, MAK_ROAD
  tagged road; raster-precision 0.2. vrf_entitymovementonslopesVRF2365.htm: "For planning paths in
  nav meshes, higher slopes cost more (whether the slope is up or down)"; the slope-avoidance-factor
  in the movement system's navigation-preference-controller "can vary the effect". THE PROFILE IS
  NOT CHANGED IN G6: the default cost is what is under test.
- The vendor's own shipped areas (SharedData navData, 2026-09-13 read): every ground-platform area
  uses raster 0.2 and cell-size 43 (Thun 41) regardless of size; tile size grows with the area
  (Ala Moana 2.67 km / 30 tiles = 89 m; Thun 10.1 km / 49 tiles = 206 m). Our 20 km area at 40
  tiles (500 m) generated in 24 min on the same settings. The single area below is that recipe
  scaled; if the generator refuses the size at raster 0.2, the documented fallback is a coarser
  raster (0.4), recorded as a deviation.

## 1. The lever
ONE N/S-aligned area containing every route vertex of data/COA-STP1_Order.xml (66) and every tasked
unit's birth point (11) with a ~1 km margin: lat 34.318 to 34.689 (41.3 km), lon -117.019 to
-116.428 (54.2 km); tile-count 108 x 82 (~500 m tiles); raster 0.2; cell-size 43; profile
ground-platform, UNCHANGED. Generated headless by bin64\vrfNavGenerator.exe through the short
junction (MAX_PATH), output under the scratch tree, nothing under C:\MAK (the permanent home is the
user's pending decision). Expected ~2.4 h (2,240 km2 at ~3.6 s/km2), ~1 GB. Registered on a NEW
terrain copy carrying ONLY this area's record (the MojaveAO20 record is dropped to avoid two
overlapping areas of the same profile), fixture R9_Mojave_Empty_52_NavAO, deployed by the
sanctioned builder command. The three known trouble points and every leg end are inside.

GENERATION RESULT (2026-09-13 20:39Z -> 23:59Z, exit 0): 8,856 sectors (108 x 82), every one
"GENERATED NAVDATA: REGULAR" (8,856/8,856 in the log; 8,810 carry AbstractData), 11,952 s total = 3.3 h
(vs the ~2.4 h estimate; ~5.3 s/km2), peak working set ~9 GB, 26,525 files / 1.2 GB (the ~30k files seen
mid-run included inputs the generator cleans up). MOVED to the permanent home
C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveCOA (DEMO_READINESS row 21),
runtime config re-pointed, terrain copy tools/navdata/out/"MAK Earth (online) + MojaveCOA.mtf" = 10 navData
records with exactly one at the new home, fixture R9_Mojave_Empty_52_NavAO built, validated (ALL FIXTURES:
OK) and deployed byte-identical; C:\MAK navData carries 0 Mojave entries. Deployed build UNCHANGED (exe
2026-09-07 11:06Z).

## 2. The run (after generation; idle machine; one variable vs P11 = the area)
COA-STP1 full order, the P11/G2 environment, PROBE type map, -NoGui, -RunSecs 900 -WatchSecs 1200,
member consoles at 4 (to capture the planner's own row: "Planned path has N points." = mesh,
"Planned path has N parts." = feature). The decisive events happen early: 1-35 froze by sim ~360
in every earlier run (wall ~230 s at 1.6x). SampleThreads.ps1 attached.

## 3. Predictions (before generation and run; a missed HIGH prediction is a stop)
- P16a (HIGH, the gate): 1-35's members print "Planned path has N points." (mesh) for the leg to V1
  - both ends are now inside the area, so the mesh branch is entered. MISS (only "N parts") = the
  gate reads differently than the code above says, or the area did not load - read the consoles'
  "Is destination in nav area?" outcome; STOP.
- P16b (MEDIUM, the router's effect): 1-35 does NOT freeze at the toe - its leader exceeds 3 km net
  by sim 600 (it froze at 1,970 m by sim 360 in P11/G2/G3/G5). HOLDS = the vendor's default slope
  cost routes around the sustained face; no profile change needed. MISS while P16a holds (mesh path
  planned, tank still stops at the face) = the default cost does not avoid 35-43 deg sustained
  faces -> the profile / slope-avoidance-factor change is justified (a C:\MAK settings edit, the
  user's sanction), and the confirming test of the FINDING's cause claim is also delivered either
  way (P16b HOLDS = released by a route around the face).
- P16c (MEDIUM): 1-6/2/1_AD's leader exceeds 4 km net by sim 700 (its stops were 2.85-3.29 km).
- P16d (RECORDED, not predicted): the sim/wall ratio per 300 s with a 2,240 km2 mesh and ~60
  vehicles planning 24-45 km legs on the full graph (useAbstractGraphs = false). This is the FIRST
  measurement of the vendor router's engine cost at this scale on an idle machine; a step collapse
  here would be the real H1, which G3 could not test (its area did not contain the legs).
- P16e (instrument sanity, HIGH): the sim reads the new area's record; nav-area console rows > 0
  for the tasked units; 128 units; order accepted.

## 4. What counts as a stop
P16a or P16e missed = stop. P16b miss with P16a held = the profile question goes to the user with the
numbers; nothing is edited under C:\MAK without the word.

## 5. Results
GENERATION started 2026-09-13 20:39Z: the generator ACCEPTED the 41.3 x 54.2 km area at raster 0.2
(no size refusal - the 20 km figure of UG52 p1276 is a GUI-side default, not a generator limit);
runtime config extent nw (-27176, 20597) to se (27262, -20597) m about the area centre; ~500 m
sectors with ~99,000 terrain triangles each; "Generated 3 distinct nav tags"; 842 sector files after
6 min (the 20 km area produced 4,803 files in 24 min, so 3-3.5 h expected). Terrain copy "MAK Earth
(online) + MojaveCOA.mtf" = 10 navData records, this area only (the AO20 record dropped); fixture
R9_Mojave_Empty_52_NavAO built, validated (positive gate) and deployed byte-identical to the sanctioned
scenarios folder. Nothing else under C:\MAK.
(run results to be filled after the run; nothing here was written before it)

## 5. RESULTS (run 2026-09-14 00:27Z, runs/20260914T002716Z_run; harvest docs/experiments/G6_RESULTS_2026-09-14.md) - STOP
| prediction | verdict | evidence |
|---|---|---|
| P16a (HIGH, gate): mesh branch plans the leg ("Planned path has N points.") | MISS | 1-35's leader: "Is current point in nav area?: success", "Is destination in nav area?: success" (goal = V1 exactly), then "Planned nav path has not enough (0) points." then "Planned path has 1 parts." (feature planner) at sim ~101. Whole run: 15 "points" rows, ALL for goals 6-23 m away (formation slots); 178 "not enough (0) points" rows, 110 of 112 paired ones for goals 9.3-20.1 km; 145 "parts" rows. Dest-in-area success 101 / fail 0. |
| P16e (HIGH, instrument): the sim reads the new area | HOLDS | 259 console rows name NavArea-ground-platform MojaveCOA; MojaveAO20 0 rows; "New Primary nav area" 162 rows from sim 75.9 on all nine moving units. |
| P16b: 1-35's leader > 3 km net by sim 600 | MISS | Stopped at 34.65608/-116.76142 - 0.0 m from its P11 and G3 final fixes, 4,104 m along-track in all three runs; plateau reached at sim 334 (G3 324, P11 285). NOT a test of the router: the mesh never planned the leg. |
| P16c: 1-6's leader > 4 km by sim 700 | MISS | Stopped 0.9 m from its G3 stop (5,208 m along). Same reason. |
| P16d (recorded): engine cost | recorded | sim/wall 1.52 / 1.74 / 1.66 / 1.45 per 300 s wall bin (P11 1.55/1.81/1.70/1.62; G3 1.52/1.67/1.57/1.57); mean 4.5 cores over five threads, no collapse (G2's 0.35/0.05 did not recur). The router's cost at scale is STILL unmeasured - no long path was ever planned. |
The MISS is not the anticipated one (the prereg's stop condition assumed the gate reads differently or the area
did not load): the area loaded, both gate conditions succeeded, and the vendor's mesh query returned nothing for
every multi-kilometre goal. G6 is therefore a FOURTH reproduction of the stops (every leader within a few hundred
metres of its P11/G3 counterpart at sim 300/600/900; the two freezes at the same metre), not a discriminator.
Level 4 bought: the nav-area OUTCOME rows (which made P16a decidable), and the ordered-speed rows ("Setting
ordered speed:", "processSetSpeed: Using ordered speed Nmps instead of task speed.") - none at or after any
maneuver-in-formation leader's stop (1-35: last at sim 132, stop at sim 642); the formation slow-down /
maintain-speed / speed-up rows appear ONLY on C/1-35's disaggregated controller. The seven-shape loop of FINDING
sec 7c reproduces verbatim for 1-35's leader at level 3, with 17 level-4 "Ticking ..." rows around it and nothing
else. Instrument caveats: POS capture began at sim ~191 (58 s later than P11), so net-from-first-fix is not
comparable across runs (along-track from the authored origin is used instead); the thread sampler's wsMB column
is pinned at 4096 (a counter ceiling); the runner was killed at t+127 s of the window (RUNNER_EXIT127_2026-09-14.md)
and the sim ran unattended for nine hours - the capture itself is complete (observers ran to their 1200 s caps).
PRELIMINARY, same-day (grep counts, pairing with goal distance pending): G1 (R9, 3x3 km area) points 44 / parts 4
/ not-enough 0; G2 (COA, 20x20 km) 96 / 62 / 4; G3 (20x20 km) 19 / 144 / 6; G6 (41x54 km, 8,856 sectors) 15 /
145 / 178. If G1's 44 mesh paths include R9's kilometre-scale legs, the failure is SCALE-dependent.
THE QUESTION MOVED: not "does the default slope cost route around the face" but "why does vrf:findPathToLocation
return zero points for a multi-kilometre goal inside one loaded area". Doc-backed candidates, none tested:
(setqb gamewareMemorySize 16) vrfSim.mtl:383 ("increasing this limit can allow for path plans on larger nav areas
by more entities simultaneously"), (setqb gamewareQueryTimeBudget 5.0) :389, UG52 66.2.1 sectorisation ("path
planning over long distances may be slower"), 66.3 lazy per-sector loading. Both .mtl settings live under C:\MAK
(a user decision; must survive reinstalls - DEMO_READINESS row 21). Docs read + goal-distance analysis dispatched
2026-09-14 ~11:00Z; no probe is registered before they report.
