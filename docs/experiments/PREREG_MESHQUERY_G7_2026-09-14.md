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
ATTEMPT 1 (run 20260914T120444Z, launched 12:04Z through scripts/RunScenario.sh - the wrapper's stages, markers and
status line all worked): VOID before measurement. The server accepted the order but the interface never received it:
the SDK's STOMP message pump died parsing it ("Unexpected end of file while parsing Comment has occurred. Line 1,
position 769", C2SIMClientSTOMPLib.cs:688) and the interface logged "C2SIM error ... Restart recommended". Cause: the
probe order carried a multi-line leading XML comment containing a BLANK LINE (byte 263 of the file; ~769 after the
server's envelope) - a blank line terminates a STOMP frame body; the working R9 order has none. Also observed: the
init created all six R9 units (CreationPolicy=AtOrder creates empty shells at init and platforms in full), so the
one-entity expectation in the design was wrong - 1.BdeHQ is still the only tasked object. Order rewritten without
blank lines or multi-line comments (same tasks, same vertices); attempt 2 follows on the same fixture and build.
ATTEMPT 2 (run 20260914T122525Z, 12:25Z): the order arrived (one ORDER on the bus at 12:27:54), the route was
created (4 pts) and MoveAlongRoute issued; the interface's arrival-evidence rule reported TASKCMPLT 47 s after
dispatch - and the by-name lookup WORKED for the 11-character marking "1.BdeHQ~PXY" (P17e's expected symptom did
NOT appear: the sim's marking limit is therefore >= 11, and the G6 truncation of the 14-char "2/1_AD/25_~PXY" to 10
chars is the case to explain - reporting B3 stands, the width is 11 not 10). VOID FOR THE MESH QUESTION: the lone
PLATFORM's Move-Along ran the NATIVE move-along controller ("Controller base-system.movement.move-along beginning
to process" at sim 112.3, "Completed" at sim 878.1) and never entered ground-vehicle-move-to.lua - zero
destination / gate / "Planned" rows at console level 4 - so P17a is a MISS for a third, unforeseen reason: the
wrong performer KIND. Control: G1's 1.BdeHQ printed 0 planner rows on the small area while 114.MechCoy's members
printed 48. The vehicle drove the whole 7,584 m route to V3 at ~9.9 m/s (766 sim s; the one-entity sim ran ~15x
real time). The hardened wrapper worked end to end twice (markers, StopIface, graceful StopVrf, RTI preserved).
ATTEMPT 3: performer = the tank platoon 1222.MechPlt (an aggregate; its members run the Lua planner through
maneuver-in-formation - the proven G1 mover), route re-derived from its position with the same seam design and
150 m margins for the formation spread; order without blank lines. Predictions P17a-P17d unchanged in substance;
P17e withdrawn (the marking width is >= 11).
ATTEMPT 3 (run 20260914T130439Z, 13:04Z; performer 1222.MechPlt as Tank Platoon (USA) = 4 M1A2 under the
no-lifeform map; order v3, no blank lines): VOID, and the reason is a FINDING. The four members were created at
order time (CreationPolicy=AtOrder) at wall ~25 and ALL 18 of their goals (slot moves, V1, V2, V3) fired between
wall 25.6 and 59.2 (the 4-tank sim ran ~15x real time; the tanks drove the whole 7.6 km route to V3 by wall ~60,
net 7,516-7,615 m each). Every one of the 18 goals FAILED the 'Is current point in nav area?' condition (18 'fail
in action' rows; the destination gate was never evaluated; 18 'Planned path has 1 parts' = feature planner), and
the area's 'New Primary nav area: NavArea-ground-platform MojaveCOA' rows for the 4 members + the shell arrived at
wall 201.4 - 140 s AFTER the last goal. The sectorised area loads LAZILY after entities are placed (vrfSim.mtl:433-436
loadAllNavigationDataOnTerrainLoad default 0: 'navigation data will be loaded when an entity is placed'; UG52 App. C
p1671), and under AtOrder the members are created and tasked in the same second, so NO first leg is ever mesh-planned
in that flow. Same mechanism seen before and misread as a race: G3's 69 early current-point failures, G6's 54 before
wall 60. Consequence for the demo: with the default AtOrder policy the mesh is never consulted for the first legs.
ATTEMPT 4: CreationPolicy=AtInit (members exist from init) + a pre-order settle >= 240 s (runner -PreOrderSettleSecs,
being added) so the area's rows precede the first goal; the harvest must show the 'New Primary nav area' rows BEFORE
the first goal row for each member. Vendor-side fix (loadAllNavigationDataOnTerrainLoad 1 in a relocated appData,
sanctioned) being prepared in parallel for the demo flow.
ATTEMPT 4 (run 20260914T154243Z, launched 15:42:43Z; CreationPolicy=AtInit, -PreOrderSettleSecs 240, order on
the bus 15:49:49.653Z; performer 1222.MechPlt = Tank Platoon (USA), 4 x M1A2 under the nolifeform map): THE
MEASUREMENT WAS TAKEN. Full record docs/experiments/G7_ATTEMPT4_RESULTS_2026-09-14.md (trace t=0 fitted to
2026-09-14T15:45:14.2Z from the taskee's own position reports; engine 8.84x).
VERDICT TABLE - 18 goals over 4 members, 36 of 36 gate rows "success", 0 failures, the only area named in the
run being NavArea-ground-platform MojaveCOA. Per member, seams re-derived from its OWN position at the goal
row: slot move 36-94 m / 0 seams -> mesh 6, 6, 11, 11 points; leg 1 520-673 m / 1 seam -> mesh 54, 59, 63, 69
points; a second 0-seam slot refresh at V1 for M1A2 1 and 4 -> mesh 2 points; leg 2 1,902-1,994 m / 4 seams ->
mesh 209, 209, 225, 226 points; leg 3 4,914-5,048 m / 10 seams -> "Planned nav path has not enough (0) points."
on 4 of 4, ONE query each, NO RETRY, then "Not using roads" + "Planned path has 1 parts" (feature planner) and
one further per-part query also 0. Gate-row to outcome-row: 0.0-0.2 s wall for EVERY outcome, successes and
refusals alike. All four tanks then drove the refused 4,989 m on the feature planner's straight part and
stopped at their V3 slots at 15:51:27.8Z (aggregate 5.1 m from V3; members 26.8-83.8 m; 7,617-7,692 m driven;
no motion for the remaining 18.7 min).
P17a HIT (36/36 gates success, MojaveCOA the only area named - by the Primary-area rows, not by the gate rows).
P17b - the FIRST branch of the decision table fires: points on leg 1 AND 0 on leg 3, so the graph IS CONNECTED
ACROSS SEAMS, reading (B) as stated is FALSIFIED, and the failure is length/budget-related -> G7b (abstract-
graph SMS) then G8 (gamewareMemorySize via --appDataDir). Stronger than the branch required: leg 2 planned
too, so connectivity holds across 4 seams / 1,994 m. MojaveCOA's ceiling is now bracketed to (1,994 m,
4,914 m], replacing G6's 9,346.5 m; MojaveAO20 (same ~502 m sectors, 1,600 vs 8,856) planned 10.1 km, so the
ceiling is a property of the AREA, not of the leg - which is reading (A). CAVEAT ON THE DESIGN: at 501.9 m per
sector, length and seam count are perfectly collinear on this area, so a residual (B') "connectivity degrades
with seam distance" is untested and only a different sector size or area can separate them.
P17c HIT (4 of 4). P17d HIT (V3 reached, evidence above plus the C2SIM reports). TIMING READING NOT
REPRODUCED: MESH_QUERY sec 7's "fast refusal" is not a discriminator here because the successes were equally
fast; at 0.1 s trace resolution and 8.84x, one bucket is 0.88 sim s - coarser than the effect.
LAZY-LOAD ADJUDICATED: H1 ("the area loads on the first planning request") FALSIFIED - 1.BdeHQ, never tasked
and never planning, printed the run's only first-acquisition "New Primary nav area" row (no "Leaving"
predecessor) at 15:49:40.2Z, 236.9 s after placement and 9.5 s BEFORE the order; and the members' FIRST goal
already passed both gates and got a mesh path. H2 survives corrected (the five aggregates print no row because
the area is a ground-PLATFORM area; the one console-enabled platform did print). H3 survives as complementary
(all 14 member rows are "Leaving X" then "New X" - re-registrations). Second instrument agrees: the back-end's
working set climbs 2,324 MB at a steady ~5 MB/s from the placement instant and plateaus within one 5 s sample
of that row. So the 240 s settle bought the run with ~9 s of margin; the implication (NOT verified here) is
that loadAllNavigationDataOnTerrainLoad=1 in the relocated appData is the demo-grade fix.
INSTRUMENT CORRECTIONS: the taskee's 16-char marking was NOT truncated (TSK,373,"1222.MechPlt~PXY" in full)
and it DID produce 147 C2SIM position-report ticks; -StopWhenComplete ran to its 1200 s cap because runner
condition (4) needs an RPT (VR-Forces radio text) line in the trace and this trace - like the four prior traces
checked - carries ZERO RPT rows, so the condition is unsatisfiable as written. "TASKCMPLT at t+77 s" is
window-relative; from the order it was +93.0 s. The 28 "VRF console [4] ?" rows are the five AGGREGATES inside
one 0.1 s window at creation (a transient name-resolution gap), never again. The watchdog's first live use was
clean (armed 15:43:48.8Z, two-observation confirm, stood down without touching anything). The concurrent
C:\MAK scan ended ~4.5 min before the first mesh query, against a back-end then holding 0.10-0.17 cores and a
flat working set; no instrument shows an effect, though it could only have LENGTHENED the nav load, which is
conservative for the settle-margin conclusion.
