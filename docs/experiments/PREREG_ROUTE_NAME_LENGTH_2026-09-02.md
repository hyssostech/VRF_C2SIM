# PREREG ROUTE-NAME LENGTH - MANIPULATE the discriminator the rung-1 forensic only OBSERVED - registered 2026-09-02, BEFORE launch

WHAT THIS IS: run 20260902T140808Z (the R9 fixed-frame run-to-complete probe,
docs/experiments/PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8, commit c0e90b7) run again
with EXACTLY ONE VARIABLE MOVED: the C2SIM `<Name>` of ONE of the order's three tasks - the
one performed by the AGGREGATE 114.MechCoy - is lengthened from 8 characters to 38, so that
the VR-Forces route name our app derives from it (`TaskName + " ROUTE"`,
src/VrfC2SimApp/VrfC2SimService.cs:929) goes from 14 characters to 44. Nothing else moves:
same init, same scenario fixture, same app binary, same bridge, TimeMultiplier 1, no env
override, same runner switches, the other two tasks byte-identical.

WHY IT MATTERS. docs/experiments/ANALYSIS_COASTP1_RUNG1_FREEZE_2026-09-02.md (commit
04bcc0f) found, on run 20260902T125423Z, an exception-free discriminator across 9 performers:
every route name <= 34 characters marched, every route name >= 36 characters froze, and the
back end's own console showed the freezers' route names CUT AT 35 CHARACTERS. That analysis
was OBSERVATIONAL - it read a table off a run that was not designed to test it, and the
short-named and long-named performers also differed in unit, template, route geometry,
position and creation order. This probe MANIPULATES the variable: one performer, one
scenario, one order, two runs, and the ONLY difference between them is 30 bytes of task name.

## 1. DOCS AND SOURCES CONSULTED (docs first, per the 2026-09-01 directive)

