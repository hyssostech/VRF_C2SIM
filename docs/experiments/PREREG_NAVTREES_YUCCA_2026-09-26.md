# PREREG NAVTREES YUCCA - does swapping Desert Succulent Shrub's YuccaPalm for a mesquite un-fragment the mesh? (5 x 5-sector control)

Lane N6, session 5fc25950. Brief N6 ("Small control generation only"). One small offline generation. Nothing is
registered: a 5 x 5 box is a test, not an area.

## Registration

PREREG ID: navtrees-yucca-2026-09-26-1
DATE (UTC): 2026-09-26, written and committed BEFORE the generator was started
BINARY / COMMIT: C:\MAK\vrforces5.2d\bin64\vrfNavGenerator.exe (420,352 B); repo branch feat/nav-gate-and-ao20
TIER AND GATE: HEAVY / PREREG

VENDOR CITATION: UG52 ch. 66 is SILENT on vegetation, trees, biomes and the LifeMap as generation inputs - it documents
Prune No-Go Terrain Areas (Table 54 p1279) and min-navigable-surface / soil-types-to-tag-with-surface-char (66.3.1
p1282) only. Release Notes VRF-8788 (p69): "Need better mechanism to identify which vegetation affects VRFSIM ... The
simTreesTool has been updated. The default behavior is now to process only files with sim models"; VRF-9016 (p72):
simTreesTool.exe is installed and gets "the bounding box dimensions for geometry" of a model. Local files (read only):
osgEarthCatalogs\biome.definitions.CA-fveg.xml:537-544 (DSS "Desert Succulent Shrub": one asset, YuccaPalm) and
:505-517 (DSW "Desert Wash": HoneyMesquiteShortSpring, HoneyMesquiteSapling, Snakeweed, CreosoteBush, SwordGrass);
osgEarthCatalogs\simVegetation.xml:156 (YuccaPalm sim_trunk_height / sim_trunk_width "-inf") and :80-81
(HoneyMesquite trunks 0.06 / 0.30 m). The DLL string "Sim model config: ... not found." sits in
vantageTerrainImplementation.dll next to DtOsgEarthVegetationLayerSourcePager::createTree (strings, not documentation).

OWN-RECORD CITATION: laneQ3_mesh_carving_research.md (session scratch, 2026-09-26: all 189 fragmented sectors contain
class 64; dryground without 64 never fragments; dose-response on the class-64 share; tags = distinct soils + 1 if a
tree-bearing biome is present); PREREG_NAVMAP_BMLAND2SAND_2026-09-26.md Result (removing the soil tag left NavData and
ratios byte-identical); FINDING_NAV_TAGS_OSM_REFUTED_2026-09-26.md; PREREG_NAVCONTROL_WEST20_2026-09-15.md:462-470
(the invocation); tools/navdata/README.md (the traps). The generator logged "Sim model config: YuccaPalm not found."
5x in gen-AO20-2026-09-25.log (:72-76).

RUN KIND: offline

## Conditions

CONSOLE LEVEL: 4
(not applicable - no simulation)

PRE-ORDER GATE: --pre-order-gate nav-area
(not applicable - no order)

DurationScale: 1.0

ONE VARIABLE: the Desert Succulent Shrub biome's asset. In a copy of the vendor TerrainConfiguration folder,
osgEarthCatalogs\biome.definitions.CA-fveg.xml line 541 inside <biome id="DSS"> reads
`<asset name="HoneyMesquiteShortSpring" />` instead of `<asset name="YuccaPalm" />` (the asset name DSW uses at
:508; vendor file sha256 39e0c41b...bc14, copy b2b68820...45e9; every other file of the 541 in the copy is
byte-identical by sha256).

