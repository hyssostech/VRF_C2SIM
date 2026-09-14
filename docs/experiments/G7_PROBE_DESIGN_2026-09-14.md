# G7 probe design (Opus executor, 2026-09-14 ~13:20Z; supervisor-adopted in PREREG_MESHQUERY_G7_2026-09-14.md)

# G7 - CROSS-SECTOR PROBE ON MojaveCOA: is the 8,856-sector graph connected across seams?

Authored 2026-09-14 by an Opus executor. DESIGN ONLY - nothing was launched, no order was
pushed, no file under C:\MAK was written, scripts\ and tools\FixtureGen were not touched.
Deliverables: data\PROBE_G7_CrossSector_Order.xml (new) and this file.
Working scripts: scratchpad\g7\{frame.py,frame2.py,explore.py,explore2.py,explore3.py,
final.py,ecef.py,asciichk.py}.

## 0. THE QUESTION AND THE DISCRIMINATOR

scratchpad\meshq\MESH_QUERY_VS_DISTANCE.md sec 0 / 5 / 10 leaves exactly two live readings of
G6:

  (A) the big graph is REFUSED or budget-cut by the planner (the failures are flat and fast:
      0.4-1.3 s from the destination gate, the same band as that run's 12-35 m successes and
      faster than the 10 km successes on MojaveAO20 - ibid. sec 7);
  (B) MojaveCOA's DATA is not connected across sector boundaries, so any query that must
      leave its start sector returns nothing. Ibid. sec 10 records that nothing in the four
      captures separates them.

The separating measurement (ibid. sec 10, sec 11 item 1): one ground unit on MojaveCOA,
console level 4, tasked along a route whose first leg is ~600 m and crosses exactly ONE
sector seam, then ~2 km, then ~5 km.

  leg 1 plans AND leg 3 returns 0  -> the graph IS connected across seams; the failure is
                                      length/budget related. (B) falsified, (A) supported.
  leg 1 returns 0                  -> the data is unusable; G6's whole "area size" reading
                                      is WITHDRAWN and nothing about 8,856 sectors was learned.
  all three plan                   -> the ceiling lies between 5 and 9.3 km (the shortest
                                      failing goal G6 measured was 9,346.5 m).

## 1. THE SECTOR GRID - DERIVATION, WITH THE LINES IT RESTS ON

Runtime config read 2026-09-14:
`C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveCOA.navRuntimeConfig`

    (extent-nw  -27176.000000 20597.000000 609.520371)
    (extent-ne   27262.000000 20597.000000 609.010425)
    (extent-sw  -27176.000000 -20597.000000 609.008580)
    (extent-se   27262.000000 -20597.000000 608.496368)
    (offset  -2366434.263044 -4700193.403656 3592989.815530)
    (nav-data-path "C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveCOA")
    (cs-type 4)

Generation config, identical in both copies
(`C:\Users\PAULOB~1\Temp\nav\cfg\NavArea-ground-platform MojaveCOA.navGenConfig` and the copy
inside the area directory):

    (extent-nw  -2385325.992991 -4677630.523786 3609949.641647)
    (extent-ne  -2336950.682038 -4701985.635837 3609949.641647)
    (extent-sw  -2395917.844051 -4698401.171474 3576029.989414)
    (extent-se  -2347327.726362 -4722864.430470 3576029.989414)
    (tile-count-x 108)  (tile-count-y 82)
    (raster-precision 0.200000)  (cell-size 43)

Those four geocentric corners convert (WGS-84, scratchpad\g7\frame.py) to

    nw 34.689000000 / -117.019000000   ne 34.689000000 / -116.428000000
    sw 34.318000000 / -117.019000000   se 34.318000000 / -116.428000000   (all at h = 700.000 m)

i.e. EXACTLY the bounds declared in docs\experiments\PREREG_NAVDATA_G6_2026-09-13.md sec 1
(lat 34.318-34.689, lon -117.019 to -116.428, 108 x 82 tiles). The declared bounds are now
CONFIRMED from the runtime data rather than carried over from the prereg - MESH_QUERY sec 9
flags that they had only ever been taken from the preregs.

