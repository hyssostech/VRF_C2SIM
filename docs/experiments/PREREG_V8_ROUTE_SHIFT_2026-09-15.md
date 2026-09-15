# PREREG V8 - the LATERAL ROUTE SHIFT ON for one run: does detouring 1-35's ridge leg 75 m north carry it past the freeze? (written 2026-09-15 ~02:25Z, BEFORE the run)

Tier HEAVY (the first live run of a feature that CHANGES WHERE UNITS DRIVE, and a remedy claim against a cause the record
has argued six ways). Gate: PREREG (this file). Nothing below has been run.

Docs consulted: docs/experiments/DESIGN_ROUTE_SHIFT_2026-09-15.md - sec 3.1/3.1a (the four-point detour), sec 4 (the two
acceptance conditions and the search), sec 5 (the config keys), sec 6 (THE REFERENCE MEASUREMENT: the authored line 1.098,
the chosen +75 m north at 0.803 with a formation band max of 0.878, and the controls), sec 8 (L1-L8) and **sec 9, the
PREREG DRAFT this file makes concrete**; docs/RUNBOOK.md sec 12 (both keys, the three log lines, the deferred dispatch and
its timeout, the offline proof); docs/experiments/PREREG_V7_AO20_2026-09-15.md RESULTS (the AO20 fixture works; V1 is
206 m OUTSIDE the area, so the V1 goal fails the vendor's `Is destination in nav area?` gate and the leg is DRIVEN
STRAIGHT); docs/experiments/PREREG_N1_N2_CORRIDOR_SLOPE_2026-09-14.md sec 10.3 (N2d: the unit completed V0->V1 on a line
1.28 km NORTH of the line every earlier run drove - "the freeze is a LINE property"); src/VrfC2SimApp/RouteShiftSelfTest.cs
(the locked geometry: bearing 263.02, leg 6,593.3 m, worst window at s = 2,006 m, `Pin`/`Pout` ON the authored line, `D1`
and `D2` at the full offset); src/VrfC2SimApp/VrfC2SimService.cs (the queue point, the splice, and the three log lines);
src/VrfC2SimApp/VrfSettings.cs (every default quoted below); scratchpad review3/VALIDATION_RUN_PLAN.md (the V8 sketch).

---

## 1. THE RUN

| item | value |
|---|---|
| fixture | `R9_Mojave_Empty_52_NavAO20_AG_S2` (the AO20 terrain V7 proved) |
| init | `data/COA-STP1_Initialization.xml` - THE ORIGINAL (1-35 de-stacks to 34.658442/-116.740092, the start of every P11/G2/G3/G5/G6 run) |
| order | `data/PROBE_RIDGE_1-35_Order.xml` (T1: V0, V1, V2, V3; Duration `PT2H`) |
| THE ONE VARIABLE | `Vrf__PreflightRouteShift=true` |
| carried with it | `Vrf__PreflightWarnings=true` (so the route that IS driven is scored too), `Vrf__PreflightOffline=true`, `Vrf__PreflightCacheDir=<repo>\tools\preflight\preflight_cache` |
| duration scale | `Vrf__DurationScale=1.0` (the order as written; nothing completes by time inside the window) |
| window | `--run-secs 900`, `--stop-when-complete` (it cannot fire at scale 1.0 - V7 measured exactly that) |
| consoles | `--object-console 4 --member-console 4` (1-35's six members at level 4 - the lane question, P5) |
| gate | `--pre-order-gate nav-area --pre-order-settle 240`, `--position-report 10`, `--sample-threads` |
| launch line | `scratchpad\v5v8\v8_launch.sh` |

Everything else is V7's line. The shift's own eight tuning keys stay at their shipped defaults
(`MaxMeters` 600, `StepMeters` 25, `MarginRatio` 0.10, `ClearFormationBand` true, `PadMeters` 50, `LeadMeters` 110,
`MaxTurnDegrees` 30, `TimeoutSeconds` 30) - a confirming run of a feature does not also tune it.

### 1a. WHY THE CACHE KEY IS SET AND WHY OFFLINE IS TRUE

`Vrf:PreflightCacheDir` defaults to `""`, which means "a `preflight-cache` folder beside the executable" - a folder that
does not exist, so a cold run would fetch every tile over HTTP AT DISPATCH and could hit the 30 s deferral timeout, which
dispatches the AUTHORED line and voids the run. The local `tools\preflight\preflight_cache` (341 `.tif` tiles) is the one
`--routeshift-selftest` scores this exact leg on with **0 network fetches**. `Vrf:PreflightOffline=true` makes a fetch
impossible, so the run cannot write to that folder either and cannot quietly succeed by reaching the network.
NOTE, against RUNBOOK sec 9's wording: that cache is **gitignored** (`tools/preflight/.gitignore`), not committed. It
exists on this machine. If the run is ever moved to another machine, the cache must be copied first or this key changed.

### 1b. THE CONFOUND THE SKETCH FLAGS, RESOLVED

The V8 sketch worries that "on AO20 the leg's start is mesh-planned for 2.3 km only if...". V7 settled it, and the
resolution is what makes this run READABLE:

- With the ORIGINAL init the unit de-stacks 2,774 m from its authored origin, so `Vrf:DropOriginVertexMeters` (100) fires
  and V0 - the authored assembly point - is DROPPED. The route the interface builds is therefore
  `[live de-stack position, V1, V2, V3]`: **four vertices, three legs, and leg 1 is the 6,593.3 m ridge leg on bearing
  263.02** - the exact line of the reference measurement, and the exact line six runs froze on.
- V1 is 206 m OUTSIDE the AO20 nav area, so the vendor's `Is destination in nav area?` gate fails and leg 1 is driven as
  a **1-part straight feature path** (V7 P-D, measured; never a `(0) points` mesh refusal). That is not a defect here -
  it is the precondition. A shifted polyline only steers a unit if the unit drives the polyline, and L5 of the design
  says exactly that. **On this leg, on this area, the shift is the only thing that can move the line.**
- V7's first goal was the 2.3 km V0 leg because its init placed the unit AWAY from the assembly point so the drop did NOT
  fire. That is the one thing this run changes back.

---

## 2. PREDICTIONS (a missed HIGH is a STOP)

### V8a (HIGH - THE GATE: the feature acts, and acts where the offline numbers say)

Three lines in `vrfc2simapp.log`, in this order:

    Task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1': ROUTE SHIFT check queued for <unit> (4 vertices); dispatch deferred
    to the result (timeout 30 s -> the authored line).

    Task '...' (<unit>) leg 1: ROUTE SHIFTED 75 m north - ratio 1.098 -> 0.803 (formation band max 0.878);
    inserted (34.6566,-116.7585) and (34.6559,-116.7652). STP's own vertices are unchanged and in order.

    Task '...' (<unit>): ROUTE SHIFT applied to 1 of <F> flagged leg(s); route 4 -> 8 vertices.

with: leg index **1**, side **north**, offset **+75 m**, before ratio **1.098**, after ratio **0.803** (<= the acceptance
`threshold - margin` = 0.82), formation band max **0.878** (< the threshold 0.92), and the route growing **4 -> 8**
vertices. `<F>` is the number of flagged legs and is expected to be 1 (the offline controls score T1 leg 2 at 0.585);
if legs 2 or 3 are also flagged the line will say so and only the SHIFTED count is scored here.

**MISS - and every one of these is a STOP, because nothing downstream is readable without it:** no shift line at all; a
SOUTHWARD shift; an offset outside +50..+550 m; a route still of 4 vertices; the line
`NO ROUTE SHIFT - NO CLEARED LINE within +/-600 m`; or
`the ROUTE SHIFT check did not finish within 30 s - dispatching on the line as authored` (the cache key is wrong - sec 1a).
`ROUTE SHIFT skipped - this aggregate dispatch drives to the route's FINAL point only` would mean
`Vrf:MoveIntoFormation` or `Vrf:AggregatePlanAndMove` is set somewhere this file did not find; neither appsettings file
sets them and no script passes them, so that line is a STOP too.

**The four inserted points, predicted.** `RouteShift.BuildDetour` places `Pin` and `Pout` ON the authored line and `D1`,
`D2` at the full 75 m cross-track, with the window padded by `PadMeters` 50 and `LeadMeters` 110 each side and a transit
of `75 / tan(30 deg)` = **129.9 m**. Reproduced from the self-test's own constants on the worst window centred at
s = 2,006 m:

| point | s along the leg (m) | cross-track (m) | lat, lon (+/- 25 m) |
|---|---|---|---|
| `Pin` | 1,696 | 0 | 34.65659, -116.75850 |
| `D1` | 1,826 | +75 north | 34.65711, -116.76001 |
| `D2` | 2,186 | +75 north | 34.65672, -116.76391 |
| `Pout` | 2,316 | 0 | 34.65591, -116.76522 |

(The self-test's own window is quoted as 1,990-2,030 in one place and 1,986-2,026 in another; that 4 m disagreement moves
every row by 4 m and is inside the tolerance. The authoritative values are whatever `VrfC2SimApp --routeshift-selftest`
prints - capture that output offline BEFORE the launch and staple it to the results.)

### V8b (HIGH - THE MEASUREMENT: the unit passes the freeze band)

1-35's leader passes the P11/G3/G5/G6 freeze band - s = 1,970 +/- 3 m on the leg axis, the point 34.65608/-116.76142
(independently recomputed here at s = 1,968.2 m, cross-track ~0) - **with a cross-track of at least +40 m NORTH of the
authored line**, and **continues past s = 2,100 m**. The nominal cross-track there is +75 m; the design's own L8 records
that a leader drives measurably off the line it was given (up to 34.6 m in READ_4-27 sec 2.2), which is why the scored
threshold is +40 and not +75. Equivalently: closest approach to 34.65608/-116.76142 above 40 m.

Six runs put a stop within 150 m of that point. Passing it IS the result.

### V8c (HIGH-ish - THE OUTCOME) 

The unit reaches V1 (inside the arrival radius) and T1 reports TASKCMPLT, **or** the window closes with it still
advancing past s = 4,000 m. N2d is the existence proof that this unit can drive this route on a clean line; this run
predicts it on a line 75 m north instead of 1.28 km north. Note that T1's Duration is PT2H at scale 1.0, so a TASKCMPLT
inside the window can only come from arrival, never from the end time - which makes it an unambiguous reading.

### V8d (MEDIUM - THE REPORT IS ON THE WIRE)

`reports-captured.log` carries an ObservationReport for the shift, at the FIRST INSERTED WAYPOINT (`Pin`), whose
`NameObservation` Marking reads

    ROUTE SHIFT: task <task> (<unit>) leg 1 - the authored line scored 1.098 against this unit's own limit; the
    interface detoured it 75 m north of the authored line around that window, scoring 0.803 (formation band max
    0.878). STP's own vertices are unchanged and in order; the detour lies between them.

paired with a `LocationObservation` at `Pin`. With `Vrf:PreflightWarnings` also on, any leg still flagged AFTER the shift
gets its own warning report - and there should be none for leg 1.

### V8e (RECORDED, not predicted)

Where the other five members stop and their cross-tracks (the lane question of FINDING_EARLY_STOPS sec 7e). With the
formation-band condition on, no slot line should sit on the 1.098 ground: the +75 m band row is 0.878 / 0.724 / 0.803 /
0.777 / 0.833 for the -50/-25/0/+25/+50 slots. Also record: the sim ratio; whether the terrain-profile continuation ran
AFTER the shift (it must - the inserted vertices need authored altitudes, and the shift queues BEFORE the profile
request by construction); and every member console line at the detour corners.

---

## 3. FALSIFIERS (the design's sec 9, carried verbatim in meaning)

- **(a) The unit freezes within 150 m of the P11 point anyway.** Then the LINE is not the whole cause and
  FINDING_EARLY_STOPS sec 7's never-excluded competing hypothesis - formation mutual speed control - is back on the
  table. **A MISS OF V8b IS A STOP.**
- **(b) The unit freezes somewhere NEW on the shifted line that the sampler scored clean.** Then the SAMPLER, not the
  remedy, is what needs work: the shift did what it claimed and the prediction underneath it was wrong.
- **(c) A vendor task failure at the inserted vertices** (e.g. "Entity not embarked on same object as target",
  READ_G2 sec 1). Then waypoint insertion has a cost this design did not anticipate, and the feature must not ship on.

## 4. STOP CONDITIONS

Any HIGH miss (V8a, V8b); falsifier (a) or (c); a crash; any `Tick phase '<name>' FAILED` or `MissingMethodException`
line; **any evidence of a DOUBLE DISPATCH** - two `MoveAlongRoute` issues, two TASKSTRTs or two routes created for T1.
That last one is the defect the design named as the one most likely to ship (the worker and the timeout sweep both
continuing); `Preflight.OneShotClaim` is the guard, and this run is its first live exercise.

## 5. CONFOUNDS, STATED

- **The route shift is not the only thing that differs from the six freeze runs.** They ran on the MojaveCOA area; this
  runs on AO20 with the AG_S2 SMS. V7 is the control for that pair on this fixture (same start ground, no shift), and
  N2d is the control for the LINE claim. Neither is a same-session control, and that is recorded, not waved away.
- The shift DEFERS the dispatch off the tick thread. With a warm offline cache the search is milliseconds, but the
  deferral is real and the dispatch instant will be a little later than in V7. It does not change the start position.
- L1 (one window per leg), L2 (a flag is a prediction, not a vendor verdict, and the record holds one measured false
  alarm), L3 (long legs rarely clear), L4 (the 0.752 limit is an ANALOGY), L6 (the formation condition is over-strict for
  a single entity), L8 (the anchor is the live position and the leader drives up to ~34.6 m off it) all stand and are not
  re-argued here.
- Legs 2 (V1->V2) and 3 (V2->V3) are not expected to be flagged, but they have never been scored together as a route in
  this exact form. If one IS flagged and shifted, V8c's reading changes and the extra shift must be recorded separately -
  it is not part of what V8b measures.

## 6. HARVEST

- `vrfc2simapp.log`: the three `ROUTE SHIFT` lines verbatim with their numbers; the inserted coordinates; the
  `route <before> -> <after> vertices` line; any `NO ROUTE SHIFT` / timeout / skipped line; the terrain-profile lines that
  follow; the dispatch and TASKSTRT.
- `watchvrf-trace.csv`: the leader's track resampled onto the leg axis (bearing 263.02 from 34.658442/-116.740092) -
  along-track s and cross-track for every fix, and the closest approach to 34.65608/-116.76142. The n1n2 harvest scripts
  already do this; their axis constants are the OLD de-stack-start axis, which is the right one for this run.
- `reports-captured.log`: the shift ObservationReport and its Marking; TaskStatus codes for T1.
- The five other members' tracks and consoles (V8e).
- `VrfC2SimApp --routeshift-selftest` output captured offline before the launch, as the pre-registration of V8a's numbers.

## 7. ADVERSARIAL REVIEW OF THIS PRE-REGISTRATION (HEAVY; before the run)

**Strongest competing reading of a PASS: "the unit crossed because it was on AO20 with AG_S2, not because of the
shift."** Partly answerable and partly not. V7 ran the SAME fixture, SMS, build and order from a start 1.2 km away and
its leader drove V1 and V2 on straight 1-part paths - so AO20 alone does not make this leg passable, because on this leg
the mesh is not consulted at all (the destination gate fails, V7 P-D). What AO20 changes for THIS leg is nothing: the
line is driven as a straight feature path either way. That is the argument, and it rests on V7's measurement rather than
on a same-session control, which this run does not have. The clean separation would be V8 with the shift OFF from the
same start on the same area - that is P11/G3/G5/G6 on a different area, and re-running it costs a whole window for a
result the record already holds six times. NOT run; recorded as the residual.

**Second competing reading: "the shift moved the line but the crossing is the 75 m of cross-track, not the feature."**
That is not a competing reading - it is the mechanism the feature claims. What would distinguish a lucky line from a
sound remedy is the sampler's prediction holding on OTHER legs, which is L2's open false-alarm question and needs more
orders, not this run.

**The thing most likely to make this run unreadable, and it is not the terrain.** The deferred dispatch. If the cache
key is wrong or the tiles do not cover a candidate offset, the check either declines (`NO CLEARED LINE`) or times out,
and BOTH dispatch the authored line - which looks, in the track, exactly like a feature that did nothing. V8a is written
to separate those three endings by their own log lines before any track is read, and sec 1a is why the cache key is set
explicitly rather than left at its default.

**Unexplained and carried, from the record, not resolved here:** what stops the units that freeze on ground this sampler
scores as benign (FINDING 7b: 1-6 in P11 at -0.14 over 55 m; 856/HHC on the flat) - this remedy does nothing for them and
must not be reported as if it did; the shared crawl speeds of moving subgroups; and whether formation mutual speed control
contributes to the freeze at all, which no capture in the record can separate. V7 also left an off-mesh crawl to a stop
on its final leg UNEXPLAINED; if this run reaches V2 or V3 it may meet the same thing, and that is a different mechanism
from the one V8b tests.

**Verified vs assumed.** VERIFIED (read this pass): the origin-vertex-drop condition and that it yields a 4-vertex route;
the queue point and that it precedes the terrain profile; `SpliceShift`'s 1-BASED leg index; the three log lines and the
Marking text; every default in sec 1; the self-test's locked geometry (bearing 263.02, 6,593.3 m, window at s = 2,006,
transit 129.9 m, `Pin`/`Pout` on the line); the cache's 341 tiles and its gitignored status; that no settings file or
script sets `MoveIntoFormation` or `AggregatePlanAndMove`. ASSUMED (not re-verified here): that the cached tiles cover
every candidate offset the search will try on leg 1 - the self-test evaluates -75..+100 offline, and the search stops at
the first clearing offset (+75), so it should never reach further; and that the deployed tree is the G-A RERUN pin.

