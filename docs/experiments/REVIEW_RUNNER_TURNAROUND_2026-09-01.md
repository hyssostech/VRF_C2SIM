# REVIEW: runner-turnaround (2026-09-01) - cold adversarial pass

Reviewed: branch `runner-turnaround` @ f8c6168 (base f6c3701), read-only, in the
worktree .claude/worktrees/runner-turnaround. Inputs: docs/RUNNER_TURNAROUND_2026-09-01.md,
docs/RUNBOOK.md 0.5.11, `git diff f6c3701..f8c6168`, the P2c / P3 run directories on
main (runs/20260901T211310Z_run, runs/20260901T221227Z_run), and offline execution of
the tests and of both tool binaries (old = deployed on main, new = built in the
worktree at 19:05:56 local, after the last source edit at 18:54:14). No federation was
joined; VR-Forces was up on the machine (a live probe) and was not touched.

## VERDICT: MERGE WITH FIXES

The non-negotiables hold on every path I could construct from the code: no observer is
ever killed; the stop-file exit is the SAME `bridge.Stop()` / `sdk.Disconnect()` path as
the duration expiry; a fresh ledgered appNo is used; the trace cannot be truncated
before window + trail by anything in the diff. Default behaviour is comparable with the
record (sec 4). The fixes below are one MEDIUM on a failure path and four LOW items;
none blocks the confirming live run, and F1 should land before the runner is relied on
unattended.

Fixes, ranked:

- F1 (MEDIUM) Grace expiry proceeds to StopVrf with an observer possibly still joined.
- F2 (LOW) TASKCMPLT line count includes lines for taskees NOT in the order.
- F3 (LOW) Superseded tasks make the count rule unsatisfiable; document it or count
  per taskee.
- F4 (LOW) ListenReports poll loop: theoretical negative `Task.Delay` race.
- F5 (LOW) The runner relies on a fresh run directory for "stop file does not
  pre-exist"; no active check or delete before Stage 5.

Merge mechanics: main has moved to 72ac6d3 (P3R outcome). `git merge-tree 72ac6d3
f8c6168` reports a content CONFLICT in docs/HANDOFF_2026-09-01_R9_COMPLETE.md (both
sides edited NEXT item 5). Docs-only; resolve by keeping both the P3R text and the
turnaround item (d). Code files merge clean.

## 1. STOP-FILE PATH (item 1) - verified

| check | result | evidence |
|---|---|---|
| Same exit path as duration expiry | YES | WatchRunner.cs:225-241: the stop poll does `break` out of the tick `while`; the code after the loop (`bridge.Stop()`, `[OK] resigned cleanly.`, `return ExitOk`, `finally bridge?.Dispose()`) is unchanged and is the only exit. No new return inside the loop. ListenReports Program.cs:177-193 likewise: `break`, then the unchanged `await sdk.Disconnect()` + `File.WriteAllTextAsync(outPath, ...)`. |
| Poll cannot throw | YES | `File.Exists` never throws (returns false on any IO/permission error). The runner's `WriteAllText` of the stop file is in try/catch (RunC2SimScenario.ps1:2171-2181); failure -> WARN, observers run to cap. |
| Stop written after StopIface + TrailSecs | YES | finally-block order: `$StopIfaceUtc` stamped (2095) -> StopIface -> wait for app exit -> `Get-TraceStopWaitSecs -ReferenceUtc $traceRef` (2166) -> sleep -> touch (2172). Note `$StopIfaceUtc` is taken BEFORE the StopIface tool runs, so the trail is 30 s from the START of StopIface, i.e. ~26 s past the app's actual exit in the record (P2c: StopIface 21:31:06.169, app exit 21:31:09.937). Adequate - see the readable=2 observation below. |
| Flush before exit | YES | WatchVrf writes through `Console.Out` (autoflush) and returns normally; ListenReports writes the capture file synchronously before `return`. |
| Grace expiry never kills | YES | `Complete-Background` (715-735): `WaitForExit(timeout)` then records `still-running` + WARN "NOT killed - it is a joined federate. It will resign on its own timer." No Kill anywhere. The oracle-died check (2197-2203) keys on a non-zero exit code; a null (still-running) code does not trip it - same as before. |
| Pre-existing stop file | exit 2, nothing joined | WatchRunner.cs:79-82 refuses BEFORE `new VrfBridge()`; ListenReports Program.cs:85-87 refuses before `sdk.Connect()`. Reproduced offline: both exit 2 with "ALREADY EXISTS ... Nothing joined." / "Nothing connected." The ledger is advanced at Update-Ledger (1569) before Stage 5 regardless, so the appNo is CLAIMED but never joined - the ledger's existing "claimed per run" semantics, no double-use possible. |
| Can the stop file pre-exist? | Practically no | `$PathStopFile = <RunDir>\observers.stop`; RunDir = runs\<yyyyMMddTHHmmssZ>_run, created with `New-Item -Force` (1565). Two runners starting in the same UTC second would collide on the WHOLE run directory (ledger, manifest) - a pre-existing hazard, not new. There is NO delete or fail-fast check of the stop file before Stage 5 (F5). |

