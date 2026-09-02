# PREREG RUNNER-CONFIRM: stop-file teardown + -StopWhenComplete - registered 2026-09-01, BEFORE running

Source: docs/RUNNER_TURNAROUND_2026-09-01.md sec 4 ("Expected budget after the change"
and its (a)-(d) confirming-run list), docs/experiments/REVIEW_RUNNER_TURNAROUND_2026-09-01.md
(verdict MERGE WITH FIXES, F1-F5 applied in 3ed334f, merged to main in 3e19c88),
docs/HEADLESS_RUN_PLAN.md sec 4a.1 (arrival + "settled" rules), docs/RUNBOOK.md 0.5.11.
Reference run: P2c 20260901T211310Z (1x, 3/3 TASKCMPLT, the endpoint record). Executor
brief 2026-09-01. ASCII only. The C++ repo (c2simVRFinterfacev2.36) is a frozen oracle.

## 1. The variable: the runner's teardown path (stop-file + -StopWhenComplete)

Everything else identical to P2c: 1x (no `Vrf__TimeMultiplier`), init
data/R9_Mojave_Lean_Initialization_NoComments.xml, order
data/R9_Mojave_UnitMove_Order_NoComments.xml, RealTemplates / Live create-altitude mode,
stock templates, no env overrides, untouched product. The runner binaries are the ones
built from main after the merge (WatchVrf.exe / ListenReports.exe, `--capabilities`
exit 0, tokens include `stop-file`).

