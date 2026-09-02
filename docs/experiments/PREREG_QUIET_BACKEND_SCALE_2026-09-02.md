# PREREG - `-q` (doNotUseConsole) AT SCALE (task 2 of 3, 2026-09-02)

ONE VARIABLE: **`-q | --doNotUseConsole` on the VR-Forces back end's command line**, added via a
new default-OFF runner switch. The comparator is COA-STP1 rung 2, run 20260902T165144Z, whose
configuration, fixture, order and window this run reproduces exactly.

THE QUESTION IS TWO QUESTIONS, and the first is answered BEFORE launch, from the vendor's own
documentation, because it is the one that could destroy the evidence:
  (1) DOES `-q` SUPPRESS `bin64/vrfSim.log`? **NO.** Sec 1 cites three independent places in the
      Users Guide. We depend on that file for every creation, task and route line, and P1 below
      is the live confirmation of the doc's claim, not a hope.
  (2) IS CONSOLE OUTPUT A MATERIAL PER-FRAME COST AT THIS SCALE? Unknown, and the point of the
      run. The docs make a positive performance claim for exactly our configuration, so the
      registered prediction is a SPEED-UP - see P2 and sec 6A for what each outcome means.

## 1. DOCS AND SOURCES CONSULTED (docs first, per the 2026-09-01 standing rule)

All read this session from the LOCAL install, quoted verbatim, with page numbers from
`C:\MAK\vrforces5.0.2\doc\VRFUsersGuide.pdf` (1848 pages) and the matching MadCap help topics.

1. **Users Guide sec 5.2, Table 8, p.177** (help: `Content/Introduction/CLI/
   vrf_vrfSimCommandLine.htm`) - the option itself:
     "(-q | --doNotUseConsole) Specifies that all vrfSim output go to the log file rather than
      the console (quiet mode). If you are using a high level of notification, sending output to
      the console can degrade performance. Running in quiet mode prevents this degradation of
      performance. This command only applies to back-ends running on Windows. To create a log
      file on Linux, redirect the output of vrfSim, for example: vrfSim > mylog.txt. For more
      details about log files, see 4.9. The VR-Forces Log Files on page 161."
   TWO THINGS, both load-bearing: output GOES TO THE LOG FILE (it is redirected, not dropped),
   and the vendor makes an explicit PERFORMANCE claim conditioned on "a high level of
   notification" - which is our configuration.
2. **Users Guide sec 4.9, p.161** (help: `Content/Introduction/Starting/vrf_logFiles.htm`):
     "On Windows, VR-Forces creates two log files, vrfSim.log and vrfGui.log. Both log files are
      written to the directory from which you run VR-Forces."
   UNCONDITIONAL on Windows. The log file's existence is not a function of `-q`. (The Linux
   paragraph, which is where redirection matters, does not apply - we are on Windows 11.)
3. **Users Guide Appendix C.1, Table 71, p.1663** - the vrfSim.mtl parameter of the same name:
     "doNotUseConsole - Disables (1) or enables (0) writing of output to the console. Default: 0."
   The parameter is defined PURELY in terms of the console. Our
   `C:\MAK\vrforces5.0.2\appData\settings\vrfSim\vrfSim.mtl:196-197` carries the vendor's own
   comment "Set this to 1 to not write output to the console" with `(setqb doNotUseConsole 0)`.
   **THAT FILE IS NOT EDITED** - editing C:\MAK settings needs USER approval we do not have. The
   lever is the command line, which is what the tasking specifies.
4. **Users Guide sec 5.4.3, p.185** - the notification levels: "0 - Only fatal messages...
   3 - Verbose information is printed. 4 - Debug information... The default notification level
   is 2." Our `vrfSim.mtl:205` is `(setqb notifyLevel 3)` - VERBOSE, one above the default and
   squarely inside Table 8's "high level of notification".
   Table 8's own `-n` entry (p.177) adds that the level governs "messages written to the console
   OR the VR-Forces log file", i.e. one level feeds both sinks; `-q` selects the sinks, `-n`
   selects the volume. They are orthogonal, so `-q` cannot thin the file.
5. **Users Guide sec 16.9.1, p.441** - `objectConsoleNotifyLevel` (ours is 3 at
   `vrfSim.mtl:208`) governs the per-object Information-dialog console, a GUI surface, "set
   individually for each object". It is NOT the process stdout that `-q` redirects. Recorded
   because the tasking named it: it is not a confound, and it is not changed.
