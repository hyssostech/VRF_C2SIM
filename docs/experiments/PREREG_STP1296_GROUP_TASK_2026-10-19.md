# PREREG STP-1296 (D114(a)) - re-task a container through the vendor GROUP task, against PlanPath (2026-10-19)

STATUS: PREREG ONLY, drafted offline. NOT RUN. The operator ruled D114(a) "1 (a)" on 2026-10-19: draft now, ONE run
AFTER the STP 5.11.1 tag, on the operator's go. VR-Forces stays parked otherwise
(docs/HANDOFF_PARK_2026-10-16.md). Roles only; no coordinates; no customer data (the R-a recipe is synthetic).
Branch stp-1296-group-prereg, off feat/aggregate-documented-move 4f31863.

Fable cold review 2026-10-19: HOLDS WITH CHANGES (13 findings). All are applied below. The reviewer's text is kept
in the seat's scratch, not in the repo.

## 0. The question, what the run can answer, and what our record already says

QUESTION (as asked): does a container re-tasked through the vendor's GROUP task avoid the back-end stop and the
planner load of our per-member pattern?

WHAT THIS RUN CAN ANSWER:
- The stop on the record is AUTO's: 5 of 6 re-tasks at the R-a place, and L2 A1.
- PlanPath, the control here, ran 0 stops in 2 arms (L2 B1, B2).
- So the run answers "does G stop on the stop recipe", and whether its load differs from P's.
- It cannot show "G avoids a stop that P has": P has none on record.
- Expected yield: mostly the load comparison, and whether Move (Group) runs on our container at all. The docs predict
  no large load difference (below).
- That is for the operator to weigh against the box slot.

VENDOR (as quoted in our records; [V] = read at the cite, on the box):
- UG p551 (28.3.2): "Group planned movement along roads first generates a route for the group. Next, it calculates
  destinations for the subordinates based on their relative positions at the time of tasking. After that, the
  subordinates queue along the route until they reach the end of the route." [V] FINDING_VENDOR_DOCS_VD1_2026-10-16.md:42-45.
- UG p561 (29.1.4): "If you assign a simulation object a task that is mutually exclusive with its current task, the
  current task is abandoned." All movement tasks are one exclusive group. [V] VD-1:23-25.
- UG p554: a new task replaces the current one, and a plan is abandoned. [V] VD-1:20-22.
- Move (Group) = AggregateTacticalLevel\scripts\group_movement_simplified. It sends EACH subordinate
  Move_To_Location_Plan_Path to that subordinate's own offset destination (.lua :28-55, :220-224; the useRoads default
  is .xml :70-74). [V] AggregateMovePlanner.cs:29-32; PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md:23-24.
- RN p69 VRF-8801: aggregate-level SMSs have a tab to add systems to pseudo-aggregates.
- RN p75 VRF-9312: in the Aggregate Tactical Level SMS, ground-echelon-group-tactics-enabler.sysdef is propagated to
  pseudo-aggregate units. [V] PREREG_L1_L2_L5_DOCS_ADDENDUM_2026-10-15.md:75-81.
- MAK's large-area aggregate sample, Road to Kaunas, on our terrain and SMS. Its plans and saved states carry
  move_to_waypoint_plan_path, move_to_location_plan_path and group-attack-to-objective (counts per scenario).
  [V] FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:105-108. Which OBJECT each task is given to (a container or a
  battalion unit) is not in our record. Gate G0b reads it.

OWN RECORD:
- The stop:
  - 5 of 6 re-tasks at the R-a place stopped the back end, and 0 of 2 at the other place
    (REPRO_BACKEND_STOP_2026-10-15.md:34-42);
  - the definition: the vendor log's last write within about 1 s of the second dispatch, the sim clock at exactly 1x
    after it, BACK END LOST about 2 min later (:25-26); R-a's working set was flat (:34).
- L2, on the same recipe (L2_RESULT_2026-10-16.md:21-48):
  - Auto (A1) STOPPED. PlanPath (B1, B2) ran clean to teardown at 8.3-8.9x, with a 13.3-13.5 MB/min slope after 5 min
    (n = 2, "implicated, not proof").
  - Move 1 closed on the unit's arrival evidence with 6 of 17 members finished. That count is set by the
    arrival-evidence closure, not by the planner.
  - On move 2, 6 of 17 members never finished, and the record does not name them (L2:38, :76).
