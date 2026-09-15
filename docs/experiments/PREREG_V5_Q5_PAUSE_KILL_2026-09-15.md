# PREREG V5 - Q5 live gate 11: a PAUSED scenario must not age a C2SIM task, and a KILLED back end must fall to the wall clock (written 2026-09-15 ~02:10Z, BEFORE the run)

Tier HEAVY (two PASS/FAIL halves of an assessment live gate, one of them carrying a falsifier the user registered as a
STOP, plus an irreversible manual action inside a live federation). Gate: PREREG (this file). Nothing below has been run.

Docs consulted: docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md sec 7.1a live gate 11 (the pause half, the kill
half as STP-809 extended it, and the three-outcome PROBE as pass 3 re-worded it) and 7.1b rows E2 / N4; docs/RUNBOOK.md
sec 11 (Q5: HOLD vs WALL, `StallPolicy.TaskClockAction`'s five tests in order, the once-a-minute repeat), sec 10 (the C16
watchdog is OFF by default and STAYS off here), sec 9 (the 2026-09-15 G-A RERUN pin: eleven consumers at
`E3F405249C561284F464A37F9CC06D49A74208FA8AD88D1515F76877A0CA4702`, `BackendControlState` and `ActiveBackendCount`
PRESENT), sec 0.5.14 item 13 (`--pause-at` / `--resume-at`, stage 8b) and item 14 (the MANUAL kill procedure);
tools/PauseSim/Program.cs (the `[RESULT]` contract and the verdict words); src/VrfC2SimApp/StallPolicy.cs
(`StaleClockWarnSeconds` 60.0, `LogRateLimitSeconds` 60.0, `ModeSwitchConfirmations` 3) and
src/VrfC2SimApp/VrfC2SimService.cs (`SampleTaskClock` and `MaybeCompleteTimed`, both at a 1 s cadence);
docs/experiments/PREREG_V7_AO20_2026-09-15.md RESULTS (the AO20 fixture, the V1 destination gate, sim ratio 5.70x);
docs/experiments/PREREG_V13_REPORTING_WATCHDOG_2026-09-14.md RESULTS (DurationScale 0.1 measured: 740 s of sim clock
served in 132 s of wall); scratchpad review3/VALIDATION_RUN_PLAN.md (the V5 sketch this file corrects).

---

## 1. THE RUN

| item | value |
|---|---|
| fixture | `R9_Mojave_Empty_52_NavAO20_AG_S2` (the AO20 terrain V7 proved; SMS `C2SIM_EntityLevel_AbstractGraphs_Slope2`) |
| init | `data/COA-STP1_Initialization.xml` - THE ORIGINAL, not the N2d copy |
| order | `data/PROBE_RIDGE_1-35_Order.xml` (one task, T1, Duration `PT2H` = 7,200 s) |
| task clock | `Vrf:TaskClock=sim` (the shipped default); C16 `Vrf:StallDetection` **OFF** (the shipped default) |
| duration scale | `Vrf__DurationScale=0.25` -> **1,800 s of task clock** |
| pause / resume | `--pause-at 120 --resume-at 300` (stage 8b; two ledgered appNumbers the RUNNER claims itself) |
| kill | MANUAL, by the SEAT, at **t+400 s** of the stage-8b clock (RUNBOOK 0.5.14 item 14) |
| window | `--run-secs 1200`, `--no-stop-when-complete` |
| consoles | `--object-console 3 --member-console 3` (3 is the floor the nav-area gate needs) |
| gate | `--pre-order-gate nav-area --pre-order-settle 240`, `--position-report 10`, `--sample-threads` |
| launch line | `scratchpad\v5v8\v5_launch.sh` |

