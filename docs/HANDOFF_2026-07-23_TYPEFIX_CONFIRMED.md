# SESSION-JUMP HANDOFF (2026-07-23) - type fix CONFIRMED; company + entity freezes are next

THE CURRENT entry point. SUPERSEDES HANDOFF_2026-07-22_LAUNCH_HARDENING.md (its STEP 1/2/3 plan
is now EXECUTED - see below). Convention: the NEWEST HANDOFF_*.md by `git log` is the entry
point - if `git log` shows one committed AFTER this file, read that. Paste the bootstrap prompt
(bottom) into a fresh session. ASCII only. Supervisor mode: you supervise/gate/adjudicate;
executor agents read/analyze/code. RE-VERIFY every load-bearing claim below against artifacts
(git, run traces, file headers, the marker) before trusting this prose - prior sessions found
errors in handoffs and preregs, and finding one is a win.

## ONE-LINE STATUS
The R9 type-mapping fix is CONFIRMED end to end on the headless product path: the confirming run
(RUN 3, commit 42b7f6b) reached tasking without crashing and 1222.MechPlt MOVED ~1163 m east
(POS==RPT). The two OTHER R9 taskees - 114.MechCoy (company) and 1.BdeHQ (entity) - FROZE as
predicted; they are SEPARATE, still-open move-along defects and the next targets.

## WHAT THIS IS (unchanged)
A HEADLESS C2SIM -> VR-Forces interface. C2SIM init+order in; units created, tasked, run, scored
from telemetry. ONE command, zero humans in the GUI (GUI use is DIAGNOSTIC ONLY). Port repo
VRF_C2SIM (this repo; branch main; commit gated-green work directly; push when offline gates
green). The C++ repo c2simVRFinterfacev2.36 is a FROZEN ORACLE - never develop there.

## WHERE WE ARE (verify each against the cited commit/file/artifact)

1. TYPE FIX CONFIRMED (commit 42b7f6b; evidence runs/20260723T174540Z_run/; full verdict in
   docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md "Outcome - RUN 3"). Two prior confirming
   runs were VOID on infrastructure; RUN 3, on the hardened launcher after a reboot, was VALID
   (all sec-2 gates met: RealTemplates active, 6 units created, order delivered, oracle live,
   RPT channel non-empty). 1222.MechPlt (ArmorPlatoon -> Tank Platoon (USA) 11.1.225.3.2.0.0,
   VRF aggregate e62d0a8b): bit-static at spawn 34.612956,-116.600487 (t=27.6-31.7) -> onset
   ~t=34-36 -> SETTLED bit-identical 34.612956,-116.587783 from t=162.7 to t=962.9 (393 samples);
   ~1163 m due east (matches Cell C ~1165 m, R9 route ~1155 m); POS==RPT (RPT reports
   34.612956,-116.594174 mid then 34.612956,-116.587860 settled, plus move-along TASKCMPLT).
   Decisive falsifier did NOT fire. This is the two-VOID-blocked end-to-end confirmation.

2. STEP 2 CRASH DISCRIMINATOR: NO REPRODUCTION (folded into RUN 3). Reached and passed the exact
   3x-CreateRoute-then-MoveAlongRoute sequence RUN 1 crashed on; clean run (exit 0), NO vrfSim
   dump written 2026-07-23. => the RUN-1 crash was ENVIRONMENTAL (mid-session MSVC servicing
   timing and/or the aged preserved RTI stack), cleared by reboot + fresh vrfSim load + a fresh
   gate-verified RTI. NO MAK support case. Held even reboot-ONLY (user chose no VC++ repair),
   which tightens attribution to the reboot/fresh-load. Procedural fix stands: never launch
   vrfSim during/right after toolchain servicing.

3. STEP 1 RTI READINESS GATE: implemented + adjudicated + partly live-validated (commit 1e649dd;
   spec+addenda docs/RTI_LAUNCH_HARDENING_DESIGN.md). tools/RtiProbe (pre-launch create-or-join
   with internal retry+backoff, exit 0/1/2) + fatal Stage 2c gate in RunC2SimScenario.ps1 before
   any back-end launch/PushInit. Validated live: WARM pass (appNo 3597 exit 0) and DOWN-but-
   startable (appNo 3598 exit 0). MAJOR OPERATIONAL FINDING: MAK AUTO-STARTS a down RTI - the
   gate/probe brings the RTI up, absorbs the startup churn, warms it, verifies it, and LEAVES it
   running (the RUN-2 fix, better than designed; no separate RTI-launch step needed). ONLY the
   genuine-unserviceable NEGATIVE path is unvalidated (auto-start prevents faking "down"; needs a
   license/config/port failure) - it did NOT block anything and is low priority.

