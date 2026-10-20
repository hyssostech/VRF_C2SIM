# PREREG - Iron Storm cut A with the vendor's documented per-unit task (STP-1296 / STP-1192) - 2026-10-20 - PARKED, NOT RUN

PARKED, NOT RUN. The operator, 2026-10-20, relayed by the supervisor: "Park the vrf stuff once more when you get to a good
spot. No more runs for now". NO PROBE AND NO RUN until the operator approves this registration. The operator, 2026-10-20, as relayed by the
supervisor: "Fix Iron Storm and run it. If there are still issues, consider going back. No probes before I approve."
OPERATOR DECISIONS on this registration (2026-10-20, relayed, verbatim "new way only, checks OK, reduced first"):
(1) arm P (PlanPath) ONLY - no control arm; it is scored on its own merits: arrivals, no stops or stalls, goals reached;
(2) the two offline checks of sec 6 are approved (no VR-Forces launch; queued with the dispatcher as IS-OFFLINE-R, then cancelled at the park);
(3) cut A (reduced) first; the full Iron Storm after, if it goes well.
Drafted by Worker R from the record only.
- Cites: repo records [O]; vendor files as those records cite them [V]; our inference [A].
- Aggregates, echelons, counts and km only: Iron Storm derives from a customer scenario, so no unit name, place, coordinate
  or way id appears here. Tasks are T-numbers; performers are echelons.
- Measurements and implications are kept in separate sentences.
- Fable cold review 1: SURVIVES NO (2 BLOCKERs, 10 more). Review 2 of the revision: SURVIVES NO (3 BLOCKERs, 8 more):
  - the app logs no per-member goal and no PlanPath road line;
  - the vertex 2+ goals cannot be computed before the run.
  All are applied below: one LOG-ONLY code change (sec 3a), a scorer fork that reads only what that build logs, and
  pre-approval conditions for the seat on the box (sec 6).
- Review 3: SURVIVES NO (1 BLOCKER, 2 MAJOR, 6 MINOR).
  - The blocker: C2 as first written named a tile-fetching, goal-moving check (the LD-22a CheckSlot overload, not built) on
    the tick thread.
  - All findings are applied.
  - PARKED with a FINAL cold pass owed at resume (sec 6a).
Companion: docs/experiments/stp1296/ROADS_ROUTES_DOCS_2026-10-20.md (38f9127).

## 0. In plain words

Iron Storm's cut A is the only aggregate order this project has scored clean. G1-6 (2026-10-04) used the default planner,
Auto: 79 PASS / 0 FAIL / 18 RECORDED, 5 of 5 TASKCMPLT, n = 1 [O PREREG_IRONSTORM_AGG_G1-6_2026-10-04.md:228].
- Auto sends navigate-to-location to every member at every STP vertex. The vendor's metadata calls that a system subtask "not
  designed for end user use in scenarios" [V base\scripts\navigate-to-location.xml :17-18 via PREREG_L1_L2_DOCUMENTED_MOVE_
  2026-10-15.md:21].
- The vendor's documented per-unit task for aggregate-level units is Move_To_Location_Plan_Path ("Move to Location (Plan
  Along Roads)", "Used by aggregate level units") [V Move_To_Location_Plan_Path.xml :6, :18; UG52 35.5.11 p735; via
  FINDING_VENDOR_DOCS_VD1_2026-10-16.md:38-41 and the code comment at src/VrfC2SimApp/AggregateMovePlanner.cs:43-55].
- The vendor's own Move (Group) sends that task to each subordinate [V group_movement_simplified.lua :220-224, ibid.].
- WHAT WE BUILT (PlanPath, M1 + M2, 8ed7b6f): our interface sends that task, per member, per STP vertex, to a destination
  offset from the vertex the way Move (Group) offsets it.
  - The offset geometry is OUR re-implementation, with two declared inferences [O MemberOffsets.cs:13-14, :33].
  - The vendor's sample does not drive containers this way: its containers get group-attack-to-objective, and its units
    plan per unit [O PREREG_STP1296_GROUP_TASK_2026-10-19.md:130-132; LD-1296-R1:32-34].
  - So this is "the vendor's documented per-unit task, sent by us", not a copy of the sample.
This run tasks cut A with that planner, ONE arm (operator decision 1). G1-6's Auto pass is a different build (sec 3), so it
is context, not a control.

## 1. What the record holds

