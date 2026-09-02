# PREREG COA-STP1 RUNG 2 - the FULL order under FFRTC with the route-uuid fix: do the four rung-1 freezers now build offset routes and march? - registered 2026-09-02, BEFORE launch

WHAT THIS IS: run 20260902T125423Z (COA-STP1 RUNG 1, the bounded scale run whose central
result was that FOUR of EIGHT dispatching aggregates built ZERO member offset routes and
never moved, SILENTLY) run again with the ROUTE-UUID FIX in the app binary and the FFRTC
fixture loaded. Same order, same init, same de-stack setting, same window length, same
sample interval. Handoff NEXT row 1.

WHY IT MATTERS. The route-uuid fix (726f762, verified live 20260902T153837Z) was proven on
ONE long-named aggregate in a three-unit order. Rung 1 is the population it was inferred
from: 128 units, 42 tasks, 9 dispatching heads. Section 2 below shows - from rung 1's OWN
vendor log, measured this turn - that the route-name cut predicts rung 1's mover/freezer
split 9 times out of 9, INCLUDING the lone entity. If that is the mechanism, rung 2 must
turn all five non-movers into movers with nothing else touched. If it is not, rung 2 says so
with a population large enough to discriminate.

## 0. CORRECTIONS TO THE TASKING BRIEF (recorded, not silently absorbed)

C1 - THE BRIEF SAYS: "rung-1 finding A, the vacuous ENTITY completion, WILL still be present
     because the native completion-status item is not in this fix." THE EVIDENCE SAYS
     OTHERWISE, and this prereg registers the opposite prediction with its reason.
     Rung-1 finding A is the lone entity taskee 1-1/2/1_AD (head T23) reporting TASKCMPLT
     while never leaving its de-stack ring. T23's route name is
     `T23_AOA_SE_1-1_RECON/2/1_AD_P1 ROUTE` = 36 characters, and rung 1's own back-end log
     printed it CUT AT 35: `Move-Along Route: "T23_AOA_SE_1-1_RECON/2/1_AD_P1 ROUT"`
     (runs/20260902T125423Z_run/bin64-vrfSim.log, measured this turn). T23's freeze is
     therefore THE SAME DEFECT as the four aggregates', by exactly one character over the
     blob, and the freeze diagnostic that fired 14,904 times in that run is literally
     `buildEntityRouteFollowingMap() : Can't find entity route` - an ENTITY route lookup.
     PREDICTION REGISTERED HERE (P4): T23 MARCHES, and its completion is therefore NO LONGER
     VACUOUS. What the fix cannot do is repair the NATIVE completion-status gap
     (`DtTaskCompleteReport::success()` dropped at VrfFacade.cpp:217-242): if the entity
     completes for a bad reason we still cannot tell. So the QUEUED NATIVE ITEM STANDS
     UNCHANGED and is neither validated nor invalidated by this run - but the OCCASION that
     produced finding A is predicted to be gone. Scoring both ways is in P4.
C2 - THE BRIEF SAYS `buildEntityRouteFollowingMap() : Can't find entity route` was 14,913 in
     rung 1. MEASURED THIS TURN on the rung-1 artifact: the exact phrase occurs **14,904**
     times, on 14,904 lines; the substring `buildEntityRouteFollowingMap` occurs on 14,943
     lines (the extra 39 are other messages from the same function, plus interleaved
     fragments - the vendor log interleaves writes from several threads). 14,913 is not
     reproducible with an exact-phrase count and is not used. The PREDICTION IS ZERO either
     way, so this correction changes no adjudication; it is recorded so the number in the
     handoff is not carried forward unchecked.
C3 - "ONE VARIABLE". This run moves TWO things relative to rung 1: the APP BINARY (the
     route-uuid fix) and the SCENARIO FIXTURE (stock TropicTortoise -> TropicTortoise_FFRTC).
     That is stated plainly rather than claimed away. FFRTC is admitted as a HELD variable on
     the strength of its own validation (PREREG_R9_FIXED_FRAME_RTC sec 8, c0e90b7: the
     ANSWER was unchanged across four runs of the R9 order - same completions, endpoints
     within 0.09 m - while wall cost fell ~9x), and because the handoff's standing rule is
     that ALL probes run under FFRTC unless the prereg says why not. It is nonetheless a
     second difference and every prediction below is written so an FFRTC-induced surprise is
     visible as such (P6 is the mode check; P2's counts are mechanism counts, not time-
     dependent ones).

