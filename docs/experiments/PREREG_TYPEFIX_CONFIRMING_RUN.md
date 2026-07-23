# PREREG: R9 type-mapping fix - confirming run (registered 2026-07-22)

STATUS: **RUN 3 (2026-07-23) PASSED - the type fix is CONFIRMED end to end for the platoon.**
RUN 1 + RUN 2 (2026-07-22) were both VOID on infrastructure; RUN 3, on the hardened launcher
after a reboot, reached tasking WITHOUT crashing and 1222.MechPlt MOVED ~1163 m east
(static->moving->settled, POS==RPT). 114.MechCoy (company) and 1.BdeHQ (entity) FROZE exactly
as predicted (separate defects). See "Outcome - RUN 1/2/3" at the bottom. ASCII only.

## 0. What this run tests (ONE variable)

The Cell C spike (docs/experiments/PREREG_PLAN_ASSIGNMENT_SPIKE.md, filled Outcome record;
supervisor-adjudicated) proved that a remote-created **correct-type** Tank Platoon (USA)
(EntityTypeSpec 11/1/225/3/2/0/0, createSubordinates=true -> 4 M1A2 members) MOVES end to
end on R9's exact path (CreateAggregate + CreateRoute + bare MoveAlongRoute). Verdict: R9's
freeze was **TYPE-MAPPING** - its ArmorPlatoon-class units emitted 11/1/225/1/1/3/0, which
has no Kind-11 aggregate leaf and fell back to the generic Ground_Aggregate, whose members
receive EMPTY offset routes and freeze.

Cell C used a bespoke tool (tools/CreateTaskAgg), not the product interface. This run closes
the loop: **the C2SIM-driven interface itself**, with the type-mapping fix applied, on the
2026-07-19 one-button path (scripts/RunC2SimScenario.ps1). The single changed variable versus
the frozen 2026-07-19 baseline (run 20260719T144109Z) is the interface's type mapping.

THE FIX (already implemented + offline-validated, this session):
- src/VrfC2SimApp/UnitTranslator.cs `ArmorPlatoon` now emits **11/1/225/3/2/0/0** (Tank
  Platoon (USA)) under the default `TypeMapping.RealTemplates`; the old 11/1/225/1/1/3/0 is
  retained under `TypeMapping.GoldenParity` (escape hatch, mirrors GroundWaypointAltitudeMode
  "Fixed100").
- Gated by VrfSettings.TypeMappingMode (default "RealTemplates"); VrfC2SimService.cs maps the
  setting and logs the active mode + target at init.
- Offline gate: `VrfC2SimApp.exe --translator-selftest` PASSES 22/22, including
  `ArmorPlatoon(D) RealTemplates` (asserts the fixed 3:2:0:0 - a regression fails it),
  `ArmorPlatoon(D) GoldenParity` (asserts the old 1:1:3:0), and the three R9-taskee-by-SIDC
  assertions. NO native C++ change was made or needed (VrfFacade.cpp:84 toDtType is a pure
  DIS-field copy into controller->createAggregate; VrfFacade.cpp:441/446).

Hold EVERYTHING else at the 2026-07-19 baseline (which was itself the product default +
Create-altitude mode=Live - verified from runs/20260719T144109Z_run/vrfc2simapp.log):
AggregateFormation="" (OFF), SubordinateFanOut=false, MoveIntoFormation="",
AggregatePlanAndMove=false, GroundWaypointAltitudeMode="Live", TimeMultiplier=1. This keeps
the type fix the ONLY delta. (Altitude was independently FALSIFIED as the R9 cause on
2026-07-22, so holding it at Live is not a confound.)

## 1. Per-taskee prediction and falsifiers (READ THE HETEROGENEITY)

The R9 order (data/R9_Mojave_UnitMove_Order.xml) tasks THREE units. Cell C proved only the
**platoon** case. The three taskees are NOT one hypothesis - predict and read each on its own.

