# Resume prompt

Paste the block below into a fresh session to resume the port. It forces the
source-of-truth docs to be read and the state checked before any action - do not
shortcut it. Last refreshed 2026-07-13 (SESSION JUMP for EXECUTION: the plan
docs/OPUS_EXECUTION_PLAN.md is WRITTEN, user-engaged (P4b held out of the first
scale run), and REVIEWED under Fable 5 (review fixes applied + pushed: exe path
RID subfolder, TrySynthesizeByTimeout signature/supersession guard, cwd
conventions, PORT.md-sec-7 commit rule, predecessor-timeout window sizing, raw
TcpClient preflight, Step 4 escalate). The next session EXECUTES it.)

Intended model topology (see the project memory note): a frontier model (Fable 5)
or the user supervises; Opus-class executes. The plan is model-agnostic - its
SUPERVISION PROTOCOL and Appendix E carry what the executor needs.

---

```
Resume the C2SIM VR-Forces -> .NET port. THIS SESSION EXECUTES
docs/OPUS_EXECUTION_PLAN.md - step by step, under supervision, honoring EVERY
gate in its SUPERVISION PROTOCOL (sec 0). Do not skip gates, do not batch past
them, do not re-plan settled steps.

WHERE THE WORK LIVES (run `git log --oneline -1` for the tips; do not trust
hashes in prose):
- PORT (source of truth + where you work): github.com/hyssostech/VRF_C2SIM.git,
  branch main. Local: C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\
  Software\Interfaces\VRF_C2SIM (nested submodule of the fork).
- FORK + SDK: github.com/hyssostech/OpenC2SIM.github.io.git, branch dev/sdk-fixes.
  SDK at Software/Library/CS/C2SIMSDK. Plan Step 1 (P4a) EDITS THE SDK - those
  changes commit to the FORK on dev/sdk-fixes (plus a port docs commit; plan 1.8).
- DEPRECATED C++ interface (FROZEN parity oracle only):
  C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36 - no git
  remote (private-remote decision still pending with the user).

EXECUTION NEEDS LOCAL: builds, the eight offline selftests, and LIVE runs all
target the local machine (MAK + VS18 + VR-Forces + the live c2sim-server).

Before touching ANYTHING, read in the PORT repo, in order:
  1. docs/OPUS_EXECUTION_PLAN.md  - THE plan. Read sec 0 (supervision protocol,
     gates, hard rules, commit mechanics) and Appendix E (confidence + unknowns)
     in full before Step 1. Appendices A-D carry the preflight checklist, appNo
     ledger, build commands, and the R11 telemetry rule.
  2. docs/START_HERE.md           - current status + repo state
  3. docs/PLAN_DERISK_NOTES.md    - the verified mechanics (WINS on plan mechanics)
  4. docs/RUNBOOK.md              - runtime procedure (sec 0/3/4/7/8)
  5. docs/PORT.md + docs/UNIT_MOVEMENT_RESEARCH.md - as the plan's steps cite them
Then run `git log --oneline -3` + `git status -sb` in the port repo AND the fork.

STATE (2026-07-13, end of the planning+review sessions; all pushed):
- The plan is user-engaged and REVIEWED (a Fable pass applied fixes 2026-07-13).
  Settled decisions INSIDE the plan - do not re-litigate: P4b is implemented but
  EXCLUDED from the first scale run (Step 3 DECISION note); the straggler TIMEOUT,
  not a <1.0 quorum, is the Step-5 robustness lever (2.7); Step 2.4 (MoveToLocation
  fan-out) is lean-to-cut (Appendix E).
- Execution order: Step 1 (P4a SDK HttpClient) -> Step 2 (fan-out quorum/timeout)
  -> Step 5 FIRST SCALE RUN (Steps 1-2 only) -> Step 3 (P4b) + its short live pass
  -> Step 4 (memo) -> Step 6 (csproj paths). Steps 3/4/6 may interleave earlier
  ONLY as offline work; nothing untested rides into the first scale run.
- Selftest baseline is INHERITED, not verified - confirm all eight green (plan
  sec 0.2; note the exe path's win-x64 RID subfolder) BEFORE any change.
- AppNos 3200-3350 consumed; START AT 3355 (plan Appendix B is the ledger).

START by reporting: git state of port + fork (anything unpushed/dirty), the
selftest baseline result (run them), and your intended Step 1 diff - then STOP
for GATE-DIFF sign-off before committing. Work strictly gate to gate after that.
Keep START_HERE/PORT/UNIT_MOVEMENT_RESEARCH/RESUME_PROMPT current AS work lands
(plan 0.3); after any context compaction re-read the plan + START_HERE before
deciding anything.
```

---

Notes for the human pasting this:
- The prompt points at the plan rather than restating it; the plan carries the
  gates, hard rules, code specs, and calibration (Appendix E). If the prompt and
  the plan ever disagree, the plan wins; if the plan and a source doc disagree,
  the source doc wins (plan preamble).
- All work is pushed (2026-07-13): PORT `VRF_C2SIM.git` main, FORK
  `OpenC2SIM.github.io.git` dev/sdk-fixes (submodule tracks port main).
- The C++ repo (c2simVRFinterfacev2.36) STILL has no remote - the only
  golden-trace rig exists on one disk. Decide on a private remote.
- If much time has passed: expect drift (license expiry 2026-09-15, VR-Forces /
  container up-ness, the hung vrfGui, the tileserver on 8080). The plan's
  Appendix A preflight checks these - do not assume.
