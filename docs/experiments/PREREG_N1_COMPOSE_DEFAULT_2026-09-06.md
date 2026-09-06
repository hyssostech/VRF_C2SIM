# PREREG N1: compose becomes the default (Vrf:ComposeHierarchy=true)

Date: 2026-09-06. Tier: STANDARD (a settled design flipped to default; verified by two runs).
Ruling: DESIGN_ORBAT_TO_VRF_2026-09-06 C2 (compose = the fix, user choice "A"), NEXT N1.
Change: VrfSettings.ComposeHierarchy default false -> true; appsettings.json gains
"ComposeHierarchy": true and "ObjectConsoleNotifyLevel": -1 (explicit vendor default). OFF is now
an explicit legacy switch (Vrf__ComposeHierarchy=false) kept only as the regression control.

## Predictions (written before the runs)
- Run D (DEFAULT: no Vrf__ComposeHierarchy env, no console level, R9_Mojave_Lean init, R9 order,
  RunSecs 360) HIGH: app log shows "ComposeHierarchy: 114.MechCoy -> EMPTY shell ..." and
  "composed - 3/3 declared children attached"; 3/3 VRF task complete + 3/3 TASKCMPLT; the 16-entity
  MechCoy cluster moves 400-1000 m at bearing ~0, spread < 1 km (= V 104042Z / C 161714Z).
  FALSIFIER: no ComposeHierarchy lines (default did not take: appsettings not deployed, or the
  env/binding path differs) or the company fails to complete -> STOP, N1 not done.
- Run L (LEGACY: Vrf__ComposeHierarchy=false, same init/order) HIGH: no ComposeHierarchy lines;
  the pre-compose signature returns - template phantoms (cluster far larger than 16 entities) and
  the company does not complete (2/3). FALSIFIER: 3/3 with the flag off -> the flag is not wired
  through (the runs would be indistinguishable) -> STOP.
- Both: 0 DROPPING/ABANDONING; runner exit 0; scoring by score_run.py (cluster 34.64763,-116.69339).

## Results
- Run D 20260906T162958Z (no Vrf__ env at all; deployed appsettings carries the new keys): PASS.
  "ComposeHierarchy: 114.MechCoy -> EMPTY shell; will attach 3 declared child unit(s)" +
  "composed - 3/3 declared children attached"; 3/3 VRF task complete, 3/3 TASKCMPLT, 0 dropped;
  16-entity MechCoy cluster moved 467-925 m at bearing 357-3 deg (= V/C). Runner exit 0.
  Side observation: with ObjectConsoleNotifyLevel at its default (-1, no level requests) the app
  still logged 24 level-1 console lines (members' "Making pivot geometry" chatter), decoded by the
  new decoder - the subscription itself is unconditional and cheap; only the level is opt-in.
- Run L 20260906T164022Z (Vrf__ComposeHierarchy=false, same init/order, N2 build): PASS as the
  regression control - 0 ComposeHierarchy lines; cluster 38 entities (template phantoms; 16 when
  composed); company NOT complete (2/3 TASKCMPLT: 1.BdeHQ + 1222.MechPlt only); 2 runaways to
  14.0 km at bearing 22-26 deg; final spread 14.9 km; 0 dropped; runner exit 0. The pre-compose
  signature (C1a phantoms + C1b gate) returns exactly when the flag is off.

VERDICT: N1 DONE. Compose is the default; OFF reproduces the legacy failure and exists only as
this control. The next flag-off run is owed only if the compose path changes (N3 policy).
