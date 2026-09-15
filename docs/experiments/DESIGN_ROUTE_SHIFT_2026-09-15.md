# DESIGN: the pre-flight LATERAL ROUTE SHIFT - the ridge remedy (2026-09-15)

STP-804 / STP-806. Tier HEAVY: this changes WHERE units drive. PLAN gate - this note is
written and committed BEFORE the code, and the supervisor reads it before the code lands.
Status: DESIGN ONLY. Nothing here has been run against VR-Forces. The feature ships OFF
(`Vrf:PreflightRouteShift` defaults false); sec 9 is the prereg draft for the run that would
earn it a default.

Author: Opus executor, worktree `feat/route-shift` off `main` `7afd9bf`.

## 0. THE ONE-PARAGRAPH CLAIM

STP authors a leg as a straight line between two vertices 4-6 km apart. The interface has
always driven exactly that line, and the vendor's Move Along Route drives it literally: "The
Move Along Route task takes a route object and performs a sequence of movements directly to
each vertex of the route. There is no path planning done as it moves toward the next vertex."
(`C:\MAK\vrforces5.2d\doc\help\Content\ConceptsEntityLevel\GroundVehMove\vrf_MoveAlongRouteTaskFunctionality.htm`).
On the Mojave AO that line runs 1-35 into 55 m of sustained 0.70-0.95 rise-over-run on sand
and the unit stops there for the rest of the run (FINDING_EARLY_STOPS sec 7). The freeze is a
property of the LINE, not of the unit, the terrain or the mesh: N2d drove the SAME unit, the
SAME order and the SAME vertices on the authored V0->V1 line 1.3 km north and completed V1
and V2 for the first time in any run (PREREG_N1_N2 sec 10.3); 4-27's leader froze in P11 and
crossed in G3 on a line 15 m away (FINDING sec 7e). The remedy is therefore a LINE remedy:
score the leg before dispatch, and where it is flagged, insert waypoints that carry the
path laterally onto ground the same sampler scores as clear - keeping STP's own vertices, in
STP's own order, and reporting the detour to the C2 side so nothing is done silently.

## 1. WHAT IS ALREADY THERE (and what this adds)

Already on `main`, from DEMO_READINESS row 20:

- `tools/preflight/leg_check.py` - the offline sampler and the calibration.
- `src/VrfC2SimApp/Preflight/*` - the C# port: `TileSource` (TMS 149 L13 + land cover, disk
  cache), `SoilChain`, `VendorSms` (the unit's own min max-slope), `LegScorer` (the PURE
  metric: worst mean-uphill over a sliding 40 m window / (max-slope x soil factor), flagged
  at >= 0.92), `PreflightService` (sampling + orchestration), `PreflightReports` (the C2SIM
  ObservationReport pair).
- `VrfC2SimService.QueuePreflight` - scores the FINAL dispatched route on a worker AFTER the
  bridge call and pushes one ObservationReport pair per flagged leg. It is a warning channel:
  "it never refuses, delays or alters the task".
- `--preflight-selftest` - the fixture comparison of the port against the python tool, run
  offline on `tools/preflight/preflight_cache`.

What this design ADDS is the first thing in the pre-flight that changes the simulation: a
PRE-DISPATCH stage that can insert waypoints. Everything else above is reused unchanged - the
same sampler, the same window, the same threshold, the same calibration. No number in the
calibration moves.

## 2. DESIGN QUESTION 1 - WHERE THE SHIFT LIVES

### 2.1 Waypoint insertion, not vertex replacement

DECISION: the shift INSERTS waypoints between STP's vertices. It never moves, drops or
reorders an authored vertex.

Argument:

1. **It preserves STP's intent exactly.** Every authored vertex is still a vertex, still in
   order, still arrived at. A moved vertex would silently redefine the objective the order
   names; an inserted waypoint only changes the PATH BETWEEN two objectives, which is the
   part STP never specified. The C2SIM order says where to be, not which side of a ridge to
   pass.
2. **It is the vendor's own mechanism for the job.** Move Along Route does no path planning
   between vertices, so a vertex is the only lever that changes the driven line, and
   `createRoute` takes an arbitrary ordered vertex list with no cap and no ordering
   constraint (`include/vrfcontrol/vrfRemoteController.h:1023-1038`; classref
   `DtVrfRemoteController::createRoute`). The units already drive 4-point routes; a 6-point
   route is the same call.
3. **It composes with the maneuver-in-formation subtask.** "Each subordinate computes an
   offset route and then traverses it by planning a path to each vertex in sequence"
   (`Tasks/MovementTasks/ManeuverAlong.htm`). Inserted vertices move every slot line with
   them, which is exactly what a lane remedy needs - and is why sec 4 scores the band.
4. **It is reversible and inspectable.** The authored route and the shifted route differ by
   two points per flagged leg; the log and the ObservationReport name both.

REJECTED ALTERNATIVES, with the reason:

