# HEADLESS RUN PLAN - the one-button C2SIM -> VR-Forces loop

Written 2026-07-18 evening, after the effort re-grounded on what it is actually
building. Plan of record for method: VRF_GROUNDWORK_PLAN.md, especially sec 1a
(THE HEADLESS MANDATE) and Phases 3-5. This document is the NEXT ACTION.

## 0. The goal, in one line

C2SIM document in -> units initialized, tasked and run in VR-Forces -> outcome
verified from telemetry. ONE command. Zero humans in the UI, zero terrain clicking,
zero GUI automation.

User's own words (2026-07-18): "I click a button, my C2SIM plan plays on its own."

## 1. What already exists - the chain is COMPLETE and BUILT

Every stage below is headless. Nothing in this table touches a GUI. All were built
and confirmed present 2026-07-18; the four C2SIM tools were rebuilt that evening
(Release, 0 errors).

| # | Stage | Tool | State |
|---|-------|------|-------|
| 1 | Bring VR-Forces up | `scripts/LaunchVrf.ps1` | VERIFIED, EXIT=0 READY |
| 2 | Run the interface | `src/VrfC2SimApp` | builds 0 errors |
| 3 | Push C2SIM init | `tools/PushInit <init.xml>` | builds; NO ARG GUARD (sec 4) |
| 4 | Push C2SIM order | `tools/PushOrder <order.xml>` | builds; NO ARG GUARD (sec 4) |
| 5 | Measure execution | `tools/WatchVrf <appNo> <secs> <sample>` | VERIFIED oracle |
| 6 | Capture outbound reports | `tools/ListenReports [secs] [outPath]` | net10.0; outPath added |
| 7 | Clean stop | `tools/StopIface` | works; ACTS WITH NO ARGS (sec 4) |
| 8 | Bring VR-Forces down | `scripts/StopVrf.ps1` | VERIFIED, EXIT=0 |

THE PIPELINE HAS ALREADY RUN END TO END. RUNBOOK sec 7 records 2026-07-10: HLA join
-> late-join QUERYINIT (49 units + 4 areas) -> order received and parsed over STOMP ->
taskee resolved -> CreateRoute + MoveAlongRoute (entity AND disaggregated aggregate) ->
unit MOVES -> task COMPLETES -> TASKCMPLT pushed back to C2SIM -> clean stop, no stale
federate. The question is NOT whether the loop can run. It is what QUALITY it produces:
known-bad type fidelity (generic fallbacks), runaways, and untrustworthy completions.

## 2. The deliverable: scripts/RunC2SimScenario.ps1

NOT YET WRITTEN. This is the next thing to build.

