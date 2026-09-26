# PREREG IRONSTORM TREES - are biome 04's trees what holds the Iron Storm corridor sectors below 0.9? (box control triples)

Lane I1b (U3), session 5fc25950. Owner: "No, fix the corridor first". Input: the corridor study
FINDING_IRONSTORM_CORRIDOR_2026-09-26.md (branch feat/ironstorm-orders, 3dbc647) - CLCplus forest share and water share
are separate risk factors; NavData size x2.5 with forest while input triangles +6 % [A: trunks carved out of the mesh].
Part 1 = two boxes x three arms, small offline generations. Part 2 (one full IRONSTORM-CENTRE regeneration on the L1b
shadow, then register + fixture + deploy) only if Part 1 passes, with its own prereg block appended below BEFORE it runs.

## Registration

PREREG ID: ironstorm-trees-2026-09-26-1
DATE (UTC): 2026-09-26; the commit that adds this file is the registration time, and no arm starts before it
BINARY / COMMIT: C:\MAK\vrforces5.2d\bin64\vrfNavGenerator.exe (420,352 B, sha256 c653ffc5...5f2062); branch
feat/ironstorm-nav at 4c88171
TIER AND GATE: HEAVY / PREREG

VENDOR CITATION: osgEarthCatalogs\biome.definitions.bioregions.xml:143-149 (biome 04 "Temperate Broadleaf and Mixed
Forests": RedMapleSpring :147, AmericanSycamoreFullSpring fill 0.6 :148, WhiteOakSpring fill 0.6 :149; bushes and
undergrowth :151-160, incl. AmericanSycamoreSeedling fill 0.3 :154); :524-526, :2330-2332, :2365-2367, :2490-2492 (04-PA
and the Baltic / Central European / Sarmatic ecoregions carry no assets, so they inherit 04); biome_mappings_bioregions
.xml:65/:140/:612; simVegetation.xml:74 RedMapleSpring trunk width 0.15 m, :99 WhiteOakSpring 0.45 m, :103
AmericanSycamoreFullSpring 0.74 m. RN VRF-8788 p69 / VRF-9016 p72 (simTreesTool). UG52 ch. 66 is silent on trees as
navigation inputs.

OWN-RECORD CITATION: PREREG_IRONSTORM_NAV_REGEN_2026-09-26.md (the baseline: every sector identical to gen 1; corridor
FAIL on 7 sectors); FINDING_IRONSTORM_CORRIDOR_2026-09-26.md secs 3-5 (levers L1a / L1b, box extents);
PREREG_NAVEDGE_JST_2026-09-26.md :113-127 (an isolated box moved a sector by up to 0.04 against the full area on
identical cells) and :36-44 (even cell counts centred on a cell edge keep a box on the parent grid);
PREREG_NAVTREES_YUCCA_2026-09-26.md (the control-pair method); tools/navdata/README.md (the generator traps).

RUN KIND: offline

## Conditions

CONSOLE LEVEL: 4
(not applicable - no simulation)

PRE-ORDER GATE: --pre-order-gate nav-area
(not applicable - no order)

DurationScale: 1.0

DEVIATION FROM RECORD: the brief names "box A over T02's failing sectors (14..18 x 12..16 ...) box B over T10's
(26..30 x 20..24)"; both are 5 x 5 = 55 cells, an ODD count, which the record says drifts half a cell off the parent
grid. Used instead (the tightest EVEN boxes that keep a one-sector margin round every failing sector):
- Box A: parent sectors i 13..18 x j 12..17 (6 x 6, cells x -91..-26, y -101..-36, 66 x 66), tile-count 6 x 6.
  Holds T02's (14,13) 0.8947, (16,14) 0.8868, (17,15) 0.8800, and three non-corridor sub-0.9 sectors (14,15) 0.865,
  (15,15) 0.814, (18,13) 0.833.
- Box B: parent sectors i 26..29 x j 20..25 (4 x 6, cells x 52..95, y -13..52, 44 x 66), tile-count 4 x 6.
  Holds T10's (27,22) 0.8475, (27,23) 0.8868, (28,21) 0.8182, (28,22) 0.8571.
