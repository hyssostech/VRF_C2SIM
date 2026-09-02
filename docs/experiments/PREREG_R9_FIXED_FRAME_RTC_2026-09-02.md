# PREREG R9 FIXED-FRAME RUN-TO-COMPLETE - does the exercise clock mode change the answer, and does our app survive a sim clock that is no longer the wall clock? - registered 2026-09-02, BEFORE launch

WHAT THIS IS: Row 3 (docs/experiments/PREREG_TERRAIN_ROW3_DEFAULT_2026-09-02.md, run
20260902T113613Z) run again with EXACTLY ONE VARIABLE MOVED: the scenario file's exercise
clock mode goes from Variable-Frame Run-To-Complete (`(frame-mode "variable-frame")`,
`(frame-time 0.100000)`) to Fixed-Frame Run-To-Complete
(`(frame-mode "fixed-frame-run-to-complete")`, `(frame-time 0.045455)`). Nothing else moves:
same init, same order, same app binary, same bridge, same TimeMultiplier 1, no env override.

WHY IT MATTERS. Our current mode is the one the vendor says "does not provide repeatable
results" (Users Guide sec 3.4.3 p.122). Fixed-Frame Run-To-Complete is the vendor's named
mode for "run a simulation overnight and view the results the following day" (p.122) - it is
the only documented route to a headless run that is both repeatable and faster than wall
clock. THE GOAL is a headless pipeline. If this mode works with our federation, every future
scale run gets cheaper AND repeatable; if it does not, we learn the exact integration defect
now, offline-diagnosable, instead of on a 45-minute scale run.

## 0. CORRECTIONS TO THE TASKING BRIEF (recorded here rather than silently absorbed)

The brief that commissioned this prereg carried four statements that the sources do not
support. Each was checked against the primary artifact before this document was written.

C1 - "The R9 lean fixture ... the runner loads via LaunchVrf -Scenario <name>". THERE IS NO
     R9 FIXTURE. Row 3 (and every other 2026-09-02 run) loaded the STOCK
     `TropicTortoise.scnx`: `runs/20260902T113613Z_run/console-row3.log:69` shows
     `LaunchVrf.ps1 -Scenario TropicTortoise`, and `run-manifest.json` records
     `inputs.scenario = "TropicTortoise"`. `C:\MAK\vrforces5.0.2\userData\scenarios\
     TropicTortoise.scnx` is 6932 bytes dated 2026-07-14 12:45 - the installed file,
     untouched. "R9 lean" names the C2SIM INIT/ORDER pair
     (`data/R9_Mojave_Lean_Initialization_NoComments.xml` /
     `data/R9_Mojave_UnitMove_Order_NoComments.xml`), not a scenario. tools/FixtureGen's
     TankPltFixture_* fixtures are a DIFFERENT experiment (region-vs-structure) and no
     2026-09-02 run loaded one. CONSEQUENCE: the variant built for this run is
     `TropicTortoise_FFRTC`, derived from stock TropicTortoise.
C2 - "28/28 TASKCMPLT via -StopWhenComplete". The R9 order has THREE tasks and THREE taskees:
     `run-manifest.json` `inputs.orderTaskCount = 3`, `inputs.orderTaskees` = the three uuids
     001aa71b / 139aa71b / 670cfdb2, and Row 3 sec 6 records 3 TASKCMPLT. 28/28 belongs to the
     COA-STP1 / P3 family of runs (28 entities), which is a different order. THE NUMBER FOR
     THIS RUN IS 3/3.
C3 - "frame-time 0.045455 (1/22 s - matches today's variable-frame step at the 22 Hz target)".
     IT DOES NOT MATCH. Measured directly off Row 3's own vendor log
     (`runs/20260902T113613Z_run/bin64-vrfSim.log`, 395 stamped lines, 85 distinct
     (wall, sim) pairs): consecutive distinct sim stamps differ by min 0.032 / median 0.033 /
     max 0.037 sim-s (n = 33 sub-0.06 gaps). That is a ~30.3 Hz frame quantum, not 22 Hz.
     0.045455 s is a 38% LARGER (coarser) frame than the one running today. Corroboration:
     `C:\MAK\vrforces5.0.2\appData\settings\vrfSim\vrfSim.mtl:283` has targetFrameRate
     COMMENTED OUT (`;; (setqb targetFrameRate 30)`), so Table 71's built-in default 22 is
     nominally in force, yet the observed quantum is 30 Hz - the doc default and the observed
     rate disagree and this prereg does not resolve which is authoritative.
     THIS RUN STILL USES 0.045455, as directed, because a single named number is what makes
     the run interpretable; but the justification stated for it is wrong, and P3 below is
     written so the result is readable either way. If the supervisor prefers the
     step-matching value, the one-line regeneration is in sec 2 with `--frame-time 0.033333`
     and NOTHING else about the fixture, the command, or the predictions changes except the
     .scn hash quoted in sec 2 and the multiple predicted in P1 (b).
C4 - "targetFrameRate default 22 (variable-frame only)" is correctly cited (Table 71 p.1669)
     but is a CEILING, not a target: p.211 says it is "the frame rate above which the VR-Forces
     back-end should begin to sleep". A 22 Hz ceiling cannot produce the observed 30 Hz.
     Unresolved; recorded, not explained.

## 1. Sources read for this prereg (docs first, per the 2026-09-01 directive)

VENDOR, `C:\MAK\vrforces5.0.2\doc\VRFUsersGuide.pdf` (5.0.2), pages 1-indexed, text extracted
with PyMuPDF and read in full:

- sec 3.4.3 Exercise Clock Modes, p.122-123. Verbatim, Variable-Frame Run-To-Complete:
  "advances simulation time by the amount of time passed since the last time the exercise
  clock was ticked. This mode is typical for distributed, interactive simulations. It does
  not provide repeatable results. Do not use this mode in time-managed HLA federations."
  Fixed-Frame Run-To-Complete: "advances simulation time by a fixed amount each frame, even if
  a frame takes longer than the fixed amount to compute ... However, if the simulation takes
  less then the fixed frame time to compute, it does not wait for the remainder of the frame
  time to elapse before starting the next frame. This mode is most useful for situations where
  you want a simulation to run with internal consistency and high fidelity, and want it to run
  to completion, but do not need to observe the simulation. So, for example, you might run a
  simulation overnight and view the results the following day. Fixed-Frame Run-To-Complete
  mode is not suited for interactive use. It is suitable for distributed use only in
  time-managed HLA federations. It disables the Simulation Time Scale Toolbar."
- sec 6.12 Tuning the Target Frame Rate, p.211: targetFrameRate "is only applicable to
  variable frame mode. The fixed frame modes already have a built in sleep that gives up
  unused time." NOTE THE TENSION with sec 3.4.3, which says FFRTC specifically does NOT wait
  out the remainder of a frame. Read literally, p.211's "built in sleep" describes
  Fixed-Frame BEST-EFFORT; if it also applied to FFRTC there would be no compression at all
  and P3 would come out at 1.0. P3 is written so either outcome is recorded, not adjusted.
- sec 7.6.1 Changing the Simulation Speed, p.254-255: "If you increase the speed at which a
  scenario runs, the frame rate is reduced and performance of models may degrade";
  `./appData/settings/vrfSim/fastForwardSettings.mtl` maps a play-speed to
  `(frame-mode 2) ;; fixed frame` + a `(frame-time ...)`. NOT USED HERE: that file is keyed on
  the time-scale multiplier, and this run keeps TimeMultiplier 1, so no fast-forward entry can
  fire. The file is READ ONLY as evidence that MAK's own answer to "go faster" is a fixed
  frame mode with a coarser frame time.
