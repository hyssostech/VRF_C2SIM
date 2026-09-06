# DESIGN - representing a C2SIM ORBAT of mixed echelon depth in VR-Forces

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
