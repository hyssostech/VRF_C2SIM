# PREREG TERRAIN ROW 2cR - UNCHANGED REPEAT of Row 2c: is the aggregate's +14 s reproducible? - registered 2026-09-02, BEFORE launch

WHAT THIS IS: Row 2c (docs/experiments/PREREG_TERRAIN_ROW2C_FLATTEN_2026-09-02.md sec 6, run
20260902T104832Z, bridge A7504441, `Vrf__GroundWaypointAltitudeMode=TerrainProfile`) run
again with NOTHING changed - same bridge, same app build, same env, same invocation, same
data. ZERO variables move. This is not a new experiment about the mode; it is a REPLICATE
whose only job is to separate two hypotheses that Row 2c's n=1 could not separate:

  H-V   RUN-TO-RUN VARIANCE of the aggregate. Row 2c's 114.MechCoy +198.1 s was an outlier
        draw; the unit's true spread is wider than the ~3 s seen in three prior runs.
  H-ALT A SYSTEMATIC EFFECT OF THE MODE. The ~40 m lower waypoint altitudes (terrain + 10
        instead of live + 50) change how the aggregate's move-along proceeds - formation
        slotting, working-route generation, or the waypoint-reached test - and the delay
        will reproduce.
  H-OTHER something else in the run entirely (recorded if the artifacts show it).

An unchanged repeat discriminates them WITHOUT moving a variable: H-ALT predicts the delay
returns; H-V predicts it does not. Both branches are written below and adjudicated from the
SAME run, so nothing is retuned after the fact.

## Sources read for this prereg (docs first, per the 2026-09-01 directive)

Question put to the documentation: DOES ROUTE-VERTEX ALTITUDE AFFECT A GROUND ENTITY-LEVEL
AGGREGATE'S MOVE-ALONG IN VR-FORCES 5.0.2? Answer up front: NO VENDOR SOURCE READ STATES
THAT IT DOES, AND NONE STATES THAT IT DOES NOT. The question is not addressed. What the
vendor DOES document is the aggregate's move-along MECHANISM, which is entirely different
from an individual entity's, and which contains two altitude-adjacent surfaces. Both are
recorded here as UNSTATED-BUT-PLAUSIBLE, not as findings.

VERIFIED, from the read-only vendor headers under C:\MAK\vrforces5.0.2\include:

- vrfmodel/disaggregatedMoveAlongController.h:34-63 (class comment). An aggregate does NOT
  follow the ordered route itself. "Movement is implemented by creating temporary working
  routes for each subordinate, positioned at an offset needed to maintain that
  subordinate's position in formation. The controller periodically monitors subordinate's
  relative positions to one another and issues DtSetSpeedRequests at a relatively faster or
  slower speed than the ordered speed... The movement task is considered complete when all
  subordinates have reached the end of the route and have issued task complete reports to
  the aggregate." So the aggregate's completion time is the MAX over its subordinates'
  completion times on DERIVED routes, plus report latency - a fundamentally different and
  noisier quantity than an individual entity's arrival. 114.MechCoy is the only taskee on
  this path.
- vrfmodel/disaggregatedMoveAlongController.h:295-311 buildOffsetRoute: builds the parallel
  offset route from the ORIGINAL route's vertex list plus a body offset, then calls
  adjustOffsetRouteStart() (:345-352, extends the first segment by the formation half
  length) and adjustOffsetRouteEnd() (:354-360, clips the end back by the formation half
  length). Both take `bool& dataAvailable`; generateFormationRoutes (:225-231) documents its
  return as "true if the function completed, false if it is still waiting for data". THE
  AGGREGATE'S ROUTE GENERATION CAN STALL ON TERRAIN DATA AVAILABILITY AND RETRY. Individual
  entities do not take this path. The header does NOT say what the extend/clip distances are
  measured in (2D or 3D) nor whether they run before or after ground clamping.
- vrfmodel/disaggregatedMoveAlongController.h:397-399 shouldGroundClamp(): "Boolean value
  indicating whether or not generated route vertices should be ground clamped or not."
- vrfobjparam/aggregateMoveAlongDescriptor.h:159-164, parameter `ground-clamp`: "Ground-clamp
  generated route vertices. Ground-vehicles and human aggregates should set this value to
  true. Fixed-wing and rotary-wing entities should set this value to false. Parameter Default
  Value: True". THIS IS THE STRONGEST DOCUMENTED EVIDENCE AGAINST H-ALT: for a ground
  aggregate at stock settings the GENERATED subordinate route vertices are ground clamped, so
  the altitude the interface authored on the ordered route is expected to be DISCARDED before
  any subordinate ever chases it. What the header does NOT state is whether the clamp happens
  before or after the extend/clip arithmetic, or whether the source altitude changes which
  terrain queries block.
