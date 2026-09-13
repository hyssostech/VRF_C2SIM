# PREREG G4: COA-STP1 on the navigation area, full length, idle machine (the clean G2)

Registered 2026-09-13 by the supervisor (Fable 5.1) BEFORE launch; the step G3's decision tree
(PREREG_NAVDATA_G3_2026-09-13.md sec 5, "P13a MISS") fixed in advance. Tier: HEAVY. Question: what
does navigation data do to the COA's movement when the engine is NOT collapsing - the question G2
was meant to answer and could not, because a concurrent load contaminated it.

## 0. What is settled going in (pointers, not re-argument)
- Loading chain works (G1; PREREG_NAVDATA_2026-09-07 G1 RESULTS): the area is read, entered and
  planned on ("Is current point in nav area?" nodes; New/Leaving Primary nav area rows).
- G2 (with the area + a concurrent load) vs P11 (no area) at EQUAL SIM TIME 1170 s: summed median
  displacement 1.04x; 1-35/2/1_A frozen at 1.97 km in both; 1-6/2/1_AD SPLIT with the mesh (4 of 6
  members past 6.2 km, 2 stuck at 2.88-2.92 km) where P11's whole unit stopped at 2.85 km;
  BlockedByVehicle 518 vs 147 by sim 1170 (3.5x). G2's engine collapse WITHDRAWN by G3 (idle
  machine: 1.80x -> 1.45x for 1500 s, sim to 2563 s, ~5 cores steady).
- P11 facts beyond sim 1170 (the recompute of 2026-09-13): first > 1 km stragglers at sim 1960
  (856/HHC), 3775 (C/1-35), 4563 (40/2/1_AD); 3 arrival-evidence completions by sim 6288; 1-6 at
  2884 m at sim 6000; 1-35 at 1970 m at sim 6000; P11's units slowed from 63.7 m per sim-second over
  the first 1170 s to 25.1 over 6235.
- Vendor statements that bear on the predictions: UG52 30.22 p598 (a unit's subordinates plan a
  path to each vertex), UG52 30.28 p604 ("If navigation data is present, simulation objects take it
  into account in planning their path"), Developer's Guide help vrf_groundVehiclePathPlanningNavMesh
  ("each off-road segment is planned using this nav mesh. The planner takes the costs of different
  types of soils and the slope of the terrain into account"), UG52 66.3 p1281 (advanced navigation
  inside areas, standard between them), ground-vehicle-move-to.lua 219-264 ("blocked" = subtask
  status BlockedByWall or BlockedByVehicle for 10 s, nothing else).

## 1. The run (ONE variable vs P11: the scenario/terrain/area; nothing else on the machine)
Exactly G2's command (PREREG_NAVDATA_2026-09-07 sec 3) - fixture R9_Mojave_Empty_52_Nav, build
80daed6, the G2/P11 environment block, the PROBE type map, -NoGui, -RunSecs 4200 -WatchSecs 4500
-BackendNotifyLevel 3 -StopWhenComplete - plus scripts/SampleThreads.ps1 (-MaxSec 4700) as in G3.
NO agents, workflows, analysis scripts or generator runs while the run is up (the rule G3 was paid
for). RTI = the pair the G3 runner started (never torn down); C2SIM server = our private container.
Comparison rule: every cross-run comparison is made at EQUAL SIMULATED TIME (the smaller run's end;
expected ~6,000 s if ~1.45x holds), never total/total (memory lessons-compare-at-equal-sim-time).

## 2. Predictions (before launch; a missed HIGH prediction is a stop)
- P14a (HIGH, the engine): no 300 s window below 1.0x sim/wall over the whole 4200 s; the sim
  process profile stays ~5 cores with the same hot-thread set. MISS = a slow-developing collapse on
  an idle machine -> G3's withdrawal is REVERSED (H1, navigation cost at scale, is back) and the
  threads.csv at the onset is the first instrument to read; stop.
- P14b (MEDIUM, no general speed effect): summed median displacement G4/P11 within 0.90-1.10 at
  sim 1170 AND at the common end (~6000). MISS either way is a finding, recorded with the per-unit
  table.
- P14c (MEDIUM, completions): arrival-evidence completions by the common sim time >= 2 (P11: 3 by
  6288). 0 or 1 = a regression the consoles must explain before anything else is claimed.
- P14d (MEDIUM, the mesh's promised effect, inside the area): 1-6/2/1_AD at the common end has
  >= 4 of 6 members beyond 6 km (G2's split reproduced or bettered; P11: none). MISS (whole unit at
  ~2.9 km as P11) = the G2 split was not the mesh's doing.
- P14e (HIGH, "terrain freed by the mesh" refuted or not): 1-35/2/1_A stays frozen at ~1.97 km
  (three runs so far, with and without the mesh, inside the area). HOLDS -> the terrain/slope
  reading of 1-35's stop is REFUTED for good (the slope-aware planner is active where it stands and
  it does not move), and its OWN console at the stop is the next instrument (blocked status? a
  formation/speed-control wait? a failed subtask?). MISS (1-35 moves past 6 km) = the mesh freed it
  and the earlier freeze had a terrain component; recorded.
- P14f (MEDIUM, blocking): BlockedByVehicle by sim 1170 in 350-700 (G2 518 vs P11 147). HOLDS -> the
  3.5x was a mesh effect (tighter paths, more mutual proximity), not a load artefact. MISS toward
  P11's 147 -> it was the load.
- P14g (recorded, not predicted): stragglers > 1 km per unit at the common end vs P11's three; the
  1-35 trap at 26 km (only if 1-35 moves); working-set growth of the sim process over 70 min (G3:
  ~20 MB/min - a watch item for demo-length runs, UG52 6.1.1 says nothing about it).

## 3. What counts as a stop
P14a missed = stop. Everything else is measurement to be reviewed cold before the record carries it.

## 4. After G4 (fixed now)
- P14a/P14b/P14e hold, P14d holds: the mesh changes formation-keeping at one unit's obstacle but not
  the COA's speed or 1-35 -> the DEMO decision (DEMO_READINESS 10a) is the user's, with these
  numbers; the interface work that follows is 1-35's console, not more mesh.
- P14e misses: the slope story for 1-35 was right after all -> the coverage question (6 areas for
  the 39x52 km box, ~2.5 h generation, ~1 GB) goes to the user.
- P14a misses: reopen the engine question with threads.csv in hand; no lever touched first.

## 5. Results
(to be filled after the run; nothing here was written before it)
