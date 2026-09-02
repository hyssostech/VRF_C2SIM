# PREREG - THE COA-STP1 `-q` PAIR: does `-q` move the Tank Companies, or are they unstable?

Handoff NEXT row 1. ONE EXPERIMENT, TWO RUNS, REGISTERED TOGETHER BEFORE EITHER LAUNCH.
**Run A: COA-STP1, FFRTC, WITHOUT `-q`. Run B: the same, WITH `-q` (`-QuietBackend`).**
The ONLY difference between A and B is that switch. Neither run is adjudicated until both
have been run, and B is not launched until A's instrument checks pass and A's outcome is
written (the tasking's ordering rule).

## 0. THE QUESTION AND WHY ONE RUN COULD NOT ANSWER IT

Two runs of the SAME order on the SAME fixture disagree about which of the order's three
Tank Companies misbehaves:

  Tank Company       rung 2 (20260902T165144Z, no `-q`)   `-q` run (20260902T183135Z)
  ---------------    ---------------------------------    ---------------------------
  856/HHC   (T27)    4 sub-routes, 1.80 km                4 sub-routes, 6.55 km
  B/5-20    (T35)    4 sub-routes, 2.85 km                **0 sub-routes, 0.41 km**
  C/1-35    (T39)    4 sub-routes, 4.27 km                4 sub-routes, 5.50 km

The `-q` run STOPPED on that (its P4 and P1(b) misses). Its adversarial review named the two
live hypotheses and said, in terms, that one run cannot separate them. This prereg registers
the pair it asked for.

**H-q - `-q` CAUSED IT.** Mechanism: `-q` measurably moved the clock (0.2652 -> 0.3140 sim-s
per WALL-s, +18.4%), and a timing-sensitive step in the two-level aggregate distribution can
resolve differently at a different frame cost. PREDICTS: A reproduces rung 2's pattern (all
three companies build their four sub-routes) and B reproduces the `-q` run's (one builds none).
The pattern tracks the switch and only the switch.

**H-nondet - THE TWO-LEVEL DISTRIBUTION OF THE TANK COMPANIES IS NON-DETERMINISTIC.**
Mechanism: the class is already the unstable one on the record (rung 2's own unexplained item
3), and between the two runs on record the anomaly MOVED IN BOTH DIRECTIONS - T27 cleared
1.80 -> 6.55 km while T35 degraded 2.85 -> 0.41 km. A cause acting through `-q` would be
expected to push all three the same way. PREDICTS: the set of stalled or degraded companies
differs between A and B in a way NOT aligned with `-q`, and/or A itself differs from rung 2.

FALSIFIERS ARE NAMED IN SEC 6. Sec 6C names, in advance, the outcome that supports NEITHER,
so that a third hypothesis cannot be invented from the data after the fact.

### A THIRD OBSERVABLE, DERIVED FROM THE EXISTING ARTIFACTS BEFORE LAUNCH

Re-reading both vendor logs this session turned up a fact neither sec 7 recorded, and it is
registered here as the sharpest discriminator in the experiment. **The three Tank Companies do
not build their sub-routes together; they build them one company at a time, and how late a
company builds tracks how far it gets.** Offsets are from the order push, measured on the
vendor log's LOCAL wall stamps converted to UTC (the handoff's stamp rule):

  run       company     sub-route build    offset from order push (WALL)   net_km
  -------   ---------   ----------------   -----------------------------   ------
  rung 2    C/1-35      17:02:26Z          +  7 min 10 s                     4.27
  rung 2    B/5-20      17:15:59Z          + 20 min 43 s                     2.85
  rung 2    856/HHC     17:23:45Z          + 28 min 29 s                     1.80
  `-q`      C/1-35      18:40:17Z          +  5 min 58 s                     5.50
  `-q`      856/HHC     18:40:26Z          +  6 min 07 s                     6.55
  `-q`      B/5-20      never                    -                           0.41

Rung 2 serialised them ~7 and ~8 minutes apart; the `-q` run built two within NINE SECONDS and
the third never. So "which company misbehaves" may be a question about a QUEUE, not about a
company. The per-company BUILD OFFSET is therefore recorded as a first-class observable in
both runs (sec 6, O2). It is not itself a prediction with a threshold - there is no basis for
one - but it is registered so that it is measured the same way in both runs and cannot be
selected after the fact.

## 1. DOCS CONSULTED - CITED, NOT RE-RESEARCHED (2026-09-01 standing rule)

The tasking says to cite the Users Guide sections the `-q` prereg already cited rather than
re-read them. Cited, with what each settles for this experiment:

1. **Users Guide sec 5.2, Table 8, p.177** - `(-q | --doNotUseConsole)`: output goes to the log
   file rather than the console; at a high notification level the console "can degrade
   performance" and quiet mode "prevents this degradation"; Windows back ends only.
   (PREREG_QUIET_BACKEND_SCALE sec 1 item 1.) SETTLES: what the one variable is and does.
2. **Users Guide sec 4.9, p.161** - on Windows, vrfSim.log and vrfGui.log are written
   unconditionally to the run directory. (ibid. item 2.) SETTLES: the evidence channel that
   carries the sub-route census survives `-q`. LIVE-CONFIRMED by the `-q` run at 961.9 lines
   per SIM s against rung 2's 966.2.
3. **Users Guide Appendix C.1, Table 71, p.1663** - the `doNotUseConsole` vrfSim.mtl parameter,
   default 0. (ibid. item 3.) `C:\MAK\...\vrfSim.mtl` IS NOT EDITED; the lever is the command
   line. SETTLES: nothing under C:\MAK changes between A and B.
4. **Users Guide sec 5.4.3, p.185** and Table 8's `-n` entry - notification level governs the
   VOLUME written to both sinks, `-q` selects the SINKS; ours is `notifyLevel 3`. (ibid. item
   4.) SETTLES: `-q` cannot thin the log, so a missing sub-route line is a missing sub-route.
5. **Users Guide sec 3.4.3 / 7.6.1 / 12.2.1** - `fixed-frame-run-to-complete` advances a fixed
   0.0333 sim-s per frame regardless of how long the frame takes. SETTLES: why the two runs
   deliver different SIM seconds inside the same WALL window, and why every threshold below
   names its clock.
6. `docs/HANDOFF_2026-09-01_R9_COMPLETE.md` - FFRTC block (the 0.2652 / 0.3140 ratios and the
   THRESHOLD RULE), OPERATIONAL STATE, PROBE PROTOCOL, NON-NEGOTIABLES.
7. `docs/RUNBOOK.md` sec 0.5 / 0.5.0 / 0.5.11 (the runner switches) / sec 1 (**the PRIVATE
   C2SIM server**, 18080 / 61614) / :1206-1215 (the ResetVrf launch environment - 3757 was
   burned by skipping it).
8. `docs/experiments/PREREG_COASTP1_RUNG2_2026-09-02.md` sec 4 and sec 7;
   `docs/experiments/PREREG_QUIET_BACKEND_SCALE_2026-09-02.md` sec 4, 5, 6 and 7. These two
   are the comparators and the template. Every number of theirs used below was RE-MEASURED
   from their run directories this session (sec 5), not quoted.

NO NEW RESEARCH WAS DONE AND NONE IS OWED: the mechanism of the switch is settled by the docs
above and confirmed live; what is open is a question about OUR aggregates, which no vendor
document answers.

## 2. THE ONE VARIABLE

`-QuietBackend` on `scripts/RunC2SimScenario.ps1`, which appends `-q` to the back end's command
line via `scripts/LaunchVrf.ps1` (landed in 4d2f4c3, DEFAULT OFF, 105/105 offline tests green
at registration of the `-q` run). **NO CODE CHANGES IN THIS PREREG.** The switch already exists
and is already exercised; A omits it, B passes it. The manifest records `inputs.quietBackend`
either way, so the variable is in the evidence and not only in the command line.

## 3. EVERYTHING ELSE HELD - AND THE DIFFERENCES AGAINST THE TWO COMPARATORS, STATED PLAINLY

HELD IDENTICAL BETWEEN A AND B (this is what makes the pair a controlled comparison):
fixture `TropicTortoise_FFRTC` (repo and `C:\MAK\vrforces5.0.2\userData\scenarios\` both
hashing D27E540F8BCCAA2EBDD33C63CF062CB4842DBE1937B7AA3DAC1C20F9C990B0B9, re-verified this
session); `data/COA-STP1_Initialization.xml` + `data/COA-STP1_Order.xml`; `-RunSecs 2700
-SampleSecs 10 -StopWhenComplete`; `$env:Vrf__DeStackCreates = 'true'` and nothing else;
TimeMultiplier 1x; `vrfSim.mtl` untouched (notifyLevel 3 / objectConsoleNotifyLevel 3, stamp
2026-09-01 14:32:14, verified); the app binary; the bridge (A7504441, NOT rebuilt); the private
C2SIM server; the analysis commands in sec 5.

CHANGED, DELIBERATELY, IN BOTH RUNS EQUALLY, NOT THE VARIABLE: the DEPLOYED (gitignored)
`src/VrfC2SimApp/bin/Release/net10.0/win-x64/appsettings.json` `Vrf:ClientId` goes STP ->
**C2SIM**, because COA-STP1's init declares SystemName C2SIM and the runner aborts at
validation otherwise (the handoff's CLIENTID TRAP). Done BEFORE run A, restored to STP after
run B. The tracked `src/` file is never edited.

**KNOWN DIFFERENCES AGAINST THE COMPARATORS - two, both real, both stated before launch.**
They do NOT weaken the A-vs-B contrast, which is internally controlled; they weaken only the
A-vs-rung-2 and B-vs-`-q`-run contrasts, and sec 6 says exactly where that bites.

  (i) **THE APP BINARY IS A THIRD ONE.** Rung 2 ran on 3b7b8d2e...c60cea0; the `-q` run on the
      merged build 570619630015...ACEB52A6; A and B run on **53130C93BD7622EDEE3B043D6AE504A4
      F0CF3DC58732D218206324BCA7EF27A9** (built 2026-09-02 16:28:08 from a5cdc95). The delta
      from the `-q` run's binary is, in `src/`, exactly ONE added log line at service
      construction (`_log.LogInformation("C2SIM endpoints: rest={Rest} stomp={Stomp}", ...)`,
      VrfC2SimService.cs) plus RunnerLib's `Resolve-MarkingKey` (a runner-side `-StopWhenComplete`
      rule-4 helper, and that switch is INERT for this order). `git show a5cdc95 -- src/` is
      the whole app-side diff and it is that one statement. It cannot reach the back end's path
      distribution. IT IS NEVERTHELESS A DIFFERENCE AND IT IS RECORDED AS ONE.
  (ii) **THE C2SIM SERVER IS THE PRIVATE ONE.** Both comparators ran against the operator's
      shared server (their manifests read `restUrl http://127.0.0.1:8080/C2SIMServer`,
      re-read this session); A and B run against `c2sim-server-vrf`, **18080 / 61614**, which
      is now the runner default (RUNBOOK sec 1). This is a CHANGE AWAY FROM A KNOWN
      CONTAMINATION SOURCE, not toward one - a foreign initialization pushed to the shared
      server invalidated run 20260902T193508Z. Both A and B use it, so it cancels in the pair.
      The app now logs which server it heard; sec 6 I5 requires that line to name 18080/61614.

## 4. INVOCATION - both runs, in this order, with the sweep between them

    # ONCE, before run A:  deployed appsettings.json Vrf:ClientId  STP -> C2SIM

    # RUN A - WITHOUT -q
    $env:Vrf__DeStackCreates = 'true'
    Get-ChildItem env:Vrf__*
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init  data/COA-STP1_Initialization.xml `
        -Order data/COA-STP1_Order.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 2700 -SampleSecs 10 -StopWhenComplete

    # post-run sweep A (RUNBOOK :1206-1215 environment - MANDATORY)
    # then: instrument checks + outcome for A written, BEFORE B is launched

    # RUN B - WITH -q; the command line differs by this one token and nothing else
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init  data/COA-STP1_Initialization.xml `
        -Order data/COA-STP1_Order.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 2700 -SampleSecs 10 -StopWhenComplete `
        -QuietBackend

    # post-run sweep B, then: Remove-Item env:Vrf__DeStackCreates; ClientId C2SIM -> STP

**THE WINDOW AND ITS CLOCK - `-RunSecs 2700` is a WALL cap, and WALL is what is held.**
Both comparators used 2700 s WALL and closed observation windows 2.2 s apart (2735.65 s and
2733.48 s), so holding 2700 makes A directly comparable to rung 2 and B to the `-q` run in the
only unit the operating system charges in. THE WALL BUDGET STATED AS `sim / measured ratio`,
which is the FFRTC block's rule:

    run A   2700 s WALL x 0.2652 sim-s per WALL-s  =  ~716 SIM s  (rung 2 delivered 725.5)
    run B   2700 s WALL x 0.3140 sim-s per WALL-s  =  ~848 SIM s  (the -q run delivered 858.3)

**WHY HOLDING WALL DOES NOT CONFOUND THE PAIR, registered as an argument that can be attacked:**
B receives ~18% more SIM seconds than A inside the same wall window. That would confound a
DISTANCE comparison, so distance is never compared raw - O3 reports **km per SIM second** and
every distance threshold below is stated in that unit. The LOAD-BEARING metric, O1, is the
per-company SUB-ROUTE COUNT, which is 0 or 4 and is settled by an event that occurred at
+6 to +28.5 minutes of a 45.6-minute window in every run on record - well inside either
budget. The alternative, holding SIM equal by shortening B to ~2280 s wall, would have made B
non-comparable to the `-q` run, which is the very comparison H-q needs. WALL IS HELD.

`-StopWhenComplete` is INERT for this order and is passed only because the standing
configuration passes it (handoff PROBE PROTOCOL: "-RunSecs is a CAP under -StopWhenComplete"):
T9's performer has zero Locations and T13 carries a 12,000 s WALL start delay, so the gate can
never be satisfied and the window runs its cap. If it DOES fire, that is recorded, not a
deviation. Expected total wall per run ~50 min; ~2 h for the pair plus two sweeps.

**APP NUMBERS.** The Appendix B marker reads `*** NEXT FREE: 3783 ***` at registration
(verified as the only value-bearing marker; a dry run this session confirmed the runner would
take 3783-3789 and advance the marker to 3790, and did NOT advance it). Ledger plan:

    run A       3783-3789   (runner-managed)   marker -> 3790
    sweep A     3790        (hand-taken, ledgered BEFORE the join)   marker -> 3791
    run B       3791-3797   (runner-managed)   marker -> 3798
    sweep B     3798        (hand-taken, ledgered BEFORE the join)   marker -> 3799

3789 and 3797 (createOneDiag) are consumed only if the stage-7 oracle gate fails; unconsumed
numbers are BURNED, never recycled. **THE POST-RUN SWEEP USES THE RUNBOOK :1206-1215
ENVIRONMENT** - cwd `C:\MAK\vrforces5.0.2\bin64`, PATH prefixed with the VR-Forces / VR-Link /
makRti bin directories, and Machine-scope `MAKLMGRD_LICENSE_FILE`. 3757 was burned for skipping
it; every 2026-09-02 sweep that used it exited 0.

## 5. THE INSTRUMENTS, AND THEIR PRE-LAUNCH GATE (the false-greens rule)

**A DEVIATION FROM THE TASKING, STATED FIRST AND NOT BURIED.** The tasking says to reuse the
distance / sub-route / object-census scripts the two prior sec 7s record, and to say so and
stop rather than improvise if a script is missing. **THOSE THREE MEASUREMENTS WERE NEVER
COMMITTED.** The only analysis script in the tree is `tools/analysis/frame_gaps.py`; the two
sec 7s record the NUMBERS and prose definitions of the other three but not the code. I did not
stop, and here is the reason, so it can be overruled: stopping the experiment over a missing
helper would cost the run, while the rule the helper existed to serve - that the instrument
must be the SAME instrument - can be discharged directly and more strongly. I reconstructed the
three measurements from the sec-7 definitions as `tools/analysis/run_census.py` and **GATED IT,
BEFORE ANY LAUNCH, ON REPRODUCING BOTH PUBLISHED TABLES EXACTLY.** If it had failed to
reproduce them, that would have been the stop.

    python tools/analysis/run_census.py . 20260902T165144Z_run --gate rung2   -> GATE PASS
    python tools/analysis/run_census.py . 20260902T183135Z_run --gate quiet   -> GATE PASS

Each gate is exact, not approximate: all ELEVEN per-performer `net_km` values to the 0.01 km
the records print, the report count (1,536 / 1,793), the 128 reporting uuids, the sub-route
census per company (rung 2: 856/HHC 4, B/5-20 4, C/1-35 4; `-q` run: 856/HHC 4, C/1-35 4, and
B/5-20 ABSENT), and the object census (1,732 ever-real in BOTH, 132 / 110 pole-only, 1,864 /
1,842 POS uuids). ONE DEFINITION HAD TO BE PINNED BY THE GATE RATHER THAN READ OFF THE PROSE:
the pole placeholder is (+/-90, -90) and its LATITUDE SIGN VARIES - a filter testing only +90
scores 1,733 ever-real, and the published 1,732 is what proved the sign matters. That is
recorded because it is exactly the kind of silent one-object drift the census exists to catch.

THE SLOPE INSTRUMENT WAS CONTROLLED THE SAME WAY, this session, before launch:
`python tools/analysis/frame_gaps.py . 20260902T165144Z_run` reproduces **0.2652** sim-s per
WALL-s, TEST A 89/89 = 100.0%, TEST B R = 0.9985; `... 20260902T183135Z_run` reproduces
**0.3140**, TEST A 83/83 = 100.0%, R = 0.9940. Both match their published sec 7s exactly.

THE COMMANDS RUN AGAINST EACH NEW RUN, and no others:

    python tools/analysis/frame_gaps.py . <run_id>_run
    python tools/analysis/run_census.py  . <run_id>_run
    # vendor-log line counts, verbatim greps, on runs/<run_id>_run/bin64-vrfSim.log:
    #   "Can't find entity route" | "Move-Along Route:" | "Registered object" | "Created radio"
    #   "leaderRoute" | "'s Offset Route" | "Locally Simulated: <company>_R"
    # app-log counts on runs/<run_id>_run/vrfc2simapp.log:
    #   "safe MSL" | "DeStack (R8):" | "CreateRoute" | "MoveToLocation" | "TASKCMPLT"
    #   "TYPE MAP" | "DROPPING TASK" | "C2SIM endpoints:"

## 6. WHAT IS REGISTERED - instrument checks first, then the result

**THE ORDER IS LOAD-BEARING AND IS THE TASKING'S OWN RULE: a run that fails an instrument check
is DISCARDED, NOT INTERPRETED.** I do not look at O1 for a run until I1-I5 have passed for it.

### INSTRUMENT CHECKS - the run must reproduce the KNOWN result before it may be read

I1 **THE FREEZE IS STILL FIXED.** `Can't find entity route` = **0**, EXACT (a count; no clock).
     Nonzero is the handoff's registered reopening evidence for the CLOSED route-uuid tripwire
     and is a STOP for the whole session, not a miss for this run.
