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

PREREG COMMIT: the predictions above were registered in commit 4f870b8 BEFORE launch. This
line is the only content added afterwards (in the immediately following commit); nothing in
sections 1-5 changed after 4f870b8, which is what the hash attests.

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

VERDICT: BRANCH (b) FIRED - H-V STANDS AND H-ALT IS REFUTED AS A REPRODUCIBLE EFFECT. The
unchanged repeat put 114.MechCoy back at +185.0 s, 1.2 s from Row 1's +183.8 and 0.2 s from
ROW2R's +185.2, with the trace plateau onset at 219.2 s - the SAME NUMBER as Row 1's 219.2 to
the trace's own 0.1 s resolution. The terrain authoring was byte-identical to Row 2c (same
three reply-shape lines, same three alts lists), so the aggregate ran on exactly the waypoint
altitudes that Row 2c produced and still finished on Row 1's schedule. Row 2c's +198.1 s was
an OUTLIER DRAW, not an effect of the mode. P1 MET, P2 MET, P3 branch (b), P4 MET. Nothing
was retuned, re-run, killed, or code-changed.

Run 20260902T111116Z_run, launched 2026-09-02 11:11:16.006Z from main at 7ee8930 (this
prereg registered at 4f870b8). `Get-ChildItem env:Vrf__*` was exactly
`Vrf__GroundWaypointAltitudeMode=TerrainProfile` at launch (echoed before the runner started)
and count 0 after. appNos 3704-3710 (vrfBackend 3704, vrfFrontend 3705, oraclePre 3706,
oracleTrace 3707, app 3708, rtiProbe 3709, createOneDiag 3710 - UNCONSUMED and BURNED, the
oracle gate passed); ledger wasValue 3704 / newValue 3711 / advanced true; marker line after
the run reads `*** NEXT FREE: 3711 ***`; ledger file CRLF 1884 / bare LF 0 / non-ASCII 0.
Pre-launch inventory held in full (sec 3). Runner exit 0; StopVrf exit 0; wall
11:11:16.006Z -> 11:18:29.274Z = 433.3 s = 7 min 13 s (Row 2c 7 min 30 s, Row 1 7 min 15 s;
band was 7 min 30 s +/- 45 s). validityFlags: the single advisory pre-init INFO only; console
[WARN]/[FAIL] 0.

