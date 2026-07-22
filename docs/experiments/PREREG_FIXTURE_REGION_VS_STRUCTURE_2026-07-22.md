# PREREG: authored-fixture region-vs-structure probe (2026-07-22)

STATUS: REGISTERED, AWAITING USER GO. Written BEFORE any live run per the standing
probe discipline (SUPERVISED_RECOVERY_PLAN evidence rules: single variable,
prediction + falsifier on record first). Fixture build + offline gates:
docs/experiments/MOJAVE_FIXTURE_2026-07-21.md. Design + confound analysis:
docs/HANDOFF_2026-07-21_SESSION_JUMP.md "NEXT ACTION". ASCII only.

## 1. Question and the single variable

Our C2SIM-created units freeze at Mojave (R9: 0 member offset routes vs 45 at
Sweden). Does a STRUCTURALLY-COMPLETE, AUTHORED disaggregated Tank Platoon (USA)
move at Mojave? R9 never controlled authored-vs-remote structure.

The two fixtures (TankPltFixture_Sweden.scnx / TankPltFixture_Mojave.scnx, both
verified on disk 2026-07-21 21:11, 28324 / 28328 bytes) are structurally identical
by construction (one generator run, offline-gated) and differ ONLY in location:
- Sweden 58.702956, 16.499229 (R9 golden start where 1222.MechPlt MOVED)
- Mojave 34.612956, -116.600487 (R9 start where the same unit FROZE)
LOCATION IS THE SINGLE VARIABLE.

## 2. Procedure (validated pipeline; ZERO code changes; multiplier 1)

Pre-flight (done at prereg time, 2026-07-22 morning): no VRF/RTI processes running
(fresh boot); ~48 GB free on C:; marker = 3553; port repo clean at 7a9eb49.

Per fixture (Sweden FIRST - it is the GATE):
1. Ledger 4 fresh appNos from the "*** NEXT FREE:" marker BEFORE any join
   (backend, frontend, WatchVrf, RunSim). Update the marker immediately.
2. pwsh -File scripts\LaunchVrf.ps1 -Scenario TankPltFixture_<loc>
   -BackendAppNumber <a> -FrontendAppNumber <b>   (returns READY; scenario PAUSED)
3. Start WatchVrf.exe <c> 360 5 CWIX-2024 (cwd=bin64, background). Let it record a
   PAUSED window of >= 60 s (the static control, same signal as the Hawaii baseline).
4. RunSim.exe <d> CWIX-2024 (cwd=bin64; NO multiplier argument = real time; RunSim
   verified in source: multiplier defaults to 1 and SetTimeMultiplier is NOT issued).
5. Observe >= 240 s of run time (route is 300 m eastward; ample at 1x).
6. StopVrf.ps1; archive trace + vrfSim/launch logs under
   runs/2026-07-22_fixture_region/<loc>/.
7. ANALYZE SWEDEN BEFORE LAUNCHING MOJAVE (gate rule below).

## 3. Observables, ranked (POS distances are NOT trusted)

- O1 (load-bearing): per-uuid static-while-paused -> moving-after-RunSim transition
  for the 4 M1A2 members (net displacement >= 50 m sustained over >= 3 samples).
  The TRANSITION is the signal; per-unit distances are DR-contaminated (2026-07-21
  baseline: multi-km single-step teleports) and are NOT measurements.
  TWO-CHANNEL RULE (the standard that made the 2026-07-19 frozen verdicts solid):
  WatchVrf emits both POS and RPT. Verdict MOVED = EITHER channel shows the
  transition; verdict FROZEN = BOTH channels agree static. A POS-only or RPT-only
  transition is recorded as MOVED-with-channel-disagreement, feeding the open
  oracle-contradiction item, not silently averaged.
- O2 (mechanism): backend/launch log evidence - offset-route construction vs the R9
  signature "moveAlong() - empty route -- not sending move along to subordinate".
- O3 (diagnostic only): loader acceptance errors, plan/task status lines in logs.

## 4. Pre-registered predictions and branch table

P-GATE (Sweden): the 4 members transition static -> moving within 120 s of RunSim.
If P-GATE FAILS: STOP. Do NOT run Mojave (uninterpretable). Diagnose per handoff:
plan-name == aggregate uuid binding, task in top-level Block, state-repo
cleanliness, loader log. That is the "benign non-start" branch, not a region finding.

Branches (registered before the run):
- A. Moves-Sweden / freezes-Mojave => REGION/terrain-specific empty-offset-route
  cause. Strongest evidence yet; draft the MAK support question with this repro.
- B. Moves-both => authored structure works at Mojave. NOT yet proof that
  remote-created structure is deficient: fixture waypoints are above-terrain by
  construction. Run the confound control: regenerate the fixture with the SAME
  order-derived waypoints the C2SIM units had (R9_Mojave_UnitMove_Order.xml legs),
  re-run offline gates, run at Mojave (same session if user GO covers it).
- C. Freezes-both (gate fail) => fixture defect or benign non-start; diagnose
  auto-start binding + state-repo BEFORE any conclusion about tank-platoon moves.
- D. Sweden freezes / Mojave moves (inversion) => treat as gate anomaly; diagnose;
  no region conclusion.

Falsifiers: the REGION hypothesis is falsified by Branch B (fixture moves at
Mojave). The STRUCTURE hypothesis is falsified by Branch A (authored-complete
structure still freezes at Mojave).

What this probe CANNOT settle (do not over-claim in the writeup):
- The POS-vs-RPT direction contradiction (open; frozen verdicts unaffected).
- Whether the C2SIM units' order-derived waypoints are the trigger (that is the
  Branch-B follow-up variant, not this run).
- Anything about completions/arrival gating or the 18.1-18.4 km band (route 300 m).

## 5. Risks and mitigations

- R1 Teardown modal: the descendant-scan StopVrf fix SHIPPED but UNVERIFIED by a
  real occurrence. If StopVrf hangs: never force-kill joined federates / RTI
  processes; follow RUNBOOK; a failed teardown does not invalidate the trace.
- R2 Fresh boot: rtiAssistant not yet running; first launch spawns it; the
  "always use this connection" answer is persisted per machine (verified
  2026-07-18). If an RTI dialog blocks anyway => launch failure, not a result.
- R3 Disk fill killed the observer on 2026-07-21 (trace2.err). ~48 GB free now;
  check free space between runs.
- R4 Wrong cwd burns the appNo (3550 lesson): every tool invocation cwd=bin64.
- R5 Loader rejects the authored .scnx: that is Branch C territory (fixture
  defect), visible in launch/backend logs; not a region finding.
- R6 UTC-vs-local timestamps (standing gotcha): tools stamp UTC, machine is local
  (-0400); Get-Date before comparing.

## 6. appNo budget (from marker 3553)

8 for the two planned runs (4 per fixture), expected 3553-3560. LEDGER PER RUN,
4 at a time, immediately before that run's first join - do NOT pre-ledger the whole
session. The Branch-B variant (if it fires) ledgers its own 4 at that point.
Standing rule unchanged: once ledgered, a number is consumed - unconsumed ledgered
numbers are BURNED, never recycled (that is WHY ledgering is per-run and late).

## 7. Outcome record (filled AFTER the run)

### Sweden gate (P-GATE): PASS (2026-07-22, appNos backend 3553 / frontend 3554 /
WatchVrf-run2 3558 / RunSim 3559; run 1 WatchVrf 3555 captured a 360 s PAUSED-only
baseline because RunSim had not yet started the clock - see the burns note below).
Artifacts: runs/2026-07-22_fixture_region/sweden/ (watch.trace = paused baseline;
watch2.trace = the movement run; runsim2.out).

RESULT: the authored Tank Platoon MOVED, and its settled endpoints MATCH the 300 m
eastward FixtureRoute - a stronger pass than a bare static->moving transition.
- BIT-STATIC while paused: aggregate 5a3ca430 and all 4 M1A2 members held identical
  coordinates from t=3 to t=28 (and across the entire separate 360 s paused run).
- MOVED once RunSim issued run() (clock start ~t=30): motion begins at t=33.
- The disaggregated-move MECHANISM engaged: 4 NEW transient objects appeared at
  movement onset (reflected 9 -> 13) = the per-member offset-route/control objects,
  i.e. the exact buildOffsetRoute path R9 found EMPTY at Mojave (0 offset routes).
- SETTLED endpoints (stable t=73.5..88.5, 15+ s unchanging):
    aggregate 5a3ca430: 16.499229 -> 16.504496 lon (+0.005267 = ~305 m EAST)
    M1A2 1 70b8d836:     16.499229 -> 16.504118 lon (~283 m EAST)
  FixtureRoute = 300 m eastward. Endpoints are route-consistent to within DR noise.
- DR WARP artifacts present intra-move exactly as documented (M1A2 1 swung to lon
  16.216 alt 88 m then back) - observer-side DR extrapolation, NOT the settled state;
  per the prereg, distances are not measurements, endpoints/transition are the signal.
- Channels: POS showed the move; RPT emitted 5 early POSITION reports (t=31/33.9) near
  the start points. No channel disagreement of the 2026-07-19 kind arose (that was a
  frozen-vs-moving direction dispute; here both are consistent with an eastward move).
- Loader accepted the authored .scnx cleanly (vrfSim.log: AR Plt 1 registered as
  AggregateEntity(6); M1A2 1-4 + FixtureRoute all "Locally Simulated" from
  C2simEx/EntityLevel). Retires the Branch-C "malformed file" risk for this fixture.

