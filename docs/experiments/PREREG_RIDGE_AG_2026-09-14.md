# PREREG RIDGE-AG: the abstract-graph SMS driven at 1-35's ridge face

Registered 2026-09-14 by an Opus design executor BEFORE any launch. DESIGN ONLY - nothing was
launched, no order pushed, no build run, no file under C:\MAK written (read-only reads of the
vendor SMS, the navigation profile and the deployed fixtures only). Tier: HEAVY (a prereg whose
result adjudicates a standing cause claim and a standing competing hypothesis).

Deliverables: this file, data/PROBE_RIDGE_1-35_Order.xml, and the launch line
scratchpad\ridge\ridge_launch.sh (dry-run only from here).

## 0. THE QUESTION, AND THE TWO CLAIMS IT DECIDES

Does the abstract-graph route (custom including SMS C2SIM_EntityLevel_AbstractGraphs,
useAbstractGraphs=true, fixture R9_Mojave_Empty_52_NavAO_AG, 8 of 8 long queries planned on benign
legs in G7b) AVOID the 55 m sustained face at 34.6561/-116.7614 that froze 1-35's leader at 1.97 km
in P11, G2, G3, G5 and G6 - or does it drive straight across it and freeze like the flat-mesh and
feature-planned runs?

Two open items are settled by the same run, and both were written down by someone else first:

- **G7B_G8_RESULTS sec 2.4, verbatim:** "Falsifier, and it is the next run: put the abstract-graph
  SMS on the 1-35 ridge lane of FINDING_EARLY_STOPS_2026-09-13. If the coarse path drives straight
  across the 55 m sustained 0.70-0.95 face and freezes at 1.97 km like the other three runs,
  'returns something useful' is refuted and the flag is only a way to get non-empty output."
- **FINDING_EARLY_STOPS sec 7, the cause claim's own falsifier:** "a run in which 1-35 is released
  from the toe by a route that avoids the face and then completes its leg - the confirming test".
  Sec 7d records that this test is STILL OPEN: the one mesh-planned COA leg in the record (G2's
  1-6) never reached its face, and the three vehicles that did cross a flagged window were on
  re-formed offset routes, not on a planned leg.

This run is the first in the record in which a unit is driven AT a flagged window with the mesh
actually planning its leg.

## 1. DOCS AND RECORD CONSULTED (docs-first rule)

Vendor documentation

- classdoc/classref vrf_the_navigation_a_p_i.html: "useAbstractGraphs, // setting to true can speed
  up long path planning queries" - the ONLY sentence in the Developer's Guide that acknowledges long
  queries as a distinct case. struct_dt_lua_nav_find_path_parameters.html: "bool useAbstractGraphs =
  true" - the API default is TRUE and the shipped VR-Forces Lua overrides it to false.
  class_dt_nav_area.html: findPathToLocation's whole documented contract is one line; no failure
  modes, no distance limit, no sector count. Quoted in NAVMESH_QUERY_DOCS_2026-09-14 (the classref
  block); that document also records, correctly, that NO MAK document says what an abstract graph IS.
