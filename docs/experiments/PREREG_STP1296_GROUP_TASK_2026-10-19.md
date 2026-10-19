# PREREG STP-1296 (D114(a)) - re-task a container through the vendor GROUP task, against PlanPath (2026-10-19)

STATUS: PREREG ONLY, drafted offline. NOT RUN. The operator ruled D114(a) "1 (a)" on 2026-10-19: draft now, ONE run
AFTER the STP 5.11.1 tag, on the operator's go. VR-Forces stays parked otherwise
(docs/HANDOFF_PARK_2026-10-16.md). Roles only; no coordinates; no customer data (the R-a recipe is synthetic).
Branch stp-1296-group-prereg, off feat/aggregate-documented-move 4f31863.

## 0. The question, and what our record already says

QUESTION: does a container re-tasked through the vendor's GROUP task avoid the back-end stop and the planner load of
our per-member pattern? In the group pattern the sim computes the member destinations and issues the member tasks
itself. In ours, the interface sends a task to each member.

VENDOR (as quoted in our records; [V] = read at the cite, on the box):
- UG p551 (28.3.2): "Group planned movement along roads first generates a route for the group. Next, it calculates
  destinations for the subordinates based on their relative positions at the time of tasking. After that, the
  subordinates queue along the route until they reach the end of the route." [V] FINDING_VENDOR_DOCS_VD1_2026-10-16.md:42-45.
- UG p561 (29.1.4): "If you assign a simulation object a task that is mutually exclusive with its current task, the
  current task is abandoned." All movement tasks are one exclusive group. [V] VD-1:23-25. UG p554: a new task replaces
  the current one, and a plan is abandoned. [V] VD-1:20-22.
- Move (Group) = AggregateTacticalLevel\scripts\group_movement_simplified. It sends EACH subordinate
  Move_To_Location_Plan_Path to that subordinate's own offset destination (.lua :28-55, :220-224; the useRoads default
  is .xml :70-74). [V] AggregateMovePlanner.cs:29-32; PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md:23-24.
- MAK's large-area aggregate sample, Road to Kaunas, on our terrain and SMS: tasks in its plans and saved states are
  move_to_waypoint_plan_path, move_to_location_plan_path and group-attack-to-objective (counts per scenario).
  [V] FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:105-108. Which OBJECT each task is given to (a container or a
  battalion unit) is not in our record. Gate G0b reads it.

OWN RECORD:
- The stop: 5 of 6 re-tasks at the R-a place stopped the back end; 0 of 2 at the other place
  (REPRO_BACKEND_STOP_2026-10-15.md:34-42). Its signature: the vendor log silent within 0.5 s of the second dispatch,
  SIM/WALL pinned at 1.000, BACK END LOST about 2 min later, a flat working set (:25-26, :34).
- L2 on the same recipe (L2_RESULT_2026-10-16.md:21-48): Auto (A1) STOPPED. PlanPath (B1, B2) ran clean to teardown at
  8.3-8.9x, with a 13 MB/min slope after 5 min (n = 2, "implicated, not proof").
  - In both B arms, 6 of 17 members never finished.
  - LD-21b put exactly the 4 second-battalion move-1 goals in OSM water (STP-976 c14322). The supervisor's ruling on
    that check: "mapping not confirmed".
- The paging-backlog failure is a different class (D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md:89-117). It is not
  reached inside today's box (RL-20261005-02).
- VD-1's ranked item 1 (VD-1:251-255) is to re-task the CONTAINER through a vendor group task. That path "has not been
  tried on the stop recipe".
- THE BLOCKER, in our own record: "Group mode on our containers: unusable as built". PseudoAggregate.ope:40 has no
  systems, and :115-119 enables only PA_* scripts. Move (Group) is enabled only by
  ground-echelon-group-tactics-enabler.sysdef:16-22, which none of our 7 brigade container templates includes
  (PREREG_L1_L2:91-94; HANDOFF_2026-10-15.md:33-34).
- The interface already carries the switch: Vrf:AggregateMovePlanner=Group issues Move (Group) on the CONTAINER per STP
  vertex (AggregateMovePlanner.cs:29-32; VrfC2SimService.cs:3764). PlanPath issues the same member script from the
  interface (AggregateMovePlanner.cs:43-54).

Implication (a separate sentence): the variable is WHO re-tasks the members. Under Group the sim's group script issues
the member tasks, from offsets it computes at tasking. Under PlanPath the interface sends each member the SAME script,
to offsets it computes by a port of that script's own offset rule (MemberOffsets.cs). The member planner task is the
same in both arms, so a large difference in planner load is not expected from the docs.

## 1. Gates before any launch (each must PASS; any miss = NOT RUN, and the miss is reported)

- G0a, a CONTAINER that can run Move (Group). A box-side, read-only docs read, cited in this file before the build:
  - which shipped container template carries ground-echelon-group-tactics-enabler (the MC p613-614 Echelon Group
    "PA" entries are the candidates, VD-1:85-86);
  - how Road to Kaunas defines its containers.
  Then ONE of these, chosen by the read and stated here:
  - (i) R-a's brigade populated as that vendor container type through the existing type map. This is config only;
  - (ii) our container template gains the enabler. That is a derived-SMS change, and it needs its own owner ruling
    (MG / UG p1309-1314).
  If neither is possible: NOT RUN. Report it, and the D114(a) question goes to MAK (D114(c)).
