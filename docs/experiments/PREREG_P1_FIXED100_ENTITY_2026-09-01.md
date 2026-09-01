# PREREG P1: the never-run Fixed100 control at Mojave (registered 2026-09-01, BEFORE running)

Source of the design: docs/RESEARCH_MECHANISMS_2026-09-01.md sec 4b + sec 6 P1. This is
"THE MISSING CONTROL" that docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md
demanded on 2026-07-15 and no session ever ran. User go for live work: 2026-09-01
("Ok to go live. Vrf is all yours"). ASCII only.

## 1. The ONE behavioral variable (and its honest compound nature)

RUN 3 (20260723T174540Z) verbatim, EXCEPT env `Vrf__GroundWaypointAltitudeMode=Fixed100`
(RUN 3 ran the VrfSettings default "Live"; binding via Host.CreateApplicationBuilder env
config, the same mechanism the runner already uses for Vrf__ApplicationNumber).

DISCLOSED: this flag is COMPOUND in the app (VrfC2SimService.cs:422 and :727) - it flips
three coupled behaviors together:
  a. ground-unit CREATE altitude: safe 10000 MSL -> plan altitude (default 1000 MSL);
  b. the deferred parity post-create SetAltitude: skipped -> registered;
  c. ground ROUTE VERTEX altitude: live reflected alt + 50 m -> fixed 100 m MSL.
That is accepted, not hidden: Fixed100 is EXACTLY the configuration under which 1.BdeHQ
made its one clean Mojave march (2026-07-13, R9_region_swap raw: spawn 34.608416,
-116.712685 -> route-end 34.608416,-116.700075, 1.5 m from the ordered final vertex).
P1 tests THE MODE as the variable. If the entity moves, a later P1b (code: split the
knob) can isolate which component matters; if it stays frozen, all three components are
exonerated together and H-ENT-2 leads.

## 2. Instrumentation deltas (declared; predicted behavior-neutral)

- vrfSim.mtl (backup vrfSim.mtl.bak-20260901): notifyLevel 2->3, objectConsoleNotifyLevel
  1->3, enableLogFileTimestamps 0->1. Purpose: the movement controllers' Info/Verbose
  object-console messages reach vrfSim.log + the CON channel (research doc sec 1b/M8:
  level 1 dropped everything below Warn - "zero CON lines" was the configured outcome).
  Risk: log volume slowing ticks; if the run shows gross clock anomalies vs RUN 3,
  record it and re-run with levels reverted before interpreting.
- Runner: teardown now copies C:\MAK\vrforces5.0.2\bin64\vrfSim.log + vrfGui.log into the
  run directory (P0.a; they were overwritten by the next launch and never captured).
- NO app rebuild; the binaries are the RUN 3 binaries.

## 3. Command, appNos, environment

- Command (cwd repo root):
  `$env:Vrf__GroundWaypointAltitudeMode='Fixed100'; pwsh -File scripts\RunC2SimScenario.ps1
   -Init data\R9_Mojave_Lean_Initialization_NoComments.xml
   -Order data\R9_Mojave_UnitMove_Order_NoComments.xml -RunSecs 900`
- appNos: the runner allocates 7 from the Appendix B marker (currently 3606) = 3606-3612,
  advances the marker to 3613 and appends the CLAIMED block itself (stage 2, before any
  join). createOneDiag's number burns unused on a healthy run - expected.
- Environment at registration: VR-Forces DOWN, NO rti processes (fresh state; stage 2c
  RtiProbe is expected to AUTO-START and warm the RTI - the DOWN-but-startable path was
  validated 2026-07-23, appNo 3598 exit 0). C2SIM docker UP (REST 8080 answers - 405 on
  GET is the live servlet; STOMP 61613 listening). License valid to 2026-09-15.
- Known risk, pre-accepted: fresh-boot RTI join race ([[rti-fresh-boot-join-race]]) - the
  stage 2c gate exists for exactly this; if the back-end still fails to join, that is an
  INFRA VOID (not a verdict), and after TWO consecutive infra failures the standing rule
  is stop-and-research, not retry.

## 4. Predictions + falsifiers (written before the run)

