# RE-BASELINE OF THE OFFLINE INSTRUMENTS ON VR-FORCES 5.2d - 2026-09-04

Plan Phase 2 ("re-baseline instruments FIRST on the new build before any
behavioural claim"). OFFLINE only: no launch, nothing under C:\MAK, no commit.
Captures (all 5.2d + RTI 5.0.1 rtiexec, execName MAK-ONE-2025, Traffic.scnx):

| capture | runs/launch52/ file | state | paired back-end log |
|---|---|---|---|
| 3856 | watchvrf_3856_rtiexec.txt | RUNNING | vrfSim_3854_20260904T102537Z.log |
| 3857 | watchvrf_3857_rtiexec_nodevaddr.txt | RUNNING, device-address=none | same |
| 3859 | watchvrf_3859_smoke.txt | PAUSED | vrfSim_3858_20260904T111511Z.log |

Method: a scratch repo under the session scratchpad, `runs/<id>/` holding
`watchvrf-trace.csv` (the capture), `bin64-vrfSim.log` (the paired vendor log),
an empty `reports-captured.log`, a stub `run-manifest.json`. Nothing in `runs/`.

## 1. Verdict per instrument

| instrument | reads | on 5.2 BEFORE | after |
|---|---|---|---|
| run_census.py | watchvrf-trace.csv (+ reports, vendor log) | PARSES, no change needed | doc only |
| frame_gaps.py | bin64-vrfSim.log | CRASH `IndexError: sims[0]` | loud diagnosis, exit 1 |
| step_profile.py | vendor log + trace | CRASH `IndexError: sims[0]` | loud diagnosis, exit 1 |
| phase_timing.py | vendor log + trace | **SILENT FALSE GREEN**, exit 0 | diagnosis, exit 1 |

The new `# t=` fields (`ent= agg= env= ctl= extattr= waitext= discovered=
backends=`), the `# DIAG` lines and the vendor `Printing Reflected Object List
counts` block are all non-POS and were already skipped. NOT ONE broke a parser.

## 2. What actually broke: the vendor log has NO timestamps on 5.2

MEASURED: all **14** `runs/launch52/vrfSim_*.log` contain **zero** matches of
`[Www Mmm D HH:MM:SS YYYY] <sim>.mmm`; the 5.0.2 comparator has 582,691.
ASSUMED, not proved offline - the cause. Leading hypothesis is MIGRATION_DIFF
row A4: 5.2d ships `vrfSim.mtl enableLogFileTimestamps 0` where our 5.0.2 box
was edited to 1; the per-process equivalent is `--enableLogFileTimestamps`,
"Print a timestamp for each line in a log file" (5.2 Users Guide sec 5.2).
Competing hypothesis, NOT excluded: the 5.2 back end was started directly with
`--logFileName` while 5.0.2 went through vrfLauncher, and the two logging paths
format differently. Do NOT read the 5.2 log's opening `Using relative
timestamps` banner as evidence for either - that string is the separate
`--useAbsoluteTimeStamps` PDU option, whose UG text reads verbatim "Otherwise
uses relative timestamps". Both hypotheses share one first action: turn
`enableLogFileTimestamps` on and re-check.

**Consequence: there is NO 5.2 frame/clock baseline yet.** frame_gaps TEST A,
TEST B and the LS clock slope are undefined on every capture so far.
PREREG_R9_52 must not cite a 5.2 frame mode until stamps are restored.

Second defect found in passing: `phase_timing.STAMP_RE` hard-coded
`[Tue Sep  1 ... 2026]`, so it returned zero stamps on ANY log from another day
- including every 2026-09-02 5.0.2 run - while still printing a report and
exiting 0. Positive control on `20260902T165144Z_run`: HEAD `stamped lines 0`,
fixed `stamped lines 1201`, equal to `frame_gaps`' independent count for that
file. Treat pre-existing phase_timing numbers as 09-01-only.

## 3. The 5.2 census baseline (for PREREG_R9_52 to cite)

`run_census.py` object census, run through the scratch layout:

| capture | posUuids | everReal | poleOnly | POS lines | NaN lines |
|---|---|---|---|---|---|
| 3856 RUNNING | 102 | 83 | 19 | 428 | 114 |
| 3857 RUNNING | 109 | 90 | 19 | 437 | 114 |
| 3859 PAUSED  | 63  | 44 | 19 | 378 | 114 |

Placeholder encodings, measured: `(90,-90)` lines **0**, `|alt| > 1e8` lines
**0** in all three - neither 5.0.2 placeholder form occurs on 5.2. The only
never-real form is `(NaN, +90.000000, NaN)` - note lon **+90**, where the 5.0.2
NaN form was lon **-90** - and per COLDSTART_REVIEW_RTIEXEC_2026-09-04 these are
the **19 control objects**, not un-placed entities. 114 = 19 x 6 samples.

Cross-checks, exact, 36/36 samples, no exceptions: real-fix POS lines at sample
t == that sample's `# t=` `ent=`; NaN POS lines == `ctl=` == `env=` == 19;
`readable` == `ent` + `ctl` in every `# t=` line; `ent`/`ctl` agree with the
vendor `printReflectedObjectCounts` block (3856 62/19/81, 3859 44/19/63; 3857
drifts by 1, the block being printed after the last sample). POS cadence: 6
samples per 60 s, first at t=3, deltas 10.0-10.1 s - as 5.0.2 (280 samples).

Motion (net first->last displacement per uuid, great-circle): 3856 RUNNING, 69
uuids with >=2 real fixes, **69** moved > 1 m, median 473 m, max 944 m over
~50 s; 3857, 72 uuids, 71 moved, median 515 m, max 986 m; 3859 PAUSED, 44
uuids, **0** moved > 1 m, min = median = max = 0.00 m. 3859 is thus a clean
negative control: sampling continues on cadence with a frozen sim, `ent` flat
at 44, while the running captures churn entities (3856 `ent` 44 -> 62). That
churn is why `everReal` (83, 90) EXCEEDS the final `ent=` (62, 54) - a union
over the run, not a headcount.

## 4. Changes made (3 files, all in tools/analysis)

- `step_profile.py` `vendor_log()`: bare `IndexError` on an unstamped log ->
  `SystemExit` naming the file, the pattern that matched nothing, and
  `enableLogFileTimestamps`. `frame_gaps.py` inherits it (it imports
  `vendor_log`) and needed no edit of its own.
- `phase_timing.py`: generalised `STAMP_RE`'s date the way `step_profile.py:41`
  already was; loud "NO VENDOR TIMESTAMPS ... NOT an empty task list" block and
  a non-zero exit when zero stamps parse.
- `run_census.py`: docstring only. No code path changed.

### Negative control (the "identical" claim)

Six 5.0.2 outputs captured before and after the edits - `run_census --gate
rung2`, `--gate quiet`, `frame_gaps` on `20260902T165144Z_run` and
`20260902T183135Z_run`, `step_profile` and `phase_timing` on
`20260901T221227Z_run` - then `diff -r <scratch>/before <scratch>/after` -> no
output, exit 0. Both gates still PASS; `frame_gaps` still reproduces LS slope
0.2652 and TEST A 89/89 = 100.0% on `20260902T165144Z_run`.

## 5. Residual risks / next

1. `--enableLogFileTimestamps` is UNVERIFIED live, twice over: it is only the
   leading hypothesis (sec 2), and even if it is the cause, the docs promise
   "a timestamp" without saying the format is the absolute
   `[Www Mmm D HH:MM:SS YYYY]` STAMP_RE needs. Check the first 5.2 run with it
   on before any frame claim; if the format differs, STAMP_RE needs a 5.2
   branch, not a patch over a guess.
2. `step_profile.py` is COA-shaped past the vendor log: hard-coded `M1A2 n`
   name lists and a `statistics.mean` over RPT intervals that raises on a
   scenario with no RPT records (Traffic has none). Phase 3 work; untouched
   here, because the 5.2 captures never reach that code.

## 6. 2026-09-06 additions (found while preparing COA-STP1 on 5.2)

3. THE 5.2 VENDOR LOG HAS NO PER-OBJECT LINES. 5.0.2 printed `Locally Simulated:
   <name> (VRF_UUID:...) using parameters: ...` for every object (rung 2: 1,841 lines,
   incl. every `<unit>_R<n>` sub-route). 5.2d prints exactly 2 such lines (GlblTerrDmg,
   GlobalEnv) even at `--notifyLevel 4` (run T 160640Z). So `run_census.py`'s sub-route
   census had no source on 5.2. FIXED: `subroute_census` now also reads the WatchVrf
   `CON` rows - a subordinate's console at level 3 prints `Task n name and parameters:
   Move-Along Route: "<parent>_R<n>"` (PREREG_CONSOLE_CHANNEL 6.3) - and the vendor log
   path falls back to the manifest's `inputs.vrfProfile.vendorLog.harvestedTo`. Gated:
   rung2 and quiet still PASS; run C 161714Z reads `114.MechCoy: [R0, R1, R2]`. It only
   works when `Vrf:ObjectConsoleNotifyLevel >= 3` was set for the subordinates WE create
   (the members' level is separate, `Vrf:ObjectConsoleMemberNotifyLevel`, and must stay
   at the vendor default at scale - PREREG_CONSOLE_CHANNEL 6.2 volume figures).
4. NO POSITION REPORTS REACH THE C2SIM BUS ON 5.2. rung 2 captured 1,536
   `PositionReportContent`; every 5.2 run captured only its TASKCMPLT task-status
   reports (run E: 3 of 3). Cause, read not inferred: the app's only position path is
   `OnVrfTextReport` relaying `POSITION "<name>" <lat> <lon>` VRF text reports, which the
   5.0.2 fixture's C2simEx SMS Lua tracking script emitted (WatchVrf `RPT` rows: 20,784 in
   rung 2, 0 in every 5.2 run; the 5.2 fixture is on EntityLevel.sms, DIFF row C2). The
   C++ oracle did NOT depend on that script for its periodic reports: C2SIMinterface.cpp
   :388-460 polls `getUnitGeodeticFromSim` (reflected object -> state repository ->
   location) every `reportInterval` and bundles. Consequences: (a) `run_census.py`
   `reports`/`net_km` and the runner's `-StopWhenComplete` rule 4 (`RPT POSITION` later
   than the `TSK` record) are BLIND on 5.2 - do not use `-StopWhenComplete` on 5.2 until
   fixed; WatchVrf `POS` is the position oracle; (b) the fix is to port the oracle's poll
   (bridge `TryGetEntityGeodetic` for entities plus an aggregate read), gated on
   `PositionReportSeconds > 0` like the oracle's `reportInterval` - queued as slice R1.
5. `tests/RunnerTurnaround.Tests.ps1`: 5 checks fail on HEAD before any 2026-09-06
   runner change (the LaunchVrf52 harvest / -DryRun terminating-error cluster; 218 pass).
   Pre-existing; not touched by `-BackendNotifyLevel` / `-ClientId`.
6. `frame_gaps.py` IS BLIND ON 5.2: it opens `<run>/bin64-vrfSim.log` (absent - the 5.2 log is
   harvested to runs/launch52) and needs the wall stamps that the 5.2 vendor log does not carry
   (sec 2; `--enableLogFileTimestamps` still unverified). REPLACEMENT for the sim/wall ratio:
   `tools/analysis/sim_ratio.py <run_dir>` - the sim narrates its own clock at the head of its
   level-3/4 console lines ("100.199 Task 0 starting subtask ...") and WatchVrf stamps each CON
   row with wall seconds; the LS slope of (wall, sim) is the ratio. Validated: run C 161714Z
   7.57x (486 samples, resid sd 4.3 s), run T 160640Z 10.49x (3,473 samples) - the R9 FFRTC
   ratio class the 5.0.2 record put at ~9x; run E (no console) correctly reports no samples.
   Needs `Vrf:ObjectConsoleNotifyLevel >= 3` on at least one of our objects.