ONE CHANGE OF SUBSTANCE AGAINST V7: the init. V7 used `COA-STP1_Initialization_N2d.xml` to place 1-35 inside the mesh;
V5 uses the ORIGINAL, so 1-35 de-stacks to its usual start (34.658442/-116.740092), the origin-vertex drop fires
(`Vrf:DropOriginVertexMeters=100`: the unit's live position ends up 2,774 m from its authored origin, so the leading
route point V0 is dropped), and the V1 leg is driven STRAIGHT because V1 is 206 m outside the AO20 extent and fails the
vendor's `Is destination in nav area?` gate (V7 P-D, measured). **THAT IS FINE AND IT IS NOT WHAT THIS RUN MEASURES.**
This probe is about the CLOCK. Where the tanks stop is irrelevant to every prediction below.

### 1a. FOUR CORRECTIONS TO THE SKETCH, each with the arithmetic that forces it

The VALIDATION_RUN_PLAN sketch reads "DurationScale 0.1 ... --pause-at 120 --resume-at 240 ... --run-secs 900
--stop-when-complete". Every one of those four is wrong for this run and the sources say why. Registered here BEFORE the
run so the corrections cannot be mistaken for post-hoc tuning.

- **C1. `DurationScale` 0.1 -> 0.25.** At 0.1 the armed Duration is 7,200 x 0.1 = 720 s **of SIM clock**, and the sim
  runs at about 5.7x wall on this fixture (V7 measured 5.70x; V13 measured 740 sim s served in 132 wall s for exactly
  this scale). T1 would therefore reach its end time at about **t+128 s of WALL** - BEFORE `PauseSim` ever issues its
  pause, which lands at t+120 plus the tool's own settle (up to 15 s) and confirm. The pause would have nothing to hold,
  and with the stop rule armed the window would close before the resume and long before the kill. At 0.25 the armed
  Duration is 1,800 s of sim clock, about 316 s of running wall - four times the pause offset.
- **C2. `--resume-at` 240 -> 300 (a 180 s pause, not 120).** `StallPolicy.StaleClockWarnSeconds` is 60.0 and
  `LogRateLimitSeconds` is 60.0, so the FIRST `TASK CLOCK ... HELD` line cannot appear until 60 wall seconds after the
  clock stops and the second cannot appear until 60 more. A 120 s pause puts the second line at the resume instant - a
  coin flip - while the sketch's own prediction is that it REPEATS (2 occurrences). 180 s puts the two lines at
  pause+60 and pause+120 with 60 s of margin before the resume clears the stale flag.
- **C3. `--stop-when-complete` -> `--no-stop-when-complete`.** The stop rule closes the window once every task carries a
  terminal report, plus a 60 s settle (V13: closed at t+184 s). This run has ONE task; ANY terminal report for it before
  t+400 - the timed completion at a faster-than-expected ratio, or a vendor-side TASKABRT - would close the window and
  the kill would never happen. The kill is the point of the run, so the window is not allowed to close on its own.
- **C4. `--run-secs` 900 -> 1200.** After the kill the axis serves WALL seconds, which are ~5.7x slower than the sim
  seconds it was serving, so the remaining armed Duration needs up to ~710 s of wall at the slowest ratio in the record.
  900 s would cut the kill half's own PASS element off the end of the run. See the table in sec 2a.

Anything the seat wants to revert is one token on the launch line; the cost of each reversion is named above.

---

## 2. THE TIMELINE (t = the stage-8b clock, seconds after `PushOrder` returned - RUNBOOK 0.5.14 item 11)

| t | what happens | who |
|---|---|---|
| ~+1 to +3 | T1 dispatches (TASKSTRT; the TerrainProfile reply re-entry) and its end time is armed | interface |
| +120 (+3..18 s of PauseSim settle) | `PauseSim pause <appNo>` - `controller->pause()` on ALL back ends | runner, stage 8b |
| ~+190 | first `TASK CLOCK: ... HELD` line (60 s of flat clock) | interface |
| ~+250 | second `TASK CLOCK: ... HELD` line (`LogRateLimitSeconds` 60) | interface |
| +300 (+3..18 s) | `PauseSim resume <appNo>` - `controller->run()` | runner, stage 8b |
| ~+306 | `TASK CLOCK: the simulation clock is readable and advancing again` | interface |
| +305 to +400 | the V6 LIVE JOIN GATE, **PART A ONLY** (`scratchpad\v5v8\v6_join_gate.sh`) | SEAT, another shell |
| **+400** | **the SEAT kills the `vrfSimHLA1516e` back end by pid** (RUNBOOK 0.5.14 item 14) | SEAT |
| +400 to +1200 | the kill half and the probe are observed; the runner runs the window out with a dead sim | - |

