# PREREG NAVAO20 MESQUITE - regenerate the full MojaveAO20 on the shadow terrain (DSS YuccaPalm -> HoneyMesquiteShortSpring)

Lane N8, session 5fc25950. Brief N8: "Yes, regenerate AO20 on the shadow terrain". One full generation.
Registration only if every sector passes the gate.

## Registration

PREREG ID: navao20-mesquite-2026-09-26-1
DATE (UTC): 2026-09-26, written and committed BEFORE the generator was started
BINARY / COMMIT: C:\MAK\vrforces5.2d\bin64\vrfNavGenerator.exe (420,352 B); repo branch feat/nav-gate-and-ao20 at origin/main 6966d2e
TIER AND GATE: HEAVY / PREREG

VENDOR CITATION: UG52 ch. 66 (pp1272-1289) is silent on vegetation, procedural trees and biomes as navigation-data
inputs; it documents Prune No-Go Terrain Areas (Table 54 p1279) and min-navigable-surface /
soil-types-to-tag-with-surface-char (66.3.1 p1282) only. Release Notes VRF-8788 (p69) and VRF-9016 (p72) on
simTreesTool / which vegetation affects VRFSIM. The question of how the generator treats an asset whose sim entry is
missing (YuccaPalm, simVegetation.xml:156, trunk "-inf") is open with MAK as case question Q6
(u3\MAK_CASE_ADDENDUM_NAVTAGS_2026-09-26.md), unanswered.

OWN-RECORD CITATION: docs/experiments/PREREG_NAVTREES_YUCCA_2026-09-26.md (N6 edited arm and N7 null arm on the same
5 x 5 box: class-64 sectors below 0.5 null 21 of 24, edited 0 of 24; 64-only NavData 472 -> 97.6 kB; tags identical
25/25; trees enter as input geometry); gen-AO20-2026-09-25 (189 of 1,600 below 0.5, 314 below 0.9, 268.7 MB; tags
1:1,050 2:142 3:400 4:8); PREREG_NAVCONTROL_WEST20_2026-09-15.md:462-470 (the invocation); tools/navdata/README.md.

RUN KIND: offline

## Conditions

CONSOLE LEVEL: 4
(not applicable - no simulation)

PRE-ORDER GATE: --pre-order-gate nav-area
(not applicable - no order)

DurationScale: 1.0

ONE VARIABLE (against gen-AO20-2026-09-25): the Desert Succulent Shrub biome's asset. The terrain is
tools\navdata\out\MAK Earth (online) + MojaveAO20_meso.mtf, byte-identical to the N6 _trees5x5 copy (sha256
30f557b5...ecbd; its one changed line names C:/C2SIM/vrf-nav/shadow/TerrainData/TerrainConfiguration/MAK Earth
(online).earth). The shadow TerrainConfiguration was re-verified before the run: 541 files, sha256-identical to the
vendor folder except osgEarthCatalogs\biome.definitions.CA-fveg.xml (vendor 39e0c41b...bc14, shadow b2b68820...45e9 -
line 541 in <biome id="DSS">: YuccaPalm -> HoneyMesquiteShortSpring); every other SharedData entry is a junction to
the vendor folder. Config: C:\C2SIM\vrf-nav\work\cfg\NavArea-ground-platform MojaveAO20_meso.navGenConfig, a byte copy
of the original 580 B AO20 config (sha256 d89bc8eb...08b7: same box, 40 x 40, cell 43, raster 0.2), renamed only so
the .navRuntimeConfig gets the new area name. Area: "NavArea-ground-platform MojaveAO20_meso".
Known differences besides the variable: the terrain path (a copy under tools\navdata\out, as in N5-N7); --userDataDir;
--navDataDir pre-created (it exists from 2026-09-25).

Command (cwd C:\MAK\vrforces5.2d\bin64; PATH prefix bin64;vrlink5.10\bin64;makRti5.0.1\bin; MAK_VRFDIR, MAK_VRLDIR;
licence from the User scope):
    vrfNavGenerator.exe --terrain "<repo>\tools\navdata\out\MAK Earth (online) + MojaveAO20_meso.mtf"
      --config "C:\Users\PAULOB~1\Temp\nav\cfg\NavArea-ground-platform MojaveAO20_meso.navGenConfig"
      --outputPath "C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveAO20_meso"
      --verbose --navDataDir "C:\C2SIM\vrf-nav\navData\MAK Earth (online)" --userDataDir "C:\C2SIM\vrf-nav\userdata"
      --logFileName "C:\Users\PAULOB~1\Temp\nav\log\gen-AO20-meso-2026-09-26.log"
Launched by a wrapper script (u3\n8_run.ps1) that holds the process handle, waits, and writes the exit code to a file;
progress polled from the foreground. Started only with no vrfSim* / vrfGui* / VrfC2SimApp* / WatchVrf* / pwsh test
suite running; no agents during the run. Possible side effect, measured not prevented: new Biomes tiles in the vendor
tile cache (appData\cache\vrfsim) for the edited configuration over the whole box.

A risk to P2 KNOWN BEFORE THE RUN (computed from the 2026-09-25 table and the N4 land-cover tiles): 4 sectors of the
2026-09-25 area were below 0.9 WITHOUT class 64 - (39,34) 0.859, (39,35) 0.806, (39,36) 0.836, (39,38) 0.895. All four
lie in the 39-cell remainder column and all four contain CA-FVEG class 62 "Joshua Tree", whose JST biome ALSO places
YuccaPalm (biome.definitions.CA-fveg.xml:521) and is NOT edited here. If they stay below 0.9, the gate fails on
non-64 sectors; the brief's reading of that is "a second mechanism", the likelier reading is the same asset through
the JST biome. Both are recorded; nothing is adjusted.

## Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | exit code 0; 1,600 sectors; the log loads the SHADOW .earth with the same layer list as 2026-09-25; "NavArea-ground-platform MojaveAO20_meso.navRuntimeConfig" written; extent (-10019,-9976)..(10062,9976) | HIGH | any limb |
| P1 | 0 "Sim model config: YuccaPalm not found." lines | HIGH (brief); mine MEDIUM - class 62 (JST, YuccaPalm) is in the box, so the line may still appear | any such line |
| P2 | 0 of 1,600 sectors below 0.5 AND every sector >= 0.9 (row 21 gate) | HIGH (brief) for "0 below 0.5"; mine LOW-MEDIUM for "every sector >= 0.9" because of the four class-62 sectors above | any sector below 0.5; any sector below 0.9 |
| P3 | area folder well below 268.7 MB: 120-200 MB | MEDIUM | outside the band |
| P4 | destack start (13,32), N2d start (12,32), V0 (16,37) read 1.000 (2026-09-25: 0.102, 0.444, 0.620) | MEDIUM | any of them below 1.000 |
| P5 | distinct-tag histogram equal to 2026-09-25's (1:1,050 2:142 3:400 4:8) - the N7 null/edit pair kept tags 25/25 | MEDIUM | any bin differs |

FALSIFIER: any class-64 sector below 0.5 -> the 5 x 5 result does not scale; STOP, register nothing. Any sector below
0.9 that does NOT contain class 64 -> STOP, register nothing, report which classes it holds.

Registration (ONLY if every sector passes 0.9): make_nav_terrain.py --terrain <the _meso copy> --runtime-config
<...MojaveAO20_meso.navRuntimeConfig> --out <the same _meso copy>; verify the copy still names the SHADOW .earth;
sha256 + bytes of the copy, the runtime config and the area folder. No fixture is built or deployed.

## Result (written after the harvest, never from a live read)

(to be written after the run)
