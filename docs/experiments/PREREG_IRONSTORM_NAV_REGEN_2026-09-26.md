# PREREG IRONSTORM NAV REGEN - regenerate the IRONSTORM-CENTRE nav area (gen 1 lost) on the vendor terrain

Lane I1 (U3), session 5fc25950. Plan PLAN_IRONSTORM_DEMO.md P1-P2. Generation 1 (2026-09-20, old session a7f6a276) is gone
from disk (C:\C2SIM\vrf-nav\ironstorm-centre-2026-09-20 no longer exists); only its console log survives in that session's
scratch (ironstorm\cuta\gen_nav_console.txt). This run is the new baseline the corridor study compares against.

## Registration

PREREG ID: ironstorm-nav-regen-2026-09-26-1
DATE (UTC): 2026-09-26T20:45Z, written and committed BEFORE the generator was started
BINARY / COMMIT: C:\MAK\vrforces5.2d\bin64\vrfNavGenerator.exe (420,352 B, sha256 c653ffc5...5f2062); branch
feat/ironstorm-nav at origin/main 8cdca96
TIER AND GATE: HEAVY / PREREG

VENDOR CITATION: UG52 ch. 66 (66.2 p1276 the 20 x 20 km cap, 66.2.1 p1277, 66.3 p1283, 66.5 p1287); the tool's own usage
text (C:\C2SIM\vrf-nav\work\log\vrfNavGenerator-help.txt). No vendor text states that a regeneration is deterministic.

OWN-RECORD CITATION: tools/navdata/README.md "Generating an area" (every trap); gen-1 record = old-session scratch
ironstorm\cuta\part3b.md and part4.md (gen 1: exit 0, 1,600 sectors, 1,594 graphed, 33 < 0.5, 313 < 0.9, mean 0.9208,
4,764 files / 298,035,892 B, 2,577.7 s; corridor FAIL on 7 sectors: T02 (14,13) 0.8947 (16,14) 0.8868 (17,15) 0.8800;
T10 (27,22) 0.8475 (27,23) 0.8868 (28,21) 0.8182 (28,22) 0.8571; T14 nudged 0 of 8); PREREG_NAVEDGE_JST_2026-09-26.md
Part 2 Q3 (a Mojave regeneration reproduced every unedited sector unchanged).

RUN KIND: offline

## Conditions

CONSOLE LEVEL: 4
(not applicable - no simulation)

PRE-ORDER GATE: --pre-order-gate nav-area
(not applicable - no order)

DurationScale: 1.0

Inputs. Config C:\C2SIM\vrf-nav\work\cfg\NavArea-ground-platform IRONSTORM-CENTRE.navGenConfig, rebuilt by
mkconfig_ironstorm.py: 572 B, sha256 d0ef8f36297b45dbcef6b276b60aadba602f758b7d7a2ae69c485a0ec3ca9943 = gen 1's
(d0ef8f36...9943). Terrain: the VENDOR C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\MAK Earth
(online).mtf (sha256 d445e109...ba19a23), as gen 1. Output C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform
IRONSTORM-CENTRE; --navDataDir pre-created; --userDataDir C:\C2SIM\vrf-nav\userdata; no --appDataDir; bare --verbose;
stdout to a file; exit code from a held process handle. No sim/RTI process running at start.

Differences from gen 1 that are not the config: --userDataDir (gen 1 used the default under C:\MAK), the output and
log folders (gen 1 used the C:\Users\PAULOB~1\Temp\navi junction), six days of elapsed time on a streamed terrain.

## Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| G0 | exit 0; 1,600 sectors; the .navRuntimeConfig is written; "Adjusted -" corners, extent (-10062,-10019 .. 10105,10019) and "Transition point total: 26802" identical to gen 1 | HIGH | any other grid, a missing runtime config, a non-zero exit | (after) |
| G1 | AREA gate (nav_gate.py) FAILS, exit 1: the < 0.5 tail sits on lakes; 20-50 sectors < 0.5 (gen 1: 33) and 1-12 graph-less (gen 1: 6) | HIGH (FAIL) / MEDIUM (counts) | area gate PASS; or the counts outside the bands | (after) |
| G2 | CORRIDOR gate FAILS on the cut-A legs (T02, T10, T14 nudged; 2 m sampling): 5-9 corridor sectors < 0.9 (gen 1: 7), worst 0.80-0.86 (gen 1: 0.8182 at (28,21)), 0 corridor sectors < 0.5, T14 clean | MEDIUM | a corridor PASS, or a count/worst outside the bands | (after) |
| G3 | The per-sector log numbers (triangles, nodes, neighbours, tags) equal gen 1's in every sector; total ~298.0 MB / ~4,764 files | MEDIUM | any sector differing from gen 1 | (after) |

Why the corridor band sits on gen 1's values: same config bytes, same binary, same vendor terrain file; the Mojave
regenerations reproduced unedited sectors unchanged. The band is widened (not +/-0) because MAK Earth (online) is
streamed and its content can change over time (WEST20 PART 2).

FALSIFIER: a result byte-different from gen 1 (G3 miss) means an input changed. It is REPORTED with the sectors that
moved, not explained away and not re-rolled.

ONE VARIABLE: none intended - this is a like-for-like regeneration of gen 1 (control = gen 1's console log).

Owner decision 2026-09-26 (relayed by the seat): "No, fix the corridor first" - if the corridor gate FAILS, stop after
the gate: register nothing, deploy nothing, keep the area and the log.

## Result (written after the harvest, never from a live read)

(pending)
