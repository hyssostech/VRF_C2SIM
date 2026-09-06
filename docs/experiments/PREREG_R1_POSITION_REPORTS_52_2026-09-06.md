# PREREG R1: periodic C2SIM position reports on 5.2 (the oracle's poll, ported)

Date: 2026-09-06. Tier: STANDARD. Source: REBASELINE_52_INSTRUMENTS sec 6 item 4.

## What and why (read, not inferred)
- Every 5.2 run put only its TASKCMPLT task-status reports on the C2SIM bus (run E: 3 of 3);
  the 5.0.2 scale run rung 2 put 1,536 `PositionReportContent` on it.
- The app's only position path was `OnVrfTextReport`, which relays `POSITION "<name>" lat lon`
  VRF text reports emitted by the 5.0.2 fixture's C2simEx Lua tracking script (WatchVrf `RPT`
  rows: 20,784 in rung 2, 0 in every 5.2 run; the 5.2 fixture is on EntityLevel.sms).
- The C++ oracle's periodic reports did not depend on that script: C2SIMinterface.cpp:388-460
  sleeps `reportInterval`, then for every unit with our clientId calls
  `getUnitGeodeticFromSim` (reflected object -> state repository -> location) and sends one
  position report (bundled). `reportTimeInterval == 0` = off (:377-378).
- Port: `Vrf:PositionReportSeconds` (0 = off, oracle default) + `Vrf:PositionReportSides`
  ("both" = oracle sendTrackingCode 1, what the scale runs used; "blue" / "red"). On the tick
  thread every N s: for each created unit passing the side filter, `TryGetEntityGeodetic` (the
  facade's port of getUnitGeodeticFromSim, entity OR aggregate) -> `BuildPositionReport` ->
  push (or the P4b bundle when Vrf:BundlePositionReports is on). One log line per round.
  The text-report path is untouched (it simply has nothing to relay on 5.2).

## Predictions (before the run)
- Run P (R9 Lean init, R9 order, default settings + `Vrf__PositionReportSeconds=10`, RunSecs
  360): app log "R1 position reports: 6 sent, 0 skipped" per round after all 6 units exist
  (earlier rounds may skip units not yet reflected); `reports-captured.log` carries
  `PositionReportContent` for all 6 C2SIM uuids, count within +/-20% of
  6 x (window / 10) ~ 6 x 36 = 216 (the round count depends on when the app joined); 3/3
  TASKCMPLT unchanged; `run_census.py` `reports` > 0, `reportUuids` = 6, performers' net_km
  > 0 for the three taskees; movement geometry = D/E (no behavior change from reporting).
  FALSIFIER: 0 position reports captured with the setting on, or any TASKCMPLT lost, or the
  tick thread stalling (POS cadence gaps) -> STOP.
- Also carried by run P (no separate run): N4 recursive-expand code on a non-battalion init
  must be inert (same compose lines as E), and the member-level split at default (-1) must
  request no member levels ("console-level requests" = 0).

## Results
Run P 20260906T173325Z: PASS on every line.
- App log: 40 "R1 position reports: 6 sent, 0 skipped" rounds (every 10 s from the first tick
  after all 6 units existed; never a skipped unit).
- Bus capture (reports-captured.log): 237 REPORT# = 234 PositionReportContent + 3 TaskStatus;
  6 distinct SubjectEntity uuids (all 6 units). 234 = 6 x 39 rounds, inside the predicted
  216 +/- 20%.
- run_census.py on the 5.2 run: reports 234, reportUuids 6, net_km of the taskees 1.15 /
  0.69 / 0.99 km (39 fixes each) - the C2SIM-side net-displacement measurement works on 5.2
  again; everReal 22.
- 3/3 VRF task complete + 3/3 TASKCMPLT; 16-entity cluster, final spread 557 m (= E); POS
  cadence unchanged. N4 inert (4 compose lines incl. the N2 order line); member-level split at
  default requested 0 member levels. Runner exit 0.
VERDICT: R1 DONE. Vrf:PositionReportSeconds=10 is the value for the scale run and the demo
default (DEMO_READINESS row 4 -> D once the demo appsettings carry it).
