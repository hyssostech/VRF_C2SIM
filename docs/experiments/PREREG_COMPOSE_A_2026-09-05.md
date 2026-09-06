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
- The company shell (createSubordinates=false) acquires a valid formation/leader AFTER children
  attach + a post-attach SetFormation+Reorganize (this is where the retired B1 belongs - AFTER
  attach on real members, NOT top-only pre-attach on phantoms).

## 2. Build order - TWO INCREMENTS (steps 1-2 are shared; de-risks the native work)
INCREMENT 1 (offline C# + one live run; NO native rebuild) - kill the phantom + drive the real
platoons via the EXISTING fan-out:
  S1 Derive parent->children from SuperiorUuid over PLANNED SURVIVORS (no parser change):
     childrenByParent, classify PARENT/CHILD/LEAF. Gate all new behaviour behind a new
     VrfSettings flag Vrf:ComposeHierarchy (default FALSE -> flag-off run byte-identical to today).
     (VrfC2SimService.cs ProcessInitialization ~540-704; InitModels.SuperiorUuid already parsed.)
  S2 Add bool CreateSubordinates to CreationPlan; set FALSE for PARENT plans; EnqueueCreates
     passes p.CreateSubordinates not literal true (:799); stable-partition creates leaves-first
     (before the immediate-vs-terrain branch ~:709-730 so both callers inherit it). This removes
     the ~48-vehicle phantom company template - the root-cause fix.
  S5a Company tasking = FAN OUT to the declared child platoons (existing SubordinateFanOut +
     FanOutTracker), each platoon a proven mover; company TASKCMPLT via the existing
     OnVrfTaskCompleted quorum -> SynthesizeUnitCompletion(company). NO new native method.
  -> Validation run V1 (below). If green: phantom gone + declared platoons move + company completes.
INCREMENT 2 (native rebuild; adds the real VR-Forces company org node = full Option A):
  S3 Add AddToOrganization(child,parent) pass-through to VrfFacade.h/.cpp (mirror SetLocation/
     SetTarget) + VrfBridge marshaling; /t:Rebuild; back up + redeploy all 7 DLL copies
     (memory: feedback-native-fixes-authorized).
  S4 Async compose state machine in OnVrfObjectCreated (+ a TickLoop timeout sweep, mirror
     ExpireTerrainRequests / TerrainProfileTimeoutSeconds; NOT the timeout-less _pendingRouteTasks):
     PendingComposition{parent, expectedChildren, arrivedUuids, deadline, phase} + _compositionReady
     TCS. Flow: register at init; when all children (or timeout subset) have ObjectCreated uuids,
     enqueue the company shell create (createSubordinates=false); when the shell exists, enqueue
     AddToOrganization(childUuid, companyUuid) per platoon in leader-first order, then
     SetFormation+Reorganize on the company, then TrySetResult.
  S5b Create-before-task gate: in RunTaskAsync, await _compositionReady[company] (bounded, same TCS
     idiom as WaitForStartAsync :1024) before ExecuteTaskOnTick, so the deferred parent create does
     not open a drop window (:1070). Optionally task the composed company directly (test) vs keep
     the S5a fan-out.
  -> Validation run V2 (adds pass conditions: GetAggregateMembers(company)=={1141,1142,1143}).

## 3. Validation run(s) - pass conditions written BEFORE the run; a miss is a STOP
Env: 5.2 rtiexec posture, R9 init, EntityLevel; do NOT pass --logFileName (harvest C:\MAK\logs);
instrument-reproduces check first (per lessons-false-greens); MAK licence expires 2026-09-15 - run
before then. Score by name via WatchVrf trace + app log.
V1 (increment 1): P1 PHANTOM GONE - no ~48-vehicle "Tank Company (USA)" template set; the company
subtree = the 3 declared platoons + their own vehicles only (falsifier: any phantom-named mover).
P2 DECLARED PLATOONS MOVE (core) - 1141/1142/1143 each net-displace > 25 m toward the route
(baseline today = 0 m). P3 NO IDLE-ORPHAN. P4 company TASKCMPLT emitted after the 3-platoon quorum.
P5 LEAF REGRESSION - 1222.MechPlt + 1.BdeHQ unchanged; flag-OFF run byte-identical to today.
Score movement by direction-toward-route-end (fix the movement_check MAGNITUDE gap), not raw disp.
V2 (increment 2): + P6 GetAggregateMembers(114.MechCoy) == {1141,1142,1143} (or their flattened
vehicles), NOT template members. + P7 a DIRECT company move (no fan-out) also moves the 3 platoons
(confirms the composed aggregate is taskable - the key ASSUMED item).

## 4. Risks (carry)
Composed-tasks-like-template ASSUMED (V2 P7 is the test); addToOrganization-aggregate-child
ASSUMED; formation-after-attach required (empty shell has no leader at create); async new
drop window (S5b barrier); leak if modelled on _pendingRouteTasks (use a deadline); re-parent
transient double-count when scoring PHANTOM-GONE; native-rebuild blast radius (redeploy all 7,
partial redeploy is a false-green); licence deadline 2026-09-15.

## 5. Result
(filled after V1 / V2)
