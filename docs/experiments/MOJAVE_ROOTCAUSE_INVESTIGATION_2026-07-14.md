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

## Experiment matrix (region x scenario/order) - recap 2026-07-15

Two terrain wrappers, Bogaland2.scn (Sweden, ~58.7,16.4) and TropicTortoise.scn (Mojave,
~34.6,-116.6), sit on the IDENTICAL underlying whole-earth streaming terrain (`MAK Earth
Space (online).mtf` + `C2simEx.sms` model set - confirmed byte-identical by unzipping both
.scnx). Two unit/order pairings exist: golden (80 units/49 creatable, STP init + the 3-task
R5 move order) and COA-STP1 (its own 128-unit init + the 42-task coa-gpt order), natively
tied to different coordinates. Only 3 of the 4 cells have actually been run:

|                          | Bogaland2 (Sweden)                | TropicTortoise (Mojave)          |
|--------------------------|------------------------------------|-----------------------------------|
| Golden set + order       | WORKS. Native pairing. R5 (2026-07-12): 3/3 TASKCMPLT. Reconfirmed as the "Sweden control" in R9 (2026-07-13, same code/day/settings as the Mojave run at right): 3/3 in ~4 min, 45 member Offset-Route objects created. Reconfirmed again 2026-07-14 (semantic Units 2/4/5, SubordinateFanOut OFF - genuine leader-path, no fan-out fallback, no nav data): 14.MechBn 5318 m, 1222.MechPlt 5634 m, 1.BdeHQ 9016 m. | MOSTLY FREEZES. R9 region-swap (2026-07-13): golden unit set/routes coordinate-transplanted onto Mojave (ground geometry preserved). 1/3: only the entity control (1.BdeHQ) completed its ~1.16 km route; the platoon moved 8 m total and froze; the company moved 410 m the wrong direction and froze. ZERO member Offset-Route objects created (vs 45 at Sweden) - `moveAlong() - empty route` is the smoking gun. This is the controlled A/B isolating *location* as the variable (same code, same day). |
| COA-STP1 set + order     | NOT RUN. No reverse-transplant experiment (COA-STP1's own units at Sweden coordinates) was ever conducted - untested, not falsified. | FREEZES / MIXED, several configurations, native pairing. Bare (R5c/R6): 0/6 aggregates marched, control-only. + de-stack (R8, 2026-07-13): still 0/6 - companies instead RAN AWAY 31-124 km past their routes, CoHQs scattered 76-93 km, platoons shuffled ~60 m (stack hypothesis falsified). + de-stack + auto-formation + SubordinateFanOut (bypass, not a fix): 5/7 unit completions on a 7-task probe (2026-07-13). Full 42-task scale run under the same bypass (2026-07-13): pipeline holds at scale, movement integrity mixed - F1 runaway under fan-out (one unit drove 53.8 km, 18.4 km past route end), F2/F2b vacuous completions (TASKCMPLT fires with zero displacement), F3 timeout-race (fixed by the F3 probe's config tune, orchestration only, does not touch movement quality). |

What this rules in/out: code/day/multiplier excluded (R9's Sweden-vs-Mojave run was same
code, same day, same 20x, only location differs); nav data excluded (Sweden marches with
zero nav data; generating nav data at Mojave did not fix it either); terrain page-in
excluded (forcing the AO to page in did not unfreeze aggregates); stacked coordinates
excluded as the SOLE cause (R8 de-stacked COA-STP1 and still got 0/6 - a different failure
mode, runaway, appeared instead). The one clean, reproducible discriminator across every
cell: member Offset-Route count - 45 at Sweden (golden), 0 at Mojave (golden-transplant AND
native COA-STP1) - the aggregate leader-path/offset-route builder returns empty specifically
at the Mojave location, on identical terrain data. `GroundWaypointAltitudeMode` (below) is
the current probe of WHY. The only proven Mojave mover is SubordinateFanOut - a bypass
(member entities, standard nav) that works AROUND the empty-offset-route problem rather than
fixing it, with known integrity warts (F1/F2b) under load.

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

## Tier-1 reverse-transplant RESULT (2026-07-15, LIVE, apps 3401-3406): COA-STP1's OWN units
## do NOT march even at the golden Sweden region - the freeze is NOT purely a region effect

Completes the matrix cell flagged as "NOT RUN" in the 2026-07-15 recap (see the matrix
section above). data/COA-STP1_Sweden_Initialization.xml + data/COA-STP1_Sweden_MinimalOrder.xml
(2 of COA-STP1's OWN units, AD/7152 platoon + 3/7159 company, coordinate-transplanted onto
Sweden per the R9-inverse transform - see those files' own comments) pushed and tasked with
a plain self-move MOVE (bare CreateRoute + MoveAlongRoute, the SAME genuine leader-path
mechanism R9 tested - AggregateFormation=auto, SubordinateFanOut OFF, no de-stack, 20x, on
the user-launched Bogaland2/Sweden backend).

RESULT: NEITHER unit marched, telemetry-verified (WatchVrf, 240 s / 15 s samples, R11 rule):
- AD/7152 (platoon): reported a REAL TASKCMPLT ("VRF task complete: AD/7152 / move-along")
  but WatchVrf shows a DEGENERATE (0,0,-6378137) position for the ENTIRE 228 s window - never
  resolved to a real geodetic location at all. A vacuous completion, the same class as R11's
  DtPlanAndMoveToTask trap and the scale run's F2 (1-1/2/1_AD) - except here it is the PLAIN
  moveAlongRoute path lying, at the GOLDEN region, not Mojave.
- 3/7159 (company): held a REAL resolvable position (58.5247, 16.7328) but drifted under 1 m
  over 210 s (noise-level, not movement) toward its ~1100 m destination - frozen, no TASKCMPLT
  ever sent.
- Both units WERE created + formation-repaired cleanly first (query -> set 'column' ->
  reorganize, the R5 mechanism) - so this is not a creation or formation-state failure; it is
  specific to the move/leader-path dispatch, same symptom class as Mojave.

ADVERSARIAL CHECK (falsification): is this a fluke of these 2 specific transplanted
coordinates rather than the units themselves (e.g. bad micro-terrain at 58.71,16.45 or
58.53,16.73)? Not excluded with certainty, but weak - both points sit inside/near the broader
AO where golden units are repeatedly proven to march (R9's own footprint ~58.68-58.75 N,
16.32-16.52 E covers the AD/7152 anchor; 3/7159 is ~20 km SE, still ordinary Sweden inland
terrain, not a documented water/obstacle feature). No prior session has EVER produced a
genuine (non-fan-out, telemetry-verified) COA-STP1 aggregate moveAlongRoute completion at
ANY region (R5c 0/6, R8 0/6 - runaway instead; the scale run's completions all went through
SubordinateFanOut, a different mechanism) - so this result is consistent with, not falsified
by, everything observed so far.

LEADING REFINED HYPOTHESIS (not yet proven - the next thing to check): SISOEntityType (DIS
type). Checked directly against both source files: golden units (R9_Mojave_Lean_Initialization.xml)
ALWAYS carry a real DIS type (DISKind=11, DISCategory 3 or 5, all 6 units). COA-STP1's ENTIRE
dataset (data/COA-STP1_Initialization.xml, all 537 SISOEntityType instances, checked via grep,
not sampled) carries DISKind=0/DISCategory=0 (unspecified/wildcard) - uniformly, not just
these 2 units. This is a clean, 100% categorical difference between the two datasets, and a
plausible mechanism: VR-Forces' aggregate offset-route/leader-path builder (Thread A's finding
above) needs to resolve the aggregate's MEMBER vehicle models to compute formation-offset
geometry; an unspecified DIS type may fail to resolve a real member model in C2simEx.sms,
leaving the aggregate creatable and formation-commandable (both succeeded) but with no real
geometry to plan a leader path against - a degenerate/frozen result regardless of region. This
would NOT contradict R9 (which used golden units, always real DIS types, at both regions) -
it would mean TWO separate blockers exist: Mojave's region-specific empty-offset-route effect
(still real, still unsolved, still the cause for GOLDEN units transplanted there) PLUS a
COA-STP1-data-specific zero-DIS-type effect that blocks genuine aggregate movement independent
of region. NOT YET TESTED: a COA-STP1 unit WITH a real DIS type, if any exist in the dataset
(none found in the entities checked so far - the zero-DIS-type property looks dataset-wide,
which would make this hard to isolate via unit selection alone and might need a synthetic
DIS-type override for a probe instead).