## 1. DOCS AND SOURCES CONSULTED (docs first, per the 2026-09-01 standing rule)

VENDOR HEADERS under C:\MAK, opened and read this turn:

- `C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h:246-253` - the DtUUID string constructor
  contract, verbatim: "If string is a UUID (VRF_UUID:) then sets a valid UUID from the
  string, else will have an invalid UUID.  Check isValid after the constructor is called to
  see if it is a valid UUID.  If blockMarkingTextLookup is true, if the string given is an
  object marking text (not UUID) blocks the lookup to map the marking text to the UUID and
  keeps the uuid as the object marking text".
- `C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h:410-412` - the storage, verbatim: "The UUID
  has been changed to be a memory blob of fixed size.  The blob's format is the first char is
  the type, and the rest is the data" followed by `char myData[36];`. 36 bytes, one of them
  the type tag, which is why a marking-text string survives to 35 characters and no further.
  THE BLOB IS STILL AN INFERENCE, NOT A READ - unchanged from the route-uuid prereg sec 6.
- `C:\MAK\vrforces5.0.2\include\vrfmodel\disaggregatedMoveAlongController.h:34-49` - the
  aggregate move-along contract, verbatim: "Movement is implemented by creating temporary
  working routes for each subordinate, positioned at an offset needed to maintain that
  subordinate's position in formation ... The movement task is considered complete when all
  subordinates have reached the end of the route and have issued task complete reports to the
  aggregate.  At that point, the aggregate destroys the temporary working routes and the task
  ends." And `:217-231`, `virtual void beginMoveAlong(const DtUUID& route, ...)` and
  `virtual bool generateFormationRoutes(const DtSimObject& route, bool reverseDirection)`
  whose return is documented "true if the function completed, false if it is still waiting
  for data". THE ROUTE ARRIVES HERE AS A DtUUID - which is the whole point of the fix.

VENDOR USERS GUIDE, `C:\MAK\vrforces5.0.2\doc\VRFUsersGuide.pdf`, read in full for the FFRTC
prereg on 2026-09-02 and quoted from that record (PREREG_R9_FIXED_FRAME_RTC sec 1, c0e90b7)
rather than re-extracted:
- sec 3.4.3 Exercise Clock Modes, p.122-123, Fixed-Frame Run-To-Complete: "advances
  simulation time by a fixed amount each frame, even if a frame takes longer than the fixed
  amount to compute ... This mode is most useful for situations where you want a simulation
  to run with internal consistency and high fidelity, and want it to run to completion, but
  do not need to observe the simulation ... It is suitable for distributed use only in
  time-managed HLA federations."
- sec 7.6.1 Changing the Simulation Speed, p.254-255 - the time-scale multiplier degrades
  models by design and is keyed to fastForwardSettings.mtl; NOT USED, TimeMultiplier stays 1.
- sec 12.2.1 / Table 17 - the scenario's own clock-mode field, which is the one the fixture
  edits.
- sec 41.1 Overview, p.989: "A graphical object's name can be up to 255 characters long." The
  order's longest route name here is 99 characters and is therefore LEGAL; nothing in this
  run shortens a name.

OUR RECORD, re-read this turn:
- docs/experiments/PREREG_COASTP1_RUNG1_BOUNDED_2026-09-02.md - the run being repeated, in
  full: sec 0b (order/init facts), sec 1 (configuration), sec 2 (invocation), sec 6 (outcome,
  the per-performer table, findings A-D, the three unexplained items).
- docs/experiments/PREREG_ROUTE_UUID_FIX_2026-09-02.md sec 6 (982de81) - the fix verified
  live on the R9 order.