The corners are a rectangle in LAT/LON, not in the tangent plane: nw and sw share
x = -27176.000 exactly, while a true ENU projection about the offset puts them 120 m apart
(frame2.py: nw east -27020.2, sw east -27140.2). So the config's local frame is a
lat/lon-linear (equirectangular) projection, and the tile grid is a uniform division of the
lat/lon rectangle:

    d_lon = (-116.428 - (-117.019)) / 108 = 0.00547222 deg  -> 501.9 m at lat 34.61
    d_lat = ( 34.689  -   34.318  ) /  82 = 0.00452439 deg  -> 501.9 m

    sector i = floor((lon + 117.019) / 0.00547222),  i in 0..107
    sector j = floor((lat -  34.318) / 0.00452439),  j in 0..81

ANCHOR: the config does NOT state which corner carries index (0,0). The file names on disk are
`ground-platform_<i>_<j>_Si0e.NavData` with i in 0..107 and j in 0..81 (8,856 triples, 26,524
files, verified by listing the profile directory), which fixes the RANGES but not the corner.
The indices above assume the south-west corner, as the brief directs. The assumption affects
only the LABELS: the grid LINES are lines of constant longitude and constant latitude at the
spacings above whichever corner is index 0, so every crossing COUNT in sec 2 holds under
either convention.

Sanity check against MESH_QUERY sec 3: 54,200/108 = 502 m and 41,300/82 = 504 m there, 501.9 m
here - the difference is that the config's own metric (92,111.7 m per degree of longitude) is a
scale valid near the area's south edge, while 501.9 m is the ground distance at the working
latitude 34.61.

## 2. THE ROUTE

PERFORMER: 1.BdeHQ, UUID 670cfdb2-6c43-f267-ad7f-bd6e739def24, authored at
34.608415817915 / -116.712685404877 (data\R9_Mojave_Lean_Initialization.xml:65-66,
`<Name>1.BdeHQ</Name>` at :74).

    S  34.608415818  -116.712685405   sector (55,64)   edge margins N 8.94 S 32.22 E 26.11 W 28.10 km
    V1 34.608651740  -116.719220764   sector (54,64)   edge margins N 8.91 S 32.24 E 26.71 W 27.50 km
    V2 34.606767214  -116.740906656   sector (50,63)   edge margins N 9.12 S 32.03 E 28.70 W 25.51 km
    V3 34.602055897  -116.795120162   sector (40,62)   edge margins N 9.64 S 31.51 E 33.68 W 20.54 km

Bearings: S->V1 272.5 deg (600 m), V1->V2 and V2->V3 264.0 deg. Minimum distance from any
vertex to an area edge: 8.91 km, far above the 1 km the brief requires.

| leg | from -> to | length m | sector step | lon-line crossings (m along leg) | lat-line crossings | total seams |
|---|---|---|---|---|---|---|
| 1 | S -> V1 | 598.7 | (55,64) -> (54,64) | 55 at 489 | none | **1** |
| 2 | V1 -> V2 | 1,995.7 | (54,64) -> (50,63) | 54, 53, 52, 51 at 394 / 897 / 1,401 / 1,905 | 64 at 1,155 | 5 |
| 3 | V2 -> V3 | 4,989.4 | (50,63) -> (40,62) | 50..41 at 413 / 916 / 1,420 / 1,923 / 2,427 / 2,931 / 3,434 / 3,938 / 4,441 / 4,945 | 63 at 3,951 | 11 |

Total route 7,583.8 m over 4 points: the interface prefixes the unit's live position, so the 3
authored vertices give a 4-point route. G1's own log shows the pattern -
`Task 'T_R5_TK1': CreateRoute 'T_R5_TK1 ROUTE' (3 pts)` for a 2-vertex task.

LEG 1 IS THE MEASUREMENT. It crosses the lon-line at index 55, 489 m from the start, and V1
lands 110 m inside the next sector - the brief's "~100 m past the boundary". Legs 2 and 3 are
the length arms.

