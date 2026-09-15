# PREREG V6c - WHY A LATE JOINER NEVER SEES THE BACK END

Written 2026-09-15 12:15Z, BEFORE any V6c run. Predictions and the STOP are on record first.
Evidence base: `docs/experiments/V6_LIVE_JOIN_GATE_2026-09-15.md` (V6 + V6b, nine falsified
hypotheses). Ticket STP-820.

**NOTHING IN THIS FILE HAS BEEN RUN.** No arm below was executed by the session that wrote it.

---

## 0. THE QUESTION

A bridge federate that joins an established 5.2 federation gets HLA object discovery and
attribute data but never learns a VR-Forces back end exists (`BackendCount` stays 0). The same
code in `VrfC2SimApp` sees `BackendCount=1` in 0.0-0.1 s, in every run, busy or quiet.

**Which is it: the back end never sends a status message a late joiner can hear, or it sends one
on a period longer than the 15 s every tool waits?**

## 1. WHAT IS ALREADY SETTLED (do not re-derive)

* `BackendCount` = `DtVrfBackendListener::backends().count()`, fed ONLY by VR-Forces
  **status messages** (`DtSimMessage`), never by object attributes -
  `vrfcontrol/vrfBackendListener.h:73-78`, `:178-186`.
* The listener asks for status **once, from its constructor** (`sendRequest`, **protected**,
  `:265-268`). There is no public re-request.
* Heartbeating is implied by `setTimeoutInterval` / `doTimeouts` (`:157-163`), but **no header
  and no vendor guide in the scratchpad dumps states the period**.
* Object-update APIs (`requestAttributeValueUpdate`, `DtReflectedObjectList::doRequestUpdates`,
  `setRequestClassUpdate/ObjectUpdate`) act on OBJECTS and are the wrong instrument.
* Falsified already, do not re-test: connection config, cwd, `MAK_*` env, bridge build,
  `StartupConfig`, appNumber, firewall, launch context (runner vs external), RTI freshness.

## 2. HYPOTHESES

**H1 (leading) - PERIOD.** Back ends emit status on a fixed period P > 15 s. Every tool's 15 s
cap is simply too short; the app wins because it joins during the init burst, when the back end's
status is changing and it emits immediately.

**H2 - ON-CHANGE ONLY.** Back ends emit status only when their state changes (loaded / running /
paused / scenario). A quiet scenario emits nothing, ever, and no wait length helps. This is
consistent with V5, where a late `PauseSim` found a back end in 0.3 s in a busy 128-unit COA
scenario.

**H3 - ONE-SHOT RACE.** The constructor's `sendRequest` goes out before the federate's
interaction subscription is effective, so its answer is lost; the federate then depends on H1/H2.
(H3 is not exclusive with H1 or H2 - it is why the miss is never repaired.)