| taskee | uuid | dispatch (SIDC ech) | OLD type -> resolves | NEW type -> resolves | 2026-07-19 result | prediction | confidence |
|--------|------|---------------------|----------------------|----------------------|-------------------|------------|-----------|
| 1222.MechPlt | 001aa71b-...6342 | ArmorPlatoon ('D') | 11/1/225/1/1/3/0 -> **Ground_Aggregate** | **11/1/225/3/2/0/0 -> Tank Platoon (USA)** | FROZE (POS/RPT contradicted; net -63 m, no progress) | **MOVES** and settles ~E on its route | HIGH (Cell C proved this exact type+path+createSubordinates=true) |
| 114.MechCoy | 139aa71b-...6242 | ArmorCompany ('E') | 11/1/225/5/2/0/0 -> Tank Company (USA) | **UNCHANGED** (already a real template) | FROZE 0.0 m (BOTH channels) | UNCERTAIN | LOW - not covered by Cell C |
| 1.BdeHQ | 670cfdb2-...ef24 | Tank/default ('H') | 1/1/225/1/1/3/0 -> single M1A2 (ENTITY) | **UNCHANGED** (entity) | FROZE 0.0 m (BOTH channels) | UNCERTAIN | LOW - not covered by Cell C |

**PRIMARY prediction (the load-bearing one):** 1222.MechPlt MOVES. Cell C's fixed platoon
traversed ~1165 m in ~11 s wall at ~15x (~7 m/s sim speed), so at TimeMultiplier=1 the R9
route (~1155 m) should traverse in ~165 s and settle. Onset should follow the MoveAlongRoute
task within a few seconds, with a member-offset-route transient (reflected count +4).

**DECISIVE FALSIFIER (of the adjudicated type-mapping diagnosis on the product path):**
1222.MechPlt bit-static through a RUNNING clock after its MoveAlongRoute is issued = the
Cell-C-to-product-path transfer FAILED = the type fix is insufficient even for the platoon.
That is the one result that overturns the diagnosis. Distinguish it from
delivery/infrastructure failure (see sec 2 gates): if the order never delivered, or the
oracle is blind, or the units never created, the run is INVALID, not a falsification.

**SECONDARY (NOT falsifiers of the platoon fix) - surfaced honestly, not buried:**
The type-mapping verdict does **NOT** account for the two genuinely-frozen 2026-07-19 units:
- 114.MechCoy resolved (statically) to the REAL Tank Company (USA) - objectType
  3:11:1:225:5:2:0:0, ground-higherUnit-disaggregated-movement.sysdef (HU), composed of a
  Tank HQ Section + 3x Tank Platoon (USA) (each a proven mover) - yet it froze. The
  "mis-mapped to Ground_Aggregate" story does not apply to it. Two live sub-hypotheses,
  neither settled offline: (a) it resolves LIVE to Ground_Aggregate despite the static
  best-match (VRF_GROUND_TRUTH 0.1.8 item 1: a 2026-07-15 live formation query for
  114.MechCoy returned the lowercase/generic signature - but "lowercase" is how ALL live
  aggregates report, so this is weak); (b) the HU (company move-along) controller has a
  defect distinct from the LF (platoon disaggregated-movement) controller Cell C exercised.
  The type fix leaves 114.MechCoy UNCHANGED, so if (b) is true it stays frozen.
- 1.BdeHQ is a single M1A2 ENTITY. Cell C tested no entity move. The type fix does not touch
  it. If entity move-along has an independent defect, it stays frozen.
IF EITHER STAYS FROZEN, that is a SEPARATE open defect (company-HU and/or entity
move-along), NOT a refutation of the platoon type-mapping fix - but it DOES mean the fix is
insufficient to make the WHOLE R9 order succeed. Record it as such; do not let a
1222.MechPlt success be diluted by a 114/1.BdeHQ freeze, and do not let a 114/1.BdeHQ freeze
be read as "the type fix failed."

**BRIEF/DOC CORRECTION surfaced (a supervisor-brief error worth flagging):** the task brief
states "1.BdeHQ is an ENTITY and worked." Per RUN_2026-07-19_MOJAVE_CHAIN.md sec 2, 1.BdeHQ's
*movement* did NOT work - it received MoveAlongRoute and moved zero bits (0.0 m, both
channels). Only its CREATION worked (exact spawn coords, CORRECTION 2). Do not enter this run
expecting 1.BdeHQ to move; a 1.BdeHQ freeze is the pre-existing entity-move gap, not a
regression introduced by this fix.

## 2. Run-validity gates (must pass or the run is INVALID, not a verdict)