PRACTICAL IMPLICATION: `GroundWaypointAltitudeMode=Live` (the altitude-mode probe, still
untested live - blocked on TropicTortoise's crash instability, RUNBOOK sec 0.5) may fix
golden/real-DIS-type units at Mojave without helping COA-STP1's own units at all, if the
DIS-type hypothesis holds - the two problems would need two separate fixes for coa-gpt
scenarios (which generate COA-STP1-shaped data) to work end-to-end at arbitrary regions.

Evidence: appNos 3401-3406 (dry-run/init-push-fix/app/order-fix/watch/post-sweep);
docs/RUNBOOK.md sec 0.6 (the two XML-comment gotchas hit + fixed en route:
prolog comments break init push; ANY block comment breaks order STOMP delivery).

## DIS-type hypothesis FALSIFIED (2026-07-15, LIVE, apps 3407-3410)

Direct test of the leading hypothesis from the Tier-1 result above: data/COA-STP1_Sweden_RealDIS_Initialization.xml
is IDENTICAL to data/COA-STP1_Sweden_Initialization.xml (same 2 units, same transplanted
Sweden coordinates, same SIDC/EchelonCode/UUIDs/ForceSides, same order) EXCEPT each unit's
SISOEntityType was swapped for a real, valid DIS type borrowed from the matching-echelon
GOLDEN unit (AD/7152 <- 1222.MechPlt's type; 3/7159 <- 114.MechCoy's type). Single variable
changed. Pushed + tasked identically (AggregateFormation=auto, SubordinateFanOut off, 20x).

RESULT: IDENTICAL failure pattern to the zero-DIS run, telemetry-verified (WatchVrf, 228 s):
AD/7152 again reported a vacuous TASKCMPLT while sitting at a degenerate (0,0,-6378137)
position for the ENTIRE window; 3/7159 again held a real position but drifted under 1 m
(58.5277,16.7329, essentially the same resting spot as the zero-DIS run). A real DIS type
alone does NOT rescue genuine aggregate leader-path movement for these units.

FALSIFIES: the "unspecified SISOEntityType blocks member-model resolution" mechanism as
sufficient on its own. Does NOT rule out DIS type as PART of a combined precondition (see
below) - only that it is not independently sufficient.

NEXT CANDIDATE (not yet tested): FORCE SIDE / hostility. The one remaining categorical
difference between the golden units (always Side=NATO Coalition, blue, SIDC prefix "SF...")
that march, and COA-STP1's AD/7152 + 3/7159 (Side=WASA, hostile, SIDC prefix "SH...") that do
not, even now with a real (but BLUE-side-native) DIS type borrowed onto a hostile-side unit.
Two clean single-variable follow-up designs, either would isolate this:
  (a) take COA-STP1's AD/7152 (real DIS type, WASA/hostile) and flip ONLY its Side to
      NATO Coalition (blue) - if it then marches, force-side (or a DIS-type x force-side
      interaction, e.g. the model set only has a resolvable model for that DIS type on the
      blue side) is implicated.
  (b) take a GOLDEN unit (e.g. 1222.MechPlt, real DIS type, blue) and flip ONLY its Side to
      WASA/hostile - if it then FREEZES, that is the cleaner, more direct isolation (known-
      good unit, one variable flipped) and would be the stronger result either way.
Design (b) is preferred - it starts from a unit already proven to march, changing exactly
one thing, rather than starting from a unit already proven not to march and changing a
second thing on top of the DIS-type change that already failed to help.

## 2026-07-15 SESSION SYNTHESIS - where this actually stands (read this section first)

This session ran DIS-type (falsified above), skipped force-side (user judgment call - "units
of any type should be taskable", not tested, not falsified, still on the table if wanted),
then hit and root-caused a real infrastructure problem before ever getting a clean altitude
result. In order:

**The vrfSim crash saga (found + root-caused, ~half this session).** Every attempt to launch
VR-Forces headlessly via `vrfSimHLA1516e.exe` directly (RUNBOOK sec 0.5's CLI recipe) produced
a backend that crashed remote-controller clients (ResetVrf, the app) with `0xC0000005` inside
`VrfFacade::Tick()`, 3 times, always at TropicTortoise, never at Sweden. Many false leads were
chased and each was cleanly falsified with evidence, not assumption: NOT a missing FOM module
(the GUI's own saved connection profile uses the identical 3-module set for both scenarios);
NOT missing/altered scenario content (TropicTortoise's `.scnx` is byte-identical to the
repo-tracked snapshot and its page-in-area object is byte-identical to Bogaland2's own, minus
expected position). The user pointed out region and LAUNCH METHOD had never actually been
isolated - every Mojave attempt was this session's own headless CLI launch, every Sweden
success was the user's GUI launch. The user then launched TropicTortoise via the GUI
themselves and a ResetVrf dry-run against it succeeded cleanly - proving the crash was never
about Mojave at all. The user's actual launch tool is `vrfLauncher.exe` (a combined front-end
+ back-end orchestrator using a saved "predefined connection" profile), not a bare
`vrfSimHLA1516e.exe` invocation - RUNBOOK sec 0.5 is corrected and the headless recipe is
marked UNSAFE. **Practical consequence: there is currently NO reliable way for an agent to
launch VR-Forces independently. A human must launch it via the GUI/vrfLauncher until a
`vrfLauncher.exe`-based headless recipe is worked out (untested candidate:
`vrfLauncher.exe -B --usePredefinedConnection "HLA 1516 Evolved RPR 2.0 with MAK extensions"
--simArgs <args>` - "HLA 1516 Evolved RPR 2.0 with MAK extensions" is the exact saved
connection-profile name confirmed on-screen (starred/default in the user's Simulation
Connections Configuration dialog, FED file RPR_FOM_v2.0_1516-2010.xml, the same 3 FOM modules,
back-end appNumber 3001 / front-end 3101) - the profile NAME is verified, the vrfLauncher
INVOCATION syntax is NOT yet tried live.**

**The altitude probe (finally run, twice, both INCONCLUSIVE).** With a stable GUI-launched
Mojave backend, `Vrf:GroundWaypointAltitudeMode=Live` was tested at
`GroundWaypointLiveClearanceMeters=0` then `=50` (data/R9_Mojave_Lean_Initialization.xml +
data/R9_Mojave_UnitMove_Order.xml, the same R9 taskee set: 1.BdeHQ entity, 1222.MechPlt
platoon, 114.MechCoy company). Neither run produced ANY telemetry-confirmed movement for ANY
of the 3 units. Clearance changed whether the aggregates' positions resolved to real
coordinates (degenerate 0,0 at clearance=0; real coordinates at clearance=50) - a genuine,
if partial, change - but neither clearance value produced actual displacement, and BOTH runs
show something new and more alarming: **1.BdeHQ - the same entity that drove its full ~1.16 km
route to completion in R9 Run A (2026-07-13, the one and only prior clean Mojave test where it
was genuinely tasked and the backend was healthy throughout) - was completely frozen (zero
displacement) in BOTH of today's runs**, despite confirmed task dispatch (CreateRoute +
MoveAlongRoute logged) and a confirmed-running sim clock (user visually verified the GUI clock
advancing). (The other prior Mojave attempt where it did not move - the 2026-07-14
"ATTEMPT 1 ABORTED" backend crash - is NOT a counterexample to R9's success: tasking never
reached dispatch there at all, "could not read live location", a different and already-
understood failure. So the honest precedent is: one clean prior success, now one clean
failure under a config that had never been tried together before - not a large body of broken
precedent, but also not nothing.) This was never explained. **This means the altitude
hypothesis is STILL UNTESTED by a clean signal** - something broader than altitude may be
suppressing task execution in this specific environment (first time this session's own app has
ever actually TASKED anything against a vrfLauncher-launched backend - object creation and
discovery were already proven fine there, but movement execution never was tested before
today).

**TOP PRIORITY FOR THE NEXT SESSION (user directive, 2026-07-15):** before resuming any
further hypothesis-chasing, read VR-Forces' own basic "Creating a Scenario" / getting-started
documentation (`C:\MAK\vrforces5.0.2\doc\help\Content\Scenarios\CreateRun\vrf_createScenario.htm`
is the entry point found this session - covers the New Scenario wizard, required settings like
"Create Global Dynamic Terrain Processor", terrain/SMS selection, etc.). The user's standing
concern, stated explicitly: three days were spent chasing this, and it is unlikely there is no
public documentation for beginners describing the configuration required to get a scenario to
actually work - meaning it is plausible the remaining mystery (entity movement frozen under
vrfLauncher, or the original Mojave freeze itself) is a basic, documented scenario-setup
requirement this investigation has not yet checked, not a deep API-level bug. Do this reading
pass BEFORE the missing-control re-test below, and BEFORE any further live experimentation -
it may make some of the remaining investigation unnecessary.

**THE MISSING CONTROL, and the clean next step (after the 101 pass above):** this session
never re-ran the ORIGINAL,
historically-proven config (`GroundWaypointAltitudeMode` unset/`Fixed100`, i.e. golden-parity
moveAlongRoute exactly as R9 ran it) against THIS vrfLauncher-launched Mojave backend. Without
that same-session control, it is impossible to tell whether the entity-freeze is a NEW,
environment-specific problem (would also break Fixed100) or something specific to the Live
altitude-mode code path. Per this whole investigation's own methodology (R9 always paired a
same-day control), THE FIRST THING a fresh session should do before any further altitude work
is: push the SAME init/order with `GroundWaypointAltitudeMode` at its default (Fixed100),
against a GUI/vrfLauncher-launched Mojave backend, and check via WatchVrf whether 1.BdeHQ
moves. If it does NOT move either, the entity-freeze is environment-specific (not
altitude-code-specific) and needs its own root-cause dig before the altitude A/B can produce
a meaningful signal. If it DOES move, that isolates the freeze to the Live-mode code path
specifically, and the clearance=0-vs-50 comparison can be trusted after all.

**Also landed, useful independent of the above:** RUNBOOK sec 0.6 documents two real
XML-authoring gotchas found while pushing today's Sweden experiments (a comment in the XML
prolog silently breaks init pushes against the real C2SIM server; ANY block comment breaks
order delivery over STOMP) - read before authoring any new `data/*.xml` file.
`tools/PushInit` gained a `--verbose` flag that surfaces the SDK's own discarded trace-level
server response - use it whenever a push fails with only a generic error.

## 2026-07-15 (fresh session, part 2) - ROOT CAUSE FOUND: our units are never properly defined
## VR-Forces objects - they fall through to a generic, content-free "Aggregate" shell

**This supersedes/reframes the ground-clamp and freeze-movement hypotheses below (both stay
plausible as compounding factors, neither is retracted, but neither is the foundational issue).**
User pushback (2026-07-15, later same day): stop inventing mechanism theories from symptoms;
read VR-Forces' own docs on how scenarios/entities are actually supposed to be built, and check
our data against that. This section is the result - grounded in VR-Forces' own documented
object-matching algorithm plus direct inspection of the on-disk OPD (Object Parameter Database)
files our scenarios actually load. Every claim below is a VERIFIED file/doc fact, not a symptom-
based inference; the mechanism theories elsewhere in this doc were built by reverse-engineering
observed behavior, this one was built by reading the specification our data is checked against.

**The documented mechanism (VERIFIED, `SimulationModels/ObjectParameterDatabase/ObjectTypes.htm`):**
VR-Forces object creation uses an 8-field enumeration (super-type + the 7-field DIS SISOEntityType).
Each `.entity` file in the OPD declares an exact `objectType` (published) and a `matchType`
(may contain -1 wildcards). "When the back-end receives a request to create a simulation object
and it cannot find an EXACT match... it finds the BEST match... among matching object types."
Fields are matched left-to-right; a non-wildcarded field that differs is a NON-match, forcing
fallback to a LESS specific (more-wildcarded) ancestor entry. This is documented with a worked
example in the doc, not inferred.

**What our translator actually sends (VERIFIED, UnitTranslator.cs + the frozen C++ oracle
C2SIMinterface.cpp, faithfully ported except where noted):** every C2SIM unit is routed by a
single SIDC echelon-letter switch to ONE of 5 synthetic "aggregate" DIS type codes
(Kind=11 = VR-Forces' own "Disaggregated unit" super-type marker, not a real DIS platform kind):
ScoutUnit (echelon B, friendly), ArmorPlatoon (D), ArmorCompany (E), ArmorCoHQ (F, "battalion ->
Co HQ" per the code comment), MobileIrregular (B, hostile). These 5 codes are meant to stand in
for whatever the unit ACTUALLY is (MechBn, MechCoy, MechPlt, BdeHQ, CoHQ, AD platoon, etc.) -
there is no branch/composition-aware mapping, only echelon size + hostility.

**What the OPD our scenarios load (C2simEx.sms -> includes EntityLevel.sms -> includes
base.sms - VERIFIED, all 3 read directly) actually defines for Kind=11 ground units:**
grepped every `.entity` file's `objectType`/`matchType` across all three SMS directories
(`C:\MAK\vrforces5.0.2\data\simulationModelSets\{C2simEx,EntityLevel,base}`). Result, field
order Kind:Domain:Country:Category:Subcategory:Specific:Extra:
- **ArmorPlatoon** `Spec(11,1,225,1,1,3,0)` - Category=1 appears in ZERO EntityLevel-scope
  `.entity` files (checked every one). No match, no wildcarded ancestor beyond the fully-generic
  root. Falls all the way to the generic fallback.
- **ArmorCompany** `Spec(11,1,225,5,2,0,0)` - Category=5 entries exist (Subcategory 0, 3, 14,
  20) but none at Subcategory=2. No match. Falls to the generic fallback.
- **ArmorCoHQ** `Spec(11,1,225,5,20,0,0)` - a REAL template exists,
  `EntityLevel\vrfSim\aggregate-Company-HQ-Friendly.entity`, matchType
  `3:11:1:225:5:20:1:0` (no wildcards) - but its Specific field is 1, ours is 0. Per the
  documented exact-match rule this is a NON-match. Falls to the generic fallback.
- **ScoutUnit** `Spec(11,1,225,2,1,1,0)` in our port vs `DtEntityType(11,1,225,14,30,0,0)` in
  the frozen C++ oracle (`C2SIMinterface.cpp:1055` local var, actual create call
  `:1065` `EntityTypeSpec{11,1,225,2,1,1,0}`- CORRECTION, see below) - **this one is a genuine
  PORTING TRANSCRIPTION BUG independent of the OPD question**: re-checked directly,
  `C2SIMinterface.cpp` line 1065's actual `facade()->CreateAggregate` call already uses
  `{11,1,225,2,1,1,0}`, matching our port exactly - the `DtEntityType(11,1,225,14,30,0,0)` at
  line 1055 is DEAD/unused local code (a leftover `DtObjectType oType` never passed anywhere).
  So ScoutUnit's live values ARE faithfully ported after all; the dead code is a red herring
  from an earlier refactor, not a bug - CORRECTED from an earlier draft of this section that
  mis-attributed a porting regression here. Category=2 does not appear in EntityLevel scope
  either way. Falls to the generic fallback regardless.
- **MobileIrregular** `Spec(11,1,0,13,34,0,1)` - EXACT match,
  `C2simEx\vrfSim\Mobile Irregular.entity` (`matchType="3:11:1:-1:13:34:0:1"`). This ONE
  resolves to a real, specific template.

**What "the generic fallback" actually is (VERIFIED, read both files):**
`base\vrfSim\base-sim-aggregate.entity` (`objectType`/`matchType` = `3:11:-1:-1:-1:-1:-1:-1`,
i.e. "any Kind=11 ground-ish unit") points at platform `Aggregate.ope`
(`EntityLevel\vrfSim\platforms\Aggregate.ope`). That file is a bare parameterized template:
`(formation "Other")` (a literal, non-functional default - this is very likely the origin of
the long-observed "Column-Left invalid formation" birth state), `(echelon-level $echelon-level)`
and `(subordinate-objects $subordinates)` and `(formation-list $formation-list)` are all
UNBOUND template variables - `base-sim-aggregate.entity`'s `<simObject>` element does not set
any of them. Contrast with `Mobile Irregular.entity` (the one real match), which explicitly
supplies: `echelon-level="SQD"`, an explicit `<formations>` block with 4 real formation files
(Line/Column/Wedge/Vee), explicit `<componentSystem>` wiring for both
`ground-aggregated-movement.sysdef` and `ground-disaggregated-movement.sysdef`, and an explicit
`<subordinates>` block naming 5 real member entity types. The generic fallback has NONE of this.

**What this means:** ArmorPlatoon/ArmorCompany/ArmorCoHQ - i.e. every echelon D/E/F unit, which
is MOST of both the golden-trace population (1222.MechPlt=D, 114.MechCoy=E, 14.MechBn=F,
1.BdeHQ=F) and COA-STP1's population - has, since the ORIGINAL C++ interface (verified these 3
factories' DIS type values are byte-identical between the C++ oracle and our port - this is not
a porting regression, it is a foundational characteristic of the whole project, present in the
golden trace itself), always been created as this content-free generic shell, not a real,
MAK-authored unit definition. Everything this investigation has built to date - R5
`Vrf:AggregateFormation=auto` (queries+sets SOME formation at runtime because the shell is born
with none), R8 de-stacking, R10 SubordinateFanOut, the Live-altitude ground-waypoint mode - are
all runtime patches compensating for the fact that the underlying object was never properly
specified. Sweden "working" (3/3 completions) is the patched-up generic shell behaving well
enough at short range/small member counts; Mojave and COA-STP1 (different elevation, different
member counts/formations, larger scale) are the same fundamentally-underspecified object
breaking down in different, inconsistent ways. This is consistent with, and gives a unifying
explanation for, essentially every symptom logged in this document (empty offset routes,
degenerate (0,0) positions, vacuous completions, F1 runaways, region-inconsistent freezes) -
they all sit downstream of "the unit was never a real unit to begin with."

**Independent confirmation this isn't just theory-with-a-fancier-source:** `base.sms` (which
EntityLevel.sms includes) ships alongside a COMPLETELY SEPARATE, rich library of real,
properly-composed EntityLevel unit templates - "Armored Cavalry Platoon/Squadron/Troop",
"Artillery Battalion/Battery/Brigade", "Field Artillery Battery (USA) M109", "Air Defense
Artillery Platoon (USA)" (`3:11:1:225:3:11:-1:-1`, matchType wildcarded on the last 2 fields -
this is almost certainly what a real "AD/7152"-type unit should resolve to, and does NOT match
what our ArmorPlatoon factory sends), "Brigade Combat Team", "Aviation Assault
Battalion/Company", etc. - dozens of them, each with real formations/subordinates/systems like
`Mobile Irregular.entity`. C2simEx.sms ALSO ships 2 more of its own custom templates
(`AR Scout.entity`, `Mobile Light Infantry.entity`) that no current factory code references at
all. None of this rich library is used by the current 5-bucket echelon-letter dispatch.

**ADVERSARIAL CHECK (falsification):** the OPD best-match ALGORITHM is directly documented
(verified, quoted above with its own worked example) - this is not inferred. What is NOT yet
LIVE-verified: that VR-Forces' actual runtime behavior matches the doc exactly for OUR specific
codes (i.e., I have not watched a live vrfSim.log line naming which platform template it
resolved for e.g. 1222.MechPlt's creation). The doc is unambiguous and matches every other piece
of evidence (the generic shell's blank formation/subordinates/echelon-level cleanly explains the
"born on invalid column-left formation, offsets 0,0,0" finding from 2026-07-12), so confidence is
HIGH, not certain. The single observation that would falsify this: a live vrfSim.log (or a
debug/verbose creation log) showing 1222.MechPlt or 14.MechBn resolving to anything OTHER than
`Aggregate.ope` at creation time. This is checkable on the NEXT live run without spending an
extra appNo (just read the existing creation log lines more closely, or add a debug flag if
VR-Forces has one for OPD match tracing - not yet searched for).

**NOT YET DECIDED (holding for the user):** the fix has two shapes and they are not equivalent.
(a) Remap our factories' DIS type codes to VR-Forces' EXISTING rich EntityLevel template library
(e.g. route MechPlt-shaped units to "Mech Inf PLT US"-equivalent EntityLevel templates if one
exists at this SMS scope, AD-shaped units to "Air Defense Artillery Platoon (USA)", etc.) -
lower effort, reuses MAK-authored, presumably-correct formations/subordinates, but requires a
real branch/composition-aware mapping (not just echelon letter) and the exact EntityLevel-scope
(not AggregateLevel-scope, which is a different unused SMS) template roster needs auditing unit
type by unit type. (b) Author project-specific `.entity` templates in C2simEx (like
`Mobile Irregular.entity`) with real subordinate/formation definitions matching what C2SIM units
actually need - more control, more work, breaks byte-parity with the golden trace (which was
ALWAYS running through the generic shell, so "parity" itself may need re-examining as a goal
here). Do not pick one unilaterally - this is a real design decision with a real time cost
either way, not a one-line fix (except the dead-code cleanup, which is optional/cosmetic).

## 2026-07-15 (fresh session, part 3) - CORRECTION to part 2 + scenario-config diff:
## the "generic empty shell" claim was overstated; it converges with, not replaces, Thread A

User pushback (correctly): part 2 below drew a "root cause found" conclusion from a truncated
grep result (a 250-line pagination cutoff I did not notice) and without checking whether golden
units - which DO work at Sweden - hit the exact same code path. Both gaps are real; here is the
corrected picture after re-checking exhaustively.

**Corrected OPD match.** The earlier grep for `objectType="3:11` was silently truncated at 250
result lines by the tool and I did not re-run it to completeness. Redoing it with a targeted
wildcard search finds an INTERMEDIATE match I missed: `EntityLevel\vrfSim\Ground_Aggregate.entity`,
`matchType="3:11:1:-1:-1:-1:-1:-1"` (Kind=11, Domain=1 pinned, everything else wild). This is
MORE specific than the fully-generic root (`base-sim-aggregate.entity`,
`3:11:-1:-1:-1:-1:-1:-1`) and LESS specific than the named leaf entries - per VR-Forces' own
documented best-match algorithm (`ObjectTypes.htm`, re-verified), THIS is what ArmorPlatoon/
ArmorCompany/ArmorCoHQ/ScoutUnit actually resolve to, not the bare root. `Ground_Aggregate.entity`
is NOT content-free: it defines a real `<formations>` block (line/column/wedge/vee - lowercase),
a real (if generic/anonymous) 4-member `<subordinates>` block (`1:1:1:225:4:14:0:0` x4), and wires
`ground-disaggregated-movement.sysdef`. gui-label is literally "Ground Unit" /
gui-categories include "Empty Unit" and `gui-can-create=False` - MAK's own naming confirms this
is meant as an internal fallback template, not a real unit-specific composition, but it is a
REAL, DEFINED fallback, not an undefined shell. INDEPENDENT CROSS-CHECK that this (not the bare
root, not Mobile Irregular) is the actual match: `Ground_Aggregate.entity`'s formation names are
LOWERCASE - this matches the already-recorded 2026-07-12 live finding "live formation lists are
ALL lowercase (static .entity analysis misleads - always query)" exactly, while
`Mobile Irregular.entity`'s formations are Title-Case. The live evidence and the file now agree.

**This converges with, rather than supersedes, Thread A (2026-07-14).** Read
`ground-disaggregated-movement.sysdef` directly: it configures
`(component-type "aggregate-lead-follow-in-formation-controller") ... (ground-clamp True)` -
this IS the exact "moveAlong" / lead-follow controller Thread A identified from DLL/header
analysis as the source of `moveAlong() - empty route`, now independently confirmed from the
actual system configuration, with ground-clamp explicitly (and deliberately - it is not a
default/oversight) enabled. Three independent angles - Thread A's binary analysis, this
session's OPD/sysdef file reading, and the 2026-07-12 live lowercase-formation observation - now
all agree on the same mechanism and the same resolved template.

**Why this matters for "do golden units work at Sweden then":** golden units (1222.MechPlt=D,
114.MechCoy=E, 14.MechBn=F) hit the IDENTICAL `Ground_Aggregate.entity` fallback COA-STP1's units
do - confirmed, this is not a difference between the two datasets (SIDC echelon-letter dispatch
is the same code for both; a fresh distribution count over the FULL data, not a spot check,
shows COA-STP1 at 64 E / 26 F / 23 D / 12 no-echelon / 2 C / 1 H and golden STP at 29 E / 24 D /
14 no-echelon / 11 F / 2 H out of their respective unit populations - both dominated 80-88% by
D/E/F, i.e. both mostly hit the same fallback). So "the object was never properly defined" is
real and applies EQUALLY to both regions and both datasets - it explains the general fragility
(why formation needs a runtime patch at all, why subordinates are 4 anonymous vehicles instead
of the unit's real composition) but does NOT by itself explain Sweden-works/Mojave-fails for the
SAME template. That regional split is still best explained by Thread A's mechanism: our own
fixed 100 m MSL waypoint altitude (VrfC2SimService.cs:693) sits near/above Sweden's ~50-110 m
terrain but ~1000 m UNDERGROUND at Mojave's ~1100 m terrain, and the now-confirmed
`ground-clamp True` step on the lead-follow controller is the plausible failure point for
exactly that input.

**1.BdeHQ verified directly (not inferred) as a separate case, confirming why it always works:**
read its actual entry in `docs/golden-trace/STP-TC-small-6-12-24_Initialization.xml`:
`APP6C-SIDC = SFGPUCIZ---H---` (position 11 = 'H', Brigade), `EchelonCode = BDE`. Our SIDC
dispatch switch only handles B/D/E/F - 'H' falls to the DEFAULT branch, `Tank(u, pos, ho)`,
DIS type `(1,1,225,1,1,3,0)` - Kind=1 (a real DIS PLATFORM kind, not the synthetic Kind=11 unit
marker) - and `ObjectTypes.htm`'s own worked example for "how to build an object type
enumeration" IS `1 1 225 1 1 3 0`, its documented M1A2 Abrams illustration. 1.BdeHQ was never
going through the fragile generic-aggregate path at all; it is a real, well-matched platform
entity, which is why it has moved reliably in literally every test, at every region, throughout
this whole investigation.

**Scenario-configuration diff (user-directed re-check, not trusting the 2026-07-14 "byte-
identical" claim on faith): re-verified myself.** Unzipped both `.scnx` bundles
(`C:\MAK\vrforces5.0.2\userData\scenarios\{Bogaland2,TropicTortoise}.scnx`, each a zip of 11
files: .scn/.oob/.orb/.pln/.ovl/.sgr/.omp/.xtr/.osrx/.gui_settings) and diffed all of them after
normalizing the scenario name. Result: the top-level `.scn` (terrain database, SMS file,
time-multiplier, `auto-reorganize=False`, frame-mode, `Create_Global_Dynamic_Terrain=1`,
`Create_Global_Environment=1`, `UseDayNightModel=1`) is IDENTICAL between the two scenarios
except filenames. `.omp/.orb/.osrx/.ovl/.pln/.sgr/.spt/.xtr` are byte-identical (post name-
normalization). Only two differ, both innocuously: `.gui_settings` (a GUI layer-visibility
toggle - "Navigation Areas"/"Tactical Graphics" panel checkboxes, purely cosmetic) and `.oob`
(3 pre-placed native scenario objects with different object-identifiers and, correctly,
different geocentric positions/orientations per region - expected, not a flaw). CONCLUSION: this
independently CONFIRMS (does not just repeat) the 2026-07-14 finding - the two scenarios really
are equivalently configured at the file level; there is no overlooked scenario-setup checkbox
differentiating them. Read `vrf_scenarioParams.htm` (the full scenario-parameter reference) in
full for this pass and cross-checked `auto-reorganize` specifically against
`vrf_aggregateReorganization.htm` ("How Units Are Organized") - CORRECTING an assumption made
mid-session: `auto-reorganize` governs ECHELON-ID renumbering after a unit member is destroyed in
combat, NOT formation validity at creation. It is unrelated to the birth-formation issue and is
not a lead (both scenarios have it False regardless).

**Still open, unchanged by this correction:** why COA-STP1's own units fail even at Sweden
(force-side/hostility still the untested candidate - both datasets hit the same OPD fallback, so
that alone does not explain a COA-STP1-vs-golden split either); the 2026-07-15 altitude-probe
confound (even 1.BdeHQ froze under vrfLauncher - 1.BdeHQ is the Tank() real-entity path, so
whatever caused that is unrelated to the aggregate/ground-clamp mechanism entirely and remains
its own open question, Freeze-Movement/Start-Resume-PDU still the standing untested lead there).

**Local documentation gap, honestly noted:** could not find any local doc page describing what
`createSubordinates=true` does when the matched template's own `<subordinates>` list IS present
but generic (as with `Ground_Aggregate.entity`) vs the exact semantics of the remote-control
`createAggregate` API - this level of detail lives only in the unreachable ftp.mak.com classdocs.
The empirical fact (R9/R5 telemetry, formation-repair discovering real formation option lists,
WatchVrf sampling multiple members) is that SOME real members do get created for these generic-
fallback aggregates; whether they are the 4 generic `1:1:1:225:4:14:0:0` vehicles named in
`Ground_Aggregate.entity`'s own subordinates list, or something else entirely, has not been
checked against a live vrfSim.log/WatchVrf capture in this pass - flagged as a concrete, cheap
next check (grep existing saved evidence .txt files for member DIS types, or add it to the next
live run) rather than assumed either way.

## 2026-07-15 (fresh session, part 4) - CONCLUSIONS from the documentation-research pass (parts 2-3)

Graded by confidence. Do not treat anything below "VERIFIED" as settled.

**VERIFIED (checked directly - files, docs, or data - not inferred from symptoms):**
- There are at least TWO, probably THREE, separate bugs/gaps at play, not one. They have been
  conflated across sessions because they all present as "Mojave doesn't work."
- (1) ARCHITECTURAL WEAKNESS, universal, not region-specific: our unit factories for echelon
  D/E/F (~80-88% of both the golden and COA-STP1 populations) resolve to VR-Forces' own generic
  `Ground_Aggregate.entity` fallback, not a properly-modeled unit. It has real formations and
  real subordinates, but the subordinates are 4 anonymous generic vehicles, not the unit's actual
  composition, and this is true at BOTH regions, for BOTH datasets equally - it explains why the
  whole system has needed so many runtime patches (auto-formation, de-stacking, fan-out) but does
  NOT by itself explain any region-specific or dataset-specific failure.
- (2) SELF-INFLICTED, region-sensitive bug (Mojave-specific, affects true aggregates only): our
  interface hardcodes ground-waypoint altitude to a fixed 100 m MSL (VrfC2SimService.cs:693,
  parity-preserved from the C++ oracle). This is near/above Sweden's ~50-110 m terrain but ~1000 m
  UNDERGROUND at Mojave's ~1100 m terrain. The generic fallback's `aggregate-lead-follow-in-
  formation-controller` has `ground-clamp True` explicitly configured (verified directly from the
  sysdef). This is the best-supported explanation for the empty-offset-route Mojave freeze -
  three independent lines of evidence now converge on it (2026-07-14 binary/DLL analysis, this
  session's sysdef read, and the 2026-07-12 live "formations are lowercase" observation, which
  only makes sense if the resolved template is the one this session found).
- 1.BdeHQ's reliability across every test, every region, is explained: its SIDC echelon ('H',
  Brigade) is unhandled by our dispatch switch, so it defaults to a real DIS platform type
  matching VR-Forces' own documented M1A2 example - it was never on the fragile aggregate path.
- Scenario-level configuration (Bogaland2.scnx vs TropicTortoise.scnx, all 11 files) is genuinely
  equivalent - re-verified directly, not assumed.
- Nothing in VR-Forces' own User's Guide (the "basic setup" documentation the user asked to be
  checked first) describes any of this - it is all below that level of documentation, living in
  the OPD/sysdef files and (unreachable) API reference. The "did we miss an obvious beginner
  checkbox" concern is answered: no.

**BEST-SUPPORTED HYPOTHESIS, NOT YET LIVE-CONFIRMED:** fixing (2) - i.e. querying real ground
altitude instead of hardcoding 100 m - should unfreeze golden-shaped aggregates at Mojave. A
candidate fix already exists in the codebase (`GroundWaypointAltitudeMode=Live`) but its only two
live tests (2026-07-15) were confounded by a THIRD, unrelated problem (below) and produced no
clean signal either way.

**STILL OPEN, NOT ADDRESSED BY ANY OF THE ABOVE (two separate unresolved issues):**
- (3) COA-STP1's own units fail to march even at the golden Sweden region, where golden units
  (hitting the identical generic-fallback code path) succeed. Since both datasets share the same
  architectural weakness (1) and the same region (ruling out (2)), something ELSE differs between
  COA-STP1's actual unit records and golden's. DIS-type already falsified as that difference;
  force-side/hostility is the one remaining untested categorical difference (COA-STP1's problem
  units are hostile/WASA; golden units are friendly/NATO) - still on the shelf, still cheap to
  test with a single-variable live probe (flip one golden unit's Side to hostile, or one COA-STP1
  unit's Side to friendly, and see if the result flips).
- (4) The 2026-07-15 Live-altitude-mode probe itself showed universal freeze, including 1.BdeHQ -
  which bypasses (1) and (2) entirely (it's the real-entity Tank() path). This means something
  about the vrfLauncher-launched live session specifically suppressed movement broadly, for
  reasons unrelated to everything else in this document. This is THE blocker preventing (2) from
  being confirmed live. Leading untested candidate from the first 2026-07-15 research pass:
  VR-Forces' documented per-entity "Freeze Movement" property, or the "Send Standard Start/
  Resume and Stop/Freeze PDUs" scenario setting (external remote-controller participants may not
  receive/honor run state without it) - see the "DOCUMENTATION RESEARCH PASS" section below.

**RECOMMENDED NEXT STEPS, IN ORDER (research-only conclusions; no live work done this pass):**
1. Resolve (4) first - it is what's blocking everything else from being testable. Cheapest check
   costs no new appNo: inspect Freeze Movement state on the stuck objects and the Start/Resume
   PDU application setting next time VR-Forces is up.
2. Once (4) is ruled out or fixed, re-run the still-missing control: Fixed100 (known-good
   baseline) against the SAME vrfLauncher-launched Mojave backend, to confirm the environment
   itself is healthy before trusting any altitude-mode result.
3. Then re-run the Live-altitude-mode A/B cleanly - this should now be a valid test of hypothesis
   (2), which the last two attempts were not.
4. (3) (force-side probe) can run independently/in parallel whenever Sweden is available - it
   does not depend on 1-3.
5. (1) (proper unit-type modeling - remap to VR-Forces' real EntityLevel template library, or
   author project-specific templates like `Mobile Irregular.entity`) is a real, separate quality
   investment, NOT blocking the above - hold for a deliberate decision, not a quick fix.

**On escalating to MAK support:** not yet warranted. Every open item above has a concrete, cheap,
self-directed next test that does not require MAK's input. If (4) turns out to be a genuine
VR-Forces/vrfLauncher environment quirk (not a config setting we control), THAT would be a
crisp, well-formed question worth asking - narrower and better evidenced than anything
available three days ago.

## 2026-07-15 (fresh session, part 5) - CORRECTION to part 4: ArmorCompany (echelon E, the
## SINGLE LARGEST bucket in both datasets) is NOT the generic fallback - it's a real template,
## and it ALSO has ground-clamp True. This simplifies the picture, does not complicate it.

Found while preparing the missing-control live test (VR-Forces up, TropicTortoise loaded fresh,
2026-07-15 evening): `data/R9_Mojave_UnitMove_Order.xml`'s own pre-existing comments (written by
an earlier, live-querying session) record `114.MechCoy ArmorCompany 11.1.225.5.2.0.0 ->
Tank Company (USA), "Column"` - i.e. NOT the generic `Ground_Aggregate.entity` fallback part 3
claimed for it. Verified directly: `EntityLevel\vrfSim\Tank Company (USA).entity`,
`matchType="3:11:1:225:5:2:-1:-1"` - wildcards only the trailing Specific/Extra fields, and our
ArmorCompany code (`11,1,225,5,2,0,0`) satisfies it. This is a REAL, well-composed template: real
Title-Case formations (Column/Line/Wedge/Vee, unit-specific .frm files), a genuine HQ+3xTank
subordinate composition (not 4 anonymous vehicles), `echelon-level="Co"` (not blank),
platform `HigherAggregate.ope` (not `Aggregate.ope`), system
`ground-higherUnit-disaggregated-movement.sysdef` (not `ground-disaggregated-movement.sysdef`).

Re-ran the wildcarded-tail search properly for every factory (my part-2/part-3 passes both
missed matches with wildcarded trailing fields - this was the actual gap, now closed): confirmed
ArmorPlatoon (Cat=1) and ArmorCoHQ (Cat=5,Sub=20, Specific mismatch) and ScoutUnit (Cat=2) still
have NO match anywhere in EntityLevel scope - they DO fall to `Ground_Aggregate.entity`, that
part of part-3 stands. Only ArmorCompany was wrong.

**Recomputed population split (E is the single largest echelon bucket in BOTH datasets):**
COA-STP1 (128 units): E=64 (real "Tank Company" match) / F=26 + D=23 (generic fallback, 49
total, 38%) / dash=12 + C=2 + H=1 (real Tank() entity path, 15 total). Golden (80 units): E=29
(real match) / D=24 + F=11 (fallback, 35 total, 44%) / dash=14 + H=2 (real entity, 16 total).
So roughly HALF of each dataset's units were never on the generic-fallback path at all.

**Why this simplifies rather than complicates the conclusion:** checked
`ground-higherUnit-disaggregated-movement.sysdef` (Tank Company (USA)'s own system) directly -
it ALSO configures `(component-type "aggregate-move-along-controller") ... (ground-clamp True)`,
same as the generic fallback's controller. So ground-clamp is a property of disaggregated-unit
MoveAlongRoute generally in this SMS, not specific to under-defined units. This is consistent
with, and actually strengthens, the history already on record: R9's Mojave region-swap
(2026-07-13) froze BOTH the generic-fallback platoon (8 m then stopped) AND the well-matched
"Tank Company" company (410 m wrong-way then stopped) - i.e. the region-specific failure does
NOT track template quality, which is exactly what you'd expect if issue (2) (our hardcoded
100 m MSL altitude vs. Mojave's ~1100 m terrain, hitting the shared ground-clamp step) is the
real driver, and NOT track-with issue (1) (generic vs. real composition) at all. Issue (1) stays
real as a separate FIDELITY gap (COA-STP1's platoons/battalions get anonymous generic vehicles
instead of real composition) but is now more clearly demoted to "does not block movement by
itself" - a well-modeled unit fails at Mojave too.

ADVERSARIAL NOTE: this is the SECOND correction to the same claim in one session (truncated grep,
then a wildcard-pattern miss). Treat any further specific-OPD-match claim in this document as
requiring a live-query cross-check (per the project's own long-standing R5 rule - "always query,
don't trust static .entity analysis") before being treated as settled, not just a repeat static
grep.

## 2026-07-15 (fresh session, part 6) - THE MISSING CONTROL RAN (LIVE, apps 3421-3425+):
## Fixed100 ALSO freezes on this vrfLauncher backend - rules OUT altitude/duration, rules IN
## a session/environment-level movement-execution block

Environment: user reported "TT is loaded in a fresh VRF" (vrfGui + vrfSimHLA1516e both started
2026-07-15 20:07:48, combined-mode vrfLauncher launch, connection profile "HLA 1516 Evolved RPR
2.0 with MAK extensions", back-end AppNo 3001 / front-end 3101 - confirmed from the user's own
Simulation Connections Configuration screenshot; OUR tools use DIFFERENT, incrementing AppNos per
the Appendix B ledger convention (never 3001/3101, which belong to the backend/frontend
themselves) - this is by design, not an error, and worked without collision for every join below).

Preflight (Appendix A) all green: loopback 15 ms, C2SIM REST 200, license valid, vrfGui/
vrfSimHLA1516e confirmed up.

Sequence run: ResetVrf --dry-run (3421, clean, 2 baseline objects) -> PushInit
data/R9_Mojave_Lean_Initialization.xml (6 units, SystemName=STP - QUERYINIT confirmed 6) -> app
(3422, ClientId=STP, TimeMultiplier=20, AggregateFormation=auto, GroundWaypointAltitudeMode
UNSET = Fixed100 default, i.e. the ORIGINAL parity config, NOT the altitude-mode probe) -> all 6
units created, all 5 aggregates formation-set+reorganized (live-queried formation lists:
1222.MechPlt/1141/1142/1143.MechPlt = [line,column,wedge,vee]; 114.MechCoy =
[column,line,wedge,vee] - ALL LOWERCASE, including 114.MechCoy, which CONTRADICTS the part-5
"Tank Company (USA), Title-Case" identification - see the OPEN QUESTION below) -> PushOrder
data/R9_Mojave_UnitMove_Order.xml (OK, 13 position-report bus messages observed flowing back
within 5s) -> app log confirms CreateRoute + MoveAlongRoute ISSUED for all 3 tasked units
(1222.MechPlt, 114.MechCoy, 1.BdeHQ; the 3 un-tasked platoons 1141/1142/1143 correctly show no
task - expected, not a bug) -> WatchVrf (3423, 150s/15s) then a second WatchVrf (3425, 60s/10s,
after a live Freeze-Movement=No toggle applied to 1.BdeHQ mid-window via the GUI).

RESULT: IDENTICAL failure to the 2026-07-15 (earlier) confounded Live-altitude runs, but this
time under FIXED100 - the untouched, historically-reliable parity-default path:
- 1.BdeHQ: real, valid position (34.608416,-116.712685,1131.4) held EXACTLY constant across BOTH
  WatchVrf windows (~200s combined wall-clock), including AFTER the user applied
  Freeze Movement=No to it live via Set > Action > Freeze Movement (GUI). NO CHANGE. Freeze
  Movement (at least as applied here) is NOT the (sole) blocker.
- 1222.MechPlt, 114.MechCoy: degenerate (0,0,-6378137) position for the entire first window -
  literally "null island" at Earth's core radius; NOT anywhere near Mojave. User independently,
  visually confirmed "114.MechCoy is not visible" in the GUI at the same time - matches exactly.
- The second WatchVrf window (post-init, same units still up) reflected 54 objects total (member
  entities from the 5 disaggregated aggregates) - EVERY ONE static across all 6 samples (60s),
  including many with REAL, non-degenerate Mojave-area coordinates. Not one displacement anywhere
  in the federation.
- Sim clock CONFIRMED advancing (user: "clock advancing fast at 15x", visually verified in the
  GUI). Duration/multiplier RULED OUT as an explanation: T_R5_PL1's route is ~578 m (recomputed
  directly from the order XML's coordinates), the unit's own ordered-speed is 10 m/s (verified in
  Ground_Aggregate.entity) - under 2 minutes to complete at 1x, i.e. under 8s of wall-clock at
  15x. We observed 200+ s of wall-clock (3000+ s of sim time at 15x) with zero displacement.

**CONCLUSION: this is NOT the altitude/ground-clamp mechanism (Fixed100 was never touched by
that code) and NOT a task-duration/multiplier artifact (ruled out by direct computation +
confirmed clock advancement). Something about THIS backend session specifically blocks movement
EXECUTION at a level below task dispatch and below clock advancement - creation, formation-
setting, task dispatch, and position-report publishing all work; only actual displacement does
not happen, for every object in the federation, regardless of whether it goes through the
generic-fallback aggregate path or the real-entity path.** This STRENGTHENS (does not weaken)
the case that the 2026-07-15 earlier "even 1.BdeHQ froze under Live-altitude-mode" result was
never about altitude at all - it is this same, still-unidentified, session/environment-level
block, and it was already present before the altitude variable was ever introduced.

STILL OPEN, NEXT CANDIDATES (untested this pass): the "Send Standard Start/Resume and Stop/Freeze
PDUs" GUI Application Setting (Settings > Application > GUI Settings) - not yet checked by the
user; per-object "AI Enabled" state (`Aggregate.ope` state-data default is True, but not
confirmed live for these objects) - Freeze Movement specifically was checked and ruled out (for
1.BdeHQ), but AI Enabled is a DIFFERENT flag that could produce the identical symptom (object
exists, publishes state, accepts tasks, but its own controllers never execute them) and has not
been checked. OPEN QUESTION, not yet reconciled: 114.MechCoy's LIVE-queried formation list this
run was lowercase ([column,line,wedge,vee]), contradicting part-5's identification of it as
matching "Tank Company (USA)" (Title-Case, HQ+3xTank composition) via the order-file comment -
either that identification was wrong, or something about ITS resolution differs from what was
recorded before; not resolved, flagged for the next pass rather than guessed at.

Housekeeping: this run's units (6 created + ~54 reflected members) have NOT yet been cleaned up
(StopIface + post-run ResetVrf sweep) as of this note - do not start a new push/create cycle
against this federation before that cleanup runs, per RUNBOOK sec 0/4.

## 2026-07-16 (part 7) - LIVE Fixed100-vs-Live A/B COMPLETED at TT + PRISTINE C++ BASELINE RUN
## at Bogaland2 with COA-STP1: one textbook Live-mode completion; the freeze/runaway zoo
## reproduces IN THE ORIGINAL INTERFACE; F1 runaway is original behavior, not a port regression

Three live results this session (apps 3421-3434; evidence files named below are in this
directory). ALSO: the C++ repo git structure was corrected this session - master reset to the
pristine baseline `191933a`, all Phase 1 work preserved on branch `phase1-vrffacade-extraction`
in a sibling worktree (see START_HERE repo-state section). The pristine baseline was then
BUILT and RUN for the first time in this project's history.

**RUN A - port, Fixed100 (parity default), R9 lean init + R9 order, TropicTortoise scenario,
20x (apps 3421-3425).** Recorded in part 6 below: universal freeze - zero displacement for all
54 reflected objects over ~200 s of confirmed-advancing 15-20x sim clock, including plain-entity
1.BdeHQ; 1222.MechPlt + 114.MechCoy degenerate (0,0). Freeze-Movement=No applied live to 1.BdeHQ:
no change. Evidence: port_fixed100_freeze_TT_app3422_2026-07-15.txt,
watchvrf_3423_port_fixed100_TT_2026-07-15.txt.

**RUN B - port, GroundWaypointAltitudeMode=Live clearance=50, same TT backend, same data, 20x
(apps 3426-3429). CORRECTION to what was reported in-session ("moved ~255m then stopped"): recomputed
against the order's actual waypoints, 1222.MechPlt executed its FULL ~1.16 km route and STOPPED
~8 m from its final waypoint** (init lon -116.600487 -> wp1 -116.594174 -> final wp -116.587860;
telemetry: t=3.1s already at -116.590737 (77% through), t=18s at -116.587947 = 8 m short of the
final waypoint, then stationary t=18->108s; TASKCMPLT fired). **This is a textbook-correct,
telemetry-verified aggregate route completion at Mojave under the Live altitude fix - the unit
that was fully DEGENERATE under Fixed100 in run A.** The other two: 114.MechCoy held ~2.4 m of
its init position the whole window with a VACUOUS TASKCMPLT; 1.BdeHQ frozen exactly at init with
a VACUOUS TASKCMPLT. NOTE THE INVERSION: the SUCCESS is the Ground_Aggregate-generic-fallback
platoon; the failures are the well-templated Tank-Company-match company and the exact-match M1A2
entity - template quality does NOT predict movement success here. Evidence:
port_livealt_TT_app3428_2026-07-15.txt, watchvrf_3429_port_live_TT_2026-07-15.txt.

**RUN C - PRISTINE ORIGINAL C++ interface (master @ 191933a, freshly built), FULL COA-STP1
(128 units + 42-task order), fresh user-loaded Bogaland2 scenario, 15x, appNo 3432 (STILL
RUNNING at time of writing; end-state telemetry capture launched as app 3434).** Motivated by a
colleague's report that coa-gpt1 "works fine" on the original interface. Results (90 s WatchVrf
window ~50 s after order push, apps 3432-3433):
- 163/163 objects created clean (128 units + 35 areas). 9 first-wave moveAlongRoute dispatches
  (the other 33 tasks are predecessor-gated - the C2SIM order is a DAG; a 90 s window
  undersamples it; the USER observed "plenty of movement for the first 15-20 min" in the GUI as
  chains progressed, so the window numbers below are the FIRST WAVE ONLY, not the whole story).
- 3 of 9 MOVED - all echelon-F ArmorCoHQ (1-35/2/1_A, 40/2/1_AD, 1-6/2/1_AD), at plausible
  ~80 km/h under 15x. BUT at least 1-35/2/1_A OVERSHOT: its T1 route ends at (34.570,-117.006);
  at t=78s it was at (34.427,-117.283), ~30 km PAST the final waypoint and still diverging.
  **F1 RUNAWAY REPRODUCED IN THE UNTOUCHED ORIGINAL INTERFACE - it is original behavior, NOT a
  port regression.**
- 6 of 9 FROZEN - all at EXACTLY (34.679985,-116.724799,1137.1), the COA-STP1 mega-pile
  coordinate, INCLUDING the plain TANK entity 1-1/2/1_AD (SIDC has no echelon char -> created
  via the createTank default path, a real M1A2-type entity, NOT an aggregate - so the
  aggregate/offset-route mechanism cannot explain ITS freeze). Frozen set spans echelons E and F
  and includes both well-templated and fallback types - echelon/template are NOT the
  discriminator inside this run either.
- The pristine C++ ran with hardcoded altitude 100 (its only mode), NO formation repair, NO
  de-stack, NO fan-out - and still produced 3 movers at ~1100 m terrain. **So altitude-100 alone
  does NOT universally block movement** (weakens the strong form of the altitude hypothesis;
  run B's clean completion still shows the altitude fix MATTERS for at least some paths).
Evidence: cpp_pristine_bogaland2_coastp1_app3432_2026-07-16.txt (42 MB - grep/tail it, do not
read whole), watchvrf_3433_cpp_bogaland2_2026-07-16.txt.

**CROSS-RUN TENSIONS (pre-registered as open, NOT resolved - do not paper over):**
1. Run A (0 movers incl. entity) vs run C (3 movers + rich GUI activity): >=4 uncontrolled
   differences (interface, scenario file TT-vs-Bogaland2, dataset/positions R9-lean-vs-COA-STP1,
   config formation-auto-vs-none). NEITHER "the port is broken" NOR "TT is broken" is
   established. The port-vs-C++ A/B on the SAME backend+data is the single highest-value missing
   comparison in the whole investigation.
2. What distinguishes run C's 3 movers from its 6 frozen? Not echelon, not template, not
   activity code (checked). Frozen all sit at the pile coordinate; whether the movers' INIT
   coordinates were also the pile (escaped) or dispersed (never stacked) is NOT yet checked
   against the init XML - key open question (vendor docs document overlapping-footprint speed
   penalties; a 54-unit pile may gridlock to ~zero speed - "H-overlap", parameter values not yet
   pulled from the SMS).
3. Run B's success/failure inversion (fallback platoon succeeds; well-templated company and
   real entity fail, both with vacuous completions) is unexplained.

Ledger: apps 3421-3434 consumed this session (Appendix B updated; 3432 still joined - clean-stop
via StopIface when the user is done observing; NEVER force-kill it).

## 2026-07-16 (part 7b) - RUN C END-STATE CENSUS (app 3434, 600s/30s window ~1h+ sim time after
## order push): THE BATTLE IS OVER AND NOT ONE FIRST-WAVE UNIT ARRIVED; ALL THREE RUNAWAYS
## TERMINATED UNDERGROUND 100-150+ KM FROM THEIR DESTINATIONS

Census method: per-object displacement between each object's first and last sample in the
600s window (threshold ~50m), 1,732 valid tracked objects (of 1,795 reflected; degenerate/NaN
filtered). Evidence: watchvrf_3434_cpp_endstate_2026-07-16.txt.

**RESULT: 0 of 1,732 objects moved during the window. The entire federation is static.** The
user-observed "plenty of movement in the first 15-20 min" phase has fully ended. Terminal states
of the 9 first-wave tasked units:
- 1-35/2/1_A (mover):  STUCK at (34.687, -116.488, **alt -1441 m**) - ~1.4 km BELOW SEA LEVEL
  (terrain there ~800-1000 m: roughly 2+ km underground), ~20 km E of its route end.
- 40/2/1_AD (mover):   STUCK at (34.084, -117.831, **alt -460 m**) - underground, ~120 km SW of
  its ordered destination.
- 1-6/2/1_AD (mover):  STUCK at (33.679, -118.114, **alt -580 m**) - that position is OFFSHORE
  (Pacific, near Catalina): an armored HQ drove ~160 km and terminated under the seabed.
- C/1-35 (previously classed frozen): actually made ONE small move after the 90s window -
  now static ~220 m east of the pile (34.6796,-116.7224). A 4th, stalled, mover.
- 5-20/2/1_A, 1-1/2/1_AD (TANK entity), B/5-20, 856/HHC, 4-27/2/1_A: still EXACTLY at the pile
  coordinate. Never moved at all.

**Interpretation (evidence-grade, not theory): the pristine original interface's end-to-end
result on COA-STP1 is ZERO correct movement-task executions.** GUI-visible motion existed but
was 100% pathological: 3 units ran 100-160 km past/away from their destinations and terminated
STATIC and UNDERGROUND (negative MSL altitude - strong physical evidence tying the runaway
mechanism to the hardcoded-100 m route altitude: the movers end up below terrain and stop
forever), the rest never left the pile. By contrast the port's Live-altitude run B produced the
ONLY telemetry-verified correct arrival ever recorded at these coordinates (1222.MechPlt, 8 m
from its final waypoint). "The original works better" is now falsified in both directions: the
original shows MORE motion and ZERO correctness; the port with the altitude fix showed less
motion but the only correct execution. The two-mechanism account (pile gridlock freezes;
bad-altitude route geometry sends escapees underground) now has physical end-state evidence and
is the audit's primary verification target.

## 2026-07-16 (part 8) - PRE-REGISTERED PROBE P1a: GUI-NATIVE TASKING OF FROZEN UNITS
## (predictions written BEFORE the run, per the new probe discipline)

OPPORTUNITY: run C's session is still live; its 6 frozen first-wave units still exist at the
mega-pile coordinate (34.680,-116.725). Tasking one NATIVELY through VR-Forces' own GUI - no
interface code involved - is the control experiment this investigation has never run.

PROCEDURE (user, in the VR-Forces GUI, sim running):
1. Select one FROZEN unit at the pile (e.g. 856/HHC or 5-20/2/1_A - any unit visibly stationary
   at the cluster). Task > Movement > Move to Location (or right-click equivalent), pick a
   destination ~2-5 km away on open ground, OK. Observe ~2-3 min at 15x.
2. If it does NOT move: drag that unit (or another frozen one) OUT of the pile with the GUI
   editor (a few hundred meters clear of every other unit), then re-issue the same native move
   task. Observe again.
3. (Baseline, optional but valuable) Create a FRESH unit of a similar type from the Simulation
   Objects Palette at a dispersed location, native-task it the same way.

PRE-REGISTERED PREDICTIONS (exactly one should survive):
- P-interface: step 1 unit MOVES -> the defect is in HOW the interfaces task units (call
  parameters/sequence), NOT in the objects or data. The C++/port tasking path becomes the focus.
- P-object-or-pile: step 1 unit does NOT move, but step 2 (dragged clear, re-tasked) MOVES ->
  the objects are fine; the PILE (overlapping-footprint gridlock, a vendor-documented mechanism,
  values being pulled by the audit) is the blocker. De-stacking with adequate spacing becomes
  the fix; coa-gpt data feedback becomes mandatory.
- P-defective-object: neither step 1 nor step 2 moves, but step 3 (fresh palette unit) does ->
  interface-CREATED objects are structurally defective vs GUI-created ones; the creation call
  becomes the focus (diff created-object attributes vs palette-created).
- P-platform: nothing moves, not even step 3 -> VR-Forces itself cannot move this unit type at
  these coordinates in this scenario; escalate to MAK with a minimal, interface-free repro -
  finally a well-formed support question.

Results to be recorded here verbatim from the user's observation. Do NOT clean-stop app 3432
until this probe is done.

## 2026-07-16 (part 9-pre) - OPERATIONAL FINDING: the pristine v2.36 baseline CANNOT clean-stop
## against the current server - protocol-version discard blocks the UNINITIALIZED signal

Found while closing out run C (user had closed VR-Forces; StopIface drove the server
UNINITIALIZED but app 3432 kept running, 53 threads). SOURCE-VERIFIED: main.cxx:36 hardcodes
`c2simVersion = "1.0.1"` and C2SIMinterface.cpp:1681-1686 DISCARDS (`continue`) any incoming
STOMP message whose `c2sim-version` header differs - the server stamps its broadcasts
CWIX2023v1.0.2, so the baseline never sees systemState==UNINITIALIZED and never resigns. The
golden-era logs contain ZERO such mismatch warnings (grep of the phase1-worktree golden-trace
log), so the golden-era environment differed in some respect not yet identified. OPEN PUZZLE
(queued to forensics): run C's ORDER demonstrably passed this same version gate (42 tasks
queued) - which messages carry which c2sim-version header, and why, is unresolved. OPERATIONAL
CONSEQUENCE for all future pristine-C++ runs: plan for NO clean stop (end-of-run = VR-Forces
close + user-approved process termination + rtiexec restart to clear the stale federate); the
port does NOT have this defect (it reads state via REST GetStatus - RUNBOOK sec 7 fixed
exactly this bug class).

## 2026-07-16 (part 9) - FRESH-CONTEXT ADVERSARIAL AUDIT, WAVE 1 VERDICTS
## (5 Opus investigators + per-claim adversarial refuters; every verdict below was
## independently re-verified against primary sources by a refuter agent before acceptance)

**KILLED - overlap-footprint slowdown as the pile-freeze mechanism (supersedes the H-overlap
lead in parts 7b/8):** the `MaximumSpeed-Footprint-Overlap-Modifier` parameter and its sole
consuming Lua script exist ONLY under the AggregateLevel model set - grep across C2simEx/
EntityLevel/base (the include chain our scenarios actually load, all three .sms read directly)
returns ZERO hits. Even where it exists it takes the MINIMUM of neighbors' modifiers (no
stacking), values span 0.5-1.0, affects MAX speed only, and by doc rule has no effect when
ordered speed < modified max. It structurally cannot freeze anything. Abandon permanently.

**KILLED - "hardcoded-100-vs-Live altitude is a red herring for the freeze":** a mechanics
finding claimed ground-clamp makes route-vertex z irrelevant; its refuter REFUTED it against
RUN A/B - the only clean single-variable A/B in the corpus (same backend, same dispersed no-pile
data, same XY endpoints, ONLY the altitude mode toggled) flipped 1222.MechPlt from frozen-
degenerate to a full ~1.16 km telemetry-verified arrival 8 m from its final waypoint. The
Live-altitude fix is a PROVEN LEVER and stays in the plan. (Both true at once: clamp exists AND
the altitude mode gates something real - mechanism still unresolved, likely via the degenerate-
position/creation path rather than the clamp itself.)

**DEMOTED to hypothesis - collision avoidance as THE entity freeze cause:** genuinely
documented (entity-level objects avoid each other; obstruction-sensor + ground-auto-collision-
controller enabled at 1.0 m lateral clearance in ground-tracked.sysdef; "might never reach the
point, but it will continue to try... as long as the task is in effect"; NOTE aggregated units
do NOT collision-avoid), and it fits run C's pile freezes - but its refuter showed it cannot
explain RUN A (dispersed units, no pile, froze anyway), R8 (de-stacked, still 0/6), or RUN B
(altitude change unlocked a unit CA can't account for). P1a (part 8) remains the discriminator;
do not commit to a de-stack/disable-CA fix before it runs.

**ESTABLISHED - code is at parity on the entity path; do not edit tasking code:** facade
moveToLocation/moveAlongRoute/createRoute calls are param-identical to the C++ raw controller
calls (VrfFacade.cpp:470/474 vs C2SIMinterface.cpp:2308/2334, verified both sides), and RUN A's
Fixed100 clamp equals C++'s hardcoded 100. RUN A emitted the same calls the C++ emits and froze
anyway. The 0-vs-3 mover gap is NOT code-attributable; RUN A and RUN C are DIFFERENT failures
(RUN A: an execution block with degenerate/sentinel-position signature on dispersed no-pile
data; RUN C: co-location freeze at the pile). The decisive experiment remains port-vs-C++ on
the SAME backend + SAME data.

**ESTABLISHED - two port-only divergences that matter for ANY full-COA-STP1 port run:**
(1) 31 of COA-STP1's 42 tasks carry a STREND predecessor; the port's bounded predecessor-
timeout + default skip policy (TaskSequencer.cs:86-135) DROPS successor subtrees the C++'s
forever-busy-wait eventually dispatches - set `Vrf:PredecessorTimeoutPolicy=force` (or a huge
timeout) for C++-comparable runs. (2) AggregateFormation=auto reorganize-at-creation is
port-only and aggregate-gated (cannot explain entity freezes, but must be OFF - "" - for strict
parity A/Bs).

**ESTABLISHED - two DISTINCT vacuous-completion mechanisms (both documented/verified):**
(a) units: route completion fires when the formation's LEADING EDGE reaches the last vertex
(RouteMoveAlong.htm) - premature by up to the formation half-extent, near-zero center
displacement when the spread is comparable to route length; (b) entities: 1.BdeHQ completed
with zero displacement against a documented at-distance tolerance of 1 m - completion signaled
by something OTHER than arrival (unexplained; queued to forensics wave 2). Truthful arrival
checking must measure center displacement and mirror the documented tolerances (entity
at-distance 1 m / near-distance 25 m / approach 2 m/s; unit in-position 0.2 m).

**Documented GUI procedure for P1a (from the audit):** create via Simulation Objects Palette
(Click-to-Create); task via Task > Movement > Move Along Route / Move to Location; read live
task state + speed on the ENTITY CONTROL PANEL (shows name/type/altitude/heading/speed and
current task of the selected entity) - use that panel, not icon motion, as the probe readout.

Wave 2 in flight: run-matrix re-run (first attempt returned garbage and was discarded),
telemetry-forensics + external-evidence re-runs (first attempts exceeded structured-output
limits), forensics now also carrying: movers' init coords vs pile, C++ completion-message
census, the entity-vacuous-completion trigger, RUN A degeneracy census, and whether run C's
underground runaways exited the paged terrain AO.

## 2026-07-16 (part 10) - THE ORIGINAL AUTHORS' OWN DOCS, FULLY MINED FOR THE FIRST TIME
## (P-OFF2 executor; C2SIM-VRForcesv2.26.pdf + README.txt + VRFadditionalFiles/README.txt +
## OpenC2SIM repo extras - all read in full, byte-diff of the shipped SMS vs installed)

Everything below is from the GMU authors' own documentation, VERIFIED by the executor with
citations. Several items are author-documented BASICS this investigation never checked:

1. **"VRForces limits callback names to 10 chars; tasking will not work if longer unit names
   are used in the initialization file"** (VRFadditionalFiles/README.txt:125-126; manual p.1:
   "Unit name are limited by VRForces internas to 10 characters"). NEVER CHECKED in a week of
   investigation. Golden log lines DO show truncation ("1222.MechP" for "1222.MechPlt").
   Immediately suspicious for COA-STP1: many names are EXACTLY 10 chars ("1-35/2/1_A",
   "5-20/2/1_A", "1-1/2/1_AD"...) - if any two of the 128 unit names share a 10-char prefix,
   name-keyed creation-callback correlation could bind the WRONG VRF uuid and misdirect
   tasking - which would present as frozen units + possible vacuous completions + a
   nondeterministic-looking split. P-OFF3 executor launched to census name lengths/collisions
   and both interfaces' correlation tolerance.
2. **"If the object is 'under ground' (elevation AGL <= 0), VRForces will not execute a route
   from c2simVRF for that object"** (README:98-100) - an AUTHOR-DOCUMENTED route-refusal rule
   that matches the Mojave empty-offset-route freeze signature exactly, and gives problem (A)
   an author-documented mechanism: the authors designed around ~sea-level Bogaland (AGL~MSL);
   the interface's 100 (intended to "trigger ground clamping", manual p.2) is ~1000 m
   UNDERGROUND at Mojave => VRF refuses/degenerates routes. Their own suggested remedy for
   failed routes: edit the movement sysdef's move-along-controller ground-clamp to False
   (README:122-124; NOTE their cited path is misspelled AND wrong - the real file is
   EntityLevel/vrfSim/systems/movement/human-disaggregated-movement.sysdef).
3. **Unit/task names must be unique and contain NO BLANKS; Order/Task UUIDs unique**
   (manual p.1:36-37).
4. **The installed C2simEx SMS faithfully matches the authors' package** - 30/31 files
   byte-identical, 1 benign GUI re-serialization, nothing author-required missing. (Kills any
   "missing author SMS content" theory.) One AUTHOR-SIDE gap: their README claims the package
   changes bin/vrfLuaDIS.dll but ships no bin/ folder at all - the implied DIS Lua
   customization is not delivered (DIS path only; we run HLA).
5. **Authors' validation envelope**: "The DIS version has not been load-tested; HLA version is
   believed to work properly ... task-follows-task" (README:138-139) + "We do not plan to
   expand c2simVRF to the full capabilities of VR-Forces" (manual p.2:65-67). COA-scale
   parallel tasking is OUTSIDE what the authors ever validated.
6. **Terrain must fully page in (green playbox) before movement is reliable** (README item 6);
   late-joiner inits place objects at LAST-REPORTED positions unless a fresh server init cycle
   runs (README note c) - relevant to session-state weirdness (problem C).
7. **Protocol version is compile-time only** (main.cxx:36 "1.0.1" with a stale "CWIX2024"
   comment; no CLI parameter) - confirms part 9-pre's clean-stop finding is a baseline defect
   with no runtime workaround.
8. **HONEST GAP (executor's own falsification note): the author docs document freeze/
   not-arrive modes but say NOTHING about run-away behavior.** F1 has no author-documented
   explanation; it remains ours to explain (current best: underground/unreachable route
   geometry + AO exit, part 7b/9).

## 2026-07-16 (part 11) - P-OFF1 RESULT: creation/member STRUCTURE also falsified as the
## mover/frozen discriminator; and RUN C's "movers" moved as LONE ICONS without their members

Executor audit of the RUN C log + COA-STP1 init vs the pristine C++ factories. Verdicts:

- **STRUCTURAL DISCRIMINATOR: FALSIFIED (VERIFIED).** Clinching pair: 1-35/2/1_A (MOVED) and
  5-20/2/1_A (FROZE) went through the identical factory (createArmorCoHQ), identical SIDC
  echelon (F), identical 5-point AOA_SE route shape from the identical pile start. Also
  falsified as discriminators: creation order (fully interleaved), time-to-tasking (all 9
  within ~4 s), route point-count (4 of 6 frozen had full 5-point routes; only 2 had
  degenerate 2-point routes), TANK-vs-aggregate (both fates in both classes).
- **COA-STP1's init defines ZERO organic subordinates** (no Subordinate tags at all - 128
  flat units; unlike the golden init which has them), and the pristine C++ spawns no members
  itself - exactly 128 creations logged. The ~1,600 additional federation entities are
  VR-Forces' OWN disaggregation spawns per the OPD templates (unattributable to parents from
  position telemetry alone).
