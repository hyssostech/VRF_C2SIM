# PREFLIGHT CALIBRATION (2026-09-13)

The route pre-flight of DEMO_READINESS row 20, built and calibrated. Tool:
`tools/preflight/leg_check.py` (+ its README, `starts_P11.csv` and
`data/unit-type-map-52-nolifeform.json`). Tier: STANDARD - the tool makes no new cause claim;
it OPERATIONALISES the cause claim already adjudicated in
`docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md` sec 7 (HEAVY, verdict STANDS WITH FIXES)
and is judged here by whether it separates the observed stops from the observed passes.
Nothing in this work launched or touched VR-Forces.

Revised 2026-09-13 after a cold-start review (COMMIT WITH FIXES). The two findings that moved
numbers: the ground truth is now DERIVED from the run trace instead of from net displacement
(which had mislabelled a unit that drove 22.7 km and then froze), and the sustained window is
40 m, not 55 m.

## 1. What was built

Given a C2SIM order and its initialization, the tool builds the route the interface would
build, samples each leg every 8 m against the SAME elevation tiles the sim streams, derates the
performing unit's own vehicle limit by the soil under each sample, and flags legs whose
SUSTAINED climb reaches the limit. Sources, verbatim in the README: elevation TMS 149 L13
(validated to +0.03 m median in FINDING sec 7); land cover TMS 154/165/188 with
highest-resolution-with-data wins; soil chain layer XML -> `landCoverDataSurfChar.map` ->
`ground-tracked.sysdef`; unit -> template from the type map by the app's own lookup rule;
template -> vehicles -> `max-slope` resolved from the vendor `.entity` files.

Four things the tool replicates rather than approximates:

- **the start rule** - route begins at the unit's LIVE position, and leading vertices within
  `Vrf:DropOriginVertexMeters` (100 m) of the AUTHORED position are dropped once the unit has
  been spread further than that (`VrfC2SimService.cs:1888-1945`);
- **the spread** - DeStack moves units onto 700 m rings at init, so `--starts` takes real start
  positions. `starts_P11.csv` is derived from the P11 trace: the member map in
  `vrfc2simapp.log` plus the FIRST `POS` fix of each unit's FORMATION LEADER;
- **the vehicles** - not a hard-coded table. Tank Headquarters Section (USA) resolves to 2x
  M1A2 (0.94), M3A2 CFV (0.94, inherited from M2A2_Bradley_IFV through `parentFile`), 2x HMMWV
  (1.0) and M577A2 (1.0); minimum 0.94. That is exactly the six members P11 created for 1-35;
- **the type map the runs actually used** - `--calibrate` defaults to
  `data/unit-type-map-52-nolifeform.json`. This is the PROBE map every COA-STP1 run since
  2026-09-06 used (DI-Guy data absent); 25 rows differ from `data/unit-type-map-52.json`; all
  resolve to min max-slope 0.94, so the numbers are unaffected, but template names now match
  what the sim created (856/HHC is a Tank Platoon (USA) in the table below because that is what
  P11 built, not because the tool substituted one).

Legs shorter than 1 m are not legs - a chained start that lands on the task's own first vertex
produces nine of them across COA-STP1 - and are skipped and counted separately. Elevation
samples that fall on a missing tile are counted per leg; above 1 % of a leg's samples the leg
gets `NO VERDICT - tiles missing` instead of a pass or a flag (0 legs in this order).

## 2. Calibration - the table the threshold came from

`--calibrate --verify-run runs/20260907T150643Z_run`: the FIRST leg of each unit's FIRST task
(the only task dispatched at order time; later ones are sequenced behind an arrival that never
happened), from the unit's P11 start, against what that unit did in P11.

The label is DERIVED from that run's own `watchvrf-trace.csv`, not from net displacement. The
leader's track is projected onto leg 1, and:

- **FROZE** - along-leg advance < 20 m over the final 600 s of the trace AND the leader never
  came within 200 m of the leg's far vertex;
