# PREREG COA-STP1 RUNG 1 - a BOUNDED scale run on the clean state: do remote-created aggregates build member offset routes at Mojave? - registered 2026-09-02, BEFORE launch

WHAT THIS IS: the first COA-STP1 run since 2026-07-16. All FOUR blocker layers are now
peeled (HANDOFF_2026-09-01_R9_COMPLETE.md "THE FOUR-LAYER BLOCKER STACK": type mapping ->
RealTemplates; the project's own NavArea artifact disabled; the formation-name warning
demoted to cosmetic; route vertices authored from the back end's own terrain, the default
since 5b82e5f). Every COA-STP1 result on the record predates at least one of those. This
run RE-ADJUDICATES the single mechanism the July record hung everything on - the empty
member offset route - at scale, on the real order, at 1x.

IT IS NOT AN ACCEPTANCE RUN. The window (2700 s at TimeMultiplier 1) is far too short for
any performer to finish its route: the SHORTEST route head is 24.09 km and observed ground
column pace is 8.6-10 m/s, so the fastest possible along-route progress in the window is
~23-27 km. ZERO TASKCMPLT from route completion is the EXPECTED outcome and is NOT a miss.
What the window CAN settle is the mechanism (P3) and the shape of the first 45 minutes of
movement (P4) - including whether the CPP-ALT-1 "stop on a common ~18.4 km radius"
signature reproduces, which at 8.6 m/s is exactly reachable inside 2700 s.

## 0. Sources read for this prereg (docs first, per the 2026-09-01 directive)

VENDOR (local install; the public class ref adds nothing this run needs):
- `C:\MAK\vrforces5.0.2\include\vrfmodel\disaggregatedMoveAlongController.h` - the class
  that produces or fails to produce the offset routes this run is about. Contributed the
  MECHANISM statement for P3: ":36-46 Movement is implemented by creating temporary working
  routes for each subordinate, positioned at an offset needed to maintain that subordinate's
  position in formation ... The movement task is considered complete when all subordinates
  have reached the end of the route and have issued task complete reports to the aggregate.
  At that point, the aggregate destroys the temporary working routes"; ":222-231
  generateFormationRoutes(route, reverseDirection) - Generate routes for subordinates to
  follow, at offsets corresponding to their position in formation ... return ... false if it
  is still waiting for data"; ":291-317 buildOffsetRoute(... bool& dataAvailable) - return
  true if offset route was built successfully, false if not"; ":398-399 shouldGroundClamp()
  - whether or not generated route vertices should be ground clamped. So (a) ONE offset
  route per taskable subordinate is the documented normal, which is what P3 counts; (b) the
  controller can legitimately return "still waiting for data" - a transient, not a failure;
  (c) the generated vertices are ground-clamped by the controller itself, which is why the
  TerrainProfile authoring of the PARENT route is the thing under test, not the members'.
- `C:\MAK\vrforces5.0.2\appData\settings\vrfSim\vrfSim.mtl:205,:208` - `(setqb notifyLevel 3)`
  and `(setqb objectConsoleNotifyLevel 3)`. VERIFIED IN PLACE, file mtime 2026-09-01 15:32,
  backup vrfSim.mtl.bak-20260901 beside it. NOT CHANGED by this prereg. This is what makes
  P3 observable at all: at level 3 the back end prints
  `Locally Simulated: <member>'s Offset Route (VRF_UUID:...) using parameters:
  ..\data\simulationModelSets\base\vrfSim\Route.entity` - 65 such lines are present in the
  current bin64\vrfSim.log from the 2026-09-02 07:36 R9 run, so the channel is PROVEN live,
  not merely configured.

PROJECT RECORD:
- `docs/OPUS_EXECUTION_PLAN.md` Step 5 (:704-790). Contributed: the R11 VACUOUS-COMPLETION
  TRAP that governs every movement claim here (":5.3 MOVEMENT is claimed ONLY from WatchVrf
  per-object displacement ... A TASKCMPLT with no corresponding WatchVrf displacement is NOT
  movement"); the P4a socket-error greps (`Only one usage of each socket address`,
  `Connection error:`, PASS = ZERO); the ClientId=C2SIM requirement; and the expected
  non-completions list. Its 5.1 CONFIG IS NOT FOLLOWED HERE and that is deliberate: 5.1
  prescribes fan-out ON, AggregateFormation=auto and 20x, all three of which would confound
  the mechanism question. The plan's own header says its config is retained for provenance.