### 2a. THE ARITHMETIC, AND WHAT IT IS ROBUST TO

Let `r` be the sim ratio. Sim seconds served by the kill are `S = r x (218 +/- 15)` s of RUNNING wall (t+2 to the pause
at ~t+130, then the resume at ~t+305 to the kill at t+400); the held 180 s contribute ZERO. The remainder
`R = 1800 - S` is then served on the WALL clock at 1 s/s, so T1 completes at `t + 400 + R`.

| r | sim s served at the kill | remaining R (wall s) | TASKCMPLT at | inside the 1,200 s window? |
|---|---|---|---|---|
| 5.0 | 1,090 | 710 | t+1,110 | yes |
| **5.7 (V7)** | **1,243** | **557** | **t+957** | **yes** |
| 6.5 | 1,417 | 383 | t+783 | yes |
| 7.5 (N2d) | 1,635 | 165 | t+565 | yes |
| 8.26 | 1,800 | 0 | completes AT the kill | edge case - the design's upper bound |

T1 is still ARMED at the kill for any `r` below 8.26, and its wall-clock completion lands inside the window for any `r`
above 4.6. Every ratio ever measured on this build (5.6, 5.70, ~7.5) is inside that band. **If the harvest shows `r`
outside 4.6-8.2, the kill half's completion element (V5h) is VOID and says nothing** - registered now, not decided later.

COUNTERFACTUAL, for reading the pause half: with no kill, T1 would complete at `t + 2 + 1800/r + 180`, i.e. **t+498 at
r=5.7**. The +180 in that expression IS the pause. The kill at +400 arrives first by design.

**WHY t+400 AND NOT THE SKETCH'S t+420.** The armed-at-the-kill condition is `r < 1800 / (running wall at the kill)`.
At t+400 that is 8.26; at t+420 it is 7.53, which N2d's ~7.5x straddles. Twenty seconds buys the whole margin.

