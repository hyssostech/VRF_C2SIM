# G7 probe design v3 (Opus executor, 2026-09-14 ~09:00 local; lane L1, Jira STP-790). Supervisor notes: adopted for
# attempt 3 (run 20260914T130439Z) - performer 1222.MechPlt, route V1/V2/V3 with 1/4/10 seams; attempt 3 was VOID
# because the sectorised area loads lazily ~175 s after the members are placed (PREREG_MESHQUERY_G7 sec 5), so attempt 4
# reuses this design unchanged with CreationPolicy=AtInit and a pre-order settle >= 240 s (runner -PreOrderSettleSecs).

# G7 v3 - CROSS-SECTOR PROBE ON MojaveCOA, RE-DERIVED FOR AN AGGREGATE PERFORMER

Authored 2026-09-14 by an Opus executor. DESIGN ONLY - nothing launched, no order pushed, no
file under C:\MAK written (read-only reads of the vendor SMS and the type maps only), no git
commit. Deliverables: data\PROBE_G7_CrossSector_Order.xml (overwritten) and this file.
Scripts: scratchpad\g7\{geom3.py,geom3b.py,geom3c.py,scan3.py,scan3b.py,final3.py,ecef3.py,
veh3*.py,coloc.py,ao20.py,g1dest.py,g1off.py,writeorder3.py,asciichk.py} plus the inherited
frame.py / final.py / ecef.py.

## 0. WHY v3 EXISTS, AND THE ONE FACT THAT DRIVES EVERY CHANGE

v1 died on a blank line inside a multi-line XML comment (SDK STOMP pump). v2 ran (run
20260914T122525Z) but measured nothing: the performer 1.BdeHQ is a lone PLATFORM, and a
platform's Move-Along runs the NATIVE move-along controller, which never enters
ground-vehicle-move-to.lua. It printed 0 planner rows and drove the whole 7.58 km at ~9.9 m/s.

THE MECHANISM, confirmed in the interface's own source: src\VrfC2SimApp\VrfC2SimService.cs:1755
opens member consoles only `if (_vrf.ObjectConsoleMemberNotifyLevel >= 0 && unit.IsAggregate)`.
1.BdeHQ resolves to M577A2_Command_Post with isAggregate false - there are no members to open
and no member to query the mesh. Only MEMBERS of an aggregate, tasked through
maneuver-in-formation, run ground-vehicle-move-to.lua and hit the nav mesh.

So v3 swaps the performer to 1222.MechPlt, an aggregate, and everything else follows.

## 1. THE QUESTION AND THE DISCRIMINATOR (unchanged from v1 sec 0)

Two live readings of G6 survive (scratchpad\meshq\MESH_QUERY_VS_DISTANCE.md sec 0/5/10):

  (A) the planner REFUSES or budget-cuts the big graph (failures flat and fast, 0.4-1.3 s);
  (B) MojaveCOA's DATA is not connected across sector boundaries, so any query that must leave
      its start sector returns nothing.

  leg 1 plans AND leg 3 returns 0 -> connected; failure is length/budget. (B) falsified.
  leg 1 returns 0                 -> the data is unusable; G6's "area size" reading WITHDRAWN.
  all three plan                  -> the ceiling lies between 5 and 9.3 km.

v3 ADDS A ZERO-SEAM CONTROL FOR FREE. Every member's FIRST ground-vehicle-move-to is the move
into its formation slot - ~50-90 m, entirely inside the start sector, zero seams. G1 shows it is
mesh-planned like any other leg ("Planned path has 14 points."). So the run now carries, on the
same vehicles, in the same area, within the same 30 sim seconds:

    slot move   ~87 m, 0 seams   <- if this returns 0 too, nothing about SEAMS was measured
    leg 1        599 m, 1 seam   <- THE MEASUREMENT
    leg 2      1,996 m, 4 seams
    leg 3      4,989 m, 10 seams

That 0-seam / 1-seam pair inside one unit is a within-run control the v1 design did not have.

## 2. THE GRID (v1 sec 1 stands, unchanged)

d_lon = 0.00547222 deg, d_lat = 0.00452439 deg from the SW corner 34.318 / -117.019; 108 x 82
tiles; 501.9 m per sector at the working latitude. sector i = floor((lon+117.019)/d_lon),
j = floor((lat-34.318)/d_lat). The SW anchor is an assumption that moves LABELS, never COUNTS.

