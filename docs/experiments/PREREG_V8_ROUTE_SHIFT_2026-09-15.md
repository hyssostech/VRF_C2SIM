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

## RESULTS - not run
