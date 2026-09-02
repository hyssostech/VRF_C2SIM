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

VERDICT: **ROUTE-NAME LENGTH IS A MANIPULATED CAUSE OF THE AGGREGATE FREEZE.** Thirty bytes
of task name, and nothing else, turned an aggregate that marched 698 m and reported TASKCMPLT
into one that did not move a single centimetre in thirty minutes and never reported anything.
P1 PASS on all three sub-clauses. P2 PASS on both. P3 resolved to branch (i), the pre-named
aggregate pattern. P4 PASS on both decision criteria, with a thin-sample caveat recorded
below. P5 MISS - the runner exited 5 on a reporting-path defect that this probe was the first
run ever to trigger; every other hygiene item is clean and no evidence was lost. NO PREDICTION
FALSIFIER FIRED.

RUN: `runs/20260902T143638Z_run`, launched 2026-09-02T14:36:38.238Z, order pushed
14:39:01.906Z, observation window closed 15:09:33.155Z at its 1800 s cap (1800.9 s used),
runner finished 15:10:12.7Z - 33 min 34 s wall against the control's 4 min 40 s, exactly the
consequence sec 3 registered in advance. appNumbers 3734-3740 (marker 3734 -> 3741 by the
runner, -> 3742 by the post-run ResetVrf sweep on 3741). `env:Vrf__*` count 0 BEFORE and
AFTER. bin64-vrfSim.log 250,405 lines (control 21,832); vrfc2simapp.log 99 lines (control 103).

REGISTRATION COMMIT (sections 0-5B, before launch): d5aee8b, hash stamped in adb89d1.

### P1 - THE MECHANISM. PASS, and demonstrated on ONE object inside ONE run.

The asymmetry between our two call paths is visible on the SAME route object, three lines
apart in the vendor's own log:

    bin64-vrfSim.log:6295  Locally Simulated: T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE
                           (VRF_UUID:b5965db3-1716-3746-8e0b-cd4959e74cc2) using parameters:
                           ..\data\simulationModelSets\base\vrfSim\Route.entity
    bin64-vrfSim.log:6296  DtLocalObjectManager::processCreateVrfObject() : created object
                           named T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE
    bin64-vrfSim.log:6335  114.MechCoy: ...Task 0 name and parameters:
                           Move-Along Route: "T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3"
    bin64-vrfSim.log:250337 DtLocalObjectManager::remove() : removing sim object
                           VRF_UUID:b5965db3-... T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE

(a) PREDICTED a 35-character cut reading exactly `T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3`.
    OBSERVED, character for character, at :6335 - the ONLY occurrence of the cut form in
    250,405 lines. The full 44-character name occurs 3 times and ALL THREE are the route
    OBJECT (creation :6295/:6296, cleanup removal :250337) - never the task. The object lived
    at its full name from creation to deletion; only the task's copy was cut. CreateRoute's
    `DtString` path (VrfFacade.cpp:529-534) is unbounded; MoveAlongRoute's `DtUUID` path
    (:569-571) is not.
    The cut here is CLEAN - 35 characters and a closing quote, no junk trailer. Rung 1's T27
    trailed unterminated garbage but its T5 (99 chars) also cut cleanly, so the junk is
    incidental to what follows the buffer in memory, not part of the signature.
(b) PREDICTED zero `114.MechCoy_R<k>` sub-routes. OBSERVED **0 occurrences** of the string
    `114.MechCoy_R` in the whole log, against the control's 29 occurrences / 4 distinct names
    (R0 x7, R1 x8, R2 x7, R3 x7). Offset-route creations: 4, and their owners are M1A2 1, 2,
    3, 4 - all four are 1222.MechPlt's members (the platoon's own offset route is built at
    :6332, eight lines after its intact task line at :6324, and 1222.MechPlt is created at
    :4336 from `Tank Platoon (USA).entity` with those members). THE COMPANY CONTRIBUTED ZERO.
    The control's owner set was M1A2 1, 5, 7, 8, 12, 15, 17 and HMMWV 1 - the extra seven are
    the company's sub-units, and every one of them is missing here.
    After :6335 the company emits NOTHING: no route lookup, no offset route, no leader
    selection, no warning, no completion. It is tasked and silent for 30 minutes.