- `docs/experiments/COA-STP1_scale_2026-07-13.txt` - the only full-order run with a complete
  artifact census. Contributed EVERY log-line signature P1/P2/P5 predict, and the counts to
  predict against: `Init dispatched: 128 units + 35 areas queued for creation.`;
  `DeStack (R8): 54 units at (34.67998497486787,-116.72479854165415) spread onto 50 m rings
  (first unit kept in place).` plus 9 more 2-unit groups = 10 groups; reflected 0 -> 1769
  within ~2 min, peak 1797, readable 1783; `NO LOCATION GIVEN - CAN'T EXECUTE`;
  `predecessor was skipped/abandoned upstream` vs `did not complete within 600s of its
  dispatch; policy=skip`; T13 NEVER dispatched (its 3h20m delay); `Cleanup: deleting 178
  created VR-Forces objects` = 128 units + 35 areas + 15 routes; ResetVrf afterwards still
  found 1 deletable leftover. ALSO the confound warning: that run had fan-out ON,
  AggregateFormation=auto and 20x, so its F1/F2b findings do NOT transfer to this config.
- `docs/experiments/F3_probe_2026-07-13.txt` - read for the 600==600 timeout-race finding
  (straggler synthesis racing the successors' predecessor windows). NOT APPLICABLE here:
  FanOutStragglerSeconds defaults to 0 and fan-out is off, so there is no straggler timer to
  race. Recorded so the absence is deliberate, not an oversight.
- `docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md:1475-1600` - the two July-16
  probes this run is the successor to. COA-DEMO-1 (apps 3448-3450; fan-out OFF, 20x, skip/600
  - the CLOSEST configuration on the record to this one): "128/128 born safe-MSL, 10 pile
  groups de-stacked, 113 aggregate creations"; "Dispatch: 9 chain-head tasks (routes + moves),
  5 abandons, 31 successors still predecessor-gated at stop, and ONLY 1 completion in ~13
  min"; "38 movers ... BUT: top excursion 541 km (net 289 km), several 100-260 km,
  terminations UNDERGROUND (-1305 m, -1680 m) and at sea level (offshore), 9 out-and-back
  signatures"; "YELLOW WARNING TRIANGLES on most units ... badge UNIDENTIFIED". CPP-ALT-1
  (the frozen C++ oracle at 1x, apps 3451-3454): "6 of the 9+ tasked units marched 18.1-18.4
  km cumulative at ~31 km/h column pace, altitudes ON TERRAIN throughout (1002-1097 m)"; "5
  tasked units ... never moved off the pile (cum 0 m, alt 1137.1)"; "all 6 marchers STOPPED
  (window movement 5-24 m) at 18.1-18.4 km from origin - 10-17 km SHORT of their ordered
  final waypoints (T1: 10.2 km short; T19: 10.2; T15: 17.1)", cause NOT adjudicated (paged-
  terrain tile boundary vs a shared intermediate-waypoint stall). CPP-ALT-1 is the direct 1x
  comparator for P4 and the source of the 8.6 m/s cruise figure.
- `docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md` sec 6 (:203-213,
  :287-290, and the "Branch selected" block). Contributed the FALSIFICATION this run builds
  on: an authored Tank Platoon at THIS AO engaged the same buildOffsetRoute path R9 reported
  empty, with above- AND below-terrain waypoints. It also contributed the scoping discipline
  this prereg copies: "WHAT REMAINS (honest scoping - do NOT collapse to 'structure
  proven')". Retracted into docs/CORRECTIONS_LOG.md this session (commit fc93a1e).
- `docs/SEMANTIC_MAPPING.md` + `src/VrfC2SimApp/VerbMapping.cs` - what the app does with each
  verb. Every one of this order's head verbs (PENTRT/DESTRY/ATTACK/BREACH/SECURE/DEFEND) maps
  to an intent whose Layer 2 is NOT wired, so all of them execute BARE MOVEMENT and log
  `verb=<CODE> -> intent=<Intent> (<composition>); Layer-2 not yet wired - executing bare
  movement.` Only SCREEN/SCOUT (Reconnoiter -> patrol) and ESCRT (Escort -> follow) diverge,
  and NO head task carries either. Combined with the 42/42 self-target fact, no FireAtTarget,
  Breach or Follow can be issued by this order.
- `docs/RUNBOOK.md` 0.5.0 (pre-flight inventory; refuse on a pre-existing vrfSim/vrfGui/
  vrfLauncher; -AllowExistingVrf is the false-READY trap; leave the RTI trio running),
  0.5.9 (StopVrf is the teardown, never "close the front-end"), 0.5.11 (the runner's
  turnaround switches; -StopWhenComplete + SettleHoldSecs 60 + rule 4), 0.5.12 (the crash-
  dump prompt recipe), and :1171-1185 (SOLUTION A IS NOT COMPLETE CLEANUP - after a COA-STP1
  run that dispatched 168 deletes and resigned clean, ResetVrf STILL found 2 leftovers; the
  runner does NOT run ResetVrf, so this prereg runs it by hand afterwards).
- `docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md` sec 6/7 + `PREREG_TERRAIN_ROW3_
  DEFAULT_2026-09-02.md` sec 6 - the mode this run inherits and the fact that the app never
  logs the mode STRING, only its EFFECT (the terrain request/reply/authoring lines). The
  create-altitude line hard-codes the text `mode=Live` for the whole live-like family and is
  NOT a mode readout.

SOURCE READ THIS SESSION (all line numbers verified in the working tree at commit fc93a1e):
- `VrfC2SimApp/VrfSettings.cs` :22 ClientId default "STP"; :42 TypeMappingMode
  "RealTemplates"; :55 AggregateFormation "" (OFF); :65 DeStackCreates false; :70
  DeStackSpacingMeters 50; :80 SubordinateFanOut false; :95 FanOutStragglerSeconds 0; :117
  TimeMultiplier 1; :123 TaskPredecessorTimeoutSeconds 600; :132 PredecessorTimeoutPolicy
  "skip"; :181 GroundWaypointAltitudeMode "TerrainProfile"; :183 TerrainClearanceMeters 10;
  :184 TerrainProfileTimeoutSeconds 10; :195 CreateAltitudeSafeMslMeters 10000.
- `VrfC2SimApp/VrfC2SimService.cs` :434-442 create-altitude branch (every GROUND unit is
  created at 10000 m MSL and logs one line); :463-467 the DeStack line; :516 "Init
  dispatched"; :716 `isGround = SymbolId[2] == 'G'`; :741-744 point 0 = the unit's LIVE
  location, PREPENDED to the task's own Locations; :763-770 zero-Locations -> `NO LOCATION
  GIVEN - CAN'T EXECUTE`; :783-812 the TerrainProfile request/defer/re-entry; :902-914 the
  single-point MoveToLocation branch; :955-993 CreateRoute + deferred MoveAlongRoute.
- `VrfC2SimApp/TaskSequencer.cs` (whole file) + `:576-600` in the service - the gate.
- `scripts/RunC2SimScenario.ps1` :1143-1166 the ClientId check reads appsettings.json ONLY;
  :1077 -RunSecs must be 30..86400; :1438-1450 the runner allocates EXACTLY 7 appNumbers from
  the single Appendix B marker and advances it; :2349-2360 it copies bin64\vrfSim.log and
  vrfGui.log into the run directory as bin64-*.log; :1899 it injects ONLY
  Vrf__ApplicationNumber. There is NO TimeMultiplier or Vrf__* capture anywhere in the
  script, so any Vrf__ variable set by hand must be echoed into the console log to be on the
  record - this prereg does that.

## 0b. Order/init facts RE-DERIVED from the XML this session (the brief's numbers, checked)

Everything below was parsed out of `data/COA-STP1_Order.xml` and
`data/COA-STP1_Initialization.xml` this turn, not inherited from prose.

CONFIRMED: 42 ManeuverWarfareTask; 31 ActionTemporalRelationship, all STREND (NOT 32 - fixed
in commit fc93a1e); 11 chain heads T1,T5,T9,T13,T15,T19,T23,T27,T31,T35,T39; 42/42
self-target; 9 tasks with ZERO Locations (T8,T9,T10,T16,T21,T24,T34,T37,T38); T13 alone
carries a start delay (SimulationTime P00Y00M00DT03H20M00S = 12,000 s, Order.xml:504) and no
task carries a nonzero RelativeTime; route lengths T17 42.37 / T39 40.20 / T15 35.55 /
T23=T31=T35 28.71 / T1=T19 28.53 / T32 23.60 / T13=T36 0.63 km; 128 Units + 35 TacticalAreas;
61 friendly / 67 hostile; all 128 ground (SIDC char 2 = 'G'); all SISOEntityType zero; SIDC
echelon char census 'E' 64 / 'F' 26 / 'D' 23 / other 15; 54 units share
34.67998497486787,-116.72479854165415, and exactly 10 coordinate groups have more than one
unit (one of 54, nine of 2).

