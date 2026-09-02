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

## 6. OUTCOME - run 20260902T125423Z, appNos 3718-3724, adjudicated from run-directory artifacts

### VERDICT

THE JULY MECHANISM IS GONE; THE FREEZE IS NOT - AND IT IS NOW SILENT.

Not one `moveAlong() - empty route -- not sending move along to subordinate` line appears
anywhere in 140,902,719 bytes of back-end log. The grep oracle that the whole July region
story rested on ("THE grep oracle for this failure mode", UNIT_MOVEMENT_RESEARCH.md:327) is
DEAD as a diagnostic: it fires on nothing here, yet FOUR of the EIGHT dispatching aggregates
built ZERO member offset routes, never moved a metre, and the back end said NOTHING about it.

The other four built offset routes and MARCHED: 13.2 to 26.7 km of real, monotone,
route-conforming progress at 8.0-8.2 m/s, still moving when the window closed. That is the
first time COA-STP1 aggregates have been observed marching their own order's routes at 1x.

Member offset routes and movement correlate 1:1 across all eight - every aggregate that built
them moved, every aggregate that did not build them froze. So offset-route construction is
still the proximate mechanism; what changed is that its failure is no longer announced.

TEMPLATE IS NOT THE DISCRIMINATOR. Both template classes contain movers and freezers:
Ground_Aggregate 3 moved / 2 froze; Tank Company (USA) 1 moved / 2 froze. Whatever selects
the freezers, it is not the type-mapping gap.

FALSIFIERS: F1 did NOT fire (P3.1 clean). F2 did NOT fire (no clean-history object left the
AO; nothing underground or offshore). F3, F4, F5 did not fire. ONE NEW FALSIFIER-CLASS
FINDING, outside the registered set: the lone ENTITY taskee reported TASKCMPLT from the
back end's own callback while never leaving its spawn ring - a true vacuous completion that
falsely released its successor (finding A below).

### Run facts

Run dir `runs/20260902T125423Z_run`. Ledger 3718 -> 3725, seven numbers claimed before any
join; 3724 (createOneDiag) allocated but UNCONSUMED - the stage-7 oracle gate passed, so the
stage-7b diagnostic never ran. Every stage exit 0 (RtiProbe, LaunchVrf, WatchVrf-precheck,
WatchVrf-trace, ListenReports, PushInit, VrfC2SimApp, PushOrder, StopIface, StopVrf); runner
exit 0. Observation window 2704.5 s of 2700 s. `Get-ChildItem env:Vrf__*` before launch =
`Vrf__DeStackCreates=true` and nothing else; empty afterwards. Artifacts: vrfc2simapp.log
54,254 B; bin64-vrfSim.log 140,902,719 B; watchvrf-trace.csv 46,844,395 B (473,057 usable POS
samples over 1,732 objects); reports-captured.log 5,704,098 B (128 distinct reporting uuids =
exactly the 128 created units).

### PER-PERFORMER TABLE