Configs from make_tree_control.py box --log <baseline log> --inset 3 --height 150, frame = the baseline's runtime
offset: IS_A 570 B sha256 ae59e99c...c724, IS_B 570 B sha256 f2a0c720...8185, copied byte-identically to one file
per arm (the runtime config is named after the config file).

The three arms (identical config per box; ONE variable per edited arm: biome 04's tree lines):
- NULL: the VENDOR terrain C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\MAK Earth (online).mtf.
- L1a (cause test): shadow C:\C2SIM\vrf-nav\shadow_is04_notrees; bioregions.xml differs from the vendor's ONLY by the
  three tree lines :147-149 removed (copy sha256 7c3cb641...ac06; vendor 85301731...7e65). Terrain copy
  tools\navdata\out\MAK Earth (online) + IS04_notrees.mtf (main checkout; sha256 3c680595...e18e). Bushes, undergrowth
  and AmericanSycamoreSeedling (no simVegetation entry) stay.
- L1b (product lever): shadow C:\C2SIM\vrf-nav\shadow_is04_maple; :148 and :149 read RedMapleSpring (fill 0.6 kept)
  (copy sha256 ce10d60a...9e87). Terrain copy ...\MAK Earth (online) + IS04_maple.mtf (sha256 5cfd8324...6de5).
`diff -rq` of each shadow's TerrainConfiguration against the vendor's: that one file only.
Generator: --navDataDir C:\C2SIM\vrf-nav\navData\MAK Earth (online) (exists); --userDataDir C:\C2SIM\vrf-nav\userdata;
no --appDataDir; bare --verbose; stdout to a file; exit code from a held handle; refused if a sim / GUI / interface /
suite / other generator runs. Box sector (bi,bj) = parent sector (i0+bi, j0+bj).

## Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| B0 | Every arm: exit 0; box A extent (-1419,-1419 .. 1419,1419), box B (-946,-1419 .. 946,1419); the box offset lies within 2 m of the predicted parent cell edge (A: cells x -58, y -68; B: x 74, y 20) | HIGH | another extent, or an offset > 2 m off -> box artefact, STOP |
| N1 | NULL reproduces each of the 7 corridor sectors within +/- 0.04 of the baseline AND each stays < 0.9 | HIGH | any of the 7 outside +/- 0.04, or at >= 0.9 in NULL -> box artefact for that sector, STOP |
| M1 | Manipulation check: L1a and L1b differ from NULL in the forest sectors (input triangles or NavData size), and a new content-keyed Biomes-* cache folder appears | HIGH | L1a identical to NULL -> the edit did not reach the generator (biome 04 not in effect); STOP - this is NOT evidence about trees |
| A1 | L1a lifts every one of the 7 to >= 0.9 | HIGH for the five forest-led sectors (14,13) (16,14) (17,15) (27,22) (27,23); MEDIUM for (28,21) / (28,22) (forest 0.13 / 0.10, water 0.34 / 0.23) | any of the 7 < 0.9 -> trees are not the (whole) cause; report what remains with its water share; STOP |
| A2 | L1b lifts every one of the 7 to >= 0.9 | MEDIUM | any of the 7 < 0.9 -> the product lever fails; no Part 2 |
| A3 | L1a NavData size in the two pure dry forest sectors (14,13) (16,14) falls by >= 40 % against NULL (trunks no longer carved) | MEDIUM | a fall < 20 % |

Descriptive (not scored): the three non-corridor sub-0.9 sectors of box A in each arm; every in-box sector's NULL
deviation from the baseline.

ONE VARIABLE: per box, L1a vs NULL = biome 04's three tree lines removed; L1b vs NULL = two of them renamed to
RedMapleSpring. Control = that box's NULL arm (not the full-area values: an isolated box is not the full area).

FALSIFIERS: N1 or B0 miss -> box artefact, stop. M1 miss -> the edit is inert, stop. A1 miss -> trees are not the cause
of every failing sector; report the remainder (water?), stop, no Part 2. A2 miss -> no Part 2.

## Result (written after the harvest, never from a live read)

Registered by commit f3b5548 (2026-09-26T21:57:14Z). Two arms ran, both NULL, on the vendor terrain:

| Box | Arm | Start | Exit | Wall |
|---|---|---|---|---|
| A | IS_A_null | 21:57:22Z | 0 | 91.7 s |
| B | IS_B_null | 22:08:26Z | 0 | 47.6 s |

The first try at box B was refused by the busy-check while another lane's RunnerTurnaround suite ran; it started when
that suite ended. Logs: C:\C2SIM\vrf-nav\work\log\gen-IS_{A,B}_null-2026-09-26.log. Areas and runtime configs: under
C:\C2SIM\vrf-nav\navData\MAK Earth (online) (kept as the controls for any re-registration).

- **B0 HIGH: HIT.** Box A extent (-1419,-1419 .. 1419,1419) and box B (-946,-1419 .. 946,1419). Each box offset lies
  0.01 m from the predicted parent cell edge (A cells -58.000 / -68.000; B 74.000 / 20.000). There are 36 and 24
  sectors. The grid method is exact.
- **N1 HIGH: MISS, on 2 of 7.** Five of the seven corridor sectors reproduce within +/- 0.03 and stay below 0.9. Two
  rise above 0.9 in NULL. Both deviations are inside +/- 0.04, but the registration makes a NULL reading >= 0.9 a
  miss.

| Box | Sector | Baseline | NULL | Scored |
|---|---|---|---|---|
| A | (14,13) | 0.8947 | **0.9298** | MISS (>= 0.9) |
| A | (16,14) | 0.8868 | 0.8868 | reproduced |
| A | (17,15) | 0.8800 | 0.8800 | reproduced |
| B | (27,22) | 0.8475 | 0.8644 | reproduced |
| B | (27,23) | 0.8868 | **0.9057** | MISS (>= 0.9) |
| B | (28,21) | 0.8182 | 0.8235 | reproduced |
| B | (28,22) | 0.8571 | 0.8857 | reproduced |

- Largest in-box deviation from the baseline: A 0.0453, B 0.0467.
- M1, A1, A2, A3: NOT MEASURED. As registered, the N1 miss is a STOP: neither L1a nor L1b was started, and no Part 2
  runs.

What the NULL arms measure. With the grid exact, a box still does not reproduce the full area's ratios closely enough
to tell a sector just under 0.9 from one just over it. Three observations, all [V]:

- (a) **Identical mesh input, different ratio.** (28,22) has the same input triangles (139,434) and the same NavData
  size (69 kB) as the baseline, yet reads 0.8857 against 0.8571. So the abstract graph depends on more than the
  sector's own mesh: the transition points on the box boundary differ from the full area's. The mechanism is [A].
- (b) **Box-edge sectors take extra input.** Sectors on a box's outer column and row carry 8-18 % MORE input triangles
  than in the full area, e.g. (18,17) 173,061 vs 145,693 and (29,25) 167,407 vs 141,592. The margin ring kept every
  corridor sector interior, and interior sectors match to within 0.5 %.
- (c) **The deviation band repeats Mojave's.** Deviations of up to 0.045-0.047 match the up to 0.04 that
  PREREG_NAVEDGE_JST measured.

Design implication, stated separately.

- A box can still test the tree lever on the sectors its own NULL keeps below 0.9. Those are box A (16,14), (17,15),
  (14,15) 0.8654, (15,15) 0.8305, (18,13) 0.8125, (18,16) 0.8696 and box B (27,22), (28,21), (28,22), (27,21) 0.8929.
  Scoring each edited arm against its own box NULL is the Mojave N10 "Registration 2" pattern, which needs owner
  approval.
- Two corridor sectors, (14,13) and (27,23), cannot be tested in a box, because the box NULL already passes them. Only
  a full-area run can settle them.
- A cheaper route to the same answer is one full-area regeneration of L1a (about 60 min). It has no box artefact, and
  its NULL is the baseline itself.

Owner / seat decision needed. The options:

- (i) Register a box Registration 2 against the box NULLs, knowing (14,13) and (27,23) stay untested.
- (ii) Skip the boxes: run a full L1a and a full L1b generation, each against the baseline, about 1 h each.
- (iii) Stop.

Side effects:

- Under C:\MAK, 0 files were written or touched since the registration.
- The two shadows C:\C2SIM\vrf-nav\shadow_is04_{notrees,maple} and the terrain copies (main checkout)
  tools\navdata\out\MAK Earth (online) + IS04_{notrees,maple}.mtf are prepared and unused. They are kept for the
  decision.
- The 60 NavDataDebug intermediates (518,104,260 B) were deleted.
