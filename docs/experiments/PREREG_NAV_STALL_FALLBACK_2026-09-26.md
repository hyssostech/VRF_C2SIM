# PREREG - NAV STALL FALLBACK: one live run on the new nav fixture (area loads, stall abort, attack engage fallback)

STATUS: REGISTERED 2026-09-26 (the registration commit's own time is the stamp; see git log), BEFORE the order
validation push, the dry run and the run. Written by lane R2 (session 5fc25950) on branch
run/nav-stall-fallback-2026-09-26 from main 15f6295. Marks: [V] = checked while writing this file; [A] = inferred.

## Registration

PREREG ID: NAV_STALL_FALLBACK-2026-09-26-1
BINARY / COMMIT: main 15f6295. NO REBUILD: main has no change under src, scripts or the runner's tools since a9d738f
(`git diff --stat a9d738f 15f6295 -- src scripts tools/RtiProbe tools/WatchVrf tools/PushInit tools/PushOrder
tools/ListenReports tools/StopIface tests` is EMPTY [V]), and the deployed app is lane R's a9d738f build:
VrfC2SimApp.exe sha256 acbb5095330ae565ef58ca3643ba66fa34dd1a3cf6ef65561f007296fd25bbb5 and .dll
7b3554a5fbf4da4c2dbb00d39ea855d527b173df8ede74cbc7de26bae97b8787, ProductVersion 1.0.0+git.a9d738f.Release-5.2;
VrfBridge.dll 90272bc95297e3304066218bd5a53128ef8531cfbd6b4dcec3ce5c65b0f25847 (the pin) in all eleven 5.2
consumers' bin\Release-5.2 [V 2026-09-26 ~18:00Z].
TIER AND GATE: HEAVY / PREREG.
RUN KIND: movement
ONE RUN, THREE GOALS - sec 1 (a) says why one run carries them.

VENDOR CITATION: UG52 sec 23.3 p505 ("There is no path planning done as it moves toward the next vertex") and sec
23.2 / 23.2.1 p500 (only Move To plans a path); UG52 68.3.3 p1312 / 68.3.4 p1313 (a derived SMS's scripts supersede
the included set's - the mechanism of the proof line); UG52 23.5.2 p508 ("vehicles can just barely move up the
max-slope ... on the best soil surface"); singleTaskControllerComponent.h:191-200 decideToGiveUpTask "always returns
false" (the vendor never reports a stuck unit - only this interface's watchdog does).

OWN-RECORD CITATION:
- G1: tools/FixtureGen/README.md (R9_Mojave_Empty_52_NavAO20_AG_jst, .scnx sha256 2c5b7c3b...55b6); RUNBOOK 0.5.14
  item 12 (the nav-area READY gate); PREREG_V7_AO20_2026-09-15.md P-A (the gate fired on an AO20 row; both the area
  rows and the proof line "C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true" appear in
  vrfc2simapp.log - V7 run 20260915T011237Z_run: 14 proof lines in the app log [V grep]) and P-C (a 2.3 km in-area
  goal "Planned path has 30 points."); RUNBOOK C11 (the object's own console is the first instrument).
- G2: docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md sec 7 (the cause claim: 1-35's straight leg toward V1 runs
  into 55 m of SUSTAINED face at 34.6561/-116.7614, 0.70-0.95 rise over run); the freeze on that line in P11, G2, G3
  (G3 WITH the MojaveAO20 area), G5, G6, V13 (PREREG_V13_REPORTING_WATCHDOG_2026-09-14.md V13c: "STALL: unit
  1-35/2/1_A~PXY task T1 ..." - the watchdog fired on it) and V8z (PREREG_V8_ROUTE_SHIFT_2026-09-15.md RESULTS
  20260915T041036Z: MojaveAO20 area behind the nav-area gate, route shift OFF, mesh-planned hops - "5 of 6 froze; 1
  crawled 478 m past"); RUNBOOK sec 12 (the lateral route shift exists to detour exactly this face - V8b crossed
  with it ON, so it must be OFF here).
- G3: laneM4 FINAL assertion list items 5 (i)-(v) and the displacement rule (reproduced as the P rows below);
  VrfSettings.cs:766 `EngageFallbackSeconds { get; set; } = 300` and VrfC2SimService.cs:5228 (a WALL-clock
  Task.Delay); PREREG_V13 V13d (1-1's T23 moved normally; no stall); SEMANTIC_MAPPING.md sec 2b ("how
  DtFireAtTargetTask behaves against an aggregate uuid is a run-only question").
- Rulings: RL-20260925-01 (D2 follow-ons of a stuck unit abandoned with their own TASKABRT; D3 stall detection ON in
  the demo profile; D4 the parked engage still issued on a late arrival); RL-20260921-05/-07/-09; RL-20260914-01;
  RL-20260920-01 (route shift ON by default - deviated from, see DEVIATION lines).
- Lane R's run COMPLETION_CONFIRM-2026-09-26-1 (VOID on its teardown limb; owner accepted the completion
  measurements, n = 1) and its TEARDOWN FINDING (StopVrf52's graceful close refused twice, cause not claimed).

## 0. Purpose, in plain words

Three things are owed on this machine and none has been seen live:
- G1: the new navigation fixture really loads its area `NavArea-ground-platform MojaveAO20_jst`, the order waits for
  it, and the abstract-graph SMS really executes (its own printInfo line in a member console).
- G2: a unit that genuinely stops gets the watchdog's STALL line, a TASKABRT that is SENT (not suppressed), and its
  follow-on task abandoned with its own TASKABRT (D2). DEMO_READINESS row 26 stays OWED until this is seen.
- G3: an ATTACK whose approach move outlasts Vrf:EngageFallbackSeconds takes the engage fallback, and the fallback is
  classified into exactly one of M4 item 5 (i)-(v) with the displacement rule applied.

## 1. Decisions taken from the record

(a) ONE RUN, NOT TWO. The three goals need three different units and nothing forces them apart: G1 needs any placed
platform (the area row is simulation-wide, V7 "residency is a simulation-wide property") and one in-area mover; G2
needs one unit on a line the record shows freezes; G3 needs one attacker that is still MOVING at the fallback. The
stuck-attacker case (M4 item 5 (iv)) is NOT targeted, and that is the one thing that would need a second run: with
one global Vrf:EngageFallbackSeconds a moving attacker needs approach > F, while a watchdog-reported stuck attacker
((iv)(a)) needs t_stuck + 240 wall s < F, and the only recorded stopper is one unit's line (1-35's) - a second
attacker on that line would share the face with the G2 unit and could not be timed against the same F within the
recorded 2.6-10x SIM/WALL band. (iv) stays owed and is proposed as a separate registered step.

(b) FIXTURE: R9_Mojave_Empty_52_NavAO20_AG_jst, deployed at C:\MAK\vrforces5.2d\userData\scenarios, sha256
2c5b7c3bff5c6525c39a93e80336678f5e1cab30bff3378fa4500de4448455b6 = README [V]; its .scn names the terrain
`<main>\tools\navdata\out\MAK Earth (online) + MojaveAO20_jst_nav.mtf` (sha256 985592b7...ef03 = README [V]), whose
navData entry is C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveAO20_jst.navRuntimeConfig
(present, extents -10019..10062 x -9976..9976 [V]), and the SMS C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs.sms,
whose scripts\ground-vehicle-move-to.lua:489 is `printInfo("C2SIM override ground-vehicle-move-to.lua:
useAbstractGraphs=true")` [V]. AO20 covers 34.5183-34.6980 N, -116.8091 to -116.5909 W
(RESEARCH_ABSTRACT_GRAPH_CONNECTIVITY_2026-09-15.md sec 7, same extents as the _jst regeneration).

(c) INIT: data/COA-STP1_Initialization.xml (SystemName C2SIM, 128 units, CreationPolicy AtOrder from the wrapper:
127 empty shells, members only for units an order references) with --type-map data/unit-type-map-52-nolifeform.json -
the V7/V8/V8z/V13 line; the repo map's lifeform templates crash the sim without DI-Guy data
(unit-type-map-52-nolifeform.json proxyNote "DI-Guy data absent -> sim crash, PREREG_COASTP1_52_RUN1 sec 6"). The R9
lean init was rejected: none of its units starts on a recorded stopper and it holds no distinct target for an ATTACK.

(d) THE ORDER - data/NAV_STALL_FALLBACK_Order.xml (committed with this file; 9,135 bytes, 213 CRLF lines, 0 comments,
0 blank lines, ASCII - generated by a script so no hand edit could add a comment). Five tasks, uuids
c4000000-0000-0000-0000-00000000000N:

| # | name | performer | verb | geometry | Duration | start | goal |
|---|---|---|---|---|---|---|---|
| 1 | T_R2_G2_135 | 1-35/2/1_A d6df3c3d... | MOVE | COA T1's 4 vertices VERBATIM (V0 assembly, V1, V2, V3) | PT2H | delay 0 | G2 stall |
| 2 | T_R2_G2_135H | 1-35/2/1_A | DEFEND | none | PT5M | STREND after #1 | G2 D2 follow-on |
| 3 | T_R2_G3_11A | 1-1/2/1_AD de16a337... | ATTACK | COA T23's 4 vertices VERBATIM | PT30M | delay 0 | G3 fallback |
| 4 | T_R2_G3_11AH | 1-1/2/1_AD | DEFEND | none | PT5M | STREND after #3 | G3 follow-on |
| 5 | T_R2_G1_47 | 47_CTCP/47 5ee33fe7... | MOVE | its init position -> 34.6450/-116.7050 (2.6 km NE, inside AO20) | none | delay 0 | G1 in-area mover |

Task #3's AffectedEntity is AD/7154 (faa0b9e3-0668-fb99-0d1e-3c3c1a68f0d4), a hostile (WASA) platoon created by this
client at 34.4166/-116.4761, about 30 km from every route - a DISTINCT entity, so ResolveAffectedTarget returns it and
DeferEngageUntilMoveCompletes parks the fire (VrfC2SimService.cs:4255/4962/5214). It is far on purpose: the fallback's
classification, not a fight, is under test. ROE on every task is ROEHold (T1/T23 verbatim).

(e) THE STOPPER, CHECKED OFFLINE BEFORE ANY LAUNCH (a test, not an essay). tools/preflight/leg_check.py over this
order with the COA init, the nolifeform map and the DeStack starts of the P11 trace (starts_P11.csv), elevation L13
[V, scratch u3\laneR2\legcheck.json]: T_R2_G2_135 leg 1 (6,593 m, V0 dropped - "the unit was spread 2774 m from it")
"40 m of 0.826 on sand at 34.6562/-116.7619, 2.0 km from the leg start ... PREDICTED IMPASSABLE (ratio 1.10 vs
threshold 0.92)" - the P11 freeze point; its other legs 0.585 / 0.698. T_R2_G3_11A legs 0.870 / 0.654 / 0.717 (not
flagged; route 26.3 km, V0 dropped at 2,762 m). T_R2_G1_47's leg 2,599 m, ratio 0.176 (flat). 1 of 7 legs flagged,
0 missing tiles.
Why the stopper is registered MEDIUM, not HIGH: it froze in eight runs, but never on this terrain copy (the _jst
biome swap), never with the plain abstract-graph SMS (V8z used _Slope2), and V8z had one member CRAWL 478 m past -
a member that keeps crawling more than 50 m per 240 wall s defeats the watchdog's criterion by design.
Why V1 is still driven straight: V1 (34.651212/-116.811637) is 206 m OUTSIDE AO20, so each member's goal fails "Is
destination in nav area?" and is a 1-part straight path (V7 P-D, verbatim console shape) - the line of every freeze.

(f) SETTINGS (env beats appsettings.json; the runner records the effective values in the manifest):
- --env Vrf__StallDetection=true (appsettings.Demo.json's one key; the wrapper loads no Demo overlay). Watchdog
  defaults kept: WALL clock, 240 s window, 50 m, 60 s floor after dispatch.
- --env Vrf__PreflightRouteShift=false (see DEVIATION).
- --env Vrf__EngageFallbackSeconds=150 (key: VrfSettings.cs:766, default 300 wall s). At the recorded 2.6-10x SIM/WALL
  band, 150 wall s is 390-1,500 SIM s after dispatch - 3-14 km along a 26.3 km approach, so the attacker is predicted
  to be still moving; 300 would reach 26 km at the top of the band.
- --pre-order-gate nav-area --pre-order-gate-timeout 900 (cold area: 236.9 s on record, RUNBOOK 0.5.14 item 12; this
  area has never been loaded on this machine). No --pre-order-settle (gate OR settle, never both).
- --no-stop-when-complete, --run-secs 1500. DurationScale 1.0, TimedCompletion/TaskClock defaults, the wrapper's
  Vrf__TaskPredecessorTimeoutSeconds=7200 (not overridden: no late-arriver-skip limb is registered here).
- Consoles: --object-console 4, --member-console 4 (wrapper defaults; the gate needs >= 3, the proof line is a member
  console line).

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: nav-area (timeout 900 s)

DurationScale: 1.0

ARMED ENDS VS STALL WINDOW: DurationScale 1.0. Armed ends in SIMULATION s: T_R2_G2_135 7200 (destination task; far
beyond the watchdog's first possible verdict at dispatch + 60 s floor + 240 WALL s, so the end time cannot pre-empt a
stall); T_R2_G2_135H 300 (no destination, never watched); T_R2_G3_11A 1800 (destination task until the engage fallback
drops its destination at 150 WALL s); T_R2_G3_11AH 300 (no destination); T_R2_G1_47 none (completes on evidence).

DEVIATION FROM RECORD: the lateral route shift is OFF (--env Vrf__PreflightRouteShift=false) although RL-20260920-01 made it the default "for any run"; the shift exists to detour exactly the 55 m face this run needs a unit to meet (RUNBOOK sec 12: V8b crossed with it, the zero-offset control V8z froze without it), so with it ON the G2 unit is predicted NOT to stop and row 26 could not be exercised. It is off for all three units; the G3 and G1 legs are not flagged (sec 1e), so the shift would not have acted on them.

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile (RL-20260925-01 Q3 "ON in the demo profile"); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels - the one key is appsettings.Demo.json's "StallDetection": true.

DEVIATION FROM RECORD: Vrf:EngageFallbackSeconds is 150 via --env, not the shipped 300, so the fallback fires while the attacker is still on its approach across the whole recorded SIM/WALL band (sec 1f); the brief allows a reduced setting and this is the key it names.

DEVIATION FROM RECORD: the type map is data/unit-type-map-52-nolifeform.json, not the repo map (RunScenario.sh default), because the COA-STP1 init's lifeform templates crash the sim without DI-Guy data; every COA-init run in the stall record (V7, V8, V8z, V13) used this map.

DEVIATION FROM RECORD: no throw-away warm-up run - DEMO_RUNBOOK sec 0.4 says to warm the terrain before a demo; this is not a demo, the gate measures the wait (WARM/COLD is recorded), and a cold area only lengthens the gate within its 900 s timeout.

DEVIATION FROM RECORD: no new persistent holder is started unless the launch slips (sec 5) - RUNBOOK 9c's demo posture is a holder "started ONCE" and held; lane R's persistent holder RtiProbe 34096 (appNo 5103, `RtiProbe.exe 5103 MAK-ONE-2025 1 28800 3`, joined 11:59:11Z) is still joined and holds until about 19:59Z, and RUNBOOK 0.5.14 item 18 says "a holder left by the previous run helps the next one".

## 2. What the code emits - log-line shapes (src at a9d738f = 15f6295; line numbers VrfC2SimService.cs)

Lane R's PREREG_COMPLETION_CONFIRM sec 3 shapes stand unchanged (no src change since) [V: git diff above]; the ones
scored here:
- L-AREA (vendor console, forwarded): `VRF console [3] <object> (VRF_UUID:<u>): New Primary nav area: | <area>`
- L-ORDER (:3649): `C2SIM Order received (<n> bytes).`
- L-PROOF (vendor console): `VRF console [<n>] <member> (VRF_UUID:<u>): C2SIM override ground-vehicle-move-to.lua:
  useAbstractGraphs=true`
- L-MEMBERS (:4211): `VRF console level 4 requested for <n> DISTINCT member(s) of <unit> (...): <name>
  [VRF_UUID:<u>], ...` (maps a member console to its unit).
- L-PLAN (vendor console): `Planned path has <N> points.` / `Planned path has <N> parts.`; `Node Is destination in nav
  area?: success` / `fail in action Is destination in nav area?`
- L-DISP / L-STRT / L-ARM-D / L-ARM-N / L-NODUR / L-INPLACE / L-GATE / L-ARRIVE / L-HELD / L-END / L-OVERDUE /
  L-CMPLT-T / L-CMPLT-L / L-CMPLT-E / L-SKIP / L-CANCEL / L-SUPP / L-WDOG / L-CLOCK / L-RATIO / L-PLACE: as lane R sec 3.
- L-STALL (:7190): `STALL: unit <unit> task <task>: no member moved more than 50 m in the last <W> <clock> s (max <m>
  m); TASKABRT reported.` then (:7203) `SENT TASK STATUS REPORT (TASKABRT) ... - STALLED (C16 progress watchdog) -
  report only: ... its follow-on tasks are abandoned.`
- L-DEFER (:5219): `Task '<task>': fire <vrf> -> <tgt> deferred until the move COMPLETES (completion-gated; fallback
  <S>s).`
- L-FB-ISSUE (:5345): `Unit <unit>: move for task '<task>' did not complete within <S>s; issuing the fire via fallback
  (it will replace the still-running move).`
- L-ENGAGE (:5417): `ATTACK: FireAtTarget <vrf> -> <tgt> issued (task '<task>').`
- L-FB-REPL (:5350): `Unit <unit>: task '<task>' - the interface replaced its move with the fire, so the unit is no
  longer travelling ... it completes <NOW - its end time has already passed | at its end time> ... Watchdog verdict at
  the fallback: <MOVING (max .. m in .. s) | MOVED since dispatch (max .. m; watchdog had no verdict) | NONE
  POSSIBLE ...>`
- L-FB-STUCK (:5332): `... did not complete within <S>s and the unit is STUCK (<why>) - the fire is NOT issued ...`
- L-FB-DROP (:5317): `... the engage fallback for task '<task>' is DROPPED - that move is no longer the unit's current
  task ...`; L-FB-NOTHING (:5282): `... the engage fallback for task '<task>' finds nothing to do ...`
- L-FB-NOW (:5368): the TASKCMPLT reason `... its end time (start time + Duration) has passed and the interface
  replaced its move with the fire (engage fallback) ...`
- Route shift OFF: NO `LATERAL ROUTE SHIFT ON` start-up line and zero `ROUTE SHIFTED` / `ROUTE SHIFT check queued`.

## 3. Sequence and exact command lines

All from Git Bash at the MAIN checkout C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\Software\
Interfaces\VRF_C2SIM (the runner resolves its exes, tools and ledger from its own root). The main checkout's
working-tree docs/OPUS_EXECUTION_PLAN.md receives this branch's ledger text before the dry run (so the runner's marker
read sees 5122), and the runner's own block is carried back to the branch afterwards (lane R's procedure).

