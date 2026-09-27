# PREREG - IRON STORM CUT A, FIRST LIVE RUN (maple nav area, T10 rerouted, pre-warmed, one scored run)

STATUS: REGISTERED 2026-09-27 (the registration commit's own time is the stamp; see git log), BEFORE the order
validation push, the holder, the pre-warm launch and the scored run. Written by lane IS1 (session 5fc25950) on branch
run/ironstorm-cuta-live1 from main a019918, from the planning lane's draft notes (2026-09-26) with every placeholder
filled from the lane I1d report. Marks: [V] = checked while writing this file; [A] = taken from the record, not re-checked.

## Registration

PREREG ID: IRONSTORM_CUTA_LIVE-2026-09-27-1
DATE (UTC): 2026-09-27, before any launch (the registration commit's own timestamp is authoritative)
BINARY / COMMIT: main a019918, deployed build 80f707f. NO REBUILD: `git diff --stat 80f707f..a019918 -- src` is EMPTY
[V]. Deployed src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64: VrfC2SimApp.exe sha256
493a07a89e43daaa8410bc489f0ba12816bec0a342219ff2e52f7829f739f31d and VrfC2SimApp.dll
c86901be4e1f5f4effabd6a9022f0dbad8c4e4088777c1186e066ff4e802d74c, ProductVersion 1.0.0+git.80f707f.Release-5.2 (no
+DIRTY); appsettings.json 2ddbaba3...aae45a = src; VrfBridge.dll 90272bc95297e330...f25847 (the pin) in all eleven 5.2
consumers' bin\Release-5.2 [V 2026-09-27 00:25Z].
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: UG52 sec 23.3 p505 (Move Along Route: "There is no path planning done as it moves toward the next
vertex") and sec 23.2 / 23.2.1 p500 (only Move To plans a path); UG52 68.3.3 p1312 / 68.3.4 p1313 (a derived SMS's
scripts supersede the included set's - the mechanism of the proof line); UG52 Appendix C p1671
(loadAllNavigationDataOnTerrainLoad - navigation data otherwise loads lazily, after placement); UG52 23.6 p508
(autonomous actions gate path planning; ON by default); UG52 40.69 p896-897 Rules of Engagement ("Hold Fire. Do not fire
at targets."); singleTaskControllerComponent.h:191-200 decideToGiveUpTask "always returns false" (only this interface's
watchdog ever reports a stuck unit).

OWN-RECORD CITATION:
- The draft: planning lane notes 2026-09-26 (scratch u3\PREREG_IRONSTORM_LIVE1_DRAFT_NOTES.md; plan P7/P8 in
  u3\PLAN_IRONSTORM_DEMO.md). The owner's gate: "No, fix the corridor first" - satisfied by lane I1d (below).
- Lane I1d (merged a019918): corridor_gate `--preset ironstorm-cuta-h` on the maple area PASS, 29 sectors, 0 below 0.9,
  min 0.9000 at T02's (17,15); cut-A change (h) (tools/scenario/CHANGES.md) rerouted T10 through one waypoint
  54.029734 / 23.305499 (Line/Route graphic c8d9cd1a-b808-5b8d-97be-7beb98393a62, a fixed uuid5); fixture
  IronStorm_Centre_52_Nav_AG_maple registered in tools/FixtureGen/README.md; navdata README REGISTERED note.
- RUNBOOK 0.5.14 items 2, 5, 12, 18 (stdout to a file; quiet period; the nav-area READY gate; the holder), 9c (the
  persistent holder, StartFederationHolder52.ps1), 11f (the sim clock), 11h (re-clamp); DEMO_RUNBOOK sec 0.4 (pre-warm).
- Rulings: RL-20260926-01 (A1/A4 unit ATTACK fire at will; A6 CNFPSL held in place, STP-866; FOLSPT advance and
  hold); RL-20260925-01 (D2/D3/D4); RL-20260921-09 (the temporary completion position); RL-20260921-02 item 3 "3. Fast."
  (the sim clock, NOT DurationScale); RL-20260920-01 item 3 (route shift ON by default); STP-846 (T02 is STP's ATTACK
  fallthrough for RECEIVE), STP-847 (SystemName "Not Set"), STP-866.
- The two live runs of 2026-09-26: PREREG_COMPLETION_CONFIRM_2026-09-26.md (sec 3 log shapes; teardown finding) and
  PREREG_NAV_STALL_FALLBACK_2026-09-26.md (G1 held: area row before the order, proof line, mesh planning; P0(ii)'s
  READY-TO-TASK limb voided it by the letter on a cold 128-unit init - that limb is NOT carried here).

## 0. Purpose, in plain words

Iron Storm has never been seen to MOVE on this machine. The first cut-A drive (run 1, gen-1 area) froze; lane I1d
rebuilt the area with red maples (biome 04), rerouted T10 around the two sub-0.9 sectors, and the corridor gate
passed. This run is the first live look: does the fixture load its maple nav area before the order, does the
abstract-graph SMS run, do the three movers (28ID's T02, 48 IBCT's T14, 1-112 IN's T10) actually displace, and does
every task end by the time rules with clean reports. It is a demo-readiness confirmation at n = 1, not an experiment.

## 1. Decisions taken from the record

(a) PERFORMERS (seat notes, [A] re-read of data/unit-type-map-52.json:71 not repeated here): 28ID maps to F-GEN-ANY
(isAggregate false) = ONE M1A2 PLATFORM; 48 IBCT = F-GEN-H, one M577A2 PLATFORM; 1-112 IN = F-UCI-F Tank HQ Section, an
AGGREGATE of 6 members, created AtOrder. TaskDispatchPolicy.ForEngage (TaskDispatchPolicy.cs:128-137 [V]) gives fire at
will only to a UNIT; T02 is a platform ATTACK whose AffectedEntity is the performer itself (R3 self) -> AdvanceOnly, with
the order's ROEHold (RoeFor -> HoldFire). There is NO unit ATTACK in cut A, so the fire-at-will line is PREDICTED
ABSENT (P8).
(b) DurationScale 0.25 is plan P7 / CHANGES.md "recommended", NOT an owner ruling; RL-20260921-02 item 3 "3. Fast." is
the sim clock. It is used here because at scale 1.0 the three movers would sit 20 min behind two 20-min holds.
(c) THE TASKS (cut-A order sha256 3801c71b05c310db8eb26069044c9bfec9585214a228beec61700d31d79fefe8 [V], init
2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec [V]; both CRLF working-tree files; scaled seconds are
SIMULATION seconds):

| Task (uuid) | performer | verb -> decision | geometry / distance | start | armed end |
|---|---|---|---|---|---|
| T01 f7b52ba4... | 28ID (platform) | CNFPSL -> held in place (STP-866) | 4 graphics, not driven | delay 0 | 300 s, no destination |
| T13 37677c40... | 48 IBCT (platform) | CNFPSL -> held in place | 4 graphics, not driven | delay 0 | 300 s, no destination |
| T10 9aab7fe6... | 1-112 IN (aggregate, 6) | CRESRV -> bare move (Layer-2 not wired) | start -> waypoint -> PassagePoint_48_IBCT_SLOT0, 1,453 + 1,276 = 2,729 m | SimulationTime PT20M -> 300 s | 450 s, destination (6.1 m/s needed) |
| T02 696fbb33... | 28ID | ATTACK -> AdvanceOnly (R3 self), ROEHold | PassagePoint_28ID_SLOT0, 5,341 m [A] | after T01's TASKCMPLT (the PT20M RelativeTime is not read [A]) | 300 s, destination (17.8 m/s needed -> late path expected) |
| T14 1075b583... | 48 IBCT | FOLSPT -> advance along graphic and hold, ROEHold | FollowAndSupport_48_IBCT_SLOT1, 2,426 m (nudged) [A] | after T13's TASKCMPLT | 300 s, destination (8.1 m/s) |

(d) The FOLSPT dispatch line (VrfC2SimService.cs:4829-4833 [V]) still says "The supported unit is not in the order";
cut A's change (f) added 116 ABCT as a SECOND AffectedEntity, which OrderParser does not read. The wording is stale for
cut A and is NOT scored.
(e) The pre-warm (step W) needs its own runner launch - there is no init-only mode - so it pushes the order too and burns
its own appNumber block; it is unscored. The maple terrain copy is new to this machine's file cache.
(f) --parse-order does NOT resolve MapGraphicIDs [V: OrderParseCheck.cs prints the ids and the embedded points only];
T10's resolved vertex list is visible only in the live app's resolver lines and CreateRoute point count (P19).
(g) ONE engage-path line per DISPATCH PASS, not per task [V code]: ExecuteTaskOnTick is re-entered for the same task by the
route-shift worker (shiftedRoute) and by the TerrainProfile reply (terrainRoute, GroundWaypointAltitudeMode default
"TerrainProfile", VrfSettings.cs:822); the R3 / "Layer-2 not yet wired" lines (VrfC2SimService.cs:4248/4269-4281) sit
before both re-entry points and so print once per pass. The draft's "one L-R3SELF" is therefore scored as "one per pass"
(P8). The resolver lines are skipped only on the TerrainProfile pass (terrainRoute == null guard, :4150).

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: --pre-order-gate nav-area --pre-order-gate-timeout 900

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: the watchdog runs on the SIMULATION clock here (--env Vrf__StallClock=sim), window 360 SIM s
(the calibrated sim window; StallWindowSeconds 0 = the clock's default). Armed ends in SIM s from dispatch: T01 300 and
T13 300 (no destination - never watched); T02 300 and T14 300 (destination tasks: their armed end is INSIDE the 360 s
window, but an unarrived destination task goes OVERDUE and stays watched, and nothing is SENT at that end, so the end
cannot pre-empt or manufacture a stall verdict - the audit S8 symptom was a TASKCMPLT at an end time, which the
temporary position no longer sends for an unarrived mover); T10 450 (destination, outside the window). A stall verdict is
possible from dispatch + 360 SIM s onwards for T02 / T14 / T10 only.

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3); the runner path loads no Demo overlay and loading it would also change the application number, connection config and console levels.

DEVIATION FROM RECORD: the successor-gate floor is 60 s, not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:308); the computed wait is max(60, 300 + 60) = 360 SIM s, so T02 and T14 are not held two hours behind a late predecessor for no reason.

DEVIATION FROM RECORD: the pre-warm is a whole runner launch that also pushes the order and runs 120 s, where DEMO_RUNBOOK sec 0.4 says "let it reach the initialization, then stop it"; there is no init-only runner mode, and stopping the runner by hand would break the quiet-period rule.

DEVIATION FROM RECORD: consoles at level 4, not the demo setting of plan P7 ("--object-console 3 --member-console 3"); the house template requires level 4 for a movement run and the movers here are lone platforms (level 3 cannot tell a mover from a non-mover).

## 2. What the code emits - log-line shapes (src at 80f707f = a019918; VrfC2SimService.cs unless named)

The COMPLETION_CONFIRM sec 3 shapes (L-DISP, L-STRT, L-ARM-D, L-ARM-N, L-INPLACE, L-GATE, L-ARRIVE, L-HELD, L-END,
L-OVERDUE, L-IDLE, L-CMPLT-T, L-LATE, L-CMPLT-L, L-CMPLT-E, L-SKIP, L-STALL, L-CANCEL, L-SUPP, L-WDOG, L-CLOCK, L-RATIO,
L-PLACE, re-clamp L-RC-*, the retired lines) and the NAV_STALL sec 2 shapes (L-AREA, L-ORDER, L-PROOF, L-MEMBERS, L-PLAN,
L-DEFER, L-ENGAGE) stand; their line numbers have moved (L-DISP :5102 [V]). Added for this run [V: grep -F at a019918]:
- L-CNFPSL (:4447): `Task '<task>' (<unit>): CNFPSL (forward passage of lines): held in place for now - route by graphic
  role (start point -> passage point -> lane -> release point) is not implemented; see STP-866.` followed by the IN PLACE
  line (:4448) `... Executing IN PLACE at <unit>'s own position ...; NO VR-Forces task is issued and the <N> geometry
  point(s) this task carries are NOT driven.` and ONE Observation pushed (:4454-4462, "... not driven").
- L-R3SELF (:5047): `ATTACK task '<task>': the order names the PERFORMING UNIT as the affected entity ... THE TARGET IS
  THE OBJECTIVE (R3 ...) ... No fire against a named entity is issued.`
- L-FAW (:5471): `Task '<task>' (verb ATTACK, <unit>): ATTACK: advancing to the objective; rules of engagement set to fire
  at will ...` - PREDICTED ABSENT.
- L-FOLSPT (:4829): `Task '<task>' (verb FOLSPT, <unit>): advancing along the task's graphic to its end and holding there -
  no engagement task, rules of engagement as ordered ('ROEHold'). ...`
- L-BARE (:4248): `Task '<task>' verb=CRESRV -> intent=HoldObjective (...); Layer-2 not yet wired - executing bare
  movement.`
- L-RESOLVE (TaskGeometryResolver.cs:424 / :467 / :489 / :496, logged as `Task '<task>': <line>.`): `MapGraphicID <u> ->
  <name> (<kind>): <n> vertex(es) dropped - they ARE the taskee's own position ...`; `path from MapGraphicID <u> -> <name>
  (<kind>, <n> vertices): <k> vertex(es) joined ...`; `destination from MapGraphicID <u> -> <name> (<what>): appended as
  the route's DESTINATION ...`; warnings (:473 NOT continuous, :500 extra destinations, :516 RETURNS ... TRUNCATED).
- L-ROUTE (:4996): `Task '<task>': CreateRoute '<route>' (<N> pts) for <unit>; move deferred to route-created.`
- L-SHIFT-ON (:720) `LATERAL ROUTE SHIFT ON (...)`; L-SHIFTED (:6328) `... ROUTE SHIFTED <d> m <side> ...`; L-SHIFTQ (:6290).
- L-CENSUS (:1635): `CreationPolicy=AtOrder (C13): <S> unit(s) created as EMPTY shells ... <P> platform(s) created in full.`

## 3. Sequence and exact command lines

All from Git Bash at the MAIN checkout C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\Software\
Interfaces\VRF_C2SIM (= F:\Repos\...\VRF_C2SIM, HEAD a019918), because the runner resolves its exes, tools and ledger
from its own root; it reads the order and init from main's data\ (identical to this branch's). The main checkout's
working-tree docs/OPUS_EXECUTION_PLAN.md receives this branch's ledger text before the holder (so the runner's marker read
sees 5137), and the runner's own two blocks are carried back to this branch afterwards (lane R's procedure).

