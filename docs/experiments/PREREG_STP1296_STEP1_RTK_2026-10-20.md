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
    and routes). That is about 64 times D36(b)'s 3,300 km2 demo box (HANDOFF_PARK_2026-10-16.md:81).
  - MANEUVER ROUTES: the six routes its maneuver tasks name, in the SAME base file, lie in 54.770-54.892 N,
    24.680-24.820 E, about 122 km2 (FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:133-138), 27 times SMALLER
    than D36(b)'s box.
  - MOVER FOOTPRINT: where the 35 plan-based movers actually go is UNMEASURED. The run measures it from the POS lines
    (the lat/lon box of the objects that move more than 500 m, and their total distance).
  - Extent is not paging load: the sim pages by object need and prediction (UG52 63.6.1 p1247), so what loads it is
    where units sit and move, not a bounding box. The movers are not like-for-like either: D36(b) moved four
    populated containers of about 17 members each (D36:16-18); RoadToKaunas has 17 disaggregated containers
    (AGGREGATE_CATALOGUE_2026-09-27.md:194-196), and the composition of its 35 movers is unmeasured.
  - Implication (separate sentence): a clean step 1 is reported as "ran clean with units placed over a 212,000 km2
    box and N movers covering M km2 / D km (POS)", with N, M and D measured. Whether that bears on Iron Storm depends
    on M against D36(b)'s box, and the verdict says so. It does not show that our tasking would run clean; that is
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
