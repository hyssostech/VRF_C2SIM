# PREREG STP-1296 STEP 1 (D231) - MAK's Road to Kaunas, AS SHIPPED, back end only, NO interface (2026-10-20)

STATUS: PREREG ONLY. NOT RUN. Written before any launch. Operator ruling D231 (relayed by the STP-1192 supervisor,
2026-10-20 ~13:10Z): re-aim STP-1296 at its purpose. The question: does the vendor's way of moving units (Road to
Kaunas) avoid the D36 large-area paging stall, so that the full Iron Storm can run? Step 1 runs the shipped sample, as
shipped, on this box. Step 2 (the interface issues group-attack-to-objective; the D36(b) re-run) comes only if step 1 is
clean. Supersedes, as a run, the arm-G re-run (D230) and c14919 amendment 2; their gate, sampler and STOP fixes carry
over. This is the never-run L1 (PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md:31-59), revised; sec 5 lists every change.
Measurements and design implications are kept in separate sentences (CLAUDE.md sec 4).

## 0. What the run can and cannot answer

- CAN: whether MAK's own aggregate scenario, on its own area (sec 0, AREA), its own terrain (MAK Earth Aggregate
  online), its own SMS (AggregateTacticalLevel) and its own frame mode, shows the D36(b) signature on this box: a
  back-end loss, a working set climbing at more than 200 MB/min, feature-queue waits of minutes.
- CANNOT, by design: say anything about our interface's tasking (nothing of ours runs), or about the Iron Storm area.
- AREA: three numbers, all measurements, none of them the paging footprint.
  - PLACEMENT (box read B2, docs/experiments/stp1296/LD-1296-R1_BOX_READ_2026-10-20.md): the base RoadToKaunas places
    its units over lat 53.05-55.88, lon 17.11-27.58, 677.5 x 313.6 km, about 212,000 km2 (about 222,000 with points
    and routes). A rectangle, like every area figure in this section. RECORD FIX (2026-10-20, supervisor): 3,300 km2
    is the option-3 demo-extent rectangle, not a scenario size (SCOPE_DEMO_EXTENT_2026-10-14.md:23); this site first
    compared the placement box with it ("about 64 times"), which compares two rectangles and says nothing about load.
  - MANEUVER ROUTES: the six routes its maneuver tasks name, in the SAME base file, lie in 54.770-54.892 N,
    24.680-24.820 E, about 122 km2 (FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:133-138); also a rectangle.
  - MOVER FOOTPRINT: where the 35 plan-based movers actually go is UNMEASURED. The run measures it from the POS lines
    (the lat/lon box of the objects that move more than 500 m, and their total distance).
  - Extent is not paging load: the sim pages by object need and prediction (UG52 63.6.1 p1247), so what loads it is
    where units sit and move, not a bounding box. The movers are not like-for-like either: D36(b) had four
    container navigates in flight (D36:89-91), one of them 48 IBCT with 17 members (D36:16-18; the others' member
    counts are not in that record); RoadToKaunas has 17 disaggregated containers
    (AGGREGATE_CATALOGUE_2026-09-27.md:194-196), and the composition of its 35 movers is unmeasured.
  - Implication (separate sentence): a clean step 1 is reported as "ran clean with units placed over a 212,000 km2
    box and N movers covering M km2 / D km (POS)", with N, M and D measured. Whether that bears on Iron Storm is
    decided by the like-for-like rule of amendment B (path km, concurrent ground movers, corridor area), not by any
    rectangle. It does not show that our tasking would run clean; that is
    step 2.

## 0a. AMENDMENT A (2026-10-20, from box reads B1-B4, LD-1296-R1; before any run, nothing launched)

Record: docs/experiments/stp1296/LD-1296-R1_BOX_READ_2026-10-20.md (the dispatcher's read, counts only).

Measured by the dispatcher (read-only, counts only; the scenario files are vendor samples, not customer data):
- B1: all three .scn use MAK Earth Aggregate (online).mtf and AggregateTacticalLevel.sms, variable-frame 0.1 s,
  run-duration 0. time-multiplier (:13): RoadToKaunas 1, PhaseTwo 15, Counterattack 15. sim-time (:19): 0 / 12,565 /
  32,232 s. No paused/running start key exists in any member.
- B4: RoadToKaunas is a CLEAN t=0 file: 0 is-tasked, every plan-execution-stack empty, all 170 sync-matrix entries
  (started False). PhaseTwo and Counterattack are SAVED MID-RUN: is-tasked True 118 / 124, plan stack entries
  104 / 100, sync-matrix entries started 128 / 175.
- B3: plan-based movers (strict / broad), union of own plans, sync matrices and SimCommands: RoadToKaunas 35 / 41,
  PhaseTwo 36 / 44, Counterattack 37 / 45. Units 108 / 124 / 131; .oob units that are orbat parents 17 / 17 / 20.
- group-attack-to-objective parameters: fireSupportUnit, objective, route (simulationobject) and leadingElements
  (simulationobjects, a LIST of length 1-3). Task nodes, group / unit attack-to-objective: 21 / 25 in RoadToKaunas.
- B2: see sec 0, AREA.
- B4: cache\vrfsim 32,301 files / 546.45 MB; fastForwardSettings.mtl holds no entry; NotifyLevel is a command-line
  argument (--notifyLevel), with no C:\MAK write.

Change (the scenario):
- RUN THE BASE RoadToKaunas, not PhaseTwo. Reason, from D231's own terms: "use no saved state that resumes mid-task (or
  declare it)". RoadToKaunas is the only one of the three with no saved state at all, and it runs at the shipped
  multiplier 1. PhaseTwo would resume 118 in-flight tasks at 15x from sim-time 12,565 s. The planned-mover count is
  about the same (35 vs 36 strict).
