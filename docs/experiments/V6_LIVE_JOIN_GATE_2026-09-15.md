# V6 / V6b - THE LIVE JOIN GATE FOR THE 5.2 BRIDGE TOOLS (STP-820)

Status 2026-09-15 12:10Z: **the gate FAILS and the cause is OPEN.** Two runs, eight falsified
hypotheses, one surviving one. This file is the record; `PREREG_V6C_LATE_JOINER_2026-09-15.md`
is the next probe.

The symptom in one line: **a bridge federate that joins an established 5.2 federation gets HLA
object discovery and attribute data, but never learns that a VR-Forces back end exists.**

---

## 1. THE RUNS

| | V6 | V6b |
|---|---|---|
| run dir | `runs/20260915T030650Z_run` | `runs/20260915T114001Z_run` |
| scenario / init | `R9_Mojave_Empty_52` + `R9_Mojave_Lean` (6 units) | same |
| rtiexec | PRESERVED (pid 69856, up since 09-13) | **FRESH, started by the run** (pid 36840) |
| tools | the four converted at `b4fcf58` | the same four, hardened at `89639c2` |
| driver | `scratchpad/validation/v6_gates.ps1` | `scratchpad/validation/v6b_gates.ps1` |
| result | every joining tool: BackendCount=0 | identical, **and the positive control failed too** |

V6b added the two defences the V6 harvest built, as SEPARATE arms, so the run could say which
one mattered. **Neither did.**

---

## 2. THE GATE LINES, VERBATIM

### V6 (03:10-03:14Z) - the false green that started this

```
ResetVrf 4390 --dry-run
    stack=5.2  federation=(from connection config; execName MAK-ONE-2025 expected)
    [OK] joined (BackendCount=0).
    [OK] discovery complete: 3 reflected object(s) (3 deletable, 0 nil/backend skipped).
           VRF_UUID:0:0:0-control-object
           VRF_UUID:0:0:0-entity
           VRF_UUID:0:0:0-unit
    [DRY-RUN] would delete 3 object(s); NO deletes issued.

ResetVrf 4368 (REAL)
    [OK] 3 deleteObject command(s) issued.
    [OK] deletes flushed.
    [OK] resigned cleanly. Verify the VR-Forces GUI now shows an empty scenario.
      -> the oracle trace held reflected=48 readable=45 for the WHOLE window. NOTHING was deleted.

SetSimRate 10 4370 / CreateTaskAgg create 4372
    [OK] joined (BackendCount=0 immediately after Start).
    [FAIL] no backend discovered after 15 s (BackendCount=0). ... refused.
```

### V6b (11:43-11:47Z) - both defences, and the positive control

```
GATE 1a  ResetVrf 4428 --dry-run        [FULL runner env, NO --config]
    connection config = C:\MAK\vrforces5.2d\appData\settings\connections\MAK-ONE-2025-Config.xml
                        exists=YES  source=bound stack (loaded vrfcontrol.dll -> C:\MAK\vrforces5.2d)
    [..] waiting for a back end to be discovered (15 s cap)...
    [FAIL] no back end discovered after 15 s (BackendCount=0).            exit=1

GATE 1b  ResetVrf 4429 --config <path> --dry-run   [V6's MINIMAL env, MAK_VRFDIR/MAK_VRLDIR cleared]
    connection config = ...MAK-ONE-2025-Config.xml  exists=YES  source=--config
    [FAIL] no back end discovered after 15 s (BackendCount=0).            exit=1

ARM COMPARISON: 1a deletable=-1 exit=1 | 1b deletable=-1 exit=1      <- the two arms are identical

GATE 3a SetSimRate 10 4430 / 3b SetSimRate 1 4431 / 4a CreateTaskAgg create 4432
GATE 2a ResetVrf 4434 (real) / 2b ResetVrf 4435 --dry-run
    all: connection config exists=YES, "[OK] joined", no back end after 15 s, exit 1
    (the hardening worked: 2a REFUSED instead of printing a reset that deleted nothing)
```

**THE POSITIVE CONTROL, and it is the important line of the whole run.** `tools/PauseSim`,
launched by the RUNNER through `Invoke-External` - the exact path that found the back end in
0.3 s in V5 - failed the same way:

```
runs/20260915T114001Z_run/pausesim-pause.stdout.log   (appNo 4443, fired 11:45:56Z)
    connection config = ...MAK-ONE-2025-Config.xml  exists=YES  source=bound stack
    [OK] joined (BackendCount=0 immediately after Start).
    [..] settling - ticking until a backend is discovered (up to 15 s)...
    [FAIL] no backend discovered after 15 s (BackendCount=0). The pause was NOT sent
    (identical for pausesim-resume, appNo 4444, 11:46:57Z)
```