- LD-21b put exactly 4 move-1 offset goals (the second battalion's Mech COs) in OSM water (L2:150-162). The supervisor's
  ruling, "mapping not confirmed", is Jira-only (STP-976 c14322).
- The paging-backlog failure is a different class (D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md:89-117). It is not
  reached inside today's box (RL-20261005-02).
- VD-1's ranked item 1 (VD-1:251-255) is to re-task the CONTAINER through a vendor group task, and that has not been
  tried on the stop recipe.
- THE BLOCKER: "Group mode on our containers: unusable as built" (PREREG_L1_L2:92-94; HANDOFF_2026-10-15.md:33-35, in
  docs/experiments/).
  - PseudoAggregate.ope:40 has no systems.
  - :115-119 enables only PA_* scripts.
  - Move (Group) is enabled only by ground-echelon-group-tactics-enabler.sysdef:16-22.
- The interface already carries the switch. With Vrf:AggregateMovePlanner=Group, IssuePlannedVertex issues Move (Group)
  on the CONTAINER per STP vertex (VrfC2SimService.cs:3709-3748, TryIssueScriptedMove :3741; AggregateMovePlanner.cs
  :29-32). PlanPath issues the same member script from the interface (AggregateMovePlanner.cs:43-55).

Implication (a separate sentence): the variable is WHO re-tasks the members.
- Under Group, the sim's group script issues the member tasks, from offsets it computes at tasking.
- Under PlanPath, the interface sends each member the same script, to offsets from a port of that script's own rule
  (MemberOffsets.cs).
- The member planner task is the same in both arms, so a large difference in planner load is not expected from the docs.

## 1. Gates before any launch (each must PASS; any miss = NOT RUN, and the miss is reported)

- G0a, a CONTAINER that can run Move (Group). This is a DRAFTING step, not part of the run: a read-only docs and data
  lookup by the dispatcher in a box gap before the tag, with no VR-Forces launch (D210). Its result is posted to
  STP-1296 and cited here BEFORE any build. The reads, in this order:
  1. RN p69 (VRF-8801) and p75 (VRF-9312) at the page. Then the shipped PseudoAggregate.ope and the sysdef list it
     actually loads in the AggregateTacticalLevel SMS: is the group-tactics enabler already propagated to our
     container? If yes, the blocker is already gone, and neither (i) nor (ii) is needed.
  2. If not: which shipped container template carries the enabler (the MC p613-614 Echelon Group "PA" entries are the
     candidates, VD-1:85-86), and which .ope it names.
  Then exactly one of these, stated here:
  - (i) R-a's brigade populated as that vendor type, through the existing type map. This is config only, and ONLY if
    its .ope is PseudoAggregate.ope. Otherwise ContainerCatalogue.RoleOfPlatform (ContainerCatalogue.cs:76-85) returns
    Unknown, and the interface will not populate it.
  - (ii) Struck. It would need either a write under C:\MAK (forbidden, CLAUDE.md sec 5) or a switch to the derived-SMS
    _C2SIM fixture (RUNBOOK:938, "NOT live-proven"), which is a second variable.
  If neither step 1 nor (i) holds: NOT RUN. The D114(a) question goes to MAK (D114(c)).
- G0b, how Road to Kaunas issues its move: from its shipped .scnx plans (read-only), the object each movement task is
  given to, and the script ids, recorded as counts and ids only. If the sample never gives a group-* task or Move (Group)
  to a CONTAINER, the premise "as the sample does" is FALSE. The question is then re-scoped before launch to Move (Group)
  vs PlanPath only, with the sample cited as unit-level.
- G0c, offline proof on the build:
  - --planned-move-selftest and --rulings-selftest PASS;
  - the R-a order with the G0a container passes --parse-order (2 tasks, STREND);
  - if G0a needs a type-map change, the type map resolves R-a's brigade to the G0a template, and RoleOfPlatform gives
    Container.
- G0d, builds: the STP engine image is tagged STP 5.11.1 or later (k5). The vrf_c2sim exe is the build named in the run
  record.
- G0e, the C2SIM server is ALIVE: D106 (operator only) has restarted c2sim-server-vrf since LV-1b, whose publishing leg
  has been dead since then (HANDOFF_PARK:109-112). A silent server must not be read as a silent sim.
- G0f, the launch script: handoff-2026-10-15/repro_run.ps1:19 launches with -FrontendAppNumber (GUI on, 5 numbers).
  This run uses -NoGui and 4 numbers per launch; guard-config refuses the mix (HANDOFF_PARK:121). The script change is
  declared and reviewed before the run.
- Memory gate (HANDOFF_PARK sec 5 step 3): at least 7.0 GiB free before StartInterface52; MEMORY STOP below 6.0 GiB.
  Two launches, 8 ledgered numbers.

## 2. The run (one session, two fresh launches, the control first)

- Recipe: REPRO_BACKEND_STOP R-a (make_backend_stop_repro.py). That is one synthetic brigade container of 17 members,
  CONSTR then STREND ENGAGE to the same destination, inside today's demo extent (Vrf:DemoExtent ON, as shipped). The
  Demo overlay's console level stays at -1 (appsettings.Demo.json:104), as R-a and L2 ran it (PREREG_L1_L2:115-117).
- ONE VARIABLE: Vrf:AggregateMovePlanner, set by the environment for the interface process only.
  - Arm P (control): PlanPath, the L2 B configuration.
  - Arm G: Group.
  - The G0a container type, if one is needed, applies to BOTH arms.
- DEVIATIONS FROM R-a / L2, the same in both arms:
  - -NoGui;
  - the G0a container type, if any;
  - the interface logger at Debug for the interface's own category. Under Group the member branch logs only at Debug
    (VrfC2SimService.cs:10039-10042), because RoutePlannedMemberCompletion returns false for Group (:3838), and the
    profile runs Information (appsettings.json:4).
- Order: P, then G, in the ONE registered session. This is the supervisor's ruling D210 (2026-10-19), inside the
  operator's "one run": the control shares the day, build and box state, and is not borrowed from L2.
- Logged, per arm (as L2 and D36 did; vendor log lines counted, never quoted or attached):
  - the back end: the top-3 thread CPU seconds per 5 s, and the working set at 1 Hz (ws_sampler2.ps1; abort at
    700 MB/min over 300 s or 16 GB);
  - SIM/WALL, the per-minute ratio line;
  - from the vendor log at its default level, as D36 and L2 read it: the feature_source "queued for N seconds" lines
    (count, median, p95 per 60 WALL s), and the off-road planning failures in BOTH forms, start and end (the LD-21
    erratum, L2:146-149);
  - the vendor log's last write after the second dispatch, and BACK END LOST;
  - from the Debug member lines: member task completions per move (of 17), with their task types;
  - for G: the container task's status lines.

## 3. Predictions (registered before the run)

- PP1 (control): P does NOT stop. P = 0.75. Basis: L2 B1, B2, n = 2, weakened by the container-type change.
- PG1: G does NOT stop. P = 0.65.
  - For: the members get the same wrapper as P, and the re-task is issued inside the sim (UG p551, p561).
  - Against: Move (Group) has never run on our container, and the stop's cause is UNEXPLAINED.
- PG2, load: G is within the P band on the working-set slope after 5 min (G <= 2 x P) and on the feature-queue median
  (within +/-50% of P). P = 0.60. Basis: the same member planner task (lua :220-224).
- PG3, members (observation, no probability):
  - in each arm, the move-1 members that had not completed when move 2 replaced move 1, and how many of 17 finish
    move 2;
  - the sim computes its offsets "at the time of tasking" (UG p551), where ours are a port.
- Glue, move 1 ONLY:
  - G is VOID (G0a did not hold) if the container refuses the first Move (Group), or if no member task start appears
    in the Debug member lines or the vendor log within 300 WALL s of the first dispatch.
  - L2's first displacement came at +92 to +110 s on a 10 s cadence, and G plans a group route before the member tasks.
  - A refusal or failure of the RE-TASK (move 2) is a RESULT, not a VOID.

## 4. Falsifiers and readings (what the logs will show; what to build is not stated here)

- F1, the stop shape in G (REPRO :25-26): the vendor log silent within 1 s of the second dispatch, SIM/WALL pinned,
  BACK END LOST.
- F2, the paging-backlog shape (D36:89-94): queue waits rising, the WS slope abort.
- F3: P stops (the L2 B result does not hold on this build and container).
- F4: G VOID (glue above).
- Readings, each a measurement statement:
  - neither stops, and G is in the P band: on this recipe the group task re-tasks the container with no stop and no
    load cost (n = 1). This is NOT "G beats our pattern": P does not stop either.
  - G stops and P does not: the in-sim group re-task does not avoid the stop on this recipe.
  - both stop: the recipe stops regardless of who re-tasks (n = 1).
  - G clearly lighter than P (WS slope or queue median outside the band, downward): recorded with its numbers. No cause
    is claimed.
- Design implications are written in a separate RESULT section, after a Fable cold review, per CLAUDE.md sec 4.

## 5. Not tested

- Any box but today's demo extent; the option-3 / LV-2 load.
- Air containers (the HANDOFF_PARK air-container note).
- Road to Kaunas's own run (L1 never ran).
- Auto. L2 A1 implicates it for re-tasks; no ruling retires it (RL-20260928-03 stands).
- Group with useRoads off (GroupOffRoad, a labelled control refused without Vrf:AllowLiteralMove).
- The cause of the stop (a MAK question, D114(c)).