- **Move the far vertex onto clear ground** - changes STP's objective. Refused outright.
- **Give the whole leg a parallel offset** (what PREREG_RIDGE_AG 3.3 measured) - moves BOTH
  endpoints, i.e. moves the vertices. The table is the evidence that a cleared side exists;
  it is not the shape of the remedy.
- **Hand the leg to the vendor's mesh instead** - already measured and unavailable here: the
  abstract-graph override plans 5 km legs on the eastern lane and refuses every long goal in
  the western one, 8-10 refusals per member (PREREG_N1_N2 sec 10.3). The mesh remains the
  better long-run answer and this feature does not block it: a leg the mesh plans is a leg
  whose driven path is not our polyline anyway.
- **Prefer Roads** (FINDING sec 8 option (a)) - needs a native facade method and changes the
  movement strategy for the whole task. Not excluded; out of scope here.

### 2.2 WHERE in the dispatch path, and on WHICH thread

The constraint that decides this: a cold leg FETCHES TERRAIN TILES OVER HTTP, and
`PreflightService`'s own header says it must never run on the VR-Forces tick thread. But the
shift must be applied BEFORE `CreateRoute`. So the shift cannot be a synchronous call in
`ExecuteTaskOnTick`, and it cannot be the existing fire-and-forget worker either.

DECISION: the shift is a DEFERRED DISPATCH STAGE, built on the plumbing that already exists
for exactly this shape - the terrain-profile continuation (`VrfC2SimService`, D1 /
`DeferredDispatch.Run`). The sequence for a ground task with route points becomes:

    pass 1 (tick)   live start -> ORIGIN VERTEX DROP -> routeGeo built
                    -> route shift ENABLED and the route not yet shifted?
                       copy the lat/lon, queue a worker, RETURN (nothing marked)
    worker (pool)   score each leg; for each flagged leg choose an offset and build the
                    detour; enqueue ONE tick action with the result (shifted or unchanged)
    pass 2 (tick)   re-entry with the shifted route -> the existing TerrainProfile request
                    (which now authors altitudes for the INSERTED vertices too) -> RETURN
    pass 3 (tick)   the terrain continuation -> CreateRoute + MoveAlongRoute + MarkDispatched

Three consequences, all deliberate:

- **The shift runs BEFORE the terrain profile**, so the inserted vertices get their altitudes
  from the same `GroundWaypointAltitudeMode` path as every other vertex. Shifting after the
  profile would leave two vertices with invented altitudes.
- **Nothing is marked dispatched until the final pass**, which is already the invariant of
  the terrain path; the re-entry does the bookkeeping exactly once.
- **The ending is `DeferredDispatch.Run`**, so a throw inside the continuation produces the
  same ERROR + `NotifyAbandoned` + one TASKABRT as every other deferred dispatch. A route
  shift that throws must never park a task silently (the D1 defect).

FAILURE AND TIMEOUT. The worker is bounded by `Vrf:PreflightRouteShiftTimeoutSeconds`
(default 30). Three ways out, all of which dispatch the task:

1. the worker finishes -> continue with the shifted (or unchanged) route;
2. the worker throws -> log ERROR, continue with the AUTHORED route;
3. the deadline passes -> a tick-loop sweep continues with the AUTHORED route.

(2) and (3) are races with (1), so the pending record carries a one-shot latch
(`Interlocked.CompareExchange`): whoever claims it enqueues the continuation, and the loser
does nothing. A double continuation would dispatch the task twice. This is the single most
dangerous defect this design can produce and the latch is the whole answer to it.

THE FEATURE NEVER REFUSES A TASK. Every failure mode above ends in the task being dispatched
on the line STP authored - i.e. in today's behaviour. That is the safety property: turning the
shift on can change WHICH line is driven; it can never stop a unit being tasked.

## 3. DESIGN QUESTION 2 - THE GEOMETRY

### 3.1 The detour  [REVISED 2026-09-15 after the build measured it - see 3.1a]

For a flagged leg A -> B of length `legLen`, the scorer returns the worst 40 m window
[s0, s1] in along-leg metres. FOUR points are inserted:

    ein     = max(0, s0 - PAD)             the padded window's near edge
    eout    = min(legLen, s1 + PAD)        its far edge
    transit = |X| / tan(MAXTURN)           the run over which the path changes lane
    D1   at (ein  - LEAD)  offset X        start of the parallel stretch
    D2   at (eout + LEAD)  offset X        end of it
    Pin  at (ein  - LEAD - transit)        ON the authored line
    Pout at (eout + LEAD + transit)        ON the authored line
    route: A -> Pin -> D1 -> D2 -> Pout -> B

The approach A -> Pin and the run-out Pout -> B lie ON the authored line: they are the ground
the interface has always driven there, unchanged.

X is signed: positive = to the RIGHT of travel. On the 1-35 leg (bearing 263.02) right is
NORTH, which is the cleared side (PREREG_RIDGE_AG 3.3).