VENDOR HEADERS, read-only under C:\MAK, re-verified line by line for this prereg (the
forensic's citations were re-opened, not copied):

- `C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h:410-412` - class DtUUID, protected member.
  Line 410 is the comment "The UUID has been changed to be a memory blob of fixed size.  The
  blob's format" (continued on 411: "is the first char is the type, and the rest is the
  data."); LINE 412 IS `   char myData[36];`. A string that is not a VRF_UUID is carried
  inside that fixed 36-byte blob as object marking text, so the payload is 35 bytes: 34
  characters plus a terminator.
- `C:\MAK\vrforces5.0.2\include\vrftasks\moveAlongTasks.h` - :26 "route name refers to a
  DtSimObject. A route is a control object that"; :41 "parameters: route name - The string
  name of the route to be followed."; :79 "//! Was: routeName"; :82 "//! Was: setRouteName";
  **:83 `   virtual void setRoute(const DtUUID&);`**. This is the call that puts a route NAME
  into a DtUUID.
- `include\vrfmodel\disaggregatedLeadFollowInFormationController.h` and
  `disaggregatedMoveAlongController.h` - both hold the task's route as
  `DtSimObjectReference myRoute`, which must RESOLVE before any offset route is generated
  (`createLeaderOffsetRoute`, `generateFormationRoutes`, `buildOffsetRoute`). An unresolved
  reference yields zero offset routes and no diagnostic.
- `VRFUsersGuide.pdf` 13.2.2 "Simulation Object Names" p.363: entities 11 characters, units
  31 characters; p.988: "A graphical object's name can be up to 255 characters long." A route
  is a control/graphical object, so a 44-character route name is LEGAL to create - and this
  is directly corroborated in-repo: run 20260902T125423Z created a 99-character route object
  intact (bin64-vrfSim.log:47421-47439). NO documented limit exists on the route name carried
  inside the move-along TASK. That doc gap is the whole subject of this probe.

OUR OWN SOURCE (read, not recalled):

- `src/VrfC2SimApp/VrfC2SimService.cs:929` - `string routeName = task.TaskName + " ROUTE";`
- `src/VrfC2SimApp/VrfC2SimService.cs:1142` and `:1172` - the route is addressed BY NAME: on
  the route's ObjectCreated callback the pending task is looked up by `e.Name` and dispatched
  as `_bridge.MoveAlongRoute(pending.TaskeeVrfUuid, e.Name)`.
- `src/VrfFacade/VrfFacade.cpp:529-534` - `CreateRoute` passes `DtString(name.c_str())`,
  UNBOUNDED. `:569-571` - `MoveAlongRoute` passes `DtUUID(routeUuid)`, the 36-byte blob.
  THE ASYMMETRY IS IN OUR OWN TWO CALL PATHS and it is one function apart.
- `src/VrfC2SimApp/OrderParser.cs:57` - `TaskName = (m.Name ?? "").Trim()`.
- C2SIM schema `C2SIMSDK/schemas/C2SIM_SMX_LOX_CWIX2024.xsd:440-447` - `NameType` is
  `<xs:restriction base="xs:string"/>` with NO maxLength facet. THE ORDER SCHEMA DOES NOT
  LIMIT NAME LENGTH; 38 characters is legal, and run 20260902T125423Z already pushed a
  93-character task name through the same server and parser.

IS TaskName USED AS AN ID THAT MUST MATCH SOMETHING ELSE? NO - checked, not assumed.
`grep -rn TaskName src/ --include=*.cs` returns 52 hits. Every one is either (a) a log
message, (b) the route name at :929, (c) the R11 waypoint name at :867 (`TaskName + " WPT"`,
reached only on the PlanAndMoveTo path, which this order does not take), or (d) a field
carried for logging inside InFlightTracker.InFlight / PendingEngage / PendingTerrain.
Correlation and sequencing key on TASK UUID (`task.TaskUuid`,
`task.StartAfterTaskUuid`), never on the name. NOTHING ELSE IN THE ORDER REFERENCES THE
TASK BY NAME, so no second element needs adjusting. The only in-app consequence of a name
collision is the duplicate-route-name FIFO warning at :935, and there is no collision here.

## 2. THE ONE VARIABLE - the order copy, and its exact byte diff

NEW FILE (the original is NOT touched):
`data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml`

    control  data/R9_Mojave_UnitMove_Order_NoComments.xml
             3889 bytes, SHA-256 b453dd4611c5b62f6bd1806583db783589aab94302b1e0c40632e6167dc6e58e
    probe    data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml
             3919 bytes, SHA-256 5eeec682c3c7f012968471f82c0cda9d07d8c56f24b6a1cce3670c4405ad5953

THE COMPLETE DIFF - ONE LINE, +30 BYTES, and nothing else in the file:

    @@ -52,3 +52,3 @@
               </Location>
    -          <Name>T_R5_CO1</Name>
    +          <Name>T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH</Name>
               <UUID>a5000000-0000-0000-0000-000000000002</UUID>

Verified mechanically at write time: exactly one occurrence of the old line; output length ==
input length + 30; CRLF count unchanged at 97 (CRLF preserved throughout); ZERO non-ASCII
bytes. UUIDs, Locations, PerformingEntity, TaskActionCode, ROE, OrderID, and both other
Task blocks are byte-identical.

WHICH TASK AND WHY. Task[1], UUID `a5000000-...-0002`, PerformingEntity
`139aa71b-75df-4888-4a5a-6056bae66242` = 114.MechCoy, THE AGGREGATE. Chosen because the
offset-route mechanism is the visible signature: only an aggregate builds member/leader
offset routes, so its absence is a positive, countable observable rather than "it did not
move". The platoon 1222.MechPlt (task[0], `T_R5_PL1`) and the entity 1.BdeHQ (task[2],
`T_R5_TK1`) are UNCHANGED and serve as the in-run control arm.

THE ARITHMETIC BEING TESTED:

    TaskName                      T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH   38 chars
    routeName = TaskName+" ROUTE" T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE   44 chars
    predicted DtUUID cut at 35    T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3      35 chars
    control routeName             T_R5_CO1 ROUTE                           14 chars

44 >= 40 as the design requires, and it is 9 characters past the 35-byte payload so the cut
is unambiguous: the surviving string ends mid-token at `TO_3` and carries no ` ROUTE` suffix.

OFFLINE PARSE PROOF, RUN BEFORE REGISTRATION (the false-greens rule - prove the input is
what you think before spending a run on it):
`VrfC2SimApp.exe --parse-order` on BOTH files. The two outputs are identical line for line
except the single task[1] name: 3 tasks, same three taskees in the same order, same 2 points
each, same MOVE/ROETight, `mapGraphic: (none -> inline points)` for all three. The padded
name round-trips through the schema deserializer intact and untrimmed.

## 3. EVERYTHING ELSE HELD

