# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the environment to be checked before any
action - do not shortcut it.

---

```
Resume the C2SIM VR-Forces -> .NET port.

Repos:
- DEPRECATED C++ interface (ported FROM; the working docs live here):
  C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36
- .NET port target VRF_C2SIM (submodule in the hyssos OpenC2SIM fork):
  ...\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM

Before doing or deciding ANYTHING, read these in the C++ repo, in order, and treat them
as source of truth over ANY summary or recollection (the previous session flailed by
trusting a compaction summary instead of the docs and the code):
  1. docs/START_HERE.md   - current status (2026-07-09 outcomes) + build/run pointers
  2. docs/PORT.md         - settled decisions WITH evidence; ESP. sec 10 (bare-support
                            finding, two-layer architecture, the aggregate-movement fix)
  3. docs/RUNBOOK.md      - operational runtime procedure; read sec 4 (CLEAN STOP) and
                            sec 6 (the runtime blocker + its fix) BEFORE any run
  4. docs/PHASE1_REWIRE.md, docs/TASK_EXPANSION_PLAN.md - as needed
Then run `git log --oneline` in that repo - it is authoritative for state.

State: Phase 1 rewire is DONE and verified. The aggregate-movement fix -
`controller->setAggregateFormation(uuid,"Wedge")` before `moveAlongRoute` - is validated
against the MAK API and prototyped as an UNCOMMITTED C++ spike that builds clean, but the
live visual proof was NOT landed: the Dockered C2SIM loopback proxy was degraded (the prior
session over-restarted Docker), stalling the interface's STOMP connect. The run method itself
succeeded many times that session.

Pick a task (or ask me):
  (a) Land the live proof on the C++ spike - reset the runtime per RUNBOOK sec 6 (if a RAW
      TCP connect to 127.0.0.1:61613 is not near-instant, restart Docker Desktop; confirm
      loopback is fast), then run RUNBOOK sec 3 (push init -> start interface -> push order)
      and confirm the disaggregated aggregates move. Keep the formation spike uncommitted/branch.
  (b) Start the real .NET work in VRF_C2SIM - the two-layer C2SIM-semantics -> vrftasks
      mapping (PORT.md sec 10), where the fix belongs and .NET SDK networking is reliable.

Non-negotiables (where the last session went wrong - do not repeat):
- READ the docs above BEFORE acting; after any compaction re-read them; trust docs + code
  over any summary. Re-read a file's CURRENT lines before editing it.
- Before any run, confirm the env (PORT.md sec 4 + RUNBOOK sec 1): VR-Forces HLA CWIX-2024
  session 1 up; C2SIM container up (8080 REST / 61613 STOMP); and a RAW TCP connect to
  127.0.0.1:61613 near-instant. Do NOT restart the C2SIM broker as a habit - that degraded
  the loopback proxy last time.
- Runtime discipline (RUNBOOK): judge "connected" by THREAD COUNT, not the block-buffered
  log. Stop the interface CLEANLY (drive the C2SIM server to UNINITIALIZED via STOP+RESET so
  it resigns); NEVER force-kill a joined interface (strands an RTI federate -> next run hangs
  -> needs a VR-Forces reload).
- Parity: the C++ interface reproduces current behaviour; its known bugs (PORT.md sec 6) are
  fixed in the .NET port, not here. The formation fix is a deliberate exception.
- Keep docs/PORT.md, docs/RUNBOOK.md, docs/START_HERE.md current AS you work.

Start by reading START_HERE.md, then report the git state and exactly what you'll do first.
Do not edit or run until you've done that.
```

---

Notes for the human pasting this:
- The prompt is deliberately terse and points at the docs rather than restating
  them, so it does not go stale as the work progresses. Keep the docs current;
  the prompt stays valid.
- If you have moved the repo, update the path on the first line.
- If a lot of time has passed, expect the environment section to have drifted
  (license, running VR-Forces/container, the tileserver on 8080) - that is why
  the prompt says "check, do not assume."