4. OPEN DEFECTS - the WHOLE R9 order is NOT yet working (these are the next targets). Both froze
   in RUN 3 AFTER receiving their MoveAlongRoute (tasking WAS delivered - CreateRoute +
   MoveAlongRoute issued for each - so it is an EXECUTION defect, not a delivery gap), and both
   were bit-static on BOTH channels:
   - 114.MechCoy (company, VRF 42d76acf): FROZE at 34.647629,-116.693388, no task-complete.
     Resolves (statically) to a REAL Tank Company (USA) (objectType 3:11:1:225:5:2:0:0,
     ground-higherUnit-disaggregated-movement.sysdef, = HQ Sec + 3x Tank Platoon (USA), each a
     proven mover). Type fix leaves it UNCHANGED. Two live sub-hypotheses (neither settled):
     (a) it resolves LIVE to the generic Ground_Aggregate despite the static best-match;
     (b) the HU (higher-unit) move-along controller has a defect distinct from the LF (platoon
     disaggregated-movement) controller RUN 3 exercised. The company-HU move is the MORE
     IMPACTFUL target (companies are common).
   - 1.BdeHQ (entity, VRF 8a7916fc): FROZE at 34.608416,-116.712685, no task-complete. A single
     M1A2 ENTITY. No entity move has ever been proven through the interface; likely an
     independent entity move-along defect.