PRIMARY - 1.BdeHQ (single M1A2 entity, task T_R5_TK1, ~2x577 m legs east):
  PREDICTION (H-ENT-1, confidence MEDIUM-HIGH): MOVES - static-while-paused ->
  onset within ~a minute of MoveAlongRoute -> settled endpoint near lon -116.700058
  (route end), POS==RPT, move-along TASKCMPLT. At 1x and M1A2 ordered speed this fits
  well inside the 900 s window (07-13 completed at 20x in <400 s sim).
  DECISIVE FALSIFIER: with all sec-5 validity gates met, 1.BdeHQ bit-static on BOTH
  channels through a running clock for >=300 s after its MoveAlongRoute is issued.
  Then H-ENT-1 (the Live/Fixed100 mode as the regression variable) is DEAD, and H-ENT-2
  (some other 07-13 -> 07-15 delta: launcher, lean init, 1x, or an environment effect)
  becomes the lead - with the useful residue that the mode flag is exonerated.

SECONDARY - 1222.MechPlt (Tank Platoon (USA), real template, task T_R5_PL1):
  PREDICTION: UNCERTAIN, either outcome informative. Under Fixed100 its route vertices sit
  ~940 m BELOW terrain; the 07-14 Thread-A mechanism predicts the LF offset-route builder
  may reject them (`moveAlong() - empty route`, zero member Offset Routes) - but that
  prediction predates the type fix (2026-07-13's freezing platoon was the Ground_Aggregate
  fallback). A freeze here does NOT touch the RUN 3 type-fix verdict (proven under Live);
  a march here plus an entity march would make Fixed100 a candidate product default.
  READ: count "Offset Route" creations + grep the empty-route line in the captured
  vrfSim.log.

SECONDARY - 114.MechCoy (Tank Company (USA), task T_R5_CO1):
  PREDICTION: FROZEN, possibly after a small wrong-way displacement (~200-430 m, the
  07-13 Fixed100 signature, consistent with the Column formation offsets in
  Formation-Column-Armor-Co(US).frm). EXPECT the stock content-defect line
  (`AR HQ Sec 1: ... invalid formation name "column-left"`) at creation regardless of
  mode. With notify level 3, NEW Info/Verbose lines from the higher-unit controller are
  the P2 intelligence this run collects for free. Nothing about the company falsifies
  anything registered here.

NOT a target: 1141/1142/1143.MechPlt (created, never tasked) - expect static.

## 5. Run-validity gates (must pass or the run is INVALID, not a verdict)

Mirrors PREREG_TYPEFIX_CONFIRMING_RUN.md sec 2, plus one new gate:
- Creation: 6 units dispatched; oracle gate (stage 7) passed on real coordinates;
  app log shows the RealTemplates type-mapping line (TypeMappingMode is untouched).
- MODE CONFIRMATION (new): the app log contains ZERO "Create-altitude mode=Live" lines
  (RUN 3 had 6). Their absence across 6 ground units is the positive evidence that
  Fixed100 bound through the env var. If any appears, the run is VOID (the variable
  never flipped) - do not score, fix the binding, re-run.
- Delivery: PushOrder EXIT=0; three CreateRoute + MoveAlongRoute lines in the app log.
- Oracle live: pre-check discovered objects; trace being written; ListenReports capturing
  (if RPT is empty, degrade to POS-only and SAY SO).
- Also record (not gate): birth altitudes in the trace pre-order - under Fixed100 units
  are created at plan altitude (1000 MSL) and the create ground-clamp should still
  surface them (07-13 showed 1040-1131 m); a unit sitting at ~1000 m MSL below terrain
  through the pre-order window is itself a finding to report.

## 6. Reading rules

Standard movement gate, unchanged: static-while-paused -> moving-once-tasked transition +
settled endpoints + POS/RPT agreement (quote BOTH channels on any disagreement); raw POS
distances are DR-poisoned - never gate on a distance; MOVED = >=25 m net sustained >=3
consecutive samples AND distance-to-final-waypoint decreased. Trace-t0 offset ~5.89 s
before aligning onset to task issue.

## 7. Budget + teardown