- sec 12.2.1 Scenario Parameters, p.351-352 and Table 17 p.353-355. The .scn carries
  `(frame-mode "variable-frame")` and `(frame-time 0.100000)` as plain top-level parameters.
  Table 17 p.354: frame-mode is one of `variable-frame` / `fixed-frame` /
  `fixed-frame-run-to-complete`; frame-time is "the length of a frame, in seconds. If
  frame-mode is set to fixed-frame or fixed-frame-run-to-complete, you must set the frame time
  to a non-zero value. A value of zero for frame time prevents simulation time from advancing
  in either of these modes." Also p.353, time-multiplier: "If you are running a scenario using
  HLA Time Management, it is strongly recommended that you set time-multiplier to 1." We do.
- Table 71 vrfSim.mtl parameters, p.1669, targetFrameRate: "Specifies the frame rate for the
  current frame above which the back-end will sleep ... This option applies only to variable
  frame mode ... Default: 22." See C3/C4 above.
- sec 12.1 p.350 and sec 12.7 p.358: a .scnx is "a compressed zip archive" of
  `scenario_name.<ext>` parts; on load the back end unpacks into a `vrfbe`-prefixed temp dir
  and the front end into a `vrffe`-prefixed one. The guide does NOT document how the .scn is
  selected inside the archive, which is why sec 2 renames every part rather than guessing.

VENDOR API HEADERS (read-only under C:\MAK, both re-verified line by line):

- `C:\MAK\vrforces5.0.2\include\vrfcgf\cgf.h:1192-1203` -
  `virtual void setFrameRateMode(const DtString& mode, DtReal deltaTime = -1);` with the
  comment block naming exactly `"variable-frame"`, `"fixed-frame"`,
  `"fixed-frame-run-to-complete"` and "If the frame mode is of type "fixed-frame" or
  "fixed-frame-run-to-complete", then the deltaTime parameter is the duration of a single
  frame in seconds."
- `C:\MAK\vrforces5.0.2\include\vrfutil\scenario.h:249-258` - `frameRateModeString()`,
  `frameRateMode()`, `setFrameRateModeString()`, `setFrameRateMode(DtFrameMode)`,
  `frameDeltaTime()`, `setFrameDeltaTime()`. (The brief cited 250-257; the block is 249-258.)
  NEITHER API IS REACHABLE FROM OUR CODE: `src/VrfFacade/VrfFacade.h` exposes no clock or
  frame-mode entry point (grep for clock/simTime returns only `SetTimeMultiplier` and
  `SetExerciseStartTime`). The scenario FILE is therefore the only lever we have, which is
  why this probe is a fixture change and not a code change.

VENDOR CONFIG, read-only:

- `C:\MAK\vrforces5.0.2\appData\settings\vrfSim\vrfSim.mtl:142` - `(setqb
  runInTimeManagementMode 0)`. OUR FEDERATION IS NOT TIME-MANAGED. This is the primary source
  behind the named risk in sec 6.
- same file :205/:208/:308 - notifyLevel 3, objectConsoleNotifyLevel 3,
  enableLogFileTimestamps 1 (the 2026-09-01 change vs `vrfSim.mtl.bak-20260901`). The
  timestamps are what make P1 and P3 measurable at all.
- targetFrameRate: commented out (:283). No override in force.

REPO:

- docs/experiments/PREREG_TERRAIN_ROW3_DEFAULT_2026-09-02.md - the 1x comparator in full.
- docs/experiments/ANALYSIS_P3_STEP_PROFILE_2026-09-01.md - measured tick quantum ~0.033
  sim-s at BOTH 1x and 5x; the variable-frame clock makes rate proxies "blind to a single
  long tick by construction"; `tools/analysis/step_profile.py` is the instrument.
  DEFECT FOR THE LIVE EXECUTOR: `step_profile.py:41` hard-codes
  `STAMP_RE = ...\[Tue Sep  1 ...\]`. It will parse ZERO stamps from a 2026-09-02 log. Change
  the weekday/date or generalize the regex BEFORE using it, or the tool will report a clean
  "no stamps" that is a false negative.
- scripts/RunC2SimScenario.ps1 - `-Scenario` (:223) is passed straight to LaunchVrf (:1744);
  `-RunSecs` is a WALL-CLOCK observation window and, with `-StopWhenComplete`, a CAP (:44-46,
  :133); `-SettleHoldSecs` (default 60) is WALL; `-SampleSecs` is the observer sample cadence.
- scripts/LaunchVrf.ps1:175-176, 235-239, 344 - the scenario is resolved to
  `C:\MAK\vrforces5.0.2\userData\scenarios\<name>.scnx`, its EXISTENCE IS HARD-CHECKED before
  launch (`Say-Fail ... $hardFail = $true`), and it is passed as
  `--scenarioFileName ../userData/scenarios/<name>.scnx`. A missed deployment is therefore a
  loud pre-launch stop, never a silent fall-back to TropicTortoise.
- tools/WatchVrf/WatchRunner.cs:158,168,195,209,225,246 and tools/ListenReports/Program.cs:162
  - EVERY observer timestamp is `DateTime.UtcNow`. There is NO sim-time column anywhere in
  watchvrf-trace.csv or reports-captured.log.
