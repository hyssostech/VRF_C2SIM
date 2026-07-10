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
  5. docs/RUNBOOK.md       - runtime procedure (only when running against live VR-Forces);
                             read sec 4 (CLEAN STOP) + sec 6 (loopback blocker) before any run
  6. docs/PHASE1_REWIRE.md, docs/TASK_EXPANSION_PLAN.md - history / verb-mapping blueprint
Then run `git log --oneline` in the PORT repo (authoritative for state) and in the fork
(the submodule pointer).

State: Phase 1 (C++ facade rewire) DONE. Port products migrated into VRF_C2SIM (C++
originals retained pending review then deletion). Phase 2 (the VrfBridge C++/CLI bridge)
DONE + verified: full facade surface + callbacks, builds green under the HLA MAK set,
runtime-load smoke passes. Phase 3 (.NET app skeleton) wires the C2SIM SDK <-> bridge.
Phase 4 IN PROGRESS: OnInitialization DONE + offline-verified (InitParser deserializes via
the SDK's schema types; UnitTranslator ports the create* factories; STP init -> 80 units,
49 creatable, 4 areas = golden trace). Latest submodule commit 378c71c. NEXT: OnOrder <-
executeTask, then reports <- reportCallback, then a LIVE run + golden-trace diff.

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
- Verify parity-critical logic OFFLINE where possible before a live run: the app has
  `--translator-selftest` and `--parse-init <file>` modes that need no VR-Forces.
- If/when running against live VR-Forces: confirm env (PORT.md sec 4 + RUNBOOK sec 1); judge
  "connected" by THREAD COUNT not the block-buffered log; STOP CLEANLY (server -> UNINITIALIZED),
  NEVER force-kill a joined federate; do NOT restart the C2SIM broker as a habit (RUNBOOK sec 6).
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