Along-route distance = the performer's own POS series (reports-captured.log, keyed by C2SIM
uuid) projected onto its authored polyline. Offset routes attributed from bin64-vrfSim.log by
`follow-in-formation: leader=<M>; leaderRoute=<task route>`, which names the task directly.

  head perf.        template            disp?  offRt  empty  1stMov  along@2700s  maxLat  finalAlt  moving at close
  ---- ------------ ------------------- -----  -----  -----  ------  -----------  ------  --------  ---------------
  T1   1-35/2/1_A   Ground_Aggregate    YES        4      0    134s     13.39 km   123 m   ~1128 m  YES 2458 m/300s
  T5   4-27/2/1_A   Ground_Aggregate    YES        0      0       -      0.09 km    37 m   ~1147 m  NO  0 m/300s
  T15  1-6/2/1_AD   Ground_Aggregate    YES        4      0     70s     26.70 km    57 m   ~1296 m  YES 2390 m/300s
  T19  40/2/1_AD    Ground_Aggregate    YES        4      0     71s     13.17 km   150 m   ~1128 m  YES 2454 m/300s
  T23  1-1/2/1_AD   Tank ENTITY         YES      n/a      0       -      0.18 km    78 m   ~1147 m  NO  0 m/300s
  T27  856/HHC      Tank Company (USA)  YES        0      0       -      0.00 km   150 m   ~1147 m  NO  0 m/300s
  T31  5-20/2/1_A   Ground_Aggregate    YES        0      0       -      0.00 km   150 m   ~1147 m  NO  0 m/300s
  T35  B/5-20       Tank Company (USA)  YES        0      0       -      0.00 km   100 m   ~1147 m  NO  0 m/300s
  T39  C/1-35       Tank Company (USA)  YES       18      0     61s     24.20 km    86 m    ~941 m  YES 2432 m/300s
  T9   A/6-56/HHC   Tank ENTITY         NO       n/a      0       -            -       -         -  no Locations
  T13  510/40       Tank Company (USA)  NO       n/a      0       -            -       -         -  3h20m delay

  maxLat for a frozen unit is its de-stack ring offset from the route's first vertex, not a
  deviation. Offset-route owners: T19 = GndV 49 (leading 50/51/52); T15 = GndV 61 (62/63/64);
  T1 = GndV 73 (74/75/76); T39 = M1A2 851/853/857/861 leading 18 members over four internal
  sub-routes C/1-35_R0..R3 - the Tank Company distributes to its 3 platoons + HQ section, a
  two-level structure the Ground_Aggregate path does not have.

  SPLIT BY TEMPLATE (the number this run existed to produce):
    Ground_Aggregate (5 dispatching)   3 moved (T1,T15,T19)   2 froze (T5,T31)
    Tank Company (USA) (3 dispatching) 1 moved (T39)          2 froze (T27,T35)
    Tank entity (1 dispatching)        0 moved                1 froze (T23) + false TASKCMPLT

### P1 CREATION - MET (P1.6 numbers wrong, see finding B)

P1.1 MET. `QUERYINIT   : 128 Units, SystemName=[C2SIM]`.
P1.2 MET. Exactly one `Init dispatched: 128 units + 35 areas queued for creation.`; zero
     ClientId-mismatch lines; zero duplicate-init skips; zero missing-lat/lon skips.
P1.3 MET. Exactly 128 `Create-altitude mode=Live: GROUND unit ... created at safe MSL 10000 m`.
P1.4 MET. Exactly 10 DeStack lines, led by
     `DeStack (R8): 54 units at (34.67998497486787,-116.72479854165415) spread onto 50 m rings
     (first unit kept in place).` plus nine 2-unit groups.
P1.5 MET. Zero `R1: created aggregate` lines (AggregateFormation OFF, as configured).
P1.6 MISSED - and the miss is a real finding, not noise. See finding B.
P1.7 MET. Oracle gate passed; 3724 unconsumed.
P1.8 MET. Reflected count peaked at 1812; 1790 at t=153.5 s (predicted band 1700-1850 within
     ~150 s).

### P2 DISPATCH - P2.1/P2.4/P2.5 MET; P2.2/P2.3/P2.6 MISSED

P2.1 MET EXACTLY. Nine CreateRoute lines with the predicted point counts - T1(5) T5(2) T15(5)
     T19(5) T23(5) T27(2) T31(5) T35(5) T39(5). Zero `MoveToLocation`, zero patrol deferrals.
     The committed 9-dispatch prediction (sec 0b) is confirmed; nothing was re-counted.
P2.2 MISSED. TWO `NO LOCATION GIVEN` lines, not one: T9 (predicted) and T24 (not predicted).
     T24 is a downstream consequence of finding A - T23's false completion released it, it
     dispatched, and it has no Locations. T13: zero mentions, as predicted.