- G0b, how Road to Kaunas issues its move. From its shipped .scnx plans (read-only): the object each movement task is
  given to, and the script ids. Recorded as counts and ids only.
  - If the sample never gives a Move (Group) or group-* task to a CONTAINER, the premise "as the sample does" is FALSE.
  - The run is then re-scoped before launch: the question becomes Move (Group) vs PlanPath only, with the sample cited
    as unit-level.
- G0c, offline proof on the build: --planned-move-selftest and --rulings-selftest PASS. The R-a order pushed to the
  container type of G0a passes --parse-order (2 tasks, STREND).
- G0d, the build is tagged STP 5.11.1 or later for the STP engine image (k5). The exe is the vrf_c2sim build named in
  the run record.
- Memory gate (HANDOFF_PARK sec 5 step 3): -NoGui; at least 7.0 GiB free before StartInterface52; MEMORY STOP below
  6.0 GiB. 4 ledgered numbers per launch, 2 launches.

## 2. The run (one session, two fresh launches, the control first)

- Recipe: REPRO_BACKEND_STOP R-a, unchanged (make_backend_stop_repro.py). That is one synthetic brigade container of 17
  members, CONSTR then STREND ENGAGE to the same destination, inside today's demo extent (Vrf:DemoExtent ON, as
  shipped).
- ONE VARIABLE: Vrf:AggregateMovePlanner, set by the environment for the interface process only.
  - Arm P (control): PlanPath, the L2 B configuration.
  - Arm G: Group.
  - The only other difference is the container type G0a needs, and it applies to BOTH arms.
- Order: P, then G. If the operator's "ONE run" means one launch, only G runs. P is then taken from L2 B1/B2, with the
  stated differences (GUI on in L2; a different build). That weakens every comparison below to a record comparison.
- Logged, per arm (as L2 and D36 did; vendor log lines counted, never quoted or attached):
  - the back end: the top-3 thread CPU seconds per 5 s, and the working set at 1 Hz (ws_sampler2.ps1; abort at
    700 MB/min over 300 s or 16 GB);
  - SIM/WALL, the per-minute ratio line;
  - NotifyLevel 4: the vendor log's feature_source "queued for N seconds" lines (count, median, p95 per 60 WALL s)
    and the off-road planning failures in BOTH forms, start and end (LD-21 erratum);
  - the vendor log's last write after the second dispatch, and BACK END LOST;
  - member completions per move (of 17), with their task types;
  - for G: the container task's own status lines, and whether the members show Move_To_Location_Plan_Path.
- Window: 6 WALL min after the second dispatch, or the CAP T0 + 1,200 WALL.

## 3. Predictions (registered before the run)

- PP1 (control): P does NOT stop. P = 0.80 (L2 B1, B2; n = 2).
- PG1: G does NOT stop. P = 0.65.
  - For: the members get the same wrapper as P, and the re-task is issued inside the sim (UG p551, p561).
  - Against: Move (Group) has never run on our container (sec 0 blocker), and the stop is UNEXPLAINED.
- PG2, load: G is within the P band on the working-set slope after 5 min (G <= 2 x P) and on the feature-queue median
  (within +/-50% of P). P = 0.60. Basis: the same member planner task (lua :220-224).
- PG3, members: on move 1, G finishes at least as many members as P. The set that fails in P (the second-battalion
  offset goals) is observed in G. Observation, no probability: the sim computes offsets "at the time of tasking"
  (UG p551) where ours are a port.
- Glue: in G the container's task is accepted and runs, and the members show Move_To_Location_Plan_Path. If it is
  refused, or no member moves within 120 WALL s of dispatch, arm G is VOID (G0a did not hold), not a reading.

## 4. Falsifiers and readings (what the logs will show; what to build is not stated here)

- F1, the stop shape in G (REPRO :25-26): the vendor log silent within 1 s of the second dispatch, SIM/WALL pinned,
  BACK END LOST.
- F2, the paging-backlog shape (D36:89-94): queue waits rising, the WS slope abort.
- F3: P stops (the L2 B result does not hold on this build).
- F4: G VOID (glue above).
- Readings, each a measurement statement:
  - neither stops, and G is in the P band: the group task re-tasks the container with no stop on this recipe and no
    load cost (n = 1);
  - G stops and P does not: the in-sim group re-task does not avoid the stop on this recipe;
  - both stop: the recipe stops regardless of who re-tasks (n = 1);
  - G clearly lighter than P (WS slope or queue median outside the band, downward): recorded with its numbers. No cause
    is claimed.
- Design implications are written in a separate RESULT section, after a Fable cold review, per CLAUDE.md sec 4.

## 5. Not tested

- Any box but today's demo extent; the option-3 / LV-2 load.
- Air containers (the HANDOFF_PARK air-container note).
- Road to Kaunas's own run (L1 never ran).
- Auto. It is retired for re-tasks by L2 A1 only as a reading, not by a ruling.
- Group with useRoads off (GroupOffRoad, a labelled control refused without Vrf:AllowLiteralMove).
- The cause of the stop (a MAK question, D114(c)).
