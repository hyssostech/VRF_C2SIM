# PREREG N3: expand-to-compose determinism, 3 runs

Date: 2026-09-06. Tier: STANDARD (no code change; a determinism claim needs 3 runs before it is
written into the CLOSED list - C5 says "124700Z, 1 run; 3x owed").
Ruling: DESIGN_ORBAT_TO_VRF C5 (coarse leaf at company+ = expand-to-compose from the mapped
template's doctrinal sub-units, HQ first). The flag-off leg (N3's old second half) is covered by
PREREG_N1 run L and is not repeated.

## Setup
`RunC2SimScenario.ps1 -VrfProfile 5.2 -NoGui -Scenario R9_Mojave_Empty_52 -Init
data\GA_LeafCompany_Initialization.xml -Order data\R9_Mojave_UnitMove_Order.xml -RunSecs 360`,
DEFAULT settings (compose ON by default since N1; no env). 114.MechCoy is a childless leaf, so
ExpandCoarseLeaves reads its mapped template's <subordinate> list (HQ + 3 platoons) and composes.
Three consecutive runs X1, X2, X3 on the preserved rtiexec 15720 / forwarder 43728.

## Predictions (before the runs)
- Each run HIGH: app log "ComposeHierarchy: ... expand ..." lines naming HQ + 3 sub-units,
  "composed - 4/4" (or the exact declared count) attached; 3/3 VRF task complete + 3/3 TASKCMPLT;
  the MechCoy cluster (now the expanded members) moves north together, no member > 2 km, final
  spread < 1.5 km; 0 dropped; runner exit 0.
- Determinism = 3 of 3 runs meet the line above. FALSIFIER: any run with the company not
  completing or a runaway member -> expand is NOT deterministic; open the object console at
  level 4 on the failing configuration (C11) before touching code.

## Results
Baseline (C5, 124700Z): EXPAND 114.MechCoy (Tank Company (USA)) -> [HQ1, TANK2, TANK3, TANK4],
4/4 attached, 3/3 TASKCMPLT, cluster 23, 4 movers > 1 km north, spread 787 m.
| run | expand line | attached | TASKCMPLT | cluster | movers > 1 km (brg) | max spread | runaway > 2 km |
|---|---|---|---|---|---|---|---|
| X1 170113Z | HQ1, TANK2, TANK3, TANK4 | 4/4 | 3/3 | 23 | 4 (357-3 deg, 1014-1040 m) | 787 m | 0 |
| X2 171105Z | HQ1, TANK2, TANK3, TANK4 | 4/4 | 3/3 | 23 | 4 (357-3 deg) | 789 m | 0 |
| X3 172132Z | HQ1, TANK2, TANK3, TANK4 | 4/4 | 3/3 | 23 | 4 (357-3 deg) | 790 m | 0 |

VERDICT: N3 DONE - expand-to-compose is DETERMINISTIC, 3 of 3 (plus the C5 run: 4 of 4),
identical expand line, attach count, completion, cluster size and geometry within 3 m of spread.
C5's "3x owed" is paid. Runner exit 0 on all three; rtiexec 15720 / forwarder 43728 preserved
across 14 consecutive launcher fixtures today without a wedge.
