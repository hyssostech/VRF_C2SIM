# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-14 (after the nav-data falsification + docs cleanup).

READ THIS FIRST (2026-07-14 corrections, so the next session does not chase dead threads):
- **NAV DATA is FALSIFIED as the Mojave aggregate-movement cause.** Do NOT generate/load a nav mesh
  expecting it to unfreeze Mojave aggregates. Proof: Bogaland2 (Sweden, moves 5+ km) and
  TropicTortoise (Mojave, freezes) are the IDENTICAL streaming terrain + model set + page-in area,
  and NEITHER region has nav data; Sweden leader-path-plans with no NavMesh. The real cause is
  UNRESOLVED and region-specific (empty member offset routes at Mojave). Full record:
  docs/experiments/navdata_FALSIFIED_bogaland_vs_tt_2026-07-14.txt. Treat any doc text proposing a
  nav-mesh fix as superseded.
- Task (c) semantic mapping: Units 2 + 5 are DONE/behavior-verified at Sweden (2026-07-14). Unit 4
  (MoveIntoFormation) is the only remaining task-(c) item.
- appNo NEXT FREE is **3386** (not 3368). No appNos were consumed by the 2026-07-14 doc work.

STATUS: the OPUS_EXECUTION_PLAN backlog is COMPLETE (all six steps + the P4b live
pass), and the first post-backlog follow-up - the F3 straggler-timeout probe
(RESUME_PROMPT candidate (a)) - is DONE and CONFIRMED (2026-07-13 evening). A fresh
session PICKS THE NEXT work item; it does not re-execute finished work. Everything is
committed + pushed (port main + fork dev/sdk-fixes).

---

```
Resume the C2SIM VR-Forces -> .NET port. The OPUS_EXECUTION_PLAN backlog is COMPLETE
(six steps + P4b live pass) AND the F3 straggler-timeout probe is DONE (2026-07-13
evening, F3 CONFIRMED). This session PICKS THE NEXT work item - do not re-execute
finished work.

WHERE THE WORK LIVES (run `git log --oneline -1` for tips; do not trust prose hashes):
- PORT (source of truth + where you work): github.com/hyssostech/VRF_C2SIM.git,
  branch main. Local: C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\
  Software\Interfaces\VRF_C2SIM (nested submodule of the fork).
- FORK + SDK: github.com/hyssostech/OpenC2SIM.github.io.git, branch dev/sdk-fixes
  (submodule -> port main; SDK edits commit in the fork under Software/Library/CS/C2SIMSDK).
- DEPRECATED C++ interface (FROZEN parity oracle only, do NOT develop there):
  C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36 - still NO git
  remote (single-disk risk; private-remote decision pending with the user).

Before touching ANYTHING, read in the PORT repo, in order (treat as source of truth
over any summary or recollection):
  1. docs/START_HERE.md - current status (the 2026-07-13 bullets: Steps 1-6 + P4b live
     pass + the F3 PROBE result at top) + build/run + env knobs
  2. docs/UNIT_MOVEMENT_RESEARCH.md sec 4c - the scale-run record (F1 runaway, F2/F2b
     vacuous classes, F3 timeout race) AND the F3 PROBE result (straggler 900 <
     predecessor 1200 unblocks successors; predecessor-timeout skips 15 -> 2)
  3. docs/OPUS_EXECUTION_PLAN.md sec 0.3 (hard rules) + Appendix A (live-run preflight)
     + Appendix B (appNo ledger - NEXT FREE: 3386)
  4. docs/RUNBOOK.md secs 0/3/4/7/8 before ANY live run
Then `git log --oneline -3` + `git status -sb` in the port AND the fork.

ENVIRONMENT NOTES (2026-07-13 evening end-state): the F3 probe left the federation
CLEAN (post-run ResetVrf 3367 deleted its 1 race leftover). Run the standard pre-run
ResetVrf dry-run + sweep FIRST anyway (fresh appNos from 3368). License expires
2026-09-15. vrfGui was hung-but-backend-healthy (116 threads, do NOT kill it);
vrfSimHLA1516e + rtiexec healthy; container up.

CANDIDATE NEXT TASKS (pick one, or ask the user):
  (a) PARTIAL QUORUM follow-up (the lever F3 surfaced): FanOutCompletionFraction < 1.0
      so a unit completes on MAJORITY member arrival instead of waiting for the stuck
      minority - the natural next fix for the F2b stuck-member units (4-27, 5-20,
      B/5-20 completed at ~0 aggregate displacement; 856/HHC stuck at 7/18 even at
      900 s). Config-only live run on COA-STP1; telemetry-gated. NOTE: this changes
      WHEN a unit is declared complete, so it can MASK non-movement - watch F2b hardest.
  (b) F2b/F1 probe matrix: per-route 1-member probes at the Mojave region (which routes
      vacuous-complete, which overshoot; vrfSim.log `moveAlong() - empty route` oracle)
      - required before trusting member completions there. Heavy live-run effort on an
      already-diagnosed EXTERNAL terrain problem (verified R9).
  (c) Semantic mapping continuation (PORT.md sec 10 / SEMANTIC_MAPPING.md): Units 2/4/5
      BEHAVIOR verification via synthetic distinct-target orders - cleaner at the golden
      SWEDEN region (movement proven 3/3) to dodge the Mojave terrain confound. The
      product value-add; Units 1/2/3/5 done, Unit 4 (MoveIntoFormation) remaining.
  (d) Housekeeping: C++ repo private remote (USER decision); delete retained C++
      originals (migration step 1); SDK nuget decoupling; a live TASKCMPLT sample with
      P4b bundling ON (the one P4b gap, piggyback on any run).

Non-negotiables (unchanged): never force-kill a joined federate (StopIface clean stop);
never push init to a running app; do not restart the c2sim-server container habitually
(loopback test FIRST: raw TCP to 127.0.0.1:61613 <1 s); fresh appNo per join, recorded
in Appendix B (NEXT FREE 3386); RTI 4.6.1 + Machine-scope license + cwd bin64 +
--contentRoot for live; movement claims REQUIRE WatchVrf displacement (completions LIE
- R11/F2/F2b); bridge builds VS18-MSBuild-via-PowerShell, app builds
DOTNET_CLI_USE_MSBUILD_SERVER=false ... --disable-build-servers; keep START_HERE/PORT/
UNIT_MOVEMENT_RESEARCH/RESUME_PROMPT current AS work lands; after any context
compaction re-read the docs before deciding anything.

START by reporting: git state of port + fork, the eight-selftest baseline
(translator 18, parse-init 80/49/4, parse-order, report 16, sequencer 12, verb 35,
destack 20, fanout 36 - exe path has the win-x64 RID subfolder), and which task you
propose - then STOP for the supervisor's go-ahead.
```

---

Notes for the human pasting this:
- The prompt points at the docs rather than restating them, so it does not go stale.
  docs/START_HERE.md + UNIT_MOVEMENT_RESEARCH.md sec 4c carry the current state.
- All work is pushed (2026-07-13 evening): PORT `VRF_C2SIM.git` main, FORK
  `OpenC2SIM.github.io.git` dev/sdk-fixes (submodule tracks port main).
- The C++ repo (c2simVRFinterfacev2.36) STILL has no remote - the only golden-trace
  rig exists on one disk. Decide on a private remote.
- If much time has passed: expect drift (license expiry 2026-09-15, VR-Forces /
  container up-ness, the hung vrfGui, the tileserver on 8080). Check, do not assume.
