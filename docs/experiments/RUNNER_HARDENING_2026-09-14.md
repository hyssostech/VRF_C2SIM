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
