# HANDOFF - parallel lanes to the demo (opened 2026-09-14)

THE CURRENT ENTRY POINT (the newest HANDOFF_*.md, by convention). Read CLAUDE.md (repo root) first, then section 1 below, then the lane
table. Supersedes HANDOFF_2026-09-01_R9_COMPLETE.md AS THE ENTRY POINT; that file remains the 5.0.2 / R9 record and its CLOSED list still
binds. ASCII + CRLF. HARD CAP 200 LINES AND 160 CHARACTERS A LINE - when a phase closes, collapse it to a few lines plus a pointer; never
drop live guidance to make room. Deep-read only the record your task touches; re-verify load-bearing claims against artifacts, not prose.
Collapsed material is verbatim in docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md and docs/experiments/HANDOFF_2026-09-21_ARCHIVE.md.

## 1. CLOSED - tripwires. Do NOT reopen without NEW live evidence. Each line names its record.

RULINGS - read this before you write the word "ruling" anywhere:
- A ruling is only what docs/RULINGS.md or docs/RULINGS_ARCHIVE.md quotes: the question as it was PUT and the owner's OWN words, with the transcript line. A
  selected option label is the seat's wording, not his words. Anything else is a supervisor statement and must say so.
- TASK COMPLETION: a TEMPORARY position is in force (owner 2026-09-21, RL-20260921-09, quoted there in full) until the doctrine and vendor research is done;
  the longer-term rule awaits that research (RL-20260921-08). Quote the ledger; never paraphrase it into a rule. The completion unit (2026-09-25, scope
  approved RL-20260925-01, branch feat/completion-temporary-position; LIVE CONFIRMATION OWED) builds it: an early finish is held to start time + Duration; a
  unit still travelling then completes when it arrives (one OVERDUE line; its follow-ons wait); no-destination tasks end at their Duration; the stall
  TASKABRT is no longer hidden and abandons the stuck unit's follow-ons. Stall detection: shipped OFF, ON in the DEMO profile. The 746c091 behaviour
  (complete at the Duration though not arrived) is gone - CORRECTIONS_LOG F-4. "R4" is
  row 4 of the 2026-09-14 question table ("When is SECURE / OCCUPY / DEFEND complete - on arrival, after its Duration, or never?") and it was never put about
  a MOVE: RL-20260914-02, with RL-20260907-01, RL-20260921-05, -07 and -08.
- The placement re-clamp (8aeb127): ANSWERED 2026-09-21, RL-20260921-06 (S555). The placement STAYS; do not revert 8aeb127. The same unit made the dispatch
  gate measure-and-log only (no hold, no BOUND-BUT-NOT-ON-THE-GROUND, no refusal) and fixed the "NEVER MEASURED" tally. Still PRE-WARM the area.

ORBAT / movement (docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md CLOSED list C1-C15 - canonical, read there):
- C1-C10 are CLOSED and their text as this doc used to carry it is archived verbatim HANDOFF_2026-09-21_ARCHIVE.md sec 4. The live imperatives: C1 the company
  move failure had TWO superimposed causes, structure (C1a) and movement (C1b), and C1b's mechanism is a move-into-formation gate whose template HQ-section
  M998 never reaches its slot - not a race, not "unresolved formation", not a vendor bug. C2 THE FIX IS COMPOSE: empty shell, create members,
  addToOrganization, task the parent; 3/3 deterministic. C3 do not retry the template path at company+. C4 AggregateFormation auto stays OFF. C5 a coarse leaf
  EXPANDS to compose. C6 no post-attach reorganize and no client-side formation-validity wait - both were refuted as "gaps". C7 leader identity does not gate
  movement. C8 address VRF objects by their REAL VRF_UUID from ObjectCreated, never a name-as-DtUUID. C9 there is no vendor MSDL/ORBAT importer sample. C10
  sendVrfObjectCreateMsg + initialFormation is DROPPED.
- C11 THE FIRST INSTRUMENT for any VRF behaviour question is the object's OWN CONSOLE at notify level 4. Silence at the vendor default level 1 is a configured
  outcome, not evidence.