- docs/HANDOFF_2026-09-01_R9_COMPLETE.md - all 200 lines.

## 2. THE PRE-LAUNCH ANALYSIS THAT MAKES THIS RUN WORTH RUNNING

Route name = `<TaskName> + " ROUTE"` (`src/VrfC2SimApp/VrfC2SimService.cs:929`). Task names
were parsed out of `data/COA-STP1_Order.xml` this turn; the cut forms were measured this turn
from rung 1's own `bin64-vrfSim.log` (`rg -No 'Move-Along Route: "'`, then uniq -c).

  head  task-name len  route-name len  what RUNG 1's BACK END printed        rung-1 outcome
  ----  -------------  --------------  -----------------------------------  --------------
  T1               28              34  "T1_AOA_SE_1-35_AR;_2/1_AD_P1 ROUTE"  MARCHED 13.39 km
  T15              28              34  "T15_AOA_SE_1-6_IN;_2/1_AD_P1 ROUTE"  MARCHED 26.70 km
  T19              27              33  "T19_AOA_SE_40_EN;_2/1_AD_P1 ROUTE"   MARCHED 13.17 km
  T39              23              29  "T39_AOA_SE_C/1-35_AR_P1 ROUTE"       MARCHED 24.20 km
  T23              30              36  "T23_AOA_SE_1-1_RECON/2/1_AD_P1 ROUT" FROZE (entity)
  T35              30              36  "T35_AOA_SE_B/5-20_IN_(MECH)_P1 ROUT" FROZE
  T31              34              40  "T31_AOA_SE_5-20_IN_(MECH);_2/1_... " FROZE
  T27              50              56  "T27_SecureMovementCorridorsAndPasse" FROZE
                                       (followed by `UUIDx` + non-printing bytes - a blob
                                        overrun printed past the string)
  T5               93              99  "T5_ConductCounter-FireAndNeutraliza" FROZE

NINE OUT OF NINE. Every route name that fits (<= 34 characters observed) marched; every name
that does not (>= 36 characters observed) was printed cut at exactly 35 characters and its
performer never moved. No exception, across two template classes and both echelon paths and
the entity path. The 35/36 boundary itself is NOT exercised by this order (no route name is
exactly 35), so this run does not locate the boundary and does not claim to.

THIS SUBSUMES RUNG 1'S OWN "UNEXPLAINED ITEM 1" ("why these four and not those four? ...
Nothing in the back-end log marks the difference"). It also subsumes its "one clean correlate"
- that T5 and T27 were the only 2-point routes and both froze - as a COINCIDENCE OF THIS
ORDER: both also have the two longest names in the run. Rung 2 separates the two hypotheses
for the first time, because the fix changes the addressing and leaves the point counts alone:
  - ROUTE-NAME/UUID hypothesis (registered): all five non-movers march.
  - 2-POINT-ROUTE hypothesis (the surviving alternative): T31 and T35 march, T5 and T27 still
    do not. That outcome is a MISS of P2/P3 and is reported as such, not re-framed.

## 3. THE VARIABLES

MOVED (2, both declared in sec 0 C3):
1. APP BINARY - the route-uuid fix. `src\VrfC2SimApp\bin\Release\net10.0\win-x64\
   VrfC2SimApp.dll` SHA-256
   3b7b8d2eb71ee5ca8228cb305b9c368baaedeb4dac65adac006b8c6edc60cea0, built 2026-09-02 11:30,
   VERIFIED BY HASH THIS TURN against the value the route-uuid prereg sec 3 recorded. NOTHING
   IS REBUILT FOR THIS RUN. The runner starts the app straight from that path
   (RunC2SimScenario.ps1:382), so building is deploying and there is no copy step to check.
2. SCENARIO FIXTURE - `TropicTortoise_FFRTC` instead of stock `TropicTortoise`. ALREADY
   DEPLOYED, VERIFIED BY HASH THIS TURN, NOT REDEPLOYED:
   `C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise_FFRTC.scnx`, 7112 bytes, mtime
   2026-09-02 10:03, SHA-256
   D27E540F8BCCAA2EBDD33C63CF062CB4842DBE1937B7AA3DAC1C20F9C990B0B9 - the value recorded in
   PREREG_R9_FIXED_FRAME_RTC sec 8 and re-verified in the route-uuid prereg. NOTHING IS
   WRITTEN UNDER C:\MAK BY THIS RUN.

