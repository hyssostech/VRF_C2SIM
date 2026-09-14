# Supervisor note (Fable, 2026-09-14 00:10Z): derived default for the SIM-clock window = 360 s at 50 m (pooled
# first false-alarm-free window 250 sim s x the wall calibration's 1.41 margin); the instrument reproduced
# stall_replay.py and every documented wall number before the sim sweep was read. The executor's ASSUMED
# item about DtClock is stale: the reader built on feat/sim-clock is DtVrfRemoteController::simTime() (the
# back end's scenario clock); whether it equals the console's printed sim prefix is a LIVE check owed.

# RECAL_SIMSECONDS - the C16 progress watchdog re-calibrated on the SIMULATION clock

2026-09-13, Opus analysis executor. Tier: STANDARD (no new cause claim; the work re-measures an
already-adjudicated calibration on a different clock and is judged by whether it reproduces the
wall-second result before it changes it). READ-ONLY: nothing under the repo was written, no
process was launched, `runs/` was not modified. All scripts and outputs live in the scratchpad
(`recal/extract.py`, `replay.py`, `control.py`, `sweep.py`, `band.py`, `band_wall.py`,
`bounds.py`, `final_numbers.py`).

## 0. What was read, and the runs verified against their preregs

Record read: `tools/analysis/stall_replay.py` and `tools/analysis/sim_ratio.py`;
`docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md` C16 (the block at lines 149-257 of the copy on branch
worktree `agent-afc0b0f7b8d49ef63` - C16 does NOT exist in the main checkout's copy, it is
uncommitted work on that branch); `docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md` secs
7, 7b, 7c; `docs/experiments/PREFLIGHT_CALIBRATION_2026-09-13.md` sec 2;
`docs/experiments/PREREG_NAVDATA_G3_2026-09-13.md`; `docs/experiments/PREREG_EARLYSTOP_G5_2026-09-13.md`.

Run identity, from each run's own `run-manifest.json` (NOT from the doc that names it):

| label | run dir | scenario | order | tasks | RunSecs/WatchSecs | runner exit | matches prereg |
|---|---|---|---|---|---|---|---|
| P11 | runs/20260907T150643Z_run | R9_Mojave_Empty_52 | data/COA-STP1_Order.xml | 42 | 4200 / 4500 | 0 | reference run, no prereg of its own |
| G3 | runs/20260913T174516Z_run | R9_Mojave_Empty_52_Nav | data/COA-STP1_Order.xml | 42 | 1500 / 1800 | 0 | YES (PREREG_NAVDATA_G3 sec 1: nav fixture, cap 1500/1800) |
| G5 | runs/20260913T185936Z_run | R9_Mojave_Empty_52 | scratchpad COA-STP1_Order_PROBE_T1only.xml | 1 | 600 / 800 | 0 | YES (PREREG_EARLYSTOP_G5 sec 1: T1-only probe order, no nav area, cap 600/800) |

One discrepancy, recorded not resolved: PREREG_NAVDATA_G3 sec 1 says "deployed build 80daed6",
while G3's manifest `/host/gitCommit` is 90910342. These are different fields (repo HEAD at launch
vs the deployed exe) and the manifest does not carry the exe's provenance, so the manifest neither
confirms nor contradicts the prereg. P11's gitCommit IS 80daed6.

METHOD, as specified by the review that asked for this. The trace carries both clocks: `POS` rows
are wall-stamped, and `CON` rows carry the object's OWN sim time as the payload's line prefix
(`sim_ratio.py` pairs them). Per run I pooled every (wall, sim) pair from every object, took the
median sim per distinct wall stamp, made the knot sequence non-decreasing, and interpolated
piecewise-linearly. I validated the map by hold-out, re-stamped every POS row to sim time, and
re-ran stall_replay's scoring logic with the window in SIM seconds. Parsing is stall_replay's and
sim_ratio's, transcribed (same regexes, same `decode()` semantics, same app-log joins, same
arrival suppression, same anchor rule).

CONTROL FIRST (the instrument reproduces before any result is interpreted). `control.py` scores
all three runs at the shipped defaults (240 wall s, 50 m) from my extracted intermediates and
prints every column stall_replay prints. The output is identical to
`tools/analysis/stall_replay.py` run directly on the same runs, row for row and column for column:
P11 1-35 @419 s / 43.4 m / 24,340 m short, 1-6 @1824 s / 48.6 m; G3 1-35 @431 / 43.9, 1-6 @742 /
49.5; G5 1-35 @385 / 49.6 / 24,304 m; all seven P11 and G3 true negatives with the same closest
and end-dist values; P11's arrival of 1-1 at 1,619 s; the re-tasked note for 5-20 and B/5-20.
These are also exactly the numbers in the C16 table (doc lines 221-225).

