# 1-6's leader in G2 (a mesh-planned leg) - read record (Opus, 2026-09-14 ~12:00Z, HEAVY, 4 adversarial passes).
# Supervisor: this REFUTES my own interim reading ('a mesh path that still stopped near the P11 stop'); see
# FINDING_EARLY_STOPS_2026-09-13.md sec 7d for the adopted summary.

# G2's 1-6 leader: why M1A2 19 stopped on a MESH-planned leg (READ, 2026-09-14)

HEAVY tier (a cause question). Read-only: nothing was launched, no tracked file was written,
no vendor sim log (C:\MAK\logs, runs\launch52, vrfSim*.log, *.callstack.log) was opened. Every
row below comes from OUR captures (WatchVrf trace, interface log) and from the public terrain
tiles through the committed pre-flight sampler.

Paths: `<REPO>` = `C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM`,
`<G2>` = `<REPO>\runs\20260907T174654Z_run`, `<P11>` = `<REPO>\runs\20260907T150643Z_run`.
Work products (scratchpad only): `g2_1-6\{terrain.py,run_terrain2.py,con_tools.py,nonloop.py,
window.py,tracks.py,g2_track.csv,p11_track.csv,profile_g2.csv,profile_p11.csv,
g2_con_dec.csv,p11_con_dec.csv,g2_pos_all.csv,p11_pos_all.csv}`.

---

## 0. HEADLINE - the observation does not survive contact with the capture

**G2's M1A2 19 did not stop on a slope, and it was not driving a mesh path when it stopped.
It was tasked-out and then bulldozed 1,994 m up the ridge by M3 4, which was jammed against
its stern at 2.96 m (sd 0.10 m) for the last 4,200 s of the run.** The four members that kept
a task drove on to 6.3 km and three of them climbed THROUGH the pre-flight's flagged window.

Five specific corrections to the observation as given, each with its row:

| as stated | as the capture reads |
|---|---|
| "Planned path has 1109 points." at wall ~53.2 | at **wall 67.4** (`<G2>\watchvrf-trace.csv:20705`, sim 84.73). At wall 53.2 M1A2 19 planned **6** points (`:4260`) - that was its formation-slot hop, not the leg |
| leader stopped at along 2,881 m, wall 345 | net progress ends at **wall 341.1, along 2,875.4**, 34.645640/-116.756507, alt 1,645.8 m. 2,881/1,648 is the LAST fix (wall 4,366.6), inside the post-stop excursion |
| "a late creep to 3,013 m by wall 4,348" | **a dead-reckoning artefact of the collapsed sim, not motion.** At wall 4,348.4 the reported altitude is 1,728.7 m where the streamed terrain is 1,675.0 m - the entity is 53.7 m in the air - and two fixes later it snaps back to along 2,864.1 / alt 1,637.9 / residual -0.03 m. See sec 1b |
| the G2 track deviates up to 123 m from the start->V1 line (median 96) vs P11's constant +20-24 m | true only AFTER the leader lost its task. Over the 1,031 m it actually drove under the mesh path the G2 track is as straight as P11's: cross-track -34.9..-16.8 m, 1.73 deg/100 m, path/chord 1.002 |
| implicitly, that the two runs' 128-m-apart stops are the same kind of event | they are two different mechanisms. P11 = the sec-7b "second mechanism" stop on benign ground, seven-shape loop, leader still tasked. G2 = a task failure plus a vehicle-on-vehicle jam |

---

## A. SOURCES

### Files read
| file | what it gave |
|---|---|
| `<G2>\watchvrf-trace.csv` (229,070,884 B) | 2,129 POS fixes x 7 objects; 44,424 CON rows for 1-6's six members + unit |
| `<G2>\vrfc2simapp.log` (74,825,833 B) | member map, uuids, console-level requests, route creation, the task-failure sequence |
| `<P11>\watchvrf-trace.csv` (926,016,995 B) | 2,089 POS fixes x 6; 220,969 CON rows for 1-6's six members |
| `<P11>\vrfc2simapp.log` (378,559,695 B) | member map, uuids |
| `<REPO>\tools\preflight\leg_check.py` + `README.md` | the elevation/soil sampler and the window definition, REUSED (sec A, last block) |
| `<REPO>\tools\WatchVrf\WatchRunner.cs:14,:20-22` | row formats; proof the trace carries POSITION ONLY (no velocity) |
| scratchpad `calib_v2.json` | the pre-flight's own leg-1 metrics for 1-6 (worst window at along 3,319.6 m) |

Row formats (`WatchRunner.cs:14,22`): `POS,<elapsed-s>,<uuid>,<lat>,<lon>,<altM>` and
`CON,<elapsed-s>,<uuid>,<level>,"<xml payload>"`. The `t` column is OBSERVER WALL seconds; the
SIM clock is inside the console payloads (`<simtime>  <id> .`). Both clocks are used below.
WatchVrf joins the federation as its own read-only observer, so its console subscription is
INDEPENDENT of the interface's - a row missing from both sinks is missing at the sim.

### The entities of 1-6/2/1_AD~PXY (both runs: template Tank Headquarters Section (USA))
| role | marking | G2 uuid | P11 uuid |
|---|---|---|---|
| unit shell (level 4) | `1-6/2/1_AD~PXY` | `92bcf9f3-4016-2848-9455-9316914d19da` | (not read) |
| **formation LEADER at t=0** | `M1A2 19` | `67b414ca-0a44-2b40-9d3e-d0e3a899cb8a` | `b3b993cd-bf5f-ea44-8940-0c595efe2e94` |
| member | `M1A2 20` | `023f3fad-...` | `b336412f-...` |
| member | `M3 4` | `66459954-...` | `aea3f8af-...` |
| member | `HMMWV 7` | `222e08e0-...` | `7cf687ed-...` |
| member | `M577A2 4` | `0f142f19-...` | `399fa1aa-...` |
| member | `HMMWV 8` | `d1d7ab7a-...` | `32621b95-...` |

**Leader = M1A2 19, VERIFIED**: the other five print `leader=M1A2 19` in their own task
parameters - `<G2>\vrfc2simapp.log:2155,2157,2169,2177,2189`; `<P11>\vrfc2simapp.log:2195,2199,
2201,2211,2213` - and M1A2 19 prints the leader's own form,
`unitRoute=M1A2 19's Offset Route; leader=M1A2 19; ...; formationLength=60`
(`<G2>\vrfc2simapp.log:2153`, `<P11>\vrfc2simapp.log:2203`).

