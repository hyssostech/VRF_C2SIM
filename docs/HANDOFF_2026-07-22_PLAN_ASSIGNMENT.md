> **SUPERSEDED (2026-07-22 evening) BY docs/HANDOFF_2026-07-22_LAUNCH_HARDENING.md.**
> That pivot SUCCEEDED - Cell C moved, the type-mapping fix landed, and two confirming runs
> then went VOID on infrastructure. Read the LAUNCH_HARDENING handoff for current truth and
> the next-session order. This file is retained for its still-accurate Cell C / spike
> background and its recorded RUN 1/RUN 2 outcome notes.

# SESSION-JUMP HANDOFF (2026-07-22) - the plan-assignment pivot

Paste into a fresh session. CURRENT truth as of 2026-07-22 evening. SUPERSEDES
HANDOFF_2026-07-21_SESSION_JUMP.md's NEXT ACTION (the region-vs-structure probe RAN -
result below). ASCII only. Supervisor mode: you supervise/gate/adjudicate; executors
read/analyze/code. Re-verify load-bearing claims against artifacts (git, traces, headers)
before trusting this prose.

## WHAT THIS IS (unchanged)
A HEADLESS C2SIM -> VR-Forces interface. C2SIM init+order in; units created, tasked, run,
scored from telemetry. ONE command, zero humans in the GUI. GUI use is DIAGNOSTIC ONLY.

## THE STRATEGIC PIVOT (user-directed 2026-07-22 - READ FIRST)
A week of black-box probing of VRF internals is an UNBOUNDED guess space with near-zero
product yield. STOP trying to reverse-engineer why the remote-create/bare-task path
freezes. BUILD ON WHAT DEMONSTRABLY WORKS.

The one non-guess asset: an AUTHORED scenario with an authored PLAN moves units correctly,
headless. Proven 3x on 2026-07-22 (Sweden + Mojave + Mojave-below-terrain; all moved and
settled ~300 m E on their route). Consequence: both ENVIRONMENTAL hypotheses for the R9
freeze are FALSIFIED - REGION (Mojave terrain) and WAYPOINT ALTITUDE (below-terrain
clamp-up). Full evidence: docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md.

The freeze is on the INTERFACE's side. The decisive, GROUNDED lead (not a guess):
- The interface tasks ONLY via bare task messages (VrfFacade.cpp moveAlongRoute:499 +
  sendTaskMsg x7). It has NEVER used a PLAN.
- VR-Forces exposes assignPlanByName(uuid, DtPlan) (vrfRemoteController.h:1735) - assign a
  whole plan to EXISTING units remotely; the vendor calls a plan "the mechanism for
  conditional/sequenced tasking from the remote side" (ground truth 0.3 sec 4).
- The moving fixture carried an authored PLAN. PLAN vs BARE-TASK-MESSAGE is the one
  variable never isolated, and it is exactly the interface-vs-fixture difference.

C2SIM's two-step (Init then Order) maps NATIVELY onto this: INIT -> instantiate units;
ORDER -> build a DtPlan, assignPlanByName onto the existing units. Two-step-native, handles
batch AND mid-run re-tasking (restartPlan/abandonPlan). This REPLACES the old
"rebuild the remote-create layer" framing of VRF_GROUNDWORK_PLAN Phase 3.

## NEXT ACTION: the plan-assignment spike (CHEAPEST-FIRST factorial)
docs/experiments/PREREG_PLAN_ASSIGNMENT_SPIKE.md - fully pre-registered. Hold TYPE =
correct real template (Tank Platoon USA) and test cheapest-first; STOP as soon as a cell
answers it:
- CELL C (NO native change - run FIRST): remote-create Tank Platoon (USA) via the
  ALREADY-EXPOSED CreateAggregate (VrfBridge.cpp:222/227) + task via CreateRoute (:255) +
  MoveAlongRoute (:287) - "R9's exact path with the CORRECT type." MOVES => the whole R9
  freeze was TYPE-MAPPING; fix = the existing TYPE_MAPPING_TABLE, no plan/authoring. STOP.
- CELL B (NO native change): authored-load a plan-LESS fixture variant + bare task. Read
  with C: B moves + C freezes => creation-path; B freezes => tasking (go to D).