F1 detail. In stop-file mode the runner now waits `-TraceStopGraceSec` (120 s) instead
of cap + 120 s. If an observer fails to see the stop file (bug, path mismatch, hung
tick), teardown proceeds to StopVrf while WatchVrf is still a joined federate, and the
runner exits with only a WARN. The observer still resigns at its cap, so no stale
federate results, but: (a) Stage 1's pre-flight inventory (1277-1313) checks only
vrfLauncher / vrfSimHLA1516e / vrfGui, so the NEXT run would launch with a foreign
observer still in CWIX-2024; (b) the record's guarantee "observers have exited before
StopVrf" is lost on this path. Before the change this path was unreachable in practice.
Fix (either): on `still-running` after the grace in stop-file mode, keep waiting up to
the remaining cap (never kill) before StopVrf; or add WatchVrf / ListenReports to the
Stage 1 inventory and refuse to launch while one is up.

Observation that supports the change: in the P2c trace (runs/20260901T211310Z_run/
watchvrf-trace.csv) every entity's last POS sample is at t=960.9 s and the remaining 254
per-sample summaries read `readable=2` - once the interface goes UNINITIALIZED the
created entities stop being readable. The 8 min 21 s of dead time contained no movement
evidence, and the post-StopIface trail cannot carry any either.

## 2. CAPABILITY PROBE (item 2) - verified

- WatchVrf Program.cs:30-40: `--capabilities` is dispatched as `args[0]` before
  `WatchRunner.Run` is ever called; the only types touched are ToolArgs and the
  strings-only WatchVrfUsage. Empirical: NEW binary prints `capabilities / con-selftest /
  stop-file`, exit 0 in 504 ms, and does NOT print the VR-Link "RDTSCP" banner; the OLD
  binary printed the banner (its `Run` JIT loads the bridge) and exited 2 with "unknown
  or misplaced option(s): --capabilities". Neither joins.
- ListenReports Program.cs:58-67: same, before `TryTakeOptionValue` (73), before any SDK
  object exists. NEW: exit 0 in 42 ms; OLD: exit 2 "unknown option(s): --capabilities".
- Runner: `Invoke-CapabilityProbe` (1208-1239) redirects stdio, `WaitForExit(30 s)`,
  never kills on timeout (records `timed-out`, treated as unsupported).
  `Test-ToolCapability` requires exit 0 AND the exact token. The probe runs with the same
  PATH prefix and cwd the live tool gets (1242-1244), so it answers for the binary as it
  will run.
- Fallback is byte-identical: `$WatchStopArgs = @()` / `$ListenStopArgs = @()` unless
  supported (1768-1769); the argument arrays at 1771 / 1776 are the base's arrays plus an
  empty array. Matches the design note's dry-run record (`3658 1160 2 CWIX-2024` /
  `1160 <reports path>`).
- Ordering: Stage 0b (1196-1275) runs after Stage 0 validation (order file, exe
  presence, Bin64) and before the main `try` (1554), so `$ProbeWatch` / `$ProbeListen`
  are always defined in the `finally`.

## 3. EARLY EXIT -StopWhenComplete (item 3) - correct in the safe direction

Criterion as implemented (RunnerLib.ps1:125-167): every distinct order taskee has >= 1
TASKCMPLT line AND total TASKCMPLT lines >= (Task, PerformingEntity) pair count, held
>= SettleHoldSecs from the poll that first saw it. Default OFF (switch).

