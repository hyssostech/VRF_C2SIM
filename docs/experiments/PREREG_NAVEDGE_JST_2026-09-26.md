# PREREG NAVEDGE JST - is the Joshua Tree biome's YuccaPalm what holds the four east-edge sectors below 0.9? (edge control pair)

Lane N9, session 5fc25950. Brief N9: "Small control pair on the edge first, then full run". Part 1 = two small offline
generations (NULL, EDITED) on one config; Part 2 (a full AO20 run) only if Part 1 passes, with its own prereg block
appended below before it runs.

## Registration

PREREG ID: navedge-jst-2026-09-26-1
DATE (UTC): 2026-09-26, written and committed BEFORE either arm was started
BINARY / COMMIT: C:\MAK\vrforces5.2d\bin64\vrfNavGenerator.exe (420,352 B); branch feat/nav-gate-and-ao20 at origin/main 4823141
TIER AND GATE: HEAVY / PREREG

VENDOR CITATION: UG52 ch. 66 (pp1272-1289) is silent on procedural trees / biomes as navigation inputs (it documents
Prune No-Go Table 54 p1279 and 66.3.1 p1282 only); Release Notes VRF-8788 p69 / VRF-9016 p72 (simTreesTool). The
behaviour for an asset with a missing / -inf sim entry (simVegetation.xml:156 YuccaPalm) is MAK case question Q6,
unanswered. Local files read: osgEarthCatalogs\biome.definitions.CA-fveg.xml:517-525 (JST "Joshua Tree": YuccaPalm
:521, YuccaPalmSeedling fill 0.25 :523) and :537-544 (DSS).

OWN-RECORD CITATION: PREREG_NAVAO20_MESQUITE_2026-09-26.md (full AO20 on the DSS-only mesquite shadow: 0 of 1,600
below 0.5, gate FAIL on (39,34) 0.859, (39,35) 0.806, (39,36) 0.836, (39,38) 0.895 - all holding class 62 Joshua Tree,
all byte-identical to 2026-09-25); PREREG_NAVTREES_YUCCA_2026-09-26.md (the N6/N7 5 x 5 control pair: the DSS swap
is the lever; trees enter as input geometry).

RUN KIND: offline

## Conditions

CONSOLE LEVEL: 4
(not applicable - no simulation)

PRE-ORDER GATE: --pre-order-gate nav-area
(not applicable - no order)

DurationScale: 1.0

DEVIATION FROM RECORD: the brief asks for "a 5x5 box that covers the four failing sectors"; a 5-wide box cannot keep
column 39's geometry: the generator sizes sectors as floor(cells / tile-count) (AO20: floor(467/40) = 11, remainder
column 38 cells), so 4 x 11 + 38 = 82 cells over 5 tiles would give 16-cell sectors. The box is therefore ONE sector
column wide: AO20 column 39 (cells 196..233, 38 cells = 1,634 m, its triangle load intact) by six sector rows, 33..38
(cells 131..196, 66 cells), tile-count 1 x 6 -> sectors of 38 x 11 cells, exactly AO20's (39,33)..(39,38).