A. Read-only checks at launch: `echo "$DOTNET_ENVIRONMENT"` empty; `env | grep '^Vrf__'` empty; no vrfNavGenerator /
   dotnet build / MSBuild / VBCSCompiler / vrfSim / vrfGui / VrfC2SimApp / WatchVrf / ListenReports process; holder
   34096 alive; rtiexec 47980 + rtiForwarder 50740 alive (never touched).
B. No build (sec Registration). Hashes re-read before and after the run.
C. Order validation (no federate, no appNumber):
   - C1 `src/VrfC2SimApp/bin/Release-5.2/net10.0/win-x64/VrfC2SimApp.exe --parse-order <order>`: EXPECT 5 tasks;
     durations 7200000 / 300000 / 1800000 / 300000 ms and (none) for #5; #2 after c4...01 and #4 after c4...03 as
     STREND; #3 AffectedEntity faa0b9e3...; 0 warn/schema/error lines.
   - C3 ONE real push to the PRIVATE server (18080/61614; never StompProbe, never 8080/61613):
         tools/PushInit/bin/Release/net10.0/PushInit.exe data/COA-STP1_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
         tools/PushOrder/bin/Release/net10.0/PushOrder.exe <order> 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
     EXPECT both exit 0, PushOrder not exit 4, one `ORDER (<n> chars)` echo carrying 5 tasks. Any failure = STOP.
