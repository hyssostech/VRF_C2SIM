# FINDING: the extra nav tags on MojaveAO20 do not follow OSM road ways (2026-09-26)

Tier: HEAVY (a cause claim was tested and refuted). Lanes N3 (Part A) and N4, session 5fc25950. Offline and
read-only: no generation, nothing written under C:\MAK, no process touched. Instrument:
tools/navdata/osm_sector_map.py (this commit). Object: the regenerated MojaveAO20 of 2026-09-25, log
C:\C2SIM\vrf-nav\work\log\gen-AO20-2026-09-25.log (sha256 21bc3e31...e9ea), 1,600 sectors, 189 fragmented
(ratio < 0.5), 314 below 0.9, distinct-nav-tag histogram 1:1,050 / 2:142 / 3:400 / 4:8 (nav_gate.py; the same
numbers as the 2026-09-15 regeneration, PREREG_NAVCONTROL_WEST20_2026-09-15.md:559, :585-590).

## 1. The hypothesis tested

Source: the seat's research note laneQ_navinput_research.md (session scratchpad, 2026-09-25; not in the repo).
It reads the vendor's ground-platform profile, navigationProfiles.mtl:261-264 (soil-types-to-tag-with-surface-char
= "road", "pavedroad"), and the OSM Roads Land Cover layer,
TerrainConfiguration/osgEarthCatalogs/coverage/layer.OSM.roads.LOD14.online.xml (every OSM highway way drawn
11-12 m wide as coverage value 191 "Roads and other asphalt surfaces", preset Paved-Road, soiltype
BM_PAINT-ASPHALT, which landCoverDataSurfChar.map sends to pavedroad). Its claim H1: the extra tag in the
fragmented sectors is a pavedroad stripe painted along OSM highway ways, so the tag count per sector is
1 (default) + (road feature volume present) + (water present) + (paved stripe present).

Prediction (HIGH, brief N3): every sector whose tag count exceeds that baseline contains an OSM highway way.
FALSIFIER: extra-tag sectors that contain no highway way.

## 2. Method

- Tiles: z14 over the AO20 box (x 2875-2885, TMS rows 9867-9877), fetched 2026-09-26 01:09Z from
  http://vr-theworld.com/vr-theworld/mbtiles/osm/ (the land-cover source, osm.features.xml TFSFeatures
  "data:osm-all-features", 121 of 121 tiles served) and .../mbtiles/osm-highways/ (the MAK_ROAD feature-volume
  source, osm.roads.model.xml "data:vehicle-roads-linear"; 102 served, 19 x 404 = empty). The server uses TMS
  rows (y counted from the south): at x 2880, XYZ row 6511 returns 404 and TMS row 9872 returns 200.
- Decoding: a stdlib Mapbox-vector-tile decoder. The land-cover class per way is a transcription of
  selectStyle() in layer.OSM.roads.LOD14.online.xml (tunnels and point features dropped as there); a road
  volume is a drivable line way per the vehicle-roads-linear filter. Each way is sampled every ~3 m with a
  +/-6 m stroke buffer (the vendor strokes are 3-12 m wide).
- Sector frame: the generator's "Sector (i,j): xMin .. yMax" rows are inclusive 43 m cell indices in the area's
  local ENU frame. Its origin (the runtime "offset") is only in the .navRuntimeConfig, which this run did not
  write. The tool builds ENU about the centroid of the log's "Adjusted -" corners and shifts it east until the
  corners land on the log's own "CalculateTransitionPointLocations extent" (-10019..10062, -9976..9976). The
  fitted shift is +21.48 m (half a cell); corner residuals after it are <= 0.05 m. The shift is fitted, so the
  offset is assumed; half a cell is 5 % of a 473 m sector.

## 3. Result

What OSM holds over the box (land-cover tileset, ways by highway / surface): track 280 + track/dirt 30, path
162, service 24 + dirt/gravel 18, unclassified 9 + gravel/unpaved 16, residential 6. No motorway, trunk, primary
or secondary. By the vendor's own style most of this paints 119 (dirt); a 191 (paved) stroke touches 111
sectors.

| tags in the sector | sectors with no highway way | sectors with a highway way |
|---|---|---|
| 1 | 650 | 400 (357 of them also crossed by a drivable road volume) |
| 2 | 70 | 72 |
| 3 | 258 | 142 |
| 4 | 2 | 6 |

- 330 sectors with two or more tags contain no highway way of any kind; 258 of them carry three tags.
- The 1-35 destack start, sector (13,32), carries 3 tags and no way within 6 m of it. The P11 freeze point's
  sector (9,32) carries 2 tags and no way.
