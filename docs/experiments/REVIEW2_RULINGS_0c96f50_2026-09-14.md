SUPERVISOR HEADER (2026-09-14). VERDICT ACCEPTED: FIX FIRST. A1 (M1 was MOVED, not closed: 21 of
COA-STP1's 42 tasks still skipped, measured) and A2 are FIXED in b6471a3 and 4ca7afa, and the
FIX-FIRST minors in 821b359 (B1), df7f7e2 (B4), d792901 (B6) and ba44d7e (B7); B3 is MOOT and Q4
and Q5 are re-decided by the USER'S RULINGS of 2026-09-14 (168207f: a task with no Duration and no
geometry is MALFORMED and is refused, Vrf:DefaultHoldSeconds deleted; f2794d7: a paused scenario
does not age a task while the back end is present). Q5-Q7 were ASKED OF THE USER and are answered
in sec 7.1 of TASK_VOCABULARY_ASSESSMENT_2026-09-14.md; B2, B5 and C1-C15 stay RECORDED DEBT there.
The review below is the reviewer's text, unaltered; its line/finding numbers refer to 0c96f50.

---

# COLD-START REVIEW, PASS 2 - feat/tasking-rulings @ 0c96f50

Reviewer: Opus cold-start, read-only. Tier HEAVY. Date 2026-09-14.
Worktree reviewed: <repo>\.claude\worktrees\rulings (NOT modified).
Base for the diff: feat/integration 26efe0c (merged in at 182bd51).
Inputs: pass-1 review of 5c67d41 (M1-M5, m1-m9, n1-n10, Q1-Q4, x1-x5); fix commits 075c0b7,
3fe69fa, 48e7c5d, 1ddb9a7, 06f8cf0, 6d46921, 09bac94, 533bc2f, 1ccfb1f, 0c96f50.

## VERDICT: **FIX FIRST**

M2, M3, M4, M5 and m1-m9 are genuinely closed, and the new clock code is the best part of the
branch. **M1 IS NOT.** It was fixed one level down and re-created one level up: the derived gate
covers the predecessor's own Duration but NOT the predecessor's LEAD TIME (its start delay and its
own gate wait), while phase 1 of the gate still runs from ORDER RECEIPT. Measured against the real
`TaskSequencer` + `TimedCompletionPolicy` + `TaskDispatchPolicy` on a common monotone clock:
**21 of COA-STP1's 42 tasks still dispatch and 21 are still SKIPPED with TASKABRT, at every shipped
setting** - defaults, the Demo overlay's 7200, and a compressed `DurationScale=0.05`. The branch's
own assessment gate 2 ("42 dispatches, not 9") remains unreachable, and `docs/RUNBOOK.md` sec 11
now tells the operator the opposite of what the code does.

---

## 0. WHAT I RAN

- `dotnet build src\VrfC2SimApp -c Release -p:BridgeConfig=Release-5.2` - **Build succeeded, 0
  errors**, 6 warnings (4 CA2024 in the SDK; 2 CS8632 at VrfC2SimService.cs:70 and :2850, both
  blamed to 88488be2 / 2026-07-13 - PRE-EXISTING, not this branch).
- **All 18 offline suites, with `C:\MAK\vrforces5.2d\bin64` prepended to PATH. All 18 exit 0:**
  `--translator-selftest SELF-TEST PASSED`; `--report-selftest ALL CHECKS PASSED`;
  `--sequencer-selftest ALL CHECKS PASSED`; `--verb-selftest ALL CHECKS PASSED`;
  `--destack-selftest ALL CHECKS PASSED`; `--fanout-selftest ALL CHECKS PASSED`;
  `--typemap-selftest SELF-TEST PASSED (783 checks)`; `--terrain-selftest PASS`;
  `--placement-selftest PlacementPolicy self-test PASSED`; `--compose-selftest ALL CHECKS PASSED`;
  `--arrival-selftest ALL CHECKS PASSED`; `--stall-selftest ALL CHECKS PASSED`;
  `--parse-selftest ALL CHECKS PASSED`; `--name-selftest ALL CHECKS PASSED`;
  `--preflight-selftest ALL CHECKS PASSED`; `--rulings-selftest ALL CHECKS PASSED`;
  `--scripted-task-selftest ALL CHECKS PASSED`; `--initgraphics-selftest ALL CHECKS PASSED`.
  The rulings suite prints **78** check lines (the brief says 77 - one more than recorded).
- `--parse-order data\COA-STP1_Order.xml`: 42 tasks; MapGraphicID 0/42; embedded Location 33/42;
  no geometry 9/42; durations 42/42 (32 x 4,800,000 ms + 10 x 7,200,000 ms); start delays 41 x 0
  and **1 x 12,000,000 ms (T13, PT3H20M)**.
- **An independent census of the order's task GRAPH** (python over the XML): 11 chains, one per
  taskee, **serial and up to FOUR tasks deep**. Depth histogram: d0 = 11, d1 = 11, d2 = 10,
  d3 = 10. Root durations PT2H (7,200 s) except T13 (PT1H20M plus the 12,000 s delay); every
  non-root PT1H20M (4,800 s).
- **A chain probe I built in the scratchpad** (NOT in the repo) that links the worktree's OWN
  `TaskSequencer.cs`, `TaskDispatchPolicy.cs` and `TimedCompletionPolicy.cs` unmodified, plus a
  4-line enum shim for `C2SIM.Schema102.TaskStatusCodeType`, and reproduces
  `VrfC2SimService.HandleOrder`: all gates started at t = 0, each window derived exactly as
  VrfC2SimService.cs:2270-2276 derives it, dispatch -> `NotifyDispatched` + `Register` +
  the anchoring `Advance`, then a monotone fake clock walked in 10 s steps.
  Source: `<scratchpad>\review_rulings\chainprobe`. Results in finding A1.
- ASCII + line-ending audit of all 19 changed files, in python (NOT `grep -P`, which reported
  "supports only unibyte and UTF-8 locales" on the control file and would have passed a dirty
  file silently): **0 offenders outside TAB/CR/LF/0x20-0x7E, 100% CRLF on every file**; the
  dirty control (e-acute, en dash, curly quotes) returns 11. The instrument works.
