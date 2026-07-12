# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-12 (Layer-2 units + git push for a session jump).

---

```
Resume the C2SIM VR-Forces -> .NET port.

WHERE THE WORK LIVES (all pushed to GitHub 2026-07-12 - a session can clone fresh):
- PORT (the .NET port, SOURCE OF TRUTH + where you work): github.com/hyssostech/VRF_C2SIM.git,
  branch main = d3a8309. Local: ...\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM (nested submodule).
- FORK + SDK (the C2SIM half rides on the SDK, which lives INSIDE the fork, NOT a separate submodule):
  github.com/hyssostech/OpenC2SIM.github.io.git, branch dev/sdk-fixes = c0dc50a (submodule -> d3a8309).
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
                             (two-layer semantic map + the aggregate finding, incl. MoveIntoFormation FAILED)
  3. docs/SEMANTIC_MAPPING.md - THE CURRENT WORK: the two-layer plan + per-unit status incl. the LIVE
                             results (sec 5). Read the Unit 4 NEGATIVE result before touching aggregates.
  4. docs/APP.md           - the .NET app, its data flow, DONE-vs-TODO
  5. docs/RUNBOOK.md       - runtime procedure for a LIVE run. Read sec 7 (the FULL .NET-port live
                             recipe), sec 4 (CLEAN STOP), sec 8 (ResetVrf self-service reset + the
                             Solution-A-misses-orphans finding) before any run.
  6. docs/PHASE2_BRIDGE.md, docs/PHASE1_REWIRE.md, docs/TASK_EXPANSION_PLAN.md - reference only.
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
Everything is COMMITTED + PUSHED (port main = d3a8309, fork dev/sdk-fixes = c0dc50a). data/ scenarios
now tracked. Run git log for the exact tip.

DEAD ENDS / FAILED EXPERIMENTS (do NOT re-try these - they are settled negatives):
- **MoveIntoFormation does NOT move the disaggregated COA-STP1 aggregates.** Live-tested 2026-07-11
  (Vrf:MoveIntoFormation=Wedge): DtMoveIntoFormationTask dispatched CLEANLY to 35 aggregates (valid
  location/heading/formation, 0 crash/abandon) but moved NONE (0 completions; position diff = only
  move-along ENTITIES moved; user visually confirmed no aggregate movement on the GUI). It is WORSE
  than Wedge+moveAlong. Strong hypothesis: it needs AGGREGATED sets, not disaggregated. RULED OUT as
  the stuck-aggregate fix. Kept opt-in (default off) as a tested-but-ineffective lever. (SEMANTIC_MAPPING sec 5 Unit 4.)
- **Wedge / SetAggregateFormation alone** is NECESSARY-but-NOT-SUFFICIENT: it moved only ~3/32 COA-STP1
  aggregates (moved 14.MechBn in the golden run). Not the full fix. (PORT.md sec 10.)
- **coa-gpt data blocks the engagement verbs from being exercised by COA-STP1:** EVERY ATTACK-family
  task self-targets (AffectedEntity == PerformingEntity) -> no real fire target (fires need a SYNTHETIC
  distinct-target order to test - already done for Unit 3). BREACH obstacles are MAP GRAPHICS, not
  init-created entities, so TryResolveVrfUuid misses them -> breach degrades to advance-only. Feed back
  to coa-gpt: emit distinct AffectedEntity + real obstacle entities. (SEMANTIC_MAPPING sec 5 Units 2/3.)
- **Solution A is NOT complete cleanup** - it MISSES objects created shortly before clean-stop (race).
  A live run left 2 route/graphic orphans (a T23_AOA route) that ResetVrf then cleared. Run ResetVrf
  after any heavy/re-pushed run to guarantee a clean slate. (RUNBOOK sec 8.)
- (Older, settled in PORT.md sec 6/7) C++ debug=1 is broken; setTarget passes a C2SIM uuid to VRF
  (no-op); busy-wait thread leak; the STOMP `selector` removal was NOT a bug. Do not "fix" these.

NEXT (all LIVE-GATED - need VR-Forces + the LOCAL machine; a cloud session can only PLAN them):
1. THE AGGREGATE DEEP-DIVE (the top open question; MoveIntoFormation is ruled out). Try, in order:
   (a) `planAndMoveToTask` (DtPlanAndMoveToTask - pathfinding move-to a point; may move a disaggregated
   set where the others don't); (b) task the SUBORDINATE entities of each set directly; (c) create the
   aggregates AGGREGATED (createSubordinates=false) then move. Each = a facade+bridge add (VS18 rebuild)
   + one live COA-STP1 run; use ResetVrf to clean between runs. First, diagnose in the VR-Forces GUI:
   compare a MOVING vs a STUCK aggregate's Subsystems tab (formation valid for the type? subordinates
   "2 of 4"? damage?).
2. Exercise Breach/Reconnoiter/Escort behavior (gated tasks didn't dispatch in the run window; need a
   longer run or an ungated/synthetic order; breach needs a resolvable obstacle).
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
- All work is pushed (2026-07-12): PORT `VRF_C2SIM.git` main=d3a8309, FORK `OpenC2SIM.github.io.git`
  dev/sdk-fixes=c0dc50a. To resume in the CLOUD, clone the fork + `git submodule update --init` - but
  remember the cloud can only do docs/planning (no MAK -> no build/run). Real work needs this machine.
- If you have moved a repo, update the paths at the top.
- If a lot of time has passed, expect the environment (license, running VR-Forces/container, the
  tileserver on 8080) to have drifted - that is why the runtime non-negotiable says "check, do not assume".
