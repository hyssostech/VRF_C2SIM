# ANALYSIS: P3 sim-time step profile vs P3R (offline, 2026-09-01)

Question (from the brief): did P3 (runs/20260901T221227Z_run, 5x) experience
larger or more irregular SIM-TIME STEPS than P3R (runs/20260901T230326Z_run,
5x, 28/28 clean), especially in the arrival window sim ~150-220 s, such that
host-frame jitter amplified by the 5x multiplier explains the M1A2 18
non-completion and the 8-14 m follower scatter? Competing hypothesis: the
step profiles are indistinguishable and the P3 deviation has another cause.

Baseline: P2c (runs/20260901T211310Z_run, 1x, clean).

Scope: OFFLINE ONLY. Read existing run artifacts; no VR-Forces launch, no
federation join, C:/MAK read-only (one vendor doc page), frozen oracle repo
untouched, runs/20260901T235823Z_run not opened.

Scripts (Python, utf-8 everywhere):
- tools/analysis/step_profile.py   - vendor-log (wall, sim) pairs, tick
  proxy, clock fit; RPT interval statistics; POS step statistics; plateaus.
- tools/analysis/phase_timing.py   - task events relative to the order in
  sim time; RPT truth approach points for AR Plt 3.
Run: python tools/analysis/step_profile.py . <run> ... and
     python tools/analysis/phase_timing.py . <run>:<mult>:<order_trace_t> ...

## VERDICT (short)

INDISTINGUISHABLE at the resolution the artifacts allow, and UNDETERMINABLE
for the single tick at which M1A2 18 stopped. Every measurable clock/step
statistic of P3 matches P3R (and, in sim units, P2c): tick quantum ~0.033
sim-s in all three runs; 5x clock rate steady to <0.1 wall-s per 10-s bin
with zero stalls; task-phase durations reproducible to ~0.1 sim-s across 1x
and 5x for most entities INCLUDING in P3. The one place jitter could hide -
the individual tick at the M1A2 18 arrival - has no observation in any run:
the vendor log has no sim stamps between the platoon and company completion
clusters, POS is dead-reckoned (km-scale garbage during motion), and RPT
truth points are 61 sim-s apart.

Separately MEASURED (and not what the jitter hypothesis predicts): P3
diverged from P2c/P3R BEFORE the follow phase, in the company
move-into-formation phase, which took 79.7 sim-s vs 71.3 (P2c) and 69.9
(P3R); the whole company started following 8.5-9.8 sim-s later and the
follow phase itself then ran at reproducible durations for most entities.
M1A2 18 stopped 1.33 m (P3R) / 1.45 m (P2c) past its reproducible stop
point on the same track, at the same cruise speed, and never fired
completion.

## Evidence sources: what existed, what was usable

| Source | Exists | Usable for the question | Notes |
|---|---|---|---|
| (a) bin64-vrfSim.log wall+sim stamps | yes | PARTLY | Stamped lines are task-event lines only (~400/run), clustered at task start/completion. Consecutive distinct stamps bound the tick size; no stamps at all in the arrival window in any run. Wall stamps are 1-s resolution. |
| (b) watchvrf-trace.csv POS steps | yes (POS/CON/TSK/RPT only) | NO | RAW records do not exist (RUNBOOK ~378); POS is dead-reckoned and DR-poisoned during motion in ALL runs (per-2-s follower step: P2c mean 7285 m, max 287 km; P3 mean 2374 m, max 21 km; P3R mean 1165 m, max 12.7 km). Only stopped plateaus are truth (post-settle drift 0.000 m; RPT final == POS final to 0.00 m). |
| (b2) watchvrf-trace.csv RPT POSITION | yes | YES (coarse) | 6-decimal truth per entity every ~61.2 sim-s (12.25 wall-s at 5x). Interval regularity is a sim-clock-RATE proxy; blind to a single long tick (see vendor-doc note). |
| (c) vrfc2simapp.log tick/time lines | no | NO | Only "Sim Run() queued (... timeMult=5)" and task-complete lines; no sim time. |
| reports-captured.log | yes | NO | C2SIM PositionReportContent per unit (aggregate), same 61-s cadence; adds nothing over RPT. |
| run-manifest.json stage timings | yes | YES (host-load frame) | Identical stage structure in all three runs; see host-load section. |
| File mtimes across the C2SIM tree, 18:12-18:31 local | yes | WEAK | Only mtimes, no CPU record. |

Vendor doc consulted (read-only): C:/MAK/vrforces5.0.2/doc/help/Content/
Introduction/Concepts/vrf_runFasterThanRealTime.htm. Default clock mode is
Variable-Frame Run-To-Complete: sim time advances by the wall time elapsed
since the last tick (times the multiplier) and "does not provide repeatable
results". INFERRED consequence: a host stall becomes ONE long tick while the
sim-clock RATE stays exactly wall x 5, so a rate proxy (RPT intervals, the
clock fit below) cannot see a single long tick.