- `git merge-base --is-ancestor 26efe0c 0c96f50` -> **26efe0c IS an ancestor of 0c96f50**.

---

## 1. FINDINGS

Severity: MAJOR = wrong behaviour on the demo path, or an operator instruction that is false.
MINOR = wrong in a reachable but narrower case, or an accuracy/consistency defect.
NOTE = record only.

### MAJOR

| # | file:line | input -> wrong outcome | fix |
|---|-----------|------------------------|-----|
| **A1** | `TaskSequencer.cs:137-149` (phase 1) + `VrfC2SimService.cs:2270-2276` (the derivation) + `:2204-2246` (every gate starts at order receipt) | **M1 WAS MOVED, NOT CLOSED. The derived window covers the predecessor's DURATION but not its LEAD TIME, and phase 1 still runs from wait-start.** The window for a successor is `max(configured, predDuration x scale + margin)` - it knows nothing about how long the predecessor itself waits before dispatching. Phase 1 demands the predecessor DISPATCH within that window, measured from the successor's own wait-start, which for every task in the order is order receipt (`HandleOrder` fires `RunTaskAsync` for all 42 in one loop and the first thing each awaits is the gate). Two independent ways that fails on COA-STP1: **(i) chain depth.** T3's window is derived from T2 (4,800 s) = 4,860 s, but T2 dispatches only when T1 completes, at 7,200 s. 7,200 > 4,860 -> T3 phase-1 times out and is SKIPPED; T4 then fails fast on T3's abandon. **(ii) a delayed root.** T13 carries a 12,000 s start delay, so T14's 4,860 s window expires 7,140 s before its predecessor dispatches. MEASURED on the real classes (probe above): defaults (600/60/1.0) -> **2 of 4** in a 4-deep chain and T14 skipped; Demo overlay (7200/60/1.0) -> **2 of 4**, T14 skipped; compressed (600/60/0.05) -> 3 of 4, and T14 sits on the exact boundary (window 600 s vs a predecessor dispatching at 600 s) where it timed out in **6 of 7 runs** and dispatched in 1 - a non-deterministic demo, which is worse than a deterministic failure. Extrapolated to the order: **21 dispatches, 21 TASKABRT(SKIPPED)** at defaults and at the Demo overlay. `PredecessorTimeoutPolicy=force` is not a workaround either - it would dispatch T3 onto a unit still running T2, which the Q1 default then TASKABRTs. The class doc at `TaskSequencer.cs:55-57` states the limitation and asserts "the real orders carry single-level chains only" - **that claim is false for the order this branch exists for**. | Phase 1's window must cover the predecessor's OWN lead time. Either (a) derive it recursively over `_taskByUuid` - `Lead(t) = Lead(pred) + scaledStartDelay(pred) + scaledDuration(pred) + margin`, cycle-guarded, computed once per task before `WaitForStartAsync`; or (b) **drop phase 1's timeout entirely when the predecessor uuid names a task in THIS order** and keep the configured value only for a DANGLING reference. (b) is sound here: I audited every path that can end a task without dispatching it - `:2210`, `:2219`, `:2327`, `:2368`, `:2394`, `:2529`, `:2630` and the `RunTaskAsync` catch - and **all of them call `_sequencer.NotifyAbandoned`**, so a successor already fails fast on any real dead end; the phase-1 timer is only a backstop for a predecessor that does not exist. Keep a very generous absolute backstop if one is wanted. Either way the M1 self-test must gain a case where the predecessor has NOT dispatched when the gate starts waiting. |
| **A2** | `docs/RUNBOOK.md` sec 11 ("You no longer need to raise `TaskPredecessorTimeoutSeconds` for a long order - and raising it does no harm"); `docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md` sec 7.1b header "ALL ITEMS FIXED", its M1 row, and live-gate 2 "42 dispatches, not 9" / gate 5 "none of them may report the skip TASKABRT" | **The operator instruction is the exact inverse of the code's behaviour (A1).** On COA-STP1 you MUST raise it: measured, `TaskPredecessorTimeoutSeconds = 20000` makes all 4 tasks of a chain dispatch (0 / 7,200 / 12,000 / 16,800) and T14 dispatch at 16,800. An operator who follows sec 11 and leaves it at 600 (or the Demo 7200) gets half an order and 21 TASKABRTs - and the run log will say the predecessor "did not complete within 4860s of its dispatch" for a predecessor that never dispatched at all (see B7), so the cause will not be legible from the log either. | Fix A1; until then correct sec 11 to "raise it above the longest chain's total lead time (>= 20,000 s for COA-STP1 at scale 1.0)", and downgrade 7.1b from "ALL ITEMS FIXED" to "M1 partially fixed - phase 2 only". |

### MINOR