D. Holder: 34096 must be alive at launch. If the launch (step E) starts after 19:10Z, FIRST start the contingency
   holder with the numbers claimed in sec 5:
       "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5118,5119,5120,5121 -SettleSecs 28800 < /dev/null > runs/launch52/r2-holder-<stamp>.log 2>&1
   (EXPECT `HOLDER JOINED ... appNo 5118`); otherwise 5118-5121 are BURNED.
E. Dry run = the exact command plus --dry-run. Then THE RUN, once, stdout to a FILE, never piped:

       scripts/RunScenario.sh \
         --scenario R9_Mojave_Empty_52_NavAO20_AG_jst \
         --init data/COA-STP1_Initialization.xml \
         --order <absolute path of this branch's committed data/NAV_STALL_FALLBACK_Order.xml> \
         --client-id C2SIM \
         --type-map 'C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM\data\unit-type-map-52-nolifeform.json' \
         --pre-order-gate nav-area --pre-order-gate-timeout 900 \
         --no-stop-when-complete --run-secs 1500 \
         --env Vrf__StallDetection=true \
         --env Vrf__PreflightRouteShift=false \
         --env Vrf__EngageFallbackSeconds=150 \
         --sample-threads \
         --log runs/launch52/RunScenario-navstall-<stamp>.log

   Defaults kept: --profile 5.2, --no-gui, --backend-notify 3, --position-report 10, REST/STOMP 18080/61614, the
   runner's Stage 2h holder (it JOINS the federation the persistent holder keeps).

