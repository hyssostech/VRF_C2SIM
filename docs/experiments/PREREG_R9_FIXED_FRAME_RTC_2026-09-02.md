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

## 8. Outcome (written from the run directory artifacts, AFTER the run - empty at registration)

(unwritten)
