# PREREG L1 + L2 - MAK's own large-area baseline, and the documented per-member move (2026-10-15)

STATUS: PREREG ONLY. NOT RUN. Waits for the operator's go, relayed by the STP-1097 supervisor. Branch
feat/aggregate-documented-move (local, not pushed). The rules for both runs:
- fresh holder, never 26720; RTI, the demo config (RL-20261005-02), stp-lt (9500-9599) and 19575 untouched;
- the box idle first; customer data never used;
- HARD STOP at the box time below; stop at once on RTI damage or a back-end-stop sign (the vendor log silent,
  SIM/WALL pinned at 1.000, BACK END LOST);
- a clean teardown.
App numbers: claimed from the marker at the go (5 per launch: holder, retry, back end, vrfGui, VrfC2SimApp), and
ledgered before the first join.

## Why live (docs first - what was read and why it does not answer)

- Large area. Read: UG 39244-39273 (the back end pages by object location and prediction; page-out by polygon
  count), AC 8918-8946 (a generated cache holds imagery and elevation; "Feature data is not cached"), AC 6020-6036
  (page-in areas: paged IVE / MetaFlight only), terrainInterfaceConfig.mtl (factory, never tuned), and the Road to
  Kaunas sample (RoadToKaunas.scn:3-4, 16-17, 22; RoadToKaunas.pdf). They show HOW MAK sets up a large-area
  aggregate scenario on our terrain - the same MAK Earth Aggregate (online) .mtf, the same SMS, the same four
  movement systems - but nothing about its paging load or memory on this host. Only running it answers that.
- The move. Read: navigate-to-location.xml:17-18 (a system subtask, "not designed for end user use");
  Move_To_Location_Plan_Path.xml:6, :18 ("Used by aggregate level units") and .lua:45 (it starts navigate-to-location
  as a subtask); group_movement_simplified.lua:28-55, :220-224 (Move (Group) sends each subordinate the wrapper to an
  offset destination); UG 29.1.4 p561 and p554 (a conflicting task replaces the current one); UG 29.4 p572 and
  29.4.2 (task the unit; a directly tasked member reverts to its superior). They define the intended use. They say
  nothing about the back-end stop (STP-1163), or about re-tasking the members of a MOVING aggregate. The API
  reference, luadoc and help are "Documentation Has Not Been Installed" stubs on this host
  (vrForces5.2.docu.exe is the operator's to install). So a registered A/B is the only way to learn whether our
  documented differences matter.

## L1 - MAK's Road to Kaunas, as shipped, with NO interface (the large-area baseline)

- Run: LaunchVrf52 with -Scenario Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunasPhaseTwo
  (the variant with the most planned movers: 24 units carry movement tasks in its plans - RTK diff, rtkdiff
  scratch). Its own .scn: MAK Earth Aggregate (online) for the sim and the GUI, variable-frame 0.1 s,
  AggregateTacticalLevel.sms. GUI on (as our runs). appData: C:\C2SIM\vrf-appdata-unattended\appData, the
  same as every demo-path run, so only the SCENARIO differs from ours.
- Nothing is pushed or tasked by us. The scenario's own plans run when the sim runs.
- Sampled: the 1 Hz working set / CPU sampler on the back end (abort at 16 GB or 700 MB/min over 300 s); the
  vendor log's feature_source "queued for N seconds" waits (counts and median/p95 only, read after the close,
  never quoted); WatchVrf --report-backends every 60 s for liveness; one end screenshot.
- Window: 20 min of simulation running after "Successfully loaded scenario".
- PREDICTIONS (MEDIUM - the first run of a vendor sample here):
  - L1-a: no back-end loss in the 20 min;
  - L1-b: working-set slope <= 200 MB/min after the first 5 min;
  - L1-c: feature-queue median wait < 30 s.
- READING:
  - All three hold: MAK's pattern carries a large area here. Our tasking pattern (and the frame mode,
    variable-frame vs our fixed-frame-run-to-complete) are the suspects. That leads to L2, and then to a
    large-box run with PlanPath.
  - L1-a or L1-b fails: the large-area load happens with MAK's own pattern too. Then the terrain levers (T2
    sim-only buildings off, T4 terrainInterfaceConfig) come before any tasking change.
- Box time: about 30 min (launch 2-3 min, window 20, teardown 3).

## L2 - the documented per-member move (M1 + M2) against today's planner, on the synthetic R-a recipe

- Build: feat/aggregate-documented-move (8ed7b6f; --planned-move-selftest (m11) red on stubs and green on
  the code; 31/31 selftests; suite 813/0/2).
- Recipe: REPRO_BACKEND_STOP R-a, unchanged. tools/stp/make_backend_stop_repro.py, destination
  (54.0403, 23.3364), one synthetic infantry brigade container (17 members), CONSTR then STREND ENGAGE to the
  same point. Fixture IronStorm_Centre_52_Aggregate (read-only), the Demo overlay as R-a ran it.
- ONE VARIABLE: Vrf:AggregateMovePlanner, set by the environment for the run. Everything else is R-a's.
  - Arm A: Auto (today's: navigate-to-location to each member, one identical point).
  - Arm B: PlanPath (Move_To_Location_Plan_Path to each member, each to its own Move (Group) offset
    destination, buffer 0 and MAK_ROAD from the movement system).
- Order: B1, A1, B2 - three fresh launches. A1 is the same-day, same-build control.
- Measured, as in R-a: the second dispatch's time; the vendor log's last write after it (read after the close);
  SIM/WALL; BACK END LOST; the top-3 thread CPU; the working set. For B also, from the interface log:
  - "PLANNED MOVE ... move_to_location_plan_path issued" with the offset note (unlocated count);
  - per-member completions of type move_to_location_plan_path closing each step.
- PREDICTIONS:
  - A1: STOP (MEDIUM-HIGH: R-a's recipe stopped at that place 5 of 6 times).
  - B1, B2: NO PREDICTION of the outcome. The cause is UNEXPLAINED. What is registered is the reading.
  - Glue (HIGH): in B the member steps close on the wrapper's completions, and at most 1 of 17 members is
    unlocated. If the steps never close, or all members are unlocated, the B arm is VOID (a glue defect),
    not evidence.
- READING:
  - A1 stops, B1 and B2 run clean: the documented differences (system subtask as a top-level task; one
    identical point) are IMPLICATED (n = 2, not proof). Next: PlanPath on the option-3 box, PREREG first, then
    the owner's ruling on RL-20260928-03.
  - A1 stops and any B stops: the documented move alone does not prevent it (the misuse hypothesis is
    falsified as sufficient). Next: the frame mode (RTK variable-frame vs our fixed-frame-run-to-complete;
    RTK diff item 1), registered as its own variable.
  - A1 does not stop: the recipe did not reproduce today. B is uninformative; record it, run nothing more.
- Stop rules as above. A stopped back end ends the arm (StopVrf52, the forced own back end on exit 3).
- Box time: about 45 min (3 x 15).

## Not in L1/L2 (separate PREREGs if the readings call for them)

- The frame-mode variable on our fixture (variable-frame 0.1 s, as RTK).
- PlanPath on the option-3 box (the large-area run).
- Group mode on our containers: unusable as built. PseudoAggregate.ope:40 has no systems and :115-119 enables
  only PA_* scripts; Move (Group) is enabled only by ground-echelon-group-tactics-enabler.sysdef:16-22, which
  none of our 7 brigade container templates includes (RTK diff, M3).
