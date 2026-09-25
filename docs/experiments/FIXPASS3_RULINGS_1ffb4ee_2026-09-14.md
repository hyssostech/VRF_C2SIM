# Fix pass 3 on feat/tasking-rulings (Opus executor, 2026-09-14 21:15-21:55Z; verbatim). Supervisor notes: items D1, D2,
# E1, E3-E7 + live gate 11 + 7.1b of REVIEW_RULINGS_8db033e; 8db033e -> 1ffb4ee; suite 112 -> 136 / 0, 18 suites green,
# -t:Rebuild 0 errors / 6 pre-existing warnings; MERGED into main 22:05Z (0f4d09e) via feat/integration; the post-merge
# rebuild + suites on main and gate G-A (native rebuild, ten bridge consumers, re-pin) follow the N2c timed run.

# FIX PASS 3 - feat/tasking-rulings, worktree .claude/worktrees/rulings

Base `8db033e` -> tip `1ffb4ee`. Seven commits, nothing merged, nothing pushed, no launches, no
sub-agents. Tier STANDARD (no new cause claim was made - D1's mechanism is the reviewer's, and I
implemented and tested it rather than re-diagnosing it).

**Suite: 112 -> 136 checks, 0 failures.** All 18 offline suites exit 0. `-t:Rebuild`: Build
succeeded, 0 errors, **6 warnings, all pre-existing** (4 CA2024 in the C2SIM SDK, 2 CS8632 at
`VrfC2SimService.cs:70` and `:3002`). The rulings suite was run **10 consecutive times**: 136/0
every time.

---

## PER ITEM