Meanwhile, **in the same federation, at the same time**:

```
runs/20260915T114001Z_run/vrfc2simapp.log:29
    Backend discovered (BackendCount=1) after 0.1 s.
watchvrf-trace.csv        reflected=48 readable=45 from t=43.8s to the end of the window
the R5 order dispatched 3 TASKSTRT and unit POS tracks changed - the sim clock was running
```

---

## 3. THE TIMELINE THAT MATTERS (V6b, from `run-manifest.json`)

| UTC | who joined | outcome |
|---|---|---|
| 11:40:04 | rtiexec started FRESH by this run | - |
| 11:40:59 | LaunchVrf52 -> sim up, scenario loaded | - |
| 11:41:45 | **WatchVrf-precheck** (short-lived, 35 s, quiet back end, no units yet) | reflected=2 **readable=1** |
| 11:42:20 | **WatchVrf-trace** (long-lived) | reflected=2 -> 48 |
| 11:42:43 | PushInit finished | - |
| 11:42:45 | **VrfC2SimApp** | **BackendCount=1 after 0.1 s** |
| 11:42:54 | PushOrder | - |
| 11:43:47 | first gate tool (4428) | **BackendCount=0 after 15 s** |
| 11:44:28 .. 11:47:21 | 4429, 4430, 4431, 4432, 4434, 4435 | all BackendCount=0 |
| 11:45:56 / 11:46:57 | **runner-launched PauseSim** 4443 / 4444 | **BackendCount=0** |

Two readings fall straight out of this table:

1. **The two channels behave differently.** `WatchVrf-precheck` is a SHORT-LIVED federate that
   joined a QUIET federation with no units in it, and it still got object discovery *and*
   attribute data (`readable=1` - the back end's own control object). So HLA object reflection
   reaches a late, short-lived joiner. The VR-Forces back-end STATUS does not. **The failure is
   specific to the VR-Forces message channel (DtSimMessage interactions), not to HLA
   reflection.**
2. **Everything that joined at or before 11:42:45 succeeded; everything from 11:43:47 failed.**
   The app is not a counterexample to "late joiners fail": it joined 2 s after PushInit, i.e.
   into a back end that was about to create six units. From 11:43:24 (order on the bus) the
   scenario is six units crawling and the back end's *status* - loaded, running, scenario name -
   never changes again.

---

## 4. FALSIFIED HYPOTHESES (each with how)

| # | hypothesis | how it died |
|---|---|---|
| 1 | null `ConnectionConfigFile` (the original) | offline native probe: the vendor default resolves cwd-relative to `..\appData\settings\connections\MAK-ONE-2025-Config.xml`, which EXISTS from the bin64 cwd every gate had; no gate log carries the vendor's `Unable to load configuration file` line (that line IS in `v6_partA_out.txt` for the 4367 attempt with the wrong cwd, so the signature is known) |
| 2 | the process lost its cwd (`Start-Process` vs `$PWD`) | replayed offline with the probe under both launch styles: child cwd = the 5.2d bin64, config `exists=YES` |
| 3 | `MAK_VRLDIR` / `MAK_VRFDIR` inherited from the Machine env (5.0.2 / vrlink5.8) | the resolver ignores them (probe), and a byte scan finds `MAK_VRFDIR` in NO 5.2d/vrlink5.10 binary, `MAK_VRLDIR` only in `managedInterface.dll` / `vrfLauncher.exe`, `MAK_RTIDIR` only in `vrvVrlQt.dll` |
| 4 | a different `VrfBridge` build in the converted tools | SHA256 `E3F405249C561284`, 997376 B, identical in all ELEVEN consumer trees |
| 5 | a different `StartupConfig` (SessionId / SiteId / DeviceAddress / HostInetAddr / identity) | the literals are identical across all nine tools, and the app's `BuildStartupConfig` differs only in reading them from settings |
| 6 | a reused appNumber | 4390, 4398, 4428-4435, 4443-4444 were all fresh and all failed |
| 7 | Windows Firewall blocking the new exes | no `Release-5.2` executable has an inbound rule at all, WatchVrf included - and WatchVrf works |
| 8 | **launch context** (runner vs external driver; full vs minimal env; `--config` vs derived) | **V6b killed this outright**: 1a and 1b are identical, and the runner-launched PauseSim failed exactly like the externally-launched tools |
| 9 | a wedged / stale RTI | V6 ran on a PRESERVED rtiexec (up since 09-13), V6b on one started FRESH by the run. Same failure. |

**Not yet excluded, and the reason V6c exists:** the back end's status-message cadence. Nobody
has ever measured it - see sec 5.

---

## 5. THE MECHANISM, FROM THE HEADERS

`VrfBridge.BackendCount()` -> `VrfFacade::BackendCount()` -> `controller->backends().count()`
(`src/VrfFacade/VrfFacade.cpp:676-678`).

* `DtVrfRemoteController::backends()` (`vrfcontrol/vrfRemoteController.h:299`) is the
  `DtVrfBackendListener`'s list (`:2223 DtVrfBackendListener* myListener`).
* `DtVrfBackendListener` is constructed with a **`DtVrfMessageInterface*`**
  (`vrfcontrol/vrfBackendListener.h:84`) and is fed by **VR-Forces messages**:
  `processStatusMessage(DtSimMessage*)`, `processScenarioStatusMessage(DtSimMessage*)`, and the
  static `statusCallback` / `scenarioStatusCallback` (`:178-186`, `:246-252`). The class doc
  (`:73-78`) is explicit: *"the applications are tracked in a list ... isActive is true if the
  application sends out a status message"*.
* **It asks exactly once, and only from its constructor**:
  `//! Send a status request message. //! Non-virtual since it is called from the constructor.
  void sendRequest(const DtSimulationAddress& address = DtSimSendToAll);` (`:265-268`,
  **protected**). There is no public re-request. A federate that misses the answer to that one
  request has no second chance of its own.
* Heartbeating exists on the RECEIVE side - `setTimeoutInterval` / `timeoutInterval` /
  `doTimeouts` (`:157-163`) *"deactivates any status objects which have not responded within the
  timeout interval"* - which implies back ends emit status unprompted. **The period is not
  documented in any header, and the 5.2 Users Guide / IOG / Interop dumps in the scratchpad
  contain nothing on it (searched).**

**The vendor's object-update APIs are the WRONG instrument here** and should not be reached for:
`requestAttributeValueUpdate` (`vl/vrlRtiAmbassador1516e.h:623`), `DtReflectedObject::requestUpdate`
(`vl/reflectedObjectHLA.h:155`), `DtReflectedObjectList::doRequestUpdates` /
`setRequestClassUpdateFlag` / `setRequestObjectUpdateFlag` (`vl/reflectedObjectListHLA.h:232-291`)
and the initializer's `setRequestClassUpdate` / `setRequestObjectUpdate` (default **true**,
`vl/exerciseConnInitializerHLA.h:229-241`) all act on **objects**. A back end is not an object in
this path; it is a sender of interactions. Sec 3's reading is the confirmation: the object
channel was already working for every joiner.