---

## RESULTS - run 20260915T023743Z (route shift ON)

Harvested 2026-09-15 by the Opus HARVEST+FIX executor, tier HEAVY (a cause claim and a code change).
Read-only on `runs/`; no vendor sim log was opened. Every offline number below was recomputed on
`tools/preflight/leg_check.py`'s own sampler against the SAME 341-tile cache the run used, **0 tiles
fetched** (scratchpad `v8harvest/repro.py`, `scan.py`).

### R1. VERDICT TABLE

| # | prediction | required | measured | verdict |
|---|---|---|---|---|
| V8a | THE GATE | three ROUTE SHIFT lines; leg 1; **north**; +50..+550 m; 1.098 -> 0.803; band 0.878; route 4 -> 8 | three lines, leg 1, **125 m SOUTH**; **1.248 -> 0.661**; band **0.836**; route 4 -> 8 | **MISS - STOP** (a southward shift is a named STOP; sec 2) |
| V8b | THE MEASUREMENT | leader passes s = 1,970 +/- 3 m with cross-track **>= +40 m NORTH** and continues past s = 2,100 | leader passed and reached s = **4,138 m**, but at s = 2,048 its cross-track was **-166.0 m (SOUTH)** | **MISS as written** (the band WAS crossed - on the opposite side) |
| V8c | THE OUTCOME | V1/TASKCMPLT, **or** the window closes with it still advancing past s = 4,000 m | no TASKCMPLT; leader at s = 4,138 m, +157.4 m over the final 600 sim s | **MET** (second branch) |
| V8d | THE REPORT | ObservationReport for the shift on the wire with leg, offset, both ratios | REPORT #232, 02:40:20.460Z, quoted in R3 | **MET** (one wording deviation, R3) |
| V8e | RECORDED | where the other five went | 4 of 5 crossed with the leader; **M3 1 froze 38.3 m from the P11 point** | **RECORDED** (R5) |
| STOPs | sec 4 | no crash / no `Tick phase FAILED` / no `MissingMethodException` / no double dispatch | 0 / 0 / 0 / none (1 `CreateRoute`, 1 `MoveAlongRoute`, 1 TASKSTRT) | **CLEAN** |
| falsifier (c) | sec 3 | a vendor task failure at the inserted vertices | **ONE**, on M3 1 at wall 48.1 s: `Entity not embarked on same object as target [%1].  Ending task \| Route 9` | **OCCURRED, once** (R5) |

The feature ACTED, deferred correctly, dispatched exactly once, reported itself, and left STP's
vertices untouched and in order. It acted on the WRONG SIDE, and that is the STOP.

### R2. THE PRE-FLIGHT LINES, VERBATIM (`vrfc2simapp.log` 1289 / 1317 / 1319)

    Task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1': ROUTE SHIFT check queued for 1-35/2/1_A~PXY (4 vertices); dispatch deferred to the result (timeout 30 s -> the authored line).
    Task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' (1-35/2/1_A~PXY) leg 1: ROUTE SHIFTED 125 m south - ratio 1.248 -> 0.661 (formation band max 0.836); inserted (34.655153,-116.759727) and (34.654771,-116.763635). STP's own vertices are unchanged and in order.
    Task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' (1-35/2/1_A~PXY): ROUTE SHIFT applied to 1 of 1 flagged leg(s); route 4 -> 8 vertices.

Order of operations, as designed: the origin-vertex drop fired first (`dropped 1 leading route
point(s) ... the unit was spread 2797 m from it`), then the shift queued, then the terrain profile
authored **all eight** vertices (`Terrain profile request 167 sent for 8 vertices`, reply
`#0:34.65820,-116.74009 #1:34.65650,-116.75754 #2:34.65515,-116.75973 #3:34.65477,-116.76363
#4:34.65566,-116.76615 #5:34.65121,-116.81164 #6:34.59635,-116.95233 #7:34.57029,-117.00622`),
then `CreateRoute ... (8 pts)` and one `MoveAlongRoute`. With `PreflightWarnings` also on, the
dispatched 7-leg route produced **no** flagged-leg warning and no error - i.e. nothing was left
flagged after the shift.

### R3. THE OBSERVATION REPORT ON THE BUS (`reports-captured.log` REPORT #232, 02:40:20.460Z)

    <Latitude>34.655153</Latitude> <Longitude>-116.759727</Longitude> (AltitudeMSL 1613.5)
    <Marking>ROUTE SHIFT: task T1_AOA_SE_1-35_AR;_2/1_AD_P1 (1-35/2/1_A~PXY) leg 1 - the authored
    line scored 1.248 against this unit's own limit; the interface detoured it 125 m south of the
    authored line around that window, scoring 0.661 (formation band max 0.836). STP's own vertices
    are unchanged and in order; the detour lies between them.</Marking>

