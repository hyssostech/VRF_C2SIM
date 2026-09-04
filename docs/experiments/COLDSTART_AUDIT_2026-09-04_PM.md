# COLD-START ADVERSARIAL AUDIT - 7f32a8b..b08447d (2026-09-04 PM)

Auditor: cold-start, no project memory, read-only. Sources: the 8 commits and diffs, both prior reviews,
PREREG_52_RTIEXEC / PROFILE_SMOKE / REFLECTION, StartRtiExec52 / LaunchVrf52 / RunC2SimScenario /
AnswerCrashDumpDialog, the rid files vs C:\MAK\makRti5.0.1\rid.mtl, every runs/launch52 capture, C:\MAK\logs
(all callstacks), the rtiexec 15720 log, RTI Users Guide 5.0.1 (fitz: 4.2 Table 2, 7.3, 8.2), the 5.2d shipped
.scnx set (zip read), memory files, a read-only process/socket inventory at ~07:40 local. VERIFIED = a file or
the live machine holds it; ASSUMED = prose only. The working tree moved during the audit (PREREG_52_APP_SMOKE,
ledger rows 3860-3865, uncommitted facade/bridge edits): noted where it bears on a ruling, not audited.

## 0. Facts the record at HEAD does not carry
- F1 A THIRD parseCmdLine crash at 07:28:06 (C:\MAK\logs\...072806-...-59936.callstack.log, frames byte-
  identical to 38180/39028; run 3860, LaunchVrf52 profile defaults: rid-min, no --deviceAddress, Traffic,
  -NoGui). Tally from C:\MAK\logs: 3 of 11 sim launches since 21:24 on 09-03; 3 of 15 overall. The docs
  carry three counts ("2 of 5" scripts/RUNBOOK/tests/memory, "2/7" HANDOFF, "3rd of 8" post-HEAD).
- F2 Crashed pid 59936 is STILL ALIVE (4 threads) parked on MAK's crash box; the post-HEAD ledger says
  launch 3862 was REFUSED (exit 2) because of it. AnswerCrashDumpDialog.ps1 :45 matches only the 5.0.2
  title '^vrfSim.*\.dmp$'; the 5.2 box ("Error vrfSimHLA1516e.exe", LaunchVrf52 :350) is answered by
  nothing in the repo; RUNBOOK 0.5.12 documents the 5.0.2 form only.
- F3 Named smoke captures do not exist: no startrtiexec_smoke.txt, no launch_3858_smoke.txt (PROFILE_SMOKE
  sec 4 names both); evidence = vrfSim_3858 log + watchvrf_3859 only. Review-2 F4 (launch_3854) repeats.
  No -DryRun output of any kind is captured under runs/.
- F4 rtiexec 15720 argv (Win32_Process): -M -R rid-501-rtiexec.mtl -P 4001 -T 4001 -A 127.255.255.255 -N
  127.0.0.1 -i 127.0.0.1 -D 5000 -r -l runs\launch52\rtiexec_501_20260903.log -n 3. The RTI REWRITES the -l
  name (file on disk = rtiexec_501_20260903 + "5.0.1-20260903-214856-Legatus-281993-15720.log"; exec log
  :164 echoes the requested name). StartRtiExec52 :122/:279 tests the literal name it passed, so its start
  path will WARN "log NOT created" and hand the manifest a non-existent path. That path never ran live.
- F5 The rtiexec log holds 9 joins and NEVER more than TWO concurrent federates (sim + one remoteControl;
  the app 3865 = federate 9). The smoke's "3 federates (sim, rtiexec's own, observer)" is wrong - an
  rtiexec is not a federate. HaveRtiLicense()=1 has only ever been observed AT the unlicensed cap.
- F6 The four -K assistants (19612/42396/49336/54616; "LEFT RUNNING" HANDOFF :173, memory) are GONE, no
  record says when or why. UDP 127.0.0.1:4001 is currently owned by pid 26100 (the resigned app 3865).
- F7 The sim writes its vendor log to C:\MAK\logs on EVERY launch besides --logFileName (14 pairs; +83
  VR-Vantage/terrain lines for 3851). LaunchVrf52 :15 "we always redirect it" is false.
- F8 All rtiexec-posture sims (3854, 3858, 3864) were -NoGui. The profile DEFAULTS to GUI ON; vrfGui has
  never joined an rtiexec-mode federation on 5.2 (every GUI launch 3799-3841 was lightweight/assistant).

## 1. Rulings
R1 "Posture final; --deviceAddress not required on either side."
- Competing: the flag is inert for HLA (UG52 Tables 10/11 give it the UDP/DIS device role; the RTI binds
  127.0.0.1 from the rid on every federate) - consistent with both results and with "one pinned side
  suffices". 3857 = sim pinned / observer none; 3859 = sim none / observer PINNED (facade default, banner
  "device-address=default"). Both-none never ran. 3859 VERIFIED (capture + 3858 argv) as a single-variable
  test of the sim side only: the observer's 127.0.0.1 could be doing the work.