Contract:

    pwsh -File scripts\RunC2SimScenario.ps1 `
         -Init data\R9_Mojave_Initialization.xml `
         -Order data\R9_Mojave_UnitMove_Order.xml `
         -RunSecs 600

Sequences stages 1-8 unattended and leaves a timestamped run directory containing:
  - the WatchVrf trace (POS + CON lines) - the movement oracle, the scoring input
  - the reports captured by ListenReports (what the interface told C2SIM)
  - the VrfC2SimApp log
  - a run manifest: every appNo used, both clocks (local + UTC), tool versions, the
    exact init/order paths, and the exit code of every stage

Requirements learned the hard way - do not omit:
  - EVERY appNo comes from the single "*** NEXT FREE:" marker in
    OPUS_EXECUTION_PLAN.md Appendix B and is LEDGERED BEFORE the join. That includes
    the app's own `Vrf__ApplicationNumber`, which has historically been set by hand
    and is exactly as capable of causing a stale-federate hang as any other join.
  - WatchVrf STARTS BEFORE the init is pushed, so unit births are in the trace.
  - The oracle pre-check (RUNBOOK 0.5.7, CORRECTED criterion) runs before anything is
    scored: require a POS line with REAL lat/lon - not NaN, not the 90/-90 pole - and
    retry for up to ~3 min. reflected>0 is NOT sufficient and reflected=0 at 20 s is
    NOT a stop.
  - Teardown runs even on failure, and never force-kills a joined federate.
  - RTI infrastructure (rtiAssistant / rtiexec / rtiForwarder) is NEVER touched.

## 3. First run target and what it settles

`data/R9_Mojave_Initialization.xml` + `data/R9_Mojave_UnitMove_Order.xml`.
Mojave matches the loaded TropicTortoise terrain, and a single unit move either
produces displacement or it does not - a binary first result with no aggregation,
no formation, and no controller-class confound.

It settles, headlessly, questions the retired Phase 1 script planned to answer with a
human: does a C2SIM-driven unit actually ARRIVE, and does the completion the interface
reports correspond to real displacement. Scale up only after that is green.

## 4. Tooling defects that must be fixed BEFORE an unattended runner

These are hazards specifically because the runner is unattended.

- `tools/StopIface` ACTS WHEN RUN WITH NO ARGUMENTS. Verified 2026-07-18: invoked with
  no args to read its usage, it drove the live C2SIM server
  RUNNING -> INITIALIZED -> UNINITIALIZED. No damage (nothing was joined, and
  UNINITIALIZED is the correct pre-run state) but it is a live state change from what
  the operator intended as a read-only probe.
- `tools/PushInit` with no args throws a bare `ArgumentException` ("need init xml path");
  `tools/PushOrder` throws `IndexOutOfRangeException` - an unhandled stack trace rather
  than a usage message and a documented exit code.
- CONTRAST, and the pattern to copy: `tools/CreateOne` and `tools/SetSimRate` were built
  with NO default appNo and hard-exit 2 on bad arguments. That is the standard. Bring
  the C2SIM tools up to it: usage message, documented exit code, NO ACTION on bad args.
- RESOLVED 2026-07-19: `tools/ListenReports` now targets net10.0 like the rest, and takes
  an optional second argument `[outPath]` (file OR directory; missing parents created) so
  the runner controls where the capture lands. With NO outPath the historical behavior is
  unchanged: `reports-captured.log` beside the binary. Bad `[seconds]` now exits 2 with a
  usage message instead of throwing `FormatException`. Same TryParse+exit-2 hardening was
  applied to `tools/StompProbe`, and to `tools/WatchVrf`, whose TryParse calls previously
  DISCARDED the bool result and so silently fell back to defaults on a typo - dangerous in
  the movement oracle, because the trace would describe the wrong appNumber or cadence.
  Each tool carries a LOCAL `Usage()` static; consolidate into a shared helper later.

CONSOLE OUTPUT THAT TELLS A HUMAN TO LOOK AT THE GUI - fix before the runner ships,
because these strings execute inside an unattended run where nobody reads the console,
and because they are the misconception in machine-readable form:
- `tools/SetSimRate/Program.cs` (~:199-201 and ~:209-210): "VERIFY IN THE GUI: there is
  no getter on the remote controller ... Confirm the rate visually before trusting it."
  and "[FAIL] multiplier may or may not have been applied - VERIFY IN THE GUI."
  The API limitation is REAL (no timeMultiplier() accessor). The prescribed remedy is
  not. Replace with the headless check: compare WatchVrf displacement per WALL second
  across the change - an Nx multiplier shows as Nx displacement per real second.
- `tools/ResetVrf/Program.cs` (~:139): "[OK] resigned cleanly. Verify the VR-Forces GUI
  now shows an empty scenario." Replace with: re-run with --dry-run, or run WatchVrf -
  a cleared scenario reflects nothing beyond the baseline objects.
- `tools/CreateOne/Program.cs` (~:185): "[FAIL] no ObjectCreated reply within 20 s ...
  check the VR-Forces GUI and WatchVrf". Reorder so WatchVrf is the instrument and the
  GUI is at most a parenthetical.

## 4a. THE PASS CRITERION - WRITE IT BEFORE THE FIRST RUN

A clean-context cold reader flagged this as the single biggest hole in the handoff, and
it is: this project's documented failure mode is inventing an acceptance criterion after
seeing the data. DECIDE AND RECORD THE ARITHMETIC BEFORE RUNNING, in this file.

*** STATUS 2026-07-19: numbers PROPOSED by the supervisor below, AWAITING USER RULING.
    The first scored run is HELD until these are ruled on. Several are military-semantics
    calls (what counts as "arrived" for a formation), which is the user's standing role.
    Rule on them, amend them, or reject them - but they are fixed BEFORE the run, and any
    change made AFTER data exists must be recorded here as a dated amendment with the
    reason. That audit trail is the whole point of this section. ***

### 4a.0 The first target, measured (not estimated)

Read from data/R9_Mojave_UnitMove_Order.xml on 2026-07-19. IMPORTANT CORRECTION to sec 3,
which calls this "a single unit move ... no aggregation, no formation, and no
controller-class confound": the order contains THREE tasks against THREE taskees, all
TaskActionCode MOVE, each a two-waypoint leg. Verb confound: none (all MOVE). Echelon /
controller-class confound: PRESENT.

| Taskee | UUID prefix | Expected actor | Geodesic leg |
|--------|-------------|----------------|--------------|
| 1.BdeHQ      | 670cfdb2 | ENTITY (lone platform) | 577.8 m |
| 114.MechCoy  | 139aa71b | UNIT (company aggregate) | 556.0 m |
| 1222.MechPlt | 001aa71b | UNIT (platoon aggregate) | 577.8 m |

The ENTITY/UNIT column is EXPECTED, not verified - it follows the type-mapping analysis
(BDE-echelon falls to a lone M1A2 entity; COY/PLT resolve to aggregates) and RUNBOOK
sec 7, where 1.BdeHQ was tasked as an entity on 2026-07-10. Run 1 must CONFIRM the actor
class per taskee from the trace before applying the entity-vs-unit tolerance below. If a
taskee turns out to be the other class, score it under the other rule and note it.

INIT FILE: use data/R9_Mojave_Lean_Initialization.xml (6 units, contains exactly these
three taskees) rather than R9_Mojave_Initialization.xml (158 unit/actor references, full
brigade org tree). Both contain all three taskee UUIDs - verified - so both work; the
lean file removes 152 irrelevant units from the trace. Sec 3 names the full file; this
supersedes it for run 1.

SCALE NOTE, load-bearing for every threshold below: these legs are ~556-578 m. Every
known failure phenomenon is 18-100 km (stall band 18.1-18.4 km; warp threshold 200 km/h;
persistent runaways 41-83 km out). The expected signal is therefore two orders of
magnitude SMALLER than the known noise. That is favorable - a warp is unmistakable
against a 578 m leg - but it means the "did it move" floor must be set low, and it means
run 1 CANNOT test the stall band at all.

### 4a.1 ARRIVAL

Two different rules, because the vendor semantics differ (ground truth 0.0 item 5).

- ENTITY taskee: ARRIVED if the entity's final settled position is within **50 m** of the
  task's final waypoint. Rationale: ~9% of a 578 m leg - comfortably above terrain-clamp
  and sampling noise, comfortably below the leg length.
- UNIT taskee: ARRIVED if the unit CENTER's final settled position is within **250 m** of
  the final waypoint. Rationale: VR-Forces reports a unit route task finished when the
  formation LEADING EDGE crosses the last vertex, verbatim from
  [Tasks\MovementTasks\RouteMoveAlong.htm], so the center legitimately lags the waypoint
  by up to the formation depth at completion. 250 m is a guess at company formation depth
  and is THE WEAKEST NUMBER IN THIS SECTION - see 4a.6.
- "Settled" = position changed by less than 10 m across the last 3 consecutive samples.
- The leading-member distance to the final waypoint is RECORDED for every unit regardless,
  so run 1 yields the data to calibrate the 250 m properly instead of guessing twice.

### 4a.2 MOVED AT ALL

- MOVED if net straight-line displacement from first to last settled position is
  **>= 25 m** (~4.3% of the shortest leg), AND the displacement is sustained across at
  least **3 consecutive samples** rather than appearing in a single sample.
- The two-part test is what separates real motion from a one-sample spike. A single
  sample that moves 25 m and returns is not motion.
- Sample interval **2 s** (sampleSecs=2), per sec 2.
- NOT MOVED with a task dispatched is the "mute unit" defect (census: 11 tasked,
  7 dispatched, 3 moved). It is a recorded RESULT, not a run failure.

### 4a.3 RUNAWAY

- RUNAWAY if net displacement exceeds **5x the ordered leg length** (i.e. > ~2.9 km for
  these legs) OR the object leaves a **5 km radius** around its own birth position.
- Both are enormous relative to a 578 m leg and tiny relative to the observed 41-83 km
  persistent runaways, so the classification is unambiguous at this scale. The factor
  will need revisiting for long-leg scenarios; it is not a universal constant.
- A runaway is a FAILED task regardless of what any TASKCMPLT says.

### 4a.4 DR ARTIFACT vs REAL MOTION

The oracle reads dead-reckoned positions, so some apparent motion is observation-layer
extrapolation, not backend motion (ground truth 0.0 item 6; census sec at :351-388).

- A sample is a CANDIDATE ARTIFACT if the implied step speed is **>= 200 km/h**
  (census threshold, sim-time corrected so it is multiplier-independent).
- It is classified TRANSIENT (reject from displacement totals, but LOG it) if the object
  returns to within **100 m of its pre-jump track within 2 samples**.
- It is classified PERSISTENT (count it - this is the real runaway class) if it does not
  return. Persistent displaced end-states are NOT explainable as DR overshoot.
- Corroborating signal, recorded not decisive: altitude leaving the terrain surface
  (observed 3417-6356 m spikes on transients; -1306/-1681 m underground on persistents).
- IF the WatchVrf raw-vs-DR logging lands (sec 4b), this whole heuristic is REPLACED by
  the direct comparison of lastSetLocation() against the extrapolated read, which settles
  the question by measurement instead of by threshold. Prefer that if available.

### 4a.5 COMPLETION TRUST

A TASKCMPLT is scored ONLY against displacement, never on its own. Four outcomes, all
recorded per task:

- ARRIVED + TASKCMPLT      -> TRUE COMPLETION (the only good outcome)
- ARRIVED + no TASKCMPLT   -> MISSING COMPLETION (known defect: ESCRT/patrol never fire)
- not ARRIVED + TASKCMPLT  -> FALSE COMPLETION (known defect, and the dangerous one -
                              it is what makes a task chain advance on a lie)
- not ARRIVED + no TASKCMPLT -> HONEST FAILURE

### 4a.6 What run 1 is FOR, and what "pass" means

Run 1 is a MEASUREMENT, not a product acceptance test. Conflating the two is how an
acceptance criterion gets retro-fitted to the data. Two separate gates:

- RUN VALIDITY (does the run count at all?): VR-Forces launched, the interface joined,
  the init was accepted by the server, the order was received and parsed, the oracle
  pre-check passed (a POS line with real non-NaN non-pole lat/lon, retried up to ~3 min),
  the trace covers the whole run, and teardown left no stale federate. If any of these
  fail, there is NO SCORE - fix and re-run. A failed run is not a failed product.
- PER-TASK SCORE (what did the product actually do?): each of the three tasks gets one of
  the 4a.5 outcomes plus its 4a.1/4a.2/4a.3 measurements. THERE IS NO AGGREGATE
  PASS/FAIL THRESHOLD FOR RUN 1 and none should be invented. The deliverable is the
  three-row table and the baseline it establishes.

The number most likely to be wrong here is the 250 m unit-arrival tolerance, because
formation depth for a company may be comparable to the 556 m leg itself - in which case
a unit could legitimately complete its route with its center barely past the start. If
run 1 shows leading-member-arrived but center-displacement under ~100 m, that is the
signal that the tolerance, not the interface, is what needs fixing. Recording the
leading-member distance (4a.1) is what makes that diagnosable on the first run instead
of the second.

## 5. What is explicitly NOT next

- NOT GUI automation of any kind. The Create/Task UIA probe is ABANDONED (mandate
  sec 1a). VR-Forces' main window does expose a UIA tree - that fact is recorded in
  RUNBOOK 0.5.9 solely because StopVrf.ps1 needs it to answer the QUIT dialog, which is
  simulator lifecycle, not tasking.
- NOT the Phase 1 human-at-the-GUI session (PHASE1_SESSION_SCRIPT.md is superseded in
  method; its banner explains what survives).
- NOT rebuilding the creation layer yet (Phase 3). Get a measured baseline of what the
  CURRENT interface produces first, or there is nothing to score the rebuild against.

## 6. Open questions carried in (unchanged by the re-grounding)

- The 18.1-18.4 km stall band: real and terminal at 1x in the archived census, with the
  ground-truth item-7 qualifier (the C++ oracle applied a time multiplier even when its
  own disable flag claimed otherwise).
- Runaway/warp: decomposed into transient DR artifacts vs persistent displaced
  end-states (ground truth 0.0 item 6). The persistent class is the real one.
- Controller-class split (LF lead-follow vs HU move-along), echelon-confounded.
- Whether the 20x multiplier is itself implicated - and note the port has NO handler for
  the SetSimulationRealtimeMultiple system command at all.
- Completions cannot be trusted in either direction; Phase 4's truthful-arrival gate
  (no TASKCMPLT without displacement-verified arrival) is what makes runs self-scoring.