- src/VrfC2SimApp/* - the wall-clock inventory in sec 5 P4.
- src/VrfFacade/VrfFacade.cpp:477-482 - see sec 5 P4, the integration question.

## 2. THE FIXTURE: TropicTortoise_FFRTC, and the deployment step the live executor must run

BUILT OFFLINE, NOT DEPLOYED. `tools/FixtureGen/build_fixture.py` gained a `--frame-mode` /
`--frame-time` pair (default None = leave the base scenario's two lines untouched), an
`--out-dir` (default unchanged = the MAK scenarios directory), and a `--frame-variant SRC:OUT`
mode that emits OUT.scnx = SRC.scnx with ONLY those two lines moved. This session wrote
NOTHING under C:\MAK.

REGRESSION PROOF THAT THE DEFAULT IS AN IDENTITY. All three pre-existing fixtures were
regenerated with the new code into a scratch directory and every staged part compared by
SHA-256 against the parts staged before the change:
`33 files, IDENTICAL 33, CHANGED 0, only-before 0, only-after 0`
(TankPltFixture_Sweden, TankPltFixture_Mojave, TankPltFixture_Mojave_BelowTerrain, 11 parts
each). `python tools/FixtureGen/validate_fixture.py` -> `ALL FIXTURES: OK`.
`python -m py_compile tools/FixtureGen/build_fixture.py` -> clean.

THE VARIANT. `tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx`, 7115 bytes,
SHA-256 `aba2b9de6fddc3c6d530b48f78741783f9033aeeb9f1408b29f394d5d31089e3`. (That archive hash
is NOT reproducible: zip entries carry file mtimes. The CONTENT identity that is stable is the
.scn: SHA-256 `2b50495d8b9e280ae04f2e6e280b38a65783a52fe511c53c3c0ac3515e294a52`, 0 non-ASCII
bytes, LF line endings exactly as the vendor writes them. The 10 non-.scn parts are
BYTE-FOR-BYTE the stock TropicTortoise parts - verified 10/10.)

THE COMPLETE .scn DIFF, stock TropicTortoise.scn -> TropicTortoise_FFRTC.scn:

    -   (Order-Of-Battle "TropicTortoise.oob")          +   (Order-Of-Battle "TropicTortoise_FFRTC.oob")
    -   (Scenario-Scripts "TropicTortoise.spt")         +   (Scenario-Scripts "TropicTortoise_FFRTC.spt")
    -   (Orbat "TropicTortoise.orb")                    +   (Orbat "TropicTortoise_FFRTC.orb")
    -   (Plan "TropicTortoise.pln")                     +   (Plan "TropicTortoise_FFRTC.pln")
    -   (Overlay "TropicTortoise.ovl")                  +   (Overlay "TropicTortoise_FFRTC.ovl")
    -   (SelectionGroups "TropicTortoise.sgr")          +   (SelectionGroups "TropicTortoise_FFRTC.sgr")
    -   (Object-Map "TropicTortoise.omp")               +   (Object-Map "TropicTortoise_FFRTC.omp")
    -   (Scenario-extras "TropicTortoise.xtr")          +   (Scenario-extras "TropicTortoise_FFRTC.xtr")
    -   (frame-mode "variable-frame")                   +   (frame-mode "fixed-frame-run-to-complete")
    -   (frame-time 0.100000)                           +   (frame-time 0.045455)
    -   (GuiObserverViews "TropicTortoise.osrx")        +   (GuiObserverViews "TropicTortoise_FFRTC.osrx")
    -   (GuiScenarioSettings "TropicTortoise.gui_settings") + (GuiScenarioSettings "TropicTortoise_FFRTC.gui_settings")

Twelve lines. TWO are the variable. The other TEN are the mechanical part-name retarget that
the rename forces, each a pure `TropicTortoise` -> `TropicTortoise_FFRTC` substitution with no
other edit. The rename is deliberate and is NOT an extra variable: it is the naming convention
build_site() has always used and the only .scnx layout this repo has ever loaded live (the
TankPltFixture_* fixtures); the guide (sec 12.1 p.350, sec 12.7 p.358) does not document how
the back end picks the .scn out of the archive, so keeping mismatched internal names would be
the untested choice, not the conservative one.

DEPLOYMENT - THE LIVE EXECUTOR RUNS THIS FIRST, from the VRF_C2SIM checkout, before any
launch. It writes ONE new file under C:\MAK and modifies nothing that exists:

    Copy-Item tools\FixtureGen\frame_variants\TropicTortoise_FFRTC.scnx `
              C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise_FFRTC.scnx

Verify before launching (the scnx hash varies with zip mtimes, so verify the .scn, which does
not):

    python -c "import hashlib,zipfile; z=zipfile.ZipFile(r'C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise_FFRTC.scnx'); b=z.read('TropicTortoise_FFRTC.scn'); print(hashlib.sha256(b).hexdigest()); print([l for l in b.decode().splitlines() if 'frame-' in l])"

    expected: 2b50495d8b9e280ae04f2e6e280b38a65783a52fe511c53c3c0ac3515e294a52
              ['   (frame-mode "fixed-frame-run-to-complete")', '   (frame-time 0.045455)']

EQUIVALENT REGENERATION (same result, writes straight into the MAK directory - use only if the
committed .scnx is unavailable). This is also the ONE-LINE change if the supervisor rules for
the step-matching frame time instead (swap 0.045455 for 0.033333; nothing else moves):

    python tools\FixtureGen\build_fixture.py --frame-variant TropicTortoise:TropicTortoise_FFRTC `
           --frame-mode fixed-frame-run-to-complete --frame-time 0.045455

STOCK TropicTortoise.scnx IS NOT TOUCHED by either path, so every prior run stays reproducible
and a revert is "pass -Scenario TropicTortoise again".

## 3. The ONE variable, and everything held

VARIABLE: the loaded scenario file - `TropicTortoise` -> `TropicTortoise_FFRTC`, whose only
semantic difference is the two frame lines.

HELD, all Row 3's: init `data/R9_Mojave_Lean_Initialization_NoComments.xml`; order
`data/R9_Mojave_UnitMove_Order_NoComments.xml` (3 tasks, 3 taskees, NO StartTime /
SimulationTime / DelayTime element anywhere in it - verified, so no sequencer delay is in play);
the app binary as built for Row 3 (GroundWaypointAltitudeMode = the compiled default
TerrainProfile, TerrainClearanceMeters 10, TerrainProfileTimeoutSeconds 10,
GroundWaypointLiveClearanceMeters 50, RealTemplates, stock templates, NavArea disabled);
VrfBridge.dll A7504441...; notify level 3; TimeMultiplier 1;
`Get-ChildItem env:Vrf__*` MUST BE EMPTY at launch and must be echoed into the console log
exactly as Row 3 did.

NOT held, and deliberately so: `-RunSecs 420` becomes `-RunSecs 1800`. This is a CAP, not a
length - with `-StopWhenComplete` the window closes on completion + `-SettleHoldSecs`. It is
raised because the mode's direction is unknown in advance: FFRTC advances 0.045455 sim-s per
frame regardless of what a frame costs, so if a frame costs MORE than 45 ms of wall compute
the sim runs SLOWER than wall and 420 s would truncate a healthy run and manufacture a P2
miss. A cap can only ever end a run that has already failed.

## 4. Invocation (main checkout, VRF_C2SIM, pwsh) - NO env line at all

    pwsh -NoProfile -File scripts/RunC2SimScenario.ps1 `
        -Init data/R9_Mojave_Lean_Initialization_NoComments.xml `
        -Order data/R9_Mojave_UnitMove_Order_NoComments.xml `
        -Scenario TropicTortoise_FFRTC `
        -RunSecs 1800 -SampleSecs 2 -StopWhenComplete

Foreground. Adjudication from the run directory artifacts ONLY (vrfc2simapp.log,
reports-captured.log, run-manifest.json, watchvrf-trace.csv, bin64-vrfSim.log, the console log).

APP NUMBERS: the ledger marker is consumed and advanced BY THE RUNNER; this prereg does not
reserve numbers and does not edit the marker. Take whatever
`*** NEXT FREE: <number> ***` reads in docs/OPUS_EXECUTION_PLAN.md Appendix B AT LAUNCH TIME.
It read 3725 when this prereg was written, but run 20260902T125423Z was in flight on the box
at that moment and consumes 7, so the expected block is 3725-3731 ONLY IF the marker still
says 3725 when you look; otherwise it is whatever the marker then says, +6. Record the actual
wasValue/newValue from the manifest in sec 7.

PREREG COMMIT: sections 0-7 below were registered in commit f1f0f38 BEFORE launch, together
with the FixtureGen change and the fixture itself. This paragraph is the only content added
afterwards (in the immediately following commit); nothing in sections 0-7 changed after
f1f0f38, which is what the hash attests.

PRE-LAUNCH INVENTORY (must hold, else STOP - never kill): the same gate Row 3 sec 3 states -
no vrfSim* / vrfGui / vrfLauncher / WatchVrf / ListenReports / VrfC2SimApp process of any
kind; the RTI trio present and its PIDs recorded; docker stp-server + c2sim_server healthy;
`Get-ChildItem env:Vrf__*` count 0; the 10 main-checkout VrfBridge.dll copies at a single
hash; PLUS, new for this run, `TropicTortoise_FFRTC.scn` hashing to the sec-2 value.

## 5. PREDICTIONS with confidence

Comparator throughout is Row 3 (run 20260902T113613Z, 1x, variable-frame), whose numbers are
quoted from its own sec 6.

P1 - THE MODE IS ACTUALLY IN EFFECT (HIGH that it is measurable; MEDIUM that it took).
    There is NO documented status line: over the 10663 lines of Row 3's bin64-vrfSim.log a
    case-insensitive census scores `clock mode` 0, `run-to-complete` 0, `variable-frame` 0,
    `fixed-frame` 0, and `frame` exactly 1 - and that one is an unrelated FOM class
    declaration (`...DIGuyXCInteraction.ScenarioFrameClass`). The back end announces nothing
    about its clock mode at notify level 3, and the app never reads the mode. So the mode is
    proved instead by its ARITHMETIC
    SIGNATURE in the vendor log's own timestamps, which is a stronger observable than a
    status string anyway:
    (a) Parse every `[Www Mmm D HH:MM:SS 2026] <sim>.mmm` stamp in bin64-vrfSim.log (Row 3
        had 395 stamps / 85 distinct (wall, sim) pairs; expect the same order of magnitude).
        Take consecutive DISTINCT sim values and their differences.
    (b) PREDICTED: every sub-0.1 s difference is an integer multiple of 0.045455 to within
        the 0.001 s stamp resolution - i.e. the small-gap set collapses onto
        {0.045, 0.046, 0.091, 0.136, ...}. Row 3's was {0.032 ... 0.037}, median 0.033, and
        contained NO value at or above 0.045 except a single 0.037 maximum.
        A frame quantum still clustered at 0.033 = THE FIXTURE PARAMETER DID NOT REACH THE
        BACK END. That is F1.
    (c) SECONDARY, weaker, corroborating only: the LS fit of sim against wall over the
        stamped pairs. Row 3 measured slope 1.0003 sim-s per wall-s over a 181 s wall span.
        Any slope materially different from 1.0 is consistent with the mode being in effect;
        a slope of 1.0 is NOT by itself a falsifier (see P3), because FFRTC's rate depends
        entirely on how much wall time a frame costs.
    (d) The front end's Simulation Time Scale Toolbar is documented as DISABLED in this mode
        (p.123). Nobody is watching a GUI in this run, so this is recorded as an expected
        cosmetic consequence, not an observable.

P2 - THE ANSWER DOES NOT CHANGE (HIGH). This is the prediction the probe exists to test and
    the only one whose miss stops the work.
    (a) 3/3 TASKCMPLT (3 in vrfc2simapp.log, 3 in reports-captured.log), one per taskee
        001aa71b / 139aa71b / 670cfdb2. `-StopWhenComplete` fires; `earlyExit.fired` true.
    (b) COMPLETION ORDER IDENTICAL to Row 3: 1.BdeHQ first, then 1222.MechPlt, then
        114.MechCoy. Row 3's report offsets from orderPushedUtc were 117.47 / 129.63 /
        182.34 s WALL; its trace TSK completionT were 145.5 / 157.7 / 210.4 s. Under
        compression the WALL offsets shrink by the P3 ratio and are NOT compared to Row 3
        directly; THE ORDER IS.
    (c) ENDPOINTS within 1 m of Row 3's trace finals: 1.BdeHQ 34.608416,-116.699994 alt
        1121.1; 114.MechCoy 34.653915,-116.693388 alt 1116.8; 1222.MechPlt
        34.612956,-116.587783 alt 1026.6. POS==RPT <= 1 m x3; settled x3.
    (d) The terrain path is unchanged: exactly THREE :813 request lines (ids 7/8/9, "sent for
        3 vertices"), THREE :1466 replies with N=3 and distinct indices {0,1,2} and no
        `#k:none`, THREE :802 "all 3 vertices authored from terrain + 10 m clearance" lines
        with alts [1050.6, 1043.9, 1036.7] / [1126.7, 1126.8, 1126.9] /
        [1141.4, 1136.3, 1131.1] - the same values Rows 2c, 2cR and 3 produced
        character-for-character. Terrain sampling is a terrain query, not a clock query; if
        the clock mode moves these numbers, something is badly wrong.
    (e) 3 x "CreateRoute ... (3 pts)" + 3 x "MoveAlongRoute issued".

P3 - THE SIM CLOCK RUNS FASTER THAN THE WALL CLOCK (MEDIUM). Ratio > 1. NO UPPER BOUND IS
    PREDICTED AND ANY VALUE IS ACCEPTABLE AS LONG AS P2 HOLDS - the point of the probe is to
    find out what the number is, not to hit one.
    MEASUREMENT, stated in advance because after the fact any method looks chosen: least-squares
    slope of sim against wall over the DISTINCT (wall, sim) stamp pairs of
    bin64-vrfSim.log, restricted to stamps at or after the order push (Row 3: 1.0003 over
    181 s of wall, sim 6.318 -> 187.818). Cross-check, coarser: (last sim stamp - first sim
    stamp) / (last wall stamp - first wall stamp). Wall stamps are 1 s resolution, so quote
    the ratio to 2 significant figures and no more.
    Reference expectation, NOT a threshold: Row 3 spent ~33 ms of wall per 0.033 sim-s frame,
    an unknown fraction of which was targetFrameRate sleep. If frame COMPUTE cost is
    unchanged and the sleep goes away, the ratio lands somewhere in [1.4, ~10]. If p.211's
    "built in sleep" really does apply to FFRTC, the ratio is ~1.0 and the mode buys us
    repeatability but no speed - a legitimate, publishable result, NOT a miss.
    A ratio BELOW 1 (sim slower than wall) means a frame costs more than 45 ms of compute.
    Record it; it is not a falsifier on its own, but it is the explanation to reach for first
    if P2 (a) fails on the -RunSecs cap.

P4 - NO WALL-CLOCK TIMEOUT IN OUR APP FIRES (HIGH), and this is the INTEGRATION QUESTION the
    run really answers. THE APP HAS NO NOTION OF SIM TIME. Verified, not assumed:
      - `src/VrfFacade/VrfFacade.h` exports no clock accessor at all; the only time-related
        entries are `SetTimeMultiplier` and `SetExerciseStartTime`.
      - `src/VrfFacade/VrfFacade.cpp:478-482`, the whole body of `VrfFacade::Tick()`, whose
        first statement is:
            p_->exConn->clock()->setSimTime(p_->exConn->clock()->elapsedRealTime());
        THE APP'S OWN FEDERATE CLOCK IS HARD-WIRED TO ELAPSED WALL TIME. It does not read,
        and cannot read, the back end's exercise clock. Under FFRTC the two clocks diverge by
        construction, and every outbound message this federate stamps carries the wall-based
        value. THIS IS THE SINGLE MOST LIKELY PLACE FOR THIS PROBE TO BREAK.
      - `src/VrfC2SimApp/VrfC2SimService.cs:296-310`, `TickLoop()`: `Thread.Sleep(50)` at
        :308 - the
        app drains input and ticks the controller 20 times per WALL second, no matter how
        many frames the back end has run in between.
      - Every timeout below is `DateTime.UtcNow` or `Task.Delay`, i.e. WALL. There is no
        sim-time timeout anywhere in src/VrfC2SimApp.
    THE COMPLETE INVENTORY, with the budget and why it cannot fire (compression SHORTENS wall,
    so every wall budget gets MORE margin, not less - the one exception is called out):

    | setting | value | measured on | armed in this run? | verdict |
    |---|---|---|---|---|
    | TaskPredecessorTimeoutSeconds (VrfSettings.cs:123, used VrfC2SimService.cs:575, TaskSequencer.cs:97/112) | 600 s | wall | only if a task has a predecessor; the R9 order has no StartTime/SimulationTime element | cannot fire; Row 3's whole order finished 182 s wall after the push, and this run is expected shorter |
    | EngageFallbackSeconds (:138, used :1031-1045) | 300 s | wall | only armed for an ENGAGE task; R9 has none | cannot fire |
    | TerrainProfileTimeoutSeconds (:184, used :797 deadline, :1476-1481 expiry) | 10 s | wall | YES, three times, before any movement | THE ONE AT RISK - see the risk note below |
    | FanOutStragglerSeconds (:95, used :974/:1060) | 0 = OFF | wall | no | cannot fire |
    | BundleFlushMs (:152, used :1543) | 2000 ms | wall | no - BundlePositionReports is false | cannot fire |
    | cleanup drain (VrfC2SimService.cs:266) | 8 s | wall | shutdown only | after all movement |
    | post-delete flush (:267) | 1500 ms | wall | shutdown only | after all movement |
    | tick thread join (:290) | 5 s | wall | shutdown only | after all movement |
    | runner -SettleHoldSecs | 60 s | wall | yes | a HOLD, not a timeout; it lengthens the run, never fails it |
    | runner -RunSecs cap | 1800 s | wall | yes | see sec 3 |
    | runner AppJoin / OracleGate / InitDispatch / PushOrderListen / StopVrf (RunC2SimScenario.ps1:245-256) | 180 / 180 / 120 / 30 / 120 s | wall | yes | all before or after the observation window; unaffected by the clock mode |

    THE RISK, NAMED IN ADVANCE: TerrainProfileTimeoutSeconds is a 10 s WALL budget on a reply
    that has to come back from a back end which, in FFRTC, never yields the remainder of a
    frame. Compression does not shrink this window - it is wall on both sides - but a back end
    pinned at 100% CPU can delay the reply. Row 3's three replies all arrived (zero :1480
    lines). If they do not here, that is F3, and it is an INTEGRATION DEFECT, not a terrain
    defect.
    PREDICTED, therefore: ZERO lines matching `timed out`, `fallback`, `Fallback`, `Partial`,
    `skip`, `request not sent`, or `warn:` in vrfc2simapp.log; and the app-log census
    identical to Row 3's - 3 `fail:` (the C2SIMSDK deserialize noise), 3 "Can't create data of
    type", 0 Exception, 0 `warn:`, six "Create-altitude mode=Live" template lines (hard-coded
    at VrfC2SimService.cs:439; NOT a mode readout, expected, not a finding).

P5 - HYGIENE AS ROW 3 (HIGH). Runner exit 0; StopVrf exit 0 with "VR-Forces is DOWN"; RTI trio
    PIDs unchanged and explicitly preserved; both observers exit on the stop-file path; no new
    .dmp in C:\MAK\vrforces5.0.2\bin64 (newest stays
    vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp); no vrfSim* window titled `*.dmp`.
    bin64-vrfSim.log censuses: SocketException 0, "Waiting for nav data" 0, "empty route" 0,
    "Can't find entity route" 0, "invalid formation name" 1 (the standing baseline).
    Every stage exit code 0. `Get-ChildItem env:Vrf__*` count 0 before AND after.

## 6. NAMED RISK - the HLA caveat (this is a risk, NOT a prediction, and NOT a falsifier)

The Users Guide says Fixed-Frame Run-To-Complete "is suitable for distributed use only in
time-managed HLA federations" (p.123). OUR FEDERATION IS NOT TIME-MANAGED:
`vrfSim.mtl:142` sets `runInTimeManagementMode 0`, and the federation is vrfSim + vrfGui +
VrfC2SimApp + WatchVrf + ListenReports, none of which negotiates HLA time. Note the symmetry
that makes this less damning than it first reads: the SAME section forbids our CURRENT mode in
time-managed federations ("Do not use this mode in time-managed HLA federations", p.122). The
guide is partitioning modes by federation type; it is not asserting that FFRTC is broken
outside time management.

WHAT THIS PREDICTS OPERATIONALLY: the back end's sim clock advances independently of every
other federate's wall clock, so remote federates see entity updates arriving in BURSTS -
several sim-seconds of motion delivered inside one wall tick of the app's 20 Hz loop, and
dead-reckoned observer positions (watchvrf-trace.csv POS) extrapolated over wall time from
velocities expressed in sim units. ANALYSIS_P3_STEP_PROFILE_2026-09-01 sec (b) already
documents exactly this failure of POS at 5x (per-2-s follower steps up to 287 km) and rules
POS "MEASURED but UNUSABLE" during motion, truth only on stopped plateaus.

THEREFORE, PRE-COMMITTED: bursty or garbage POS during motion, a spiky RPT receipt cadence,
and a trace plateau onset that does not line up with the report offset the way it did at 1x
are ALL EXPECTED and are NOT misses. They become findings only if P2 fails. Endpoint
adjudication uses stopped-plateau POS and post-completion RPT, exactly as Row 3 did, and the
trace-plateau corroboration Row 3 relied on is explicitly SUSPENDED for this run - it is a
wall-sampled instrument pointed at a sim-time process.

## 7. FALSIFIER BRANCHES - PRE-NAMED

F1 - THE MODE IS NOT IN EFFECT: P1 (b) fails - the small-gap sim-stamp differences still
     cluster at ~0.033 and no value sits at 0.045455 or a multiple of it. THE FIXTURE
     PARAMETER PATH IS WRONG. STOP. Do not retune the frame time and do not re-run. Diagnose
     in this order: (i) the console log's LaunchVrf line - did it actually say
     `-Scenario TropicTortoise_FFRTC`; (ii) hash `TropicTortoise_FFRTC.scn` inside the
     deployed .scnx against sec 2 - was the right file deployed; (iii) whether the back end
     loaded the scenario at all (bin64-vrfSim.log scenario-load lines) - a load failure could
     leave it on a default clock; (iv) whether the back end even honours `(frame-mode ...)`
     from a .scn as opposed to `setFrameRateMode()` at runtime, which cgf.h:1203 offers and
     our facade does not expose. Branch (iv) is the interesting one and would make this a
     code question, not a fixture question.
F2 - P2 MISS: fewer than 3 TASKCMPLT, a different completion ORDER, an endpoint more than 1 m
     off, or a terrain line that differs. THE MODE CHANGES BEHAVIOUR. STOP - this is the
     answer the probe was built to get and it is a negative one. Record which sub-clause
     failed and do not attempt a second configuration.
F3 - ANY WALL-CLOCK TIMEOUT FIRES: a :1480 terrain timeout, a :807 Partial/Fallback, a
     predecessor timeout, an engage fallback, or any `warn:` line. INTEGRATION DEFECT -
     sim-time vs wall-time. STOP and record WHICH timeout, its budget, and the wall interval
     it was measuring. This is a defect in OUR app (VrfFacade.cpp:480 and the wall-clock
     inventory above), not in the vendor mode.
F4 - THE RUN DOES NOT COMPLETE INSIDE THE 1800 s CAP. Either the sim is running slower than
     wall (check P3's ratio first) or something is wedged. Record the P3 ratio and the last
     sim stamp reached, then STOP.
F5 - ANY CRASH OR INFRASTRUCTURE FAILURE - the MAK dump prompt, a non-zero runner or StopVrf
     exit, an observer that never reached the stop-file path, an RTI PID change, a killed
     federate. Infrastructure, not an answer. Dump prompt:
     `pwsh -File scripts\AnswerCrashDumpDialog.ps1` then `pwsh -File scripts\StopVrf.ps1` per
     RUNBOOK 0.5.12 (ALWAYS Yes). STOP; after two infrastructure failures this session, stop
     entirely.

NOTE: a P3 ratio of exactly 1.0 is NOT a falsifier and does not fire F1 on its own - F1 is
decided by the frame quantum in P1 (b), which is a direct reading of the mode, not by the
rate, which is a reading of how expensive a frame turned out to be.

## 7A. AMENDMENTS BEFORE LAUNCH (live executor, 2026-09-02, registered BEFORE any launch)

Sections 0-7 above are UNCHANGED from registration commit f1f0f38 - the attestation in
sec 4 still holds. Everything in this section was added by the live executor before the
run and committed before the launch command was issued.

A1 - FRAME TIME 0.045455 -> 0.033333 (one-line regeneration, sec 2). REASON: sec 0 C3
     already records that 0.045455's stated justification ("matches today's variable-frame
     step at the 22 Hz target") is FALSIFIED BY MEASUREMENT - Row 3's own quantum is
     ~0.033 sim-s (~30.3 Hz), so 0.045455 is a 38% COARSER frame and would have moved a
     SECOND variable (frame length, hence integration fidelity) alongside the clock mode.
     0.033333 is the measured Row 3 quantum, which keeps the probe to ONE variable: the
     MODE, at the frame length the box is already running.

     REGENERATION IS PROVED DETERMINISTIC BEFORE THE SWAP. The 0.045455 fixture was
     rebuilt from stock TropicTortoise.scnx into a scratch directory with the committed
     code and compared part-by-part against the committed
     tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx:
       11 parts, IDENTICAL 11, CHANGED 0, only-committed 0, only-regenerated 0
       .scn SHA-256 2b50495d8b9e280ae04f2e6e280b38a65783a52fe511c53c3c0ac3515e294a52
       = the sec 2 value, reproduced exactly.

     THE NEW FIXTURE (same path, same name, so sec 2's deployment line and sec 4's
     -Scenario are unchanged):
       tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx, 7112 bytes
       .scn SHA-256 3d8960732bf78cbde02e581c9f04b93e5b926ae3db9cd5c9d679859fb99107ad
       .scn 1562 bytes, 0 non-ASCII, 0 CR (LF exactly as the vendor writes it)
       10 non-.scn parts byte-for-byte identical to stock TropicTortoise: 10/10
       diff vs the 0.045455 fixture: EXACTLY ONE LINE,
         -   (frame-time 0.045455)   +   (frame-time 0.033333)
       diff vs stock TropicTortoise.scn: the same TWELVE lines sec 2 lists, with
         0.033333 in place of 0.045455.
     The sec 2 verification snippet still applies with the new expected values:
       expected: 3d8960732bf78cbde02e581c9f04b93e5b926ae3db9cd5c9d679859fb99107ad
                 ['   (frame-mode "fixed-frame-run-to-complete")', '   (frame-time 0.033333)']

A2 - P1 (b) IS RESTATED, because the naive form of the test CANNOT PASS even if the mode
     is in effect. The vendor prints sim time to THREE DECIMALS. A true fixed grid of
     q = 0.033333 does not print as a constant 0.033: three frames are exactly 0.100 s, so
     consecutive grid points print as 0.033, 0.033, 0.034, repeating. A "median gap" test
     therefore cannot separate the modes at all (both give 0.033), and the brief's proposed
     ">= 95% of gaps within +/-0.0005 of 0.033333" is UNREACHABLE BY CONSTRUCTION - a
     perfect grid tops out near 67% on that statistic. WHAT SEPARATES THE MODES IS THE
     SPREAD, and it is tested two independent ways by tools/analysis/frame_gaps.py
     (new, this session; it reuses step_profile.vendor_log so both instruments share one
     parser):

     TEST A - GAP CENSUS, n = the sub-0.06 s gaps between consecutive DISTINCT sim stamps.
       On a fixed grid of 0.033333 a one-frame gap can print ONLY as 0.033 or 0.034 -
       no other value is arithmetically reachable, and two frames (0.067) is already above
       the 0.06 filter. THRESHOLD: >= 95% of small gaps in {0.033, 0.034}.
       ROW 3 MEASURED (recomputed this session with the fixed parser, see A3):
         n = 33; census 0.032 x2, 0.033 x17, 0.034 x10, 0.035 x3, 0.037 x1;
         min 0.032 median 0.033 max 0.037 sd 0.0010;
         IN {0.033, 0.034}: 27/33 = 81.8%.

     TEST B - GRID RESIDUAL, n = the distinct sim stamps themselves (85 for Row 3, ~2.5x
       Test A's sample count and independent of WHICH frames happened to be logged). On a
       fixed grid every stamp satisfies t = k*q + phase, so its residual about the grid is
       bounded by the 0.0005 s print rounding. The phase is FITTED (circular mean of
       t mod q), not assumed zero, so a clock that did not start at 0 still passes.
       THRESHOLD: >= 95% of distinct stamps with |residual| <= 0.0005 s, AND resultant
       length R >= 0.99.
       ROW 3 MEASURED: R = 0.3760; |residual| <= 0.0005 for 3/85 = 3.5%;
       residual sd 0.00702 s (uniform scatter on this grid would be 0.00962).

     F1 (sec 7) IS THEREFORE DECIDED BY: Test A < 95% in {0.033, 0.034} AND Test B
     R < 0.99 -> the fixture parameter did not reach the back end, STOP and diagnose per
     sec 7 F1 (i)-(iv). Both tests passing = the mode is in effect. A SPLIT RESULT (one
     passes, one fails) is not pre-named; it would be recorded as unexplained, not
     adjudicated.
     P1 (c) is unchanged: the LS slope is corroborating only, and a slope of 1.0 is not a
     falsifier.

A3 - INSTRUMENT DEFECT FIXED AND THE FIX PROVED (the false-green rule; sec 1 flagged this
     defect and left it for the live executor). tools/analysis/step_profile.py:41 hard-coded
     STAMP_RE = ...\[Tue Sep  1 ...\], which parses ZERO stamps from any log not written on
     2026-09-01 and reports that silently as a clean result. The weekday / month / day /
     year are now unpinned; only HH:MM:SS and the sim float are captured, so the capture
     group signature is unchanged and the wall axis is still seconds-of-day (a run crossing
     midnight would wrap - noted, not applicable here).
     PROVED BEFORE USE, by re-running the fixed script on ROW 3's OWN LOG
     (runs/20260902T113613Z_run/bin64-vrfSim.log) and reproducing sec 0 C3's numbers
     character-for-character:
       lines 10663, stamped 395, discarded 0, DISTINCT STAMPS 85
       tick proxy n=33, min 0.032, MEDIAN 0.033, max 0.037
       LS clock slope 1.0003 sim-s/wall-s, resid sd 0.31, max 0.56
     That is the 0.033 median / 85 pairs the prereg was written against. The instrument
     reproduces; only now is it trusted on the new run.

A4 - APP NUMBERS. The marker in docs/OPUS_EXECUTION_PLAN.md Appendix B:1626 reads
     *** NEXT FREE: 3726 *** at the time of this amendment, so the expected block is
     3726-3732. The runner consumes and advances it; the actual wasValue/newValue from
     run-manifest.json is recorded in sec 8.

A5 - DEPLOYMENT. Exactly the sec 2 command, one new file under C:\MAK, nothing existing
     modified:
       Copy-Item tools\FixtureGen\frame_variants\TropicTortoise_FFRTC.scnx `
                 C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise_FFRTC.scnx
     The SHA-256 of the DEPLOYED .scnx and of the .scn inside it are recorded in sec 8.
     Stock TropicTortoise.scnx is not touched.

A6 - RUNNER PASSTHROUGH RE-VERIFIED (the brief required a STOP if absent):
     scripts/RunC2SimScenario.ps1:223 declares [string] $Scenario = 'TropicTortoise' and
     :1744 passes '-Scenario', $Scenario to LaunchVrf.ps1, which at :175-176 resolves it to
     C:\MAK\vrforces5.0.2\userData\scenarios\<name>.scnx, HARD-CHECKS its existence at
     :235-239 (Say-Fail + $hardFail), and passes it as --scenarioFileName at :344.
     :1186 records inputs.scenario in the manifest. No STOP required.

NOTHING ELSE MOVES. Same init, same order, same binary, same bridge, TimeMultiplier 1,
no env override, -RunSecs 1800, -SampleSecs 2, -StopWhenComplete. P2, P3, P4, P5 and the
sec 6 risk note stand exactly as registered, except that every "0.045455" in P1/P3 now
reads "0.033333".
## 8. Outcome (written from the run directory artifacts, AFTER the run)

VERDICT: **FIXED-FRAME RUN-TO-COMPLETE WORKS WITH THIS FEDERATION.** The mode was in effect
(P1 pass, decisively, on both independent tests). The answer did not change (P2 pass on every
sub-clause). The sim clock ran roughly 9-10x the wall clock while the units were moving and
faster still when they were not (P3 pass, ratio > 1). No wall-clock timeout in our app fired
(P4 pass). Hygiene was clean (P5 pass). NO FALSIFIER FIRED.

RUN: `runs/20260902T140808Z_run`, launched 2026-09-02T14:08:08.871Z, finished 14:12:48.169Z
(4 min 40 s wall end to end, vs Row 3's 7 min 15 s). Order pushed 14:10:33.360Z. Observation
window closed EARLY at t+66 s of the 1800 s cap (65.7 s used). Runner EXIT=0.
appNumbers 3726-3732 (marker 3726 -> 3733; 3733 then consumed by the post-run ResetVrf sweep,
marker -> 3734). `env:Vrf__*` count 0 before AND after.

AMENDED PREREG COMMIT (this file, sections 0-7A, registered BEFORE launch): 2030ebd.
FIXTURE: tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx, 7112 bytes; .scn SHA-256
3d8960732bf78cbde02e581c9f04b93e5b926ae3db9cd5c9d679859fb99107ad. DEPLOYED COPY
C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise_FFRTC.scnx: .scnx SHA-256
D27E540F8BCCAA2EBDD33C63CF062CB4842DBE1937B7AA3DAC1C20F9C990B0B9, inner .scn SHA-256
3d896073... (matches), frame lines read
`(frame-mode "fixed-frame-run-to-complete")` / `(frame-time 0.033333)`.
Stock TropicTortoise.scnx untouched (6932 bytes, mtime 2026-07-14 12:45).

### P1 - THE MODE IS IN EFFECT. PASS, on both tests, by a wide margin.

`python tools/analysis/frame_gaps.py . <run>` (thresholds fixed in sec 7A A2 BEFORE the run):

| statistic | THIS RUN (FFRTC 0.033333) | ROW 3 (variable-frame) | threshold |
|---|---|---|---|
| vendor-log lines / stamped / distinct sim stamps | 21832 / 401 / 85 | 10663 / 395 / 85 | - |
| TEST A gap census | 0.033 x18, 0.034 x14 | 0.032 x2, 0.033 x17, 0.034 x10, 0.035 x3, 0.037 x1 | - |
| TEST A min / median / max / sd | 0.033 / 0.033 / 0.034 / 0.0005 | 0.032 / 0.033 / 0.037 / 0.0010 | - |
| TEST A in {0.033, 0.034} | **32/32 = 100.0%** | 27/33 = 81.8% | >= 95% |
| TEST B resultant length R | **0.9986** | 0.3760 | >= 0.99 |
| TEST B \|residual\| <= 0.0005 s | **85/85 = 100.0%** | 3/85 = 3.5% | >= 95% |
| TEST B residual sd | 0.00029 s | 0.00702 s | (uniform would be 0.00962) |
| TEST B fitted phase | 0.000001 s | 0.014813 s | - |

Every one of the 85 distinct sim stamps lies on an exact integer multiple of 0.033333 s to
within the vendor's 0.001 s print rounding, and the FITTED PHASE CAME BACK AT ONE
MICROSECOND - i.e. the grid is not merely regular, it is zero-phased: the exercise clock
starts at 0 and advances in exact multiples of the frame time we put in the .scn. Row 3's
same 85 stamps scatter (R = 0.376, 3.5% on grid). The fixture parameter reached the back end.
P1 (c), corroborating only: LS slope 10.1784 sim-s/wall-s vs Row 3's 1.0003.

### P2 - THE ANSWER DOES NOT CHANGE. PASS on (a) through (e).

(a) 3/3 TASKCMPLT in vrfc2simapp.log AND 3/3 in reports-captured.log, one per taskee
    (001aa71b / 139aa71b / 670cfdb2). `-StopWhenComplete` fired; the runner closed the window
    early.
(b) COMPLETION ORDER IDENTICAL to Row 3, taskee for taskee: 1.BdeHQ (670cfdb2, task ...0003)
    then 1222.MechPlt (001aa71b, ...0001) then 114.MechCoy (139aa71b, ...0002).
(c) ENDPOINTS (trace finals, from stopped plateaus, vs Row 3; tolerance 1 m):

    | taskee | this run | vs Row 3 | alt | POS vs RPT | settled |
    |---|---|---|---|---|---|
    | 1.BdeHQ | 34.608416,-116.699993 | 0.09 m | 1121.1 (+0.0) | 0.00 m | yes |
    | 114.MechCoy | 34.653915,-116.693388 | 0.00 m | 1116.8 (+0.0) | 0.00 m | yes |
    | 1222.MechPlt | 34.612956,-116.587783 | 0.00 m | 1026.6 (+0.0) | 0.00 m | yes |

(d) THE TERRAIN PATH IS CHARACTER-FOR-CHARACTER UNCHANGED: three requests (ids 7/8/9, "sent
    for 3 vertices"), three replies each with 3 samples and distinct indices {0,1,2} and no
    `#k:none`, three "all 3 vertices authored from terrain + 10 m clearance" lines with alts
    [1050.6, 1043.9, 1036.7] / [1126.7, 1126.8, 1126.9] / [1141.4, 1136.3, 1131.1] - the
    values this prereg predicted and that Rows 2c, 2cR and 3 produced.
(e) 3 x "CreateRoute ... (3 pts)" + 3 x "MoveAlongRoute issued".

STRONGEST SINGLE PIECE OF P2 EVIDENCE: a whole-file diff of vrfc2simapp.log against Row 3's,
with wall timestamps normalised. BOTH FILES ARE 103 LINES AND DIFFER IN SIX PLACES, NONE
SEMANTIC: the RDTSCP tick-rate probe, one .NET host-startup line that logged in a different
order, a thread id, the three run-scoped VRF_UUIDs the routes were issued against, and the
cleanup duration in ms. Nothing else moved.

### P3 - THE SIM CLOCK RUNS FASTER THAN THE WALL CLOCK. PASS, ratio > 1.

Measured as pre-registered, plus two cross-checks:

| instrument | this run | Row 3 |
|---|---|---|
| LS slope of sim on wall, distinct stamp pairs (the pre-registered measure) | **10.18** | 1.0003 |
| coarse (last sim - first sim)/(last wall - first wall) | 9.7 (193.698 sim-s / 20 wall s) | 1.0 (181.500 / 181) |
| WALL FROM ORDER PUSH TO LAST TASKCMPLT | **20.18 s** | **182.34 s** -> **9.04x** |
| wall to 1st / 2nd / 3rd TASKCMPLT | 12.75 / 14.05 / 20.18 s | 117.47 / 129.63 / 182.34 s |
| per-taskee ratio | 9.21 / 9.23 / 9.04 | - |

To two significant figures the movement-phase compression is **9 to 10x**. Fitting a constant
ratio plus a fixed non-scaling overhead to the first and third completion offsets gives ratio
8.7 with overhead -0.8 s, i.e. the completion offsets are consistent with a constant ratio and
NO measurable fixed overhead.

THE RATIO IS LOAD-DEPENDENT, AND THAT IS MEASURED, NOT ASSUMED. Report receipt intervals
(a sim-periodic report, ~61.2 sim-s, received on wall time) split cleanly by phase:

    before the order (idle)          n=  88  mean 3.457 wall s
    fully inside the movement window n= 122  mean 5.380 wall s
    after the last TASKCMPLT (idle)  n= 797  mean 3.799 wall s

Frames are cheaper when nothing is moving, and FFRTC advances a fixed 0.033333 s per frame
regardless of what a frame costs, so the clock runs FASTER when idle. Binned over the trace
the implied ratio rises from ~12.7 during movement to ~16.6 after completion. p.211's "built
in sleep" therefore does NOT apply to Fixed-Frame Run-To-Complete: the tension recorded in
sec 1 resolves in favour of sec 3.4.3's plain reading, and the ratio landed inside the
[1.4, ~10] band sec 5 named as a reference expectation.

### P4 - NO WALL-CLOCK TIMEOUT FIRED. PASS.

Census of vrfc2simapp.log, THIS RUN then ROW 3, identical in every cell:
`timed out` 0/0; `Fallback` 0/0; `Partial` 0/0; `skip` 0/0; `request not sent` 0/0;
`warn:` 0/0; `Exception` 0/0; `fail:` 3/3 (the C2SIMSDK deserialize noise);
"Can't create data of type" 3/3; "Create-altitude mode=Live" 6/6 (hard-coded template line,
not a mode readout, expected). The 3 `fallback` hits in each file are the words "-> Live
fallback" INSIDE the request lines themselves - descriptive text, not a fired fallback.

Every armed wall-clock budget, fired or not:
- TerrainProfileTimeoutSeconds 10 s - ARMED 3 times, FIRED 0 times. **3/3 replies returned.**
  LATENCY: not directly measurable - vrfc2simapp.log carries no timestamps and the vendor log
  does not record the terrain-profile interaction at all (0 matching lines). The bound that IS
  established: each reply arrived inside its own 10 s wall budget (otherwise the :1476-1481
  expiry path would have logged and the alts would have come from Live fallback, and neither
  happened), and all three requests, all three replies, all three route creations, all three
  move dispatches AND the first unit's entire move completed inside 12.75 s wall of the order
  push. THE NAMED RISK DID NOT MATERIALISE.
- TaskPredecessorTimeoutSeconds 600 s - not armed (no predecessor in the R9 order).
- EngageFallbackSeconds 300 s - not armed (no ENGAGE task).
- FanOutStragglerSeconds - OFF. BundleFlushMs - not armed (bundling off).
- runner -SettleHoldSecs 60 s - elapsed (60.1 s), a hold, not a failure.
- runner -RunSecs 1800 s cap - not reached (66 s used).
- shutdown drains (8 s / 1500 ms / 5 s) and runner stage budgets - all after movement, all met.
CONCLUSION ON THE INTEGRATION QUESTION: VrfFacade::Tick()'s hard-wiring of the federate clock
to elapsed REAL time (VrfFacade.cpp:480) and the app's 20 Hz wall TickLoop did NOT break under
a sim clock running ~9-10x wall. Compression only ever gives a wall budget MORE margin, and
the one budget it does not shorten - the terrain-profile round trip - was met three times.

### P5 - HYGIENE. PASS.

Runner EXIT=0. Every stage exit code 0: RtiProbe, LaunchVrf, WatchVrf-precheck,
WatchVrf-trace, ListenReports, PushInit, VrfC2SimApp (clean resign, code 0), PushOrder,
StopIface, StopVrf. StopVrf reported "VR-Forces is DOWN (graceful quit; no process was
force-killed)". Both observers exited on the stop-file path and were never killed. RTI trio
PIDs UNCHANGED and explicitly preserved: rtiAssistant 41336, rtiexec 224608,
rtiForwarder 76620. No new .dmp in C:\MAK\vrforces5.0.2\bin64 (newest is still
vrfSim5.0.2-MSVC++15.0_64-249613-70668.dmp, 2026-09-02 06:00). bin64-vrfSim.log censuses,
THIS RUN / ROW 3: SocketException 0/0, "Waiting for nav data" 0/0 (the disabled NavArea
confirmed live), "empty route" 0/0, "Can't find entity route" 0/0, "invalid formation name"
1/1 (the standing baseline), FATAL 0/0.
POST-RUN SWEEP: `tools/ResetVrf 3733` - joined clean, BackendCount=0, discovered 0 reflected
(0 deletable, 0 nil), exit 0, resigned cleanly. ZERO LEFTOVERS. Same caveat the rung-1
executor recorded as finding D: run AFTER StopVrf, this proves NO STALE FEDERATE and nothing
about scenario contents.

### FALSIFIERS - ALL PRE-NAMED, NONE FIRED

F1 (mode not in effect) NOT FIRED - Test A 100% vs the 95% threshold, Test B R = 0.9986 vs
   the 0.99 threshold. F2 (answer changes) NOT FIRED - 3/3, same order, endpoints within
   0.09 m, terrain lines identical. F3 (a wall-clock timeout fires) NOT FIRED - zero of every
   pattern. F4 (no completion inside the cap) NOT FIRED - 66 s of an 1800 s cap. F5 (crash or
   infrastructure failure) NOT FIRED - no dump prompt, no new .dmp, all exits 0, RTI intact.

### OBSERVER BEHAVIOUR (recorded as data, sec 6 pre-committed that none of it is a miss)

- RPT cadence: 1095 intervals (vs Row 3's 159) over a shorter wall trace - 26 reports per
  entity vs 4. Whole-trace mean interval 4.045 wall s (sd 0.813, min 3.00, max 7.00) vs Row
  3's 61.307 (sd 0.713). Spiky exactly as predicted, and the spikiness is informative: it is
  the load-dependence of the ratio, not noise.
- POS: 50 samples per entity vs Row 3's 126; trace sample cadence itself unchanged at ~2 s
  wall. THE PRE-COMMITTED EXPECTATION OF WORSE POS DID NOT MATERIALISE - per-sample steps for
  the three taskees during motion were mean 190 m / p50 102 m / MAX 700 m in this run, against
  Row 3's mean 651 m / p50 9 m / MAX 82.6 km (1.BdeHQ alone). Dead reckoning behaved BETTER
  under FFRTC than at 1x here. CAVEAT ON THAT COMPARISON: 30 in-motion samples vs 267, so this
  run had far less opportunity to catch an outlier; treat it as "no evidence of degradation",
  not as a demonstration of improvement.
- Endpoint adjudication used stopped-plateau POS and post-completion RPT, as pre-committed;
  the trace-plateau corroboration was suspended and not used.

### UNEXPLAINED, AND LEFT UNEXPLAINED

U1 - THE THREE RATIO INSTRUMENTS DO NOT FULLY AGREE ON THE MOVEMENT-PHASE NUMBER. Wall to
     last TASKCMPLT says 9.04x; the vendor-log stamps say 9.7 (coarse) to 10.18 (LS); the RPT
     intervals fully inside the movement window say 11.4x. The direction of the disagreement
     is consistent (the RPT figure is the one contaminated by idle time, since a 5-6 s
     interval cannot be contained cleanly in a 20 s window and both of its ends straddle the
     ramp), and three candidate causes are not separated by this run's artifacts: (i) the
     assumed 61.2 sim-s report period is approximate; (ii) window-edge straddling; (iii) burst
     delivery distorting WatchVrf's wall receipt stamps, which sec 6 pre-committed as
     expected. NOT ADJUDICATED. The pre-registered measure is the LS slope and it is quoted as
     such; the operationally meaningful number is the completion wall time, 20.18 s vs 182.34.
U2 - CARRIED FORWARD, UNTOUCHED BY THIS RUN: sec 0 C3/C4's contradiction between Table 71's
     documented targetFrameRate default of 22 Hz and the ~30.3 Hz quantum Row 3 actually ran
     under variable frame. This probe replaced the variable frame entirely and says nothing
     about it.
C1 - A CORRECTION, not an anomaly: the vendor log's wall stamps are LOCAL TIME, not UTC. Row
     3's first stamp reads 07:38:38 against an orderPushedUtc of 11:38:37Z, and this run's
     reads 10:08:24 against a startUtc of 14:08:08Z (offset -4). Every ratio above is built
     from stamp DIFFERENCES and is unaffected, but any future cross-reference of a vendor-log
     stamp to a UTC artifact must convert.

### WHAT THIS BUYS THE HEADLESS GOAL

The vendor's named mode for "run a simulation overnight and view the results the following
day" works with this federation as it stands, un-time-managed, with no code change and a
two-line scenario edit - and it is repeatable by construction (the clock is now a deterministic
grid, R = 0.9986, phase 0). It cost 20 s of wall where Row 3 spent 182 s for an identical
result. The next question this raises, and does NOT answer, is whether a COARSER frame time
buys more compression without moving the answer - that is a new single-variable probe
(frame-time only), not a re-run of this one.