- Verdict: CONFIRMED for the profile AS CONFIGURED (sim/gui none + bridge federates 127.0.0.1 = the 3859
  arms); WEAK as a two-sided claim. Closing run: WatchVrf --device-address none against a profile-launched
  sim, plus one GUI-on launch (F8). The bridge default (VrfFacade.h, VrfSettings.cs "127.0.0.1") keeps
  every tool and the app pinned by default - the hidden variable review-2 named, still carried.
R2 rtiexec 15720 vs StartRtiExec52. rid-501-rtiexec.mtl vs rid-min differ in ONE key (tcpNetworkInterface
  Addr 127.0.0.1 vs 0.0.0.0; diff VERIFIED); with -M the CLI governs the connection block (RTI UG 4.2
  Table 2 p45-47: -P/-A/... "ignored unless --manual") and both pass -i 127.0.0.1, so the exec's effective
  configuration is identical (exec log: 4001/4001, 127.255.255.255, both interfaces 127.0.0.1, notify 3).
  CONFIRMED: no material difference. "Idempotent, never restart" is sound but BLIND: any rtiexec.exe from
  the RTI dir plus any TCP 4001 listener = READY; never tested (ASSUMED). First reboot: exec + forwarder
  die, the START path runs live for the first time and trips F4; LaunchVrf52 alone only WARNS on a missing
  exec (:258) - only the runner's Stage 2r is fatal. WEAK on "first run after boot".
R3 The smoke label. Honest in its own sec 4 ("infrastructure only"), dishonest as the HANDOFF's "smoke
  green". Exercised: LaunchVrf52 defaults, StartRtiExec52's ALREADY-UP branch invoked DIRECTLY (not via the
  runner), WatchVrf join + reflect; -NoGui; scenario paused. NOT exercised: RunC2SimScenario itself (no
  live 5.2 runner run exists), Stage 2r wiring and the READY-marker regex on live output, manifest fields
  (connectionMode/deviceAddress*/rtiExec.* exist in the diff - VERIFIED - populated NEVER), in-runner crash
  handling, teardown, GUI-on. "Gates re-run by the seat" (DryRun diffs, tests) = ASSUMED, no capture.
  F5 corrects P4. Verdict: "profile infrastructure green" REFUTED as a label; "three scripts work
  standalone" CONFIRMED.
R4 Startup crash. 38180/39028/59936 identical (vl.dll <- vlHLA1516e <- DtVrfSimOptions::parseCmdLine(768)
  <- DtVrfApp::init(632)); 48944 is the different DIGuy crash (3826). Matrix: 3848 Start-Process, rid-
  rtiexec, 13 s after the exec started; 3853 direct ProcessStartInfo, redirected stdio, lightweight rid;
  3860 Start-Process, rid-min, profile defaults, 11 min after the previous sim stopped. Successes span
  every rid, both launchers, -q and not. Ranking: (1) nondeterministic init inside VR-Link's option/
  connection parsing (same frames, ~27%, no configuration correlate) - the three .dmp in C:\MAK\logs plus
  the bin64 PDBs have never been opened, the vendor-diagnostics-first step; (2) an RTI-side race at
  connect (fits 3848 only); (3) launcher/env - falsified. Preceding-sim exit times are unrecorded, so
  "overlap with the previous exit" cannot be ranked. "Detect and fail" is NOT enough: ~1 in 4 launches
  kills an unattended run at Stage 3 and F2 makes every retry refused until a human closes the box. Owed
  now: (a) close-a-crashed-pid (the standing narrow permission covers a process that failed its own
  start) + a 5.2 crash-box answerer; (b) ONE retry on fresh ledgered numbers with the crash written to the
  manifest so the rate stays visible; (c) the dump read before any cause is written down.
R5 Doc trims - deleted and surviving nowhere: DIFF A12 "cancelling now exits the engine cleanly (VRF-9175)"
  (zero hits in docs/); HANDOFF "rung 2 P4(c), -q P1(b), QPAIR I4/I6 were all this error class" (the
  cross-list; each instance survives only in its own prereg); ONE-LINE STATUS "TWO INSTRUMENT CHECKS NEED
  A RULING" (NEXT row 1 now records both falsified, the pointer went). Survive elsewhere: fixture SHA
  D27E540F (7 preregs), "gated on 4.6.1 rtiSimple" (PREREG_52_LAUNCH), --out-dir (runner :494), docs.mak
  URLs (memory). Deleting the refuted "elevated windows invisible" was right. Two real losses, minor.