How the variable is delivered without writing under C:\MAK (tools/navdata/make_tree_control.py):
- The .earth pulls its includes by RELATIVE path ({% include x %} resolved against the including file, xi:include
  href="./...", urls "../../ModelData", "../../TerrainData/Terrain/..."). Instead of absolute includes, a mirror
  C:\C2SIM\vrf-nav\shadow reproduces SharedData\19\latest: TerrainData\TerrainConfiguration is a real COPY; every
  other entry (ModelData, images, TerrainData\Terrain, ...) is a directory junction to the vendor folder.
- Terrain copy tools\navdata\out\MAK Earth (online) + MojaveAO20_trees5x5.mtf = the vendor .mtf with ONE line changed:
  the .earth <myFilename> now names C:/C2SIM/vrf-nav/shadow/TerrainData/TerrainConfiguration/MAK Earth (online).earth
  (sha256 30f557b5...ecbd); the vendor .surfChar.map beside it (as in N5). No terrain-specific land-cover map (the global
  one applies, as on 2026-09-25).
- FEASIBILITY is read from the run's own first log lines (no separate launch is possible before the machine is
  quiet): "Loading features from C:\C2SIM\vrf-nav\shadow\...\MAK Earth (online).earth" and the same feature-layer list
  as gen-AO20-2026-09-25.log. If the .earth does not resolve, the run is a STOP and the alternative variable (Biomes
  vrfsim:enabled="false", which tests "trees at all") is reported, not run.

The box. Sectors (11..15, 34..38) of the MojaveAO20 grid, centred on (13,36) = cells x -112..-58, y 142..196 in the
AO20 frame (offset from the 2026-09-26 .navRuntimeConfig, verified to 0.02 m), corners inset 1 m so the generator's
43 m alignment keeps 55 x 55 cells; tile-count 5 x 5 keeps 11 cells per sector, i.e. cell size 43 and sector size
473 m identical to AO20. Corners (WGS84, h = 700 m), config
C:\C2SIM\vrf-nav\work\cfg\NavArea-ground-platform MojaveAO20_trees5x5.navGenConfig (578 B, sha256 d317db2e...4dfd):
    nw 34.684501598 / -116.752656040   ECEF -2363683.813924 -4688921.941482 3609539.256606
    ne 34.684509985 / -116.726872358   ECEF -2361573.274040 -4689984.675469 3609540.021732
    sw 34.663203093 / -116.752642615   ECEF -2364287.847765 -4690122.918409 3607595.915156
    se 34.663211473 / -116.726865521   ECEF -2362177.307114 -4691185.653102 3607596.679873
Round trip through osm_sector_map.SectorFrame: the four corners land in AO20 sectors (11,38) / (15,38) / (11,34) /
(15,34) at 0.02 m from the intended cell edges. New sector (a,b) is compared with AO20 sector (11+a, 34+b).

What the box holds on 2026-09-25 (MAK_CASE_sector_table_AO20_2026-09-26.csv): 25 sectors; 24 contain class 64 - 11
are 64-only ((12,38) (13,38) (13,37) (13,36) (11,35) (13,35) (14,35) (11,34) (12,34) (13,34) (14,34)), 13 are
60 + 64; one is clean, (15,38) (60 only, ratio 1.00, 57 kB). Of the 24
class-64 sectors, 21 were below 0.5 (the three that were not: (14,38) 0.63, (15,37) 0.77, (12,36) 0.70). 64-only
NavData 410-541 kB.

Command (cwd C:\MAK\vrforces5.2d\bin64; PATH prefix bin64;vrlink5.10\bin64;makRti5.0.1\bin; MAK_VRFDIR, MAK_VRLDIR;
licence from the User scope):
    vrfNavGenerator.exe --terrain "<repo>\tools\navdata\out\MAK Earth (online) + MojaveAO20_trees5x5.mtf"
      --config "C:\Users\PAULOB~1\Temp\nav\cfg\NavArea-ground-platform MojaveAO20_trees5x5.navGenConfig"
      --outputPath "C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveAO20_trees5x5"
      --verbose --navDataDir "C:\C2SIM\vrf-nav\navData\MAK Earth (online)" --userDataDir "C:\C2SIM\vrf-nav\userdata"
      --logFileName "C:\Users\PAULOB~1\Temp\nav\log\gen-AO20-trees5x5-2026-09-26.log"
    stdout/stderr -> C:\C2SIM\vrf-nav\work\log\gen-AO20-trees5x5-2026-09-26.{stdout,stderr}.txt; exit code captured in
    the SAME shell call that waits for it.
