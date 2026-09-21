# PREREG - DEMO REHEARSALS (2026-09-20, seat). Written BEFORE any rehearsal runs.

Goal (user 2026-09-20): get to the demoable deliverable. DEMO_RUNBOOK sec 10 lists what was never rehearsed;
each rehearsal below closes one of those lines. One run per claim; a missed HIGH prediction is a STOP.

## D1 - Way A with the GUI, verbatim from DEMO_RUNBOOK sec 1

Command: scripts/RunScenario.sh --gui --scenario R9_Mojave_Empty_52 --init data/R9_Mojave_Lean_Initialization.xml
--order data/R9_Mojave_UnitMove_Order.xml --client-id STP --object-console -1 --member-console -1 (plus --log).
Main 259082d or later: Stage 2h federation holder (STP-825), bridge pin 90272BC9 (STP-832), STP-833 route-extent
check ON, STP-837 traversal-based arrival, STP-822 liveness ON, WS tripwire abort at 3 alerts.

Predictions:
- HIGH: the holder joins, vrfLauncher + the GUI front end + the back end all JOIN (no create failure), READY.
- HIGH: 6 units created from the lean init and visible; 3 TASKSTRT; nothing refused by STP-833 (legs 557-578 m).
- HIGH: the three tasks complete with VENDOR completions; under STP-837 no task closes on arrival evidence before
  its members have travelled >= half the route (the order's routes are out-and-away, so arrival evidence may still
  close the company/entity tasks - but only after traversal). First live exercise of STP-837.
- HIGH: no BACK END LOST, no WS RUNAWAY abort, teardown clean (GUI and back end closed, rtiexec + holder left).
- MEDIUM: the fixture's frame mode makes the move visible to an audience (if it is fixed-frame-run-to-complete
  the 580 m legs finish in ~20-30 wall seconds - too fast to watch; then readiness row 10's REAL-TIME fixture is
  the next work item, not a failure of D1).
- UNKNOWN (observe, do not predict): whether the GUI raises any modal (licence, terrain download, session
  dialog) that blocks an unattended start. Any modal = a finding for the runbook, and a STOP if it blocks READY.
MISS = any startup failure, any task not terminal, any leftover process after teardown.

### D1 RESULT (run 20260920T172141Z, harvest 2026-09-20)

Verdicts (sec 0): 1 HIT (holder+back end+GUI joined, no FOM Reader error, READY; vrfLauncher
clause NOT-MEASURABLE - LaunchVrf52 starts no vrfLauncher). 2 HIT (6 units, 3 TASKSTRT, 0
STP-833 refusals). 3 HIT on the traversal guard - all three closed on ARRIVAL EVIDENCE
(STP-837 margins +407/+648/+1040 m over 578/578/548 m thresholds); the 'vendor completions'
half is MISSED (later, swallowed). 4 MISS - vrfGui pid 39652 survived teardown (exit 3).
5 MISS, favourably - sim/wall ratio 1.00 (+/-0.02); terminal reports at wall 38.0 s / 153.4 s /
284.0 s (order-to-last-completion 4 min 44 s). 6 NO SIGN of a startup modal (46.5 s to READY,
inside the headless range 18.0-49.5 s, n=34). 7 HIT (TASKCMPLT label names the real code).

Teardown cause (sec 8d): MOST PROBABLE CAUSE is the GUI's documented exit prompt (UG52
4.6/4.6.1, myShowQuitDialogOnClose=1, ON in this run) going unanswered - StopVrf52.ps1 sends
WM_CLOSE and drives no dialog by design. FIRST 5.2 GUI-on teardown ever (1 of 80 StopVrf52
runs); the 8/8 and 79/79 clean record is headless-only. VERIFIED by window enumeration of pid
39652 (sec 9): TWO modals, both class makVrf::DtNeverAskAgainMessageBox - "Are You Sure?" /
"Quit VR-Forces GUI" with checkbox "Quit All Sim Engines" (underneath, disabled), and "Session
Status" / "The current session has ended. Close current terrain?" with checkbox "Execute
session changes without prompting." (on top, the only enabled window). WM_CLOSE was accepted
(it opened modal 1); no save-scenario prompt exists anywhere. Modal 2 arises because
StopVrf52 stops the back end ~20 s after closing the GUI while modal 1 is still open - our own
teardown order manufactures the stack.

Remedy direction (seat): both never-ask-again settings via documented configuration, no GUI
automation; Jira STP-844. Unpredicted finding: four dismounts gave up on an obstructed
destination - Jira STP-845.

D1 = STOP on prediction 4; D2 NOT RUN; remedy lane fix/gui-quit-prompt-teardown; the
confirming run is D1b, to be registered before it runs.
2026-09-21: the "real time / ratio 1.00" reading here is WITHDRAWN - see D7 RESULT N7.

## D1b - Way A with the GUI and the run-owned appData (STP-844 confirming run)

Registered 2026-09-20 BEFORE the run. Command = D1's exact command (above) plus
`--vrf-appdata-dir C:\C2SIM\vrf-appdata-unattended\appData` (NO trailing backslash - it
makes the quoted argument's closing `\"` read as an escaped quote and vrfGui silently
ignores the option). One-time setup: `scripts\NewVrfAppData52.ps1 -Dest
C:\C2SIM\vrf-appdata-unattended`, run once by the seat, plus a post-seed copy of the
settings directory kept in the scratchpad for a post-run diff.