- CELL D (the ONE native addition, only if B/C show tasking is the issue): assignPlanByName
  with a DtPlan replicating the fixture .pln. The ONLY new native code is one
  AssignMoveAlongPlan method (DtPlanBuilder+assignPlanByName) in VrfFacade/VrfBridge;
  createAggregate/CreateRoute/MoveAlongRoute already exist. Then /t:Rebuild + 7-copy
  redeploy (feedback-native-fixes-authorized).
DO NOT build the native change before Cell C rules it in - Cell C could make the whole
fix the mapping table (ground truth 0.1.7: the dominant R9 fault was TYPE, ~64/128 mis-map).

## OPERATIONAL STATE (end of 2026-07-22 session)
- VR-Forces DOWN (clean StopVrf). RTI infrastructure PRESERVED but is the RESTARTED-IDLE
  stack (rtiAssistant 12852 / rtiexec 46356 / rtiForwarder 48664; forwarder at 1 thread).
  PREFER A FRESH BOOT for the spike - fresh-boot RTI is known-good; the restarted stack is
  wedge-prone (see below).
- appNo marker NEXT FREE = 3578 (OPUS_EXECUTION_PLAN.md Appendix B). This session used
  3553-3577; burns 3556/3557 (RunSim arg/cwd) and 3560-3567 (wedged-RTI attempts).
- C2SIM server docker assumed RUNNING (not exercised this session - the fixture runs used
  RunSim, zero C2SIM code). Verify REST 8080 / STOMP 61613 before any C2SIM-driven run.
- Fixtures on disk: TankPltFixture_{Sweden,Mojave,Mojave_BelowTerrain}.scnx in
  C:\MAK\vrforces5.0.2\userData\scenarios\. Generator tools/FixtureGen/build_fixture.py
  (route_alt_msl override + argv site filter added this session).

## KEY INFRA FINDINGS THIS SESSION (do not re-derive)
- RTI BOOT DIALOG answerable headlessly: on a fresh boot the first launch raises "Choose
  RTI Connection" (Qt, no UIA tree). Answer via DPI-AWARE coordinate click:
  SetProcessDpiAwarenessContext(-4); GetWindowRect gives the true physical rect (573x583);
  Connect at window-relative ratio (0.668, 0.949). Select "Legatus's predefined rtiexec
  loopback connection" + "Always try to use this connection" checked. Verified working.
- TEARDOWN-RELAUNCH CAN WEDGE THE RTI FORWARDER (observers go blind, reflected=0, survives
  relaunch) - INTERMITTENT (hit once on boot-spawned RTI; did NOT recur after a restart).
  RECOVERY (user-approved, proven): StopVrf -> Stop-Process -Force the wedged rtiexec +
  rtiForwarder ONLY, KEEP the answered rtiAssistant -> LaunchVrf; fresh RTI respawns, no
  dialog. ALWAYS oracle-pre-check (WatchVrf reflected>0) after any relaunch before RunSim.
- RunSim/WatchVrf/SetSimRate MUST run cwd=bin64 (Set-Location or -WorkingDirectory; the
  `& $exe` trap inherits caller cwd -> CouldNotOpenFDD, burned 3557).
- Movement signal = static-while-paused -> moving-once-RunSim transition + reflected 9->13
  (offset-route transients) + settled endpoints. POS distances are DR-poisoned; endpoints
  and the transition are the signal. POS and RPT AGREED in all 2026-07-22 moving runs.

## NON-NEGOTIABLES
- NEVER force-kill a JOINED federate. Killing rtiexec/rtiForwarder is normally forbidden -
  the 2026-07-22 exception was a USER-APPROVED restart of DEMONSTRABLY-WEDGED processes;
  do not generalize it to healthy ones without a fresh ruling.
- ASCII only in tracked files. C++ repo c2simVRFinterfacev2.36 is a FROZEN ORACLE - never
  develop there; work in the PORT repo VRF_C2SIM (branch main; commit gated green work).
- Fresh ledgered appNo per join, from the single "*** NEXT FREE:" marker, BEFORE the join.

## START BY REPORTING
git log --oneline -5 + status -sb of the PORT repo (HEAD 4b4163c or later, clean bar the
untracked .code-workspace); the NEXT FREE marker (expect 3578); a vrf/rti process
inventory; confirmation you read this handoff + the spike prereg. Then scope the ARM-1
plan-less fixture + the ARM-2 native change and get a go before touching native code.

