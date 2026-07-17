# RESUME PROMPT (2026-07-17 handoff - PHASE 0 OFFLINE DONE, PHASE 2 EXECUTING)

Paste the block below into a fresh session. It supersedes all earlier resume prompts.

---

Resume the C2SIM VR-Forces -> .NET effort in SUPERVISOR MODE (user-directed standing
model): YOU supervise - design and gate probes, adjudicate evidence adversarially, keep
docs current AS work lands - while Opus (or lower) EXECUTOR agents do the work (code,
analysis, reading, runs). Adversarial refuter pass on load-bearing claims before
acceptance (re-run executors' acceptance checks with your own hands - that is how real
defects have been caught). Pre-register every probe (one variable; prediction +
falsifier written BEFORE running). Movement claims REQUIRE WatchVrf displacement -
completions LIE in both directions (instant-vacuous AND absent).

WHERE THE WORK LIVES: port repo VRF_C2SIM (submodule at OpenC2SIM.github.io/Software/
Interfaces/VRF_C2SIM, branch main - run git log --oneline -3 for the tip; unpushed
commits are expected; tip at write time a61adbe, ahead 25). READ IN ORDER:
(1) docs/VRF_GROUNDWORK_PLAN.md - THE plan of record (read Status top entries first).
(2) docs/VRF_GROUND_TRUTH.md - Phase 0 deliverable (0.0 supervisor cross-findings
    FIRST - note the 0.0-#2 REFINEMENT; then 0.1 catalog / 0.2 curriculum / 0.3 API
    audit / 0.5 scnx container as needed).
(3) docs/TYPE_GAP_ADJUDICATION.md (pending user decisions) and
    docs/PHASE1_SESSION_SCRIPT.md (the next live session, pre-registered P1-A..D).
(4) docs/SUPERVISED_RECOVERY_PLAN.md - standing rules (telemetry oracle, probe
    discipline, appNo ledger); its sec 3/3c probe ordering is RETIRED.
(5) docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md parts 13-16 as
    needed; RUNBOOK secs 0/0.5/0.6/7 before any live work.

STATE (2026-07-17): Phase 0 offline work is COMPLETE and COMMITTED (50a5c0c..a61adbe):
- 0.6 CONSOLE CAPTURE BUILT: WatchVrf emits CON,<t>,<uuid>,<level>,<msg> beside POS
  (one UTC base; ConFormat reversible escaping; --con-selftest offline). LIVE GATE
  PENDING: does VRF deliver console messages over the wire in our federation.
- 0.5 SCNX HARNESS BUILT: tools/ScnxDiff (dump/diff). KEY FINDINGS (ground truth 0.5):
  .scnx units are MAK S-EXPRESSIONS not XML; echelon IDs NOT persisted; parent-name
  uuid = the only org linkage; leader=doc-order is an assumption to live-verify.
- 0.4 SELF-LAUNCH DRAFTED, NOT LIVE-GATED: scripts/LaunchVrf.ps1 (combined-mode
  vrfLauncher, refuses backend-only + existing processes, -DryRun verified) +
  docs/experiments/PREREG_0_4_SELFLAUNCH.md (prediction: ResetVrf --dry-run clean
  TWICE; top risk = session-startup dialog RISK A). Needs user approval to run.
- TYPE-GAP ADJUDICATION TABLE ready for the user (see PENDING USER below).
- PHASE 1 SESSION SCRIPT ready (docs/PHASE1_SESSION_SCRIPT.md): real types M1A2 /
  Tank Platoon (USA) LF / Tank Company (USA) HU; BOTH platoon and company cross the
  18.1-18.4 km band; badge-clearing capture protocol; open decision D1 (how to set
  20x: prefer remote setTimeMultiplier(20) - GUI toolbar caps at 15).
Earlier settled state stands: buried-birth altitude FIXED both codebases;
runaway/warp + pile split ALTITUDE-EXONERATED, open, reproduce in both codebases;
yellow badges = unread Object Console warnings; ~64/128 COA-STP1 -> Tank Company
mis-map, ~49 generic fallback, ~15 lone M1A2.

PHASE 2 OFFLINE WORK LANDED, GATED, COMMITTED (2026-07-17, commits 277cbab/375c5e0/
974f443 - all supervisor-accepted with independent re-verification):
- 2.1 docs/TYPE_MAPPING_TABLE.md: 128 = EXACT 7 / NEAR 64 / PEND 54 / LONE 1 / AVN 2;
  71/128 need NO user decision; PEND rows reproduce (not decide) the adjudications.
- 2.3 docs/TASK_VOCABULARY_V2.md: real collapse-to-move = HoldObjective family +
  CLRLND; combat verbs need DISAGGREGATION (user policy question); Patrol/Follow
  never self-complete; recommendation = external sequencer re-keyed to displacement.
