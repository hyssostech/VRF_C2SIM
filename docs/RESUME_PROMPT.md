# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-14 (after the nav-data falsification + docs cleanup).

READ THIS FIRST (2026-07-14 - corrections + a REALITY CHECK; do not chase dead threads and do not
oversell green results):

REALITY CHECK (what works vs not - read before believing any "SUCCESS/COMPLETE" in the docs):
- The END-TO-END PRODUCT GOAL - run a coa-gpt scenario at Mojave with correct aggregate movement -
  does NOT work. Mojave aggregates FREEZE. That is the central unsolved problem.
- What IS verified is narrower: the plumbing (join/create/dispatch) works; at the GOLDEN SWEDEN
  region aggregates produce telemetry-backed movement; the semantic verb->vrftask map dispatches.
  Those are component/control tests, NOT product success at the target region.
- The green paths still carry unresolved warts: aggregate move-completion events are UNRELIABLE
  (moveAlong's never fires; MoveIntoFormation's fires ~40s early - movement real, TASKCMPLT timing
  wrong); the full COA-STP1 scale run at Mojave shows F1 member RUNAWAYS (180+ km) + F2b VACUOUS
  completions; the lean-creation filter REGRESSED live tasking (stashed). Do NOT read "task (c)
  complete" or "backlog complete" as "the product works".

- **NAV DATA is FALSIFIED as the Mojave cause.** Do NOT generate/load a nav mesh to unfreeze Mojave
  aggregates. Bogaland2 (Sweden, moves) and TropicTortoise (Mojave, freezes) are the IDENTICAL
  streaming terrain + model set + page-in area, and NEITHER region has nav data; Sweden
  leader-path-plans with no NavMesh. Real cause UNRESOLVED + region-specific (empty member offset
  routes at Mojave). Full record: docs/experiments/navdata_FALSIFIED_bogaland_vs_tt_2026-07-14.txt.
- Task (c) semantic mapping: Units 2, 4, 5 all dispatched + moved in their CONTROLLED SWEDEN tests
  (2026-07-14); recorded "complete" in that narrow component sense ONLY (see the completion-event
  wart above). It is NOT evidence the product runs at Mojave.
- appNo NEXT FREE is **3386** (not 3368). No appNos were consumed by the 2026-07-14 doc work.

STATUS: the OPUS_EXECUTION_PLAN step backlog + the F3 probe are done as ENGINEERING milestones - this
is NOT the same as the product working end-to-end (see the reality check). A fresh session PICKS THE
NEXT work item; it does not re-execute finished work. Everything is committed + pushed (port main +
fork dev/sdk-fixes).

---

```
Resume the C2SIM VR-Forces -> .NET port. The OPUS_EXECUTION_PLAN step backlog + the F3 probe are
done as ENGINEERING milestones - this is NOT the product working. REALITY CHECK: the end-to-end goal
(a coa-gpt scenario at MOJAVE with correct aggregate movement) does NOT work - Mojave aggregates
FREEZE (unsolved; nav data FALSIFIED as the cause). What is verified is narrower: plumbing +
telemetry-backed movement at the GOLDEN SWEDEN region + semantic verb->vrftask dispatch (component/
control tests). The "SUCCESS/COMPLETE" notes in the docs are component-scoped; do not read them as
"it works". This session PICKS THE NEXT work item - do not re-execute finished work, and do not
oversell.

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
  1. docs/START_HERE.md - current status (top = the 2026-07-14 reality-check banner + NAV-DATA
     FALSIFIED + task (c) component-complete; then Steps 1-6 + P4b + F3) + build/run + env knobs
  2. docs/UNIT_MOVEMENT_RESEARCH.md sec 4c - the scale-run record (F1 runaway, F2/F2b
     vacuous classes, F3 timeout race) AND the F3 PROBE result (straggler 900 <
     predecessor 1200 unblocks successors; predecessor-timeout skips 15 -> 2)
  3. docs/OPUS_EXECUTION_PLAN.md sec 0.3 (hard rules) + Appendix A (live-run preflight)
     + Appendix B (appNo ledger - NEXT FREE: 3386)
  4. docs/RUNBOOK.md secs 0/3/4/7/8 before ANY live run
Then `git log --oneline -3` + `git status -sb` in the port AND the fork.

ENVIRONMENT NOTES (2026-07-14): vrf was restarted fresh; the C2SIM server was left RUNNING with an
R9 Mojave init loaded but NO app ever joined (no stale federate from this session). Run the standard
pre-run ResetVrf dry-run + sweep FIRST anyway (fresh appNos from 3386). License expires 2026-09-15.
vrfGui hung-but-backend-healthy (do NOT kill it); vrfSimHLA1516e + rtiexec healthy; container up. If
much time has passed, expect drift - check, do not assume.

CANDIDATE NEXT TASKS (pick one, or ask the user):
  (a) MOJAVE AGGREGATE MOVEMENT - THE central unsolved blocker (the product does not work here).
      Root cause is region-specific: member OFFSET-ROUTE generation returns EMPTY at Mojave (0
      routes vs Sweden 45; vrfSim.log `moveAlong() - empty route`); the full COA-STP1 scale run
      also shows F1 member RUNAWAYS (180+ km) + F2b VACUOUS completions. NAV DATA is FALSIFIED as
      the cause - do NOT chase it (docs/experiments/navdata_FALSIFIED_bogaland_vs_tt_2026-07-14.txt).
      Two paths: (i) run coa-gpt at Mojave via SubordinateFanOut=true (R10 moves member entities in
      CONTROLLED probes - but the scale run's F1/F2b warts mean this is NOT yet a clean end-to-end
      success; verify with telemetry, do not assume); or (ii) a fresh root-cause dig into why the
      offset routes are empty (candidate directions - BE terrain-paging depth / VR-TheWorld coverage
      at the AO / an offset-route setting / route geometry - are UNVERIFIED; do NOT record any as
      the fix). USER decision.
  (b) COMPLETION-EVENT RELIABILITY (cross-region wart surfaced by task (c)): aggregate move-
      completion events are unreliable - moveAlong's never fires; MoveIntoFormation's fires ~40s
      early. Movement is correct; only the C2SIM TASKCMPLT arrival-timing is wrong. Harden if
      downstream arrival-accuracy matters. (Task (c) itself - Units 2/4/5 - is component-verified
      at Sweden; NOT a Mojave/product result. Do not re-run it as "unfinished".)
  (c) COA-GPT SCENARIO IMPROVEMENTS (docs/COA_GPT_FEEDBACK.md): bloat/lean-creation (BUILT
      but STASHED - regressed live tasking; needs the deferred-creation lifecycle debugged +
      a live smoke before it lands; do NOT run experiments on that build); echelon-dedup for
      co-located piles (GATED - first verify coa-gpt emits redundant command trees, else it
      loses force/threat); OPFOR right-sizing / do-not-thin-the-threat (Item 5).
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
- All work is pushed (2026-07-14): PORT `VRF_C2SIM.git` main, FORK
  `OpenC2SIM.github.io.git` dev/sdk-fixes (submodule tracks port main).
- The C++ repo (c2simVRFinterfacev2.36) STILL has no remote - the only golden-trace
  rig exists on one disk. Decide on a private remote.
- If much time has passed: expect drift (license expiry 2026-09-15, VR-Forces /
  container up-ness, the hung vrfGui, the tileserver on 8080). Check, do not assume.
