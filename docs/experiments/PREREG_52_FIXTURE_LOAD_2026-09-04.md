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
AMENDMENT registered 2026-09-04 BEFORE this run (supervisor): P1's crash clause was written
when the startup crash was believed to fire "3/8 on ANY scenario". PREREG_52_CRASH_BISECT has
since bound it to `--logFileName` (11/30 with, 0/12 without, p=0.013) and neither launcher nor
runner passes that option any more. So the expectation TIGHTENS: expect ZERO startup crashes.
A parseCmdLine crash is therefore no longer an expected nuisance to retry past - it would mean
the option leaked back in (check the invocation FIRST) or the bisect's scope is wrong. Retry
ONCE, and if it recurs with `--logFileName` absent from the logged command line, that is a MISS
against the bisect and a STOP for this prereg. Pre-committing this so a crash cannot be
explained away after the fact.
P1 HIGH: the sim loads the fixture ("Successfully loaded scenario") and joins; no crash
   attributable to the fixture (see the amendment above for the crash rule). Falsifier: a
   load error naming the .scn/.oob/.pln, or the
   sim exiting after "Loading scenario" -> the empty-.oob shape does not load headless
   (research sec 6 Q1) -> STOP, fall back to R2 (-T terrain, no -L) for the next run.
P2 MEDIUM: MAK Earth (online) pages in at the AOI within 120 s of load (sim log terrain
   lines; thread count settles). Falsifier: repeated terrain/tile errors or a 5+ min stall.
AMENDMENT 2 registered 2026-09-04 BEFORE the CreateOne, then CORRECTED the same day after the
user challenged it (supervisor): sec 1 says "alt 50"; the create used alt 10000 MSL instead.
THE DEVIATION STANDS, ITS ORIGINAL JUSTIFICATION DOES NOT. As first written this amendment
said a low-MSL create "froze entities". THAT IS FALSIFIED - CORRECTIONS_LOG.md "Birth
altitude": the 10000 m fix was ALREADY ACTIVE in the three 2026-07-19 scored runs and units
froze anyway, and three taskees at the SAME birth altitude split one-mover / two-frozen, so
birth altitude is NOT the freeze discriminator. I read the refuted link in CreateOne's header
comment and repeated it without checking the record; the comment and the plan of record are
now fixed. THE ACTUAL REASONS for 10000 m: (1) it is the SHIPPED posture in both codebases,
so it is the configuration every other run is measured in - deviating adds a variable; and
(2) what a below-terrain create does to a STATIONARY entity is an OPEN question
(VRF_GROUND_TRUTH sec 5 item 3), so alt 50 would confound P3 with an unsettled one. P3 is
scored on the CLAMPED value landing on terrain near 34.615/-116.55. The buried case, if ever
wanted, is a separate one-variable run - and it would be an OPEN question, not a known bug.
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

## 4. Result (2026-09-04; appNos 3908 sim / 3909 CreateOne / 3910 WatchVrf; captures in
runs/launch52/fixtureload_3908_*.txt and watch_3910_*.trace)
SUCCESS = P1 AND P3. BOTH HELD.
DEPLOY: repo artifact copied to C:\MAK\vrforces5.2d\userData\scenarios\, sha256 verified
byte-identical after the copy (ED65C351...3A68). Nothing else under C:\MAK was written.
P1 HELD - the sim launched READY FIRST TRY (pid 83912, 12 threads, floor 8) and joined:
"Joined federation MAK-ONE-2025 with federate type VR-Forces Sim Engine 5.2d". NO CRASH,
consistent with the amendment's tightened expectation now that --logFileName is not passed.
  CARE POINT, recorded because it nearly read as a falsifier: the vendor log NEVER prints a
  "Successfully loaded scenario" line and names the fixture ONLY in the echoed command line;
  what it prints is "Creating new scenario on terrain ...MAK Earth (online).mtf". That is not
  evidence of an empty default start. DISCRIMINATOR: vrfSim.mtl sets
  (setqb defaultTerrainDatabasePath "") - a scenario-less 5.2 start has NO terrain - yet this
  run loaded the exact terrain our .scn names, resolved through SHARED_DATA_DIR, with 218
  elevation extents and max_lod 15. The terrain could only have come from the fixture.
  P3's clamp (below) is the second, independent confirmation.
P2 HELD - MAK Earth (online) paged in well inside the 120 s cap; navData for that terrain
found; no tile errors. Unrelated noise present and NOT a miss: missing DIGuy/vegetation
model data under SharedData\19 and a Nahimic-service warning.
P3 HELD - CreateOne 3909 -> ObjectCreated uuid VRF_UUID:7113902b-1b64-064d-b7b5-327b3ed64661,
entityId 1:3908:6. WatchVrf 3910 (60 s, 12 samples) reflects it at lat 34.615000, lon
-116.550000, alt **1149.8 m** on every sample - REAL coordinates, no NaN, no 10000, nothing
negative. Requested 10000 m MSL clamped to 1149.8 m: the create-time ground clamp worked and
the terrain is live AT THE AOI (a clamp to 1149.8 there requires real elevation data).
Counts steady at ent=1 agg=0 env=1 ctl=1 backends=1; the vendor's own
printReflectedObjectCounts agrees (1 entity, 1 control object, 2 total).
P4 NOT SCORED, as registered - no 5.2 frame baseline exists (REBASELINE_52_INSTRUMENTS).
The fixture DOES carry frame-mode fixed-frame-run-to-complete / frame-time 0.033333 (read
from the .scn inside the .scnx), and the _VF variant carries variable-frame / 0.100000 as
the discriminating control, so the pair is ready for that baseline.
ROUTE DECIDED: R1 (the authored empty fixture) LOADS HEADLESS. The R2 fallback (-T terrain,
no -L), which would have cost the frame lever, is NOT needed.

Adversarial review: the strongest competing account was "the sim ignored our file and built a
default scenario, and the terrain is a coincidence" - which the missing load line and the
"Creating NEW scenario" phrasing actively suggest. Falsified by the empty
defaultTerrainDatabasePath (no scenario => no terrain) plus the clamp landing on real
elevation at the requested AOI. Not independently checked: whether a 5.2 sim prints a load
line at all at notifyLevel 3 - no surviving Traffic-load log was available to compare against,
so the "no load line is normal" reading is INFERRED, not verified. Cheapest way to close it if
it ever matters: one scenario-less launch, which should show NO terrain load at all.
UNRELATED CORRECTION made during this run, at user challenge: amendment 2 originally justified
the altitude choice with the FALSIFIED "low-MSL create freezes entities" claim. See that
amendment; the code comment and plan-of-record that carried the refuted link are now fixed.
