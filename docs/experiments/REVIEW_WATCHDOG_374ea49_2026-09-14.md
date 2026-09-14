# Cold-start review of 374ea49 (Opus, 2026-09-14 ~14:30Z): SAFE TO ARM for G7 attempt 4 with --watch-secs 2000;
# fixes F2-F8/F10 applied in the commit that adds this file ("Runner watchdog: review fixes F2-F8/F10 ...";
# a file cannot carry its own sha); F1 applied to the launch line; F9/F11/F12 no change. Verification: sec 13 of
# docs/experiments/RUNNER_HARDENING_2026-09-14.md. Everything below this line is the reviewer's text, verbatim.

# REVIEW - commit 374ea49 "Runner A2: detached run watchdog + pre-order settle"

Cold-start adversarial review, READ-ONLY, against the files AS OF 374ea49
(`git show 374ea49:<path>`), not the working tree. Reviewer: Opus, 2026-09-14.
Scope: `scripts\RunnerWatchdog.ps1` (new), the stage 6b-w / stage 7d changes in
`scripts\RunC2SimScenario.ps1`, `scripts\RunScenario.sh`, `scripts\StopVrf52.ps1`,
`scripts\RunnerLib.ps1`, RUNBOOK 0.5.14 items 6-7, RUNNER_HARDENING secs 8-12.

## VERDICT

SAFE TO ARM for G7 attempt 4 - the watchdog has no path that tears down a healthy run
under this launch line - BUT CHANGE ONE NUMBER FIRST: `--watch-secs 1800` is BELOW the
derived cap of 2000 that the 240 s hold creates, so the observers can stop before the
observation window does and truncate the trace at its most valuable end. Launch with
`--watch-secs 2000` (or `--watch-secs 0` to let the runner derive it). Do NOT use
`-NoWatchdog`.

---

## 1. THE ARITHMETIC FOR THE PROPOSED LAUNCH LINE

Inputs: `--run-secs 1200 --watch-secs 1800 --pre-order-settle 240`, everything else default.

Defaults in force (`scripts\RunC2SimScenario.ps1`):

| name | value | line |
|------|-------|------|
| PreRollSecs | 20 | :337 |
| AppJoinTimeoutSec | 180 | :338 |
| OracleGateTimeoutSec | 180 | :339 |
| InitDispatchWaitSec | 120 | :340 |
| PushOrderListenSec | 30 | :341 |
| TrailSecs | 30 | :342 |
| AppExitTimeoutSec | 120 | :347 |
| StopVrfTimeoutSec | 120 | :348 |
| StageTimeoutSec | 600 | :375 |
| TraceStopGraceSec | 120 | :396 |
| ObserverCapMarginSecs | 30 | :646 |

**Derived observer cap** (`scripts\RunnerLib.ps1:32-33`, called at :2181-2183):

    20 + 180 + 120 + 180 + 30 + 1200 + 30 = 1760
    + PreOrderSettleSecs 240 (:2189)      = 2000   <- $DerivedWatchSecs

**Effective observer cap** (:2190): `-WatchSecs 1800` is explicit and > 0, so it WINS:

    $EffWatchSecs = 1800   (NOT 2000)

The WARN at :2199-2201 therefore FIRES (240 > 0 AND 1800 > 0 AND 1800 < 2000). It is a
flag in the manifest and a line in the log; nothing adjusts, nothing stops. See MAJOR-1.

**Watchdog -MaxSec** (:2750):

    min(86400, 1800 + 120 + 120 + 120 + 600) = 2760 s = 46.0 min      -PollSec 5

**Can exit 4 (MaxSec expired, runner alive) fire before the run's own end?** No.
Measured from the arming point (stage 6b-w, :2760), the LATEST the observation window can
close, with every stage taking its whole budget:

    AppJoin 180 + InitDispatch 120 + OracleGate 180 + settle 240 + PushOrder 30 + run 1200
      = 1950 s        (2550 s if PushOrder consumes its full Invoke-External budget,
                       PushOrderListenSec + StageTimeoutSec = 630, :3012)