## 1. The wall -> sim map, per run

| run | raw (wall,sim) pairs | distinct wall knots | monotonicity fixes | train / hold-out | median abs err | p95 abs err | max abs err |
|---|---|---|---|---|---|---|---|
| P11 | 2,066,874 | 42,048 | 0 | 37,843 / 4,205 | 0.023 s | 0.070 s | 0.190 s |
| G3 | 884,518 | 15,281 | 0 | 13,753 / 1,528 | 0.028 s | 0.070 s | 0.225 s |
| G5 | 139,650 | 6,208 | 0 | 5,587 / 621 | 0.150 s | 0.340 s | 0.493 s |

Hold-out: 10 % of the deduped knots, chosen with a fixed seed, never the two endpoints (that would
test extrapolation, not interpolation); the map is rebuilt from the other 90 % and asked to predict
the held-out knot's sim value. The gate the brief set was "investigate if the 95th percentile
exceeds 2 s". The worst p95 is 0.34 s, 6x inside the gate, so no investigation was owed. Zero
monotonicity fixes in all three runs is independent evidence the pooled median knots are a
consistent clock and not a scatter.

Second, independent noise measure (the pairing's own floor rather than the interpolation's): at a
single wall stamp several objects usually print, and their sim stamps disagree slightly. Median
spread per shared wall stamp / 95th percentile: P11 0.100 / 0.200 s, G3 0.130 / 0.230 s, G5 0.370 /
0.800 s. Both error scales are two to three orders of magnitude below the 10 s scan grid and the
100-500 s windows, so the map is not a material source of error in anything below.

Coverage and extrapolation:

| run | wall span of the knots | sim span | POS rows outside the knots |
|---|---|---|---|
| P11 | 66.3 .. 4327.3 s | 53.1 .. 6288.1 s | 73 of 144,145 (all BEFORE the first knot) |
| G3 | 47.3 .. 1591.5 s | 67.9 .. 2563.4 s | 72 of 52,167 (all BEFORE the first knot) |
| G5 | 35.4 .. 665.9 s | 37.0 .. 3953.6 s | 0 of 2,163 |

The dispatch anchors sit 1-2 wall s before the first knot in P11 (wall 64-65 vs knot 66.3) and G3
(wall 46-47 vs 47.3), and 0.4 s before it in G5, so each t0 is extrapolated on the first segment's
slope by at most ~4 sim s. Every fire below occurs hundreds of sim seconds later and the scan grid
is 10 s, so this cannot move a verdict.

RATIO OVER TIME, 300 s WALL BINS - the load variation that makes a single-slope conversion
inadmissible:

| run | ratio per 300 s wall bin (sim s per wall s) | min | max | spread |
|---|---|---|---|---|
| P11 | 1.99 1.81 1.70 1.62 1.57 1.56 1.54 1.51 1.37 1.33 1.25 1.25 1.14 1.10 1.14 | 1.10 | 1.99 | 1.81x |
| G3 | 1.80 1.67 1.57 1.57 1.54 1.45 | 1.45 | 1.80 | 1.24x |
| G5 | 6.23 6.20 6.16 | 6.16 | 6.23 | 1.01x |

P11's clock runs at 1.99x in its first bin and 1.10x in its thirteenth: a fixed 240 wall s window
covers 478 sim s early in that run and 264 sim s late - a 1.8x swing WITHIN one run, on top of the
4.3x swing BETWEEN runs. Whole-run least-squares slopes reproduce the documented figures exactly
(P11 1.456x vs 1.46 documented; G3 1.601x vs 1.60; G5 6.208x vs 6.21), which is the check that my
pooled-median knots and stall_replay's stride-37 sample describe the same clock.

## 2. The truth set, explicitly, with its source lines

TRUE POSITIVES (must fire), from the `--expect-fire` arguments of the three calibration
invocations recorded in the C16 doc block (DESIGN_ORBAT_TO_VRF_2026-09-06.md lines 213-220) and in
`stall_replay.py`'s docstring (lines 44-70):
- `1-35/2/1_A~PXY` in P11, G3 and G5. Label evidence: PREFLIGHT_CALIBRATION sec 2 line 72
  ("FROZE", along 1,968 m, advance -2 m over the final 600 s); FINDING sec 1 (identical printed
  shapes in P11, G2, G3); PREREG_EARLYSTOP_G5 sec 4 (P15a HOLDS - 1-35 alone froze at the same
  place, leader net 1,970 m at 34.65607/-116.76144).