- **NEW PATHOLOGY DETAIL: the 3 "movers" translated as LONE unit icons - no member cluster
  followed them** (each mover's new position shows exactly 1 object; ~833 objects remain in
  the pile box). Even RUN C's "movement" was a unit icon detaching from its members - further
  confirmation that NOTHING in RUN C constituted correct execution.
- SUPERVISOR ADJUDICATION of the executor's proposed "task-chain dependency race" follow-up
  lead: REJECTED as an explanation for the 6 frozen - all 9 first-wave tasks are verified
  chain-HEADS with confirmed "Tasked moveAlongRoute" log lines (wave-2 forensics); the
  quoted "starts after end of Task" lines belong to the 31 queued successors, not these 9.
  Frozen-despite-confirmed-dispatch stands.
- Two frozen units' 2-point degenerate routes (4-27/2/1_A DESTRY, 856/HHC SECURE) are a real
  sub-observation (verb-collapse to moveAlongRoute produces near-trivial routes for
  non-movement verbs) but explain at most 2 of 6.

STATE OF THE MOVER/FROZEN QUESTION after parts 7-11: every static axis is now falsified
(init data, DIS type, echelon, factory, template, route shape, creation order, dispatch
timing, member structure). Remaining live candidates: (1) name-truncation collision binding
(P-OFF3, in flight - author-documented 10-char limit; many COA-STP1 names are exactly 10
chars; a collision would misdirect tasking nondeterministically), (2) VRF-internal
nondeterministic stack resolution (would require the P1a GUI-native probe or a MAK support
question to pin). If P-OFF3 comes back negative, (2) becomes the default and P1a the only
remaining discriminator we can run ourselves.