Before reading any movement result, confirm the run was real:
- **Creation:** VrfC2SimApp log shows 6 units dispatched, and the post-init ORACLE GATE
  (RunC2SimScenario stage 7) passed on REAL coordinates. The app log MUST show
  "Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA) (11.1.225.3.2.0.0))"
  - if it says GoldenParity, the fix is not active and the run is void.
- **Delivery:** PushOrder EXIT=0 and the three MoveAlongRoute task lines appear in the app
  log (as in 2026-07-19: "CreateRoute (3 pts) for <taskee> -> MoveAlongRoute <id>").
- **Oracle live:** the oracle pre-check (stage 4) discovered objects (reflected>0); the
  scoring trace is being written; ListenReports is capturing (2026-07-19 caught it empty -
  if the RPT channel is empty again, the two-channel rule degrades to POS-only and that
  limitation must be stated, not hidden).

## 3. The movement oracle (how to read it - two channels, event-based, not distance-based)

Two INDEPENDENT channels, per the 2026-07-19 lesson:
- **POS** = WatchVrf's reflected object states (watchvrf-trace.csv).
- **RPT** = VR-Forces' own PositionReports the interface emits to C2SIM (ListenReports).

Rules (from RUN_2026-07-19_MOJAVE_CHAIN.md + PREREG_TSK_DELIVERY_2026-07-19.md):
1. **Never gate on raw POS distances** - they are dead-reckoning-poisoned (intra-move warps
   that resolve). The 2026-07-19 "63 m oscillation" of 1222.MechPlt was a POS artifact; RPT
   showed it moving. Gate on EVENTS, not metres.
2. **The signal is the transition + the transients + the settled endpoint:**
   - static-while-paused / pre-order -> moving-after-task ONSET (a few seconds after
     MoveAlongRoute for that taskee);
   - reflected-count transient at onset (the platoon's +4 member offset routes - the exact
     machinery Ground_Aggregate lacked; Cell C saw reflected 8->13);
   - a SETTLED endpoint: the same lat/lon held bit-identical across many consecutive samples,
     displaced ~east along the route, at the correct terrain-clamped altitude.
3. **POS/RPT two-channel rule (LOAD-BEARING):** on any DISAGREEMENT between POS and RPT,
   **quote BOTH channels, never one.** The canonical cautionary case is 2026-07-19
   1222.MechPlt: POS said frozen ~65 m west, RPT said moving east at ~1.4 m/s - the report
   must carry both. Agreement (POS==RPT) is the clean pass; the two frozen 2026-07-19 units
   agreed on 0.0 m, which is why their freeze STANDS.
4. Clock convention (RUN_2026-07-19 sec 1, 2026-07-20 addendum): trace t = wall-clock minus
   stage start minus a ~5.89 s trace-t0 offset. Apply the offset before aligning onset to the
   task-issue time, or every alignment carries a systematic +5.9 s error.

MOVED (HEADLESS_RUN_PLAN 4a.2 AMENDED): >= 25 m net displacement sustained across >= 3
consecutive samples AND distance to the task's FINAL waypoint DECREASED by >= 25 m. A
sideways/oscillation displacement is NOT MOVED (the amendment that reclassified 2026-07-19
1222.MechPlt from a misleading MOVED to a correct NO).

## 4. Observation window (the 2026-07-19 lesson)

2026-07-19 observed only ~145 s and that was too short to see settling; -RunSecs 900+ was the
untested cheap fix. PRIMARY: **-RunSecs 900** at TimeMultiplier=1. This is generous - the Cell
C traversal implies ~165 s of actual travel for the platoon, so 900 s leaves wide margin for
onset latency, DR transients and a clean settled plateau.

Justified sim-rate ALTERNATIVE (if wall-time budget is tight): TimeMultiplier ~10-15 with
-RunSecs ~180. Cell C VALIDATED that event gates are rate-independent and a correct-type
platoon settles cleanly at ~15x. COST: it adds a second changed variable versus the 1x
2026-07-19 baseline, so PREFER the 1x/-RunSecs 900 path for the cleanest single-variable
comparison; use the sim-rate path only if the long window is not affordable, and ledger the
SetSimRate appNo if a join is used to apply it.

## 5. Pre-run checks, budget, posture