- "Has a highway way" and "tags >= 2" agree on 54.4 % of sectors. The same test with the map mirrored E-W gives
  54.5 %, N-S 51.0 %, both 46.9 %, transposed 46.5 %: no association.
- On the ASCII map (osm_sector_map.py --map) the 3-tag sectors form one contiguous areal patch over the
  northern and north-western rows; the way-bearing sectors are mostly 1-tag sectors in the centre and south.

## 4. Verdict

The falsifier fired. The extra nav tags on this area are not road stripes: 330 extra-tag sectors contain no
highway way, and paved strokes touch 111 sectors against 408 sectors with three or four tags.

Design implication, stated separately: a profile that drops "pavedroad" from soil-types-to-tag-with-surface-char
is not supported by this evidence as the fix for the fragmentation; the control run proposed on it (brief N3
Part B) stays parked.

## 5. Two corrections to laneQ_navinput_research.md (recorded here; the scratch note is the seat's)

(a) Road stripes refuted as the source of the extra tag - section 4 above.
(b) The counted tag is soil-derived only. 357 sectors crossed by a drivable MAK_ROAD volume carry exactly one
tag, so a road FEATURE volume adds no tag to the "Generated N distinct nav tags." count. That fits the log's
order: the count is printed inside ProduceSectorInputs, before "PerformIntegrationOfTagvolumes() BEGIN". The
baseline "1 + road volume + water" was therefore wrong; the baseline is 1 (the default tag) plus one per
soil-type tag present.

## 6. Adversarial checks

1. Mapping error (the strongest objection). Against it: the frame reproduces the log's extent at all four
   corners to <= 0.05 m; the TMS convention is fixed by HTTP status; the two independent tilesets land on the
   same sectors (532 sectors crossed by both); no mirror or transpose improves the association. The fitted
   half-cell shift cannot move a sector-scale areal patch.
2. Tile date. The tiles are one day newer than the run; a day of OSM edits cannot create 258 road-free
   3-tag sectors.
3. Baseline dependence. The falsifier counts sectors with NO way at all, so it holds whatever the baseline.
4. Not checked visually: a Navigation Lab view of sector (13,32) (UG52 66.6 p1287-1289) needs a GUI session.

---

## 7. NEXT CHECK - which land-cover class covers the 3-tag region (PREREG, written 2026-09-26 before any
##    land-cover data was opened; only the vendor mapping FILES had been read)