- Parser vs real log (RunnerLib.ps1:95): the app's console logger writes the category
  on one line and the message indented on the next; the regex is unanchored at the start
  and matched the real P2c/P3 lines (tests 3.x, re-run here). `task=(none).` accepted.
- Sequential tasks for one taskee: the app has a sequencer (`_sequencer.CompleteTask`,
  VrfC2SimService.cs:1224) so gated successors DO complete and produce a second line -
  the count rule is right for that case. Simultaneous dispatch SUPERSEDES
  (VrfC2SimService.cs:951-955, "the old task will not complete"): only one line ever
  appears, count < taskCount, early exit never fires, window runs to the cap. Safe
  direction (no truncation), but the switch is then inert and the design note ("needs
  two completions") does not say so (F3).
- Fan-out: members are swallowed; ONE unit-level line via SynthesizeUnitCompletion
  (VrfC2SimService.cs:1178-1187). Counted once. Correct.
- TASKCMPLT for a taskee NOT in the order: `$State.lineCount = @($Completions).Count`
  (RunnerLib.ps1:137) counts every line. With one order taskee holding a superseded task
  (1 line) plus one stray line for a unit outside the order, the count rule is satisfied
  by the stray and the window closes with the superseded task uncompleted. Harmless in
  effect (that task never completes anyway) but the rule does not mean what it says.
  Fix: count only lines whose taskee is in `$Taskees` (F2).
- Non-UUID PerformingEntity values would parse from the order but never match the
  36-hex-dash taskee regex -> never fires -> cap. Safe direction.
- Plateau adequacy: in P2c the TSK lines land at trace t=145.2 / 157.1 / 212.5 and the
  last >10 m step of ANY entity is at t=217.3 (company members closing formation ~5 s
  after the unit's TSK). Hold 60 s + trail 30 s = ~45 samples at 2 s against the 4a.1
  "settled" rule of 3 samples. Adequate with margin; the hold is over-measured by up to
  one 5 s poll, never under.
- App death disables the early exit and the window runs out (2029-2033). Zero taskees
  -> WARN at 0b, never fires.
- Manifest: adds `oracle.earlyExit` (enabled/fired/firstSeenUtc/closedUtc/...) and
  `inputs.orderTaskCount/orderTaskees`. No validity FLAG is added or changed by firing.
  No script in scripts/ or tools/ reads run-manifest.json or the trace header
  (adjudication is manual), so no consumer breaks.

## 4. DEFAULTS - is the default path comparable with the record? YES

With no new switches, differences from f6c3701, exhaustively from the diff:

1. Stage 0b: two offline exe launches before Stage 1 (measured 0.5 s total).
2. WatchVrf / ListenReports argument lists gain `--stop-file <runDir>\observers.stop`
   when the deployed binary advertises it (today's deployed binaries do NOT -> WARN,
   duration-only, arguments byte-identical to the record until rebuild + redeploy).
3. Trace header line gains ` stop-file=... (duration is the upper bound)`; trace gains
   one `# STOP requested via stop-file ...` comment line at the end. No consumer parses
   either (`Get-TraceSummaryLine` keys on `# t=`).
4. Trace ends at StopIface + 30 s instead of the cap. The record shows readable=2 for
   the whole difference (sec 1), so no scoreable data is lost.
5. Stage 8b sleep clamp (2017): for RunSecs a multiple of 30 (600 default, 900, 420)
   the loop exits on the same iteration as before - iteration k sleeps min(30,
   ceil(remaining)) = 30 until the last, whose remaining is 30 - k*overhead ~ 29.9 ->
   ceil 30. Record check: P2c observationEnd - orderPushed = 932.9 s = 30 + 900 + 2.9 s
   overhead, reproduced by the new loop. For non-multiples of 30 the window is now
   exactly RunSecs instead of RunSecs + up to 30 s - a change, but no run in the record
   used one.
6. Status line: printed on a 30 s `$nextStatus` cadence instead of every iteration;
   with the 30 s poll this is the same cadence. Cosmetic.
7. Complete-Background waits grace (120 s) instead of cap + 120 in stop-file mode (F1).
8. New manifest fields: `inputs.traceStop`, `inputs.orderTaskCount`,
   `inputs.orderTaskees`, `oracle.earlyExit`, `clocks.traceStopRequestedUtc`. Additive.
9. `Set-StrictMode -Version Latest` in RunnerLib.ps1 is redundant with the runner's own
   (RunC2SimScenario.ps1:314) - no behavioural change.

Nothing else on the default path changed. Window length, order/init handling, appNo
allocation, StopIface -> StopVrf order and the never-kill rule are untouched.

## 5. TESTS - run here, real functions, fault injection reproduces

`pwsh -NoProfile -File tests\RunnerTurnaround.Tests.ps1` from the worktree:
`48 passed, 0 failed`, exit 0 (full output below). The tests dot-source the branch's
scripts\RunnerLib.ps1 (tests line 22), the same file the runner dot-sources
(RunC2SimScenario.ps1:363) - real functions, not copies. Test data are the real R9
order and verbatim lines from the P2c / P3 app logs.

Fault injection reproduced: in a scratch copy of scripts/tests/data, flipping
RunnerLib.ps1:165 `-ge` to `-gt` gives `45 passed, 3 failed`, exit 1, failing exactly
"hold 60 s of 60: closes", "one performer, two tasks, two TASKCMPLT lines ... closes with
hold 0", "settle hold 0: closes on the completing poll".

Gap: nothing offline exercises the runner's Stage 8b / finally wiring around these
helpers (the `break`, the `$appDeathRecorded` guard, the touch). Those are the
confirming run's job, as the design note says.

```
=== 1. observer duration cap (Get-DerivedWatchSecs) ===
  [PASS] defaults + RunSecs 900 -> 1460 (manifest 20260901T211310Z watchSecs)
  [PASS] defaults + RunSecs 420 -> 980 (manifest 20260901T221227Z watchSecs)
  [PASS] defaults + RunSecs 600 -> 1160 (runner default)
=== 2. order parsing (Get-OrderTasks / Get-OrderTaskees) ===
  [PASS] R9 order yields 3 (Task, PerformingEntity) pairs
  [PASS] R9 order yields the 3 known taskee UUIDs
  [PASS] R9 task UUIDs are the a5000000-...-0001/2/3 task ids, not location/route ids
  [PASS] unparseable XML yields an empty array, not a throw
  [PASS] empty text yields an empty array
  [PASS] synthetic: 2 tasks, 1 distinct taskee (two tasks for one performer)
  [PASS] synthetic: deeper UUID not mistaken for the task UUID
=== 3. TASKCMPLT parsing (Get-CompletedTasks) - real interface-log lines ===
  [PASS] P2c log -> 3 completions
  [PASS] P3 log -> 2 completions
  [PASS] P2c completions carry the task uuid without the trailing period
  [PASS] CRLF log text parses the same
  [PASS] task=(none) form is accepted
  [PASS] a TASKCMPLT-looking line for a non-uuid taskee is ignored
  [PASS] empty log -> no completions
=== 4. early-exit decision (Update-CompletionState / Test-EarlyExit) ===
  [PASS] P3 (2/3 taskees): AllComplete is false
  [PASS] P3 (2/3 taskees): ShouldClose is false even after an hour
  [PASS] P3 (2/3 taskees): Missing names the company
  [PASS] no completions yet: nothing seen, allCompleteUtc null
  [PASS] first completion stamps firstSeenUtc for that taskee only
  [PASS] 2 of 3 seen: not all complete, no close
  [PASS] all 3 seen: allCompleteUtc stamped at the poll that first saw it (t+215)
  [PASS] all complete at t+215: AllComplete true, ShouldClose false (hold 0 of 60)
  [PASS] hold 59 s of 60: still open
  [PASS] hold 60 s of 60: closes
  [PASS] a later poll does NOT move allCompleteUtc (hold is measured from the first sighting)
  [PASS] first-seen stamps are never overwritten
  [PASS] zero taskees: never all-complete, never closes
  [PASS] one performer, two tasks, one TASKCMPLT: NOT all complete
  [PASS] one performer, two tasks, two TASKCMPLT lines: all complete, closes with hold 0
  [PASS] settle hold 0: closes on the completing poll
=== 5. trace stop timing (Get-TraceStopWaitSecs) ===
  [PASS] app exited 4 s after StopIface, trail 30 -> wait 26
  [PASS] fractional remainder rounds UP (3.2 s -> 4)
  [PASS] already past the trail -> 0, never negative
  [PASS] trail 0 -> 0
=== 6. capability probe parse (Test-ToolCapability) ===
  [PASS] exit 0 + token present -> supported
  [PASS] exit 0 + token absent -> unsupported
  [PASS] exit 2 (old binary: unknown option) -> unsupported even if stdout had the token
  [PASS] null exit (timed out / could not start) -> unsupported
  [PASS] empty stdout -> unsupported
  [PASS] token match is exact after trim (" stop-file " ok, "stop-files" not)
=== 7. both PowerShell files parse ===
  [PASS] scripts\RunC2SimScenario.ps1 parses with 0 errors
  [PASS] scripts\RunnerLib.ps1 parses with 0 errors
  [PASS] runner declares -StopWhenComplete as a switch
  [PASS] runner declares -SettleHoldSecs default 60
  [PASS] runner declares -TraceStopGraceSec default 120

48 passed, 0 failed
```

## 6. HYGIENE - clean

- Line endings: all 10 touched files are CRLF in the working tree with zero bare LF
  and no BOM; the index is LF for both base and branch (repo has core.autocrlf=true),
  so the convention is preserved.
- ASCII: byte scan of all 10 files = 0 bytes > 0x7F; `rg -n -P "[^\x00-\x7F]"` finds
  nothing after firing on a dirty control (em dash + NBSP) first.
- Both .ps1 parse (test 7), and the runner's `Set-StrictMode Latest` +
  `$ErrorActionPreference = 'Stop'` are respected by the new code (all new variables
  assigned before read; `$ProbeWatch`/`$ProbeListen`/`$stopFileTouched` defined on every
  path into `finally`).
- Style: comment density, `Say-*` reporting, `[ordered]` manifest fragments and the
  Verb-Noun helper names match the existing runner. Tool-side changes follow the
  ToolArgs standard (exit 2 = nothing done) and reuse the pre-existing
  `TryTakeOptionValue`.
- Docs vs code: RUNBOOK 0.5.11 and HANDOFF item 5(d) match the implementation. One
  wording nit: RUNBOOK 0.5.11 item 1 "Measured dead time removed: 8 min 21 s" - the dead
  time is measured, the removal is predicted until the confirming run (the section
  header says PENDING CONFIRMING RUN, so this is acceptable as written).

## 7. Low findings, detail

- F4 ListenReports Program.cs:181-182: `remaining = listenEnd - UtcNow` is computed
  after the `while (UtcNow < listenEnd)` check; if >= 1 ms elapses between the two clock
  reads at the very end, `Task.Delay(negative)` throws ArgumentOutOfRangeException, the
  capture is not written and the tool exits non-zero. Sub-millisecond race, STOMP not
  HLA; fix is `if (remaining <= TimeSpan.Zero) break;` before the delay.
- F5 RunC2SimScenario.ps1:1459: add `if (Test-Path -LiteralPath $PathStopFile) { fail
  fast }` (or Remove-Item) before Stage 5 so the tools' exit-2 refusal is never what
  reports it.
- F3 docs/RUNNER_TURNAROUND_2026-09-01.md sec 3 and RUNBOOK 0.5.11 item 2: state that
  for a taskee with two SIMULTANEOUSLY dispatched tasks the switch never fires (VRF
  supersession), and that it does fire for sequenced (gated) tasks.

## 8. Adversarial review

Competing hypothesis weighed: "the stop file can arrive before the trail and truncate
the plateau". Falsifier checked: the touch is unconditionally after
`Get-TraceStopWaitSecs(StopIfaceUtc + TrailSecs)`; the only way to touch earlier is
`-TrailSecs 0`, which is an operator choice and pre-existing. Not falsified.
Second hypothesis: "an old deployed binary is killed or misfires on `--capabilities`".
Falsifier checked by running the old binaries: exit 2, nothing joined, runner falls back
to byte-identical arguments. Not falsified. Unexplained symptom: none. Not verified
(needs a federation): the live resign on stop-file touch and a live early-exit firing -
the confirming run's (a)-(d) list in the design note sec 4 stands.
