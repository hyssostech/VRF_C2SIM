# PREREG NAVCONTROL WEST20 - the owed OFFLINE control: does a FRESH 20 x 20 km area over the SAME western ground fragment?
# (prediction section written 2026-09-15 03:40Z, BEFORE the generator was started; RESULTS appended after)
# Supervisor notes: this is the control PREREG_V7_AO20 RESULTS "Caveats" left owed - AO20 was generated 2026-09-07 and
# MojaveCOA 2026-09-13, so the V7 run changed area SIZE and generation DATE together. One variable here: a 20 x 20 km
# area over the western ground, generated TODAY, same terrain, same profile, same generator binary. P1 HOLDS -> C1 stands
# with the date effect excluded. P1 MISSES -> the AO20 result was a streaming-state artefact and C1's "size" reading is
# FALSIFIED; STOP, do not adjust.

Tier: HEAVY (the result feeds a cause claim - C1 of docs/experiments/RESEARCH_ABSTRACT_GRAPH_CONNECTIVITY_2026-09-15.md).
Gate: PREREG (this document). Nothing was launched, no fixture was deployed, no .mtf was edited, nothing was written under
C:\MAK, and no vendor sim log was opened.

## 0. THE CLAIM UNDER TEST AND THE HOLE IN ITS EVIDENCE

