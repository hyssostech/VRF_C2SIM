# PREREG - IRON STORM ON THE AGGREGATE PROFILE, RUN G1-6: G1-5 re-run with ONE scorer change (a narrow culvert clause) - the first SCORED aggregate cut-A run with arrivals, doubling as a rehearsal of the demo's runner path (one unscored pre-warm + one scored run)

STATUS: PASSED 2026-10-04 - W GATE PASS (culvert rule; the ways rule FAILS both runs, RECORDED); scored run 79 PASS / 0 FAIL,
every HIGH row and H6 held (the Result below; Fable ACCEPT WITH FIXES, applied).
Was: REGISTERED 2026-10-04, LAUNCH PENDING - the seat's go-live under RL-20260928-01; Fable cold review owed
(RL-20260928-04). Registered by lane G1-6 on branch prereg/g1-6 from main 3d64dba, BEFORE any order push, holder action or
launch. PREPARATION ONLY: no C2SIM push, no rtiexec or holder start, no VR-Forces or runner launch, no appNumber claimed, no
edit of docs/OPUS_EXECUTION_PLAN.md, no C:\MAK write, no build. CUT TO THE MINIMUM by RL-20261004-04 (demo readiness first;
the culvert question is deferred): one clause, one constant, the fail-first set of sec 1(z), nothing else new. Marks: [V] =
checked by this lane while writing this file (read-only hashes, process list, the scorer on real run directories, offline);
[A] = taken from the record, not re-checked. This file is G1-5's registration (PREREG_IRONSTORM_AGG_G1-5_2026-10-04.md, "G1-5"
below) as a DELTA: every section of G1-5 not restated here applies unchanged. Scratch <L6> = H:\claude\F--Repos-C2SIM-
c2simVRFinterfacev2-36\7207877b-f104-4515-91f7-c662e3fce85c\scratchpad\laneG1-6 (every g1_6_* script and output below).

## Registration

PREREG ID: IRONSTORM_AGG_G1-2026-10-04-6
DATE (UTC): 2026-10-04, about 22:00Z, before any launch (the registration commit's timestamp is authoritative)
BINARY / COMMIT: THE SAME BUILD AS G1-5 - the deployed main-checkout build 1.0.0+git.52f50e0.Release-5.2 (W1 DEPLOY, RUNBOOK
sec 9, managed only), bridge pin 03226dd0. Read-only hashes at 2026-10-04T21:48Z [V], all equal to G1-5's registration:
VrfC2SimApp.exe 9253e7f3..., VrfC2SimApp.dll deb658eb..., appsettings.json e9c742e9..., appsettings.Demo.json 09bf49bc...,
VrfBridge.dll 03226dd0...; exe written 2026-10-04 19:16:08.86Z. `git diff --stat 52f50e0 3d64dba -- src tools data config`
is EMPTY [V]; `git diff --stat b0bad53 3d64dba -- scripts` = EXACTLY scripts/StartRtiExec52.ps1 (lane HX's log finder, G1-5
Result N4 (d), merge 4df7749) [V] - a harness-only change: the runner's Stage 2r and step R call it, and it never kills or
restarts an rtiexec. THE SYSTEM HAS NO NEW VARIABLE: same build, order, fixture, init, composition, settings, command line
and sequence as G1-5.
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: G1-5's list, unchanged. For the culvert clause there is NO vendor text: MAK ONE 2025 Adding Content sec 7.8
(docs/vendor/mak-5.2/txt/MAK_ONE_2025_Adding_Content.txt :8736-8800) lists the aggregate mobility classes and priorities but
is silent on culverts and on how priority resolves a road over a river line [A: G1-5 Result N1 (d) ADDENDUM]; UG52 23.2 p501
(the planner "finds a route on a network of road features") is why a crossing is judged on the road ways at all (G1-5 sec
Registration). The clause therefore rests on G1-5's observation (N1), not on the docs - recorded as such.

OWN-RECORD CITATION:
- RL-20261004-03 (G1-5's W FAIL binds as scored; the culvert clause belongs to G1-6, registered before its run, fail-first on
  G1-5's pre-warm); RL-20261004-04 (demo readiness first; the culvert question deferred - this registration is the minimum);
  RL-20261004-01 (H5 on every tasked member; the ways rule), RL-20261004-02, and G1-5's list (RL-20260927-01..-06,
  RL-20260928-01..-04 and the rest) [V: docs/RULINGS.md].
- G1-5 in full: sec 1(z) (the ways rule and its constants, APPROVED by the seat, sec 8 item (4)), the Result N1 (the culvert
  crossing: road 300614718 over culvert way 1467512812, 17 members, every fix on the road network), N3 (T14 vertex 2 not
  reached at the 120 s cap), N4 (the harness defects) and the ADDENDA (Adding Content silent; the speed dip UNEXPLAINED).
- docs/PLAN_MOVEMENT_2026-09-27.md CLOSED block and rows G1-4, G1-5 and the G1-6 step (:101) [V].
- docs/HANDOFF_SEAT_2026-09-28.md sec 3 items 3-4 (live state: rtiexec 65540, forwarder 58844, holder 67984 on appNo 5281 to
  about 04:22:51Z 2026-10-05; marker 5296) [V: process list 21:48Z shows exactly 65540, 58844, 67984].
- THE CONTROL: IRONSTORM_AGG_G1-2026-10-04-5 (G1-5; pre-warm 20261004T202533Z_run, STOPPED at its W gate).

## 0. Purpose, in plain words

G1-6 is the first SCORED aggregate cut-A run with arrivals, and doubles as a rehearsal of the demo's runner path
(scripts/RunScenario.sh, DEMO_RUNBOOK sec 1 "Way A", headless: --no-gui and consoles at 4, as G1-5). G1-5 passed every W
limb but one: its scorer read 17 T14 members driving road 300614718 over a culvert as a river crossing off a bridge (G1-5
Result N1). G1-6 re-runs G1-5 with ONE scorer change - the culvert clause of sec 1(z) - and the merged harness fixes.

## 1. Decisions taken from the record

(a)-(aa) - G1-5's, unchanged. Differences:
- (x) THE SCORER: <L6>\g1_6_score.py = G1-5's g1_5_score.py + sec 1(z), derived by the asserted edit script
  <L6>\g1_6_edit_score.py (14 edits, each old text present exactly once) [V].
- THE HARNESS: lane HX's corrected g1_6_rti_federates.py (both join-line forms) and g1_6_golive_checks.ps1 (-AllowStage2hPid
  for the post-W inventory), copied to <L6>; the golive check's scripts-diff line now says "expected G1-6: EXACTLY
  scripts/StartRtiExec52.ps1 - anything else = STOP" [V: its -SelfCheck 0 FAILED, <L6>\g1_6_golive_checks_selfcheck.txt].
  g1_6_runner.sh, g1_6_c3_push.sh and g1_6_claim_holder.ps1 are G1-5's with the log names, scratch paths and default
  number (5296) changed only [V: diff].