| # | file:line | input -> wrong outcome | fix |
|---|-----------|------------------------|-----|
| B1 | `VrfC2SimService.cs:2988-2992` | **The Q1 supersede TASKABRT does not abandon the task in the sequencer.** Every other TASKABRT path in the file pairs `PushTaskStatus(...TASKABRT...)` with `_sequencer.NotifyAbandoned(...)`; this one does not. So a task the interface has just told STP is NOT being performed still holds its successors at the gate - and since M1 the gate is now `predDuration + 60` (up to 7,260 s) rather than 600, so the successors wait far LONGER than before the fix before being skipped. The abandon semantics contradict the report the interface just sent. | `_sequencer.NotifyAbandoned(old.TaskUuid)` beside the TASKABRT (and NOT in the `TASKCMPLT` branch, where the timer still stands). |
| B2 | `VrfC2SimService.cs:4285-4310` (`SynthesizeUnitCompletion`) vs `:2965-2992` | **Pass-1's m2 is only half fixed.** The state-clearing half is fixed (the in-place branch clears nothing). The ATTRIBUTION half is not: a superseded move's LATE vendor completion reaches `SynthesizeUnitCompletion`, `_inFlight.TryComplete` pops whatever is current - **the NEW task** - and emits TASKCMPLT for it, which `PushTaskStatus` then uses to CANCEL the new task's armed end time and `CompleteTask` uses to release ITS successors early. With Q1's default the interface makes two wrong statements in a row: TASKABRT(old) and a premature TASKCMPLT(new). Same reachability as B1 (needs a supersede: 0 taskees have concurrent ungated tasks on COA-STP1 under `policy=skip`). | Record the superseded task uuid per unit and swallow its late vendor completion (the `_arrivalReported` mechanism already does exactly this for the arrival case), or compare `fin.TaskUuid` against a "superseded, expect one late callback" set. |
| B3 | `VrfC2SimService.cs:3040-3049` (Q4 hold) vs `:2270-2276` (the derivation) | **`Vrf:DefaultHoldSeconds` is invisible to the gate.** The predecessor's end time is derived from `predTask.DurationMs` only; a task that got its end time from the INVENTED `DefaultHoldSeconds` contributes 0, so the window falls back to the configured floor. At the shipped 60 vs 600 that is safe, but the two knobs can be set to contradict each other (e.g. `DefaultHoldSeconds=1200`, `TaskPredecessorTimeoutSeconds=600` skips the successor 600 s before the hold it is waiting for expires) and nothing warns. | Validate at start-up that `DefaultHoldSeconds + TaskPredecessorEndMarginSeconds <= TaskPredecessorTimeoutSeconds`, or teach the derivation that a geometry-less predecessor with no Duration ends at `DefaultHoldSeconds`. |
| B4 | `VrfC2SimService.cs:2269-2276` | **The derivation ignores `Vrf:TimedCompletion`.** With `TimedCompletion=false` (evidence-only completion - the documented escape hatch) no timed completion ever fires, yet every gated successor now waits `predDuration + 60` (4,860 s) instead of the configured 600 s before being skipped. Turning R4 OFF makes the failure mode eight times slower and quieter. | Derive the predecessor end only when `_vrf.TimedCompletion` is true. |
| B5 | `VrfC2SimService.cs:2296-2302` | **The ABSOLUTE StartTime is a WALL instant served on the SIM axis, and scaled.** `startMs = (absoluteStart - DateTime.UtcNow).TotalMilliseconds` is a real-seconds delta; it is then `ScaleOrderMs`-compressed and handed to `WaitForStartAsync`, which serves it on the task-clock axis. At the measured 0.27x a task dated one hour ahead dispatches 3.7 hours later in wall time; at 9x, 400 s later. An absolute date-time is by definition a wall instant. Not reachable on any order on disk (STP exports the relative `SimulationTime` form), so it is a latent unit error, not a live one. The log line at `:2301` does not name a clock either. | Serve an absolute StartTime on the WALL clock (or convert it to sim seconds through the measured ratio and say so); do not apply `DurationScale` to a wall-derived offset. |
| B6 | `appsettings.json`, `appsettings.Demo.json` | **None of the six R4 keys is in either settings file** - `TimedCompletion` (default TRUE), `DurationScale`, `TaskClock` (default `sim`), `TaskPredecessorEndMarginSeconds`, `SupersededTaskCode`, `DefaultHoldSeconds`. For a branch whose next milestone is a STANDALONE DEMO deployment whose behaviour must be readable from its settings, the two decisions that most change what an operator sees (timed completion ON, and the SIM clock as the default time base for every task time) are visible only in C# source. | Add all six to `appsettings.Demo.json` with the `_Key` explanation comments that file already uses. |
| B7 | `VrfC2SimService.cs:2318-2320` | **The gate-timeout message cannot tell phase 1 from phase 2.** `why` is always `"did not complete within {timeout}s of its dispatch"`, but a phase-1 timeout means the predecessor NEVER DISPATCHED. A1's failures will all print the phase-2 wording, which mis-states the cause and defeats live gate 5 (which is specified as a log check). | Return a distinct `GateResult` (or an out flag) for the phase-1 timeout and word it "never dispatched within Ns of order receipt". |

### NOTE

