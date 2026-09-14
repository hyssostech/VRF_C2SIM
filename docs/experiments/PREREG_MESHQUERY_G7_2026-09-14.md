# PREREG G7 - cross-sector probe on the full-COA navigation area (2026-09-14)

Supervisor: Fable 5.1. Tier HEAVY (a discriminating measurement between two live cause readings).
Gate: PREREG - predictions and falsifiers written before the run. Design: docs/experiments/
G7_PROBE_DESIGN_2026-09-14.md (Opus executor; supervisor-adopted). Docs consulted: NAVMESH_QUERY_DOCS_
2026-09-14 (UG52 66.2 p1276 max area 20x20 km at default precision, 66.2.1 p1277 sectorisation "path
planning over long distances may be slower", Appendix C p1670-1671 gamewareMemorySize / QueryTimeBudget,
class_dt_nav_area_query outOfWorkingMemory / hadComputationError), MESH_QUERY_VS_DISTANCE_2026-09-14
(the area-dependence finding), ground-vehicle-move-to.lua :486-530 / :1416-1470 (one mesh job per leg,
the three outcome rows).

## 1. The question
G6 showed vrf:findPathToLocation returning 0 points for every multi-km goal inside the loaded 41x54 km
MojaveCOA area (8,856 sectors), while the 20x20 km MojaveAO20 (1,600 sectors) planned the same
destination bytes at 10.1 km (G2/G3). Two readings survive: (A) the big graph is refused or budget-cut
by the planner (refusals are fast: 0.4-1.3 s); (B) MojaveCOA's data is not connected across sector
seams. Nothing in the captures separates them.

## 2. The run (ONE variable vs G6 = the order; same fixture, same area, same build)
- Fixture R9_Mojave_Empty_52_NavAO (the G6 fixture; terrain copy with the MojaveCOA record; NOT the
  _AG sibling). Deployed build unchanged (exe 2026-09-07 11:06Z). Hardened runner (f6e68d9) through
  scripts/RunScenario.sh - its FIRST live use (row 22).
- Init data/R9_Mojave_Lean_Initialization.xml (G1's), ClientId STP, type map
  data/unit-type-map-52-nolifeform.json, CreationPolicy AtOrder -> ONE entity expected.
- Order data/PROBE_G7_CrossSector_Order.xml: performer 1.BdeHQ (M577A2_Command_Post, one platform,
  no formation gate, no lifeforms), route S 34.608416/-116.712685 -> V1 (598.7 m, crosses exactly one
  lon seam at 489 m) -> V2 (1,995.7 m, 5 seams) -> V3 (4,989.4 m, 11 seams); all vertices >= 8.9 km
  inside the area; pre-flight worst sustained ratio 0.205 (sand), i.e. no slope confound.
- Env = the P11/G2/G3/G6 block with member consoles 4 (Vrf__ObjectConsoleNotifyLevel=4 and
  ObjectConsoleMemberNotifyLevel=4). -RunSecs 900 -WatchSecs 1200 -StopWhenComplete; expected drive
  ~774 sim s (~9.8 m/s from G1's own controller clock) = ~500-560 wall s at ~1.6x.
- Machine idle (no executors) for the duration; runner stdout to a file; foreground polling.

## 3. Predictions (a missed HIGH prediction is a stop)
- P17a (HIGH, gate): on all three legs the console prints "Is current point in nav area?: success" and
  "Is destination in nav area?: success" and names NavArea-ground-platform MojaveCOA. MISS = the area
  did not load or the wrong fixture ran; stop, do not read the mesh rows.
- P17b (THE MEASUREMENT - no prior; the record does not separate A from B): leg 1 (599 m, one seam)
  prints EITHER "Planned path has N points." (mesh) OR "Planned nav path has not enough (0) points.".
  Reading: points on leg 1 AND 0 on leg 3 -> the graph IS connected across seams, (B) falsified,
  the failure is length/budget-related (then G7b = the abstract-graph SMS, then G8 = gamewareMemorySize
  via --appDataDir). 0 on leg 1 -> the data is unusable across seams; the G6 "area size" reading is
  WITHDRAWN and the generation is the suspect (regenerate / different raster / sector size). Points on
  all three -> the ceiling is between 5 and 9.3 km (G6's shortest failing goal 9,346.5 m).
- P17c (MEDIUM): leg 3 (4,989 m) returns 0 points.
- P17d (MEDIUM, movement): the vehicle reaches V3 (arrival by the trace, not by a C2SIM report). A stop
  on this benign route is the SECOND mechanism (FINDING sec 7b/7c), recorded against the FINDING, not
  explained away.
- P17e (instrument, RECORDED): 1.BdeHQ~PXY is 11 characters, so VR-Forces truncates its marking and the
  interface's by-name lookups miss (reporting gap G1, fix B3 in flight): expect "1 skipped" R1 lines,
  NO C2SIM position reports and NO arrival-evidence TASKCMPLT for it, so -StopWhenComplete never fires
  and the window runs to the 900 s cap. The trace (by uuid) is unaffected. If position reports DO
  appear, G1's mechanism is wrong and the reporting assessment is corrected.

## 4. Falsifiers and confounds named up front
- Timing reading (MESH_QUERY sec 7): per leg, wall delta from the destination-gate row to the outcome
  row; compare with G6's 0.4-1.3 s failures and MojaveAO20's 2.4-4.2 s 10 km successes.
- If more than one entity is created (CreationPolicy not honoured), pairing weakens to G6's inferred
  pairing; check the creation count first.
- The start's own sector clearance is 11.9 m east of a seam; leg 1 runs west, so the count is 1 unless
  the created position is > 11.9 m east of the authored one - re-derive from the first POS fix.
- Vehicle type: an M577A2 is one of the 22 documented G2->G6 flips (M577A2 7), so it is a proven mesh
  client; the unexplained HMMWV-vs-M1A2 asymmetry on the small area is carried, not resolved here.
- Optional paired control (design sec 9): the same order on the MojaveAO20 fixture - only after a
  fresh boot (teardown-relaunch wedges the RTI).

## 5. RESULTS
(to be written from the harvest; verdict table per leg: seams / gate cur / gate dest / mesh outcome /
N points / gate->outcome s / vertex reached)