CORRECTED - the brief's route-head list is INCOMPLETE and the correction changes P3/P4:
- The brief names 6 aggregate route heads (T1,T15,T19,T31,T35,T39) + 1 entity (T23). It omits
  **T5 and T27**, which are ALSO aggregate heads with REAL long moves. Their single Location
  is not near the spawn: T5's point is 33.54 km from 4-27/2/1_A (bearing 202) and T27's is
  24.09 km from 856/HHC (bearing 261). 856/HHC in fact marched 22,474 m in the July scale run.
- MoveToLocation is UNREACHABLE for this order. `routeGeo` is seeded with the unit's live
  position BEFORE the zero-Locations check, so a task that survives that check always has
  >= 2 points. T5 and T27 therefore take CreateRoute + MoveAlongRoute with a 2-point route,
  exactly like the 4-vertex heads. Predicting `MoveToLocation` lines would be wrong.
- So the DISPATCHING set is 9, not 7: **8 aggregates** (T1, T5, T15, T19, T27, T31, T35, T39)
  + **1 entity** (T23, 1-1/2/1_AD, a lone Tank).
- Per-performer template, re-derived from each performer's own SIDC via UnitTranslator's
  echelon dispatch:

      head  performer     SIDC             ech  template emitted (RealTemplates)
      T1    1-35/2/1_A    SFGPUCA----F---   F   ArmorCoHQ    11.1.225.5.20.0.0
      T5    4-27/2/1_A    SFGPUCF----F---   F   ArmorCoHQ    11.1.225.5.20.0.0
      T9    A/6-56/HHC    SFGPUCD--------   -   Tank ENTITY   1.1.225.1.1.3.0
      T13   510/40        SFGPUCE----E---   E   ArmorCompany 11.1.225.5.2.0.0
      T15   1-6/2/1_AD    SFGPUCI----F---   F   ArmorCoHQ    11.1.225.5.20.0.0
      T19   40/2/1_AD     SFGPUCE----F---   F   ArmorCoHQ    11.1.225.5.20.0.0
      T23   1-1/2/1_AD    SFGPUCR--------   -   Tank ENTITY   1.1.225.1.1.3.0
      T27   856/HHC       SFGPUULM---E---   E   ArmorCompany 11.1.225.5.2.0.0
      T31   5-20/2/1_A    SFGPUCIZ---F---   F   ArmorCoHQ    11.1.225.5.20.0.0
      T35   B/5-20        SFGPUCIZ---E---   E   ArmorCompany 11.1.225.5.2.0.0
      T39   C/1-35        SFGPUCA----E---   E   ArmorCompany 11.1.225.5.2.0.0

  NOTE: NOT ONE performer is an ArmorPlatoon ('D'), so the 2026-07-22 type fix - whose only
  branch is ArmorPlatoon - does NOT touch any performer directly. It touches 23 of the 128
  units, none of them taskees. This is a REAL limit on what this run can attribute and it is
  recorded here, before the run, not discovered afterwards.
- All 11 performers sit in the 54-unit pile at the shared spawn coordinate.
- Route vertex 0 of T1/T15/T19/T23/T31/T35/T39 IS the spawn coordinate, so after the live
  point-0 prepend those routes carry 5 points, the first two ~0-100 m apart (de-stack ring
  offset). T5/T27 carry 2 points.

## 1. Configuration - the "July configuration" as it exists on the CLEAN state

ONE deliberate deviation from stock defaults: `Vrf__DeStackCreates=true`. Everything else is
the compiled default. Rationale: the 54-unit pile costs ~13 minutes to escape by gridlock
alone (R8 record, START_HERE :359-368), which would consume a third of the window and
confound P4 for every performer. De-stack is July hygiene, was ON in every July COA-STP1 run,
and moves units at most ~150 m off their source coordinates.

    TimeMultiplier                  1        (default, NO env - user ruling 2026-09-02)
    Vrf__DeStackCreates             true     (THE ONE ENV VAR; spacing default 50 m)
    SubordinateFanOut               false    (default) - aggregates are tasked AS UNITS.
                                             This is the whole point: fan-out would BYPASS
                                             the offset-route path P3 measures.
    AggregateFormation              ""       (default = OFF). No RequestAvailableFormations,
                                             no "R1: created aggregate" lines, no create-time
                                             formation set. Layer 3 is cosmetic (P2c ruling).
    FanOutStragglerSeconds          0        (default) - no straggler timer, so the July F3
                                             600==600 race cannot occur.
    PredecessorTimeoutPolicy        skip     (default), TaskPredecessorTimeoutSeconds 600
    TypeMappingMode                 RealTemplates (default)
    GroundWaypointAltitudeMode      TerrainProfile (default since 5b82e5f), clearance 10 m,
                                    timeout 10 s
    CreateAltitudeSafeMslMeters     10000    (default)
    BundlePositionReports           false    (default) - keeps the P4a socket verdict clean
    appsettings Vrf:ClientId        "C2SIM"  (TRACKED EDIT, committed with this prereg;
                                             REVERTED to "STP" in the outcome commit)

