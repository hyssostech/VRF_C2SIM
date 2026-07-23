# SESSION-JUMP HANDOFF (2026-07-22 evening) - launch hardening before the confirming run

THE CURRENT entry point. SUPERSEDES HANDOFF_2026-07-22_PLAN_ASSIGNMENT.md (whose "pivot"
framing is now historical - that pivot SUCCEEDED). Convention: the NEWEST HANDOFF_*.md by
`git log` is the entry point - if `git log` shows one committed AFTER this file, read that.
Paste into a fresh session. ASCII only. Supervisor mode: you supervise/gate/adjudicate;
executor agents read/analyze/code. RE-VERIFY every load-bearing claim below against
artifacts (git, run traces, file headers) before trusting this prose - prior sessions found
errors in handoffs and preregs, and finding one is a win.

## ONE-LINE STATUS
The R9 remote-create freeze is diagnosed (TYPE-MAPPING, platoon-scoped) and the fix is
committed + offline-green, but it is UNTESTED end to end: two live confirming runs went VOID
on two different INFRASTRUCTURE failures. Next session HARDENS the launcher, then re-tests.

## UPDATE 2026-07-23 (STEP 1 implementation landed offline)
STEP 1's C1 readiness gate is IMPLEMENTED offline-green + supervisor-adjudicated (commit
1e649dd): new tools/RtiProbe (pre-launch throwaway create-or-join with internal retry+backoff,
exit 0/1/2) + a fatal "Stage 2c" gate in RunC2SimScenario.ps1 before any back-end launch /
PushInit. Design amended vs the original spec: RTI_ASSISTANT_DISABLE is FORBIDDEN (RUNBOOK
0.5.5 - dropped from C3); forwarder-drop deferred; probe is a DEDICATED tool, not a WatchVrf
mode. Full record + the offline verification: docs/RTI_LAUNCH_HARDENING_DESIGN.md
("ADJUDICATION ADDENDUM" A1-A7, the DECISION block, and "IMPLEMENTATION STATUS (2026-07-23)").
STILL PENDING for STEP 1: LIVE validation (cold/warm/negative paths) - needs a user go. STEP 2
(reboot + VC++ repair + replay) and STEP 3 (RUN >= 3) unchanged. NEXT FREE still 3597.

## WHAT THIS IS (unchanged)
A HEADLESS C2SIM -> VR-Forces interface. C2SIM init+order in; units created, tasked, run,
scored from telemetry. ONE command, zero humans in the GUI. GUI use is DIAGNOSTIC ONLY.
Port repo VRF_C2SIM (this repo; branch main; commit gated-green work directly). The C++ repo
c2simVRFinterfacev2.36 is a FROZEN ORACLE - never develop there.

## WHERE WE ARE (verify each against the cited commit/file)

1. CELL C MOVED -> R9 freeze = TYPE-MAPPING (commit b5f840b; evidence
   docs/experiments/CELLC_PLAN_ASSIGNMENT_2026-07-22/, 04_watch_main.trace is the MOVEMENT
   ORACLE - the verification trace, distinct from the C++ "FROZEN ORACLE" repo above).
   A remote-created CORRECT-type Tank Platoon (USA) (DIS 11.1.225.3.2.0.0 = 11/1/225/3/2/0/0
   in the preregs' slashed notation; createSubordinates=true -> 4 M1A2 members) moved
   ~1165 m on R9's exact Mojave path via the ALREADY-EXPOSED CreateAggregate + CreateRoute +
   bare MoveAlongRoute (bespoke tool tools/CreateTaskAgg, NOT the product interface). Full
   gate met: bit-static through ~40 s of RUNNING clock, onset 1.6 s after task, reflected
   8->13 (a +5 delta = 4 member offset-route transients - exactly what R9's mis-mapped
   Ground_Aggregate lacked - plus the route object), settled endpoint POS==RPT. SCOPE CAVEAT: the verdict is PLATOON-scoped. It does NOT explain the 2026-07-19
   one-button run's OTHER two frozen taskees - 114.MechCoy (company; resolved to a REAL Tank
   Company (USA) yet froze) and 1.BdeHQ (a single-entity M1A2; only its CREATION worked).
   Those are SEPARATE untested defects (company-HU move, entity move), not covered by the
   type fix.