The lateral offset is taken perpendicular to the LEG bearing in the local tangent plane -
the same construction `READ_4-27_G3_AND_OFFSET_SCORING` sec 2.1 used for its offset-line
generator, so the numbers in that record and the numbers here are on one scale (reproduced:
sec 6).

### 3.1a WHY FOUR POINTS AND NOT TWO - a decision REVERSED BY ITS OWN MEASUREMENT

This note first specified TWO inserted waypoints (D1 and D2 only), with the lateral transit
absorbed into the whole approach A -> D1. That form was BUILT, MEASURED on the reference leg,
and REJECTED. The measurement, on the same sampler and the same cache as everything else here:

| along the leg | the AUTHORED line scores | the 2-point detour's approach scores |
|---|---|---|
| s 0 - 1,800 m (the approach) | 0.608 | 0.79 - 0.86, worst at s ~ 1,650 m |
| s 1,800 - 2,300 m (the face) | 1.097 | removed by the detour |
| s 2,300 - 6,593 m | 0.346 | unchanged |

A 1.8 km diagonal does not stay beside the authored line - it drifts across the ridge's own
shoulder and picks up 0.79-0.86 on ground the authored line crosses at 0.608. The consequence
was not cosmetic: under C1 (sec 4.1) EVERY northward candidate then scored 0.82-0.88 and was
refused, and the search ran on to a **150 m SOUTHWARD** shift, which scored 0.695 because its
diagonals happened to dodge that shoulder too. South is the side the record says worsens
(PREREG_RIDGE_AG 3.3: -50 m = 1.239) and the side the P11/G3 vehicles froze on; north is the
side N2d actually drove. A remedy whose first live act contradicted the only successful run
would have been worse than no remedy.

With four points the approach stays on the authored line and only ~600 m of the leg is new.
The same search then returns **+75 m north at 0.803** (formation band max 0.878) - what the
record and this note's own premise predict. The two-waypoint form is kept in the self-test as
a FAIL-FIRST break (B5), so the reason for four is CHECKED rather than remembered.

The corner angle is now what `MaxTurnDegrees` MEANS - the transit length follows from it,
|X| / tan - rather than a cap on something computed elsewhere. At the shipped 30 degrees a
75 m shift transits over 130 m. A tank does not care about a 30 degree course change (its
turning-radius is 0.3 m); the vendor's curve-speed constraint is what the angle is about.

### 3.2 PAD - why the window's edges are not the detour's edges

MEASURED: the vehicles stop BEFORE the scored window. P11's and G3's 1-35 leader freezes at
s = 1,970.4 m, which is 31.5 m short of the window's near edge and 44.0 m short of its centre
(PREREG_RIDGE_AG 3.2); FINDING sec 7b says the same in the tool's own terms ("18 m before its
near edge"). They stop at the TOE of the face, not in it - which is precisely what FINDING
sec 7 argues selects the vehicle's effective limit near 0.75. A detour that began at the
window edge would begin 30-45 m too late.

`Vrf:PreflightRouteShiftPadMeters` DEFAULT 50 m: the measured 31.5 m toe distance rounded up
past the 44.0 m centre distance. Not a tuned number - a measured one with the next round
number above it.

### 3.3 LEAD - why it is the formation, not the turning circle

The obvious candidate, the vehicle's turning radius, is NOT the constraint: the M1A2's
`turning-radius` is 0.3 m (`M1A2_Abrams_MBT.entity:303`; the parameter is documented at
`include/vrfobjparam/movingObjectParameters.h:286-293` and is "used by non-pivoting ground
vehicles"). A tank pivots. What actually constrains the detour is the FORMATION:

- the unit's own `maneuver-in-formation` rows carry `formationLength=60` (FINDING sec 1; the
  60 m corridor of G7b and PREREG_RIDGE_AG 3.3);
- followers take `rightOffset` in {-50, -25, 0, +25, +50}
  (`READ_4-27_G3_AND_OFFSET_SCORING` sec 2.2), so the formation is 100 m wide;
- and the vendor limits speed on curves: "While the vehicle is moving, its speed is
  constrained to ensure that it follows the route reasonably well even if the route curves"
  (`vrf_MoveAlongRouteTaskFunctionality.htm`).

`Vrf:PreflightRouteShiftLeadMeters` DEFAULT 110 m = formationLength 60 + the widest slot 50.
It is the length of SETTLED PARALLEL RUN before the padded window: the whole formation, at
its full width, is established on the shifted line before it reaches the ground that flagged.

The lead is a run of its OWN, distinct from the transit (3.1a): transit, then LEAD metres of
settled parallel run, then the padded window.

`Vrf:PreflightRouteShiftMaxTurnDegrees` DEFAULT 30 SETS the transit length:
`transit = |X| / tan(30 deg)`, so the corner where the path leaves the authored line and the
corner where it rejoins are both exactly 30 degrees. The FEASIBILITY test is then simply
whether the leg has room: a candidate is REFUSED when `ein - LEAD - transit <= 0` or
`eout + LEAD + transit >= legLen`. A window in the first ~160 m of a leg therefore gets no
detour and is REPORTED instead - the honest outcome: a unit cannot side-step a face it is
already standing on. A larger offset needs a longer transit (600 m needs 1,039 m), so the same
test bounds how far the search can usefully reach on a short leg.