- 2.4 docs/experiments/RUNAWAY_WARP_CENSUS_2026-07-17.md: controller-split SUPPORTED
  directionally (LF 3/5 moved, HU 0/4, entity 0/2, BOTH codebases;
  echelon-confounded); 18.1-18.4 km band real+terminal at 1x; arrived=0 both runs.
  SUPERVISOR ADDENDUM (sec 11) + ground truth 0.0 item 6: transient warps are
  lockstep formation-group jumps - leading candidate OBSERVER-SIDE dead-reckoning
  artifact; persistent underground end-states remain the real port-20x runaway
  class; member warp telemetry observation-suspect. Phase 1 script now runs WatchVrf
  at sampleSecs=2 as the zero-code artifact-vs-real discriminator; a raw-vs-DR
  WatchVrf read is the QUEUED code enhancement (needs user go-ahead - oracle code).

HYPOTHESIS ON RECORD (ground truth 0.0 item 2 + REFINEMENT): the LF/HU controller
boundary is per-template WIRING, not echelon - Stryker Rifle Platoon (USA Army) is
PLT-echelon yet wires HU. Cleanest probe pair: Tank Platoon (USA) (LF) vs Stryker
Rifle Platoon (USA Army) (HU), same echelon, tasked identically. E7's census is the
offline test of the same hypothesis; if E7 supports it, the live probe rises in
priority.

PENDING USER DECISIONS (do not decide these yourself):
1. TYPE_GAP_ADJUDICATION.md: gap 1 engineer (rec: Tank Breach Company proxy), gap 2
   mech-inf (rec: aggregate-Co-Infantry if dismounted OK), gap 3 mortar/rocket (rec:
   M109 FA platoon/battery proxy), item 4 ArmorCoHQ (rec: option B Tank HQ Section),
   plus policy Q1 hostile-side country (USA vs RUS mirrors), Q2 aggregate-level ever
   acceptable, Q3 surface proxy substitutions downstream, Q4 authoring new templates
   in scope.
2. 0.4 live-gate approval (supervised session; 4 app numbers; abort criteria in the
   prereg).
3. D1: 20x mechanism for Phase 1 (recommend remote setTimeMultiplier(20), the same
   mechanism every port run used).
4. C++ repo untracked tools/ dir: preserve on probe branch or leave.

SEQUENCE FROM HERE: gate E5/E6/E7 -> commit accepted deliverables -> next LIVE
session runs PHASE1_SESSION_SCRIPT.md (fold the 0.4 live gate into its bring-up if
the user approves; open the Object Console Summary Panel FIRST; if the 0.6 CON
stream works live, every later run gets VRF's own diagnostics) -> Phase 2.2
GUI-vs-remote scnx diff (needs the Phase-1 saved .scnx + a remote saveScenario
capture) -> Phase 3 creation-layer rebuild (blocked on the user's mapping
adjudications; UnitTranslator stays untouched as the parity oracle) -> Phase 4
truthful-arrival gate + runaway containment -> Phase 5 COA-STP1 scored by
displacement only.

OPERATIONAL STATE: VR-Forces CLOSED; rtiexec STOPPED; C2SIM server docker RUNNING
(REST 8080 / STOMP 61613). appNo ledger (OPUS_EXECUTION_PLAN.md Appendix B): NEXT
FREE 3455 (no joins since; RE-CHECK the ledger tail before any live work). License
expires 2026-09-15. TIMESTAMP GOTCHA: app/tool logs stamp UTC, machine runs local -
check Get-Date before comparing. Non-negotiables unchanged: never push init to a
running app; fresh appNo per join, ledgered; never force-kill a joined federate
without user approval; RTI 4.6.1 + Machine-scope license + cwd bin64 +
--contentRoot for live runs; raw vrfSimHLA1516e CLI launch CONFIRMED UNSAFE
(vrfLauncher.exe only); XML gotchas per RUNBOOK 0.6; ASCII-only in tracked files;
keep the groundwork plan / ground truth / this prompt current AS work lands; after
any context compaction re-read the plan doc before deciding anything.

DO-NOT-RELITIGATE (recovery plan sec 1 + investigation parts 9-16, all
evidence-settled): altitude as the FREEZE cause (FIXED, both codebases), altitude as
the RUNAWAY cause (EXONERATED, both codebases), nav data, terrain page-in AS THE
FREEZE CAUSE (paged-tile boundary as STOP/TERMINATION context stays OPEN - user
hypothesis, queued), DIS type, formation names, pile-size-as-sufficient, name-length
collisions, template quality, echelon-as-such, member structure. "The original works
better" is falsified in both directions - the pristine baseline measured ZERO
correct arrivals on COA-STP1.

START by reporting: git state of the port repo (git log --oneline -5, git status
-sb), whether E5/E6/E7 outputs exist and whether the plan Status shows them gated,
confirmation you read the plan Status + ground truth 0.0, and your proposed next
step - gate ungated deliverables first; get user go-ahead before any live work or
any executor touching product code.