TASK CHOICE PER ECHELON, vendor vs ours:
- Model set: the highest TASKED echelon decides. Above BN -> AggregateTacticalLevel only. BN and below -> EntityLevel by
  default, overridable upward [O RULINGS_ARCHIVE.md:534-543, RL-20260927-06; ModelSetGuard.cs:219-262]. Cut A's highest
  tasked echelon is DIV -> aggregate.
- Entity level, not exercised by cut A [O RL-20260927-01; V UG52 30.22, 23.3 via CLAUDE.md sec 2 D1]:
  - a lone ground platform gets Move To per STP vertex;
  - a unit keeps the unit Move Along Route, whose members plan to each vertex.
- Aggregate level, vendor sample (Road to Kaunas; our reads of a vendor sample):
  - units plan per unit: move_to_*_plan_path, unit-attack and trailing-march [O FINDING_AGGREGATE_MOVEMENT_OBSTACLES_
    2026-09-28 sec 2-3; LD-1296-R1..R4];
  - containers get group-attack-to-objective, whose script sends its subordinates PA_Move_Along_Route [V
    group-attack-to-objective.lua:533 via DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md:21]. That it drives the AUTHORED route
    is [A], from the task's "route (simulationobject)" parameter [O LD-1296-R1:32-33];
  - the sample's six maneuver routes lie 100 % within 10 m of an OSM road, with 0 building crossings; median vertex spacing
    is 52 m over all 57 ground routes [O FINDING_AGGREGATE_MOVEMENT_OBSTACLES:135-138].
- Aggregate level, ours today (VrfC2SimService.cs:6415-6456):
  - a tasked container moves vertex by vertex, member by member, with Auto (RL-20260928-03);
  - a one-point task keeps PA_Move_To_Location_Direct, and a patrol keeps PA_Patrol_Route.
- Not available to us today:
  - group-attack-to-objective takes a leadingElements list, and the facade marshals no list variable [O VrfFacade.cpp:169-
    192 via FINDING_AGG:112-114; LD-1296-R1:32-33]. Whether the script REQUIRES the list is [A];
  - Move (Group) needs the ground-echelon-group-tactics enabler. Measured: the bare Aggregate Container and the 2 R-a row
    types lack it [O PREREG_STP1296_GROUP_TASK:122, :155-157]. Other container types: not measured.
- ROADS: no planning task needs roads; dense or obstacle-clear routes are nowhere documented as required [O
  ROADS_ROUTES_DOCS_2026-10-20.md sec 1].
OUTCOMES ON RECORD, per mechanism (n small; none is a ranking):
- Auto on cut A: G1-3 to G1-5 FAILED (an executor refusal, a scorer limb, the culvert rule). G1-6 PASSED, n = 1 [O
  PLAN_MOVEMENT_2026-09-27.md:83-85; G1-6 Result].
- Auto on the re-task recipe R-a (a second move while members are still moving): the back end stopped 5 of 6 times at that
  place, and again in L2 A1 [O REPRO_BACKEND_STOP_2026-10-15.md:34-42; L2_RESULT_2026-10-16.md:24-48].
- PlanPath on R-a [O L2_RESULT]:
  - L2 B1 and B2 ran clean to teardown; that is n = 2, not proof (:28-33, :47);
  - move 2 stalled with 6 of 17 members out, UNEXPLAINED (:75-80);
  - in B1, 4 of 17 offset goals were in OSM water (:153-173; HANDOFF_PARK_2026-10-16.md:92-97). LD-22a, the goal check,
    is NOT STARTED.
- PlanPath has never run on Iron Storm. Move (Group) has never run anywhere. group-attack-to-objective has never been issued
  by our interface.
- Scale: D36(b) on the option-3 rectangle (3,300 km2) was BACK END LOST about 2 min after the order, WS 2.6 -> 14.3 GB [O
  D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md:103-104]; 3 containers / 50 members were in flight at the scored loss [O
  LD-1296-R4:38-39].
- The vendor's own PhaseTwo with no memory limits (S3) reported a PLATEAU, with commit at 95-97 % of the limit for 66 min
  [O STP-1296 c14982; stp-data 6c49990].
[A] What the two planners share and where they differ on cut A:
- Auto chose MAK_ROAD on 6 of 6 legs [O G1-3:119, :281-286]. PlanPath's path query is the member movement system's,
  MAK_ROAD by default per the vendor Lua as read into a code comment (AggregateMovePlanner.cs:50-53), with no [O] record.
- They differ in the task (documented user task vs system subtask), in the destination (each member's offset goal vs the
  vertex) and in the 10 m obstacle buffer (Auto has it; PlanPath has none, per the same comment).
