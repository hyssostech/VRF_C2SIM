# RTI launch-hardening design (next-session implementation spec)

Status: DESIGN, not implemented. Follows the user decision (2026-07-22): after two VOID
live runs, STOP live runs and HARDEN the launch procedure before re-testing the type fix.
Evidence base: docs/experiments/RTI_LAUNCH_HARDENING_RESEARCH_2026-07-22.md (URL-cited).
Implementation + validation REQUIRES a live RTI (the readiness gate is live behavior), so
it is next-session work, not tonight's. ASCII only.

## Problem (both VOID runs, same root class: environment/procedure, not the fix)
- RUN 2 (fresh boot): back-end federation-create lost a TCP race - "connection has been
  BROKEN" = RTI listener up but not yet able to service create/join. A port-open check is
  insufficient.
- RUN 1 (warm): vrfSim crashed at tasking (separate; MAK-support item + reboot/VC++
  discriminator). NOT addressed by launch hardening, but a warm resident RTI is the posture
  that let RUN 1 reach tasking at all.

## Design goals
1. Never launch the VR-Forces back-end against an RTI that cannot yet service a create/join.
2. Prefer a WARM, RESIDENT, CONFIRMED-READY rtiexec; make cold-boot-race impossible.
3. Remove the loopback-unnecessary moving parts that can leave the connection half-wired.

## Changes (verify each against the CURRENT scripts/LaunchVrf.ps1 + RunC2SimScenario.ps1
## before coding - line numbers not pinned here on purpose)

### C1. Real RTI readiness GATE before the back-end launches
Insert a gate BETWEEN "RTI processes exist" and "launch vrfSim back-end". The gate must
prove the RTI can SERVICE an operation, not just that a port is open:
- Necessary but insufficient precheck: TCP connect-accept on rtiexec 4000 (and forwarder
  5000 if the forwarder is retained - but see C3).
- Sufficient gate (pick one, prefer (a)):
  (a) THROWAWAY PROBE FEDERATE: a tiny join/resign against CWIX-2024 that must SUCCEED
      (join returns, then clean resign) before the back-end launches. Reuse the CreateOne/
      WatchVrf join path (they already join CWIX-2024). This is the strongest gate - it
      exercises the exact create/join the back-end will do. Ledger its appNo like any join.
  (b) RETRY-ON-CREATE: wrap the back-end's federation-create so a "TCP connection has been
      broken" / RTIinternalError at create RETRIES with backoff (e.g. 5 tries x 2 s). This
      needs the back-end/launcher to expose the create result - may be harder than (a).
- On gate failure after its cap: FAIL the launch loudly (do not proceed to PushInit - RUN 2
  proceeded to push init/order against a dead back-end and produced a confusing VOID).

### C2. Warm-resident rtiexec as a distinct, confirmed phase
- Treat rtiexec (+ a settled FedExec) as INFRASTRUCTURE brought up ONCE and kept resident
  across runs (already the standing rule: never kill rtiexec/rtiForwarder/rtiAssistant).
- Add an explicit "RTI READY" phase to the launcher: bring RTI up (or detect it resident),
  run the C1 gate, and only then enter the VR-Forces launch. The gate makes "resident but
  wedged" (teardown-survivor class) detectable BEFORE spending a run - this also subsumes
  the existing oracle pre-check intent, moved EARLIER (before back-end launch, not after).
- If RTI is being booted fresh in this phase, the C1 probe absorbs the cold-create window
  so the back-end never races it.

### C3. Drop the loopback-unnecessary Forwarder; suppress the Assistant dialog
- The RTI Forwarder is a WAN device, unnecessary for single-machine/loopback (research). It
  is ALSO the component whose half-wired state is the leading suspect for RUN 2's broken
  connection. EVALUATE running rtiexec-only on loopback (confirm our FOM/transport does not
  require it - CWIX-2024 uses reliable transport; verify the forwarder is not load-bearing
  for that first). LOW-CONFIDENCE / verify before removing - do not drop it blind.