A. Read-only checks: `echo "$DOTNET_ENVIRONMENT"` empty; `env | grep '^Vrf__'` empty; NO vrfNavGenerator / dotnet build
   / MSBuild / VBCSCompiler / vrfSim / vrfGui / VrfC2SimApp / WatchVrf / ListenReports / RtiProbe process; rtiexec 47980 +
   rtiForwarder 50740 alive (else `scripts/StartRtiExec52.ps1` - starting is not a restart); rtiAssistant 30240 never
   touched; REST 200 on 18080; fixture sha256 57465c35...0e32; order/init hashes; `derive_ironstorm_cuta.py --check`.
B. No build (Registration). Version string and hashes re-read before W and after E.
C. Order validation (no federate, no appNumber):
   - C1 `VrfC2SimApp.exe --parse-order data/IRONSTORM_CUTA_Order.xml`: EXPECT "Tasks: 5"; durations 1200000 ms x 4 and
     1800000 ms (T10); T10 lists mapGraphic c8d9cd1a-b808-5b8d-97be-7beb98393a62 THEN cc23071f-aa60-9f52-884c-11505051cc99;
     T02 startAfter f7b52ba4..., T14 startAfter 37677c40...; 0 WARN lines.
   - C2 `VrfC2SimApp.exe --parse-init data/IRONSTORM_CUTA_Initialization.xml "Not Set"` (Program.cs:20-21: the third
     argument is the client id): EXPECT 36 of 40 units in scope.
   - C3 ONE real push to the PRIVATE server (never StompProbe, never 8080/61613):
         tools/PushInit/bin/Release/net10.0/PushInit.exe data/IRONSTORM_CUTA_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
         tools/PushOrder/bin/Release/net10.0/PushOrder.exe data/IRONSTORM_CUTA_Order.xml 30 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
     EXPECT both exit 0, PushOrder not exit 4, one `ORDER (<n> chars)` echo carrying 5 tasks. Any failure = STOP.