> SUPERSEDED VALUES BELOW (this section was written for RUN 1 on 2026-07-22 afternoon):
> the appNo marker is now **NEXT FREE 3597** (not 3585 - RUN 1/RUN 2 consumed 3585-3596),
> and the RTI posture is superseded by docs/RTI_LAUNCH_HARDENING_DESIGN.md (harden + gate
> readiness first; the "use the PRESERVED stack and just re-run" posture and the PIDs
> 8888/68020/68464 below are stale). Read the current handoff for both. The rest of this
> section (server/XML/teardown mechanics) still holds.

**C2SIM server (RUNBOOK 0.6/1):** `docker ps` shows c2sim-server up; REST 8080 reachable;
STOMP 61613 reachable. `docker restart c2sim-server` (~30 s to ActiveMQ-ready) if degraded.
RunC2SimScenario checks the server by default (do NOT pass -SkipServerCheck); it also refuses
on a clientId/SystemName mismatch (init SystemName must be "STP").

**Input files (RUNBOOK 0.6 - LOAD-BEARING for delivery):** RUNBOOK 0.6 records that a large
XML block comment breaks order STOMP delivery and a prolog comment breaks init pushes. The
annotated originals contain a prolog block comment (order lines 2-9) plus inline comments;
2026-07-19 delivered them anyway (PushOrder ships the raw text and the server drops the prolog
on re-broadcast; the inline PerformingEntity comments are the small single-line kind RUNBOOK
0.6 explicitly permits). To remove ALL doubt, COMMENT-FREE copies were produced this session
and VALIDATED (well-formed, 0 comments, ASCII-only, all 3 taskee UUIDs + SystemName STP + 6
units + coordinates preserved):
- data/R9_Mojave_Lean_Initialization_NoComments.xml
- data/R9_Mojave_UnitMove_Order_NoComments.xml
RUN WITH THESE: `-Init data/R9_Mojave_Lean_Initialization_NoComments.xml -Order
data/R9_Mojave_UnitMove_Order_NoComments.xml`. The annotated originals are preserved.

**appNo budget (OPUS_EXECUTION_PLAN.md Appendix B marker = NEXT FREE 3585):** allocate the
6 the runner needs BEFORE any join - backend, frontend, oracle pre-check, scoring trace, app,
+1 spare (2026-07-19 pattern was 3510-3515). Expected consumption 3585-3590; advance the
marker to 3591. Ledger each from the single marker immediately before its join. If the
sim-rate alternative uses a SetSimRate join, add one more.

**RTI posture:** use the PRESERVED stack (post-Cell-C: rtiAssistant 8888 / rtiexec 68020 /
rtiForwarder 68464), which is now in the teardown-survivor (wedge-prone) class. GATE it with
the oracle pre-check (WatchVrf reflected>0) BEFORE trusting it. The approved NARROW restart
(StopVrf -> Stop-Process -Force the wedged rtiexec + rtiForwarder ONLY, KEEP the answered
rtiAssistant -> LaunchVrf) is the fallback ONLY if the stack is demonstrably wedged
(reflected=0 after relaunch). A FRESH BOOT (stopping healthy-idle rti*) needs a NEW user
ruling - the 2026-07-22 fresh-boot ruling was CONSUMED by the Cell C run. NEVER force-kill a
healthy joined federate.

**Teardown:** the finally-block runs StopIface (clean resign) then StopVrf on every path.
StopVrf may exit 3 with a graceful back-end fallback (documented intermittent case) - that is
acceptable and force-kills nothing; RTI stays untouched.

## 6. What a clean PASS looks like, and what it establishes

PASS (primary): 1222.MechPlt shows static->moving onset a few seconds after its MoveAlongRoute,
a +4 reflected transient, and a settled endpoint displaced ~east along its route with POS==RPT.
That CONFIRMS the R9 type-mapping diagnosis end to end on the headless one-button product path:
the freeze was the mis-mapped type, and mapping ArmorPlatoon to the real Tank Platoon (USA)
fixes it.

It does NOT, by itself, establish that company-echelon (HU) or single-entity move-along work
through the interface - 114.MechCoy and 1.BdeHQ are the open questions the same run will
incidentally probe. Their result (move or freeze) is recorded per-taskee and, if either
freezes, scoped as the NEXT defect to chase (company-HU controller; entity move-along), not as
a mark against this fix.

## Appendix A - offline evidence for the fix (file:line)