| # | where | note |
|---|-------|------|
| C1 | `TaskDispatchPolicy.cs:99-143` | **The M1 rationale is attached to the wrong method.** Lines 99-126 are ONE contiguous `///` block (two `<summary>` elements plus `<param name="configuredSeconds">` and `<param name="predecessorEndSeconds">`), so the compiler binds all of it to `ScaleOrderMs` at :127, while `PredecessorTimeoutSeconds` at :143 carries only an orphan `<param name="marginSeconds">`. Invisible today (no `/doc`), but this repo treats the header comment as the contract. Move :99-118 down to :141. |
| C2 | `VrfC2SimService.cs:3701` | `_timed.Advance(clockNow, usingSim: true)` is HARDCODED. It is correct - the axis already refuses to accumulate across a mode change, so a second re-anchor would only lose time - and the comment at :3681-3684 says exactly that. The consequence is that `TimedCompletionPolicy`'s documented mode-change re-anchor (`:109-115`) is now DEAD in production and exercised only by the self-test. Worth stating in the policy header so a future reader does not "fix" the caller. |
| C3 | `TaskClockSampleSeconds = 1.0` (`:207`), `TimedCheckSeconds = 1.0` (`:176`) | **Quantified M2 error.** The axis is a staircase updated once per WALL second with the sim value, so at sim ratio r one step is r sim s. A timed TASKCMPLT is late by up to ~3r sim s (anchor lag <= 2 samples plus overshoot <= 1 walk); the phase-2 gate expires at `predEnd + 60` on the SAME axis, so the M1 phase-2 relation holds while `margin > ~3r`. At the measured 0.27x that is <= 0.9 s; at the R9-measured 9x it is <= ~27 s against a 60 s margin. **It breaks above roughly r = 20**, and nothing checks the ratio against the margin. Worth one line in `VrfSettings`. |
| C4 | `VrfC2SimService.cs:3584-3592` | **New per-second bridge call in the DEFAULT configuration.** `SampleTaskClock` reads `_bridge.SimTimeSeconds()` whenever `Vrf:TaskClock` prefers sim - which is the default - so the shipped build now calls into the native bridge once a second on the tick thread where, with `StallDetection=false` (also the default), it previously never did. Almost certainly free, but it is a behaviour change on the golden path and nothing measured it. |
| C5 | `SimClockTracker.cs:118-123`, `VrfC2SimService.cs:3619-3654` | **M4 cannot distinguish PAUSED from DEAD, and it chose to keep serving.** After 60 wall seconds of a flat sim clock the axis serves WALL seconds, so an operator who pauses the scenario for ten minutes burns 600 s off every armed Duration; the same happens at start-up before the scenario is ever run (the clock reads 0, flat, readable -> stale at 60 s -> wall). The class header states the divergence from the watchdog deliberately. It is a semantics decision, not a bug - but R4's own ruling ("a paused scenario does not age a task") is only true for 60 seconds. See Q5. |
| C6 | `SimClockTracker.cs:120` | `flatFor = wallSeconds - _lastChangeWall` counts UNREADABLE time as flat time, because `_lastChangeWall` only moves on a readable non-flat sample. A sim clock that disappears for two minutes and returns at its old value is `Stale` on its very first readable sample. Harmless (one extra warning; the wall fallback was already in force) but the number in the log overstates the freeze. |
| C7 | `VrfC2SimService.cs:201-202, 3596, 3881` | `_simClockLast` is a ~40-byte non-volatile struct field. **Safe as written** - `SampleTaskClock` writes it and `MaybeCheckStalls` reads it, and both run only inside `TickPhase` on the tick thread - but a torn read is what a future off-thread consumer would get. `TaskClockAxis` by contrast is correctly `Volatile`-guarded for its cross-thread readers. |
| C8 | `RulingsSelfTest.cs:451-484` (`WalkTaskClock`) | The M3/M4 checks use the REAL `SimClockTracker`, `TaskClockAxis` and `TimedCompletionPolicy`, but `WalkTaskClock` **re-implements the 8 lines of service glue** in `SampleTaskClock:3644-3654`. The policies are covered; the wiring is not. A drift in `SampleTaskClock` (say, dropping the `heldOnSim && !obs.Readable` early return) would not fail a single check. |
| C9 | `VrfC2SimService.cs:173-175` | Stale comment: "the timed-completion cadence ... costs one SimTimeSeconds read a second". Since 48e7c5d the timed walk reads only the axis; `SampleTaskClock` owns the read. |
| C10 | `docs/RUNBOOK.md` | Sec **11 is inserted ABOVE sec 10** in the file. |
| C11 | `VrfC2SimService.cs:1259-1285` vs `:1329-1345` | M5's line/point registration loop runs AFTER the AREA create-enqueue loop, so an order arriving inside that window sees areas registered and lines/points not. Same class as pass-1's n5 (order-before-init) and equally narrow; the registration itself is correctly BEFORE any line/point create and correctly INDEPENDENT of `Vrf:CreateInitLines`/`Points`. |
| C12 | `TaskGeometryResolver.cs:188-194` | Pass-1's n1 stands unchanged and is not claimed fixed: the area "centroid" is the vertex arithmetic mean, 149-1,130 m from the true area centroid on COA-STP1's 12 multi-vertex areas against a 500 m arrival radius. Recorded debt. |
| C13 | build | 2 CS8632 warnings (`VrfC2SimService.cs:70`, `:2850`) are PRE-EXISTING (`git blame` -> 88488be2, 2026-07-13), not introduced here. Pass-1's "0 warnings" was optimistic about the whole build. |
| C14 | `VrfC2SimService.cs:3663-3672` | `TaskClockDelayAsync` polls at 5 Hz per waiting gate instead of arming one timer - 31 concurrent pollers on COA-STP1, for hours. Negligible cost; noted only because it is the one busy-wait the port re-introduced, and the C++ busy-wait is what `TaskSequencer` exists to replace. |
| C15 | - | ASCII + CRLF clean across all 19 changed files (0 offenders; control file returns 11). |

---

## 2. ANSWERS TO THE BRIEF'S SIX QUESTIONS

### 2.1 Were M1-M5 closed, or moved?

**M1 - MOVED (finding A1).** Phase 2 is genuinely fixed and proven: with the derived window a
successor dispatches exactly at its predecessor's timed completion, measured. Phase 1 is not, and
phase 1 is what COA-STP1 hits.

*The arithmetic, derived and then measured.* Window = `max(max(1, configured), predEnd + margin)`
where `predEnd = ScaleOrderMs(pred.DurationMs)/1000`. For the 31 gated tasks at `DurationScale=1.0`
with `configured=600, margin=60`: the 11 tasks behind a PT2H root get 7,260 s; the other 20 behind a
PT1H20M predecessor get 4,860 s.

| chain position | window | predecessor dispatches at | phase-1 verdict |
|---|---|---|---|
| d1 behind a PT2H root (10 tasks) | 7,260 s | ~0 s | PASS -> dispatch at 7,200 s |
| T14 behind T13 (PT3H20M delay) | 4,860 s | 12,000 s | **TIMEOUT -> SKIPPED** |
| d2 (10 tasks) | 4,860 s | 7,200 s | **TIMEOUT -> SKIPPED** |
| d3 (10 tasks) | 4,860 s | (predecessor abandoned at 4,860 s) | **ABANDONED -> SKIPPED** |

**Total: 21 dispatched, 21 TASKABRT.** Demo overlay (`configured=7200`): identical - T3's window
becomes 7,200 while T2 dispatches at 7,200 plus slop, and the slop is strictly positive by pass-1's
own ordering argument, so it loses by a second or two; T4's predecessor dispatches at 12,000
regardless. At `DurationScale=0.05` the chain shortens (360 / 240 / 240 / 240) and d3 lands exactly
on the 600 s floor: measured as a boundary race, PredecessorTimeout in 6 of 7 runs. Raising
`TaskPredecessorTimeoutSeconds` to 20,000 makes all four dispatch (0 / 7,200 / 12,000 / 16,800) and
T14 dispatch at 16,800: measured. **No successor can be skipped before its predecessor's timed
COMPLETION (phase 2 is sound); successors ARE skipped before their predecessor's DISPATCH.**

