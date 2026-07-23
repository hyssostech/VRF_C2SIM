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