## (a) Vendor log: sim stamps, tick proxy, clock fit (MEASURED)

| Run | Stamped lines | Discarded (sim >= 5000, garbled) | Distinct stamps | LS clock slope (sim-s / wall-s) | Residual sd (s) | Tick proxy n (consecutive distinct diffs < 0.06 s) | Tick min / median / max (sim-s) |
|---|---|---|---|---|---|---|---|
| P2c 1x | 403 | 0 | 86 | 1.000 | 0.29 | 33 | 0.032 / 0.033 / 0.045 |
| P3 5x | 400 | 0 | 85 | 5.003 | 1.48 | 25 | 0.030 / 0.034 / 0.056 |
| P3R 5x | 393 | 0 | 77 | 4.978 | 1.42 | 28 | 0.025 / 0.034 / 0.045 |

- Garbled lines: entity-name PREFIXES are frequently interleaved across
  threads (e.g. "M1A2 9M1A2 8::") but the "[Tue Sep  1 HH:MM:SS 2026] <sim>"
  stamp itself parsed on every stamped line; no stamp had to be discarded.
- The residual sd is dominated by the 1-s wall-stamp quantization
  (x5 = up to 5 sim-s), not by clock irregularity.
- Per-tick sim step is ~0.033 sim-s at BOTH 1x and 5x. P3 and P3R are
  indistinguishable. Two-tick spacings (0.062-0.075) appear at 5x as well.
- Cluster rate (wall span >= 3 s): P3 4.45x over sim 72-94 (wall
  18:15:01-06 local, +/-1 s quantization on a 5-s span -> 3.7x..5.5x
  bounds); P3R 4.87x; P2c 1.00-1.01x. Not evidence of a P3 slowdown at
  this resolution.
- Gaps of 0.2-0.6 s between consecutive distinct EVENT stamps occur at 1x
  too (P2c: 0.334, 0.269, 0.203, 0.302, 0.333). They are event spacings,
  not tick lengths. This contradicts the REVIEW_P3 inference that 0.25-0.6
  sim-s gaps appearing "only at 5x" imply 0.7-2.4 m per-frame displacement
  at 5x; the measured tick quantum at 5x is ~0.033 sim-s -> ~0.33 m per tick
  at the 10.0 m/sim-s cruise speed (see RPT speeds below), same as 1x.
- Arrival window coverage: NONE. Last stamp of the platoon-completion
  cluster -> first stamp of the company cluster: P3 155.279 @18:15:18 ->
  214.017 @18:15:29; P3R 161.472 -> 210.377; P2c 134.892 -> 185.271. The
  P3 M1A2 18 stop (trace t 66.3-68.4 -> sim ~+170..+180 after order, i.e.
  sim ~196-206) falls inside this unobserved gap in every run.
- Teardown stamp: P3 2283.150 @18:22:23 local (the "clearing its task" line
  for M1A2 18 is the interface StopIface, not a late completion).

## (b2) RPT interval regularity (MEASURED)

| Run | n intervals | mean (wall-s) | sd | min / max | intervals deviating > 0.5 s | max per-10-s-bin deviation from run mean |
|---|---|---|---|---|---|---|
| P2c 1x | 659 | 61.291 | 0.743 | 59.90 / 62.90 | n/a (1x) | - |
| P3 5x | 1619 | 12.260 | 0.161 | 11.90 / 12.70 | 0 | 0.073 (t=70-80, n=15); arrival bins t=50-60 +0.003, t=60-70 -0.018 |
| P3R 5x | 1627 | 12.255 | 0.161 | 11.90 / 12.80 | 1 (t=323.4, M1A2 22, post-arrival) | <= 0.032 |

Receipt-time resolution is 0.1 s. No clock-rate stall or jump anywhere in
P3, including the arrival window (trace t ~50-70). Caveat as above: this
proxy is blind to a single long tick by construction of the variable-frame
clock.

## (b) POS step statistics (MEASURED but UNUSABLE for the question)

Requested per-run max / p95 / p99 / count > 1.0 m over the approach. The
numbers exist (script output) but every value is dominated by dead-reckoning
error, e.g. follower per-2-s step over the move: P2c max 287 km, P3 max
21 km, P3R max 12.7 km; count > 1.0 m is essentially every sample in every
run. They say nothing about sim-time steps and are not reported as a
statistic. The RAW record type the brief assumed does not exist.

## Task-phase timing in sim time relative to the order (MEASURED)

Order = first "move-along-controller beginning" stamp (P2c sim 6.274,
P3 26.537, P3R 32.697). Offsets in sim-s.

Platoon 1222.MechPlt follow completions (M1A2 2/3/4, unit):
P2c +122.585/+124.889/+126.020/+128.618; P3 +122.679/+124.978/+126.140/
+128.742; P3R +122.735/+125.057/+126.179/+128.775. Agreement to 0.16 sim-s
across 1x and 5x, P3 included.

Company 114.MechCoy move-into-formation phase (order -> follow-in-formation
BEGIN): P2c +71.250, P3 +79.702, P3R +69.898. P3 is 8.5-9.8 sim-s longer.
Where: the HQ section element M1A2 6 reached its formation slot late -
move-to-location done at +64.256 (P2c +54.134, P3R +52.087), stop-moving
+64.824, turn-to-heading done +79.484 (P2c +71.082, P3R +69.674); AR HQ
Sec 1 and the company MIF completed 0.04 s after it. Every other entity
MIF sub-event matches P2c/P3R to ~0.5 sim-s. The company waited on M1A2 6.

Company follow phase durations (FOL-begin -> follow-in-formation completed,
sim-s):

| Entity | P2c | P3 | P3R | P3 delta |
|---|---|---|---|---|
| M1A2 13 | 107.747 | 107.778 | 107.782 | +0.03 |
| M1A2 10 | 108.215 | 108.191 | 108.243 | -0.03 |
| M1A2 16 | 110.485 | 110.367 | 110.467 | -0.11 |
| M1A2 14 | 111.754 | 111.651 | 111.781 | -0.11 |
| M1A2 8/9 | 110.418 | 110.336 | 110.432 | -0.09 |
| M1A2 17 | 111.452 | 113.686 | 111.439 | +2.23 |
| M1A2 12 | 109.450 | 116.895 | 109.445 | +7.45 |
| M1A2 18 | 108.283 | never | 108.277 | n/a |
| M1A2 7 (lead) | 111.919 | 111.900 | 111.920 | -0.02 |
| M1A2 15 (lead) | 111.953 | 111.966 | 111.958 | +0.01 |
| M1A2 11 (lead) | 112.286 | 112.298 | 112.307 | +0.01 |
| M1A2 5 (lead) | 112.620 | 111.512 | 112.654 | -1.11 |

Reading: after the follow phase began, the P3 sim clock and motion were as
reproducible as the other two runs for 9 of 12 listed entities (to 0.1
sim-s, at 5x vs 1x). Pervasive step jitter in the follow phase would have
smeared all of these. The anomalies are confined to specific followers
(M1A2 17, 12, 18) and one leader (M1A2 5).

## M1A2 18 approach comparison (MEASURED where stated)

Cruise speed between consecutive RPT truth points mid-route: 10.0 m/sim-s
in all three runs, all four AR Plt 3 entities (P2c 8.65-10.08 incl.
accel/decel; P3 10.01-10.06; P3R 10.01-10.06). Approach bearing north
(0/360 deg) along lon -116.693660 for M1A2 15 and 18 in all runs.

Last RPT truth point before stop, M1A2 18: P2c d=547.2 m at sim +127; P3
d=257.6 m at sim +154; P3R d=303.5 m at sim +134 (all on-track, E offset
0.0 m). Nothing distinguishes the P3 approach until the last ~26 s, which
no source observes.

Final stop (POS plateau == RPT final):

| Run | M1A2 18 final | vs P2c | settle (trace t) | siblings settle | leader M1A2 15 final |
|---|---|---|---|---|---|
| P2c | 34.650227 -116.693660 | 0 | 211.1 | 213.2 | 34.651067 -116.693660 |
| P3 | 34.650240 -116.693660 | +1.45 m N (past) | 68.4 | 70.4 | 34.651068 -116.693660 |
| P3R | 34.650228 -116.693660 | +0.11 m N | 66.3 | 66.3 | 34.651068 -116.693660 |

- P3 M1A2 18 stopped 1.33 m north of its P3R stop and 1.45 m north of its
  P2c stop, on the same longitude, i.e. PAST the reproducible point along
  the direction of travel. Its leader stopped within 0.1 m of the other
  runs. It stopped one 2-s POS sample (~10 sim-s at 5x) before its siblings
  in P3, as it also did in P2c (2 s at 1x); in P3R all four settled in the
  same sample.
- P3 completed followers of AR Plt 3 also ended off their reproducible
  points: M1A2 16 +10.9 m N, +3.3 m E (and its approach track ran 3.3 m
  west of its final); M1A2 17 -6.3 m S; P3R matches P2c to 0.1 m for both.
  Their follow completion durations nonetheless matched P2c/P3R (M1A2 16 to
  0.11 s; M1A2 17 +2.2 s).
- Post-settle drift 0.000 m over 200-370 samples in every run; no creep.