DEVIATION FROM V8d AS WRITTEN: the `LocationObservation` is at **D1**, the first OFFSET waypoint,
not at `Pin`. The code is right and this pre-registration's wording was wrong - `LegShift.In` is
documented as "the two OFFSET waypoints - where the detour actually stands clear of the face", and
`ChooseForLeg` sets `In = poly[2]`. Nothing to fix; recorded so the next reader is not misled.

### R4. (a) WHY 1.248 AND NOT 1.098 - WHICH LINE WAS SCORED

**The anchor.** `VrfC2SimService.ExecuteTaskOnTick` builds `routeGeo[0]` from `live.LatDeg/LonDeg`,
the **UNIT object's** own position (`TryGetEntityGeodetic` on the unit's VRF uuid,
VrfC2SimService.cs:2770), then the origin-vertex drop removes V0, so leg 1 is `live -> V1`. That
live position is recoverable exactly from the unit's own first PositionReport at 02:40:16.802Z,
four seconds before the shift line:

    1-35/2/1_A~PXY  34.65820208652259, -116.74009186651882

The reference measurement, the published lateral tables and `--routeshift-selftest` are anchored
somewhere else: `leg_check.py:starts_from_run()` takes "the FIRST POS row of the FORMATION LEADER
(the first member listed)", which is M1A2 1 at **34.658442, -116.740092** (its first fix in this
run, to 6 dp). The two are **26.7 m apart**, the unit sitting 26.5 m SOUTH of the reference line.
Corroboration from the run's own arithmetic: V0 -> reference anchor = 2,773.9 m (this file's
"2,774 m"); V0 -> live anchor = 2,796.9 m, and the log says **2797 m**.

Twenty-seven metres rotates a 6.59 km leg enough to change everything that follows:

| | anchor | length | bearing | worst window | **ratio** |
|---|---|---|---|---|---|
| calibration / self-test | 34.658442, -116.740092 | 6,593.3 m | 263.017 | s 1,990-2,030 | **1.098** |
| **what V8 scored** | 34.6582021, -116.7400919 | 6,590.1 m | 263.247 | s 1,983-2,023 | **1.248** |

The P11 freeze point 34.65608/-116.76142 sits at cross-track -22.8 m on the reference axis but
**-4.2 m on the live axis** - the live line runs almost exactly through it.

Offline reproduction of the LIVE decision, on the run's own cache: base **1.248**, chosen
**-125 m south at 0.661**, band **0.836**, inserted points **(34.655153,-116.759727)** and
**(34.654771,-116.763635)** - the log's numbers and coordinates to every digit printed.

### R5. (b) WHERE THE SIX MEMBERS WENT

Leg axis = the reference axis (start 34.658442/-116.740092 -> V1), kept per `n2b_harvest.py`.
NORTH is positive. Sim ratio ~5.9x (5,501 sim s in ~928 wall s); tracks span sim 55 - 5,556.

| member | slot | cross-track at s ~ 2,006 | closest to the P11 freeze point | last s | advance over final 600 sim s | verdict |
|---|---|---|---|---|---|---|
| M1A2 1 (leader) | 0 | **-166.0 m** | 146 m | **4,138 m** | +157.4 m | PASSED |
| M1A2 2 | -50 | -186.0 m | 168 m | 4,182 m | +180.9 m | PASSED |
| M577A2 1 | -25 | -188.3 m | 166 m | 4,135 m | +180.8 m | PASSED |
| HMMWV 1 | 0 | -142.3 m | 124 m | 4,173 m | +183.0 m | PASSED |
| HMMWV 2 | +25 | -142.0 m | 115 m | 4,119 m | +184.4 m | PASSED |
| **M3 1** | **+50** | **-59.0 m** | **38.3 m** | **1,975 m** | **+0.6 m** | **FROZE** |

The detour was DRIVEN, not merely authored. The leader's own track passes within **1 m of Pin,
14 m of D1, 1 m of D2 and 1 m of Pout**.

**M3 1 is the one member that did not get clear laterally** (it stopped 217 m short of D2) and it
stopped at s = 1,975 m, 38.3 m from the point P11/G3/G5/G6 stopped at. Its console then repeats
`Starting condition node Is path blocked? / fail in action Is path blocked?` 5,406 times to the end
of the run with no further goal and no replan - the same "running, unblocked, goal unchanged
forever" signature FINDING_EARLY_STOPS sec 7 records. Its speed falls to 0.08-0.10 m/s.
**Its slot line was inside the band the chooser scored at 0.836, i.e. ground this sampler called
clear.** That is prereg falsifier (b) realized, against the SAMPLER and not against the remedy.

**THE CRAWL, unexplained and carried.** All five movers drop from 5.7 m/s to ~0.30 m/s at sim
~700-800 (leg s ~ 2,400-2,600, just past `Pout`) and hold 0.26-0.42 m/s for the remaining 4,800
sim s. V7 left the same thing unexplained on its final leg. Nothing in this run separates it.

**A CONFOUND THIS RUN CREATED AND CANNOT REMOVE - the inserted waypoints were MESH-PLANNED.**
Every one of the leader's five short goals (Pin, D1, D2, Pout and the offset start) passed the
vendor's `Node Is destination in nav area?: success` and was planned on the AO20 mesh -
`Planned path has 5 / 22 / 7 / 15 / 6 points`. Only the final `Pout -> V1` goal failed that gate
(`fail in action Is destination in nav area?`, `Planned path has 1 parts`) and was driven straight.
Sec 1b of this pre-registration assumed leg 1 would be a single straight feature path; inserting
waypoints INSIDE the nav area changed that. So the crossing has two candidate causes at once - the
125 m of lateral offset, and the fact that the leg became five mesh-planned hops - and V8 cannot
separate them. See R8 for the control that can.

### R6. (c) THE CAUSE

**H1 - missing tiles scored as passable (a FALSE CLEAR on the south): FALSIFIED.**
Every candidate polyline the chooser could try was re-scored offline on the run's own cache at
every magnitude 25..600 m, **both sides, both anchors**: **0 NaN samples, everywhere**. Not one
segment of one candidate touched a data hole. PREREG_RIDGE_AG 3.3's caveat ("the cache is not
complete south of -100 m") is about a FULL PARALLEL line over the whole 6.59 km; the detour leaves
the authored line for only ~860 m of it, and those tiles are all present. The false-clear
hypothesis is dead and the branch name that carries this fix is a historical artefact of it.

**H2 - the anchor (R4): contributing, but not itself an error.** The 26.7 m moved the whole lateral
profile by ~19 m at the worst window and pushed the cleared northern corridor one step outward. But
the route IS dispatched from the unit's live position, so scoring from there is correct by
construction (design note L8). What it exposes is that the calibration and the self-test are
anchored on a DIFFERENT point (the formation leader) than the live path - a real instrument
mismatch, now a fixture rather than a surprise.

**H3 - C2, the formation band, chose the SIDE: SUPPORTED and DECISIVE.**

| magnitude | north ratio | C1 | band | south ratio | C1 | band |
|---|---|---|---|---|---|---|
| 25 | 1.022 | no | - | 1.280 | no | - |
| 50 | 0.829 | no | - | 1.050 | no | - |
| 75 | **0.735** | **yes** | **1.022 -> C2 refuses** | 0.836 | no | - |
| 100 | 0.835 | no | - | 0.715 | yes | 1.050 -> C2 refuses |
| 125 | 0.841 | no | - | **0.661** | **yes** | **0.836 -> TAKEN** |

Under **C1 alone** - the brief's own rule, `ClearFormationBand=false` - this same live anchor
chooses **+75 m NORTH at 0.735**. C2 refused it because its inner slot line (-50) still scored
1.022, and the single-phase search then walked past the north side N2d actually drove and onto the
south side six runs froze on. The design note says C2 "is used here only to SIZE a shift that the
route-line verdict has already demanded" (sec 4.1). It sided it. That is the defect.

FALSIFIER FOR H3, checked: if C1 alone had also chosen south, H3 would be refuted. It does not -
+75 north passes C1 at magnitude 75 while -75 south fails at 0.836. Re-run offline and in the fixed
build; both agree.

### R7. THE FIX - worktree `fix/route-shift-missing-tiles`, commit **6f6d68b** (NOT merged)

1. **`ChooseForLeg` is now two phases.** PHASE A picks the SIDE from **C1 alone** (first magnitude
   at which anything clears; lower ratio on a tie). PHASE B picks the MAGNITUDE **on that side
   only**. If nothing on that side ever clears C2, the smallest C1-clearing candidate on it is taken
   - exactly the brief's rule - and a WARNING, the `LegShift` note and the C2SIM Marking all say so.
   Refusing instead would leave the whole unit on the face to spare one slot line.
2. **Unknown is never clear.** Any candidate polyline (or formation slot line) with a SINGLE
   missing-tile sample is UNSCORABLE and refused. The FLAG keeps its calibrated 1 % tolerance
   (`LegScorer.MaxNanFraction`) - a warning must not be silenced by a tile gap, an action must not be
   taken over unread ground. **This was not the cause**; it is the hardening the investigation asked
   for, and it costs nothing here (0 NaN in the whole band).
3. **Every candidate is logged with its own missing-tile count** (`ROUTE SHIFT candidates - ...`),
   and the band-fallback ending gets its own WARNING line.

WHAT THE FIXED CHOOSER RETURNS:

| anchor | before the fix | after the fix |
|---|---|---|
| reference / leader (the self-test's) | +75 m north, 0.803, band 0.878 | **unchanged**: +75 m north, 0.803, band 0.878 |
| **V8 live (the unit's)** | **-125 m SOUTH, 0.661, band 0.836** | **+250 m NORTH, 0.761, band 0.862** |
| V8 live, `ClearFormationBand=false` | +75 m north, 0.735 | +75 m north, 0.735 |

+250 m north sits inside the +50..+550 m band PREREG_RIDGE_AG 3.3 measured clear (+250 = 0.709
there), so it is a measured-clear corridor and not an extrapolation.

TESTS. `--routeshift-selftest` **84 ok / 0 fail**, **0 network fetches**, 57 cache hits; offline
suites **19/19 exit 0**; `-t:Rebuild` 0 errors. New fixtures: the V8 live anchor must return NORTH,
+250 m at 0.761 band 0.862, never -125 m south, with no southward candidate ever accepted; the same
anchor under `ClearFormationBand=false` gives +75 m north at 0.735; a 0.100 candidate carrying ONE
missing sample is refused while the same ratio with none is taken at the first step; an empty cache
yields NO VERDICT, no flag, no shift and the authored route point for point.

FAIL-FIRST, driven through the suite and reverted:

- **B1** - phase B scans both sides, i.e. the OLD single-phase rule: **5 checks trip**, and the
  chooser returns **exactly** the run's `-125 m south at 0.661`. The suite would have caught this.
  Note that sections 2 and 3 (the reference anchor) stay green under B1 - which is precisely why the
  defect shipped: at the calibration anchor the old rule is right.
- **B2** - the NaN gate removed: **3 checks trip**.

Docs updated the same turn on that branch: DESIGN_ROUTE_SHIFT sec 4.2a/4.2b and sec 7; RUNBOOK
sec 12.

### R8. WHAT V8b (THE CONFIRMING RE-RUN) MUST SHOW

1. The shift line must read **`ROUTE SHIFTED 250 m north - ratio 1.248 -> 0.761 (formation band max
   0.862)`** for leg 1 - side, magnitude and both ratios pre-registered here. A southward shift, or
   any offset other than +250 m from this start, is a STOP and means the anchor moved again.
2. The `ROUTE SHIFT candidates` line must show `nan 0` on every candidate. Any non-zero NaN count
   means the cache no longer covers the band and the run is not comparable to this one.
3. 1-35's leader must pass s = 1,970 +/- 3 m with a cross-track of at least **+40 m NORTH** and
   continue past s = 2,100 m - V8b as originally written, now on the side the record supports.
4. **M3 1 is the reading that matters.** It is the only member that froze here, on its own +50 slot
   line, on ground the sampler scored at 0.836. On a +250 m north shift its slot line is 300 m north
   of the authored line. If it freezes again, the SAMPLER is indicted and the lane question of
   FINDING sec 7e is still open; if it crosses, C2's whole purpose is demonstrated.
5. **RUN THE ZERO-OFFSET CONTROL, or V8b cannot separate the two causes (R5).** One extra run with
   the four detour points inserted **at offset 0** (on the authored line) isolates "waypoint
   insertion turns the leg into mesh-planned hops" from "125-250 m of lateral offset". Without it,
   every crossing on this AO is confounded, in both directions.
6. The crawl to ~0.30 m/s past `Pout` must be measured at equal sim time against this run. It is
   unexplained by anything here and is not evidence for or against the shift.

### R9. ADVERSARIAL REVIEW (HEAVY)

**Strongest competing explanation of the southward shift, and it is the one the brief expected:
missing tiles made unknown ground look clear.** Falsified, not argued away - 0 NaN samples on every
candidate at every magnitude on both sides and both anchors, on the very cache the run used, with
the chooser's own numbers reproduced to the digit. Had the count been non-zero anywhere the reading
would be different; it is zero everywhere.

**Second competing explanation: the anchor alone did it, and C2 is innocent.** Refuted by a direct
test: with C2 off, the same 26.7 m anchor still chooses +75 m NORTH. The anchor moved the numbers;
C2 moved the side. Both are recorded; only one is the defect.

**Third: "the south shift WORKED - five of six crossed, so the remedy was fine and the fix is
cosmetic."** This is the reading I most want to resist, and it is half right. Five members did cross
a band that had stopped six runs, and the sampler's 0.661 for that specific line was not refuted by
anything in the track. But (i) the run was pre-registered with a southward shift as a STOP, and a
prediction is not allowed to be re-scored after the fact; (ii) the member whose slot sat nearest the
authored line froze at the historical point; and (iii) R5's mesh-planning confound means the
crossing is not cleanly attributable to the offset at all. A rule that reaches the right side by
accident on one leg is not a rule.

**What the fix could make worse.** +250 m is 3.3x the detour the reference anchor would take, and
lateral sensitivity exceeds the calibration margin (0.902 -> 1.130 over 30 m, READ_4-27 sec 2.3).
Three bounds: the shifted line is re-scored WHOLE before it is committed, it must beat the threshold
by the full margin, and +250 m north is inside the band PREREG_RIDGE_AG 3.3 measured clear. It is
still a bigger intervention in STP's intent than +75 m, and that is a cost, not a free win.

**Unexplained and carried, not resolved here.** The ~0.30 m/s crawl of all five movers from sim 700
onward. M3 1's freeze on ground scored 0.836. The single
`Entity not embarked on same object as target [%1].  Ending task | Route 9` on M3 1 at wall 48.1 s -
it is the message prereg falsifier (c) named, it happened once, on the member that later froze, and
it did NOT stop it then (M3 1 replanned and drove another ~1.4 km). Correlation only; no causal
claim is made and none is available from this capture. And what stops units that freeze on ground
this sampler scores as benign (FINDING 7b) - this remedy does nothing for them.

**VERIFIED (measured this pass).** The three shift lines and the ObservationReport, verbatim. The
live anchor, from the unit's own PositionReport. The 26.7 m separation and its corroboration by the
run's own "2797 m". 1.248 / -125 m / 0.661 / 0.836 and both inserted coordinates, reproduced offline
to every printed digit. 0 NaN samples across the whole search band on both sides and both anchors.
C1-alone choosing +75 m north from the live anchor. The six tracks, cross-tracks, last fixes,
closest approaches and speed bins. The leader passing within 1-14 m of all four inserted points. The
five mesh-planned goals and the one straight one. 0 crashes, 0 `Tick phase FAILED`, 0
`MissingMethodException`, 1 `CreateRoute`, 1 `MoveAlongRoute`, 1 TASKSTRT, no terminal report, clean
teardown with RTI preserved, `-StopWhenComplete` did not fire (900 s cap, as predicted for PT2H).
The fixed build: 84/84, 19/19, both fail-first breaks tripping the checks they should.

**ASSUMED (not re-verified this pass).** That `Vrf:PreflightWarnings`' worker ran and found nothing
flagged - inferred from the absence of any flagged-leg warning AND of any pre-flight error, because
that path emits no line at all when nothing is flagged (a small observability gap, noted, not
fixed). That the sim ratio ~5.9x is the fixed-frame speed-up and not something else. That the python
reproduction and the C# port remain digit-identical beyond the values checked here (the self-test's
fixture comparison is the standing guard). That +250 m north is drivable - it is scored clear by the
same sampler and lies in a band measured clear, but NOTHING has driven it.

---

## PREREG V8z - the zero-offset control (written before the run)

Tier STANDARD (one run, one variable, no code change, and the reading is a comparison against two
runs that already exist or are in flight). Gate: PREREG - this section is committed BEFORE the run.
Written 2026-09-15 by the V8 harvest executor. NOTHING BELOW HAS BEEN RUN.

**WHY IT EXISTS.** V8's RESULTS R5 records a confound V8 created and cannot remove: inserting four
waypoints inside the AO20 nav area turned leg 1 from ONE 6.6 km straight feature path into FIVE
short **mesh-planned** hops (`Node Is destination in nav area?: success`, `Planned path has
5 / 22 / 7 / 15 / 6 points`), and at the same time carried the path 125 m laterally. Five of six
members then crossed a band that had stopped six runs. Two candidate causes changed together, and
R8 item 5 named the control that separates them. This is that control.

### 1. THE RUN

| item | value |
|---|---|
| fixture / init / env | **identical to V8** (`R9_Mojave_Empty_52_NavAO20_AG_S2`, `data/COA-STP1_Initialization.xml`, same DeStack, gate, consoles, position reports, offline cache) |
| order | **`data/PROBE_RIDGE_1-35_ZEROOFFSET_Order.xml`** (new; sec 2) |
| THE ONE VARIABLE | `Vrf__PreflightRouteShift=**false**` - nothing may shift |
| carried | `Vrf__PreflightWarnings=true`, `Vrf__PreflightOffline=true`, `Vrf__PreflightCacheDir=<repo>\tools\preflight\preflight_cache` |
| window | `--run-secs 900`, `--stop-when-complete` (cannot fire at `DurationScale` 1.0 / PT2H) |
| launch line | `scratchpad\validation\v8z_launch.sh` |

### 2. THE FOUR POINTS AND HOW THEY WERE DERIVED

`RouteShift.BuildDetour`'s own arithmetic, on the leg V8 actually scored (live anchor
**34.65820208652259, -116.74009186651882** -> V1, 6,590.1 m, bearing 263.247, worst 40 m window
s = 1,983.4-2,023.4), at the shipped defaults **pad 50 m, lead 110 m, corner 30 deg**, with the
transit taken from the offset V8 chose (|-125| / tan 30 = **216.506 m**):

    eIn  = 1983.4 - 50  = 1933.4      d1S = eIn  - 110 = 1823.4      Pin  = d1S - 216.506 = 1606.9
    eOut = 2023.4 + 50  = 2073.4      d2S = eOut + 110 = 2183.4      Pout = d2S + 216.506 = 2399.9

The four control vertices are those four stations taken **on the authored line** - lateral offset
**zero**:

| # | along-leg s | lat, lon | relation to V8 |
|---|---|---|---|
| **Z1** | 1,606.9 m | `34.656497731970752, -116.757536731645459` | **byte-identical to V8's `Pin`** (V8's transit points already lay on the line) |
| **Z2** | 1,823.4 m | `34.656268090232025, -116.759887222131852` | V8's `D1` (34.655153,-116.759727) projected back onto the line |
| **Z3** | 2,183.4 m | `34.655886261992272, -116.763795412559006` | V8's `D2` (34.654771,-116.763635) projected back onto the line |
| **Z4** | 2,399.9 m | `34.655656620253545, -116.766145903045398` | **byte-identical to V8's `Pout`** |

HOP LENGTHS, control vs V8 (m): `1606.8 / 216.5 / 360.0 / 216.5 / 4190.3` against V8's
`1606.8 / 250.0 / 360.0 / 249.9 / 4190.3`. Same structure, same first and last hop to the metre; the
two transit hops are 33 m shorter because a zero-offset transit is pure along-track. **The
`Z2 -> Z3` hop is 360.0 m and runs straight through the flagged window (s 1,983-2,023) and within
5 m of the P11/G3/G5/G6 freeze point** - which is the whole point of the control.

OFFLINE SCORES of the dispatched route, same sampler, same committed cache, **0 tiles fetched**:

| leg | control (zero offset) | V8 (shifted -125 m) |
|---|---|---|
| 1 live -> Z1/Pin (1,607 m) | 0.588 | 0.588 |
| 2 transit (216 / 250 m) | 0.589 | 0.583 |
| **3 Z2 -> Z3 / D1 -> D2 (360 m)** | **1.248 FLAGGED** | **0.661** |
| 4 transit (216 / 250 m) | 0.630 | 0.581 |
| 5 -> V1 (4,190 m) | 0.345 | 0.345 |
| 6 V1 -> V2 (14,246 m) | 0.585 | 0.585 |
| 7 V2 -> V3 (5,722 m) | 0.698 | 0.698 |

The two routes differ in **two vertices and one leg score**. Everything else is identical, which is
exactly what a control has to be.

**HOW THE ORIGIN-VERTEX DROP INTERACTS.** `Vrf:DropOriginVertexMeters` is 100 and the order's first
vertex V0 (34.67998,-116.72480) IS the unit's authored origin, so it is dropped exactly as in V8
(`PreflightRoute.Build` / `ExecuteTaskOnTick`: the loop stops at the first vertex more than 100 m
from the authored origin). The four Z vertices are **3,973 / 4,154 / 4,461 / 4,649 m** from V0, so
the loop stops after V0 and drops nothing else. The dispatched route is therefore
`[live position, Z1, Z2, Z3, Z4, V1, V2, V3]` = **8 vertices**, the same count V8 dispatched.
`VrfC2SimApp --parse-order data/PROBE_RIDGE_1-35_ZEROOFFSET_Order.xml` confirms the authored order:
`points: 8`, `34.67998..., 34.65649..., 34.65626..., 34.65588..., 34.65565..., 34.65121...,
34.59635..., 34.57029...`, `action: MOVE`, `embedded shape: Route`, `duration: 7200000 ms`, exit 0.

