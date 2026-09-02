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

## 6. Outcome (written 2026-09-02 00:50Z from the artifacts, after the run)

Run 20260902T003710Z (runs/20260902T003710Z_run), runner exit 0, appNos 3669-3675
(ledger wasValue 3669 / newValue 3676 - as predicted; docs/OPUS_EXECUTION_PLAN.md after
the rewrite: CRLF 1819 / bare LF 0 - ruling item 2 holds). Pre-launch inventory held
(VR-Forces down, no observers, RTI 41336 / 224608 / 76620, docker Up, checkout at e602f76).

A. COMPLETION - MET. TASKCMPLT offsets from orderPushedUtc 00:39:34.627: +117.1 / +129.1 /
   +182.1 s (P2c +117.2 / +129.2 / +184.6; CONFIRM +117.2 / +129.2 / +183.7). 3 TASKCMPLT
   lines in vrfc2simapp.log and 3 in reports-captured.log; TSK trace records at 145.4 /
   157.3 / 210.3 (CONFIRM 145.3 / 157.3 / 211.8).
B. ENDPOINTS - MET (all gated items, including the one CONFIRM missed). POS finals:
   1.BdeHQ 34.608416,-116.699996 (0.27 m from P2c, 0.00 m from CONFIRM); 114.MechCoy
   34.653915,-116.693388 (0.00 m / 0.00 m); 1222.MechPlt 34.612956,-116.587783 (0.09 m /
   0.09 m). Settled true for all three. POS==RPT: 1.BdeHQ 0.0 m (last RPT t=278.8),
   114.MechCoy 0.0 m (t=267.1), 1222.MechPlt 0.0 m (t=217.8). Plateau onsets 147.6 /
   214.7 / 159.8 (CONFIRM 147.6 / 218.8 / 161.7).
C. EARLY EXIT - MET. oracle.earlyExit.fired true; allCompleteUtc 00:42:40.473,
   evidenceSatisfiedUtc 00:43:38.428, closedUtc 00:43:45.078, windowSecsUsed 220.2 of
   420; reportEvidence all three satisfied, distanceM 0.0, lastRptT > completionT
   (1222.MechPlt 217.8 > 157.3; 114.MechCoy 267.1 > 210.3; 1.BdeHQ 217.3 > 145.4);
   clocks.traceStopRequestedUtc 00:44:15.724; watchvrf-trace.csv ends `# STOP requested
   via stop-file at t=309.8s (duration cap was 980s)` / `[..] bridge.Stop() -
   resigning...` / `[OK] resigned cleanly.`; listenreports.stdout.log `stop requested
   via stop-file at t=313.6s ... disconnecting` then `captured 30 reports ->
   reports-captured.log`; WatchVrf-trace exit 0 ended 00:44:17.986, ListenReports exit 0
   ended 00:44:17.989, StopVrf started 00:44:18.029; 0 `[WARN]`, 0 `[FAIL]` lines in
   the console log. Console: "ALL taskees reported TASKCMPLT at t+156s; holding >= 60s
   AND waiting for post-completion reports" -> "report evidence IN for all 3 taskee(s)
   at t+214s: 1222.MechPlt RPT t=217.8 vs POS 0 m; 114.MechCoy RPT t=267.1 vs POS 0 m;
   1.BdeHQ RPT t=217.3 vs POS 0 m" -> "settle hold of 60s elapsed (63.9s) and report
   evidence in - closing the observation window EARLY at t+220s of the 420s cap".
D. WALL TIME - MET. startUtc 00:37:10.418 -> savedUtc 00:44:27.637 = 7 min 17 s
   (budget 9; CONFIRM 7 min 9 s; P2c 26 min 23 s). Setup 144.2 s (2.4 min) / window
   250.5 s (4.2 min) / tail 42.6 s (0.7 min). Rule 4 cost +4.4 s of window vs CONFIRM
   (the floor gated, see F); the tail grew 3.9 s (StopVrf 9.5 s vs 5.9 s - unrelated
   to the change; report only).
E. HYGIENE - MET. StopVrf exit 0; `no WatchVrf / ListenReports observer remains`;
   Get-Process after the runner: only rtiAssistant 41336 / rtiexec 224608 / rtiForwarder
   76620 (unchanged); bin64-vrfSim.log 0 / 0 / 0 nav-route errors, 1 "invalid formation
   name" (baseline); 0 SocketException / "Only one usage" / "Connection error".
F. CLOSE TIME - MET. closedUtc - allCompleteUtc = 64.6 s (band 60-90). Detail: the
   report rounds landed at 24-39, 84-100, 145-162, 206-223, 267-279 (44 lines each, the
   last cut at 32 by StopIface) - anchored ~6 s EARLIER on the trace clock than CONFIRM.
   The company's round-4 line (t=206.7, 34.653270, 72 m short) came BEFORE its TSK
   (210.3) and was correctly NOT counted; its round-5 line (t=267.1) read the final and
   satisfied rule 4 at the 00:43:38 poll, 58.0 s after allComplete - so in THIS run the
   60 s FLOOR was the binding condition and the evidence was already in; the close came
   at the next poll after the floor (+64.6 s). The 213.3-shaped case (a post-TSK report
   of a still-converging centre) did not recur here; rule 4's protection against it is
   established by the offline fixture (tests 4b), not by this run.

VERDICT: all six predictions MET; prediction B - the one CONFIRM missed - is MET with
POS==RPT 0.0 m for all three taskees. -StopWhenComplete runs are now valid for POS==RPT
adjudication (n=1 under rule 4; keep the canonical fixed-window run per milestone). No
unexplained symptom: the two `POS,308.6,...,NaN,90.000000,NaN` samples at the very end
of the trace are post-StopIface (entities being torn down at t~305-310, after the
interface resigned) and are filtered as degenerate by both Get-RealPositions and
Get-TraceEvidence; same shape as the end of CONFIRM's trace.
