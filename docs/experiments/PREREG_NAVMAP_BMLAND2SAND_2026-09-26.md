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

RUN: 2026-09-26 11:20:58Z -> 11:56:21Z (log last write), ~2,122 s wall; the generator's own "Generation time: 2076.82".
Started after two consecutive clear polls (another lane's RunnerTurnaround suite was running at 11:18Z and was waited
out). Stdout ends in the normal shutdown sequence; stderr empty. Exit code NOT captured (the process handle lived in an
earlier shell call). Log C:\C2SIM\vrf-nav\work\log\gen-AO20-bmland2sand-2026-09-26.log, 5,430,571 B, sha256
3eb5ca4f...f22e. Gate output saved beside it (nav_gate-AO20-bmland2sand-2026-09-26.txt / .json).

| # | Verdict | Measured |
|---|---|---|
| P0 | HIT on every observed limb; exit code not captured | 1,600 "Sector (" rows; "Generation time: 2076.82"; extent (-10019,-9976)..(10062,9976); the .navRuntimeConfig WAS written into the pre-created --navDataDir (773 B). Its FILE NAME follows the --config base name, "NavArea-ground-platform MojaveAO20.navRuntimeConfig", not the --outputPath area name; its nav-data-path names ...\NavArea-ground-platform MojaveAO20_bmland2sand and its original-terrain names the terrain copy. Nothing new under C:\MAK (bin64, userData, SharedData, appData: 0 files newer than the start). The 1,600 .ClientInput intermediates (10,814,658,196 B) went to C:\C2SIM\vrf-nav\userdata\NavDataDebug as intended |
| P1 | HIT (tool consistency only) | landcover_sector_map.py with the new map: 0 dryground sectors |
| P2 | MISS | tags 1: 1,100 / 2: 485 / 3: 15 / 4: 0 - 1-tag sectors 1,100 < 1,500, and 15 sectors still carry 3 tags |
| P3 | MISS | 189 sectors below 0.5, 314 below 0.9, 1,210 at 1.00, min 0.0185 at (13,36) - the SAME numbers and the same failing sectors as the 2026-09-25 run |
| P4 | MISS on size, HIT on duration | 4,803 files, 268,703,128 B (268.7 MB; outside 150-190); 2,076.8 s (inside 1,300-2,500) |

FALSIFIER FIRED (more than 20 fragmented sectors, and 3-tag sectors remain). STOP: nothing was registered, no fixture
was touched.

The override WAS read. Per sector, 2026-09-25 -> today: tags 1->1 in 1,050 sectors, 2->1 in 50, 2->2 in 92, 3->2 in
393, 3->3 in 7, 4->3 in 8. Every change (451 sectors) is a loss of exactly one tag; 446 of the 451 are
sectors where N4 found a dryground class (5 are not), and 88 dryground sectors kept their count.
The generator log itself prints no line naming the terrain-specific map (the DLL string "Paging in terrain landCover
characteristics file" never appears), so the proof is the tag change, as registered.

The navigation data did NOT change. Against the 2026-09-25 area: per-sector input triangle counts identical in 1,600
of 1,600 sectors; per-sector "NavData Size" identical in 1,600 of 1,600; 4,802 of the 4,803 files have the same size - the exception is Generator.GenIO, 3,004 vs 3,028 B, i.e. 2 x the
12 characters of "_bmland2sand" in its recorded paths, which is also the whole 24 B difference of the area total (file
names carry a different area suffix, uLxq vs rXUU; in a 1-in-40 sample the contents differ in 8-76 bytes in most
files and in 100-2,100 bytes in the rest - not attributed); connectivity ratios identical sector for sector. The ClientInput total is identical to the byte (10,814,658,196 B).

What this measures. Mapping BM_LAND to sand removes one distinct nav tag from 451 sectors and changes nothing else the
generator writes: the NavMesh size, the abstract graphs and the fragmentation are the same to the byte count and to the
ratio. The per-sector distinct-tag count is therefore not what fragments the abstract graphs on this area; the
tag-change / fragmentation association of PREREG_NAVCONTROL_WEST20_2026-09-15.md:595-597 is a correlate, and its
mechanism sentence (:662-664, "the input that changed is the per-sector surface classification") is not supported by
this intervention.

Design implication, stated separately: a land-cover soil remap (this lever) does not produce a passing area and is not
pursued; nothing in this run was registered.

Side effect, as a measurement: in the terrain copy's map, BM_LAND areas are sand for anything that reads the map,
including the simulation if a scenario used this copy. No scenario does. On soil effects in 5.2 the record carries
the Table 26 soil -> roughness mapping (UG52 p506) and the movement sysdef soil-list factors (UG52 p507); a 5.2 change
to per-soil max-speed factors is referred to in the brief as CLAUDE.md sec 2 D7 - not re-read here, so not asserted.

UNEXPLAINED, carried:
1. What still carves the mesh along the same boundaries. The byte sizes say the polygon structure is unchanged, so the
   regions that N4 located (CA-FVEG Desert Succulent Shrub / Desert Wash next to Desert Scrub) are still separated in
   the mesh even though their soil tag is now the same. Candidates not tested: the CA-FVEG class attributes (dense,
   lush, rugged, traits) through the vendor's biome / procedural vegetation (the run streamed "Biomes" and "Life-Map"
   caches, lane N 2026-09-25), which would add geometry or no-go regions per class.
2. Why 92 sectors that became sand-only keep 2 tags, and 15 keep 3.
3. The 4,803 vs the record's 4,804 files (unchanged from 2026-09-25).

Confounds registered before the run and still open: the terrain path/name (content byte-identical) and --userDataDir.
Neither can explain an UNCHANGED mesh; they matter only if the tags had not changed.

VERIFIED in passing (feeds FINDING_NAV_TAGS_OSM_REFUTED_2026-09-26.md sec 2): this run's .navRuntimeConfig gives the
area offset (-2361540.715256, -4695377.073151, 3602563.309340); in the ENU frame about the adjusted-corner centroid it
sits at (-21.50, 0.00) m, i.e. the +21.48 m east shift fitted by osm_sector_map.py is the true offset to 0.02 m.
