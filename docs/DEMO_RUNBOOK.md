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

## 1. Start order (three consoles)
1. rtiexec (once; survives sessions): `pwsh -File scripts\StartRtiExec52.ps1`
   expect: "rtiexec READY ... TCP 4001".
2. VR-Forces with the GUI (the audience's window):
   `pwsh -File scripts\LaunchVrf52.ps1 -Scenario R9_Mojave_Empty_52 -BackendAppNumber 9201
   -FrontendAppNumber 9202` (demo appNumbers; the test ledger uses 4xxx)
   expect: "VR-Forces READY"; the GUI shows an empty Mojave scenario, sim clock RUNNING.
   (The fixture is the test one - fixed-frame-run-to-complete; for a real-time demo build a
   real-time fixture with tools/FixtureGen and name it here - DEMO_READINESS row 10.)
3. The interface: `pwsh -File scripts\StartInterface52.ps1 -ClientId <SystemName of STP's init>`
   expect (its console): "Connected to C2SIM (...) clientId=<...>", then the join lines and
   "READY" / waiting for the initialization. `-WhatIf` prints the environment without starting.
4. STP pushes the Initialization: expect one "PLACEMENT: UNIT <name>" line per unit, the
   "ComposeHierarchy:" lines for units with declared children or coarse companies, and the
   units appearing in the GUI within ~30 s.
5. STP pushes Orders: expect "CreateRoute ... move deferred to route-created" then
   "MoveAlongRoute issued" per task; units move in the GUI; STP receives position reports every
   10 s and a task-status (TASKCMPLT) report per completed task.

## 2. Stop order
Ctrl+C the interface (it resigns from the federation), close VR-Forces (GUI File > Exit, or
`scripts\StopVrf52.ps1`), leave rtiexec running.

## 3. Reset between demo runs (DEMO_READINESS row 9 - verify)
Reload the scenario in the GUI (File > Open, the same fixture) or `tools\ResetVrf`; restart the
interface (step 3) so its unit map is empty; push the init again. Do not push a second init
into a running interface - it ignores duplicates by uuid (a guard, not a reset).

## 4. Known traps
- PATH: the interface must start through StartInterface52.ps1 (or an equivalent prefix); a
  bare `VrfC2SimApp.exe` fails at the first bridge call ("procedure imported by VrfBridge.dll
  could not be loaded").
- clientId vs SystemName: they must be equal or the interface creates nothing (RUNBOOK sec 2).
- Terrain: MAK Earth (online) needs internet; without it VR-Forces stalls on terrain load.
- Two interfaces on one network need different -AppNumber values.