**THE ONE LOAD-BEARING ASSUMPTION UNDER BOTH HALVES: NOTHING ELSE TERMINATES T1 BEFORE THE KILL.** Any TASKCMPLT or
TASKABRT CANCELS the armed end time, and an unarmed T1 takes V5h away and - on probe outcome (c) - leaves the kill with
no witness at all (sec 4). Three things could terminate it and none is expected here: (i) ARRIVAL at V3 - impossible,
since ~1,250 sim s of driving reaches only about V1 (V7 measured V1 at sim 1,348 and V2 at 2,787) and after the kill
nothing moves; (ii) the C16 progress watchdog - it is OFF, which is the whole reason this run does not set
`Vrf__StallDetection`; (iii) a VENDOR give-up on the ridge freeze - the vendor does not give up, by design: the base
give-up test always returns false and the move-to Lua has no progress watchdog (memory: VRF stall detection is the
integrator's), and in six freeze runs the sim reported running / unblocked / goal-unchanged forever. **If T1 does carry
a terminal report before t+400, the kill half is VOID for V5h and the run is scored on V5f/V5g alone** - and the
terminal report itself is then the finding, because item (iii) would have been refuted.

---

## 3. PASS/FAIL, THE PAUSE (assessment gate 11, first half)

- **V5a (HIGH).** During the pause the line

      TASK CLOCK: the simulation clock has not advanced past <T> s for <S> wall seconds and the back end REPORTS
      PAUSED (DtPauseControlType; BackendCount=1, active=1) - the scenario is PAUSED, not gone. C2SIM task times are
      HELD: 1 task(s) waiting on an end time age by NOTHING until the scenario runs again (Q5, user ruling 2026-09-14).

  appears and **REPEATS once per wall minute: exactly 2 occurrences**, at about t+190 and t+250. It must name the
  CONFIRMED pause (`REPORTS PAUSED (DtPauseControlType; ...)`) and carry **NO** caveat sentence.
  MISS, shape A: the clause reads `the control state is UNKNOWN - falling back to the back-end COUNT` or
  `the control state could not be READ` - then the control state is not reaching this process on the new pin and **that
  is a finding in itself** (gate 11's own words); record `BackendCount=` and `active=` and do not score the rest of this
  half as a pass. MISS, shape B: fewer than 2 occurrences with no resume in between -> the repeat is not happening; STOP
  and read `LogRateLimitSeconds`.
- **V5b (HIGH).** **NO TASKCMPLT, and no TaskStatus of any kind for T1, between the pause and the resume.** The end time
  is 1,800 s of task clock and only ~700 have been served, so a completion in that window would mean the axis moved while
  the scenario stood still - the exact thing Q5 forbids.
- **V5c (HIGH).** The two `[RESULT] PauseSim` lines read, in the field order the tool contracts:
  - pause: `verdict=PAUSE_CONFIRMED basis=state+clock exit=0 ... controlBefore=Running controlAfter=Paused ...
    clockDelta=0.000` (the scenario clock did not advance over the 2 s hold).
  - resume: `verdict=RESUME_CONFIRMED basis=state+clock exit=0 ... controlBefore=Paused controlAfter=Running ...` with
    `clockDelta` strictly positive (about `2 x r` = ~11 s at r=5.7 under fixed-frame-run-to-complete).

  `basis=clock` alone (the control state not read) downgrades this to the CLOCK-ONLY verdict and is the same finding as
  V5a shape A. `PAUSE_CONTRADICTED` / `RESUME_CONTRADICTED` (exit 1) is a FAIL of this half.
  NOTE: the sketch calls these verdicts "PAUSE_CONFIRMED / RESUME"; the tool's words are `PAUSE_CONFIRMED` and
  `RESUME_CONFIRMED` (tools/PauseSim/Program.cs, the verdict block).
- **V5d (HIGH).** **The pause costs the order exactly nothing.** `simTimeAfterHold` on the RESUME line equals
  `simTimeAfterHold` on the PAUSE line to within a couple of seconds - the scenario clock did not move for 180 s of wall -
  and one `TASK CLOCK: the simulation clock is readable and advancing again (<T> s)` INFO line appears within ~5 s of the
  resume. The completion arithmetic of sec 2a then reconciles: sim seconds served + ZERO for the held interval + wall
  seconds after the kill = 1,800, to within the 1 s sampling staircase times the ratio (about +/- 3 x r s, the C3 margin).
- **V5e (RECORDED, not predicted).** The `active=` and `BackendCount=` numbers on EVERY `TASK CLOCK` line, both halves.
  Gate 11 asks for them explicitly: they are the measurement, whichever way the run goes.

---

## 4. PASS/FAIL, THE KILL (assessment gate 11, second half - the half STP-809 added)

The seat kills `vrfSimHLA1516e` by pid at t+400 (list before killing; never a sweep; never `rtiexec` / `rtiForwarder` /
`rtiAssistant`; never `vrfGui` or `VrfC2SimApp`). T1 is still armed (sec 2a).

- **V5f (HIGH).** Within `StallPolicy.ModeSwitchConfirmations` = 3 samples of the vendor withdrawing the back end, the
  task-clock axis moves to the **WALL** clock, and the run SAYS SO. WHICH line says it depends on which of the three
  probe outcomes occurs, and the two cases are NOT interchangeable - this is the detail the sources force and the sketch
  does not carry:
  - **If the reader stays READABLE (probe outcomes (a) cached-flat and (b) 0.0):** at kill+60 s the stale branch fires
    with `activeBackends == 0` (test 1 of `TaskClockAction`) and prints

        TASK CLOCK: the simulation clock has not advanced past <T> s for <S> wall seconds and the back end's last
        status said RUNNING but NOT ONE known back end is still simulatable or in transition - it has DIED, it was not
        paused (BackendCount=1, active=0) - the simulation is GONE, not paused. C2SIM task times (1 task(s) waiting on
        an end time) are served on the WALL clock until a back end returns; no task is completed early and no wait is
        restarted.

  - **If the reader goes UNREADABLE (probe outcome (c), the facade returning -1):** the hysteresis takes it at
    kill+~3 s, and **the `TASK CLOCK` transition line does NOT print.** The E1-corrected branch is guarded on
    `_taskClockStaleWarned`, which the RESUME cleared, so nothing in that branch can fire. What prints instead is
    `MaybeCompleteTimed`'s clock line, re-logged because `usingSim` flipped:

        TIMED COMPLETION (R4): 1 task(s) are timing out against the WALL clock - Vrf:TaskClock=sim, but the sim clock
        could not be read; Vrf:DurationScale=0.25.

    **That line is only emitted because a task is still armed** (`MaybeCompleteTimed` returns early at
    `_timed.Count == 0`), which is the second reason this run keeps T1 armed across the kill.

  MISS: neither line within 90 s of the kill -> the axis did not switch; go to V5g.
- **V5g (THE FALSIFIER - a miss here is a STOP, not an adjustment).** Gate 11 names it: **if the `HELD` line keeps
  repeating with `active=` a NON-ZERO number for longer than the vendor's back-end status timeout after the kill, then
  STP-809's active count is a NO-OP on this stack.** The pre-STP-809 behaviour then stands (frozen task time, one WARNING
  a minute), the next lever is the per-address `getControlState(addr)` / `lookupBackend(addr)->status()` pair rather than
  the aggregate, and this lane STOPS for a user ruling. Record the `active=` and `BackendCount=` numbers from every
  `TASK CLOCK` line either way - they are the measurement.
- **V5h (HIGH, the PASS element the window was lengthened for).** T1's remaining armed Duration completes **on the wall
  clock**, at `t + 400 + R` (sec 2a; ~t+957 at r=5.7), with

      TIMED COMPLETION: task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' on <unit> reached its END TIME - <served> s of a 1800 s
      Duration served on the wall clock (C2SIM Duration x Vrf:DurationScale 0.25). R4: completion is given by the end
      time.

  and ONE TASKCMPLT on the bus. No task may complete EARLY and no wait may restart: `<served>` must be >= 1,800 and the
  completion must not arrive before `t + 400 + R - 3r`.
  VOID (not a miss) if the harvested ratio is outside 4.6-8.2 - sec 2a.
- **V5i (MEDIUM, the instrument check).** No `TASK CLOCK: the back-end CONTROL STATE could not be read (STP-809)` warning
  anywhere: that warning is a `MissingMethodException` and means a PARTIAL DEPLOY (RUNBOOK sec 9), which would invalidate
  the whole gate. No `SIM CLOCK: stepped BACKWARDS` before the kill.

---

## 5. THE PROBE - THREE OUTCOMES, NO PREDICTION (gate 11, as pass 3 re-worded it)

What `DtVrfRemoteController::simTime()` returns for a back end that is in the vendor's list but DEACTIVATED is unknown.
All three are defensible. **RECORD WHICH HAPPENED AND WHAT THE LOG SAID, VERBATIM** - this is a measurement, and no
outcome is a failure of the run:

- **(a) the last CACHED value** -> readable, flat -> `Stale` after 60 s -> the stale branch, where the control state and
  the active count now decide (V5f, readable case).
- **(b) 0.0** -> a large BACKWARDS step, classified `RolledBack`, so a `SIM CLOCK: stepped BACKWARDS, <was> s -> 0.0 s
  (DtVrfRemoteController::rollbackToSnapshot, vrfRemoteController.h:605)` WARNING fires FIRST and is MISLEADING; the axis
  re-anchors at 0 and then goes flat, reaching the same stale branch by a different route.
- **(c) a throw** -> the facade returns -1 -> unreadable -> `ModeSwitchConfirmations` samples -> the wall clock via the
  hysteresis, with the re-logged `TIMED COMPLETION (R4)` line as the only witness (V5f, unreadable case).

Record, for each: the first sim-clock values visible in the log after the kill, the exact wall instant of the kill (the
seat writes it down - RUNBOOK 0.5.14 item 14), and every `TASK CLOCK` / `SIM CLOCK` / `TIMED COMPLETION` line for 120 s
afterwards. Before STP-809 (a) and (b) both ended in a permanent freeze; V5f/V5g are the test of whether they still do.

---

## 6. WHAT THE RUNNER DOES AFTER THE KILL - UNMEASURED, RECORD IT

RUNBOOK 0.5.14 item 14 states the knowns, and they are the reason `--run-secs 1200` is affordable at all:

- **The runner DOES NOT DETECT the sim's death during the window.** Stage 8b polls the APP (`$AppProc.HasExited`); the
  one place it watches the back-end pid is the stage-7d pre-order hold, which is over before the order is pushed. So the
  window runs to its cap with a dead sim. Expect that; it is not a fault.
- **Teardown still runs** (it is a `finally`). Whether `StopVrf52.ps1` behaves as it does against a live sim is
  **UNMEASURED**. READ ITS EXIT CODE, do not assume. MAK's crash-dump prompt may appear;
  `scripts\AnswerCrashDumpDialog.ps1` (0.5.12) is what answers it.
- **Afterwards the federation has a corpse in it.** Inventory before the next launch (0.5.0): a leftover back end
  HARD-BLOCKS `LaunchVrf` and `-AllowExistingVrf` is the false-READY trap.
- **The observers go blind.** `WatchVrf` keeps running against a dead federation and its trace simply stops advancing.
- RECORD: the teardown exit codes, whether the dump prompt appeared, whether `rtiexec` and `rtiForwarder` survived (they
  must - they are NEVER touched), whether the detached watchdog stood down on `runner.teardown-ran`, and how long the app
  took to notice anything at all.

---

## 7. STOP CONDITIONS

- V5g - the falsifier - fires: STOP, do not iterate, report for a user ruling.
- Any HIGH miss in sec 3.
- Neither line of V5f fires within 90 s of the kill: that is a real defect in the interface (a silent clock-mode change),
  and a STOP.
- A `MissingMethodException` or `Tick phase '<name>' FAILED` line anywhere: the deployed tree is not the G-A RERUN pin and
  NOTHING in this run is evidence about STP-809. Stop and re-check sec 9's hash.
- The pause half never fires (the window closed first, or `PauseSim` exited 2): the run is void for Q5; fix the offsets.
- A crash before t+400.

## 8. CONFOUNDS, STATED

- **The V6 gate runs inside this federation between t+305 and t+400**, PART A ONLY: `SmokeTest`, which joins nothing, and
  `ResetVrf --dry-run`, which joins, discovers and issues no deletes. Two extra federate joins and ~35 s of one core. It
  perturbs the sim ratio slightly; the ratio is measured over the pre-pause segment and the 4.6-8.2 band already allows
  for load. **PARTS B AND C OF THAT BLOCK MUST NOT RUN IN THIS FEDERATION** - `ResetVrf` without `--dry-run` DELETES
  every object, `SetSimRate` changes the very clock this run measures, and `CreateTaskAgg` creates and tasks new units.
  The block's own header says so.
- `PauseSim` blocks the runner's poll loop for its own ~20 s per half (by design - the clock reading has to be the one at
  that offset), and the loop polls every 5 s while either half is armed, so the offsets are honoured to about 5 s.