*Predecessor with NO Duration.* `predEnd = 0` -> the configured floor stands alone (600 s), verified
by self-test and by reading `PredecessorTimeoutSeconds:147`. If that predecessor is also
geometry-less, Q4 arms `DefaultHoldSeconds = 60` and it completes at 60 s, well inside 600 - so the
successor DISPATCHES, which is the intended abandon-free semantics. The two knobs are not
cross-checked (B3).

*Predecessor SUPERSEDED (Q1 = TASKABRT).* The successor neither dispatches nor fails fast: it WAITS
OUT the full derived window - now up to 7,260 s - and is then skipped with TASKABRT. That is
inconsistent with the abandon semantics used everywhere else (B1). What STP should see, in my
reading: the successors of a superseded task are exactly as dead as their predecessor, so they
should be abandoned at the supersede point and reported TASKABRT immediately - one
`NotifyAbandoned` call.

**M2 - CLOSED.** `Vrf:TaskClock` is its own knob (default `sim`), `StallClock` is documented as the
watchdog's alone, and all three task times ride `TaskClockAxis`. **Nothing in the R4 path is still
measured in wall seconds:** `TaskClockDelayAsync:3663-3672` uses `Task.Delay(200 ms)` only as a
POLL - the loop's exit condition is `TaskClockSeconds - start < seconds`, i.e. axis seconds. The
1 s cadences (`TaskClockSampleSeconds`, `TimedCheckSeconds`) are wall SAMPLING rates by necessity
and set the quantization, not the unit. Quantified error in C3: <= ~3r sim seconds of lateness at
ratio r, i.e. <= 0.9 s at 0.3x and <= ~27 s at 9x, against a 60 s margin; the relation breaks above
about r = 20. The one genuine leftover is B5, the absolute StartTime (a wall delta served on the sim
axis) - unreachable on any order on disk.

**M3 - CLOSED.** One sampler (`SampleTaskClock`), one observer (`SimClockTracker`), the
hysteresis-CONFIRMED mode for both consumers (`heldOnSim = preferSim && obs.ReadableConfirmed`), and
`if (heldOnSim && !obs.Readable) return;` so a missed sample keeps the anchor instead of flipping.
The fail-first control is real and fails on the raw mode (axis 0 s after 400 samples).

**M4 - CLOSED, with a semantics consequence (C5, Q5).** `obs.Stale` is honoured by both consumers;
the watchdog SUSPENDS judging, the task clock falls back to WALL and keeps serving, each warned once
per transition.