Scenario fixture `TropicTortoise_FFRTC` - ALREADY DEPLOYED, VERIFIED BY HASH, NOT REDEPLOYED:
`C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise_FFRTC.scnx`, 7112 bytes, mtime
2026-09-02 10:03:30, .scnx SHA-256
D27E540F8BCCAA2EBDD33C63CF062CB4842DBE1937B7AA3DAC1C20F9C990B0B9, inner
`TropicTortoise_FFRTC.scn` SHA-256
3d8960732bf78cbde02e581c9f04b93e5b926ae3db9cd5c9d679859fb99107ad, frame lines
`(frame-mode "fixed-frame-run-to-complete")` / `(frame-time 0.033333)` - every value matching
PREREG_R9_FIXED_FRAME_RTC sec 8 exactly. NOTHING IS WRITTEN UNDER C:\MAK BY THIS PROBE.

Also held: init `data/R9_Mojave_Lean_Initialization_NoComments.xml`; the app binary as built
for the control (TypeMappingMode RealTemplates, GroundWaypointAltitudeMode the compiled
default TerrainProfile, TerrainClearanceMeters 10, TerrainProfileTimeoutSeconds 10,
SubordinateFanOut off, NavArea disabled, stock templates); the deployed VrfBridge; vrfSim.mtl
notify level 3; TimeMultiplier 1 (frame mode, not multiplier, is the sanctioned speed
mechanism - memory `vrf-frame-mode-not-multiplier`); `Get-ChildItem env:Vrf__*` count 0 at
launch, echoed into the console log; `-RunSecs 1800 -SampleSecs 2 -StopWhenComplete` exactly
as the control used them.

NO SOURCE, appsettings, or bridge DLL is changed by this probe. IT IS A DATA CHANGE ONLY.

RUN-LENGTH CONSEQUENCE, STATED IN ADVANCE: `-StopWhenComplete` closes the window only when
ALL THREE taskees have reported TASKCMPLT (RunC2SimScenario.ps1:2145). If P2/P3 hold,
114.MechCoy never reports and the window runs to its full 1800 s cap - so this run is
expected to take ~35 minutes wall end to end, against the control's 4 min 40 s. That is the
point of keeping -RunSecs at the control's value: the frozen unit is observed for a long
window rather than a truncated one. AN EARLY EXIT WOULD ITSELF BE A SIGNAL (see P3).

## 4. INVOCATION (main checkout, VRF_C2SIM, pwsh) - no env line at all

    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 1800 -SampleSecs 2 -StopWhenComplete

Adjudication from the run directory artifacts ONLY (bin64-vrfSim.log, vrfc2simapp.log,
watchvrf-trace.csv, reports-captured.log, run-manifest.json, the console log).

APP NUMBERS: consumed and advanced BY THE RUNNER. The marker at
docs/OPUS_EXECUTION_PLAN.md Appendix B:1647 reads `*** NEXT FREE: 3734 ***` at registration
time, so the expected block is 3734-3740 (7 numbers) with the marker advancing to 3741, and
the post-run ResetVrf sweep then consuming 3741 and advancing to 3742. The actual
wasValue/newValue from run-manifest.json is recorded in sec 6.

PRE-LAUNCH INVENTORY (must hold, else STOP - never kill): no vrfSim* / vrfGui / vrfLauncher
/ WatchVrf / ListenReports / VrfC2SimApp process of any kind; the RTI trio present with its
PIDs recorded (rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 as of session start,
re-verified at launch, NEVER killed); docker stp-server + c2sim_server healthy; env Vrf__*
count 0; the FFRTC fixture hashing to the sec-3 values.

## 5. PREDICTIONS - registered before launch, with confidence and falsifiers

CONTROL VALUES, quoted from run 20260902T140808Z's own artifacts so nothing is chosen after
the fact:

    bin64-vrfSim.log:6322  1222.MechPlt ...Task 0 ... Move-Along Route: "T_R5_PL1 ROUTE"
    bin64-vrfSim.log:6333  114.MechCoy  ...Task 0 ... Move-Along Route: "T_R5_CO1 ROUTE"
    bin64-vrfSim.log:6337  1.BdeHQ      ...Task 0 ... Move-Along Route: "T_R5_TK1 ROUTE"
    bin64-vrfSim.log:6341  1.BdeHQ      ...Subtask 1 ... Move-Along Route: "T_R5_TK1 ROUTE"
    bin64-vrfSim.log:11253/11256/11257/11260 - the FOUR leader offset sub-routes the company
      built and then tasked its sub-units along: "114.MechCoy_R2", "114.MechCoy_R1",
      "114.MechCoy_R3", "114.MechCoy_R0"
    census over the whole 21832-line control log (grep -o, occurrences not lines):
      "114.MechCoy_R"          29   (4 distinct names: R0 x7, R1 x8, R2 x7, R3 x7)
      "Offset Route (VRF_UUID"  8   (M1A2 1, 5, 7, 8, 12, 15, 17, HMMWV 1)
      "'s Offset Route"        67
      "Move-Along Route:"       8
    watchvrf-trace.csv POS, first (t=23.4 s) -> last (t=123.1 s):
      114.MechCoy   34.647629,-116.693388,1116.7 -> 34.653915,-116.693388,1116.8  (698 m)
      1222.MechPlt  34.612956,-116.600487,1040.6 -> 34.612956,-116.587783,1026.6
      1.BdeHQ       34.608416,-116.712685,1131.4 -> 34.608416,-116.699993,1121.1
    watchvrf-trace.csv TSK: 1.BdeHQ t=40.8, 1222.MechPlt t=42.2, 114.MechCoy t=48.2
    3/3 TASKCMPLT; order push -> last TASKCMPLT 20.18 s wall; earlyExit fired at t+66 s.