`Get-ChildItem env:Vrf__*` is echoed into the run console log immediately before launch and
must show `Vrf__DeStackCreates=true` and NOTHING ELSE.

## 2. Invocation

    $env:Vrf__DeStackCreates = 'true'
    Get-ChildItem env:Vrf__*            # echoed into the console log
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init  data/COA-STP1_Initialization.xml `
        -Order data/COA-STP1_Order.xml `
        -RunSecs 2700 -SampleSecs 10

`-StopWhenComplete` is DELIBERATELY OMITTED: it closes the window only when ALL 11 taskees
have TASKCMPLT, which this order cannot reach in 2700 s (and 2 of the 11 - T9's and T13's
performers - are never even tasked). With it set the runner would just log the WARN and run
to the cap anyway; omitting it keeps the manifest honest. `-SampleSecs 10` (default 2) is a
MEASUREMENT parameter: ~270 samples x ~1785 objects is already a large CSV.

Foreground, 75-minute timeout (2700 s window + ~9 min of stage overhead; the July scale run's
launch->order-push was 4 min 20 s and the runner is faster).

LEDGER: marker reads `*** NEXT FREE: 3718 ***` at docs/OPUS_EXECUTION_PLAN.md:1603, VERIFIED
as the only value-bearing marker in the file. The runner allocates EXACTLY 7 (vrfBackend,
vrfFrontend, oraclePre, oracleTrace, app, rtiProbe, createOneDiag) and advances the marker
itself, so expected wasValue 3718 / newValue 3725, appNos 3718-3724. createOneDiag (3724) is
consumed ONLY if the stage-7 oracle gate fails.

AFTER the run: `Remove-Item env:Vrf__DeStackCreates`, then `tools/ResetVrf` (the documented
authoritative sweep, RUNBOOK :1171-1185 - the runner does NOT run it), output recorded.

PREREG COMMIT: the predictions below are registered in the commit named on the last line of
this file, BEFORE launch. That line is the only content added afterwards.
REGISTERED IN COMMIT d1f2e10 (2026-09-02), before any process was launched.

## 3. Pre-launch inventory (VERIFIED 2026-09-02 before registration; must still hold, else STOP)

- NO vrfSim* / vrfGui / vrfLauncher / WatchVrf / ListenReports / VrfC2SimApp process.
  VERIFIED: the only matching processes are the RTI trio.
- RTI trio RESIDENT and ANSWERED: rtiAssistant 41336 (9 threads, up since 2026-09-01 14:34),
  rtiexec 224608, rtiForwarder 76620. NEVER killed.