### 3.4 When neither side clears - REPORT, DO NOT INVENT

If no offset in the band satisfies sec 4's acceptance, the task dispatches ON THE AUTHORED
LINE and an ObservationReport says so, naming the leg, the band searched and the best ratio
found. The interface does not invent a waypoint it cannot defend, and it does not refuse the
task.

THIS IS NOT A CORNER CASE - it is measured, in the same order: 1-35's T4 / 1-1's T26 leg 1
(PL BLUE, 42,892 m) is flagged at 0.988, and NO offset within +/-600 m clears it. The reason
is instructive and is a stated limitation (sec 8 L3): the detour moves ONE window, and this
leg carries other windows that floor the whole polyline at ~0.887 wherever the detour goes.

## 4. DESIGN QUESTION 3 - SCORING THE SHIFTED LINE BEFORE COMMITTING IT

A shifted line is not trusted because the lateral table said the side was clear. It is
re-scored, whole, by the same `LegScorer`, and it must pass on its own numbers. The record
requires this: `READ_4-27_G3_AND_OFFSET_SCORING` sec 2.3 measures 30 m of lateral shift
moving a ratio from 0.902 to 1.130 - a sensitivity FOUR TIMES the 0.097 calibration margin
the threshold rests on. An unscored shift would be a guess.

### 4.1 The acceptance conditions

A candidate offset X is ACCEPTED when both hold.

**C1 - the route line, with margin.** The worst ratio over ALL segments of the shifted
polyline (A->D1, D1->D2, D2->B) is `<= threshold - Vrf:PreflightRouteShiftMarginRatio`
(0.92 - 0.10 = 0.82 at the defaults). The margin default 0.10 is the calibration margin
0.097 (PREFLIGHT_CALIBRATION / FINDING 7b: lowest frozen 0.966, best clean pass 0.870)
rounded up to the next hundredth. Rationale: the threshold separates frozen from clean legs
by 0.097 on nine legs of one order on one terrain; a REMEDY that lands inside that gap has
not demonstrated anything.

**C2 - the formation band, at the threshold.** Every slot line of the shifted polyline, at
the vendor's own rightOffsets {-50, -25, 0, +25, +50}, scores BELOW THE THRESHOLD (0.92, not
0.82). Configurable, `Vrf:PreflightRouteShiftClearFormationBand`, DEFAULT TRUE.

C2 IS AN ADDITION TO THE BRIEF'S RULE AND IS FLAGGED FOR THE SUPERVISOR. The brief specifies
C1 alone. Here is the measurement that argues for C2, on the reference leg (sec 6 table):
under C1 alone the chooser picks +50 m, whose INNER SLOT LINE (-50, i.e. the original line)
still scores 1.098 - the very face the remedy exists to avoid, with a fifth of the formation
on it. Under C1+C2 it picks +75 m, whose whole band maxes at 0.878. The remedy costs 25 m
more and stops leaving vehicles on the face. G3's 4-27 is the evidence that this matters:
three of six vehicles stopped at the toe while the leader crossed, and FINDING sec 7e's
ruling is that WHICH LANE stops is what reproduces.
Setting `Vrf:PreflightRouteShiftClearFormationBand=false` reproduces the brief's rule exactly.

C2 DOES NOT TOUCH THE CALIBRATION. The standing ruling
(`READ_4-27_G3_AND_OFFSET_SCORING` sec 2.5) is "keep the route line as the verdict, print the
band", and it is respected: the band still FLAGS nothing, decides no leg's verdict and moves
no threshold. It is used here only to SIZE a shift that the route-line verdict has already
demanded. The band's premise that its own sec 4 doubts - that a member meets the terrain of
its straight slot line - is a CONSERVATIVE assumption in this direction: the actual tracks
curve into gentler ground (0.41-0.43 vs 0.96-1.01), so a band test can over-shift, never
under-shift.

### 4.2 The search  [REVISED 2026-09-15 by the first live run - see 4.2a]