What changes: (1) both observers receive `--stop-file <runDir>\observers.stop`, which the
runner touches at StopIface + TrailSecs (30 s) - the observers end there instead of at
their 980 s duration cap; (2) `-StopWhenComplete` closes the observation window
SettleHoldSecs (60 s) after the last order taskee's TASKCMPLT instead of at RunSecs.
`-RunSecs 420` stays the cap (P2c used 900; NEXT 5(a) adopted 420 for probes; the cap
is not expected to bind - P2c's last completion was at +184.6 s).

## 2. Invocation (from the main checkout, VRF_C2SIM)

    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_NoComments.xml `
        -RunSecs 420 -StopWhenComplete

Console log to the session scratchpad; adjudication from the run directory artifacts only.
Ledger: marker `*** NEXT FREE: 3662 ***` before the run; the runner allocates 7 numbers
per run, so the expected ledger record is wasValue 3662 / newValue 3669 (appNos
3662-3668) if nothing else consumed a number in between.

## 3. Pre-launch inventory (must hold, else STOP and report - never kill)

VR-Forces DOWN (no vrfLauncher / vrfSimHLA1516e / vrfGui); no WatchVrf / ListenReports /
VrfC2SimApp; RTI trio resident: rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620
(handoff OPERATIONAL STATE); docker stp-server + c2sim server Up; main checkout clean.

## 4. Predictions (HIGH confidence unless noted) and what counts as a miss

A. COMPLETION. 3/3 TASKCMPLT (taskees 1.BdeHQ / 1222.MechPlt / 114.MechCoy) within the
   window; order-push-relative times (reports-captured.log stamps minus
   clocks.orderPushedUtc) within +/-15 s of P2c's +117.2 / +129.2 / +184.6 s.
   Miss = fewer than 3, or any offset outside the band. A miss is a STOP (no re-run).
B. ENDPOINTS. Each taskee's final POS plateau within 2 m of P2c (1.BdeHQ
   34.608416,-116.699993; 114.MechCoy 34.653915,-116.693388; 1222.MechPlt
   34.612956,-116.587784); POS==RPT (last RPT POSITION line within 1 m of the POS
   final); "settled" per 4a.1 (<10 m change over the last 3 consecutive POS samples).
   Plateau onset (first sample after which every later sample is within 1 m of the
   final) is REPORTED, not gated: P2c 147.8 / 217.3 / 160.1 s on the trace clock, and
   the confirming trace starts at a different wall offset. Miss on any gated item = STOP.
C. EARLY EXIT. run-manifest.json: oracle.earlyExit true; clocks.traceStopRequestedUtc
   present; `# STOP requested via stop-file` in watchvrf-trace.csv FOLLOWED by
   `[OK] resigned cleanly.`; `stop requested via stop-file` in listenreports.stdout.log
   and a written reports-captured.log; both observer stages exitCode 0 with endedUtc
   BEFORE the StopVrf stage startedUtc; the Complete-Background grace WARN (review F1
   path) does NOT fire. Miss on any item = STOP (design note sec 4: (a) and (d)).
D. WALL TIME (MEDIUM confidence). startUtc -> savedUtc <= 12 min; design ~7.5 min
   (RUNNER_TURNAROUND sec 4: ~173 s setup + ~244 s window + ~31 s tail + ~6 s StopVrf).
   Breakdown reported as setup (start -> orderPushed) / window (orderPushed ->
   observationEnd) / tail (observationEnd -> saved) against P2c's 2.4 / 15.5 / 8.4 min.
   Miss = over 12 min: report the breakdown; not a STOP by itself.
E. HYGIENE. VR-Forces down after teardown (StopVrf exit 0, post-run inventory clean);
   RTI trio PIDs unchanged (41336 / 224608 / 76620); no WatchVrf / ListenReports process
   left; bin64-vrfSim.log: 0 "Waiting for nav data", 0 "empty route", 0 "Can't find
   entity route" (the 1 "invalid formation name" line is the P2c/P3/P3R baseline);
   0 SocketException / "Only one usage" / "Connection error" across the run's logs.
   Miss = STOP (stale federate or RTI change) or report (log counts).

## 5. Adjudication method

Scratch script (session scratchpad adjudicate_confirm.py, calibrated on P2c BEFORE the
run: it reproduces P2c's +117.2 / +129.2 / +184.6 s offsets, the three endpoints above,
POS==RPT 0.0 m, settled true, and the vrfSim/socket counts 0/0/0/1 and 0/0/0). Taskee
name -> VRF_UUID from bin64-vrfSim.log "Locally Simulated: <name> (VRF_UUID:...)".
Wall-time and stage ordering from run-manifest.json clocks/stages. Process state from
Get-Process after the runner exits.

## 6. Outcome (written 2026-09-02 00:15Z from the artifacts, after the run)

Run 20260901T235823Z (runs/20260901T235823Z_run), runner exit 0, appNos 3662-3668
(ledger wasValue 3662 / newValue 3669 - as predicted). Pre-launch inventory held.

A. COMPLETION - MET. reports-captured.log TASKCMPLT stamps 00:02:44.4 / 00:02:56.4 /
   00:03:50.9 vs orderPushedUtc 00:00:47.172 = +117.2 / +129.2 / +183.7 s (P2c +117.2 /
   +129.2 / +184.6; deltas 0.0 / 0.0 / -0.9 s). vrfc2simapp.log 3 TASKCMPLT lines;
   manifest oracle.earlyExit.firstSeenUtc 00:02:47.8 / 00:02:57.9 / 00:03:53.1.
B. ENDPOINTS - PARTIAL MISS (one gated item). POS finals: 1.BdeHQ 34.608416,-116.699996
   (0.27 m from P2c); 114.MechCoy 34.653915,-116.693388 (0.00 m); 1222.MechPlt
   34.612956,-116.587782 (0.18 m). Settled (<10 m over last 3 samples) true for all
   three. Plateau onsets 147.6 / 218.8 / 161.7 s (P2c 147.8 / 217.3 / 160.1). POS==RPT:
   1.BdeHQ 0.0 m, 1222.MechPlt 0.0 m, 114.MechCoy 11.79 m -> MISSED for the company.
   The last company RPT is `RPT,213.3,"POSITION ""114.MechCoy"" 34.653809 -116.693388"`,
   emitted 5 s BEFORE the company's own TASKCMPLT (t=217.8) while the center was still
   closing (P2c's same-round report at 214.3 s read 34.653877, 4.2 m short, and the
   NEXT round at 275.3 s read the final). VR-Forces text reports come in ~60 s rounds
   (~44 POSITION lines spread over ~10 s); in this run the last round started at
   t=267.4 and had emitted 20 of ~44 lines when StopIface fired at t=278.0 (00:04:53.3)
   - the company's line of that round had not come yet. Ground truth (POS) puts the
   company at the P2c endpoint; the RPT side is a report that was never emitted before
   teardown, not a wrong report. Per sec 4 this is a STOP: no re-run, no retune here.
   Cause claim and remedy are for the supervisor - see the note below.
C. EARLY EXIT - MET. oracle.earlyExit.fired true (allCompleteUtc 00:03:53.062, closedUtc
   00:04:53.292, windowSecsUsed 215.8 of 420); clocks.traceStopRequestedUtc
   00:05:23.961Z; watchvrf-trace.csv ends `# STOP requested via stop-file at t=305.2s
   (duration cap was 980s)` / `[..] bridge.Stop() - resigning...` / `[OK] resigned
   cleanly.`; listenreports.stdout.log `stop requested via stop-file at t=308.6s
   (duration cap was 980s) - disconnecting` then `captured 29 reports -> ...
   reports-captured.log`; stages WatchVrf-trace exit 0 ended 00:05:25.915, ListenReports
   exit 0 ended 00:05:25.917, StopVrf started 00:05:25.955 (both observers ended ~2 s
   after the touch and BEFORE StopVrf); no `[WARN]` line anywhere in the console log
   (the F1 cap-wait path did not fire).
D. WALL TIME - MET. startUtc 23:58:23.020 -> savedUtc 00:05:31.987 = 7 min 9 s
   (design ~7.5 min, budget 12). Breakdown: setup 144.2 s (2.4 min) / window 246.1 s
   (4.1 min) / tail 38.7 s (0.6 min) vs P2c 2.4 / 15.5 / 8.4 min. Tail = StopIface ->
   app exit 3.7 s + hold to StopIface+30 s + stop-file -> observers exit ~2 s + StopVrf
   5.9 s + manifest.
E. HYGIENE - MET. StopVrf exit 0; post-run inventory `no WatchVrf / ListenReports
   observer remains`; Get-Process after the runner: only rtiAssistant 41336 / rtiexec
   224608 / rtiForwarder 76620 (unchanged; manifest preflight.rtiInfra identical);
   bin64-vrfSim.log 0 / 0 / 0 nav-route errors, 1 "invalid formation name" (baseline);
   0 SocketException / "Only one usage" / "Connection error".

VERDICT: the turnaround mechanism is confirmed (A, C, D, E all MET, F1-F5 paths behaved).
The one miss is in the RPT half of B and is a WINDOW-LENGTH interaction, not a movement
or interface defect: with SettleHoldSecs 60 equal to the ~60 s text-report cadence, the
post-completion report round is only PARTLY inside the window (it takes ~10 s to emit
and StopIface can land mid-round). NOT verified here (single run, n=1): that a longer
hold (>= 90 s) or a "hold until every taskee has a post-completion RPT" rule closes the
gap - that is a design change and needs its own prereg. Until ruled on, -StopWhenComplete
runs cannot be used for POS==RPT adjudication; the POS side is unaffected.