INFERRED: a 1.3-1.45 m overshoot is ~4 ticks of motion at 0.33 m/tick, not
one long tick; a single 5x-amplified stall large enough to carry a follower
1.4 m past its point would be a 0.14 sim-s tick, four times the measured
quantum, and would have to occur exactly at arrival for one entity while
its three siblings completed on schedule. The measurements do not exclude
it (unobserved window) but do not support it either.

## Host-load check (MEASURED, weak)

- run-manifest.json: identical stage sequence and structure in P2c, P3,
  P3R (RtiProbe, LaunchVrf, WatchVrf-precheck, WatchVrf-trace,
  ListenReports, PushInit, VrfC2SimApp, PushOrder, StopIface, StopVrf).
  P3: trace 22:14:18.7Z, PushOrder 22:14:51.3-22:15:22.0Z, VrfC2SimApp to
  22:22:27Z, StopVrf 22:30:44Z. Arrival window = 22:15:18-22:15:35Z
  (18:15:18-18:15:35 local). Runner stages active then: WatchVrf-trace,
  ListenReports, VrfC2SimApp - the same set active in P3R and P2c at the
  same phase.
- P3 oracle gate counts differ (posLines 114 / realCoordLines 88 /
  appThreads 17 vs 158/132/27 in P2c and P3R) - noted, not explained here.
- Files modified anywhere under the C2SIM repos tree in 18:12-18:31 local,
  excluding the P3 run directory: 8. Five are git objects of the P3 host
  commit 269794f at 18:12:12 (pre-run). Three are another agent worktree
  (.claude/worktrees/agent-a51dbe56992f78330): VrfFacade.h 18:26:11,
  VrfSettings.cs 18:27:58, a VrfBridge Release build object 18:28:22 - a
  native build 11-13 min AFTER the arrival window, while P3 was idle (all
  tasks done by 18:15:31; teardown 18:22:23). Nothing on disk changed
  during 18:14-18:16. No CPU/scheduler record exists, so a transient stall
  cannot be excluded by this evidence; it is only not indicated.
- No VRF_C2SIM commits between 18:12:12 and 18:37:50 local.

## MEASURED vs INFERRED summary

MEASURED: all tables above; tick quantum 0.033 sim-s in all runs; zero RPT
cadence stalls in P3; no vendor-log stamps in the arrival window of any
run; P3 move-into-formation phase 8.5-9.8 sim-s longer, attributable to
the late slot arrival of M1A2 6; follow-phase durations reproducible to
0.1 sim-s for 9/12 entities in P3; M1A2 18 overshoot 1.33/1.45 m; M1A2
16/17 finals 6-11 m off with reproducible completion times; concurrent
native build at 18:26-18:28 local (post-arrival).

INFERRED: variable-frame clock makes the RPT/clock-fit proxies blind to a
single long tick (from vendor doc); overshoot magnitude corresponds to ~4
nominal ticks; the P3 anomaly is entity-specific rather than run-wide
clock jitter.

## Answer to the question

- P3 jittered relative to P3R? NOT SUPPORTED. Every measurable step and
  clock-rate statistic is indistinguishable between P3 and P3R.
- Undeterminable component: the tick length at the instant M1A2 18 reached
  its stop point. No artifact observes it in any run. This is a
  measurement gap, not evidence for jitter.
- What the data do show instead: a run-level divergence that began in the
  move-into-formation phase (M1A2 6 late, company held ~9 sim-s), followed
  by entity-specific follow-phase anomalies (M1A2 12 +7.5 s, M1A2 17
  +2.2 s, M1A2 5 -1.1 s, M1A2 18 never, and 6-11 m final offsets for
  M1A2 16/17) while 9/12 entities were reproducible to 0.1 sim-s. Any
  continuation should look at what differed for M1A2 6 during formation
  assembly and at the follower at-distance termination (h1 in REVIEW_P3)
  rather than at host-frame jitter.

## Limits / dissent notes

- The REVIEW_P3 per-frame-displacement inference (0.7-2.4 m per frame at
  5x) is not supported by the stamps: the 0.25-0.6 s gaps it cites are
  event spacings that also occur at 1x, and the tick quantum measured
  directly is ~0.033 sim-s at 5x. Its h1 (at-distance overshoot without a
  passed-destination test) remains open and is consistent with the
  1.3-1.45 m overshoot measured here.
- To observe tick length at arrival in a future run, an instrument that
  samples the vendor sim clock per tick (or raises the vrfSim.log notify
  level to stamp per-tick output) is required; POS/RPT cannot do it.

## Files

- docs/experiments/ANALYSIS_P3_STEP_PROFILE_2026-09-01.md (this file)
- tools/analysis/step_profile.py, tools/analysis/phase_timing.py
- Raw outputs kept in the session scratchpad (step_profile_out.txt,
  phase_timing_out.txt), not committed.