Started only after no vrfSim* / vrfGui* / VrfC2SimApp* / WatchVrf* process exists (lane R holds a live run; one polling
script, 60 s sleep inside its loop, up to 3 h; if it never clears, no run and a report).

Known differences besides the variable (registered so the result cannot drop them): the terrain/.earth PATHS (content
byte-identical except the variable); the box size (5 x 5 instead of 40 x 40 - its own grid, aligned to AO20's to about
a cell [A]); --userDataDir. A possible side effect: the Biomes layer is cached (caching="true"); if its cache key
follows the layer configuration, new tiles may be written under C:\MAK\vrforces5.2d\appData\cache (the tile cache the
owner keeps; --appDataDir stays unpassed). It is measured, not prevented.

## Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | the run completes (25 "Sector (" rows, "Generation time:", exit code 0), the log loads the SHADOW .earth with the same feature-layer list as 2026-09-25, the .navRuntimeConfig "NavArea-ground-platform MojaveAO20_trees5x5.navRuntimeConfig" is written into --navDataDir | HIGH | any limb |
| P0b | no "Sim model config: YuccaPalm not found." line (no class 62 in or near the box; DSS no longer names YuccaPalm) | MEDIUM | the line appears |
| P0c | alignment: per-sector input triangle counts within 2 % of the AO20 sector they are compared with | MEDIUM | > 2 % in more than 3 sectors |
| P1 | every class-64 sector in the box (24) reaches ratio >= 0.9 | HIGH | any class-64 sector below 0.9 |
| P2 | per-sector distinct-tag counts UNCHANGED against the AO20 sector (a tree is still present, so the tree term of the laneQ3 tag rule is kept) | HIGH | any sector's count changes |
| P3 | NavData size in the 11 64-only sectors falls toward the non-fragmented dryground level: median between 100 and 250 kB (was 410-541) | MEDIUM | median outside 100-250 kB |

FALSIFIER: more than 2 of the 24 class-64 sectors stay below 0.5 -> H1 (YuccaPalm's broken sim entry as the carving
input) fails and H1a (trees in general, by density / trunk) rises. Tag counts changing -> the edit did not take or
reached beyond DSS. If the YuccaPalm line still appears AND nothing changes, the edit did not take (include not read,
or a stale Biomes cache) - that is a STOP on the instrument, not a result about trees.

Interpretation fixed in advance: P1 and P2 HOLD -> the mesh is carved by the YuccaPalm placement specifically (its
sim entry has trunk -inf), not by tree presence or the soil tag. P1 holds and P2 misses -> the carving and the tag
both follow the asset, so the tag-rule reading changes. P1 misses (falsifier) -> not the broken asset.

## Result (written after the harvest, never from a live read)

