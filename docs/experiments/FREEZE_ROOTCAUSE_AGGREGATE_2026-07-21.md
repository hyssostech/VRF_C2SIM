# FREEZE ROOT CAUSE - aggregate movement model (2026-07-21)

Provenance: supervisor + a fan-out workflow, every load-bearing fact re-derived by the
supervisor from primary sources (MAK VR-Forces 5.0.2 shipped docs + stock model-set files +
the port source + the Jul-19 run artifacts). ASCII only.

## Question

Why, in the Jul-19 R9_Mojave_Lean scored runs, did 1222.MechPlt MOVE while 114.MechCoy and
1.BdeHQ FROZE bit-exact, when all three were surface-clamped and each got an identical
CreateRoute (3 pts) -> MoveAlongRoute?

## Answer (for the two AGGREGATES: 1222 moved, 114 froze) - vendor-confirmed

VR-Forces moves a unit differently depending on aggregation state and on whether the unit's
object prototype is a leaf "Unit" or a "HigherUnit" (unit-of-units). The port creates every
aggregate DISAGGREGATED (VrfC2SimService.cs:460, CreateAggregate(..., Disaggregated, true)).

Per the VR-Forces Users Guide (5.0.2), Chapter 21:
- p.498/p.500: a DISAGGREGATED unit given a movement task does NOT move itself - "each member
  of the unit plots a path and follows it"; the unit position is derived from its members.
- p.501: "When a disaggregated unit is given a task, it sends radio messages to its
  subordinates directing them to carry out their role." No movable subordinates => no motion.
- p.499/p.500: an AGGREGATED unit "only simulates movement behavior" and "the object
  representing the unit moves" as a single icon - no members required.

The two units resolve (via VRF OPD best-match on the DIS objectType the port sends) to two
structurally different stock prototypes (self-verified from the .entity/.ope files):

MOVER - 1222.MechPlt, C2SIM ArmorPlatoon (SIDC echelon 'D'), DIS 11.1.225.1.1.3.0
  -> EntityLevel/vrfSim/Ground_Aggregate.entity (broad wildcard match 3:11:1:-1:-1:-1:-1:-1)
     platform = platforms/Aggregate.ope  (display-name "Unit",
                "For unit objects, i.e. objects that have subordinate objects")
     movement = ground-disaggregated-movement.sysdef
     subordinates = 4 x objectType 1:1:1:225:4:14:0:0  = DIS Kind 1 PLATFORM ENTITIES
                    (individual vehicles that drive directly)
  => disaggregated move-along drives the 4 leaf vehicles -> unit MOVES.

FROZEN - 114.MechCoy, C2SIM ArmorCompany (SIDC echelon 'E'), DIS 11.1.225.5.2.0.0
  -> EntityLevel/vrfSim/'Tank Company (USA).entity' (specific match 3:11:1:225:5:2:-1:-1)
     platform = platforms/HigherAggregate.ope  (display-name "HigherUnit",
                "Objects of this type contain other units as subordinates")
     movement = ground-higherUnit-disaggregated-movement.sysdef  (a DIFFERENT sysdef)
     subordinates = 3:11:1:225:14:2:1:0 (HQ) + 3 x 3:11:1:225:3:2:0:0 (TANK)
                    = DIS Kind 11 sub-UNITS (aggregates), not leaf entities
  => a unit-of-units whose move must cascade through a multi-level subordinate hierarchy that
     the create path never populates (the C2SIM subordinates 1141/1142/1143.MechPlt were
     dispatched as SEPARATE top-level units, not as members of the company) -> unit FREEZES.

Dispatch is by SIDC echelon char only (UnitTranslator.Plan:45-66): 'D'->ArmorPlatoon,
'E'->ArmorCompany, 'F'->ArmorCoHQ, else->Tank ENTITY. So every company-echelon unit lands on
the HigherAggregate and freezes; every platoon lands on the leaf Ground_Aggregate and moves.

## Ruled out (both verified this session)
- Birth altitude / underground: falsified - both froze ON the terrain surface (10000 MSL birth
  already active in the scored runs; see CORRECTIONS_LOG "Birth altitude").
- Formation case ("column" vs "Column"): not the cause - formation was OFF (golden parity) in
  the scored runs; AutoFormationFor is superseded/never-read; the live matcher is
  case-insensitive. The case difference merely REFLECTS the two OPDs' real formation names
  (Ground_Aggregate: lowercase line/column/wedge/vee; Tank Company: Title-Case).

## STILL OPEN - 1.BdeHQ (the frozen ENTITY)
1.BdeHQ maps (echelon 'H' -> default) to a plain Tank ENTITY, not an aggregate, so the
aggregate mechanism above does NOT explain it. A lone entity is documented to march at Mojave
(R10). Its bit-exact freeze is a SEPARATE, still-unexplained defect. The confirming fix-run
(below) will isolate it: if the aggregate fix makes 114 move but 1.BdeHQ still freezes, it is
cleanly a distinct entity-level bug to chase next.

## Proven vs inferred
- PROVEN (files + manual): the two OPDs' structure (leaf-entity vs sub-unit subordinates, two
  movement sysdefs); that disaggregated units delegate movement to subordinates while
  aggregated units move as one object; that the port creates disaggregated with the company's
  C2SIM subordinates dispatched as separate units.
