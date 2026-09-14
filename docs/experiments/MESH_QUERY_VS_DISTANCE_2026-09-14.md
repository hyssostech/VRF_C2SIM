# Mesh query vs goal distance across G1/G2/G3/G6 (Opus analysis executor, 2026-09-14 ~11:50Z). Supervisor
# verdict: the refusal is AREA-dependent (1,600 vs 8,856 sectors), not distance-dependent - the same
# destination bytes got 1,104 / 1,090 mesh points on MojaveAO20 (G2/G3) and 0 on MojaveCOA (G6). Two live
# explanations (refusal/budget vs disconnected data); the cross-sector probe of sec 10 is G7.
# FRAME CORRECTION accepted: G1 ran on MojaveAO20 (20x20 km), not on the 3x3 km timing-test area.

# MESH QUERY vs GOAL DISTANCE - does vrf:findPathToLocation ever plan a kilometre?

Read-only analysis, 2026-09-14, by an Opus analysis executor.  Four of our own WatchVrf
captures (G1, G2, G3, G6).  Nothing was launched, nothing in the repo was changed; every
figure below comes from the runs' own watchvrf-trace.csv plus the vendor Lua source at
C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\scripts\ground-vehicle-move-to.lua.
Scripts: scratchpad\meshq\{meshq_harvest,meshq_pair,meshq_all,gates,crossgoal}.py ;
G6 reuses scratchpad\g6\cache_G6.pkl and g6_harvest.py's decoder rather than re-reading 1.0 GB.

## 0. ANSWER

YES.  The nav-mesh planner planned a 10.13 km goal, twice, in two different runs, returning
1,104 and 1,090 path points - both on the 20 x 20 km / 1,600-sector area "MojaveAO20".  On
the 41.3 x 54.2 km / 8,856-sector area "MojaveCOA" it never produced a path longer than
34.6 m, and returned zero points for every one of 86 long goals (9.3 - 20.1 km).

The dependence is NOT on goal distance.  Inside MojaveAO20 the query succeeded at every
distance it was ever asked for - 0.3 m to 10,131.6 m - at a 98 % rate (92 of 94 whole-leg
queries in G2, 19 of 21 in G3).  The two exceptions were HMMWVs at 9.48 / 9.68 km, in runs
where M1A2s of the same company got paths at 9.75 - 10.13 km in the same wall second.

