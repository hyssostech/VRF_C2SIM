# PREREG N2: attach composed children in the AUTHORED <Subordinate> order

Date: 2026-09-06. Tier: LIGHT-to-STANDARD (fidelity fix, offline-testable; one confirming run).
Ruling: DESIGN_ORBAT_TO_VRF_2026-09-06 C7 ("honor the declared order for FIDELITY - a small fix,
not a ruling"), ORBAT_LOADING_REQUIREMENTS G2. UG52 18.1.1 p438-439 / 13.3.1 p365: the order
subordinates are added fixes the leader (designator 1) and echelon IDs; not changeable afterwards.

## Change
- InitParser/InitModels: InitUnit.DeclaredSubordinates = UnitType.Subordinate[] verbatim (the R9
  Lean init declares 114.MechCoy -> [1141, 1142, 1143]; the creation list stays UUID-sorted for
  oracle parity, which orders them 1143, 1141, 1142).
- ComposeOrder.ByDeclared (pure) + `--compose-selftest` (5 checks); ApplyHierarchyComposition
  re-orders each parent's attach list by the parent's declared uuids (undeclared children follow
  in creation order) and logs "children attach in the DECLARED <Subordinate> order [...]
  (creation order was [...])" only when the order changed.

## Predictions (before the run)
- Offline: `--compose-selftest` 5/5; all other self-tests unchanged.
- Run E (default settings = compose ON, R9 Lean init, R9 order): app log carries
  "ComposeHierarchy: 114.MechCoy children attach in the DECLARED <Subordinate> order
  [1141.MechPlt, 1142.MechPlt, 1143.MechPlt] (creation order was [1143.MechPlt, 1141.MechPlt,
  1142.MechPlt])" and "composed - 3/3 declared children attached"; 3/3 TASKCMPLT; cluster moves
  as V/C/D (16 entities 400-1000 m north, spread < 1 km). C7 says leader identity does not gate
  movement, so NO movement change is predicted; the run is a no-regression check, and the
  attach-order line is the fidelity evidence (the sim's echelon ID/designator is not exposed on
  our channels - UG52 25.2.1 - so it is not measured here).
  FALSIFIER: the log line is absent (declared list not parsed / not wired) or the company fails
  to complete -> STOP.

## Results
- Offline: `--compose-selftest` 5/5 PASS; the other 9 self-tests unchanged (0 failed).
- Run E 20260906T165015Z (default settings, N2 build): PASS. App log, verbatim:
  "ComposeHierarchy: 114.MechCoy children attach in the DECLARED <Subordinate> order
  [1141.MechPlt, 1142.MechPlt, 1143.MechPlt] (creation order was [1143.MechPlt, 1141.MechPlt,
  1142.MechPlt]); first = leader (UG52 18.1.1)." then "-> EMPTY shell; will attach 3 declared
  child unit(s) [1141.MechPlt, 1142.MechPlt, 1143.MechPlt]" and "composed - 3/3 declared
  children attached." 3/3 VRF task complete, 3/3 TASKCMPLT, 0 dropped; 16-entity cluster moved
  north as V/C/D, final spread 559 m; runner exit 0. No movement change (as C7 predicts).

VERDICT: N2 DONE. The attach follows the authored order; 1141.MechPlt (declared first) is attached
first and is therefore the leader per UG52 18.1.1 - the designator itself is not observable on our
channels (UG52 25.2.1) and is not claimed as measured.
