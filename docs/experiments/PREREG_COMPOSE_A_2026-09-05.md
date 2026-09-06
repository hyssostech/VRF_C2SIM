# PREREG / PLAN - Option A: hierarchy-aware create (compose a company from its declared platoons)

Date 2026-09-05. Tier HEAVY (public-ish create/tasking behaviour; native VrfFacade/VrfBridge
rebuild; work no offline test fully covers -> a live validation run is the gate). User chose
Option A (docs/experiments/PREREG_F_DIVERGE ROOT CAUSE). This is the PLAN + validation prereg;
write it BEFORE coding. Research basis: wf_e5cb1379 (feasibility FEASIBLE_WITH_CAVEATS; synthesis
self-verified by direct header reads; the refuter agent errored, so the ASSUMED items below are
gated on the live validation run, not on a passed adversarial agent).

## 0. Verified basis (direct reads; cite before building)
- moveAlongRoute on a parent RECURSES to subunits: "Subordinate units are issued move-along.
  Subordinate platforms are issued maneuver-in-formation" (disaggregatedManeuverAlongController
  .h:39-57,:250-271); UG52 25.2-25.3 pp.516-518 (leader path + per-subordinate offsets).
- VR-Forces org model IS the military ORBAT (UG52 13.3 pp.364-365); MSDL import is the documented
  analog of a C2SIM OOB import.
- API (base DtVrfRemoteController, which our facade already uses): createAggregate(...,bool
  createSubordinates) - false = empty shell, no template (cgf.h:617-621); addToOrganization(child,
  superior) - "detached from any superior it currently belongs to first", child may be an
  aggregate (vrfRemoteController.h:1334-1339; cgf.h addSubordinate "platform or aggregate").
- CONSTRAINT: UG52 18.1 p.437 - cannot create a unit already parented; create flat then re-parent
  (so empty-shell + addToOrganization is REQUIRED). UG52 18.1.1 p.438 - subordinate ORDER fixes
  leader/echelon, unchangeable post-create -> attach order must map the intended C2SIM leader.
  disaggregatedMoveAlongController.h:451-454 - the controller REQUIRES a command radio on the
  aggregate (our aggregate platforms carry one).
- Our code today: createSubordinates hardcoded TRUE at VrfC2SimService.cs:799; facade/bridge
  expose CreateAggregate(...,createSubordinates) + GetAggregateMembers (recursive) +
  ReorganizeAggregate, but NOT addToOrganization; init hierarchy 1141/1142/1143.Superior ==
  114.MechCoy uuid 139aa71b (init xml).
- NOT FOUND / DO NOT INVENT: aggregateAs(vector<DtUUID>) absent from base controller; no
  "DeStackCreates"-style compose setting; no same-force/same-domain constraint for normal units.

## 1. ASSUMED - must be confirmed by the live validation run, not asserted
- A bottom-up composed aggregate tasks/moves IDENTICALLY to a template one (ifAggregateAs.h says
  "pseudo-aggregate").
- addToOrganization accepts an AGGREGATE (platoon) as the child.
- The company shell (createSubordinates=false) acquires a valid formation/leader once children are
  attached (5.2 VRF-8968 auto-waits for validity). The SAMPLE calls NO SetFormation/Reorganize;
  only if the move will not start do we add a post-attach SetFormation+Reorganize (that - not
  top-only pre-attach on phantoms - is where the retired B1 would belong).

## 2. Build order - FOLLOW THE VENDOR SAMPLE RECIPE, no invention
CORRECTION 2026-09-06 (user): a prior draft split this into "INCREMENT 1 = fan out to the declared
platoons via the existing SubordinateFanOut" then "INCREMENT 2 = real composition". That fan-out
is NOT the vendor recipe - it was invented to dodge the native rebuild. DROPPED. The MAK sample
(commandLineRemoteController.cxx:717-775 build, :1520-1554 attach) IS the recipe for exactly this
case, and we follow it verbatim: create the parent EMPTY, create the members, addToOrganization in
the object-created callback, then task the parent (VR-Forces recurses). One implementation, not two.
  S1 Derive parent->children from SuperiorUuid over PLANNED SURVIVORS (no parser change): classify
     PARENT / CHILD / LEAF. Gate all new behaviour behind a new VrfSettings flag Vrf:ComposeHierarchy
     (default FALSE -> flag-off run byte-identical to today). (VrfC2SimService.cs ~540-704;
     InitModels.SuperiorUuid already parsed.)
  S2 Add bool CreateSubordinates to CreationPlan; set FALSE for PARENT plans (empty shell, no
     ~48-vehicle template - the root-cause fix); children/leaves keep TRUE (a leaf platoon's
     template subs ARE its real vehicles). EnqueueCreates passes p.CreateSubordinates not literal
     true (:799).
  S3 Add AddToOrganization(childVrfUuid, parentVrfUuid) pass-through to VrfFacade.h/.cpp (mirror
     SetLocation/SetTarget) + VrfBridge marshaling -> controller->addToOrganization
     (vrfRemoteController.h:1334-1339). /t:Rebuild; back up + redeploy all 7 DLL copies (memory:
     feedback-native-fixes-authorized). This is the ONE new native method, straight from the sample.
  S4 Async compose in OnVrfObjectCreated - the sample's own pattern ("buffer the members, attach in
     the callback once the aggregate has been created", commandLineRemoteController.cxx:736-742,
     1520-1554): PendingComposition{parent, expectedChildren, arrivedUuids, deadline} + a
     _compositionReady TCS, with a TickLoop timeout sweep (mirror ExpireTerrainRequests /
     TerrainProfileTimeoutSeconds; NOT the timeout-less _pendingRouteTasks). When both the parent
     shell and a child exist, AddToOrganization(childUuid, parentUuid). Attach children in the
     init's declared order (UG52 18.1.1: order fixes the leader/echelon). The sample calls NO
     SetFormation/Reorganize - so we DON'T either, unless the live run shows the move will not start
     (5.2 VRF-8968 auto-waits for formation validity); only then add a post-attach SetFormation+
     Reorganize and re-run (that, not top-only-pre-attach, is where the retired B1 would belong).
  S5 Task the COMPANY directly - our existing MoveAlongRoute(companyUuid) path, unchanged; VR-Forces
     recurses to the platoons (disaggregatedManeuverAlongController.h:39-57). Create-before-task
     gate: the parent create is deferred until children exist, so in RunTaskAsync await
     _compositionReady[company] (bounded, same TCS idiom as WaitForStartAsync :1024) before
     ExecuteTaskOnTick so the deferral does not open a drop window (:1070). NO fan-out.
  -> Validation run V (below).

