# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-15 (after the crash root-cause + inconclusive
altitude probe + DIS-type falsification).

READ THIS FIRST (2026-07-15 - do not chase already-falsified threads and do not
oversell inconclusive results):

REALITY CHECK (what works vs not - read before believing any "SUCCESS/COMPLETE" in the docs):
- The END-TO-END PRODUCT GOAL - run a coa-gpt scenario at Mojave with correct aggregate movement -
  does NOT work. Mojave aggregates FREEZE. That is still the central unsolved problem.
- What IS verified is narrower: the plumbing (join/create/dispatch) works; at the GOLDEN SWEDEN
  region GOLDEN units produce telemetry-backed movement; the semantic verb->vrftask map dispatches.
  Those are component/control tests, NOT product success at the target region.
- 2026-07-15 finding: the freeze is NOT purely a region effect. COA-STP1's OWN unit data
  (coordinate-transplanted onto the golden Sweden region, genuine leader-path, no fan-out) ALSO
  froze there - twice (unspecified DIS type, then a real DIS type borrowed from a golden unit -
  DIS-TYPE HYPOTHESIS FALSIFIED, identical failure both times). This may mean there are TWO
  separate blockers (a Mojave-region-specific one for golden units, a COA-STP1-data-specific one
  independent of region) rather than one.
- 2026-07-15 also found + root-caused a real infrastructure crash (NOT a scenario-content
  problem - a launch-method gap; see below) that cost most of a session, and then ran the
  altitude-mode probe (`GroundWaypointAltitudeMode=Live`) live for the first time - but the
  result is CONFOUNDED and INCONCLUSIVE (see "top priority" below). Do NOT read either
  clearance value (0 or 50) as validated or invalidated.
- **NAV DATA is FALSIFIED as the Mojave cause** (2026-07-14, unchanged). Do NOT generate/load a
  nav mesh to unfreeze Mojave aggregates.
- appNo NEXT FREE is **3421**. VR-Forces must be launched by a human via the GUI/`vrfLauncher.exe`
  - a headless CLI self-launch (`vrfSimHLA1516e.exe` directly) is CONFIRMED UNSAFE (crashes
  remote-controller clients on tick) - see RUNBOOK.md sec 0.5.

STATUS: the OPUS_EXECUTION_PLAN step backlog + the F3 probe are done as ENGINEERING milestones -
this is NOT the same as the product working end-to-end. A fresh session PICKS THE NEXT work item;
it does not re-execute finished or falsified work. Everything through this session is committed
on the port (branch main); NOT YET PUSHED to origin as of this refresh - check `git status -sb`
and ask the user before pushing if it still shows ahead.

---