- Docker: `stp-server` Up 20 hours (healthy), `c2sim_server4.8.4.9` Up 20 hours.
- NavArea DISABLED. The live path is
  `C:\MAK\SharedData\16\latest\TerrainData\navData` (NOT under vrforces5.0.2 - the handoff's
  path is wrong and is corrected here). `navData\MAK Earth Space (online)\` - the directory
  for the terrain this scenario runs - contains **0 files**. The artifact sits in
  `navData\_disabled_20260901\` (120,006 files, incl. `NavArea-ground-platform 1`,
  `.navGenConfig`, `.navGenConfig.generated`, `.navRuntimeConfig`), mtime 2026-09-01 15:40.
- VrfBridge.dll = A7504441 on 10/10 main-checkout copies (SmokeTest, VrfBridge/build/Release,
  VrfC2SimApp, CreateOne, CreateTaskAgg, ResetVrf, RtiProbe, RunSim, SetSimRate, WatchVrf).
  The other hashes on disk (A48ABE6C x5, 4286B64D x2) are all inside `.claude/worktrees/`,
  not the main checkout.
- vrfSim.mtl notify levels 3/3, unchanged.
- No back end parked on a dump prompt. Seven historical .dmp files sit in bin64 (2023-12,
  2026-07-14/15/22, and 70668 from 2026-09-02) - do not confuse them with a new one.

## 4. PREDICTIONS - registered before launch

Adjudicated from run-directory artifacts ONLY: `vrfc2simapp.log`, `bin64-vrfSim.log`,
`watchvrf-trace.csv`, `reports-captured.log`, `run-manifest.json`, `console-rung1.log`.

### P1 - CREATION (confidence HIGH)

P1.1 `pushinit.stdout.log` reports `QUERYINIT: 128 Units` with `SystemName=[C2SIM]`.
P1.2 App log contains EXACTLY ONE
     `Init dispatched: 128 units + 35 areas queued for creation.`
     and ZERO `0 of 128 units matched Vrf:ClientId` lines.
P1.3 EXACTLY 128 lines matching `Create-altitude mode=Live: GROUND unit .* created at safe
     MSL 10000 m` (one per unit; all 128 are ground). The literal text "mode=Live" is a
     KNOWN hard-coded string covering the whole live-like family and is NOT evidence of the
     mode - see sec 0. It is used here only as a per-unit creation counter.
P1.4 EXACTLY 10 `DeStack (R8):` lines - one `54 units at
     (34.67998497486787,-116.72479854165415) spread onto 50 m rings` plus nine `2 units at
     ...`.
P1.5 ZERO `R1: created aggregate` lines (AggregateFormation is OFF - this is the correct
     value, not a fault; the July run's 113 R1 lines came from `auto`).
P1.6 Template census in `bin64-vrfSim.log`, counting
     `using parameters: ...\<T>.entity` occurrences:
       `Tank Company (USA).entity`             = 64   (echelon 'E')
       `Tank Headquarters Section (USA).entity`= 26   (echelon 'F')
       `Tank Platoon (USA).entity`             = 23   (echelon 'D', the type-fix template)
       `M1A2_Abrams_MBT.entity`                >= 15  (the 15 lone Tanks PLUS every
                                                       aggregate member; not a clean count)
     113 aggregates + 15 entities = 128. SECONDARY evidence: if the back end truncates or
     rolls the log this becomes UNSCORED, not MISSED.
P1.7 Runner stage-7 oracle gate PASSES (a POS line with real lat/lon), so appNo 3724
     (createOneDiag) goes UNCONSUMED.
P1.8 `watchvrf-trace.csv` reflected-object count rises from ~0 and plateaus in the band
     1700-1850 within ~150 s of the init push. (July: 1769 at ~2 min, peak 1797, readable
     1783.)

### P2 - DISPATCH (confidence HIGH)

P2.1 EXACTLY 9 head tasks reach VR-Forces, each with a `CreateRoute '<TaskName> ROUTE'
     (N pts) for <unit>; move deferred to route-created.` line:
     T1(5), T5(2), T15(5), T19(5), T23(5), T27(2), T31(5), T35(5), T39(5).
     ZERO `MoveToLocation` lines (unreachable for this order - sec 0b).
     ZERO `patrol deferred` / PatrolRoute lines (no SCREEN head).
P2.2 T9 logs `NO LOCATION GIVEN - CAN'T EXECUTE TASK 'T9_ProvideAirDefense...'` and nothing
     else. T13 logs NOTHING at all in the window (still inside its 12,000 s delay at stop).
P2.3 EXACTLY 31 gate-skip WARN lines, all reading `-> NOT dispatched.`, split:
       10 x `did not complete within 600s of its dispatch; policy=skip`
          = the second link of each of the 9 dispatched chains, plus T14 (whose predecessor
            T13 never dispatches at all - phase-1 timeout);
       21 x `was skipped/abandoned upstream`
          = links 3 and 4 of those 9 chains (18) plus T10, T11, T12 (T9's chain, which
            cascades immediately because T9 was abandoned at t~0).
     TIMING: the 21 abandoned-upstream lines for T9's chain appear within seconds of the
     order push; the 10 timeouts cluster at t = order-push + ~600 s; the remaining 18
     abandoned lines follow each timeout within ~1 s.
     THIS IS EXPECTED BEHAVIOR, NOT A MISS. Under policy=skip an in-flight head task is
     never disturbed: DEFECT B is fixed (P0.2), and with no retask there is no supersession,
     so ZERO `SUPERSEDES in-flight task` lines and ZERO `policy=force` behaviour.