- **MOVED / arrived** - it did reach that vertex (five units drove THROUGH vertex 1 and were
  15-20 km past it at the end of the trace, which is why the test is closest approach, not
  end distance);
- **MOVED / crawling** - it did not reach the vertex but was still advancing, by less than
  300 m over that final 600 s.

Net displacement over the whole trace cannot see a unit that drives far and then stops: that is
exactly what 4-27/2/1_A did, and it had been labelled MOVED.

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

A/6-56/HHC's first task (T9) carries no location at all, so it has no leg - DEMO_READINESS
row 16, and the same refusal P11 logged. `along m` is the leader's last fix projected onto
leg 1; a NEGATIVE `adv 600 s` means the unit had passed vertex 1 long before and was moving
away from leg 1's heading on a later leg.

**Frozen 0.966 .. 1.098; clean movers 0.438 .. 0.870; margin 0.096** (0.097 as printed from the
unrounded ratios). The default threshold is the midpoint, **0.92**: three flags, three units
that froze, zero misses and zero false alarms on this set. 856/HHC is excluded from both ends -
crawling is neither a clean pass nor a stop - and at 0.864 it sits just under the highest clean
mover anyway.

Where the flagged windows land, per run, without mixing runs in one sentence:

- **T1 / 1-35.** The worst 40 m window is centred 2,006 m along the leg (34.656242/-116.761859).
  In P11 the leader stopped at 1,968 m along, 38 m short of that centre and 18 m short of the
  window's near edge; its last fix is 47 m from the G3 freeze point (34.65608/-116.76142). The
  ground immediately ahead of the P11 stop rises at +0.79 over 40 m. True positive on the face.
- **T5 / 4-27.** Worst window centred 22,702 m along; in P11 the leader stopped at 22,697 m -
  inside the window - and did not move again for the remaining 2,870 s (+/-2 m). True positive
  on the face, and the one the old net-displacement label had hidden.
