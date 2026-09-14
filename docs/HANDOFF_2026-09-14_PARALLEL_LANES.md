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

Early stops (docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md secs 6-7d - point at it, not restate):
- sec 6 VENDOR PASS: a vehicle that stops while its task still runs is UNDETECTED BY DESIGN in 5.2
  (the base give-up test "always returns false"; the sample hands that test to the integrator).
- sec 7 CAUSE, SUPPORTED for the units that freeze ON a scored face (1-35, 4-27): seven consecutive
  8 m postings rising at 0.70-0.95 over 55 m on sand ahead of the freeze; the sampler reproduces the
  sim's own reported altitudes to ~3 cm.
- sec 7b SCOPE: this is a claim about those units, NOT about "the early stops". A SECOND mechanism is
  active (1-6 stopped on DESCENDING ground; 856/HHC on FLAT ground). Pre-flight calibration as
  corrected: 40 m window, threshold 0.92, margin 0.097, 3/3 caught, 0/6 false alarms - one order,
  one terrain, three positives.
- sec 7c it is the LEADER that freezes; followers crawl past it. The console excludes blockage,
  replan, give-up and goal change - it does NOT exclude formation speed control.
- sec 7d a THIRD stop kind: a move-along subtask FAILED ("Entity not embarked on same object as
  target"), VR-Forces re-formed the unit, and the task-less hull was PUSHED 1,994 m by the vehicle
  behind it. Two consequences that bind: (a) a vendor task FAILURE produces NO C2SIM report today
  (reporting item B1); (b) in a collapsed-ratio capture the observer's dead reckoning draws sawtooth
  motion - CHECK THE ALTITUDE RESIDUAL before reading any excursion as movement (G2's post-collapse
  tail is withdrawn as motion).

Navigation mesh (PREREG_NAVDATA_G6 sec 5 = STOP; MESH_QUERY_VS_DISTANCE_2026-09-14 sec 0):
- G6 STOPPED on its instrument gate: the 41 x 54 km area loaded, BOTH nav-area gates returned
  success, and vrf:findPathToLocation returned "not enough (0) points" for every multi-kilometre
  goal. G6 is therefore a FOURTH reproduction of the stops, not a discriminator.
- The dependence is on the AREA, not on goal distance: 10,131.6 m planned twice on the 1,600-sector
  MojaveAO20, 0 points on the 8,856-sector MojaveCOA for BYTE-IDENTICAL destinations; 22 pairs flip
  that way, none the other way. Refusals return in 0.4-1.3 s, FASTER than the 2.4-4.2 s long
  successes - a refusal, not an exhausted search. Frame correction: G1 ran on MojaveAO20; there
  never was a 3 x 3 km COA area.
- The generator accepted a 41 x 54 km area: UG52's 20 x 20 km is a GUI default, not a generator limit.
- G6/G7 CLOSED 2026-09-14 17:15Z: the refusal was the vendor script's hard-coded
  useAbstractGraphs = false (ground-vehicle-move-to.lua:488), not an area-size or memory/budget
  limit. The custom including SMS (C:\C2SIM\vrf-sms) planned the 10-seam, 4,989 m leg 8/8 with 0
  refusals (G7b); gamewareMemorySize and gamewareQueryTimeBudget are NOT the lever (G8/G8b still
  refused on the stock SMS). Record: docs/experiments/G7B_G8_RESULTS_2026-09-14.md (in progress);
  OPEN: whether abstract-graph paths avoid the ridge faces (FINDING_EARLY_STOPS).

Harness (docs/experiments/RUNNER_EXIT127_2026-09-14.md; RUNBOOK 0.5.14):
- "runner exit: 127" does NOT mean "command not found". On this MSYS bash it is a high-bit Windows
  exit code; the only SILENT one is 0xFFFFFFFF = TerminateProcess(-1) = Stop-Process / Process.Kill.
  127 + a silent log + no WER event = THE RUNNER WAS KILLED FROM OUTSIDE. The killer is unidentified
  (no process auditing on this machine). The G6 capture was COMPLETE; "trace: (no samples)" was a
  separate tail-read defect, fixed. NO Stop-Process / taskkill sweeps while a run window is open.

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

**State at 2026-09-14 17:30Z:** main is at cd2106c (appNo ledger for the appData validation, G8,
G7b and G8b runs). feat/integration sits at 26efe0c (tasking foundation V2/V3 merged, 10 suites
green, native rebuilt in the worktree, gate G-B PASS) and feat/tasking-rulings at 5c67d41 plus
its fix pass (cold-start review REVIEW_RULINGS_5c67d41_2026-09-14.md = FIX FIRST on M1-M5; fix
pass running). The day's four probe runs (4-7) all tore down cleanly, the watchdog standing down
each time. Owed: gate G-A - rebuild the native VrfBridge/VrfFacade and redeploy to the ten
consumer copies, then re-pin the deployed build, once the rulings fix pass merges into
feat/integration; the validation runs follow. Open with the user (Q1-Q4 of
REVIEW_RULINGS_5c67d41_2026-09-14.md): whether a superseded task still completes at its authored
end time, which clock a C2SIM Duration is measured on, whether a zero-geometry task may look
identical to a performed one on the wire, and how a task with no Duration and no geometry should
behave; supervisor defaults stand meanwhile (superseded -> TASKABRT, Duration on the SIM clock
under Vrf:TaskClock, wire ambiguity accepted, DefaultHoldSeconds 60). The MAK licence is RENEWED
to 2026-10-31 (RUNBOOK 0.5.15), superseding the 2026-09-15 lapse tripwire above.

## 2. Where each lane stands (source: docs/PLAN_PARALLEL_LANES_2026-09-14.md - the live plan)

| Lane | What it is | State 2026-09-14 ~13:00Z | Jira |
|---|---|---|---|
| L1 | Mesh-query refusal | attempts 1 and 2 VOID (tripwires above); ATTEMPT 3 (tank platoon 1222.MechPlt) launched 13:04Z as runs/20260914T130439Z_run - verdict PENDING, PREREG_MESHQUERY_G7 sec 5 | STP-788 epic; STP-790/791/792/793 |
| L2 | Reporting | assessment done (11 gaps, B1-B10); B5/B3/B1/B2/B8 built on feat/reporting | STP-777 epic; STP-778..787 |
| L3 | Watchdog + sim clock | feat/sim-clock f052ea7, reviewed to pass 4, supervisor-verified; MERGE AFTER G7 | STP-783 |
| L4 | Runner robustness | landed on main (f6e68d9); first live use = the G7 attempts | STP-789 epic; STP-794 |
| L5/L10 | Records | this file + docs/DEMO_RUNBOOK.md (readiness row 13) | - |
| L6 | User-owned | licence renewal; DI-Guy package; TASKABRT decided; C:\MAK sanctions given | - |
| L8 | Task vocabulary beyond MOVE | assessment running (docs-first) | epic created 2026-09-14 |
| L9 | STP-side strict parse check | running (offline console test) | STP-615 |
| L11 | 4-27 second read + offset-line scoring | running | - |
| L12 | A2 detached run watchdog | running (scripts only) | STP-794 |

BRANCHES AND WHAT EACH NEEDS TO LAND (worktrees under .claude\worktrees\; `git worktree list`):
- feat/sim-clock @ f052ea7 - the C16 progress watchdog (report-only, DEFAULT OFF) measured on the
  back end's scenario clock. Reviewed until clean (pass 3 = REVIEW3_SIMCLOCK_08146a2; pass-4 fixes
  verified: 66/66 stall checks, wall path identical to 51d78a5 on ten feeds, Decide byte-identical).
  Needs: the merge, then the validation run below.
