# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-12 (deep-review corrections folded in + P0
orchestration fixes landed; NEXT_SESSION_GUIDANCE.md added to the read list).

---

```
Resume the C2SIM VR-Forces -> .NET port.

WHERE THE WORK LIVES (all pushed to GitHub 2026-07-12 - a session can clone fresh; run
`git log --oneline -1` for the exact tips, do not trust pinned hashes in prose):
- PORT (the .NET port, SOURCE OF TRUTH + where you work): github.com/hyssostech/VRF_C2SIM.git,
  branch main. Local: ...\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM (nested submodule).
- FORK + SDK (the C2SIM half rides on the SDK, which lives INSIDE the fork, NOT a separate submodule):
  github.com/hyssostech/OpenC2SIM.github.io.git, branch dev/sdk-fixes (submodule -> port main).
  SDK at Software/Library/CS/C2SIMSDK. To get a COMPLETE tree: clone the FORK, `git checkout
  dev/sdk-fixes`, `git submodule update --init --recursive`.
- DEPRECATED C++ interface (ported FROM; FROZEN parity oracle only, do NOT develop there):
  C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36

CLOUD vs LOCAL (READ THIS FIRST): the port drives VR-Forces through a NATIVE C++/CLI bridge over the
MAK libraries (PORT.md sec 1 - a pure-.NET interface is impossible). So a CLOUD checkout can do docs /
planning / source-editing ONLY - it CANNOT build or run the app (needs VrfBridge.dll from the MAK
native build) and CANNOT do any VR-Forces run. All builds, offline selftests, and LIVE runs require
the LOCAL machine (MAK + VS18 + VR-Forces). If you are in the cloud, confine yourself to docs/design.

Before doing or deciding ANYTHING, read these in the PORT repo (VRF_C2SIM), in order, and treat them
as source of truth over ANY summary or recollection (an earlier session flailed by trusting a
compaction summary instead of the docs and code):
  1. docs/START_HERE.md    - current status + repo state + build/run commands + tools list
  2. docs/PORT.md          - settled decisions WITH evidence; ESP. sec 8 (phase status) and sec 10
                             (two-layer semantic map + the aggregate root cause / experiment ladder)
  3. docs/SEMANTIC_MAPPING.md - THE CURRENT WORK: the two-layer plan + per-unit status incl. the LIVE
                             results (sec 5). Read the Unit 4 REOPENING before touching aggregates.
  4. docs/APP.md           - the .NET app, its data flow, DONE-vs-TODO
  5. docs/RUNBOOK.md       - runtime procedure for a LIVE run. Read sec 7 (the FULL .NET-port live
                             recipe), sec 4 (CLEAN STOP), sec 8 (ResetVrf self-service reset + the
                             Solution-A-misses-orphans finding) before any run.
  6. docs/NEXT_SESSION_GUIDANCE.md - the 2026-07-12 deep-review deliverable: verified corrections
                             (tagged VERIFIED/AGENT/HYPOTHESIS), the P0 fixes (landed), and the
                             aggregate experiment ladder (sec 4, E1 first). READ LAST; where it
                             conflicts with older docs IT WINS.
  7. docs/PHASE2_BRIDGE.md, docs/PHASE1_REWIRE.md, docs/TASK_EXPANSION_PLAN.md - reference only.
Then run `git log --oneline` in the PORT repo (authoritative for state) and in the fork.

STATE (2026-07-12): Phases 1-5 DONE (the port runs the full C2SIM<->VR-Forces loop live: join ->
late-join -> order over STOMP -> parse -> task -> sim -> move -> complete -> TASKCMPLT + position
reports -> clean stop, no stale federate). Two-layer semantic mapping is UNDERWAY:
- Layer-1 verb classifier DONE (`VerbMapping` + `--verb-selftest`, grounded on the real order verbs).
- Layer-2 vrftasks WIRED + built + live-dispatched (commit faa4398): Unit 3 fires (ATTACK ->
  DtFireAtTargetTask) FULL-LIVE via a synthetic order; Unit 2 Breach (DtBreachTask); Unit 4
  MoveIntoFormation (DtMoveIntoFormationTask, opt-in `Vrf:MoveIntoFormation`); Reconnoiter
  (DtPatrolRouteTask, SCREEN/SCOUT); Escort (DtFollowEntityTask, ESCRT). HoldObjective + Clear are
  documented bare-move fallbacks. Affected-entity resolution via `_unitByC2SimUuid` -> `_vrfUuidByName`.
- Solution A (delete-on-stop) DONE + LIVE (`Vrf:CleanupCreatedOnStop`, default on) - runs self-clean.
- ResetVrf HARD reset DONE + LIVE (`tools/ResetVrf`, pure VR-Forces, `--dry-run` = discover-only) -
  joins, discovers EVERY reflected object (facade BeginTrackingReflectedObjects/GetAllReflectedUuids
  via the UUID-manager change callbacks; base reflected lists have NO iterator), DeleteObject's each.
- P0 ORCHESTRATION FIXES LANDED 2026-07-12 (guidance sec 3; offline-verified, all six selftests
  green): P0.1 per-unit in-flight completion attribution (InFlightTracker - TASKCMPLT names the
  RIGHT task; superseded tasks' gates stay closed); P0.2 predecessor-timeout policy
  (`Vrf:PredecessorTimeoutPolicy` skip|force|whenIdle, default SKIP - no more retask bursts; the
  completion window now runs from predecessor DISPATCH; abandoned tasks fail successors fast);
  P0.3 completion-gated advance-then-engage (fire/breach issued when the move COMPLETES,
  `Vrf:EngageFallbackSeconds` fallback). Plus: FIFO route-name matching, duplicate-init guard,
  loud 0-units-matched-ClientId error, order-parse warnings (multi-ATR / non-STREND).
Everything is COMMITTED + PUSHED. data/ scenarios tracked. Run git log for the exact tip.

DEAD ENDS / FINDINGS (corrected 2026-07-12 per docs/NEXT_SESSION_GUIDANCE.md - read that doc's
sec 2 before trusting ANY settled negative below):
- **MoveIntoFormation negative REOPENED (was "RULED OUT" - that verdict is RETRACTED).** The
  2026-07-11 run stands (dispatched ~35 times to the 11 distinct COA-STP1 performers, 0 crash,
  0 aggregate moved) but was CONFOUNDED: MAK help says Move Into Formation IS for DISAGGREGATED
  units (FormationMoveInto.htm - the opposite of the "needs AGGREGATED sets" hypothesis); the
  targets still held the invalid "column-left" formation (never repaired, and "Wedge" was the
  wrong CASE for most types); and the pre-P0 orchestration defects (retask bursts,
  misattribution) corrupted the run. Keep default-off; re-test per guidance sec 4 E2 AFTER E1.
  (SEMANTIC_MAPPING sec 5 Unit 4.)
- **Wedge / SetAggregateFormation alone** is NECESSARY-but-NOT-SUFFICIENT: it moved only ~3/32
  COA-STP1 aggregate dispatches. ROOT CAUSE now known (guidance sec 2.1): formation names are
  per-unit-type and case-inconsistent (Ground_Aggregate catch-all = lowercase; Tank Company =
  Title-Case; Infantry/Artillery Bn = EMPTY list), and created aggregates arrive with invalid
  "column-left" (128 hits in vrfSim.log). Title-Case "Wedge" resolved only for company-matched
  types. Fix = guidance sec 4 E1 (per-matched-type names). (PORT.md sec 10.)
- **coa-gpt data blocks the engagement verbs from being exercised by COA-STP1:** ALL 42 TASKS
  self-target (AffectedEntity == PerformingEntity - not just the 19 ATTACK-family ones). So
  ATTACK/BREACH/ESCRT can NEVER dispatch their Layer-2 tasks from this order at ANY run length -
  the distinct-target guards correctly degrade them to movement. (The earlier "BREACH obstacles
  are map graphics that TryResolveVrfUuid misses" claim was WRONG: the order has zero
  MapGraphicID elements; resolution succeeds and the self-target guard rejects.) Synthetic
  orders are REQUIRED (the Unit-3 fires test already exists). Feed back to coa-gpt: emit real,
  distinct AffectedEntity values. (SEMANTIC_MAPPING sec 5 Units 2/3/5; guidance sec 2.3/5.)
- **Solution A is NOT complete cleanup** - it MISSES objects created shortly before clean-stop (race).
  A live run left 2 route/graphic orphans (a T23_AOA route) that ResetVrf then cleared. Run ResetVrf
  after any heavy/re-pushed run to guarantee a clean slate. (RUNBOOK sec 8.)
- (Older, settled in PORT.md sec 6/7) C++ debug=1 is broken; setTarget passes a C2SIM uuid to VRF
  (no-op); busy-wait thread leak; the STOMP `selector` removal was NOT a bug. Do not "fix" these.

NEXT (all LIVE-GATED - need VR-Forces + the LOCAL machine; a cloud session can only PLAN them):
1. THE AGGREGATE DEEP-DIVE - **SOLVED for dispersed scenarios (2026-07-12 R5, 3/3
   TASKCMPLT incl. the never-moved ArmorPlatoon type)**: read docs/UNIT_MOVEMENT_RESEARCH.md
   sec 4. The working sequence, now built into `Vrf:AggregateFormation=auto`: on aggregate
   creation QUERY the unit's own formation list (RequestAvailableFormations), on the reply
   SET a valid lowercase name from that list (snap) + ReorganizeAggregate (leader), all
   BEFORE tasking. Ground truth: live lists are ALL lowercase (never trust static .entity
   analysis - query); currentFormation reads '' even when set (trust the list). New tools/
   facade: ReorganizeAggregate + RequestAvailableFormations/AvailableFormations event +
   tools/WatchVrf (member telemetry). REMAINING: **R5c** - the same sequence on COA-STP1
   (stacked/identical unit coordinates) to isolate the scenario-data pathology (R6 coa-gpt
   feedback: disperse positions); then E2 re-test MoveIntoFormation with sane preconditions.
   Use ResetVrf between runs; record outcomes in PORT.md sec 10.
2. Exercise Breach/Escort/Screen behavior via SYNTHETIC orders (COA-STP1 CANNOT exercise them - all
   42 tasks self-target; SCREEN additionally gated + T24 has no Location). Before the Escort test,
   fix FollowEntity's zero offset (guidance P3.5).
3. Report parity polish (EntityHealthStatus, aggregate-component dedup, multi-content bundling - reports
   are chatty); deferred sec-6 bug fixes (distinct C2SimUuid/VrfUuid types) + OnObjectInitialization
   stub; formal golden-trace message diff.
4. Housekeeping: the tools/*.csproj (PushInit/PushOrder/StopIface/ListenReports/SdkVerify/StompProbe) use
   ABSOLUTE SDK ProjectReference paths (C:\Users\...) - make them relative for portability (the main app
   csproj is already relative). Delete the retained C++ originals; decouple the SDK to a published nuget.

Non-negotiables (where earlier sessions went wrong - do not repeat):
- READ the docs above BEFORE acting; after any compaction re-read them; trust docs + code over any
  summary. Re-read a file's CURRENT lines before editing it (docs get concurrently edited).
- Build the bridge with the VS18 (net10-capable) MSBuild, NOT VS2019 BuildTools:
  & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
    src\VrfBridge\VrfBridge.vcxproj /p:Configuration=Release /p:Platform=x64 /m
- Develop the port in VRF_C2SIM only. The C++ repo is a FROZEN oracle; its bugs (PORT.md sec 6) are
  fixed in the port, not there. The port's VrfFacade is a SEPARATE, deliberately-diverging copy - parity
  is the golden TRACE, not source identity.
- Parse C2SIM by DESERIALIZING into the SDK's XSD schema types (C2SIM.Schema10x via ToC2SIMObject<T>),
  NOT by hand-navigating element names. InitParser/OrderParser are the pattern.
- Verify parity-critical logic OFFLINE first (LOCAL only; the exe loads VrfBridge for value types so the
  MAK bin dirs must be on PATH): `--translator-selftest`, `--parse-init <file> [clientId]`,
  `--parse-order <file>`, `--report-selftest`, `--sequencer-selftest`, `--verb-selftest` (expect the
  counts in START_HERE "Run / verify"). Build the app with
  `DOTNET_CLI_USE_MSBUILD_SERVER=false ... --disable-build-servers` (concurrent dotnet builds deadlock
  the shared build server - a whole cycle was lost to this).
- For a LIVE run, follow RUNBOOK sec 7 EXACTLY: RTI **4.6.1** on PATH (NOT 4.6b), `MAKLMGRD_LICENSE_FILE`
  from Machine scope, cwd=VRF bin64 + `--contentRoot`, matching FED/FOM, a FRESH appNumber; PushInit
  BEFORE starting the app (it late-joins); STOP CLEANLY via `tools/StopIface` (server -> UNINITIALIZED),
  NEVER force-kill a joined federate. Runs self-clean (Solution A); run `tools/ResetVrf` to clear ORPHANS
  after a crash/force-kill OR a heavy/re-pushed run (Solution A misses late-created objects). Do NOT
  restart the C2SIM broker as a habit (RUNBOOK sec 6). The .NET app log FLUSHES - read it directly.
- Keep docs/PORT.md, docs/APP.md, docs/START_HERE.md, docs/SEMANTIC_MAPPING.md current AS you work.

Start by reading START_HERE.md, then report the git state and exactly what you'll do first.
Do not edit or run until you've done that.
```

---

Notes for the human pasting this:
- The prompt points at the docs rather than restating them, so it does not go stale as the work
  progresses. Keep the docs current; the prompt stays valid.
- All work is pushed (2026-07-12): PORT `VRF_C2SIM.git` main, FORK `OpenC2SIM.github.io.git`
  dev/sdk-fixes (submodule tracks port main; `git log` for exact tips). To resume in the CLOUD,
  clone the fork + `git submodule update --init` - but remember the cloud can only do docs/planning
  (no MAK -> no build/run). Real work needs this machine.
- If you have moved a repo, update the paths at the top.
- If a lot of time has passed, expect the environment (license, running VR-Forces/container, the
  tileserver on 8080) to have drifted - that is why the runtime non-negotiable says "check, do not assume".
