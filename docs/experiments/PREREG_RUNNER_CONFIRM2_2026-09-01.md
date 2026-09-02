# PREREG RUNNER-CONFIRM2: evidence-based settle hold (rule 4) - registered 2026-09-02, BEFORE running

Source: supervisor ruling 2026-09-02 on PREREG_RUNNER_CONFIRM_2026-09-01.md sec 6
prediction B (company POS==RPT 11.8 m miss), implemented in c4e19c7 (RunnerLib
Test-ReportEvidence + Test-EarlyExit -ReportEvidence; design docs/RUNNER_TURNAROUND_
2026-09-01.md sec 3 rule 4; RUNBOOK 0.5.11 item 2). Reference runs: P2c 20260901T211310Z
(1x fixed window, the endpoint record) and CONFIRM 20260901T235823Z (1x -StopWhenComplete,
hold-only rule). docs/HEADLESS_RUN_PLAN.md sec 4a.1 for arrival / settled. ASCII only.
The C++ repo (c2simVRFinterfacev2.36) is a frozen oracle.

## 1. The variable: the early-exit criterion gains rule 4 (report evidence)

Identical to CONFIRM in every other respect: 1x (no `Vrf__TimeMultiplier`), init
data/R9_Mojave_Lean_Initialization_NoComments.xml, order
data/R9_Mojave_UnitMove_Order_NoComments.xml, RealTemplates / Live create-altitude mode,
stock templates, no env overrides, untouched product, the same WatchVrf.exe /
ListenReports.exe (stop-file capable), `-RunSecs 420 -StopWhenComplete`, SettleHoldSecs
60 (default).

What changes (c4e19c7): the window now closes only when (1-3) every order taskee has
TASKCMPLT, the line count covers the task count, and 60 s have elapsed since the poll that
first saw that (the FLOOR), AND (4) for every taskee the live watchvrf-trace.csv holds an
`RPT POSITION "<marking>"` line LATER (trace clock) than that taskee's `TSK` record and
within 2 m of the taskee's latest real `POS`. In CONFIRM the company's post-TSK RPT at
t=213.3 (1.5 s after TSK 211.8) read a still-converging centre (11.8 m from the POS final
reached at 218.8); its next round (started 267.4) was cut by StopIface at 278.0 before the
company's line. Rule 4 must therefore hold the window through that next round. Also new,
LIGHT: the runner's ledger rewrite now writes explicit CRLF.