How the half-cell drift of N6 is avoided (and how it is checked). Measured on the two runs on record: the generator
takes the box's corners, builds a frame about their centroid, and extends each side to the next whole cell
(floor(min/43) .. ceil(max/43)) - N6's odd 55-cell request became 56 cells centred on the centroid, i.e. half a cell
off AO20's grid. Here both dimensions are EVEN in cells (38, 66) and the requested box is centred on an AO20 cell
EDGE (cell coordinates 215, 164), so a centroid-centred, whole-cell grid lands on AO20's edges. Corners are inset 3 m
so the meridian-convergence rotation between the frames (about 0.057 deg; up to 1.4 m at the box corners) cannot
push a side over to an extra cell. PREDICTED grid: extent (-817, -1419)..(817, 1419); rows "Sector (0,j): xMin: -19
yMin: -33+11j xMax: 19 yMax: -23+11j" (j = 5: yMax 33). This is checked on the NULL log BEFORE the EDITED arm is
started; any other extent or offset is reported and the arms are compared as they fall. [A] residual: the 0.057 deg
rotation means cell edges agree with AO20's to about 1.4 m at the corners, not exactly. A known oddity not covered by
this rule: the AO20 offset itself sits 10.78 m west of its corners' centroid.
Config (both arms, byte-identical content; renamed per arm so each .navRuntimeConfig gets its area's name):
C:\C2SIM\vrf-nav\work\cfg\NavArea-ground-platform MojaveAO20_edge_{null,jst}.navGenConfig, 578 B, sha256
2a84fffa...ac8b, tile-count 1 x 6, cell 43, raster 0.2. Corners (h 700 m): nw 34.684460179 / -116.608123429, ne
34.684445477 / -116.590359689, sw 34.658934432 / -116.608151600, se 34.658919744 / -116.590393300.

The two arms (ONE variable: the JST biome's asset line 521).
- NULL: terrain tools\navdata\out\MAK Earth (online) + MojaveAO20_edge_null.mtf = byte copy of the _meso copy (sha256
  30f557b5...ecbd) -> C:\C2SIM\vrf-nav\shadow (DSS edited, JST not); area "NavArea-ground-platform MojaveAO20_edge_null".
- EDITED: terrain tools\navdata\out\MAK Earth (online) + MojaveAO20_edge_jst.mtf (sha256 78dbdcc5...0c9b) ->
  C:\C2SIM\vrf-nav\shadow_jst, a second mirror whose biome.definitions.CA-fveg.xml carries BOTH swaps: :541 DSS and :521
  JST, YuccaPalm -> HoneyMesquiteShortSpring (sha256 ae5ddb26...647b; all other 540 files sha256-identical to the vendor
  folder); area "NavArea-ground-platform MojaveAO20_edge_jst". YuccaPalmSeedling (:523, no simVegetation.xml entry at
  all) is NOT changed.
Both: same generator, cwd, environment, --userDataDir C:\C2SIM\vrf-nav\userdata, pre-created --navDataDir, no
--appDataDir, stdout to a file, exit code through the held process handle; started only with no vrfSim* / vrfGui* /
VrfC2SimApp* / WatchVrf* / pwsh test suite running. Each arm's NavDataDebug intermediates are deleted after it.

The six sectors on 2026-09-25 / the mesquite full run (identical): (39,33) 0.952, (39,34) 0.859, (39,35) 0.806,
(39,36) 0.836, (39,37) 0.917, (39,38) 0.895; all hold class 62; input triangles 350k-369k each.

## Predictions (written BEFORE either arm; a missed HIGH prediction is a stop, not an adjustment)

| # | Prediction | Confidence | What counts as a MISS |
|---|---|---|---|
| E0 | both arms exit 0; the NULL log shows the predicted grid (extent +/-817 x +/-1419, rows as above) | HIGH (exit) / MEDIUM (grid) | exit != 0; any other grid (reported, then read as it falls) |
| E1 | NULL reproduces the four failing sectors at their full-area ratios +/- 0.03 (0.859, 0.806, 0.836, 0.895) and prints "Sim model config: YuccaPalm not found." | HIGH | any of the four outside +/- 0.03, or no such line |
| E2 | EDITED lifts all four to >= 0.9 and prints no "YuccaPalm not found" line | HIGH | any of the four below 0.9, or the line present |
| E3 | NULL input triangles within 1 % of the full-area values for the same six sectors (same geometry) | MEDIUM | more than 1 % off in any |
| E4 | EDITED has fewer input triangles than NULL in the class-62 sectors (the N7 signature) | MEDIUM | not fewer |

FALSIFIERS: NULL does NOT reproduce (E1 misses) -> a box artefact; STOP and report, no reading about trees. EDITED
leaves any of the four below 0.9 while NULL reproduced -> the edge column / triangle load is the cause, NOT the tree;
STOP, report, no full run. Only NULL reproduces AND EDITED passes all four -> Part 2.

## Result Part 1 (written after both arms)

NULL arm: 2026-09-26 14:18:24Z -> 14:19:19Z (54.8 s wall; "Generation time: 24.924"), EXIT CODE 0 (held handle,
u3\n9_arm.ps1). Started after the seat's suite run cleared (u3\n9_wait_suite.ps1, quiet 14:15:31Z) and origin/main
4823141 was merged. Log C:\C2SIM\vrf-nav\work\log\gen-AO20-edge_null-2026-09-26.log (26,476 B, 882 lines, sha256
7f2a37ae...6ecf); gate output beside it. The log loads the DSS-only shadow .earth; 5 "Sim model config: YuccaPalm not
found." lines. Nothing new under C:\MAK. The arm's NavDataDebug intermediates (6 files, 124,719,888 B) were deleted
after the run.

The grid came out EXACTLY as predicted: extent (-817,-1419)..(817,1419); rows (0,0) -19/-33..19/-23 ... (0,5)
-19/22..19/33; and the area offset maps into the AO20 frame at cell coordinates (215.000, 164.000) - on an AO20 cell
edge, so N6's half-cell drift is gone.

| AO20 sector | full area (mesquite run) ratio / input triangles | NULL ratio / input triangles |
|---|---|---|
| (39,33) | 0.9516 / 350,410 | 0.9388 / 349,958 (-0.13 %) |
| (39,34) | 0.8594 / 356,793 | 0.8571 / 356,952 (+0.04 %) |
| (39,35) | 0.8060 / 369,343 | 0.8462 / 371,169 (+0.49 %) |
| (39,36) | 0.8358 / 361,053 | 0.8438 / 360,650 (-0.11 %) |
| (39,37) | 0.9167 / 352,465 | 0.8966 / 351,997 (-0.13 %) |
| (39,38) | 0.8947 / 350,088 | 0.8723 / 381,334 (+8.93 %) |

| # | Verdict | Measured |
|---|---|---|
| E0 | HIT | exit 0; the predicted grid exactly |
| E1 HIGH | MISS | (39,34) -0.002, (39,36) +0.008, (39,38) -0.022 are inside +/- 0.03, but (39,35) reads 0.846 against 0.806, +0.040 - outside the registered tolerance. The YuccaPalm line is present. All four stay below 0.9, and a fifth sector, (39,37), falls to 0.897 (0.917 in the full area) |
| E3 MED | MISS | triangles within 0.5 % in five sectors; (39,38), the box's top row, +8.9 % |
| E2, E4 | NOT RUN | - |

STOP, as registered: E1 is the NULL-reproduces condition and it missed on one of the four sectors, so the falsifier
"NULL does not reproduce -> box artefact; stop, report" applies. The EDITED arm was NOT started, Part 2 was NOT
prepared, nothing was registered.

What this measures. The edge box reproduces AO20 column 39's geometry exactly (same cells, 0.5 % triangles in five
of six sectors) and reproduces the gate failure qualitatively - every one of the four failing sectors is below 0.9
(0.84-0.87), plus (39,37) - but not their exact ratios: sector ratios in this column move by up to 0.04 when the
column is generated in isolation, even with identical cell boxes. The top row's +8.9 % triangles suggests the box
edge changes what geometry a sector gathers (tiles or overlap beyond the box) [A].

Design implication, stated separately: the registered +/- 0.03 tolerance was tighter than this box can reproduce.
The seat can amend the criterion before the EDITED arm (e.g. "NULL: all four below 0.9 and the YuccaPalm line
present" - which this NULL meets) and run EDITED against this NULL, which is the like-for-like comparator anyway;
that is a new registration, not a re-reading of this one.

## REGISTRATION 2 (2026-09-26, after the NULL arm, BEFORE the EDITED arm)

Lane N10. The Part 1 result above stands as scored: E1 is a MISS and stays a MISS.

OWNER SELECTION (2026-09-26, AskUserQuestion, the seat's option label, quoted as selected): "Record the miss;
register and run the edited arm".

Reason, stated plainly: the seat's +/- 0.03 reproduction band was tighter than an isolated box can reproduce -
column-39 ratios moved by up to 0.04 when generated on their own, on identical cells. The NULL arm above is the
like-for-like comparator for the EDITED arm (same cells, same box, same config bytes), so the EDITED arm is scored
against THIS NULL, not against the full-area values. This is a new registration, not a re-reading of E1.

Conditions: unchanged from the registration above. EDITED config C:\C2SIM\vrf-nav\work\cfg\NavArea-ground-platform
MojaveAO20_edge_jst.navGenConfig, sha256 2a84fffa...ac8b (byte-identical to the NULL arm's); terrain
tools\navdata\out\MAK Earth (online) + MojaveAO20_edge_jst.mtf (sha256 78dbdcc5...0c9b) -> C:\C2SIM\vrf-nav\shadow_jst
(DSS :541 and JST :521 YuccaPalm -> HoneyMesquiteShortSpring). Same generator, runner (u3\n9_arm.ps1, exit code via
the held handle), busy-check traps; intermediates deleted after.

NULL ratios (this box): (39,33) 0.9388, (39,34) 0.8571, (39,35) 0.8462, (39,36) 0.8438, (39,37) 0.8966, (39,38) 0.8723.

| # | Prediction (EDITED arm only, vs THIS NULL on identical cells) | Confidence | What counts as a MISS |
|---|---|---|---|
| R1 | all four (39,34), (39,35), (39,36), (39,38) >= 0.9 AND (39,37) >= 0.9 | HIGH | any of the five below 0.9 |
| R2 | 0 "YuccaPalm not found" lines in the EDITED log | HIGH | one or more |
| R3 | each of the six sectors' ratio >= its NULL ratio above | MEDIUM | any sector below its NULL ratio |

FALSIFIER: any of the four (39,34/35/36/38) below 0.9 -> the edge column / triangle load is the cause, not the tree;
STOP, no Part 2.

## Result of REGISTRATION 2 - the EDITED arm (written after the arm)

EDITED arm: 2026-09-26 14:22:23Z -> 14:23:01Z (38.7 s wall; "Generation time: 23.9075"), EXIT CODE 0 (held handle,
u3\n9_arm.ps1; busy-check clear). Log C:\C2SIM\vrf-nav\work\log\gen-AO20-edge_jst-2026-09-26.log (26,162 B, sha256
b2c61fd3...05ab); gate output beside it (nav_gate-AO20-edge_jst-2026-09-26.txt: GATE PASS). The log loads
C:\C2SIM\vrf-nav\shadow_jst\...\MAK Earth (online).earth; grid identical to the NULL arm (extent +/-817 x +/-1419, same
six sector rows; area offset at AO20 cells 215.000 / 164.000).

| AO20 sector | NULL ratio / NavData kB / input triangles | EDITED ratio / NavData kB / input triangles |
|---|---|---|
| (39,33) | 0.9388 / 323.2 / 349,958 | 1.0000 / 239.0 / 347,169 |
| (39,34) | 0.8571 / 548.7 / 356,952 | 1.0000 / 256.6 / 349,743 |
| (39,35) | 0.8462 / 731.9 / 371,169 | 1.0000 / 334.2 / 359,795 |
| (39,36) | 0.8438 / 722.9 / 360,650 | 1.0000 / 270.1 / 351,843 |
| (39,37) | 0.8966 / 383.7 / 351,997 | 1.0000 / 215.1 / 347,506 |
| (39,38) | 0.8723 / 348.9 / 381,334 | 1.0000 / 213.3 / 378,685 |

| # | Verdict | Measured |
|---|---|---|
| R1 HIGH | HIT | all five named sectors 1.0000 (node counts 47-63, not degenerate) |
| R2 HIGH | HIT | 0 "YuccaPalm" lines (NULL: 5) |
| R3 MED | HIT | every sector >= its NULL ratio (all six 1.0000) |

Falsifier did not fire. Distinct tags per sector unchanged (3,3,4,4,3,3). Input triangles fell 0.7-3.1 % in every
sector (the N7 signature: fewer tree-crown triangles); NavData fell 26-63 %.

What this measures: on AO20 column 39's exact cells, changing the JST biome's YuccaPalm line to
HoneyMesquiteShortSpring (DSS already changed in both arms) takes all six sectors from 0.84-0.94 to 1.000 and removes
every "YuccaPalm not found" line. Verified: the one-line difference between the two arms' terrain and the result.
Assumed [A]: that the mechanism is the same as N6/N7's (sim-less YuccaPalm geometry fragmenting the mesh) - consistent
with the triangle drop, not isolated further.

Side effects: 9 new files in the vendor tile cache C:\MAK\vrforces5.2d\appData\cache\vrfsim\Biomes-ccf789e273a44b78
(a new, content-keyed folder for the shadow_jst biome configuration; the NULL arm wrote none); the arm's NavDataDebug
intermediates (6 files, 122,579,808 B) deleted. Nothing else new under C:\MAK. -> Part 2.

## PART 2 REGISTRATION (2026-09-26, after the EDITED arm passed, BEFORE the full run)

Brief N9 Part 2 (restated by the coordinator for N10): one full AO20 generation on shadow_jst; register only on a
full pass, into a new terrain copy naming the shadow_jst .earth; README; no fixtures.

RUN KIND: offline. TIER AND GATE: HEAVY / PREREG. Generator, environment, traps and runner as in Part 1
(u3\n9_arm.ps1 -Arm jst: held handle, exit code to u3\n9_jst_status.txt, refuses to start if a sim / GUI / interface /
test suite / generator is running).

ONE VARIABLE against gen-AO20-meso-2026-09-26 (N8): the JST biome's asset line 521 (YuccaPalm ->
HoneyMesquiteShortSpring), on top of N8's DSS line 541.
- Terrain: tools\navdata\out\MAK Earth (online) + MojaveAO20_jst.mtf, a byte copy of the _edge_jst copy (sha256
  78dbdcc5...0c9b; its one changed line names C:/C2SIM/vrf-nav/shadow_jst/TerrainData/TerrainConfiguration/MAK Earth
  (online).earth). shadow_jst TerrainConfiguration re-verified before the run: 541 files, sha256-identical to the vendor
  folder except biome.definitions.CA-fveg.xml (ae5ddb26...647b: lines 521 JST and 541 DSS).
- Config: C:\C2SIM\vrf-nav\work\cfg\NavArea-ground-platform MojaveAO20_jst.navGenConfig, byte copy of the N8 _meso
  config (sha256 d89bc8eb...08b7: 40 x 40, cell 43, raster 0.2). Area "NavArea-ground-platform MojaveAO20_jst".
- Log C:\C2SIM\vrf-nav\work\log\gen-AO20-jst-2026-09-26.log. C: has 683 GB free (N8's intermediates were 10.7 GB).

| # | Prediction | Confidence | What counts as a MISS |
|---|---|---|---|
| Q0 | exit 0; 1,600 sectors; the log loads the shadow_jst .earth; runtime config written; extent (-10019,-9976)..(10062,9976) | HIGH | any limb |
| Q1 | 0 "Sim model config: YuccaPalm not found." lines (N8: 4) | HIGH | any such line |
| Q2 | 0 of 1,600 sectors below 0.5 AND every sector >= 0.9 (row 21 gate PASS) | HIGH | any sector below 0.9 |
| Q3 | the 1,600 - k sectors holding no class 62 are identical to N8 in ratio, NavData size and input triangles, except up to 30 (N8 had 24 unexplained non-64 changes) | MEDIUM | more than 30 such sectors differ |
| Q4 | area folder below N8's 176.9 MB | MEDIUM | >= 176.9 MB |

FALSIFIER: any sector below 0.9 -> the JST edit does not close the gate at full scale; STOP, register nothing, report
the sector(s) and their classes.

Registration (ONLY on a full Q2 pass): make_nav_terrain.py --terrain <the _jst copy> --runtime-config
<...MojaveAO20_jst.navRuntimeConfig> --out <the same _jst copy>; verify the copy still names the shadow_jst .earth;
sha256 + bytes of the copy, the runtime config and the area folder; tools/navdata/README.md updated. Side-effect
sentence to carry: on that copy both the Desert Succulent Shrub and the Joshua Tree biomes place mesquite, not
Joshua trees / yucca palms, for rendering and simulation alike. No fixture is built or deployed. Intermediates deleted.