D. THE HOLDER FIRST (the 2026-09-26 holder 34096 has RESIGNED; no RtiProbe is up [V 00:22Z]):
       "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5133,5134,5135,5136 -SettleSecs 28800 -WhatIf
       "/c/Program Files/PowerShell/7/pwsh.exe" -NoProfile -File scripts/StartFederationHolder52.ps1 -AppNumbers 5133,5134,5135,5136 -SettleSecs 28800 < /dev/null > runs/launch52/is1-holder-<stamp>.log 2>&1
   EXPECT `HOLDER JOINED: pid ... appNo 5133`, exit 0. Exit 1 (none joined) = STOP (RUNBOOK 9c).
W. PRE-WARM (unscored): the step-E command with `--run-secs 120` and `--log runs/launch52/RunScenario-ironstorm-prewarm-
   <stamp>.log`, dry run first. GATE for E: the runner's StopVrf exit is 0 or 6 and the post-W inventory shows no vrfSim /
   VrfC2SimApp / WatchVrf / ListenReports. Recorded: the gate's WARM/COLD and wait, the init PLACEMENT line.
E. THE RUN, once, dry run first, stdout to a FILE, never piped:

       scripts/RunScenario.sh \
         --scenario IronStorm_Centre_52_Nav_AG_maple \
         --init data/IRONSTORM_CUTA_Initialization.xml \
         --order data/IRONSTORM_CUTA_Order.xml \
         --client-id "Not Set" \
         --duration-scale 0.25 \
         --pre-order-gate nav-area --pre-order-gate-timeout 900 \
         --object-console 4 --member-console 4 \
         --no-stop-when-complete --run-secs 2700 \
         --env Vrf__StallDetection=true \
         --env Vrf__StallClock=sim \
         --env Vrf__TaskPredecessorTimeoutSeconds=60 \
         --sample-threads \
         --log runs/launch52/RunScenario-ironstorm-live1-<stamp>.log

   Defaults kept: --profile 5.2, --no-gui, REST/STOMP 18080/61614, route shift ON (the shipped default), the runner's
   Stage 2h holder (it JOINS the federation the persistent holder keeps).

