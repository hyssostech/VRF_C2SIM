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
(final numbers + ratio + movement after teardown)
