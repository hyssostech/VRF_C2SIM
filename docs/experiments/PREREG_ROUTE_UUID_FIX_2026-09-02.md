# PREREG ROUTE-UUID FIX - address the route by its REAL uuid, not by its name - registered 2026-09-02, BEFORE launch

WHAT THIS IS: run 20260902T143638Z (the route-name-length probe that FROZE 114.MechCoy with a
44-character route name; docs/experiments/PREREG_ROUTE_NAME_LENGTH_2026-09-02.md sec 6, commit
854841a) run again with EXACTLY ONE VARIABLE MOVED: the APP CODE. The order file, the init, the
scenario fixture, the deployed bridge DLL, the settings and the runner switches are all byte-
identical to that run. The C2SIM task name, and therefore the 44-character route name, is
UNCHANGED - deliberately. The change is that `VrfC2SimService.OnVrfObjectCreated` now passes the
created route's REAL uuid (`e.Uuid`, the "VRF_UUID:..." string the callback already carried)
into MoveAlongRoute / PatrolRoute / PlanAndMoveTo instead of the route's NAME (`e.Name`).

WHY IT MATTERS. The name-length probe SETTLED the cause by manipulation but applied no fix. This
run tests the fix that the vendor's own headers prescribe, on the exact input that broke: if the
freeze was the name being squeezed through a 36-byte DtUUID blob, then handing that call a real
uuid must make a 44-character route name march exactly like a 14-character one. A pass turns the
name length from a constraint we must work around (the withdrawn "short synthetic route ids"
candidate) into a non-issue: names stay full length and human-readable, and the vendor's
documented 255-character limit is the only limit that applies.

## 1. DOCS AND SOURCES CONSULTED (docs first, per the 2026-09-01 directive)

VENDOR HEADERS, read-only under C:\MAK, opened and re-read line by line for this prereg:

- `C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h:246-253` - THE LOAD-BEARING CITATION. The
  DtUUID string constructors, with their own doc block verbatim:
      //! If string is a UUID (VRF_UUID:) then sets a valid UUID from the string,
      //! else will have an invalid UUID.  Check isValid after the constructor is
      //! called to see if it is a valid UUID.  If blockMarkingTextLookup is true, if the string
      //! given is an object marking text (not UUID) blocks the lookup to map the marking text to the UUID and
      //! keeps the uuid as the object marking text
      explicit DtUUID(const DtString&, DtUUIDOwner* = 0, bool blockMarkingTextLookup = false);
      explicit DtUUID(const std::string&, DtUUIDOwner* = 0, bool blockMarkingTextLookup = false);
      explicit DtUUID(const char*, DtUUIDOwner* = 0, bool blockMarkingTextLookup = false);
  So the ONLY string form that yields a VALID uuid is "VRF_UUID:...". Any other string is
  handled as OBJECT MARKING TEXT and goes through a lookup - the path our route NAME took.
- `rwUUID.h:409-412` - the storage that does the cutting: "The UUID has been changed to be a
  memory blob of fixed size. The blob's format is the first char is the type, and the rest is
  the data" / `char myData[36]`. 1 type byte + 35 payload = the observed 35-character cut.
- `C:\MAK\vrforces5.0.2\include\vrfcontrol\vrfRemoteController.h:102-103` - the create callback
  ALREADY HANDS US THE UUID; we were throwing it away for this purpose:
      typedef void (*DtVrfObjectCreatedCallbackFcn)
         (const DtString& name, const DtEntityIdentifier& id, const DtUUID& uuid, void* usr);
- `C:\MAK\vrforces5.0.2\doc\VRFUsersGuide.pdf` sec 41.1 "Overview" (ch. 41, Introduction to
  Tactical Graphics), printed page 989: "Tactical graphics have names. New objects are assigned
  default names in the format Point1, Point2, Route1, Route2, and so on. You can specify an
  alternative name when you create the object. A GRAPHICAL OBJECT'S NAME CAN BE UP TO 255
  CHARACTERS LONG." The 44-character name is therefore LEGAL and stays as it is; shortening it
  would have been working around our own defect, not the vendor's limit.

OUR SOURCE, re-opened (not quoted from memory):

- `src/VrfC2SimApp/VrfC2SimService.cs:1110` - `_vrfUuidByName[e.Name] = e.Uuid;` The handler has
  had the route's real uuid in hand since the beginning.
- `src/VrfC2SimApp/VrfC2SimService.cs:1166` (pre-fix) - `_bridge.MoveAlongRoute(m.Uuid, e.Name);`
  and its three siblings at :1151 (PatrolRoute), :1158 (PlanAndMoveTo), :1172 (MoveAlongRoute).
  The TASKEE was always addressed by uuid and always worked; only the ROUTE was addressed by
  name. That asymmetry is the defect.
