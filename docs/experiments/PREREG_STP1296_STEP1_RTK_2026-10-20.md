# PREREG STP-1296 STEP 1 (D231) - MAK's Road to Kaunas, AS SHIPPED, back end only, NO interface (2026-10-20)

STATUS: PREREG ONLY. NOT RUN. Written before any launch. Operator ruling D231 (relayed by the STP-1192 supervisor,
2026-10-20 ~13:10Z): re-aim STP-1296 at its purpose. The question: does the vendor's way of moving units (Road to
Kaunas) avoid the D36 large-area paging stall, so that the full Iron Storm can run? Step 1 runs the shipped sample, as
shipped, on this box. Step 2 (the interface issues group-attack-to-objective; the D36(b) re-run) comes only if step 1 is
clean. Supersedes, as a run, the arm-G re-run (D230) and c14919 amendment 2; their gate, sampler and STOP fixes carry
over. This IS the never-run L1 (PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md:31-56), revised; sec 5 lists every change.
Measurements and design implications are kept in separate sentences (CLAUDE.md sec 4).

## 0. What the run can and cannot answer

- CAN: whether MAK's own aggregate scenario, on its own (small, see the caveat) area, its own terrain (MAK Earth Aggregate online), its own SMS
  (AggregateTacticalLevel) and its own frame mode, shows the D36(b) signature on this box: a back-end loss, a working
  set climbing at more than 200 MB/min, feature-queue waits of minutes.
