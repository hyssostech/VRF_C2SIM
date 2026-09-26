# FINDING: what the Iron Storm corridor's sub-0.9 sectors have in common (2026-09-26)

Tier: HEAVY (a cause claim is tested). Lane U3/I2, session 5fc25950. Offline: no generation, no sim, nothing written
under C:\MAK. Object: IRONSTORM-CENTRE generation 1 (2026-09-20, raster precision 0.2), console log
`...\a7f6a276-...\scratchpad\ironstorm\cuta\gen_nav_console.txt` (3,850,237 B). Owner decision 2026-09-26: no Iron Storm
run until every route (corridor) sector passes 0.9, so this finding is the input to the corridor fix.
Scratch artefacts (not in the repo): `...\5fc25950-...\scratchpad\u3\laneI2\` (corridor.py, relief.py, the JSON tables).

## 1. Instrument (all [V])

- `nav_gate.py` could not read this log: it prints only name-form sector rows
  ("Sector ground-platform_14_13_iiSc has N triangles."), and the gate keyed on "Sector (i,j):" rows, so it exited 2
  ("sectors total 0"). It now keys on both forms. On the two real 5.2d AO20 logs that carry both, the name's i_j equals
  the "Sector (i,j)" row for 1,600 of 1,600 sectors and the gate's JSON is byte-identical before and after. New dirty
  control "dirtyname" + test F (tests\NavGate.Tests.ps1, 20/20).
- Gen-1 result: 1,600 sectors, 1,561 measured, 6 without a report, 33 with 0 nodes (lakes), 280 below 0.9, none in
  0 < ratio < 0.5, GATE FAIL. The 7 corridor sectors reproduce exactly: T02 (14,13) 0.8947, (16,14) 0.8868,
  (17,15) 0.8800; T10 (27,22) 0.8475, (27,23) 0.8868, (28,21) 0.8182, (28,22) 0.8571. The 09-21 record's "313 below
  0.9, 33 below 0.5" counted the 0-node sectors as ratio 0 (280 + 33 = 313).
- `osm_sector_map.py`: a name-only log now gets its sector cells from the extent line (11 cells per sector, the last
  keeps the remainder). Checked against both AO20 logs: 1,600 of 1,600 equal the logged rows. Tile fetch switched to
  curl (the server now answers Python's user agent with 403: 675 of 675 tiles).
- `landcover_sector_map.py`: adds CLCplus 10 m (TMS 59, L14) and OSM inland water as the TOP layer, per the vendor
  composite `osgEarthCatalogs\biomes.landcover.coverage.online.xml:32` ("lowest to highest"), `:50` (CLCplus),
  `:55` (OSM roads), `:58` (OSM water); water values per `coverage\layer.OSM.water.LOD14.online.xml:14-45`, filter per
  `osm.features.water.xml:15-19`. Same order as `tools\preflight\leg_check.py:118-126`. `--selftest` (10 checks; the
  layer-order check fails when CLCplus is put above water). Maxar (TMS 199) answered 404 on the one tile probed over the AO (54.03 N, 23.26 E).
- Corridor = the 29 sectors the three cut-A legs cross (T02 16, T10 8, T14 8; straight legs of SCR paired.py, 2 m
  stride) - the same 29 as the 09-21 record. Sector frame fitted to the log's extent (+21.45 m east shift, corner
  residuals <= 0.09 m); the offset is assumed, as for AO20 (where it was later verified to 0.02 m).
- Coverage: 522 CLCplus tiles, 6 Copernicus, 128 OSM-water tiles with features (368 polygons, 0 lines: the
  osm-water tileset carries no stream lines, so no stream is carved by this path), 225 osm + 224 osm-highways tiles.

## 2. Prediction (registered before the table was built; laneI2_report.md)

DRY = no sampled point in OSM water or CLCplus 100. "Explains the corridor" = (a) >= 6 of the 7 failing corridor
sectors carry the feature AND (b) among DRY sectors area-wide the feature group fails at >= 2x the rate of the rest
(or the stated median margin).

| H | prediction | confidence | result |
|---|---|---|---|
| H1 tag count | dry-fail median tags >= pass + 1 | MEDIUM-HIGH | **MISS, reversed**: dry-fail median 2, dry-pass 3; tags >= 4 fail 9.1 %, <= 3 fail 19.2 % |
| H2 CLCplus forest 21/22/31/33 | dry-fail median forest >= pass + 0.10 | MEDIUM | **HIT**: 1.00 vs 0.84; forest >= 0.10 fails 19.5 % (191/977) vs 3.4 % (9/261), 5.7x; 7 of 7 corridor fails >= 0.10 |
| H3 OSM roads / MAK_ROAD | < 10 points difference in road-present share | MEDIUM | **MISS (12 points) in the OPPOSITE direction**: roads are more common in PASSING sectors (84 % vs 73 %); road-free dry sectors fail 25.3 %, road sectors 14.2 %. Not a cause |
| H4 small water edges | small-wet fails >= 1.5x dry | LOW-MEDIUM | **MISS**: water area < 0.10 fails 15.9 % vs dry 16.2 %. Larger water shares do rise: 0.10-0.30 31 %, 0.30-0.60 49 % |

## 3. Result

The per-sector table for the 7 (ratio, CLCplus forest share, water share, roads, relief):

| sector | leg | ratio | tags | forest | water (OSM) | paved/dirt road hits | relief m |
|---|---|---|---|---|---|---|---|
| (28,21) | T10 | 0.8182 | 5 | 0.13 | 0.340 lake | 0 / 86 | 5.5 |
| (27,22) | T10 | 0.8475 | 4 | 0.70 | 0.058 | 890 / 1193 | 20.5 |
| (28,22) | T10 | 0.8571 | 5 | 0.10 | 0.225 | 0 / 380 | 18.3 |
| (17,15) | T02 | 0.8800 | 6 | 0.29 | 0.020 river | 641 / 1410 | 5.9 |
| (27,23) | T10 | 0.8868 | 5 | 0.85 | 0.057 | 1038 / 459 | 16.8 |
| (16,14) | T02 | 0.8868 | 2 | 1.00 | 0 | 0 / 565 | 23.0 |
| (14,13) | T02 | 0.8947 | 3 | 0.96 | 0 | 0 / 2198 | 21.3 |

Passing corridor controls: 22 sectors (16 dry, 6 wet), mean forest 0.63 - as forested as the failing 7 (0.58).

- Two additive risk factors, each with a dose-response, area-wide:
  - forest share (dry sectors): 0-0.1 fails 3.4 %, 0.1-0.3 7.9 %, 0.3-0.6 11.2 %, 0.6-0.9 19.1 %, 0.9-1.0 21.9-23.7 %;
    Spearman(ratio, forest) = -0.48.
  - water share: see H4.
  - 2 x 2: forest >= 0.3 AND wet 31.0 % (39/126); forest only 21.3 %; water only 20.8 %; neither 4.9 % (19/387).
- The failing 7 = 5 forest+water sectors and 2 near-pure dry forest sectors. Every one carries at least one factor;
  the 22 passing corridor sectors carry them just as often. The factors raise the failure RATE; they do not say which
  sector of a forest fails (dry pure-forest sectors that fail and that pass have the same median NavData size, 182 vs
  179 kB, and node count, 57 vs 55).
- Mechanism evidence [V correlation, A mechanism]: median NavData size grows 2.5x with forest share (dry: 71.6 kB at
  < 0.1 to 179.7 kB at >= 0.9) while the input triangle count grows only 6 % - the extra complexity is added during
  meshing, consistent with tree trunks carved as obstacles (the Mojave record's mechanism: PREREG_NAVTREES_YUCCA and
  PREREG_NAVAO20_MESQUITE, where swapping one tree asset cleared 189 fragmented sectors).
- The 09-21 note "5 of 9 corridor sectors 0.00 wet" was a CLCplus-only measure. With OSM water on top (the vendor's
  order) 5 of the 7 failing sectors hold water (2-34 %); CLCplus maps lakes under 50 % water as land.
- The 09-21 note "tag richness correlates" is a WATER confound: water adds soils (deeplake / shallowlake) and tags;
  among dry sectors the correlation reverses (pure forest = 2 tags and fails most).

## 4. Falsification

- Competing hypothesis: forest is a proxy for RELIEF (forest sits on the moraine hills). Checked with the vendor DEM
  (TMS 149, leg_check.Tiles, 618,223 samples at 0.0003 deg): Spearman(forest, mean slope) = +0.45, so they are linked;
  but Spearman(ratio, slope) = -0.28 is weaker than forest's -0.48, and WITHIN each slope tercile forest still orders
  the failure rate (5 / 13 / 26 %, 4 / 16 / 20 %, 5 / 18 / 25 %) while within pure forest slope adds nothing (26 % vs
  25 %). Relief as the main driver: FALSIFIED on this data.
- Would falsify H2 as causal: a paired generation where the forest's trees are removed or shrunk and the forest
  sectors' ratios do NOT rise (section 5, L1a). Not run (no generation in this lane).
- Unexplained: why a given forest sector fails and its neighbour of equal forest share and size does not. The rate
  model leaves 76 % of pure-forest sectors passing.

## 5. Candidate levers (none applied; each needs its own prereg and a generation)

| # | lever | vendor citation | cheap test | cost / side effect |
|---|---|---|---|---|
| L1a | CAUSE TEST: biome 04 with NO tree assets on a shadow terrain | `osgEarthCatalogs\biome.definitions.bioregions.xml:143-149` (biome 04 "Temperate Broadleaf and Mixed Forests": RedMapleSpring, AmericanSycamoreFullSpring, WhiteOakSpring); `:524-526`, `:2330-2332`, `:2365-2367`, `:2490-2492` (04-PA and the Baltic / Central European / Sarmatic ecoregions carry no assets, so they inherit biome 04); RN VRF-8788 (p69), VRF-9016 (p72) | 5 x 5-sector box pair (unedited vs edited, same config) over T02's (14..18, 12..16) and T10's (26..30, 20..24); predict the forest sectors rise >= 0.9 on the edit and stay put on the control | diagnostic only (no trees in forests) |
| L1b | PRODUCT lever: swap the wide-trunk trees for the narrow one | `osgEarthCatalogs\simVegetation.xml:74` RedMapleSpring trunk 0.15 m, `:99` WhiteOakSpring 0.45 m, `:103` AmericanSycamoreFullSpring 0.74 m | same box pair, edit = `--swap 04:AmericanSycamoreFullSpring=RedMapleSpring --swap 04:WhiteOakSpring=RedMapleSpring` | forests render and simulate as red maples; the Mojave analogue passed the gate (README, N10) |
| L2 | raster precision 0.4 | UG52 66.3.1 p1282 (generation parameters); own record SCR part3b.md | already measured (gen 2): corridor 7 -> 2 fails (T02 (14,13), (16,14) at 0.8837) | FIDELITY FAIL: lake sectors meshed over (all 39 rose above 0.9). Rejected |
| L3 | route around the water-heavy T10 sectors | tools\preflight route shift (Vrf:PreflightRouteShift) | leg_check / corridor.py on a shifted leg | changes the drive, not the mesh; does not help T02's dry forest sectors |

Tool work before L1a/L1b: `make_tree_control.py` hard-codes `biome.definitions.CA-fveg.xml` and the AO20 grid origin
(cells -233 / -232) and needs a `.navRuntimeConfig` (gen 1's is lost; the regeneration running now will write one).
It needs a `--defs` file option and a grid taken from the log (osm_sector_map.synth_cells). [A] that the Suwalki AO
falls in a biome-04 ecoregion (the plan's reading; the three candidate ecoregions above are all 04 and all inherit).

## 6. Verified / assumed

VERIFIED: the gate numbers and the 7 sectors; the frame synthesis on AO20; the layer order and line citations above;
the tables in sections 2-3 and 4 (scratch corridor_out.txt, relief_out.txt). ASSUMED: the sector frame offset
(fitted, half a cell); that a pixel's effective class is the top mapped layer (osgEarth compositing); that the OSM
roads layer is not in the effective class (roads are counted separately); the trunk-carving mechanism; the biome-04
reading. The fresh 2026-09-26 regeneration was 385 of 1,600 sectors in when this was written and is not used.