P1 - TERRAIN AUTHORING REPEATS EXACTLY - MET. Three :1466 reply-shape lines and three :802
   authoring lines, verbatim from vrfc2simapp.log (lines 53/55/57 and 59/63/67):

     Terrain profile reply 7: 3 sample(s) [#0:34.61296,-116.60049,1040.6 #1:34.61296,-116.59417,1033.9 #2:34.61296,-116.58786,1026.7].
     Terrain profile reply 8: 3 sample(s) [#0:34.64763,-116.69339,1116.7 #1:34.65263,-116.69339,1116.8 #2:34.65763,-116.69339,1116.9].
     Terrain profile reply 9: 3 sample(s) [#0:34.60842,-116.71269,1131.4 #1:34.60842,-116.70637,1126.3 #2:34.60842,-116.70006,1121.1].

     Terrain profile 7 for task 'T_R5_PL1': all 3 vertices authored from terrain + 10 m clearance; alts [1050.6, 1043.9, 1036.7].
     Terrain profile 8 for task 'T_R5_CO1': all 3 vertices authored from terrain + 10 m clearance; alts [1126.7, 1126.8, 1126.9].
     Terrain profile 9 for task 'T_R5_TK1': all 3 vertices authored from terrain + 10 m clearance; alts [1141.4, 1136.3, 1131.1].

   These are CHARACTER-FOR-CHARACTER the lines Row 2c sec 6 quotes - not "within 0.1 m",
   identical. N = 3 on all three, indices {0,1,2} distinct, no `#k:none`. ZERO :807
   Partial/Fallback, ZERO :1480 timeout, ZERO :793 "request not sent", ZERO :1453 partial,
   ZERO :810 Note, and ZERO `warn:` lines in the whole app log. Three :813 "request sent"
   lines (47/49/51). The terrain query is now deterministic across runs, which is what makes
   this a clean replicate: the aggregate received the SAME lowered waypoint altitudes twice.

P2 - THE TWO INDIVIDUAL ENTITIES REPRODUCE - MET. Offsets of the TASKCMPLT report receipts
   (reports-captured.log `[hh:mm:ss.fff]` stamps) from clocks.orderPushedUtc
   2026-09-02T11:13:40.178Z:
     1.BdeHQ      (task ...0003, entity 670cfdb2) receipt 11:15:37.628 -> +117.45 s
                  Row 1 +117.3, Row 2c +117.5, ROW2R +118.0. Delta vs Row 1 +0.15. OK
     1222.MechPlt (task ...0001, entity 001aa71b) receipt 11:15:49.850 -> +129.67 s
                  Row 1 +129.2, Row 2c +129.6, ROW2R +130.1. Delta vs Row 1 +0.47. OK
   Band was +/-5 s. Plateau onsets 147.9 (Row 1 148.0, Row 2c 148.0) and 160.1 (Row 1 160.1,
   Row 2c 162.3). The run is a clean replicate, so P3 is readable.

P3 - THE DISCRIMINATOR - BRANCH (b) FIRED. 114.MechCoy (task ...0002, entity 139aa71b)
   receipt 11:16:45.217 -> +185.04 s. Branch (b)'s band was 183.8 +/- 5 s, i.e. [178.8,
   188.8]: IN. Branch (a)'s band was 198.1 +/- 5 s, i.e. [193.1, 203.1]: OUT by 8.1 s.
   Corroborated by the trace exactly as the prereg required - the two measures move together,
   so this is arrival, not report lag:
     - trace plateau onset (first POS within 1 m of the final POS) 219.2 s.
       Row 1 219.2, ROW2R 221.6, CONFIRM2 214.7, Row 2c 233.3. The onset is IDENTICAL to
       Row 1's to the trace's 0.1 s resolution and is 14.1 s earlier than Row 2c's.
     - trace TSK completionT (run-manifest oracle.earlyExit.reportEvidence) 213.2 s.
       Row 1 212.0, ROW2R 214.1, CONFIRM2 210.3, Row 2c 226.4.
   CONSEQUENCE, as pre-written: H-V STANDS. Row 2c's +198.1 s was an outlier draw. H-ALT is
   REFUTED as a REPRODUCIBLE effect - two runs with byte-identical terrain-authored waypoints
   produced +198.1 and +185.0, so the lowered waypoint altitudes cannot be a systematic cause
   of a ~14 s delay. Per branch (b)'s own terms, the honest statement of this unit's spread
   now INCLUDES the excursion and every future band for 114.MechCoy must be widened
   accordingly - see the table below.

   114.MechCoy COMPLETION OFFSET ACROSS EVERY RUN IN runs/ THAT PUSHED THIS ORDER AND GOT A
   TASKCMPLT FOR TASKEE 139aa71b-75df-4888-4a5a-6056bae66242. Offset = the report-receipt
   stamp in reports-captured.log minus clocks.orderPushedUtc from that run's own manifest;
   traceTSK = the same run's oracle.earlyExit.reportEvidence completionT. Mode/bridge from
   the run's own app log (count of "Terrain profile " lines; timeMult=) and the design doc's
   deploy records:

     run                    offset_s  traceTSK  timeMult  mode            bridge     note
     20260901T203702Z_run    178.2     n/r        1       Live            A48ABE6C   R9 baseline
     20260901T211310Z_run    184.6     n/r        1       Live            A48ABE6C   P2c endpoint record
     20260901T230326Z_run     37.0     n/r        5       Live            A48ABE6C   5x multiplier - NOT comparable
     20260901T235823Z_run    183.7     n/r        1       Live            A48ABE6C   CONFIRM1
     20260902T003710Z_run    182.1    210.3       1       Live            A48ABE6C   CONFIRM2
     20260902T010704Z_run    183.8    212.0       1       Live            28E993FE   ROW 1 control
     20260902T101431Z_run    185.2    214.1       1       TerrainProfile  28E993FE   ROW 2R (Partial - Live alts used)
     20260902T104832Z_run    198.1    226.4       1       TerrainProfile  A7504441   ROW 2c
     20260902T111116Z_run    185.0    213.2       1       TerrainProfile  A7504441   ROW 2cR (this run)

     Excluded and why: 20260902T011908Z_run (ROW 2) has an orderPushedUtc but NO TASKCMPLT for
     this taskee - the back end had already crashed, 0/3 moved. Runs 20260719T*, 20260723T*,
     20260901T191004Z / 194029Z / 200935Z / 221227Z contain the taskee UUID in captured
     reports but zero TASKCMPLT lines for it (the freeze-era and pre-fix runs). The 5x run
     20260901T230326Z is listed for completeness but is NOT comparable: at timeMult=5 its
     37.0 s of wall clock is ~185 s of simulated time, which is itself consistent with the
     rest of the column, but the comparison is not like-for-like and it is excluded from the
     statistics below.
     STATISTICS over the eight comparable 1x runs: min 178.2, max 198.1, range 19.9 s.
     Over the seven excluding Row 2c: 178.2 to 185.2, range 7.0 s. The claim in Row 2c sec 6
     that "the four-run spread for 114.MechCoy before this run was ... about 3 s" was
     understated - it counted only CONFIRM2 / Row 1 / ROW2R (182.1 / 183.8 / 185.2) and
     omitted the R9 baseline at 178.2 and P2c at 184.6, which were on disk. THE REAL PRE-ROW-2c
     SPREAD WAS 7.0 s, not 3 s. That correction matters: it more than doubles the natural
     spread this unit shows and makes Row 2c's +14.3 s excursion a smaller multiple of the
     known noise than Row 2c's own write-up implied. Recorded as a defect in Row 2c sec 6.
     THE BAND TO USE GOING FORWARD for 114.MechCoy at 1x: 178-199 s observed, so a +/-10 s
     band around ~185 covers everything except Row 2c itself; treat a single excursion to
     ~198 s as within this unit's demonstrated behaviour and require n>=2 before calling any
     future shift on this taskee an effect.
   Neither individual entity shifted (P2), and no OTHER unit shifted, so branch (c) did not
   fire.

P4 - HYGIENE AND SURVIVAL - MET. TASKCMPLT counts 3 (vrfc2simapp.log) and 3
   (reports-captured.log). Endpoints from the trace final POS (t=278.2): 1.BdeHQ
   34.608416,-116.699996 alt 1121.1; 114.MechCoy 34.653915,-116.693388 alt 1116.8;
   1222.MechPlt 34.612956,-116.587783 alt 1026.6 - within 0.2 m of Row 2c and Row 1 (the
   1.BdeHQ longitude differs in the sixth decimal, -116.699996 vs -116.699994 = 0.18 m; the
   other two are identical to all six decimals). Resting altitudes unchanged. POS==RPT
   0.0 / 0.0 / 0.0 m, satisfied x3, reason "post-completion RPT agrees with POS"; settled
   true x3. Three "CreateRoute '<T> ROUTE' (3 pts)" + three "Route '<T> ROUTE' created;
   MoveAlongRoute issued" (VRF_UUIDs b347010c / 730897a1 / 43b8ef04). Six "Create-altitude
   mode=Live" create lines (expected - VrfC2SimService.cs:439 hard-codes "mode=Live" for the
   whole live-like family). earlyExit.fired true; allCompleteUtc 11:16:46.176Z, closedUtc
   11:17:51.036Z -> 64.9 s (band [60, 90]); windowSecsUsed 220.5 of the 420 cap (Row 2c
   236.2); completionLinesSeen 3.
   Back end: bin64-vrfSim.log 11,700 lines, 6,465 of them stamped 07:13 local or later, i.e.
   at or after the order push at 07:13:40 local (11:13:40.178Z); last line "Exception in
   destroyFederationExecution: Federation Execution Already Exists.[Wed Sep  2 07:18:25
   2026]" - the normal teardown tail. NO new .dmp in C:\MAK\vrforces5.0.2\bin64: the newest
   is still the ROW2R-era vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp, 598,441 B,
   2026-09-02 06:00:18 local. AnswerCrashDumpDialog.ps1 was never needed and never run; the
   post-run process sweep found no vrfSim* at all, so no window title to poll.
   Teardown: runner exit 0; StopVrf exit 0 with "VR-Forces is DOWN (graceful quit; no process
   was force-killed)."; post-run Get-Process shows exactly rtiAssistant 41336 / rtiexec
   224608 / rtiForwarder 76620 and nothing else of ours - RTI PIDs unchanged and explicitly
   reported preserved by StopVrf. Both observers took the stop-file path: trace "# STOP
   requested via stop-file at t=309.8s" -> "[OK] resigned cleanly."; ListenReports "stop
   requested via stop-file at t=313.5s ... - disconnecting", "captured 32 reports".
   Censuses: bin64-vrfSim.log "Waiting for nav data" 0 / "empty route" 0 / "Can't find entity
   route" 0 / "invalid formation name" 1 (baseline) = 0/0/0/1, SocketException 0, and ZERO
   lines matching IfRequest / TerrainProfile / terrain profile / IntersectionInformation (the
   back end still logs nothing about the request at notify level 3 - unchanged from ROW2R and
   Row 2c). App log: 3 `fail:` (the C2SIMSDK deserialize noise), 3 "Can't create data of
   type", 0 Exception, 0 `warn:` - identical to Row 1's and Row 2c's census.

