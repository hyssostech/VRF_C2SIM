# PREREG - IRON STORM ON THE AGGREGATE PROFILE, RUN G1-6: G1-5 re-run with ONE scorer change (a narrow culvert clause) - the first SCORED aggregate cut-A run with arrivals, doubling as a rehearsal of the demo's runner path (one unscored pre-warm + one scored run)

STATUS: REGISTERED 2026-10-04, LAUNCH PENDING - the seat's go-live under RL-20260928-01; Fable cold review owed
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

(empty - nothing has been run under this registration)