## OUTCOME (2026-07-22 late evening) - CELL C RAN AND **MOVED**; SPIKE CLOSED EARLY

The spike's Cell C ran live (tools/CreateTaskAgg, no native change) and the remote-created
correct-type Tank Platoon (USA) MOVED through the FULL pre-registered gate: bit-static
through ~40 s of running clock, onset 1.6 s after the task, +4 member offset-route
transients (reflected 8->13), TSK "move-along", settled ~1165 m east ~180 s bit-identical,
POS==RPT. Evidence: docs/experiments/CELLC_PLAN_ASSIGNMENT_2026-07-22/. Adjudication +
deviations (15x mid-run rate change etc.): the prereg's filled Outcome record - READ IT
before extending any conclusion.

VERDICT: the R9 remote-create freeze was **TYPE-MAPPING** (mis-map to Ground_Aggregate).
The remote-create + bare-task path WORKS with the correct type. Cells B/D not run
(registered stop-early rule); NO native change exists or is needed for this defect.
Plan-assignment stays attractive for C2SIM two-step ORDER handling but is design work,
not the freeze fix.

NEXT ACTION (product path, needs a user go for the live run): fix TYPE_MAPPING_TABLE so
C2SIM GroundVehicle/armor unit types map to real aggregate templates (R9's
11.1.225.1.1.3.0 -> 11.1.225.3.2.0.0 Tank Platoon (USA) class), then the CONFIRMING RUN =
the C2SIM-driven interface itself (init + order via the C2SIM server, docker REST 8080 /
STOMP 61613 - verify first), gated by the same movement oracle. That closes the R9
diagnosis end to end on the headless one-button path.

OPERATIONAL STATE AFTER THE RUN: VR-Forces DOWN (clean StopVrf; graceful back-end
fallback fired, no force-kill). RTI trio PRESERVED from the run's fresh boot:
rtiAssistant 8888 / rtiexec 68020 / rtiForwarder 68464. NOTE: after a StopVrf this stack
is again in the teardown-survivor class (wedge-prone) - oracle-pre-check before trusting
it, prefer fresh boot for the next run (fresh-boot ruling was CONSUMED; a new stop of
healthy rti* needs a new ruling). appNo marker NEXT FREE = 3585 (3578-3584 consumed,
0 burned). No RTI boot dialog appeared this fresh boot (persisted auto-connect held).

SCOPE CORRECTION (2026-07-22, mapping-fix review - executor-caught, supervisor-adopted):
the TYPE-MAPPING verdict above is precise for the PLATOON class (R9's/07-19's
1222.MechPlt: ArmorPlatoon 'D' -> Ground_Aggregate). It does NOT account for the
2026-07-19 one-button run's two other frozen taskees: 114.MechCoy already resolved
(statically) to the REAL Tank Company (USA) yet froze 0.0 m both channels (company-HU
move path - untested by Cell C), and 1.BdeHQ is a single M1A2 ENTITY that froze 0.0 m
both channels (entity move path - untested by Cell C). This section's earlier phrase
"closes the R9 diagnosis end to end" therefore OVERREACHED for the full 3-taskee order;
read PREREG_TYPEFIX_CONFIRMING_RUN.md sec 1 for the per-taskee predictions. Also
corrected: a supervisor brief stated "1.BdeHQ worked" - only its CREATION worked; its
movement froze (RUN_2026-07-19_MOJAVE_CHAIN.md sec 2).

TYPE-MAPPING FIX LANDED (2026-07-22, offline): UnitTranslator ArmorPlatoon now emits
11/1/225/3/2/0/0 (Tank Platoon (USA)) under default TypeMapping=RealTemplates;
GoldenParity escape hatch retains the old enum; VrfSettings.TypeMappingMode gates it;
selftest 22/22 green at build time. Comment-free R9 XML copies added (RUNBOOK 0.6):
data/R9_Mojave_{Lean_Initialization,UnitMove_Order}_NoComments.xml. NEXT: the confirming
run per PREREG_TYPEFIX_CONFIRMING_RUN.md (-RunSecs 900, mult 1, preserved-RTI + oracle
gate, budget from marker 3585).

