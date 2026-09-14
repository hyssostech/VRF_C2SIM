# 4-27 on G3 + offset-line pre-flight scoring (Opus, 2026-09-14 ~13:25Z; lane L11). Adopted in FINDING sec 7e:
# the P11 pattern reproduces as a property of the LANE (three of six vehicles stop at the toe in both runs, stop
# points 1.2-7.6 m apart across different vehicles), not of the leader; in G3 the leader crested the face 43.7 m
# higher and descended; the ~0.30 m/s constant crawl is absent. Pre-flight: keep the route line as the verdict,
# print the formation band as a sensitivity range (lateral sensitivity exceeds the calibration margin).

# 4-27 on G3, and offset-line scoring for the route pre-flight

Read record, Opus analysis executor, 2026-09-14. READ-ONLY: nothing was launched, no tracked file
was written, no vendor sim log (C:\MAK\logs, runs\launch52, vrfSim*.log) was opened. All tiles came
from a COPY of the committed cache in the scratchpad; `tiles fetched: 0` on every run below.

`<REPO>` = `C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM`
`<G3>` = `<REPO>\runs\20260913T174516Z_run`   `<G2>` = `<REPO>\runs\20260907T174654Z_run`
`<P11>` = `<REPO>\runs\20260907T150643Z_run`

Tier: HEAVY (the answer to Q1 is a cause-bearing reproducibility claim and it CONTRADICTS a
published verdict). Adversarial review note at the end of each part; falsification gate applied.

---

## 0. HEADLINE

**Q1. 4-27's P11 pattern does NOT reproduce in G3 at the unit or leader level, and DOES reproduce
at the OFFSET-LINE level.** In G3 the leader M1A2 3 drove a line 7.2 m right of the pre-flight's
scored line, climbed the same face at 0.45-0.84 in successive steps (0.51 over its last 60 m of
track), crested 43.7 m HIGHER than P11's leader ever reached, descended the far side and was still
advancing (1.40 m/s, decaying) when the capture ended 350 m past P11's stop. Three of its five
followers stopped at the toe instead - and one of them, HMMWV 4 on the +30.9 m line, stopped
**1.24 m** from where P11's leader (on the +34.6 m line) stopped. A second pair matches to 6.95 m.
The ~0.30 m/s constant follower crawl is NOT present.

**Q2. Offset-line scoring is not the fix for row 20, and the reason is measurable: the ratio's
LATERAL sensitivity is bigger than its calibration margin.** On 4-27's leg the same 40 m statistic
reads 0.902 / 0.966 / 1.130 at -10 / 0 / +20 m of lateral shift, while the whole calibration rests
on a margin of 0.097. A formation-band MAX keeps "3/3 frozen, 0 false alarms" at threshold 0.92 only
because 1-1/2/1_AD lands 0.003 under it, and it lifts the crawling unit 856/HHC from 0.864 to 1.056
- above the lowest frozen leg (1.054) - so under the strict reading the separation goes NEGATIVE
(-0.003), the same failure mode the 80 m window has. **Recommendation: keep the route line as the
verdict line, print the formation band as a lateral-sensitivity range, do not raise the flag from
it.** The leader's nominal line IS the route line (`rightOffset=0`, vendor console) - but measured,
the leader drives +15.2 m off it in P11 and +0.1 m in G3 on the identical leg.

---

# PART 1 - 4-27/2/1_A on G3 (Q1)

## 1.1 Sources and instrument validation

| what | value | evidence |
|---|---|---|
| run | `<G3>` (navigation area present, idle machine) | - |
| unit shell, level 4 | `17c4e45b-3b47-964c-b7b4-d8800e1e65c8` | `<G3>\vrfc2simapp.log:1297` |
| members, level 3 (6) | M1A2 3 `1a079389`, M1A2 4 `4953599d`, M3 2 `6a6abe6c`, HMMWV 3 `5261a0d9`, M577A2 2 `b45ee546`, HMMWV 4 `953a03f0` | `<G3>\vrfc2simapp.log:1445`, repeated `:1673` |
| level CONFIRMED from their own rows | "Setting to notify level 3." wall 45.7, all six | `<G3>\watchvrf-trace.csv:747-752` |
| **leader = M1A2 3, VERIFIED** | its own task: `unitRoute=M1A2 3's Offset Route; leader=M1A2 3; M1A2 4, HMMWV 3, M3 2, HMMWV 4, M577A2 2; formationLength=60; rightOffset=0; forwardOffsetFromLeader=0` | `<G3>\vrfc2simapp.log:1907` |
| followers' own `leader=M1A2 3` rows | M1A2 4 `rightOffset=-50`, M3 2 `+50`, HMMWV 3 `0/-40`, HMMWV 4 `+25/-60`, M577A2 2 `-25/-60` | `<G3>\vrfc2simapp.log:1877,1897,1899,1871,1915` |
| task and route | T5, `CreateRoute ... (2 pts)`; terrain profile alts [1291.7, 1044.7] | `<G3>\vrfc2simapp.log:1677`, `:1671` |
| capture | 756 POS fixes x 7 objects, wall 45.8 - 1,590.6; 103,993 member console rows | this read |
| sim clock (from the console stamps) | wall 45.8 -> sim 67.9 ; wall 1,590.6 -> sim 2,562.2 ; mean **1.62x** | `<G3>\watchvrf-trace.csv` CON payloads |