6. **THE BINARY ITSELF**, checked this session because the doc's performance claim only bites if
   there IS a console: `C:\MAK\vrforces5.0.2\bin64\vrfSimHLA1516e.exe` has PE subsystem **3
   (CONSOLE)**, as does vrfLauncher.exe, and `scripts/LaunchVrf.ps1:402` starts the launcher with
   `Start-Process` WITHOUT `-NoNewWindow` or `-WindowStyle`, so Windows gives the console-subsystem
   process its own console window. Every one of rung 2's 700,975 log lines was therefore also
   written to a visible Windows console. That is the cost `-q` removes.
7. `docs/HANDOFF_2026-09-01_R9_COMPLETE.md` - FFRTC block (the clock finding and the THRESHOLD
   RULE), PROBE PROTOCOL, OPERATIONAL STATE, NON-NEGOTIABLES.
8. `docs/experiments/PREREG_COASTP1_RUNG2_2026-09-02.md` sec 4 (the invocation reproduced here)
   and sec 7 (the comparator; every number in sec 5 was RE-MEASURED, not quoted).
9. `docs/experiments/PREREG_MERGED_BUILD_CONTROL_2026-09-02.md` sec 7 - the gate that makes this
   run adjudicable on the merged binary. See sec 3's KNOWN SECOND DIFFERENCE.

**CONCLUSION FROM THE DOCS, REGISTERED AS A CLAIM ABOUT WHAT WE WILL SEE:** `-q` redirects the
back end's output away from the console; `vrfSim.log` is created and written regardless; and at
notifyLevel 3 with a real console window attached, the vendor says removing the console write
removes a performance drag. So the docs predict the log KEEPS its content and the clock gets
FASTER. Both are testable and both are registered below.

## 2. THE ONE VARIABLE - the code diff

`scripts/LaunchVrf.ps1`: a new `[switch] $QuietBackend` in the param block, and

    $simBlock = @('--appNumber', "$BackendAppNumber")
    if ($QuietBackend) { $simBlock += '-q' }

`scripts/RunC2SimScenario.ps1`: a matching `[switch] $QuietBackend`, appended to Stage 3's
`$launchArgs` only when set, and recorded as `inputs.quietBackend` in the run manifest either way.

