# RUNNER TURNAROUND (2026-09-01) - end the trace with the window; optional early exit

Status: IMPLEMENTED OFFLINE on branch `runner-turnaround`, PENDING ONE CONFIRMING
LIVE RUN. Nothing here changes what the runner measures; it changes how long the
runner waits after the measurement is over. Default behaviour is unchanged except
that the observers now end with the window (item 1). Early exit (item 2) is OFF by
default.

Related: docs/HEADLESS_RUN_PLAN.md sec 2 (the runner), docs/RUNBOOK.md 0.5.11,
docs/HANDOFF_2026-09-01_R9_COMPLETE.md NEXT item 5, tests/RunnerTurnaround.Tests.ps1.

## 1. The two measured wastes

From the manifests of runs 20260901T211310Z_run (P2c, RunSecs 900) and
20260901T221227Z_run (P3, RunSecs 420). All clocks UTC.

| stage                        | P2c 900 s        | P3 420 s         |
|------------------------------|------------------|------------------|
| runner start                 | 21:13:10.163     | 22:12:27.065     |
| WatchVrf-trace / ListenReports start | 21:15:01 | 22:14:18         |
| order pushed                 | 21:15:33         | 22:14:51         |
| window open (order + 30 s listen) | 21:16:03    | 22:15:21         |
| last TASKCMPLT seen          | ~+184 s (3/3)    | never (2/3)      |
| observationEndUtc            | 21:31:06.168     | 22:22:23.147     |
| StopIface                    | 21:31:06.169     | 22:22:23.149     |
| VrfC2SimApp exited           | 21:31:09.937     | 22:22:27.117     |
| WatchVrf-trace / ListenReports ended | 21:39:27.0 | 22:30:44.44    |
| StopVrf                      | 21:39:27.053 - 21:39:33.088 | 22:30:44.469 - 22:30:50.737 |
| manifest saved               | 21:39:33.181     | 22:30:50.877     |
| **total wall time**          | **26 min 23 s**  | **18 min 24 s**  |
| dead time observationEnd -> StopVrf | **8 min 21 s** | **8 min 21 s** |
| window spent after outcome decided (P2c) | ~716 s of 900 | n/a (2/3, ran to cap correctly) |

Waste 1 (both runs, identical): the observers' duration argument is the SUM of every
stage's worst-case budget (PreRoll 20 + AppJoinTimeout 180 + InitDispatchWait 120 +
OracleGateTimeout 180 + PushOrderListen 30 + RunSecs + Trail 30 = 1460 / 980). The
real stages between observer start and window open took ~62 s, not 530 s, so the
observers outlived the window by 530 - 62 + (30 trail) ~ 8.3 min, and the teardown
(Complete-Background, which never kills) waited for them. 8 min 21 s per run, in
every run, regardless of RunSecs.

Waste 2 (P2c): all three taskees had reported TASKCMPLT by t=+184 s of a 900 s
window; the remaining ~12 min of window added plateau samples only. P3 shows the
other side: with 2/3 complete the window MUST run to the cap, and did.

## 2. Item 1 - end the trace with the window (default ON, no switch)

### Mechanism: stop-file, capability-probed

- The runner reserves `<runDir>\observers.stop`. It does not exist at observer start
  (both tools REFUSE a pre-existing stop file with exit 2 and join/connect nothing,
  so a stale file can not silently produce an empty trace).
- `WatchVrf.exe <appNo> <durationSecs> <sampleSecs> <federation> --stop-file <path>`
  polls the path once a second inside its tick loop; when it appears it emits
  `# STOP requested via stop-file at t=<s>s (duration cap was <d>s)` and falls
  through to the SAME `bridge.Stop()` resign path it always used. Federate
  behaviour is unchanged: it resigns cleanly, nothing is killed.
- `ListenReports.exe <secs> <outPath> --stop-file <path>` likewise: 1 s poll,
  `stop requested via stop-file ...`, then the normal Disconnect + write of
  reports-captured.log.
- The duration argument is now the CAP (same formula, unchanged): if the runner dies
  mid-run the observers still end on their own, exactly as before.
