# PHASE 1 SESSION SCRIPT - native reference baseline (TropicTortoise)

Status: DRAFT (supervisor-authored 2026-07-16) - for user review before the next
live session. Groundwork plan Phase 1. Roles: user drives the GUI; supervisor
records, timestamps, and gates. Budget ~1 hour of live time. Evidence rules per
SUPERVISED_RECOVERY_PLAN.md (WatchVrf displacement is the only movement oracle).

Everything below uses ONLY the VR-Forces GUI for creation and tasking. The one
deliberate exception is the 20x clock set in Step 4 (decision point D1 below).

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

- [ ] rtiexec running per RUNBOOK sec 0; VR-Forces launched via vrfLauncher.exe
      by the user (or via scripts/LaunchVrf.ps1 ONLY if the 0.4 live gate has
      passed twice and the user approves). Raw vrfSim CLI remains forbidden.
- [ ] TropicTortoise (Mojave) scenario loaded; confirm scenario parameters are
      the defaults: frame-mode variable-frame, frame-time 0.1,
      time-multiplier 1.0 (ground truth 0.2 sec 8).
- [ ] Fresh appNo for WatchVrf from OPUS_EXECUTION_PLAN.md Appendix B (next
      free at write time: 3455 - RE-CHECK the ledger at session time), entered
      in the ledger BEFORE the join.
- [ ] Get-Date logged at session start (tool logs stamp UTC; machine runs
      local - record both once, reconcile all later timestamps against this).
- [ ] Settings > Display > Entity Display Settings > "Show Object Console
      Warning Icon" = ON (it is the default; verify anyway).
- [ ] WatchVrf running and logging BEFORE any object is created (births must be
      in the trace). If the 0.6 console-capture build has passed its live gate,
      use the extended build (POS + CON lines); otherwise GUI capture per
      Step 0 protocol is the only console channel - follow it strictly.
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
- [ ] Placement: dispersed (>= 500 m apart) in the COA-STP1 AO start area (the
      same area the CPP-ALT-1 force used).
- [ ] Record per object: GUI name, VRF uuid (from WatchVrf), creation wall time,
      and any badge AT BIRTH (capture its console text per Step 0 protocol).

## Step 2 - native route tasks at 1x

- [ ] Confirm time-multiplier is 1.0 before the first task.
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
      is shared. Use the wait to transcribe console captures and settle D1.
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
- [ ] D1 DECISION POINT (user adjudicates before the session): how to set 20x.
      Preferred: remote setTimeMultiplier(20) - the SAME mechanism every port
      run used (single-variable discipline); needs the small remote call
      available at session time (supervisor confirms which tool exposes it
      before the session). Fallback: the GUI Time Scale toolbar, which caps at
      15 by default (ground truth 0.2 sec 8) - if used, 15x replaces 20x and
      the deviation is recorded (weakens but does not void P1-C).
- [ ] Record as in Step 2; afterwards set the multiplier back to 1.0 and note
      the time.

## Step 5 - save and teardown

- [ ] File > Save As -> phase1_native_baseline.scnx (record the FULL saved
      path; this is the 0.5 scnx-diff reference input).
- [ ] Final Object Console Summary Panel capture; only then Acknowledge All.
- [ ] Teardown per RUNBOOK sec 7. Ledger every appNo used as USED.

## Scoring (decided by WatchVrf displacement, post-session)

| Outcome | Meaning |
|---------|---------|
| All native units arrive at 1x, incl. the >20 km company route | Environment exonerated at 1x; Phase 2 gap analysis proceeds against a proven-good target |
| Native stop in the 18.1-18.4 km band | Environmental cause; MAK support question fires NOW with this repro + the Page-in Area result |
| Misbehavior ONLY at 20x (Step 4) | Multiplier implicated; port acceptance runs must move to 1x; warp mechanism hypothesis (ground truth 0.2 sec 8) gains support |
| Native company mute/short while platoon clean | Controller-split hypothesis strengthened; the platoon-vs-company probe (pre-registered, ground truth 0.0 item 2) is next |
| Native units misbehave broadly | STOP rebuilding; MAK question with repro becomes the critical path |

Open items before this runs: D1 (20x mechanism), and whether the 0.6 console
tool and 0.4 self-launch gate are available in time (both optional for this
session; the script works without them).