- `1-6/2/1_AD~PXY` in P11 and G3. PREFLIGHT_CALIBRATION sec 2 line 73 ("FROZE", along 2,837 m,
  advance +2 m over the final 600 s).

TRUE NEGATIVES (must not fire) - the other seven tasked units, in P11 and in G3:
`1-1/2/1_AD~PXY`, `4-27/2/1_A~PXY`, `40/2/1_AD~PXY`, `5-20/2/1_A~PXY`, `856/HHC~PXY`,
`B/5-20~PXY`, `C/1-35`. This is the set stall_replay scores as true negatives with those same
`--expect-fire` lists; the C16 table records "7 of 9" for both runs (doc lines 221-225: the table's 'true neg' column). G5 has no
true negatives: only 1-35 was tasked (one-task probe order).

THE P11 CRAWLERS THAT FALSE-ALARMED AT 120 s, the units the 240 s wall window exists to suppress
(C16 doc lines 230-234): `4-27/2/1_A~PXY` @1519, `40/2/1_AD~PXY` @3475, `856/HHC~PXY` @2520,
`C/1-35` @3699 - "each of which then covers 203-1,191 m more". My wall sweep reproduces all four
fire times to the second (sec 4).

TWO LABELS THAT ARE NOT MINE TO SETTLE, carried forward exactly as stall_replay carries them so
that window settings stay comparable:
- `4-27/2/1_A` is a TRUE NEGATIVE at unit level even though PREFLIGHT_CALIBRATION sec 2 line 74
  scores it "FROZE". FINDING sec 7c lines 433-435 resolves the apparent conflict: the LEADER froze
  (confirmed slope stop), but "THE UNIT DID NOT FREEZE" - three of five followers walked 1.1-1.25
  km past it and were still creeping at 0.301 / 0.299 / 0.301 m/s (sim) at the end of capture. C16
  stalls a unit only when EVERY readable member is under the threshold, so the unit-level label is
  "not stalled" and the two documents agree.
- `856/HHC~PXY` in G3 is recorded UNDECIDED (C16 doc lines 244-257): it stops 3,448 m short and no
  member moves 50 m after wall 1,456 s, but the POS rows end 135 s later. Scored as a true negative
  here, as stall_replay scores it. Sec 5 shows the recommendation does not depend on this label.

## 3. The sweep in SIM seconds (window 100-500 in 10 s steps; move 30 / 50 / 70 m)

Rule as replayed: net displacement per member between the oldest sample in the window and now;
fire when EVERY member with data is under the threshold; C15 arrival suppression applied first;
scan starts at t0 + max(min-since-dispatch, window) and steps by the check interval. Window,
min-since-dispatch (60) and check (5) are all expressed in SIM seconds for this sweep.

MOVE 50 m - every fire, every window. `[FA x]` = false alarm, `MISS:x` = a true positive the
window fails to catch. Times are SIM seconds.

| window | P11 | G3 | G5 |
|---|---|---|---|
| 100 | 1-35@391 1-6@636 [FA 4-27@2471] [FA 40@4017] [FA 856@2532] [FA C/1-35@5531] | 1-35@428 1-6@583 [FA 856@2363] | 1-35@402 |
| 110 | 1-35@401 1-6@646 [FA 4-27@2481] [FA 40@4242] [FA 856@2542] [FA C/1-35@5546] | 1-35@438 1-6@593 [FA 856@2378] | 1-35@412 |
| 120 | 1-35@411 1-6@656 [FA 4-27@2491] [FA 40@4492] [FA 856@2552] [FA C/1-35@5556] | 1-35@448 1-6@603 [FA 856@2388] | 1-35@422 |
| 130 | 1-35@421 1-6@671 [FA 4-27@2501] [FA 40@4737] [FA 856@2562] [FA C/1-35@5571] | 1-35@458 1-6@613 [FA 856@2398] | 1-35@432 |
| 140 | 1-35@431 1-6@686 [FA 4-27@2511] [FA 40@5047] [FA 856@2572] [FA C/1-35@5586] | 1-35@468 1-6@623 [FA 856@2408] | 1-35@442 |
| 150 | 1-35@441 1-6@696 [FA 4-27@2521] [FA 40@5267] [FA 856@2582] [FA C/1-35@5601] | 1-35@478 1-6@638 [FA 856@2418] | 1-35@452 |
| 160 | 1-35@451 1-6@706 [FA 4-27@2531] [FA 40@5647] [FA 856@2592] [FA C/1-35@5616] | 1-35@488 1-6@648 [FA 856@2428] | 1-35@462 |
| 170 | 1-35@461 1-6@721 [FA 4-27@2541] [FA 40@5927] [FA 856@2602] [FA C/1-35@5626] | 1-35@498 1-6@658 [FA 856@2438] | 1-35@472 |
| 180 | 1-35@471 1-6@731 [FA 4-27@2551] [FA 856@2882] | 1-35@508 1-6@668 [FA 856@2448] | 1-35@482 |
| 190 | 1-35@481 1-6@741 [FA 4-27@2561] | 1-35@518 1-6@678 [FA 856@2458] | 1-35@492 |
| 200 | 1-35@491 1-6@751 [FA 4-27@2571] | 1-35@528 1-6@688 | 1-35@502 |
| 210 | 1-35@501 1-6@761 [FA 4-27@2581] | 1-35@538 1-6@698 | 1-35@512 |
| 220 | 1-35@511 1-6@771 [FA 4-27@2591] | 1-35@548 1-6@718 | 1-35@522 |
| 230 | 1-35@521 1-6@781 [FA 4-27@2616] | 1-35@558 1-6@753 | 1-35@532 |
| 240 | 1-35@531 1-6@791 [FA 4-27@2716] | 1-35@568 1-6@773 | 1-35@542 |
| 250 | 1-35@541 1-6@2576 | 1-35@578 1-6@783 | 1-35@552 |
| 260 | 1-35@551 1-6@2591 | 1-35@588 1-6@793 | 1-35@562 |
| 270 | 1-35@561 1-6@2606 | 1-35@598 1-6@803 | 1-35@572 |
| 280 | 1-35@571 1-6@2621 | 1-35@608 1-6@813 | 1-35@582 |
| 290 | 1-35@581 1-6@2641 | 1-35@618 1-6@1098 | 1-35@592 |
| 300 | 1-35@591 1-6@2661 | 1-35@628 1-6@1113 | 1-35@602 |
| 310 | 1-35@601 1-6@2686 | 1-35@638 1-6@1128 | 1-35@612 |
| 320 | 1-35@611 1-6@2721 | 1-35@648 1-6@1143 | 1-35@622 |
| 330 | 1-35@626 1-6@2976 | 1-35@658 1-6@1153 | 1-35@632 |
| 340 | 1-35@636 1-6@2996 | 1-35@668 1-6@1173 | 1-35@642 |
| 350 | 1-35@646 1-6@3006 | 1-35@678 1-6@1183 | 1-35@652 |
| 360 | 1-35@656 1-6@3016 | 1-35@688 1-6@1203 | 1-35@662 |
| 370 | 1-35@666 1-6@3026 | 1-35@703 1-6@1218 | 1-35@672 |
| 380 | 1-35@676 1-6@3036 | 1-35@713 1-6@1233 | 1-35@682 |
| 390 | 1-35@686 1-6@3046 | 1-35@723 1-6@1243 | 1-35@692 |
| 400 | 1-35@696 1-6@3056 | 1-35@733 1-6@1263 | 1-35@702 |
| 410 | 1-35@706 1-6@3066 | 1-35@743 1-6@1283 | 1-35@712 |
| 420 | 1-35@716 1-6@3071 | 1-35@753 1-6@1298 | 1-35@722 |
| 430 | 1-35@726 1-6@3081 | 1-35@763 1-6@1313 | 1-35@732 |
| 440 | 1-35@736 1-6@3096 | 1-35@773 1-6@1328 | 1-35@742 |
| 450 | 1-35@746 1-6@3106 | 1-35@783 1-6@1348 | 1-35@752 |
| 460 | 1-35@756 1-6@3116 | 1-35@793 1-6@1373 | 1-35@762 |
| 470 | 1-35@766 1-6@3121 | 1-35@803 1-6@1393 | 1-35@772 |
| 480 | 1-35@776 1-6@3131 | 1-35@818 1-6@1413 | 1-35@782 |
| 490 | 1-35@786 1-6@3141 | 1-35@828 1-6@1433 | 1-35@792 |
| 500 | 1-35@796 1-6@3151 | 1-35@858 1-6@1453 | 1-35@802 |

FALSE-ALARM BOUNDARY IN SIM SECONDS (move 50 m):

| run | last window that still false-alarms | first false-alarm-free window | the offending row(s) |
|---|---|---|---|
| P11 | 240 | 250 | 4-27 alone from window 180 up; 40, 856/HHC, C/1-35 drop out at 180-200 |
| G3 | 190 | 200 | 856/HHC alone throughout |
| G5 | none in 100-500 | 100 | (single tasked unit; no true negatives) |
| POOLED | 240 | 250 | |

Both true positives are retained at EVERY window from 100 to 500 sim s in all three runs at 50 m,
so the sim-clock band at 50 m is 250 .. >=500 sim s with no upper ceiling inside the sweep.

THE OTHER TWO THRESHOLDS (same sweep, same runs):

| move | run | last false alarm | first FA-free window | highest window that still catches both TPs | usable band |
|---|---|---|---|---|---|
| 30 m | P11 | 150 | 160 | >=500 | 160..500 |
| 30 m | G3 | 110 | 120 | 330 (1-6 missed from 340) | 120..330 |
| 30 m | G5 | none | 100 | >=500 | 100..500 |
| 30 m | POOLED | 150 | 160 | 330 | 160..330 |
| 50 m | P11 | 240 | 250 | >=500 | 250..500 |
| 50 m | G3 | 190 | 200 | >=500 | 200..500 |
| 50 m | G5 | none | 100 | >=500 | 100..500 |
| 50 m | POOLED | 240 | 250 | >=500 | 250..500 |
| 70 m | P11 | 330 | 340 | >=500 | 340..500 |
| 70 m | G3 | 270 | 280 | >=500 | 280..500 |
| 70 m | G5 | none | 100 | >=500 | 100..500 |
| 70 m | POOLED | 330 | 340 | >=500 | 340..500 |

The boundary scales almost exactly linearly with the threshold (150 / 240 / 330 sim s at 30 / 50 /
70 m), which is what a constant-speed crawl predicts: the tightest true negative's per-window
minimum displacement grows at 0.21-0.23 m/s of SIM time (P11's 4-27: 52 m at 250 s, 71 m at 340 s,
76 m at 360 s, 86 m at 400 s; slope 0.227 m/s). That is the same order as, and just under, the
0.299-0.301 m/s (sim) FINDING sec 7c measured directly on 4-27's three crawling followers - a
window minimum is a transient dip below the steady rate, so the two agree. Reading it on the sim
clock is what makes the number physical: it is a vehicle speed in the simulated world, not an
artefact of how fast the host happened to be running.

## 4. The same sweep in WALL seconds - does my replay reproduce the documented boundary?

YES, exactly. Required before anything in sec 3 is interpreted.

| window | P11 | G3 | G5 |
|---|---|---|---|
| 100 | 1-35@279 1-6@419 [FA 4-27@1499] [FA 40@3040] [FA 856@1540] [FA C/1-35@3674] | 1-35@291 1-6@382 [FA 856@1492] | 1-35@210 |
| 110 | 1-35@289 1-6@429 [FA 4-27@1509] [FA 40@3195] [FA 856@1550] [FA C/1-35@3689] | 1-35@301 1-6@392 [FA 856@1502] | 1-35@255 |
| 120 | 1-35@299 1-6@439 [FA 4-27@1519] [FA 40@3475] [FA 856@2520] [FA C/1-35@3699] | 1-35@311 1-6@402 [FA 856@1512] | 1-35@265 |
| 130 | 1-35@309 1-6@449 [FA 4-27@1529] [FA 40@3545] [FA 856@2675] [FA C/1-35@3714] | 1-35@321 1-6@422 [FA 856@1532] | 1-35@275 |
| 140 | 1-35@319 1-6@1504 [FA 4-27@1539] [FA 40@3760] [FA 856@3210] [FA C/1-35@3729] | 1-35@331 1-6@447 | 1-35@285 |
| 150 | 1-35@329 1-6@1514 [FA 4-27@1569] [FA 40@4005] [FA 856@3660] [FA C/1-35@3774] | 1-35@341 1-6@457 | 1-35@295 |
| 160 | 1-35@339 1-6@1529 [FA 40@4150] [FA 856@4070] [FA C/1-35@4119] | 1-35@351 1-6@467 | 1-35@305 |
| 170 | 1-35@349 1-6@1544 | 1-35@361 1-6@477 | 1-35@315 |
| 180 | 1-35@359 1-6@1564 | 1-35@371 1-6@652 | 1-35@325 |
| 190 | 1-35@369 1-6@1579 | 1-35@381 1-6@667 | 1-35@335 |
| 200 | 1-35@379 1-6@1604 | 1-35@391 1-6@682 | 1-35@345 |
| 210 | 1-35@389 1-6@1629 | 1-35@401 1-6@692 | 1-35@355 |
| 220 | 1-35@399 1-6@1804 | 1-35@411 1-6@712 | 1-35@365 |
| 230 | 1-35@409 1-6@1814 | 1-35@421 1-6@727 | 1-35@375 |
| 240 | 1-35@419 1-6@1824 | 1-35@431 1-6@742 | 1-35@385 |
| 250 | 1-35@429 1-6@1834 | 1-35@441 1-6@757 | 1-35@395 |
| 260 | 1-35@439 1-6@1844 | 1-35@451 1-6@777 | 1-35@405 |
| 270 | 1-35@449 1-6@1854 | 1-35@461 1-6@787 | 1-35@415 |
| 280 | 1-35@459 1-6@1864 | 1-35@471 1-6@802 | 1-35@425 |
| 290 | 1-35@469 1-6@1874 | 1-35@481 1-6@822 | 1-35@435 |
| 300 | 1-35@479 1-6@1884 | 1-35@516 1-6@842 | 1-35@445 |
| 310 | 1-35@489 1-6@1894 | 1-35@546 1-6@862 | 1-35@455 |
| 320 | 1-35@499 1-6@1899 | 1-35@566 1-6@882 | 1-35@465 |
| 330 | 1-35@509 1-6@1914 | 1-35@591 1-6@937 | 1-35@475 |
| 340 | 1-35@519 1-6@1919 | 1-35@611 MISS:1-6 | 1-35@485 |
| 350 | 1-35@529 1-6@1934 | 1-35@631 MISS:1-6 | 1-35@495 |
| 360 | 1-35@539 1-6@1944 | 1-35@656 MISS:1-6 | 1-35@505 |
| 370 | 1-35@549 1-6@1954 | 1-35@686 MISS:1-6 | 1-35@515 |
| 380 | 1-35@559 1-6@1959 | 1-35@721 MISS:1-6 | 1-35@525 |
| 390 | 1-35@569 1-6@1974 | 1-35@786 MISS:1-6 | 1-35@535 |
| 400 | 1-35@579 1-6@1979 | 1-35@851 MISS:1-6 | 1-35@545 |
| 410 | 1-35@589 1-6@1994 | MISS:1-35 MISS:1-6 | 1-35@555 |
| 420 | 1-35@599 1-6@2004 | MISS:1-35 MISS:1-6 | 1-35@565 |
| 430 | 1-35@609 1-6@2014 | MISS:1-35 MISS:1-6 | 1-35@575 |
| 440 | 1-35@619 1-6@2024 | MISS:1-35 MISS:1-6 | 1-35@585 |
| 450 | 1-35@629 1-6@2034 | MISS:1-35 MISS:1-6 | 1-35@595 |
| 460 | 1-35@639 1-6@2039 | MISS:1-35 MISS:1-6 | 1-35@605 |
| 470 | 1-35@649 1-6@2054 | MISS:1-35 MISS:1-6 | 1-35@615 |
| 480 | 1-35@659 1-6@2059 | MISS:1-35 MISS:1-6 | 1-35@625 |
| 490 | 1-35@669 1-6@2074 | MISS:1-35 MISS:1-6 | 1-35@635 |
| 500 | 1-35@679 1-6@2079 | MISS:1-35 MISS:1-6 | 1-35@645 |

- The 120 s quartet, C16 doc line 230-232: documented "4-27 (@1519), 40 (@3475), 856/HHC (@2520)
  and C/1-35 (@3699)"; measured 4-27 @1519, 40 @3475, 856/HHC @2520, C/1-35 @3699. Identical.
- The 160 s triple, C16 doc lines 235-237: documented "window 160 still leaves three false alarms
  in P11 (40 @4150, 856/HHC @4070, C/1-35 @4119)"; measured 40 @4150, 856/HHC @4070, C/1-35 @4119.
  Identical.
- "window 170 leaves none in either run": measured - P11's last false alarm is at 160 and G3's at
  130; both are false-alarm-free from 170 up. Identical.
- The shipped 240 s behaviour: measured fires are exactly the C16 table - P11 1-35 @419 / 43.4 m
  and 1-6 @1824 / 48.6 m, G3 1-35 @431 / 43.9 m and 1-6 @742 / 49.5 m, G5 1-35 @385 / 49.6 m, no
  false alarms in any run.
- The threshold separation at 240 wall s, C16 doc lines 238-243: documented "the tightest true
  negative is 74 m per window (P11's 40, 856/HHC and C/1-35; 4-27 at 78 m)"; measured 856/HHC 74 m,
  C/1-35 74 m, 40 74 m, 4-27 78 m. Documented "MEASURED BAND ... 35 m to 70 m are clean AND still
  catch both frozen units"; measured clean thresholds at 240 wall s = 35, 40, 45, 50, 55, 60, 65,
  70 (20-30 miss G3's 1-6; 75+ false-alarm). Identical.

Nothing failed to reproduce. One fact the wall sweep surfaces that the C16 record does not state:
on the WALL clock the usable band has an UPPER ceiling at 330 s, because from 340 wall s G3's 1-6
is no longer caught (and from 410 neither is G3's 1-35). The shipped 240 s sits at 0.73 of that
ceiling. On the sim clock that ceiling is outside the sweep. This is a property of G3's short
trace (1,591 wall s but 2,563 sim s), not of the rule.

## 5. The margin-preserving default in SIM seconds

The arithmetic the wall default was built on (C16 doc line 237): the last false alarm disappears
between 160 and 170 wall s, and the shipped window is 240 wall s, so the margin is
240 / 170 = 1.4118 (the doc rounds it to 1.41x). The margin is taken over the FIRST FALSE-ALARM-FREE
window, not over the last offending one; I keep that convention and also show the other reading.

Reading A - the documented convention, margin over the first false-alarm-free window:
  pooled first FA-free window = 250 sim s (P11 250; G3 200; G5 <=100)
  250 x 1.4118 = 352.9 sim s
  on the sweep's 10 s grid: 360 sim s (actual margin 360 / 250 = 1.44x); 350 sim s if rounded down
  (margin 1.40x).

Alternative - the largest PER-RUN boundary x 1.41:
  largest per-run first FA-free window = P11's 250 sim s
  250 x 1.4118 = 352.9 sim s -> the SAME 360 sim s.
  The two agree because P11 is the run that sets the pooled boundary; G3 (200) and G5 (<=100) are
  strictly looser. There is no run whose boundary the pooled figure hides.

Reading B - margin over the last window that still false-alarms (240 sim s):
  240 x 1.4118 = 338.8 sim s -> 340 sim s; its margin over the FA-free window is 340 / 250 = 1.36x,
  less than the wall default carries.

RECOMMENDATION: StallWindowSeconds = 360 SIM seconds, StallMoveMeters unchanged at 50 m.
Supporting measurements at that setting:
- threshold band at 360 sim s = 35, 40, 45, 50, 55, 60, 65, 70, 75 m clean across all three runs
  (50 m sits mid-band, as it does at 240 wall s where the band is 35-70 m);
- separation at 360 sim s: tightest true-negative per-window minimum 76 m (P11's 4-27), 100 m
  (856/HHC), 102 m (40), 103 m (C/1-35), against firing per-window maxima of 33.6-49.4 m. The wall
  default's equivalent separation is 74-78 m against 43.4-49.6 m, so the sim default is no tighter;
- 250 sim s would also be false-alarm-free but its band collapses to 25-50 m (4-27's minimum is
  52 m there, 2 m above the threshold) - it buys no margin at all, which is exactly why the wall
  calibration did not ship its own 170.

DETECTION DELAY IMPLIED, window 360 sim s / 50 m (wall equivalents are the run's own piecewise map
inverted at the fire instant, not a fixed ratio):

| run | unit | fire (sim s) | fire (wall s) | since dispatch (sim s) | since dispatch (wall s) | shipped 240-wall-s fire (wall s) |
|---|---|---|---|---|---|---|
| P11 | 1-35 | 656 | 375 | 605 | 311 | 419 |
| P11 | 1-6 | 3016 | 1813 | 2965 | 1749 | 1824 |
| G3 | 1-35 | 688 | 396 | 620 | 350 | 431 |
| G3 | 1-6 | 1203 | 709 | 1135 | 662 | 742 |
| G5 | 1-35 | 662 | 135 | 625 | 100 | 385 |

The 360 sim s window fires EARLIER in wall time than the shipped 240 wall s window in all five true
positives - dramatically so in G5 (135 s against 385 s, 2.9x sooner) and modestly in P11 and G3
(375 vs 419; 1,813 vs 1,824; 396 vs 431; 709 vs 742). That is the sim clock doing its job: the
freezes all happen early, when the sim is running at its fastest (P11's first bin is 1.99x), so a
sim-second window is SHORTER in wall terms exactly where it needs to be.

What the window costs in wall seconds, per run (the same 360 sim s):

| run | at the run's mean ratio | at its slowest 300 s bin | at its fastest 300 s bin |
|---|---|---|---|
| P11 | 246 wall s (1.463x) | 327 wall s (1.10x) | 181 wall s (1.99x) |
| G3 | 223 wall s (1.616x) | 248 wall s (1.45x) | 200 wall s (1.80x) |
| G5 | 58 wall s (6.208x) | 58 wall s (6.16x) | 58 wall s (6.23x) |

For contrast, the shipped 240 WALL s covers 351 sim s in P11 on average (264-478 across its bins),
388 sim s in G3 (348-432), and 1,490 sim s in G5 - the last figure being the one C16 already
flags at doc line 167 ("at G5's 6.21x, ~1,490 sim s"), reproduced here from the map rather than
from a single slope.

## 6. Verified vs assumed

VERIFIED (measured this session, from the runs' own files):
- My replay reproduces `tools/analysis/stall_replay.py` exactly at the shipped defaults on all
  three runs, every column (sec 0).
- The wall-second sweep reproduces every documented calibration number: the 120 s quartet with
  identical fire times, the 160 s triple with identical fire times, the 160/170 boundary, the 240 s
  fires and margins, the 74/78 m true-negative minima, the 35-70 m clean band (sec 4).
- The three wall->sim maps interpolate to a hold-out p95 of 0.07 / 0.07 / 0.34 sim s with zero
  monotonicity violations, and their whole-run slopes reproduce the documented 1.46 / 1.60 / 6.21x
  (sec 1).
- The sim/wall ratio varies by 1.81x WITHIN P11 (1.10x to 1.99x over 300 s bins) - a single-slope
  conversion would have mis-stated P11's window by up to 45 % at the two ends of the run (sec 1).
- The pooled false-alarm boundary on the sim clock is 240 sim s (last offending window) / 250 sim s
  (first clean), at 50 m, set by P11's 4-27 and G3's 856/HHC (sec 3).
- The boundary scales linearly with the move threshold (150 / 240 / 330 sim s at 30 / 50 / 70 m),
  consistent with a constant crawl of 0.227 m/s sim, itself consistent with FINDING sec 7c's
  directly measured 0.299-0.301 m/s (sim) followers (sec 3).
- Run identity: G3 and G5 match their preregs on scenario, order, task count and caps (sec 0).

ASSUMED (stated, not established here):
- That a live sim-clock reader would report the same clock the object console prints. The map is
  built from the console's printed sim prefix, which is the only sim stamp in the capture. C16
  records that no sim-clock reader is exported today and that `DtClock::simTime()`
  (`vlTime.h:47`) is the one-line fix; if the reader returned a different quantity (scenario time
  vs exercise time, say) this calibration would need re-deriving.
- That scanning on a 5 SIM-second grid is an adequate model of the live tick thread, which will
  still wake on wall time. At G5's 6.21x, 5 sim s is 0.8 wall s - finer than the 2 s POS sampling,
  so the replay cannot be finer than the data; at a 1.1x-load moment it is 4.5 wall s. Immaterial
  at the 10 s window grid, but it is a model, not a measurement.
- `StallMinSecondsSinceDispatch` was converted to sim seconds along with the window (60 sim s). It
  never binds in these runs (every window >= 100 s dominates it), so the choice is untested.
- The two contested labels (sec 2). Both cut in the SAFE direction for the recommendation: if
  4-27 (P11) were relabelled a true positive, P11's boundary would drop and 360 sim s would gain
  margin; if 856/HHC (G3) were relabelled too, no false alarm would remain anywhere in 100-500 sim
  s. The recommendation is therefore an upper bound on what the truth set demands, not a value
  that depends on the disputed rows.

DID NOT REPRODUCE: nothing. Every documented figure I checked came back identical.

RESIDUAL RISKS / LIMITS, in the direction that matters:
- One order, one terrain, three runs, two true positives, seven true negatives. The sim-second
  boundary rests on exactly two crawling units (P11's 4-27, G3's 856/HHC); the wall-second one
  rested on four. A different scenario whose crawl floor is slower than 0.23 m/s sim would push
  the boundary up and 360 sim s would not hold.
- G5 contributes no true negatives at all (one tasked unit), so it constrains the upper end of the
  band only, never the lower. Its value here is the extreme ratio (6.21x) that shows the wall
  window failing: 240 wall s = 1,490 sim s there, more than four times the recommended window.
- The G3 wall-clock TP ceiling at 340 s and the G5 one at 470 s (30 m) are trace-length artefacts,
  not rule properties, and must not be read as evidence about long windows.
- Per the C16 record the watchdog is REPORT-ONLY and `Vrf:StallDetection` ships OFF; this
  recalibration changes a number, not that posture.