One run, ~35 min wall (900 s observation + launch/teardown). Teardown is the runner's
standard finally (StopIface -> app self-exit -> observers finish -> StopVrf). RTI is left
RESIDENT afterward, per procedure. Never kill rtiAssistant/rtiexec/rtiForwarder.

## Outcome

(to be filled AFTER the run, against the predictions above)

### Outcome - RUN 1 (20260901T183422Z): INFRA VOID (sec 3 pre-accepted risk), predictions UNTESTED

Stage 2c RtiProbe (appNo 3611) hung >625 s and was left running (never killed); the runner
failed cleanly through teardown (exit 3). CAUSE, verified live: fresh-boot state had NO
rtiAssistant; the first federate contact raised the once-per-boot "Choose RTI Connection"
dialog (rtiAssistant pid 41336 showed exactly that MainWindowTitle) and RtiProbe blocked
mid-Start() behind it ("Connected to RTI Assistant. Loading Config File..." then nothing).
This is the KNOWN boot-dialog precondition (RUNBOOK 0.5.3; the runner's own pre-flight
WARNed it), distinct from the fresh-boot join race. NOT a Fixed100 result - the app was
never launched; the variable was never exercised.

RECOVERY (2026-09-01, this session): scripts/AnswerRtiDialog.ps1 - first scripted
implementation of the 2026-07-22 DPI-click recipe (dialog found via the assistant
process's MainWindowHandle; Connect clicked at window-relative 0.668,0.949 of the
573x583 physical rect; exit 0 only when the window is confirmed GONE). One gotcha cost
one attempt: a FindWindowW P/Invoke without CharSet=Unicode marshals ANSI and never
matches - fixed. After the click: dialog gone, rtiexec + rtiForwarder spawned, RtiProbe
unblocked and exited. RTI stack now RESIDENT but NOT yet gate-verified - the re-run's
own Stage 2c gate is the serviceability instrument.

Ledger: appNos 3606-3612 consumed/burned by RUN 1 (marker correctly at 3613). The
captured bin64-vrfSim.log in the RUN-1 dir is the JULY 23 file (no launch happened this
run) - correct capture behavior, do not misread it as today's sim log.

RUN 2 = the same registered protocol, unchanged; this remains infra failure #1 of the
two allowed before stop-and-research.

### Outcome - RUN 2 (20260901T191004Z): VALID; H-ENT-1 FALSIFIED; the real mechanism OBSERVED

VALIDITY: ALL sec-5 gates MET. RealTemplates line present; 6 units created; oracle live
(460 POS samples/unit, 2.0 MB trace); PushOrder ok + 3 CreateRoute/MoveAlongRoute; RPT
channel LIVE (190 PositionReports + 4 TaskStatus); MODE GATE: ZERO "Create-altitude
mode=Live" lines -> Fixed100 bound. Clean run + clean teardown (runner exit 0; RTI
preserved). appNos 3613-3619. Clock advanced normally - the notify-level-3
instrumentation was behavior-neutral as predicted (sim log ~40 KB/s, trace unaffected).

PER-TAAKEE (two channels; both quoted; they AGREE on all three):
- 1.BdeHQ (84928680): **BIT-STATIC 460/460 samples** at 34.608416,-116.712685 t=25.6
  through 962.1; RPT identical through t=951.2. Task issued ~t=28; >900 s static past
  task with all gates met. **THE DECISIVE FALSIFIER FIRED: H-ENT-1 IS DEAD** - the
  waypoint/create mode is NOT the entity-freeze variable (frozen under Fixed100 too).
- 1222.MechPlt (cc9ccf59): **MOVED under Fixed100** - onset t=31.7 (~4 s after task),
  settled BIT-IDENTICAL 34.612956,-116.587783 from t=162.2 to 962.1 (~800 s plateau,
  the same endpoint as RUN 3), POS==RPT, TSK move-along complete t=158.4, TASKCMPLT
  sent. So ~940-m-below-terrain vertices do NOT break the real-template platoon - the
  Thread-A underground-vertex mechanism is refuted for this path as well.
- 114.MechCoy (9956438c): FROZEN bit-static 460/460, POS==RPT, no completion.