### Notify levels, with the evidence (the question asked)
**All six members at level 3 in BOTH runs, confirmed by their own rows, not by the request:**

    <G2>\watchvrf-trace.csv:730-735   wall 49.2  "Setting to notify level | 3."  (M1A2 19, M1A2 20, M3 4, HMMWV 7, M577A2 4, HMMWV 8)
    <P11>\watchvrf-trace.csv:906-911  wall 63.2  "Setting to notify level | 3."  (same six)

Requested at `<G2>\vrfc2simapp.log:1509,1691`. The unit shell is at level 4 (`<G2>\vrfc2simapp.log:1403`;
`watchvrf-trace.csv:799` "Rules of engagement set to: hold-fire" carries level field 4) and
produced **7 rows in the whole run**, all between wall 49.9 and 50.1 - the aggregate move-along
controller is again not a progress channel. ZERO level-4 rows exist for any member: a configured
outcome of the level-3 request, exactly as READ_4-27_CONSOLE sec A records. No velocity anywhere.

### Instrument validation (before any result is read)
`terrain.py` imports `leg_check.Tiles.elev` (TMS 149, L13, bilinear - the surface the sim
streams), `leg_check.SoilChain.classify`, `dist_m`, `interp` and `project_along` unchanged, and
adds only a POLYLINE driver (leg_check samples a straight leg; a track is not straight). The
window statistic is `analyse_leg.worst()`'s own definition (`leg_check.py:823-846`): the largest
MEAN UPHILL grade over any sliding window spanning at least `window` metres, sampled every 8 m;
the governing limit inside a window is the lowest `max-slope x soil-factor` in it.

- documented control reproduces: elevation at 34.65607/-116.76144 = **1585.61 m** (README
  `--selftest` expects ~1585.6); soil there = CA FVEG 15 m class 30 "Sagebrush" -> BM_SAND ->
  sand, factor 0.80.
- against the sim's OWN reported altitudes on these two tracks: median residual **-0.03 m (G2)**
  and **+0.06 m (P11)**; at the two stop points -0.02 m and -0.06 m. The sampler is reading the
  same surface the sim clamped these vehicles to.
- tiles fetched during this work: **0** (the cache was copied to the scratchpad and the run was
  fully offline; the repo cache was not written).

---

## 1. WHERE THEY STOPPED, and the stop test

Stop rule as instructed: the first fix followed by < 5 m of displacement over the next 60 s.
Along-leg distances are `project_along(start, V1, p)` with start = 34.662498/-116.732644 (both
runs, identical) and V1 = 34.609047/-116.803322; the leg is 8,782.8 m.

| | G2 | P11 |
|---|---|---|
| stop wall / sim | **341.1 / 587.7** | **312.1 / 539.5** |
| position | 34.645640/-116.756507 | 34.645077/-116.755297 |
| sim-reported altitude | 1,645.8 m | 1,575.5 m |
| along-leg | 2,875.4 m | 2,836.3 m |
| track length driven | 3,068.0 m | 2,840.4 m |
| distance between the two stops | - | **127.9 m** |
| distance short of the pre-flight's worst 40 m window (centre along 3,319.6 m, 34.64230/-116.75936) | 444 m | 483 m |
| sim/wall ratio at the stop | 1.68x | 1.85x |

The G3 freeze of the same unit (34.64237/-116.75907, FINDING sec 7 B1) is **28 m** from that
window centre; neither of these two stops is on it.

### 1b. The post-stop "limit cycle" in G2 is mostly a capture artefact - NEW, and it bites
After wall ~1,000 the G2 leader's reported along-leg position sweeps **2,583.9 .. 3,012.7 m**, a
429 m sawtooth, and the altitude residual against the streamed terrain runs from **-139 m**
(wall 3,748.7: alt 1,456.0 where the terrain is 1,595.3) to **+53.7 m** (wall 4,348.4: alt
1,728.7 where the terrain is 1,675.0), returning to residual -0.03 m at the bottom of every
sweep. That is dead reckoning between state updates: G2's sim collapsed to 0.02x after wall
~628, so each extrapolation runs for minutes of wall time and the observer draws a ramp that
snaps back when the next update lands. M3 4 shows the identical sawtooth (2,581.4 .. 3,010.0),
which is part of why the pair's separation looks so rigid in the tail.

Consequences: (i) the "late creep to 3,013 m at alt 1,728.7" is not motion - the leader's true
resting point is along ~2,864 m, alt 1,637.9 m, on the terrain; (ii) **P11 is clean** - its
residual never leaves -0.13..+0.16 m and its post-stop along spread after wall 1,000 is 2.5 m,
so P11's small oscillation is real; (iii) any future reading of an "excursion decaying to a
limit cycle" (FINDING sec 7, sec 7c) must check the altitude residual first, because a
collapsed-ratio capture manufactures that shape. G2's pre-collapse readings (wall < 628) are
unaffected: residuals there are within 0.1 m.

---

## 2. QUESTION 1 - the terrain under BOTH actual tracks

Sampled: the leader's OWN track from along-leg 2,000 m to its stop, plus 300 m of straight
continuation from the stop toward V1 (the remaining leg direction - "beyond the stop" has no
track to sample). 8 m steps, bilinear, soil per sample. Full profiles in `profile_g2.csv`,
`profile_p11.csv` (columns s_m, s_rel_stop_m, lat, lon, z_tile_m, soil, factor).

