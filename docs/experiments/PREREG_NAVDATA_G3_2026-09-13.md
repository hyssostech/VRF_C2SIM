# PREREG G3: does the G2 engine collapse reproduce on an idle machine, and which thread is hot?

Registered 2026-09-13 by the supervisor (Fable 5.1) BEFORE launch. Follows PREREG_NAVDATA_2026-09-07.md
(G1 loading chain HOLDS; G2 = COA-STP1 on the 20x20 km ground-platform area: the sim/wall ratio held
~1.5x for ten minutes then stepped to a 0.02-0.04x floor for the remaining fifty; TWO variables, the
area and a concurrent 212-agent load on the same machine from wall -47 s to +1063 s). Tier: HEAVY (a
cause claim is under test). Licence: the DEMO licence lapses 2026-09-15 (user-owned renewal).

## 0. Docs consulted (read 2026-09-13 by a Sonnet reader; key lines re-grepped by the supervisor)
- UG52 6.1.1 p196-198 (multiprocessor performance): numCallbackThreads are "spawned to run simulation
  ticks on locally simulated objects ... Scenarios with higher complexity entities typically can be
  improved by increasing the callback thread count"; "the sum of these three thread counts [callback,
  network callback, nav queue] is about 2 less than the total available logical CPUs"; "run only one
  simulation engine per computer". This machine: 32 logical CPUs. Shipped vrfSim.mtl: numCallbackThreads
  4 (line 218), numNetworkCallbackThreads commented out (422), numberOfNavQueueProcessingThreads 2 (415).
- C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl (shipped, untouched): gamewareMemorySize 16
  (383) "working-memory limit (MB) for GameWare path calculation ... increasing this limit can allow for
  path plans on larger nav areas by more entities simultaneously"; gamewareQueryTimeBudget 5.0 (389)
  "time budget for GameWare navigation queries per frame ... the higher the value, the more responsive
  path planning will be, but the slower the VR-Forces sim frame rate will be" (the Developer's Guide
  table says default 1.0 ms - the two sources disagree); numberOfNavQueueProcessingThreads 2 (415)
  "adding more threads will decrease the length of time it takes for entities to perform path planning
  ... will not change the simulation frame rate"; regenerateNavigationDataAtRuntime 1 (395);
  maxNavigationRegenerationThreads 2 (402); loadAllNavigationDataOnTerrainLoad 0 (436);
  blockOnAsynchronousOperations 0 (445): "if enabled, scenarios that are fixed frame rate will block on
  asynchronous operations (terrain, feature, pathplanning, and navigation operations)".
- UG52 3.4.3 p122: fixed-frame-run-to-complete "advances simulation time by a fixed amount each frame,
  even if a frame takes longer than the fixed amount to compute"; 6.11 p215: targetFrameRate applies to
  variable frame mode only. At the G2 floor each FRAME therefore took ~50x longer to compute than in
  P11. The docs do not say what happens when the per-frame navigation query budget is exceeded (SILENT).
- UG52 66.2.1 p1277 (sectorizing): "Path planning over long distances may be slower"; our area has
  1,600 sectors of 500 m. 66.3 p1281: data loads lazily when an object that uses it is placed.
- No vendor source quantifies runtime cost against vehicle count, sector count or area size; every
  quantified figure in ch 66 is about GENERATING data (SILENT on steady-state cost).
- The sim's own usage text: --numCallbackThreads, --disableParallelTick, --waitQueue and
  --setMainThreadToHighPriority exist as command-line options; the gameware* and nav-queue keys are
  .mtl-only; -d/--settingsFile replaces vrfSimSettings.xml, NOT vrfSim.mtl (UG52 Table 11 p183); the
  vrfSim.mtl includes are cwd-relative, so --appDataDir does not cleanly redirect them. A settings probe
  therefore means editing the vendor's vrfSim.mtl - a write under C:\MAK, the user's call.

