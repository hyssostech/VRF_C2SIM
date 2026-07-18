# PHASE 1 SESSION SCRIPT - native reference baseline (TropicTortoise)

Status: READY. Groundwork plan Phase 1; this IS the next live session.

*** BRING-UP (corrected 2026-07-18 evening - an earlier version of THIS HEADER said
"the user launches VR-Forces MANUALLY" and "the 0.4 gate was DEMOTED". BOTH ARE
RETIRED.) *** 0.4 PASSED and VR-Forces launches UNATTENDED via
scripts/LaunchVrf.ps1 - see the Preconditions block below for the command, the
never-kill-RTI-infrastructure rule, and the MANDATORY oracle pre-check.

The 0.6 console-capture live gate is FOLDED INTO this session rather than run
separately. Roles: the supervisor brings the system up, records, timestamps and
gates; the user drives the VR-Forces GUI for creation/tasking steps. Evidence rules per
SUPERVISED_RECOVERY_PLAN.md (WatchVrf displacement is the only movement oracle).

BUDGET (revised 2026-07-18, was "~1 hour"): plan for ~1.5 hours. The original
hour assumed the clock-persistence test rode along inside Step 2's concurrency
wait; moving it to Step 1b to keep it out of the scored window makes it SERIAL
and costs 5-10 min off the top. Rough shape: setup/Step 0 console capture and
Step 1 creation ~15 min; Step 1b ~10 min; Step 2 dominated by a ~40-45 min
concurrent 1x movement wait (20+ km routes at ~30 km/h column pace); Step 4 and
teardown ~15 min. The Step 2 wait is shared across all three movers ONLY if they
are tasked within a few minutes of each other - if that slips, the session
overruns. If time runs short, Step 4 (the 20x repeat) is the designated cut:
P1-C is the least load-bearing prediction and can move to a later session, while
P1-A/P1-B (native arrival and the 18.1-18.4 km band) are the reason this session
exists and must not be truncated.

Everything below uses ONLY the VR-Forces GUI for creation and tasking. The one
deliberate exception is the clock: it is set remotely via tools/SetSimRate
(D1, resolved 2026-07-18 - see Step 4).

## What this session settles

1. The native acceptance baseline: telemetry from GUI-created, GUI-tasked REAL
   types is the target every later port run is scored against (Phase 3/5).
2. Environment vs interface: if NATIVE units also stall at the 18.1-18.4 km band
   or run away, the cause is environmental/VRF-internal and the MAK support
   question fires immediately with this repro - before anything is rebuilt.
3. First native evidence on the controller-split hypothesis (ground truth 0.0
   item 2): the force deliberately spans the LF/HU controller boundary.
4. Whether the 20x multiplier (used by every port run) is itself implicated in
   the warp/runaway class (native 1x vs 20x, same unit, same route).

## Pre-registration (written 2026-07-16, before the run)

- P1-A (baseline): native M1A2, Tank Platoon (USA), and Tank Company (USA),
  tasked along COA-shaped routes at 1x, all arrive - WatchVrf center
  displacement reaches the last vertex (unit tolerance: leading edge per doc
  semantics) with zero runaways. FALSIFIER: any native non-arrival or runaway
  at 1x.
- P1-B (18.4 km band): no native mover crossing 18.1-18.4 km cumulative from
  its start stops in that band at 1x. BOTH the platoon (LF class) and the
  company (HU class) cross the band, so a stop cannot be mis-attributed
  between controller class and the band. FALSIFIER: any native stop in the
  band (=> environmental cause; MAK question fires with this repro; the
  Terrain Page-in Area remedy in Step 3 becomes the immediate follow-up probe).
- P1-C (clock): if the Step 4 20x repeat warps/overshoots while the same unit
  and route were clean at 1x, the multiplier is implicated (every port run used
  20x). If both are clean, the multiplier is exonerated for native units and
  the runaway cause moves back to the creation/tasking difference.
- P1-D (controller split, observational): the company (HU class) shows the
  leading-edge completion behavior (task reported complete when the formation
  front reaches the last vertex - premature BY DESIGN per ground truth 0.2
  sec 3b) and the platoon (LF class) publishes member positions throughout.
  FALSIFIER for the hypothesis: platoon and company behave identically despite
  the different wired controllers.

