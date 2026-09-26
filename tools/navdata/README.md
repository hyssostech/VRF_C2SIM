# tools/navdata - generating, gating and registering navigation areas

- `nav_gate.py` - the connectivity gate. Run it on every generation log before anything uses the area.
- `make_nav_terrain.py` - registers a `.navRuntimeConfig` on a COPY of the terrain (`out/`, git-ignored).
- `osm_sector_map.py` - maps OSM highway ways onto a generated area's sectors (diagnostic).
- `landcover_sector_map.py` - land-cover class and soil per sector against the tag count (diagnostic).
- Tests: `pwsh -NoProfile -File tests\NavGate.Tests.ps1`; `python make_nav_terrain.py --selftest`.

## Generating an area (vrfNavGenerator.exe, headless)

Vendor references: the tool's own usage text (`vrfNavGenerator.exe --help`, captured 2026-09-25 to
`C:\C2SIM\vrf-nav\work\log\vrfNavGenerator-help.txt`), plus UG52 ch. 66 (66.2 p1276, 66.2.1 p1277, 66.3 p1283,
66.5 p1287). No PDF documents the command line. Own record: docs/experiments/PREREG_NAVCONTROL_WEST20_2026-09-15.md
:462-470 (the invocation) and PREREG_ASSEMBLY_LAYOUT_2026-09-07.md sec 3g (gen1-gen4).

    cwd  C:\MAK\vrforces5.2d\bin64
    env  PATH = C:\MAK\vrforces5.2d\bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin;<PATH>
         MAK_VRFDIR=C:\MAK\vrforces5.2d  MAK_VRLDIR=C:\MAK\vrlink5.10
         MAKLMGRD_LICENSE_FILE = the User-scope value (the Machine scope may name an expired file)
    vrfNavGenerator.exe --terrain "<.mtf>" --config "<cfg>\<AREA>.navGenConfig" --outputPath "<navDataDir>\<AREA>"
         --verbose --navDataDir "<navDataDir>" --userDataDir "C:\C2SIM\vrf-nav\userdata"
         --logFileName "<log>\gen-<AREA>.log"   > stdout file (never a pipe)

Rules, each from a recorded failure:
- `--verbose` is a bare SWITCH. `--verbose 1` is refused (WEST20 :282-289). Log headers print "--verbose 1" anyway.
- The tool EMPTIES `--outputPath` first. Keep the config and the log outside it (ASSEMBLY_LAYOUT gen1).
- Always pass `--navDataDir`. The default is $(SHARED_DATA_DIR)/TerrainData/navData, which is under C:\MAK (gen1).
- PRE-CREATE the `--navDataDir` folder (or pass `--runtimeConfigPath <file>`). On 2026-09-25 the folder did not exist
  when the tool wrote the .navRuntimeConfig. The log said "DtReaderWriterFile::putSelf() : Could not open file ...
  for writing", and the run finished with NO runtime config. Leading explanation, not yet re-tested: the write does
  not create parent folders.
- The .navRuntimeConfig FILE NAME follows the --config base name, not the --outputPath area name (2026-09-26: config
  "...MojaveAO20.navGenConfig" + output "...MojaveAO20_bmland2sand" wrote "...MojaveAO20.navRuntimeConfig" whose
  nav-data-path names the _bmland2sand folder). Name the .navGenConfig after the area, or pass --runtimeConfigPath.
  With --navDataDir pre-created the file WAS written (it was not on 2026-09-25, when the folder did not exist yet).
- Pass `--userDataDir C:\C2SIM\vrf-nav\userdata` (owner decision 2026-09-25). At the end of a run the tool MOVES every
  per-sector .ClientInput intermediate to <userData>\NavDataDebug. The default <userData> is ..\userData, i.e.
  C:\MAK\vrforces5.2d\userData. On 2026-09-25 that was 1,600 files / 10.8 GB for a 20 x 20 km area. During the run
  the intermediates sit in --outputPath, so expect ~10 GB there mid-run.
- Do NOT pass `--appDataDir` (WEST20 :47-49; owner keeps that). Known consequence: the tool writes its streamed-terrain
  tile cache to C:\MAK\vrforces5.2d\appData\cache\vrfsim (144 MB on 2026-09-25).
- MAX_PATH: keep the path prefixes short. gen3 failed at 338 characters. The output tree under C:\C2SIM\vrf-nav is
  short enough without a junction. Long scratch folders are not.
- Never run the generator during a live run; it is CPU-heavy (WEST20 :129-134).
- A regeneration is a NEW artefact. On MAK Earth (online) the streamed terrain input changes over time (WEST20 PART 2),
  so the old area's numbers do not carry over. Gate it again.