(c) DISCRIMINATING SUB-CLAUSE - OUR OWN DISPATCH IS EXONERATED. vrfc2simapp.log:65 issues
    `CreateRoute 'T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE' (3 pts) for 114.MechCoy` and
    :73 logs `Route 'T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE' created; MoveAlongRoute
    issued for VRF_UUID:27304e58-f8c7-674c-8219-a6b842ecece1` - the FULL 44 characters on both
    sides. The ObjectCreated callback returned the full name, `_pendingRouteTasks` matched on
    it, and the move WAS dispatched. The failure is entirely on the far side of
    `moveAlongRoute(DtUUID, DtUUID)`. The alternative failure mode this clause was written to
    catch did not occur.

### P2 - THE BEHAVIOUR. PASS on (a) and (b).

(a) 114.MechCoy DID NOT MOVE. 900 POS samples spanning trace t=23.4 s to t=1859.6 s - a
    30-minute window - and the first and last are the SAME SIX-DECIMAL COORDINATE:
    34.647629,-116.693388 (alt 1116.7 -> 1116.6). Net displacement 0.0 m; MAXIMUM EXCURSION
    FROM THE FIRST SAMPLE ACROSS ALL 900 SAMPLES: 0.0 m. It ends 699.76 m from the control
    endpoint it reached with a 14-character route name. Registered threshold was 25 m; the
    measurement is zero.
(b) THE TWO UNCHANGED TASKEES ARE UNAFFECTED, to the centimetre:

    | taskee | trace final | vs control endpoint | net displacement |
    |---|---|---|---|
    | 1222.MechPlt | 34.612956,-116.587783 alt 1026.6 | **0.00 m** | 1163.9 m |
    | 1.BdeHQ | 34.608416,-116.699993 alt 1121.1 | **0.00 m** | 1162.9 m |
    | 114.MechCoy | 34.647629,-116.693388 alt 1116.6 | 699.76 m (never left spawn) | 0.0 m |

    Both completed with TSK records (1.BdeHQ t=37.4, 1222.MechPlt t=38.4; control 40.8 / 42.2)
    in the same relative order as the control.