Predictions (guiquit_report.md sec 8, review-amended):
- P1 (HIGH): LaunchVrf52's [OK] line names the RELOCATED path, not C:\MAK.
- P2 (HIGH): "Are You Sure?" / "Quit VR-Forces GUI" never opens - CloseMainWindow TRUE, the
  front end gone inside the 20 s grace, no post-grace window diagnostic runs.
- P3 (MEDIUM-HIGH): "Session Status" / "The current session has ended. Close current
  terrain?" never opens.
- P4 (HIGH): StopVrf52 exit 0, runner exit 0, post-run VR-Forces inventory empty, nothing
  force-killed.
- P5 (HIGH): everything upstream of teardown repeats D1 (holder joins, READY unattended, 6
  units, 3/3 terminal, real time). D1b changes TWO things vs D1, not one: -VrfAppDataDir
  also relocates the connection-config file the runner reads (RunC2SimScenario.ps1:883-888),
  and the seed captures POST-D1 vendor state, not D1's starting state.
- P6 (LOW): default_Application.apsx and default_SessionSettings.srsx in the run-owned tree
  still hold the patched values after the run.

STOP rule: a P2 miss is a STOP. Falsification note for P3 (review item 8): a P3 miss WITH a
P2 hit does not fail the remedy - three candidates besides the bit mapping: one of the seven
unnamed set bits in mySessionOptions; the flag words in applicationSettings.xml; or session
settings arriving from the session database because DtAlwaysJoinWithSessionDatabase (0x4) is
set. The post-run settings-directory diff discriminates between them.

### D1b RESULT (run 20260920T185227Z)

P1-P6 all HIT (sec 0). StopVrf 9.87 s clean (D1: 121.3 s, exit 3); CloseMainWindow TRUE, no
post-grace window diagnostic ran. Upstream identical to D1 (READY 45.7 s, 6 units, 3
TASKSTRT/TASKCMPLT, ratio ~1.00, STP-845 recurred unchanged, 0 STP-833 refusals).
Settings diff: 344 vs 344 files, 0 added, 0 removed; of 4 changed, only
`layout_UILastSavedLayout.uisx` is D1b's - the other three carry D2 mtimes (D2 launched 49 s
later, contaminating the shared tree). Vendor `default_Application.apsx` still reads 1,
mtime unmoved; zero of 344 vendor settings files were written.
H2 (unnamed mySessionOptions bit) and H3 (applicationSettings.xml flags) RULED OUT; H4
(session-database supply) NOT SUPPORTED; H1 (DtShowSessionDialogs) CONSISTENT, not proven.
ADVERSARIAL: P3's hit is VACUOUS - modal 2 only ever arose because modal 1 was still open
when the back end was asked to close; modal 1 never opened here, so the condition that
raises modal 2 never occurred, and the 0x10 lever remains UNVERIFIED belt-and-braces.
D1's STOP is CLEARED.
2026-09-21: the "real time / ratio 1.00" reading here is WITHDRAWN - see D7 RESULT N7.

## D2 - reset between runs, the FULL CYCLE with the GUI (DEMO_RUNBOOK sec 6 item 1: 'run the one command again')

Registered 2026-09-20 BEFORE D1 runs. D2 = D1b's exact command a second time, started after D1b's teardown inventory is
clean (observer process count 0; D1b's holder may still be joined - EXPECTED, it is why D2's sim joins).
- HIGH: D2 reaches READY and completes 3/3 exactly as D1 did; no wedge (the 5.0.2-era teardown-relaunch wedge does
  not reproduce: the 5.2 runner teardown was 8/8 clean on 2026-09-14, headless; the GUI is the new variable).
- HIGH: D2's Stage 2h holder JOINS (fresh ledgered appNo) whether or not D1's holder is still in the federation.
MISS = D2 startup failure, observers blind, or any task not terminal -> STOP; the reset story for the demo is then
'restart from a clean desktop', and row 9 stays open. Sec 6 items 2-3 (GUI reload, ResetVrf) stay UNVERIFIED and
out of the demo - not needed once the full cycle is proven with the GUI.

### D2 RESULT (run 20260920T190230Z)

P2 HIT - holder appNo 4619 (pid 89116) joined in 3 s alongside D1b's still-joined 58520. P1
SPLIT: functional HIT (READY, 6 units, 3/3 TASKCMPLT, clean teardown, no wedge), but "exactly
as D1" is a MISS. Teardown 9.77 s clean (D1b 9.87 s).
The +97 s = a constant fleet-wide 0.57-0.63x entity speed, sim/wall 1.00 in both runs;
identical 1155 m BdeHQ path and identical MechPlt end geometry (12/16, 47 m, 1226 m) - later,
not different. STP-845 give-ups recurred at order+375.6 s vs D1b's +239.6 s (ratio 0.64, same
scaling); the company's close rule FLIPPED arrival evidence (D1b) -> vendor completion (D2).
REFUTED: (a) sim clock slow; (a') harvest load (0.57x predates the diff script); (b)
different path/members (geometry identical); (c) D1b's holder still joined (no recovery after
its 19:07:37.8Z resign). SURVIVING, not a finding: "second cycle, no settle time" -
confounded by an I/O-heavy harvest agent live in D2's window (launched the same moment,
incl. one OutOfMemory scan). Cause NOT DIAGNOSED; discriminator = D3 (same command, no
agents live, after a settle gap), to be registered before it runs.
Verdict: FUNCTIONAL reset VERIFIED; performance-neutral reset NOT VERIFIED. Operator
figures (n=1): READY->order 114 s; all-terminal 290 s rested / 387 s back to back; teardown
~10 s; previous exit -> next READY 80.1 s.

