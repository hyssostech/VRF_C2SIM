# PREREG D5 - the Way B rehearsal: hand-started, three-window, STP-driven

REGISTERED by the seat 2026-09-21, BEFORE the run. Drafted by an Opus executor from the docs and scripts
(nothing was launched to write it); the seat's additions are section 10. A missed HIGH-confidence limb
(conf >= 0.80) is a STOP, never a patch; limbs below 0.80 are recorded as findings.

**REVISED against main ef2b640** (`fix/wayb-as-typed`, merged 2026-09-21), which fixed the
defects this prereg's first draft was written around - DR-1 (no endpoint control), DR-2/DR-8
(9201/9202), DR-3..DR-7 and R-3 (the working directory). **D5 now rehearses the runbook AS
TYPED**: `-Server private` on step 3, `9102/9103` on step 2, `StopIface` as sec 7's own stop.
Superseded text is deleted, not left standing beside the correction.

Run script: `scratchpad\validation\d5_wayb_orchestrator.ps1` (parse-clean, 7538 tokens; its
own `-DryRun` printed every command line and started nothing).
Control: D8 `runs\20260921T052350Z_run`, harvest `scratchpad\validation\v6harvest\d8_harvest_report.md`.
Target rows: DEMO_READINESS_2026-09-06 rows 5, 7, 13 (all "the STP-driven sequence end to end").

---

## 0. WHAT IS BEING TESTED, AND WHAT IS NOT

TESTED: that an operator following `docs\DEMO_RUNBOOK.md` sec 2 -> 3 -> 4 -> 7, typing the
three commands as written, reaches a running demo and a clean stop, with PushInit/PushOrder
standing in for STP.

NOT TESTED: Way A (proven D1-D8); STP itself; the GUI's appearance (no human is watching);
anything the runbook does not tell an operator to type.

THE COMPARISON IS NOT LIKE-FOR-LIKE BY CONSTRUCTION. D8 ran through the runner, which sets
NO `DOTNET_ENVIRONMENT` (appsettings.json:37 says so in as many words) and injects its
settings as `Vrf__*` environment variables from `scripts\RunScenario.sh`. Way B sets
`DOTNET_ENVIRONMENT=Demo` (StartInterface52.ps1:15,149) and takes them from
`appsettings.Demo.json`. Section 3 lists every effective difference; section 4 predicts only
what those differences permit.

---

## 1. THE WINDOWS. Every ratio and every duration below names one.

- **W-STEP(n)**: from the moment the orchestrator starts step n's process to that step's
  documented EXPECT text appearing in its own captured stdout.
- **W-MOVE**: from the app's FIRST `DISPATCHED <unit> task ... at WALL <stamp>, SIMULATION
  clock <s> s.` line to the app's LAST completion line. This is D8's own movement window
  (d8_harvest_report sec 0b) and the only window any ratio here is quoted over.
- **W-RUN**: order on the bus -> the orchestrator's verdict line.
- **W-STOP**: StopIface start -> post-run inventory.

RATIOS ARE READ, NEVER COMPUTED FROM TWO WALL FIGURES (RUNBOOK 11f: that is exactly how D6
came to record "sim/wall 1.00" for a scenario D7 measured at 3x). The instruments are the
app's own `SIM/WALL RATIO` lines and its `<N> WALL s after dispatch = <M> SIMULATION s`
completion lines, which since main 1d0fb69 are emitted UNCONDITIONALLY.

---

## 2. THE EXPECT TEXTS, PER STEP (the gates the orchestrator stops on)

| # | Command (DEMO_RUNBOOK) | EXPECT | Source |
|---|---|---|---|
| 1 | `pwsh -File scripts\StartRtiExec52.ps1` | `ALREADY UP - an rtiexec from ... is running and TCP 4001 is listening. NOTHING was started` + `RTIEXEC READY rtiexec=47636 forwarder=7696 tcp=127.0.0.1:4001 started=no`, exit 0 | DEMO_RUNBOOK:183; StartRtiExec52.ps1:222-231 |
| 2 | `pwsh -File scripts\LaunchVrf52.ps1 -Scenario R9_Mojave_Empty_52 -BackendAppNumber 9102 -FrontendAppNumber 9103 -AppDataDir C:\C2SIM\vrf-appdata-unattended\appData` | `=== Federation HOLDER (STP-825) - before the back end ===` then `federation HELD: holder pid <p> (appNumber 9190) joined MAK-ONE-2025 within 45s on attempt 1/2`, then `back-end started (pid ...)`, `front-end started (pid ...)`, `scenario LOAD CONFIRMED in the vendor log`, `READY: 5.2d back-end HEALTHY by thread count, front-end with a real main window`, exit 0 | DEMO_RUNBOOK:196-198; LaunchVrf52.ps1:1133,1200,1226,1337,1361 |
| 3 | `pwsh -File scripts\StartInterface52.ps1 -ClientId STP -Server private` **[ef2b640]** | FIRST `*** C2SIM SERVER THE INTERFACE WILL LISTEN TO: rest=http://127.0.0.1:18080/C2SIMServer  stomp=http://127.0.0.1:61614/topic/C2SIM ***`, THEN `READY - joined the federation, 1 VR-Forces back-end(s), type mapping = FidelityTable, compose = on, position reports every 10s. Waiting for the C2SIM initialization (clientId=STP)` | DEMO_RUNBOOK sec 2 step 3 ("For a REHEARSAL without STP, use `-Server private`") and sec 3 |
| 4 | `PushInit.exe <init> http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM` | exit 0 and a `QUERYINIT : 6 Units` line | DEMO_RUNBOOK:241-242; D8 manifest (6 of 6 init units) |
| 4 | `PushOrder.exe <order> 60 <rest> <stomp>` | exit 0 | DEMO_RUNBOOK:243-244 |