**THE ONE THING THAT CAN MOVE.** The Z vertices are AUTHORED (fixed lat/lon); V8's were computed
from the live anchor at dispatch. If this run's de-stack puts the unit somewhere other than
34.6582021/-116.7400919 the authored Z points will sit slightly off the dispatched line. Same init,
same DeStack settings, so it should be identical - but it is a measurement, not an assumption:
report the live anchor from the unit's first PositionReport and the cross-track of Z2 and Z3 from
the dispatched leg. More than ~5 m and P-z2's tolerance is doing work it was not sized for.

### 3. PREDICTIONS

**P-z1 (HIGH - THE GATE: the control is a control).** In `vrfc2simapp.log`:

- **NO line containing `ROUTE SHIFT`, of any kind** - not queued, not applied, not declined, not
  skipped, not timed out. The feature is off.
- `Task '...': terrain profile request <n> sent for 8 vertices` and
  `CreateRoute '... ROUTE' (8 pts)`; one `MoveAlongRoute`; one TASKSTRT.
- **exactly one** `ROUTE PRE-FLIGHT task '...' (1-35/2/1_A~PXY) leg 3:` warning, naming ~40 m of
  **0.939** on **sand** at ~34.6561/-116.7614, limit **0.752** (max-slope 0.94 x soil 0.80) - i.e.
  the authored line's own 1.248, now carried entirely by leg 3. With it, one ObservationReport.

**MISS - each is a STOP, because nothing downstream is readable:** any `ROUTE SHIFT` line (the env
did not take and the route is not the one this section registers); a route of other than 8 vertices;
no leg-3 warning (the inserted geometry did not land on the face and the control does not control).

**P-z2 (HIGH - THE CONTROL HOLDS).** 1-35's leader's cross-track at s = 1,970 +/- 3 m on the leg axis
is **within +/- 15 m** of the authored line (V8 measured -166.0 m there). The tolerance is L8's
measured leader wander (up to 34.6 m in READ_4-27 sec 2.2) deliberately TIGHTENED, because a control
that drifts 30 m is not a zero-offset control; if the measured value lands between 15 and 35 m the
run is reported as INCONCLUSIVE on P-z2 rather than scored either way.

**MISS:** |cross-track| > 15 m at that station. Then the unit is not on the line six runs froze on
and nothing below separates anything.

**P-z3 (THE DISCRIMINATOR - and it has three endings, not two).**

- **BRANCH A - the unit FREEZES like the six straight-line runs.** Closest approach of the leader's
  last fix to 34.65608/-116.76142 **< 150 m** AND net along-track advance over the final 600 sim s
  **< 20 m**. READING: **the LATERAL OFFSET is what carries units across in V8/V8b.** Waypoint
  insertion and mesh-planned hops, on their own, do not. The route shift's central claim survives
  its hardest available test.
- **BRANCH B - the unit CROSSES like V8.** Max along-track s **> 3,000 m** (V8's leader reached
  4,138 m). READING: **WAYPOINT INSERTION - the leg becoming short mesh-planned hops - is what
  carries them, and the shift's lateral component is NOT demonstrated by V8 or V8b.** The remedy
  would then be "insert waypoints", the lateral search would be unproven decoration, and
  DESIGN_ROUTE_SHIFT sec 2.1 would need re-opening.
- **NEITHER - and it must be reported as NEITHER, not forced into a branch.** A stop between
  s = 2,100 and s = 3,000, or a stop inside 150 m of the P11 point that is still advancing more than
  20 m per 600 sim s, or a crossing with a leader that never exceeds 3,000 m. Then the two causes
  are partial and the honest answer is that one run cannot rank them.

**P-z4 (RECORDED, not predicted).**
(i) **M3 1's fate** - it is the member that froze in V8 (38.3 m from the P11 point, on its own +50
slot line, on ground the sampler scored 0.836). Here its slot line sits +50 m north of the authored
line, which is the ground the +25 m lateral row scores ~0.88.
(ii) **The hop plan counts per object** (`plan_counts.py`: goals / planned / 0pts / parts), and
specifically whether the `Z2 -> Z3` goal was MESH-PLANNED (`Node Is destination in nav area?:
success` + `Planned path has N points`) as V8's `D1 -> D2` was with 15 points. If it was not, the two
runs are not in the same planning regime (falsifier (c)).
(iii) **The crawl**, binned per 200 sim s, against V8's `0:5.68 200:5.72 400:2.99 600:1.81 800:0.30
... 5400:0.25` for the leader.
(iv) The sim ratio (V8: ~5.9x), so wall-derived quantities are never compared
(`lessons-wall-derived-speed`).

### 4. FALSIFIERS

- **(a) A `ROUTE SHIFT` line appears.** The control is VOID - `Vrf__PreflightRouteShift=false` did
  not take. Nothing in the run may be compared to V8.
- **(b) The leader's cross-track exceeds +/- 15 m at s ~ 1,970.** The unit is not on the authored
  line; whatever it did, it did somewhere else.
- **(c) The `Z2 -> Z3` goal is NOT mesh-planned while V8's `D1 -> D2` was.** The two runs differ in
  planning regime as well as in lateral offset, and the comparison is broken in the direction that
  would make Branch A look true for the wrong reason.
- **(d) The live anchor differs from V8's 34.6582021/-116.7400919 by more than ~5 m.** The authored
  Z points then sit off the dispatched line and the "zero offset" is only approximately zero;
  measure and report it rather than assuming it.

### 5. THE COMPARISON, AT EQUAL SIM TIME

`lessons-compare-at-equal-sim-time` and `lessons-wall-derived-speed` govern this. Totals over the
run, wall-windowed verdicts and displacement / total-sim-seconds are all FORBIDDEN here; two of them
produced false headline claims on the nav-data run.

- **ONE AXIS FOR ALL THREE RUNS:** start `34.658442, -116.740092` (the reference / formation-leader
  anchor) -> V1 `34.651212159120796, -116.81163703922806`, north positive. That is
  `n2b_harvest.py`'s axis and V8's RESULTS are already on it. Do not re-anchor per run.
- **ONE CLOCK ORIGIN:** `sim0` = the sim time of the leader's FIRST `ground-vehicle-move-to` goal,
  as `n2b_harvest.py` computes it (V8: sim 53.7 at wall 35.7).
- **THE TABLE.** Leader along-track s at `sim0 + 300 / 600 / 900 / 1200 s`, against V8's measured
  **1,732 / 2,362 / 2,602 / 2,722 m**, and against V8b's when it lands. Plus, per run: cross-track at
  s = 1,970; closest approach to 34.65608/-116.76142; max s; net advance over the final 600 sim s;
  and the same six rows for every member.
- **THE READING IS A THREE-WAY, NOT A PAIR.** V8 (shift on, -125 m south), V8b (shift on, +250 m
  north, the fixed chooser) and V8z (insertion only, zero offset) differ in ONE axis each. If V8z is
  Branch A and V8b crosses, the lateral offset is established. If V8z is Branch B, V8 and V8b are
  both explained without it.

### 6. ADVERSARIAL REVIEW OF THIS PRE-REGISTRATION (before the run)

**The strongest objection: this control is not clean either.** True, and it must be said. The
control changes the route's SHAPE (5 hops instead of 1) exactly as V8 did, but it also changes the
GROUND under the tasked line back to the flagged face - which is the intended variable - while
leaving the *hop lengths* 33 m shorter on the two transits. A 33 m difference in a 216 m hop is not
plausibly load-bearing for a mesh query, but it is a difference and it is recorded rather than
waved away. The alternative - matching V8's hop lengths exactly by moving the stations - would
change the along-track positions of the waypoints relative to the face, which is worse.

**Second objection: "Branch B would not actually refute the lateral offset, because a zero-offset
insertion still gives the planner four fresh goals ON ground the sampler flags."** Correct, and that
is why Branch B is worded as "the shift's lateral component is NOT DEMONSTRATED" rather than
"refuted". A crossing here would mean V8's evidence for the lateral mechanism is gone, not that the
lateral mechanism is false. What would establish it positively is V8b crossing while V8z freezes.

**Third: the members are not the leader.** V8's own reading was 5 of 6 crossing and one freezing, so
a single-vehicle verdict here would be an artefact. Every prediction above is scored on the LEADER
and every member is recorded; a split result (some cross, some freeze) is a legitimate outcome and is
reported as such, against V8's split.

**What this run cannot settle.** The ~0.30 m/s crawl past `Pout` that all five V8 movers fell into -
it is unexplained, it will probably recur, and it is not evidence for either branch. Whether the
AO20 mesh would plan the 6.6 km leg at all if the destination gate passed (it does not: V1 is 206 m
outside the area, V7 P-D). And the false-alarm rate of the 0.92 threshold, which stays at nine legs
of one order.

**VERIFIED (computed this pass, offline, 0 tiles fetched).** The window s = 1,983.4-2,023.4 on the
live-anchor leg; the four stations and their lat/lon; the hop lengths of both routes; the seven leg
scores of both routes; the 3,973-4,649 m distances of the Z vertices from V0 (so only V0 is
dropped); `--parse-order` reading 8 points in the authored order with `action: MOVE`,
`embedded shape: Route`, `duration: 7200000 ms`, exit 0. **ASSUMED (not verified).** That this run's
de-stack reproduces V8's live anchor to within a few metres (falsifier (d) measures it). That the
`Z2 -> Z3` goal will be mesh-planned as V8's `D1 -> D2` was (falsifier (c) measures it). That
`Vrf__PreflightRouteShift=false` reaches the process - P-z1's first bullet is the check.

---

## RESULTS - run 20260915T033226Z (V8b, the fixed chooser)

Harvested 2026-09-15 after teardown (`VR-Forces is down` in `scratchpad\validation\v8b_runner.log`).
Read-only on `runs/`; no vendor sim log opened. Same fixture, init, order, env, gate, consoles and
offline cache as V8 - **the only difference is the VrfC2SimApp binary** (main `c5f166d`, the two-phase
chooser). The V8z zero-offset control is a separate run and is PENDING.

### V1. VERDICT TABLE - every R8 item of the V8 RESULTS

| R8 | required | measured | verdict |
|---|---|---|---|
| **1** | `ROUTE SHIFTED 250 m north - ratio 1.248 -> 0.761 (formation band max 0.862)` | **exactly that string**, leg 1 | **MET** |
| **2** | `nan 0` on every candidate | **13 candidates, every one `nan 0`** | **MET** |
| **3** | leader cross-track **>= +40 m NORTH** at s = 1,970 +/- 3, and past s = 2,100 | **+283.6 m NORTH** at the nearest fix (s = 2,026); max s **15,148 m** | **MET** |
| **4** | M3 1's fate (it froze in V8 38.3 m from the P11 point) | **M3 1 CROSSED** - +314.4 m north at s = 2,030, max s **15,166 m**, closest approach to the P11 point **328 m** | **MET - the strongest single result** |
| **5** | the zero-offset control | separate run; order + prereg committed `7490383`; **NOT YET RUN** | **PENDING** |
| **6** | the crawl at equal sim time vs V8 | **NO CRAWL**: all six hold **9.7-10.1 m/s** from sim 800 to the end of the window; V8 held 0.26-0.42 m/s over the same span | **MET - refutes the crawl for this line** |
| STOPs | no crash / `Tick phase FAILED` / `MissingMethodException` / double dispatch | **0 / 0 / 0 /** 1 `CreateRoute`, 1 `MoveAlongRoute`, 1 TASKSTRT | **CLEAN** |
| report | the shift ObservationReport on the bus | REPORT #232, 03:34:58.442Z, quoted in V3 | **MET** |
| warnings | `Vrf:PreflightWarnings` on the dispatched route | **no** `ROUTE PRE-FLIGHT ... leg N:` line and no error - nothing left flagged | as in V8 |
| teardown | clean | `VrfC2SimApp exited with code 0 (clean resign)`, `VR-Forces is down (graceful; RTI infrastructure preserved)`, `no VR-Forces processes remain`, rtiexec 69856 / rtiForwarder 50520 preserved | **CLEAN** |
| stop rule | `-StopWhenComplete` cannot fire at PT2H | did not fire; `observation window complete (903.6s used of 900s)`; 0 terminal reports | as predicted |

**Every number V8's RESULTS pre-registered for this run came out exactly right, to three decimals
and to the metre.** The live anchor was byte-identical to V8's
(`34.65820208652259, -116.74009186651882`, the unit's own first PositionReport), so the leg, the
base ratio and the candidate table are directly comparable.

### V2. THE SHIFT LINES, VERBATIM (`vrfc2simapp.log` 1289 / 1317 / 1319 / 1321)

    Task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1': ROUTE SHIFT check queued for 1-35/2/1_A~PXY (4 vertices); dispatch deferred to the result (timeout 30 s -> the authored line).
    Task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' (1-35/2/1_A~PXY) leg 1: ROUTE SHIFTED 250 m north - ratio 1.248 -> 0.761 (formation band max 0.862); inserted (34.658498,-116.760208) and (34.658116,-116.764116). STP's own vertices are unchanged and in order.
    Task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' (1-35/2/1_A~PXY) leg 1: ROUTE SHIFT candidates - +25 m ratio 1.022 nan 0 ratio 1.022 > 0.820; -25 m ratio 1.280 nan 0 ratio 1.280 > 0.820; +50 m ratio 0.829 nan 0 ratio 0.829 > 0.820; -50 m ratio 1.050 nan 0 ratio 1.050 > 0.820; -75 m ratio 0.836 nan 0 ratio 0.836 > 0.820; +75 m ratio 0.735 band 1.022 nan 0 formation band max 1.022 >= 0.92; +100 m ratio 0.835 nan 0 ratio 0.835 > 0.820; +125 m ratio 0.841 nan 0 ratio 0.841 > 0.820; +150 m ratio 0.942 nan 0 ratio 0.942 > 0.820; +175 m ratio 0.938 nan 0 ratio 0.938 > 0.820; +200 m ratio 0.863 nan 0 ratio 0.863 > 0.820; +225 m ratio 0.816 band 0.935 nan 0 formation band max 0.935 >= 0.92; +250 m ratio 0.761 band 0.862 nan 0 ACCEPTED
    Task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' (1-35/2/1_A~PXY): ROUTE SHIFT applied to 1 of 1 flagged leg(s); route 4 -> 8 vertices.

**THE CANDIDATE TRACE IS THE FIX, VISIBLE.** The order `+25, -25, +50, -50, -75` then `+75` is the
two-phase chooser at work: PHASE A walked the magnitudes on BOTH sides and stopped at 75 m, where
+75 cleared C1 at 0.735 and -75 did not (0.836 > 0.820) - so **NORTH won the side on C1 alone**.
PHASE B then walked NORTH only, and C2 pushed the magnitude from 75 (band 1.022) past 225 (band
0.935) to **250 (band 0.862)**. **No southward candidate was ever evaluated past 75 m.** In V8 the
identical C1 table led to `-125 m SOUTH` because C2 was allowed to choose the side. Every candidate
carries `nan 0`, which also closes R8 item 2 and re-confirms offline that the cache covers the band.

The inserted points reproduce the offline geometry to 1e-6: predicted `D1 34.658498,-116.760209` /
`D2 34.658116,-116.764117` against the logged `34.658498,-116.760208` / `34.658116,-116.764116`.
Terrain profile 167 authored all 8 vertices (`#0 34.65820,-116.74009 #1 34.65673,-116.75519
#2 34.65850,-116.76021 #3 34.65812,-116.76412 #4 34.65543,-116.76850 #5 V1 #6 V2 #7 V3`), then
`CreateRoute '... ROUTE' (8 pts)` and one `MoveAlongRoute`.

