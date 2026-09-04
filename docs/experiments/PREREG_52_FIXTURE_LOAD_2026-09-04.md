# PREREG - first 5.2 FIXTURE: the empty R9 AOI scenario loads headless on MAK Earth (online)

Date 2026-09-04 (written before the fixture exists; filled in when FIXTURE_52_EMPTY lands).
Tier STANDARD. Docs: RESEARCH_52_FIXTURE_FORMAT_2026-09-04.md (container unchanged; .oob
object-type syntax break irrelevant to an EMPTY fixture; frame keys UG52 Table 20 p354
unchanged; MAK Earth (online) is the only shipped terrain covering the R9 box; batch mode
read-only; sec 6 live-only questions), DIFF sec G Y-7 (online default) / Y-9 (block-on-async
ON for golden runs) / Y-13, PREREG_52_PROFILE_SMOKE (the profile posture).

## 1. Frame
Fixture R9_Mojave_Empty_52.scnx (FixtureGen --profile 5.2; globals-only .oob; terrain MAK
Earth (online); SMS EntityLevel; frame-mode fixed-frame-run-to-complete, frame-time
0.033333), deployed with the sanctioned `--out-dir C:\MAK\vrforces5.2d\userData\scenarios`.
Launched by LaunchVrf52 on the profile defaults (RTI 5.0.1 rtiexec posture), -NoGui, from
the runner's env; observed with WatchVrf --diag --report-backends; one CreateOne at the R9
AOI (34.6150, -116.5500, alt 50) to prove the terrain is live at the AOI. Network required
(online terrain) - the machine has it. ONE variable vs PREREG_52_PROFILE_SMOKE: the scenario.

## 2. Predictions
P1 HIGH: the sim loads the fixture ("Successfully loaded scenario") and joins; no crash
   attributable to the fixture (the known startup crash is 3/8 on ANY scenario; a crash
   with the same parseCmdLine callstack is NOT a fixture miss - repeat once; a DIFFERENT
   callstack IS a miss -> STOP). Falsifier: a load error naming the .scn/.oob/.pln, or the
   sim exiting after "Loading scenario" -> the empty-.oob shape does not load headless
   (research sec 6 Q1) -> STOP, fall back to R2 (-T terrain, no -L) for the next run.
P2 MEDIUM: MAK Earth (online) pages in at the AOI within 120 s of load (sim log terrain
   lines; thread count settles). Falsifier: repeated terrain/tile errors or a 5+ min stall.
P3 MEDIUM: CreateOne at the AOI returns ObjectCreated and WatchVrf then reflects it with
   a REAL position at ~34.615/-116.55 (alt clamped to terrain, not 50 m MSL if that is
   below ground - record the value). Falsifier: created but reflected at (NaN,90) or
   never reflected -> record; the Traffic runs prove reflection works, so this isolates
   the terrain/AOI.
P4 (record, not scored here): frame mode - the runner's frame_gaps instrument is being
   re-baselined on 5.2 (REBASELINE_52_INSTRUMENTS); whether the fixed-frame lever holds
   is PREREG_R9_52's claim, not this prereg's.
Success = P1 AND P3.

## 3. Procedure
0. FixtureGen build + validate_fixture PASS (executor); deploy via the sanctioned --out-dir.
1. Ledger 3 numbers (sim, CreateOne, WatchVrf). 2. LaunchVrf52 -Scenario R9_Mojave_Empty_52
-NoGui. 3. Wait for load + terrain; capture. 4. CreateOne at the AOI. 5. WatchVrf 60 s.
6. Fill sec 4; teardown.

## 4. Result
(filled after the run)