- vrfobjparam/aggregateMoveAlongDescriptor.h:111-139 and vrfmodel/maintainFormationMonitor.h:
  55-74. The formation monitor is the aggregate's speed governor: catchup-factor default 1.5,
  slowdown-factor default 0.1 (a subordinate judged ahead is cut to one TENTH of ordered
  speed), in-position-tolerance 0.25 of formation spacing (monitor default 0.2),
  evaluation-interval 1.0 s. maintainFormationMonitor.h:95-99 positionStatus() takes
  `const DtVector& geocentricOrigin`, `const DtVector& geocentricTestPosition`, and
  `const DtVector& idealBodyCoordinatesPosition` - a 3D signature; whether the comparison is
  projected to the horizontal is NOT STATED. This is the only mechanism read that could turn
  a small geometric difference into a multi-second arrival difference on an aggregate but not
  on an individual: a 0.1x brake applied on a different schedule.
- vrfmodel/groundFollowInFormationController.h:105-110: findClosestSegmentIndex "Calls
  closestPointAlongChord2D". Where this vendor projects to 2D it SAYS SO in the header. No
  comparable 2D statement exists anywhere on the DtDisaggregatedMoveAlongController path,
  which is why the 2D-vs-3D question stays UNSTATED rather than answered by analogy.
- vrfmodel/groundMoveAlongControllerComponent.h:95-136 (the INDIVIDUAL entity path, for
  contrast): sets myControlPoint to the current vertex position and "advance[s] to the next
  vertex rather then actually stop when a vertex is reached". The header never states the
  reached test's dimensionality either.

DOCUMENTATION SEARCHED AND FOUND SILENT:
- C:\MAK\vrforces5.0.2\doc contains NO Developer's Guide PDF (AddingContent.pdf,
  MAKInteroperabilityGuide.pdf, VR-ForcesFirstExperience.pdf, VRFUsersGuide.pdf,
  VRFEntityCatalog.pdf, VRFMigrationGuide.pdf, the 5.0/5.0.1/5.0.2 release notes, plus
  doc/help/ which is the MadCap-packaged Users Guide). Nothing there addresses route-vertex
  altitude versus aggregate move-along timing.
- docs.mak.com: already mined for this branch and recorded in
  docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 1 - the class reference pages are
  generated from the same headers and "add nothing", and 5.x DROPPED the narrative aggregate
  and organization chapters (memory note "MAK developer docs URLs"; handoff item 3b). The
  Users Guide page vrf_setRouteVertexAltitude.htm (design sec 1, contract C5) says only that
  vertex altitude is the AUTHOR's responsibility and that above-sea-level vertices can end up
  underground. It makes NO statement about movement timing.

REPO PRECEDENT ON WAYPOINT ALTITUDE (the strongest empirical prior, and it cuts against
H-ALT): docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md sec 6a, the
below-terrain confound variant, run 2026-07-22. An authored Tank Platoon - a DISAGGREGATED
AGGREGATE, the same class of taskee as 114.MechCoy - was given a route with waypoints 941 m
BELOW terrain and "MOVED - IDENTICALLY to the above-terrain Mojave run"; "The below-terrain
route was CLAMPED UP to the surface: the aggregate held ~1041->1037 m (surface) throughout;
the clamp did NOT drop vertices, did NOT return an empty offset route." That run confirms the
ground-clamp of generated vertices empirically and shows a 941 m vertex-altitude change
producing no gross movement change on an aggregate. CAVEAT, stated so it is not over-read:
that comparison was coarse (settled ~300 m E, "stable by t=88..93"), NOT a seconds-resolution
completion-offset comparison like the one at issue here. It makes a 40 m altitude change
producing a reproducible 14 s delay IMPLAUSIBLE, but it does not exclude it.
Also read: docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 1 (the C5 contract and the
docs.mak.com survey), sec 7 (Rows 1 / 2 / 2R / 2c results); docs/CORRECTIONS_LOG.md:118
(ROUTE/WAYPOINT altitude named as an un-examined surface); docs/experiments/
MOJAVE_FIXTURE_2026-07-21.md:160-166 (the same falsification, summarized).

CONCLUSION OF STEP 1, HONESTLY SCOPED. The docs do not answer the question. They do tell us
(a) the aggregate path is structurally different and has its own latency surfaces
(terrain-data stalls in route generation; a 1 Hz formation governor with a 0.1x brake), and
(b) the vendor's default is to GROUND-CLAMP the generated vertices, which - together with the
07-22 941 m result - makes a direct altitude->timing mechanism the LESS likely of the two
hypotheses on prior evidence. That is a prior, not a result. This run is the test.

## 1. The ONE variable: NONE. This is a replicate.

Identical to Row 2c in every respect: bridge VrfBridge.dll
A7504441F421B668D10F5AFD8B4FD71110002D13FE6ABAE0DB576C7C209236F5 on 10/10 main-checkout
copies (re-verified before launch); Ijwhost.dll unchanged; the same app build (no code edited
since c864dc1); `Vrf__GroundWaypointAltitudeMode=TerrainProfile` set in the invoking pwsh
session and nothing else in `env:Vrf__*`; `Vrf__TimeMultiplier` NOT set (1x); the same init
and order files; RealTemplates; stock templates; NavArea disabled; notify level 3;
`-RunSecs 420 -StopWhenComplete`; SettleHoldSecs 60; TerrainClearanceMeters 10;
TerrainProfileTimeoutSeconds 10; GroundWaypointLiveClearanceMeters 50;
MaxHorizontalMismatchMeters 50.

## 2. Invocation (main checkout, VRF_C2SIM, pwsh)

    $env:Vrf__GroundWaypointAltitudeMode = 'TerrainProfile'
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_NoComments.xml `
        -RunSecs 420 -StopWhenComplete
    Remove-Item env:Vrf__GroundWaypointAltitudeMode

Foreground, 15-minute timeout (Row 2c wall was 7 min 30 s). Adjudication from the run
directory artifacts ONLY (vrfc2simapp.log, reports-captured.log, run-manifest.json,
watchvrf-trace.csv, bin64-vrfSim.log). Ledger: marker `*** NEXT FREE: 3704 ***` before the
run (docs/OPUS_EXECUTION_PLAN.md:1577, verified as the single value-bearing marker); the
runner consumes 7 numbers and advances the marker itself, so expected wasValue 3704 /
newValue 3711, appNos 3704-3710.

## 3. Pre-launch inventory (must hold, else STOP - never kill)

VERIFIED 2026-09-02 before launch: no vrfSim* / vrfGui / vrfLauncher / WatchVrf /
ListenReports / VrfC2SimApp process; RTI trio exactly rtiAssistant 41336 (start 9/1 14:34) /
rtiexec 224608 (15:08) / rtiForwarder 76620 (15:09), unchanged since Row 1; docker stp-server
"Up 18 hours (healthy)" + c2sim_server4.8.4.9 "Up 18 hours"; `Get-ChildItem env:Vrf__*`
count 0; 10/10 main-checkout VrfBridge.dll copies hash A7504441 (the only other copies on
disk are the two labelled backup directories and the .claude worktrees, none of which this
run loads).

## 4. Predictions with numbers

Comparators are Row 2c's own numbers (run 20260902T104832Z) and Row 1's (20260902T010704Z).

P1 - TERRAIN AUTHORING REPEATS EXACTLY (HIGH). Exactly three :1466 Information lines
    "Terrain profile reply {Id}: 3 sample(s) [...]" for ids 7 / 8 / 9, each with N = 3, three
    tokens, DISTINCT indices {0,1,2}, no `#k:none`. Exactly three :802 Information lines
    "all 3 vertices authored from terrain + 10 m clearance", one per id. ZERO :807
    Partial/Fallback, ZERO :1480 timeout, ZERO :793 "request not sent", ZERO :1453 partial,
    ZERO :810 Note, and ZERO `warn:` lines in the whole app log. The `alts` lists IDENTICAL
    to Row 2c to 0.1 m:
        id 7 / T_R5_PL1 / 1222.MechPlt: [1050.6, 1043.9, 1036.7]
        id 8 / T_R5_CO1 / 114.MechCoy:  [1126.7, 1126.8, 1126.9]
        id 9 / T_R5_TK1 / 1.BdeHQ:      [1141.4, 1136.3, 1131.1]
    A MISS here means the input to the movement question is not the same input, and P3
    becomes uninterpretable - record it and STOP.

P2 - THE TWO INDIVIDUAL ENTITIES REPRODUCE (HIGH). TASKCMPLT offsets from
    clocks.orderPushedUtc, measured off the reports-captured.log `[hh:mm:ss.fff]` stamps -
    the same measure Row 1 sec 6 A and Row 2c sec 6 P3 used - within +/-5 s of Row 1:
        1.BdeHQ       117.3 s  (Row 2c 117.5, ROW2R 118.0, CONFIRM2 117.1)
        1222.MechPlt  129.2 s  (Row 2c 129.6, ROW2R 130.1, CONFIRM2 129.1)
    These are the negative control on the run itself: if THEY move, the run is not a clean
    replicate and P3 cannot be read.