### G2 - 896.2 m of track + 300.0 m of continuation at bearing 226.5 deg, 151 samples
| window | grade | run | s rel. stop | z | soil | limit | ratio |
|---|---|---|---|---|---|---|---|
| AT the stop, looking AHEAD | **+0.372** | 40.0 m | +7.8 .. +47.8 | 1649.7 -> 1664.6 | sand | 0.752 | **0.495** |
| AT the stop, last 40 m DRIVEN | +0.387 | 40.0 m | -40.2 .. -0.2 | 1630.1 -> 1645.6 | sand | 0.752 | 0.515 |
| worst 40 m in the last 300 m before the stop | +0.404 | 40.0 m | -176.2 .. -136.2 | 1596.4 -> 1612.6 | sand | 0.752 | 0.537 |
| worst 40 m in the 300 m beyond the stop | +0.372 | 40.0 m | +7.8 .. +47.8 | 1649.7 -> 1664.6 | sand | 0.752 | 0.495 |
| worst 40 m anywhere in the sampled stretch | +0.537 | 40.0 m | -24.2 .. +15.8 | 1631.7 -> 1653.2 | sand | 0.752 | 0.714 |
| 55 m ahead of the stop | +0.355 | 56.0 m | +7.8 .. +63.8 | 1649.7 -> 1669.6 | sand | 0.752 | 0.472 |
| short windows ahead (5 / 10 / 20 m) | 0.429 / 0.396 / 0.381 | | +7.8 onward | | sand | 0.752 | 0.570 / 0.527 / 0.506 |

Soil at the stop: **sand** (BM_SAND, CA FVEG 15 m class 30 "Sagebrush") -> M1A2 derated limit
**0.94 x 0.80 = 0.752**. What this leader had ALREADY surmounted on its own track (1 m resample,
whole approach): 5 m **0.688**, 10 m 0.687, 20 m 0.624, 40 m **0.527**, 55 m 0.502.

**Reading: the ground ahead of G2's stop is climbable by this vehicle on this metric and on its
own demonstrated performance. Every window ahead is below the limit (max ratio 0.570, on 5 m),
and the 40 m ahead (0.372) is GENTLER than the 40 m it had just driven (0.387) and far gentler
than its own best 40 m of the approach (0.527). It is not a toe. Nothing like 1-35's
seven-posting 0.70-0.95 face (mean 0.858 over 55 m) or 4-27's 0.558-then-0.879 pre-stop pitches.**

sec-7-style native cut along the last-300 m heading (243.5 deg, 7.86 m postings) from the stop:
1645.8 1650.4 1654.4 1658.3 1662.2 1666.1 1669.8 1673.0 1675.7 ->
**+0.590 +0.506 +0.494 +0.500 +0.498 +0.466 +0.411 +0.342** - a long, even ~0.5 ramp, not a face.

### P11 - 811.9 m of track + 300.0 m of continuation at bearing 227.7 deg, 140 samples
| window | grade | run | s rel. stop | z | soil | limit | ratio |
|---|---|---|---|---|---|---|---|
| AT the stop, looking AHEAD | **+0.551** | 40.0 m | +4.1 .. +44.1 | 1578.7 -> 1600.7 | sand | 0.752 | **0.732** |
| AT the stop, last 40 m DRIVEN | **-0.127** | 40.0 m | -43.9 .. -3.9 | 1578.0 -> 1572.9 | sand | 0.752 | -0.169 |
| worst 40 m in the last 300 m before the stop | +0.356 | 40.0 m | -299.9 .. -259.9 | 1574.6 -> 1588.9 | sand | 0.752 | 0.473 |
| worst 40 m in the 300 m beyond the stop | +0.551 | 40.0 m | +4.1 .. +44.1 | 1578.7 -> 1600.7 | sand | 0.752 | 0.732 |
| worst 40 m anywhere in the sampled stretch | +0.640 | 40.0 m | -379.9 .. -339.9 | 1527.7 -> 1553.3 | sand | 0.752 | 0.852 |
| 55 m ahead of the stop | +0.501 | 56.0 m | +4.1 .. +60.1 | 1578.7 -> 1606.7 | sand | 0.752 | 0.666 |
| last 55 m DRIVEN | **-0.192** | 56.0 m | -59.9 .. -3.9 | 1583.7 -> 1572.9 | sand | 0.752 | -0.255 |
| short windows ahead (5 / 10 / 20 m) | **0.774** / 0.701 / 0.630 | | +4.1 onward | | sand | 0.752 | **1.029** / 0.932 / 0.838 |

What THIS leader had already surmounted: 5 m **0.733**, 10 m 0.708, 20 m 0.674, 40 m **0.641**,
55 m **0.618**.

**Reading: P11's stop reproduces FINDING sec 7b exactly - it stopped on ground DESCENDING at
-0.148 over the last 66 m (7b: "-0.14 over 55 m") - and sec 7 row B2's "SUPPORTS on short windows
only" survives only as the weakest possible row: the 5 m ahead is 0.774 (ratio 1.029), but this
very vehicle had already climbed 0.733 over 5 m and 0.618 over 55 m earlier on the same leg, and
the 55 m ahead of the stop (0.501) is GENTLER than that. Not a toe either.**

sec-7-style native cut at P11's stop (227.3 deg, the same heading sec 7 B2 used):
1575.4 1581.6 1587.2 1591.6 1595.3 1598.6 1601.8 1604.7 1607.5 ->
+0.784 +0.708 +0.561 +0.474 +0.424 +0.405 +0.369 +0.355. B2 recorded 0.698 0.456 0.363 from the
last fix rather than this fix; same shape, first posting steeper here, and the difference does
not change the verdict on either reading.

**Answer to Q1: NEITHER stop sits at the toe of a face the way 1-35's and 4-27's leaders' do.
Both are on ground this metric scores as benign for an M1A2 on sand. Soil is identical at both
(sand, 0.80) and identical to the G3 freeze and to 1-1's control crossing - it does not
discriminate.**

---

## 3. QUESTION 2 - the sim's OWN reported altitudes over the last fixes

(Rise/run over each track step between consecutive POS fixes; altitudes validated to 3 cm in
sec A.)

### G2, M1A2 19, last 8 fixes to the stop
| wall | along m | track m | alt m | run m | rise/run |
|---|---|---|---|---|---|
| 326.7 | 2763.4 | 2947.7 | 1617.2 | 28.7 | 0.327 |
| 328.8 | 2799.0 | 2988.5 | 1626.4 | 40.8 | 0.225 |
| 330.9 | 2836.0 | 3028.2 | 1630.2 | 39.7 | 0.096 |
| 332.9 | 2853.0 | 3045.4 | 1632.1 | 17.2 | 0.111 |
| 335.0 | 2853.7 | 3046.1 | 1632.3 | 0.7 | (0.283 - sub-metre step) |
| 337.0 | 2855.5 | 3047.9 | 1632.9 | 1.8 | (0.326 - sub-metre step) |
| 339.1 | 2872.1 | 3064.7 | 1643.5 | 16.8 | 0.630 |
| 341.1 | **2875.4** | **3068.0** | **1645.8** | 3.3 | 0.706 |

