# REPRO: VR-Forces 5.2d back-end stop on a second navigate - SANITIZED (for a vendor case; NOTHING SENT)

STATUS: PREREG committed BEFORE any launch. Scope (STP-1097 supervisor, under the operator's "run the live tests you
need"): at most 3 live runs; a synthetic minimal init, no customer data; (a) the same point, (b) a clearly different dry
point - to separate "the place" from "the re-task shape". Rules as D36/D47 (RTI, holder 26720, stp-lt, 19575 untouched).
App numbers: Appendix B 5468-5482.

## Inputs (generated, no customer data)

tools/stp/make_backend_stop_repro.py -> repro_init.xml (one generic infantry brigade TEST_BDE, SFGPUCI----H---, echelon
BDE, at 54.0194 N 23.3139 E - the stops' start rounded to ~10 m; sides SIDE_A / SIDE_B; no task organisation) and
repro_order_<a|b>.xml (TEST_MOVE_1 CONSTR -> STREND TEST_MOVE_2 ENGAGE, both to the destination, ROEHold, 60-min slots).
xsd PASS all three; --parse-order 2 tasks, STREND parsed. The interface (VRF_C2SIM MAIN) populates TEST_BDE from its own
authored table as an aggregate container of 17 members and drives each move as one navigate-to-location per member
(obstacleQuery MAK_OBSTACLE, pathQuery MAK_ROAD, buffer 10). Pushed through a disposable STP container (STP main 6a90ee35)
to c2sim-server-vrf; VR-Forces 5.2d on the run-owned appData, scenario IronStorm_Centre_52_Aggregate (MAK Earth (online)
terrain, aggregate model set) - the scenario carries no unit data of its own that the repro uses.

## Runs and predictions

- R-a: destination (54.0403, 23.3364) - the point of all 4 stops. Predict STOP (MEDIUM-HIGH: 4 of 4 with Iron Storm's
  48 IBCT; this init differs only in names, uuids, no superior, a rounded start).
- R-b: destination (54.000, 23.290) - D36 arm (ii)'s dry point. Predict NO STOP (MEDIUM: arm (ii), n=1).
- R-c (spare): repeat whichever of R-a / R-b disagrees with its prediction; not run if both match.
STOP = the vendor log's last write within ~1 s of the second dispatch (read after the forced exit), simTime() at exactly
1x after it, BACK END LOST ~2 min later. Window: 6 min after the second dispatch.
Reading: R-a stops, R-b does not -> the place (or a place-dependent planning case) is involved; both stop -> the re-task
shape alone; neither -> the synthetic init differs in something that matters (then the Iron Storm recipe stays the repro).

## RESULT (2026-10-15 01:49-02:16Z; runs/launch52/repro/Ra, Rb; app 5468-5477; R-c not run - both matched)

| Run | destination | move 2 dispatched (members ended / 17) | vendor log last write | after | STOP |
|---|---|---|---|---|---|
| R-a | (54.0403, 23.3364) | 01:52:49.772Z, 90 s after move 1 (4) | 01:52:50.217Z (+0.45 s) | SIM/WALL 5.5 -> 1.000; BACK END LOST; 3 threads at 100%; working set flat 3.1 GB; 37 in-obstacle lines | YES |
| R-b | (54.000, 23.290) | 02:07:30.571Z, 64 s after move 1 (10) | 02:13:42Z (ran to teardown) | SIM/WALL ~10; both moves TASKCMPLT; "the LAST of 17: 17 succeeded"; 0 in-obstacle lines | NO |
Both teardowns: StopVrf exit 3 -> forced exit 6; RTI and holder 26720 intact; no dump. R-a's move 1 pre-flighted water on
the line from 0.77 km (OSM natural=water at 0.75 km); R-b's did not.
READING: the hang REPRODUCES with NO customer data - a generic brigade, generic names and uuids, no superior, a start
rounded to 10 m - at the same place, and does not at the other. With D47 G1 (the same place, 0 members outstanding, the
hang) and D36 arm (ii) (the other dry place, 7 outstanding, no hang), the place is involved and the outstanding-member
count is not what decides it. Not explained: D36 arm (i) (the same place by a separate order 8.25 min later, no hang).
Stops at that place: 5 of 6 re-tasks (19:45Z, F, H, G1, R-a; not (i)); at the other place: 0 of 2 ((ii), R-b).
Vendor case: docs/vendor-cases/MAK_BACKEND_HANG_2026-10-15.md + its attachment folder (operator ruled D51 "send it";
the operator / support-account holder submits it - the seat does not).
