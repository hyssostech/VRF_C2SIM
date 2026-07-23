# PREREG: STEP 1 gate validation (COLD + NEGATIVE) + STEP 2 sequencing (registered 2026-07-23)

STATUS: REGISTERED, NOT RUN. All of this is LIVE and requires a REBOOT the user attends -> a
deliberate user go before any of it executes (non-negotiable). ASCII only.

Follows the WARM PASS already recorded (docs/experiments/PREREG_RTIPROBE_WARM_2026-07-23.md:
RtiProbe exit 0, resident stack serviceable, gate PASS path validated live). The two tests
below complete STEP 1's live validation (RTI_LAUNCH_HARDENING_DESIGN.md "Validation"); STEP 2
is the independent RUN-1 crash discriminator. A single attended reboot cycle bundles all three
(sec 3), but each keeps its own registered prediction/falsifier and its own verdict (the DAG
keeps STEP 1 and STEP 2 independent).

## Test N - NEGATIVE path: RTI down => gate FAILS the launch, never reaches PushInit

ONE VARIABLE: the RTI is DOWN (not serviceable); everything else is a normal gate invocation.

METHOD (two fidelities; run at least the cheap one, prefer both):
- CHEAP (tool-level): with the RTI stopped, run
  `RtiProbe.exe <freshAppNo> CWIX-2024 5 2 3` (cwd bin64, PATH+license as in the warm prereg).
- FULL (runner-level): run scripts/RunC2SimScenario.ps1 (NOT -DryRun) with the RTI down; the
  gate is Stage 2c. This exercises the Stop-Runner wiring, not just the tool. Needs the C2SIM
  server up and a scenario; costs the runner's full appNo allocation. Optional if the cheap
  test passes and the wiring was already read.

PREDICTION: every attempt's bridge.Start() fails (connection refused/broken - nothing
listening or FedExec unreachable); RtiProbe exhausts 5 attempts and EXITS 1 after ~25 s of
definite backoff plus per-attempt cost. Runner-level: Stage 2c takes the exit-1 branch ->
Stop-Runner 3 "REFUSING THE LAUNCH", and NO back-end is launched and NO PushInit is pushed.

DECISIVE FALSIFIER: RtiProbe EXITS 0 against a provably-down RTI (a false "serviceable") -> the
gate would wave a dead RTI through = the gate is broken and STEP 1 is NOT done. Secondary
falsifier (runner-level): the runner proceeds to Stage 3 / PushInit despite exit 1 -> the
Stage-2c wiring is wrong. Either overturns the warm-pass's "gate works" implication.

## Test C - COLD path: fresh-boot RTI => gate retries THROUGH the churn window and passes

ONE VARIABLE: the RTI is FRESHLY BOOTED (the RUN-2 condition - transient rtiexec churn for
~10-13 s before the persistent stack settles); the gate is invoked as normal.

METHOD: immediately after starting a fresh RTI (rtiexec just launching, before it settles), run
`RtiProbe.exe <freshAppNo> CWIX-2024 5 2 3`. Timing matters: start the probe INTO the churn
window, not after it settles, or the test does not exercise a retry.

PREDICTION: attempt 1 (and possibly 2) FAILS - bridge.Start() throws "connection has been
broken" during the churn - the probe backs off 3 s and SUCCEEDS on a later attempt (EXIT 0),
demonstrating the internal retry ABSORBS the exact cold-create window that killed RUN 2. If it
passes on attempt 1, the boot settled faster than the probe reached Start() (still a PASS, but
note it did not exercise a retry - re-run into a tighter window if we want to see the retry
fire).

FALSIFIER (TUNING, not design refutation): RtiProbe EXITS 1 after all 5 attempts against a
fresh boot that DOES eventually settle (confirm settling independently: rtiexec/rtiForwarder
reach steady thread counts, or a later WARM RtiProbe passes) -> the retry cap
(5 x (2+3) ~ 25 s) is shorter than this machine's cold-create window -> raise maxAttempts
and/or backoffSecs and re-run. Distinguish from the rti-fresh-boot-join-race pathology: a fresh
boot that NEVER becomes serviceable is an RTI problem (memory rti-fresh-boot-join-race), and the
CORRECT gate behavior there is exit 1 (refuse the launch) - which is a gate SUCCESS, not a
failure. Read Test C against whether the RTI eventually settles, not against exit 1 alone.

## 3. Sequencing - one attended reboot cycle covers N, C, and STEP 2

STEP 2 (RUN-1 crash discriminator, per HANDOFF_2026-07-22_LAUNCH_HARDENING.md STEP 2): reboot;
repair BOTH x64 + x86 VC++ 2015-2022 redistributables; bring RTI + C2SIM docker back clean;
replay the exact 3x-CreateRoute-then-MoveAlongRoute sequence on a warm RTI. The reboot supplies
the windows for N and C for free:

1. After reboot, BEFORE bringing the RTI up (RTI is down): run **Test N** (negative).
2. Start rtiexec fresh; IMMEDIATELY run **Test C** (cold) into the churn window.
3. Once the stack is warm + confirmed-ready (a WARM RtiProbe exit 0, like today's): run
   **STEP 2's replay** (the crash discriminator). Reproduces => MAK case (dump preserved);
   does not => mid-session MSVC servicing was the RUN-1 culprit, procedural fix only.
4. Only after BOTH STEP 1 (warm+cold+negative all green) AND STEP 2 (crash resolved or shown
   not to recur) do we proceed to STEP 3 (RUN >= 3, the confirming run - a SEPARATE go).

appNo discipline: each RtiProbe join (N, C, any extra warm re-checks) takes a FRESH appNo from
the single "*** NEXT FREE:" marker (now 3598), ledgered BEFORE the join; STEP 2's replay uses
its own ledgered numbers per the runner. Verify docker REST 8080 / STOMP 61613 after the reboot
before any C2SIM-driven step.

## 4. Non-negotiables carried in
- One variable per test; predictions/falsifiers above are FIXED before running.
- NEVER force-kill a JOINED federate. Stopping rtiexec/rtiForwarder/rtiAssistant for the
  negative test or the reboot is a deliberate user-attended action (the reboot itself), not a
  force-kill of a healthy stack mid-run.
- After two consecutive RTI/VR-Forces framework failures, research before the next attempt.
- This whole plan is LIVE and awaits a deliberate user go + an attended reboot. NOT started.

## 5. Outcomes (filled as run)

### Test N (2026-07-23 13:31 local, appNo 3598): exit 0 - INVALID as a negative test; MAJOR finding

The user rebooted; post-reboot inventory showed NO rti* processes (RTI down) and no VR-Forces.
Ran RtiProbe.exe 3598 CWIX-2024 5 2 3 expecting exit 1. RESULT: **exit 0 on attempt 1, 27 s.**
RtiProbe stdout: "Attempt to create and connect to Assistant" -> "Connected to RTI Assistant."
-> loaded rid.mtl + vrfLegion.lua -> created/joined CWIX-2024 -> resigned cleanly.

INTERPRETATION (adversarially checked): NOT a gate defect / NOT the decisive falsifier firing.
The falsifier premise ("a PROVABLY-DOWN, unserviceable RTI") was NOT met: the RTI was merely
NOT RUNNING, and **MAK AUTO-STARTED it** (Assistant 51140 born 13:31:29, rtiexec 8196 13:31:36,
rtiForwarder 47544 13:31:38 - all created BY the probe during the run). Confirmed by a
post-run inventory: the auto-started trio PERSISTED after the probe exited (it did NOT tear it
down, unlike RUN 2's transient rtiexec instances). exit 0 is CORRECT: a serviceable RTI now
exists.

WHY THIS MATTERS (positive for the gate's RUN-2 utility): a DOWN RTI -> the gate auto-starts
it, absorbs the startup/create churn (the exact window RUN 2's back-end lost its race in), warms
it, confirms serviceable, and LEAVES it running. The back-end then joins a settled rtiexec with
no churn. This is the RUN-2 fix, and it also means the runner needs no separate RTI-launch step
- the gate brings the RTI up and verifies it. (It succeeded on attempt 1, so the retry did not
have to fire here - this machine's auto-start settled fast.)

CONSEQUENCE for the NEGATIVE test: "just don't run rtiexec" cannot produce exit 1 because MAK
auto-starts it. A genuine negative test needs a truly-unserviceable condition the auto-start
cannot rescue - e.g. a broken/absent license, a broken rid.mtl / connection config, or an
occupied rtiexec port 4000. Redesign required before the negative path can be validated; the
gate's exit-1 path (Program.cs / Stage 2c) remains code-verified but live-unexercised.

STATUS after Test N: WARM path (3597) and DOWN-but-startable path (3598) both PASS (gate makes
the RTI serviceable). NEGATIVE path (genuine-unserviceable) still needs a valid test. COLD-with-
forced-retry not yet seen (auto-start settled on attempt 1). RTI is now freshly UP and warm
(51140 / 8196 / 47544) - a clean substrate for the STEP 2 crash discriminator.

### STEP 2 (2026-07-23, folded into the confirming run RUN 3, appNos 3599-3604 +3605 burned): NO REPRODUCTION

Ran the full confirming run (scripts/RunC2SimScenario.ps1, R9 NoComments, -RunSecs 900) on the
warm gate-verified RTI, reboot-only (user chose no VC++ repair). It reached and passed the
exact CreateRoute->MoveAlongRoute sequence RUN 1 crashed on (all 3 routes + all 3 MoveAlongRoute
issued), observed 900 s, and tore down cleanly (runner exit 0). NO vrfSim dump written
2026-07-23. VERDICT: the RUN-1 crash did NOT reproduce -> it was ENVIRONMENTAL (mid-session MSVC
servicing timing and/or the aged preserved stack), cleared by the reboot + fresh vrfSim load +
fresh gate-verified RTI. No MAK support case; the procedural fix stands (never launch vrfSim
during/right after toolchain servicing). Notably it held even WITHOUT the VC++ repair, which
tightens the attribution to the reboot/fresh-load. STEP 2 CLEARS. Full movement verdict (the
platoon MOVED; company + entity froze) is in PREREG_TYPEFIX_CONFIRMING_RUN.md "Outcome - RUN 3".

NEGATIVE gate path (Test N redesign) remains the only open STEP 1 validation item; it did NOT
block STEP 2/3 and can be done later with a genuinely-unserviceable RTI (license/config/port).
