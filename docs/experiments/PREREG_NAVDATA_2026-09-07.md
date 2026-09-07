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
- G1b HOLDS: the sim loaded the terrain COPY from a path outside $(SHARED_DATA_DIR) - its own
  lines name "...\tools\navdata\out\MAK Earth (online) + MojaveAO20.mtf" for "Creating new
  scenario on terrain", "Loading terrain ... into VR-Vantage" and the surface-characteristics
  paging. No fallback to the shipped terrain. 6 of 6 create altitudes came from the terrain
  query; 3 taskees, 3 routes, 3 MoveAlongRoute, 3/3 TASKCMPLT by t+120 s (t+8/72/108 s);
  interface resigned with exit 0; RTI infra untouched (rtiexec 15720, forwarder 43728).
  Vendor-log error-ish line count identical to the P11 harvest (278 vs 278; the set difference
  of error-ish lines is EMPTY), so the terrain copy introduced no new vendor complaint.
- G1c HOLDS - the data is loaded and the planner uses it: the entities' own consoles carry
  39 "New Primary nav area: NavArea-ground-platform MojaveAO20" and 42 "Leaving Primary nav
  area: NavArea-ground-platform MojaveAO20" rows across 17 distinct entities between t=24.5 s
  and t=81.1 s, and the ground-vehicle-move-to behaviour tree runs its condition nodes "Is
  current point in nav area?" and "Is destination in nav area?" in the same window. The
  entering/leaving is expected from the geometry: the R9 units are created inside the area
  (8 of 9 distinct placement coordinates fall in lat 34.518-34.698, lon -116.809 to -116.591)
  and their routes run east out of it, which is the vendor's "advanced navigation within the
  areas and standard navigation if they move between them" (UG52 66.3 p1281).
  UNEXPLAINED DETAIL (recorded, not smoothed): 42 "Leaving" vs 39 "New" - three more exits
  than entries. Candidates not yet checked: the trace opened after the first entries, or an
  entity created inside the area counts an initial area without a "New" line.
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
TRACK STOPPED per sec 4: two HIGH predictions missed as written. No parameter was adjusted.
The verdict and the lever choice are the user's / Fable's; what follows is measurement only.
One variable vs P11 (20260907T150643Z): the scenario, hence the terrain copy and the area.
Same build (80daed6, exe 11:06Z), same env, same probe type map, same 4200 s cap.

THE HEADLINE MEASUREMENT - THE ENGINE COLLAPSED, THE MOVEMENT DID NOT.
Sim/wall ratio in 300 s wall windows (sim_ratio.py samples, 406,540 console timestamps):
  window (wall s)   0-300  300-600  600-900  900-1200  1200-1500  then to 4500
  G2 (nav area)     1.86x   1.68x    0.36x     0.05x      0.04x    0.02x flat
  P11 (no area)     1.97x   1.81x    1.70x     1.62x      1.57x    decays to 1.10x
G2 simulated about 1,170 sim-seconds in 4,216 s of wall; P11 simulated about 6,236. The
G2 curve is a step, not a decay: it holds P11's rate for ten minutes, falls over two
windows, then sits on a floor near 0.02x for the remaining fifty minutes.
- Distances (median member displacement, metres): 1-1 11,134 (P11 26,111); 1-35 1,958
  (1,970); 1-6 6,254 (2,931); 4-27 11,310 (23,734); 40 9,693 (27,910); 5-20 10,958
  (15,086); 856/HHC 11,069 (24,133); B/5-20 11,427 (13,989); C/1-35 7,786 (21,412);
  A/6-56/HHC 0 (0, the ADA task refused for want of a location - DEMO row 16, unchanged).
  Per SIMULATED second the G2 units covered roughly two to three times what P11's did.
- Formation: straggler_track reports "no straggler (all members within 1 km)" for EVERY
  unit in G2. P11's signature shape, one runaway member with the rest stalled, is absent.
- Completions: 0 TASKCMPLT and 0 arrival-evidence completions (P11: 3 and 3). With ~1,170
  sim-seconds and legs of 24-33 km, no unit was near its last vertex when the cap fell.
- Mesh demonstrably in use: 324 "New/Leaving Primary nav area" console rows (P11: 0) and
  292 in-nav-area behaviour-tree condition nodes (P11: 184 of the generic form).
- Per SIMULATED minute: BlockedByVehicle 26.7 (P11 4.1); replan 9.5 (P11 2.1); stall or
  give-up 0.21 (P11 0.05).
PREDICTIONS AS WRITTEN: P12a MISS (it required both units past 6 km; 1-6 passed at 6,254 m,
1-35 did not at 1,958 m). P12b UNTESTABLE (1-35 never approached the 26 km trap). P12c MISS
(0 completions, not >= 3; ratio 0.24x against P11's 1.46x, far outside the 20 % band).
P12d HOLDS. P12e recorded above.

Adversarial review of the one causal-sounding statement, "the collapse is associated with
this scenario's vehicle count ON the mesh":
- Competing hypothesis 1, MY OWN CONTAMINATION: I launched a 212-agent documentation sweep
  on this machine at about wall 300 s of this run, and it ran for 1,112 s doing local PDF
  extraction. That overlap is a self-inflicted breach of the one-variable rule and it
  covers the onset window, so THE ONSET TIMING IS CONTAMINATED AND CANNOT BE USED. It does
  not explain the floor: the sweep ended near wall 1,400 s and the ratio stayed at 0.02x
  for the following 2,800 s with the machine otherwise idle.
- Competing hypothesis 2, the mesh is expensive per se: REFUTED by G1, which ran the SAME
  area with 6 units at 3.95x, the fastest ratio in the record.
- Competing hypothesis 3, COA-STP1 is simply slow at this scale: WEAKENED by P11, the same
  128 units and the same order without an area, which held 1.10-1.97x for the whole window.
- UNEXPLAINED, recorded as falsifiers rather than footnotes: (a) why the collapse begins
  around wall 600-900 s rather than at first mesh use at wall 44 s; (b) why 1-35 stopped at
  about 1.95 km in BOTH runs, with and without the mesh, which no terrain-planner account
  covers; (c) whether the engine was CPU-bound at the floor - CPU was NOT sampled during
  this run, so that is unmeasured, not established.
- NOT MEASURED / NOT CLAIMED: nothing here says navigation data cannot work. It says this
  run bought better formation-keeping and better distance per simulated second at a wall
  cost that made the fixed 4200 s window unusable, on a machine whose contamination window
  is known. A clean repeat with no concurrent load, CPU sampling, and either a longer cap
  or fewer vehicles is the obvious next measurement, and it is Fable's call to order it.

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