HONEST NOTE ON THE START'S OWN CLEARANCE. S sits only 11.9 m WEST of the lon-line at index 56
(its clearances are W 490.0, E 11.9, S 94.8, N 407.1 m). The route therefore runs WEST, away
from the near line, so the leg-1 count is 1. If the created position were more than 11.9 m east
of the authored longitude, S would sit in column 56 and leg 1 would cross two seams instead of
one. That is unlikely - G1's own placement line reads "PLATFORM 1.BdeHQ~PXY domain=1 created at
authored lat/lon" - and, more to the point, it does not change any of the three verdicts in
sec 0: a 599 m goal one OR two sectors away answers the same question. Read the run's first POS
fix and re-derive the sector before writing the result. The alternative starts are worse on
other axes: 114.MechCoy sits 72.3 m from its north line and brings 48 members; 1222.MechPlt is
comfortably mid-column (240.7 / 261.2 m) but brings 16 members including 12 dismounts.

## 3. WHY THIS PERFORMER (a deviation from the brief's "tank platoon-or-smaller")

The R9 init has no tank-typed unit. The type map - both data\unit-type-map-52.json and
data\unit-type-map-52-nolifeform.json carry the IDENTICAL row F-UCIZ-H - resolves:

  - 1222.MechPlt, 114.MechCoy and its three platoons -> "Mechanized Platoon (USA) IFV
    (Deprecated)", 16 or 48 members each, including 6 R + 6 AT4 dismounts per platoon;
  - 1.BdeHQ -> M577A2_Command_Post, fidelity PROXY, isAggregate false: ONE ground platform.

1.BdeHQ is chosen because it is the smallest thing this init can task and it removes three
confounds at once: no formation / move-into-formation gate (DESIGN_ORBAT C1b), no composition,
and no lifeform creation at all (the DI-Guy data package is still absent and the headless sim
crashes at the first DI-Guy human). One entity also means ONE console stream and exactly one
mesh query per leg, so the pairing of goal row to outcome row is direct - the labelling that
MESH_QUERY sec 9 lists as INFERRED stops being an inference.

Transfer caveat, recorded rather than buried: the G2->G6 flip set is dominated by M1A2s, and
MESH_QUERY sec 10 leaves an UNEXPLAINED per-vehicle-type asymmetry (HMMWVs failed at 9.5-9.7 km
while M1A2s of the same company planned longer goals in the same wall second). An M577A2 is not
an M1A2. It is, however, one of the 22 documented flips itself - M577A2 7, 9,630.5 m planned on
MojaveAO20 -> 0 points on MojaveCOA (MESH_QUERY sec 5c) - so it is a proven mesh client on the
small area. If the supervisor wants the M1A2 arm it costs a second run on a COA-STP1 unit and
should be registered separately; it is not needed for the sec-0 discriminator.

Also relevant: with Vrf__CreationPolicy=AtOrder, an order naming only 1.BdeHQ creates only
1.BdeHQ. G1 created 6 units from an order naming 3, the extra 3 being 114.MechCoy's own
subordinates; 1.BdeHQ has no subordinates, so the expectation is a ONE-entity federation. If
the run shows more, nothing breaks - only 1.BdeHQ is tasked - but the one-query-per-leg
simplification weakens, so check the creation count first.

## 4. PRE-FLIGHT (tools\preflight\leg_check.py, the committed tool, real invocation)

    python tools\preflight\leg_check.py ^
      --order data\PROBE_G7_CrossSector_Order.xml ^
      --init  data\R9_Mojave_Lean_Initialization.xml ^
      --typemap data\unit-type-map-52.json ^
      --no-starts --text --verbose --json <out>.json

`--no-starts` because starts_P11.csv holds COA-STP1 units only and R9's units are dispersed, so
the authored position IS the start. The tile cache was pointed at scratchpad\g7\preflight_cache
- a copy of the committed cache - so nothing was written into the repo; 0 tiles had to be
fetched for the final route.

Result - 0 legs flagged of 3, 0 skipped as under 1 m, 0 without a verdict:

| leg | length m | sustained 40 m | short 20 m | soil at worst | limit | ratio (M577A2) | ratio vs M1A2 0.94 | climb / descend m | NaN |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 598.7 | 0.0762 | 0.1304 | sand (BM_SAND, CA FVEG 15 m) | 0.800 | **0.095** | 0.101 | 5 / 10 | 0.000 |
| 2 | 1,995.7 | 0.1542 | 0.1663 | sand (BM_SAND, CA FVEG 15 m) | 0.800 | **0.193** | 0.205 | 35 / 90 | 0.000 |
| 3 | 4,989.4 | 0.0395 | 0.0598 | sand (BM_SAND, CA FVEG 15 m) | 0.800 | **0.049** | 0.052 | 50 / 47 | 0.000 |