Both are inside 2760. Remaining slack for teardown: 810 s (or 210 s in the PushOrder-stalled
case). Normal teardown costs StopIface (seconds) + app exit (<= 120, :3244) + trail 30 +
observer grace (<= 120, :3307) + StopVrf (<= 120) + two log copies + inventory, i.e. roughly
300-400 s, which fits. Expected total from arming to runner exit on a healthy run:
~100-180 s pre-order actual + 240 + 30 + <= 1200 + ~200 teardown = 1770-1850 s, about 900 s
inside MaxSec. Only a pathological teardown (StopIface stalling to its 600 s stage timeout at
:3217, or StopVrf to 120+600 at :3338) overruns MaxSec, and that produces exit 4 with NOTHING
torn down - safe, but the backstop is gone for the rest of that teardown.

**Does the explicit -WatchSecs truncate this run?** Let P = actual seconds from observer start
(stage 5, :2643) to PushOrder. The trace ends at observer_start + 1800. The window plus trail
ends at P + 1200 + 30. Truncation iff P > 570 s. G7 attempt 3 (20260914T130439Z) reached the
nav-area rows at wall ~201 s, so P is expected around 200-260 s and there is ~310 s of margin -
but that margin is unbudgeted and is consumed by exactly the things that go wrong first (a slow
app join, a slow AtInit creation of the whole init, a slow oracle gate). 2000 costs nothing.

---

## 2. FINDINGS

