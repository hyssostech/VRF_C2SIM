# PREREG: order-time materialization + CPU load of the crawl (2026-09-06 evening)

Tier: HEAVY (a cause claim is on the table: "the crawl is the machine being overwhelmed" vs
"the crawl is self-inflicted scale in the wrong model of the scenario"). Gate: PREREG (this
file, written BEFORE either run), SPEND: none (local runs). Written by the supervisor after the
user's rulings of 2026-09-06 21:50-22:10Z:
- "There are just 11 taskees. These are the only ones that need to be simulated." (C13)
- "Can the configuration happen at order time, but still get the units to display during
  initialization?" -> yes (design in the reply, sec 2 below).
- "Let's give this a shot. Observe the cpu load so we can understand if this particular
  machine is being overwhelmed, which would explain the crawl."

## 0. Docs consulted (feedback-docs-first)
- UG52 6.1.1 p196-197: the sim engine's default configuration "limits the overall CPU usage";
  numCallbackThreads (factory 4), numberOfNavQueueProcessingThreads (factory 2),
  numNetworkCallbackThreads (commented out -> forced 1 unless the RTI is known thread-safe);
  "the sum of these three thread counts is about 2 less than the total available logical
  CPUs" for a heavily loaded engine. Machine: i9-13900HX, 24 cores / 32 logical, 64 GB,
  RTX 4070 Laptop (4 GB) + Intel UHD.
- UG52 6.1.2 p197: the video card is listed under GUI performance only. Every 5.2 scale run
  today was headless (no vrfGui log written all day) -> the GPU is not in the loop.
- UG52 6.2 / 6.2.1 p200: Performance Monitor panel (GUI) and the Tracy profiler are the
  vendor's per-frame breakdown (object tick vs network tick). Not used here (headless;
  Tracy is interactive). This prereg measures the OS view: scripts/SampleCpu.ps1.
- Compose recipe (vendor sample commandLineRemoteController.cxx:717-775 / :1520-1554; CLOSED
  C2): empty shell (createSubordinates=false) + members + addToOrganization in the created
  callback. Nothing in the sample or in UG52 ties addToOrganization to initialization time.
- Run 1 (174427Z) evidence: a member-less battalion shell reflected its authored position for
  the whole run (274 POS rows) and was included in every 128-unit position-report round.

## 1. The two runs (ONE variable between them: Vrf:CreationPolicy)
Common: 5.2 profile, R9_Mojave_Empty_52 fixture (EntityLevel.sms, FFRTC), -NoGui, ClientId
C2SIM, TypeMappingMode=FidelityTable with the SCRATCH no-lifeform map (the S4/S6 map, so the
DI-Guy install is NOT a variable here; its own falsifier run is separate), object consoles at
the vendor default (-1: NOT the S4/S6 instrument levels - see 1.1), BackendNotifyLevel 3,
SampleCpu.ps1 at 5 s alongside, RunSecs 1500 (enough for the CPU question and the ratio; the
movement question needs the second run to complete, StopWhenComplete on it).

### 1.1 CONTROL = the crawl configuration (CreationPolicy=AtInit, today's code)
All 128 units created and expanded at init (1,333 objects with this map), 11 tasked.
Consoles OFF is a deliberate departure from S4/S6: if this run does NOT crawl, the console
instrument was the crawl (a finding on its own); if it crawls, the CPU numbers are clean.
PREDICTIONS (high confidence unless marked):
- P1 ratio (sim_ratio.py) < 1.0x (S4 0.73x, S6 0.44x).
- P2 vrfSim CPU = 4-8 cores' worth sustained after the order (the configured thread budget:
  4 callback + 2 nav + 1 network + main); machine-wide CPU < 60 %.
  => the MACHINE is not overwhelmed; the ENGINE's thread budget is.
- P3 no taskee moves > 1 km in 1500 s (S4: 0.1-0.9 km in 2700 s). Medium confidence with
  consoles off.
FALSIFIERS: machine-wide CPU >= 85 % sustained (>= 5 consecutive samples) or vrfSim >= 24
cores' worth -> P2 wrong, the machine IS the bottleneck; ratio >= 1.5x with consoles off ->
the console instrument was a large part of the crawl.

### 1.2 TREATMENT = order-time materialization (CreationPolicy=AtOrder, new code)
Init: every matched unit is created as an EMPTY shell at its authored position (the compose
recipe's first step), shells attached to their declared superiors; the member/expansion plan
is stored per unit. Order: for each unit an order references (PerformingEntity or
AffectedEntity), the stored plan runs (members created, addToOrganization), the existing
composition-ready await gates the task. Everything else stays a shell.
PREDICTIONS:
- P4 object count after the order = 128 shells + the 11 taskees' members (~150-200 entities;
  5 BN -> CP proxy HQ section, 4 COY -> tank company members, 2 no-echelon -> per the map).