- UG52 23.5.2 p508 (slope and soil: "On the best soil surface (dry pavement), vehicles can just
  barely move up the max-slope defined in the entity parameters by using maximum throttle. It is
  possible that vehicles may slide down slopes, especially if the soil is slippery"); 23.5.1 p507
  (acceleration-factor); 23.5 (the feature-obstacle planner ignores slope); 30.22 p598 (Maneuver
  Along: leader and followers use MUTUAL speed control, which "can override ordered speed");
  21.9.1 p483 (object console notify levels 0-4); 63.6.1 p1247 (terrain paging); p1276 (the 20 km
  navigation-area figure is a GUI default, not a generator limit - G6 sec 5).
- Migration Guide 2.4 p18 (the mesh planner needs a mesh from the start of the movement path to its
  end).
- C:\MAK\vrforces5.2d\appData\settings\vrfSim\navigationProfiles.mtl, ground-platform profile
  (read 2026-09-14): step-max 0.56, **slope-max 46.000000 deg**, soil-types-to-tag-with-surface-char
  = road and pavedroad ONLY, raster-precision 0.2, **generate-abstract-data True**. 46 deg is
  rise-over-run 1.036 - ABOVE every posting on the face, so the face is MESH-LEGAL, and the mesh was
  never told the ground is sand.
- navigationPreferenceDescriptor.h:111-132 (path cost = inherent cost x slope-avoidance-factor x
  (slope/max-slope)^2 - a cost, no hard gate; and it calls its own derating "an estimate").
- movingObjectParameters.h:294-310 (max-slope = the slope at which acceleration is still >= 0).
- ground-vehicle-move-to.lua (5.2d): :488 useAbstractGraphs = false (the sole occurrence, and :497
  the sole findPathToLocation call site); :1416-1426 the two nav-area gates; :1461-1470 and
  :1570-1581 the fallback selector that turns a gate failure into a feature plan; :219-264 "blocked"
  means only BlockedByWall / BlockedByVehicle; no progress watchdog anywhere.

Our own record (the anchor order: our settled notes before any new derivation)

- FINDING_EARLY_STOPS_2026-09-13 secs 1, 2, 7, 7b, 7c, 7d, 7e (the printed rows, the stop points,
  the elevation test and its VERDICT, the scope narrowing, 4-27's two reads, G2's third stop kind,
  and the ruling that the claim is about LINES ACROSS THE FACE, not about a unit or a role).
- G7B_G8_RESULTS_2026-09-14 secs 0, 1.3, 1.5, 2.3, 2.4, 3, 6 (the four-run ladder; run C's 32/32;
  the coarse-path spacing table; the cross-track table; the gate-timing table that withdraws
  "first legs are mesh-planned under AtOrder with zero settle"; the appData knob measured to do
  nothing; the NEXT list, whose item 2 is this run).
- G6_RESULTS_2026-09-14 secs 0, 2, 6, 7 (MojaveCOA loads, both nav-area gates pass, the FLAT query
  returns zero points for every long goal, and every leader sits within a few hundred metres of its
  P11/G3 counterpart at sim 300/600/900 - 1-35 frozen at 4,104 m on the authored-origin axis in all
  three runs).
- PREREG_EARLYSTOP_G5_2026-09-13 (the "1-35 alone at console level 4" design and its results: the
  T1-only probe order, AtOrder, 1-35 alone freezes at the same metre; the object console at level 4
  does NOT carry commanded-vs-actual speed).
- PREFLIGHT_CALIBRATION_2026-09-13 and tools/preflight/README.md (the 40 m window, the 0.92
  threshold, the nine-leg calibration, the assumptions list).
- PREREG_NAVDATA_G6_2026-09-13 sec 1 and sec 5 (the area's extent, and the generation result: 8,856
  sectors, 8,810 carrying AbstractData).
- Memory: lessons-compare-at-equal-sim-time; lessons-vendor-diagnostics-first;
  vrf-stall-detection-by-design; feedback-anchor-vendor-and-own-notes.

## 2. THE RUN

### 2.1 The one variable, and how it is bracketed

No run in the record differs from this one in exactly one thing, so the comparison is bracketed by
two that differ in one each:

| run | fixture / SMS | area | policy | taskees | 1-35's leader ends at (O-axis) |
|---|---|---|---|---|---|
| P11 (20260907T150643Z) | Empty_52, stock SMS | none | AtOrder | 9 | 4,104 m, frozen |
| G3 (20260913T174516Z) | Nav, stock SMS | MojaveAO20 | AtOrder | 9 | 4,104 m, frozen |
| G5 (20260913T185936Z) | Empty_52, stock SMS | none | AtOrder | **1 (1-35 only)** | 4,106 m, frozen |
| G6 (20260914T002716Z) | NavAO, stock SMS | **MojaveCOA** | AtOrder | 9 | 4,104 m, frozen |
| **THIS RUN** | **NavAO_AG, AG SMS** | MojaveCOA | AtOrder | 1 (1-35 only) | ? |

- Against **G6** the only difference is the SMS (plus the taskee count).
- Against **G5** the only difference is the area plus the SMS (the fixture terrain).
- G6 shows the AREA ALONE changes nothing: with MojaveCOA loaded and both nav-area gates returning
  success, the FLAT query returned zero points and the leader froze at the same metre. G5 shows the
  TASKEE COUNT changes nothing: 1-35 alone froze at the same metre.
- Together they leave the SMS as the only untested difference. Neither control alone is
  single-variable and this prereg does not pretend otherwise.

### 2.2 The init and the type map - DECISION, with the reason

**INIT: data/COA-STP1_Initialization.xml, unchanged. No new init file is created.**
The run cost that file is feared for is not incurred here: with `Vrf__CreationPolicy=AtOrder` and a
ONE-TASK order, only 1-35 materialises into vehicles (6: M1A2 1, M1A2 2, M3 1, HMMWV 1, HMMWV 2,
M577A2 1) and the other 127 units stay empty shells. G5 ran exactly this posture and reproduced the
P11/G3 freeze point to 2.1 m.

A lean 1-35-only init was considered and REJECTED, and the reason is the measurement itself. 1-35
does not start where the init authors it: 54 units share the authored origin 34.67998/-116.72480 and
`DeStacker.Apply` spreads them onto 700 m hex rings, putting 1-35 at **34.658442 / -116.740092**,
2,774 m away. That spread position is what selects WHICH LINE the unit draws across the ridge, and
FINDING sec 7e's ruling is that the slope claim "is about lines across the face, not about a unit or
a role" - the outcome moves with a few tens of metres of lateral shift (sec 3.3 below). A lean init
holding one unit has nothing to de-stack, so 1-35 would start on its authored point and draw a
different line; authoring the lean init AT the spread point instead would stop V0 being dropped and
add a 2.8 km leg. Either way the run stops measuring the thing it is for.

**TYPE MAP: data/unit-type-map-52-nolifeform.json** with `Vrf__TypeMappingMode=FidelityTable` - the
probe map every COA-STP1 run since 2026-09-06 used (the DI-Guy data package is still absent and the
headless sim crashes at the first DI-Guy human). Verified byte-identical to the scratchpad copy the
G6 and G7b runs passed on the command line; the repo path is used so the run is reproducible from
the tree. It resolves 1-35 (EchelonCode BN, APP6C functionId UCA, echelon char F) to **Tank
Headquarters Section (USA)**, min max-slope 0.94 - the same six vehicles P11 created.

**CREATION POLICY: AtOrder.** A deliberate departure from G7b run C (AtInit + a 240 s settle); it
carries the one real risk in this design, and both are set out in sec 6.1.

### 2.3 The order

data/PROBE_RIDGE_1-35_Order.xml - task T1 of data/COA-STP1_Order.xml with the vertices, the task
name and the task uuid verbatim, the performer 1-35/2/1_A (d6df3c3d-f31b-701a-bfc6-2fb9bc86092a),
ROEHold, Duration P2H, StartTime delay 0, and ONE content change: `TaskActionCode` PENTRT ->
**MOVE**, with `AffectedEntity` dropped.

That change is a verified no-op for this task. PENTRT classifies as TaskIntent.Attack
(VerbMapping.cs:90), whose layer 2 resolves AffectedEntity to a fire target - but T1's AffectedEntity
IS its PerformingEntity, so the self-target guard fires (VrfC2SimService.cs:1803-1815: "affected
entity is the taskee itself (self-target fire-support?); no fire, advancing only") and the dispatch
is the same CreateRoute + MoveAlongRoute that MOVE takes (VerbMapping.cs:84). The offline parser and
the pre-flight tool both confirm the route is unchanged (sec 7).

All four vertices lie inside NavArea-ground-platform MojaveCOA (lat 34.318..34.689, lon
-117.019..-116.428), including V1 at -116.811637, which lay 229 m OUTSIDE the old MojaveAO20 area and
was one of the two reasons G3's mesh could not help (FINDING sec 4, check C2).

### 2.4 Environment and command

    scenario     R9_Mojave_Empty_52_NavAO_AG    (deployed; its .scn names
                 C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs.sms and the same
                 tools\navdata\out\"MAK Earth (online) + MojaveCOA.mtf" terrain as the non-AG twin)
    init         data\COA-STP1_Initialization.xml      client-id C2SIM (the init's SystemName)
    order        data\PROBE_RIDGE_1-35_Order.xml
    type map     data\unit-type-map-52-nolifeform.json
    env          Vrf__TypeMappingMode=FidelityTable  Vrf__CreationPolicy=AtOrder
                 Vrf__DeStackCreates=true  Vrf__DeStackSpacingMeters=700  Vrf__DeStackRotationDeg=0
                 Vrf__DropOriginVertexMeters=100  Vrf__TaskPredecessorTimeoutSeconds=7200
    consoles     object 4, member 4     position reports 10 s     backend notify 3
    window       --run-secs 900   --watch-secs 0 (derived)   --pre-order-settle 240
    appData      VENDOR (no --vrf-appdata-dir) - see sec 6.2
    launcher     scratchpad\ridge\ridge_launch.sh (modelled on g7b_launch.sh; --dry-run only here)

-RunSecs 900 is sized from the measured clocks, not guessed. P11's least-squares sim/wall ratio is
1.456x overall and 1.55-1.81x over the first 1,200 wall seconds; G6 (the same area, 82 member
entities) ran 1.663x; G5 (six vehicles, no area) ran 6.21x. The leg to V1 is 6,593 m, which at the
ordered 10 m/s is ~660 sim s of driving plus ~90 sim s of slot move and turn-to-route, so an
unobstructed arrival lands near sim 750-830. 900 wall seconds buys sim >= 900 even at 1.0x, sim
~1,440 at 1.6x and sim ~5,500 at 6.2x - enough for the freeze (complete by sim 360 in every earlier
run), for the three published comparison points sim 300/600/900, and for 600 sim s of no-movement
evidence afterwards.

## 3. THE GROUND, MEASURED BEFORE THE RUN

### 3.1 The face and the freeze

From FINDING sec 7 (the elevation test, instrument-validated against the sim's own reported altitudes
to a median +0.03 m over 129 samples): walking WEST on heading 263.2 through 34.656073/-116.761444,
the native L13 postings read 1582.85, 1588.38, 1595.07, 1602.19, 1609.17, 1615.84, 1622.62, 1630.07 m
at 7.86 m steps - rise-over-run 0.70, 0.85, 0.91, 0.89, 0.85, 0.86, 0.95, i.e. **55 m of face at
35-43 deg, mean 0.858, on sand** (CA FVEG 30 Sagebrush -> BM_SAND -> factor 0.80; derated limit
0.94 x 0.80 = 0.752). No leader in any run surmounted a 55 m window above 0.714.

### 3.2 The axis and the numbers this run is scored against

Two axes, both re-derived here and both reproducing the published figures:

- **leg axis** s = along-track from the DeStack start 34.658442/-116.740092 toward V1, bearing
  263.02, leg length 6,593.3 m. The pre-flight's worst 40 m window centres at s = 2,006 m (segment
  ends 34.656260/-116.761686 and 34.656216/-116.762119); the P11/G3 freeze projects to s = 1,970.4 m
  with cross-track +22.8 m (LEFT of travel, i.e. to the south); G5's to s = 1,972.4 m.
- **authored-origin axis (O-axis)**, the one G6 sec 6 publishes: along-track from V0
  34.67998/-116.72480 toward V1. The P11 freeze projects to **4,104.0 m** on it - the exact figure
  G6's table gives for P11, G3 AND G6. V1 is 8,562.1 m along it.

| point | lat / lon | leg s (m) | O-axis (m) | straight line to V1 (m) |
|---|---|---|---|---|
| DeStack start (every run) | 34.658442 / -116.740092 | 0 | 2,134 | 6,593 |
| P11 and G3 leader freeze | 34.65608 / -116.76142 | 1,970.4 | 4,104.0 | 4,625 |
| G5 leader freeze | 34.65607 / -116.76144 | 1,972.4 | 4,106.2 | 4,623 |
| G2 leader stop | 34.65616 / -116.76002 | 1,842.1 | 3,981.8 | 4,754 |
| worst-window near edge | 34.656260 / -116.761686 | 1,992.1 | 4,119.1 | 4,604 |
| worst-window centre | 34.656242 / -116.761859 | 2,006 (tool) | 4,134.6 | 4,588 |
| worst-window far edge | 34.656216 / -116.762119 | 2,032.1 | 4,157.8 | 4,564 |
| V1 | 34.651212 / -116.811637 | 6,593.3 | 8,562.1 | 0 |

The P11 freeze is 31.5 m short of the window's near edge and 44.0 m short of its centre. G5's freeze
is 2.1 m from P11's; G3's is the same point to the printed precision.

### 3.3 How far a detour has to go - the lateral sensitivity, computed here

The whole leg was re-scored with both endpoints shifted perpendicular to its bearing, using the
pre-flight's own sampler on the offline tile cache (scratchpad\ridge\lateral.py). Positive = NORTH.
Rows with missing tiles are excluded: the cache is complete on the north side out to +900 m and is
NOT complete south of -100 m, so the south side is indicative only.

| lateral shift | worst 40 m sustained | ratio vs limit 0.752 | verdict at threshold 0.92 |
|---|---|---|---|
| -50 m (south) | 0.932 | **1.239** | flagged, WORSE than the line |
| 0 (the leg itself) | 0.826 | **1.098** | flagged |
| +50 m | 0.600 | 0.798 | clear |
| +100 m | 0.649 | 0.863 | clear |
| +150 m | 0.599 | 0.796 | clear |
| +250 m | 0.533 | 0.709 | clear |
| +400 m | 0.651 | 0.866 | clear |
| +550 m | 0.541 | 0.720 | clear |

The face is ASYMMETRIC: it worsens to the south - where the P11 leader's +22.8 m cross-track and its
followers' 34.6556-34.6566 spread put them - and clears to the north, where 1-1's line 400 m away
crossed at 0.661 over 55 m and that unit drove 25 km. Every offset from +50 to +550 m north scores
below the threshold. **X is set at 150 m** for the detour reading of P18b: 2.5x the 60 m corridor
inside which every flat-mesh and feature-planned track in G7b stayed (that corridor is the formation
offset, not a routing decision), at a shift where the ratio is 0.796 with margin, and well inside the
231-292 m of cross-track the abstract-graph tracks actually reached in G7b run C.

## 4. PREDICTIONS (written before launch; a missed HIGH prediction is a stop)

### P18a (HIGH - THE GATE): the leg is planned by the mesh, with abstract graphs

For 1-35's formation leader M1A2 1, the SECOND ground-vehicle-move-to goal of the run - the one whose
destination is V1 plus the member's formation offset, "the last goal any member ever receives" in
P11/G2/G3 - the console prints, in order: `Node Is current point in nav area?: success` ->
`Node Is destination in nav area?: success` -> `C2SIM override ground-vehicle-move-to.lua:
useAbstractGraphs=true` -> `Job Calc off road nav path part success` -> `Planned path has N points.`
with **N > 1**; and for that goal there is NO `Planned nav path has not enough (0) points.` and NO
`Planned path has N parts.`. N itself and its spacing are RECORDED, not predicted; G7b's measured
coarsening (137-177 m per point on a 5 km leg) puts the expectation near 30-60 points over 6.6 km.
Instrument sanity folded in: the six members are created, the leader is born within 50 m of
34.658442/-116.740092, the interface logs `dropped 1 leading route point(s)` and a 4-point route, and
the proof line appears at least once per mesh query (32 times in G7b run C, 0 times in the three flat
runs).
MISS -> nothing about the router has been measured; STOP, re-run per sec 6.1, and do not read
P18b-P18d.

### P18b (THE MEASUREMENT): where the driven track goes at the face

Scored on the leader M1A2 1's own POS track from watchvrf-trace.csv, projected on the leg axis of sec
3.2, after the sec 6.5 altitude-residual check.

- **DETOUR** - the abstract path routed around the face: the track reaches leg abscissa s = 2,006 m
  and its cross-track there is **>= 150 m to the NORTH**, AND its closest approach to the
  worst-window centre 34.656242/-116.761859 is **> 150 m**. (X = 150 m, derived in sec 3.3.)
- **STRAIGHT** - the abstract path drove the chord: the track's last fix is **within 50 m of
  34.65608/-116.76142** (P11 and G3 sit on that point, G5 2.1 m away, G2 128 m away) and its net
  advance over the final 600 sim s is under 20 m. That is what the flat-mesh and feature-planned runs
  did, four times.
- **MIDDLE CASE, registered now so it cannot be read post hoc** - the track passes s = 2,006 m with a
  cross-track under 150 m and keeps going. That is NEITHER reading: it means the coarse path shifted
  the LANE without routing around anything. It is scored by re-running the pre-flight on the leader's
  own driven polyline (`analyse_leg` on its own track): if that line's worst 40 m ratio is below 0.92,
  the pre-flight metric explains the release and the abstract graph's contribution is a lane shift,
  not an avoidance.

Predicted, with its reason, so this is falsifiable rather than a menu: **STRAIGHT is the primary
prediction (MEDIUM confidence).** The ground-platform profile's own slope-max is 46 deg = 1.036
rise-over-run, above every posting on the face, so the face is mesh-LEGAL; the mesh carries no soil
tag except road and pavedroad, so it does not know the ground is sand; and the abstract path is
coarse - 137-177 m between points at this range in G7b - which is three to four times the length of
the entire 40 m window. A planner that legalises the face, cannot see the soil, and samples at 150 m
has no mechanism by which to avoid it. DETOUR would mean the slope COST term
(slope-avoidance-factor x (slope/max-slope)^2) bites hard enough at abstract-graph resolution to bend
the route by hundreds of metres, which nothing in the documentation claims.

### P18c: does it finish the leg, at equal sim time

The straight-line distance from the leader to V1 - the column G6 sec 6 publishes - falls **below
200 m by sim 1,000**; and the leader's O-axis along-track at sim 300 / 600 / 900 is recorded beside
P11 (4,117 / 4,108 / 4,105), G3 (3,907 / 4,103 / 4,107) and G6 (3,823 / 4,110 / 4,103). Every
comparison in the harvest is at equal SIM time and never at equal wall time
(lessons-compare-at-equal-sim-time; the two false headline claims of 2026-09-13).
HOLDS -> the confirming test of FINDING sec 7 fires and the cause claim is CONFIRMED.
MISS while P18b reads DETOUR -> the unit left the face and still did not finish: a SECOND stop is in
the way, and FINDING sec 7b's "second mechanism" is the first thing to read.

### P18d (RECORDED, not predicted): the second mechanism

For each of the six members: mean speed over the final 120 sim s, over sim 1,000-1,200, and the
per-200-sim-s bin series; plus whether any member overruns the leader. FINDING secs 7b/7c/7e record
that every stuck unit in P11 degraded to a 0.2-0.8 m/s crawl (4-27's three followers at
0.301 / 0.299 / 0.301 m/s; G3's three movers sharing 1.71 then 1.40 m/s) while 4-27's leader held
10.0 m/s in every bin to sim 2,200. PRESENT = at least one member sits in a constant 0.2-0.8 m/s band
for >= 300 sim s while its task still reports TaskRunning. ABSENT = every member is either at ordered
speed or at zero. Recorded either way; it decides nothing here.

## 5. WHAT COUNTS AS A STOP

- P18a missed = STOP. Report, re-run per sec 6.1, read nothing else.
- P18b reading STRAIGHT while the unit does NOT freeze - it crosses the face on the chord and
  continues - = STOP and report: that FALSIFIES FINDING sec 7's cause claim for this lane, and the
  first thing to check is whether the SMS override changed anything but the query (sec 6.3).
- Anything else: harvest all four predictions, adjudicate, and write the results into this file and
  into FINDING_EARLY_STOPS sec 7 / G7B_G8_RESULTS sec 2.4 the same turn.

## 6. CONFOUNDS, NAMED

### 6.1 The gate timing - the one real risk, and its fallback

G7B_G8_RESULTS sec 1.5 WITHDRAWS the claim that first legs are mesh-planned under AtOrder with zero
settle: in runs A, B and D the order reached the bus 4.7-7.7 s BEFORE the `New Primary nav area` row,
and the first 7-8 goals failed `Is current point in nav area?` and fell through to the feature
planner. Its rule 6 says AtInit is what buys 32 of 32.

Why AtOrder is still chosen here: the G7 legs were 476-673 m, so their leg-1 goals arrived within ~6 s
of their slot goals. 1-35's slot move is 40-90 m and its V1 goal arrives at **sim 88-110**, 36-58 sim
s after the slot goal (FINDING sec 1). **G6 is the direct precedent on this exact unit, leg and area:
its area rows landed at sim 76-90 and its V1 goal at sim ~101, and BOTH gates returned success** - the
failure there was the flat query, not the gate. AtInit is rejected because with this init it
materialises all 128 units (~1,732 objects), which is the S4 scale posture whose crawl is precisely
what P18d is trying to read.

The residual risk is stated rather than hidden: G6 ran at 1.663x, this run has six vehicles and may
run several times faster, which shrinks the WALL margin between the area row (9-12 s after placement
when the navData files are in the OS cache, 236 s when they are not - G7b sec 3(b)) and the V1 goal.
Two mitigations, both in the launch script: `--pre-order-settle 240` (a no-op for the area under
AtOrder, kept as a quiet-machine settle), and an explicit read-only warm of
C:\C2SIM\vrf-nav\navData before the run so that the 9-12 s figure is the one in force.
**FALLBACK if P18a misses on the GATE: re-run unchanged except CreationPolicy=AtInit.** That is safe
for the lane: `ExpandCoarseLeaves` APPENDS its synthesised children to `toCreate`
(VrfC2SimService.cs:1268-1281, `toCreate.Add(childPlan)`) before `DeStacker.Apply` runs, and
DeStacker groups by coordinate and indexes members in ascending list order, so the 54 original units
at the authored origin keep their ring slots and 1-35 is born at the same point. That is read from the
code, not from a run; it is a prediction about the fallback, not a premise of this design.

### 6.2 The appData tree

The relocated C:\C2SIM\vrf-appdata\appData (loadAllNavigationDataOnTerrainLoad = 1) is NOT used.
G7b sec 3(a) and 3(c) measured it: the nav-data stream is placement-triggered regardless of the
setting, the curves are the same shape and the same size, and the gates still do not pass at once -
"on this evidence: nothing". Using it would add a second difference against G6, the single-variable
comparator this design rests on. The vendor tree is also what G7b run C - the only run that has ever
planned this way - used.

### 6.3 The SMS override could change more than the query

The custom including SMS overrides one whole Lua file. Verified read-only 2026-09-14: the ONLY
differences between
C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs\scripts\ground-vehicle-move-to.lua and the vendor
copy are line 488 (`useAbstractGraphs = false` -> `true`; `useChannels = true, channelRadius = 4.0`
are unchanged and already the vendor's) and one added printInfo at line 497. If the unit is released
on a straight line, this diff is the first thing to re-read.

### 6.4 Abstract-data coverage

G6's generation produced 8,856 sectors of which **8,810 carry AbstractData** - 46 do not. If the ridge
sectors are among the 46, the query can fail and look like a refusal. Check: the sector indices of the
leg (i = floor((lon+117.019)/0.00547222), j = floor((lat-34.318)/0.00452439)) against any
`not enough (0)` row.

### 6.5 Reading motion out of a trace

FINDING sec 7d: in a collapsed-ratio capture the observer's dead reckoning draws sawtooth excursions
that look like a limit cycle. Any excursion or crawl claim must be checked against the altitude
residual versus the L13 sampler FIRST (clean is within ~0.16 m; G2's post-collapse tail reached
+53.7 m in the air and -139 m underground). The same check decides whether P18b's "net advance under
20 m" is real.

### 6.6 What the instrument cannot carry

G5 established that the object console at level 4 does NOT print commanded-versus-actual speed,
throttle, slope, soil, traction, or any stop / hold / wait row; after the freeze its shape set is
byte-identical to the shape set while moving. So this run CANNOT settle the standing "formation mutual
speed control holds the leader" hypothesis (UG52 30.22 p598) either. It settles routing, and only
routing.

### 6.7 Order-level

`TaskActionCode` MOVE in place of the COA's PENTRT (sec 2.3) - argued to be a no-op and confirmed
offline by the parser and the pre-flight; the OrderID is the COA order's own, which this server has
accepted repeatedly, including for G5's T1-only probe.

## 7. OFFLINE GATES - RUN BEFORE COMMIT, WITH RESULTS

1. **XML well-formed, no illegal comment content**: `xml.dom.minidom.parseString` OK; zero comments
   containing `--`. (The first draft carried `SFGPUCA----F---` inside a comment; the interface parser
   silently reported `Tasks: 0` rather than erroring - recorded, because a silent zero is the failure
   shape to watch for.)
2. **Interface parser offline**:
   `src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.exe --parse-order data\PROBE_RIDGE_1-35_Order.xml`
   -> `Tasks: 1`; task `T1_AOA_SE_1-35_AR;_2/1_AD_P1` uuid cd589832; taskee
   d6df3c3d-f31b-701a-bfc6-2fb9bc86092a; `action: MOVE   ROE: ROEHold`; affectedEntity (none);
   **points: 4** (V0, V1, V2, V3 exactly as the COA order gives them).
3. **Pre-flight on THIS order** (offline cache, nolifeform map, starts_P11.csv):
   `T1_AOA_SE_1-35_AR;_2/1_AD_P1 leg 1 (1-35/2/1_A): 40 m of 0.826 on sand at 34.6562/-116.7619,
   2.0 km from the leg start; Tank Headquarters Section (USA) limit 0.752 (max-slope 0.94 x sand
   0.80); PREDICTED IMPASSABLE (pre-flight estimate, ratio 1.10 vs threshold 0.92)`; legs 2 and 3
   pass at 0.59 and 0.70; 1 leg flagged of 3. Every leg-1 metric is IDENTICAL to the same leg parsed
   out of COA-STP1_Order.xml (length 6593.2852302014235 m, sustained 0.8255225198626558, worst_s_m
   2005.9570821582513, ratio 1.0977693083279998, worst_seg unchanged), the tool drops the same one
   leading vertex and uses the same start - so the probe order IS the COA leg, not a re-derivation
   of it.
4. **Blank lines in the order**: 0. No multi-line comments (STP-795).
5. **Line endings**: order = LF only (0 CR bytes, matching data/PROBE_G7_CrossSector_Order.xml); this
   file = CRLF (matching every other docs/experiments/*.md). INSTRUMENT NOTE: in this Git Bash
   `grep -c $'\r'` reported 57 CR-bearing lines for a file with ZERO CR bytes, and 80 for another -
   it is a FALSE INSTRUMENT here and was replaced by a byte count in python, validated against
   PROBE_G7 (0 CR) and FINDING_EARLY_STOPS (540 CRLF). `core.autocrlf` is true in this repo, so both
   files normalise to LF in the index regardless.
6. **ASCII**: ripgrep `[^\x09\x0a\x0d\x20-\x7E]` over both new files - no matches; the same pattern
   on a deliberately dirty control (scratchpad\ridge\dirty_control.txt: em dash, NBSP, BEL,
   zero-width space) flags 4 of its 5 lines and leaves the clean line alone.
7. **Fixture and SMS, read-only**: R9_Mojave_Empty_52_NavAO_AG.scnx's .scn names
   `C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs.sms` where the non-AG twin names
   `$(DATA_DIR)\simulationModelSets\EntityLevel.sms`; both name the same MojaveCOA .mtf. The Lua diff
   is the two lines of sec 6.3.
8. **Launcher, dry run PASSED** (20260914T183711Z, wrapper exit 0, nothing launched, no run
   directory created, backstop correctly not armed): profile 5.2, scenario
   R9_Mojave_Empty_52_NavAO_AG, gui off; init data/COA-STP1_Initialization.xml with order
   data/PROBE_RIDGE_1-35_Order.xml, clientId C2SIM; type map the repo
   data/unit-type-map-52-nolifeform.json; RunSecs=900, backendNotify=3; observers DERIVED
   1700 s (20+180+120+180+30+run 900+30+settle 240); PreOrderSettleSecs=240; consoles
   object=4 member=4, positionReport=10 s; endpoints 127.0.0.1:18080 / :61614 (the private
   test server); licence SALES-TEMP-10-31-26 valid to 31-oct-2026; thread sampler WOULD
   start against vrfSimHLA1516e for 2235 s.

## 7b. ADVERSARIAL REVIEW OF THIS DESIGN (HEAVY; done before the commit, not after the run)

Four attempts to break the design, and what survives.

1. **"AtOrder will feature-plan the leg and you will measure nothing."** The strongest objection,
   because G7b's own sec 1.5 withdrew exactly that claim for the G7 legs. Weighed and answered in
   sec 6.1: the G7 legs were 476-673 m so their leg-1 goal arrived ~6 s after the slot goal, while
   1-35's arrives 36-58 sim s later, and G6 - the same unit, leg, area and policy - printed BOTH
   gate successes. NOT EXCLUDED: G6 ran at 1.663x and this run has six vehicles, so the wall margin
   is smaller. That is why P18a is the HIGH gate with an explicit stop and a named fallback rather
   than a background assumption, and why the launcher warms the navData cache.
2. **"Two variables against G5, and G6 is not the same run shape."** True, and stated in sec 2.1
   rather than smoothed over. The bracket works only because BOTH controls exist: G6 fixes the area
   (same area, flat query, same metre) and G5 fixes the taskee count (one unit, same metre). If the
   result is a release, the honest claim is "the SMS is the only untested difference", not "the SMS
   caused it" - and the cheap confirmation is G7c (the _AG fixture against run A), already queued
   as G7B_G8_RESULTS NEXT item 1.
3. **"A DETOUR reading could be produced by the formation offset, not by the router."** Guarded by
   the threshold: every flat-mesh and feature track in G7b stayed inside a 60 m corridor that IS the
   formation offset, and X is 150 m. The middle case (a lane shift under 150 m) is registered in
   advance with its own scoring rule so it cannot be claimed as a detour afterwards.
4. **"The pre-flight window is the tool's opinion, so the target may be the wrong ground."** Partly
   right and it is why P18b is scored against the P11/G3/G5 FREEZE POINT - a measured position in
   four runs - and only secondarily against the tool's window. The two are 31-44 m apart, and
   FINDING sec 7e already logs one pre-flight false alarm (4-27/T5 in G3), so the window is
   corroboration, not the target.

UNRESOLVED AND RECORDED: this run cannot separate "cannot move" from "commanded to stop" (sec 6.6 -
the console does not carry velocity), so the formation mutual-speed-control hypothesis of
FINDING sec 7 review item 5 stays open whatever happens; and 46 of 8,856 sectors carry no abstract
data (sec 6.4), which would look like a refusal rather than a design fault.

## 8. RESULTS

(to be filled after the run; nothing in this section was written before it)

## Addendum before the run (supervisor, 2026-09-14 ~18:40Z)
One deviation from the launch line above, recorded BEFORE launch: the run uses the runner's new READY GATE
(`--pre-order-gate nav-area`, commit 7531bc6 - PushOrder waits for the simulator's own "New Primary nav area" row
from a placed platform and logs the placement-to-row delta as WARM/COLD) with the 240 s settle kept ONLY as the
timeout fallback. This does not change the measurement (P18a-P18d) - it replaces a guessed wait with the
simulator's own ready signal (G7B_G8_RESULTS sec 3) - and makes this run the gate's FIRST LIVE USE; if the gate
misfires (pushes before the members can plan, or times out on a warm machine) that is a runner finding to record
in RUNNER_HARDENING sec 15, separate from the ridge verdict. Cache state at launch: warm (seven scenario loads of
the same area in the preceding three hours), so P18a's fallback (AtInit re-run) should not be needed on that account.
