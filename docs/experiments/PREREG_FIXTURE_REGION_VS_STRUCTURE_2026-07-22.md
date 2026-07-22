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

## 7. Outcome record (filled AFTER the run - empty at registration)

- Sweden gate result:
- Mojave result:
- Branch selected:
- Deviations from procedure:
