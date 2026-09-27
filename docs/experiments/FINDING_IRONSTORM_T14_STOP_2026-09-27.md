# FINDING: why 48 IBCT (T14) stopped 1.6 km short in the cut-A pre-warm (2026-09-27)

Tier: HEAVY (a cause claim). Lane T14, session 5fc25950. Offline: no sim, no launch, nothing written under C:\MAK,
vendor logs count-grep only. [V] = verified against the cited primary source; [A] = assumed or inferred.
Inputs: runs/20260927T003120Z_run (the IRONSTORM_CUTA_LIVE-2026-09-27-1 pre-warm), cut-A order sha256 3801c71b...79fefe8.

## 1. What happened [V]

- 48 IBCT did NOT "never move". WatchVrf POS rows of VRF_UUID:247147e3 (the M577A2 the app bound to 48 IBCT): at the
  start 54.019389, 23.313902 until dispatch, then ~130 m per 2 wall s along the leg, within 4 m of the centreline, to
  850 m along; one last 46 m step turning 32 deg LEFT of the route heading (heading -15.7 deg against +16.1 deg); then
  54.026779, 23.317195 (alt 128.8) for the remaining 54 rows. No other entity came within 400 m of that point.
- The STALL line's "(max 0.0 m)" is the 360-SIM-s sliding window of SampleAndJudgeStall (VrfC2SimService.cs:7205-7249,
  measured against the oldest ring sample), not the displacement since dispatch. The PREREG Result is corrected.

## 2. The vendor chain (docs and installed files first) [V]

- UG52 23.3 p505: Move Along Route plans no path and "does avoid obstacles as it moves". UG52 23.5.1 p505-507: soil
  drives mobility; Table 26 maps ocean / deepLake / deepRiver -> deep-water; acceleration-factor 0.0 means "the
  surface's drag prevents the vehicle from moving at all (deep water)". ground-tracked.sysdef:813-815: deep-water
  acceleration-factor 0.000000 (shallow-water 0.70).
- The terrain is `MAK Earth (online).earth` plus California / Hawaii .medf insets (the fixture .mtf's 15 patches); there
  is NO model geometry over Suwalki, so the fixture's surfChar.map (material DetailWater -> Ocean) has nothing to act on
  here. That map is a red herring for this AO.
- Soil comes from the land-cover composite `biomes.landcover.coverage.online.xml`, included by
  `biome.config.online.xml:13` with `vrfsim:enabled="true"`. Its layers, lowest to highest: Copernicus :35, NLCD :38,
  CA-FVEG :41, CLCplus :50, OSM roads :55, **OSM inland water :58**. The earth file also loads the same OSM water as a
  VRFSIM "Lake" feature layer (`osm.features.water.xml`, `mak_vrf_layer` Lake).
- OSM natural=water with no water= tag: selectStyle() default -> coverage value 80 -> preset Water -> BM_WATER
  (`layer.OSM.water.LOD14.online.xml`, `presets.xml:36`) -> deeplake (`landCoverDataSurfChar.map:346`) -> deep-water.

## 3. Verdict per hypothesis

| H | claim | what was checked | verdict |
|---|---|---|---|
| H1 | zero-mobility water surface at the stop | OSM osm-water z14 tile 14_9253_11125: the stop is **0.5 m outside** way 197345448 (natural=water, the lake Jezioro Wiersnie; 567 x 759 m within that tile); along the planned heading from the last moving fix the polygon starts 17 m ahead; the centreline first enters it at 834 m (78 of 1,214 samples at 2 m, `leg_check --osm-water`). CLCplus under the stop is 53 (herbaceous); CLCplus 100 starts 25 m east of the line at 850 m. Chain in sec 2. | **CONFIRMED [V geometry + config; A: the sim's soil query resolves the composite exactly as its XML reads - not observed in a sim query]** |
| H1 (DetailWater path) | the .mtf's material map DetailWater -> Ocean | the .mtf patch list: no geometry over Suwalki | **FALSIFIED as the path [V]**; the water is the OSM coverage layer |
| H1b | a sub-L12 bank beyond max-slope | elevation 149 is served at **L12 only** here (L13-L15 NaN at the stop); the 9 moving POS altitudes equal the L12 bilinear DEM to 0.0-0.1 m, so the sim's own ground IS the L12 surface; steepest 10 m window over 600-1,100 m along = 0.041 (M577A2 max-slope 1.0); the stop is the flat bottom (128.5-128.9 m) | **FALSIFIED [V]** |
| H2 | dynamic obstacle-avoidance deadlock | no entity within 400 m of the stop at any time [V]; trees (CLCplus 31/22) cover 51 of 327 lattice points within 100 m [V]. The final left turn fits avoidance of the water edge as well as of trees | **NOT FALSIFIED as a contributor [A]**; it cannot explain a dead stop on its own without the water |

Tooling gap [V]: `leg_check.py` read CLCplus only, so the (e) nudge was "verified dry at four strides" on a line that
runs through an OSM lake. `--osm-water DIR` (opt-in, unit-tested in `--selftest`) now reads the top layer; on the
pre-(i) order it reports T14 "WATER ON THE LINE - 78 of 1214 sample(s) ... deep-water (OSM water z14)".
Not closed: the C# pre-flight (src/VrfC2SimApp/Preflight/TileSource.cs:60, LegScorer.cs:60) reads the raster layers
only, so the interface's own WATER ON THE LINE warning has the same blind spot. Proposed next step (not done here): port
the OSM water lookup into TileSource/SoilChain behind the same parity pin, once a z14 osm-water fetch is wired there.

## 4. Action taken

Cut-A change (i): one waypoint 54.014600, 23.331500 routes T14 round the lake's south-east side; destination unchanged;
4,169 m; 0 wet samples at 0 / +-25 / +-50 m; maple corridor PASS min 0.9167; order sha256 5d6bbae4...49fc9a
(data/IRONSTORM_CUTA_CHANGES.md (i)). Found on the way, NOT changed: T10's (h) leg 2 grazes OSM water (1 centreline
sample at 54.02359, 23.31049; 81 / 102 samples at 25 / 50 m right of travel) - an owner decision.
Since fixed by cut-A change (j) (a second T10 waypoint 54.024000, 23.313000; data/IRONSTORM_CUTA_CHANGES.md (j)).

## 5. Adversarial review

- Strongest competitor: H2, the vehicle's obstacle avoidance turning it away from something (trees or the water edge)
  into a deadlock, with water incidental. Against it: the stop is 0.5 m from a mapped deep-water polygon on the line
  it was driving, and no other entity was near. What would falsify H1: in the -2 run on the (i) route, 48 IBCT stops
  again on dry ground (>= 50 m from any water of any source) - or, flown on the old leg, it stops somewhere other than
  within ~50 m of 54.02678, 23.31720.
- Unexplained: why the last 46 m step turned 32 deg left before stopping (avoidance of the edge ahead is the reading;
  not verified). Assumed: the OSM tiles fetched 2026-09-26 are the ones the sim streamed on 2026-09-27.
