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

## Decisions log

- 2026-07-14: user moved to the supervisor seat and directed a multi-Opus dig at this
  blocker (goal: a class of coa-gpt COAs executable, not just COA-STP1). Investigation
  is read-only until the supervisor approves a single-variable live probe at GATE-ENV.
  No live run / no appNo consumed yet (next free 3386).