VERIFIED BEFORE REGISTRATION, three ways, all run this session:
  - `LaunchVrf.ps1 -DryRun` with the switch OFF prints the command line CHARACTER FOR CHARACTER
    as the record's runs did: `... --simArgs --appNumber 9001 --scenarioFileName "..." --guiArgs
    --appNumber 9002`. With `-QuietBackend` it prints `... --appNumber 9001 -q --scenarioFileName
    ...`. The default path is untouched.
  - `RunC2SimScenario.ps1 -DryRun` passes `-QuietBackend` through to LaunchVrf when set and OMITS
    it when not. The dry run did NOT advance the appNo marker (still 3767 afterwards).
  - `tests/RunnerTurnaround.Tests.ps1`: **105 passed, 0 failed** after the change.

## 3. EVERYTHING ELSE HELD - and the ONE KNOWN SECOND DIFFERENCE

HELD at rung 2's values: fixture `TropicTortoise_FFRTC` (repo and C:\MAK deploy both hashing
D27E540F8BCCAA2EBDD33C63CF062CB4842DBE1937B7AA3DAC1C20F9C990B0B9, re-verified this session);
`data/COA-STP1_Initialization.xml` + `data/COA-STP1_Order.xml`; `-RunSecs 2700 -SampleSecs 10
-StopWhenComplete`; `$env:Vrf__DeStackCreates = 'true'` and nothing else; TimeMultiplier 1x;
`vrfSim.mtl` untouched (notifyLevel 3 / objectConsoleNotifyLevel 3, last written 2026-09-01
14:32:14); bridge A7504441, NOT rebuilt.

CHANGED, DELIBERATELY, and NOT the variable under test: the deployed `appsettings.json`
`Vrf:ClientId` goes STP -> **C2SIM**, because COA-STP1's init declares SystemName C2SIM and the
runner ABORTS at validation otherwise (it did, exit 2, on the first dry run). This is the
handoff's CLIENTID TRAP and it is rung 2's own value; the DEPLOYED (gitignored) copy is edited,
never the tracked `src/` file.

**KNOWN SECOND DIFFERENCE, STATED PLAINLY: the app binary is not rung 2's.** Rung 2 ran on
3b7b8d2e...c60cea0; this runs on the merged build 570619630015...ACEB52A6. Task 1
(PREREG_MERGED_BUILD_CONTROL sec 7) showed the two are behaviourally identical on the R9 order -
app logs diffing to ZERO HUNKS, endpoints to six decimals - but that was 3 units, not 128. The
only unconditional new work the merge adds on the hot path is one `EchelonCode.ToString()` per
unit in `InitParser`, i.e. 128 string conversions ONCE at init, which cannot move a per-frame
clock slope. It is nevertheless a second difference and P2's adjudication says so. Sec 5's P3
control clauses exist partly to catch it if it is more than that.

## 4. INVOCATION

    $env:Vrf__DeStackCreates = 'true'
    Get-ChildItem env:Vrf__*            # echoed into the console log
    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init  data/COA-STP1_Initialization.xml `
        -Order data/COA-STP1_Order.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 2700 -SampleSecs 10 -StopWhenComplete `
        -QuietBackend

THE WINDOW AND ITS CLOCK. `-RunSecs 2700` is a **WALL** cap and it is rung 2's own value, held so
the two runs are directly comparable in the only unit the operating system charges us in. At
rung 2's measured 0.2652 sim-s per wall-s that window buys ~716 SIM seconds; if `-q` helps, the
same wall window buys MORE sim seconds, which is the effect under test and is why the WALL side
is what is held fixed. The runner's own cap is 30..86400 s, so 2700 is well inside it. Expected
total wall ~50-60 min (rung 2: 49 min 57 s). `-StopWhenComplete` is INERT for this order for the
reasons rung 2 registered (T9's performer has zero Locations, T13 carries a 12,000 s WALL delay),
so the window will run its cap; if it DOES fire, that is recorded, not a deviation.

APP NUMBERS. The Appendix B marker reads `*** NEXT FREE: 3767 ***` at registration (the only
value-bearing marker). The runner allocates 7 and advances the marker itself: expected 3767-3773,
marker -> 3774, with 3773 (createOneDiag) consumed only if the stage-7 oracle gate fails. The
post-run ResetVrf sweep takes 3774 by hand, ledgered BEFORE the join, marker -> 3775.

AFTER: `Remove-Item env:Vrf__DeStackCreates`, restore the deployed `Vrf:ClientId` to STP, then
`tools/ResetVrf <fresh appNo>` WITH the RUNBOOK :1208-1215 environment (cwd
C:\MAK\vrforces5.0.2\bin64 + the VR-Forces / VR-Link / makRti bin PATH prefix + Machine-scope
MAKLMGRD_LICENSE_FILE). 3757 was burned for skipping that; 3766 used it and exited 0.

## 5. THE COMPARATOR - rung 2, run 20260902T165144Z, ALL RE-MEASURED THIS SESSION

Not quoted from prose. Every number below came from a command run against that run's own
artifacts today, and the two instruments were checked against their published results FIRST
(the false-greens rule):

  frame_gaps.py         LS slope **0.2652** sim-s per WALL-s; TEST A 89/89 = 100.0%;
                        TEST B R = 0.9985; |resid| <= 0.0005 s 279/286 = 97.6%;
                        700,975 lines / 1,201 stamped / 286 distinct sim stamps
  vrfc2simapp.log       `safe MSL` **128**; `DeStack (R8):` **10**; `CreateRoute` **9**;
                        `MoveToLocation` **0**; `TASKCMPLT` **0**; `TYPE MAP` **0**
  bin64-vrfSim.log      **700,975** lines; `Can't find entity route` **0**;
                        `Move-Along Route:` **22**; `Registered object` **3,727**;
                        `Created radio` **1,733**; `invalid formation name` **64**;
                        `'s Offset Route` **210** lines; FATAL 0; SocketException 0;
                        `moveAlong() - empty route` 0; `Waiting for nav data` 0
  reports-captured.log  **1,536** position reports over **128** distinct uuids
  per-performer march   **9 of 11** order taskees with net displacement > 0.5 km, measured on
                        the C2SIM report stream keyed by the manifest's `orderTaskees`:
                        6.07, 6.64, 6.64, 6.57, 5.95, 1.80, 6.59, 2.85, 4.27 km - which
                        REPRODUCES the rung-2 outcome table to 0.01 km. The two zeros are T9
                        (zero Locations, never taskable) and T13 (12,000 s WALL start delay).
  window                observation window 2,735.7 s WALL, total run 49 min 57 s

DERIVED, and the basis of every rate below: rung 2 covered **725.7 SIM seconds**
(2735.7 wall s x 0.2652). So rung 2's rates are **966 vendor-log lines per sim-second** and
**2.12 position reports per sim-second**.

