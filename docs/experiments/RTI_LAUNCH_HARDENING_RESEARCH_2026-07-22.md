# RTI / VR-Forces launch-hardening research (2026-07-22)

External research triggered by TWO consecutive VOID live runs of the type-mapping
confirming run (same night): RUN 1 (warm RTI) - vrfSim FATAL CRASH after 3x CreateRoute,
before MoveAlongRoute; RUN 2 (fresh boot) - vrfSim NEVER JOINED, federation-create lost a
TCP race. Neither reached tasking, so the type-mapping fix is still UNTESTED end to end.
Per the 2-strike rule (two failures against third-party RTI/VR-Forces framework behavior),
research was done BEFORE any third live attempt. ASCII only. URLs verbatim from the sweep.

Full evidence: docs/experiments/TYPEFIX_CONFIRMING_2026-07-22/ (RUN 1),
docs/experiments/TYPEFIX_CONFIRMING_RUN2_2026-07-22/ (RUN 2).

## Scope honesty
- FAILURE 1 (RUN 2 startup race): WELL-SUPPORTED by HLA/MAK primary sources.
- FAILURE 2 (RUN 1 vrfSim crash): essentially NO public evidence - MAK's bug tracker is
  private and the classdoc FTP host (ftp.mak.com) was unreachable. This one must go to MAK
  support with the dump; the web will not answer it.

## FAILURE 1 - "RTIinternalError TCP connection has been broken" at createFederationExecution

Back-end log (RUN 2): "Could not create Federation Execution CWIX-2024: RTI exception:
RTIinternalError TCP connection has been broken. Could not create a valid network
connection. No FOM specified. Stopping run of back-end." (supervisor-verified in
bin64\vrfSim.log; the trailing "No FOM specified" is a CASCADE - RUN 1 loaded the identical
FOM fine on a warm RTI - not a root cause.)

KEY DISTINCTION (load-bearing): "connection has been BROKEN" != "connection REFUSED".
Refused = nothing listening (RTI process not started). BROKEN = a TCP connection was
ESTABLISHED then torn down mid-handshake = listener up but the service behind it not ready
(Forwarder accepted the socket but its uplink to rtiexec was not wired, or rtiexec was still
initializing the FedExec). CONSEQUENCE: a readiness gate that only checks "is the port open"
is INSUFFICIENT - the port can be open before the RTI can service a create/join.

Documented facts:
- FedExec-readiness window: "When createFederationExecution() completes, there is a brief
  period before the FedExec is ready to accept joining federates... an immediate call to
  joinFederationExecution() will probably fail."
  http://www.cs.cmu.edu/afs/cs/academic/class/15413-s99/www/hla/doc/rti_RTIAmb/RTIAmb_1.html
  https://www.cs.cmu.edu/afs/cs/academic/class/15413-s99/www/hla/doc/rti_FAQ/HLA_FAQ.html
- MAK RTI listen ports (readiness targets): rtiexec TCP 4000; RTI Forwarder TCP 5000.
  https://github.com/hlacontainers/vtmak-rtiexec/blob/main/README.md
- RTI Assistant can BLOCK the connection path: "If the RTI Assistant is used and there are
  no RTI licenses configured for the RTI Executive, a license error is displayed by the RTI
  Assistant, blocking the Forwarder from starting." (fresh boot raised our "Choose RTI
  Connection" dialog; a just-answered dialog can leave the Forwarder/connection half-wired
  at the instant the back-end connects). Same vtmak-rtiexec README.
- RTI Forwarder is a WAN device, NOT needed for single-machine/loopback: it "listen[s] for
  local UDP multicast packets and send[s] them via TCP across the WAN"; "unnecessary for
  single-LAN deployments." https://www.mak.com/mak-one/tools/mak-rti/capabilities
- RTI_ASSISTANT_DISABLE env var bypasses the Assistant GUI requirement. vtmak-rtiexec README.
- MAK "auto-recover from broken TCP" does NOT rescue this - it reconnects an ALREADY-
  ESTABLISHED federate mid-run, not a cold create that never fully connected.
  https://www.mak.com/mak-one/tools/mak-rti/capabilities