Worst 40 m windows: leg 1 at 34.608642 / -116.718959 (575 m along, z 1,147.3 m); leg 2 at
34.608599 / -116.719828 (56 m along, z 1,156.8 m); leg 3 at 34.604102 / -116.771576 (2,823 m
along, z 1,086.0 m). All three on sand; the tool resolves the performer's limit as M577A2
max-slope 1.00 x sand 0.80 = 0.800.

The design gate the brief set was ratio < 0.6 of the vehicle limit on every leg. The worst leg
is 0.205 against the conservative M1A2 limit (0.94 x 0.80 = 0.752) and 0.193 against the
performing vehicle's own. That is 4.5x inside the tool's 0.92 flag threshold and below every
clean mover in its calibration table (0.438-0.870). A stop on this route is NOT a slope stop.
The bearing pair was chosen by scanning 0.5 deg steps for leg 1 and 4 deg steps for legs 2-3
under the constraints "exactly one seam on leg 1", "the leg comes no closer than 60 m to a seam
it does not cross", ">= 1.5 km edge margin" and "turn <= 20 deg" (explore3.py; 155 candidates
met them and this is the minimum-max-ratio one).

INDEPENDENT CHECK OF THE ELEVATION SOURCE AT THIS EXACT POINT: the pre-flight reads 1,154.7 m
under S. G1's own placement line for the same lat/lon reads "terrain height under the create
point: 1154.7 m" (runs\20260907T170643Z_run\vrfc2simapp.log:81). The tool and the sim's own
terrain query agree to 0.1 m at the start point.

Caveats carried from the tool's README, not re-argued here: the soil hop is ASSUMED except sand
(and every window here IS sand, the one row FINDING sec 7 confirms end to end); the limit is
`max-slope x acceleration-factor`, a nav-mesh path-cost formula used as an analogy; and legs are
sampled as straight lines, which is what the vendor drives where no mesh covers the leg.

## 5. WHAT TO RUN

    INIT     data\R9_Mojave_Lean_Initialization.xml
             (G1's init, per run-manifest.json of runs\20260907T170643Z_run; 1.BdeHQ is inside
             MojaveCOA with 8.9 km of margin)
    ORDER    data\PROBE_G7_CrossSector_Order.xml   (this design)
    FIXTURE  R9_Mojave_Empty_52_NavAO
    TYPE MAP data\unit-type-map-52-nolifeform.json

FIXTURE VERIFIED 2026-09-14 by reading
C:\MAK\vrforces5.2d\userData\scenarios\R9_Mojave_Empty_52_NavAO.scnx (a ZIP): it names
tools\navdata\out\"MAK Earth (online) + MojaveCOA.mtf", and G6's own manifest records scenario
R9_Mojave_Empty_52_NavAO. R9_Mojave_Empty_52_Nav.scnx names the MojaveAO20 terrain copy instead.
NOTE a sibling R9_Mojave_Empty_52_NavAO_AG.scnx is also deployed on the SAME MojaveCOA terrain
copy - do not pick it up by mistake.

TYPE MAP: row F-UCIZ-H is byte-identical between the repo map and the PROBE nolifeform map, so
the performer resolves the same either way; the nolifeform map is named for parity with G6.