- Dispatch keys off SIDC echelon char (index 11); 'D' -> ArmorPlatoon:
  src/VrfC2SimApp/UnitTranslator.cs Plan (echelon branch, "if (echelon == 'D')").
- ArmorPlatoon factory now branches on TypeMapping: UnitTranslator.cs `ArmorPlatoon(... TypeMapping tm)`.
- Setting + service wiring: VrfSettings.cs `TypeMappingMode` (default "RealTemplates");
  VrfC2SimService.cs (typeMapping computed before the create loop; UnitTranslator.Plan(unit, typeMapping)).
- Aggregate create uses createSubordinates=true (matches Cell C): VrfC2SimService.cs:459-460;
  bridge convenience overload VrfBridge.cpp:221-226; native VrfFacade.cpp:441-449.
- DIS enum is a pure pass-through to the backend (no native remap): VrfFacade.cpp:84-87 toDtType.
- Installed templates verified on disk (C:\MAK\vrforces5.0.2\data\simulationModelSets\EntityLevel\vrfSim\):
  - Tank Platoon (USA).entity: objectType 3:11:1:225:3:2:0:0, ground-disaggregated-movement.sysdef
    + vehiclePlatoonScriptEnable, 4x M1A2 (1:1:1:225:1:1:3:0) with handles PL/PSG/PLWM/PSGWM.
  - Tank Company (USA).entity: objectType 3:11:1:225:5:2:0:0, ground-higherUnit-disaggregated-movement.sysdef,
    HQ Sec + 3x Tank Platoon (USA).
  - Ground_Aggregate.entity: objectType 3:11:1:0:0:0:0:0, SAME ground-disaggregated-movement.sysdef,
    4x anonymous 1:1:1:225:4:14:0:0 with EMPTY function handles (the mechanistic difference vs
    Tank Platoon (USA): empty handles + no platoon script -> empty subordinate offset routes).
  All three are in the loaded chain (C2simEx.sms includes EntityLevel.sms).
- Offline gate: `VrfC2SimApp.exe --translator-selftest` -> SELF-TEST PASSED (22/22).

## Outcome - RUN 1 (2026-07-22 19:16 local): VOID (infrastructure), predictions UNTESTED

vrfSim (back-end) suffered a FATAL CRASH (dump bin64\vrfSim5.0.2-MSVC++15.0_64-249613-
36676.dmp.dmp, written 19:18:32) BEFORE any MoveAlongRoute was issued. Per sec 1, the
decisive falsifier requires 1222.MechPlt static through a running clock AFTER its
MoveAlongRoute - that condition never opened. NOT a falsification; NOT a confirmation.

What PASSED before the crash (all sec 2 validity gates that could run):
- Type-mapping mode = RealTemplates logged; 6 units created at exact init coords with
  ground clamp (1222.MechPlt 34.612956,-116.600487 alt 1040.6 - byte-identical to the
  Cell C creation); company cluster ~38 objects; ~46 reflected total. CREATION WITH THE
  FIXED TYPES IS CLEAN - the crash did NOT occur at create time (back-end lived ~14 s
  past creation, ~2 min past launch).
- PushInit/PushOrder EXIT=0; QUERYINIT 6 units; order (3 tasks) on the bus.
- Last app action 19:18:20: three "CreateRoute (3 pts) ... move deferred to
  route-created" lines. Crash 12 s later; route-created callbacks never arrived; no
  MoveAlongRoute line for any taskee; RPT channel empty (nothing ever moved).
Evidence: docs/experiments/TYPEFIX_CONFIRMING_2026-07-22/ (RUN_INDEX.md is the raw
write-up); runs/20260722T231614Z_run intact. appNos 3585-3589 consumed, 3590 burned,
marker 3591.

CRASH HYPOTHESES (open; deliberately NOT adjudicated - three confounds changed together
vs the last clean runs):
- H-RTI: the PRESERVED teardown-survivor RTI stack (Cell C ran on a FRESH boot; the
  documented teardown-survivor pathology so far was forwarder-wedge/blindness - a
  back-end crash would be a NEW symptom of the same class).