HELD, identical to rung 1 (PREREG_COASTP1_RUNG1_BOUNDED sec 1):
    Vrf__DeStackCreates             true     THE ONE ENV VAR, spacing default 50 m. Held
                                             BECAUSE rung 1 had it: the 54-unit pile costs
                                             ~13 minutes to escape by gridlock alone, and
                                             removing it would add a third variable.
    TimeMultiplier                  1        default, no env (user ruling 2026-09-02)
    SubordinateFanOut               false    default - aggregates tasked AS UNITS, which is
                                             the whole point; fan-out bypasses the offset-
                                             route path P2 measures
    AggregateFormation              ""       default OFF
    FanOutStragglerSeconds          0        default
    PredecessorTimeoutPolicy        skip     default; TaskPredecessorTimeoutSeconds 600
    TypeMappingMode                 RealTemplates (compiled default)
    GroundWaypointAltitudeMode      TerrainProfile (compiled default), clearance 10 m,
                                    timeout 10 s
    CreateAltitudeSafeMslMeters     10000    default
    BundlePositionReports           false    default
    NavArea                         DISABLED (verified this turn: the live
                                    `C:\MAK\SharedData\16\latest\TerrainData\navData\
                                    MAK Earth Space (online)\` holds 0 files)
    VrfBridge.dll                   A7504441 on 10/10 main-checkout copies, VERIFIED THIS
                                    TURN. No native source changes; no rebuild.
    vrfSim.mtl                      notifyLevel 3 / objectConsoleNotifyLevel 3, untouched
    order / init                    data/COA-STP1_Order.xml, data/COA-STP1_Initialization.xml
                                    - byte-identical to rung 1, unchanged since before it
    appsettings Vrf:ClientId        "C2SIM" - REQUIRED: the COA-STP1 init declares
                                    `<SystemName>C2SIM</SystemName>` and the runner ABORTS at
                                    validation with exit 2 on a mismatch
                                    (RunC2SimScenario.ps1:1154-1165, which reads the
                                    appsettings.json NEXT TO THE EXE). The repo default is
                                    "STP" (for the R9 lean init). Rung 1 made this a TRACKED
                                    edit and reverted it afterwards; THIS RUN DOES NOT TOUCH
                                    THE TRACKED FILE. The edit is made ONLY to the DEPLOYED,
                                    GITIGNORED copy
                                    `src\VrfC2SimApp\bin\Release\net10.0\win-x64\
                                    appsettings.json` (.gitignore:9 `bin/`), which is a JSON
                                    config file and CANNOT change the DLL hash recorded
                                    above, and it is REVERTED to "STP" after the run. Both
                                    values are recorded in sec 7. Rationale for the
                                    deviation from rung 1's method: another executor owns
                                    src/ in a separate worktree this session, so no tracked
                                    file under src/ is edited or committed here.

`Get-ChildItem env:Vrf__*` is echoed into the run console log immediately before launch and
must show `Vrf__DeStackCreates=true` and NOTHING ELSE.

## 4. INVOCATION

    $env:Vrf__DeStackCreates = 'true'
    Get-ChildItem env:Vrf__*            # echoed into the console log
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init  data/COA-STP1_Initialization.xml `
        -Order data/COA-STP1_Order.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 2700 -SampleSecs 10 -StopWhenComplete

WINDOW: 2700 s and 10 s sampling are RUNG 1'S OWN VALUES, held for comparability. Under FFRTC
the same wall window buys more sim time, which can only help; it is not tuned for it.

`-StopWhenComplete` IS INERT FOR THIS ORDER AND IS REGISTERED AS SUCH. The gate requires ALL
taskees and ALL tasks to report TASKCMPLT (RunC2SimScenario.ps1:2145, :1605); the order has
42 tasks and 11 distinct performers, of which T9's performer can never be tasked (zero
Locations) and T13's carries a 12,000 s start delay measured on our app's WALL clock. So the
window WILL run its 2700 s cap and the manifest will record `earlyExit.enabled=true`,
`fired=false`. It is passed because the standing configuration passes it (handoff PROBE
PROTOCOL: "-RunSecs is a CAP under -StopWhenComplete"); it costs a 5 s poll cadence instead
of 30 s and changes nothing else. If it DOES fire, that is a stronger result than rung 1, and
the time is recorded - it is not a deviation.