QUIET PERIOD (RUNBOOK 0.5.14 item 5): from step E to the post-run inventory: no Stop-Process / taskkill, no build, no
subagent or other agent session started by this lane, no second runner; foreground polling of the run's own files
with bounded waits. The single exception is the brief's NEW 1 AFTER the runner has exited: if StopVrf52 refused, THIS
run's back end may be force-stopped by pid after re-checking its name and start time - never rtiexec, rtiForwarder,
rtiAssistant or any holder - with the StopVrf52 output, thread count and window state captured first.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a STOP, not an adjustment)

Every count is PER TASK UUID c4...01-05, read from vrfc2simapp.log in line order (its line order is its clock), and
cross-checked against reports-captured.log; runner/manifest lines are named where used. M4 items are cited per row.

| # | Prediction | Confidence | FALSIFIER (what counts as a MISS) | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) Persistent holder alive at launch; the back end JOINS (rtiexec count-grep: no create for its appNo) and "READY - joined the federation, 1 VR-Forces back-end(s)". (ii) READY TO TASK names every init unit bound; L-WDOG once, on the WALL clock; L-CLOCK names SIMULATION and DurationScale=1; no "LATERAL ROUTE SHIFT ON" line. (iii) The runner's stage 7d gate FIRED (no NOT-READY, no exit 3). (iv) MATERIALIZE lines for 1-35/2/1_A, 1-1/2/1_AD and 47_CTCP/47, and one for AD/7154 (the AffectedEntity). (v) The 3 roots c4...01/03/05 each get L-DISP + L-STRT. (vi) Each of the 3 performers has >= 1 member displaced > 50 m within 360 SIM s of its L-DISP (WatchVrf trace). | HIGH | Any limb = VOID + STOP (launch, terrain or harness failure - no code verdict). | |
| P1 | G1 AREA: the FIRST L-AREA row in vrfc2simapp.log names `NavArea-ground-platform MojaveAO20_jst` and precedes L-ORDER; the runner prints "NAV AREA ACQUIRED ... NavArea-ground-platform MojaveAO20_jst"; ZERO L-AREA rows name any other area (the terrain lists one Mojave area only: MojaveAO20_jst [V grep of the .mtf]; a row with an EMPTY area on leaving AO20 is recorded, not a miss). WARM/COLD and the gate wait are RECORDED. | HIGH | No row before L-ORDER, a gate timeout, or any other named area = STOP (the fixture did not take its terrain). | |
| P1b | G1 SMS: >= 1 L-PROOF line whose VRF_UUID is a member of 47_CTCP/47 by its L-MEMBERS line (the in-area mover, not a G2/G3 unit). L-PROOF lines from 1-35 / 1-1 members are counted separately. | HIGH | Zero L-PROOF lines from 47_CTCP/47's members = STOP (the derived SMS did not execute for the G1 unit). | |
| P1c | G1 MESH: >= 1 of 47_CTCP/47's members' goals toward its destination prints "Is destination in nav area?: success" and "Planned path has N points." with N > 1 (mesh-planned); zero "not enough (0) points" for them. | MEDIUM | No such pair (recorded; P1b still carries the SMS proof). | |
| P1d | G1 COMPLETION (M4 item 7): L-NODUR once for T_R2_G1_47; zero armed / TIMED COMPLETION lines naming it; ARRIVAL EVIDENCE (or a vendor completion) then exactly one SENT TASKCMPLT "unit 47_CTCP/47~PXY completed its task." (unit label as the app prints it). | HIGH on the structure; MEDIUM that it arrives inside the window | An armed end; a Duration-based reason; two TASKCMPLTs. No arrival by window end = recorded, not a miss. | |
| P2 | G2 THE STOP: 1-35's members freeze within 150 m of the P11 point 34.65608/-116.76142 and ONE L-STALL for T_R2_G2_135 appears inside the window. | MEDIUM (sec 1e) | No L-STALL for 1-35 by window end: recorded MISS (MEDIUM - not a stop); P3 then NOT EXERCISED and row 26 stays OWED. | |
| P3 | G2 STALL PATH (M4 item 4), live only if P2 fired: (a) the L-STALL line then exactly ONE SENT TASKABRT for c4...01 with "STALLED (C16 progress watchdog) - report only", and NO L-SUPP for it; (b) NO L-CANCEL naming c4...01; (c) T_R2_G2_135H: the "-> NOT dispatched." WARNING and exactly ONE SENT TASKABRT "SKIPPED: predecessor c4000000-0000-0000-0000-000000000001", and NO L-DISP / L-STRT for c4...02; (d) NO SENT TASKCMPLT for c4...01 unless an ARRIVAL EVIDENCE line for T_R2_G2_135 precedes it (such an arrival is COUNTED as a false alarm under D2). DISPLACEMENT recorded from the trace: each member over the 240 s window before the STALL and since dispatch. | HIGH (conditional on P2) | Any of (a)-(d) failing = STOP. | |
| P3t | If T_R2_G2_135's 7200 SIM s end time is reached inside the window: exactly one L-OVERDUE for it and nothing SENT for c4...01 at that moment. | MEDIUM (conditional) | A TASKCMPLT for c4...01 at its end time. | |
| P4 | G3 PARK: exactly one L-DEFER for T_R2_G3_11A reading "fallback 150s", and the attacker's L-DISP / L-STRT for c4...03. | HIGH | Zero or two; a different fallback value (the env did not reach the app) = STOP. | |
| P5 | G3 CLASS (M4 item 5): every engage fallback for c4...03 is classified into EXACTLY ONE of (i)-(v). Predicted class: (iii) - L-FB-ISSUE, then L-ENGAGE for T_R2_G3_11A, then L-FB-REPL naming "MOVED since dispatch (max .. m; watchdog had no verdict)" (the watchdog's 240 s window cannot be full at 150 s; a "MOVING (max .. m in .. s)" verdict also counts as (iii)). | HIGH that exactly one class fits; MEDIUM that it is (iii) | Two classes, or no class, for c4...03 = STOP. Any class other than (iii) = recorded MEDIUM miss (not a stop). | |
| P5b | (iii) COMPLETION LIMBS, live only if P5 is (iii): no TASKABRT for c4...03 from the fallback itself; exactly one SENT TASKCMPLT for c4...03 - at its end time (L-END "... 1800 s Duration ..." then L-CMPLT-T) or at once with the L-FB-NOW reason if the end time had already passed; T_R2_G3_11AH's L-DISP / L-STRT only AFTER that TASKCMPLT, then its own one TASKCMPLT "(300 s after dispatch)". | HIGH on "no second terminal code, follow-on after"; MEDIUM on "the TASKCMPLT is at the end time" | A second TASKCMPLT, or the follow-on dispatched before c4...03's TASKCMPLT = STOP. A vendor FAILURE of the FireAtTarget itself (then the failed-task path sends TASKABRT and abandons #4) = recorded MEDIUM miss + finding (SEMANTIC_MAPPING 2b's run-only question answered), not a stop. | |
| P5c | M3-1 / M4-1 / DISPLACEMENT RULE: no L-ENGAGE for c4...03 after c4...04's L-DISP; zero L-FB-DROP / L-FB-NOTHING for T_R2_G3_11A unless the attacker ARRIVED first; for the fallback, the attacker's member displacement over the preceding 240 s window (or since dispatch, whichever is shorter) AND since dispatch is RECORDED from the trace; a (iii) whose trace shows no member > 50 m over that span is the stated residual and is COUNTED, not passed; an ARRIVAL within about one tick of the fallback with no engage is M4-1 and is COUNTED. | HIGH on "no engage while another task is current"; the counts RECORDED | An L-ENGAGE for T_R2_G3_11A after c4...04 dispatched = STOP. | |
| P6 | M4 ITEM 6: ZERO L-SUPP for a TASKCMPLT or TASKABRT; at most ONE SENT TASKCMPLT per uuid; exactly one SENT TASKSTRT for each DISPATCHED task. Expected terminal set if P2 and P5 hold as predicted: c4...01 TASKABRT (stall), c4...02 TASKABRT (skip), c4...03 TASKCMPLT, c4...04 TASKCMPLT, c4...05 TASKCMPLT (if it arrives). reports-captured.log holds the same task-status bodies; the app's "Reports this run: ... 0 FAILED". | HIGH | A duplicate terminal code, an L-SUPP for a terminal code, capture and log disagreeing, a failed push. | |
| P7 | M4 ITEM 9: the runner's terminal count (RunnerLib Get-TerminalTaskReports replayed offline over the app log; the manifest's earlyExit is empty with the early exit off, lane R P9) equals a grep of the FIRST `SENT TASK STATUS REPORT \((TASKCMPLT\|TASKABRT)\)` per task uuid. | HIGH | The two counts differ. | |
| P8 | M4 ITEM 8, retired-line limb: ZERO `NOT DISPATCHED YET`, `HELD as`, `REFUSED [`, `BOUND-BUT-NOT-ON-THE-GROUND`. World (WARM / COLD) read from the LAST L-PLACE line; COLD-world limbs as lane R's P8 if any FALLBACK create. | HIGH (retired lines; cold limbs if cold); MEDIUM on which world (WARM expected) | A retired line; a TASKABRT after an L-RC-OFF; re-clamp counts that do not sum. | |
| P9 | RUN HEALTH IN THE WINDOW: exe / dll / bridge sha256 identical before and after; no `BACK END LOST`; no WS-runaway exit 6; no new .dmp / .callstack.log for this back-end pid; VrfC2SimApp exits 0 (clean resign); rtiexec 47980, rtiForwarder 50740, rtiAssistant 30240 and every holder untouched. | HIGH | Any limb = VOID. | |
| P9t | TEARDOWN (a separate finding since lane R's run, owner verdict 2026-09-26): StopVrf52 exits 0 inside the runner's teardown and no vrfSim / VrfC2SimApp / WatchVrf / ListenReports is left. | MEDIUM | StopVrf refused again: recorded as the SECOND consecutive refusal (2 of 2 today), NEW 1 applied and recorded; it voids nothing above. | |
| P10 | CLOCK (RECORDED, no band claimed): every per-minute SIM/WALL RATIO line; the SIMULATION stamp of each L-DISP; the attacker's SIM seconds at the fallback. | RECORDED | - | |

STOP RULES:
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P9 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- The executor never intervenes in the window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as NAV_STALL_FALLBACK-<date>-2, with new appNumbers.

ONE VARIABLE: none - a confirmation run of three code paths. Its HIGH rows are log-structure predictions that need no
control. The MEDIUM rows (the stopper, the class, arrival timing) depend on terrain, the SMS and the clock ratio.

## 5. Application numbers

The Appendix B marker read `*** NEXT FREE: 5118 ***` at 15f6295 [V]. This registration claims 5118-5132 and leaves
the marker at 5133.
- 5118-5121: CONTINGENCY persistent holder (sec 3 step D), hand-claimed in THIS commit; used only if the launch starts
  after 19:10Z; otherwise BURNED.
- Runner block (written by the runner itself at Stage 2, in its own allocation order):

| appNo | what | expected |
|---|---|---|
| 5122 | back end (vrfSimHLA1516e) | CONSUMED |
| 5123 | front end (vrfGui) | BURNED - --no-gui |
| 5124 | WatchVrf pre-init oracle pre-check | CONSUMED |
| 5125 | WatchVrf main run trace | CONSUMED |
| 5126 | VrfC2SimApp - the interface | CONSUMED |
| 5127 | RtiProbe Stage 2c readiness gate | CONSUMED |
| 5128 | CreateOne Stage 7b diagnostic | BURNED unless the oracle gate fails |
| 5129-5132 | RtiProbe Stage 2h holder attempts 1-4 | attempt 1 JOINS (5129); 5130-5132 BURNED |

PushInit / PushOrder / ListenReports / StopIface are C2SIM clients, not federates. A launch that aborts burns its
whole block; a relaunch needs a NEW registration and new numbers.

## 6. Harvest (after the run, read-only) and where results go

From the run directory (runs\launch52\last-run-dir.txt): vrfc2simapp.log, reports-captured.log, c2sim-bus.log, the
manifest, watchvrf-trace.csv, thread-samples.csv (+ alerts), holder logs, stopvrf logs, the wrapper log. Vendor sim
logs dump the environment in cleartext: count-grep only, never quoted. One grep pass for the sec 2 shapes; per-uuid
tables for c4...01-05; member->unit mapping from L-MEMBERS; displacement from POS rows of the trace (per member, first
fix after dispatch vs the fix at the event, and over the preceding window). Results go to: the Result block below
(measurement and design implication in separate sentences); DEMO_READINESS rows 19 / 25 / 26 (row 26's "Owed" is
discharged ONLY if P3 ran and held); HANDOFF sec 6 (200 x 160 cap); tools/FixtureGen/README.md (the _jst fixture
live-proven or not, by P1/P1b); Appendix B annotated from the manifest. ASCII + CRLF throughout.

## 7. What this run does NOT claim

- Nothing about the stuck-attacker class (M4 item 5 (iv)) - not targeted (sec 1a); nothing about (i), (ii) or (v)
  unless they happen.
- Nothing about whether an attack's effect is achieved (a far target; ROEHold) or how FireAtTarget behaves against an
  aggregate beyond what the log shows.
- Nothing about the route shift (off here), the demo profile as a whole, or a warm-up procedure.
- Nothing about why the face stops a vehicle beyond FINDING_EARLY_STOPS sec 7; no new cause claim is made from a
  freeze (or a non-freeze) here.
- No timing generalisation: n = 1, one host, one fixture, a load-dependent clock.

## Result (written after the harvest, never from a live read)

Written 2026-09-26 by lane A (session 5fc25950) from the harvested files of run 20260926T181639Z_run (runs\ in the
main checkout), read-only. Quotes: vrfc2simapp.log (L<n> = its line number, 2,816,6xx lines, streamed - never
opened whole), reports-captured.log, the manifest, the runner log runs\launch52\RunScenario-navstall-20260926T181638Z.log,
watchvrf-trace.csv (streamed; POS/CON rows), and a COUNT-grep of the rtiexec log. The vendor sim log was not opened.
Trace times are the trace's own t (wall s since the trace began); they are anchored by the trace's CON rows that carry
a SIMULATION stamp: dispatch = t 142.6 (CON "535.128 Task 0 starting subtask maneuver-along"), the refused fire = t
292.6 (the "No controller" CON row), the stall = t ~454 (interpolated between SIM stamps 1244.9 at t 325.2 and 2087.3
at t 553.7 - [A]), the stray completion = t 851.4 (SIM 3263.9).

### VERDICT: VOID BY THE LETTER - P0 (HIGH) limb (ii) MISSED ("READY TO TASK - NOT REACHED"). No STOP-class code miss.

L873 `READY TO TASK - NOT REACHED within 20 s: only 0 of 128 init unit(s) are bound with a readable location.` P0 says
"Any limb = VOID + STOP". Measurement: the barrier's bound is the capped 20 s (L337 "THE BOUND THAT APPLIES HERE IS 20
s"), the init was 128 units on a COLD terrain (runner: first placement -> area row 99.4 s, "COLD file cache"), and the
order arrived about 100 s later; all four order-referenced units were re-created and "ready for tasking" (L1647-L1653)
and all three performers were measured ON the terrain at dispatch (L1729/L1747/L1765). Design implication: the limb
was written from lane R's 6-unit init (READY after 0.9 s) and does not fit a 128-unit cold init; no measured row below
depends on it. Whether to accept the measurements (as the owner did for lane R's run) or re-register a -2 run is the
owner's call. Every other HIGH row held; two MEDIUM rows missed (P5b, P9t); two anomalies are reporting/design
findings (A1 fixed on fix/run2-reporting, A2 open) - see the lane A report.

| # | Verdict | Measured (quoted) |
|---|---|---|
| P0 | MISS (limb ii) -> VOID by the letter | (i) HIT: persistent holder RtiProbe 34096 alive at launch (runner Stage 1 "a PERSISTENT FEDERATION HOLDER from outside this run"); rtiexec log count-grep: 2 "Create Response" lines, both the morning's (log lines 3613/3614) - none for this run, so the back end JOINED; L43 "READY - joined the federation, 1 VR-Forces back-end(s)". (ii) MISS: L873 above. L16 "PROGRESS WATCHDOG ON (C16, report-only): a 240 s no-progress window on the WALL clock" (once); L26 TASK CLOCK on the SIMULATION clock; L6819 "TIMED COMPLETION: 2 task(s) are timing out against the SIMULATION clock; Vrf:DurationScale=1."; zero "LATERAL ROUTE SHIFT" / "ROUTE SHIFT" lines. (iii) HIT: runner "NAV AREA ACQUIRED after 48.6s of gate", no NOT-READY, no exit 3. (iv) HIT: L1653 1-35/2/1_A~PXY, L1649 1-1/2/1_AD~PXY, L1647 47_CTCP/47~PXY, L1651 AD/7154 "ready for tasking". (v) HIT: L1771/L1773 (c4...01), L1753/L1755 (c4...03), L1733/L1735 (c4...05) DISPATCHED + TASKSTRT. (vi) HIT (trace, dispatch t 142.6 to t 235.9 = 360 SIM s interpolated from CON stamps): 1-35 members 569-869 m, 1-1 members 358-1,811 m, 47_CTCP members 71-76 m. |
| P1 | HIT | First L-AREA L1561 `VRF console [3] 2/1_AD/25_~PXY (...): New Primary nav area: \| NavArea-ground-platform MojaveAO20_jst` precedes L1565 `C2SIM Order received (7684 bytes).`; 39 L-AREA rows, ALL "NavArea-ground-platform MojaveAO20_jst", zero other names, zero empty. Runner: "NAV AREA ACQUIRED after 48.6s of gate: object "2/1_AD/25_~PXY" took primary area "NavArea-ground-platform MojaveAO20_jst""; "first placement (UNIT B/40~PXY) -> area row: 99.4s -> COLD file cache". RECORDED: COLD, gate wait 48.6 s. |
| P1b | HIT | 20 L-PROOF lines `C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true`; 10 from 47_CTCP/47's members (all six by L1659's list; first L6423 HMMWV 4 VRF_UUID:c26a8413...), 6 from 1-35's members (e.g. L7205 HMMWV 1), 4 from 1-1's members. |
| P1c | HIT | 47_CTCP members: e.g. L7393 M1A2 8 "Node Is destination in nav area?: success" then L7977 "Planned path has 11 points."; all six planned 3-25 points (L7977, L8629, L9173, L10021, L10023, L10777); zero "not enough (0) points" in the whole log. (1-35 and 1-1 members: "fail in action Is destination in nav area?" + "Planned path has 1 parts.", e.g. L17727/L19037 - V1 lies outside AO20, as registered in sec 1e.) |
| P1d | HIT | L1737 "Task 'T_R2_G1_47': the order gives NO Duration, so this task has no end time" (once); no end-time-armed line names it (L6819 counts 2 timed tasks = c4...01/03); L1178683 "ARRIVAL EVIDENCE: 47_CTCP/47~PXY task 'T_R2_G1_47' - 6/6 member(s) within 500 m of the last vertex"; L1178685 "SENT TASK STATUS REPORT (TASKCMPLT) taskee=5ee33fe7-... task=c4000000-0000-0000-0000-000000000005 - unit 47_CTCP/47~PXY completed its task." (exactly one); the vendor's later completion swallowed (L1325145). Arrived inside the window (SIM ~2042.6). |
| P2 | HIT | L935703 `STALL: unit 1-35/2/1_A~PXY task T_R2_G2_135: no member moved more than 50 m in the last 240 wall s (max 48.2 m); TASKABRT reported.` (one). Trace: every member froze at t 204-208 (62-66 wall s after dispatch) and its final position is 3.7-50.5 m from the P11 point 34.65608/-116.76142 (M1A2 1 3.7 m, HMMWV 1 13.3 m, HMMWV 2 35.7 m, M577A2 1 41.9 m, M3 1 48.8 m, M1A2 2 50.5 m); none moved more than 1.5 m after the stall. |
| P3 | HIT | (a) L935705 "SENT TASK STATUS REPORT (TASKABRT) taskee=d6df3c3d-... task=c4000000-0000-0000-0000-000000000001 - STALLED (C16 progress watchdog) - report only: ..." (one); zero SUPPRESSED lines in the log. (b) zero CANCEL lines. (c) L935707 (warn) "Task 'T_R2_G2_135H' predecessor c4000000-0000-0000-0000-000000000001 was skipped/abandoned upstream; policy=skip, unit 1-35/2/1_A~PXY is BUSY (task in flight) -> NOT dispatched."; L935709 "SENT TASK STATUS REPORT (TASKABRT) ... task=c4000000-0000-0000-0000-000000000002 - SKIPPED: predecessor c4000000-0000-0000-0000-000000000001 ..." (one); no DISPATCHED/TASKSTRT for c4...02. (d) no TASKCMPLT for c4...01. DISPLACEMENT (trace): over the 240 wall s before the stall (t ~214-454) 0.6-45.1 m per member (HMMWV 2 45.1, HMMWV 1 19.1, M1A2 1 13.1, M3 1 9.3, M1A2 2 3.1, M577A2 1 0.6); since dispatch 1,552-1,909 m. |
| P3t | NOT EXERCISED | T_R2_G2_135's end is SIM 534.8 + 7200 = 7734.8; the last RATIO line reads SIM 6830.3 (L2800277); zero OVERDUE lines. |
| P4 | HIT | L1759 "Task 'T_R2_G3_11A': fire VRF_UUID:dd565449-... -> VRF_UUID:e2a36305-... deferred until the move COMPLETES (completion-gated; fallback 150s)." (one); L1753 DISPATCHED + L1755 TASKSTRT for c4...03. |
| P5 | HIT (class iii, as predicted) | L479661 "Unit 1-1/2/1_AD~PXY: move for task 'T_R2_G3_11A' did not complete within 150s; issuing the fire via fallback (it will replace the still-running move)."; L479663 "ATTACK: FireAtTarget VRF_UUID:dd565449-... -> VRF_UUID:e2a36305-... issued (task 'T_R2_G3_11A')."; L479665 "... Watchdog verdict at the fallback: MOVED since dispatch (max 5210.8 m; watchdog had no verdict)". One fallback, one class. |
| P5b | MISS (MEDIUM, the registered vendor-failure branch) | L479763 (vendor console of the UNIT object 1-1/2/1_AD~PXY) "No controller or Controller is disabled, unable to carry out task %1 \| Fire Weapon Task: auto select weapon = True.  Target = AD/7154 Rounds to fire = 1"; L479765 "VRF task complete: 1-1/2/1_AD~PXY / fire-at-target (success=False)"; L479767 end time cancelled; L479769 "SENT TASK STATUS REPORT (TASKABRT) ... task=c4000000-0000-0000-0000-000000000003 - unit 1-1/2/1_AD~PXY: VR-Forces reported the task FAILED (success=false)"; L479771 T_R2_G3_11AH NOT dispatched; L479813 TASKABRT c4...04 "SKIPPED". No TASKCMPLT for c4...03, no second terminal CODE for any uuid, the follow-on never dispatched (the HIGH limbs hold). SEMANTIC_MAPPING 2b's run-only question is ANSWERED for this unit: VR-Forces refuses DtFireAtTargetTask on a Tank Platoon (USA) proxy unit object. BUT the move the fire was meant to replace was NOT replaced: members kept "move-along ... TaskRunning" (L479699, L479747), moved 902-911 m in the next 30 wall s and 20,965-20,979 m to the route's last vertex, and completed at SIM 3263.9 (L1786309 "move-along (success=True)"), which the interface reported as L1786313 "SENT TASK STATUS REPORT (TASKCMPLT) taskee=de16a337-... task=(none)" - anomaly A1 (fixed on fix/run2-reporting; lane A report). |
| P5c | HIT | No ATTACK line after c4...04 (never dispatched); zero "is DROPPED" / "finds nothing to do" lines. DISPLACEMENT at the fallback (trace, dispatch t 142.6 -> fallback t 292.6, 150 wall s - shorter than the 240 s window): 4,582-6,039 m per member (M1A2 3 4,861, M1A2 4 6,039, M1A2 5 4,896, M1A2 6 4,582); no (iii) residual; no arrival near the fallback (M1A2 3 was ~100 m from V1, 26 km route). |
| P6 | HIT (literal) - with A1 | Zero SUPPRESSED lines; one TASKCMPLT per uuid (c4...05 only); one TASKSTRT per dispatched task (3/3). Terminal set: c4...01 TASKABRT (stall), c4...02 TASKABRT (skip), c4...03 TASKABRT (vendor failure - the P5b branch, not the predicted TASKCMPLT), c4...04 TASKABRT (skip), c4...05 TASKCMPLT, PLUS one TASKCMPLT with task=(none) for taskee de16a337 (L1786313) - no uuid, so no per-uuid rule catches it (A1). Capture agrees: reports-captured.log holds 3 TASKSTRT + 4 TASKABRT + 2 TASKCMPLT bodies = the 9 SENT lines; REPORT #9842 [18:33:16.808] is the empty-task TASKCMPLT (`<CurrentTask />`, ReportingEntity de16a337-...). L2816569 "Reports this run: 20338 delivered, 0 FAILED"; the capture's last is REPORT #20338. |
| P7 | HIT | RunnerLib Get-TerminalTaskReports replayed offline over the 9 SENT lines: 6 records - 5 attributed (c4...01-05, first codes ABRT/ABRT/ABRT/ABRT/CMPLT) + 1 "(none)"; the grep of the FIRST terminal line per task uuid: 5. Equal. Manifest earlyExit empty (disabled), as lane R's P9. |
| P8 | HIT (COLD limbs) | Zero NOT DISPATCHED YET / HELD as / REFUSED [ / BOUND-BUT-NOT-ON-THE-GROUND. World: the LAST L-PLACE (L1625) is "1 of 1 ... TERRAIN QUERY, 0 from the FALLBACK" (warm for the order's materializations), but the INIT's L865 is "0 of 128 ... 128 from the FALLBACK" -> the COLD limbs apply: zero "measured OFF the terrain at dispatch" (three gate lines ON the terrain, gaps 0.1 / 1.1 / 0.0 m); the LAST summary L204785 "0 ... ON, 0 RE-CLAMPED AND VERIFIED, 1 CORRECTED, READ-BACK NOT RECEIVED, 0 STILL OFF, 0 NEVER MEASURED ... (1 enrolled)" sums; its 1 = the one correction line (L1545, 2/1_AD/25_~PXY) with no VERIFIED line. Side finding A4 (not scored): each order materialization re-prints the ARMED line "1 of 1 object(s) ... were created at the FALLBACK altitude" (L1609/L1615/L1621/L1627) right after its own "1 of 1 ... TERRAIN QUERY" summary - the init's concluded entry is never removed from the re-clamp map (VrfC2SimService.cs: _reclamp is never cleared), so the line misstates the batch. |
| P9 | HIT (one limb not measured) | exe acbb5095..., dll 7b3554a5..., VrfBridge.dll 90272bc9... re-read after the run: identical; zero BACK END LOST; runner exit 4 (teardown), not 6; runner "VrfC2SimApp exited with code 0 (clean resign)"; runner "RTI infrastructure preserved (correct): rtiAssistant(pid 30240), rtiexec(pid 47980), rtiForwarder(pid 50740)"; holder 34096 never touched. NOT MEASURED by lane A: new .dmp / .callstack.log for pid 40220 (under C:\MAK, outside this lane's reach). |
| P9t | MISS (MEDIUM) - 2 of 2 today | stopvrf.stdout.log: "taskkill /PID 40220 (no /F): SUCCESS: Sent termination signal"; "[FAIL] still running after 120s: vrfSimHLA1516e(pid 40220)"; runner "StopVrf exited 3", "TEARDOWN INCOMPLETE", exit 4. NEW 1 applied by the SEAT, not the run lane: back end 40220 force-stopped at ~18:52:53Z (thread-samples: 80 threads, 4.1 GB, responding, no main window). The SECOND consecutive refusal today (lane R's 20260926T115957Z_run was the first). Research (lane A, A3): in both of today's runs StopVrf's inventory reads `window=""`, while every earlier run that closed - including the 09-05..09-15 --no-gui runs - reads `window="C:\MAK\vrforces5.2d\bin64\vrfSimHLA1516e.exe"` (a console window). |
| P10 | RECORDED | RATIO lines (26): L40647 9.165 (SIM 554.9, mostly pre-dispatch), then 3.29-4.78 through SIM 6830.3 (L2800277). L-DISP SIM stamps: 532.6 (T_R2_G1_47), 534.8 (T_R2_G3_11A), 534.8 (T_R2_G2_135). Attacker at the fallback: SIM ~1120.3 (member console stamps "1120.29", L479675). Stall: SIM ~1721. Stray move completion: SIM 3263.9. |

Measurement: G1 held on every limb - the _jst fixture loaded its one nav area before the order, the abstract-graph SMS
printed its proof line in all three units' members, and the in-area mover mesh-planned and completed on arrival
evidence. G2 held - the 1-35 unit froze on the registered face (all members within 51 m of the P11 point), the stall
TASKABRT was SENT once, and its follow-on was abandoned with its own TASKABRT (D2). G3's fallback classified as (iii),
but VR-Forces refused the fire on the unit object at once, so the task was aborted and the un-replaced move ran on.
Design implication: DEMO_READINESS row 26's owed live confirmation ("the stall TASKABRT sent, not suppressed") is
discharged by P3, subject to the owner's acceptance of a run VOID by the letter on P0(ii). The ATTACK engage as built
(an entity-level Fire At task sent to a UNIT object) cannot succeed on a pseudo-aggregate performer - UG52 31.12 p632
files Fire At under "Engagement Tasks for Entity-Level Scenarios" ("The Fire At task commands an entity to fire at a
specific target ... If the entity cannot fire on the target, it abandons the task") - and the fallback's "it will
replace the still-running move" is false when the fire is refused; that is a design question for the owner (A2), not
a code slip.
