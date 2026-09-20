# Route pre-flight (`leg_check.py`)

Walks every leg of a C2SIM order against the SAME terrain VR-Forces streams and the performing
unit's own vehicle limits, and flags the legs a tracked vehicle probably cannot traverse - a
pre-flight ESTIMATE off terrain tiles, never a vendor verdict. Nothing here launches, joins or
touches VR-Forces; it reads the order, the initialization, the vendor data files and the public
terrain tiles.

Why it exists: the interface authors ground routes from the terrain height at the ROUTE
VERTICES only - 4-5 km apart - and never sees what lies between them. In P11/G2/G3/G5 two
battalions drove into 55 m of 0.86 rise-over-run on sand and stopped there for the rest of the
run while the vendor reported the task `TaskRunning` for ever, with no failure and no arrival
(`docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md` sec 7). DEMO_READINESS row 20.

## Quick start

    python tools/preflight/leg_check.py --selftest      # tile math + vendor chain
    python tools/preflight/leg_check.py --calibrate --verify-run runs/20260907T150643Z_run
    python tools/preflight/leg_check.py --calibrate --verify-run RUNDIR --sensitivity
    python tools/preflight/leg_check.py --text          # every leg of every task
    python tools/preflight/leg_check.py --text --json out.json \
        --c2sim-observations obs.xml --vrf-overlay overlay.json