- INFERRED (to be confirmed by the fix-run or a live GetAggregateMembers count): that the
  HigherAggregate's subordinate hierarchy is not populated/does not cascade, hence zero motion,
  rather than being populated-but-otherwise-stuck.

## Fix options (documented; a user decision - ties to the open "aggregation-state policy")
1. Create units AGGREGATED (flip VrfC2SimService.cs:460 Disaggregated -> Aggregated). The unit
   moves as one icon; no subordinate hierarchy required. One-line, single-variable. Tradeoff
   (Users Guide p.499): an aggregated unit models movement only, not combat/attrition. Best fit
   for the current headless movement+telemetry goal. RECOMMENDED as the first test.
2. Map company/higher echelons to the leaf Ground_Aggregate type (as the platoon is). Keeps
   disaggregation + members that move, but discards the correct company OPD identity/structure.
3. Keep disaggregated and properly instantiate the HigherAggregate's subordinate units as VRF
   members. Most faithful, most work.

## CORRECTION + REVISED PLAN (2026-07-21, later same day)

The clean "leaf-aggregate moves / HigherAggregate freezes" story above is OVER-CONFIDENT and
was partly falsified by this session's own adversary pass. Corrections:

1. NO unit demonstrably executed its route. The Jul-13 R9 log shows
   "moveAlong() - empty route -- not sending move along to subordinate" for BOTH 1222 AND 114
   (R9_region_swap_2026-07-13.txt:34-35): VRF's aggregate LEADER-PATH PLANNER returned an empty
   route for the leaf aggregate too, so 1222 did not route-follow either.
2. The "1222 moved" evidence rests on the POS/WatchVrf channel, which the resume brief flags as
   MISREPORTING this unit (POS: ~63 m WEST, away from its eastward objective; RPT: EAST toward
   it). The POS-vs-RPT oracle contradiction is UNRESOLVED, so "moved vs froze" is not a clean
   fact. 1222's small displacement is more likely a one-time spawn-settle / member form-up than
   route execution.
So: the type/OPD mapping (leaf Ground_Aggregate vs HigherAggregate vs bare entity) is a REAL
structural input that selects the movement controller, but it is NOT proven to be the
sufficient cause of the outcome. The shared, still-open blocker is that the aggregate
leader-route comes back EMPTY at these AOs, so nothing is forwarded to subordinates.

### Verified facts that DO stand
- Port dispatch is by SIDC echelon char only (UnitTranslator.Plan:45-66); it sends WRONG DIS
  types: ArmorPlatoon -> 11.1.225.1.1.3.0 (a DEAD aggregate branch -> generic Ground_Aggregate
  fallback). The REAL stock "Tank Platoon (USA)" is 11.1.225.3.2.0.0 = VehicleAggregate.ope with
  4x 1:1:1:225:1:1:3:0 M1A2 tank ENTITIES + combat components (spot-report, weapon-list). VRF's
  OPD catalog supplies the platform-level TO&E the C2SIM TO omits, keyed by DIS type.
- Port creates every unit FLAT (VrfC2SimService.cs:454-463): it reads the C2SIM Superior link
  only for a missing-coords fallback (InitParser.cs:105, InitModels.cs:21) and never rebuilds
  the VRF hierarchy, so a company's C2SIM platoons become independent siblings, not members.
- Aggregated state = movement only, no combat (Users Guide p.499). Combat/attrition needs the
  disaggregated state with real member entities (which the stock OPDs provide).

### REVISED APPROACH (user-directed 2026-07-21): learn from a clean working scenario first
Stop debugging the broken C2SIM path in the dark. Build a MINIMAL known-good scenario that
follows the docs, confirm it actually moves, learn the correct recipe, THEN apply it to
COA-STP1 (coa-gpt1).
- Phase A (offline): extract the documented create/task/route recipe (Ch 23 Creating and
  Controlling Units; the Move Along Route task; road/off-road movement p.568; aggregation
  control) and the conditions that make an aggregate leader-route come back EMPTY (waypoint
  below terrain? unreachable? on/off-road?). Anchor on the real Tank Platoon (USA) OPD.
- Phase B (one live run, gated): create ONE stock Tank Platoon (USA) (DIS 11.1.225.3.2.0.0) at
  a valid Mojave spot, disaggregated + subordinates, task a SHORT VALID route (waypoints above
  terrain, reachable, near the unit), observe whether its 4 M1A2 tanks move. Single variable:
  correct type + valid route. If it moves -> recipe learned; if leader-route still EMPTY ->
  the blocker is route/terrain/planner, debugged on a clean baseline.
- Phase C: apply the recipe to coa-gpt1 - correct type mapping (C2SIM type -> real VRF
  standard-unit OPD), rebuild hierarchy from the C2SIM TO, ensure valid route waypoints; run
  and score.

The "supplement the TO to platform level" answer: the platform level comes from VRF's OPD
TO&E, selected by mapping each C2SIM unit to the CORRECT standard-unit type; you do not author
platforms in C2SIM. Supplementation needed = correct type match + hierarchy wiring. Only truly
non-standard org structures (no stock OPD) would need a custom OPD or an external MTOE table.