P2.4 ZERO `NO in-flight task recorded` lines and ZERO empty-uuid TASKCMPLT.
P2.5 Layer-2 accounting, from the 42/42 self-target fact: ZERO FireAtTarget, ZERO Breach,
     ZERO FollowEntity issued. Expect `ATTACK task '...': affected entity is the taskee
     itself (self-target fire-support?); no fire, advancing only.` for the Attack-intent
     heads (T1 PENTRT, T5 DESTRY, T15 ATTACK, T23 PENTRT, T31 ATTACK, T35 ATTACK, T39
     ATTACK = 7) and `BREACH task '...': affected obstacle '...' not resolvable to a
     distinct VRF unit; advancing only, no breach.` for T19.
P2.6 TASKCMPLT count: **0 expected from route completion** (sec "IT IS NOT AN ACCEPTANCE
     RUN"). ANY TASKCMPLT that does appear is scored against the R11 trap: it is a VACUOUS
     completion unless watchvrf shows the performer within 100 m of its route end.

### P3 - THE MECHANISM (the re-adjudication; confidence HIGH; this is the run's purpose)

P3.1 `bin64-vrfSim.log` contains **ZERO** occurrences of
     `empty route -- not sending move along to subordinate`
     and ZERO of `moveAlong() - empty route`.
P3.2 For EACH of the 8 dispatching AGGREGATE heads (T1, T5, T15, T19, T27, T31, T35, T39)
     `bin64-vrfSim.log` shows `Locally Simulated: <member>'s Offset Route (VRF_UUID:...)
     using parameters: ...\Route.entity` lines, count > 0 per aggregate and approximately
     equal to that aggregate's taskable subordinate count (documented: one working route per
     subordinate, disaggregatedMoveAlongController.h:36-46/:222-231). TOTAL offset-route
     creations across the run: > 50.
     ATTRIBUTION LIMIT, stated in advance: member names are VRF-assigned and appear nowhere
     in the app log, so per-aggregate attribution of an offset route is by member-name family
     and creation timestamp, NOT by uuid. Where that is ambiguous the count is reported as a
     RUN TOTAL only, and P3.2 is scored on the total plus at-least-one-per-aggregate.
P3.3 App log: 9 `terrain profile request {Id} sent for {N} vertices` lines (N=5 for the seven
     4-vertex heads, N=2 for T5 and T27), 9 matching `Terrain profile reply {Id}: {N}
     sample(s)` with the SAME N, and 9 `all {N} vertices authored from terrain + 10 m
     clearance; alts [...]`. ZERO `partial`, ZERO `keep the Live altitude`, ZERO `terrain
     profile request not sent`, ZERO timeout fallbacks. (Rows 2c/2cR proved this path on
     3 routes x 3 vertices; this is its first test at 5 vertices and at 9 concurrent
     requests.)
P3.4 The authored altitudes in P3.3 are within ~15 m of terrain (clearance 10 m) and lie in
     the 950-1500 m MSL band for this AO. No authored vertex at 100 m (the Fixed100 relic)
     and none at ~10000 m (the create altitude).

### P4 - MOVEMENT (confidence MEDIUM; telemetry-gated, R11 trap governs)

Along-route distance is measured from `watchvrf-trace.csv` by projecting each performer's
own POS samples onto its authored polyline. Aggregate POS is the aggregate's own reflected
position; member clustering is reported as corroboration, never as attribution.

P4.1 At least 6 of the 9 dispatching performers leave the pile (net displacement > 1 km) and
     show MONOTONE along-route progress. (CONFIDENCE MEDIUM. Comparators disagree:
     CPP-ALT-1 at 1x got 6 of ~9 marching and 5 frozen; COA-DEMO-1 at 20x got 38 movers but
     off-route. Both predate all four fixes.)
P4.2 Every performer that moves does so at >= 3 m/s average once clear of the pile and
     reaches >= 5 km along-route by t=2700 s. (At the documented 8.6-10 m/s column pace,
     2700 s buys 23-27 km; 5 km is a deliberately loose floor that still excludes shuffling.)
