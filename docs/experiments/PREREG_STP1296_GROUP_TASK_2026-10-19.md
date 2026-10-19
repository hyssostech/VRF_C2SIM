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
  - (i) R-a's brigade CREATED as that vendor type. CORRECTED by AMENDMENT 1 (sec 1c): this is NOT config. The
    container's init type is chosen in code by ContainerTypeRule.Choose (ContainerCatalogue.cs:116-181; call site
    VrfC2SimService.cs:1690), and it needs a code change with its own self-test. The type must also resolve with
    RoleOfPlatform = Container (ContainerCatalogue.cs:76-85), i.e. its .ope must be PseudoAggregate.ope.
  - (ii) Struck. It would need either a write under C:\MAK (forbidden, CLAUDE.md sec 5) or a switch to the derived-SMS
    _C2SIM fixture (RUNBOOK:938, "NOT live-proven"), which is a second variable.
  If neither step 1 nor (i) holds: NOT RUN. The D114(a) question goes to MAK (D114(c)).
- G0b, how Road to Kaunas issues its move: from its shipped .scnx plans (read-only), the object each movement task is
  given to, and the script ids, recorded as counts and ids only. If the sample never gives a group-* task or Move (Group)
  to a CONTAINER, the premise "as the sample does" is FALSE. The question is then re-scoped before launch to Move (Group)
  vs PlanPath only, with the sample cited as unit-level.
- G0c, offline proof on the build:
  - --planned-move-selftest and --rulings-selftest PASS;
  - the R-a order passes --parse-order (2 tasks, STREND);
  - the container-type change of sec 1c: see the G0c lines there.
- G0d, builds: the STP engine image is tagged STP 5.11.1 or later (k5). The vrf_c2sim exe is the build named in the run
  record.
- G0e, the C2SIM server is ALIVE: D106 (operator only) has restarted c2sim-server-vrf since LV-1b, whose publishing leg
  has been dead since then (HANDOFF_PARK:109-112). A silent server must not be read as a silent sim.
- G0f, the launch script: handoff-2026-10-15/repro_run.ps1:19 launches with -FrontendAppNumber (GUI on, 5 numbers).
  This run uses -NoGui and 4 numbers per launch; guard-config refuses the mix (HANDOFF_PARK:121). The script change is
  declared and reviewed before the run.
- Memory gate (HANDOFF_PARK sec 5 step 3): at least 7.0 GiB free before StartInterface52; MEMORY STOP below 6.0 GiB.
  Two launches, 8 ledgered numbers.

## 1b. G0a / G0b RESULT (dispatcher lookup, 2026-10-19; read-only, no launch; stp-data runs/stp1296/G0a-G0b-vrforces-docs.md)

Measurements, as reported and spot-checked by the dispatcher:
- RN p69 VRF-8801, verbatim: "For aggregate-level SMSs, there is now a tab that allows you to add additional systems
  to pseudo-aggregates."
- RN p75 VRF-9312, verbatim: "For Aggregate Tactical Level SMS, propagate ground-echelon-group-tactics-enabler.sysdef
  to pseudoaggregate units."
- The propagation is per .entity, NOT at SMS level. 41 of 79 PseudoAggregate.ope entities carry the enabler (a
  componentSystem line at :13/:14, e.g. BN_Armor.entity:13); 38 do not. No hit in AggregateTacticalLevel.sms or in any
  .ope.
- The bare Aggregate Container has no enabler: PseudoAggregate.ope:40 `(systems )` is empty, and :115-119 lists only
  PA_* scripts. Our record's "unusable as built" therefore stands for the type the interface creates today.
- The sysdef (:5-24) enables group_movement_simplified (= "Move (Group)", group_movement_simplified.xml:6),
  group-set-ordered-speed, group-navigate-route-to-location, group-attack-to-objective and connect-headquarters-network.
- Enabler-carrying types include 11:1:-1:6:2:1:-1 (BN_Armor.entity, .ope PseudoAggregate.ope),
  11:1:-1:6:30:1:-1, 11:1:-1:8:4:1:111, 11:1:-1:8:30:1:-1, 11:1:-1:8:4:1:211, plus country-specific ones.
- The Model Catalog p613-614 citation in VD-1 does not match the catalog's contents (Echelon Group 5.4 is at p668).
  This is not reconciled.
- G0b: Road to Kaunas gives CONTAINERS group-attack-to-objective only (Counterattack 5 plans + 9 saved states,
  PhaseTwo 3 + 5, RoadToKaunas 0; all on enabler-carrying PA types). There is NO Move (Group) and no other group-* task
  anywhere in the sample.
- Not measured: a live Move (Group) on such a container.

Readings (each a measurement statement):
- G0a = YES via (i): a vendor PA type with platform PseudoAggregate.ope and the enabler (a CODE change picks it, sec 1c). Its
  RoleOfPlatform is Container. Step 1 (SMS-level propagation) = NO.