Instrument checks, all passed before any result was read:
- `leg_check.py --selftest --offline`: elevation at 34.65607/-116.76144 = **1585.61 m** (expected
  ~1585.6), soil Sagebrush -> sand 0.80, 5 vendor templates resolved, **0 tiles fetched**.
- fix counts reproduce the published reads exactly: P11 **2,089** per entity (READ_4-27 sec B),
  G2 **2,129** per entity x 7 (READ_G2 sec A).
- leg length from the P11/G3 start to T5's far vertex = **32,269.0 m** (tool: 32,268.98).
- the nine calibration ratios reproduce to three decimals (sec 2.1).
- **the dead-reckoning falsifier of READ_G2 sec 1b does not fire in G3**: the leader's reported
  altitude against the streamed terrain over its whole track is median **-0.00 m**, range
  **-0.09 .. +0.05 m** (76 samples). G3's sim ran at 1.62x, not G2's 0.02x collapse.

`<REPO>\tools\WatchVrf\WatchRunner.cs:14` - the trace carries POSITION ONLY; no velocity exists in
G3 either, so every speed below is a position delta in SIM seconds.

## 1.2 The leader did not stop - it climbed the face and went over it

Along-leg distances use `leg_check.py:719-722`'s projection from A = the leader's first POS fix
(34.667649/-116.724799, **identical in P11 and G3**) to T5's far vertex 34.3993811486055/
-116.85915007933568. The pre-flight's worst 40 m window is centred **22,702 m** along.

| G3, M1A2 3, own fixes | along m | alt m | d_along | d_alt | rise/run |
|---|---|---|---|---|---|
| wall 1,474.2 sim 2,393.9 | 22,627.8 | 1,080.4 | +32.3 | +0.9 | 0.028 |
| wall 1,476.2 sim 2,396.9 | 22,659.0 | 1,086.3 | +31.2 | +5.9 | **0.189** |
| wall 1,478.2 sim 2,399.8 | 22,687.7 | 1,099.1 | +28.7 | +12.8 | **0.446** |
| wall 1,480.3 sim 2,401.9 | 22,702.6 | 1,110.6 | +14.9 | +11.5 | **0.771** |
| wall 1,482.3 sim 2,404.8 | 22,721.7 | 1,122.7 | +19.1 | +12.1 | **0.632** |
| wall 1,484.3 sim 2,407.7 | 22,742.4 | 1,135.3 | +20.7 | +12.6 | **0.609** |
| wall 1,486.4 sim 2,410.8 | 22,756.5 | 1,147.1 | +14.1 | +11.8 | **0.838** |
| wall 1,488.4 sim 2,413.7 | 22,766.3 | 1,152.7 | +9.8 | +5.6 | 0.574 |
| **wall 1,492.6 sim 2,420.1 CREST** | **22,786.5** | **1,155.5** | - | - | - |
| wall 1,590.6 sim 2,562.2 (capture end) | 23,045.9 | 1,089.8 | +259.4 from the crest | -65.7 | descending |

Cumulative on its own track to the crest: last 40 m **0.459**, last 60 m **0.507**, last 100 m
**0.543**, last 200 m 0.351. Its worst 40 m window scored off the tiles along its ACTUAL track is
**0.710 / 0.752 = 0.944**.

**P11's leader, for comparison** (recomputed here from `<P11>\watchvrf-trace.csv`, reproducing
READ_4-27 sec D): furthest reach along 22,708.6 m, alt 1,111.8 m; cumulative over its own last
39.1 m of track 0.660, over 70.1 m 0.462 - identical to the published 0.660 / 0.462 at the same
actual runs. It never got above 1,111.8 m. **G3's leader crested at 1,155.5 m, 43.7 m higher, and
came down the other side.**

## 1.3 Where each vehicle ended, and who stopped

Stop test as in READ_G2 sec 1: the first fix followed by < 5 m of displacement over the next 60
wall s. Cross-track (XT) is metres RIGHT of the tool's scored line; `slot` is the vendor's own
`rightOffset` from the console rows above.

### G3 (capture ends wall 1,590.6 = sim 2,562)
| member | slot | stop wall / sim | along at stop | alt | XT | at capture end (stop +102..108 wall s) | outcome |
|---|---|---|---|---|---|---|---|
| M1A2 3 (leader) | 0 | - | - | - | +7.2 | along 23,045.9, alt 1,089.8 | **crossed, still moving** |
| M1A2 4 | -50 | - | - | - | -63.0 | along 23,046.5, alt 1,071.0 | **crossed, still moving** |
| M577A2 2 | -25 | - | - | - | -46.4 | along 22,986.0, alt 1,077.6 | **crossed, still moving** |
| M3 2 | +50 | 1,488.4 / 2,413.7 | 22,690.6 | 1,096.6 | +68.3 | along 22,687.6, alt 1,094.7 | **STOPPED at the toe** |
| HMMWV 3 | 0 | 1,482.3 / 2,404.8 | 22,681.4 | 1,094.5 | -11.8 | along 22,685.1, alt 1,097.0 | **STOPPED at the toe** |
| HMMWV 4 | +25 | 1,486.4 / 2,410.8 | 22,696.5 | 1,101.0 | +30.9 | along 22,696.4, alt 1,100.9 | **STOPPED at the toe** |

