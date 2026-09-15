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