## Gating

    python tools\navdata\nav_gate.py <gen.log> --area-dir "<navDataDir>\<AREA>" --area-hash [--json]

Exit 0 = every sector's ratio (Average Neighbor Node Count / (Average Node Count - 1)) >= 0.9. Exit 1 = at least one
sector fails; the failing sectors are listed. Exit 2 = nothing parsed. Record the byte size, the manifest sha256 and
the nav-tag histogram alongside the result.

## Registering

    python tools\navdata\make_nav_terrain.py --runtime-config "<navDataDir>\<AREA>.navRuntimeConfig" --out "<copy .mtf>"

The copy names the runtime config by absolute path, so the data must stay where it was generated.

## Diagnostics: OSM ways per sector (osm_sector_map.py)

    python tools\navdata\osm_sector_map.py --log <gen.log> --tiles <dir> [--fetch] [--json out.json] [--map]

Inputs: the generation log (the "Adjusted -" corners, the "CalculateTransitionPointLocations extent" line, the
"Sector (i,j)" rows and the per-sector "Generated N distinct nav tags." lines) and the z14 OSM vector tiles of
mbtiles/osm/ (land-cover roads) and mbtiles/osm-highways/ (MAK_ROAD volumes), stored as <set>/14_<x>_<tmsy>.pbf.
The server uses TMS rows (y from the south: tms_y = 2^z - 1 - xyz_y); a 404 is an empty tile.
The sector frame is ENU about the centroid of the adjusted corners, shifted east so the corners land on the
log's extent. The shift exists because the true origin (the runtime "offset") is only in the .navRuntimeConfig,
which the 2026-09-25 run did not write; for AO20 it is +21.5 m (half a 43 m cell) and the corners then match to
<= 0.05 m. It is fitted to one log line, so treat sub-cell placement as assumed.
Record: docs/experiments/FINDING_NAV_TAGS_OSM_REFUTED_2026-09-26.md.

Same frame, for land cover: `python tools\navdata\landcover_sector_map.py --log <gen.log> --tiles <dir> [--fetch]`
(global-geodetic PNG TMS 154 CA-FVEG / 165 NLCD / 188 Copernicus; the server serves curl, not Python's default
user agent). Record: FINDING_NAV_TAGS_OSM_REFUTED_2026-09-26.md secs 7-8.

## A terrain-specific land-cover map (make_landcover_map.py)

Release Notes VRF-7074 (p64): a <terrain>.landCoverDataSurfChar.map overrides the global
appData\settings\vrfSim\landCoverDataSurfChar.map for that terrain. To use one without writing under C:\MAK, work on a
terrain COPY:

    python tools\navdata\make_landcover_map.py --out "tools\navdata\out\<name>.mtf" --set BM_LAND=sand

It copies the vendor .mtf and <terrain>.surfChar.map beside each other as <name>.*, and writes
<name>.landCoverDataSurfChar.map = the installed global map with only the --set lines changed (each must hit exactly
one Match line). Pass the copy to vrfNavGenerator --terrain. The derived map is not committed; the script is.
Record: docs/experiments/PREREG_NAVMAP_BMLAND2SAND_2026-09-26.md.

## A sub-box on an edited biome (make_tree_control.py)

The MAK Earth (online) .earth resolves its includes by RELATIVE path, so an edited copy of one catalog file needs a
mirror, not absolute includes: `make_tree_control.py shadow` copies TerrainData\TerrainConfiguration to
C:\C2SIM\vrf-nav\shadow and junctions every other SharedData\19\latest entry to the vendor folder, then applies
one --swap BIOME:OLD=NEW asset edit; `terrain` writes a .mtf copy whose .earth <myFilename> names the shadow copy;
`box` writes a .navGenConfig for sectors i0..i0+n-1 / j0..j0+n-1 of the AO20 grid (tile-count n keeps 11 cells
per sector). Record: docs/experiments/PREREG_NAVTREES_YUCCA_2026-09-26.md.
The generator does NOT keep a sub-box on the parent grid: it widened 55 x 55 cells to 57 x 57, centred on its own
offset, half a cell off AO20's (2026-09-26). Compare sub-box sectors with the parent's as ~91 % shared ground, or
generate the unedited control on the SAME sub-box config - that is the like-for-like comparison.

Full-area result on the shadow (DSS -> mesquite) terrain, 2026-09-26: 0 of 1,600 sectors below 0.5 (was 189) but the
gate still FAILS on four remainder-column sectors (39,34/35/36/38) that hold class 62 Joshua Tree (JST biome, also
YuccaPalm, not edited); nothing registered. Record: docs/experiments/PREREG_NAVAO20_MESQUITE_2026-09-26.md.