**H4 - RUN MODE.** Our federate never calls `communicationManager()->run()`, which the 5.2d
sample calls right after `setSessionId` (`examples/remoteControl/main.cxx:56`;
`vrfMsgTransport/communicationManager.h:163` - *"Puts the simulation into run mode by letting the
network connections know"*), and since Y-6 it does not drive the exercise clock either
(`VrfFacade.cpp:668-671`). H4 alone cannot explain app-vs-tool (the app omits it too) but could
be a necessary condition for the status exchange.

## 3. ARMS - ONE RUN, THE QUIET 6-UNIT FIXTURE (V6b's), NOTHING NEW BUILT EXCEPT A0

**A0 - THE INSTRUMENT, AND IT IS A PRECONDITION.** `scripts/RunC2SimScenario.ps1` has NEVER
passed `--report-backends` to either WatchVrf (grep: zero hits), so no run in the record says
when a back end's status reaches an observer. Pass it to BOTH the precheck and the trace
observer. Every `# t=` sample line then carries ` backends=<n>` at 2 s resolution.
*This is a one-flag runner change and it must land before any arm below is interpreted.*

**A1 - PATIENCE.** One tool invocation with the settle cap raised from 15 s to **180 s**
(`SetSimRate --settle-secs 180`, or an equivalent env/flag; a managed change only, no bridge
rebuild). Fire it at t+180 s of the observation window, deep in the quiet phase.

**A2 - PROVOKE.** A second invocation that, immediately after `Start()`, sends ONE broadcast
remote-control message that is a no-op on an already-running scenario - `PauseSim resume`
(`controller->run()`, no address, applies to all back ends) - and only then waits 180 s.
NOTE: today the tools refuse to send anything at `BackendCount==0`; A2 needs a probe build that
sends first and asks afterwards. That is a deliberate, one-arm-only relaxation of the
false-green rule and must not be merged.

**A3 - EARLY.** One tool invocation fired between `VR-Forces READY` and `PushInit` (i.e. in the
same window the app joins in), with the SAME 15 s cap as V6b.

**A4 - BUSY CONTROL.** Re-run V5's fixture (`R9_Mojave_Empty_52_NavAO20_AG_S2` + COA-STP1 init)
and fire the same tool at the same offset as V6b's PauseSim.

**A5 - RUN MODE (deferred, native).** Add `p_->controller->communicationManager()->run();` after
`setSessionId` in `VrfFacade::Start`, for vendor-sample parity. **NOT built by this prereg**: it
is a native change, so it drags the whole gate G-A eleven-consumer redeploy behind it, and it is
justified by parity, not by evidence that it is the cause. Build it only if A1-A4 leave H4
standing.

## 4. PREDICTIONS (write the miss down too)

| arm | H1 true | H2 true | H3 only | miss = |
|---|---|---|---|---|
| A0 | the long-lived observer shows `backends=1` from its first sample and it never drops | same | same | `backends=0` on a long-lived observer would REFUTE everything above and make the observer the next subject |
| A1 | `backends>=1` at some t between 15 s and 180 s - **record t, that is P** | still 0 at 180 s | still 0 | - |
| A2 | irrelevant (A1 already hit) | `backends>=1` within 2 s of the send | `backends>=1` within 2 s | still 0 after the send => the back end does not answer commands with status either |
| A3 | hits in < 1 s | hits in < 1 s | hits in < 1 s | a MISS here is the strongest result in the set: it would kill "join window" and leave only a property of the tool process itself |
| A4 | hits in < 1 s (reproduces V5) | hits in < 1 s | hits in < 1 s | a MISS refutes the busy/quiet contrast and voids H2 |

**HIGH-CONFIDENCE PREDICTION (the one that stops the work if it misses):** A3 hits. The app,
`WatchVrf-precheck` and `WatchVrf-trace` all joined in that window and all succeeded, in both a
quiet and a busy scenario. If a plain tool fired in that window ALSO fails, then the difference
is not the join window at all, and every environmental and temporal hypothesis in this file is
wrong together.

## 5. STOP CONDITIONS

* **A1 hits** -> the fix is a longer, documented settle cap plus A0's instrument. Raise the cap
  to `2P` in every tool, record P in RUNBOOK sec 9, close STP-820. **Stop the native hunt.**
* **A1 misses and A2 hits** -> the fix is "provoke, then wait": the tools get a documented
  no-op broadcast before the settle. Design it so a tool still never reports success on a
  no-op action.
* **A1 and A2 both miss, A3 hits** -> the back end only talks to federates that join inside a
  window. The product answer is that short-lived tools must be launched by the runner inside
  that window; write it into the RUNBOOK and stop.
* **A3 misses** -> STOP AND ASK. Every frame in this file is wrong and the next step is a
  supervisor decision, not another probe.
* Four passes without a verdict -> stop and ask (CLAUDE.md).

## 6. RULES FOR THE RUN

* ONE fresh ledgered appNumber per invocation; a burned number stays burned.
* Never kill a joined federate; never kill rtiexec / rtiForwarder / rtiAssistant.
* No agents running during the timed run (`vrf-navigation-data-headless`, G3).
* Live reads steer the next probe only; the harvest reader's verdict is what goes to Jira
  (`lessons-live-reads-are-provisional`).
* A2's probe build is a PROBE. It must not be merged and must not be left in a deployed tree.

---

## HOW THE ARMS ARE FIRED

Written 2026-09-15 before the run. Two runs, four arms, four ledgered appNumbers (claimed by the
seat, ledger commit 96e5a96). **Nothing below has been executed.**

### Run 1 - the QUIET fixture (arms A3, A1, A2)

```
shell 1:  bash scratchpad/validation/v6c_launch.sh          # R9_Mojave_Empty_52, 6 units, --run-secs 720
shell 2:  pwsh -NoProfile -File scratchpad/validation/v6c_gates.ps1
```

`v6c_launch.sh` is `v6b_launch.sh` with the PauseSim probe removed (V6b already used the
runner's own PauseSim as the positive control and it failed with everything else), `--run-secs`
raised to 720 so the window outlasts A2 + its settle, and `--log ...\v6c_runner.log`.
`--client-id STP`, the fixture, the init, the order, the type map and the consoles are V6b's,
unchanged: the arms change WHEN a tool joins and WHAT it does on joining, nothing else.

| arm | appNo | fired at | exact command |
|---|---|---|---|
| **A3 EARLY** | **4445** | the runner log's `VR-Forces READY` **+ 5 s**, and only while `PushInit: EXIT` has NOT yet appeared (Stage 3b's ~45 s settle is the window) | `SetSimRate.exe 1 4445 --settle-secs 15` |
| **A1 PATIENCE** | **4446** | **t+180 s** of the observation window (`Stage 8b - observation window` + `PushOrder: EXIT=0`) | `SetSimRate.exe 1 4446 --settle-secs 180` |
| **A2 PROVOKE** | **4447** | **t+420 s** of the same window | `PauseSim.exe resume 4447 --provoke --settle-secs 180` |

### Run 2 - the BUSY control (arm A4)

```
shell 1:  bash scratchpad/validation/v6c_busy_launch.sh     # V5's fixture, --run-secs 600
shell 2:  pwsh -NoProfile -File scratchpad/validation/v6c_busy_gates.ps1
```

| arm | appNo | fired at | exact command |
|---|---|---|---|
| **A4 BUSY CONTROL** | **4448** | **t+180 s** of the observation window - the SAME offset as A1 | `SetSimRate.exe 1 4448 --settle-secs 180` |

`v6c_busy_launch.sh` is `v5_launch.sh`'s scenario line (R9_Mojave_Empty_52_NavAO20_AG_S2 +
COA-STP1 init + PROBE_RIDGE_1-35 order + the nav-area gate + DurationScale 0.25 + the de-stack
settings + the read-only AO20 navData warm) with the pause probe removed, `--run-secs 600`, and
`--log ...\v6c_busy_runner.log`. A4 is A1 with ONE variable changed: the scenario under it.