### D1 (MUST) - commit `5f661f5`
**New file `src/VrfC2SimApp/DeferredDispatch.cs`** - the one ending a dispatch gets when it dies on
the VR-Forces tick thread: ERROR naming the task, where it died and the exception TYPE;
`TaskSequencer.NotifyAbandoned`; ONE TASKABRT through `PushTaskStatus` (the single emit point, so
the one-per-task rule stays `TaskStatusPolicy`'s and is not re-implemented). Nothing announces a
start.

- `VrfC2SimService.cs:240-249` - `PendingTerrain` gains `TaskUuid` / `TaskeeUuid`.
- `VrfC2SimService.cs:4808-4846` - new `RunTerrainContinuation`, the ONE place a terrain
  continuation runs. Both enqueues now call it: `:4793` (the reply, `OnVrfTerrainProfile`) and
  `:4805` (the timeout sweep, `ExpireTerrainRequests`). Verified by grep that no
  `pending.Continue(...)` call survives outside it.
- `VrfC2SimService.cs:2853, 2871-2874` - the route consumer's `PendingTerrain` carries the task
  identity, which is what lets the ending run. The INIT PLACEMENT consumer (`:2101`) leaves them
  empty and gets the ERROR alone - it has no C2SIM task to abandon.
- `VrfC2SimService.cs:2504-2513` - `b76c9c7`'s inline guard replaced by the same
  `DeferredDispatch.Run`, so there is one pattern and not two.
- `RulingsSelfTest.cs` block `(f9)`, **8 checks**: a throw BEFORE `MarkDispatched` (one TASKABRT,
  zero TASKSTRT, successor `PredecessorAbandoned` at **0 s of clock** not the 86,400 s backstop,
  the abort names the exception type and where, a second failure adds no second TASKABRT) and a
  throw AFTER `MarkDispatched` (the one TASKSTRT it had already sent stands, exactly one TASKABRT,
  `ShouldEmitStart` refuses a second start for the same execution, successor abandoned at 0 s).

**WHICH OF THE TWO THE BRIEF OFFERED:** the runner was EXTRACTED and is what the suite drives.
The service glue around it needs a bridge, a tick thread and a C2SIM server and cannot be driven
offline. The suite composes the production `DeferredDispatch.Run`, the production `TaskSequencer`
and the production `TaskStatusPolicy`; its ONE stand-in is `PushTaskStatus`, reduced to its single
deciding line (consult `TaskStatusPolicy.ShouldEmit`, emit only when it says yes). That the two
enqueue sites call the runner is verified by READING, and is said plainly in the commit message.

**FAIL-FIRST EVIDENCE:** `DeferredDispatch.Run` was temporarily reduced to the pre-fix ending (log,
tell nobody), rebuilt and run: **114 PASS / 6 FAIL**, the six being the new `(ix)` assertions -
`"exactly ONE TASKABRT (got 0)"`, `"PredecessorAbandoned ... (got still waiting at 0 s)"`, the
abort-text check `(got "-")`, the no-second-abort check, and both halves of the after-MarkDispatched
case. The two deliberate pre-fix CONTROLS passed, as they must. Probe reverted; 120/0 again.

### D2 (MUST) - commit `fc23f14`
- `docs/RUNBOOK.md` sec 11 - the pre-Q5 clock-fallback bullet replaced by **four** bullets: task
  time HOLDS and all three task times (timed completion, both gate phases, the start delay) freeze
  together on one axis; wall only when the READER is gone (three unreadable samples, the hysteresis
  path), with the transition lines named; the HOLD WARNING REPEATS once a wall minute
  (`StallPolicy.LogRateLimitSeconds`) and why that is deliberate; and the LIMIT stated as the
  demo symptom - `doTimeouts()` deactivates rather than removes (`vrfBackendListener.h:161-163`
  against `:153-155`, `backends()` = "all KNOWN"), so a back end that dies IN PLACE freezes task
  time indefinitely with one WARNING a minute as the only output, and the fix is
  `backendsControlState()` (`vrfRemoteController.h:321-323`), **STP-809**, not taken before the demo.
- `VrfC2SimService.cs:545-552` - the start-up `TASK CLOCK (R4):` line's `"or has gone stale"`
  clause (the pre-Q5 rule, and the line the RUNBOOK cites as proof) replaced by the Q5 rule.
- Sec 11 still sits below sec 10.

### E1 - commit `2cbd722`
`VrfC2SimService.cs:3838-3865` - the transition branch now splits on `heldOnSim`: still true is the
genuine recovery and keeps the INFO line; false is a WARNING naming what happened (the reader is
gone, the confirmations that decided it, the last sim reading, how many tasks wait, that no wait
restarted and nothing completed early, and that a task now ages in REAL seconds).
`RulingsSelfTest.cs` `(e2d)`, **2 checks** on the real `SimClockTracker` and `StallPolicy`: 119 wall
seconds of a readable-but-flat clock with a back end present is a HOLD, and then losing the reader
clears `taskSimStale` exactly as a recovery does while `TaskClockAction` turns to `FallBackToWall`.
That locks the branch's REACHABILITY and the opposite meaning of the two exits; the sentences are
service log lines and are verified by reading.

### E3 - commit `846de1d` (flagged in the commit message as a SUPERVISOR DECISION extending Q4)
- `TaskDispatchPolicy.cs` - `CyclicPredecessorRefusal`, `FindPredecessorCycles` (one forward walk
  per unvisited node; the graph has out-degree one), `DescribePredecessorCycle` ("A -> B -> A").
- `VrfC2SimService.cs:2274-2288` (the walk) and `:2311-2328` (the refusal) - at order receipt,
  after `_taskByUuid` is filled, every task on a loop gets Q4's shape: ERROR naming the loop,
  `NotifyAbandoned`, one TASKABRT, `continue` - before any orchestration starts and therefore
  before any TASKSTRT. Tasks that merely POINT AT a loop are NOT refused; they cascade through the
  existing `PredecessorAbandoned` path. The walk uses `_taskByUuid` deliberately: that is the map
  the GATE consults, so what is checked is the graph that would take the backstop.
- `RulingsSelfTest.cs` `(f10)` + one check inside `(f5)`, **8 checks**: self-reference, 2-cycle and
  3-cycle (every member on the loop), healthy chain and dangling reference are NOT cycles, the
  entrant is not on the loop and its gate fails `PredecessorAbandoned` at 0 s, the loop description,
  and **NO FALSE POSITIVE on the real 42-task COA-STP1 graph off disk** - the check that matters,
  because this refusal is terminal.

**FAIL-FIRST EVIDENCE:** two controls in the suite, walked on the real sequencer under the A1 rule:
a self-referencing task dispatches **0 of 1** and is skipped only after the 86,400 s backstop (at
90,000 s, `PredecessorNeverDispatched`), and a 2-cycle dispatches **0 of 2**, both skipped no
earlier than the backstop. Before A1 the same order failed in 600 s. The checks themselves could
not be run against pre-fix code because `FindPredecessorCycles` did not exist; the controls measure
the cost the fix removes, which is the evidence available offline.

### E4 + E6 - commit `c4f7785` (one commit: E6's corrected figure is produced by E4's function)
- `TaskDispatchPolicy.cs` - `ChainNode`, `LongestChainLeadSeconds`, `LongestChainEndSeconds`
  (the gate's own arithmetic, after `Vrf:DurationScale`, cycle-guarded and depth-bounded).
- `VrfC2SimService.cs:2290-2309` - one `CHAIN DEPTH:` INFO line per order (lead, end, scale,
  backstop) and a WARNING when the lead meets or exceeds the backstop, naming what to raise.
- **E6:** `TaskDispatchPolicy.cs:333-341` and `VrfSettings.cs:449-455` - "26,400 s" corrected to
  **16,800 s** (longest DISPATCH lead) and **21,600 s** (last END), in BOTH places, and now
  MEASURED by the suite off `data/COA-STP1_Order.xml` rather than asserted in a comment.
- `RulingsSelfTest.cs` - `WalkChain`'s horizon now calls the production function, so the suite's
  private copy of that arithmetic is GONE (one re-implementation fewer; N5's family). **4 checks**:
  the real graph measures 16,800 / 21,600; both fit the backstop with 5.1x headroom; the lead is
  measured after `DurationScale` (0.05 -> 840 s; 10 -> past the backstop, the WARNING's case).

### E5 - commit `9a0a928`
**DECISION: REFUSE, do not clamp** (documented in RUNBOOK sec 11).
- `TaskDispatchPolicy.cs` - `DefaultPredecessorEndMarginSeconds` (60.0) and
  `IsUsablePredecessorEndMargin` (finite and > 0), with the reason zero is not harmless.
- `VrfC2SimService.cs:508-526` (block `0d-ii`, beside the `DurationScale` validation) - a
  non-positive value is an ERROR naming the consequence and the run proceeds at 60 s. The validated
  value goes into `_predecessorEndMargin` (`:2360`) and the three read sites (`:559`, `:2395`,
  `:2414`) use THAT, so a value the start-up line has refused cannot be read again and quietly
  applied - the same shape `_durationScale` already had.
- `RulingsSelfTest.cs` - the false label is now **three** checks: the clamp is a floor (a negative
  margin never shortens the window below the predecessor's end time); ZERO is not usable and is
  refused at start-up; and the difference is the point (at 0 the gate expires exactly when the
  predecessor is due, at 60 s it outlives it).
- `docs/RUNBOOK.md` sec 11 - its own bullet, plus the E4 CHAIN DEPTH bullet.

### E7 + live gate 11 + 7.1b - commit `1ffb4ee`
- **E7:** 7.1a's "(90 checks across SIX sections)" -> **136**, and 7.1b's "112" kept as what it was
  at the END of pass 2 with "(it is 136 after pass 3)" beside it. Both places now agree with the
  suite. 7.1a's sha list gains the pass-3 commits.
- **Live gate 11** re-worded as two halves. PASS/FAIL: the real pause (the HOLD line appears and
  REPEATS, nothing completes during it, every task completes its full remaining Duration after the
  resume). PROBE, three recorded outcomes and no prediction: (a) last cached value -> readable,
  flat -> Stale after 60 s -> HOLD forever; (b) 0.0 -> a backwards step classified `RolledBack`,
  re-anchored, then flat -> HOLD by another route with a misleading rollback WARNING first;
  (c) a throw -> facade -1 -> unreadable -> WALL through the transition line E1 corrected. The gate
  records WHICH occurred and WHAT THE LOG SAID, verbatim. The old wording predicted a line E2 shows
  cannot print.
- **7.1b** is now THREE reviews, with a pass-3 table: D1, D2, E1, E3, E4, E5, E6, E7 with their
  shas, and the **RECORDED DEBT the brief named, all listed and none of it touched**: E2 (with
  STP-809 as its fix), N1 (M1 doc-comment binding), N2 (`_taskByUuid` spans orders), N3 (start-delay
  log vs the sequencer's rule), N4 (a check locking a state the facade cannot produce), N5 (the
  glue re-implementations), N6 (`IssueEngage`'s bare lambda), and pass-2's B2, B5 and C2-C15 carried
  forward unchanged. It also records that A1 is CLOSED by the reviewer's independent re-derivation.

---

## ADVERSARIAL REVIEW - what it caught

1. **A non-deterministic assertion I introduced.** E3's FAIL-FIRST control asserted the exact
   `GateResult` of BOTH halves of a 2-cycle. The two gates expire at the same clock reading and
   whichever the thread pool resolves first abandons the other, so the result of each is a race: it
   passed in the E3 commit and FAILED on the next run. Repaired in `c4f7785` (disclosed in that
   commit message): a SELF-cycle carries the exact outcome (one task, nothing to race), the 2-cycle
   carries the cost, and only the non-racy properties are asserted. 10 consecutive runs identical.
   This is the second time on this branch that a green suite hid a boundary race - it is worth
   reading any new multi-gate assertion for one.
2. **A new build warning.** E4's WARNING repeated `{B:F0}` three times against two arguments -
   CA2017, 6 warnings -> 7. Fixed before the commit; back to the pre-existing 6. The `-t:Rebuild`
   gate is what caught it; an incremental build would have said "up-to-date, 0 warnings" (N9).
3. **Competing hypothesis for D1's fix being in the wrong place:** "`RunTerrainContinuation` is not
   really on the default path, so the fix is decorative." Checked and rejected:
   `GroundWaypointAltitudeMode` is `"TerrainProfile"` at `VrfSettings.cs:601`, neither settings file
   overrides it, the deferral branch is `isGround && IsTerrainProfileMode()`, and `grep` over every
   `_tickActions.Enqueue` confirms the only two terrain enqueues both call the new runner and that
   no bare `pending.Continue(...)` survives.
4. **Competing hypothesis for E1:** "the wrong-sentence branch is unreachable, like E2's." Rejected
   by construction, and that is exactly what `(e2d)` locks: `SimClockTracker` really does reach
   `ReadableConfirmed == false` after a HOLD, at which point `taskSimStale` goes false with no
   recovery having happened.

---

## RESIDUAL RISKS (verified vs assumed, separated)

**Verified** (built, run, parsed, or read in this worktree):
- The 136/0 suite result, the 18 green suites, the 6 pre-existing warnings, and the 10-run
  determinism.
- Both terrain enqueues and the first dispatch pass go through the shared ending; no direct
  `pending.Continue` call survives (grep).
- `_predecessorEndMargin` is validated at `:515` before the start-up line at `:559` reads it and
  before any order is handled.
- ASCII + CRLF clean over all 7 files changed since `8db033e`, with a dirty control that fires at
  exactly 3 before every run (the instrument is proved each time, not assumed).
- E6's 16,800 / 21,600 come from `data/COA-STP1_Order.xml` through the production function.

**Assumed / not checked here:**
- **No live VR-Forces run.** Everything is offline. That the terrain reply path really throws, and
  what it throws, is unobserved - D1 makes the ENDING correct, not the failure rare.
- **A throw AFTER the bridge move calls now reports TASKABRT for a task VR-Forces may really be
  executing.** This is what the brief specified ("do NOT emit a second TASKSTRT and still emit the
  single TASKABRT") and it is the honest report of the interface's own state - the bookkeeping
  failed, so nothing here will complete the task. The armed end time is cancelled by
  `PushTaskStatus`, and a later arrival or vendor completion can still produce a TASKCMPLT, which is
  the C16 shape (TASKABRT does not suppress a later TASKCMPLT). Worth an operator note if it is ever
  seen live.
  CORRECTION 2026-09-25: this is a SUPERVISOR reading (2026-09-13), not an owner ruling - RL-20260914-01 covers only the TASKABRT code; see its scope note. The current TEMPORARY completion position is RL-20260921-09.
- The `CHAIN DEPTH:` and cycle-refusal LOG LINES are verified by reading; their arithmetic and their
  decisions are under test, the sentences are not.
- E3 uses `_taskByUuid`, which spans orders (N2, recorded debt). A cross-order cycle is detected,
  but only the tasks of the CURRENT order are refused - a prior order's task on the loop was already
  handled by its own pass. Documented in the code and in 7.1b.
- 7.1b references `docs/experiments/REVIEW_RULINGS_8db033e_2026-09-14.md` as the in-repo copy of the
  pass-3 review, matching the brief's statement that the supervisor is recovering it there. **I did
  not create that file** - the reference is dangling until the supervisor files it.

## NOT DONE (deliberately)
- The recorded debt (E2, N1-N6, pass-2 B2/B5/C2-C15) is listed in 7.1b and untouched, per the brief.
- No merge, no push. The branch tip is `1ffb4ee`; the supervisor merges.