- The sim ratio is not a constant: it is load-bound under `fixed-frame-run-to-complete`. Everything in sec 2a is written
  as a band for that reason.
- 1-35 will almost certainly freeze on the ridge as it has in seven runs. IRRELEVANT here - C16 is OFF, so no stall
  TASKABRT can cancel the armed end time - and deliberately not predicted.
- The kill instant is a human action with a few seconds of jitter. Every prediction above is scored against the RECORDED
  instant, not against the nominal 400.

## 9. HARVEST

- `vrfc2simapp.log`: every `TASK CLOCK`, `SIM CLOCK` and `TIMED COMPLETION` line, in order, with wall stamps, and the
  `active=` / `BackendCount=` numbers from each.
- `runs\<run>\pausesim-pause.stdout.log` and `pausesim-resume.stdout.log`: the two `[RESULT] PauseSim` lines verbatim, and
  the manifest's `probes.pauseResume` block (armed, the offsets, whether each half fired, the appNumber, whether it was
  consumed, the exit code and every parsed field).
- `reports-captured.log`: TaskStatus codes and their instants for T1 - exactly one TASKSTRT, exactly one TASKCMPLT, and
  NOTHING between the pause and the resume.
- `tools/analysis/sim_ratio.py` on the run dir for `r`, measured over the **PRE-PAUSE segment only**: after the pause the
  clock is held and after the kill it is dead, so a whole-window ratio would be meaningless (the compare-at-equal-sim-time
  lesson).