- P5 ratio >= 3x (R9 with 23 entities: ~9x). Medium confidence (128 shells + ~170 entities).
- P6 >= 8 of the 11 taskees move > 1 km; >= 1 TASKCMPLT (S6 had 1; S4 0). Medium.
- P7 vrfSim CPU < 4 cores' worth sustained; machine-wide < 40 %.
- P8 the 128 shells reflect positions and appear in the position reports (as in run 1).
FALSIFIERS: P6 miss with P5/P7 met -> the crawl was NOT load; look at the taskees' own
consoles (C11) in a follow-up, no theory first. P4 miss (members created for non-taskees) ->
implementation defect, fix before reading anything else. A materialized unit that fails to
move where the same unit type moved at init in R9/L3 -> the design is wrong (sec 2 adversarial
note).

## 2. Design note (order-time materialization) - see DESIGN_ORBAT C13
Pieces already in the code: shell = CreateSubordinates=false (VrfC2SimService.cs:904/954);
members + AddToOrganization in OnVrfObjectCreated (PendingComposition); the composition-ready
await before tasking (RunTaskAsync :1473); ExpandCoarseLeaves builds the member plans from the
mapped template. New: a per-unit stored plan (shell now, members later) and a trigger in
OnOrder that enqueues the stored plans for referenced units before RunTaskAsync waits.
Setting: Vrf:CreationPolicy = "AtInit" (today's behaviour, the control) | "AtOrder" (new,
demo default in appsettings.Demo.json). Assumed, to be shown by the run: late members inherit
the shell's formation as they do at init; a GUI draws a member-less unit at its position
(demo run, GUI on).

## 3. Results

### 3.1 CONTROL (crawl configuration, AtInit) - launched 21:36Z, RunSecs 1500
INSTRUMENT CHECK FIRST (lessons-false-greens): the sampler's working-set column read 4096 MB
flat for the sim; the PowerShell tool host is 32-bit ([Environment]::Is64BitProcess False) and
clamps a 64-bit process's WorkingSet64 at 4 GB; CIM read 5,183 MB WS / 10,851 MB virtual for the
same pid. CPU times and thread counts are NOT affected (TotalProcessorTime is a TimeSpan). The
sampler now reads WS via Win32_Process; the control's WS column is the clamped one. Free RAM
during the run: ~28 GB of 64 - no memory pressure.
INTERIM (t = 5..401 s, order pushed ~t = 230 s; cpu_summary.py --order-tsec 230):
- post-order: sim engine 5.40 cores' worth MEAN, 5.67 max, 74 threads; machine-wide 26.1 %
  mean, 46.7 % max; interface 0.18 cores; WatchVrf 0.21; rtiexec/forwarder ~0.