**P-STEP1** (conf 0.95). Step 1 takes the ALREADY-UP branch; NOTHING is started.
Basis: rtiexec pid 47636 and rtiForwarder pid 7696 are up now (the orchestrator's own
dry-run inventory, 06:14Z) and TCP 4001 is the listener test.
MISS: any other branch, or exit != 0, inside W-STEP(1) (cap 180 s).

**P-STEP2** (conf 0.80). Step 2 prints the HOLDER section, its holder JOINS on attempt 1 on
appNumber 9190, the back end JOINS (never creates), and the script exits 0.
Basis: RUNBOOK 9c - "a JOIN never exercises the FOM-module receive path a CREATE does (RM
13.3), so the persistent holder AVOIDS the defect"; the seat's persistent holder RtiProbe
74612 is joined to MAK-ONE-2025 until ~11:23Z, so LaunchVrf52's own holder also only joins.
D8's Stage 2h holder joined on attempt 1 in 3 s.
SECONDARY: after step 2 there are **2** live `RtiProbe.exe` (the day's persistent one plus
this launch's own 9190 holder). Neither is ever stopped, and the orchestrator starts neither -
step 2's holder is started by the runbook's own command. **Timing limb:** the seat's
persistent holder expires about **11:23Z**. If D5 starts after that, the inventory will show
NO RtiProbe and step 2's own holder is the only thing keeping the launch off the STP-825
create path - still the documented sec 2 step 1b behaviour, but it removes one layer, so
P-STEP2's confidence drops to 0.7 and a create refusal becomes the expected failure mode.
The orchestrator records both facts (`holder.persistentHolderSeen`, `holder.atStepTwo`,
`holder.launchHolderSection`) so the run can be read either way. If the seat wants the
demo-day posture instead, `scripts\StartFederationHolder52.ps1` is now a repo script - but
starting it is the SEAT's action, not this script's.
MISS: exit 3 with `the federation HOLDER could not join` or `CRASHED AT STARTUP`, or the
absence of the HOLDER section, inside W-STEP(2) (cap 900 s).

**P-SERVER** (conf 0.90) **[ef2b640, NEW]**. Before the READY line, the interface prints
`*** C2SIM SERVER THE INTERFACE WILL LISTEN TO: rest=http://127.0.0.1:18080/C2SIMServer
stomp=http://127.0.0.1:61614/topic/C2SIM ***`, naming the SAME pair the pushes use. The
orchestrator asserts it and REFUSES TO PUSH on a mismatch, which is the unattended reading of
sec 2 step 3's "READ THAT LINE" and sec 4's "A MISMATCHED PAIR IS SILENT".
Basis: StartInterface52.ps1:208 maps `-Server private` to those two constants (:96-97) and
:315 prints the line unconditionally; the script now scopes and restores `C2SIM__RestUrl` /
`C2SIM__StompUrl` itself, and the orchestrator sets NO endpoint environment, so the inherited-
override case that lane's own review found cannot arise here.
**This limb is the live falsifier for DR-1**: if the line reads 8080/61613 with
`-Server private`, the fix is wrong and the run stops before touching the operator's server.
MISS: no such line within 120 s of step 3, or a pair that differs from the push pair.

**P-STEP3** (conf 0.85). The READY line appears with **N = 1**, `FidelityTable`, `compose =
on`, `position reports every 10s`, `clientId=STP`.
Basis: appsettings.Demo.json sets TypeMappingMode FidelityTable (:9), ComposeHierarchy true
(:24), PositionReportSeconds 10 (:65), ClientId STP (:22); StartInterface52 passes
`-ClientId STP` through as `Vrf__ClientId` (:150).
MISS: no READY line within 300 s, or N != 1, or a clientId other than STP. Any of these is
a STOP before anything is pushed (DEMO_RUNBOOK:222-223).

---

## 3. WHAT WAY B GETS THAT WAY A (D8) DID NOT - EVERY EFFECTIVE DIFFERENCE

Way A (D8) = appsettings.json + `RunScenario.sh` exports (:302-311) + the runner's AppEnv52
(RunC2SimScenario.ps1:3637-3648). Way B = appsettings.json + appsettings.Demo.json +
StartInterface52.ps1's env (:143-158). Read both files; this is the whole list.

| key | D8 (Way A) | Way B | same? | consequence |
|---|---|---|---|---|
| `DOTNET_ENVIRONMENT` | unset -> NO Demo overlay | `Demo` | **NO** | the mechanism under every row below |
| ObjectConsoleNotifyLevel | **3** | **-1** | **NO** | see P-CONSOLE |
| ObjectConsoleMemberNotifyLevel | **3** | **-1** | **NO** | see P-CONSOLE |
| ApplicationNumber | ledgered (env) | **9101** (Demo :19) | **NO** | demo-block exemption, appsettings.Demo.json:18 |
| ConnectionConfigFile | the **relocated** tree's copy (runner derives it from `--vrf-appdata-dir`) | the **VENDOR** copy, hard-coded (Demo :7) | **NO** | RISK R-2 |
| C2SIM RestUrl/StompUrl | injected 18080/61614 | 18080/61614, from `-Server private` **[ef2b640]** | yes | was DEFECT DR-1; fixed at main ef2b640 and gated by P-SERVER |
| Vrf VrfHome/VrLinkHome/RtiHome/RidFile/RtiAssistantDisable | absent -> MakRuntime leaves the env alone | present (Demo :11-16) -> the app prepares its own process | **NO** | expect a `MakRuntime: PATH prefixed with ...` line that D8 does not have |
| TypeMappingMode | FidelityTable (sh :302) | FidelityTable (Demo :9) | yes | - |
| TypeMapFile | unit-type-map-52.json (AppEnv52) | unit-type-map-52.json (Demo :10) | yes | - |
| CreationPolicy | AtOrder (sh :303) | AtOrder (Demo :26) | yes | - |
| DeStackCreates / SpacingMeters | true / 700 (sh :304-305) | true / 700 (Demo :28-29) | yes | - |
| DeStackRotationDeg | 0 (sh :306) | not set -> C# default 0.0 (VrfSettings.cs:523) | yes | ring orientation identical |
| DeStackComposedSiblings | true (appsettings.json:42) | true (Demo :31) | yes | the D8 symmetric ring is reproduced |
| ArrivalApproachFraction | 0.5 (appsettings.json:77) | 0.5 (Demo :33) | yes | the D8 traversal bar is reproduced |
| DropOriginVertexMeters | 100 (sh :307) | not set -> C# default 100.0 (VrfSettings.cs:533) | yes | - |
| PreflightRouteShift | true | true (Demo :56) | yes | - |
| PreflightCacheDir / Offline | empty / false | empty / false | yes | SAME directory (the build output); see P-TILES |
| PreflightElevationLevel / MinLevel | 13 / 11 | 13 / 11 (Demo :59-60) | yes | expect L13 again |
| PreflightWarnings | not set -> off | false (Demo :63) | yes | - |
| PositionReportSeconds | 10 (sh :311, POS_REPORT default 10) | 10 (Demo :65) | yes | - |
| TaskClock / TimedCompletion / DurationScale | sim / true / 1.0 | sim / true / 1.0 | yes | - |
| TaskPredecessorTimeoutSeconds | 7200 (sh :308) | 7200 (Demo :35) | yes | - |
| RouteExtentCheck (STP-833) | true | true (base) | yes | - |

**P-CONSOLE** (conf 0.90). The object consoles are OFF in Way B, so:
- `vrfc2simapp.log` (here: `step3_interface.stdout.log`) is of the order of **40 KB**, not
  D8's 3,639,680 B; the WatchVrf trace carries ~**0 CON rows**, not 22,880, and is of the
  order of 1.8 MB, not 9.6 MB (d8_harvest_report sec 5a, against the D3/D6 console-off runs).
- **No `Giving up on movement task` lines, no `BlockedByVehicle` rows, no `New Primary nav
  area` row.** Way B therefore CANNOT reproduce D8's P2 (jam instrument) or P6 (give-ups)
  at all. Stated up front so their absence is not read as a change in behaviour.
MISS: CON rows > 100, or an app log over 500 KB - either means the overlay did not load.

**P-TILES** (conf 0.80, CONDITIONAL on R-1). The banner reads `ROUTE PRE-FLIGHT TILE CACHE:
...\bin\Release-5.2\net10.0\win-x64\preflight-cache - 7 file(s)` and the run ends with
`TILE CENSUS ... cache HIT(s) > 0, 0 HTTP FETCH(es)` at `ELEVATION LEVEL ACTUALLY USED - L13`.
Basis: the directory holds 7 tiles now, is the SAME one D8 used, and D8 reported 2 hits / 0
fetches (d8_harvest_report sec 5).
CONDITION: **a clean rebuild of `Release-5.2` DELETES this cache** (appsettings.json:64,
"it sits INSIDE THE BUILD OUTPUT"). An Opus code lane is live in
`.claude\worktrees\shouldfix-d8`.
MISS: FETCHES > 0 (then the run needed the internet on a dispatch path, and the dispatch
deferral limb below is void).

---

## 4. THE RUN ITSELF

**P-DISPATCH** (conf 0.75). Three tasks dispatch; the first-task dispatch deferral (order on
the bus -> first `DISPATCHED` line) is <= 5.5 s. Basis: D8 +4.749 / +5.082 / +5.203 s against
a registered 5.5 s limit (d8_harvest sec 0g). Window: W-RUN. MISS: any deferral > 5.5 s.

**P-SIM** (conf 0.70). Measured on the app's own SIMULATION clock over W-MOVE, the three
completions land within **+/-12%** of D8:
T_R5_TK1 **81.1**, T_R5_PL1 **485.5**, T_R5_CO1 **660.5** SIMULATION s after dispatch
(d8_harvest sec 0f). Justification for predicting equality at all: every setting that moves
a unit - route shift, de-stack spacing and rotation, composed-sibling spread, drop-origin,
creation policy, arrival fraction, elevation level, task clock, duration scale - is IDENTICAL
between the two configurations (section 3), and the fixture, init, order and build are the
same. MISS: any of the three outside +/-12% on the SIM clock.

**P-RATIO** (direction only, conf 0.60). The W-MOVE sim/wall ratio is **>= D8's 3.362** and
the three WALL figures are correspondingly **<=** D8's 30.9 / 145.9 / 196.3 s.
Justification: N11 - the ratio is load-dependent and monotone with load (2.63x at peak load
to ~4.7x idle); Way B removes the level-3 console traffic that cost D8 ~10 MB of I/O, so the
load is LOWER. **No magnitude is predicted and none may be read into the result**; this limb
exists so that a faster wall clock is not later mistaken for a behaviour change.
MISS: a W-MOVE ratio BELOW 3.362, which would falsify the load account and needs explaining.

**P-REPORTS** (conf 0.70). `reports-captured.log` holds **> 100** bodies with **0 failed**,
including exactly **3 TASKSTRT + 3 TASKCMPLT**. Basis: D8 164 captured / 0 failed / 6
task-status. Count, not rate: the wall duration differs (P-RATIO). MISS: any failed push,
any TASKABRT, or fewer than 6 task-status bodies.

**P-TERMINAL** (conf 0.80). All three order tasks reach a terminal report inside the 900 s
cap, by the same RunnerLib criterion Way A uses (one terminal report per (taskee, task) pair).
MISS: the cap is reached with an open task.

---

## 5. TEARDOWN

**P-STOP** (conf 0.75). In W-STOP: StopIface exit 0; the interface exits by itself; both
observers exit within the 90 s grace; StopVrf52 exit 0 with `CloseMainWindow ... TRUE` on the
GUI and a `taskkill` WITHOUT `/F` on the back end; post-run inventory shows **no** vrfSim /
vrfGui / VrfC2SimApp / WatchVrf / ListenReports, and DOES show rtiexec 47636, rtiForwarder
7696 and at least one RtiProbe.
Basis: STP-844 is fixed and confirmed twice (D1b 9.87 s, D2 9.77 s) precisely BECAUSE of the
run-owned appData, which Way B passes with the same `-AppDataDir`; D8 tore down at exit 0.
MISS: any survivor, or StopVrf52 exit 3 or 5. Nothing is killed either way - a survivor is
reported and the verdict fails.

---

## 6. WHAT WAY B CANNOT SHOW THAT WAY A SHOWS, AND WHAT REPLACES IT

| Way A evidence (D8 sec 5) | available in Way B? | replacement |
|---|---|---|
| runner truth 7/7: input sources with provenance | NO | the orchestrator's manifest records inputs, pids, exit codes, UTC stamps; the app's own start-up banner announces route shift, de-stack, arrival fraction, tile cache and elevation level |
| predicted-vs-announced CONFIRMED lines (route shift, DeStackComposedSiblings, ArrivalApproachFraction) | NO - nobody predicts | the app's announce lines are still in the log; the PREDICTION side is this document |
| `host.deployedAppBuild` read from the binary (1d0fb69, dirty=False) | NO | the seat records `Get-FileHash` + ProductVersion of `VrfC2SimApp.exe` BEFORE the run - mandatory, because the shouldfix-d8 lane can redeploy under us (R-1) |
| connection-config sha256 comparison, `connectionConfigIdentical` | NO | the seat compares the two copies by hash before the run (R-2) |
| vendor logs by pid | YES | the orchestrator calls RunnerLib `Copy-VendorLogByPid` - a copy, never a read |
| holders-at-launch inventory | YES | the conditions block, by process NAME |
| the nav-area READY gate / pre-order settle | NO (needs console >= 3) | none. R9's legs are ~0.6-1.1 km and the terrain cache is warm; this is a stated residual risk, not a covered one |
| early-exit / task-coverage criterion | YES | the same RunnerLib functions, dot-sourced |
| oracle validity flags, QUERYINIT unit count | PARTLY | QUERYINIT is parsed from PushInit stdout; there is no validity-flag machinery |
| the jam instrument and give-up rows (P2/P6) | NO (console -1) | nothing. Stated, not worked around |

---

## 7. RISKS THAT ARE NOT PREDICTIONS

- **R-1 the live code lane.** `.claude\worktrees\shouldfix-d8` may rebuild/redeploy
  `Release-5.2` mid-rehearsal. That would change the binary under the run AND wipe the
  preflight tile cache. GATE: record the exe hash before and after; if they differ the run is
  VOID, not "a result".
- **R-2 two connection configs.** The interface reads the VENDOR
  `MAK-ONE-2025-Config.xml` (Demo :7) while the sim and GUI read the RELOCATED tree's copy
  (`-AppDataDir`). D8 found the two byte-identical (sha256 F445629EEE...). If they ever
  diverge the federates may not share a connection and an observer reflecting 0 entities
  would look like an observation failure instead of a configuration one (D1b harvest A2).
  GATE: hash both before the run.
- **R-3 the interface's working directory - CLOSED at main ef2b640.** StartInterface52 now
  starts the app the way the proven runner path does: `cwd = VR-Forces bin64` plus
  `--contentRoot=<exe dir>` (:81-89, :339). The risk this prereg registered (the script's own
  exe-directory cwd, never exercised live) no longer exists. Kept as a line, not deleted,
  because if step 3 still dies with a Legion FATAL the hypothesis it names is the one to
  re-open.
- **R-4 the third-holder limit.** LaunchVrf52's holder numbers are FIXED at 9190/9191
  (RUNBOOK 9c KNOWN LIMIT). A second Way B launch inside one 900 s hold has one number left;
  a third has none. `-HolderAppNumber` exists on the orchestrator for that case.
- **R-5 n = 1.** One run, one fixture, one order, one host. Nothing here generalises to
  COA-STP1, whose measured ratio has been as low as 0.27x.

---

## 8. APPLICATION NUMBERS **[ef2b640 - ONE CLAIM, NOT THREE]**

Every federate the rehearsal is responsible for now sits in the documented 9101-9199 demo
block, which `appsettings.Demo.json:18` puts outside the engineering ledger: back end
**9102** and front end **9103** (DEMO_RUNBOOK sec 2 step 2 since ef2b640 - they used to read
9201/9202, which were exempt from nothing), the interface **9101**, and LaunchVrf52's holder
**9190/9191** (RUNBOOK:2338-2339).

**THE ONE LEDGER CLAIM IS THE ADDED OBSERVER.** WatchVrf joins the federation and is named in
the ledger rule itself (`docs\OPUS_EXECUTION_PLAN.md:926-927`: "app / ResetVrf / **WatchVrf** /
SetSimRate / LaunchVrf back-end + front-end each take one"), so one number is taken from the
`*** NEXT FREE: <n> ***` marker (:3304, currently 5051) and recorded as consumed.
Checked, not assumed: **ListenReports joins no federation** - it is a C2SIM-bus listener whose
positionals are `<seconds> <captureFile>` with optional `--rest-url` / `--stomp-url`, it takes
no application number and does not appear in the ledger rule. Nothing else this orchestrator
starts joins with a non-demo-block number: `StopIface`, `PushInit` and `PushOrder` are C2SIM
REST/STOMP clients, and `StopVrf52` starts no federate.

## 9. WHAT IS STILL NOT THE TYPED RUNBOOK **[ef2b640]**

Two things, both named in the script's own header and in its manifest:

- **D-3 the observers.** WatchVrf / ListenReports are harness federates Way B does not
  mention. They are the price of a scoreable rehearsal and the only reason a ledger number is
  spent at all. They start before the init (Way A's Stage 5 ordering) and stop by their
  stop-file before StopVrf52.
- **D-4 nobody is reading the screen.** Sec 2 step 3 instructs the OPERATOR to read the loud
  server line and match it against sec 4's pushes; the script asserts it instead (P-SERVER).
  Stricter than the instruction, not a departure from it.

RETIRED, because the typed command now does them: the C2SIM endpoint override (`-Server
private`), the application numbers (9102/9103 inside the block), `StopIface` as the stop
(sec 7 item 1 types it, and says it "is also the only stop a script can perform"), and
PushInit/PushOrder standing in for STP (sec 4's own sanctioned rehearsal path).
The orchestrator **starts and stops no federation holder**: it records what it finds at start
and what LaunchVrf52 itself reports about the holder step 2 starts as typed (sec 2 step 1b).

---

## 10. THE SEAT'S ADDITIONS AT REGISTRATION

- CONDITIONS GATE: no subagent live, no dotnet / MSBuild / VBCSCompiler / vrfNavGenerator, no vrfSim / vrfGui /
  VrfC2SimApp at launch; CPU recorded. A run started under any other condition is VOID (the D2 lesson).
- R-1 / R-2 GATES are the seat's, recorded in the RESULT block: sha256 + ProductVersion of the deployed
  VrfC2SimApp.exe before and after (expected 1d0fb69, unchanged), and the sha256 of the vendor and relocated
  MAK-ONE-2025-Config.xml copies before the run (expected identical).
- THE SERVER is OURS (c2sim-server-vrf, 18080/61614). The operator's 8080/61613 carries a live STP session with
  Iron Storm loaded and is not touched by D5; the orchestrator refuses -Server standard.
- WHAT D5 CAN CLOSE: DEMO_READINESS rows 5, 7 and the Way B half of 13, and DEMO_RUNBOOK's 'UNVERIFIED as a single
  sequence' note - only on a run with every HIGH limb met. STP itself pushing (rather than the stand-in) stays
  owed to a session with the user at the box.
- The run script is scratch validation/d5_wayb_orchestrator.ps1 (UPDATE 2, parse-clean, dry-run verified);
  the ledger claim for the WatchVrf observer is made BEFORE the launch and named in the RESULT block.

---

## 11. D5 RESULT (run 20260921T070926Z_wayb, launched 07:09:26Z) - STOPPED AT STEP 2: P-STEP2 MISS (HIGH)

- Conditions at launch: clean (no subagent, build servers shut down with `dotnet build-server shutdown`, CPU 4 percent);
  exe 1.0.0+git.1d0fb69.Release-5.2 sha256 51798C0C...9A535A; both connection configs F445629E... identical; 7 tiles;
  persistent holder RtiProbe 74612 joined. Scripts at main 8709b2a + the ledger commit (ef2b640's Way B scripts).
- P-STEP1 HIT: ALREADY UP branch, nothing started, exit 0.
- P-STEP2 MISS: LaunchVrf52 exited 1 one second after start, BEFORE any holder, back end or GUI process existed:
  `scripts/LaunchVrf52.ps1:1158` calls `Say-Info`, a function the script never defines (it defines Say-Head / Ok / Warn /
  Fail / Plan). VERIFIED by the seat: the call was introduced by d1885c0 (2026-09-20, 'give the standalone demo path its
  own STP-825 federation holder'); the runner always passes -FederationHeldByCaller, so this path had NEVER executed -
  every 'standalone start' on the record went through the runner. Way B as typed has been dead at step 2 since d1885c0.
  The registered MISS clause named a holder that could not join or a startup crash; the actual miss is more basic than
  either. Competing explanation checked and excluded: an environment/PATH problem would not name an undefined command
  at a fixed script line, and the stderr does exactly that.
- Everything after step 2 (P-SERVER, P-STEP3, P-DISPATCH, P-SIM, P-RATIO, P-REPORTS, P-TERMINAL, P-STOP): NOT REACHED.
  The orchestrator's gates behaved as designed: no retry, STOP section ran, post-run inventory CLEAN, rtiexec /
  rtiForwarder / holder 74612 untouched. One ORCHESTRATOR defect on its failure path ('Argument types do not match' after
  the inventory; no manifest written) - scratch, being fixed with a simulated-failure dry run.
- appNo 5051 (WatchVrf) was claimed and never joined: BURNED.
- Per the rule a missed HIGH limb is a STOP: no patch-and-continue under this registration. The defect is fixed on its
  own branch with a static test (every Say-* a script calls must be defined) and the rehearsal is re-registered as D5b.
- What D5 already bought: the rehearsal found in one second what eight Way A runs could not - the demo's real path had
  an unexecuted branch. DEMO_READINESS rows 5 / 7 / 13 stay open.

---

## 12. D5b - REGISTERED 2026-09-21 BEFORE THE RUN: the same rehearsal on main 1553e48

What changed since D5: LaunchVrf52 defines Say-Info (merge 1553e48; static checks 12/12b now resolve every Say-* and
Verb-Noun call in scripts/*.ps1 - Say-Info was the only hole); the should-fix lane is merged (4a4d1fa: vendor log
copies go to <run>/vendor/, the HELD line quotes the MATCHED rtiexec text, runner env restoration - the last does not
touch Way B). The APP BINARY IS UNCHANGED: 1.0.0+git.1d0fb69.Release-5.2 (the merged C# changes are NOT deployed; the
rebuild follows D5b so that D5b differs from D8 by the PATH only). The orchestrator (scratch, UPDATE 3) had its
failure path fixed and exercised by simulated-failure dry runs for steps 1-3 and the server gate.

Predictions: sections 1-7 stand UNCHANGED, limb by limb, with these notes.
- P-STEP2 stays at conf 0.80 but the seat flags it as the limb most likely to miss AGAIN: LaunchVrf52's standalone
  holder block (~:1170-1260) has STILL never run to completion live; its success lines in the orchestrator are
  predictions from source. A second miss there is a STOP like the first.
- The persistent holder RtiProbe 74612 is expected joined (expires ~11:23Z), so step 2's own holder JOINS.
- The ledger claim is a NEW number from the marker (5051 is burned); named in the RESULT block.
- Conditions gate and R-1 / R-2 hash gates as section 10. MISS clauses as registered in sections 2-5.

---

## 13. D5b RESULT (run 20260921T072530Z_wayb, launched 07:25:30Z) - Way B ran end to end for the first time; the back end crashed 1.3 s after the init/order overlap; 3 TASKABRT, 0 TASKCMPLT

- Conditions at launch: CPU 6 percent, no dotnet/MSBuild, no vrfSim/vrfGui/VrfC2SimApp, no rtiAssistant, persistent holder
  RtiProbe 74612 joined (07:25:21Z). Exe hash 51798C0C...9A535A (1.0.0+git.1d0fb69.Release-5.2), identical before and
  after; both connection configs F445629E... identical; both rid copies 649c7c87... identical. Scripts main 1553e48+.
- P-STEP1 HIT: ALREADY-UP branch, exit 0, W-STEP1 2.02 s.
- P-STEP2 HIT - FIRST LIVE EXECUTION of the standalone holder block that killed D5: holder pid 86900 (appNo 9190) joined
  MAK-ONE-2025 on attempt 1/2 within 45 s (the garble-tolerant regex matched a doubled rtiexec line again); back end
  Federate15 JOINED 07:25:39Z; no federation create or destroy anywhere in the rtiexec log; W-STEP2 24.62 s.
- P-SERVER HIT: 18080/61614 named and pushed against.
- P-STEP3 HIT: READY, N=1, FidelityTable, compose=on, 10s, clientId=STP; W-STEP3 5.06 s.
- P-CONSOLE HIT on the numbers, HALF VOID as evidence: app log 41,064 B (genuine, under the 500 KB limit);
  watchvrf-trace.csv 996 B / 0 CON rows - but that is because the observer never joined the federation (H4 below), not
  because the console is off. The trace limb proves nothing either way.
- P-TILES HIT: 7 tiles before and after; 5 cache HIT(s), 0 HTTP FETCH(es); L13 x2 elevation.
- P-DISPATCH MISS: one DISPATCHED line only (T_R5_CO1, +11.647 s against the 5.5 s limit); the other two never dispatched.
- P-SIM MISS (not measurable): zero completions.
- P-RATIO VOID as a measurement, MISS as written: no movement window exists; the one 1.000 ratio window lies entirely
  after the back end died - it is crash evidence, not a rate.
- P-REPORTS MISS: 67 bodies / 0 failed - 1 TASKSTRT, 3 TASKABRT, 0 TASKCMPLT; the "any TASKABRT" clause fires three times.
- P-TERMINAL MET ON THE LETTER, HOLLOW: 3 of 3 terminal at +40.9 s into a 900 s cap - all three are TASKABRT, not
  completions.
- P-STOP MISS: WatchVrf 57756 survived past the stop-file grace (the registered "any survivor" clause fires); nothing
  was killed automatically, which is correct under the standing rule. StopVrf52 closed the crashed back end gracefully
  (CloseMainWindow TRUE on the GUI; `taskkill /PID` with no `/F` on the back end) at 08:00:57-08:01:07Z, 34 minutes
  after the crash - WatchVrf 57756 itself was left running past that and was stopped by the seat, by pid, at about
  08:12Z, under the standing narrow permission for a federate that failed its own join. rtiAssistant 48392 (sec H4) is
  left running - the no-kill rule makes that the user's call.
- Seat gates PASS: R-1 exe hash identical before/after; R-2 both connection configs and both rid copies identical.

H1 FALSIFIED IN ITS MECHANISM: the back end was alive and serving terrain (reply 1 back in under a second, six correct
Mojave elevations) when the order arrived - it was not slow, it was alive then dead. Its quantitative half is also
wrong: Way A's protective interval measured 3.75 s (Stage 7's evidence-based oracle gate, not a fixed sleep), not the
~180 s H1 assumed; Way B's gap was 0.31 s with no gate at all.
H2 DEAD for the crash: the D5b and D8 back-end command lines are IDENTICAL but for `--appNumber` (9102 vs 5040); both
connection configs and both rid copies are byte-identical. The only things left standing are the rtiAssistant
(H4-indirect, below) and the 0.31 s init/order gap (H3/H5).
H3 CONFIRMED, VERIFIED, and it alone explains T_R5_TK1: 1.BdeHQ~PXY's task was DROPPED because the unit "was not
created" - then the unit WAS created one log line later. Init creation on a platform-typed (AtOrder, shell-less)
taskee is deferred on the terrain-profile reply and has no gate the way `MaterializeUnit`'s composition path does; the
order arrived inside that window this run (terrain requests 10 and 11 also timed out this run, widening the window to
10 s where it happens). Filed as **STP-852**: an order arriving while the init is still materializing is ABORTED
instead of deferred, and there is no READY TO TASK signal telling an operator when it is safe to push the order (fix
lane `fix/defer-dispatch-until-init-bound`).
H4 FALSIFIED IN ITS FEDERATION FORM: WatchVrf (pid 57756, appNo 5052) never appears in the 113,188-line rtiexec log and
is absent from the 4-federate roster printed at join time; no federation was ever created or destroyed - it could not
have killed the back end through the federation. A WEAKER FORM SURVIVES, UNEXCLUDED: WatchVrf was started with no RTI
environment (a harness defect), inherited the machine-scope rid, and spawned an rtiAssistant (pid 48392) at 07:26:06Z -
23 s before the crash - which is now the only environmental oddity left standing; a residual, not a diagnosis.
H5 SURVIVES, UNDIAGNOSED, n=1: the back end crashed 1.3 s after the order hit the bus and about 1 s after the init's six
objects were created (callstack created 07:26:29Z; three independent instruments agree - terrain replies 10/11 timing
out, the SIM/WALL ratio collapsing to exactly 1.000, and the back end sitting on a Windows error dialog 34 minutes
later). NOT the known `--logFileName` startup crash (not passed; the process ran 51 s and reached "Successfully loaded
scenario" cleanly). Candidate mechanism: concurrent create-and-delete of the same objects during the init/order
overlap - the control performs the same churn without dying, so churn alone is not the answer. Not claimed as
diagnosed. Filed as **STP-854**: the D5b back-end crash, n=1, undiagnosed.

A1 (new, its own ticket, **STP-853**): the interface reported a dead simulation as healthy for 2 min 38 s - 60 position
reports at 4 units' byte-identical frozen coordinates, none marked stale, and it DISPATCHED T_R5_CO1 into a back end
already dead for 10 s. Detection chain: crash -> ~118 s VR-Forces back-end ageing timeout -> +40 s STP-822 rule -> loss
declared, 158 s total. The most demo-relevant finding in the run: an audience would have seen a healthy-looking
scenario for over two minutes after the simulator died.

Harness defects (scratch orchestrator, fixed in UPDATE 4 after this run): no RTI environment given to the observers
(the direct cause of the H4 weak form and the P-CONSOLE VOID); manifest `orderPushedUtc` off by 60 s (it records
PushOrder's exit, not the push - the order actually reached the bus 07:26:28.200Z); the FAIL text called WatchVrf 57756
"a joined federate" when it never joined, which is what seeded H4; one undeclared deviation D-5 (the orchestrator
pushes the order 0.31 s after PushInit returns, with no gate an operator could type) - DECLARED here because it is what
exposed H3, not scrubbed from the record.

The seat's own misses, plainly recorded: the brief said "start the observers as Way A does" without naming the env
contract; the conditions inventory used an anchored regex that could not see vrfSimHLA1516e, so a crashed back end
sitting on an error dialog went unlisted for 34 minutes; the first D5b launch wrapper was piped and hung the shell.

What Way B PROVED: steps 1-3 of the runbook work as typed on main 1553e48 (after D5 found LaunchVrf52.ps1:1158's
undefined `Say-Info`, fixed in that merge), and the server gate works. D5b does NOT close DEMO_READINESS rows 5, 7 or 13.

VERIFIED vs ASSUMED (from the harvest sec 8, in its sense). VERIFIED: WatchVrf's absence from the rtiexec log and
roster; the full federate join order and times; the crash artefact timestamps (callstack, dmp, back-end log last
write); the back end alive on an error dialog at 08:00:57Z; terrain replies 1/8/9 answered and 10/11/14 timed out; the
0.31 s init-order gap; the log-ordering reversal against the D8 control; 1.BdeHQ~PXY's actual creation and reporting;
the report/code counts; both exe and config hashes; rtiAssistant 48392's start time and absence from the pre-run
inventory; the full teardown sequence; that all four federate-starting scripts set `RTI_RID_FILE`; the D5b-vs-D8
back-end command-line comparison (identical but for `--appNumber`). ASSUMED / INFERRED, flagged as such: that the
callstack's creation timestamp is the fault time (standard, corroborated three ways, not read from the file itself);
that rtiAssistant 48392 was spawned by WatchVrf (inferred from timing and WatchVrf's own trace, no process-tree
evidence survives); that the ~118 s ageing gap is a VR-Forces back-end timeout (fits, not read from vendor docs this
session); the causal link between the init/order overlap and the crash itself (coincident to the second, every named
alternative excluded, but n=1 and undiagnosed).

NEXT: C2 = the same rehearsal after the defer/READY-TO-TASK fix is merged, rebuilt and deployed, with the observer
given the runner's env (the new P-RID gate) or run with `-NoObserver`. C1 (same 0.31 s gap, fixed observer environment,
OLD binary) is registered only if the crash itself still needs isolating from the harness defect - it separates "the
harness caused it" from "the overlap caused it" in one run, and is not needed if C2 alone is run next.

---

## 14. D5c - REGISTERED 2026-09-21 BEFORE THE RUN: the Way B confirming run on the STP-852 build (binary 4f1f149)

Same orchestrator path as D5b (runbook steps as typed, -Server private, 9102/9103, the order pushed immediately after
PushInit returns - DECLARED deviation D-5, about 0.3 s: it is what exposed STP-852 and no operator can type it), with
the orchestrator's UPDATE 4: the WatchVrf observer gets the runner's per-process RTI environment and a new live gate
P-RID stops the run before step 3 if the observer loads any rid but config/rid-501-rtiexec-min.mtl or mentions an RTI
Assistant. Binary 1.0.0+git.4f1f149.Release-5.2 (build report scratch validation/4f1f149_build_report.md, GO). Way A
control on the same binary: D9 (run 20260921T094051Z, 3/3 TASKCMPLT). Expected strings: build report sec 8; must-show
list: scratch validation/defer_not_abort_review.md (Run 1 + the DELTA additions).
KNOWN CONDITION: rtiAssistant pid 48392 (left by D5b's harness) is STILL RUNNING - the never-kill rule makes its removal
the user's call. It was present through D9 with no observed effect. Consequence for inference, stated up front: a CLEAN
D5c is consistent with 'the init/order overlap caused the D5b crash' but cannot exclude the assistant's appearance as a
factor (in D5b it APPEARED mid-run; here it pre-exists); a SECOND back-end fault with the overlap removed would point
away from the overlap. STP-854 stays open either way (n is still small).

P-STEP1 / P-STEP2 / P-SERVER / P-STEP3 / P-TILES / P-CONSOLE / P-STOP: as sections 2-5, unchanged (P-STEP2's holder
block has now run live once, D5b: conf 0.85).
P-RID (HIGH, new): the observer's first lines name OUR rid; no 'RTI Assistant' text; no NEW rtiAssistant process is
spawned (48392 remains the only one); WatchVrf appears in the rtiexec log as a joined federate and exits on its stop-file
within the grace period.
P-READY (HIGH): the interface prints 'INIT CREATION BARRIER: 6 object(s) planned ...' quoting 20 s (the empty-shell
count as D9's own log printed it, 5 - the build report's offline '4' was wrong or differently counted; D9's harvest
settles which) and 'READY TO TASK - 6 of 6 ...' exactly once, NOT the 'NOT REACHED' variant.
P-HOLD (HIGH): ZERO 'has no VR-Forces object bound to its name', ZERO 'live location could not be read'. T_R5_TK1
(1.BdeHQ~PXY, a platform that never parks) is HELD - 'WAITING FOR THE BACK END: task ... held' naming
[PLANNED-BUT-NOT-REQUESTED] or [REQUESTED-BUT-NOT-BOUND] - and RELEASED; held-count == released-count; every hold lasts
seconds (< 10 s), none reaches its bound. T_R5_PL1 / T_R5_CO1 wait on their composition gate and may print no hold line;
'MATERIALIZE ... HELD' appears for the aggregates only (expected TWO lines, MEDIUM on the count). An 'ORDER BEFORE READY
TO TASK' warning is EXPECTED here (the order does arrive early) - MEDIUM: RUNBOOK 11g says it cannot fire when every
name is already bound.
P-OVERLAP (HIGH): no order-driven delete / MATERIALIZE-deleted line between the first and the last init PLACEMENT
line; the first order-driven delete follows READY TO TASK. ZERO 'not signalled within', ZERO 'REFUSING TO MATERIALIZE',
ZERO 'MATERIALIZATION-PARKED' timeouts.
P-OUTCOME (HIGH on completion, MEDIUM on the numbers): 3/3 TASKCMPLT, 0 TASKABRT; completions on the app's SIM clock
within +/-12 percent of D9's (D8: 81.1 / 485.5 / 660.5 SIM s after dispatch) - window: each task's own DISPATCHED line
to its completion line; movement-window sim/wall ratio reported against D9's, a value near 1.0 is the dead-back-end
signature and a MISS.
P-FAULT (MEDIUM, conf 0.7): no back-end fault - no new .dmp / .callstack for this run's back end (names + mtimes only),
no BACK END LOST, position reports not frozen.
MISS = any race abort, a hold that runs to its bound, READY TO TASK NOT REACHED, an order-driven delete inside the init's
creations, any task not TASKCMPLT, P-RID failing, a survivor at teardown. A missed HIGH limb is a STOP. A back-end fault
is recorded as NEW EVIDENCE for STP-854, not as a repeat.
WHAT A PASS CLOSES: DEMO_READINESS rows 5 / 7 and the Way B half of 13, and DEMO_RUNBOOK's 'UNVERIFIED as a single
sequence' note (check 11j is then updated in the same commit). STP itself pushing stays owed to a session with the user.