## Preconditions (run through in order; abort the session if any fails)

- [ ] VR-Forces up. CHANGED 2026-07-18 - scripts/LaunchVrf.ps1 NOW WORKS UNATTENDED
      and is the preferred bring-up; the "launch manually" instruction is retired:
        pwsh -File scripts\LaunchVrf.ps1 -Scenario TropicTortoise `
             -BackendAppNumber <fresh> -FrontendAppNumber <fresh>
      Expect EXIT=0 and "[OK] READY". Do NOT wait for rtiexec as a readiness signal
      and do NOT treat UDP 4000 as health - health is THREAD COUNT (blocked 2-4,
      healthy 23-67). Raw vrfSim CLI remains forbidden.
      *** NEVER KILL rtiAssistant / rtiexec / rtiForwarder *** - they are RTI
      infrastructure and an already-answered rtiAssistant is what makes unattended
      launch work (RUNBOOK sec 0.5).
      IF the "Choose RTI Connection" dialog appears (e.g. after a reboot): answer it
      once with "Always try to use this connection" CHECKED, then Connect. It is a Qt
      window with no UI Automation tree - automate by screenshot + coordinate click if
      needed (Connect centre is window-relative (383,553) on a 573x583 dialog).
- [ ] ORACLE PRE-CHECK (MANDATORY - CRITERION CORRECTED 2026-07-18 evening; the
      full evidence and the superseded rule are in RUNBOOK sec 0.5.7). The OLD rule
      said "require reflected>0; if 0 after 20 s, STOP". BOTH HALVES ARE WRONG:
      * reflected>0 PASSES ON GARBAGE. The TropicTortoise baseline objects are
        POSITIONLESS - they reflect as 90.000000,-90.000000,0.0 and as NaN. A live
        pre-check scored reflected=3 readable=2 and "passed" with nothing but those.
      * reflected=0 at 20 s IS NOT A BLIND ORACLE - it is usually MEASURED TOO EARLY.
        LaunchVrf's READY is thread-count + main-window only and does NOT imply the
        scenario is loaded or the federation joined. Same launch, same scenario:
        visible at ~40 s, BLIND at ~20-50 s, visible again at ~104 s.
      USE INSTEAD:
      * PASS = at least one POS line with REAL lat/lon (not NaN, not the 90/-90 pole).
      * RETRY = reflected=0 or all-degenerate. Re-run with a FRESH ledgered appNo;
        allow up to ~3 MINUTES from launch before concluding anything.
      * STOP = no real-coordinate POS after ~3 min AND a CreateOne entity also fails
        to appear with real coordinates.
      THE "STRONGER CHECK" IS THEREFORE MANDATORY, NOT OPTIONAL - on a stock
      TropicTortoise load it is the only check that CAN pass: tools/CreateOne creates
      one throwaway M1A2 and WatchVrf must report REAL coordinates for its uuid.
      VERIFIED 2026-07-18: created uuid read back 34.517156,-116.973525,1060.7 -
      requested lat/lon exact, 10000 m MSL ground-clamped.
      RELAUNCH AFTERWARDS so the throwaway never enters the scored trace - VERIFIED
      to work: after StopVrf + LaunchVrf the created uuid was gone and the counts
      returned to the reflected=3 readable=2 baseline (evidence that the oracle was
      NOT merely blind at the time of the check).
- [ ] TropicTortoise (Mojave) scenario loaded; confirm scenario parameters are
      the defaults: frame-mode variable-frame, frame-time 0.1,
      time-multiplier 1.0 (ground truth 0.2 sec 8).
- [ ] Fresh appNos, ALL ledgered BEFORE their joins. Take them from the single
      line marked "*** NEXT FREE:" in OPUS_EXECUTION_PLAN.md Appendix B (search for
      that exact string - it is the ONLY authoritative value; do NOT infer one from
      the highest number you see). Reserved for this session: 3455 WatchVrf, plus
      3456-3459 for SetSimRate. YOU ALSO NEED TWO MORE for LaunchVrf itself (its
      back-end and front-end each consume one) - take those from the NEXT FREE
      marker and advance it.
- [ ] FOUR MORE fresh appNos reserved and ledgered for SetSimRate (Step 1b up +
      down, Step 4 up + down - one join each), all distinct from WatchVrf's.
      SetSimRate has NO default appNumber; a missing appNo is a hard exit 2, so
      this reservation is not optional.
- [ ] Get-Date logged at session start (tool logs stamp UTC; machine runs
      local - record both once, reconcile all later timestamps against this).
- [ ] Settings > Display > Entity Display Settings > "Show Object Console
      Warning Icon" = ON (it is the default; verify anyway).
- [ ] WatchVrf running and logging BEFORE any object is created (births must be
      in the trace). USE THE EXTENDED 0.6 BUILD (POS + CON lines) - it is
      AVAILABLE, and THIS SESSION IS ITS LIVE GATE (see the folded-in note at
      the end of this script and the plan Status). Do not wait for a gate that
      only this session can run. VERIFIED BY SUPERVISOR 2026-07-18 immediately
      before the session: tools/WatchVrf/bin/Release/net10.0/win-x64/
      WatchVrf.exe exists and is NEWER than its last source commit (50a5c0c),
      and --con-selftest re-run by hand passes 25/25 exit 0. If the CON stream
      proves faulty live, fall back to the Step 0 GUI capture protocol and
      record the 0.6 gate as FAILED - the POS records are unchanged by the 0.6
      refactor (S0 gate: the POS emission block does not appear in the diff at
      all), so a CON fault does NOT invalidate the movement oracle or any
      P1-A..D claim.
- [ ] WatchVrf sample interval SHORT: sampleSecs = 2 (not the default 15). Why
      (census sec 11 / ground truth 0.0 item 6): transient member "warps" in the
      archived data are suspected OBSERVER-SIDE dead-reckoning artifacts; at a
      2 s sample the DR excursion-and-snap-back pattern is resolvable (spiky
      track) vs real motion (smooth track), making this session itself the
      first artifact-vs-real discriminator at zero code cost.

## Step 0 - Object Console FIRST (before creating anything)

- [ ] View > Object Console Summary Panel: open it and keep it open all session.
- [ ] Capture (screenshot + transcribe) anything already in it.
- [ ] PROTOCOL - the badge-clearing gotcha (ground truth 0.2 sec 7): opening a
      unit's Information dialog REMOVES its warning icon, and Acknowledge All
      clears the panel. Capture message text from the Summary Panel FIRST;
      never open an Information dialog or press Acknowledge All before the
      supervisor confirms the capture is recorded.

## Step 1 - palette-create the real force (Click to Create, default properties)

Types are from the 0.1 catalog - REAL installed templates, no generics:

- [ ] 1 x M1A2_Abrams_MBT entity (DIS 1.1.225.1.1.3.0).
- [ ] 1 x Tank Platoon (USA) (objectType 3:11:1:225:3:2:0:0; LF lead-follow
      class; 4 x M1A2; expected birth formation Column-Left).
- [ ] 1 x Tank Company (USA) (objectType 3:11:1:225:5:2:0:0; HU move-along
      class; 1 x Tank HQ Section + 3 x Tank Platoon).
- [ ] 1 x ADDITIONAL M1A2_Abrams_MBT entity created SOLELY as the Step 1b
      clock-test mover. Give it a distinguishing GUI name so it is unmistakable
      in the telemetry - suggested: CLOCKTEST. It is a THROWAWAY: it is
      EXCLUDED from all P1-A / P1-B / P1-C / P1-D scoring, and no claim in this
      session may rest on it. Record its uuid explicitly so the post-session
      adjudication can filter it out of the WatchVrf trace.
- [ ] Placement: dispersed (>= 500 m apart) in the COA-STP1 AO start area (the
      same area the CPP-ALT-1 force used). Place CLOCKTEST clear of the three
      scored objects' routes so it cannot be confused with them visually.
- [ ] Record per object: GUI name, VRF uuid (from WatchVrf), creation wall time,
      and any badge AT BIRTH (capture its console text per Step 0 protocol).

## Step 1b - clock persistence pre-check (before anything scored)

WHY THIS STEP EXISTS - UNRETIRED RISK: THE MULTIPLIER MAY NOT SURVIVE OUR
RESIGN. SetSimRate sets the rate and immediately leaves the federation.
Supervisor reasoning that it SHOULD persist: it is a sim-interface control
message (DtIfSetTimeMultiplier, DtSetTimeMultiplierMessageType = 28), not owned
object state, so the backend adopts the clock rate as its own state with no
ownership or lease semantics. NOT VERIFIED - the competing possibility (VRF ties
sim control to a controlling federate and reverts on resign) is not excluded
offline. FAILURE MODE IF WRONG: the tool reports success and the clock silently
returns to 1x, so Step 4 would measure a 1x run labelled 20x.

This step runs entirely BEFORE the scored 1x baseline begins and entirely on the
CLOCKTEST throwaway, so no clock excursion ever falls inside the scored window.

- [ ] Task CLOCKTEST on a SHORT route (2-5 km) at 1x. Nothing else is tasked yet.
- [ ] Confirm CLOCKTEST is ACTUALLY MOVING from WatchVrf displacement - not from
      a GUI impression. If it is not moving, STOP: Step 1b cannot run, and a
      native entity failing to move at 1x is itself a major finding (record it,
      capture its console text per Step 0, and treat it as a P1-A falsifier
      signal before deciding whether the session continues).
- [ ] Note the BASELINE displacement-per-wall-second at 1x over a clean interval.
- [ ] Run SetSimRate to 20x (first invocation; fresh ledgered appNo). Note the
      wall time SetSimRate exits.
- [ ] Watch the WatchVrf displacement rate for ~1 MINUTE AFTER SetSimRate has
      EXITED - the whole point is the behavior after the tool has resigned, not
      while it is joined.
- [ ] Run SetSimRate back to 1 (second invocation; a DIFFERENT fresh ledgered
      appNo) and confirm the displacement rate drops back to the 1x baseline.
- [ ] DECISION RULE (apply before Step 2):
      - If displacement-per-wall-second jumps about 20x and STAYS there after
        SetSimRate exits -> the multiplier PERSISTS. Step 4 proceeds as written
        with the remote setTimeMultiplier.
      - If it reverts when SetSimRate exits -> the tool's design is FALSIFIED
        for this purpose. Fall back to the GUI Time Scale toolbar at 15x for
        Step 4 and record the deviation (this weakens but does not void P1-C,
        per the Step 4 fallback text).
      Either way, record the observed rates and times verbatim - this is the
      first direct evidence on the question and it belongs in ground truth.
- [ ] Confirm the multiplier is back at 1.0 and CLOCKTEST is stopped (or simply
      ignored for the rest of the session). ONLY THEN begin Step 2.

## Step 2 - native route tasks at 1x

- [ ] Confirm the time-multiplier reads 1.0 (Step 1b restored it) and that
      CLOCKTEST is stopped or being ignored, before the first scored task. No
      clock change occurs anywhere inside Step 2 or Step 3.
- [ ] Task each of the three objects with the GUI's native route-move task
      (record the exact task name and every parameter the dialog offers and
      what was left at default - this feeds the Phase 2.3 task vocabulary map).
- [ ] Routes: 2-3 waypoints each, legs 2-20 km, COA-shaped (use the COA-STP1
      axes where practical). BOTH THE PLATOON AND THE COMPANY ROUTES MUST
      EXCEED 20 KM CUMULATIVE so each crosses the 18.1-18.4 km stop band from
      its own start point (P1-B needs both controller classes across the
      band). The M1A2 entity route stays short (2-5 km) for an early
      completion data point.
- [ ] Task all three within a few minutes of each other so they run
      CONCURRENTLY - at 1x and ~30 km/h column pace a 20+ km route takes
      ~40-45 min of wall time, and the 1-hour budget only holds if that wait
      is shared. Use the wait to transcribe console captures (the clock
      persistence question is already settled by Step 1b - do NOT touch the
      multiplier here).
- [ ] Record per unit: task start time, every GUI status change, every badge
      (capture text immediately, panel-first), the completion indication and
      its time, and - for the company - where the TRAILING subordinates are
      when completion is indicated (leading-edge semantics check, P1-D).
- [ ] Let all three run to completion or clear failure before Step 4. Movement
      claims come from WatchVrf displacement only, adjudicated after the
      session; GUI impressions are recorded as impressions, not results.

## Step 3 - contingency branches (pre-registered, only if triggered)

- IF a mover stalls mid-route (no displacement for 3+ min at 1x): capture its
  console messages FIRST, note its cumulative distance (the 18.1-18.4 km band
  is the loaded question), then attach a Terrain Page-in Area to the stalled
  unit (documented remedy, ground truth 0.2 sec 6) and observe 5 min: resumes
  or not. Either outcome is MAK-question evidence. Do not delete or re-task
  the unit before the console capture.
- IF a mover runs away (passes its final vertex by >20% of route length, or
  leaves the AO): capture console text, let it run ~2 min for telemetry, then
  stop it with the GUI stop/clear-tasks action. No sim kill, no process kill.

## Step 4 - the 20x repeat (single variable = the clock)

- [ ] Same unit (the platoon), same route geometry, re-tasked identically;
      everything else untouched. CONTINGENCY: if the platoon did not complete
      its 1x route (P1-A/P1-B falsified on it), re-running the same geometry
      would just replay the 1x failure - use the M1A2 entity and ITS completed
      Step 2 route for the 20x repeat instead, and record the substitution.
- [ ] D1 RESOLVED (user ruled 2026-07-18): use the REMOTE setTimeMultiplier,
      NOT the GUI toolbar. This is the SAME mechanism every port run used, so
      Step 4 keeps single-variable discipline. Mechanism: a new tool
      tools/SetSimRate - additive, cloned from ResetVrf; VrfFacade/VrfBridge
      already exposed SetTimeMultiplier, so NO existing source was edited and
      the WatchVrf POS path is untouched. VERIFIED BY SUPERVISOR 2026-07-18:
      builds clean (0 warnings, 0 errors); argument validation smoke-tested -
      no args, non-numeric multiplier, zero multiplier, and missing appNo all
      fail loudly with exit 2. It has NO default appNumber by design.
      Fallback (unchanged): the GUI Time Scale toolbar, which caps at 15 by
      default (ground truth 0.2 sec 8) - if used, 15x replaces 20x and the
      deviation is recorded (weakens but does not void P1-C).
- [ ] The exact session command lines (verified exe path). The <FRESH_APPNO_n>
      tokens are PLACEHOLDERS - substitute real ledgered numbers, do not paste
      as-is:

      ```powershell
      $env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;$env:PATH"
      $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
      Push-Location C:\MAK\vrforces5.0.2\bin64
      # go to 20x - replace <FRESH_APPNO_1> from the Appendix B ledger
      & <repo>\tools\SetSimRate\bin\Release\net10.0\win-x64\SetSimRate.exe 20 <FRESH_APPNO_1>
      # back to real time - a DIFFERENT fresh appNo
      & <repo>\tools\SetSimRate\bin\Release\net10.0\win-x64\SetSimRate.exe 1 <FRESH_APPNO_2>
      Pop-Location
      ```

      TWO fresh ledgered appNos are required FOR STEP 4: each invocation is a
      full join/resign, so the 20x call and the return-to-1x call each consume
      one. SESSION TOTAL: at least FOUR SetSimRate invocations - Step 1b up +
      down, Step 4 up + down - each a separate join requiring its OWN fresh
      ledgered appNo, all four DISTINCT from each other and from WatchVrf's, and
      all entered in the OPUS_EXECUTION_PLAN.md Appendix B ledger BEFORE use.
- [ ] THERE IS NO READBACK. setTimeMultiplier has no getter on the remote
      controller (VRF_GROUND_TRUTH 0.3 sec 6a); the value is only readable from
      DtIfStatus, which the port does not subscribe to. The tool is therefore
      write-only and self-reports success purely from "the call did not throw".
      Confirm the rate change VISUALLY in the GUI, and quantitatively from the
      WatchVrf displacement rate - the movement oracle is the authoritative
      witness for the clock rate, not any API readback.
- [ ] SetSimRate joins as an ADDITIONAL federate while WatchVrf is joined.
      ResetVrf uses the same join/resign pattern but normally with nothing else
      observing. Whether a third federate joining/leaving mid-run perturbs the
      WatchVrf POS stream is UNTESTED, and Step 4's whole verdict rests on that
      stream. Watch for any POS discontinuity coincident with SetSimRate's join
      and resign, and note it.
- [ ] SetSimRate waits for BackendCount() > 0 (15 s cap) before issuing the
      message and REFUSES to send if no backend was discovered (exits 1).
      Rationale: backends are not known at the instant Start() returns, and
      issuing against zero discovered backends would be a silent no-op reported
      as success. The 15 s settle cap and the 3 s post-call flush are calibrated
      by analogy to ResetVrf, NOT measured for this message.
- [ ] Record as in Step 2; afterwards set the multiplier back to 1.0 (the
      second SetSimRate invocation above) and note the time.

## Step 5 - save and teardown

- [ ] File > Save As -> phase1_native_baseline.scnx (record the FULL saved
      path; this is the 0.5 scnx-diff reference input).
- [ ] Final Object Console Summary Panel capture; only then Acknowledge All.
- [ ] Teardown per RUNBOOK sec 0.5.9 (close the FRONT-END; in combined mode the
      back-end follows) and sec 4 (CLEAN STOP for any joined interface). NOT sec 7 -
      that section is about running the .NET port and contains no teardown procedure.
      LEAVE rtiAssistant / rtiexec / rtiForwarder RUNNING (RUNBOOK 0.5.2).
      Ledger every appNo used as USED, and update the single "*** NEXT FREE:" marker.

## Scoring (decided by WatchVrf displacement, post-session)

| Outcome | Meaning |
|---------|---------|
| All native units arrive at 1x, incl. the >20 km company route | Environment exonerated at 1x; Phase 2 gap analysis proceeds against a proven-good target |
| Native stop in the 18.1-18.4 km band | Environmental cause; MAK support question fires NOW with this repro + the Page-in Area result |
| Misbehavior ONLY at 20x (Step 4) | Multiplier implicated; port acceptance runs must move to 1x; warp mechanism hypothesis (ground truth 0.2 sec 8) gains support |
| Native company mute/short while platoon clean | Controller-split hypothesis strengthened; the platoon-vs-company probe (pre-registered, ground truth 0.0 item 2) is next |
| Native units misbehave broadly | STOP rebuilding; MAK question with repro becomes the critical path |

Open items before this runs (as of 2026-07-18):

- D1 (20x mechanism) is CLOSED - remote setTimeMultiplier via tools/SetSimRate
  (Step 4); GUI Time Scale at 15x remains the recorded-deviation fallback.
- 0.4 self-launch is CLOSED - GATE PASSED 2026-07-18 (PREREG_0_4_SELFLAUNCH.md
  sec 12). REVERSED LATER THE SAME DAY: the earlier "demoted; launch MANUALLY;
  LaunchVrf.ps1 is NOT used" instruction is RETIRED. scripts/LaunchVrf.ps1 IS the
  bring-up for this session and needs no human interaction - see the Preconditions
  block at the top of this script for the exact command and the never-kill-the-RTI-
  infrastructure rule.
- 0.6 console capture is FOLDED IN, not a separate session: if the extended
  WatchVrf build (POS + CON lines) is available at session time, use it and this
  session doubles as its live gate; otherwise the Step 0 GUI capture protocol
  is the only console channel and the script still runs unchanged.
- Remaining genuinely open: whether the multiplier survives SetSimRate's resign
  (untested - the Step 1b pre-check settles it live, on a throwaway unit and
  entirely outside the scored window), and whether a third
  federate joining mid-run perturbs the WatchVrf POS stream (untested).