Last 79.5 m of track: **+19.4 m, mean +0.244**. The two steep readings are the last 16.8 m and
3.3 m - short pitches this vehicle beat repeatedly on the way up (its own 5 m max was 0.688).

### P11, M1A2 19, last 8 fixes to the stop
| wall | along m | track m | alt m | run m | rise/run |
|---|---|---|---|---|---|
| 297.7 | 2803.5 | 2807.6 | 1573.6 | 5.4 | -0.390 |
| 299.8 | 2809.1 | 2813.2 | 1571.7 | 5.6 | -0.340 |
| 301.8 | 2814.3 | 2818.4 | 1570.5 | 5.2 | -0.232 |
| 303.9 | 2819.3 | 2823.4 | 1570.0 | 5.0 | -0.100 |
| 305.9 | 2823.2 | 2827.4 | 1570.0 | 4.0 | 0.000 |
| 308.0 | 2828.0 | 2832.1 | 1570.9 | 4.7 | 0.190 |
| 310.0 | 2832.2 | 2836.3 | 1572.9 | 4.2 | 0.478 |
| 312.1 | **2836.3** | **2840.4** | **1575.5** | 4.1 | 0.634 |

Last 66.2 m of track: **-9.8 m, mean -0.148**. Note the step lengths: P11's leader was already
down to 2.0-2.7 m/s (4-5 m per 2 s fix) for 15 s before it stopped - it decelerated into the
stop on DESCENDING ground and only met an up-slope in its last three fixes. G2's leader was
still doing 8-20 m per fix two fixes out. Different approach signatures.

---

## 4. QUESTION 3 - the consoles at the stop

Windows as instructed: G2 wall **225.0 - 645.0** (the stop is at 341.1; 645 is ~17 s past the
onset of the sim collapse - see sec 7); P11 wall **192.1 - 612.1**.

### G2 - 24,464 rows, and the leader is not in them
| object | rows in window | rows in the whole run | first wall | last wall |
|---|---|---|---|---|
| **M1A2 19 (leader)** | **0** | **1,964** | 49.2 | **187.2** |
| M1A2 20 | 4,891 | 8,506 | 49.2 | 4,336.6 |
| M3 4 | 4,901 | 8,517 | 49.2 | 4,362.5 |
| HMMWV 7 | 4,974 | 8,589 | 49.2 | 4,312.2 |
| M577A2 4 | 4,866 | 8,437 | 49.2 | 4,312.2 |
| HMMWV 8 | 4,832 | 8,404 | 49.2 | 4,358.8 |
| unit `92bcf9f3` | 0 | 7 | 49.9 | 50.1 |

Classification of every row in the G2 window (classes as READ_4-27_CONSOLE sec C):

| class | rows | which |
|---|---|---|
| planned-path (maintenance branch, goal unchanged) | 6,790 | "Starting sequence node Maybe plan path"; "Starting condition node Goal new or changed?" |
| blocked-check, NEGATIVE | 6,788 | "Starting sequence node Maybe Skirt Blockage"; "Starting condition node Is path blocked?" |
| "Condition false." | 6,805 | the answers to both |
| task status echo | 3,363 | `Status of task "move-along" is "TaskRunning"` |
| **BLOCKED status** | **19** | `Status of task "move-along" is "BlockedByVehicle"` |
| blockage, other | 19 | 9 x "Movement stopped by vehicle; will be blocked at time #"; 9 x "Starting selector node Move on unblocked path or replan"; 1 x "Move to position and heading: Cannot find an unblocked path." |
| nav/mesh planning | 143 | 24 x "New Primary nav area: NavArea-ground-platform MojaveAO20"; 24 x "Leaving Primary nav area: ..."; 9 x "Planned path has # points."; 10 x "Is current point in nav area?"; 10 x "Is destination in nav area?"; 10 x "Calc off road nav path part"; 9 x "Job Calc off road nav path part success"; rest = the same branch's node names |
| formation | 18 | 9 x "Controller ... maneuver-in-formation beginning to process"; 9 x "New task conflicts with existing task: maneuver-in-formation: ..." |
| move-to | 20 | "ground-vehicle-move-to" subtask starts, "Move along route", 1 fail |
| task-status other | 4 | `"TaskNotActive"` |
| other (BT node names, "Condition true.", "All selection actions done", "Stop moving", pivot/arc nodes, ...) | 495 | |

**This is NOT the seven-shape loop.** Differences from 4-27/P11 sec C, all real rows:
- **19 `BlockedByVehicle` status rows and 9 "Movement stopped by vehicle" rows** (4-27's window
  has 0 of each). They belong to M577A2 4, M1A2 20 and HMMWV 7 - never to M3 4, never to the
  leader.
- **143 nav/mesh-planning rows** including 9 replans (`Planned path has N points.`) and 48
  nav-area enter/leave rows. 4-27/P11's window has none of this class.
- **18 formation rows** and a whole re-tasking event inside the window's run-up (sec 5).
- **the leader contributes nothing at all.** Its LAST rows, verbatim:

      <G2>\watchvrf-trace.csv:126976  wall 187.0  lvl 3   Leaving Primary nav area: NavArea-ground-platform MojaveAO20
      <G2>\watchvrf-trace.csv:126991  wall 187.0  lvl 1   Entity not embarked on same object as target [%1]. Ending task Route 54
      <G2>\watchvrf-trace.csv:126993  wall 187.0  lvl 2   %1: Controller %2's subtask has %3 (ID=%4) | 320.097 | base-system.movement.move-along | Failed | 7
      <G2>\watchvrf-trace.csv:127123  wall 187.2  lvl 2   %1: Controller %2's task has %3 (ID=%4) | 320.430 | base-system.movement.maneuver-in-formation | Failed | 0

  (the same four rows in the interface log at `<G2>\vrfc2simapp.log:212237,212267,212271,212531`;
  212531 is the LAST of 1,966 mentions of `67b414ca` in 74.8 MB). Two independent subscriptions
  stop at the same sim instant, so the sim stopped printing - not the observer.

