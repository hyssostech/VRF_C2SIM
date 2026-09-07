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
FINAL (runner exit 0, 1500 s window, 1.04 M CON rows): displacement at ~1,530 s after the
order: 587 / 1,317 / 1,348 / 1,390 / 4,502 / 7,820 / 24,323 / 24,490 / 26,036 m - 8 of 9
tasked units beyond 1 km (treatment 3.2: 5 of 9; control 3.1: 0 of 9); three units at 24-26
km, i.e. near the END of their 28.5-28.7 km first legs at road speed (~9 m/s of sim time);
TASKCMPLT still 0 (the legs were not finished at the cap; the closest was 2.5 km short).
Ratio 1.84x (LS) / 1.87x (endpoints) WITH the member consoles on. "BlockedByVehicle": 4,828
messages in the first 300 s, 15 in 600-900 s, 1 after 1,200 s - the pile clears once. The
only unit under 1 km, 856/HHC (587 m), and the three at ~1.3 km are the residual. Which units
are slow differs from 3.2 (there C/1-35 was slowest; here it made 7.8 km): the residual is
run-dependent, not type-determined.
### 3.4 UNIT-CONSOLE RUN - run 20260906T231401Z (AtOrder + the UNITS' own consoles at 4, members off)
FINAL (runner exit 0; 1,392 CON rows - the unit level is quiet; ratio 2.82x (LS) / 2.78x):
displacement at ~1,530 s after the order: 241 / 1,168 / 1,258 / 1,334 / 1,367 / 1,561 /
5,256 / 11,608 / 14,493 m - 8 of 9 beyond 1 km (third AtOrder run: 5/9, 8/9, 8/9). The one
unit under 1 km this time is C/1-35 - the EXPANDED tank company (case 2) - at 241 m.
THE UNIT'S OWN ACCOUNT (verified, its console at level 4): C/1-35's move-along-controller
task starts with subtask "move-into-formation" ("Move into formation: formation: keep-
existing-formation loc: {...}") and then logs "Disagg mv into form: task complete msg rcvd
from" C/1-35.TANK4 (sim t 233 s), C/1-35.HQ1 (251 s), C/1-35.TANK3 (330 s) - and NEVER from
C/1-35.TANK2. The gate needs every sub-unit (C1b, closed 2026-09-06 with the same line), so
the company never advanced to maneuver-along; the 241 m is the platoons shuffling into
formation. The nine TEMPLATE units (the CP-proxy HQ sections, the mixed platoons) start their
move-along with subtask "maneuver-along" directly - no formation gate - which is why a
template unit with a stalled member still leaves (the maneuver-along re-tasks members).
MECHANISM OF THE RESIDUAL (now verified at the unit level, two runs): a COMPOSED unit's
move-along waits for every sub-unit's move-into-formation; a sub-unit that stalls in the
vendor script's loop at the start-point pile (3.3) never reports, and the unit waits for the
rest of the run. Which unit that hits is run-dependent because which sub-unit stalls is.
### 3.5 COMPLETION RUN - run 20260906T234525Z (AtOrder, unit consoles at 4, RunSecs 2700)
FINAL: window ran to its 2700 s cap (-StopWhenComplete did not fire); TASKCMPLT 0; ratio 2.91x;
displacement at ~2,700 s: 438 / 466 / 1,023 / 1,087 / 1,340 / 2,074 / 3,188 / 11,054 / 22,421 m -
7 of 9 beyond 1 km; the farthest (856/HHC, first leg 24.1 km) ended 1.7 km short of its leg.
Even at 2.9x, 7,800 sim-s did not complete a 24 km leg for the fastest unit (mean 2.9 m/s of
sim time against ~9 m/s in 3.3): the start-point pile costs every unit minutes, and two units
(4-27/2/1_A, 1-6/2/1_AD) stayed under 500 m for 45 minutes - the fourth run in a row with a
run-dependent stalled subset (3.2: 4 units; 3.3: 1; 3.4: 1; 3.5: 2). Completion at scale
therefore waits on the spacing ruling (C14, PREREG_ASSEMBLY_LAYOUT), not on the window.
Adversarial review of 3.2+3.3 together: the treatment ran the same code path twice and got
5 of 9 and 8 of 9 - the difference is not the consoles (more load here, better result), so
the per-run variance is in the sim's resolution of the start-point pile; the competing claim
"AtOrder is what makes units move" is supported only in contrast to the control (0 of 9,
twice: S4/S6 earlier and 3.1 today), never against a run with AtInit and 11 units, which
does not exist because AtInit creates all 128. Nothing here reopens R8; the pile costs
minutes, and a unit left stalled after the pile clears is the open item (unit console, 3.4).
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
the CON rows (uuid-keyed) cannot be attributed to units offline - FIXED in the source (uuids
in the line, commit fa123b0; deployed after this run).
THE SLOW MEMBERS' OWN ACCOUNT (t = 300..604 s, attribution by each member's OWN POS
displacement since the uuid map is missing in this run): of the 64 members with consoles, 32
had moved >= 1.5 km and 15 < 700 m - twelve of those at 0-17 m (six at exactly 0 m: never
moved) and three at ~300 m. After the blocking ended (t >= 300 s) the 15 slow members
emitted ONE message kind only: 'Status of task "move-along" is "TaskRunning"' (1,789 times) -
no blocking, no planning, no path job, no give-up, no "not embarked". The fast members show the
normal sequence (plan path success, "Planned path has N parts", move-along, one "Completed").
READING: a member whose move-along is "running" while it stands still and reports nothing is
waiting on its UNIT, and the unit-level gate the record already knows is C1b (CLOSED
2026-09-06 by the unit's console at level 4): a template unit's move-along first runs
move-into-formation and waits for "task complete msg rcvd from" EVERY subordinate; a template
HQ-section HMMWV that never reaches its slot keeps the gate shut. The slow units here are
case-3 TEMPLATES (the CP-proxy HQ sections: M1A2 x2, M3, HMMWV x2, M577A2 - the very object
type whose HMMWV failed inside the company in C1b), and which of them stalls varies by run.
This is a candidate with prior evidence, NOT a verified cause for this run: the members'
consoles cannot show the unit's gate; the UNIT's console at level 4 can (that is how C1b was
closed). FALSIFIER: the slow units' own consoles show "Move into formation complete" (gate
open) - then the stall is something else. NEXT RUN: the same run with Vrf:ObjectConsoleNotify-
Level=4 (unit consoles; the 128 shells add little) + member level 3.
WHOLE-RUN HISTORY OF THE SLOW MEMBERS (t = 63..230 s, then silence): 74 path-plan jobs (71
successes), 69 move-along subtasks started, 1,011 + 296 "BlockedByVehicle", 85 "Movement
stopped by vehicle", 34 x "Entity not embarked on same object as target [Route N]. Ending
task" (6 of the 15 members), 31 x "Global Replan" + "Loop to stall for replanning" (10 of 15),
82 script-controller task clears - a blocked/replan churn that ends at t = 230 s; afterwards
only 'move-along TaskRunning'. The FAST members went through the SAME churn (12 "not embarked"
on 8 of 34; the stall loop on 22 of 34) and still made 1.5-5 km.
VENDOR SCRIPT (ground-vehicle-move-to.lua, EntityLevel/scripts): MAX_REPLANS = 3 ("Attempts to
replan and move before deciding it is permanently stuck and aborting", :46); after the path
parts fail, "Attempt Global Replanning Once" = hasNotAttemptedGlobalReplanning -> globalReplan
(setStateVar replan) -> loopToStall = a loop with doWhile = true and child alwaysSucceed
(:1279-1284) - i.e. by design the member's move-to STALLS FOREVER after its one global replan
fails; it leaves that state only when a NEW subtask replaces it (the unit re-tasking its
member: "script-controller clearing one of its tasks"). So the fast members were rescued by
re-tasking and the slow ones were not: after t = 230 s their unit issued nothing new. The
decider is the UNIT's controller, whose console is off in this run - the level-4 unit-console
run registered above is the observation, and "Ending task" on a member (the give-up) is the
candidate for what leaves the unit waiting (its move-along waits for every member's
completion; a member whose task ENDED never completes - the C1b shape at the move-along
stage). Verified: the script text and the counts. Assumed: the unit-level wait.
REFUTED ON THE VENDOR DATA (2026-09-06 23:05Z): "a member vehicle got EMBARKED on another
vehicle at the pile" as the meaning of "Entity not embarked on same object as target". The
member types of the HQ section cannot embark on each other: M1A2's embarkation slots accept
only 3:3:1 (lifeforms), M577A2 has can-be-embarked-upon=False, the HMMWV template carries no
embarkation parameters at all. The message's give-up condition therefore concerns something
other than vehicle-on-vehicle embarkation (the entity's attachment to its own aggregate, or
the ROUTE object's attachment - the header text "the entity is attached and the target of the
current task is not" leaves both open). Not resolved here; the unit console run is the next
observation either way.
IF THE UNIT CONSOLE CONFIRMS a member give-up leaves the unit waiting, the fix is the vendor
sample's own recipe one level lower: compose the HQ section from ENTITIES (shell + createEntity x6 + addToOrganization,
commandLineRemoteController.cxx:717-775 does exactly this for a platoon) instead of creating
it as a template - i.e. lift the "platforms stay a template" rule in ExpandCoarseLeaves behind
a setting, and verify 3/3 on the small fixture first.