- RECORD DISCREPANCY, not reconciled: G0b (PREREG_STP1296_GROUP_TASK_2026-10-19.md:130-132) gave RoadToKaunas 0
  container group-attack plans; B3 counts 21 group-attack task nodes in RoadToKaunas. They may count different things
  (plans owned by containers vs task nodes anywhere, including sync matrices). Step 1 does not depend on it; step 2's
  PREREG must reconcile it.
- PhaseTwo's 15x multiplier also contradicts sec 2's first text ("time multiplier 1.0"). That text is corrected below.

## 0b. AMENDMENT B (2026-10-20, from box read B3b = LD-1296-R2; before any run, nothing launched)

Record: docs/experiments/stp1296/LD-1296-R2_BOX_READ_2026-10-20.md (the dispatcher's read, counts only).
Measured (base RoadToKaunas.scnx):
- Vendor docs, as read on the box: help Plans\PlansIntro.htm "When you open a scenario, it is in pause mode ... When
  you start the simulation, the plans start executing"; GlobalPlansQuickLaunch.htm, a quick-launch global plan "does not
  start executing until you launch it"; plan.h:95-100 QuickLaunchFlags (IsQuickLaunch 0x1, Launched 0x4);
  vrf_synchronizationMatrixFunction.htm, each phase ends on its Phase End Condition, then the next begins.
- What starts at run(): 8 unit plans whose first Task runs at start (e.g. automatic_air_defense, patrol-route; their
  domains are NOT read). Of 54 sync-matrix phase-0 entries, 1 has an immediate task and 53 are Set-only ("phase 0
  starts at run" is INFERRED in R2). 2 plans start after a 3,600 s wait.
- What does not start by itself: 19 MANUAL quick-launch global plans (flags 3, not Launched) carrying 48 SimCommands
  (scenario-event 5, issue-plan 13, task-command 8, ...); 105 sync-matrix entries in phases 1-6, which start only when
  the previous phase ends. Every Init phase ends on SyncMatrixPlansCompleted AND a ScenarioEvent. Scenario events come
  from the manual quick-launch bodies or from When triggers (29 SimCommands inside When; conditions LeftOfLine,
  EntInArea, Destroyed, ...). The only sim-time condition in the file is "SimTime > 300 s" OR a LeftOfLine (one entry).
- Movers' footprint (routes + destinations + placements of the 35-37 movers): about 99,800 km2; routes and
  destinations alone about 92,100 km2; destinations alone about 30,750 km2. R2 names air units and air routes at the
  box edges (unmarked in R2 as VERIFIED or INFERRED); a ground-only box was not computed.

Implications (separate sentences; no cause claim):
- At run() with nobody launching plans (no GUI, -NoGui), the ground maneuver in phases 1-6 very likely does not start
  within the window; what moves is mostly the 8 start-at-run plans. INFERRED from the plan structure; the run
  measures it.
- Q4 as first registered (any 5 objects over 500 m) could then PASS on air movers alone. How much an air mover loads
  terrain paging is unmeasured (the sim pages by object need, aircraft included; UG52 63.6.1 p1247), but it is not the
  ground-maneuver load D36(b) failed on. Q4 is narrowed below, BEFORE the run.

Changes:
- Q4 counts GROUND movers only. GROUND = the entity-type DOMAIN in the .oob is land (the DIS domain of the type,
  the rule CLAUDE.md sec 3 sets for "ground"). Box read B3c supplies the domain of every object (uuid -> domain)
  before scoring; without it Q4 is NOT SCORED (no altitude fallback).
- WHY NOT ALTITUDE: POS altM is WGS-84 ELLIPSOID height (VrfFacade.cpp:1439-1444, DtNoGeoid; VRF_ALTITUDE_FRAMES.md
  :33-39), not MSL; the routes box holds terrain to about 345 m (the Minsk Upland), and a helicopter flying low reads
  under 400 m. Each mover's altitude range is RECORDED, not used to classify.
- Air movers are recorded (count, distance) but do not count toward Q4.
- Q4 levels, all measurement statements:
  - VOID: no ground object moves more than 500 m by T0 + 300 s, while Q1 holds and the clock runs;
  - UNINFORMATIVE: 1-4 ground objects move more than 500 m in the window;
  - LOAD FLOOR MET: at least 4 ground units each move more than 5 km (POS) in the window - the scale of D36(b)'s
    four navigating containers;
  - in between (at least 5 over 500 m, floor not met): Q4 PASSES as registered, at a load below D36(b)'s.
- Prediction for Q4 changes from MEDIUM-LOW to LOW; the expected outcome is VOID or UNINFORMATIVE.
- What the run still yields if so (measurements, not a verdict on D36(b)): a Q1 / Q3 idle-plus-start-at-run baseline
  with units placed over 212,000 km2; the queue waits at that load; and instrument validation (--report-simtime,
  POS on aggregate-level units, whether plan-driven tasks produce TSK lines).
- Implication of VOID / UNINFORMATIVE (separate sentence): the vendor's ground maneuver here is driven by manual
  quick-launch and phase-end events this run does not fire, so the run does not test it. Firing them remotely is a
  new tool and a deviation from "as shipped"; it needs the operator's ok and its own amendment.
- The footprint M is reported as both the R2 planned box and the POS-measured ground box.

## 0c. AMENDMENT C (operator D236, 2026-10-20; before any run, nothing launched) - an OBSERVATION run of PhaseTwo

Ruling D236 (relayed by the supervisor): "Start by running the Kaunas sample so we can see how a proper plan behaves.
Then apply the lessons to our code." This amendment SUPERSEDES amendment A's scenario choice and amendment B's Q4 levels
for THIS run. Amendment B (sec 0b) and the remote quick-launch run on the base file become the NEXT step.

What runs:
- RoadToKaunasPhaseTwo AS SHIPPED (the first text's choice): -Scenario
  Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunasPhaseTwo\RoadToKaunasPhaseTwo.
  Then RoadToKaunasCounterattack the same way, in a separate launch, ONLY if PhaseTwo ends on IDLE with no safety STOP.
- SAVED STATE, DECLARED (D231 "or declare it"): PhaseTwo is a mid-run save - sim-time 12,565 s, 118 tasks in flight,
  104 plan-stack entries, 128 sync-matrix entries started (LD-1296-R1, B1/B4). It resumes them on run(); that is the
  point of this run (the ground maneuver and group-attack tasks are already in progress, so no launch tool is needed).
  Counterattack: sim-time 32,232 s, 124 tasks in flight.
- MULTIPLIER: the file's own saved 15 (B1). RunSim sends run() only (-Multiplier 0); RunSim sets a multiplier only if
  > 1 (tools/RunSim/Program.cs), so the saved 15 stands. Running at 1x would need a SetTimeMultiplier(1) call that no
  tool here sends - itself a change to the file as shipped. D235 ruling (Fable cold review, 2026-10-20): 15x
  QUALIFIES - it drives 15 times the paging and planning per wall second, and a frozen back end shows as the clock
  falling from 15x to the controller's 1x extrapolation (D36 (a)), which a 1x run cannot show.
  - That a -NoGui back end honours the .scn's saved multiplier is UNMEASURED. The script reports the mean SIM/WALL and
    classifies it, as separate results: about 1x = NOT APPLIED (the saved 15 did not take effect; the run is then a 1x
    run, declared as such); 1.5-13.5 = LOAD SHORTFALL (recorded; not a failure of the vendor method); >= 13.5 = at the
    saved multiplier. The 600 s idle interval means about 150 sim min at 15x and 10 sim min at 1x; the result states
    which applied.
- NO FIXED WINDOW (D234): the run ends on IDLE - no GROUND object moves more than 100 m over 600 WALL s (POS) - or on a
  SAFETY STOP (memory, back end lost, the samplers, a guard block), or by the operator (below). Exit codes: 0 IDLE,
  7 STOP, 8 OPERATOR END.
  - [REFUTED by B5 for PhaseTwo - see sec 0d: its air routes go down to 118.5 m; the idle test uses the DIS domain.]
    First text: "GROUND for the idle test = latest POS altitude <= 1,500 m. Every air route of the scenario lies at
    3,000-8,000 m above the ellipsoid (LD-1296-R3 sec 4, read on the base file; INFERRED to hold for PhaseTwo)." 
  - IDLE is declared ONLY with the instruments alive over the interval: the observer process runs, the back end
    process runs, new '# t=' samples arrived, backends= reads 1, sim= advanced, AND the back end's vendor log was
    written within the interval. A back end that freezes in place is NOT a frozen clock: it keeps backends=1
    (BackendCount keeps a timed-out back end; VrfFacade.h:392-399) and its sim= advances at the controller's 1x
    extrapolation (D36 (a)). So the vendor log's freshness is the liveness test, and a clock that falls from >= 13.5x
    to below 1.5x in an interval is also a STOP. A dead observer, a dead, lost or silent back end, or a clock drop is a
    STOP, never IDLE (Fable's probes showed both earlier drafts would have read IDLE).
  - OWED ON THE BOX, at the first idle check of the run: the vendor log's LastWriteTimeUtc advances while the back end
    runs (NotifyLevel 4). If a healthy scenario wrote nothing for 600 s, the stale-log test would STOP it - a false
    STOP, which is safe (it never authorises Counterattack), and is reported as such. There is NO ceiling (supervisor, 2026-10-20:
  "no artificial time limits"). Every 4 h the script writes a CHECKPOINT (state; c15-checkpoints.txt) and keeps running;
  only the OPERATOR may end the run there, by creating OPERATOR-END.txt in the run folder. The instruments (holder,
  samplers, observer) are started for 24 h, the holder's maximum (LaunchVrf52 0..86400). The script warns once, 2 h
  before the earliest-started instrument (the system sampler) ends, because its ending is a STOP; each checkpoint logs
  sim=, backends=, the TSK count and the back end's working set. The scenario's own last phase ends on NOT True (never; R2), so IDLE
  is the only completion signal available; that is declared. That reading is of the BASE file's matrices (R2); for
  PhaseTwo only counts were read (R1 B4), so it is INFERRED there.
- -NoGui, NotifyLevel 4, no interface, no order, the S1-1 fix (ff_check.ps1, commit 8e3a835) in force; its self-test
  passed on a byte-identical copy of the box's fastForwardSettings.mtl (old rule entry=True, new rule entry=False).
- COUNTERATTACK runs only if PhaseTwo ends with exit 0 (IDLE, instruments alive). Its save is at sim-time 32,232 s,
  which by the sample's guide timeline is INFERRED to be after the scenario's end state, so it may have little left to
  play; that is recorded, not a reason to skip it.

What is recorded (OBSERVATION; no pass/fail on Iron Storm in this run):
- How the vendor plan moves, from the sources we have:
  - TSK lines (task type per completion, in order; whether plan-driven tasks report at all is itself recorded);
    the TSK count is written at every checkpoint and at the end;
  - the vendor log at level 4, COUNTS ONLY: per task-type name, the count and first/last time it appears, in order of
    first appearance; feature_source lines. M5's planning-line patterns are NOT pre-named: M5 is EXPLORATORY in this run
    (patterns chosen after reading the log are labelled post hoc);
  - POS, GROUND vs AIR by the DIS domain of the type: per container, how many of its subordinates move at once (per
    minute); path km driven per object; whether a mover's track stays on its route's vertices (route following) or
    departs from them (planning).
  - OWED BEFORE SCORING (a read-only PhaseTwo box read, B5): uuid -> DIS domain for every object; uuid -> parent
    (the orbat, for "subordinates per container"); the vertices of every route a PhaseTwo mover is tasked on; and the
    task types of the 118 in-flight tasks. Without B5 those observation items are NOT SCORED (the run still records
    POS for them).
  - What is NOT observable here: which task a container issued to which member (no task-issue stream reaches an
    observer). Inferred only from timing, and labelled as inference.
- M1 path km driven (ground), M2 peak concurrent ground movers, M3 corridor area (path km x 2w, w = 1 and 5 km), M4 the
  cache\vrfsim delta, M5 planning-line and feature_source counts.
- The D36 safety numbers: Q1 (backends=, the vendor log's last write at the end), Q3 working-set slope per 5-min
  window, the queue n / median / p95, the amendment 2 memory figures; SIM/WALL.
- Q1 and Q3 stay SAFETY lines: a back-end loss or the samplers' ABORT ends the run (as before). They are not scored
  against Iron Storm here.

Record of what changes for this run: the scenario (PhaseTwo, then Counterattack), the multiplier (saved 15, conditional),
the end rule (IDLE / safety STOP, no window), and the deliverable (observation, no Iron Storm verdict). Ledger: five
numbers per launch (holder, retry, back end, RunSim, WatchVrf): PhaseTwo 5565-5569 (claimed, c0dfd34);
Counterattack needs 5 more, claimed before its own launch.

SUPERSEDED FOR THIS RUN (marked at each site): sec 0b's Q4 levels and sec 3's pass rule (Q1/Q3 are SAFETY lines here;
Q4 is not scored), sec 4's predictions (none are scored; no Iron Storm verdict), sec 2's scenario/multiplier/saved-
state text, sec 5's item list (this amendment is the change record), and sec 6's run line and ledger count.
Run (PhaseTwo): `pwsh -NoProfile -File docs\experiments\stp1296\rtk_run.ps1 -AppBase 5565 -Root <W> -Tag p2
-NonGroundUuidFile <W>\runs\launch52\stp1296\p2-nonground.txt` (amendment C1)
Run (Counterattack, only after a PhaseTwo exit 0, with 5 new numbers): `... -AppBase <N> -Root <W> -Tag ca
-Scenario Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunasCounterattack\RoadToKaunasCounterattack`

## 0d. AMENDMENT C1 (2026-10-20, from box read B5; before any run - the S1 launch line was refused, nothing started)

Measured (B5, stp-data b7ea2de runs/stp1296/step1/LD-1296-B5_PHASETWO_BOX_READ_2026-10-20.md, sec 3a): in PhaseTwo, 5
routes used by AIR (domain 2) movers lie at or below 1,500 m ellipsoid height, all MQ-27 routes; the lowest vertex is
118.5 m. PhaseTwo has 124 kind-11 objects: land 100, air 18, domain 9 x4, domain 11 x2. R3's 3,000 m air floor was
read on the BASE file; it does not hold for PhaseTwo.
Implication (separate sentence): sec 0c's altitude cut would count low air movers as ground and could hold the idle
test open (safe - it never authorises Counterattack - but it changes how the run ends).
Change: the idle test's GROUND is the DIS domain from B5 (the uuid forms match: WatchVrf's POS key is
"VRF_UUID:" + the 8-4-4-4-12 uuid, VrfFacade.cpp:312; the file may give a bare, braced or VRF_UUID: uuid). rtk_run.ps1 -NonGroundUuidFile <file> takes one uuid per
line for every kind-11 object whose domain is not 1 (24 in PhaseTwo), written by the dispatcher from B5 before the
launch; matching is on the uuid's hex digits. Each idle check prints the ground / non-ground counts; if non-ground reads
0 while air objects exist, the uuid forms did not match: every object then counts as ground, a stricter test that
idles only when air movers also stop (safe). The UNSAFE direction is guarded: IDLE is judged only if at least one
ground object exists AND (with the file) at least one file uuid is seen among the reflected objects; otherwise the
check logs "idle not judged" and the run continues. Every file line must be exactly one uuid (32 hex digits) or the
script ABORTs naming the line. Without the file the script keeps the altitude cut and says so.
- GATE (added to sec 6 for this run): the file exists with 24 uuids, and the launch log shows "ground = NOT in the 24
  non-ground uuids". Before accepting, the dispatcher checks a recent 5.2d c13-watch.txt: POS keys start with
  "VRF_UUID:".

## 0e. AMENDMENT C3 (operator D239, 2026-10-20; before the re-run) - is the back-end climb the vendor's or ours?

Measured (S1, LD-1296-S1; stp-data 24feee1 runs/stp1296/step1/p2, LDS1-REPORT.md): PhaseTwo as shipped, 15x
(SIM/WALL 14.98), stopped on the MEMORY STOP at T0 + 49 s; the back end's working set went 3,220 -> 8,613 MB, peak climb
3,922 MB/min, already rising before T0; backends stayed 1; 0 TSK lines; the back end was forced 35 s after the STOP.
Change - re-run PhaseTwo as shipped with two of OUR additions removed, everything else the same (RunSim run(), the
RtiProbe holder, the RID file, the saved 15x, -NoGui, both samplers, the gate at 10.5 GiB):
- the back end's notify level = the vendor default 2 (rtk_run.ps1 -NotifyLevel 2; our runs used 4);
- NO WatchVrf observer (rtk_run.ps1 -NoWatch).
- A MEMORY STOP (the system sampler's marker, or ws_sampler2's ABORT) forces our own back end AT ONCE, after the same
  pid + start-time identity check StopVrf52 uses (RunnerLib Test-OwnBackendIdentity); no graceful attempt. Other
  STOPs keep the graceful-then-force path.
Pre-stated reading (measurement statements; the script prints both slopes at T0 + 65 s):
- the back end's WS slope over the first 60 s after T0, against S1's (peak 3,922 MB/min), with the slope over the 60 s
  before T0 printed beside it, because S1's climb began before T0;
- SIMILAR (within a factor of 2 of S1's first-60-s slope, from S1's c11 CSV): the climb is the vendor's on this box at
  15x, without our observer and at the vendor's notify level;
- MUCH LOWER (under half): our additions are implicated (the observer or the level-4 logging; the next split);
- in between: no reading; recorded.
- S1's WS was ALREADY RISING BEFORE T0 (paused, after load). So the script also prints the slope from READY to T0, and
  the slope over the 60 s before T0, each read against S1's same interval from its c11 CSV. READY -> run() is kept as
  in S1 (observer start + 20 s, then RunSim; under -NoWatch the observer wait is skipped, so READY -> T0 is SHORTER -
  declared, and the comparison uses MB/min, not MB).
Implication of each is for the operator; no cause is claimed here.
Ends: a MEMORY STOP (or any STOP); or the WS stays flat for 600 s (max - min < 100 MB) - reported once as ANSWERED, and
the run then continues as an observation until the operator ends it (OPERATOR-END.txt); or the operator. There is no
IDLE end under -NoWatch (no POS).
Also added / fixed:
- a vendor-log freshness sample every 5 s (c16-vlog.csv: now, the log's last write, its bytes). S1 measured the
  vendor log's last write 24 s BEFORE T0 and 73 s before the STOP, while WatchVrf saw the clock advancing at about 15x
  and 3 threads near-saturated (a measurement; no claim).
- the top-3 thread CPU measure (S1 console: InvalidArgument x3, concatenated values) is rewritten as an explicit loop.
- PRE-LAUNCH GATE (dispatcher, read-only; RUNBOOK sec 0, the check after a force-stop): S1's back end (app 5567) was
  FORCED, and its holder RtiProbe 40160 was gone after the STOP while rtiexec's WS went 17 -> 75 MB. Before this launch,
  a COUNT-grep of the serving rtiexec log for 5567's join and its resign / lost-connection lines; after this launch,
  RUNBOOK sec 0 check (2): no new "Create Response" for this launch (the back end JOINED, it did not create). A failure =
  NOT RUN and RECOVER with tools/ResetVrf (RUNBOOK sec 8) on the operator's word. rtiexec is never touched.
- CacheSize now counts the junction's target (S1 read 0 files through appData\cache); the dispatcher's hand
reading stays the record of M4. Ledger: 5 new numbers.

## 1. Citations (CLAUDE.md sec 1, both kinds)

VENDOR:
- UG52 3.4.3 p122: variable-frame is the interactive mode; fixed-frame-run-to-complete is "suitable for distributed use
  only in time-managed HLA federations" (as quoted in PREREG_L5_FRAME_MODE_ADDENDUM_2026-10-15.md:9-16).
- UG52 63.6.1 p1247: the sim pages terrain by object need and prediction, and pages out by polygon count
  (PREREG_L1_L2_L5_DOCS_ADDENDUM_2026-10-15.md item (6)). AC p239: "Feature data is not cached" (VD-1:135-136).
- UG 7.6.1 p261: fastForwardSettings.mtl entries switch frame mode by play speed (addendum item (5)).
- UG Table 14 p233: frame-time is not enabled in variable-frame (addendum item (4)).
- RoadToKaunas.scn:3-4, 16-17, 22 (terrain, SMS, variable-frame 0.1 s; AGGREGATE_CATALOGUE_2026-09-27.md:220: time
  multiplier 1.0, run-duration 0). FEG p20: "in-scenario descriptions and quick launch events" (VD-1:94-96).
- vrfRemoteController.h:356 simTime(): "Returns the simulation time of the specified back end" (quoted at VrfFacade.h:463-466).

OWN RECORD:
- D36(b) (D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md:30-49, :99-118): the criteria P1-P4 mirrored in sec 3, the
  SCORED result (BACK END LOST 2 min after the order; 2.6 -> 14.3 GB, about 1,860 MB/min; queue median 73 s, p95 135 s,
  NotifyLevel 4), and its reading "the memory climb started at order receipt with one navigate out".
- D36 (a): a stopped back end's sim clock reads as advancing at exactly 1x (the controller extrapolates; D36:24, :69).
- L1 (never run): PhaseTwo has the most planned movers, 24 units with movement tasks (PREREG_L1:40-41; L1 line numbers here are after this branch's two fix notes at L1:34-39); its three
  predictions (:49-52) are kept in sec 4.
  CORRECTION (B3, amendment A): by the dispatcher's count PhaseTwo does not have the most; strict plan-based movers are
  RoadToKaunas 35, PhaseTwo 36, Counterattack 37 (L1's "24" used a different count, not reconciled).
- G0b (PREREG_STP1296_GROUP_TASK_2026-10-19.md:130-132): containers get group-attack-to-objective only - Counterattack
  5 plans + 9 saved states, PhaseTwo 3 + 5, RoadToKaunas 0.
- PhaseTwo's .oob saves in-flight tasks, e.g. a unit "(is-tasked True)" with move_to_waypoint_plan_path
  (EXTRACT_RoadToKaunasPhaseTwo_oob_2026-09-28.txt lines 31 and 59, which quote .oob lines 71986 and 72014).
- RunSim: "a scenario loaded via LaunchVrf starts PAUSED" (tools/RunSim/Program.cs:13; written on 5.0.2).
- c14963 and amendment 2 (stp1296_arm.ps1 header): the memory gate, the second sampler, back end first on a STOP.
- Road to Kaunas has never been launched on this box (STP1296 PREREG :308; no ledger line names it).

## 2. Setup (as shipped, declared)

[SUPERSEDED FOR THE AMENDMENT C RUN (PhaseTwo / Counterattack observation): see sec 0c. This section stands for
the base-file run, which is the NEXT step with amendment B.]

- SCENARIO (amendment A): the base RoadToKaunas. -Scenario repeats the basename,
  Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunas\RoadToKaunas, because LaunchVrf52
  appends .scnx and checks it exists (LaunchVrf52.ps1:811-812, :854-856). FIX AT ITS SITE: L1:33 omits the repeated
  basename and would fail that check. (First text: PhaseTwo, with a switch rule to Counterattack; withdrawn by
  amendment A, because both are saved mid-run.)
- SAVED STATE, DECLARED: none. RoadToKaunas is a t=0 file (B4). Its plans and sync matrices are bound at load and not
  started; R2 (sec 0b) shows most of the ground maneuver waits for quick-launch and phase-end events. The Q4 levels
  (sec 0b, sec 3) cover that.
- FRAME MODE, DECLARED: the scenario's own, variable-frame (frame-time 0.1 s is inert in that mode), time multiplier 1
  (RoadToKaunas.scn:13). RunSim sends run() with NO multiplier. Our Iron Storm runs use fixed-frame-run-to-complete;
  that difference is the vendor's and is part of "the vendor way". fastForwardSettings.mtl holds no entry (B4; the
  script ABORTs otherwise).
- CACHE, DECLARED: the run-owned appData C:\C2SIM\vrf-appdata-unattended\appData, as D36(b), whose cache\vrfsim is a
  junction to the vendor cache (NewVrfAppData52.ps1:43). The cache holds tiles from the Iron Storm runs; the Iron Storm AOI and
  D36(b)'s box lie INSIDE this scenario's unit placement box, so "cold" can hold only for where the movers go, and that
  is unmeasured. The junction is appData\cache (NewVrfAppData52.ps1:43), the vendor's own cache; its contents by area
  are unmeasured, and the files / MB recorded before and after cannot show area. Cache state: UNKNOWN for the mover
  footprint, declared as such.
- C:\MAK, DECLARED: the script writes nothing there. The scenario is never saved: there is no GUI and no save call.
  LaunchVrf52 copies the vendor log at load to runs\launch52\vrfSim_*.log (its READY snapshot; LaunchVrf52.ps1:825-827,
  :1449). That copy stays on the box: never attached, never committed. The run script copies nothing.
  The SIM itself writes its log to C:\MAK\logs and its terrain cache through the junction, as on every run
  (LaunchVrf52.ps1:23-25, :199-202; APPDATA_RELOCATION_2026-09-14.md:187-190).
- NotifyLevel 4, passed explicitly (-NotifyLevel 4). The queue measure is absent at level 3 (D36:32).
- NO GUI (-NoGui, the standing ruling of 10-16), NO interface, NO C2SIM server, NO order.

## 3. Measured, and the pass rule (mirrors D36(b) P1-P4)

[SUPERSEDED FOR THE AMENDMENT C RUN (PhaseTwo / Counterattack observation): see sec 0c. This section stands for
the base-file run, which is the NEXT step with amendment B.]

Instruments (docs/experiments/stp1296/rtk_run.ps1):
- WatchVrf 10 s samples with --report-backends and the NEW --report-simtime (sim=; tools/WatchVrf, this branch),
  plus its POS (positions) and TSK (task completions) lines;
- ws_sampler2.ps1 (back end, 1 Hz, from READY); sys_sampler.ps1 (system, about 1 Hz, from the gate);
- the vendor log, COUNTS ONLY, read in place after the close: last write, lines, feature_source lines, and the
  "queued for <s> seconds" n / median / p95. Never copied or quoted.
- top-3 thread CPU over 5 s at the window end; cache files / MB before and after.

T0 = RunSim's run(). Window = 1,200 WALL s (20 min) after T0.
PASS only if ALL hold; any miss = FAIL:
- Q1 (D36 P1, no back-end loss): WatchVrf's backends= never drops 1 -> 0 (the stop signature on record: 1 -> 0 about
  121 s after a stop, DESIGN_BACKEND_LIVENESS_2026-09-15.md:23), AND the vendor log's last write, read AT THE WINDOW
  END before any teardown, is within 60 s of it. The post-close stamp is recorded too, not scored.
- Q2 (D36 P2, the clock): RECORDED, NOT GATING - a declared departure. SIM/WALL per 60 s from the sim= and t= deltas.
  - D36 P2 was ">= 1.000" under fixed-frame-run-to-complete, where load slows sim time. At variable-frame and
    multiplier 1.0, sim time follows wall time and takes longer steps under load (PREREG_L5_FRAME_MODE_ADDENDUM:52),
    and a stopped back end also reads 1x (D36 (a)). So the ratio is insensitive to load and to a stop; it shows only
    whether the clock RUNS (a paused clock reads 0). It feeds the VOID test below, not the verdict.
- Q3 (D36 P3): the back end's working-set slope <= 200 MB/min over every 5-min window from T0 + 5 min to the window
  end (ws_sampler2's CSV).
- Q4 (D36 P4, movement happens; narrowed to GROUND movers by amendment B, sec 0b): at least 5 ground objects each
  move more than 500 m (POS) between T0 + 60 s and the window
  end. TSK completions are RECORDED, not gating: whether the back end reports completions of plan-driven tasks that
  no remote controller assigned is NOT IN RECORD (every TSK on record followed an interface-assigned task).
  - VOID instead of FAIL: no GROUND object moves more than 500 m by T0 + 300 s, while Q1 holds and the sim= clock runs
    (Q2 > 0). That is a measurement.
  - Implication (separate sentence): the first movements start later, or need a trigger or a quick-launch event; it
    is not evidence about paging. A re-run at the vendor's play speed (RunSim <N> 15, as the vendor's own PhaseTwo save
    was played) is a declared deviation that needs the operator's ok.
  - SIM-TIME COVERAGE, declared: 20 min at 1x is about 1,200 sim s of a scenario the vendor saved at 12,565 sim s
    (PhaseTwo) while playing at 15x. Whether anything starts within the first 1,200 sim s is NOT IN RECORD.
  - The script STOPs (no score) if the load is not confirmed ("scenario LOAD CONFIRMED", LaunchVrf52.ps1:1445) or
    RunSim does not exit 0 (tools/RunSim/Program.cs:143-152), so neither can be misread as VOID.

Recorded, not gating (as D36 recorded the queue); on a STOP the script still records the queue, the top-3 thread CPU
and the vendor log's last write before the stop:
- the feature queue n / median / p95, against D36(b) SCORED (73 / 135 s) and FILL (114 / 161 s);
- memory: the gate line, the sys CSV's minimum Available and when, the vmmem* and per-container figures, any WARN or
  STOP snapshot;
- GROUND and AIR separately (domain from B3c): the number of objects that moved more than 500 m and more than 5 km,
  their total POS distance and lat/lon box (the mover footprint, sec 0), each mover's altitude range, and TSK counts
  per task type. (Saved in-flight tasks: 0 by construction, B4.)

## 4. Predictions (L1's, unchanged; MEDIUM - the first run of a vendor sample on this box)

[SUPERSEDED FOR THE AMENDMENT C RUN (PhaseTwo / Counterattack observation): see sec 0c. This section stands for
the base-file run, which is the NEXT step with amendment B.]

L1 made them for PhaseTwo, not knowing it was a 15x mid-run save; they are kept, unchanged, for the base file.

- L1-a: no back-end loss in the 20 min (= Q1).
- L1-b: working-set slope <= 200 MB/min after the first 5 min (= Q3).
- L1-c: feature-queue median wait < 30 s. Recorded, not gating.
- New, for Q4: ground movement happens. LOW (amendment B: the ground maneuver waits for manual quick-launch and
  phase-end events this run does not fire; first text said MEDIUM-LOW).

Readings (each a measurement statement; implications follow in separate sentences):
- PASS with the LOAD FLOOR MET: MAK's own pattern ran 20 min on its own scenario with no D36(b) signature, with N
  ground movers over M km2 (POS).
  - Implication: step 2 may be registered (the operator's rule). The area numbers of sec 0 and the measured ground
    mover count and footprint travel with the verdict.
- PASS below the floor: Q1 / Q3 clean at load N ground movers over M km2.
  - Implication: no bearing on D36(b)'s load; the operator decides on step 2 or a quick-launch amendment.
- FAIL on Q1 or Q3: the D36(b) signature occurs with MAK's own pattern and frame mode, with nothing of ours running.
  - Implication: changing our tasking to the vendor's way does not avoid it; step 2 is not run, and the terrain levers
    (T2 sim-only buildings off, T4 terrainInterfaceConfig) come first (L1:57-58).
- VOID or UNINFORMATIVE (Q4): no reading about paging under ground maneuver; the residual yield of sec 0b is reported.
  - Implication: the operator decides between step 2 as registered, a quick-launch amendment, or park.

## 5. Changes from L1 as registered (each declared)

[SUPERSEDED FOR THE AMENDMENT C RUN (PhaseTwo / Counterattack observation): see sec 0c. This section stands for
the base-file run, which is the NEXT step with amendment B.]

1. -NoGui (L1:42 said GUI on): the 10-16 standing ruling and memory.
2. A clock start: L1 named no run() sender (L1:44). RunSim (tools/RunSim), no multiplier, one app number.
3. NotifyLevel 4 passed (L1's command ran at the default 3, where the queue measure has 0 lines; D36:32).
4. The scenario path repeats the basename (L1:33 would fail LaunchVrf52's check).
5. The holder is held 2,400 s (-FederationHoldSecs 2400), not the 900 s default, which is shorter than the window.
6. Liveness and the clock by WatchVrf --report-backends plus the new --report-simtime (L1 had liveness only).
7. Amendment 2's memory gate (>= 11.5 GiB Available), samplers, snapshots and back-end-first STOP.
8. The pass rule mirrors D36(b) (sec 3), as D231 asks. L1's predictions are kept as they were.
9. The scenario (amendment A): the t=0 base RoadToKaunas at its shipped multiplier 1, not PhaseTwo (a mid-run save
   at 15x).

## 6. Gates before the launch (any miss = NOT RUN)

[SUPERSEDED FOR THE AMENDMENT C RUN (PhaseTwo / Counterattack observation): see sec 0c. This section stands for
the base-file run, which is the NEXT step with amendment B.]

- B1-B4, box reads, read-only, no launch: B1, B2, B3, B4 DONE 2026-10-20 (LD-1296-R1, amendment A; B3b LD-1296-R2,
  amendment B):
  - B1: the .scn of each of the three scenarios: terrain, SMS, frame mode, the start state, any resume flag;
  - B2: the extent, lat/lon min/max over all objects and route vertices, in km2;
  - B3: per scenario, units / containers / planned movers, and which plans are trigger-gated;
  - B3b (DONE, LD-1296-R2): for the base RoadToKaunas, how many plans and sync-matrix
    entries start on run(), on a trigger, or at a sim time (and which sim times), and the lat/lon box of the 35
    movers' routes and destinations. It decides whether Q4 can score at 1x in 20 min and gives the footprint M.
  - B4: the saved in-flight task count per task type, and the cache and fastForwardSettings state.
  - B3c (OWED before SCORING, not before the launch; read-only): the entity-type domain of every RoadToKaunas object
    (uuid -> domain), and the minimum vertex altitude of its air routes. Without it Q4 is NOT SCORED.
- Build: WatchVrf with --report-simtime (`WatchVrf.exe --capabilities` lists report-simtime; the script checks) and
  RunSim, both Release-5.2. An offline check: `WatchVrf.exe --con-selftest` passes.
- The memory gate: >= 11.5 GiB Available. The dispatcher first stops sonar-local if docker stats shows it holding more
  than 0.5 GiB. stp-lt (9500-9599), 19575, the customer system container and c2sim-server-vrf are never touched.
- Ledger: 5 numbers from `*** NEXT FREE ***` (expected 5565-5569: holder, retry, back end, RunSim, WatchVrf), pushed
  before the first join, with the scenario choice (sec 2).
- After the STP-1292 observed run, on the supervisor's go.

STOP rules: as c14919 (the guard refuses a line; any RTI damage; never kill rtiexec / rtiForwarder / rtiAssistant;
StopVrf52 only, and the forced stop of our own back end only) and amendment 2 (MEMORY STOP below 6.0 GiB Available;
the back end first). No VR-Forces GUI and no ArcGIS Pro.

Run: `pwsh -NoProfile -File docs\experiments\stp1296\rtk_run.ps1 -AppBase <N> -Root <W>`; records to stp-data
runs/stp1296/step1/ (CSVs, the watch trace, snapshots, the gate file; vendor log counts only).

## 7. Registration lines (C-4 prereg lint, check 13d)

VENDOR CITATION: UG52 3.4.3 p122; UG52 63.6.1 p1247; AC p239; UG 7.6.1 p261; UG Table 14 p233; RoadToKaunas.scn:3-4,
16-17, 22; FEG p20; vrfRemoteController.h:356 simTime() (as quoted at VrfFacade.h:463-466).
OWN-RECORD CITATION: D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md :24, :30-49, :69, :99-118; DESIGN_BACKEND_LIVENESS_2026-09-15.md:23;
PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md :31-59; stp1296/LD-1296-R1_BOX_READ_2026-10-20.md; stp1296/LD-1296-R2_BOX_READ_2026-10-20.md;
VRF_ALTITUDE_FRAMES.md:33-39;
FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:133-138; AGGREGATE_CATALOGUE_2026-09-27.md:194-196; PREREG_L1_L2_L5_DOCS_ADDENDUM_2026-10-15.md items (4)-(6);
PREREG_STP1296_GROUP_TASK_2026-10-19.md :130-132, :308; EXTRACT_RoadToKaunasPhaseTwo_oob_2026-09-28.txt:31, :59;
FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:137-138; HANDOFF_PARK_2026-10-16.md:81; Jira STP-1296 c14963.
RUN KIND: movement
DEVIATION FROM RECORD: no "--pre-order-gate" and no "CONSOLE LEVEL: 4": no interface runs and no order is pushed.