A member cycle at the stop, verbatim, for contrast (M3 4, still tasked, 2.9 m from the leader):

      <G2>\watchvrf-trace.csv:261285  wall 341.0  lvl 3   587.79 15 .       Starting sequence node Maybe plan path
      <G2>\watchvrf-trace.csv:261286  wall 341.0  lvl 3   587.79 15 .          Starting condition node Goal new or changed?
      <G2>\watchvrf-trace.csv:261287  wall 341.0  lvl 3   587.79 15 .          Condition false.
      <G2>\watchvrf-trace.csv:261288  wall 341.0  lvl 3   587.79 15 .                Starting sequence node Maybe Skirt Blockage
      <G2>\watchvrf-trace.csv:261289  wall 341.0  lvl 3   587.79 15 .                   Starting condition node Is path blocked?
      <G2>\watchvrf-trace.csv:261290  wall 341.0  lvl 3   Status of task "move-along" is "TaskRunning"
      <G2>\watchvrf-trace.csv:261291  wall 341.0  lvl 3   587.79 15 .                   Condition false.

M3 4 stays in exactly those six distinct strings from wall 645 to the end of the capture (973
rows, 139 cycles, last row `<G2>\watchvrf-trace.csv:1172475` wall 4,362.5 sim 1,224.32) while
physically jammed against a dead tank for 4,200 s. **The vendor never reports that blockage.**

### P11 - the seven-shape loop, exactly
| object | rows in window | rows in the whole run | first wall | last wall |
|---|---|---|---|---|
| M1A2 19 (leader) | 5,250 | 43,101 | 63.2 | 4,327.0 |
| M1A2 20 | 5,285 | 43,140 | 63.2 | 4,326.8 |
| M3 4 | 5,271 | 43,052 | 63.2 | 4,326.5 |
| HMMWV 7 | 5,278 | 43,052 | 63.2 | 4,326.6 |
| M577A2 4 | 5,293 | 43,018 | 63.2 | 4,326.5 |
| HMMWV 8 | 3,664 | 5,606 | 63.2 | **452.9** |

**P11's leader: 5,250 rows in the window in SIX distinct strings, and 39,613 rows / 5,659 cycles
from the stop to the end of the capture, still six distinct strings** - 1,500 "Condition false.",
750 each of the other four nodes, 750 `TaskRunning`. Identical to 4-27's leader (sec C). Its
cycle at the stop:

      <P11>\watchvrf-trace.csv:239273-239284  wall 312.2  sim 539.49
        (Maybe plan path / Goal new or changed? / Condition false. / Maybe Skirt Blockage /
         Is path blocked? / Status of task "move-along" is "TaskRunning" / Condition false.)

Whole-window classes for the six P11 objects: planned-path 8,470; blocked-check 8,470;
"Condition false." 8,484; TaskRunning 4,149; **BlockedByVehicle 74** and 11 "Movement stopped by
vehicle" (all in members, none in the leader); nav/feature planning 40; move-to 13; formation 1
(`maneuver-in-formation ... Completed`); other 314. Zero of any of that for the leader.

HMMWV 8's console dies at wall 452.9 when its own task ENDS -
`<P11>\watchvrf-trace.csv:368016` `... 795.259 | base-system.movement.maneuver-in-formation |
Completed | 2` - and it is never tasked again (`<P11>\vrfc2simapp.log:612869` is its last
mention). **A level-3 entity with no task prints nothing, and does not move.** That is the
control for reading G2's leader silence.

---

## 5. What actually happened to G2's leader (the event the window is built around)

Sim clock in the third column; rows from `<G2>\watchvrf-trace.csv` unless marked.

| wall | sim | row | event |
|---|---|---|---|
| 50.3 | 55.233 | `:1197` | M1A2 19 begins `maneuver-in-formation` (ID=0), `unitRoute=M1A2 19's Offset Route`, leader of the other five |
| 51.3 | 56.40 | `:2100-2121` | plans its formation-slot hop: "Is current point in nav area? -> **Condition true**", "Is destination in nav area? -> **Condition true**", `CreateOffRoadSegment`, `Calc off road nav path part` |
| 53.2 | 57.43 | `:4260` | "**Planned path has 6 points.**" (the slot hop, not the leg) |
| 66.5 | 83.73 | `:19955-19964` | the LEG plan: same mesh branch, both nav-area conditions **true** |
| 67.4 | 84.73 | `:20704-20705` | "Job Calc off road nav path part success; sub. M1A2 19" -> "**Planned path has 1109 points.**" |
| 122-187 | 214-320 | POS | the whole unit degrades to ~1.9-2.0 m/s (all six, 60 s bins). **M3 4 closes from 49.8 m (wall 60) to 23.5 m (100) to 4.1 m (140) and never leaves** |
| 187.0 | 320.097 | `:126976,126991,126993` | "Leaving Primary nav area: ... MojaveAO20"; "**Entity not embarked on same object as target [%1]. Ending task Route 54**"; move-along subtask **Failed** |
| 187.2 | 320.397 | `:127111-127115` | the five OTHERS are re-tasked: `leader=M1A2 20`, `M1A2 20's Offset Route; leader=M1A2 20; M577A2 4, HMMWV 7, M3 4, M1A2 19, HMMWV 8` (`<G2>\vrfc2simapp.log:212517-212525`) |
| 187.2 | 320.430 | `:127123` | M1A2 19's `maneuver-in-formation` task **Failed**. **It is never tasked again** and never prints again |
| 187-341 | 320-588 | POS | M1A2 19 nevertheless advances from along 1,067.7 m to 2,875.4 m (+1,807.7 along-leg, 1,994 m of track), climbing 1,233.0 -> 1,645.8 m, terrain residual ~0 throughout |
| 341.1 | 587.7 | POS | net progress ends |

**The 2.96 m lock.** Separation M1A2 19 <-> M3 4, every fix from wall 140 to the end of the
capture: **n = 2,085, mean 2.96 m, sd 0.10 m, min 1.85, max 4.07**. An M1A2 is 9.8 m long and an
M3 CFV about 6.5 m: centre-to-centre 2.96 m is contact/interpenetration, held for 4,200 s. During
the post-failure advance (wall 187.2-341.1) M3 4's bearing from M1A2 19, relative to M1A2 19's
own direction of travel, is **median 147 deg, and |relative bearing| > 120 deg in 61 of 67
moving samples** - M3 4 is astern, on the push side, the whole way.

