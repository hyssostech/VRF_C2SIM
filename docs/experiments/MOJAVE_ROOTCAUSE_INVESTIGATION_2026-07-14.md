# Mojave aggregate-movement root-cause investigation (SUPERVISED)

Status doc for the multi-agent dig into WHY unit/aggregate movement freezes at the
COA-STP1 Mojave region while it works at the golden Sweden region. ASCII-only. Keep
current AS findings land (living-handoff discipline). Source of truth over recollection.

Started 2026-07-14. Supervisor: Opus 4.8 session (driving + adjudicating Opus executors).

## Objective

Root-cause the Mojave freeze so that a CLASS of coa-gpt-generated COAs (arbitrary
region, not just Sweden) become executable at the gold-run level. Deliver ONE of:
  (1) a code/config fix that makes unit leader-path planning succeed at arbitrary
      regions on the online whole-earth terrain, OR
  (2) a proven, telemetry-CLEAN fallback (SubordinateFanOut without the F1 runaway /
      F2b vacuous-completion warts) PLUS a coa-gpt region-validation gate,
whichever the evidence shows is achievable. The supervisor adjudicates toward whichever
the mechanism supports; both count as "make the class executable".

## The single load-bearing fact (R9, verified 2026-07-13)

At Mojave, the VR-Forces backend logs (3x per aggregate):
  `<unit>: moveAlong() - empty route -- not sending move along to subordinate`
and creates ZERO member "Offset Route" objects. The Sweden control (SAME code, SAME
day, SAME 20x, SAME short golden routes) creates 45 offset routes and completes 3/3.
So the unit's LEAD-FOLLOW controller leader-path plan comes back EMPTY at Mojave.
Entity move-along (non-unit) WORKS at Mojave (entities drive their routes) - so the
failure is specific to the disaggregated-unit path-planning path, NOT basic movement.
Evidence: docs/experiments/R9_region_swap_2026-07-13.txt; UNIT_MOVEMENT_RESEARCH.md sec 4c.

## Already FALSIFIED - do NOT re-chase (each cost a prior session)

- NAV DATA / nav mesh: Sweden marches with NO nav mesh; neither region has nav data.
  (navdata_FALSIFIED_bogaland_vs_tt_2026-07-14.txt)
- TERRAIN PAGE-IN area: forcing a page-in area did not unfreeze aggregates (apps 3378-3379).
- FORMATION NAMES: E1 falsified; R5 auto-formation repairs the birth "column-left" 3/3 at Sweden.
- STACKED / identical coordinates: R8 live A/B falsified pile-size as the blocker.
- ROUTE LENGTH / geometry: R9 transplanted the SAME ~1 km golden routes to Mojave and
  STILL got empty offset routes (0 vs 45). Length is controlled-out.
- 20x TIME MULTIPLIER and CODE DRIFT: the same-day Sweden control (3/3) excludes both.

## Key disk assets (this is what makes THIS pass different)

The full MAK VR-Forces 5.0.2 source-adjacent tree is on disk and greppable:
  C:\MAK\vrforces5.0.2\include\  (ctdb, dted, terrain, vrfmodel, vrftasks, vrfcontrol, ...)
  C:\MAK\vrforces5.0.2\makLua\  \data\  \appsrc\  \doc\help\  \doc\luadoc\
Prior sessions derived hypotheses from a mental model and tested them live (burning
appNos). This pass grounds the mechanism in the vendor's ACTUAL implementation +
external evidence FIRST, then designs ONE single-variable live probe.

## Investigation threads (parallel, read-only, Opus executors)

- Thread A - MECHANISM FROM SOURCE: in include/ + makLua/, find exactly where the
  disaggregated move-along controller computes the leader path + member offset routes,
  and identify the data query/precondition whose failure yields the empty route +
  the `moveAlong() - empty route` string. Deliver the dependency chain with file:line.
- Thread B - TERRAIN-DATA DISCRIMINATOR: characterize what route-planning terrain data
  (CTDB / DTED / elevation / VR-TheWorld tiles) the online "MAK Earth Space (online).mtf"
  provides, and whether coverage/kind differs at the Sweden AO (~58.69,16.5) vs the
  Mojave AO (~34.6,-116.6). Falsifiable: the two AOs differ in a way that gates planning.
- Thread C - EXTERNAL EVIDENCE (web): MAK/ST Engineering docs + forums + release notes
  on unit/aggregate path-planning failure on online/whole-earth terrain, the
  `empty route`/`not sending move along` symptom, and the vendor-recommended config for
  unit path planning on streaming terrain.