- Teardown order (runner finally-block): StopIface -> wait for the app to exit ->
  hold until StopIface + TrailSecs (Get-TraceStopWaitSecs) -> touch the stop file ->
  Complete-Background with a wait of `-TraceStopGraceSec` (default 120 s) instead of
  the full cap + 120 -> StopVrf. The trace therefore still covers
  preroll -> init -> order -> window -> trail, and `clocks.traceStopRequestedUtc` in
  the manifest records the touch.
- Grace expiry (review F1, docs/experiments/REVIEW_RUNNER_TURNAROUND_2026-09-01.md):
  if an observer has NOT exited when the grace runs out it may not have seen the
  file and may still be joined. Teardown then WARNs and KEEPS WAITING - never kills -
  up to the observer's own cap: stage `startedUtc` + the duration argument + a 30 s
  margin (`Get-ObserverCapRemainingSecs`; P2c's WatchVrf-trace exited 5.5 s after
  start + cap, so 30 s covers the observed overshoot). StopVrf runs only after that.
  If it is STILL running past cap + margin it is recorded `still-running` (WARN) and
  StopVrf proceeds; the next run's Stage 1 inventory then REFUSES to launch while a
  WatchVrf or ListenReports process exists (report, never kill - wait it out). The
  same two names are inventoried after teardown (`preflight.postRunObservers`).
- Pre-existing stop file (review F5): `<runDir>\observers.stop` already existing
  means the whole run directory collides with an earlier run (same UTC second). The
  runner refuses with exit 2 BEFORE creating, ledgering or launching anything, and
  again (Stop-Runner 3, through teardown) immediately before Stage 5 if the file
  appeared meanwhile. It is never deleted.
- Capability probe (Stage 0b, offline, before any launch): the runner runs each tool
  with the sole argument `--capabilities` (30 s timeout, never killed on timeout ->
  treated as unsupported) and passes `--stop-file` ONLY if the tool exits 0 and
  prints the token `stop-file`. A deployed binary that predates the flag rejects
  `--capabilities` as an unknown option (exit 2) -> the runner falls back to the
  pre-turnaround duration-only behaviour, byte-identical argument lists, and WARNs
  (`traceStop.mode` = `duration-only` or `partial` in the manifest). This is the
  guard against repeating the -ConsoleLogDir landmine (an unknown flag kills the
  oracle stage with exit 2 after a full launch cycle).

### Alternatives considered and rejected

| alternative | why not |
|-------------|---------|
| Tighter duration computed from actual stage times | Impossible at observer start: the observers are started BEFORE init (Stage 5, so the trace has a preroll) and the window's end depends on stages that have not run yet. Any fixed number is either a cap (the waste) or a guess that can truncate the trail. |
| Console Ctrl-C / CloseMainWindow / GenerateConsoleCtrlEvent | Start-External redirects the tools' stdio into files; there is no console window to close and Ctrl-C delivery to a redirected detached child is not reliable from PowerShell. A misfire is either a no-op (back to the waste) or a hard stop that skips the resign - exactly the outcome the never-kill rule exists to prevent. |
| stdin sentinel | Would require Start-External to keep a writable stdin pipe to each tool for the whole run and the tools to read stdin from a second thread inside a 50 ms tick loop. More moving parts, harder to test offline, and a closed pipe on runner death would look like a stop request. The stop file is inert if the runner dies. |
| Shorter cap via -WatchSecs | Already exists as an override; it trades the waste for a risk of truncating the trail and defeats the "observers end on their own if the runner dies" safety net. |
| Kill the observers after the window | Forbidden. WatchVrf is a joined federate; killing it leaves a stale federate and the next launch hangs at RTI join (RUNBOOK sec 0). |

## 3. Item 2 - optional early exit `-StopWhenComplete` (default OFF)

OFF by default for comparability with the record (every run so far used a fixed
window). Turn it on for probes; keep ONE canonical fixed-window run per milestone
(HANDOFF NEXT item 5).

### Exact criterion