### A real deviation from the vendor sample, found while reading (NOT a proven cause)

`examples/remoteControl/main.cxx:30-56` does, in order: `init(...)`,
`setMonitorBackendState(true)`, `vrfMessageInterface()->setSessionId(...)`, then

```cpp
// Start the local exercise clock
remoteController->communicationManager()->run();
```

`VrfFacade::Start` does the first three in the same order (`VrfFacade.cpp:565-577`) and **never
calls the fourth**. `DtCommunicationManager::run()` is documented as *"Puts the simulation into
run mode by letting the network connections know"* (`vrfMsgTransport/communicationManager.h:163`).
The 5.0.2 build drove the clock itself each tick; Y-6 removed that on 5.2 with the note *"the
5.2d sample does not drive the clock"* (`VrfFacade.cpp:668-671`) and did not add the sample's
`run()` in its place. **So on 5.2 our federates neither drive the exercise clock nor enter run
mode.** This cannot by itself explain app-vs-tool (the app omits it too and works), but it is a
one-line, sample-cited parity gap and it is arm A5 of the V6c prereg.

---

## 6. WHAT THE HARDENING BRANCH DID BUY

`fix/tools-connection-config` `89639c2` (not merged) does not fix the gate, and V6b proves it.
It does three things that V6b demonstrated live:

* every tool PRINTS `connection config = <path> exists=YES source=...` before it joins, so
  "was it the config?" is answerable from the log instead of from an argument;
* `ResetVrf` REFUSED instead of reporting a reset that deleted nothing - the V6 false green
  cannot recur;
* the `VRF_UUID:0:0:0-*` placeholders can never be counted as deletable again (they are
  `DtNonVrfUUIDResolver` ids under the `entity-identifier` scheme - one per reflected list, built
  from the null DIS id - not objects).

---

## 7. NEXT

`docs/experiments/PREREG_V6C_LATE_JOINER_2026-09-15.md`. Do not run another gate until its A0
instrument (WatchVrf `--report-backends`, which the runner has never passed) is on: without it
no run in the record says when a back end's status arrives, and that is the one number the
surviving hypothesis turns on.

---

## 8. V6c (2026-09-15) - THE ARMS RAN, AND THE INSTRUMENT CHANGED THE QUESTION

