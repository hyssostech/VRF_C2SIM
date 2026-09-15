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

---

## AMENDMENT 2 (2026-09-15, after the V6c rerun): PREREG V6d - CREATION OR TASKING?

**Nothing in this amendment has been run.**

### What V6c settled, and what it left

Results in `V6_LIVE_JOIN_GATE_2026-09-15.md` sec 8. In short: A1 waited **180 s** and saw no
back end (**H1 dead**); A2's blind broadcast `run()` provoked nothing (**H3 dead**); A3 and the
CreateOne diagnostic both hit in 0.2 s in an EMPTY scenario, the latter **6 minutes late**
(**"late joiners are the problem" dead**). And the A0 instrument produced the finding of the
run: a LONG-LIVED observer that held `backends=1` for 150 s **lost it** at t=151 s - ~2 minutes
after the order was pushed - and never got it back, while still reflecting all 48 objects. The
tasked units never moved and no terminal report was ever produced.

So H5 stands, refined: **the back end stops emitting status once the scenario is populated
and/or tasked**, and every controller loses it. **A5 never fired** - the runner pushed the order
inside the driver's 30 s post-init hold - so creation and tasking are still not separated. That
is the whole job of V6d.

### The run

```
shell 1:  bash scratchpad/validation/v6d_launch.sh
shell 2:  "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File scratchpad/validation/v6d_gates.ps1
```

`v6d_launch.sh` is `v6c_launch.sh` plus **`--pre-order-settle 150`**. That is the only change,
and it is the enabling one: it holds PushOrder back for 150 s after the init dispatches, which
turns the few seconds that swallowed A5 into a window wide enough to fire in and measure.
Log: `v6d_runner.log`.

### The two arms - ONE variable between them: the order

| arm | appNo | fired at | command | scenario state |
|---|---|---|---|---|
| **A5 POPULATED-QUIET** | **4456** | the runner's `interface dispatched` line **+ 45 s**, and only while `Stage 8 - PushOrder` has NOT appeared | `SetSimRate.exe 1 4456 --settle-secs 15` | objects EXIST, **nothing tasked** |
| **A6 TASKED** | **4464** | **window + 120 s** (`Stage 8b` + `PushOrder: EXIT=0`) | `SetSimRate.exe 1 4464 --settle-secs 15` | objects exist **and are tasked** |

Same tool, same 15 s cap, same environment, same cwd, ~4 minutes apart. The only thing that
changes between them is whether the order has been pushed. A6 deliberately reuses the 15 s cap
rather than A1's 180 s: V6c already proved 180 s buys nothing, and a 15 s arm keeps A5 and A6
strictly comparable.

### Predictions - written before the run

| outcome | reading | what follows |
|---|---|---|
| **A5 HIT + A6 MISS** | **TASKING is the trigger** | the back end goes silent when it accepts the `move-along` tasks. Next: the object console at notify level 4 on a taskee, to see whether it is refusing, busy or stopped. |
| **A5 MISS** | **CREATION is the trigger** | the back end goes silent once the app's controller has created objects. Next: a run where the app joins and creates NOTHING while a second federate creates the units - which separates "objects exist" from "this controller created them". |
| **A5 HIT + A6 HIT** | neither is the trigger | **STOP AND ASK.** It would mean V6c's A1/A2 misses came from something the arms do not model, and a seventh probe is not the answer. |
| A5 NOT RUN again | the window still closed too fast | raise `--pre-order-settle` and rerun; do NOT reinterpret A6 alone. |

**Also read, every time:** the trace's `backends=` column. The A5/A6 verdicts are the tools'
own; the column says WHEN the back end went quiet for a federate that already had it, which is
the measurement that made V6c worth running.

### A4, the busy control - what it means NOW

`v6c_busy_launch.sh` + `v6c_busy_gates.ps1` (appNo **4448**) are already written and unchanged.
After V6c, A4's two outcomes mean:

* **A4 HIT** (V5's fixture, populated AND tasked, late tool finds the back end) -> H5 is NOT
  about population or tasking as such, and the difference is in V5's FIXTURE: the AO20 nav data
  with the custom SMS, the relocated appData, `AtOrder` creation, 128 units, DurationScale 0.25.
  The next question becomes which of those keeps the back end alive - and the honest reading is
  that the QUIET fixture's back end is the broken one, not V5's.
* **A4 MISS** -> V5's 0.3 s success is the anomaly, not the rule. Everything since V5 has been
  measured against a run that may itself have been unusual; re-examine what V5's `PauseSim`
  actually proved before treating it as the baseline.

Run A4 AFTER V6d: V6d costs one run and answers a two-way question, while A4 costs a run on the
heavier fixture and only matters once creation and tasking are separated.

---

## AMENDMENT 3 (2026-09-15, after V6d): A4's PREDICTIONS, REGISTERED BEFORE IT RUNS

**Nothing here has been run.** V6d answered its two-way question - **TASKING is the trigger**
(A5 populated-untasked HIT in 0.1 s, A6 populated-and-tasked MISS at 15 s; full harvest in
`V6_LIVE_JOIN_GATE_2026-09-15.md` sec 9). A4 is now the discriminator between the two readings
of WHY, and its predictions go on record first.

### What A4 is

```
shell 1:  bash scratchpad/validation/v6c_busy_launch.sh        # V5's fixture, unchanged
shell 2:  "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File scratchpad/validation/v6c_busy_gates.ps1
```

`SetSimRate 1 4448 --settle-secs 180` at **window + 180 s**, on V5's fixture
(`R9_Mojave_Empty_52_NavAO20_AG_S2` + COA-STP1 init + the `PROBE_RIDGE_1-35` order + the
nav-area gate + `AtOrder` + DurationScale 0.25). Both files are already written and unchanged.

### Why A4 is now the RIGHT next arm, and what makes it sharp

V6d did not merely answer its question - it produced the back end's own account (sec 9.3). At
the TASKSTRT instant every member walked the movement behaviour tree to:

```
Is current point in nav area?  -> Condition FALSE.
fail in action Is current point in nav area?
Starting sequence node Plan off feature path
Starting job node Plan path
Checking status of job for M1A2 10        <- the last line the back end ever emitted
```

The quiet fixture `R9_Mojave_Empty_52` is the PLAIN variant - no nav data at all -
so that condition is false for every unit. V5's fixture carries the AO20 nav area. That is the
sharpest difference between them, and A4 tests it directly. The full comparison:

| | V6c / V6d (quiet fixture) | V5 (busy fixture) |
|---|---|---|
| taskees | `1222.MechPlt~PXY`, `114.MechCoy~PXY`, `1.BdeHQ~PXY` - **three `~PXY` PROXY objects** (platoon, company, brigade HQ) | `1-35`, a **composed battalion with real members** |
| task | `move-along` x3 | `move-along` on a de-stacked unit |
| creation | `AtInit` | `AtOrder` |
| terrain / nav | plain `R9_Mojave_Empty_52`, no nav data | AO20 nav area + the custom SMS (`useAbstractGraphs=true`) |
| outcome | back end silent; **nothing ever moved**; 0 terminal reports | back end answered a late joiner in 0.3 s |

And the offsets match, which removes timing as an explanation: V5's `PauseSim` hit at
**window + 120 s**; V6d's A6 missed at **window + 120 s**. Same offset, opposite result.

### PREDICTIONS

| outcome | reading | what follows |
|---|---|---|
| **A4 HIT** - **the PREDICTED outcome** | the nav-area account holds: V5's fixture HAS a nav area, so `Is current point in nav area?` is TRUE, the members plan through the mesh instead of falling into `Plan off feature path`, and the engine keeps running. The trigger is not tasking as such but **tasking a ground move onto terrain with no nav area**. | Then the answer is a product rule, not another probe: never dispatch a ground move on a fixture without nav data, and make the interface REFUSE or warn instead of dispatching happily and then driving a stopped back end. Confirm by checking that A4's `backends=` column never drops. |
| **A4 MISS** | the nav-area account is WRONG or incomplete: tasking stops the back end even where a nav area exists, and **V5's 0.3 s success becomes the anomaly** - not the baseline everything since has been measured against. | Re-read V5 FIRST: its `PauseSim` fired at window+120 s, and 121 s is exactly how long a cached entry survives - V5 may have caught the back end one sample before its own drop. Then the question is what accepting a `move-along` does to the frame loop on ANY fixture, and the instruments are the level-4 console plus a CPU/thread sample of `vrfSimHLA1516e`. |

**Either way, read the `backends=` column, not just the tool's verdict.** A4's tool gives one
joiner's yes/no at one instant; the column gives the whole run, and in V6c it showed a federate
that ALREADY had the back end losing it 121 s after dispatch. If A4 HITs at +180 s, check
whether the column ever dropped at all - a back end that goes quiet and comes back is a third
behaviour neither hypothesis predicts.

### One measurement A4 should carry that V6c/V6d could not

`--sample-threads` was passed on every V6 run and **no thread/CPU artifact reached the run
directory** - so the one reading that would separate "busy loop" from "idle and silent" has
never been taken. Before A4, confirm the sampler actually writes a file; if it does not, take a
manual `Get-Process vrfSimHLA1516e | Select CPU,Threads` sample a few seconds before and after
the arm. A back end pegged at 100% is a hang; one at idle is a deliberate silence.

---

## AMENDMENT 4 (2026-09-15, after A4): PREREG V6e - IS IT THE NAV DATA, OR THE SMS?

**Nothing here has been run.** A4 HIT at 0.3 s exactly as AMENDMENT 3 predicted, and its
`backends=` column never dropped in 347 samples (`V6_LIVE_JOIN_GATE_2026-09-15.md` sec 10). The
nav-area account survives - but A4 changed **three** variables at once, not one.

### The confound, stated plainly

| | V6d (back end STOPS) | A4 (back end runs) |
|---|---|---|
| fixture | `R9_Mojave_Empty_52` | `R9_Mojave_Empty_52_NavAO20_AG_S2` |
| nav data | **none** | **MojaveAO20 area** |
| SMS | vendor `EntityLevel.sms` | **custom** `C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs_Slope2.sms` |
| init | R9 lean, 6 units, `AtInit` | COA-STP1, 128 units, `AtOrder` |

Any of the three could be what keeps the engine alive. The nav-area reading is the only one with
a mechanism in the back end's own console output - `Is current point in nav area?` -> FALSE is
literally the branch it fails on - but a custom SMS that replaces the movement model could be
avoiding the same dead end by another route, and a different init could be doing it by accident.

### V6e - the one-run discriminator

```
shell 1:  bash scratchpad/validation/v6e_launch.sh
shell 2:  "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File scratchpad/validation/v6e_gates.ps1
```

**V6e is V6d with nav data added and NOTHING else changed.** The deployed fixture
`R9_Mojave_Empty_52_Nav` is exactly that, and it was checked rather than assumed - its `.scn`,
read out of the `.scnx`, gives:

```
(Terrain-Database "...\tools\navdata\out\MAK Earth (online) + MojaveAO20.mtf")   <- nav data
(Simulation-Model-Set-Files "$(DATA_DIR)\simulationModelSets\EntityLevel.sms")   <- VENDOR SMS
```

versus A4's fixture, which names the custom `C2SIM_EntityLevel_AbstractGraphs_Slope2.sms`, and
versus V6d's plain fixture, which names `$(SHARED_DATA_DIR)\...\MAK Earth (online).mtf` with no
nav data at all. Same terrain + nav area as A4; vendor movement model as V6d.

**And the area contains the units.** `NavArea-ground-platform MojaveAO20.navRuntimeConfig` gives
extents +/-10.0 km E-W and +/-9.98 km N-S about an ECEF offset that converts to **34.6082 N,
-116.7001 W** - i.e. lat 34.518..34.698, lon -116.809..-116.591. The R9 lean init's units sit at
34.650-34.654 N, -116.689 to -116.693 W (measured from the V6d trace): **inside**. This is the
check that would otherwise sink the run - a MISS for want of coverage would look exactly like a
MISS for want of a mechanism.

`--pre-order-gate nav-area` is passed because the nav data loads LAZILY (a 2.3 GB async stream
from placement; 237 s cold, 9-12 s warm), and consoles are at 4 so the same
`Is current point in nav area?` line is readable either way.

### The arm

| arm | appNo | fired at | command |
|---|---|---|---|
| **V6e late tool** | **4479** | window + 180 s | `SetSimRate.exe 1 4479 --settle-secs 15` |

15 s, not 180: V6c's A1 already proved patience buys nothing, and A4 answered in 0.3 s.

### PREDICTIONS

| outcome | reading | what follows |
|---|---|---|
| **HIT** (and `backends=` never drops) | **nav data is the variable; the SMS and the init size are EXONERATED.** The quiet fixture's back end stops because there is no nav area, full stop. | Close the lane. STP-823 becomes a hard precondition and STP-822 an interface refusal + liveness timer. No further probe. |
| **MISS** (back end gone, as V6d) | nav data alone is NOT enough: the **custom SMS or the init/creation policy** is doing the work in A4. | Next is a 2x2 on one axis at a time - A4's fixture with the R9 lean init, and V6e's fixture with the custom SMS. Do NOT change two things again. |
| HIT but the column DROPS and recovers | a third behaviour neither hypothesis predicts | STOP AND ASK. |

**High-confidence prediction, and its STOP:** V6e HITs. If it MISSES, the console's own
`Is current point in nav area?` evidence is not the whole story and the lane's cause statement
must be reopened rather than patched - stop and ask.

### Carry the process sampler

A4 measured a RUNNING back end (~312% of one core, 83 threads, 4 GB). No quiet run was ever
sampled, so nobody knows whether a stopped one spins or idles. Run
`scratchpad/validation/a4_simsampler.csv`'s sampler again for V6e; if V6e MISSES it finally
answers hang-vs-idle, and if it HITs it costs nothing.

---

## AMENDMENT 5 (2026-09-15, after V6e): PREREG V6f - WHICH R5 TASK STOPS THE BACK END?

**Nothing here has been run.**

### V6e missed, and the STOP was honoured

AMENDMENT 4 registered HIT as the high-confidence prediction and made a miss a STOP rather than
a patch. V6e MISSED: the same R5 order on the SAME MojaveAO20 area with the VENDOR SMS stopped
the back end exactly as V6d did. The nav-area cause statement is WITHDRAWN - and the console
shows why it was never the mechanism:

```
V6d (no nav area):  Is current point in nav area? -> FALSE -> fail in action
                    -> Plan off feature path -> Starting job node Plan path
                    -> Checking status of job for M1A2 10          <- last line ever
V6e (nav area):     Is current point in nav area? -> TRUE ... CreateOffRoadSegment: success
                    -> Starting job node Calc off road nav path part
                    -> Checking status of job for M1A2 2           <- last line ever
```

Different branch, different job, **identical stopping point**. And the stopped state is now
measured, not assumed: `runs/launch52/RunScenario-<stamp>.threads.csv` shows the working set
running away at **~2.2 GB/min with well under one core of CPU and a flat thread count** on every
R5 run (V6d 3,117 -> 32,032 MB; V6e 2,922 -> 31,243 MB), while both ridge-order runs stay flat
(A4 4,019 -> 4,041 MB at 3.5-4.0 cores). Idle-hang and busy-loop are both dead; this is a loop
that allocates and blocks.

### The question V6f asks

The trigger is in the **R5 order**. Its three tasks are not equal loads - the console attributes:

| task | taskee | load |
|---|---|---|
| `T_R5_PL1` | `1222.MechPlt~PXY` | one platoon, `maneuver-in-formation` on offset routes |
| **`T_R5_CO1`** | `114.MechCoy~PXY` | **fans out to `1141`/`1142`/`1143.MechPlt`** - one task, three platoons |
| `T_R5_TK1` | `1.BdeHQ~PXY` | one entity, `base-system.movement.move-along` - and its taskee is the ONLY object in V6e that moved (~299 m) |

16 distinct members start building movement trees within half a second of dispatch; exactly one
reaches the path job. **Neither V5 nor A4 ever tasked a composed company** - their ridge order
moves a single entity-level proxy, and both ran flat for a full window.

### The run

```
shell 1:  bash scratchpad/validation/v6f_launch.sh
shell 2:  "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File scratchpad/validation/v6f_gates.ps1
```

**V6f is V6e with ONE thing changed: the order.** `data/PROBE_V6F_PLATOON_Order.xml` is R5 with
the 2nd and 3rd `<Task>` blocks removed - `T_R5_PL1` only, same OrderID, same route, same ROE,
same performing entity, generated from the original by structure and not by line number.
Fixture, init, client id, nav-area gate, `--pre-order-settle 150`, consoles at 4 and the 720 s
window are V6e's, unchanged.

**Why remove the suspect rather than keep it:** a HIT then means the company fan-out was
necessary, which is a positive result about the thing we care about; and if it MISSES we have
reproduced the fault with the smallest possible order, which is the better starting point for
everything after.

| arm | appNo | fired at | command |
|---|---|---|---|
| **V6f late tool** | **4487** | window + 180 s | `SetSimRate.exe 1 4487 --settle-secs 15` |

### PREDICTIONS

| outcome | reading | what follows |
|---|---|---|
| **HIT**, and `backends=` never drops, and `wsMB` stays FLAT | **the company fan-out is the trigger** | **V6g**: the company task ALONE, to confirm from the other side. Then the product rule is about tasking composed companies, and STP-823's title is wrong as written. |
| **HIT but `wsMB` climbs** | not a clean hit - the same fault, slower | treat as a MISS for ranking; the fan-out only changes the rate. |
| **MISS with the same ~2 GB/min slope** | one platoon move is enough; the fan-out is NOT necessary | the next variable is the **init/composition** (R9 lean + `ComposeHierarchy` + `AtInit`), which every failing run shares and no healthy run used. One axis at a time. |
| **MISS with NO runaway** | a different failure mode | **STOP AND ASK.** |

**High-confidence prediction:** V6f HITs. Its miss is not a STOP this time - a MISS is a
legitimate and informative outcome that promotes candidate 2 - but a MISS *without* the working
set slope is a STOP, because it would mean two different faults wearing the same symptom.

### Standing instructions for this run

* Carry the working-set sampler and **watch it live**: at ~2.2 GB/min a 32 GB machine is ~30
  minutes from exhaustion, and every R5 run so far was saved only by the 720 s window. The seat
  is adding a runner tripwire on ws slope; until it lands, abort by hand if wsMB passes ~20 GB.
* One fresh ledgered appNumber, never recycled; a joined tool is never killed; rtiexec /
  rtiForwarder / rtiAssistant are never touched.
* Read the `backends=` column and the last `Starting job node ...` / `Checking status of job`
  pair before writing any verdict - the tool's yes/no is one instant, those are the run.