## 6. PREDICTIONS - registered before launch, with confidence, clock, and falsifiers

P1 - **THE LOG FILE SURVIVES `-q`.** HIGH confidence; this is the risk the tasking told me to
     answer from the docs first, and sec 1 answers it. The live confirmation:
  (a) `bin64-vrfSim.log` is captured into the run directory and is **> 100,000 lines**.
  (b) It still carries all four evidence forms, at these floors: `Registered object` **>= 3,000**
      (rung 2: 3,727), `Created radio` **>= 1,400** (1,733), `Move-Along Route:` **>= 22** (22),
      `leaderRoute` **>= 40** (55). Floors, not equalities, because more sim time in the same
      wall window means MORE of each - the direction P2 predicts.
  (c) Vendor-log lines per SIM second in **[500, 1500]** (rung 2: 966). CLOCK: explicitly per
      SIM second, computed as lines / (window_wall_s x measured_slope) - the THRESHOLD RULE.
      The band is wide on purpose: log composition changes with what the units are doing, and
      the claim under test is "the file still gets written", not "written identically".
  FALSIFIER: an empty, absent or thin log (a rate near zero). That would mean the Users Guide's
  own three statements are wrong for 5.0.2 on Windows, and it is a STOP.

P2 - **THE CLOCK GETS FASTER.** This is the question. Docs basis: Table 8 p.177 says console
     output at a high notification level "can degrade performance" and quiet mode "prevents this
     degradation"; we are at notifyLevel 3 with a console-subsystem back end given its own window;
     rung 2 pushed 700,975 lines through it in 2,735.7 s.
  (a) DIRECTION (registered at HIGH): LS clock slope **> 0.2652** sim-s per WALL-s.
  (b) MAGNITUDE (registered at MEDIUM): slope **>= 0.30**, i.e. at least a 13% gain.
  CLOCK: both are sim-seconds per WALL-second by construction; both units named.
  I EXPECT (a) AND (b), and I am registering the direction as the load-bearing half because it
  is what the documentation actually asserts; the vendor gives no magnitude and I have no basis
  to predict one beyond "big enough to be worth a switch".
  CROSS-CHECK, registered so the clock cannot be measured by one instrument alone: position
  reports per SIM second in **[1.6, 2.7]** (rung 2: 2.12). Rung 2 established the C2SIM report
  stream is SIM-PACED, so if the slope rises and this rate holds, two independent instruments
  agree on the same sim clock. If the slope rises and this rate FALLS proportionally, the
  "speed-up" is an artifact of the log-stamp sample and not a real clock change.

P3 - **IT IS STILL THE SAME RUN.** HIGH confidence, EXACT values - these are controls, not
     findings, and they are what would catch the merged binary being a bigger difference than
     task 1 measured:
  (a) vrfc2simapp.log: `safe MSL` **128**, `DeStack (R8):` **10**, `CreateRoute` **9**,
      `MoveToLocation` **0**. All EXACT.
  (b) `TYPE MAP` **0** and any FidelityTable log form **0** - the mode line must read
      `Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA) (11.1.225.3.2.0.0)).`
  (c) `Can't find entity route` **0** EXACT, and ZERO 35-character route-name cuts. The freeze
      stays fixed; a nonzero here is the handoff's registered reopening evidence.