## 2026-07-16 (part 12) - P-OFF3 RESULT: name-truncation collisions FALSIFIED; the offline
## probe program is now EXHAUSTED for the mover/frozen split

Executor census of unit names across all three datasets + both interfaces' correlation code:

- **COLLISION HYPOTHESIS: FALSIFIED (VERIFIED).** Zero 10-char truncation collisions exist in
  ANY dataset (COA-STP1: all 128 names are already <=10 chars and unique; golden: 57 names
  >10 chars but every one truncates to a DISTINCT 10-char form - cross-validated against the
  golden log's own 57 truncation warnings; R9 lean: same). No blanks anywhere. Name binding
  cannot explain any observed fate.
- **KEY MECHANICAL CORRECTION: the 10-char truncation is the C++ INTERFACE'S OWN, applied at
  XML parse time** (C2SIMxmlHandler.cpp:2364-2370, with the WARNING lines seen in golden
  logs) - and C++ correlation is exact-match on the pre-truncated key, collision-defended.
  Route names pass through VR-Forces at up to 99 chars UNTRUNCATED.
- **SUPERVISOR ADJUDICATION - the executor's claimed "port latent truncation bug" is
  REFUTED by live evidence and must NOT be ticketed as-is.** The claim assumed VRF callbacks
  deliver 10-char-truncated names, making the port's full-name task lookup miss. But the
  port has repeatedly created AND successfully tasked >10-char-named units end-to-end
  (1222.MechPlt, 12 chars: R5, R9-B, RUN B - full name in the creation callback log lines,
  successful CreateRoute+MoveAlongRoute, telemetry-verified movement). In this HLA1516e/VRF
  5.0.2 configuration the callback evidently returns FULL names; the authors' "VRForces
  limits callback names to 10 chars" README claim appears stale/self-referential (describing
  their own parse-time truncation), at least for this protocol path. (The executor also
  misattributed RUN B to the C++ interface - RUN B was the port.) No port fix warranted;
  flagging so a future session does not "fix" a non-bug.