- G0b: the premise "as the sample does" holds for group-tasking a container, but with a different group task
  (group-attack-to-objective), not Move (Group). Per the G0b rule, the question is re-scoped: Move (Group) vs PlanPath,
  with the sample cited as "containers get a group task (attack), never Move (Group)".

OWED before the build (G0c): see sec 1c.

## 1c. AMENDMENT 1 (2026-10-19, before any build or run): the container type for BOTH arms

Fable cold reviews: the first draft (d4ed4ef) FAILED; this rewrite HOLDS WITH CHANGES, all applied.

Measurements, box (dispatcher follow-up, read-only; stp-data 3f6c01c runs/stp1296/G0a-followup.md; starred lines
spot-checked by the dispatcher):
- "BDE Airborne PA.entity" (AggregateTacticalLevel):
  - :13 is `<componentSystem systemName="ground-echelon-group-tactics-enabler" ...>` (starred);
  - :3 names PseudoAggregate.ope, objectType and matchType 11:1:-1:8:4:1:111 (starred);
  - it has no force or affiliation field.
- "BDE, Air Assault PA.entity" carries the same enabler at :14, plus an air-assault-enabler at :13 (11:1:-1:8:4:1:211).
- The two containers R-a's composition row names carry NO enabler (0 hits; starred). Both are in AggregateLevelBase, on
  PseudoAggregate.ope, with objectTypes 3:11:1:-1:8:3:1:1 ("BDE Light Infantry.entity") and 3:11:1:-1:6:3:1:-1
  ("BN_Light Infantry.entity").
- Our rows write the US country code (225) in their requested types, where the vendor files have -1. That is a request,
  not a defect: a matchType field of -1 is a wildcard (ObjectTypeResolver.cs:17, :51, :317), so the 225 request
  resolves to the -1 template.
- Move_To_Location_Plan_Path (and move-to-location, the useRoads=false branch, group_movement_simplified.lua :223-224)
  appear in NO PseudoAggregate.ope list and NOT in the enabler sysdef. They appear only in the
  *-aggregated-movement.sysdef files. VERIFIED: the lists. ASSUMED, not run: that the sim dispatches a script only to an
  object whose lists name it.

Measurements, code (this repo, read):
- Population FLATTENS: every simulated leaf attaches directly to the tasked container (ContainerComposition.cs:489-492, the doc;
  the code is ExpandRowInto :585-640, flattened++ at :623; ContainerPopulation.cs:495 AddToOrganization; the table's own NESTING note, unit-composition-52-aggregate.json:18).
  So the object R-a and L2 ran already had 17 DIRECT unit subordinates and no battalion container. The records give
  17 members (REPRO_BACKEND_STOP:13-14; L2:146-148); "direct" rests on the code cites.
- PlanPath addresses those same 17 attached members (VrfC2SimService.cs:3630, :3718-3728). Move (Group) on the container
  would address its direct subordinates: the same 17 units.
- The container's init TYPE is chosen in code by ContainerTypeRule.Choose (ContainerCatalogue.cs:114-190, call site
  VrfC2SimService.cs:1690). For UCI it tries branches {3, 4} with specific/extra (1,0) then (1,1) only. The composition
  row's "container" field is read only for its echelon rank (VrfC2SimService.cs:3181-3184). No Vrf:* setting overrides
  the choice.
- The leaf units ran Move_To_Location_Plan_Path in L2's B arms (L2:35-37). Their Stryker / Mech CO classification is
  ASSUMED there from member names (L2:99-100).
- The populate SOURCE: CompositionResolver.Resolve takes the created template's OWN configured subordinates ahead of
  the authored table (ContainerComposition.cs:523-525), for the CREATED type's entry (VrfC2SimService.cs:3265-3266).
  "BDE Airborne PA" configures 0 subordinates (AGGREGATE_CATALOGUE_2026-09-27.csv:65), so with it the authored row
  F-UCI-H still populates the 17 units. That is from the catalogue census, not run.

Readings (separate from the measurements):
- The first draft's "R-a's nested composition cannot exercise Move (Group)" is WITHDRAWN. There is no nesting at
  runtime, so no composition change is needed, and none is made.
- The one change arm G needs is the CONTAINER TYPE: an enabler-carrying brigade PA instead of BDE Light Infantry.
  Landing 11:1:-1:8:4:1:111 is not reachable through config; it needs a code change.