- `src/VrfFacade/VrfFacade.cpp:211` - `ev.uuid = uuid.uuidString().string()`, i.e. `e.Uuid` IS
  the "VRF_UUID:..." form the rwUUID.h:246-253 contract requires.
- `src/VrfFacade/VrfFacade.cpp:569-571` (MoveAlongRoute -> `DtUUID(routeUuid)`), `:574-579`
  (PlanAndMoveTo -> `setControlPoint(DtUUID(...))`), `:694-701` (PatrolRoute ->
  `setRoute(DtUUID(...))`). ALL THREE take a DtUUID. Nothing native changes; the same call sites
  now receive a string that satisfies their contract.

PRIOR RUN EVIDENCE:
- docs/experiments/PREREG_ROUTE_NAME_LENGTH_2026-09-02.md sec 6 (854841a) - the manipulated
  cause, run 20260902T143638Z, and its P1(c) finding that OUR DISPATCH IS EXONERATED (the app
  logged the full 44-character name on both create and dispatch), which is what makes this a
  one-line fix on the far side of the call rather than a callback-path repair.
- docs/experiments/PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8 (c0e90b7) - the FFRTC control,
  run 20260902T140808Z, and the frame-mode decision rule reused here as P4.

## 2. THE ONE VARIABLE - the code diff

`src/VrfC2SimApp/VrfC2SimService.cs`, the pending-route block inside OnVrfObjectCreated. Four
call sites, one substitution each, plus the comment that said "the along-route task resolves the
route by name, so pass e.Name (== routeName)" (that sentence was WRONG - it is what this run
falsifies in code):

    -   _bridge.PatrolRoute(pending.TaskeeVrfUuid, e.Name);      ->  ..., e.Uuid);
    -   _bridge.PlanAndMoveTo(pending.TaskeeVrfUuid, e.Name);    ->  ..., e.Uuid);
    -   _bridge.MoveAlongRoute(m.Uuid, e.Name);                  ->  ..., e.Uuid);   (R10 fan-out)
    -   _bridge.MoveAlongRoute(pending.TaskeeVrfUuid, e.Name);   ->  ..., e.Uuid);

The `_pendingRouteTasks` QUEUE is still keyed by the route NAME - that is the only handle
CreateRoute gave us and it is proven to work (the callback returns the full name; the lookup hit
in the freezing run). Only the TASK's route reference changes. The four log lines now carry
BOTH, `Route '{Route}' ({RouteUuid}) created; ...`, which is also the deploy fingerprint below.

ALSO CHANGED, and declared here so it is not mistaken for a second experimental variable:
- `scripts/RunC2SimScenario.ps1:2169` - `$missing = @(Test-EarlyExit ...).Missing` ->
  `$missing = @( (Test-EarlyExit ...).Missing )`. The repo defect the last run FOUND (runner
  EXIT=5 under Set-StrictMode; sec 6 P5 of the name-length prereg). It runs only on the
  did-not-fire branch AFTER the window closes and cannot affect any measurement in this run;
  P5 covers it.
- `tests/RunnerTurnaround.Tests.ps1` - 5 new offline checks (section 8) pinning that defect,
  INCLUDING a check that the OLD form really does unwrap to a bare string (so the new checks
  cannot pass vacuously). Gate: 101 passed, 0 failed.
- `src/VrfFacade/VrfFacade.h:349-358` - COMMENT ONLY. The old text claimed "routeUuid is the
  route name, resolved like MoveAlongRoute", which was true of neither call. A comment in a
  header changes no compiled code: THE DEPLOYED VrfBridge DLL IS UNTOUCHED AND WAS NOT REBUILT
  (deployed bridge stays A7504441, 10/10 copies). The same stale claim also sits in
  VrfFacade.cpp:696-697 and is DELIBERATELY LEFT for the queued native item (success()/taskId()
  forwarding), because this task changes no native source.

## 3. EVERYTHING ELSE HELD

ORDER: `data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml` - THE PADDED ONE, unchanged, 3919
bytes, mtime 2026-09-02 10:29:56. The company's task name stays 38 characters and its route name
stays 44. THIS IS THE POINT: the input that froze is re-run verbatim.

INIT: `data/R9_Mojave_Lean_Initialization_NoComments.xml`, unchanged.