**STATE OF THE MOVER/FROZEN QUESTION after parts 7-12: ALL offline-checkable candidates are
now exhausted and falsified** - init data, DIS type, echelon, factory, template, route shape,
creation order, dispatch timing, member structure, name length, name collisions. The only
remaining live candidate is VR-Forces-internal nondeterministic resolution of the 54-way
coincident stack. The only remaining discriminating instruments: (1) probe P1a (GUI-native
tasking of frozen units, pre-registered part 8) on the next live session, and (2) a MAK
support question - which is NOW fully earned and well-formed: "N identically-created,
identically-tasked aggregates co-located at one coordinate: a nondeterministic subset detach
and move as lone unit icons (members left behind) and never stop at route end; the rest never
move despite accepted tasks and an advancing clock - what governs stack resolution, and what
is the supported way to task co-located aggregates?" - with the RUN C minimal repro
(COA-STP1 init + order + telemetry) attachable.

## 2026-07-16 (part 13) - PRE-REGISTERED PROBE P-C1: Live-mode run TWICE back-to-back on ONE
## fresh TropicTortoise session (decides problem C's nature)

Config (both runs IDENTICAL, = RUN B exactly): port, R9 lean init + R9 order, ClientId=STP,
TimeMultiplier=20, AggregateFormation=auto, GroundWaypointAltitudeMode=Live, Clearance=50.
Fresh user-loaded TT session, 2026-07-16 afternoon. Apps: 3435 sweep, 3436 app-1, 3437
watch-1, 3438 sweep, 3439 app-2, 3440 watch-2, 3441 final sweep. Movement oracle: WatchVrf
displacement only.