Environment - the P11/G2/G3/G6 block, with member consoles raised to 4 per the brief. The only
delta from docs\experiments\PREREG_NAVDATA_G3_2026-09-13.md sec 1 is
ObjectConsoleMemberNotifyLevel 3 -> 4:

    export Vrf__TypeMappingMode=FidelityTable Vrf__CreationPolicy=AtOrder \
           Vrf__DeStackCreates=true Vrf__DeStackSpacingMeters=700 Vrf__DeStackRotationDeg=0 \
           Vrf__DropOriginVertexMeters=100 Vrf__TaskPredecessorTimeoutSeconds=7200 \
           Vrf__ObjectConsoleNotifyLevel=4 Vrf__ObjectConsoleMemberNotifyLevel=4 \
           Vrf__PositionReportSeconds=10

    pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\RunC2SimScenario.ps1 \
         -VrfProfile 5.2 -NoGui -Scenario R9_Mojave_Empty_52_NavAO \
         -Init data\R9_Mojave_Lean_Initialization.xml \
         -Order data\PROBE_G7_CrossSector_Order.xml -ClientId STP \
         -TypeMapFile data\unit-type-map-52-nolifeform.json \
         -RunSecs 900 -WatchSecs 1200 -BackendNotifyLevel 3 -StopWhenComplete

`-ClientId STP` is what G1 used with this init (G6 used C2SIM with the COA-STP1 init).
DropOriginVertexMeters stays at 100 m: no authored vertex is within 100 m of the authored
position, so none is dropped and all three legs are driven.