So the sequence is: the leader's task fails; VR-Forces re-forms the formation under M1A2 20 and
leaves M1A2 19 without a task; M3 4, re-tasked on the unit route and already jammed into the
leader's stern, drives on and shoves the now task-less hull 1,994 m up the ridge; the pair jams
at along 2,875 m and stays there for the rest of the run while M3 4's console reports
goal-unchanged, not-blocked, TaskRunning, once a second, for ever.

---

## 6. QUESTION 4 - the five followers

Along-leg metres / sim altitude. "+300 s" = wall stop+300 (G2 641.1, P11 612.1). "d2ldr" =
distance to the leader at that instant.

### G2 (stop wall 341.1)
| member | along @ stop | alt @ stop | d2ldr | along @ +300 s | alt @ +300 s | d2ldr | max along reached | final along |
|---|---|---|---|---|---|---|---|---|
| **M1A2 19** (task-less) | 2,875.4 | 1,645.8 | 0.0 | 2,864.4 | 1,638.1 | 0.0 | ~2,881 (real) | 2,881.0 |
| **M3 4** (pushing) | 2,872.7 | 1,644.6 | **2.9** | 2,861.7 | 1,637.2 | **3.0** | ~2,878 (real) | 2,878.3 |
| M1A2 20 (new leader) | 3,075.7 | 1,529.6 | 325.2 | 5,776.3 | 1,450.0 | 2,912.4 | **6,351.2** | 6,288.8 |
| HMMWV 7 | 3,054.3 | 1,518.6 | 329.7 | 5,741.1 | 1,464.9 | 2,877.2 | **6,376.1** | 6,328.9 |
| M577A2 4 | 3,029.1 | 1,509.3 | 327.7 | 5,252.9 | 1,666.5 | 2,392.9 | **6,485.9** | 6,277.4 |
| HMMWV 8 | 2,939.8 | 1,674.7 | 65.6 | 5,350.8 | 1,592.2 | 2,487.2 | **6,437.5** | 6,412.1 |

**G2's unit did not stop.** Four of six drove another 3.4-3.6 km past the stop point. Only the
task-less hull and the vehicle wedged against it stayed. (The `max along` for M1A2 19 / M3 4 is
given as the real value; their raw maxima of 3,012.7 / 3,010.0 are the DR artefact of sec 1b.)

### P11 (stop wall 312.1)
| member | along @ stop | alt @ stop | d2ldr | along @ +300 s | alt @ +300 s | d2ldr | final along | final alt |
|---|---|---|---|---|---|---|---|---|
| **M1A2 19** (leader, frozen) | 2,836.3 | 1,575.5 | 0.0 | 2,836.8 | 1,575.9 | 0.0 | 2,837.3 | 1,576.2 |
| M1A2 20 | 2,836.5 | 1,563.8 | 40.7 | 2,955.7 | 1,595.3 | 125.7 | **3,187.3** | 1,576.2 |
| M3 4 | 2,836.3 | 1,604.6 | 61.2 | 2,894.0 | 1,626.4 | 84.0 | 2,893.2 | 1,626.0 |
| HMMWV 7 | 2,796.5 | 1,584.7 | 42.3 | 2,869.5 | 1,600.8 | 36.1 | **3,299.2** | 1,616.6 |
| M577A2 4 | 2,464.9 | 1,532.8 | 371.4 | 2,462.0 | 1,530.8 | 374.8 | 2,461.4 | 1,530.5 |
| HMMWV 8 | 2,776.4 | 1,583.3 | 59.9 | 2,830.7 | 1,572.0 | 6.1 | 2,830.7 | 1,572.0 |

**P11 reproduces the 4-27 pattern of sec 7c**: the LEADER froze (along spread 2.5 m over the last
3,300 s); two followers crawled 351 m and 463 m PAST it; one converged to 6.1 m and stopped when
its own task Completed (HMMWV 8, wall 452.9); one (M577A2 4) was stuck 371 m BEHIND and never
moved again. "1-6 froze" is, again, true of the leader only.

---

## 7. QUESTION 5 - was the mesh path actually FOLLOWED, and did the leg use roads?

**Roads: no, in both runs, by the interface's own instruction.** The unit is set to ignore-roads
at level 4 - `<G2>\vrfc2simapp.log:1859` "VRF console [4] 1-6/2/1_AD~PXY ...: Setting navigation
preference to ignore-roads" (trace `:1069`) - and every member's own tree confirms it:
"Starting condition node Is road following disabled? -> Condition true" in both runs. There is no
"Prefer roads" row and no road-following branch anywhere in 1-6's rows in either run.

**Which planner ran - exhaustive, from the rows:**

| | G2 (MojaveAO20 loaded) | P11 (no nav area) |
|---|---|---|
| "Is current point in nav area?" evaluations | 30 | 14 |
| its outcome | **Condition true, 30 of 30** | **Condition false, 14 of 14** |
| "New / Leaving Primary nav area: NavArea-ground-platform MojaveAO20" | **117** | 0 |
| "Not using roads for move planning." (the feature-planner row) | **0** | **14** |
| the plan row | "Planned path has **N points**" (6, 12, 12, 1065, 1072, 1075, then 1109, 1050, 1081, ...) | "Planned path has **N parts**" (1) |

Verbatim, side by side (each `Condition` line answers the line above it):

    G2   <G2>\watchvrf-trace.csv:19957-19964  wall 66.5  sim 83.73  M1A2 19
         Starting condition node Is road following disabled? / Condition true.
         Starting condition node Is current point in nav area? / Condition true.
         Starting condition node Is destination in nav area?  / Condition true.
         Starting task node CreateOffRoadSegment
         Starting job node Calc off road nav path part
         :20704-20705  wall 67.4   Job Calc off road nav path part success; sub. M1A2 19
                                   Planned path has 1109 points.

    P11  <P11>\watchvrf-trace.csv:2536-2558  wall 67.3  sim 54.07  M1A2 19
         Starting condition node Is road following disabled? / Condition true.
         Starting condition node Is current point in nav area? / Condition false.
         Starting sequence node Plan off feature path
         Starting job node Plan path
         Not using roads for move planning.
         :4703-4704    wall 68.0   Job Plan path success; sub. M1A2 19
                                   Planned path has 1 parts.