## 3. Validation run(s) - pass conditions written BEFORE the run; a miss is a STOP
Env: 5.2 rtiexec posture, R9 init, EntityLevel; do NOT pass --logFileName (harvest C:\MAK\logs);
instrument-reproduces check first (per lessons-false-greens); MAK licence expires 2026-09-15 - run
before then. Score by name via WatchVrf trace + app log.
V (single run, flag ON): P1 PHANTOM GONE - no ~48-vehicle "Tank Company (USA)" template set; the
company subtree = the 3 declared platoons + their own vehicles only (falsifier: any phantom-named
mover). P2 MEMBERSHIP - GetAggregateMembers(114.MechCoy) == the 3 declared platoons 1141/1142/1143
(or their flattened vehicles), NOT template members. P3 DECLARED PLATOONS MOVE (core) - a DIRECT
company MoveAlongRoute (no fan-out) moves 1141/1142/1143 each > 25 m toward the route (baseline
today = 0 m); this is also the test of the key ASSUMED item (a composed aggregate is taskable).
P4 NO IDLE-ORPHAN. P5 company TASKCMPLT emitted for 114.MechCoy. P6 LEAF REGRESSION - 1222.MechPlt
+ 1.BdeHQ unchanged; flag-OFF run byte-identical to today. Score movement by direction-toward-
route-end (fix the movement_check MAGNITUDE gap), not raw disp.

## 4. Risks (carry)
Composed-tasks-like-template ASSUMED (P3 is the test; ifAggregateAs.h says "pseudo-aggregate");
addToOrganization-accepts-an-aggregate-child ASSUMED; the move may not start without a post-attach
formation (S4 note); async drop window (S5 _compositionReady barrier); leak if modelled on the
timeout-less _pendingRouteTasks (use a deadline sweep); re-parent transient double-count when
scoring PHANTOM-GONE; native-rebuild blast radius (redeploy all 7, partial redeploy is a
false-green); licence deadline 2026-09-15.

## 5. Result - 2026-09-06, validation V (run 20260906T104042Z, appNos 3975-3981) - PASS
Vrf__ComposeHierarchy=true, AggregateFormation unset (B1 off - sample calls no formation API).
Commit 06b0cd8 (native Release-5.2 rebuilt EXIT=0; app rebuilt; app-deployed VrfBridge.dll == the
fresh native). Env clean: RTI serviceable, oracle gate PASSED, clean teardown.
- Compose fired (app log): "114.MechCoy -> EMPTY shell; will attach 3 declared child unit(s)
  [1143,1141,1142] ... composed - 3/3 declared children attached."
- P1 PHANTOM GONE: 45 trace objects (was 53 with the phantom); the company cluster is 16
  real-platoon objects, no ~48-vehicle generic "Tank Company (USA)" template.
- P2 MEMBERSHIP: 3/3 declared children attached (log); the company move recursed to them.
- P3 DECLARED PLATOONS MOVE (core) **PASS**: MechCoy cluster = 16 movers / 0 still, all NORTH
  (~357-3 deg, the route direction) 469-926 m. Baseline every phantom run: declared platoons 0 m.
  Strict inversion. Runaways GONE (max ~2.6 m/s, realistic; the ~30-45 m/s runaways were the
  phantom).
- P4 NO IDLE-ORPHAN: 0 still in the cluster.
- P5 COMPANY TASKCMPLT: 3/3 - 114.MechCoy (139aa71b) + 1.BdeHQ + 1222.MechPlt. DROP/ABANDON=0.
- P6 LEAF REGRESSION: 1222.MechPlt + 1.BdeHQ moved + completed (6 non-cluster movers). Flag-off
  byte-identical is by construction (default off); a flag-off confirm run is owed.
CONFIRMED (were ASSUMED, sec 1): a composed aggregate IS taskable; addToOrganization accepts an
aggregate child (3/3 platoons attached + moved); a company move-along recurses to its declared
members. NO post-attach SetFormation/Reorganize was needed (the move started on its own -
VRF-8968) - the retired B1 stays retired.
Adversarial review: H-vacuous (empty shell reports done, nothing moved) killed by 16 members
moving 469-926 m; H-phantom-still-present killed by 0 still + object-count drop + the shell/3-3
log; H-not-really-recursion killed by tasking ONLY the company (139aa71b, no fan-out) yet 16
coherent northward movers. NOT independently verified: per-platoon name->uuid (Debug create-log
off with B1 off) - cluster evidence conclusive; enable Debug for a by-name confirm if wanted.
CAVEAT: the phantom failure was NON-DETERMINISTIC, so a single clean run is not a determinism
proof - 2 confirming repeats running (V2a/V2b). VERDICT (pending the 2 repeats): the root cause
is FIXED - a C2SIM company-level move now drives the real declared platoons and completes.