CONSEQUENCE: the fixture is VALID and the positive control holds. Mojave is now
interpretable. Proceeding to the Mojave leg.

BURNS this session (ledger OPUS_EXECUTION_PLAN.md Appendix B): 3556 (RunSim usage
error, federation in multiplier slot, exit 2 pre-join); 3557 (RunSim wrong cwd, not
bin64 -> CouldNotOpenFDD, the 3550 lesson repeated). LESSON RE-CONFIRMED: RunSim MUST
run with cwd=bin64; invoking via `& $exe` inherits the caller cwd - use Set-Location
$Bin64 or Start-Process -WorkingDirectory.

### Mojave result: BLOCKED - RTI infrastructure wedged, NOT a region/structure result.
The Mojave leg could not be scored because the movement ORACLE went blind. This is an
infrastructure failure introduced by the teardown-relaunch cycle, and it is
DECONFOUNDED from the fixture question (see falsification below).

Sequence (appNos 3560 backend / 3561 frontend / 3562 WatchVrf / 3563 RunSim, all
attempt 1; 3564 probe; 3565/3566/3567 attempt 2):
- LaunchVrf -Scenario TankPltFixture_Mojave READY, backend healthy, fixture LOADED
  (vrfSim.log: "Joined federation CWIX-2024"; AR Plt 1 f0be86a8 + M1A2 members
  registered; established TCP to rtiexec:6003 + rtiForwarder:4001). Backend IS joined
  and publishing.
- BUT every observer federate is BLIND: WatchVrf 3562 reflected=0 for 154 s; a FRESH
  30 s probe 3564 reflected=0; RunSim 3563 discovered BackendCount=0 in 15 s (run()
  NOT sent). A clean StopVrf+relaunch (attempt 2, 3565/3566) + oracle pre-check 3567
  reflected=0 for 40 s. reflected=0 means the observer sees NOTHING - not even the 3
  location-independent global env objects that were reflected=9 at Sweden.
- The preserved rtiForwarder (pid 50572, spawned 07:29 at boot) sits at 1 thread (it
  was 4 during the working Sweden run). rtiexec/rtiForwarder are the ORIGINAL
  boot-spawned processes; StopVrf preserves them (non-negotiable), so the relaunch
  reuses the same wedged forwarder.

FALSIFICATION (adversarial): the ONLY thing that differs from the working Sweden run
and could blind the observer is FRESH RTI infra (Sweden: rtiexec/rtiForwarder spawned
seconds earlier when the boot dialog was answered) vs PRESERVED-after-teardown RTI
(Mojave). Competing hypotheses tested and FALSIFIED: (a) fixture/location - a
coordinate value cannot blind an observer to the BACKEND and the GLOBAL objects, and
the identical fixture-family moved at Sweden; (b) dead/unjoined backend - the log +
TCP prove it joined and registered objects; (c) discovery latency - 154 s and a fresh
2.5-min-late probe both at exactly 0, vs Sweden's 0.2 s. The single observation that
would falsify the wedged-RTI hypothesis is a fresh RTI restart making Mojave observers
discover - untested because it collides with the "NEVER kill rtiexec/rtiForwarder"
non-negotiable (a USER DECISION, escalated 2026-07-22).

NEW OPERATIONAL FINDING (the real deliverable of this leg): the per-fixture
LaunchVrf-per-scenario pipeline the handoff assumed does NOT survive a StopVrf teardown
between fixtures on the rtiexec loopback connection - the forwarder wedges and every
later observer goes blind, and a relaunch does NOT clear it. FRESH-BOOT RTI worked
(Sweden). Implication for method: to run a SECOND fixture in one session either
(i) restart the RTI stack (needs the non-negotiable relaxed), (ii) reboot and run the
target fixture FIRST, or (iii) find a no-teardown scenario-swap (remote loadScenario -
but that changes the load method, a confound vs the launcher-loaded Sweden run).

### Branch selected: NONE - Mojave uninterpretable (oracle blind). Sweden gate PASS
stands on its own as the positive-control result. Region-vs-structure at Mojave remains
OPEN pending a fresh-RTI Mojave run.
### Deviations from procedure: 3556/3557 burned (above); WatchVrf run 1 was
paused-only (RunSim not yet corrected); LaunchVrf reported EXIT=3 because its readiness
poll expired while the fresh-boot RTI "Choose RTI Connection" dialog blocked - answered
once via DPI-aware coordinate click (physical rect 573x583, Connect at doc ratio
0.668/0.949), after which backend went healthy (66 threads) and the fixture loaded.
The dialog is a known once-per-boot bring-up step (scaffolding, not product).