## NEXT-SESSION ORDER (get a user go before any LIVE run; pre-register every probe)
Priority order, but the user chooses the target:
1. DIAGNOSE THE COMPANY-HU FREEZE (114.MechCoy) - highest value. Start OFFLINE: distinguish
   sub-hypothesis (a) live-type vs (b) HU-controller. A cheap live probe (bespoke tool, like
   Cell C's tools/CreateTaskAgg) can remote-create a correct-type Tank Company (USA) and issue a
   bare MoveAlongRoute on R9's CO path - if it MOVES, the interface's company handling is the
   fault, not the controller; if it FREEZES, the HU controller/type is the fault. Pre-register
   with prediction + falsifier; ledger appNos from the marker (NEXT FREE 3606).
2. DIAGNOSE THE ENTITY FREEZE (1.BdeHQ) - same shape: bespoke remote-create a single M1A2 entity
   + bare MoveAlongRoute; MOVES vs FREEZES localizes it.
3. OPTIONAL: a REPLICATE of RUN 3 (confidence on the platoon result - already decisive via the
   bit-identical settled plateau + POS==RPT, so low priority); and the NEGATIVE-gate test
   (redesign Test N with a genuinely-unserviceable RTI: rename the license, break rid.mtl, or
   occupy rtiexec port 4000 - then RtiProbe must exit 1 and Stage 2c must refuse the launch).

Movement claims REQUIRE the full gate: static-while-paused -> moving-once-tasked transition +
SETTLED endpoints + POS/RPT agreement (quote BOTH channels on any disagreement; raw POS
distances are dead-reckoning-poisoned - never gate on a raw distance).

## READ IN ORDER (then re-verify against artifacts)
1. THIS FILE.
2. docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md - "Outcome - RUN 3" (the confirmed verdict
   + the per-taskee heterogeneity the next work chases) and the movement-oracle reading rules.
3. docs/experiments/RUN_2026-07-19_MOJAVE_CHAIN.md - the company/entity freeze baseline (what
   the type fix does NOT cover), background for the two open defects.
4. docs/RTI_LAUNCH_HARDENING_DESIGN.md - the STEP 1 gate design + ADJUDICATION ADDENDUM (A1-A7)
   + IMPLEMENTATION STATUS; and docs/experiments/PREREG_STEP1_COLD_NEGATIVE_AND_STEP2_2026-07-23.md
   (the auto-start finding + STEP 2 no-reproduction + the open negative-gate test).
5. Background: docs/experiments/PREREG_PLAN_ASSIGNMENT_SPIKE.md (Cell C, the platoon proof-of-
   mover and the bespoke tools/CreateTaskAgg pattern to reuse for the company/entity probes).

## OPERATIONAL STATE (end of 2026-07-23 session)
- HEAD is at/after 42b7f6b (the RUN-3 commit; THIS handoff commits on top, so use "newest
  HANDOFF by git log", not a pinned hash), branch main, pushed/in sync with origin; working tree
  clean bar the untracked c2simVRFinterface-workspace.code-workspace (and this handoff file until
  it is committed). All gated-green work committed + pushed.
- VR-Forces DOWN (clean teardown after RUN 3). C2SIM server docker UP (verified REST 8080 = HTTP
  response, STOMP 61613 listening this session; re-verify before any C2SIM-driven run).
- RTI: the RUN-3 stack is RESIDENT and was gate-verified serviceable this session: rtiAssistant
  51140 / rtiexec 8196 / rtiForwarder 47544 (auto-started 13:31; idle thread counts 9/2/1 are
  NORMAL and serviceable - do NOT read a low idle thread count as "wedged", proven this session).
  INVENTORY at start; do not rely on these PIDs. The Stage 2c gate (or a standalone RtiProbe)
  is the readiness instrument - and it will AUTO-START the RTI if it is down.
- appNo marker NEXT FREE = 3606 (OPUS_EXECUTION_PLAN.md Appendix B, the single value-bearing
  "*** NEXT FREE:" marker). RUN 3 consumed 3599-3604 (6 joins); 3605 (CreateOne stage-7b
  failure-path diagnostic) was allocated then BURNED unused (oracle gate passed, so 7b never
  ran). Ledger each appNo BEFORE its join.
- Crash dumps on disk are ALL pre-2026-07-23 (RUN-1 dump = 36676.dmp.dmp @ 2026-07-22 19:18);
  no dump written this session.

## NON-NEGOTIABLES
- One variable per probe; prediction + falsifier written BEFORE running. Movement claims REQUIRE
  static->moving transition + settled endpoints + POS/RPT agreement (quote BOTH on disagreement).
  Raw POS distances are dead-reckoning-poisoned - never gate on a raw distance.
- NEVER force-kill a JOINED federate. NEVER kill rtiAssistant/rtiexec/rtiForwarder without a
  fresh user ruling. A VR-Forces instance that failed ITS OWN join may be closed under the
  standing narrow carveout.
- ASCII only in tracked files (verify with ripgrep, NOT grep -P - this box's locale makes
  `grep -P "[^\x00-\x7F]"` ERROR OUT and report a FALSE clean; use the ripgrep-based tool).
- Fresh ledgered appNo per join. RunSim/WatchVrf/tools run cwd=bin64. Commit gated-green work to
  the port repo; the C++ repo is frozen. Get a user go before any LIVE run.
- After two consecutive RTI/VR-Forces framework failures, RESEARCH before the next attempt.

## START BY REPORTING
git log --oneline -5 + status -sb of the PORT repo (HEAD 42b7f6b or later, clean bar the
untracked .code-workspace); the "*** NEXT FREE:" marker value (expect 3606); a vrf/rti process
inventory; confirmation you read this handoff + the RUN-3 outcome + the two open-defect
descriptions. Then scope the chosen target (company-HU freeze recommended) and get a go before
any live run.

## COPY-PASTE BOOTSTRAP PROMPT (for a fresh session)
Paste everything between the lines into a fresh session.
-----8<-----------------------------------------------------------------------
Resume the C2SIM -> VR-Forces interface effort in SUPERVISOR MODE: you supervise, gate, and
adjudicate evidence adversarially; executor agents do the reading, analysis, and code. Keep the
in-repo docs current AS work lands. Pre-register every probe (one variable; prediction +
falsifier written BEFORE running). Movement claims REQUIRE the static-while-paused ->
moving-once-tasked transition + settled endpoints + POS/RPT agreement (quote BOTH channels on
disagreement); raw POS distances are dead-reckoning-poisoned - never trust a raw distance.

WHERE THE WORK LIVES: port repo VRF_C2SIM (submodule at OpenC2SIM.github.io/Software/Interfaces/
VRF_C2SIM, branch main; commit gated-green work directly; push when offline gates green). The
C++ repo c2simVRFinterfacev2.36 is a FROZEN ORACLE - never develop there.

READ IN ORDER, then re-verify every load-bearing claim against artifacts (git, run traces, file
headers, the marker) before trusting the prose:
(1) docs/HANDOFF_2026-07-23_TYPEFIX_CONFIRMED.md - THE entry point and current truth. Convention:
    the NEWEST HANDOFF_*.md by `git log` is the entry point - if git log shows one committed
    after it, read that.
(2) docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md "Outcome - RUN 3" - the confirmed verdict +
    the per-taskee heterogeneity + the movement-oracle reading rules.
(3) docs/experiments/RUN_2026-07-19_MOJAVE_CHAIN.md - the company/entity freeze baseline.
(4) docs/RTI_LAUNCH_HARDENING_DESIGN.md (STEP 1 gate + ADDENDUM A1-A7 + IMPLEMENTATION STATUS)
    and docs/experiments/PREREG_STEP1_COLD_NEGATIVE_AND_STEP2_2026-07-23.md (auto-start finding,
    STEP 2 no-reproduction, open negative-gate test).
(5) Background: docs/experiments/PREREG_PLAN_ASSIGNMENT_SPIKE.md (Cell C; the bespoke
    tools/CreateTaskAgg mover pattern to reuse for the company/entity probes).

CURRENT STATE (verify against cited commits): the R9 type-mapping fix is CONFIRMED end to end
(RUN 3, commit 42b7f6b; evidence runs/20260723T174540Z_run/). 1222.MechPlt (ArmorPlatoon ->
Tank Platoon (USA) 11.1.225.3.2.0.0) MOVED ~1163 m east, static->moving->settled (bit-identical
plateau t=162.7-962.9, 393 samples), POS==RPT, move-along TASKCMPLT. STEP 2 crash discriminator:
NO REPRODUCTION (environmental; no MAK case; held reboot-only). STEP 1 RTI gate (commit 1e649dd):
implemented + warm/auto-start-validated; MAK AUTO-STARTS + warms a down RTI via the gate; only
the genuine-unserviceable NEGATIVE gate path is unvalidated (non-blocking).

OPEN (the R9 order is NOT yet fully working - the next targets): 114.MechCoy (company, VRF
42d76acf) and 1.BdeHQ (entity, VRF 8a7916fc) BOTH received their MoveAlongRoute in RUN 3 but
FROZE (bit-static, both channels) - SEPARATE move-along defects the type fix does not cover. The
company-HU freeze is the more impactful target: sub-hypotheses (a) live-resolves to
Ground_Aggregate vs (b) HU move-along controller defect - a bespoke correct-type Tank Company
create + bare MoveAlongRoute probe discriminates them.

NEXT-SESSION ORDER (get a go before any LIVE run): (1) diagnose the company-HU freeze
(114.MechCoy) - offline hypothesis split, then a pre-registered bespoke probe; (2) then the
entity freeze (1.BdeHQ); (3) optional replicate of RUN 3 + the negative-gate test.

OPERATIONAL STATE: VR-Forces DOWN (clean). C2SIM docker UP (re-verify REST 8080 / STOMP 61613).
RTI resident + gate-verified this session (rtiAssistant 51140 / rtiexec 8196 / rtiForwarder
47544; idle thread counts 9/2/1 are NORMAL + serviceable - a low idle thread count is NOT a
wedge, proven this session) - INVENTORY at start, do not rely on those PIDs; the Stage 2c gate /
standalone RtiProbe is the readiness instrument and AUTO-STARTS a down RTI. appNo marker NEXT
FREE = 3606 (the single "*** NEXT FREE:" marker in OPUS_EXECUTION_PLAN.md Appendix B; ledger
each BEFORE the join). RunSim/WatchVrf/tools run cwd=bin64.

NON-NEGOTIABLES: never force-kill a JOINED federate; never kill rtiAssistant/rtiexec/
rtiForwarder without a fresh user ruling; ASCII only in tracked files (verify with ripgrep, NOT
grep -P - this box's locale makes grep -P report a FALSE clean); fresh ledgered appNo per join;
after two consecutive RTI/VR-Forces framework failures, research before the next attempt.

START by reporting: git log --oneline -5 + status -sb of the PORT repo (HEAD 42b7f6b or later,
clean bar the untracked .code-workspace); the "*** NEXT FREE:" value (expect 3606); a vrf/rti
process inventory; confirmation you read the handoff + the RUN-3 outcome + the two open-defect
descriptions. Then scope the chosen target (company-HU freeze recommended) and get a go before
any live run.
-----8<-----------------------------------------------------------------------