## D3 - Way A on the route-shift-default-ON build, clean conditions (registered BEFORE the run)

Build: main f26d4ad (route shift default ON, user ruling 2026-09-20; AO-independent
preflight); app rebuilt 2026-09-20T20:00:51Z; bridge pin 90272BC9 unchanged on all eleven
consumers; runner suite 341/0; 21/22 selftests green (22nd = fail-first liveness arm);
cold-start review NO BLOCKER - F1/F2/F3/F5/F6 deferred to a lane AFTER D3.
Command: D1b's exact command (with `--vrf-appdata-dir`). CONDITIONS (recorded because D2 was
contaminated): no subagent live, no stray scan process (seat lists pwsh/powershell/python/rg
first); >= 30 min since the previous teardown; the app's tile cache PRE-WARMED via
`tools\preflight\leg_check.py` for the R9 order - 4 L13/ds149 elevation tiles + 3 ds154
land-cover tiles, no CLCplus tiles (none served over Mojave); tool verdict 0 of 6 legs
flagged at L13.
PURPOSE, honestly: on this order NO leg is steep, so D3 does not test the shift FIRING (V8b
did); it tests default-ON is HARMLESS on the proven order and is the clean-conditions control
for D2's 0.60x.
P1 (HIGH): start-up banner says LATERAL ROUTE SHIFT ON, agrees with manifest
inputs.routeShift.effective=True; no EMPTY-cache WARN.
P2 (HIGH): each ground move preflighted at L13 from cache (0 HTTP fetches expected, a
handful tolerated), NO "did not finish within 30 s" line, NO ROUTE SHIFTED row, all three
dispatched on the AUTHORED line within ~5 s of the order.
P3 (HIGH): 3/3 terminal, closes by arrival evidence with D1-like margins, no STP-833
refusal, no BACK END LOST, no WS alerts. P4 (HIGH): teardown clean as D1b (StopVrf ~10 s,
exit 0/0, nothing left but rtiexec/rtiForwarder/holder).
P5 (MEDIUM, the 0.60x control): completions within +/-15% of D1/D1b (38/153-159/284 s after
the order), fleet speed ~4.6-5.9 m/s, NOT D2's 2.8-3.7. Rule: P5 HIT = D2's slowdown belongs
to D2's conditions (back-to-back cycle and/or the two I/O-heavy jobs, needing its own run);
P5 MISS (again ~0.60x) = the slowdown is NOT load - do not attribute it to the route shift
without a control, STOP and read.
Any leg scored COARSER than L13 makes that run's ratios unquotable (review F2); a 30 s
timeout line for a task means disregard that task's shift rows (review F1).

### D3 RESULT (first launch 20260920T202203Z - never reached VR-Forces; adjudicated launch 20260920T202549Z)

