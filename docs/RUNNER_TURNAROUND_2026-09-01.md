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
2. the number of TASKCMPLT lines is >= the number of (Task, PerformingEntity) pairs
   in the order (so an order with two tasks for one performer needs two
   completions), AND
3. `-SettleHoldSecs` (default 60) have elapsed since the runner's poll that FIRST
   saw 1 and 2 satisfied.

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

The "after" column is a prediction, not a measurement: the confirming run must show
(a) `# STOP requested via stop-file` in the WatchVrf trace followed by a clean
resign line, (b) `stop requested via stop-file` in the ListenReports log and a
written reports-captured.log, (c) WatchVrf-trace end within ~TrailSecs + a few
seconds of StopIface, (d) StopVrf exit 0 with no stale federate on the next launch.
A miss on (a) or (d) is a STOP, not a tuning item.

## 5. Other behaviour changes worth knowing

- Stage 8b's poll sleep is clamped to the time remaining, so the window now ends at
  RunSecs instead of RunSecs + up to 30 s. For RunSecs that are multiples of 30 there
  is no practical difference.
- New Stage 0b (capability probe) runs the two tool binaries offline before Stage 1.
  It adds ~1-2 s. WatchVrf's `--capabilities` path is pure managed code (no bridge
  load, nothing joined).
- New parameters: `-StopWhenComplete` (switch), `-SettleHoldSecs` (0..86400,
  default 60), `-TraceStopGraceSec` (10..3600, default 120). `-RunSecs` is now
  documented as the window CAP.
- `-DryRun` prints the trace-stop mode, the stop-file path, the early-exit plan and
  the teardown plan; it creates no run directory and touches no ledger.

## 6. Verification record (offline, 2026-09-01)

- tests/RunnerTurnaround.Tests.ps1: 48 checks, all pass; fault injection (hold
  comparison flipped in a scratch copy) fails 3 checks and exits 1.
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