P4.3 NO RUNAWAY. Falsifiable bound, fixed now: every reflected object's final position lies
     inside lat [34.15, 34.95] x lon [-117.10, -116.25]. That box is the init UNIT extent
     (lat 34.2154..34.8810, lon -117.0078..-116.3279) plus ~7 km. All four July
     monster-mover endpoints - (33.3844,-117.6329), (35.9984,-116.5029), (33.7439,-115.7002),
     (33.9249,-115.9121) - fall OUTSIDE it, so the box discriminates. Additionally: no mover
     is more than 2 km laterally from its own route corridor, and none is beyond its route
     end + 2 km.
P4.4 NO UNDERGROUND / OFFSHORE TERMINATION. No sampled altitude below 500 m MSL and none
     within 50 m of 0 m. (July: terminations at -1305 m and -1680 m, and at sea level.)
     Terrain here is ~1000-1400 m; CPP-ALT-1's marchers held 1002-1097 m throughout.
P4.5 THE 18.4 km RADIUS - a NAMED, REACHABLE test. At 8.6 m/s, 18.4 km takes 2140 s, so a
     performer that starts within ~9 minutes of the order push CAN reach it inside the
     window. PREDICTION: performers pass 18.5 km and keep moving. The CPP-ALT-1 signature
     REPRODUCES if two or more performers come to rest (< 25 m of movement over >= 300 s)
     at 18.1-18.4 km great-circle from the spawn while their route continues beyond. That
     would be a MISS of this prediction and a MAJOR finding (it would mean the signature is
     codebase- AND layer-independent); a stop at any OTHER common radius is also recorded.
     No performer's route ends near 18.4 km (the nearest end is T27 at 24.09 km), so a stop
     there cannot be mistaken for an arrival.
P4.6 THE YELLOW BADGE from COA-DEMO-1 stays UNIDENTIFIED and is NOT scored: this run is
     headless and no one is looking at the GUI. Recorded so its absence is not read as a fix.

### P5 - HYGIENE (confidence HIGH)

P5.1 The back end SURVIVES: no vrfSim* process with a `^vrfSim.*\.dmp$` title, no new .dmp in
     bin64, StopVrf exit 0.
P5.2 ZERO `Only one usage of each socket address` and ZERO `Connection error:` in the app log
     (the P4a live gate; BundlePositionReports stays false).
P5.3 Runner exit 0; every stage exit 0; `run-manifest.json` records wasValue 3718 /
     newValue 3725 and the marker in OPUS_EXECUTION_PLAN.md reads `*** NEXT FREE: 3725 ***`
     afterwards.
P5.4 `Cleanup: deleting 172 created VR-Forces objects before resign...` - 128 units + 35
     areas + 9 routes. (July: 178 = 128 + 35 + 15 routes, with 15 tasks dispatched.) A
     different number is scored by re-deriving it from the actual CreateRoute count, not by
     widening the band.
P5.5 `tools/ResetVrf` afterwards joins clean (proving no stale federate) and finds a SMALL
     number of leftovers - 0 to 3 - which it deletes. Any larger number is recorded as a
     Solution-A gap, not as a run failure (RUNBOOK :1171-1185 documents this class).

## 5. FALSIFIERS - what STOPS the rung

F1  ANY `empty route -- not sending move along to subordinate` line, OR zero member offset
    routes for any dispatching aggregate head. -> The July mechanism is STILL LIVE under the
    clean state, with the type mapping, the NavArea and the vertex frame all corrected. STOP.
    Next step is DOCS-FIRST (disaggregatedMoveAlongController + the aggregate move-along
    chapter, then a MAK question), explicitly NOT a retune of this config.
F2  ANY runaway per P4.3, or any underground/offshore termination per P4.4. -> STOP and
    record WHICH unit and WHICH template, because the July record could never attribute them
    (member uuids are VRF-assigned). Do not re-run.
F3  Creation census off: P1.2, P1.3, P1.4 or the 113/15 split wrong. -> STOP; the run is not
    the run that was registered.
F4  Back end crash / dump prompt. -> `scripts\AnswerCrashDumpDialog.ps1` then
    `scripts\StopVrf.ps1` (RUNBOOK 0.5.12), then STOP and report.
F5  Two infrastructure failures. -> STOP and report (standing rule).

NOT falsifiers, stated in advance so they are not retro-fitted: zero TASKCMPLT; the 31 skip
lines; T13 never dispatching; T9 abandoning for no Locations; the absence of R1/fan-out/
straggler lines; the yellow badge being unobserved.

## 6. OUTCOME

(to be appended after the run: verdict first, then P1-P5 MET/MISSED with verbatim lines and
counts, the per-performer table, verified vs inferred, and anything unexplained)
