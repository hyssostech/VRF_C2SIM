# PREREG - IRON STORM CUT A, RUN E2-2: the E2 registration re-run on the timer-anchor fix (d7dd5d0), successor-gate floor 600 s - T14 as a 2-vertex chain through the (i) waypoint (one scored run)

STATUS: SCORED 2026-09-27 (Result at the end): run 20260927T231937Z ran once at the seat's go-live on the registered
build d7dd5d0. Every HIGH prediction held on the registered line families: 48 IBCT completed its 2-vertex Move To chain
and arrived 6.9 m from its destination past the -2 hamlet, the falsifier P20F did not fire, and the timer fix held live
(no SKIP; the holds' timer counted from their dispatch sample). One MEDIUM miss (P31b); one reading of P19b is flagged
for the seat. Everything between this STATUS paragraph and the Result is unchanged since bbdb126 (merged d2468c3), when
the STATUS read "REGISTERED - LAUNCH PENDING (seat's go-live)". Registered by lane E2 (session 5fc25950; the registration
commit's own time is the stamp) on branch run/ironstorm-cuta-e2-2 from main d7dd5d0, BEFORE any order push, holder
action or launch. PREPARATION ONLY at registration: no C2SIM push, no holder start, no VR-Forces or runner launch (one runner
PREP DRY RUN, sec 3 E), no appNumber claimed; the launch waits for the seat's go-live (two offline lanes are running).
Written from E2-1's registration (PREREG_IRONSTORM_CUTA_E2_2026-09-27.md, IRONSTORM_CUTA_E2-2026-09-27-1, STOPPED at P4:
its structure, settings, sequence, P0-P30 and scorer carried over; its STOP RULES name this registration as its re-run)
and the seat's -2 revision list: (a) the successor-gate floor 600 s, (b) the timer-fix predictions and the anchor
falsifier, (c) the fix's residual, (d) the controls. ONE ITEM OF (b) IS REGISTERED DIFFERENTLY FROM THE BRIEF, with the
evidence, in sec 1(n): the served-figure limb is MEDIUM (P31b), not a STOP, and the anchor falsifier measures the anchor
directly (P31F). Marks: [V] = checked while writing this file; [A] = taken from the record, not re-checked.

## Registration

PREREG ID: IRONSTORM_CUTA_E2-2026-09-27-2
DATE (UTC): 2026-09-27, before any launch (the registration commit's own timestamp is authoritative; the launch waits
for the seat's go-live)
BINARY / COMMIT: main d7dd5d0 = E2-1's src (0849334) + the timer-anchor fix (0ff13f4 on fix/timer-anchor-at-dispatch,
merged d7dd5d0). `git diff --stat 0849334 d7dd5d0 -- src` is exactly RulingsSelfTest.cs, TaskDispatchPolicy.cs,
TimedCompletionPolicy.cs, TimerAnchorSelfTest.cs (new) and VrfC2SimService.cs [V]; their non-comment change is the
anchor alone - MarkDispatched reads the task-clock axis once (`double dispatchClock = TaskClockSeconds;`,
VrfC2SimService.cs :5277) and hands that value to TaskSequencer.NotifyDispatched and to TimedCompletionPolicy.Register
(dispatchClock:, dispatchClockUsingSim: true), which seeds the entry's anchor with it; no log line is added or changed
[V]. Preflight/* is unchanged [V]. The rest of `-- tools scripts data config` since 0849334 is the C1 design lane's
data/unit-composition-52-aggregate.json and tools/aggregate/*, which no source file reads (`unit-composition` has no hit
under src) [V]. BUILT AND DEPLOYED BY THIS LANE 2026-09-27T22:24:32-22:24:54Z per RUNBOOK sec 9 into the MAIN checkout's
own output folders (never C:\MAK), main checkout HEAD d7dd5d0 with a clean tracked tree, `dotnet build <csproj> -c
Release -p:BridgeConfig=Release-5.2 -t:Rebuild -m:1 -nodeReuse:false`, 0 errors, VrfC2SimApp's documented 4 CA2024 + 2
CS8632, the others 0 [V]: VrfC2SimApp.exe sha256 3f2e06b63821979c0f727231810aeb98799908bcf9ab831828578254a1630120
(162,304 B), VrfC2SimApp.dll sha256 0cbf7a113f5d8ee08fad41570c5a989bf181ff107024c36a878a6f6157b4fb2c (1,680,384 B),
ProductVersion 1.0.0+git.d7dd5d0.Release-5.2 (exe and dll, no +DIRTY); deployed appsettings.json
5e85e4b5ba24202442c9903ada6ea7974f5abb714a5781fdfed1feda5d08daa2 = src = E2-1's (TaskPredecessorTimeoutSeconds 600,
TaskPredecessorEndMarginSeconds 60, TaskChainBackstopSeconds 86400, TimedCompletion true, TaskClock "sim"; the M1 / M2
keys as E2-1); appsettings.Demo.json 12391080...f8f6ca = src; VrfBridge.dll
90272bc95297e3304066218bd5a53128ef8531cfbd6b4dcec3ce5c65b0f25847 (the pin, NOT rebuilt) in all eleven output trees and
the build directory. TEN consumers rebuilt in place (each ProductVersion names d7dd5d0); the ELEVENTH, tools/RtiProbe,
NOT: its bin\Release-5.2 tree is held open by the persistent holder RtiProbe 45600 (RUNBOOK sec 9, the 2026-09-26
19:58Z precedent, as E2-1); `git diff 5e8d6f1 d7dd5d0 -- tools/RtiProbe tools/Shared` is EMPTY, so its tree
(RtiProbe.dll 070d3e34..., 18:00:32Z, 8 files) is E1's build of unchanged code, and a -t:Rebuild into a scratch output
compiled 0 errors / 0 warnings at d7dd5d0 [V]. Offline suites against the DEPLOYED exe [V 22:25:23-22:26:11Z, MAK PATH
prefix, DOTNET_ENVIRONMENT empty, 0 Vrf__]: 26 of 26 exit 0, every count as E2-1's except --rulings-selftest 446 PASS / 0
FAIL = E2-1's 431 + the 15 of the new section "ONE anchor - the end-time timer and the STREND gate count from dispatch"
(t0; t1 FAIL-FIRST: the pre-fix anchoring skips at stamp + 360 with "315 s of a 300 s Duration served", and the fix
completes the hold at stamp + 300 while the gate proceeds; t2 x5; t3 x2; the RL-20260925-01 Q1 FINDING t4; t5 x4
source tripwires on the service at d7dd5d0); the three --disabled fail-first arms exit 1 as designed;
--routeshift-selftest 90 ok, "0 fetches, 57 cache hits"; --osm-selftest on the lane-I2 real tiles 143 PASS;
`DOTNET_ENVIRONMENT=Demo --runtime-check` "runtime-check: OK". `git diff --stat d7dd5d0..HEAD -- src` on this branch is
EMPTY [V at the registration commit]. Fixture C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Nav_AG_maple.scnx
sha256 57465c3545e884f0c32d283e945f35ce6694233cc17a0bc14116dd4aa44a0e32 [V]. The tile cache the pre-flight reads: sec
1(k), unchanged since E2-1.
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: as E2-1 [A: its text, unchanged]: UG52 sec 23.1 p500 (Move To "Moves the vehicle to a destination
location, causing the entity to first plan a route on roads and around obstacles as appropriate, then following that
planned route using Move Along Route and road-movement tasks"); UG52 23.2 / 23.2.1 p500-501 (roads, nav meshes and
feature-obstacle planning); UG52 23.2.2 p502 (recovery - back up, drive around, one replan - Move To only); UG52 23.3
p505 (Move Along Route: "There is no path planning done as it moves toward the next vertex"); MG 2.4 p18-19;
vrftasks/moveToTask.h :60 and :181-184; vrfcontrol/vrfRemoteController.h :1651 (moveToLocation) and :605
(rollbackToSnapshot - the one way the scenario clock steps backwards); move_along_route_and_continue.lua :31-58;
ground-vehicle-move-to.lua :1401-1404 and :423-460; UG52 30.22 p598 / 30.24 p600; UG52 23.5.1 p507 + Table 26;
osm.features.water.xml :6, osm.features.xml :21, featureconfig.txt :423 and :211 [A: RUNBOOK sec 12a];
singleTaskControllerComponent.h :191-200.

OWN-RECORD CITATION:
- IRONSTORM_CUTA_E2-2026-09-27-1 (PREREG_IRONSTORM_CUTA_E2_2026-09-27.md, STOPPED at P4; run 20260927T205652Z): the
  STREND gate skipped T02 and T14 (TASKABRT "SKIPPED: predecessor ... did not complete within 360s of its dispatch",
  capture C187 / C188 at 20:59:51.171Z) 0.8 WALL s before the holds' TIMED COMPLETION ("315 s of a 300 s Duration
  served", app log L21569 / L21575; TASKCMPLT C189 / C190 at 20:59:51.972Z); the cause, verified in code and log: the
  end timer counted from its first walk after arming, the gate from the dispatch stamp, >= 45 SIM s apart against the
  60 s margin. T10 and M2's stage on T10's route held exactly as registered. Its STOP RULES: "A VOID or STOPPED run is
  re-registered as IRONSTORM_CUTA_E2-<date>-2, with new appNumbers."
- THE FIX: 0ff13f4, merged d7dd5d0 ("Timer anchor at dispatch: the end-time timer and the STREND gate count from ONE
  reading (run E2)"); TimerAnchorSelfTest.cs (the --rulings-selftest section above); RUNBOOK sec 11, the CORRECTION
  paragraph under the E5 margin bullet: "FIXED the same day ... RESIDUAL (not changed, put to the seat): the margin is
  still all that separates an end time from a skip. One task-clock step LARGER than 60 s between two timed walks ...
  lets the gate see end + 60 before the walk sees the end time" [V].
- RL-20260921-09 (end time = start + Duration, the owner's temporary position) and RL-20260925-01 (follow-ons wait for
  a late predecessor; its Q1 race, FINDING t4 of the new section, is OPEN with the owner) [A: docs/RULINGS.md].
- The rest as E2-1's OWN-RECORD CITATION [A]: RL-20260927-01 (Move To per vertex; the pre-flight port), RL-20260927-02 /
  -03 / -04 (containers; nothing in this run); PLAN_MOVEMENT_2026-09-27.md rows M1, M1b, M2, E1 and E2; IRONSTORM_CUTA_
  E1-2026-09-27-1 (SCORED); IRONSTORM_CUTA_LIVE-2026-09-27-2 (the headline's CONTROL); RUNBOOK secs 9, 9c, 11 and 12a;
  data/IRONSTORM_CUTA_CHANGES.md (i) and (j); the M1 / M1b / M2 source references of E2-1, whose VrfC2SimService.cs line
  numbers are +11 at d7dd5d0 for every line past the old :5305 (sec 2) [V]; the rulings E2-1 kept, kept.

## 0. Purpose, in plain words

E2-1 never tested its headline: the gate skipped both lone platforms before they moved. E2-2 is E2-1 again - the same
ruled order, fixture, tile cache, settings and predictions - on the build that fixes what stopped it, with the
successor-gate floor at the shipped 600 s instead of E2-1's 60 s. THE HEADLINE stays E2-1's: 48 IBCT gets Move To to
the (i) waypoint, then to its destination, the continuation works live (exactly two move-to completions, in order), and
on leg 2 the planner takes the vehicle past the hamlet where the same route on Move Along Route stopped dead in run -2.
THE FALSIFIER stays E1's (P20F). ADDED: the fix's live evidence - no skip, the holds complete at their end time, the
successors dispatch after the holds' TASKCMPLT, and the anchor measured (P31-P32).

WHAT E2-2 CAN AND CANNOT PROVE ABOUT THE FIX, said before the run: with the 600 s floor a hold's successor can be
skipped only by a task-clock step over 300 SIM s, so "no skip" alone is weak evidence (E2-1's >= 45 SIM s anchor loss
would not skip here); the served figure is counted from the timer's OWN anchor and cannot show a late anchor (sec 1(n));
the one live measure that can is P31F's anchor lag, to about one sample step. The fix's proof stays the offline replay
(t1) and the source tripwires (t5).

## 1. Decisions taken from the record

(a)-(j) AS E2-1 sec 1 [A], unchanged: the performers (28ID = one M1A2 platform, 48 IBCT = one M577A2 platform, 1-112 IN =
an aggregate of 6 created AtOrder; T02 an R3-self ATTACK -> AdvanceOnly, ROEHold); DurationScale 0.25; the tasks of the
RULED order 7a9861372f07702fc136f91f63b8bf971650fcc91c20aa62803d73cfd869f5c7 on init 2000e856cb00314064ab6d40c7f7df64cea
098b3466fe70d7614f3d26dc93eec [V: `derive_ironstorm_cuta.py --check` exit 0, three CHECK lines, in this preparation]; the FOLSPT
line not scored; no pre-warm launch; the resolver counted per DISPATCH PASS; one engage-path line per pass; the form
decided once on the final route (T14 MoveToPerVertex N = 2, T02 N = 1, T10 CreateRoute + MoveAlongRoute); the harness
notes. The one change to sec 1(c)'s table: T02 and T14 start "after T01's / T13's TASKCMPLT", with a successor window of
max(600, 300 + 60) = 600 SIM s from the hold's dispatch stamp (E2-1: 360). C1 `--parse-order` with THIS build's exe is
byte-identical to E2-1's 69 lines (Tasks 5; 4 x 1200000 + 1 x 1800000 ms; T14 mapGraphic 7ff48b93 THEN 7351f662; T02
after f7b52ba4, T14 after 37677c40; T10 simStartMs 1200000); C2 `--parse-init ... "Not Set"` identical but for the
vendor's RDTSCP timing line ("Units: 40", "would create ...: 36") [V].
(f') P1 is RECORDED WITHOUT AN EXPECTATION: E2-1 read "36 of 36 ... TERRAIN QUERY" (no re-clamp summary), E1 and -2 read 0
of 36 (one RE-CLAMP summary) [A: their Results].
(k) THE OSM TILE CACHE - AS E2-1 sec 1(k), NOT RE-STAGED [V]: the deployed <exe dir>\preflight-cache (29 raster files,
osm 225, osm-water 225 = 128 tiles + 97 absent markers) hashes, with the registered instrument cache_manifest.ps1 (scratch
u3\laneE2; one "<relative path, lower case>:<sha256>" line per file, sorted ORDINALLY, LF-terminated, hashed as ASCII),
479 files, sha256 682bdea48f43b6f33d0c34339c6ec269efd786965d81ccd66e4aa7f30ac85b6d - before this build, after it (the
Rebuild does not own the folder), after the 26 suites and after the harness re-run, in pwsh 7 and Windows PowerShell 5.1.
THE PREDICTION INSTRUMENT, RE-RUN ON THIS BUILD: the scratch harness (u3\laneE2\harness) rebuilt against the deployed
d7dd5d0 dll (its copy 0cbf7a11..., PV git.d7dd5d0) and run `predispatch <deployed cache> offline`: 45 lines, identical
to E2-1's but one timing figure (15 ms now, 16 ms in E2-1) - T14 / T02 / T10 VERTEX CHECK 2 / 1 / 3, 0 moved, 0 kept, 0
unverified; ROUTE SHIFT - no leg flagged for all three; 0 ObservationReports; 46 cache hits, 0 fetched [V]. Sec 4 P26 is
carried unchanged.
(l) THE REPORT-EVIDENCE GATE (M1b) - AS E2-1 sec 1(l) [A]. E2-1 ran it live for the first time but without a chain line
(neither platform dispatched): 28ID and 48 IBCT unmapped, 1-112 IN mapped from its route join, anchor empty, all three
'via C2SIM-capture', early close at 375.8 s of the 2700 s cap [A: E2-1 Result P30]. The prediction for chained
platforms stands (P30).
(m) THE GATE FLOOR - THE SEAT'S REVISION (a). Vrf__TaskPredecessorTimeoutSeconds = 600, appsettings.json's shipped value
(VrfSettings.cs :660's default too) [V], passed with --env because the wrapper exports 7200 (scripts/RunScenario.sh :320)
and applies --env after it (:324) [V]. E2-1 passed 60, a test-window deviation; it is not repeated: the RL-20260925-01
Q1 race (a task-clock step larger than the margin between two timed walks can still skip a late predecessor's
follow-ons - FINDING t4) is open with the owner. With 600 the window for T02 and T14 is max(600, 300 + 60) = 600 SIM s
from the hold's dispatch stamp while the holds end at 300, so a successor can be skipped only by a single task-clock step
over 300 SIM s between two timed walks - or by an anchor that is not at dispatch. Nothing else reads the floor
(VrfC2SimService.cs :868, the start-up L-CLOCK line, and :3988-4007, the gate [V]); under -StopWhenComplete the window
still closes on completion. The wrapper does not echo --env values, so the live confirmation that 600 arrived is the
app's own lines (P0 (vi)); E2-1's 60 arrived the same way (its L32 and L654 / L662) [V].
(n) WHAT THE TIMER'S OWN LINES CAN AND CANNOT SHOW - and why the brief's served-figure limb is registered as MEDIUM.
[V: scratch u3\laneE2-2\timer_measures.py <runDir> on the three control runs' app logs and captures.] The TIMED
COMPLETION line prints "<S> s of a 300 s Duration served", S = the timer's Elapsed counted from the TIMER'S OWN anchor;
the walk completes a hold at the first walk with S >= 300, so S - 300 is less than the axis advance between the last two
walks, WHATEVER THE ANCHOR IS. The controls, all pre-fix builds, with R the chain's mean ratio defined below: E1 (holds
stamped SIM 634.1) S = 312 at R = 14.3x - 0.8 of a sample step; -2 (SIM 72.3) S = 330 at R = 12.9x - 2.3 steps; E2-1
(SIM 5.6, no successor) S = 315 while the axis moved >= 360 SIM s in the 28.3 WALL s from the hold's L-DISP to its
TASKCMPLT (>= 12.7x) - up to 1.2 steps at that mean, ~2 at the trace's local ~7x there. E2-1's anchor had lost >= 45 SIM
s and its S still read 315. So S cannot see the anchor, and it exceeds one sample step in two of three controls with no
anchor fault (a busy tick thread stretches the walk interval). The brief's limb "a TIMED COMPLETION served figure of Duration plus
more than one sample step (ratio x 1 wall s)" would have STOPPED two of the three controls; it is registered as P31b,
MEDIUM (a recorded miss). WHAT CAN SEE THE ANCHOR: the SUCCESSOR's L-DISP SIMULATION stamp s2 against the hold's s0.
Both stamps are the tick thread's last 1 Hz sample, and with the fix the timer's anchor IS s0's sample: the hold
completes at the first walk whose sample reads s0 + S, the successor dispatches after it, so L = s2 - s0 - S is the sim
advance from the completion walk's sample to the successor's dispatch sample, about R x (w2 - wc) (w2 = the successor's
L-DISP WALL, wc = the hold's TASKCMPLT arrival in the capture, R = (s2 - s0) / (w2 - w0) the chain's mean ratio, w0 the
hold's L-DISP WALL). An anchor LATE by A adds A: A = L - R x (w2 - wc). Controls: E1 A = -10.5 / -8.0 SIM s (T01->T02 /
T13->T14; -0.73 / -0.55 sample steps of R x 1 WALL s), -2 A = +5.7 / +8.0 SIM s (+0.45 / +0.61 steps) - pre-fix builds
whose first walk came within a second of dispatch, i.e. what the fix guarantees; each successor dispatched 0.62-0.92
WALL s after its hold's TASKCMPLT. E2-1 dispatched no successor, but its anchor loss (>= 45 SIM s) is more than three
sample steps at any chain-mean R up to 14.3x, the fastest mean it measured (its first 22 WALL s, SIM 5.6 -> 321.5 at
T10's L-DISP). The estimator's own error is about one sample step (each stamp is the last
1 Hz sample; the walk's sample is up to one step old; R is a mean over the hold). So the anchor limb of P31F is set at
TWO sample steps, and it is scored only when the successor dispatched within 3 WALL s of its hold's TASKCMPLT (P31c) -
a slower dispatch path widens the ratio-mismatch term past the threshold.
(o) THE FIX'S RESIDUAL - THE SEAT'S REVISION (c). The anchor is the axis reading at dispatch, the last 1 Hz sample, which
trails the scenario clock by up to one sample step; so an end time may fire up to one sample step EARLY in scenario
time. A mover arriving inside that sliver logs OVERDUE and then completes on arrival - path (b) by the log: RECORDED as
the residual, not a miss (P11). A hold's TASKCMPLT may come up to one step early: no prediction depends on it.
(p) THE CONTROLS - THE SEAT'S REVISION (d): E2-1 (20260927T205652Z, the SKIP: the same order, fixture, cache and
settings, the pre-fix build 53215e9 and the 60 s floor); E1 (20260927T182140Z, the 1-vertex chain: T14 on Move To
arrived 6.9 m on its original line; its holds served 312 and its successors dispatched at the holds' completion sample);
-2 = LIVE2 (20260927T021020Z, Move Along Route: 48 IBCT stopped among the hamlet's buildings mid-leg 2; holds served 330,
and its gate was also 360: the successors' stamps put the holds' completion at most 347.6 SIM s after their stamp, so
it escaped E2-1's skip by 12.4 SIM s or more). Their timer figures: sec 1(n). The headline's control
is -2 (ONE VARIABLE).

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: --pre-order-gate nav-area --pre-order-gate-timeout 900

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: as E2-1 - the watchdog runs on the SIMULATION clock (--env Vrf__StallClock=sim), window 360
SIM s; armed ends in SIM s from dispatch: T01 300 and T13 300 (no destination - never watched); T02 300 and T14 300
(destination tasks: an unarrived one goes OVERDUE and stays watched, nothing is SENT at that end); T10 450. A stall
verdict is possible from dispatch + 360 SIM s onwards for T02 / T14 / T10 only; T14's vertex-1 handoff is judged like
any other stop. With the fix an end time fires, in scenario time, between one sample step early and one walk interval
late (sec 1(o)); the floor change does not touch the watchdog.

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels.

DEVIATION FROM RECORD: the successor-gate floor is 600 s - appsettings.json's shipped value - not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:320); the computed wait is max(600, 300 + 60) = 600 SIM s, so T02 and T14 are not held two hours behind a late predecessor, and E2-1's 60 s test-window value is not repeated (sec 1(m)).

DEVIATION FROM RECORD: consoles at level 4, not the demo setting of plan P7 ("--object-console 3 --member-console 3"); the house template requires level 4 for a movement run and the movers here are lone platforms.

DEVIATION FROM RECORD: a FALLBACK placement does not stop this run, where DEMO_RUNBOOK sec 0.4 says of anything but N of N from the terrain query "Stop, warm the area, start again"; E1 and -2 record that warming does not change that line on this AO, and the re-clamp that repairs it is scored in P5.

DEVIATION FROM RECORD: the window closes EARLY under -StopWhenComplete, where E1 registered "--no-stop-when-complete --run-secs 2700" (E1 sec 3 E); the reason is to run the runner's report-evidence gate (M1b) live on chained platforms (E2-1 ran it without a chain line), and the 2700 s cap is kept so the run ends even if a task never reaches a terminal report.

EFFECT OF THAT DEVIATION ON THE WINDOW: as E2-1 [A]: the runner closes the window once all 3 taskees and all 5 tasks
have a TERMINAL report, 60 s (-SettleHoldSecs) have passed since ALL-COMPLETE, and every taskee has post-completion
position evidence (Test-EarlyExit). E2-1 closed at 375.8 s of the 2700 s cap; on E1's timeline (T02 / T14 / T10 all
dispatched at the holds' completion, T10's TASKCMPLT 184 WALL s later) E2-2 closes ~5-6 min after the order. What that
costs: anything after the close is unobservable (a late vendor completion - E2-1 saw no T10 move-along completion inside
its window - or T10's members' last convergence); the pre-arrival measures (P6, P20, P20b, P20F, P21, P26, P28, P29) and
every timer measure (P31-P32) are inside the window by construction.

Flag notes (not deviations): --env Vrf__TaskPredecessorTimeoutSeconds=600 (E2-1: =60); everything else as E2-1 -
`--no-gui` explicit; Vrf:PlatformMoveToPerVertex, --model-set, route shift, the pre-flight and the tile cache location
NOT passed (the shipped values).

## 2. What the code emits - log-line shapes (src at d7dd5d0 [V])

The fix adds no log line and changes none [V], so E2-1 sec 2 stands verbatim for every shape. Its VrfC2SimService.cs line
numbers were taken at 0849334; at d7dd5d0 the fix inserted 8 lines after the old :5269 and 3 after the old :5301, so every
cited line up to :5267 is unchanged and every cited line from :5306 on is +11 [V: git diff 0849334 d7dd5d0, e.g. L-MOVETO
:5397 -> :5408, `is ALREADY driving` :5347 -> :5358, L-OSMSET :6136 -> :6147, L-VCHECK :6721 -> :6732, the shift queue
line :6792 -> :6803, L-RATIO :7212 -> :7223, L-VRFDONE :8294 -> :8305]. The shapes the timer predictions score, at
d7dd5d0 [V]:
- L-CLOCK (start-up, :864-868) `TASK CLOCK (R4): C2SIM task times are measured on the SIMULATION clock (Vrf:TaskClock=sim)
  ... Vrf:DurationScale=0.25; a successor waits max(Vrf:TaskPredecessorTimeoutSeconds=<F> s, the predecessor's own
  scaled Duration + Vrf:TaskPredecessorEndMarginSeconds=60 s) ...`.
- L-GATE (:3993, once per gated task at order receipt) `Task '<T>': gated on <pred>, which IS a task in this order and is
  armed to end 300 s after ITS dispatch. It has 86400 s to DISPATCH (...) and then <W> s to COMPLETE; the configured
  Vrf:TaskPredecessorTimeoutSeconds=<F> s (+60 s margin) is the floor. ...`.
- L-SKIP (:4052) `Task '<T>' predecessor <pred> <why>; policy=<p>, unit <name> is <state> -> <action>.` and its SENT
  `SKIPPED: predecessor <pred> <why>; policy=<p>` (:4063).
- L-TIMED (:7491) `TIMED COMPLETION: task '<T>' on <unit> reached its END TIME - <S> s of a <D> s Duration served on the
  simulation clock (C2SIM Duration x Vrf:DurationScale 0.25). ...`; its OVERDUE twin (:7480) `... <S> s of a <D> s
  Duration served on the simulation clock - but the unit has NOT ARRIVED: OVERDUE. ...`.
- L-DISP (:5176) `DISPATCHED <name> task '<T>' (<kind>) at WALL <iso>Z, SIMULATION clock <s> s. ...` - <s> is the tick
  thread's last 1 Hz observation (:5165-5170), the same observation that advanced the task-clock axis the anchor reads.
- L-RATIO (:7223) `SIM/WALL RATIO <r> over the last 60 WALL s (simulation clock now <s> s; ...)`, once a minute.

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run unless marked PREP)

All from Git Bash at the MAIN checkout F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM (HEAD d7dd5d0
when this was written). The order is the main checkout's own data/IRONSTORM_CUTA_Order.xml (7a986137 [V]). The runner
writes its appNumber block into the MAIN checkout's working-tree OPUS_EXECUTION_PLAN.md; that block is carried back to
this branch afterwards (the E1 / E2-1 procedure). Scripts (scratch u3\laneE2-2): golive_checks.ps1, golive_dryrun.sh,
golive_run.sh (the lines below, verbatim), prep_dryrun.sh (PREP), timer_measures.py (harvest).

A0. PRECONDITIONS: (1) the seat's go-live; (2) no other lane is building, running a suite or an agent for the quiet
    period - the offline lanes feat/aggregate-containers and feat/aggregate-authored-units included; (3) the main
    checkout's data/IRONSTORM_CUTA_Order.xml sha256 7a986137...; (4) the deployed build is still d7dd5d0 (exe
    3f2e06b6..., dll 0cbf7a11...) and `git diff --stat d7dd5d0..main -- src` is EMPTY - if main's src has moved on,
    NOTHING is rebuilt: the registered build is what runs and the difference is recorded; if the DEPLOYED hashes differ
    (another lane rebuilt the main checkout's output folders), STOP before E and the seat decides; (5) the deployed
    preflight-cache manifest still hashes 682bdea4... (479 files) - a changed cache is a changed input: STOP before E;
    (6) the persistent holder RtiProbe 45600 (appNo 5170, started 18:20:45Z with a 28,800 s hold -> resigns
    2026-09-28T02:20:45Z) is ALIVE with at least 60 min of hold left - a go-live after 01:20:45Z uses D-NEW below.
A.  Read-only checks, re-run immediately before E: scratch u3\laneE2-2\golive_checks.ps1 (E2-1's script with this
    build's exe / dll hashes, the src diff from d7dd5d0 and the marker 5196): DOTNET_ENVIRONMENT empty; 0 Vrf__
    variables; exe / dll / appsettings / bridge / fixture / order / init hashes as registered; the cache manifest; the
    maple area (4,764 files, 287,120,328 B, .navRuntimeConfig 40b46032...); REST 200 on 18080; rtiexec 47980,
    rtiForwarder 50740, rtiAssistant 30240 and RtiProbe 45600 up and never touched; no vrfNavGenerator, MSBuild, vrfSim,
    vrfGui, VrfC2SimApp, WatchVrf or ListenReports; the marker read (5196 at registration); and `derive_ironstorm_cuta.py
    --check` exit 0 with three CHECK lines. [PREP V 22:32:48Z in pwsh 7 and in Windows PowerShell 5.1: 0 checks FAILED;
    ProductVersion exe 1.0.0+git.d7dd5d0.Release-5.2; holder 45600 then had 228 min of hold left.]
B.  No build (done in the preparation, above).
C.  Order validation (no federate, no appNumber): C1 `--parse-order` and C2 `--parse-init` [PREP V, sec 1]; C3 ONE real
    push to the PRIVATE server, at go-live, after this registration is committed:
        tools/PushInit/bin/Release/net10.0/PushInit.exe data/IRONSTORM_CUTA_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
        tools/PushOrder/bin/Release/net10.0/PushOrder.exe data/IRONSTORM_CUTA_Order.xml 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
    EXPECT both exit 0, "QUERYINIT : 40 Units", and one `ORDER (<n> chars)` echo carrying 5 tasks, 5 Durations and the
    verbs CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT (E2-1: "ORDER (69670 chars)"). Any failure = STOP.
D.  THE HOLDER: REUSE the persistent holder RtiProbe 45600 (appNo 5170, E1's). No new holder, no new claim. The PREP dry
    run recognised it: "RtiProbe pid=45600 started=2026-09-27T18:20:45.000Z is ALREADY RUNNING - a PERSISTENT FEDERATION
    HOLDER from outside this run" [V]. D-NEW (only if A0 (6) fails): E1 sec 3 D verbatim with four fresh numbers from the
    marker (5196-5199 at registration), hand-claimed in Appendix B and committed on this branch BEFORE the holder joins,
    -WhatIf first; exit 1 or 2 = STOP, no blind relaunch (RUNBOOK 9c); sec 5 then shifts by four.
E.  DRY RUN, then THE RUN once, stdout to a FILE, never piped:

        scripts/RunScenario.sh \
          --scenario IronStorm_Centre_52_Nav_AG_maple \
          --init data/IRONSTORM_CUTA_Initialization.xml \
          --order data/IRONSTORM_CUTA_Order.xml \
          --client-id "Not Set" \
          --duration-scale 0.25 \
          --pre-order-gate nav-area --pre-order-gate-timeout 900 \
          --object-console 4 --member-console 4 \
          --stop-when-complete --run-secs 2700 \
          --env Vrf__StallDetection=true \
          --env Vrf__StallClock=sim \
          --env Vrf__TaskPredecessorTimeoutSeconds=600 \
          --sample-threads \
          --no-gui \
          --log runs/launch52/RunScenario-ironstorm-e2-2-<stamp>.log

    The dry run is the same line plus --dry-run and its own log name. PREP DRY RUN [V 2026-09-27T22:32:07Z, exit 0,
    runs/launch52/RunScenario-ironstorm-e2-2-prep-dryrun-20260927T223207Z.log]: order data/IRONSTORM_CUTA_Order.xml;
    "deployed VrfC2SimApp BUILD IDENTITY: git d7dd5d0, written 2026-09-27T22:24:40Z (ProductVersion
    1.0.0+git.d7dd5d0.Release-5.2)"; "model set : EntityLevel <- default"; the fixture loads EntityLevel ("matches
    -ModelSet"); route shift PREDICTED ON; RtiProbe 45600 a PERSISTENT FEDERATION HOLDER; block 5196-5206, "marker would
    advance to: 5207"; "window : 2700s CAP; -StopWhenComplete closes it once all 3 taskee(s) and all 5 task(s) have a
    TERMINAL report ..."; "NOTHING was launched, NO server was contacted, the Appendix B marker was NOT advanced (it still
    reads 5196)"; the main checkout's tracked tree unchanged. THE GO-LIVE DRY RUN must show: the persistent holder
    recognised; build identity d7dd5d0; model set EntityLevel; the block of sec 5 exactly; the window line above. A
    different layout is written into sec 5 BEFORE launch. Defaults kept as E2-1. GATE on teardown: StopVrf52's exit 0 or
    6 (P18); 3, 5 or 7 = STOP.
F.  Post-run, in the FOREGROUND: the post-run inventory (Win32_Process: only rtiexec, rtiForwarder, rtiAssistant, the
    persistent holder and, inside its 900 s hold, the Stage 2h holder; no vrfSim / VrfC2SimApp / WatchVrf /
    ListenReports) ends the quiet period; the hashes and the cache manifest are re-read; then the harvest (sec 6).

QUIET PERIOD (RUNBOOK 0.5.14 item 5), as E2-1: from the launch of E to the post-run inventory (F): no Stop-Process /
taskkill of any kind (StopVrf52's own identity-gated force of the run's own back end is allowed), no build, no suite,
no subagent, no second runner - in this lane or any other. The executor polls the run's own files from the FOREGROUND
with bounded waits and does not end its turn while the run is open. Never touched: rtiexec, rtiForwarder, rtiAssistant,
any RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES: as E2-1 sec 4 [A] - per TASK UUID from the scored run's vrfc2simapp.log in line order, cross-checked against
reports-captured.log; displacement, ARRIVAL, THE STALL MEASURE (P20F), VERTEX 1 REACH, THE TSK ROWS, THE HANDOFF and PATH
SHAPE on watchvrf-trace.csv, exactly as defined there; THE SCORER scratch u3\laneE2\e2_score.py <runDir>, whose controls
(-2, E1) are recorded in E2-1 sec 4, and which on E2-1's own run found T10 as E1 and no DISPATCHED line for T02 / T14
[V: u3\laneE2\score_e2.txt]. NEW FOR E2-2, THE TIMER MEASURES (scratch u3\laneE2-2\timer_measures.py <runDir>; its
control outputs control_timer_E1.txt, control_timer_LIVE2.txt, control_timer_E2-1.txt [V]): per chain (T01 -> T02, T13
-> T14) the hold's L-DISP (w0, s0), its L-TIMED figure S, its TASKCMPLT arrival wc in the capture, the successor's L-DISP
(w2, s2), R = (s2 - s0) / (w2 - w0), L = s2 - s0 - S and A = L - R x (w2 - wc); ONE SAMPLE STEP = R x 1 WALL s (the
app samples the clock once a WALL second); plus every L-RATIO line, the L-CLOCK floor and the two L-GATE lines.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) The persistent holder 45600 alive at launch and recognised PERSISTENT by the runner; the back end JOINS (rtiexec count-grep: no create for its appNo); "READY - joined the federation". (ii) L-CENSUS reads 4 EMPTY shells and 32 platforms. (iii) L-WDOG once, naming the SIMULATION clock; L-CLOCK names SIMULATION and "Vrf:DurationScale=0.25"; the route-shift start-up line once. (iv) The runner's stage 7d gate FIRED (no NOT-READY, no exit 3). (v) Exe 3f2e06b6... / dll 0cbf7a11... / bridge hashes and the cache manifest 682bdea4... as registered, before and after. (vi) THE FLOOR ARRIVED: L-CLOCK reads `Vrf:TaskPredecessorTimeoutSeconds=600 s` and both L-GATE lines (T02, T14) read `and then 600 s to COMPLETE; the configured Vrf:TaskPredecessorTimeoutSeconds=600 s (+60 s margin)`. (READY TO TASK is RECORDED.) | HIGH | Any limb = VOID + STOP (launch, terrain, harness or settings failure - no code verdict). | |
| P1 | PLACEMENT ALTITUDE SOURCE: RECORDED without an expectation (sec 1(f')). | RECORDED | - | |
| P2 | AREA: the FIRST L-AREA row names `NavArea-ground-platform IRONSTORM-CENTRE_maple` and precedes L-ORDER; zero L-AREA rows name any other area; runner "NAV AREA ACQUIRED ... IRONSTORM-CENTRE_maple". WARM/COLD and the gate wait RECORDED. | HIGH | No row before L-ORDER, a gate timeout, or another named area = STOP. | |
| P3 | THE PLANNER RUNS ON THE LONE PLATFORMS: >= 1 L-PROOF line on 48 IBCT's OWN console and >= 1 on 28ID's; >= 1 for a 1-112 IN member. The count on 48 IBCT's console RECORDED (expected 2: one ground-vehicle-move-to per vertex). | HIGH | Zero on either lone platform's console = STOP. | |
| P3b | PLANNING EVIDENCE: >= 2 `Planned path has <N> points.` (N >= 2) on 48 IBCT's console (one per vertex) and >= 1 on 28ID's; every `Node Is destination in nav area?` result RECORDED per Move To. | MEDIUM | Fewer = recorded MEDIUM miss. | |
| P4 | DISPATCH STRUCTURE: exactly one SENT TASKSTRT per uuid (5); T02's L-DISP after T01's SENT TASKCMPLT and T14's after T13's; one "Task 'T10...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25) - it will not dispatch before then." line and T10's L-DISP after it; ZERO "SKIPPED: predecessor" lines. | HIGH | A missing or duplicate TASKSTRT; a successor before its predecessor's TASKCMPLT; any predecessor SKIP = STOP. | |
| P5 | RE-CLAMP: an "is ON the terrain" gate line for each of T02, T14, T10; ZERO "measured OFF the terrain" gate lines; ZERO retired lines (`NOT DISPATCHED YET`, `HELD as`, `REFUSED [`, `BOUND-BUT-NOT-ON-THE-GROUND`). A mover whose terrain profile fell back to Live has no gate line: RECORDED NOT MEASURED. | HIGH | An OFF gate line or a retired line; a mover with a profile reply and no gate line = STOP. | |
| P6 | T02's 28ID and T14's 48 IBCT are each displaced MORE THAN 50 m after dispatch (trace, displacement since dispatch). | HIGH | Either at or under 50 m = STOP. | |
| P6b | T10: at least one 1-112 IN member displaced more than 50 m after T10's L-DISP. | HIGH | None = STOP. | |
| P6c | Per mover: L-PLAN lines, max speed, net displacement, any mesh "not enough (0) points" lines. | RECORDED | - | |
| P7 | CNFPSL HOLDS T01 and T13: each one L-CNFPSL + one IN PLACE line; one STP-866 Observation each in the capture; "it has no destination" armed at 300; NO L-ROUTE, L-MOVETO or MoveToLocation; exactly one SENT TASKCMPLT "(300 s after dispatch)". | HIGH | A move issued for a hold; zero or two TASKCMPLTs; a missing L-CNFPSL; an OVERDUE for a hold = STOP. | |
| P8 | T02 PLATFORM PATH: L-R3SELF for T02 on every dispatch pass and on no other task; ZERO L-FAW, ZERO `FireAtTarget`, ZERO `deferred until the move COMPLETES` lines. | HIGH | Any fire-at-will / FireAt / deferred-engage line; no L-R3SELF for T02; a count unequal to T02's pass count = STOP. | |
| P9 | T14 FOLSPT: exactly one DISTINCT L-FOLSPT naming 'ROEHold'; exactly one L-MOVETO for T14; NO L-ROUTE for T14; an L-ARM-D (destination) at 300. | HIGH | Missing, another ROE, an L-ROUTE for T14, two L-MOVETO for T14, or no move = STOP. | |
| P10 | T10 CRESRV: L-BARE for T10; an L-ROUTE "(4 pts)" for T10 (4 + 4 per ROUTE SHIFTED line for T10 - predicted none) followed by its L-MAR; an L-ARM-D at 450; no IN PLACE line for T10. | HIGH | Any limb failing = STOP. | |
| P11 | COMPLETION PER THE TIME RULES (RL-20260921-09): each mover (T02, T14, T10) takes EXACTLY ONE path - (a) early; (b) late: one L-OVERDUE, nothing SENT at the end, then arrival and one TASKCMPLT on arrival; (c) not arrived by window end; (d) stalled: L-STALL + one TASKABRT "STALLED". On a chained platform the arrival is the app's arrival evidence or its LAST vertex's completion, whichever comes first - never an intermediate vertex's. THE FIX'S RESIDUAL (sec 1(o)): an arrival within one sample step after the end time that the log calls OVERDUE and then completes on arrival is path (b) by the log - RECORDED as the residual, not a miss. | HIGH | Two paths, a TASKCMPLT before start + Duration less one sample step, a TASKCMPLT at an end time for an unarrived mover, or any completion taken from T14's vertex 1 = STOP. | |
| P11b | WHICH PATH: T14 late (b) - arrival evidence within 500 m of the destination after >= 3,669 m of travel incl. the vertex-1 stop, at E1's 9-10 m/s ~370-410 SIM s, past its 300 s end; early (a) would need >= 12.2 m/s sustained. T02 late (b) as E1. T10 late (b) as E1 and E2-1. | MEDIUM | Another path for any = recorded MEDIUM miss. | |
| P12 | REPORT HYGIENE: ZERO suppressed terminal reports; at most one terminal report per uuid; ZERO `task=(none)` SENT lines; capture and log agree; "Reports this run: ... 0 FAILED". | HIGH | Any of these = STOP. | |
| P13 | STALLS / FALLBACKS: every L-STALL, OVERDUE and arrival RECORDED with its displacements. | RECORDED | - | |
| P14 | NO ENGAGEMENT: zero L-ENGAGE / FireAt lines and zero vendor-console "Fire Weapon" lines. | HIGH | Any = STOP. | |
| P16 | CLOCK: every L-RATIO line; each L-DISP's SIMULATION stamp; the sim/wall band; every figure of P31-P32. | RECORDED | - | |
| P17 | RUN HEALTH: no `BACK END LOST`; no WS-runaway exit 6 (runner); no new .dmp / .callstack.log for this back-end pid (names only); VrfC2SimApp exits 0; rtiexec, rtiForwarder, rtiAssistant and every holder untouched. | HIGH | Any limb = VOID. | |
| P18 | TEARDOWN on StopVrf52's exit code inside the runner: 0 or 6, and no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left; the post-force wait line and the runner's post-check RECORDED. | HIGH | Exit 3, 5 or 7 = STOP. | |
| P19 | T10 RESOLVED ROUTE (as E1 and E2-1): T10's DISTINCT L-RESOLVE lines are exactly E1 P19's (c8d9cd1a and 51a59f89 each "1 vertex(es) dropped"; c8d9cd1a joined with no suffix; 51a59f89 "joined <d> m after the previous graphic's end", d in 795-815; cc23071f -> PassagePoint_48_IBCT_SLOT0 appended as the DESTINATION), each recurring once per dispatch pass; zero resolver WARNINGs for T10; L-ROUTE "(4 pts)". | HIGH | Any other distinct line set, a warning, or another point count = STOP. | |
| P19b | T14 RESOLVED ROUTE: T14's DISTINCT resolver lines are exactly the four of E2-1 sec 1(g) - 7ff48b93 and 7351f662 each "1 vertex(es) dropped"; "path from MapGraphicID 7ff48b93-... (line, 1 vertices): 1 vertex(es) joined." with NO gap suffix; "path from MapGraphicID 7351f662-... (line, 1 vertices): 1 vertex(es) joined <d> m after the previous graphic's end." with d in 2890-2915 - each recurring once per dispatch pass (the count of passes RECORDED); zero resolver warnings for T14; the ROUTE SHIFT queue line "(3 vertices)". | HIGH | Another graphic, another drop / join, another destination, a warning, or another vertex count = STOP: the run would not test the registered route. | |
| P20 | HEADLINE: T14's 48 IBCT reaches within 100 m of its destination 54.040348, 23.324206 (closest trace fix after dispatch) by window end - via the (i) waypoint and past the -2 hamlet stop on leg 2. | HIGH | Not within 100 m = STOP. NAMED OUTCOMES, each recorded with the stop point, the STALL measure and the object's last console lines: (1) the P20F condition; (1b) a TRAFFIC STALL (a vehicle within 25 m ahead); (2) a stall AT an OSM feature - above all the -2 outcome recurring, a stop among the hamlet's buildings; (3) a chain FAILED line (PathPlanFailure) and its TASKABRT; (4) still moving at window end; (5) stuck at the (i) waypoint with vertex 2 issued; (6) never dispatched (E2-1's outcome - also a P4 / P31 STOP). | |
| P20b | T02's 28ID reaches within 100 m of PassagePoint_28ID_SLOT0 54.028874, 23.264401 by window end. | HIGH | Not within 100 m = STOP, with the same named outcomes. | |
| P20F | THE FALSIFIER (E1 P20F as AMENDED): NO stall of T14 of >= 60 SIM s with no OSM feature within ~10 m ahead AND no vehicle within 25 m ahead - the vertex-1 handoff included. The same measure RECORDED for T02. | HIGH | The condition met = the frame of RL-20260927-01 is wrong for this vehicle and ground: STOP and ask; no re-run under this registration. | |
| P21 | NO WATER ON ANY TRACK: no POS fix of any object that moved > 50 m falls in an OSM water cell (the z14 osm-water raster of the staged cache, leg_check.load_osm_water); a fix on an absent tile counts UNKNOWN. | MEDIUM | >= 1 wet fix or any fix on an absent tile = recorded MEDIUM miss. | |
| P22 | M1 AND D1 ARE THE BUILD: L-M1-ON exactly once, naming "100 m (Vrf:VertexArrivalRadiusMeters)"; ZERO `MOVE TO PER VERTEX off (` lines; L-MODELSET exactly once at INFO, naming `Vrf:ModelSet='EntityLevel'`. | HIGH | Missing, duplicated, the off line, or a MODEL SET WARNING / another model set = STOP. | |
| P22b | NO UNJUDGED MOVER: ZERO L-JUDGES lines. | MEDIUM | Any = recorded MEDIUM miss. | |
| P23 | THE FORM PER MOVER: T14 exactly one L-MOVETO "MOVE TO PER VERTEX for 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE (VRF_UUID:<u>) - vertex 1 of 2: MoveToLocation (54.014600,23.331500); the other 1 vertex(es) are issued one at a time ..."; T02 exactly one "... 28ID__FRIENDLY_INFANTRY_DIVISION ... - vertex 1 of 1: MoveToLocation (54.028874,23.264401); the other 0 vertex(es) ..."; ZERO L-ROUTE and ZERO L-MAR for T02 or T14; T10 exactly one L-ROUTE and one L-MAR and ZERO L-MOVETO naming 1-112 IN. | HIGH | Any other form, vertex count or first vertex = STOP. | |
| P24 | VENDOR COMPLETIONS: for 48 IBCT exactly TWO L-VRFDONE "VRF task complete: 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE / move-to (success=True)", the first before its L-VTX-ISSUE and the second after it; ZERO of another type or success=False; the trace carries two '48_IBCT/28' "move-to" TSK rows in the same order. For 28ID exactly one "move-to (success=True)" if its track comes within 15 m of its destination (as E1). T10: one "... / move-along (success=True)" if it arrives inside the window (E2-1: none inside its early-closed window - RECORDED). A platform ending 15-100 m short is RECORDED. | HIGH | For 48 IBCT: not exactly two, another type, success=False, or the second missing while its track came within 15 m of the destination = STOP. | |
| P25 | THE CHAIN LINES, in this order and exactly once each: L-VTX "VERTEX CHAIN 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE task 'T14_...': vertex 1 of 2 COMPLETED - the unit is D1 m from it and moved M1 m since dispatch; issuing vertex 2 (RL-20260927-01)." with D1 < 100; L-VTX-ISSUE "... vertex 2 of 2 issued - MoveToLocation (54.040348,23.324206) (RL-20260927-01)."; L-VTX-LAST "... LAST vertex 2 of 2 COMPLETED - the unit is D2 m from it and moved M2 m since vertex 1." with D2 < 100 and NO VACUOUS suffix. For 28ID: one L-VTX-LAST "LAST vertex 1 of 1 COMPLETED ... since dispatch", D < 100 (as E1). ZERO lines of the chain failure family (E2-1 sec 2). The benign lower-case "- swallowed." lines RECORDED. M1 and M2 values RECORDED. | HIGH | A missing, duplicated or out-of-order chain line, a D >= 100, a VACUOUS suffix, or any failure-family line = STOP. | |
| P26 | M2's PRE-DISPATCH STAGE, predicted by the harness (sec 1(k), re-run on THIS build) on the deployed cache: (a) at start-up one L-CACHE "... preflight-cache - 29 file(s), the SHIPPED FALLBACK ... Vrf:PreflightOffline=False" and one L-SCRUB "29 cached tile file(s) checked, all carry a valid TIFF/PNG signature"; (b) exactly one L-OSMSET, at the pre-flight's first use, naming EntityLevel and "osm-water (225 tile file(s), 0 of them 0 bytes = UNKNOWN) and \osm (225, 0 0 bytes)", with ZERO model-set and not-vector-tile warnings; (c) per mover exactly one DISTINCT L-VCHECK: T14 "2 authored vertex(es) checked ...: 0 moved, 0 kept on bad ground, 0 unverified, the rest clear.", T02 "1 ... 0 moved, 0 kept on bad ground, 0 unverified", T10 "3 ... 0 moved, 0 kept on bad ground, 0 unverified"; ZERO VERTEX MOVED / NOT MOVED / UNVERIFIED lines; (d) per mover one DISTINCT L-NOFLAG "ROUTE SHIFT - no leg flagged; the route is unchanged."; ZERO L-OSMWATER, L-OSMUNK, L-BRIDGE, `ROUTE SHIFTED`, `NO ROUTE SHIFT` (incl. RIVER CROSSING), `PRE-DISPATCH applied` and land-cover `WATER ON THE LINE` lines for T02 / T10 / T14; (e) ELEVATION LEVEL ACTUALLY USED L12 on every leg (T14 x2, T02 x1, T10 x3); (f) ZERO pre-flight ObservationReports: no captured ObservationReport whose Marking begins "ROUTE PRE-FLIGHT", "ROUTE SHIFT", "VERTEX MOVED" or "VERTEX NOT MOVED" (the capture's total RECORDED against E1's and E2-1's 39). The harness's numbers behind it, as E2-1: T14 leg 1 slope ratio 0.140, nearest OSM water 60.1 m, leg 2 0.060, 62.2 m; T02 0.105, 141.3 m; T10 0.114 / 0.011 / 0.079, 109.0 / 156.7 m / none; no vertex within 25 m of OSM water or 10 m of a building. NOT a flag, by rule: T14's AUTHORED leg 2 runs THROUGH the -2 hamlet's two footprints (OSM 857519447 and 857519777 at 0.0 m), and on EntityLevel buildings flag no leg (RUNBOOK sec 12a) - getting round them is the planner's job, which is P20. E2-1 held every T10 and start-up limb of this row word for word [A: its Result]. | HIGH | Any limb (a moved or unverified vertex, a flagged leg, an OSM finding, a report, another count) = STOP: the stage did not do on these routes what its own code does offline on the same tiles. | |
| P26b | TILE READS: every L-CENSUS-TILE line and L-TILETOTAL say 0 HTTP FETCH(es), 0 given up, 0 undecodable; the deployed cache manifest is unchanged after the run (682bdea4..., cache_manifest.ps1). Cache HITS RECORDED (harness, sequential: 38 live-equivalent; concurrent workers may read a tile twice; E2-1: 14, T10 alone). | HIGH | A fetch, a changed manifest, an exhausted or undecodable tile = STOP (the registered input changed under the run). | |
| P27 | RECORDED (the seat's list, as E2-1): per Move To (T14 x2, T02 x1) the destination altitude the console reports against the terrain-profile reply for that vertex (E1: +10 m, TerrainClearanceMeters) and its `Node Is destination in nav area?` result - E1 saw T14's destination judged OUTSIDE the nav area; vertex 2 is that same point at the same +10 m, so the same verdict is EXPECTED (MEDIUM), vertex 1 has no expectation; the planner's branch per Move To; PATH SHAPE per leg; the side of the -2 stop the track passes and its closest approach to the hamlet's footprints; the teardown code; the literal type string of every L-VRFDONE. | RECORDED | - | |
| P28 | VERTEX 1 IS REACHED: the L-VTX distance D1 < 100 m (Vrf:VertexArrivalRadiusMeters) AND the closest trace fix to the (i) waypoint is < 100 m, before T14's first fix within 100 m of its destination. | HIGH | Either >= 100 m, or the destination reached first = STOP. | |
| P29 | THE HANDOFF at vertex 1 (RUNBOOK sec 11 NOT YET SEEN LIVE): the stationary run around 48 IBCT's first TSK row lasts < 60 SIM s; its SIM length, the gap between the two TSK rows and the plan lines between them RECORDED. | MEDIUM | >= 60 SIM s = recorded MEDIUM miss here, and P20F then decides whether it is the falsifier. | |
| P30 | THE REPORT-EVIDENCE GATE (-StopWhenComplete, M1b), per E2-1 sec 1(l)'s replay: the runner's per-taskee evidence (manifest earlyExit.reportEvidence) names 48 IBCT's and 28ID's VRF uuid from their MOVE TO PER VERTEX lines and 1-112 IN's from its route join; the anchor is EMPTY for all three (the trace's TSK rows carry truncated markings) - "LAST of 2" does NOT appear; each satisfied 'via C2SIM-capture'; the window closes EARLY ("closing the observation window EARLY ... of the 2700s cap") after the settle hold. | MEDIUM | Any limb = recorded MEDIUM miss (the gate is harness, not the frame); a "LAST of 2 TSK" anchor would mean the marking resolution differs from the replay - recorded, the reason looked for. | |
| P31 | THE TIMER FIX, LIVE (the seat's revision (b)): (a) ZERO "SKIPPED: predecessor" lines and ZERO TASKABRT for T02 or T14 before their L-DISP; (b) for each chain the hold's L-TIMED line precedes its successor's L-DISP in the app log, and in the capture the hold's TASKCMPLT precedes the successor's TASKSTRT; (c) S, s0, w0, wc, s2, w2, R, L and A RECORDED per chain (sec 4 MEASURES). | HIGH | (a) or (b) failing = STOP. | |
| P31b | THE SERVED FIGURE (the brief's limb, registered MEDIUM - sec 1(n)): each hold's L-TIMED reads "<S> s of a 300 s Duration served" with S - 300 less than one sample step (R x 1 WALL s); S RECORDED exactly. Controls: E1 12 at R 14.3x (0.8 step), -2 30 at R 12.9x (2.3 steps), E2-1 15 at >= 12.7x (up to 1.2 steps; ~2 at the trace's local ~7x). | MEDIUM | S - 300 >= R x 1 WALL s = recorded MEDIUM miss: a long walk interval at the end time, NOT an anchor verdict. | |
| P31c | THE SUCCESSORS FOLLOW AT ONCE: each successor's L-DISP within 3 WALL s of its hold's TASKCMPLT arrival (E1 0.62 / 0.80 s, -2 0.74 / 0.92 s). | MEDIUM | Later = recorded MEDIUM miss (its dispatch path's own lines - terrain profile, ROUTE SHIFT - recorded); P31F's lag limb is then RECORDED, not scored. | |
| P31F | THE ANCHOR FALSIFIER (the seat's revision (b), as CHANGED in sec 1(n)): (i) for each chain whose P31c holds, the anchor-lag estimate A = (s2 - s0 - S) - R x (w2 - wc) is at most TWO sample steps (2 x R x 1 WALL s); controls E1 -0.73 / -0.55 steps, -2 +0.45 / +0.61 steps, E2-1's pre-fix loss more than 3 steps; (ii) no predecessor SKIP happens without a task-clock step larger than the window minus the end time (600 - 300 = 300 SIM s here; the brief's "over 60 s" is that difference at E2-1's floor), as P32's instruments see it. | HIGH | (i) A over two sample steps for either chain, or (ii) a SKIP with no such step = the anchor is not at dispatch live: STOP and ask. (A SKIP WITH such a step is the RL-20260925-01 Q1 race - still a P4 / P31 STOP, recorded as the race, not as the anchor.) | |
| P32 | THE RACE PRECONDITION, RECORDED (the seat's revision (b)): every task-clock step over 60 SIM s between two samples that an instrument can see, with its source and time: (1) per hold, S - 300 (a lower bound on the step across its end time); (2) every L-RATIO line (a 60 WALL s mean over 60x makes steps over 60 certain); (3) every pair of consecutive L-DISP stamps (the mean ratio between dispatches); (4) the trace's CON-row clock (the largest SIM advance within any 1 and any 2 WALL s) wherever level-4 consoles print ticks, its COVERAGE RECORDED (E2-1's began at trace t 56.2, after T10's dispatch). NOT OBSERVABLE: a step inside a stretch no instrument covers - above all from the order to the first tick-printing console - and a step made long by a stalled tick thread outside the end-time crossing; the absence of a step there is NOT claimed. | RECORDED | - | |

(P15 is retired into P26, as E2-1.)

WHY P20, P20F, P24, P25, P26 AND P28 ARE HIGH: as E2-1 [A] - the frame's own predictions (FINDING_GROUND_MOVEMENT_
PRACTICE secs 0 and 5.2, UG52 23.1-23.2.2), E1 held them on a 1-vertex chain; VertexChainTracker's deterministic
sequence, pinned offline by --rulings-selftest (446 PASS on this build); P26 the production code's own output on the
deployed tiles (the harness, re-run on this build). WHY P31 AND P31F ARE HIGH: the anchor is deterministic code, pinned
offline by the new section's fail-first replay (t1) and source tripwires (t5) on this build; P31F's threshold sits
between the controls' working values (at most 0.73 step either way) and E2-1's measured loss (more than 3 steps) with
one step of estimator error allowed for. WHY P3b, P11b, P21, P22b, P29, P30, P31b AND P31c ARE MEDIUM: as E2-1 for the first six; P31b because
the controls exceed one step two times in three with no anchor fault; P31c because the successors' dispatch path now
runs M2's pre-flight, which E1's did not.

STOP RULES (as E2-1):
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P17 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- P20F met is reported as the frame's falsifier: STOP and ask, before anything else is run.
- P31F met is reported as the fix's falsifier: STOP and ask, before anything else is run.
- The executor never intervenes in the window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as IRONSTORM_CUTA_E2-<date>-3, with new appNumbers.

ONE VARIABLE: for the headline mover T14, as E2-1 - the TASK a lone ground platform gets, Move To per vertex
(RL-20260927-01) instead of Move Along Route, on the SAME route of the SAME order; the CONTROL is run -2 (20260927T021020Z:
48 IBCT on Move Along Route passed the (i) waypoint and stopped dead among the hamlet's buildings mid-leg 2). Other
differences from that control, named so that no claim rests on them: the build (80f707f against d7dd5d0 - M1, D1/M1b,
A1, M2 and the timer anchor), T02's task (Move To), the window (-StopWhenComplete with a 2700 s cap), the gate floor
(600 against -2's 60), the persistent holder, StopVrf52's post-force wait. Against E2-1 (the timer control) TWO things
differ, both named: the build (the anchor fix; src otherwise identical) and the gate floor (600 against 60) - the anchor
decides WHEN a hold completes (P31F), the floor decides whether a late one could skip its successor (with 600 not before
600 SIM s), so a clean E2-2 does not isolate the fix live; its isolated proof is the offline replay. E1 (20260927T182140Z)
is a reference for the 1-vertex chain, not a control.

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5196 ***` at d7dd5d0, in the worktree and in the main checkout [V]. NOTHING is
claimed by this registration (the preparation phase's hard stop). At the go-live, from the marker M read then (5196
unless another run has moved it), with the persistent holder 45600 REUSED (no holder numbers), the runner's SCORED block
is written by the runner at its Stage 2 (the PREP dry run's layout [V]): M back end (5196), M+1 front end (5197, BURNED,
--no-gui), M+2 WatchVrf pre-check (5198), M+3 WatchVrf trace (5199), M+4 VrfC2SimApp (5200), M+5 RtiProbe 2c (5201), M+6
CreateOne (5202, BURNED unless the oracle gate fails), M+7 to M+10 Stage 2h holder attempts (5203 JOINS; 5204-5206
BURNED); marker -> M+11 (5207). Under D-NEW the holder takes M..M+3 (5196-5199) first and the block starts at M+4 (5200;
marker -> 5211). A launch that aborts burns its whole block. PushInit / PushOrder / ListenReports / StopIface are C2SIM
clients, not federates.

## 6. Harvest (after the run, read-only) and where results go

As E2-1 sec 6 [A]: from the run directory (runs\launch52\last-run-dir.txt) vrfc2simapp.log, reports-captured.log,
c2sim-bus.log, the manifest (earlyExit.reportEvidence for P30), watchvrf-trace.csv, thread-samples.csv, holder logs,
stopvrf logs, the wrapper log; scratch u3\laneE2\e2_score.py <runDir> and u3\laneE2\harvest.py <runDir> (the line
families); tools/analysis/applog_chain.py (`--selftest`) for the chain lines; the harness's P26 lines against the app
log's; the cache manifest re-hashed; NEW: scratch u3\laneE2-2\timer_measures.py <runDir> for P31-P32 and
u3\laneE2\sim_profile.py <runDir> for P32 (4). Vendor sim logs dump the environment in cleartext: count-grep only,
never quoted. Results go to: the Result block below (measurement and implication in separate sentences);
PLAN_MOVEMENT_2026-09-27.md row E2; the RUNBOOK sec 11 M1 bullet's "NOT YET SEEN LIVE" items and the E5 CORRECTION
paragraph (the fix, live); Appendix B annotated from the manifest. ASCII + CRLF.

## 7. What this run does NOT claim

- As E2-1 sec 7 [A]: M2's vertex nudge, water detour, river report or bridge rule working live (none is predicted to
  fire, P26); that the continuation generalises past one intermediate vertex, one vehicle, one waypoint (n = 1); that the
  planner clears hamlets in general, or that per-vertex Move To beats one Move To to the destination; that M1b's
  last-completion anchor works on live markings; anything new about units, the aggregate profile (G1), the full 23-task
  order, the demo server, the GUI or the demo profile; any timing generalisation (n = 1, one host, one fixture).
- That the timer fix is proven live beyond P31F's resolution (about one sample step): the 600 s floor removes the skip
  that exposed the defect, so the fix's proof stays the offline replay (t1) and the tripwires (t5).
- Anything about the RL-20260925-01 Q1 race (FINDING t4): the holds have no destination and the 600 s window needs a
  step over 300 SIM s; P32 only records what the instruments can see.

## Result (written after the harvest, never from a live read)

Written 2026-09-27 ~23:55Z by lane E2 (session 5fc25950) from the harvested files of the scored run 20260927T231937Z_run
(main checkout runs\): vrfc2simapp.log (582,406 lines; L = its line numbers), reports-captured.log (C = record number and
arrival stamp, UTC; 1,345 records), run-manifest.json, watchvrf-trace.csv (t = trace seconds from the WatchVrf join at
23:21:33.579Z), stopvrf.stdout.log (S), the runner log runs\launch52\RunScenario-ironstorm-e2-2-20260927T231936Z.log (R)
and the rtiexec log (count-grep only). Instruments, scratch u3\laneE2-2: timer_measures.py, verify_e22.py, clock_steps.py,
moveto_alt.py, console_window.py, recovery_count.py, rtiexec_count.py; u3\laneE2: e2_score.py, harvest.py,
sim_profile.py, replay_evidence.ps1. Vendor sim logs were not opened. Registration rows are cited by ID and by their line
in this file (reg. L329-L372, before this Result was added).

GO-LIVE, as sec 3 wrote it, with the seat's one change to A0 (4) [V]. THE SOURCE CHECK was taken against the commit the
deployed build came from: `git diff --stat d7dd5d0 d7dd5d0 -- src` is EMPTY; ProductVersion 1.0.0+git.d7dd5d0.Release-5.2
with no +DIRTY; exe 3f2e06b6 / dll 0cbf7a11 and all eleven output trees as at the build (22:24:40Z). DRIFT, RECORDED AND
NOT DEPLOYED: main had moved to e48162b; `git diff --stat d7dd5d0 e48162b -- src` = 14 files, +2886 / -24 (C1 5561d90 and
C2 6e5fe67: VrfBridge.cpp, VrfFacade.cpp / .h, Container*.cs, ObjectTypeResolver.cs, Program.cs, UnitTranslator.cs,
VrfC2SimService.cs, VrfSettings.cs, appsettings.json) - merged after the registered build; not deployed (the deployed
appsettings.json is still 5e85e4b5; src's is now 336c8ac4). Of the runtime inputs only data/unit-type-map-52-aggregate.json
and data/unit-composition-52-aggregate.json changed, and an EntityLevel run reads neither (R: "type map :
data/unit-type-map-52.json - declares EntityLevel"; no reader of the composition file in src at d7dd5d0); scripts, tests
and config are unchanged since d7dd5d0. A (golive_checks.ps1) 23:17:24Z and again 23:18:58Z: 0 checks FAILED (the
hashes, cache manifest 682bdea4 / 479 files, the maple area, REST 200, holder 45600 with 182 min left, marker 5196);
`derive_ironstorm_cuta.py --check` exit 0, three CHECK lines. Quiet period: every other lane had reported; two idle build
servers from another lane were present and left alone (VBCSCompiler 36056 from 22:55:09Z, an MSBuild reuse node 49048
from 23:03:01Z; 0 CPU seconds over 10 s). C3 23:18:15Z: PushInit exit 0 ("QUERYINIT : 40 Units"), PushOrder exit 0, one
"ORDER (69670 chars)" echo with 5 tasks, 5 Durations and CNFPSL/ATTACK/CRESRV/CNFPSL/FOLSPT. D: holder 45600 reused
(R78). E: dry run 23:18:59Z exit 0 with sec 5's layout exactly (5196-5206, marker -> 5207, build d7dd5d0, EntityLevel);
THE RUN once, launched 23:19:36Z; the ORDER reached the bus at 23:22:07.992Z (R256); the window closed EARLY at
23:27:58.359Z, 320.3 s of the 2700 s cap (R275); runner exit 0 at 23:30:34Z. F: post-run inventory 23:30:51Z - only
rtiAssistant 30240, rtiexec 47980, rtiForwarder 50740, RtiProbe 45600 and the run's Stage 2h holder RtiProbe 32536
(inside its 900 s hold); exe / dll / appsettings / bridge / fixture / order / init hashes and the cache manifest
unchanged; marker 5207. RL-20260927-06 (ruled after this registration): under its automatic selector (package D2, not
built) this order's tasked echelons (a division and a brigade) would run on the aggregate model set only; this run is the
registered EntityLevel run with single-platform performers, launched at the seat's go-live.

### VERDICT: SCORED. Every HIGH prediction held on the registered line families; one MEDIUM miss, P31b (the served figure - a long walk interval, as sec 1(n) warned, not an anchor verdict); one reading flagged for the seat, P19b (a TaskGeometryResolver warning outside the registered L-RESOLVE family, present identically in E1 and -2). THE HEADLINE HELD: 48 IBCT drove Move To to the (i) waypoint, the chain issued vertex 2 when that completed, and the second Move To took it round the west side of the -2 hamlet stop to 6.9 m from its destination - two move-to completions, in order, no stall, no blockage. THE FALSIFIER P20F did not fire. THE TIMER FIX HELD live: both holds completed at their end time, and both successors dispatched 0.75 / 0.94 WALL s after the holds' TASKCMPLT, on the completion's own clock sample; no SKIP.

| # | Reg. | Verdict | Evidence |
|---|---|---|---|
| P0 | L329 | PASS | (i) R78 holder 45600 "a PERSISTENT FEDERATION HOLDER"; rtiexec count-grep 19:19:30-19:31:00 local (23:19:30-23:31:00Z): 12 "Could not create federation MAK-ONE-2025, because it already exists.", 12 JoinConfirm, 6 join lines, 0 federations created; L49 "READY - joined the federation" once. (ii) L221 "4 unit(s) created as EMPTY shells ... 32 platform(s) created in full". (iii) L55 "STALL WATCHDOG: the 360 s no-progress window is measured on the SIMULATION clock." once; L32 TASK CLOCK (R4) on the SIMULATION clock, "Vrf:DurationScale=0.25"; L22 LATERAL ROUTE SHIFT ON once. (iv) R246 "NAV AREA ACQUIRED after 0s of gate"; no NOT-READY, no exit 3. (v) hashes and the cache manifest identical before (23:18:58Z) and after (23:30:51Z) [V]. (vi) L32 "max(Vrf:TaskPredecessorTimeoutSeconds=600 s, ..."; L630 (T02) and L638 (T14) "... and then 600 s to COMPLETE; the configured Vrf:TaskPredecessorTimeoutSeconds=600 s (+60 s margin) is the floor". RECORDED: L592 READY TO TASK "36 of 36 ... after 7.8 s". |
| P1 | L330 | RECORDED | L333 "36 of 36 create altitude(s) came from the TERRAIN QUERY, 0 from the FALLBACK" (as E2-1, so no PLACEMENT RE-CLAMP summary); L700 "1 of 1 ..." (the order-time materialization of 1-112 IN). |
| P2 | L331 | PASS | First L-AREA L594 (`NavArea-ground-platform IRONSTORM-CENTRE_maple`) before L-ORDER L626; all 70 L-AREA rows name the maple area; R246 "NAV AREA ACQUIRED after 0s of gate"; R248 "first placement ... -> area row: 0s -> WARM file cache". |
| P3 | L332 | PASS | 48 IBCT's own console: 2 L-PROOF lines, L3151 (vertex 1) and L74193 (vertex 2) - the recorded expectation of 2; 28ID L3249; 1-112 IN members L3441 (M577A2 1), L3443 (M1A2 1) and others (45 L-PROOF lines in all). |
| P3b | L333 | PASS | 48 IBCT: L3859 "Planned path has 14 points." (vertex 1); L74075 "Planned path has 1 parts." then L74581 "Planned path has 27 points." (vertex 2); 28ID L4115 "Planned path has 50 points.". RECORDED per Move To: vertex 1 L3139 "Node Is destination in nav area?: success"; vertex 2 L73609 / L73611 "Condition false." / "fail in action Is destination in nav area?"; T02 L3237 success. |
| P4 | L334 | PASS | Five SENT TASKSTRT, one per uuid: L654 T01, L682 T13, L893 T10, L907 T14, L1203 T02. T14's L-DISP L905 after T13's SENT TASKCMPLT L721; T02's L-DISP L1201 after T01's L727; L636 "Task 'T10...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25) - it will not dispatch before then." once, before T10's L-DISP L891. ZERO "SKIPPED: predecessor" lines (the one "SKIPPED" in the log, L743, is the route-extent rule (c) line). Capture: C150 / C151 TASKCMPLT (T13 / T01, 23:22:29.356Z / .357Z) before C152 T10, C153 T14 and C154 T02 TASKSTRT (23:22:30.107Z / .112Z / .302Z). |
| P5 | L335 | PASS | L885 (T10, gap 0.3 m), L901 (T14, 0.0 m), L1197 (T02, 0.0 m) "PLACEMENT RE-CLAMP gate: ... is ON the terrain"; 0 OFF gate lines; 0 retired lines. |
| P6 | L336 | PASS | 28ID displaced 5,341.4 m, 48 IBCT 2,424.8 m after dispatch (trace, the farthest fix from the dispatch fix). |
| P6b | L337 | PASS | 1-112 IN's proxy and all six members displaced 2,543-2,677 m. |
| P6c | L338 | RECORDED | 48 IBCT: L-PLAN 14 points (vertex 1), 1 part + 27 points (vertex 2); max speed 10.8 m/s on the CON clock, median of moving samples 9.9 m/s; path 4,178 m against the authored 4,169 m. 28ID: 50 points; max 11.3 m/s, median 9.8 m/s; path 5,304 m. T10 members: 39 L-PLAN lines. 0 mesh "not enough (0) points" lines. |
| P7 | L339 | PASS | T01 L658 + L660, T13 L686 + L688 (one L-CNFPSL + one IN PLACE each); L656 / L684 "end time armed at 300 s ... it has no destination"; exactly one TASKCMPLT each, L727 (T01) / L721 (T13) "(300 s after dispatch)"; the capture's two STP-866 observations; no move and no OVERDUE for a hold. |
| P8 | L340 | PASS | L-R3SELF L751, L875, L1199, all T02; T02 passes = 1 + L753 (route-shift check queued) + L877 (terrain profile request 47) = 3, E1's count; 0 fire-at-will, 0 FireAtTarget, 0 deferred-engage lines. |
| P9 | L341 | PASS | One L-FOLSPT L903 "rules of engagement as ordered ('ROEHold')" (L741 / L857 are the V4b geometry lines); one L-MOVETO L911; 0 L-ROUTE for T14; L909 "end time armed at 300 s ... it has a destination". |
| P10 | L342 | PASS | L-BARE L769 / L841 / L889 (one per pass); L897 "CreateRoute '...' (4 pts) for 1-112_IN/..."; L925 MoveAlongRoute issued for VRF_UUID:1d7b98d5...; L895 armed at 450 s "it has a destination"; no IN PLACE line for T10; 0 ROUTE SHIFTED lines. |
| P11 | L343 | PASS | Each mover took path (b), once. T02: L135013 "331 s of a 300 s Duration served ... NOT ARRIVED: OVERDUE", L218315 ARRIVAL EVIDENCE (1/1 within 500 m, nearest 222 m), L218319 TASKCMPLT "complete on arrival". T14: L135015 OVERDUE (331 of 300), L179085 ARRIVAL EVIDENCE (1/1, nearest 341 m, 438.2 SIM s after dispatch), L179089 TASKCMPLT. T10: L191235 OVERDUE (475 of 450), L534881 (6/6, nearest 361 m), L534885 TASKCMPLT. No TASKCMPLT before start + Duration; T14's came from arrival evidence 341 m from its LAST vertex, not from vertex 1 (L72737 issued vertex 2 and sent nothing). The residual's sliver did not occur (arrivals 138-1,084 SIM s after the end times). |
| P11b | L344 | PASS | T14, T02 and T10 all late (b). T14's arrival evidence came at 438.2 SIM s after dispatch, past the ~370-410 SIM s of the arithmetic (the vertex-1 stop, the turn and a 4,178 m path). |
| P12 | L345 | PASS | 0 SUPPRESSED; one terminal report per uuid (5 TASKCMPLT); 0 `task=(none)`; capture = log (5 TASKSTRT / 5 TASKCMPLT; 1,345 records); L582401 "Reports this run: 1345 delivered, 0 FAILED". |
| P13 | L346 | RECORDED | No L-STALL. OVERDUE: T02 / T14 at 331 of 300 s (L135013 / L135015), T10 at 475 of 450 s (L191235). Arrivals 438.2 (T14), 550.8 (T02) and 1,534.1 (T10) SIM s after dispatch (L179085, L218315, L534881, the app's own figures; the trace's CON clock gives 434.9 and 547.7 for the first two). |
| P14 | L347 | PASS | 0 L-ENGAGE / FireAt / "Fire Weapon" lines. |
| P16 | L348 | RECORDED | L-DISP SIMULATION stamps: T01 / T13 4.6 s (L652, L680); T10 / T14 / T02 326.3 s (L891, L905, L1201). L-RATIO 8.511 (L84513, sim 516.7 s), 7.267, 7.715 and 7.704 in the window; 9.745 and 12.744 in the shutdown tail (L582371, L582397). Mean over the holds from the stamps: 321.7 SIM s in 21.886 WALL s = 14.7x. Timer figures: P31-P32. |
| P17 | L349 | PASS | 0 BACK END LOST, 0 "Tick phase ... FAILED"; runner exit 0; no .dmp / .callstack.log under C:\MAK\vrforces5.2d or C:\MAK\logs newer than 23:19Z (names only); R284 "VrfC2SimApp exited with code 0 (clean resign)"; rtiexec 47980, rtiForwarder 50740, rtiAssistant 30240 and holder 45600 untouched (inventory 23:30:51Z). RECORDED: the app log's last lines L582404-L582405 `fail: C2SIM.C2SIMSDK[0]` "STOMP block reading cancelled" at shutdown, after the tile total (as E2-1). |
| P18 | L350 | PASS | R294 StopVrf EXIT=6: the graceful close (S24, taskkill without /F) was refused within 120 s (S28-S32), then S33 "FORCED ... Stop-Process -Id 26944 -Force" by identity. RECORDED: S35 "VR-Forces 5.2d is down ONLY BECAUSE the run's own back end was FORCED (pid 26944) after its graceful close was refused. Exit 6." - no "still exiting" line; R298 "no VR-Forces processes remain"; R301 the Stage 2h holder 32536 still joined, EXPECTED. |
| P19 | L351 | PASS | L755 / L757 "1 vertex(es) dropped" (c8d9cd1a, 51a59f89); L759 c8d9cd1a "(line, 1 vertices): 1 vertex(es) joined." (no suffix); L761 51a59f89 "... joined 805 m after the previous graphic's end"; L763 cc23071f -> PassagePoint_48_IBCT_SLOT0 "(point, 1 vertex): appended as the route's DESTINATION"; the same distinct lines on the second pass (L827-L835); 0 warnings of any kind for T10; L897 "(4 pts)". |
| P19b | L352 | PASS on the registered L-RESOLVE family; FLAGGED for the seat | L729 / L731 "1 vertex(es) dropped" (7ff48b93, 7351f662); L733 "path from MapGraphicID 7ff48b93-... (line, 1 vertices): 1 vertex(es) joined." (no suffix); L735 "path from MapGraphicID 7351f662-... (line, 1 vertices): 1 vertex(es) joined 2906 m after the previous graphic's end."; the same four on the second pass (L845-L851) - two resolver passes (order receipt; route-shift re-entry; the terrain-profile re-entry does not re-resolve); L745 "ROUTE SHIFT check queued ... (3 vertices)"; 0 of the family's registered warnings (-2 sec 2, carried by E1, E2-1 and this file: :473 NOT continuous, :500 extra destinations, :516 RETURNS ... TRUNCATED). FLAG: the resolver class also logged, at WARN, L739 / L855 "the MapGraphicID geometry and the embedded Location on this task END 2428 m apart (more than 1000 m): the order and the initialization disagree about where this task is, and the MapGraphicID was used." (TaskGeometryResolver.cs :260-263; its INFO twin L737 / L853). Read as "any warning from TaskGeometryResolver", P19b is a HIGH MISS by its MISS column's "a warning". The route resolved exactly as registered, and the same two warnings are in the control -2 (L1347, L1523) and in E1 (L1825, L1995), whose Results recorded "0 warnings" for T14. The reading is the seat's (E1's precedent for a wording defect in this row). |
| P20 | L353 | PASS | 48 IBCT's closest fix to 54.040348, 23.324206: 6.9 m (t=122.5; final 6.9 m); first fix within 100 m at t=120.5 = 64.0 WALL s / 452.6 SIM s after dispatch (CON clock); via the (i) waypoint (closest 4.7 m, t=79.4) and past the -2 stop (closest fix 109.2 m from it, 56.5 m WEST of the line). None of the named outcomes. |
| P20b | L354 | PASS | 28ID's closest fix 0.9 m (final 0.9 m); within 100 m at t=134.9 = 78.2 WALL s / 560.5 SIM s after dispatch. |
| P20F | L355 | NOT MET - the falsifier did not fire | T14: 0 stationary runs (>= 2 consecutive fixes within 5 m) between its first movement and its arrival, so none of >= 30 or >= 60 SIM s; the vertex-1 handoff is a 1-fix run (P29). T02: 0 stationary runs. |
| P21 | L356 | PASS | 0 wet fixes over the 9 objects that moved > 50 m (48 IBCT, 28ID, 1-112 IN and its six members); 0 fixes on absent tiles. |
| P22 | L357 | PASS | L24 "MOVE TO PER VERTEX ON (Vrf:PlatformMoveToPerVertex, RL-20260927-01; ...) ... farther than 100 m (Vrf:VertexArrivalRadiusMeters) ..." once; 0 "MOVE TO PER VERTEX off (" lines; L26 "MODEL SET for task judging: Vrf:ModelSet='EntityLevel' -> EntityLevel" once, at INFO. |
| P22b | L358 | PASS | 0 TASK JUDGES lines. |
| P23 | L359 | PASS | L911 "MOVE TO PER VERTEX for 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE (VRF_UUID:f0e8401e-d813-184a-9480-e541a4b64899) - vertex 1 of 2: MoveToLocation (54.014600,23.331500); the other 1 vertex(es) are issued one at a time ..."; L1207 "... 28ID__FRIENDLY_INFANTRY_DIVISION (VRF_UUID:86d99038-...) - vertex 1 of 1: MoveToLocation (54.028874,23.264401); the other 0 vertex(es) ..."; 0 L-ROUTE and 0 L-MAR for T02 or T14; T10 one L-ROUTE (L897), one L-MAR (L925), 0 L-MOVETO naming 1-112 IN. |
| P24 | L360 | PASS | 48 IBCT exactly two "VRF task complete: 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE / move-to (success=True)": L72735 (before the L-VTX-ISSUE L72787) and L192963 (after); the trace's two '48_IBCT/28' "move-to" TSK rows, t=77.9 and t=122.2, in that order; 0 of another type or success=False. 28ID one move-to (success=True), L227149 (track within 0.9 m). T10 one ".../ move-along (success=True)", L582359 (TSK t=287.0, inside the window). |
| P25 | L361 | PASS | L72737 "VERTEX CHAIN 48_IBCT/... task 'T14_...': vertex 1 of 2 COMPLETED - the unit is 1 m from it and moved 1266 m since dispatch; issuing vertex 2 (RL-20260927-01)."; L72787 "... vertex 2 of 2 issued - MoveToLocation (54.040348,23.324206) (RL-20260927-01)."; L192965 "... LAST vertex 2 of 2 COMPLETED - the unit is 7 m from it and moved 2904 m since vertex 1. The chain ends ..." (no VACUOUS suffix); L227151 28ID "LAST vertex 1 of 1 COMPLETED - the unit is 1 m from it and moved 5341 m since dispatch."; 0 chain-failure-family lines (the one "VACUOUS" in the log is L24's own prose). RECORDED: the benign "- swallowed." lines L192967, L227153, L582361; M1 1,266 m, M2 2,904 m. |
| P26 | L362 | PASS | (a) L18 "... preflight-cache - 29 file(s), the SHIPPED FALLBACK ... Vrf:PreflightOffline=False."; L20 "29 cached tile file(s) checked, all carry a valid TIFF/PNG signature (SF3).". (b) L785 once: "ROUTE PRE-FLIGHT MODEL SET EntityLevel ... osm-water (225 tile file(s), 0 of them 0 bytes = UNKNOWN) and \osm (225, 0 0 bytes) ..."; 0 model-set / not-vector-tile warnings. (c) L807 T14 "VERTEX CHECK (EntityLevel) - 2 authored vertex(es) checked against OSM water and buildings: 0 moved, 0 kept on bad ground, 0 unverified, the rest clear.", L861 T02 "1 ...", L805 T10 "3 ..." - the harness's lines word for word; 0 VERTEX MOVED / NOT MOVED / UNVERIFIED. (d) L825 T14, L867 T02, L823 T10 "ROUTE SHIFT - no leg flagged; the route is unchanged."; 0 OSM WATER, could-NOT-be-read, bridge, ROUTE SHIFTED, NO ROUTE SHIFT, PRE-DISPATCH applied and WATER ON THE LINE lines. (e) L811 T14 "L12 x2 over 2 leg(s)", L863 T02 "L12 x1", L809 T10 "L12 x3". (f) 0 captured ObservationReports with a pre-flight marking; 39 ObservationReports in all, as E1 and E2-1. |
| P26b | L363 | PASS | L869 census "39 cache HIT(s), 0 HTTP FETCH(es)"; L582403 run total "39 cache HIT(s), 0 HTTP FETCH(es), 0 tile(s) given up on after 3 attempts, 0 undecodable body(ies)"; the cache manifest unchanged after the run (682bdea4, 479 files). 39 hits against the harness's 38 sequential. |
| P27 | L364 | RECORDED - the MEDIUM expectation HELD | Destination heights (the console's geocentric Move-To, converted, against the terrain-profile replies L899 / L1195, terrain + 10 m): vertex 1 L921 149.9 m = 149.9; T02 L1263 151.5 m = 151.5; vertex 2 L72935 142.9 m = 142.9 (E1's T14 destination, the same point: 142.9 m). Nav-area verdicts: vertex 1 success (L3139); vertex 2 FAILURE (L73609 / L73611), the point and height E1 saw fail; T02 success (L3237). Branch: vertex 1 and T02 "CreateOffRoadSegment" -> 14 / 50 points; vertex 2 "Plan off feature path", "Not using roads for move planning.", "Planned path has 1 parts." (L74075), "Is PathPart outside nav area?" false, "Calc off road nav path part" -> 27 points (L74581) - E1's branch for this point - then "Is heading close to route" false -> "Turn to route", "Setting ordered speed: 3mps" (L74599-L74601). Along both legs "Is path blocked?" was false on all 458 of 48 IBCT's checks (560 of 28ID's); 0 global-replan, skirt or back-up nodes started. PATH SHAPE: leg 1 at most 80.4 m right of the line (537 m along); leg 2 at most 132.4 m LEFT (west) of it (1,473 m along); the -2 stop passed on its WEST side (closest fix 109.2 m from it; 25.8 m from the nearest building within 250 m of it); leg 2's closest approach to any OSM building 6.2 m, to OSM water 37.7 m; leg 1's nearest water 145.7 m. Teardown code 6. L-VRFDONE types: "move-to" x3, "move-along" x1. |
| P28 | L365 | PASS | L72737 D1 = 1 m; closest trace fix to the (i) waypoint 4.7 m (t=79.4); first fix within 100 m of it at t=77.3, before T14's first fix within 100 m of its destination (t=120.5). |
| P29 | L366 | PASS | The stationary run around the first '48_IBCT/28' TSK row (t=77.9) is 1 fix (t=77.3, 29.9 m from the waypoint) = 0.0 SIM s. RECORDED: TSK gap t=77.9 -> 122.2 = 44.3 WALL s (150.1 -> 464.6 SIM s after dispatch); plan lines between them L74075 "1 parts", L74581 "27 points". The console times the handoff (console clock): the vertex-2 move-to began at 497.33 (L72933), about 0.3 s after the vertex-1 TSK row (497.0 on the CON clock); its first tick found "Is Vehicle Stopped?" true at 498.43; the path was planned at 500.56, and the vehicle turned to the route heading at 3 m/s before driving on - a stop of a few SIM s, below the trace's ~2 WALL s sampling. |
| P30 | L367 | PASS | Manifest earlyExit.reportEvidence: 28ID vrfUuid 86d99038... and 48 IBCT f0e8401e... (from their MOVE TO PER VERTEX lines), 1-112 IN 1d7b98d5... (the route join); anchor null for all three; "LAST of 2" absent from the manifest and the runner log; each satisfied via C2SIM-capture; R275 "closing the observation window EARLY at t+320s ... of the 2700s cap" after the 60 s hold (63.2 s). The offline replay (replay_evidence.ps1) gives the same mapping, anchors and 'via'. |
| P31 | L368 | PASS | (a) 0 "SKIPPED: predecessor" lines; 0 TASKABRT in the run. (b) L717 (T13) before T14's L-DISP L905, L723 (T01) before T02's L-DISP L1201; C150 before C153, C151 before C154. (c) T13 -> T14: s0 4.6, w0 23:22:08.217, S 322, wc 23:22:29.356 (C150), s2 326.3, w2 23:22:30.102, R 14.70, L -0.3, A -11.3 SIM s. T01 -> T02: s0 4.6, w0 23:22:08.212, S 322, wc 23:22:29.357 (C151), s2 326.3, w2 23:22:30.296, R 14.57, L -0.3, A -14.0 SIM s. |
| P31b | L369 | MEDIUM MISS (as sec 1(n) allowed: not an anchor verdict) | Both holds "322 s of a 300 s Duration served" (L717, L723): S - 300 = 22 SIM s against one sample step of 14.6 / 14.7 SIM s = 1.5 steps. |
| P31c | L370 | PASS | T14 0.746 WALL s and T02 0.939 WALL s after the holds' TASKCMPLT (C150 / C151). |
| P31F | L371 | NOT MET - the falsifier did not fire | (i) A = -0.77 steps (T13 -> T14) and -0.96 steps (T01 -> T02), at most two allowed; (ii) no SKIP occurred. |
| P32 | L372 | RECORDED | (1) The holds' S - 300 = 22 SIM s (more than 21.5 after the F0 rounding): the task clock's step across their end time. The same measure outside the registered list: T02 / T14 331 of 300 (a step of more than 30.5 SIM s), T10 475 of 450 (more than 24.5). (2) L-RATIO: see P16; none implies a step over 60. (3) L-DISP pairs: 4.6 -> 326.3 SIM s over 21.886 WALL s = 14.7x mean; no other consecutive dispatches. (4) The trace's CON clock covers t 56.0-233.0 (23:22:29.6Z-23:25:26.6Z; console clock 344.7-1,669.5 s) with holes of at most 0.2 WALL s: its largest advance is 9.7 SIM s within 1 WALL s (t 58.1-59.1) and 17.1 SIM s within 2 WALL s (t 139.2-141.2); no step over 60. NOT OBSERVABLE: the holds' whole span, from the order (23:22:07.992Z) to the first tick-printing console at the movers' dispatch (t 56.0). No instrument saw a task-clock step over 60 SIM s; that none occurred in the uncovered span is not claimed. |

THE TIMER FIX, LIVE [V unless marked]. The order met a scenario clock at 4.6 s (the holds' L-DISP, L652 / L680) - the
young-clock condition that broke E2-1 (5.6 s), again on a warm machine (P2: a 0 s gate). Both holds completed at
23:22:29.356-.357Z (C150, C151) reporting "322 s of a 300 s Duration served" (L717, L723). T10, T14 and T02 dispatched
0.74-0.94 WALL s later under ONE SIMULATION stamp, 326.3 s (L891, L905, L1201). The successors' stamp minus the holds'
stamp is 326.3 - 4.6 = 321.7 SIM s, equal to the served 322 within its F0 rounding: L = -0.3. A dispatch stamp is the
tick thread's last 1 Hz observation, the same observation that advances the task-clock axis (sec 2), so the successors
dispatched on the completion walk's own sample and the served figure counted from the holds' dispatch sample. An anchor
one sample late (~14.7 SIM s at the 14.7x of that span) would have printed about 307; E2-1's first-walk anchor, >= 45
SIM s late, printed 315 while its successors were skipped. The registered estimator A (-0.77 / -0.96 steps) carries its
known bias here: it charges R x (w2 - wc) for wall time in which the stamp did not advance. MEASUREMENT: on build d7dd5d0,
with the order in the scenario's first seconds, the end-time timer counted from the holds' dispatch sample, the holds
completed at their end time, and their successors dispatched on the completion's own sample. DESIGN IMPLICATION,
separately [A]: E2-1's anchor defect is closed live (n = 1). The residual that RL-20260927-05 addresses remains: here
the task clock stepped more than 30 SIM s across the movers' end times (P32 (1)), half the 60 s margin, at ~7-8x; no
mover in this order has a successor, so it cost nothing.

THE HEADLINE [V unless marked]. 48 IBCT (T14), a lone M577A2, got "MOVE TO PER VERTEX ... vertex 1 of 2: MoveToLocation
(54.014600,23.331500)" (L911) and ran the vendor planner on its own console (L3151; a 14-point path). It reached the (i)
waypoint 150.1 SIM s after dispatch: the vendor's move-to completed (L72735; TSK t=77.9) with the vehicle 1 m from the
vertex by the app's own read (L72737), and the interface issued vertex 2 (L72787, 50 log lines later). The second Move
To found the vehicle stopped, judged the destination outside the nav area as in E1, planned 27 points off the feature
planner's single part, turned to the route heading and drove north. On leg 2 the track bowed up to 132 m WEST of the
authored line and passed the point where the same vehicle had stopped dead on Move Along Route in run -2 at 109 m, 25.8 m
from the nearest building beside it. "Is path blocked?" never answered true, and no recovery node started. The second
move-to completed (L192963; TSK t=122.2) 7 m from the destination by the app and 6.9 m by the trace; arrival evidence
had already released the task as late (L179085-L179089). 28ID (T02), a 1-vertex chain, ended 0.9 m from its
destination; 1-112 IN (T10) arrived as in E1 and E2-1. MEASUREMENT: in one run, one M577A2 on Move To per vertex
completed a 2-vertex chain (1,266 m, then 2,904 m) and ended 6.9 m from its destination, passing the -2 hamlet stop on
its west side with no blockage and no stall; the vertex-1 stop lasted a few SIM s (a re-plan and a turn). DESIGN
IMPLICATION, separately [A]: the frame of RL-20260927-01 held on the ground and vehicle where the literal executor
stopped (n = 1: one vehicle, one intermediate vertex, one fixture); the per-vertex continuation is now seen live once.

UNEXPLAINED OR OPEN:
- Why the vendor judges this destination point OUTSIDE the nav area at 10 m above the terrain while the (i) waypoint (also
  at +10 m) and 28ID's destination pass - E1's open item, reproduced exactly; it changes the planner's branch, not the
  outcome.
- The end-time walk interval: more than 30 SIM s between two timed walks at the movers' end time (P32 (1)) while the CON
  clock never advanced more than 17.1 SIM s in 2 WALL s, so those two walks were >= ~3.6 WALL s apart [A: the tick thread
  was busy]; nothing records what occupied it.
- The ~20.6-21.8 SIM s offset between the app's L-DISP stamps and the trace's CON clock at the same wall instant (e2_score:
  T14 326.3 against 346.9, T02 326.3 against 348.1; E2-1 26.9 s, E1 ~16 s). New [V]: the app's own "SIM s after dispatch"
  figures agree with the CON clock's differences within 3.3 SIM s (L179085 438.2 against 434.9; L218315 550.8 against
  547.7), so within a run the offset is a near-constant difference between the two clocks' zero points, not a stale
  dispatch stamp. Why the zero points differ, and by a different amount each run, is not established. It does not enter
  P31F, which uses the app's stamps only.

RECORD CORRECTION - a dissent line for the seat, not a re-scoring of those runs: the Results of E1 (P19b, "0 warnings")
and -2 (P19b, "0 warnings for T14") are contradicted by their own logs, which carry the TaskGeometryResolver
embedded-Location warning for T14 twice each (E1 L1825 / L1995; -2 L1347 / L1523), as this run does (L739 / L855). The
order's T14 still carries STP's embedded Location, which ends 2,428 m from the cut-A MapGraphicID route; the resolver
uses the MapGraphicID and says so at WARN on every resolver pass. A registration that predicts "zero resolver warnings"
for T14 on this order must name that line as expected, or name the family it means.

Adversarial review: (1) THE ANCHOR CLAIM rests on L = -0.3 and on the successors dispatching on the completion walk's own
sample. Competing hypothesis: a late anchor hidden by a later dispatch sample. Refuted by the arithmetic: a late anchor
LOWERS the served figure and a later sample RAISES s2 - both push L up (L = anchor lag + any extra samples), and L is
-0.3, within S's rounding; an axis HOLD during the hold span would also push L up. (2) THE HEADLINE's competing reading -
that the track missed the -2 stop without any planning - is refuted by the path: the vendor's 27-point off-road path bowed
132 m west of the straight line that Move Along Route drives, and 48 IBCT's console shows the plan being made (L74075,
L74581). What is NOT shown: that per-vertex Move To beats one Move To to the destination (sec 7). (3) THE ONE CONTESTABLE
SCORE is P19b; it is scored on the registered family and flagged with the stricter reading, so the seat can take either.
(4) P31b is a registered MEDIUM miss, left as missed. (5) Unexplained items stay listed above; none enters a HIGH row.
Verified here against the harvested files: every line, count and time in the table. Inferred and marked [A]: the tick
thread's load at the end-time walk and the design implications.