FALSIFIER BRANCH TAKEN: none of G1 / G2 / G4. G3 did not fire either - P3 landed cleanly
inside branch (b), not between the branches.

ADJUDICATION AGAINST THE HYPOTHESES (verified vs. inferred):
- VERIFIED: with NOTHING changed, 114.MechCoy completed at +185.0 s and physically arrived at
  trace t=219.2, against Row 2c's +198.1 / 233.3 on byte-identical waypoint altitudes. Two
  runs, same input, 13 s apart.
- VERIFIED: H-ALT is REFUTED as a reproducible effect. A systematic consequence of the ~40 m
  lower waypoints would have to appear in both runs that used them; it appeared in one.
- VERIFIED: H-V STANDS. The aggregate's completion offset varies run-to-run over at least
  178.2-198.1 s at 1x on identical inputs; the two individual taskees vary over 117.1-118.0
  and 129.1-130.1 across the same nine runs. The aggregate is roughly twenty times noisier
  than either individual, which is consistent with (but not proof of) the documented
  mechanism in the sources section: the aggregate's completion is the MAX over its three
  subordinates' arrivals on derived offset routes, governed by a 1 Hz formation monitor whose
  slowdown factor is 0.1x ordered speed. One extra brake cycle on one subordinate is worth
  seconds.