RUN LENGTH. G1's own console clock gives this vehicle's speed: T_R5_TK1's 1,156 m route ran
from sim 55.266 (controller begins) to sim 173.732 (controller Completed) = 118.5 sim s, i.e.
9.8 m/s. 7,583.8 m at 9.8 m/s = 774 sim s, plus ~55 s to dispatch and ~60 s of turn and
acceleration overhead = ~890 sim s. At the fixed-frame ratio of ~1.6x that is ~556 wall seconds
of observation window; G1 ran at 3.95x on 6 units and this run has one entity, so expect faster.
-RunSecs 900 -WatchSecs 1200 (G6's numbers, unchanged) gives 1,440 sim s of headroom at 1.6x,
and -StopWhenComplete closes the window at arrival. -RunSecs is a WALL-clock cap on the
observation window (scripts\RunC2SimScenario.ps1:158-170).

The DECISIVE row arrives within seconds of the order push - leg 1's query fires at task
dispatch - so even a truncated run answers the primary question. Legs 2 and 3 are only queried
when the vehicle reaches V1 (~61 sim s later) and V2 (~204 sim s after that), so the window has
to be long enough for the drive.

## 6. EXPECTED CONSOLE ROWS, PER LEG

Per ground-vehicle-move-to.lua (MESH_QUERY sec 2, line numbers verified there): :1461-1470
maybePlanOffroadNavPath runs isCurrentPointInNavArea (:1416) -> isDestInNavArea (:1422) -> the
ONE mesh job "Calc off road nav path part" (:486-530) for the WHOLE LEG, BEFORE the feature
planner. Its exitFn prints exactly one of three lines.

For each of the three legs, on 1.BdeHQ~PXY's own console at level 4, in this order:

    L3  ...Subtask N name and parameters: ground-vehicle-move-to: destination={x, y, z}
    L4  .            Node Is current point in nav area?: success
    L4  .            Node Is destination in nav area?: success
    then EXACTLY ONE of
    L3  Planned path has <N> points.                     <- MESH PLANNED the leg (:511)
    L2  Planned nav path has not enough (0) points.      <- mesh returned <= 1 point (:508)
    L2  Planned nav path is nil.                         <- query returned nil (:506)
    and, only on the two failure branches, the feature fallback:
    L3  Not using roads for move planning.
    L3  Planned path has <N> parts.                      <- FEATURE planner (:1398)
    L4  .            Starting condition node Is PathPart outside nav area?
    L2  Planned nav path has not enough (0) points.      <- the PER-PART query (call site B)

"parts" is never a mesh result. A per-part failure row can follow a "parts" row; the WHOLE-LEG
outcome is the one with no "parts" row between it and its goal row.

PREDICTIONS (registered before the run; a missed HIGH prediction is a stop):

- P17a (HIGH, the gate): both nav-area conditions report success on all three legs - all four
  vertices are inside MojaveCOA by >= 8.9 km. A MISS means the area did not load or the fixture
  is wrong; stop and check the terrain copy, do not interpret the mesh rows.
- P17b (THE MEASUREMENT, no prior stated): leg 1 prints either "Planned path has N points." or
  "not enough (0)". Both outcomes are informative and both are pre-registered in sec 0. Stating
  a prior here would be theatre - the record genuinely does not separate (A) from (B).
- P17c (MEDIUM): leg 3 (4,989 m) returns 0 points. G6's shortest measured failure was 9,346.5 m
  and its longest success 34.6 m, so 5 km sits inside the unmeasured gap; if leg 3 PLANS, the
  ceiling is between 5 and 9.3 km and sec 0's third branch fires.
- P17d (MEDIUM, movement): the vehicle reaches V3. The pre-flight scores every leg benign
  (worst ratio 0.205), so a stop on this route is a SECOND mechanism, not slope - record it
  against FINDING_EARLY_STOPS_2026-09-13 rather than explaining it away.

## 7. HARVEST RECIPE - WHICH STRINGS DECIDE EACH LEG

The run writes runs\<stamp>_run\watchvrf-trace.csv with CON rows shaped
`CON,<wall>,VRF_UUID:<uuid>,<level>,"<text>"`. There is one simulated object, so UUID filtering
is not strictly needed; take the UUID anyway from vrfc2simapp.log:

    grep -n "VRF console level 4 requested for 1.BdeHQ" runs\<stamp>_run\vrfc2simapp.log
    grep -n "CreateRoute 'T_G7_XSECT ROUTE'"           runs\<stamp>_run\vrfc2simapp.log   # expect (4 pts)
    grep -n "PLACEMENT: PLATFORM 1.BdeHQ"              runs\<stamp>_run\vrfc2simapp.log   # start lat/lon

Then, in trace order, the three goal rows ARE legs 1, 2, 3:

    grep -n "ground-vehicle-move-to: destination=" runs\<stamp>_run\watchvrf-trace.csv

Expected destinations (geocentric metres, computed at the sampled terrain height; the sim's own
z comes from its own terrain query, so match the leading 6 significant figures of x and y, not
the bytes):

    V1  {-2363264.8, -4694904.7, 3602871.8}   (terrain 1,150.0 m)
    V2  {-2365074.5, -4694075.3, 3602668.4}   (terrain 1,094.8 m)
    V3  {-2369649.7, -4692102.0, 3602239.4}   (terrain 1,097.0 m)

For each goal row take the NEXT of these on the same object and record which it is:

    grep -n "Planned path has .* points\."       # MESH SUCCESS - record N
    grep -n "Planned nav path has not enough"    # MESH FAILURE (<= 1 point)
    grep -n "Planned nav path is nil\."          # MESH FAILURE (nil)
    grep -n "Planned path has .* parts\."        # FEATURE fallback - NOT a mesh result

Gate rows, to prove the mesh branch was entered at all:

    grep -n "Is current point in nav area?"
    grep -n "Is destination in nav area?"
    grep -n "New Primary nav area: NavArea-ground-platform MojaveCOA"

Timing, for the fast-refusal reading of MESH_QUERY sec 7: per leg take the wall delta from the
"Is destination in nav area?" row to the outcome row, and compare against G6's failure band
(0.4-1.3 s) and MojaveAO20's 10 km successes (2.4-4.2 s).

Movement, for P17d: the R1 position reports (PositionReportSeconds 10) in
runs\<stamp>_run\reports-captured.log plus the POS rows of the trace; project onto each leg and
record the along-leg advance over the final 600 s, exactly as the pre-flight calibration does
(tools\preflight\README.md, "FROZE / MOVED / crawling").

Verdict table to fill in:

| leg | length m | seams | gate cur | gate dest | mesh outcome | N | gate->outcome s | reached the vertex? |
|---|---|---|---|---|---|---|---|---|
| 1 | 598.7 | 1 | | | | | | |
| 2 | 1,995.7 | 5 | | | | | | |
| 3 | 4,989.4 | 11 | | | | | | |

## 8. VALIDATION DONE ON THE ORDER (nothing was pushed)

- tools\PushOrder has NO --validate and NO --dry-run mode - its own source says so
  (tools\PushOrder\Program.cs:37: "PushOrder has NO --dry-run mode - it always performs a real
  push"). It was therefore NOT run. Nothing was sent to any broker.
- XML well-formedness: xml.etree.ElementTree.parse - OK.
- SCHEMA: validated with .NET XmlReader + XmlSchemaSet against
  Software\Library\CS\C2SIMSDK\C2SIMSDK\schemas\C2SIM_SMX_LOX_V1.0.1.xsd (the schema whose
  generated classes the repo uses - tools\preflight\README.md, "Outputs"): 0 validation
  messages. CONTROL: data\R9_Mojave_UnitMove_Order.xml, the order G1 actually pushed, validates
  against the same schema with 0 messages, so the validator reproduces.
- Route parse: tools\preflight\leg_check.py built the route from the order and the init and
  produced 3 legs of 599 / 1,996 / 4,989 m - the interface's own rule (live position prefixed,
  nothing dropped) reproduced independently of my own arithmetic.
- Encoding: ASCII only, LF line endings, no BOM, trailing newline - MATCHING
  data\R9_Mojave_UnitMove_Order.xml, which is LF, not CRLF (0 CR bytes; `file` reports "XML 1.0
  document, ASCII text"). The brief said CRLF; its operative clause is "the same encoding/line
  endings as the R9 order", so LF was taken. Deviation recorded, not silently made.
- Control-character sweep with scratchpad\g7\asciichk.py (`[^\x09\x0a\x0d\x20-\x7E]`, the
  corrected class): 0 offenders in the order, and the known-dirty control file (NBSP + VT + BEL)
  reports 4 - the checker reproduces. NOTE that `grep -P` FAILED on this machine ("-P supports
  only unibyte and UTF-8 locales"), which is exactly the broken-locale trap the standing rule
  warns about; the python checker is the instrument that works here.
- Task name length: "T_G7_XSECT ROUTE" is 16 characters, inside the 34-character DtUUID payload
  limit of PREREG_ROUTE_NAME_LENGTH_2026-09-02.

## 9. OPTIONAL PAIRED CONTROL - THE SAME ORDER ON MojaveAO20

The whole route also lies inside MojaveAO20 (lat 34.518-34.698, lon -116.809 to -116.591),
minimum margin 1.27 km at V3, and the sector topology there is nearly identical (40 x 40 tiles,
0.00545 x 0.00450 deg, so 501-502 m sectors as well):

    S (17,20) -> V1 (16,20): ONE seam | V1 -> V2 (12,19): 5 seams | V2 -> V3 (2,18): 11 seams

So the identical order, init and unit can be run on fixture R9_Mojave_Empty_52_Nav (the same
terrain copy family, carrying the MojaveAO20 record) as a CONTROL. If MojaveAO20 plans all three
legs and MojaveCOA fails only leg 3, the length/budget reading becomes a paired within-design
result instead of a cross-run comparison. This is the cheapest strengthening available and it
costs one short run.

HARD CONSTRAINT IF IT IS TAKEN: the two runs must NOT be two launcher-loaded fixtures in one
session. StopVrf + relaunch between fixtures wedges rtiForwarder and later observers go blind
(reflected = 0) - the teardown-relaunch wedge. Run the second fixture after a fresh boot.

## 10. RESIDUAL RISKS AND WHAT WOULD FALSIFY THE DESIGN

- The start's 11.9 m clearance to the lon-line at index 56 (sec 2). Mitigated by running leg 1
  WEST; re-derive the sector from the run's first POS fix before writing the result.
- The SW anchor of the tile index is an assumption (sec 1). It moves labels, never counts.
- Vehicle-type transfer: an M577A2 is not an M1A2, and MESH_QUERY sec 10 records an unexplained
  per-type asymmetry on the SMALL area. If leg 1 plans and that result is to speak for the M1A2
  freezes, the M1A2 arm has to be run.
- If the sim creates more than one entity (CreationPolicy not AtOrder), the one-query-per-leg
  pairing weakens to G6's inferred pairing. Check the creation count first.
- If BOTH nav-area gates do not report success, nothing about the mesh was measured (P17a).
- The pre-flight is an estimate off terrain tiles, never a vendor verdict: it says this route
  carries no sustained face, not that the vehicle will arrive.
- 2026-09-13's other lesson applies to the run itself - do not run agents concurrently with it
  (G2's "engine collapse" was concurrent agent load, withdrawn in G3), and compare anything at
  equal SIM time, never equal wall time.
