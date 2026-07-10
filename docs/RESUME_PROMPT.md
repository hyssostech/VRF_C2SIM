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
  3. docs/APP.md           - the CURRENT work: the .NET app, its data flow, and the
                             Phase 4 parity-port DONE-vs-TODO list
  4. docs/PHASE2_BRIDGE.md - reference: the C++/CLI bridge (DONE) + proven build config
  5. docs/RUNBOOK.md       - runtime procedure for a LIVE VR-Forces run. Read sec 7 (the
                             FULL .NET-port live recipe: deploy, launch env, the 6 fixed
                             bugs) + sec 4 (CLEAN STOP) before any run.
  6. docs/PHASE1_REWIRE.md, docs/TASK_EXPANSION_PLAN.md - history / verb-mapping blueprint
Then run `git log --oneline` in the PORT repo (authoritative for state) and in the fork
(the submodule pointer).

State: Phases 1-5 DONE. The .NET port RUNS THE FULL C2SIM<->VR-Forces LOOP LIVE (verified
2026-07-10 vs VR-Forces HLA + c2sim-server 4.8.4.9): join -> late-join (49 units) -> order
over STOMP -> parse -> task (entity + disaggregated aggregate) -> sim runs -> unit moves ->
completes -> TASKCMPLT + position reports -> clean stop, no stale federate. Aggregate movement
works via opt-in `Vrf:AggregateFormation` (14.MechBn moved). COA-STP1 (128 units, 42 tasks)
validated the pipeline AT SCALE but showed Wedge is necessary-not-sufficient for its aggregate
types (PORT.md sec 10). Latest port HEAD `fcba5f4`; submodule pointer `222fddf`; ALL local/UNPUSHED.
NEXT (START_HERE "immediate next task"): (1) aggregate deep-dive (why most COA-STP1 aggregates
stay stuck - per-type formation / planAndMoveToTask); (2) report parity polish (health/dedup/
bundling); (3) deferred sec-6 bug fixes + the OnObjectInitialization stub; (4) the two-layer
TaskActionCode->vrftask semantic mapping (the big value-add); (5) formal golden-trace diff;
(6) housekeeping (PUSH branches, delete C++ originals, decouple SDK, decide on `data/`).

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
  `--sequencer-selftest` (all self-check; expect the counts in START_HERE "Run / verify").
- For a LIVE VR-Forces run, follow RUNBOOK sec 7 EXACTLY (it is the proven recipe): RTI
  **4.6.1** on PATH (NOT 4.6b - that only works for the offline DLL-load), `MAKLMGRD_LICENSE_FILE`
  from Machine scope, cwd=VRF bin64 + `--contentRoot`, matching FED/FOM, a FRESH appNumber;
  PushInit BEFORE starting the app (it late-joins); STOP CLEANLY via `tools/StopIface`
  (server -> UNINITIALIZED), NEVER force-kill a joined federate; RELOAD the VR-Forces scenario
  between heavy runs (entities accumulate -> creates stop reflecting). Do NOT restart the C2SIM
  broker as a habit (RUNBOOK sec 6). (The .NET app's console log FLUSHES - read it directly,
  unlike the block-buffered C++ interface.)
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
