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

(to be written after both arms)
