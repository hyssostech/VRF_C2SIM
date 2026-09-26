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

(to be written after the run)