- For T14's 17-member BDE the INIT ring has r 490 m [O ContainerSelfTest.cs:19 (p6)]. The DESTINATION goals are computed
  at dispatch from the members' LIVE positions (MemberOffsets.For; VrfC2SimService.cs:3721-3726). So the init ring is an
  [A] proxy for the vertex-1 goals only.

## 2. Still OPEN (this run answers none of them)

1. Whether Auto stays the default: the owner's ruling on RL-20260928-03 is owed [O L2_RESULT:47-48; HANDOFF_PARK:74-78].
2. The back-end STOP on re-task (STP-1163) is unexplained; cut A carries no R-a re-task.
3. Road vs off-road frame: STP's routes are straight polylines [O IRONSTORM_CUTA_CHANGES.md:22-27]. Road-tracing them
   was struck by the owner [O CORRECTIONS_LOG.md:646-657].
4. group-attack-to-objective cannot be issued until the facade marshals a list. D231 step 2 waits on that [O
   PREREG_STP1296_STEP1_RTK_2026-10-20.md:6-7].
5. Scale beyond the demo extent (D112 / LV-2) [O HANDOFF_PARK:79-84].
6. G0b counted 0 container group-attack plans in the base file, but R1 B3 counts 21 group-attack task nodes [O
   LD-1296-R1:34]. Likely they sit in the manual quick-launch plan [O LD-1296-R3:30-31]. Not confirmed.

## 3. The run (registered, NOT RUN)

ORDER AND INIT: cut A as in G1-6: data/IRONSTORM_CUTA_Initialization.xml and data/IRONSTORM_CUTA_Order.xml, with shas
2000e856 / 7a986137 over the CRLF bytes [O IRONSTORM_CUTA_CHANGES.md:7-13]. An LF checkout hashes 2f2d05a1 / 523c8ce6;
the go-live check hashes the bytes as deployed on the box. 40 units; 5 tasks, 3 movers [O G1-3:279-286]:
- T02, DIV-HQ container: 1 vertex, 5,341 m, 1 member;
- T10, BN: 3 vertices, 2,771 m, 5 members;
- T14, BDE: 2 vertices, 4,172 m, 17 members.
The cut A order's start offsets (T10 PT20M, the rest PT0S or RelativeTime) pass through STP-850's receipt rebase with
minOffset 0. Under that rebase T10 keeps its 5 min at DurationScale 0.25 (declared; the order is unchanged).
ONE ARM (arm P), a pre-warm plus a scored run: G1-6's runner path and settings, plus --env Vrf__AggregateMovePlanner=PlanPath
on both.
PATH: G1-6's runner path [O G1-6 sec 0, Conditions, sec 3]:
- scripts/RunScenario.sh, DEMO_RUNBOOK sec 1 "Way A", headless --no-gui, consoles at 4;
- DurationScale 0.25 (--duration-scale 0.25); stall detection on (--env Vrf__StallDetection=true); successor-gate floor 600 s.
COMMAND LINE: pinned to G1-6's g1_6_runner.sh bytes (log names aside), with no Vrf__DemoExtent env.
- The runner reads appsettings.json, not the Demo profile (DEMO_RUNBOOK:292), and DemoExtent is OFF there [O
  appsettings.json:78]. The STP-1210 rule is therefore not exercised.
- DEMO_RUNBOOK sec 3's current Way A line DOES pass Vrf__DemoExtent (:289), so it is NOT the line used here.
- The app's start-up extent line is recorded for every launch.
BUILD: origin/feat/aggregate-documented-move (head 4f31863 at drafting), the branch L2 and LV-1 ran. main (d1730c8) does NOT
contain PlanPath (git grep, 0 hits). The go-live record states the deployed sha.
- Against G1-6's build 52f50e0, that branch carries 34 non-merge commits under src/ (46 files, +4,252 / -253). Named here:
  STP-850 start offsets, X9 RelativeTime, LBL labels, the demo extent (OFF here), STP-871 FOLSPT (a container keeps its
  geometry; T14 is FOLSPT), the D2 code maps, STP-1127 (no blanket SetTarget), STP-795 stream recovery, RL-20261011-02,
  M1 + M2 (PlanPath), and STP-1210. The BACK END LOST line (aab1142, STP-822) is in both builds.