THIN: the exact string tied to createFederationExecution appears in no public MAK doc; our
diagnosis rests on the general principle + our own process-start-time evidence (rtiexec
60672 started 20:29:15, forwarder 61696 20:29:18, at/after the back-end's federation-create
attempt). Root cause between "Assistant/forwarder not wired" vs "rtiexec FedExec still
initializing" is NOT pinned - but BOTH are cured by the same fix.

## FAILURE 2 - vrfSim crash after CreateRoute, before MoveAlongRoute (warm RTI)

NO public evidence of this specific crash. Two competing hypotheses that make OPPOSITE
predictions about reproducibility after a reboot (the cheap discriminator):
1. Application/API fault in vrfSim's route-created callback / aggregate-tasking path
   (deterministic; reproduces on warm RTI regardless of runtime state). Favored by the clean
   crash-at-a-specific-API-sequence + the minidump.
2. Mid-session MSVC runtime servicing (VS 2026 18.8 updater ran minutes before this vrfSim
   launched) left this process with skewed runtime DLLs (non-deterministic; will NOT
   reproduce after reboot + VC++ repair). Note: RUN 2's vrfSim launched ~20:28, AFTER the
   updater settled (~19:45), and failed on a DIFFERENT axis (RTI race) - so the MSVC angle is
   specific to RUN 1's timing, not a general cause.
General Windows support: VC++ redistributable servicing can leave mismatched
vcruntime140/msvcp140 DLLs; a process launched during/after servicing can pick up a
half-updated runtime. Repair = reinstall x64+x86 VC++ redistributables.
https://learn.microsoft.com/en-us/answers/questions/5855127/
https://www.thewindowsclub.com/microsoft-visual-c-redistributable-rolled-back-or-corrupted

MAK support path (Failure 2 is MAK-only): info@mak.com / https://www.mak.com/support.
Assemble: minidump (bin64\vrfSim5.0.2-MSVC++15.0_64-249613-36676.dmp.dmp), vrfSim/vrfGui
logs, rid.mtl / RTI RID, exact versions (VR-Forces 5.0.2, MAK RTI 4.6.1, RPR FOM v2.0,
HLA 1516e), the scenario, and the precise remote-control call sequence before the crash.

## ACTIONABLE launch-procedure changes (evidence-supported)

1. REAL readiness GATE before launching the back-end - prove the RTI can SERVICE an
   operation, not just that a port is open. Poll TCP 4000 (rtiexec) [+5000 forwarder if
   used] for accept, THEN either (a) retry createFederationExecution with backoff (the
   documented create->join race idiom, applied to the cold create), or (b) a throwaway probe
   create/join that must succeed before the back-end launches. Bare port-open is explicitly
   insufficient ("broken" != "refused").

2. PREFER a WARM/RESIDENT rtiexec; eliminate the cold-boot federate path. RUN 1 joined
   cleanly on a warm RTI; RUN 2 failed on cold boot. Bring rtiexec (+ FedExec via a keep-
   alive probe federate if needed) fully up and CONFIRMED-READY as a distinct phase, then
   launch VR-Forces. On loopback: DROP the RTI Forwarder (WAN-only) and auto-dismiss/disable
   the RTI Assistant (RTI_ASSISTANT_DISABLE, or persist the connection so the dialog never
   appears) so no popup can leave the path half-wired. Aligns with the standing rule that
   rtiexec is never killed between runs.

3. For FAILURE 2, run the REBOOT + VC++ REPAIR DISCRIMINATOR before escalating: reboot,
   repair both x64/x86 VC++ 2015-2022 redistributables, replay the exact
   3x-CreateRoute-then-MoveAlongRoute sequence against a warm RTI. Still crashes => app/API
   fault => MAK case with the dump. Does NOT reproduce => mid-session servicing was the
   culprit => procedural fix (never launch vrfSim during/right after toolchain servicing).

RESIDUAL UNCERTAINTY to flag: RUN 2's exact broken-connection root cause is not pinned by
public evidence, but the same fix (warm resident rtiexec + real readiness gate + no
Assistant popup + no loopback forwarder) cures both candidates, so we can act without
resolving which.
