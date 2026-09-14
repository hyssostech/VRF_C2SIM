# G7 ATTEMPT 4 - RESULTS (run 20260914T154243Z, 2026-09-14)

Adjudicates PREREG_MESHQUERY_G7_2026-09-14 (predictions P17a-P17d). Tier HEAVY. Harvest is
read-only: runs\20260914T154243Z_run\{vrfc2simapp.log, watchvrf-trace.csv, reports-captured.log,
run-manifest.json, runner-watchdog.log, c2sim-bus.log} plus
runs\launch52\RunScenario-20260914T154242Z.threads.csv (the back-end thread/memory sampler).
No vendor sim log was opened (they carry the process environment in cleartext).

## 0. THE RUN, AND THE CLOCK THE REST OF THIS FILE USES

Fixture R9_Mojave_Empty_52_NavAO (the MojaveCOA terrain copy), init
data\R9_Mojave_Lean_Initialization.xml, order data\PROBE_G7_CrossSector_Order.xml, performer
1222.MechPlt~PXY resolved through the NOLIFEFORM type map to "Tank Platoon (USA)" = 4 x M1A2
(manifest inputs.typeMapFile, typeMapIsRepoMap false). CreationPolicy AtInit, pre-order settle
240 s (manifest inputs.preOrderSettleSecs = 240). Object console 4, member console 4,
PositionReportSeconds 10. -RunSecs 1200 -WatchSecs 2000 -StopWhenComplete. runnerExitCode 0.

The trace's `t` is seconds on WatchVrf's own clock. **trace t = 0 is 2026-09-14T15:45:14.2Z**,
fitted (not assumed) from nine C2SIM position reports of the moving taskee against the trace's
own POS track for the same object: median offset 56,714.2 s, spread 56,714.03..56,714.31 (0.28 s
over 80 s of motion). Two independent checks agree: (a) the trace's stop-file detection at
t = 1,536.8 s against the stop file's own stamp 16:10:50.678Z bounds t=0 to [15:45:13.9,
15:45:15.9]; (b) the interface's 10 s R1 ticks bracket the 1.BdeHQ nav-area row (app log :350,
between R1 line 24 and 25) to (15:49:31.5, 15:49:41.5), and the trace puts it at 15:49:40.2Z.
Every UTC in this file is trace t + 15:45:14.2, +/- ~0.3 s.