- With no control arm (operator decision 1), nothing in this run is attributed to the planner as against Auto. Arm P
  differs from G1-6 in the build, the cache, the box state since D36 / L2 / S3, and the STP-850 rebase. The seat lists every
  commit (`git log --oneline 52f50e0..<deployed sha> -- src`) in the go-live record.
- The sim cache grows run by run (G1-6:256 21,807 files / 495.6 MB; D36:106 32,283 / 518.9 MB). Its file count and bytes
  are recorded before each launch.
MEMORY: no memory-based stop is armed. This follows the operator, directly, 2026-10-20 (VRF_C2SIM 78920a5: memory-based
stops OFF by default; failure-based stops kept).
- The runner path has no free-memory gate (RunC2SimScenario.ps1, RunnerLib, LaunchVrf52, StartInterface52: grep, 0 hits).
- The build branch is NOT 78920a5's branch: its RunC2SimScenario.ps1 still defaults -WsRunawayAbortAfter to 3 (:628).
  - That abort acts only on SampleThreads alerts. SampleThreads is off by default on this path (scripts/RunScenario.sh:109)
    and is not enabled, so the abort is inert.
  - The run line passes nothing for it either way; the go-live record shows the abort-rule plan line absent.
- Back-end WS and private bytes are RECORDED by ws_sampler2.ps1 from 78920a5, aborts at their default 0 (off). Its exit is
  ignored. The box's Available at T0 is recorded for each launch.
STOPS AND WHO ACTS (THE SEAT RUNS every step, RL-20260928-01; G1-6:133):
- back-end crash, or the process gone: the runner's BACK END CRASHED -> VOID path (RunC2SimScenario.ps1:1946-2003);
- a hung-but-joined back end (the R-a / D36 signature: process alive, vendor log silent).
  - The app declares BACK END LOST about 40 s after the silence (BackendLivenessSeconds 10 + BackendLossConfirmSeconds 30,
    appsettings.json:69-70).
  - Its ERROR line (VrfC2SimService.cs:9442) pushes TASKABRT for every in-flight and held task and abandons successors
    (:9449-9466), but it stops no process.
  - No runner script matches that line (grep, 0 hits). The SEAT therefore runs a tail on the app log for the run's
    duration (Get-Content -Wait | Select-String 'BACK END LOST'). On a match it starts the normal teardown (StopIface,
    then StopVrf) within 60 s. Outcome LOST;
- RTI damage (rtiexec or rtiForwarder gone; a federation the runner cannot join): the runner's own failure path;
- stp-lt unhealthy; the 2700 s cap; the operator.
SCORERS:
- arm P is scored by a declared PlanPath FORK, g1_7p_score.py, of G1-6's g1_6_score.py (sha256 de7bd755...) [O G1-6:234].
  The base lived in the seat's scratch (laneG1-6; G1-6:133) and is NOT in the repo (find, 0 hits); the seat confirms the
  file and its sha on the box (sec 6). The fork changes exactly these rules and nothing else:
  (a) the vertex-issue line it greps is "move_to_location_plan_path issued" (the script id const AggregateMovePlanner.cs:100 via ForVertex :203; printed by VertexLine :323-327), not
      "navigate-to-location issued";
  (b) the W-gate road limb ("every leg NEAR -> MAK_ROAD") is DROPPED for arm P. Under PlanPath the app logs no road line
      (VrfC2SimService.cs:3715-3716 logs it under Auto only). The vertex line's "useRoads true" is the app's own setting
      (ForVertex :200-204), not evidence of the query the sim ran; it is recorded, never scored;
  (c) H3's destination echo is compared with each member's OWN goal from sec 3a's PLANNED GOAL line, not with the vertex.
      This checks marshalling (app -> facade), not the goal the sim planned to. Rule (d) is the independent instrument.
      Which facade line prints the echo is to be named in the fork (not located under src/VrfC2SimApp by review 3);
  (d) W-DONE and H6 measure each member's arrival against its own PLANNED GOAL, within the same 50 m.
- The base and the fork are COMMITTED to the repo (docs/experiments/stp1296/) with their shas before approval.

## 3a. The code change this registration needs (LOG ONLY; no behaviour change)

C1. In IssuePlannedVertex's per-member offset branch (VrfC2SimService.cs:3719-3743 on the build branch), log one line per
member, ONLY when TryIssueMemberMovesEach's verdict is Ready. A non-Ready verdict issues nothing (ContainerPopulation.cs:
473-477) and throws at :3743. The line:
`PLANNED GOAL <container> vertex <k>/<n>: member <name> (<uuid>) -> (<lat F6>,<lon F6>)<, UNLOCATED -> the vertex>`.
- Name, uuid and the goal are in scope there (ctx.Members, offs.Destinations[m.Name], :3724-3728).
- The UNLOCATED flag is derived as goal == the vertex. MemberOffsets.Result carries only the Unlocated COUNT
  (MemberOffsets.cs:23-25).
