# RUNBOOK - operating the c2simVRFinterface against VR-Forces

Hard-won runtime procedure. Read this BEFORE any run; do NOT re-derive it (a whole
session was burned rediscovering the pieces below). Companion to START_HERE.md
(build) and PORT.md sec 4 (environment). ASCII-only.

## 0. The single most important rule

NEVER force-kill a JOINED interface (`Stop-Process -Force`, `taskkill /F`). It does
not resign from the RTI, so it leaves a STALE FEDERATE; the next interface start then
HANGS at RTI join (1 thread, ~0 CPU, log frozen at the config banner). The only
recovery for a stale federate is a manual VR-Forces scenario reload in the GUI.
Stop the interface CLEANLY instead (sec 4) - it resigns, leaves no stale federate,
and needs no reload. Force-kill + reload was the old clunky path (pre- and
post-compaction); the clean stop replaces it.

## 1. Environment (verify - do not assume; see PORT.md sec 4)

- VR-Forces running HLA1516e, execName CWIX-2024, siteId 1, sessionId 1
  (`vrfSimHLA1516e` + `rtiexec` processes up).
- C2SIM server container `c2sim-server` up: REST 8080, STOMP 61613 (`docker ps`).
  `docker restart c2sim-server` (~30 s to ActiveMQ-ready) if STOMP has degraded
  across many runs. After a Docker restart the container may rebind 0.0.0.0:8080 itself.
- IPv4 8080 free (stop the COA-GPT `tileserver.py` if it reclaimed it).
- exe env: `QT_QPA_PLATFORM_PLUGIN_PATH=C:\MAK\vrforces5.0.2\bin64\platforms`; PATH must
  include `C:\MAK\makRti4.6.1\bin` (MAK RTI - NOT Pitch/prti1516e) and
  `C:\MAK\vrforces5.0.2\bin64`; launch with cwd = `C:\MAK\vrforces5.0.2\bin64`.
  The repo `runc2simVRFHLApRTI.bat` prepends PITCH RTI - do NOT use it as-is; this
  interface links MAK RTI 4.6.1 (confirmed by its startup log "Using MAK ... RTI 4.6.1").

## 2. Launch command (arg map from main.cxx argv[1..18])

`bin64\c2simVRFHLA1516e.exe <srvIP> <restPort> <stompPort> <clientId> <skipInit> <ibml> <tracking> <vrfAddr> <reportInterval> <blueForce> <debug> <sessionId> <appNumber> <siteId> <obs> <timeMult> <bundle> <federation>`

- Golden STP:  `127.0.0.1 8080 61613 STP   0 0 3 127.0.0.1 0 0 0 1 3201 1 3 0 0 CWIX-2024 0`
- COA-STP1:    `127.0.0.1 8080 61613 C2SIM 0 0 0 127.0.0.1 0 0 0 1 <freshAppNo> 1 0 0 0 CWIX-2024`
- clientId (argv4) MUST equal the init's SystemName (STP init -> STP; COA-STP1 -> C2SIM),
  or the interface creates 0 units.