Two runs: `runs/20260915T122145Z_run` (aborted - its init never dispatched; see RUNBOOK sec 7c)
and `runs/20260915T124231Z_run` (the rerun, all arms). Driver
`scratchpad/validation/v6c_gates.ps1` rev 2, arms per
`PREREG_V6C_LATE_JOINER_2026-09-15.md`.

### 8.1 The arms

| arm | appNo | fired | scenario state | result |
|---|---|---|---|---|
| **A3 EARLY** | 4445 | 12:22:59Z (run 122145Z), by hand in the READY..PushInit window | **empty** | **HIT, 0.2 s** |
| (unplanned) CreateOne diagnostic | 4449 | ~12:29Z (run 122145Z), **+6 min after the app** | **empty** | **HIT, 0.2 s** |
| **A5 POPULATED-QUIET** | 4456 | - | - | **NOT RUN** - the runner pushed the order inside the driver's 30 s post-init hold; 4456 **not burned** |
| **A1 PATIENCE** | 4446 | 12:48:31Z, window+180 s | populated **+ tasked** | **MISS at 180 s** |
| **A2 PROVOKE** | 4447 | 12:52:31Z, window+420 s | populated **+ tasked** | **MISS**, and the broadcast provoked nothing |

Verbatim:

```
A3  (v6c_A3_manual.out, 12:23:00Z)
    back-end settle cap = 15 s (default)
    [OK] joined (BackendCount=0 immediately after Start).
    [OK] 1 backend(s) discovered after 0.2 s.
    [OK] resigned cleanly. RESULT: simulation time multiplier set to 1x   exit=0

A1  (v6c_A1_patience.out/.err, 12:48:31Z)
    back-end settle cap = 180 s (--settle-secs)
    [OK] joined (BackendCount=0 immediately after Start).
    [..] settling - ticking until a backend is discovered (up to 180 s)...
    [FAIL] no backend discovered after 180 s (BackendCount=0).            exit=1

A2  (v6c_A2_provoke.out/.err, 12:52:31Z)
    PROVOKE MODE (V6c arm A2): run() will be issued ONCE at BackendCount=0, BEFORE the settle.
    [OK] joined (BackendCount=0 immediately after Start).
    [..] PROVOKE: issuing bridge.Run() -> controller->run() BLIND at BackendCount=0 ...
    [OK] PROVOKE sent and flushed (2.0 s of ticks); BackendCount=0 immediately after.
    [RESULT] PauseSim action=provoke verdict=PROVOKE_NO_BACKEND basis=backend-count exit=1
             appNumber=4447 backends=0 provoke=yes provokeBackends=0 settleSecs=180
             utc=2026-09-15T12:55:36.358Z
```

### 8.2 THE A0 INSTRUMENT'S FIRST READING - AND IT IS THE RESULT OF THE RUN

The runner now passes WatchVrf `--report-backends`, so for the first time a trace carries a
`backends=` column. `runs/20260915T124231Z_run/watchvrf-trace.csv`, sampled every 2 s by a
LONG-LIVED observer that joined at 12:44:27.8Z:

```
# t=3s     reflected=2  readable=1  backends=1     <- has the back end from its first sample
# t=62.2s  reflected=48 readable=45 backends=1
# t=123.5s reflected=48 readable=45 backends=1
# t=150.1s reflected=48 readable=45 backends=1     <- LAST sample with a back end
# t=152.1s reflected=48 readable=45 backends=0     <- and never again
# t=798.1s reflected=48 readable=45 backends=0
totals: backends=1 on 73 samples, backends=0 on 323
```

**An observer that ALREADY HAD the back end LOST it.** That is not a late-joiner problem and it
cannot be one: this federate joined before the units existed, held `backends=1` for 150 s, and
then dropped to 0 for the remaining 11 minutes while still reflecting all 48 objects.

Put the run's own stamps against it (manifest `clocks`, trace t=0 ~ 12:44:28Z):

| UTC | t | event |
|---|---|---|
| 12:44:27.8 | 0 | WatchVrf-trace joins; `backends=1` from its first sample |
| ~12:44:5x | ~30 | app dispatches the init; 48 objects reflected by t=62 |
| 12:44:57.9 | ~30 | **PushOrder**; the app sends 3 TASKSTRT and dispatches 3 `move-along` tasks |
| 12:46:58 | **150.1** | **last `backends=1`** |
| 12:47:00 | **152.1** | **`backends=0`**, permanently |
| 12:48:31 | 243 | A1 joins and NEVER sees a back end in 180 s |
| 12:52:31 | 483 | A2 joins, provokes, sees nothing in 180 s |