### V3. THE OBSERVATION REPORT (REPORT #232, 03:34:58.442Z)

    <Latitude>34.658498</Latitude> <Longitude>-116.760208</Longitude> (AltitudeMSL 1613.5)
    <Marking>ROUTE SHIFT: task T1_AOA_SE_1-35_AR;_2/1_AD_P1 (1-35/2/1_A~PXY) leg 1 - the authored
    line scored 1.248 against this unit's own limit; the interface detoured it 250 m north of the
    authored line around that window, scoring 0.761 (formation band max 0.862). STP's own vertices
    are unchanged and in order; the detour lies between them.</Marking>

### V4. THE SIX MEMBERS (same columns as V8's R5; leg axis = the reference axis, NORTH positive)

| member | slot | cross-track at s ~ 2,006 | closest approach to the P11 freeze point | max s | advance over final 600 sim s | verdict |
|---|---|---|---|---|---|---|
| M1A2 1 (leader) | 0 | **+283.6 m** (fix at s = 2,026) | 279 m | **15,148 m** | +5,669.6 m | **CROSSED** |
| M1A2 2 | -50 | +262.8 m (s = 1,998) | 234 m | 15,132 m | +5,670.3 m | CROSSED |
| M577A2 1 | -25 | +278.5 m (s = 2,021) | 263 m | 15,077 m | +5,664.2 m | CROSSED |
| HMMWV 1 | 0 | +265.4 m (s = 1,967) | 276 m | 15,112 m | +5,672.3 m | CROSSED |
| HMMWV 2 | +25 | +286.1 m (s = 2,023) | 289 m | 15,105 m | +5,675.3 m | CROSSED |
| **M3 1** | **+50** | **+314.4 m** (s = 2,030) | **328 m** | **15,166 m** | +5,668.7 m | **CROSSED** |

**SIX OF SIX.** In V8, five crossed and M3 1 - the +50 slot, the lane nearest the authored line -
froze 38.3 m from the P11 point. Here the whole formation is 234-328 m clear of that point and every
member is still doing ~10 m/s when the window closes. **That is C2's stated purpose demonstrated:**
the band condition exists so that no slot line sits on the face, and the slot that failed in V8 is
the one the +250 m shift rescued.

The detour was DRIVEN, not merely authored: the leader passed within **1 m of Pin, 1 m of D1, 1 m of
D2 and 5 m of Pout**. Every member reached s ~ 15.1 km, i.e. past V1 (6,590 m) and 8.5 km along
leg 2 toward V2 - the unit's own last PositionReport is `34.61688, -116.90009` at 9.5 m/s.

**HOP PLAN COUNTS (leader).** Ten goals, **zero `not enough (0) points` refusals for any member**
(`plan_counts.py`: 7-11 planned paths each, 0 zero-point):

| # | goal | `Planned path has` |
|---|---|---|
| 1 | offset-route start (s = 26 m) | 5 points |
| 2 | Pin (s = 1,392 m) | 21 points |
| 3 | s = 146 m (RE-GOAL, see V6) | 5 points |
| 4 | s = 204 m (RE-GOAL) | 8 points |
| 5 | Pin again | 10 points |
| 6 | D1 (s = 1,840 m) | 8 points |
| 7 | D2 (s = 2,198 m) | 6 points |
| 8 | Pout (s = 2,616 m) | 24 points |
| 9 | **V1** | `fail in action Is destination in nav area?` -> `1 parts` (driven STRAIGHT) |
| 10 | **V2** | `1 parts` (driven STRAIGHT) |

Goals 1-8 are inside AO20 and were all MESH-PLANNED; V1 and V2 are outside it and were driven
straight, exactly as V7 P-D and V8 measured. All six members logged
`fail in action Is current point in nav area?` at wall 338.9-344.5 s - that is them leaving AO20 on
the way to V2, expected and benign.

### V5. EQUAL SIM TIME - V8 vs V8b (leader, leg s in metres; `sim0` = the leader's first goal)

| since sim0 | V8 (shift -125 m south) | **V8b (shift +250 m north)** |
|---|---|---|
| 300 s | 1,732 | **695** |
| 600 s | 2,362 | **2,191** |
| 900 s | 2,602 | **4,251** |
| 1,000 s | 2,639 | **5,253** |
| 1,200 s | 2,722 | **7,111** |
| end of track | 4,138 (sim 5,556) | **15,148 (sim 2,095)** |

Leader speed per 200 sim s (m/s along path):

    V8   0:5.68  200:5.72  400:2.99  600:1.81  800:0.30  1000:0.41 ... 5400:0.25
    V8b  0:1.42  200:6.71  400:4.08  600:5.76  800:10.03 1000:9.41 1200:10.01 1400:9.95
                 1600:9.99 1800:9.97 2000:9.80

**THE CROSSOVER IS AT SIM ~700.** Before it V8 is ahead; after it V8 collapses to 0.3 m/s and never
recovers while V8b settles at ~10 m/s and holds it for 1,300 sim s. The asymmetric track lengths are
a WINDOW artefact, not a speed artefact: the sim ratio was **2.20x** in V8b (sim 41 -> 2,096 in
931 wall s) against **5.92x** in V8 (sim 49 -> 5,557 in 930 wall s), so the same 900 s wall window
bought V8b only 2,055 sim s. Comparisons above are therefore taken at equal SIM time only
(`lessons-compare-at-equal-sim-time`, `lessons-wall-derived-speed`).

### V6. THE SLOWER FIRST 300 SIM S - IT HOLDS, AND IT IS NOT LOAD

The seat's provisional mid-window read is **confirmed in both halves**: ~10 m/s from sim 800 on, and
a slower start (leg s 695 m at sim0+300 against V8's 1,732 m; the seat's "694 m" and "~14.8 km" are
right to the metre and to 2 %). The mechanism is in the goal stream, on the sim clock:

| sim | V8 leader leg s | V8b leader leg s |
|---|---|---|
| 100 | 31 m | 38 m |
| 150 | 276 m | 91 m |
| 200 | 819 m | 150 m |
| 250 | 1,215 m | 207 m |
| 300 | 1,613 m | 341 m |
| 400 | 1,815 m | 1,307 m |
| 600 | 2,227 m | 2,026 m |

V8's leader was given the `Pin` goal at sim 75.6 (22-point plan) and drove it without interruption to
the D1 goal at sim 305.7. **V8b's leader was given `Pin` at sim 67.5 (21-point plan) and was then
RE-GOALED twice to points only 146 m and 204 m along the leg, at sim 188.5 (5-point plan) and
sim 207.7 (8-point plan), before being re-issued `Pin` at sim 246.2 (10-point plan) and reaching the
D1 goal at sim 412.2.** Between sim 100 and sim 250 it advanced 38 -> 207 m, about 1.1 m/s. That
re-plan cycle IS the whole 1,037 m deficit at sim0+300, and V8b had made it up by sim 600.

