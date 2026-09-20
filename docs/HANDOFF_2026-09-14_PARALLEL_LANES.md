# HANDOFF - parallel lanes to the demo (opened 2026-09-14)

THE CURRENT ENTRY POINT (the newest HANDOFF_*.md, by convention). Read CLAUDE.md (repo root) first,
then section 1 below, then the lane table. Supersedes HANDOFF_2026-09-01_R9_COMPLETE.md AS THE ENTRY
POINT; that file remains the 5.0.2 / R9 record and its CLOSED list still binds. ASCII + CRLF. HARD
CAP 200 LINES - when a phase closes, collapse it to a few lines plus a pointer; never drop live
guidance to make room. Deep-read only the record your task touches; re-verify load-bearing claims
against artifacts, not prose.

## 1. CLOSED - tripwires. Do NOT reopen without NEW live evidence. Each line names its record.

ORBAT / movement (docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md CLOSED list C1-C15 - canonical, read there):
- C1 the company move failure had TWO superimposed causes: C1a structure (createSubordinates on a
  unit that ALSO has declared children -> template phantoms + orphaned platoons) and C1b movement (a
  template higher-unit created remotely scatters). C1b's MECHANISM IS CLOSED by the sim's own
  console: a move-into-formation gate whose template HQ-section M998 never reaches its slot, so the
  route move never starts. Not a race, not "unresolved formation", not a vendor bug.
- C2 THE FIX IS COMPOSE (vendor sample: empty shell + create members + addToOrganization + task the
  parent), 3/3 deterministic. C3 do not retry the template path at company+. C4 AggregateFormation
  auto stays OFF. C5 a coarse leaf EXPANDS to compose (recursive; pure higher-units only).
- C6 no post-attach reorganize and no client-side formation-validity wait (both refuted as "gaps").
  C7 leader identity does not gate movement; declared order is fidelity only. C8 address VRF objects
  by their REAL VRF_UUID from ObjectCreated, never a name-as-DtUUID (settled 2026-09-02). C9 there is
  no vendor MSDL/ORBAT importer sample. C10 sendVrfObjectCreateMsg + initialFormation: DROPPED.
- C11 THE FIRST INSTRUMENT for any VRF behaviour question is the object's OWN CONSOLE at notify
  level 4. Silence at the vendor default level 1 is a configured outcome, not evidence.
- C12 LIFEFORMS CRASH the 5.2d headless sim until the DI-Guy data package is installed (user-owned,
  readiness row 17). Every infantry / mortar / CSS / fire-support row of the fidelity table hits it.
- C13 THE INIT IS THE ORBAT (context); the ORDERS define the simulated set. COA-STP1 = 11 taskees,
  not 128 units. Vrf:CreationPolicy=AtOrder is the demo default. Never size a run by the init.
- C14 co-located units are SPREAD at startup (DeStack, 700 m rings) - user ruling 2026-09-07,
  supersedes the 2026-07-13 R8 ruling (that one was 5.0.2). C15 a unit's TASKCMPLT may be reported
  from the unit's OWN ARRIVAL EVIDENCE (a MAJORITY of members within 500 m of the last vertex); the
  vendor's later completion is swallowed once.

Early stops (docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md secs 6-7d canonical; full recap archived docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 1):
- sec 6 VENDOR PASS: a vehicle that stops while its task still runs is UNDETECTED BY DESIGN in 5.2 (the base give-up test always returns false).
- sec 7 CAUSE SUPPORTED for the units that freeze ON a scored face (1-35, 4-27): seven consecutive 8 m postings at 0.70-0.95 over 55 m on sand ahead of the freeze; sampler matches sim altitude to ~3 cm.
- sec 7b SCOPE: a claim about those units only, NOT "the early stops" generally - a SECOND mechanism is active (1-6 on descending ground, 856/HHC on flat); calibration as corrected: 40 m window, 0.92 threshold, 0.097 margin, 3/3 caught, 0/6 false alarms.
- sec 7c it is the LEADER that freezes; followers crawl past it. The console excludes blockage, replan, give-up and goal change - it does NOT exclude formation speed control.
- sec 7d a THIRD stop kind: a failed move-along subtask ("Entity not embarked on same object as target") left a task-less hull PUSHED 1,994 m by the vehicle behind it; a vendor task FAILURE produces NO C2SIM report today (B1), and CHECK THE ALTITUDE RESIDUAL before reading push motion as movement.