THE MECHANISM, OBSERVED DIRECTLY (the instrumentation payoff): with
objectConsoleNotifyLevel 3, the frozen units' controllers said what they are doing -
`1.BdeHQ: Waiting for nav data to load.` x12,100 and the company-tree vehicles x~10,000
each (M1A2 5..18, M3, HMMWV), in a loop, once per tick, with NO load-completion line
ever. The moving platoon's members: 0-2 such lines. These messages are Info-level and
were INVISIBLE in every prior run (console gated to warnings).

**H-ENT-3 (new leading hypothesis, mechanism-named):** the nav data being waited on is
`SharedData\16\latest\TerrainData\navData\MAK Earth Space (online)\NavArea-ground-platform 1`
- generated 2026-07-14 11:59-12:44 BY THIS PROJECT's nav-data investigation (generator
log + file dates; 120,002 tiles, 442 MB; extent 18.09x18.09 km centered 34.6411,
-116.6419, which CONTAINS all three taskees) and left on disk when "missing nav data"
was falsified as a cause. Timeline: entity moved 2026-07-13 (pre-artifact); frozen in
EVERY run from 2026-07-15 on (post-artifact). Live mode becoming the default the same
day was a coincidental confound - RUN 2 just unconfounded it. CAVEAT, stated: the
moving platoon sits INSIDE the extent too, so the stall is finer-grained than the
bounding box (per-tile or per-request); the waiting-line counts are the discriminator,
not the geometry. Also new: the company-tree VEHICLES received movement demands (their
waits began at task time) - the HU controller acted further than any prior run showed;
the company freeze is nav-data-implicated too, with the formation defect still standing
as a second candidate.

### PREREG P1c (registered before running): remove the 2026-07-14 NavArea artifact

ONE variable vs RUN 2: the presence of the generated NavArea. Action: MOVE (not delete)
the four top-level items - "NavArea-ground-platform 1" (dir), ".navGenConfig",
".navGenConfig.generated", ".navRuntimeConfig" - from navData\MAK Earth Space (online)\
into navData\_disabled_20260901\ (same volume; restorable by moving back). Everything
else identical to RUN 2 (Fixed100 env var kept; notify level 3 kept - required to see
the waiting loop vanish). appNos: runner takes 3620-3626 from the marker.

PREDICTIONS + FALSIFIERS:
- 1.BdeHQ: MOVES (onset within ~a minute of task; settled near lon -116.700058;
  POS==RPT; TASKCMPLT) and emits ZERO "Waiting for nav data" lines. FALSIFIER of
  H-ENT-3: entity again bit-static >=300 s past task with gates met AND no waiting
  loop -> the artifact was not the blocker; H-ENT-2 residue leads.
  (Entity static WITH the waiting loop still present would instead mean the artifact
  is still being found - a procedure failure, VOID, check the move.)
- 1222.MechPlt: MOVES (control; unchanged).
- 114.MechCoy: DISCRIMINATOR, either way informative. Moves -> nav-data was the
  company's blocker too (P2's formation fix likely unnecessary; the stock content
  defect stays a MAK report). Stays frozen WITHOUT waiting lines -> the HU/formation
  defect leads and P2 proceeds as designed.
Validity gates: sec 5 unchanged (incl. the Fixed100 mode gate).

### Outcome - RUN 3 / P1c

(to be filled AFTER the run)

### Outcome - RUN 3 / P1c (20260901T194029Z): PASS - H-ENT-3 CONFIRMED; the ENTITY-FREEZE CLASS IS CLOSED

VALIDITY: all gates MET (mode gate 0 Live-lines; 6 units; oracle live 460 samples/unit;
delivery ok; RPT live). Clean run + teardown (exit 0); appNos 3620-3626, marker -> 3627.
The captured bin64-vrfSim.log contains ZERO "Loading NavData" and ZERO "Waiting for nav
data" lines - the artifact's absence took effect at the engine level before tasking.

