# RUNNER_HARDENING 2026-09-14 - the fixes for the G6 runner death

Tier: STANDARD (mechanical changes against an already-adjudicated diagnosis, verified by
tests). The diagnosis itself is NOT re-argued here: it is the HEAVY investigation of
`runs\20260914T002716Z_run` (G6), whose findings this document implements. Nothing in this
file is a new cause claim.

WHAT THE INVESTIGATION ESTABLISHED, in one paragraph. The G6 runner pwsh was TERMINATED
from outside at 00:33:02.6Z, t+127 s into its 900 s observation window. `runner exit: 127`
on this MSYS bash does not mean "command not found": it means the child exited with a
high-bit (NTSTATUS-shaped) Windows exit code MSYS does not map to a signal, and of those,
only `0xFFFFFFFF` (-1) - what `TerminateProcess(handle, -1)` writes, i.e. `Process.Kill()`
/ `Stop-Process` - is silent; the other three raise a WER event and print to stderr, and
neither happened. `TerminateProcess` cannot be intercepted, so the runner's `finally`
teardown never ran and VR-Forces plus the interface stayed joined for nine hours
(`vrfc2simapp.log` reached 5.89 GB). Two independent defects rode along: the status line
printed `trace: (no samples)` for the whole run because it read a fixed 256 KB tail of a
trace whose `# t=` summaries are ~1.6 MB apart, and the loop re-read the entire app log
every 5 s inside a 32-BIT pwsh (bare `pwsh` on this machine's PATH is the 32-bit build).
The capture itself was complete; the runner's death cost no evidence, only the teardown.

---

## 1. What changed

| # | Change | Where |
|---|--------|-------|
| 1 | 64-bit host GATE, exit 2 before anything is allocated | `scripts\RunC2SimScenario.ps1:467` |
| 2 | `runner.launched` marker (contains the runner's PID) | `scripts\RunC2SimScenario.ps1:2469` |
| 3 | `runner.teardown-ran` marker, last statement of the `finally` | `scripts\RunC2SimScenario.ps1:3291` |
| 4 | `Get-LastLineWithPrefix` - backward 1 MB block scan | `scripts\RunC2SimScenario.ps1:1178` |
| 5 | status line uses it instead of `Read-LiveTail` + `Get-TraceSummaryLine` | `scripts\RunC2SimScenario.ps1:2917` |
| 6 | `Read-LiveDelta` - incremental reader with a per-key offset | `scripts\RunC2SimScenario.ps1:1020-1021` |
| 7 | observation loop reads only NEW app-log bytes | `scripts\RunC2SimScenario.ps1:2931` |
| 8 | `$completionLinesAll` running total for the status message | `scripts\RunC2SimScenario.ps1:2888` |
| 9 | condition (4) (report evidence) throttled to once per 30 s | `scripts\RunC2SimScenario.ps1:2941-2942` |
| 10 | `lineCount` becomes a RUNNING TOTAL | `scripts\RunnerLib.ps1:155` |
| 11 | ALL-COMPLETE asked of the accumulated set, not of this call's input | `scripts\RunnerLib.ps1:162` |
| 12 | the launch wrapper: 64-bit pinned, stdout to a FILE, teardown backstop, 127 legend | `scripts\RunScenario.sh` (new, 269 lines) |
| 13 | the operating procedure | `docs\RUNBOOK.md` sec 0.5.14 (:741) |

`Read-LiveTail` is now unreferenced (its only caller was the status line). It is LEFT IN
PLACE deliberately - it is a correct, cheap primitive and removing it is a separate,
unrelated edit. `Get-TraceSummaryLine` still has four callers (stages 2 and 7).

---

## 2. The 64-bit gate

`scripts\RunC2SimScenario.ps1:467` refuses a 32-bit host with exit 2 (the documented
"aborted at validation; nothing was launched" code) and names the 64-bit path. It is placed
immediately after the `Say-*` helpers, which is as early as it can be and still print - the
investigation's suggested spot (:409-410) is before `Say-Fail` exists.

The gate is UNCONDITIONAL: it applies to `-DryRun` too. A dry run has no observation loop
and is not itself at risk, but an exemption is one more rule to remember, and the point of
the change is that the 2026-09-07 mitigation was a procedure and procedures lapse.

`scripts\RunScenario.sh` carries the same gate one layer out: it pins
`C:\Program Files\PowerShell\7\pwsh.exe` by full path and verifies `Is64BitProcess` before
launching, so the refusal happens before a run directory or an appNumber is spent.

MEASURED, this machine, 2026-09-14:

```
$ which pwsh
/c/Program Files (x86)/PowerShell/7/pwsh
$ pwsh -NoProfile -Command '[Environment]::Is64BitProcess'
False
$ pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/RunC2SimScenario.ps1 -DryRun
  [FAIL] this runner is hosted in a 32-BIT PowerShell (PSHOME C:\Program Files (x86)\PowerShell\7). ...
  [FAIL]   Relaunch with the 64-bit build, BY FULL PATH - bare "pwsh" is the 32-bit one on this machine:
  [FAIL]       "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File scripts\RunC2SimScenario.ps1 ...
  [FAIL]   scripts\RunScenario.sh pins that path (and redirects stdout to a file) for you. NOTHING was launched.
exit=2
```

---

## 3. The teardown backstop, and why it is not a `finally`

**An in-process try/finally is not a candidate fix. It is the thing that already failed.**
`RunC2SimScenario.ps1` has had a correct teardown `finally` covering the whole observation
window all along; no `finally`, `trap`, `Register-EngineEvent PowerShell.Exiting` or
`AppDomain.ProcessExit` handler runs when a process is terminated. The backstop is therefore
in the WRAPPER, and the runner's only job is to say what state it reached. Two markers:

```
<RunDir>\runner.launched       written the instant VR-Forces became this run's to stop
                               (beside $VrfLaunched = $true). CONTENTS = the runner's PID.
<RunDir>\runner.teardown-ran   written as the LAST statement of the finally, so its presence
                               means the whole teardown ran.
```

Wrapper rule (`scripts\RunScenario.sh`, sec `[BACKSTOP]`): **launched AND NOT
teardown-ran** => StopIface (`<restUrl> <stompUrl> --yes`; the `--yes` flag and the two
mandatory positionals were read out of `tools/StopIface/Program.cs`), then
`scripts\StopVrf52.ps1` (or `StopVrf.ps1` off-profile), then `touch observers.stop`, then
print what is still up. Nothing is force-killed there either, and rtiexec / rtiForwarder /
rtiAssistant are never touched.

WHY `runner.launched` EXISTS AT ALL (this is an addition to the investigation's A1, not a
restatement of it). A1 as proposed keys only on the absence of `runner.teardown-ran`. But
the runner has validation aborts that happen AFTER the run directory is created and BEFORE
anything is launched (`RunC2SimScenario.ps1:1933`, `:1950`), and one of those aborts is the
Stage 1 refusal to launch while VR-Forces is already up. Under the marker-absent rule alone,
that abort would make the wrapper tear down a session the run never owned - which is the
project's single most important rule, inverted. `runner.launched` closes that: absent means
"this run launched nothing", and the wrapper then touches nothing.

RESIDUAL: there is a sub-second window between LaunchVrf returning and the marker being
written in which a kill would leave VR-Forces up with no marker. Accepted and stated.

VERIFIED, 2026-09-14, with a synthetic run directory (`runs/ZZTEST_BACKSTOP_run` holding
only `runner.launched`), StopIface pointed at a dead port so nothing real could be driven,
and no VR-Forces process on the machine (only rtiexec 69856 / rtiForwarder 50520):

```
*** THE RUNNER DID NOT RECORD A COMPLETED TEARDOWN (exit 0). Tearing down from the wrapper. ***
    run directory : runs/ZZTEST_BACKSTOP_run
    runner pid was: 99999
    StopIface ...
      StopIface exit: 1   (0 ok; 1 the server did NOT reach UNINITIALIZED ...)
    scripts/StopVrf52.ps1 ...
      StopVrf exit: 0   (0 down/already down; 3 still running - NOTHING killed; 5 unexpected)
    observers.stop touched - WatchVrf and ListenReports resign within ~1 s.
    rtiexec / rtiForwarder / rtiAssistant were NOT touched (RUNBOOK 0.5.2).
    STILL RUNNING NOW:
      (none of ours)
```

The StopVrf52 stdout in the run log reads `rtiexec pid=69856 - RTI infrastructure, WILL BE
PRESERVED` / `rtiForwarder pid=50520 - ... WILL BE PRESERVED` / `no VR-Forces processes
running - nothing to do.`, and both RTI pids were still 69856 / 50520 afterwards. The test
directory was deleted. The negative branch was exercised by the same wrapper on a normal
dry run: `no runner.launched marker in the newest run directory - nothing was launched, so
the wrapper tears NOTHING down`.

---

## 4. B1 - the status line now tells the truth, in 70 ms on a 1 GB trace

`Get-LastLineWithPrefix` (`:1178`) walks BACKWARD in 1 MB blocks, newest first, and stops at
the first block holding a match; peak allocation is one block. Only COMPLETE lines are
returned: the newest block is truncated at its last newline (the file is being appended to
while we read it), and a line straddling a block boundary is rejoined through a carry.
`$MaxScanBytes` (32 MB) bounds the work when the prefix is absent.

MEASURED against the real G6 trace, `runs\20260914T002716Z_run\watchvrf-trace.csv`,
1,002,579,653 bytes, read-only:

```
NEW    : [# t=1198.1s reflected=342 readable=327]
elapsed: 69.9 ms          (5 further calls: 18.2, 28.7, 30.3, 29.8, 15.5 ms)
OLD    : [(no samples)]   (10.9 ms)   <- Read-LiveTail 256 KB + Get-TraceSummaryLine
```

INDEPENDENT ORACLE (a full scan of the same file, a different tool):

```
$ grep -a "^# t=" runs/20260914T002716Z_run/watchvrf-trace.csv | tail -1
# t=1198.1s reflected=342 readable=327          real 1.121s
```

Same answer as the block scan, 16x the time. Other cases, all passing:

| case | result |
|------|--------|
| tiny file with one `# t=` line | returns it |
| tiny file with no match | `$null` |
| empty file | `$null` |
| missing path / empty path | `$null` |
| last line half-written (`# t=2.0s refl`) | returns the PREVIOUS complete line, not the fragment |
| `# t=` line straddling a block boundary (`-BlockBytes 1024`) | returned intact, byte-equal to the source |

---

## 5. B2 - the loop stopped re-reading the whole app log

`Read-LiveDelta -Path <p> -Key <k>` (`:1020-1021`) keeps a per-key offset in
`$script:LiveOffsets`, opens with `FileShare ReadWrite|Delete` like the other live readers,
reads only the new bytes (capped at `$MaxBytes` = 16 MB per call, so a backlog is drained
over several polls instead of in one allocation), and cuts the returned text back to the
last complete line - the remainder is re-read next poll rather than parsed as a fragment and
lost. `0x0A` cannot occur inside a UTF-8 multi-byte sequence, so cutting on the byte is
safe. A file that SHRANK (rotated under us) restarts from 0 instead of seeking past its end
and silently returning nothing forever.

The loop (`:2931`) feeds only that delta to `Get-CompletedTasks`. Two consequences in
`RunnerLib.ps1`, both required for correctness and both no-ops under a whole-file reader:

- `:155` `$State.lineCount += $inOrder.Count` (was `=`). The doc comment above
  `New-CompletionState` now says RUNNING TOTAL.
- `:162` ALL-COMPLETE is asked of `$State.firstSeenUtc` (the ACCUMULATED set) instead of
  `$completed` (the taskees present in THIS call's input). Under whole-file input the two
  sets are identical; under a delta reader, using `$completed` would require every taskee to
  re-report inside a single poll and all-complete would essentially never fire.

`$EarlyExit.completionLinesSeen` is unchanged in meaning: it was `lineCount` at the last
poll, which under a whole-file reader WAS the total.

### 5.1 EQUALITY PROOF (the old whole-file path vs the new incremental path)

Corpus: the first 100 MB of the real G6 `vrfc2simapp.log` (5.89 GB; only the prefix was ever
opened, copied with `head -c`, deleted afterwards). The incremental run writes that prefix
into a second file in 20 appends whose cut points are DELIBERATELY NOT line boundaries
(pseudo-random, seed 20260914), calling `Read-LiveDelta` after each append, so every poll
ends mid-line and the rewind is exercised. Both paths use a fixed `NowUtc` so the states are
literally comparable.

```
--- case 1: G6 vrfc2simapp.log first 100 MB (real, unmodified) ---
source: 104,857,600 bytes
  OLD whole-file : lineCount=0 allComplete=null taskees=[]   lines=0   (14.3 s)
  NEW incremental: lineCount=0 allComplete=null taskees=[]   lines=0   polls=20   (22.3 s)
  EQUAL: True
```

Case 1 is TRUE but VACUOUS: G6 never produced a TASKCMPLT line. So case 2 splices five
real-shaped lines into the same 100 MB prefix at line boundaries - four for order taskees
(including a repeat for one taskee) and one for a taskee that is NOT in the order and must
be ignored. The three taskee uuids are lifted verbatim from a run that really did complete,
`runs\20260907T170643Z_run\vrfc2simapp.log`.

```
--- case 2: same prefix with 5 real-shaped TASKCMPLT lines spliced in ---
spliced: 104,858,215 bytes
  OLD whole-file : lineCount=4 allComplete=2026-09-14T12:00:00.0000000Z
                   taskees=[670cfdb2-...def24,001aa71b-...76342,139aa71b-...66242]  lines=5  (14.3 s)
  NEW incremental: lineCount=4 allComplete=2026-09-14T12:00:00.0000000Z
                   taskees=[670cfdb2-...def24,001aa71b-...76342,139aa71b-...66242]  lines=5  polls=20  (22.4 s)
  EQUAL: True
```

Same `lineCount` (4 - the non-order line correctly ignored), same total line count (5), same
taskee set IN THE SAME ORDER, same all-complete stamp. The copies were deleted.

### 5.2 What it costs, measured

The per-poll comparison above is the WRONG comparison (it charges the incremental path 20
function calls for one file's worth of parsing). The right one is the loop's actual shape -
N polls over a file that GROWS - and there the whole-file reader parses O(N * size) while
the delta reader parses O(size):

```
--- cost of 20 polls over a file growing 0 -> 100 MB (the observation loop, compressed) ---
  NEW incremental:    24.3 s   chars parsed    104,858,166
  OLD whole-file :   159.2 s   chars parsed  1,101,011,250
  ratio          : 6.6x time, 10.5x characters
```

A real 900 s window at a 5 s poll is 180 polls, not 20, and the ratio grows linearly with
the poll count: the whole-file path would parse ~90x what the delta path parses. G6's status
cadence had already stretched from a nominal 30 s to 46 s by t+127 s; this is what that was.

Two further checks:

```
--- no-growth polls return nothing once caught up ---
  caught up after 12 polls: consumed 104,858,166 chars of 104,858,215 bytes; offset=104,858,166
  next two polls return 0 and 0 chars (expect 0, 0)
--- a file that SHRANK (rotated) restarts from 0 ---
  offset after first read: 8 ; after truncation, delta=[z\n] offset=2
```

(The 49 bytes never consumed are the final partial line of the `head -c` cut - correctly
withheld, not lost.)

### 5.3 Condition (4): THROTTLED to 30 s, not made incremental. Say which - and what it costs

`Get-VrfUuidByName` correlates a `CreateRoute` line with a LATER `Route ... created` line,
and `Test-ReportEvidence` needs the first TSK, the last RPT and the last POS. Neither can be
fed a delta without cross-poll state of its own, so both still read their whole file. They
are now evaluated at most once per 30 s (`:2941-2942`) instead of once per 5 s, and
`$evidence` / `$evidenceOk` persist across polls; the first evaluation after all-complete is
immediate, so the pending-reason message can never dereference a `$null`.

HONEST COST, STATED IN BOTH DIRECTIONS. The close decision now runs on an evidence snapshot
up to 30 s old. An out-to-IN flip is seen late, which lengthens the window (safe). An
in-to-OUT flip is ALSO seen late, so the window can close on evidence that was satisfied
30 s ago and is not satisfied now. `$ReportToleranceMeters` is 2.0 m (`:1826`), which is
tight enough that this is not impossible. It is accepted because condition (4) is only ever
evaluated after every taskee has reported TASKCMPLT - the units have stopped and the RPT
stream converges on a static POS - and because `$EarlyExit.evidenceSatisfiedUtc` plus the
per-taskee `reportEvidence` written at that evaluation record exactly which snapshot the
close was made on. If a run ever closes on stale evidence, that pair is the evidence of it.
The alternative is ~12 whole-trace reads during a 60 s hold, at ~2 GB of UTF-16 per read on
a 1 GB trace - which is the failure this whole document is about.

---

## 6. The inherited pipe - documented, because nothing in `Start-External` can fix it

`Start-External` (`RunC2SimScenario.ps1:876-925`) starts every child with `Start-Process
-NoNewWindow -PassThru -RedirectStandardOutput/-RedirectStandardError`. .NET's
`Process.Start` with ANY redirection calls `CreateProcess` with `bInheritHandles = TRUE`,
which duplicates every inheritable handle in the parent into the child - including the
handle the runner holds as its own stdout. In G6 that was the harness's pipe: `VrfC2SimApp`
never wrote to it but held it open, and the harness saw no EOF until the app was stopped by
hand nine hours later. There is no `Start-Process` switch that suppresses this, so the fix
is upstream and it is a RULE, not code:

- the runner's stdout AND stderr go to a FILE, and its stdin comes from `/dev/null`;
- the runner is NEVER piped and NEVER `| tee`-ed (`tee` also makes `$?` tee's status);
- any background subshell in a wrapper gets `< /dev/null` too.

`scripts\RunScenario.sh` does all three (its optional `--sample-threads` subshell is the
worked example) and `docs\RUNBOOK.md` sec 0.5.14 item 2 is the rule. Watch a run from
another shell with `tail -f <log>`.

---

## 7. Gates run

| gate | result |
|------|--------|
| `[scriptblock]::Create((Get-Content -Raw ...))` on both .ps1 | PARSE OK, both |
| `bash -n scripts/RunScenario.sh` (LF and CRLF) | OK both |
| `scripts\RunC2SimScenario.ps1 -DryRun`, 64-bit | exit 0, `DRY-RUN complete`, no run directory, marker still 4255 |
| same, 32-bit `pwsh` | exit 2, the gate's four `[FAIL]` lines, nothing launched |
| `scripts/RunScenario.sh --dry-run` end to end (CRLF file) | exit 0, config banner, 127 legend table, backstop negative branch |
| `scripts/RunScenario.sh --help`, unknown option | usage; exit 2 |
| `tests\RunnerTurnaround.Tests.ps1` | 222 passed, 1 failed |
| `rg -nP "[^\x09\x0a\x0d\x20-\x7E]"` on all four touched files | clean; the checker reproduces on a dirty control (2 hits) |
| CRLF on all four touched files | `tr -cd` counts: CR == LF on each |

THE ONE TEST FAILURE IS PRE-EXISTING AND UNRELATED: `the ready-path harvest is skipped when
the launch crashed` is a regex over `$lv52Text`, i.e. `scripts\LaunchVrf52.ps1`, which this
work does not touch (`git status` shows four modified/added files, and that is not one of
them). It is NOT fixed here and it is NOT this change's to fix.

The .sh is CRLF because every `.ps1` and `.md` in this repo is CRLF, there is no
`.gitattributes`, and `core.autocrlf` is `true` - so a LF-committed `.sh` would be checked
out as CRLF anyway. MEASURED FIRST, on this MSYS2 bash 5.2.15: a CRLF script with a shebang,
`[ -n ]` and `[ -d ]` tests, `$(...)` substitution, numeric comparison, `case`, and a
Windows exe call all behave identically to LF; `$(ls -1dt ...)` assigned to a variable and
then used as a path resolves (i.e. no trailing `\r` contamination). The wrapper was then run
end to end in its CRLF form.

---

## 8. NOT covered - read this before assuming the hole is closed

1. **THE WRAPPER DYING TOO.** The backstop only runs if the wrapper's bash survives the
   runner. In G6 it did (it printed the echo), so this design would have prevented that
   nine-hour leak. It would NOT cover a kill that takes the whole shell, a closed terminal,
   or a reboot. **SUPERSEDED THE SAME DAY - A2 IS NOW BUILT: `scripts\RunnerWatchdog.ps1`,
   started by the runner at stage 6b-w. See sec 10 (and sec 12 for its tests).** What remains
   uncovered is narrower and is stated there: the watchdog has not yet run inside a live run,
   and a kill in the sub-second window before stage 6b-w leaves the wrapper as the only
   backstop.
2. **THE KILLER IS STILL UNIDENTIFIED.** No process-creation auditing (Security 4688 count
   0 for the window), Kernel-Process/Analytic disabled, no Sysmon. The shape that fits every
   observation - exactly one pwsh dead, the sampler pwsh and the parent bash alive - is a
   `Stop-Process` filtered on a CommandLine substring the runner's command line contains and
   the sampler's does not. It was not found. **Nothing here prevents the next kill**; the
   fixes make its consequences bounded, not its occurrence impossible. RUNBOOK 0.5.14 item 5
   bans sweeps during a run window and points at `runner.launched` for the pid to exclude;
   that is procedure, not enforcement.
3. **The sub-second window** between LaunchVrf returning and `runner.launched` being written.
4. **The 30 s evidence staleness** of sec 5.3.
5. **`Read-LiveTail` is now dead code** and was left in place.
6. **NOT RUN LIVE.** Every result above is from the offline gates, the recorded G6
   artefacts, and a synthetic backstop exercise. No VR-Forces process was launched by this
   work. The first live run through `scripts\RunScenario.sh` is the real test of items 1-4.

---

## 9. Adversarial review of THIS change

**"The 64-bit gate will break something that legitimately runs 32-bit."** Checked:
`tests\RunnerTurnaround.Tests.ps1` never EXECUTES the runner - it uses
`Parser::ParseFile` and `Get-Content -Raw` - so the gate cannot affect it, and the suite
runs unchanged. No other caller of `RunC2SimScenario.ps1` exists in the repo. The gate is
before the `RunnerLib.ps1` dot-source, so it also cannot be defeated by a failure in it.

**"The backstop will tear down a session it does not own."** This was a REAL defect in the
minimal A1 as proposed, found here and fixed by the `runner.launched` marker (sec 3): the
runner has validation aborts after the run directory exists but before anything is launched,
one of which fires precisely when VR-Forces is ALREADY UP and belongs to someone else.
Without `runner.launched`, a marker-absence rule turns that abort into a teardown of the
other session. With it, absence means "touched nothing" and the wrapper stands down - which
is what the dry-run exercise printed.

**"The incremental reader will drop a line."** The falsifier would be a TASKCMPLT line
straddling a poll boundary going unseen. That is exactly what the case-2 splice tests, with
cut points chosen NOT to be line boundaries: `EQUAL: True` on all of `lineCount`, the taskee
set and its order, and the all-complete stamp. The reader also refuses to advance its offset
past an incomplete line at all (the 49 withheld bytes in sec 5.2 are that behaviour).
Remaining, un-falsified: a file that is both truncated AND regrown between two polls would be
re-read from 0 and double-count `lineCount`. The app log is opened for append by one writer
and never rotated, so this is theoretical - but it is not excluded by anything measured.

**"The block scan will return a truncated line."** Two ways it could: a half-written line at
the end of the file, and a line cut by a block boundary. Both are tested explicitly (sec 4,
last two rows) and both pass, the second by byte-equality against the source string. Not
tested: a multi-byte UTF-8 character split across a block boundary would decode as a
replacement character at the join. Every `# t=` line is ASCII, so this cannot affect the
value returned, and the function is only used for that prefix.

**"lineCount now double-counts."** It would, under a whole-file caller - and that is why
`RunnerLib.ps1`'s header comment now says RUNNING TOTAL in as many words, and why sec 5.2's
control run prints `OLD whole-file : lineCount=48` where the delta path prints 4. The runner
has exactly one caller and it passes deltas. A second caller passing whole files would be a
defect, and the comment is where it will be caught.

**A symptom NOT explained away:** the status cadence in G6 stretched to 44 / 37 / 46 s
against a nominal 30 s. Sec 5.2 shows the whole-file read is large enough to account for
that, but nothing here MEASURES the runner's own loop at G6's volume - that measurement is
owed to the first live run, and if the cadence still stretches with the delta reader in
place, this explanation is wrong and something else is eating the poll.

---

## 10. A2 - THE DETACHED WATCHDOG, BUILT (2026-09-14, second pass)

Sec 8 item 1 said the wrapper backstop does not cover the wrapper dying too, and that A2 was
"DESIGNED AND NOT BUILT". It is now built: `scripts\RunnerWatchdog.ps1` (361 lines), started by
the runner at a new stage 6b-w. Tier: STANDARD - mechanical work against the already-adjudicated
G6 diagnosis, verified by offline tests. No new cause claim is made here.

### 10.1 What it is, in one paragraph

A separate process, started by the runner the instant the interface exists, that polls the
runner's PID every 5 s and does NOTHING ELSE while the runner lives. When the runner is gone it
applies the wrapper's own rule - `runner.launched` present AND `runner.teardown-ran` absent -
and, if it holds, claims a new marker `runner.watchdog-ran` and runs the runner's teardown
order: StopIface (clean resign), then `StopVrf52.ps1` / `StopVrf.ps1`, then touch
`observers.stop`, then print what is still up. It force-kills NOTHING on any path, and
rtiexec / rtiForwarder / rtiAssistant are never touched - they appear only in its closing
inventory. `-NoWatchdog` on the runner turns it off.

Exit codes: 0 nothing owed (or teardown done, all steps ok); 2 REFUSED at validation or on the
foreign-session guard, having touched nothing; 3 teardown ran with a failed step; 4 `-MaxSec`
expired while the runner was STILL ALIVE (nothing torn down - a live run is never torn down by
a timer); 5 unexpected terminating error.

### 10.2 Why it is DETACHED, and the measurement that says it is

Three handles decide whether this process survives what kills the runner.

**Its console.** `Start-External` has always passed `-NoNewWindow`, which makes the child SHARE
the parent's console. A console-sharing child receives that console's Ctrl+C / Ctrl+Break and
dies when the terminal closes - i.e. it would die in exactly the scenario the watchdog exists
for. So `Start-External` gained a `-NewConsole` switch: it drops `-NoNewWindow` and passes
`-WindowStyle Hidden` instead, which sends PowerShell's `CreateProcess` path down its
CREATE_NEW_CONSOLE (0x10) + STARTF_USESHOWWINDOW / SW_HIDE branch.

MEASURED on this machine, 2026-09-14, with `GetConsoleProcessList` (which reports every process
attached to the CALLER'S console) called from inside a child started BOTH ways by the same
parent (pid 69496):

```
  PARENT ITSELF      : consoleProcessCount=4  consolePids=[43792,69496,44988,77916]  parentInList=True
  -NoNewWindow       : consoleProcessCount=4  consolePids=[68116,69496,44988,77916]  parentInList=True
  -WindowStyle Hidden: consoleProcessCount=1  consolePids=[33540]                    parentInList=False
```

The `-NoNewWindow` child sits in the parent's console with three other processes; the
`-WindowStyle Hidden` child is ALONE in a console of its own. Redirection keeps working in both
shapes (both children's stdout files were written), so the detachment costs nothing.

**Its stdin.** PowerShell sets STARTF_USESTDHANDLES whenever anything is redirected and fills
the handles it was not given from the PARENT'S own - so without `-RedirectStandardInput` the
watchdog would hold the launching terminal's stdin for as long as it lives. `Start-External`
therefore also gained `-StdInFile`, and the runner points it at `<RunDir>\watchdog.stdin.empty`,
a zero-byte file: the Windows equivalent of the wrapper's `< /dev/null`.

**Everything else it inherits.** `Process.Start` with any redirection calls `CreateProcess` with
`bInheritHandles=TRUE` and there is no switch that suppresses it (sec 6), so the watchdog DOES
receive duplicates of whatever else the runner holds. Under `scripts\RunScenario.sh` those are
FILES, which have no EOF semantics for a reader, so the duplicates are harmless. THIS IS WHY
RUNBOOK 0.5.14 ITEM 2 IS A RULE AND NOT A PREFERENCE: pipe the runner and this process - the one
designed to outlive everything - would hold that pipe open for the whole window, which is
precisely G6's nine-hour symptom. Not instrumented here; it follows from sec 6.

Both `Start-External` parameters default to the previous behaviour exactly (`-NoNewWindow`, no
stdin redirection), so no other stage changes. They have ONE caller: stage 6b-w.

### 10.3 The contract, and what the runner passes

```
scripts\RunC2SimScenario.ps1
  :421-428   -NoWatchdog switch (comment + parameter)
  :926-941   Start-External gains -StdInFile and -NewConsole (defaults = previous behaviour)
  :966,:970  the splat: -NoNewWindow unless -NewConsole, and the stdin redirect
  :2104-2112 watchdog.stdout.log / watchdog.stderr.log / watchdog.stdin.empty / watchdog.pid
  :2737-2816 STAGE 6b-w: the launch, ledgered through Start-External like every other stage
             (:2786-2799 the try/catch: a backstop that cannot start must never fail the run)
```

The runner passes its own `$PID`, the run directory, the profile, both C2SIM endpoints (never
guessed - StopIface has no defaults and a guessed endpoint could drive the OPERATOR'S server),
`-PollSec 5`, and `-MaxSec` = the observers' whole cap + `-TraceStopGraceSec` +
`-AppExitTimeoutSec` + `-StopVrfTimeoutSec` + 600, capped at 86400. Generous is the safe
direction because the timer NEVER tears anything down. The watchdog's pid goes to
`<RunDir>\watchdog.pid`, to `run-manifest.json` (`artifacts.watchdog`) and to the stage ledger.

Two guards that the wrapper's version cannot have. (1) The watchdog is told which pid to watch
AND reads `runner.launched`; if the two disagree it REFUSES (exit 2) - the run directory belongs
to a different runner. (2) `runner.watchdog-ran` is claimed with `FileMode::CreateNew`, so two
watchdogs on one run directory can never both act.

A missing `RunnerWatchdog.ps1` is a WARN flag and the run CONTINUES: a backstop must never fail
a healthy run. `-NoWatchdog` raises the same flag.

### 10.4 KNOWN, BENIGN: the wrapper and the watchdog can both tear down

`scripts\RunScenario.sh` predates this script and neither reads nor writes `runner.watchdog-ran`
(the brief for this work did not open the .sh). So a kill that leaves the WRAPPER alive produces
two teardowns: the wrapper's, immediately, and the watchdog's ~5-7 s later. Every step is a
graceful idempotent request - StopIface against an already-UNINITIALIZED server, StopVrf against
nothing to stop, an `observers.stop` that already exists - so the second is a no-op that logs;
it will show as `StopIface exit 1` and a watchdog exit 3, which must NOT be read as two
failures. The complete fix is two lines in the wrapper: check `runner.watchdog-ran` beside
`runner.teardown-ran`, and `touch` it before its own teardown. RECOMMENDED, NOT DONE HERE.

Second known false positive, shared with the wrapper: the runner's `runner.teardown-ran` write
is best-effort (`try {} catch {}`), so a completed teardown whose marker could not be written
looks exactly like a death. The consequence is the same benign duplicate teardown.

Deliberate non-action: when `runner.teardown-ran` IS present the watchdog stands down even if
VR-Forces is still up. That state is the runner's own exit 4 - loud, flagged, and already
inspected by a human; a watchdog that re-ran teardown there would be overriding a judgment the
runner already made and reported.

---

## 11. STAGE 7d - THE PRE-ORDER SETTLE (supervisor addition, 2026-09-14)

Unrelated to the G6 death; landed in the same pass because it is the same file. OFF by
default (`-PreOrderSettleSecs 0`), so a default run is byte-identical to one from before it
existed.

WHY. The sectorised navigation area loads LAZILY, AFTER the entities are placed. Run
`20260914T130439Z` logged the area's "New Primary nav area" rows 175 s after the members were
created, so a task issued before that is PLANNED WITHOUT THE MESH. With N > 0 the runner holds
the order between the oracle gate and PushOrder for N seconds, printing one status line every
30 s.

WHAT IT IS NOT. Not a fix and not evidence. It says how long we wait; it cannot say the mesh
arrived - only the object consoles can (MEMORY lessons-vendor-diagnostics-first). The
vendor-side alternative is UG52 Appendix C `loadAllNavigationDataOnTerrainLoad`, which loads
every sector at terrain load; that is a CONFIG change and the better answer if the hold turns
out to matter. Nothing here claims it does.

```
scripts\RunC2SimScenario.ps1
  :350-362   the parameter and why it exists
  :1516-1520 validation 0..3600 (a negative is meaningless; a huge hold eats the run)
  :2184-2201 the hold is ADDED to $DerivedWatchSecs, ledgered as inputs.preOrderSettleSecs, and
             the WARN when an EXPLICIT -WatchSecs is below the now-larger derived cap (sec 12.4)
  :2210-2214 the planned-run banner lines (observers cap breakdown, pre-order line)
  :2962-2999 STAGE 7d itself: the 30 s status loop, clocks.preOrderSettleStart/EndUtc
scripts\RunScenario.sh
  :58 :84-85 :116 :168-171 :183   --pre-order-settle N -> -PreOrderSettleSecs N
```

THE ONE NON-OBVIOUS PART, and the defect it avoids: the hold sits INSIDE the observers'
coverage (between the oracle gate and PushOrder), so without adding it to their duration cap a
long hold could end the trace BEFORE the observation window does - silent evidence loss. It is
added to `$DerivedWatchSecs` in the RUNNER (`:2189`), NOT inside `Get-DerivedWatchSecs`, so the
formula pinned by `tests\RunnerTurnaround.Tests.ps1` check 1 - and by the record it reproduces
- is untouched. With the default 0 the line changes nothing.

LINE-ENDING NOTE: sec 7 says `scripts\RunScenario.sh` is CRLF. It is NOT, as checked out today:
`git ls-files --eol` reports `i/lf w/lf` for it (the .ps1 and .md files are `i/lf w/crlf`). The
edits above therefore use LF, matching the file, rather than introducing mixed endings.

---

## 12. GATES AND TESTS for sec 10 (watchdog) and sec 11 (stage 7d)

All offline. NO VR-Forces was launched by this work, and every test below ran with the machine
verified EMPTY of vrfSimHLA1516e / vrfGui / vrfLauncher / VrfC2SimApp / WatchVrf /
ListenReports first - the driver ABORTS otherwise, because StopVrf52 would close a live sim.
rtiexec 69856 and rtiForwarder 50520 were up throughout and were still up, unchanged,
afterwards.

| gate | result |
|------|--------|
| `[scriptblock]::Create((Get-Content -Raw ...))` on both .ps1 | PARSE OK, both |
| `bash -n scripts/RunScenario.sh` | OK |
| `scripts/RunScenario.sh --help` / unknown option | usage printed; exit 0 / exit 2 |
| `scripts/RunScenario.sh --dry-run --pre-order-settle 175` | exit 0, DRY-RUN complete, no run directory, marker NOT advanced |
| `scripts/RunScenario.sh --dry-run -- -NoWatchdog` | exit 0, stage 6b-w SKIPPED + WARN flag |
| `tests\RunnerTurnaround.Tests.ps1` | 222 passed, 1 failed - the SAME pre-existing failure as sec 7 ("the ready-path harvest is skipped when the launch crashed", a `LaunchVrf52.ps1` regex this work does not touch). Baseline unchanged. |
| `rg -nP "[^\x09\x0a\x0d\x20-\x7E]"` on all five touched files | clean; the checker reproduces on a dirty control (em dash / NBSP / VT = 3 hits) |
| line endings | .ps1 and .md CRLF (CR == LF on each); `RunScenario.sh` LF, matching the file as checked out - see the note in sec 11 |
| watchdog argument validation | 7 refusals in one run, exit 2, nothing touched |

### 12.1 Test (a) - the runner dies, the watchdog tears down

Synthetic `runs\ZZTEST_WATCHDOG_run` holding only `runner.launched` = a FAKE runner pid (a
pwsh that sleeps 10 s), StopIface pointed at DEAD ports 18099 / 61699 (never the private test
server 18080 / 61614, never the operator's 8080 / 61613). Watchdog started exactly as the
runner starts it: own hidden console, stdout / stderr / stdin all files in the run directory.

```
watchdog pid 83280 started (own hidden console, stdio redirected to the run dir)
WATCHDOG EXIT = 3
13:34:07.196Z [OK]   runner.launched present and its pid matches (-RunnerPid 69860).
13:34:07.201Z [OK]   watching runner pid 69860 (started 2026-09-14 09:34:06)
13:34:17.228Z [INFO] runner pid 69860 IS GONE (exit code as seen from here: 0).
13:34:19.242Z [WARN] *** THE RUNNER DIED WITHOUT A COMPLETED TEARDOWN. Tearing down from the watchdog. ***
13:34:19.243Z [INFO] claimed ...\runs\ZZTEST_WATCHDOG_run\runner.watchdog-ran
13:34:19.249Z [INFO] StopIface: ...\StopIface.exe http://127.0.0.1:18099/C2SIMServer http://127.0.0.1:61699/topic/C2SIM --yes
13:34:21.465Z [FAIL] StopIface exit 1 (... the interface MAY STILL BE JOINED ...). Nothing was force-killed.
13:34:21.467Z [INFO] StopVrf: C:\Program Files\PowerShell\7\pwsh.exe ... StopVrf52.ps1 -TimeoutSec 120
13:34:22.086Z [OK]   StopVrf exit 0: VR-Forces is down or was already down (graceful; RTI preserved).
13:34:22.089Z [OK]   observers.stop touched ... - WatchVrf and ListenReports resign within ~1 s.
13:34:22.168Z [OK]   nothing of ours is left running.
13:34:22.198Z [OK]   RTI infrastructure PRESERVED (correct): rtiexec(pid 69856), rtiForwarder(pid 50520)
13:34:22.199Z [FAIL] watchdog teardown RAN but at least one step did not report success. Exit 3.
```

Death seen within ONE poll (10.0 s life, noticed at +10.03 s); markers afterwards:
`runner.launched` T, `runner.teardown-ran` F, `runner.watchdog-ran` T, `observers.stop` T.
`watchdog.stderr.log` 0 bytes. StopIface's own stderr carries the expected dead-port refusal
(`Connection error: ... actively refused it. (127.0.0.1:18099)`), which is what exit 1 means
here - the TOLERATED failure the design calls for, and the whole teardown continued past it.
StopVrf52's stdout: `rtiexec pid=69856 - RTI infrastructure, WILL BE PRESERVED` /
`rtiForwarder pid=50520 - ... WILL BE PRESERVED` / `no VR-Forces processes running - nothing to
do.` Exit 3 is CORRECT for this case: the teardown ran, one step failed, and it says so.

### 12.2 Test (b) - the runner tore down its own run

Same directory with `runner.teardown-ran` added.

```
WATCHDOG EXIT = 0
13:34:48.509Z [INFO] runner pid 80724 IS GONE (exit code as seen from here: 0).
13:34:50.523Z [OK]   runner.teardown-ran is present: the runner completed its own teardown.
                     Standing down without touching anything.
```

StopIface did NOT run, StopVrf did NOT run, `runner.watchdog-ran` F, `observers.stop` F. The
run directory afterwards holds only the log, the two markers, watchdog.pid, the empty stdin
file and two 0-byte stdio files.

### 12.3 Test (c) - no runner.launched: REFUSE

```
WATCHDOG EXIT = 2
13:34:59.346Z [FAIL] REFUSED: no runner.launched in ...\runs\ZZTEST_WATCHDOG_run. This run
                     launched nothing, so what is up may be a FOREIGN live session
                     (RUNBOOK sec 0). NOTHING was touched.
```

Refused in 50 ms, at STARTUP, while the fake runner was still alive - it never reached the poll
loop. No marker, no stop file, no teardown tool started. `runs\ZZTEST_WATCHDOG_run` was deleted
after the three tests.

### 12.4 The stage-7d trap this testing found (FIXED)

An EXPLICIT `-WatchSecs` overrides the derived cap, settle included - and `RunScenario.sh`
passes `--watch-secs 1200` by default. So the default wrapper config plus a 175 s hold gives an
observers' cap of 1200 against a derived requirement of 1635: the observers could end BEFORE
the window does and truncate the trace with no error. The runner now SAYS SO rather than
overriding the operator's explicit value, and the banner prints the derived total with the
`+ preOrderSettle N` term in it:

```
  [WARN] -WatchSecs 1200 was passed EXPLICITLY and is below the derived cap 1635, which now
         includes the 175s stage-7d hold. The observers can end BEFORE the observation window
         does, truncating the trace with no error. Raise -WatchSecs to at least 1635, or drop
         it and let the runner derive it.
  observers   : 1200s CAP (derived 1635: preRoll 20 + appJoin 180 + initDispatch 120 +
                oracleGate 180 + pushOrderListen 30 + run 900 + trail 30 + preOrderSettle 175)
```

WHOEVER RUNS THE FIRST 7d RUN: raise `--watch-secs` by at least the hold, or drop the flag.

### 12.5 NOT TESTED - owed to the first live run

1. **The watchdog has never run inside a REAL run.** Stage 6b-w is exercised only in `-DryRun`
   (the plan lines, the command line, all four redirects) and the watchdog itself only against
   a fake runner pid. What a live run must confirm: `watchdog.pid` written, the stage present
   in the manifest, the watchdog exiting 0 within ~7 s of a NORMAL teardown, and
   `watchdog.stderr.log` still 0 bytes at the end.
2. **The kill case, live.** Nobody has killed a real runner with the watchdog armed. The honest
   test is a deliberate one on a throwaway run, not a wait for the next incident.
3. **The double teardown of sec 10.4** (wrapper + watchdog) has been reasoned about, not seen.
4. **Stage 7d has never held a real order back**, and nothing here measures whether 175 s is
   enough, or whether a loaded mesh changes anything. That is a consoles question, not a
   runner one.

---

## 13. THE COLD-START REVIEW OF 374ea49, AND THE FIXES IT BOUGHT (2026-09-14)

A cold-start adversarial review of 374ea49 (Opus, read-only, against `git show 374ea49:<path>`)
returned SAFE TO ARM for G7 attempt 4 with one launch-line change - `--watch-secs 2000`, not
1800, because an explicit `-WatchSecs` overrides the derived cap and 1800 is below the 2000 the
240 s stage-7d hold creates. The full text is copied verbatim into
`docs\experiments\REVIEW_WATCHDOG_374ea49_2026-09-14.md`; do not re-derive its arithmetic here.

Findings F2-F8 and F10 are APPLIED. F1 is the launch line (supervisor). F9 (the watchdog stage
stays `started-background` in the manifest - correct by design), F11 (`--sample-threads` cannot
outlive the run) and F12 (a worktree has no `StopIface.exe`, and stage 0 hard-fails first) are
NO CHANGE.

| # | what changed | where |
|---|--------------|-------|
| F2 | A failed handle cache now DROPS the process object, so the documented `Get-Process` + `StartTime` fallback is the code that actually runs. `Process.HasExited` with no cached handle re-opens the process on every call and treats ANY failure to open it as "exited" - and the flag is STICKY - so the old code turned one transient `OpenProcess` failure into a permanent "the runner is dead" and a teardown of a HEALTHY run. | `scripts\RunnerWatchdog.ps1` :192-215 |
| F2 | A death must now be observed TWICE, 2 s apart, before the poll loop is left. One reading is never acted on. | `scripts\RunnerWatchdog.ps1` :260-282 |
| F2 | `Get-Process` failing AT STARTUP no longer logs "ALREADY GONE" when the runner is alive: `Win32_Process` is asked as an independent second source, and its `CreationDate` becomes the `StartTime` PID-reuse guard when it answers. | `scripts\RunnerWatchdog.ps1` :217-230 |
| F2 | Two TEST-ONLY switches, both off by default, both logged loudly, never passed by the runner: `-NoHandleCache` (force the fallback path) and `-TestFakeDeadReads N` (force the first N liveness observations to report GONE). Without them the two F2 fixes are unreachable offline - nothing can make Windows fail an `OpenProcess` on demand. | `scripts\RunnerWatchdog.ps1` :74-82, :122, :155-157, :233-241 |
| F3 | `scripts\RunScenario.sh`'s own backstop now CLAIMS `runner.watchdog-ran` atomically (`set -C; : > ...` in a subshell) before it tears anything down, and STANDS DOWN with a message if the claim fails. The two backstops no longer overlap; sec 10.4's "benign but unmeasured" duplicate teardown is closed. | `scripts\RunScenario.sh` :287-311 |
| F4 | RUNBOOK 0.5.14 item 5 now names `<RunDir>\watchdog.pid` alongside `runner.launched` as a pid a cleanup sweep must exclude. The watchdog is a direct CHILD of the runner, so a tree kill or a CommandLine-pattern sweep - the shape that fits every observation of the G6 kill - removes it with the runner. | `docs\RUNBOOK.md` 0.5.14 item 5 |
| F5 | After arming, the runner waits 750 ms and, if the watchdog has already exited, records a WARN flag with its exit code and the path of `runner-watchdog.log`. A watchdog that REFUSES at validation exits in ~50 ms; the manifest used to say "armed" while the run went on unprotected. The run still continues - a backstop must never fail a healthy run. | `scripts\RunC2SimScenario.ps1` :2749-2764 |
| F6 | The watchdog is armed at STAGE 3w - immediately after `runner.launched` is written - not at stage 6b. The unprotected window was never "sub-second": it held stage 3b's settle, the stage-4 pre-check, the observers, the pre-roll and PushInit, and a runner death in it leaves a back-end that HARD-BLOCKS the next launch. `-MaxSec` therefore now covers the stages between: `max(EffWatchSecs, DerivedWatchSecs) + LaunchSettleSec + (PreCheckSecs + StageTimeoutSec) + StageTimeoutSec + (TraceStopGraceSec + AppExitTimeoutSec + StopVrfTimeoutSec) + 600`. The arithmetic is spelled out in the code. | `scripts\RunC2SimScenario.ps1` :2639-2690 |
| F7 | Stage 7d is no longer a blind sleep: the back-end pid, `VrfC2SimApp` and the trace observer are polled inside the 30 s status loop and a death is `Stop-Runner 3`, not something slept through and discovered later for the wrong reason. The order has not been pushed at that point, so this is a clean stop. | `scripts\RunC2SimScenario.ps1` :3090-3109 |
| F8 | The runner's OWN StopVrf launch used a bare `pwsh` - the 32-BIT build on this machine (RUNBOOK 0.5.14 item 1). It is now `Join-Path $PSHOME 'pwsh.exe'`, like the watchdog. | `scripts\RunC2SimScenario.ps1` :3453-3456 |
| F10 | The wrapper no longer guesses the run directory by MTIME. The runner writes `runs\launch52\last-run-dir.txt` when it creates the run directory; the wrapper DELETES that pointer before launching and reads it afterwards, falling back to the old scan (loudly) only if it is absent. A dry run, which creates no run directory, now skips the backstop entirely instead of scanning. | `scripts\RunC2SimScenario.ps1` :2324-2339; `scripts\RunScenario.sh` :220-285 |

### 13.1 Gates and tests, all offline

The machine was verified EMPTY of `vrfSimHLA1516e` / `vrfGui` / `vrfLauncher` / `VrfC2SimApp` /
`WatchVrf` / `ListenReports` before the watchdog tests (the driver ABORTS otherwise - it did
abort once, on a transient `VrfC2SimApp`, and the run was repeated when the machine was clean).
`rtiexec` 69856 and `rtiForwarder` 50520 were up throughout and were untouched and still up
afterwards. StopIface was pointed at DEAD ports 18099 / 61699 - never the private test server
(18080 / 61614), never the operator's own (8080 / 61613). NO VR-Forces was launched.

| gate | result |
|------|--------|
| `Parser::ParseFile` on `RunC2SimScenario.ps1`, `RunnerWatchdog.ps1`, `RunnerLib.ps1` | PARSE OK, 0 errors, all three |
| `bash -n scripts/RunScenario.sh` | OK |
| `scripts/RunScenario.sh --help` / unknown option | exit 0 / exit 2 |
| `scripts/RunScenario.sh --dry-run` | exit 0; stage 3w armed in the plan, `-MaxSec` 3695 = cover 1460 + settle 45 + preCheck 630 + pushInit 600 + teardown 360 + slack 600 |
| `scripts/RunScenario.sh --dry-run --pre-order-settle 240 --run-secs 1200 --watch-secs 2000` | exit 0; observers cap 2000 (derived 2000, no WARN); `-MaxSec` 4235 s = 70.6 min, matching the comment's arithmetic |
| `scripts/RunScenario.sh --dry-run -- -NoWatchdog` | exit 0; "Stage 3w - detached teardown watchdog: SKIPPED" + the WARN flag |
| `tests\RunnerTurnaround.Tests.ps1` | 222 passed, 1 failed - the SAME pre-existing failure ("the ready-path harvest is skipped when the launch crashed", a `LaunchVrf52.ps1` regex this work does not touch). Baseline unchanged. |
| watchdog tests (a) / (b) / (c) | exit 3 (claimed + tore down) / exit 0 (stood down on `runner.teardown-ran`) / exit 2 (REFUSED, no `runner.launched`) - unchanged from sec 12 |
| watchdog test (d), NEW | see below |
| wrapper claim primitive | first `( set -C; : > marker )` CLAIMS, second STANDS DOWN, the marker still holds the FIRST claim only, and noclobber does not leak out of the subshell |
| pointer read | a CRLF Windows path is read, `cygpath -u`-converted and `-d`-tested; a pointer naming a non-directory falls back with a WARN |
| `rg -nP "[^\x09\x0a\x0d\x20-\x7E]"` on every touched file | clean; the checker reproduces on a dirty control (it caught a BEL byte that an octal escape put into the TEST DRIVER during this work - the `\7` of a `PowerShell\7` path, the MEMORY lesson exactly) |
| line endings | `.ps1` / `.md` CRLF (CR == LF in each), `scripts\RunScenario.sh` LF, unchanged |

### 13.2 Test (d) - a live runner read as GONE is NOT a death

`-NoHandleCache -TestFakeDeadReads 1` against a fake runner that sleeps 30 s: the fallback path
is the one running, the first liveness observation is FORCED to report GONE while the process is
provably alive, and the watchdog must absorb it. Under the one-observation rule of 374ea49 this
is a teardown of a healthy run at t+0.02 s.

```
14:28:15.367Z [WARN] -NoHandleCache (TEST): the handle cache is SKIPPED, so liveness runs on the
                     Get-Process + StartTime FALLBACK for pid 34904.
14:28:15.368Z [OK]   watching runner pid 34904 (started ...; liveness via Get-Process + StartTime)
14:28:15.373Z [WARN] -TestFakeDeadReads (TEST): this observation of pid 34904 is FORCED to report GONE
14:28:15.374Z [INFO] pid 34904 read as GONE. CONFIRMING in 2 s - one observation is never enough
                     to tear down a run.
14:28:17.387Z [WARN] pid 34904 read as GONE once and ALIVE 2 s later: the first read was TRANSIENT
                     and NOTHING was touched. Continuing to poll.
14:28:47.442Z [INFO] pid 34904 read as GONE. CONFIRMING in 2 s ...
14:28:49.453Z [INFO] pid 34904 read as GONE TWICE, 2 s apart. Treating the runner as dead.
```

The watchdog acted 39.5 s after it started - i.e. after the runner REALLY died at ~30 s, not at
the forced read - and then claimed the marker and ran the normal teardown (exit 3, from the
expected StopIface refusal on a dead port). A control run with `-TestFakeDeadReads 2` tore the
LIVE fake runner down, which is correct and is the point: two consecutive dead observations ARE
the death criterion, and the rule buys exactly one transient, not immunity.

### 13.3 Still not tested, and honestly so

1. **No live run.** Everything above is offline. The watchdog has still never armed inside a real
   run (sec 12.5 item 1 stands), and stage 3w moves the arming point, so the first live run must
   confirm `watchdog.pid`, the manifest block with `armedAtStage`, the 750 ms survival check
   passing, and the watchdog exiting 0 within ~7 s of a normal teardown.
2. **The F3 stand-down has not been seen in a REAL double-teardown** - only the claim primitive
   and the branch logic were exercised. What it now prevents is the overlap, not a failure.
3. **A genuine transient `OpenProcess` failure has never been observed** on this machine; test (d)
   INJECTS one. The fix is reasoned + injected, not field-observed.

---

## 14. FIVE DEFECTS FROM THE RUNS OF 2026-09-14 (third pass)

Evidence: `docs/experiments/G7_ATTEMPT4_RESULTS_2026-09-14.md` sec 4.2 and the runs of
2026-09-14. Everything below is OFFLINE - no simulator, no VR-Forces, no launch.

### 14.1 Defect 1 - `-StopWhenComplete` had never fired, and could not

`--stop-when-complete` is ON by default in `RunScenario.sh` and has closed ZERO windows.
The cause is not timing or tuning: condition (4) of the early exit ("post-completion report
evidence") could only be satisfied by an **RPT** record in the WatchVrf trace, and an RPT row
is a VR-FORCES RADIO TEXT REPORT (`tools/WatchVrf/ConFormat.cs:83-96`) - the Lua tracker's
`POSITION "<marking>" <lat> <lon>` broadcast. This interface never asks for one and these
scenarios never run that tracker.

    RPT rows in the trace: 0     (all 8 runs of 2026-09-14)

So every run ran its `-RunSecs` cap out with its units already stopped. The report channel
the interface DOES drive is the C2SIM PositionReport (R1, `Vrf__PositionReportSeconds`;
`VrfC2SimService.MaybeSendPositionReports`) - 147 fixes for the single taskee of run
`20260914T154243Z` against 0 RPT rows - and it is the channel the adjudication reads.

**THE FIX.** `RunnerLib Test-ReportEvidence` now accepts, per taskee, ANY ONE of three
sources, and records which one in `oracle.earlyExit.reportEvidence[<taskee>].via`:

| via | what it is | live? |
|-----|------------|-------|
| `RPT` | the 2026-09-02 rule, UNCHANGED: a post-completion text report within `-ReportToleranceMeters` of the sampled POS | yes, but has never existed |
| `C2SIM-capture` | a C2SIM PositionReport for the taskee's OWN uuid, captured after that taskee's TASKCMPLT (`Get-ReportCaptureEvidence` over `reports-captured.log`) | **yes** since 2026-09-14 pm - sec 14.9 (was **no**) |
| `R1-applog` | an `R1 position reports: N sent, 0 skipped` line appearing BELOW that taskee's TASKCMPLT line in `vrfc2simapp.log` (`Get-AppLogPositionEvidence`) | yes |

`-SettleHoldSecs` (60) stays the FLOOR; an unsatisfied taskee still runs the window to its
cap, which is the safe direction, and its reason is printed every 30 s and ledgered.

**THE SUPERVISOR'S BRIEF ASKED FOR `reports-captured.log` AS THE LIVE SOURCE. IT COULD NOT BE
- until sec 14.9 made it one. The paragraph below is the state at db77917; read 14.9 with it.**
`tools/ListenReports/Program.cs` writes that file exactly once, in the closing
`await File.WriteAllTextAsync(outPath, ...)` after the listen ends. During an observation
window the file does not exist, so a live read returns nothing. It is nevertheless
implemented and is the AUTHORITY, because it is what the offline replay below and the
adjudication read, it carries the taskee's own uuid, and it dates BOTH the TASKCMPLT (which
the interface also publishes, as a `TaskStatus` report) and every position fix on ONE wall
clock. `R1-applog` is the live stand-in: the app log has no timestamps at all, but it is a
single totally ordered file, so line order answers "did a position-report round happen after
this taskee completed". (Making `C2SIM-capture` live would mean changing `ListenReports` to
flush incrementally, which is outside this change's edit surface; it is the better long-term
fix - DONE the same day, sec 14.9.)

TWO GUARDS ON `R1-applog`, both found by reading `MaybeSendPositionReports` rather than the
log line:

* `0 skipped` - a round WITH skips does not say WHICH units were sent, so it cannot be
  attributed to this taskee.
* `sides=both` - the side filter runs BEFORE either `skipped++` branch
  (`if (hostile ? !red : !blue) continue;`), so under `Vrf__PositionReportSides=blue` a
  HOSTILE taskee is neither sent nor skipped and the round still reads `N sent, 0 skipped`.
  That would have satisfied condition (4) on behalf of a unit that got nothing. Rounds now
  count only when the interface says it is reporting both sides - its default, and what
  every run in the record logs. Under a one-sided filter the satisfier never fires and the
  window runs to its cap, exactly as before 2026-09-14.

Checked against synthetic logs (`Get-AppLogPositionEvidence` directly):

```
sides=both AFTER the TASKCMPLT line -> True
sides=blue AFTER the TASKCMPLT line -> False
"5 sent, 1 skipped" AFTER           -> False
a clean round BEFORE the TASKCMPLT  -> False
```

### 14.2 The offline replay, and what it says

`Condition4Replay.ps1` (scratchpad) replays a finished run's capture through the REAL
functions - `Get-ReportCaptureEvidence`, `Update-CompletionState`, `Test-ReportEvidence`,
`Test-EarlyExit` - on the runner's own 5 s poll grid, with the RPT source deliberately
starved (`-TraceText ''`) and the app-log source withheld, so what fires is
`C2SIM-capture` ALONE.

```
=== 20260914T154243Z_run ===
  run start (UTC)  : 2026-09-14T15:42:43.348Z
  order taskees    : 001aa71b-4c26-a1ea-28b2-f7dfe8e76342   tasks: 1
  RECORDED OUTCOME : fired=False  window used 1200.7s of 1200s cap
  RPT rows in the trace: 0   (the old condition (4) could ONLY be satisfied by these)
  taskee 001aa71b-...-f7dfe8e76342: TASKCMPLT 2026-09-14T15:51:22.659Z, 147 PositionReports, last 2026-09-14T16:10:15.353Z
  replay grid      : 2026-09-14T15:51:17.659Z .. 2026-09-14T16:13:15.367Z every 5s
  WOULD HAVE FIRED at 2026-09-14T15:52:22.659Z   (hold 60s >= 60s, evidence IN)
    via=C2SIM-capture  C2SIM PositionReport at 2026-09-14T15:52:21.935Z is later than the capture own TASKCMPLT at 2026-09-14T15:51:22.659Z (40 fixes captured)
  recorded close   : 09/14/2026 16:10:20   replay close: 2026-09-14T15:52:22.659Z
```

That is **t+123.0 s of a 1200 s window instead of the full 1200.7 s - 18.0 minutes of dead
time per run**, and it matches the prediction in the brief (~15:52:23Z) to within one poll.

```
=== 20260914T130439Z_run ===
  RECORDED OUTCOME : fired=False  window used 1208.1s of 1200s cap
  RPT rows in the trace: 0
  taskee 001aa71b-...-f7dfe8e76342: TASKCMPLT 2026-09-14T13:08:54.159Z, 123 PositionReports, last 2026-09-14T13:28:02.242Z
  WOULD HAVE FIRED at 2026-09-14T13:09:54.159Z   (hold 60s >= 60s, evidence IN)
    via=C2SIM-capture  C2SIM PositionReport at 2026-09-14T13:09:49.238Z is later than the capture own TASKCMPLT at 2026-09-14T13:08:54.159Z (14 fixes captured)
```

**CORRECTION TO THE BRIEF.** `20260914T130439Z` was named as the negative control ("no
completion -> never"). It is not one: that run DID reach ALL-COMPLETE - its manifest records
`allCompleteUtc 2026-09-14T13:08:57.568Z` and `reason: "no RPT POSITION line for this
marking yet"` - and it failed on condition (4) alone, exactly like the other. With the fix it
closes at t+112.3 s instead of 1208.1 s, another 18.3 minutes. The real negative control is
`20260914T120444Z`, whose taskee never reported TASKCMPLT at all:

```
=== 20260914T120444Z_run ===
  taskee 670cfdb2-6c43-f267-ad7f-bd6e739def24: TASKCMPLT (none), 93 PositionReports, last 2026-09-14T12:23:18.197Z
  VERDICT: at least one taskee never reported TASKCMPLT - ALL-COMPLETE can never hold,
           so -StopWhenComplete NEVER fires and the window runs to its cap. (control)
```

The LIVE source was exercised separately, against the same runs' REAL app logs
(`LiveSourcesCheck.ps1`, scratchpad) - this is the path a running runner takes:

```
=== 20260914T154243Z_run : LIVE sources ===
  app log          : ...\vrfc2simapp.log (14527528 bytes)
  R1-applog satisfier for 001aa71b-4c26-a1ea-28b2-f7dfe8e76342 : True
  Test-ReportEvidence with the app-log source ONLY -> AllSatisfied = True
    via=R1-applog  the interface logged a COMPLETE R1 position-report round (0 skipped) after this taskee TASKCMPLT line
  ORDER reached the bus at : 2026-09-14T15:49:49.653Z

=== 20260914T120444Z_run : LIVE sources ===
  R1-applog satisfier for 670cfdb2-6c43-f267-ad7f-bd6e739def24 : False (no TASKCMPLT line for this taskee)
  Test-ReportEvidence with the app-log source ONLY -> AllSatisfied = False
```

### 14.3 Defect 2 - the thread sampler died before the order

`RunScenario.sh` started `SampleThreads.ps1` with `-MaxSec $((WATCH_SECS + 100))`.
`--watch-secs 0` means "let the runner derive the cap", so that arithmetic is **100
seconds** - and in run `20260914T164906Z` the sampler was gone long before PushOrder. The
wrapper now derives the effective observer window itself (the runner's own formula, printed
in the banner) and sizes the sampler from it:

    SAMPLER_MAX = EFF_WATCH + 75 + 360 + 100
                  EFF_WATCH  the derived (or explicit) observer cap
                  + 75       launchSettle 45 + preCheck 30, spent BEFORE the observers start
                             (SampleThreads starts its clock when the SIM APPEARS)
                  + 360      traceStopGrace 120 + appExit 120 + stopVrf 120  (teardown)
                  + 100      the historical margin

`SampleThreads.ps1` exits on its own when the sim exits, so a generous budget costs nothing
and a short one loses the measurement silently. `--sample-threads` semantics are unchanged,
except that a `--dry-run` no longer STARTS the sampler: with no sim to find it would idle for
ten minutes and, worse, could attach to a sim ANOTHER LANE is running. It now prints what it
would start.

`--watch-secs` also DEFAULTS TO 0 (derive) in the wrapper, and the derived value is printed:

```
  observers   : DERIVED 1460 (20+180+120+180+30+run 900+30+settle 0)
  observers   : EXPLICIT 900 (derived would be 1460)  *** BELOW the derived cap - the observers can end BEFORE the window does; pass --watch-secs 0 ***
```

Every explicit `--watch-secs` passed on 2026-09-14 was below the derived cap and earned the
runner's truncation WARN (`20260914T170824Z`: 900 < 1100).

### 14.4 Defect 3 - the interface was pointed at the VENDOR appData

With `-VrfAppDataDir` set, `LaunchVrf52.ps1` gives the sim and the gui `--appDataDir <the
relocated tree>`, but the runner still set
`Vrf__ConnectionConfigFile=C:\MAK\vrforces5.2d\appData\settings\connections\MAK-ONE-2025-Config.xml`
- the VENDOR copy (banner of run `20260914T164906Z`). The two files are byte-identical
today, so nothing has failed yet; the point is that the relocated tree is the one the sim
reads, and "identical today" is not a property the runner should depend on. The runner now
follows the relocation, and says which tree it used:

```
  (default)                Vrf__ConnectionConfigFile=C:\MAK\vrforces5.2d\appData\settings\connections\MAK-ONE-2025-Config.xml  (vendor appData)
  --vrf-appdata-dir ...    Vrf__ConnectionConfigFile=C:\C2SIM\vrf-appdata\appData\settings\connections\MAK-ONE-2025-Config.xml  (from the RELOCATED -VrfAppDataDir tree - the one the sim reads)
```

The pre-launch existence check and `inputs.connectionConfigFile` follow the same value, so a
relocated tree that is missing the file is refused before anything is launched.

### 14.5 Defect 4 - two clocks printed as one

`TASKCMPLT seen for 1/1 taskee(s) ... (t+77s)` is the OBSERVATION-WINDOW clock, which starts
when **PushOrder RETURNS** - up to `-PushOrderListenSec` (30 s) after the order actually
reached the bus. Every stage-8b message now says `t+Ns after PushOrder returned` and, when
`c2sim-bus.log` holds an `ORDER` record, appends `, Ms after the ORDER reached the bus at
HH:MM:SS.fffZ`. The moment is parsed by `RunnerLib Get-BusOrderUtc` (the first
`[HH:mm:ss.fff] ORDER (<n> chars)` header - NOT necessarily line 1: run `20260914T120444Z`'s
bus log opens with a `REPORT`) and ledgered as `clocks.orderOnBusUtc`. Measured on run
`20260914T154243Z`: the order reached the bus at `15:49:49.653Z`.

### 14.6 Defect 5 - the licence residual (RUNBOOK 0.5.15)

Two scripts still read the MACHINE scope alone, and `scripts\LaunchVrf.ps1` (the 5.0.2
profile) did worse than read it: on its live path it ASSIGNED `$env:MAKLMGRD_LICENSE_FILE`
from Machine, OVERWRITING the per-process pin the runner had just set - so a 5.0.2 run
resolved the renewed licence and then had the lapsed 15-sep-2026 one put back underneath it.
`LaunchVrf.ps1` (`$licUser`/`$licMachine`/`$licResolved`/`$licScope`, used by the
precondition report, the dry-run plan and the live assignment) and
`scripts\Probe52Reflection.ps1` now use the same User-then-Machine resolver as
`LaunchVrf52.ps1` (c8730e7), preserve an inherited value rather than replacing it with a path
that resolves to nothing, and name the scope they used. `LaunchVrf.ps1` additionally WARNS
when the two scopes disagree. The resolver now exists in SIX places; RUNBOOK 0.5.15 says
CHANGE ONE, CHANGE ALL SIX.

### 14.7 Gates run

| gate | result |
|------|--------|
| `Parser::ParseFile` on `RunnerLib.ps1`, `RunC2SimScenario.ps1`, `SampleThreads.ps1`, `LaunchVrf.ps1`, `Probe52Reflection.ps1`, `LaunchVrf52.ps1` | PARSE OK, 0 errors, all six |
| `bash -n scripts/RunScenario.sh` | OK |
| condition (4) replay, `20260914T154243Z` | fires at `15:52:22.659Z` = t+123.0 s (recorded: never, 1200.7 s) |
| condition (4) replay, `20260914T130439Z` | fires at `13:09:54.159Z` = t+112.3 s (recorded: never, 1208.1 s) |
| condition (4) replay, `20260914T120444Z` (control) | never fires - no TASKCMPLT for the taskee |
| live source over the real app logs | `R1-applog` True for `154243Z`, False for the control |
| `RunScenario.sh --dry-run` | exit 0; `observers : DERIVED 1460`; vendor connection config |
| `RunScenario.sh --dry-run --vrf-appdata-dir C:\C2SIM\vrf-appdata\appData` | exit 0; `Vrf__ConnectionConfigFile=C:\C2SIM\vrf-appdata\appData\...` |
| `RunScenario.sh --dry-run --watch-secs 0` | exit 0; DERIVED 1460 (identical to the default - 0 IS the default now) |
| `RunScenario.sh --dry-run --watch-secs 900` | exit 0; `EXPLICIT 900 (derived would be 1460)` + the BELOW-the-cap shout |
| `RunScenario.sh --dry-run --sample-threads` | exit 0; `WOULD start SampleThreads.ps1 ... -MaxSec 1995s` (was 100 s with `--watch-secs 0`) |
| `tests\RunnerTurnaround.Tests.ps1` | 222 passed, 1 failed - the SAME pre-existing failure ("the ready-path harvest is skipped when the launch crashed"). Baseline unchanged. |
| `rg -nP "[^\x09\x0a\x0d\x20-\x7E]"` on every touched file | clean; the checker reproduces on a dirty control |
| line endings | `.ps1` / `.md` CRLF, `scripts/RunScenario.sh` LF |

### 14.8 NOT covered

1. **No live run.** All of the above is a replay of finished runs and a set of dry runs. The
   first live run with `--stop-when-complete` must confirm that the window really closes, that
   `oracle.earlyExit.via` names `R1-applog`, and that the trace still covers the whole run.
2. **CLOSED by sec 14.9** (was: "`C2SIM-capture` cannot fire live until `ListenReports`
   flushes incrementally"). It does now. The residual risk is unchanged for `R1-applog`: if
   the app log's R1 line ever changes shape that satisfier goes silent - noisy, not
   dangerous, and visible in the manifest's `via` field.
3. **No test was added to `tests\RunnerTurnaround.Tests.ps1`** - that file was outside this
   change's edit surface. The replay harnesses live in the session scratchpad; their sources
   are short and their outputs are quoted above in full.
4. **The sampler budget is arithmetic mirrored from the runner's parameter defaults.** If a
   runner budget default moves, the wrapper's sum goes stale (the comment says so). Only the
   banner and the sampler size are affected - the runner still derives its own cap.

---

### 14.9 `C2SIM-capture` MADE LIVE - `ListenReports` appends as it listens (2026-09-14 pm)

**The defect.** `tools/ListenReports/Program.cs` built the whole capture in a
`List<string>` and wrote it in ONE call after the listen ended:

    await File.WriteAllTextAsync(outPath, string.Join("\n\n", captured));

So the file the `C2SIM-capture` satisfier reads did not exist until the observer had
already been told to stop - the evidence meant to CLOSE the observation window only
appeared after that window was over. The runner shipped the satisfier anyway (db77917) and
leaned on the `R1-applog` stand-in.

**The change.** The capture file is opened BEFORE `Connect()` with
`FileShare.ReadWrite | FileShare.Delete` (the same share mode `Read-LiveText` opens it
with) and each report is appended and FLUSHED in the `ReportReceived` handler, under a lock
that also assigns the record's `#n` so the numbering can never disagree with the file order.
`ListenReports --capabilities` now advertises `incremental-capture`, so a STALE DEPLOYED
BINARY is detectable instead of silently one-shot. The one-shot write survives only as the
fallback for a capture whose incremental write failed.

**The format did not change.** Records joined by `\n\n` (the separator precedes every
record but the first), no trailing newline, UTF-8 with no BOM, no newline translation - the
bodies keep the CRLF that `XElement.ToString()` gives them. `tools/analysis/run_census.py`
`read_reports`, `RunnerLib Get-ReportCaptureEvidence` and the offline replay are unchanged.

**Offline evidence** (scratchpad `listen_flush\`; no simulator, no C2SIM server - a
~110-line PowerShell STOMP stub on 127.0.0.1 feeds three synthetic C2SIM reports to a REAL
`ListenReports.exe`, so what is under test is the shipped binary, not a re-implementation of
its writer):

| what | legacy binary (pre-change source, built to a scratch dir) | new binary |
|------|-----------------------------------------------------------|------------|
| capture size after connect, 0 reports | file ABSENT | 0 B |
| after report 1 | file ABSENT | 761 B |
| after report 2 | file ABSENT | 1,540 B |
| after report 3 | file ABSENT | 2,303 B |
| after exit | 2,303 B | 2,303 B |
| bytes, both files, arrival stamps normalised | `sha256 4ce773e0ed70f904becfb4b01bed9c08d94575729086832af385722ccff561af` | SAME sha256 - IDENTICAL |

The second driver replays the real purpose: report 1 = a PositionReport for uuid `U`,
report 2 = a `TASKCMPLT` TaskStatus for `U`, report 3 = another PositionReport for `U`,
with the REAL `RunnerLib Get-ReportCaptureEvidence` / `Test-ReportEvidence` run over the
file after each one, through `Read-LiveText`'s share mode, WHILE `ListenReports` still
holds it open:

    NEW     after report 1  bytes= 739  posReports=1  TASKCMPLT=(none)      satisfied=False
    NEW     after report 2  bytes=1473  posReports=1  TASKCMPLT=18:15:48.4  satisfied=False
    NEW     after report 3  bytes=2214  posReports=2  TASKCMPLT=18:15:48.4  satisfied=True via=C2SIM-capture
    LEGACY  after reports 1/2/3          file ABSENT, satisfied=False
    LEGACY  after exit      bytes=2214  posReports=2  TASKCMPLT=18:16:05.6  satisfied=True via=C2SIM-capture

i.e. the satisfier that could only ever fire AFTER the window now fires DURING it, on the
same input and the same bytes.

**Partial last line.** Each poll above was repeated against the same text with its last 37
characters cut off. Every one parsed without error, and the truncated poll at report 2
reported `TASKCMPLT=no` - the cut removed the `<ReportingEntity>` line. That is the only
direction a torn tail can move the answer: a record's header is written before its body, so
an incomplete record can DROP evidence but never invent it, and the whole file is re-read on
the next poll.

**Regression.** `Condition4Replay.ps1 -RunDir runs\20260914T154243Z_run` gives the SAME
verdict as at db77917 - `WOULD HAVE FIRED at 2026-09-14T15:52:22.659Z`, `via=C2SIM-capture`,
`... later than the capture own TASKCMPLT at 2026-09-14T15:51:22.659Z (40 fixes captured)`.

**Gates.** `dotnet build tools\ListenReports -c Release` succeeded (0 errors; the 4 warnings
are the pre-existing `CA2024` in `C2SIMClientSTOMPLib.cs`), and the deployed
`tools\ListenReports\bin\Release\net10.0\ListenReports.exe` is the rebuilt one.
`tests\RunnerTurnaround.Tests.ps1`: **222 passed, 1 failed** - the SAME baseline sec 14.7
recorded, unchanged by this edit. HOST TRAP worth recording: run under a 32-BIT `pwsh`
(bare `pwsh` is the 32-bit build on this machine, PSHOME `C:\Program Files (x86)\PowerShell\7`)
the suite reports 219/4 instead - check 8d spawns the runner, and the runner REFUSES a
32-bit host with exit 2 before it ever reaches the injected terminating error. Nothing to do
with the code under test; use `C:\Program Files\PowerShell\7\pwsh.exe` by full path, as
`scripts\RunScenario.sh` does. ASCII check
`rg -nP "[^\x09\x0a\x0d\x20-\x7E]"` clean on every touched file, reproduced on a dirty
control; `.cs` / `.ps1` / `.md` all CRLF.

**Not covered.** No live run. The first live `-StopWhenComplete` run must confirm the window
really closes and that `oracle.earlyExit.reportEvidence[<taskee>].via` names `C2SIM-capture`
rather than `R1-applog`. Those four stale "ONLY at exit" strings in
`RunC2SimScenario.ps1` (the `-StopWhenComplete` help, the `Start-External` note, the manifest
`source` string and the comment above the live read) are CORRECTED - see sec 15.

---

## 15. STAGE 7d - THE READY GATE: push on the simulator's own signal (2026-09-14, fourth pass)

Supersedes nothing in sec 11 - `-PreOrderSettleSecs` still works exactly as it did - but it
demotes it. The settle was a GUESS, and the measurement that would have sized it arrived
after it was written: `docs/experiments/G7B_G8_RESULTS_2026-09-14.md` sec 1.5 and 3.

### 15.1 What the four runs of 2026-09-14 established

The sectorised navigation area is not usable when the entities are placed. It becomes usable
at the first

    VRF console [3] <object> (VRF_UUID:...): New Primary nav area: | <area>

row from a placed PLATFORM, and that row partitions the move-to nav gate PERFECTLY: in runs
A, B and D every goal issued before it failed `Is current point in nav area?` and fell to the
FEATURE planner on one straight part, and every goal after it passed and was mesh-planned.
Run D resolves the transition to 0.3 s. Two numbers follow, and they are why a fixed settle
cannot be right:

| | first placement -> area row | source |
|---|---|---|
| WARM file cache (4 back-to-back runs) | **9.1 - 12.1 s** | G7B_G8_RESULTS sec 1.5 table |
| COLD (attempt 4, after a C:\MAK filesystem scan) | **236.9 s** | G7B_G8_RESULTS sec 3b |

A 20x spread. `loadAllNavigationDataOnTerrainLoad = 1` moves it by nothing (sec 3a). Under
`CreationPolicy=AtOrder` the order reached the bus 4.7-7.7 s BEFORE the row in every run.

### 15.2 What was built

`-PreOrderGate NavArea` (wrapper: `--pre-order-gate nav-area`) makes stage 7d wait for that
row and push the order the moment it appears, instead of waiting a fixed number of seconds.
`-PreOrderGateTimeoutSec` (wrapper `--pre-order-gate-timeout`, default 300, range 30..1800)
bounds the wait. Default is OFF (`''`), so a default run's command line and behaviour are
byte-identical to every run in the record.

```
scripts\RunnerLib.ps1
  :711-779   Get-NavAreaRows / Get-PlacementRows - PURE parsers, which is what lets the
             same code be replayed offline over a finished run (15.4)
scripts\RunC2SimScenario.ps1
  :191-217   .PARAMETER PreOrderGate / PreOrderGateTimeoutSec
  :412-428   the parameters and why the gate is not the settle
  :1279-1311 $script:NavGate + Update-NavGateWatch - ONE incremental reader, ONE offset key
             ('applog-navgate'), called from BOTH the stage-7 loop and the gate loop
  :1737-1754 validation: the gate name (a typo must not fall through to "off"), the
             30..1800 timeout, and $PreOrderGateWarmSecs = 60
  :1881-1911 STAGE 0 REFUSAL when the object console is below 3
  :2484      the timeout is added to $DerivedWatchSecs where the settle is added, which is
             what carries it into the stage-3w watchdog budget ($WatchdogCoverSecs); the
             truncation WARN at :2493-2500 covers the gate too (15.7)
  :2490-2492 inputs.preOrderGate / preOrderGateTimeoutSec / objectConsoleNotifyLevel
  :3291      the stage-7 oracle loop stamps the PLACEMENT instant (see 15.3)
  :3352-3505 STAGE 7d itself; :3505 `if ($RunPreOrderSettle)` is the old settle, unchanged
scripts\RunScenario.sh
  :59-60 :89-98 :135-136 :205-224 :257 :266 :285-288 :301-310
```

THE GATE REQUIRES OBJECT CONSOLE >= 3 and stage 0 refuses it otherwise, at BOTH layers. The
row prints at level 3 and at no lower level, and `appsettings.json` ships
`Vrf:ObjectConsoleNotifyLevel = -1` (consoles OFF) - so without the check the overwhelmingly
likely first use of this flag would have been a run that burned its whole 300 s timeout with
VR-Forces up, and then either stopped or silently fell back. The runner resolves the level
from `Vrf__ObjectConsoleNotifyLevel` (what `--object-console` exports) and falls back to
`appsettings.json`; it refuses when it cannot resolve it at all.

GATE OR SETTLE, NEVER ONE AFTER THE OTHER. With both given the gate is in force and the
settle is the TIMEOUT FALLBACK only (`oracle.preOrderGate.fellBackToSettle`, plus a WARN
flag). A gate that never fires therefore degrades to the pre-existing behaviour instead of
failing a run that would otherwise have been fine; with no settle given, a timeout is a loud
NOT-READY `Stop-Runner 3` naming the area rows seen (0) and the elapsed time.

Manifest: `clocks.preOrderGate{Start,Fired,TimedOut}Utc`, `inputs.preOrderGate`,
`inputs.preOrderGateTimeoutSec`, and `oracle.preOrderGate` with the object, its uuid, the
area name, the row verbatim, the first placement, `placementToAreaSec`, `cacheState` and
`waitedSec`.

### 15.3 The one non-obvious part: WHERE the placement instant is stamped

The delta the gate prints - first PLACEMENT line to first area row - is the CACHE-STATE
indicator (~10 s warm, ~240 s cold), and it is the number to quote when a demo is slow to
start. It is measurable only because the watcher is called from the STAGE-7 ORACLE LOOP as
well as from the gate loop: the PLACEMENT lines land DURING the oracle wait, and the gate
starts after it. Stamping the placement at gate start instead would under-report the delta by
the whole length of the oracle wait - about 5 s in runs A/B/D - and could report a cold
machine as warm, which is the one thing this indicator exists to tell apart.

Both instants are the RUNNER's own observation times: `vrfc2simapp.log` carries no per-line
timestamp (one line in the whole file has a clock, `INFO[Util] ... loadConfigScript`). The
resolution is therefore the poll interval - 5 s in the oracle loop, 2 s in the gate loop -
which is stated in the manifest as `deltaResolutionNote` and is two orders of magnitude below
the 10 s / 240 s the indicator separates.

### 15.4 Offline replay of the detector over two finished runs

`Get-NavAreaRows` / `Get-PlacementRows` are pure, so the gate's detector can be run over a
finished run's `vrfc2simapp.log` with the shipped `RunnerLib.ps1` dot-sourced - the same code
the live gate polls with, not a copy. The wall clock comes from `watchvrf-trace.csv` (first
real-coordinate `POS` row = first placement; first `CON` row carrying the area text = the
acquisition), because the app log has none.

```
=== 20260914T164906Z_run ===
  DETECTOR (scripts\RunnerLib.ps1 over vrfc2simapp.log, 14.1 MB):
    placement rows : 7   first = UNIT 1222.MechPlt~PXY
    nav-area rows  : 7   first object = "1.BdeHQ~PXY"  level 3  area = "NavArea-ground-platform MojaveCOA"
    first row      : VRF console [3] 1.BdeHQ~PXY (VRF_UUID:a0400858-513d-6346-91c2-f37ed0762dd5): New Primary nav area: | NavArea-ground-platform MojaveCOA
  WALL CLOCK (watchvrf-trace.csv w= seconds):
    first placement  w=23.5
    first area row   w=35.6
    delta            12.1s  ->  WARM (threshold 60s)

=== 20260914T154243Z_run ===
  DETECTOR (scripts\RunnerLib.ps1 over vrfc2simapp.log, 13.9 MB):
    placement rows : 6   first = UNIT 1222.MechPlt~PXY
    nav-area rows  : 15   first object = "1.BdeHQ~PXY"  level 3  area = "NavArea-ground-platform MojaveCOA"
    first row      : VRF console [3] 1.BdeHQ~PXY (VRF_UUID:929db847-6be8-5c45-96eb-16a21e582798): New Primary nav area: | NavArea-ground-platform MojaveCOA
  WALL CLOCK (watchvrf-trace.csv w= seconds):
    first placement  w=29.5
    first area row   w=266
    delta            236.5s  ->  COLD (threshold 60s)
```

12.1 s WARM and 236.5 s COLD, against the 12.1 s and 236.9 s the results record derives from
a different pair of instruments (the working-set sampler and the fitted trace t0). The
detector found the row in both, with the object, the level and the area name.

### 15.5 Dry runs

```
--pre-order-gate nav-area
  observers   : 1760s CAP (derived 1760: preRoll 20 + appJoin 180 + initDispatch 120 +
                oracleGate 180 + pushOrderListen 30 + run 900 + trail 30 + preOrderGate 300)
  pre-order   : stage 7d READY GATE -PreOrderGate NavArea: PushOrder waits for the
                simulator's own "New Primary nav area" row (object console 4), timeout 300s,
                which IS in the derived cap; on TIMEOUT the run STOPS (exit 3) - pass
                -PreOrderSettleSecs N to make the timeout fall back to a fixed hold instead
  watches pid 696 ... at most 3995s (66.6 min): cover 1760 + settle 45 + preCheck 630 +
                pushInit 600 + teardown 360 + slack 600

--pre-order-gate nav-area --pre-order-settle 240
  observers   : 2000s CAP (derived 2000: ... + preOrderGate 300 + preOrderSettle 240)
  pre-order   : ... on TIMEOUT it falls back to the 240s -PreOrderSettleSecs hold
                (FALLBACK ONLY - gate first, never both in sequence)
  [DRY-RUN] on gate TIMEOUT would fall back to the 240s -PreOrderSettleSecs hold

--pre-order-gate nav-area --object-console 2
  wrapper : "--pre-order-gate nav-area needs --object-console 3 or 4; it is 2." exit 2
  runner  : [FAIL] -PreOrderGate NavArea REQUIRES object console >= 3 and it is 2 (from env
            Vrf__ObjectConsoleNotifyLevel ...) ... [FAIL] Aborting at validation. NOTHING was
            launched and NO server was contacted.   exit 2

(no gate flags)
  observers   : 1460s CAP (derived 1460: ... + run 900 + trail 30)     <- unchanged
  pre-order   : no hold and no gate (-PreOrderSettleSecs 0, -PreOrderGate off)
```

Also refused at stage 0: an unknown gate name (`-PreOrderGate Mesh`), a timeout outside
30..1800, and the gate with the console unresolvable. `--pre-order-settle 240` ALONE prints
the sec 11 stage-7d block unchanged, word for word.

### 15.6 The four stale capture strings, fixed

`ListenReports` has appended `reports-captured.log` incrementally since 0999eeb, but four
strings in `RunC2SimScenario.ps1` still said it writes only at exit: the `-StopWhenComplete`
help, the `Start-External` note on the tool, the `earlyExit.source` manifest string and the
comment above the live capture read. All four now say APPENDED-as-it-arrives and name the
commit. They under-promised rather than misbehaved, but a reader deciding whether the
C2SIM-capture satisfier can fire live would have been told the wrong thing by all four.

### 15.7 Adversarial review of this change - one defect, fixed

**The truncation WARN did not cover the gate.** `:2497` fired only when
`$PreOrderSettleSecs -gt 0`, so a gated run with an explicit low `-WatchSecs` would have had
its derived cap grow by 300 s and its trace truncated off the END of the observation window
IN SILENCE - the exact failure that WARN was written for (sec 12.4), reintroduced by adding a
second budget beside the settle. Condition now
`(($PreOrderSettleSecs -gt 0 -or $PreOrderGateOn) -and ...)` and the message names whichever
budget applies. Verified both ways in dry runs:

```
--pre-order-gate nav-area --watch-secs 900
  [WARN] -WatchSecs 900 ... below the derived cap 1760, which now includes the stage-7d
         budget (READY GATE timeout 300s) ...
--pre-order-settle 240 --watch-secs 900
  [WARN] -WatchSecs 900 ... the derived cap 1700 ... budget (settle 240s) ...
```

Weighed and NOT changed:
- **The placement instant could be stamped in the same poll as the area row** (delta 0.0 s ->
  "WARM"). It cannot mislead: `Update-NavGateWatch` runs at the TOP of the stage-7 loop
  iteration, so the placement is stamped on the same poll that breaks that loop at the
  latest - at most ~5 s late, against a cold interval of 236 s.
- **A row that arrives BEFORE the gate starts** (AtInit, or a very fast warm machine) is not
  missed: the stage-7 loop's watcher records it, and the gate's first test breaks
  immediately, pushing the order at once.
- **Offset-key collision.** The gate reads under `'applog-navgate'`; stage 8b's completion
  reader uses `'applog-completions'`. Separate offsets over the same file, by design.

### 15.8 Gates

`tests\RunnerTurnaround.Tests.ps1` under `C:\Program Files\PowerShell\7\pwsh.exe`:
**222 passed, 1 failed** - the same baseline sec 14.7 and sec 12 record, unchanged. The
runner and `RunnerLib.ps1` both parse with 0 errors (checks 7.1/7.2 of that suite, and
directly); `bash -n scripts\RunScenario.sh` clean and the file is still pure LF. ASCII
`rg -nP "[^\x09\x0a\x0d\x20-\x7E]"` clean on all five touched files, reproduced on a
dirty control first.

### 15.9 NOT covered

NO LIVE RUN. Everything above is dry runs, offline replay and the test suite. What only a
live run can show: that the gate actually fires (the replay proves the detector matches a
finished log, not that the incremental reader sees the row within 2 s of it being written);
that `placementToAreaSec` on the runner's own poll clock lands near the trace-derived figure;
and - the point of the whole exercise - that the first legs of an order pushed on the gate
are MESH-planned, i.e. that `Planned path has N points` replaces
`fail in action Is current point in nav area?` for goal 1. Until that run exists this is a
mechanism built on a measurement, not a demonstrated fix.

Also not addressed here: `CreationPolicy=AtInit` (run C) put the members in place before the
wait and got 32 of 32 gate successes. The gate does not make that choice; it only removes the
guess from the waiting.