A full +300 s window does not exist in G3: the capture ends 102-108 wall s (145-152 sim s) after the
three stops. Over that tail the three held to within 3.1 m of their stop positions.

### The reproduction is at the LINE, not at the vehicle
| P11 stop | XT | G3 stop | XT | distance apart |
|---|---|---|---|---|
| **M1A2 3 (leader)** 34.479034/-116.819681, along 22,697.2, alt 1,101.6 | +34.6 | **HMMWV 4** 34.479037/-116.819668, along 22,696.4, alt 1,100.9 | +30.9 | **1.24 m** |
| **HMMWV 3** 34.479042/-116.819751, along 22,698.8 | +41.9 | **HMMWV 4** (same) | +30.9 | 7.63 m |
| **M577A2 2** 34.479033/-116.819158, along 22,679.1 | -8.6 | **HMMWV 3** 34.478971/-116.819148, along 22,685.1 | -11.8 | **6.95 m** |

Two runs, two different vehicles per pair (one of them an HMMWV, whose own `max-slope` is 1.0 -
HIGHER than the M1A2's 0.94), the same ground to within 1.2 m and 7.0 m. The P11 pattern reproduced
as a property of the LANE, not of the unit, the vehicle or the role.

## 1.4 Speeds, and the ~0.30 m/s crawl question

Net displacement / dt in SIM seconds (path rate in brackets where it differs). P11's leader stopped
at sim 2,366; G3's collapse from 10 m/s begins in the same window.

| member | sim 2,300-2,360 | 2,400-2,460 | 2,460-2,520 | 2,500-2,562 |
|---|---|---|---|---|
| M1A2 3 (leader) | 9.92 | 3.16 | 1.71 | **1.40** |
| M1A2 4 | 10.00 | 3.18 | 1.71 | **1.40** |
| M577A2 2 | 9.98 | 3.16 | 1.71 | **1.39** |
| M3 2 | 9.98 | 0.38 (3.07) | 0.25 (1.51) | 0.10 (1.18) |
| HMMWV 3 | 9.97 | 0.43 (1.24) | 0.02 (0.53) | 0.03 (0.46) |
| HMMWV 4 | 9.97 | 0.94 (1.61) | 0.00 (0.54) | 0.01 (0.46) |

**The constant ~0.30 m/s follower crawl is NOT present in G3.** What IS present is the other half of
the P11 signature: **the moving group shares one speed to three significant figures** (1.71/1.71/1.71,
then 1.40/1.40/1.39), exactly as P11's three crawlers shared 0.301/0.299/0.301. The stopped three
shuffle at 0.46-1.5 m/s of path with 0.00-0.25 m/s net - the shape P11's LEADER showed (0.04 net,
1.71 path). **In G3 the roles are swapped and the crawl speed is 4.6x higher.**

## 1.5 The console says the same seven things, and nothing else

103,993 member rows scanned for the six. Keyword scan over all of them:

| keyword | hits after wall 90 |
|---|---|
| `BlockedByVehicle` / `Blocked` / `Movement stopped` | **0** (5 hits total, all at wall 61.7-65.3, spin-up) |
| `stuck`, `give`, `Failed`, `Global Replan`, `Entity not embarked`, `slope`, `impassable` | **0** |
| `Completed` | **0** (34 total, all at wall 55.3-79.9) |
| `ordered speed` | **0** (7 total, all at wall 49.0-73.2 - "Saving ordered speed") |
| `terrain page` | **0** (2 total, wall 68.3 and 72.8) |
| the seven-shape loop ("Maybe plan path" / "Goal new or changed?" / "Condition false." / "Maybe Skirt Blockage" / "Is path blocked?" / "Condition false." / `Status of task "move-along" is "TaskRunning"`) | 2,449 cycles for the leader = **99.1 %** of its 17,304 rows |

