# Cold-start review, pass 3, of feat/tasking-rulings @ 8db033e (Opus, 2026-09-14 ~20:10-20:50Z; verbatim, ASCII/CRLF as
# delivered). Supervisor notes: verdict FIX FIRST (D1 unguarded terrain-profile continuation on the default ground-move
# dispatch path; D2 RUNBOOK sec 11 stale on Q5) then merge; A1 CLOSED on the reviewer's own parse of the order (42/42);
# E2 is the documented BackendCount limit (STP-809); E3 (cyclic STREND = malformed) adopted as an extension of Q4 by the
# supervisor; fix pass dispatched 21:15Z (BRIEF_fixpass3). The first TaskOutput copy of this text arrived truncated and
# the task transcript was empty - the reviewer re-wrote it to a file on request (lessons-executor-reports-to-file).

# COLD-START REVIEW, PASS 3 - feat/tasking-rulings @ 8db033e

Reviewer: Opus cold-start, read-only. Tier HEAVY. Date 2026-09-14.
Worktree reviewed: <repo>\.claude\worktrees\rulings (NOT modified).
Diff base for this pass: 0c96f50 (pass 2's subject).
Inputs: the pass-2 review (read in full) and the eleven fix commits b6471a3, 4ca7afa, 821b359,
168207f, df7f7e2, f2794d7, d792901, ba44d7e, 3c78533, b76c9c7, 8db033e.
Rulings held against the code: Q1-Q7 (user, 2026-09-14).

## VERDICT: **FIX FIRST** - two items, both cheap, both on the demo path

**A1 IS CLOSED.** I re-derived COA-STP1's outcome independently - my own XML parse of the order
plus a hand re-implementation of the three window formulas read off `TaskDispatchPolicy.cs` - and
got **42 dispatches / 0 skips** at `DurationScale` 1.0 AND 0.05, at the shipped 600 s floor and at
the Demo overlay's 7,200. B1, B4, B6, B7 and the Q1/Q4/Q5/Q6/Q7 rulings are all implemented as
ruled. The suite is 112 checks with eight named FAIL-FIRST controls, and they exercise the
production classes.

The two things that must be fixed first are not in that work. They are:

- **D1** the A1 follow-up (`b76c9c7`) guards the wrong dispatch path: `GroundWaypointAltitudeMode`
  defaults to `"TerrainProfile"`, so the real dispatch of every ground move happens on an
  **unguarded** re-entry, where a throw now costs the chain **86,400 s** instead of 4,860 s - the
  exact trade the commit says the backstop must not make.
- **D2** `RUNBOOK.md` sec 11 still states the **pre-Q5** fallback rule and says the TASK CLOCK
  lines are said "once". Since `f2794d7` a flat clock HOLDS and the line REPEATS. Sec 11 has no Q5
  bullet at all, so the demo operator is not told what a pause does, nor what the one symptom of a
  dead back end looks like. Same class as pass-2's A2.

---

## 0. WHAT I RAN

- `dotnet build src\VrfC2SimApp -c Release -p:BridgeConfig=Release-5.2` - **Build succeeded, 0
  errors.** NOTE: the plain incremental build reports "All projects are up-to-date" and **0
  warnings**; that is a no-op, not a clean compile. A forced `-t:Rebuild` gives the real number:
  **6 warnings** - 4 CA2024 in the C2SIM SDK and 2 CS8632 at `VrfC2SimService.cs:70` and
  `:2925`, both pre-existing (pass 2 blamed them to 88488be2 / 2026-07-13).
- **All 18 offline suites, `C:\MAK\vrforces5.2d\bin64` first on PATH. All 18 exit 0:**
  translator SELF-TEST PASSED; report / sequencer / verb / destack / fanout / compose / arrival /
  stall / parse / name / preflight / rulings ALL CHECKS PASSED; typemap SELF-TEST PASSED (783
  checks); terrain PASS; placement PASSED; scripted-task and initgraphics ALL CHECKS PASSED.
- `--rulings-selftest`: **112 `[PASS]` lines across 6 sections, 0 failures** (the brief's count is
  right). Eight of them are explicit FAIL-FIRST controls: M1, M3, M4, Q4 x2, A1 x3 (the 4-deep
  chain, the delayed root, and the whole 42-task graph at 21/21), B1 (vi), B4 (vii).
- **An independent census of the order graph** (my own `xml.etree` parse, not `--parse-order` and
  not the suite): 42 tasks; 11 STREND roots; depth histogram d0 11 / d1 11 / d2 10 / d3 10;
  durations 32 x PT1H20M + 10 x PT2H; one `DelayTimeAmount` of PT3H20M (12,000 s) on the T13 root.
  Shape: **ten chains of PT2H + three x PT1H20M, plus ONE two-deep chain** (T13 delayed 12,000 s,
  T14 behind it). Longest chain LEAD = **16,800 s**; the last task ENDS at 21,600 s.
- **An independent re-derivation of the gate outcome** from the code's own formulas
  (`PredecessorEndSeconds` / `PredecessorTimeoutSeconds` / `PredecessorDispatchTimeoutSeconds`,
  transcribed from `TaskDispatchPolicy.cs:236-290`) over that graph: **42/42 dispatch at scale 1.0
  and at 0.05**, no phase-1 and no phase-2 failure on any edge. This agrees with the suite but does
  not come from it.
- Vendor headers read directly: `C:\MAK\vrforces5.2d\include\vrfcontrol\vrfBackendListener.h` and
  `vrfRemoteController.h`; and the project's own `src/VrfFacade/VrfFacade.cpp`.
- ASCII + CRLF audit of all 12 changed files, in python (not `grep -P`): **0 offenders outside
  TAB/CR/LF/0x20-0x7E, 100% CRLF on every file**; the dirty control returns 19. The instrument
  works.
- `git merge-base --is-ancestor 26efe0c 8db033e` -> **true**.
- The in-repo copy of the pass-2 review is the original text byte-for-byte plus an 11-line
  supervisor header that says so. Faithful record.

---

## 1. FINDINGS

Severity: MAJOR = wrong behaviour on the demo path, or an operator instruction that is false.
MINOR = wrong in a reachable but narrower case, or an accuracy defect. NOTE = record only.

### MAJOR

| # | file:line | input -> wrong outcome | fix |
|---|-----------|------------------------|-----|
| **D1** | `VrfC2SimService.cs:2742-2773` (the deferral) + `:4671` and `:4683` (the bare enqueues) + `:715-719` (the drain's generic catch) + `VrfSettings.cs:601` (the default) | **`b76c9c7` guards the dispatch path the demo does NOT take.** `Vrf:GroundWaypointAltitudeMode` defaults to **`"TerrainProfile"`**, and neither settings file overrides it. For every GROUND unit with route points - most of COA-STP1 - the guarded first call to `ExecuteTaskOnTick` runs as far as `:2748`, sends the terrain-profile request and **returns at `:2773` having marked NOTHING** (`:2740-2741` says so). The real dispatch is the re-entry `ExecuteTaskOnTick(task, unit, r.Vertices)` at `:2768`, which runs inside `pending.Continue(...)` - enqueued **bare** at `:4671` (reply) and `:4683` (timeout). A throw there is caught only by the drain at `:717-718`, which logs `Tick action failed:` and tells the sequencer nothing: **no TASKSTRT, no TASKABRT, no `NotifyAbandoned`**. The task is silent to STP forever and its successors now wait `Vrf:TaskChainBackstopSeconds` = 86,400 task-clock seconds - at the measured 0.27x-0.73x sim ratios, **32 to 89 wall hours** - where before A1 they waited 4,860 s. The re-entry executes strictly MORE code than the guarded first pass (route creation, the bridge move calls, `MarkDispatched`, the fan-out), and the sibling init-placement enqueue at `:2046-2059` already carries its own guard, added by an earlier cold-start review for exactly this reason. | Wrap both `pending.Continue(...)` enqueues in the same try/catch `b76c9c7` added at `:2402-2414` - or, better, put the guard inside `PendingTerrain.Continue` so every future caller inherits it. The handler has the task uuid in the closure, so it can `NotifyAbandoned` + TASKABRT identically. Add a self-test that drives a throwing continuation. |
| **D2** | `docs/RUNBOOK.md:1429-1433`; same sentence at `VrfC2SimService.cs:524-525` | **Sec 11 states the rule Q5 REPLACED.** It reads: "The sim clock falls back to WALL seconds on its own, whenever `DtVrfRemoteController::simTime()` cannot be read for three consecutive samples **or has been flat for 60 wall seconds**" and "The `TASK CLOCK:` lines say each way, **once**." Since `f2794d7` a clock flat for 60 s with a back end present **HOLDS** (`StallPolicy.TaskClockAction`, `VrfC2SimService.cs:3716-3741`) and the hold line **repeats every 60 s by design** (`:3727-3728`). Sec 11 carries **no Q5 bullet at all**: a demo operator is not told that pausing the scenario freezes every armed Duration, that the repeating WARNING is the intended signal, or that a back end which dies without being removed from `backends()` freezes task time permanently (E2). Pass 2 rated the same defect - sec 11 telling the operator the inverse of the code - MAJOR (A2), and this is the section a standalone demo deployment points its operator at. The identical stale clause is in the start-up "proof" line the RUNBOOK cites at `:1460`. | Replace the fallback bullet with the Q5 rule (hold while a back end is present; wall only when the reader is gone), state that the hold line REPEATS and why, state the BackendCount limit and the symptom ("if this repeats and nobody paused anything, the back end has died and task time is frozen"), and fix the `"or has gone stale"` clause at `VrfC2SimService.cs:525`. |

### MINOR

| # | file:line | input -> wrong outcome | fix |
|---|-----------|------------------------|-----|
| E1 | `VrfC2SimService.cs:3755-3761` | **The "clock is back" line is printed when the clock has gone AWAY.** The branch fires on `_taskClockStaleWarned && !taskSimStale` and never checks `obs.Readable`. Reachable: after a HOLD (`_taskClockStaleWarned = true`), lose the sim reader for `ModeSwitchConfirmations` = 3 samples; `heldOnSim` goes false at `:3691`, so `taskSimStale` goes false at `:3704`, the early return at `:3696` no longer fires, and the interface logs **"the simulation clock is readable and advancing again"** at the exact tick it switches every task time to the WALL clock. `8db033e` fixed the once-only guard for the OTHER hold->wall transition (E2 shows that one is unreachable) and left this one. Partly mitigated: `MaybeCompleteTimedTasks:3804-3810` prints a correct mode line - but only while `_timed.Count > 0` (`:3791`). | Branch on `obs.Readable`: readable -> "advancing again"; not readable -> "the sim reader is GONE; task times are now served on the WALL clock". |
| E2 | `VrfC2SimService.cs:3704-3716` + `:3742-3754` vs `VrfFacade.cpp:670-686` | **Q5's wall-fallback branch is dead code, and the executor's own competing hypothesis is CONFIRMED.** `VrfFacade::SimTimeSeconds` returns -1.0 exactly when `p_->controller->backends().count() <= 0` (`VrfFacade.cpp:681`), and `VrfFacade::BackendCount` returns `p_->controller->backends().count()` (`:671`) - literally the same expression on the same object. `SampleTaskClock` returns early at `:3696` unless the sample was readable, so by the time `BackendCount()` is read at `:3708` the preceding `SimTimeSeconds()` has already proved `count() > 0`. `taskSimStale && !backEndPresent` is therefore reachable only by a race between two adjacent calls or by a bridge throw, and the `else if` at `:3742-3754` ("NO VR-Forces back end is present ... the simulation is GONE") never prints. Vendor confirmation of the underlying hazard: `vrfBackendListener.h:161-163` - `doTimeouts()` "deactivates any status objects which have not responded within the timeout interval"; `:153-155` - `remove()` is the only thing that takes one out and "normally should not need to get called"; `:89-90` and `vrfRemoteController.h:298-299` - `backends()` is "all KNOWN back ends". **Rating for the demo: a back end that dies without being removed freezes task time permanently** - no timed completion, no phase-1 or phase-2 gate expiry (both ride the frozen axis), no start delay, and no stall verdict either (the watchdog suspends on `Stale`) - with one WARNING a minute as the only output. The ruling is still honoured in OUTCOME for a back end that is genuinely gone, because the reader then goes unreadable and the axis falls to wall through E1's path. | Nothing to change in C# for the ruling itself. (a) Correct assessment live gate 11, which predicts a line that cannot print. (b) Take the OWED facade change the code's own note names (`StallPolicy.cs:218-220`): `DtVrfRemoteController::backendsControlState()` (`vrfRemoteController.h:321-323`) returns `DtPauseControlType` vs `DtRunControlType` - one line in `VrfFacade`, and it is the discriminator the predicate actually wants. |
| E3 | `VrfC2SimService.cs:2284-2298`; `TaskDispatchPolicy.cs:281-290` | **No cycle or self-reference guard, and A1 made a cycle 144x more expensive.** A `StartAfterTaskUuid` naming its own task, or two tasks naming each other, makes `predecessorInThisOrder` TRUE, so phase 1 gets the 86,400 s backstop - and a cycle is the one dead end that nothing ever `NotifyAbandoned`s. Both tasks hang a full sim day and are then TASKABRT'd with "never dispatched within 86400s of order receipt". Before A1 the same order failed in 600 s. The whole graph is already in `_taskByUuid` at `:2206-2207`, and the suite already carries a cycle-guarded walk (`RulingsSelfTest.cs:1323-1343`). Consistency: Q4 refuses a malformed task loudly and at once; a cyclic STREND reference is equally malformed and is instead held silently for a sim day. | At order receipt, walk the predecessor graph once; a cycle (or a self-reference) is a MALFORMED order - ERROR + TASKABRT + `NotifyAbandoned` for every task on it, exactly as Q4 does. |
| E4 | `VrfC2SimService.cs:2209-2249` | **Nothing compares the order's own longest chain lead against the backstop.** COA-STP1 is safe (16,800 s measured, 5.1x under 86,400) - but a deeper chain, or `Vrf:DurationScale > 1`, is silently truncated at the backstop and the operator learns it as a burst of "never dispatched within 86400s" lines hours later. The graph is in hand at `:2206`. | One INFO line at order receipt: longest chain lead L s vs `TaskChainBackstopSeconds` B s; WARNING when `L >= B`. |
| E5 | `TaskDispatchPolicy.cs:209-218`; `RulingsSelfTest.cs` check "(a negative or non-finite margin is treated as zero - never as a reason to expire early)" | **`TaskPredecessorEndMarginSeconds = 0` breaks M1's relation and nothing warns.** At margin 0 the phase-2 window equals the predecessor's scaled Duration EXACTLY, while the completion is observed up to ~3 x (sim ratio) seconds late - the 1 s `TaskClockSampleSeconds` staircase plus the 1 s `TimedCheckSeconds` walk (pass-2's C3). Gate and completion then race, which is the non-determinism A1 was fixed to remove. The self-test's wording ("never as a reason to expire early") is true for a NEGATIVE margin and false for 0. | Validate `TaskPredecessorEndMarginSeconds > 0` at start-up (it already has a validation slot beside `DurationScale` at `:486-500`), or state the `margin > 3 x ratio` relation where `Vrf:TaskClock` is documented. |
| E6 | `TaskDispatchPolicy.cs:241-243` | **The number the backstop is justified by is wrong.** "COA-STP1's deepest is 26,400 s". Measured from the order: the deepest chain LEAD is **16,800 s** and the last task ENDS at 21,600 s. 26,400 would be right only if T13's delayed chain were four deep; it is two deep (12,000 s delay + PT1H20M, then T14). Harmless in direction - the backstop is still 5.1x - but it is the load-bearing figure in the justification. | Correct to 16,800 s (dispatch) / 21,600 s (end). |
| E7 | `docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md` sec 7.1a | Says "(90 checks across SIX sections)". 7.1b says 112 and the suite prints 112. Two statements about the same number in one document. | Make 7.1a say 112. |

### NOTE

| # | where | note |
|---|-------|------|
| N1 | `TaskDispatchPolicy.cs:167-194` vs `:195`, `:209-211` | **Pass-2's C1 is NOT fixed** (it is listed as recorded debt, correctly). Lines 167-194 are one contiguous `///` block, so M1's summary and its `<param name="configuredSeconds">` / `<param name="predecessorEndSeconds">` still bind to `ScaleOrderMs` at `:195`, while `PredecessorTimeoutSeconds` at `:211` carries only the orphan `<param name="marginSeconds">`. Invisible today (no `/doc`), but this repo treats the header as the contract. |
| N2 | `VrfC2SimService.cs:148-153`, `:2286`, `:2307` | `_taskByUuid` is never pruned and spans ORDERS, so `predecessorInThisOrder` - and its log line "IS a task in this order" - really mean "in any order this process has seen". I could construct no unsafe case (a prior order's task is dispatched, completed or abandoned, and all three satisfy the gate), but the words are wrong and a cross-order reference gets the 86,400 s backstop rather than the configured window. |
| N3 | `VrfC2SimService.cs:2334-2337` vs `TaskSequencer.cs:202-203`; `RulingsSelfTest.cs:1110-1111` | The start-delay LOG reports `Math.Max(scaledStartMs, scaledRelativeMs)` while the sequencer uses sim-first-else-relative. Differs only for an order carrying BOTH forms (COA-STP1 does not). The suite's graph builder copies the log's `Math.Max`, not the sequencer's rule - so if such an order ever appears, the offline walk and the live gate would disagree. |
| N4 | `RulingsSelfTest.cs:392-396` | The check "a frozen sim clock with NO back end ... completes on the WALL fallback" drives a READABLE frozen reader with `backEndPresent: false` - a state E2 shows the facade cannot produce. Sound as a unit test of the policy; misleading as evidence that Q5's "gone" half is exercised. |
| N5 | `RulingsSelfTest.cs:499-528` (`WalkTaskClock`), `:1192-1233` (`WalkChain`) | Pass-2's C8 stands: both re-implement the service glue. I checked the reproduction argument by argument against `SampleTaskClock:3691-3766` and `RunTaskAsync:2285-2341` and it is faithful **as of 8db033e** - same predicates, same parameter positions - but a future drift in the service would not fail a check. Specifically uncovered: the ORDER of the Q4 refusal relative to `MarkDispatched`. I verified that by reading (`:2650` precedes `:2672`), not by a test. |
| N6 | `VrfC2SimService.cs:3201-3209` | `IssueEngage` enqueues a bare lambda too. A throw leaves the parked engage unissued and unreported - but the task was already `MarkDispatched`'d as the move, so its armed end time still closes it. Same family as D1, far lower stakes. |
| N7 | - | B2 (a superseded move's late vendor completion attributed to the NEW task), B5 (an absolute `StartTime` served as a scaled wall delta on the sim axis) and C2-C15 are unchanged and correctly carried as recorded debt in 7.1b. |
| N8 | - | ASCII/CRLF clean: 0 offenders across all 12 changed files, 100% CRLF; dirty control returns 19. |
| N9 | build | Use `-t:Rebuild`. The incremental build says "up-to-date" and "0 Warning(s)", which is a no-op result, not a clean compile. Real: 6 warnings, all pre-existing. |

---

## 2. ANSWERS TO THE BRIEF'S SEVEN QUESTIONS

### 2.1 A1 closure

**CLOSED.** Re-derived from the code and from my own parse of the order, not from the executor's
report or the suite.

*The graph (independently parsed).* 42 tasks; 11 STREND roots; depth 11 / 11 / 10 / 10. **Ten
chains** are a PT2H root + three PT1H20M successors; **one chain** is T13 (PT1H20M, delayed
PT3H20M = 12,000 s) -> T14 (PT1H20M). No dangling references, no cycles.

*The windows, from `TaskDispatchPolicy.cs`.*
`predEnd = PredecessorEndSeconds(TimedCompletion, inOrder, predDurationMs, scale)`;
`phase2 = PredecessorTimeoutSeconds(configured, predEnd, margin) = max(max(1,cfg), predEnd+margin)`;
`phase1 = PredecessorDispatchTimeoutSeconds(inOrder, phase2, backstop) = inOrder ? max(phase2,
backstop) : phase2`.

| profile | phase-1 window (in-order) | dispatch times | outcome |
|---|---|---|---|
| defaults 600 / 60 / 1.0 / 86400 | 86,400 s | 0 / 7,200 / 12,000 / 16,800; T13 12,000, T14 16,800 | **42 / 0** |
| Demo overlay 7200 / 60 / 1.0 | 86,400 s | identical (the floor only raises phase 2) | **42 / 0** |
| compressed 600 / 60 / 0.05 | 86,400 s | 0 / 360 / 600 / 840; T13 600, T14 840 | **42 / 0**, and DETERMINISTIC - pass-2's 6-in-7 boundary race is gone because phase 1 no longer has the 600 s bound |

Longest lead **16,800 s** vs the 86,400 s backstop: 5.1x headroom.

*Any remaining path where a successor is skipped before its predecessor's timed completion?*
Phase 2 expires at `pred.DispatchedAtClock + phase2` and the timed completion fires at
`pred.DispatchedAtClock + scaledDuration`, on the SAME axis, so the margin is what covers the
staircase. Three residual ways, all named:
1. `margin = 0` (E5) - the two coincide and race.
2. sim ratio above ~20 - the <= 3 x ratio observation lag eats the 60 s margin (pass-2's C3, still
   unstated in `VrfSettings`).
3. a predecessor with NO Duration - `predEnd = 0`, the window is the configured floor, and the
   successor is skipped at 600 s if arrival evidence takes longer. Correct by configuration, and
   not reachable on COA-STP1 (all 42 carry a Duration).
With `Vrf:TimedCompletion = false` nothing completes at all and every successor times out at the
configured floor - which is B4's intended behaviour, now verified by the (vii) control.

*The 86,400 s backstop - what happens at it?* Phase 1 returns `PredecessorNeverDispatched`
(`TaskSequencer.cs:175`); `GateFailureReason` produces "never dispatched within 86400s of order
receipt"; `policy=skip` -> `NotifyAbandoned` + one TASKABRT ("SKIPPED: predecessor X never
dispatched ...", `:2361-2366`), which cascades down the chain. So: **skip + abort + a WARNING per
task.** Acceptable for a multi-day scenario? **Only by luck.** The backstop is measured in
task-clock (i.e. SIM by default) seconds from order receipt, so an order whose longest chain lead
exceeds 24 sim hours is truncated silently. Nothing checks - see E4.

*Dangling predecessor.* `predecessorInThisOrder` false -> `predEnd = 0` -> phase 2 = phase 1 =
`max(1, configured)` = 600 s, and the (iii) check measures the skip landing at exactly 600 s.
Correct and as ruled.

*The tick-thread throw (b76c9c7).* On the path it covers (`:2402-2414`) it abandons exactly once
(`NotifyAbandoned` is `TrySetResult`, idempotent) and emits exactly one TASKABRT
(`TaskStatusPolicy.ShouldEmitAbort` is one-per-task and blocked after TASKCMPLT). **But it does not
cover the default ground-move path - D1.**

### 2.2 Q4 - the malformed refusal

- **Decided BEFORE dispatch.** `IsMalformedZeroGeometryTask` is tested at `VrfC2SimService.cs:2650`
  and the branch returns at `:2662`; `MarkDispatched` - the ONE place TASKSTRT is pushed
  (`:3106`) - is at `:2672`, after it. **A refused task never reports TASKSTRT.** That is the right
  order for STP: a task that never started must not report started.
- **Successors through the same path.** `_sequencer.NotifyAbandoned(task.TaskUuid)` at `:2659` -
  the same call every other dead end makes. Each successor's gate returns `PredecessorAbandoned`,
  and each pushes its OWN TASKABRT at `:2365`. It cascades transitively.
- **Geometry but no Duration is NOT refused - verified.** `IsMalformedZeroGeometryTask` requires
  `action == ExecuteInPlace`, which requires `taskPoints.Count == 0` (`:2614`). With geometry the
  block is never entered. `MarkDispatched:3120-3123` then logs the WARNING "no Duration, so this
  task has no end time - it completes only on its own evidence", which is exactly R4's silence, and
  arms nothing. Correct.
- **Duration but no geometry is NOT refused - verified.** `durationMs > 0` fails the test, so the
  task takes the `ExecuteInPlace` branch at `:2664`: `MarkDispatched("hold-in-place")`, TASKSTRT,
  the armed end time, the NameObservation, no vendor task. R2 as ruled.
- `Vrf:DefaultHoldSeconds` is **gone**: five textual references remain, all in comments and docs
  that explain the deletion; **zero** in `VrfSettings.cs` as a property and zero in either
  settings file.

### 2.3 Q5 - hold vs wall

*The exact code path.* `SampleTaskClock` (tick thread, once per wall second, `TickPhase` at `:732`)
-> `_bridge.SimTimeSeconds()` at `:3658` -> `SimClockTracker.Observe` -> `heldOnSim = preferSim &&
obs.ReadableConfirmed` (`:3691`) -> **early return at `:3696` when the sample is unreadable** ->
`taskSimStale = heldOnSim && obs.Stale` (`:3704`) -> **`_bridge.BackendCount()` read at `:3708`,
only in the stale branch, on the SAME tick thread** -> `StallPolicy.TaskClockAction` ->
`_taskAxis.Advance(usingSim ? obs.SimSeconds : wallNow, usingSim)` (`:3766`).

*All three task times hold together - verified.* `HoldOnSim` passes the flat `obs.SimSeconds` with
`usingSim: true`, and `TaskClockAxis.Advance:165-166` adds only `reading > _lastReading`, so it
adds **0** and does not re-anchor. Every consumer reads that one axis:
- timed completion: `MaybeCompleteTimedTasks:3799` `clockNow = TaskClockSeconds` ->
  `TimedCompletionPolicy.Advance:116-118` adds `clockNow - LastClock` only when positive -> 0;
- the phase-2 gate and phase 1: `WaitForStartAsync` is handed `_taskClockAxis` (`:2340`), whose
  `Now` is `TaskClockSeconds` and whose `DelayAsync` is `TaskClockDelayAsync:3775-3784`, a loop on
  `TaskClockSeconds - start < seconds`;
- the start delay: the same `clock.DelayAsync` at `TaskSequencer.cs:207`.
**All three freeze together. Confirmed by reading, and by the (Q5) 600-sample check.**

*What `BackendCount` returns for a deactivated back end - CONFIRMED FROM THE VENDOR HEADER.*
`vrfBackendListener.h:161-163`: `doTimeouts()` "Goes through the status object list and
**deactivates** any status objects which have not responded within the timeout interval."
`:153-155`: `remove()` - "Forcibly remove a backend from the list. Normally, this should not need
to get called!" `:89-90` and `vrfRemoteController.h:298-299`: `backends()` is the list of "all
**known**" back ends. **`count()` does NOT drop a dead back end.** The branch's own doc
(`StallPolicy.cs:208-220`) states this correctly and is the best-documented decision on the branch.

*The executor's competing hypothesis - CONFIRMED, and it goes further than stated.* `VrfFacade.cpp`
`SimTimeSeconds` (`:674-686`) and `BackendCount` (`:670-672`) read the SAME `backends().count()`.
Combined with the early return at `:3696`, `taskSimStale && !backEndPresent` is unreachable in
practice, so the `else if` at `:3742-3754` is dead code and `8db033e`'s hold->wall transition
logging covers a transition that cannot occur through it - **E2**. The ruling's outcome still
holds, because a genuinely-removed back end makes the reader unreadable and the axis falls to wall
through the hysteresis path instead - but that path prints the WRONG line (**E1**).

*Risk rating for the demo.* A back end that dies **without being removed** freezes task time
permanently: no completions, no gate expiry, no start delays, and no stall verdict either (the
watchdog suspends on `Stale`). The run hangs with one WARNING a minute. This is the accepted cost
of the user's ruling and the code says so - but it is not in the RUNBOOK (**D2**), and the cheap
real fix is already named by the code and present in the vendor header
(`backendsControlState()`, `vrfRemoteController.h:321-323`).

*Reverse transition (back end returns).* Correct. If the sim clock has moved, the first readable
sample is a CHANGE -> `Stale` false -> `ServeSim`; if it is at its old value, `flatFor` counts the
blind period (pass-2's C6) -> `Stale` -> `HoldOnSim`. Either way `usingSim` flips back to true, the
axis sees a mode change and **re-anchors without adding anything** (`Advance:165`), so nothing is
double-counted and no wait restarts.

*Is the HOLD line repeating?* Yes: `!_taskClockStaleWarned || (now - _taskClockHoldLineUtc) >=
StallPolicy.LogRateLimitSeconds` (`:3727-3728`, `LogRateLimitSeconds = 60.0` at
`StallPolicy.cs:181`) - **once per wall minute**, and the text carries the CAVEAT sentence naming
the dead-back-end case. Correct, and the right decision.

### 2.4 B1 / B4 / B6 / B7 and the settings

- **B1 + Q1.** `NotifyAbandoned(old.TaskUuid)` at `:3074` sits **only** in the `else` of
  `SupersedeAbandonsSuccessors` - i.e. only on the TASKABRT branch; the TASKCMPLT branch (`:3058`)
  only logs and keeps the timer armed, as ruled. Fires at most once (guarded by `old.TaskUuid !=
  task.TaskUuid` at `:3044`, and idempotent anyway). `PushTaskStatus` cancels the armed end time
  first (`:4776`), so one call does both. Verified.
- **B4.** `PredecessorEndSeconds(_vrf.TimedCompletion, ...)` at `:2292-2293`; with R4 off the
  derived window collapses to the configured floor. The (vii) FAIL-FIRST control measures the
  difference: 7,260 s before, 600 s after.
- **B6.** `appsettings.json:37-43` carries **all seven** keys at the exact defaults the brief lists:
  `TimedCompletion: true`, `DurationScale: 1.0`, `TaskClock: "sim"`,
  `TaskPredecessorTimeoutSeconds: 600`, `TaskPredecessorEndMarginSeconds: 60`,
  `TaskChainBackstopSeconds: 86400`, `SupersededTaskCode: "TASKABRT"`. They match
  `VrfSettings.cs:430/440/455/473/483/513/530` exactly. `appsettings.Demo.json:30-49` carries six
  with a `_Key` explanation each (`TaskPredecessorTimeoutSeconds: 7200` as the floor, which changes
  nothing: it only raises phase 2). **No `DefaultHoldSeconds` in either file.** Verified.
- **B7.** `GateResult.PredecessorNeverDispatched` is its own outcome (`TaskSequencer.cs:43`,
  returned at `:175`), and `GateFailureReason` (`TaskDispatchPolicy.cs:110-120`) is called with
  `(gate, dispatchTimeoutSeconds, timeoutSeconds)` in that order - matching the signature. The two
  sentences are locked verbatim by four checks, including one that asserts they can never be equal.

### 2.5 Double or missing terminal codes

CORRECTION 2026-09-25: this is a SUPERVISOR reading (2026-09-13), not an owner ruling - RL-20260914-01 covers only the TASKABRT code; see its scope note. The current TEMPORARY completion position is RL-20260921-09.

**`ReportBuilder.BuildTaskStatusReport` still has exactly ONE call site** -
`VrfC2SimService.cs:4794`, inside `PushTaskStatus`. Nothing bypasses it (grepped over all sources).
`TaskStatusPolicy` gives one TASKSTRT per execution, one TASKCMPLT per task, one TASKABRT per task
and never after a TASKCMPLT (`:56-94`).

| outcome | code(s) | verdict |
|---|---|---|
| timed | TASKCMPLT `:3832` + `CompleteTask` | one; `Advance` removes atomically before reporting |
| arrival / vendor success | TASKCMPLT `:4454` + `CompleteTask` `:4430` | one; timer cancelled first at `:4776` |
| vendor `success=false` | TASKABRT `:4454` + `NotifyAbandoned` `:4431` | one |
| supersede | TASKABRT `:3065` + `NotifyAbandoned` `:3074` | one, TASKABRT branch only |
| stall | TASKABRT `:4221` | one; a later arrival TASKCMPLT is deliberate (C16 ruling) |
| malformed refusal (Q4) | TASKABRT `:2660` + `NotifyAbandoned` `:2659`, **before** any TASKSTRT | one |
| tick-throw abandon | TASKABRT `:2410` + `NotifyAbandoned` `:2409` | one - **but not on the terrain re-entry, D1** |
| backstop / phase-1 expiry | TASKABRT `:2365` + `NotifyAbandoned` `:2361` | one |
| phase-2 expiry, orchestration catch, no-performing-unit, taskee-not-in-init, unreachable-Refuse | TASKABRT + `NotifyAbandoned` | one each |
| no `PerformingEntity` (`:2211-2215`) | `NotifyAbandoned` only | correct - there is no taskee uuid to address a report to; pre-existing |

**The one MISSING-code path this pass found is D1**: a throw inside the terrain-profile re-entry
produces no code at all, for a task that also never reported TASKSTRT.

### 2.6 Self-tests

**112 checks, six sections, 0 failures.** Not vacuous: eight FAIL-FIRST controls, and each drives
the PRODUCTION classes on the pre-fix rule -

- A1 (i): `WalkChain(..., preFixPhase1: true)` on a 4-deep chain -> 2 of 4, `T3 =
  PredecessorNeverDispatched`; fixed -> 0 / 7,200 / 12,000 / 16,800.
- A1 (ii): T13/T14 -> 1 of 2 pre-fix; 12,000 / 16,800 after.
- A1 (v): **the whole 42-task graph read off `data/COA-STP1_Order.xml`** -> 21/21 pre-fix, 42/0
  after, 42/0 under the Demo overlay, and 42/0 in **20 repetitions** at scale 0.05 (the
  determinism check pass 2 asked for).
- A1 (iv): an ABANDONED predecessor fails the gate at **0 s of clock**, not the backstop - which is
  what pays for dropping phase 1's timeout.
- Q4 x2: the pre-ruling code armed an invented 60 s end time AND its successor then dispatched on
  it; the new code abandons and the successor is `PredecessorAbandoned`.
- B1 (vi), B4 (vii), M3, M4.

All of these run the real `TaskSequencer`, `TimedCompletionPolicy`, `SimClockTracker`,
`TaskClockAxis` and `TaskDispatchPolicy` on a shared fake clock. Pass-2's specific complaint - "the
one integration test pre-dispatches the predecessor, so phase 1 is never exercised" - is fully
answered: `WalkChain` starts every gate at t = 0 with nothing dispatched, which is `HandleOrder`'s
own topology.

**Nothing asserts against a value the code under test computed.** `window` at `:1019` and
`derivedWindow`/`demoEnd` come from `TaskDispatchPolicy`, but every one is then compared to a
LITERAL (`>= 4860.0`; `42`/`0`; `0/7200/12000/16800`; `== configured`; the two B7 sentences
verbatim).

**Three gaps** (all NOTEs): the service glue is still re-implemented rather than driven (N5); the
Q5 truth-table row `(stale, no back end) -> FallBackToWall` and the `(e2)` frozen-reader check lock
a state the facade cannot produce (N4/E2); and the Q4 ordering - refusal before `MarkDispatched` -
is verified only by reading.

### 2.7 Docs

- **Assessment 7.1** is accurate on Q1-Q7 and honest about the limits: the Q5 entry states the
  `BackendCount` limit verbatim and names the OWED facade change. Q6 correctly records "no
  stop-gap"; I confirmed `20000` appears nowhere in either settings file.
- **Assessment 7.1b** is an accurate item table. `7.1a` contradicts it on the check count (E7).
- **Live gate 11** predicts the "NO VR-Forces back end is present" line on a killed back end. E2
  shows that branch cannot print. The gate is still worth running - it is the only way to learn
  what `simTime()` really does when a back end dies - but it must be re-worded as a probe with
  three possible outcomes, not as a pass/fail on a line.
- **RUNBOOK sec 11** is now below sec 10 (pass-2's C10 fixed), lists all seven keys with their
  defaults, and states both gate windows correctly (A2 closed). **Its clock-fallback bullet is
  stale and there is no Q5 bullet - D2.**
- **Merge-back:** `26efe0c` is an ancestor of `8db033e`; the branch is the only one containing the
  tip; the in-repo copy of the pass-2 review is byte-identical to the original under an 11-line
  supervisor header that says so.

---

## 3. VERIFIED vs ASSUMED

**Verified in this worktree, by building it, running it, parsing the data, or reading the vendor
headers:**
- Build 0 errors; 6 warnings on a FULL rebuild, all pre-existing; all 18 suites exit 0; the rulings
  suite prints 112 `[PASS]` and 0 failures.
- COA-STP1's graph, from my own parse: 42 tasks, 11 roots, depth 11/11/10/10, ten 4-deep chains +
  one 2-deep, one 12,000 s delay on T13, longest lead **16,800 s**.
- **42/42 dispatch** at scale 1.0 and 0.05 and under the Demo overlay - re-derived from the code's
  formulas independently of the suite.
- `VrfFacade::SimTimeSeconds` and `VrfFacade::BackendCount` read the same `backends().count()`
  (`VrfFacade.cpp:671`, `:681`).
- `doTimeouts()` deactivates rather than removes; only `remove()` takes a back end out; `backends()`
  is "all known" (`vrfBackendListener.h:89, 153-155, 161-163`; `vrfRemoteController.h:298-299`).
- `backendsControlState()` exists and returns Paused vs Running (`vrfRemoteController.h:321-323`).
- `GroundWaypointAltitudeMode` defaults to `"TerrainProfile"` (`VrfSettings.cs:601`) and neither
  settings file overrides it; `pending.Continue` is enqueued unguarded at `:4671` and `:4683`.
- One `BuildTaskStatusReport` call site; one `SimTimeSeconds` call site; the Q4 refusal precedes
  `MarkDispatched`; the supersede abandon is in the TASKABRT branch only.
- All seven R4 keys present in both settings files at the stated defaults; no `DefaultHoldSeconds`
  anywhere but in explanatory comments.
- ASCII/CRLF clean over all 12 changed files, with a dirty control proving the instrument.

**Assumed / not checked here:**
- **No live VR-Forces run.** Everything is static, offline-executed, or data-derived.
- D1's consequence is reasoned from the code path, not observed: I did not inject a throw into the
  terrain continuation. What is verified is that the continuation is unguarded, that it is the
  DEFAULT path for ground moves, and that the drain's only handler logs and returns.
- What `DtVrfRemoteController::simTime()` actually returns for a DEACTIVATED back end - the last
  cached value, 0, or a throw - is unknown. See the adversarial section.
- The 0.27x-0.73x and 9x sim ratios used in the wall-time estimates are from the record, not
  re-measured.
- STP's discarding of ObservationReports (STP-800) carried from the record.

---

## 4. NEEDS A USER RULING

**None is strictly blocking**, and that is deliberate: Q1-Q7 are ruled and the code implements
them. Two things are worth the user's attention rather than a session's:

1. **Q5's residual case is not what the ruling can reach.** The ruling says "hold while the back
   end reports, wall when it is gone". The only signal available cannot see "reports" - it sees
   "was ever discovered and never explicitly removed" (E2, vendor-confirmed). A back end that dies
   in place therefore holds task time forever. The code documents this and holds anyway, which is
   the faithful reading. The alternative is a ~1-line `VrfFacade` addition
   (`backendsControlState()`) that gives the real Paused/Running answer. Standing authorization for
   native fixes exists; whether to spend it before the demo is the user's call.
2. **A cyclic or self-referential STREND chain** (E3) is as malformed as Q4's no-Duration-no-geometry
   task, and Q4's ruling was "refuse it loudly, do not invent a way through". Today a cycle is held
   silently for a sim day. Applying Q4's principle to it is a small change and seems to follow from
   the same ruling, but it IS an extension of the ruling rather than an implementation of it.

---

## 5. WHAT TO FIX BEFORE MERGE

1. **D1** - guard the terrain-profile continuation at `VrfC2SimService.cs:4671` and `:4683` (or
   inside `PendingTerrain.Continue`) with the same abandon + TASKABRT `b76c9c7` added at
   `:2402-2414`, and add a self-test that drives a throwing continuation.
2. **D2** - rewrite RUNBOOK sec 11's clock-fallback bullet for Q5, add the hold/repeat/BackendCount
   paragraph, and fix the `"or has gone stale"` clause in the start-up line at
   `VrfC2SimService.cs:525`.

Strongly recommended in the same turn, all one-liners: **E1** (the false "readable and advancing
again" line), **E6** (26,400 -> 16,800), **E7** (90 -> 112), and correcting assessment live gate 11
to a three-outcome probe.

Mergeable as recorded debt: E2 (documented limit, fix is a facade change), E3, E4, E5, N1-N9, and
pass-2's B2, B5, C2-C15.

---

## 6. ADVERSARIAL REVIEW

**Competing hypothesis for D1: "the terrain re-entry is not the real path - `TerrainProfile` mode
is a test setting, or the re-entry cannot throw, so this is theoretical."** *Falsifiers checked and
found against it.* (a) `VrfSettings.cs:601` sets `"TerrainProfile"` as the DEFAULT and
`grep`ping both settings files shows no override, so the demo runs it. (b) The branch condition at
`:2744` is `isGround && IsTerrainProfileMode()`, and COA-STP1's taskees are ground. (c) The
re-entry runs strictly MORE code than the guarded first pass - it is the half that creates the
route, calls the bridge and runs `MarkDispatched`. (d) The project has already been bitten here:
the TickLoop comment at `:722-729` records a stale-deploy `MissingMethodException` from a JIT'd
bridge call, and `:2052-2058` records an earlier cold-start review adding exactly this guard to the
sibling init-placement enqueue for exactly this reason. **Not falsified.**

**Competing hypothesis for "A1 is closed": "the 42/0 is the suite grading its own homework - it
reproduces the service's glue, so of course it agrees."** *Falsifier checked:* I did not use the
suite. I parsed `COA-STP1_Order.xml` with my own code, built the graph myself, transcribed the
three window formulas from `TaskDispatchPolicy.cs:211-290` into an independent implementation, and
computed each edge's phase-1 and phase-2 verdict. **42/42 at scale 1.0 and at 0.05, zero failing
edges, longest lead 16,800 s against an 86,400 s backstop.** Two independent derivations agree.
**Falsified.**

**Competing hypothesis for E2: "`BackendCount` and `SimTimeSeconds` read different lists, so the
wall branch is live."** *Falsifier checked:* `VrfFacade.cpp:671` is
`p_->controller->backends().count()` and `:681` is `p_->controller->backends().count()` - the same
expression, the same object, one function apart. Combined with the early return at `:3696`, a
stale-but-readable sample proves `count() > 0` microseconds earlier. **Falsified.**

**Competing hypothesis for E2's conclusion: "therefore Q5 is not implemented."** *Weighed and
rejected.* The user's ruling is honoured in OUTCOME: a pause holds (the predicate fires, the axis
adds nothing, all three task times freeze together), and a back end that is genuinely removed makes
the reader unreadable and the axis falls to wall through the hysteresis path. What is NOT delivered
is the third case - dead but still listed - which the code names as an accepted limit rather than
papering over. The defect is that the branch WRITTEN for the "gone" case is not the branch that
runs it, so `8db033e` logged the wrong transition and the one that does run prints a false sentence
(E1).

**Competing hypothesis for D2: "sec 11 is close enough - the code's own log lines tell the truth at
run time."** *Rejected on this branch's own standard.* B7 was accepted as a defect precisely
because "the run log is where an operator has to tell them apart", and A2 was rated MAJOR because a
RUNBOOK sentence contradicted the code. Sec 11's fallback bullet contradicts `f2794d7` in the same
way, and the Q5 symptom - a WARNING repeating once a minute while nothing completes - is exactly
the thing an operator will see at a demo and have no entry to look up.

**Symptom I could NOT explain and am not filing as resolved.** What
`DtVrfRemoteController::simTime()` returns for a back end that is in the list but DEACTIVATED is
unknown. Three readings are each defensible and each ends somewhere different on the way in:
its last cached value (-> readable, flat -> `Stale` after 60 s -> HOLD forever), 0.0 (-> a large
BACKWARDS step, classified `RolledBack` not `Stale`, re-anchored at 0, then flat -> HOLD forever
by a different route, with a misleading rollback WARNING first), or a throw (-> the facade returns
-1 -> unreadable -> wall, via E1's wrong log line). All three end in a defensible state, but which
one occurs decides what the log says, and nobody has seen it. This is a falsifier for any claim
that the Q5 path's DIAGNOSTICS are correct, and it should be treated as one. Assessment live gate
11 is the right experiment; its stated expectation is wrong.

**Symptom still unexplained, not this branch's.** Pass-1's doctrine verb-vs-symbol mismatch
(`GFTPAS`/`GFTPA`/`GFTPN` drawn in the init while FOLSPT/FOLASS/NTRCOM appear in none of the 42
`TaskActionCode` values) is untouched and still open on the STP side. Pass-2's C12 (the area
"centroid" is the vertex arithmetic mean, 149-1,130 m off on 12 multi-vertex areas against a 500 m
arrival radius) likewise stands as debt.

**What no test can cover here.** The three live clock behaviours (alternating, flat, backwards) are
still unobserved on 5.2, so the suite proves the LOGIC is right GIVEN the behaviour, not that the
behaviour occurs - and E2 now shows one of those logical branches cannot be reached by the real
facade at all. D1 is the opposite case: fully decidable offline, missed by a green 112-check suite
because the suite tests policies and the service assembles them. That is the same shape as A1, and
it is the strongest argument in this review for spending the next test on the DISPATCH PATH
topology - specifically, a fault-injected continuation - rather than on more pure functions.

---

## VERDICT LINE

**FIX FIRST** - **D1** (the tick-throw abandon does not cover the default ground-move dispatch
path, and A1 made that path's failure 18x longer) and **D2** (RUNBOOK sec 11 states the pre-Q5
fallback rule and never mentions the hold). Both are small. Everything else on this branch is
sound: A1 is genuinely closed at 42/42 on an independent derivation, Q1/Q4/Q5/Q6/Q7 are implemented
as ruled, the settings carry all seven keys at their defaults, and the 112-check suite has real
teeth.