## Adjudication protocol (supervisor)

Adversarial, not confirmatory. A finding is ACCEPTED only if: it cites primary evidence
(file:line / on-disk artifact / cited doc), separates verified from assumed, names the
single observation that would falsify it, and survives cross-check against the other two
threads. Convergence test: do A (mechanism), B (data), C (external) point at the SAME
missing precondition? If yes -> design a single-variable live probe that toggles ONLY
that precondition. If they diverge -> the supervisor re-tasks with the specific conflict.

## Findings log (filled as threads report)

- 2026-07-14 Thread A (mechanism from source) - ACCEPTED (HIGH on mechanism, MEDIUM on the
  specific trigger). Verified against on-disk 5.0.2 headers + the vrfmodel.dll string table:
  * The empty route is produced in DtDisaggregatedMoveAlongController's per-member OFFSET-ROUTE
    build (disaggregatedMoveAlongController.h processMoveAlong->beginMoveAlong->
    generateFormationRoutes->buildOffsetRoute). generateOffsetRoute is PURE GEOMETRY (no
    terrain); the only location-dependent step is the GROUND CLAMP of the generated vertices
    (descriptor ground-clamp default True) via the GENERAL terrain closestIntersection/
    terrainHeight* with a dataAvailable flag - the SAME layer entities use (NOT a separate
    CTDB). This independently RE-CONFIRMS Thread B (no special data layer) and REFUTES my
    seeded CTDB crux.
  * Entity/convoy move TOLERATES a failed clamp (DLL strings: "Setting point altitude to last
    available altitude. Movement might not succeed"); the aggregate offset-route build does
    NOT - a failed/empty clamp drops the vertex -> empty offset route -> the exact log line.
    This is the source-level entity-works / unit-fails asymmetry.
  * DtGroundAggregatePathMetric (terrain-sampled aggregate A* planning) is DEAD CODE in 5.0.2
    - so "leader path planning" is a misnomer; it is offset geometry + ground clamp, not A*.
  * THE ACTIONABLE, INTERFACE-SIDE TRIGGER: our app hands ground route waypoints a FIXED 100 m
    MSL (VrfC2SimService.cs:689 point0, :723 route points - VERIFIED by the supervisor). 100 m
    is a sea-level assumption: fine at Sweden (terrain ~50-110 m, waypoint at/above ground),
    but at Mojave (terrain ~1100 m) every waypoint is ~1000 m UNDERGROUND -> the degenerate
    input the aggregate ground-clamp rejects where the continuously-clamping entity mover
    shrugs it off.
  * LOAD-BEARING UNKNOWN (honestly flagged by A): whether closestIntersection is bidirectional
    (up+down; would TOLERATE below-ground -> altitude NOT the cause) or downward/delta-limited
    (below-ground fatal). The .cxx bodies are compiled-only; resolvable in ONE live probe.
  SUPERVISOR CAVEAT (adversarial): a KNOWN HOLE - R9 telemetry shows a Sweden unit at terrain
  110.4 m marched with 100 m waypoints (10 m below ground tolerated). So "below-ground -> empty"
  is not clean; it would need a magnitude/fraction threshold (all-vertices-1000 m-below at Mojave
  vs one-vertex-10 m-below at Sweden). Plausible but unproven -> the live probe must settle it,
  not assumption.

## The experiment (single-variable live A/B, on the currently-loaded TropicTortoise/Mojave terrain)

Environment (preflight 2026-07-14, all green): loopback 18 ms, REST 200, license valid to
2026-09-15, vrfSim/rtiexec healthy, scenario TropicTortoise (Mojave) on MAK Earth Space (online),
"No feature layers found" (confirms B), page-in area PRESENT at the AO (so terrain-paging is
controlled OUT - the AO is paged in, yet aggregates freeze).

Code: added Vrf:GroundWaypointAltitudeMode ("Fixed100" default = byte-parity | "Live" =
live ground altitude + Vrf:GroundWaypointLiveClearanceMeters, default 50). Build 0 errors; all
8 offline selftests GREEN (parity default holds). VrfSettings.cs + VrfC2SimService.cs:689/723.

Design (whole-earth terrain is global -> Sweden and Mojave inits run against the SAME load):
- Phase 1 (control A): R9_Mojave init+order, ClientId=STP, 20x, AggregateFormation=auto,
  SubordinateFanOut=OFF (genuine leader-path), GroundWaypointAltitudeMode=Fixed100. EXPECT the
  R9 freeze: "moveAlong() - empty route", 0 offset routes, no displacement (WatchVrf).
- Phase 2 (test B): identical EXCEPT GroundWaypointAltitudeMode=Live. Measure: member
  "Offset Route" creation count (vrfSim.log) + WatchVrf displacement. PASS if offset routes > 0
  AND the aggregate marches; FALSIFIES altitude if still 0/empty (-> pivot to fan-out fallback +
  deeper dig). Telemetry-gated (Appendix D R11 rule: completions lie; displacement is the oracle).
- Phase 3 (regression, only if B passes): Sweden (golden STP) init with Live -> confirm no
  Sweden regression.
appNos from 3386 (ledger-record each). Non-negotiables per RUNBOOK sec 0 / OPUS 0.3 (clean stop
via StopIface; never force-kill; fresh appNo each join; do NOT ResetVrf-sweep - it would delete
the scenario's page-in area; dry-run only).
- 2026-07-14 Thread B (terrain-data) - ACCEPTED, HIGH confidence. HYPOTHESIS REFUTED:
  the two AOs are terrain-data-IDENTICAL for route planning; there is NO data Sweden
  has that Mojave lacks. Verified by extracting both .scnx:
  * Bogaland2 (Sweden) and TropicTortoise (Mojave) BOTH declare terrain
    "MAK Earth Space (online).mtf" + model set C2simEx.sms - byte-identical.
  * That .earth wires ONLY low-res global elevation (one LOCAL file
    WorldElevation.dem, valid at both AOs per R9 telemetry: Mojave ~1100 m, Sweden
    ~50-110 m) + streamed imagery (NOT consumed by the planner) + a sensor layer.
    NO landcover, NO roads, NO OSM features, NO CTDB, NO DTED, NO navmesh, NO tile
    cache anywhere on C:\MAK. The road/OSM/landcover .xml includes exist but belong to
    a DIFFERENT terrain ("MAK Earth (online).earth") the scenarios do NOT use.
  * Internal control that clinches it: ENTITY move-along path planning SUCCEEDS at
    Mojave on this exact terrain (R9/R10 telemetry) - so the data is provably
    sufficient for planning there. Only the AGGREGATE lead-follow leader-path returns
    empty. If anything, the US/Mojave would get MORE data than Sweden on the ground
    variant - the opposite of the hypothesis.
  * The only lat/lon differences: elevation VALUES (both valid) + imagery tiles (unused).
  RESIDUAL handed to A: could the coarse elevation VALUES / surface at the Mojave AO
  perturb the aggregate planner? That is a planner-BEHAVIOR question, not data-availability.

  >> ADJUDICATION PIVOT (supervisor, after B+C): the root cause is NOT missing terrain
  data. Same coarse-elevation-only terrain plans the leader path at Sweden and fails at
  Mojave. This WEAKENS Thread C's "road/feature vector" mechanism (there are NO roads at
  EITHER AO, yet Sweden plans) and MOOTS the "build CTDB / add data / page-in features"
  fix family. The cause lives in the AGGREGATE LEADER-PATH CODE and its interaction with
  the specific location (coordinates / projection / route geometry / coarse-elevation
  surface) or an aggregate/formation-state precondition - Thread A owns this. IMPORTANT
  DELIVERABLE IMPLICATION: if A confirms a data-INDEPENDENT backend planner failure we
  cannot easily fix, the robust answer for arbitrary-region coa-gpt COAs is the
  SubordinateFanOut fallback (entity-level planning works anywhere entity move works,
  proven at Mojave) made telemetry-CLEAN (fix F1 runaway / F2b vacuous completion) + a
  region/entity-move validation gate - possibly MORE achievable than fixing MAK's planner.
  Cheap offline confirm available (not yet run): gdallocationinfo on WorldElevation.dem at
  both AOs to compare elevation validity/roughness (R9 already shows both valid at runtime).
- 2026-07-14 Thread C (external web) - ACCEPTED with one flagged tension. Vendor-sourced
  mechanism (moderate-high confidence, triangulated across 4 MAK pages):
  * PLANNED/route movement (what a disaggregated unit's move-along uses to lay out member
    offset routes) consumes path-planner inputs = a navigation mesh/Navigation Data +
    ROAD/RIVER/FEATURE VECTOR data + a road network; a single ENTITY move-to steers
    directly and needs only elevation. This is the entity-works / unit-fails asymmetry,
    vendor-documented in general terms (mak.com/.../capabilities; support?id=15).
  * On STREAMING osgEarth whole-earth terrain, features page in PER TILE/LOD
    (DtOsgEarthStreamedFeatureManager exposes feature nodes only "when tiles page in"),
    so feature/route data availability is LOCATION- and LOD-dependent - fits "same .mtf,
    different behavior by location".
  * VR-TheWorld: global 30 m DTED2 elevation everywhere; features are global OSM but
    HIGH-DETAIL insets exist only for named regions. Mojave desert = OSM-sparse.
  * VRF 5.2 headline "ground path planning now enhanced with vector-based terrain data"
    => path planning was historically data-sensitive; a known weak spot.
  * NO public hit on the exact log strings (proprietary); ftp.mak.com classdocs were
    DNS-unreachable from the agent - the on-point disaggregated-move-along/streamed-feature
    class pages remain unread. THE key unknown Thread C flags: is navmesh path planning
    fully supported on streaming osgEarth AT ALL, or only on compiled static/CDB/CTDB
    terrain? Likely the true root cause; lives in gated docs (VRF Users Guide, "Adding
    Content" ref manual).
  TENSION TO ADJUDICATE (supervisor): Thread C leans on "navmesh/Navigation Data", but our
  own nav-data FALSIFICATION stands (Sweden marches with NO navmesh; neither AO has nav
  data). Reconciliation candidate: the leader-path planner is NOT using a navmesh but ROAD/
  FEATURE VECTORS paged per tile - present at the Sweden AO, sparse/unpaged at Mojave. This
  does NOT contradict the nav-data falsification and must be confirmed by Thread A (what the
  planner actually READS) + Thread B (what data each AO actually has). Do NOT re-adopt
  navmesh as the cause on Thread C alone.

## Live A/B - ATTEMPT 1 ABORTED: vrfSim backend CRASHED (2026-07-14, apps 3386/3387)

Phase 1 (Fixed100 baseline, R9_Mojave full init) crashed the SIM BACKEND during unit creation.
- Setup: PushInit R9_Mojave_Initialization.xml (the full 80-unit / 49-creatable golden STP set
  TRANSPLANTED to Mojave) ON TOP of the already-loaded TropicTortoise Mojave scenario; app 3386
  (STP, 20x, auto formation, fan-out OFF, Fixed100). 49 units + 4 areas created.
- CRASH: vrfSimHLA1516e exited during/just after creation (vrfSim.log last write ~19:10 local ==
  the run window; process gone). vrfSim.log tail is GARBLED (concurrent-write crash signature)
  with repeated "DtArticulatedPartCollection: Found missing attached-to part ... part 4736 to
  4128" and "Z61/Z62/Z63/Z64.Amphib: Entity has run aground". Likely trigger: creating the full
  Sweden set - incl. AMPHIBIOUS craft that "run aground" on the Mojave DESERT + articulated-part
  vehicles - on top of an already-populated scenario destabilized the backend.
- All 3 move tasks then ABANDONED "could not read live location" (the backend was already
  dead/dying) - so the offset-route mechanism was NEVER exercised; the A/B produced NO altitude
  evidence. (Also a procedure note: the order was pushed only ~25 s after creation, too early -
  but moot here since the backend died.)
- CLEANUP (clean): StopIface drove the C2SIM server RUNNING->INITIALIZED->UNINITIALIZED; the app
  (PID 89880) caught it and RESIGNED cleanly - NO stale federate. rtiexec + vrfGui intact. No
  force-kill, no container restart. Server left UNINITIALIZED.
- BLOCKER: vrfSimHLA1516e must be RELAUNCHED to continue (backend down = STOP-AND-ESCALATE per
  OPUS 0.5). Escalated to the user.
- LESSON for retry: do NOT re-slam the full amphib-laden transplant. Use a LEAN Mojave init -
  a few GROUND aggregates only (e.g. the 3 R9 taskees 1.BdeHQ / 1222.MechPlt / 114.MechCoy), no
  amphibs, ideally on a fresh/empty scenario - and settle units (~60-90 s) before tasking. This
  also isolates the altitude variable with far less backend stress.
- appNos consumed: 3386 (app), 3387 (WatchVrf - never got clean telemetry). Next free: 3388.

## Decisions log

- 2026-07-14: user moved to the supervisor seat and directed a multi-Opus dig at this
  blocker (goal: a class of coa-gpt COAs executable, not just COA-STP1). Investigation
  is read-only until the supervisor approves a single-variable live probe at GATE-ENV.
  No live run / no appNo consumed yet (next free 3386).