```
Resume the C2SIM VR-Forces -> .NET port. REALITY CHECK: the end-to-end goal (a coa-gpt scenario at
MOJAVE with correct aggregate movement) does NOT work - Mojave aggregates FREEZE, cause UNSOLVED
(nav data and DIS-entity-type are both FALSIFIED; the freeze is NOT purely a region effect either -
COA-STP1's own units freeze even at the golden Sweden region). A live altitude-mode probe
(GroundWaypointAltitudeMode=Live) was finally run 2026-07-15 but came back CONFOUNDED/INCONCLUSIVE -
do not treat it as a settled result either way. This session PICKS THE NEXT work item - do not
re-execute finished or already-falsified work, and do not oversell.

TOP PRIORITY, USER DIRECTIVE (2026-07-15): before ANY further live experimentation, read VR-Forces'
own basic scenario-creation documentation -
C:\MAK\vrforces5.0.2\doc\help\Content\Scenarios\CreateRun\vrf_createScenario.htm is the entry point
found last session (covers the New Scenario wizard: terrain/SMS selection, "Create Global Dynamic
Terrain Processor", etc.). Three days were spent chasing deep hypotheses (DIS type, force side, FOM
modules, a native crash); the user's explicit, standing concern is that it is unlikely there is no
public documentation for beginners describing what a scenario needs to actually work, and this
investigation has not yet checked that. This reading pass may make some of the remaining
investigation unnecessary - do it first.

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
  1. docs/START_HERE.md - current status (top banner has the 2026-07-15 reality check); then
     "The immediate next task" sec 1 for the Mojave item specifically.
  2. docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md, section "2026-07-15 SESSION
     SYNTHESIS" - the full load-bearing account of yesterday's crash saga, its root cause, the
     inconclusive altitude probe, and the prioritized next steps. Read this in full before
     deciding anything - do not rely on summaries of it.
  3. docs/RUNBOOK.md sec 0 (force-kill rule) + sec 0.5 (VR-Forces launch - CONFIRMED UNSAFE
     headless recipe, corrected root cause) + sec 0.6 (two XML-authoring gotchas that silently
     break server pushes) + secs 3/4/7/8 before ANY live run.
  4. docs/OPUS_EXECUTION_PLAN.md Appendix A (live-run preflight) + Appendix B (appNo ledger -
     NEXT FREE: 3421) - the ledger entries from 2026-07-15 (apps 3386 onward) narrate the whole
     crash-and-recovery story blow-by-blow if you need more detail than the synthesis section.
Then `git log --oneline -3` + `git status -sb` in the port AND the fork.

ENVIRONMENT NOTES (2026-07-15): VR-Forces must be launched by the USER via the GUI/
`vrfLauncher.exe` - do NOT attempt a headless `vrfSimHLA1516e.exe` CLI launch, it is confirmed to
produce a backend that crashes remote-controller clients (ResetVrf, the app) on tick. Ask the user
to launch it and tell you which scenario (Bogaland2 for Sweden, TropicTortoise for Mojave) before
any live work. Run the standard pre-run ResetVrf dry-run + sweep once it is up (fresh appNos from
3421). License expires 2026-09-15. If much time has passed, expect drift - check, do not assume
anything about the container or process state.

CANDIDATE NEXT TASKS (the first one is the user's explicit priority; pick among the rest, or ask):
  (a) SCENARIO-CREATION 101 RESEARCH (top priority, see above) - read VR-Forces' own basic
      scenario-setup docs before anything else. May reframe or resolve open items below.
  (b) THE MISSING CONTROL - re-run the ORIGINAL Fixed100 (parity-default) config against a
      GUI/vrfLauncher-launched Mojave backend (data/R9_Mojave_Lean_Initialization.xml +
      data/R9_Mojave_UnitMove_Order.xml, no GroundWaypointAltitudeMode override). This was never
      done this session and is needed before the Live-altitude-mode A/B (clearance 0 vs 50, both
      already run but confounded - see the synthesis doc) can mean anything: if 1.BdeHQ (the
      entity that has always moved before) STILL fails to move under Fixed100 in this same
      environment, the entity-freeze is environment-specific, not altitude-code-specific, and
      needs its own root-cause dig first.
  (c) MOJAVE AGGREGATE MOVEMENT root cause, if (a)/(b) do not resolve it - member OFFSET-ROUTE
      generation returns EMPTY at Mojave for golden units (0 routes vs Sweden 45; vrfSim.log
      `moveAlong() - empty route`). INTERIM PROVEN MOVER: `Vrf:SubordinateFanOut` (R10) marches
      member entities at Mojave, bypassing the empty-offset-route path, with known F1/F2b warts
      under load (UNIT_MOVEMENT_RESEARCH.md sec 4c).
  (d) COA-STP1-DATA-SPECIFIC blocker (separate from (c) per the 2026-07-15 finding) - force-side/
      hostility was proposed as the next hypothesis after DIS-type was falsified but was
      DEPRIORITIZED, not tested, per user judgment ("units of any type should be taskable" - an
      untested skepticism, not a falsification). Revisit only if (a)/(b)/(c) do not explain it.
  (e) A `vrfLauncher.exe`-based headless self-launch recipe - untested candidate:
      `vrfLauncher.exe -B --usePredefinedConnection "HLA 1516 Evolved RPR 2.0 with MAK
      extensions" --simArgs <args>` (that exact profile name is confirmed on-screen from the
      user's connection config; the vrfLauncher invocation syntax itself is NOT yet tried live -
      `vrfLauncher.exe --help` output is captured in RUNBOOK.md sec 0.5's history). Lower
      priority than (a)-(d); "a human launches it" works fine for now.
  (f) COMPLETION-EVENT RELIABILITY / COA-GPT SCENARIO IMPROVEMENTS / housekeeping - see
      docs/START_HERE.md "immediate next task" secs 2-4 for the pre-existing backlog, unchanged
      by this session's findings.

Non-negotiables (unchanged, reinforced this session): never force-kill a joined federate
(StopIface clean stop). A process that never completed a real join (e.g. stuck behind a startup
error dialog) is IN PRINCIPLE fine to close directly, but the harness's own safety classifier
does not reliably honor this distinction in practice (blocked closing attempts twice this
session even against processes judged safe, once requiring the user's direct real-time
confirmation before it allowed the same action) - expect to need the user's live go-ahead for
any process close, do not assume standing authorization carries across a session boundary or
even within one. Never push init to a running app; do not restart
the c2sim-server container habitually; fresh appNo per join, recorded in Appendix B (NEXT FREE
3421); RTI 4.6.1 + Machine-scope license + cwd bin64 + --contentRoot for live; movement claims
REQUIRE WatchVrf displacement (completions LIE - R11/F2/F2b, and now also seen with the Live
altitude mode - a real TASKCMPLT with a real resolved position but zero net displacement);
VR-Forces launch REQUIRES a human via the GUI/vrfLauncher (see above); a comment in an XML
prolog breaks init pushes, ANY block comment breaks order STOMP delivery (RUNBOOK sec 0.6); keep
START_HERE/the investigation doc/RESUME_PROMPT current AS work lands; after any context
compaction re-read the docs before deciding anything.

START by reporting: git state of port + fork, the eight-selftest baseline
(translator 18, parse-init 80/49/4, parse-order, report 16, sequencer 12, verb 35,
destack 20, fanout 36 - exe path has the win-x64 RID subfolder), current environment state
(ask the user what VR-Forces scenario, if any, is currently loaded - do not assume), and which
task you propose - then STOP for the supervisor's go-ahead.
```

---

Notes for the human pasting this:
- The prompt points at the docs rather than restating them, so it does not go stale.
  docs/START_HERE.md + the investigation doc's "2026-07-15 SESSION SYNTHESIS" carry the current
  state in full.
- Push status as of this refresh: the port (branch main) has 2026-07-15 commits NOT YET pushed to
  origin (check `git status -sb` - it will show "ahead N"). The fork's submodule pointer has not
  been bumped to match. Ask the user before pushing.
- The C++ repo (c2simVRFinterfacev2.36) STILL has no remote - the only golden-trace
  rig exists on one disk. Decide on a private remote.
- If much time has passed: expect drift (license expiry 2026-09-15, VR-Forces /
  container up-ness, whether the user's vrfLauncher-launched instance from 2026-07-15 is still
  up). Check, do not assume - and remember VR-Forces needs a human to launch it now (sec 0.5).
