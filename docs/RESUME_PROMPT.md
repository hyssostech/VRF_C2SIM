# RESUME PROMPT (2026-07-18 - ALL OFFLINE WORK DONE; PHASE 1 IS THE NEXT ACTION)

Paste the block below into a fresh session. It supersedes all earlier resume prompts.

---

Resume the C2SIM VR-Forces -> .NET effort in SUPERVISOR MODE (user-directed standing
model): YOU supervise - design and gate probes, adjudicate evidence adversarially, keep
docs current AS work lands - while Opus (or lower) EXECUTOR agents do the work (code,
analysis, reading, runs). Adversarial refuter pass on every load-bearing claim before
acceptance: RE-RUN executors' acceptance checks with your own hands. Every gate this
effort ran that dug deeper found something real - a poll-logic bug, an overstated doc
claim, a full warp-mechanism reinterpretation, a silent-no-op design bug, and (2026-07-18)
a supervisor instruction that would have contaminated the session's own baseline. Assume
your own instructions are defective too; executors have caught them twice. Pre-register
every probe (one variable; prediction + falsifier written BEFORE running). Movement claims
REQUIRE WatchVrf displacement - completions LIE in both directions (instant-vacuous AND
absent). ASCII-only in tracked files.

WHERE THE WORK LIVES: port repo VRF_C2SIM (submodule at OpenC2SIM.github.io/Software/
Interfaces/VRF_C2SIM, branch main, remote origin = hyssostech/VRF_C2SIM). As of
2026-07-18 EVERYTHING IS PUSHED - tip 3064991 on both main and origin/main, nothing
unpushed (this is new; earlier prompts said unpushed commits were expected). Run
git log --oneline -5 and git status -sb to confirm. PUSH POLICY (user, 2026-07-18):
push whenever it makes sense AFTER PROPER TESTING - the user's concern is losing work.
Standing interpretation: commit and push once the offline gates are green (build clean,
selftests pass, ASCII check clean, docs gated) and nothing live-unverified is being
recorded AS verified. Do not sit on completed work. READ IN ORDER:
(1) docs/VRF_GROUNDWORK_PLAN.md - THE plan of record; its Status top entries ARE the
    current state.
(2) docs/VRF_GROUND_TRUTH.md sec 0.0 cross-findings 1-7 FIRST (note the item-2
    REFINEMENT, the item-6 warp decomposition, and the NEW item-7 oracle bug), then
    0.1 catalog / 0.2 curriculum / 0.3 API audit (incl. sec 6a time-multiplier
    contract) / 0.5 scnx container as needed.
(3) docs/PHASE1_SESSION_SCRIPT.md - THE NEXT ACTION. Status READY, pre-registered
    P1-A..D, D1 resolved. Read it end to end before the session.
(4) The Phase-2 deliverables: docs/TYPE_GAP_ADJUDICATION.md (incl USER RULINGS at the
    end), docs/TYPE_MAPPING_TABLE.md, docs/TASK_VOCABULARY_V2.md,
    docs/PRIOR_ART_SURVEY.md, docs/experiments/RUNAWAY_WARP_CENSUS_2026-07-17.md
    (sec 11 = the warp reinterpretation).
(5) docs/SUPERVISED_RECOVERY_PLAN.md standing rules (its sec 3/3c ordering is
    RETIRED); RUNBOOK secs 0/0.5/0.6/7 before any live work.

STATE (2026-07-18, ALL COMMITTED): EVERY OFFLINE TRACK IS DONE. Phase 0 offline, Phase 2
offline, and the 2026-07-18 S0 gate are all complete and supervisor-verified. There is no
remaining offline work on the critical path - THE NEXT ACTION IS LIVE.
- 0.6 console capture BUILT and re-verified (WatchVrf CON,<t>,<uuid>,<level>,<msg>
  beside POS, one UTC base; CON selftest 25/25; escaping fuzzed 200k cases clean).
  POS RECORDS are byte-identical across the change; total stdout is NOT (the "#"
  summary line became culture-invariant, banners reworded). LIVE GATE PENDING, now
  FOLDED INTO the Phase 1 session.