- appNumber (argv13) must be FRESH each run (a prior run's federate lingers). Increment it.
- debug (argv11) MUST be 0 (debug=1 is broken - PORT.md sec 6).

## 3. Run cycle (ORDER MATTERS)

1. Push the init FIRST, THEN start the interface (documented: PHASE1_REWIRE.md
   Verification step 3; START_HERE Run/verify). The interface late-joins via QUERYINIT
   at startup and creates the units.
   `tools\PushInit\bin\Release\net10.0\PushInit.exe <init.xml>`  -> expect "QUERYINIT: N Units".
2. Start the interface (sec 2). JUDGE CONNECT BY THREAD COUNT, NOT THE LOG: stdout
   redirected to a file is BLOCK-BUFFERED (~4 KB), so the log sits at ~1133 B showing
   only the config banner even after a successful connect. Connected = ~9-10 threads;
   hang-at-RTI = 1 thread / ~0 CPU. Unit creation flushes the buffer (log jumps past ~6 KB).
3. Push the order:
   `tools\PushOrder\bin\Release\net10.0\PushOrder.exe <order.xml> <listen-secs>`
4. Observe: task start/complete lines in the interface log; entity movement in the VR-Forces GUI.

## 4. CLEAN STOP (do this instead of force-kill)

The interface exits and resigns from the RTI when the C2SIM server broadcasts
`systemState == UNINITIALIZED` (C2SIMinterface.cpp:1828 -> `setTimeToQuit(true)` ->
main.cxx:424 loop exit -> `delete facade` -> RTI resign). Drive the server there with
STOP then RESET (NOT INITIALIZE, which would move on to INITIALIZING):
via the SDK, `await sdk.PushCommand(C2SIMCommands.STOP); await sdk.PushCommand(C2SIMCommands.RESET);`
(`PushCommand` is public - C2SIMSSDK.cs:537/556; states enum C2SIMSSDK.cs:35).
TODO: add `tools/StopIface` that does exactly this so the stop is one command.

Corollary: NEVER push a fresh init to a RUNNING interface. `PushInit` calls
`ResetToInitializing` = STOP/RESET/INITIALIZE, and the RESET step's UNINITIALIZED
transient triggers the interface's clean shutdown. That is why a mid-run PushInit drops
the interface to 1 thread - it is RESIGNING, not hanging. (Push init only while NO
interface is running - sec 3.)

## 5. If a federate got stale anyway (after an accidental force-kill)

Symptom: next interface start hangs at RTI join (1 thread, ~0 CPU, log frozen at config).
Recovery: reload the VR-Forces scenario in the GUI (re-creates the federation). This is
the ONLY step that needs the human, and it is only needed because of a prior force-kill.
Avoid it entirely by clean-stopping (sec 4).

## 6. Known runtime blocker (2026-07-09): the C++ STOMP client hangs at connect

Symptom: a FRESH interface run (correct sec-3 push-init-first sequence, healthy broker)
connects to the RTI (~9-10 threads) but NEVER establishes a STOMP connection to 61613
(confirmed: `Get-NetTCPConnection -OwningProcess <pid>` shows no 61613 connection), so it
never late-joins and creates 0 units. Its log is frozen at the config banner (block-buffered,
sec 3) and stderr is empty. This is the "connecting STOMP stream" hang flagged in PORT.md sec 8.

Diagnosed - what it is NOT:
- NOT the push order (sec 3 is correct; coa3 proved it - its log shows RTI -> "connecting STOMP
  stream" -> "SERVER ALREADY RUNNING - REQUESTING LATE JOIN" -> received INIT -> 128 units).
- NOT broker readiness (PushInit's .NET STOMP client works against the same broker seconds before).
- NOT a port shadow: the two 61613 listeners are just Docker Desktop dual-stack forwarding
  (`com.docker.backend` on 0.0.0.0 + [::], `wslrelay` on [::1]); both reach the container.

What it IS (CORRECTED - an earlier NordVPN guess was WRONG and is retracted; loopback 127.0.0.1
never touches a VPN tunnel, and the user's STP connector + the .NET PushInit both connect to
http://127.0.0.1:8080/C2SIMServer and 127.0.0.1:61613/topic/C2SIM fine): the Docker Desktop / WSL2
loopback PORT-PROXY went slow. A raw TCP connect to the loopback ports measured 5-9 SECONDS (should
be <1 ms) - `com.docker.backend` + `wslrelay` had degraded, almost certainly from THIS session
OVER-CHURNING Docker (an unnecessary `docker restart c2sim-server` on top of an earlier full
Docker-recovery). The interface's C++/boost STOMP client cannot ride out that latency, so it stalls
before it even opens the socket (`Get-NetTCPConnection` shows 0 connections for the PID); the .NET
SDK clients tolerate it. The METHOD IS SOUND - this SAME session ran it successfully many times
(transcript: golden trace "initialized 49 units", then "connecting STOMP stream"/"created units"
repeatedly, and after a Docker recovery, coa3 "initialized 128 units").

FIX for a fresh session: do NOT restart the broker as a habit - it was never the problem, and the
restarts are what degraded the proxy. If a raw TCP connect to 127.0.0.1:61613 is not near-instant,
reset the Docker port proxy (restart Docker Desktop, or reboot), confirm loopback is fast, THEN run
sec 3 unchanged. The session transcript (~/.claude/projects/.../a1852c45-...jsonl, around lines
1540-1605) shows the working launch + push + late-join sequence and a prior Docker recovery.

Impact: blocks the LIVE proof ONLY. The aggregate-movement fix (PORT.md sec 10) is validated
independently (MAK `setAggregateFormation` API + valid formation names + clean build) and does
not depend on this run. Its real home is the .NET port (VRF_C2SIM), whose STOMP client is the
.NET SDK - which demonstrably works (PushInit). Decision: do NOT sink more time into the
deprecated-C++ live proof. If a visual is later deemed essential, the next lever is a full
Docker Desktop / container RECREATE (not just restart), which is disruptive.

## 7. Running the .NET PORT (VrfC2SimApp) live - hard-won 2026-07-10

First live bring-up of the .NET port. The C2SIM server had been removed; redeployed from
`Downloads/Docker.zip` (c2sim-docker-4.8.4.9-rev1 + c2simFiles-v3) per its `.docx`:
`docker image load -i c2sim-docker-4.8.4.9-rev1.tar.gz`, untar c2simFiles, then
`docker run -d --name c2sim-server -v "<host>\c2simFiles\c2simFiles":/opt/c2simFiles -p 8080:8080 -p 61613:61613 <imageId>`.
Verify: REST `http://127.0.0.1:8080/C2SIMServer` -> HTTP 200; 8080/61613 open + fast.

LAUNCH ENV that actually works (four things the offline docs got wrong or omitted):
1. **Runtime RTI must be 4.6.1, NOT 4.6b.** VR-Forces' rtiexec is `C:\MAK\makRti4.6.1`
   (`MAK_RTIDIR`/`RTI_RID_FILE` both 4.6.1). The bridge is *built* against 4.6b libs but
   runs fine on 4.6.1 (proven: the app logged "Using MAK ... RTI version 4.6.1" and joined).
   So PATH = `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;...`.
   (The START_HERE/APP.md offline PATH lists 4.6b - fine for `--parse-*` which only LOAD the
   DLLs, WRONG for a live join, which must match the federation's RTI = 4.6.1.)
2. **`MAKLMGRD_LICENSE_FILE` must point at the RENEWED license.** A shell may inherit a STALE
   session value pointing at a now-deleted expired `.lic` -> the RTI/VR-Link license checkout
   HANGS in `bridge.Start()` before any socket (low CPU, threads decreasing, 0 connections).
   Fix: `$env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')`.
3. **cwd must be `C:\MAK\vrforces5.0.2\bin64`** (as for the C++ interface) so Legion finds
   `vrfLegion.lua` + terrain data. Wrong cwd -> `FATAL[Legion] ... vrfLegion.lua ... No such file`
   then an SEHException. Since the .NET host loads appsettings from cwd, pass
   `--contentRoot="<exe dir>"` so config still loads while cwd = VRF bin64.
4. **FED file + FOM modules MUST match VR-Forces**, else `bridge.Start()` crashes `0xC0000005`
   after "addInteractionCallback - bad class name: Data/RadioSignal.*/Comment" (missing FOM
   class handles). Set in appsettings `Vrf`: `FedFileName=RPR_FOM_v2.0_1516-2010.xml`,
   `FomModules=[MAK-VRFExt-6_evolved.xml, MAK-DIGuy-7_evolved.xml, MAK-LgrControl-2_evolved.xml]`
   (all resolve from VRF bin64). Read VR-Forces' own `--fedFileName/--fomModules` off its
   command line if they differ. Use a FRESH `Vrf__ApplicationNumber` each run (stale-federate).

With all four, the app JOINS HLA (RTI ports established, no crash) and logs "Connected to
C2SIM". Clean stop: `tools/StopIface` drives the server STOP->RESET->UNINITIALIZED (the
RUNBOOK sec-4 tool, now built) - the interface is meant to catch UNINITIALIZED and resign.

PORT GAPS found + FIXED this session (the app now runs live end-to-end):
- **STOMP receive works** - the earlier "receives nothing" was a MISDIAGNOSIS. `tools/StompProbe`
  (subscribe + hook every event) proved the SDK receives the init + status broadcasts fine, with
  BOTH the app's `1.0.2` and the tools' `CWIX2024v1.0.2` settings. The app only *looked* dead
  because of the three real gaps below (it doesn't log raw/received messages).
- **No late-join (FIXED).** The app only subscribed to FUTURE broadcasts; with push-init-first it
  created 0 units. FIX: after `_sdk.Connect()`, call `_sdk.JoinSession()` (REST QUERYINIT) and feed
  the result through `ProcessInitialization`. Verified live: "late-join QUERYINIT ... 49 units".
- **Parsers assumed `<MessageBody>` root (FIXED).** The SDK's live events deliver the BARE inner
  body (`<C2SIMInitializationBody>`, `<OrderBody>`), but InitParser/OrderParser (tested on FILES)
  expected the full envelope -> 0 units / no task on live events. FIX: try the envelope, then the
  bare body directly (both body types carry `[XmlRoot]`). Verified: init + order both parse live.
- **Empty status body (FIXED).** The STOMP status broadcast body is empty `<SystemMessageBody/>`
  and the header has no state, so `OnStatusChanged`'s `e.Body.Contains("UNINITIALIZED")` NEVER
  matched -> no clean stop. FIX: treat the event as a trigger and read the real state via REST
  `GetStatus()` (== `C2SIMServerStatus.UNINITIALIZED`). Verified: StopIface -> app resigns clean,
  rtiexec back to 2 (no stale federate).
- Also aligned appsettings `C2SIM` to the proven tool values: `ProtocolVersion=CWIX2024v1.0.2`,
  `RestPassword=v0lgenau` (for the REST GetStatus/QUERYINIT/report-push calls).

ONE REMAINING ITEM (does not block the pipeline): tasking `14.MechBn` (the golden order) ABANDONS
at point 0 - `TryGetEntityGeodetic` returns false for that DISAGGREGATED aggregate (the known
"frozen" unit, PORT.md sec 5). Everything upstream works (order received -> parsed -> taskee
resolved to its VRF uuid). Likely the disaggregated-aggregate reflection edge case (the port's
dynamic_cast/aggregateStateRep may be stricter than the C++ static_cast, or the reflected object
isn't available yet) - next: try an ENTITY-level taskee to isolate aggregate-vs-timing, and
compare against the C++ static_cast behavior for a disaggregated set.
