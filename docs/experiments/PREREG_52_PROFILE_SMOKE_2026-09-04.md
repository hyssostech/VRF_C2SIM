# PREREG - first run of the runner's 5.2 profile on the documented posture (smoke)

Date 2026-09-04. Tier STANDARD (no new cause claim; it exercises the profile as built
by the executor and closes the sim-side half of the --deviceAddress question).
Predecessors: PREREG_52_RTIEXEC_2026-09-04.md (posture: MAK RTI 5.0.1 in rtiexec mode,
config/rid-501-rtiexec-min.mtl, headless rtiexec; --deviceAddress not required observer-
side, P4), COLDSTART_REVIEW_RTIEXEC_2026-09-04.md (crash trigger unknown - the launch
stage must detect it), RESEARCH_52_HLA_CONNECTION_CONFIG_2026-09-04.md.

## 1. What is exercised
Not the C2SIM pipeline - no 5.2 fixture exists yet (DIFF C1 / Y-7: R9 and COA fixtures
must be re-authored on a 5.2 terrain; the runner's init/order stages need them). This
smoke runs the profile's INFRASTRUCTURE stages in the runner's own order: StartRtiExec52
(idempotent against the rtiexec 15720 / forwarder 43728 already running), LaunchVrf52
WITHOUT -DeviceAddress on Sample\Traffic, the RTI probe, the WatchVrf pre-check, then
the stop stage - whatever subset the runner exposes without fixtures (a -SmokeOnly /
-DryRun-then-live path, or the stages invoked directly with the profile's env). Every
run tee'd to runs/launch52/; app numbers ledgered before each join.

## 2. Predictions
P1 HIGH: StartRtiExec52 reports READY without starting a second rtiexec (pids unchanged,
   TCP 127.0.0.1:4001 LISTEN owned by 43728). Falsifier: a second rtiexec/forwarder
   spawns -> the idempotency check is wrong; STOP (an extra exec on the same ports is a
   federation hazard).
P2 MEDIUM: the sim launched WITHOUT -DeviceAddress joins MAK-ONE-2025 and loads Traffic
   (no parseCmdLine crash; if the crash strikes - ~2/5 so far - the new detection fails
   the stage with the callstack frames, exit 3, and the run is REPEATED ONCE with fresh
   numbers; two crashes in a row = STOP and record).
P3 MEDIUM: WatchVrf --diag reflects >= 1 entity within 60 s with the sim launched
   without -DeviceAddress -> the sim-side half closes: --deviceAddress is not required
   anywhere on 5.2. Falsifier: ent=0 for 60 s with backends=1 -> required SIM-side only;
   the profile default flips back to 127.0.0.1 and the record says why.
P4 LOW (record): thread-count baseline of the 5.2 sim under the profile; whether
   HaveRtiLicense()=1 holds with sim + probe + observer joined (3 federates).

## 3. Procedure
0. Executor's gates re-run by the seat: 5.0.2 -DryRun diff empty; 5.2 -DryRun shows the
   posture; StartRtiExec52 -DryRun; builds; tests.
1. Ledger the numbers. 2. Run the smoke path. 3. Fill sec 4. 4. Commit.

## 4. Result (2026-09-04 07:15 local; runs/launch52/startrtiexec_smoke.txt, launch_3858_smoke.txt,
vrfSim_3858_*.log, watchvrf_3859_smoke.txt)
Seat re-ran the executor's gates first: 5.0.2 -DryRun HEAD-vs-tree diff 0 lines; 5.2 -DryRun exit
0, 0 FAIL markers, Stage 2r present, LaunchVrf52 called WITHOUT -DeviceAddress; StartRtiExec52
-DryRun 0 and C:\MAK refusal 2; both app builds 0 errors; tests green; ASCII/CRLF clean (5cea2ed).
P1 HELD: StartRtiExec52 live -> ALREADY UP, rtiexec=15720 forwarder=43728, TCP 127.0.0.1:4001
LISTENING (owner 43728), nothing started; pids unchanged after.
P2 HELD: sim 3858 on the profile defaults (5.0.1, rid-501-rtiexec-min, no --deviceAddress) READY at
16 threads, joined MAK-ONE-2025, Traffic loaded, no crash (the detector armed, not triggered).
P3 HELD: WatchVrf 3859 (device-address=default, i.e. the observer's own 127.0.0.1) reflects
ent=44 / env=19 / ctl=19 from t=3 s, vendor count Reflected Entities 44 / Total 63, 264 real POS
lines - with the scenario PAUSED (no RunSim this run): the sim-side --deviceAddress is NOT
required either. POSTURE FINAL = MAK RTI 5.0.1 in rtiexec mode (rid-501-rtiexec-min.mtl +
headless rtiexec); --deviceAddress is a tunable with no observed effect on 5.2.
P4: 5.2 healthy thread baseline under the profile 16 (ready poll) -> 73 (scenario loaded);
licence rti=1 vrlink=1 with 3 federates (sim, rtiexec's own, observer) - the 4+-federate cap
check is still owed to the first full-profile run.
Adversarial review: the smoke exercised infrastructure only - no C2SIM init/order stages ran (no
5.2 fixture exists, DIFF C1/Y-7); "profile works" means the stages up to observation. The
"paused sim reflects 44" observation retires every clock-gated hypothesis from 2026-09-03.
Unexplained residue: env=19 == ctl=19 on every sample (ReflectedCounts env accessor anomaly,
still open); the parseCmdLine startup crash did not recur (0/2 today) - trigger still unknown.