- 0.5 ScnxDiff BUILT and re-verified (3/3 acceptance checks). .scnx units are
  S-EXPRESSIONS not XML; echelon IDs not persisted; parent-name is the only org linkage.
- tools/SetSimRate BUILT (commit dda7f75): the D1 lever. Additive, no existing file
  touched, WatchVrf untouched. No default appNo (missing appNo = hard exit 2). Waits
  for BackendCount() > 0 and refuses to send if no backend was discovered.
- 0.4 self-launch DEMOTED behind Phase 1 by the user. THREE CONFIRMED DEFECTS in
  scripts/LaunchVrf.ps1 must be fixed before it is rescheduled (prereg sec 11).
- Phase 2 offline COMPLETE: 2.1 TYPE_MAPPING_TABLE (128 = EXACT 7 / NEAR 64 / PEND 54 /
  LONE 1 / AVN 2; 71 need no user decision); 2.3 TASK_VOCABULARY_V2 (real
  collapse-to-move = HoldObjective family + CLRLND; combat verbs REQUIRE
  disaggregation; Patrol/Follow NEVER self-complete - chain poison; recommendation:
  keep external sequencer, re-key CompleteTask to displacement arrival); 2.4 census
  (controller split SUPPORTED directionally - LF 3/5 moved, HU 0/4, entity 0/2, BOTH
  codebases, echelon-confounded; 18.1-18.4 km stall band REAL and terminal at 1x;
  arrived=0 in both archived runs).

THE NEXT LIVE SESSION - PHASE 1 (run PHASE1_SESSION_SCRIPT.md verbatim):
- The USER launches VR-Forces MANUALLY per RUNBOOK. Do NOT use LaunchVrf.ps1.
- Budget ~1.5 hours. If time runs short, Step 4 (the 20x repeat) is the DESIGNATED CUT;
  P1-A/P1-B (native arrival + the 18.1-18.4 km band) are why the session exists.
- Step 0: Object Console Summary Panel FIRST - opening a unit Information dialog CLEARS
  its badge. Capture panel text before touching anything.
- WatchVrf sampleSecs=2 (the zero-code raw-vs-DR discriminator - spiky track = DR
  artifact, smooth = real motion).
- Step 1b (NEW) runs the clock-persistence pre-check on a CLOCKTEST throwaway BEFORE
  anything scored. It is excluded from every prediction - do NOT let it reinforce P1-A
  at adjudication time; it ran partly at 20x.
- appNos 3455-3459 are RESERVED (Appendix B): WatchVrf + four SetSimRate joins. Each
  SetSimRate invocation is a full join/resign and takes its own number. Next free 3460.
- IF NATIVE UNITS ALSO MISBEHAVE -> the MAK support question fires IMMEDIATELY with that
  repro + PRIOR_ART_SURVEY.md's "nobody solved this publicly" list. Stop rebuilding.

QUEUED AFTER PHASE 1: 0.6 CON gate adjudication; MSDL import spike (ScnxDiff harness
ready); 0.4 gate on a repaired script; then Phase 3 creation-layer rebuild.

PENDING USER DECISIONS (do NOT decide these yourself; the user asked to be asked WHEN
NEEDED - none of these block Phase 1, they gate Phase 3+):
- ArmorCoHQ Decision 4: A (one-field match fix -> 4 generic dismounts) vs B (retarget ->
  Tank Headquarters Section; militarily correct). Supervisor recommends B. NOTE both are
  LF class, so this cannot confound the controller-split work; and since authoring is now
  in scope, an authored armor BN HQ is a real option C the adjudication doc dismissed
  (both A and B are echelon-wrong for these 26 BN-echelon units).
- Q1 hostile-side country: (a) USA-225 as today, (b) RUS-222 mirrors where they exist,
  (c) author Country-45 (China) content. VERIFIED: 11 Chinese platforms on disk, air/
  naval/AD ONLY - ZERO ground-combat platforms, ZERO Chinese aggregates. (c) means
  authoring platforms AND units with no visual models. Ask what is driving the Chinese
  question before recommending; if exploratory, (b) is clearly right.