The gap between task dispatch (~t=30-50) and the drop (t=151) is ~100-120 s, which is what a
cached entry aged out by `DtVrfBackendListener::doTimeouts()` looks like
(`vrfBackendListener.h:157-163`, *"deactivates any status objects which have not responded
within the timeout interval"*). **INFERRED, not measured:** the back end went silent at task
dispatch and the observer simply held a stale entry until its timeout expired. The timeout
interval itself is not in any header we have.

### 8.3 And nothing ever moved

The same trace: unit `VRF_UUID:130f47e4-...` sits at `34.654142, -116.693115, 1367.5` at
t=25.5 s, at t=139.8 s and still at t=700.1 s - identical to the metre. 3 TASKSTRT went out,
**0 terminal reports**, 0 TSK/RPT rows in the whole trace, and `-StopWhenComplete` never fired.
So the back end stopped reporting status AND never simulated the tasks - while still accepting a
graceful `StopVrf` twelve minutes later.

### 8.4 The falsified list, extended

| # | hypothesis | how it died |
|---|---|---|
| 10 | **H1 - the status cadence is a period longer than 15 s** | A1 waited **180 s** and got nothing. Twelve times the old cap, no arrival. |
| 11 | **H3 - the one-shot ctor request is lost, so just ask again** | A2 sent a broadcast `run()` at `BackendCount=0` and then waited 180 s. Nothing. A back end that is silent does not answer a command either. |
| 12 | **"late joiners are the problem"** | the CreateOne diagnostic joined **6 minutes late** into an EMPTY scenario and hit in 0.2 s; and in the rerun a federate that joined FIRST still lost the back end at t=151 s. |

### 8.5 What survives: H5, and its two open discriminators

**H5 (refined by 8.2):** the back end stops emitting VR-Forces status messages once the
scenario is populated and/or tasked. Every remote controller then loses it - new joiners never
see it, and established ones drop it when their cached entry times out. An empty, untasked
federation heartbeats normally and answers a joiner in 0.2 s however late it arrives.

Two things H5 does not yet settle:

1. **CREATION or TASKING?** A5 (populated, untasked) is still unfired - the runner pushed the
   order inside the driver's hold. V6d exists for exactly this: `--pre-order-settle 150` widens
   the untasked window, A5 fires in it, and A6 fires after the order. See AMENDMENT 2.
2. **WHY DID V5 WORK?** V5 was populated (128 units) AND tasked, and its late `PauseSim` hit in
   0.3 s. Everything that differed is listed in AMENDMENT 1. A4, the busy control, is the only
   arm that can tell "busy back ends do answer" from "V5's fixture was special".

A third reading is now open and was not before: the back end may not be *refusing* to talk at
all - it may have **stopped running**. It stopped heartbeating, it never moved a tasked unit,
and it produced no completion report, all from roughly the same moment. That is one event with
three symptoms, not three faults, and the object console at notify level 4 is the instrument
that would show it (lessons-vendor-diagnostics-first). V6d's consoles are at 1; raise them if
A5/A6 do not separate creation from tasking.

---

## 9. V6d (2026-09-15) - TASKING IS THE TRIGGER, AND THE BACK END SAID WHY

`runs/20260915T130627Z_run`, driver `scratchpad/validation/v6d_gates.ps1`, launcher
`v6d_launch.sh` (= `v6c_launch.sh` + `--pre-order-settle 150`). **SEAT AMENDMENT, recorded:**
this run was launched with `--object-console 4 --member-console 4`, so the taskees' own consoles
are in `watchvrf-trace.csv` as CON rows. That decision is what turned a yes/no into a cause.

### 9.1 The two arms

| arm | appNo | fired | scenario state | result |
|---|---|---|---|---|
| **A5 POPULATED-QUIET** | 4456 | 13:09:59Z, inside the 150 s pre-order hold | 6 units exist, **nothing tasked** | **HIT** - `[OK] 1 backend(s) discovered after 0.1 s`, exit 0 |
| **A6 TASKED** | 4464 | 13:14:21Z, window + 120 s | the same units, **tasked** | **MISS** - `[FAIL] no backend discovered after 15 s`, exit 1 |

Driver verdict: **TASKING is the trigger.** Creation is not: A5 joined a federation with 6 units
already in it and was answered in 0.1 s.

### 9.2 The same 121 seconds, twice

Both runs carry the `backends=` column, and they agree to within the 2 s sample interval:

| | V6c rerun `124231Z` | V6d `130627Z` |
|---|---|---|
| 3x TASKSTRT on the bus | **12:44:58.950 / .957 / .960** | **13:11:51.150 / .156 / .158** |
| last `backends=1` | t=150.1 s ~ 12:46:58Z | t=299.9 s ~ 13:13:50Z |
| first `backends=0` | t=152.1 s ~ 12:47:00Z | t=301.9 s ~ 13:13:52Z |
| **dispatch -> drop** | **121 +/- 2 s** | **121 +/- 2 s** |
| samples with a back end / without | 73 / 323 | 146 / 324 |

Two runs, dispatch stamps 27 minutes apart, **the same 121 s**. That is not a cadence and not a
race: it is a fixed timeout. The back end goes silent AT DISPATCH, and each already-joined
observer keeps its cached entry until `DtVrfBackendListener::doTimeouts()` deactivates it
(`vrfBackendListener.h:157-163`). A5 fired at t~70 s - inside the `backends=1` window - and hit;
A6 fired at t~331 s, after the drop, and missed. Every arm is consistent with one event.

### 9.3 THE CONSOLE ACCOUNT - the back end's last words

2,525 CON rows, spanning **t=23.1 s .. t=180.7 s and not one after**:

```
rows per 10 s:  t=20-29  30 | t=80-89  24 | t=140-149  23 | t=170-179  136 | t=180-189  2312
                                                            ^ 2,432 rows in the FINAL SECOND
```

t=180.7 s is **13:11:51Z** - the TASKSTRT instant. The burst IS the dispatch, and the console
then stops for the remaining 13 minutes of the run.

What the burst contains, in order: the tasks are ACCEPTED and STARTED -
`Move-Along Route: "T_R5_PL1 ROUTE"` / `T_R5_CO1 ROUTE` / `T_R5_TK1 ROUTE`,
`Controller ... beginning to process move-along task`, `Task 0 starting subtask maneuver-along`,
`maneuver-in-formation: unitRoute=...`, `Setting navigation preference to ignore-roads` (36x),
`Task 0 starting subtask ground-vehicle-move-to` (15x) - and then every member walks the
movement behaviour tree. The **last 20 rows**, at sim time 2322.81, are one member's tree:

```
Starting selector node Select off-road nav planning or feature planning
  Starting sequence node Maybe plan off-road nav path
    Starting condition node Is road following disabled?    -> Condition true.
    Starting condition node Is current point in nav area?  -> Condition FALSE.
    fail in action Is current point in nav area?
  Starting sequence node Plan off feature path
    Starting job node Plan path
Not using roads for move planning.
Checking status of job for M1A2 10
<- and nothing, ever again>
```

**`Is current point in nav area?` is FALSE because this fixture has no nav area.**
`R9_Mojave_Empty_52` is the plain variant - no nav data at all (the `_Nav*` / `_NavAO20_AG_S2`
variants are the ones that carry it). So every member fails that condition, falls through to
`Plan off feature path`, starts an asynchronous `Plan path` job, and the engine's last observable
act is polling it: *Checking status of job for M1A2 10*.

### 9.4 Liveness: it did not crash, it stopped

* **Nothing moved.** 22 units with real coordinates; the three sampled are at identical
  lat/lon/alt at t=23.5 s and t=960.2 s. 3 TASKSTRT, **0 terminal reports**, 0 TSK/RPT rows.
* **No fault.** Count-only grep of the vendor sim log
  (`vrfSimHLA1516e5.2d-20260915-090640-...log`, 13,883 lines): `assert` 0, `deadlock` 0,
  `ERROR` 0, `FATAL` 0, `Exception` 0; no `.callstack.log` and no `.dmp` for the run. The same
  for the V6c rerun's log. The back end never faulted.
* **It still shut down cleanly.** `StopVrf: EXIT=0`, *VR-Forces is down (graceful)* - twelve
  minutes after it stopped saying anything.

### 9.5 The interface never noticed - and could not have

Asked directly, because a product that drives a dead back end in silence is a defect:

* the app logged `Backend discovered (BackendCount=1) after 0.1 s` at startup and **no warning
  of any kind afterwards** (`has not advanced past`: 0 occurrences, both runs);
* it kept working the whole time: 91 R1 cycles, **543 position reports delivered, 0 failed, 0
  cycles with a kinematics read failure** - it was reading attributes off reflected objects,
  which the RTI still held, so every read succeeded on stale data.

It is not a stale cache. **The reading is never taken again.** `VrfC2SimService` re-reads
`BackendCount` only inside the task-clock stale branch - `taskSimStale = heldOnSim && obs.Stale`
(`VrfC2SimService.cs:4121-4127`) - and `heldOnSim` is only true while a task is waiting on an
end time. The R5 order gives **no Duration** (the app says so per task: *"the order gives NO
Duration, so this task has no end time"*), so nothing was ever held, the branch never ran, and
the interface had no mechanism to discover that its back end was gone. **Product defect, and it
is not confined to this fixture:** any order without Durations leaves the interface blind to a
dead back end for the whole run.

### 9.6 VERDICT

**Dispatching the `move-along` tasks stops the back end.** Not "silences" - stops. In the same
second it accepts the tasks it emits 2,432 console lines, drives every member into off-feature
path planning because the point is not in a nav area, starts a `Plan path` job, polls it once -
and then never emits another console line, never moves a unit, never completes a task and never
sends another status message. 121 s later every remote controller has dropped it.

**The strongest competing hypothesis, and why it loses.** *C2 - the back end deliberately stops
publishing status while tasks run.* It explains the silence and nothing else: a deliberate
silence would not freeze the units, would not suppress the object console, and would not
withhold terminal reports for 13 minutes. C2 predicts 1 of 4 symptoms; **C1 - a hang or
non-returning busy loop in the movement/path-planning machinery** - predicts all four (no
frames, so no status, no motion, no console, no reports) and is what the console's last lines
show the engine entering. C1 is ADOPTED as the working cause.

**What is still ASSUMED, not verified:** that the engine is *in* the `Plan path` job rather than
stopped just after it. Nobody has sampled the process. `--sample-threads` was passed on every
V6 run and **no thread/CPU artifact reached any run directory** - so the one reading that would
separate a spinning loop from an idle stall has never been taken. Take it on A4.

**Why V5 did not do this** falls straight out: V5's fixture is
`R9_Mojave_Empty_52_NavAO20_AG_S2` - it HAS a nav area. `Is current point in nav area?` is TRUE
there, the members plan through the mesh instead of falling into off-feature planning, the
engine keeps running, and a late joiner finds the back end in 0.3 s. That is now a prediction
about A4, not a loose end - see AMENDMENT 3.

This also joins the ridge / early-stops thread rather than sitting beside it: the same
`Is current point in nav area?` condition and the same off-feature fallback are what the
G-series was reading when nav queries returned 0 points. The new part is that on a fixture with
NO nav area the engine does not merely plan badly - it stops.

---

## 10. A4 - THE BUSY CONTROL: PREDICTED HIT, AND IT HIT

`runs/20260915T133259Z_run`, V5's fixture unchanged (`R9_Mojave_Empty_52_NavAO20_AG_S2` +
COA-STP1 init + the `PROBE_RIDGE_1-35` order + the nav-area gate + `AtOrder` + DurationScale
0.25). `SetSimRate 1 4448 --settle-secs 180` at **window + 180 s = 13:39:37Z**, three minutes
after the ridge order dispatched:

```
    back-end settle cap = 180 s (--settle-secs)
    [OK] joined (BackendCount=0 immediately after Start).
    [OK] 1 backend(s) discovered after 0.3 s.
    [OK] command issued ... RESULT: simulation time multiplier set to 1x     exit=0
```

**HIT in 0.3 s - exactly what AMENDMENT 3 registered as the predicted outcome.** (The driver's
trailing verdict line still carries pre-V6d 'H2 / provoke-then-wait' text; the registered
reading is AMENDMENT 3's, and it is the one scored here.)

### 10.1 Scored against AMENDMENT 3

| prediction | outcome |
|---|---|
| A4 HIT -> the nav-area account holds; the trigger is not tasking as such but tasking a ground move onto terrain with **no nav area** | **MET** |
| "Confirm by checking that A4's `backends=` column never drops" | **MET - 347 samples, every one `backends=1`, zero at 0** |
| the tasked units should drive, as in V8b | **MET - 7 objects moved 2.29-2.45 km** (1-35's members + its proxy); the other 128 COA context units are untasked and correctly static |

### 10.2 The console and the process - what a RUNNING back end looks like

* **162,787 CON rows** spanning t=39.1 .. 676.9 s, ~15,000 per minute, **continuous to the end**
  of the run. Compare V6d: 2,525 rows, all inside one 157 s stretch, then nothing for 13 minutes.
* **The process sampler** (`scratchpad/validation/a4_simsampler.csv`, 78 samples at 10 s):
  `cpu_s` 2.2 -> 2406.0, i.e. **2,404 CPU-seconds over ~770 s wall = ~312% of one core**,
  sustained (per-10 s deltas mean 31.2 s, max 43.9 s); **threads 8 -> 83**; working set
  212 MB -> 4,040 MB. That is a healthy multi-threaded engine under load.
* **8,297 reports** captured (vs 549 in V6d, 459 in the V6c rerun).

**The quiet runs have no such sample.** The sampler was added for A4, so V6c/V6d's back end was
never measured while it was stopped. A4 therefore establishes only the RUNNING baseline; it does
not tell a spinning loop from an idle stall on the quiet fixture. Carry the sampler on V6e.

### 10.3 The interface still did not notice - on a healthy run this time

Same reading as V6d, and it is not a fixture property: `has not advanced past` = 0, 66 R1
cycles, **8,169 position reports, 0 failed, 0 kinematics read failures**. Here the back end was
alive, so nothing was missed - but the mechanism that would have told the interface either way
still never ran. STP-822 stands.

---

## VERDICT - THE V6 LANE, CLOSED

| run | fixture | nav data | SMS | state at join | back end found? |
|---|---|---|---|---|---|
| **V6** 030650Z | `R9_Mojave_Empty_52` | **none** | vendor | populated + tasked | **NO** (3 placeholder uuids, a false-green reset) |
| **V6b** 114001Z | `R9_Mojave_Empty_52` | **none** | vendor | populated + tasked | **NO** - and the runner-launched PauseSim failed too |
| **V6c** 122145Z | `R9_Mojave_Empty_52` | **none** | vendor | **empty** (init never dispatched) | **YES**, 0.2 s - twice, one of them 6 min late |
| **V6c** 124231Z | `R9_Mojave_Empty_52` | **none** | vendor | populated + tasked | **NO** at 180 s; a blind broadcast provoked nothing |
| **V6d** 130627Z | `R9_Mojave_Empty_52` | **none** | vendor | **populated, UNtasked** | **YES**, 0.1 s |
| **V6d** 130627Z | `R9_Mojave_Empty_52` | **none** | vendor | populated **+ tasked** | **NO** at 15 s |
| **A4** 133259Z | `R9_Mojave_Empty_52_NavAO20_AG_S2` | **AO20 area** | custom AG/Slope2 | populated **+ tasked** | **YES**, 0.3 s, and `backends=1` on all 347 samples |

**The cause.** On a fixture with NO nav area, dispatching a ground `move-along` STOPS the back
end. Its own level-4 console shows the last thing it does: every member walks the movement
behaviour tree to `Is current point in nav area?` -> **FALSE** -> `fail in action` -> `Plan off
feature path` -> `Starting job node Plan path` -> `Checking status of job for M1A2 10`, and then
emits nothing ever again. No frames, so no status heartbeat (every controller drops it exactly
121 s later, twice measured), no motion (22 units at identical coordinates for 11 minutes), no
terminal reports. It does not crash - 0 assert / 0 deadlock / 0 ERROR / 0 FATAL in the vendor
log, no dump, and `StopVrf` still exits 0. With a nav area present the same order on the same
engine runs for the whole window at ~312% CPU, moves its units 2.4 km and answers a late joiner
in 0.3 s.

### The adversarial paragraph

**The strongest residual confound is that A4 did not change one variable, it changed three.**
V5's fixture brings nav data, a CUSTOM SMS (`C2SIM_EntityLevel_AbstractGraphs_Slope2.sms`, which
turns on abstract graphs and a slope-avoidance factor) **and** a 128-unit init with `AtOrder`
creation. Any of the three could be what keeps the engine alive. The nav-area account is the
only one with a mechanism written in the back end's own words - the console names that exact
condition as the branch it fails - but "the console named it" is not "the console proved the
others irrelevant". A custom SMS that replaces the movement model could equally well be avoiding
the same dead end by another route.

That is decidable in ONE run, and it is written up as AMENDMENT 4 (**V6e**): the deployed
`R9_Mojave_Empty_52_Nav` fixture is **MojaveAO20 nav data with the VENDOR SMS**
(`$(DATA_DIR)\simulationModelSets\EntityLevel.sms`) - verified by reading the `.scn` out of the
`.scnx` - and the AO20 area (centre 34.608 N, -116.700 W, +/-10 km) **contains the R9 lean
init's units** (34.650-34.654 N, -116.689 to -116.693 W). So V6e = V6d with nav data added and
nothing else changed. HIT exonerates the SMS and the init size; MISS puts them back in play.

Two further things this lane did NOT establish, stated so nobody assumes them:

1. **Whether the stopped back end is spinning or idle.** Never sampled on a quiet run.
2. **Whether it is inside the `Plan path` job or stopped just after it.** The console's last
   line is a status poll for that job; that is where it ends, not necessarily where it sits.

### What follows for the product

* **STP-823 - nav data is a PRECONDITION for ground tasking.** Not an optimisation, not a
  quality setting: without a nav area a ground `move-along` stops the simulator. Every AO the
  scenario-prep capability produces must carry a nav area covering the units it will task
  (STP-802's tiling rule already exists; this makes it mandatory rather than advisable).
* **STP-822 - the interface must (a) REFUSE a ground task when the taskee's position is not in a
  nav area, and (b) read back-end liveness ON A TIMER.** Today it re-reads `BackendCount` only
  in the task-clock stale branch, which needs a task held on an end time; an order without
  Durations leaves it blind for the whole run. Both V6c and V6d show it happily reporting
  positions off stale reflected data while the engine behind them was stopped.
* **The runner passes `--report-backends` always** (arm A0, on main since `0ecc14a`). Every
  finding in sections 8-10 rests on that column; before it existed, five runs could not tell a
  joiner problem from a dead back end.
