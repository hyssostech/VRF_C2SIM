# REHEARSAL - WAY B ON THE HAND-STARTED DEMO PATH (DEMO_RUNBOOK secs 1-4), FIRST RUN (light registration)

STATUS: REGISTERED 2026-10-05; NOT RUN. Light registration under RL-20261005-01 (standing go-live, light registration,
shared resources agreed with the other sessions first). The seat runs it; this lane prepared it and launched nothing.
Predecessor: REHEARSAL_WAYB_BOUNDED_2026-10-05.md (Way A: the runner played the hand path's part; B1-B7 HELD). This run
tests what that one did not: the operator's own command sequence in docs/DEMO_RUNBOOK.md secs 1.1-1.6, 2.1-2.4 and 4,
on the Demo profile (no runner, no --env), with PushInit/PushOrder standing in for STP on the PRIVATE server.

## Registration

PREREG ID: REHEARSAL_WAYB-2026-10-05-3
DATE (UTC): 2026-10-05, before any launch (the commit stamp is authoritative)
BINARY / COMMIT: the main-checkout deploy recorded by RUNBOOK sec 9 "OVERLAY+X9 DEPLOY 2026-10-05 17:34Z" (main 522880c =
the Demo overlay a73ede5 + X9 60206b2; doc fix a008607). Read read-only by this lane at 17:41-17:45Z from
src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\ of the main checkout:
    VrfC2SimApp.exe        ProductVersion 1.0.0+git.522880c.Release-5.2 (not DIRTY), 162,304 B
                           sha256 cf931da41357decd9fd8255e0f8e2afc60646376604ea250a3e52d18a2b71545  (= the deploy line)
    VrfC2SimApp.dll        2,343,936 B
                           sha256 3ef97350734dcee50fb7263be95a945a5340d7537e85f6d982677f192d38e6aa  (= the deploy line)
    appsettings.Demo.json  25,700 B, sha256 0cfea21b5fd2ebe307e6c383f137af8ee740a0dc4f9d2c9caa991967801840d2
                           (= src\VrfC2SimApp\appsettings.Demo.json at a008607; carries "ModelSet": "AggregateTacticalLevel")
    appsettings.json       sha256 6efdbc6418ad08e39e16764a90d4592eee0d69f26279490fd133f0eb99391e93 (= src at a008607)
    VrfBridge.dll          sha256 9e8c96b5d20e1d4a0c8c4da404d23d1d8097470b8b21bbc1740fb3733b46b577 (the pin)
If any of these differs at step S2, NO launch: re-register.
TIER AND GATE: STANDARD / PREREG (light)
RUN KIND: movement

VENDOR CITATION: as REHEARSAL_WAYB_BOUNDED_2026-10-05 (UG52 13.2 Table 21 p363, 21.2.3 p473-474; MAK ONE 2025 Adding Content
7.8 p234-236; UG52 4.6.1 "Disabling the Quit Prompt", 4.3.1 "Show Session Terrain Change Prompts"); UG52 Table 10 p164 /
Table 11 p178 (--appDataDir, LaunchVrf52's header).

OWN-RECORD CITATION: RL-20261004-04, -05; RL-20261005-01, -02, -03; RL-20260927-06 (above BN is aggregate-only);
REHEARSAL_WAYB_BOUNDED_2026-10-05 Result (B1-B8); RUNBOOK sec 9 "OVERLAY+X9 DEPLOY"; DEMO_RUNBOOK secs 1-4 (2026-10-05
rewrite); PREREG_D5_WAYB_2026-09-21 (D5c, the one earlier live hand-started sequence, on Mojave); CLAUDE.md sec 5.

## Application numbers - every federate join, from the ledger

The demo block 9101-9199 the runbook types is outside the ledger, and 9101/9102/9103/9190 were already joined in D5, D5b,
D5c and D5d (OPUS_EXECUTION_PLAN.md App. B, the 2026-09-21 D5 entries). This run takes a FRESH LEDGER number for every
join. Marker read 17:41-17:45Z: `*** NEXT FREE: 5344 ***`. EVERY script on the path takes its number by parameter:

| # | Federate (process) | Joins when | Parameter that carries the number | Number |
|---|---|---|---|---|
| 1 | LaunchVrf52's own STP-825 holder, attempt 1 (tools\RtiProbe.exe, detached, 900 s, never killed) | always (holder ON by default) | `-FederationHoldAppNumber 5344` | 5344 |
| 2 | the same holder, attempt 2 (retry on a refused create) | only if attempt 1 is refused | none - the script uses `-FederationHoldAppNumber + 1` itself | 5345 |
| 3 | back end vrfSimHLA1516e | always | `-BackendAppNumber 5346` | 5346 |
| 4 | front end vrfGui | always (GUI on) | `-FrontendAppNumber 5347` | 5347 |
| 5 | the interface VrfC2SimApp | always | StartInterface52 `-AppNumber 5348` (exported as Vrf__ApplicationNumber; the profile's 9101 is the fallback) | 5348 |

BLOCK TO CLAIM BEFORE S8: 5344-5348, marker 5344 -> 5349. Unconsumed numbers (5345 on a first-attempt join) are BURNED.
NOT federates (no number): StartRtiExec52 (rtiexec is the RTI, not a federate), PushInit, PushOrder and StopIface (C2SIM
REST/STOMP only), StopVrf52, Enable-LabelDecoration, StopIface --dry-run. Not on this path: StartFederationHolder52 (1.3) -
the day's holder RtiProbe 41292 on 5329 is up until about 20:05:56Z (12:05:56Z + 28,800 s), so 1.3 is not re-run; no WatchVrf
(not part of the demo path - see Evidence). Ledger text for the seat to append to App. B before S8:
    - 2026-10-05 REHEARSAL_WAYB-2026-10-05-3 (docs/experiments/REHEARSAL_WAYB_HAND_2026-10-05.md), the hand-started Way B
      path, every join ledgered: 5344 = LaunchVrf52 -FederationHoldAppNumber (its STP-825 holder, attempt 1); 5345 = that
      holder's automatic retry (+1), consumed only on a refused create; 5346 = back end; 5347 = vrfGui; 5348 = VrfC2SimApp
      (StartInterface52 -AppNumber). PushInit/PushOrder/StopIface join no federation. CLAIMED. Marker 5344 -> 5349.

## Conditions

CONSOLE LEVEL: -1 (the Demo profile's ObjectConsoleNotifyLevel / ObjectConsoleMemberNotifyLevel)

DEVIATION FROM RECORD: departing from "CONSOLE LEVEL: 4" - this run tests the Demo profile as shipped, which keeps the
vendor default -1 (DEMO_RUNBOOK 2.2, "object-console levels 4 / 4 ... the profile keeps -1"). Nothing here scores on the
object-console channel.

PRE-ORDER GATE: none (the bounded rehearsal's and G1-6's)

DEVIATION FROM RECORD: departing from "--pre-order-gate" - the hand path has no runner; the operator's gate is the interface's
own "READY TO TASK - N of N init unit(s) bound" line (DEMO_RUNBOOK 2.3), which the seat waits for before PushOrder.

DurationScale: 0.25 ($env:Vrf__DurationScale = '0.25' in the interface's shell, DEMO_RUNBOOK 2.2 PACE)

DEVIATION FROM RECORD: departing from the Demo profile's "DurationScale": 1.0 - the demo pace is the owner's open choice
(DEMO_RUNBOOK 2.2 PACE); this run sets 0.25 per the runbook so it compares with the bounded rehearsal and G1-6 (all ran 0.25).

ARMED ENDS VS STALL WINDOW: the bounded rehearsal's - the watchdog on the SIMULATION clock (StallClock sim, now from the Demo
profile, not --env), 360 SIM s, 50 m, StallDetection true. Holds (T1, T2, T10, T11, T13) have no destination and end on their
scaled Durations (300 SIM s; T10 450 SIM s per the bounded Result); T14's mover arrived about 640 SIM s after dispatch there.

C2SIM SERVER: PRIVATE, REST http://127.0.0.1:18080/C2SIMServer, STOMP http://127.0.0.1:61614/topic/C2SIM (container
c2sim-server-vrf, "Up 21 hours" at 17:41-17:45Z). StartInterface52 -Server private; -ClientId "Not Set" (the raw export's
SystemName). The standard server (8080/61613) is not touched.

ORDER CONTENT: data/STP-IRON-STORM-SYNTHETIC_Initialization.xml and _Order.xml, unmodified (the bounded rehearsal's pair).
FIXTURE: IronStorm_Centre_52_Aggregate (present under C:\MAK\vrforces5.2d\userData\scenarios at 17:41-17:45Z).
appData: C:\C2SIM\vrf-appdata-unattended\appData (present at 17:41-17:45Z).
SHARED RESOURCES (RL-20261005-01): rtiexec 65540, rtiForwarder 58844, holder RtiProbe 41292 (5329) - never killed; the
c2sim-server-vrf container - never reset except by this run's own PushInit and StopIface; quiet machine agreed with the other
sessions before S8. At 17:41-17:45Z no vrfSim / vrfGui / VrfC2SimApp / WatchVrf was running; 41292 was the only RtiProbe.
TIME WINDOW: start S8 no later than about 19:20Z so teardown ends before 41292's hold expires (about 20:05:56Z). If 41292 is
gone at S1: STOP - 1.3 needs four new ledger numbers; amend this registration first.

## Sequence - copy-paste, in order (PowerShell 7, from the MAIN checkout root
F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM)

Each line is one command. Nothing carries over between lines (no shell variables); every log goes to
runs\launch52\wayb-hand-3\ (git-ignored). The runbook's `^` continuations (2.4, 4.1) are cmd syntax and are NOT used here.

S0 CLAIM (the seat, before S8): append the ledger text above; marker 5344 -> 5349.

S1 (1.1 quiet machine)
    Get-Process vrfSim*, vrfGui*, VrfC2SimApp, WatchVrf -ErrorAction SilentlyContinue
      SEE: nothing.
    Get-Process -Id 65540, 58844, 41292 | Select-Object Id, ProcessName
      SEE: rtiexec, rtiForwarder, RtiProbe. 41292 missing = STOP (Conditions, TIME WINDOW).
    Get-ChildItem Env:Vrf__*, Env:C2SIM__*, Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue
      SEE: nothing.
    New-Item -ItemType Directory -Path 'runs\launch52\wayb-hand-3'
      SEE: created. Already exists = STOP (another writer).

S2 (0.2 build, read-only)
    (Get-Item src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.exe).VersionInfo.ProductVersion
      SEE: 1.0.0+git.522880c.Release-5.2
    Get-FileHash src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.exe, src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.dll, src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\appsettings.Demo.json -Algorithm SHA256 | Format-List Hash, Path
      SEE: the three hashes of the Registration. Any other = STOP.
    Select-String -Path src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\appsettings.Demo.json -Pattern '"ModelSet": "AggregateTacticalLevel"'
      SEE: exactly one line.

S3 (1.2)
    pwsh -NoProfile -File scripts\StartRtiExec52.ps1 *> runs\launch52\wayb-hand-3\s3-startrtiexec.log; $LASTEXITCODE
      SEE: "ALREADY UP ... NOTHING was started", 0.

S4 (1.3) NOT RUN - holder 41292 (5329) holds the federation today (Conditions).

S5 (1.4 labels)
    pwsh -NoProfile -File tools\display\Enable-LabelDecoration.ps1 -Verify; $LASTEXITCODE
    pwsh -NoProfile -File tools\display\Enable-LabelDecoration.ps1 -AppDataDir 'C:\C2SIM\vrf-appdata-unattended\appData' -Verify; $LASTEXITCODE
      SEE: "ON" for 5/Ground and 5/Default, 0, both.

S6 (1.5 server; read-only)
    curl.exe -s -o NUL -w "%{http_code}" http://127.0.0.1:18080/C2SIMServer
      SEE: 200.
    tools\StopIface\bin\Release\net10.0\StopIface.exe http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM --dry-run *> runs\launch52\wayb-hand-3\s6-server-state-before.log
      SEE: "before      : <state>" and "DRY RUN - nothing will be sent." RECORD the state (H2 reads it).

S7 (1.6) NOT RUN - no reboot since the bounded rehearsal's launch at 12:06Z (check: (Get-CimInstance Win32_OperatingSystem).LastBootUpTime).

S8 (2.1 VR-Forces with the GUI) - dry run, then the launch (timeout 600 s)
    pwsh -NoProfile -File scripts\LaunchVrf52.ps1 -Scenario IronStorm_Centre_52_Aggregate -BackendAppNumber 5346 -FrontendAppNumber 5347 -FederationHoldAppNumber 5344 -AppDataDir 'C:\C2SIM\vrf-appdata-unattended\appData' -DryRun *> runs\launch52\wayb-hand-3\s8-launchvrf52-dryrun.log; $LASTEXITCODE
      SEE: 0; the plan names holder appNumber 5344 (retry 5345), back end 5346, front end 5347, the unattended appData.
    pwsh -NoProfile -File scripts\LaunchVrf52.ps1 -Scenario IronStorm_Centre_52_Aggregate -BackendAppNumber 5346 -FrontendAppNumber 5347 -FederationHoldAppNumber 5344 -AppDataDir 'C:\C2SIM\vrf-appdata-unattended\appData' *> runs\launch52\wayb-hand-3\s8-launchvrf52.log; $LASTEXITCODE
      SEE, in order: "federation HELD: holder pid <p> (appNumber 5344) joined ... on attempt 1/2"; "vrfGui teardown prompts are
      OFF in C:\C2SIM\vrf-appdata-unattended\appData"; NO "[WARN] LABEL DECORATION NOT ON"; "back-end started (pid N)";
      "scenario LOAD CONFIRMED"; "READY: 5.2d back-end HEALTHY"; exit 0. 4 = a dialog is waiting (answer, never kill);
      3 or 2 = STOP, section 4.
    Get-Process vrfSimHLA1516e | Select-Object Id, @{n='StartUtc';e={$_.StartTime.ToUniversalTime().ToString('o')}} | Format-List | Out-File -FilePath runs\launch52\wayb-hand-3\s8-backend.txt -Encoding ascii
      RECORD N and its start time (S14 needs both).

S9 (2.2 the interface, Demo profile, BACKGROUND, its console to a file)
    Get-ChildItem Env:Vrf__*, Env:C2SIM__* -ErrorAction SilentlyContinue
      SEE: nothing.
    $env:Vrf__DurationScale = '0.25'; pwsh -NoProfile -File scripts\StartInterface52.ps1 -ClientId "Not Set" -Server private -AppNumber 5348 -WhatIf *> runs\launch52\wayb-hand-3\s9-interface-whatif.log; $LASTEXITCODE
      SEE: 0; the environment lines Vrf__ApplicationNumber = 5348, Vrf__ClientId = Not Set, DOTNET_ENVIRONMENT = Demo;
      "*** C2SIM SERVER THE INTERFACE WILL LISTEN TO: rest=http://127.0.0.1:18080/C2SIMServer  stomp=http://127.0.0.1:61614/topic/C2SIM ***".
    $env:Vrf__DurationScale = '0.25'; pwsh -NoProfile -File scripts\StartInterface52.ps1 -ClientId "Not Set" -Server private -AppNumber 5348 *> runs\launch52\wayb-hand-3\s9-interface.log
      RUN IN THE BACKGROUND (it runs until S12). Then poll the log (Select-String, no pipe into the process) until the READY
      line of H1 appears; READY absent after 180 s, or "DEMO EXTENT NOT ARMED" / "-> EntityLevel" = STOP, section 4.

S10 (2.4 stand-in for STP: the initialization, PRIVATE pair)
    tools\PushInit\bin\Release\net10.0\PushInit.exe data\STP-IRON-STORM-SYNTHETIC_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM *> runs\launch52\wayb-hand-3\s10-pushinit.log; $LASTEXITCODE
      SEE: 0, "QUERYINIT   : <n> Units, SystemName=[Not Set]". Then poll s9-interface.log for "READY TO TASK" (H2).
      "Server UNINITIALIZED; initiating clean stop." in s9-interface.log = H2's falsifier: the interface has left - STOP, section 4.

S11 (2.4 the order, after READY TO TASK; note the wall time first)
    (Get-Date).ToUniversalTime().ToString('o') | Out-File -FilePath runs\launch52\wayb-hand-3\s11-order-t0.txt -Encoding ascii
    tools\PushOrder\bin\Release\net10.0\PushOrder.exe data\STP-IRON-STORM-SYNTHETIC_Order.xml 60 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM *> runs\launch52\wayb-hand-3\s11-pushorder.log; $LASTEXITCODE
      SEE: 0. T0 = the order's arrival in s9-interface.log (the bounded run: PushOrder returned about 30 s after the order).
    Screenshots of the vrfGui main window ONLY (PrintWindow on its handle, no input sent; the seat's helper of the bounded
    rehearsal) at T0 + 2 min -> runs\launch52\wayb-hand-3\shot_t02.png and T0 + 8 min -> shot_t08.png.
    HOLD THE WINDOW until T2's TASKCMPLT is in s9-interface.log, and at least until the +8 min shot; cap T0 + 25 wall-min.

S12 (4.1 the interface first - StopIface, not Ctrl+C: the interface runs in the background with its console in a file)
    tools\StopIface\bin\Release\net10.0\StopIface.exe http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM --yes *> runs\launch52\wayb-hand-3\s12-stopiface.log; $LASTEXITCODE
      SEE: "after RESET : UNINITIALIZED", 0; s9-interface.log ends with "Server UNINITIALIZED; initiating clean stop." and the
      background job exits 0.

S13 (4.2 VR-Forces)
    pwsh -NoProfile -File scripts\StopVrf52.ps1 *> runs\launch52\wayb-hand-3\s13-stopvrf52.log; $LASTEXITCODE
      SEE: 0 (down), or 3 (the back end ignored the graceful close, NOTHING killed - both 2026-10-05 runs' back ends did).

S14 (4.2, only on S13 exit 3: force ONLY S8's back end, by pid AND start time from s8-backend.txt)
    pwsh -NoProfile -File scripts\StopVrf52.ps1 -ForceOwnBackendPid N -ForceOwnBackendStartUtc '<StartUtc from s8-backend.txt>' *> runs\launch52\wayb-hand-3\s14-stopvrf52-force.log; $LASTEXITCODE
      SEE: 6. 7 / 8 / 5 = record, call it in the Result (8 = crash: the .callstack.log / .dmp, never the vendor .log).

S15 (4.3)
    Get-Process vrfSim*, vrfGui*, VrfC2SimApp -ErrorAction SilentlyContinue
      SEE: nothing.
    Get-Process -Id 65540, 58844, 41292 | Select-Object Id, ProcessName
      SEE: all three. LaunchVrf52's own holder RtiProbe (5344) may still be up until its 900 s end - EXPECTED, never kill.
    Get-ChildItem C:\MAK\logs -Include *.callstack.log, *.dmp -Recurse | Where-Object LastWriteTimeUtc -gt ([datetime]'2026-10-05T17:30:00Z').ToUniversalTime() | Select-Object FullName, LastWriteTimeUtc
      SEE: nothing (read-only; H10).

S16 (4.4 labels after the first GUI exit) - the two S5 lines again. SEE: ON, 0, both.

S17 the seat annotates the S0 ledger entry (consumed / burned per number; marker stays 5349) and writes the Result here.

## Evidence the seat captures

- s9-interface.log: the interface's whole console (StartInterface52 writes NO log file of its own - it runs the exe in its
  console; the runner gets vrfc2simapp.log the same way, by redirecting stdout). The scoring source for H1-H7.
- s8-launchvrf52.log (+ the vendor-log copy LaunchVrf52 harvests to runs\launch52\vrfSim_5346_<stamp>.log - it holds the
  process environment in cleartext: read locally, quote nothing, never attach).
- s10/s11 push logs, s6 server state, s12-s14 teardown logs, s8-backend.txt, s11-order-t0.txt.
- shot_t02.png, shot_t08.png (the vrfGui window only).
- NO WatchVrf trace: it is not on the demo path, and by hand it needs the runner's RTI environment (PATH prefix, MAK_*,
  RTI_RID_FILE, RTI_ASSISTANT_DISABLE, cwd bin64, the federation argument) - its own risk for a light rehearsal. Back-end
  liveness is read from the interface (position reports every 10 s; an STP-822 back-end-lost line would say so).

## Expectations (RECORDED, not gated)

| # | Expectation | Confidence | Measured |
|---|---|---|---|
| H1 | s9-interface.log carries 2.2's lines: "*** C2SIM SERVER THE INTERFACE WILL LISTEN TO: rest=http://127.0.0.1:18080/C2SIMServer  stomp=http://127.0.0.1:61614/topic/C2SIM ***"; "MODEL SET RULE (D2b; ...): Vrf:ModelSet='AggregateTacticalLevel' -> AggregateTacticalLevel"; "DEMO EXTENT ON (Vrf:DemoExtent, RL-20261005-02): ... + 2 km margin"; the TASK CLOCK line with "Vrf:DurationScale=0.25"; "READY - joined the federation, 1 VR-Forces back-end(s), type mapping = FidelityTable" - all from the Demo profile with only Vrf__DurationScale set by hand | HIGH | |
| H2 | PushInit (exit 0) does NOT stop the running interface: no "Server UNINITIALIZED; initiating clean stop." before S12; then "READY TO TASK - N of N init unit(s) bound" (N = 36 expected) and 36 empty, labelled containers on the GUI. RECORDED: S6's server state before (PushInit's ResetToInitializing sends STOP, RESET, INITIALIZE unless the server is already INITIALIZING, and the interface stops on UNINITIALIZED, VrfC2SimService.cs:1517-1520 - a race; D5c passed it once) | MEDIUM | |
| H3 | At the order: one "DEMO EXTENT ... 23 task(s) in this order, 16 refused" summary; 16 TASKABRT "OUT OF DEMO EXTENT" within about 1 s (T3-T9, T12, T15, T16, T18-T23); T17 SKIPPED (predecessor T16 refused); the D2b rule ALLOWS the order (no "ORDER REFUSED") | HIGH | |
| H4 | POPULATE IN PLACE three times only: 28ID 1, 1-112 IN 5, 48 IBCT 17 = 23 members, each "N of N created"; nothing else populated | HIGH | |
| H5 | T14 (48 IBCT) moves about 2.8 km along STP's line (Auto planner, navigate-to-location to its 17 members), visibly between shot_t02 and the end, and reaches TASKCMPLT; T1, T10, T11, T13 run as holds | MEDIUM | |
| H6 | X9 LIVE: at order receipt "Task '<T2>': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25)"; T2 dispatches 300 SIM s after T1's TASKCMPLT (T1 ends about 300 SIM s after the order), never within seconds of it (the 2026-10-05 00:30Z run: 44 ms = unread), then completes on its scaled Duration. Wall time is RECORDED with the app's SIM/WALL lines: about 5 wall-min only if the sim runs 1x; the bounded run measured 3.0-7.4x, i.e. about 40-100 wall s | MEDIUM (first live X9) | |
| H7 | The clean designator on screen (RL-20261005-03): "48 IBCT/28ID" on the container, "48 IBCT/28ID.<member>" (e.g. ".HQ1") on members, no "__" suffix text (shot_t02) - the first live look | MEDIUM | |
| H8 | THE GUI EXITS UNATTENDED: StopIface 0 ("after RESET : UNINITIALIZED"); the interface exits 0; StopVrf52 0, or 3 then the forced S14 6; no "Session Status" / "Are You Sure?" modal; no vrfGui left (S15) | MEDIUM | |
| H9 | Label still ON after the GUI exit in the unattended tree AND the vendor tree (S16 -Verify, 0 both) | HIGH | |
| H10 | No crash: S8 exit 0, no back-end loss line in s9-interface.log, interface exit 0, no new .callstack.log / .dmp in C:\MAK\logs (S15), rtiexec 65540 / rtiForwarder 58844 / holder 41292 alive throughout | MEDIUM | |

OPTIONAL EXTRA (T14 route display, displayRoute): NOT run. displayRoute is a code constant (false) in
src/VrfC2SimApp/AggregateMovePlanner.cs:194 (NavigateVars), not a setting - showing the route needs a code change. Open point.

STOP RULE: none gates the window except a crash, H2's falsifier (the interface leaves at PushInit) and the S1/S2 preconditions.
Any of those = section 4 teardown, record, no retry under this ID (a retry takes new numbers and a new registration).

## Open points (for the seat and the runbook)

- OP1 APPLICATION NUMBERS. Every script already takes its number by parameter (LaunchVrf52 -BackendAppNumber, -FrontendAppNumber,
  -FederationHoldAppNumber and its implicit +1 retry; StartInterface52 -AppNumber; StartFederationHolder52 -AppNumbers) - none
  needs a code change. The runbook's fixed numbers are the problem: 2.1 types 9102/9103 and leaves the holder on its default
  9190/9191; 2.2's command omits -AppNumber, so StartInterface52's default 9101 (and the profile's 9101) is reused every run.
  Proposed runbook change: before 2.1, claim five numbers from a marker; 2.1 adds -FederationHoldAppNumber h (claim h AND h+1);
  2.2 adds -AppNumber i. Which marker the demo uses (the engineering ledger, or a demo ledger with its own NEXT FREE in
  9101-9199) is the owner's choice.
- OP2 `^` CONTINUATIONS. 2.4 and 4.1 continue lines with `^` (cmd) while the runbook says PowerShell 7: in pwsh the `^` is a
  literal argument (PushInit would take it as the REST URL) and the next line runs alone. Make them single lines or backticks.
- OP3 INTERFACE-FIRST vs INIT-FIRST. RUNBOOK sec 3 says push the init FIRST, then start the interface; DEMO_RUNBOOK 2.2-2.3 starts
  the interface first, and PushInit resets the server while it listens (H2). H2's result decides whether 2.3 needs a step.
- OP4 STALE RUNBOOK TEXT after the OVERLAY+X9 deploy: 0.2 expects 1.0.0+git.f58de23; 2.2 says the profile "is not deployed".
- OP5 StartInterface52 writes no log file; a hand operator captures the console by redirect, and then stops with StopIface.
- OP6 X9's wall time depends on the sim rate (H6): the brief's "about 5 wall-min" holds only at 1x.
- OP7 No script on the path has a -Help switch; StartInterface52 carries comment help (Get-Help), the others header comments.

## Result

(not run)