2. THE TYPE FIX LANDED (commit 0b4529f), OFFLINE-GREEN, UNTESTED LIVE.
   src/VrfC2SimApp/UnitTranslator.cs ArmorPlatoon now emits 11.1.225.3.2.0.0 (Tank Platoon
   (USA)) under the default VrfSettings.TypeMappingMode="RealTemplates"; the old
   11.1.225.1.1.3.0 (-> Ground_Aggregate freeze) is retained under "GoldenParity" (escape
   hatch). `VrfC2SimApp.exe --translator-selftest` PASSES 22/22 (supervisor re-ran it and
   counted PASS lines; the tool prints "SELF-TEST PASSED", no N/N). NO native C++ change (the
   DIS enum is a pure passthrough). Comment-free R9 inputs added for STOMP delivery hygiene
   (an XML block comment silently breaks order delivery - RUNBOOK 0.6):
   data/R9_Mojave_{Lean_Initialization,UnitMove_Order}_NoComments.xml. THIS FIX HAS NEVER MOVED A UNIT THROUGH THE PRODUCT
   INTERFACE - Cell C used a bespoke tool. The confirming run is what tests it.

3. TWO CONFIRMING RUNS, BOTH VOID ON INFRASTRUCTURE (fix never reached tasking):
   - RUN 1 (warm RTI; commit f649610): vrfSim CRASHED at tasking (MSVC minidump saved) after
     CLEAN 6-unit creation with the correct types + order delivery + 3x CreateRoute, BEFORE
     any MoveAlongRoute. So correct-type CREATION is clean; the crash is downstream.
   - RUN 2 (fresh-boot RTI; commit c267c53): vrfSim FAILED TO CREATE/JOIN the federation
     ("RTIinternalError TCP connection has been broken", no dump, 0 units, reflected=0). A
     fresh-boot STARTUP RACE: create at 20:29:05 raced transient rtiexec churn ~10-13 s
     before the persistent stack settled; the fresh-boot "Choose RTI Connection" dialog
     (answered by DPI-click) is a flagged contributor. This is NOT RUN 1's crash and does
     NOT discriminate its hypotheses.
   Full outcomes: docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md ("Outcome - RUN 1/2");
   evidence dirs docs/experiments/TYPEFIX_CONFIRMING_2026-07-22/ and
   docs/experiments/TYPEFIX_CONFIRMING_RUN2_2026-07-22/.

## NEXT-SESSION ORDER (user decision 2026-07-22: stop live runs, harden first)
DEPENDENCY GRAPH (a DAG, NOT a strict chain): STEP 1 and STEP 2 are INDEPENDENT of each
other (either order, or in parallel); BOTH must complete before STEP 3, the final
integration test. Get a user go before any LIVE run.

STEP 1 - IMPLEMENT + LIVE-VALIDATE the launch hardening.
   Spec: docs/RTI_LAUNCH_HARDENING_DESIGN.md. Evidence: docs/experiments/
   RTI_LAUNCH_HARDENING_RESEARCH_2026-07-22.md (URL-cited). Core: a readiness GATE that
   PROVES the RTI can service a create/join (a throwaway probe federate join/resign, or
   retry-on-create) BEFORE the back-end launches - a port-open check is INSUFFICIENT
   ("connection broken" != "refused"). Prefer a warm resident confirmed-ready rtiexec; on
   loopback evaluate dropping the RTI Forwarder (WAN-only) and suppressing the Assistant
   dialog (RTI_ASSISTANT_DISABLE or a persisted connection). Touches shared launch scripts
   (scripts/LaunchVrf.ps1, scripts/RunC2SimScenario.ps1) - NOT a mechanical edit; full
   adversarial review, and the gate can only be VALIDATED against a live RTI (needs a run).

STEP 2 - RUN-1 CRASH DISCRIMINATOR (independent of STEP 1; MUST clear before STEP 3).
   Reboot; repair BOTH x64 + x86 VC++ 2015-2022 redistributables; bring RTI + C2SIM docker
   back up clean; replay the exact 3x-CreateRoute-then-MoveAlongRoute sequence on a warm RTI.
   REPRODUCES => application/API fault => open a MAK support case (dump preserved:
   bin64\vrfSim5.0.2-MSVC++15.0_64-249613-36676.dmp.dmp; MAK wants dump + vrfSim/vrfGui logs
   + rid.mtl + versions + the call sequence) and DO NOT run STEP 3 until MAK resolves it (it
   would crash again). DOES NOT reproduce => mid-session MSVC servicing was the culprit (a VS
   18.8 updater serviced MSVC minutes before RUN 1) => procedural fix only (never launch
   vrfSim during/right after toolchain servicing), and STEP 3 is clear to run. NOTE: the
   reboot CLEARS the resident RTI trio and docker - expected and harmless, because STEP 3
   uses the hardened launcher, which re-establishes + re-gates a warm RTI at every launch.

