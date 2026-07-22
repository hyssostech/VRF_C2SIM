# PREREG: plan-assignment tasking spike (registered 2026-07-22)

STATUS: REGISTERED, NOT RUN. This is THE next action - a NEW SESSION executes it
(fresh-boot RTI + clean context; see the handoff). Cell C (run FIRST) needs NO native
change - only Cell D does, and only if C/B rule tasking in. Written before the run per
probe discipline. ASCII only.

## Why this exists (the pivot)
A week of black-box probing of VRF internals was an unbounded guess space with near-zero
product yield (user's words, 2026-07-22). The one demonstrated asset: an AUTHORED
scenario with an authored PLAN moves units correctly, headless (Sweden + Mojave + Mojave
below-terrain, all 2026-07-22; PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md). Both
environmental hypotheses for the R9 freeze (region, waypoint altitude) are FALSIFIED.

VERIFIED FACTS that motivate the spike (grounded, not guessed):
1. The interface tasks ONLY via BARE TASK MESSAGES - VrfFacade.cpp MoveAlongRoute:499
   uses controller->moveAlongRoute; all other verbs use controller->sendTaskMsg (:508,
   :610, :619, :628, :637, :668, :687). It has NEVER built or assigned a PLAN.
2. VR-Forces exposes REMOTE PLAN ASSIGNMENT to EXISTING objects:
   assignPlanByName(uuid, DtPlan, addr, ...) (vrfRemoteController.h:1735),
   assignMultiplePlanByName(:1741), restartPlan(:1725), abandonPlan(:1837); plans built
   with DtPlanBuilder (example textIf.cxx:1494-1536). Ground truth 0.3 sec 4:
   "A plan is the mechanism for conditional/sequenced tasking from the remote side."
   Deprecation heeded: assign plans BY NAME, not by echelon (header:1733-1734).
3. The moving fixture carried an authored PLAN (move-along task in a Block, plan-name ==
   aggregate uuid, empty triggers -> auto-run). The variable never isolated is
   PLAN vs BARE-TASK-MESSAGE, which is exactly the interface-vs-fixture difference.

## The C2SIM two-step maps NATIVELY onto plan assignment (the design)
C2SIM pushes Initialization first, Order later (possibly mid-run). Plan assignment is
two-step-native and strictly better than baking the order into a loaded .scnx:
- INIT  -> instantiate units (authored-scenario load OR the existing remote-create).
- ORDER -> build a DtPlan from the order, assignPlanByName onto the EXISTING units.
Handles BOTH the batch "one-button" flow AND dynamic mid-run re-tasking (restartPlan /
abandonPlan on live units). The order handler in VrfC2SimService already exists; this
swaps WHAT it sends, not a rewrite.

## The factorial and the test cells (CHEAPEST-FIRST - each decisive, stop early)
Three variables separate the WORKING fixture from FAILING R9, and the earlier two-arm
draft missed the cheapest and most likely one (TYPE). Hold TYPE = the correct real
template (Tank Platoon (USA), object-type 3 (11 1 225 3 2 0 0)) throughout - that is what
a fixed mapping WOULD produce - and vary the other two:
  V-creation: authored-load  vs  remote-create
  V-tasking:  baked plan (fixture)  vs  bare moveAlongRoute  vs  assigned plan
Known cell A = authored-load + baked-plan = the fixture = MOVES.

CELL C (NO native change; run FIRST - cheapest, potentially the whole answer):
  remote-create Tank Platoon (USA) via the ALREADY-EXPOSED CreateAggregate
  (VrfBridge.cpp:222/227, createSubordinates) + task via the ALREADY-EXPOSED CreateRoute
  (:255) + MoveAlongRoute (:287). i.e. "R9's exact path but with the CORRECT type."
  - MOVES => the entire R9 freeze was TYPE-MAPPING. Fix = the existing TYPE_MAPPING_TABLE;
    NO plan assignment, NO authoring rewrite. Biggest, cheapest win. STOP - that is the fix.
  - FREEZES => correct type is not sufficient; the defect is creation-path or tasking. Go on.

CELL B (NO native change; small managed tasking tool): authored-load the plan-LESS fixture
  variant (FixtureGen: empty .pln), task via bare CreateRoute+MoveAlongRoute.
  - Read WITH Cell C (both are bare-task; they differ only in creation authored-vs-remote):
    B MOVE + C FREEZE => CREATION-PATH is the defect (authored structure moves under bare
      task, remote-created same-type does not) => init needs authored-load.
    B FREEZE (+ C FREEZE) => bare task fails even on authored good structure => TASKING is
      the defect => go to Cell D (the plan).

CELL D (the ONE native addition - only if B/C show tasking is the issue): remote-create
  (or authored-load) the correct type, task via assignPlanByName with a DtPlan REPLICATING
  the fixture .pln (one move-along Task in a Block, empty triggers).
  - MOVES => plan assignment is the fix; C2SIM two-step maps trivially (INIT create,
    ORDER assignPlanByName); maybe no authoring rewrite.
  - FREEZES => tasking is broken in a way plans do not fix; escalate to MAK with the
    repro (authored fixture moves via its OWN baked plan; no remote tasking path works).

WHY CELL C FIRST: it is free (all methods exist), and per ground truth 0.1.7 the dominant
R9 fault was TYPE (~64/128 mis-map to Tank Company, ~49 to Ground_Aggregate fallback). If
correct-type remote-create + plain move-along MOVES, the fix is the mapping table alone and
the native plan work is unnecessary. Do not build the native change before Cell C rules it in.

## Native change scope (Cell D ONLY - build after Cell C/B rule tasking in) - feedback-native-fixes-authorized
FIRST, RE-VERIFY IN THE HEADER (the :1735 etc. line numbers below are sourced from
ground truth 0.3, NOT re-opened this session): open
C:\MAK\vrforces5.0.2\include\vrfcontrol\vrfRemoteController.h and confirm the exact
signature of assignPlanByName + how DtPlan/DtPlanBuilder are constructed
(example C:\MAK\vrforces5.0.2\examples\remoteControl\textIf.cxx:1494-1536).
- VrfFacade: add AssignMoveAlongPlan(uuid, routeUuid) that builds a DtPlan via
  DtPlanBuilder - one move-along Task in a Block, empty triggers - REPLICATING the working
  fixture .pln shape (docs/experiments/MOJAVE_FIXTURE_2026-07-21.md grammar section), then
  calls controller->assignPlanByName(DtUUID(uuid), plan, DtSimSendToAll). The route must
  exist first (reuse the existing VrfFacade::CreateRoute:457). RISK: whether a
  DtPlanBuilder plan is structurally equivalent to the authored .pln is unverified - if
  the built plan does not move the unit, diff it (saveScenario) against the fixture .pln.
  AUTO-RUN: confirm whether an assignPlanByName'd plan runs on assignment or needs a
  restartPlan(uuid) (:1725) nudge. NOTE launchGlobalPlan is for GLOBAL plans
  (assignGlobalPlan) - NOT the per-object assignPlanByName path; do not confuse them.
- Cell C/D remote createAggregate ALREADY EXISTS end to end - VrfFacade::CreateAggregate
  (VrfFacade.cpp:441, createSubordinates) exposed via VrfBridge CreateAggregate
  (VrfBridge.cpp:222/227). So the ONLY genuinely new native code is the plan-assignment
  method; creation and route are already there. Just call CreateAggregate with the Tank
  Platoon (USA) type.
- VrfBridge: expose the one new AssignMoveAlongPlan to managed (mirror the existing
  MoveAlongRoute wrapper at VrfBridge.cpp:287).
- New managed tool tools/AssignPlan (clone tools/CreateOne pattern) OR wire into the
  VrfC2SimService order handler behind a Vrf:TaskingMode=plan flag (parity mode = the
  current moveAlongRoute path, retained as escape hatch).
- Build: back up the 7 DLL copies, /t:Rebuild (NOT plain build - false-green trap),
  redeploy all 7 copies (feedback-native-fixes-authorized). Toolchain proven healthy.

## Run pipeline (unchanged, validated 2026-07-22)
Fresh-boot RTI preferred (avoids the intermittent teardown wedge -
vrf-teardown-relaunch-wedges-rti). Per cell:
- Cell C / D-remote: LaunchVrf -Scenario TropicTortoise (a BASE with the globals + model
  set, no combat units), then a managed tool REMOTE-CREATES the Tank Platoon (USA) +
  route and tasks it (bare move-along for C; assignPlan for D). Same pattern as
  tools/CreateOne, which already creates into a loaded base.
- Cell B: LaunchVrf -Scenario <plan-less fixture variant>, then the managed tasking tool
  issues bare CreateRoute+MoveAlongRoute against the loaded aggregate's deterministic uuid.
Common tail: ORACLE PRE-CHECK (WatchVrf reflected>0 BEFORE any RunSim) -> start WatchVrf
main -> RunSim (cwd=bin64, mult 1) -> analyze static->moving transition + reflected 9->13
+ settled endpoints -> StopVrf. Ledger appNos from the marker (NEXT FREE 3578) BEFORE each
join. RunSim MUST run cwd=bin64 (Set-Location or -WorkingDirectory; the & $exe trap burned
3557). NOTE: remote-create (C/D) needs the sim RUNNING to execute tasks - create into the
loaded base, then RunSim; verify the created unit reflects (POS) before tasking.

## Outcome record (empty at registration)
- Cell C (remote-create correct type + bare task) result:
- Cell B (authored-load plan-less + bare task) result:
- Cell D (correct type + assigned plan) result [only if built]:
- Joint verdict (TYPE-MAPPING / CREATION-PATH / TASKING-PATH):
- Native change committed at [only if Cell D built]:
