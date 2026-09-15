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

## RESULTS - run 20260915T021316Z_run (PAUSE HALF; KILL HALF NOT RUN)

Harvested 2026-09-15 (Opus HARVEST executor, HEAVY). Read-only over `runs\20260915T021316Z_run`; no vendor sim log was
opened. Sources: `vrfc2simapp.log` (589,147 lines), `reports-captured.log` (15,977 reports), `watchvrf-trace.csv`
(251,824 usable console sim samples), `pausesim-pause.stdout.log` / `pausesim-resume.stdout.log`, `run-manifest.json`,
the runner log (`scratchpad\validation\v5_runner.log`) and `scratchpad\validation\v6_partA_out.txt`.

### R0. WHAT ACTUALLY HAPPENED, AND THE THREE CORRECTIONS TO THE TIMELINE OF SEC 2

The pause half ran exactly as registered. **The kill half did not happen**: the seat's `Stop-Process` was refused by the
permission classifier and the manual kill of pid 65952 did not arrive before the window closed at its 1,200 s cap
(`--no-stop-when-complete`, by design). `StopVrf52.ps1` found pid 65952 alive at teardown and closed it gracefully, so
the back end was never killed at all. **V5f, V5g and V5h are NOT RUN - they are not passes and not misses.**

Three corrections the run forces on sec 2's timeline, all VERIFIED:

- **C-T1. `t = 0` is NOT the dispatch; the dispatch is at `t - 29 s`.** `PushOrder` carries a 30 s argument and the
  runner's stage-8b clock starts when the child returns, not when the order lands. The order reached the bus at
  **02:15:49.705Z** (runner log) and TASKSTRT was on the bus at **02:15:50.715Z**, while stage-8b `t=0` is
  **02:16:19.4-02:16:20.1Z** (back-computed from the manifest's `firedUtc` for both halves). Sec 2's "~+1 to +3 s: T1
  dispatches" is wrong by about 30 s, and it is 30 s in the direction that SHORTENS the kill half's margin (C-T3).
- **C-T2. The measured sim ratio is 7.13x, not 5.7x.** Pre-pause (post-order) least squares over the object-console
  stamps: **7.30x**; post-resume **6.66x** over the whole tail and **6.80x** over the first 325 s; whole-capture 5.83x
  (that figure is contaminated by the hold and must not be used). The one that matters is task-clock served / running
  wall = 1828 / 256.5 = **7.13x**. Inside sec 2a's stated 4.6-8.2 precondition.
- **C-T3. At the nominal kill instant T1 had 8.9 s of wall left, not the comfortable margin sec 2a computed.** T1
  completed at **02:23:08.672Z = t+408.9 s**; the kill was to be at t+400. With the true dispatch at t-29.0 the
  armed-at-the-kill condition is `r < 1800 / (429.0 - 181.5) = 7.27x` and the measured r was 7.13x - inside by 2%, not
  by the 8.26x sec 2a claimed. **Any re-run of the kill half must anchor its offset on the DISPATCH, not on stage-8b
  t=0, or shorten it.**

### R1. VERDICT TABLE

| id | tier | verdict | one line |
|---|---|---|---|
| V5a | HIGH | **PASS** | exactly 2 `TASK CLOCK ... HELD` lines, at 61 s and 122 s of flat clock, both naming `REPORTS PAUSED (DtPauseControlType; BackendCount=1, active=1)`, neither carrying the caveat sentence |
| V5b | HIGH | **PASS** | 2 TaskStatus reports in the entire run (TASKSTRT 02:15:50.715Z, TASKCMPLT 02:23:08.672Z); ZERO of any code between the pause and the resume |
| V5c | HIGH | **PASS** | `PAUSE_CONFIRMED basis=state+clock exit=0 ... clockDelta=0.000` and `RESUME_CONFIRMED basis=state+clock exit=0 ... clockDelta=2.052`; both `basis=state+clock`, neither CONTRADICTED. One parenthetical of the prediction MISSED - see F-1 |
| V5d | HIGH | **PASS** (one tolerance exceeded) | `simTimeAfterHold` 1167.855 (pause) vs `simTimeBefore` 1167.888 (resume): +0.033 s over 181 s of wall; recovery line inside ~1 s of the clock restarting; the completion arithmetic reconciles at 7.13x. The 1828-vs-1800 overshoot is 28 sim s against sec 3's `3 x r` = 21 s tolerance - see F-2 |
| V5e | RECORDED | **DONE** | `BackendCount=1, active=1` on both HELD lines; the recovery line carries neither, by design (it is the E1 recovery branch) |
| V5f | HIGH | **NOT RUN** | the back end was never killed |
| V5g | FALSIFIER | **NOT RUN** | the falsifier was not exercised; the lane is NOT cleared by this run |
| V5h | HIGH | **NOT RUN** | no kill, so no wall-clock remainder. T1 instead completed wholly on the SIM clock, inside the window, at t+408.9 s |
| V5i | MEDIUM | **PASS** for its whole-run half | zero `MissingMethodException`, zero `TASK CLOCK: the back-end CONTROL STATE could not be read (STP-809)`, zero `Tick phase '<name>' FAILED`, zero `SIM CLOCK:` lines of any kind (so no backwards step). `PauseSim` also printed `control-state read-back = AVAILABLE (VrfBridge.BackendControlState, STP-809)` |

**The pause half of assessment live gate 11 PASSES. The kill half is still owed, and STP-809's active-count path is
still UNCONFIRMED LIVE.**

### R2. THE TASK CLOCK LINES, VERBATIM (V5a, V5e)

Five `TASK CLOCK` matches in the whole app log: the start-up `TASK CLOCK (R4)` banner, the `CHAIN DEPTH` line (which
contains the words "TASK CLOCK"), and the three below. There is no fourth HOLD line and no STP-809 warning.

    warn: VrfC2Sim[0]  (line 96287)
          TASK CLOCK: the simulation clock has not advanced past 1167.9 s for 61 wall seconds and the back end REPORTS
          PAUSED (DtPauseControlType; BackendCount=1, active=1) - the scenario is PAUSED, not gone. C2SIM task times are
          HELD: 1 task(s) waiting on an end time age by NOTHING until the scenario runs again (Q5, user ruling
          2026-09-14).

    warn: VrfC2Sim[0]  (line 96301)
          TASK CLOCK: the simulation clock has not advanced past 1167.9 s for 122 wall seconds and the back end REPORTS
          PAUSED (DtPauseControlType; BackendCount=1, active=1) - the scenario is PAUSED, not gone. C2SIM task times are
          HELD: 1 task(s) waiting on an end time age by NOTHING until the scenario runs again (Q5, user ruling
          2026-09-14).

    info: VrfC2Sim[0]  (line 96861)
          TASK CLOCK: the simulation clock is readable and advancing again (1168.7 s) - C2SIM task times are served on
          it once more.

COUNT 2, exactly as predicted; REPEAT 61 s, exactly `LogRateLimitSeconds`; WORDING the confirmed-pause wording, with NO
caveat clause. **`active=1`**: the vendor called the paused back end simulatable-or-in-transition throughout, which is
what makes `TaskClockAction` test 2 (PAUSED -> HOLD) the one that fired rather than test 1. The recovery line's
neighbours in the log are console rows stamped sim 1174.05-1174.25, i.e. it printed about 1 s after the scenario clock
restarted and about 4 s before `PauseSim`'s resume `[RESULT]`.

### R3. TASKSTATUS ON THE BUS (V5b)

Every TaskStatus report in `reports-captured.log`, in full - there are two, out of 15,977 captured reports (15,872
Position, 103 Observation):

| stamp (UTC) | report # | code | task | reporting entity |
|---|---|---|---|---|
| 02:15:50.715Z | 232 | TASKSTRT | cd589832-... | d6df3c3d-... |
| 02:23:08.672Z | 5865 | TASKCMPLT | cd589832-... | d6df3c3d-... |

The pause window is **02:18:21.4Z (command) / 02:18:32.3Z (`[RESULT]`) to 02:21:23.1Z / 02:21:33.6Z**. Nothing of any
code lies in it - in fact nothing lies between 02:15:50.7Z and 02:23:08.7Z at all. No TASKABRT, no TASKINPRG, no second
TASKSTRT. V5b PASS.

### R4. THE TWO `[RESULT]` LINES (V5c)

    [RESULT] PauseSim action=pause verdict=PAUSE_CONFIRMED basis=state+clock exit=0 appNumber=4381 backends=1
    controlBefore=Running controlAfter=Paused confirmSecs=3.0 simTimeBefore=1166.988 simTimeAfterCmd=1167.855
    simTimeAfterHold=1167.855 holdSecs=2.1 clockDelta=0.000 utc=2026-09-15T02:18:32.258Z

    [RESULT] PauseSim action=resume verdict=RESUME_CONFIRMED basis=state+clock exit=0 appNumber=4382 backends=1
    controlBefore=Paused controlAfter=Running confirmSecs=3.0 simTimeBefore=1167.888 simTimeAfterCmd=1170.794
    simTimeAfterHold=1172.846 holdSecs=2.1 clockDelta=2.052 utc=2026-09-15T02:21:33.647Z

Field order, verdict words and `basis=state+clock` are exactly the contract. Neither degraded to `basis=clock`, so V5a
shape A did not occur on either half. appNumbers 4381/4382 CONSUMED (manifest `appNumberConsumed: true` for both);
marker advanced 4374 -> 4383.

### R5. THE PAUSE COST THE ORDER NOTHING (V5d) - THE ARITHMETIC

The completion lines, verbatim:

    TIMED COMPLETION: task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1' on 1-35/2/1_A~PXY reached its END TIME - 1828 s of a 1800 s
    Duration served on the simulation clock (C2SIM Duration x Vrf:DurationScale 0.25). R4: completion is given by the
    end time.

    SENT TASK STATUS REPORT (TASKCMPLT) taskee=d6df3c3d-... task=cd589832-... - task 'T1_AOA_SE_1-35_AR;_2/1_AD_P1'
    reached the end time given by its C2SIM Duration (1800 s after dispatch).

**"served on the simulation clock"** - the axis never left sim mode, which is the whole claim. Four independent
measurements agree that the held 181 s contributed nothing:

1. **The interface's own reader.** `simTimeAfterHold` 1167.855 at the pause, `simTimeBefore` 1167.888 at the resume:
   **+0.033 s over 181 s of wall**. The two HELD lines both report the same flat reading, 1167.9.
2. **The vendor's own console channel, which the interface does not write.** The object-console rows in
   `watchvrf-trace.csv` stop dead for **181.5 s** (trace wall 189.3 -> 370.8; about 02:18:26.8Z -> 02:21:28.3Z) and the
   sim stamp on the rows either side moves **1167.05 -> 1167.89, i.e. 0.84 s**. At the 7.3x that was running a moment
   earlier, 181.5 s of wall was worth about 1,325 sim s; the scenario produced 0.84.
3. **The served count against the clock itself.** Between the two bus reports the console sim stamp advanced
   **1825.5 s** while the app logged **1828 s** served - 0.14% apart. The axis tracked the sim clock one for one and
   nothing else.
4. **The completion instant against the counterfactuals.** Observed **02:23:08.672Z**. Wall between the two bus reports
   438.0 s, of which 181.5 s was frozen, leaving 256.5 s of running wall (1828 / 256.5 = 7.13x). Re-serving the hold on
   the pre-Q5 rules and re-running the arithmetic at the post-resume 6.82x:

   | model | task-clock seconds the hold would add | predicted TASKCMPLT | vs observed |
   |---|---|---|---|
   | **Q5 HOLD (shipped)** | 0 | **02:23:08.7Z** | **observed** |
   | pre-Q5 (wall served once STALE at 60 s) | 122 | 02:22:50.9Z | 17.8 s too early |
   | naive "a pause ages the task" | 182 | 02:22:42.1Z | 26.6 s too early |

   The separation (17.8 s) is more than 4x the run's own completion overshoot (3.9 s of wall, F-2), so the observation
   discriminates: the axis held, it did not serve wall seconds.

### R6. THE KILL HALF - NOT RUN, AND WHAT IS STILL OWED

No `Stop-Process` was issued. `StopVrf52.ps1`'s teardown inventory found `vrfSimHLA1516e pid=65952 threads=81` ALIVE and
closed it with `taskkill /PID 65952` (no `/F`) - a graceful close at about 02:36:45Z, 13 minutes after the completion
and outside any measurement. So:

- V5f, V5g and V5h are NOT RUN. Sec 5's three-outcome PROBE (what `simTime()` returns for a DEACTIVATED back end) was
  NOT exercised: no outcome (a), (b) or (c) was observed, and `active=` was never seen as anything but 1.
- RUNBOOK sec 11's "**UNCONFIRMED LIVE** (assessment live gate 11)" line stands unchanged for the kill half.
- Sec 6's unmeasured teardown-against-a-dead-sim question is also untouched: this teardown ran against a LIVE sim.

### R7. INSTRUMENT FINDINGS (none of them changes a verdict)

- **F-1 (new, and it matters for reading any future `PauseSim` output).** `DtVrfRemoteController::simTime()` as read by
  a freshly joined control federate LAGS the back end's own console clock, and the lag GROWS for several seconds after a
  resume. Measured against the console stamps at the same wall instants: lag 0.0 sim s at the "before" read (paused),
  **12.4 sim s** at `simTimeAfterCmd`, **32.3 sim s** at `simTimeAfterHold`. That is why `clockDelta=2.052` over a 2.1 s
  hold reads as 0.98x when the scenario was in fact running at 6.8x (the console went 1167.9 -> 1176.0 in the 1.2 s
  after the resume). Sec 3's parenthetical "`clockDelta` about `2 x r` = ~11 s" is therefore REFUTED as an expectation;
  the predicate it qualifies ("strictly positive") is what held. **`PauseSim`'s `clockDelta` is a LIVENESS test, not a
  rate measurement** - it must never be used to estimate a sim ratio, and a `basis=clock`-only verdict off a 2 s hold is
  quantisation-prone. Either lengthen `holdSecs` or say so in the tool's own output.
- **F-2.** The timed completion overshot by **28 sim s** (1828 served against 1800 armed) = **3.9 s of wall** at 7.13x.
  Sec 3's tolerance was "the 1 s sampling staircase times the ratio, about `3 x r`" = 21 s, so the overshoot is 7 sim s
  outside it. The mechanism is F-1: the axis reader advances in bursts of up to about 30 sim s, not in a 1 s staircase,
  so the 1 s timed walk can only ever notice the end time one burst late. The tolerance is the thing that was wrong,
  not the interface; a future prereg should write it as `3 x r` **plus one reader burst**.
- **F-3.** The V6 gate's `ResetVrf` ran from 02:22:11Z to 02:23:43Z, straddling the completion at 02:23:08.7Z. It cost
  nothing measurable: the ratio over trace wall 400-500 (which contains it) is 6.89x against 6.78x for the next 100 s.
  Sec 8's confound is CLOSED as negligible for this run.

### R8. THE V6 LIVE JOIN GATE, PART A (`scratchpad\validation\v6_partA_out.txt`, 02:22:11Z-02:23:43Z)

**GATE 0 SmokeTest: PASS, exit 0.** All the required strings are present:
`[PASS] new VrfBridge() - IJW load + native vrf::VrfFacade constructed in-process`;
`native stack = 5.2|C:\MAK\vrforces5.2d\bin64\vrfcontrol.dll  (stack=5.2)`;
`[PASS] subscribed to ObjectCreated / TaskCompleted / TextReport / ScenarioClosed`;
`[PASS] dispose - native facade teardown (~VrfFacade -> Stop) ran without fault`;
`SMOKE PASSED on stack 5.2 - the managed bridge loads and the native facade lives in-process under net10.`
No `5.0.2|` anywhere, so the deployed binary is the 5.2 one.

**GATE 1 ResetVrf --dry-run 4367: DID NOT COMPLETE, exit 124 (the seat's 90 s `timeout`). It is a JOIN HANG, not a slow
discovery, and the cause is the SHELL ENVIRONMENT, not the tool.** appNumber 4367 is BURNED.

No trace of federate 4367 exists anywhere in the run's files - not in `watchvrf-trace.csv` (which carries only CON and
POS rows plus its own join/resign banner), not in `vrfc2simapp.log` (its only "4367" matches are console rows whose SIM
TIME happens to be 4367.xx seconds), not in `c2sim-bus.log` (VR-Forces joins are not C2SIM traffic) and not in the
runner log. That absence is expected and is not itself evidence either way. What IS evidence is the tool's own stdout,
read against `tools/ResetVrf/Program.cs`:

- Program.cs prints `[..] bridge.Start() - joining the federation...` (line 143), then on success
  `[OK] joined (BackendCount={n}).` (line 151), and only THEN begins discovery. **The `[OK] joined` line never
  printed**, so the tool was inside `bridge.Start()` when the timeout cut it.
- **Its own discovery budget could not have been the thing that ran long.** The loop at lines 157-177 is capped at
  **20 s hard**, breaks at **8 s** if the federation is empty, and settles after about 4 s once a count holds steady.
  90 s is 4.5x that budget. Had the join succeeded, the whole tool - join, discover, `[DRY-RUN] would delete N`,
  resign - would have finished inside about 30 s. The control is in this same run: `PauseSim` joined the SAME
  federation 3 minutes earlier and 1 minute later and printed `[OK] joined` about 2 s after `bridge.Start()`.
- **Three markers in the ResetVrf stdout say the 5.2 launch environment documented at Program.cs lines 24-31 (and in
  the V6 block's own ENVIRONMENT preamble) was not applied to that shell**, and each has `PauseSim` in the same run as
  its control:
  1. `Unable to load configuration file: ..\appData\settings\connections\MAK-ONE-2025-Config.xml` - the cwd was not
     `C:\MAK\vrforces5.2d\bin64`, so the connection config that OWNS the 5.2 federation identity (execName
     MAK-ONE-2025, FOM modules) never loaded. `PauseSim` printed no such line, and `VrfC2SimApp` logged
     `ConnectionConfigFile='C:\MAK\vrforces5.2d\appData\settings\connections\MAK-ONE-2025-Config.xml'`.
  2. `Attempt to create and connect to Assistant` / `Connected to RTI Assistant.` - `RTI_ASSISTANT_DISABLE` was not
     set. No other federate in this run went near an assistant.
  3. `Loading Config File: C:\MAK\makRti5.0.1\rid.mtl` - the DEFAULT rid, not `config\rid-501-rtiexec-min.mtl`.
     `PauseSim`, `VrfC2SimApp` and `WatchVrf` all loaded the repo rid. Peers must SHARE the rid, and the rtiexec
     posture (rtiexec mode + interface 127.0.0.1) is what this federation runs on.

  The output stops immediately after marker 3, BEFORE the `RTI_extend13and1516interop` / FOM-sorting lines that every
  successful join in this run printed next - i.e. it blocked in RTI connect/create/join, which is exactly what a
  federate on the wrong rid and a wrong-posture assistant does.

**FINDING (not a fix): gate 1 is UNSCORED, and `ResetVrf` is neither passed nor failed by it.** A gate that was not run
is not a pass. Two cheap things would settle it: re-run gate 1 with the five env vars and the `Push-Location` from the
V6 block's preamble actually applied in that shell; and - separately, as a real improvement - have `ResetVrf` refuse to
call `bridge.Start()` at all when `RTI_RID_FILE`, `RTI_ASSISTANT_DISABLE` and the cwd do not match the bound stack, so
a posture error fails in one line instead of hanging for 90 s. `Program.cs` already documents that environment in a
comment; nothing enforces it. The same hole is in every bridge consumer.

### R9. CRASH, TICK PHASE, DEPLOY, TEARDOWN

- `MissingMethodException`: **0**. `Tick phase '<name>' FAILED`: **0**. `SIM CLOCK:` lines: **0** (so no
  `stepped BACKWARDS`). No crash, no dump prompt, no `AnswerCrashDumpDialog` invocation.
- App-log warnings, all six: three init-time name warnings (NAME COLLISION RISK / NAME PRE-FLIGHT for `BANDIT_II` vs
  `BANDIT_III`, and the `2/1_AD/25_` DIS-marking truncation), the two HELD lines, and one
  `fail: C2SIM.C2SIMSDK[0] STOMP block reading cancelled` as the very last line of the log - that is the resign, after
  `Cleanup: 164 deletes dispatched` and `Reports this run: 15977 delivered, 0 FAILED`.
- Teardown, every stage exit 0: StopIface 0 (server driven RUNNING -> INITIALIZED -> UNINITIALIZED), VrfC2SimApp exited
  **0 (clean resign)**, WatchVrf 0, ListenReports 0, StopVrf52 0 with
  `[OK] VR-Forces 5.2d is down (graceful; nothing was killed)` and
  `[OK] RTI infrastructure preserved (correct): rtiexec(pid 69856), rtiForwarder(pid 50520)`. The detached watchdog
  confirmed the runner GONE twice 2 s apart, found `runner.teardown-ran`, and stood down without touching anything.
  Window: 1201.9 s used of 1200.
- **The killed ResetVrf federate left nothing visible.** The teardown inventory lists only `vrfSimHLA1516e`, `rtiexec`
  and `rtiForwarder`; `no VR-Forces processes remain` and `no WatchVrf / ListenReports observer remains` both fired.
  That is consistent with a federate that never completed its join, but note the limit: **this run carries no
  instrument that would SEE a stale federate**, so "left nothing visible" is the honest claim, not "left nothing".
  `rtiexec`'s own federate list is where that would be checked, and it was not checked.
- The pin (sec 9 `E3F405...4702`) is NOT recorded in `run-manifest.json`, which hashes each exe separately; all six
  bridge-linked tools in the manifest carry `productVersion 1.0.0+5881b7d1227065b07383fecd5c9e31e651008ae5`. The
  positive evidence that the STP-809 members are deployed is behavioural: `PauseSim` printed
  `control-state read-back = AVAILABLE (VrfBridge.BackendControlState, STP-809)`, the HELD lines carry the STP-809
  clause with `active=`, and no partial-deploy warning fired anywhere.

### R10. ADVERSARIAL REVIEW (HEAVY)

**Strongest competing reading: "the HOLD proves nothing - the clock was flat and the task simply had not accumulated
enough sim time to finish yet, pause or no pause. Nothing was actually held; the run just ran short."**

It is refuted, and by the completion rather than by the HOLD lines. T1 DID complete, inside the window, 408.9 s after
stage-8b `t=0` - so the run did NOT run short and the end time was genuinely reached. The question is then only WHICH
clock the 181 s of pause were charged to, and that is a 17.8 s discrimination the data makes (R5 item 4): had the
interface served wall seconds once the clock went stale at 60 s - the pre-Q5 behaviour, the thing Q5 changed - the
completion would have arrived at 02:22:50.9Z, and it arrived at 02:23:08.7Z. The run's own completion overshoot is
3.9 s of wall, so the gap is over 4x the noise. A second, independent refutation needs no model at all: the log line
says `1828 s of a 1800 s Duration served on the simulation clock`, and the sim clock is measured - by the vendor's own
console stamps, a channel the interface does not write - to have advanced **0.84 s** across the 181.5 s hold. There is
no reading of those two facts in which the hold aged the task.

**Second competing reading: "the HELD line fired off the BackendCount fallback, and a fallback hold looks identical to
a confirmed pause."** Refuted by the text: both lines read `the back end REPORTS PAUSED (DtPauseControlType;
BackendCount=1, active=1)` and neither carries the `CAVEAT: this rests on the back-end COUNT` sentence, which
`VrfC2SimService` appends whenever `control != Paused`. `PauseSim` independently read `controlBefore=Running
controlAfter=Paused` through a SEPARATE federate. The positive control-state read is present on both channels.

**Third competing reading: "the pause was never real - `PauseSim` only reported what it had just commanded."** Refuted
by the console channel: 181.5 s with not one object-console row, on a run producing hundreds of rows a second before
and after. The back end stopped stepping; it did not merely report that it had.

**THE SYMPTOM THIS RUN DOES NOT FULLY EXPLAIN, stated rather than footnoted.** `PauseSim`'s post-resume readings
(1167.888 -> 1170.794 -> 1172.846 over about 6.6 s) are inconsistent with the console's (1167.9 -> 1205.1 over the same
interval). One of the two instruments is wrong about the rate. F-1 records the measurement - the control federate's
cached `simTime()` lags and the lag GROWS after a resume, reaching 32.3 sim s - and names the likely mechanism (the
cached scenario clock refreshes on back-end status messages and interpolates near 1x between them), but that mechanism
is INFERRED from the numbers, NOT read out of the vendor headers, and it is not settled here. It does not touch any
verdict: every pause-half predicate rests either on a control-state read or on a zero / non-zero clock delta, and the
completion arithmetic uses one reader consistently end to end. It DOES mean any future prereg that predicts a
`clockDelta` MAGNITUDE is predicting an artefact.

**What this run cannot settle.** Everything in sec 4 and sec 5 (the whole kill half and the deactivated-back-end
probe); sec 6's teardown-against-a-dead-sim question; whether a stale federate was left by the timed-out `ResetVrf`;
and - new - whether the `simTime()` lag of F-1 is a status-message cadence or something else.

### R11. VERIFIED vs ASSUMED

**VERIFIED** (read this pass, from the files named at the top): every quoted log line and its count; the two bus stamps
and that they are the only TaskStatus reports in 15,977; both `[RESULT]` lines and the manifest's `probes.pauseResume`
block (`pauseFired` / `resumeFired` true, `firedAtTPlusSec` 122 / 303, `firedUtc` 02:18:21.439Z / 02:21:23.055Z, both
appNumbers consumed, exit 0); the 181.5 s console silence and the 0.84 s of sim across it; the ratios (7.30x pre-pause,
6.80x post-resume, 7.13x served / running); 1825.5 sim s between the two bus reports against 1828 s logged; the
`ResetVrf` stdout, its exit 124, and `ResetVrf/Program.cs` lines 143-177; every teardown exit code; the zero counts for
MissingMethod / Tick-phase / SIM CLOCK.

**DERIVED** (arithmetic over verified numbers, stated so it can be rechecked): the trace's wall origin, estimated at
**02:15:17.5Z +/- about 1 s** by aligning the runner's stage-8b poll lines and the console freeze against `PauseSim`'s
UTC stamps - it is used only to place sim readings at named UTC instants, and every duration above (181.5 s, 438.0 s,
256.5 s, 1825.5 s) is a difference that does not depend on it; the three counterfactual completion instants of R5; the
7.27x armed-at-the-kill bound of C-T3.

**ASSUMED** (not re-verified here): that the deployed `bin\Release-5.2` trees are the G-A RERUN pin - the manifest does
not record that hash, so R9's behavioural evidence is what stands in for it; and that `reports-captured.log` stamps and
the runner / manifest UTC stamps share one clock (they agree to under a second wherever both exist).

### R12. WHAT A KILL-HALF RE-RUN MUST CHANGE

1. **Anchor the kill offset on the DISPATCH, not on stage-8b `t=0`** (C-T1). At `DurationScale=0.25` and the measured
   7.1-7.3x the armed-at-the-kill ceiling was 7.27x, not 8.26x, and T1 beat the nominal kill instant by 9 s. Either
   kill at dispatch+300 s, or raise `DurationScale` to 0.35 (2,520 task s), which moves the ceiling to about 10x.
2. **Get the kill authorised before the window opens.** The classifier refusal, not the procedure, is what cost this
   run its second half.
3. **Re-run V6 gate 1 with the environment actually applied** (R8), and consider making the bridge consumers refuse to
   join on a mismatched posture instead of hanging.
4. Keep `--no-stop-when-complete`: with the completion at t+409 the stop rule would have closed the window at about
   t+469 and taken the kill with it, exactly as C3 predicted.