Fixed points (UTC):
  15:45:43.3  all 6 units + all 16 members created; 6 unit consoles set to 4   (trace w=29.1)
  15:45:48.8  pre-order settle starts (manifest clocks.preOrderSettleStartUtc)
  15:49:40.2  1.BdeHQ~PXY prints its ONLY "New Primary nav area" row           (w=266.0)
  15:49:48.9  settle ends; PushOrder starts
  15:49:49.6  the 4 member consoles set to 4                                    (w=275.4)
  15:49:49.653 ORDER on the bus (c2sim-bus.log line 1; pushorder.stdout.log)
  15:49:50.4  FIRST member goal row, sim 3438.1                                 (w=276.2)
  15:50:29.3  LAST leg-3 refusal                                                (w=314.9)
  15:51:22.659 TASKCMPLT on the bus (reports-captured.log REPORT #211)
  15:51:27.8  all four tanks stop at their V3 slots; no further motion          (w=373.6)
  16:10:20.4  observation window closes at its 1200 s cap

Engine speed over the measurement window: 855.2 sim s in 96.7 wall s = **8.84x** (console sim
prefixes, w=276.2 sim 3438.099 -> w=372.9 sim 4293.257). Sim time is only observable while
sim-prefixed console rows are being emitted, i.e. w=276.3..372.9; there is no sim stamp for the
settle, so the settle is reported in wall seconds only.

## 1. THE VERDICT TABLE (Q1)

18 goal rows, 36 gate rows, 36 gate SUCCESSES, 0 gate failures, 14 mesh plans, 4 refusals,
4 feature-planner fallbacks, 4 per-part refusals. Counted over the whole app log:
`Node Is current point in nav area?: success` 18, `Node Is destination in nav area?: success` 18,
no `: fail` of either; `Planned nav path has not enough (0) points.` 8; `Planned path has 1
parts.` 4. The only nav area named anywhere in the run is `NavArea-ground-platform MojaveCOA`
(29 occurrences, 0 other names).

Sector index i = floor((lon+117.019)/0.00547222), j = floor((lat-34.318)/0.00452439) (design
sec 2). "seams" is re-derived PER MEMBER from its own POS at the goal row to its own goal, as
|di|+|dj|, not taken from the design. j = 65 throughout; the two-seam residual of design sec 4a
did NOT fire on any member. "gate->out" is the wall delta from the member's own
`Is destination in nav area?: success` row to its outcome row.

| member | k | sub | kind | goal UTC | sim | own leg m | seams | gate cur | gate dest | outcome | gate->out |
|---|---|---|---|---|---|---|---|---|---|---|---|
| M1A2 1 | 1 | 1  | slot     | 15:49:50.4 | 3438.1 |    36.2 | 0  | success | success | mesh 6 points   | 0.1 s |
| M1A2 1 | 2 | 6  | leg 1 V1 | 15:49:52.6 | 3459.3 |   520.0 | 1  | success | success | mesh 59 points  | 0.1 s |
| M1A2 1 | 3 | 8  | V1 slot refresh | 15:50:02.3 | 3547.3 | 90.7 | 0 | success | success | mesh 2 points | 0.2 s |
| M1A2 1 | 4 | 10 | leg 2 V2 | 15:50:02.8 | 3552.3 | 1,906.1 | 4  | success | success | mesh 225 points | 0.1 s |
| M1A2 1 | 5 | 12 | leg 3 V3 | 15:50:25.6 | 3771.1 | 5,013.7 | 10 | success | success | **REFUSED (0)** | 0.1 s |
| M1A2 2 | 1 | 1  | slot     | 15:49:50.4 | 3438.1 |    41.9 | 0  | success | success | mesh 6 points   | 0.1 s |
| M1A2 2 | 2 | 6  | leg 1 V1 | 15:49:54.4 | 3474.3 |   618.1 | 1  | success | success | mesh 63 points  | 0.0 s |
| M1A2 2 | 3 | 11 | leg 2 V2 | 15:50:04.2 | 3564.6 | 1,994.3 | 4  | success | success | mesh 209 points | 0.1 s |
| M1A2 2 | 4 | 13 | leg 3 V3 | 15:50:27.1 | 3784.9 | 4,941.4 | 10 | success | success | **REFUSED (0)** | 0.1 s |
| M1A2 3 | 1 | 1  | slot     | 15:49:50.4 | 3438.1 |    94.2 | 0  | success | success | mesh 11 points  | 0.2 s |
| M1A2 3 | 2 | 6  | leg 1 V1 | 15:49:53.8 | 3469.3 |   521.3 | 1  | success | success | mesh 54 points  | 0.1 s |
| M1A2 3 | 3 | 11 | leg 2 V2 | 15:50:01.4 | 3540.4 | 1,969.5 | 4  | success | success | mesh 209 points | 0.1 s |
| M1A2 3 | 4 | 13 | leg 3 V3 | 15:50:23.6 | 3754.8 | 5,047.8 | 10 | success | success | **REFUSED (0)** | 0.2 s |
| M1A2 4 | 1 | 1  | slot     | 15:49:50.4 | 3438.1 |    93.4 | 0  | success | success | mesh 11 points  | 0.1 s |
| M1A2 4 | 2 | 3  | leg 1 V1 | 15:49:54.7 | 3477.4 |   673.0 | 1  | success | success | mesh 69 points  | 0.1 s |
| M1A2 4 | 3 | 8  | V1 slot refresh | 15:50:04.5 | 3567.8 | 94.6 | 0 | success | success | mesh 2 points | 0.2 s |
| M1A2 4 | 4 | 10 | leg 2 V2 | 15:50:05.0 | 3571.9 | 1,902.3 | 4  | success | success | mesh 226 points | 0.1 s |
| M1A2 4 | 5 | 12 | leg 3 V3 | 15:50:28.6 | 3798.7 | 4,913.9 | 10 | success | success | **REFUSED (0)** | 0.1 s |

Goal-to-vertex pairing is by the logged destination converted back to lat/lon (WGS-84 ecef2geo,
the design's frame.py): every goal lands 25.0-83.9 m from the vertex it is paired with - the
member's formation slot offset - and the sector step is exactly the designed one:
(76,65) for the slot, (76,65)->(75,65) for leg 1, (75,65)->(71,65) for leg 2,
(71,65)->(61,65) for leg 3.

After each refusal, exactly one fallback, in this order and nothing else:
`Planned nav path has not enough (0) points.` (the whole-leg mesh query, one "fail in action
Calc off road nav path part" row per member - 4 in the run) -> `Not using roads for move
planning.` -> `Planned path has 1 parts.` (the FEATURE planner, never a mesh result) ->
`Planned nav path has not enough (0) points.` (the per-part query, call site B). Elapsed
refusal-row to parts-row: 0.1-0.4 s.

**RETRIES: NONE.** Each member issued the leg-3 mesh query exactly ONCE. There is no further
goal row for any member after subtask 12/13 - the maneuver task ran to completion on the
feature planner's single straight part.

**ANSWERS TO THE THREE ASKED DIRECTLY.** Leg 1 (1 seam, 520-673 m actual): MESH-PLANNED, 4 of 4
members, 54/59/63/69 points. Leg 2 (4 seams, 1,902-1,994 m actual): MESH-PLANNED, 4 of 4,
209/209/225/226 points. Leg 3 (10 seams, 4,914-5,048 m actual): REFUSED, 4 of 4, 0 points, on
the first and only attempt.

**THE SUPERVISOR'S 15:51Z LIVE READ - CONFIRMED, with the pairing fixed.** "per member:
~2/6/11-point plans, 54-69-point plans, 209-226-point plans, TWO 'not enough (0) points' and
ONE '1 parts'" is right row for row. The pairing: the 6- and 11-point plans are the ZERO-SEAM
formation-slot move (M1A2 1/2 got 6, M1A2 3/4 got 11); the 2-point plans are a second zero-seam
slot refresh at V1 that only M1A2 1 and M1A2 4 received (the other two went straight from leg 1
to leg 2); 54-69 = leg 1; 209-226 = leg 2; the TWO "not enough (0)" rows per member are the
leg-3 whole-leg query and the per-part query that follows the fallback - one refused leg, not
two; the "1 parts" row is the feature planner, not a mesh result.

**VERTEX REACHED - yes, all of them.** Trace POS, closest approach and final held position:

| object | closest V1 | closest V2 | closest V3 | last motion | final distance to V3 | path driven |
|---|---|---|---|---|---|---|
| M1A2 1 | 25.1 m | 24.1 m | 32.0 m | 15:51:27.8 | 32.0 m | 7,686.4 m |
| M1A2 2 | 25.7 m | 33.8 m | 38.9 m | 15:51:27.8 | 38.9 m | 7,617.5 m |
| M1A2 3 | 37.3 m | 29.7 m | 26.8 m | 15:51:27.8 | 74.0 m | 7,672.1 m |
| M1A2 4 | 33.0 m | 23.3 m | 83.8 m | 15:51:27.8 | 83.8 m | 7,692.3 m |
| 1222.MechPlt~PXY (aggregate) | 33.7 m | 36.7 m | **5.1 m** | 15:51:27.8 | 5.1 m | 7,600.4 m |

Design route 7,583.5 m; the residual is the formation offset, which is also the residual at V1
and V2 while the mesh WAS planning. Nothing moved after 15:51:27.8Z for the remaining 18.7 min
of the window. Second channel, same conclusion: the C2SIM position reports for taskee
001aa71b-4c26-a1ea-28b2-f7dfe8e76342 hold 34.61563797/-116.68299989 (5.1 m from V3) from
15:51:31.8Z to the last report at 16:10:15.4Z.

## 2. PREDICTIONS (Q2)

**P17a (HIGH, gate) - HIT.** 36 of 36 gate rows report success; 0 failures. The area named in
the run is `NavArea-ground-platform MojaveCOA` and no other. One wording correction for the
record: the gate rows do NOT name the area; the naming evidence is the 15 "New Primary nav
area: NavArea-ground-platform MojaveCOA" / 14 "Leaving Primary nav area: ..." rows, which name
MojaveCOA and nothing else.

**P17b (THE MEASUREMENT) - the decision table's FIRST branch fires:** points on leg 1 AND 0 on
leg 3. Per the prereg this reads as: **the graph IS connected across sector seams; reading (B)
as stated ("any query that must leave its start sector returns nothing") is FALSIFIED; the
failure is length/budget-related; next = G7b (abstract-graph SMS) then G8 (gamewareMemorySize
via --appDataDir).** This run is stronger than the branch required, because leg 2 also planned:
connectivity is demonstrated across 4 seams and 1,994 m, not just 1.

What the run brackets, and what it cannot separate:
- MojaveCOA (41 x 54 km, 8,856 sectors) SUCCEEDS at <= 1,994 m / 4 seams and FAILS at
  >= 4,914 m / 10 seams. That replaces G6's bracket (shortest measured failure 9,346.5 m) and
  the prereg's "between 5 and 9.3 km" with **(1,994 m, 4,914 m]**.
- MojaveAO20 (20 x 20 km, 1,600 sectors, the SAME ~502 m sector size) planned 10.1 km / ~20
  seams (G2/G3). So neither 10 seams nor 5 km is intrinsically beyond the planner: the ceiling
  is a property of the AREA, not of the leg. That is exactly reading (A) - a fixed per-query
  working-memory or time budget spent over a 5.5x larger graph - and it is what G8 tests.
- **LENGTH AND SEAM COUNT CANNOT BE SEPARATED ON THIS AREA, BY CONSTRUCTION.** Sectors are
  501.9 m at this latitude, so any leg of L metres crosses ~L/502 seams whatever its bearing.
  A residual (B') - "connectivity degrades beyond some seam distance" - survives this run
  untested. Separating it needs a different sector size (regeneration) or a different area, and
  that is a design constraint on G7b, not a footnote.

**P17c (MEDIUM: leg 3 returns 0) - HIT**, 4 of 4 members.

**P17d (MEDIUM, movement: the vehicle reaches V3) - HIT.** Arrival evidence is the trace POS
table in sec 1 (aggregate 5.1 m from V3, members 26.8-83.8 m at their slots, all stationary from
15:51:27.8Z) and the independent C2SIM position-report stream. No stop, no freeze, no
FINDING_EARLY_STOPS second mechanism on this route - and note that the last 4,989 m were driven
on the FEATURE planner's single straight part at ~9.5 m/s (leg 3: w 309.4-314.4 -> 373.6, ~527
sim s for 4,989 m), against ~10.3 m/s for the mesh-planned leg 2. The pre-flight scored every
leg benign (worst sustained ratio 0.166 of a 0.752 limit), and it was.

**TIMING READING (prereg sec 4) - NOT REPRODUCED; the discriminator does not survive.** Every
one of the 18 outcomes - 14 successes from 2 to 226 points, and all 4 refusals - landed within
**0.0-0.2 s wall** of its own destination gate. MESH_QUERY sec 7 proposed the split "failures
flat and fast, 0.4-1.3 s; MojaveAO20's 10 km successes 2.4-4.2 s". Here the successes are as
fast as the refusals, so a fast refusal is not evidence of a refusal. Two reasons this run
cannot settle it: the trace stamps at 0.1 s resolution, and at 8.84x a 0.1 s wall bucket is
0.88 sim s - coarser than the whole effect. Any future timing claim needs sim-stamped rows on
both sides of the query, or a run at ~1x.

**P17e** was withdrawn before the run (prereg sec 5, attempt 2); see sec 4 for what actually
happened to the reporting channel, which is not what the withdrawal assumed.

## 3. THE LAZY-LOAD TRIGGER (Q3)

### 3.1 The timestamps that decide it

(a) **Creation.** All 6 units and all 16 members were created at **15:45:43.3-43.7Z** - app log
:71-:83 (PLACEMENT, 6 of 6 altitudes from the terrain query, 1222.MechPlt at terrain 1,117.2 m),
console-enable rows at trace w=29.1, first POS sample for every one of the 10 tracked objects at
w=29.5. CreationPolicy AtInit did what it says: members existed 4 minutes before the order.

(b) **Member console request.** App log :408 (and a duplicate at :424), `VRF console level 4
requested for 4 members of 1222.MechPlt~PXY`, executed at trace w=275.4 = **15:49:49.6Z** -
i.e. AT order time, 0.05 s before the order reached the bus. The four members were mute before
that instant.

(c) **First "New Primary nav area" row per object.**

| object | console on since | first "New Primary nav area" | preceded by "Leaving"? |
|---|---|---|---|
| 1.BdeHQ~PXY (PLATFORM) | 15:45:43.3 | **15:49:40.2Z** (w=266.0), app log :350 | NO |
| M1A2 1 | 15:49:49.6 | 15:49:50.7 (w=276.5) | yes |
| M1A2 2 | 15:49:49.6 | 15:49:50.7 (w=276.5) | yes |
| M1A2 3 | 15:49:49.6 | 15:49:50.7 (w=276.5) | yes |
| M1A2 4 | 15:49:49.6 | 15:49:55.1 (w=280.9) | yes |
| 114.MechCoy, 1141/1142/1143.MechPlt (AGGREGATES) | 15:45:43.3 | none, ever | - |

(d) **First "Is current point in nav area?" row:** 15:49:50.5Z (w=276.3), all four members, and
it reads `success`.

### 3.2 Ruling

**H1 - "the area loads on the first PLANNING request" is FALSIFIED, twice over.**
Falsifier 1 (its own): a "New Primary nav area" row on an object that never planned anything.
1.BdeHQ~PXY is a lone platform, was never named in the order, never received a task, printed
zero goal rows and zero gate rows in the entire run - and printed `New Primary nav area:
NavArea-ground-platform MojaveCOA` at 15:49:40.2Z, 9.5 s BEFORE the order reached the bus and
237 s after it was created.
Falsifier 2: under H1 the FIRST planning request cannot already have the area. It did - the
first goal at 15:49:50.4Z passed both gates and returned a 6-point mesh path 0.1 s later. The
control is attempt 3 on the same fixture and the same area with plan-at-creation: 18 of 18
current-point gate FAILURES and the area rows arriving 140 s after the last goal
(PREREG_MESHQUERY_G7 sec 5).

**H2 - "the area loaded earlier but only the members' consoles could print it" SURVIVES, in a
corrected form.** The brief's falsifier for H2 was "the units with consoles ON since init should
have printed rows before the order - did they?" The answer is yes, the one that could did: five
of those six units are AGGREGATES, and the area is a ground-PLATFORM area
(`NavArea-ground-platform MojaveCOA`) - an aggregate is not its client and never prints the row
(0 rows in 25 minutes). The sixth, 1.BdeHQ, is a platform, and it printed at 15:49:40.2Z.

**H3 - "the row is printed only on a CHANGE of primary area" SURVIVES and is complementary, not
alternative.** All 14 member rows are immediately preceded by `Leaving Primary nav area:
NavArea-ground-platform MojaveCOA` for the SAME area - you cannot leave what you never had, so
those are re-registrations on re-plan, not acquisitions. 1.BdeHQ's single row has NO "Leaving"
predecessor: it is a first acquisition. That is the load-completion signal, and it is the only
one in the run.

### 3.3 A second, independent instrument puts the load at ~237 s

runs\launch52\RunScenario-20260914T154242Z.threads.csv samples the back-end process every 5 s:

  15:43:48 - 15:45:38   0.10-0.17 cores, working set FLAT at 2,919 MB   (scenario up, no entities)
  15:45:43              0.85 cores, WS starts climbing                  (entities placed)
  15:45:43 - 15:49:39   1.4-2.1 cores, WS +22 to +31 MB per 5 s sample, monotone
  15:49:39 - 15:49:44   growth stops: +26, +5, +0 MB; WS plateaus at 5,243 MB
  15:49:44 - 16:10:24   WS 5,243 -> 5,302 MB (+59 MB in 20 min), i.e. flat

**2,324 MB allocated over ~236 s, starting at entity placement and stopping within one 5 s
sample of the only first-acquisition nav-area row (15:49:40.2Z).** Read together with
vrfSim.mtl:433-436 (`loadAllNavigationDataOnTerrainLoad` default 0, "navigation data will be
loaded when an entity is placed") and UG52 App. C p1671, the supported reading is: placing an
entity starts an ASYNCHRONOUS load of the sectorised area, it streams for ~4 minutes and ~2.3 GB
on this 8,856-sector area, and it is not usable until it completes. Attempt 3 saw the same
mechanism at ~176 s and read it as a race.

### 3.4 Consequence for the demo flow

The 240 s settle did NOT buy nothing - it bought the run, with about **9 seconds of margin**
(load complete 15:49:40.2, order 15:49:49.7). A settle sized by guesswork against a load whose
duration scales with area size and machine state is not a demo-grade control: one slower disk,
one bigger area, and the first legs are back on the feature planner with no error anyone sees.

The remedy is the one already in preparation: `loadAllNavigationDataOnTerrainLoad = 1` in a
relocated appData (docs\experiments\APPDATA_RELOCATION_2026-09-14.md), which moves the load to
terrain load - before any entity exists and before any order can arrive. **Stated as the
implication of this run, NOT as verified: nothing in this run exercised that setting.** What
would verify it: the same fixture with the flag on, showing the WS growth completed before
PushInit and a "New Primary nav area" row (or a successful first-goal mesh plan) with a zero
settle.

## 4. INSTRUMENT NOTES (Q4)

**4.1 The `VRF console [4] ?` rows.** 28 rows, app log :100-:158, ALL inside a single 0.1 s
window at trace w=29.1 (15:45:43.3Z), never again. The five unresolved ids are exactly the five
AGGREGATES, identifiable from the children in their own weapon-response text:
`1:4276:11-unit` = 114.MechCoy~PXY (children 1141/1142/1143), `1:4276:12-unit` = 1143.MechPlt
(M1A2 5-8), `1:4276:18-unit` = 1141.MechPlt (M1A2 9-12), `1:4276:23-unit` = 1142.MechPlt
(M1A2 13-16), `1:4276:6-unit` = 1222.MechPlt (by elimination; it printed only its notify-level
echo). 1.BdeHQ, a platform, resolved to its marking in the same window. So this is a transient
marking->name resolution gap for aggregates at creation, not a lost object: every one of the
five prints under its proper name from :172 onward.

**4.2 The window ran to its 1200 s cap - and the reason is NOT the marking.** The runner line,
verbatim (scratchpad g7_runner4.log:282):

    [..]     -StopWhenComplete did NOT fire; window ran to its 1200s cap. Taskees without
    TASKCMPLT: (none). Report evidence pending: 1222.MechPlt~PXY: no RPT POSITION line for this
    marking yet

CORRECTION TO THE BRIEF, three verified points. (i) The taskee's 16-character marking was NOT
truncated: the trace's own task record reads `TSK,373,"1222.MechPlt~PXY","move-along"` in full,
the runner resolved it (manifest oracle.earlyExit.reportEvidence.completionT = 373.0), and
TASKCMPLT was both logged (app log :172630) and published (bus 15:51:22.659Z). (ii) The taskee
DID produce position reports - 147 ten-second ticks on the C2SIM bus for
001aa71b-4c26-a1ea-28b2-f7dfe8e76342, tracking it from the start point through the whole route
to V3; the app log says `R1 position reports: 6 sent, 0 skipped` on 147 of its 148 R1 lines (the first, logged
before any object existed, reads `0 sent`). (iii) The
gate that never closed is runner condition (4), which needs an `RPT` line in the TRACE - a
VR-Forces radio TEXT report (tools\WatchVrf\ConFormat.cs:83-96, one record per
VrfBridge.TextReport), a different channel from both the C2SIM position report and TSK. **This
trace carries ZERO RPT rows, and so do the four prior traces checked (20260914T002716Z,
20260914T122525Z, 20260914T130439Z, 20260907T170643Z: RPT=0 in all four).** Condition (4) is
therefore unsatisfiable as written in this configuration, for any taskee and any marking width:
-StopWhenComplete cannot fire. That is a runner defect to raise, and it is cheap - the evidence
it wants already exists on the C2SIM bus.

Related correction: "TASKCMPLT at t+77 s" is the runner's OBSERVATION-WINDOW clock (window
opens 15:50:19.7Z at PushOrder's return). From the order reaching the bus the interface
published TASKCMPLT at **+93.0 s**; and it published it at 15:51:22.659Z, 4.5 s BEFORE the sim's
own controllers reported the task complete (w=373.0 = 15:51:27.2Z), which is consistent with the
interface's arrival rule being position-based rather than controller-driven. Not a defect;
recorded so the two clocks are not mixed again.

**4.3 The watchdog's first live use - clean.** runner-watchdog.log: armed 15:43:48.8Z at Stage
3w over runner pid 84196 with budget "poll 5s, max 4235s (70.6 min)"; the runner's own line
`[OK] the watchdog was still alive 750 ms after arming (it did not refuse at validation)`
(g7_runner4.log:134) is the survival check, and its pointer line is `[OK] teardown watchdog pid
58980 armed (pid also in ...\watchdog.pid)` (:133). At 16:11:01.406Z it read the pid GONE,
waited, and confirmed at 16:11:03.420Z - the two-observation rule (review of 374ea49, finding
F2) doing exactly its job - then found runner.teardown-ran and **stood down without touching
anything** (16:11:05.431Z). Nothing was force-killed; rtiexec 69856 and rtiForwarder 50520 were
alive before and after (manifest preflight.rtiInfra / postRunRti).

**4.4 The two WARNs.** manifest validityFlags: `simulator log vrfSim.log not found at
C:\MAK\logs\vrfSim.log` and the same for vrfGui.log, both at 16:10:56Z. Pre-existing harvest
naming; noted only, and those files are never to be attached anywhere regardless.

**4.5 Two `Failed to deserialize xml to type C2SIM.Schema102.MessageBodyType: There is an error
in XML document (1, 2).`** App log :37 (init push) and :404 (order receipt). Both are followed
immediately by correct handling - `ORDER: 1 task(s) for 1 taskee(s); verbs [MOVE].` at :405 -
so nothing was lost. Unexplained on this evidence; recorded, not explained away.

**4.6 The concurrent C:\MAK filesystem scan (~15:43-15:45Z): no measurable effect, and it was
over before anything was measured.** Assessed against this run's own instruments, not by
assertion. The scan window sits 4.5 minutes before the first mesh query (15:49:50.4Z) and ~5
minutes before the last (15:50:29.3Z). In the scan window the back-end held 0.10-0.17 cores with
a flat 2,919 MB working set - it had no entities and no nav data to load, so there was nothing
for the scan to slow down. Across the measurement window the back-end ran 1.4-2.5 cores with no
dip, the observer's POS cadence held 2.00-2.10 s (its nominal 2 s, no outlier anywhere in the run), the interface's R1 ticks held 10 s
(15:45:50.7 to 16:10:15.4, 147 ticks), and the engine sustained 8.84x. The G3 correction is
still the standing rule (never run agents during a timed run) and it was honoured for the
measurement itself; the exposure here is confined to the pre-init stages and shows nothing.
One residual I cannot rule out from inside the run: if the scan competed for disk while the
nav-area stream was ramping, it could have LENGTHENED the ~236 s load - which would make the
9 s of settle margin a measurement of a degraded case, not the best case. That direction is
conservative for the sec 3.4 conclusion.

## 5. ADVERSARIAL REVIEW (HEAVY)

**CR1 - "the refused goal was not leg 3 at all, but a re-plan or another slot move."** REFUTED.
Each refused goal is the LAST subtask of its member's maneuver task (12, 13, 13, 12); its
destination converts to 26.8-83.9 m from V3; the member's own position at the goal row is
4,913.9-5,047.8 m away in sector (71,65) against a goal in (61,65), i.e. 10 seams; and the
member then drove that distance and stopped there. The zero-seam slot moves are in the same
table and they planned (6, 11, 2 points) - the run contains its own control for "was this just
a slot move".

**CR2 - "a transient budget hit, not a ceiling."** NOT EXCLUDED, and this is the honest weak
point: each member queried leg 3 exactly once, so the run has zero evidence about repeatability
of that specific query. What it does have: 4 independent queries, 4 different start points and
4 different destinations, spread over 5 wall seconds, refused 4 out of 4 - against 14 out of 14
successes at <= 1,994 m. A transient that hits every long query and no short one is not much of
a transient. Falsifier for my reading: re-issue the SAME leg-3 goal later in the same run (or in
a second order) and get points back. That was never issued, so G7b must issue it - a re-task of
the identical leg after arrival is one order and no new fixture.

**CR3 - "the feature planner drove the leg fine, so the refusal is cosmetic."** TRUE FOR THIS
ROUTE, FALSE FOR THE REASON THE MESH EXISTS, and it is the caveat that matters most. The tanks
covered the refused 4,989 m at ~9.5 m/s and arrived - on a route the pre-flight scored benign on
every leg (worst sustained ratio 0.166 against a 0.752 limit). Of course a straight line works
where a straight line works. The mesh was introduced for FINDING_EARLY_STOPS_2026-09-13, where
the straight leg crosses ground the vehicle cannot take; what this run shows is that on
MojaveCOA every leg longer than ~2-5 km is SILENTLY back on the straight-line planner, with the
only trace a level-2 console line nobody reads at runtime. Falsifier for that reading: a leg
above 5 km on MojaveCOA that IS mesh-planned (kills the ceiling), or an early stop on a leg
SHORTER than 2 km where the mesh was demonstrably used (would show the mesh does not prevent the
freeze anyway, and would move the freeze question off this thread entirely).

**CR4 - "the 2.3 GB working-set growth is terrain or imagery paging, not the nav area."** The
strongest competing hypothesis for sec 3.3, and it is not excluded. For the nav-area reading:
the growth starts at the placement instant, stops within one 5 s sample of the run's only
first-acquisition nav-area row, the fixture's only difference from the plain R9 fixture is the
navData record in the terrain .mtf, and the vendor's own default text ties the load to entity
placement. Against it: 22 objects appearing at once also force terrain paging, and I did not
separate the two. Falsifier: run the SAME fixture without the navData record (or the _AG
sibling) and watch the sampler - if 2.3 GB still appears over 236 s, sec 3.3's mechanism is
wrong and only the timing coincidence stands. SUPPORTED, NOT PROVEN.

**CR5 - "the refusal was fast, therefore it was refused rather than computed."** NOT SUPPORTED
by this run and explicitly withdrawn as a discriminator here: the successes were exactly as
fast (sec 2, timing reading).

**VERIFIED vs ASSUMED.**
VERIFIED (a row in this run's own files says it): the 18 goals and their sector steps; 36 gate
successes and 0 failures; 14 mesh plans with their point counts; 4 leg-3 refusals with no
retry; the feature fallback and the per-part refusal; arrival at V3 by two independent channels;
the 1.BdeHQ nav-area row at 15:49:40.2Z with no "Leaving" predecessor; zero nav-area rows from
the five aggregates; the member consoles opening at order time; the working-set curve and its
plateau; zero RPT rows here and in four prior runs; the full 16-character marking in the TSK
record; 147 C2SIM position-report ticks for the taskee; the watchdog's arm/confirm/stand-down.
ASSUMED or INFERRED: trace t=0 = 15:45:14.2Z (fitted to +/-0.3 s, cross-checked twice, but
fitted); the sector grid's SW anchor (moves labels, never counts - design sec 2); that the
working-set growth IS the nav-area load (CR4); that the 236 s load time generalises beyond this
one object on this one machine - 1.BdeHQ sits 10.3 km from the platoon in different sectors, so
strictly this run shows only that the platoon's own sectors were ready by 15:49:50.4Z; that
MojaveAO20's 10.1 km success (G2/G3) is comparable evidence, which it is only if the areas
differ in nothing but extent.

**STILL UNEXPLAINED, listed rather than footnoted:** (1) why M1A2 1 and M1A2 4 received a second
zero-seam slot refresh at V1 and M1A2 2 and M1A2 3 did not; (2) the two deserialize failures
(sec 4.5); (3) whether the RPT channel has ever produced a line in any run - four traces say no,
and nobody has yet identified what is supposed to emit one.

## 6. SOURCES

runs\20260914T154243Z_run\vrfc2simapp.log - :37 and :404 deserialize; :71-:83 PLACEMENT (6 of 6
from the terrain query, 1222.MechPlt terrain 1,117.2 m); :85-:95 unit consoles to 4; :100-:158
the 28 unresolved-name rows; :350 1.BdeHQ `New Primary nav area`; :402 `C2SIM Order received
(2021 bytes)`; :405 `ORDER: 1 task(s) for 1 taskee(s); verbs [MOVE]`; :408 and :424 member
console request; :409/:419/:421 terrain profile 8 (4 vertices, alts 1127.2/1145.4/1120.5/1278.2);
:426 `CreateRoute 'T_G7_XSECT ROUTE' (4 pts)`; :432 MoveAlongRoute issued; :172630 TASKCMPLT.
Goal / gate / outcome rows, by member and subtask: slot :490/:740/:998/:1252 with gates
:1578-:1870 and outcomes :1942/:1944/:1946/:2570; leg 1 :9936 -> :10590 (59 pts), :12216 ->
:12870 (54), :13914 -> :14744 (63), :15206 -> :16322 (69); V1 refresh :32662 -> :33316 (2),
:37764 -> :38418 (2); leg 2 :30966 -> :31622 (209), :34068 -> :34722 (225), :36780 -> :37418
(209), :38892 -> :39546 (226); leg 3 :73434 -> :74136/:74148/:74316/:74678, :77174 ->
:77828/:77840/:78006/:78270, :81140 -> :81952/:81964/:82218/:82564, :84446 ->
:85100/:85112/:85326/:85638.
runs\20260914T154243Z_run\watchvrf-trace.csv - CON/POS/TSK rows; TSK at t=373.
runs\20260914T154243Z_run\reports-captured.log - REPORT #211 TASKCMPLT 15:51:22.659Z; 147
position ticks for 001aa71b.
runs\20260914T154243Z_run\c2sim-bus.log:1 - ORDER 15:49:49.653Z.
runs\20260914T154243Z_run\run-manifest.json - clocks, inputs (preOrderSettleSecs 240, the
nolifeform type map), oracle.earlyExit, validityFlags.
runs\20260914T154243Z_run\runner-watchdog.log - arm 15:43:48.8Z, stand-down 16:11:05.4Z.
runs\20260914T154243Z_run\observers.stop - stop stamp 16:10:50.678Z (clock cross-check).
runs\launch52\RunScenario-20260914T154242Z.threads.csv - back-end cores / working set, 5 s.
scratchpad\g7_runner4.log - :134 watchdog survival, :186-:198 the 240 s settle, :282 the
StopWhenComplete line.
tools\WatchVrf\ConFormat.cs:83-96 and scripts\RunnerLib.ps1:366-406 - what an RPT line is and
what condition (4) requires.
docs\experiments\PREREG_MESHQUERY_G7_2026-09-14.md, G7_PROBE_DESIGN_v3_2026-09-14.md,
MESH_QUERY_VS_DISTANCE_2026-09-14.md, G6_RESULTS_2026-09-14.md, NAVMESH_QUERY_DOCS_2026-09-14.md.
Harvest scripts (scratchpad, read-only, reuse g6_harvest.py's decode / sim-prefix clock /
hav / along): g7a4\{harvest.py, verdict.py, seams.py, timeline.py}.