P2.3 MISSED, mechanism understood. 30 gate-skip lines, not 31 (T24 dispatched instead of
     skipping), split 25 timeout / 5 abandoned-upstream against a predicted 10 / 21.
     WHY: `TaskSequencer.WaitForStartAsync` phase 1 starts its window at WAIT-START, i.e. at
     order arrival, for every gated task simultaneously. So a link-3 task's own 600 s timer
     expires microseconds BEFORE its link-2 predecessor's abandon signal arrives, and the
     `PredecessorTimeout` branch wins the race. This is the LIMITATION the class doc already
     states ("phase 1's window runs from wait-start, so a healthy chain deeper than one
     timeout-length per link can still phase-1-time-out"). NOT a new defect; my prediction
     mis-modelled a documented behaviour. The 5 abandoned-upstream lines are exactly the two
     chains whose head resolved early: T10/T11/T12 (T9 abandoned at t~0) and T25/T26 (T24
     abandoned after T23's false completion).
     The important part MET: ZERO `SUPERSEDES in-flight task`, ZERO `-> dispatching.` - no
     head task was ever disturbed, exactly as DEFECT B's fix promises under policy=skip.
     The timeout lines record the frozen units as `BUSY (task in flight)`, confirming their
     move-along was dispatched and simply never completed.
P2.4 MET. Zero `NO in-flight task recorded`; no empty-uuid TASKCMPLT.
P2.5 MET in substance: zero FireAtTarget, zero Breach issued - the 42/42 self-target fact
     holds. Counts are doubled (14 self-target, 2 breach-unresolvable) because
     `ExecuteTaskOnTick` is RE-ENTERED on the terrain-profile reply and re-logs the Layer-1
     and Layer-2 classification. Cosmetic logging defect, recorded as finding C.
P2.6 MISSED. One TASKCMPLT, predicted zero - and it is vacuous. Finding A.

### P3 MECHANISM - P3.1/P3.3 MET; P3.2 MISSED (the headline); P3.4 near-miss

P3.1 MET. In bin64-vrfSim.log: `empty route -- not sending move along to subordinate` = 0;
     `moveAlong() - empty route` = 0; the bare substring `empty route` = 0. Also
     `Waiting for nav data` = 0 (the disabled NavArea confirmed live) and `FATAL` = 0.
     F1 DID NOT FIRE.
P3.2 MISSED. 31 distinct member offset routes were created, but they belong to only FOUR of
     the EIGHT dispatching aggregates (predicted: >0 for each of the eight, >50 total).
     T1, T15, T19 got 4 each (their full Ground_Aggregate member set); T39 got 18 (its full
     Tank Company member set). T5, T27, T31 and T35 got ZERO - with no diagnostic line of any
     kind. This is the run's central result and the reason the verdict says the failure has
     gone SILENT.
P3.3 MET, and it is the first test of this path at 5 vertices and 9 concurrent requests.
     9 requests sent, 9 replies, 9 `all N vertices authored from terrain + 10 m clearance`,
     ZERO partial replies, ZERO `keep the Live altitude` warnings, ZERO `request not sent`.
     Verbatim, e.g.:
       `Terrain profile 164 for task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1': all 5 vertices authored
       from terrain + 10 m clearance; alts [1147.9, 1147.1, 1129.4, 1123.0, 1128.2].`
       `Terrain profile 169 for task 'T27_SecureMovementCorridorsAndPassesAlongPlYellow.':
       all 2 vertices authored from terrain + 10 m clearance; alts [1146.8, 1076.4].`
     Note this holds for the FROZEN units too - T5, T27, T31 and T35 all received fully
     terrain-authored routes. Waypoint altitude is therefore NOT the freeze discriminator,
     independently of the 2026-07-22 falsification.
P3.4 NEAR-MISS, recorded as stated rather than re-banded. Authored altitudes span
     928.8-1452.8 m; the registered band was 950-1500 m, so T39's vertex at 928.8 m sits
     21.2 m below it. The band was my estimate of AO terrain, not a measurement; the
     substantive predictions hold - no vertex at 100 m (the Fixed100 relic) and none at
     10000 m (the create altitude).

### P4 MOVEMENT - P4.2/P4.3/P4.4/P4.5 MET; P4.1 MISSED

P4.1 MISSED. FOUR of nine dispatching performers left the pile and progressed monotonically;
     the prediction was at least six. See the table.
P4.2 MET for every mover. Along-route at window close: T15 26.70 km, T39 24.20 km, T1
     13.39 km, T19 13.17 km - all far above the 5 km floor. Speed over the last 300 s:
     8.19, 8.11, 8.19, 8.18 m/s - above the 3 m/s floor and consistent with the documented
     8.6 m/s column pace and the back end's own `speed=10`. Max lateral deviation from the
     authored corridor over the whole run: 57-123 m, i.e. formation width, not wandering.
P4.3 MET, after a required correction to the instrument. Of 1,732 objects, 27 emit physically
     impossible position fixes - altitudes up to 22,786,533 m, latitudes to -21.2, longitudes
     to +132.8 - interleaved with correct ones. Example, uuid 8bf8cd18: born at the pile
     (34.679532,-116.723804,1136.5) at t=53.2, reads (23.47,-107.53,180636) at t=855.4, is
     back at (34.6267,-116.6767,1101.2) at t=2360.5, then (47.89,+132.80,22786533) at
     t=2761.9. This is the cast-corrupted-reflection class already in CORRECTIONS_LOG ("BOTH
     readable objects are cast-corrupted", altitudes 1.02e15 and 6.4e72), not motion.
     COMPETING HYPOTHESIS TESTED AND REFUTED: that these were MUNITIONS (born at a shooter,
     flying ballistically, and not text-reporting). bin64-vrfSim.log contains no munition
     creation and no detonation - every "Munition"/"Detonation" hit is a FOM/FED schema
     declaration emitted at federation join (08:54:47/08:54:53), and zero FireAtTarget were
     issued. Also consistent with corruption and not flight: no named object EVER text-
     reported a position outside the AO (0 of 1,732).
     VERDICT ON THE CLEAN POPULATION: of the 1,705 objects with no impossible fix, ZERO end
     outside lat[34.15,34.95] x lon[-117.10,-116.25]. No runaway. The July 541 km / 166 km
     excursion class did NOT reproduce.
P4.4 MET. Zero clean objects below 500 m MSL; minimum plausible altitude across the whole
     federation 721.1 m. No underground termination, no offshore termination.
P4.5 MET - the CPP-ALT-1 signature did NOT reproduce. All four movers were STILL MOVING at
     window close (2390-2458 m in the final 300 s) at radii of 13.2, 13.4, 24.2 and 26.7 km
     from spawn. Two crossed 18.4 km and kept going. No performer came to rest at any common
     radius. The 2026-07-16 "all 6 marchers stopped at 18.1-18.4 km" observation is not
     reproduced under the clean state at 1x.
P4.6 NOT SCORED (headless run, as registered).

### P5 HYGIENE - MET

P5.1 MET. No new .dmp, no process with a `^vrfSim.*\.dmp$` title, StopVrf exit 0 and
     `VR-Forces is DOWN (graceful quit; no process was force-killed)`. RTI trio 41336 /
     224608 / 76620 untouched throughout and still resident.
P5.2 MET. Zero `Only one usage of each socket address`; zero `Connection error:`.
P5.3 MET. Runner exit 0; marker 3718 -> 3725, ledgered before any join.
P5.4 MET EXACTLY as predicted. `Cleanup: deleting 172 created VR-Forces objects before
     resign...` / `Cleanup: 172 deletes dispatched (1565 ms).` = 128 units + 35 areas + 9
     routes.
P5.5 MET but WEAK, and the weakness is recorded rather than counted as a pass. `tools/ResetVrf
     3725` joined clean and found 0 reflected objects, 0 deletable, exit 0, resigned cleanly -
     but it reported `[OK] joined (BackendCount=0)`, because the runner's StopVrf had already
     terminated the back end. A post-StopVrf sweep can prove there is NO STALE FEDERATE (it
     does) but CANNOT enumerate scenario leftovers, because the scenario died with the back
     end. To exercise RUNBOOK :1171-1185 as written, ResetVrf must run between StopIface and
     StopVrf. Recorded as finding D.

### FINDINGS (each recorded, none fixed in this rung)

A. VACUOUS COMPLETION ON THE ENTITY PATH - falsifier-class, and the first time this class has
   been caught with the completion's origin identified. The lone entity taskee 1-1/2/1_AD
   (C2SIM uuid de16a337-b2a6-c029-07b5-869191631621) NEVER MOVED: 0.18 km net over 45 position
   reports spanning the whole window, 0.0 m of movement in the final 300 s, ending 0.20 km
   from spawn against a 28.72 km route - i.e. it sat in its de-stack ring. Yet at
   vrfc2simapp.log:447-449:
     `VRF task complete: 1-1/2/1_AD / move-along`
     `SENT TASK STATUS REPORT (TASKCMPLT) taskee=de16a337-... task=468c0325-99b9-4f97-afc6-39fe301e0c55.`
   (468c0325 is T23_AOA_SE_1-1_RECON/2/1_AD_P1.)
   WHICH LAYER PRODUCED IT: the BACK END. `_bridge.TaskCompleted += OnVrfTaskCompleted`
   (VrfC2SimService.cs:159); OnVrfTaskCompleted logs the `VRF task complete` line at :1201
   straight off that callback. Fan-out is OFF, so `_fanOut.TryCompleteMember` returns false
   and control falls to `SynthesizeUnitCompletion` at :1238; there is no straggler timer
   (FanOutStragglerSeconds=0) and no quorum path. Our layer only ATTRIBUTED the completion
   (correctly, via the P0.1 in-flight record) and reported it. VR-Forces asserted that a
   move-along completed for an entity that never moved.
   CONSEQUENCE, not a completion: the false completion released T23's successor T24, which
   dispatched and failed with `NO LOCATION GIVEN` at :451, which in turn cascade-skipped T25
   and T26. Three of this run's task outcomes are downstream of one false report.
   This reproduces the July F2 class ("F2 R11 VACUOUS COMPLETION ... 1-1/2/1_AD (T23) zero
   displacement ... yet 'VRF task complete: 1-1/2/1_AD / move-along' fired") on the CLEAN
   state, for the SAME unit, with all four blocker layers peeled.

B. ECHELON 'F' UNITS LAND THE GENERIC Ground_Aggregate FALLBACK - 26 of 128, including FIVE of
   the nine dispatching performers. This contradicts the read-only survey's "26 F ->
   ArmorCoHQ (Tank Company HQ)" and my own P1.6 prediction, both of which assumed the factory
   name meant a real Co-HQ template landed.
   MAPPING LINE: `UnitTranslator.cs:70` sends SIDC echelon 'F' to `ArmorCoHQ`, which emits
   `Spec(11,1,225,5,20,0,0)` at `:134-135`. Per docs/TYPE_GAP_ADJUDICATION.md Decision item 4
   (:104-110), the intended `aggregate-Company-HQ-Friendly` template has matchType
   `3:11:1:225:5:20:1:0` with Specific=1 NOT wildcarded, so our Specific=0 is a non-match and
   VR-Forces falls back to generic `Ground_Aggregate`. That decision is still awaiting a USER
   call (option A: one-field match fix, 4 generic dismounts; option B: retarget to Tank
   Headquarters Section (USA), the militarily correct composition).
   MEASURED CREATION CENSUS (bin64-vrfSim.log, `Locally Simulated: X (VRF_UUID:..) using
   parameters: ..\T.entity`), which is what P1.6 should have predicted:
     Tank Company (USA)              64   = the 64 echelon-'E' units
     Ground_Aggregate                26   = the 26 echelon-'F' units  <-- the fallback
     Tank Platoon (USA)             215   = 23 echelon-'D' units + 3 platoons inside each of
                                            the 64 companies (23 + 192)
     Tank Headquarters Section (USA) 64   = one HQ section INSIDE each company (NOT the
                                            'F' units)
     ground-vehicle-parameters      104   = 26 Ground_Aggregate x 4 anonymous GndV members
     M1A2_Abrams_MBT               1003, M998 HMMWV 128, M3A2_Bradley_CFV 64,
     M577A2_Command_Post 64, Area 35, Route 12
   The top-level 113 aggregates + 15 entities split predicted in the prereg DOES hold
   (64 + 26 + 23 = 113).
   CRITICALLY, THE FALLBACK IS NOT THE FREEZE CAUSE. Ground_Aggregate DOES publish members
   (4 GndV each) and DOES build offset routes and march - T1, T15 and T19 are all
   Ground_Aggregate and all three marched 13-27 km. Meanwhile two Tank Company (USA) units on
   the correct template froze. The July inference that the generic fallback means "no member
   set for buildOffsetRoute" is therefore NOT true of Ground_Aggregate in 5.0.2 as configured
   here. Decision item 4 remains worth taking on military-correctness grounds; it is not a
   movement fix.

C. DOUBLE-LOGGED TASK CLASSIFICATION (cosmetic). Under GroundWaypointAltitudeMode=TerrainProfile
   `ExecuteTaskOnTick` returns after issuing the terrain query and is RE-ENTERED on the reply
   (VrfC2SimService.cs:783-812), so every log line ABOVE the query point is emitted twice per
   task: the Layer-1 `verb=... Layer-2 not yet wired`, the ATTACK self-target notice (14 lines
   for 7 tasks) and the BREACH unresolvable warning (2 lines for 1 task). Lines below the
   query point (CreateRoute, MarkDispatched) fire once, and the zero-Locations abort fires
   once because it precedes the query. No behavioural effect; it inflates any count taken
   from those lines.

D. THE POST-RUN SWEEP IS BLIND AS SEQUENCED. See P5.5. ResetVrf after StopVrf cannot see
   scenario leftovers. RUNBOOK :1171-1185 assumes a live back end.

### VERIFIED vs INFERRED

VERIFIED (direct artifact reads): every count and verbatim line in P1, P2, P3, P5; the
per-performer displacement, along-route distance, lateral deviation and final-300 s movement
(reports-captured.log, 45 fixes per performer); the offset-route -> task attribution
(`follow-in-formation: leader=<M>; leaderRoute=<task route>`); the creation census; the
absence of munitions; the 27 corrupted objects and the clean-population box test; finding A's
log lines and the source path that produced them.

INFERRED: that the 27 corrupted objects are the same defect class as the CORRECTIONS_LOG
baseline-object corruption - the signature matches (impossible values interleaved with correct
ones) but the root cause has not been traced in this run. That GndV 49/61/73 lead full 4-member
sets is read from `lead-formation` groupings, not from a published roster. That the frozen
units' `BUSY (task in flight)` status means the move-along was accepted and never completed -
consistent with all evidence but not directly observed in the back end's task state.

### UNEXPLAINED - carried forward, not resolved

1. WHY these four and not those four? No property yet separates T5/T27/T31/T35 from
   T1/T15/T19/T39: not template (both classes split), not waypoint altitude (all nine got
   fully terrain-authored routes), not route length (T5 33.5 km froze, T39 40.2 km marched;
   T27 24.1 km froze, T19 28.5 km marched), not point count (both 2-point routes froze while
   both 2-point... no: T5 and T27 are the only 2-point routes and BOTH froze, while all four
   movers have 5-point routes - the only clean correlate found, but three 5-point routes also
   froze, so it is not sufficient). Nothing in the back-end log marks the difference.
2. Why the back end asserts move-along completion for a stationary entity (finding A).
3. What corrupts the position reflection for 27 of 1,732 objects.

### NEXT (not decided here)

The one clean correlate worth testing first is the 2-point route: T5 and T27 are the only
2-point routes in the run and both froze. That is a cheap, single-variable probe. The
remaining freezers (T31, T35) have 5-point routes identical in shape to movers T1/T19/T23,
which makes a per-unit rather than per-route cause more likely for them. Docs first either
way: the aggregate move-along chapter and generateFormationRoutes' "still waiting for data"
return are unread.