- The teardown logs and the runner log for sec 6.

## 10. ADVERSARIAL REVIEW OF THIS PRE-REGISTRATION (HEAVY; before the run)

**Strongest competing reading of a PASS: "the HOLD line proves nothing - the clock was flat because the sim was busy,
not because it was paused."** Answered by construction, three ways: the line now names `DtPauseControlType` as a POSITIVE
reading rather than inferring a pause from flatness (STP-809); `PauseSim` independently samples the scenario clock either
side of a 2 s hold and reports the delta; and the pause is something THIS RUN CAUSED at a known offset, not something
observed. If the clause degrades to the `BackendCount` fallback, V5a shape A already says that is a finding, not a pass.

**Strongest competing reading of a KILL PASS: "the axis fell to wall because the interface lost the RTI, not because the
active count dropped."** Distinguishable in the log: the `active=0` route prints the `it has DIED, it was not paused`
clause with BOTH numbers; the hysteresis route prints no `TASK CLOCK` line at all and only the re-logged
`TIMED COMPLETION (R4) ... WALL` line. Sec 4 registers both and asks which. They are not scored as the same evidence.

**The symptom this prereg would otherwise leave unexplained.** The E1-corrected transition line is guarded on
`_taskClockStaleWarned`, and the resume CLEARS that flag. So on probe outcome (c) the mode change to the wall clock is
SILENT on the `TASK CLOCK` channel. That is not a defect for the run to discover and shrug at: it is registered as V5f's
second case, with the exact line that does fire instead, and it is why C1 and C3 (make the Duration outlast the kill;
keep the window open) are not cosmetic. If NEITHER line fires, that is a real interface defect and a STOP (sec 7).

**What this run CANNOT settle.** Whether the vendor's `doTimeouts()` deactivation is visible through
`DtBackend::isInSimulatableState` / `isInTransitionStatus` in every death mode - it tests exactly ONE death mode, a
`Stop-Process -Force` on the back-end pid, which is not a network partition, a hung process or a graceful `StopVrf`. It
cannot settle the teardown question (sec 6) beyond one observation. And it says nothing about the ridge freeze, the route
shift, or anything the tanks do.

**Verified vs assumed.** VERIFIED (read this pass, in the sources named at the top): every constant, every log-line text,
the `TaskClockAction` test order, the `[RESULT]` contract and the verdict words, the origin-vertex-drop condition, the
runner's offsets clock and its one-appNumber-per-invocation rule, and the pin's member presence. ASSUMED (not re-verified
here): that the deployed `bin\Release-5.2` trees are still at the G-A RERUN pin at run time - V5i is the check - and that
the sim ratio on this fixture stays inside 4.6-8.2, which sec 2a makes a stated precondition rather than a hope.

---

## RESULTS - not run