- No other code path changes. A selftest pins the line's shape (fail-first).
- Today the app logs no per-member goal: L2_RESULT:158 records "The centre was not logged", and LD-21b had to recompute
  the goals offline.
- Without C1, rules (c) and (d) have no instrument, and arm P is NOT RUN.
C2. In the same place, a LOG-ONLY, CACHE-ONLY goal check on the tick thread. It uses the primitive the Auto road check
already uses there: OsmQuery.Point(RoadCache().Provider, goal, rules, buildingClearance) (Preflight/OsmFeatures.cs:1058).
Its OsmCacheReader never fetches; it returns OsmTile.Unknown for a tile not in the cache (Preflight/TileSource.cs:851-872;
VrfC2SimService.cs:8450-8452).
- NOT the LD-22a CheckSlot overload. That one moves goals, is not built, and the existing CheckSlot can fetch, so it is
  run only off the tick thread (PreflightService.cs:487-520, :663; VrfC2SimService.cs:3410).
- The line: `GOAL CHECK <container> vertex <k>: member <name> clear | IN WATER | ON BUILDING | UNVERIFIED (tile not cached)`.
  - Tiles under goals 400-500 m out are not guaranteed cached (HANDOFF_PARK:96; z14 tiles, OsmFeatures.cs:278).
  - UNVERIFIED is counted per vertex and excluded from the goal reading's denominator.
- RULES DECLARED: the check states which water clearance it applies. AggregateTacticalLevel's WaterCorridorMeters is 0
  (ModelSetRules.cs:74), while the slot check uses 10 m (RL-20261004-01). The scorer's "members inside water" uses the
  same definition, or the two are reported under separate names.
- It changes no goal, issues nothing and fetches nothing. It is needed because vertex 2+ goals depend on live positions
  and cannot be computed before the run (review 2, finding 3).
Both commits are made on the build branch, reviewed cold, built and deployed on the box before approval. Their shas go into
the amendment (sec 6).

## 4. Pre-stated readings (measurement statements; the implications are the operator's)

- PASS = the W gate prints "W GATE: PASS", and the scored run gives 0 FAIL on its scorer's rows: SL0-SL4,
  H1-H6, P-FALS(e) under the culvert rule, and V-CRASH [O G1-6:164]. 5 of 5 tasks must also close TASKCMPLT, with VACUOUS
  0 (tools/analysis/movement_check.py).
- FAIL = any FAIL row. Record the row, the task and the member count; no cause claim.
- VOID = a back-end crash (V-CRASH). LOST = BACK END LOST (sec 3). For either, record the time since the order reached the
  bus and the dispatches in flight.
- On its own merits (operator decision 1), RECORDED with every outcome: arrivals per member against its PLANNED GOAL, stops
  and stalls, goals reached, and:
  - the time to each TASKCMPLT;
  - per-member plan-failure lines, counted;
  - stalls (TASKABRT by the 360 SIM s watchdog);
  - members inside water or on a building;
  - back-end WS and private at the order and at the end;
  - SIM/WALL.
- GOAL CHECK beside each FAIL (arm P): a member whose GOAL CHECK reads IN WATER or ON BUILDING, and which then fails
  P-FALS(e), H5 or H6, is recorded as "FAIL (goal IN WATER / ON BUILDING)". It counts as FAIL. Whether the goal caused it
  is the operator's reading, not the scorer's.
- Arm P completion, both paths [A]:
  - D-6 first: the last vertex goes to D-6 (VrfC2SimService.cs:3776-3780). D-6 compares the CONTAINER's own reflected
    position with the route end (:3538-3540) and withholds when it is more than VertexArrivalRadiusMeters (100 m) away
    (:3533). No centroid is computed. Whether the vendor publishes the container at its members' centre is not on record.
    - If the container is near the route end, TASKCMPLT comes from the vendor's completion.
    - If not, D-6 withholds, and the arrival evidence decides.
  - Arrival evidence: more than half the members (ArrivalMemberFraction 0.5, VrfSettings.cs:301) within the arrival radius
    of the last vertex. The radius is min(500 m, 0.25 x route), and each member must pass a traversal bar of max(0.5 x
    route, floor) (ArrivalPolicy.cs:155-158, :165-168). For cut A's routes (5,341 / 2,771 / 4,172 m) the radius is 500 m.
    With T14's goals near a 490 m ring, 9 of 17 members stopping about 10 m outside their goals can leave T14 without
    TASKCMPLT.
  - RECORDED per task: D-6's line "CONTAINER ... COMPLETE with the container {D} m from the route end" or its WITHHELD line
    (:3552-3557), and the ArrivalPolicy decision line.