THE CHANGE, the same in BOTH arms (so the one variable stays Vrf:AggregateMovePlanner):
- Container type: BDE Airborne PA, 11:1:-1:8:4:1:111 ("BDE Airborne PA.entity":13 enabler, :3 PseudoAggregate.ope).
  - Chosen over Air Assault because it carries no second enabler. Echelon BDE, as R-a's.
  - It differs from R-a's container in subcategory 4 / extra 111 (MechanizedInfantry, airborne) against 3 / 1. That
    changes only the container's own symbol.
  - The pin is logged as fidelity Exact, with an OVERRIDE flag on its TYPE MAP line. It is not reported as a proxy
    substitution, because it is the run's declared condition, the same in both arms. A proxy tag would also change the
    unit's marking.
  - In the run's init (R-a's synthetic one), exactly ONE unit maps to F-UCI-H, the test brigade, so the pin applies to
    it alone.
- Mechanism: a CODE change. Vrf:ContainerTypeOverride (ContainerTypeOverride.cs; default empty = OFF), set for the
  run to "F-UCI-H=8:4:1:111". It has its own self-test (--populate-selftest p18, plus an OFF-identity check in p7), a
  reviewed diff, and a build before the run.
  - Constraint: Choose lands BDE Light Infantry at branch 3, (1,1), before any branch-4 candidate. Adding (1,111) to
    its SpecificExtra list therefore cannot reach 8:4:1:111; the override must pin the type AHEAD of the candidate
    loop.
- Composition: UNCHANGED (17 leaf units, flattened as today).
- G0c gains:
  - the override self-test PASSes, and its dirty control fails;
  - --populate-selftest and tools/aggregate/composition_check.py PASS on the build;
  - with the override, R-a's brigade is chosen as 3:11:1:225:8:4:1:111, resolves to "BDE Airborne PA", RoleOfPlatform =
    Container, and populates with 17 direct units, with the populate source the authored row F-UCI-H, not CATALOGUE.
    Without it, R-a's brigade is unchanged.
  - Dirty control (p18): the init rule's loop with (1,111) ADDED to its specific/extra list, first or last, still lands
    8:3:1:1. The pin must come ahead of the loop.
  - ON probe (p7, box): pinning F-UCI-H lands every F-UCI-H unit on 'BDE Airborne PA' and leaves every other unit's
    decision unchanged.
  - A pinned key that is no aggregate row of the type map refuses to start (a typo would otherwise pin nothing).

RISKS, each with its own falsifier:
- R1, NO VENDOR SAMPLE of Move (Group) on a container (G0b: Road to Kaunas gives its containers group-attack-to-objective
  only). Falsifier R1-F: under G, move 1 shows no member Move_To_Location_Plan_Path start within 300 WALL s, or the
  container's task fails at once. Then G is VOID (sec 3 glue); the reading is "Move (Group) did not run on this
  container", not a stop result.
- R3, the container type differs from R-a's and L2's (enabler present, PA platform, airborne branch). Falsifier R3-F:
  P stops on the new type while it did not in L2. F3 then fires, and the run records that P differs from L2 in
  container type, build, -NoGui and Debug logging.
- R4, the list-based reading is ASSUMED (above). Falsifiers R4-F, either of:
  - (a) under G, the CONTAINER itself shows a Move_To_Location_Plan_Path or move-to-location task;
  - (b) a member shows a task id outside its own lists (AggregateLevelAggregate.ope plus its *-aggregated-movement.sysdef).
  Either is recorded verbatim (counts and ids only) and marks the list reading as incomplete. Neither voids the run.

HYPOTHESIS, recorded as one, not as a cause:
- The enabler gap on our containers was found on 2026-10-15 (HANDOFF_2026-10-15:33-35). RL-20260928-03 was ruled
  2026-09-28 with Move (Group) BELIEVED available on containers (FINDING_AGGREGATE_MOVEMENT_OBSTACLES:20, :167-168), on
  the roads-per-leg question (RULINGS_ARCHIVE:558-567).
- So the gap was unknown when M3 was ruled, and it does not bear on that ruling's stated basis.
- Whether it bears on M3's choice of planner is open. Nothing here links it to the back-end stop.

STALE SITE (CLAUDE.md sec 4): PlannedMoveSelfTest.cs:607 still says "Move (Group) runs on them by its metadata". It is
corrected in the same commit as this amendment.

## 2. The run (one session, two fresh launches, the control first)

- Recipe: REPRO_BACKEND_STOP R-a (make_backend_stop_repro.py). That is one synthetic brigade container of 17 members,
  CONSTR then STREND ENGAGE to the same destination, inside today's demo extent (Vrf:DemoExtent ON, as shipped). The
  Demo overlay's console level stays at -1 (appsettings.Demo.json:104), as R-a and L2 ran it (PREREG_L1_L2:115-117).
- ONE VARIABLE: Vrf:AggregateMovePlanner, set by the environment for the interface process only.
  - Arm P (control): PlanPath, the L2 B configuration.
  - Arm G: Group.
  - The sec 1c type (BDE Airborne PA) applies to BOTH arms.
- DEVIATIONS FROM R-a / L2, the same in both arms:
  - -NoGui;
  - the container type BDE Airborne PA, through the sec 1c code override (default off);
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
