# PREREG ADDENDUM L5 - the exercise-clock frame mode on the large box (candidate; 2026-10-15)

STATUS: CANDIDATE ONLY. NOT RUN. Separate from L1/L2 (PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md). It needs:
the operator's go; an OWNER RULING, because it touches RL-20260921-02 item 3 ("3. Fast."); and a sanctioned
fixture deploy. Rules as L1/L2.

## What the documentation says (docs first)

- UG52 3.4.3 p122, the three exercise-clock modes:
  - Variable-Frame Run-To-Complete "is typical for distributed, interactive simulations. It does not provide
    repeatable results. Do not use this mode in time-managed HLA federations."
  - Fixed-Frame Run-To-Complete "is most useful for situations where you want a simulation to run with internal
    consistency and high fidelity, and want it to run to completion, but do not need to observe the simulation
    ... run a simulation overnight and view the results the following day. Fixed-Frame Run-To-Complete mode is
    not suited for interactive use. It is suitable for distributed use only in time-managed HLA federations. It
    disables the Simulation Time Scale Toolbar."
- UG52 4.4.2 p143: with time management, a fixed-frame-run-to-complete sim engine "tries to advance simulation
  time as fast as possible, independent of wall-clock time". Our federation is NOT time-managed (no
  runInTimeManagementMode; RTI_timeMgmt not set).
- UG52 Table 76 p1669 (vrfSim.mtl blockOnAsynchronousOperations): "Applies only to scenarios that are set to run
  in a fixed frame clock mode ... block on asynchronous operations (terrain, feature, path planning,
  navigation)". It is factory 0 here (vrfSim.mtl:445), so OUR fixed-frame runs are NOT repeatable either: path
  planning depends on when tiles arrive (VRF_5.2_DECISION_EVIDENCE Y-3/Y-9).
- physicalWorldParams.mtl:93 (AggregateLevelBase): "Blocking paging calls are never expired."
- Every vendor ground sample ships variable-frame. Road to Kaunas does (RoadToKaunas.scn:16-17, 0.1 s), and so
  does WainwrightMechanizedAttack (DECISION_EVIDENCE Y-3: "FFRTC is a batch posture, not their demo posture").

READING (documented): the demo - interactive, distributed (STP / Pro watching), in a federation that is NOT
time-managed - runs in a mode the vendor says is for unobserved batch runs, and suitable for distributed use only
under time management. The documented interactive mode is variable-frame, and the documented way to make it run
fast is the time multiplier (the Simulation Time Scale toolbar FFRTC disables;
DtVrfRemoteController::setTimeMultiplier, vrfRemoteController.h:827 - already wired as Vrf:TimeMultiplier and
tools/SetSimRate). This is LIKELY MISUSE by the documentation's terms. Whether it bears on the large-box backlog
is UNEXPLAINED.

## Why we chose it, and what changing it would lose

- 2026-09-02 (PREREG_R9_FIXED_FRAME_RTC; FixtureGen README "Exercise-clock frame settings"): chosen for a HEADLESS
  pipeline, "the only documented route to a headless run that is both repeatable and faster than wall clock".
  Its VERDICT: the mode works with our federation. Determinism was the stated goal. It is NOT delivered with
  blockOnAsynchronousOperations 0 (above), and the recommended 1 was never applied (it needs a C:\MAK edit).
- 2026-09-21, RL-20260921-02 item 3: the owner chose "Fast" over the seat's real-time recommendation for the demo
  clock. RUNBOOK 3504-3518: the fast clock is load-dependent - 2.6-15x, and 0.27x on COA-STP1 at scale.
- What we RELY on:
  - (i) a FAST demo clock: DurationScale 0.0417 makes a 1-h slot 150 SIM s, about 25 WALL s at 6x. Under
    variable-frame the multiplier provides that (a fixed rate, not "as fast as the load allows").
  - (ii) the TASK CLOCK on simulation time (Vrf:TaskClock=sim) - works in either mode; R9 proved the app copes
    with a sim clock that is not the wall clock.
  - (iii) nothing relies on repeatability (it is not delivered today, see above).
- What changing it would LOSE:
  - the idle-phase speed-up beyond the chosen multiplier (10-15x when little moves);
  - fixed time steps under load: variable-frame takes LARGER steps when frames run long. That is lower fidelity
    under load (INF), where FFRTC slows simulation time instead.
- What it might GAIN (INF, not a claim): real-time pacing keeps paging and planning in step with the wall clock
  instead of letting simulation time run ahead of tile arrival.

## L5 (candidate) - variable-frame + time multiplier on the D36(b) large-box recipe

- Fixture: a frame variant of IronStorm_Centre_52_Aggregate - ONLY (frame-mode "variable-frame") (frame-time 0.1),
  as RTK. Built with FixtureGen --frame-variant into a scratch --out-dir, then deployed by the one sanctioned
  write (FixtureGen --out-dir to C:\MAK\vrforces5.2d\userData\scenarios), under a NEW name - the demo fixture
  itself is untouched.
- Recipe: D36(b), unchanged otherwise. COA 1 init + order, the option-3 extent (S 53.813 W 23.050 N 54.450
  E 23.764 + 2 km) through a SCRATCH overlay, the same appData and the same build as D36(b) for a like-for-like
  comparison. Vrf:TimeMultiplier set so the movement-phase pace is comparable (6, the D36/pre-run movement-phase
  SIM/WALL). Two coupled settings, declared - the frame mode needs the multiplier to keep the pace.
- Comparison: D36(b) measured BACK END LOST 2 min after the order, working set 2.6 -> 14.3 GB at ~1,860 MB/min,
  feature-queue median 73 s / p95 135 s.
- NO outcome prediction (the backlog's cause is unexplained). Registered readings:
  - no loss for 10 min, slope <= 200 MB/min, queue median < 30 s: the frame mode is implicated; the owner
    rules on RL-20260921-02 item 3 (fast by multiplier instead of run-to-complete);
  - the same collapse as D36(b): the frame mode is not the lever; it stays as ruled.
- Order: AFTER L1 (if RTK on its own variable-frame is clean, L5 isolates the frame mode on OUR scenario) and
  independent of L2. Box time about 30 min.