FIRST LAUNCH: all four Stage 2h creates refused by STP-825 (appNos 4630-4633, four modules,
one probe exit 0xC0000005); nothing reached VR-Forces, runner exit 3. Seat hand-started a
PERSISTENT holder (appNo 4634 refused + 0xC0000005, appNo 4635 JOINED, 28800 s hold); the
second launch's Stage 2h holder then joined normally. SAME registered run re-launched, not a
second sample; no prediction covered the holder failing (D1's prereg had it HIGH, D3's did not).
P1-P5 all HIT (sec 0). The "EMPTY"/"NO VERDICT" strings flagged are C13 ComposeHierarchy
shell-creation lines and the start-up banner's own prose, not preflight warnings; the real
empty-cache WARN never appears. Cache: app resolved exactly the pre-warmed 7-file directory;
0 fetches and L13-for-every-leg are INFERRED from cache-file forensics (no coarser tile
written), not logged - the service logs neither the resolved level nor fetch/hit counts
(instrumentation gap). Speed: fleet median 4.54-4.98 m/s (D1b's band), NOT D2's 2.8-3.7;
completions +0.7% / 0.0% / +1.7% vs D1b. Default-ON cost = ~4.5 s one-off dispatch deferral
on the first ground task only (vendor-table load, once per process).
P5 interpretation (registered rule): D2's 0.60x belongs to D2's conditions. D3 cannot
separate "back-to-back cycle" from "the two I/O-heavy jobs" - it removed both at once. The
separating run = the same command 30-60 s after a teardown with nothing else live (NOT yet
registered).
Anomalies: E1 a backwards sim-clock step (74.1 -> 72.9 s) before any dispatch, unexplained;
E3 BdeHQ's arrival margin still drifting (+407/+360/+308 m over three runs); E4 the manifest
omits the persistent holder; E8 D2's 439 extra GUI log lines did not recur.
2026-09-21: the "real time / ratio 1.00" reading here is WITHDRAWN - see D7 RESULT N7.

## D6 - R9 regression on main 248143f (registered BEFORE the run)

D4/D5 keep their reserved numbers; D6 is out of numeric order because it registers after them.
Build: main 248143f, rebuilt 2026-09-20T22:52:15Z, bridge pin 90272BC9 unchanged on all eleven consumers, 23 selftests (22 exit 0 + fail-first arm), suite 397/0, tile cache intact (7 R9 tiles).
First LIVE exercise of: runner-truth merge 35664ea (input-source provenance, route-shift PREDICTED vs ANNOUNCED + agreement flag, vendor logs captured BY PID never opened, LaunchVrf52 -FederationHeldByCaller, connection-config hash check) and intake merges 9d67f97+248143f (MapGraphicID resolution - R9 has 0 MapGraphicIDs, geometry UNCHANGED; de-stack ON but a NO-OP on R9 lean - 0 units move, 1141/1142/1143.MechPlt are composed children with no authored coordinates; tile decode-before-cache; claim-then-log in the shift worker; the one-line start-up cache banner).
Known open defect, UNREACHABLE here: CA2017 at VrfC2SimService.cs:3161 (HoldInPlace path; R9 is MOVE x3) - fix lane in flight.
Command: D3's exact command. Conditions RECORDED at launch as in D3: no subagent, no stray scan/build/generator process (vrfNavGenerator, dotnet, VBCSCompiler, MSBuild), >= 30 min since the last teardown, tile cache warm, baseline CPU noted; rtiexec pid 51560 (fresh, -n 3) with holder pid 12916 joined, so Stage 2h JOINS.
P1 (HIGH): start/init/6 units/3 TASKSTRT/3-3 terminal/teardown clean (StopVrf ~10 s, 0/0, only RTI infra + holder left) - as D3.
P2 (HIGH): completions within +/-10% of D3's 38.2/158.6/289.1 s, same close rule per task (arrival evidence), margins within +/-60 m of D3's, the same 4 STP-845 give-ups at comparable sim time, sim/wall 1.00; de-stack moves 0 units, placement lines say so.
P3 (HIGH): runner truth - Stage 0 names scenario/init/order source; manifest routeShift predicted ON and announced ON, agreement true; vendor sim+GUI logs for this run's pids copied in; the two historical "vrfGui.log not found" WARNs GONE; the false "no holder" alarm GONE; connection-config hash VERIFIED IDENTICAL.
P4 (HIGH): warm cache, L13, 0 fetches, no shift, new banner states cache path + tile count.
P5 (MEDIUM): first-ground-task dispatch deferral stays ~4-5 s as in D3.
MISS = any P1 failure; a completion outside +/-10% with no condition recorded to explain it; any member/taskee off D3's start; any runner-truth line absent or wrong. A P3 miss is a finding against the scripts merge, not a reason to patch mid-run.

### D6 RESULT (run 20260920T233339Z)

Conditions: no subagent, no generator/build/sim process (one idle dotnet build server only), baseline CPU 12-15%, ~3 h since the last teardown, rtiexec 51560 fresh at -n 3 with holder 12916 joined, 7 tiles.
P1 HIT. P3 HIT 7/7: Stage 0 input-source lines; inputs.inputSources (argument, fromEnv=[]); routeShift PREDICTED True and ANNOUNCED True (app banner), agreement CONFIRMED; the two legacy "vrfGui.log not found" WARNs GONE (vendor logs now captured BY PID); the false "no holder" line GONE (-FederationHeldByCaller); connection-config hash VERIFIED IDENTICAL. P4 HIT: new cache banner + SF3 tile-signature scrub line present, 0 fetches, L13, no shift. P5 HIT: first-task deferral +3.77 s vs D3's +4.89 s.
P2 MISS, cause ESTABLISHED: T_R5_CO1/114.MechCoy closed at +208.67 s vs D3's +289.10 s (-27.8%), same close rule (arrival evidence, vendor swallowed), caused by the de-stack SCOPE rule of commit 2746a0d - D3's "DeStack (R8): 4 units ... spread onto 700 m rings" vs D6's "DeStack (C14 scope): 3 unit(s) were NOT considered"; 63 of 86 tracked objects start elsewhere. (a) faster motion, (b) close-rule change, (d) poll artefact each FALSIFIED; (c) placement SURVIVES. THE PREREG WAS DEFECTIVE: the seat registered the scope change as "a no-op on R9" and predicted D3's numbers, believing de-stack was off in D1-D3; D3's own log shows it was ON - the miss is the seat's prediction error, not evidence against 248143f.
PART 2: D6 = one blob (platoon aggregates 24-56 m apart, 3 of 3 footprints overlapping, 137 vs 44 member pairs under 7 m) - VR-Forces' formation placement does NOT separate composed sub-aggregates. By the 2026-09-07 ruling's criterion (700 m > the 630 m company formation span) D3 SATISFIES and D6 VIOLATES; NO JAM in either run on kinematics (first movement within 5 s, 0 objects under 5 m displacement, nobody stranded; D6's 5 give-ups at +0.9 s were transient and all arrived, D3's 4 at +246.2 s were TERMINAL) - BUT objectConsoleNotifyLevel = -1 in both manifests, so the ruling's own P1 instrument was OFF and cannot be scored. The 80 s gap is the TRAVERSAL BAR, not the motion: at +208.67 s D3 had 42 of 57 members inside the radius but could COUNT only 23 - a sibling spread R from the parent stays countable only while R <= 0.5 x route, and 700 > 548. Verdict (c) a TRADE: D3 = an active countability defect + terminal give-ups; D6 = a latent violation of the ruling, unmeasurable at R9 scale.
OPTIONS put to the user 2026-09-20, NOT decided: A = traversal bar from each member's own distance-to-destination at dispatch (changes the ruled STP-837 rule); C = spread composed siblings at the echelon's own scale (~300 m for platoons; measured footprint radii 110-127 m); D = keep 2746a0d. Seat recommends A+C, confirmed by one R9 run with object consoles at 3. D6 is NOT adopted as the new R9 baseline pending that decision.
Anomalies: E1 did not recur; E5 fixed; E8 did not recur; E4 recurs (manifest omits the persistent holder); E6 recurs (no fetch-count/level logging); E3 withdrawn as a trend; NEW N2 (STP-837 false-negative surface) and N3 (manifest host.gitCommit records the working tree 70afafb, not the build 248143f). Unexplained: BdeHQ 8.7-12.3% slower than D3 on identical geometry; why five give-ups.

## D7 - R9 confirmation of the user's 2026-09-21 ruling (de-stack option C + arrival option A), object consoles at 3 (registered BEFORE the run)

Build: main adca180, app rebuilt 2026-09-21T01:17Z, bridge pin unchanged on all eleven consumers, 23 selftests (22 exit 0 + the fail-first arm), suite 397/0, exactly 6 warnings, CA2017 now a compiler ERROR (verified from the effective MSBuild WarningsAsErrors property, not just csproj text).
Changed vs D6's 248143f: composed SIBLINGS now spread at their echelon's formation scale (platoon 350 m, from the chain-resolved vendor span 320.9 m; company and above stay 700 m; Vrf:DeStackComposedSiblings=true) superseding the 2746a0d exemption; STP-837's traversal bar is now PER MEMBER (Vrf:ArrivalApproachFraction=0.5: max(min(0.5 x route, 0.5 x the member's own distance-to-last-vertex at dispatch), 100 m)); the 627fc07 log-arity fix.
COMMAND: D6's exact command with `--object-console 3 --member-console 3` passed EXPLICITLY - the wrapper's own defaults are 4/4, NOT off (D1-D6 passed -1). The interface applies the levels per object via SetObjectNotifyLevel; capture is the WatchVrf trace's CON rows, read with tools\analysis\console_narrative.py - never a vendor log. Volume expectation: order 100-200 MB for ~6 min (ASSUMED - level 3 has never been measured; the runner issues no volume warning).
CONDITIONS as in D6, plus: the seat starts a fresh rtiexec (-NotifyLevel 3) and arms a persistent holder BEFORE the run (there is currently none on the host), so Stage 2h JOINS.
P1 (HIGH), START GEOMETRY ONLY: at materialization (first POS row per member), 1143/1141/1142.MechPlt centroids sit ~350 m from 114.MechCoy at bearings 0/60/120 deg, pair separations ~350/350/606 m (tolerance +/-40 m, formation-placement noise); three DISJOINT platoon footprints judged against the MEASURED footprint radii (110-127 m in D6), NOT the 320.9 m span the table is derived from (two trailing columns can reach 641.8 m); all three taskees start exactly where D3/D6 did; the app's DeStack start-up line names the sibling group, the echelon, and 350 m. Later-time separations are REPORTED, not predicted - D3's own separations decayed from 690-1211 m at creation to 164-366 m by +150 s as the company formed up and moved, so a ">= 320 m at +30/60/100/150 s" clause would score a correct build as a MISS and is deliberately not required.
P2 (HIGH): the 2026-09-07 ruling's own jam instrument is SCOREABLE for the first time in this series - BlockedByVehicle / Loop to stall / Global Replan / Skirt row counts from the CON rows; a ZERO count is an INSTRUMENT MISS unless at least one level-3 CON row is present in the trace; predicted: no sustained jam (first movement within ~10 s for every member, no member under 5 m displacement at +60 s).
P3 (HIGH): 3/3 terminal, all by arrival evidence; T_R5_CO1's offline countability is 48/48 under BOTH rules with per-platoon bars 381.6/493.0/556.6 m; its completion falls between D6's +208.7 s and D3's +289.1 s with 10% slack on each end, i.e. [187.8 s, 318.0 s]; the other two tasks within +/-10% of D6's stamps.
P4 (MEDIUM): FEWER give-ups than D6's 5-at-materialization (0-3) and NONE terminal at the destination as in D3 - the builder's reasoning: the obstruction was the three platoons materializing on one coordinate, which the 350 m spread removes.
P5 (HIGH): runner truth 7/7 as D6; clean teardown; no BACK END LOST; no WS alerts; console volume does not break the run (trace size recorded).
LIMIT REGISTERED IN ADVANCE: on R9 lean, A and C are CONFOUNDED in any completion-time number (C alone removes the collision; A only lowers two platoons' thresholds by ~38 s and ~14 s of travel) - one LIVE run cannot separate them; the control is Vrf__ArrivalApproachFraction=0, NOT run unless the seat registers it. They CAN be separated OFFLINE: replay D7's own WatchVrf trace through the arrival rule at ArrivalApproachFraction 0 and 0.5 (the reconstruction's calibration error cancels in the difference) - this replay is part of D7's harvest. Runner gap: no prediction/announce line exists for DeStackComposedSiblings or ArrivalApproachFraction, so P1/P3 are scored from the app's own log.
MISS = a taskee starting elsewhere; overlapping sibling footprints; a sustained jam; any task not terminal; or T_R5_CO1's completion outside [187.8 s, 318.0 s].

### D7 RESULT (run 20260921T013902Z)

Conditions at launch: no subagent live; one idle VBCSCompiler; baseline CPU ~10%; 92 GB free; ~2 h since the previous teardown; rtiexec 46960 fresh at -NotifyLevel 3 with persistent holder 25484 joined.
Instrument ON: 21,269 CON rows (19,682 at level 3) vs D6's 5; 77 of 82 objects acknowledged level 3; trace 8.9 MB / app log 3.4 MB - the prereg's 100-200 MB estimate was 11-23x too high; no cost (deferrals +3.13/3.46/3.46 s, the fastest of the three runs).
P1 MISS on ONE limb, HIT on the rest: siblings at 354.0 m @ 1.4 deg / 312.3 m @ 64.0 deg / 347.4 m @ 121.4 deg, pair separations 318/348/607 m (all inside +/-40 m), 3 of 3 footprints DISJOINT (D6: 3 of 3 overlapping), crowding NN median 3.6 m and 50 pairs < 7 m (D3 4.3 m/44; D6 1.3 m/137) - C restores D3's separation at less than half the spacing. THE MISS = N4: the company TASKEE's PUBLISHED position moved 262 m because VR-Forces publishes a composed aggregate at its members' CENTROID, and a 0/60/120 deg ring moves that centroid 233 m by construction, so the route origin moved: L 1,112 -> 1,039.3 m, radius 278 -> 260 m, route bar 556 -> 520 m (the build report's offline model assumed the parent stays put - wrong by 74 m of route).
P2 HIT - the 2026-09-07 ruling's jam instrument is scoreable for the first time and it PASSES: BlockedByVehicle 3 rows / 2 objects against the ruling's 4,828-in-300 s co-located reference, Loop to stall 0, Global Replan 0; the 2,324 "Skirt" rows are BT node-entry traces (+0.7..+54.9 s), volume not a jam signal; kinematics: first 10 m within 4-8 s for all 57 objects, 0 under 5 m displacement at any checkpoint.
P3 MISS: T_R5_CO1 closed at order +168.669 s, 19.1 s below the registered [187.8 s, 318.0 s] floor - STATED PLAINLY as a PREDICTION-CONSTRUCTION ERROR BY THE SEAT (the interval was built from the baselines D6-10% .. D3+10% and EXCLUDED the design's own point prediction ~171 s, which was HIT to 1.4%), not a finding against adca180; T_R5_TK1 -11.7% vs D6.
The registered A-vs-C split (offline replay validated to -0.2 s on D7, -4.6 s D6, -3.9 s D3): on D7's own trace rule A moves the close by 0.0 s (only 8 of 48 members have d0-radius < route bar, against a quorum of 25); on D3's trace A alone = -90 s; splitting the raw -39.4 s vs D6: A 0.0 s, run-to-run rate ~-18 s (control = 1222.MechPlt reaching an identical evidence state at 145.1 s vs 159.3 s, factor 0.911), C ~-21 s (range -15..-27 s) - NEVER QUOTE THE -40 s RAW. Caution: half of C's gain here is one platoon starting 354 m nearer because the route runs due north and ring slot 1 points north - on the opposite orientation it is a handicap of the same size, untested.
P4 MISS on both limbs: 4 give-ups, all TERMINAL at the destination, D3's exact roles.
P5 HIT 7/7.
NEW N7 (a correction): "sim/wall 1.00" was NEVER measured - the app's "Ns after dispatch" is wall minus wall (VrfC2SimService.cs:4979); D7's level-3 BT rows carry a sim clock advancing 3.00x the wall clock (stable 2.99-3.09), the unit INFERRED not documented - so D1-D7 most likely ran at ~3x, not real time. Corroboration: D1's brigade-HQ M577 proxy "averaged 96 km/h" is 32 km/h at 3x. Every earlier "real time / ratio 1.00" statement in this file is therefore UNVERIFIED and must be read as such.
NEW N5: the console request lists 64 members including 16 duplicate uuids; arrival de-duplicates to 48 - a log defect only. NEW N6: "Mismatch in subs and formation slots; unit index -1, form size 7" x441 on the company aggregate, not attributed, no control run exists. N3/E4/E6 recur.
UNEXPLAINED: the untouched units ran 8-9% faster than in D6, concentrated in the first ~200 m; N6's cause.

## D8 - R9 on main 1d0fb69: symmetric sibling ring, rule-A loop guard, FIRST run with a measured sim clock (registered BEFORE the run)

Build: main 1d0fb69, app rebuilt 2026-09-21T04:31:12Z, deployed dll stamped 1.0.0+git.1d0fb69.Release-5.2 (no +DIRTY), bridge pin unchanged on all eleven, 23 selftests (22 exit 0 + the fail-first arm; 1,914 offline checks), runner suite 425/0 with the persistent holder alive, 6 warnings, CA2017 an error, cold-start review: no blocker.

Changes vs D7's binaries adca180: composed siblings on ONE ring at equal bearings 360/N with radius spacing/(2 sin(pi/N)) (N=3: 202.1 m at the 350 m platoon spacing) so the children's centroid stays on the parent; rule A relaxes per member only when the taskee's start-to-last-vertex distance >= 0.5 x route (APPLIED on all three R9 tasks, ratio 1.00); sim time logged beside WALL time + SIM/WALL RATIO once a minute (DtVrfRemoteController::simTime - the back end's SCENARIO clock, seconds, absolute, -1.0 with no back end, a cached non-blocking read now made unconditionally at 1 Hz); elevation level + tile hits/fetches logged; manifest records holders-at-launch and the DEPLOYED build identity; announce-vs-predict for DeStackComposedSiblings and ArrivalApproachFraction.

COMMAND: D7's exact command (consoles at 3, passed explicitly). CONDITIONS to record at launch as in D7 (no subagent live, no stray build/generator/sim process, baseline CPU, time since the previous teardown, rtiexec pid + notify level, persistent holder pid).

P1 (HIGH) START GEOMETRY ANCHORED ON THE ROUTE, NOT ON THE PUBLISHED POSITION (review SF-H: in D7 the published-at-dispatch sample sat 33.90 m from the three-child centroid while the ROUTE ORIGIN matched the model to 0.35 m): T_R5_CO1's route length 1,110-1,116 m, radius ~278 m, route bar ~556 m (D7: 1,039.3 / 260 / 520; D3/D6: 1,112); children at bearings 0 / 120 / 240 deg (+/- 10 deg), ~202 m from the parent (+/- 40 m), pair separations 350 m (+/- 40 m), 3 of 3 footprints DISJOINT against the measured radii; all three taskees' authored positions unchanged; the taskee's PUBLISHED position vs authored is REPORTED, with an expectation of tens of metres not hundreds (D7: 262 m) - NOT a pass/fail line.
P2 (HIGH) jam instrument stays PASS: BlockedByVehicle < 500 rows (D7: 3), Loop to stall 0, Global Replan 0, every member moving within ~10 s, none under 5 m at +60 s - registered RISK: the ring is now 202 m not 350 m from the parent, so crowding metrics may rise toward D6's; report NN median and pairs < 7 m against D3 4.3 m/44, D6 1.3 m/137, D7 3.6 m/50.
P3 (HIGH) 3/3 terminal by arrival evidence with "relaxation APPLIED" on all three; T_R5_CO1 closes LATER than D7's +168.7 s (longer route, no platoon pre-positioned 354 m up the route: the model has one platoon ~202 m nearer and two ~101 m farther) and not later than D6's +208.7 s by more than 10%: interval [165 s, 230 s] WALL after the order, MEDIUM confidence - and this time the interval CONTAINS the design's own point expectation (~190-205 s); the two untouched tasks within +/-12% of D7's stamps (run-to-run rate has been +/-9%).
P4 (HIGH) THE CLOCK: the app logs SIM/WALL RATIO lines and sim time on dispatch and completion; the fixture's frame mode is fixed-frame-run-to-complete (frame-time 0.033333) which predicts "as fast as the load allows", so the registered expectation comes from D7's console-derived 3.00x: ratio in [2.5, 3.5] over the movement window; a ratio near 1.0 REFUTES D7's N7 and re-opens the "real time" question; whatever the number, D8 is the first run where it is MEASURED. No "Tick phase SampleTaskClock FAILED" line; first-task dispatch deferral not above D7's 3.5 s by more than 2 s (the unconditional sim read is new).
P5 (HIGH) runner truth 7/7 plus the NEW lines: holders-at-launch names the persistent holder, the manifest carries the deployed build 1d0fb69, announce-vs-predict agreement for both settings, elevation level L13 with tile HITS > 0 and FETCHES 0; clean teardown.
P6 (MEDIUM) give-ups: report count/roles/time; D3 and D7 had 4 TERMINAL at the destination, D6 5 transient at the start - no prediction of which, stated as such.
MISS = route length outside 1,110-1,116 m, overlapping sibling footprints, a sustained jam, any task not terminal, T_R5_CO1 outside [165, 230] s, no SIM/WALL line, or a ratio outside [2.5, 3.5] (the last is a finding about N7, not about the build).


D8 ADDENDUM (2026-09-21 05:25Z, registered BEFORE the re-launch): the first D8 launch (run 20260921T045700Z, 04:57Z)
never reached VR-Forces - Stage 2h refused it after four holders (appNos 5036-5039) that had ALL JOINED went
unrecognised: rtiexec 47636 writes its log through a doubled / token-interleaved sink ('Federate Federate
remoteControl 87404 ... has joined federation " has joined federation "MAK-ONE-2025MAK-...'), which the join
detector's exact-quote regex could not match (the regex was unchanged by 1d0fb69 - verified). Fixed in scripts only
(merge 9711f46: shared matcher Test-HolderJoinedInLog, suite 437/0); the APP BINARIES ARE UNCHANGED (1d0fb69, built
04:31:12Z). The re-launch is the same registered run, on the SAME rtiexec instance so the fix is exercised against
the log that broke it; P1-P6 unchanged. Added expectation (HIGH): Stage 2h recognises its holder's join on
attempt 1 from a garbled line.

### D8 RESULT (run 20260921T052350Z, the re-launch)

P1 HIT on every limb: route 1,112.0 m / radius 278 m / bar 556 m; children at 0/120/240 deg (+/-0.3 deg) at 200.9-202.3 m; pair separations 348-349 m; 3 of 3 footprints DISJOINT; both taskees' authored positions unchanged; the composed taskee's PUBLISHED position at dispatch is 0.0 m from authored (D7: 262 m) - SETTLED: VR-Forces publishes a composed aggregate at the centroid of its DIRECT CHILDREN, not of all leaf members (the two are indistinguishable at rest, which is why the symmetric ring fixes the route origin); corollary: once moving, the company's published position runs ahead of its own fleet by up to ~260 m.
P2 HIT: BlockedByVehicle 11 rows / 3 objects (D7: 3; ceiling 500), Loop to stall 0, Global Replan 0, everyone moving by 6.6 s, 0 of 57 under 5 m at +60 s; the registered crowding RISK did not materialise - NN median 4.7 m and 46 pairs < 7 m, at or better than D7's 3.6 m / 50 despite halving the ring radius 350 -> 202 m.
P3 HIT: T_R5_CO1 closed at order +201.406 s, inside the registered [165, 230] s and inside the design's own 190-205 s point band; T_R5_TK1 -5.8% and T_R5_PL1 +1.8% vs D7. All three closed on arrival evidence with "relaxation APPLIED" and the vendor completion swallowed; rule A again moved the close by 0.0 s (16 of 48 members relieved, quorum crossing unaffected) and the SF-1 loop guard bit nothing on this fixture, as predicted.
P4 HIT on the registered quantity (movement-window ratio 3.362; per-task 2.625/3.328/3.365; independent console-clock cross-check 3.436) but MISS on the MISS clause read literally - two of the four logged 60 s windows lie outside [2.5, 3.5] (3.538, a PURE movement window, and 4.090, 54% post-completion idle). MISS-CLAUSE WORDING DEFECT (the prereg's, third of its kind after D6 P2 and D7 P3): the clause never named which window it bound. THE FINDING (N11): the sim/wall ratio is NOT a constant, it is LOAD-DEPENDENT and monotone - 2.63x at peak load, ~3.0x through the first movement minute, 3.4-3.5x as tasks close, ~4.7x post-completion idle, 3.5x pre-order; a future band must name its phase. D7's N7 is REFINED, not simply confirmed: the console's leading number IS the back end's scenario clock in seconds (app simTime 43.4 s vs console 43.8 s at the same instant, unit now SETTLED), but "3.00x" was only the loaded-movement rate. No STOP on build 1d0fb69 - the prereg itself says a ratio miss outside the band is a finding about N7, not about the build, and no patch is made to the band after the fact.
P5 HIT: runner truth 7/7 plus every new line - holders-at-launch names the persistent holder (pid 74612), the manifest carries the deployed build 1d0fb69 (not dirty), both announce-vs-predict lines CONFIRMED in first live production (DeStackComposedSiblings, ArrivalApproachFraction), elevation level L13 with tile HITS 2 / FETCHES 0 on the registered limb; clean teardown, RTI preserved.
P6 reported, not predicted: 4 give-ups, all TERMINAL 41-45 m short of the destination, D3's and D7's exact roles (1141.MechPlt dismounts), firing 40 s AFTER T_R5_CO1 closed - the ring did not change this, third time in four runs.
ADDENDUM MET AND EXERCISED, not merely met-untested: holder pid 57200's own rtiexec join line WAS garbled on this instance; replayed over all six holder pids in the log, the OLD regex matches 0 of 6 and the NEW matcher (Test-HolderJoinedInLog, merge 9711f46) matches 6 of 6 - including the four holders the first launch wrongly refused.
Completion in SIM seconds (exact, the app's own clock): T_R5_TK1 81.1, T_R5_PL1 485.5, T_R5_CO1 660.5 s; D7's T_R5_CO1 (its only other measured run) 560.5 s, a +17.8% gap that is REAL and geometric, matching the +18.8% WALL gap - not a clock artefact. D1-D6's completions in sim seconds are DERIVED via a 3.0-3.4x band and must be labelled so wherever quoted; on that band D8's 660.5 s and D6's derived 614-696 s are indistinguishable.
Option C (the symmetric ring) buys geometric correctness only - published position 0.0 m, N4 FIXED, 3/3 disjoint footprints, crowding did not rise - and NO speed: +28 s (range +27..+30) vs D7 from geometry alone (D7's speed came from fixture luck, one platoon parked 354 m up a due-north route by the old hex ring), and about +9 s (range +6..+12) vs D6 at equal rate. Nobody should quote a completion-time gain from option C; rule A's contribution is 0.0 s again.
N5 FIXED as a log defect: the member line now reads 48 DISTINCT members (64 published, 16 duplicate uuids from VrfFacade::collectMembers' un-deduplicated depth-3 recursion, 0 with no uuid) - the facade's own duplication is unchanged and still un-ticketed as a native fix.
N6 RECURS, slightly up: "Mismatch in subs and formation slots; unit index -1, form size 7" now 500 rows (D7: 441) on 114.MechCoy~PXY at level 2; still unattributed, still no control run with consoles on and de-stack off; the count barely moved between a hex ring and a symmetric ring, so the ring is not the cause.
N8 (new): the tile census fires ONCE, immediately after the FIRST task's preflight, and its "cumulative" figure (2 hits, 0 fetches) is therefore not a run total - the other two tasks' tile resolutions at L13 are never counted, though the registered limb (HITS > 0, FETCHES 0) is still satisfied.
N9 (new): the runner's Stage 2h "rtiexec log:" quote in the HELD line is a CONSTRUCTED string (RunC2SimScenario.ps1:3956), not the matched log line - on a garbled instance it prints a clean-looking sentence that masks the very garble the ADDENDUM's fix targets; it should quote the matched text verbatim (truncated) instead.
N12 (new, a HYPOTHESIS, not a claim): D7's unexplained "untouched units ran 8-9% faster than D6" may be a SIM/WALL RATIO difference between hosts (D6/D3 ~3.1x vs D7/D8 ~3.4x) rather than a units effect - on the sim clock D8 and D6 are indistinguishable. NOT VERIFIED (D3/D6 have no sim clock to check this against); falsifier: a D6-equivalent run on 1d0fb69 with the clock logged - a movement-window ratio of 3.4 kills the hypothesis.
UNEXPLAINED (from the harvest's closing paragraph): (i) the dispatch deferral grew 1.6-1.7 s over D7, back to D3's level - inside the registered 5.5 s limit but not diagnosed, candidates (the new unconditional 1 Hz sim read, the per-task terrain-profile round trip, host state) not separated; (ii) N6's 500 mismatch rows, still no control run; (iii) why the pre-order idle phase (3.5x) runs SLOWER than the post-completion idle phase (4.7x) when both have nothing moving - plausible (init work, terrain queries, 57 object creations) but untested.

## D4 - the audience scenario (COA-STP1's 11 taskees, GUI, real-time, route shift) - needs the user's rulings first.
## D5 - Way B, hand-started and STP-driven, with the LaunchVrf52 holder (lane feat/demo-federation-holder).