**The ONLY non-loop row after wall 600, for any of the six:** `Leaving Primary nav area:
NavArea-ground-platform MojaveAO20`, at wall 1,172.8 / 1,174.6 / 1,176.2 / 1,176.7 / 1,177.1 /
1,178.8 - `<G3>\watchvrf-trace.csv:949789,951224,952268,952975,953165,954565`. All six left the
navigation area **300 wall seconds BEFORE the face**. The mesh had no say in this stop; the goal is
22.7 km out and the area is 20x20 km (FINDING sec 8's "areas that CONTAIN the goals").

## 1.6 P11 vs G3, one table

| | P11 (`20260907T150643Z_run`) | G3 (`20260913T174516Z_run`) |
|---|---|---|
| start (identical) | 34.667649/-116.724799 | 34.667649/-116.724799 |
| leg / window centre (identical) | 32,269 m / 22,702 m | 32,269 m / 22,702 m |
| capture | 2,089 fixes, wall 4,326, **sim 6,288** | 756 fixes, wall 1,591, **sim 2,562** |
| sim/wall | 1.45x | 1.62x |
| LEADER's line (mean XT) | **+15.2 m** | **+0.1 m** |
| leader's speed in the last 60 sim s before the face | 9.92 m/s | 9.92 m/s |
| leader at the face | **stopped**, sim 2,366, along 22,696.2, peak alt 1,111.8 | **climbed it**, crest sim 2,420, along 22,786.5, alt **1,155.5** |
| leader's own-track pitch into it | 0.558 then 0.879 (READ_4-27 sec D) | 0.446, 0.771, 0.632, 0.609, 0.838 |
| leader afterwards | 22.5 m limit cycle decaying to 3.2 m, zero net progress for **3,922 sim s** | descended 65.7 m, +259 m along, **still moving at 1.40 m/s** at sim 2,562 |
| members that stopped at the toe | 3 of 6 (leader, HMMWV 3, M577A2 2) | 3 of 6 (M3 2, HMMWV 3, HMMWV 4) |
| members that went past | 3 of 6 (M1A2 4, M3 2, HMMWV 4), 1,078-1,246 m, **0.301/0.299/0.301 m/s** | 3 of 6 (leader, M1A2 4, M577A2 2), 290-350 m, **1.40/1.40/1.39 m/s** |
| tool's leader-derived label for T5 | **FROZE** | **MOVED** (advance over the final 600 s = +8.0 km) |
| console | seven shapes, TaskRunning for ever, no exit row | identical seven shapes, 99.1 % of rows, no exit row |
| nav area | none | left it 300 wall s before the face |

## 1.7 Adversarial review of Part 1

**Competing hypothesis 1 - "the G3 climb is dead reckoning, not motion" (the falsifier READ_G2 sec
1b installed against exactly this kind of reading).** CHECKED AND REFUTED: the leader's altitude
residual against the streamed terrain is median -0.00 m over its whole track, range -0.09..+0.05 m;
G2's artefact showed +53.7 m in the air and -139 m underground. G3's ratio is 1.62x, not 0.02x. A DR
ramp also snaps back; this track crests and then descends monotonically over 98 wall seconds.

**Competing hypothesis 2 - "P11 and G3 are the same outcome and the G3 capture simply ended too
early."** PARTLY CONCEDED, and it bounds the claim. G3's capture ends at sim 2,562, only 196 sim s
after P11's leader stopped and 142 sim s after G3's own crest. I CANNOT say G3's leader completed
anything; I can say it **crossed the flagged window and descended the far side**, which P11's leader
never did in 3,922 further sim seconds of trying. The three "still moving" G3 vehicles are
UNDETERMINED, not "passed", and their 1.40 m/s could still be decaying toward P11's 0.30 m/s.
**This is the one symptom I cannot close** and the reason Part 1 does not claim a clean pass.

**Competing hypothesis 3 - "the 1.24 m stop-point coincidence is chance."** WEAKENED, NOT EXCLUDED:
three pairs match at 1.24 / 6.95 / 7.63 m across two runs with different vehicles on the line, and
the vehicle whose own limit is HIGHER (HMMWV, max-slope 1.0) is one of the stoppers. Against it: the
two runs share the identical order, route, terrain and DeStack spread, so they are not independent
draws - only the slot-to-vehicle assignment differs. A third run with a different spread is the test.

**What this contradicts in the record.** FINDING sec 7c's 4-27 verdict is a P11 statement and stays
one; its own closing line ("a second read would test whether the 3-of-5 crawl-past is
reproducible") is now answered: **it does not reproduce as stated**. The crawl-past is not a property
of "followers of a stopped leader" - in G3 the leader is among the movers. What reproduces is the
LANE. FINDING sec 7b's sentence "two independent freezes ON scored faces support the mechanism of
sec 7" survives for P11 but must now read "three of six lanes in one run, three of six in another".

---

# PART 2 - OFFSET-LINE SCORING (Q2)

## 2.1 Method, and the instrument reproducing

`leg_check.py`'s own functions were imported unchanged - `Tiles.elev` (TMS 149 L13, bilinear),
`SoilChain.classify`, `analyse_leg` (`:806-870`) with its `worst()` window (`:823-846`), `dist_m`,
`interp`, `project_along`, `build_route`, `run_preflight`, `truth_for`. Two drivers were added: an
OFFSET-LINE generator (shift both endpoints d metres along the right-hand normal in the same
tangent plane) and a POLYLINE sampler (resample an actual track every 8 m, then the same sliding
40 m max-mean-uphill). The nine calibration ratios reproduce the published table exactly:

| unit | published (PREFLIGHT_CALIBRATION sec 2) | recomputed here |
|---|---|---|
| 1-35/2/1_A | 1.098 | **1.098** |
| 1-6/2/1_AD | 0.990 | **0.990** |
| 4-27/2/1_A | 0.966 | **0.966** |
| 1-1/2/1_AD | 0.870 | **0.870** |
| 856/HHC | 0.864 | **0.864** |
| 40/2/1_AD | 0.743 | **0.743** |
| C/1-35 | 0.725 | **0.725** |
| B/5-20 | 0.452 | **0.452** |
| 5-20/2/1_A | 0.438 | **0.438** |

0 tiles fetched, 0 NaN samples on any offset line of any leg.

## 2.2 Which line does the LEADER drive?

**Nominally, offset 0 - the unit route itself.** The leader's own `maneuver-in-formation` parameters
carry `rightOffset=0; forwardOffsetFromLeader=0` on a route named after itself:

- G3 4-27: `unitRoute=M1A2 3's Offset Route; leader=M1A2 3; ...; formationLength=60; rightOffset=0;
  forwardOffsetFromLeader=0` - `<G3>\vrfc2simapp.log:1907`
- G2 1-6: same shape for M1A2 19 - `<G2>\vrfc2simapp.log:2153`; and after the task failure, for the
  new leader M1A2 20 - `<G2>\vrfc2simapp.log:212525`
- followers take `rightOffset` in {-50, -25, 0, +25, +50} and `forwardOffsetFromLeader` in
  {0, -40, -60} (`forwardOffset` +26.67 / -13.33 / -33.33) - `<G3>\vrfc2simapp.log:1871,1877,1897,
  1899,1915`; `<G2>\vrfc2simapp.log:2155,2157,2169,2177,2189`; P11's equivalents are
  `<P11>\vrfc2simapp.log:2077-2091` (INHERITED from READ_4-27 sec A, not re-opened here).

**Measured, the leader is NOT on the tool's line, and not reproducibly anywhere:**

| run / unit | leader | mean XT vs the tool's scored line | the route's own anchor (unit shell) |
|---|---|---|---|
| P11 4-27 | M1A2 3 | **+15.2 m** (at its stop, +34.6 m) | -10.2 m |
| G3 4-27 | M1A2 3 | **+0.1 m** (at the face, +7.2 m) | -10.2 m |
| G2 1-6 | M1A2 19 | **-23.8 m** | (not computed) |
| G3 1-6 | M1A2 19 | **-15.0 m** | (not computed) |

Two causes, both measured. (i) The tool anchors the leg at the **leader's first POS fix**
(`starts_P11.csv`, `--starts-from-run`), while the sim anchors the route at the **unit shell's live
position**: in both P11 and G3 the shell's first fix is 26.7 m from the leader's, **-10.2 m** of
cross-track. (ii) Each member joins its slot from wherever DeStack left it, and the resulting line
drifts: within one P11 unit the six lanes span **-65.8 .. +108.4 m**, wider than the +/-50 m the
slots nominally allow.

## 2.3 Ratio per offset line - the three flagged first legs and the whole calibration set

Offsets are metres RIGHT of the route line; `+0` is the tool's current verdict. Step 8 m, window
40 m, bilinear, the units' own limits (Tank HQ Section -> 0.94; sand -> x0.80 = 0.752).

| unit | P11 label | leg m | **-75** | **-50** | **-25** | **+0** | **+25** | **+50** | **+75** | band min..max (+/-60) |
|---|---|---|---|---|---|---|---|---|---|---|
| **1-35/2/1_A T1** | FROZE | 6,593 | 0.977 | 1.240 | **1.277** | 1.098 | 0.877 | 0.798 | 0.848 | 0.798 .. **1.277** |
| **1-6/2/1_AD T15** | FROZE | 8,783 | 1.216 | **1.237** | 1.023 | 0.990 | 0.810 | 0.796 | 0.815 | 0.796 .. **1.237** |
| **4-27/2/1_A T5** | FROZE | 32,269 | 0.580 | 0.364 | 0.647 | 0.966 | **1.054** | 0.924 | 1.034 | 0.364 .. **1.054** |
| 1-1/2/1_AD T23 | MOVED arrived | 6,168 | 0.760 | 0.822 | 0.829 | 0.870 | 0.910 | **0.917** | 0.858 | 0.822 .. **0.917** |
| 856/HHC T27 | MOVED crawling | 24,594 | 0.993 | **1.056** | 0.992 | 0.864 | 0.916 | 0.916 | 0.870 | 0.864 .. **1.056** |
| 40/2/1_AD T19 | MOVED arrived | 9,425 | 0.806 | 0.802 | 0.753 | 0.743 | 0.682 | 0.679 | 0.775 | 0.679 .. 0.802 |
| C/1-35 T39 | MOVED arrived | 9,323 | 0.627 | 0.688 | 0.782 | 0.725 | 0.624 | 0.539 | 0.488 | 0.539 .. 0.782 |
| B/5-20 T35 | MOVED arrived | 9,266 | 0.710 | 0.614 | 0.499 | 0.452 | 0.512 | 0.434 | 0.439 | 0.434 .. 0.614 |
| 5-20/2/1_A T31 | MOVED arrived | 10,155 | 0.434 | 0.509 | 0.461 | 0.438 | 0.503 | 0.607 | 0.635 | 0.438 .. 0.607 |

Fine sweep every 5 m on the two legs that matter (ratio):

    1-6 T15   -60:1.324  -50:1.237  -40:1.060  -30:0.970  -20:1.064  -10:1.086   +0:0.989
              +10:0.946  +20:0.822  +30:0.845  +40:0.826  +50:0.796  +60:0.810  +100:0.898
    4-27 T5   -60:0.328  -50:0.364  -40:0.430  -30:0.551  -20:0.748  -10:0.902   +0:0.966
              +10:0.999  +20:1.130  +30:1.004  +40:0.983  +50:0.924  +60:0.974  +100:0.822

**The lateral sensitivity is larger than the calibration margin.** On 4-27's leg, 30 m of lateral
shift - one formation slot, or the distance between the two defensible anchors - moves the ratio
from 0.902 to 1.130. The whole threshold rests on a margin of 0.097.

## 2.4 The 40 m ratio along the ACTUAL tracks

Each member's own track, resampled every 8 m and scored with the same statistic; truncated at its
own stop (the ground it actually drove); fixes whose reported altitude is more than 3 m off the
streamed terrain are dropped as the READ_G2 sec 1b dead-reckoning artefact (count shown).

### 1-6/2/1_AD in G2 (`<G2>`, uuids `<G2>\vrfc2simapp.log:1509`)
| member | slot | stop wall | along at stop | mean XT | XT at stop | track sust 40 m | **ratio** | DR fixes dropped |
|---|---|---|---|---|---|---|---|---|
| M1A2 19 (leader, task-less from sim 320) | 0 | 345.2 | 2,865 | -23.8 | -9.6 | 0.512 | **0.681** | 0 |
| M3 4 (the pusher) | +50 | 345.2 | 2,863 | -18.4 | -8.5 | 0.540 | **0.718** | 0 |
| M1A2 20 (re-formed leader) | -50 | 853.4 | 6,099 | -43.2 | -37.0 | 0.557 | **0.741** | 0 |
| HMMWV 7 | 0 | 812.7 | 6,090 | -33.9 | -0.4 | 0.587 | **0.781** | 0 |
| M577A2 4 | -25 | 1,246.9 | 6,050 | +1.0 | +35.1 | 0.558 | **0.742** | 8 |
| HMMWV 8 | +25 | 1,402.9 | 6,266 | +74.0 | +169.6 | 0.655 | **0.871** | 3 |

### 1-6/2/1_AD in G3 (`<G3>`, uuids `<G3>\vrfc2simapp.log:1423`)
| member | slot | stop wall | along at stop | mean XT | XT at stop | track sust 40 m | **ratio** |
|---|---|---|---|---|---|---|---|
| M1A2 19 (leader) | 0 | 275.1 | 3,288 | -15.0 | -13.6 | 0.624 | **0.830** |
| M1A2 20 | -50 | 295.6 | 3,184 | -58.9 | -63.7 | 0.846 | **1.124** |
| M3 4 | +50 | 424.7 | 3,358 | +33.7 | +39.0 | 0.625 | **0.831** |
| HMMWV 7 | 0 | 526.7 | 3,312 | -5.4 | -2.4 | 0.581 | **0.772** |
| M577A2 4 | -25 | 279.2 | 3,191 | -46.7 | -50.4 | 0.680 | **0.904** |
| HMMWV 8 | +25 | (none) | 3,506 at the end | +20.7 | +22.6 | 0.599 | **0.797** |

Two readings this forces:

1. **An actual track's ratio is a LOWER BOUND on what the vehicle could climb, never a predictor** -
   a vehicle that stops before a face never books the face's grade. The highest value any surviving
   track shows is G3's 4-27 leader at **0.944** (0.710/0.752) - and it climbed it and went over.
   A straight offset line through the same window scores 0.941-1.010. The face does not have to be
   under 0.92 to be climbable.
2. **The offset line is not the ground the offset vehicle meets.** In G2, M1A2 20, HMMWV 7 and
   M577A2 4 crossed the flagged T15 window (READ_G2 sec 9: 20-22 m from its centre) meeting
   **0.41-0.43** on their own tracks, while their own STRAIGHT offset lines through that same
   window score **0.961 / 1.007 / 0.817**. Real tracks curve into gentler ground; a parallel straight
   line does not.

### The sharpest test: the 40 m window immediately AHEAD of each stop, on that vehicle's own offset line
| run / unit | member | XT | ahead-of-stop ratio | outcome | rule right? |
|---|---|---|---|---|---|
| P11 4-27 | M1A2 3 | +34.6 | **1.002** | stopped | yes |
| P11 4-27 | HMMWV 3 | +41.9 | **0.958** | stopped | yes |
| P11 4-27 | M577A2 2 | -8.6 | 0.921 | stopped | yes (0.001 over) |
| P11 4-27 | M1A2 4 | -62.8 | 0.363 (at the window) | crossed | yes |
| P11 4-27 | M3 2 | +107.6 | 0.756 (at the window) | crossed | yes |
| P11 4-27 | HMMWV 4 | -22.7 | 0.693 (at the window) | crossed | yes |
| G3 4-27 | M3 2 | +68.3 | **1.028** | stopped | yes |
| G3 4-27 | HMMWV 4 | +30.9 | **1.000** | stopped | yes |
| G3 4-27 | HMMWV 3 | -11.8 | 0.851 | stopped | **MISS** |
| G3 4-27 | M1A2 3 | +7.2 | **0.941** (at the window) | **crossed** | **FALSE ALARM** |
| G3 4-27 | M1A2 4 | -63.0 | 0.365 (at the window) | crossed | yes |
| G3 4-27 | M577A2 2 | -46.4 | 0.376 (at the window) | crossed | yes |
| G3 1-6 | M1A2 19 | -13.6 | **1.076** | stopped | yes |
| G3 1-6 | M1A2 20 | -63.7 | **1.163** | stopped | yes |
| G3 1-6 | M577A2 4 | -50.4 | **1.102** | stopped | yes |
| G3 1-6 | HMMWV 7 | -2.4 | **0.974** | stopped | yes |
| G3 1-6 | M3 4 | +39.0 | 0.797 | stopped | **MISS** |
| G3 1-6 | HMMWV 8 | +22.6 | 0.809 (at the window) | crossed | yes |
| G2 1-6 | M1A2 20 | -37.0 | 0.961 (at the window) | crossed | **FALSE ALARM** |
| G2 1-6 | HMMWV 7 | -0.4 | 1.007 (at the window) | crossed | **FALSE ALARM** |
| G2 1-6 | HMMWV 8 | +169.6 | 0.913 (at the window) | crossed | yes |
| G2 1-6 | M577A2 4 | +35.1 | 0.817 (at the window) | crossed | yes |
| G2 1-6 | M1A2 19, M3 4 | -9.6, -8.5 | 0.577, 0.594 ahead of their jam | stopped (the READ_G2 sec 7d push, not a slope stop) | n/a |

**22 scoreable lines: 17 right, 2 misses (0.851, 0.797), 3 false alarms.** All three false alarms
are crossings of a window scored >= 0.92 on a line the vehicle did not actually stay on; two of the
three are the G2 re-formed members whose real tracks met 0.41-0.43 there.

## 2.5 The decision: does a formation-band score keep the calibration?

Truth set = PREFLIGHT_CALIBRATION sec 2 (9 legs, 3 FROZE, 5 clean movers, 1 crawler), labels
DERIVED from the P11 trace by the tool's own `verify_leg`.

| candidate rule | lowest frozen | highest clean mover | **margin** | midpoint | flags @ 0.92 | misses / false alarms @ 0.92 | margin COUNTING the crawler as a mover |
|---|---|---|---|---|---|---|---|
| **A - route line (offset 0) = today's tool** | 0.966 | 0.870 | **+0.097** | 0.918 | 1-35, 1-6, 4-27 | none / none | **+0.097** |
| B - max over the +/-60 m formation band | 1.054 | 0.917 | +0.136 | 0.985 | 1-35, 1-6, 4-27, **856** | none / none | **-0.003** |
| B' - max over +/-75 m | 1.054 | 0.917 | +0.136 | 0.985 | same as B | none / none | **-0.003** |
| C - mean over the band | 0.791 | 0.869 | **-0.079** | - | 1-35, 1-6, 856 | **4-27 MISSED** / none | -0.158 |
| D - min over the band | 0.364 | 0.822 | **-0.458** | - | nothing | **all three MISSED** / none | -0.500 |
| E - max over +/-25 m only | 1.023 | 0.910 | +0.113 | 0.967 | 1-35, 1-6, 4-27, **856** | none / none | **+0.031** |

Read that table with three facts in front of it:

- **B's "0 false alarms" is 0.003 wide.** 1-1/2/1_AD, a clean mover that arrived, scores **0.917**
  under B against a 0.92 threshold. Any of the changes the sensitivity grid already documents
  (nearest sampling, a 4 m or 16 m step) moves ratios by more than 0.003.
- **B lifts the crawler above the lowest frozen leg.** 856/HHC goes 0.864 -> **1.056**, past
  4-27's 1.054. Counting the crawler as a mover - the strict reading PREFLIGHT_CALIBRATION prints
  in every sensitivity cell - B has **no separation at all**, which is precisely how the 80 m
  window failed (-0.003 there, -0.003 here).
- **B's premise is falsified by the tracks** (sec 2.4 item 2): a member on a slot does not meet the
  terrain of the straight slot line.

E (a half-band, +/-25 m) is the only widening that keeps a positive margin under both readings, and
it is worth **+0.031** - a third of A's. It also flags the crawler. It buys nothing A does not have.

**RECOMMENDATION**

1. **Keep rule A: the verdict stays the route line at threshold 0.92.** It is the only rule with a
   positive margin under both readings of the crawler, and it is the line the vendor says the
   leader drives (`rightOffset=0`).
2. **Add the band as a printed SENSITIVITY, not as a verdict.** Alongside the flag, print the ratio
   at the vendor's own slot offsets -50/-25/0/+25/+50 (they are in the app log, `maneuver-in-
   formation` parameters) and say what the spread means:
   - 1-6 T15: **0.796 .. 1.237** - above the limit on three of the five slots; a wall across most
     of the formation's width, and G3 duly stopped five of six vehicles within 174 m of each other.
   - 1-35 T1: **0.798 .. 1.277** - same shape.
   - 4-27 T5: **0.364 .. 1.054** - a NARROW ridge: the left half of the formation is on 0.36-0.65
     ground. G3 duly put three vehicles over it and stopped three.
   That single line turns "PREDICTED IMPASSABLE" into something an operator can act on, without
   touching the calibrated number.
3. **Do NOT re-anchor the leg on the unit shell as a "fix".** It is the geometrically correct
   anchor (the interface's route starts at the unit's live position) and it is -10.2 m, which on
   4-27's leg takes the ratio from 0.966 to **0.902** and UNFLAGS a leg that froze in P11. Both
   anchors are defensible and neither is the line the leader drove (+34.6 m in P11, +7.2 m in G3).
   Record the anchor as a stated assumption with its 26.7 m magnitude, as PREFLIGHT_CALIBRATION sec
   4 item 6 already half does.
4. **Soften the unit-level wording one more notch.** The truth labels are LEADER labels, and Part 1
   shows the leader is not the unit: on the same leg, from the same start, T5's leader froze in P11
   and crossed in G3 while three vehicles stopped at the toe in both. The defensible sentence is
   "this leg crosses a sustained face **some of your vehicles** probably cannot climb", with the
   band spread as the estimate of how many.
5. **The row 20 false-alarm candidate: NOT a false alarm as filed, but there is now a real one.**
   G2's three crossings are not evidence against T15's flag - they happened after a vendor task
   failure re-formed the unit (`<G2>\vrfc2simapp.log:212517-212525`), at a sim ratio collapsed to
   0.02x, on lines up to 37 m off the leader's, and G3's leader of the same unit froze on that face
   with **1.076** ahead of it on its own line. The genuine false alarm is **4-27 / T5 in G3**: a leg
   flagged at 0.966 whose leader climbed the face on a line scoring 0.941 and drove 350 m past it.
   One leg, one run, half the unit - log it against the threshold, do not move the threshold on it.

---

## 3. VERIFIED vs ASSUMED

**VERIFIED here, from the captures and the committed sampler:** every uuid, marking, notify level
and formation offset quoted (each with its file:line); the G3 clock map and 1.62x ratio; every
along-leg, cross-track, altitude and speed number for the 7 G3 4-27 objects, the 7 G2 1-6 objects,
the 7 G3 1-6 objects and the 7 P11 4-27 objects; the stop times under the stated rule; the
stop-point coincidences; the complete keyword inventory of G3's 103,993 member console rows and the
six `Leaving Primary nav area` rows; the altitude residuals; the nine calibration ratios reproduced
to three decimals; every offset-line and actual-track ratio (0 tiles fetched, 0 NaN samples).

**ASSUMED / INHERITED:** the far vertex of T5 (34.3993811486055/-116.85915007933568) and of T15
(34.609047/-116.803322) come from the published reads, not re-derived from the order XML - the app
log's "(2 pts)" and the reproduced 32,269.0 m leg length are consistent with them; P11's
`vrfc2simapp.log:2077-2091` slot rows are quoted from READ_4-27 sec A, not re-opened; the
`DtSoilType -> DtRoughnessSoilType` hop for 26 of 28 rows (every number here is a `sand` row, the
confirmed one); the 0.752 limit remains the sec-7 ANALOGY (`navigationPreferenceDescriptor.h:
111-132`), used here only to compare lines against each other on one common scale; the
"stopped" label for G3's three toe vehicles rests on 102-108 wall seconds of holding, not on a
P11-length tail.

**NOT IN ANY CAPTURE, and still needed:** reflected or commanded velocity, and any member console
row at notify level 4. Both P11 and G3 are level 3 on every member, so neither run can separate
"the vehicle could not climb" from "something commanded it to stop", and this read repeats that gap
rather than closing it - exactly as READ_4-27 sec E and READ_G2 sec 8 record.

## 4. Strongest competing explanation for the Part 2 recommendation

**"The band DOES help and the truth set is simply too small to show it."** Nine legs, three
positives, one order, one terrain - PREFLIGHT_CALIBRATION sec 5 says so itself, and a rule that
wins by 0.136 on nine points is not distinguishable from one that wins by 0.097. What tips it is
not the margin but sec 2.4 item 2: three vehicles crossed a window their own straight offset lines
score at 0.96-1.01 while meeting 0.41-0.43 on the ground. That is a measurement against the band's
PREMISE, not against its score, and it is the reason the recommendation keeps the band out of the
verdict. If a future run shows members stopping where their own slot lines score high and their
actual tracks also score high, the band becomes the right statistic and this recommendation should
be reopened - the evidence that would do it: a unit whose members stop at DIFFERENT along-distances
ordered by their slot's line score, with track ratios within 0.05 of the slot-line ratios.

**Dissent line, for the log:** I disagree with nothing settled. I record that FINDING sec 7c's
"the three crawlers are a second mechanism in the followers of a stopped leader" no longer fits G3,
where the leader is one of the movers; the evidence that would reopen the 7c reading as written is
a third run in which a stopped leader again yields exactly-equal follower crawl speeds.