TOOLCHAIN EVENT (2026-07-22 ~18:40, transient, RESOLVED ~19:45): a Visual Studio 18.8
updater removed the .NET 10 SDK payload + net10 runtime mid-session, then restored SDK
10.0.302 + runtime 10.0.10. Supervisor re-verified: VrfC2SimApp rebuild clean, selftest
22/22, exit 0. Residual relevance: the updater serviced MSVC components minutes before
the RUN-1 crash below (hypothesis H-ENV).

CONFIRMING RUN 1 (2026-07-22 19:16): **VOID - vrfSim FATAL CRASH** (dump 19:18:32),
after 6-unit creation (CLEAN, RealTemplates active, exact coords) + order delivery +
3x CreateRoute, BEFORE any MoveAlongRoute. Predictions UNTESTED - not a falsification.
Full outcome + 3 open crash hypotheses (H-RTI preserved-stack / H-CONTENT / H-ENV) +
registered discriminator (identical re-run on FRESH-BOOT RTI): the Outcome section of
PREREG_TYPEFIX_CONFIRMING_RUN.md. Evidence: docs/experiments/TYPEFIX_CONFIRMING_
2026-07-22/. appNos 3585-3589 consumed, 3590 burned, marker NEXT FREE = 3591.

CONFIRMING RUN 2 (2026-07-22 20:28, fresh-boot RTI - user-ruled): **VOID by a DIFFERENT
mechanism** - back-end FAILED TO CREATE/JOIN the federation ("RTIinternalError TCP
connection has been broken", no dump), 0 units, reflected=0 whole trace. A fresh-boot RTI
startup race: federation-create at 20:29:05 raced transient rtiexec instances ~10-13 s
before the persistent stack settled; the "Choose RTI Connection" dialog (fresh boot)
answered by DPI-click is a flagged contributor. Does NOT reproduce RUN 1's crash and does
NOT discriminate its hypotheses. appNos 3591-3596 consumed, 0 burned. Full: prereg
"Outcome - RUN 2"; evidence docs/experiments/TYPEFIX_CONFIRMING_RUN2_2026-07-22/.

DECISION (user, 2026-07-22 evening): two VOID runs from two different INFRA failures (not
the fix) => STOP live runs; HARDEN the launcher first; HOLD the MAK case until the RUN-1
reboot+VC++-repair discriminator. Governing docs written this session:
- docs/RTI_LAUNCH_HARDENING_DESIGN.md - the next-session implementation spec (RTI readiness
  gate before back-end launch; warm resident confirmed-ready rtiexec; drop loopback
  forwarder / suppress Assistant dialog; VALIDATE against live RTI).
- docs/experiments/RTI_LAUNCH_HARDENING_RESEARCH_2026-07-22.md - URL-cited external evidence
  (the "broken != refused" readiness insight; MAK ports 4000/5000; forwarder is WAN-only).
NEXT-SESSION ORDER: (1) implement + live-validate the launch hardening; (2) reboot + VC++
repair + replay the RUN-1 route sequence (crash discriminator; MAK case only if it
reproduces); (3) THEN RUN >= 3 of PREREG_TYPEFIX_CONFIRMING_RUN.md to finally test the type
fix through to tasking. The type fix (commit 0b4529f) is offline-green but UNTESTED end to
end - nothing tonight changed that.

OPERATIONAL STATE (end of 2026-07-22 session, CLEAN): VR-Forces fully DOWN (StopVrf EXIT 0;
the app resigned exit 0; the RUN-1 and RUN-2 vrfGui orphans were BOTH cleared - RUN 1 by
user force-kill ruling, RUN 2 under the standing failed-own-join carveout). ONLY the
fresh-boot RTI trio remains, UP and untouched: rtiAssistant 40956 (20:28:41) / rtiexec
60672 (20:29:15) / rtiForwarder 61696 (20:29:18) - teardown-survivor class; oracle/readiness
pre-check before trusting it next session. C2SIM docker RUNNING (REST 8080 / STOMP 61613
verified this session). appNo marker NEXT FREE = **3597**. Crash dump preserved for MAK:
bin64\vrfSim5.0.2-MSVC++15.0_64-249613-36676.dmp.dmp (+ prior 7/14, 7/15 dumps).
RunC2SimScenario.ps1 parse regression (8c36abe) fixed in-tree this session.