QUIET PERIOD (RUNBOOK 0.5.14 item 5): from step W to the post-run inventory after E: no Stop-Process / taskkill of any
kind (StopVrf52's own identity-gated force of the run's own back end is allowed), no build, no suite, no subagent or
other agent session started by this lane, no second runner. Foreground polling of the run's own files with bounded
waits. Never touched: rtiexec, rtiForwarder, rtiAssistant, the holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a STOP, not an adjustment)

Every count is PER TASK UUID, read from the scored run's vrfc2simapp.log in line order and cross-checked against
reports-captured.log; displacement from watchvrf-trace.csv POS rows (per object: first fix after its L-DISP vs every
later fix; for T10 per member). Short uuids as in sec 1c.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P0 | PRECONDITIONS. (i) Persistent holder alive at launch; the back end JOINS (rtiexec count-grep: no create for its appNo); "READY - joined the federation". (ii) L-CENSUS reads 4 EMPTY shells and 32 platforms (36 = C2's in-scope count). (iii) L-WDOG once, naming the SIMULATION clock; L-CLOCK names SIMULATION and "Vrf:DurationScale=0.25"; L-SHIFT-ON once. (iv) The runner's stage 7d gate FIRED (no NOT-READY, no exit 3). (v) Exe / dll / bridge hashes as registered, before and after. (READY TO TASK is RECORDED, not scored.) | HIGH | Any limb = VOID + STOP (launch, terrain or harness failure - no code verdict). | |
| P1 | WARM: the init's L-PLACE line reads "36 of 36 create altitude(s) came from the TERRAIN QUERY, 0 from the FALLBACK". | HIGH | Any FALLBACK create in the init = STOP (the pre-warm did not warm). | |
| P2 | AREA: the FIRST L-AREA row names `NavArea-ground-platform IRONSTORM-CENTRE_maple` and precedes L-ORDER; zero L-AREA rows name any other area (empty-area rows RECORDED); runner "NAV AREA ACQUIRED ... IRONSTORM-CENTRE_maple". WARM/COLD and the gate wait RECORDED. | HIGH | No row before L-ORDER, a gate timeout, or another named area = STOP. | |
| P3 | SMS: >= 1 L-PROOF line (`C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true`) in the app log. Per mover (28ID, 48 IBCT, a 1-112 IN member by L-MEMBERS) >= 1 each. | HIGH (>= 1); MEDIUM (per mover) | Zero = STOP; a mover with none = recorded MEDIUM miss. | |
| P4 | DISPATCH STRUCTURE: exactly one SENT TASKSTRT per uuid (5); T02's L-DISP after T01's SENT TASKCMPLT and T14's after T13's; one "Task 'T10...': start delay 300 s (order says 1200 s; Vrf:DurationScale=0.25) - it will not dispatch before then." line (:3988) and T10's L-DISP after it; ZERO L-SKIP / "SKIPPED: predecessor". | HIGH | A missing or duplicate TASKSTRT; a successor dispatched before its predecessor's TASKCMPLT; any SKIP = STOP. | |
| P5 | RE-CLAMP: an L-RC-PASS "is ON the terrain" gate line (VrfC2SimService.cs:3517, printed at dispatch when the terrain-profile reply carries samples; Vrf:PlacementReclamp default true) for each of T02, T14, T10; ZERO "measured OFF the terrain"; ZERO retired lines (`NOT DISPATCHED YET`, `HELD as`, `REFUSED [`, `BOUND-BUT-NOT-ON-THE-GROUND`). A mover whose terrain profile fell back to Live (the 10 s timeout line) has no gate line: recorded NOT MEASURED, not a miss. | HIGH | An OFF line or a retired line; a mover with a profile reply and no gate line = STOP. | |
| P6 | HEADLINE: T02's 28ID platform AND T14's 48 IBCT platform are each displaced more than 50 m from their first POS fix after L-DISP, by window end (trace). | HIGH | Either at or under 50 m = STOP. | |
| P6b | T10: at least one 1-112 IN member displaced more than 50 m after T10's L-DISP (trace). | HIGH | No member over 50 m = STOP. | |
| P6c | Per mover: L-PLAN lines ("Planned path has N points/parts", "Is destination in nav area?"), max speed and net displacement from the trace, and any mesh "not enough (0) points" lines. | RECORDED | - | |
| P7 | CNFPSL HOLDS T01 and T13: each has exactly one L-CNFPSL + one IN PLACE line; one Observation in the capture naming STP-866; an armed line "it has no destination" at 300; NO L-ROUTE / MoveToLocation for it; exactly one SENT TASKCMPLT "(300 s after dispatch)". | HIGH | A move issued for a hold; zero or two TASKCMPLTs; a missing L-CNFPSL; an OVERDUE for a hold = STOP. | |
| P8 | T02 PLATFORM PATH: L-R3SELF for T02 on every dispatch pass and on no other task (passes = 1 + L-SHIFTQ lines for T02 + terrain-profile-request lines for T02); ZERO L-FAW, ZERO `FireAtTarget`, ZERO `deferred until the move COMPLETES` lines in the whole log. | HIGH | Any fire-at-will / FireAt / deferred-engage line; no L-R3SELF for T02; an L-R3SELF count unequal to T02's pass count = STOP. | |
| P9 | T14 FOLSPT: exactly one L-FOLSPT naming 'ROEHold'; an L-ROUTE for T14 and an L-ARM-D (destination) at 300. | HIGH | Missing, duplicated, another ROE, or no route move = STOP. | |
| P10 | T10 CRESRV: L-BARE for T10 (verb=CRESRV -> intent=HoldObjective ... executing bare movement); an L-ROUTE for T10 and an L-ARM-D at 450; no IN PLACE line for T10. | HIGH | Any limb failing = STOP. | |
| P11 | COMPLETION PER THE TIME RULES: each mover (T02, T14, T10) takes EXACTLY ONE path - (a) early: arrival, one L-HELD, one TASKCMPLT at L-END "had already arrived"; (b) late: one L-OVERDUE, nothing SENT at the end, then arrival, L-LATE, one L-CMPLT-L; (c) not arrived by window end: L-OVERDUE and no terminal report; (d) stalled: L-STALL + one SENT TASKABRT "STALLED". Which path: RECORDED; T02 late (b) or (c). | HIGH (exactly one path each); MEDIUM (T02 late) | Two paths, or a TASKCMPLT at an end time for an unarrived mover = STOP. T02 early (a) = recorded MEDIUM miss. | |
| P12 | REPORT HYGIENE: ZERO L-SUPP for a terminal code; at most one terminal report per uuid; ZERO `task=(none)` SENT lines; capture and log agree; "Reports this run: ... 0 FAILED". | HIGH | Any of these = STOP. | |
| P13 | STALLS / FALLBACKS: every L-STALL, OVERDUE and arrival is REPORTED with the member displacement over the preceding 360 SIM s and since dispatch (the displacement rule). | RECORDED | - | |
| P14 | NO ENGAGEMENT: the WASA hostiles are ~44 km from every route against a 3-4 km weapon reach [A]; zero L-ENGAGE / FireAt lines (P8) and zero vendor-console lines with "Fire Weapon" in the app log. | HIGH | Any engagement line = STOP (the ROE or geometry is not what was registered). | |
| P15 | ROUTE SHIFT: L-SHIFT-ON once; ZERO L-SHIFTED lines (no cut-A leg is flagged on the maple area; I1d leg_check 0 water on the three movers' legs). | MEDIUM | A ROUTE SHIFTED line (recorded; P19 then counts its inserted vertices). | |
| P16 | CLOCK: every L-RATIO line; the SIMULATION stamp of each L-DISP; the sim/wall band. | RECORDED | - | |
| P17 | RUN HEALTH: no `BACK END LOST`; no WS-runaway exit 6 (runner); no new .dmp / .callstack.log for this back-end pid (names only); VrfC2SimApp exits 0; rtiexec, rtiForwarder, rtiAssistant 30240 and every holder untouched. | HIGH | Any limb = VOID. | |
| P18 | TEARDOWN, scored on StopVrf52's exit code inside the runner: 0 or 6 (6 = the run's own back end force-stopped by identity after a refused close), and no vrfSim / VrfC2SimApp / WatchVrf / ListenReports left. | HIGH | Exit 3, 5 or 7 = STOP. | |
| P19 | T10 RESOLVED ROUTE (the interface's own resolver): T10's distinct L-RESOLVE lines are exactly - graphic c8d9cd1a (T10_Reroute_1-112_IN__CUT_A_H_ROUTE_AROUND_SECTORS_28_21_28_22): "(line): 1 vertex(es) dropped - they ARE the taskee's own position" (its first vertex IS 1-112 IN's authored position) and "path from MapGraphicID c8d9cd1a-b808-5b8d-97be-7beb98393a62 -> T10_Reroute_... (line, 1 vertices): 1 vertex(es) joined" (the count printed is AFTER the drop, TaskGeometryResolver.cs:427/467 [V]); graphic cc23071f-aa60-9f52-884c-11505051cc99 -> PassagePoint_48_IBCT_SLOT0 "(<kind>, 1 vertex): appended as the route's DESTINATION"; zero resolver WARNINGs for T10; no "dropped N leading route point(s)" line for T10. So the route is start -> 54.029734 / 23.305499 -> PassagePoint_48_IBCT_SLOT0, and T10's L-ROUTE reads "(3 pts)" (3 + 2 per L-SHIFTED line for T10, if any). C1 shows both MapGraphicIDs on T10, in that order. | HIGH | Any other vertex list: a different drop/join count, a missing or different destination, a resolver warning for T10, or an L-ROUTE point count other than 3 + 2 x shifts = STOP. | |

STOP RULES:
- A missed HIGH row is a STOP: record it, no patch, no re-run under this registration, nothing adjusted.
- P0 or P17 failing makes the run VOID. Two identical launch failures in a row: no third (RUNBOOK 9c).
- The executor never intervenes in the window; a live read is for watching only.
- A VOID or STOPPED run is re-registered as IRONSTORM_CUTA_LIVE-<date>-2, with new appNumbers.

ONE VARIABLE: none - a first live confirmation of a new scenario, fixture and area. The control for "Iron Storm moves"
is run 1 (gen-1 area, froze); it differs in the area, the fixture, T10's route, the build and the pre-warm, so no
single-variable claim is made from the comparison.

## 5. Application numbers

The Appendix B marker read `*** NEXT FREE: 5133 ***` at a019918 [V]. This registration claims 5133-5158 and leaves the
marker at 5159.
- 5133-5136: persistent holder (step D), HAND-CLAIMED in this commit; marker advanced to 5137 here. Expected: 5133
  CONSUMED on a first-attempt join; 5134-5136 BURNED.
- 5137-5147: the PRE-WARM runner block (step W), written by the runner at its Stage 2: back end 5137, front end 5138
  (BURNED, --no-gui), WatchVrf pre-check 5139, WatchVrf trace 5140, VrfC2SimApp 5141, RtiProbe 2c 5142, CreateOne 5143
  (BURNED unless the oracle gate fails), Stage 2h holder attempts 5144-5147 (5144 JOINS; 5145-5147 BURNED).
- 5148-5158: the SCORED runner block (step E), same layout: 5148 back end ... 5155-5158 Stage 2h attempts.
PushInit / PushOrder / ListenReports / StopIface are C2SIM clients, not federates. A launch that aborts burns its whole
block; a relaunch needs a NEW registration and new numbers.

## 6. Harvest (after the run, read-only) and where results go

From the run directory (runs\launch52\last-run-dir.txt): vrfc2simapp.log, reports-captured.log, c2sim-bus.log, the
manifest, watchvrf-trace.csv, thread-samples.csv (+ alerts), holder logs, stopvrf logs, the wrapper log. Vendor sim logs
dump the environment in cleartext: count-grep only, never quoted. Results go to: the Result block below (measurement and
design implication in separate sentences); DEMO_READINESS Iron Storm rows; HANDOFF sec 6 (200 x 160 cap);
tools/FixtureGen/README.md (IronStorm_Centre_52_Nav_AG_maple live-proven or not, by P1/P2/P3); Appendix B annotated from
the manifests. ASCII + CRLF throughout.

## 7. What this run does NOT claim

- No unit ATTACK or fire at will (none in cut A); no BREACH; CNFPSL as a real passage (held in place); T10 as a reserve
  posture (it is a bare move); following 116 ABCT (not read); T02 as an attack (it is STP-846's fallthrough for RECEIVE).
- Nothing about the 0.82-0.90 corridor band, vegetation fidelity (red maples replace biome 04), or why run 1 froze.
- Nothing about the full 23-task order, the demo server (8080/61613), the GUI, or the demo profile as a whole.
- No timing generalisation: n = 1, one host, one fixture, a load-dependent clock.

## Result (written after the harvest, never from a live read)

Written 2026-09-27 ~00:50Z by lane IS1 (session 5fc25950) from the harvested files of the PRE-WARM run
20260927T003120Z_run (runs\ in the main checkout): its stopvrf.stdout.log, runner log
runs\launch52\RunScenario-ironstorm-prewarm-20260927T003119Z.log, run-manifest.json, thread-samples.csv and a streamed
grep pass over vrfc2simapp.log (269,577 lines). The vendor sim log was not opened.

### VERDICT: STOPPED AT THE STEP-W GATE. The scored run (step E) was NOT launched; NO prediction P0-P19 was scored.

Sec 3 step W: "GATE for E: the runner's StopVrf exit is 0 or 6". The pre-warm's teardown returned StopVrf exit 7 (runner
log L282-283 "StopVrf: EXIT=7"; runner exit 4 "TEARDOWN INCOMPLETE"). Nothing was adjusted, patched or re-run.

What the exit 7 was, measured [V]: stopvrf.stdout.log - the graceful close was refused ("taskkill /PID 30600 (no /F):
SUCCESS", then TIMEOUT after 120 s), StopVrf52 forced its own back end by identity ("FORCED - graceful close refused ...
Stop-Process -Id 30600 -Force ... (within 2 s of the recorded start)"), then "still running after 120s:
vrfSimHLA1516e(pid 30600). pid 30600 was FORCED; the processes listed are what is left." and "Exit 7". The only process
listed as left is the forced pid itself. thread-samples.csv: pid 30600 present at 00:39:46.167Z (4,311 MB, 81 threads)
and "process gone" at 00:39:51.173Z. Post-run inventory 00:39:59Z [V, Win32_Process]: no vrfSim / vrfGui /
VrfC2SimApp / WatchVrf / ListenReports; rtiexec 47980, rtiForwarder 50740, rtiAssistant 30240, persistent holder 42672
and the run's Stage 2h holder 49984 up and untouched. scripts/StopVrf52.ps1:563-575 waits at most 15 s for a forced pid
to vanish before it lists what is left, and :597 prints "other VR-Forces processes are still up" whenever anything is
listed. Measurement: the forced back end outlived that 15 s wait and was gone within about 20 s. Design implication, stated
separately: exit 7 as coded does not distinguish "the forced pid is still exiting" from "another VR-Forces process is up";
the gate that read it is correct as registered, and whether exit 7 should be reclassified is a code question, not a
verdict of this run. No cause is claimed for the refused graceful close (third refusal in three 2026-09-26/27 runs).

UNSCORED OBSERVATIONS OF THE PRE-WARM (120 s window; recorded because they bear on the next registration; they are not
scores, the pre-warm was registered unscored):
- Area: first L-AREA row L762 "... New Primary nav area: | NavArea-ground-platform IRONSTORM-CENTRE_maple" before L788
  "C2SIM Order received (66359 bytes)."; runner "NAV AREA ACQUIRED after 0s of gate", placement -> area 25.2 s, WARM.
- Init: L217 census "4 unit(s) created as EMPTY shells ... 32 platform(s) created in full"; L383 "PLACEMENT summary: 0 of
  36 create altitude(s) came from the TERRAIN QUERY, 36 from the FALLBACK" (a cold init - what the pre-warm is for); L389
  "READY TO TASK - NOT REACHED within 20 s".
- Tasks: T01/T13 hold-in-place with the CNFPSL line (L824/L852), TASKCMPLT "(300 s after dispatch)" (L19979/L19985); T10
  resolver lines L1213/L1215/L1217 = "1 vertex(es) dropped", "(line, 1 vertices): 1 vertex(es) joined", PassagePoint
  "appended as the route's DESTINATION"; "ROUTE SHIFT - no leg flagged" (L1265); L1299 "CreateRoute 'T10 ... ROUTE' (3
  pts)". T02: three R3 lines on three passes (L20003/L20705/L21121), CreateRoute (2 pts), OVERDUE at 304 s (L111509), then
  ARRIVAL EVIDENCE and L168159 TASKCMPLT "arrived after its task's end time". T14: FOLSPT line naming 'ROEHold' (L20481),
  OVERDUE (L111511), then L158309 "STALL: unit 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE task T14...: no member
  moved more than 50 m in the last 360 SIM s (max 0.0 m); TASKABRT reported." and its TASKABRT (L158311). The 48 IBCT
  platform's console shows move-along beginning at SIM 377.996 and no other distinct line from that object after it (cause NOT claimed).
  [CORRECTION 2026-09-27: "max 0.0 m" is the 360-SIM-s window figure; the unit had moved ~850 m then stopped - see below.]
  T10: OVERDUE at 478 s (L150099), no terminal report by window end.
- Clock: SIM/WALL 9.139 then 6.838 (L76679, L193851). Reports: "624 delivered, 0 FAILED" (L269573). Zero fire-at-will /
  FireAt / deferred-engage lines; zero retired lines; zero SUPPRESSED.
- App log ends with one "fail: C2SIM.C2SIMSDK[0]" line at L269576 (after the report tally; teardown-time; not examined).

Measurement: the fixture loaded its maple area before the order and all five tasks dispatched inside the 120 s window; one
of the two headline movers (28ID) arrived and the other (48 IBCT) did not move at all [REFUTED - see CORRECTION
2026-09-27 below] and was reported stalled. Design
implication, stated separately: a -2 registration should not carry P6's "48 IBCT displaced > 50 m" at HIGH without first
reading why the 48 IBCT platform's move-along produced no movement here (UG52 23.3 / its own member console), and the
step-W gate needs a decision on how a forced-but-still-exiting back end is read.

CORRECTION 2026-09-27 (lane T14; the text above is kept as written). "48 IBCT did not move at all" is WRONG. The
independent WatchVrf trace (runs/20260927T003120Z_run/watchvrf-trace.csv, POS rows of VRF_UUID:247147e3, the M577A2
the app bound to 48 IBCT) shows it drove about 850 m along its leg in about 15 wall s after dispatch, then stopped dead
at 54.026779, 23.317195 (alt 128.8) about 1,575 m short of its destination and stayed there to the last POS row (54
identical rows). The STALL line's "(max 0.0 m)" is the displacement inside the stall watchdog's 360-SIM-s SLIDING
window (VrfC2SimService.cs SampleAndJudgeStall, measured against the oldest sample in the ring), not the displacement
since dispatch. By the letter, P6 for 48 IBCT (> 50 m from its first POS fix after L-DISP) would have been met at
about 850 m. The implication above is superseded: the question is why it STOPPED, not why it did not move. Answer
(offline, [V] config chain + geometry): the stop point is 0.5 m outside OSM way 197345448 (natural=water, the lake
Jezioro Wiersnie), which the vendor land-cover composite resolves to deep-water, acceleration-factor 0.0; the
change-(e) nudge had been checked dry against CLCplus only. Record: FINDING_IRONSTORM_T14_STOP_2026-09-27.md. The cut-A
order now routes T14 round the lake (IRONSTORM_CUTA_CHANGES.md change (i)).
