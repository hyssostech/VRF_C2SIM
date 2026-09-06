# DESIGN - representing a C2SIM ORBAT of mixed echelon depth in VR-Forces

## CLOSED - tripwires (REGROUP 2026-09-06, supervisor). Do NOT reopen without NEW live evidence.
Anchors: the vendor sample (commandLineRemoteController.cxx:717-775 build, :1520-1554 attach), the
installed UG52 (22.3 -> 18.1.1 compose; 18.1 create-flat-then-reparent + order fixes leader), and
the DETERMINISTIC runs. A doc-inferred "gap" that a passing run contradicts is NOT a gap.
C1 (CORRECTED 2026-09-06 audit) the company move failure had TWO INDEPENDENT causes superimposed -
   the earlier "root cause = hierarchy-blind create" CONFLATED them:
   C1a STRUCTURE/LOADING bug: createSubordinates=true on a unit that ALSO has declared children ->
       ~48 template phantoms + the declared platoons orphaned at 0 m. A fidelity error (wrong objects
       moved). Verified by the trace. Fixed by compose (represents the declared structure).
   C1b MOVEMENT bug, INDEPENDENT of C1a: a template higher-unit created via remote createAggregate
       does not move - it scatters - with OR without a post-create formation settle (G-A 112702Z clean
       template, auto off; 131748Z auto on; same signature). FALSIFIER of "C1a caused the move
       failure": fixing the structure alone (G-A) did NOT fix movement. Fixed by compose /
       expand-to-compose. MECHANISM CLOSED 2026-09-06 BY THE SIM'S OWN CONSOLE (PREREG_CONSOLE_
       CHANNEL sec 6.1, run T 160640Z at object notify level 4): the higher-unit's move-along
       controller starts a move-into-formation subtask first (VRF-8977: "waits until a unit's
       formation is considered valid before initiating the movement") which fans a formation-slot
       Move-To to every DIRECT member; the 3 sub-platoons report complete at t=51 but ONE
       template-created HQ-section vehicle (M998 HMMWV "AUX", Tank Headquarters Section (USA).entity
       :65/:67) never completes its slot move - an internally generated move-along drives it 12-15 km
       off in a straight line - so the formation is never valid, the gate never opens, the route move
       never starts. NOT a formation-at-creation problem (no formation/leader/organization complaint
       on the company's console; birth geometry not stacked). Compose works because its members are
       the DECLARED platoons (+ our one HQ entity), never the template's HMMWV pair. CONTROL run C
       (161714Z, composed, console 4): identical controller path, all three platoons report
       move-into-formation complete by t=51.3, "Move into formation complete", the company issues
       offset routes 114.MechCoy_R0/R1/R2 (UG52 25.2), each platoon's maneuver-along completes,
       company move-along Completed t=77.7, 3/3 TASKCMPLT. "Unresolved formation", "orientation
       race", "settle" were all wrong; initialFormation-at-create (C10) cannot help a member that
       never arrives.
   Compose fixes BOTH, which is why it is the answer and why the session reached the right FIX from
   a conflated causal story. Not a vendor bug (the vendor sample = compose); not a race per se (the
   non-determinism was C1b's symptom).
C2 THE FIX = COMPOSE per the vendor sample: empty shell (createSubordinates=false) + create members
   + addToOrganization in the object-created callback + task the parent. 3/3 DETERMINISTIC with ZERO
   formation calls (V 104042Z/105344Z/110323Z). PREREG_COMPOSE_A. UG52 22.3: ORBAT units are built
   this way (combine existing = Aggregate As).
C3 A TEMPLATE higher-unit via remote createAggregate SCATTERS - with AggregateFormation OFF (G-A
   112702Z) AND ON (131748Z), same signature. Do not retry the template path for company+.
C4 AggregateFormation='auto' (SetAggregateFormation+Reorganize) is NOT needed for compose (V ran with
   it OFF) and does NOT rescue a template. Keep OFF. The earlier "B1 harmful" / "B1 is the missing
   settle" flip-flops were confounds (phantom context; then falsified by 131748Z).
C5 COARSE LEAF (no declared children) at company+ = EXPAND-to-compose: read the mapped template's
   .entity doctrinal sub-units (ObjectTypeResolver), create each as a platoon template, compose in
   DECLARED order (HQ first) - full TO&E moved + completed (124700Z; DETERMINISTIC 3/3 more:
   PREREG_N3_EXPAND_3X X1/X2/X3 170113Z/171105Z/172132Z, identical). LEAF PLATOON = template
   (1222 4/4). Regiment+ leaf exceeds the EntityLevel ceiling -> Y-15. RECURSIVE since N4
   (2026-09-06): a synthesized sub-unit that is itself coarse is expanded too (MaxExpandDepth 3),
   so no template COMPANY is ever created under a battalion leaf (C1b). EXPAND ONLY PURE HIGHER-UNITS
   (every subordinate a unit): a MIXED template (mech platoon = IFVs + squads) stays a template,
   because synthesizing its unit subs would drop its vehicles (readiness audit 2026-09-06; 22 COA-STP1
   units; ComposeOrder.IsPureHigherUnit, --compose-selftest). NOTE the 5.2 type map sends
   BN rows to the "Tank Headquarters Section (USA)" CP PROXY (data/unit-type-map-52.json F-*-F),
   whose subs are vehicles - so for COA-STP1 the recursion does not fire; the proxy is a type-map
   fidelity ruling, not a compose question.
C6 NO post-attach reorganize is needed (addToOrganization resolves the formation - designators +
   promotion map) and NO client-side formation-validity wait is needed (VRF-8977: the app waits).
   Both were listed as "gaps" by the requirements audit and are REFUTED by V. Do not add them.
C7 LEADER IDENTITY does not gate movement (V: platoon leader; expand: HQ leader; both completed).
   Honor the C2SIM declared <Subordinate> order for FIDELITY (UG52 18.1.1) - a small fix, not a
   ruling. There is no "HQ vs maneuver" policy decision.
C8 VRF objects are addressed by their REAL VRF_UUID (from ObjectCreated), NEVER a name-as-DtUUID -
   SETTLED 2026-09-02 (PREREG_ROUTE_UUID_FIX; rwUUID.h 35-char marking blob; VrfC2SimService.cs:
   1908-1916). The name is only the in-app map KEY; all VRF calls use the real uuid (:1375). The
   audit's "G5 name-correlation gap" re-discovered this settled rule and is NOT a gap.
C9 There is NO vendor sample/source for the MSDL/ORBAT importer (headers only; importOrbat builds a
   DATA tree and does not create objects). The vendor's only worked aggregate sample = compose (C2).
C10 sendVrfObjectCreateMsg + initialFormation is a real API but NOT a compose replacement for a
    declared ORBAT (template subs get VRF-assigned uuids -> violates one-object-per-UUID, UG52 22.1);
    at most a coarse-leaf probe. DROPPED 2026-09-06: C1b's mechanism is a member that never reaches
    its slot, which an initial formation cannot fix. Do not build it.
C11 THE FIRST INSTRUMENT for any VRF behavior question is the object's OWN CONSOLE at notify level 4
    (UG52 21.9.1 p483; remote API setObjectNotifyLevel vrfRemoteController.h:1953; app setting
    Vrf:ObjectConsoleNotifyLevel; WatchVrf CON rows = the complete capture, the app callback gets
    only a subset by an unestablished rule). One run at level 4 closed C1b after a week of inference. Silence at the
    vendor default level 1 is the configured outcome, not evidence (lessons-vendor-diagnostics-first).
    The template company path is unusable in this build not because of our create call but because
    the vendor's Tank/Mech HQ-section templates carry M998 HMMWVs whose generated slot move never
    completes; compose/expand (C2/C5) sidestep it and are the design regardless (fidelity, C1a).
C12 LIFEFORMS CRASH THE 5.2d HEADLESS SIM until the DI-Guy DATA is installed (2026-09-06, three runs
    183612Z / 184832Z / 185519Z, identical callstack DtDiGuyController::determineInitialHandItem <-
    preFirstTickInit; C:\MAK\SharedData\19\latest\ModelData\Lifeforms\DIGuy ABSENT - the 2026-09-02
    minimal-data ruling skipped the 6.33 GB DI-Guy package as "GUI visuals"). Not our code, not the
    type map: the fidelity table is the first thing on any build to instantiate a DI-Guy human.
    FIX = install the package (user). No vrfSim switch disables DI-Guy (--nodiguy is
    translationFileCreate's). Interim vehicle-only proxies for lifeform rows = fidelity regression
    = user ruling. PREREG_COASTP1_52_RUN1 sec 6-7; DEMO_READINESS row 17.
RESEARCH AUDIT (2026-09-06, supervisor) - the ORBAT thread's workflows, graded on what survived
the NEXT live run. Sound core throughout = the vendor sample + installed UG52 + deterministic runs;
every specific MECHANISM the workflows produced was later falsified or left open:
  wf_52b70722 F-DIVERGE mechanism -> "higher-unit controller / orientation race": characterised
    C1b's SYMPTOM (non-deterministic scatter) but missed the template-vs-compose split; superseded.
  wf_5004b243 aggregate-tasking -> "B1 harmful; double-creation not the fault (rank5)": B1 verdict
    was context-confounded (later: auto neither needed nor rescuing); rank5 was right about C1b,
    wrong to dismiss C1a (the trace then found it).
  wf_e5cb1379 compose feasibility -> CORRECT; validated 3/3 (C2).
  wf_16e3e97f template-vs-composed -> "unresolved formation, settle fixes it": the settle (auto)
    did NOT rescue the template (131748Z) - mechanism INCOMPLETE; CLOSED 2026-09-06 by the sim's
    own console (C1b/C11): a formation GATE, but the blocker is a template HQ-section vehicle that
    never reaches its slot - not an "unresolved formation".
  wf_1f29a1ad vendor catalog-leaf -> "auto is the missing settle" FALSIFIED by 131748Z; "the MSDL
    importer IS the recursion (verified)" OVERSTATED (data model only, no sample; C9).
  wf_80d19fed loading mandate -> manufactured gaps G3/G4 (refuted by V) + G5 (settled 2026-09-02,
    C8); G1 real (compose default); G2 fidelity-only.
  Pattern: doc/header inference + agent synthesis, in isolation from the repo's settled notes and
  the passing runs, produced confident causes that the next run falsified. The (A)/(B) conflation
  in C1 survived INTO the first regroup until this audit. Rule (feedback-anchor-vendor-and-own-
  notes): anchor order sample > installed docs > deterministic runs > headers > agents.
DISSENT LOG: a session that disagrees writes ONE line here naming the NEW evidence; reopening is
the user's call.
NEXT (the only real work): N1 DONE 2026-09-06 (PREREG_N1_COMPOSE_DEFAULT: default ON verified by
run D 162958Z 3/3 with no env; flag-off run L 164022Z reproduces the legacy 38-phantom / 2-of-3
signature - the switch is the regression control). N2 DONE 2026-09-06 (PREREG_N2_DECLARED_ORDER:
ComposeOrder.ByDeclared + --compose-selftest 5/5; run E 165015Z attaches [1141, 1142, 1143] in the
authored order, 3/3). N3 DONE 2026-09-06 (PREREG_N3_EXPAND_3X: 3/3 identical; flag-off covered by
L). NEXT = COA-STP1 at scale on 5.2 (readiness: REBASELINE_52_INSTRUMENTS sec 6 - census reads 5.2
sub-routes from the console; position reports need slice R1 = the oracle's periodic poll; runner
-ClientId C2SIM; NO -StopWhenComplete on 5.2 until R1 lands). THEN (user ruling 2026-09-06)
DEMO-READY = the interface as a STANDALONE deployment without the harness (docs/DEMO_READINESS_
2026-09-06.md, 15-item gap list) BEFORE the real STP task vocabulary. Everything else below is
history.

2026-09-06. Generalizes the compose recipe (PREREG_COMPOSE_A, validated V) from one company to an
arbitrary C2SIM ORBAT where units are declared to DIFFERENT depths (some to platoon/platform, some
only to battalion). Plan/design - not yet implemented beyond what COMPOSE_A already ships. All
echelon/catalog facts below were read from the installed 5.2d catalog this session.

## 0. What C2SIM gives us (the ORBAT), and the two vendor primitives
The init is a TREE: each unit has a Superior (parent) uuid (InitParser captures it), an EchelonCode
(BDE/BN/COY/PLT/SECT/...), a SIDC (echelon char at index 11: D=platoon, E=company, F=battalion,
G=regiment, H=brigade), a type/function, and a position. The tree's DEPTH VARIES per branch: one
branch may bottom out at platoon, another at battalion, another at a single platform.

Two VR-Forces primitives, both vendor-verified (commandLineRemoteController.cxx:717-775,1520-1554;
UG52 18.1, 13.3; createAggregate createSubordinates flag cgf.h:617-621; addToOrganization
vrfRemoteController.h:1334-1339):
- TEMPLATE unit  = createAggregate(createSubordinates=TRUE). VR-Forces instantiates the unit's
  TYPICAL doctrinal formation for that echelon/type (a Tank Company template -> HQ + 3 platoons +
  vehicles). Use for a unit the ORBAT does NOT decompose (a LEAF).
- COMPOSED unit  = createAggregate(createSubordinates=FALSE) empty shell + addToOrganization(child,
  parent) per declared child. Use for a unit the ORBAT DOES decompose (an INTERNAL node); the shell
  needs NO template at its echelon.
- ENTITY (platform) = createEntity. Use for an ORBAT leaf that is a single platform.

## G-A RESULT 2026-09-06 (run 20260906T112702Z) - "leaf -> template" is FALSE for company+
G-A tested a CLEAN leaf template COMPANY (114.MechCoy with its declared platoons REMOVED ->
createSubordinates=true, NO double-creation, no B1). It SCATTERED like the phantom: company cluster
23 movers / 0 still, 19 SOUTH-staged + 3 RUNAWAYS (16.7 / 13.6 / 4.7 km), only 4 north, company did
NOT complete (only 1.BdeHQ + 1222 completed); 37 objects (clean, no double-creation). This
FALSIFIES two beliefs: (1) the phantom failure was the double-creation - NO, a clean template
scatters too; (2) leaf->template works above platoon - NO. The real distinction is COMPOSE vs
TEMPLATE: composed company = 3/3 deterministic PASS; template company = 7/8 runs scatter (phantom
4/4 + B1-off 2/3 + this clean 1/1), same signature. Only TEMPLATE PLATOONS work (1222 4/4; the
platoon controller, not the higher-unit one). MECHANISM (VERIFIED 2026-09-06, wf_16e3e97f, refuter
SURVIVES, all cites opened): task-time is byte-identical (both send only CreateRoute+MoveAlongRoute
on the company uuid) - the fork is entirely CREATE-TIME. The company higher-unit sysdef's ONLY mover
is aggregate-move-along-controller (no adapter, no maneuver-along, NO isUnitMovementExhausted
fail-safe, no take_formation; ground-higherUnit-disaggregated-movement.sysdef:177-203;
disaggregatedMoveAlongController.h:56,422,449) and builds member routes purely from DtFormationState
offsets. createSubordinates=true batch-instantiates 1 HQ + 3 platoons all at offset (0,0,0) with an
UNRESOLVED formation state -> lookupFollowOffset returns (0,0,0)/default offsets (formationState.h:
52-53,61-66,150-152) -> members staged backward + flung km away, no fail-safe -> scatter (birth
dispersal 738 m vs composed 79 m). COMPOSE fixes it: addToOrganization makes the org controller
assign distinct leader-first designators + the command net -> VALID promotion map
(aggregateOrganizationController.h:96,103,108) -> valid offsets. Template PLATOONS work only because
the platoon sysdef adds the adapter + maneuver-along controller (leader + fail-safe;
ground-disaggregated-movement.sysdef:176-221). ASSUMED (one inferred link): the template's formation
state is invalid specifically AT TASK TIME (batch-instantiation path not in the headers) - but every
candidate sub-trigger routes through the same fail-safe-less controller, so expand-to-compose
sidesteps it by construction.
=> REVISED RULE (sec 1): a LEAF at COMPANY+ echelon must NOT be a template; EXPAND it into its
doctrinal sub-units (platoon templates, which work) and COMPOSE (the proven path). Leaf platoon/
platform unchanged. See sec 1 (revised) + sec 5.

## 1. The mapping rule (recursive, bottom-up) - REVISED after G-A
Classify each ORBAT node by whether ANOTHER surviving unit names it as Superior, AND by echelon:
- LEAF at PLATOON or below (platoon, section, platform): represent by the VR-Forces TEMPLATE for
  its echelon+type (a platform -> entity). VERIFIED works (1222 platoon 4/4). The template's
  members run on the PLATOON controller, which is reliable.
- LEAF at COMPANY or above: do NOT use a template - G-A proved a template higher-unit scatters
  (the higher-unit controller). Instead EXPAND the unit into its DOCTRINAL sub-units (the sub-units
  the catalog template would contain - e.g. a Tank Company = HQ + 3 tank platoons) and COMPOSE it
  from those (each sub-unit a platoon template, which works) via the proven empty-shell +
  addToOrganization path. Source the doctrinal composition from the catalog (.entity file / a
  composition table), NOT from the broken createSubordinates=true template.
- INTERNAL (has declared children): create an EMPTY shell (no template) and COMPOSE it from its
  declared children via addToOrganization. Recurse: children are LEAF or INTERNAL. Create bottom-up
  (leaves first), attach up the tree, attach in the ORBAT's declared order (UG52 18.1.1: order
  fixes leader/echelon).
So COMPOSE is the reliable higher-unit path in BOTH the internal case (children from the ORBAT) and
the coarse-leaf case (children from doctrine); TEMPLATE is used ONLY at platoon-and-below.
COMPOSE_A validated INTERNAL=company / LEAF=platoon (V: 3/3 platoons attached, company moved+
completed, deterministic). The coarse-leaf expand-to-compose is HIGH-confidence (it is exactly what
V did, sourcing platoons from doctrine instead of the ORBAT) but NOT yet run - validation owed.
Current code (ApplyHierarchyComposition) does internal-compose + leaf-template; the coarse-leaf
EXPAND step is NEW and not yet implemented.

## 2. The template ceiling (VERIFIED from the installed catalog) - the real constraint
EntityLevel (our scenario SMS) .entity templates exist at: Platoon (37), Section (23), Company
(15), Squadron (8), Battalion (7 - e.g. "Mechanized Battalion (US Army M2)", "Infantry Battalion").
NO Regiment/Brigade in EntityLevel. AggregateTacticalLevel holds Battalion Tactical Group /
Regiment templates. Y-15 (settled): SMS is PER-SCENARIO, no mixing (UG52 13.7).
Consequences for a LEAF (a template is required only for leaves; internal nodes are composed and
need no template):
- Leaf at PLATOON..BATTALION echelon -> native EntityLevel template exists. Representable.
- Leaf at REGIMENT+ -> NO EntityLevel template -> exceeds the ceiling. Options (decision, not the
  recipe): (a) run that ORBAT in AggregateTacticalLevel (Y-15 SMS choice; coarser for everything);
  (b) COMPOSE the regiment from doctrinal battalions - but those are NOT in the ORBAT, so this
  INVENTS structure (violates feedback-fidelity unless a doctrinal composition table is authored);
  (c) FAIL LOUDLY (fidelity default: never silently mis-echelon). Recommend (c) now, (a) as the
  scale path.
- INTERNAL node at ANY echelon (incl. regiment/brigade WITH declared children) -> composed from
  its children, NO template needed -> the ceiling does NOT bite it. So a brigade that decomposes to
  battalions that decompose to companies is representable in EntityLevel by compose all the way down
  to leaves that are within the ceiling.

## 3. VALIDATION GATES (honest unknowns - a live run each, before relying on the general case)
G-A (PIVOTAL): does a LEAF template at COMPANY/BATTALION echelon TASK and MOVE correctly? This is
  the higher-unit move-along controller path. VERIFIED works: leaf PLATOON template (1222.MechPlt
  4/4) and COMPOSED company (V). NOT verified: a clean leaf COMPANY/BATTALION template (createSub-
  ordinates=true, no declared children, no double-creation) tasked. The ONLY higher-unit template
  we ever tasked was the phantom company - and it scattered, but that was CONFOUNDED by double-
  creation (template company + separately-created declared platoons stacked at one point).
  Hypothesis (not asserted): a clean template company/battalion moves like the composed company
  (structurally both are aggregate-of-platoon-aggregates; the composed run already moved template
  platoons under the higher-unit controller). Test: init one leaf "Mechanized Company"/"Mechanized
  Battalion" (no children), task a move; pass = its members move + it completes, like V.
G-B: multi-level compose (depth 3: battalion -> companies -> platoons). ApplyHierarchyComposition
  handles a node that is both parent and child (TryAdvanceComposition advances both), but only
  depth-2 is validated. Test: a 3-echelon ORBAT; pass = leaves move, every echelon composes, top
  completes.
G-C: TYPE-MAP coverage at higher echelons. data/unit-type-map-52.json currently maps echelons
  ~D/E/F to platoon/company/HQ templates; a BATTALION-echelon leaf has no row -> it would FAIL type
  mapping (AuthoredPending/Failed) and not be created. Extend the fidelity table with battalion (and
  squadron/troop) echelon rows -> the EntityLevel battalion templates (feedback-fidelity: map to the
  correct catalog template, do not proxy down an echelon).
G-D: HQ elements + leader order. Many real units carry an HQ section (type map shows "Tank
  Headquarters Section"). Decide: is the HQ a DECLARED child in the ORBAT (compose it in, first =
  leader) or part of the template? Attach order must put the intended leader/HQ first (UG52 18.1.1).

## 4. Edge cases to handle (mostly already guarded in ApplyHierarchyComposition)
- A child that fails type-mapping is dropped -> its parent composes from the survivors + warns
  (already). - A parent whose children ALL fail -> no composition, parent falls back to template
  (already). - A parent that is not an aggregate but has children -> warn, children standalone
  (already). - Mixed force/domain members in one composed unit: UG52 18.1.1 allows heterogeneous
  members but tasks do not affect non-VR-Forces entities; no same-force constraint found - revisit
  if a future ORBAT mixes. - Partial/duplicate ORBAT delivery: existing duplicate-init guard.

## 5. Recommended sequencing (REVISED after G-A)
1. G-A: DONE 2026-09-06 - FAILED (a clean template company scatters). Conclusion: coarse leaves
   need EXPAND-to-compose, not template. Everything below hangs on this pivot.
2. EXPAND-to-compose design + doctrinal-composition SOURCE - how does a coarse leaf (company/
   battalion) get its sub-unit list? Options (docs-first, decide before coding): (a) parse the
   catalog .entity file for that unit type (it lists the template's sub-units - authoritative
   doctrine); (b) a small authored composition table (unit-type -> sub-unit templates), keyed like
   unit-type-map-52.json; (c) create the template once, GetAggregateMembers (already recursive) to
   READ its composition, delete it, re-create members as composable aggregates (uses the broken
   template only to read - wasteful/risky). Lean (a) or (b).
3. VALIDATE expand-to-compose: one leaf company (expanded -> 3 platoon templates -> composed) move.
   Pass = same as V (platoons move north, company completes). High confidence (it is V with
   doctrine-sourced platoons) but a live gate is owed.
4. G-B: depth-3 ORBAT compose (battalion of companies of platoons).
5. G-C: type-map echelon coverage so company/battalion units resolve to a type (to be expanded).
6. Ceiling/Y-15: regiment+ leaf policy.
Internal-compose CODE is done + validated (COMPOSE_A); the EXPAND step (items 2-3) is NEW.

## EXPAND VALIDATION RESULT 2026-09-06 (run 20260906T124700Z) - PASS (recipe-faithful, full TO&E)
Coarse leaf 114.MechCoy (childless, GA_LeafCompany init), Vrf:ComposeHierarchy=true: live catalog
loaded (C:\MAK\vrforces5.2d, EntityLevel, 1912 templates); 114.MechCoy resolved to "Tank Company
(USA)"; EXPANDED to 4 doctrinal sub-units [HQ1, TANK2, TANK3, TANK4] in the .entity's DECLARED order
(HQ first, full TO&E per the user ruling), 4/4 attached via AddToOrganization; company TASKCMPLT
(139aa71b), DROP=0; MechCoy cluster 23 movers / 0 still, ALL NORTH, 0 runaway, maxdisp 1040 m (route
legs ~550-1100 m); 59 objects. The HQ-as-leader hazard did NOT materialize (HQ attached first, unit
moved clean) - which is why the earlier maneuver-first reordering was removed as an unneeded
invention. Members are catalog templates + the vendor addToOrganization recipe; the only new code is
the compose orchestration + reusing the existing ObjectTypeResolver to read the .entity composition.
1 run; the compose path itself is 3/3 deterministic - confirm 3/3 if this ships.
OPEN (user 2026-09-06): is a SIMPLER vendor-DIRECT catalog-leaf bind possible (createSubordinates=
true + a settle / reorganizeAggregate / formation-validity wait) that makes a template higher-unit
taskable, replacing the synthesize-and-compose expand? Research wf_1f29a1ad running. Expand stands
as the verified fallback.

## RESEARCH wf_1f29a1ad (refuter SURVIVES) - reframes G-A; VENDOR-DIRECT bind is now on live test
Verified from docs/headers (independent of any run):
- The vendor has TWO first-class create paths, both taskable at any echelon: COMPOSE (empty shell +
  addToOrganization; the only worked sample, commandLineRemoteController.cxx:717-775,1520-1554) and
  PRECONFIGURED CATALOG TYPE (createSubordinates=true; the GUI/ORBAT/MSDL DEFAULT, entityCreationData
  .h:33, cgf.h:617-621). createSubordinates=true is NOT inherently broken.
- The remote createAggregate(createSubordinates) overload has NO initialFormation param
  (vrfRemoteController.h:1295-1306); the GUI create data DOES (entityCreationData.h:33). So a REMOTE
  template create skips the initial geometry the GUI supplies - the app must settle it via
  AggregateFormation='auto' (SetAggregateFormation+ReorganizeAggregate). THIS WAS OFF IN BOTH G-A
  RUNS. Composed passed WITHOUT it (addToOrganization resolves geometry); template scattered WITHOUT
  it. So G-A did NOT test the vendor-canonical direct path, and my "B1/auto is harmful" was
  CONFOUNDED (that verdict came from the double-creation phantom + top-only application; auto
  completed a battalion 3/3 on GOLDEN terrain 2026-07-14).
- The user's rule IS the vendor MSDL/ORBAT importer recursion (explicit subordinates -> create
  members with parent; leaf -> catalog type at any echelon; "empty" unit types for explicit-children
  nodes, UG52 7.2.2). Move Along works at any echelon, forwarded to the lead subordinate
  (moveAlongTasks.h:32-37, Migration 2.4.1). CORRECTION: the 5.2 formation-validity wait is VRF-8977,
  NOT VRF-8968 (our docs say 8968 - fix on the next pass).
- Also noted (unused): sendVrfObjectCreateMsg (vrfRemoteController.h:1577-1595) carries createSubObjects
  AND initialFormation - a more-GUI-like create the port does not use.
DECISIVE LIVE TEST (running, bvw2w4q44): G-A leaf-company, createSubordinates=true (direct catalog
bind, ComposeHierarchy OFF) + AggregateFormation='auto' (the settle that was off). PASS => the
coarse leaf binds to the catalog DIRECTLY (+ auto) and expand is unnecessary (simpler, more vendor-
native, matches the user rule). Caveat (refuter): Mojave has an empty-leader-path failure mode where
only SubordinateFanOut has worked, so auto-alone may not suffice -> then + fan-out; if still not,
compose stays. NOTE: my prior "B1 harmful / do not use auto" record is UNDER REVIEW pending this test.

### DIRECT-BIND + auto RESULT 2026-09-06 (run 20260906T131748Z) - FAILS; direct catalog bind is out
G-A leaf-company, createSubordinates=true (direct template, ComposeHierarchy OFF) + AggregateFormation
='auto'. The settle FIRED (app log: "114.MechCoy - set formation 'column' (from its own list) +
reorganize"). It STILL SCATTERED: 19 south + 3 runaways (16.6/15.1/5.0 km), 4 north, 114.MechCoy NOT
completed (TASKCMPLT=2); 37 objects - essentially identical to G-A (auto off). So the settle made NO
difference. CONCLUSIONS (now un-confounded, both arms tested):
- The DIRECT catalog-template bind does NOT work at company level on 5.2/Mojave, WITH OR WITHOUT the
  vendor settle (auto). The research's "auto is the missing step" hypothesis is FALSIFIED here.
- COMPOSE is the working path (V 3/3; expand full-TO&E 124700) and is the vendor's own only worked
  aggregate recipe. "resolve to the catalog at the leaf" must mean COMPOSE from the catalog's
  declared composition (= expand-to-compose), NOT template-instantiate it.
- B1/auto reconciled: it does NOT rescue a template company (tested); it is neither the fix nor
  needed for the working (compose) path (V + expand passed with auto OFF). So "do not rely on auto
  for a higher-unit" stands - now for a tested reason, not the earlier confounded one.
- MECHANISM still not fully closed (carried, not asserted): SetFormation+Reorganize resolves
  designators but the template's per-member follow-offsets stay degenerate on this build - reason
  unverified.
UNTESTED (declined - not simpler, not "direct"): template + auto + SubordinateFanOut (the flagged
Mojave mitigation); even a pass would be more moving parts than compose and would task members, not
the unit. => DECISION: ship EXPAND-TO-COMPOSE (validated) for coarse leaves; template higher-units
are not a usable direct bind on this build.

### INSTALLED 5.2 USERS GUIDE - READ DIRECTLY 2026-09-06 (had been skipping these for this Q)
The vendor's documented ORBAT/unit build, from the installed VRFUsersGuide.pdf (page = PDF page):
- Ch 22 "Creating and Using an Order of Battle" (p489-495): an ORBAT is a hierarchy of sim objects
  each with a UNIQUE UUID (unlike the palette, which is a template for many instances); you build it
  (add members, aggregate into units), INSTANTIATE members into a scenario, and import/export it.
- 22.3 "Creating Units in the Order of Battle Panel" (p493): units in an ORBAT are built "the SAME as
  18.1.1 Creating a Unit by Combining Existing Simulation Objects" -> i.e. COMPOSE ("Aggregate As"),
  NOT preconfigured templates. So the vendor's documented ORBAT-unit build IS COMPOSE - exactly the
  user rule + our expand-to-compose.
- 18.1 (p438): two create methods - combine existing (compose, "Aggregate As") vs preconfigured unit
  from the palette (template). "You cannot create units that are subordinates of an existing unit...
  once you create a unit, you can subordinate it" = create-flat-then-reparent (= addToOrganization).
- 18.1.1 / 13.3.1 (p438/p365): subordinate ORDER at create fixes the LEADER (= designator 1),
  unchangeable after; aggregate assigns leader designator 1 then successive. Matches our declared-
  order attach (HQ-first worked in the expand run).
- AddingContent 13.11 (p327) "Importing a Hierarchy File" is about ELEMENT DEFINITIONS (.hier/.leaf
  visual/definition catalog), NOT instantiating an ORBAT of units - not the create-and-task path.
NET: the installed docs GROUND compose as the vendor-canonical ORBAT-unit build. Expand-to-compose
is that method with the leaf's composition read from the catalog. The direct preconfigured-template
is the GUI's other option; its REMOTE equivalent needs initialFormation (sendVrfObjectCreateMsg,
vrfRemoteController.h:1577-1600) which our createAggregate overload lacks - the reason the remote
direct bind scattered. DECISION UNCHANGED: expand-to-compose (validated + documented). sendVrfObject
CreateMsg + initialFormation is the documented remote direct-bind alternative, untested, native add.

### CORRECTION 2026-09-06 - "the MSDL importer IS the recursion (VERIFIED)" was OVERSTATED
There is NO vendor SAMPLE for the MSDL/ORBAT importer (checked: no examples/ MSDL|ORBAT project - only
scenarioMetrics; every "source" grep hit is a compiled .dll; no msdl/orbat .cxx/.cpp anywhere; the
symbols live only in headers). The importer is a built-in GUI/core feature and its importOrbat()
"simply creates the structures. Does not do anything with the imported data"
(vrfMsdlOrbatImporterExporter.h:57) - it builds an ORBAT DATA tree (DtOrbatManagerNode: parentUUID +
orbatOrderedList + childAdded/Removed, orbatManagerNode.h), it does NOT create or task entities.
createFromMSDLItem is protected with no shipped .cpp. So the importer is verified only as a DATA
STRUCTURE (parent/ordered-children tree - the SHAPE of the user rule), NOT as a runnable create-and-
task recipe, and the entity-create step is the GUI's (not shipped; it carries initialFormation the
remote API lacks - the same gap that scatters the remote template). NET: the ONLY runnable vendor
sample for a taskable aggregate is commandLineRemoteController = COMPOSE. The importer gives no
shortcut. This firms the decision (expand-to-compose) rather than changing it.

## 6. Adversarial review
Strongest competing view: "leaf=template just works at every echelon, no gate needed." Not safe -
the one higher-echelon template we tasked failed (phantom), and although that is best explained by
double-creation (a clean template has none), it is not VERIFIED clean; G-A is cheap and decisive, so
gate it. Corrected assumption: I first believed EntityLevel topped out at COMPANY (so battalion
needed AggregateTacticalLevel) - FALSIFIED by the catalog (EntityLevel has 7 battalion templates);
the true ceiling is REGIMENT. VERIFIED: the two primitives + their APIs; the EntityLevel echelon
inventory; that internal nodes need no template; that ApplyHierarchyComposition already encodes
leaf-vs-internal. ASSUMED (gated): G-A/G-B behaviours; that the type map can be extended cleanly to
battalion templates; that VRF template internals (HQ placement) do not break the leader-order rule.