## 3. THE PERFORMER

1222.MechPlt, UUID 001aa71b-4c26-a1ea-28b2-f7dfe8e76342, authored
34.612955587412 / -116.600486942341 (data\R9_Mojave_Lean_Initialization.xml:277-278, Name at
:286, UUID at :296).

Type-map resolution, reproduced by tools\preflight\leg_check.py (rule b:functionId+sidcEchelon,
row F-UCIZ-D - the same rule and row G1's own log records at line 39):

  data\unit-type-map-52.json          -> "Mechanized Platoon (USA) IFV (Deprecated)", 16 members
  data\unit-type-map-52-nolifeform.json -> "Tank Platoon (USA)", 4 members

*** THE TWO MAPS ARE NOT INTERCHANGEABLE FOR THIS UNIT. *** v1 sec 5 said row F-UCIZ-H was
byte-identical between them; that is true for 1.BdeHQ and FALSE for the platoon echelon. The
brief's premise (16 members, mech platoon, the G1 mover) selects data\unit-type-map-52.json -
the repo map, which is what G1 used. Run 20260914T122525Z used a scratchpad copy of the
nolifeform map; carrying that forward would silently make the performer a TANK platoon.

Members the order will create (G1 line 137, verbatim labels):
  4 x "M2 1".."M2 4"  = M2A2 Bradley IFV   <- the ONLY mesh clients
  6 x "R 1".."R 6"    + 6 x "AT4 1".."AT4 6" = dismounts, controller base-system.movement.
                        human-move-along; they print NO ground-vehicle-move-to rows
  Total 17 simulated objects (1 aggregate shell + 16 members).

Vehicle limit (vendor .entity resolution, VendorSms.min_max_slope over the 16 leaves):
M2A2 Bradley IFV max-slope 0.94 x4, the 12 dismount leaves resolving to
ground-vehicle-parameters 1.0 x12 -> MIN LIMIT USED = 0.94, identical to the M1A2. On sand
(factor 0.80) the derated limit is 0.752 on every leg. No lifeform substitution fires
(LIFEFORM_SLOPE is 1.2 and no leaf reaches it).

CO-LOCATION (the brief asked): 1222.MechPlt is NOT co-located with anything. Nearest other
authored unit is 9,333.5 m away (114.MechCoy and its three platoons, which ARE co-located with
each other at 0.0 m); 1.BdeHQ is 10,280.4 m away. DeStackCreates/DeStackSpacingMeters=700
therefore has nothing to act on, and under CreationPolicy=AtOrder none of the co-located four is
created at all - the order names only 1222.MechPlt, and 1222.MechPlt declares no <Subordinate>.

DI-GUY RISK, MEASURED NOT ASSUMED. The DI-Guy data package is still absent (C:\MAK has no
DI-Guy directory). The standing note is that the headless sim crashes at the first DI-Guy human.
G1 (runs\20260907T170643Z_run, VR-Forces 5.2d, this same init, this same type map) created
exactly these 12 dismounts, ran them for 712 sim seconds under human-move-along, and exited 0
with no crash row in the log. That is direct evidence for THIS template on THIS build; it is not
a claim about the fidelity-table lifeform rows that crashed runs 2/L1/L2/S3.

## 4. THE ROUTE

Leg 1 bearing 287.0 deg, legs 2-3 bearing 271.0 deg (a 16 deg turn at V1). Chosen as the
minimum-max-ratio point of a two-stage scan (geom3c.py geometry filter, 5,079 survivors; scan3.py
9 x 11 preflight grid; scan3b.py refinement at 0.5 deg over b1 285-289 / g 268-274, 117 rows).

| point | lat | lon | sector | seam clearance W/E/S/N m | area edge N/S/E/W km |
|---|---|---|---|---|---|
| S' | 34.612955587412 | -116.600486942341 | (76,65) | 241 / 261 / 97 / 405 | 8.44 / 32.72 / 15.82 / 38.38 |
| V1 | 34.614536922 | -116.606743032 | (75,65) | 169 / 333 / 272 / 230 | 8.26 / 32.90 / 16.39 / 37.81 |
| V2 | 34.614851569 | -116.628546593 | (71,65) | 177 / 325 / 307 / 195 | 8.23 / 32.93 / 18.39 / 35.81 |
| V3 | 34.615638184 | -116.683055704 | (61,65) | 196 / 306 / 394 / 108 | 8.14 / 33.02 / 23.39 / 30.81 |

Minimum distance from any vertex to an area edge: 8.14 km, far above the 1.5 km required.
The interface prefixes the unit's live position, so 3 authored vertices give a 4-point route.

| leg | from -> to | length m (leg_check great-circle) | sector step | seams crossed (m along leg) | total |
|---|---|---|---|---|---|
| 1 | S' -> V1 | 598.9 | (76,65)->(75,65) | lon-line 76 at 252 | **1** |
| 2 | V1 -> V2 | 1,995.6 | (75,65)->(71,65) | lon 75 @169, 74 @671, 73 @1,173, 72 @1,675 | 4 |
| 3 | V2 -> V3 | 4,989.0 | (71,65)->(61,65) | lon 71 @177, 70 @679, 69 @1,181, 68 @1,682, 67 @2,184, 66 @2,686, 65 @3,188, 64 @3,690, 63 @4,192, 62 @4,694 | 10 |

Total route 7,583.5 m. No lat-line is crossed anywhere on the route: the whole corridor stays in
row j = 65. LEG 1 IS THE MEASUREMENT: the seam sits 252 m from S' and V1 lies 348 m past it.

### 4a. THE FORMATION BOX CHECK - AND A CORRECTION TO THE BRIEF'S 60 m

The brief assumed members start within ~60 m of S' and take slots within ~60 m of V1. G1's own
console rows for THIS unit falsify the number. Converting the four Bradleys' logged
ground-vehicle-move-to destinations back to lat/lon (g1off.py):

    M2 1  slot 86.1 m from S   V1-slot 13.8 m   final 86.1 m
    M2 2  slot 50.3 m          V1-slot 36.2 m   final 50.3 m
    M2 3  slot 72.8 m          V1-slot 63.8 m   final 72.7 m
    M2 4  slot 87.4 m          V1-slot 86.1 m   final 87.4 m

MEASURED ENVELOPE: up to 87.4 m, not 60 m; cross-track spread -63.8 m to +86.1 m about the route
line. The design is therefore verified at BOTH radii - the brief's 120 m box and a 180 m box
that actually covers the measurement:

| test | box half-width | required seam clearance | result |
|---|---|---|---|
| the brief's | 60 m (120 m box) | 150 m each side | PASS - seam x=76, 251.7 m in, 348.3 m past |
| G1-measured | 90 m (180 m box) | 200 m each side | PASS - seam x=76, 251.7 m in, 348.3 m past |

All 16 corner-to-corner lines (4 corners of the S' box x 4 corners of the V1 box) cross exactly
one seam, and it is the same seam, at both radii. The u-coordinate envelope over all 8 corners:

    half 60 m: u_lat 65.0728 .. 65.6614 (inside 65..66)  u_lon 75.2167 .. 76.5991 (straddles 76 only)
    half 90 m: u_lat 65.0130 .. 65.7212 (inside 65..66)  u_lon 75.1570 .. 76.6589 (straddles 76 only)

RESIDUAL, STATED PLAINLY. S' sits only 96.5 m north of the lat-line at j = 65. At the 90 m
radius the southernmost corner clears that line by 6.5 m. If the sim places a member more than
96.5 m south of S', that member's leg 1 crosses TWO seams, not one. The observed southern
extent in G1 was 63.8 m (33 m of margin), but formation slots are body-frame and G1's heading was
090 while ours is 287 - the lateral signs can flip. This is why leg 1 runs NORTH of west
(bearing 287): V1 is 175 m north of S', so the corridor climbs away from that line and V1 itself
sits 272 m clear of it. Verify per member from the run (sec 8), do not assume.

## 5. PRE-FLIGHT (tools\preflight\leg_check.py, the committed tool, real invocation)

    set PREFLIGHT_CACHE=<scratchpad>\g7\preflight_cache
    python tools\preflight\leg_check.py ^
      --order data\PROBE_G7_CrossSector_Order.xml ^
      --init  data\R9_Mojave_Lean_Initialization.xml ^
      --typemap data\unit-type-map-52.json ^
      --no-starts --text --verbose --json <out>.json

`--no-starts` because starts_P11.csv holds COA-STP1 units only; R9's units are dispersed, so the
authored position IS the start (the tool reports start_source "authored position
(initialization)"). The cache was pointed at the scratchpad copy - nothing written into the repo,
0 tiles fetched for the final route.

Result - 0 legs flagged of 3, 0 skipped, 0 without a verdict, 0 dropped vertices:

| leg | length m | sustained 40 m | short 20 m | soil | limit | **ratio** | climb / descend m | NaN |
|---|---|---|---|---|---|---|---|---|
| 1 | 598.9 | 0.0649 | 0.0656 | sand (BM_SAND, CA FVEG 15 m) | 0.752 | **0.086** | 19 / 1 | 0.000 |
| 2 | 1,995.6 | 0.1249 | 0.1695 | sand (BM_SAND, CA FVEG 15 m) | 0.752 | **0.166** | 20 / 45 | 0.000 |
| 3 | 4,989.0 | 0.1173 | 0.1481 | sand (BM_SAND, CA FVEG 15 m) | 0.752 | **0.156** | 176 / 18 | 0.000 |

MIN LIMIT USED: max-slope 0.94 (M2A2 Bradley IFV, the minimum over the platoon's 16 leaves) x
sand 0.80 = 0.752 on all three legs. Worst 40 m windows: leg 1 at 34.613483 / -116.602572 (200 m
along, z 1,122.1 m); leg 2 at 34.614721 / -116.619476 (1,165 m along, z 1,107.2 m); leg 3 at
34.615579 / -116.678950 (4,613 m along, z 1,271.2 m).

The brief's gate was ratio < 0.6 of the limit on every leg. The worst leg is 0.166 - 3.6x inside
that gate, 5.5x inside the tool's own 0.92 flag threshold, and below every clean mover in its
calibration table (0.438-0.870). A stop on this route is NOT a slope stop. No bearing iteration
beyond the scan was needed: the first minimum-max-ratio candidate already passed.

ELEVATION SOURCE CHECKED AT THIS EXACT POINT: the pre-flight reads 1,117.2 m under S'. G1's own
placement line for the same lat/lon reads "terrain height under the create point: 1117.2 m"
(runs\20260907T170643Z_run\vrfc2simapp.log:75). Tool and sim terrain query agree to 0.1 m.

Caveats from the tool's README, not re-argued: the soil hop is ASSUMED except sand (every window
here IS sand); the limit is max-slope x acceleration-factor, a path-cost formula used as an
analogy; legs are sampled as straight lines.

## 6. WHAT TO RUN

    INIT      data\R9_Mojave_Lean_Initialization.xml
    ORDER     data\PROBE_G7_CrossSector_Order.xml
    FIXTURE   R9_Mojave_Empty_52_NavAO        (the MojaveCOA terrain copy; v1 sec 5 verified it
              from the .scnx, and runs 20260914T120444Z / 122525Z used it. Do NOT pick up the
              sibling R9_Mojave_Empty_52_NavAO_AG.scnx.)
    TYPE MAP  data\unit-type-map-52.json      *** THE REPO MAP, NOT THE NOLIFEFORM ONE ***

    export Vrf__TypeMappingMode=FidelityTable Vrf__CreationPolicy=AtOrder \
           Vrf__DeStackCreates=true Vrf__DeStackSpacingMeters=700 Vrf__DeStackRotationDeg=0 \
           Vrf__DropOriginVertexMeters=100 Vrf__TaskPredecessorTimeoutSeconds=7200 \
           Vrf__ObjectConsoleNotifyLevel=4 Vrf__ObjectConsoleMemberNotifyLevel=4 \
           Vrf__PositionReportSeconds=10

    pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\RunC2SimScenario.ps1 \
         -VrfProfile 5.2 -NoGui -Scenario R9_Mojave_Empty_52_NavAO \
         -Init data\R9_Mojave_Lean_Initialization.xml \
         -Order data\PROBE_G7_CrossSector_Order.xml -ClientId STP \
         -TypeMapFile data\unit-type-map-52.json \
         -RunSecs 1200 -WatchSecs 1500 -BackendNotifyLevel 3 -StopWhenComplete

IGNORE-ROADS IS KEPT: it is not an env switch, it is what the aggregate's maneuver-along
controller already does - G1 line 333, "Setting navigation preference to ignore-roads", on this
same unit. The confound is avoided by changing nothing.

MEMBER CONSOLE LEVEL 4 IS REQUIRED, not cosmetic. At member level 3 (G1) the gate rows print
only "Starting condition node Is destination in nav area?" with NO verdict; at level 4 they print
"Node Is destination in nav area?: success" (253 such pairs in runs\20260914T002716Z_run). Level
3 is enough for the goal row and the outcome row; level 4 is what makes P1 checkable.

DropOriginVertexMeters stays 100 m: the nearest authored vertex is 599 m from the authored
position, so none is dropped and all three legs are driven.

## 7. RUN LENGTH - THE BRIEF'S 10 m/s IS THE WRONG VEHICLE

The brief cites "~10 m/s in formation; G1: 1,163 m in ~118 s". That 118 s is T_R5_TK1, the
1.BdeHQ PLATFORM (sim 55.266 -> 173.732). 1222.MechPlt's own G1 numbers are different and are
what matters here:

    the 4 Bradleys   maneuver-in-formation Completed at sim 232.86-232.93, from dispatch 55.4
                     -> ~177 sim s for a ~1,243 m member route; per-leg 494 m in 85.7 s (5.8 m/s)
                     and 664 m in 67.9 s (9.8 m/s). Working figure: 8 m/s.
    the 12 dismounts human-move-along Completed at sim 711.5-712.0 -> 1.76 m/s. THEY gate the
                     unit: the aggregate's move-along controller reported Completed at sim
                     712.093, 479 s after the last Bradley arrived.

Projected timeline for this route (dispatch at sim ~55, as in G1):

    sim  55   task dispatched, members begin move-into-formation
    sim  56   SLOT goal row + gates + outcome        <- the 0-seam control, ~1 s to the outcome
    sim  77   slot reached (~87 m)
    sim  79   LEG 1 goal row + gates + outcome       <- THE MEASUREMENT, ~24 s after dispatch
    sim 155   V1 reached (599 m at 8 m/s)
    sim 157   LEG 2 goal row + outcome
    sim 407   V2 reached (1,996 m)
    sim 409   LEG 3 goal row + outcome
    sim 1,033 V3 reached by the Bradleys (4,989 m)
    sim 4,364 the dismounts would arrive; the UNIT task completes only then

CONSEQUENCE: -StopWhenComplete WILL NOT FIRE inside any sane window. Budget the full wall cap.
G1's observation window ran sim 712 in 637 wall s on 64+ objects, a ratio of ~1.12x; this run has
17 objects, so expect >= 1.12x. 1,100 sim s of Bradley drive is then <= ~1,000 wall s, which is
why -RunSecs is raised to 1200 (a WALL cap, RunC2SimScenario.ps1:158-170) and -WatchSecs to 1500.
Every mesh outcome is in by sim ~410, so even a truncated run answers the primary question.

IF THE SUPERVISOR WANTS THE UNIT TO COMPLETE, the switch is one flag and no order change:
-TypeMapFile data\unit-type-map-52-nolifeform.json makes the performer "Tank Platoon (USA)" -
4 x M1A2, no dismounts, same limit 0.94, same three ratios, unit Completed at ~sim 1,035, and the
vehicle becomes the M1A2 of the G2-G6 flip set (a CLOSER transfer to the freeze question). The
cost is that it is no longer the unit G1 measured, and there is no prior that these members plan
on MojaveAO20. Registered as a supervisor decision, not taken here.

## 8. EXPECTED CONSOLE ROWS, PER MEMBER

Per ground-vehicle-move-to.lua (MESH_QUERY sec 2): :1461-1470 maybePlanOffroadNavPath runs
isCurrentPointInNavArea (:1416) -> isDestInNavArea (:1422) -> ONE mesh job "Calc off road nav
path part" (:486-530) for the WHOLE LEG, before the feature planner; its exitFn prints exactly
one of three lines.

PER BRADLEY (M2 1..M2 4), FOUR goal rows in this order - slot, leg 1, leg 2, leg 3:

    L3  ...Subtask N name and parameters: ground-vehicle-move-to: destination={x, y, z}
    L4  .            Node Is current point in nav area?: success
    L4  .            Node Is destination in nav area?: success
    then EXACTLY ONE of
    L3  Planned path has <N> points.                 <- MESH PLANNED the leg (:511)
    L2  Planned nav path has not enough (0) points.  <- mesh returned <= 1 point (:508)
    L2  Planned nav path is nil.                     <- query returned nil (:506)
    and, only on the failure branches, the feature fallback:
    L3  Not using roads for move planning.
    L3  Planned path has <N> parts.                  <- FEATURE planner (:1398), never a mesh result
    L4  .            Starting condition node Is PathPart outside nav area?
    L2  Planned nav path has not enough (0) points.  <- the PER-PART query (call site B)

G1's actual sequence for M2 1, which is the template to match (log lines 847 / 10467-10471 /
19963, then 75807 / 78987-78997 / 81365, then 212285 / 212527-212531 / 212539 / 212871):

    Subtask 1  slot  86 m  -> Planned path has 14 points.
    Subtask 6  V1   494 m  -> Planned path has 51 points.
    Subtask 8  V2   664 m  -> "Not using roads..." + "Planned path has 1 parts."

The third one is NOT a mesh failure: that goal lay OUTSIDE MojaveAO20's east edge, so the
destination gate failed and no mesh row was printed at all. WATCH FOR THAT SHAPE - when the gate
fails, the sim prints NO mesh outcome row, it jumps straight to the feature planner. On our
route every goal is >= 8.1 km inside MojaveCOA, so it must not recur; if it does, the area did
not load.

Totals to expect: 16 goal rows (4 Bradleys x 4), 32 gate rows, 16 outcome rows. The 12 dismounts
print human-move-along rows and NO ground-vehicle-move-to row - do not look for them there.

PREDICTIONS (registered before the run; a missed HIGH prediction is a stop):

- P1 (HIGH, the gate): both nav-area conditions report success on all 16 member goals. Every
  goal is >= 8.1 km inside the area even after a 90 m formation offset. A MISS means the area
  did not load or the fixture is wrong; stop, do not interpret the mesh rows.
- P2 (HIGH, the zero-seam control): the SLOT move (~87 m, 0 seams) plans - "Planned path has N
  points." - on all four Bradleys. If the 87 m intra-sector move returns 0, the run measured
  nothing about seams and the whole reading is void.
- P3 (THE MEASUREMENT, no prior stated): leg 1 (599 m, 1 seam) prints either "Planned path has N
  points." or "not enough (0)". Both outcomes are informative and both are pre-registered in
  sec 1. Stating a prior would be theatre - the record genuinely does not separate (A) from (B).
- P4 (MEDIUM): leg 3 (4,989 m, 10 seams) returns 0 points. G6's shortest measured failure was
  9,346.5 m and its longest success 34.6 m, so 5 km sits inside the unmeasured gap.
- P5 (MEDIUM, movement): the four Bradleys reach V3 by sim ~1,035. The pre-flight scores every
  leg benign (worst 0.166), so a stop is a SECOND mechanism, not slope - record it against
  FINDING_EARLY_STOPS_2026-09-13 rather than explaining it away.
- P6 (HIGH, bookkeeping): 17 objects created, 16 of them members of 1222.MechPlt. More means
  CreationPolicy was not AtOrder and the per-member pairing weakens.

## 9. HARVEST RECIPE FOR 16 MEMBERS

Identify the objects first - the app log enumerates every member with its VRF UUID on one line:

    grep -n "VRF console level 4 requested for 16 members of 1222.MechPlt" runs\<stamp>\vrfc2simapp.log
    grep -n "VRF console level 4 requested for 1222.MechPlt"               runs\<stamp>\vrfc2simapp.log
    grep -n "CreateRoute 'T_G7_XSECT ROUTE'"                               runs\<stamp>\vrfc2simapp.log   # expect (4 pts)
    grep -n "PLACEMENT: UNIT 1222.MechPlt"                                 runs\<stamp>\vrfc2simapp.log   # start lat/lon + terrain (expect 1117.2 m)
    grep -n "TYPE MAP .*1222.MechPlt"                                      runs\<stamp>\vrfc2simapp.log   # expect Mechanized Platoon (USA) IFV (Deprecated)

Split that member line into 16 (label, uuid) pairs. The FOUR with label "M2 n" are the mesh
clients; the "R n" / "AT4 n" twelve are expected to produce no planner rows at all (assert it).

THEN, PER BRADLEY UUID, in trace order (runs\<stamp>\watchvrf-trace.csv, CON rows shaped
`CON,<wall>,VRF_UUID:<uuid>,<level>,"<text>"`; the same text is in vrfc2simapp.log):

    grep "<uuid>" ... | grep -n "ground-vehicle-move-to: destination="     # expect 4, in order
    grep "<uuid>" ... | grep -nE "Planned path has [0-9]+ points\.|Planned nav path has not enough|Planned nav path is nil\.|Planned path has [0-9]+ parts\."

Pair by POSITION, not by value: goal row k is followed by exactly one outcome row before goal
row k+1. k = 1 is the SLOT (0 seams), k = 2,3,4 are legs 1,2,3. A "parts" row is never a mesh
result; if a "parts" row appears with no mesh row before it, the DESTINATION GATE failed - read
the two gate rows for that k and record the gate verdict, not a mesh verdict.

PAIR EACH OUTCOME TO ITS OWN GOAL, AND RE-DERIVE THAT MEMBER'S OWN SEAM COUNT. Convert each
logged destination {x,y,z} back to lat/lon (scratchpad\g7\frame.py ecef2geo, validated against
G1 in g1dest.py) and compute i = floor((lon+117.019)/0.00547222),
j = floor((lat-34.318)/0.00452439) for the goal and for the member's position at the goal row.
The seam count for that member's own leg 1 is |di| + |dj|. EXPECT 1; the design guarantees it
only for members within 96.5 m of S' in latitude (sec 4a). Any member showing 2 is reported
separately, not averaged in.

Anchor values for the AUTHORED vertices (geocentric metres at the sampled terrain height; member
goals are formation slots offset from these by 14-90 m, so match the leading 4 significant
figures of x and y, never the bytes):

    S'  {-2353396.9, -4699525.2, 3603246.2}   terrain 1,117.2 m
    V1  {-2353872.1, -4699192.6, 3603400.9}   terrain 1,135.4 m
    V2  {-2355642.1, -4698260.4, 3603415.5}   terrain 1,110.5 m
    V3  {-2360146.8, -4696088.9, 3603576.9}   terrain 1,268.2 m

Gate rows, to prove the mesh branch was entered:

    grep -n "Node Is current point in nav area?: "
    grep -n "Node Is destination in nav area?: "
    grep -n "New Primary nav area: NavArea-ground-platform MojaveCOA"

Timing, for the fast-refusal reading of MESH_QUERY sec 7: per goal take the wall delta from the
"Is destination in nav area?" row to the outcome row and compare against G6's failure band
(0.4-1.3 s), MojaveAO20's 10 km successes (2.4-4.2 s) and G1's own 1.0 sim s for a 494 m success.

Movement, for P5: the R1 position reports (PositionReportSeconds 10) in
runs\<stamp>\reports-captured.log plus the POS rows of the trace, projected onto each leg;
compare at EQUAL SIM TIME only, never equal wall time.

Verdict table to fill in - one row per Bradley per goal, 16 rows:

| member | k | goal | length m | seams | gate cur | gate dest | mesh outcome | N | gate->outcome s | reached? |
|---|---|---|---|---|---|---|---|---|---|---|
| M2 1 | 1 | slot | ~87 | 0 | | | | | | |
| M2 1 | 2 | V1 | ~599 | 1 | | | | | | |
| M2 1 | 3 | V2 | ~1,996 | 4 | | | | | | |
| M2 1 | 4 | V3 | ~4,989 | 10 | | | | | | |
| ... M2 2, M2 3, M2 4 identically ... |

## 10. VALIDATION DONE ON THE ORDER (nothing was pushed)

- tools\PushOrder has NO --validate and NO --dry-run (Program.cs:37). NOT run. Nothing sent.
- Well-formedness: xml.etree.ElementTree.parse - OK.
- SCHEMA: .NET XmlReader + XmlSchemaSet against
  Software\Library\CS\C2SIMSDK\C2SIMSDK\schemas\C2SIM_SMX_LOX_V1.0.1.xsd - 0 messages.
  POSITIVE CONTROL: data\R9_Mojave_UnitMove_Order.xml (the order G1 actually pushed) - 0 messages.
  NEGATIVE CONTROL: the same order with one <BogusElement> injected - 1 Error, naming the element.
  The validator therefore fires; a green is not a false green.
- Route parse: tools\preflight\leg_check.py rebuilt the route from the order + init and produced
  3 legs of 599 / 1,996 / 4,989 m with 0 dropped vertices - the interface's own rule reproduced
  independently of my arithmetic. (Its great-circle metric is 0.2% shorter than the
  metres-per-degree formula used in the seam derivation; the seam distances shift by <= 0.5 m.)
- Bytes: 2,998; 48 lines; ASCII only; LF endings, 0 CR; trailing newline; **0 occurrences of
  "\n\n"** (asserted at write time and re-checked on the file). One single-line leading comment,
  no multi-line comment anywhere - the v1 STOMP-pump failure cannot recur.
- Control-character sweep with scratchpad\g7\asciichk.py (`[^\x09\x0a\x0d\x20-\x7E]`): 0
  offenders in the order; the known-dirty control file reports 4. The checker reproduces.
  (`grep -P` still fails on this machine - "-P supports only unibyte and UTF-8 locales".)
- Task name "T_G7_XSECT ROUTE" is 16 characters, inside the 34-character DtUUID payload limit.

## 11. OPTIONAL PAIRED CONTROL - THE SAME ORDER ON MojaveAO20

The whole route also lies inside MojaveAO20 (lat 34.518-34.698, lon -116.809 to -116.591) and the
sector topology there is IDENTICAL leg for leg (40 x 40 tiles, 0.00545 x 0.00450 deg):

    S' (38,21) -> V1 (37,21): 1 seam | V1 -> V2 (33,21): 4 seams | V2 -> V3 (23,21): 10 seams

So the identical order, init, unit and type map can be run on fixture R9_Mojave_Empty_52_Nav as a
paired control. If MojaveAO20 plans all four goals and MojaveCOA fails from leg 1, (B) is
established within one design instead of across runs. Caveats: S' is only 870 m inside AO20's
EAST edge (the route runs west, away from it, so every leg is fine, but the ~90 m formation
envelope leaves 780 m); and S' is only ~51 m north of AO20's own lat-line 21, so the southern
members' leg 1 may cross 2 seams there - re-derive per member (sec 9).

HARD CONSTRAINT IF TAKEN: the two runs must NOT be two launcher-loaded fixtures in one session.
StopVrf + relaunch between fixtures wedges rtiForwarder and later observers go blind. Fresh boot
for the second fixture.

## 12. RESIDUAL RISKS AND WHAT WOULD FALSIFY THE DESIGN

- S' is 96.5 m from the lat-line at j = 65 and the measured formation envelope is 87.4 m. A
  member placed further south than 96.5 m crosses two seams on leg 1. Mitigated by the 287 deg
  bearing; verified per member from the log, never assumed (sec 4a, sec 9).
- TYPE MAP: running the nolifeform map by reflex (as runs 120444Z / 122525Z did) silently turns
  the performer into a 4-tank platoon. The map is a first-class input here, not parity dressing.
- DI-GUY: the data package is still absent. G1 is the evidence that this template's 12 dismounts
  do not crash 5.2; it is one run, and it is the only one. If the sim dies at creation, switch to
  the nolifeform map and re-run - the order needs no change.
- The unit-level task will NOT complete within the window (the dismounts walk at 1.76 m/s), so
  -StopWhenComplete is inert and any "did not complete" row is expected, not a finding.
- The SW anchor of the tile index is an assumption. It moves labels, never counts.
- The pre-flight is an estimate off terrain tiles, never a vendor verdict: it says this route
  carries no sustained face, not that the vehicle will arrive.
- Vehicle transfer: the mesh client here is an M2A2 Bradley, and MESH_QUERY sec 10 records an
  unexplained per-vehicle-type asymmetry on the small area. If the result is to speak for the
  M1A2 freezes, run the nolifeform arm too.
- Do NOT run agents concurrently with the timed run (G2's "engine collapse" was concurrent agent
  load, withdrawn in G3), and compare anything at equal SIM time.