C1 (RESEARCH_ABSTRACT_GRAPH_CONNECTIVITY sec 7): the oversized area is what fragmented the western abstract graphs.
MojaveCOA is 41 x 54 km, 5.5x the maximum UG52 66.2 p1276 states for the default raster precision ("THE MAXIMUM SIZE FOR A
NAVIGATION AREA USING THE DEFAULT VALUES IS 20 KM BY 20 KM"); 254 of its 8,856 sectors have an abstract-graph connectivity
ratio below 0.5, clustered on 1-35's lane; MojaveAO20, 20.08 x 19.95 km, has 0 of 1,600.

The hole, pre-registered by PREREG_V7_AO20 itself and repeated in its RESULTS adversarial paragraph: **AO20 was generated
2026-09-07 and MojaveCOA on 2026-09-13.** Size and generation date moved together, so a generation-date / terrain-
streaming-state effect (the online MAK Earth terrain is streamed and cached during generation; its cache state and the
served tiles are not held constant across six days) rides along with the size change and cannot be separated by any run.
The V7 live run could not close it either - swapping the area swaps both.

**The separation is offline and is exactly the falsifier the research named for C1**: "if a fresh 20 x 20 km generation
over the same western ground also produces sectors with ratio < 0.5, size is not the operative variable."

## 1. DOCS AND RECORDS CONSULTED (read this session, before writing any prediction)

- docs/experiments/RESEARCH_ABSTRACT_GRAPH_CONNECTIVITY_2026-09-15.md - H1-H5, sec 3 (the metric), sec 4.2 (the vendor's
  own 33 shipped areas: the largest ground-platform one is 10 x 10 km), sec 4.3 (the generator has NO abstract-graph and
  NO working-memory option; the only levers are extent, tile-count, cell-size, raster-precision), sec 7 (C1/C2/C3 and
  their falsifiers), sec 11 (the adversarial review of the metric itself).
- docs/experiments/PREREG_V7_AO20_2026-09-15.md - the run, its four hits, and the Caveats/adversarial paragraph that
  owes this control.
- docs/experiments/NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE_2026-09-14.md - the abstract-graph and slope reading behind the
  lane analysis.
- docs/experiments/PREREG_NAVDATA_2026-09-07.md sec 1 - how MojaveAO20 was specified and generated (20 x 20 km, north
  edge 2 km north of the STP assembly point, centred on lon -116.70, tile-count 40 x 40, raster 0.2, cell-size 43,
  profile ground-platform, through the short junction for MAX_PATH; 1,436 s wall, 1,600 sectors, 166 MB).
- docs/experiments/PREREG_NAVDATA_G6_2026-09-13.md - how MojaveCOA was specified and generated (108 x 82, 11,952 s).
- docs/experiments/PREREG_ASSEMBLY_LAYOUT_2026-09-07.md sec 3g - the four generator launches and every trap they found:
  --outputPath is EMPTIED by the tool (gen1 deleted its own config and its log), MAX_PATH kills it at ~338 characters
  (gen3), --navDataDir must be passed or the runtime config is written under C:\MAK (gen1), and the DEMO licence covers
  CREATION, not only regeneration (gen4).
- docs/experiments/APPDATA_RELOCATION_2026-09-14.md - the relocated tree C:\C2SIM\vrf-appdata\appData differs from the
  vendor tree in exactly one RUNTIME setting (loadAllNavigationDataOnTerrainLoad) and is irrelevant to generation;
  --appDataDir is therefore NOT passed, exactly as in the AO20 and COA generations.
- UG52 66.2 p1276 / 66.2.1 p1277 / 66.2.3 p1280 (the 20 km maximum, sectorization, "generation of large unsectorized
  navigation areas sometimes fails"), via the extracts already in the record.
- The two generation logs' own "Command line arguments:" headers - the exact CLI of the AO20 and COA runs, recovered
  rather than recalled.

## 2. THE CONTROL - ONE VARIABLE

Held constant against the MojaveAO20 generation of 2026-09-07:

| held constant | value | evidence it is the same |
|---|---|---|
| terrain | C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\MAK Earth (online).mtf | the same --terrain string as gen-AO20.log and gen-COA.log |
| generator binary | C:\MAK\vrforces5.2d\bin64\vrfNavGenerator.exe, 420,352 bytes, LastWrite 2026-01-06 01:38:40Z | install-dated, never patched since |
| navigation profile | ground-platform from C:\MAK\vrforces5.2d\appData\settings\vrfSim\navigationProfiles.mtl, SHA256 65C09CC7C8E7B262B091948F153A3737E74D8297C5B73CE2783ABABE2CDDC02D, LastWrite 2026-01-06 01:23Z | vendor-original; the relocated appData copy hashes identically |
| feature config | appData\settings\featureconfig.txt, LastWrite 2026-01-06 01:23Z, vendor-original | --appDataDir NOT passed, so the vendor tree is read, as in both prior generations |
| generation parameters | tile-count 40 x 40, allow-abstract-data True, generate-full-terrain False, generate-transition-points True, prune-no-go-areas True, raster-precision 0.200000, cell-size 43, profiles-to-generate ground-platform | byte-identical block to the AO20 .navGenConfig |
| area SIZE | 19,930.5 m N-S x 20,021.6 m E-W | the SAME degree spans as AO20 (dlat 0.179662235, dlon 0.218286800) |
| corner height | h = 700 m, WGS84 | decoded from both existing configs; the same constructor |

Changed, on purpose: **where the box sits** (shifted 0.12 deg = 11.0 km west, same latitude band) and **the date**
(2026-09-15 instead of 2026-09-07). The date cannot be held constant - that is the whole point: the fresh generation
carries TODAY's terrain-streaming state, so if the western ground still comes out clean, the 2026-09-07 AO20 result was
not a date artefact.

## 3. THE AREA - "NavArea-ground-platform MojaveWest20" (geometry computed before generation)

Latitude band: **identical to MojaveAO20**, 34.518288963 .. 34.697951198 (the same two parallels that produced 0 of 1,600).
Longitude band: 34.60812 / -116.82 centre, -116.929143400 .. -116.710856600.
Spans 19,930.5 m N-S x 20,021.6 m E-W, both equal to AO20's to the metre. Sectors 40 x 40 = 1,600, 11 cells of 43 m each.

ECEF corners written into the .navGenConfig (WGS84, h = 700 m):

    (extent-nw  -2377731.163336 -4680861.689931 3610766.185708)
    (extent-ne  -2359880.694131 -4689886.433041 3610766.185708)
    (extent-sw  -2382858.312621 -4690955.125655 3594360.432972)
    (extent-se  -2364969.352092 -4699999.329000 3594360.432972)

Coverage of the named points, computed before generation (margins in metres to the N / S / E / W edges):

| point | lat / lon | in? | N | S | E | W |
|---|---|---|---|---|---|---|
| 1-35 destack start (N2b/N2c) | 34.658442 / -116.740092 | IN | 4,383 | 15,548 | 2,682 | 17,340 |
| 1-35 N2d start | 34.657894 / -116.745512 | IN | 4,444 | 15,487 | 3,179 | 16,843 |
| P11/G3/G5/G6 freeze point | 34.65608 / -116.76142 | IN | 4,645 | 15,286 | 4,638 | 15,384 |
| V0 assembly origin | 34.679985 / -116.724799 | IN | 1,993 | 17,937 | 1,279 | 18,743 |
| V1 | 34.651212 / -116.811637 | **IN** (AO20: 206 m OUT) | 5,185 | 14,746 | 9,244 | 10,778 |
| V2 | 34.596351 / -116.952329 | OUT by 2,127 m west | - | - | - | - |
| V3 | 34.570294 / -117.006223 | OUT by 7,070 m west | - | - | - | - |

**V2 and V3 cannot be held by any 20 km square that also holds V0**, and that is arithmetic, not a choice: the V0-to-V2
E-W span is 20,869 m and V0-to-V3 is 25,813 m, against a 20,022 m maximum edge. The box is therefore biased as far west as
V0's containment allows (V0 keeps a 1,279 m = 2.7-sector margin from the east edge). The V1-to-V2 leg leaves the west edge
at lat 34.6054, 9.7 km above the south edge, so the whole of the V0 -> V1 leg and the first 10.5 km of the V1 -> V2 leg
are inside.

Config written before generation to (and generated through the short junction C:\Users\PAULOB~1\Temp\navc, which points
at the same directory - the MAX_PATH trap of gen3):

    C:\C2SIM\vrf-nav\control-west20-2026-09-15\cfg\NavArea-ground-platform MojaveWest20.navGenConfig   (580 bytes)

## 4. THE COMMAND LINE (recorded before running; the AO20/COA line with only the paths changed)

    cwd  : C:\MAK\vrforces5.2d\bin64
    env  : PATH = C:\MAK\vrforces5.2d\bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin;<Machine PATH>
           MAK_VRFDIR=C:\MAK\vrforces5.2d   MAK_VRLDIR=C:\MAK\vrlink5.10
           MAKLMGRD_LICENSE_FILE resolved from the USER scope (RUNBOOK 0.5.15) =
             C:\MAK\MAKLicenseManager\SALES-TEMP-10-31-26-MAK-node-locked-DEMO_1-dec-2025.lic
    exe  : C:\MAK\vrforces5.2d\bin64\vrfNavGenerator.exe
           --terrain      "C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\MAK Earth (online).mtf"
           --config       "C:\Users\PAULOB~1\Temp\navc\cfg\NavArea-ground-platform MojaveWest20.navGenConfig"
           --outputPath   "C:\Users\PAULOB~1\Temp\navc\navData\MAK Earth (online)\NavArea-ground-platform MojaveWest20"
           --verbose      1
           --navDataDir   "C:\Users\PAULOB~1\Temp\navc\navData\MAK Earth (online)"
           --logFileName  "C:\Users\PAULOB~1\Temp\navc\log\gen-WEST20.log"

The config and the log live OUTSIDE --outputPath on purpose (gen1 lost both to the tool's own emptying of that
directory). --navDataDir is passed so no .navRuntimeConfig is written under C:\MAK (gen1). Nothing under C:\MAK is
written by this run; the only writes are under C:\C2SIM\vrf-nav\control-west20-2026-09-15.

Scheduling: the generator is CPU-heavy and a concurrent load already produced one false headline in this project (the
2026-09-13 G2 "engine collapse" was my own agent load, not the mesh). The generator is therefore held behind the live
lane: the coordinator's rule of 2026-09-15 03:35Z / 03:41Z is that it starts only on an explicit release, or at 04:45Z
if none arrives. The wall-clock start, and whether any VR-Forces back end was up at that instant, are recorded in the
RESULTS - a forced start beside a live run is a caveat on THAT run, not on this generation, but it is recorded either
way.

## 5. THE METRIC AND THE INSTRUMENT (both validated on the OLD logs before the new run)

Metric, unchanged from RESEARCH_ABSTRACT_GRAPH_CONNECTIVITY sec 3:

    ratio = Average Neighbor Node Count / (Average Node Count - 1)

from each sector's ABSTRACT GRAPH POST PROCESS REPORT in the generation log. ratio = 1.00 iff every abstract node
reaches every other; "fragmented" = ratio < 0.5, the research's threshold.

Instrument: scratchpad\navcontrol\parse_area.py - a generic re-implementation of parse_gen.py / parse_ao20.py / cover.py
that takes any generation log plus that area's .navRuntimeConfig (for the ENU offset) and reports the per-sector ratios,
the histogram, and the sector under any lat/lon. **It was run on both existing logs FIRST and reproduces the published
numbers exactly** - that is the "check the instrument reproduces before interpreting any result" rule, and it is done:

| area | sectors | ratio < 0.5 | median ratio | median NavData | destack start | N2d start | V1 |
|---|---|---|---|---|---|---|---|
| MojaveCOA (2026-09-13, 41 x 54 km) | 8,856 | **254** | 1.0000 | 60.5 kB | (54,79) **0.111** | (53,79) **0.289** | (40,78) **0.071** |
| MojaveAO20 (2026-09-07, 20 x 20 km) | 1,600 | **0** | 1.0000 | 59.9 kB | (13,32) 1.000 | (12,32) 1.000 | not covered |

**One correction to the record, found by this pass and registered before the run**: the P11/G3/G5/G6 freeze point
(34.65608/-116.76142) falls in COA sector (50,79), whose ratio is **1.000** (49 nodes, 48 neighbours, 78.7 kB). The
freeze point's own sector was never fragmented on MojaveCOA. It is kept in P1 because the brief names it, but it carries
no discriminating power: the discriminating sectors are the destack start (0.111), the N2d start (0.289) and - newly
inside the box, and the strongest fragmenter of the three - V1 (0.071), which AO20 could not test at all.

**Sector geometry, measured on both existing areas so the fresh one can be read against it.** The generator lays 11-cell
(473 m) sectors and lets the LAST row and column absorb the remainder: AO20 is 39 x 11 cells plus one 39-cell column and
one 36-cell row; MojaveCOA is 107 x 11 plus one **90-cell (3,870 m)** column and one 82-cell row. WEST20, at 40 x 40 over
465 cells, will have 39 x 11 plus a ~36-cell remainder - AO20's shape, not COA's. A fragmented sector found in the
remainder row or column is therefore NOT the same observation as one found in a regular 11-cell sector, and the RESULTS
will say which kind each is. All four named points sit in regular interior sectors (V0 is 4.2 sectors below the north
edge, the destack start 9.3).

**The one documented generation-side mechanism, so the prediction is not just a correlation.** Autodesk Navigation SDK,
"Creating AbstractGraphs" (quoted in NAVDOCS sec 2.1): "GeneratorAbstractGraphParameters::m_workingMemorySizeLimit
parameter gives the size limit (in Bytes) of the WorkingMemory", and m_extentsInNumberOfCells "controls the cell box
length each AbstractGraph covers". VR-Forces exposes neither (RESEARCH sec 4.3), so both are at their built-in values for
every area we generate. A FIXED per-graph working-memory budget applied to a sector whose NavMesh came out 8-9x larger is
a mechanism that would produce exactly the observed signature - a complete NavMesh with a shattered abstract graph on top
of it - and it predicts that fragmentation should track NavData SIZE, which is what the research measured (navkb > 400 kB
-> 98 % fragmented). That makes the NavData size of the named sectors a second, independent pre-registered measurement,
and it is what P1c scores.

## 5b. THE ANALYSIS, PRE-REGISTERED (so nothing here is chosen after seeing the numbers)

1. `parse_area.py <gen-WEST20.log> <MojaveWest20.navRuntimeConfig> WEST20` - whole-area counts, the ratio histogram over
   the same bins used for AO20 and COA, and the sector under each of the eight named lat/lons.
2. `cmp3.py` with WEST20 as the reference area against BOTH existing areas - a matched-ground table over the
   ~1,600 sectors WEST20 shares with MojaveCOA and the ~720 it shares with MojaveAO20 (their overlap is the 9.0 km
   longitude strip -116.809143 .. -116.710857, full height). This is the same instrument that reproduces H3's
   1,560-sector AO20-vs-COA control, run today against fresh data.
3. The comparison rows reported are, for each named point and for the matched populations: ratio, NavData kB, input
   triangle count. No other statistic is introduced after the fact; anything additional is labelled as such.

**The baseline over exactly this footprint, computed before the run** (every sector of an existing area whose centre
falls inside the WEST20 box):

| existing area | sectors inside the WEST20 box | ratio < 0.5 | ratio < 0.9 | median ratio | median NavData | median tris |
|---|---|---|---|---|---|---|
| MojaveCOA (41 x 54 km, 2026-09-13) | 1,470 | **162 (11.0 %)** | 296 | 1.0000 | 62.5 kB | 98,888 |
| MojaveAO20 (20 x 20 km, 2026-09-07) | 760 (its overlap) | **0 (0.0 %)** | 0 | 1.0000 | 60.2 kB | 98,754 |

That 11.0 % is the point of this box: this ground carries nearly four times COA's area-wide fragmentation rate of 2.87 %.
COA contributes 1,470 rather than 1,600 because its north edge (34.689) is 1.0 km SOUTH of the shared north edge
(34.697951), so WEST20's top two sector rows have no COA counterpart; AO20 contributes only its 760-sector overlap strip.

## 6. PREDICTIONS (written before the generator was started; a missed HIGH prediction is a STOP)

- **P0 (HIGH, instrument).** The generation completes on its own: 1,600 "Sector (i,j)" blocks, 1,600
  "^Generated: ground-platform" rows, a "Generation time:" line, exit code 0, and no licence failure and no GUI
  requirement. MISS -> STOP and report; no workaround, no retry with different parameters.
- **P1 (HIGH, THE TEST).** In the fresh area's ABSTRACT GRAPH POST PROCESS REPORT, the sectors containing the
  **destack start, the N2d start and the P11 freeze point** all have ratio >= 0.5 - i.e. **0 of those 3 sectors is
  fragmented**, against 2 of 3 on MojaveCOA (0.111 and 0.289). Stated in the strong form the research uses: those
  sectors read 1.00 or near it.
- **P1b (HIGH, the strongest single discriminator, new to this control).** The sector containing **V1** has ratio
  >= 0.5. On MojaveCOA it is 0.071 - the worst of the four named points - and AO20 did not cover V1 at all, so this is
  the one datum no existing area can supply.
- **P1c (MEDIUM, the independent second measurement).** The NavData size of the destack-start and N2d-start sectors in
  the fresh area is AO20-like, not COA-like: **under 150 kB** (AO20 read 55.9 kB and 62.1 kB on that exact ground; COA
  read 474.9 kB and 385.6 kB), and their input-triangle counts are near the area median (AO20 98,576 / 98,978 on that
  ground; COA 114,178 / 112,833 - 15 % higher). This scores the mechanism, not just the outcome: a COA-like 400 kB sector
  WITH a clean ratio, or an AO20-like 60 kB sector WITH a shattered ratio, would each break the size-of-the-mesh reading
  even if P1 itself passes, and either is reported as a finding rather than smoothed over.
- **P2 (MEDIUM).** The whole fresh area has **0 sectors with ratio < 0.5**, as AO20 had (0 of 1,600), against the 162
  (11.0 % of 1,470) MojaveCOA has over this exact footprint. A small non-zero count away from the four named points
  weakens but does not kill C1; a count anywhere near 162, or any clustering of fragmented sectors back onto 1-35's lane,
  is a P1-level miss whatever the four named sectors read. Reported as measured either way, split into interior and
  remainder-row/column sectors.
- **P3 (RECORDED, not scored).** Generation wall time (AO20 1,435.8 s for the same size; COA 11,952.3 s), sector /
  cell count (expect 1,600 sectors), file count and bytes (AO20 4,803 files / 166 MB), the warning-ish line count over
  the whole log (AO20 4, COA 5, by the pattern warn|error|fail|cannot|could not|unable), the median NavData size and
  the median input-triangle count (AO20 59.9 kB / 98,760 tris; COA 60.5 kB / 98,739).

**Reading.**
- P1 and P1b HOLD -> **C1 stands with the generation-date effect excluded**: the same western ground, generated six days
  later under today's terrain-streaming state, is clean inside 20 km and shattered inside 54 km, so SIZE (or something
  that travels with it - see sec 8) is the operative variable and the product rule (cap at 20 x 20 km, tile the AO, gate
  on the ratio) is right for STP-802/803.
- P1 MISSES - the same ground fragments in a fresh 20 km area -> **the 2026-09-07 AO20 result was a streaming-state /
  date artefact and C1's "size" reading is FALSIFIED.** The object becomes the ground and the streaming state, not the
  extent. STOP and report; do not adjust the area, the profile, the threshold or the parameters to recover a pass.
- P1 holds and P1b misses -> the fragmentation is real but not purely a size effect at the V1 end; record it and stop
  before generalising.

## 7. WHAT COUNTS AS A STOP

A missed HIGH prediction (P0, P1, P1b). No parameter is adjusted to make one pass. If the generator demands the GUI,
fails its licence checkout, or refuses the extent, that is a STOP and a report - no workaround.

## 8. CONFOUNDS THIS CONTROL CANNOT REMOVE (pre-registered, so the RESULTS cannot quietly drop them)

1. **Size and per-sector INPUT COMPLEXITY are not separated by this design either.** The 20 km box and the 54 km box
   cover different amounts of ground, so "the area is smaller" and "the generator's per-sector inputs are smaller/
   different" move together. What this control does remove is the DATE; what it cannot remove is whatever else scales
   with extent (working memory during generation, sector-graph budgets, the order tiles are streamed in).
2. **Terrain tile cache state.** MAK Earth (online) is streamed. The 2026-09-13 COA generation and this one both run
   against a warm-ish local cache, but neither cache state was measured, and this run's cache has been touched by every
   run since. If P1 holds, this is an argument the fresh result is CONSERVATIVE (today's cache is the one that produced
   the fragmented COA area); if P1 misses, cache state becomes a live candidate.
3. **One generation, no repeat.** n = 1, as for AO20 and COA. Generation is not proven deterministic on a streamed
   terrain; a second 20 km generation over the same box is the repeat this record does not have.
4. **The metric is measured at GENERATION, not at query time.** Carried from the research's own sec 11.6: the runtime
   could in principle repair or worsen what the generator wrote. Only a live run reads the query side, and this is an
   offline control by design.
5. **The generator's own version is held constant but is a single sample.** Everything here speaks about this binary
   (2026-01-06) on this terrain; nothing generalises to other MAK builds.
6. **ADDED 11:14Z, after the generator was started and before any result was read** (so it is a confound, never a
   prediction): **Windows rebooted at 04:31:22Z**, between the prereg being written and the generation being released.
   This generation therefore runs in a FRESH process tree on a COLD in-memory state, and the MAK Earth terrain tile
   cache is cold in RAM (the on-disk cache survives). MojaveAO20 and MojaveCOA were each generated in a
   long-lived, warmer session. If P1 holds, the reboot only strengthens the reading (the clean result was obtained under
   the LESS favourable cache state); if P1 misses, the cold cache joins the streaming state as a live candidate and must
   be separated before anything is concluded about the ground.

---

## RESULTS - generation 2026-09-15 11:12:02Z -> 11:36:42Z (exit 0, 1,480.0 s wall; the generator's own "Generation time: 1429.81")

**P1 MISSES. P1b, P1c and P2 MISS. C1's "size" reading is FALSIFIED as stated, and the stop rule applies: nothing was
adjusted after seeing this.** The fresh, fully compliant 20 x 20 km area, generated today over the western ground with
MojaveAO20's extents held to the metre, is **as fragmented as the 5.5x-oversized MojaveCOA** - and it fragments the very
ground MojaveAO20 rendered perfectly clean eight days earlier. The operative variable is not the area extent. It is the
**terrain input the generator was served**, and that changed between 2026-09-07 and 2026-09-13 and has not changed back.

### The run
Command line exactly as registered in sec 4, with ONE correction found by the run itself: `--verbose` is a **switch**,
not a valued option. Attempt 1 (11:11:28Z) passed `--verbose 1`, was refused in 1.0 s with
`PARSE ERROR: Argument: 1 / Couldn't find match for argument`, and wrote nothing. The `--verbose 1` line in gen-AO20.log
and gen-COA.log is the generator's own RENDERING of its parsed options, not the literal argv - reading it as a command
line was the error. Attempt 2 (11:12:02Z, pid 41584) passed a bare `--verbose`, and its log header prints `--verbose 1`
just the same, which verifies the diagnosis rather than assuming it. Nothing was written under C:\MAK; the only writes
are under C:\C2SIM\vrf-nav\control-west20-2026-09-15 (4,802 files, 283.9 MB, plus the 5.4 MB generation log).
Windows had rebooted at 04:31:22Z (confound 6): fresh process tree, cold in-memory tile cache, no VR-Forces back end
running at the start.

### Scored predictions

| pred | predicted | measured | verdict |
|---|---|---|---|
| **P0** (HIGH, instrument) | generation completes on its own: 1,600 sector blocks, 1,600 "^Generated: ground-platform", a "Generation time:" line, exit 0, no licence failure, no GUI | exit 0; **1,600** sector blocks; **1,600** "^Generated: ground-platform"; "Generation time: 1429.81"; 4,802 files / 283.9 MB; 4 warning-ish lines, the same four as AO20 ("Could not determine index file name for layer ..." x2 each); no licence error, no GUI requirement | **HIT** (after the 1.0 s parse-error abort above, which wrote nothing) |
| **P1** (HIGH, THE TEST) | the destack-start, N2d-start and P11-freeze sectors all read ratio >= 0.5 - 0 of 3 fragmented | destack start sector **(36,32) ratio 0.094** (54 nodes, 5 neighbours, 443.6 kB); N2d start **(35,32) ratio 0.548** (43 nodes, 23 neighbours, 348.2 kB); P11 freeze **(32,32) ratio 1.000** (52/51, 79.9 kB). **1 of 3 below 0.5**, and the second is barely above it against a COA baseline of 0.289 | **MISS** |
| **P1b** (HIGH, the discriminator AO20 could not supply) | V1's sector reads >= 0.5 | V1 **(22,31) ratio 0.043** - 48 nodes, **2** neighbours, 481.7 kB. **Worse than MojaveCOA's 0.071 on the same ground** | **MISS** |
| **P1c** (MEDIUM, the mechanism) | the destack-start and N2d-start sectors come out under 150 kB with near-median triangle counts (AO20 read 55.9 / 62.1 kB and 98,576 / 98,978 tris there) | **443.6 kB / 113,609 tris** and **348.2 kB / 111,360 tris** - COA-like to within 7 % (COA: 474.9 kB / 114,178 and 385.6 kB / 112,833), nothing like AO20 | **MISS** |
| **P2** (MEDIUM) | the whole fresh area has 0 sectors below 0.5, as AO20 had | **234 of 1,598** graphed sectors below 0.5 (**14.6 %**), 409 below 0.9; **only 5 of the 234 lie in the oversized remainder row/column**, so this is not an edge artefact. Median ratio is still 1.0000 and 1,118 sectors are exactly 1.00 - the damage is local and clustered, exactly as on COA | **MISS** |
| **P3** (RECORDED) | time, counts, warnings, medians | generator time 1,429.81 s (AO20 1,435.8 s for the same size; COA 11,952.3 s) - **the same size takes the same time**; 1,600 sectors; 4,802 files vs AO20's 4,803, but **283.9 MB against AO20's 166 MB**; 4 warning-ish lines (AO20 4, COA 5); median NavData **68.0 kB** (AO20 59.9, COA 60.5); median input triangles 98,978 (AO20 98,760, COA 98,739); max NavData 1,449.7 kB; max triangles 1,122,862 | **RECORDED** |

### The matched-ground control - this is what falsifies C1

Sector grids are cell-aligned between the areas, so the comparison is exact, not approximate: over ground whose terrain
input did not change, matched pairs agree to hundredths of a kB (AO20 (0,0) 55.24 kB vs WEST20 (23,0) 55.23; (0,1) 55.73
vs 55.70; (0,3) 55.72 vs 55.70; (0,4) 55.70 vs 55.70).

| comparison | what differs | matched sectors | fragmented (ratio < 0.5) |
|---|---|---|---|
| **WEST20 vs MojaveAO20** | **only the date** (2026-09-15 vs 2026-09-07) - same ground, same 20 km extents, same profile, same binary | 679 | **WEST20 135  /  AO20 0** |
| **WEST20 vs MojaveCOA** | **only the size** (20 x 20 km vs 41 x 54 km), two days apart | 1,558 | **WEST20 231  /  COA 222** |

Read them together: **holding size constant and changing the date flips 135 sectors from clean to shattered; holding the
date roughly constant and changing the size by 5.5x changes essentially nothing (231 vs 222).** The whole-footprint
baseline registered before the run was COA 162 of 1,470 (11.0 %) and AO20 0 of 760; the fresh 20 km area returns 234 of
1,598 (14.6 %) - it lands on the COA side, not the AO20 side.

### What actually changed: the terrain input, measured sector by sector on identical ground

Comparing WEST20 against MojaveAO20 over their 679 shared sectors:

- **215 sectors (32 %) gained more than 5 % input triangles since 2026-09-07. 123 of those (57 %) are fragmented today -
  and 0 of them were fragmented on 2026-09-07.**
- **434 sectors are within +/- 2 % of their 2026-09-07 triangle count. Only 4 of them are fragmented today.**
- Sectors fragmented today (n=135): median input triangles **98,890 -> 112,244 (+13.5 %)**, median NavData
  **58.4 kB -> 421.3 kB (x7.2)**.
- Sectors still clean today (n=544): median triangles 98,717 -> 98,918, median NavData 61.0 -> 66.3 kB. Unchanged.

And within the fresh area the dose-response is the same one the research measured on COA, so the metric is behaving
identically on new data:

| sector NavData | n | fragmented |
|---|---|---|
| under 100 kB | 1,094 | **0 (0 %)** |
| 100-300 kB | 245 | 15 (6 %) |
| 300-400 kB | 131 | 104 (79 %) |
| over 400 kB | 128 | 115 (90 %) |

(By input triangles: under 102,000 -> 1 % fragmented; 102,000-105,000 -> 25 %; over 105,000 -> 54 %.)

So the chain is: **the terrain served for part of this ground became denser between 2026-09-07 and 2026-09-13; a denser
NavMesh produces a much larger NavData blob; and above roughly 300 kB per sector the abstract graph comes out shattered.**
Extent never enters it.

### What this does and does not overturn

- **Does NOT overturn the V7 live run.** MojaveAO20's navData on disk is clean, and the run that used it planned 6 of 6
  slot moves and the 2.3 km leg. That observation stands.
- **Overturns its causal attribution and the product rule drawn from it.** "The oversized area's generation shattered the
  western abstract graphs" is false as stated: a compliant 20 km area generated today shatters the same graphs slightly
  harder than the oversized one did. **Capping generation at 20 x 20 km does not produce navigable data** - STP-802's
  tiling rule is necessary for the documented vendor limit but is NOT the fix for this failure.
- **Promotes STP-803.** The offline connectivity-ratio gate stops being a nice-to-have and becomes the only control that
  would have caught this: it is the one check that distinguishes the AO20 generation from the COA and WEST20 ones, and it
  costs one script over a log we already write.
- **Demotes "MojaveCOA is retired for navigation" from a size verdict to a data verdict.** It is retired because its
  navData is fragmented on the lane, not because it is 41 x 54 km. MojaveAO20 is usable only because it was generated on
  2026-09-07; regenerating it today would, on this evidence, be expected to fragment it.
- **Opens the real object:** what changed in the terrain input, and whether it is the online MAK Earth service's data or
  the local streamed-tile cache's level of detail. That is the next question, and it is offline.

### Adversarial review

The statement under test is "the fresh 20 km area fragments the same ground, so size is not the operative variable."

1. **"The parser or the ENU frame is wrong for the new area."** The strongest objection, and it is refuted three ways:
   the same regexes and the same script reproduce AO20's 0/1,600 and COA's 254/8,856 with the published per-point ratios
   (run before this generation, on the old logs); the fresh log parses 1,600 of 1,600 sectors with no missing
   AbstractGraph blocks; and on unchanged ground the matched pairs agree to 0.01-0.03 kB, which cannot happen if the
   frames were misaligned.
2. **"The box sits somewhere else, so the generator sampled the terrain differently - it is placement, not date."**
   Refuted by spatial selectivity: 434 of the 679 shared sectors reproduce their 2026-09-07 triangle count to within
   2 % and 430 of those are clean. A sampling or grid-alignment artefact would move all of them, not a third of them.
   The sector grids are also whole-sector offset (AO20 (0,j) maps to WEST20 (23,j)), so the cell boxes coincide.
3. **"The concurrent dotnet rebuild of nine tools perturbed the generation."** Refuted by the same clustering test that
   killed the "terrain-cache streak" reading on COA: bad-bad adjacency is **162** in the generation-consecutive direction
   (same i, j+1) against **158** in the generation-distant direction (same j, i+1) - symmetric, so the damage is spatial,
   not temporal. The rebuild also covered about 5 minutes of a 24.7-minute generation, while the fragmented blobs span
   the whole northern half of the area.
4. **"The reboot's cold tile cache is what made the mesh denser."** NOT refuted, and it is the live residual - but it
   cannot be the whole story, because **MojaveCOA on 2026-09-13 shows the same fragmentation on the same ground**
   (231 vs 222 over 1,558 sectors; 114,178 triangles at the destack start against WEST20's 113,609). Two independent
   generations two days apart agree with each other and disagree with 2026-09-07, which localises the change to between
   2026-09-07 and 2026-09-13 rather than to this morning's boot. What this pass cannot separate is **which** terrain
   input changed: the online service's data, or the local cache's level of detail. Both are "terrain input state"; the
   verdict on C1 does not depend on the answer, and the next question does.
5. **UNEXPLAINED, carried, not buried.** (a) The generator-side mechanism is not demonstrated. I show that mesh size
   predicts fragmentation and that mesh size grew; I do not show why a bigger NavMesh shatters the abstract graph. The
   documented candidate is Gameware's fixed `m_workingMemorySizeLimit` per graph (NAVDOCS sec 2.1), which VR-Forces
   exposes nowhere (RESEARCH sec 4.3), so it was not varied and not measured. (b) V0's sector (39,37) carries **346,253
   input triangles**, 3.5x the area median and by far the largest regular sector in the area, at ratio 0.850 - no account
   here covers why that one cell of ground is so dense. (c) The research's carried second mechanism (G7b run A's
   same-second success and refusal for co-located vehicles) is untouched by this pass and stays open.
6. **What would falsify THIS verdict.** Regenerate MojaveAO20's exact extents today. If it comes back with 0 of 1,600
   fragmented, then something about the WEST20 box - not the date - is responsible and this reading collapses. That is
   one 24-minute offline run, it moves only the extent, and it is the single cheapest next measurement.

### VERIFIED vs ASSUMED

**VERIFIED** (from this generation's own artefacts, the two prior generation logs, and the files on disk; no vendor sim
log was opened): every number in every table above; that the area really is 20 km - its .navRuntimeConfig reads
extent-nw/ne/sw/se (-10019, 9976) / (10062, 9976) / (-10019, -9976) / (10062, -9976), **identical to MojaveAO20's
runtime extents**, and its sector grid is identical in shape (1,560 sectors of 11 x 11 cells plus a 39-cell column and a
36-cell row); that the generator binary, navigationProfiles.mtl and featureconfig.txt are the vendor-original files that
built both earlier areas; that no VR-Forces back end was running at the start.

**ASSUMED** (not measured here): that the MojaveAO20 and MojaveCOA navData and logs on disk are faithful to what those
generations wrote (their presence and their contents were read, their hashes were never recorded); that the terrain
.mtf's referenced layers are the same FILES (the path is identical - the streamed CONTENT is exactly what is in
question and was never versioned by us); that the connectivity measured at generation is what the runtime query sees
(carried unchanged from RESEARCH sec 11.6); and that the 2026-09-07 AO20 generation ran with a warmer cache than this
one - plausible from the record but not instrumented.

### VERDICT

**C1 IS FALSIFIED AS STATED.** The oversized extent is not what fragmented the western abstract graphs. A fully
compliant 20 x 20 km area, generated today over the same ground with the same profile and the same binary, reproduces
MojaveCOA's fragmentation (231 vs 222 on 1,558 shared sectors) and shatters 135 sectors that MojaveAO20 rendered
perfectly clean eight days ago. The operative variable is the terrain input at generation time, working through NavMesh
size: over 300 kB of NavData per sector and the abstract graph comes out broken, whatever the area's extent.

Consequences, for the supervisor and for STP-802 / STP-803, stated but not enacted here: the 20 km cap stays only as
vendor-limit compliance, not as the navigation fix; the connectivity-ratio gate becomes mandatory on every generated
area; "regenerate the nav data" is not a safe idempotent operation on a streamed terrain and needs the gate plus a
recorded terrain state; and MojaveAO20's clean data should be treated as a dated artefact, not as a reproducible
baseline. The owed measurement is the falsifier in point 6 above.