- C12 LIFEFORMS CRASH the 5.2d headless sim until the DI-Guy data package is installed (user-owned, readiness row 17). Every infantry / mortar / CSS /
  fire-support row of the fidelity table hits it.
- ALTITUDE is settled - read docs/VRF_ALTITUDE_FRAMES.md sec 5 + 7 before writing about burial, clamps or altitude frames; "buried therefore never moves"
  re-entered a third time on 2026-09-21.
- C13 THE INIT IS THE ORBAT (context); the ORDERS define the simulated set. COA-STP1 = 11 taskees, not 128 units. Vrf:CreationPolicy=AtOrder is the demo
  default. Never size a run by the init.
- C14 co-located units are SPREAD at startup (DeStack, 700 m rings) - user ruling 2026-09-07 (RL-20260907-02), supersedes the 2026-07-13 R8 ruling (that one
  was 5.0.2). C15 a unit's TASKCMPLT may be reported from the unit's OWN ARRIVAL EVIDENCE (a MAJORITY of members within 500 m of the last vertex); the
  vendor's later completion is swallowed once (RL-20260907-01).

Early stops (docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md secs 6-7d canonical; full recap archived HANDOFF_2026-09-14_ARCHIVE_pm.md sec 1):
- sec 6 VENDOR PASS: a vehicle that stops while its task still runs is UNDETECTED BY DESIGN in 5.2 (the base give-up test always returns false).
- sec 7 CAUSE SUPPORTED for the units that freeze ON a scored face (1-35, 4-27): seven consecutive 8 m postings at 0.70-0.95 over 55 m on sand ahead of the
  freeze; sampler matches sim altitude to ~3 cm. sec 7b SCOPE: that is a claim about those units only, NOT about "the early stops" generally - a SECOND
  mechanism is active (1-6 on descending ground, 856/HHC on flat). Calibration as corrected: 40 m window, 0.92 threshold, 0.097 margin, 3/3 caught, 0/6 false
  alarms.
- sec 7c it is the LEADER that freezes; followers crawl past it. The console excludes blockage, replan, give-up and goal change - it does NOT exclude
  formation speed control. sec 7d a THIRD stop kind: a failed move-along subtask ("Entity not embarked on same object as target") left a task-less hull PUSHED
  1,994 m by the vehicle behind it; a vendor task FAILURE produces NO C2SIM report today (B1), and CHECK THE ALTITUDE RESIDUAL before reading push motion as
  movement.

Navigation mesh (PREREG_NAVDATA_G6_2026-09-13.md sec 5 = STOP; MESH_QUERY_VS_DISTANCE_2026-09-14.md sec 0; full recap archived
HANDOFF_2026-09-14_ARCHIVE_pm.md sec 2):
- G6 stopped on its instrument gate and the refusal proved to be a property of the AREA, not of goal distance (22/22 byte-identical goals planned on
  MojaveAO20 and refused on MojaveCOA; refusals return FASTER than successes, so it is a refusal and not an exhausted search). UG52's 20 x 20 km is a GUI
  default, not a generator limit. The evidence bullets are archived verbatim HANDOFF_2026-09-21_ARCHIVE.md sec 5.
- G6/G7 CLOSED 2026-09-14 17:15Z: the refusal was the vendor script's hard-coded useAbstractGraphs = false (ground-vehicle-move-to.lua:488), not area-size or
  memory/budget - the custom including SMS (C:\C2SIM\vrf-sms) planned the 10-seam 4,989 m leg 8/8 with 0 refusals (G7b); gamewareMemorySize/QueryTimeBudget
  are NOT the lever (G8/G8b still refused on the stock SMS); docs/experiments/G7B_G8_RESULTS_2026-09-14.md; OPEN: whether abstract-graph paths avoid the ridge
  faces (FINDING_EARLY_STOPS).
- DOCS PASS 2026-09-14 (NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE; PREREG_N1_N2_CORRIDOR_SLOPE sec 10.2/10.3; the N1/N2b/N2c/N2d run narrative is archived verbatim
  HANDOFF_2026-09-21_ARCHIVE.md sec 3): propagation-box-extent is DEAD for ground vehicles, so N1 was NOT RUN and the abstract-graph SMS stays the fix.
  Abstract graphs are documented COST-BLIND and the ridge face is legal to the mesh (slope-max 46 deg, soil invisible), so NO planner flag routes around it -
  the documented lever is slope-avoidance-factor. N2c falsified load timing as the cause of the refusals.
