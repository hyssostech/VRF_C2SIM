# DEMO-READY: the interface as a standalone deployment (no harness)

User ruling 2026-09-06: "When the time comes for tackling the real tasks, not just move along,
let's first get the app ready for deployment as a demo, working without the harness for this
bit." SEQUENCE: (1) finish the current move-along / scale measurement (COA-STP1 on 5.2), then
(2) THIS milestone, then (3) the STP task vocabulary (goal-full-stp-task-vocabulary).

"Without the harness" = the operator starts VR-Forces and the interface by hand (or by ONE thin
start script that only sets the interface's own environment), STP pushes the init and orders to
the C2SIM server, the interface creates the ORBAT and executes the orders, and STP / the
VR-Forces GUI show the result. No runner, no WatchVrf, no ledger, no scoring - those stay the
TEST harness. The GUI is the audience's window (a human watching), which is not GUI automation
and does not contradict the headless goal; the interface itself stays headless.

## Gap list (D = done and verified today or earlier; O = owed; V = exists, needs one verification)

| # | Requirement | Status | Evidence / what is owed |
|---|---|---|---|
| 1 | ORBAT loads faithfully (declared tree composed; coarse leaves expanded; leader = declared first) | D | N1/N2/N3 2026-09-06: PREREG_N1/N2/N3; DESIGN_ORBAT C1-C11 |
| 2 | Units MOVE as units on 5.2 (company) | D | compose 3/3 x5 runs; C1b closed by the console |
| 3 | Task-status (TASKCMPLT) reports to the C2SIM bus | D | every run since R9 |
| 4 | POSITION reports to the C2SIM bus on 5.2 | V | R1 (PREREG_R1): run P shows "6 sent" per round and ListenReports counts Position reports; final verdict pending teardown. Demo default `Vrf:PositionReportSeconds` must be > 0 in appsettings (today 0 = oracle default) |
| 5 | appsettings.json DEMO defaults | V | `src/VrfC2SimApp/appsettings.Demo.json` (2026-09-06): the 5.2 profile keys the runner injects (ConfigFileIdentity, Federation "", FedFileName "", ConnectionConfigFile MAK-ONE-2025, TypeMapFile 52, VrfHome) + ClientId STP (override per init), compose ON, PositionReportSeconds 10, console levels -1. Loaded by DOTNET_ENVIRONMENT=Demo (HostApplicationBuilder default overlay); csproj copies it. Owed: one standalone start with it (row 7's run) |
| 6 | Runtime environment without the runner | V | `scripts/StartInterface52.ps1` (2026-09-06): PATH prefix (5.2d bin64, VR-Link 5.10 bin64, RTI 5.0.1 bin), MAK_VRFDIR/VRLDIR/RTIDIR, RTI_ASSISTANT_DISABLE=1 + RTI_RID_FILE (rtiexec posture), DOTNET_ENVIRONMENT=Demo, Vrf__ClientId/ApplicationNumber/PositionReportSeconds; refuses without rtiexec on 4001; `-WhatIf` prints and starts nothing (dry run 2026-09-06 OK). VR-Forces: LaunchVrf52.ps1 (not a harness); rtiexec: StartRtiExec52.ps1 |
| 7 | VR-Forces GUI ON + standalone start | V | every 5.2 run today was -NoGui and runner-started. ONE end-to-end standalone run owed: StartRtiExec52 -> LaunchVrf52 (GUI) -> StartInterface52 -> PushInit/PushOrder as the STP stand-in; the interface path is expected unchanged (UG52 4.1.2 independent mode; the GUI is a second federate) |
| 8 | ApplicationNumber without the ledger | D | demo block 9101-9199 in appsettings.Demo.json (outside the test ledger's 4xxx range); `-AppNumber` on the start script; "two interfaces on one network must differ" documented in the overlay and the runbook |
| 9 | Reset between demo runs | V | tools/ResetVrf (RUNBOOK sec 8) exists; verify a clean "reload scenario -> push init again" cycle without restarting VR-Forces (duplicate-init guard exists: VrfC2SimService.cs:577) |
| 10 | Terrain + real-time fixture | V | MAK Earth (online) needs internet; an offline fallback terrain (a shipped SharedData terrain) should be named in the demo runbook. Real-time fixture BUILT 2026-09-06: `tools/FixtureGen/frame_variants/R9_Mojave_Empty_52_RT.scnx` (build_fixture.py --profile 5.2 --empty --frame-mode variable-frame --out-name R9_Mojave_Empty_52_RT; frame-time 0.1; validate_fixture.py passes every structural check and fails only its two FFRTC-pair assertions, frame-mode/frame-time, by design). Owed: the sanctioned deploy to C:\MAK\vrforces5.2d\userData\scenarios (same command + --out-dir) and one run |
| 10b | Type mapping = the fidelity table | D | appsettings.Demo.json sets TypeMappingMode=FidelityTable + the 5.2 map. Found on COA-STP1 run 1 (2026-09-06): the base default RealTemplates is the 5.0.2 parity dispatch (every company = Tank Company (USA), hostile included); the table is read only in FidelityTable mode. Its live gate = COA-STP1 run 2 |
| 11 | Type-map coverage of the DEMO ORBAT | O | "type-map live gate" in the queue; the demo's init is STP's real one - run `--parse-init <init> <clientId>` + `--typemap-selftest` against it; PROXY rows (BN -> HQ-section CP) are a fidelity decision to state in the demo notes |
| 12 | Licence | O (user) | node-locked DEMO licence lapses 2026-09-15 (MAK Sales). Hard prerequisite for any demo after that date |
| 13 | Demo runbook | O | docs/DEMO_RUNBOOK.md: prerequisites, start order (rtiexec -> VR-Forces with GUI -> interface -> STP init -> orders), what the audience sees, the three traps (PATH, RTI dialog once per reboot, leftover instances), stop order |
| 14 | Clean stop | V | the interface's Stop path resigns (VrfC2SimService.StopAsync); VR-Forces stays up; the harness's teardown problems (memory: "TEARDOWN IS NOT SOLVED") are harness problems - verify the interface alone stops cleanly and can be restarted against the running sim |
| 15 | Observability for the operator | O | the app log at INFO is the operator's view (run P: endpoints, bridge start, "Backend discovered", "Connected to C2SIM ... clientId", "C2SIM Initialization", mode line, compose lines, "Init dispatched: N units", PLACEMENT per unit, then per-task CreateRoute/MoveAlongRoute and "VRF task complete"). Owed: one explicit "READY - joined, N backend(s), type map <mode>, waiting for the initialization" line after the join, and a per-order summary ("order <id>: N tasks for M taskees"); the console channel stays -1 |
| 16 | Real tasks | later | after this milestone: the 17 verbs of the STP vocabulary (VerbMapping: recognized, Layer-2 not wired) |
| 17 | DI-Guy character data (makData19 DI-Guy package, 6.33 GB) | O (user) | Found 2026-09-06 (PREREG_COASTP1_52_RUN1 sec 6-7): without `C:\MAK\SharedData\19\latest\ModelData\Lifeforms\DIGuy` the 5.2d headless sim crashes (0xC0000005, DtDiGuyController::determineInitialHandItem) at the first DI-Guy human it creates - i.e. at the first infantry / mortar / CSS / fire-support unit the fidelity table maps. A demo whose ORBAT has any infantry needs the package installed first; the interim (vehicle-only proxies for lifeform rows) is a fidelity regression = user ruling |

## What this milestone does NOT include
GUI automation of any kind; the harness features (ledger, observers, scoring, StopWhenComplete);
golden-parity to 5.0.2 (ARCHIVE).

## Exit criterion
An operator following DEMO_RUNBOOK.md, with no repo tooling beyond the three start scripts,
brings up rtiexec + VR-Forces (GUI) + the interface, pushes the R9 init and the R9 order from
STP (or PushInit/PushOrder as the STP stand-in), and sees the three units created and moving in
the GUI and their position + task-status reports in STP - twice in a row after a reset,
without restarting VR-Forces.