**WHY the re-goal is OPEN.** Two candidates and this capture cannot separate them: the
maneuver-in-formation controller recomputing the leader's own `M1A2 1's Offset Route` after the
route changed, or the boundaries of a multi-part path (the leader logged 2 `Planned path has N parts`
rows). The standing limitation applies - the object console prints COMMANDS, never the vehicle's
response (`lessons-vendor-diagnostics-first`). It did not cost the run anything: the deficit was
closed by sim 600 and V8b ended 11 km ahead of V8.

**IT IS NOT LOAD, and here is why that is decidable.** Every quantity above is binned on the SIM
clock, so wall-clock contention cannot move it; what contention CAN move is the sim RATIO and hence
how much sim time fits in a 900 s wall window. Recorded honestly: **this executor's own V8z work ran
INSIDE V8b's observation window** (03:34:55-03:50:28Z) - python leg-scoring passes over the committed
tile cache, one `VrfC2SimApp --parse-order`, and a git commit at 03:39:20Z - which is a violation of
the standing "never run agents during a timed run" rule (`vrf-navigation-data-headless`, G3). Other
executors' builds are not visible from this run's own evidence and are not claimed either way. The
run's `--sample-threads` produced **no thread-sample rows** in either the runner log or the app log
(an observability gap worth a row of its own); the manifest carries only `inputs/sampleSecs = 2` and
`oracle/appThreads = 19`. The far larger and non-contentious driver of the ratio difference is
intrinsic: `fixed-frame-run-to-complete` is load-bound by design
(`vrf-frame-mode-not-multiplier`), and V8b's six vehicles drove 15 km with continuous mesh
re-planning while V8's sat at 0.3 m/s.

### V7. ADVERSARIAL REVIEW (HEAVY)

**Strongest competing explanation for the crossing: WAYPOINT INSERTION, not the lateral offset.**
Both V8 and V8b insert four waypoints inside the nav area and both turn leg 1 into short
mesh-planned hops, so insertion alone cannot be what separates them - and that is the point worth
being precise about. **The V8/V8b pair already isolates the LATERAL component** with insertion held
approximately constant: same 8-vertex structure, same four-point shape, same mesh-planned regime,
0 planning refusals in both, and the *only* substantive difference is where the two offset vertices
sit (-125 m south at 0.661 vs +250 m north at 0.761). One froze a member and crawled at 0.3 m/s; the
other put all six through at 10 m/s. What the pair does NOT establish is whether insertion is
*necessary* at all - whether the authored line, cut into the same five hops at zero offset, would
also cross. That is exactly and only what V8z answers. The honest statement today: **lateral
position demonstrably matters; whether insertion is also required is untested.**
Caveat on "approximately constant": the transits differ (216.5 m vs 433.0 m), so V8b's `Pin` is at
s = 1,390 and its hops are `1390 / 500 / 360 / 500 / 3974` against V8's `1607 / 250 / 360 / 250 /
4190`. Same shape, not identical lengths.

**Second competing explanation, for the SPEED difference: load.** Answered above on its merits -
every compared quantity is on the sim clock - but the exposure is real and is disclosed rather than
argued away, including this executor's own concurrent work inside the window.

**Third: "V8b simply had a luckier line."** The sampler scored +250 m north at 0.761 with a band max
of 0.862 BEFORE the run, offline, on the committed cache, and the live chooser reproduced that to
three decimals; the offset also sits inside the +50..+550 m corridor PREREG_RIDGE_AG 3.3 measured
clear, where +250 scored 0.709. So the line was predicted clear by an instrument calibrated on nine
legs, not found clear after the fact. That is not luck, but it is still ONE leg on ONE terrain, and
L2's open false-alarm question is untouched.

**What this run does NOT show.** No TASKCMPLT: the task's final point is V3 and the unit was still on
leg 2 when the window closed, so V8c-style "arrival" is not demonstrated - only that it passed V1 and
kept going. The crawl is refuted FOR THIS LINE; V8's own crawl past `Pout` is still unexplained as a
mechanism, and if it is a property of that ground rather than of the vehicles then V8b simply never
met it. The `Entity not embarked on same object as target` message seen once in V8 did not recur
here (0 occurrences), which is consistent with it being incidental, not caused by insertion.

**VERIFIED (measured this pass).** The four shift lines verbatim and the full candidate trace with
`nan 0` on all 13. The live anchor identical to V8's. The inserted points against the offline
geometry to 1e-6. The ObservationReport. The 8-vertex terrain profile, `CreateRoute (8 pts)`, one
`MoveAlongRoute`, one TASKSTRT, 0 TASKCMPLT/TASKABRT. 0 `Tick phase FAILED`, 0
`MissingMethodException`, 0 crash. The six tracks, cross-tracks, closest approaches, max s, final-600
advance and speed bins. The leader's ten goals with their plan-point counts and the two out-of-area
`1 parts` goals. The 0 zero-point refusals for all six. The early-progress table and the re-goal
timeline on the sim clock. Sim ratios 2.20x / 5.92x. Clean teardown with RTI preserved and
`-StopWhenComplete` not firing.

**ASSUMED (not re-verified).** That `Vrf:PreflightWarnings`' worker ran and found nothing flagged -
inferred from the absence of both a flagged-leg line and an error, because that path prints nothing
when nothing is flagged. That the two candidate mechanisms for the sim-100-250 re-goal are the only
plausible ones. That no other executor's work loaded the machine during the window - not claimed,
simply not visible from this run's evidence. That +250 m north is representative of the northern
corridor rather than a single fortunate line (one leg, one terrain).

---

## RESULTS - run 20260915T041036Z (V8z, the zero-offset control)

Scored against the PREREG V8z section above, exactly as registered. Read-only on `runs/`; no vendor
sim log opened. **Two earlier directories, `20260915T035132Z_run` and `20260915T035134Z_run`, are a
VOIDED double-launch pair (created 2 s apart) and are not scored.**

**TEARDOWN CAVEAT, not a defect of the measurement.** Windows rebooted at ~04:31Z, moments after the
observation window closed. The runner had reached `StopVrf: waiting for the DIRECT CHILD only` and
never printed `VR-Forces is down`, so the final StopVrf stage and the manifest's completion flags are
absent. Everything the measurement rests on completed first: `observation window complete (902.6s
used of 900s)`, StopIface ran, `ListenReports: EXIT=0`, the observers resigned, and
`watchvrf-trace.csv` carries 362,407 rows to wall 995.5 s with `vrfc2simapp.log` and
`reports-captured.log` intact. Nothing below reads the manifest.

### Z1. VERDICT TABLE

| prediction | required | measured | verdict |
|---|---|---|---|
| **P-z1** GATE | **no** `ROUTE SHIFT` line of any kind | **0 occurrences** in the whole log | **MET** |
| P-z1 | 8-vertex route | `terrain profile request 167 sent for 8 vertices`; all four Z vertices present in order; `CreateRoute '... ROUTE' (8 pts)`; one `MoveAlongRoute` | **MET** |
| P-z1 | **exactly one** `ROUTE PRE-FLIGHT ... leg 3` warning | exactly one, quoted in Z2 - **40 m of 0.939 on sand, limit 0.752, ratio 1.25**, at 34.6561/-116.7618, plus `1 flagged leg(s) of 7 checked; sent 1 ObservationReport(s)` | **MET, to the digit** |
| **P-z2** CONTROL HOLDS | leader cross-track within **+/- 15 m** at s = 1,970 +/- 3 | **-7.0 m** at the nearest fix (s = 1,988) | **MET** |
| **P-z3** | **Branch A**: < 150 m of the P11 point AND < 20 m over the final 600 sim s | leader **19.7 m** and **+1.7 m**; four more members 8.4-38.6 m and -1.3 to +1.8 m | **BRANCH A** (5 of 6) |
| P-z3 | Branch B: max s > 3,000 m | max s **1,934-1,999 m** for five; HMMWV 2 reached 2,466 m - **neither branch**, see Z4 | **not Branch B** |
| **P-z4** | recorded | M3 1, hop plan counts, the crawl - Z4 | RECORDED |
| falsifiers | (a) a shift line; (b) cross-track > 15 m; (c) the Z2 -> Z3 hop not mesh-planned; (d) live anchor moved > ~5 m | **none fired** - see Z6 | **CLEAN** |
| STOPs | crash / `Tick phase FAILED` / `MissingMethodException` / double dispatch | **0 / 0 / 0 /** 1 `CreateRoute`, 1 `MoveAlongRoute`, 1 TASKSTRT, 0 terminal | **CLEAN** |

### Z2. THE PRE-FLIGHT LINES, VERBATIM (`vrfc2simapp.log` 1547 / 2309) - and the silence that matters

    ROUTE PRE-FLIGHT task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' (1-35/2/1_A~PXY) leg 3: 40 m of 0.939 on sand at 34.6561/-116.7618, 0.2 km along the leg; Tank Headquarters Section (USA) limit 0.752 (max-slope 0.94 x soil 0.80); PREDICTED IMPASSABLE (pre-flight estimate, ratio 1.25 vs threshold 0.92).
    ROUTE PRE-FLIGHT task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' (1-35/2/1_A~PXY): 1 flagged leg(s) of 7 checked; sent 1 ObservationReport(s).

**`grep -c "ROUTE SHIFT" = 0`.** The feature was off and stayed off; the control is a control.

The ObservationReport is on the bus (`reports-captured.log`):

    <Marking>ROUTE PRE-FLIGHT: task T1_AOA_SE_1-35_AR;_2/1_AD_P1 leg 3 - 40 m of sustained 0.939
    rise-over-run on sand, 0.18 km along the leg; Tank Headquarters Section (USA) limit 0.752
    (max-slope 0.94 x soil 0.80). PREDICTED IMPASSABLE (pre-flight estimate, ratio 1.25 vs
    threshold 0.92).</Marking>

Terrain profile 167 authored all eight vertices and every authored Z point is there, in order:
`#0 34.65820,-116.74009 (live) #1 34.65650,-116.75754 (Z1) #2 34.65627,-116.75989 (Z2)
#3 34.65589,-116.76380 (Z3) #4 34.65566,-116.76615 (Z4) #5 V1 #6 V2 #7 V3`.

### Z3. THE SIX MEMBERS (same columns as R5/V4; axis = the reference anchor -> V1, NORTH positive)

| member | slot | cross-track at s ~ 2,006 | closest approach to the P11 freeze point | max s | last s | advance over final 600 sim s | verdict |
|---|---|---|---|---|---|---|---|
| **M1A2 1 (leader)** | 0 | **-7.0 m** (fix at s = 1,988) | **19.7 m** | 1,988 | 1,974 | **+1.7 m** | **FROZE (Branch A)** |
| M1A2 2 | -50 | -57.9 m (s = 1,999) | 38.6 m | 1,999 | 1,983 | -1.3 m | FROZE |
| M577A2 1 | -25 | -25.6 m (s = 1,934) | 36.5 m | 1,934 | 1,931 | -0.5 m | FROZE |
| HMMWV 1 | 0 | -18.7 m (s = 1,994) | **8.4 m** | 1,994 | 1,977 | +0.2 m | FROZE |
| **M3 1** | +50 | -7.9 m (s = 1,988) | **19.0 m** | 1,988 | 1,974 | +1.8 m | **FROZE** |
| HMMWV 2 | +25 | +55.3 m (s = 1,995) | 8.1 m | **2,466** | 2,466 | **+323.3 m** | **neither** (Z4) |

