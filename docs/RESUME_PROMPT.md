# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-13 (PLAN EXECUTED: all six
docs/OPUS_EXECUTION_PLAN.md steps AND the P4b live pass are DONE and pushed.
P4a proven live twice (zero 10048); Step-2 quorum/timeout proven live; the
42-task scale run produced the mixed movement verdict with findings F1-F4;
the c2sim-server ingests multi-content report bundles. The plan doc is now
largely HISTORICAL - its sec 0 hard rules + Appendices A/B stay operative for
any live run.)

Intended model topology (see the project memory note): a frontier model (Fable 5)
or the user supervises; Opus-class executes. The supervisor holds the gates
(GATE-DIFF / GATE-ENV / GATE-VERDICT) and runs its own scheduled ground-truth
checks during live windows - executor timed waits MISSED their wake-ups three
times on 2026-07-13; poll evidence files directly, do not trust a single
monitor event.

---

```
Resume the C2SIM VR-Forces -> .NET port. The OPUS_EXECUTION_PLAN backlog is
COMPLETE (all six steps + the P4b live pass, 2026-07-13, all pushed). This
session picks the NEXT work item - do not re-execute plan steps.

WHERE THE WORK LIVES (run `git log --oneline -1` for tips; do not trust prose):
- PORT (source of truth + where you work): github.com/hyssostech/VRF_C2SIM.git,
  branch main. Local: C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\
  Software\Interfaces\VRF_C2SIM (nested submodule of the fork).
- FORK + SDK: github.com/hyssostech/OpenC2SIM.github.io.git, branch dev/sdk-fixes.
- DEPRECATED C++ interface (FROZEN parity oracle only):
  C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36 - still NO
  git remote (private-remote decision pending with the user).

Before touching ANYTHING, read in the PORT repo, in order:
  1. docs/START_HERE.md - current status (the 2026-07-13 execution bullets:
     Steps 1-6 + Step-5 verdict + P4b live pass) + build/run + env knobs
  2. docs/UNIT_MOVEMENT_RESEARCH.md sec 4c - the scale-run record incl. findings
     F1 (runaway under fan-out), F2/F2b (vacuous-completion classes), F3 (the
     600==600 timeout race + undersized straggler window), F4 (unexplained mover)
  3. docs/OPUS_EXECUTION_PLAN.md sec 0.3 (hard rules) + Appendix A (live-run
     preflight) + Appendix B (appNo ledger - NEXT FREE: 3363)
  4. docs/RUNBOOK.md secs 0/3/4/7/8 before ANY live run
Then `git log --oneline -3` + `git status -sb` in the port AND the fork.

ENVIRONMENT NOTES (2026-07-13 end-state): 2 Solution-A-race leftover objects
remain on the CWIX-2024 federation (the 3363 sweep was permission-denied) - run
the standard pre-run ResetVrf dry-run + sweep FIRST (fresh appNos from 3363).
License expires 2026-09-15. vrfGui was hung-but-backend-healthy all day.

CANDIDATE NEXT TASKS (pick one, or ask the user):
  (a) F3 tuning re-run: the straggler timeout sized to route length (and/or the
      Step-2.3 idle-timeout refinement) and set BELOW the predecessor timeout,
      to see the synthesis lever actually unblock successors. Config-only live
      run on the COA-STP1 order.
  (b) F2b/F1 probe matrix: per-route 1-member probes at the Mojave region
      (which routes vacuous-complete, which overshoot; vrfSim.log oracle) -
      required before trusting member completions there.
  (c) Semantic mapping continuation (PORT.md sec 10 / SEMANTIC_MAPPING.md):
      Units 2/4/5 BEHAVIOR verification via synthetic distinct-target orders
      (COA-STP1 self-targets everything; the coa-gpt memo asked for fixed data).
  (d) Housekeeping: C++ repo private remote (USER decision); delete retained
      C++ originals (migration step 1); SDK nuget decoupling; a live TASKCMPLT
      sample with bundling ON (piggyback on any run - the one P4b gap).

Non-negotiables (unchanged): never force-kill a joined federate (StopIface
clean stop); never push init to a running app; do not restart the c2sim-server
container habitually (loopback test FIRST: raw TCP to 127.0.0.1:61613 <1 s);
fresh appNo per join, recorded in Appendix B; RTI 4.6.1 + Machine-scope license
+ cwd bin64 + --contentRoot for live; movement claims REQUIRE WatchVrf
displacement (completions LIE - R11/F2/F2b); keep START_HERE/PORT/
UNIT_MOVEMENT_RESEARCH/RESUME_PROMPT current AS work lands; after any context
compaction re-read the docs before deciding anything.

START by reporting: git state of port + fork, the eight-selftest baseline
(translator 18, parse-init 80/49/4, parse-order, report 16, sequencer 12,
verb 35, destack 20, fanout 36 - exe path has the win-x64 RID subfolder), and
which task you propose - then STOP for the supervisor's go-ahead.
```

---

Notes for the human pasting this:
- The prompt points at the docs rather than restating them; keep the docs
  current and the prompt stays valid. If the prompt and a doc disagree, the
  doc wins.
- All work is pushed (2026-07-13 end of execution session): PORT main,
  FORK dev/sdk-fixes (submodule pointer current).
- The C++ repo STILL has no remote - the only golden-trace rig exists on one
  disk. Decide on a private remote.
- Subagent operational lesson (2026-07-13): executor timed waits missed their
  wake-ups three times; the supervisor must schedule its own ground-truth
  sweeps of evidence files during live windows.
- If much time has passed: expect drift (license 2026-09-15, VR-Forces /
  container up-ness, the tileserver on 8080). Appendix A preflight checks
  these - do not assume.