## 1. The lever (ONE variable vs G2)
NOTHING else runs on the machine during the run: no agents, no workflows, no analysis scripts, no
generator. Added INSTRUMENT (observation only): scripts/SampleThreads.ps1 samples every 5 s the
vrfSimHLA1516e process CPU (single-core equivalents), working set, thread count, and the top threads by
% Processor Time with their thread ids. Everything else = G2 exactly: fixture R9_Mojave_Empty_52_Nav
(terrain copy + MojaveAO20 area), deployed build 80daed6 (exe 2026-09-07 11:06Z), the P11/G2
environment block, the PROBE type map, -NoGui, BackendNotifyLevel 3, unit consoles 4 / member consoles
3. Cap: -RunSecs 1500 -WatchSecs 1800 (G2's onset was at wall ~628-660 s and its floor was established
by ~1000 s; 1500 s gives ~8 minutes of floor with CPU samples). The machine was rebooted since 09-07:
the runner's StartRtiExec52 stage brings the RTI up and the Stage 2b watcher answers the once-per-reboot
dialog.
Command (64-bit pwsh from Bash; env identical to G2):
  export Vrf__TypeMappingMode=FidelityTable Vrf__CreationPolicy=AtOrder Vrf__DeStackCreates=true
         Vrf__DeStackSpacingMeters=700 Vrf__DeStackRotationDeg=0 Vrf__DropOriginVertexMeters=100
         Vrf__TaskPredecessorTimeoutSeconds=7200 Vrf__ObjectConsoleNotifyLevel=4
         Vrf__ObjectConsoleMemberNotifyLevel=3 Vrf__PositionReportSeconds=10
  pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\RunC2SimScenario.ps1 -VrfProfile 5.2 -NoGui
       -Scenario R9_Mojave_Empty_52_Nav -Init data\COA-STP1_Initialization.xml
       -Order data\COA-STP1_Order.xml -ClientId C2SIM
       -TypeMapFile <scratch>\unit-type-map-52-nolifeform.json
       -RunSecs 1500 -WatchSecs 1800 -BackendNotifyLevel 3 -StopWhenComplete
  and, started as soon as the sim process exists: scripts\SampleThreads.ps1 -ProcessName vrfSimHLA1516e
       -MaxSec 1900 -IntervalSec 5 -OutFile <scratch>\g3-threads.csv (copied into the run dir after).

## 2. Hypotheses on the table (from G2's record)
- H1 navigation cost at scale: ~60 vehicles planning on a 1,600-sector mesh exceed the per-frame budget
  or working memory, and the engine spends its frame in navigation.
- H2 my contamination: the concurrent load induced the onset; the floor was a consequence that outlived
  it (untested because CPU was never sampled).
- H3 a per-unit pathology: 1-6's failed move-along subtask and nav-area toggling at its 2.9 km line
  drive per-frame replanning that stalls the engine.
- H4 the mesh is expensive per se - already REFUTED by G1 (same area, 6 units, 3.95x).

## 3. Predictions (written before launch; a missed HIGH prediction is a stop)
- P13a (HIGH, decides H2): the step collapse REPRODUCES with nothing else running - the 300 s
  least-squares sim/wall ratio falls below 0.2x in some window before wall 1200 s and stays below 0.1x
  to the cap. MISS = the G2 collapse was load-induced (H2): the engine finding is withdrawn from the
  record, G2's equal-sim-time movement result (1.04x, one unit split) stands as the mesh's effect, and
  the next run is a full-length clean repeat of G2 (4200 s).
- P13b (MEDIUM): at the floor the vrfSimHLA1516e process is BUSY, >= 100 % of one core sustained
  (single-core equivalent). MISS (near-idle at the floor) = the bottleneck is a wait (lock, I/O,
  network), not compute - a different investigation.
- P13c (RECORDED, not predicted): which threads carry the process CPU at the floor (main thread vs a
  few worker threads vs many). One or two threads carrying most of it points at a serial section (H1 on
  the main thread's query budget, or H3's replanning); CPU spread over the 4 callback threads points
  elsewhere.
- P13d (MEDIUM, H3): the onset coincides (within 60 s wall) with a 1-6/2/1_AD member's "Controller's
  subtask has Failed (base-system.movement.move-along)" plus Leaving/New Primary nav area toggling, and
  1-6's members are at ~2.9 km displacement at that moment. HOLDS -> H3 gains a discriminating test
  (the same run with everything except 1-6 tasked). MISS -> H3 weakens.
- P13e (instrument sanity, HIGH): 128 units created, order accepted, nav-area console rows > 0 (the
  mesh is in use as in G1/G2), the thread CSV has >= 200 rows with a non-zero process CPU.

## 4. What counts as a stop
P13a or P13e missed = stop, report, no parameter adjusted. Whatever P13a shows, G4 (below) is a NEW
prereg, not a continuation.

## 5. G4 decision tree (written now so the choice is not made after seeing the data)
- P13a HOLDS and P13c shows a serial hot section: doc-backed levers in this order - (i) regenerate the
  area with FEWER, LARGER sectors (ours; no C:\MAK write; UG52 66.2.1 names the long-distance planning
  cost of sectors) and rerun; (ii) vrfSim.mtl gamewareQueryTimeBudget / gamewareMemorySize /
  numberOfNavQueueProcessingThreads / numCallbackThreads per UG52 6.1.1 (a C:\MAK write = the user's
  call, asked once with the numbers).