- pre-order (creation of 1,4xx objects): sim 1.80 mean / 5.47 max; machine 12.8 % mean.
READING: P2 HOLDS. The machine is NOT overwhelmed (a quarter of its CPUs busy, 28 GB free).
The sim engine is pinned at ~5.5 cores' worth = its CONFIGURED thread budget (4 callback + 2
nav + 1 network + main, UG52 6.1.1; factory vrfSim.mtl) - the engine's frame runs to completion
on those threads and the sim clock falls behind wall (ratio < 1). That is a saturated ENGINE
BUDGET under a self-inflicted object count, not a saturated machine. The vendor's lever for a
scenario that genuinely needs 1,400 entities is the thread configuration ("about 2 less than
the total available logical CPUs" - a C:\MAK settings edit, user's call); for COA-STP1 the
object count itself was the error (C13).
INSTRUMENT GAP (found during the run): with the object consoles at the vendor default there are
no CON rows, and sim_ratio.py reads the sim clock from CON rows only - P1 (the ratio) is NOT
measurable in either run of this pair. The comparable metric is displacement per WALL time
(tools/analysis/taskee_displacement.py, joined through the app log's MoveAlongRoute lines), the
same in both runs. The vendor log carries no sim time at notify level 3.
INTERIM MOVEMENT (t = 418 s, order at ~230 s): the 9 units with MoveAlongRoute issued moved
13-103 m (max displacement from birth) - the crawl reproduces with the consoles OFF, so the
console instrument was not the crawl (the 1.1 alternative is refuted).
FINAL (run 20260906T213656Z, RunSecs 1500, runner exit 0, clean resign; cpu-samples.csv in the
run dir, 281 samples over the app's lifetime):
- post-order (244 samples): sim 5.34 cores' worth mean / 5.48 p90 / 5.67 max, 74 threads;
  machine 24.6 % mean / 30.6 p90 / 48.3 max; interface 0.17; WatchVrf 0.21; RTI ~0.
- movement (taskee_displacement.py, ~1,350 s after the order): 48, 53, 82, 86, 259, 284, 349,
  400, 586 m max displacement for the 9 tasked units with MoveAlongRoute; 0 of 9 beyond 1 km;
  TASKCMPLT 0.
VERDICT on the user's question ("is this machine being overwhelmed?"): NO. P2 holds, P3 holds.
The crawl is the sim engine saturating its CONFIGURED thread budget (~5.5 cores of 32) under
1,4xx entity-level objects the scenario never needed (C13). The console instrument is
exonerated (crawl reproduced with consoles off).
REVIEW OF THE TREATMENT CODE (workflow wf_dcad86e3, 37 agents, 3 lenses x 2 verifiers): 16
findings, 9 distinct, all fixed before the treatment run (commit after this run): early order
before the shell's ObjectCreated is now DEFERRED to the shell's arrival (was: dropped forever);
a composition completes its OWN gate (PendingComposition.Ready) so a case-1 parent cannot be
released by its init composition; tasks gate on the AffectedEntity's readiness too; AtOrder
without ComposeHierarchy refuses to start; init deliveries serialised (_initLock); case-3
release on REFLECTION (ReleaseReflected, deadline CompositionTimeoutSeconds); case-3 re-attach
restores the DECLARED subordinate order; case-1 children materialize in declared order;
de-stacked positions carried into the stored plans; only an aggregate superior takes a
re-created child back. Non-findings: the AtInit path is behaviourally unchanged (all three
lenses); deleteObject on an attached subordinate needs no removeFromOrganization; a member-less
createAggregate is the vendor sample's own first step; late addToOrganization has no timing
precondition in the header.

CAVEAT ON THE CPU METRIC (found at the treatment's first samples): the fixture runs fixed-
frame-run-to-complete, and UG52 (Time Management, ~p160) says the engine then "tries to
advance simulation time as fast as possible, independent of wall-clock time" - so the engine
consumes its thread budget WHATEVER the load (control 5.3 cores with 1,45x objects; treatment
4.5 cores with 357). CPU alone therefore cannot grade a run in this mode; the sim/wall ratio
can (needs CON rows), and displacement per wall time is the proxy used here. The machine-level
verdict (never above half the CPUs, 28 GB free) is unaffected.

### 3.2 TREATMENT (AtOrder) - run 20260906T221202Z, launched 22:12Z
MID-RUN (t = 204 s, order at ~170 s): the design ran as specified - "CreationPolicy=AtOrder
(C13): 127 unit(s) created as EMPTY shells", 128 of 128 placed from the terrain query, reflected
357 objects (control: 1,455-1,529); the order triggered 9 case-3 re-creates (the CP-proxy
battalions, the mixed-template companies) and 2 case-2 expands (510/40 Tank Breach Company,
C/1-35 Tank Company -> 4 sub-units each), all 9 re-created objects REFLECTED before release,
2 compositions composed, 9 MoveAlongRoute issued, 0 tasks dropped, 0 gate timeouts. Within
~35 s of tasking the 9 units had moved 50-400 m (control at +190 s: 13-103 m). P4 holds. The
two "Failed to deserialize xml" SDK lines are pre-existing (2 in the control as well).
FINAL (runner exit 0, window ran its 1500 s cap; -StopWhenComplete did not fire):
- objects: reflected 368 at the end (control 1,529) - P4 HOLDS (128 shells + the 11 taskees'
  members).
- movement (taskee_displacement.py, ~1,400 s after the order; 9 units with MoveAlongRoute):
  454, 490, 511, 559, 1,017, 1,149, 1,418, 2,231, 10,471 m; 5 of 9 beyond 1 km (control: 0 of
  9, max 586 m). Median 1,017 m vs 259 m (4x); max 10,471 m vs 586 m (18x).
- TASKCMPLT 0 - but the first-leg routes are 24-45 km (route length per task computed from the
  order; the fastest unit, 1-1/2/1_AD recon, did 10.5 km in ~1,400 s wall = 7.5 m/s), so no
  completion was reachable inside the window: P6 was MIS-SPECIFIED (written without the route
  lengths), not falsified. The 33 remaining tasks were correctly held by the predecessor policy.
- CPU post-order: sim 4.26 cores mean / 5.29 max, machine 19.4 % mean / 31.0 max - see the
  FFRTC caveat (the engine consumes its budget regardless); P7's "< 4 cores" was mis-specified
  for this frame mode; the machine-level reading holds.
- P8 HOLDS: the shells reflected (368 = shells + members) and were tasked/placed normally.
- mechanics: 0 dropped tasks, 0 gate timeouts, 9 case-3 re-creates all reflected before release
  (ReleaseReflected), 2 case-2 expands composed; the two 'Failed to deserialize' SDK lines are
  pre-existing.
VERDICT: order-time materialization WORKS as designed and is the new baseline: same order, same
map, same window, 4x median / 18x max displacement, a quarter of the objects. RESIDUAL: four of
the nine tasked units stayed under 600 m in 23 minutes (C/1-35 = the expanded tank company;
B/5-20 = a mixed IFV-platoon template; 4-27/2/1_A and 856/HHC = CP-proxy HQ sections, the SAME
type as 1-1/2/1_AD which made 10.5 km). No theory is offered: their consoles were off in this
pair (by design, for the CPU question). NEXT (C11): the same run with the tasked units' MEMBER
consoles at level 3 (11 units only, so the volume is small) for the sim's own account of the
four slow units - and CON rows also give the sim/wall ratio this pair could not measure.
Adversarial review: the competing explanation for the 4x/18x gain is "different tasks, not
the policy" - refuted: identical order, map, fixture, window and code path except
CreationPolicy (the runner injects Vrf__CreationPolicy; the app log states the policy). The
competing explanation for the four slow units is not adjudicated here (consoles off); the
falsifier for "the policy causes slowness" would be a unit slower than in the control - none
is (every unit moved farther than its control counterpart).

### 3.3 C11 DIAGNOSTIC - run 20260906T224326Z (treatment + tasked units' MEMBER consoles at 3)
MID-RUN (t = 400 s, order at ~60 s): 18 member-console requests (64 members named); movement
at +340 s: 141, 534, 607, 628, 1,657, 3,294, 3,517, 4,416, 5,036 m - 5 of 9 beyond 1 km
already, and the SLOW SET IS NOT THE SAME as in 3.2 (C/1-35 and B/5-20 fast here, 856/HHC and
40/2/1_AD slow here): the slow set is not type-determined; it varies run to run.
THE SIM'S OWN ACCOUNT (the object consoles, C11): among ~21k non-routine member messages so
far, 3,073 x 'Status of task "move-to-direct" is "BlockedByVehicle"', 1,755 x 'Status of task
"move-along" is "BlockedByVehicle"', 546 x "Movement stopped by vehicle; will be blocked at
time <sim s>", from 48 of the 64 members, from t = 65 s (tasking) onwards. The routine
behaviour-tree loop (Maybe plan path / Is path blocked? / Maybe Skirt Blockage) runs at ~3 Hz
per member. No "not embarked" line so far.
TIME PROFILE OF THE BLOCKING (per 30 s of wall): 819 / 1,861 / 976 / 420 / 382 / 325 / 38 / 7
messages in the seven half-minutes after tasking, then ZERO from t = 300 s on (order at ~60 s).
The blocking is TRANSIENT: the vehicles of the 11 materialized units, all born on the one
coordinate STP gave them, block each other for ~3.5 min of wall (the sim's own words,
"BlockedByVehicle" / "Movement stopped by vehicle", on 5.2 whose ground movement has dynamic
obstacle avoidance among vehicles - MG 2.4, UG52 23.6), and the pile then clears. Which unit
escapes first is run-dependent.
SIM/WALL RATIO (sim_ratio.py on the CON rows, t = 63..522 s): 2.08x (LS) / 2.11x (endpoints).
The full-creation runs measured 0.44x-0.73x. The engine is no longer behind wall time.
WHAT THIS DOES NOT EXPLAIN: a unit still slow AFTER t = 300 s (no blocking anywhere) is not
explained by co-location; the final displacement of this run decides whether such units exist
(in 3.2 four units were < 600 m at 1,400 s). If they do, their members' post-300 s messages
are the next thing to read - not a theory.
DISSENT LINE (per feedback-anchor-vendor-and-own-notes; reopening is the user's call): the
R8 ruling ("stacked spawns are not the blocker", 2026-07-13) was made on 5.0.2, which had no
vehicle-vehicle avoidance; the sim's console on 5.2 now records ~5,400 vehicle-blocking
messages in the first minutes after tasking on the co-located units. The cost is a start-up
delay of minutes, not a stall. Not acted on. If the user wants that delay removed: (a) an
interface-side assembly-area layout at materialization; (b) STP supplies real positions;
(c) Autonomous Actions off (no avoidance at all - realism cost); (d) the aggregate-level
profile, where "aggregate-level scenarios do not support collision avoidance among simulation
objects" (UG52 27.1.4) - already in the plan.
INSTRUMENT GAP: the member-console request line names the members but not their uuids, so
the CON rows (uuid-keyed) cannot be attributed to units offline - add the uuids to that line.