APP NUMBERS. The marker at docs/OPUS_EXECUTION_PLAN.md Appendix B reads
`*** NEXT FREE: 3750 ***` at registration time (verified as the only value-bearing marker in
the file). The runner allocates EXACTLY 7 (vrfBackend, vrfFrontend, oraclePre, oracleTrace,
app, rtiProbe, createOneDiag) and advances the marker itself: expected wasValue 3750 /
newValue 3757, appNos 3750-3756, with 3756 (createOneDiag) consumed ONLY if the stage-7
oracle gate fails. The post-run `tools/ResetVrf` sweep then takes 3757 by hand, ledgered
BEFORE the join, advancing the marker to 3758. Actuals are recorded in sec 7.

AFTER the run: `Remove-Item env:Vrf__DeStackCreates`, then `tools/ResetVrf <fresh appNo>`,
output recorded. STANDING CAVEAT (rung-1 finding D): the sweep runs AFTER StopVrf, so it
proves NO STALE FEDERATE and nothing about scenario contents.

FOREGROUND EQUIVALENT: launched unattended in the background with a ceiling of 75 minutes
(2700 s window + ~9 min of stage overhead + trail). The MAK crash-dump dialog, if it appears,
is answered by `pwsh -File scripts\AnswerCrashDumpDialog.ps1` then `scripts\StopVrf.ps1`
(RUNBOOK 0.5.12, always Yes), and that is an F5-class infrastructure event.

## 5. PRE-LAUNCH INVENTORY (taken at registration; must still hold at launch, else STOP)

Every line below was measured this turn, not inherited:
- NO vrfSim* / vrfGui / vrfLauncher / WatchVrf / ListenReports / VrfC2SimApp process of any
  kind. The ONLY matching processes are the RTI trio.
- RTI trio RESIDENT and ANSWERED, PIDs unchanged from the whole 2026-09-02 record:
  rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620. NEVER killed.
- Docker: stp-server Up 24 hours (healthy), c2sim_server4.8.4.9 Up 24 hours, stp-lt511 Up
  about an hour (healthy).
- `Get-ChildItem env:Vrf__*` count 0.
- Newest bin64 dump is still vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp (2026-09-02 06:00) -
  no new dump since the route-uuid run. No back end parked on a dump prompt.
- Deployed app DLL and bridge hashes as in sec 3; FFRTC fixture hash as in sec 3.
- git: branch main, working tree clean apart from untracked `.claude/`, a workspace file,
  `docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md` (another executor's file, NOT touched or
  committed here) and `tools/analysis/__pycache__/`.
- MAK license expires 2026-09-15 (handoff); today is 2026-09-02, so it is valid.

## 6. PREDICTIONS - registered before launch, with confidence and falsifiers

Adjudicated from run-directory artifacts ONLY: `vrfc2simapp.log`, `bin64-vrfSim.log`,
`watchvrf-trace.csv`, `reports-captured.log`, `run-manifest.json`, the console log.
RUNG-1 CONTROL VALUES quoted below are from `runs/20260902T125423Z_run`, re-measured this
turn where a number is load-bearing.

---