PER-TASKEE (both channels agree on all three):
- **1.BdeHQ (e3fe4e9b): MOVED AND COMPLETED.** Onset shortly after task; settled
  BIT-IDENTICAL at 34.608416,-116.699993 (route end -116.700059, ~6 m) from t<=186.8
  through 960.8; TSK move-along complete t=145.3; TASKCMPLT sent; RPT last fix
  identical. Observed transient, recorded not interpreted: a westward excursion
  (~-116.7162 at t=105) mid-move before eastward arrival. PREDICTION CONFIRMED,
  waiting loop ABSENT. **H-ENT-3 CONFIRMED**: the 2026-07-14 generated NavArea was the
  entity-freeze cause. Chain: RUN 2 showed the unit naming the wait; removal -> it
  drives. The seven-week entity-freeze class (07-15 -> 08-31) closes as a self-inflicted
  artifact, silent one notify level below the console default.
- 1222.MechPlt (9352848e): MOVED + completed (control; endpoint identical to RUN 2/RUN 3-July).
- **114.MechCoy (74e61b30): NO LONGER WAITS - now EXECUTES A WRONG MANEUVER.** Moved
  ~422 m DUE SOUTH (34.647629 -> 34.643835) between task and t=90.8, then settled
  bit-identical ~870 s; route goes NORTH; no completion; ZERO working routes for the
  company tree (the run's offset routes are all platoon M1A2 1-4); the HQ-section
  "invalid formation name column-left" warning fired again. This is the 2026-07-13
  pre-artifact signature reproduced cleanly - and ~422 m matches the company Column
  .frm slot offsets (entries at -430/-230 m). The company defect is now isolated,
  live, and non-silent: the formation/leading-edge family (H-CO-1/H-CO-3). P2 decides.

SCOREBOARD after P1c: the R9 order executes 2 of 3 taskees end-to-end headless
(platoon + entity, both TASKCMPLT + telemetry-verified). Remaining: the company.

### PREREG P2 (registered before running): give the HQ section the formation names the
### company formations demand

ONE variable vs P1c: content edit (backed up) to
data\simulationModelSets\EntityLevel\vrfSim\Tank Headquarters Section (USA).entity -
ADD alias formation entries so every name the four Formation-*-Armor-Co(US).frm files
assign to the HQ slot or that recursion can hand it resolves: column-left ->
Ar_Co_HQ_Column.frm, column-right -> Ar_Co_HQ_Column.frm, line-left -> Ar_Co_HQ_Line.frm,
wedge-left -> Ar_Co_HQ_Wedge.frm, wedge-right -> Ar_Co_HQ_Wedge.frm. Existing entries
untouched. Backup: <file>.bak-20260901. Everything else identical to P1c (NavArea still
disabled; Fixed100 env; notify 3). appNos: runner takes 3627-3633.

PREDICTIONS + FALSIFIERS:
- The "invalid formation name" line does NOT appear (the direct check that the edit took).
- H-CO-1 (formation defect blocks the HU controller): 114.MechCoy generates working
  routes for its sub-units and marches NORTH toward 34.657629, completing or clearly
  progressing. FALSIFIER: warning absent AND still zero company-tree working routes AND
  the same ~420 m-south-then-freeze -> the HQ-section formation was NOT the blocker;
  H-CO-3 (route start vs formation leading edge) becomes the lead, probed next by
  starting the route ahead of the formation instead of at the unit centre.
- Platoon + entity: MOVE (controls; unchanged).
Validity gates unchanged.

### Outcome - RUN 4 / P2 (20260901T200935Z): VALID; the formation blocker is REMOVED and the
### NEXT blocker in the stack is EXPOSED - empty working routes

VALIDITY: all gates MET (mode gate 0; clean run/teardown; appNos 3627-3633, marker 3634).
- THE EDIT TOOK: ZERO "invalid formation name" lines (P2's direct check). ZERO nav-data
  waits (P1c state persists).
- 114.MechCoy (97ca5ff0): the HU controller advanced FURTHER THAN EVER OBSERVED - it
  created FOUR working routes (114.MechCoy_R0..R3, one per sub-unit incl. the HQ
  section), maneuvered NORTH ~370 m toward the route (new), then logged
  `moveAlong() - empty route -- not sending move along to subordinate` x3 (the exact
  2026-07-13 signature, now from the HU controller), flooded
  `buildEntityRouteFollowingMap() : Can't find entity route` x2,577, swung to the same
  ~420-m-south point (34.643833) and froze bit-identical for the window. No completion.
- 1222.MechPlt + 1.BdeHQ: MOVED + completed again (controls; 2 TASKCMPLT).

ADJUDICATION vs the P2 prereg: the registered falsifier ("warning absent AND zero
company-tree working routes AND same south-freeze") does NOT fire in its letter -
working routes WERE created - and H-CO-1 is adjudicated PARTIALLY CONFIRMED: the
formation defect was a real blocker in a STACK; removing it moved the failure point
from formation resolution to working-route CONTENT generation. The company's residual
defect is now: the HU controller's route builder emits EMPTY parallel routes where the
platoon's LF builder (same terrain, same 100-m-MSL original vertices, smaller offsets)
emits full ones. This is the Thread-A clamp family (MOJAVE_ROOTCAUSE 2026-07-14
findings log), never tested at company offsets.

### PREREG P2b (registered before running): Live vertex altitudes for the company's
### route builder

ONE variable vs P2: REMOVE the Vrf__GroundWaypointAltitudeMode env override -> the app
default "Live" (route vertices at live reflected altitude + 50 m instead of 100 m MSL;
the compound create-altitude side reverts to safe-10000+clamp, the RUN-3-July config
under which the platoon marched). Template edit stays; NavArea stays disabled; notify 3
stays. appNos: runner takes 3634-3640.
PREDICTIONS + FALSIFIERS:
- H-CO-4 (empty working routes are altitude/clamp-sensitive at company offsets):
  114.MechCoy_R* routes get content, no "empty route" lines, the company marches NORTH
  and progresses/completes. FALSIFIER: still empty + same freeze under Live -> altitude
  is exonerated for the company builder too; next candidates are the 3-point route
  geometry vs adjustOffsetRouteStart at ~430 m offsets, and the route-start-at-centre
  (leading-edge) issue - probed by starting the route AHEAD of the formation.
- MODE GATE inverts: the app log MUST show 6 "Create-altitude mode=Live" lines.
- Platoon: expect MOVES (proven under Live in RUN 3-July, post-type-fix). Entity: NOW
  UNDER LIVE for the first time since the artifact's removal - expect MOVES (H-ENT-3
  says the artifact, not the mode, was its blocker; this doubles as the entity's
  Live-mode regression cross-check).

### Outcome - RUN 5 / P2b

(to be filled AFTER the run)

### Outcome - RUN 5 / P2b (20260901T203702Z): PASS - H-CO-4 CONFIRMED; **THE WHOLE R9
### ORDER EXECUTES 3/3**

VALIDITY: gates MET; the inverted mode gate PASSED (6 "Create-altitude mode=Live" lines).
appNos 3634-3640, marker 3641. Teardown: StopVrf exit 3 left vrfGui up (the known
intermittent GUI-quit failure; backend was already down); a second StopVrf pass exited 0,
everything down gracefully, RTI preserved, nothing killed. Stage 2b watcher: clean
no-dialog no-op (log in the run dir).

PER-TASKEE (both channels agree on all three; all three TASKCMPLT):
- **114.MechCoy (53d5f248): MOVED NORTH AND COMPLETED.** 87 distinct positions; settled
  BIT-IDENTICAL at 34.653928,-116.693388 from t=213.2 to 960.9 (~750 s plateau); TSK
  move-along complete t=206.1; RPT last fix identical. The settle point is ~415 m short
  of the final vertex (34.657629) - the DOCUMENTED unit completion geometry ("finished
  when the leading edge reaches the last point"; the company Column formation is ~430 m
  deep). ZERO "empty route" lines, ZERO "Can't find entity route" lines in the captured
  sim log (P2's run had 3 and 2,577).
- 1.BdeHQ: moved + completed (as P1c/P2, now under Live - the entity works under BOTH
  altitude modes with the NavArea artifact gone; the mode was never its variable).
- 1222.MechPlt: moved + completed (control, endpoint unchanged).

**H-CO-4 CONFIRMED**: the company's empty working routes were vertex-altitude-sensitive -
above-terrain-frame vertices (Live) fill them; the 100-m-MSL underground vertices
(Fixed100, the Users-Guide-warned authoring ERROR) starve them. The LF platoon builder
tolerated the error; the HU builder did not. No paging probe needed.

## FINAL STATE OF THE DAY (5 runs, appNos 3606-3640)

THE R9 MOJAVE ORDER NOW EXECUTES END-TO-END, HEADLESS, ALL THREE TASKEES, WITH
TELEMETRY-VERIFIED ARRIVALS AND COMPLETIONS. The blocker stack, fully peeled and each
layer evidence-attached:
  1. TYPE-MAPPING (fixed 2026-07-22, Cell C / RUN 3-July) - platoon class.
  2. THE 2026-07-14 NavArea ARTIFACT (found RUN 2, confirmed P1c) - entity + company
     wait-forever; now in navData/_disabled_20260901/ (restorable; keep disabled).
  3. HQ-SECTION FORMATION NAMES (stock MAK content defect; fixed P2 via template
     aliases, backup .bak-20260901) - unblocked the HU controller.
  4. ROUTE-VERTEX ALTITUDE FRAME (fixed P2b by using Live mode = approx above-terrain;
     the DOCUMENTED frame) - filled the HU working routes.
Configuration that works: TypeMappingMode=RealTemplates + GroundWaypointAltitudeMode=
Live (default) + NavArea artifact disabled + HQ-section formation aliases. The
Fixed100 escape hatch is now documented as the authoring ERROR it always was.
NEXT (documented-frame hardening): author route vertices via
DtIfRequestTerrainProfileInformation (vrfmsgs/ifRequestTerrainProfileInformation.h -
request terrain heights for the route points from the back-end) instead of the live+50
approximation; then the COA-STP1 scale re-run on this clean state; then the MAK
reports (formation content defect WITH verified fix; 5.x docs chapter regressions).

### PREREG P2c (registered before running): are the formation aliases actually necessary?

USER CHALLENGE (2026-09-01, verbatim intent): a mature, worldwide product with a bug in
a universally-used feature is beyond belief; the "defect" more likely marks US as off
the beaten path. The record supports taking this seriously: (a) the vendor docs say an
invalid formation name falls back to a WORKING FORMATION "ensur[ing] continuous
operation despite configuration issues" (disaggregatedFormationController.h /
getCurrentFormation) - i.e. non-fatal BY DESIGN; (b) the P2->P2b sequence CONFOUNDS the
aliases with the altitude frame - the company only worked after BOTH changed. The
missing cell: stock template + artifact disabled + Live altitudes.

ONE variable vs P2b: RESTORE the stock "Tank Headquarters Section (USA).entity" (copy
.bak-20260901 back; the aliased version preserved as .aliased-20260901). Everything
else identical to P2b (Live default - no env override; NavArea disabled; notify 3).
appNos: runner takes 3641-3647.

PREDICTIONS + FALSIFIERS (honestly uncertain; the user's beaten-path prior says WORKS):
- BEATEN-PATH prior: the invalid-formation warning REAPPEARS but is COSMETIC - the
  working-formation fallback supplies offsets, the HU controller fills its routes under
  Live altitudes, 114.MechCoy marches north and completes as in P2b. THEN: H-CO-1
  demotes to a cosmetic warning; there is NO MAK defect report (at most a docs note);
  the aliases are reverted permanently; the real fixes of record reduce to type mapping
  + our own artifact + the documented altitude frame.
- ALIAS-NECESSARY alternative: the company reverts to a P1c/P2-style failure (no/empty
  working routes or wrong-way freeze). THEN the aliases are load-bearing; the finding
  goes to MAK framed as a QUESTION about intended template/formation interplay on the
  entity-level company path, not as "your bug".
- AGAINST-CONFOUND evidence noted: P1c (stock template + Fixed100) created ZERO company
  working routes while P2 (aliases + Fixed100) created R0-R3 - the aliases did change
  behavior UNDER FIXED100. P2c decides whether they matter under the documented frame.
- Controls: platoon + entity expected to MOVE + complete (unchanged).
Validity gates as P2b (6 "mode=Live" lines expected).

### Outcome - RUN 6 / P2c

(to be filled AFTER the run)
