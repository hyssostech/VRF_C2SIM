# PREREG: navigation data for the COA's maneuver ground (2026-09-07, registered before launch)

Follows PREREG_ASSEMBLY_LAYOUT_2026-09-07.md 3g (the generator runs headless under the DEMO
licence). User direction: "Dig deeper for api support ... even if not based on a third party
vendor"; "Read the docs, not just code! Search the internet!". Tier: HEAVY (a cause claim -
"silent whole-unit stops are the slope-blind planner" - is under test; PREREG gate).

## 0. Docs consulted (cited, not recalled)
- UG52 66.1-66.3 (p1273-1284): navigation areas are rectangular, N/S aligned, at most
  20x20 km at the default raster precision (Table 54 p1279: "Do not try to create an area
  larger than 20 km by 20 km unless you have changed the raster precision"); several areas
  per terrain; "Entities use advanced navigation within the areas and standard navigation if
  they move between them" (66.3 p1281); data loads lazily "until you place a simulation
  object that will use it" (p1281); sectorize large areas (66.2.1; "generation of large
  unsectorized navigation areas sometimes fails", Table 54).
- UG52 66.3.3 p1284: newly generated data is used after the scenario is (re)opened.
- UG52 66.5 p1287: importing = selecting the .navRuntimeConfig, "the navigation area is
  added to the terrain", then "Save the terrain" - i.e. the terrain .mtf record IS the
  registration. The shipped MAK Earth (online).mtf carries 9 `<Type>navData</Type>` generic
  records (myGenericRecords count 9, .mtf:63423-63434), each `path` -> a .navRuntimeConfig.
- vrfobjcore/vrfNavAreaManager.h:91-98 (processGenericRecordLoaded: "processes terrain
  records indicating nav areas to be loaded. Prepares the area for loading when first
  requested"), :136-138 loadAllNavData, :150-157 findNavAreasForLocation(loadNavAreas=true).
  No remote-control message creates or loads an area (vrfRemoteController.h has none).
- UG52 23.5 (p~ground movement): "ground slope is not taken into account by the path planner
  that plans around feature obstacles"; vehicles "can just barely move up the max-slope".
  UG52 40.54: military vehicles ignore roads by default. navigationProfiles.mtl:242-267
  ground-platform profile: entity-radius 3, step-max 0.56, slope-max 46 deg, raster 0.2.
- Research workflow wf_c3f93816 (2026-09-07, 46 agents): no other slope/soil-aware planner
  exists without navigation data; DtSetNavigationPreference 'prefer-roads' is the only
  remote lever without data.
- Baseline instrument: the vendor sim log of run 150643Z prints "File found at
  <...>.navRuntimeConfig" for each of the 9 shipped records at terrain load.

## 1. The lever (ONE variable per run)
A navigation area "NavArea-ground-platform MojaveAO20": 20x20 km, north edge 2 km north of
the STP assembly point (34.67998/-116.72480), centred on lon -116.70 (lat 34.518-34.698, lon
-116.809 to -116.591), tile-count 40x40 (500 m sectors), raster 0.2, cell-size 43, profile
ground-platform - generated headless by bin64\vrfNavGenerator.exe (3g RESULTS) into the
scratch tree via the short junction C:\Users\PAULOB~1\Temp\nav (MAX_PATH). Registered on a
COPY of the terrain: tools/navdata/make_nav_terrain.py appends a 10th navData record whose
path is the absolute .navRuntimeConfig; the copy lives at tools/navdata/out/ (gitignored),
NOT under C:\MAK. The fixture R9_Mojave_Empty_52_Nav.scnx = the empty 5.2 fixture with
Terrain-Database / Gui-Terrain-Database = that copy (build_fixture.py --terrain), deployed
by the sanctioned command to C:\MAK\vrforces5.2d\userData\scenarios. Everything else
(build, overlay, type map, de-stack 700 m, DropOriginVertexMeters, arrival completion,
predecessor timeout 7200) = P11 (run 150643Z).
Coverage: the area holds the STP point, the 700 m rings, the P11b stop points of 1-6
(3.3 km out) and 1-35 (2.0 km out), the R9 anchor (34.613/-116.600) and the first ~18 km
south. It does NOT hold 1-35's fixed mid-route trap (34.58197/-116.98337, 26 km west) nor
the far ends of the 24-45 km legs - those stay on standard navigation = the within-run
control.

GENERATED 2026-09-07 16:42-17:06Z (before any run): 4,803 files, 166 MB, 1,600 sectors
("Generated: ground-platform", no error), 1,436 s wall (the 3x3 km test area took 47 s -
roughly linear in area, ~3.6 s per km2 at cell-size 43 / raster 0.2). Nothing under C:\MAK.
The .navRuntimeConfig (offset ECEF, extent +/-10 km, nav-data-path absolute via the
junction, original-terrain = the C:\MAK .mtf) is the record registered on the terrain copy.

## 2. Gate G1 - the loading chain on the small fixture (R9, 3 units, ~10 min)
Run: R9 init + R9 order on R9_Mojave_Empty_52_Nav (runner -Scenario), -NoGui, everything
else as the last green R9 run on the fidelity table (L3, 201623Z 2026-09-06: FidelityTable,
repo map, 3/3). Build = the deployed P11 build (80daed6, exe 2026-09-07 11:06Z), unchanged.
Command (64-bit pwsh from Bash; env = observation levels + the type-mapping mode only):
  export Vrf__TypeMappingMode=FidelityTable Vrf__ObjectConsoleNotifyLevel=4 \
         Vrf__ObjectConsoleMemberNotifyLevel=3
  pwsh -NoProfile -File scripts\RunC2SimScenario.ps1 -VrfProfile 5.2 -NoGui \
       -Scenario R9_Mojave_Empty_52_Nav -Init data\R9_Mojave_Lean_Initialization.xml \
       -Order data\R9_Mojave_UnitMove_Order.xml -RunSecs 600 -SampleSecs 2
(RunSecs 600 vs 360: a cap, not a lever - the runner exits early on completion; the margin
is for the lazy nav-data load.)
Instruments: the vendor sim log in C:\MAK\logs (grep COUNTS only - it holds the process
environment in cleartext), the unit/member consoles in the WatchVrf trace, the runner
verdict (3/3 TASKCMPLT), sim_ratio.py.
- G1a (record read): the vendor sim log prints "File found at <our .navRuntimeConfig>" -
  10 such lines (9 shipped + ours) vs 9 in the baseline. MISS = the record form or the
  absolute path is not accepted -> fix the record, not the run.
- G1b (terrain accepted by path): the sim loads the terrain COPY (log names the copy's
  path; no "terrain not found"/fallback), the three units are created and R9 completes 3/3
  as before (no regression from the terrain copy).
- G1c (data loaded): at or after the first ground unit's creation inside the area the sim
  log or the unit console (level 4) shows a nav-data load message for MojaveAO20 (exact
  text unknown - recorded, not predicted). Absence of ANY such line while G1a holds is a
  finding to read before G2, not a failure of G2.
Falsifier of the whole approach at G1: the sim refuses a Terrain-Database outside
$(SHARED_DATA_DIR) - then the copy must sit beside the original under C:\MAK (a write under
C:\MAK = the user's call, asked, not taken).

## 3. Gate G2 - COA-STP1 with the area (the P11 run, one variable)
Run: COA-STP1 init + order, 4200 s, -NoGui, member consoles as P11. Command = P11's
(recovered from the session transcript 2026-09-07 17:00Z) with ONLY -Scenario changed:
  export Vrf__TypeMappingMode=FidelityTable Vrf__CreationPolicy=AtOrder \
         Vrf__DeStackCreates=true Vrf__DeStackSpacingMeters=700 Vrf__DeStackRotationDeg=0 \
         Vrf__DropOriginVertexMeters=100 Vrf__TaskPredecessorTimeoutSeconds=7200 \
         Vrf__ObjectConsoleNotifyLevel=4 Vrf__ObjectConsoleMemberNotifyLevel=3 \
         Vrf__PositionReportSeconds=10
  pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\RunC2SimScenario.ps1 -VrfProfile 5.2 \
       -NoGui -Scenario R9_Mojave_Empty_52_Nav -Init data\COA-STP1_Initialization.xml \
       -Order data\COA-STP1_Order.xml -ClientId C2SIM \
       -TypeMapFile <scratch>\unit-type-map-52-nolifeform.json   (PROBE map, as P11) \
       -RunSecs 4200 -WatchSecs 4500 -BackendNotifyLevel 3 -StopWhenComplete
- P12a (inside the area, HIGH confidence): 1-6/2/1_AD and 1-35/2/1_A, which stopped as
  whole units 3.3 km and 2.0 km out in P11, pass 6 km (both, straggler_track). MISS =
  slope is NOT the mechanism of those stops -> stop, read their consoles; competing
  hypotheses recorded in sec 5.
- P12b (outside the area, MEDIUM): 1-35's fixed trap at 34.58197/-116.98337 (26 km, three
  runs) still fires IF 1-35 reaches it - the trap is outside the area, on standard
  navigation. If the trap does NOT fire, the "terrain trap" reading of it weakens (or the
  lane differs); recorded either way.
- P12c (no regression, HIGH): arrival-evidence completions >= 3 (P11e: 3), no duplicate
  TASKCMPLT, no crash, sim ratio within 20 % of P11's.
- P12d (the planner is different inside the area, MEDIUM): the tasked units' consoles show
  the nav-mesh planner in use (messages naming "nav" / the area / advanced navigation)
  for legs inside the area and not for legs outside. Recorded.
- P12e (blocking, not predicted): "BlockedByVehicle" count recorded against P11's 424.

### G1 RESULTS - run 20260907T170643Z (R9 on R9_Mojave_Empty_52_Nav; 600 s cap; runner exit 0)
Measurement, not a verdict on the mechanism. All three gates HOLD.
- G1a HOLDS: the sim read 10 .navRuntimeConfig records at terrain load (9 shipped + ours,
  "NavArea-ground-platform MojaveAO20.navRuntimeConfig"); the baseline of the P11 run was 9.
  The record form written by tools/navdata/make_nav_terrain.py is accepted as shipped ones are.
  COUNT PROVENANCE (from the harvested VENDOR log - unauditable from the run dir under the
  secrets rule; OWED: the runner emits these two grep COUNTS into the run dir).
- G1b HOLDS: the sim loaded the terrain COPY from a path outside $(SHARED_DATA_DIR) - its own
  lines name "...\tools\navdata\out\MAK Earth (online) + MojaveAO20.mtf" for "Creating new
  scenario on terrain", "Loading terrain ... into VR-Vantage" and the surface-characteristics
  paging. No fallback to the shipped terrain. 6 of 6 create altitudes came from the terrain
  query; 3 taskees, 3 routes, 3 MoveAlongRoute, 3/3 TASKCMPLT by t+120 s (t+8/72/108 s);
  interface resigned with exit 0; RTI infra untouched (rtiexec 15720, forwarder 43728).
  Vendor-log error-ish line count identical to the P11 harvest (278 vs 278; the set difference
  of error-ish lines is EMPTY), so the terrain copy introduced no new vendor complaint - again
  from the harvested VENDOR log, unauditable from the run dir under the secrets rule; OWED:
  the runner emits these two grep COUNTS into the run dir.
- G1 sim/wall ratio: 3.95x least-squares (sim_ratio.py, 612 samples, wall 29-233 s, sim 55-864 s) - the fastest in the record, on 6 units / 3 taskees.
- G1c HOLDS - the data is loaded and the entities' movement scripts QUERY the area (whether a path was planned THROUGH the mesh was not observed in G1): the entities' own consoles carry
  39 "New Primary nav area: NavArea-ground-platform MojaveAO20" and 42 "Leaving Primary nav
  area: NavArea-ground-platform MojaveAO20" rows across 14 distinct entities between t=24.5 s
  and t=81.1 s, and the ground-vehicle-move-to behaviour tree runs its condition nodes "Is
  current point in nav area?" and "Is destination in nav area?" in the same window.
- SUPERVISOR CORRECTION 2026-09-13 (cold-start review) - WHAT THOSE ROWS ARE: per entity the
  rows come in Leaving/New PAIRS (1 to 4 pairs per entity, spanning 2-18 s); each "Leaving" is followed at the
  IDENTICAL timestamp by a "New" for the SAME area (1 to 4 pairs per entity, spanning 2-18 s),
  and 12 of the 14 entities open with a "Leaving". The 42-vs-39 imbalance decomposes EXACTLY
  as 4 trailing unmatched "Leaving" rows minus 1 leading unmatched "New" - no third term. So
  the rows are repeated RE-ACQUISITION OF THE SAME PRIMARY AREA, once per plan (observed
  behaviour; the vendor does not state the mechanism), not units driving out of the area.
  WITHDRAWN with this correction: (i) the geometry reading, that the units are created inside
  the area and their routes run east out of it, i.e. UG52 66.3's advanced-inside/standard-
  between; and (ii) BOTH candidates offered for the imbalance (a trace that opened after the
  first entries; an initial area counted without a "New" line). The containment fact stands
  on its own but is not evidence for these rows, and the "8 of 9 placement coordinates" figure
  does not reproduce - the app log prints no coordinates; the init's 3 distinct coordinates
  all fall in lat 34.518-34.698, lon -116.809 to -116.591
  (data\R9_Mojave_Lean_Initialization.xml has 3 distinct lat/lon pairs).
- INSTRUMENT NOTE (not a fixture defect): -StopWhenComplete did not fire, so the window ran
  its 600 s cap. The early-exit criterion also wants a post-completion RPT POSITION per
  taskee, and this run did not set Vrf__PositionReportSeconds. Set it for G2 (P11 used 10 s).
- Adversarial review: the competing explanation for the nav-area rows is a SHIPPED area
  (Range220 / Ala Moana / Thun / Kilo2) being entered instead of ours - refuted, the message
  names MojaveAO20, which exists only in the copy's tenth record. Second competing
  explanation, "the sim silently fell back to the shipped terrain and found the area some
  other way" - refuted, only the copy carries the record and the sim's terrain lines name the
  copy. Not tested by G1: whether navigation data CHANGES movement outcomes - G1 was the
  loading chain only; that is G2.

### G2 RESULTS - run 20260907T174654Z (COA-STP1 on R9_Mojave_Empty_52_Nav; 4200 s cap; exit 0)
TRACK STOPPED per sec 4: two HIGH predictions missed as written; no parameter was adjusted,
and the verdict is the user's / Fable's. REWRITTEN 2026-09-13 by the supervisor after an
independent recomputation and a cold-start review; the 2026-09-07 text overclaimed in two
places (a per-sim-second speed advantage and "no straggler in any unit"), both refuted at
equal simulated time.

TWO VARIABLES, NOT ONE, vs P11 (20260907T150643Z). (1) The scenario - hence the terrain copy
and the navigation area. (2) A concurrent 212-agent documentation workflow on this machine:
its files span 17:48:02Z-18:06:32Z and this run's trace t=0 is 17:48:49Z, so the load ran from
wall -47 s to +1063 s - over the whole fast phase, the onset, AND the establishment of the
floor. MY OWN breach of the one-variable rule: a measurement with a known contaminant, not
a clean comparison. Same build/env/type map/4200 s cap as P11; order pushed at wall 50 s.

THE HEADLINE: THE ENGINE COLLAPSED. Least-squares sim/wall ratio per 300 s wall window, in
order from wall 0 to wall 4500:
  G2 (area)  1.545 1.675 0.28* 0.050 0.039 0.038 0.033 0.024 0.022 0.023 0.025 0.023 0.018
             0.018 0.015        (* 600-900 is 0.352x by endpoints, 0.28x by least squares)
  P11 (none) 1.550 1.806 1.700 1.623 1.574 1.556 1.537 1.510 1.373 1.333 1.252 1.247 1.145
             1.095 1.139
Overall least squares: G2 0.240x, P11 1.456x. Wall spans are close (G2 51-4368 s = 4,317 s; P11
66-4327 s = 4,261 s, 1.3 % shorter); sim spans differ: G2 56.5-1224.4 (1,167.9 s), P11 53.4-6288.1
(6,234.7 s). The fall is a STEP, and at 60 s resolution it is sharp: the last fast window is
wall 580-640 (1.566x), the next 640-700 is 0.337x, then 0.175, 0.114, 0.100, 0.089, 0.056 -
onset wall ~628-660 s. From wall 1,200 to 4,368 the engine advanced 81.3 SIM-SECONDS in
3,168 wall-seconds. The floor is not flat: the post-step windows decay 0.039 -> 0.015.

MOVEMENT AT EQUAL SIMULATED TIME (median member displacement from birth, metres) - totals at
equal WALL time are meaningless when the clocks run at different rates, so both runs are cut
at the same sim clock:
  unit           G2@sim1170  P11@sim1170  G2@sim600  P11@sim600
  1-1/2/1_AD          10493        10459       5014        5010
  1-35/2/1_A           1974         1970       1972        1971
  1-6/2/1_AD           6190         2845       2988        2804
  4-27/2/1_A          10632        10692       4804        4968
  40/2/1_AD            9235         8516       4985        4346
  5-20/2/1_A          10412         9391       4770        5017
  856/HHC             10461        10675       4694        4966
  A/6-56/HHC              0            0          0           0   (ADA task refused for want
  B/5-20              10828        10786       5129        5145    of a location, DEMO row 16)
  C/1-35               7077         9165       3077        3558
  SUM                 77302        74498      37432       37786
Rate over the first 1,170 sim-s: G2 66.1 vs P11 63.7 m per sim-second (1.04x); over the first
600 sim-s, 62.4 vs 63.0 (0.99x) - at equal sim time the two runs move the same distance.
WITHDRAWN: the 2026-09-07 claim of "two to three times" the distance per simulated second
(69.7 vs 25.1 = 2.78x). It used total displacement over total sim-seconds, which charges P11
for the 5,065 sim-seconds G2 never ran - seconds in which its units slowed or stood (1-1:
10.5 km by sim 1170, 26.1 km by sim 6288; 1-35 frozen at 1.97 km from sim 600).

(Per-unit figures in the tables above are rounded; the SUM rows were computed at full precision, so cells may not add exactly.)
FORMATION AT EQUAL SIMULATED TIME (max member distance from the unit centroid). At sim 600 G2
is tighter: all units <= 180 m except C/1-35 at 475 m; P11 all <= 311 m except C/1-35 at
400 m. At sim 1170 G2 is WORSE where it matters: 1-6/2/1_AD 2,278 m and C/1-35 536 m (856/HHC
137 m, the rest <= 95 m), while NO P11 unit is above 1 km at that clock (max C/1-35 359 m,
1-6 353 m); P11's first >1 km straggler is 856/HHC at sim 1960, after G2's whole run had
ended at sim 1224. WITHDRAWN: "no straggler in any unit" - straggler_track on G2 reports
1-6/2/1_AD "STRAGGLER M3 4 ... 2301 m => STUCK", so 9 of 10 units show no straggler, not 10
of 10. That instrument is also not comparable between the runs: its window is 15 WALL minutes,
holding ~18 sim-seconds at G2's floor against ~1,400 in P11.

COMPLETIONS: 0 TASKCMPLT and 0 arrival-evidence completions (P11: 3 and 3) - the clock, not
the movement: 1,168 sim-seconds against legs of 24-33 km left no unit near its last vertex.
BLOCKING AT EQUAL SIM TIME: BlockedByVehicle 518 (G2) vs 147 (P11) by sim 1170 - 3.5x. The
2026-09-07 per-sim-minute figures for replan (9.5 vs 2.1) and stall/give-up (0.21 vs 0.05)
used the same invalid total/total denominator and have NOT been recounted; they are struck.
MESH IN USE: 324 "New/Leaving Primary nav area" console rows (P11: 0) and 292 in-nav-area
behaviour-tree condition nodes (P11: 184 of the generic form), front-loaded - per 300 s wall
window 250, 14, 54 (48 of them in 600-660), 0, 0, 0, 6, 0 ...; last row at wall 2,022 s.
PER UNIT: 1-35/2/1_A stops at 1.97 km in BOTH runs - the area does not touch it. 1-6/2/1_AD
is SPLIT in G2, not freed: M1A2 20, M577A2 4, HMMWV 7 and HMMWV 8 reach 6,219-6,397 m while
M3 4 and M1A2 19 stand at 2,883-2,918 m, the same ~2.9 km line at which P11's WHOLE 1-6
stopped (2,804 at sim 600; 2,845 at 1170; 2,884 at 6000). The median 6,190 m hides the split.
EVENT AT THE BREAK (correlation only, no mechanism claimed): at wall 627.9-631.5 s, inside the
628-660 s onset band, HMMWV 7 of 1-6/2/1_AD (uuid ending 6fa96235442f) logs "Controller's
subtask has Failed (base-system.movement.move-along, ID 25)", then "Task Move along route
fail" and "Task Turn to route fail", and toggles Leaving/New Primary nav area four times in
3.6 s; at wall 628.0 every 1-6 member is re-tasked to maneuver-in-formation on unitRoute
T15_AOA_SE_1-6_IN;_2/1_AD_P1 ROUTE. Console volume falls across the same seconds: 41,457 CON
rows in wall 540-600, 28,774 in 600-660, 6,452 in 660-720, 2,843 in 780-840, ~900-1,100
thereafter. No app-log exception near the onset. One entity's trace beside the step, not
tested as its cause.
PREDICTIONS AS WRITTEN: P12a MISS - 1-6 did not pass 6 km AS A UNIT (two of its six members
stand at the 2.9 km line) and 1-35 did not move past 1.97 km. P12b UNTESTABLE (1-35 never
approached the 26 km trap). P12c MISS (0 completions, not >= 3; 0.240x against P11's 1.456x,
far outside the 20 % band). P12d HOLDS. P12e recorded above.

Adversarial review (statement under test: "the navigation mesh made this scenario collapse"):
- Competing hypothesis 1, MY OWN CONCURRENT LOAD: the 212-agent workflow spans wall -47 s to
  +1063 s, covering the fast phase, the onset AND the establishment of the floor - THE ONSET
  TIMING IS UNUSABLE. It does not cover the following ~3,300 s, over which the ratio never
  recovered (1063-1363 s 0.040; 1663-1963 s 0.037; 3000-3300 s 0.025; 4000-4300 s 0.017). But
  "the machine was otherwise idle" was NOT measured - no CPU, memory or process sampling was
  taken during this run - so the persistence is CONSISTENT WITH a load-induced onset that left
  the engine in a slow state; it is not evidence against that hypothesis.
- Competing hypothesis 2, the mesh is expensive per se: REFUTED IN ITS SCALE-FREE FORM by G1 - the same area on the R9 fixture (6
  units, 3 taskees) ran at 3.95x (sim_ratio.py on run 20260907T170643Z, measured 2026-09-07 - see G1 RESULTS). A LOAD-DEPENDENT mesh cost at 11-unit scale is NOT refuted by G1.
- Competing hypothesis 3, COA-STP1 is simply slow at this scale: WEAKENED by P11 - the same
  units and order without an area held 1.10-1.81x for the whole window.
- Competing hypothesis 4, a PER-UNIT pathology rather than a global cost: 1-6's failed
  move-along subtask and its nav-area toggling at the 2.9 km line could drive per-frame
  replanning for that unit's members. NOT TESTED. The discriminating observation is whether a
  clean repeat (no concurrent load, CPU sampled) collapses at the same SIM time (~950-1000 s),
  which points at the scenario state, or at the same WALL time, which points at the machine.
- UNEXPLAINED, recorded as falsifiers rather than footnotes: (a) why 1-35 stops at 1.97 km in
  BOTH runs, mesh or no mesh, which no terrain-planner account covers; (b) why 2 of the 6
  members of 1-6 stop at the same ~2.9 km line that held P11's whole unit, mesh or no mesh.
- NOT MEASURED / NOT CLAIMED: nothing here says navigation data cannot work, and nothing here
  establishes that it caused the collapse. A clean repeat with no concurrent load, with CPU
  sampling, and with either a longer cap or fewer vehicles is the next measurement; ordering
  it is Fable's / the user's call.

## 4. What counts as a stop
A missed HIGH prediction (P12a or P12c) stops the track; no parameter is adjusted to make
it pass. G1a missing stops G2 until fixed.

## 5. Competing hypotheses for the P11b stops (to be weighed in the review)
- H2: the vehicles' OWN max-slope (entity parameter) is lower than the mesh profile's
  46 deg, so a mesh-legal path can still stall a vehicle - then P12a fails while P12d holds.
- H3: the stops are formation-driven (the maneuver-along controller waiting on a member),
  not terrain - then P12a fails and the consoles show a formation/wait state, no planner
  message.
- H4: the stops are route-geometry driven (the new direct line after the origin vertex
  drop) - then rotating the ring or restoring the vertex changes them, the area does not.

## 6. Results
(to be filled after each gate; nothing here was written before the runs)