THE RUNG-1 SIGNATURE THIS PROBE MUST REPRODUCE ON DEMAND, for line-by-line comparison:
runs/20260902T125423Z_run/bin64-vrfSim.log:52949-52987 - movers intact at :52950 (34 ch),
:52963 (34), :52968 (33), :52985 (29); freezers cut at 35 at :52961
(`"T5_ConductCounter-FireAndNeutraliza"`), :52973 / :52977
(`"T23_AOA_SE_1-1_RECON/2/1_AD_P1 ROUT"`), :52981, :52983; :52979 the UNTERMINATED
`"T27_SecureMovementCorridorsAndPasseUUIDx` + junk; :52994 the single
`(null) destinationcould not be setup` warning.

---

P1 - THE MECHANISM (HIGH CONFIDENCE). The route name reaches the back end TRUNCATED, and the
     aggregate builds no offset routes.
  (a) bin64-vrfSim.log's `114.MechCoy: ... ...Task 0 name and parameters: Move-Along Route:`
      line shows a string of EXACTLY 35 characters,
      `T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3`, optionally followed by unterminated junk in the
      T27 manner - NOT the full 44-character
      `T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE`.
  (b) ZERO `114.MechCoy_R<k>` sub-route names appear anywhere in bin64-vrfSim.log (control:
      29 occurrences, 4 distinct, R0-R3), and the company contributes ZERO
      `Offset Route (VRF_UUID` creations. The platoon's own offset route(s) still appear -
      1222.MechPlt is unchanged - so the census is scored PER UNIT, by the sub-route name for
      the company and by ownership/timestamp for the member offset routes.
  FALSIFIER: the full 44-character name appears in the move-along task line, OR any
      `114.MechCoy_R<k>` sub-route is created. Either kills the mechanism.
  (c) DISCRIMINATING SUB-CLAUSE - THIS PROBE CAN FAIL IN A SECOND, DIFFERENT WAY AND IT MUST
      NOT BE MISREAD AS CONFIRMATION. Our own dispatch resolves the pending task by the
      route object's callback name (VrfC2SimService.cs:1142 `_pendingRouteTasks[e.Name]`). If
      the ObjectCreated name came back truncated, the lookup would MISS and MoveAlongRoute
      would never be issued at all - our defect, not the vendor's DtUUID. PREDICTED
      THEREFORE: vrfc2simapp.log DOES contain
      `Route 'T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE' created; MoveAlongRoute issued
      for VRF_UUID:<company>`, exactly as run 20260902T125423Z's app log did for its
      99-character route (:431). If that line is ABSENT, P1 is NOT confirmed: the failure
      moved upstream into our callback path and the run says nothing about the DtUUID. Record
      that outcome as its own finding.

P2 - THE BEHAVIOUR (HIGH CONFIDENCE). One unit freezes; the other two are unaffected.
  (a) 114.MechCoy does not move: net displacement from its FIRST trace POS to its LAST
      <= 25 m (control: 698 m), and it does not come within 25 m of the control endpoint
      34.653915,-116.693388.
  (b) 1.BdeHQ and 1222.MechPlt complete normally, with trace-final endpoints within 1 m of
      the control's (34.608416,-116.699993 alt 1121.1 and 34.612956,-116.587783 alt 1026.6),
      each with a TSK record and a post-completion RPT agreeing with its POS.
  FALSIFIER: 114.MechCoy marches to its control endpoint (or moves more than 25 m toward
      it); or either unchanged taskee fails or lands more than 1 m off.