*C16 calibration.* **Unchanged.** The 360 sim s / 240 wall s windows are measured in clock seconds,
which the shared sampler does not touch, and `ClampCheckSeconds`/`NextCheckSeconds` still own the
cadence (floor 1 s = the sampler's cadence, so the watchdog can never outrun its sample). What DID
change is mode-adoption LATENCY: `ModeSwitchConfirmations = 3` is now counted at the sampler's fixed
1 s rather than at the watchdog's adaptive `StallCheckSeconds`, so a sustained reader loss is
adopted in ~3 s instead of 3 x StallCheckSeconds, and a mode change drops every ring. Direction:
rings are dropped sooner and more often on a flaky reader. A second, smaller effect: the watchdog's
ring stamps now come from a sample up to 1 s old (at 9x, up to 9 sim s of skew on a 360 sim s
window = 2.5%). Neither invalidates RECAL_STALL_SIMSECONDS; both are worth a line in that doc.

*"Readable but frozen for 3 s then resumes - flip-flop?"* **No.** Frozen is READABLE, so the mode
never changes; `Stale` needs 60 wall seconds of flatness. The axis simply adds 0 for those 3 s and
resumes. Only a 60 s-plus freeze crosses to wall, and then one sample is lost each way.

*"A reader that steps BACKWARDS (rollback) - is the axis monotone, does a task age correctly?"*
**Yes.** `TaskClockAxis.Advance:165-166` adds only when `reading > _lastReading`, so a rollback adds
zero and re-anchors at the LOWER value; add-only plus `Volatile.Read/Write` makes `Seconds` monotone
non-decreasing. `SimClockTracker` classifies it `RolledBack` (not `Stale`) and the service logs it
once, rate-limited. The task then ages on the NEW timeline - which means the re-simulated stretch is
served TWICE (once before the rollback, once after). That is the documented "forward movement only"
semantics and is the right call for a deadline that must not fire on a rollback, but it is worth
stating out loud: after a rollback a task ends LATER in scenario time than the order says (Q7).

**M5 - CLOSED.** `InitParser` collects `S.LineType` and `S.PointType`; `VrfC2SimService.cs:1329-1345`
registers every line and point unconditionally, BEFORE any of their creates is enqueued, and
independently of `Vrf:CreateInitLines`/`Points` (the registration-before-create rule is honoured for
all three kinds - areas at `:1271`, ahead of the area create at `:1276`). The init line reports
"R1 RESOLUTION (M5) is independent of those flags: N graphic(s) ...". Unmatched ids are WARNINGS
(`TaskGeometryResolver.cs:140-142` and `:165-168`), and a MapGraphicID naming one of the init's **16
TaskGraphic wrappers** - which `InitParser:163-166` still deliberately skips - falls into exactly
that path: a Warning plus the embedded-Location fallback, and if the task has no embedded Location,
a second Warning saying it "will be executed IN PLACE (R2), which is NOT what the order asked for".
That is the right behaviour. `_names.Requested` is called only for graphics actually created, which
is correct - the prefix-scan hazard exists only for names VR-Forces will report back. Ordering nit
in C11.

### 2.2 New code paths

**`SimClockTracker.cs` (171 lines) - read in full.** Pure state machine over `StallPolicy`'s
predicates; no clock, no bridge, no log. **Thread safety:** `Observe` has no lock and needs none -
the only caller is `SampleTaskClock`, on the tick thread; the only reader of its output,
`MaybeCheckStalls`, is also tick-thread (both go through `TickPhase` in `TickLoop`). The carrier
field `_simClockLast` is non-volatile (C7). **NaN/Inf/negative:** all funnel through
`StallPolicy.UsingSimClock(true, x) = IsFinite(x) && x >= 0.0`, which refuses NaN, +/-Inf and -1;
an unreadable sample skips the value bookkeeping entirely, and `SampleTaskClock` can therefore never
hand a negative or non-finite reading to the axis (`heldOnSim` implies readable; otherwise `wallNow`
is passed). `TaskClockAxis.Advance` re-checks `IsFinite` anyway. **Stale threshold and units:**
`StallPolicy.StaleClockWarnSeconds = 60.0`, compared against WALL seconds
(`wallNowSeconds - wallLastAdvancedSeconds`) while the flatness test is on SIM values - correct, and
the only sound pairing (a flat sim clock cannot measure its own flatness). `ModeSwitchConfirmations
= 3`, counted in samples at 1 s. C6 is the one wrinkle.

**`TaskDispatchPolicy.PredecessorTimeoutSeconds` (`:143-150`).** Correct in itself: the floor is
`max(1, configured)` (a non-finite configured value degrades to 1), a non-finite or non-positive
`predecessorEndSeconds` leaves the floor alone, and a negative or NaN margin is treated as 0 - so no
input can make the window SHORTER than configured. Its defect is what it is not given (A1): it never
sees the predecessor's start delay or the predecessor's own wait. Doc misattached (C1).

**Q1 supersede path (`:2969-2999`).** Timer cancelled: yes - `PushTaskStatus` calls `_timed.Cancel`
FIRST, above the taskee guard (m5's fix), so even an unsendable status disarms the timer. Emitted
ONCE: yes - `TaskStatusPolicy.ShouldEmitAbort` is one-per-task and blocked after a TASKCMPLT, and
the `old.TaskUuid != task.TaskUuid` guard keeps the TerrainProfile re-entry and the deferred-engage
re-record from firing it. Successors: **not abandoned** - B1. `SupersededTaskCode=TASKCMPLT` takes
the other branch and only logs, leaving the timer armed, which is the documented literal reading.

**Q4 path (`:3034-3049`).** `inPlaceWithoutDuration = task.DurationMs <= 0 && kind ==
"hold-in-place"`; `DefaultHoldSeconds` is registered DIRECTLY (533bc2f) so the log cannot claim a
0 s Duration; the WARNING names the invention verbatim. At the end time the task emits TASKCMPLT
through the single emit point and `_sequencer.CompleteTask` releases the successors, whose window is
the 600 s floor - so yes, they dispatch. Gap B3.

**m6 (`:2525-2540` and `:2626-2637`).** Closed. The real "no performing unit" case
(`TryGetEntityGeodetic` failure) now pushes TASKABRT with its own wording, and the unreachable
`Refuse` arm is kept as a LOUD guard (`LogError` plus `NotifyAbandoned` plus TASKABRT) rather than
deleted - a defensible choice, clearly documented.

**m7 (`TaskGeometryResolver.cs:143-158`).** Closed, and better than asked: it logs how many embedded
points were dropped AND the separation from the first resolved point, and escalates to a WARNING
past `EmbeddedDisagreementMeters = 1000`. The separation is equirectangular, which is right for a
1 km threshold check.

**m8 (`:485-500`).** Closed. `IsUsableDurationScale` is checked ONCE in `ExecuteAsync`, logs
`LogError` and falls back to `_durationScale = 1.0`; `_vrf.DurationScale` is never read again.
**No exception on the host build path** - it is a plain `if` inside `ExecuteAsync` before the bridge
starts, and `ScaleOrderMs` independently returns `ms` unchanged for an unusable scale, so the
failure is double-guarded. Confirmed by the build and by the suite's boundary checks (0, negative,
NaN, +/-Inf).

### 2.3 Double or missing terminal codes

**No double terminal code found.** `ReportBuilder.BuildTaskStatusReport` has exactly ONE call site in
the service (`:4682`, inside `PushTaskStatus`) - **nothing bypasses the emit point**, verified by
grep over all sources. The pairings:

| pair | outcome |
|---|---|
| timed TASKCMPLT vs arrival TASKCMPLT | `TimedCompletionPolicy.Advance` removes-then-reports with an atomic `TryRemove(KeyValuePair)`, and `PushTaskStatus` cancels the timer before anything else; if both still reach the emit point, `ShouldEmitComplete` emits one. |
| timed TASKCMPLT vs vendor `success=false` TASKABRT | whichever is first wins; `ShouldEmitAbort` refuses an abort after a completion, and a TASKABRT cancels the timer. |
| supersede TASKABRT vs the old task's armed end time | timer cancelled inside the same `PushTaskStatus`. |
| stall TASKABRT vs a later arrival TASKCMPLT | both emitted, deliberately (the C16 ruling). |
| supersede TASKABRT vs the old task's LATE VENDOR completion | **mis-attributed to the NEW task - B2.** Not a double code for the old task; a spurious one for the new. |

**Missing codes:** the only dispatch dead end that stays silent is `:2208-2211` (no
`PerformingEntity`), and it has no taskee uuid to address a report to - correct and pre-existing.
Everything else pairs `NotifyAbandoned` with a TASKABRT. A dispatched task with
`TimedCompletion=false` and no arrival evidence still ends in silence, which is the documented
meaning of that setting.

### 2.4 Merge state

`git merge-base --is-ancestor 26efe0c 0c96f50` -> **true**. Pass-1's merge hazards are resolved:

- **x1 resolved correctly**: `TickPhase("MaybeCompleteTimedTasks", _vrf.TimedCompletion,
  MaybeCompleteTimedTasks);` at `VrfC2SimService.cs:735` - guarded, and
  `TickPhase("SampleTaskClock", true, SampleTaskClock)` at `:728` is placed FIRST so the axis is
  advanced before anything reads it.
- **x2/x3**: `MaybeCheckArrivals` and `SynthesizeUnitCompletion(name, type, success)` carry the
  integration signatures; the rulings code adds no conflicting caller.
- **x4 resolved**: `InitLine`/`InitPoint` are parsed AND the registration loops exist (M5).
- Usage string at `Program.cs:112-118` includes `--rulings/`; **all 18 suites exist and pass**
  (verdict lines quoted in sec 0).
- Files changed by both series: `VrfC2SimService.cs`, `Program.cs`, `InFlightTracker.cs`,
  `OrderParser.cs` / `OrderModels.cs` / `OrderParseCheck.cs`, `VrfSettings.cs`,
  `SequencerSelfTest.cs`. All build and all their suites are green.

### 2.5 Self-tests

78 checks, five sections, all pass. **Not vacuous overall**, and pass-1's n8 was acted on: `(d2)` was
rewritten from the un-failable `RefusesForTarget` assertion into a real mapping check (exactly one of
four resolutions names an entity). `(c4)` still compares `ZeroGeometryObservation` to its own
literal, but that is a legitimate wire-text lock.

**The fail-first controls exercise the production classes, not re-implementations:**
`GateOutcome` (`:315-330`) drives the REAL `TaskSequencer` and REAL `TimedCompletionPolicy` on a
shared fake clock; `WalkTaskClock` (`:451-484`) drives the REAL `SimClockTracker`, `TaskClockAxis`
and `TimedCompletionPolicy`. Both controls genuinely fail on the pre-fix behaviour.

**Two gaps:**
1. **`GateOutcome` calls `seq.NotifyDispatched(pred, clock.Now)` BEFORE `WaitForStartAsync`**
   (`:319` vs `:323`), so the predecessor is already dispatched when the gate begins waiting and
   **phase 1 is never exercised**. That is precisely why A1 survives a green suite. Any new M1 test
   must start the gate with the predecessor UNdispatched.
2. `WalkTaskClock` re-implements `SampleTaskClock`'s glue (C8), so the service wiring is uncovered.

**Checks whose expected value is computed by the code under test:** `derivedWindow` (`:164`) and
`demoEnd` (`:188`) come from `TaskDispatchPolicy`, but both are then asserted against LITERAL
constants (`predDuration + margin`; `< configured`), so the checks still have teeth. Nothing else.

### 2.6 Docs

**Sec 7.1 states all four defaults and says they are supervisor defaults pending the user's own
ruling** - Q2 `TaskClock=sim`, Q1 `SupersededTaskCode=TASKABRT`, Q3 accepted as recorded,
Q4 `DefaultHoldSeconds=60`; 7.1b lists the four new keys including
`TaskPredecessorEndMarginSeconds=60`. That part is accurate and honest, including the "what a live
run must prove" list and the explicit "THE CLOCK HAZARDS ARE STILL UNOBSERVED".

**RUNBOOK sec 11 is accurate to the code on three of its four claims** - the TaskClock/StallClock
separation, the three-samples / 60-wall-seconds fallback rule (`ModeSwitchConfirmations = 3`,
`StaleClockWarnSeconds = 60`), and the start-up `DurationScale` validation. **The fourth is false
and actively harmful: "You no longer need to raise `TaskPredecessorTimeoutSeconds` for a long
order"** (A2). Sec 11 also sits above sec 10 (C10).

---

## 3. VERIFIED vs ASSUMED

**Verified in this worktree, by building it, running it, or measuring the data:**
- Build green; all 18 suites exit 0; `--rulings-selftest` prints 78 checks; the 2 CS8632 warnings
  pre-date the branch (`git blame` -> 88488be2).
- 26efe0c is an ancestor of 0c96f50.
- COA-STP1's graph: 42 tasks, 11 serial chains, max depth 3 (d0 11 / d1 11 / d2 10 / d3 10);
  durations 42/42 (32 x 4,800 s, 10 x 7,200 s); one 12,000 s start delay, on T13, whose successor
  is T14. Independently parsed AND cross-checked against `--parse-order`'s own census.
- **A1's numbers are MEASURED, not derived**: a scratchpad probe linking the worktree's unmodified
  `TaskSequencer.cs`, `TaskDispatchPolicy.cs` and `TimedCompletionPolicy.cs` reproduces
  `HandleOrder`'s gate topology and produces 2 of 4 at defaults, 2 of 4 at the Demo overlay,
  3 of 4 at scale 0.05, and 4 of 4 only at `configured = 20000`; T14 skipped at 600, at 7200 and
  in 6 of 7 runs at scale 0.05, dispatched at 20000.
- Only ONE `_bridge.SimTimeSeconds()` call site and only ONE `BuildTaskStatusReport` call site in
  the service.
- Every `NotifyAbandoned` site except the no-taskee one is paired with a TASKABRT.
- `_timed.Cancel` now precedes the taskee guard in `PushTaskStatus` (m5).
- `Vrf:TaskClock`, `TimedCompletion`, `DurationScale`, `TaskPredecessorEndMarginSeconds`,
  `SupersededTaskCode`, `DefaultHoldSeconds` appear in NEITHER appsettings file.
- ASCII/CRLF clean over all 19 changed files, with a dirty control proving the instrument (and with
  `grep -P` shown to be UNUSABLE in this shell - it errors on the control, so a `-P` check here
  would have passed a dirty file).
- `SampleTaskClock` and `MaybeCheckStalls` are both tick-thread-only (`TickPhase` in `TickLoop`).

**Assumed / not checked here:**
- **No live VR-Forces run.** Everything is static, offline-executed, or data-derived.
- The probe models `HandleOrder`'s gate topology; it does not run the service, the tick loop, the
  bridge or the reports. Its two inputs (all gates start at order receipt; the window is derived
  from the immediate predecessor only) were read off `VrfC2SimService.cs:2204-2246` and
  `:2270-2276`, quoted above - but the end-to-end "21 of 42" is an extrapolation from the chain
  census, not an observed run.
- Whether `DtVrfRemoteController::simTime()` actually alternates, freezes or rolls back on 5.2 is
  still unproven either way (the branch says so itself).
- STP's discarding of ObservationReports (STP-800) is carried from the record.
- The 0.27x-0.73x and 9x sim ratios used in C3 are from the record, not re-measured here.

---

## 4. QUESTIONS FOR THE USER

**Q1 (pass-1's Q1) - the supervisor default `SupersededTaskCode=TASKABRT` stands.** One follow-on
this branch did not decide: **should a superseded task's SUCCESSORS be abandoned immediately?**
Today they wait out the full derived gate (up to 7,260 s) and are then skipped. Recommendation:
abandon at the supersede point (B1) - the interface has already told STP the task is not being
performed.

**Q5 (NEW, from M4) - does a PAUSED scenario age a task?** R4's rule as written says no. The M4 fix
says: for 60 seconds. After that the interface cannot tell "the operator paused" from "the back end
died", and it keeps serving on WALL seconds - so a ten-minute coffee break burns 600 s off every
armed Duration, and so does the period before the operator starts the scenario at all. Options:
(a) keep serving (today); (b) HOLD on a stale clock and warn loudly and repeatedly; (c) hold only
when the back end is still reporting (a different predicate than "the clock is flat"). This needs a
ruling before the demo, not after.

**Q6 (NEW, from A1) - what is the interim setting for the demo?** Until A1 is fixed, COA-STP1 at
scale 1.0 needs `Vrf:TaskPredecessorTimeoutSeconds >= 20000` (measured). Write that into
`appsettings.Demo.json` now as a stop-gap, or fix A1 properly first?

**Q7 (NEW, informational) - after a `rollbackToSnapshot` a task serves the re-simulated stretch
twice** and therefore ends later in scenario time than the order says. Correct by the "forward
movement only" rule and certainly better than firing on the rollback - confirm it is what you want.

---

## 5. WHAT TO FIX BEFORE MERGE

1. **A1** - phase 1's window must cover the predecessor's LEAD TIME, or drop phase 1's timeout for a
   predecessor that exists in the order. Add the self-test that starts a gate with its predecessor
   UNDISPATCHED, and a case with a delayed root (T13/T14).
2. **A2** - correct RUNBOOK sec 11 and assessment 7.1b once A1 is fixed (or immediately, if it is
   not).
3. **B1** - `NotifyAbandoned` on the supersede TASKABRT.
4. **B4** - do not derive the long window when `Vrf:TimedCompletion` is false.
5. **B3** - cross-check `DefaultHoldSeconds` against `TaskPredecessorTimeoutSeconds` at start-up.
6. **B7** - distinguish the phase-1 timeout in the log, because live gate 5 is a log check.
7. **B6** - put the six R4 keys in `appsettings.Demo.json` before the demo deployment.

Mergeable as recorded debt: B2, B5, C1-C15.

---

## 6. ADVERSARIAL REVIEW

**Competing hypothesis for A1: "the gates do not all start at order receipt - a successor's
orchestration begins only when its predecessor dispatches, so phase 1 is never the binding
constraint."** *Falsifier checked and found:* `VrfC2SimService.cs:2204-2246` is a single `foreach`
over `order.Tasks` that fires `_ = RunTaskAsync(t, u)` for every task, and the first thing
`RunTaskAsync` awaits (`:2287-2305`) is the gate - the only work before it is logging and a
synchronous `MaterializeUnit`. There is no per-chain scheduling anywhere. **Hypothesis falsified.**

**Second competing hypothesis for A1: "phase 1 will pass anyway, because `pred.Completed` /
`pred.Abandoned` also satisfy the `WhenAny` and something completes the predecessor early."**
*Falsifier checked:* nothing completes T2 before T1 completes - T2's own timer is armed only at T2's
dispatch - and the probe, which runs the REAL `TaskSequencer`, returns `PredecessorTimeout` for T3
in every configuration except `configured >= 20000`. **Falsified by execution, not by reading.**

**Third competing hypothesis: "this is the accepted limitation the class doc already records, so it
is known debt rather than a defect."** *Partly true, and that is exactly why it is MAJOR:* the doc
at `TaskSequencer.cs:55-57` accepts the limitation **on the stated ground that "the real orders
carry single-level chains only"**. COA-STP1 carries four-level chains - measured, 42 tasks, 11
chains, depth 3 - so the premise the acceptance rests on is false for the order the branch was built
for. A limitation accepted on a refuted premise is not accepted.

**Competing hypothesis for "M2/M3/M4 are closed": "the task clock is still partly wall, so M2 only
moved too."** *Checked and rejected:* the only wall `Task.Delay` left in the R4 path is the 200 ms
POLL inside `TaskClockDelayAsync`, whose loop condition is on the axis; the two 1 s cadences are
sampling rates, and C3 quantifies what they cost. The one real wall/sim mixing left is B5, which no
order on disk reaches.

**Symptom I could not fully explain and am NOT filing as resolved:** the `DurationScale=0.05`
T13/T14 case returned **DISPATCHED once and PredecessorTimeout in the other six runs**, with
identical inputs. I believe that is the exact-boundary race it looks like (window 600 s vs a
predecessor dispatching at 600 s), consistent with A1's mechanism, but I did not isolate the
scheduling detail that flips it. It is a falsifier for any claim that the compressed demo profile is
deterministic, and it should be treated as one rather than as noise.

**Symptom still unexplained, not this branch's:** pass-1's doctrine verb-vs-symbol mismatch
(`GFTPAS` x3, `GFTPA`, `GFTPN` drawn in the init while FOLSPT / FOLASS / NTRCOM appear in none of
the 42 `TaskActionCode` values) is untouched here and still open on the STP side.

**What no test can cover here:** M3, M4 and C5 are reactions to reader behaviours nobody has
observed on 5.2; the suite proves the LOGIC is right given the behaviour, not that the behaviour
occurs. A1, by contrast, was fully decidable offline and the suite missed it only because its one
integration test pre-dispatches the predecessor - which is the strongest argument in this review for
spending the next test on the gate TOPOLOGY rather than on more pure functions.

---

## VERDICT LINE

**FIX FIRST** - A1 (M1 is not closed: 21 of 42 COA-STP1 tasks still skipped at every shipped
setting, measured), A2 (the RUNBOOK tells the operator the opposite), then B1, B3, B4, B6, B7.
Everything else on this branch is sound, and it is a real improvement on 5c67d41.