- H-CONTENT: 3 simultaneous route creates + deferred moves into a ~46-object real-
  template scene, sim running (Cell C had 1 platoon, 1 route, and moved; per-element
  operations identical to 07-19's clean run).
- H-ENV: VS 18.8 updater serviced MSVC components minutes before vrfSim launched (dump
  name embeds MSVC++15.0); prior vrfSim dumps exist on disk from 7/14 + 7/15 (nonzero
  base rate under remote-create load, pre-typefix).
DISCRIMINATOR (cheapest, registered for RUN 2): identical re-run on a FRESH-BOOT RTI
(needs a new user ruling). Clean pass -> answers the movement question AND implicates
the preserved stack; second crash on fresh RTI -> content/environment, escalate with
both dumps (MAK support material).

RUN-1 DEVIATIONS (recorded): (1) mechanical parse fix to scripts/RunC2SimScenario.ps1
:1447 (backtick-continuation regression introduced by 8c36abe post-07-19; validated by
DryRun + live passage through the formerly-broken stage; no gate/threshold touched).
(2) On supervisor abort, the runner tree was TaskStop'd, which terminated the joined app
federate 3589 without a clean StopIface resign (back-end already dead; supervisor's
clean-resign order arrived after the fact - sequencing error owned by the supervisor;
possible stale federate 3589 until RTI timeout, no appNo reuse so no collision).
(3) vrfGui 22512 remnant could not be closed gracefully (StopVrf exit 3), NOT force-
killed (needs a user ruling; it HARD-BLOCKS the next LaunchVrf). [RESOLVED: user ruled
force-kill; cleared 20:0x. The RUN 2 vrfGui orphan was later cleared under the standing
failed-own-join carveout.]

## Outcome - RUN 2 (2026-07-22 20:28 local): VOID (infrastructure, DIFFERENT mechanism)

Fresh-boot RTI (user-ruled). The back-end (vrfSimHLA1516e, appNo 3591) FAILED TO
CREATE/JOIN the federation and self-terminated cleanly (NO dump). vrfSim.log (last write
20:29:05): "Could not create Federation Execution CWIX-2024: RTIinternalError TCP
connection has been broken ... No FOM specified. Stopping run of back-end." Zero units;
reflected=0 for the entire 1442 s trace; app logged "No backends found for object
creation" x6; CreateOne diagnostic joined CWIX-2024 with BackendCount=0 (triangulates a
genuinely absent back-end, NOT a blind oracle). This is NOT RUN 1's crash and does NOT
discriminate the RUN 1 hypotheses - it failed far upstream, at RTI join.

ORDERING-RACE EVIDENCE: the 20:29:05 federation-create failure PRECEDES the settled RTI
stack (rtiexec 60672 @20:29:15, rtiForwarder 61696 @20:29:18) by ~10-13 s. The back-end
raced against TRANSIENT fresh-boot rtiexec instances (72748/76380) that were then torn
down. A "Choose RTI Connection" dialog appeared (fresh boot; no persisted auto-connect)
and was answered by DPI-click - the one procedural difference from RUN 1/Cell C, and a
flagged residual-uncertainty contributor (the answerer clicked Connect only; did not
verify the pre-selected connection nor tick "Always try to use this connection").

Validity gates: RealTemplates active (PASSED - fix on, not GoldenParity); 6 units created
NOT MET (0 instantiated, no back-end); oracle gate FAILED (reflected=0); order never
pushed. appNos 3591-3596 consumed, 0 burned, marker 3597. Clean teardown (StopIface EXIT
0, app resigned exit 0, StopVrf EXIT 0). Evidence:
docs/experiments/TYPEFIX_CONFIRMING_RUN2_2026-07-22/ (vrfSim.backend.log.txt is the
discriminating artifact).

## DECISION (user, 2026-07-22 evening) + what governs RUN >= 3
Two VOID runs from two different infra failures => STOP live runs; HARDEN the launch
procedure first; HOLD the MAK case until the RUN-1 reboot+VC++-repair discriminator runs.
- Launch hardening spec: docs/RTI_LAUNCH_HARDENING_DESIGN.md (readiness gate before
  back-end launch; warm resident confirmed-ready rtiexec; drop loopback forwarder /
  suppress the Assistant dialog - all VERIFY-then-implement, needs live RTI to validate).
- Research basis (URL-cited): docs/experiments/RTI_LAUNCH_HARDENING_RESEARCH_2026-07-22.md.
- RUN 1 crash track (separate): reboot + repair x64/x86 VC++ redistributables + replay the
  3x-CreateRoute-then-MoveAlongRoute sequence on a warm RTI. Reproduces => MAK case (dump
  preserved). Does not => mid-session MSVC servicing, procedural fix only.