P3 - WHAT THE BACK END REPORTS FOR THE PADDED TASK (MEDIUM). One of two outcomes, both
     consistent with the mechanism; RECORD WHICH:
       (i) NO TASKCMPLT for 114.MechCoy at all - no TSK record, no third completion in
           vrfc2simapp.log or reports-captured.log. This is the rung-1 AGGREGATE pattern
           (all four frozen aggregates were silent), and is the more likely of the two.
      (ii) A FALSE TASKCMPLT preceded by a `destination could not be setup`-style warning -
           the rung-1 ENTITY pattern (T23, :52994).
     A TASKCMPLT WITH MOVEMENT is neither - that is the P2 falsifier.
     OPERATIONAL COROLLARY of (i): `-StopWhenComplete` never fires, `earlyExit.fired` is
     false, and the window runs to the 1800 s cap. If the window instead closes EARLY with
     3/3, either P2 or P3 has missed and the run STOPS for adjudication.

P4 - MODE CHECK, CHEAP (HIGH). `python tools/analysis/frame_gaps.py . <run>`: TEST A >= 95%
     of sub-0.06 s gaps in {0.033, 0.034} and TEST B resultant length R >= 0.99 - the exact
     thresholds registered in PREREG_R9_FIXED_FRAME_RTC sec 7A A2, which the control met at
     100.0% and 0.9986. This confirms the fixed-frame mode is still in effect and that
     nothing about the fixture path changed; it is a held-variable check, not a finding.
     step_profile.py's date regex was already generalised (A3) and the instrument was proved
     against Row 3's own log; it is reused unmodified.

P5 - HYGIENE (HIGH). Runner EXIT=0 and every stage exit code 0; StopVrf reports VR-Forces
     DOWN with no force-kill; both observers exit on the stop-file path; RTI trio PIDs
     UNCHANGED (41336 / 224608 / 76620) and never touched; no new .dmp in
     C:\MAK\vrforces5.0.2\bin64 (newest stays vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp,
     2026-09-02 06:00); `Get-ChildItem env:Vrf__*` count 0 BEFORE and AFTER; the post-run
     `tools/ResetVrf <fresh appNo>` sweep reports ZERO leftovers; the appNo marker advances
     by exactly 7 for the run plus 1 for the sweep.
     KNOWN CAVEAT, carried forward from rung-1 finding D and repeated by the control: the
     sweep runs AFTER StopVrf, so it proves NO STALE FEDERATE and nothing about scenario
     contents.

## 5A. THE MISS RULE

A MISSED HIGH-CONFIDENCE PREDICTION IS A STOP. If P1 or P2 misses, the work stops, the miss
is adjudicated in sec 6, and NOTHING IS RETUNED - no second name length, no second run, no
adjustment of the prediction to fit. P3 is medium and both of its branches are pre-named, so
it cannot be missed by picking a branch; only a TASKCMPLT-with-movement (which is already the
P2 falsifier) counts against it. A P4 miss means the fixture/mode path moved and the run is
uninterpretable as a one-variable probe - STOP and diagnose per PREREG_R9_FIXED_FRAME_RTC
sec 7 F1. A P5 miss is infrastructure: record it, and after TWO infrastructure failures this
session, stop entirely. The crash-dump prompt is answered with
`pwsh -File scripts\AnswerCrashDumpDialog.ps1` then `pwsh -File scripts\StopVrf.ps1`
(RUNBOOK 0.5.12, ALWAYS Yes).

## 5B. WHAT A CLEAN PASS WOULD AND WOULD NOT ESTABLISH

WOULD: that route-name length ALONE, holding unit, template, geometry, region, position,
scenario and code fixed, is sufficient to turn a marching aggregate into a silent frozen one
- promoting the rung-1 correlation to a manipulated cause, and making the 34-character cap
(or short synthetic route ids) a justified fix rather than a plausible one.

WOULD NOT: prove that `DtUUID::myData[36]` is the specific buffer doing the cutting. That
stays INFERRED - the header comment plus a 35-character observed cut make it the only fitting
candidate, but no instrumentation in this run reads the blob. Nor does it explain the
forensic's UNEXPLAINED symptom, the 14,880 `buildEntityRouteFollowingMap() : Can't find
entity route` lines; whether any appear here is recorded as data either way.

## 6. OUTCOME (written from the run directory artifacts, AFTER the run)

TO BE COMPLETED AFTER THE RUN.

## 7. REGISTRATION

Sections 0-5B above were registered in commit <TO BE STAMPED IN THE FOLLOWING COMMIT>,
together with data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml, BEFORE the launch command
was issued. Sec 6 is the only content added afterwards.