RUN: waited out lane R's live run (u3\n6_wait_quiet.ps1: busy from 12:28Z, two clear polls at 13:14 / 13:15Z); started
2026-09-26 13:15:34Z, finished 13:16:40Z (66 s wall; the generator's own "Generation time: 29.3659"). Stdout ends in the
normal shutdown sequence; stderr empty. EXIT CODE NOT CAPTURED although the prereg promised it: the Start-Process
object returned an empty ExitCode after WaitForExit - an instrument failure of mine, not a measurement. Log
C:\C2SIM\vrf-nav\work\log\gen-AO20-trees5x5-2026-09-26.log (90,016 B, sha256 07e166fe...6386); gate output beside it
(nav_gate-AO20-trees5x5-2026-09-26.txt / .json).

Feasibility (no STOP): the log reads "Loading features from c:\C2SIM\vrf-nav\shadow\TerrainData\TerrainConfiguration\MAK
Earth (online).earth"; its feature-layer and elevation-layer lines are identical to gen-AO20-2026-09-25.log; tag
volumes MAK_WATERWAY 0 / MAK_VEGETATION 0 / MAK_ROAD 15 (box-sized). The edited biome definitions were used: the run
created a NEW Biomes cache folder, appData\cache\vrfsim\Biomes-9f1dab2ed573d922 (13 files, 32,289 B - the only writes
under C:\MAK; the 2026-09-25 folder is Biomes-255fe9580a63069a), and printed no "Sim model config" line.

The grid. The generator did NOT keep the 55 x 55 cells: it widened the box to 57 x 57 (extent -1204..1204 m, rows
"Sector (i,j)" with cells -28..28, 11-cell sectors plus a 13-cell last row and column) and centred it on its own
offset. Mapped into the AO20 frame the new grid sits exactly half a cell (21.5 m) west and south of AO20's: new
sector (a,b) covers AO20 sector (11+a, 34+b) shifted by 21.5 m in x and y (about 91 % shared ground; the last row
and column are 13 cells wide).

| # | Verdict | Measured |
|---|---|---|
| P0 HIGH | HIT on every observed limb; the exit-code limb UNVERIFIED | 25 sectors, "Generation time: 29.3659", the shadow .earth loaded with the same layer list, "NavArea-ground-platform MojaveAO20_trees5x5.navRuntimeConfig" written (extent +/-1204, offset -2362930.560903 -4690053.796939 3608567.968444) |
| P0b MED | HIT | 0 "Sim model config: YuccaPalm not found." lines (2026-09-25: 5) |
| P0c MED | MISS | per-sector input triangles differ from the AO20 sector by 2.8-13.6 % (38.5 % in the 13 x 13-cell corner sector) - the half-cell offset above |
| P1 HIGH | HIT | every one of the 24 class-64 sectors reads ratio 1.000 (AO20: 21 of them below 0.5, down to 0.019 at (13,36)); the clean sector 1.000 as before. nav_gate: 25 measured, 0 below 0.9, GATE PASS |
| P2 HIGH | MISS | the histogram is identical (1: 1, 2: 11, 3: 13, as the same 25 AO20 sectors) but 2 sectors changed: new (2,2) <- (13,36) 2 -> 3 and new (1,1) <- (12,35) 3 -> 2 |
| P3 MED | MISS (fell further than predicted) | 64-only NavData median 97.6 kB, range 90.5-107.9 (AO20 410-541; the band was 100-250) |

The P2 miss, tested rather than explained away: the laneQ3 tag rule (distinct soils + 1 if a tree-bearing class is
present), evaluated on the NEW sector boxes with the 2026-09-26 CA-FVEG tiles, predicts 23 of 25 exactly - and the two
it misses are exactly these two ((2,2): only class 64 inside, predicted 2, actual 3; (1,1): class 64 plus a 6-sample
sliver of 60, predicted 3, actual 2). So the half-cell offset does not account for them. Unexplained. The rule's error
rate over the 1,600 AO20 sectors is 22 of 1,600 (laneQ3), so 2 of 25 is higher than that but on 25 sectors; the
prereg reading "reached beyond DSS" is neither shown nor excluded.

What this measures. With Desert Succulent Shrub's single asset changed from YuccaPalm to HoneyMesquiteShortSpring and
nothing else in the terrain changed, every class-64 sector of this box has a fully connected abstract graph and about
a fifth of its former NavData, while the distinct-tag count is unchanged in 23 of 25 sectors.

What it does NOT yet exclude - the strongest competing reading: the box SIZE. This is a 2.45 km box; the AO20 numbers
came from the 20 km area. The unedited 5 x 5 control (the same config on the vendor terrain, about one minute) was not
run - one generation was authorised. Until it is, "small boxes do not fragment" and "YuccaPalm fragments" are not
separated. The prereg listed the box size as a known difference; it is the next measurement.

Design implication, stated separately: if the unedited 5 x 5 control fragments, the lever is the one asset line in
the DSS biome (delivered without writing under C:\MAK through the shadow terrain), and a full AO20 regeneration on the
shadow terrain is the candidate for a passing area - a new prereg, not enacted here. Nothing was registered: a 5 x 5
box is a test, not an area.

Side effects: 13 new files (32 KB) in the vendor tile cache under C:\MAK\vrforces5.2d\appData\cache (pre-registered as
possible); 25 ClientInput intermediates (158,307,124 B) in C:\C2SIM\vrf-nav\userdata\NavDataDebug.

---

## PREREG N7 - the NULL arm (written and committed BEFORE the null run)

Why: the edited run above cannot be read without the same box on the UNEDITED terrain. The first prereg listed the
box size as a known difference but registered no null; this block adds it. Same brief lineage (N7), same citations
as above.

ONE VARIABLE (null vs the edited run): the .earth. The null terrain is
tools\navdata\out\MAK Earth (online) + MojaveAO20_null5x5.mtf, a BYTE COPY of the vendor MAK Earth (online).mtf
(sha256 d445e109...9a23), i.e. the _trees5x5 copy with its one changed line restored - it names the vendor
$(SHARED_DATA_DIR)/.../MAK Earth (online).earth. Chosen over the vendor .mtf itself so the terrain file PATH is of
the same kind (a copy under tools\navdata\out) in both arms; the .surfChar.map beside it is the same byte copy
(253c042e...bce0). Config: NavArea-ground-platform MojaveAO20_null5x5.navGenConfig, a byte copy of the _trees5x5
config (sha256 d317db2e...4dfd), renamed only so the .navRuntimeConfig gets the null area's name. Same generator,
cwd, environment, --userDataDir C:\C2SIM\vrf-nav\userdata, pre-created --navDataDir, no --appDataDir; output area
"NavArea-ground-platform MojaveAO20_null5x5"; log gen-AO20-null5x5-2026-09-26.log. Exit code captured through the
process handle (-PassThru, handle held, WaitForExit, ExitCode). Started only with no vrfSim* / vrfGui* /
VrfC2SimApp* / WatchVrf* process.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| N0 | the run completes with exit code 0; the generator widens to the same 57 x 57-cell box (extent +/-1204) with the same offset as the edited run, so the two arms share every sector box exactly | HIGH | exit != 0, or a different extent/offset |
| N1 | the fragmentation reappears: >= 18 of the 24 class-64 sectors below 0.5 (the full-area value on 2026-09-25 was 21 of 24) | HIGH | fewer than 18 |
| N2 | "Sim model config: YuccaPalm not found." lines are present | HIGH | none |
| N3 | the 64-only NavData median is back in the 400-550 kB range | MEDIUM | outside it |
| N4 | the two P2 anomalies are checked in the null: if (13,36) reads 3 and (12,35) reads 2 in the null too, they are a box/offset artefact, not the tree swap | RECORDED | - |

FALSIFIER: FEWER THAN 10 class-64 sectors below 0.5 in the null -> box size (or the half-cell offset) alone removes
the fragmentation, and the tree swap is NOT shown to be the lever. Between 10 and 17: a partial; the swap's effect is
then measured as the difference between the arms, sector for sector, and said so.

## RESULT N7 (written after the null run)

RUN: 2026-09-26 13:22:33Z -> 13:23:11Z (37 s wall; the generator's own "Generation time: 25.1174"), EXIT CODE 0
(captured through the held process handle). No vrfSim / vrfGui / VrfC2SimApp / WatchVrf / test-suite process at the
start. Log C:\C2SIM\vrf-nav\work\log\gen-AO20-null5x5-2026-09-26.log (90,201 B, sha256 6c7198c6...84f0); gate
output beside it (nav_gate-AO20-null5x5-2026-09-26.txt / .json). The log loads the VENDOR .earth
(c:\MAK\SharedData\19\latest\...\MAK Earth (online).earth). Nothing new under C:\MAK (bin64, userData, SharedData,
appData: 0 files newer than the start - the Biomes cache for the vendor configuration already existed).

| # | Verdict | Measured |
|---|---|---|
| N0 HIGH | HIT | exit 0; extent (-1204,-1204)..(1204,1204); offset -2362930.560903 -4690053.796939 3608567.968444, identical to the edited run; the 25 "Sector (i,j): xMin .. yMax" rows identical line for line - the two arms share every sector box exactly |
| N1 HIGH | HIT | 21 of the 24 class-64 sectors below 0.5 (the same count as the full area on 2026-09-25); 24 of 25 sectors below 0.9, min 0.020 at (2,2); the clean sector 1.000 |
| N2 HIGH | HIT | 4 "Sim model config: YuccaPalm not found." lines (log :73-76) |
| N3 MED | HIT | 64-only NavData median 472.2 kB |
| N4 | RECORDED: the P2 anomalies are a box/offset artefact | the null ALSO reads (13,36) -> 3 tags and (12,35) -> 2 tags. Null vs edited: tag counts identical in 25 of 25 sectors |

FALSIFIER not met (21, not fewer than 10). THE TREE SWAP IS THE LEVER ON THIS BOX: same sector boxes, same terrain
except one asset line - null 21 of 24 class-64 sectors below 0.5, edited 0 of 24; null 64-only NavData median 472 kB,
edited 97.6 kB; tag counts identical 25 of 25.

Re-scoring the edited run's P2 against the correct comparator: the P2 prediction ("tag counts UNCHANGED") was
registered against the full-area AO20 sectors; against the null arm - the like-for-like comparator this block adds -
the tag counts are unchanged in 25 of 25. The original P2 MISS stands as registered; its cause is the half-cell
offset, which the null reproduces.

NEW measurement, not predicted: the input triangle count changes with the asset. Null vs edited, the per-sector
"Sector ... has N triangles." differs in 24 of 25 sectors - every class-64 sector has 3.5-11 % MORE input
triangles with YuccaPalm than with the mesquite (10-11 % in the 64-only sectors, less where Desert Scrub shares the
sector) (e.g. (2,2) 113,619 vs 102,637; (0,4) 131,221 vs 120,335) - and is
identical only in the tree-free sector (4,4) (137,087 both). So the procedural trees enter the generator as input
GEOMETRY. This corrects laneQ3's "not InputTriangles" (a cross-sector correlation, r = -0.11), which this
intervention supersedes for this box.

What this measures. On this 2.45 km box, replacing Desert Succulent Shrub's single asset YuccaPalm by
HoneyMesquiteShortSpring - and nothing else - removes the abstract-graph fragmentation from every class-64 sector
(21 -> 0 below 0.5), cuts their NavData to about a fifth and their input triangles by up to a tenth, and leaves the
distinct-tag counts unchanged.

Design implication, stated separately: the lever for a passing MojaveAO20 is the DSS asset line, delivered without
writing under C:\MAK through the shadow terrain (tools/navdata/make_tree_control.py). A full AO20 regeneration on the
shadow terrain, gated by nav_gate.py, is the next candidate - a new prereg, not enacted here. The side effect is a
MEASUREMENT consequence for the simulation too: class-64 ground would carry mesquite instead of Joshua trees
wherever that terrain copy is used.

Still open: why YuccaPalm's geometry shatters the graph (its sim entry is -inf; the generator logs "not found";
what geometry it then uses is not documented - MAK case Q6); the 2026-09-07 area's clean state (laneQ3: the tree
term was absent then).