STEP 3 - RUN >= 3 of the confirming run. ONLY after BOTH: STEP 1 (launcher hardened +
   validated) AND STEP 2 (crash resolved or shown NOT to recur). STEP 3 exercises the exact
   tasking path where RUN 1 crashed, so running it before STEP 2 clears risks a third VOID.
   docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md - its per-taskee predictions are
   UNCHANGED and still govern: 1222.MechPlt (platoon) MOVES = HIGH confidence (Cell C proved
   the type+path); 114.MechCoy (company) and 1.BdeHQ (entity) UNCERTAIN and scored
   separately (their freeze would be a NEW defect, not a refutation of the platoon fix).
   Run with the NoComments inputs, -RunSecs 900, TimeMultiplier 1. DECISIVE FALSIFIER:
   1222.MechPlt bit-static through a running clock AFTER its MoveAlongRoute = the type fix is
   insufficient even for the platoon. This is the run that finally tests the fix end to end.

## READ IN ORDER (then re-verify against artifacts)
1. THIS FILE.
2. docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md (the registration + both VOID outcomes
   + the governing per-taskee predictions/falsifiers for RUN >= 3).
3. docs/RTI_LAUNCH_HARDENING_DESIGN.md (step 1 spec) +
   docs/experiments/RTI_LAUNCH_HARDENING_RESEARCH_2026-07-22.md (its evidence).
4. Background as needed: docs/experiments/PREREG_PLAN_ASSIGNMENT_SPIKE.md (Cell C, the
   proof-of-mover); docs/experiments/RUN_2026-07-19_MOJAVE_CHAIN.md (the company/entity
   freeze baseline the type verdict does NOT cover).

## OPERATIONAL STATE (end of 2026-07-22 session)
- VR-Forces DOWN (clean). C2SIM server docker RUNNING (REST 8080 / STOMP 61613 verified this
  session; re-verify before any C2SIM-driven run). .NET 10 SDK/runtime restored (a VS 18.8
  updater removed then restored it mid-session; build + selftest re-verified green).
- RTI: the fresh-boot trio was left RESIDENT at session end (rtiAssistant 40956 / rtiexec
  60672 / rtiForwarder 61696). INVENTORY at session start - do not rely on these PIDs; they
  may be gone, and step 2's reboot clears them regardless. Teardown-survivor class: run the
  readiness / movement-oracle pre-check before trusting a resident stack.
- appNo marker NEXT FREE = 3597 (OPUS_EXECUTION_PLAN.md Appendix B, the single value-bearing
  "*** NEXT FREE:" marker). Ledger each appNo BEFORE its join; burned numbers are burned.
- Crash dump preserved (MAK material): bin64\vrfSim5.0.2-MSVC++15.0_64-249613-36676.dmp.dmp.
- Repo: branch main, in sync with origin. This handoff + the launch-hardening docs + the
  audit fixes are committed ON TOP of c267c53 (so HEAD is at/after c267c53); working tree
  clean bar the untracked .code-workspace. scripts/RunC2SimScenario.ps1 stage-3 parse
  regression (from 8c36abe) was fixed this session.

## NON-NEGOTIABLES
- One variable per probe; prediction + falsifier written BEFORE running. Movement claims
  REQUIRE the static-while-paused -> moving-once-run transition + settled endpoints +
  POS/RPT agreement (quote BOTH channels on any disagreement). Raw POS distances are
  dead-reckoning-poisoned - never gate on a raw distance.
- NEVER force-kill a JOINED federate. NEVER kill rtiAssistant/rtiexec/rtiForwarder (the
  2026-07-22 fresh-boot stop was a consumed one-time user ruling; a new stop of healthy
  rti* needs a new ruling). A VR-Forces instance that failed ITS OWN join may be closed
  under the standing narrow carveout.
- ASCII only in tracked files. Fresh ledgered appNo per join. RunSim/WatchVrf/tools run
  cwd=bin64. Commit gated-green work to the port repo; the C++ repo is frozen.
- After two consecutive failures against the RTI/VR-Forces framework, RESEARCH before the
  next attempt (done this session; that is why we are hardening, not re-running blind).

## START BY REPORTING
git log --oneline -5 + status -sb of the PORT repo (HEAD c267c53 or later, clean bar the
untracked .code-workspace); the "*** NEXT FREE:" marker value (expect 3597); a vrf/rti
process inventory; confirmation you read this handoff + the confirming-run prereg + the
launch-hardening design. Then scope STEP 1 (the launcher readiness gate) and get a go
before any live run.