P3 - THE DISCRIMINATOR (HIGH). 114.MechCoy's offset, same measure. TWO BRANCHES, BOTH
    WRITTEN NOW, ADJUDICATED FROM THIS ONE RUN:
    (a) 198.1 +/- 5 s  ->  H-V IS REFUTED. Two independent draws at ~198 s after four draws
        at 182-185 s under the other mode is not a variance story. H-ALT - or some other
        systematic effect of running in TerrainProfile mode - STANDS, and the next step is
        the documentation answer above turned into a MAK question (does the aggregate's
        generated-route ground-clamp or its formation governor see the ordered vertex
        altitude?), NOT a code change and NOT a further probe.
    (b) 183.8 +/- 5 s  ->  H-V STANDS. Row 2c's +198.1 was an outlier draw, and the honest
        statement of the aggregate's spread becomes 182.1 / 183.8 / 185.2 / ~184 / 198.1 -
        i.e. it now INCLUDES a 14 s excursion, which must be carried into every future band
        for this unit rather than quietly forgotten.
    (c) ANYTHING ELSE - a value between the branches, a value outside both, or a shift that
        lands on a DIFFERENT taskee than 114.MechCoy - is recorded as UNDECIDED. No branch
        is widened to swallow it.
    Corroboration required in every branch: the trace plateau onset (first POS within 1 m of
    the final POS) must move the same way as the report offset, as it did in Row 2c
    (233.3 s vs Row 1's 219.2). Report offset and plateau onset disagreeing is itself a
    finding - it would mean report lag, not movement.

P4 - HYGIENE AND SURVIVAL (HIGH). 3/3 TASKCMPLT in both vrfc2simapp.log and
    reports-captured.log. Endpoints from the trace final POS within 1 m of Row 2c
    (1.BdeHQ 34.608416,-116.699994 alt 1121.1; 114.MechCoy 34.653915,-116.693388 alt 1116.8;
    1222.MechPlt 34.612956,-116.587783 alt 1026.6). POS==RPT <= 1 m x3; settled true x3.
    3 x "CreateRoute ... (3 pts)" + 3 x "MoveAlongRoute issued". earlyExit.fired true with
    closedUtc - allCompleteUtc in [60, 90] s. No vrfSim* process with MainWindowTitle
    matching `^vrfSim.*\.dmp$` after teardown; no new .dmp in C:\MAK\vrforces5.0.2\bin64;
    runner exit 0; StopVrf exit 0 and "VR-Forces is DOWN"; RTI trio PIDs 41336 / 224608 /
    76620 unchanged; both observers on the stop-file path. bin64-vrfSim.log counts
    "Waiting for nav data" 0 / "empty route" 0 / "Can't find entity route" 0 / "invalid
    formation name" 1. App-log census: 3 `fail:` + 3 "Can't create data of type", 0
    Exception, 0 `warn:`. Wall 7 min 30 s +/- 45 s.

## 5. Falsifier branches - PRE-NAMED

G1 - P1 MISSES (different sample count, repeated index, a `none`, a :807, or an alts list
     differing by more than 0.1 m). The replicate is not a replicate. Record verbatim, STOP,
     do NOT read P3.
G2 - P2 MISSES (an individual entity outside +/-5 s of Row 1). The whole run is slower or
     faster, so a 114.MechCoy delta is not attributable to the aggregate. Record all three
     offsets and STOP; the discriminator is void this run.
G3 - P3 branch (c) fires. UNDECIDED. Record the number, the plateau onset, and which unit
     moved. Do not widen a band, do not re-run inside this prereg.
G4 - ANY CRASH, TIMEOUT, OR INFRASTRUCTURE FAILURE - a :1480, a :793, the MAK dump prompt, a
     non-zero runner or StopVrf exit, an observer that never reached the stop-file path, an
     RTI PID change. Infrastructure, not an answer. Dump prompt: `pwsh -File
     scripts\AnswerCrashDumpDialog.ps1` then `pwsh -File scripts\StopVrf.ps1` per RUNBOOK
     0.5.12 (ALWAYS Yes). STOP; after two infrastructure failures this session, stop entirely.

NOTE, correcting Row 2c's sec 5 defect (named in its own sec 6): a movement-timing result is
NOT an infrastructure failure. In this prereg P3 has its own branches and its own verdict, and
neither branch is a "miss" - the run is designed so that either answer is informative. Only
G1/G2/G4 are stops-without-an-answer.

## 6. Outcome (written from the run directory artifacts, after the run)

TO BE WRITTEN AFTER THE RUN.
