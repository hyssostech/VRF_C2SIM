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