The cross-track spread (-57.9 to +55.3 m) is the formation's own `rightOffset` band of +/- 50 m; the
prediction is scored on the LEADER, which sat **7 m** from the authored line. **The unit stopped on
the line six earlier runs stopped on, at the point they stopped at.**

### Z4. P-z4 - WHAT WAS RECORDED

**M3 1.** It froze 19.0 m from the P11 point, on its +50 slot line. Across the three runs, the same
vehicle: **V8 froze at 38.3 m** (south shift), **V8b crossed to s = 15,166 m** (north shift),
**V8z froze at 19.0 m** (no shift). One vehicle, three lines, three outcomes that track the lateral
offset and nothing else.

**HOP PLAN COUNTS - the planning regime is IDENTICAL to V8 and V8b.** **Zero** `not enough (0)
points` refusals for any member; every goal passed `Node Is destination in nav area?: success`.
Goals landing within 60 m of an authored vertex: **five of six members were goaled to BOTH Z2 and
Z3** (HMMWV 1, HMMWV 2, M1A2 2, M3 1, M577A2 1), and HMMWV 2 alone went on to Z4 and V1. The leader's
own eleven goals planned `5 / 22 / 10 / 7 / 18 / 5 / 5 / 5` points. So the inserted waypoints WERE
turned into short mesh-planned hops here exactly as in V8 and V8b - and the units froze anyway.

**THE FREEZE SIGNATURE.** `Starting condition node Is path blocked? / fail in action Is path
blocked?` repeats **5,323-5,887 times** for each of five members to the end of the run (806 for the
leader, which drives its own offset route): the "running, unblocked, goal unchanged, forever"
signature of FINDING_EARLY_STOPS sec 7, P11, G3, G5, G6 and V8's M3 1.

**THE CRAWL, per 200 sim s (m/s along path).** Every member collapses at sim ~700-800 and never
recovers:

    M1A2 1    0:1.84  200:3.25  400:6.37  600:1.87  800:0.56  1000:0.43 ... 2000:0.28
    M3 1      0:2.19  200:3.29  400:6.37  600:1.86  800:0.56  1000:0.43 ... 2000:0.28
    HMMWV 1   0:2.01  200:2.89  400:5.66  600:0.99  800:0.03 ... 2000:0.11
    M577A2 1  0:1.64  200:4.04  400:5.20  600:0.57  800:0.05 ... 2000:0.06
    HMMWV 2   0:2.13  200:3.32  400:5.20  600:1.23  800:1.51  1000:0.27 ... 2000:0.40

**THE ONE EXCEPTION, reported as NEITHER and not forced into a branch.** HMMWV 2 (the +25 slot, whose
own line sits ~55 m north of the authored line here) crawled **478 m past** the freeze point to
s = 2,466 m at 0.2-0.95 m/s. That is 534 m short of Branch B's 3,000 m and 16x slower than V8b's
10 m/s. It is a partial effect of being a little further north - the same variable V8b tested at
250 m - not a crossing.

Sim ratio **2.16x** (sim 43 -> 2,052 in 931 wall s), against V8b's 2.20x and V8's 5.92x.

### Z5. EQUAL SIM TIME - THE THREE RUNS (leader leg s in metres; one axis, one clock origin)

Axis: reference anchor `34.658442,-116.740092` -> V1. Clock origin: the leader's first
`ground-vehicle-move-to` goal (V8 sim 53.7, V8b sim 47.8, V8z sim 44.1).

| since sim0 | V8 (shift -125 m SOUTH) | V8b (shift +250 m NORTH) | **V8z (insertion, ZERO offset)** |
|---|---|---|---|
| 300 s | 1,732 | 695 | **696** |
| 600 s | 2,362 | 2,191 | **1,959** |
| 900 s | 2,602 | 4,251 | **1,970** |
| 1,000 s | 2,639 | 5,253 | **1,973** |
| 1,200 s | 2,722 | 7,111 | **1,973** |
| end of track | 4,138 (sim 5,556) | **15,148** (sim 2,095) | **1,988** (sim 2,052) |

Two readings fall out of this table. **First: V8z is flat from sim0+900 onward** - 1,970, 1,973,
1,973 - which is a stop, not a slow crossing. **Second: V8z's sim0+300 figure (696 m) is within 1 m
of V8b's (695 m) while V8's is 1,732 m.** The slow first 300 sim s that V8b's RESULTS left OPEN is
therefore NOT a property of the lateral offset: it appears at +250 m north and at zero offset and is
absent at -125 m south, on the same ground, from the same anchor. V8z shows the same early re-goal
cycle (the leader was goaled to Z1 at sim 68, then back to s = 133 / 184 / 681-700 between sim 172
and 322). Best reading: a run-to-run vendor formation/replan behaviour, seen in 2 of 3 runs,
attributable to neither variable. V8b's RESULTS sec V6 should be read with that correction.

### Z6. THE FOUR FALSIFIERS - CHECKED, NONE FIRED

- **(a) a `ROUTE SHIFT` line of any kind** - `grep -c` = **0**. The control was not a shift run.
- **(b) leader cross-track > +/- 15 m at s ~ 1,970** - **-7.0 m**. The unit drove the authored line.
- **(c) the `Z2 -> Z3` hop not mesh-planned while V8's `D1 -> D2` was** - five of six members were
  goaled to Z2 AND Z3, every goal passed the destination-in-nav-area gate, and there were **0**
  zero-point refusals anywhere. Same regime, so the comparison is not broken in the direction that
  would make Branch A look true for the wrong reason.
- **(d) the live anchor moved** - the unit's first PositionReport (04:13:09.989Z) is
  `34.65820208652259, -116.74009186651882`, **byte-identical to V8's and V8b's**. The authored Z
  points sit exactly on the dispatched line; "zero offset" is exactly zero.

### Z7. ADVERSARIAL REVIEW (HEAVY)

**The competing explanation this run was built to kill: "waypoint insertion, not the lateral offset,
is what carried units across in V8b."** It is dead. V8z inserts the same four points at the same
along-track stations, gets the same 8-vertex route, the same mesh-planned hops and the same zero
planning refusals - and five of six members stop within 8-39 m of the point six earlier runs stopped
at, with less than 2 m of movement in the final 600 sim s. Insertion alone does nothing.

**Strongest surviving objection: the machine rebooted at ~04:31Z, minutes after the window closed -
was it already degrading during the run?** Cannot be excluded outright, and it is recorded. Three
things bound it: every quantity above is binned on the SIM clock, so wall-clock contention cannot
move it; the sim ratio (2.16x) is within 2 % of V8b's (2.20x), so the simulator was not visibly
starved; and a freeze is not a slowdown - the leader moved 1.7 m in 600 sim s while the `Is path
blocked?` loop ran 5,000+ times, which is the vendor's own "running and unblocked" state, not a
stalled host.

**Second objection: HMMWV 2 got 478 m past, so insertion is not completely inert.** Correct, and it
is reported as NEITHER rather than folded into Branch A. But it crawled that distance at 0.2-0.95 m/s
on the +25 slot line - 55 m north of the authored line here - and stopped 534 m short of the Branch B
threshold. That is more evidence for the lateral variable, not against it.

**Third: does the sampler's window match where they actually stopped?** Yes, and this is the
control's incidental confirmation of the instrument. The flagged 40 m window sits at s ~ 1,983-2,023
on the reference axis (the warning puts it 0.18-0.2 km into the 360 m Z2 -> Z3 leg); the six members'
last fixes are at s = 1,931-1,983 and their furthest reach 1,934-1,999 (HMMWV 2 excepted). They stop
at the **TOE**, 0-52 m short of the window - exactly the 31.5-44.0 m the record measured
(PREREG_RIDGE_AG 3.2) and exactly why `PadMeters` is 50.

**What would still falsify the attribution.** A V8b repeat that freezes (the north crossing is one
run); a V8z repeat that crosses (the freeze is one run); evidence that the +250 m north corridor is
drivable for a reason unrelated to slope, which would leave the sampler right by accident; or a
demonstration that the early re-goal cycle - present in V8b and V8z, absent in V8 - is itself doing
work rather than being vendor noise. None of these is addressed by the three runs in hand.

**VERIFIED (measured this pass).** Zero `ROUTE SHIFT` lines. The single leg-3 warning and its
ObservationReport, verbatim. The 8-vertex terrain profile with all four Z points in order,
`CreateRoute (8 pts)`, one `MoveAlongRoute`, one TASKSTRT, 0 terminal reports. 0 `Tick phase
FAILED`, 0 `MissingMethodException`, 0 crash. The six tracks, cross-tracks, closest approaches, max
and last s, final-600 advance and per-200-sim-s speeds. The goal/plan stream, the 0 zero-point
refusals, the Z2/Z3 goals for five of six, and the 5,323-5,887-row `Is path blocked?` loops. The live
anchor byte-identical to V8's and V8b's. Sim ratio 2.16x. The runner's `observation window complete
(902.6s used of 900s)` and the absent StopVrf stage.

**ASSUMED (not verified).** That the reboot did not degrade the host during the window - argued from
the sim ratio and the freeze signature, not proven. That the early re-goal cycle is vendor noise
rather than a mechanism - it is consistent across V8b and V8z but its cause is unread (the console
prints commands, never the vehicle's response). That one run per condition is enough to rank the
three; each of V8, V8b and V8z is n = 1. That the two voided 0351xxZ directories contain nothing that
contradicts this - they were not opened.

---

## ATTRIBUTION (V8 / V8b / V8z)

Three runs, one leg, one start, one anchor (`34.65820208652259,-116.74009186651882`, byte-identical
in all three), the same 8-vertex inserted-waypoint route and the same mesh-planned regime with zero
planning refusals. They differ in ONE thing: where the two offset vertices sit.

| run | lateral offset | leader outcome | formation |
|---|---|---|---|
| V8 | **-125 m SOUTH** (0.661) | stopped at s = 4,138, crawling 0.3 m/s | 5 of 6 past, M3 1 froze at 38.3 m |
| V8b | **+250 m NORTH** (0.761) | s = 15,148 at ~10 m/s, past V1 | **6 of 6 crossed** |
| V8z | **ZERO** (the authored line, 1.248) | **froze 19.7 m from the P11 point**, +1.7 m in 600 sim s | 5 of 6 froze; 1 crawled 478 m past |

**The lateral offset is the remedy.** Waypoint insertion is the DELIVERY MECHANISM and is inert on
its own (V8z); the SIDE and SIZE of the offset decide the outcome (V8 vs V8b); and the sampler that
picks them located the face correctly, since the V8z units stopped at its toe, 0-52 m short of the
window it flagged. M3 1 alone makes the chain visible: same vehicle, same slot, froze / crossed /
froze as the offset went south / north / none. Each condition is n = 1, and the three runs cannot
speak to other legs, other terrain, or the threshold's false-alarm rate.