- Suppress the "Choose RTI Connection" dialog deterministically: either RTI_ASSISTANT_DISABLE
  (headless) or ensure the connection is persisted so the dialog never appears. Fresh boots
  raised it this session (auto-connect did NOT persist across the RUN 2 fresh boot), and a
  just-answered dialog is a suspected contributor to the half-wired connection. The existing
  DPI-click answerer is a fallback, not the primary mechanism.

## Validation (needs live RTI - the whole point)
- Cold path: kill RTI, run the hardened launcher; the C1 gate must either wait-out or
  retry-through the cold-create window and the back-end must join cleanly (reproduce the
  RUN 2 condition and show it is now handled).
- Warm path: with RTI resident, launcher detects ready via C1 and launches fast.
- Negative test: point the gate at a deliberately-down RTI; it must FAIL the launch, not
  proceed to PushInit.

## Explicitly OUT of scope for this hardening
- RUN 1's vrfSim crash. Separate track: reboot + repair x64/x86 VC++ redistributables +
  replay the exact 3x-CreateRoute-then-MoveAlongRoute sequence on a warm RTI. Reproduces =>
  MAK support case (dump already preserved). Does not => mid-session MSVC servicing was the
  culprit, procedural fix only (never launch vrfSim during/right after toolchain servicing).
- The type-mapping fix itself (committed, offline-green; UNTESTED through to tasking - the
  hardened run is what finally tests it, per PREREG_TYPEFIX_CONFIRMING_RUN.md RUN >= 3).

## ADJUDICATION ADDENDUM (2026-07-23): terrain verified against source

Supervisor adjudicated the above design against the CURRENT scripts + native source before
scoping the implementation. Findings that AMEND the design (each verified at the cited
file:line, not taken from prose):

- A1 (AMENDS C3 - LANDMINE). RTI_ASSISTANT_DISABLE is FORBIDDEN, not an option. RUNBOOK.md
  sec 0.5.5 (RUNBOOK.md:165-179) and VRF_GROUNDWORK_PLAN.md:501: with the Assistant disabled,
  federates JOIN but NEVER DISCOVER each other and the movement oracle goes silently blind
  (reflected=0), because rid.mtl sets RTI_configureConnectionWithRid 0, making the Assistant's
  stored connection the ONLY source of connection values. Verified live at appNo 3475
  (SESSION_2026-07-18_SELFLAUNCH.md). CONSEQUENCE: C3's "suppress the dialog" goal is served
  ONLY by the persisted-connection half (a resident, already-answered rtiAssistant with
  "Always try to use this connection" checked - the standing one-time-per-machine rule). The
  Assistant is LOAD-BEARING; keep it, never disable it. Drop RTI_ASSISTANT_DISABLE from scope.

- A2 (AMENDS C1). The C1 probe MUST be WatchVrf, NOT CreateOne. CreateOne refuses to act
  without a discovered back-end (CreateOne/Program.cs:160-167 -> exit 1) and the C1 gate runs
  BEFORE the back-end launches, so CreateOne can never be the pre-launch gate. WatchVrf joins
  as a read-only observer, needs no back-end, self-resigns on its own timer
  (WatchVrf/WatchRunner.cs:212-214), exit 0 clean / exit 1 operational-failure (join threw)
  / exit 2 bad-args (WatchRunner.cs:119-123,217-225). The design named the two tools as
  interchangeable; they are not for this purpose.