- ONLY AFTER the launcher is hardened + validated does RUN >= 3 (this prereg's per-taskee
  predictions, unchanged) execute to finally test the type fix through to tasking.

## Outcome - RUN 3 (2026-07-23 ~13:45-14:11 local): PASS - type fix CONFIRMED for the platoon

Run: scripts/RunC2SimScenario.ps1 -Init R9_Mojave_Lean_Initialization_NoComments.xml -Order
R9_Mojave_UnitMove_Order_NoComments.xml -RunSecs 900 (TimeMultiplier 1x). Post-reboot, on the
hardened runner (Stage 2c RTI gate passed warm), RTI freshly auto-started + gate-verified
(rtiexec 8196). appNos 3599-3605 consumed, marker -> 3606. Evidence dir
runs/20260723T174540Z_run/. Runner exited 0 (clean StopIface + StopVrf); RTI preserved.

VALIDITY GATES (sec 2) all MET: Type-mapping mode RealTemplates active; 6 units created; order
delivered (PushOrder OK, "Message processed successfully"); oracle live (watchvrf-trace 459
POS samples/uuid; ListenReports captured 39+ reports incl. a TaskStatus - RPT channel NOT
empty this time). Run is VALID.

STEP 2 (crash discriminator, folded into this run): NO REPRODUCTION. All 3 CreateRoute -> all
3 MoveAlongRoute issued (the exact point RUN 1 crashed 12 s into), full 900 s observation,
clean teardown, NO dump written 2026-07-23. The RUN-1 crash was ENVIRONMENTAL (reboot + fresh
vrfSim load + fresh gate-verified RTI cleared it), NOT a deterministic app fault. No MAK case;
procedural fix stands. Held even WITHOUT the VC++ repair (user did reboot-only).

PER-TASKEE VERDICT (adversarially adjudicated; two-channel rule enforced):

- **1222.MechPlt (ArmorPlatoon -> Tank Platoon (USA) 11.1.225.3.2.0.0): MOVED. PRIMARY
  PREDICTION CONFIRMED; decisive falsifier did NOT fire.** VRF aggregate e62d0a8b:
  - static-while-paused: bit-static at spawn 34.612956,-116.600487 for t=27.6/29.7/31.7;
  - onset: ~t=34-36 (first sample east of spawn at t=35.8, with a small DR wobble at t=33.8);
  - SETTLED endpoint: bit-identical 34.612956,-116.587783 held from t~169 through t~963
    (~390 consecutive samples, ~13 min plateau) - the gold-standard settle;
  - displacement ~1163 m DUE EAST (lat unchanged; lon -116.600487 -> -116.587783) - matches
    Cell C (~1165 m) and the R9 route (~1155 m); meets the MOVED criterion (>=25 m sustained
    >=3 samples + distance-to-final-waypoint decreased) by a wide margin;
  - POS==RPT (agreement, clean pass): RPT (c2sim-bus.log GeodeticCoordinate) reports the
    platoon at 34.612956,-116.594174 mid-traverse then 34.612956,-116.587860 settled (~7 m
    from POS), plus SENT TASK STATUS REPORT (TASKCMPLT) taskee=001aa71b-...6342 / move-along.
    Both channels agree: moved east and completed. No disagreement to quote.

- **114.MechCoy (company, VRF 42d76acf): FROZE** - bit-identical 34.647629,-116.693388 first
  and last (459 samples), no task-complete. The company-HU move defect (predicted UNCERTAIN;
  the type fix leaves it UNCHANGED). NOT a refutation of the platoon fix.

- **1.BdeHQ (entity, VRF 8a7916fc): FROZE** - bit-identical 34.608416,-116.712685 first and
  last, no task-complete. The entity-move defect (predicted; type fix does not touch it).

CONCLUSION: the R9 remote-create freeze for the platoon class WAS the type mapping, and mapping
ArmorPlatoon to the real Tank Platoon (USA) fixes it end to end on the headless product path.
The WHOLE R9 order is not yet fully working - company-HU and entity move-along remain open,
SEPARATE defects (the next targets). One clean, rigorous, two-channel-agreeing data point; a
replicate would add confidence but the settled bit-identical plateau + POS==RPT make this
decisive.