- feat/reporting @ f0d1c68 - B5 (sniff the inbound root: no more false SDK deserialize ERROR), B3
  (find the created object when VR-Forces truncates its name), B1 (TASKSTRT at dispatch, TASKABRT
  for tasks that will never run), B2 (never lose a report silently), B8 (re-announce a substitution),
  plus one review fix. Branched off 08146a2, so it carries part of the sim-clock series with it.
  Needs: a cold review pass, the merge, then its live gates.
- feat/heading-speed (B7, heading + speed in the reports; native) and feat/preflight-port @ 7672957
  (B4, the calibrated leg scorer ported into the interface at order receipt - the Python tool stays
  test-harness only; the product contains no Python). Both branched off f052ea7; both worktrees are
  locked while their executors run.
- MERGE ORDER: sim-clock, reporting, heading-speed, preflight-port.
- AFTER THE SIM-CLOCK MERGE THE NATIVE DLL MUST BE REBUILT AND REDEPLOYED. main's VrfFacade /
  VrfBridge have NO SimTimeSeconds (verified: the symbol exists only on the branch), so the merged
  managed code cannot read the scenario clock until the C++ is rebuilt (/t:Rebuild always; back up
  the DLLs; redeploy all 7 copies). Re-pin the deployed build for the next run afterwards.

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
- LICENCE: the node-locked DEMO licence LAPSES 2026-09-15. It gates the whole toolchain, including
  vrfNavGenerator. The renewal is user-owned and the download was still owed at 13:00Z.
- The deployed interface build is PINNED for the G7 series (exe 2026-09-07 11:06Z). Do not redeploy
  mid-series; re-pin deliberately after the merges (section 2).

## 5. Next steps, in order

1. G7 ATTEMPT 3 VERDICT (L1). Harvest against PREREG_MESHQUERY_G7 sec 3 and fill sec 5: per leg -
   seams / gate current / gate destination / mesh outcome / N points / gate-to-outcome seconds /
   vertex reached. Points on leg 1 AND 0 on leg 3 -> the graph IS connected across seams and the
   failure is length or budget -> G7b (the abstract-graph SMS, fixture _AG), then G8
   (gamewareMemorySize via --appDataDir). 0 points on leg 1 -> the generated data is unusable across
   seams, the "area size" reading is WITHDRAWN and the generation becomes the suspect.
2. MERGES + REBUILD + DEPLOY: sim-clock, then reporting (after its cold review), then heading-speed,
   then preflight-port; rebuild and redeploy the native VrfBridge / VrfFacade after the sim-clock
   merge (section 2), then re-pin the deployed build.
3. WATCHDOG VALIDATION RUN (C16, STP-783): it is default OFF, so enable it explicitly for that run;
   it must fire on the known 1-35 freeze by sim ~500 and must NOT fire on the units that completed.
4. REPORTING LIVE GATES (L2): one run whose bus capture shows TASKSTRT at dispatch, a TASKABRT for a
   refused or stalled task, a position report for the platform-mapped unit, and zero silent push
   losses. B10's five questions to STP are drafted (DRAFT_STP_QUESTIONS_2026-09-14.md) - the USER
   sends them.
5. B9 BUNDLE RUN (STP-786): the whole reporting set exercised together on one scenario.
6. DEMO-READY residue (readiness rows 9, 13, 15, 18), then the STP TASK VOCABULARY (L8) - the user's
   stated goal beyond MOVE - and then the aggregate-level profile (Y-15).
