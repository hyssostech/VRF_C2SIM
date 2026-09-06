# Vendor ORBAT-loading mandate vs our implementation - requirements + gaps

## SUPERVISOR CORRECTION 2026-09-06 (regroup) - READ THIS FIRST; the gap list below is OVERSTATED
This audit was doc-inferred and did NOT check the repo's own settled notes or the passing runs. Its
gaps re-graded against the anchors (vendor sample + installed UG52 + the deterministic runs):
- G1 (compose off by default) - REAL. The only true fix. Already decided; flip the default.
- G2 (authored subordinate order discarded) - REAL but FIDELITY only: leader identity does NOT gate
  movement (V: platoon leader; expand: HQ leader; both completed). Small fix, no ruling needed.
- G3 (reorganize after attach) - REFUTED. Run V composed with ZERO formation calls and moved +
  completed 3/3: addToOrganization resolves the formation. Do not add a reorganize.
- G4 (client formation-validity wait) - REFUTED. VRF-8977: the app waits; V moved with no client
  wait. Do not add one.
- G5 (name-only correlation) - NOT A GAP; a SETTLED rule re-discovered. PREREG_ROUTE_UUID_FIX
  (2026-09-02) + VrfC2SimService.cs:1908-1916: never pass a NAME as a DtUUID (rwUUID.h 35-char
  blob); address by the real VRF_UUID from ObjectCreated - which the code does (name = in-app map
  KEY only; all VRF calls use the real uuid, :1375). The audit conflated the vendor's import-rename
  behaviour with our in-app map over names we control. Preserving the C2SIM uuid as startingUUID is
  an OPTIONAL simplification, not a gap.
- G6 (unmapped-type silent default) - real but minor; per refuter it is a silent Tank default, not
  Ground_Aggregate. Track, low priority.
The direct-create (sendVrfObjectCreateMsg) item is PARKED (see DESIGN_ORBAT CLOSED C10).
Lesson recorded in memory: a doc-inferred gap that a passing run contradicts is not a gap; check
the repo's own settled notes before "discovering" anything.