FIXTURE: `TropicTortoise_FFRTC` - ALREADY DEPLOYED, VERIFIED BY HASH, NOT REDEPLOYED:
`C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise_FFRTC.scnx`, 7112 bytes, mtime
2026-09-02 10:03:30, SHA-256 D27E540F8BCCAA2EBDD33C63CF062CB4842DBE1937B7AA3DAC1C20F9C990B0B9 -
identical to the value PREREG_ROUTE_NAME_LENGTH sec 3 and PREREG_R9_FIXED_FRAME_RTC sec 8
recorded. NOTHING IS WRITTEN UNDER C:\MAK BY THIS RUN.

Also held: TypeMappingMode RealTemplates; GroundWaypointAltitudeMode TerrainProfile (compiled
default) with TerrainClearanceMeters 10 and TerrainProfileTimeoutSeconds 10; SubordinateFanOut
off; AggregatePlanAndMove off; NavArea disabled; stock templates; the deployed VrfBridge
A7504441; vrfSim.mtl notify level 3; TimeMultiplier 1 (frame mode, not multiplier); zero
`Vrf__*` environment variables; `-RunSecs 1800 -SampleSecs 2 -StopWhenComplete` exactly as both
the control and the freezing run used them.

DEPLOY PROOF, recorded BEFORE launch. The runner starts the app straight from the build output
(`RunC2SimScenario.ps1:382`), so building IS deploying; there is no copy step to get wrong.
  build: `dotnet build src/VrfC2SimApp -c Release --disable-build-servers` (0 errors, 6 pre-
    existing warnings), 2026-09-02 11:30:09 local.
  src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.dll
    SHA-256 3b7b8d2eb71ee5ca8228cb305b9c368baaedeb4dac65adac006b8c6edc60cea0
  src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.exe
    SHA-256 ed3797eaff70a32cdecbe9881d0b855e2e834beddbf6c5fffa04321e4e299778
  STRING EVIDENCE inside that DLL (UTF-16 scan of the metadata heap): all FOUR new format
  strings present - "({RouteUuid}) created; MoveAlongRoute issued for {Vrf}.",
  "({RouteUuid}) created; PatrolRoute issued for {Vrf} (Reconnoiter).",
  "({WptUuid}) created; PlanAndMoveTo issued for {Vrf} (R11).",
  "({RouteUuid}) created; R10 fan-out MoveAlongRoute issued to {N} members of {Vrf}." - and all
  three OLD forms ABSENT ("Route '{Route}' created; MoveAlongRoute issued for {Vrf}.",
  "Route '{Route}' created; PatrolRoute issued", "Waypoint '{Wpt}' created; PlanAndMoveTo
  issued"). The previous binary was 2026-09-02 07:31.
  OFFLINE GATES, all green on this binary: --translator-selftest, --report-selftest,
  --sequencer-selftest, --verb-selftest, --destack-selftest, --fanout-selftest (all EXIT 0);
  tests\RunnerTurnaround.Tests.ps1 101 passed / 0 failed.

## 4. INVOCATION (main checkout, VRF_C2SIM, pwsh) - no env line at all

    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_LongCoRouteName.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 1800 -SampleSecs 2 -StopWhenComplete

Adjudication from the run directory artifacts ONLY (bin64-vrfSim.log, vrfc2simapp.log,
watchvrf-trace.csv, reports-captured.log, run-manifest.json, the console log).

APP NUMBERS. The marker at docs/OPUS_EXECUTION_PLAN.md Appendix B reads `*** NEXT FREE: 3742 ***`
at registration time, so the expected block is 3742-3748 (7 numbers) with the marker advancing to
3749, and the post-run ResetVrf sweep then consuming 3749 and advancing to 3750. The actual
wasValue/newValue from run-manifest.json is recorded in sec 6.

PRE-LAUNCH INVENTORY, taken at registration and re-checked at launch (must hold, else STOP -
never kill): no vrfSim* / vrfGui / vrfLauncher / WatchVrf / ListenReports / VrfC2SimApp process
of any kind (verified: the ONLY matching processes are the RTI trio); RTI trio present with the
PIDs the whole 2026-09-02 record carries - rtiAssistant 41336 / rtiexec 224608 / rtiForwarder
76620 - NEVER killed; docker stp-server + c2sim_server4.8.4.9 + stp-lt511 all Up; `Get-ChildItem
env:Vrf__*` count 0; newest bin64 dump still vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp
(2026-09-02 06:00); the FFRTC fixture hashing to the sec-3 value.

## 5. PREDICTIONS - registered before launch, with confidence and falsifiers

CONTROL VALUES. TWO controls, both quoted from their own artifacts:
  CONTROL-A = run 20260902T143638Z, the FREEZING run. Same order, same fixture, OLD binary. This
    is what the fix must overturn:
      bin64-vrfSim.log:6335  114.MechCoy ...Task 0 ... Move-Along Route:
                             "T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3"   <- 35-char cut, 1 occurrence
      "114.MechCoy_R"  0 occurrences;  company offset routes 0 (only the platoon's 4 exist)
      "buildEntityRouteFollowingMap() : Can't find entity route"  67,590 occurrences
      114.MechCoy POS first == last, 34.647629,-116.693388, net 0.0 m over 900 samples / 1859 s
      2 TASKCMPLT (no company); earlyExit.fired false; window ran its 1800 s cap; runner EXIT=5
  CONTROL-B = run 20260902T140808Z, the UNPADDED FFRTC control (short names, all three marched):
      114.MechCoy   34.647629,-116.693388 -> 34.653915,-116.693388 alt 1116.8   (698 m)
      1222.MechPlt  34.612956,-116.600487 -> 34.612956,-116.587783 alt 1026.6
      1.BdeHQ       34.608416,-116.712685 -> 34.608416,-116.699993 alt 1121.1
      "114.MechCoy_R" 29 occurrences, 4 distinct (R0 x7, R1 x8, R2 x7, R3 x7)
      "buildEntityRouteFollowingMap() : Can't find entity route"  0 occurrences
      3/3 TASKCMPLT; order push -> last TASKCMPLT 20.18 s wall; earlyExit fired at t+66 s
      frame_gaps TEST A 32/32 = 100.0%, TEST B R = 0.9986

---

P1 - THE MECHANISM (HIGH CONFIDENCE). The route reference reaches the back end INTACT, and the
     aggregate builds its offset routes.
  (a) ZERO occurrences of the 35-character cut form `T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3` in
      bin64-vrfSim.log (CONTROL-A: exactly 1, at :6335). The company's move-along task line
      instead references the route by its VRF_UUID (e.g. `Move-Along Route: "VRF_UUID:<guid>"`)
      OR, if the back end resolves the uuid back to the object for display, by the FULL
      44-character name `T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE`. EITHER FORM PASSES - the
      claim under test is that nothing is CUT, not which representation the console prints.
  (b) The company builds its member offset routes: `114.MechCoy_R` appears with at least 4
      DISTINCT sub-route names R0-R3 (CONTROL-B: 29 occurrences, 4 distinct; CONTROL-A: 0), and
      `Offset Route (VRF_UUID` creations exceed the platoon's 4 (CONTROL-B: 8 owners, the extra
      seven being the company's sub-units).
  FALSIFIER: the 35-character cut form appears at all, OR zero `114.MechCoy_R` sub-routes are
      created. Either one says the fix did not reach the controller.
  (c) OUR SIDE OF THE CALL, recorded for the record: vrfc2simapp.log must contain
      `Route 'T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE' (VRF_UUID:<guid>) created;
      MoveAlongRoute issued for VRF_UUID:<company>` - the NEW log form, which is also the
      in-run proof that the new binary is the one that ran. If the OLD form appears, the run is
      VOID (wrong binary) and is not adjudicated at all.

P2 - THE BEHAVIOUR (HIGH CONFIDENCE). All three taskees complete, and the company lands where
     the short-named control landed.
  (a) 3/3 TASKCMPLT in vrfc2simapp.log and in reports-captured.log, with a TSK record for each
      of the three taskees in watchvrf-trace.csv (CONTROL-A: 2, no company).
  (b) 114.MechCoy's trace-final POS is within 1 m of CONTROL-B's endpoint
      34.653915,-116.693388 (alt 1116.8), i.e. it marches its ~698 m. The record's own
      replicate spread for this order is 0.09 m across three runs (FFRTC prereg sec 8), so 1 m
      is a loose threshold, deliberately.
  (c) The other two taskees are unchanged: 1222.MechPlt within 1 m of 34.612956,-116.587783
      (alt 1026.6) and 1.BdeHQ within 1 m of 34.608416,-116.699993 (alt 1121.1) - both were
      already at 0.00 m from these in CONTROL-A, so a shift here would mean the code change
      perturbed something it should not have touched.
  FALSIFIER: fewer than 3 TASKCMPLT; or 114.MechCoy more than 1 m off CONTROL-B's endpoint
      (in particular, still sitting at its 34.647629,-116.693388 spawn); or either unchanged
      taskee more than 1 m off.

P3 - THE FREEZE DIAGNOSTIC IS GONE (HIGH CONFIDENCE). ZERO occurrences of
     `buildEntityRouteFollowingMap() : Can't find entity route` in bin64-vrfSim.log.
     CONTROL-A 67,590; CONTROL-B 0; rung 1 14,913. The name-length probe attributed this line to
     a unit whose task route reference does not resolve; if the reference now resolves, the line
     must not be emitted at all.
     FALSIFIER: any non-zero count. A small non-zero count is NOT a pass with a caveat - the
     control that marched produced exactly zero.

P4 - MODE CHECK, CHEAP (HIGH). `python tools/analysis/frame_gaps.py . <run>`: TEST A >= 95% of
     sub-0.06 s gaps in {0.033, 0.034} AND TEST B resultant length R >= 0.99 - the thresholds
     registered in PREREG_R9_FIXED_FRAME_RTC sec 7A A2 (F1 fires only if BOTH fail). A held-
     variable check that the fixed-frame fixture is still in effect, not a finding. The sample
     should be RICHER than CONTROL-A's thin 24 stamped lines, because three moving units
     generate stamped console activity (CONTROL-B: 401 stamped / 85 distinct).

P5 - HYGIENE (HIGH), including the runner fix. Runner EXIT=0 and every stage exit code 0. If
     -StopWhenComplete fires (which P2 implies it must), :2169 is not even reached - so the
     STRONGER statement is the one under test at :2169's own branch, and it is covered offline by
     the 5 new checks in tests\RunnerTurnaround.Tests.ps1 section 8; live, the claim is simply
     that nothing regressed. StopVrf reports VR-Forces DOWN with no force-kill; both observers
     exit on the stop-file path; RTI trio PIDs UNCHANGED (41336 / 224608 / 76620) and never
     touched; no new .dmp in C:\MAK\vrforces5.0.2\bin64 (newest stays
     vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp, 2026-09-02 06:00); `Get-ChildItem env:Vrf__*`
     count 0 BEFORE and AFTER; the post-run `tools/ResetVrf <fresh appNo>` sweep reports ZERO
     leftovers; the marker advances 3742 -> 3749 (7, by the runner) -> 3750 (1, hand-taken and
     ledgered BEFORE the join, for the sweep).
     STANDING CAVEAT (rung-1 finding D, repeated by both controls): the sweep runs AFTER StopVrf,
     so it proves NO STALE FEDERATE and nothing about scenario contents.

## 5A. THE MISS RULE

A MISSED HIGH-CONFIDENCE PREDICTION IS A STOP. P1, P2, P3, P4 and P5 are all registered HIGH. If
any of them misses, the work stops, the miss is adjudicated in sec 6, and NOTHING IS RETUNED - no
second name length, no shortened route name, no second run, no adjustment of a prediction to fit
what came back. A P1(c) OLD-log-form observation VOIDS the run outright (wrong binary) rather
than scoring it. A P4 miss means the fixture/mode path moved and the run is uninterpretable as a
one-variable probe - STOP and diagnose per PREREG_R9_FIXED_FRAME_RTC sec 7 F1. A P5 miss is
infrastructure: record it, and after TWO infrastructure failures this session, stop entirely.
The crash-dump prompt is answered with `pwsh -File scripts\AnswerCrashDumpDialog.ps1` then
`pwsh -File scripts\StopVrf.ps1` (RUNBOOK 0.5.12, ALWAYS Yes).

## 5B. WHAT A CLEAN PASS WOULD AND WOULD NOT ESTABLISH

WOULD: that the aggregate freeze is REPAIRED by addressing the route through the uuid the vendor's
own callback hands us, on the exact input that produced the freeze - i.e. that the defect was
OURS (a contract violation at rwUUID.h:246-253), that route names may stay full-length and
human-readable up to the documented 255, and that the "cap the name at 34 characters / short
synthetic route ids" candidate is unnecessary and is WITHDRAWN.

WOULD NOT: prove anything about COA-STP1 AT SCALE. This run has ONE long-named aggregate; rung 1
had four, among 128 units and 42 tasks, and its other findings (the vacuous ENTITY completion
that falsely released T24; the echelon-'F' generic fallback) are untouched by this change and
remain open. Nor does it read `DtUUID::myData[36]` - the blob remains the INFERRED cutter, as
sec 6 of the name-length prereg already recorded. Nor does it validate PatrolRoute or
PlanAndMoveTo live: those two call sites are changed identically and by the same argument, but
this order exercises neither (no SCREEN/SCOUT verb, AggregatePlanAndMove off). That is stated
here so a pass is not over-read into them.

## 6. OUTCOME (written from the run directory artifacts, AFTER the run)

PENDING - to be written after the run.

## 7. REGISTRATION

Sections 0-5B above, together with the source, script and test changes they describe, were
registered in commit <stamped after commit> BEFORE the launch command was issued. Sec 6 is the
only content added afterwards.