- CANNOT, by design: say anything about our interface's tasking (nothing of ours runs), or about the Iron Storm area.
- AREA CAVEAT (gate B2). The only extent on record is the box of the six maneuver routes its tasks name, 54.770-54.892 N,
  24.680-24.820 E (FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:137-138), about 120 km2 (computed here, not
  recorded). D36(b) failed on 3,300 km2 (HANDOFF_PARK_2026-10-16.md:81). The playbox is whole-globe
  (AGGREGATE_PROFILE_OFFLINE_2026-09-27.md:115), so it bounds nothing. HANDOFF_2026-10-15:26's "an area covering Iron
  Storm's" is UNVERIFIED: the Iron Storm AOI is 53.94-54.12 N, 23.11-23.41 E (AGGREGATE_PROFILE_OFFLINE:99), south-west
  of those routes. Box read B2 measures the real extent before the run, and the result states it beside the verdict.
  - If B2 gives under 1,000 km2: a clean step 1 shows the vendor pattern is clean on a SMALLER area than D36(b)'s. It
    does not show that it carries 3,300 km2. That is a measurement statement.
  - Implication (separate sentence): in that case only step 2, on the D36(b) box, can answer the operator's question.

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
- L1 (never run): PhaseTwo has the most planned movers, 24 units with movement tasks (PREREG_L1:36-37; L1 line numbers here are after this branch's fix note at L1:34-36); its three
  predictions (:46-49) are kept in sec 4.
- G0b (PREREG_STP1296_GROUP_TASK_2026-10-19.md:130-132): containers get group-attack-to-objective only - Counterattack
  5 plans + 9 saved states, PhaseTwo 3 + 5, RoadToKaunas 0.
- PhaseTwo's .oob saves in-flight tasks, e.g. a unit "(is-tasked True)" with move_to_waypoint_plan_path
  (EXTRACT_RoadToKaunasPhaseTwo_oob_2026-09-28.txt lines 31 and 59, which quote .oob lines 71986 and 72014).
- RunSim: "a scenario loaded via LaunchVrf starts PAUSED" (tools/RunSim/Program.cs:13; written on 5.0.2).
- c14963 and amendment 2 (stp1296_arm.ps1 header): the memory gate, the second sampler, back end first on a STOP.
- Road to Kaunas has never been launched on this box (STP1296 PREREG :308; no ledger line names it).

## 2. Setup (as shipped, declared)

- SCENARIO: RoadToKaunasPhaseTwo (L1's choice: the most planned movers; containers carry group-attack-to-objective,
  the task step 2 would issue). -Scenario repeats the basename,
  Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunasPhaseTwo\RoadToKaunasPhaseTwo, because
  LaunchVrf52 appends .scnx and checks it exists (LaunchVrf52.ps1:811-812, :854-856). FIX AT ITS SITE: L1:33 omits the
  repeated basename and would fail that check.
  - Switch rule, decided BEFORE the run from box reads B3/B4 over ALL THREE scenarios (Counterattack, PhaseTwo and the
    base RoadToKaunas): if PhaseTwo's container group-attack plans are all trigger-gated (none starts on Run) and
    Counterattack has at least one that starts on Run, run Counterattack instead. The base RoadToKaunas carries no
    container group-attack (G0b), so it is not a candidate for step 2's purpose; its B3/B4 figures are recorded so
    the choice is shown, not asserted.
    The choice and its reason are written into the ledger line before the first join.
- SAVED STATE, DECLARED: PhaseTwo ships saved in-flight tasks (above). "As shipped" means they load as saved. They are
  not edited out. Box read B4 counts them (is-tasked True, per task type); the result reports that count. Whether they
  resume mid-task on load is NOT IN RECORD; WatchVrf's POS and TSK lines show it.
- FRAME MODE, DECLARED: the scenario's own, variable-frame (frame-time 0.1 s is inert in that mode), time multiplier 1.0.
  RunSim sends run() with NO multiplier. Our Iron Storm runs use fixed-frame-run-to-complete; that difference is the
  vendor's and is part of "the vendor way". fastForwardSettings.mtl must hold no entry (the script ABORTs otherwise).
- CACHE, DECLARED: the run-owned appData C:\C2SIM\vrf-appdata-unattended\appData, as D36(b), whose cache\vrfsim is a
  junction to the vendor cache (NewVrfAppData52.ps1:43). The cache holds tiles from the Iron Storm runs and none from
  this area, so it is COLD for this scenario's area. That is an INFERENCE from run history: the junction is
  appData\cache (NewVrfAppData52.ps1:43), the vendor's own cache, its contents by area are unmeasured, and the
  files / MB recorded before and after cannot show area.
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
- Q4 (D36 P4, movement happens): at least 5 objects each move more than 500 m (POS) between T0 + 60 s and the window
  end. TSK completions are RECORDED, not gating: whether the back end reports completions of plan-driven tasks that
  no remote controller assigned is NOT IN RECORD (every TSK on record followed an interface-assigned task).
  - VOID instead of FAIL: no object moves more than 500 m by T0 + 300 s, and Q1 still holds, and the sim= clock is
    running (Q2 > 0). Then every plan is trigger-gated; that is not evidence about paging.
  - The script STOPs (no score) if the load is not confirmed ("scenario LOAD CONFIRMED", LaunchVrf52.ps1:1445) or
    RunSim does not exit 0 (tools/RunSim/Program.cs:143-152), so neither can be misread as VOID.

Recorded, not gating (as D36 recorded the queue); on a STOP the script still records the queue, the top-3 thread CPU
and the vendor log's last write before the stop:
- the feature queue n / median / p95, against D36(b) SCORED (73 / 135 s) and FILL (114 / 161 s);
- memory: the gate line, the sys CSV's minimum Available and when, the vmmem* and per-container figures, any WARN or
  STOP snapshot;
- the count of saved in-flight tasks (B4), the number of objects that moved, and TSK counts per task type.

## 4. Predictions (L1's, unchanged; MEDIUM - the first run of a vendor sample on this box)

- L1-a: no back-end loss in the 20 min (= Q1).
- L1-b: working-set slope <= 200 MB/min after the first 5 min (= Q3).
- L1-c: feature-queue median wait < 30 s. Recorded, not gating.
- New, for Q4: movement happens (MEDIUM-LOW: plan triggering is NOT IN RECORD, and FEG p20 says some movement is
  "quick launch events", which a -NoGui run never fires).

Readings (each a measurement statement; implications follow in separate sentences):
- PASS: MAK's own pattern ran 20 min on its own scenario with no D36(b) signature, on the area B2 measures.
  - Implication: step 2 may be registered (the operator's rule). The area caveat (sec 0) travels with the verdict.
- FAIL on Q1 or Q3: the D36(b) signature occurs with MAK's own pattern and frame mode, with nothing of ours running.
  - Implication: changing our tasking to the vendor's way does not avoid it; step 2 is not run, and the terrain levers
    (T2 sim-only buildings off, T4 terrainInterfaceConfig) come first (L1:54-55).
- FAIL on Q4 alone: recorded as such. The operator decides whether step 2 proceeds.
- VOID (Q4): no reading about paging. One re-run is allowed only after the trigger question is read on the box.

## 5. Changes from L1 as registered (each declared)

1. -NoGui (L1:39 said GUI on): the 10-16 standing ruling and memory.
2. A clock start: L1 named no run() sender (L1:41). RunSim (tools/RunSim), no multiplier, one app number.
3. NotifyLevel 4 passed (L1's command ran at the default 3, where the queue measure has 0 lines; D36:32).
4. The scenario path repeats the basename (L1:33 would fail LaunchVrf52's check).
5. The holder is held 2,400 s (-FederationHoldSecs 2400), not the 900 s default, which is shorter than the window.
6. Liveness and the clock by WatchVrf --report-backends plus the new --report-simtime (L1 had liveness only).
7. Amendment 2's memory gate (>= 11.5 GiB Available), samplers, snapshots and back-end-first STOP.
8. The pass rule mirrors D36(b) (sec 3), as D231 asks. L1's predictions are kept as they were.

## 6. Gates before the launch (any miss = NOT RUN)

- B1-B4, box reads, read-only, no launch (asked of the supervisor 2026-10-20):
  - B1: the .scn of each of the three scenarios: terrain, SMS, frame mode, the start state, any resume flag;
  - B2: the extent, lat/lon min/max over all objects and route vertices, in km2;
  - B3: per scenario, units / containers / planned movers, and which plans are trigger-gated;
  - B4: the saved in-flight task count per task type, and the cache and fastForwardSettings state.
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
PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md :31-56; PREREG_L1_L2_L5_DOCS_ADDENDUM_2026-10-15.md items (4)-(6);
PREREG_STP1296_GROUP_TASK_2026-10-19.md :130-132, :308; EXTRACT_RoadToKaunasPhaseTwo_oob_2026-09-28.txt:31, :59;
FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md:137-138; HANDOFF_PARK_2026-10-16.md:81; Jira STP-1296 c14963.
RUN KIND: movement
DEVIATION FROM RECORD: no "--pre-order-gate" and no "CONSOLE LEVEL: 4": no interface runs and no order is pushed.
