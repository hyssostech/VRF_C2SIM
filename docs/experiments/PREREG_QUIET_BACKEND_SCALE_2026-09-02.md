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

## 7. OUTCOME

(written after the run, from the run-directory artifacts only)

## 8. REGISTRATION

Sections 0-6B written and committed BEFORE launch. Commit hash stamped in sec 7.
