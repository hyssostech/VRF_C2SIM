# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-13 afternoon.

STATUS: the planning deliverable is DONE - docs/OPUS_EXECUTION_PLAN.md is written
(the supervised, step-by-step plan for the six ready backlog items, built on
docs/PLAN_DERISK_NOTES.md). It is PENDING the user's review. Once approved, the next
session EXECUTES it under supervision, step by step, honoring every gate in its
SUPERVISION PROTOCOL section (offline selftests, GATE-ENV before live, GATE-VERDICT
after, telemetry-only movement claims). Until then, the block below (a planning prompt)
still applies for re-reading state; after approval, swap the deliverable line to
"execute docs/OPUS_EXECUTION_PLAN.md under supervision; do not skip its gates".
Prior context: R10 fan-out live-verified, COA-STP1 unblocked 5/7 vs R5c 0/6; R11
negative/trap; P4 root cause pinned to file:line.

---

```
Resume the C2SIM VR-Forces -> .NET port. THIS SESSION IS A PLANNING SESSION: its
deliverable is docs/OPUS_EXECUTION_PLAN.md - an execution plan detailed enough for an
Opus-class model to carry out under a supervisor's review. Do NOT execute the backlog
items themselves this session (beyond what plan-writing strictly requires).

WHERE THE WORK LIVES (all pushed to GitHub 2026-07-13; run `git log --oneline -1` for
the exact tips, do not trust pinned hashes in prose):
- PORT (the .NET port, SOURCE OF TRUTH + where you work): github.com/hyssostech/VRF_C2SIM.git,
  branch main. Local: C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\
  Software\Interfaces\VRF_C2SIM (nested submodule of the fork).
- FORK + SDK (the C2SIM half rides on the SDK, which lives INSIDE the fork):
  github.com/hyssostech/OpenC2SIM.github.io.git, branch dev/sdk-fixes (submodule -> port
  main). SDK at Software/Library/CS/C2SIMSDK. NOTE: plan item P4a EDITS THE SDK
  (C2SIMClientLib) - SDK changes commit to the FORK repo on dev/sdk-fixes.
- DEPRECATED C++ interface (FROZEN parity oracle only, do NOT develop there):
  C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36 - still has NO git
  remote (single-disk risk; private-remote decision still pending with the user).

CLOUD vs LOCAL: plan WRITING can happen anywhere; the plan's execution targets the
LOCAL machine (MAK + VS18 + VR-Forces + the live c2sim-server). Builds, the eight
offline selftests, and LIVE runs need LOCAL.

Before writing ANYTHING, read these in the PORT repo, in order, and treat them as
source of truth over ANY summary or recollection:
  1. docs/START_HERE.md      - current status + repo state + build/run + tools inventory
  2. docs/PORT.md            - settled decisions WITH evidence; ESP. sec 8 + sec 10
  3. docs/UNIT_MOVEMENT_RESEARCH.md - the aggregate-movement arc R1-R11 (sec 4/4b/4c:
     R8 de-stack, R9 geography+mechanism, R10 fan-out verified, R11 vacuous-completion
     TRAP, COA-STP1 unblock run 5/7, the fan-out robustness follow-up)
  4. docs/PLAN_DERISK_NOTES.md - THE PLANNING INPUTS (2026-07-13, all verified):
     P4a port-exhaustion root cause (HttpClient-per-call, C2SIMClientRestLib.cs:125/:369,
     fix spec + the shared-client header-race subtlety), P4b bundling shape (C++
     textIf.cxx:435-530: position-only, N ReportContent per envelope, count-10/size-10KB/
     2s-timer flushes; .NET schema already array-ready), fan-out quorum design (incl. the
     late-straggler-swallow subtlety), scale-run criteria, the 6 csproj paths, and the
     supervision-protocol facts. On plan mechanics THIS DOC WINS.
  5. docs/RUNBOOK.md         - runtime procedure the plan must embed (sec 0/3/4/7/8)
  6. docs/SEMANTIC_MAPPING.md - verb map status (context for the memo + scale run)
  7. docs/NEXT_SESSION_GUIDANCE.md - the older deep-review deliverable (historical)
Then run `git log --oneline -5` in the port repo and the fork.

STATE (2026-07-13 midday; everything COMMITTED + PUSHED, eight selftests green):
- Phases 1-5 + P0 fixes: DONE long since. R8 de-stack: works, stack hypothesis
  falsified. R9: GEOGRAPHY confirmed as the aggregate blocker (unit leader-path plans
  come back EMPTY at the Mojave region; `moveAlong() - empty route` is the backend
  oracle; Sweden control 3/3 same code).
- R10 SUBORDINATE FAN-OUT: live-verified. Vrf:SubordinateFanOut tasks member entities
  directly (GetAggregateMembers: published entities + RECURSIVE subAggregates rosters);
  FanOutTracker synthesizes the unit TASKCMPLT when ALL members complete. Mojave probe
  3/3 (telemetry-verified marches); COA-STP1's own units at its own location: 5/7 unit
  completions (both platoons, BOTH companies incl. mega-pile-center B/40, control) vs
  R5c 0/6; the 2 CoHQs ended 3/4 members (one stuck GndV each holds the unit task open
  - THE robustness gap the plan fixes first).
- R11 NEGATIVE + TRAP: DtPlanAndMoveToTask completes VACUOUSLY at path-dead regions
  (TASKCMPLT fired, units verified still at spawn). Completions can LIE - WatchVrf
  telemetry is the only movement oracle. The plan must encode this rule.
- P4 port exhaustion: fired in EVERY live run; root cause + fix spec are in
  PLAN_DERISK_NOTES sec 1 (SDK HttpClient-per-call).
- Evidence archives: docs/experiments/*.txt (R8/R9/R10-R11 raw extracts).

THE TASK - write docs/OPUS_EXECUTION_PLAN.md covering, in this order:
  1. P4a SDK shared-HttpClient fix (smallest step, biggest operational win)
  2. Fan-out robustness: completion quorum + straggler timeout (+ extend
     --fanout-selftest; optional MoveToLocation fan-out)
  3. P4b position-report bundling (C++-parity shape, opt-in)
  4. coa-gpt data memo (4 evidence-backed items)
  5. COA-STP1 FULL 42-task order scale run (live)
  6. Housekeeping: 6 tools csproj relative paths (+ anything the user flags)
For EVERY step the plan must give: exact files + code-level change spec (from the
de-risk notes), exact build/test commands, acceptance criteria + verification gate
(offline selftests; live decision rules with TELEMETRY oracles where movement is
claimed), rollback note, and STOP-AND-ESCALATE conditions. Open with a SUPERVISION
PROTOCOL section: the executor checks in at every gate (diff review before commit,
env checklist before every live run, verdict review after); the non-negotiables below
are copied into the plan verbatim as hard rules; docs (START_HERE/PORT/
UNIT_MOVEMENT_RESEARCH/RESUME_PROMPT) are updated AS work lands, commits pushed to
port main + fork dev/sdk-fixes (submodule bump; SDK edits commit in the fork).
Get the user's review of the plan BEFORE any execution starts.

Non-negotiables (full text RUNBOOK + PLAN_DERISK_NOTES sec 6 - copy into the plan):
- NEVER force-kill a joined federate; clean-stop via tools/StopIface. FRESH
  Vrf__ApplicationNumber every RTI join (ResetVrf/WatchVrf included; 3200-3350 are
  used, START AT 3355). NEVER push an init to a running interface.
- Do NOT restart the c2sim-server container habitually. Loopback test first: TCP
  connect to 127.0.0.1:61613 must be near-instant.
- LIVE runs: RTI 4.6.1 on PATH (4.6b = build/offline only), MAKLMGRD_LICENSE_FILE from
  Machine scope, cwd=C:\MAK\vrforces5.0.2\bin64 + --contentRoot=<exe dir>. PushInit
  FIRST, then start the app (it late-joins). ResetVrf between heavy runs.
- vrfGui is HUNG (multi-day) - the sim BACKEND is healthy; do NOT kill vrfSimHLA1516e.
  WatchVrf is the visual channel. Backend also unresponsive -> STOP, coordinate with
  the user.
- Movement claims REQUIRE telemetry (WatchVrf displacement), never completion events
  alone (the R11 trap).
- Bridge builds: VS18 MSBuild via PowerShell (git-bash mangles /p:). App builds:
  DOTNET_CLI_USE_MSBUILD_SERVER=false dotnet build ... --disable-build-servers.
- Offline selftests before/after ANY change (MAK bin dirs on PATH, 4.6b fine):
  --translator-selftest 18/18, --parse-init 80/49/4, --parse-order, --report-selftest
  9/9, --sequencer-selftest 12, --verb-selftest 28/28, --destack-selftest 20,
  --fanout-selftest 16 (grows with the quorum work).
- Keep docs/START_HERE.md, PORT.md, UNIT_MOVEMENT_RESEARCH.md, RESUME_PROMPT.md
  current AS you work; after any context compaction re-read them before deciding.

Start by reading the docs above, then report: git state of port + fork (incl. anything
unpushed) and your outline for OPUS_EXECUTION_PLAN.md. Write the plan, commit + push
it (port main + fork submodule bump), and present it for the user's review. Do not
begin executing the plan.
```

---

Notes for the human pasting this:
- The prompt points at the docs rather than restating them, so it does not go stale.
  docs/PLAN_DERISK_NOTES.md carries the verified mechanics every plan step needs.
- All work is pushed (2026-07-13): PORT `VRF_C2SIM.git` main, FORK
  `OpenC2SIM.github.io.git` dev/sdk-fixes (submodule tracks port main).
- The C++ repo (c2simVRFinterfacev2.36) STILL has no remote - the only golden-trace
  rig exists on one disk. Decide on a private remote.
- If much time has passed: expect drift (license expiry 15-sep-2026, VR-Forces /
  container up-ness, the hung vrfGui, the tileserver on 8080). Check, do not assume.