- **T15 / 1-6.** 1-6's leg is flagged; in G3 the unit froze on the flagged face
  (34.642372/-116.759057, 32 m from the tool's window centre); in P11 it stopped 482 m SHORT of
  it, on ground DESCENDING at -0.13 over the 40 m behind the stop, so for P11 this is a true
  positive on the leg, not on the face.

(FINDING sec 7b quotes 22,723 m, 13 m and 22 m for the same three geometries from the same
trace under an equal-scale projection; this tool uses the R_EARTH tangent plane its own
distances use, and the two disagree by under 30 m in every case.)

Corroboration of the instrument, not of the threshold: the tool's along-heading figure for 1-35
is 0.826 over 40 m where FINDING sec 7 read 0.858 over 55 m off the native posting row through
the same point (different cut, 8 m bilinear sampling against 7.86 m postings), and it
independently reproduces 1-6's number.

### The window is an operating point, not a constant

`--calibrate --verify-run RUNDIR --sensitivity` prints the grid the 40 m default rests on.
Margin = lowest frozen ratio - highest mover ratio; each cell is EXCLUDING the crawling unit /
COUNTING it as a mover. Negative means the metric no longer separates the observed stops from
the observed passes.

| interp | step | w=40 | w=55 | w=80 |
|---|---|---|---|---|
| bilinear | 4 m | +0.091 / +0.091 | +0.023 / +0.023 | +0.043 / +0.043 |
| bilinear | 8 m | **+0.097 / +0.097** | +0.025 / +0.025 | +0.040 / +0.040 |
| bilinear | 16 m | +0.139 / +0.139 | +0.041 / +0.041 | +0.033 / +0.033 |
| nearest | 4 m | +0.110 / +0.077 | +0.076 / +0.076 | +0.043 / +0.008 |
| nearest | 8 m | +0.109 / +0.012 | +0.003 / **-0.008** | +0.032 / **-0.003** |
| nearest | 16 m | +0.101 / +0.056 | +0.072 / +0.024 | +0.033 / +0.033 |

At an 80 m window the separation DIES (-0.003 at nearest/8 m, counting the crawling 856/HHC as
a mover), and at 55 m it is -0.008 in the same cell. 40 m is the only window that holds its
margin across all six sampling variants. Read that as an operating point chosen on one order,
one terrain and three positives - not as a property of the vendor's dynamics.

## 3. All 42 tasks

`leg_check.py --text` over the whole order, with `data/unit-type-map-52.json` (the production
map): **48 legs across 42 tasks** (9 tasks carry no location at all; 9 further route segments
are under 1 m - a chained start sitting on the task's own first vertex - and are skipped),
**14 legs flagged, on 8 units**, 0 legs without a verdict. Only the FIRST leg of each unit's
FIRST task has ground truth; everything below the first three rows of section 2 is an
unverified prediction.

| task | unit | leg m | sust 40 m | limit | ratio | soil (FVEG class) | worst window |
|---|---|---|---|---|---|---|---|
| T11 | A/6-56/HHC | 46,780 | 0.773 | 0.588 | 1.32 | hard-packed (Pinyon-Juniper) | 34.3348/-116.9677, 45.8 km in |
| T1 | 1-35/2/1_A | 6,593 | 0.826 | 0.752 | 1.10 | sand (Sagebrush) | 34.6562/-116.7619, 2.0 km in |
| T18 | 1-6/2/1_AD | 42,547 | 1.002 | 0.921 | 1.09 | hard-packed (Pinyon-Juniper) | 34.3306/-116.8679, 32.8 km in |
| T14 | 510/40 | 11,615 | 0.992 | 0.921 | 1.08 | hard-packed (Pinyon-Juniper) | 34.3341/-116.9734, 10.8 km in |
| T22 | 40/2/1_AD | 11,866 | 0.975 | 0.921 | 1.06 | hard-packed (Pinyon-Juniper) | 34.3340/-116.9735, 11.1 km in |
| T30 | 856/HHC | 11,866 | 0.975 | 0.921 | 1.06 | hard-packed (Pinyon-Juniper) | 34.3340/-116.9735, 11.1 km in |
| T29 | 856/HHC | 23,512 | 0.759 | 0.752 | 1.01 | sand (Desert Scrub) | 34.5654/-116.9799, 8.9 km in |
| T15 | 1-6/2/1_AD | 8,783 | 0.744 | 0.752 | 0.99 | sand (Sagebrush) | 34.6423/-116.7594, 3.3 km in |
| T26 | 1-1/2/1_AD | 42,892 | 0.910 | 0.921 | 0.99 | hard-packed (Montane Hardwood-Conifer) | 34.3273/-116.9324, 39.1 km in |
| T4 | 1-35/2/1_A | 42,892 | 0.910 | 0.921 | 0.99 | hard-packed (Montane Hardwood-Conifer) | 34.3273/-116.9324, 39.1 km in |
| T13 | 510/40 | 35,727 | 0.736 | 0.752 | 0.98 | sand (Sagebrush) | 34.6544/-116.7503, 3.7 km in |
| T5 | 4-27/2/1_A | 32,269 | 0.727 | 0.752 | 0.97 | sand (Desert Scrub) | 34.4789/-116.8193, 22.7 km in |
| T20 | 40/2/1_AD | 15,412 | 0.721 | 0.752 | 0.96 | sand (Desert Scrub) | 34.5193/-116.9933, 5.7 km in |
| T19 leg 3 | 40/2/1_AD | 5,721 | 0.693 | 0.752 | 0.92 | sand (Desert Scrub) | 34.5830/-116.9783, 2.8 km in |

Two of these are new against the 55 m / 0.90 table this document carried before the review: T5
(4-27, the third real freeze) and T19 leg 3.

Two clusters, both readable on the map. The first is the 1-35 / 1-6 / 510-40 ridge at
34.64-34.66 N, -116.75 to -116.76 - the face FINDING sec 7 measured, crossed by three different
units' opening legs. The second is the southern approach to PL BLUE around 34.327-34.335 N,
-116.87 to -116.97, where five units' later legs run into Pinyon-Juniper and Montane
Hardwood-Conifer slopes: on the current chaining assumption those legs start where the previous
task's route ended, which is the interface's declared-order behaviour but was never exercised in
P11 (no unit ever finished its first task).

Governing land cover came from CA FVEG 15 m for all 48 legs - the AO is inside the California
inset, so neither NLCD nor Copernicus was ever needed.

## 4. What is measured and what is assumed

MEASURED: the elevation surface and its agreement with the sim (FINDING sec 7); the land-cover
class at every sample and the vendor chain that turns it into a soil; the vehicles under each
template and their `max-slope` values, read out of the vendor `.entity` files with `parentFile`
inheritance; the P11 start positions, the leader tracks and the derived labels, all recomputed
here from the trace; the separation in section 2.

ASSUMED, in descending order of how much weight it carries:

1. **`DtSoilType` -> `DtRoughnessSoilType` for 26 of 28 rows.** The mapping lives in
   `DtMapSurfaceToRoughness::operator()`, which ships only in the geometry DLL;
   `geometry/mapSurfaceToRoughness.h` declares it and nothing else. `sand` is the one row
   FINDING sec 7 confirms end to end. Every flag in section 2 is a sand row; **7 of the 14 in
   section 3** (Pinyon-Juniper and Montane Hardwood-Conifer -> BM_VEGETATION -> forest ->
   hard-packed 0.98) rest on the guess. It is not load-bearing: re-run with the hop changed, all
   7 stay flagged under the most permissive soil in the vendor's table (paved-road 1.00: ratios
   0.969-1.289) and rise to 1.211-1.611 under sand (0.80). The flagged SET is identical - 14
   legs - under both alternatives, so the hop cannot add or remove a flag here. `--selftest`
   lists the assumed rows and `--json` marks each sample with its soiltype and surface
   characteristic.
2. **The limit formula is an analogy.** `max-slope x acceleration-factor` (0.94 x 0.80 = 0.752
   for an M1A2 on sand) is a nav-mesh PATH-COST derating
   (`navigationPreferenceDescriptor.h:111-132`), not a cited dynamics gate; FINDING sec 7 says
   so and rests its own verdict on the sustained extent rather than on the number. What section
   2 shows is that this number plus the 40 m window separates the observed outcomes.
3. **Straight-line legs.** Sampling assumes the vehicle drives the straight line, which is what
   `ground-vehicle-move-to` does where no navigation mesh covers the leg (P11 had none). Inside
   an area the planner may route around a face, so a flag there is a warning, not a refusal.
4. **The type map.** `--calibrate` reads the probe map every run since 2026-09-06 used;
   `--text` over the order reads the production map. The 25 rows that differ all resolve to min
   max-slope 0.94 either way (the production map's lifeform rows are caught by the tool's own
   >= 1.2 proxy, which is now a FALLBACK for maps that still carry them), so no ratio in this
   document changes between the two - verified by re-running section 2 with each.
5. **Start positions are P11's, and only for P11's ten units.** `starts_P11.csv` is one run's
   spread; a different seed or DeStack radius moves every first leg, so the calibration is valid
   for that run alone. The other COA-STP1 taskees (510/40 among them) fall back to the AUTHORED
   assembly coordinate, where the origin-vertex drop does not fire and the leg geometry is not
   what a run would produce. Their rows in section 3 are weaker than the rest.
6. **The formation LEADER stands for the unit.** Starts, tracks and labels use the first member
   listed. FINDING sec 2 shows the members of a frozen unit span 150-350 m, so this is a
   position to a few hundred metres, not a point.

## 5. Adversarial review

- *The threshold is fitted to nine points and three positives.* True, and it is the reason
  DEMO_READINESS row 20 puts the map graphic and the overlay first and holds `TASKABRT` back.
  The margin is 0.096 wide on a ratio whose inputs (bilinear sampling, an assumed soil hop, a
  path-cost analogy for the limit) each move things by more than that - the sensitivity grid in
  section 2 shows the same nine legs giving -0.008 at another sampling. Treat 0.92 at a 40 m
  window as the current operating point, not as a constant.
- **Competing hypothesis not excluded: the known formation / scale-crawl stop (memory C1b; S4
  2026-09-06). Two of the four leg-1 stops in P11 (856/HHC on flat ground, -0.05 over the 40 m
  ahead of the stop and -0.02 over the 40 m behind; 1-6 on a downhill, -0.13) occur on ground
  this metric scores as benign, so the pre-flight explains SOME stops (1-35 stopping 38 m from
  its window centre, 4-27 inside its window), not the stop class.** The correct wording for the
  warning is "this leg crosses a sustained face your vehicles probably cannot climb", never
  "this is why your unit will stop" - which is why the emitters now say PREDICTED IMPASSABLE
  with the ratio and the threshold, and no longer assert that the vehicles cannot traverse.
- *A confound: the two steepest-scoring units also start closest to the ridge.* Weakened, not
  excluded. C/1-35 starts in the same assembly area, from a real P11 spread position, and its
  first leg crosses the same ground at ratio 0.725 and reached its vertex; 1-1 and 40 likewise
  start in the pile and clear it. 4-27 breaks the confound from the other side: it froze 22.7 km
  from the assembly area, on its own scored window. The one unit whose flagged leg crosses that
  ridge without ground truth, 510/40, does so from the AUTHORED assembly coordinate (it was
  never tasked in P11, so it has no spread start) - its geometry is not what a run would
  produce, and that row should not be read as evidence either way.
- *The instrument could be reproducing the FINDING's arithmetic rather than the terrain.* It is
  a different cut (straight-line, 8 m, bilinear) of the same tiles and gives 0.826 over 40 m
  against 0.858 over 55 m at the same point, and it independently reproduces 1-6's number. The
  elevation control at 34.65607/-116.76144 returns 1585.61 m against the sim's reported 1585.50.
- *Unexplained, recorded:* T13 (510/40, ratio 0.98) and T29 (856/HHC, 1.01) sit above the
  threshold on sand with no ground truth - 510/40 was never among P11's ten tasked units (and
  runs from the authored pile), and 856/HHC's flagged leg is its SECOND task, reached only under
  the chaining assumption. If a later run tasks 510/40 from a real spread position and it
  crosses that ridge unhindered, the threshold is too low and this document says so first.

## 6. Next

1. Wire the warning channel: the `--c2sim-observations` skeleton into `ReportBuilder` and the
   `--vrf-overlay` skeleton into a C# consumer of the remote controller's overlay-object API
   (DEMO_READINESS row 20 deliveries 2 and 3). Both emitters are written against the SDK's
   generated classes and a captured report; the one open schema question is recorded as a TODO
   in the emitter - C2SIM 1.0.2 has no observation type carrying a location AND free text, so a
   flagged leg currently emits a `LocationObservation` plus a `NameObservation` whose `Marking`
   carries the numbers.
2. Fix the +10.0 m systematic in the route-authoring heights (FINDING sec 1 note) while in that
   code.
3. Measure the second stop mechanism before reading any of this as a stop predictor: 4-27's and
   856/HHC's object consoles at notify level 4 are the instrument (memory: vendor diagnostics
   first), and the formation gate of DESIGN_ORBAT C1b is the standing candidate.
4. Only after a run with zero false alarms on legs that DID pass: consider `TASKABRT` at order
   time.
5. The confirming test FINDING sec 7 registers - release 1-35 from the toe with a route that
   avoids the face - is now cheap to author: the tool prints the worst window's endpoints.