- VERIFIED, AS A CORRECTION: the "about 3 s" prior spread quoted in Row 2c sec 6 was computed
  over three runs when five 1x Live runs were on disk. The true pre-Row-2c spread is 7.0 s
  (178.2-185.2). Row 2c's own "not a comfortable reading" was therefore built on an
  understated baseline.
- RESIDUAL, STATED SO IT IS NOT OVER-READ (adversarial pass): only TWO runs have ever moved
  this aggregate on terrain-authored waypoints - Row 2c (198.1) and Row 2cR (185.0) - against
  six on Live-style altitudes (178.2 / 184.6 / 183.7 / 182.1 / 183.8 / 185.2, all <= 185.2).
  A RARE or INTERMITTENT altitude-triggered effect that fires on some runs and not others is
  therefore NOT excluded by n=2; what is excluded is a systematic one. The falsifier that
  would reopen H-ALT: further TerrainProfile runs drawing ~198 s while Live runs stay at or
  below ~185 s. Nothing in this run's artifacts, and nothing in the vendor documentation read
  for this prereg, supplies a mechanism for such an effect, and the vendor's ground-clamp
  default argues against one - so the ruling stands and reopening it needs new evidence, not
  re-argument.- NOT CLAIMED: WHY Row 2c drew 198.1. Nothing in either run's artifacts identifies the
  subordinate or the event that cost the extra 13 s - the app log carries only the aggregate's
  own TASKCMPLT, and the back end logs nothing about subordinate speed control at notify
  level 3. If that ever needs answering it is a back-end verbosity question, not an interface
  question, and it is NOT on the critical path: the mode works and movement is unaffected.
- CONSEQUENCE FOR THE BRANCH: TerrainProfile mode is FUNCTIONAL and has NO demonstrated
  movement-timing cost. Design sec 7 Row 2 checks 1, 2 and 3 are all MET on this run. The
  open item Row 2c handed the supervisor ("the aggregate's +14 s, docs-first") is CLOSED as
  run-to-run variance of the aggregate.