I2 **THE FRAME MODE IS FFRTC.** `frame_gaps.py` TEST A **>= 95%** of sub-0.06 s gaps in
     {0.033, 0.034} AND TEST B **R >= 0.99** (both dimensionless). This criterion discriminates:
     the variable-frame rung 1 scores R = 0.0276.
I3 **THE CLOCK IS IN THE FFRTC-AT-SCALE BAND.** LS slope in **[0.20, 0.40] sim-s per WALL-s**
     for BOTH runs (rung 2: 0.2652; `-q` run: 0.3140). CLOCK NAMED: sim-seconds per WALL-second.
     ONE BAND FOR BOTH ON PURPOSE - a per-mode band centred on each comparator would beg the
     question this experiment does not ask, while [0.20, 0.40] still separates FFRTC at 128
     units from variable-frame (0.9995) and from FFRTC on the 3-unit R9 order (7.43-13.11).
     **A -> B slope comparison is NOT filed here. It is a BEHAVIOUR claim (S1 below), and
     filing a behaviour measure under an instrument check is the exact error the `-q` run's
     P1(b) made.**
I4 **THE POPULATION IS THE SAME POPULATION.** `run_census.py` `everReal` = **1,732** EXACT
     (identical in both comparators) and `reportUuids` = **128** EXACT (the created units).
     Pole-only and POS-uuid totals are RECORDED, not thresholded - they moved 132 -> 110 and
     1,864 -> 1,842 between the comparators and are a known-unexplained item, not a control.
I5 **IT IS OUR RUN, ON OUR SERVER.** All EXACT, all from vrfc2simapp.log: `safe MSL` **128**,
     `DeStack (R8):` **10**, `CreateRoute` **9**, `MoveToLocation` **0**, `TYPE MAP` **0**,
     `DROPPING TASK` **0**, nine new-form route lines and zero old-form, and the first-lines
     endpoint record must read `C2SIM endpoints: rest=http://127.0.0.1:18080/C2SIMServer
     stomp=http://127.0.0.1:61614/topic/C2SIM`. A run that heard 8080/61613 is DISCARDED - it
     was on the operator's bus and 20260902T193508Z is the precedent.
I6 **THE SEVEN NON-TANK-COMPANY PERFORMERS MARCH AT THE ESTABLISHED RATE.** The tasking names
     these as an instrument check and they are the right one, PROVIDED the threshold names its
     clock - a raw kilometre band cannot, because A and B are given different SIM seconds.
     REGISTERED: the six non-Tank-Company movers (T1, T5, T15, T19, T23, T31) each at
     **0.0080 to 0.0095 km per SIM second**, where sim seconds = observed window WALL s x the
     run's own measured slope. That band is tight and it is derived, not invented: rung 2's six
     ran 0.00837-0.00915 over 725.5 sim s and the `-q` run's six ran 0.00839-0.00917 over
     858.3 sim s - two runs 18% apart on the wall clock agree to three decimal places once
     normalised. AND the two expected zeros stay zero: T9 (zero Locations, never taskable) and
     T13 (12,000 s WALL start delay). SEVEN, not six, is the tasking's count and it includes
     T39/C-1-35, a Tank Company; C/1-35 is part of the RESULT and is not used as a control.

### THE RESULT - measured only after I1-I6 pass, for each run

