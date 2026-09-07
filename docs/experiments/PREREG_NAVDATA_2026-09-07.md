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

## 2. Gate G1 - the loading chain on the small fixture (R9, 3 units, ~10 min)
Run: R9 init + R9 order on R9_Mojave_Empty_52_Nav (runner -Scenario), -NoGui, everything
else as the last green R9 run.
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
Run: COA-STP1 init + order, 4200 s, -NoGui, member consoles as P11.
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