The window closes early when ALL of:
1. every distinct PerformingEntity UUID in the pushed order has at least one
   `SENT TASK STATUS REPORT (TASKCMPLT) taskee=<uuid> task=<uuid>.` line in the
   interface log (`<runDir>\vrfc2simapp.log`), AND
2. the number of TASKCMPLT lines WHOSE TASKEE IS IN THE ORDER is >= the number of
   (Task, PerformingEntity) pairs in the order (so an order with two tasks for one
   performer needs two completions). Lines for taskees outside the order are ignored
   entirely - neither counted nor stamped (review F2), AND
3. `-SettleHoldSecs` (default 60) have elapsed since the runner's poll that FIRST
   saw 1 and 2 satisfied.

What rule 2 means for multi-task orders (review F3):
- Two tasks dispatched SIMULTANEOUSLY to the same taskee are SUPERSEDED by VR-Forces
  - the interface logs "the old task will not complete"
  (src/VrfC2SimApp/VrfC2SimService.cs:951-955) and only ONE TASKCMPLT line ever
  appears. The count can never reach the pair count, so `-StopWhenComplete` is
  INERT for such an order: it never fires and the window runs to `-RunSecs`. That is
  the safe direction (no truncation), but do not expect a turnaround gain from the
  switch on an order shaped like that.
- SEQUENCED (gated) tasks for one taskee DO complete one after another through the
  app's sequencer (`_sequencer.CompleteTask`, VrfC2SimService.cs:1228) and produce
  one line each; rule 2 is right for them and the switch fires after the last.
- A FAN-OUT task (a unit whose members each get a VR-Forces task) is counted ONCE:
  member completions are swallowed and a single unit-level line is synthesized
  (SynthesizeUnitCompletion, VrfC2SimService.cs:1183-1203).

RunSecs remains the cap. The runner polls every 5 s when the switch is on (30 s
otherwise). The app log carries no timestamps, so "time of last completion" is
"the poll that first saw it" - late by at most one poll interval, which only
LENGTHENS the hold. If the interface process dies the window still runs to the cap
(FAIL flag, early exit disabled). If the order yields zero taskees (unparseable or no
PerformingEntity) the runner WARNs at Stage 0b and the switch never fires.

Why the completion source is the app log and not reports-captured.log:
tools/ListenReports writes its capture ONCE, at exit; during the window it does not
exist. The app's TASKCMPLT line is written when the report is SENT
(src/VrfC2SimApp/VrfC2SimService.cs), so it is the earliest and only live signal.
Caveat (RUNBOOK sec 3): redirected stdout may be block-buffered; that delays the
sighting and again only lengthens the hold.

Manifest (`oracle.earlyExit`): enabled, source, criterion, taskees, taskCount,
settleHoldSecs, windowSecsCap, fired, firstSeenUtc (per taskee), allCompleteUtc,
closedUtc, windowSecsUsed, completionLinesSeen.

## 4. Expected budget after the change

| | before (measured) | after, fixed window | after, -StopWhenComplete (P2c-like) |
|---|---|---|---|
| runner start -> window open | ~173 s | unchanged | unchanged |
| window | RunSecs | RunSecs | ~184 + 60 = ~244 s (cap RunSecs) |
| window end -> observers end | ~501 s (P2c) / ~501 s (P3) | ~31 s (trail 30 + 1 s poll) | ~31 s |
| observers end -> manifest saved (StopVrf) | ~6 s | ~6 s | ~6 s |
| **P2c-shaped run, RunSecs 900** | **26 min 23 s** | **~18 min 33 s** | **~7 min 34 s** |
| **P3-shaped run, RunSecs 420** | **18 min 24 s** | **~10 min 33 s** | ran to cap (2/3): ~10 min 33 s |