Navigation mesh (PREREG_NAVDATA_G6_2026-09-13.md sec 5 = STOP; MESH_QUERY_VS_DISTANCE_2026-09-14.md sec 0; full recap archived docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 2):
- G6 STOPPED on its instrument gate: the 41 x 54 km area loaded, both nav-area gates passed, and vrf:findPathToLocation returned 0 points for every multi-km goal - a FOURTH reproduction of the stops, not a discriminator.
- The dependence is on the AREA, not goal distance: 10,131.6 m planned twice on the 1,600-sector MojaveAO20, 0 points on the 8,856-sector MojaveCOA for byte-identical destinations (22/22 pairs); refusals return FASTER (0.4-1.3 s) than successes (2.4-4.2 s) - a refusal, not an exhausted search; G1 ran on MojaveAO20, there never was a 3 x 3 km COA area.
- The generator accepted a 41 x 54 km area: UG52's 20 x 20 km is a GUI default, not a generator limit.
- G6/G7 CLOSED 2026-09-14 17:15Z: the refusal was the vendor script's hard-coded useAbstractGraphs = false (ground-vehicle-move-to.lua:488), not area-size or memory/budget - the custom including SMS (C:\C2SIM\vrf-sms) planned the 10-seam 4,989 m leg 8/8 with 0 refusals (G7b); gamewareMemorySize/QueryTimeBudget are NOT the lever (G8/G8b still refused on the stock SMS); docs/experiments/G7B_G8_RESULTS_2026-09-14.md; OPEN: whether abstract-graph paths avoid the ridge faces (FINDING_EARLY_STOPS).
- DOCS PASS 2026-09-14 20:00Z (NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE_2026-09-14.md): the flat A*
  query's documented propagation-box-extent is 200 m (Autodesk AStarQuery option,
  Ground_Vehicle.ope); the successful G7c-gate abstract routes deviated 231-292 m, outside it.
  Abstract graphs are documented COST-BLIND; the ridge face is legal to the mesh (slope-max 46
  deg, cost no-go unreachable at factor 1.0, soil invisible), so no planner flag routes around it
  - the documented lever is slope-avoidance-factor. 20:40Z HEADER READING (PREREG_N1_N2_CORRIDOR_SLOPE):
  propagation-box-extent is DEAD for ground vehicles (DtNavBot parameter; Ground_Vehicle.ope uses the
  dynamic-obstacle nav interface) -> N1 NOT RUN, AG SMS stays the fix. N2b RAN 21:05Z: P20a MISS -> STOP (AG query 0 points for EVERY goal of AtOrder members queried 5-10 s after the first area row; 5th freeze at the P11 point); N2c REFUSED identically (timing FALSIFIED); N2d (start +500 m): short goals plan, ALL long goals refused (regional AG), the unit drove the authored V0->V1->V2->V3 line 1.3 km north of the freeze - the freeze is a LINE property; remedy = pre-flight route shift - PREREG_N1_N2 sec 10.2/10.3.