## 2. Invocation (from the main checkout, VRF_C2SIM)

    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_NoComments.xml `
        -RunSecs 420 -StopWhenComplete

Console log to the session scratchpad; adjudication from the run directory artifacts only.
Ledger: marker `*** NEXT FREE: 3669 ***` before the run (verified 2026-09-02, working copy
CRLF 1806 / bare LF 0); 7 numbers per run -> expected wasValue 3669 / newValue 3676
(appNos 3669-3675). The ledger file must still be all-CRLF after the run (item 2 of the
ruling).

## 3. Pre-launch inventory (must hold, else STOP and report - never kill)

VR-Forces DOWN (no vrfLauncher / vrfSimHLA1516e / vrfGui); no WatchVrf / ListenReports /
VrfC2SimApp; RTI trio resident: rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620
(handoff OPERATIONAL STATE 2026-09-02 00:15Z, VR-Forces confirmed down at 00:05:36Z);
docker stp-server + c2sim server Up; main checkout clean at c4e19c7 or later.

## 4. Predictions and what counts as a miss

A. COMPLETION (HIGH). 3/3 TASKCMPLT (1.BdeHQ / 1222.MechPlt / 114.MechCoy) within the
   window; order-push-relative times (reports-captured.log stamps minus
   clocks.orderPushedUtc) within +/-15 s of P2c's +117.2 / +129.2 / +184.6 s (CONFIRM:
   +117.2 / +129.2 / +183.7). Miss = fewer than 3 or any offset outside the band = STOP.
B. ENDPOINTS (HIGH - raised from CONFIRM's partial miss; this is the prediction rule 4
   exists to make). Each taskee's final POS plateau within 2 m of P2c (1.BdeHQ
   34.608416,-116.699993; 114.MechCoy 34.653915,-116.693388; 1222.MechPlt
   34.612956,-116.587784); ALL THREE POS==RPT <= 2 m (last RPT POSITION line for the
   marking vs the POS final); "settled" per 4a.1 (<10 m over the last 3 consecutive POS
   samples). Plateau onsets REPORTED, not gated (P2c 147.8 / 217.3 / 160.1; CONFIRM
   147.6 / 218.8 / 161.7, trace clock). Miss on any gated item = STOP.
C. EARLY EXIT (HIGH). run-manifest.json oracle.earlyExit.fired true; evidenceSatisfiedUtc
   present and <= closedUtc; reportEvidence.<taskee>.satisfied true for all three with
   distanceM <= 2 and lastRptT > completionT; clocks.traceStopRequestedUtc present;
   `# STOP requested via stop-file` in watchvrf-trace.csv FOLLOWED by `[OK] resigned
   cleanly.`; `stop requested via stop-file` in listenreports.stdout.log and a written
   reports-captured.log; both observer stages exitCode 0 with endedUtc BEFORE the StopVrf
   stage startedUtc; no Complete-Background grace WARN. Miss on any item = STOP.
D. WALL TIME (MEDIUM). startUtc -> savedUtc <= 9 min (CONFIRM 7 min 9 s; rule 4 is
   expected to add ~5-20 s to the window). Breakdown reported as setup / window / tail vs
   CONFIRM 2.4 / 4.1 / 0.6 min and P2c 2.4 / 15.5 / 8.4 min. Miss = report, not STOP.
E. HYGIENE (HIGH). VR-Forces down after teardown (StopVrf exit 0, post-run inventory
   clean); RTI trio PIDs unchanged; no WatchVrf / ListenReports left; bin64-vrfSim.log
   0 "Waiting for nav data", 0 "empty route", 0 "Can't find entity route" (1 "invalid
   formation name" = baseline); 0 SocketException / "Only one usage" / "Connection
   error". Miss = STOP (stale federate or RTI change) or report (log counts).
F. CLOSE TIME (MEDIUM - n=1 cadence measurement; the report rounds' anchor relative to
   the trace clock has been seen in one run). closedUtc - allCompleteUtc in
   [60 s, 90 s]. Reasoning from CONFIRM: text-report rounds every ~60.5 s, each ~44
   lines over ~17 s (24-39, 84-102, 146-163, 206-224, 267-...), the company's line ~7 s
   into a round, the platoon's ~15 s in; last TSK at 211.8 -> floor ends at the poll
   stamp + 60 (~272-277); the company's agreeing RPT is expected ~274.5 and is picked up
   by the next 5 s poll -> close ~275-280 = +63..68 s; a round drifting later pushes it
   toward +80 s. Below 60 s is impossible by construction (floor). Miss = report and
   explain (which taskee gated, when its RPT came); NOT a STOP unless the window ran to
   the cap (then C has already failed).

## 5. Adjudication method

Same scratch script as CONFIRM (session scratchpad adjudicate_confirm.py, calibrated on
P2c: reproduces +117.2 / +129.2 / +184.6, the endpoints, POS==RPT 0.0 m, settled true,
vrfSim/socket counts 0/0/0/1 and 0/0/0), run against the new run directory with CONFIRM
as the reference. Plus, for C and F: the manifest's oracle.earlyExit block
(evidenceSatisfiedUtc, reportEvidence per taskee, allCompleteUtc, closedUtc) and the
console log's "report evidence IN" / "closing the observation window EARLY" lines. Ledger
line-ending check: byte count of CRLF vs bare LF in docs/OPUS_EXECUTION_PLAN.md after the
run (expected bare LF = 0).

## 6. Outcome

(to be written from the artifacts after the run)
