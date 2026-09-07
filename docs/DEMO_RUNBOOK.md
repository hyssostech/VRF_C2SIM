# DEMO RUNBOOK - the C2SIM <-> VR-Forces 5.2d interface, standalone (no test harness)

Status 2026-09-06: DRAFT, written before the first standalone run (DEMO_READINESS row 7). The
steps are the ones the test runner performs, reduced to what an operator needs. Verify each
"expect" line on the first standalone run and correct this file the same day.

## 0. Prerequisites (once per machine)
- VR-Forces 5.2d, VR-Link 5.10, MAK RTI 5.0.1 installed under C:\MAK; the MAK licence valid
  (the current node-locked DEMO licence lapses 2026-09-15 - renew via MAK Sales before any demo
  after that date). Check: `lmutil lmstat -a -c <licfile>` shows vrforces + vrlink features.
- .NET 10 runtime; the interface built once: `dotnet build src\VrfC2SimApp -c Release
  -p:BridgeConfig=Release-5.2` (needs the native VrfBridge Release-5.2 build present).
- Network: the C2SIM server STP talks to (REST + STOMP; appsettings.json C2SIM section);
  internet for the fixture's "MAK Earth (online)" terrain, or an offline terrain (see 4).
- The first VR-Forces start after a REBOOT may show the MAK RTI "Choose RTI Connection"
  dialog: pick the rtiexec connection once (RUNBOOK 0.5.3/0.5.4). The 5.2 profile here is
  assistant-free, so with the rid below the dialog should not appear at all.
- Nothing from a previous session may still be running: `Get-Process vrfSim*,vrfGui*,
  VrfC2SimApp` must be empty (a leftover back end blocks the launch - RUNBOOK 0.5.0).
  rtiexec / rtiForwarder MAY stay up between sessions and must NEVER be killed.
- DEPLOYMENT SMOKE TEST (2026-09-07, do this first on any new machine): from the deployed
  folder, `set DOTNET_ENVIRONMENT=Demo` then `VrfC2SimApp.exe --runtime-check`. Expect
  "MakRuntime: PATH prefixed with ...", "VrfBridge loaded; native stack = 5.2|C:\MAK\
  vrforces5.2d\bin64\vrfcontrol.dll" and "runtime-check: OK", exit 0. It applies the app's
  own MAK runtime settings (appsettings.Demo.json: VrfHome / VrLinkHome / RtiHome / RidFile),
  binds the bridge and exits without joining anything; a wrong install path fails loudly.

## 1. Start order (three consoles)
1. rtiexec (once; survives sessions): `pwsh -File scripts\StartRtiExec52.ps1`
   expect: "rtiexec READY ... TCP 4001".
2. VR-Forces with the GUI (the audience's window):
   `pwsh -File scripts\LaunchVrf52.ps1 -Scenario R9_Mojave_Empty_52 -BackendAppNumber 9201
   -FrontendAppNumber 9202` (demo appNumbers; the test ledger uses 4xxx)
   expect: "VR-Forces READY"; the GUI shows an empty Mojave scenario, sim clock RUNNING.
   (The fixture is the test one - fixed-frame-run-to-complete; for a real-time demo build a
   real-time fixture with tools/FixtureGen and name it here - DEMO_READINESS row 10.)
3. The interface: either `set DOTNET_ENVIRONMENT=Demo` + `set Vrf__ClientId=<SystemName of
   STP's init>` + `VrfC2SimApp.exe` from the deployed folder (the exe prepares its own MAK
   runtime since 2026-09-07 - MakRuntime lines first), or the convenience script
   `pwsh -File scripts\StartInterface52.ps1 -ClientId <SystemName>` which sets the same.
   expect (its console): "MakRuntime: ..." lines, "Connected to C2SIM (...) clientId=<...>",
   then the join lines and "READY" / waiting for the initialization.
4. STP pushes the Initialization: expect "CreationPolicy=AtOrder (C13): N unit(s) created as
   EMPTY shells" (every unit is a shell until an order names it - the whole ORBAT displays,
   only the COA's units get vehicles), "DeStack (R8): N units at (...) spread onto 700 m
   rings" for units STP placed on one coordinate (C14), one "PLACEMENT: UNIT <name>" line per
   unit, and the units appearing in the GUI within ~30 s as unit icons spread over an
   assembly area (a brigade's worth of units on 700 m rings spans a few km).
5. STP pushes Orders: expect "MATERIALIZE <unit> (task ...)" lines (the referenced units get
   their members: a company expands into its platoons, an HQ section is re-created as its
   template), "CreateRoute ... move deferred to route-created" then "MoveAlongRoute issued"
   per task; units move in the GUI; STP receives position reports every 10 s and a
   task-status (TASKCMPLT) report per completed task. A completion comes from either the
   vendor ("VRF task complete: <unit>") or, when the vendor waits on a straggling member,
   from the interface's own arrival evidence ("ARRIVAL EVIDENCE: <unit> ... N/M member(s)
   within 500 m of the last vertex", C15) - one TASKCMPLT per task either way. Successor
   tasks wait up to 2 h (TaskPredecessorTimeoutSeconds) for their predecessor; a 28 km leg
   takes 30-60 min of sim time.

## 2. Stop order
Ctrl+C the interface (it resigns from the federation), close VR-Forces (GUI File > Exit, or
`scripts\StopVrf52.ps1`), leave rtiexec running.

## 3. Reset between demo runs (DEMO_READINESS row 9 - verify)
Reload the scenario in the GUI (File > Open, the same fixture) or `tools\ResetVrf`; restart the
interface (step 3) so its unit map is empty; push the init again. Do not push a second init
into a running interface - it ignores duplicates by uuid (a guard, not a reset).

## 4. Known traps
- PATH: no longer a trap when the Demo overlay is in place - the exe prefixes its own PATH
  from appsettings.Demo.json (MakRuntime) and `--runtime-check` proves it. Without the
  overlay (no DOTNET_ENVIRONMENT=Demo, empty VrfHome keys) a bare `VrfC2SimApp.exe` still
  fails at the first bridge call ("procedure imported by VrfBridge.dll could not be loaded").
- A stale build given a switch it does not know used to fall through and START the interface;
  since 2026-09-07 an unknown "--switch" exits 2 without starting (host "--Key=Value" switches
  pass).
- The console stream: the demo overlay keeps the VR-Forces object consoles OFF
  (ObjectConsoleNotifyLevel -1). Turning them on writes every console line into the
  interface's log (1.2 GB overnight at scale) - a diagnostic setting, never a demo one.
- clientId vs SystemName: they must be equal or the interface creates nothing (RUNBOOK sec 2).
- Terrain: MAK Earth (online) needs internet; without it VR-Forces stalls on terrain load.
- Two interfaces on one network need different -AppNumber values.
