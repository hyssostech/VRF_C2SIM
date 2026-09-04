# PREREG - VrfC2SimApp (Release-5.2) joins the 5.2d sim the CONFIG-FILE way (app smoke)

Date 2026-09-04. Tier STANDARD. Predecessors: PREREG_52_PROFILE_SMOKE_2026-09-04.md (the
profile's infrastructure stages are green), runner commit 5cea2ed (Vrf:ConfigFileIdentity,
Vrf:DeviceAddress), COLDSTART_REVIEW_2026-09-03 D1 ("every claimed join needs a capture").

## 1. Frame
The runner's Stage 6b (VrfC2SimApp) has never run on 5.2. The app starts the bridge BEFORE
it connects to C2SIM (src/VrfC2SimApp/VrfC2SimService.cs :219 Start, :253 _sdk.Connect), so
its VR-Forces join can be observed with the private C2SIM server DOWN (Docker is not
running; no server is touched). Env = exactly what the runner's 5.2 DryRun prints for the
app: 5.2 PATH prefix, MAK_*, RTI_RID_FILE=config/rid-501-rtiexec-min.mtl,
RTI_ASSISTANT_DISABLE, Vrf__ApplicationNumber (ledgered), Vrf__Federation="",
Vrf__FedFileName="", Vrf__ConfigFileIdentity=true, Vrf__ConnectionConfigFile=MAK-ONE-2025-
Config.xml, Vrf__TypeMapFile=data/unit-type-map-52.json, C2SIM__RestUrl/StompUrl = the
private server (down), cwd = C:\MAK\vrforces5.2d\bin64, --contentRoot = bin\Release-5.2
output dir. Sim: LaunchVrf52 profile defaults, Sample\Traffic, -NoGui.

## 2. Predictions
P1 HIGH: the app logs the config-file join line ("joining the CONFIG-FILE way ... Native
   stack (pre-Start) = 5.2|...") and Start() succeeds (its post-Start log line names
   NativeStackInfo 5.2|...vrforces5.2d\bin64\vrfcontrol.dll). Falsifier: Start() false /
   FileLoadException / a 5.0.2 stack string -> STOP (wrong binary or env).
P2 MEDIUM: the app discovers the back-end (a BackendCount >= 1 log line, or its own
   "backend" readiness message) within 15 s. Falsifier: 0 -> record; the tools discover it
   in 0.1 s on the same posture, so an app-only miss is an app-side defect.
P3 LOW (record): the app's C2SIM connect fails against the down server and the app stays
   alive/retries or exits per its own contract - whichever, captured; no VRF-side state is
   created (no init).
Success = P1 (the capture is the evidence D1 asked for).

## 3. Procedure
Ledger two numbers; LaunchVrf52; run the app with redirected stdout for ~60 s; stop the app
(our process; it never joined C2SIM) and the sim; tee everything to runs/launch52/.

## 4. Result (2026-09-04 07:30 local; runs/launch52/app_3865_smoke.txt, launch_3864_appsmoke_retry.txt)
Attempt 1 (3860/3861): the sim CRASHED at startup (parseCmdLine 0xC0000005, pid 59936 -
the 3rd of 8 launches); LaunchVrf52's new detection reported it with the callstack and
exited 3 - it worked. Defect found: the crashed pid LINGERS under MAK's crash handler and
the next launch (3862) was REFUSED by the pre-existing-process precondition (exit 2, not a
crash); executor tasked to close a detected crashed pid (failed its own start). 3860-3863
BURNED. Forensics lane open on the crash (FORENSICS_52_STARTUP_CRASH_2026-09-04).
Retry (3864/3865) after closing the crashed pid by hand: sim READY, joined.
P1 HELD: the app logged "Vrf:ConfigFileIdentity - joining the CONFIG-FILE way: no
--execName, no --fedFileName, FOM modules cleared. Native stack (pre-Start) = 5.2|C:\MAK\
vrforces5.2d\bin64\vrfcontrol.dll." and, after Start(), "VrfBridge native stack = 5.2|...;
ConnectionConfigFile='...MAK-ONE-2025-Config.xml'" - the app federate joined on the
documented posture with the 5.2 binaries.
P2 NOT OBSERVED: the app writes no BackendCount line at this log level (its vocabulary
lacks one) - not a miss, an instrument gap; the tools' 0.1 s discovery on the same posture
stands as the evidence for the bridge. Add a post-Start backend line to the app (small).
P3 as predicted: C2SIM STOMP connect refused (server down), app alive at 60 s, stopped.
Adversarial note: P1 is the executor-flagged gap ("the app must be seen logging 'joining
the CONFIG-FILE way'") closed with a capture; nothing here claims init/order behaviour.
