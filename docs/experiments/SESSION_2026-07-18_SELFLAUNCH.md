# SESSION 2026-07-18 - scripted VR-Forces bring-up (0.4), live attempt

## *** ROOT CAUSE FOUND (documented behavior, not a bug) - READ THIS FIRST ***

THE RTI ASSISTANT PROMPT IS A MANDATORY INTERACTIVE STEP IN THE VENDOR'S OWN
DOCUMENTED STARTUP SEQUENCE. C:\MAK\vrforces5.0.2\doc\help\Content\SharedTopics\
XMLrti\InstallMAK-RTI.htm, verbatim:

    "To run a MAK application with the MAK RTI:
     Be sure the license server is running.
     Start the application. The RTI Assistant will prompt you to choose an RTI
     configuration.
     Choose a configuration. If necessary, start the rtiexec.
     Click Connect. The application should run."

A MAK federate on HLA DOES NOT RUN until someone picks a connection and clicks
Connect. VR-Forces' own help documents NO suppression for this and defers to the
MAK RTI Users Guide (on disk: C:\MAK\makRti4.6.1\doc\RTIUsersGuide.pdf - the
VR-Forces help says "follow the instructions in Chapter 2 of MAK RTI Users
Guide"). That PDF was never read by this effort until 2026-07-18.

WHY EVERY OBSERVATION NOW FITS:
- An rtiAssistant (pid 9284) had been running since 2026-07-15 ALREADY ANSWERED
  AND CONNECTED. It held TCP 6003.
- Every launch spawns its OWN rtiAssistant. Each new one FAILED to bind 6003
  (56-byte logs) and died instantly - leaving the already-connected 9284 to serve
  the federation. THE PORT COLLISION WAS LOAD-BEARING AND BENIGN: it prevented an
  unanswered dialog from ever appearing. That is why the bare launch ("Test A",
  13:22) came up healthy.
- The supervisor then KILLED 9284 as "cleanup". From that moment every launch got
  a FRESH, UNANSWERED assistant sitting at "Choose RTI Connection", and every
  backend correctly blocked - hanging right after VR-Link init and BEFORE the
  parameter database, exactly where a pre-RTI-connect block belongs.

SUPERVISOR ERROR, RECORDED DELIBERATELY: the "stale" process was the only thing
making unattended launch work. Tidying it destroyed the working configuration,
and seven single-variable experiments were then spent hunting a bug that was
documented behavior. The correct first move - reading the vendor's startup
procedure and the RTI Users Guide it points to - was skipped in favour of
experimentation. Cost: the entire live window; Phase 1 did not run.

EVERY CAUSE HYPOTHESIS TESTED AND KILLED THIS SESSION (all falsified by evidence,
none of them the answer): argument overrides; --scenarioFileName; appNumber
override; stale federate from force-kills; license seat exhaustion (license is
node-locked UNCOUNTED with no SERVER lines - no daemon needed, lmstat confirms);
Pitch-RTI-vs-MAK-RTI PATH collision (MAK RTI is first on PATH); --simArgs
replacing profile args (the spawned command line is complete, including
--frontEndPID).

## *** THE FIX - FOUND IN RTIUsersGuide.pdf AND VERIFIED LIVE ***

    set RTI_ASSISTANT_DISABLE=1   (environment variable, any value or none)

MAK RTI Reference Manual sec 5.2.10, printed p. 5-11 (PDF p. 82), verbatim:

    "To disable the RTI Assistant, create an environment variable called
     RTI_ASSISTANT_DISABLE. It does not require a value. Its existence causes
     the RTI to not create the RTI Assistant."

VERIFIED LIVE 2026-07-18 15:16-15:19, process-scope env var only (shared rid.mtl
NOT edited):
- Bare launch: back-end HEALTHY AND JOINED in 8 SECONDS (UDP 4000 bound, 23
  threads, no assistant process, no prompt). Prior attempts never reached health
  at all in 200+ s.
- Launch WITH --simArgs --appNumber 3472 --scenarioFileName TropicTortoise.scnx
  --guiArgs --appNumber 3473: HEALTHY IN 8 SECONDS (59 threads), "Successfully
  loaded scenario." in vrfSim.log, both TropicTortoise baseline objects locally
  simulated (GlblTerrDmg, Blocking Terrain Page-In Area).
  => THE ARGUMENT OVERRIDES ARE FULLY EXONERATED. They were accused twice in this
  session and were never implicated.

RELATED DOC FINDING - AND A CORRECTION TO THIS SESSION'S OWN EARLIER CORRECTION.
RTI Users Guide p. 7-8 / RefMan Appendix A Table A-1 state VERBATIM, under each of
RTI_useRtiExec, RTI_udpPort, RTI_tcpPort, RTI_destAddrString, RTI_tcpForwarderAddr
and RTI_forceFullCompliance: "This parameter is ignored unless
RTI_configureConnectionWithRid is set to 1." Our rid.mtl has
(setqb RTI_configureConnectionWithRid 0). THEREFORE the sec 1 claim below -
"rtiexec never runs BECAUSE RTI_useRtiExec 0" - reasons from a parameter the RTI
discards. The OBSERVATION (no rtiexec process; UDP 4000 bound on a healthy
back-end) stands and is unaffected; the EXPLANATION was wrong. Corrected in
RUNBOOK sec 0.5 as well. By default the Assistant's stored connection
configuration overrides rid.mtl entirely (UG p. 7-8).

## *** RTI_ASSISTANT_DISABLE ALONE IS NOT SUFFICIENT - PHASE 1 IS BLOCKED ***

VERIFIED 2026-07-18 (appNo 3475): with RTI_ASSISTANT_DISABLE=1, WatchVrf joins
the federation cleanly and resigns cleanly, but reports `reflected=0 readable=0`
for a FULL 40 s against a back-end whose own log proves TropicTortoise loaded and
both baseline objects are locally simulated. ResetVrf (3470/3471/3474) reports the
same: BackendCount=0, 0 objects.

CONSEQUENCE, STATED PLAINLY: THE MOVEMENT ORACLE IS BLIND IN THIS CONFIGURATION.
PHASE 1 MUST NOT RUN LIKE THIS - it would generate a full session of empty
telemetry and the WatchVrf displacement evidence every P1-A..D claim depends on
would not exist. This check (WatchVrf discovery BEFORE the session) is now a
mandatory Phase 1 precondition regardless of how VR-Forces is launched.

MECHANISM (doc-grounded): RefMan Appendix A on RTI_mcastDiscoveryEnabled, verbatim
- "This parameter is set to 0 unless RTI_configureConnectionWithRid is set to 1."
Multicast discovery is FORCED OFF while RTI_configureConnectionWithRid is 0, and
by default the RTI Assistant's stored connection configuration is what supplies
connection values (UG p. 7-8). Disable the Assistant WITHOUT making rid.mtl
authoritative and nothing supplies a working discovery path: federates join
CWIX-2024 but never see each other. So the Assistant is doing TWO jobs - prompting
(unwanted) and supplying the connection (required). Killing it drops both.

TWO WAYS FORWARD (neither requires editing shared global config):

(A) KEEP THE ASSISTANT, ANSWER IT ONCE. Do NOT set RTI_ASSISTANT_DISABLE. Launch
    normally, answer the "Choose RTI Connection" dialog once (a human click, or UI
    automation), and LEAVE THAT ASSISTANT RUNNING. This is exactly the state the
    machine was in from 2026-07-15 to 2026-07-18, during which discovery worked
    and Appendix B recorded "Clean (2 baseline)". Lowest risk; costs one click per
    machine boot. The Assistant also offers a persisted "Always Try to Use this
    Connection" check box (UG p. 4-5) which may remove even that click - UNTESTED.

(B) PROCESS-SCOPED RID OVERRIDE - fully unattended, ZERO global effect. Copy
    rid.mtl to a private directory inside this repo's working area, set
    RTI_configureConnectionWithRid 1 in THE COPY, and point only our own processes
    at it via the RTI_CONFIG environment variable. Documented in
    SharedTopics\XMLrti\InstallMAK-RTI.htm, verbatim: "Put the configuration files
    in the directory from which you are running, or set the environment variable
    RTI_CONFIG to the directory that contains them." This makes rid.mtl values
    authoritative (activating RTI_useRtiExec / ports / mcast discovery) WITHOUT
    modifying C:\MAK\makRti4.6.1\rid.mtl and without affecting any other
    application. Caveat from RefMan: all federates in a federation must agree on
    RTI_useRtiExec, so RTI_CONFIG must be set for EVERY process we launch
    (back-end, front-end, WatchVrf, ResetVrf, the port app) - which we control.

SUPERVISOR NOTE: the supervisor initially proposed editing the SHARED
C:\MAK\makRti4.6.1\rid.mtl. The user objected. The objection was correct - option
(B) achieves the same result process-scoped, and option (A) needs no config change
at all. Do not edit the machine-wide rid.mtl.

## OPEN ITEM - THE 0.4 GATE IS NOT PASSED

Prereg sec 4 predicts a ResetVrf --dry-run that "will JOIN AND READ CLEANLY - it
discovers the scenario's baseline objects (2 for TropicTortoise ...)" TWICE.

WHAT WAS OBSERVED (appNos 3470, 3471 no-scenario; 3474 scenario-loaded):
- PASSED: all three joined cleanly, exit 0, resigned cleanly, ZERO 0xC0000005.
  That crash has blocked this path since 2026-07-15 and did not recur.
- NOT PASSED: every run reported BackendCount=0 and discovered 0 reflected
  objects - including 3474, run against a back-end whose own log proves the
  scenario was loaded and both baseline objects were locally simulated.
  Appendix B 3414/3451 historically recorded "Clean (2 baseline)", so ResetVrf
  CAN see them.

LEADING HYPOTHESIS (NOT TESTED - do not record as fact): disabling the Assistant
changed WHICH connection the RTI uses. With RTI_configureConnectionWithRid 0 the
rid.mtl connection values are ignored and the Assistant's stored configuration
normally supplies them; with the Assistant disabled, what supplies them is
UNDOCUMENTED (the RTI Users Guide never states the fallback). The Assistant's
persisted connection (%APPDATA%\MAK\RTI\4.6\Legatus\connections.xml) is an
rtiexec-style entry - tcp 4001, forwarder 5000 - whereas the rid.mtl lightweight
default is port 4000. Federates may now be joining a DIFFERENT connection than
before, one on which they do not discover each other.

NEXT PROBE (single variable): set RTI_configureConnectionWithRid 1 so rid.mtl
becomes authoritative and the connection is explicit rather than undocumented,
then re-run the 3474 gate. CAUTION from RefMan: this makes every rid.mtl
connection value live for ALL federates, and all federates in a federation must
use the same RTI_useRtiExec value - so it must be applied uniformly, and rid.mtl
is SHARED CONFIG (get user approval before editing it).

---

Status: ROOT CAUSE FOUND (above). Three durable findings landed and TWO earlier
supervisor conclusions are RETRACTED below (the retractions are kept in place
deliberately - they are the record of how the wrong answers were reached). Live work ended when the user
stopped VR-Forces after RTI Assistant error dialogs. ASCII only.

Session clock: start 2026-07-18 12:56:31 local (-04:00) = 16:56:31Z. Tool logs
stamp UTC; this machine runs local. All times below are LOCAL.

## 0. What was attempted and why

The user directed (three times, escalating) that the assistant must learn to drive
the live machinery from the CLI rather than hand every launch to a human. 0.4 had
been demoted behind Phase 1 on 2026-07-18, but the demotion was a SCHEDULING call,
not a prohibition. The three recorded LaunchVrf.ps1 defects were fixed and the
script was driven live. The user then chose option (b) - keep debugging the launch
- over (a) salvage Phase 1.

IMPORTANT SCOPE NOTE for whoever reads this next: this session spent the live
window on the BRING-UP MECHANISM, not on the Phase 1 native baseline. That is
exactly the trade PREREG_0_4_SELFLAUNCH.md sec 11.1 judged to be the worse one.
Phase 1 did NOT run. PHASE1_SESSION_SCRIPT.md remains READY and unstarted.

## 1. FINDING - rtiexec NEVER RUNS ON THIS MACHINE (RUNBOOK sec 0.5 is WRONG)

`C:\MAK\makRti4.6.1\rid.mtl` contains, verbatim:

    (setqb RTI_useRtiExec 0)

RUNBOOK sec 0.5 states "rtiexec is spawned automatically by the RTI on first
federate join - do not launch it separately". The second clause is right; the
first is FALSE under this RID. rtiexec never appears, so any readiness gate that
waits for it can never pass. LaunchVrf.ps1's readiness poll did exactly that and
reported NOT READY (exit 3) against a launch whose front-end was fully healthy.

Related RID settings that decide the real transport:

    (setqb RTI_udpPort 4000)
    (setqb RTI_tcpPort 4000)
    (setqb RTI_mcastDiscoveryEnabled 0)
    (setqb RTI_distributedUdpForwarderMode 0)
    (setqb RTI_tcpForwarderAddr "127.0.0.1")
    (setqb RTI_distributedForwarderPort 5000)

`rtiForwarder` (a long-lived daemon, pid 17372, up since 7/15) listens on 5000 and
4001, but `distributedUdpForwarderMode 0` means it is NOT the federation path. A
VERIFIED-HEALTHY backend has NO connection to :5000.

## 2. FINDING - the correct backend-readiness oracle

Measured against a backend confirmed healthy in this session (Test A, sec 4):

| Signal                        | Healthy       | Stalled      |
|-------------------------------|---------------|--------------|
| UDP 4000 bound (RTI_udpPort)  | YES           | not observed |
| thread count                  | 36 (5->7->22->37 over ~60 s) | STUCK AT 2 |
| CPU                           | climbs steadily | flat ~1.2  |
| vrfSim.log progression        | reaches parameter database / physicalWorldParams / sensor propagators | FROZEN at the VR-Link/MSVC banner |

USE THESE. Do NOT use, all three shown wrong this session:
- process presence ("vrfSimHLA1516e exists") - the stalled backend was present
  the whole time. This is the same present-but-dead defect the effort keeps
  finding elsewhere; the launch script committed it.
- rtiexec presence - structurally impossible here (sec 1).
- a connection to forwarder :5000 - dark even when fully healthy.

vrfSim.log is BLOCK-BUFFERED (RUNBOOK sec 3), so a short log is not by itself
proof of a stall; it is corroborating evidence only, read alongside threads/CPU.

## 3. FINDING - stale rtiAssistant squatting port 6003 (NEW, environmental)

An rtiAssistant instance (pid 9284) had been running since 2026-07-15 10:24 with
its window stuck on the modal title "Choose RTI Connection", holding TCP 6003.
Every VR-Forces launch starts its own RTI Assistant, which then FAILS to bind
6003. User-captured dialog, verbatim:

    Error starting RTI Assistant
    RTI Assistant server creation failed. The port [ 6003 ] may be in use,
    either by an RTI Assistant that is already running or another application.
    This port can be changed with the RTI_ASSISTANT_PORT environment variable.

pid 9284 then crashed and wrote rtiAssistant9284.dmp. AFTER the crash a FRESH
rtiAssistant (pid 99372) took 6003 with NO modal dialog - i.e. the environment is
now in a BETTER state than at any point since 7/15.

RTI_ASSISTANT_PORT is currently unset in both Machine and User scope.

Every federate observed this session (vrfSim, vrfGui, ResetVrf, rtiForwarder)
opens a connection to 6003. ResetVrf prints "Connected to RTI Assistant."
immediately before the point where it froze.

## 4. THE ISOLATION ATTEMPT - AND THE RETRACTION

Two live launches were run:

- RUN 1 (LaunchVrf.ps1, overrides ON): `--usePredefinedConnection "<profile>"
  --simArgs --appNumber 3460 --scenarioFileName "../userData/scenarios/
  TropicTortoise.scnx" --guiArgs --appNumber 3461`. Backend STALLED at 2 threads.
  vrfGui came up fully healthy (47 threads, real window title). A subsequent
  `ResetVrf --dry-run` (3462) hung at the RTI join, frozen at the rid.mtl banner,
  and was killed (exit 255) under the failed-own-join exception.
- RUN 2 / "Test A" (CONTROL, overrides OFF): bare `vrfLauncher
  --usePredefinedConnection "<profile>"`, profile's own 3001/3101, no scenario.
  Backend HEALTHY per the sec 2 oracle.

SUPERVISOR CONCLUSION AT THE TIME: "the argument overrides break the backend."

*** THAT CONCLUSION IS RETRACTED. THE COMPARISON WAS NOT SINGLE-VARIABLE. ***

The rtiAssistant state (sec 3) ALSO differed between the two runs and was not
controlled:
- RUN 1: vrfSim and vrfGui both held ESTABLISHED connections to 6003, to the OLD
  assistant that was sitting on a modal "Choose RTI Connection" dialog.
- RUN 2: those 6003 connections were in FinWait2 (closing/dead).

A backend blocking on an RTI Assistant that is itself stuck on a modal dialog
would present EXACTLY as "2 threads, log frozen at the VR-Link banner". So the
argument overrides and the assistant state are FULLY CONFOUNDED - the identical
error the Appendix B 3414 entry records ("region and launch method were fully
confounded"). Two candidates remain live and NEITHER is eliminated:
  (H1) the --simArgs/--guiArgs appNumber overrides and/or --scenarioFileName;
  (H2) the stale-rtiAssistant / port-6003 collision.

SECONDARY RETRACTION: earlier in the same session the supervisor DISMISSED the
rtiAssistant hypothesis, reasoning that ledger entries 3451-3454 joined cleanly on
7/17 while that dialog was already open. That inference was invalid: those were
HUMAN GUI launches, and no evidence established the assistant was in the same
blocked state then. "The dialog existed" was treated as "the dialog was blocking",
which does not follow. The user's screenshots falsified the falsification.

## 5. WHAT IS NEVERTHELESS ESTABLISHED

Bare `vrfLauncher.exe --usePredefinedConnection "HLA 1516 Evolved RPR 2.0 with MAK
extensions"`, cwd bin64, MAKLMGRD_LICENSE_FILE refreshed from Machine scope,
brought up a HEALTHY combined-mode backend + front-end UNATTENDED, with zero human
clicks. That is the first confirmed scripted VR-Forces bring-up in this effort and
it directly answers the user's standing ask. What is NOT yet established is the
same launch WITH fresh app numbers and an auto-loaded scenario.

## 6. NEXT STEPS (ordered; all cheap, all single-variable)

1. De-confound H2 FIRST, because it is now free: the stale assistant is gone and a
   clean one holds 6003. Re-run RUN 1's EXACT overridden command line against the
   clean environment. If the backend is healthy -> H2 was the cause and the
   overrides are exonerated. If it stalls again -> H1, and continue to step 2.
2. If step 1 still stalls, split H1: add ONLY the appNumber overrides (no
   --scenarioFileName), then ONLY --scenarioFileName. One variable each.
3. Fix LaunchVrf.ps1 DEFECT 4 (below) before either run, so the script stops
   asserting an impossible readiness condition.
4. Consider setting RTI_ASSISTANT_PORT, or a precondition that FAILS when a
   pre-existing rtiAssistant holds 6003, so this class cannot recur silently.
5. Only then re-schedule the 0.4 gate proper (two clean ResetVrf dry-runs).

## 7. LaunchVrf.ps1 defect status

FIXED this session (committed):
1. Readiness was process-presence only; the -DryRun text advertised a
   MainWindowTitle check the poll never performed. Now actually enforced, with a
   distinct exit 4 BLOCKED verdict for "front-end process up but no window title"
   (the modal-dialog signature). This fix WORKED: RUN 1 correctly reported the
   front-end as having a real window.
2. App numbers were baked-in defaults behind a warning. Now MANDATORY, hard exit 2,
   including an identical-numbers check. Verified by direct test.
3. MAKLMGRD_LICENSE_FILE was overwritten unconditionally, blanking a working
   process-scope value when the Machine value was empty. Now conditional.

DEFECT 4 - FOUND THIS SESSION, NOT YET FIXED: the readiness poll requires rtiexec,
which never runs on this machine (sec 1). The script therefore CANNOT report READY
here regardless of actual health. Replace the rtiexec condition with the sec 2
oracle (UDP 4000 bound + thread growth + log progression).

## 8. Application numbers consumed

- 3460 - vrfLauncher back-end, RUN 1. Backend stalled; never joined.
- 3461 - vrfLauncher front-end, RUN 1. Front-end healthy; never joined a federation.
- 3462 - ResetVrf --dry-run, hung at join, killed. CONSUMED.
- 3463 - ledgered for a second ResetVrf dry-run; NEVER USED (burned, do not recycle).
- RUN 2 / Test A used the connection profile's own 3001 / 3101, NOT ledgered
  numbers. This is what a bare launch does and what every human launch has always
  done; recorded here so the trace is not mistaken for an unledgered join.

Phase 1's reservations 3455-3459 were NOT touched and remain valid.