### A2 needed a new flag, and it is built

`tools/PauseSim --provoke` (worktree `fix/tools-connection-config`, commit **b160aaa**) issues
`bridge.Run()` -> `controller->run()` ONCE, immediately after `Start()` and BEFORE the settle -
i.e. deliberately at `BackendCount=0` - flushes it with 2 s of ticks, then settles normally.
**resume only**; `--provoke pause` is exit 2, because `run()` is a no-op on an already-running
scenario while `pause` would stop one just to ask a question. The blind send is stated in a
`PROVOKE MODE` banner and on the `[RESULT]` line (`provoke=`, `provokeBackends=`,
`settleSecs=`, `settleTookSecs=`, appended at the END - the format is a contract the runner
greps), and a settle that still finds nothing emits
`[RESULT] PauseSim action=provoke verdict=PROVOKE_NO_BACKEND`. That verdict is arm A2's ANSWER,
not a tool fault. `v6c_gates.ps1` refuses to start if the deployed PauseSim lacks the flag.

### What every arm shares, and what the drivers refuse

* The FULL runner `ProfileEnv` for every child - PATH prefix, `MAK_VRFDIR`,
  `MAK_VRLDIR=C:\MAK\vrlink5.10`, `MAK_RTIDIR`, `RTI_RID_FILE`, `RTI_ASSISTANT_DISABLE`,
  `MAKLMGRD_LICENSE_FILE` - and `-WorkingDirectory C:\MAK\vrforces5.2d\bin64` passed
  explicitly, never inherited. (V6b showed none of this is the variable; it is held fixed so it
  cannot become one.)
* Both drivers key on the run's OWN markers in its OWN log, polling `VR-Forces READY` every 1 s
  for A3 and `Stage 8b` + `PushOrder: EXIT=0` for the rest, and ABORT without firing if the log
  matches dry-run text (`would `, `DRY RUN`, `Nothing was launched`) or if the runner has
  already reached teardown.
* A tool that printed `[OK] joined` is NEVER killed. rtiexec / rtiForwarder / rtiAssistant are
  never touched. One appNumber per join, BURNED on launch whatever the outcome - and an arm the
  driver declines to fire says so in the log and leaves its number unburned.
* Per-arm stdout/stderr to `v6c_<arm>.out` / `.err`; the driver log is `v6c_gates.log` /
  `v6c_busy_gates.log`, UTC-stamped.
* Arm A0 is already in the runner (main, `0ecc14a`): WatchVrf gets `--report-backends` when the
  deployed binary advertises it, so BOTH runs' traces carry a `backends=` column. Read it.

---

## AMENDMENT 1 (2026-09-15, after run 20260915T122145Z): LATE JOIN ALONE IS NOT THE VARIABLE