VENDOR CITATION: UG52 23.5 p505 and 66.3.1 p1282 ("Only soil types included in the list will generate
navigation tags specific to that soil type. Other soil types receive the default navigation tag."); UG52
Table 26 p506 (the soil-type names: no soil type is named "road"); navigationProfiles.mtl:121-125 and
:261-264; biomes.landcover.coverage.online.xml (layer order Copernicus 100 m < NLCD 30 m < CA-FVEG 15 m < ...
< OSM roads < OSM water, "Add layers in order of resolution from lowest to highest");
coverage/presets.xml; the four layer files; appData\settings\vrfSim\landCoverDataSurfChar.map (the
BM_* -> soil map).

OWN-RECORD CITATION: this finding secs 3-5; PREREG_NAVCONTROL_WEST20_2026-09-15.md:583-602 (484 sectors changed
tag count between 2026-09-07 and 2026-09-15; the tag-volume totals were identical, so the change is in the
per-sector surface classification).

What the mapping files already say (read, not measured): along the static chain the ONLY route to
"pavedroad" is BM_PAINT-ASPHALT / BM_PAINT-CONCRETE / CDB_PavedRoad, and the only reachable producer of those
in this box is the OSM 191 stroke (preset Paved-Road) - which section 3 shows touches 111 sectors. "road" names
no soil type in Table 26. Four soiltype strings the land-cover layers emit - BM_VEGETATION,
BM_VEGETATION-BRUSH, BM_VEGETATION-MARSH, BM_VEGETATION-MOOR - have NO Match line in
landCoverDataSurfChar.map; what the generator does with an unmatched material is not documented in the files
read. MAK Earth (online).surfChar.map (the texture-name map) contains "Match SatelliteImage pavedRoad".

Method (fixed before looking): fetch the global-geodetic PNG TMS tiles of Copernicus (188, level 10), NLCD
(165, level 11) and CA-FVEG (154, level 11) - their stated data levels - over the box; take each pixel's
EFFECTIVE class as the highest layer whose value has a mapping line (unmapped values, e.g. the commented-out
water values, fall through) [A: osgEarth compositing]; chain class -> preset -> soiltype -> soil; per sector
record the set of classes and soils present (>= 1 pixel centre inside the sector). Score each candidate against
two binaries, B3 = (tags >= 3) and B2 = (tags >= 2). Base rates to beat: predicting "never" scores 74.5 % on B3
and 65.6 % on B2. Search space, fixed now: every single class of each layer and of the effective composite,
every union of two, and "any class whose soil is unmatched". Because this is a search, a best score is reported
with the number of candidates tried.

| # | Prediction | Confidence | What counts as a MISS |
|---|---|---|---|
| Q1 | The seat's: one class or a union of two reproduces B3 AND B2 at > 90 % of sectors, and its soil chain ends at road or pavedroad | seat MEDIUM; mine LOW (sec 7 paragraph 3) | no candidate reaches > 90 % on both; or one does but its soil is neither road nor pavedroad |
| Q2 | Mine: if a class reproduces the map, its soiltype is one landCoverDataSurfChar.map does NOT match (BM_VEGETATION*), i.e. the extra tag rides on an unmatched material rather than on a mapped road soil | LOW-MEDIUM | the reproducing class has a matched soiltype |
| Q3 | The 2-tag sectors differ from the 3-tag ones by exactly one soil class present | LOW (exploratory) | no single-class difference separates them |

FALSIFIER (both Q1 and Q2): no class combination reproduces B3 and B2 at > 90 % -> the extra tag is not a
function of land-cover class presence at sector scale; report which inputs remain (elevation layers, the
texture surfChar map, the tag volumes, the generator's own processing).

## 8. RESULT of the next check (written 2026-09-26 after the land-cover data were read)

Instrument: tools/navdata/landcover_sector_map.py (this commit; the sec-7 prereg was committed first, 405a25d).
Tiles fetched 2026-09-26 ~01:20Z with curl (the server answers 403 to Python's default user agent and 200 to
curl): CA-FVEG (154) at levels 11 and 12 (level 13: 404), NLCD (165) at 11 and 12, Copernicus (188) at 10 (11:
404). Every pixel is R = G = B with alpha 255. Primary run at CA-FVEG 12 / NLCD 12 / Copernicus 10; the
level-11 rerun moves no row of the tables below by more than 3 sectors.

What covers the box. CA-FVEG has a mapped value at every sampled point, so it is the effective layer
everywhere; NLCD (98 % "52 Shrub/Scrub") and Copernicus never show through. Effective classes by sectors
touched: 60 Desert Scrub (BM_SAND -> sand) 1,493; 64 Desert Succulent Shrub (BM_LAND -> dryground) 394;
61 Desert Wash (BM_LAND -> dryground) 179; 30 Sagebrush (BM_SAND -> sand) 105; 9 Barren (BM_LAND ->
dryground) 49; 41 Alkali Desert Scrub (BM_LAND) 21; 12 Urban (BM_LAND -> dryground) 18; 55 Pinyon-Juniper
(BM_VEGETATION -> forest) 15; 11 Annual Grassland (BM_LAND-GRASS -> grass) 14; 62 Joshua Tree (BM_LAND) 11.
Chains: layer.CA-FVEG.15m.online.xml mapping lines; landCoverDataSurfChar.map:246 (BM_LAND dryground), :251
(BM_LAND-GRASS grass), :343 (BM_VEGETATION forest). No class in the box reaches road or pavedroad.

Soil set present in a sector -> its tag-count histogram:

| soils present | tags 1 | tags 2 | tags 3 | tags 4 |
|---|---|---|---|---|
| sand only | 1,046 | 6 | 4 | 0 |
| dryground + sand | 0 | 47 | 377 | 0 |
| dryground only | 4 | 83 | 4 | 0 |
| dryground + grass + sand | 0 | 0 | 8 | 0 |
| dryground + forest + sand | 0 | 0 | 0 | 8 |
| forest + sand | 0 | 0 | 7 | 0 |
| grass + sand | 0 | 6 | 0 | 0 |

- "A dryground-soil class is present" matches tags >= 2 on 98.3 % of sectors and tags >= 3 on 90.9 % (base
  rates 65.6 % and 74.5 %). This candidate was formed AFTER seeing the class table (it groups the BM_LAND
  classes by their soil); the best candidate inside the registered search (87 candidates: single classes and
  pairs) is the pair 61 Desert Wash | 64 Desert Succulent Shrub, 95.1 % on tags >= 2 and 92.6 % on tags >= 3.
- Two classes with the SAME soil do not add a tag: sectors holding only sand classes but two of them (30 + 60)
  read 1 tag in 48 of 50; dryground-only sectors with two or more dryground classes read 2 tags in 37 of 39.
- The descriptive rule tags = [sand present] + 2 x [dryground present] fits 94.7 % of sectors exactly (94.5 % at
  level 11). It is post hoc and is not offered as the mechanism. (This bullet and the previous one come from
  the session-scratch analysis n4_count.py over the same tiles, whose sampling box differs from the tool by a
  few metres; the tool reproduces its tables to within 2 sectors per cell.)

Scoring.

| # | Verdict | Measured |
|---|---|---|
| Q1 (seat MEDIUM, mine LOW) | MISS as registered | the presence limb holds (the 61/64 pair reaches > 90 % on both binaries), but its chain ends at dryground (BM_LAND, landCoverDataSurfChar.map:246), which is neither road nor pavedroad |
| Q2 (mine, LOW-MEDIUM) | MISS | the reproducing classes have a MATCHED soiltype (BM_LAND -> dryground) |
| Q3 (LOW, exploratory) | HIT for the bulk | 3-tag sectors are dryground + sand (377 of 400); 2-tag sectors are mostly dryground only (83 of 142): the difference is the presence of the sand soil (classes 60 / 30). Exceptions: 47 dryground + sand sectors read 2, 6 grass + sand sectors read 2 |
| FALSIFIER | did NOT fire | land-cover presence at sector scale does reproduce the tag map above 90 % on both binaries |

CORRECTION to the sec 7 prereg text (found by the instrument, not by a re-read; the prereg text is left as
written): BM_VEGETATION and BM_VEGETATION-BRUSH are NOT unmatched - landCoverDataSurfChar.map:343-344 send
both to forest. My earlier listing hid them with a filter on the word "forest". BM_VEGETATION-MARSH and
BM_VEGETATION-MOOR do have no Match line, and neither occurs in this box.

What this measures. On this area the extra nav tags follow the CA-FVEG classes whose soil is dryground (Desert
Succulent Shrub, Desert Wash, Barren, Alkali Desert Scrub, Urban, Joshua Tree), and the third tag follows
their co-occurrence with the sand classes (Desert Scrub, Sagebrush). The 3-tag patch of sec 3 is where
Desert Succulent Shrub and Desert Wash meet Desert Scrub.

UNEXPLAINED, carried (both contradict the documented chain, so the tag is not yet explained - only located):
1. dryground, sand, grass and forest are NOT in soil-types-to-tag-with-surface-char (navigationProfiles.mtl
   :261-264), and UG52 p505 / p1282 say "Other soil types receive the default navigation tag". Yet the tag count
   rises with exactly those soils.
2. pavedroad IS in that list, and the OSM 191 strokes map to it (Paved-Road -> BM_PAINT-ASPHALT ->
   pavedroad), yet 98 sectors crossed by a 191 stroke read 1 tag (sec 3 data). The stroke is 11-12 m wide
   against 43 m cells and a 0.2 raster precision; whether the generator samples land cover finely enough to see
   it is not documented in the files read.
What remains as inputs for the tag itself: the generator's own land-cover sampling (resolution, which layers it
composites), the texture-name map MAK Earth (online).surfChar.map, and the preset attributes (dense / lush /
rugged / traits) - the 61 and 64 classes carry identical attributes (0.6 / 0.2 / 0.18) and the two sand
classes differ (0.3 / 0.2 / 0.18 vs 0.7 / 0.1 / 0.25) without adding a tag, which argues against the
attributes but does not exclude them.

Design implication, stated separately: a profile that drops pavedroad and road from the tag list is not
expected to remove these tags, because the soils that carry them are not on that list. The two levers the
measurement points at - a custom Coverage block without the CA-FVEG layer (the vendor's own note in
biomes.landcover.coverage.online.xml:1-15 recommends custom blocks), or a terrain-specific land-cover map
that sends the BM_LAND classes to sand (Release Notes VRF-7074) - are candidates for a new prereg, not
recommendations; each changes the simulated soil as well as the nav tags.

Open for the 2026-09-07 -> 2026-09-13 change (not tested here): the CA-FVEG layer is the only one of the three
that is cached (cacheid="CA_FVEG_WHR_15m"; NLCD and Copernicus are cache_policy no_cache). If CA-FVEG was not
served or not composited on 2026-09-07, the effective layer would have been NLCD 52 (-> forest) and the
dryground / sand mixture would not have existed. The 09-07 per-sector data are lost, so only the histogram
(1:1,136 2:447 3:17 4:0, WEST20:585-590) can be tested against that scenario.