Offsets are tried in increasing MAGNITUDE, both sides at each magnitude, from
`Vrf:PreflightRouteShiftStepMeters` (default 25 m - the vendor's own slot granularity) to
`Vrf:PreflightRouteShiftMaxMeters` (default 600 m - the band PREREG_RIDGE_AG 3.3 measured,
+50..+550 clear, rounded up one step). The FIRST magnitude at which any candidate is accepted
wins; if both sides are accepted at that magnitude, the LOWER ratio wins. So the rule is
"the smallest shift that clears, on the side that clears", and the asymmetry of the face is
found rather than assumed: on the 1-35 leg -25 scores 1.278 and -50 1.256, and the search
walks past them to +50/+75.

Cost: a candidate is 5 segment scorings (plus 20 more under C2, on slot lines within 50 m of
the route line - the SAME tiles). MEASURED on the shipped defaults: the reference leg is
decided at the third magnitude, 6 candidates, 57 cache hits and ZERO network fetches, in
under a second.

### 4.2a C2 MAY SIZE A SHIFT; IT MAY NEVER SIDE ONE - a rule REVERSED BY THE FIRST LIVE RUN

Run `20260915T023743Z` (PREREG_V8 RESULTS) is the measurement. The interface anchors the route on
the UNIT object's live position; the calibration, the published lateral tables and the self-test are
anchored on the unit's FORMATION LEADER (`leg_check.py:starts_from_run` takes the first member's
first fix). In that run the two sat 26.7 m apart, which is enough to rotate a 6.59 km leg: the
authored line scored **1.248**, not 1.098, and the cleared northern corridor moved one step outward.
The single-phase search then did this, reproduced offline to the third decimal:

| magnitude | north | C1 | band | south | C1 | band |
|---|---|---|---|---|---|---|
| 25 | 1.022 | no | - | 1.280 | no | - |
| 50 | 0.829 | no | - | 1.050 | no | - |
| 75 | **0.735** | **yes** | **1.022** (C2 refuses) | 0.836 | no | - |
| 100 | 0.835 | no | - | 0.715 | yes | 1.050 (C2 refuses) |
| 125 | 0.841 | no | - | **0.661** | **yes** | **0.836 -> TAKEN** |

So C2 did not size the shift, it SIDED it: it walked the search past the north side N2d actually
drove and onto the south side six runs froze on and sec 9 P1 named a STOP. Every candidate at every
magnitude 25..600 on both sides had **0** missing-tile samples, so this was not a data gap - it is
the rule.

THE RULE IS NOW TWO PHASES (`RouteShift.ChooseForLeg`):

- **PHASE A - the SIDE, from C1 ALONE.** Magnitudes in increasing order, both sides at each; the
  first magnitude at which anything satisfies C1 decides the side (lower ratio on a tie).
- **PHASE B - the MAGNITUDE, on that side only.** Outward from there, the first magnitude that
  satisfies C1 and (when on) C2.
- If nothing on that side ever clears C2, the chooser takes the smallest C1-clearing candidate on it
  - exactly sec 4.1's brief rule - and says so in the note, the log (a WARNING) and the Marking.
  Refusing would leave the WHOLE unit on the face to spare one slot line.

On the reference (leader) anchor this changes NOTHING: +75 m north at 0.803, band 0.878. On the live
anchor it returns **+250 m north at 0.761, band 0.862** instead of -125 m south. With
`ClearFormationBand=false` the live anchor gives +75 m north at 0.735 - which is the proof that the
southward choice was C2 and not the ground.

### 4.2b UNKNOWN IS NEVER CLEAR

A candidate polyline with ANY sample that had no elevation tile is UNSCORABLE and refused, rather
than tolerated up to the flag's `LegScorer.MaxNanFraction` (1 %). A flag is a warning and must not be
silenced by a tile gap; a shift is an ACTION and must not be taken over ground nothing was read on.
The two rules are deliberately different and both are asserted. This was NOT the cause of the
southward shift above - it is the hardening its investigation demanded, and it costs nothing on the
committed cache (0 NaN samples everywhere in the band).

## 5. DESIGN QUESTION 4 - CONFIG AND LOGGING

All keys under `Vrf:`; all documented in `VrfSettings.cs` and `docs/RUNBOOK.md`.

| key | default | what it is |
|---|---|---|
| `PreflightRouteShift` | **false** | the whole feature. OFF until a live run confirms it (sec 9). The demo turns it on. |
| `PreflightRouteShiftMaxMeters` | 600 | the search band, +/-. PREREG_RIDGE_AG 3.3's +50..+550 plus one step. |
| `PreflightRouteShiftStepMeters` | 25 | search granularity = the vendor's own formation slot spacing. |
| `PreflightRouteShiftMarginRatio` | 0.10 | C1's margin below the threshold; the 0.097 calibration margin rounded up. |
| `PreflightRouteShiftClearFormationBand` | true | C2. false = the brief's rule (line only). |
| `PreflightRouteShiftPadMeters` | 50 | the toe pad each side of the flagged window (measured 31.5 / 44.0 m). |
| `PreflightRouteShiftLeadMeters` | 110 | settled parallel run before the padded window = formationLength 60 + widest slot 50. |
| `PreflightRouteShiftMaxTurnDegrees` | 30 | feasibility cap on the transit angle. |
| `PreflightRouteShiftTimeoutSeconds` | 30 | worker budget; on expiry the AUTHORED route is dispatched. |

The THRESHOLD, WINDOW, STEP, SHORT WINDOW, CACHE, OFFLINE flag and SharedData root are the
pre-flight's own existing keys, reused unchanged. There is no second threshold.

