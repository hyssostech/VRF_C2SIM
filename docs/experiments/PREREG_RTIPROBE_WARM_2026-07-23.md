# PREREG: RtiProbe warm probe vs the resident RTI (registered 2026-07-23)

STATUS: REGISTERED, not yet run. ASCII only. This is a READINESS/SERVICEABILITY probe, NOT a
movement test - no POS/RPT channels, no movement claim is made or implied.

## 0. What this run tests (ONE variable)

Run the STEP 1 gate tool `tools/RtiProbe/bin/Release/net10.0/win-x64/RtiProbe.exe` STANDALONE,
one time, against the CURRENT resident RTI trio, and read its exit code. Nothing else changes:
no reboot, no back-end launch, no VrfC2SimApp, no C2SIM init/order, no runner. Just the probe
vs the resident stack.

- appNo: 3597 (ledgered to 3598 in OPUS_EXECUTION_PLAN.md Appendix B BEFORE the join).
- Command: `RtiProbe.exe 3597 CWIX-2024 5 2 3` (defaults: 5 attempts, 2 s settle, 3 s backoff).
- Env (mirrors the runner exactly): cwd = C:\MAK\vrforces5.0.2\bin64;
  PATH prefix = C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;
  MAKLMGRD_LICENSE_FILE = the resident Machine-scope .lic (already set). A wrong PATH would
  fail the join spuriously and fake a "wedged" verdict - the env is replicated deliberately.
- Resident stack under test (inventory taken immediately before): rtiAssistant 40956 (9 thr),
  rtiexec 60672 (1 thr), rtiForwarder 61696 (1 thr), resident since 2026-07-22 20:28; no
  vrf back-end present. This is the teardown-survivor / wedge-prone class.

## 1. Purpose (two things at once)

(a) FIRST live exercise of the STEP 1 gate instrument. RtiProbe's non-DryRun create-or-join
    path has never run against a live RTI; this validates it before it ever gates a real launch.
(b) Ground-truth on the resident stack: is it actually serviceable, or wedged? The 1-thread
    rtiexec/rtiForwarder raised suspicion at session start.

## 2. Predictions (both outcomes are informative - this is a discriminator, not a pass/fail)

- EXIT 0 (a create-or-join of CWIX-2024 SUCCEEDED and RtiProbe resigned cleanly): the resident
  stack IS serviceable despite the 1-thread appearance; the gate PASS path is validated live.
- EXIT 1 (every attempt failed to create/join, OR it joined but could not resign cleanly): the
  resident stack is UNSERVICEABLE/wedged; the gate FAIL-loud path is validated live (the
  instrument correctly refuses an unserviceable RTI). Consistent with the wedge suspicion.
- EXIT 2 (usage/args): a RtiProbe DEFECT - the appNo is valid, so a 2 would be a tool bug, NOT
  an RTI verdict. Would fail this probe as INVALID and send me to fix the tool.

REGISTERED LEAN (so a surprise is visible, not rationalized after the fact): given the
1-thread rtiexec/rtiForwarder and the teardown-survivor residency, I lean slightly toward
EXIT 1 (wedged). Genuinely uncertain; either result is accepted as informative.

## 3. Falsifier of "the gate instrument is reliable"

If RtiProbe HANGS past a hard timeout (no exit code returned) or crashes without a clean exit,
the instrument itself is unreliable - a gate that can hang or crash a launch is a defect, not a
verdict. I will impose a hard ~90 s wall timeout (internal budget is 5*(2+3)=25 s of definite
sleeps plus per-attempt join cost). A hang/crash => STEP 1 gate needs a fix (e.g. a bounded
Start() timeout) BEFORE it can be trusted; it is NOT a statement about the RTI.

## 4. What this does NOT establish
- Nothing about MOVEMENT, the type fix, or the confirming run (STEP 3). Readiness only.
- A single warm PASS does not validate the COLD or NEGATIVE gate paths (those are separate
  STEP 1 validation runs, per RTI_LAUNCH_HARDENING_DESIGN.md).
- Per A3, even a PASS does not prove the probe leaves a warm FedExec for the back-end.

## 5. Outcome (2026-07-23 ~12:36 local): EXIT 0 - RESIDENT STACK SERVICEABLE

RESULT: **exit 0 on attempt 1/5, wall 13 s.** Matched the EXIT-0 (serviceable) prediction.
FALSIFIED my registered lean toward EXIT 1 (wedged): the resident stack serviced a create-or-
join cleanly despite the 1-thread rtiexec/rtiForwarder appearance.

Key RtiProbe stdout (full capture in the run log):
- "Using MAK Technologies' RTI version 4.6.1 HLA 1516-2010 ... Release Mode."
- "Connected to RTI Assistant." (the resident rtiAssistant 40956 - only one exists)
- "Loading Config File: C:\MAK\makRti4.6.1\rid.mtl" ; "Loading config file: vrfLegion.lua"
- "[..] attempt 1/5: bridge.Start() - create-or-join CWIX-2024..."
- "[..] bridge.Stop() - resigning cleanly..."
- "[OK] RTI serviceable on attempt 1/5: created/joined CWIX-2024 and resigned cleanly."

Adversarial check (all clear): it used the RESIDENT stack (single rtiAssistant, real rid.mtl,
named federation CWIX-2024); it genuinely ran the full MAK RTI init + join + clean resign (not
a spurious true); it exited cleanly in 13 s (no hang/crash -> the gate instrument is reliable
on the PASS path); the reliability falsifier did NOT fire.

Process inventory AFTER (clean): resident trio intact and unchanged (rtiAssistant 40956 / 9
thr, rtiexec 60672 / 1 thr, rtiForwarder 61696 / 1 thr); NO leftover RtiProbe process; NO
stale federate. Ledger: appNo 3597 CONSUMED; marker advanced 3597 -> 3598.

WHAT THIS VALIDATES: the STEP 1 gate's non-DryRun PASS path works LIVE for the first time
(exit 0 -> the runner would proceed to Stage 3), and the resident RTI is serviceable right now
(so a STEP 3 run today would clear the gate). CORRECTION carried forward: idle 1-thread
rtiexec/rtiForwarder is NOT a wedge indicator - do not read it as one in future inventories.

STILL PENDING (per sec 4, separate live steps): the COLD path (gate retrying through a
fresh-boot churn window) and the NEGATIVE path (RTI down -> gate exits 1 -> launch refused,
never reaching PushInit). This warm PASS does not validate those.
