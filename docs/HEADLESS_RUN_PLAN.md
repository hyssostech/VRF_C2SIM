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
| 6 | Capture outbound reports | `tools/ListenReports` | runs; targets net6.0 |
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
- `tools/ListenReports` targets net6.0 while the rest target net10.0, and writes its
  capture beside its own binary rather than to a run directory. The runner needs it to
  write where it is told.

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
