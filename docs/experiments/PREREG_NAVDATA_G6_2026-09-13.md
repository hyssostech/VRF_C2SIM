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