- Golden-order file identity for Phase-5 scoring (TASK_VOCABULARY_V2 sec 1a): the
  golden-trace MOVE pair (authoritative but MOVE-only, a thin bar) vs
  data/VRF-Approved-5June24_Order.xml (38 MOVE + 29 SCOUT, but SCOUT is dead).
- AGGREGATION-STATE POLICY - CONFIRM OR CORRECT: a supervisor inferred from the user's
  task-performability principle that entity-level units should run DISAGGREGATED where
  the verb needs combat. It is recorded as policy and will drive Phase 3, but the user
  never said it. Flagged as a possible over-read - get a yes/no.
- raw-vs-DR WatchVrf change (touches the movement oracle): supervisor recommends
  DEFERRING - Phase 1's sampleSecs=2 answers the same question at zero code cost. Build
  only if that comes back ambiguous, and never during a session.

UNRETIRED RISKS carried into the session (all recorded in the script, none papered over):
- Does the multiplier SURVIVE SetSimRate resigning? Reasoned yes (control message, type
  28, not owned object state) but NOT verified; if wrong, the tool reports success while
  the clock stays 1x. Step 1b tests it. Fallback: GUI Time Scale at 15x.
- There is NO readback for the multiplier (no getter). The WatchVrf displacement rate is
  the authoritative witness for the clock - do not go looking for an API confirmation.
- Does a third federate joining mid-run perturb the WatchVrf POS stream? UNTESTED. Step
  1b's joins now happen before any scored telemetry, so a perturbation shows up on data
  nobody is scoring.
- SetSimRate's 15 s backend-settle cap and 3 s flush are calibrated by analogy to
  ResetVrf, NOT measured.

OPERATIONAL STATE: VR-Forces CLOSED; rtiexec STOPPED; C2SIM server docker RUNNING
(REST 8080 / STOMP 61613). License expires 2026-09-15. TIMESTAMP GOTCHA: app/tool logs
stamp UTC, the machine runs local - Get-Date before comparing. Non-negotiables: never
push init to a running app; fresh appNo per join, ledgered; never force-kill a joined
federate without user approval; RTI 4.6.1 + Machine-scope license + cwd bin64 +
--contentRoot for live runs; the raw vrfSimHLA1516e CLI is CONFIRMED UNSAFE (vrfLauncher
only); XML gotchas per RUNBOOK 0.6; keep the plan / ground truth / this prompt current AS
work lands; after any context compaction re-read the plan Status before deciding anything.

DO-NOT-RELITIGATE (evidence-settled): altitude as the FREEZE cause (FIXED, both
codebases); altitude as the RUNAWAY cause (EXONERATED, both); nav data; DIS type;
formation names; pile-size-as-sufficient; name-length collisions; template quality;
echelon-as-such; member structure; aggregate-level modeling (user RULED it out,
doc-verified); "the original works better" (falsified both directions - the pristine
baseline measured ZERO correct arrivals on COA-STP1). OPEN BY DESIGN (do not close
without the queued evidence): paged-tile boundary as the stop context; warp
backend-vs-observer; and NEW 2026-07-18 - the E7 "18.1-18.4 km band at 1x" verdict
carries a qualifier, because the C++ oracle applies a time multiplier even when its own
disable flag says it is ignoring it (ground truth 0.0 item 7). Not treated as
contaminated; not treated as clean. Check raw C++ console logs if any survive.

C++ repo (c2simVRFinterfacev2.36): probe branch probe/create-altitude-above-ground at
b96688b; master stays pristine at 191933a - DO NOT develop there. Its untracked tools/
dir is BUILD OUTPUT ONLY (PushInit/PushOrder bin+obj, no source) - do not commit it.

START by reporting: git log --oneline -5 + git status -sb of the port repo; confirmation
you read the plan Status, ground truth 0.0 (items 1-7), and the Phase 1 script; then
propose the next step. Get explicit user go-ahead before ANY live work and before ANY
executor touches product code. Committing and pushing GATED, GREEN work no longer needs
a separate go-ahead (push policy above, user 2026-07-18) - but the gate itself is not
optional, and the go-ahead to WRITE the code is still required before it exists.