- G1-6 (Auto, an older build) is quoted beside the result as context only. A PASS on arm P does not rank the planners and
  does not retire Auto (sec 2 item 1).

## 5. Application numbers

Nothing is claimed by this registration. Two launches (pre-warm, scored) take G1-6's 11-number block each (G1-6 sec 5):
22 numbers, plus 4 for a day holder if one is needed. The dispatcher claims them from the Appendix B marker at the go-live;
it reads "*** NEXT FREE: 5580 ***" on origin/stp-1296-group-prereg (78920a5). An aborted
launch burns its whole block.

## 6. Before approval and before the go-live (each one a NOT-RUN condition; the seat does 2-4 on the box)

1. The operator approves this registration.
2. CODE (sec 3a): C1 and C2 committed on the build branch, cold-reviewed, built and deployed. Their selftests pass, fail-first.
3. SCORER INSTRUMENT. Confirm g1_6_score.py and its sha de7bd755 on the box; it must reproduce 79 / 0 / 18 on G1-6's scored
   run. Then run g1_7p_score.py --wgate and in full on:
   - L2 B1's run directory (app 5524, PlanPath, 17 members; L2_RESULT:22, :122) - it predates C1, so rules (c)/(d) must
     report "no PLANNED GOAL lines" there, not pass;
   - G1-6's scored run, as a control.
   Before approval, grep B1's app log for "roads " lines and for per-member destinations under the PLANNED MOVE lines;
   review 2 expects 0 and 0. List every limb that fails on line SHAPE. Record both shas and outputs in an amendment.
3b. OFFSET GOALS, vertex 1. Compute the vertex-1 goals offline from the init positions (MemberOffsets.For). This is [A]:
   members may move before dispatch. Check them against the pre-flight water and building tiles.
   - T14's last vertex was nudged 799 m out of a lake [O IRONSTORM_CUTA_CHANGES.md:157-168, :195-199]. A 490 m ring is
     smaller than 799 m, so whether any goal reaches that water is not yet measured.
   - The vertex-1 count goes into the amendment. Vertex 2+ are measured live by C2 (sec 4).
3c. COMPLETION PATH. grep L2 B1's app log (PlanPath, 17 members) for D-6's COMPLETE and WITHHELD lines, and record the
   container-to-route-end distance. That settles which completion path arm P takes.
4. BUILD LIST: the deployed sha and the commit list under src/ since 52f50e0 go into the go-live record.
5. c2sim-server-vrf is healthy (its restart, D106, is OPERATOR ONLY). rtiexec and rtiForwarder are up and untouched;
   stp-lt (9500-9599) and 19575 are never touched.
6. Fable cold-reviews the amendment from items 2-4 (C1/C2, the scorers, the goal counts) and the go-live record before the
   launch.

## 6a. OWED at resume (PARKED 2026-10-20)

- C1 and C2 (sec 3a): NOT written. Owed before approval.
- A FINAL Fable cold pass of this file: owed. Review 3's findings are applied, but the result has not been re-reviewed.
- Check 3c (the D-6 completion path on L2 B1's log): owed; added after IS-OFFLINE-R was queued.
- The seat's tail-watcher needs the app log FILE path the runner writes; it is named in the go-live record.
- The scorer fork g1_7p_score.py: NOT written. Owed.
- IS-OFFLINE-R (the sec 6 checks 3 and 3b): CANCELLED by the dispatcher at the park, NOT done; re-queue at resume. Its results go into an amendment.
- The full Iron Storm registration: after cut A, if it goes well (operator decision 3).

## 7. What this run does NOT claim

It does not test:
- the entity level;
- the re-task stop (STP-1163);
- scale;
- Move (Group) or group-attack-to-objective;
- the live STP COA-1 export.
The live STP path (Way B) waits on the STP 5.11.1 escape: LV-1b's order never reached the interface because of the server's
STOMP non-ASCII bug [O HANDOFF_PARK:29-71, :109-115]. Which planner that path uses is the owner's call (sec 2 item 1).