R6 Memory. FALSE now: MEMORY.md :7 "extra rid keys crash the sim" (refuted by 3853/3860) and "+ interface
  127.0.0.1" as posture (dropped by 3857/3859); :32 "4 -K assistants" (F6) and "smoke green" (R3).
  rti-assistant-version-gate.md: description names interface 127.0.0.1 as posture; "the 12 parameters"
  (six off stock); "ELEVATED" 97708 never tested (review-1 R1). vrf-52-migration-phase0.md: "gates stay
  on 1516e/makRti4.6.1" and the PATH-prefix line naming makRti4.6.1\bin (stale since the 5.0.1 ruling);
  "Prototype zero NOT automatable" (review-1 R8: by pipe only); "not required on either side" (R1).
  lessons-live-capture-pipelines / feedback-docs-first: nothing false. The refuted rid-key cause also
  survives in code: StartRtiExec52 :58, runner :133, rid-501-rtiexec-min.mtl header.
R7 "No 5.2 fixture -> fixtures next" is NOT the critical path. The app creates units from the init
  (VrfC2SimService.cs :598-601); R9 init is at 34.61N/-116.71W; 5.2d ships EMPTY scenarios on MAK Earth
  (online) - Sample\VR-TheWorld_Online\GroundMovement, BehaviorGround*, WainwrightMechanizedAttack (0 OOB
  objects, zip read) - and SharedData\19 holds MAK Earth (online).mtf (VERIFIED). A first 5.2 C2SIM run
  needs the runner + -Scenario one of those + data\R9_Mojave_Lean_Initialization.xml. Real blockers: the
  runner's 5.2 DEFAULT scenario is firstexperience on Ala Moana (Honolulu) and cannot host Mojave units;
  online terrain needs the network (Y-7 ruled online default). FFRTC/repeatability needs a fixture LATER.
  Verdict REFUTED as ordering (post-HEAD RESEARCH_52_FIXTURE_FORMAT row R2 names a no-fixture route).
R8 Licence. RTI UG 8.2 p82 (fitz): WITH an rtiexec "a federation execution will not allow a third
  unlicensed federate to join" and licensed/unlicensed "cannot interoperate" - under this posture the cap
  is a LOUD join failure, not lightweight's silent forcible-resign. But nothing in the runner reads
  HaveRtiLicense (grep: MAKLMGRD_LICENSE_FILE non-empty only, :1923/:2019); the exec runs
  rtiExecPerformsLicensing 0 so every LRC checks out alone; F5 = 2 concurrent is all ever seen; the .lic
  name says 9-15-26. A refusal at Stage 4/6b surfaces as a tool/app non-zero exit with no licence word
  in it. Verdict: cap loud (docs), runner detection ASSUMED; owed = pre-check FAILS on rti=0 + expiry check.

## 2. Prior-review defects - closure at HEAD
Review 1: #1 gate relabel CLOSED (channel repaired 3856); #2 env drift recorded, Q1/Q2 (who spawned 54616,
who changed PATH) still open, F6 adds "who ended them"; #3 licence gate still DIFF A13 "N (later)" -
declared in RUNBOOK 0.5.13, not built. Review 2: #1 ledger 3853 relabelled, PREREG sec 4 corrected -
CLOSED except the refuted cause survives in MEMORY.md + three comments (R6); #2 --deviceAddress tunable -
CLOSED sim/gui/observer, bridge default still pinned (R1); #3 3838 cited - CLOSED; 3854 capture still
missing and F3 repeats it; env==ctl and the header-size warning acknowledged open; licence cap untested.

## 3. Dissent log (one line each; what settles it)
- D1 vs "trigger unknown": open the three .dmp with the bin64 PDBs; the faulting vl.dll function decides
  option-parsing vs connection-init before any retry policy is written.
- D2 vs "not required on either side": one run sim none + observer none, and one GUI-on launch.
- D3 vs "smoke green": one live `RunC2SimScenario -VrfProfile 5.2` whose manifest carries rtiExec.* and
  connectionMode populated settles whether the runner, not its scripts, works.
- D4 vs "fixtures next": one runner run on GroundMovement with the R9 lean init; units created = settled.
- D5 vs "idempotent": StartRtiExec52 beside a foreign 4001 listener; READY = the check is blind.

## 4. Top 3 defects by consequence
1. The startup crash is 3/11 and its aftermath is a hard stop: the dead pid parks on a 5.2 crash box
   nothing answers and every later launch is refused (F1, F2). The runner cannot run unattended on 5.2.
2. "Profile green" rests on scripts run by hand: the runner never executed live on 5.2, Stage 2r's
   marker/manifest path never ran, StartRtiExec52's start path is untested and mis-names its log (F4),
   GUI-on is untested (F8), two named captures do not exist (F3).
3. The always-loaded record teaches refuted causes: MEMORY.md still says extra rid keys crash the sim and
   interface 127.0.0.1 is posture, three code comments repeat it; licence evidence sits exactly at the
   unlicensed cap with no runner check and 11 days to expiry (F5, R8).