Run 122145Z never dispatched its init (a separate defect - the app's deployed content root had
lost `appsettings.json`, so `C2SIM:SubmitterId` was unset and the late-join QUERYINIT threw
`Error - Submitter not specified`; see the RUNBOOK sec 7c note). A1 and A2 therefore did not
fire. But the run produced two results that change this prereg:

**A3 HIT.** Fired by hand at 12:22:59Z inside the `VR-Forces READY`..`PushInit` window
(`SetSimRate 1 4445 --settle-secs 15`, full ProfileEnv, cwd bin64):
`[OK] 1 backend(s) discovered after 0.2 s`, multiplier set, clean resign, exit 0.
`scratchpad/validation/v6c_A3_manual.out`. appNo 4445 is CONSUMED. **The prereg's
high-confidence prediction held**, so its STOP did not trigger.

**AND A LATE JOINER SUCCEEDED TOO - in an EMPTY scenario.** The runner's own CreateOne
diagnostic (appNo 4449) joined at ~12:29Z, **six minutes** after the app, and got
`[OK] backend discovered (BackendCount=1) after 0.2s`; it created ORACLETEST
(`createone-diagnostic.stdout.log`). That federation had NO units and NO tasks, because the
init never dispatched.

So the contrast is no longer early-vs-late. It is:

| run | scenario state when the tool joined | offset | result |
|---|---|---|---|
| 122145Z A3 | empty | early (in the app's window) | **HIT** 0.2 s |
| 122145Z CreateOne | **empty** | **+6 min, late** | **HIT** 0.2 s |
| V6 / V6b tools | 6 units created AtInit, R5 order dispatched | late | **MISS**, every one |
| V5 PauseSim | 128 units, tasks running | late | HIT 0.3 s |

### H5 - THE POPULATED/TASKED FEDERATION IS THE TRIGGER

Once the app's controller has CREATED objects and/or DISPATCHED TASKS, the back end stops
answering a new remote controller's status request - or the request/response path stops
working for a joiner that arrives after that point. An empty federation answers in 0.2 s no
matter how late the joiner is.

H5 SUPERSEDES the early/late framing of H1-H3 (the cadence question stays open underneath it:
if H5 holds, the "cadence" is simply that a busy back end never volunteers status).

**V5 REMAINS THE COUNTEREXAMPLE AND IS NOT EXPLAINED.** It was populated (128 units) AND tasked,
and its late joiner hit in 0.3 s. What differed from V6/V6b, all of it, so the next reader does
not have to re-derive the list:

* fixture `R9_Mojave_Empty_52_NavAO20_AG_S2` (AO20 nav data + the custom SMS with
  `useAbstractGraphs=true`), not the plain `R9_Mojave_Empty_52`;
* a RELOCATED appData tree for the sim (the custom SMS lives outside the vendor appData);
* `Vrf__CreationPolicy=AtOrder`, not `AtInit` - units are created when an order references
  them, so creation is spread out rather than a single burst;
* a 128-unit COA-STP1 init instead of a 6-unit lean init;
* `Vrf__DurationScale=0.25`, the de-stack settings, `--pre-order-gate nav-area`, consoles at 3.

Any of those could be the difference; none is tested by the arms below. A4 (the busy control)
reproduces V5's fixture EXACTLY for that reason - it is the only arm that can tell "busy back
ends do answer" from "V5 was special".

### NEW ARM A5 - POPULATED-QUIET (appNo 4456)

`SetSimRate 1 4456 --settle-secs 15`, fired **~30 s after the app dispatches the init** (the
runner's Stage 6d line `interface dispatched N units`) and **BEFORE `Stage 8 - PushOrder`**.
Objects EXIST; nothing is TASKED. Wired into `v6c_gates.ps1` rev 2.

With A3 and A1 it makes a three-point curve out of a yes/no:

| point | scenario state | prediction if H5 = creation | prediction if H5 = tasking |
|---|---|---|---|
| A3 | empty | HIT (measured) | HIT (measured) |
| **A5** | populated, untasked | **MISS** | **HIT** |
| A1 | populated + tasked | MISS | MISS |

**A5 HIT + A1 MISS** -> TASKING is the trigger. Look at what dispatching a task does to the
back end's message interface.
**A5 MISS** -> CREATION is the trigger; the suspect is the app's own controller having created
objects, and the next probe is a run where the app joins but creates NOTHING while a second
federate creates the units.
**A5 HIT + A1 HIT** -> nothing is the trigger and the 122145Z CreateOne result was the anomaly:
STOP AND ASK.

### Arm order, revised

A3 (seat-fired, done) -> **A5** at init+30 s -> A1 at window+180 s -> A2 at window+420 s, then
A4 in its own run. A0's `backends=` column is on in both runs.
