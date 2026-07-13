# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-13 late morning (R10 SUBORDINATE FAN-OUT
LIVE-VERIFIED - the working mitigation for path-plan-dead regions; COA-STP1
UNBLOCKED at its own location: 5/7 unit completions incl. both companies where
R5c scored 0/6. R11 planAndMoveTo NEGATIVE + TRAP: vacuous completions, no
movement. Next: fan-out straggler quorum/timeout, P4 bundling, coa-gpt memo).

---

```
Resume the C2SIM VR-Forces -> .NET port.

WHERE THE WORK LIVES (all pushed to GitHub 2026-07-12; run `git log --oneline -1` for
the exact tips, do not trust pinned hashes in prose):
- PORT (the .NET port, SOURCE OF TRUTH + where you work): github.com/hyssostech/VRF_C2SIM.git,
  branch main. Local: C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\
  Software\Interfaces\VRF_C2SIM (nested submodule of the fork).
- FORK + SDK (the C2SIM half rides on the SDK, which lives INSIDE the fork):
  github.com/hyssostech/OpenC2SIM.github.io.git, branch dev/sdk-fixes (submodule -> port main).
  SDK at Software/Library/CS/C2SIMSDK. Complete tree: clone the FORK, checkout dev/sdk-fixes,
  `git submodule update --init --recursive`.
- DEPRECATED C++ interface (FROZEN parity oracle only, do NOT develop there):
  C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36 - still has NO git
  remote (single-disk risk; private-remote decision still pending with the user).

CLOUD vs LOCAL (READ FIRST): a cloud checkout can do docs/design/source-editing ONLY.
Builds, offline selftests, and LIVE runs need the LOCAL machine (MAK + VS18 + VR-Forces).

Before doing or deciding ANYTHING, read these in the PORT repo, in order, and treat them
as source of truth over ANY summary or recollection:
  1. docs/START_HERE.md    - current status + repo state + build/run + tools inventory
  2. docs/PORT.md          - settled decisions WITH evidence; ESP. sec 8 + sec 10
  3. docs/SEMANTIC_MAPPING.md - the two-layer verb map + per-unit live status
  4. docs/RUNBOOK.md       - runtime procedure; sec 0/3/4/7/8 BEFORE any live run
  5. docs/UNIT_MOVEMENT_RESEARCH.md - THE aggregate-movement reference (vendor-doc
     research + R1-R5c results + the approved next step). On aggregate topics IT WINS.
  6. docs/NEXT_SESSION_GUIDANCE.md - the 2026-07-12 deep-review deliverable (P0 fixes -
     LANDED + LIVE-VERIFIED; its sec 4 ladder is superseded by #5's plan).
Then run `git log --oneline -5` in the port repo and the fork.

STATE (2026-07-13; everything below is COMMITTED + PUSHED, selftests green):
- Phases 1-5 DONE long since (full C2SIM<->VR-Forces loop live). P0 orchestration fixes
  (completion attribution via InFlightTracker, dispatch-relative predecessor timeouts w/
  Vrf:PredecessorTimeoutPolicy=skip default, completion-gated ATTACK/BREACH engage w/
  Vrf:EngageFallbackSeconds) are IMPLEMENTED and LIVE-VERIFIED (golden re-run: correct
  TASKCMPLT attribution with multiple tasks in flight; clean stop; Solution A cleanup).
- AGGREGATE MOVEMENT SOLVED FOR DISPERSED SCENARIOS (the R5 breakthrough): with
  Vrf:AggregateFormation=auto, every created aggregate is QUERIED for its own formation
  list (new facade/bridge RequestAvailableFormations -> AvailableFormations event); the
  reply sets a valid lowercase name from that list (snap) + ReorganizeAggregate (new
  call; establishes the lead subordinate) BEFORE any tasking. R5 on the golden STP init:
  3/3 TASKCMPLT incl. an ArmorPlatoon-type unit that had NEVER moved; WatchVrf telemetry
  showed clean on-axis marches. GROUND RULES learned: never trust static formation names
  (.entity files mislead; live lists are all lowercase - ALWAYS query); the
  currentFormation read-back returns '' even when the set took (trust the LIST).
- R5c on COA-STP1 (same code, same probe order): repair applied 113/113, 0/6 aggregates
  marched (entity control 1/7, ~13 min to escape the spawn pile); its "stacked
  coordinates are the blocking pathology" verdict and its "runaway eliminated" reading
  are BOTH superseded by the R8 live A/B (next bullet): the runaway was gridlock-
  suppressed and the stack was not the blocker.
- R8 CREATE-TIME DE-STACKING: IMPLEMENTED + OFFLINE-VERIFIED + LIVE-VERIFIED
  (2026-07-12/13). Vrf:DeStackCreates (default off) + Vrf:DeStackSpacingMeters
  (default 50) spread identically-positioned units onto deterministic hex rings before
  creation (pure DeStacker.cs; --destack-selftest 20 checks; --parse-init prints
  stacked groups). OFFLINE: the GOLDEN init is ALSO stacked (10 groups, 48/49
  creatable, max pile 13 - superior-coordinate inheritance, C++ parity) and R5 marched
  3/3 out of those piles; COA-STP1's distinguishing feature is a 54-unit MEGA-pile.
  LIVE A/B (exact R5c probe, only de-stack toggled; UNIT_MOVEMENT_RESEARCH.md sec 4b):
  R8 WORKS (mega-pile spread; control completed 4x faster ~3.5 vs ~13 min; CoHQ
  creation now CLEAN) - recommend ON for stacked scenarios - but STILL 0/6 aggregates
  marched: companies DRIVE 31-124 km past their 1.1 km routes (E1 runaway re-expressed
  - R5c's "runaway eliminated" was GRIDLOCK suppression, not a fix), CoHQs scatter
  76-93 km ON TASKING (member warp, not driving), platoons shuffle ~60 m. STACKED
  COORDINATES ARE FALSIFIED as the sufficient blocker.
- R9 REGION SWAP DONE (2026-07-13 morning; UNIT_MOVEMENT_RESEARCH.md sec 4c;
  evidence docs/experiments/R9_region_swap_2026-07-13.txt): GEOGRAPHY CONFIRMED.
  The golden unit set transplanted to Mojave (data/R9_Mojave_*.xml, geometry
  preserved) fails 1/3 (platoon frozen 8 m, company 410 m wrong-way then frozen,
  entity completes); the same-day SWEDEN CONTROL (original files, same code, 20x)
  completes 3/3 in ~4 min - code drift and multiplier EXCLUDED. MECHANISM: at
  Mojave vrfSim.log shows `moveAlong() - empty route -- not sending move along to
  subordinate` + ZERO member Offset Routes (Sweden: 45) - unit leader-path
  planning returns EMPTY at that location on the whole-earth online terrain
  ("MAK Earth Space (online).mtf"). NOT an interface defect (identical command
  stream both runs). Entity moves work at BOTH regions.
- New tools/surface since the last jump: facade/bridge ReorganizeAggregate +
  RequestAvailableFormations/AvailableFormations; tools/WatchVrf (member-level position
  telemetry, the GUI-independent movement oracle); data/E1_Formation_Order.xml +
  data/R5_UnitMove_Order.xml (de-confounded probe orders); --sequencer-selftest now 12
  checks; InitParseCheck prints planned-creations-by-type + stacked groups;
  --destack-selftest.

STATE ADDENDUM (2026-07-13 late morning - UNIT_MOVEMENT_RESEARCH.md sec 4c, evidence
docs/experiments/R10_R11_fanout_2026-07-13.txt):
- R10 DONE + LIVE-VERIFIED: Vrf:SubordinateFanOut fans aggregate moves to member
  entities (facade GetAggregateMembers: published entities + RECURSIVE subAggregates
  rosters; FanOutTracker synthesizes the unit TASKCMPLT when all members complete).
  Mojave R9 probe 3/3, member marches telemetry-verified (1.1-1.3 km cohorts).
  COA-STP1's OWN units at its OWN location (de-stack + auto + fan-out, E1 probe):
  5/7 unit completions - both platoons, BOTH companies (18/18 members each; B/40
  from the 54-pile center), control - vs R5c 0/6. The 2 CoHQs ended 3/4 members
  each (one stuck GndV per unit holds the unit task open - the known caveat).
- R11 NEGATIVE + TRAP: DtPlanAndMoveToTask at a path-dead region completes VACUOUSLY
  (TASKCMPLT fired; WatchVrf shows units still AT SPAWN). Never adopt as a fix;
  completions can LIE - telemetry is the movement oracle. Experiment-only knob
  Vrf:AggregatePlanAndMove carries the caveat.

DO FIRST (in order):
1. Fan-out ROBUSTNESS (app-only): straggler policy so one stuck member cannot hold a
   unit task open - a completion QUORUM (suggest Vrf:FanOutCompletionFraction, e.g.
   0.75 -> synthesize completion + warn) and/or a per-member timeout; consider also
   fanning the single-point MoveToLocation path. Extend --fanout-selftest, then a
   quick live re-probe of the two CoHQs.
2. P4 REPORT BUNDLING/DEDUP - now overdue: ephemeral-port exhaustion fired in EVERY
   run today ("Only one usage of each socket address" on 127.0.0.1:8080); bundle
   position reports / reuse connections in the report-push path.
3. coa-gpt data memo - FOUR evidence-backed items: distinct AffectedEntity, timing
   hygiene, DISPERSED positions (nuanced: R8 de-stack mitigates interface-side), and
   REGION VALIDATION (R9: unit maneuver capability depends on the scenario location;
   probe a region with 1 unit first; R10 fan-out is the interface-side mitigation).
4. Then: COA-STP1 FULL 42-task order at scale (de-stack + fan-out); CoHQ straggler
   forensics (member telemetry on the stuck GndVs); E2 MoveIntoFormation re-test (at
   the GOLDEN region first); housekeeping (6 tools csproj absolute SDK paths; the
   ~2500-char server-broadcast truncation - clean orders pass).

Non-negotiables (full text RUNBOOK + guidance sec 1 - violating these destroys state):
- NEVER force-kill a joined federate; clean-stop via tools/StopIface. FRESH
  Vrf__ApplicationNumber every RTI join (this includes ResetVrf/WatchVrf runs; 3200-3350
  are used, start at 3355). NEVER push an init to a running interface.
- Do NOT restart the c2sim-server container habitually. Loopback test first: TCP connect
  to 127.0.0.1:61613 must be near-instant.
- LIVE runs: RTI 4.6.1 on PATH (4.6b = build/offline only), MAKLMGRD_LICENSE_FILE from
  Machine scope, cwd=C:\MAK\vrforces5.0.2\bin64 + --contentRoot=<exe dir>. PushInit
  FIRST, then start the app (it late-joins). ResetVrf between heavy runs.
- vrfGui is HUNG (has been all day) - the sim BACKEND is healthy; do NOT kill
  vrfSimHLA1516e. WatchVrf replaces the visual channel. If a fresh session finds the
  backend also unresponsive, STOP and coordinate with the user (a VR-Forces restart
  needs the GUI/user).
- Build the bridge with VS18 MSBuild
  ("C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
  src\VrfBridge\VrfBridge.vcxproj /p:Configuration=Release /p:Platform=x64 /m); build the
  app with DOTNET_CLI_USE_MSBUILD_SERVER=false dotnet build ... --disable-build-servers.
- Offline selftests before/after ANY change (MAK bin dirs on PATH, 4.6b fine):
  --translator-selftest 18/18, --parse-init 80/49/4, --parse-order, --report-selftest
  9/9, --sequencer-selftest 12 checks, --verb-selftest 28/28, --destack-selftest 20,
  --fanout-selftest 16.
- Keep docs/START_HERE.md, PORT.md, SEMANTIC_MAPPING.md, UNIT_MOVEMENT_RESEARCH.md,
  RESUME_PROMPT.md current AS you work; after any context compaction re-read them
  before deciding anything.

Start by reading the docs above, then report: git state of port + fork (incl. anything
unpushed) and exactly what you will do first. Do not edit or run until you have done that.
```

---

Notes for the human pasting this:
- The prompt points at the docs rather than restating them, so it does not go stale as
  the work progresses. Keep the docs current; the prompt stays valid.
- All work is pushed (2026-07-12 night): PORT `VRF_C2SIM.git` main, FORK
  `OpenC2SIM.github.io.git` dev/sdk-fixes (submodule tracks port main; `git log` for
  exact tips). Cloud sessions can clone but only do docs/planning (no MAK -> no
  build/run). Real work needs this machine.
- The C++ repo (c2simVRFinterfacev2.36) STILL has no remote - the only golden-trace rig
  exists on one disk. Decide on a private remote.
- If much time has passed: expect drift in the environment (license expiry 15-sep-2026,
  VR-Forces/container up-ness, the hung vrfGui, the tileserver on 8080). The runtime
  non-negotiables say check, do not assume.
