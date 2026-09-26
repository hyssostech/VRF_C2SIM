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

RUN: 2026-09-26 13:31:50Z -> 14:09:11Z, 2,240.7 s wall (the generator's own "Generation time: 2227.64"), EXIT CODE 0
(held process handle in u3\n8_run.ps1). No vrfSim / vrfGui / VrfC2SimApp / WatchVrf / test suite at the start; other
sessions' Claude processes used about three of 32 cores. Log C:\C2SIM\vrf-nav\work\log\gen-AO20-meso-2026-09-26.log
(5,384,655 B, sha256 b0b99bd9...ed56); gate output beside it (nav_gate-AO20-meso-2026-09-26.txt / .json). The log
loads the SHADOW .earth; feature/elevation layer lines identical to 2026-09-25; tag volumes 2 / 0 / 416 as on
2026-09-25. Runtime config C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform
MojaveAO20_meso.navRuntimeConfig (759 B, sha256 3cb5c847...d624): extents and offset identical to the 2026-09-26
bmland2sand config, nav-data-path the _meso folder, original-terrain the _meso copy.

| # | Verdict | Measured |
|---|---|---|
| P0 HIGH | HIT | exit 0; 1,600 sectors; shadow .earth; runtime config written; extent (-10019,-9976)..(10062,9976) |
| P1 HIGH (brief) / MEDIUM (mine) | MISS | 4 "Sim model config: YuccaPalm not found." lines - the JST biome (class 62 Joshua Tree) still places YuccaPalm, as flagged before the run |
| P2 HIGH | "0 below 0.5": HIT; "every sector >= 0.9": MISS | 0 of 1,600 below 0.5 (2026-09-25: 189); 4 below 0.9: (39,35) 0.806, (39,36) 0.836, (39,34) 0.859, (39,38) 0.895 - GATE FAIL |
| P3 MED | HIT | 4,803 files, 176,903,872 B (176.9 MB; 2026-09-25: 268.7 MB) |
| P4 MED | HIT | destack start (13,32) 0.102 -> 1.000; N2d start (12,32) 0.444 -> 1.000; V0 (16,37) 0.620 -> 1.000 (P11 (9,32) 1.000 as before) |
| P5 MED | MISS | tags 1:1,052 2:145 3:395 4:8 (2026-09-25 1:1,050 2:142 3:400 4:8); 11 sectors changed (3->2 x7, 2->1 x2, 2->3 x2) |

FALSIFIER, second limb, FIRED: four sectors below 0.9 that do NOT contain class 64. STOP; nothing registered; no
terrain copy with nav records was made; no fixture touched. Their classes (N4 land-cover tiles): (39,34) 60 Desert
Scrub, 61 Desert Wash, 62 Joshua Tree; (39,35) and (39,36) 9 Barren, 55 Pinyon-Juniper, 60, 61, 62; (39,38) 9, 60, 62.
Every one contains class 62 (Joshua Tree, JST biome: YuccaPalm, biome.definitions.CA-fveg.xml:521, NOT edited) and
every one lies in the 39-cell remainder column (about 350,000-370,000 input triangles, 3.5 x a regular sector). All
four are BYTE-FOR-BYTE unchanged from 2026-09-25 in ratio, NavData size (543.2 / 729.4 / 726.3 / 349.4 kB) and input
triangles: the DSS edit did not reach them. The first limb of the falsifier (a class-64 sector below 0.5) did NOT fire.

Per-sector comparison against 2026-09-25 (N4 classes):
- class-64 sectors (396): 395 changed; 0 below 0.5 and 0 below 0.9 (2026-09-25: 189 below 0.5, 310 below 0.9);
  the 48 sectors holding only class 64 (N4 L12 sampling): NavData median 462.4 -> 95.6 kB.
- non-64 sectors (1,204): 1,180 identical in ratio, NavData size and input triangles; 24 changed, most of them
  holding class 61 Desert Wash (a mesquite biome) - not explained by the class table at 10 m sampling [A: sub-pixel
  class-64 presence or the biome layer's own resolution]; none of the 24 is below 0.9.

What this measures. With Desert Succulent Shrub's single asset changed from YuccaPalm to HoneyMesquiteShortSpring,
the full MojaveAO20 generates with no fragmented sector (189 -> 0 below 0.5), the named route points all at 1.000, and
268.7 -> 176.9 MB of navigation data; the gate still fails on four remainder-column sectors that contain class 62 Joshua
Tree and are identical to 2026-09-25 (ratios 0.81-0.89).

Design implications, stated separately: (1) the DSS edit is a working lever at full scale. (2) The remaining four
sectors point at the same asset through the JST biome - the reading is untested; editing JST's YuccaPalm line (521)
the same way, or checking whether any route crosses column 39 (the east edge), are the seat's options. The gate
rule (row 21) is ANY sector, so nothing was registered.

Side effects: 366 new files (721,992 B) in the vendor tile cache C:\MAK\vrforces5.2d\appData\cache\vrfsim
(pre-registered); 1,600 ClientInput intermediates (10,657,765,816 B) in C:\C2SIM\vrf-nav\userdata\NavDataDebug. The
area folder stays at C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveAO20_meso (manifest
sha256 2a808d73...f8a9).
Side effect for the terrain, as a measurement: on the shadow terrain copy the Desert Succulent Shrub biome places
mesquite, not Joshua trees, for rendering and simulation alike.