| # | Sev | Where | Concrete failure | Fix |
|---|-----|-------|------------------|-----|
| 1 | MAJOR | RunC2SimScenario.ps1:2190, :2199-2201; RunScenario.sh:167 | `--watch-secs 1800` + `--pre-order-settle 240`: EffWatchSecs = 1800 while the covered window needs up to 2000. If the pre-order stages take more than 570 s of wall (slow app join / init dispatch / oracle gate), WatchVrf and ListenReports hit their cap and resign while the observation window is still open. The trace ends early with NO error - only a WARN flag - and the lost seconds are the END of the movement window, the part G7 exists to read. | Launch with `--watch-secs 2000`. `--watch-secs 0` also works (`-WatchSecs 0` means "derive", :2190, and nothing validates a lower bound) - note the commit message's advice to "drop it" is not reachable through the wrapper, which always passes `-WatchSecs` (RunScenario.sh:167). |
| 2 | MAJOR (low likelihood) | RunnerWatchdog.ps1:172-174 vs :180 and :190-192 | The comment says that if the handle cannot be cached "the loop falls back to Get-Process + StartTime". The CODE does not do that: :180 logs a WARN and leaves `$runnerProc` set, and `Test-RunnerAlive` still prefers `$Proc.HasExited` (:190-191). With no cached handle, .NET's `Process.HasExited` re-opens the process on each call and treats ANY failure to open it as "exited" (`UpdateHasExited`: `handle.IsInvalid -> _exited = true`, and `_exited` is sticky). So in exactly the state the comment claims is handled, one transient OpenProcess failure makes the watchdog declare a LIVE runner dead, claim `runner.watchdog-ran` and run StopIface + StopVrf52 against a healthy run. The precondition (handle-cache failure for a same-user child) is improbable and IS logged, but the consequence is the one thing this design promises never to do. | In the catch at :180 set `$runnerProc = $null` so the documented fallback is what actually runs; and require TWO consecutive dead observations before breaking the loop (`if (-not $alive) { Start-Sleep 2; $alive = Test-RunnerAlive ...; if (-not $alive) { break } }`). |
| 3 | MINOR | RunnerWatchdog.ps1:226-260 vs RunScenario.sh:240-265 | The two backstops OVERLAP; they are not sequential. The commit and RUNNER_HARDENING sec 10.4 describe "the wrapper first, the watchdog ~5-7 s later", but the wrapper's StopIface can run 5-30 s and its StopVrf52 up to 120 s (20 s of that is the front-end grace, StopVrf52.ps1:124-128), so the watchdog's StopIface starts INSIDE the wrapper's StopVrf52. Result: two concurrent drives of the C2SIM server to UNINITIALIZED, two CloseMainWindow on vrfGui and two no-/F taskkill on vrfSimHLA1516e. Nothing is force-killed, so no stale federate; but the "benign, idempotent" claim was tested with the watchdog ALONE (RUNNER_HARDENING sec 12.1), never with both running at once. | The one-liner sec 10.4 already recommends: have RunScenario.sh claim `runner.watchdog-ran` atomically (`set -C; : > "$RUNDIR/runner.watchdog-ran"`) before its own teardown and stand down if the claim fails. Until then, read a `StopIface exit 1` + watchdog exit 3 pair on a killed run as ONE teardown, not two failures. |
| 4 | MINOR | RunnerWatchdog.ps1 (process topology); RUNBOOK 0.5.14 item 5 | The watchdog is a direct CHILD of the runner, so a tree kill (`taskkill /T`, or a `Stop-Process` sweep whose CommandLine filter also matches `pwsh ... -File ...\scripts\RunnerWatchdog.ps1 -RunnerPid ... -RunDir ...\runs\<id>_run`) removes it along with the runner - and the hypothesised G6 killer (RUNNER_HARDENING sec 8 item 2) is precisely a CommandLine-filtered sweep. I measured `IsProcessInJob` = False for a pwsh started from this bash, so job-object KILL_ON_JOB_CLOSE is EXCLUDED here; tree kills and pattern sweeps are not. RUNBOOK 0.5.14 item 5 names only `runner.launched`'s pid as the pid a sweep must exclude, though item 6 writes `watchdog.pid` two paragraphs later. | Add `<RunDir>\watchdog.pid` to the item-5 exclusion rule, next to `runner.launched`. |
| 5 | MINOR | RunC2SimScenario.ps1:2788-2812 | The runner never checks that the watchdog SURVIVED arming. It records the pid (:2800-2801) and the manifest block (:2802-2809) and moves on. A watchdog that refuses at validation (exit 2 in ~50 ms - the commit's own test (c)) leaves the manifest saying "armed" and the run silently unprotected. | After Start-External: `Start-Sleep -Milliseconds 750; if ($WatchdogProc.HasExited) { Add-Flag 'WARN' (...exit code..., runner-watchdog.log path) }`. |
| 6 | MINOR | RunC2SimScenario.ps1:2544 (Stage 3) vs :2760 (Stage 6b-w); RUNNER_HARDENING sec 8 item 1 | The unprotected window is NOT "sub-second". `runner.launched` is written at Stage 3, the instant VR-Forces is this run's to stop; the watchdog is armed only after Stage 6b. Between them run stage 3b settle, stage 4 oracle pre-check, stage 5 observers, stage 5b pre-roll (20 s), stage 6 PushInit and stage 6b - tens of seconds to a couple of minutes in which a runner death leaves VR-Forces up (and after 6b a joined interface) with ONLY the wrapper as backstop. A leftover back-end HARD-BLOCKS the next launch. | Arm the watchdog immediately after `runner.launched` is written (Stage 3). Nothing in RunnerWatchdog.ps1 needs the interface to exist; it only needs the marker, which is written first. |
| 7 | NOTE | RunC2SimScenario.ps1:2962-2997 | Stage 7d is a BLIND sleep: 240 s with no liveness check on the back-end, the interface or the observers. Stage 8b polls `$BackendPid` for exactly this reason (a 5.2 sim can die mid-run - DI-Guy, 0xC0000005). If the sim or WatchVrf dies during the hold, the runner sleeps through it and pushes the order into a corpse, and the failure then surfaces later and for the wrong reason. | Poll `$BackendPid` / `$AppProc` (and `$WatchProc`) inside the 30 s status loop at :2985-2990 and `Stop-Runner 3` on a death. |
| 8 | NOTE | RunC2SimScenario.ps1:3334 | The runner's OWN teardown starts StopVrf with bare `pwsh`, which on this machine is the 32-BIT build (RUNBOOK 0.5.14 item 1 - stated in this same commit). Pre-existing, harmless for StopVrf52, and the watchdog gets it right (`Join-Path $PSHOME 'pwsh.exe'`, RunnerWatchdog.ps1 :131 region) - but it contradicts the rule. | `-File (Join-Path $PSHOME 'pwsh.exe')`. |
| 9 | NOTE | RunC2SimScenario.ps1:973-990, :3308-3313 | The RunnerWatchdog stage stays `outcome = 'started-background'` with `exitCode = null` in run-manifest.json forever: `Complete-Background` is called only for WatchVrf-trace and ListenReports. Correct by design (the watchdog outlives the runner) and the stage Note says so, but any scorer that treats an uncompleted background stage as a defect will misread every run from now on. | Leave as is; if a scorer is written, exclude `RunnerWatchdog` by name. |
| 10 | NOTE | RunScenario.sh:240 | The wrapper's backstop picks the run directory with `ls -1dt runs/*_run` piped to `head -1` - the NEWEST directory, not the one it just launched. The watchdog is told `-RunDir` explicitly and is the safer of the two. Second-order: the watchdog CREATES `runner.watchdog-ran` inside the run dir, which updates that directory's mtime, so its own claim can flip the wrapper's `-t` ordering if another run dir exists. | Have the wrapper capture the run directory from the runner's output or from a fixed pointer file instead of by mtime. |
| 11 | NOTE | RunScenario.sh:195-203; SampleThreads.ps1:58-67 | `--sample-threads` runs the sampler with `-MaxSec WATCH_SECS+100` = 1900 s from ITS own start, detached with `< /dev/null`, no `-StopFile`. It exits when vrfSimHLA1516e is gone (:64-67), so it ends with StopVrf and cannot outlive the run by more than a poll. No hazard; recorded because the question asked. | none |
| 12 | NOTE | tools/ is untracked in git; RunC2SimScenario.ps1:1541-1544 | A run launched from a WORKTREE has no `tools\StopIface\bin\Release\net10.0\StopIface.exe`, and the watchdog resolves that path itself (`Split-Path -Parent $PSScriptRoot`). Stage 0 hard-fails first (:1543), so this cannot silently disarm the watchdog - but it means the run MUST be launched from the main checkout. | none; launch from the main checkout. |

---

## 3. THE PRIORITY QUESTIONS, ANSWERED

### Q1. Any path in which the watchdog tears down a HEALTHY run?

**Runner PID reuse.** Closed on the primary path. :179-180 takes a `Get-Process` object and
touches `.Handle`, which makes .NET open and CACHE a process handle for the object's lifetime;
Windows does not recycle a PID while any handle to it is open, so between the runner's exit and
the watchdog's own exit the number cannot be re-issued. The residual is finding 2: when the
handle cache FAILS the code does not actually fall back as documented.

**Transient "dead" reads.** The death decision is made on ONE observation (:211-212), with no
confirmation poll. Three ways a live runner reads as dead: (a) the .NET `HasExited` behaviour in
finding 2; (b) `Get-Process -Id ... -ErrorAction SilentlyContinue` (:193) returning `$null` for
any reason other than death; (c) `Get-Process` failing at startup (:179) while the runner IS
alive, which logs the misleading "runner pid N is ALREADY GONE at watchdog start" and then
continues the loop with BOTH `$runnerProc` and `$runnerStart` null, i.e. with the PID-reuse guard
fully absent for the rest of the run. (c) fails in the SAFE direction (a reused PID reads as
alive, so the watchdog never acts and exits 4), but it degrades silently.

**32/64-bit mismatch.** Not a hazard. The runner starts the watchdog with `Join-Path $PSHOME
'pwsh.exe'` (:2765), which is its own 64-bit host, and `Get-Process` / `HasExited` are
bitness-agnostic.

**Wrong PID (bash instead of the pwsh runner).** Not possible. The runner passes `[string]$PID`
(:2769), its OWN pid, and the same number is the content of `runner.launched` (:2544); the
watchdog refuses with exit 2 if the two disagree (:160-164).

**The exit/marker ordering - the question that matters most.** There is NO window. The runner
writes `runner.teardown-ran` at :3483-3487, which is the last statement BEFORE `exit $RunnerExit`
(:3488) and AFTER the whole teardown (StopIface :3213, app exit :3244, stop file :3291, StopVrf
:3334, log capture :3361, inventory :3385). The write therefore completes while the process is
still alive, and the watchdog can only observe "gone" after the process has exited, i.e. strictly
after the write. The extra `Start-Sleep -Seconds 2` at :226 is belt-and-braces for filesystem
visibility only. Ordering is sound.

The residual on that path is the one sec 10.4 already admits: the marker write is
`try { } catch { }` best-effort, so a COMPLETED teardown whose marker could not be written looks
exactly like a death and produces a duplicate teardown. Everything the watchdog then does is a
graceful request, so the cost is a confusing log (StopIface exit 1 -> watchdog exit 3), not a
stale federate.

**Marker race with the wrapper.** The `CreateNew` claim (:246-252) is genuinely atomic and does
protect watchdog-against-watchdog. It does NOT protect watchdog-against-wrapper, because the
wrapper neither reads nor writes that marker (RunScenario.sh:241) - see finding 3. Ordering: the
wrapper fires the instant bash reaps the runner, the watchdog at death + PollSec + 2 s, so the
wrapper is always first to START; they then OVERLAP rather than run in sequence.

**Net answer.** Under the proposed launch line, with the handle cache succeeding (the log records
whether it did: "watching runner pid N (started ...)" vs the WARN at :180), there is no path in
which a healthy run is torn down. Findings 2 and 3 are the two ways that statement could fail,
and both are one-line fixes.

### Q2. -MaxSec derivation

Answered in full in sec 1. Summary: derived cap 2000, effective cap 1800 (explicit wins), watchdog
`-MaxSec` 2760 s. Exit 4 cannot fire before the end of the observation window even with every
pre-order stage at its full budget (worst 1950 s, or 2550 s if PushOrder itself stalls; both
< 2760). It CAN fire during a pathological teardown; that is safe by construction (nothing torn
down) but ends the backstop. And NO, 1800 is NOT >= the derived 2000 - the runner's WARN fires and
the trace can be truncated at the end. Raise it.

### Q3. Stage 7d placement

- **After the init and after entity creation:** yes. Order of stages: 6 PushInit (:2682) -> 6b app
  (:2708) -> 6b-w watchdog (:2760) -> 6c join (:2818) -> 6d "init dispatched: N units" (:2861) ->
  7 oracle gate, which requires a POS line with a REAL lat/lon (:2889) -> **7d hold (:2962)** ->
  8 PushOrder (:3001). With `Vrf__CreationPolicy=AtInit` the entities are created at init dispatch,
  two stages before the hold, and the oracle gate proves at least one of them is real and
  positioned before the hold starts. That is exactly the intent (let the lazily loaded nav area
  arrive after placement).
- **Observers and position reports keep running:** yes. WatchVrf and ListenReports start at stage 5
  (:2643) and run to their own cap; nothing in 7d touches them. `Vrf__PositionReportSeconds=10` is
  app configuration, so periodic reports continue through the hold. The only way the hold costs
  trace is finding 1 (the cap being below the derived value).
- **-StopWhenComplete cannot fire during the hold:** correct. All of the early-exit bookkeeping and
  its evaluation live in stage 8b (:3038 onward), after PushOrder; at 7d no order has been pushed
  and no TASKCMPLT can exist.
- **The 30 s status line:** `Say-Info` is `Write-Host` (:474), which the host renders to the
  runner's stdout; RunScenario.sh:207 redirects the runner's stdout AND stderr to a FILE with stdin
  from /dev/null and the header (:21-26) forbids a pipe or a tee. So the status line lands in
  `runs/launch52/RunScenario-<stamp>.log` and no pipe handle exists to be inherited. Confirmed.
- One implementation nit: the loop at :2985-2990 prints BEFORE sleeping `min(30, remaining)`, so a
  240 s hold prints 8 lines (240, 210, ... 30) and exits cleanly. `$settleStart` is stamped UTC and
  `$settleEnd` is local-clock based, but both comparisons are local-vs-local, so the hold is
  exactly 240 s. Correct.

### Q4. Detachment claims

- **Own console:** the runner passes `-NewConsole` (:2792), which makes Start-External use
  `$sp.WindowStyle = 'Hidden'` INSTEAD of `-NoNewWindow` (:966), sending PowerShell's own
  CreateProcess path down its CREATE_NEW_CONSOLE + STARTF_USESHOWWINDOW / SW_HIDE branch. This was
  MEASURED on this machine with `GetConsoleProcessList` (RUNNER_HARDENING sec 10.2:
  `-NoNewWindow` child -> 4 pids including the parent; `-WindowStyle Hidden` child -> 1 pid, parent
  NOT in the list). That is conclusive for Ctrl+C / Ctrl+Break / terminal-close: the watchdog does
  not receive the runner's console events.
- **Do the redirected stdout/stderr survive the parent's death?** Yes. PowerShell's Start-Process
  takes FILE PATHS for redirection and opens the files itself, passing the file HANDLES in
  STARTUPINFO; the child owns its duplicates for its own lifetime and a file handle has no
  writer-side EOF semantics, so the parent exiting changes nothing. (Contrast the G6 defect, which
  was the opposite direction: a child inheriting the parent's PIPE and holding it open.)
- **Job objects:** measured rather than assumed. `IsProcessInJob(GetCurrentProcess(), NULL)`
  returned **False** for a 64-bit pwsh started from this bash, so there is no job object here and
  therefore no KILL_ON_JOB_CLOSE to take the watchdog down with the shell. CREATE_NEW_CONSOLE would
  NOT have saved it from a job; the measurement is what closes that question. What is NOT closed is
  a tree kill or a CommandLine-pattern sweep - finding 4.
- **Can the wrapper's backstop kill the watchdog, or vice versa?** No, in both directions. The
  wrapper's backstop runs StopIface, StopVrf52 and `touch observers.stop` (RunScenario.sh:246-261)
  and kills no process. StopVrf52 acts only on the NAMES `vrfGui`, `vrfSimHLA1516e`, `vrfLauncher`
  (StopVrf52.ps1:64-66, :114-121, :140-147) - never pwsh, never bash. The watchdog likewise kills
  nothing (`Invoke-Step` explicitly LEAVES an overrunning child running). Neither can touch the
  other.

### Q5. Teardown order and safety

Watchdog order is StopIface -> StopVrf(52) -> observers.stop -> report-only inventory, the runner's
own order. Verified against `StopVrf52.ps1` at this commit:

- Validates arguments first and exits 2 (:58-62), before touching anything.
- Inventories vrfGui / vrfSimHLA1516e / vrfLauncher and the three RTI names, and exits 0 if none of
  the three VR-Forces processes is present (:88-98).
- `CloseMainWindow()` (WM_CLOSE) on vrfGui and vrfLauncher (:114-121), then a grace, then
  `taskkill /PID <backend>` **without /F** (:140-147) - a close REQUEST, not a termination.
- `/F` appears nowhere on any path. `rtiAssistant` / `rtiexec` / `rtiForwarder` are inventoried and
  explicitly preserved (:67, :92-94, :161-163).
- Still-running is exit 3 with nothing killed (:168-169).
- Selection is BY PROCESS NAME, so it neither needs nor has a self-PID or watchdog-PID exclusion:
  no pwsh, no bash and no StopIface process can match. Two consequences worth stating: (a) it is
  structurally incapable of killing its caller; (b) it would also stop a FOREIGN VR-Forces started
  after the runner died - inherent to the design and shared with the wrapper, bounded by the
  `runner.launched` foreign-session guard only for the case where nothing was launched at all.

The watchdog's closing inventory is REPORT ONLY and prints the RTI processes as "PRESERVED
(correct)". Nothing is stopped there.

### Q6. Anything else material

- **Exit codes.** 0 / 2 / 3 / 4 / 5 as documented, and the codes match the paths that return them.
  Note that exit 3 is also what a benign DUPLICATE teardown produces (StopIface exit 1 against an
  already-UNINITIALIZED server), so exit 3 is not by itself evidence of a problem -
  RUNNER_HARDENING sec 10.4 says so; nobody reading only the exit code will know that.
- **The "NOT FOUND" path in Invoke-Step** returns `$null` and the step is skipped, so a missing
  StopIface.exe would silently mean the interface is never asked to resign. Closed upstream: Stage 0
  hard-fails if StopIface.exe is absent (:1541-1544), and the watchdog resolves the same path from
  the same repo root (`$ToolsDir = Join-Path $RepoRoot 'tools'`, :508; `$ExeStopIface`, :578).
- **Ledger / manifest.** `artifacts.watchdog` carries pid / pidFile / stdout / stderr / log / maxSec
  / pollSec (:2802-2809), and `inputs.preOrderSettleSecs` plus `clocks.preOrderSettle*Utc` are
  recorded (:2194, :2992-2993). What is NOT recorded: the watchdog's own exit code (it cannot be -
  it outlives the runner) and the fact that `inputs.watchSecs` (1800) is below `watchSecsDerived`
  (2000); the latter is only in the flags array.
- **`-NoWatchdog` and a missing script both WARN and continue** (:2751-2758) - correct, a backstop
  must never fail a healthy run. Same for a Start-Process failure (:2789-2798).
- **Evidence locations for the post-run read:** `<RunDir>\runner-watchdog.log` (fixed name, appended
  by the watchdog itself), `watchdog.stdout.log`, `watchdog.stderr.log`, `watchdog.pid`,
  `runner.launched`, `runner.teardown-ran`, and - only if it acted - `runner.watchdog-ran` with the
  claiming pid and timestamp inside it.

---

## 4. RECOMMENDED LAUNCH LINE (one change)

    bash scripts/RunScenario.sh --profile 5.2 --no-gui \
      --scenario R9_Mojave_Empty_52_NavAO --init ... --order ... \
      --run-secs 1200 --watch-secs 2000 --pre-order-settle 240 \
      --backend-notify 3 --object-console 4 --member-console 4 --position-report 10 \
      --stop-when-complete --sample-threads --env Vrf__CreationPolicy=AtInit ...

With `--watch-secs 2000` the runner's WARN at :2199 does not fire, the observers cover the whole
window, and the watchdog's `-MaxSec` becomes 2000 + 120 + 120 + 120 + 600 = 2960 s (49.3 min),
which only widens the already-sufficient margin. Everything else in the line is sound.

After the run, read `<RunDir>\runner-watchdog.log` even on a clean exit: on a healthy run it must
end with "runner.teardown-ran is present: the runner completed its own teardown. Standing down
without touching anything." and exit 0. Anything else on a healthy run is a defect in this design,
and this is its first live use.

## 5. WHAT I DID NOT VERIFY

- The CREATE_NEW_CONSOLE behaviour of PowerShell's Start-Process was NOT re-measured here; I relied
  on the GetConsoleProcessList table in RUNNER_HARDENING sec 10.2, which is a direct measurement on
  this machine and is conclusive as recorded.
- No live run, no VR-Forces process, nothing launched or stopped. The only thing executed was a
  read-only `IsProcessInJob` / `QueryInformationJobObject` probe on a throwaway pwsh.
- Concurrent wrapper + watchdog teardown (finding 3) is unmeasured by anyone, here or in the commit.