P0 - RUN IDENTITY (HIGH). This is the registered run, on the new binary.
  (a) `vrfc2simapp.log` contains the NEW route-line format,
      `Route '<name>' (VRF_UUID:<guid>) created; MoveAlongRoute issued for VRF_UUID:<unit>` -
      which no pre-fix binary can emit. IF THE OLD FORM (`Route '<name>' created;`) APPEARS,
      THE RUN IS VOID (wrong binary) and is not adjudicated at all.
  (b) Creation census unchanged from rung 1: `QUERYINIT   : 128 Units, SystemName=[C2SIM]`;
      exactly one `Init dispatched: 128 units + 35 areas queued for creation.`; zero
      `0 of 128 units matched Vrf:ClientId`; exactly 128 `Create-altitude mode=Live: GROUND
      unit ... created at safe MSL 10000 m`; exactly 10 `DeStack (R8):` lines led by the
      54-unit group; zero `R1: created aggregate`.
  (c) EXACTLY 9 head tasks dispatch with `CreateRoute '<TaskName> ROUTE' (N pts) ...` at the
      rung-1 point counts: T1(5), T5(2), T15(5), T19(5), T23(5), T27(2), T31(5), T35(5),
      T39(5). Zero `MoveToLocation`, zero patrol deferrals.
  FALSIFIER: the old log form (VOID); or any creation count above differing from rung 1's.
      A different dispatch set means the run is not the run that was registered - STOP.

P1 - THE NAME REACHES THE BACK END INTACT (HIGH). This is the mechanism, measured at the
     narrowest point.
  (a) ZERO cut forms in `bin64-vrfSim.log`. Concretely: the five 35-character truncations
      listed in sec 2 (`T23_AOA_SE_1-1_RECON/2/1_AD_P1 ROUT`, `T35_AOA_SE_B/5-20_IN_(MECH)_P1
      ROUT`, `T31_AOA_SE_5-20_IN_(MECH);_2/1_... `, `T27_SecureMovementCorridorsAndPasse`,
      `T5_ConductCounter-FireAndNeutraliza`) each occur ZERO times as the terminal content of
      a `Move-Along Route: "..."` field.
  (b) ALL NINE head route names appear in a `Move-Along Route:` field either at FULL length
      with a closing quote, or as `VRF_UUID:<guid>`. Either representation passes - the claim
      is that nothing is CUT, not which form the console prints. (In the R9 verification the
      back end resolved the uuid and printed the full name; that is the expected branch.)
  RUNG-1 CONTROL: 4 of 9 intact, 5 of 9 cut at 35 characters. Measured this turn.
  FALSIFIER: any cut form appears at all.

P2 - OFFSET ROUTES FOR ALL EIGHT DISPATCHING AGGREGATES (HIGH). The headline; this is the
     prediction rung 1 MISSED.
  (a) EACH of T1, T5, T15, T19, T27, T31, T35, T39 shows member offset-route construction in
      `bin64-vrfSim.log` - `<member>'s Offset Route (VRF_UUID:...) using parameters:
      ...\Route.entity` and/or `follow-in-formation ... leaderRoute=<that task's route>`.
      RUNG 1: FOUR of eight (T1, T15, T19 with 4 members each; T39 with 18 over four internal
      sub-routes C/1-35_R0..R3); T5, T27, T31, T35 ZERO with no diagnostic of any kind.
  (b) Run totals rise accordingly. RUNG-1 CONTROLS, measured this turn: `Offset Route
      (VRF_UUID` on 20 lines; `'s Offset Route` on 111 lines. PREDICTION: both at least
      DOUBLE, because the four freezers include one Ground_Aggregate (T31, 4 members), two
      Tank Companies (T27, T35, ~18 members each) and one Ground_Aggregate (T5, 4 members).
      Stated as a direction with a floor rather than a point estimate because the vendor log
      interleaves writes from several threads and these line counts are approximate by
      construction - which is why (a), a per-aggregate presence test, is the scoring clause
      and (b) is corroboration.
  ATTRIBUTION LIMIT, stated in advance (unchanged from rung 1): member names are VRF-assigned
      and appear nowhere in the app log, so per-aggregate attribution is by
      `leaderRoute=<task route>` and by member-name family plus creation timestamp, never by
      uuid.
  FALSIFIER: ANY of the eight dispatching aggregates builds ZERO member offset routes.
      If the ZERO set is exactly {T5, T27} - the two 2-point routes - that is the surviving
      2-point-route hypothesis from sec 2 and is reported as that finding, still a MISS.

P3 - THE FREEZE DIAGNOSTIC IS GONE (HIGH). ZERO occurrences of
     `buildEntityRouteFollowingMap() : Can't find entity route` in `bin64-vrfSim.log`.
     CONTROLS, measured this turn: RUNG 1 **14,904**; the frozen R9 run (20260902T143638Z)
     67,590; the fixed R9 run (20260902T153837Z) 0; the short-name R9 control
     (20260902T140808Z) 0.
     FALSIFIER: any non-zero count. A small non-zero count is NOT a pass with a caveat.

P4 - MOVEMENT: FIVE NEW MOVERS, INCLUDING THE ENTITY (HIGH on the four aggregates, HIGH on
     the entity, see sec 0 C1).
  (a) ALL NINE dispatching performers leave the pile: net displacement > 1 km, with monotone
      along-route progress. RUNG 1: four of nine (T1 13.39, T15 26.70, T19 13.17, T39
      24.20 km along-route at window close; T5 0.09, T23 0.18, T27 0.00, T31 0.00, T35
      0.00 km).
  (b) Every mover holds CORRIDOR DISCIPLINE: maximum lateral deviation from its own authored
      polyline <= 500 m. RUNG-1 MOVERS: 57-150 m, i.e. formation width. 500 m is a
      deliberately loose ceiling that still excludes wandering.
  (c) Every mover averages >= 3 m/s once clear of the pile. RUNG-1 MOVERS: 8.11-8.19 m/s over
      the final 300 s.
  (d) T23 (the lone ENTITY, rung-1 finding A) MOVES: net displacement > 1 km. If T23 reports
      TASKCMPLT, that completion is scored GENUINE only if watchvrf/reports show it within
      100 m of its route end at the time of the report (the R11 trap); otherwise it is
      recorded as another vacuous completion and finding A REPRODUCES.
  FALSIFIER: any of the nine still frozen (< 1 km net); or any mover beyond 500 m lateral;
      or T23 reporting TASKCMPLT while stationary (= finding A reproduces, which is a MISS
      of the prediction registered in sec 0 C1 and a confirmation of the brief's).
  NOTE ON COMPLETION COUNTS: the number of TASKCMPLT is NOT predicted. Under FFRTC the sim
      clock outruns our app's WALL-clock task timers by an unknown, load-dependent factor
      (handoff FFRTC block: our app has no notion of sim time), so which downstream chain
      links dispatch is not predictable from rung 1. It is RECORDED, not scored. Rung 1's
      gate-skip census (30 lines, 25 timeout / 5 abandoned-upstream) is likewise recorded
      for comparison and explicitly NOT registered as a prediction.

P5 - NO RUNAWAY, NO UNDERGROUND/OFFSHORE TERMINATION (HIGH). Every clean-history reflected
     object's final position lies inside lat [34.15, 34.95] x lon [-117.10, -116.25] (the
     init unit extent plus ~7 km), no sampled altitude below 500 m MSL and none within 50 m
     of 0 m. RUNG 1 MET this on 1,705 of 1,732 objects; the other 27 emit physically
     impossible fixes interleaved with correct ones (the cast-corrupted-reflection class,
     CORRECTIONS_LOG) and are EXCLUDED by the same rule rung 1 used, declared here in
     advance rather than after seeing the data. The COUNT of corrupted objects is recorded
     for comparison and is not itself a prediction.
     FALSIFIER: any clean-history object outside the box, or terminating underground/offshore.