- P13a HOLDS and P13d HOLDS: first the H3 discriminator (same run minus 1-6's task), then the levers.
- P13a MISS: full-length clean repeat of G2; the record's "engine collapsed" finding is withdrawn.

## 6. Results - run 20260913T174516Z (launched 17:45:16Z, 1500 s cap, runner exit 0, clean teardown)
HEADLINE (supervisor's own read of the two instruments, 18:15Z; the executor's full harvest
follows below when it lands): THE COLLAPSE DID NOT REPRODUCE. P13a MISSED.
- sim/wall per 300 s wall window (endpoint ratios, sim_ratio.samples): 1.800x, 1.672x, 1.573x,
  1.573x, 1.544x, 1.451x (partial last window). Sim reached 2,563 s in 1,592 s of wall; no
  window anywhere near 0.2x. G2 at the same wall times: 1.545, 1.675, 0.352, 0.050, 0.039.
- threads.csv (351 samples): the sim process held 4.6-5.0 cores from wall ~180 s to the cap,
  working set 4.2 -> 4.75 GB, 81-84 threads; the SAME five hot threads all run (tid 44084 at
  0.85-0.90 cores - the main thread by its birth at t=5 s - plus four at 0.67-0.81 = the four
  callback threads of vrfSim.mtl:218). No thread-set change, no idle phase.
- Bring-up after the reboot: rtiexec/forwarder started by the runner (pids 69856/50520 - the
  never-kill rule now covers these), no RTI dialog (assistant-free profile), oracle gate 256
  POS lines / 128 uuids, order accepted; nothing else ran on the machine (verified before
  launch; the only concurrent processes were the runner's own observers and the sampler).
- P13e HOLDS (128 units, order accepted, thread CSV 351 rows with non-zero CPU; nav-area rows
  counted by the harvest). P13b/P13c are moot for "the floor" - there was none - but recorded:
  the fast-phase profile above IS the engine's steady state under this scenario with the mesh.
CONSEQUENCE, per sec 4/5 as written before launch: the "engine collapsed under navigation
data" finding of PREREG_NAVDATA_2026-09-07 G2 RESULTS is WITHDRAWN. G2's equal-sim-time
movement result (1.04x vs P11 at sim 1170; 1-35 frozen at 1.97 km in both; 1-6 SPLIT 4/2 at
the 2.9 km line) stands as the mesh's measured effect so far. G4 = the full-length clean repeat
of G2 (4200 s), registered in PREREG_NAVDATA_G4_2026-09-13.md before launch.
Adversarial review: the pre-committed reading of a P13a miss is H2 (my concurrent 212-agent
load induced G2's collapse). A second difference between G2 and G3 was NOT controlled and is
named here rather than buried: G2 ran on an rtiexec/forwarder pair alive since 09-04 with a
teardown-relaunch history (vrf-teardown-relaunch-wedges-rti); G3 ran on a fresh pair started
minutes earlier after a reboot. So G3 shows "not reproduced on an idle machine with a fresh
RTI"; it does not by itself separate load (H2) from RTI age (H5). Both are absent from G4's
conditions, so G4 does not need to separate them either; the discriminating test for H2 alone
would be a deliberate re-contamination on a fresh RTI, not worth a run before the licence
lapses. H3 (a 1-6 pathology) is decided by the harvest's event check (task 6). The mechanism by
which a transient load could leave a 50-minute floor after it ended (G2 showed no recovery)
remains UNEXPLAINED and is recorded as such, not smoothed.
EXECUTOR HARVEST (Opus, g3_harvest.py in the session scratchpad; its instrument check reproduced
P11's 147 BlockedByVehicle-by-sim-1170 exactly):
- Ratio: full-run LS 1.601x (envelope 1.616x); 60 s windows min 1.437x / max 2.112x / mean
  1.634x; windows below 1.0x: 0. Slow monotone decay, no step. P13a MISS CONFIRMED.
- Threads: from tSec 300 to the end exactly the same five hot tids every sample - 44084 (main)
  0.87 cores, four workers 0.70-0.78 each; top-1 share of process CPU mean 0.24 -> CPU spread
  evenly over main + the 4 callback threads, no serial hot section. Process mean 4.89 cores over
  tSec 300-1500. Working set 4589 -> 4758 MB from tSec 600 = 8.8 MB/min, decelerating. Threads
  flat at 81-83.
- Equal-sim-time displacement (median member, m) G3 / P11 / G2 at sim 1170 and G3 / P11 at 2500:
  1-1 10022/10459/10493, 23140/23518; 1-35 1970/1970/1974, 1969/1970; 1-6 3291/2845/6190,
  3292/2884; 4-27 10321/10692/10632, 22778/22674; 40 9081/8516/9235, 22022/21405; 5-20
  9206/9391/10412, 22353/22500; 856/HHC 10226/10675/10461, 21035/23090; B/5-20
  10622/10786/10828, 23813/23937; C/1-35 8601/9165/7077, 21152/21165; A/6-56 0 (ADA task
  refused). SUM G3/P11 = 0.984 at 1170 and 0.990 at 2500. THE MESH HAS NO GENERAL SPEED EFFECT.
- Formation (max member-to-centroid, m) at 2500: G3 tighter than P11 on every unit except
  1-35 (194 vs 57) and 4-27 (155 vs 106); the only members > 1 km are 856/HHC's, in BOTH runs
  (G3 2, P11 4).
- 1-6/2/1_AD in G3: NEITHER the P11 whole-unit stop at 2.85 km NOR the G2 4/2 split - it passed
  the 2.9 km line and stopped AS A WHOLE at 3.29 km (spread 108 m), frozen from sim 1170 to the
  end with zero failure rows. Three runs, three different outcomes at the same unit's early stop.
- 1-35/2/1_A in G3: 1970 m at sim 1170 and 2500 - IDENTICAL to P11 (1970) and G2 (1974). Three
  runs, one freeze, with the slope-aware planner active over its position.
- H3 check: "Controller's subtask has Failed" rows in G3 = 0 (all 370 task-controller outcome
  rows are Completed; the instrument is live). The G2 break event did not recur. P11 - no area,
  no collapse - carries 9 such failures for 1-6's HMMWV 8 at sim 739-795 and 2 for C/1-35: the
  1-6 move-along failure is a P11 feature too. P13d MISS; H3 REFUTED as a collapse mechanism.
- Nav-area rows: G3 141 (New 79 / Leaving 62; C/1-35 50, 4-27 16, 1-1 12, 5-20 12, 40 12, 856
  12, B/5-20 8, 1-6 8, 1-35 6, A/6-56 4) vs G2 324 vs P11 0; the ratio is unchanged through
  every nav-row burst.
- BlockedByVehicle: G3 172 by sim 1170 then SATURATES at 181 for the run; P11 147 by 1170 and
  keeps accruing to 424; G2 518 by 1170. G2's 3.5x was NOT a mesh effect (G3 ~ P11 early).
- P13b not evaluable (no floor); P13c recorded above; P13e HOLDS (128 units, order accepted,
  141 nav rows, 349 sampler rows).
SUPERVISOR READING (the verdict is mine; the user decides the demo lever):
1. The engine question is CLOSED for this scenario at this scale: the mesh does not slow the
   engine; a concurrent load on the machine did. Rule: nothing else runs during a timed run.
2. The MOVEMENT question the track was opened for - "silent whole-unit stops 2-3 km out are the
   slope-blind planner" (PREREG_ASSEMBLY_LAYOUT 3f/3g) - is REFUTED for 1-35: the slope-aware
   planner was active over its position in G2 and G3 and it froze at the same 1.97 km as
   without it. For 1-6 the stop is VARIABLE across three runs (2.85 km whole / 4-2 split at
   2.9 and 6.2 km / 3.29 km whole), which no fixed terrain feature produces. Whatever stops
   these two units, navigation data is not the cure and terrain is not the demonstrated cause.
3. NEXT INSTRUMENT (no run needed): the units' OWN consoles at the freeze, in the three traces
   we already hold (lessons-vendor-diagnostics-first) - what 1-35's controller and members say
   from sim 300 to 900, and where the freeze point sits relative to 1-35's route vertices.
4. G4 (the full-length clean repeat) stays registered and is run after that read, on an idle
   machine; it answers completions and the far legs, which G3's 2,563 sim-seconds could not.
ADDED 2026-09-13 ~22:40Z (found by the progress-watchdog replay validator, tools/analysis/
stall_replay.py on this run): 856/HHC~PXY stopped EARLY in G3 - all four members under 47 m of
movement per 120 s from wall ~1,500 s (sim ~2,400), 3.5 km short of its destination, while 1-1 was
still covering 1.7 km per window (the sim was live). P11's 856/HHC reached 23.1 km by sim 2500 and
its stragglers appeared at sim 1,960; G3's stopped as a unit. A third early stop, previously
uncatalogued, on the mesh run; its geometry (face? soil?) is UNREAD - a candidate for the
pre-flight tool's calibration set and for the G6 run's watch list.
Adversarial review of the reading: the strongest competing account for 1-35's identical freeze
is "the mesh does not cover its position" - refuted: its authored position and its 1.97 km
stop point lie inside lat 34.518-34.698 / lon -116.809 to -116.591 by construction (the area
is centred 2 km north of the STP point) and 1-35 itself emitted 6 nav-area rows in G3. Second
competing account, "the unit is waiting on its ADA task chain or a predecessor" - not excluded
here; the console read decides it. Unexplained and recorded: why G3's blocking count saturates
at 181 while P11's keeps accruing; why 1-6 stops at three different places.