(z) THE CULVERT CLAUSE (RL-20261004-03), NARROW - --bridge-rule culvert, the new default. G1-5's ways rule is unchanged and
applies first; where it reads a crossing OFF, the crossing is ON THE ROAD only when ALL of:
- (a) a waterway way tagged tunnel=culvert (any waterway kind) intersects a DRIVABLE road way (the same filter as the bank
  test: highway present, not NoDriveHighway, not area=yes), AND that culvert lies on THE CROSSED RIVER - it is the crossed way,
  or a vertex of it lies within BRIDGE_RIVER_TOL_M 5 m of the crossed way's line (G1-5's existing "over that same river" test,
  reused; no new constant). Lane G1-6's narrowing beyond the three-part brief: without it a long chord could cross an
  unrelated river near some road-over-culvert and read ON (sec 8 item 1).
- (b) both bank fixes within ROAD_TOL_M 15 m of a drivable road (G1-5's bank test).
- (c) the chord passes within CULVERT_TOL_M = 15 m of that road x culvert intersection point.
THE ONE NEW CONSTANT, CULVERT_TOL_M 15 m, registered and never changed after the run. WHY 15: on G1-5's 17 real crossings
(6 distinct chords, 67-170 m) the chord passes 1.0-9.2 m from the road x culvert point (54.019835,23.328533) [V:
g1_6_ii_g15prewarm_culvert.txt] - the offset is the road's bend off a straight chord between fixes 2 trace-s apart (G1-5
Result N1 (b), 1.2-10.2 m to the centreline); 15 m covers the worst (9.2 m, INF1HQ1's 170 m chord) with 5.8 m margin, equals
BRIDGE_TOL_M and ROAD_TOL_M (the scorer's existing width for "on the road / on the bridge"), and sits well inside the 40 m
dirty control (44.3 m, fires). Its BLIND ZONE: a ford within about 15 m of a road-over-culvert on the same river, both banks
on roads, reads ON. Every crossing line prints the culvert measure ("CULVERT CLAUSE: culvert <id> x road <id> at ...: the
chord passes <d> m from it ...; the crossing point <d> m from it").
FAIL-FIRST ON REAL DATA [V: <L6>\g1_6_failfirst.sh -> the files named]:
- (i) THE CONTROL - G1-5's pre-warm under G1-5's rule (`--wgate --bridge-rule ways`): FIRES, "W GATE: FAIL (branch A;
  P-FALS(e))" - the same output as G1-5's own W gate file line for line, the header aside (g1_6_i_g15prewarm_ways.txt).
- (ii) THE NEW RULE, the same run (`--wgate`): all 18 crossings ON (the 17 T14 crossings by the culvert clause, chord 1.0-9.2
  m from the point; T02's by the 15 m bridge acceptance), "P-FALS (e) ... (bridge rule: culvert): PASS", "W GATE: PASS"
  (g1_6_ii_g15prewarm_culvert.txt).
- (iii) UNCHANGED VERDICTS: G1-4's pre-warm 20260928T225800Z_run - "W GATE: FAIL (branch A; H5, SL0, SL2)" under both the
  ways and the culvert rule, P-FALS (e) PASS in both (its crossing ON by the bridge 218414262); G1-2's scored run
  20260928T142731Z_run (`--member-names c1c`) - 0 crossings, P-FALS (e) PASS under both (g1_6_iii_*.txt).
- (iv) THE DIRTY CONTROL - G1-5's real 48_IBCT.INF1RIF1 track translated 40 m WEST along the river, a 20 m synthetic road
  stub under each bank fix: its chord cuts river 197345450 41.3 m from C and passes 44.3 m from the road x culvert point -
  FIRES under the culvert rule (and the ways rule); the untranslated track (the oracle) is ON under the culvert rule and OFF
  under the ways rule (g1_6_culvert_control.py -> g1_6_iv_control.txt, "ALL AS REQUIRED").
THE SELFTEST: g1_6_score.py --selftest 76 PASS, 0 FAIL = G1-5's 71 unchanged plus 5 synthetic culvert cases - the ways rule
fires beside a road-over-culvert, the culvert rule does not (chord 12 m from the point); and the culvert rule FIRES with the
chord 50 m beside the culvert (banks on road stubs), with the culvert under NO road, and on another river 8 m from the culvert
not joined to it (g1_6_score_selftest.txt).

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: none (G1-5's); the order is pushed right after the runner's stage-7 oracle gate.

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: G1-5's, unchanged (the watchdog on the SIMULATION clock, window 360 SIM s, 50 m; T02 / T14 300
and T10 450 are destination tasks; T01 / T13 never watched).

DEVIATION FROM RECORD: the template's "PRE-ORDER GATE: --pre-order-gate nav-area" ("gate PushOrder on the first "New Primary nav area" row; warm the area first") is not used - the aggregate model set loads no nav data (G1's to G1-5's, unchanged).

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3) (G1-3's to G1-5's, unchanged).

DEVIATION FROM RECORD: the successor-gate floor is 600 s - appsettings.json's shipped value - not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:357) (G1-3's to G1-5's, unchanged).

DEVIATION FROM RECORD: the window closes EARLY under -StopWhenComplete, where E1 registered "--no-stop-when-complete --run-secs 2700"; the 2700 s cap is kept (G1-3's to G1-5's, unchanged).

DEVIATION FROM RECORD: the pre-warm is a whole runner launch that also pushes the order and runs 120 s, where DEMO_RUNBOOK sec 0.4 says "let it reach the initialization, then stop it"; there is no init-only runner mode (G1-3's to G1-5's, unchanged; it is the W gate).

DEVIATION FROM RECORD: the design's G1 outline ("Order: cut A reduced to T13 -> T14 on T14's ORIGINAL line") is replaced by the ruled cut-A order at the seat's direction (G1-3's to G1-5's, unchanged).

## 2. What the code emits

G1-5's sec 2, unchanged (the same build).

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run)

G1-5's sequence and command lines with <L6>'s scripts (g1_6_golive_checks.ps1, g1_6_c3_push.sh, g1_6_runner.sh
prewarm-dryrun | prewarm | scored-dryrun | scored - G1-5's command line byte for byte, the log names aside,
g1_6_claim_holder.ps1, g1_6_rti_federates.py <rtiexec pid>, g1_6_score.py). THE SEAT RUNS every step (RL-20260928-01).
The W gate: `<python> <L6>/g1_6_score.py <prewarm run dir> --wgate` prints "W GATE: PASS" - G1-5's limbs with P-FALS (e)
under the culvert rule; then `--bridge-rule ways` and `--bridge-rule chord` RECORDED. The post-W inventory:
golive_checks -Phase prelaunch ... -AllowStage2hPid <the pre-warm's Stage 2h holder pid, from the runner log>.
TWO BRANCHES, decided at W from the live process list:
- (a) HOLDER 67984 (appNo 5281) STILL UP with >= 60 min of its hold left at W AND at E (golive_checks -MinHoldLeftMin 60;
  the hold ends about 04:22:51Z 2026-10-05): NO step R, D' or S beyond the checks - golive_checks -Phase prelaunch
  -RtiexecPid 65540 -HolderPid 67984 -MarkerWant 5296, then C3, W, E, F as G1-5. The runner finds 67984 PERSISTENT [A: G1-5
  Result THE NUMBERS]. THE LATEST E START: 03:22:51Z (the 60-min check governs); its window then ends by about 04:11:51Z
  (launch to order about 4 min + the 2700 s cap), 11 min inside the hold. The arithmetic bound alone would be 03:33:51Z.
  Past 03:22:51Z at E = no E under (a): STOP and take branch (b') after the hold ends, numbers from the marker as it reads.
- (b') 67984 HAS RESIGNED (or the machine rebooted): R (golive_checks -Phase prertiexec; StartRtiExec52.ps1 ensure-up - it
  finds a running rtiexec and starts nothing if 65540 is still up), D' (g1_6_claim_holder.ps1 -From 5296; the holder
  `scripts/StartFederationHolder52.ps1 -AppNumbers 5296,5297,5298,5299 -SettleSecs 28800`; then g1_6_rti_federates.py
  EXPECTS exactly one joined federate, the new holder), S (`docker start c2sim-server-vrf` if it is not up; REST 200), then
  A, C3, W, E, F as G1-5.
QUIET PERIOD, never touched (rtiexec, rtiForwarder, any rtiAssistant, any RtiProbe holder), the c2sim-server-vrf container
left running: G1-5's.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES: G1-5's, with g1_6_score.py (--bridge-rule culvert by default). The rows are G1-5's, carried; the W gate and the
scored run judge them as G1-5 sec 3 W / sec 4 say.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| SL | SL0-SL4 as G1-5 (the same 4 slots moved - 48_IBCT.INF1RIF2, INF1RIF3, INF1WPN1, INF3WPN1 - landings within 1 m; 19 clear; 0 UNVERIFIED / KEPT), in the pre-warm and the scored run | HIGH | any limb = STOP (G1-5's S12) | |
| H1-H4 | G1-5's (0 "route does not exist"; 0 EXECUTOR REFUSED; every echo exact; identity by uuid) | HIGH | G1-5's | |
| H5 | EVERY TASKED MEMBER MOVES at every vertex it is sent to, all 23, in the W gate and the scored run (RL-20261004-01) | HIGH | G1-5's | |
| P-FALS(e) | NO RIVER CROSSED OFF A BRIDGE by the CULVERT rule (sec 1(z)) and no fix deeper than 25 m inside OSM water; T14 expected to cross at the Wiersnianka culvert ON THE ROAD again, T02 at bridge 218414262 | HIGH | a crossing OFF by the culvert rule after which the member drove on 50 m or more, or a fix > 25 m inside water = STOP; a crossing only the ways or chord rule calls off is RECORDED | |
| H6 | T14 AND T10 ARRIVE in the scored run (G1-3's / G1-4's arrival measure). T14's vertex 2 was NOT reached in G1-5's 120 s pre-warm (Result N3), so this is the FIRST LIVE TEST of T14's arrival under Auto | MEDIUM | either does not arrive inside the 2700 s cap | |
| V | V-CRASH: no back-end crash before the window closes; a crash VOIDS the window | MEDIUM | a crash (VOID, not a pass or a fail) | |
| W-MOVE, W-DONE | G1-5's (W gate only) | HIGH | G1-5's | |
| WATCH, MM | G1-5's WATCH (INF3WPN1, INF1WPN1) and MOVED MEMBER records | RECORDED | - | |
| SPEED | the speed ratio r (chord speed / mean of its neighbours) on each T14 crossing chord and on T02's bridge chord, by lane JS's method (scratch 7207877b...\laneJS\js_dip.py; G1-5 Result ADDENDUM NEXT (3c): culvert median 0.75, T02 bridge 1.20) | RECORDED, never gating | - | |

ONE VARIABLE: none in the system - the build, order, fixture, init, composition, settings, command line and sequence are
G1-5's. The ONE change is the scorer's culvert clause (sec 1(z)), which changes how a crossing is JUDGED, not what the run
does; plus the merged harness fixes (StartRtiExec52's log finder; the federate parser; the post-W inventory). The CONTROL is
G1-5's pre-warm (20261004T202533Z_run).
STOP RULES: G1-5's. A VOID or STOPPED run is re-registered as IRONSTORM_AGG_G1-<date>-7 with new numbers. CULVERT_TOL_M, like
G1-5's constants, is not changed after the run.

## 4a. Differences named, not intended

- THE FEDERATION: under (a) the run joins the federation holder 67984 created for G1-5 on rtiexec 65540 (up since 20:22:01Z);
  under (b') a new holder on the same or a fresh rtiexec. Either is recorded.
- THE CONTAINER c2sim-server-vrf is up since G1-5's step S [V: docker ps]; C3 resets it as before.
- THE SCORED RUN'S CONTROL stays G1-2's scored run (the Literal path): G1-5 never reached E.

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5296 ***` (docs/OPUS_EXECUTION_PLAN.md :3648 in the main checkout, read
2026-10-04T21:48Z [V]). NOTHING is claimed by this registration. G1-5's block layout M..M+10 (marker -> M+11). From the marker
read at the go-live (5296 unless another run moved it; every number shifts with it and the shift is recorded):
- BRANCH (a): no holder step; PRE-WARM 5296-5306 (marker -> 5307); SCORED 5307-5317 (marker -> 5318).
- BRANCH (b'): holder claim 5296-5299 by g1_6_claim_holder.ps1 before it joins (marker -> 5300); PRE-WARM 5300-5310 (-> 5311);
  SCORED 5311-5321 (-> 5322).
A launch that aborts burns its whole block. Never reuse a number.

## 6. Harvest

G1-5's sec 6 with g1_6_score.py (the culvert rule; `--bridge-rule ways` and `chord` RECORDED). Results go to the Result block
below, PLAN_MOVEMENT row G1-6, RUNBOOK sec 11o, HANDOFF_SEAT sec 2 and Appendix B (the seat's). ASCII + CRLF.

## 7. What this run does NOT claim

- G1-5's list, unchanged.
- THE CULVERT ITSELF (deferred, RL-20261004-04): the clause judges fixes against ways; it does not say the sim drove the road
  over the culvert (the path is not printed), nor how the sim resolves a road over a river line (Adding Content sec 7.8 is
  silent), nor what causes the speed dip at C (UNEXPLAINED, G1-5 Result ADDENDUM NEXT (3c)).

## 8. Open points for the seat

1. THE SAME-RIVER LINK in (a) is lane G1-6's narrowing beyond the three-part brief (it reuses BRIDGE_RIVER_TOL_M 5 m, no new
   constant; 0.0 m on both G1-5 ways). Its residual: a culvert way that joins the crossed river more than 5 m from its line
   (a river split oddly across OSM ids) makes the rule FIRE falsely - read by hand if it occurs. The seat keeps or drops it.
2. CULVERT_TOL_M 15 m (sec 1(z)): the seat approves or alters it before the go-live; after the run it is a re-score.
3. THE REHEARSAL is headless (--no-gui, consoles 4, cut A on IronStorm_Centre_52_Aggregate): it rehearses DEMO_RUNBOOK Way A's
   wrapper and teardown, not the GUI (--gui, --vrf-appdata-dir) or the demo's console settings - a GUI rehearsal is separate.
4. BRANCH CHOICE AT W: under (a) a W that passes too late for E by 03:22:51Z leaves the pre-warm's block spent and E under
   (b') later, with the shift recorded; the seat may prefer (b') from the start if W cannot begin by about 03:00Z.

FABLE COLD REVIEW 2026-10-04 ~22:10Z (RL-20260928-04, sized per RL-20261004-04): GO - the one-scorer-change claim, the clause,
the fail-first set (i)-(iv), the numbers and the hold arithmetic verified; two non-blocking notes: the banks limb is never the sole
falsifier in a culvert control (correct by inspection); a chord within 15 m of the road x culvert point that cuts the same river
elsewhere would read ON - RECORDED, no new gate. The index holds only two river-class culverts under drivable roads (1467512812
and canal 265181802, far from the order).
SEAT DECISIONS 2026-10-04, before any go-live step: the same-river condition is KEPT (fails safe); CULVERT_TOL_M 15 is APPROVED
and frozen; the GUI rehearsal is separate (this run is headless); branch (b') is taken from the start if W cannot begin by 03:00Z.

## Result (written after the harvest, never from a live read)

RESULT 2026-10-04: PASS. The pre-warm's W GATE PASSED (culvert rule) and in the scored run EVERY REGISTERED HIGH ROW HELD;
H6 (MEDIUM) HELD - T14 and T10 arrived, T14 for the first time under Auto; V (MEDIUM) HELD - no crash. Scored run
20261004T223015Z_run: g1_6_score 79 PASS, 0 FAIL, 18 RECORDED; 5 of 5 tasks TASKCMPLT, the last 168.4 s after the order
reached the bus. The P-FALS (e) pass rests on the registered culvert clause: G1-5's ways rule FAILS the same runs (RECORDED,
below). Marks: [V] = checked by this harvest lane (read-only: both run directories and manifests, both runner logs, the
rtiexec log's join/resign lines, the seat's go-live artefacts in scratch laneG1-6, the scorer re-run); [A] = the seat's
record or an inference. Vendor logs: names, sizes, counts only. This lane's files: scratch 7207877b...\harvestG1-6
(h_rescore.py, h_full_culvert.txt, h_full_ways.txt, h_full_chord.txt, h_wgate_repro.txt).
THE SCORER [V]: g1_6_score.py sha256 de7bd755..., written 21:53Z, before the registration commits 6ed351e (22:01:25Z) and
d1fc69e (22:06:29Z); this lane's re-run reproduces the seat's W gate file (164 lines) and full score (368 lines) line for line,
the header aside. It prints no speed ratio.

THE SEQUENCE AS RUN (the seat, RL-20260928-01; BRANCH (a) - holder 67984 up throughout) [V unless marked]:
- A 22:13:33Z: prelaunch 0 FAILED (hashes and settings as registered, src diff empty, scripts diff exactly
  StartRtiExec52.ps1, cache 520 / 27c2117e, rtiexec 65540 + forwarder 58844, holder 67984 369 min left, marker 5296, REST 200).
- C3 22:13:52-22:14:25Z: PushInit exit 0 ("QUERYINIT : 40 Units"), PushOrder exit 0, one "ORDER (69670 chars)" echo.
- W: cache listed 22:14:33Z; dry run exit 0; pre-warm launched 22:14:42Z (runner log RunScenario-...-g1-6-prewarm-
  20261004T221442Z.log): 67984 PERSISTENT (:88), marker 5296 -> 5307 before any join (:130), Stage 2r started nothing (:147),
  Stage 2h holder 52056 on 5303 joined in 3 s (:161), back end 49644 on 5296 (:185), app 45264 on 5300, oracle gate 72 POS
  lines / 36 uuids (:251), order on the bus 22:18:24.922Z (:259), window to its 120 s cap with T14 (1075b583...) open (:271),
  app exit 0 (:280), StopVrf EXIT 6 (:289); runner exit 0, 22:23:40Z. W gate written 22:23:51Z: "W GATE: PASS"; --bridge-rule
  ways and chord RECORDED 22:24:04-06Z, both "W GATE: FAIL (branch A; P-FALS(e))", as registered. Post-W inventory 22:24:08Z
  (-AllowStage2hPid 52056) 0 FAILED, marker 5307.
- E: the seat waited for 52056 to leave its 900 s hold [A: resigned 22:30:02Z]; the rtiexec log has its NormalResign (L47511)
  before the next join, and E's prelaunch at 22:30:08Z lists only 65540, 58844 and 67984 - 0 FAILED, 353 min left, marker 5307.
  Scored launch 22:30:14Z (RunScenario-...-g1-6-20261004T223014Z.log): 67984 PERSISTENT (:88), 5307 -> 5318 (:130), Stage 2h
  24776 on 5314 (:161), back end 52080 on 5307 (:185), app 40880 on 5311 (:226), oracle gate 72 / 36 (:250), order on the bus
  22:33:48.019Z (:258); the window closed EARLY under -StopWhenComplete at 240.3 s of 2700 s (:275-276); app exit 0 (:284);
  StopVrf EXIT 6 (:293); runner exit 0, 22:41:01Z.
- F 22:41:18Z: inventory 0 FAILED (24776 allowed; 67984 342 min left), marker 5318; scored 22:41:27Z.
- THE SIM CACHE: 21,807 files, 495,599,180 B at all three listings (22:14:33Z, 22:24:08Z, 22:41:21Z) - unchanged.
THE CLOCKS (scored) [V]: trace t0 = WatchVrf join 22:33:13.026Z; T10 dispatched t=70.7 (SIM 386.9), T14 t=74.0 (SIM 422.9),
T02 t=74.9 (SIM 423.9); last fix t=335.8; SIM/WALL about 7.85. TASKCMPLT (reports-captured.log): T01 and T13 22:34:26.46Z,
T10 22:35:26.45Z, T02 22:35:41.01Z, T14 22:36:36.43Z.

THE ROWS [V: laneG1-6\W_gate_20261004T221443Z_run.txt (W) and score_20261004T223015Z_run.txt (E), both reproduced]:
- SL (HIGH) HELD, W and E: SL0 L32 exact, once; SL1 23 slot lines, exactly 4 SLOT MOVED - 48_IBCT.INF1RIF2, INF1RIF3,
  INF1WPN1, INF3WPN1; SL2 4 of 4 as predicted; SL3 19 clear within 1 m; SL4 0 UNVERIFIED, 0 KEPT ON BAD GROUND.
- H1-H4 (HIGH) HELD, W and E: H1 0 relayed "route does not exist" (R-VEND 0); H2 0 EXECUTOR REFUSED, 0 pre-M3b equivalent;
  H3 50 of 50 echoes exact; H4 I1-I17 (W) / I1-I18 (E): 59 of 59 created and bound by uuid, the trace carries 26 of 26 uuids.
- H5 (HIGH) HELD: W 33 member-vertex pairs judged, 0 not moved; E 50 (T14 vertex 2 included), 0 not moved, 0 NOT EVALUATED.
- P-FALS (e) (HIGH) HELD under the culvert rule, W and E: 0 fixes inside OSM water for all 23 members (longest run 0, deepest
  0.0 m); 18 crossings, each ON, 0 OFF. W: 17 T14 crossings of 1467512812 by the culvert clause, the chord 1.0-3.0 m from the
  road x culvert point (54.019835,23.328533); T02 crossed 8011072 20.9 m from bridge 218414262, ON by the ways limb (the path
  via the bridge 1.01 x the chord). E: 16 T14 crossings of 1467512812, chords 0.5-3.0 m (the 12 Mech COs one chord, 3.0 m);
  CAV1 crossed 197345450 (the river way that ends at the culvert, G1-5 N1 (a)) - ON through the same-river link, its chord
  6.0 m and its crossing point 7.7 m from the point, banks 1.8 / 2.6 m from a drivable road; T02 crossed 8011072 30.8 m from
  bridge 218414262, ON by the ways limb (1.05 x the 151.5 m chord), drove on 2,988 m; T14 members drove on 2,284-2,297 m.
- H6 (MEDIUM) HELD - T14 AND T10 ARRIVED (E). T14: vertex 1 closed with the unit 6 m from it, vertex 2 4 m (R12); all 17
  members within 50 m of vertex 2 by t=217.6; OVERDUE at its 300 s Duration (L57855, 315 SIM s), then ARRIVAL EVIDENCE
  (L114191: nearest member 410 m, inside the 500 m bar; 129.4 WALL s = 950.4 SIM s after dispatch) and "was OVERDUE and the
  unit has now ARRIVED" (L114193); final fix 2.1 m (container), members 0.8-1.7 m, CAV1 10.3 m. This is the first T14
  arrival under Auto (sec 4 H6: "the FIRST LIVE TEST"). T10: ARRIVAL EVIDENCE L67795, 62.2 WALL s = 471.5 SIM s; vertices
  closed 10 / 16 / 16 m; final 1.7 m (members 1.6-1.8 m). T02 (recorded with them): OVERDUE (313 SIM s), ARRIVED L77449-77451,
  73.0 WALL s = 508.2 SIM s; final 0.9 m.
- V (MEDIUM) HELD: V-CRASH no signal for back end 49644 (W) or 52080 (E); 0 "BACK END CRASHED" in either app log; C:\MAK\logs
  holds their .log files (14,321,228 B and 19,525,643 B) and no .callstack.log or .dmp for either pid.
- W-MOVE, W-DONE (HIGH, W only) HELD: W-MOVE PASS (T10 5 of 5 > 50 m; T14 17 of 17 and T02 1 of 1 RECORDED); W-DONE PASS -
  closed T02, T10, T14 (vertex 1), nothing reached but not closed; T14 vertex 2 issued, no member within 50 m of it at the cap
  (its O1 line; NOT REACHED, recorded, as G1-5 N3).
- WATCH (RECORDED): W - INF1WPN1 1,054.9 m, within 50 m of vertex 1 at t=125.9, vertex 2 NONE; INF3WPN1 1,701.0 m, t=158.5,
  NONE. E - INF1WPN1 2,490.3 m, t=101.0 / 217.6; INF3WPN1 2,279.3 m, t=133.6 / 217.6.
- MM (RECORDED; first-leg bearing, first-300-m water minimum, "Movement constrained" count; W / E; G1-5 in brackets):
  INF1RIF2 311 / 297 deg, 15.8 m, 0 [310, 15.8]; INF1RIF3 166 / 173 deg, 14.5 m, 0 [174, 14.5]; INF1WPN1 276 / 274 deg,
  11.4 m, 0 [294, 11.3]; INF3WPN1 89 / 89 deg, 9.4 / 9.3 m (lake 16373225), 0 [89, 9.2].
- SPEED (RECORDED, never gating): NOT MEASURED - the scorer prints no ratio; lane JS's js_dip.py not run (RL-20261004-04).
- THE OTHER RULES (RECORDED, sec 6) [V: this lane's re-runs on E]: --bridge-rule ways - P-FALS (e) FAIL, the 17 T14 crossings
  OFF, T02 ON; --bridge-rule chord - FAIL, all 18 OFF, T02 included (30.8 m > 15 m; G1-5's T02 read ON at 13.3 m). Every other
  row is identical under the three rules (79 PASS, 0 FAIL). The pre-warm's ways / chord files show the same pattern.
- CARRIED, RECORDED (E): O8 did not recur (36 of 36 terrain-query altitudes, 0 FALLBACK, L293); P19 79 cache HITs, 0 HTTP
  FETCH (L190199); 0 "Movement constrained" relayed (R18); 18 raw "No creator found" lines (benign, as G1-5).

THE NUMBERS [V: both run-manifest.json appNumbers and ledger; the runner logs :92-105, :130, :161, :185, :226; the pre-check
and Stage 2c pids by the rtiexec log's join order [A]]:
- PRE-WARM 5296-5306 (marker 5296 -> 5307): 5296 (back end 49644, force-stopped by identity, StopVrf exit 6, no crash), 5298
  (WatchVrf pre-check 40232 [A]), 5299 (trace 59104), 5300 (app 45264, exit 0), 5301 (Stage 2c 54216 [A], exit 0), 5303
  (Stage 2h holder 52056, joined in 3 s, NormalResign at the end of its hold) CONSUMED; 5297 (--no-gui), 5302 (the oracle gate
  passed), 5304-5306 BURNED.
- SCORED 5307-5317 (marker 5307 -> 5318): 5307 (back end 52080, StopVrf exit 6, no crash), 5309 (pre-check 55080 [A]), 5310
  (trace 44608), 5311 (app 40880, exit 0), 5312 (Stage 2c 25940 [A], exit 0), 5314 (Stage 2h holder 24776, joined in 3 s,
  NormalResign L67990) CONSUMED; 5308, 5313, 5315-5317 BURNED.
- Holder 67984 (5281, G1-5's claim) never touched; it holds to about 04:22:51Z 2026-10-05. MARKER 5318.
APPENDIX B ANNOTATION, for the seat (this lane does not edit OPUS_EXECUTION_PLAN.md) - below the 5296-5306 block:
  "- RESULT 2026-10-04 (G1-6 PRE-WARM 20261004T221443Z_run, IRONSTORM_AGG_G1-2026-10-04-6; from the harvest lane): holder
  branch (a), 67984 (5281) PERSISTENT, nothing claimed for it; 5296 (back end 49644, StopVrf exit 6, no crash), 5298
  (pre-check 40232 [A]), 5299 (trace 59104), 5300 (app 45264, exit 0), 5301 (Stage 2c 54216 [A]), 5303 (Stage 2h holder
  52056) CONSUMED; 5297, 5302, 5304-5306 BURNED. W GATE PASS (culvert rule). Marker 5296 -> 5307."
  and below the 5307-5317 block:
  "- RESULT 2026-10-04 (G1-6 SCORED 20261004T223015Z_run; from the harvest lane): 5307 (back end 52080, StopVrf exit 6, no
  crash), 5309 (pre-check 55080 [A]), 5310 (trace 44608), 5311 (app 40880, exit 0), 5312 (Stage 2c 25940 [A]), 5314 (Stage 2h
  holder 24776) CONSUMED; 5308, 5313, 5315-5317 BURNED. 79 PASS / 0 FAIL, 5 of 5 TASKCMPLT. Marker 5307 -> 5318. Claims
  nothing."

WHAT IT MEANS (implication, not measurement): on cut A under the M3b planner, the five-task order ran to completion once:
every tasked member moved, T14 drove both legs and arrived, and every crossing read on a road or a bridge under the registered
clause; the headless runner path (pre-warm + scored, persistent holder) ran end to end with no crash. That is enough to show
cut A as work in progress (RL-20261004-04); it does not rehearse Way B (STP pushes its own order, RL-20261004-05), the GUI, or
the full order. The P-FALS (e) pass judges fixes against OSM ways; it adds nothing on how the sim resolves a road over a
river line (deferred, sec 7).

NEXT: (1) Fable's cold review of this Result, sized per RL-20261004-04 (RL-20260928-04). (2) The seat's doc sync (sec 6):
Appendix B as above, PLAN_MOVEMENT row G1-6, RUNBOOK sec 11o, HANDOFF_SEAT sec 2. (3) THE DEMO TRACK (RL-20261004-04/-05):
Way B - STP's own order (STP-848 durations; leniency is with the owner); the Label decoration by an automated, verifiable
deploy step (LBL hardened + new pin); the tier-1 full order (c3b2bb5) on the aggregate profile; the DEMO_RUNBOOK rewrite and
a GUI rehearsal (sec 8 item 3). Deferred, not dropped: the culvert mechanism, the speed dip, N1's cause.

ADVERSARIAL REVIEW: (1) n = 1 - one pre-warm, one scored run; H6 held once; the crash record on this path is now 1 in 5
runs (G1-3's), so V's MEDIUM stays MEDIUM. (2) THE CULVERT CLAUSE WAS FITTED ON THIS CROSSING: CULVERT_TOL_M 15 came from
G1-5's chords at the same culvert (1.0-9.2 m), and G1-6's (0.5-6.0 m) fall inside that range, so the P-FALS (e) pass is not
an independent test of the clause - it re-met the crossing it was built for. What protects the verdict: the clause was
registered and frozen before the run, its fail-first set (i)-(iv) fires on the 44.3 m dirty control, and the frozen ways rule
is printed beside it (FAIL). The on-the-road reading itself stays [A] (the path is not printed). (3) CAV1's ON rests on the
same-river link (lane G1-6's narrowing; 197345450 meets the culvert way at C), the case it was built for; by hand its banks
are on the network (1.8 / 2.6 m) and its chord 6.0 m from the point. (4) T02's ON now rests on the ways limb's path-via-bridge
test (crossing point 30.8 m from the bridge, outside the 15 m acceptance that carried it in G1-5); the chord rule calls it
OFF. (5) StopVrf exit 6 on both runs: the graceful close was refused and the run's own back end force-stopped by pid and
start time (windows "NVOGLDC invisible" and "Default IME" only, no modal). The registered gate accepts 0 or 6; it comes after
the window, so it does not bear on V; the rtiexec log shows each back end's LostConnectionResign and the next joins
succeeded (no stale federate seen). It is a teardown risk for the demo, unexplained, as G1-4 / G1-5. (6) TASKCMPLT is sent
on ARRIVAL EVIDENCE (T14: nearest member 410 m), before the unit is at the point, and T14 / T02 went OVERDUE first; H6 is read
on the trace finals (0.9-2.1 m), not on report times. (7) Assumed, not verified: 52056's resign second (22:30:02Z, the seat);
the pre-check and Stage 2c pids (join order); that no step beyond the artefacts read occurred between W and F (no other
runner log in runs\launch52 in that interval [V: listing]).