So the mesh planner definitely RAN for G2's leader and produced a 1,109-point path over an
8.8 km leg (about 7.9 m per point - consistent with a real nav-mesh polyline).

**Did the track show it? Split at the task failure (wall 187.2), because after that the leader is
not steering:**

| segment | track len | chord | path/chord | total turning | deg/100 m | 25 m segments turning >10 deg | cross-track vs start->V1 |
|---|---|---|---|---|---|---|---|
| **G2, under the mesh path (along 30-1,068 m)** | 1,031 m | 1,029 m | **1.002** | 18 deg | **1.73** | 0 of 41 | **-34.9 .. -16.8 m** |
| G2, after the task failure (along 1,068-2,875 m) | 1,994 m | 1,812 m | 1.100 | 545 deg | 27.31 | 20 of 79 | -122.8 .. +97.3 m |
| G2, whole approach to the stop | 3,026 m | 2,839 m | 1.066 | 657 deg | 21.70 | 20 of 121 | -122.8 .. +97.3 m |
| **P11, feature path, whole approach** | 2,794 m | 2,794 m | **1.000** | 8 deg | **0.29** | 0 of 111 | **-23.7 .. -18.7 m** |

**Answer to Q5: the 1,109-point mesh path WAS the leader's plan, and over the 1,031 m it drove
under that plan the resulting track is a straight line - path/chord 1.002, 1.73 deg/100 m, and a
cross-track offset (-35..-17 m) of the same character and magnitude as P11's formation-slot
offset (-24..-19 m). The mesh router, on this leg, laid 1,109 points along what is effectively
the same straight line the feature planner drew.** The curvature and the 123 m deviation the
observation attributes to "the mesh path" live entirely in the 1,994 m the hull was being pushed
with no task and no console.

---

## 8. QUESTION 6 - VERDICT

### The sim ratio caveat first (asked): the stop precedes the collapse
From the console sim stamps in the trace (n = 300-700 rows per band):

| wall | 60 | 100 | 150 | 200 | 250 | 300 | **341** | 400 | 500 | 600 | 628 | 700 | 1000 | 2000 | 4300 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| G2 sim | 70.3 | 153.4 | 250.3 | 343.6 | 433.7 | 519.0 | **587.7** | 687.7 | 853.5 | 1021.3 | 1069.6 | 1102.5 | 1134.9 | 1173.1 | 1223.4 |
| G2 local ratio | - | 2.08 | 1.94 | 1.87 | 1.80 | 1.71 | **1.68** | 1.69 | 1.66 | 1.68 | 1.72 | **0.46** | **0.11** | **0.04** | **0.02** |
| P11 local ratio | - | - | 2.19 | 1.90 | 1.88 | 1.84 | **1.85** | 1.82 | 1.80 | 1.78 | 1.76 | 1.73 | 1.67 | 1.57 | 1.28 |

**CONFIRMED: at the stop (wall 341.1, sim 587.7) G2 was running at 1.68x, against P11's 1.85x at
the same wall time - a 9 % difference, normal. The collapse starts between wall 628 and 700
(1.72x -> 0.46x -> 0.11x), 287 s after the stop.** The stop is not a collapse artefact. What IS a
collapse artefact is everything the leader's POS row appears to do afterwards (sec 1b).

### VERDICT
**G2's 1-6 leader stop is NEITHER a slope stop NOR the sec-7b "second mechanism". It is a third
thing, specific to this run: a formation-leader TASK FAILURE followed by a vehicle-on-vehicle
jam.** The evidence, each item a row or a measurement:

1. The ground at and ahead of the stop is benign for this vehicle: 0.372 over the 40 m ahead
   (ratio 0.495), 0.355 over 55 m, 0.429 over the worst 5 m - all below the derated limit 0.752,
   and all below what this same leader had already climbed on this leg (0.688 / 0.527 over
   5 / 40 m).
2. The leader had no task from sim 320.430 (`:127123`, `maneuver-in-formation ... Failed`) and no
   console row after wall 187.2 in either independent sink.
3. M3 4 was locked to its stern at 2.96 m (sd 0.10 m, n = 2,085 fixes, median relative bearing
   147 deg) from 154 s BEFORE the stop until the end of the capture.
4. The four members that kept a task drove 3.4-3.6 km further, and three of them climbed THROUGH
   the pre-flight's flagged window (sec 9).
5. The interface never learned any of it: M3 4 reported "Goal new or changed? -> false / Is path
   blocked? -> false / TaskRunning" once a second for 4,200 s while pinned against a dead tank.