`Vrf:PreflightRouteShift` is independent of `Vrf:PreflightWarnings`: the first changes the
route, the second reports on the route that is finally dispatched. With both on the C2 side
gets the shift observation AND a warning for any leg still flagged after it - which is the
intended demo posture.

ONE WARNING LINE PER SHIFTED LEG, naming task, unit, leg, the offset and side, the before and
after ratios, the inserted coordinates, and the band; one line per flagged leg that could not
be shifted, naming the band searched and the best ratio; one INFO line per task summarising
how many legs were shifted. Silence is not an option for a feature that changes where units
drive.

## 6. THE REFERENCE MEASUREMENT (offline, before any code)

`leg_check.py`'s own `Tiles`, `SoilChain` and `analyse_leg`, on the committed cache,
0 tiles fetched, 0 NaN samples. This is what the self-test of sec 7 locks.

Reproduction control - PREREG_RIDGE_AG 3.3's full-parallel table, recomputed here:

| lateral | published | recomputed |
|---|---|---|
| -50 | 1.239 | 1.240 |
| 0 | 1.098 | 1.098 |
| +50 | 0.798 | 0.797 |
| +100 | 0.863 | 0.863 |
| +150 | 0.796 | 0.796 |
| +250 | 0.709 | 0.709 |
| +400 | 0.866 | 0.867 |
| +550 | 0.720 | 0.722 |

The 1-35 leg (start 34.658442/-116.740092 = the DeStack start of every run, to V1
34.651212/-116.811637; 6,593.3 m; bearing 263.02; limit 0.94 x sand 0.80 = 0.752): base ratio
**1.098**, worst 40 m window centred at s = 2,006.0 m.

Detour candidates (PAD 50, LEAD 110, the FOUR-point form of sec 3.1; transit = |X| / tan 30).
These are the shipped code's own numbers, printed by `--routeshift-selftest`, and they agree
with the pre-registration computed on leg_check.py before the code existed:

| offset | transit | polyline ratio | C1 (<= 0.82) | formation band max | C2 (< 0.92) |
|---|---|---|---|---|---|
| +25 | 43 m | 0.878 | no | - | - |
| -25 | 43 m | 1.276 | no | - | - |
| +50 | 87 m | **0.728** | yes | **1.098** | no |
| -50 | 87 m | 1.240 | no | - | - |
| **+75** | 130 m | **0.803** | **yes** | **0.878** | **yes** |
| -75 | 130 m | 0.978 | no | - | - |

The formation band of each candidate shift (C2 reads the row max against 0.92; every vertex of
the detour moved to the slot's own offset - a definition that reproduces READ_4-27 sec 2.3's
published row for the UNSHIFTED leg to 0.001, which is the instrument check on it):

| shift | -50 | -25 | 0 | +25 | +50 |
|---|---|---|---|---|---|
| 0 (the authored leg) | 1.240 | 1.276 | 1.098 | 0.877 | 0.797 |
| +25 | 1.276 | 1.098 | 0.878 | 0.731 | 0.800 |
| +50 | **1.098** | 0.878 | 0.728 | 0.802 | 0.798 |
| **+75** | 0.878 | 0.724 | 0.803 | 0.777 | 0.833 |
| +100 | 0.723 | 0.803 | 0.793 | 0.922 | 0.913 |

So: C1 alone -> **+50 m north at 0.728**; C1 + C2 -> **+75 m north at 0.803, band max 0.878**.
Both are NORTH and inside the +50..+550 band PREREG_RIDGE_AG 3.3 measured clear.

Controls, same sampler, same settings:

| line | ratio | outcome |
|---|---|---|
| 1-1/2/1_AD T23 leg 1 (34.662425/-116.746154 -> 34.650886/-116.812115) | 0.870 | not flagged -> UNTOUCHED |
| 1-35 authored V0->V1 (34.67998/-116.72480 -> V1) - the line N2d drove | 0.524 | not flagged -> UNTOUCHED |
| 1-35 T1 leg 2, T2, T3 | 0.585 / 0.893 / 0.394 | not flagged -> UNTOUCHED |
| T4 / T26 PL BLUE leg 1 (42,892 m) | 0.988 | flagged, NO cleared line within +/-600 m (best candidate -475 m at 0.887) -> reported, dispatched as authored |

The 0.524 on 1-35's authored V0->V1 line is an independent offline confirmation of N2d: the
one line on which this unit ever completed a leg is the one line in its record that this
sampler scores as clean.

## 7. DESIGN QUESTION 5 - TESTS

A new offline suite, `VrfC2SimApp --routeshift-selftest`, on the committed tile cache with
`Offline = true` (a live fetch is a failure, as in `--preflight-selftest`). It asserts:

1. **GEOMETRY, pure.** Offsetting right of a 263.02 bearing goes NORTH; the offset distance
   is the requested distance (round-trip to within 0.5 m); Pin and Pout have ZERO cross-track
   (the approach is unchanged ground) while D1 and D2 are BOTH at the full offset; the transit
   is |X| / tan(corner); a window with no room for transit+lead is REFUSED, not clamped.
