# LD-1296-R4 - like-for-like load measures, Road to Kaunas (base) vs Iron Storm COA 1 (2026-10-20)

The local dispatcher's read for STP-1296 (read-only; nothing launched; vendor logs read in place, counts only). Iron
Storm derives from a customer scenario: it appears here only as task numbers, mover / container ordinals, counts and
km. VERIFIED unless marked INFERRED. Ground only (Kaunas: DIS domain; Iron Storm: all performers ground, INFERRED from
battle dimension G and 11:1 aggregate types).

## Inputs
- Kaunas: base RoadToKaunas.scnx (t=0). Iron Storm: the 37-task D36 export (runs\launch52\d36\export-coa1, matched to
  D36 by its GET log, build main-6a90ee35, 37 tasks). Option 4 exists only on the 25-task pre-run export
  (prerun-976); it was never emulated on the 37-task export.
- The interface turns a task into a move as TaskGeometryResolver.cs:215-289 / AssembleRoute :377-526 do, and a C2SIM
  Line (Route OR Boundary) registers as a path (VrfC2SimService.cs:2163-2170): the interface does NOT exclude
  boundaries. The emulation of the four D36 navigates gives 82.7 km; the interface's own pre-flight gave 82.69 km.
- Boundary / phase-line exclusion (the STP-1150 rule): removes nothing on the 37-task export; on the 25-task export
  it removes 4 lines on 2 tasks (task 9: 223.7 -> 4.2 km; net -219.5 km).

## M1 path km (literal = polyline length, a point task = the straight leg from start, a LOWER BOUND; chained = a
## sequenced task starts where the previous ended, INFERRED)
| set | moving tasks | movers | literal km | chained km |
|---|---|---|---|---|
| Kaunas base, headline (all but not-activated sync entries) | 107 | 21 | 1,659.0 | 876.0 |
| Kaunas, incl. not-activated entries | 115 | 27 | 1,846.4 | - |
| Kaunas, distinct routes counted once | 31 routes | - | 265.0 | - |
| Iron Storm, all 37 tasks | 26 | 8 | 1,118.4 | 789.7 |
| Iron Storm, option 3 admitted 20 | 9 | 6 | 144.6 | (not meaningful) |
| Iron Storm, option 3 executed 12 (D36(b)) | 7 | 5 | 90.8 | 88.8 |
| Iron Storm, option 4 admitted 25, boundaries excluded | 17 | 6 | 514.9 | 475.1 |
| Iron Storm, option 4 executed 20, boundaries excluded | 12 | 4 | 474.1 | 429.8 |
| Iron Storm, option 4 executed 20, as the interface resolves it | - | - | 693.6 | 649.3 |
Kaunas own .pln plans carry 0 ground movement (all 22 own-plan movement tasks are air). The longest Kaunas ground route
is 35.9 km; approach legs to routes' first vertices (369.7 km chained) are not in M1.

## M2 concurrency (peak ground movers moving at once)
- Kaunas: per sync phase, combined by index (lockstep ASSUMED; the matrices share no movers): 0, 11, 8, 4, 4, 3, 6;
  PEAK 11 movers (index 1). Immediate tasks only: peak 7. Ceiling if all moved at once: 21 (INFERRED). Counted in
  MOVERS (units / containers); members of Kaunas containers are not counted (needs the orbat).
- Iron Storm D36(b) FILL: 4 container navigates in flight together (tasks 33, 15, 2, 23), 4 containers with 67
  MEMBERS (19, 26, 5, 17), none to TASKCMPLT before the loss. SCORED: 3 containers / 50 members in flight at the loss.

## M3 corridor area = M1 x 2w (km2; overlaps not removed, an upper bound), w = 1 km / 5 km
- Kaunas headline literal 3,318 / 16,590; chained 1,752 / 8,760; distinct routes 530 / 2,650.
- Iron Storm all 37 literal 2,237 / 11,184; option 3 executed 12 182 / 908; option 4 executed 20 (excl.) 948 / 4,741.
- No vendor page-in radius for the sim engine was found (below). pathfinder-min-search-radius = 1000 m
  (terrainInterfaceConfig.mtl:98-101) equals the w = 1 km column; using it as a half-width is INFERRED.

## M4 cache\vrfsim (files / MB)
- D36 FILL +3,467 / +7.2 MB (28,070 -> 31,537); SCORED +746 / +2.2 MB (-> 32,283, from the D36 doc only).

## M5 planning queries (D36 FILL / SCORED vendor logs, counts only)
- feature_source 131,878 / 157,394; "queued for N seconds" 45,736 / 65,017 lines, parsed median 114.0 / 73.4 s, p95
  160.6 / 134.9 s (the D36 doc's counts are 45,243 / 64,362 - a method difference, not reconciled).
- Planning keywords (navigat, plan_path, could not compute path, planner, route plan, obstacle query, ...) are 0 or a
  handful, identical in both logs. INFERRED: load-time lines; these logs carry no per-request planning line.

## Vendor text
- AC p155: remote terrain "is streamed when needed ... As tiles are streamed from the terrain server, they are cached
  locally."
- AC p156 (3.3 Manually Paging In Terrain): "VR-Forces pages in terrain based on the location of simulation objects
  and an algorithm that predicts where terrain will be needed." "there may be cases where it cannot, for example,
  when there are many simulation objects over a wide area or many fast-moving simulation objects." Page-in areas are
  for paged IVE / MetaFlight only and "only affect the sim engine".
- AC p239: "(Feature data is not cached.)" (GUI cache generation); VRFSIM_OSGEARTH_CACHE_PATH.
- UG p1247 (63.6.1): the sim "does not load terrain until it needs the terrain to support the objects in the
  scenario"; "If a simulation object is moving, paging in terrain that it needs is a higher priority than predictive
  paging"; page-out "is calculated based on the polygon count of the terrain, not on its memory usage".
- No sim-engine page-in radius or prediction distance found; terrainInterfaceConfig.mtl has no paging-radius key.
