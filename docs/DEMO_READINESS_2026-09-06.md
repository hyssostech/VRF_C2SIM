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
| 5 | appsettings.json DEMO defaults | O | today the runner injects per-process env for the 5.2 profile (Vrf__ConfigFileIdentity, Federation "", FedFileName "", ConnectionConfigFile MAK-ONE-2025, TypeMapFile 52; RunC2SimScenario.ps1:1989-2001). Standalone needs them IN appsettings (or a `appsettings.Demo.json` selected by env) - including ClientId = the STP init's SystemName ("STP" for the R9-style inits, "C2SIM" for COA-STP1), compose ON (done), position reports ON, console level -1 |
| 6 | Runtime environment without the runner | O | the RUNTIME PATH TRAP (5.2d bin64 + VR-Link 5.10 bin64 + RTI 5.0.1 bin prefix, MAK_VRFDIR, RTI_ASSISTANT_DISABLE + rid) is set per process by the runner today. Owed: `scripts/StartInterface52.ps1` that sets ONLY the interface's environment and starts the exe (not a harness), plus the same for VR-Forces (LaunchVrf52.ps1 already exists and is not a harness - reuse) and rtiexec (StartRtiExec52.ps1) |
| 7 | VR-Forces GUI ON | V | every 5.2 run today was -NoGui; the demo shows the GUI. One run with the GUI attached (LaunchVrf52 default) to confirm the interface path is unchanged (UG52 4.1.2 independent mode; the GUI is a second federate) |
| 8 | ApplicationNumber without the ledger | O | the never-reuse rule is a TEST rule (traces must be attributable); a demo needs a fixed, documented, unique-on-the-network appNumber in appsettings and a note that two interfaces on one network must differ |
| 9 | Reset between demo runs | V | tools/ResetVrf (RUNBOOK sec 8) exists; verify a clean "reload scenario -> push init again" cycle without restarting VR-Forces (duplicate-init guard exists: VrfC2SimService.cs:577) |
| 10 | Terrain | V | MAK Earth (online) needs internet; an offline fallback terrain (a shipped SharedData terrain) should be named in the demo runbook, with the fixture's frame mode = real-time (NOT fixed-frame-run-to-complete, which is the test speed-up) |
| 11 | Type-map coverage of the DEMO ORBAT | O | "type-map live gate" in the queue; the demo's init is STP's real one - run `--parse-init <init> <clientId>` + `--typemap-selftest` against it; PROXY rows (BN -> HQ-section CP) are a fidelity decision to state in the demo notes |
| 12 | Licence | O (user) | node-locked DEMO licence lapses 2026-09-15 (MAK Sales). Hard prerequisite for any demo after that date |
| 13 | Demo runbook | O | docs/DEMO_RUNBOOK.md: prerequisites, start order (rtiexec -> VR-Forces with GUI -> interface -> STP init -> orders), what the audience sees, the three traps (PATH, RTI dialog once per reboot, leftover instances), stop order |
| 14 | Clean stop | V | the interface's Stop path resigns (VrfC2SimService.StopAsync); VR-Forces stays up; the harness's teardown problems (memory: "TEARDOWN IS NOT SOLVED") are harness problems - verify the interface alone stops cleanly and can be restarted against the running sim |
| 15 | Observability for the operator | O | the app log at INFO is the operator's view; the console channel stays at -1 (diagnostic only); a one-line "READY: joined federation, N backends, waiting for init" and per-order summaries ("order X: N tasks, M dispatched") exist partly - review the log for a non-developer reader |
| 16 | Real tasks | later | after this milestone: the 17 verbs of the STP vocabulary (VerbMapping: recognized, Layer-2 not wired) |

## What this milestone does NOT include
GUI automation of any kind; the harness features (ledger, observers, scoring, StopWhenComplete);
golden-parity to 5.0.2 (ARCHIVE).

## Exit criterion
An operator following DEMO_RUNBOOK.md, with no repo tooling beyond the three start scripts,
brings up rtiexec + VR-Forces (GUI) + the interface, pushes the R9 init and the R9 order from
STP (or PushInit/PushOrder as the STP stand-in), and sees the three units created and moving in
the GUI and their position + task-status reports in STP - twice in a row after a reset,
without restarting VR-Forces.