2. **THE REFERENCE LEG.** The 1-35 leg scores 1.098 and is flagged; the chooser returns a
   POSITIVE (north) offset inside +50..+550 m; the shifted polyline's worst ratio is below
   the threshold and satisfies C1; under the shipped defaults the offset is +75 m at 0.803
   with a band max of 0.878; with `ClearFormationBand=false` it is +50 m at 0.728.
3. **THE UNTOUCHED CONTROLS.** 1-1's T23 leg 1 (0.870) and 1-35's authored V0->V1 (0.524) are
   not flagged and come back with the vertex list IDENTICAL to the input - not merely
   unshifted, byte-identical.
4. **THE REPORT-ONLY CASE.** The 42,892 m PL BLUE leg is flagged, yields no accepted offset
   in +/-600 m, and produces the "no cleared line" observation with the band in it.
5. **THE ORDER OF THE SEARCH.** On the reference leg the south candidates at the winning
   magnitude score worse than the north ones, and the chooser's answer is the smallest
   magnitude that clears - checked by asserting no smaller magnitude was acceptable.
6. **THE REPORTS.** One observation per shifted leg and one per unshiftable flagged leg;
   none for an untouched leg; the Marking carries the offset, both ratios and the band.

ADDED 2026-09-15 after the first live run (sec 4.2a): **the V8 LIVE ANCHOR is a fixture**
(34.65820208652259, -116.74009186651882 - the unit's own first PositionReport in that run). The
suite asserts that from it the chooser returns a NORTH offset, specifically +250 m at 0.761 with a
band max of 0.862, that -125 m south is never returned, that no southward candidate is ever
accepted on this leg, and that with `ClearFormationBand=false` the same anchor gives +75 m north at
0.735 - so C2 can be seen to size and never to side. Plus sec 4.2b's unknown-ground checks: a 0.100
candidate carrying ONE missing sample is refused (and the same ratio with none is taken at once),
and a service on an EMPTY cache gets NO VERDICT, flags nothing, shifts nothing and returns the
authored route point for point.

FAIL-FIRST is part of the deliverable, and it is what caught the geometry of 3.1a. Five breaks
are driven through the suite and each must trip checks: the margin removed (2 fail), the
lateral sign flipped (8), acceptance inverted (4), the one-shot claim always winning (2), and
the transit removed - the two-waypoint form (2). A check that cannot fail is not a check (the
n8 finding).

The existing 18 offline suites must stay green, `--preflight-selftest` included: the shift
must not perturb the port's fixture comparison with the python tool.

## 8. LIMITATIONS, STATED

- **L1 - one window per leg.** The scorer returns the WORST window; the detour moves that
  one. A leg with two faces is re-scored whole, so a second face keeps the polyline above
  the acceptance and the shift is DECLINED (reported, dispatched as authored). Iterating over
  every flagged window is deliberately NOT built.
- **L2 - the estimate is still an estimate.** A flag is a prediction off terrain tiles, never
  a vendor verdict; two of P11's four leg-1 stops are on ground this scores as benign
  (FINDING 7b). The shift inherits every caveat of the flag, including the one measured false
  alarm (4-27 T5 in G3).
- **L3 - long legs rarely clear.** Measured: the 42.9 km PL BLUE leg floors at ~0.887
  whatever the offset. On long legs the feature will usually report rather than shift.
- **L4 - the 0.752 limit is an ANALOGY**, not a cited dynamics gate (FINDING sec 7 "WHAT IS
  ASSUMED"). It is used here only to compare lines against each other on one scale, which is
  what a chooser needs.
- **L5 - the mesh.** Where the vendor's navigation mesh plans a leg, the driven path is not
  our polyline and the shift's effect is unpredictable. On this AO the western lane refuses
  every long goal (PREREG_N1_N2 10.3), which is why the remedy is worth building at all.
- **L6 - a single-entity taskee has no formation**, so C2 is over-strict for one. It is
  conservative, and the feature ships off.
- **L7 - aggregate dispatches that collapse the route.** With `Vrf:MoveIntoFormation` or the
  R11 `Vrf:AggregatePlanAndMove` probe set, an AGGREGATE drives to the route's final point and
  the intermediate vertices are discarded. The shift is SKIPPED there (with a log line saying
  why) rather than reporting a detour nothing drives.
- **L8 - the anchor.** The pre-flight scores from the unit's LIVE position, which is the line
  the interface authors; the leader measurably drives up to 34.6 m off it
  (`READ_4-27` sec 2.2). Unchanged by this design and recorded as an assumption, not fixed.

## 9. PREREG DRAFT - WHAT A LIVE RUN MUST PROVE

Not run. Registered here so the confirming run is not designed after its own result.

**Configuration.** One variable against the N2d/RIDGE-AG posture: `Vrf:PreflightRouteShift`
true, everything else at the shipped defaults, `Vrf:PreflightWarnings` true so the dispatched
route is scored too, `Vrf:PreflightOffline` true with the AO cache pre-warmed (the shift must
not wait on the network at dispatch; this is the STP-802 scenario-prep posture). Order:
1-35's T1 alone, from the same DeStack start as P11/G3/G5, on the same terrain.

**P1 (HIGH - THE GATE).** The interface logs a shift for T1 leg 1 with a POSITIVE (north)
offset in +50..+550 m, the before ratio 1.098 and an after ratio below 0.92, and the created
route carries 8 vertices instead of 4. A miss - no shift, a southward shift, or a route still
of 4 vertices - is a STOP: the feature did not act and nothing downstream is readable.

**P2 (THE MEASUREMENT).** 1-35's leader passes the P11/G3/G5 freeze band
(s = 1,970 +/- 3 m on the leg axis, 34.65608/-116.76142) with a cross-track of at least
+40 m north of the authored line, and continues past s = 2,100 m. Six runs put a stop within
150 m of that point; passing it is the result.

**P3.** The unit reaches V1 (within the arrival radius) and the task reports TASKCMPLT, or
the run's window closes with it still advancing past s = 4,000 m. N2d is the existence proof
that this unit can do it on a line 1.3 km north; this predicts it on a line 75 m north.

**P4.** The ObservationReport carrying the shift is on the wire and readable at the C2 side,
with the leg, the offset and both ratios in its Marking.

**P5 (RECORDED, not predicted).** Where the other five members stop, and their cross-tracks -
the lane question of FINDING 7e. With C2 on, no slot line should sit on the 1.098 ground.

**FALSIFIERS.** (a) The unit freezes within 150 m of the P11 point anyway - then the line is
not the whole cause and FINDING sec 7's competing hypothesis (formation mutual speed control,
never excluded) is back on the table. (b) The unit freezes somewhere NEW on the shifted line
that the sampler scored clean - then the sampler, not the remedy, is what needs work.
(c) A vendor task failure at the inserted vertices ("Entity not embarked on same object as
target", READ_G2 sec 1) - then waypoint insertion has a cost this design did not anticipate.

**WHAT THE RUN CANNOT SETTLE.** Whether the vehicles could have climbed the face under some
other configuration; member velocity or commanded speed (not in any capture - members at
notify level 3); and the false-alarm rate of the threshold, which stays at nine legs of one
order until more orders are scored.

## 10. ADVERSARIAL REVIEW OF THIS DESIGN (HEAVY; before the code, not after)

**Strongest competing design: do nothing in the interface and fix the mesh.** The vendor
ships a planner; building a second one is the thing FINDING sec 8 calls "last resort". Weighed
and rejected FOR NOW on measurement, not preference: the abstract-graph override plans 5 km
legs on the eastern lane and refuses EVERY long goal in the western one where 1-35 drives
(PREREG_N1_N2 10.3, 8-10 refusals per member), the flat query plans nothing beyond 23 m
anywhere (G6), and the diagnosis needs a GUI tool (NavigationLab) or MAK. This design is also
not a planner: it does not search a graph, it moves one straight line sideways by one offset
chosen from a scored list, and it declines whenever it cannot defend the result. If the mesh
is repaired, a mesh-planned leg simply is not our polyline any more and the feature costs
nothing.

**"The shift will make things worse somewhere else."** The exposure is real: lateral
sensitivity exceeds the calibration margin (0.902 -> 1.130 over 30 m). Three things bound it -
the shifted line is re-scored WHOLE before it is committed, it must beat the threshold by the
full calibration margin, and the feature ships OFF. The residual case is a shift onto ground
the SAMPLER scores clean and the SIM does not; falsifier (b) of sec 9 is exactly that, and it
would indict the sampler, which is instrumented (median residual +0.03 m over 129 samples
against the sim's own altitudes) rather than the geometry.

**"The window is the wrong thing to avoid."** FINDING sec 7's own verdict says the
discriminator is SUSTAINED EXTENT, not a single window against a threshold, and 4-27 in G3 is
a measured false alarm at 0.966. Answer: the detour is placed by the window but ACCEPTED by
re-scoring the whole polyline with the same statistic that was calibrated - so a bad window
placement can only fail to help, not silently harm. And the pad exists because the measured
stops are 18-44 m BEFORE the window.

**Unexplained and carried, from the record, not resolved here:** what stops the units that
freeze on benign ground (FINDING 7b: 1-6 in P11 at -0.14 over 55 m; 856/HHC on the flat) -
this remedy does nothing for them and must not be reported as if it did; the shared crawl
speeds of moving subgroups (0.30 x3 in P11, 1.40 x3 in G3), unmeasured; and whether formation
mutual speed control contributes to the freeze at all, which no capture in the record can
separate.

**The defect this design is most likely to ship:** the double dispatch of sec 2.2 (worker and
timeout both continuing). It is named, it has a one-shot latch, and the self-test drives the
race directly rather than trusting the reading.