P4 - **ALL NINE PERFORMERS STILL MARCH.** HIGH confidence. **>= 9 of 11** order taskees with net
     displacement **> 0.5 km** on the C2SIM report stream, with the same two zeros (T9, T13).
     CLOCK: net displacement is a DISTANCE, so no clock qualifies the threshold itself - but the
     distance accrues over SIM time, so a faster clock can only increase it. The 0.5 km floor is
     therefore safe whichever way P2 lands (rung 2's smallest mover was 1.80 km).
  FALSIFIER: eight or fewer marchers, or a different pair of zeros.

P5 - **HYGIENE AND MODE.** HIGH.
  (a) FFRTC mode check still PASSES on the handoff's criterion: TEST A >= 95% in {0.033, 0.034}
      AND TEST B R >= 0.99. `-q` must not change the frame mode; if it does, P2 is measuring
      two variables.
  (b) Runner exit 0, every stage exit 0; no new .dmp; the fixture still hashes D27E540F8BCC...B0B9
      and vrfSim.mtl still stamps 2026-09-01 14:32:14 (NOTHING written under C:\MAK); RTI trio
      PIDs unchanged; ResetVrf sweep joins clean, 0 reflected, exit 0.

## 6A. THE MISS RULE - registered before launch, deliberately asymmetric

**STOP (write it up, do not adjust, do not re-run):** any miss on P1, P3, P4 or P5. These are
HIGH-confidence controls and safety properties. A miss on P1 means the vendor documentation is
wrong about its own product and every future run's evidence is at risk. A miss on P3 or P4 means
`-q` (or the merged binary at 128 units) changed behaviour, which is a far bigger finding than
any speed number.

**NOT A STOP - a recorded finding, and the work continues:** P2(b), the MAGNITUDE. If the slope
rises but by less than to 0.30, the answer is "console I/O is a real but small cost at this
scale", which is worth knowing and is not a failure of understanding.

**STOP:** P2(a), the DIRECTION. If the slope comes in at or BELOW 0.2652 by more than the
measurement's own spread, then removing ~700,000 console writes did not speed the back end up,
which CONTRADICTS Table 8 p.177 for our configuration. That is not a tuning miss; it means the
mechanism is not what the vendor's sentence says, and the right response is to write it up - it
would be the first vendor-documentation discrepancy of the whole saga, and the running count of
vendor defects found is ZERO, so the prior is strongly that I have misread something.

VOID CONDITION. An abort before the order is pushed is an infrastructure event, not a miss:
recorded, retried once. Two consecutive infrastructure failures stop the session for research.

## 6B. WHAT A CLEAN PASS WOULD AND WOULD NOT ESTABLISH

WOULD: that `-q` is safe to adopt (the log, which is our entire vendor-side evidence channel,
survives it) and that console I/O costs a measurable fraction of the back end's frame budget at
128 units. It would give the FFRTC block a second, cheaper clock lever alongside the frame mode.

WOULD NOT: it would NOT make FFRTC a speed lever - even a large gain on 0.2652 stays far below
the 1.0 that plain variable-frame delivers, so the handoff's CLOSED tripwire stands and a scale
run's wall budget is still `sim / measured ratio`. It would NOT say anything about the FRONT end
(vrfGui keeps its console; `-q` is documented as back-end only, and Windows-only). It would NOT
be a licence to change `vrfSim.mtl` - that still needs the user.

## 7. OUTCOME - run 20260902T183135Z_run, appNos 3767-3773, adjudicated from run-directory artifacts

### VERDICT - THE RUN ANSWERS BOTH QUESTIONS, AND IT **STOPS** UNDER SEC 6A

**(1) THE DOCUMENTATION IS RIGHT: `-q` DOES NOT SUPPRESS `vrfSim.log`.** The vendor log came back
**825,576 lines** (rung 2: 700,975) and, per SIM second, at **961.9 lines against rung 2's 966.2 -
a 0.5% difference.** The file is written at the same rate; only its destination changed. The
registered risk is closed on evidence.

**(2) CONSOLE OUTPUT IS A REAL PER-FRAME COST: THE CLOCK ROSE FROM 0.2652 TO 0.3140 sim-s per
WALL-s, +18.4%.** Both P2 clauses pass, and an INDEPENDENT instrument agrees: the sim-paced C2SIM
report stream ran 1,793 reports against rung 2's 1,536, a ratio of **1.167** against the clock
ratio **1.184** - two instruments, 1.5% apart.

**AND THE RUN STOPS.** Two registered predictions MISSED, both traceable to ONE fact: **B/5-20
(T35), one of the order's three Tank Companies, built ZERO internal sub-routes and STALLED after
0.41 km**, where in rung 2 it built four and marched 2.85 km. P4 required >= 9 of 11 marchers and
got **8**; P1(b) required `Move-Along Route:` >= 22 and got **18** - and the four missing entries
are EXACTLY `B/5-20_R0..R3`. Under sec 6A a miss on P1 or P4 is a STOP: this is written up, nothing
is retuned, nothing is re-run, and **`-q` is NOT adopted** on the strength of its own passing
speed number. I CANNOT SAY FROM ONE RUN WHETHER `-q` CAUSED THE STALL - see the adversarial review.

### RUN FACTS (all from the run directory)

Run dir `runs/20260902T183135Z_run`. Order pushed 2026-09-02T18:34:18.812Z, observation window
closed 19:19:52.295Z (**2,733.48 s** WALL against its 2,700 s cap plus trail - rung 2's was
2,735.65 s, so the two wall windows are 2.2 s apart), manifest saved 19:20:32.037Z; **48 min 57 s**
total wall (rung 2: 49 min 57 s). appNumbers 3767-3773; ledger `wasValue` 3767 -> `newValue` 3774,
`advanced` true, taken BEFORE any join; 3773 (createOneDiag) UNCONSUMED, the stage-7 oracle gate
having passed. `runnerExitCode` **0**, all ten stages exit 0. One `validityFlags` entry, severity
INFO, the standing stock-TropicTortoise advisory. **`inputs.quietBackend` = `True`** in the
manifest - the switch is recorded in the evidence, not just in the command line.
`Get-ChildItem env:Vrf__*` = `Vrf__DeStackCreates=true` before, **0** after.

DERIVED SIM WINDOWS, used for every rate below (THRESHOLD RULE - each names its clock):
rung 2 = 2,735.65 wall s x 0.2652 = **725.5 SIM s**; this run = 2,733.48 x 0.3140 = **858.3 SIM s**.
The same wall window bought **132.8 more sim seconds**.

### P1 - THE LOG FILE SURVIVES `-q`. (a) PASS. (c) PASS. **(b) MISSED on one floor of four.**

(a) PASS. `bin64-vrfSim.log` captured, **825,576 lines** (predicted > 100,000).
(c) PASS, and it is the sharpest number in the run: **961.9 vendor-log lines per SIM second**
    against rung 2's **966.2**, a 0.5% difference, well inside the registered [500, 1500]. The
    band was wide because log composition varies; it did not need to be. The log is not thinned,
    not truncated, and not rate-limited - `-q` moved it, exactly as Table 8 p.177 says.
(b) **MISSED, and recorded as a miss rather than re-banded.** Three of four floors passed:
    `Registered object` **3,683** (floor 3,000; rung 2: 3,727), `Created radio` **1,733** (floor
    1,400; rung 2: **1,733** - identical), `leaderRoute` **43** (floor 40; rung 2: 55).
    `Move-Along Route:` came in at **18** against a floor of **22**.
    WHY THE FLOOR WAS WRONG, as an explanation and NOT an adjustment: I filed that count under
    "the log survives", but `Move-Along Route:` counts TASK DISPATCHES, not log lines of a fixed
    set - it is a BEHAVIOUR measure wearing a log-fidelity label. The diff is exact and it is not
    lost lines: all ten task-route entries are present at FULL length (including T5's 99-character
    name), all four `C/1-35_R0..R3` and all four `856/HHC_R0..R3` are present, and the ONLY
    entries missing against rung 2 are `B/5-20_R0`, `_R1`, `_R2`, `_R3`. Same for `leaderRoute`
    43 vs 55 and `'s Offset Route` 166 vs 210: one company of three did not distribute.
    THIS IS THE SAME THRESHOLD-DEFINITION ERROR CLASS RUNG 2 STOPPED ON TWICE. It is mine.

### P2 - THE CLOCK GETS FASTER. **PASS on both clauses, plus the cross-check.**

| statistic | THIS RUN (`-q`) | RUNG 2 (console) | registered |
|---|---|---|---|
| LS clock slope, sim-s per WALL-s | **0.3140** | 0.2652 | (a) > 0.2652; (b) >= 0.30 |
| ratio | **1.184x** | 1.0 | - |
| lines / stamped / distinct sim stamps | 825576 / 1074 / 284 | 700975 / 1201 / 286 | - |
| position reports | **1,793** | 1,536 | - |
| reports per SIM second | **2.089** | 2.117 | [1.6, 2.7] |
| report-count ratio | **1.167x** | 1.0 | - |

(a) PASS: 0.3140 > 0.2652.
(b) PASS: 0.3140 >= 0.30.
CROSS-CHECK PASS, and it is what makes the number believable: the report stream is SIM-paced
(rung 2's finding), so if the clock really ran 1.184x faster the same wall window should carry
~1.18x the reports. It carried **1.167x**. Two independent instruments - vendor-log sim stamps and
the C2SIM report count - agree to 1.5%, and reports-per-sim-second is FLAT at 2.089 vs 2.117.
The "speed-up" is therefore a real clock change, not an artifact of the log-stamp sample.

RECORDED, NOT REGISTERED, AND IT MATTERS FOR HOW MUCH TO TRUST 0.3140: the LS fit's residual is
much worse this run - **resid sd 58.66, max 999.17**, against rung 2's 1.66 / 4.81. The slope is
a least-squares fit over 284 stamped sim/wall pairs, and a large residual means the rate was NOT
uniform across the window. That is consistent with the T35 stall (the load changes when one of
128 units stops distributing), and it means 0.3140 should be read as "the window average, +18%",
not as a precise constant. The report-count cross-check, which is a pure ratio over the whole
window and needs no fit, is the more robust of the two and gives +16.7%. **Both are well clear of
zero and the direction is not in doubt.**

### P3 - IT IS STILL THE SAME RUN. PASS on all three clauses, all EXACT.

(a) vrfc2simapp.log: `safe MSL` **128**, `DeStack (R8):` **10**, `CreateRoute` **9**,
    `MoveToLocation` **0** - rung 2's values exactly. App log 517 lines vs rung 2's 517.
(b) `TYPE MAP` **0**; the mode line reads
    `Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA) (11.1.225.3.2.0.0)).`
(c) `Can't find entity route` **0** EXACT; ZERO 35-character route-name cuts; **9** new-form route
    lines, **0** old-form. THE FREEZE STAYS FIXED. The handoff's reopening evidence did not appear.
ALSO: the app log's warning/error census is IDENTICAL to rung 2's, line form for line form (33
`warn:`, 3 `fail: C2SIM.C2SIMSDK`, 2 deserialise failures, 1 `fail: VrfC2Sim`, the two standing
"No creator found" notices) and `DROPPING TASK` is **0** in both. OUR SIDE DID NOTHING DIFFERENT.

### P4 - ALL NINE PERFORMERS STILL MARCH. **MISSED: 8 of 11.**

Per-performer net displacement from the C2SIM report stream, keyed by the manifest's
`orderTaskees` - the same instrument that reproduced rung 2's published table to 0.01 km:

  performer            RUNG 2 net_km   THIS RUN net_km   note
  ------------------   -------------   ---------------   ----------------------------------
  3ac081eb (T5)             6.64             7.87        further, as a faster clock predicts
  50828a9b (T31)            6.59             7.72        further
  6977b035 (T19)            6.57             7.84        further
  74bdb03b (T15)            6.64             7.86        further
  d6df3c3d (T1)             6.07             7.32        further
  de16a337 (T23)            5.95             7.20        further
  6a266f06 (T27 856/HHC)    1.80             6.55        MUCH further - rung 2's anomaly cleared
  b5b42765 (T39 C/1-35)     4.27             5.50        further
  **1375ca0a (T35 B/5-20)   2.85             0.41        STALLED - the miss**
  5cd92a83 (T9)             0.00             0.00        expected: zero Locations, never taskable
  e151451b (T13)            0.00             0.00        expected: 12,000 s WALL start delay

Eight of eleven cleared the 0.5 km floor. The two expected zeros are the same two. THE MISS IS
T35, and it is not a threshold artifact - it is a stall with a timestamp. Its own track:

  T35 in RUNG 2   18:xx equivalent: creeps N to 34.684654,-116.724819 by fix 6, then at
                  17:18:55 TURNS SW and runs to 34.670546,-116.753148 - 2.85 km net, 12 fixes.
  T35 THIS RUN    creeps N to **34.684690,-116.724806** by fix 6 (18:51:07), then sits at that
                  coordinate for the remaining **8 fixes over 26 minutes**, moving under 2 m.

It reached the same waypoint and then did not distribute. `Locally Simulated: B/5-20_R*` objects:
rung 2 **4**, this run **0**. `B/5-20` lines carrying `leaderRoute`: rung 2 **14**, this run **0**.
The other two companies built their four each in BOTH runs. B/5-20 is otherwise alive and
chattering throughout - its message-form census is rung 2's, with MORE of every form (896 vs 768
continuation lines, 56 vs 48 position texts), and no error form appears in one run and not the
other.

### P5 - HYGIENE AND MODE. PASS.

(a) FFRTC mode check PASSES on the handoff's criterion: TEST A **83/83 = 100.0%** in
    {0.033, 0.034} (>= 95%) AND TEST B **R = 0.9940** (>= 0.99). `-q` did not change the frame
    mode, so P2 measured one variable. (|resid| <= 0.0005 s was 98.2%, better than rung 2's 97.6%.)
(b) Runner exit 0; all ten stages exit 0; no new .dmp (newest is still
    vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp, 2026-09-02 06:00); the FFRTC fixture still hashes
    D27E540F8BCC...B0B9 and vrfSim.mtl still stamps 2026-09-01 14:32:14 - **NOTHING WAS WRITTEN
    UNDER C:\MAK**; RTI trio PIDs UNCHANGED (41336 / 224608 / 76620) and never touched; no
    VR-Forces process and no observer remains.
    POST-RUN SWEEP: `tools/ResetVrf 3774` with the RUNBOOK :1208-1215 environment - joined clean
    (BackendCount=0), 0 reflected, resigned cleanly, **exit 0**. LEDGER 3767 -> 3774 (7, runner)
    -> 3775 (1, hand-taken and ledgered BEFORE the join). The deployed `Vrf:ClientId` was set to
    C2SIM for this init and has been **restored to STP**.

### ADVERSARIAL REVIEW - what stalled T35, and can `-q` be blamed?

MY HYPOTHESIS, stated so it can be attacked: **H3 - the two-level Tank-Company distribution is
NON-DETERMINISTIC, and this run is a second sample of an instability rung 2 already recorded.**

THE STRONGEST COMPETING HYPOTHESIS: **H1 - `-q` caused it.** It has a real mechanism - `-q` moved
the clock by 18%, and a timing-sensitive step in the aggregate distribution could resolve
differently at a different frame cost. This run cannot exclude it, and I am not going to pretend
it can.

THE EVIDENCE THAT SEPARATES THEM, such as it is, favours H3:
  - **THE ANOMALY MOVED BETWEEN COMPANIES, IN BOTH DIRECTIONS.** Rung 2's own unexplained item 3
    was "T27/T35 lateral ~400 m against 1-72 m for every other performer" - the two Tank
    Companies were ALREADY the odd ones, at 1.80 and 2.85 km against seven performers at
    6.07-6.64 km. This run: T27 **cleared** (1.80 -> 6.55 km, right in the pack), T35 **degraded**
    (2.85 -> 0.41 km), C/1-35 improved (4.27 -> 5.50 km). A cause acting through `-q` or through
    the binary would be expected to push all three the same way; instead the class is unstable and
    which member misbehaves is not stable.
  - **NOTHING ON OUR SIDE DIFFERS.** Identical app-log line count, identical warning census, zero
    dropped tasks, nine CreateRoutes, nine new-form route lines, zero `Can't find entity route`.
    The app issued T35's move-along exactly as it issued the other eight.
  - **H2 (the merged binary) is the weakest of the three.** Task 1 gated it to a ZERO-HUNK app-log
    diff, and its only unconditional new work is 128 `EchelonCode.ToString()` calls, once, at init.
    It cannot reach into the back end's path distribution 17 minutes later.
WHAT WOULD FALSIFY H3 AND CONFIRM H1: a COA-STP1 run on this binary WITHOUT `-q` in which all
three companies distribute, paired with a second WITH `-q` in which one stalls again. That is a
two-run experiment, it is the obvious next step, and **it is NOT run under the stop.**

**THE UNEXPLAINED SYMPTOM IS RECORDED AS A FALSIFIER, NOT A FOOTNOTE:** until that pair is run,
`-q` is NOT established as safe, and its +18% must not be spent. A speed lever that might cost a
silently non-distributing company is worse than no lever - that is precisely the failure mode
(silent, per-unit, invisible in the aggregate numbers) this whole saga was about.

A SECOND CHECK I RAN BEFORE BELIEVING THE SPEED NUMBER: could the +18% be an artifact of the T35
stall itself - one fewer company distributing means less work per frame, hence a faster clock?
Partly, and it cannot be fully separated in one run. But it cannot be the whole story: the eight
marching performers ALL went FURTHER than rung 2 (7.20-7.87 km against 5.95-6.64 km), which is
more sim time delivered to the units that were working, and the report stream - which counts all
128 units, not just the companies - rose by the same 1.17x. A single stalled aggregate out of 128
does not buy 133 sim seconds.

ALSO RECORDED, against rung 2's other open items: the real-object population is IDENTICAL - 1,732
objects ever reporting real coordinates in BOTH runs. Rung 2's unexplained item 1 (objects stuck
at the (90,-90,0) pole) persists and is slightly smaller: **110 pole-only objects this run against
132 in rung 2**, out of 1,842 and 1,864 total. Still unexplained; still not growing.

### CONSEQUENCE

The `-q` question is ANSWERED on both halves - the log survives, and the console costs ~18% of the
back end's throughput at 128 units - and the runner switch stays, DEFAULT OFF, which is where it
was registered. Nothing else is built on it. The FFRTC block's CLOSED tripwire is untouched: even
at 0.3140, FFRTC at scale is still a **3.2x slowdown** against variable-frame's 0.9995, so a scale
run's wall budget is still `sim / measured ratio` and FFRTC is still not a speed lever.

## 8. REGISTRATION

Sections 0-6B written and committed BEFORE launch as **4d2f4c3**, together with the one-variable
code change. Sec 7 added after the run, from the run-directory artifacts only.
