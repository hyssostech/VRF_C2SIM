# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it.

---

```
Resume the C2SIM VR-Forces -> .NET port.

HOME (the .NET port, source of truth + where you work):
  ...\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM   (nested submodule, branch main)
DEPRECATED C++ interface (ported FROM; frozen parity oracle only):
  C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36
SDK (the C2SIM half rides on it): ...\OpenC2SIM.github.io\Software\Library\CS\C2SIMSDK (branch dev/sdk-fixes)

Before doing or deciding ANYTHING, read these in the PORT repo (VRF_C2SIM), in order,
and treat them as source of truth over ANY summary or recollection (an earlier session
flailed by trusting a compaction summary instead of the docs and code):
  1. docs/START_HERE.md    - current status + repo state + build/run commands
  2. docs/PORT.md          - settled decisions WITH evidence; ESP. sec 8 (phase status)
                             and sec 10 (two-layer semantic-mapping target)
  3. docs/SEMANTIC_MAPPING.md - the CURRENT work: the port-grounded two-layer plan + status
                             (Layer-1 classifier + Unit 3 fires DONE; grounded verb->intent
                             table; the coa-gpt self-target finding). Supersedes TASK_EXPANSION_PLAN.
  4. docs/APP.md           - the .NET app, its data flow, and the parity-port DONE-vs-TODO list
  5. docs/RUNBOOK.md       - runtime procedure for a LIVE VR-Forces run. Read sec 7 (the FULL
                             .NET-port live recipe: deploy, launch env, the fixed bugs), sec 4
                             (CLEAN STOP), and sec 8 (self-service reset + the ResetVrf plan)
                             before any run.
  6. docs/PHASE2_BRIDGE.md, docs/PHASE1_REWIRE.md, docs/TASK_EXPANSION_PLAN.md - reference:
                             the C++/CLI bridge + build config, Phase 1 history, verb blueprint
Then run `git log --oneline` in the PORT repo (authoritative for state) and in the fork
(the submodule pointer).

State: Phases 1-5 DONE + the two-layer semantic mapping is UNDERWAY. The .NET port runs the full
C2SIM<->VR-Forces loop live (join -> late-join -> order over STOMP -> parse -> task -> sim runs ->
move -> complete -> TASKCMPLT + position reports -> clean stop, no stale federate; aggregates move
via opt-in `Vrf:AggregateFormation`). Newer work (2026-07-11), on top of that:
- SEMANTIC MAP (PORT.md sec 10 / docs/SEMANTIC_MAPPING.md): Layer-1 verb classifier DONE
  (`VerbMapping` + `--verb-selftest`, grounded on the real order verbs). Unit 3 fires DONE + FULL
  LIVE: ATTACK-family -> `FireAtTarget` (DtFireAtTargetTask), resolve affected -> advance -> fire
  deferred after MoveAlongRoute; verified via a SYNTHETIC distinct-target order. KEY DATA FINDING:
  COA-STP1 self-targets EVERY attack verb (AffectedEntity==PerformingEntity) - a coa-gpt data issue.
- SOLUTION A (delete-on-stop) DONE + LIVE-VERIFIED: the app deletes every object it created on
  clean-stop, so runs SELF-CLEAN (164 objects deleted, GUI empty) - NO more manual VR-Forces reloads
  between clean runs. Opt-out `Vrf:CleanupCreatedOnStop=false`.
Latest port HEAD `340d608` (29 ahead of origin/main); ALL local/UNPUSHED.

NEXT (START_HERE #4 + RUNBOOK sec 8):
1. THE IMMEDIATE TASK - ResetVrf tool (hard reset for ORPHANS from crashes/force-kills that
   Solution A cannot reach). TURNKEY PLAN in RUNBOOK sec 8 (Option 1 delete-all-reflected,
   file-free; Option 2 loadScenario needs bogoland's .scnx path).
2. Unit 4: `moveIntoFormationTask` - the REAL fix for the stuck-aggregate finding (the original
   "aggregate deep-dive"; serves it). Then Unit 2: Breach (DtBreachTask). Unit 5+: HoldObjective
   (SECURE/OCCUPY/...) / Reconnoiter (SCREEN/SCOUT).
3. Report parity polish (health/dedup/bundling); deferred sec-6 bug fixes + OnObjectInitialization
   stub; formal golden-trace diff; housekeeping (PUSH branches, delete C++ originals, decouple SDK,
   decide on `data/`).

Non-negotiables (where earlier sessions went wrong - do not repeat):
- READ the docs above BEFORE acting; after any compaction re-read them; trust docs + code
  over any summary. Re-read a file's CURRENT lines before editing it.
- Build the bridge with the VS18 (net10-capable) MSBuild, NOT VS2019 BuildTools.
- Develop the port in VRF_C2SIM only. The C++ repo is a frozen parity oracle - its known
  bugs (PORT.md sec 6) are fixed in the port, not there. The port's VrfFacade is a SEPARATE
  copy from the C++ one and is MEANT to diverge; parity is the golden trace, not source identity.
- Parse C2SIM messages by DESERIALIZING into the SDK's XSD-generated schema types
  (C2SIM.Schema10x via C2SIMSDK.ToC2SIMObject<T>), NOT by hand-navigating element names off a
  sample. InitParser is the pattern to follow for OnOrder. The C2SIM XSDs + OWL are in the repo.
- Verify parity-critical logic OFFLINE first (no VR-Forces): `--translator-selftest`,
  `--parse-init <file> [clientId]`, `--parse-order <file>`, `--report-selftest`,
  `--sequencer-selftest`, `--verb-selftest` (all self-check; expect the counts in START_HERE
  "Run / verify"). Build the app with `DOTNET_CLI_USE_MSBUILD_SERVER=false --disable-build-servers`
  (concurrent dotnet builds deadlock the shared build server - a whole cycle was lost to this).
- For a LIVE VR-Forces run, follow RUNBOOK sec 7 EXACTLY (it is the proven recipe): RTI
  **4.6.1** on PATH (NOT 4.6b - that only works for the offline DLL-load), `MAKLMGRD_LICENSE_FILE`
  from Machine scope, cwd=VRF bin64 + `--contentRoot`, matching FED/FOM, a FRESH appNumber;
  PushInit BEFORE starting the app (it late-joins); STOP CLEANLY via `tools/StopIface`
  (server -> UNINITIALIZED), NEVER force-kill a joined federate. Runs now SELF-CLEAN on clean-stop
  (Solution A deletes the app's created objects), so a manual VR-Forces reload is NO LONGER needed
  between clean runs - only reload / run ResetVrf to clear ORPHANS after a crash or force-kill
  (RUNBOOK sec 8). Do NOT restart the C2SIM broker as a habit (RUNBOOK sec 6). (The .NET app's
  console log FLUSHES - read it directly, unlike the block-buffered C++ interface.)
- Keep docs/PORT.md, docs/APP.md, docs/START_HERE.md current AS you work.

Start by reading START_HERE.md, then report the git state and exactly what you'll do first.
Do not edit or run until you've done that.
```

---

Notes for the human pasting this:
- The prompt is deliberately terse and points at the docs rather than restating
  them, so it does not go stale as the work progresses. Keep the docs current;
  the prompt stays valid.
- If you have moved a repo, update the paths at the top.
- If a lot of time has passed, expect the environment (license, running
  VR-Forces/container, the tileserver on 8080) to have drifted - that is why the
  runtime non-negotiable says "check, do not assume."
