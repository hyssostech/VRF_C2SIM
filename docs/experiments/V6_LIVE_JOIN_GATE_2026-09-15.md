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