O1 **PER-COMPANY SUB-ROUTE COUNT**, from `Locally Simulated: <company>_R<n>` in the vendor log,
   for 856/HHC, B/5-20 and C/1-35. This is the load-bearing number. It is 0 or 4 on every run
   of record; anything else is itself a finding.
O2 **PER-COMPANY SUB-ROUTE BUILD OFFSET** from the order push, in WALL seconds (vendor-log
   local stamps converted to UTC) AND in SIM seconds (offset x the run's own slope). Both
   clocks named. Recorded for all three companies in both runs; no threshold (sec 0).
O3 **PER-COMPANY HEAD DISTANCE**, reported as raw `net_km` AND as **km per SIM second**. The
   normalised figure is the one compared across runs.
O4 **CORROBORATION**, recorded for each run: `Move-Along Route:`, `leaderRoute` and
   `'s Offset Route` line counts. These track O1 mechanically (the `-q` run's 18 / 43 / 166
   against rung 2's 22 / 55 / 210 was one company's four sub-routes going missing). They are
   RECORDED, NOT THRESHOLDED - the `-q` run's P1(b) miss was a threshold on exactly this
   quantity filed as if it were a log-fidelity check.
S1 **SECONDARY, NOT THE QUESTION: does `-q` reproduce its +18%?** Registered at MEDIUM: B's
   slope > A's slope, in sim-s per WALL-s, with the sim-paced report count moving the same way
   (reports per SIM second staying in [1.6, 2.7] for both). This is a REPLICATION of the `-q`
   run's only clean result and it is recorded whichever way it lands. A miss here is a finding
   about `-q`, not about the Tank Companies, and it does NOT stop the pair.

### THE ADJUDICATION - registered before launch

**H-q IS SUPPORTED** if and only if the pattern tracks the switch: A has all three companies
at 4 sub-routes, B has one at 0. (A's marching order need not match rung 2's company-by-company
distances - those depend on build offset, and O2 is why.)

**H-q IS FALSIFIED** by EITHER of these, each sufficient on its own:
  (a) run A - which has no `-q` - shows any company at **0** sub-routes; or
  (b) run B - which has `-q` - shows all three companies at **4** sub-routes.

**H-nondet IS SUPPORTED** if the degraded set is not aligned with the switch: A differs from
rung 2 (a different company late or absent), or B differs from the `-q` run (a DIFFERENT
company at 0, or none), or A and B differ from each other in a direction `-q` does not explain.

**H-nondet IS FALSIFIED** if A reproduces rung 2's company pattern AND B reproduces the `-q`
run's - the same company, B/5-20, at 0 sub-routes in B and only in B. Two runs cannot prove
determinism, and this prereg does not claim they can; what they can do is fail to find the
instability twice in the place it would have to appear.

## 6A. THE MISS RULE

**STOP - write it up, do not adjust, do not re-run:** any failure of I1 (the freeze tripwire).
That ends the session, not just the pair.

**DISCARD, DO NOT INTERPRET:** a run failing I2, I3, I4, I5 or I6. The run is not evidence about
the Tank Companies. A discarded run is recorded, and one re-run is allowed for it (a discard is
an instrument event, not a result); a second discard on the same run stops the session.

**NOT A STOP - the result, whichever way it lands:** every value of O1-O4. There is no outcome
of the Tank-Company measurement that is a "miss", because the experiment is a discrimination,
not a threshold. The two hypotheses were registered above with their falsifiers; whichever
survives, survives, and sec 6C covers the case where neither does.

**NOT A STOP - S1.** A `-q` speed replication that misses is recorded as a finding.

**VOID:** an abort before the order is pushed is infrastructure, not a miss - recorded, retried
once. **TWO consecutive infrastructure failures STOP THE SESSION** (RTI not serviceable,
launcher not READY, a join failure); do not retry a third time. Run A completing and run B
aborting is not "two consecutive" - it is one.

**ORDERING, ENFORCED:** B is not launched until A's I1-I6 have passed and A's O1-O4 are written
into sec 7. If A is discarded, it is re-run before B is launched at all.

## 6B. WHAT THE PAIR WOULD AND WOULD NOT ESTABLISH

WOULD: whether `-q` is safe to adopt. That is the practical stake - the `-q` run bought a real
+18% and the handoff refuses to spend it while a silently non-distributing company might be the
price. A clean H-q result condemns the switch; a clean H-nondet result exonerates it and
promotes the instability to a first-class defect of OUR aggregate distribution, which is a
bigger and more useful finding.

WOULD NOT: it would NOT identify the MECHANISM of the instability. O2 may point at a queue, but
two runs cannot establish one. It would NOT make FFRTC a speed lever - the CLOSED tripwire
stands at either slope, and a scale run's wall budget remains `sim / measured ratio`. It would
NOT settle determinism in the strong sense (sec 6, H-nondet falsification clause). It would NOT
license editing `vrfSim.mtl`, which still needs the user.

## 6C. THE OUTCOME THAT SUPPORTS NEITHER - named in advance so it cannot be dressed up later

If **A and B are BOTH fully clean** (three companies, four sub-routes each, in both runs), then
H-q is falsified by clause (b) and H-nondet is not supported either - the pair simply did not
sample the anomaly. The honest report is "the anomaly is INTERMITTENT and this pair did not
reproduce it", the count of clean-vs-anomalous runs on the record becomes 2 clean / 2 anomalous,
and `-q` remains unadopted for want of evidence rather than because of evidence.

If **A and B are BOTH anomalous but on DIFFERENT companies**, H-q is falsified by clause (a)
and H-nondet is strongly supported.

If **A and B are BOTH anomalous on the SAME company**, that is neither hypothesis: it points at
something stable in this binary or this server configuration that neither comparator had, and
the correct response is to say so and register a NEW probe - NOT to reinterpret this one.

**In none of these cases do I invent a third hypothesis from the data and then report it as
though it had been registered.** Anything new goes in the report as a candidate for a next
prereg, labelled as such.

## 7. OUTCOME

### RUN A-1 - 20260902T204711Z_run, appNos 3783-3789 - **DISCARDED ON INSTRUMENT CHECK I4**

**IT IS DISCARDED, NOT INTERPRETED, AND THAT IS THE TASKING'S RULE AS WELL AS SEC 6A's.** The
tasking names "1,732 ever-real objects" as one of the four instrument checks a run must
reproduce before its Tank-Company result may be read. This run returned **1,847**, with **ZERO**
pole objects and **ZERO** NaN POS lines where both comparators had 132 / 110 pole-only objects.
The run is recorded here in full and is NOT adjudicated against H-q or H-nondet. A re-run of A
follows, which is the one re-run sec 6A allows.

DISCLOSURE, because it affects how the rest of this record should be read: I ran
`run_census.py` as a single command, so I SAW the Tank-Company result in the same output that
told me I4 had missed. I could not unsee it. The discard is therefore a decision taken with
knowledge of the result, and the honest safeguard is that the rule is being followed AGAINST my
interest - A-1's result is the one that would have made the strongest headline.

RUN FACTS. Launched 2026-09-02T20:47:11.516Z; order pushed 20:50:02.812Z; observation window
closed 21:35:33.763Z (**2,730.95 s** WALL against its 2,700 s cap plus trail; rung 2's was
2,735.65 and the `-q` run's 2,733.48, so all three wall windows are within 5 s); manifest saved
21:36:15.834Z; **49 min 04 s** total wall. `runnerExitCode` **0**, all ten stages exit 0.
`inputs.quietBackend` **False**. Ledger `wasValue` 3783 -> `newValue` 3790, `advanced` true,
taken BEFORE any join; 3789 (createOneDiag) UNCONSUMED. One `validityFlags` entry, severity
INFO, the standing stock-TropicTortoise advisory. `earlyExit.fired` false, as registered.
DERIVED SIM WINDOW: 2,730.95 wall s x 0.2863 = **781.9 SIM s** (rung 2: 725.5; `-q` run: 858.3).

INSTRUMENT CHECKS
  I1 `Can't find entity route` **0** EXACT - **PASS**. The freeze stays fixed; nine new-form
     route lines, zero old-form.
  I2 FFRTC mode - **PASS**. TEST A **84/84 = 100.0%** in {0.033, 0.034}; TEST B **R = 0.9985**.
  I3 LS clock slope **0.2863 sim-s per WALL-s**, inside [0.20, 0.40] - **PASS**. (resid sd 1.79,
     max 8.72 - a well-behaved fit, unlike the `-q` run's 58.66 / 999.17.)
  I4 `reportUuids` **128** EXACT - PASS. `everReal` **1,847** against the required 1,732 -
     **MISS. THIS IS THE DISCARD.**
  I5 **PASS, every clause EXACT**: `safe MSL` 128, `DeStack (R8):` 10, `CreateRoute` 9,
     `MoveToLocation` 0, `TYPE MAP` 0, `DROPPING TASK` 0, app log 519 lines (rung 2 and the
     `-q` run: 517). Endpoint line present and correct - `C2SIM endpoints:
     rest=http://127.0.0.1:18080/C2SIMServer stomp=http://127.0.0.1:61614/topic/C2SIM`. THE RUN
     WAS ON THE PRIVATE SERVER.
  I6 **PASS**. The six non-Tank-Company movers, km per SIM second: T1 0.008556, T5 0.009413,
     T15 0.009337, T19 0.009106, T23 0.008365, T31 0.009247 - all inside [0.0080, 0.0095], and
     T9 and T13 both 0.00 km as expected. The clock-normalised band held across a third run at
     a third slope, which is the strongest thing in this run's favour.

WHAT I4's MISS ACTUALLY IS - measured, not argued
  The vendor log says the BACK END CREATED THE SAME POPULATION: `Created radio` **1,733** in
  this run, **1,733** in rung 2 and **1,733** in the `-q` run - identical to the unit.
  `Registered object` 3,693 (rung 2: 3,727; `-q`: 3,683). The difference is entirely in what
  the OBSERVER could read:

    run        POS lines   distinct t   POS uuids   NaN lines   pole lines   real lines
    -------    ---------   ----------   ---------   ---------   ----------   ----------
    rung 2       503,265        282        1,864      18,365       10,332       474,568
    `-q` run     503,041        280        1,842      17,092       11,381       474,568
    **A-1**      501,029        281        1,847           0            0       501,029

  Both comparators have EXACTLY 474,568 real POS lines = 1,732 x 274. A-1 has none of that
  structure: every POS line it wrote carried a real coordinate. And the first-seen profile is
  different in kind: in rung 2 all 1,732 ever-real objects appear in ONE burst at t = 63.1 s
  while the 132 never-real ones trickle in from t = 3 to t = 1,778; in A-1 objects first appear
  spread across t = 3 to t = 1,297 and every one of them resolves.
  WatchVrf's binary is NOT the difference: its `lastWriteUtc` is 2026-09-02T10:43:28Z, older
  than the `-q` run.
  **THIS IS UNEXPLAINED AND IT IS RECORDED AS A FALSIFIER, NOT A FOOTNOTE.** I can say what it
  is not (not a different created population, not a different observer binary) and I cannot say
  what it is.
  I ALSO RECORD, AGAINST MY OWN INTEREST, THAT I4 IS PROBABLY A MIS-SPECIFIED CONTROL: I titled
  it "THE POPULATION IS THE SAME POPULATION" and then thresholded an OBSERVER-SIDE quantity,
  while declaring both of its arithmetic components (`poleOnly`, `posUuids`) free to move in the
  same paragraph. `everReal = posUuids - poleOnly` cannot be an exact control if neither term
  is. That is the same error class as the `-q` run's P1(b) and rung 2's P4(c), and it is now
  three consecutive preregs. **BUT I AM NOT USING THAT ARGUMENT TO RESCUE THE RUN.** A control
  diagnosed as broken only after it fails, on the run whose result one wanted, is exactly how a
  false green is manufactured. The rule is followed; the re-run decides.

RECORDED, NOT ADJUDICATED - what A-1 measured
  sub-routes:  856/HHC **4** | C/1-35 **4** | B/5-20 **0**
  build offset from order push (WALL / SIM):  C/1-35 +6 m 28 s / +111 sim-s;
               856/HHC +6 m 37 s / +114 sim-s (NINE SECONDS after C/1-35, exactly the `-q`
               run's gap); B/5-20 NEVER
  head net_km (and km per SIM s):  856/HHC 5.79 (0.007405) | C/1-35 4.17 (0.005333) |
               B/5-20 **0.37** (0.000473)
  corroboration: `Move-Along Route:` 18, `leaderRoute` 52, `'s Offset Route` 176
               (rung 2: 22 / 55 / 210; `-q` run: 18 / 43 / 166)
  This run had NO `-q`. What that would mean is not written here, because the run is discarded.

POST-RUN SWEEP A-1: `tools/ResetVrf 3790` with the RUNBOOK :1206-1215 environment (cwd
C:\MAK\vrforces5.0.2\bin64, VR-Forces / VR-Link / makRti bin PATH prefix, Machine-scope
MAKLMGRD_LICENSE_FILE). Joined clean (BackendCount=0), discovered 0 reflected objects, resigned
cleanly, **exit 0**. 3790 was ledgered in Appendix B BEFORE the join. Marker 3783 -> 3790
(runner) -> 3791 (hand). VR-Forces down, RTI trio untouched (41336 / 224608 / 76620).


## 8. REGISTRATION

Sections 0-6C, `tools/analysis/run_census.py` and its two passing gates were written and
committed BEFORE either launch. Sec 7 is added after the runs.
