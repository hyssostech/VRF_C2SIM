# PREREG NAVMAP BMLAND2SAND - regenerate MojaveAO20 with a terrain-specific land-cover map that sends BM_LAND to sand

Lane N5, session 5fc25950. Brief N5 (lever A: a terrain-specific map). Offline generation, one run.

## Registration

PREREG ID: navmap-bmland2sand-2026-09-26-1
DATE (UTC): 2026-09-26, written and committed BEFORE the generator was started
BINARY / COMMIT: C:\MAK\vrforces5.2d\bin64\vrfNavGenerator.exe (420,352 B, the binary of every earlier AO20 run); repo branch feat/nav-gate-and-ao20
TIER AND GATE: HEAVY / PREREG

VENDOR CITATION: VR-Forces 5.2 Release Notes VRF-7074 (p64, also p8): "You can now define a terrain-specific land
cover mapping that overrides the global one, for example, myTerrain.landCoverDataSurfChar.map." The vendor's local
example, TerrainConfiguration\Example_Ala Moana.landCoverDataSurfChar.map, sits beside Ala Moana.mtf and has the form
"SurfaceCharacteristicMap / { / Match<TAB>BM_WATER<TAB>shallowlake / }". UG52 66.5 p1287 (navigation area files) and
66.3.1 p1282 / 23.5 p505 (soil-types-to-tag-with-surface-char). The lookup rule itself (which folder, which base name)
is in the "Composing a Terrain from Source Data" chapter of the help / the Adding Content manual, NEITHER installed; the
only further vendor evidence is two strings in bin64\vantageTerrainImplementation.dll, ".landCoverDataSurfChar.map" and
"Paging in terrain landCover characteristics file " (read as strings, not documentation).

OWN-RECORD CITATION: docs/experiments/FINDING_NAV_TAGS_OSM_REFUTED_2026-09-26.md sec 8 (the extra nav tags on the
regenerated MojaveAO20 follow the CA-FVEG classes whose chain ends at BM_LAND -> dryground, landCoverDataSurfChar.map:246;
3-tag = dryground + sand); PREREG_NAVCONTROL_WEST20_2026-09-15.md:462-470 (the invocation) and tools/navdata/README.md
(the traps). Baseline run: gen-AO20-2026-09-25 (189 of 1,600 sectors below 0.5, 314 below 0.9; tags 1:1,050 2:142
3:400 4:8; 268.7 MB; 2,263.5 s).

RUN KIND: offline

## Conditions

CONSOLE LEVEL: 4
(not applicable - no simulation; kept for the template)

PRE-ORDER GATE: --pre-order-gate nav-area
(not applicable - no order is pushed; kept for the template)

DurationScale: 1.0

ONE VARIABLE: the land-cover map. The terrain copy
tools\navdata\out\MAK Earth (online) + MojaveAO20_bmland2sand.mtf is a byte copy of the vendor MAK Earth (online).mtf
(sha256 d445e109...9a23, 3,007,370 B); beside it, a byte copy of MAK Earth (online).surfChar.map (253c042e...bce0) and
MAK Earth (online) + MojaveAO20_bmland2sand.landCoverDataSurfChar.map = the installed global map (59678d11...8fb2) with
ONE line changed, "Match<TAB>BM_LAND<TAB>dryground" -> "Match<TAB>BM_LAND<TAB>sand" (sha256 d537499d...a5cf, 10,260 B),
written by tools/navdata/make_landcover_map.py. The sand spelling is the global map's own: "Match<TAB>BM_SAND<TAB>sand"
(:317). Held: the 580 B .navGenConfig (sha256 d89bc8eb...08b7), profile ground-platform (vendor file untouched), the
binary, cwd and environment.
Known differences besides the variable, registered so the result cannot drop them: (1) the terrain path and name
(content byte-identical); (2) --userDataDir C:\C2SIM\vrf-nav\userdata (the README rule; on 2026-09-25 the default
userData\terrainIndices was EMPTY, so nothing the generator could read lived there); (3) --navDataDir pre-created.

Command (cwd C:\MAK\vrforces5.2d\bin64; PATH prefixed bin64;vrlink5.10\bin64;makRti5.0.1\bin; MAK_VRFDIR, MAK_VRLDIR;
licence from the User scope):
    vrfNavGenerator.exe --terrain "<repo>\tools\navdata\out\MAK Earth (online) + MojaveAO20_bmland2sand.mtf"
      --config "C:\Users\PAULOB~1\Temp\nav\cfg\NavArea-ground-platform MojaveAO20.navGenConfig"
      --outputPath "C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveAO20_bmland2sand"
      --verbose --navDataDir "C:\C2SIM\vrf-nav\navData\MAK Earth (online)" --userDataDir "C:\C2SIM\vrf-nav\userdata"
      --logFileName "C:\Users\PAULOB~1\Temp\nav\log\gen-AO20-bmland2sand-2026-09-26.log"
    stdout/stderr -> C:\C2SIM\vrf-nav\work\log\gen-AO20-bmland2sand-2026-09-26.{stdout,stderr}.txt
Started only with no vrfSim / vrfGui process and no other lane's test suite (pwsh) running.

HOW THE OVERRIDE IS PROVEN READ: (a) a log line of the shape "Paging in terrain landCover characteristics file
<our path>" - the string exists in the vendor DLL, but whether it reaches the generator's log is unknown (the 2026-09-25
log printed no surface-characteristics line of any kind); (b) failing (a), the generator's OWN per-sector tag counts:
if they collapse as P2 predicts, the override changed what the generator was fed. Re-running
tools/navdata/landcover_sector_map.py with the new map is a consistency check of the tool, not proof of reading - the
tool reads the same files.

## Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | the run completes (exit 0 or a normal shutdown, 1,600 "Sector (" rows, "Generation time:"), the .navRuntimeConfig IS written into the pre-created --navDataDir, the log's extent equals (-10019,-9976)..(10062,9976), and no new file appears under C:\MAK except the known appData\cache\vrfsim tile cache | HIGH | any limb fails | |
| P1 | per-sector soils from the tool with the new map: 0 sectors hold dryground | HIGH (tool consistency) | any dryground sector | |
| P2 | tag histogram collapses: 1-tag sectors >= 1,500 of 1,600 and NO 3- or 4-tag sector | HIGH (brief); my own confidence MEDIUM - the lookup rule is undocumented locally and FINDING sec 8 leaves unexplained why an unlisted soil adds a tag at all | 1-tag < 1,500, or any 3/4-tag sector | |
| P3 | fragmented (ratio < 0.5) 0-5 sectors AND the 0.9 gate passes on EVERY sector | HIGH (brief); mine MEDIUM, same reasons | > 5 below 0.5, or any sector below 0.9 | |
| P4 | area folder 150-190 MB; duration 1,300-2,500 s | MEDIUM | outside either band | |

FALSIFIER: more than 20 fragmented sectors, or any 3-tag sector, or dryground still present -> the override did not take
or the mechanism is elsewhere. STOP, register nothing, report. If the log shows no override line AND the tags do not
collapse, the two readings (override not read / mechanism elsewhere) are NOT separated by this run; say so.

Registration only if EVERY sector passes 0.9: make_nav_terrain.py --terrain <the bmland2sand copy> --runtime-config
<the new .navRuntimeConfig> --out <same copy>; sha256 + bytes recorded; no fixture is built or deployed.

## Result (written after the harvest, never from a live read)

(to be written after the run)