P6 - MODE CHECK (HIGH), a held-variable check and not a finding.
     `python tools/analysis/frame_gaps.py . <run>`: TEST A >= 95% of sub-0.06 s gaps in
     {0.033, 0.034} AND TEST B resultant length R >= 0.99 (the thresholds registered in
     PREREG_R9_FIXED_FRAME_RTC sec 7A A2; F1 fires only if BOTH fail).
     The observed sim-s-per-wall-s SLOPE IS NOT PREDICTED. FFRTC compression is documented
     load-dependent (7.4-13.1 across the R9 family, 3 movers max); this run has ~1,785
     objects and up to 9 marching aggregates with member offset routes, an order of magnitude
     more work per frame, so the slope may fall to or below 1.0. That is not a fault and not
     a falsifier - it is recorded.
     FALSIFIER: both TEST A and TEST B fail, i.e. the fixture/mode path moved and the run is
     uninterpretable as a controlled repeat - STOP per PREREG_R9_FIXED_FRAME_RTC sec 7 F1.

P7 - HYGIENE (HIGH). Runner EXIT=0 and every stage exit code 0; `manifest.flags` empty; the
     back end SURVIVES (no new .dmp in bin64, no process with a `^vrfSim.*\.dmp$` title);
     StopVrf EXIT=0 reporting VR-Forces down with no force-kill; both observers exit on the
     stop-file path and are never killed; RTI trio PIDs UNCHANGED (41336 / 224608 / 76620)
     and never touched; ZERO `Only one usage of each socket address` and ZERO
     `Connection error:` in the app log; `env:Vrf__*` = `Vrf__DeStackCreates` only before and
     EMPTY after; the FFRTC fixture still hashing to D27E540F8BCC...B0B9 (nothing written
     under C:\MAK); the marker advancing 3750 -> 3757 by the runner and -> 3758 by the sweep.
     CLEANUP COUNT: `Cleanup: deleting N created VR-Forces objects` should read 172 = 128
     units + 35 areas + 9 routes (rung 1: 172 exactly). A different N is re-derived from the
     actual CreateRoute count, not excused by widening a band.
     FALSIFIER: a crash/dump prompt (F4-class), or a second infrastructure failure this
     session (F5-class) - either STOPS the work.

## 6A. THE MISS RULE

A MISSED HIGH-CONFIDENCE PREDICTION IS A STOP. P0-P7 are all registered HIGH. If any misses,
the work stops, the miss is adjudicated in sec 7, and NOTHING IS RETUNED - no second window,
no shortened name, no re-run, no adjustment of a prediction to fit what came back. A P0(a)
old-log-form observation VOIDS the run rather than scoring it. A P6 double failure means the
mode path moved and the run is uninterpretable. A P7 miss is infrastructure: record it, and
after TWO infrastructure failures this session, stop entirely.

## 6B. WHAT A CLEAN PASS WOULD AND WOULD NOT ESTABLISH

WOULD: that the route-uuid fix scales from one aggregate in a three-unit order to nine
performers in a 128-unit, 42-task order; that rung 1's "unexplained item 1" (why those four
froze) is answered and CLOSED by the name-cut mechanism, with the 2-point-route correlate
refuted as a coincidence; that the entity path was failing for the same reason as the
aggregate path; and that the offset-route construction the vendor documents at
disaggregatedMoveAlongController.h:34-49 works for every dispatching aggregate in this order.

WOULD NOT: prove the order EXECUTES. This is a mechanism-and-corridor run, exactly as rung 1
was. Task-chain semantics under FFRTC (which links dispatch, which time out) are RECORDED,
not scored, because our app's timers are wall and the sim clock is not (handoff FFRTC block).
It would not validate the NATIVE completion-status item (VrfFacade.cpp:217-242) - a
success=false failure remains indistinguishable from a success no matter how this run comes
out; the queued native item stands either way. It would not settle the echelon-'F' generic
Ground_Aggregate fallback (rung-1 finding B, TYPE_GAP item 4), which is a USER ruling and
which rung 1 already showed is NOT a movement cause. It would not locate the 35/36-character
blob boundary (no route name in this order is exactly 35), and it does not read
`DtUUID::myData[36]` - the blob remains the INFERRED cutter.

## 7. OUTCOME (written from the run-directory artifacts, AFTER the run)

(to be completed)

## 8. REGISTRATION

Sections 0-6B above were registered in the commit named on this line BEFORE the launch
command was issued. Section 7 is the only content added afterwards.
REGISTERED IN COMMIT (hash recorded here after the registration commit):