### Strongest competing hypothesis, and whether the capture can kill it
**"The leader stopped on the slope first, and the task failure and the jam are consequences of
that."** For it: the whole unit degraded to ~1.9 m/s at wall 122-180, i.e. BEFORE the task
failure, and the leader was at along ~1,050-1,083 m and barely moving for ~40 wall s before the
failure row - so something did stall it first.
Against it: that first stall point is at **along 1,068 m, altitude 1,233 m**, where the terrain
is nothing like a face (the leader's own next 1,800 m climbs 413 m at a mean 0.21 with no
window near the limit), so the first stall is not a slope stall either; and the eventual stop at
along 2,875 m happened while the hull was un-tasked and being pushed, which is not a mobility
verdict about that ground at all. The message that accompanies the failure - "Entity not embarked
on same object as target [%1]. Ending task Route 54" - points at route/embark bookkeeping, not at
traction.
**Falsifier for my verdict, and whether it exists in the capture:** a level-4 leader row showing
throttle or ordered speed, or a reflected velocity, at wall 180-341 would settle whether the hull
was driving itself or being driven. **It does not exist** - WatchVrf carries position only
(`WatchRunner.cs:14`) and no member was ever put at level 4 (sec A). A second falsifier that DOES
exist and does NOT fire: if this were a slope stop, the four members that continued should also
have stalled on that ridge - they did not, and three of them crossed the worst 40 m of the leg.
**Unexplained, recorded:** (i) what moved the leader between sim 320.43 and the stop - a level-3
entity with no task prints nothing AND does not move (P11's HMMWV 8 is the control), yet this one
moved 1,994 m; the push by M3 4 is the only mechanism in evidence, and the capture contains no
contact/collision row to confirm it (the vendor prints "BlockedByVehicle" for three OTHER members
but never for M3 4 against M1A2 19); (ii) why the whole unit degraded to 1.9 m/s at wall 122-180
in the first place.

### What this leg says - and does not say - about "the mesh router routes around faces"
It says three things, narrowly:
- **The mesh planner really did run on this leg** and produced a 1,109-point path, under
  ignore-roads, with both nav-area conditions true (30 of 30 across the unit). The instrument
  works and the nav area was live.
- **On this leg the mesh path was not a detour.** Over the 1,031 m the leader drove under it the
  track is straight to within 1.002 path/chord. Whatever the 1,109 points encode, they did not
  take this vehicle around anything. A mesh laid over benign ground yields the straight line;
  that is the expected result, not a failure.
- **Three vehicles under the re-formed mesh-planned tasks crossed the flagged window and kept
  going** (sec 9) - the closest thing to a positive result this run offers.

It does NOT say that the router avoids faces the tank cannot climb:
- the leg's flagged window lies **444 m beyond** the leader's stop, so the leader never reached
  the face under mesh planning - on this vehicle the test was not run;
- the three that did cross it were **not on the leader's plan**: they were re-tasked at sim
  320.4 onto their own offset routes after the formation re-formed, so the geometry that took
  them through at 0.41-0.43 instead of 0.744 is a formation offset as much as a routing decision;
- and **G3 is the counter-example already in the record**: the same unit, the same mesh area,
  froze AT that window (34.64237/-116.75907, 28 m from its centre). One run in which some
  vehicles get through and one in which the leader does not is not a confirming test.

**The FINDING's confirming test is still open and this run does not close it.** What would close
it is the test as registered: a unit released from its toe by a route that avoids the face and
then completing its leg, or the same unit driven at the flagged window with nothing else changed.

---

## 9. Bonus row the supervisor will want: who crossed the flagged window in G2

Pre-flight worst 40 m window for 1-6's leg 1 (scratchpad `calib_v2.json`): centre along
**3,319.6 m**, 34.64230/-116.75936, z 1,627.7 m, sustained **0.744**, limit 0.752, **ratio
0.990** - the second of the three calibration flags.

| member | closest approach to the window centre | at wall | alt there | max along reached |
|---|---|---|---|---|
| M1A2 20 | **22.0 m** | 378.0 | 1,617.8 | 6,351.2 |
| HMMWV 7 | **20.8 m** | 382.1 | 1,622.7 | 6,376.1 |
| M577A2 4 | **21.7 m** | 384.2 | 1,623.8 | 6,485.9 |
| HMMWV 8 | 138.6 m | 390.4 | 1,674.8 | 6,437.5 |
| M1A2 19 / M3 4 | 326 m / 329 m (never approached) | - | - | ~2,881 / ~2,878 |

M1A2 20's own climb through that stretch, from its own POS altitudes: along 3,200.2 -> 3,581.5 m
(381 m), alt 1,581.1 -> 1,721.5 m (+140.4 m), **mean 0.37**, steepest 25 m 0.43. It passed 22 m
to the side of the window centre and met 0.41-0.43 instead of the 0.744 the straight-line
pre-flight scores there. **An M1A2 crossed the flagged ground in G2.** That bears directly on
DEMO_READINESS row 20's false-alarm count and deserves a registered follow-up - but it is one
vehicle on one offset line, and it is contradicted by G3's freeze 28 m from the same point.

---

## Verified vs assumed

**VERIFIED (rows or measurements in this document):** the member map and leader identity in both
runs; notify level 3 on all twelve members from their own rows; the mesh vs feature branch,
30 of 30 vs 14 of 14, with the verbatim condition rows; the 1,109-point plan and its wall/sim
stamps; the leader's four terminal rows and the fact that both independent sinks stop there; the
re-tasking of the other five under M1A2 20 at sim 320.397 and the exclusion of M1A2 19; every
POS-derived number (stops, along-leg distances, member positions, separations, bearings, turning,
cross-track deviation); the terrain grades, soils, limits and ratios, from the committed sampler
with its documented control reproduced and a -0.03 / +0.06 m median residual against the sim's
own altitudes; the sim/wall ratios; the dead-reckoning artefact (residual +53.7 / -139 m with
snap-back to residual -0.03 m).

**ASSUMED / NOT ESTABLISHED:** (i) that M3 4 physically pushed M1A2 19 - the 2.96 m lock, the
astern bearing and the joint motion are measured, but no vendor row states a contact, and the
alternative "both were independently commanded along the same line 3 m apart" is not excluded by
the capture, only made implausible by the leader's task failure; (ii) the mechanism of the
leader's console silence (task-less, per the HMMWV 8 control, versus a notify level lost with its
controllers) - not separable here; (iii) the 0.752 limit remains the sec-7 ANALOGY
(`navigationPreferenceDescriptor.h:111-132`, a path-cost formula), not a cited dynamics gate -
note that this document uses it only to say the ground was BELOW it, which is the direction the
analogy is safe in; (iv) the soil hop is the documented-guess table except `sand`, which is the
confirmed row and the only one used here; (v) "the formation leader stands for the unit" fails in
both runs and is not used.

Adversarial review (4 passes, HEAVY): pass 1 caught that the observation's wall-53.2 plan is the
6-point formation-slot hop, not the 1,109-point leg plan (that one is at wall 67.4). Pass 2
caught that the leader's console ends at wall 187.2, which puts ZERO leader rows in the requested
window - so the window had to be reported as an absence, not summarised as "nothing unusual".
Pass 3 caught that the curvature statistic pooled the mesh-driven and the pushed segments and so
answered Q5 backwards; splitting at the task failure reversed the answer. Pass 4 caught that the
"late creep" and most of the post-stop limit cycle are dead-reckoning excursions of a collapsed
capture (residual +53.7 m in the air), which retires that part of the observation and raises a
caveat against the same shape where FINDING sec 7 / 7c rely on it - P11 and the pre-collapse G2
readings are unaffected (residuals within 0.16 m). Symptoms still unexplained after four passes:
what moved the un-tasked hull 1,994 m, and why the unit degraded to 1.9 m/s at wall 122-180.