- A3 (CONFIRMS the C1 mechanism, source-grounded). WatchVrf's join path DOES exercise the
  exact createFederationExecution operation RUN 2 lost the TCP race on. VrfBridge -> native
  VrfFacade::Start builds a synthetic argv with --execName <federation> and constructs
  new DtExerciseConn(*appInit) (VrfFacade.cpp:299,319-325); DtExerciseConn is MAK's
  create-or-join connection object. So a WatchVrf probe run FIRST against a not-yet-created
  CWIX-2024 performs the create-then-join - the precise thing to de-risk. CAVEAT: on the
  probe's resign, VrfFacade::Stop does delete p_->exConn (VrfFacade.cpp:369-377), whose MAK
  destructor resigns and (as last member) will attempt to DESTROY the FedExec. So a lone
  probe likely does NOT leave a warm FedExec - the back-end still does its own create. The
  gate therefore proves "the RTI can service a create/join right now"; it does NOT by itself
  guarantee the back-end's subsequent create. On a WARM RESIDENT rtiexec (no churn) that is
  sufficient (RUN 1 joined cleanly on a warm stack); the keep-alive-FedExec federate (C2's
  parenthetical) and/or retry-on-create backoff are the FALLBACKS if live validation shows
  the back-end still races the create on a warm stack. OPEN, resolve at implementation:
  MAK DtExerciseConn destructor's exact destroy behavior (SDK, not our source).

- A4 (AMENDS C1/C2 - insertion point). There is NO discrete "vrfSim back-end launch" step to
  insert before. The scripts launch vrfLauncher.exe in COMBINED mode only on the one-button
  path (LaunchVrf.ps1:349,402); back-end + GUI are its descendants, spawned together (a -B
  BackendOnly mode exists at :350 but the runner does not use it). The gate belongs in the
  RUNNER, not LaunchVrf, because the probe needs a ledgered appNumber only the runner
  allocates (LaunchVrf never reads the ledger). Precise slot: after env/PATH/license setup
  (RunC2SimScenario.ps1:1418-1427), before the Stage 3 header (:1432). "RTI processes exist"
  = Stage 1 inventory (:1147-1155); "launch back-end" = Stage 3 Invoke-External LaunchVrf
  (:1448).

- A5 (LEVERAGE - the probe already exists, mis-placed). Stage 4 (RunC2SimScenario.ps1:
  1476-1518) is ALREADY a WatchVrf join/resign whose stated job is "prove the oracle can JOIN
  and DISCOVER" - but it runs AFTER Stage 3 (post-launch, advisory). C1 is essentially Stage 4
  HOISTED ahead of Stage 3 and made FATAL. The invocation pattern (:1482-1486) is directly
  liftable. DECISION for implementation: ADD C1 pre-launch (fatal gate) and KEEP Stage 4
  post-launch (advisory, tests post-back-end discovery) - they test different moments; net
  +1 ledgered appNo per run (7 allocated, marker 3597 -> 3604 per live run).

- A6 (out of core scope). C3's forwarder-drop is LOW-CONFIDENCE in the design itself
  (rtiForwarder half-wired state was a SUSPECT, not a proven cause). No script currently
  starts/stops/manages the forwarder (it is assumed resident; LaunchVrf.ps1:271-278 only
  detects+reports, never gates). Dropping it is a speculative change that could break
  discovery; the gate + warm-resident rtiexec address RUN 2 without touching it. DEFER the
  forwarder-drop out of STEP 1 core; revisit only if the gate alone proves insufficient.

- A7 (appNo discipline for a retry loop). If the C1 gate RETRIES the probe to wait out a
  cold-create window, each WatchVrf re-join needs a FRESH ledgered appNo (never reuse -
  OPUS_EXECUTION_PLAN.md:926-927); WatchVrf does not retry internally today. Two options at
  implementation: (a) external retry, ledger one appNo per attempt (burns a few integers on a
  cold launch - acceptable); or (b) add internal create/join retry-with-backoff to WatchVrf
  (or a tiny dedicated probe tool) on a single appNo (cleaner ledger, small code change).
  Prefer (b) if the tool change is cheap; either is fine.

STATUS after adjudication: design is sound; C1=WatchVrf pre-launch fatal readiness gate in
the runner + keep warm resident answered rtiAssistant/rtiexec (persisted connection); C3
RTI_ASSISTANT_DISABLE and forwarder-drop are OUT. Implementation is OFFLINE; VALIDATION needs
a live RTI (gated, requires a user go).
