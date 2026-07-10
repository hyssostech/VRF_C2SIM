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
  1. docs/START_HERE.md    - current status + repo state + build commands
  2. docs/PORT.md          - settled decisions WITH evidence; ESP. sec 8 (phase status)
                             and sec 10 (two-layer semantic-mapping target)
  3. docs/PHASE2_BRIDGE.md - the CURRENT work: the C++/CLI bridge, proven build config,
                             ordered next steps
  4. docs/RUNBOOK.md       - runtime procedure (only when running against live VR-Forces);
                             read sec 4 (CLEAN STOP) + sec 6 (loopback blocker) before any run
  5. docs/PHASE1_REWIRE.md, docs/TASK_EXPANSION_PLAN.md - history / verb-mapping blueprint
Then run `git log --oneline` in the PORT repo (authoritative for state) and in the fork
(the submodule pointer).

State: Phase 1 (C++ facade rewire) DONE + verified. Port products migrated into VRF_C2SIM
(submodule 7c6c5a6; C++ originals retained pending review then deletion). Phase 2 STARTED:
slice 1 (the outbound bridge path) BUILDS GREEN (submodule b24c380) - VrfFacade.cpp native +
VrfBridge.cpp /clr:netcore link into VrfBridge.dll under the HLA1516e MAK set. Next: runtime-
load smoke, then the callbacks slice, then the full facade surface, then the .NET app.

Non-negotiables (where earlier sessions went wrong - do not repeat):
- READ the docs above BEFORE acting; after any compaction re-read them; trust docs + code
  over any summary. Re-read a file's CURRENT lines before editing it.
- Build the bridge with the VS18 (net10-capable) MSBuild, NOT VS2019 BuildTools.
- Develop the port in VRF_C2SIM only. The C++ repo is a frozen parity oracle - its known
  bugs (PORT.md sec 6) are fixed in the port, not there. The port's VrfFacade is a SEPARATE
  copy from the C++ one and is MEANT to diverge; parity is the golden trace, not source identity.
- If/when running against live VR-Forces: confirm env (PORT.md sec 4 + RUNBOOK sec 1); judge
  "connected" by THREAD COUNT not the block-buffered log; STOP CLEANLY (server -> UNINITIALIZED),
  NEVER force-kill a joined federate; do NOT restart the C2SIM broker as a habit (RUNBOOK sec 6).
- Keep docs/PORT.md, docs/PHASE2_BRIDGE.md, docs/START_HERE.md current AS you work.

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