PRE-REGISTERED PREDICTIONS (written before execution):
- P1 BOTH runs: 1222.MechPlt marches (~1.16 km, arrives): problem C was specific to the dead
  2026-07-15/16 sessions; the Live lever is REPRODUCIBLE; proceed to P1b/P-A. Watch whether
  114.MechCoy + 1.BdeHQ reproduce their RUN-B freezes - if yes, that stable per-unit split
  becomes its own tracked question (no longer attributable to session degradation).
- P2 BOTH runs frozen: the block reproduces on fresh sessions with this scenario+data+config;
  IMMEDIATELY run P1a (GUI-native tasking) on the live frozen units + capture AI-Enabled /
  Start-Resume-PDU states.
- P3 runs DISAGREE: per-run nondeterminism on identical config+session - worst case;
  escalates the MAK question with this as the sharpest repro.

**RESULT (2026-07-16 afternoon): PREDICTION P1 CONFIRMED, with the per-unit split
reproducing EXACTLY in both runs.**
- Run 1 (apps 3436/3437): 1222.MechPlt marched + completed BEFORE the watch window opened
  (real TASKCMPLT; its 4 member vehicles telemetry-confirmed clustered within ~15 m of the
  final waypoint at ~34.6129,-116.5879 - members arrived WITH the unit, unlike run C's
  lone-icon movers). 114.MechCoy degenerate (0,0,-6378137) all window; 1.BdeHQ frozen at
  init all window.
- Run 2 (apps 3439/3440, watch started BEFORE the order - full transit captured):
  1222.MechPlt: degenerate at t=13-23s, RESOLVED at t=33s at its init, then marched east
  sample-by-sample (-116.6001 -> -116.5963 -> -116.5936 -> -116.5908 -> -116.5879 at t=73s,
  ~28 m/s sim = column speed at 15-20x) and STOPPED at -116.587946 = 8 m from the final
  waypoint, stationary for the remaining 100 s. TEXTBOOK arrival, reproduced. 114.MechCoy:
  degenerate the ENTIRE window; 1.BdeHQ: frozen at init the entire window.
- NEW DIAGNOSTIC FACT from the transit capture: the aggregate degenerate-position state is
  a NORMAL TRANSIENT that resolves (~30 s for the platoon) - the company's pathology is
  that its position NEVER resolves. "Degenerate" = aggregate position not yet published;
  platoon publishes and marches; company never publishes.
- CONCLUSIONS: (i) the Live-altitude lever is REPRODUCIBLE - two textbook arrivals
  back-to-back; (ii) problem C's "universal session block" did NOT reproduce - the dead
  2026-07-15 sessions were anomalous, deprioritize chasing them; (iii) the REAL object of
  study is now the stable per-unit split: platoon arrives / company never publishes /
  entity frozen at init - reproducible on demand, same config, same session.
- Note vs history: the SAME entity (1.BdeHQ) with the SAME order geometry MOVED at TT on
  2026-07-13 (R9-A, FULL 80-unit transplant init) but freezes in every 2026-07-15/16 run
  (LEAN 6-unit init) - init-file difference (full vs lean) is a live candidate variable
  alongside route-engagement geometry (first order waypoint 60 m from the platoon's init vs
  575+ m for the entity / 1.7 km for the company - the within-run correlation that holds in
  every Live run so far).

## 2026-07-16 (part 13b) - PRE-REGISTERED PROBE P-C2: first-waypoint-at-own-position for the
## frozen entity (same session, single task, single variable)

Config: same session/config as P-C1 run 2. New order file
data/PC2_EntityFirstWaypoint_Order.xml: ONE task, for 1.BdeHQ only, with the route's FIRST
waypoint AT the unit's own init position (34.608416,-116.712685) and the second ~1.2 km east
(the original T_R5_TK1 destination). Fresh Order/Task UUIDs; ZERO XML comments (RUNBOOK 0.6).
Apps: 3441 app, 3442 watch.
PREDICTIONS: (a) 1.BdeHQ MARCHES -> route-engagement distance (first waypoint too far from
the entity) is the entity-freeze discriminator; mechanism hunt moves to VRF's route-start
engagement rules; ALSO explains the company (wp1 1.7 km away). (b) STILL FROZEN -> waypoint
geometry falsified for the entity; next candidate = lean-vs-full init difference (R9-A
comparison), probed by re-running with the FULL transplant init.

**RESULT: PREDICTION (b) - STILL FROZEN.** The P-C2 order was pushed to the RUNNING app 3439
(supersession logged cleanly: T_PC2_TK1 replaced in-flight T_R5_TK1), CreateRoute +
MoveAlongRoute confirmed for the SAME live object (841a5bb2), route = [point0 at live pos,
wp1 at live pos, wp2 1.16 km east], all vertices at live-ground+50 m (sane altitudes). Zero
displacement across 11 samples / 113 s (watch 3441). WAYPOINT/ROUTE GEOMETRY IS FALSIFIED as
the entity-freeze discriminator. Remaining candidates for the entity freeze: the LEAN-vs-FULL
init difference (R9-A moved this same entity with the full 80-unit transplant init on
2026-07-13; every lean-init run freezes it), and object-level state only the GUI can see.
P1a (GUI-native tasking of THIS live frozen entity) is now the sharpest instrument and the
unit is alive and waiting.

**P1a RESULT (2026-07-16, live, user-executed): NATIVE GUI TASKING ALSO FAILS - the
interface is EXONERATED for the entity freeze.** Sequence: (1) when the user issued the
native Move to Location, VR-Forces raised an Interrupt-Task dialog proving our T_PC2_TK1
move-along was CURRENTLY EXECUTING (accepted + active, not rejected/dropped) on the frozen
entity. (2) After replacing it, the Entity Control Panel showed: Type=M1A2 Abrams MBT,
native "Move to {34:36.53 N, 116:42.76 W, 1131}" task ACTIVE (with active subtask),
Platform Send Observation + Tracking Reports active, No Contacts, Altitude MSL 3712 ft
(=1131 m, ON terrain), and **Speed (kph): 0**. VR-Forces' own tasking produces zero motion
on this entity while an identically-created platoon marches nearby in the same session.
Per the part-8 pre-registration this eliminates P-interface for the entity and leaves
P-defective-object vs P-platform - discriminated next by object state (fuel/ordered-speed/
subsystems) and by the drag-and-retask + fresh-palette-unit steps.

**P1a CONCLUSION - P-DEFECTIVE-OBJECT (breakthrough, live, user-executed 2026-07-16):**
- State Data of the frozen entity was CLEAN on every obvious axis: AI Enabled=Yes, Freeze
  Movement=No, Frozen=False, Under Fire=False, Suppression=0, Skill=Expert, ROE=fire-when-
  fired-upon, DR=DrDrmRvw. Resources FULL (movement 465/465 100%, all weapons 100%) - fuel/
  resource starvation RULED OUT. Altitude MSL 1131 m = on terrain (displayed state clamped OK).
- DECISIVE: the user DRAGGED the frozen entity to a new position (GUI re-place), which CLEARED
  the stuck Active move task, then issued a fresh native Move to Location - and THE ENTITY
  MOVED ("flew over there"). A GUI re-place + re-task RELEASES the freeze.