Needs Python 3, Pillow, and `curl` on PATH (python's urllib gets 403 from vr-theworld.com).
Tiles are cached under `tools/preflight/preflight_cache/` unless `--cache DIR` or the
`PREFLIGHT_CACHE` environment variable says otherwise; the cache is not tracked. First run over
the whole order fetches a few hundred tiles; later runs are offline-fast (`--offline` forbids
fetching entirely).

## What it computes

For each task (`ManeuverWarfareTask`) it builds the route the interface would build, then for
each leg samples the straight line every 8 m (`--step`) and reports:

| metric | meaning |
|---|---|
| `sustained` | the largest MEAN UPHILL grade over any sliding 40 m window (`--window`) - the measured discriminator |
| `short` | the same over 20 m (`--short-window`) - reported, never the verdict |
| `limit` | the performing unit's own limit on that ground: min `max-slope` over its vehicles x the soil's acceleration-factor |
| `ratio` | `sustained / limit` - the number the threshold applies to |
| worst window | lat/lon, metres along the leg, terrain height, soil class, surface characteristic and source |
| `climb_m`, `descend_m` | total ascent and descent along the leg |
| `nan_samples` | elevation samples that fell on a missing tile |

A leg is FLAGGED when `ratio >= --threshold` (default 0.92) and the leg is longer than one
window. The verdict deliberately rests on SUSTAINED extent, not on a single steep window:
FINDING sec 7 measured every leader surmounting short pitches well above its limit (1-35's own
leader up to 0.909/0.904/0.863 over 5/10/20 m) while no leader ever surmounted a 55 m window
above 0.714.

Two legs never get a verdict. A route segment under 1 m is not a leg - it is a chained start
sitting on the task's own first vertex, nine of them across COA-STP1 - and is skipped and
counted. A leg with more than 1 % of its elevation samples on a missing tile prints
`NO VERDICT - tiles missing` and is neither flagged nor passed.

Two findings are NOT verdicts about grade and are reported whatever the ratio says:

- **NO ELEVATION AT ANY LEVEL.** A leg whose whole cascade (`--elev-level` down to
  `--elev-min-level`) found no tile is printed in full and counted separately. It used to be
  invisible: with the level pinned at 13, every leg over an AO served at 12 was NaN, read as
  "tiles missing", flagged nothing, and let the default-ON lateral route shift do nothing at all
  while reporting no problem.
- **WATER ON THE LINE.** Deep water is acceleration-factor 0.000 in `ground-tracked.sysdef` - a
  dead stop the vendor reports as `TaskRunning` for ever. Any sample on a water soil is a finding
  and an ObservationReport, even when the water lies outside the worst window and the leg is
  therefore not flagged. Nothing is refused or altered by it.

`--level-sensitivity` scores the same legs at each level of the cascade and prints the table.
The 0.92 threshold was calibrated at L13; a coarser DEM can only AVERAGE relief away, so the bias
is one-sided - a real face reads LOWER and a flag can be MISSED, never invented. Measured on the
26 COA-STP1 first legs, L13 -> L12 moved every ratio DOWN (mean -0.018, worst -0.055) and changed
no flag, but it ate most of the calibration margin on the marginal legs (0.966 -> 0.927).

## Data sources

| what | where |
|---|---|
| elevation | VR-TheWorld TMS dataset **149**, 257x257 float32 GeoTIFF, EPSG:4326, bilinear. This is the MAK Earth elevation layer the sim streams (`elevation.worldwide.online.xml:24-35`). The LEVEL IS NOT A CONSTANT (STP-802): `--elev-level` starts at **13** - the deepest level served over the Mojave AO, posting 7.86 m E-W x 9.55 m N-S at 34.66 N - and falls back one level at a time to `--elev-min-level` (**11**) wherever the server has no tile, remembering what worked per area. FINDING sec 7 validated L13 against the sim's own reported altitudes: median residual +0.03 m over 129 samples. |
| land cover | VR-TheWorld TMS **59** (CLCplus 10 m, L14 - EUROPE only), **154** (CA FVEG 15 m, L12), **165** (NLCD 30 m, L12), **188** (Copernicus 100 m, L10) - highest-resolution-with-data wins; class value 0 or a missing tile means "no data here". The levels are the deepest the server actually serves (probed 2026-09-13; CLCplus 2026-09-20). |
| class -> soiltype | `<SharedData>/TerrainData/TerrainConfiguration/osgEarthCatalogs/coverage/layer.*.online.xml` (+ `presets.xml`). Commented-out rows are ignored, which is why Copernicus water resolves to no soil: its class 80 row is commented out. CLCplus class 100 (`preset="Water"`) is live, so over Europe water DOES resolve - to `deep-water`, acceleration-factor 0.000. |
| soiltype -> surface characteristic | `<VRF>/appData/settings/vrfSim/landCoverDataSurfChar.map` |
| soil -> acceleration-factor | `<VRF>/data/simulationModelSets/EntityLevel/vrfSim/systems/movement/ground-tracked.sysdef`, `soil-factors/soil-list` (sand 0.80, rocks 0.80, hard-packed 0.98, paved-road 1.00, muck 0.40, ...) |
| unit -> VRF template | `data/unit-type-map-52.json`, looked up exactly as `UnitTypeMap.Lookup` does (functionId = SIDC 5-10 trailing `-` trimmed; SIDC echelon char; then EchelonCode; then the echelon-only and catch-all rows). `--calibrate` defaults instead to `data/unit-type-map-52-nolifeform.json`, the probe map every COA-STP1 run since 2026-09-06 actually used; `--typemap FILE` overrides either way |
| template -> vehicles -> `max-slope` | the vendor `.entity` files: `<subordinates>` resolved recursively by objectType (8-field VRF types have their superType stripped, `matchType` wildcards honoured), `<real paramName="max-slope">` read with `parentFile` inheritance (M3A2_Bradley_CFV inherits M2A2_Bradley_IFV). The unit's limit is the MIN over its leaves. |

`--vrf-home` and `--shared-data` override the `C:\MAK` locations (or set `VRF_HOME` /
`MAK_SHARED_DATA`).

## Start positions

The interface SPREADS co-located units onto 700 m rings at init (DeStack), so a unit's authored
position is NOT where it starts, and the first leg is the one that matters. `--starts FILE`
takes a `unit,lat,lon` CSV of real start positions; `starts_P11.csv` ships with the tool and is
the default. **It is an AO-SPECIFIC default: ten MOJAVE positions.** A start further than
`--starts-max-km` (100, the interface's own `Vrf:MaxVertexFromTaskeeKm`) from every vertex of its
own unit's tasks is REFUSED by name, and the authored initialization position is used instead; if
that refuses every row of the DEFAULT file the tool STOPS rather than guess (pass `--no-starts`,
your own `--starts`, or `--starts-from-run`). It was derived with

    python tools/preflight/leg_check.py --starts-from-run runs/20260907T150643Z_run \
        --write-starts tools/preflight/starts_P11.csv

which reads the member map from `vrfc2simapp.log` ("members of &lt;unit&gt;: &lt;name&gt;
[VRF_UUID:...]") and takes the FIRST `POS` row of each unit's FORMATION LEADER (the first member
listed) from `watchvrf-trace.csv`. `--no-starts` falls back to the authored positions.

The interface's own start rule is replicated exactly (`VrfC2SimService.cs:1888-1945`): the route
begins at the unit's live position, and leading vertices within `Vrf:DropOriginVertexMeters`
(100 m, `--drop-origin-meters`) of the unit's AUTHORED position are dropped once the unit has
been spread further than that from it - never all of them. A unit's SECOND and later tasks start
at the end of its previous task's route, because the interface sequences a unit's tasks in
declared order.

## Calibration - where the 40 m window and the 0.92 threshold come from

`--calibrate --verify-run RUNDIR` runs the FIRST leg of each COA-STP1 unit's FIRST task from its
P11 start position and puts it beside what that unit actually did in that run.

The label is DERIVED from the run's own `watchvrf-trace.csv`, never from net displacement: the
formation leader's track is projected onto leg 1, and the unit is

- **FROZE** when its along-leg advance is under 20 m over the final 600 s of the trace AND it
  never came within 200 m of the leg's far vertex;
- **MOVED / arrived** when it did reach that vertex. Five of the nine P11 units drove THROUGH
  vertex 1 and ended 15-20 km past it, which is why the test is closest approach and not end
  distance (both columns are printed);
- **MOVED / crawling** when it reached neither, but was still advancing by less than 300 m over
  that final 600 s.

Net displacement over a whole trace cannot see a unit that drives far and then stops. 4-27/2/1_A
did exactly that - 22.7 km, then nothing for the last 2,870 s - and the stored net label had it
as MOVED.

| unit | task | leg m | sust 40 m | 20 m | soil at worst | limit | ratio | P11 (derived) | along m | adv 600 s | min dist to vertex | end dist |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1-35/2/1_A | T1_AOA_SE_1-35_AR | 6,593 | 0.826 | 0.912 | sand | 0.752 | **1.098** | FROZE | 1,968 | -2 | 4,601 | 4,625 |
| 1-6/2/1_AD | T15_AOA_SE_1-6_IN | 8,783 | 0.744 | 0.836 | sand | 0.752 | **0.990** | FROZE | 2,837 | +2 | 5,944 | 5,945 |
| 4-27/2/1_A | T5_ConductCounter-Fire | 32,269 | 0.727 | 0.823 | sand | 0.752 | **0.966** | FROZE | 22,697 | +1 | 9,557 | 9,568 |
| 1-1/2/1_AD | T23_AOA_SE_1-1_RECON | 6,168 | 0.654 | 0.696 | sand | 0.752 | 0.870 | MOVED arrived | 25,587 | 0 | 25 | 20,099 |
| 856/HHC | T27_SecureMovementCorridors | 24,594 | 0.650 | 0.699 | sand | 0.752 | 0.864 | MOVED crawling | 23,868 | +123 | 724 | 724 |
| 40/2/1_AD | T19_AOA_SE_40_EN | 9,425 | 0.559 | 0.571 | sand | 0.752 | 0.743 | MOVED arrived | 27,423 | +183 | 1 | 18,801 |
| C/1-35 | T39_AOA_SE_C/1-35_AR | 9,323 | 0.545 | 0.629 | sand | 0.752 | 0.725 | MOVED arrived | 24,517 | +96 | 17 | 15,205 |
| B/5-20 | T35_AOA_SE_B/5-20_IN | 9,266 | 0.416 | 0.515 | hard-packed | 0.921 | 0.452 | MOVED arrived | 409 | -5,881 | 25 | 16,550 |
| 5-20/2/1_A | T31_AOA_SE_5-20_IN | 10,155 | 0.404 | 0.458 | hard-packed | 0.921 | 0.438 | MOVED arrived | 14,514 | -2,271 | 1 | 5,978 |

(A/6-56/HHC's first task carries no location at all - DEMO_READINESS row 16 - so it has no leg.
A negative `adv 600 s` means the unit was long past vertex 1 and moving off leg 1's heading.)

Frozen legs: ratio 0.966 .. 1.098. Clean movers: 0.438 .. 0.870. **Margin 0.096**, and the
default threshold **0.92** is the midpoint of that gap: three flags, three units that froze,
zero misses and zero false alarms. 856/HHC is excluded from both ends - a crawl is neither a
clean pass nor a stop - and at 0.864 it sits just under the highest clean mover anyway.

Two of the three flags land on the ground the unit stopped on: T1's worst window is centred 38 m
past 1-35's P11 stop (18 m past the window's near edge; its last fix is 47 m from the G3 freeze
at 34.65608/-116.76142), and 4-27's P11 stop at 22,697 m lies INSIDE T5's worst window (centred
22,702 m). 1-6 is a true positive on the leg but not on the face: in P11 it stopped 482 m short
of the flagged window, on ground descending at -0.13.

**The 40 m window is an operating point, not a constant.** `--sensitivity` re-runs the same nine
legs over windows 40/55/80 m, steps 4/8/16 m, and nearest vs bilinear elevation sampling.
Margin = lowest frozen ratio - highest mover ratio; each cell is EXCLUDING the crawling unit /
COUNTING it as a mover:

| interp | step | w=40 | w=55 | w=80 |
|---|---|---|---|---|
| bilinear | 4 m | +0.091 / +0.091 | +0.023 / +0.023 | +0.043 / +0.043 |
| bilinear | 8 m | **+0.097 / +0.097** | +0.025 / +0.025 | +0.040 / +0.040 |
| bilinear | 16 m | +0.139 / +0.139 | +0.041 / +0.041 | +0.033 / +0.033 |
| nearest | 4 m | +0.110 / +0.077 | +0.076 / +0.076 | +0.043 / +0.008 |
| nearest | 8 m | +0.109 / +0.012 | +0.003 / **-0.008** | +0.032 / **-0.003** |
| nearest | 16 m | +0.101 / +0.056 | +0.072 / +0.024 | +0.033 / +0.033 |

At an 80 m window the separation DIES (margin -0.003, nearest sampling at 8 m steps), and 55 m
goes to -0.008 in the same cell. Only 40 m holds its margin across all six sampling variants.

Read the margin for what it is: nine legs, one order, one terrain, three positives, and a metric
whose own sampling choices can erase it. It is enough to justify a WARNING channel (the map
graphic and the VR-Forces overlay), and DEMO_READINESS row 20 already rules that `TASKABRT`
waits until the calibration shows zero false alarms on more runs than this. A second stop
mechanism is active in these runs and this metric does not see it: two of the four P11 leg-1
stops are on ground it scores as benign (`docs/experiments/PREFLIGHT_CALIBRATION_2026-09-13.md`
sec 5).

## Outputs

- `--text` - one planner-readable line per flagged leg (`--verbose` also prints the passing ones).
  A flag reads `PREDICTED IMPASSABLE (pre-flight estimate, ratio X vs threshold Y)`; the tool
  never asserts that the vehicles cannot traverse the ground, because it is an estimate off
  terrain tiles and not a vendor verdict.
- `--json FILE` - every leg with all its metrics, the worst-window location and segment, the soil
  with its soiltype, surface characteristic and source, whether the soil hop was assumed, and the
  count and fraction of NaN elevation samples with the `no_verdict` flag those raise.
- `--c2sim-observations FILE` - EMITTER SKELETON. One `ReportBody` per flagged leg; element names
  and order mirror the SDK's generated classes (`C2SIM_SMX_LOX_V1.0.1.cs`) and a real captured
  report (`runs/20260902T193508Z_run/reports-captured.log`). C2SIM 1.0.2 has no observation type
  that carries a location AND free text, so each flagged leg emits a `LocationObservation` for
  WHERE plus a `NameObservation` whose `Marking` carries the numbers and the same
  `PREDICTED IMPASSABLE (pre-flight estimate, ratio X vs threshold Y)` wording; the TODOs in the
  emitter say so rather than inventing a field. The file wraps the reports in a non-schema
  container so it can hold several - send them one at a time.
- `--vrf-overlay FILE` - EMITTER SKELETON. `{label, color, points}` entries for a later C#
  consumer to draw through the remote controller's overlay-object API: the worst window in red,
  the whole leg in amber.

## Assumptions, marked

1. **The soil hop is assumed except sand.** `DtSoilType` (`geometry/surface.h:19-44`, the
   right-hand column of `landCoverDataSurfChar.map`) -> `DtRoughnessSoilType`
   (`surface.h:75-88`, the names in the sysdef's `soil-list`) is implemented in
   `DtMapSurfaceToRoughness::operator()`, which ships only in the geometry DLL - the header
   declares it and nothing more. The table in the tool is a documented guess for 26 of its 28
   rows; the `sand` row is the one FINDING sec 7 confirms end to end for this AO, and it is the
   row every calibration flag rests on. Seven of the fourteen flags over the whole order rest on
   `forest -> hard-packed` instead; re-run with that hop changed and all seven stay flagged
   (paved-road 1.00: 0.969-1.289; sand 0.80: 1.211-1.611), so the guess cannot add or remove a
   flag here. `--selftest` lists the assumed rows; `--json` marks each sample.
2. **The type map is the one the runs used.** The fidelity table points several COA-STP1 rows at
   DI-Guy lifeform templates, and the headless sim crashes at the first DI-Guy human while the
   DI-Guy data package is absent - so every run since 2026-09-06, P11 included, ran
   `unit-type-map-52-nolifeform.json`. That file now lives in `data/` and `--calibrate` reads it
   by default, so the calibration checks the templates the sim really created. 25 rows differ
   from `data/unit-type-map-52.json` and all resolve to min `max-slope` 0.94, so no ratio
   changes. The tool's own proxy - any template whose minimum `max-slope` is >= 1.2 is replaced
   by Tank Platoon (USA)/(RUS) - is now a FALLBACK for maps that still carry lifeform rows. The
   test is unambiguous: no vendor GROUND VEHICLE exceeds 1.0 and every lifeform is 1.5 or 1.57.
   `--allow-lifeforms` turns the fallback off once the DI-Guy package is installed.
3. **Vehicle limits are resolved, not tabulated.** The templates resolve to real vehicle lists
   (Tank Headquarters Section (USA) -> 2x M1A2 0.94, M3A2 CFV 0.94, 2x HMMWV 1.0, M577A2 1.0 ->
   min 0.94), matching the members P11 actually created. The hard-coded fallback (0.94) is used
   only when the vendor SMS is unreachable, and it says so in the output.
4. **The limit itself is an analogy, not a cited dynamics gate.** `max-slope x
   acceleration-factor` (0.94 x 0.80 = 0.752 for an M1A2 on sand) comes from
   `navigationPreferenceDescriptor.h:111-132`, a nav-mesh PATH-COST formula, and FINDING sec 7
   records that the dynamics-side warrant is qualitative (UG52 23.5.2 p508;
   `movingObjectParameters.h:294-310`). What the calibration shows is that this number, with the
   sustained-window metric, separates the observed stops from the observed passes - not that the
   vendor evaluates it.
5. **Straight lines.** Each leg is sampled as the straight line between its endpoints, which is
   what the vendor's `ground-vehicle-move-to` drives where no navigation mesh covers the leg
   (P11 had none). Inside a nav area the planner may route around a face and a flag may be a
   false alarm; the pre-flight is a warning, not a refusal.
6. **First legs only have ground truth.** Every leg of the order is checked, but only the first
   leg of each unit's first task can be checked against a run.
7. **A flag is not a stop prediction.** Two of the four leg-1 stops in P11 happened on ground
   this metric scores as benign, so a second stop mechanism (the formation / scale-crawl stop -
   DESIGN_ORBAT C1b, the S4 crawl of 2026-09-06) is active in these runs and the pre-flight does
   not see it. The warning says "this leg crosses a sustained face your vehicles probably cannot
   climb", never "this is why your unit will stop".

## Gates

- `--selftest`: the three `lcctl.py` land-cover controls (Pacific / Los Angeles / Lake Tahoe)
  against the raw class value each tileset returns, the 1-35 freeze point through the whole soil
  chain (CA FVEG 30 "Sagebrush" -> BM_SAND -> sand -> 0.80), the elevation at
  34.65607/-116.76144 (1585.61 m against the expected ~1585.6), the posting size, the vendor
  chain's five template resolutions, and a list of the assumed soil rows.
- `--calibrate --verify-run runs/20260907T150643Z_run`: must report `misses ... none` and
  `false alarms ... none` at the default window and threshold, and a positive margin.
- `--text` over the whole order: 48 legs across 42 tasks, 9 skipped as under 1 m, 0 without a
  verdict, 14 flagged.
- ASCII: `rg -nP "[^\x09\x0a\x0d\x20-\x7E]" tools/preflight/*` must exit 1.
- Line endings: CRLF in every tracked file here.