2026-09-06. Captured from the INSTALLED 5.2 docs (VRFUsersGuide.pdf, ReleaseNotes, class ref) and
audited against our code. Workflow wf_80d19fed (5 doc/code angles + synth + refuter SURVIVES; every
load-bearing doc page + code line re-opened by the refuter). User was right: we had not captured the
loading mandate - the create-mechanism work was done in isolation and several loading rules are unmet.
"Vendor-mandated" for the REMOTE API is an INFERENCE (docs describe GUI/editor workflows; the ORBAT
panel's "Aggregate As" == 18.1.1, and the MAK sample uses empty-shell+addToOrganization) - sound but
not a literal remote-API directive.

## MET (verified compliant)
- Compose order-of-ops: empty shell (createSubordinates=false) + create members + addToOrganization
  after both exist, deferred from the object-created callback (VrfC2SimService.cs:890,936-941,961;
  vendor sample commandLineRemoteController.cxx:717-775,1520-1554).
- createSubordinates=false suppresses the type's default subs (no phantom); no dedicated "empty unit"
  type needed - a subs-suppressed real type is equivalent (UG52 71.4 p1407).
- create-flat-then-reparent; born at force level (UG52 18.1 p438).
- Full multi-level Superior tree; a mid-echelon node is both shell + child (VrfC2SimService.cs:869-917).
- One distinct object per ORBAT member (UG52 22.1 p490).

## GAPS (verified, refuter-confirmed) - fix in this order
G1 COMPOSE IS OFF BY DEFAULT. appsettings.json has no ComposeHierarchy key -> false -> the DEFAULT
   load applies the TEMPLATE method to every unit = the phantom breakage. UG52 22.3 p493 mandates the
   combine-existing method (18.1.1) for ORBAT units. FIX: default Vrf:ComposeHierarchy=true (add to
   appsettings.json); keep false only as an explicit legacy golden-parity mode.
   DONE 2026-09-06 (N1): VrfSettings default true + appsettings "ComposeHierarchy": true; OFF only via
   Vrf__ComposeHierarchy=false (the N3 regression control). Verified by PREREG_N1_COMPOSE_DEFAULT.
G2 DONE 2026-09-06 (N2): InitUnit.DeclaredSubordinates + ComposeOrder.ByDeclared; the attach follows
   the authored <Subordinate> order (run E 165015Z; PREREG_N2_DECLARED_ORDER). Original finding:
   AUTHORED SUBORDINATE ORDER DISCARDED -> WRONG LEADER. InitParser sorts units by UUID
   (InitParser.cs:118) and never parses the authored <Subordinate> order (init xml HQ-first); attach
   follows UUID order, so the lowest-UUID child (not the intended leader) becomes designator 1. UG52
   18.1.1 p438-439 / 13.3.1 p365: subordinate ORDER fixes the leader + echelon and cannot change
   after create. FIX: honor the authored <Subordinate> / document order for the attach sequence.
   [SOFT RULE: the vendor-faithful default is "honor the C2SIM declared order" (whatever C2SIM lists
   first leads; for an expanded coarse leaf, the .entity order - HQ first - which the expand run used
   and it moved+completed). Only needs a user ruling if we want to OVERRIDE C2SIM's order.]
G3 LEADER/FORMATION NEVER RE-ESTABLISHED POST-ATTACH. setAggregateFormation+reorganizeAggregate fire
   on the EMPTY shell at create (before children attach) = a documented no-op (setAggregateFormation
   "no effect" unless a valid aggregate leader with members, vrfRemoteController.h:1545-1549);
   FinishComposition never reorganizes after attach (VrfC2SimService.cs:945-973). FIX: move
   setAggregateFormation+reorganizeAggregate into FinishComposition AFTER all attaches, on the
   populated aggregate; relax the _formationApplied one-shot for the compose path. (Partly qualified:
   UG52 25.1 p516 says echelon IDs are auto-assigned at unit creation - may make this redundant for
   the all-at-once path; the incremental empty-shell+attach case is UNKNOWN - verify by echelon-ID
   read-back.)
G4 NO FORMATION-VALIDITY WAIT before MoveAlongRoute (VRF-8977 shape). Pre-task gate waits for member
   attachment only (VrfC2SimService.cs:1342-1352), not formation validity. UG52 25.2 p517: a unit
   move needs a valid formation. FIX: wait for formation validity before tasking; set
   AggregateFormation=auto by default. (UNKNOWN: is VRF-8977's fix backend (auto-benefits remote
   moves) or GUI-only - resolve before sizing client-side work.)
G5 C2SIM UUID DROPPED; CORRELATION IS NAME-ONLY. CreateAggregate passes DtUUID::nullUUID()
   (VrfFacade.cpp:710), CreateEntity passes none; correlation is _vrfUuidByName by NAME
   (VrfC2SimService.cs:1864). Names are NOT unique and are renamed/truncated on import (entity names
   <=11 chars, unit <=31; UG52 13.2 Table 21 p362, 13.2.1/2 p363) - name-keying collides
   last-writer-wins and mis-routes tasks/reports. The create API accepts startingUUID
   (vrfRemoteController.h:1276,1305) and we already preserve it for AREAS (CreateControlArea
   VrfFacade.cpp:730-734). FIX: pass the C2SIM UUID as startingUUID on CreateEntity/CreateAggregate;
   re-key correlation (_vrfUuidByName, _childToParent, task routing, report attribution) on UUID.
G6 (reworded per refuter) UNMAPPED TYPE NOT REJECTED IN RealTemplates. NOT the "Ground_Aggregate"
   claim (that is GoldenParity-only, UnitTranslator.cs:222) - the real issue: RealTemplates SILENTLY
   substitutes a default (Tank, UnitTranslator.cs:95/101) for an unmapped type instead of the
   doc-sanctioned "add an empty unit" / reject (UG52 7.2.2 p241; rejection only fires in FidelityTable
   mode, VrfC2SimService.cs:606). FIX: reject/flag unmapped types in RealTemplates too.

## ALSO MISSED (refuter-added, verified)
- NAME LENGTH CAPS entity<=11 / unit<=31 chars + rename-on-import (UG52 13.2.2 p363) - reinforces G5.
- SOME TYPES CANNOT BE AGGREGATED: Crowds, Convoys, Carrier Air Wings, Animal Herds (UG52 71.4
  p1407) - if a C2SIM node maps to one, compose is invalid; guard it.

## CORRECTED (overstated in the first pass)
- Compose is mandated for forming a unit FROM declared members; a coarse leaf as a preconfigured
  (template) unit is vendor-SANCTIONED (UG52 22.1 p490 - an ORBAT may include palette objects), not a
  violation. So: declared-children node -> compose (G1); coarse leaf -> template OR expand-to-compose.

## WHERE DIRECT-CREATE (sendVrfObjectCreateMsg + initialFormation) FITS
It is the ONLY create exposing both createSubObjects and initialFormation (vrfRemoteController.h:
1577-1600, "users should not need to call this"). It is NOT a compose replacement for declared
ORBATs (a template spawns generic subs with VRF-assigned UUIDs, violating one-object-per-UUID, UG52
22.1 p490). Its role = a SINGLE-VARIABLE PROBE: does supplying initialFormation at create prevent the
scatter that the formation-less createAggregate causes? Useful for COARSE LEAVES only (where no
doctrinal catalog is available to expand). Run AFTER G1 (compose is the baseline), on its own branch.

## UNKNOWNS - close by live echelon-ID/formation read-back on a deep ORBAT, not by code
(a) does addToOrganization ORDER set the leader in the empty-shell path? (b) backend auto-reorganize
on/off in our rid? (c) is re-parenting a populated aggregate valid? (d) VRF-8977 backend vs GUI-only?
(e) unit move-along completion criterion (leader-arrives vs all-arrive)?

## SEQUENCE
G1 (config) -> G2 (order) -> G3 (reorganize-after-attach) -> G4 (formation-validity wait) -> G5
(UUID identity) -> G6 + non-aggregatable guard. Direct-create probe after G1. Validate G2-G4 by
reading back echelon IDs + leader + formation validity after a composed build.
