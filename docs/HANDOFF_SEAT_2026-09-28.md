# SEAT HANDOFF - Fable seat (session 5fc25950) to the Opus 5.5 seat, 2026-09-28 (RL-20260928-04)

Read in this order, then act: this file; the CLOSED block at the top of docs/PLAN_MOVEMENT_2026-09-27.md; docs/RULINGS.md
(RL-20260928-01..-04 are today's); docs/experiments/PREREG_IRONSTORM_AGG_G1-3_2026-09-28.md Result; CLAUDE.md sec 3 and 5.
The rite (global CLAUDE.md): PREREG before any launch; falsification gate on every cause claim; the ledger and the
corrections log are the record; ASCII + CRLF (edit with perl/Edit/python with newline="", never sed -i); a suite run
(`"C:\Program Files\PowerShell\7\pwsh.exe" -NonInteractive -NoProfile -File tests/RunnerTurnaround.Tests.ps1 < /dev/null`,
770 passed / 0 failed / 1 skipped on main today) before every push; push only when green.

## 1. The seat model rule (RL-20260928-04)

The seat runs on Opus 5.5. Before any cause claim or design brief reaches the owner, call Agent(model: "fable") as a
COLD-START REVIEWER on the evidence only (the run's app log lines, the vendor citations, the brief - not this session).
Use Fable for the judgment-heavy lanes (vendor-mechanism research, a crash reading if the owner authorises it). Measure the
seat by the owner's corrections per day (2026-09-28: six - all seat mechanism claims made without the record or the docs).
THE OPS TIER (under RL-20260928-04; the follow-up question of 2026-09-28 ~19:58Z, "Is even Opus overkill for the seat?"): the seat keeps the decisions - briefs, the adversarial
reading of reports, merge order and conflicts, the owner's rulings - and hands the MECHANICAL sequences to a cheap ops agent
(Agent, model haiku or sonnet) driven by the scripted procedures that exist: merge -> suite -> push (push only on 0 failed);
the go-live A/C3/D/W/E/F command lines; the ASCII/CRLF/13c checks; the sim-cache listings; RUNBOOK sec 9 rebuild lines. One-line
task in, one-line result out, so the seat's own context stays small - the seat's cost is its context re-read, not its price.
A Sonnet seat is untested here; if the owner wants that data, take it on a docs-only day, never on a go-live day.

## 2. State at the handoff (2026-09-28 ~20:05Z)

- main: see `git log -1`; every commit of today is pushed; the tree is clean except the runner-written ledger (below).
- Deployed build: 1.0.0+git.fde3ff2.Release-5.2 in the eleven output trees (RUNBOOK sec 9, lane R4 17:30Z); bridge pin
  03226dd0 (NEW PIN 13:34Z, C1d); the road layer staged: 41 osm-highways tiles in the deployed preflight-cache (manifest
  213bae20...). The M3b lane (below) changes managed code only - a managed rebuild of the ten consumers redeploys it
  (RUNBOOK sec 9 R2/R4 procedure); tools/RtiProbe's tree is held open by the live holder and its code is unchanged.
- 2026-10-04: G1-4 Result merged (91d84ba), ruling RL-20261004-01. Holder 56380 (appNo 5255) GONE since the owner's reboot
  2026-09-28 23:16:45Z; the RTI trio is down (Stage 2r starts a fresh rtiexec). NEVER kill rtiexec/rtiForwarder/rtiAssistant.
- Ledger marker (docs/OPUS_EXECUTION_PLAN.md Appendix B): `*** NEXT FREE: 5281 ***` (the G1-4 pre-warm took 5270-5280; its
  scored block was never claimed). G1-5, branch (b'): holder claim 5281-5284 (marker -> 5285), pre-warm 5285-5295 (-> 5296),
  scored 5296-5306 (-> 5307).
- M3b MERGED as 739cce2 (branch fix/planned-move-destination 5ae2193; managed only, pin 03226dd0 stands): (1) member DISPLAY names
  at most 16 characters (VrfNames.cs, ContainerComposition.cs) so the vendor script's route reference "<member> Path part N_<counter>"
  fits the 35-byte DtUUID payload (uuid.h :247-249; the 2026-09-02 cut, PREREG_ROUTE_NAME_LENGTH; the vendor's own saves name a
  script route's uuid "<name>_<counter>"); (2) a vacuous planned intermediate vertex = EXECUTOR REFUSED -> TASKABRT (VertexChain,
  AggregateMovePlanner; the last vertex still goes to D-6; M1 lone platforms unchanged); (3) a crashed back end VOIDS the window
  (RunnerLib/RunC2SimScenario: crash record by name+mtime, the modal, the process gone, WatchVrf backends 1->0; StopVrf52 exit 8;
  the watchdog reads 8). Tests: planned-move 81, populate 199, rulings 615, RunnerTurnaround 777/0/2 in the lane's worktree.
  DEPLOYED 2026-09-28 21:20Z (Opus seat): ten consumers at 1.0.0+git.b0bad53.Release-5.2, all eleven trees at pin 03226dd0,
  appsettings redeployed, 779/0/1 (RUNBOOK sec 9, "M3b DEPLOY" line); RtiProbe not rebuilt (held; code unchanged; optional).
  Unexplained, carried: the back-end crash (callstack unread; the owner's call); vertex 2's "Cound not create route" for two
  members. Next hypothesis if short names still fail in G1-4: buffer 10 (the vendor's save used 0) - did not arise: G1-4
  carried 35 of 35 references whole with 0 refusals (n = 1).
- The STP session (stp-live-picture-d8, STP-850) holds a PR #1 (fix/stp-850-start-at-max) on this repo: the task-start
  anchor at receipt. Review it here with the record rules; the DEFAULT anchor (Receipt) is the owner's to confirm.

## 3. Next steps, in order

1. (DONE by the Fable seat) M3b merged as 739cce2, suite green, pushed.
2. (DONE 21:20Z by the Opus seat, RUNBOOK sec 9 "M3b DEPLOY") Deploy: a managed rebuild of the ten consumers from main 739cce2 (RUNBOOK sec 9, the R4 line is the model; an ops
   agent can run it); verify by the output trees; record the sec 9 line; RtiProbe stays as is while the holder runs.
3-4. DONE 2026-09-28 by the Opus seat: G1-4 registered (7ac84c2, Fable GO WITH FIXES applied), owner "Go on go-live";
   pre-warm run 20260928T225800Z_run (block 5270-5280, marker 5281, 17b0859): W GATE FAIL (H5, P-FALS(e)) - STOP before E,
   scored block never claimed. M3b's question answered at n = 1: H1 0 "route does not exist" (G1-3: 15), R-REF 35/35 whole,
   H2/H3/M10/M12 PASS, T10 and T02 vertex 1 CLOSED, no crash. Misses, UNEXPLAINED (no cause claimed): T14 48_IBCT.INF3WPN1
   0.0 m (tasked, planned, move-along on the whole ref, no refusal); T02 28ID.HQ1 crossed river 8011072 16 m from a road
   bridge. Seat's own registration flaw: W-MOVE ungated T14 members but H5 still gated them in W. One new sim-cache URI
   entry (23:02:31Z). Evidence: scratchpad H:\claude\F--Repos-C2SIM-c2simVRFinterfacev2-36\b25cd950-e1a5-4e5b-97e2-4b1267e629d2\
   scratchpad\laneG1-4\ (W_gate_20260928T225800Z_run.txt). NEXT: the Result by a read-only harvest lane -> Fable cold ->
   owner; then G1-5; the Label follow-up (5) is unblocked. Holder 56380 ends at the owner's reboot: next launch claims a
   holder from 5281 (branch b').
   RESULT HARVESTED (Fable lane, 2026-09-28 ~23:55Z): LOCAL branch result/g1-4 at 1184663 (worktree .claude\worktrees\
   result-g1-4), NOT merged, NOT yet cold-reviewed. Its readings, each [A] until reviewed: INF3WPN1 held by an impassable
   feature at its nudged birth point [WEAKENED 2026-10-04: G1-2 moved it from the same point twice - G1-4 Result ADDENDUM N1 (e)] (1.7 m outside OSM lake 16373225; "Movement constrained by features" L27577; which
   feature unsettled - siblings 2.3-7.5 m from another lake moved); T02 HQ1 crossed ON wooden bridge OSM 218414262 (the 16 m
   is the fix-chord corner cut vs scorer BRIDGE_TOL_M 15). Owner stopped the session here (~23:58Z). NEXT, in order: Fable
   cold review of the Result -> owner (RULE: does the W FAIL bind as scored, given N3 = the H5/T14 registration flaw);
   merge; then G1-5 per the Result's NEXT. Numbers after the reboot (branch b'): holder 5281-5284, pre-warm 5285-5295,
   scored 5296-5306, marker -> 5307. RTI trio down since the reboot (23:16:45Z).
   COLD-REVIEWED 2026-10-04 (Fable, evidence only; text: scratchpad H:\claude\F--Repos-C2SIM-c2simVRFinterfacev2-36\
   7207877b-f104-4515-91f7-c662e3fce85c\scratchpad\fable_g14\REVIEW_G1-4_RESULT_fable_2026-10-04.txt): ACCEPT WITH FIXES -
   every reproducible number held; fixes: N2 "on the bridge" -> "consistent with every fix" (no fix is on it); N3 is not a
   contradiction (W-MOVE >50 m and H5 >1 m are different limbs; as written H5 governs); N1 (e) to [A] plus the unweighed
   competitor (the planner's MAK_OBSTACLE 10 m buffer vs a 1.7 m birth); "the executor took every route" -> 22 of 23.
   Independent of N3 the gate still FAILS on P-FALS(e) (16.24 m > 15) under the registered rule. RULED RL-20261004-01
   ("As recommended"): the W FAIL binds as scored; G1-5 gates H5 on every member after a build giving nudged member slots
   a water clearance >= the 10 m buffer; the scorer's bridge rule on the ways, fail-first on G1-4's pre-warm. Fixes +
   ruling applied on result/g1-4 (bc1127c) and merged. NEXT: lane W1 (water clearance, code + tests) -> Fable cold on its
   design -> merge/deploy (RUNBOOK sec 9) -> G1-5 registration; lane D1 (the Result's sec 6 doc updates) in parallel.
   D1 MERGED 0000f8c, pushed 4dbb94c (779/0/1). W1 = fix/slot-water-clearance e4badc8 (test-first, 7 FAIL) + 03a350d (fix:
   Vrf:PreflightSlotWaterClearanceMeters 10; slots IN or WITHIN 10 m of water nudged; no bridge exemption for slots; vertex
   path untouched). Fable cold: GO WITH FIXES (text only: EntityLevel prints the effective clearance; sec 4 comment; pin the
   10.0 m boundary) - applied 377ce98. The band and the no-bridge rule for slots (0 effect on cut A) KEPT by RL-20261004-02.
   W1 MERGED b0c3bb6 and pushed - the seat pushed it on a RED suite (778/1/1: 13c, this paragraph's un-id'd "owner
   2026-10-04"; push chained in one command with the suite read - never again); fixed by the next commit. G1-5 must predict (Fable harness, scratchpad 7207877b...\fable_w1\REVIEW_W1_fable_
   2026-10-04.txt): 0 newly moved; the same 4 48_IBCT slots move - INF1RIF2 150 m NW 15.5 m, INF1RIF3 125 m SW 13.6 m,
   INF1WPN1 25 m SW 10.9 m (thinnest), INF3WPN1 75 m S 13.6 m. The scorer's bridge rule is the G1-5 lane's, not W1's.
   W1 DEPLOYED d455092 (1.0.0+git.52f50e0, pin 03226dd0). G1-5 REGISTERED (IRONSTORM_AGG_G1-2026-10-04-5; Fable GO WITH FIXES,
   applied) and MERGED 1fdc820; GO-LIVE APPROVED ("Go live is approved", session 7207877b). Watch INF3WPN1 (13.7 m, lake
   between it and the road; effective margin 3.7 m). Machine booted 4x since G1-4 (09-28 23:16Z, 09-30 x3 incl. Windows
   Update KB5121794); c2sim-server-vrf container Exited - `docker start`, not RUNBOOK :1716's `docker run`. Numbers: holder
   5281-5284, pre-warm 5285-5295, scored 5296-5306, marker -> 5307; step R starts rtiexec before the holder.
   G1-5 GO-LIVE 2026-10-04: R rtiexec 65540 (+fwd 58844); D' holder RtiProbe 67984 on 5281 (to 04:22Z 10-05; NEVER kill);
   S docker start, REST 200; A 0 FAILED; C3 ok; pre-warm 20261004T202533Z_run (5285-5295, marker 5296, StopVrf exit 6):
   W GATE FAIL (P-FALS(e)) - STOP before E, scored block never claimed. Every other limb PASSED: SL0-SL4 (4 slots as
   predicted), H1-H5 (INF3WPN1 moved 1,701 m), W-MOVE, W-DONE, V-CRASH. The miss: all 17 T14 members crossed the
   Wiersnianka (OSM 197345450 / 1467512812) at 54.01986,23.32848 with both banks on roads; 1467512812 is tagged
   tunnel=culvert under road 300614718 [V: waytags_t14_crossing.txt] - the ways rule has no culvert clause [reading A].
   Harness defect: g1_5_rti_federates.py misses the single-form join line of a fresh rtiexec (verified 1 join by hand).
   Result harvested (Fable ACCEPT WITH FIXES, applied) and MERGED 1758db2; Appendix B annotated. RULED RL-20261004-03: the
   FAIL binds as scored; the culvert clause is G1-6's, registered before its run, and WAITS for the vendor Adding Content
   manual (FOUND 2026-10-04: public MAK_ONE_2025_Adding_Content.pdf, now in docs/vendor/mak-5.2; sec 7.8 is silent on
   culverts and on overlap priority, so the clause rests on G1-5's observation, [A]). Unexplained: a
   ~0.6x speed dip on every T14 crossing chord (Result N1 (b)). Holder 67984 up to 04:22Z 10-05; marker 5296.
   IN FLIGHT (independent of the docs): lane LBL (Label = full C2SIM designation, item 5), lane HX (harness defects +
   stale PLAN rows: D2b/AF1-3/C1b are in the deployed 52f50e0), lane JS (Result NEXT 3c junction speeds, read-only).
   DEMO TRACK (RL-20261004-04, 2026-10-04 ~21:40Z): demo readiness FIRST; the culvert, N1's cause, the speed dip, the G1-3
   stack and small-part verbs/units are DEFERRED, not dropped. HX merged 4df7749, JS recorded (G1-5 ADDENDUM, dip unexplained),
   Adding Content found (public) and indexed. In flight: G1-6 registration CUT TO THE MINIMUM (narrow culvert clause; the first
   scored aggregate cut-A run, doubling as a Way-A rehearsal); LBL (labels); DR1 (read-only survey: the FULL 23-task order on
   the aggregate profile - per-task outcome, blockers, recommended demo scope). Then: DEMO_RUNBOOK rewrite for aggregate Iron
   Storm and a rehearsal. Demo date: not in the record (asked 2026-10-04).
   G1-6 RAN 2026-10-04 (RL-20261004-05 "(1) go"): pre-warm 20261004T221443Z_run (5296-5306) W GATE PASS (culvert rule;
   ways/chord RECORDED FAIL as expected); scored 20261004T223015Z_run (5307-5317, marker 5318): 5/5 TASKCMPLT in 169 s of the
   order, T02/T10/T14 ARRIVED 0.9-2.1 m, 79 PASS / 0 FAIL on g1_6_score (scratch laneG1-6), no crash, StopVrf exit 6. The
   FIRST fully passing scored aggregate cut-A run. Result not yet harvested. Holder 67984 up to 04:22Z 10-05.
   RL-20261004-05: the demo is WAY B (STP pushes its own order: STP-848 durations + no cut-A hand fixes - leniency is the
   owner's call, asked); the Label decoration may be written under C:\MAK ONLY by an automated, verifiable deploy step on
   every install. Tier-1 full order merged c3b2bb5 (22 tasks). NEXT: LBL hardened + new pin; composition sources (public).
   STATE 2026-10-05 ~12:45Z (main 6d20422, deployed 1.0.0+git.f58de23, pin 9e8c96b5 in all eleven trees; rtiexec 65540 +
   holder RtiProbe 41292 (5329, to ~20:06Z); marker 5344; c2sim-server-vrf up). DONE since G1-6: labels (clean designator,
   RL-20261005-03) + the Label decoration deploy (vendor appData and C:/C2SIM/vrf-appdata-unattended); ISO short durations
   (RL-20261004-06); tier-2a compositions; Vrf:DemoExtent (RL-20261005-02). REHEARSAL_WAYB (raw order, 99 members) VOID: the
   back end fell minutes behind paging terrain for the far-east routes (vendor log); REHEARSAL_WAYB_BOUNDED PASSED B1-B7
   (no backlog, GUI exits unattended on the unattended appData, T14 arrived) - content thin (1 mover). Doctrine research
   (scratch laneIMPL): implied positions are mostly STP's to author; RL-20260914-02 governs. OPEN FOR THE OWNER: extended
   labels for the suffix (native + new pin); keep RL-20260914-02; file the STP geometry ask; the stray C:/C2SIMvrf-appdata-
   unattended junction cleanup (his - never recursive before the 3 junctions are rmdir'ed). Demo items: pace (time
   multiplier), refused containers on the map, label overprint, T14 stray members, StopVrf exit 6.
   (was:) G1-4 registration (a lane, from PREREG_IRONSTORM_AGG_G1-3_2026-09-28.md: same order/fixture/init; the one
   variable = M3b; HIGH: 0 "route does not exist", every member moves, T14 and T10 arrive; the crash-void line as a
   falsifier; numbers per sec 2). Fable reviews the registration's cause claims cold before the go-live.
4. Go-live (the seat, under RL-20260928-01): A -> C3 -> W (dry run, pre-warm, W gate) -> E -> F, the scripts of
   u3\laneG1-3 adapted (scratchpad H:\claude\F--Repos-C2SIM-c2simVRFinterfacev2-36\5fc25950-1a10-4ade-9a7b-68cb5c1daf05\
   scratchpad\u3\laneG1-3\); ask the STP session to hold builds first (idle MSBuild workers fail the pre-launch check;
   let them time out, never kill).
5. LABEL follow-up (asked 2026-09-28 ~20:55Z; UG52 13.2.5 / 21.2 / 21.3): our creates send label = nullString
   (VrfFacade.cpp :943, :964, :980), so the map shows only the NAME - the 30-character cut for containers, the 16-character
   display name for members. The vendor's LABEL is uncapped, not unique and never a key: put the full C2SIM designation in the
   Label on every create (a small native change - the facade hard-codes the null label; the bridge overloads take a label
   argument; NEW PIN procedure) and enable the Label symbol decoration in the fixture's display settings (21.2), so the demo
   audience sees the full designation. After G1-4's Result, not before; one lane; the fixture edit is the sanctioned deploy.
6. After that: the STP-850 PR; audit follow-ups (AUDIT_RULINGS_IN_CODE_2026-09-28 sec 3: the owner's decisions;
   the time multiplier; the D2b/M3 partials); the 8i-2 and observer test-isolation weaknesses are fixed / noted.

## 4. Decisions that are the owner's (do not decide them)

- Whether the G1-3 crash callstack (C:\MAK\logs, pid 3344, 19:05:16Z) is read (RL-20260921-02 item 4 precedent).
- The STP-850 default anchor (Receipt recommended) and the PR merge.
- The audit's open items: the stall window (240 wall / 360 sim s vs the approved 120 sim s), the platoon ring 350 m vs
  "about 300 m", RL-20260914-04 (fan-out) retire or keep, the time multiplier, RL-20260921-04's open question.
- Whether highway=track counts as a road for the AUTO planner and whether 500 m is the right proximity (observed:
  every cut-A leg is NEAR on tracks; the NONE branch is untested).

## 5. What the Fable seat got wrong today (so the next seat does not repeat it)

CORRECTIONS_LOG 2026-09-28 entries: the name limit re-derived instead of the uuid (RL-20260928-02); "roads are not a
routing option" written into the CLOSED list without the docs; "plan-then-follow along roads" asserted from a script skim;
the G1-3 binding diagnosis given to the owner before the harvest and refuted by the Result. Common cause: a mechanism
claim built on a lane's stated assumption or a file skim, not on the record, the vendor docs and the run's own evidence.
