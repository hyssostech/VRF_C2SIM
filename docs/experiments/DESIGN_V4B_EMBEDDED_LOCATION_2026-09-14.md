# V4b - WHAT THE EMBEDDED TASK Location MEANS, PER VERB (design note, 2026-09-14)

Build item V4b of the STP task vocabulary (assessment sec 5, "V4b. What the points MEAN, per
verb"), under the user's R1 ruling of 2026-09-14: the embedded Location inside a task is VALID
C2SIM and stays supported beside MapGraphicID; what is missing is the VERB-TYPED interpretation of
the points, the on-the-fly creation of the VR-Forces control object they describe, and the
reporting of the limits that reading inherits from the export.

Branch `feat/v4b-embedded-location`. Offline only - nothing here has been run against VR-Forces.
Sources: `docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md` (secs 1, 3-5, 7.1),
`docs/experiments/DOCTRINE_FOR_TASKING_RULINGS_2026-09-14.md` sec 1.1,
`src/VrfC2SimApp/TaskGeometryResolver.cs` (V4, merged), `TaskDispatchPolicy.cs`, `OrderParser.cs`,
`VerbMapping.cs`, `data/COA-STP1_Order.xml` and `data/COA-STP1_Initialization.xml` (both
re-measured this pass), and the CWIX2024 schema bindings in
`Library/CS/C2SIMSDK/C2SIMSDK/C2SIM_SMX_LOX_CWIX2024.cs`.

---

## 0. THE DECISION IN ONE PARAGRAPH

An embedded Location list is a bare sequence of geodetic points: the C2SIM schema attaches no
shape, no closure flag, no radius and no graphic type to it (sec 1). So the shape has to be READ,
and the reading is (verb, geometry) -> one of THREE kinds:

  ROUTE           - drive the points in order. Today's behaviour for every verb.
  OBJECTIVE AREA  - the points are a ring: create a VR-Forces control area from them under the
                    TASK's own uuid, and move to its centroid.
  POINT           - one place: move there.

plus NONE, which is not a reading at all - it is R2's in-place execution, unchanged.
MapGraphicID keeps precedence over all of it (V4, `TaskGeometryResolver`): when an id resolves to a
graphic created at init, that graphic's own kind already decided the geometry, V4b does not re-read
it, and it creates nothing (the object exists).

On COA-STP1 as exported TODAY the reading is 9 NONE / 22 POINT / 11 ROUTE / 0 OBJECTIVE AREA -
i.e. **no task changes its dispatch kind**, and the whole COA-STP1 movement path is untouched.
That is not a weak result, it is the measurement (sec 2): STP's export never emits a ring, because
what it linearises is the task's FIRST graphic, which for these 42 tasks is an axis, a task-mission
symbol, or a single point. The area reading exists for the geometry C2SIM CAN carry and STP CAN
export - and for the hand-built and re-exported fixtures that will carry it before the demo.

---

## 1. WHAT C2SIM ACTUALLY GIVES US (schema, read this pass)

`ManeuverWarfareTaskType` (`C2SIM_SMX_LOX_CWIX2024.cs:4026-4194`) carries `Location[]` (:4069) and
`MapGraphicID[]` (:4080). Nothing else in the type is geometric.

`LocationType` (:3610-3625) is a CHOICE of exactly two things:

  - `GeodeticCoordinate` -> `GeodeticCoordinateType` (:3634): `Latitude`, `Longitude`, and the two
    OPTIONAL altitudes `AltitudeAGL` / `AltitudeMSL` (with their `...Specified` flags). That is all.
  - `RelativeLocation` -> `RelativeLocationType`. UNSUPPORTED by this interface (user ruling
    2026-09-14: STP does not emit it). `OrderParser` lifts only the geodetic branch - and, since
    this pass, WARNS when it drops a non-geodetic one instead of dropping it in silence.

Consequences that fix the whole design:

  1. **No shape type.** A polygon, a line and a point are the same XML. Nothing says "this list is
     closed" and nothing says "this is an area".
  2. **No radius.** There is no `Radius` element anywhere in the CWIX2024 binding (grep: zero
     hits). A circular objective cannot be expressed as an embedded Location; a one-point task is a
     POINT objective, and any radius a vendor task needs is that task's own parameter, not
     something C2SIM handed us. The user's R1 note mentions "point-with-radius" as a shape to
     support: it is supportable as a MapGraphic (the init's area graphics), NOT through an embedded
     Location, and this note records that limit rather than inventing a default radius.
  3. **Altitude.** COA-STP1 carries neither AGL nor MSL on any task point (measured), so every
     vertex is authored at elevation 0 and the existing ground-clamp path
     (`Vrf:GroundWaypointAltitudeMode`) decides the altitude of anything driven. V4b changes
     nothing there.

## 1a. WHAT STP PUTS IN IT (the export defect, STP-801)

Carried from the record (`TASK_VOCABULARY_ASSESSMENT` sec 1.3 / 7.1 R1; STP source read in the
2026-09-03 and 2026-09-14 passes, NOT re-read here): `C2SimXmlBuilder.cs` emits `MapGraphicID` only
when `IncludeMapGraphicIdInTasks` is set (`C2SimBridgeAgentParams.cs:128`), which was OFF for both
exports on disk; the `task.Objective` emission branch is commented out entirely; and absent the flag
the exporter writes Location = the FIRST task graphic LINEARISED (a live `// TODO: multiple TGs`
marks the rest being dropped). That is STP-801, on the STP side, and it is the reason this item
exists at all.

Two limits follow from it directly, and both are REPORTED rather than guessed around:

  - **FIRST GRAPHIC ONLY.** A task drawn against an axis AND an objective exports the axis alone.
    No amount of reading recovers the objective; only the re-export does.
  - **A POINT CANNOT IDENTIFY ITS GRAPHIC.** Measured this pass: T2's single exported point
    coincides EXACTLY (to 1e-6 deg) with 11 init graphics and T3's with 14 - among them, in T2's
    case, the `OBJ_OBJ_MA`, `OBJ_BND_OB`, `OBJ_OBJ_00` and `OBJ_OBJ_01` areas and the `PL_OBJ_MAD`
    line. This is why V4's name/geometry heuristic was WITHDRAWN under R1 and why V4b does not
    resurrect it: at a cluster like that, "nearest graphic" is a coin toss with four faces.

---

## 2. THE CENSUS - WHAT SHAPE EACH OF THE 42 TASKS ACTUALLY CARRIES

Measured this pass directly from `data/COA-STP1_Order.xml` (scratchpad `census.py`, `shapes.py`,
`matchgfx.py`); the verb counts reproduce the assessment's sec 1.1 table exactly, the SHAPE columns
are new.

Whole-order facts: 42 tasks, 17 verbs, **0 MapGraphicID**, point histogram **0 x9, 1 x20, 2 x2,
3 x2, 4 x9**, and - the finding that shapes the rule - **ZERO closed rings**: no task's first point
equals its last.

| n | tasks | what the points are | kind read |
|---|-------|---------------------|-----------|
| 0 | T8, T9, T10, T16, T21, T24, T34, T37, T38 (9) | nothing | NONE (R2, in place) |
| 1 | T4, T5, T6, T7, T11, T12, T14, T18, T20, T22, T25, T26, T27, T28, T29, T30, T33, T40, T41, T42 (20) | one place | POINT |
| 2 | T2 (FIX), T3 (ATTACK) | TWO IDENTICAL POINTS, 0 m apart | POINT (coincident) |
| 3 | T13 (BREACH), T36 (CLRLND) | a 3-vertex figure about 360 m across | ROUTE (+ symbol note) |
| 4 | T1, T15, T17, T19, T23, T31, T32, T35, T39 | an axis of advance, 23-42 km long | ROUTE |

Per-verb totals of the reading: NONE 9 (ATTACK 2, DEFEND 2, BREACH 1, MOVE 1, RETAIN 1, SCREEN 1,
SECURE 1); POINT 22; ROUTE 11; OBJECTIVE AREA 0.

Those seventeen verbs are the COMPLETE set in this order - ATTACK, SECURE, FIX, OCCUPY, BREACH,
SCREEN, PENTRT, BLOCK, DESTRY, DISRPT, DEFEND, MOVE, ESCRT, GUARD, SEIZE, RETAIN, CLRLND. FOLSPT,
FOLASS and NTRCOM are NOT among them even though the init draws their FM 3-90 Appendix B symbols
(three FOLLOW AND SUPPORT `GFTPAS`, one FOLLOW AND ASSUME `GFTPA`, one NEUTRALIZE `GFTPN`): the
exported TaskActionCode disagrees with the drawn task symbol in at least four places. That is the
pass-1 doctrine finding, it is on the STP side, and it is still open there - recorded here because
a verb that never arrives cannot be read, however good the reading is.

### 2.1 The three measurements that decide the rule

**(a) The axes are not rings, and the arithmetic says so.** For the nine 4-point tasks the
straight-line distance from the last point back to the first is 0.998 of the total path length
(e.g. T32 SEIZE: legs 7,093 + 11,791 + 4,743 m = 23,627 m of path, 23,579 m from end back to
start). A ring returns toward its start; these never turn back. **T32 matters: it is a SEIZE - an
area verb - with four points.** A rule that read "area verb and n >= 3 means area" would collapse a
23.6 km axis of advance to a centroid and the battalion would stop driving it. So the rule must
test the SHAPE, not only the verb.

**(b) The init's areas ARE explicitly closed, so the closed-ring test is calibrated on real
authored data.** All 12 multi-vertex tactical areas in `COA-STP1_Initialization.xml` (MADISON,
MONROE, JEFFERSON, HAMILTON, EAGLES, NORMANS, SAXONS, OILERS, IRON_FIST, BANDIT, BANDIT_II,
BANDIT_III) repeat their first vertex as their last: end-to-start distance 0 m, closure ratio 0.000,
against perimeters of 11.8-46.5 km. The remaining 23 "areas" are degenerate 1-point graphics. So
when C2SIM authoring means a ring, it CLOSES the ring - the first-equals-last test is the primary
discriminator, not a guess.

**(c) The two 3-point tasks are TASK-MISSION SYMBOLS, not geometry to drive.** T13's three points
match init TaskGraphic `__FRIEN_16` EXACTLY, and T36's match `__FRIEN_13` (vertex for vertex, to
1e-6 deg). Doctrine says what those symbols are: for BREACH, "the area located between the arms of
the graphic shows the general location for the breach ... the length of the arms extend to include
the entire depth of the area that must be breached" (FM 3-90 B-8); for CLEAR, "the bar connecting
the arrows designates the desired limit of advance for the clearing force ... also establishes the
width of the area to clear" (FM 3-90 B-17). So the three points are the SYMBOL's anchor points -
neither a path nor a polygon. They are read as a ROUTE (which is what the interface does today, and
a 629 m drive around the symbol is harmless), and the log SAYS they are a symbol so that V5/V6 can
bind them to the vendor parameter they really are (`company_breach` lane1/lane2, `co_clear` limit of
advance) instead of re-deriving this from the XML later.

---

## 3. THE INTERPRETATION RULE

Pure function, `TaskGeometryInterpretation.Classify(actionCode, points)`. It is applied ONLY to
geometry that came from the embedded Location (`GeometrySource.EmbeddedLocation`); geometry that
came from a `MapGraphicID` was already typed by the graphic it names.

    n == 0                                          -> NONE           (R2 dispatches in place)
    all points within PointCoincidenceMeters (10 m) -> POINT          (covers n == 1, and T2/T3)
    first == last (within 10 m), >= 3 distinct pts  -> OBJECTIVE AREA (any verb - see 3.1)
    area verb and n >= 3 and closure ratio <= 0.75  -> OBJECTIVE AREA (an unclosed but returning ring)
    otherwise                                       -> ROUTE

`closure ratio` = (distance from the last point back to the first) / (total path length). It is 0
for an explicitly closed ring, at most 1 by the triangle inequality, and about 1.0 for a monotone
axis; measured values are 0.000 on the init's 12 closed areas, 0.43 on the two 3-point symbols and
0.998 on the nine COA-STP1 axes. The 0.75 threshold sits in the empty middle of that distribution.

"area verb" is NOT a second table: it is
`VerbMapping.Classify(code).Intent == TaskIntent.HoldObjective`, the same table the dispatch already
uses - SECURE, OCCUPY, SEIZE, RETAIN, BLOCK, DEFEND, GUARD. (The brief's fourth example, HOLD, is
not a C2SIM code: it is in neither STP's 51 emittable codes nor our verb table. A HOLD-like task
arrives as one of those seven, and a closed ring is read as an area under ANY verb anyway.)

### 3.1 The ambiguity rules, stated

- **ATTACK with a closed ring: OBJECTIVE, not route.** Doctrine is unambiguous - an attack's
  minimum control measures are an LD, a time, and "the objective", where the objective is the area
  graphic `G*GPOAO---` (FM 3-90 5-8). Driving the ring would walk the unit around the perimeter of
  its own objective. The ring branch of the rule is therefore verb-INDEPENDENT: a closed ring is an
  area for ATTACK, PENTRT, BREACH and MOVE exactly as for SEIZE.
- **MOVE with a ring: go to the centroid, do NOT lap the ring.** A move to an area is a move to the
  area, and the vendor primitive for it is a move-to (the same one the MapGraphicID path already
  takes for an init area - `TaskGeometryResolver` collapses an area graphic to its centroid). A
  patrol of the perimeter is a different task (`DtPatrolRouteTask`) that the order did not ask for,
  and issuing it would turn a one-shot MOVE into a task that never self-completes
  (HELP/Tasks/MovementTasks/PatrolRoute.htm: "continues going back and forth until given another
  command") - which would then hang its STREND successors on a gate only R4's end time could
  release. So: centroid.
- **An area verb with a long open polyline: ROUTE** (the T32 SEIZE case, sec 2.1a). The verb says
  "area", the geometry says "axis", and the geometry is the thing that was actually exported. It is
  driven, and the log names the disagreement.
- **A non-area verb with a compact, unclosed figure: ROUTE, plus a symbol note** (T13/T36, 2.1c).

### 3.2 What each kind DOES in the dispatch

| kind | points handed to the mover | VR-Forces object created | resulting dispatch |
|------|---------------------------|--------------------------|--------------------|
| NONE | none | none | R2 in-place, unchanged |
| POINT | the one point (coincident duplicates collapsed) | none | live position + 1 vertex -> CreateRoute + MoveAlongRoute, as today |
| ROUTE | every point, in order, unchanged | none (beyond the route object the move already creates) | unchanged |
| OBJECTIVE AREA | ONE point: the ring's centroid | ONE control area, from the ring's vertices, under the TASK's uuid | move to the centroid |

The area is created through **the same factory the init uses** - `_bridge.CreateControlArea(pts,
name, "TacticalArea", uuid)` marshalled onto the tick thread, with the name REGISTERED in the
`NameRegistry` before the create is enqueued (the tasking-foundation rule: an unregistered name
falls through to the prefix scan and can bind a graphic's uuid under a unit's name). The
registration-then-enqueue pair is now one private method, `EnqueueControlAreaCreate`, called by the
init loop and by the task path; the only thing that differs is the dedupe key (`area:<uuid|name>`
for the init, `taskarea:<task uuid|name>` for a task).

  - **uuid = the TASK's C2SIM uuid.** This is the same identity policy the init uses (the C2SIM uuid
    IS the VR-Forces uuid: `createControlArea` and its neighbours take a `startingUUID`,
    `vrfRemoteController.h:991-1108`), so V5/V6 can bind the objective parameter of `company_seize`
    / `unit-attack-to-objective` to the task's own uuid with no extra map.
  - **name = "<TaskName> OBJECTIVE"**, the same convention as the existing "<TaskName> ROUTE".
  - **EXACTLY ONCE PER TASK.** The guard is the existing `_createdAreaKeys` dictionary, so the
    TerrainProfile re-entry into `ExecuteTaskOnTick` (which re-runs the whole geometry block with
    the same task) and a duplicated order delivery both create one object, not two or three.
  - **ONLY when no MapGraphicID resolved.** If the order names the graphic, the init already created
    it under that uuid and a second object would be a duplicate of it.
  - **Config:** `Vrf:CreateTaskObjectiveAreas`, default TRUE. The INTERPRETATION is not behind the
    flag - the centroid move happens either way - only the object creation is, so an operator who
    does not want extra control objects on the map can turn it off without changing where units go.

### 3.3 The centroid, and the debt it inherits (C12 - NOT fixed here)

The centroid is `TaskGeometryResolver.Centroid`, reused rather than re-implemented: the arithmetic
mean of the vertices. That is deliberately the SAME rule the MapGraphicID path applies to an init
area, so an order that names its objective and an order that embeds it arrive at the same point.

It is also the same DEBT. Pass-2 review item **C12** (`TASK_VOCABULARY_ASSESSMENT` sec 7.1b): the
vertex arithmetic mean is not the polygon centroid, and on the init's 12 multi-vertex areas it sits
149-1,130 m from the true centroid, against a 500 m arrival radius. V4b does NOT fix C12 - a
centroid change would move the MapGraphicID path too, which is merged, live-gated work - it
INHERITS it and cites it here so that the fix lands once, for both paths.
One thing V4b does do, because it is new behaviour rather than a change to existing behaviour: when
the ring is EXPLICITLY closed, the repeated final vertex is dropped before the mean, so the first
vertex is not weighted twice.

---

## 4. PER-VERB TABLE: WHAT IS EXPORTED, WHAT IS READ, WHAT THE VENDOR TASK WANTS

"exported shape" = what COA-STP1 actually carries for that verb (sec 2). "vendor task needs" is from
the assessment secs 3.2/3.3/4 and the doctrine table sec 1.1; EL = EntityLevel scripts, ATL =
AggregateTacticalLevel. "V4b reading" is what this item does TODAY.

| verb | n | exported shape | V4b reading | doctrinal graphic (FM 3-90) | vendor task needs | item |
|------|---|----------------|-------------|------------------------------|-------------------|------|
| ATTACK | 10 | 4-pt axis x4, 1-pt x3, 2-pt coincident x1, none x2 | ROUTE / POINT / NONE | axis + objective AREA (5-8) | `unit-attack-to-objective` (ATL): objective + route; `plt_movement_to_contact` (EL): axis + objective | V6 |
| SECURE | 4 | 1-pt x3, none x1 | POINT / NONE | AREA, "includes the entire area to be secured" (B-56) | `company_seize` then hold: the area | V5 |
| FIX | 3 | 4-pt axis, 2-pt coincident, 1-pt | ROUTE / POINT | enemy-oriented ARROW (B-35) | `plt_attack_by_fire` (EL): line of fire + enemy area | V6 |
| OCCUPY | 3 | 1-pt x3 | POINT | AREA, "should encompass the entire area" (B-53) | `occupy_firing_positions` (EL) / `unit-defend` (ATL): the position area | V5 |
| BREACH | 3 | 3-pt SYMBOL, 4-pt axis, none | ROUTE / NONE | the area between the arms; a lane is a 2-pt LINE (B-8) | `company_breach`: objective, lane1, lane2, enemyArea | V5 |
| SCREEN | 3 | 1-pt x2, none x1 | POINT / NONE | oriented on the protected force; phase lines / OPs (13-54) | `patrol_route_script_with_options` (EL) / `reconnoiter-route` (ATL): a ROUTE + duration | V7 |
| PENTRT | 2 | 4-pt axis x2 | ROUTE | narrow-front axis + objective beyond it (2-36) | as ATTACK | V6 |
| BLOCK | 2 | 1-pt x2 | POINT | line perpendicular to the enemy advance (B-5) | defensive composition on an area | V5 |
| DESTRY | 2 | 1-pt x2 | POINT | enemy-oriented symbol on the target unit (B-23) | fires onto the objective (R3) | V6 |
| DISRPT | 2 | 1-pt x2 | POINT | "the center arrow points toward the targeted enemy unit" (B-29) | attack-by-fire / `provide_suppressive_fire` | V6 |
| DEFEND | 2 | none x2 | NONE (R2) | battle position AREA `G*GPDAB---` | `automatic_air_defense` (ATL) takes an AREA | V8 |
| MOVE | 1 | none | NONE (R2) | a route or a destination point | move-along / move-to - already wired | - |
| ESCRT | 1 | 1-pt | POINT | the ROUTE and the escorted force (route security) | `unit-ground-follow` (EL): followSubject + offset | V10 |
| GUARD | 1 | 1-pt | POINT | oriented on the protected main body (13-72) | as SCREEN | V7 |
| SEIZE | 1 | 4-pt axis | ROUTE | "the arrow points to the location or objective to seize"; objective `G*GPOAO---` (B-57) | `company_seize`: enemyArea + departLine + assaultRoute | V5 |
| RETAIN | 1 | none | NONE (R2) | AREA, "includes the entire area to be retained" (B-55) | defensive composition on an area | V5 |
| CLRLND | 1 | 3-pt SYMBOL | ROUTE (+ symbol note) | arrows + the bar = limit of advance and width (B-17) | `co_clear`: Limit of Advance LINE + Starting Point | V5 |

Read the table as the gap list it is: for eleven of the seventeen verbs the vendor task wants an
AREA or a LINE, and what the export gives is a single point or an axis. V4b closes the half that is
decidable from the data (read a ring as an area, create it, move to it); the other half closes when
STP re-exports with `IncludeMapGraphicIdInTasks=True` (sec 5), not before.

---

## 5. WHAT CHANGES WHEN STP RE-EXPORTS WITH MapGraphicID ON

Every task then carries the uuid of the graphic it was drawn against, the init already created that
graphic under the same uuid (35 areas today, plus the 41 lines and 317 points registered by M5), and
`TaskGeometryResolver`'s FIRST branch takes over:

  - the area resolves to its centroid and the line/point to its vertices - typed by the graphic,
    with no shape inference at all;
  - **the on-the-fly object becomes unnecessary** and is not created (the create is refused whenever
    the source is `MapGraphic`), because the object it would duplicate already exists and already
    carries the C2SIM uuid the vendor task will bind to;
  - the embedded Location is still parsed, still logged, and still cross-checked: the existing m7
    consistency check reports how far the two answers are apart and WARNS past 1 km;
  - the STP-801 marker stops appearing in the log, which is the one-line way to tell the two worlds
    apart in a run log.

V4b is therefore a TRANSITION reading, exactly as V4 was, and it is written so that better data
switches it off rather than fighting it.

---

## 6. WHAT THIS DOES NOT DO (limits, reported not guessed)

1. **No name or proximity matching.** Withdrawn under R1 and not resurrected. Sec 1a shows why it
   cannot work on a point: 11 and 14 coincident graphics.
2. **The 3-point task-mission symbols are driven, not understood** (T13, T36). They get a note in the
   log naming the shape; binding them to `company_breach` / `co_clear` parameters is V5.
3. **C12 stands**: vertex-mean centroid, 149-1,130 m off on the init's 12 multi-vertex areas.
4. **Convexity is not checked.** `createControlArea`'s header says the vertices "should form a convex
   polygon" (`vrfRemoteController.h:1091-1108`) and STP rings are arbitrary polygons. The init
   already sends 12 such rings, so V4b takes no NEW risk - but neither does it clear the one that is
   there. Live gate, not an offline question.
5. **No vendor tactical task is issued.** OBJECTIVE AREA changes WHERE the unit is sent (the
   centroid) and creates the object a later item will pass as a parameter; it does not make the unit
   seize, occupy or defend anything. That is V5/V6.
6. **RelativeLocation** is parsed only far enough to warn that it was dropped.
7. **Nothing here has been run against VR-Forces.** The offline evidence is the `--rulings-selftest`
   V4b section and the `--parse-order` shape census; the live gate is in sec 7.

---

## 7. THE LIVE GATE (one run, when a run is next available)

An order whose task carries a CLOSED RING (a hand-built fixture today, an STP re-export later) must,
in one run:

  1. log `embedded Location read as ObjectiveArea (N points, verb SEIZE)` for that task;
  2. produce exactly ONE ObjectCreated for the area named `<TaskName> OBJECTIVE`, carrying the
     TASK's uuid - and, when the same task is re-entered through the TerrainProfile reply, still
     exactly one;
  3. drive the taskee to the ring's centroid and not around its perimeter;
  4. on the SAME order with `MapGraphicID` present, log the MapGraphic path instead and create NO
     on-the-fly area;
  5. leave the COA-STP1 run unchanged in its movement commands: 9 in place, 22 single-point moves,
     11 routes, no area created (the offline census asserts this shape; the run confirms that the
     command stream still matches it).