The dependence that the record does show is on the AREA.  The decisive control is in sec 5:
the same vehicle (C/1-35's M1A2 50) with the byte-identical destination
{-2357445.694748, -4698802.325654, 3601576.479853} got "Planned path has 1104 points." on
MojaveAO20 (G2), "Planned path has 1090 points." on MojaveAO20 again (G3), and "Planned nav
path has not enough (0) points." on MojaveCOA (G6) - with both nav-area gates returning
success in all three.  22 member+destination pairs flip that way from G2 to G6, 15 from G3,
and none flip the other way.

A second, independent signal points the same way (sec 7): on MojaveCOA the failing query
returns in 0.4 - 1.3 s from the gate, i.e. in the SAME time as the 12 - 35 m successes in
that run and FASTER than the 10 km successes on MojaveAO20 (2.4 - 4.2 s).  A 20 km search
that exhausts an 8,856-sector graph does not finish faster than a 23 m lookup.  This reads
as an early refusal, not an exhausted search.

## 1. FRAME CORRECTION - G1 IS NOT A 3 x 3 km AREA (this changes the question)

The brief, and docs\experiments\PREREG_NAVDATA_G6_2026-09-13.md:127, describe G1 as
"(R9, 3x3 km area)".  That is wrong.  G1's own trace names the area 81 times and every one
of them is "NavArea-ground-platform MojaveAO20" - the SAME 20 x 20 km / 1,600-sector area
G2 and G3 used (grep -o on watchvrf-trace.csv; "MojaveAssembly" appears 0 times).
PREREG_NAVDATA_2026-09-07.md:36-39 declares that area and its G1 RESULTS section (:115-137)
records the G1 run against it.  The 3 x 3 km area was a GENERATION TIMING test (47 s,
same doc :55); it was never loaded by a run.

So G1 vs G2/G3 is not small-area vs large-area.  It is SHORT ORDER (R9) vs LONG ORDER
(COA-STP1) on ONE area.  The only area contrast in the record is G2/G3 (1,600 sectors)
against G6 (8,856 sectors), and that contrast is where the whole effect sits.

## 2. WHAT THE THREE MESSAGES MEAN (vendor source, verified by reading)

ground-vehicle-move-to.lua defines ONE job, "Calc off road nav path part" (:486-530), whose
exitFn prints all three mesh lines:

    :506  printInfo   ("Planned nav path is nil.")                   - query returned nil
    :508  printInfo   ("Planned nav path has not enough (", #res, ") points.")  - <= 1 point
    :511  printVerbose("Planned path has ", n, " points.")           - a usable mesh path

and the FEATURE planner is a different job, "Plan path" (:1371-1407), which calls
vrf:computePathThroughFeatures and prints at :1398

    "Planned path has ", n, " parts."

That job is the fallback; "parts" is never a mesh result.

The one mesh job is wired at TWO call sites, which matters for reading goal distance:

  A. maybePlanOffroadNavPath (sequence, :1461-1470):
       isRoadFollowingDisabled -> isCurrentPointInNavArea (:1416) -> isDestInNavArea (:1422)
       -> createOffroadSegment (:1426, sets ActivePathPart = {this:getLocation3D() ->
          taskParameters.destination}) -> calcOffroadNavPath -> markNoRoadNavPathPlanned.
     This runs BEFORE the feature planner and asks the mesh for the WHOLE LEG.  Its query
     goal IS the move-to destination, so the measured goal distance is exact.
  B. maybeMakeNavPath (fallback selector, :534-545):
       isNavPathPlanned -> isPathPartOutsideNavArea (:478) -> calcOffroadNavPath ->
       alwaysSucceed.
     This runs per path PART after the feature planner, so it appears AFTER a "N parts" row
     and its goal is a path-part end that the console never prints.

Discriminator used throughout: an outcome row with no "N parts" row between it and the
preceding move-to goal is call site A (labelled WHOLE-LEG); one after a "parts" row is call
site B (PER-PART), and for those the distance shown is the distance to the TASK goal, an
UPPER BOUND on the query's own goal.

## 3. METHOD

One streaming pass per trace (G1/G2/G3).  POS rows maintain a rolling last-fix per object;
CON rows are kept only if the raw text carries one of the markers.  Goals are the
geocentric triples in "ground-vehicle-move-to: destination={x, y, z}", converted with the
WGS-84 ecef2ll of scratchpad\console_freeze.py:313; distance is great-circle (haversine)
from the object's most recent POS fix at or before the goal row.  For G6 the same code runs
off scratchpad\g6\cache_G6.pkl, whose filter already kept every move-to / Planned / nav-area
row, so the 1.0 GB file was read only once more, by a targeted grep for line numbers.

Pairing is OUTCOME-ANCHORED - each mesh outcome walks back to the most recent move-to goal
on the same object.  This is the direction G6_RESULTS.md sec 2 used (which is why its counts
are 8 points / 112 nil, not one row per goal) and it counts every query, since one goal can
spawn a whole-leg query AND a per-part query.

Area bounds used for the in-area columns:

    MojaveAO20  lat 34.518 - 34.698, lon -116.809 - -116.591, 40 x 40 tiles, 1,600 sectors
                (PREREG_NAVDATA_2026-09-07.md:36-39)
    MojaveCOA   lat 34.318 - 34.689, lon -117.019 - -116.428, 108 x 82 tiles, 8,856 sectors
                (PREREG_NAVDATA_G6_2026-09-13.md:46-48)

Both areas have ~500 m sectors (20,000/40 = 500 m; 54,200/108 = 502 m; 41,300/82 = 504 m),
so a 10 km path crosses ~20 sectors in EITHER area.  Only the sector COUNT differs, by 5.5x.

POS staleness (goal row wall - POS row wall), which bounds the distance error:
G1 med 1.9 s, max 13.4 s | G2 med 0.6 s, max 2.0 s | G3 med 1.2 s, max 3.8 s.
At 8 m/s the G1 tail is worth ~107 m; every G1 figure below carries that slack.

## 4. TABLES

### 4a. Where each move-to goal stopped in the Lua sequence (one row per goal)

| run | area | sectors | goals | mesh PLANNED | mesh returned 0 | dest gate failed | current-point gate failed | no gate row |
|-----|------|---------|-------|--------------|-----------------|------------------|---------------------------|-------------|
| G1  | MojaveAO20 | 1,600 |  48 | **44** |  0 |  4 |   0 | 0 |
| G2  | MojaveAO20 | 1,600 | 160 | **92** |  2 | 36 |  24 | 6 |
| G3  | MojaveAO20 | 1,600 | 170 | **19** |  2 | 18 | 124 | 7 |
| G6  | MojaveCOA  | 8,856 | 163 | **14** | **86** |  0 |  59 | 4 |

Query success rate once the query was actually reached (whole-leg call site A):
G1 44/44 = 100 % | G2 92/94 = 98 % | G3 19/21 = 90 % | G6 14/100 = 14 %.

Read G3's low "mesh PLANNED" count correctly: it is NOT a query failure.  124 of its 170
goals never reached the query because "Is current point in nav area?" returned false
(163 condition starts, only 39 "Is destination in nav area?" starts - in a sequence the
second is reached only if the first succeeded).  69 of those 124 are before wall 60 s, i.e.
before the area record has been picked up; G6 shows the same race explicitly at level 4
(54 of its 59 current-point failures are before wall 60, and the area is first named at
wall 56.5).  G2 by contrast has zero current-point failures before wall 410 s.

### 4b. Outcome x goal-distance bin, one row per QUERY (outcome-anchored)

G1 - MojaveAO20, R9 order, 44 paired outcomes, all with a POS fix

| phase | outcome | <50 m | 50-500 m | 0.5-2 km | 2-5 km | 5-10 km | >10 km | no POS |
|-------|---------|------:|---------:|---------:|-------:|--------:|-------:|-------:|
| WHOLE-LEG | points (mesh path) | 10 | 9 | 25 | 0 | 0 | 0 | 0 |

(no failures of any kind; "not enough" and "nil" appear 0 times in the whole trace)

G2 - MojaveAO20, COA-STP1, 100 paired outcomes, all with a POS fix

| phase | outcome | <50 m | 50-500 m | 0.5-2 km | 2-5 km | 5-10 km | >10 km | no POS |
|-------|---------|------:|---------:|---------:|-------:|--------:|-------:|-------:|
| WHOLE-LEG | points (mesh path) | 15 | 35 | 9 | 7 | 27 | 3 | 0 |
| WHOLE-LEG | not enough (0)     |  0 |  0 | 0 | 0 |  2 | 0 | 0 |
| PER-PART  | not enough (0)     |  0 |  0 | 0 | 0 |  2 | 0 | 0 |

per-1-km band (POINTS/NIL): 0 km 57/0 | 1 km 2/0 | 3 km 7/0 | 7 km 6/0 | 8 km 7/0 |
9 km 14/4 | 10 km 3/0

G3 - MojaveAO20, COA-STP1, 25 paired outcomes, all with a POS fix

| phase | outcome | <50 m | 50-500 m | 0.5-2 km | 2-5 km | 5-10 km | >10 km | no POS |
|-------|---------|------:|---------:|---------:|-------:|--------:|-------:|-------:|
| WHOLE-LEG | points (mesh path) | 3 | 0 | 0 | 0 | 13 | 3 | 0 |
| WHOLE-LEG | not enough (0)     | 0 | 0 | 0 | 0 |  2 | 0 | 0 |
| PER-PART  | not enough (0)     | 0 | 1 | 1 | 0 |  2 | 0 | 0 |

per-1-km band (POINTS/NIL): 0 km 3/2 | 9 km 13/4 | 10 km 3/0

G6 - MojaveCOA, COA-STP1, 193 paired outcomes, 120 with a POS fix

| phase | outcome | <50 m | 50-500 m | 0.5-2 km | 2-5 km | 5-10 km | >10 km | no POS |
|-------|---------|------:|---------:|---------:|-------:|--------:|-------:|-------:|
| WHOLE-LEG | points (mesh path) | 8 | 0 | 0 | 0 |  0 |  0 |  7 |
| WHOLE-LEG | not enough (0)     | 1 | 0 | 0 | 0 | 15 | 40 | 30 |
| PER-PART  | not enough (0)     | 1 | 0 | 0 | 0 | 15 | 40 | 36 |

per-1-km band (POINTS/NIL): 0 km 8/2 | 9 km 0/30 | 10 km 0/6 | 14 km 0/38 | 20 km 0/36

G6's 73 "no POS" outcomes are all goal rows before wall 110.0, when POS capture began; the
same objects' FIRST fix (wall 110) gives a proxy distance for them, accurate to whatever
they drove in the intervening 24-57 s (sub-kilometre, sec 4a of G6_RESULTS).  On that proxy
the 66 unmeasured failures fall <50 m 4 | 0.5-2 km 2 | 2-5 km 4 | 5-10 km 36 | >10 km 20,
and the 7 unmeasured successes fall <50 m 5 | 50-500 m 2.  So MojaveCOA DID get a handful of
0.5-5 km goals and they all returned zero points - estimated, not measured.  Measured or
estimated, the run never asked MojaveCOA for a goal between about 120 m and 500 m, and the
50 m - 500 m band contains successes only.  That is the remaining hole in the record (sec 10).

## 5. THE DECISIVE ROWS (verbatim, file:line)

All paths are runs\<run>\watchvrf-trace.csv.

### 5a. Longest goal the mesh ever planned - 10,131.6 m, 1,104 points (G2)

    runs\20260907T174654Z_run\watchvrf-trace.csv:81255
    CON,134.6,VRF_UUID:7ebf05d9-a9fe-af49-b9c2-5ba1fe984a77,3,"...Subtask 10 name and parameters: ground-vehicle-move-to: destination={-2357445.694748, -4698802.325654, 3601576.479853}"

      object = C/1-35 / M1A2 50 ; goal 34.594591, -116.643472 (inside MojaveAO20)
      its own position at POS wall 134.3 = 34.664028, -116.715169 (inside) ; 10,131.6 m to run

    runs\20260907T174654Z_run\watchvrf-trace.csv:81638
    CON,135.3,...,3,"  10 .                Starting condition node Is current point in nav area?"
    runs\20260907T174654Z_run\watchvrf-trace.csv:81640
    CON,135.3,...,3,"  10 .                Starting condition node Is destination in nav area?"
    runs\20260907T174654Z_run\watchvrf-trace.csv:82544
    CON,137.7,VRF_UUID:7ebf05d9-a9fe-af49-b9c2-5ba1fe984a77,3,"...<string paramName=""translate="">Planned path has 1104 points.\n</string>..."

3.1 s after the goal row, 2.4 s after the gate started.  NO "Planned path has N parts." row
follows for this goal: the sequence completed at markNoRoadNavPathPlanned (:1441) and the
feature planner never ran.  Sim time is not stamped on these rows; the run's own
console-prefix clock puts wall 134.6 at sim ~233.

### 5b. The same thing in G3 - 10,131.7 m, 1,090 points

    runs\20260913T174516Z_run\watchvrf-trace.csv:70811
    CON,118.8,VRF_UUID:4a23bbce-4cf2-2d4b-8179-3fdbd8d998da,3,"...Subtask 7 name and parameters: ground-vehicle-move-to: destination={-2357445.694748, -4698802.325654, 3601576.479853}"
    runs\20260913T174516Z_run\watchvrf-trace.csv:71736
    CON,119.7,...,3,"  7 .                Starting condition node Is current point in nav area?"
    runs\20260913T174516Z_run\watchvrf-trace.csv:71740
    CON,119.7,...,3,"  7 .                Starting condition node Is destination in nav area?"
    runs\20260913T174516Z_run\watchvrf-trace.csv:73186
    CON,123.9,VRF_UUID:4a23bbce-4cf2-2d4b-8179-3fdbd8d998da,3,"...Planned path has 1090 points.\n..."

Same object name (C/1-35 / M1A2 50), same destination bytes, 5.1 s after the goal row,
4.2 s after the gate started.  Wall 118.8 is sim ~217.

### 5c. The SAME destination on MojaveCOA - zero points (G6)

runs\20260914T002716Z_run, object VRF_UUID:dda7c370-39d2-3c41-ac13-54bd6c2fd97b =
C/1-35 / M1A2 50, decoded from scratchpad\g6\cache_G6.pkl in trace order:

    wall 115.5 L3  ...Subtask 7 name and parameters: ground-vehicle-move-to:
                   destination={-2357445.694748, -4698802.325654, 3601576.479853}
    wall 116.1 L4  .            Node Is current point in nav area?: success
    wall 116.2 L4  .            Node Is destination in nav area?: success
    wall 116.7 L2  Planned nav path has not enough (0) points.
    wall 116.7 L3  Not using roads for move planning.
    wall 117.5 L3  Planned path has 1 parts.
    wall 118.1 L2  Planned nav path has not enough (0) points.

Byte-identical destination to :81255 (G2) and :70811 (G3).  Both gates pass.  The mesh
returns nothing in 0.5 s, the feature planner produces one part, and the per-part mesh query
on that part also returns nothing.

Cross-run sweep of every (member, destination-string) pair present in both runs
(scratchpad\meshq\crossgoal.py; 114 pairs common to G2 and G6):

    MESH-PLANNED in G2 -> MESH-RETURNED-0 in G6 : 22 pairs
    MESH-PLANNED in G3 -> MESH-RETURNED-0 in G6 : 15 pairs
    MESH-RETURNED-0 in G2 -> MESH-PLANNED in G6 :  0 pairs

Sixteen of the 22 have a measured G6 goal distance, and it matches G2's to within 23 m
(G2 m / G6 m): M1A2 50 10,131.6 / 10,131.7 ; M1A2 48 10,081.6 / 10,081.6 ;
M1A2 47 10,031.6 / 10,031.6 ; M1A2 49 9,981.5 / 9,981.5 ; M1A2 46 9,901.6 / 9,901.6 ;
M1A2 44 9,851.6 / 9,851.6 ; M1A2 43 9,801.6 / 9,801.6 ; M1A2 45 9,751.5 / 9,751.5 ;
M577A2 7 9,630.5 / 9,635.8 ; M3 7 9,590.5 / 9,596.6 ; M1A2 38 9,543.9 / 9,546.5 ;
M1A2 37 9,474.0 / 9,496.5 ; M1A2 42 9,446.5 / 9,446.5 ; M1A2 41 9,397.0 / 9,397.1 ;
M1A2 40 9,396.5 / 9,396.5 ; M1A2 39 9,346.5 / 9,346.5.

### 5d. Longest / shortest per run

| run | LONGEST goal WITH a mesh path | SHORTEST goal with NO mesh path |
|-----|-------------------------------|---------------------------------|
| G1 | **821.9 m**, N=84, 114.MechCoy~PXY / M2 9, trace:86461 -> :86846, wall 53.9 -> 54.3 | none - the trace has 0 "not enough" and 0 "nil" rows |
| G2 | **10,131.6 m**, N=1104, C/1-35 / M1A2 50, :81255 -> :82544, wall 134.6 -> 137.7 | **9,478.8 m**, C/1-35 / HMMWV 13, :81200 -> :82553, wall 134.5 -> 137.7 |
| G3 | **10,131.7 m**, N=1090, C/1-35 / M1A2 50, :70811 -> :73186, wall 118.8 -> 123.9 | **9,596.3 m** whole-leg, C/1-35 / HMMWV 13, :74579 -> :75809, wall 125.8 -> 127.5 |
| G6 | **34.6 m**, N=2, 1-1/2/1_AD~PXY / M1A2 26, wall 428.3 -> 429.4 (outcome at trace:1044396) | **17.5 m**, 40/2/1_AD~PXY / M3 5, wall 621.9 -> 623.8 ; next shortest 9,346.5 m |

G1's longest, verbatim:

    runs\20260907T170643Z_run\watchvrf-trace.csv:86461
    CON,53.9,VRF_UUID:ec1e6fe1-8599-c143-8237-0c3a2339f7a7,3,"...Subtask 5 name and parameters: ground-vehicle-move-to: destination={-2359791.776835, -4693303.734516, 3607554.391618}"
      goal 34.658696, -116.693237 (inside) ; from 34.651340, -116.694125 at POS wall 40.5
    runs\20260907T170643Z_run\watchvrf-trace.csv:86846
    CON,54.3,VRF_UUID:ec1e6fe1-8599-c143-8237-0c3a2339f7a7,3,"...Planned path has 84 points.\n..."

G2's shortest failure, verbatim:

    runs\20260907T174654Z_run\watchvrf-trace.csv:81200
    CON,134.5,VRF_UUID:3163e8e9-07be-834a-8f01-6d85f98614c6,3,"...Subtask 11 name and parameters: ground-vehicle-move-to: destination={-2357480.931262, -4698801.865483, 3601554.249758}"
      C/1-35 / HMMWV 13 ; goal 34.594347, -116.643817 (inside) ; from 34.659324, -116.710873
    runs\20260907T174654Z_run\watchvrf-trace.csv:82553
    CON,137.7,VRF_UUID:3163e8e9-07be-834a-8f01-6d85f98614c6,2,"...Planned nav path has not enough (0) points.\n..."

G6's longest success, verbatim (line from a targeted grep over the 1.0 GB trace):

    runs\20260914T002716Z_run\watchvrf-trace.csv:1044396
    CON,429.4,VRF_UUID:4a526239-9ed0-af40-be5d-b406a3560caa,3,"...Planned path has 2 points."
      1-1/2/1_AD~PXY / M1A2 26 ; goal 34.651090, -116.812231 (inside MojaveCOA) ;
      from 34.651159, -116.811863 at POS wall 426.5 ; 34.6 m.

G3's shortest failure OVERALL is a per-part row at 305.0 m (trace:1195 goal -> :40205
outcome) but the two rows are 34.4 s apart, which is long enough that the attribution is not
safe; the whole-leg figure in the table is the defensible one.  The same caveat retires
G2's two >10 s latencies (sec 7).

## 6. DID THE MESH PLAN R9's ACTUAL ROUTE LEGS IN G1?  YES - but R9's legs are 0.56-0.58 km

data\R9_Mojave_UnitMove_Order.xml (the order G1 pushed, per run-manifest.json:28) has three
tasks, each a 2-vertex route:

    T_R5_PL1  578 m | T_R5_CO1  556 m | T_R5_TK1  578 m

G1's 44 mesh-planned goals span 31.6 m to 821.9 m, median 523.1 m - i.e. the mesh planned
the authored legs themselves (the spread above 578 m comes from per-member offset routes
and from up-to-13 s POS staleness).  All 44 were whole-leg queries; every one succeeded, at
N = 5 to 84 points.

The four goals that did NOT reach the mesh in G1 were rejected by the DESTINATION gate, not
by the query: goals 34.612831/-116.586934 (trace:107682), 34.613281/-116.587479 (:110777),
34.612381/-116.587479 (:110974), 34.613732/-116.588023 (:113444) - 1222.MechPlt / M2 1-4.
All four lie EAST of MojaveAO20's east edge (-116.591), 576-674 m out.  Those four goals,
and only those, produced the run's four "Planned path has 1 parts." rows.

So the brief's decisive test - "if the mesh planned multi-hundred-metre or kilometre goals
on the small area and only short goals on the large one, the failure is scale-dependent" -
resolves as: the mesh planned multi-hundred-metre goals (G1, up to 822 m) AND kilometre
goals (G2/G3, up to 10.13 km) on the 1,600-sector area, and only 12-35 m goals on the
8,856-sector area.  Scale-dependent, with the AREA - not the goal - as the varying term.

## 7. HOW LONG THE QUERY TOOK (fast refusal vs exhausted search)

Two measures.  The tight one - from the "Is destination in nav area?" condition to the mesh
outcome row, which brackets the query itself - and the loose one from the goal row.

Tight (dest-gate START -> mesh outcome, wall seconds):

| run | POINTS n / min / med / max | NIL n / min / med / max |
|-----|----------------------------|-------------------------|
| G1 | 44 / 0.0 / 0.4 / 1.4 | - |
| G2 | 96 / 0.4 / 1.6 / 4.0 | 2 / 0.7 / 2.7 / 2.7 |
| G3 | 19 / 0.9 / 3.4 / 5.0 | 2 / 1.1 / 3.0 / 3.0 |
| G6 | 15 / 0.5 / 0.7 / 1.2 | 86 / 0.4 / 0.6 / 1.3 |

G6 only (it is the run with level-4 member consoles, so the gate's SUCCESS row is printed):
dest-gate SUCCESS -> outcome, n=86 failures: min 0.4, p25 0.5, med 0.6, p75 0.6, max 1.3 s.
Histogram: <0.5 s 15 | 0.5-1 s 69 | 1-2 s 2 | >2 s 0.
Same measure for G6's 15 successes: min 0.5, med 0.7, max 1.2 s.

Loose (goal row -> outcome) for the 178 G6 "not enough" rows: min 0.8, p25 1.2, med 1.8,
p75 2.4, max 4.0 s ; histogram 0.5-1 s 11 | 1-2 s 83 | 2-3 s 69 | 3-5 s 15 | >5 s 0.
Split by goal band, G6 NIL: <1 km n=2 med 3.1 s ; >=9 km n=110 min 0.8 med 1.7 max 3.2 s.

Reading, stated as a reading and not a mechanism: NO G6 failure is instantaneous at the
capture's 0.1 s stamp resolution, but every one of them resolves within 1.3 s of the gate -
the same band as that run's 12-35 m successes (0.5-1.2 s), and quicker than the 10 km
SUCCESSES on the smaller area (2.4 s in G2, 4.2 s in G3, sec 5a/5b).  Cost does not rise
with goal length on MojaveCOA; it is flat at the floor.  That is what a refusal or a budget
cut-off looks like, not a search that walked 8,856 sectors and gave up.
Caveat: these are console-row deltas, quantised by the behaviour-tree tick and by WatchVrf
delivery; they bound the query's wall cost from above, they do not measure its CPU time.

## 8. "Is PathPart outside nav area?" IN G6

Resolution rows: ZERO.  A grep over the whole 1.0 GB trace for
"Node Is PathPart outside nav area?: " and "fail in action Is PathPart" returns no lines at
all, while the same run prints 101 success + 59 fail for "Is current point in nav area?" and
101 success + 0 fail for "Is destination in nav area?".  So the condition's verdict must be
read from what follows the "Starting condition node" row, using the selector's semantics
(:534-545: the condition SUCCEEDING means the part is outside and the mesh call is skipped):

| G6, 145 "Starting condition node Is PathPart outside nav area?" rows | n |
|---|---|
| followed by a mesh outcome -> condition FALSE, part judged INSIDE, mesh queried | **92** - and all 92 returned "not enough (0) points." |
| followed straight by "Turn to route" / the next goal -> condition TRUE, part OUTSIDE, mesh skipped | 51 |
| undetermined within the window | 2 |

(163 "Creating condition" rows exist for that node; 145 of those conditions were ticked.)

So of G6's 178 "not enough" rows, 86 are whole-leg queries and 92 are per-part queries on
feature-planned parts the sim itself judged to be INSIDE the area.  Every path part it
considered in-area, it then failed to mesh-plan.

The same classification for the other runs is less determinate, because those runs ran the
objects at notify level 3 and many following rows are not in the kept set.  For the record:
G2 62 PathPart starts -> 2 mesh-queried (both 0 points), 24 skipped as outside, 36
undetermined; G3 146 starts -> 4 mesh-queried (all 0 points), 82 skipped as outside, 60
undetermined; G1 4 starts -> 4 undetermined (G1 has no mesh-failure row at all, so none of
the four can have been queried with a result - consistent with all four parts lying outside,
which their goals do, sec 6).

## 9. VERIFIED vs ASSUMED

VERIFIED (from the runs' own bytes or the vendor source):

- The three message strings and which planner emits each, from ground-vehicle-move-to.lua
  :506, :508, :511, :1398 - read directly.
- Both call sites of the mesh job and the gate sequence, :478, :486-545, :1416-1470.
- Row counts per run (grep -c on each watchvrf-trace.csv), points/parts/not-enough:
  G1 44/4/0, G2 96/62/4, G3 19/144/6, G6 15/145/178.  These match the brief's counts.
- G1/G2/G3 name "NavArea-ground-platform MojaveAO20" and nothing else; G6 names "MojaveCOA"
  and nothing else.
- The verbatim rows of sec 5 and 6, at the stated file:line.
- The byte-identical destination string across G2:81255, G3:70811 and G6's M1A2 50 row.
- G6's gate outcomes at level 4: destination-in-area success 101, fail 0.
- R9's authored leg lengths from data\R9_Mojave_UnitMove_Order.xml.
- G6 has no "Is PathPart outside nav area?" resolution row anywhere in the trace.

ASSUMED / INFERRED (flagged, not established):

- Goal distance is measured from the object's LAST POS FIX, not its true position at the
  goal row.  Staleness is median 0.6-1.9 s (max 13.4 s in G1).  Every distance carries that.
- WHOLE-LEG vs PER-PART is inferred from row ORDER, not from an identifier in the message.
  A dropped or reordered row would mislabel a query.  The labelling is self-consistent:
  G6's 92 per-part queries equal its 92 in-area path parts of sec 8.
- The gate decomposition of sec 4a for G1/G2/G3 is inferred from which "Starting condition
  node" rows appear, because those runs print no success/fail rows.  G6, which prints both,
  behaves exactly as that inference predicts.
- The declared area bounds are taken from the preregs, not re-read from the .navRuntimeConfig.
- Sector SIZE is computed from the declared extent divided by the declared tile count.
- "Sim time" for the G1/G2/G3 decisive rows is interpolated from the run's console-prefix
  clock; those specific rows carry no sim prefix.
- G6's 73 outcomes without a POS fix (7 successes, 66 failures) are all before POS capture
  began at wall 110.0.  Their goal distances in sec 4b are PROXIES taken from the object's
  first fix, 24-57 s after the goal row.  The seven successes stood 0.9-118.7 m from their
  goal at that fix; the 66 failures 1 m to 31.3 km.  Four of the 66 proxy at <50 m, which is
  probably the proxy lagging a goal the object had already reached, not a 40 m failure.

## 10. STRONGEST COMPETING EXPLANATION, AND WHAT WOULD FALSIFY THE READING

Reading offered: the mesh query's ability to return a long path collapsed when the
navigation area went from 1,600 to 8,856 sectors, and the collapse is a refusal (flat,
floor-level cost) rather than an exhausted search.

STRONGEST COMPETITOR: THE MojaveCOA DATA ITSELF IS DEFECTIVE - the generator wrote 8,856
sectors but the resulting graph is not connected across sector boundaries (or a subset of
sectors is empty), so any query leaving the start sector returns nothing.  This explains
every observation here exactly as well as a size or budget effect does: the only successes
are 12-35 m goals, which fit inside one ~500 m sector; the failures are flat and fast,
because a query that cannot leave its sector fails immediately; and G2/G3 succeed because
their smaller area was generated in one clean 24-minute run.  Nothing in these four captures
distinguishes the two.

A weaker third competitor - concurrency / engine load - is already strained: G2 and G6 issue
almost the same number of goals (160 vs 163) from the same order with the same vehicle
count, and G6's queries are the FASTER ones.

The observation that separates them is thin in the record, and completing it is cheap.  The
only MojaveCOA evidence between 35 m and 9.3 km is the six PROXY-distance failures of sec 4b
(two at 0.5-2 km, four at 2-5 km) - suggestive that even short goals fail there, which would
favour the broken-data competitor, but not measured and therefore not decisive.  The test:

  On MojaveCOA, task ONE vehicle to a goal ~600 m away that crosses exactly one sector
  boundary, then ~2 km, then ~5 km, member console at level 4.
  - If 600 m returns points and 5 km returns 0, the graph IS connected across sectors and
    the failure is distance- or budget-related on the big graph.  That falsifies "the data
    is broken" and supports the refusal reading.
  - If even the 600 m cross-sector goal returns "not enough (0) points.", the MojaveCOA data
    is broken, and the whole "area size" reading of G6 is WITHDRAWN - nothing about 8,856
    sectors would have been learned, only that this generation is unusable.
  - A third outcome - 600 m and 2 km succeed, 5 km fails - puts a measurable ceiling between
    2 and 9 km and makes (setqb gamewareQueryTimeBudget), UG52 66.2.1's "path planning over
    long distances may be slower", and sectorisation the first things to test.

Two further falsifiers that would break the reading as stated:

- If G2's 1,104-point path turns out never to have been FOLLOWED (the tree accepted it but
  the vehicle drove a feature path anyway), "the mesh planned a kilometre" is weaker than
  stated.  Checked so far: no "N parts" row follows that goal on that object, which is what
  markNoRoadNavPathPlanned (:1441) predicts.  The vehicle's actual track was NOT compared
  against the 1,104 points; that comparison is still owed.
- If a rerun on a REGENERATED MojaveCOA plans a 10 km leg, the area-size reading dies and
  the 2026-09-13 generation (3-3.5 h, 26,525 files) becomes a single-run defect.

UNEXPLAINED, recorded as falsifiers rather than footnotes:

- On MojaveAO20 the two whole-leg failures in G2 and the two in G3 are all HMMWVs
  (HMMWV 13 at 9,478.8 / 9,596.3 m, HMMWV 14 at 9,678.2 / 9,684.6 m) while M1A2s of the same
  company got 1,036-1,104-point paths to goals 100-650 m FURTHER in the same wall second.
  Nothing here accounts for a per-vehicle-type difference in the same query on the same
  graph; the bounding-volume buffer the FEATURE planner uses (:1379) has no counterpart in
  the mesh call's params (useAbstractGraphs=false, useChannels=true, channelRadius=4.0,
  :495), so the obvious explanation does not apply.
- G6 has ONE sub-50 m failure (17.5 m, 40/2/1_AD / M3 5, wall 621.9) sitting among
  successes at 12-35 m on the same area.
- G3 lost 124 of 170 goals to the current-point gate while G2, same order and same area,
  lost 24 - and 69 of G3's are in the first 60 wall-seconds.  This analysis records the
  asymmetry and does not explain it.

## 11. NEXT

1. The three-goal cross-sector probe of sec 10 on MojaveCOA - one vehicle, ~600 m / 2 km /
   5 km, console level 4.  It is the only measurement that separates the two live
   explanations, and it is a single short run.
2. Only if that says the graph is connected: compare (setqb gamewareQueryTimeBudget) and
   gamewareMemorySize against the 8,856-sector graph (both live under C:\MAK per
   PREREG_NAVDATA_G6_2026-09-13.md:133-134).
3. Owed regardless: check G2's M1A2 50 track against its 1,104 planned points, to confirm
   the mesh path was followed and not merely returned.
4. Correct PREREG_NAVDATA_G6_2026-09-13.md:127 ("G1 (R9, 3x3 km area)") - G1 ran on the
   20 x 20 km MojaveAO20, sec 1.