MEASURED 2026-09-02 (run 20260901T235823Z, R9 1x, -RunSecs 420 -StopWhenComplete,
docs/experiments/PREREG_RUNNER_CONFIRM_2026-09-01.md sec 6): start -> window open
144 s; window 246 s (all-complete at +186 s + 60 s hold); window end -> observers end
33 s; observers end -> manifest 6 s; TOTAL 7 min 9 s (predicted ~7 min 34 s). (a)-(d)
below all held. One interaction found: SettleHoldSecs 60 equals the ~60 s VR-Forces
text-report cadence, so the post-completion report round (~44 lines over ~10 s) was cut
mid-emission by StopIface and the company's last RPT predates its completion - POS==RPT
cannot be adjudicated from a -StopWhenComplete run until the hold rule is revised
(supervisor decision; options: hold >= 90 s, or hold until every taskee has a
post-completion RPT). POS endpoints matched P2c to 0.00-0.27 m.

The "after" column was a prediction, since measured above: the confirming run must show
(a) `# STOP requested via stop-file` in the WatchVrf trace followed by a clean
resign line, (b) `stop requested via stop-file` in the ListenReports log and a
written reports-captured.log, (c) WatchVrf-trace end within ~TrailSecs + a few
seconds of StopIface, (d) StopVrf exit 0 with no stale federate on the next launch.
A miss on (a) or (d) is a STOP, not a tuning item.

## 5. Other behaviour changes worth knowing

- Stage 8b's poll sleep is clamped to the time remaining, so the window now ends at
  RunSecs instead of RunSecs + up to 30 s. For RunSecs that are multiples of 30 there
  is no practical difference.
- Stage 1's pre-flight inventory now also refuses (exit 2, never kills) on a leftover
  WatchVrf or ListenReports process (review F1), and the post-teardown inventory
  reports them (`preflight.existingObservers` / `postRunObservers`).
- A pre-existing `<runDir>\observers.stop` is refused with exit 2 before anything is
  created (review F5) - it means a run-directory collision, not something to delete.
- ListenReports' 1 s poll clamps a negative remaining delay to break (review F4).
- New Stage 0b (capability probe) runs the two tool binaries offline before Stage 1.
  It adds ~1-2 s. WatchVrf's `--capabilities` path is pure managed code (no bridge
  load, nothing joined).
- New parameters: `-StopWhenComplete` (switch), `-SettleHoldSecs` (0..86400,
  default 60), `-TraceStopGraceSec` (10..3600, default 120). `-RunSecs` is now
  documented as the window CAP.
- `-DryRun` prints the trace-stop mode, the stop-file path, the early-exit plan and
  the teardown plan; it creates no run directory and touches no ledger.

## 6. Verification record (offline, 2026-09-01)

- tests/RunnerTurnaround.Tests.ps1: 65 checks, all pass (48 at review + 17 for the
  review fixes: F2 stray-taskee lines x4, F1 cap-fallback arithmetic + manifest
  stamp parse x10, F1 wiring via the runner's AST x3); fault injection in a scratch
  copy: hold comparison flipped -> 3 fail; F2 filter removed -> 4 fail; cap margin
  dropped -> 3 fail; one -CapSecs call site removed -> 1 fail; each exits 1.
- New WatchVrf: `--capabilities` exit 0 (capabilities, con-selftest, stop-file);
  `--con-selftest` ALL CHECKS PASSED; pre-existing stop file refused exit 2 ("Nothing
  joined."); `--bogus`, missing value, extra argument each exit 2.
- New ListenReports: `--capabilities` exit 0 (capabilities, stop-file); pre-existing
  stop file refused exit 2 ("Nothing connected."); `--bogus`, missing value each exit 2.
- Old (deployed, pre-turnaround) binaries: `--capabilities` -> `unknown option(s)`
  exit 2 for both -> runner mode `duration-only`, argument lists byte-identical to
  the record (`3658 1160 2 CWIX-2024` / `1160 <reports path>`).
- Dry run exit 0 in both modes. Because a foreign VR-Forces instance was up on the
  machine, Stage 1 (correctly, pre-existing behaviour) refused even in -DryRun; the
  dry runs were exercised through a scratch harness stubbing Get-Process for the
  three vrf process names only.
- NOT verified offline (needs a federation + STOMP): the actual resign on stop-file
  touch and an early exit firing on live completions. Both are the confirming run's
  job.