STRONGEST SINGLE PIECE OF P2 EVIDENCE: vrfc2simapp.log is 99 lines against the control's 103,
and the four missing lines are EXACTLY the company's completion block - `VRF task complete:
114.MechCoy / move-along` and its `SENT TASK STATUS REPORT (TASKCMPLT) taskee=139aa71b-...
task=a5000000-...-0002` with their two `info: VrfC2Sim[0]` prefixes. Everything else in the
app log matches: 3 terrain requests (`sent for 3 vertices` x3), 3 replies, 3
`all 3 vertices authored from terrain + 10 m clearance` lines with alts [1050.6, 1043.9,
1036.7] / [1126.7, 1126.8, 1126.9] / [1141.4, 1136.3, 1131.1] - the SAME values Rows 2c, 2cR,
3 and the control produced, INCLUDING the company's, whose terrain authoring succeeded
normally before its route reference died. Cleanup deleted 9 objects in both runs.

### P3 - WHAT THE BACK END REPORTED. BRANCH (i), the pre-named aggregate pattern.

NO TASKCMPLT OF ANY KIND FOR 114.MechCoy: 2 TASKCMPLT lines in vrfc2simapp.log, 2 in
reports-captured.log, 2 TSK records in the trace, and zero of any of them naming the company
or taskee 139aa71b. Branch (ii) did NOT occur - `could not be setup` appears 0 times in
250,405 lines, so this aggregate did not even reach the ground-move controller that produced
rung-1's T23 warning. The operational corollary held exactly: `earlyExit.fired` false,
`completionLinesSeen` 2, `allCompleteUtc` null, window ran 1800.9 s of its 1800 s cap. This
is the four-frozen-aggregate signature of rung 1 reproduced on demand with one unit.

A TASKCMPLT WITH MOVEMENT - the thing that would have killed P2/P3 - did not happen.

### P4 - MODE CHECK. PASS on both decision criteria; SAMPLE IS THIN, recorded not glossed.

`python tools/analysis/frame_gaps.py . 20260902T143638Z_run`:

| statistic | THIS RUN | CONTROL 20260902T140808Z | threshold |
|---|---|---|---|
| lines / stamped / distinct sim stamps | 250405 / 24 / 7 | 21832 / 401 / 85 | - |
| TEST A in {0.033, 0.034} | 1/1 = 100.0% | 32/32 = 100.0% | >= 95% |
| TEST B resultant length R | **0.9977** | 0.9986 | >= 0.99 |
| TEST B \|residual\| <= 0.0005 s | 6/7 = 85.7% | 85/85 = 100.0% | >= 95% |
| TEST B residual sd | 0.00036 s | 0.00029 s | - |
| LS slope sim-s per wall-s | 13.11 | 10.18 | - |

F1 is decided (sec 7A A2 of the FFRTC prereg) by Test A < 95% **AND** Test B R < 0.99. Test A
is 100% and R is 0.9977, so F1 does not fire and the fixed-frame mode was in effect - which is
independently certain anyway, since the deployed fixture was verified by SHA-256 to be the
identical file the control loaded. HONEST CAVEAT: only 24 stamped lines and 7 distinct stamps
exist here against the control's 401 / 85, because a frozen unit generates almost no stamped
console activity and 67,590 of this log's lines are a single unstamped diagnostic. At n=7 the
85.7% residual figure is one stamp away from 100%, and n=1 makes Test A nearly vacuous. P4 is
a PASS on its registered decision rule, not a strong independent measurement.
The slope rose from 10.18 to 13.11 sim-s per wall-s, consistent with the FFRTC prereg's
measured load-dependence: a unit that never moves is cheaper to simulate.

### P5 - HYGIENE. MISS on the runner exit code; everything else clean.

THE MISS: **runner EXIT=5**, "unexpected terminating error: The property 'Count' cannot be
found on this object", at `scripts/RunC2SimScenario.ps1:2171 char:167`. DIAGNOSED, NOT LEFT AS
A SHRUG:

    2169:  $missing = @(Test-EarlyExit -State $completion -Taskees $OrderTaskees ... ).Missing
    2171:  Say-Info ('... {1} ...' -f $RunSecs, $(if ($missing.Count -gt 0) { ... }), ...)

`Test-EarlyExit` (RunnerLib.ps1:172-183) returns `Missing = @($Taskees | Where-Object ...)`.
The `@()` at :2169 wraps the FUNCTION CALL, not the property, so `.Missing` member-enumerates
over a one-element array and PowerShell UNWRAPS a single-element result to a bare `[string]`.
Under `Set-StrictMode -Version Latest` (:314) `.Count` on a scalar is an error, so :2171
throws. The branch needs EXACTLY ONE taskee missing: zero missing yields `$null` (also throws)
and two or more yields a real array (works). It is reached only when `-StopWhenComplete` FAILS
to fire - and every prior `-StopWhenComplete` run completed 3/3, so this code had never
executed live. THIS PROBE WAS DESIGNED TO PRODUCE EXACTLY ONE MISSING TASKEE, and it found the
bug. The fix is one character pair: `$missing = @( (Test-EarlyExit ...).Missing )`.
NOT APPLIED HERE - this probe changes data only; it is logged as the next repo task.

NOTHING WAS LOST. The throw is in a human-readable Say-Info AFTER the window closed and after
every artifact was written; the failure path still ran the full teardown. Verified: every
stage exit code 0 (RtiProbe, LaunchVrf, WatchVrf-precheck, WatchVrf-trace, ListenReports,
PushInit, VrfC2SimApp, PushOrder, StopIface, StopVrf); run-manifest.json complete, with
`clocks.observationEndUtc`, the full `oracle.earlyExit` block and `runnerExitCode: 5`;
watchvrf-trace.csv 44,161 POS + 16,818 RPT; reports-captured.log 55,069 lines; both vendor logs
captured.

EVERYTHING ELSE: StopVrf EXIT=0, "VR-Forces is down (graceful; RTI infrastructure preserved)";
both observers exited on the stop-file path and were never killed; RTI trio PIDs UNCHANGED and
explicitly preserved (rtiAssistant 41336, rtiexec 224608, rtiForwarder 76620); no new .dmp in
C:\MAK\vrforces5.0.2\bin64 (newest is still vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp,
2026-09-02 06:00); `env:Vrf__*` 0 before and after; NOTHING written under C:\MAK. Vendor-log
censuses THIS RUN / CONTROL: SocketException 0/0, "Waiting for nav data" 0/0, "moveAlong() -
empty route" 0/0, "invalid formation name" 1/1 (the standing baseline), FATAL 0/0.
POST-RUN SWEEP: `tools/ResetVrf 3741` - joined clean (BackendCount=0), discovered 0 reflected
(0 deletable, 0 nil), resigned cleanly, exit 0. ZERO LEFTOVERS. Same standing caveat (rung-1
finding D): run AFTER StopVrf, it proves NO STALE FEDERATE and nothing about scenario contents.
LEDGER: marker 3734 -> 3741 (7, by the runner) -> 3742 (1, hand-taken for the sweep and
ledgered BEFORE the join) - advanced by exactly 7 + 1 as predicted.

### NEW FINDING - THE FORENSIC'S UNEXPLAINED SYMPTOM IS NOW ATTRIBUTED

ANALYSIS_COASTP1_RUNG1_FREEZE sec 7 left `buildEntityRouteFollowingMap() : Can't find entity
route` unexplained: 14,880 occurrences (recounted here as 14,913) in rung 1, unattributable
because the line carries no object prefix at notify level 3. This probe attributes it by
manipulation:

    CONTROL  20260902T140808Z (three short names, all three marched):       0 occurrences
    PROBE    20260902T143638Z (one 44-char name, one frozen aggregate): 67,590 occurrences
    RUNG 1   20260902T125423Z (four long names, four frozen aggregates): 14,913 occurrences

The first occurrence is bin64-vrfSim.log:6342 - SEVEN LINES AND THE SAME SECOND (10:39:02)
after the company's truncated task line at :6335 - and the last is :250315 at 11:09:33, the
second the window closed. IT IS THE FREEZE'S DIAGNOSTIC: emitted per frame, by a unit whose
task route reference does not resolve, and completely absent when every route name fits.
The per-unit-per-frame rate is consistent between the runs once the clock is accounted for
(rung 1 ran at 1x with four freezers; this run ran at ~13x with one).
The line is still not attributable to a NAMED unit from the log alone - that has not changed -
but its CAUSE is no longer open. It is the only diagnostic this failure produces, and it says
"can't find entity route", which is precisely what an unresolved `DtSimObjectReference myRoute`
would say.

### ADVERSARIAL REVIEW - competing explanations for the observed cut, and what killed them

- "THE CONSOLE PRINT TRUNCATES, NOT THE PAYLOAD." REFUTED by behaviour. A print-side cut
  changes no execution: the route would still resolve, offset routes would still be built and
  the unit would still march. Instead the company built zero offset routes and did not move
  0.0 m in 900 samples. A logging artefact cannot do that.
- "THE PADDED NAME CONTAINS A CHARACTER VR-FORCES DISLIKES." REFUTED by construction. The pad
  uses only `[A-Z0-9_]`, a strict subset of the alphabet already present in the control name
  `T_R5_CO1` (letters, digits, underscore). No new character class was introduced; only length
  changed.
- "THE ROUTE OBJECT WAS NEVER CREATED AT 44 CHARACTERS." REFUTED directly: :6295/:6296 create
  it at full length and :250337 removes it at full length, same VRF_UUID.
- "THE FAILURE IS IN OUR OWN `_pendingRouteTasks` LOOKUP." REFUTED by P1(c): the app logged
  the full 44-character name on both the create and the dispatch.
- "114.MechCoy IS JUST A UNIT THAT FREEZES." REFUTED by the control: THE SAME UNIT, in THE SAME
  scenario, from THE SAME spawn coordinate, with THE SAME route geometry and THE SAME terrain
  vertices, under a 14-character name, marched 698 m and reported TASKCMPLT 4 hours earlier.
  The in-run controls say the same thing from the other side: two taskees whose names did not
  change landed on their control endpoints to 0.00 m.

STILL INFERRED, NOT PROVEN, and deliberately left that way: that `DtUUID::myData[36]`
(rwUUID.h:412) is the specific buffer doing the cutting. The header's own comment plus a cut at
exactly 35 characters - one type byte short of 36 - make it the only fitting candidate, and
`moveAlongTasks.h:83 setRoute(const DtUUID&)` is the only transport between our call and the
controller; but nothing in this run reads the blob. The 34/35/36-character boundary itself was
NOT bisected here: this probe tested 14 vs 44, so it establishes that LENGTH is the cause and
that 44 fails, not that 35 is the exact threshold. Rung 1's observational table remains the
only evidence for where the boundary sits.

NOTHING ELSE IS UNEXPLAINED IN THIS RUN.

### CONSEQUENCE FOR THE FIX

The fix named in the forensic's sec 8 is now justified rather than plausible: cap the name
passed to `MoveAlongRoute` at 34 characters, or better, name routes with a short synthetic id
the way VR-Forces names its own (`114.MechCoy_R0`, 14 characters, resolved fine in the
control). The C2SIM task name belongs in the log line, not the object name. Independently, the
dropped `DtTaskCompleteReport::success()` (forensic sec 6) stays on the list. Neither is
applied here - this was a data-only probe, and the fix is a separate, testable change.

## 7. REGISTRATION

Sections 0-5B above were registered in commit d5aee8b,
together with data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml, BEFORE the launch command
was issued. Sec 6 is the only content added afterwards.