- WESTERN ABSTRACT-GRAPH REFUSALS: the cause is FRAGMENTED GENERATED nav data. It is NOT load timing (N2c's +300 s start delay refused identically to N2b) and
  NOT a query flag - do not re-probe query flags. An earlier line here blamed the oversized area; AREA SIZE WAS FALSIFIED as the cause on 2026-09-15 by the
  WEST20 control and the AO20 regeneration (189/1,600 vs 0 on 09-07, 62d810a): the LAND-COVER classification changed between 09-07 and 09-13 and the generator
  is bit-stable. The connectivity gate is the control (STP-803); soil reads from before 09-13 sit on different data (STP-793).
  RESEARCH_ABSTRACT_GRAPH_CONNECTIVITY_2026-09-15.md.
- THE RIDGE FREEZE IS A LINE PROPERTY (N2d drove the authored line 1.3 km north of the six-run freeze) - the remedy is the pre-flight route shift, not a
  planner flag.

Harness (docs/experiments/RUNNER_EXIT127_2026-09-14.md; RUNBOOK 0.5.14; recaps archived HANDOFF_2026-09-14_ARCHIVE_pm.md sec 3 and
HANDOFF_2026-09-21_ARCHIVE.md sec 6):
- "runner exit: 127" does NOT mean "command not found" here - it is a high-bit Windows code, and 127 with a silent log and no WER event means THE RUNNER WAS
  KILLED FROM OUTSIDE. NO Stop-Process / taskkill sweeps while a run window is open.

User rulings, 2026-09-13/14 (docs/DEMO_READINESS_2026-09-06.md rows 19-23 carry the full text; the OWNER'S WORDS are in docs/RULINGS.md
and docs/RULINGS_ARCHIVE.md - RL-20260913-01, RL-20260913-02, RL-20260913-03, RL-20260914-01, RL-20260914-02, RL-20260914-05):
- row 19: TASKABRT IS the TaskStatus code STP sees for a stalled unit (RL-20260914-01). That entry covers the CODE only: a later TASKCMPLT never being
  suppressed is a supervisor's reading of it, not part of his answer - see the entry's own scope note.
- row 20: his answer (RL-20260913-01) welcomes the pre-flight idea and asks how it would be delivered. The delivery order is SUPERVISOR design, no
  owner words on file: WARNINGS (an STP ObservationReport plus the VR-Forces overlay) FIRST; TASKABRT at order time only after calibration shows zero
  false alarms. The wording stays predictive ("PREDICTED IMPASSABLE"), never "this is why your unit will stop".
- row 21: the nav data's home is C:\C2SIM\vrf-nav (realized; C:\MAK holds zero nav entries) and the custom including SMS's home is C:\C2SIM\vrf-sms. If the
  SMS cannot live outside C:\MAK its placement there is SANCTIONED, as is relocating appData via --appDataDir for the G8 variable (RL-20260914-01 item 3,
  RL-20260913-02).
- rows 22/23: the hardened runner is built (f6e68d9); the reporting gaps are ranked B1-B10, with TASKABRT also decided (SUPERVISOR, no owner words on file)
  for refused or skipped tasks.
- row 16: the seven task-vocabulary rulings of 2026-09-14 20:00Z are quoted in full, question and answer, at RL-20260914-05 - read them there, not from a
  restatement. In short: Q1 superseded -> TASKABRT and successors abandoned; Q2 Duration on the sim clock; Q3 wire ambiguity accepted; Q4 no Duration and no
  geometry = MALFORMED, refused, no invented hold; Q5 a paused scenario holds task time; Q6 no interim stop-gap; Q7 rollback forward-only. Built on
  feat/tasking-rulings @ 8db033e; STP-809 followed (RL-20260914-06).

Method lessons (each one cost a false claim or a night):
- COMPARE RUNS AT EQUAL SIM TIME. Total-displacement and wall-windowed straggler verdicts produced two false headline claims about the nav-data runs
  (2026-09-13).
- NEVER RUN AGENTS DURING A TIMED RUN (the G2 "engine collapse" was this project's own concurrent agent load, not the mesh; claim withdrawn in G3), and NEVER
  AWAIT A RUN THROUGH A NOTIFICATION - a timed run is polled from the FOREGROUND with bounded waits and the supervisor does not end its turn while a run is
  open (PLAN sec 0; subagent wait-states miss wake-ups).
- A BLANK LINE inside a pushed C2SIM message kills the SDK's STOMP pump (a blank line terminates a STOMP frame body) - G7 attempt 1, Jira STP-795. No
  multi-line XML comments in authored messages. A LONE PLATFORM performer never enters ground-vehicle-move-to.lua (it runs the native move-along), so it
  cannot test the mesh planner - G7 attempt 2; measured there too, the sim's marking width is >= 11 characters, so reporting item B3 is about the 14-character
  name, not a 10-char limit.

**State - the live carry-over.** The run-by-run narrative to 2026-09-15 02:45Z is archived verbatim: HANDOFF_2026-09-21_ARCHIVE.md sec 1.
- BUILD PIN (RUNBOOK :2331, NEW PIN 2026-09-15 16:52Z, STP-832 merge 13e8c73): VrfBridge.dll 90272BC95297E330 (1,001,472 B, mtime 16:52:06Z). ELEVEN
  5.2 consumers must carry ONE hash; E3F40524 (997,376 B) and 99B7B235 / 165e04c are SUPERSEDED. Always -p:BridgeConfig=Release-5.2 and check the
  OUTPUT TREE, not the exit code.
- NAV PRODUCT RULE (adopted after V7, 21c1430): area cap 20 x 20 km, one tile, connectivity ratio >= 0.9. MojaveCOA is RETIRED for navigation; MojaveAO20 is
  the fixture. The 0.9 bar is a SUPERVISOR's gate and NOT an owner ruling - RL-UNVERIFIED-NAV09 - and it is not reopened here; it stands or falls on its own
  engineering merits.
- The WS runaway tripwire aborts a run at 3 alerts since dispatch (f5c38d2, exit 6); the warm-up reset is built but unwired.
- STILL OPEN from that series: the vendor create rejection STP-825 (working posture = a persistent HOLDER federate so the sim JOINS; the MAK case is OPEN,
  RL-20260921-02), and the surviving reading "path LENGTH, not membership" - one run, V6h, still owed.
- OPEN: the Q5 kill-half re-run - STILL unconfirmed (RUNBOOK :2692-2697; anchor the kill on the DISPATCH instant); authorised, RL-20260920-01 (item 9).
- OPEN: STP-853, the interface reported a dead sim as healthy and dispatched a task into it (DEMO_READINESS row 10).
- Waiting on the owner: sec 6 items; his own unanswered question RL-20260921-04; the 300 m vs 350 m readiness row (RL-20260921-01). The watchdog
  default is decided for the demo profile (ON, 2026-09-25, RL-20260925-01). The re-clamp keep/remove question is ANSWERED (RL-20260921-06, S555).

## 2. Where each lane stands (source: docs/PLAN_PARALLEL_LANES_2026-09-14.md - the live plan)

The lanes and their Jira homes, one line instead of the old thirteen-line table, which is archived verbatim HANDOFF_2026-09-21_ARCHIVE.md
sec 7: L1 mesh-query refusal (STP-788 epic; STP-790/791/792/793); L2 reporting (STP-777 epic; STP-778..787); L3 watchdog + sim clock
(STP-783); L4 runner robustness (STP-789 epic; STP-794); L5/L10 records; L6 user-owned; L8 task vocabulary beyond MOVE (epic created
2026-09-14); L9 STP-side strict parse check (STP-615); L11 4-27 second read + offset-line scoring; L12 A2 detached run watchdog (STP-794).

Archived verbatim 2026-09-14 20:30Z, HANDOFF_2026-09-14_ARCHIVE_pm.md: the per-lane "State ~13:00Z" column (sec 4) and BRANCHES (sec 6; all landed).

## 3. Standing rules (breaking one of these is how this project has lost its days)

- DOCUMENTATION FIRST, WITH CITATIONS: C:\MAK\vrforces5.2d\doc -> docs.mak.com classref -> internet. A prereg without BOTH a vendor citation and an own-record
  citation is not registered. Two failed attempts on one symptom = stop and research before the third. CORRECTION 2026-09-21: this rule used to end "NO
  questions to MAK (user ruling)". That is a SUPERVISOR STATEMENT, not an owner ruling - he was asked once and never answered (RL-UNVERIFIED-MAK01), and on
  2026-09-21 he wrote "5. Open." about the MAK support case (RL-20260921-02). The case is OPEN. Draft questions here, and let him send them.
- ONE VARIABLE PER PREREGISTERED RUN; predictions and falsifiers written BEFORE the run; a missed HIGH prediction is a stop, not something to adjust.
- ASCII-only and CRLF in tracked files: `rg -P "[^\x09\x0a\x0d\x20-\x7E]" <file>`, gated on a known-dirty control first, and CHAINED BEFORE `git add` - not
  run after the commit.
- Windows paths inside inline python one-liners lose their backslashes through the shell. Write a script file (scratchpad) and run that instead.
- NEVER kill rtiexec / rtiForwarder / rtiAssistant. Never write under C:\MAK except the sanctioned fixture deploy (plus the row-21 sanctions). Never edit the
  frozen C++ oracle repo.
- Vendor sim logs dump the whole environment in CLEARTEXT (secrets) - never attach one anywhere; send the .callstack.log / .dmp. runs/ stays gitignored.
- The PRIVATE C2SIM server ONLY: REST 18080 / STOMP 61614 (container c2sim-server-vrf). The operator's own 8080 / 61613 server is theirs - never push to,
  reset or restart it. (The demo itself may use 8080 / 61613 - RL-20260920-01 item 2.)
- FRESH LEDGERED appNumber per federate join: the one authoritative marker is `*** NEXT FREE: <n> ***` in docs/OPUS_EXECUTION_PLAN.md Appendix B. Read the
  MARKER, never the ledger tail, never a number quoted in prose; record each number consumed. Demo block 9101-9199.
- LAUNCH EVERY LIVE RUN THROUGH scripts\RunScenario.sh (RUNBOOK 0.5.14): 64-bit pwsh pinned, stdout to a FILE (never a pipe, never "| tee"), and the
  out-of-process teardown backstop on the runner.launched / runner.teardown-ran markers.
- THE RECORD CHECKS RUN OFFLINE: `pwsh -NoProfile -File tests\RunnerTurnaround.Tests.ps1` section 13 enforces these caps, the altitude and completion
  tripwires, and the ledger ids. Regenerate its two generated files only with the named switches, and READ the diff.

## 4. Operational state to re-verify at the start of a session (cheap, and it has bitten us)

- The RTI PAIR is up and is NOT ours to kill: rtiexec + rtiForwarder (plus any -K assistants). Inventory vrf* / rti* processes FIRST - a leftover VR-Forces
  back end HARD-BLOCKS LaunchVrf, and `-AllowExistingVrf` is the false-READY trap.
- C:\C2SIM\vrf-nav (the full-COA nav area, 1.2 GB) and C:\C2SIM\vrf-sms (C2SIM_EntityLevel_AbstractGraphs.sms): both PRESENT as of 2026-09-14 13:00Z.
- Deployed fixtures in C:\MAK\vrforces5.2d\userData\scenarios: R9_Mojave_Empty_52_NavAO (the G6/G7 fixture) and R9_Mojave_Empty_52_NavAO_AG (the
  abstract-graph sibling, for G7b) - both present.
- LICENCE: RENEWED to 2026-10-31 (RUNBOOK 0.5.15; c8730e7 resolves MAKLMGRD_LICENSE_FILE per process). The 2026-09-15 lapse warning is superseded; archived
  verbatim HANDOFF_2026-09-14_ARCHIVE_pm.md sec 5.
- The deployed interface build is PINNED for the G7 series (exe 2026-09-07 11:06Z). Do not redeploy mid-series; re-pin deliberately after the merges (section
  2).

## 5. Next steps, in order

Old items 1-2 of 2026-09-14 (G7 attempt-3 verdict; the four-branch merge series) are superseded; verbatim in HANDOFF_2026-09-14_ARCHIVE_pm.md sec 7.
1. WATCHDOG VALIDATION RUN (C16, STP-783): it is default OFF, so enable it explicitly for that run; it must fire on the known 1-35 freeze by sim ~500 and must
  NOT fire on the units that completed.
2. REPORTING LIVE GATES (L2): one run whose bus capture shows TASKSTRT at dispatch, a TASKABRT for a refused or stalled task, a position report for the
  platform-mapped unit, and zero silent push losses. B10's five questions to STP are drafted (DRAFT_STP_QUESTIONS_2026-09-14.md) - the USER sends them.
3. B9 BUNDLE RUN (STP-786): the whole reporting set exercised together on one scenario.
4. DEMO-READY residue (readiness rows 9, 13, 15, 18), then the STP TASK VOCABULARY (L8) - the user's stated goal beyond MOVE (RL-20260903-01) - and then the
  aggregate-level profile (Y-15).

D-series and Iron Storm, the live carry-over. The full run-by-run narrative is archived verbatim: HANDOFF_2026-09-21_ARCHIVE.md sec 2.
- USER RULINGS 2026-09-21 (RL-20260921-02): rtiAssistant closed; Iron Storm cut-A one diagnostic drive authorised; DEMO CLOCK = FAST; the D5b callstack read
  (a vendor use-after-destroy race, STP-854); the MAK case OPEN, package in prep.
- Way B is VERIFIED LIVE end to end (D5c/D10, 4f1f149 and 51d59c0), closing readiness rows 5 and 7 and half of 13. STP-855 (route-origin race) CONFIRMED on
  both provenance branches and CLOSED; STP-852 confirmed (ordering, not wall time); STP-854 stays open, 3 confounds.
- Iron Storm cut A: the fixture is deployed and drivable; T10 crossed the 0.82-0.90 connectivity band for the first time (crawling at 0.28 m/s, cause open).
  T02 and T14, the control, measured 0 m displacement, both created at FALLBACK altitude on a cold-streamed terrain - STP-856, a CREATE defect. It is NOT a
  shown freeze cause; that causal claim was withdrawn on 2026-09-21 (VRF_ALTITUDE_FRAMES sec 5). It blocks full cut A and its T14-only fallback either way.
- The sim clock is load-dependent, not a constant (D8: 2.6x-4.7x on R9 run-to-complete). Name the sim-clock source and the window in any speed claim.

## 6. U2 STATUS 2026-09-21 (the record-cleanup unit)

- LANDED on this branch: docs/RULINGS.md + docs/RULINGS_ARCHIVE.md (the ledger); lane A's dated corrections at every STP-857 and setAltitude/burial site; the
  offline record checks (tests/RunnerTurnaround.Tests.ps1 section 13 + tests/RecordChecks.ps1), enforced.
- OPEN WITH THE OWNER: the Jira STP-857 correction text, drafted and NOT posted. The re-clamp decision is ANSWERED (RL-20260921-06, S555).
- RESEARCH BACKBONE, and it is the seat's work and not his: docs/experiments/TASK_COMPLETION_RESEARCH_2026-09-21.md, the per-verb completion table. It
  recommends nothing. Two headline MEASUREMENTS from it: of the four vendor task scripts examined, none evaluates a desired effect; plt_attack_by_fire
  ends on 'no more enemy contacts detected' plus a give-up timeout (file:line evidence in the research doc); the sim can be asked through plan trigger
  conditions (DtCeEntDestroyed, DtCeEntInArea), which this repo does not use.
- BUILT BUT NOT INSTALLED: the two PreToolUse hooks (docs/SESSION_HOOKS.md). Installing them edits a settings.json and is his call.
- CODE UNIT MERGED a9d738f; live run COMPLETION_CONFIRM-2026-09-26-1 VOID on its P10 (teardown), but the owner ACCEPTED its completion rows
  as live confirmation (n=1); stall abort + attack/breach fallback still OWED (row 26). Then the cold-start units, re-scoped by RL-20260921-08.