- WESTERN ABSTRACT-GRAPH REFUSALS = the oversized nav area (UG52 66.2's 20 x 20 km maximum) - not
  load timing (N2c's +300 s start delay refused identically to N2b) and not a query flag; do not
  re-probe query flags. RESEARCH_ABSTRACT_GRAPH_CONNECTIVITY_2026-09-15.md.
- Western AG refusals = FRAGMENTED GENERATED nav data; SIZE FALSIFIED as cause (WEST20 control + AO20 regenerated today 189/1,600 vs 0 on 09-07, 62d810a): the LAND-COVER classification changed between 09-07 and 09-13, the generator is bit-stable; the connectivity gate is the control (STP-803); soil reads before 09-13 sit on different data (flagged STP-793).
- THE RIDGE FREEZE IS A LINE PROPERTY (N2d drove the authored line 1.3 km north of the six-run
  freeze) - the remedy is a pre-flight route shift, not a planner flag.

Harness (docs/experiments/RUNNER_EXIT127_2026-09-14.md; RUNBOOK 0.5.14; full recap archived docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 3):
- "runner exit: 127" does NOT mean "command not found" - on this MSYS bash it is a high-bit Windows code; the only SILENT one is 0xFFFFFFFF (TerminateProcess(-1) / Stop-Process / Process.Kill), so 127 + a silent log + no WER event = THE RUNNER WAS KILLED FROM OUTSIDE (killer unidentified, no process auditing here); the G6 capture was COMPLETE - "trace: (no samples)" was a separate tail-read defect, now fixed. NO Stop-Process / taskkill sweeps while a run window is open.

User rulings, 2026-09-13/14 (docs/DEMO_READINESS_2026-09-06.md rows 19-23 carry the full text):
- row 19: TASKABRT IS the TaskStatus code STP sees for a stalled unit (user, 2026-09-14 ~12:40Z).
  Abort-then-complete stands: a later TASKCMPLT is never suppressed.
- row 20: pre-flight findings are delivered as WARNINGS (an STP ObservationReport plus the VR-Forces
  overlay) FIRST; TASKABRT at order time only after calibration shows zero false alarms. The wording
  stays predictive ("PREDICTED IMPASSABLE"), never "this is why your unit will stop".
- row 21: the nav data's home is C:\C2SIM\vrf-nav (realized; C:\MAK holds zero nav entries) and the
  custom including SMS's home is C:\C2SIM\vrf-sms. If the SMS cannot live outside C:\MAK its
  placement there is SANCTIONED, as is relocating appData via --appDataDir for the G8 variable.
- rows 22/23: the hardened runner is built (f6e68d9); the reporting gaps are ranked B1-B10, with
  TASKABRT also decided (supervisor) for refused or skipped tasks.
- row 16: task-vocabulary rulings Q1-Q7 IN 2026-09-14 20:00Z (Q1 superseded -> TASKABRT and its
  successors abandoned; Q2 Duration measured on the sim clock; Q3 wire ambiguity accepted; Q4 no
  Duration + no geometry = MALFORMED, refused with error + TASKABRT, no invented hold; Q5 a
  paused scenario holds task time while the back end reports; Q6 no interim stop-gap; Q7 rollback
  forward-only). Fix pass 2 LANDED on feat/tasking-rulings @ 8db033e; pass-3 cold-start review
  RUNNING before merge; follow-up STP-809 filed (facade exposes back-end status
  Paused/Playing/gone so a dead back end does not freeze task time under Q5).

Method lessons (each one cost a false claim or a night):
- COMPARE RUNS AT EQUAL SIM TIME. Total-displacement and wall-windowed straggler verdicts produced
  two false headline claims about the nav-data runs (2026-09-13).
- NEVER RUN AGENTS DURING A TIMED RUN. The G2 "engine collapse" was this project's own concurrent
  agent load, not the mesh; the claim was withdrawn (G3).
- NEVER AWAIT A RUN THROUGH A NOTIFICATION. A timed run is polled from the FOREGROUND with bounded
  waits and the supervisor does not end its turn while a run is open (PLAN sec 0; subagent
  wait-states miss wake-ups).
- A BLANK LINE inside a pushed C2SIM message kills the SDK's STOMP pump (a blank line terminates a
  STOMP frame body) - G7 attempt 1, Jira STP-795. No multi-line XML comments in authored messages.
- A LONE PLATFORM performer never enters ground-vehicle-move-to.lua (it runs the native move-along),
  so it cannot test the mesh planner - G7 attempt 2. Measured there too: the sim's marking width is
  >= 11 characters, so reporting item B3 is about the 14-character name, not a 10-char limit.

**State at 2026-09-15 02:45Z:** main is at 03b955e (the V5/V8 appNo ledger). main carries: rulings
+ three review passes (0f4d09e), STP-809 (4cd84d7), the runner stop-rule fix (ff15a4e), the tools52
conversion (b4fcf58), V4b (16995b3), PauseSim (f13df1b) and route-shift (96fa396), all merged. GATE
G-A RERUN DONE (a9b8c2c): VrfBridge.dll re-pinned at main 5881b7d, hash E3F40524... (997,376 B,
2026-09-15T01:34:46Z) - ELEVEN consumers one hash, 19/19 offline suites, rulings-selftest 176/0,
routeshift-selftest 61/0; the 99B7B235/165e04c pin is SUPERSEDED. LIVE PROOFS off the merged build:
V2 (230706Z) ran the 42-task COA-STP1 chain, 41/42 dispatched, 40 timed TASKCMPLT, one ruled
TASKABRT cascade; V13 (001159Z) held all five gates (Q4 refusal, C16 fire/silent, stop rule +184s
of 900); B7 heading/speed PASSED off V2's capture. NAV THREAD CLOSED BY V7
(PREREG_V7_AO20_2026-09-15.md, 21c1430, C1 CONFIRMED): 6/6 formation-slot moves and the leader's
2.3 km V0 leg (30 points) planned on MojaveAO20, "not enough" 0 times in 846,600 trace lines -
product rule cap 20x20 km / tile / ratio >= 0.9 adopted, MojaveCOA retired for navigation
(mechanism in section 1's CLOSED list). V5 (PREREG_V5, 0e9884e): PAUSE HALF PASSES (2 TASK CLOCK
HELD lines naming REPORTS PAUSED, T1 at 1828/1800 armed sim s with the 181.5 s pause costing
nothing, PauseSim PAUSE_CONFIRMED/RESUME_CONFIRMED basis=state+clock both halves); KILL HALF NOT
RUN (the seat's Stop-Process was refused by the permission classifier, window closed with the back
end alive, STP-809's active-count path stays UNCONFIRMED LIVE - a re-run must anchor the kill on
the DISPATCH, t-29 s of PushOrder's own 30 s argument, not stage-8b t=0, and needs the user's hand
or a permission rule). V8 (023743Z, de32dac): shift went SOUTH - C2 SIDED the choice; FIXED c5f166d.
V8b (033226Z, 79d3fe4): ALL R8 MET - +250 m north, SIX OF SIX crossed, no crawl. V8z zero-offset control
(041036Z, 8d87391): BRANCH A - froze 19.7 m from P11; THE LATERAL OFFSET IS THE REMEDY, insertion inert. OPEN USER RULINGS: none. V6 RUN 03:10Z (scratch federation
20260915T030650Z): the tools JOIN but see NO back end + placeholder uuids only, ResetVrf a false green
(STP-820/823/822). CLOSED BY V6g (17:11Z run, V6_LIVE_JOIN_GATE sec 13, 79e1b12): the same three tasks with MOJAVE coordinates (PROBE_V6G_MOJAVE_Order.xml) COMPLETED - ws flat (13.4 MB/min vs 780-2,219), 51/51 off-road path jobs succeed in ~1 sim s, back end discoverable throughout, 3 TASKCMPLT with vendor pairing - so the 'memory runaway' was a DATA defect: the R5 order's waypoints are in SWEDEN and the interface dispatched an 8,769 km leg (STP-823 DONE; STP-833 refuse-out-of-terrain-routes MERGED and LIVE PASS 18:44Z in V6i - the Sweden order refused in 1 s, nothing reached the back end; STP-837 traversal-based arrival MERGED, first live exercise owed; RUNBOOK 11c/11d). STP-820 and STP-822 DONE (liveness live PASS). Open: the vendor's create rejection STP-825 (wire capture: sender byte-perfect; the runner's Stage 2h holder makes every launch a JOIN - first live run clean; MAK case + bundling-size experiment = user's decisions), STP-832 merged (new bridge pin 90272BC9), the surviving reading 'path LENGTH not membership' (one run V6h: a 40-60 km in-area leg), the WS tripwire abort MERGED f5c38d2 (exit 6 at 3 alerts since dispatch; warm-up reset built, unwired).
verdict, the Q5 kill-half re-run, and the nav second mechanism / offline 20x20 km control. Prior
01:00Z paragraph archived verbatim: docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 8
(append).

## 2. Where each lane stands (source: docs/PLAN_PARALLEL_LANES_2026-09-14.md - the live plan)

| Lane | What it is | Jira |
|---|---|---|
| L1 | Mesh-query refusal | STP-788 epic; STP-790/791/792/793 |
| L2 | Reporting | STP-777 epic; STP-778..787 |
| L3 | Watchdog + sim clock | STP-783 |
| L4 | Runner robustness | STP-789 epic; STP-794 |
| L5/L10 | Records | - |
| L6 | User-owned | - |
| L8 | Task vocabulary beyond MOVE | epic created 2026-09-14 |
| L9 | STP-side strict parse check | STP-615 |
| L11 | 4-27 second read + offset-line scoring | - |
| L12 | A2 detached run watchdog | STP-794 |

Per-lane "State 2026-09-14 ~13:00Z" column dropped here (superseded by section 1; duplicated by the live docs/PLAN_PARALLEL_LANES_2026-09-14.md); full table archived verbatim docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 4.

BRANCHES AND WHAT EACH NEEDS TO LAND section dropped here (2026-09-14 20:30Z, superseded - all
four feature branches landed via feat/integration, then main); full text archived verbatim
docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 6.

## 3. Standing rules (breaking one of these is how this project has lost its days)

- DOCUMENTATION FIRST, WITH CITATIONS: C:\MAK\vrforces5.2d\doc -> docs.mak.com classref -> internet.
  A prereg without BOTH a vendor citation and an own-record citation is not registered. Two failed
  attempts on one symptom = stop and research before the third. NO questions to MAK (user ruling).
- ONE VARIABLE PER PREREGISTERED RUN; predictions and falsifiers written BEFORE the run; a missed
  HIGH prediction is a stop, not something to adjust.
- ASCII-only and CRLF in tracked files: `rg -P "[^\x09\x0a\x0d\x20-\x7E]" <file>`, gated on a
  known-dirty control first, and CHAINED BEFORE `git add` - not run after the commit.
- Windows paths inside inline python one-liners lose their backslashes through the shell. Write a
  script file (scratchpad) and run that instead.
- NEVER kill rtiexec / rtiForwarder / rtiAssistant. Never write under C:\MAK except the sanctioned
  fixture deploy (plus the row-21 sanctions). Never edit the frozen C++ oracle repo.
- Vendor sim logs dump the whole environment in CLEARTEXT (secrets) - never attach one anywhere;
  send the .callstack.log / .dmp. runs/ stays gitignored.
- The PRIVATE C2SIM server ONLY: REST 18080 / STOMP 61614 (container c2sim-server-vrf). The
  operator's own 8080 / 61613 server is theirs - never push to, reset or restart it.
- FRESH LEDGERED appNumber per federate join: the one authoritative marker is
  `*** NEXT FREE: <n> ***` in docs/OPUS_EXECUTION_PLAN.md Appendix B. Read the MARKER, never the
  ledger tail, never a number quoted in prose; record each number consumed. Demo block 9101-9199.
- LAUNCH EVERY LIVE RUN THROUGH scripts\RunScenario.sh (RUNBOOK 0.5.14): 64-bit pwsh pinned, stdout
  to a FILE (never a pipe, never "| tee"), and the out-of-process teardown backstop on the
  runner.launched / runner.teardown-ran markers.

## 4. Operational state to re-verify at the start of a session (cheap, and it has bitten us)

- The RTI PAIR is up and is NOT ours to kill: rtiexec + rtiForwarder (plus any -K assistants).
  Inventory vrf* / rti* processes FIRST - a leftover VR-Forces back end HARD-BLOCKS LaunchVrf, and
  `-AllowExistingVrf` is the false-READY trap.
- C:\C2SIM\vrf-nav (the full-COA nav area, 1.2 GB) and C:\C2SIM\vrf-sms
  (C2SIM_EntityLevel_AbstractGraphs.sms): both PRESENT as of 2026-09-14 13:00Z.
- Deployed fixtures in C:\MAK\vrforces5.2d\userData\scenarios: R9_Mojave_Empty_52_NavAO (the G6/G7
  fixture) and R9_Mojave_Empty_52_NavAO_AG (the abstract-graph sibling, for G7b) - both present.
- LICENCE: RENEWED to 2026-10-31 (RUNBOOK 0.5.15; c8730e7 resolves MAKLMGRD_LICENSE_FILE per
  process). The 2026-09-15 lapse warning is superseded; archived verbatim
  docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 5.
- The deployed interface build is PINNED for the G7 series (exe 2026-09-07 11:06Z). Do not redeploy
  mid-series; re-pin deliberately after the merges (section 2).

## 5. Next steps, in order

Next-steps items 1-2 (the G7 attempt-3 verdict and the sim-clock/reporting/heading-speed/
preflight-port merge series) dropped here (2026-09-14 20:30Z, superseded - the nav-mesh
question is CLOSED per section 1 and those four branches already landed via feat/integration);
current owed next steps are in section 1's state paragraph (V6, the V8 harvest, the Q5 kill re-run). Full text archived verbatim docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 7.
1. WATCHDOG VALIDATION RUN (C16, STP-783): it is default OFF, so enable it explicitly for that run;
   it must fire on the known 1-35 freeze by sim ~500 and must NOT fire on the units that completed.
2. REPORTING LIVE GATES (L2): one run whose bus capture shows TASKSTRT at dispatch, a TASKABRT for a
   refused or stalled task, a position report for the platform-mapped unit, and zero silent push
   losses. B10's five questions to STP are drafted (DRAFT_STP_QUESTIONS_2026-09-14.md) - the USER
   sends them.
3. B9 BUNDLE RUN (STP-786): the whole reporting set exercised together on one scenario.
4. DEMO-READY residue (readiness rows 9, 13, 15, 18), then the STP TASK VOCABULARY (L8) - the user's
   stated goal beyond MOVE - and then the aggregate-level profile (Y-15). D1 2026-09-20 GUI-on rehearsal STOPPED on teardown (vrfGui survives its exit prompt, cause VERIFIED - STP-844/STP-845); D1b PASS (STP-844 fixed), D2 functional reset VERIFIED, 0.60x second-cycle speed open; route shift default ON + AO-independent preflight merged f26d4ad; D3 PASS (default-ON harmless, speed normal); STP-825 rate 42% + clustering -> persistent holder; Iron Storm export characterised (scratch ironstorm_export_report.md) - representation decision with the user; STP-825 3-arm RID experiment registered (PREREG_STP825_BUNDLING) - docs/experiments/PREREG_DEMO_REHEARSAL_2026-09-20.md D1 RESULT.