- VERDICT: the entity is created in a DEFECTIVE MOVEMENT STATE by the interface (both C++ and
  port); native GUI tasking can't move it AS-CREATED, but a GUI drag (which re-places it
  cleanly on terrain and resets task/movement state) fixes it. This is NOT pile-gridlock
  (1.BdeHQ is a solo dispersed entity), NOT the tasking path (native GUI failed identically
  until the drag), NOT object type / fuel / AI / freeze-flag. The defect is at CREATION, and
  the leading mechanism is the altitude the interface sets at create/task time: route vertices
  hardcoded 100 m MSL and the create-time SetAltitude derived from ElevationAgl default 1000
  (ALL three inits carry ZERO ElevationAgl fields, so the 1000 default always applies) - both
  sea-level-era constants that land ~1000 m UNDERGROUND at Mojave's 1131 m terrain. Authors'
  own README: an object at elevation AGL <= 0 "will not execute a route" (part 10 item 2) -
  an AUTHOR-DOCUMENTED match for exactly this create-underground -> accepts-task-but-cannot-
  move -> GUI-drag-to-surface-fixes-it behavior.
- CAVEAT to verify: user said the re-tasked tank "flew over there" - confirm it drove on the
  ground vs went airborne (would indicate the destination/route altitude is still being taken
  as an above-ground value, i.e. the fix must clamp to terrain, not just to a non-underground
  number). Flagged for the fix's acceptance test.

**THIS CLOSES THE ENTITY-FREEZE ROOT CAUSE (problem A, entity variant): create/task-time
altitude places ground objects underground at high-elevation terrain; VRF then accepts tasks
but never executes movement.** The fix is the staged route-vertex terrain guard (plan sec 4
item 2) EXTENDED to the create-time SetAltitude: clamp every interface-emitted altitude
(create AND route) to live terrain height + small clearance, never a fixed MSL/AGL constant.
This also predicts the Live-altitude mode's success (RUN B/P-C1: it already queries live
ground for route vertices - which is why the platoon marched) and tells us the create-time
SetAltitude needs the same treatment (why some units still froze under Live: the freeze was
latched at CREATION before the Live route altitude ever applied).

**MECHANISM VERIFIED IN CODE (2026-07-16):** the create-time altitude the interface sends is
GEODETIC/MSL, not AGL - VrfFacade.cpp:90-93 builds `DtGeodeticCoord(lat, lon, g.altMeters)`
where altMeters is the C2SIM ElevationAgl (UnitTranslator.cs:101 Tank = `D(ElevationAgl)+1.0`).
ALL inits carry ZERO ElevationAgl fields -> the parity default 1000 always applies -> every
ground unit is created at **1000 MSL**. At Bogaland (~50 m terrain) 1000 MSL is ~950 m in the
AIR -> VR-Forces ground-clamp pulls it to the surface (works). At Mojave (~1131 m terrain)
1000 MSL is ~131 m UNDERGROUND -> cannot clamp UP through terrain -> born buried -> authors'
documented rule "elevation AGL <= 0 will not execute a route" (part 10) fires -> accepts task,
Active, speed 0. This is the SAME sea-level-constant-flips-sign-at-elevation bug as the route
vertices (hardcoded 100 MSL), verified end to end and corroborated by the P1a drag-fix.

**THE FIX (staged, plan sec 4 item 2, now precisely specified):** replace the fixed-MSL
sentinels at BOTH create and route time with a live-ground-clamped altitude - query terrain
height at the unit's lat/lon and set MSL = terrain + small clearance ("0 AGL" expressed in the
MSL API). This is the exact query GroundWaypointAltitudeMode=Live ALREADY performs for route
vertices (proven by the platoon's repeated clean marches); the change is to (a) make Live the
default (retire Fixed100) and (b) extend the same ground query to the create-time SetAltitude
in the create factories (UnitTranslator PostCreateAltitude / the service's create path),
because the freeze is latched at CREATION before any route altitude applies. ACCEPTANCE:
re-run R9 lean init with the fix and NO manual drag - all units (entity + aggregates) should
move without intervention. OPEN/SEPARATE: 114.MechCoy's signature is "position NEVER publishes"
(degenerate 0,0 whole window), distinct from the entity's "real position but won't move" - the
create-altitude fix is CONFIRMED for the entity, LIKELY-but-unproven for the company; the
non-publishing aggregate may need its own check (the platoon, same aggregate path, DID march,
so aggregates are not universally broken).

## 2026-07-16 (part 13c) - SUPERVISOR HEADER CHECK (fix-session bring-up): setAltitude's
## third arg is aboveGroundLevel - the deferred SetAltitude is AGL, NOT MSL; the
## buried-birth latch narrows to the CREATE call itself

- MAK header (vrfRemoteController.h:1390-1392): `setAltitude(const DtUUID&, double altitude,
  bool aboveGroundLevel = false, ...)`. VrfFacade.cpp:453 passes TRUE -> the deferred
  post-create SetAltitude(ElevationAgl+1 = 1001) means 1001 m ABOVE GROUND LEVEL, not
  1001 MSL. Parts 13/13b (and plan sec 3b as first written) said "SetAltitude = 1001 MSL" -
  CORRECTED here. A positive-AGL set cannot bury an object, and the identical command runs
  at Bogaland where everything works - the "SetAltitude forces the clamped object back
  underground" suspect is DEAD.
- ALSO: createEntity (header :1287-1311) carries `bool groundClamp = true`; the facade's
  callback-overload call (VrfFacade.cpp:411-413) stops at uniqueName, so groundClamp
  DEFAULTS TRUE on every entity create. createAggregate has NO groundClamp parameter.
- Evidence-consistent reading: at Bogaland the entity create lands ~950 m ABOVE terrain
  (1000 MSL vs ~50 m terrain) and the clamp DROPS it to the surface - works. At Mojave the
  same 1000 MSL is ~131 m BELOW 1131 m terrain; a clamp cannot raise a birth through
  terrain, the movement state latches dead at birth (authors' AGL<=0 rule), and later
  normalization puts the DISPLAYED position on terrain (P1a saw 1131 MSL) while the latch
  persists until a GUI re-place (drag) resets it.
- CONSEQUENCE for the sec-3b fix: the CREATE call's own pos.AltMeters is the PRIMARY
  target - under Live, ground units must be born at-or-above terrain (born-above + clamp is
  the Bogaland-proven safe path); skipping the deferred SetAltitude for ground units under
  Live is secondary hygiene, not the cure. FALLBACK if live acceptance still freezes:
  programmatic drag-mimic (SetLocation re-place at the reflected clamped geodetic via
  TryGetEntityGeodetic, guarded against the known ~30 s degenerate-position transient on
  aggregates - never re-place to a degenerate reading).

## 2026-07-15 (fresh session) - DOCUMENTATION RESEARCH PASS (read-only, no live run)

User directive: before continuing hypothesis-chasing, do real research - read VR-Forces' own
docs, check whether this is a basic setup gap, and only escalate to MAK support if the
investigation is genuinely stuck on something real (not "clueless"). This pass read the
user-flagged 101 doc plus ~10 more targeted VR-Forces 5.0.2 user-guide pages (on-disk,
C:\MAK\vrforces5.0.2\doc\help\Content) and ran a few external web searches. No live run.

NEGATIVE RESULT (addresses the user's top concern): `vrf_createScenario.htm` (New Scenario
wizard: terrain/SMS selection, "Create Global Dynamic Terrain Processor", advanced params) is
a generic beginner walkthrough. It does NOT mention aggregate offset-route generation, ground
clamping, or any streaming-terrain path-planning precondition. Also checked "Configuring
Aggregate-Level Movement Restrictions" (featureconfig.txt terrain-mobility speed factors -
governs movement SPEED modifiers, not whether offset-route generation returns empty) and "How
Units Move" (vrf_closeFormationVsReorganization.htm - confirms, does not contradict, Thread
A's mechanism: leader path -> parallel offset paths per member). CONCLUSION: this is not a
missed beginner checkbox. The mechanism Thread A found 2026-07-14 (fixed 100 m MSL waypoint
altitude -> below-ground input -> aggregate offset-route ground-clamp failure, asymmetric vs
entity movement) sits below the level of anything in VR-Forces' User's Guide - it is compiled-
implementation behavior, consistent with Thread A's own note that the relevant .cxx bodies are
compiled-only. Checked for an API/class reference doc set locally (none exists under
doc\help - user-guide only) and via ftp.mak.com/out/classdocs (confirmed DNS-unreachable from
this environment too, matching Thread C's 2026-07-14 finding - not a retry-harder gap).

NEW LEAD (untested, cheap to check, not previously considered): VR-Forces has a documented
per-entity **Freeze Movement** property (`vrf_setFreezeMovement.htm`, DataRequests/EntityLevel):
"The Freeze Movement request freezes an entity's movement system... Tasks that require an
entity to move, such as Move to Location, PAUSE while Freeze Movement is set to Yes. The
entity's other systems... remain active." This matches the 2026-07-15 altitude-probe symptom
for 1.BdeHQ EXACTLY: confirmed CreateRoute+MoveAlongRoute dispatch, confirmed-running sim
clock, zero net displacement over 150-228 s (apps 3416/3419) - and 1.BdeHQ is a plain entity,
so it does NOT go through the offset-route/ground-clamp mechanism Thread A diagnosed; its
freeze needs a SEPARATE explanation. Also found a related scenario-level setting,
`vrf_sendingStartStopPdus.htm` (Scenarios/CreateRun): "By default... VR-Forces sends scenario
control messages that are meaningful only to VR-Forces applications that are part of the
current session" - i.e. by default, external (non-VR-Forces) participants like our
DtVrfRemoteController app may NOT receive/honor standard run/pause semantics unless "Send
Standard Start/Resume and Stop/Freeze PDUs" is explicitly enabled (Settings > Application >
GUI Settings). Neither property has been checked for the objects created in the confounded
2026-07-15 altitude runs. WatchVrf currently only samples position (TryGetEntityGeodetic - no
Appearance/frozen-bit read); extending it to log DIS Appearance (which carries a standard
"Frozen Status" bit per DIS 1278.1 for any entity, independent of VR-Forces' own Freeze
Movement flag) would be a small, well-scoped addition. NOT YET CONFIRMED OR REFUTED - flagged
as the cheapest next check (a GUI inspection of the stuck objects' Freeze Movement property, or
the Application Settings checkbox, costs no new appNo) before re-running the missing control.

ADVERSARIAL NOTE: this is a hypothesis, not a finding - it explains 1.BdeHQ's zero-displacement-
with-real-dispatch symptom well, but does NOT by itself explain the ORIGINAL Mojave
empty-offset-route mechanism (Thread A's ground-clamp finding stands on its own evidence,
un-touched by this). The two could be independent (freeze flag blocks entity movement broadly
under vrfLauncher; ground-clamp blocks aggregate offset-routes specifically) or the freeze flag,
if it turns out to apply to the unit LEADER's own entity representation, could also explain the
original empty-route symptom (an untested unification, not yet supported by evidence either
way).

RECOMMENDATION (this session): do NOT draft a MAK support message yet. The investigation is not
"clueless" - Thread A/B/C's 2026-07-14 mechanism work is genuine, source-grounded engineering,
and there is still a concrete, nearly-free next check (Freeze Movement / Start-Resume-Stop-
Freeze PDU state) that has not been run. If that is checked and ruled out, the next step is
still the previously-identified "missing control" (Fixed100 vs the vrfLauncher-launched Mojave
backend). A MAK support question becomes worthwhile once the environment-vs-code confound is
resolved and a single, precisely-stated mechanism question remains (candidate draft: whether
DtDisaggregatedMoveAlongController's offset-route ground-clamp has a documented below-terrain
tolerance, and whether a supported client-side API exists to query real ground height at a
point before tasking, on streaming/online whole-earth terrain).

## Decisions log

- 2026-07-14: user moved to the supervisor seat and directed a multi-Opus dig at this
  blocker (goal: a class of coa-gpt COAs executable, not just COA-STP1). Investigation
  is read-only until the supervisor approves a single-variable live probe at GATE-ENV.
  No live run / no appNo consumed yet (next free 3386).
- 2026-07-15 (fresh session): user directed a documentation-first pass before further live
  work, explicitly skeptical that the investigation had checked VR-Forces' own basic docs and
  open to drafting a MAK support message if the team is genuinely stuck on something real. See
  the research-pass section above for the outcome: 101 docs checked (negative - not a missed
  basic setup step), a new Freeze Movement / Start-Resume-Stop-Freeze PDU hypothesis surfaced
  (untested), MAK support message NOT drafted this session (premature - a cheap check remains).
