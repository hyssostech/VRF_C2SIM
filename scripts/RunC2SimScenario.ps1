<#
.SYNOPSIS
    THE ONE-BUTTON UNATTENDED C2SIM -> VR-Forces RUNNER. Sequences stages 1-8 of
    docs/HEADLESS_RUN_PLAN.md sec 1 with zero humans in the UI, and leaves a
    timestamped run directory full of EVIDENCE.

.DESCRIPTION
    Contract (HEADLESS_RUN_PLAN.md sec 2):

        pwsh -File scripts\RunC2SimScenario.ps1 -Init <init.xml> -Order <order.xml> -RunSecs 600

    THIS SCRIPT DOES NOT SCORE THE RUN. It collects: the WatchVrf POS/CON trace
    (the movement oracle), the ListenReports capture (what the interface told
    C2SIM), the PushOrder bus log, the VrfC2SimApp log, and a run manifest
    recording every appNumber consumed, both clocks, tool identities, exact input
    paths and the exit code of every stage. HEADLESS_RUN_PLAN.md sec 4a was RATIFIED
    2026-07-19 (before any data existed), and sec 4a.6 says run 1 is a MEASUREMENT,
    not an acceptance test - so NO threshold from sec 4a (50 m / 250 m / 25 m / 5x /
    200 km/h) appears anywhere in this file, BY DESIGN AND STILL. Ratification did
    NOT make this script a scorer. Keeping collection and scoring in separate
    programs is what allows a trace to be re-scored under an amended criterion
    without re-running the simulation - and it removes any temptation to tune a
    threshold in the same edit that produces the data. A separate scorer consumes
    the manifest + trace.

    STAGE ORDER (each constraint below cost a live session; do not "improve" them):
      0  validate every input, up front, BEFORE VR-Forces is launched
      1  pre-flight process inventory (RUNBOOK 0.5.0) - REFUSE on a pre-existing
         vrfSimHLA1516e / vrfGui / vrfLauncher. -AllowExistingVrf is NOT used and
         is NOT offered: it is the false-READY trap.
      2  allocate EVERY appNumber from the single marker in
         OPUS_EXECUTION_PLAN.md Appendix B and ADVANCE the marker, BEFORE any join
      3  LaunchVrf.ps1 (combined mode) - or, with -VrfProfile 5.2, LaunchVrf52.ps1
         (independent mode); same exit contract either way
      4  oracle pre-check, passive (RUNBOOK 0.5.7) - ADVISORY by default, see below
      5  WatchVrf + ListenReports START HERE, BEFORE the init, so unit births are
         in the trace
      6  PushInit, then start VrfC2SimApp (RUNBOOK sec 3: init first, app late-joins)
      7  post-init ORACLE GATE - the RUNBOOK 0.5.7 CORRECTED criterion, applied to
         the live trace: a POS line with REAL lat/lon (not NaN, not the 90/-90
         pole), retried up to ~3 minutes
      7b ON THE STAGE-7 FAILURE PATH ONLY: the RUNBOOK 0.5.7 STRONGER CHECK, run
         as a DISAMBIGUATION DIAGNOSTIC (tools/CreateOne). It does NOT rescue the
         run - see the block below.
      8  PushOrder, then observe for -RunSecs (or, with -StopWhenComplete, until
         every order task has a TERMINAL status report (TASKCMPLT or TASKABRT),
         every taskee has one, and -SettleHoldSecs has passed - RunSecs stays
         the cap)
      9  teardown in a finally: StopIface (clean resign), wait for the app to
         exit, hold until StopIface + TrailSecs, then TELL THE OBSERVERS TO STOP
         (touch <runDir>\observers.stop - they resign cleanly on seeing it), THEN
         StopVrf.ps1. Their duration argument is the CAP, not the run length
         (docs/RUNNER_TURNAROUND_2026-09-01.md).

    *** DEVIATION FROM THE BRIEF, DELIBERATE AND FLAGGED (stage 4 vs stage 7) ***
    The brief asks for a hard oracle pre-check before anything is scored. RUNBOOK
    0.5.7 states that on a stock TropicTortoise load the baseline objects are
    POSITIONLESS ("90.000000,-90.000000" and NaN), so a passive real-coordinate
    pre-check run BEFORE any C2SIM unit exists is EXPECTED TO FAIL - "the STRONGER
    CHECK below is IN PRACTICE THE ONLY CHECK THAT CAN PASS ON A STOCK
    TropicTortoise LOAD". Making that fatal would abort every healthy run.
    Therefore:
      - stage 4 (pre-init, passive) is ADVISORY: it proves the oracle can JOIN and
        DISCOVER, and its degenerate-coordinate result is RECORDED, not fatal.
        Pass -StrictPreInitOracle to make it fatal if that is ruled the criterion.
      - stage 7 (post-init) applies the 0.5.7 CORRECTED criterion for real, against
        the scoring trace itself, once C2SIM units exist. It is FATAL.
    HEADLESS_RUN_PLAN sec 2 says the pre-check runs "before anything is SCORED",
    not "before the init is pushed", and sec 4a.6 lists it as a run-VALIDITY gate -
    both of which stage 7 satisfies.

    *** STAGE 7b - THE STRONGER CHECK, AS A FAILURE-PATH DIAGNOSTIC ***
    RUNBOOK 0.5.7 "STRONGER CHECK" (tools/CreateOne a throwaway M1A2 at a known
    coordinate, then confirm WatchVrf emits a POS line for that uuid with REAL
    coordinates) is now implemented, but ONLY on the stage-7 failure path.

    WHY IT EXISTS: if the interface DISPATCHES units (stage 6d says N units
    queued) and yet NO real-coordinate POS line ever appears, stages 4 and 7
    alone CANNOT distinguish
        (a) THE ORACLE IS BLIND        - a federation / discovery problem, from
        (b) OUR INIT CREATED NOTHING   - a type-mapping / creation problem.
    Those have different causes and different fixes. A KNOWN-GOOD entity injected
    by CreateOne separates them: if IT reads real coordinates and our C2SIM units
    do not, the oracle is fine and the creation layer is the suspect; if not even
    CreateOne appears, the oracle is blind.

    WHY FAILURE-PATH ONLY: RUNBOOK 0.5.7 requires that the throwaway never enter
    a SCORED trace, and prescribes a RELAUNCH to clear it. On the happy path we
    therefore want no throwaway in the trace at all. On the failure path the run
    is already unscored (stage 7 has failed and the runner is about to exit 3), so
    the throwaway costs nothing and buys the disambiguation.

    IT DOES NOT RESCUE THE RUN. Stage 7b changes the EXIT REASON and the RECORDED
    EVIDENCE. The run still fails with exit 3, on every verdict.

    NO RELAUNCH CYCLE IS ADDED. The RUNBOOK's relaunch exists to purge the
    throwaway; teardown already makes that moot. Stage 7b runs inside the try
    block, so the finally ALWAYS follows and brings VR-Forces down via
    StopVrf.ps1. The entity lives only in the back-end's in-memory scenario - it
    is never saved to the scenario file - so it dies with vrfSimHLA1516e, and the
    next run's LaunchVrf reloads TropicTortoise from file. Adding a relaunch here
    would only relaunch something the runner is about to shut down anyway.

    NON-NEGOTIABLES HONOURED HERE:
      - NOTHING is ever force-killed. Not the app, not a federate, not VR-Forces.
        A federate that will not exit is REPORTED, loudly, and left alone
        (RUNBOOK sec 0).
      - rtiAssistant / rtiexec / rtiForwarder are NEVER touched, never even
        refused on (RUNBOOK 0.5.2). They are inventoried for the manifest only.
      - Teardown runs on every path, success or failure, via finally. CAVEAT,
        stated rather than glossed: PowerShell does NOT reliably run a top-level
        finally on Ctrl-C. A Ctrl-C'd run can therefore leave VR-Forces up and the
        interface JOINED. Recovery is scripts/StopVrf.ps1 (and tools/StopIface if
        the interface is still up) - never a force-kill.
      - Every external invocation's exit code is CAPTURED and RECORDED. None is
        assumed. A MISSING exit code (stage timed out, or would not start) is
        never treated as success - see Test-StageProduced.
      - NO stage waits on a PROCESS TREE. Every foreground stage waits for its
        DIRECT CHILD ONLY, under a per-stage timeout (-StageTimeoutSec). See the
        big header block above Invoke-External for the 47-minute deadlock this
        rule was written from.

.PARAMETER Init
    C2SIM initialization XML. Default data/R9_Mojave_Lean_Initialization.xml.
    HEADLESS_RUN_PLAN 4a.0: the LEAN file (6 units) supersedes sec 3's full file
    (158 unit/actor references); both contain all three taskee UUIDs, the lean one
    keeps 152 irrelevant units out of the trace.

.PARAMETER Order
    C2SIM order XML. Default data/R9_Mojave_UnitMove_Order.xml. Three MOVE tasks
    against three taskees (4a.0), legs ~556-578 m.

.PARAMETER VrfProfile
    WHICH VR-FORCES STACK the whole pipeline runs on: '5.0.2' (default, the historical
    path - unchanged in every observable way) or '5.2'. It is the ONLY selector, and it
    derives: the roots (vrforces5.2d + vrlink5.10 + makRti5.0.1), the per-process
    environment (5.2 PATH prefix, MAK_VRFDIR/MAK_VRLDIR/MAK_RTIDIR, the SHARED rtiexec
    rid config\rid-501-rtiexec-min.mtl, RTI_ASSISTANT_DISABLE), the launch and stop
    scripts (LaunchVrf52.ps1 / StopVrf52.ps1), a headless-rtiexec stage
    (StartRtiExec52.ps1), the Release-5.2 build tree of every bridge-linked binary, the
    config-file federation identity (NO federation argument - execName comes from
    MAK-ONE-2025-Config.xml) and the 5.2 type-map table. Passing -VrfRoot / -VrLinkRoot /
    -RtiDir / -Federation beside it is REFUSED: a mixed environment loads the wrong DLLs
    silently. See docs/RUNBOOK.md "5.2 profile".

    THE RTI CONNECTION MODE IS NOT A KNOB (2026-09-04). UG52 5.5.1 p190: "You cannot use
    the MAK RTI in lightweight mode with VR-Forces". Every lightweight 5.2 run reflected
    ZERO entities; with MAK RTI 5.0.1 in rtiexec mode the same observer reflected 62
    (PREREG_52_RTIEXEC_2026-09-04). The profile therefore always runs rtiexec mode. The
    VR-Forces-level interface address is SEPARATE, TUNABLE and NOT part of that repair -
    see -DeviceAddress, which run 3857 falsified observer-side and which now defaults to
    passing nothing at all.

.PARAMETER NoGui
    5.2 only: launch the back end without vrfGui. Default OFF - the GUI is the one channel
    that shows whether the entities the control path creates are really there while the
    5.2 observation channel is still under investigation.

.PARAMETER RunSecs
    Observation window AFTER the order is pushed. Default 600. With
    -StopWhenComplete this is the CAP on the window, not its length.

.PARAMETER StopWhenComplete
    OPT-IN early close of the observation window (default OFF, so a default run
    stays comparable with the record). When set, the stage-8b loop polls the
    interface log (vrfc2simapp.log) every 5 s for
        SENT TASK STATUS REPORT (<CODE>) taskee=<uuid> task=<uuid> - <why>.
    and closes the window once (a1) EVERY distinct PerformingEntity in the pushed
    order has at least one TERMINAL report AND (a2) EVERY order task - counted per
    (taskee, task) PAIR, not by line total - has one, AND (b) -SettleHoldSecs have
    elapsed since the poll that first saw (a), AND (c) every taskee has
    POST-COMPLETION POSITION EVIDENCE. -RunSecs remains the cap. The manifest
    records oracle.earlyExit (enabled / fired / per-taskee first-seen /
    tasksClosed / terminalByCode / reportEvidence with the 'via' that satisfied
    it / failedCondition / closedUtc).

    TERMINAL = TASKCMPLT *or* TASKABRT (2026-09-14). A task VR-Forces fails, and a
    successor the interface skips because its predecessor was abandoned, are END
    STATES: an order containing either could never satisfy a TASKCMPLT-only count
    and the window always ran to its cap (run 20260914T230706Z: 42 tasks, 40
    TASKCMPLT + 2 TASKABRT). TASKSTRT is not terminal. A report the app can not
    attribute to a task uuid (task=(none)) is counted and printed but closes no
    task - the window then runs to its cap, the safe direction.

    (c) accepts ANY ONE of three sources (RunnerLib Test-ReportEvidence):
      RPT            a VR-Forces radio TEXT report agreeing with the sampled POS.
                     The 2026-09-02 rule, kept - but NO run has ever produced an
                     RPT row (RPT=0 in all 8 traces of 2026-09-14), which is why
                     this switch had never once fired before today.
      C2SIM-capture  a C2SIM PositionReport for the taskee's own uuid, captured
                     after its TASKCMPLT, out of reports-captured.log.
      R1-applog      a complete R1 position-report round (>=1 sent, 0 skipped)
                     logged AFTER this taskee's TASKCMPLT line.
    The interface log is the LIVE source for (a) and for R1-applog. Since 0999eeb
    ListenReports APPENDS reports-captured.log as each report arrives, so the
    C2SIM capture is normally POPULATED during the window too - it is the
    authority whenever it carries the taskee's record. Never fires when the order
    yields zero taskees (WARN), or after the interface has died (the window is run
    out so the trace covers the death, exactly as before).

.PARAMETER PreOrderGate
    OPT-IN READY GATE on stage 7d. '' (the default) = off. 'NavArea' = hold
    PushOrder until the SIMULATOR ITSELF says the sectorised navigation area is
    its primary one, i.e. until the first

        VRF console [N] <object> (VRF_UUID:...): New Primary nav area: | <area>

    row appears in the interface log, then push the order at once. Measured
    2026-09-14 over four runs (docs/experiments/G7B_G8_RESULTS_2026-09-14.md
    sec 1.5 and 3): that row lands 9.1-12.1 s after the first entity placement
    with a WARM file cache and 236.9 s COLD, and under CreationPolicy=AtOrder the
    order reached the bus 4.7-7.7 s BEFORE it in every run - so every member's
    slot move and first leg was planned by the FEATURE planner, silently.
    The gate logs the object, the area and the delta from the first PLACEMENT
    line, and calls that delta WARM or COLD; it is the cache-state indicator.
    REQUIRES the object consoles open (Vrf:ObjectConsoleNotifyLevel >= 3, the
    level the row prints at) - stage 0 refuses the gate below that, because the
    row would never print and the gate could only ever time out.

.PARAMETER PreOrderGateTimeoutSec
    How long -PreOrderGate waits. Default 300 (range 30..1800), which covers the
    cold ~240 s with margin. On timeout the run STOPS (exit 3) with a NOT-READY
    message - UNLESS -PreOrderSettleSecs is also greater than 0, in which case the
    gate falls back to that fixed hold. GATE OR SETTLE, never one after the other:
    with both given the gate is what is in force and the settle is the fallback.

.PARAMETER PauseAtSec
    THE Q5 PROBE, PAUSE HALF. 0 (the default) = OFF, and the run is byte-identical
    to one without this parameter. N > 0 = at t+Ns inside the stage-8b observation
    window - measured from the moment PushOrder RETURNED, the same clock every
    other stage-8b message uses - run tools/PauseSim ONCE to PAUSE the scenario
    (controller->pause() on all back ends) and record the sim time it read either
    side of the call. It costs ONE ledgered appNumber, claimed at stage 2 like
    every other and ONLY when this switch is armed.

.PARAMETER ResumeAtSec
    THE Q5 PROBE, RESUME HALF. 0 (the default) = OFF. M > 0 = at t+Ms in the same
    window, run tools/PauseSim resume (controller->run() - the vendor's own
    counterpart to pause; there is no separate resume call). With both given, M
    MUST be greater than N. It takes its OWN appNumber: each invocation is a whole
    join/resign cycle, so a pause and a resume are TWO numbers, never one reused
    (Appendix B, the tools/SetSimRate NOTE: "four invocations, four numbers").
    THE KILL HALF OF THE Q5 PROBE IS NOT AUTOMATED AND MUST NOT BE: killing a back
    end is a MANUAL step, RUNBOOK 0.5.14 item 14.

.PARAMETER FederationHoldSecs
    STAGE 2h, THE FEDERATION HOLDER (STP-825). 5.2 PROFILE ONLY. Seconds a holder
    federate STAYS JOINED to the federation before Stage 2c runs, so that neither
    the C1 gate nor the SIM ever has to CREATE it - and the CREATE is the operation
    rtiexec 5.0.1 has been rejecting intermittently since 2026-09-15 14:23Z ("Failed
    to process FOM file <module> ... Sending Create Response = Error"), which kills
    the sim at startup (LaunchVrf exit 3) and crashes the creating process (STP-832).
    JOINS have never failed. Default 900. It is a WALL-CLOCK hold measured from the
    holder's own join, and the holder DELIBERATELY OUTLIVES this run: teardown never
    touches it and never waits for it, and it resigns on its own timer (RUNBOOK sec
    0 - a joined federate is never force-killed). 0 DISABLES the stage entirely and
    restores the pre-STP-825 behaviour, which is the failure mode; it exists as the
    single-variable control, not as a normal setting.

.PARAMETER FederationHoldAttempts
    How many times Stage 2h will try to get a holder joined, default 4. EACH ATTEMPT
    COSTS ONE LEDGERED appNumber, allocated up front like every other join: the
    operation that fails is the create, a failed create can crash the creating
    process AFTER it registered a federate, and reusing that number would be the
    stale-federate trap. Attempts after the one that succeeds are UNCONSUMED and
    BURNED. All attempts failing is FATAL before anything is launched.

.PARAMETER SettleHoldSecs
    Seconds the ALL-COMPLETE condition must hold before -StopWhenComplete closes
    the window. Default 60. Exists so the movement gate (HEADLESS_RUN_PLAN 4a.1
    "settled" = <10 m over 3 consecutive 2 s samples) still sees a post-arrival
    plateau; -TrailSecs is added on top by the teardown.

.PARAMETER TraceStopGraceSec
    How long teardown waits for WatchVrf-trace / ListenReports to exit AFTER the
    stop file is touched, before recording them still-running (never killed).
    Default 120. Only applies when the deployed tool advertised 'stop-file' in
    its --capabilities; otherwise the pre-turnaround wait (duration + 120 s) is
    kept and the run is flagged WARN traceStop=duration-only.

.PARAMETER StageTimeoutSec
    Slack added on top of each foreground stage's OWN blocking budget before that
    stage is declared timed out. Default 600. A timeout RECORDS the stage as
    timed-out and fails the run through the normal teardown; it NEVER force-kills
    anything, least of all a federate-joining tool (RUNBOOK sec 0).

.PARAMETER ConsoleLogDir
    OPT-IN. A NAME (not a path) for a subdirectory of this run's directory. When
    supplied, the stage-5 WatchVrf trace federate is started with
    --console-log-dir <runDir>\<name>, which makes it ask each discovered object's
    BACKEND to write that object's console to a file THERE, bypassing the console
    network path. That is what turns an empty CON stream from an ambiguity into an
    answer: a populated file beside an empty CON stream proves a DELIVERY gap, an
    empty file proves nothing was raised. Armings appear in the trace as
    *** NOT SUPPORTED: the deployed WatchVrf has no --console-log-dir flag (went out with revert 5d14eda) and emits no CONARM record. -ConsoleLogDir is accepted-and-IGNORED with a warning; it does not arm anything.
    Not supplied = the flag is not passed and the run is unchanged. Opt-in because
    it raises the notify level of every object, perturbing the system observed.
    CAVEAT: the path is resolved by the BACKEND. If the backend is another machine
    the files land there and this directory stays empty for reasons unrelated to
    whether any message was raised.

.PARAMETER DryRun
    Print the ENTIRE planned sequence - every command line, every appNumber that
    WOULD be allocated, every output path - and do nothing else. -DryRun:
      * launches nothing, starts no process except read-only inventory
      * contacts no server (no REST, no STOMP, no RTI)
      * does NOT advance the Appendix B marker and does NOT write to it
      * does NOT create the run directory
    This is how the script is reviewed. It is the only self-test permitted before
    a live gate.

.OUTPUTS
    Exit codes (the RUNNER's own; per-stage codes are in the manifest):
      0  the run completed and the evidence was collected (this is NOT a verdict
         on the scenario - see 4a.6), or a dry run completed
      2  usage / validation error, or a called tool exited 2. NOTHING was launched
         where the check could be made before launch.
      3  a stage failed after VR-Forces was up; teardown ran. Evidence is partial
         and the manifest says which stage failed.
      4  teardown itself did not fully complete - VR-Forces and/or the interface
         MAY STILL BE RUNNING and MAY STILL BE JOINED. Nothing was force-killed.
         MANUAL INSPECTION REQUIRED before the next run.
      5  unexpected terminating error. Same warning as 4.
      6  Stage 8b WS RUNAWAY ABORT (RUNBOOK 0.5.11 item 17 extension): -WsRunawayAbortAfter
         confirmed thread-samples.alerts.txt episodes accumulated since the order reached
         the bus. Teardown ran (same path as exit 3); the manifest names the alerts under
         preflight.wsRunaway. Disabled with -WsRunawayAbortAfter 0.

.EXAMPLE
    pwsh -File scripts\RunC2SimScenario.ps1 -DryRun

.EXAMPLE
    pwsh -File scripts\RunC2SimScenario.ps1 -RunSecs 600
#>
[CmdletBinding()]
param(
    [string] $Init,
    [string] $Order,
    [int]    $RunSecs = 600,

    # Where the evidence lands. A timestamped subdirectory is created under this.
    [string] $RunRoot,

    # WHICH VR-FORCES STACK. This is the ONE switch: it derives the roots, the per-process
    # environment, the launch/stop scripts, the tool + app binaries, the federation identity,
    # the type-map table and the connection config. Nothing else selects a stack, and an
    # explicit -VrfRoot / -VrLinkRoot / -RtiDir / -Federation together with -VrfProfile 5.2
    # is REFUSED at validation (a half-5.2 environment is the DLL-name-binding trap: MAK
    # libraries bind BY NAME on PATH, so a 5.2 exe under a 5.0.2 PATH silently loads the
    # wrong stack - docs/VRF_5.2_MIGRATION_DIFF.md sec H).
    [ValidateSet('5.0.2','5.2')]
    [string] $VrfProfile = '5.0.2',

    # 5.2 ONLY: launch the back end WITHOUT vrfGui (LaunchVrf52.ps1 -NoGui). Default OFF -
    # the GUI stays ON during the migration because it is the only channel that shows
    # whether the entities the control path creates are actually there (the reflected=0
    # observation-channel defect, PREREG_52_TOOLJOIN_2026-09-03.md). Refused on 5.0.2,
    # whose combined-mode launcher has no such option.
    [switch] $NoGui,

    # 5.2 ONLY: the VR-Forces-level network interface address - the 5.0.2 Launcher panel's
    # "Network Interface Address", which that configuration fixed at 127.0.0.1. It becomes
    # --deviceAddress + --hostAddressString on the sim and the gui (UG52 Table 10 p177 /
    # Table 11 p180-181) and --deviceAddress on the bridge federates' HLA argv.
    #
    # DEFAULT EMPTY = PASS NOTHING, and VR-Forces picks "the first device listed" (IOG 5.2.1
    # p81). It defaulted to 127.0.0.1 for a few hours on 2026-09-04 because
    # PREREG_52_RTIEXEC's repairing run had set it at the same time as the RTI connection
    # mode. The DISCRIMINATOR then ran (sec 4 P4, run 3857): an observer with
    # --device-address none, against the same rtiexec sim, still reflected 54-56 entities -
    # so the VR-Forces-level address is NOT required observer-side, and it is not part of the
    # cause. The SIM-SIDE half is still open, which is why the first live run of this profile
    # launches WITHOUT it. Set a dotted IPv4 here to pin it deliberately; the RTI-layer pin
    # (RTI_networkInterfaceAddr 127.0.0.1 in the rid) is a DIFFERENT layer and is unaffected.
    # Refused on 5.0.2, whose combined-mode launcher takes the value from its saved profile.
    [string] $DeviceAddress = '',

    # 5.2 ONLY: relocated appData for the sim and the gui (LaunchVrf52.ps1 -AppDataDir ->
    # --appDataDir on both, UG52 Table 11 p178 / Table 10 p164). EMPTY (default) = nothing is
    # passed and VR-Forces uses $VrfRoot\appData, so every existing command line is unchanged.
    # 'C:\C2SIM\vrf-appdata\appData' is the reinstall-safe copy whose only delta from the
    # vendor tree is (setqb loadAllNavigationDataOnTerrainLoad 1) - navigation data loaded
    # WITH the scenario instead of lazily at first entity placement (UG52 App. C p1671);
    # see that tree's README-C2SIM.txt. Refused on 5.0.2 (LaunchVrf.ps1 has no such option).
    [string] $VrfAppDataDir = '',

    # VR-Forces bring-up (passed straight through to the profile's launch script).
    # The four below are 5.0.2 values and are DERIVED from -VrfProfile on 5.2.
    [string] $Scenario   = 'TropicTortoise',
    [string] $VrfRoot    = 'C:\MAK\vrforces5.0.2',
    [string] $VrLinkRoot = 'C:\MAK\vrlink5.8',
    [string] $RtiDir     = 'C:\MAK\makRti4.6.1',
    [string] $Federation = 'CWIX-2024',

    # Pass -q | --doNotUseConsole to the back end (LaunchVrf.ps1 -QuietBackend). DEFAULT OFF,
    # so every existing command line and every run in the record keeps its console. Per the
    # Users Guide (Table 8 p.177) this sends back-end output to the LOG FILE instead of the
    # console; it does NOT suppress vrfSim.log (sec 4.9 p.161). Recorded in the manifest.
    [switch] $QuietBackend,

    # 5.2 ONLY: the back end's --notifyLevel (0 fatal .. 4 debug; LaunchVrf52.ps1 -NotifyLevel,
    # default 3 = the level every 5.2 run in the record used). 4 makes the vendor log carry the
    # engine's debug stream for a diagnostic run. This is the SIM-WIDE level; a unit's own
    # console is a separate per-object level (app setting Vrf:ObjectConsoleNotifyLevel, UG52
    # 21.9.1 p483). Recorded in the manifest.
    [int]    $BackendNotifyLevel = 3,

    # The app's C2SIM clientId (Vrf:ClientId), which MUST equal the init's SystemName (RUNBOOK sec 2,
    # LIMITATION 6). EMPTY (default) = whatever the deployed appsettings.json pins ("STP" - the R9
    # inits). Pass e.g. -ClientId C2SIM for data\COA-STP1_Initialization.xml: the value is exported
    # as Vrf__ClientId to the app (env overrides appsettings) and validated against the init here.
    # Before 2026-09-06 this needed a hand edit of appsettings.json per init (d1f2e10 / 7963aed).
    [string] $ClientId = '',

    # 5.2 ONLY: the fidelity table the app loads (Vrf__TypeMapFile, read under
    # Vrf:TypeMappingMode=FidelityTable). EMPTY (default) = data/unit-type-map-52.json, the repo map.
    # A PROBE may pass another file (e.g. a scratch copy with lifeform rows redirected while the
    # DI-Guy data package is absent - PREREG_COASTP1_52_RUN1 sec 8); the path is recorded in the
    # manifest so the run can never be mistaken for a repo-map run. Note the 5.2 profile SETS
    # Vrf__TypeMapFile for the app from this value - an inherited env var is overwritten.
    [string] $TypeMapFile = '',

    # C2SIM endpoints. DEFAULT = THE PRIVATE TEST SERVER (2026-09-02): docker container
    # c2sim-server-vrf on 18080 / 61614, a second instance of the same image with its
    # own bind mount (RUNBOOK sec 1). The operator's own server stays on 8080 / 61613;
    # an initialization pushed there from the C2SIM GUI mid-run reset the interface
    # (run 20260902T193508Z) - the two must never share a server again. They reach
    # PushInit / PushOrder / StopIface as arguments, ListenReports as --rest-url /
    # --stomp-url (capability 'endpoints'), and the app as C2SIM__RestUrl /
    # C2SIM__StompUrl in its environment (the Host builder maps '__' to ':').
    [string] $RestUrl  = 'http://127.0.0.1:18080/C2SIMServer',
    [string] $StompUrl = 'http://127.0.0.1:61614/topic/C2SIM',

    # Oracle cadence. 2 s per HEADLESS_RUN_PLAN 4a.2 ("Sample interval 2 s").
    # This is a MEASUREMENT PARAMETER, not a threshold - it says how often we look,
    # never what counts as movement.
    [int] $SampleSecs = 2,

    # Timing budgets. All are upper bounds; the run does not wait them out when the
    # signal arrives earlier.
    [int] $PreRollSecs         = 20,   # trace time before the init is pushed
    [int] $AppJoinTimeoutSec   = 180,  # app: launch -> "Connected to C2SIM"
    [int] $OracleGateTimeoutSec= 180,  # RUNBOOK 0.5.7 "allow up to ~3 MINUTES"
    [int] $InitDispatchWaitSec = 120,  # app: -> "Init dispatched: N units"
    [int] $PushOrderListenSec  = 30,   # PushOrder's own blocking listen window
    [int] $TrailSecs           = 30,   # trace time after the observation window
    [int] $LaunchSettleSec     = 45,   # after LaunchVrf READY, before the oracle
                                       # pre-check. READY is thread-count + window
                                       # only; it does NOT imply scenario loaded or
                                       # federation joined (RUNBOOK 0.5.7).
    [int] $AppExitTimeoutSec   = 120,  # app: StopIface -> process gone (NEVER killed)
    [int] $StopVrfTimeoutSec   = 120,

    # STAGE 7d - HOLD THE ORDER BACK this many seconds after the oracle gate passes, before
    # PushOrder. 0 (the default) = OFF and the run is byte-identical to one without this
    # parameter; a default run therefore stays comparable with the record.
    #
    # WHY IT EXISTS: the sectorised navigation area loads LAZILY, AFTER the entities are
    # placed, so a task issued too early is planned WITHOUT the mesh. Run 20260914T130439Z
    # shows the area's "New Primary nav area" rows 175 s after the members were created.
    # This is a MEASUREMENT PARAMETER, not a fix: it says how long we wait, never what
    # counts as loaded, and it cannot tell you the mesh arrived - only the consoles can.
    # The vendor-side alternative is UG52 Appendix C loadAllNavigationDataOnTerrainLoad,
    # which loads every sector at terrain load; that is a CONFIG change, not a runner one.
    # The hold is INSIDE the observers' coverage, so it is added to their duration cap.
    [int] $PreOrderSettleSecs  = 0,

    # STAGE 7d - THE READY GATE. '' (default) = off, and a default run is byte-identical to
    # one without this parameter. 'NavArea' = hold PushOrder until the SIMULATOR'S OWN
    # "New Primary nav area: | <area>" row appears in the interface log, then push at once.
    #
    # WHY IT IS NOT THE SETTLE ABOVE: the settle says how long we wait; this says what we
    # are waiting FOR, and it is the simulator's own signal. Four runs on 2026-09-14
    # (docs/experiments/G7B_G8_RESULTS_2026-09-14.md sec 1.5) put that row 9.1-12.1 s after
    # the first placement warm and 236.9 s cold - a 20x spread no fixed number covers, and
    # the fixed -PreOrderSettleSecs is a guess against exactly that spread.
    # It needs the object consoles OPEN (Vrf:ObjectConsoleNotifyLevel >= 3): the row prints
    # at level 3 and nowhere else. Stage 0 refuses the gate below that level.
    [string] $PreOrderGate = '',

    # The gate's own timeout, 30..1800. 300 covers the cold ~240 s with margin. On timeout
    # the run STOPS unless -PreOrderSettleSecs > 0, which then serves as the fallback hold.
    [int] $PreOrderGateTimeoutSec = 300,

    # STAGE 8b - THE Q5 PAUSE/RESUME PROBE. Both 0 (default) = OFF, and a default run is
    # byte-identical to one without these parameters: no tool is invoked, no appNumber is
    # claimed for one, and the manifest keeps exactly the shape every run in the record has.
    #
    # WHAT IT IS FOR: Q5 says a PAUSED scenario must not age a C2SIM task clock (assessment
    # live gate 11, validation V5). Proving that live needs the scenario paused mid-run and
    # resumed - which nothing in the repo could do until tools/PauseSim. The offsets are in
    # seconds after PushOrder RETURNED and are honoured at POLL granularity (the loop below
    # polls every 5 s whenever either is armed).
    # -ResumeAtSec MUST be greater than -PauseAtSec when both are given (stage 0 refuses
    # otherwise): resuming before the pause would leave the scenario paused for the rest of
    # the run, which is the one outcome that silently destroys the whole window.
    [int] $PauseAtSec  = 0,
    [int] $ResumeAtSec = 0,

    # STAGE 8b - THE WS RUNAWAY ABORT (RUNBOOK 0.5.11 item 17 extension; V6f harvest defects 1+2,
    # 2026-09-15). scripts\SampleThreads.ps1's tripwire, when -SampleThreads is armed on the wrapper
    # (scripts\RunScenario.sh --sample-threads), writes CONFIRMED "BACK-END WS RUNAWAY" lines to
    # <runDir>\thread-samples.alerts.txt - but nothing reads that file WHILE the run is live (defect 1;
    # the wrapper only surfaces it AFTER teardown has already completed). This polls that same file every
    # 10 s during the Stage 8b observation window and counts only the alerts timestamped AT OR AFTER the
    # order reaching the bus (orderOnBusUtc, falling back to orderPushedUtc if the bus log parse ever
    # misses) - an alert from before the order existed can not be evidence the order caused a runaway.
    # That "since dispatch" filter is deliberately NOT how defect 2 (the object-creation-burst alert
    # firing shortly after a mid-run dispatch, independent of the universal process-start warm-up) is
    # fixed - that fix is SampleThreads.ps1's own re-armable warm-up (-WarmupResetAtUtc /
    # -WarmupResetFile), untouched here. -WsRunawayAbortAfter is instead the SECOND, independent net:
    # even a real post-dispatch burst alert can not unilaterally abort a run - only a SUSTAINED signal
    # (this many confirmed episodes) does.
    # 0 = disabled = today's behaviour (the wrapper's post-hoc report is all a run ever gets); the
    # default of 3 matches the V6f record, where the back end grew from 3.1 to 16+ GB over 6 confirmed
    # episodes - by episode 3 (independent of the creation-burst alert, since the default abort-after
    # already requires 3 REGARDLESS of which alert is first) the pattern is no longer one legitimate
    # step. Aborting takes the SAME failure path as every other post-launch Stage 8b failure
    # (Stop-Runner): Add-Flag 'FAIL', the normal finally teardown (StopIface then StopVrf; nothing here
    # ever force-kills anything, and the Stage 2h federation holder is never touched), a manifest record
    # naming which alerts triggered it, and a distinct runner exit code (6) so a caller can tell "the
    # back end diagnosed a runaway" apart from every other failure code (2/3/4/5).
    [int] $WsRunawayAbortAfter = 3,

    # STAGE 2h - THE FEDERATION HOLDER (STP-825, 2026-09-15). 5.2 PROFILE ONLY.
    #
    # WHY IT EXISTS. On the 5.2 profile nothing creates the federation before the SIM does:
    # Stage 2c's RtiProbe creates MAK-ONE-2025, joins, resigns - and, being the last
    # federate in it, DESTROYS it again. Since 14:23Z on 2026-09-15 rtiexec 5.0.1 rejects a
    # CREATOR's FOM-module distribution intermittently ("Failed to process FOM file
    # <module> ... FOM Reader reports", "Sending Create Response = Error"), so the sim's own
    # create fails at Stage 3 and it dies at startup (LaunchVrf exit 3); a failed create
    # also crashes the creating process (STP-832). JOINS have never failed. A HOLDER
    # federate started before Stage 2c keeps the federation alive, so both the gate and the
    # sim take the JOIN path instead (confirmed live, runs 20260915T151959Z / 20260915T160253Z).
    #
    # -FederationHoldSecs is how long the holder STAYS JOINED (RtiProbe's settleSecs), in
    # WALL-CLOCK seconds measured from its own join - it deliberately outlives this runner.
    # 0 DISABLES the stage completely: no stage, no appNumbers, no output, i.e. exactly the
    # pre-STP-825 behaviour. -FederationHoldAttempts is how many times the stage will try,
    # each on its OWN ledgered appNumber (the create is the operation that fails, and it can
    # crash the process after registering a federate - so a retry must never reuse a number).
    [int] $FederationHoldSecs     = 900,
    [int] $FederationHoldAttempts = 4,

    # SLACK added on top of a FOREGROUND stage's OWN known blocking budget before
    # the runner declares that stage timed out. An unattended runner must never
    # block forever (it did, for 47 minutes, on 2026-07-19 - see the Invoke-External
    # header). Every foreground stage's ceiling is computed as
    #     <that stage's own documented blocking budget> + $StageTimeoutSec
    # so raising -PushOrderListenSec or -StopVrfTimeoutSec can never make the
    # ceiling cut off a healthy stage. Measured healthy durations for reference:
    # LaunchVrf ~35-60 s, StopVrf ~6-10 s, PushInit/StopIface seconds, PushOrder
    # blocks for -PushOrderListenSec (default 30) plus overhead. 600 s of slack is
    # therefore roughly a 10x margin on the slowest of them.
    # A TIMEOUT NEVER FORCE-KILLS ANYTHING - see $FederateJoiningTools.
    [int] $StageTimeoutSec     = 600,

    # Stage 7b only: how long to watch the EXISTING trace for the CreateOne entity
    # after CreateOne reports its uuid. Generous relative to the ~2 s sample cadence;
    # the entity already exists by the time this starts, so this is reflect+sample
    # latency, not settle time.
    [int] $CreateOneWatchSec   = 90,

    # Explicit override for the WatchVrf / ListenReports duration. 0 = derive it.
    # Either way it is the observers' CAP: teardown tells them to stop (stop file)
    # at StopIface + TrailSecs when the deployed tools support it.
    [int] $WatchSecs = 0,

    # ---- turnaround (2026-09-01, docs/RUNNER_TURNAROUND_2026-09-01.md) ----
    # OPT-IN early close of the observation window once every taskee in the order
    # has reported TASKCMPLT and the hold below has elapsed. OFF by default so a
    # default run is comparable with the record. -RunSecs stays the cap.
    [switch] $StopWhenComplete,
    [int]    $SettleHoldSecs    = 60,
    # After the stop file is touched, how long teardown waits for the observers to
    # exit on their own before recording them still-running (never killed).
    [int]    $TraceStopGraceSec = 120,

    # OPT-IN backend-side console capture for the stage-5 scoring trace.
    #
    # Supply a NAME (not a path): it becomes a subdirectory of THIS run's directory,
    # and WatchVrf is started with --console-log-dir pointing at it. WatchVrf then asks
    # each discovered object's BACKEND to write that object's console to a file there,
    # bypassing the console network path this runner's two zero-CON runs left under
    # suspicion. A populated file beside an empty CON stream proves a DELIVERY gap; an
    # empty file proves nothing was raised. Each arming appears in the trace as
    # NOT SUPPORTED - see above; -ConsoleLogDir is ignored, no CONARM record exists.
    #
    # NOT SUPPLIED (the default) = the flag is not passed and the run is byte-identical
    # to before this parameter existed. Deliberately opt-in: it changes the notify level
    # of every object in the federation, which is a real perturbation of the system under
    # observation and must never happen implicitly on a scoring run.
    #
    # CAVEAT the runner cannot check for you: the path is resolved by the BACKEND. If the
    # backend runs on another machine the files land THERE, and this directory stays empty
    # for a reason that has nothing to do with whether messages were raised.
    [string] $ConsoleLogDir,

    [switch] $StrictPreInitOracle,
    [switch] $SkipServerCheck,

    # Do NOT start scripts\RunnerWatchdog.ps1 at stage 6b. The watchdog is the OUT-OF-PROCESS
    # teardown backstop for the case the launching wrapper dies with the runner (design A2,
    # docs\experiments\RUNNER_EXIT127_2026-09-14.md sec 3.1); with it off, the only backstop
    # left is scripts\RunScenario.sh, which requires its bash to survive. Use this only when
    # the watchdog itself is what is under test, or when a run is deliberately being left up
    # for inspection after the runner exits. It is NOT a performance switch: the watchdog
    # sleeps between 5 s polls and reads nothing while the runner is alive.
    [switch] $NoWatchdog,
    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptVersion = '1.0.0-draft'

# =============================================================================
# KNOWN TOOL LIMITATIONS THIS SCRIPT WORKS AROUND (read before editing)
# =============================================================================
# 1. tools/ListenReports used to HARDCODE RestUrl/StompUrl to 127.0.0.1:8080/61613.
#    Since 2026-09-02 it takes --rest-url / --stomp-url (capability token
#    'endpoints'). A DEPLOYED binary that predates the flag still hears only the
#    historical endpoints, so when the capability is absent and the endpoints are
#    not those historical values this script REFUSES to start, rather than produce
#    a capture file that looks valid and is empty. The non-localhost refusal stays:
#    every stage assumes the server is on this host (the VR-Forces host).
# 2. tools/WatchVrf exit codes are UNAMBIGUOUS: 2 = usage/argument error only
#    (ToolArgs.ExitUsage), 1 = operational failure (ToolArgs.ExitFailure, e.g. the
#    join throwing - WatchRunner.cs:122/225). Because this runner GENERATES WatchVrf's
#    arguments, an exit 2 means the RUNNER BUILT BAD ARGUMENTS, not a federation/RTI
#    fault; an exit 1 is the operational failure. Do not conflate them.
# 3. tools/PushOrder writes its bus log to c2sim-bus.log BESIDE ITS OWN BINARY
#    (PushOrder/Program.cs:112) with no override. This script COPIES it into the
#    run directory afterwards; the copy can be stale if PushOrder failed before
#    writing, so the copy is timestamp-checked and the result recorded.
# 4. tools/StopIface requires <restUrl> <stompUrl> AND --yes, with NO defaults, and
#    exits 1 if the server does not reach UNINITIALIZED (StopIface/Program.cs:138).
#    Exit 1 there means the interface MAY STILL BE JOINED - it is escalated, not
#    swallowed.
# 5. src/VrfC2SimApp reads appsettings.json from its content root, so it is
#    launched with --contentRoot=<exe dir> while cwd is VR-Forces bin64
#    (RUNBOOK sec 7 item 3). Its ApplicationNumber is overridden through the
#    environment variable Vrf__ApplicationNumber - historically hand-edited in
#    appsettings.json, which is exactly how stale-federate hangs were created.
# 6. appsettings.json pins Vrf:ClientId = "STP". RUNBOOK sec 2: clientId MUST equal
#    the init's SystemName or the interface creates 0 units. This script READS the
#    SystemName out of the init file and REFUSES on a mismatch, up front.
# =============================================================================

# ---- output helpers (ASCII only) -------------------------------------------
function Say      { param([string]$m) Write-Host $m }
function Say-Head { param([string]$m) Write-Host ''; Write-Host ('=== ' + $m + ' ===') }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Info { param([string]$m) Write-Host ('  [..]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [DRY-RUN] ' + $m) }

# ---- HARD GATE: 64-BIT HOST ONLY (added 2026-09-14) --------------------------
# Bare "pwsh" on this machine's PATH resolves to C:\Program Files (x86)\PowerShell\7 -
# the 32-BIT build, ~2 GB of address space - because that PATH entry precedes the
# 64-bit one. A 32-bit host died of address-space exhaustion inside this runner's
# observation loop on 2026-09-07 (docs/experiments/PREREG_ASSEMBLY_LAYOUT_2026-09-07.md
# sec 4, recorded exit 9). The mitigation written down that day was a PROCEDURE -
# "launch the runner from a 64-bit pwsh" - and a procedure lapses silently: the G6 run
# of 2026-09-14 ran 32-bit again (docs/experiments/RUNNER_HARDENING_2026-09-14.md).
# A rule a checker can enforce becomes one. Refused HERE, before any path, marker,
# ledger entry, run directory or process exists, so exit 2 keeps its documented
# meaning: "aborted at validation; nothing was launched".
if (-not [Environment]::Is64BitProcess) {
    Say-Fail ('this runner is hosted in a 32-BIT PowerShell (PSHOME {0}). Long runs die of address-space exhaustion in the observation loop (2026-09-07: dead at 176 MB).' -f $PSHOME)
    Say-Fail '  Relaunch with the 64-bit build, BY FULL PATH - bare "pwsh" is the 32-bit one on this machine:'
    Say-Fail '      "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File scripts\RunC2SimScenario.ps1 ...'
    Say-Fail '  scripts\RunScenario.sh pins that path (and redirects stdout to a file) for you. NOTHING was launched.'
    exit 2
}

# ---- pure helpers shared with tests/RunnerTurnaround.Tests.ps1 ---------------
# Duration cap, order/taskee parse, TASKCMPLT parse, early-exit decision, stop-file
# timing and the capability-probe parse live in RunnerLib.ps1 so they can be
# exercised without a simulator. Nothing in there starts a process or sleeps.
. (Join-Path $PSScriptRoot 'RunnerLib.ps1')

# ---- THE MAK LICENCE: resolved from the REGISTRY, never from the inherited env ----
# The licence was renewed on 2026-09-14 and the new path was written to the USER scope; the
# MACHINE scope still names the old 15-sep-2026 file (the elevation to change it was refused).
# Windows composes a NEW process's environment as Machine-then-User, so a freshly started tree
# gets the renewed file - but a process that was ALREADY RUNNING when the value changed keeps
# the stale one and hands it to everything it launches: the runner, the launch script, the sim,
# the gui, the interface, the observers, every tool. An expired licence then surfaces as a sim
# that dies at startup, not as a licence error. So each entry script resolves User-then-Machine
# ITSELF and pins the result onto its own process, which every child inherits.
# Only the PATH and the EXPIRY are ever printed; nothing else is read out of the file.
# RUNBOOK 0.5.15.
# FOUR COPIES ON PURPOSE - no module is dot-sourced by all of these scripts:
#   scripts\RunC2SimScenario.ps1, scripts\LaunchVrf52.ps1, scripts\StartInterface52.ps1 and,
#   in bash, scripts\RunScenario.sh. CHANGE ONE, CHANGE ALL FOUR.
# Write-Host rather than the local Say-* helpers, so the three .ps1 copies stay byte-identical;
# the prefixes are the ones Say-Ok / Say-Warn print.
function Resolve-MakLicenseFile {
    param([string]$PathOverride = '')
    if ([string]::IsNullOrWhiteSpace($PathOverride)) {
        $lic = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','User')
        if (-not $lic) { $lic = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine') }
        $licSrc = 'registry: User scope, else Machine'
    } else {
        $lic = $PathOverride
        $licSrc = 'EXPLICIT OVERRIDE - the registry was NOT consulted'
    }
    $info = [pscustomobject]@{ Path = $lic; Source = $licSrc; Exists = $false; ExpiryText = ''; Expiry = $null }
    if ([string]::IsNullOrWhiteSpace($lic)) {
        Write-Host '  [WARN] *** MAKLMGRD_LICENSE_FILE is EMPTY in BOTH the User and the Machine scope. ***'
        Write-Host '  [WARN] *** Nothing is stopped here, but every MAK process may HANG on its licence checkout (RUNBOOK 0.5.15). ***'
        return $info
    }
    if (-not (Test-Path -LiteralPath $lic -PathType Leaf)) {
        Write-Host ('  [WARN] *** THE LICENCE FILE DOES NOT EXIST: {0} ***' -f $lic)
        Write-Host ('  [WARN] *** Source: {0}. Nothing is stopped here, but expect a licence failure in every MAK process (RUNBOOK 0.5.15). ***' -f $licSrc)
        return $info
    }
    $info.Exists = $true
    # THE POINT OF ALL THIS: children inherit THIS value, not the one this tree started with.
    $env:MAKLMGRD_LICENSE_FILE = $lic
    # FlexLM: "INCREMENT <feature> <vendor> <version> <expiry> <count> ..." - field 5 is the
    # expiry (d-mmm-yyyy, or permanent / 1-jan-0000). FIRST INCREMENT line only, and no other
    # field of the file is ever read out of it.
    try {
        foreach ($licLine in [System.IO.File]::ReadAllLines($lic)) {
            if ($licLine -notmatch '^\s*INCREMENT\s') { continue }
            $licFields = @(($licLine -split '\s+') | Where-Object { $_ -ne '' })
            if ($licFields.Count -ge 5) { $info.ExpiryText = $licFields[4] }
            break
        }
    } catch { }
    if ($info.ExpiryText -and ($info.ExpiryText -notmatch '^(permanent|1-jan-0000|0)$')) {
        $licParsed = [datetime]::MinValue
        # [string[]] IS LOAD-BEARING. An untyped PowerShell @(...) is an object[], which does
        # NOT bind the string[] formats overload: PowerShell then picks the SINGLE-format one
        # and stringifies the array to "d-MMM-yyyy dd-MMM-yyyy", so every parse returns false,
        # $info.Expiry stays $null and the expired-licence gate silently never fires. Measured
        # 2026-09-14 against a scratch 1-jan-2020 copy. Month names match case-insensitively,
        # so the file's lower-case "oct" / "jan" is fine.
        if ([datetime]::TryParseExact($info.ExpiryText, [string[]]@('d-MMM-yyyy','dd-MMM-yyyy'),
                [System.Globalization.CultureInfo]::InvariantCulture,
                [System.Globalization.DateTimeStyles]::None, [ref]$licParsed)) { $info.Expiry = $licParsed }
    }
    Write-Host ('  [OK]   licence file: {0} (expires {1})' -f $lic, $(if ($info.ExpiryText) { $info.ExpiryText } else { 'UNKNOWN - no INCREMENT line' }))
    if ($info.Expiry -and ($info.Expiry.Date -lt (Get-Date).Date)) {
        Write-Host ('  [WARN] *** THAT LICENCE EXPIRED ON {0} - MAK processes will fail their checkout. ***' -f $info.ExpiryText)
    }
    return $info
}

# ---- paths ------------------------------------------------------------------
$RepoRoot  = Split-Path -Parent $PSScriptRoot
$DocsDir   = Join-Path $RepoRoot 'docs'
$DataDir   = Join-Path $RepoRoot 'data'
$ToolsDir  = Join-Path $RepoRoot 'tools'
$LedgerDoc = Join-Path $DocsDir 'OPUS_EXECUTION_PLAN.md'

# ---- THE PROFILE: everything the stack choice decides, decided in ONE place ---
# 5.0.2 is the historical path and MUST stay byte-for-byte what it was (its -DryRun
# output is the regression control for this parameter). 5.2 derives:
#   roots            vrforces5.2d + vrlink5.10 + makRti5.0.1 (HLA 1516e; HLA 4 is a
#                    separate phase - DIFF Y-16, NOT reachable here). 4.6.1 does NOT
#                    appear in this profile: the posture below is defined on 5.0.1.
#   RTI posture      the DOCUMENTED one and nothing else (UG52 5.5.1 p190 "You cannot use
#                    the MAK RTI in lightweight mode with VR-Forces"; PREREG_52_RTIEXEC_
#                    2026-09-04: rtiexec mode -> 62 entities reflected, lightweight -> 0):
#                    MAK RTI 5.0.1 in RTIEXEC mode on config\rid-501-rtiexec-min.mtl and a
#                    headless rtiexec ensured up before anything joins (Stage 2r). The rid
#                    pins the RTI's OWN interface (RTI_networkInterfaceAddr 127.0.0.1),
#                    which is inherent to the loopback-broadcast rtiexec connection rather
#                    than a separate decision. RTI_ASSISTANT_DISABLE stays: assistant-free
#                    is orthogonal to lightweight-vs-rtiexec, and a 5.0.1 assistant
#                    version-rejects peers.
#   interface addr   -DeviceAddress, a TUNABLE that DEFAULTS TO EMPTY = pass nothing. This
#                    is the VR-FORCES-level address (--deviceAddress/--hostAddressString on
#                    sim + gui), NOT the RTI-level pin above. It is NOT part of the repair:
#                    run 3857 (PREREG_52_RTIEXEC sec 4 P4) had an observer reflect 54-56
#                    entities with its device address blank against the same rtiexec sim.
#                    The sim-side half is still open, so the profile pins nothing.
#   launch / stop    LaunchVrf52.ps1 (independent mode, UG52 4.1.2) / StopVrf52.ps1
#   binaries         the Release-5.2 build tree of the BRIDGE-LINKED tools and the app
#                    (BridgeConfig axis). PushInit/PushOrder/ListenReports/StopIface are
#                    pure C2SIM managed tools with no bridge reference, so they have no
#                    5.2 variant and the SAME binaries are used on both profiles.
#   identity         NO federation argument: on 5.2 the tools and the app take execName
#                    from appData\settings\connections\MAK-ONE-2025-Config.xml
#                    (tools/Shared/StackIdentity.cs; DIFF rows A2/A9 - config FOM modules
#                    are ADDITIVE, so the 5.0.2 module list must not be submitted).
#   environment      the 5.2 PATH prefix + MAK_VRFDIR/MAK_VRLDIR/MAK_RTIDIR + the SHARED
#                    rtiexec rid + RTI_ASSISTANT_DISABLE, for EVERY spawned process.
#                    Federates that do not share the rid do not share a connection
#                    (PREREG_52_LAUNCH_2026-09-03.md).
$Is52 = ($VrfProfile -eq '5.2')
# The BridgeConfig output tree the bridge-linked binaries were built into.
$BridgeOut = if ($Is52) { 'Release-5.2' } else { 'Release' }
if ($Is52) {
    if (-not $PSBoundParameters.ContainsKey('VrfRoot'))    { $VrfRoot    = 'C:\MAK\vrforces5.2d' }
    if (-not $PSBoundParameters.ContainsKey('VrLinkRoot')) { $VrLinkRoot = 'C:\MAK\vrlink5.10' }
    if (-not $PSBoundParameters.ContainsKey('RtiDir'))     { $RtiDir     = 'C:\MAK\makRti5.0.1' }
    if (-not $PSBoundParameters.ContainsKey('Federation')) { $Federation = '' }
    # 5.2d ships its samples one level down (userData\scenarios\Sample\...). The 5.0.2
    # default TropicTortoise does not exist there, and its terrain 'MAK Earth Space
    # (online)' was DROPPED from makData 19 (DIFF row C1) - the re-authored fixtures are
    # a Phase-2 deliverable, deployed with
    #   python tools\FixtureGen\build_fixture.py <site> --out-dir C:\MAK\vrforces5.2d\userData\scenarios
    # (the SANCTIONED fixture write; --out-dir already exists - do not invent another).
    if (-not $PSBoundParameters.ContainsKey('Scenario'))   { $Scenario   = 'Sample\FirstExperience\firstexperience' }
}

$LaunchVrf = Join-Path $PSScriptRoot $(if ($Is52) { 'LaunchVrf52.ps1' } else { 'LaunchVrf.ps1' })
$StopVrf   = Join-Path $PSScriptRoot $(if ($Is52) { 'StopVrf52.ps1' }   else { 'StopVrf.ps1' })
# 5.2 ONLY: the headless-rtiexec ENSURE-UP stage (Stage 2r). Never used on 5.0.2, whose
# rtiexec comes up behind an ANSWERED rtiAssistant connection instead.
$StartRtiExec = Join-Path $PSScriptRoot 'StartRtiExec52.ps1'
# Where THE RTIEXEC's own log goes - runs\launch52 (persistent, gitignored), NOT a run
# directory. The rtiexec outlives the run that started it and keeps writing to that file
# while it serves later runs, so filing it under one run's evidence would misattribute a
# shared, long-lived process's output. The stage's own stdout/stderr stay in the run dir.
$RtiExecLogDir = Join-Path $RepoRoot 'runs\launch52'

$ExeWatchVrf      = Join-Path $ToolsDir ('WatchVrf\bin\{0}\net10.0\win-x64\WatchVrf.exe' -f $BridgeOut)
$ExePushInit      = Join-Path $ToolsDir 'PushInit\bin\Release\net10.0\PushInit.exe'
$ExePushOrder     = Join-Path $ToolsDir 'PushOrder\bin\Release\net10.0\PushOrder.exe'
$ExeListenReports = Join-Path $ToolsDir 'ListenReports\bin\Release\net10.0\ListenReports.exe'
$ExeStopIface     = Join-Path $ToolsDir 'StopIface\bin\Release\net10.0\StopIface.exe'
$ExeCreateOne     = Join-Path $ToolsDir ('CreateOne\bin\{0}\net10.0\win-x64\CreateOne.exe' -f $BridgeOut)
$ExeRtiProbe      = Join-Path $ToolsDir ('RtiProbe\bin\{0}\net10.0\win-x64\RtiProbe.exe' -f $BridgeOut)
# Stage 8b Q5 probe only (-PauseAtSec / -ResumeAtSec). Bridge-linked, so it follows the same
# BridgeConfig output tree as the other tools - it is the ELEVENTH consumer (RUNBOOK sec 9).
$ExePauseSim      = Join-Path $ToolsDir ('PauseSim\bin\{0}\net10.0\win-x64\PauseSim.exe' -f $BridgeOut)
$ExeApp           = Join-Path $RepoRoot ('src\VrfC2SimApp\bin\{0}\net10.0\win-x64\VrfC2SimApp.exe' -f $BridgeOut)

$Bin64 = Join-Path $VrfRoot 'bin64'

# The federation ARGUMENT the tools are given. On 5.2 it is deliberately EMPTY: RtiProbe
# and WatchVrf treat a blank positional as "stack default" and StackIdentity then joins
# the config-file way, while a non-empty value is an explicit OVERRIDE of the connection
# config's execName (tools/Shared/StackIdentity.cs).
#
# *** CORRECTED 2026-09-15 (RUNBOOK 0.5.14 item 15 addendum) - THE CLAIM BELOW WAS FALSE. ***
# This used to say an empty argument "survives ProcessStartInfo.ArgumentList as "" and
# stays in position". It does not: $sp.ArgumentList here (Invoke-External/Start-External,
# below) is the Start-Process CMDLET PARAMETER of that name, not the .NET
# System.Diagnostics.ProcessStartInfo.ArgumentList collection the comment described - and
# Start-Process SILENTLY DROPS an empty-string element instead of passing it through as a
# real "" argument (reproduced directly: an array of 5 elements with element[1] = ''
# reaches the child as 4 elements, everything after the gap shifted down one slot).
# Every OTHER call site puts federation LAST (WatchVrf, PauseSim: appNumber/duration/
# sample/[federation], action/appNumber/[federation]), where a dropped trailing argument
# is harmless - a tool that never receives its optional last positional behaves exactly
# as if it received it empty. RtiProbe is the ONE tool that takes federation in the
# MIDDLE, between appNumber and its retry counts, so this drop moved every argument
# after it - the C1 gate joined a federation literally named "5" (its own maxAttempts
# value, shifted left), not MAK-ONE-2025 (runs/20260915T030650Z_run/rtiprobe.stdout.log:
# "federation=5 (explicit override...)"; V6 harvest, scratchpad/v6harvest). FIXED at the
# Stage 2c call site below: the argument list is now built CONDITIONALLY instead of
# always passing all five positionals.
$FederationArg = if ($Is52) { '' } else { $Federation }

# Per-process environment the PROFILE adds on top of PATH/licence. EMPTY on 5.0.2, so
# that profile's environment is untouched. Applied to EVERY child: the capability probe
# (Stage 0b), the launch script, both observers, the tools and the app.
# THE RID IS THE POSTURE. rid-501-rtiexec-min.mtl is the 5.0.1 rid configured EXACTLY like
# an assistant rtiexec connection (RTI UG 5.0.1 sec 7.3 p73: useRtiExec 1, tcp/udp 4001,
# dest 127.255.255.255, interface 127.0.0.1, forwarder 5000, internal messages reliable) and
# nothing more - the superset rid-501-rtiexec.mtl crashed the 5.2 sim in
# DtVrfSimOptions::parseCmdLine. The assistant-free LIGHTWEIGHT rid used until 2026-09-03
# (config/rid-461-ridconfigured.mtl) was a WRONG FIX: it removed the version-locked assistant
# but put the federation in a mode VR-Forces does not support (UG52 5.5.1 p190), and every
# observer under it reflected 0. It is NOT reachable from this profile any more.
$RidFile        = Join-Path $RepoRoot 'config\rid-501-rtiexec-min.mtl'
# The connection config is read out of the appData tree the SIM IS USING. When
# -VrfAppDataDir relocates that tree (LaunchVrf52 --appDataDir), the vendor copy under
# $VrfRoot is NOT the one in force, and pointing the interface at it made the app and the
# sim read two different files - benign only for as long as they stay byte-identical
# (they are today; run 20260914T164906Z printed the vendor path while the sim ran on
# C:\C2SIM\vrf-appdata). Follow the relocation instead of assuming the delta stays zero.
$ConnConfigFile = $(if ($Is52 -and -not [string]::IsNullOrWhiteSpace($VrfAppDataDir)) {
                        Join-Path $VrfAppDataDir 'settings\connections\MAK-ONE-2025-Config.xml'
                    } else {
                        Join-Path $VrfRoot 'appData\settings\connections\MAK-ONE-2025-Config.xml'
                    })
$ConnConfigFromAppDataDir = ($Is52 -and -not [string]::IsNullOrWhiteSpace($VrfAppDataDir))
$TypeMapFile52  = $(if ($TypeMapFile) { $TypeMapFile } else { 'data/unit-type-map-52.json' })
if ($TypeMapFile -and -not (Test-Path -LiteralPath $TypeMapFile -PathType Leaf)) { $bad += ('-TypeMapFile not found: {0}' -f $TypeMapFile) }
# The VR-Forces-level interface address for this run (-DeviceAddress; see the param block).
# EMPTY by default and therefore NOT PASSED anywhere: run 3857 falsified it observer-side
# (an observer with no device address still reflected 54-56 entities off the rtiexec sim), so
# it is not part of the repair and must not be smuggled in as if it were. When it IS set, it
# reaches the sim and the gui through LaunchVrf52 -DeviceAddress and the app through
# Vrf__DeviceAddress. With it empty, the bridge federates keep VrfFacade's own default
# (127.0.0.1, VrfFacade.h) - recorded in the manifest so the run says what it really used.
$DeviceAddress52 = $DeviceAddress
$DeviceAddressPassed = -not [string]::IsNullOrWhiteSpace($DeviceAddress52)
# What the bridge-linked federates (tools + app) end up with either way. Not a decision this
# runner makes when -DeviceAddress is empty - just the C++ default, stated so it is evidence.
$BridgeDeviceAddress = $(if ($DeviceAddressPassed) { $DeviceAddress52 } else { '127.0.0.1 (VrfFacade default - nothing overrode it)' })
$ProfileEnv = [ordered]@{}
if ($Is52) {
    $ProfileEnv['MAK_VRFDIR']           = $VrfRoot
    $ProfileEnv['MAK_VRLDIR']           = $VrLinkRoot
    $ProfileEnv['MAK_RTIDIR']           = $RtiDir
    $ProfileEnv['RTI_RID_FILE']         = $RidFile
    $ProfileEnv['RTI_ASSISTANT_DISABLE']= '1'
}

if ([string]::IsNullOrWhiteSpace($Init))    { $Init    = Join-Path $DataDir 'R9_Mojave_Lean_Initialization.xml' }
if ([string]::IsNullOrWhiteSpace($Order))   { $Order   = Join-Path $DataDir 'R9_Mojave_UnitMove_Order.xml' }
if ([string]::IsNullOrWhiteSpace($RunRoot)) { $RunRoot = Join-Path $RepoRoot 'runs' }

$ProcBackend  = 'vrfSimHLA1516e'
$ProcFrontend = 'vrfGui'
$ProcLauncher = 'vrfLauncher'
$RtiNames     = @('rtiAssistant','rtiexec','rtiForwarder')
# The runner's OWN observers (review F1). A WatchVrf left over from an earlier run is
# a foreign federate still joined to the federation, and a ListenReports is still
# subscribed to the STOMP topic; either would contaminate a new run. Stage 1 treats
# them like a leftover sim: report and REFUSE. They are never killed - they end on
# their own duration cap.
$ProcObservers = @('WatchVrf','ListenReports')
# Slack added to an observer's duration cap when teardown falls back to waiting for
# it (review F1): process start-up before the tool's own clock starts plus resign /
# disconnect latency. P2c's WatchVrf-trace exited 5.5 s after start + cap.
$ObserverCapMarginSecs = 30

# ---- WHICH TOOLS JOIN THE FEDERATION (and are therefore NEVER force-killed) --
# These tools JOIN the HLA federation. If one of them overruns its stage timeout
# it is RECORDED and LEFT ALONE. Force-killing a joined federate leaves a STALE
# FEDERATE and the next start hangs at RTI join - RUNBOOK sec 0, the project's
# single most important rule.
# NOTE ON SCOPE, stated so nobody later "optimises" it: this runner kills NOTHING
# on a timeout, federate or not. The list below is not a filter that decides who
# gets killed; it is the reason the kill path does not exist at all, written down
# where the timeout is handled so the rule stays legible. StopIface is not in the
# list because it does not join - but it is the tool that makes the interface
# RESIGN, so killing it is equally forbidden.
$FederateJoiningTools = @(
    'VrfC2SimApp',                              # the interface federate
    'WatchVrf', 'WatchVrf-trace', 'WatchVrf-precheck',  # the oracle federate
    'RtiProbe',                                 # stage 2c pre-launch RTI readiness gate (throwaway join/resign)
    'CreateOne', 'CreateOne-diagnostic',        # stage 7b throwaway injector
    'ResetVrf', 'SetSimRate'                    # not used here; listed so a future
                                                # caller inherits the rule
)

# ---- manifest ---------------------------------------------------------------
# Written to disk after EVERY stage, so an aborted or crashed run still leaves a
# manifest describing exactly how far it got.
$Manifest = [ordered]@{
    schema          = 'vrf-c2sim-run-manifest/1'
    scriptVersion   = $ScriptVersion
    scoring         = 'NONE. This runner collects evidence only. HEADLESS_RUN_PLAN.md sec 4a was RATIFIED 2026-07-19 before any data existed; sec 4a.6 declares run 1 a measurement, not an acceptance test. No threshold from 4a is embedded in this script, by design - collection and scoring are separate programs so a trace can be re-scored under an amended criterion without re-running the simulation.'
    dryRun          = [bool]$DryRun
    clocks          = [ordered]@{}
    host            = [ordered]@{}
    inputs          = [ordered]@{}
    tools           = [ordered]@{}
    appNumbers      = @()
    ledger          = [ordered]@{}
    preflight       = [ordered]@{}
    stages          = @()
    oracle          = [ordered]@{}
    artifacts       = [ordered]@{}
    validityFlags   = @()
    runnerExitCode  = $null
}
$RunDir       = $null
$ManifestPath = $null

function Save-Manifest {
    if ($DryRun -or -not $ManifestPath) { return }
    try {
        $Manifest.clocks.savedLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
        $Manifest.clocks.savedUtc   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        $json = $Manifest | ConvertTo-Json -Depth 12
        [System.IO.File]::WriteAllText($ManifestPath, $json, (New-Object System.Text.UTF8Encoding($false)))
    } catch {
        Say-Warn ('could not write the manifest: {0}' -f $_.Exception.Message)
    }
}

function Add-Flag {
    param([string]$Severity, [string]$Text)
    $Manifest.validityFlags += [ordered]@{
        severity  = $Severity
        text      = $Text
        atUtc     = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    }
    if ($Severity -eq 'FAIL') { Say-Fail $Text } elseif ($Severity -eq 'WARN') { Say-Warn $Text } else { Say-Info $Text }
}

function Add-Stage {
    param(
        [string]$Name, [string]$File, [string[]]$Arguments, [string]$Cwd,
        $ExitCode, [string]$StdOut, [string]$StdErr, [string]$Note, [string]$Outcome,
        [bool]$TimedOut = $false, $TimeoutSec = $null, $ProcessId = $null
    )
    $Manifest.stages += [ordered]@{
        name        = $Name
        commandLine = (Format-CommandLine -File $File -Arguments $Arguments)
        cwd         = $Cwd
        startedUtc  = $script:LastStageStartUtc
        endedUtc    = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        exitCode    = $ExitCode
        stdoutFile  = $StdOut
        stderrFile  = $StdErr
        outcome     = $Outcome
        timedOut    = $TimedOut
        timeoutSec  = $TimeoutSec
        processId   = $ProcessId
        federateJoining = ($FederateJoiningTools -contains $Name)
        note        = $Note
    }
    Save-Manifest
}

function Format-CommandLine {
    param([string]$File, [string[]]$Arguments)
    $parts = @()
    foreach ($a in @($Arguments)) {
        if ($null -eq $a) { continue }
        # An EMPTY argument is quoted too, so the MANIFEST line shows it explicitly rather
        # than silently collapsing two spaces into one. *** CORRECTED 2026-09-15: this used
        # to claim ProcessStartInfo.ArgumentList "passes it to the child as "" and it HOLDS
        # ITS POSITION" - FALSE for the Start-Process -ArgumentList this runner actually
        # uses (RUNBOOK 0.5.14 item 15 addendum): an empty element is silently DROPPED, so
        # for RtiProbe specifically this function's quoted "" in the logged line did NOT
        # match what the child actually received - the manifest recorded a command line
        # that never ran. Harmless here only because Format-CommandLine is display/manifest
        # only, never the actual invocation; the real fix is building RtiProbe's argument
        # list conditionally (Stage 2c), not this function. ***
        if ($a -eq '' -or $a -match '[\s"]') { $parts += ('"' + ($a -replace '"','\"') + '"') } else { $parts += $a }
    }
    if ($parts.Count -eq 0) { return $File }
    return ($File + ' ' + ($parts -join ' '))
}

$script:LastStageStartUtc = $null
# Set when the movement oracle (WatchVrf-trace) exits non-zero. Initialised HERE
# because the script runs under Set-StrictMode -Version Latest, where reading an
# unassigned variable is a terminating error - which would turn a crashed-oracle run
# into an exit-5 "unexpected error" and bury the real cause.
$script:OracleDied = $false

# ---- external invocation ----------------------------------------------------
# EVERY external process in this script goes through Invoke-External or
# Start-External. Neither ever assumes an exit code; both record what they got.
#
# =============================================================================
# WHY Invoke-External DOES NOT USE Start-Process -Wait (a 47-MINUTE DEADLOCK)
# =============================================================================
# Start-Process -Wait waits for the PROCESS TREE, not for the process. Microsoft's
# own Start-Process documentation, verbatim:
#     -Wait: "Indicates that this cmdlet waits for the specified process AND ITS
#      DESCENDANTS to complete before accepting more input."
#     NOTES: "When using the Wait parameter, Start-Process waits for the PROCESS
#      TREE (the process and all its descendants) to exit before returning
#      control. This is different than the behavior of the Wait-Process cmdlet,
#      which only waits for the specified processes to exit."
#
# Stage 3 invokes scripts/LaunchVrf.ps1, whose entire PURPOSE is to leave
# VR-Forces RUNNING. On 2026-07-19 LaunchVrf reached "[OK] READY" in 51 s and its
# own pwsh exited - but vrfGui and vrfSimHLA1516e are DESCENDANTS of that call
# (spawned via vrfLauncher). Start-Process -Wait therefore sat waiting for the
# simulator it had just started to exit: the runner blocked for 47 minutes at
# 1.3 s of CPU, would have blocked forever, and never reached stage 4.
#
# A COMPETING EXPLANATION THAT WAS TESTED AND FALSIFIED - do not "fix" it again:
# inherited stdout/stderr file handles are NOT the cause. The redirected log file
# was verified openable with EXCLUSIVE access while the runner was still blocked.
# The cause is purely the documented descendant-wait semantic.
#
# THE PATTERN USED INSTEAD: Start-Process -PassThru WITHOUT -Wait, then wait on
# the returned Process object. Process.WaitForExit waits for THAT PROCESS ONLY -
# the same guarantee the docs give Wait-Process ("only waits for the specified
# processes to exit"). A detached grandchild no longer holds the runner.
#
# THE EXIT-CODE GOTCHA, MEASURED RATHER THAN ASSUMED: a Process object whose
# native handle was never materialised reports ExitCode as EMPTY once the
# process is gone. That failure was reproduced on this machine - but only for an
# object obtained from Get-Process. An object returned by Start-Process
# -PassThru already owns its handle, and $p.ExitCode was verified to read back
# correctly (7) after WaitForExit BOTH with and without the explicit cache, on
# pwsh 7 / .NET here. So the '$null = $p.Handle' line below is DEFENSIVE, not
# load-bearing: it costs one property read, it guarantees the handle exists
# before the process can exit, and it makes the requirement explicit for anyone
# who later refactors this to obtain the process some other way. It is safe to
# keep and NOT safe to replace with a Get-Process lookup.
#
# THE TIMEOUT NEVER FORCE-KILLS. See $FederateJoiningTools.
# =============================================================================
function Invoke-External {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$File,
        [string[]]$Arguments = @(),
        [string]$Cwd,
        [string]$StdOutFile,
        [string]$StdErrFile,
        [string]$Note,
        # Wall-clock ceiling for THIS stage. Callers pass the stage's own known
        # blocking budget PLUS $StageTimeoutSec of slack; 0 means "slack only".
        [int]$TimeoutSec = 0
    )
    $cmd = Format-CommandLine -File $File -Arguments $Arguments
    $eff = if ($TimeoutSec -gt 0) { $TimeoutSec } else { $StageTimeoutSec }
    if ($DryRun) {
        Say-Plan ('STAGE {0}' -f $Name)
        Say      ('            cwd    : {0}' -f $Cwd)
        Say      ('            run    : {0}' -f $cmd)
        Say      ('            timeout: {0}s (waits for THE CHILD ONLY, never its descendants; on expiry the stage is recorded timed-out and NOTHING is killed)' -f $eff)
        if ($StdOutFile) { Say ('            stdout : {0}' -f $StdOutFile) }
        if ($StdErrFile) { Say ('            stderr : {0}' -f $StdErrFile) }
        if ($Note)       { Say ('            note   : {0}' -f $Note) }
        return [pscustomobject]@{ ExitCode = 0; DryRun = $true; TimedOut = $false; Outcome = 'dry-run' }
    }

    $script:LastStageStartUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    Say-Info ('{0}: {1}' -f $Name, $cmd)
    Say-Info ('{0}: waiting for the DIRECT CHILD only, up to {1}s' -f $Name, $eff)

    # NO Wait = $true here. See the block above.
    $sp = @{ FilePath = $File; WorkingDirectory = $Cwd; PassThru = $true; NoNewWindow = $true }
    if ($Arguments.Count -gt 0) { $sp.ArgumentList = $Arguments }
    if ($StdOutFile) { $sp.RedirectStandardOutput = $StdOutFile }
    if ($StdErrFile) { $sp.RedirectStandardError  = $StdErrFile }

    $code     = $null
    $outcome  = 'ran'
    $timedOut = $false
    $procId   = $null
    try {
        $p = Start-Process @sp
        if ($null -eq $p) { throw 'Start-Process returned no process object.' }
        $procId = $p.Id
        # Cache the native handle while the process is alive - see the header.
        # If this fails the process almost certainly exited instantly; say so,
        # because ExitCode below may then read back as $null and the caller will
        # fail the stage rather than guess.
        try { $null = $p.Handle } catch { Say-Warn ('{0}: could not cache the process handle ({1}). The exit code may not be readable.' -f $Name, $_.Exception.Message) }
        if ($p.WaitForExit($eff * 1000)) {
            $code = $p.ExitCode
            if ($null -eq $code) { $outcome = 'exit-code-unavailable' }
        } else {
            $timedOut = $true
            $outcome  = 'timed-out'
        }
    } catch {
        $outcome = 'could-not-start'
        $code = $null
        Say-Fail ('{0}: could not start: {1}' -f $Name, $_.Exception.Message)
    }
    Add-Stage -Name $Name -File $File -Arguments $Arguments -Cwd $Cwd -ExitCode $code `
              -StdOut $StdOutFile -StdErr $StdErrFile -Note $Note -Outcome $outcome `
              -TimedOut $timedOut -TimeoutSec $eff -ProcessId $procId
    if ($timedOut) {
        # NOT KILLED. RUNBOOK sec 0. The caller fails the run and the existing
        # finally-block teardown then does the clean thing (StopIface, then
        # StopVrf), which is the ONLY correct way to bring a federate down.
        $kind = if ($FederateJoiningTools -contains $Name) {
            'It JOINS THE FEDERATION, so force-killing it would leave a STALE FEDERATE and the next start would hang at RTI join (RUNBOOK sec 0).'
        } else {
            'It is not a federate-joining tool, but this runner force-kills NOTHING (RUNBOOK sec 0).'
        }
        Add-Flag 'FAIL' ('{0} (pid {1}) did not exit within {2}s. IT WAS NOT KILLED. {3} Teardown will run.' -f $Name, $procId, $eff, $kind)
    }
    if ($null -ne $code) { Say-Info ('{0}: EXIT={1}' -f $Name, $code) }
    return [pscustomobject]@{ ExitCode = $code; DryRun = $false; TimedOut = $timedOut; Outcome = $outcome }
}

# ---- the ONE place a stage result is turned into a go/no-go -----------------
# A missing exit code is NOT a pass. $code stays $null on both 'timed-out' and
# 'could-not-start'.
#
# WHAT THIS IS NOT: it is NOT rescuing the callers from a switch that ignores
# $null. That was checked rather than assumed, and the assumption was WRONG -
# `switch ($null) { default { ... } }` DOES run its default branch on pwsh 7.
# So a null exit code already reached each caller's `default` and already failed
# the run. This guard exists for a smaller and honest reason: `default` reports
# it as 'LaunchVrf exited  - undocumented code', which names the wrong problem
# to whoever reads the manifest at 3 a.m. Running before the switch lets the
# failure say "timed out, left running, never killed" instead.
# Consequence to preserve: on the paths where this guard calls Stop-Runner the
# switch never runs, so there is exactly one report. Do NOT add this guard in
# front of a switch whose default ALSO flags - that double-reports (which is
# why the two teardown stages below are handled inside their default branch).
function Test-StageProduced {
    param($Result)
    if ($DryRun) { return $true }
    return ($null -ne $Result.ExitCode)
}

function Get-StageFailureText {
    param([string]$Name, $Result)
    if ($Result.TimedOut) {
        return ('{0} did not exit within its stage timeout and was left running (never killed). No exit code exists, so the run cannot be judged on it. Failing cleanly through teardown.' -f $Name)
    }
    return ('{0} produced no exit code (outcome: {1}). Failing cleanly through teardown.' -f $Name, $Result.Outcome)
}

function Start-External {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$File,
        [string[]]$Arguments = @(),
        [string]$Cwd,
        [string]$StdOutFile,
        [string]$StdErrFile,
        # ---- the two below exist for ONE caller: the stage-6b watchdog (2026-09-14) -------
        # DEFAULTS PRESERVE THE EXACT PREVIOUS BEHAVIOUR of every other stage: no -StdInFile
        # means no stdin redirection, and no -NewConsole means -NoNewWindow exactly as before.
        #
        # -StdInFile: the child's stdin comes from this file instead of the runner's own.
        # Without it PowerShell fills hStdInput with the PARENT'S handle (it sets
        # STARTF_USESTDHANDLES whenever anything is redirected), so a child would hold the
        # launching terminal's stdin open for as long as it lives. Harmless for a child that
        # dies with the run; NOT harmless for one designed to outlive it.
        #
        # -NewConsole: do NOT pass -NoNewWindow, so the child gets CREATE_NEW_CONSOLE plus a
        # HIDDEN window instead of SHARING THE RUNNER'S CONSOLE. A console-sharing child
        # receives that console's Ctrl+C / Ctrl+Break and dies when the terminal closes -
        # i.e. it would die in exactly the scenario the watchdog exists to cover.
        [string]$StdInFile,
        [switch]$NewConsole,
        [string]$Note
    )
    $cmd = Format-CommandLine -File $File -Arguments $Arguments
    if ($DryRun) {
        Say-Plan ('STAGE {0}  (background{1})' -f $Name, $(if ($NewConsole) { ', OWN HIDDEN CONSOLE - detached' } else { '' }))
        Say      ('            cwd    : {0}' -f $Cwd)
        Say      ('            run    : {0}' -f $cmd)
        if ($StdOutFile) { Say ('            stdout : {0}' -f $StdOutFile) }
        if ($StdErrFile) { Say ('            stderr : {0}' -f $StdErrFile) }
        if ($StdInFile)  { Say ('            stdin  : {0}' -f $StdInFile) }
        if ($Note)       { Say ('            note   : {0}' -f $Note) }
        return $null
    }
    Say-Info ('{0} (background): {1}' -f $Name, $cmd)
    # Start-External was ALREADY correct with respect to the -Wait defect: it has
    # never passed -Wait, so it never waited on a process tree. It did share the
    # exit-code gotcha, though - the Process object it returns is read much later
    # (Complete-Background, and $AppProc.ExitCode in stages 6c/6d/8b and teardown),
    # by which time the process is usually gone. Cache the handle here too, while
    # the process is alive, so those reads are reliable.
    $sp = @{ FilePath = $File; WorkingDirectory = $Cwd; PassThru = $true }
    # -NoNewWindow and -WindowStyle are mutually exclusive in Start-Process; -NewConsole picks
    # the second. PowerShell's own CreateProcess path passes CREATE_NEW_CONSOLE (0x10) plus
    # STARTF_USESHOWWINDOW / SW_HIDE when CreateNoWindow is false and WindowStyle is Hidden.
    if ($NewConsole) { $sp.WindowStyle = 'Hidden' } else { $sp.NoNewWindow = $true }
    if ($Arguments.Count -gt 0) { $sp.ArgumentList = $Arguments }
    if ($StdOutFile) { $sp.RedirectStandardOutput = $StdOutFile }
    if ($StdErrFile) { $sp.RedirectStandardError  = $StdErrFile }
    if ($StdInFile)  { $sp.RedirectStandardInput  = $StdInFile }
    $p = Start-Process @sp
    try { $null = $p.Handle } catch { Say-Warn ('{0}: could not cache the process handle ({1}). Its exit code may read back as null later.' -f $Name, $_.Exception.Message) }
    $Manifest.stages += [ordered]@{
        name        = $Name
        commandLine = $cmd
        cwd         = $Cwd
        startedUtc  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        endedUtc    = $null
        exitCode    = $null
        stdoutFile  = $StdOutFile
        stderrFile  = $StdErrFile
        outcome     = 'started-background'
        note        = $Note
        processId   = $p.Id
    }
    Save-Manifest
    return $p
}

# NOT AFFECTED by the -Wait defect, and audited as such: this waits on a Process
# object with Process.WaitForExit, which waits for THAT PROCESS ONLY. WatchVrf and
# ListenReports spawn nothing, and even if they did, their descendants would not
# hold this wait. It also already does the right thing on expiry - records
# 'still-running' and leaves the federate alone.
#
# -CapSecs (review F1, docs/experiments/REVIEW_RUNNER_TURNAROUND_2026-09-01.md): in
# stop-file mode TimeoutSec is only the GRACE after the stop file was touched. If it
# expires the observer may not have seen the file (path mismatch, hung tick) and is
# possibly STILL JOINED; proceeding to StopVrf under a joined observer would lose the
# record's guarantee that the observers have exited before the sim comes down. So
# when CapSecs > 0 the wait is EXTENDED - never a kill - up to the observer's own
# duration cap (stage start + CapSecs + CapMarginSecs), which is when the tool ends
# itself regardless of the stop file. Only after THAT is 'still-running' recorded.
function Complete-Background {
    param([string]$Name, $Process, [int]$TimeoutSec, [string]$Note, [int]$CapSecs = 0, [int]$CapMarginSecs = 0)
    if ($DryRun -or $null -eq $Process) { return }
    Say-Info ('waiting for {0} (pid {1}) to finish on its own - it is NEVER killed' -f $Name, $Process.Id)
    $null = $Process.WaitForExit($TimeoutSec * 1000)
    $waitedSec = $TimeoutSec
    if (-not $Process.HasExited -and $CapSecs -gt 0) {
        # Grace expired in stop-file mode. Find when this observer's cap ends it.
        $startedUtc = $null
        foreach ($s in $Manifest.stages) {
            if ($s.name -eq $Name -and $s.outcome -eq 'started-background') { $startedUtc = ConvertFrom-ManifestUtc -Text $s.startedUtc; break }
        }
        if ($null -eq $startedUtc) { try { $startedUtc = $Process.StartTime.ToUniversalTime() } catch { $startedUtc = (Get-Date).ToUniversalTime() } }
        $capWait = Get-ObserverCapRemainingSecs -StartedUtc $startedUtc -DurationSecs $CapSecs -MarginSecs $CapMarginSecs -NowUtc (Get-Date).ToUniversalTime()
        Add-Flag 'WARN' ("{0} (pid {1}) did NOT exit within the {2}s grace after the stop file was touched - it may not have seen it. NOT killed. Waiting up to {3}s more for its own {4}s duration cap (+{5}s margin) so StopVrf does not run under a possibly still-joined observer." -f $Name, $Process.Id, $TimeoutSec, $capWait, $CapSecs, $CapMarginSecs)
        if ($capWait -gt 0) { $null = $Process.WaitForExit($capWait * 1000) }
        $waitedSec = $TimeoutSec + $capWait
    }
    $code = $null
    $outcome = 'still-running'
    if ($Process.HasExited) { $code = $Process.ExitCode; $outcome = 'exited' }
    foreach ($s in $Manifest.stages) {
        if ($s.name -eq $Name -and $null -eq $s.exitCode -and $s.outcome -eq 'started-background') {
            $s.endedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            $s.exitCode = $code
            $s.outcome  = $outcome
            if ($Note) { $s.note = $Note }
            break
        }
    }
    Save-Manifest
    if ($outcome -eq 'exited') { Say-Info ('{0}: EXIT={1}' -f $Name, $code) }
    else { Add-Flag 'WARN' ("{0} (pid {1}) had not exited after {2}s. NOT killed - it is a joined federate. It will resign on its own timer. The NEXT run's Stage 1 inventory REFUSES to launch while it is up." -f $Name, $Process.Id, $waitedSec) }
}

# ---- Stage 2h (STP-825) helpers --------------------------------------------
# THE RTIEXEC LOG IS THE JOIN AUTHORITY, because the holder's own stdout is not: RtiProbe
# writes its "[OK] ... created/joined <federation> and resigned cleanly" line AFTER the
# settle loop (tools/RtiProbe/Program.cs), i.e. only once the WHOLE hold has elapsed. A
# 45 s wait can therefore only be answered by the rtiexec's own
#     Federate remoteControl <pid> ("remoteControl" 2) has joined federation "<name>".
# line. Stage 2r knows which rtiexec is serving; this resolves ITS log file. The recorded
# path is a REAL path only when this run started the rtiexec - when it found one already up
# the marker reads "(not started by this run - see <dir>)" - so fall back to the file the
# RTI names after the serving pid (rtiexec_<stamp>...-<pid>.log).
function Get-RtiExecLogPath {
    param([string]$Recorded, [string]$LogDir, $RtiExecPid)
    if ($Recorded -and (Test-Path -LiteralPath $Recorded -PathType Leaf)) { return $Recorded }
    if ($RtiExecPid -and $LogDir -and (Test-Path -LiteralPath $LogDir -PathType Container)) {
        $f = @(Get-ChildItem -LiteralPath $LogDir -Filter ('rtiexec_*-{0}.log' -f $RtiExecPid) -ErrorAction SilentlyContinue |
               Sort-Object LastWriteTimeUtc -Descending) | Select-Object -First 1
        if ($f) { return $f.FullName }
    }
    return ''
}

# Read a live log from a byte OFFSET instead of loading the whole file. The rtiexec log runs
# to tens of thousands of lines (44k+ on 2026-09-15) and Stage 2h polls it once a second, so
# the seat's Get-Content | Select-Object -Skip would re-read megabytes per tick. FileShare
# ReadWrite because the rtiexec holds it open for writing; an Offset past the end means the
# file was rotated or truncated under us, so start again from 0 rather than return nothing.
function Read-TextFromOffset {
    param([string]$Path, [long]$Offset)
    if (-not $Path -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    try {
        $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        try {
            $from = $Offset
            if ($from -gt $fs.Length -or $from -lt 0) { $from = 0 }
            $null = $fs.Seek($from, [System.IO.SeekOrigin]::Begin)
            $sr = New-Object System.IO.StreamReader($fs)
            try { return $sr.ReadToEnd() } finally { $sr.Dispose() }
        } finally { $fs.Dispose() }
    } catch { return '' }
}

# ---- live-file reading (the trace is being written while we read it) --------
# The t+Ns in every stage-8b message is measured from the moment PUSHORDER RETURNED, which
# is up to -PushOrderListenSec AFTER the order actually reached the bus. Both numbers are
# printed so the two clocks can never be read as one (defect 4 of 2026-09-14). Empty until
# the bus log has been copied and parsed, and empty for ever if it could not be.
$script:OrderOnBusUtc = $null
function Get-OrderClockNote {
    if ($null -eq $script:OrderOnBusUtc) { return '' }
    return (', {0}s after the ORDER reached the bus at {1}' -f `
        [int]((Get-Date).ToUniversalTime() - [datetime]$script:OrderOnBusUtc).TotalSeconds,
        ([datetime]$script:OrderOnBusUtc).ToString('HH:mm:ss.fffZ'))
}

function Read-LiveText {
    param([string]$Path)
    if (-not $Path -or -not (Test-Path -LiteralPath $Path)) { return '' }
    $fs = $null; $sr = $null
    try {
        $share = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
        $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, $share)
        $sr = New-Object System.IO.StreamReader($fs)
        return $sr.ReadToEnd()
    } catch {
        return ''
    } finally {
        if ($sr) { $sr.Dispose() } elseif ($fs) { $fs.Dispose() }
    }
}

# ---- incremental reading of a live, growing log (added 2026-09-14) ----------
# Read-LiveText above reads the WHOLE file. The observation loop called it on the app
# log every 5 s; by t+127 s of the G6 run that log was ~40 MB, i.e. an ~80 MB UTF-16
# string plus a ~500,000-element String[] rebuilt six times a minute, and the status
# cadence had already stretched from 30 s to 46 s
# (docs/experiments/RUNNER_HARDENING_2026-09-14.md sec 5). Read-LiveDelta returns ONLY
# the bytes appended since the last call for the same $Key, cut back to the last
# COMPLETE line so a half-written line is never parsed-and-skipped: the remainder is
# re-read on the next poll. The offset is per-key script state, so use one key per file.
$script:LiveOffsets = @{}
function Read-LiveDelta {
    # $MaxBytes bounds the peak allocation even after a long stall; the rest of the
    # backlog is returned by the following calls, in order.
    param([string]$Path, [string]$Key, [int]$MaxBytes = 16777216)
    if (-not $Path -or -not $Key) { return '' }
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    if (-not $script:LiveOffsets.ContainsKey($Key)) { $script:LiveOffsets[$Key] = [long]0 }
    $fs = $null
    try {
        $share = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
        $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, $share)
        $start = [long]$script:LiveOffsets[$Key]
        $len   = [long]$fs.Length
        # A file that SHRANK was rotated or replaced under us. Start over rather than
        # seek past its end and silently read nothing for the rest of the run.
        if ($start -gt $len) { $start = [long]0 }
        if ($len -le $start) { return '' }
        $want = [int][Math]::Min([long]$MaxBytes, ($len - $start))
        $null = $fs.Seek($start, [System.IO.SeekOrigin]::Begin)
        $buf  = New-Object byte[] $want
        $got  = 0
        while ($got -lt $want) {
            $n = $fs.Read($buf, $got, $want - $got)
            if ($n -le 0) { break }
            $got += $n
        }
        if ($got -le 0) { return '' }
        # 0x0A cannot occur inside a UTF-8 multi-byte sequence, so cutting on the byte
        # is safe and never splits a character.
        $cut = [Array]::LastIndexOf($buf, [byte]10, $got - 1)
        if ($cut -lt 0) { return '' }   # no complete line yet - offset deliberately unchanged
        $script:LiveOffsets[$Key] = $start + $cut + 1
        return [System.Text.Encoding]::UTF8.GetString($buf, 0, $cut + 1)
    } catch {
        return ''
    } finally {
        if ($fs) { $fs.Dispose() }
    }
}

# ---- stage 7d READY GATE state: the nav-area acquisition watcher ------------
# The gate's evidence is two lines of the interface log, and they arrive at
# DIFFERENT STAGES: the PLACEMENT lines land during the stage-7 oracle wait, the
# "New Primary nav area" row lands after it. One watcher, one incremental reader
# key, called from BOTH loops, so the placement instant is stamped WHEN IT HAPPENS
# rather than when the gate starts. Stamping it at gate start would silently
# UNDER-report the delta by the length of the oracle wait and could call a cold
# machine warm - the one thing this indicator exists to tell apart.
#
# Wall clocks, not log clocks: vrfc2simapp.log carries no per-line timestamp, so
# both instants are the runner's own observation times and carry the polling
# interval as their resolution (5 s in the oracle loop, 2 s in the gate loop).
# That is far below the 10 s / 240 s the indicator has to separate, and it is the
# reason the printed delta is reported to 0.1 s but must not be read as one.
$script:NavGate = [ordered]@{
    firstPlacementUtc  = $null
    firstPlacementName = $null
    firstPlacementKind = $null
    placementRowsSeen  = 0
    firstRow           = $null
    firstRowUtc        = $null
    areaRowsSeen       = 0
}
function Update-NavGateWatch {
    param([string]$AppLogPath)
    $delta = Read-LiveDelta -Path $AppLogPath -Key 'applog-navgate'
    if ([string]::IsNullOrEmpty($delta)) { return }
    $nowUtc = (Get-Date).ToUniversalTime()
    $pl = @(Get-PlacementRows -AppLogText $delta)
    if ($pl.Count -gt 0) {
        $script:NavGate.placementRowsSeen += $pl.Count
        if ($null -eq $script:NavGate.firstPlacementUtc) {
            $script:NavGate.firstPlacementUtc  = $nowUtc
            $script:NavGate.firstPlacementName = $pl[0].name
            $script:NavGate.firstPlacementKind = $pl[0].kind
        }
    }
    $rows = @(Get-NavAreaRows -AppLogText $delta)
    if ($rows.Count -gt 0) {
        $script:NavGate.areaRowsSeen += $rows.Count
        if ($null -eq $script:NavGate.firstRow) {
            $script:NavGate.firstRow    = $rows[0]
            $script:NavGate.firstRowUtc = $nowUtc
        }
    }
}

# ---- the RUNBOOK 0.5.7 CORRECTED coordinate criterion -----------------------
# PASS = at least one POS line whose lat/lon are real numbers, NOT NaN, and NOT
#        the 90.000000,-90.000000 pole placeholder.
# This function answers ONLY "is this coordinate real". It says nothing about
# distance, arrival or movement - those are the scorer's job (4a), not this
# script's.
#
# *** HARDENED 2026-07-19 AFTER A FALSE GREEN ON LIVE GARBAGE. ***
# Run 20260719T185814Z: WatchVrf CRASHED (0xC0000005) after emitting ONE POS line,
# and this gate declared "ORACLE GATE PASSED: 1 real-coordinate POS lines" on it:
#
#     POS,3,VRF_UUID:cde66adc-...,0.000001,-90.000000,1020484223153767.2
#
# lat 1e-6, lon -90, ALTITUDE 1.02e15 METRES. It satisfied every clause of the old
# rule - not NaN, |lat| well under the 89.999999 pole threshold, |lon| <= 180 - while
# being obvious nonsense read through a bad pointer. THE ALTITUDE WAS THE TELL AND THE
# GATE NEVER LOOKED AT IT.
#
# This is the SAME CLASS OF FAILURE as the retracted "reflected>0" criterion that
# 0.5.7 documents: a validity gate passing on degenerate data. Two lessons applied:
#   1. CHECK EVERY FIELD YOU ARE GIVEN. The altitude column was sitting right there.
#   2. A gate that can pass on a run that CRASHED is not a gate. See also the separate
#      hardening of the crash/exit-code handling - a run whose oracle died must never
#      report success no matter what the trace contains.
function Get-RealPositions {
    param([string]$TraceText)
    $real = @()
    $degenerate = 0
    $posLines = 0
    foreach ($line in ($TraceText -split "`r?`n")) {
        if (-not $line.StartsWith('POS,')) { continue }
        $posLines++
        $f = $line.Split(',')
        if ($f.Length -lt 6) { $degenerate++; continue }
        $lat = 0.0; $lon = 0.0
        $styles = [System.Globalization.NumberStyles]::Float
        $inv    = [System.Globalization.CultureInfo]::InvariantCulture
        if (-not [double]::TryParse($f[3], $styles, $inv, [ref]$lat)) { $degenerate++; continue }
        if (-not [double]::TryParse($f[4], $styles, $inv, [ref]$lon)) { $degenerate++; continue }
        if ([double]::IsNaN($lat) -or [double]::IsNaN($lon) -or
            [double]::IsInfinity($lat) -or [double]::IsInfinity($lon)) { $degenerate++; continue }
        # The pole placeholder. Compared with a tolerance rather than for equality
        # because the trace prints F6 and an exact string match is brittle.
        if ([Math]::Abs($lat) -ge 89.999999) { $degenerate++; continue }
        if ([Math]::Abs($lon) -gt 180.0)     { $degenerate++; continue }

        # ADDED 2026-07-19 - ALTITUDE SANITY. This is the check that would have caught
        # the false green. A bogus pointer read produced alt = 1.02e15 m. Nothing in
        # this project legitimately exceeds 100 km MSL: CreateOne deliberately spawns at
        # 10000 m so the ground clamp can drop it, and clamped ground units land near
        # 1040 m at Mojave. Anything past 100 km is not a position, it is memory.
        $alt = 0.0
        if (-not [double]::TryParse($f[5], $styles, $inv, [ref]$alt)) { $degenerate++; continue }
        if ([double]::IsNaN($alt) -or [double]::IsInfinity($alt))     { $degenerate++; continue }
        if ([Math]::Abs($alt) -gt 100000.0)                           { $degenerate++; continue }

        # ADDED 2026-07-19 - EQUATOR/NULL-ISLAND PLACEHOLDER. VR-Forces parks
        # positionless objects at an ECEF placeholder that converts to lat ~9e-6 (the
        # scenario .oob shows GlblTerrDmg and GlobalEnv at ECEF (6378137,1,1)). No
        # scenario this project runs is within 100 m of the equator - Mojave is ~34.6N,
        # Sweden ~58.6N - so a near-zero latitude here is a placeholder, never a fix.
        if ([Math]::Abs($lat) -lt 0.001) { $degenerate++; continue }

        $real += [ordered]@{ line = $line; uuid = $f[2]; lat = $lat; lon = $lon; alt = $alt }
    }
    return [pscustomobject]@{
        PosLineCount   = $posLines
        RealCount      = $real.Count
        DegenerateCount= $degenerate
        First          = $(if ($real.Count -gt 0) { $real[0] } else { $null })
        Uuids          = @($real | ForEach-Object { $_.uuid } | Select-Object -Unique)
    }
}

# Read only the LAST $TailBytes of a live file (default 256 KB). For the observation-window status
# line, which needs nothing but the newest "# t=..." summary: Read-LiveText's ReadToEnd of a trace
# growing at ~17 MB/min (object consoles on) inside a 32-bit host killed the runner at 176 MB on
# 2026-09-07 (PREREG_ASSEMBLY_LAYOUT sec 4, runner incident). Same share mode as Read-LiveText.
function Read-LiveTail {
    param([string]$Path, [int]$TailBytes = 262144)
    if (-not $Path -or -not (Test-Path -LiteralPath $Path)) { return '' }
    $fs = $null
    try {
        $share = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
        $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, $share)
        $len = $fs.Length
        $start = [Math]::Max(0, $len - $TailBytes)
        $null = $fs.Seek($start, [System.IO.SeekOrigin]::Begin)
        $buf = New-Object byte[] ($len - $start)
        $read = $fs.Read($buf, 0, $buf.Length)
        $text = [System.Text.Encoding]::UTF8.GetString($buf, 0, $read)
        # drop the (possibly partial) first line when we did not start at 0
        if ($start -gt 0) { $nl = $text.IndexOf("`n"); if ($nl -ge 0) { $text = $text.Substring($nl + 1) } }
        return $text
    } catch {
        return ''
    } finally {
        if ($fs) { $fs.Dispose() }
    }
}

# Find the LAST line starting with $Prefix by walking BACKWARD in $BlockBytes blocks,
# newest first, stopping at the first block that holds one. Peak allocation is ONE
# block - not the file, and not a fixed tail.
#
# WHY (2026-09-14): the status line used Read-LiveTail's fixed 256 KB window. With the
# member consoles at notify level 4 each CON row of the trace is a full XML document, so
# WatchVrf's "# t=..." summaries end up ~1.6 MB apart: the last 256 KB of the G6 trace
# held 724 CON rows and ZERO summaries, and the only instrument the observation window
# has printed "trace: (no samples)" for the whole run against a file holding 162k POS
# rows (docs/experiments/RUNNER_HARDENING_2026-09-14.md sec 4). Enlarging the tail is
# not the fix - 4 MB still held only 2 summaries and costs an 8 MB string every 30 s.
#
# Only COMPLETE lines are returned: the newest block is truncated at its last newline
# (the file is being appended to while we read it), and a line straddling a block
# boundary is rejoined through $carry. $MaxScanBytes bounds the work when the prefix is
# absent from the file entirely.
function Get-LastLineWithPrefix {
    param([string]$Path, [string]$Prefix, [int]$BlockBytes = 1048576, [long]$MaxScanBytes = 33554432)
    if (-not $Path -or -not $Prefix) { return $null }
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $fs = $null
    try {
        $share = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
        $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, $share)
        $end     = [long]$fs.Length
        $scanned = [long]0
        $carry   = ''
        $first   = $true
        while ($end -gt 0 -and $scanned -lt $MaxScanBytes) {
            $start = [long][Math]::Max(0, $end - $BlockBytes)
            $null  = $fs.Seek($start, [System.IO.SeekOrigin]::Begin)
            $want  = [int]($end - $start)
            $buf   = New-Object byte[] $want
            $got   = 0
            while ($got -lt $want) {
                $n = $fs.Read($buf, $got, $want - $got)
                if ($n -le 0) { break }
                $got += $n
            }
            $text = [System.Text.Encoding]::UTF8.GetString($buf, 0, $got)
            if ($first) {
                # the newest block can end in a HALF-WRITTEN line; never return one
                $lastNl = $text.LastIndexOf("`n")
                $text = $(if ($lastNl -ge 0) { $text.Substring(0, $lastNl) } else { '' })
                $first = $false
            } else {
                $text = $text + $carry
            }
            $carry = ''
            if ($start -gt 0) {
                # this block's own first line began in the block BEFORE it - hand it back
                $nl = $text.IndexOf("`n")
                if ($nl -ge 0) { $carry = $text.Substring(0, $nl + 1); $text = $text.Substring($nl + 1) }
                else           { $carry = $text; $text = '' }
            }
            $hit = $null
            foreach ($line in ($text -split "`r?`n")) { if ($line.StartsWith($Prefix)) { $hit = $line } }
            if ($hit) { return $hit }
            $scanned += $got
            $end = $start
        }
        return $null
    } catch {
        return $null
    } finally {
        if ($fs) { $fs.Dispose() }
    }
}

function Get-TraceSummaryLine {
    # The "# t=..s reflected=N readable=M" line WatchVrf emits after each sample.
    param([string]$TraceText)
    $last = $null
    foreach ($line in ($TraceText -split "`r?`n")) {
        if ($line.StartsWith('# t=')) { $last = $line }
    }
    return $last
}

# ---- stage 7b: uuid matching between CreateOne and the trace -----------------
# CreateOne prints the raw uuid the backend assigned; WatchVrf prints it in the
# POS line's field 2, which on the shape recorded in RUNBOOK 0.5.7 is PREFIXED
# ("VRF_UUID:adfaadb3-..."). Neither format is contractual, so match tolerantly
# and in BOTH directions rather than assume one decorates the other. A false
# NON-match here would silently mis-report ORACLE_BLIND, which is the exact wrong
# diagnosis to hand an operator.
function Test-UuidMatch {
    param([string]$Reported, [string]$FromTrace)
    if ([string]::IsNullOrWhiteSpace($Reported) -or [string]::IsNullOrWhiteSpace($FromTrace)) { return $false }
    $a = $Reported.Trim();  if ($a -match '(?i)^VRF_UUID:(.+)$') { $a = $Matches[1] }
    $b = $FromTrace.Trim(); if ($b -match '(?i)^VRF_UUID:(.+)$') { $b = $Matches[1] }
    if ($a.Length -eq 0 -or $b.Length -eq 0) { return $false }
    if ($a -eq $b) { return $true }
    return ($a.IndexOf($b, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
            $b.IndexOf($a, [System.StringComparison]::OrdinalIgnoreCase) -ge 0)
}

# =============================================================================
# STAGE 7b - THE CreateOne DISAMBIGUATION DIAGNOSTIC (failure path only)
# =============================================================================
# Runs ONLY when the stage-7 oracle gate has FAILED. It answers ONE question:
# when units dispatched but no real coordinate ever appeared, was the ORACLE
# blind, or did OUR INIT create nothing usable? It NEVER rescues the run.
#
# WHY IT WATCHES THE EXISTING TRACE INSTEAD OF STARTING A DEDICATED WatchVrf:
# the thing under suspicion IS the stage-5 WatchVrf federate that produced the
# failing trace. Injecting the entity and then observing it through a DIFFERENT,
# freshly joined WatchVrf would change two variables at once (new entity AND new
# oracle federate) and could produce the one genuinely uninterpretable outcome -
# real coordinates in the new watcher, none in the scoring trace - which names
# neither cause. Reusing the live trace holds the oracle CONSTANT so the entity
# is the only new variable, which is precisely what makes the verdict readable.
# It also consumes no second appNumber and adds no second join.
function Invoke-CreateOneDiagnostic {
    param([int]$AppNumber, [string]$TracePath, $WatchProcess, [int]$WatchSec)

    Say-Head 'Stage 7b - CreateOne DISAMBIGUATION DIAGNOSTIC (stage 7 FAILED; RUNBOOK 0.5.7 STRONGER CHECK)'
    Say '  The gate found no real-coordinate POS line. That alone cannot tell ORACLE_BLIND'
    Say '  (a federation/discovery fault) from CREATION_LAYER_SUSPECT (a type-mapping/creation'
    Say '  fault). CreateOne injects a KNOWN-GOOD M1A2 at a known coordinate; whether the SAME'
    Say '  oracle then reads it decides which of the two it is.'
    Say '  THIS DOES NOT RESCUE THE RUN. The run still fails; only the reason and the evidence change.'

    $verdict = 'INCONCLUSIVE'
    $reason  = $null
    $uuid    = $null
    $matchLine = $null
    $exitCode  = $null
    $otherReal = @()
    $appNumberWasConsumed = $false

    if ($DryRun) {
        Say-Plan 'STAGE 7b IS NOT REACHED ON A HEALTHY RUN. Shown here because a dry run prints the whole plan.'
    }

    if (-not $CreateOneAvailable) {
        $reason = ('CreateOne.exe not present at {0}, so the disambiguation could not be attempted.' -f $ExeCreateOne)
        Say-Warn $reason
    } elseif ((-not $DryRun) -and $null -ne $WatchProcess -and $WatchProcess.HasExited) {
        # No live oracle left to observe through. Creating a throwaway now would
        # burn the appNumber and prove nothing.
        $reason = 'the stage-5 WatchVrf trace federate had already exited, so there was no live oracle to observe the injected entity through. NOT creating a throwaway entity that nothing could see.'
        Say-Warn $reason
    } else {
        # Defaults are deliberate: CreateOne's built-in M1A2 at the COA-STP1 AO
        # coordinate, altitude 10000 m MSL (the buried-birth-altitude fix; ground
        # clamp brings it down). Only the appNumber is passed, so this stays the
        # RUNBOOK's check and not a variant of it.
        $r = Invoke-External -Name 'CreateOne-diagnostic' -File $ExeCreateOne `
                -Arguments @([string]$AppNumber) -Cwd $Bin64 `
                -StdOutFile $PathCreateOneOut -StdErrFile $PathCreateOneErr `
                -TimeoutSec $StageTimeoutSec `
                -Note 'STAGE 7b FAILURE-PATH DIAGNOSTIC. exit 0 created and uuid reported; 1 join/backend/create failed; 2 usage. Defaults only (M1A2 at the COA-STP1 AO coord, 10000 m MSL) so this stays the RUNBOOK 0.5.7 STRONGER CHECK verbatim.'
        $exitCode = $r.ExitCode
        # The number is BURNED the moment CreateOne is launched, whatever happens
        # after. Deriving "consumed" from the exit code alone would call a
        # timed-out CreateOne unconsumed and invite recycling a number that has
        # already joined - the exact stale-federate trap Appendix B exists to
        # prevent.
        $appNumberWasConsumed = ($r.Outcome -ne 'could-not-start')

        if ($DryRun) {
            Say-Plan ('would parse the uuid out of {0}, then poll the EXISTING trace {1} for up to {2}s for a POS line carrying that uuid with REAL lat/lon' -f $PathCreateOneOut, $TracePath, $WatchSec)
            Say-Plan 'would then record ORACLE_BLIND / CREATION_LAYER_SUSPECT / INCONCLUSIVE in the manifest and FAIL the run regardless'
            return $null
        }

        $coText = Read-LiveText -Path $PathCreateOneOut
        # Prefer the RESULT block; fall back to the ObjectCreated line.
        $um = [regex]::Match($coText, '(?m)^\s*uuid\s*:\s*(\S+)\s*$')
        if (-not $um.Success) { $um = [regex]::Match($coText, 'uuid=(\S+)') }
        if ($um.Success) { $uuid = $um.Groups[1].Value }

        if ($exitCode -ne 0 -or -not $uuid) {
            $reason = ('CreateOne exited {0} and reported uuid [{1}] - no known-good entity was proven to exist, so nothing can be concluded about the oracle from its absence. See {2}.' -f $(if ($null -ne $exitCode) { [string]$exitCode } else { ('NO EXIT CODE - outcome ' + $r.Outcome) }), $(if ($uuid) { $uuid } else { '(none)' }), $PathCreateOneOut)
            Say-Warn $reason
        } else {
            Say-Ok ('CreateOne created a known-good M1A2, uuid={0}. Watching the EXISTING trace for up to {1}s.' -f $uuid, $WatchSec)
            $deadline = (Get-Date).AddSeconds($WatchSec)
            while ($true) {
                $tt  = Read-LiveText -Path $TracePath
                $rp  = Get-RealPositions -TraceText $tt
                foreach ($p in @($rp.Uuids)) {
                    if (Test-UuidMatch -Reported $uuid -FromTrace $p) { $matchLine = $p }
                }
                if ($matchLine) {
                    # Re-scan for the full line, not just the uuid.
                    foreach ($line in ($tt -split "`r?`n")) {
                        if ($line.StartsWith('POS,')) {
                            $f = $line.Split(',')
                            if ($f.Length -ge 6 -and (Test-UuidMatch -Reported $uuid -FromTrace $f[2])) { $matchLine = $line; break }
                        }
                    }
                    break
                }
                if ((Get-Date) -ge $deadline) { break }
                Say-Info ('  no real-coordinate POS for the CreateOne uuid yet - {0}' -f $(if (Get-TraceSummaryLine -TraceText $tt) { Get-TraceSummaryLine -TraceText $tt } else { 'no samples yet' }))
                Start-Sleep -Seconds 5
            }

            # Anything else that turned real during this window matters: if our own
            # C2SIM units appeared while we were watching, the gate simply timed out
            # early and NEITHER failure mode is established.
            $rpEnd = Get-RealPositions -TraceText (Read-LiveText -Path $TracePath)
            $otherReal = @(@($rpEnd.Uuids) | Where-Object { -not (Test-UuidMatch -Reported $uuid -FromTrace $_) })

            if ($otherReal.Count -gt 0) {
                $verdict = 'INCONCLUSIVE'
                $reason  = ('real-coordinate POS lines appeared during the diagnostic window for {0} uuid(s) that are NOT the CreateOne entity [{1}]. The premise of this diagnostic - that NOTHING reads real - no longer holds, so neither failure mode is established. The likeliest reading is that the stage-7 gate timed out too early; -OracleGateTimeoutSec was {2}s.' -f $otherReal.Count, ($otherReal -join ','), $OracleGateTimeoutSec)
            } elseif ($matchLine) {
                $verdict = 'CREATION_LAYER_SUSPECT'
                $reason  = ('the oracle read REAL coordinates for the injected known-good entity ({0}) while reading none for any C2SIM unit. The oracle, the federation and discovery are therefore WORKING; the suspect is our creation path - type mapping, the init, or the interface never actually creating entities.' -f $matchLine)
            } else {
                $verdict = 'ORACLE_BLIND'
                $reason  = ('a known-good entity provably EXISTS (CreateOne exited 0 with uuid {0}) and the oracle read no real coordinate for it either, within {1}s. The fault is NOT in our creation path - it is in the oracle / federation / discovery layer. This is the RUNBOOK 0.5.7 STOP condition, genuinely met.' -f $uuid, $WatchSec)
            }
        }
    }

    if ($DryRun) { return $null }

    $Manifest.oracle.createOneVerdict = $verdict
    $Manifest.oracle.createOneDiagnostic = [ordered]@{
        whatThisIs      = 'RUNBOOK 0.5.7 STRONGER CHECK, run ONLY because the stage-7 oracle gate FAILED. tools/CreateOne injects a KNOWN-GOOD M1A2 at a known coordinate and the SAME live trace is watched for it. Its purpose is to tell a blind oracle apart from a broken creation path. It is a DIAGNOSTIC: it does NOT rescue the run and it is NOT a score.'
        verdict         = $verdict
        verdictMeanings = [ordered]@{
            ORACLE_BLIND           = 'CreateOne provably created an entity, and the oracle read no real coordinate for it either. The fault is in the oracle / federation / discovery layer, NOT in our creation path.'
            CREATION_LAYER_SUSPECT = 'The oracle read REAL coordinates for the injected entity but none for any C2SIM unit. The oracle is WORKING; the suspect is our creation path (type mapping, init handling, or the interface creating nothing usable).'
            INCONCLUSIVE           = 'The diagnostic could not be run, could not prove the injected entity exists, had no live oracle to watch, or its premise was violated because unrelated real coordinates appeared meanwhile. NEITHER failure mode is established.'
        }
        reason          = $reason
        appNumber       = $AppNumber
        appNumberConsumed = $appNumberWasConsumed
        createOneExit   = $exitCode
        createdUuid     = $uuid
        matchedTraceLine= $matchLine
        watchSec        = $WatchSec
        watchedTrace    = $TracePath
        otherRealUuidsDuringWindow = @($otherReal)
        stdoutFile      = $PathCreateOneOut
        throwawayEntity = 'A throwaway entity was injected into the LIVE scenario. It is confined to the back-end in-memory scenario and is NEVER written to the scenario file. This run is already FAILED and therefore UNSCORED, and teardown brings VR-Forces down via StopVrf.ps1, which destroys it - so the RUNBOOK 0.5.7 relaunch is already satisfied and no extra launch cycle is performed.'
        rescuesRun      = $false
    }
    Save-Manifest

    Say-Head 'Stage 7b - CONCLUSION'
    switch ($verdict) {
        'ORACLE_BLIND'           { Say-Fail 'VERDICT: ORACLE_BLIND - the oracle could not read a known-good entity either.' }
        'CREATION_LAYER_SUSPECT' { Say-Fail 'VERDICT: CREATION_LAYER_SUSPECT - the oracle IS working; our creation path is the suspect.' }
        default                  { Say-Warn 'VERDICT: INCONCLUSIVE - the disambiguation did not resolve.' }
    }
    Say ('  {0}' -f $reason)
    Say  '  The run FAILS either way. This stage changed the exit REASON and the recorded EVIDENCE, not the outcome.'
    return $verdict
}

# =============================================================================
# STAGE 8b - THE Q5 PAUSE / RESUME PROBE (-PauseAtSec / -ResumeAtSec)
# =============================================================================
# ONE invocation of tools/PauseSim, inside the observation window, at an offset measured from
# the moment PUSHORDER RETURNED - the same t+Ns clock every other stage-8b message uses.
#
# WHY A WHOLE PROCESS AND NOT A CALL: this runner holds no federation connection of its own
# and must not acquire one. Every VR-Forces control it issues is a short-lived federate that
# joins, sends and RESIGNS (RtiProbe, CreateOne, the observers). PauseSim is that same shape,
# and the price of the shape is one ledgered appNumber per invocation - which is exactly why
# the pause and the resume take two.
#
# IT BLOCKS THE POLL LOOP for the tool's own ~20 s (settle up to 15 s + flush 3-10 s + a 2 s
# clock hold). That is deliberate: the probe is a MEASUREMENT, and the scenario-clock reading
# it takes has to be the one at THAT offset, not one a background job produces later. With the
# probe armed the loop polls every 5 s, so at most one completion poll is displaced.
# ITS CEILING IS THE ONE PLACE THIS DEPARTS FROM THE -StageTimeoutSec CONVENTION, and on purpose:
# every other stage gets its own budget PLUS the full 600 s slack, but this one runs INSIDE the
# observation window, so a hung probe under that rule would silently eat ten minutes of it. The
# ceiling is min(-StageTimeoutSec, 120) - never below 60 (the parameter's own floor) and therefore
# never less than twice the tool's ~30 s worst case, so a HEALTHY probe can still not be cut off.
#
# IT NEVER FAILS THE RUN. A pause that does not take is recorded (the tool's own verdict is
# CONTRADICTED and its exit code is 1) and the window carries on: the run's evidence is the
# trace and the reports, and destroying that because a probe missed would cost more than the
# probe is worth. What must never happen is a run LABELLED as a pause probe with no record of
# whether the pause landed - so everything the tool reported goes in the manifest.
function Invoke-PauseSimProbe {
    param(
        [Parameter(Mandatory)][ValidateSet('pause','resume')][string]$Action,
        [Parameter(Mandatory)][int]$AppNumber,
        [Parameter(Mandatory)][string]$StdOutFile,
        [Parameter(Mandatory)][string]$StdErrFile,
        [Parameter(Mandatory)][int]$AtSec,
        [Parameter(Mandatory)][int]$ElapsedSec
    )

    $firedUtc = (Get-Date).ToUniversalTime()
    Say-Info ('  Q5 PROBE: {0} at t+{1}s (asked for t+{2}s) - tools/PauseSim, appNumber {3}. This BLOCKS the poll loop for ~20 s.' -f `
        $Action.ToUpperInvariant(), $ElapsedSec, $AtSec, $AppNumber)

    $r = Invoke-External -Name ('PauseSim-' + $Action) -File $ExePauseSim `
            -Arguments @($Action, [string]$AppNumber, $FederationArg) -Cwd $Bin64 `
            -StdOutFile $StdOutFile -StdErrFile $StdErrFile `
            -TimeoutSec ([Math]::Min($StageTimeoutSec, 120)) `
            -Note 'STAGE 8b Q5 PROBE. exit 0 issued and read back; 1 not joined / no back end / the back end CONTRADICTED the command; 2 usage. Prints one "[RESULT] PauseSim ..." line; the run is NOT failed on any of them.'

    $probe = [ordered]@{
        action            = $Action
        askedAtSec        = $AtSec
        firedAtTPlusSec   = $ElapsedSec
        firedUtc          = $firedUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        appNumber         = $AppNumber
        # BURNED the moment the tool is LAUNCHED, whatever happens after - the same rule stage
        # 7b applies to CreateOne. Deriving "consumed" from the exit code would call a timed-out
        # PauseSim unconsumed and invite recycling a number that has already joined.
        appNumberConsumed = ($r.Outcome -ne 'could-not-start')
        exitCode          = $r.ExitCode
        outcome           = $r.Outcome
        stdoutFile        = $StdOutFile
        resultLine        = $null
        verdict           = $null
        fields            = [ordered]@{}
    }
    if ($DryRun) { return $probe }

    # The tool's [RESULT] line is a CONTRACT (tools/PauseSim/Program.cs): "[RESULT] PauseSim "
    # then space-separated key=value pairs with no spaces inside a value. Parsed generically so
    # a field added at the end of that line lands in the manifest without a change here.
    $text = Read-LiveText -Path $StdOutFile
    $m = [regex]::Match($text, '(?m)^\[RESULT\] PauseSim (.+?)\s*$')
    if ($m.Success) {
        $probe.resultLine = ('[RESULT] PauseSim ' + $m.Groups[1].Value)
        foreach ($kv in ($m.Groups[1].Value -split '\s+')) {
            $eq = $kv.IndexOf('=')
            if ($eq -gt 0) { $probe.fields[$kv.Substring(0, $eq)] = $kv.Substring($eq + 1) }
        }
        if ($probe.fields.Contains('verdict')) { $probe.verdict = $probe.fields['verdict'] }
    }

    if (-not (Test-StageProduced -Result $r)) {
        Add-Flag 'WARN' ('the stage-8b {0} probe produced no exit code ({1}); the window was NOT cut short. See {2}.' -f $Action, $r.Outcome, $StdErrFile)
    } elseif ($r.ExitCode -eq 0 -and $probe.verdict -and $probe.verdict -notmatch 'UNCONFIRMED') {
        Say-Ok ('  Q5 PROBE {0}: {1}' -f $Action, $probe.resultLine)
    } else {
        Add-Flag 'WARN' ('the stage-8b {0} probe exited {1} with verdict [{2}]. The scenario may NOT be {3} - do not score the rest of the window as if it were. Line: {4}' -f `
            $Action, $r.ExitCode, $(if ($probe.verdict) { $probe.verdict } else { 'no [RESULT] line' }), `
            $(if ($Action -eq 'pause') { 'paused' } else { 'running' }), $(if ($probe.resultLine) { $probe.resultLine } else { '(none)' }))
    }
    return $probe
}

# =============================================================================
# STAGE 0 - VALIDATE EVERYTHING, BEFORE ANYTHING IS LAUNCHED
# =============================================================================
$nowLocal = Get-Date
$nowUtc   = $nowLocal.ToUniversalTime()
# The run's start, FROZEN. $nowUtc is re-assigned inside the stage-8b poll loop, so anything
# that needs "when did this run begin" - the date for the report capture's date-less
# [HH:mm:ss.fff] stamps, and for c2sim-bus.log's - must read this instead.
$RunStartUtc = $nowUtc
$stamp    = $nowUtc.ToString('yyyyMMddTHHmmssZ')

Say-Head ('RunC2SimScenario.ps1 v{0} ({1})' -f $ScriptVersion, $(if ($DryRun) { 'DRY-RUN' } else { 'LIVE' }))
Say ('  local clock : {0}' -f $nowLocal.ToString('yyyy-MM-dd HH:mm:ss zzz'))
Say ('  UTC clock   : {0}   <- this machine stamps logs UTC' -f $nowUtc.ToString('yyyy-MM-dd HH:mm:ss'))
Say ('  repo root   : {0}' -f $RepoRoot)
Say ('  init        : {0}' -f $Init)
Say ('  order       : {0}' -f $Order)
Say ('  RunSecs     : {0}' -f $RunSecs)
Say ''
Say '  THIS SCRIPT DOES NOT SCORE. It collects evidence. HEADLESS_RUN_PLAN sec 4a'
Say '  was RATIFIED 2026-07-19; sec 4a.6 makes run 1 a measurement, not a test.'

# THE LICENCE, resolved from the registry and pinned onto THIS process BEFORE anything
# is launched, so LaunchVrf52, the sim, the gui, the interface, both observers and every
# tool inherit the same file - never the stale one this process tree may have started
# with (RUNBOOK 0.5.15). Runs in -DryRun too: the dry run is what proves the wiring.
$LicInfo = Resolve-MakLicenseFile

$Manifest.clocks.startLocal    = $nowLocal.ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
$Manifest.clocks.startUtc      = $nowUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
$Manifest.clocks.timeZoneId    = [System.TimeZoneInfo]::Local.Id
$Manifest.clocks.utcOffsetHours= [System.TimeZoneInfo]::Local.GetUtcOffset($nowLocal).TotalHours
$Manifest.host.machine         = $env:COMPUTERNAME
$Manifest.host.user            = $env:USERNAME
$Manifest.host.psVersion       = $PSVersionTable.PSVersion.ToString()
$Manifest.host.os              = [System.Environment]::OSVersion.VersionString

Say-Head 'Stage 0 - validation (nothing is launched or contacted until this passes)'
$bad = @()

if ($RunSecs -lt 30 -or $RunSecs -gt 86400) { $bad += ('-RunSecs must be 30..86400 (got {0})' -f $RunSecs) }
if ($SampleSecs -le 0 -or $SampleSecs -gt 3600) { $bad += ('-SampleSecs must be 1..3600 (got {0})' -f $SampleSecs) }
foreach ($pair in @(
    @{n='-PreRollSecs';v=$PreRollSecs}, @{n='-AppJoinTimeoutSec';v=$AppJoinTimeoutSec},
    @{n='-OracleGateTimeoutSec';v=$OracleGateTimeoutSec}, @{n='-InitDispatchWaitSec';v=$InitDispatchWaitSec},
    @{n='-PushOrderListenSec';v=$PushOrderListenSec}, @{n='-TrailSecs';v=$TrailSecs},
    @{n='-LaunchSettleSec';v=$LaunchSettleSec}, @{n='-AppExitTimeoutSec';v=$AppExitTimeoutSec},
    @{n='-StopVrfTimeoutSec';v=$StopVrfTimeoutSec}, @{n='-CreateOneWatchSec';v=$CreateOneWatchSec},
    @{n='-StageTimeoutSec';v=$StageTimeoutSec}, @{n='-SettleHoldSecs';v=$SettleHoldSecs})) {
    if ($pair.v -lt 0 -or $pair.v -gt 86400) { $bad += ('{0} must be 0..86400 (got {1})' -f $pair.n, $pair.v) }
}
# A floor: the observers poll the stop file once a second and then resign; a grace
# under 10 s would flag a HEALTHY resign as still-running.
if ($TraceStopGraceSec -lt 10 -or $TraceStopGraceSec -gt 3600) {
    $bad += ('-TraceStopGraceSec must be 10..3600 (got {0})' -f $TraceStopGraceSec)
}
# A floor, not a style preference: LaunchVrf legitimately takes 35-60 s and StopVrf
# 6-10 s, so slack below a minute could time out a HEALTHY stage and abort a good
# run. Setting it to 0 would restore the "waits forever" failure this parameter
# exists to prevent.
if ($StageTimeoutSec -lt 60) {
    $bad += ('-StageTimeoutSec must be at least 60 (got {0}). It is SLACK added on top of each stage own blocking budget; a healthy LaunchVrf takes 35-60 s.' -f $StageTimeoutSec)
}
# Stage 7d. Negative is meaningless and a very large hold would silently eat the run: the
# observers' cap grows with it, but so does the wall time before a single task is issued.
if ($PreOrderSettleSecs -lt 0 -or $PreOrderSettleSecs -gt 3600) {
    $bad += ('-PreOrderSettleSecs must be 0..3600 (got {0}). 0 = no stage 7d hold.' -f $PreOrderSettleSecs)
}
# Stage 7d READY GATE. One gate exists; an unknown name is a TYPO and must not fall through
# to "off", which would run the very unGated push the operator asked to avoid.
$PreOrderGateOn = $false
if ($PreOrderGate -ne '') {
    if ($PreOrderGate -in @('NavArea','navarea','nav-area','navArea')) {
        $PreOrderGate   = 'NavArea'
        $PreOrderGateOn = $true
    } else {
        $bad += ("-PreOrderGate '{0}' is not a gate this runner knows. Supported: NavArea (or '' = off)." -f $PreOrderGate)
    }
}
if ($PreOrderGateOn -and ($PreOrderGateTimeoutSec -lt 30 -or $PreOrderGateTimeoutSec -gt 1800)) {
    $bad += ('-PreOrderGateTimeoutSec must be 30..1800 (got {0}). The cold nav-area wait measured 236.9 s, so 30 is already optimistic and the default 300 is the one with margin.' -f $PreOrderGateTimeoutSec)
}
# The WARM/COLD split for the placement -> area-row delta. 60 s sits an order of magnitude
# above the warm 9.1-12.1 s and a quarter of the cold 236.9 s, so nothing measured lands
# near it (G7B_G8_RESULTS_2026-09-14 sec 3b).
$PreOrderGateWarmSecs = 60
# Stage 8b Q5 probe. OFF unless one of the two offsets is positive; both must sit inside a
# plausible window, and a resume that is not AFTER its pause is refused outright - it would
# leave the scenario paused for the remainder of the run and destroy the evidence the window
# exists to collect.
$PauseProbeOn = ($PauseAtSec -gt 0 -or $ResumeAtSec -gt 0)
if ($PauseAtSec -lt 0 -or $PauseAtSec -gt 86400) {
    $bad += ('-PauseAtSec must be 0..86400 (got {0}). 0 = no stage 8b pause.' -f $PauseAtSec)
}
if ($ResumeAtSec -lt 0 -or $ResumeAtSec -gt 86400) {
    $bad += ('-ResumeAtSec must be 0..86400 (got {0}). 0 = no stage 8b resume.' -f $ResumeAtSec)
}
if ($PauseAtSec -gt 0 -and $ResumeAtSec -gt 0 -and $ResumeAtSec -le $PauseAtSec) {
    $bad += ('-ResumeAtSec ({0}) must be GREATER than -PauseAtSec ({1}). Both are offsets from the same instant (PushOrder returning), so a resume at or before the pause would run in the wrong order and leave the scenario PAUSED for the rest of the window.' -f $ResumeAtSec, $PauseAtSec)
}
# Stage 8b WS runaway abort (RUNBOOK 0.5.11 item 17 extension). 0 = off; anything positive is a
# COUNT of confirmed SampleThreads.ps1 alerts, so there is no upper window to bound it against.
$WsRunawayOn = ($WsRunawayAbortAfter -gt 0)
if ($WsRunawayAbortAfter -lt 0) {
    $bad += ("-WsRunawayAbortAfter must be >= 0 (got {0}). 0 = never abort (today's behaviour); a positive count is how many confirmed thread-samples.alerts.txt episodes, timestamped at or after the order reaching the bus, before the run is failed early." -f $WsRunawayAbortAfter)
}
# Stage 2h federation holder (STP-825). ARMED only on the 5.2 profile and only when the
# hold is positive, so a 5.0.2 run and a -FederationHoldSecs 0 run are byte-identical to
# what they were before this stage existed. The bounds mirror RtiProbe's own contract:
# settleSecs must be a POSITIVE int (tools/RtiProbe/Program.cs, ToolArgs.TryPositiveInt), so
# 0 can only mean "stage off", never "hold for zero seconds".
$FederationHoldOn = ($Is52 -and $FederationHoldSecs -gt 0)
if ($FederationHoldSecs -lt 0 -or $FederationHoldSecs -gt 86400) {
    $bad += ('-FederationHoldSecs must be 0..86400 (got {0}). 0 = no Stage 2h holder (pre-STP-825 behaviour); anything positive is the wall-clock hold RtiProbe is given as its settleSecs.' -f $FederationHoldSecs)
}
if ($FederationHoldAttempts -lt 1 -or $FederationHoldAttempts -gt 10) {
    $bad += ('-FederationHoldAttempts must be 1..10 (got {0}). Each attempt costs ONE ledgered appNumber, so the ceiling is deliberately low.' -f $FederationHoldAttempts)
}
# EXPLICITLY PASSED ONLY. The default is 900, so testing the VALUE here would have made
# every 5.0.2 run abort at validation on a parameter its operator never typed - the 5.0.2
# profile must stay byte-for-byte what it was. Same shape as the -NoGui / -DeviceAddress
# refusals above: the complaint is about the ARGUMENT, not about the default.
if ($PSBoundParameters.ContainsKey('FederationHoldSecs') -and $FederationHoldSecs -gt 0 -and -not $Is52) {
    $bad += ('-FederationHoldSecs {0} was passed on the 5.0.2 profile. Stage 2h is a 5.2-only repair (STP-825 is an rtiexec 5.0.1 CREATE failure under the 5.2 posture); on 5.0.2 nothing would run and the appNumbers would be burned for nothing. Pass -VrfProfile 5.2, or drop the argument.' -f $FederationHoldSecs)
}
if ($PSBoundParameters.ContainsKey('FederationHoldAttempts') -and -not $Is52) {
    $bad += '-FederationHoldAttempts is a 5.2 profile switch (Stage 2h, STP-825). On 5.0.2 the stage does not run; drop the argument or pass -VrfProfile 5.2.'
}
# How long Stage 2h waits for ONE attempt to show up as JOINED in the rtiexec log before it
# gives up on that attempt. Not a parameter: the holder either joins in the first seconds or
# its create was rejected (measured joins are sub-second; the seat's hand-run holders on
# 2026-09-15 joined immediately or crashed). 45 s is ~50x the observed join latency.
$FederationHoldJoinWaitSec = 45
# THE FEDERATION THE HOLDER MUST CREATE. It has to be the identity Stage 2c and the sim use,
# and on 5.2 that is the connection config's own execName - the tools are given NO federation
# argument and tools/Shared/StackIdentity.cs reads it from there. Read it out of the file this
# run is ACTUALLY using rather than restating the string: -VrfAppDataDir can relocate that
# file, and a holder on the wrong execName would create a SECOND federation and help nothing.
# The literal is the documented fallback for a config that cannot be read or parsed.
$FederationHoldName = ''
$FederationHoldNameSource = ''
if ($Is52) {
    $FederationHoldName       = 'MAK-ONE-2025'
    $FederationHoldNameSource = ('FALLBACK literal - execName could not be read from {0}' -f $ConnConfigFile)
    if (Test-Path -LiteralPath $ConnConfigFile -PathType Leaf) {
        try {
            $ccExec = [regex]::Match((Get-Content -LiteralPath $ConnConfigFile -Raw -Encoding UTF8),
                                     '<execName\s+value\s*=\s*"([^"]+)"')
            if ($ccExec.Success -and -not [string]::IsNullOrWhiteSpace($ccExec.Groups[1].Value)) {
                $FederationHoldName       = $ccExec.Groups[1].Value
                $FederationHoldNameSource = ('execName from {0}' -f $ConnConfigFile)
            }
        } catch {
            $FederationHoldNameSource = ('FALLBACK literal - {0} could not be read: {1}' -f $ConnConfigFile, $_.Exception.Message)
        }
    }
} else {
    $FederationHoldName       = $Federation
    $FederationHoldNameSource = '-Federation (5.0.2 profile - Stage 2h does not run there)'
}
# StopVrf.ps1 validates TimeoutSec 5..600 itself and exits 2 - catch it here so the
# failure lands BEFORE VR-Forces is launched instead of during teardown.
if ($StopVrfTimeoutSec -lt 5 -or $StopVrfTimeoutSec -gt 600) {
    $bad += ('-StopVrfTimeoutSec must be 5..600 - StopVrf.ps1 exits 2 outside that range (StopVrf.ps1:83-86). Got {0}.' -f $StopVrfTimeoutSec)
}
if ($PushOrderListenSec -gt 86400) { $bad += 'PushOrder accepts seconds-to-listen 0..86400 only.' }

foreach ($f in @(
    @{n='-Init';  p=$Init},
    @{n='-Order'; p=$Order})) {
    if (-not (Test-Path -LiteralPath $f.p -PathType Leaf)) { $bad += ('{0} file not found: {1}' -f $f.n, $f.p) }
}
foreach ($f in @(
    @{n='LaunchVrf.ps1';   p=$LaunchVrf},
    @{n='StopVrf.ps1';     p=$StopVrf},
    @{n='WatchVrf.exe';    p=$ExeWatchVrf},
    @{n='RtiProbe.exe';    p=$ExeRtiProbe},
    @{n='PushInit.exe';    p=$ExePushInit},
    @{n='PushOrder.exe';   p=$ExePushOrder},
    @{n='ListenReports.exe';p=$ExeListenReports},
    @{n='StopIface.exe';   p=$ExeStopIface},
    @{n='VrfC2SimApp.exe'; p=$ExeApp},
    @{n='Appendix B ledger'; p=$LedgerDoc})) {
    if (-not (Test-Path -LiteralPath $f.p -PathType Leaf)) { $bad += ('{0} not found: {1} (build Release, or fix the path)' -f $f.n, $f.p) }
}
# CreateOne is checked SOFTLY, on purpose. It is used ONLY by the stage-7b
# failure-path diagnostic, so a missing build must not block an otherwise healthy
# run - it only costs the disambiguation, which stage 7b then reports as
# INCONCLUSIVE. Every tool above is on the happy path and stays a hard failure.
$CreateOneAvailable = (Test-Path -LiteralPath $ExeCreateOne -PathType Leaf)
# PauseSim is checked HARD, and only when the probe is ARMED. The opposite of CreateOne's
# soft check on purpose: CreateOne is a failure-path diagnostic whose absence costs only the
# disambiguation, whereas an armed -PauseAtSec that silently does nothing would produce a run
# LABELLED as a pause probe with no pause in it - the worst of both (a false green, and an
# appNumber burned for nothing). Unarmed, the tool need not exist at all.
if ($PauseProbeOn -and -not (Test-Path -LiteralPath $ExePauseSim -PathType Leaf)) {
    $bad += ('-PauseAtSec/-ResumeAtSec are armed but PauseSim.exe is not at {0}. Build it: dotnet build tools\PauseSim\PauseSim.csproj -c Release -p:BridgeConfig={1} -t:Rebuild (RUNBOOK sec 9 - CHECK FOR THE OUTPUT TREE, not the exit code).' -f $ExePauseSim, $BridgeOut)
}

if (-not (Test-Path -LiteralPath $Bin64 -PathType Container)) {
    $bad += ('VR-Forces bin64 not found: {0} - it is the mandatory cwd for every HLA process (RUNBOOK sec 7 item 3)' -f $Bin64)
}

# ---- PROFILE integrity (nothing here can fire on the 5.0.2 default) ----------
# -VrfProfile is the ONLY stack selector. A hand-passed root/federation beside it would
# produce a MIXED environment, and the failure mode is silent: MAK DLLs bind by NAME on
# PATH, so a 5.2 binary under a 5.0.2 prefix loads 5.0.2 and reports it only in the app's
# NativeStackInfo line. Refuse instead, before anything is launched.
if ($Is52) {
    foreach ($p in @('VrfRoot','VrLinkRoot','RtiDir','Federation')) {
        if ($PSBoundParameters.ContainsKey($p)) {
            $bad += ('-{0} was passed together with -VrfProfile 5.2. The profile DERIVES it; passing both is how a half-5.2 environment is built. Drop -{0}.' -f $p)
        }
    }
    foreach ($f in @(
        @{n='rtiexec rid (RTI_RID_FILE, shared by EVERY federate)';  p=$RidFile},
        @{n='connection config (federation identity, DIFF row A2)';  p=$ConnConfigFile},
        @{n='Stage 2r headless-rtiexec script';                      p=$StartRtiExec})) {
        if (-not (Test-Path -LiteralPath $f.p -PathType Leaf)) { $bad += ('{0} not found: {1}' -f $f.n, $f.p) }
    }
    # A 4.6.1 RTI in the 5.2 profile is the version gate that rejected every federate on
    # 2026-09-03. The default is 5.0.1 and -RtiDir is refused above, so this can only fire
    # if the derivation itself is edited - which is exactly when it must.
    if ($RtiDir -notmatch '5\.0\.1') {
        $bad += ('the 5.2 profile RTI is {0}. The documented posture is MAK RTI 5.0.1 in rtiexec mode (UG52 5.5.1 p190, PREREG_52_RTIEXEC_2026-09-04); a mixed RTI version rejects every federate.' -f $RtiDir)
    }
    # -DeviceAddress is a tunable, but a MALFORMED one would be handed to the sim's command
    # line and to the app's config; empty is legal and means "pass nothing" (the discriminator).
    if ((-not [string]::IsNullOrWhiteSpace($DeviceAddress)) -and ($DeviceAddress -notmatch '^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$')) {
        $bad += ("-DeviceAddress must be a dotted IPv4 address or EMPTY (empty = pass nothing, VR-Forces picks the first device listed, IOG 5.2.1 p81); got '{0}'." -f $DeviceAddress)
    }
    # -VrfAppDataDir is OPTIONAL (empty = pass nothing, the vendor appData), but a non-empty
    # value that is not a directory is a typo the sim would find only after the launch had
    # begun. LaunchVrf52 re-checks it and exits 2; this refuses it BEFORE anything starts, so
    # a -DryRun catches the mistake too. Pass the directory that CONTAINS settings\ (i.e.
    # ...\vrf-appdata\appData, not its parent) - LaunchVrf52 warns about that specific slip.
    if ((-not [string]::IsNullOrWhiteSpace($VrfAppDataDir)) -and -not (Test-Path -LiteralPath $VrfAppDataDir -PathType Container)) {
        $bad += ("-VrfAppDataDir must be an EXISTING directory or EMPTY (empty = pass nothing; VR-Forces then uses its own appData, UG52 Table 11 p178); got '{0}'." -f $VrfAppDataDir)
    }
} else {
    if ($NoGui) {
        $bad += '-NoGui is a 5.2 profile switch (LaunchVrf52.ps1 -NoGui). The 5.0.2 combined-mode launcher has no headless option; use -VrfProfile 5.2 or drop -NoGui.'
    }
    if ($PSBoundParameters.ContainsKey('DeviceAddress')) {
        $bad += '-DeviceAddress is a 5.2 profile switch (LaunchVrf52.ps1 -DeviceAddress). On 5.0.2 the interface address comes from the saved Launcher connection profile; use -VrfProfile 5.2 or drop -DeviceAddress.'
    }
    if ($VrfAppDataDir -and -not $Is52) {
        $bad += '-VrfAppDataDir is a 5.2 profile switch (LaunchVrf52.ps1 -AppDataDir). The 5.0.2 combined-mode launcher takes appData from the installation; use -VrfProfile 5.2 or drop -VrfAppDataDir.'
    }
}

# LIMITATION 1: the server must be on this host, and a deployed ListenReports that
# predates --rest-url/--stomp-url hears ONLY 127.0.0.1:8080/61613. Refuse rather
# than capture nothing against a server the observer is not listening to. (The
# capability itself is probed in Stage 0b; that check is applied right after it.)
foreach ($u in @(@{n='-RestUrl';v=$RestUrl}, @{n='-StompUrl';v=$StompUrl})) {
    if ($u.v -notmatch '(?i)://(127\.0\.0\.1|localhost|\[::1\])[:/]') {
        $bad += ("{0}='{1}' is not localhost. Every stage assumes the C2SIM server runs on this host (RUNBOOK sec 1). Refusing." -f $u.n, $u.v)
    }
}

# LIMITATION 6: clientId must equal the init's SystemName (RUNBOOK sec 2).
$initSystemNames = @()
$appClientId = $null
if (Test-Path -LiteralPath $Init -PathType Leaf) {
    try {
        $initText = Get-Content -LiteralPath $Init -Raw -Encoding UTF8
        $initSystemNames = @([regex]::Matches($initText, '<SystemName>([^<]*)</SystemName>') |
                             ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
    } catch { $bad += ('could not read -Init to check SystemName: {0}' -f $_.Exception.Message) }
}
$appSettings = Join-Path (Split-Path -Parent $ExeApp) 'appsettings.json'
# A MISSING appsettings.json is FATAL, not a warning (2026-09-15, run 20260915T122145Z).
#
# That run reached the observation window with no units in it. The app joined, saw the back end,
# and then died on its late-join QUERYINIT with
#     C2SimClientLib.C2SIMClientException: Error - Submitter not specified
# which C2SIMClientRestLib.cs:190-194 throws BEFORE it builds a URL - so nothing ever reached the
# server, and no amount of restarting the container could have helped. The submitter comes from
# C2SIM:SubmitterId in appsettings.json ONLY: there is no Vrf__/C2SIM__ override for it in this
# runner or in RunScenario.sh, and no C# default. The app's deployed content root had lost both
# appsettings.json and appsettings.Demo.json (a Clean that ran while the previous run still held
# the exe: the locked .exe survived, the unlocked .json files did not), and EVERY other setting
# the run depends on is injected as an environment variable - so the app started, logged a
# normal-looking banner, joined, and only failed three stages later.
#
# The block below already opened this file, but only WARNED when it could not - and -ClientId
# masked even that, because it assigns $appClientId itself. Fail here instead: Stage 0 has
# launched nothing, contacted no server and burned no appNumber.
if (-not (Test-Path -LiteralPath $appSettings -PathType Leaf)) {
    $bad += ("appsettings.json is MISSING from the app's content root ({0}). The interface reads " +
             "C2SIM:SubmitterId from it and from NOWHERE else - no environment override exists - so " +
             "its QUERYINIT would throw 'Error - Submitter not specified' and the init would never be " +
             "dispatched (run 20260915T122145Z). Rebuild the app: dotnet build " +
             "src\VrfC2SimApp\VrfC2SimApp.csproj -c Release -p:BridgeConfig={1} - and never Clean it " +
             "while a run still holds the exe.") -f $appSettings, $BridgeOut
}
# Kept after the parse: the -PreOrderGate console-level check below reads the same object,
# so the file is opened once and the two checks can never disagree about its contents.
$cfgApp = $null
if (Test-Path -LiteralPath $appSettings -PathType Leaf) {
    try {
        $cfg = Get-Content -LiteralPath $appSettings -Raw -Encoding UTF8 | ConvertFrom-Json
        $cfgApp = $cfg
        if ($cfg.PSObject.Properties.Name -contains 'Vrf' -and
            $cfg.Vrf.PSObject.Properties.Name -contains 'ClientId') { $appClientId = [string]$cfg.Vrf.ClientId }
    } catch { Say-Warn ('could not parse {0}: {1}' -f $appSettings, $_.Exception.Message) }
}
if ($ClientId) {
    # -ClientId wins over appsettings: the app reads Vrf__ClientId from its environment (the standard
    # env-override mechanism every other Vrf__ setting uses). Exported here so the app inherits it.
    $appClientId = $ClientId
    $env:Vrf__ClientId = $ClientId
}
if ($appClientId -and $initSystemNames.Count -gt 0 -and ($initSystemNames -notcontains $appClientId)) {
    $bad += ("clientId MISMATCH: appsettings Vrf:ClientId='{0}' but the init declares SystemName [{1}]. RUNBOOK sec 2: they MUST match or the interface creates 0 UNITS. Fix appsettings.json (or the init) before running." -f $appClientId, ($initSystemNames -join ','))
}
if (-not $appClientId) { Say-Warn 'could not read Vrf:ClientId from the app appsettings.json - the SystemName match is UNVERIFIED.' }

# THE LATERAL ROUTE SHIFT, IN THE EVIDENCE (user ruling 2026-09-20: "Route shift: ON. Use as
# default for any run."; STP-804/806, RUNBOOK sec 12). It CHANGES WHERE UNITS DRIVE, so a run
# that cannot say which line it drove is not evidence of anything - the manifest records the
# value the app will really use, resolved the way the app resolves it:
#   Vrf__PreflightRouteShift in this shell (inherited by the child) beats the json file, the
#   deployed appsettings.json beats the C# initialiser, and the initialiser is TRUE.
# The runner sets nothing here: turning it off is the operator's env var, not a runner flag, so
# there is exactly one switch to find. The app's own log line is the confirmation; this is the
# PREDICTION, and the two disagreeing is itself a finding.
$RouteShiftEnv = [Environment]::GetEnvironmentVariable('Vrf__PreflightRouteShift')
$RouteShiftJson = $null
if ($cfgApp -and ($cfgApp.PSObject.Properties.Name -contains 'Vrf') -and
    ($cfgApp.Vrf.PSObject.Properties.Name -contains 'PreflightRouteShift')) {
    $RouteShiftJson = [bool]$cfgApp.Vrf.PreflightRouteShift
}
# The .NET configuration binder accepts ONLY true/false for a bool (case-insensitive) and
# THROWS on anything else, so '1' and 'yes' are not off-switches and must not be reported as
# though they were - an unparseable value is called out here rather than guessed at.
# An UNPARSEABLE env value is not "fall back to the file": the binder throws and the app never
# starts, so the manifest says THAT rather than naming a value the run will never reach.
if     ($RouteShiftEnv -match '^true$')  { $RouteShiftEff = $true;  $RouteShiftSrc = 'env Vrf__PreflightRouteShift=true' }
elseif ($RouteShiftEnv -match '^false$') { $RouteShiftEff = $false; $RouteShiftSrc = 'env Vrf__PreflightRouteShift=false' }
elseif ($RouteShiftEnv)                  { $RouteShiftEff = $null;  $RouteShiftSrc = ("env Vrf__PreflightRouteShift='{0}' is UNPARSEABLE - the app will not start" -f $RouteShiftEnv) }
elseif ($null -ne $RouteShiftJson)       { $RouteShiftEff = $RouteShiftJson; $RouteShiftSrc = 'appsettings.json Vrf:PreflightRouteShift' }
else                                     { $RouteShiftEff = $true;  $RouteShiftSrc = 'VrfSettings.cs initialiser (the key is in NEITHER the environment NOR the deployed appsettings.json)' }
$Manifest.inputs.routeShift = [ordered]@{
    effective   = $(if ($null -eq $RouteShiftEff) { 'UNKNOWN - unparseable Vrf__PreflightRouteShift' } else { [bool]$RouteShiftEff })
    source      = $RouteShiftSrc
    envValue    = $(if ($RouteShiftEnv) { $RouteShiftEnv } else { '(unset)' })
    appSettings = $(if ($null -ne $RouteShiftJson) { $RouteShiftJson } else { '(key absent)' })
    note        = 'Vrf:PreflightRouteShift. ON detours a FLAGGED leg laterally before dispatch and defers that dispatch up to Vrf:PreflightRouteShiftTimeoutSeconds; it never refuses a task - on a timeout, a throw, an empty tile cache or no cleared line the AUTHORED line is dispatched. The route the unit was GIVEN (shifted or not) is what STP-837 measures its arrival bar from.'
}
if ($null -eq $RouteShiftEff) {
    Say-Warn ("Vrf__PreflightRouteShift='{0}' is NOT a value the .NET configuration binder accepts for a bool (only true/false, case-insensitive): the app will THROW binding its Vrf section and this run will have no interface at all. Set true or false, or unset it." -f $RouteShiftEnv)
} elseif ($RouteShiftEff) {
    Say-Ok ('route shift is ON for this run ({0}) - a flagged leg may be DETOURED before dispatch; the app logs every shift and every decline' -f $RouteShiftSrc)
} else {
    Say-Warn ('route shift is OFF for this run ({0}) - flagged legs are dispatched on the authored line' -f $RouteShiftSrc)
}

# -PreOrderGate NavArea REQUIRES the object consoles open. The row it waits for is printed
# at object-console level 3 and at no lower level, so with the console below 3 the gate can
# only ever time out - after burning its whole timeout with VR-Forces up. That is a stage-0
# failure, before anything is launched, not a run-time surprise.
# The level reaches the app the way every other Vrf setting does: the environment
# (Vrf__ObjectConsoleNotifyLevel, which scripts\RunScenario.sh exports from --object-console)
# overrides appsettings.json, where the shipped default is -1 = consoles OFF.
$ObjectConsoleLevel       = $null
$ObjectConsoleLevelSource = '(unresolved)'
if (-not [string]::IsNullOrWhiteSpace($env:Vrf__ObjectConsoleNotifyLevel)) {
    $ocl = 0
    if ([int]::TryParse($env:Vrf__ObjectConsoleNotifyLevel.Trim(), [ref]$ocl)) {
        $ObjectConsoleLevel       = $ocl
        $ObjectConsoleLevelSource = 'env Vrf__ObjectConsoleNotifyLevel (RunScenario.sh --object-console)'
    }
}
if ($null -eq $ObjectConsoleLevel -and $null -ne $cfgApp -and
    $cfgApp.PSObject.Properties.Name -contains 'Vrf' -and
    $cfgApp.Vrf.PSObject.Properties.Name -contains 'ObjectConsoleNotifyLevel') {
    $ObjectConsoleLevel       = [int]$cfgApp.Vrf.ObjectConsoleNotifyLevel
    $ObjectConsoleLevelSource = ('appsettings.json ({0})' -f $appSettings)
}
if ($PreOrderGateOn) {
    if ($null -eq $ObjectConsoleLevel) {
        $bad += ("-PreOrderGate {0} needs the object-console level and it could NOT be resolved (neither env Vrf__ObjectConsoleNotifyLevel nor Vrf:ObjectConsoleNotifyLevel in {1}). The gate waits for a row that only prints at level >= 3; refusing rather than waiting out {2}s for a row that may never come." -f $PreOrderGate, $appSettings, $PreOrderGateTimeoutSec)
    } elseif ($ObjectConsoleLevel -lt 3) {
        $bad += ("-PreOrderGate {0} REQUIRES object console >= 3 and it is {1} (from {2}). The 'New Primary nav area' row the gate waits for is printed at level 3; below that it is never emitted and the gate could only time out. Pass --object-console 3 (or 4) to scripts\RunScenario.sh, or drop the gate and use -PreOrderSettleSecs." -f $PreOrderGate, $ObjectConsoleLevel, $ObjectConsoleLevelSource)
    }
}

if ($bad.Count -gt 0) {
    Say-Head 'Result'
    foreach ($b in $bad) { Say-Fail $b }
    Say-Fail 'Aborting at validation. NOTHING was launched and NO server was contacted.'
    exit 2
}
Say-Ok 'inputs, tools, timing budgets and the clientId/SystemName match all validate'
# 5.2 ONLY. Guarded so the 5.0.2 profile's output stays byte-for-byte what it was.
if ($Is52) {
    Say-Ok ('VrfProfile 5.2 - VR-Forces {0}, VR-Link {1}, RTI {2}' -f $VrfRoot, $VrLinkRoot, $RtiDir)
    Say     ('         binaries    : bin\{0}\ (bridge-linked tools + app); PushInit/PushOrder/ListenReports/StopIface are managed-only and shared with 5.0.2' -f $BridgeOut)
    Say     ('         launch/stop : {0} / {1}{2}' -f (Split-Path -Leaf $LaunchVrf), (Split-Path -Leaf $StopVrf), $(if ($NoGui) { '  (-NoGui: no vrfGui)' } else { '  (GUI ON - migration observability)' }))
    Say     ('         federation  : NO argument passed - identity from {0} (execName MAK-ONE-2025)' -f $ConnConfigFile)
    Say     ('         RTI posture : RTIEXEC MODE on MAK RTI 5.0.1 - the DOCUMENTED posture and the only one offered')
    Say     ('                       (UG52 5.5.1 p190 "You cannot use the MAK RTI in lightweight mode with VR-Forces";')
    Say     ('                        PREREG_52_RTIEXEC_2026-09-04: rtiexec mode reflects 62 entities, lightweight 0)')
    Say     ('         rid (SHARED by every federate): {0}' -f $RidFile)
    Say     ('         rtiexec     : Stage 2r ensures one is up on TCP 4001 ({0}); NEVER killed or restarted, persists across runs' -f (Split-Path -Leaf $StartRtiExec))
    Say     ('         interface   : -DeviceAddress {0}' -f $(if ($DeviceAddressPassed) { ('{0} - pinned on sim + gui and on the app' -f $DeviceAddress52) } else { 'EMPTY (the default): NOT passed to the sim or the gui; VR-Forces picks the first device listed (IOG 5.2.1 p81)' }))
    Say     ('                       bridge federates (tools + app) use {0}' -f $BridgeDeviceAddress)
    Say     ('                       NOT part of the repair: run 3857 reflected 54-56 entities with the observer device address blank (PREREG_52_RTIEXEC sec 4 P4). Sim-side necessity still OPEN.')
    Say     ('         back-end log: --logFileName is NOT passed (PREREG_52_CRASH_BISECT_2026-09-04 sec 5: 6 startup crashes / 18 launches with it,')
    Say     ('                       0 / 12 without, p = 0.031; a short vendor-default path crashed too, so it is the OPTION). LaunchVrf52')
    Say     ('                       HARVESTS the vendor''s own C:\MAK\logs\vrfSim*-<pid>.log for that pid into runs\launch52 instead.')
    Say     ('                       SECRETS: the harvested copy holds the FULL PROCESS ENVIRONMENT IN CLEARTEXT (FORENSICS_52_STARTUP_CRASH')
    Say     ('                       _2026-09-04 sec 10) - NEVER attach it to a ticket, mail or issue; send the .callstack.log / .dmp instead.')
    # appData. PRINTED ONLY WHEN RELOCATED, so a default run's banner is byte-identical to
    # every run in the record; the absent case is still ledgered (inputs.vrfAppDataDir).
    if ($VrfAppDataDir) {
        Say ('         appData     : {0}' -f $VrfAppDataDir)
        Say ('                       -VrfAppDataDir -> LaunchVrf52 -AppDataDir -> --appDataDir on the sim AND the gui (UG52 Table 11 p178 /')
        Say ('                       Table 10 p164). That tree''s ONE delta from the vendor copy is loadAllNavigationDataOnTerrainLoad 1:')
        Say ('                       nav data loads WITH the scenario instead of lazily at first entity placement (UG52 App. C p1671).')
        Say ('                       LaunchVrf52 re-validates the path and echoes the setting it actually read into runs\launch52.')
    }
    Say     ('         scenario    : {0} (relative to {1}\userData\scenarios)' -f $Scenario, $VrfRoot)
    foreach ($k in $ProfileEnv.Keys) { Say ('         env         : {0}={1}' -f $k, $ProfileEnv[$k]) }
}
Say-Ok ('init SystemName [{0}] matches app clientId [{1}]' -f ($initSystemNames -join ','), $appClientId)
if ($CreateOneAvailable) {
    Say-Ok ('CreateOne.exe present - the stage-7b failure-path disambiguation diagnostic is ARMED: {0}' -f $ExeCreateOne)
} else {
    Say-Warn ('CreateOne.exe NOT found at {0}. This is NOT fatal - it is only used by the stage-7b' -f $ExeCreateOne)
    Say-Warn '  failure-path diagnostic. If the oracle gate fails, the run will not be able to tell'
    Say-Warn '  ORACLE_BLIND from CREATION_LAYER_SUSPECT and will record INCONCLUSIVE. Build Release to arm it.'
}

$Manifest.inputs.init          = (Resolve-Path -LiteralPath $Init).Path
$Manifest.inputs.order         = (Resolve-Path -LiteralPath $Order).Path
$Manifest.inputs.runSecs       = $RunSecs
$Manifest.inputs.sampleSecs    = $SampleSecs
$Manifest.inputs.scenario      = $Scenario
$Manifest.inputs.quietBackend  = [bool]$QuietBackend
$Manifest.inputs.backendNotifyLevel = $BackendNotifyLevel
$Manifest.inputs.vrfAppDataDir = $(if ($Is52 -and $VrfAppDataDir) { $VrfAppDataDir } elseif ($Is52) { '(not passed - vendor appData)' } else { $null })
$Manifest.inputs.clientId      = $(if ($ClientId) { $ClientId } else { ('(appsettings) {0}' -f $appClientId) })
$Manifest.inputs.typeMapFile   = $(if ($Is52) { $TypeMapFile52 } else { '(5.0.2 profile: appsettings)' })
$Manifest.inputs.typeMapIsRepoMap = [bool](-not $TypeMapFile)
$Manifest.inputs.federation    = $Federation
# THE PROFILE, in the evidence. A trace can only be compared with another trace from the
# SAME stack, so which stack ran is a first-class manifest field - roots, binaries, the
# rid every federate had to share (hashed: a DIFFERENT rid is a different connection and
# the federates would not see each other), the assistant state and the connection config
# that supplied the federation identity. nativeStack is filled in at stage 6c from the
# app's own "VrfBridge native stack = ..." line - the RUNTIME fact, not this switch.
$Manifest.inputs.vrfProfile = [ordered]@{
    profile             = $VrfProfile
    vrfRoot             = $VrfRoot
    vrLinkRoot          = $VrLinkRoot
    rtiDir              = $RtiDir
    bridgeConfig        = $BridgeOut
    launchScript        = $LaunchVrf
    stopScript          = $StopVrf
    noGui               = [bool]$NoGui
    federationArgument  = $(if ($Is52) { '(none - config-file identity)' } else { $Federation })
    connectionConfigFile= $(if ($Is52) { $ConnConfigFile } else { $null })
    ridFile             = $(if ($Is52) { $RidFile } else { $null })
    ridSha256           = $(if ($Is52 -and (Test-Path -LiteralPath $RidFile -PathType Leaf)) { (Get-FileHash -LiteralPath $RidFile -Algorithm SHA256).Hash } else { $null })
    rtiAssistantDisable = $(if ($Is52) { '1' } else { $null })
    typeMapFile         = $(if ($Is52) { $TypeMapFile52 } else { $null })
    # THE CONNECTION SETTINGS, in the evidence. connectionMode is the repair (rtiexec, not
    # lightweight). The interface fields are recorded BECAUSE they are not the repair: run
    # 3857 showed an observer with no device address still reflects, so a later reader must
    # be able to see exactly what this run passed (usually nothing) and what the bridge
    # federates therefore used (VrfFacade's own default). rtiExec.* is filled in by Stage 2r
    # with the pids of the rtiexec and the rtiForwarder it started - NOT this run's children,
    # and they outlive it.
    connectionMode      = $(if ($Is52) { 'rtiexec (UG52 5.5.1 p190 - lightweight is NOT supported with VR-Forces)' } else { 'assistant-chosen (5.0.2 golden path: rtiexec loopback, 4.6.1)' })
    deviceAddressPassed = $(if ($Is52) { [bool]$DeviceAddressPassed } else { $null })
    deviceAddress       = $(if ($Is52 -and $DeviceAddressPassed) { $DeviceAddress52 } elseif ($Is52) { '(not passed - sim/gui launched without --deviceAddress)' } else { $null })
    bridgeDeviceAddress = $(if ($Is52) { $BridgeDeviceAddress } else { $null })
    deviceAddressStatus = $(if ($Is52) { 'NOT part of the observation-channel repair: run 3857 (PREREG_52_RTIEXEC sec 4 P4) reflected 54-56 entities with the observer device address blank. Sim-side necessity still open.' } else { $null })
    rtiExec             = $(if ($Is52) { [ordered]@{ script = $StartRtiExec; logDir = $RtiExecLogDir; logFile = $null; exitCode = $null; rtiExecPid = $null; forwarderPid = $null; started = $null; tcpPort = 4001 } } else { $null })
    # STAGE 2h's HOLDER (STP-825), filled in by the stage. armed=false means the stage was
    # off (-FederationHoldSecs 0) and the SIM created the federation itself - which is the
    # failure mode, so a later reader must be able to tell the two runs apart at a glance.
    # processId names a federate that OUTLIVES this run on purpose; it is never killed.
    federationHolder    = $(if ($Is52) { [ordered]@{ armed = [bool]$FederationHoldOn; federation = $FederationHoldName; federationSource = $FederationHoldNameSource; holdSecs = $FederationHoldSecs; maxAttempts = $FederationHoldAttempts; joinWaitSec = $FederationHoldJoinWaitSec; appNumbers = @(); appNumber = $null; processId = $null; attemptsUsed = 0; joinedUtc = $null; joinWaitedSec = $null; rtiExecLog = $null; joinEvidence = $null; attempts = @() } } else { $null })
    # THE BACK-END LOG, and why it is a COPY. --logFileName is NOT passed on the 5.2 profile:
    # PREREG_52_CRASH_BISECT_2026-09-04 sec 5 measured 6 startup crashes / 18 launches with the
    # option against 0 / 12 without (Fisher's exact, one-sided, p = 0.031), and a 22-character
    # path inside the vendor's own log directory crashed too, so it is the OPTION, not the path.
    # The sim writes its own log regardless; LaunchVrf52 harvests THAT file for its pid into
    # runs\launch52 and prints one marker line, parsed at Stage 3 into harvestedFrom/To.
    # SECRETS: the harvested copy carries the full process environment in cleartext
    # (FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10) - never attach it to a ticket or mail.
    vendorLog           = $(if ($Is52) { [ordered]@{
            logFileNamePassed = $false
            reason            = 'NOT passed - PREREG_52_CRASH_BISECT_2026-09-04 sec 5: 6 startup crashes / 18 launches with --logFileName, 0 / 12 without (p = 0.031); the OPTION, not the path.'
            harvestedFrom     = $null
            harvestedTo       = $null
            harvestOccasion   = $null
            secrets           = 'The harvested copy contains the FULL PROCESS ENVIRONMENT IN CLEARTEXT (DtPrintEnvironmentVariables at notifyLevel 3, FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10). NEVER attach it to a ticket, mail or issue - send the .callstack.log / .dmp instead. Not scrubbed, by decision.'
        } } else { $null })
    env                 = $ProfileEnv
    nativeStack         = $null
}
$Manifest.inputs.restUrl       = $RestUrl
$Manifest.inputs.stompUrl      = $StompUrl
$Manifest.inputs.clientId      = $appClientId
$Manifest.inputs.initSystemName= ($initSystemNames -join ',')

# ---- tool identities --------------------------------------------------------
function Get-ToolIdentity {
    param([string]$Path)
    $o = [ordered]@{ path = $Path; exists = $false }
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        $fi = Get-Item -LiteralPath $Path
        $o.exists         = $true
        $o.sizeBytes      = $fi.Length
        $o.lastWriteUtc   = $fi.LastWriteTimeUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
        try {
            $vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($Path)
            if ($vi.FileVersion)    { $o.fileVersion    = $vi.FileVersion }
            if ($vi.ProductVersion) { $o.productVersion = $vi.ProductVersion }
        } catch { }
        try { $o.sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash } catch { }
    }
    return $o
}
foreach ($t in @(
    @{k='LaunchVrf';     p=$LaunchVrf},     @{k='StopVrf';   p=$StopVrf},
    @{k='WatchVrf';      p=$ExeWatchVrf},   @{k='PushInit';  p=$ExePushInit},
    @{k='PushOrder';     p=$ExePushOrder},  @{k='StopIface'; p=$ExeStopIface},
    @{k='ListenReports'; p=$ExeListenReports}, @{k='VrfC2SimApp'; p=$ExeApp},
    @{k='CreateOne';     p=$ExeCreateOne},     @{k='RtiProbe';   p=$ExeRtiProbe},
    @{k='RunC2SimScenario'; p=$PSCommandPath})) {
    $Manifest.tools[$t.k] = Get-ToolIdentity -Path $t.p
}
# 5.2 ONLY, so the 5.0.2 manifest keeps exactly the tool set it always had.
if ($Is52) { $Manifest.tools['StartRtiExec'] = Get-ToolIdentity -Path $StartRtiExec }
# ARMED RUNS ONLY, so an unarmed run's manifest keeps exactly the tool set it always had.
if ($PauseProbeOn) { $Manifest.tools['PauseSim'] = Get-ToolIdentity -Path $ExePauseSim }
try {
    $gitHead = & git -C $RepoRoot rev-parse HEAD 2>$null
    $gitBranch = & git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null
    $Manifest.host.gitCommit = ("$gitHead").Trim()
    $Manifest.host.gitBranch = ("$gitBranch").Trim()
} catch { $Manifest.host.gitCommit = '(unavailable)' }

# =============================================================================
# STAGE 0b - OBSERVER CAPABILITY PROBE (offline, read-only; runs in -DryRun too)
# =============================================================================
# `<tool>.exe --capabilities` is pure managed: WatchVrf's Program.cs dispatches it
# before any type that references VrfBridge is JITted, ListenReports' before the
# C2SIM SDK is touched. No federation, no server, no MAK DLL. A tool that predates
# the flag rejects it (exit 2 via ToolArgs.UnknownFlags) or, for WatchVrf without
# the MAK PATH, fails to load its bridge - both are "not exit 0" = NOT supported,
# and the runner then passes NO --stop-file and keeps the pre-turnaround wait.
# THIS IS THE -ConsoleLogDir LESSON APPLIED: a flag the deployed binary lacks
# kills the oracle stage with exit 2 AFTER a full launch cycle. Probe first.
# On timeout the probe process is recorded and LEFT ALONE (this runner kills
# nothing), and the capability is treated as unsupported.
function Invoke-CapabilityProbe {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][string]$File, [string]$Cwd, [int]$TimeoutSec = 30)
    $o = [ordered]@{ tool = $Name; exitCode = $null; outcome = 'not-run'; lines = @(); supportsStopFile = $false; supportsReportBackends = $false }
    if (-not (Test-Path -LiteralPath $File -PathType Leaf)) { $o.outcome = 'binary-missing'; return $o }
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $File
    $psi.ArgumentList.Add('--capabilities')
    $psi.WorkingDirectory = $Cwd
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    # Same PATH the live tools get, so the probe answers for the binary AS IT WILL RUN.
    $psi.Environment['PATH'] = ('{0};{1}' -f $PathPrefixForProbe, $env:PATH)
    # ...and the same profile environment. On 5.2 this is not cosmetic: a 5.2 tool run
    # under the default environment dies with FileLoadException BEFORE it can print its
    # usage or capabilities (PREREG_52_TOOLJOIN_2026-09-03.md sec 2 - the exit-2 usage
    # contract holds ONLY under the 5.2 PATH), so an unprofiled probe would report
    # "not supported" for a binary that supports the flag. Empty on 5.0.2.
    foreach ($k in $ProfileEnv.Keys) { $psi.Environment[$k] = [string]$ProfileEnv[$k] }
    try {
        $p = [System.Diagnostics.Process]::Start($psi)
        $stdoutTask = $p.StandardOutput.ReadToEndAsync()
        $stderrTask = $p.StandardError.ReadToEndAsync()
        if ($p.WaitForExit($TimeoutSec * 1000)) {
            $o.exitCode = $p.ExitCode
            $o.outcome  = 'ran'
            $o.lines    = @(($stdoutTask.GetAwaiter().GetResult() -split "`r?`n") | Where-Object { $_ -ne '' })
        } else {
            $o.outcome = 'timed-out'
            Say-Warn ('{0} --capabilities (pid {1}) did not exit within {2}s. NOT killed. Treated as unsupported.' -f $Name, $p.Id, $TimeoutSec)
        }
    } catch {
        $o.outcome = 'could-not-start'
        $o.error   = $_.Exception.Message
    }
    $o.supportsStopFile  = Test-ToolCapability -ProbeLines $o.lines -ExitCode $o.exitCode -Capability 'stop-file'
    $o.supportsEndpoints = Test-ToolCapability -ProbeLines $o.lines -ExitCode $o.exitCode -Capability 'endpoints'
    # V6c arm A0 (2026-09-15). Until today NO run in the record said when a VR-Forces back end's
    # STATUS reaches an observer: this runner never passed --report-backends, so no trace carried
    # a backends= column - and the one surviving explanation of the V6/V6b join-gate failure is
    # exactly that cadence (docs/experiments/PREREG_V6C_LATE_JOINER_2026-09-15.md).
    $o.supportsReportBackends = Test-ToolCapability -ProbeLines $o.lines -ExitCode $o.exitCode -Capability 'report-backends'
    return $o
}
Say-Head 'Stage 0b - observer capability probe (offline, read-only): can the deployed observers be told to stop?'
$PathPrefixForProbe = ('{0};{1};{2}' -f $Bin64, (Join-Path $VrLinkRoot 'bin64'), (Join-Path $RtiDir 'bin'))
$ProbeWatch  = Invoke-CapabilityProbe -Name 'WatchVrf'      -File $ExeWatchVrf      -Cwd $Bin64
$ProbeListen = Invoke-CapabilityProbe -Name 'ListenReports' -File $ExeListenReports -Cwd $RepoRoot
foreach ($pr in @($ProbeWatch, $ProbeListen)) {
    if ($pr.supportsStopFile) {
        Say-Ok ('{0}: --capabilities exit {1}: {2} -> stop-file SUPPORTED' -f $pr.tool, $pr.exitCode, ($pr.lines -join ','))
    } else {
        Say-Warn ('{0}: --capabilities outcome={1} exit={2} -> stop-file NOT supported; it will run to its full duration cap (pre-turnaround behaviour)' -f $pr.tool, $pr.outcome, $pr.exitCode)
    }
}
# LIMITATION 1 (second half): without the 'endpoints' capability the deployed
# ListenReports hears ONLY the historical 8080/61613 server. Refuse a silently-empty
# capture; nothing has been launched or contacted at this point.
$ListenHistoricalRest  = 'http://127.0.0.1:8080/C2SIMServer'
$ListenHistoricalStomp = 'http://127.0.0.1:61613/topic/C2SIM'
if ($ProbeListen.supportsEndpoints) {
    Say-Ok ('ListenReports: endpoints SUPPORTED -> will listen on rest={0} stomp={1}' -f $RestUrl, $StompUrl)
} elseif ($RestUrl -ne $ListenHistoricalRest -or $StompUrl -ne $ListenHistoricalStomp) {
    Say-Fail ('ListenReports: --capabilities lacks ''endpoints'' (outcome={0} exit={1}); the deployed binary hears ONLY {2} / {3}, but this run targets {4} / {5}. The report capture would be SILENTLY EMPTY. Rebuild/redeploy tools/ListenReports, or pass the historical endpoints. Aborting - NOTHING was launched and NO server was contacted.' -f $ProbeListen.outcome, $ProbeListen.exitCode, $ListenHistoricalRest, $ListenHistoricalStomp, $RestUrl, $StompUrl)
    exit 2
} else {
    Say-Warn 'ListenReports: endpoints NOT supported by the deployed binary; the historical 8080/61613 endpoints match this run, so it still hears the right server.'
}
if ($ProbeWatch.supportsReportBackends) {
    Say-Ok 'WatchVrf: report-backends SUPPORTED -> every ''# t='' sample line will carry backends=<n>, the back-end status cadence instrument (V6c arm A0).'
} else {
    Say-Warn 'WatchVrf: report-backends NOT advertised by the deployed binary - the trace will carry NO backends= column, and the back-end status cadence stays UNMEASURED (V6c arm A0 cannot be read from this run). Rebuild/redeploy tools/WatchVrf.'
}
$TraceStopMode = if ($ProbeWatch.supportsStopFile -and $ProbeListen.supportsStopFile) { 'stop-file' }
                 elseif ($ProbeWatch.supportsStopFile -or $ProbeListen.supportsStopFile) { 'partial' }
                 else { 'duration-only' }
$Manifest.inputs.traceStop = [ordered]@{
    mode         = $TraceStopMode
    stopFile     = $null            # filled in once the run directory is known
    graceSec     = $TraceStopGraceSec
    trailSecs    = $TrailSecs
    probes       = @($ProbeWatch, $ProbeListen)
}
if ($TraceStopMode -ne 'stop-file') {
    Add-Flag 'WARN' ('traceStop={0}: at least one observer cannot be told to stop, so it runs to its duration cap and teardown waits for it (the pre-turnaround dead time). Rebuild/redeploy tools/WatchVrf and tools/ListenReports with --stop-file support.' -f $TraceStopMode)
}

# What the ORDER asks for - needed by -StopWhenComplete, recorded regardless so a
# reader of the manifest can see what "all taskees" meant for this run.
$OrderTasks   = @(Get-OrderTasks   -OrderText (Get-Content -LiteralPath $Order -Raw))
$OrderTaskees = @(Get-OrderTaskees -OrderText (Get-Content -LiteralPath $Order -Raw))
# taskee UUID -> marking, for the report-evidence half of the early-exit criterion
# (RunnerLib Test-ReportEvidence). Read once; the init does not change mid-run.
$TaskeeNames  = Get-InitUnitNames -InitText (Get-Content -LiteralPath $Init -Raw -Encoding UTF8)
# RPT/POS agreement tolerance for that criterion. 2 m: the two entity taskees read
# 0.0 m in run 20260901T235823Z; the company aggregate read 11.8 m while its centre
# was still converging - the miss this condition exists to catch.
$ReportToleranceMeters = 2.0
$Manifest.inputs.orderTaskCount = $OrderTasks.Count
$Manifest.inputs.orderTaskees   = $OrderTaskees
if ($StopWhenComplete -and $OrderTaskees.Count -eq 0) {
    Add-Flag 'WARN' '-StopWhenComplete is set but the order yields ZERO (Task, PerformingEntity) pairs; early exit can never fire and the window will run to -RunSecs.'
}

# =============================================================================
# STAGE 1a - RUNNER LAUNCH LOCK (RUNBOOK 0.5.14 item 15)
# =============================================================================
# WHY THIS EXISTS. Run V8z (2026-09-15 03:51Z, voided): two RunC2SimScenario.ps1
# instances were started 2 s apart. BOTH passed the pre-flight inventory below (it
# checks vrfLauncher/vrfSimHLA1516e/vrfGui/WatchVrf/ListenReports, never ANOTHER
# RUNNER), both allocated appNos, both launched; the second saw the first's READY
# back end, its PushInit failed, and ITS teardown STOPPED THE SIM under the FIRST
# runner's live order. Before anything else: refuse if another runner is live,
# then take an exclusive lock so a second runner cannot start until the first has
# finished tearing down. The parsing/matching helpers are PURE and live in
# RunnerLib.ps1 (Get-OtherRunnerProcessInfo, ConvertFrom-RunnerLockText,
# Format-RunnerLockContent, Format-OtherRunnerRefusal) so they can be exercised
# offline; the file/process I/O below cannot be pure and stays here.
Say-Head 'Stage 1a - runner launch lock'

$script:RunnerLockTaken = $false   # initialised BEFORE any path that can reach the outer finally (dry runs and early aborts never take the lock)
$PathRunnerLock = Join-Path $RunRoot 'runner.lock'
# The run directory THIS invocation would create if it gets past Stage 1/2 (the
# REAL $RunId/$RunDir are computed later, under "RUN DIRECTORY + DERIVED PATHS",
# from this same $stamp/$RunRoot - known early only so the lock can name it).
$IntendedRunDir = Join-Path $RunRoot ('{0}_run' -f $stamp)

$PwshSnapshot = @()
try { $PwshSnapshot = @(Get-CimInstance -ClassName Win32_Process -Filter "Name = 'pwsh.exe'" -ErrorAction Stop) } catch { $PwshSnapshot = @() }
# @(...) on both: see the note in RunnerLib.ps1's Get-OtherRunnerProcessInfo - a
# HashSet or array return value gets unwrapped to a bare scalar by the pipeline
# when it holds exactly one item, which breaks .Contains()/.Count/[0] downstream.
$SelfAncestorIds = @(Get-ProcessAncestorIds -StartPid $PID -Processes $PwshSnapshot)
$OtherRunners    = @(Get-OtherRunnerProcessInfo -Processes $PwshSnapshot -SelfPid $PID)

$ExistingLockText = ''
if (Test-Path -LiteralPath $PathRunnerLock -PathType Leaf) {
    try { $ExistingLockText = Get-Content -LiteralPath $PathRunnerLock -Raw -Encoding ASCII } catch { $ExistingLockText = '' }
}
$ExistingLock = ConvertFrom-RunnerLockText -Text $ExistingLockText

# Fold the lock file's pid into the picture: if it names a pid the process scan
# already found, reuse its recorded run dir instead of "(unknown)". If the scan
# found nothing but the lock names a DIFFERENT, still-alive, non-ancestor pid,
# that is a second live runner the scan missed (only a race before it wrote the
# lock should ever produce this).
$LiveOther = $null
if ($OtherRunners.Count -gt 0) {
    $o  = $OtherRunners[0]
    $rd = if ($ExistingLock.pid -eq $o.pid -and $ExistingLock.runDir) { $ExistingLock.runDir } else { '(unknown - no launch lock written yet)' }
    $LiveOther = [ordered]@{ pid = $o.pid; utc = $o.startedUtc; runDir = $rd }
} elseif ($ExistingLock.pid -gt 0 -and -not $SelfAncestorIds.Contains($ExistingLock.pid)) {
    $stillAlive = $false
    try { $null = Get-Process -Id $ExistingLock.pid -ErrorAction Stop; $stillAlive = $true } catch { $stillAlive = $false }
    if ($stillAlive) { $LiveOther = $ExistingLock }
}

if ($LiveOther) {
    $lockMsg = Format-OtherRunnerRefusal -OtherPid $LiveOther.pid -Utc $LiveOther.utc -RunDir $LiveOther.runDir
    if ($DryRun) {
        Say-Warn ('DRY RUN - would REFUSE: {0}' -f $lockMsg)
        Say-Warn '  a dry run launches nothing, so it proceeds anyway - reporting only, by design.'
    } else {
        Say-Head 'Result'
        Say-Fail $lockMsg
        Say-Fail '  RUNBOOK 0.5.14 item 15 (the V8z double-launch incident): a second runner must never'
        Say-Fail '  start while a first one is live. It releases the lock in its own teardown, AFTER'
        Say-Fail '  tearing down - wait for it, then re-run.'
        exit 2
    }
} else {
    Say-Ok 'no other runner process is live'
}

if ($DryRun) {
    Say-Plan ('would take the exclusive lock file {0} (pid {1}, run dir {2}); NOT taken in a dry run.' -f $PathRunnerLock, $PID, $IntendedRunDir)
} else {
    if ($ExistingLock.pid -gt 0) {
        # Reaching here with a lock file already on disk means $LiveOther above was
        # $null for it - its pid is dead (or our own ancestor) - so it is STALE.
        # Report it and remove it rather than refuse a healthy launch forever.
        Say-Warn ('stale lock file {0}: pid {1} is not running. Removing it.' -f $PathRunnerLock, $ExistingLock.pid)
        try { Remove-Item -LiteralPath $PathRunnerLock -Force -ErrorAction Stop } catch {
            Say-Fail ('could not remove the stale lock file {0}: {1}' -f $PathRunnerLock, $_.Exception.Message)
            exit 2
        }
    }
    $LockDir = Split-Path -Parent $PathRunnerLock
    if (-not (Test-Path -LiteralPath $LockDir -PathType Container)) { New-Item -ItemType Directory -Path $LockDir -Force | Out-Null }
    $LockNowUtc  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $LockContent = Format-RunnerLockContent -RunnerPid $PID -Utc $LockNowUtc -RunDir $IntendedRunDir
    $LockTaken = $false
    for ($lockAttempt = 1; $lockAttempt -le 2 -and -not $LockTaken; $lockAttempt++) {
        try {
            $lockFs = [System.IO.File]::Open($PathRunnerLock, [System.IO.FileMode]::CreateNew,
                                             [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                $lockBytes = [System.Text.Encoding]::ASCII.GetBytes($LockContent)
                $lockFs.Write($lockBytes, 0, $lockBytes.Length)
            } finally { $lockFs.Dispose() }
            $LockTaken = $true
        } catch [System.IO.IOException] {
            # CreateNew fails if the file exists - either a genuine race (someone
            # else took it in the instant between our checks above and here) or a
            # lock left by a runner that has since died. Distinguish by pid.
            $racedText = ''
            try { $racedText = Get-Content -LiteralPath $PathRunnerLock -Raw -Encoding ASCII } catch { }
            $raced = ConvertFrom-RunnerLockText -Text $racedText
            $racedAlive = $false
            if ($raced.pid -gt 0) { try { $null = Get-Process -Id $raced.pid -ErrorAction Stop; $racedAlive = $true } catch { $racedAlive = $false } }
            if ($racedAlive -and -not $SelfAncestorIds.Contains($raced.pid)) {
                Say-Head 'Result'
                Say-Fail (Format-OtherRunnerRefusal -OtherPid $raced.pid -Utc $raced.utc -RunDir $raced.runDir)
                Say-Fail '  (won the lock in the instant between this runner''s checks - RUNBOOK 0.5.14 item 15.)'
                exit 2
            }
            if ($lockAttempt -eq 1) {
                Say-Warn ('stale lock file {0} appeared mid-check. Removing it and retrying once.' -f $PathRunnerLock)
                try { Remove-Item -LiteralPath $PathRunnerLock -Force -ErrorAction Stop } catch { }
            } else {
                Say-Fail ('could not create the lock file {0} after a retry: {1}' -f $PathRunnerLock, $_.Exception.Message)
                exit 2
            }
        }
    }
    $script:RunnerLockTaken = $true
    $script:RunnerLockPath  = $PathRunnerLock
    Say-Ok ('launch lock taken: {0}' -f $PathRunnerLock)
}

# OUTER try covering EVERYTHING from here to the end of the script (closed by the
# "finally" at the very end of the file, which releases the launch lock). See that
# finally's header comment for why this exists (RUNBOOK 0.5.14 item 15 addendum,
# 2026-09-15, V6b live defect) - every exit path after the lock is taken, not only
# the ones that reach the launch try/catch/finally below, must release it.
try {

# =============================================================================
# STAGE 1 - PRE-FLIGHT PROCESS INVENTORY (RUNBOOK 0.5.0)
# =============================================================================
Say-Head 'Stage 1 - pre-flight process inventory (RUNBOOK 0.5.0)'
$existing = @()
foreach ($n in @($ProcLauncher, $ProcBackend, $ProcFrontend)) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
        $threads = '?'
        try { $threads = $p.Threads.Count } catch { }
        $existing += [ordered]@{ name = $p.Name; processId = $p.Id; threads = $threads }
        Say-Warn ('{0} pid={1} threads={2} ALREADY RUNNING' -f $p.Name, $p.Id, $threads)
    }
}
# A leftover observer of OUR OWN is a hard block too (review F1): a WatchVrf from an
# earlier run is a foreign federate still joined to CWIX-2024 and would contaminate
# the trace; a ListenReports is still subscribed to the topic. Report and refuse -
# NEVER kill; both end on their own duration cap, so the fix is to wait it out.
$existingObservers = @()
foreach ($n in $ProcObservers) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
        $started = ''
        try { $started = $p.StartTime.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ') } catch { }
        $existingObservers += [ordered]@{ name = $p.Name; processId = $p.Id; startedUtc = $started }
        Say-Warn ('{0} pid={1} started={2} ALREADY RUNNING - a leftover observer' -f $p.Name, $p.Id, $started)
    }
}
$infra = @()
foreach ($n in $RtiNames) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
        $title = ''
        try { $title = $p.MainWindowTitle } catch { }
        $infra += [ordered]@{ name = $p.Name; processId = $p.Id; windowTitle = $title }
        Say-Ok ('{0} pid={1} - RTI INFRASTRUCTURE. Never touched, never refused on (RUNBOOK 0.5.2). Window: "{2}"' -f $p.Name, $p.Id, $title)
    }
}
$Manifest.preflight.existingVrf       = $existing
$Manifest.preflight.existingObservers = $existingObservers
$Manifest.preflight.rtiInfra          = $infra

if ($existingObservers.Count -gt 0) {
    Say-Head 'Result'
    Say-Fail 'A WatchVrf / ListenReports observer from an earlier run is STILL RUNNING. Refusing to start.'
    Say-Fail '  WatchVrf is a JOINED FEDERATE: a new run would share the federation with it and its'
    Say-Fail '  trace would be foreign evidence. It is NOT killed (RUNBOOK sec 0) - it ends on its own'
    Say-Fail '  duration cap (its 2nd argument, seconds, from its start time). Wait for it, then re-run.'
    Say-Fail '  A leftover observer means the previous run''s teardown saw it still running after its'
    Say-Fail '  grace + cap wait: read that run''s manifest flags before running again.'
    exit 2
}
if ($existing.Count -gt 0) {
    Say-Head 'Result'
    Say-Fail 'A VR-Forces instance is ALREADY RUNNING. Refusing to start.'
    Say-Fail '  RUNBOOK 0.5.0: an instance from an earlier session survives a context clear. Its'
    Say-Fail '  scenario contents are UNKNOWN and could contaminate a scored trace, and LaunchVrf.ps1'
    Say-Fail '  hard-fails on it anyway (LaunchVrf.ps1:255-260).'
    Say-Fail '  DO NOT reach for -AllowExistingVrf. It is the FALSE-READY trap: LaunchVrf picks the'
    Say-Fail '  back-end with Select-Object -First 1 and would measure the OLD instance. This runner'
    Say-Fail '  does not offer that switch at all.'
    Say-Fail '  FIX: pwsh -File scripts\StopVrf.ps1   (leaves rtiAssistant/rtiexec/rtiForwarder up), then re-run.'
    exit 2
}
Say-Ok 'no pre-existing vrfLauncher / vrfSimHLA1516e / vrfGui and no leftover WatchVrf / ListenReports - clear to launch'
if ($infra.Count -eq 0 -and $Is52) {
    # No ASSISTANT is expected here (RTI_ASSISTANT_DISABLE + the rid-configured connection;
    # one would be ignored, not consulted). An RTIEXEC, however, IS required from 2026-09-04
    # on - lightweight mode is not supported with VR-Forces - and Stage 2r starts one if this
    # inventory found none. That is the normal first-run-after-a-reboot path, not a fault.
    Say-Ok 'no RTI infrastructure process running - no rtiAssistant is expected on the 5.2 profile (assistant-free; the connection comes from the rid).'
    Say-Ok '  No rtiexec either: Stage 2r will START one (headless, rid-configured). Later runs find it already up and touch nothing.'
} elseif ($infra.Count -eq 0) {
    Say-Warn 'no rtiAssistant is running. RUNBOOK 0.5.3: on HLA a federate does not start until an'
    Say-Warn '  RTI Assistant has been ANSWERED. LaunchVrf.ps1 warns about this too and will proceed;'
    Say-Warn '  if the launch stalls at 2-4 back-end threads, that is the cause. Do NOT kill anything.'
    Add-Flag 'WARN' 'No pre-existing rtiAssistant at pre-flight (RUNBOOK 0.5.3 - unattended launch depends on an already-answered one).'
}

# Optional read-only reachability probe of the C2SIM server, BEFORE VR-Forces goes
# up, so a dead broker costs a launch cycle instead of a whole run. Read-only GET.
if (-not $SkipServerCheck) {
    if ($DryRun) {
        Say-Plan ('would GET {0} (read-only reachability probe; -SkipServerCheck disables it). NOT PERFORMED IN A DRY RUN.' -f $RestUrl)
    } else {
        try {
            $r = Invoke-WebRequest -Uri $RestUrl -UseBasicParsing -TimeoutSec 15
            Say-Ok ('C2SIM REST reachable: HTTP {0}' -f $r.StatusCode)
            $Manifest.preflight.c2simRestStatus = $r.StatusCode
        } catch {
            $Manifest.preflight.c2simRestStatus = ('unreachable: ' + $_.Exception.Message)
            Say-Head 'Result'
            Say-Fail ('C2SIM REST at {0} is not reachable: {1}' -f $RestUrl, $_.Exception.Message)
            Say-Fail '  RUNBOOK sec 1: the C2SIM server container for these endpoints must be up (private test server c2sim-server-vrf = REST 18080, STOMP 61614; operator server = 8080 / 61613).'
            Say-Fail '  Aborting BEFORE VR-Forces is launched. Pass -SkipServerCheck to bypass.'
            exit 2
        }
    }
}

# =============================================================================
# STAGE 2 - APPLICATION NUMBERS: ALLOCATE AND LEDGER BEFORE ANY JOIN
# =============================================================================
# HEADLESS_RUN_PLAN sec 2 / RUNBOOK 0.5.1 / Appendix B: EVERY join takes a fresh
# number from the single marker, ledgered BEFORE the join. That includes the app's
# own Vrf__ApplicationNumber, historically hand-set and exactly as capable of
# causing a stale-federate hang as any other join.
#
# The marker is searched BY ITS FORM. The bare string "*** NEXT FREE:" also
# matches the instructions and several pointers elsewhere in that file; only ONE
# line carries a number, and Appendix B says to STOP and reconcile if two are ever
# found. This regex enforces exactly that.
Say-Head 'Stage 2 - appNumber allocation (Appendix B marker), BEFORE any join'

$MarkerPattern = '\*\*\*\s*NEXT\s+FREE:\s*(\d+)\s*\*\*\*'

$ledgerRaw = Get-Content -LiteralPath $LedgerDoc -Raw -Encoding UTF8
$markerMatches = [regex]::Matches($ledgerRaw, $MarkerPattern)
if ($markerMatches.Count -ne 1) {
    Say-Head 'Result'
    Say-Fail ('found {0} value-bearing "NEXT FREE" markers in {1}; expected EXACTLY 1.' -f $markerMatches.Count, $LedgerDoc)
    Say-Fail '  Appendix B: "There is exactly ONE such line; if you ever find two, STOP and reconcile."'
    Say-Fail '  Aborting. Nothing was launched, nothing was ledgered.'
    exit 2
}
$FirstFree = [int]$markerMatches[0].Groups[1].Value
if ($FirstFree -le 0 -or $FirstFree -gt 65000) {
    Say-Fail ('the marker value {0} is not a usable appNumber (WatchVrf accepts 1..65535).' -f $FirstFree)
    exit 2
}

# The allocation. Purposes are the ledger text; keep them specific enough that a
# reader six months later can tell which join each number was.
$Alloc = @(
    # The purpose text is what lands in the LEDGER, so it names the launch mode that
    # actually consumed the number. 5.0.2 wording is unchanged.
    [ordered]@{ key='vrfBackend';  purpose=$(if ($Is52) { 'LaunchVrf52.ps1 back-end (vrfSimHLA1516e), 5.2d independent mode' } else { 'LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode' }) }
    [ordered]@{ key='vrfFrontend'; purpose=$(if ($Is52) { 'LaunchVrf52.ps1 front-end (vrfGui), 5.2d independent mode (allocated even with -NoGui, then BURNED)' } else { 'LaunchVrf.ps1 front-end (vrfGui), combined mode' }) }
    [ordered]@{ key='oraclePre';   purpose='WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)' }
    [ordered]@{ key='oracleTrace'; purpose='WatchVrf MAIN run trace - the movement oracle / scoring input' }
    [ordered]@{ key='app';         purpose='VrfC2SimApp Vrf__ApplicationNumber (the interface federate)' }
    [ordered]@{ key='rtiProbe';    purpose='tools/RtiProbe - STAGE 2c PRE-LAUNCH RTI READINESS GATE (C1). Throwaway create-or-join against the federation with internal retry+backoff, then clean resign, BEFORE the back-end launches (RTI_LAUNCH_HARDENING_DESIGN.md A2-A7 - the RUN-2 fix). CONSUMED on EVERY run (the gate always runs pre-launch). One number covers all internal retries - RtiProbe reuses this single appNumber across attempts by design.' }
    [ordered]@{ key='createOneDiag'; purpose='tools/CreateOne - STAGE 7b FAILURE-PATH DIAGNOSTIC ONLY (RUNBOOK 0.5.7 STRONGER CHECK). CONSUMED ONLY IF THE ORACLE GATE FAILS; on a healthy run it is NEVER JOINED and this number goes UNCONSUMED. Unconsumed numbers are BURNED, never recycled - see the NOTE below. Allocated here rather than mid-run because every number must be ledgered BEFORE any join.' }
)
# THE Q5 PROBE, appended ONLY when armed, so a DEFAULT run's ledger footprint stays exactly
# the seven numbers every run in the record claimed. ONE NUMBER PER INVOCATION: tools/PauseSim
# joins and resigns each time it is called, so the pause and the resume are two joins and two
# numbers - the rule the tools/SetSimRate entry in Appendix B already states ("four
# invocations, four numbers"). Never one number reused for both halves.
if ($PauseAtSec -gt 0) {
    $Alloc += [ordered]@{ key='pauseSim'; purpose=('tools/PauseSim pause - STAGE 8b Q5 PROBE at t+{0}s of the observation window (controller->pause() on ALL back ends). CONSUMED ONLY IF the window is still open at that offset; an unconsumed number is BURNED, never recycled.' -f $PauseAtSec) }
}
if ($ResumeAtSec -gt 0) {
    $Alloc += [ordered]@{ key='resumeSim'; purpose=('tools/PauseSim resume - STAGE 8b Q5 PROBE at t+{0}s of the observation window (controller->run() on ALL back ends). A SEPARATE join from the pause and therefore a separate number. CONSUMED ONLY IF the window is still open at that offset.' -f $ResumeAtSec) }
}
# STAGE 2h - THE FEDERATION HOLDER (STP-825), appended ONLY when the stage is armed, so a
# 5.0.2 run and a -FederationHoldSecs 0 run keep exactly the ledger footprint every run in
# the record claimed (the first seven numbers above are untouched either way). ONE NUMBER PER
# ATTEMPT, and that is not tidiness: the operation that fails here is the CREATE, a failed
# create CRASHES the creating process (STP-832), and a crashed creator may already have
# registered a federate - so retrying on the same appNumber is exactly the stale-federate
# trap RUNBOOK sec 0 exists for. Attempts after the one that succeeds are UNCONSUMED and
# BURNED, like every other unconsumed number this runner allocates.
if ($FederationHoldOn) {
    for ($h = 1; $h -le $FederationHoldAttempts; $h++) {
        $Alloc += [ordered]@{ key=('fedHold{0}' -f $h); purpose=('tools/RtiProbe - STAGE 2h FEDERATION HOLDER attempt {0} of {1} (STP-825): create-or-join {2} and STAY JOINED for {3}s so the SIM never has to CREATE the federation (rtiexec 5.0.1 rejects creator FOM distribution intermittently; joins have never failed). CONSUMED ONLY IF attempt {0} is reached; an earlier success leaves the rest UNCONSUMED and BURNED.' -f $h, $FederationHoldAttempts, $FederationHoldName, $FederationHoldSecs) }
    }
}
for ($i = 0; $i -lt $Alloc.Count; $i++) { $Alloc[$i].appNumber = $FirstFree + $i }
$AppNo = @{}
foreach ($a in $Alloc) { $AppNo[$a.key] = $a.appNumber }
$NextFree = $FirstFree + $Alloc.Count
# The holder's numbers in attempt order, so Stage 2h can index them without re-deriving
# the key names. EMPTY (and never read) when the stage is off.
$FedHoldAppNos = @()
if ($FederationHoldOn) {
    for ($h = 1; $h -le $FederationHoldAttempts; $h++) { $FedHoldAppNos += $AppNo[('fedHold{0}' -f $h)] }
}

Say ('  marker currently reads : {0}' -f $FirstFree)
foreach ($a in $Alloc) { Say ('    {0,-6}  {1,-12} {2}' -f $a.appNumber, $a.key, $a.purpose) }
Say ('  marker would advance to: {0}' -f $NextFree)

$Manifest.appNumbers      = $Alloc
$Manifest.ledger.file     = $LedgerDoc
$Manifest.ledger.wasValue = $FirstFree
$Manifest.ledger.newValue = $NextFree
if ($FederationHoldOn) { $Manifest.inputs.vrfProfile.federationHolder.appNumbers = $FedHoldAppNos }

function Update-Ledger {
    param([int]$From, [int]$To, [string]$RunId, $Allocation)
    $nl = if ($ledgerRaw -match "`r`n") { "`r`n" } else { "`n" }
    $lines = @()
    $lines += ''
    $lines += ('CLAIMED {0} by scripts/RunC2SimScenario.ps1 (run {1}). Ledgered BEFORE any join,' -f (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm'), $RunId)
    $lines += 'per the never-reuse non-negotiable. Annotate with results from the run manifest.'
    foreach ($a in $Allocation) { $lines += ('- {0}: CLAIMED - {1}' -f $a.appNumber, $a.purpose) }
    $lines += 'NOTE: numbers this runner allocates but does not consume (e.g. an abort before the'
    $lines += 'join) are BURNED, not recycled. The run manifest records which were actually used.'
    $lines += ''
    $block = ($lines -join $nl) + $nl

    $m = [regex]::Match($ledgerRaw, $MarkerPattern)
    if (-not $m.Success) { throw 'the marker vanished between the read and the write - refusing to guess.' }
    $newMarker = $m.Value -replace [regex]::Escape($From.ToString()), $To.ToString()
    $updated = $ledgerRaw.Substring(0, $m.Index) + $block + $newMarker + $ledgerRaw.Substring($m.Index + $m.Length)

    # Re-verify BEFORE writing: exactly one marker, and it carries the new value.
    $check = [regex]::Matches($updated, $MarkerPattern)
    if ($check.Count -ne 1 -or [int]$check[0].Groups[1].Value -ne $To) {
        throw ('ledger rewrite self-check FAILED (markers={0}). Not written.' -f $check.Count)
    }
    # Explicit CRLF (ConvertTo-CrlfText): the repo checks out CRLF and an LF working
    # copy must not be perpetuated (found after run 20260901T235823Z).
    [System.IO.File]::WriteAllText($LedgerDoc, (ConvertTo-CrlfText $updated), (New-Object System.Text.UTF8Encoding($false)))
}

# =============================================================================
# RUN DIRECTORY + DERIVED PATHS
# =============================================================================
$RunId  = ('{0}_run' -f $stamp)
$RunDir = Join-Path $RunRoot $RunId

$PathTrace        = Join-Path $RunDir 'watchvrf-trace.csv'
$PathTraceErr     = Join-Path $RunDir 'watchvrf-trace.stderr.log'
$PathPreTrace     = Join-Path $RunDir 'watchvrf-precheck.csv'
$PathPreTraceErr  = Join-Path $RunDir 'watchvrf-precheck.stderr.log'
$PathReports      = Join-Path $RunDir 'reports-captured.log'
$PathReportsOut   = Join-Path $RunDir 'listenreports.stdout.log'
$PathReportsErr   = Join-Path $RunDir 'listenreports.stderr.log'
$PathAppLog       = Join-Path $RunDir 'vrfc2simapp.log'
$PathAppErr       = Join-Path $RunDir 'vrfc2simapp.stderr.log'
$PathLaunchOut    = Join-Path $RunDir 'launchvrf.stdout.log'
$PathLaunchErr    = Join-Path $RunDir 'launchvrf.stderr.log'
$PathPushInitOut  = Join-Path $RunDir 'pushinit.stdout.log'
$PathPushInitErr  = Join-Path $RunDir 'pushinit.stderr.log'
$PathPushOrderOut = Join-Path $RunDir 'pushorder.stdout.log'
$PathPushOrderErr = Join-Path $RunDir 'pushorder.stderr.log'
$PathBusLog       = Join-Path $RunDir 'c2sim-bus.log'
$PathStopIfaceOut = Join-Path $RunDir 'stopiface.stdout.log'
$PathStopIfaceErr = Join-Path $RunDir 'stopiface.stderr.log'
$PathCreateOneOut = Join-Path $RunDir 'createone-diagnostic.stdout.log'
$PathCreateOneErr = Join-Path $RunDir 'createone-diagnostic.stderr.log'
$PathRtiProbeOut  = Join-Path $RunDir 'rtiprobe.stdout.log'
$PathRtiProbeErr  = Join-Path $RunDir 'rtiprobe.stderr.log'
# Stage 8b Q5 probe. Written only when -PauseAtSec / -ResumeAtSec are armed AND the offset is
# actually reached; each half gets its own pair, so the two invocations never overwrite
# each other's evidence.
$PathPauseSimOut  = Join-Path $RunDir 'pausesim-pause.stdout.log'
$PathPauseSimErr  = Join-Path $RunDir 'pausesim-pause.stderr.log'
$PathResumeSimOut = Join-Path $RunDir 'pausesim-resume.stdout.log'
$PathResumeSimErr = Join-Path $RunDir 'pausesim-resume.stderr.log'
# Stage 2r (5.2 only). Named for the stage, not for rtiexec, because the process it ensures
# is up is NOT this run's child. The STAGE's own stdout/stderr are this run's evidence and
# live here; the RTIEXEC'S log does NOT - it goes to runs\launch52 (persistent and
# gitignored), because the rtiexec outlives the run that started it and keeps writing to that
# file while it serves later runs. Filing it under one run's directory would attribute a
# shared, long-lived process's output to a single run. The manifest records the exact path.
$PathRtiExecOut   = Join-Path $RunDir 'startrtiexec.stdout.log'
$PathRtiExecErr   = Join-Path $RunDir 'startrtiexec.stderr.log'
$PathStopVrfOut   = Join-Path $RunDir 'stopvrf.stdout.log'
$PathStopVrfErr   = Join-Path $RunDir 'stopvrf.stderr.log'
# Stage 6b-w, the detached teardown watchdog. All four files live in the run directory
# because the watchdog OUTLIVES the runner and must not hold a handle the runner (or the
# wrapper, or the terminal) owns - see the -NewConsole / -StdInFile block in Start-External.
# watchdog.stdin.empty is a zero-byte file: Start-Process needs a real path to redirect from,
# and an empty file is the Windows equivalent of the wrapper's `< /dev/null`.
$PathWatchdogOut  = Join-Path $RunDir 'watchdog.stdout.log'
$PathWatchdogErr  = Join-Path $RunDir 'watchdog.stderr.log'
$PathWatchdogIn   = Join-Path $RunDir 'watchdog.stdin.empty'
$PathWatchdogPid  = Join-Path $RunDir 'watchdog.pid'
# Stage 2h - the federation holder (STP-825). Each ATTEMPT gets its own stdout/stderr pair
# (holder.<n>.stdout.log / .stderr.log, built in the stage) so a failed create and the
# attempt that finally joined never overwrite each other's evidence. holder.stdin.empty is
# the same zero-byte device the watchdog uses and for the same reason: the holder is started
# DETACHED and OUTLIVES this runner, so it must not hold the launching terminal's stdin open.
$PathHolderStdIn  = Join-Path $RunDir 'holder.stdin.empty'
$ManifestPath     = Join-Path $RunDir 'run-manifest.json'
# The observers' stop signal. Teardown creates it at StopIface + TrailSecs; WatchVrf
# and ListenReports poll for it once a second and resign / disconnect on seeing it.
# Inside the run directory so (a) it is new for every run - both tools REFUSE a
# pre-existing stop file with exit 2 - and (b) it stays with the evidence.
$PathStopFile     = Join-Path $RunDir 'observers.stop'
$Manifest.inputs.traceStop.stopFile = $PathStopFile
# Fail fast (review F5): the run directory is named to the UTC second, so a stop file
# already at this path means the WHOLE run directory collides with an earlier run
# (its ledger block, manifest and logs would be overwritten). That is not something to
# delete silently; refuse before anything is created, launched or ledgered. Without
# this check the first symptom would be both observers refusing with exit 2 after a
# full launch cycle.
if (Test-Path -LiteralPath $PathStopFile) {
    Say-Fail ('the observer stop file ALREADY EXISTS: {0}' -f $PathStopFile)
    Say-Fail ('  The run directory {0} collides with an earlier run (same UTC second). Nothing was created,' -f $RunDir)
    Say-Fail '  launched or ledgered. Do NOT delete the file - re-run; the new run id is a fresh directory.'
    exit 2
}

# -ConsoleLogDir is OPT-IN and is a NAME, not a path: it is joined to $RunDir, so it is
# validated as a single filename component here. Rejecting separators and '..' is not
# defensive noise - '..\..\somewhere' would silently place backend-written console files
# OUTSIDE the run directory, and the run artifact would then be incomplete in a way no
# later reader could detect. Checked BEFORE anything launches, so this is an exit-2
# (nothing was done) failure, consistent with the rest of the pre-flight.
$PathConsoleDir = $null
$WatchConsoleArgs = @()
if ($PSBoundParameters.ContainsKey('ConsoleLogDir')) {
    $bad = [System.IO.Path]::GetInvalidFileNameChars()
    if ([string]::IsNullOrWhiteSpace($ConsoleLogDir) -or
        $ConsoleLogDir -eq '.' -or $ConsoleLogDir -eq '..' -or
        $ConsoleLogDir.IndexOfAny($bad) -ge 0) {
        Say-Fail ('-ConsoleLogDir must be a single directory NAME inside the run directory, not a path; got: {0}' -f $ConsoleLogDir)
        exit 2
    }
    $PathConsoleDir = Join-Path $RunDir $ConsoleLogDir
    # *** DISARMED 2026-07-19 LATE - THIS WAS A LIVE LANDMINE. ***
    # The --console-log-dir flag went out with the native revert (commit 5d14eda) and no
    # longer exists in tools/WatchVrf. WatchRunner.cs rejects ANY unknown flag on the LIVE
    # path with exit 2, so passing it KILLED THE MOVEMENT ORACLE STAGE - and therefore the
    # whole run - after a full launch cycle and six burned appNumbers. Verified against the
    # deployed binary: `WatchVrf.exe 3599 30 2 CWIX-2024 --console-log-dir foo` -> EXIT=2.
    # Kept accepted-but-ignored rather than fatal so existing call sites do not break.
    # RE-ENABLE ONLY when the flag actually exists in WatchVrf again.
    $WatchConsoleArgs = @()
    Say-Warn ('-ConsoleLogDir is IGNORED. tools/WatchVrf has no --console-log-dir flag - it ' +
              'went out with the native revert, and passing it would fail the oracle stage ' +
              'with exit 2 and kill the run.')
    $Manifest.artifacts.consoleLogDir = 'IGNORED - flag does not exist in WatchVrf (native revert 5d14eda)'
}

$Manifest.artifacts.runDir        = $RunDir
$Manifest.artifacts.trace         = $PathTrace
$Manifest.artifacts.preCheckTrace = $PathPreTrace
$Manifest.artifacts.reports       = $PathReports
$Manifest.artifacts.appLog        = $PathAppLog
$Manifest.artifacts.busLog        = $PathBusLog
$Manifest.artifacts.createOneDiagnosticLog = $PathCreateOneOut
$Manifest.artifacts.manifest      = $ManifestPath

# WatchVrf / ListenReports must cover the WHOLE run (a 4a.6 run-validity item), so
# the duration is an UPPER BOUND over every intermediate wait. It is the CAP, not
# the run length: when the deployed observers support --stop-file (stage 0b),
# teardown ends them at StopIface + TrailSecs and the sum below is only the safety
# net for a runner that dies mid-run. When they do not, the observers run the sum
# out and teardown waits for them (measured 2026-09-01: ~8.3 min of dead time per
# run - docs/RUNNER_TURNAROUND_2026-09-01.md). Formula unchanged, by design.
$DerivedWatchSecs = Get-DerivedWatchSecs -PreRollSecs $PreRollSecs -AppJoinTimeoutSec $AppJoinTimeoutSec `
                        -InitDispatchWaitSec $InitDispatchWaitSec -OracleGateTimeoutSec $OracleGateTimeoutSec `
                        -PushOrderListenSec $PushOrderListenSec -RunSecs $RunSecs -TrailSecs $TrailSecs
# The stage-7d pre-order settle sits INSIDE the observers' coverage - between the oracle gate
# and PushOrder - so it has to be added to their cap, or a run with a long hold could end its
# trace before the window does (silent evidence loss). Added HERE rather than inside
# Get-DerivedWatchSecs so the pinned formula (tests\RunnerTurnaround.Tests.ps1 check 1, and the
# record it reproduces) is untouched; with the default 0 this line changes nothing.
if ($PreOrderSettleSecs -gt 0) { $DerivedWatchSecs += $PreOrderSettleSecs }
# Same argument for the stage-7d READY GATE, and it is the reason the watchdog budget below
# covers it too ($WatchdogCoverSecs takes the max of this and an explicit -WatchSecs): the
# gate's WORST case is the full timeout, spent inside the observers' coverage. When the gate
# AND the settle are both given the settle is only the timeout fallback - but the worst case
# is then gate-timeout THEN settle, so both are added and the cap is right for that path too.
if ($PreOrderGateOn) { $DerivedWatchSecs += $PreOrderGateTimeoutSec }
$EffWatchSecs = if ($WatchSecs -gt 0) { $WatchSecs } else { $DerivedWatchSecs }
if ($EffWatchSecs -le 0) { Say-Fail 'computed observer duration is not positive.'; exit 2 }
$Manifest.inputs.watchSecs          = $EffWatchSecs
$Manifest.inputs.watchSecsDerived   = $DerivedWatchSecs
$Manifest.inputs.preOrderSettleSecs = $PreOrderSettleSecs
$Manifest.inputs.preOrderGate            = $(if ($PreOrderGateOn) { $PreOrderGate } else { '' })
$Manifest.inputs.preOrderGateTimeoutSec  = $(if ($PreOrderGateOn) { $PreOrderGateTimeoutSec } else { 0 })
$Manifest.inputs.pauseAtSec              = $PauseAtSec
$Manifest.inputs.resumeAtSec             = $ResumeAtSec
$Manifest.inputs.federationHoldSecs      = $FederationHoldSecs
$Manifest.inputs.federationHoldAttempts  = $FederationHoldAttempts
$Manifest.inputs.objectConsoleNotifyLevel = $ObjectConsoleLevel
# An EXPLICIT -WatchSecs wins over the derived value, settle included - which is correct (it is
# the operator's choice) and is also how a stage-7d hold could silently truncate a trace: the
# hold is spent INSIDE the observers' coverage, so a cap that was already below the derived
# value loses those seconds off the END of the window. Said out loud rather than adjusted.
# The READY GATE grows the cap the same way and for the same reason, so it earns the same
# WARN - found by the review of this change: without the -or below, a gated run with an
# explicit low -WatchSecs would truncate its trace in silence.
$PreOrderBudgetText = @()
if ($PreOrderGateOn)           { $PreOrderBudgetText += ('READY GATE timeout {0}s' -f $PreOrderGateTimeoutSec) }
if ($PreOrderSettleSecs -gt 0) { $PreOrderBudgetText += ('settle {0}s' -f $PreOrderSettleSecs) }
if (($PreOrderSettleSecs -gt 0 -or $PreOrderGateOn) -and $WatchSecs -gt 0 -and $WatchSecs -lt $DerivedWatchSecs) {
    Add-Flag 'WARN' ('-WatchSecs {0} was passed EXPLICITLY and is below the derived cap {1}, which now includes the stage-7d budget ({2}). The observers can end BEFORE the observation window does, truncating the trace with no error. Raise -WatchSecs to at least {1}, or drop it and let the runner derive it.' -f $WatchSecs, $DerivedWatchSecs, ($PreOrderBudgetText -join ' + '))
}

# HLA environment, identical for WatchVrf and the app (RUNBOOK sec 7 items 1-3).
$PathPrefix = ('{0};{1};{2}' -f $Bin64, (Join-Path $VrLinkRoot 'bin64'), (Join-Path $RtiDir 'bin'))
# The licence was resolved and pinned at the Stage 0 banner ($LicInfo). The Machine scope
# is deliberately NOT read here: it still names the pre-2026-09-14 file (RUNBOOK 0.5.15).

Say-Head 'Planned run'
Say ('  run id      : {0}' -f $RunId)
Say ('  run dir     : {0}' -f $RunDir)
Say ('  observers   : {0}s CAP (derived {8}: preRoll {1} + appJoin {2} + initDispatch {3} + oracleGate {4} + pushOrderListen {5} + run {6} + trail {7}{9})' -f `
        $EffWatchSecs, $PreRollSecs, $AppJoinTimeoutSec, $InitDispatchWaitSec, $OracleGateTimeoutSec, $PushOrderListenSec, $RunSecs, $TrailSecs, `
        $DerivedWatchSecs, ($(if ($PreOrderGateOn) { (' + preOrderGate {0}' -f $PreOrderGateTimeoutSec) } else { '' }) +
                            $(if ($PreOrderSettleSecs -gt 0) { (' + preOrderSettle {0}' -f $PreOrderSettleSecs) } else { '' })))
Say ('  trace stop  : {0} - {1}' -f $TraceStopMode, $(if ($TraceStopMode -eq 'stop-file') { ('teardown touches {0} at StopIface + {1}s; observers resign within ~1 s; grace {2}s' -f $PathStopFile, $TrailSecs, $TraceStopGraceSec) } else { 'observers run to the CAP and teardown waits for them (pre-turnaround dead time)' }))
Say ('  pre-order   : {0}' -f $(
    if ($PreOrderGateOn) {
        ('stage 7d READY GATE -PreOrderGate {0}: PushOrder waits for the simulator''s own "New Primary nav area" row (object console {1}), timeout {2}s, which IS in the derived cap; {3}' -f `
            $PreOrderGate, $ObjectConsoleLevel, $PreOrderGateTimeoutSec, `
            $(if ($PreOrderSettleSecs -gt 0) { ('on TIMEOUT it falls back to the {0}s -PreOrderSettleSecs hold (FALLBACK ONLY - gate first, never both in sequence)' -f $PreOrderSettleSecs) } else { 'on TIMEOUT the run STOPS (exit 3) - pass -PreOrderSettleSecs N to make the timeout fall back to a fixed hold instead' }))
    } elseif ($PreOrderSettleSecs -gt 0) {
        ('stage 7d holds {0}s between the oracle gate and PushOrder (the nav area loads LAZILY after placement); it IS in the derived cap, and {1}' -f $PreOrderSettleSecs, $(if ($WatchSecs -gt 0) { 'the EXPLICIT -WatchSecs above overrides that derivation - see the flag' } else { 'the derived cap is the one in force' }))
    } else { 'no hold and no gate (-PreOrderSettleSecs 0, -PreOrderGate off)' }))
Say ('  window      : {0}s{1}' -f $RunSecs, $(if ($StopWhenComplete) { (' CAP; -StopWhenComplete closes it once all {0} taskee(s) and all {1} task(s) have a TERMINAL report (TASKCMPLT or TASKABRT), {2}s have passed AND every taskee has post-completion position evidence (RPT | C2SIM-capture | R1-applog)' -f $OrderTaskees.Count, $OrderTasks.Count, $SettleHoldSecs) } else { ' fixed (-StopWhenComplete not set)' }))
Say ('  clientId    : {0}' -f $(if ($ClientId) { ('{0} (-ClientId -> Vrf__ClientId)' -f $ClientId) } else { ('{0} (appsettings.json)' -f $appClientId) }))
Say ('  HLA PATH    : {0};<inherited>' -f $PathPrefix)
Say ('  licence     : {0}' -f $(if ($LicInfo.Exists) { ('{0} (expires {1})' -f $LicInfo.Path, $LicInfo.ExpiryText) } else { '(UNRESOLVED - checkout may hang; RUNBOOK 0.5.15)' }))
Say ('  HLA cwd     : {0}' -f $Bin64)
if ($Is52) {
    Say ('  profile env : {0} (EVERY child: launch, tools, observers, app)' -f (($ProfileEnv.Keys | ForEach-Object { '{0}={1}' -f $_, $ProfileEnv[$_] }) -join '  '))
    Say ('  app config  : Vrf__Federation="" Vrf__FedFileName="" Vrf__ConfigFileIdentity=true (FomModules CLEARED - config modules are ADDITIVE, DIFF row A9)')
    Say ('                Vrf__ConnectionConfigFile={0}{1}' -f $ConnConfigFile, $(if ($ConnConfigFromAppDataDir) { '  (from the RELOCATED -VrfAppDataDir tree - the one the sim reads)' } else { '  (vendor appData)' }))
    Say ('                Vrf__TypeMapFile={0}' -f $TypeMapFile52)
    Say ('                Vrf__DeviceAddress={0}' -f $(if ($DeviceAddressPassed) { $DeviceAddress52 } else { '(NOT SET - -DeviceAddress is empty; the app keeps VrfFacade''s 127.0.0.1)' }))
    Say ('  RTI         : rtiexec mode on {0}, rid {1}' -f $RtiDir, (Split-Path -Leaf $RidFile))
    Say ('                Stage 2r ensures a headless rtiexec is LISTENING on TCP 4001 before anything joins; it is never killed and outlives the run.')
}

# =============================================================================
# LIVE RUN
# =============================================================================
$RunnerExit          = 0
$WatchProc           = $null
$ListenProc          = $null
$AppProc             = $null
$VrfLaunched         = $false
$AppStarted          = $false
$SavedPath           = $env:PATH
$SavedLicense        = $env:MAKLMGRD_LICENSE_FILE
$SavedVrfAppNumber   = $env:Vrf__ApplicationNumber
$SavedC2SimRestUrl   = $env:C2SIM__RestUrl
$SavedC2SimStompUrl  = $env:C2SIM__StompUrl
$LedgerAdvanced      = $false
# Stage 2h (STP-825). Initialised BEFORE the try so the teardown inventory and the outer
# finally can read them on EVERY exit path, including an abort that never reaches the stage
# (Set-StrictMode -Version Latest turns an unset variable into a terminating error - the
# exact defect check 8h pins for $script:RunnerLockTaken).
$script:RtiExecLogPath        = ''
$script:RtiExecServingPid     = $null
$script:FederationHolderPids  = @()
# The profile's own environment, and what it displaced. Both maps are EMPTY on 5.0.2,
# so every loop over them below is a no-op there. Restored in the finally beside PATH.
$SavedProfileEnv     = [ordered]@{}
foreach ($k in $ProfileEnv.Keys) { $SavedProfileEnv[$k] = [Environment]::GetEnvironmentVariable($k) }
# App-only config overrides (the Host maps '__' to ':'). On 5.2 the app must join the
# CONFIG-FILE way exactly as the tools do: empty Federation/FedFileName and NO FOM module
# list. An empty JSON array in a second settings file could NOT clear the three modules
# appsettings.json already declares (a later configuration provider cannot REMOVE keys),
# so the app gained one minimal switch, Vrf:ConfigFileIdentity, which clears all three in
# BuildStartupConfig (src/VrfC2SimApp/VrfSettings.cs). Default false = 5.0.2 unchanged.
$AppEnv52 = [ordered]@{}
if ($Is52) {
    $AppEnv52['Vrf__ConfigFileIdentity']  = 'true'
    $AppEnv52['Vrf__Federation']          = ''
    $AppEnv52['Vrf__FedFileName']         = ''
    $AppEnv52['Vrf__ConnectionConfigFile']= $ConnConfigFile
    $AppEnv52['Vrf__TypeMapFile']         = $TypeMapFile52
    # The interface address is an OVERRIDE only. With -DeviceAddress empty (the default) no
    # Vrf__DeviceAddress is set at all and the app keeps VrfFacade's own 127.0.0.1 - the
    # runner asserts nothing it has not tested. Setting it binds Vrf:DeviceAddress in
    # VrfSettings, which BuildStartupConfig passes to the bridge.
    if ($DeviceAddressPassed) { $AppEnv52['Vrf__DeviceAddress'] = $DeviceAddress52 }
}
$SavedAppEnv52 = [ordered]@{}
foreach ($k in $AppEnv52.Keys) { $SavedAppEnv52[$k] = [Environment]::GetEnvironmentVariable($k) }

function Stop-Runner {
    param([int]$Code, [string]$Reason)
    Add-Flag 'FAIL' $Reason
    $script:RunnerExit = $Code
    throw [System.OperationCanceledException]::new($Reason)
}

try {
    if ($DryRun) {
        Say-Head 'DRY RUN - the full planned sequence, in order. NOTHING below is executed.'
        Say-Plan ('would create the run directory {0}' -f $RunDir)
        Say-Plan ('would write the run-directory pointer {0} (scripts\RunScenario.sh reads it instead of guessing by mtime)' -f (Join-Path $RepoRoot 'runs\launch52\last-run-dir.txt'))
        if ($PathConsoleDir) {
            Say-Plan ('would IGNORE -ConsoleLogDir ({0}). The --console-log-dir flag does NOT exist in tools/WatchVrf (native revert 5d14eda) and passing it would fail the oracle stage with exit 2. Nothing is created and nothing is passed.' -f $PathConsoleDir)
        }
        Say-Plan ('would REWRITE the Appendix B marker in {0}: {1} -> {2}, and append a CLAIMED block for {3} numbers.' -f $LedgerDoc, $FirstFree, $NextFree, $Alloc.Count)
        Say-Plan ('would set, for HLA child processes only: PATH="{0};<inherited>", Vrf__ApplicationNumber={1} (MAKLMGRD_LICENSE_FILE is ALREADY pinned - Stage 0 resolved it from the registry)' -f $PathPrefix, $AppNo['app'])
        foreach ($k in $ProfileEnv.Keys) { Say-Plan ('would set, for ALL children (profile {0}): {1}={2}' -f $VrfProfile, $k, $ProfileEnv[$k]) }
        foreach ($k in $AppEnv52.Keys)   { Say-Plan ('would set, for the app only (profile {0}): {1}={2}' -f $VrfProfile, $k, $(if ($AppEnv52[$k] -eq '') { '(empty)' } else { $AppEnv52[$k] })) }
        Say ''
    } else {
        New-Item -ItemType Directory -Path $RunDir -Force | Out-Null
        Say-Ok ('run directory created: {0}' -f $RunDir)
        # THE RUN-DIRECTORY POINTER (review of 374ea49, finding F10). scripts\RunScenario.sh
        # used to find the run directory by MTIME (`ls -1dt runs/*_run | head -1`), which names
        # the NEWEST directory - not necessarily the one it launched - and any later write into
        # another run directory (the watchdog's own runner.watchdog-ran, for one) can flip that
        # ordering. The wrapper reads this file instead. It DELETES it before launching, so a
        # pointer found afterwards is this run's or it is not there at all.
        try {
            $PointerDir = Join-Path $RepoRoot 'runs\launch52'
            if (-not (Test-Path -LiteralPath $PointerDir -PathType Container)) {
                New-Item -ItemType Directory -Path $PointerDir -Force | Out-Null
            }
            [System.IO.File]::WriteAllText((Join-Path $PointerDir 'last-run-dir.txt'), ($RunDir + "`r`n"))
            Say-Ok ('run-directory pointer written: {0}' -f (Join-Path $PointerDir 'last-run-dir.txt'))
        } catch {
            Say-Warn ('could not write the run-directory pointer ({0}). scripts\RunScenario.sh falls back to its mtime scan, which can name the wrong directory.' -f $_.Exception.Message)
        }
        Save-Manifest

        Update-Ledger -From $FirstFree -To $NextFree -RunId $RunId -Allocation $Alloc
        $LedgerAdvanced = $true
        $Manifest.ledger.advanced = $true
        Say-Ok ('Appendix B marker advanced {0} -> {1} and {2} numbers CLAIMED, BEFORE any join' -f $FirstFree, $NextFree, $Alloc.Count)
        Save-Manifest

        $env:PATH = ('{0};{1}' -f $PathPrefix, $SavedPath)
        # The profile environment goes on THIS process, so every child inherits the same
        # stack roots and the same rid. No-op on 5.0.2 (the map is empty).
        foreach ($k in $ProfileEnv.Keys) {
            Set-Item -Path ('Env:' + $k) -Value ([string]$ProfileEnv[$k])
            Say-Ok ('profile env set for all children: {0}={1}' -f $k, $ProfileEnv[$k])
        }
        if ($LicInfo.Exists) {
            $env:MAKLMGRD_LICENSE_FILE = $LicInfo.Path
            Say-Ok ('MAKLMGRD_LICENSE_FILE pinned for every child = {0} (expires {1})' -f $LicInfo.Path, $LicInfo.ExpiryText)
        } elseif (-not [string]::IsNullOrWhiteSpace($SavedLicense)) {
            Say-Warn ('the registry licence path did not resolve to an existing file - PRESERVING the inherited process value ({0}) rather than blanking it (RUNBOOK 0.5.15).' -f $SavedLicense)
        } else {
            Add-Flag 'WARN' 'MAKLMGRD_LICENSE_FILE resolved to nothing in the User scope, the Machine scope AND this process - licence checkout may hang (RUNBOOK 0.5.15).'
        }
    }

    # ---------------------------------------------------------------------
    # STAGE 2c - RTI READINESS GATE (C1) - FATAL, BEFORE ANY BACK-END LAUNCH
    # ---------------------------------------------------------------------
    # THE RUN-2 FIX (docs/RTI_LAUNCH_HARDENING_DESIGN.md, ADJUDICATION ADDENDUM
    # A2-A7). RUN 2 went VOID because the VR-Forces back-end lost a TCP race at
    # createFederationExecution against a fresh-booting RTI, and the runner then
    # pushed init/order at a back-end that never joined. This gate PROVES the RTI
    # can service a create-or-join RIGHT NOW - the exact operation the back-end will
    # do (VrfFacade::Start -> new DtExerciseConn(--execName <federation>),
    # VrfFacade.cpp:319-325) - BEFORE Stage 3 launches VR-Forces, and REFUSES the
    # launch loudly if it cannot, so a not-ready RTI costs a launch cycle instead of
    # a confusing VOID. RtiProbe joins as a throwaway federate with INTERNAL
    # retry+backoff on ONE ledgered appNumber (absorbs a cold-create window) and
    # resigns cleanly. This is FATAL and PRE-launch; Stage 4 (post-launch WatchVrf
    # pre-check) is KEPT as-is and stays ADVISORY - they test different moments.
    # Stage 2b - ARM THE BOOT-DIALOG WATCHER (2026-09-01; RUNBOOK 0.5.3/0.5.4).
    # The "Choose RTI Connection" dialog is ONCE PER REBOOT: with no answered
    # rtiAssistant, the FIRST federate contact (RtiProbe, below) raises it and blocks
    # behind it - exactly what voided run 20260901T183422Z for 625 s while the
    # pre-flight had only WARNED. This watcher runs AnswerRtiDialog.ps1 every 5 s for
    # up to 150 s in the background: it clicks ONLY inside that exact dialog, exits
    # the loop the moment the dialog is answered, and is a no-op when no dialog ever
    # appears (the normal answered-assistant case). Best-effort by design - it never
    # fails the run; Stage 2c's gate remains the serviceability authority.
    $AnswerDialogScript = Join-Path $PSScriptRoot 'AnswerRtiDialog.ps1'
    if ($Is52) {
        # RETIRED on this profile (DIFF row A12, PREREG_52_LAUNCH_2026-09-03.md): every
        # federate here runs with RTI_ASSISTANT_DISABLE and takes its connection from the
        # rid, so no assistant is consulted and the dialog cannot be raised. Arming the
        # watcher would also be futile against an ELEVATED assistant, whose windows a
        # non-elevated session can neither see nor click.
        Say-Ok 'Stage 2b: boot-dialog watcher NOT armed - the 5.2 profile is assistant-free (RTI_ASSISTANT_DISABLE + rid-configured connection), so no "Choose RTI Connection" dialog can occur.'
    } elseif ($DryRun) {
        Say-Plan 'Stage 2b: would arm the boot-dialog watcher (AnswerRtiDialog.ps1 every 5s for up to 150s, background, best-effort)'
    } elseif (Test-Path -LiteralPath $AnswerDialogScript) {
        $watchCmd = ('for ($i=0; $i -lt 30; $i++) {{ & ''{0}'' *> ''{1}''; if ($LASTEXITCODE -eq 0) {{ break }}; Start-Sleep -Seconds 5 }}' -f `
                        $AnswerDialogScript, (Join-Path $RunDir 'answer-rti-dialog.log'))
        $null = Start-Process -FilePath 'pwsh' -ArgumentList @('-NoProfile','-Command', $watchCmd) -WindowStyle Hidden -PassThru
        Say-Ok 'Stage 2b: boot-dialog watcher armed (background, best-effort; log: answer-rti-dialog.log)'
    } else {
        Say-Warn ('Stage 2b: AnswerRtiDialog.ps1 not found at {0} - the once-per-reboot dialog would block Stage 2c unanswered.' -f $AnswerDialogScript)
    }

    # ---------------------------------------------------------------------
    # STAGE 2r - HEADLESS rtiexec (5.2 PROFILE ONLY) - FATAL, BEFORE STAGE 2c
    # ---------------------------------------------------------------------
    # THE RULING (UG52 5.5.1 p190: "You cannot use the MAK RTI in lightweight mode with
    # VR-Forces"; PREREG_52_RTIEXEC_2026-09-04: with the documented posture the observer
    # reflects 62 entities, with any lightweight posture 0). This stage ENSURES a headless
    # MAK RTI 5.0.1 rtiexec is up on the rendezvous port for the rid every federate in this
    # run shares, and it runs BEFORE Stage 2c: RtiProbe's create-or-join is the first thing
    # that needs the rendezvous, so a missing rtiexec must be fixed before, not diagnosed
    # after. It NEVER kills or restarts an rtiexec/rtiForwarder/rtiAssistant (RUNBOOK 0.5.2);
    # when one is already up it reports READY and touches nothing. RTI INFRASTRUCTURE
    # PERSISTS ACROSS RUNS BY DESIGN - teardown does not bring it down, so on a machine that
    # has run once since boot this stage is a no-op that costs a second.
    # The rtiexec's pid and the pid of the rtiForwarder it starts are recorded in the
    # manifest: they are NOT this run's processes, and a later run reading the manifest must
    # be able to tell whether it inherited the same rendezvous or a different one.
    if ($Is52) {
        Say-Head 'Stage 2r - headless MAK RTI 5.0.1 rtiexec (5.2 profile) - ensure-up, never restarted'
        Say '  UG52 5.5.1 p190: the MAK RTI in LIGHTWEIGHT mode is not supported with VR-Forces, and every'
        Say '  lightweight 5.2 run reflected ZERO entities. exit 0 = a rid-configured rtiexec is LISTENING on'
        Say '  TCP 4001 (already up, or started here); 2 = precondition/args (nothing started); 3 = not'
        Say '  listening within the timeout -> the launch is REFUSED before anything joins.'
        $rtiExecTimeoutSec = 30 + $StageTimeoutSec
        # -InterfaceAddress is deliberately NOT passed: the rtiexec's interface is the
        # RTI-LAYER pin, fixed at 127.0.0.1 by the loopback-broadcast connection the rid
        # describes, and it has nothing to do with -DeviceAddress (the VR-Forces layer).
        # Coupling the two would hand the RTI a blank address whenever -DeviceAddress keeps
        # its empty default. StartRtiExec52's own default and the rid must agree, which is
        # why neither is derived from the other here.
        $r = Invoke-External -Name 'StartRtiExec52' -File 'pwsh' `
                -Arguments @('-NoProfile','-File', $StartRtiExec,
                             '-RtiDir', $RtiDir, '-RidFile', $RidFile,
                             '-VrfRoot', $VrfRoot, '-VrLinkRoot', $VrLinkRoot,
                             '-LogDir', $RtiExecLogDir) `
                -Cwd $RepoRoot -StdOutFile $PathRtiExecOut -StdErrFile $PathRtiExecErr `
                -TimeoutSec $rtiExecTimeoutSec `
                -Note 'ENSURE-UP, never a restart. exit 0 READY (already up = nothing touched; or started and LISTENING); 2 precondition/usage; 3 not listening within the timeout. The rtiexec and the rtiForwarder it spawns OUTLIVE this run by design and are never killed by teardown.'
        if (-not $DryRun) {
            if (-not (Test-StageProduced -Result $r)) { Stop-Runner 3 (Get-StageFailureText -Name 'StartRtiExec52' -Result $r) }
            # The pids come from the stage's own marker line (it filters by image path, which
            # this runner cannot do for an elevated process), with a Get-Process fallback so a
            # changed banner degrades to a coarser answer instead of an empty manifest field.
            $rtiExecPid = $null; $rtiFwdPid = $null; $rtiStarted = $null; $rtiLog = $null
            if (Test-Path -LiteralPath $PathRtiExecOut -PathType Leaf) {
                # log= is last and may contain spaces, so it runs to end of line.
                $m = [regex]::Match((Get-Content -LiteralPath $PathRtiExecOut -Raw),
                                    'RTIEXEC READY rtiexec=(\d+|none) forwarder=(\d+|none) tcp=\S+ started=(yes|no) log=(.*?)\s*$',
                                    [System.Text.RegularExpressions.RegexOptions]::Multiline)
                if ($m.Success) {
                    if ($m.Groups[1].Value -ne 'none') { $rtiExecPid = [int]$m.Groups[1].Value }
                    if ($m.Groups[2].Value -ne 'none') { $rtiFwdPid  = [int]$m.Groups[2].Value }
                    $rtiStarted = ($m.Groups[3].Value -eq 'yes')
                    $rtiLog     = $m.Groups[4].Value
                }
            }
            if ($null -eq $rtiExecPid) { $rtiExecPid = @(Get-Process -Name 'rtiexec'      -ErrorAction SilentlyContinue | ForEach-Object { $_.Id }) | Select-Object -First 1 }
            if ($null -eq $rtiFwdPid)  { $rtiFwdPid  = @(Get-Process -Name 'rtiForwarder' -ErrorAction SilentlyContinue | ForEach-Object { $_.Id }) | Select-Object -First 1 }
            $Manifest.inputs.vrfProfile.rtiExec.exitCode     = $r.ExitCode
            $Manifest.inputs.vrfProfile.rtiExec.rtiExecPid   = $rtiExecPid
            $Manifest.inputs.vrfProfile.rtiExec.forwarderPid = $rtiFwdPid
            $Manifest.inputs.vrfProfile.rtiExec.started      = $rtiStarted
            $Manifest.inputs.vrfProfile.rtiExec.logFile      = $rtiLog
            # Stage 2h reads THIS rtiexec's log to see the holder's join, so resolve the file
            # once, here, where the stage's own pids and marker line are in hand. log= is a
            # real path only when this run STARTED the rtiexec; on the ordinary
            # already-up path it is prose, and the file has to be found by the serving pid.
            $script:RtiExecServingPid = $rtiExecPid
            $script:RtiExecLogPath    = Get-RtiExecLogPath -Recorded ([string]$rtiLog) -LogDir $RtiExecLogDir -RtiExecPid $rtiExecPid
            Save-Manifest
            switch ($r.ExitCode) {
                0 { Say-Ok ('rtiexec READY (pid {0}, forwarder {1}, started-by-this-run={2}). It is NEVER torn down - the next run will find it.' -f $rtiExecPid, $rtiFwdPid, $rtiStarted) }
                2 { Stop-Runner 2 ('StartRtiExec52 exited 2 (precondition/argument failure). Its arguments are GENERATED by this runner, so a usage failure is a RUNNER DEFECT. Nothing was started - see {0}.' -f $PathRtiExecOut) }
                3 { Stop-Runner 3 ('StartRtiExec52 exited 3: no rid-configured rtiexec is LISTENING on TCP 4001. Without it the federation would fall back to the LIGHTWEIGHT mode UG52 5.5.1 p190 says is not supported with VR-Forces, and every observer would reflect 0 (PREREG_52_RTIEXEC_2026-09-04). REFUSING the launch before anything joins. Nothing was killed - read {0} and the newest rtiexec_*.log in {1}.' -f $PathRtiExecOut, $RtiExecLogDir) }
                default { Stop-Runner 3 ('StartRtiExec52 exited {0} - undocumented code. Refusing to launch on an uninterpretable gate result.' -f $r.ExitCode) }
            }
        }
    }

    # ---------------------------------------------------------------------
    # STAGE 2h - FEDERATION HOLDER (5.2 PROFILE ONLY) - FATAL, BEFORE STAGE 2c
    # ---------------------------------------------------------------------
    # STP-825 (2026-09-15). WHAT GOES WRONG WITHOUT IT. On this profile the SIM ends up
    # being the federation's CREATOR at Stage 3: Stage 2c's RtiProbe creates MAK-ONE-2025,
    # joins it, resigns - and, being the last federate in it, DESTROYS it on the way out.
    # Since 14:23Z on 2026-09-15 rtiexec 5.0.1 rejects a CREATOR's FOM-module distribution
    # INTERMITTENTLY ("Failed to process FOM file <module> when creating federation
    # MAK-ONE-2025, FOM Reader reports ...", "Sending Create Response = Error"), so the
    # sim's create fails and it DIES AT STARTUP (LaunchVrf exit 3); a failed create also
    # crashes the creating process (STP-832). JOINS HAVE NEVER FAILED - not once, in any
    # run in the record.
    #
    # THE REPAIR: make sure the federation ALREADY EXISTS before the sim asks. This stage
    # starts a HOLDER federate - RtiProbe with maxAttempts 1 and a LONG settle - which
    # creates-or-joins the federation and then STAYS JOINED for -FederationHoldSecs. Stage
    # 2c then JOINS instead of creating, and its resign no longer destroys the federation
    # because the holder is still in it; the sim's own create returns "already exists" and
    # it JOINS too. Confirmed live with exactly this holder started by hand: runs
    # 20260915T151959Z and 20260915T160253Z.
    #
    # THE HOLDER IS A JOINED FEDERATE, and two consequences are not negotiable:
    #   * it is started DETACHED (its own hidden console plus a file for stdin, exactly like
    #     the stage-6b watchdog) because it MUST outlive this runner - the hold is a
    #     WALL-CLOCK timer, not a run length; and
    #   * teardown NEVER touches it and never waits for it. It resigns by itself when the
    #     settle expires. Force-killing a federate strands it and the next start hangs at
    #     RTI join (RUNBOOK sec 0). Its own destroy attempt after resigning fails
    #     harmlessly - by then the sim is joined, so it is not the last one out.
    #
    # COST: one ledgered appNumber PER ATTEMPT (see the allocation block for why a retry
    # must not reuse one). -FederationHoldSecs 0 disables the stage completely and restores
    # the pre-STP-825 behaviour: no stage, no appNumbers, no output.
    if ($FederationHoldOn) {
        Say-Head 'Stage 2h - federation HOLDER (STP-825) - FATAL, before Stage 2c'
        Say ('  A holder federate creates-or-joins {0} and STAYS JOINED for {1}s, so neither Stage 2c nor' -f $FederationHoldName, $FederationHoldSecs)
        Say '  the SIM ever has to CREATE the federation - and the CREATE is the operation rtiexec 5.0.1'
        Say '  rejects intermittently ("Failed to process FOM file <module> ... Sending Create Response = Error").'
        Say '  Joins have never failed. The sim will then log "already exists" followed by "Joined federation".'
        Say ('  Up to {0} attempt(s), ONE ledgered appNumber each; join is read from the rtiexec log, not from' -f $FederationHoldAttempts)
        Say ('  the holder''s stdout (RtiProbe prints "created/joined" only AFTER the {0}s hold).' -f $FederationHoldSecs)
        Say '  The holder is DETACHED, OUTLIVES this run, is NEVER killed by teardown and is never waited for.'
        Say ('  federation identity: {0}' -f $FederationHoldNameSource)

        $holderExeArgs1 = @([string]$(if ($FedHoldAppNos.Count -gt 0) { $FedHoldAppNos[0] } else { 0 }),
                            $FederationHoldName, '1', [string]$FederationHoldSecs, '3')
        $holderNote = ('STP-825 federation holder: create-or-join and HOLD. maxAttempts 1 (a retry is a NEW appNumber, by design), settle = the hold, backoff 3. It RESIGNS ON ITS OWN TIMER and is NEVER killed or waited for - teardown leaves it alone (RUNBOOK sec 0). Its later destroy attempt fails harmlessly because the sim is joined by then.')

        if ($DryRun) {
            Say-Plan ('would start the federation holder RtiProbe.exe <appNo> {0} 1 {1} 3 DETACHED (own hidden console), cwd {2}, under the 5.2 profile env + PATH prefix + RTI_RID_FILE + RTI_ASSISTANT_DISABLE - exactly as Stage 2c' -f $FederationHoldName, $FederationHoldSecs, $Bin64)
            Say-Plan ('would allocate {0} appNumbers for it, ONE PER ATTEMPT ({1}) - a dry run consumes NONE and the marker is not advanced' -f $FederationHoldAttempts, ($FedHoldAppNos -join ','))
            Say-Plan ('would wait up to {0}s per attempt for the rtiexec log line: Federate remoteControl <holderPid> ... has joined federation "{1}"' -f $FederationHoldJoinWaitSec, $FederationHoldName)
            Say-Plan ('would write holder.<attempt>.stdout.log / holder.<attempt>.stderr.log and the empty stdin file {0} into the run directory' -f $PathHolderStdIn)
            Say-Plan ('would FAIL the run (Stop-Runner, STP-825) if no attempt joins after {0} tries, naming the rejected FOM module' -f $FederationHoldAttempts)
            Say-Plan 'would LEAVE the holder joined at teardown and NOT wait for it - it is a federate on a wall-clock hold, named in the post-teardown inventory as EXPECTED'
            $null = Start-External -Name 'FederationHolder-1' -File $ExeRtiProbe -Arguments $holderExeArgs1 `
                        -Cwd $Bin64 -StdOutFile (Join-Path $RunDir 'holder.1.stdout.log') `
                        -StdErrFile (Join-Path $RunDir 'holder.1.stderr.log') `
                        -StdInFile $PathHolderStdIn -NewConsole -Note $holderNote
        } else {
            try { [System.IO.File]::WriteAllText($PathHolderStdIn, '') }
            catch { Say-Warn ('could not create {0}: {1}. The holder will inherit this runner''s stdin instead.' -f $PathHolderStdIn, $_.Exception.Message) }

            $holderLog = $script:RtiExecLogPath
            if ($holderLog) {
                Say-Ok ('join will be read from the rtiexec log: {0}' -f $holderLog)
            } else {
                Add-Flag 'WARN' ('Stage 2h could not resolve the serving rtiexec''s log file (Stage 2r recorded none and no rtiexec_*-<pid>.log was found in {0}). The join can then only be confirmed from the holder''s OWN stdout, which RtiProbe writes AFTER its {1}s hold - so with the default hold this stage will report the join UNCONFIRMED even when it succeeded.' -f $RtiExecLogDir, $FederationHoldSecs)
            }

            $holderJoined     = $false
            $holderPid        = $null
            $holderAppNo      = $null
            $holderJoinedUtc  = $null
            $holderWaitedSec  = 0
            $holderEvidence   = ''
            $holderAttemptLog = @()
            $holderUsed       = 0

            for ($a = 1; $a -le $FederationHoldAttempts -and -not $holderJoined; $a++) {
                $holderUsed = $a
                $hAppNo = $FedHoldAppNos[$a - 1]
                $hOut   = Join-Path $RunDir ('holder.{0}.stdout.log' -f $a)
                $hErr   = Join-Path $RunDir ('holder.{0}.stderr.log' -f $a)
                # Byte offset BEFORE the holder exists, so the scan can never match another
                # federate's earlier join line - the pid filter already makes that unlikely,
                # and pids are reused on Windows, which makes "unlikely" not good enough.
                [long]$hOffset = 0
                if ($holderLog) { try { $hOffset = (Get-Item -LiteralPath $holderLog).Length } catch { $hOffset = 0 } }

                $hProc = $null
                try {
                    $hProc = Start-External -Name ('FederationHolder-{0}' -f $a) -File $ExeRtiProbe `
                                -Arguments @([string]$hAppNo, $FederationHoldName, '1', [string]$FederationHoldSecs, '3') `
                                -Cwd $Bin64 -StdOutFile $hOut -StdErrFile $hErr `
                                -StdInFile $(if (Test-Path -LiteralPath $PathHolderStdIn) { $PathHolderStdIn } else { '' }) `
                                -NewConsole -Note $holderNote
                } catch {
                    $hProc = $null
                    Say-Warn ('Stage 2h attempt {0}: the holder could not be started: {1}' -f $a, $_.Exception.Message)
                }
                if ($null -eq $hProc) {
                    # SAME RECORD SHAPE as every other attempt (waitedSec included), and
                    # $holderWaitedSec is reset so the manifest's joinWaitedSec can never
                    # report a PREVIOUS attempt's wait for one that never started.
                    $holderWaitedSec = 0
                    $holderAttemptLog += [ordered]@{ attempt = $a; appNumber = $hAppNo; processId = $null; joined = $false; exited = $true; exitCode = $null; fomModule = ''; stdout = $hOut; waitedSec = 0; atUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ') }
                    continue
                }
                $script:FederationHolderPids += $hProc.Id
                $hJoinRe = ('remoteControl\s+{0}\b.*has joined federation "{1}"' -f $hProc.Id, [regex]::Escape($FederationHoldName))
                Say-Info ('Stage 2h attempt {0}/{1}: holder pid {2} on appNumber {3}; waiting up to {4}s for its join.' -f $a, $FederationHoldAttempts, $hProc.Id, $hAppNo, $FederationHoldJoinWaitSec)

                $hStart = Get-Date
                while (((Get-Date) - $hStart).TotalSeconds -lt $FederationHoldJoinWaitSec) {
                    Start-Sleep -Seconds 1
                    if ($holderLog) {
                        if ((Read-TextFromOffset -Path $holderLog -Offset $hOffset) -match $hJoinRe) {
                            $holderJoined   = $true
                            $holderEvidence = ('rtiexec log: remoteControl {0} has joined federation "{1}"' -f $hProc.Id, $FederationHoldName)
                            break
                        }
                    } elseif ((Read-LiveText -Path $hOut) -match 'created/joined') {
                        # The documented fallback when the rtiexec log path is unknown. It can
                        # only fire for a hold SHORTER than the wait (RtiProbe prints that line
                        # after the settle) - it is here so a short-hold probe run is still
                        # confirmable, not because it can answer the default 900 s case.
                        $holderJoined   = $true
                        $holderEvidence = ('holder stdout: "created/joined" in {0} (rtiexec log path unknown)' -f $hOut)
                        break
                    }
                    if ($hProc.HasExited) { break }
                }
                $holderWaitedSec = [int]((Get-Date) - $hStart).TotalSeconds

                $hExited = $hProc.HasExited
                $hCode   = $null
                if ($hExited) { try { $hCode = $hProc.ExitCode } catch { $hCode = $null } }
                # The rtiexec's own account of WHY a create was refused, when it refused one.
                $hFom = ''
                if ($holderLog) {
                    $hFomM = [regex]::Matches((Read-TextFromOffset -Path $holderLog -Offset $hOffset), 'Failed to process FOM file (\S+)')
                    if ($hFomM.Count -gt 0) { $hFom = $hFomM[$hFomM.Count - 1].Groups[1].Value }
                }
                $holderAttemptLog += [ordered]@{
                    attempt   = $a
                    appNumber = $hAppNo
                    processId = $hProc.Id
                    joined    = $holderJoined
                    exited    = [bool]$hExited
                    exitCode  = $hCode
                    fomModule = $hFom
                    stdout    = $hOut
                    waitedSec = $holderWaitedSec
                    atUtc     = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                }

                if ($holderJoined) {
                    $holderPid       = $hProc.Id
                    $holderAppNo     = $hAppNo
                    $holderJoinedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                    Say-Ok ('federation HELD: holder pid {0} (appNumber {1}) joined {2} in {3}s on attempt {4}/{5}. {6}' -f `
                                $holderPid, $holderAppNo, $FederationHoldName, $holderWaitedSec, $a, $FederationHoldAttempts, $holderEvidence)
                    Say-Ok ('  It stays joined for {0}s from now, so Stage 2c JOINS and the sim''s own create returns "already exists". It is NEVER killed.' -f $FederationHoldSecs)
                } else {
                    Say-Warn ('Stage 2h attempt {0}/{1} FAILED: holder pid {2} on appNumber {3} did not join within {4}s (exited={5}, exitCode={6}{7}).' -f `
                                $a, $FederationHoldAttempts, $hProc.Id, $hAppNo, $holderWaitedSec, $hExited, $hCode, `
                                $(if ($hFom) { (', rtiexec: Failed to process FOM file ' + $hFom) } else { '' }))
                    if (-not $hExited) {
                        Add-Flag 'WARN' ('Stage 2h attempt {0}: holder pid {1} did NOT join within {2}s but is STILL RUNNING. It was NOT killed (RUNBOOK sec 0 - it may be a joined federate this runner simply could not see). The next attempt starts a SECOND holder on a fresh appNumber; both resign on their own {3}s timers.' -f $a, $hProc.Id, $holderWaitedSec, $FederationHoldSecs)
                    }
                }
            }

            $Manifest.inputs.vrfProfile.federationHolder.appNumber     = $holderAppNo
            $Manifest.inputs.vrfProfile.federationHolder.processId     = $holderPid
            $Manifest.inputs.vrfProfile.federationHolder.attemptsUsed  = $holderUsed
            $Manifest.inputs.vrfProfile.federationHolder.joinedUtc     = $holderJoinedUtc
            $Manifest.inputs.vrfProfile.federationHolder.joinWaitedSec = $holderWaitedSec
            $Manifest.inputs.vrfProfile.federationHolder.rtiExecLog    = $holderLog
            $Manifest.inputs.vrfProfile.federationHolder.joinEvidence  = $holderEvidence
            $Manifest.inputs.vrfProfile.federationHolder.attempts      = $holderAttemptLog
            Save-Manifest

            if (-not $holderJoined) {
                $hFoms = @($holderAttemptLog | ForEach-Object { $_.fomModule } | Where-Object { $_ } | Select-Object -Unique)
                Stop-Runner 3 ('STP-825: the federation HOLDER could not join {0} after {1} attempt(s) on appNumbers {2}. Without a holder the SIM becomes the federation CREATOR at Stage 3, and rtiexec 5.0.1 is currently rejecting creator FOM distribution{3} - the sim would die at startup (LaunchVrf exit 3) after a full launch cycle, and a failed create also crashes the creating process (STP-832). REFUSING THE LAUNCH here instead, before any back end is started. Read {4} and the rtiexec log {5}. Nothing was killed; any holder process still alive is a federate and resigns on its own {6}s timer. To run WITHOUT the holder (the pre-STP-825 behaviour, which is the failure mode): -FederationHoldSecs 0.' -f `
                    $FederationHoldName, $FederationHoldAttempts, ($FedHoldAppNos -join ','), `
                    $(if ($hFoms.Count -gt 0) { (' (modules refused this run: ' + ($hFoms -join ', ') + ')') } else { '' }), `
                    (Join-Path $RunDir 'holder.1.stdout.log'), $(if ($holderLog) { $holderLog } else { $RtiExecLogDir }), $FederationHoldSecs)
            }
        }
    } elseif ($Is52) {
        Say-Head 'Stage 2h - federation HOLDER (STP-825)'
        Say-Warn '-FederationHoldSecs 0: the holder is DISABLED, so the SIM becomes the federation CREATOR at Stage 3.'
        Say-Warn '  That is the STP-825 failure mode: rtiexec 5.0.1 rejects a creator''s FOM-module distribution'
        Say-Warn '  intermittently, and the sim then dies at startup (LaunchVrf exit 3) after a full launch cycle.'
        Say-Warn '  This is the pre-2026-09-15 behaviour and it is kept only as the single-variable control.'
        Add-Flag 'WARN' 'Stage 2h federation holder DISABLED (-FederationHoldSecs 0). The sim will CREATE the federation, which is the STP-825 failure mode.'
    }

    Say-Head 'Stage 2c - RTI readiness gate (C1) - FATAL, before any back-end launch'
    Say '  Proves the RTI can service a create-or-join NOW (the RUN-2 fix). RtiProbe joins the'
    Say '  federation as a throwaway federate with internal retry+backoff, then resigns cleanly.'
    Say '  exit 0 = serviceable -> proceed; 1 = NOT serviceable -> launch REFUSED here, before any'
    Say '  back-end is started or any init is pushed; 2 = arg/usage (runner defect).'
    $probeAttempts = 5
    $probeSettle   = 2
    $probeBackoff  = 3
    # Ceiling >= worst-case internal duration + slack: the definite internal sleeps are
    # attempts*(settle+backoff); Start()'s own per-attempt cost is absorbed by the large
    # $StageTimeoutSec slack, so a slow-but-eventually-ready RTI is never cut off early.
    $probeTimeoutSec = $probeAttempts * ($probeSettle + $probeBackoff) + $StageTimeoutSec
    # CONDITIONAL ARGUMENT LIST (RUNBOOK 0.5.14 item 15 addendum, 2026-09-15). RtiProbe
    # takes federation as its SECOND positional, ahead of the three retry-tuning numbers -
    # the one call site among all of $FederationArg's users where an empty middle argument
    # is not harmless. RtiProbe.exe has no named flags (tools/RtiProbe/Program.cs), so the
    # only way to "omit" federation without shifting maxAttempts/settleSecs/backoffSecs
    # left is to omit federation AND those three together, relying on RtiProbe's own
    # defaults - which $probeAttempts/$probeSettle/$probeBackoff above are already set to
    # match exactly (5/2/3; tools/RtiProbe/Program.cs, UsageLines()), so omitting all four
    # changes NOTHING about what RtiProbe does, only which slot is or is not on the command
    # line. Positional arguments stay aligned on both branches. [string[]] on the
    # assignment (not just Invoke-External's own [string[]] parameter) so $RtiProbeArgs
    # is a real one-element array even in the empty branch - PowerShell's if/else-as-
    # expression enumerates a single-element @(...) result back down to a bare string
    # otherwise (verified: an untyped assignment here gives type String, not String[]).
    [string[]]$RtiProbeArgs = if ([string]::IsNullOrEmpty($FederationArg)) {
        @([string]$AppNo['rtiProbe'])
    } else {
        @([string]$AppNo['rtiProbe'], $FederationArg, [string]$probeAttempts, [string]$probeSettle, [string]$probeBackoff)
    }
    $r = Invoke-External -Name 'RtiProbe' -File $ExeRtiProbe `
            -Arguments $RtiProbeArgs `
            -Cwd $Bin64 -StdOutFile $PathRtiProbeOut -StdErrFile $PathRtiProbeErr `
            -TimeoutSec $probeTimeoutSec `
            -Note 'C1 pre-launch FATAL gate (the RUN-2 fix). RtiProbe exit 0 = RTI serviceable (create/join OK, clean resign) -> proceed; 1 = RTI NOT serviceable after all internal retries -> REFUSE the launch BEFORE any back-end/PushInit; 2 = arg/usage (args are generated, so a 2 is a RUNNER DEFECT). Self-resigns on every path. Uses ONE ledgered appNumber for all retries.'
    if (-not $DryRun) {
        # A gate that produced no exit code (timed out / could-not-start) is NOT a pass.
        # If it timed out it is a joined federate left running (never killed, RUNBOOK sec 0);
        # fail cleanly through teardown, exactly like Stage 3/4.
        if (-not (Test-StageProduced -Result $r)) { Stop-Runner 3 (Get-StageFailureText -Name 'RtiProbe' -Result $r) }
        $Manifest.oracle.rtiGate = [ordered]@{
            fatal      = $true
            appNumber  = $AppNo['rtiProbe']
            exitCode   = $r.ExitCode
            maxAttempts= $probeAttempts
            settleSecs = $probeSettle
            backoffSecs= $probeBackoff
        }
        Save-Manifest
        switch ($r.ExitCode) {
            0 { Say-Ok 'RTI serviceable - create/join succeeded and resigned cleanly. Clear to launch.' }
            1 { Stop-Runner 3 ('RtiProbe exited 1: the RTI could not be proven serviceable (create/join failed on every retry, or it joined but could not resign cleanly - see {0}). REFUSING THE LAUNCH before any back-end is started or any init is pushed - this is the RUN-2 fix (do NOT proceed to a confusing VOID). Bring the RTI to a confirmed-ready state (warm, resident, answered rtiAssistant - RUNBOOK 0.5.x / RTI_LAUNCH_HARDENING_DESIGN.md) and re-run. Nothing was launched.' -f $PathRtiProbeOut) }
            2 { Stop-Runner 2 'RtiProbe exited 2 (usage/argument error). Its arguments are GENERATED by this runner, so this is a RUNNER DEFECT, not an RTI problem. Nothing joined, nothing launched.' }
            default { Stop-Runner 3 ('RtiProbe exited {0} - undocumented code. Refusing to launch on an uninterpretable gate result.' -f $r.ExitCode) }
        }
    }

    # ---------------------------------------------------------------------
    # STAGE 3 - bring VR-Forces up
    # ---------------------------------------------------------------------
    Say-Head $(if ($Is52) { 'Stage 3 - LaunchVrf52.ps1 (5.2d INDEPENDENT mode, UG52 4.1.2)' } else { 'Stage 3 - LaunchVrf.ps1 (combined mode)' })
    $launchArgs = @(
        '-NoProfile','-File', $LaunchVrf,
        '-Scenario', $Scenario,
        '-VrfRoot', $VrfRoot,
        '-RtiDir', $RtiDir,
        '-BackendAppNumber',  [string]$AppNo['vrfBackend'],
        '-FrontendAppNumber', [string]$AppNo['vrfFrontend']
    )
    # 5.2 extras. -VrLinkRoot/-RidFile are passed EXPLICITLY even though LaunchVrf52
    # defaults to the same values: the launch command line is what the manifest records,
    # and the rid is the object every federate in this run has to share. -DeviceAddress
    # (RESEARCH_52_HLA_CONNECTION_CONFIG P3) is passed ONLY when non-empty: by default the sim
    # and the gui are launched WITHOUT --deviceAddress/--hostAddressString, which is the
    # sim-side arm still open after run 3857 closed the observer-side one. The flag is left
    # off the command line entirely rather than passed empty, so the launch line in the
    # evidence says plainly that nothing was pinned.
    if ($Is52) {
        $launchArgs += @('-VrLinkRoot', $VrLinkRoot, '-RidFile', $RidFile)
        if ($DeviceAddressPassed) { $launchArgs += @('-DeviceAddress', $DeviceAddress52) }
        # -AppDataDir follows the same convention: absent unless -VrfAppDataDir was given, so
        # the launch line above says plainly whether the sim read the vendor appData or the
        # relocated copy (APPDATA_RELOCATION_2026-09-14 sec 4). Validated in the preconditions.
        if ($VrfAppDataDir) { $launchArgs += @('-AppDataDir', $VrfAppDataDir) }
        if ($NoGui) { $launchArgs += '-NoGui' }
        $launchArgs += @('-NotifyLevel', [string]$BackendNotifyLevel)
        # STP-825 (2026-09-15): LaunchVrf52.ps1 now defaults to starting ITS OWN federation
        # holder (-FederationHoldSecs 900) for the STANDALONE DEMO path, which has no runner
        # in front of it. THIS runner already holds the federation at Stage 2h above, before
        # Stage 2c ever runs - so Stage 3 passes 0 EXPLICITLY here to keep this call site
        # BYTE-IDENTICAL to its pre-STP-825-demo-holder behaviour. Without this, a 5.2 run
        # would burn a SECOND appNumber on a second holder on top of Stage 2h's own.
        $launchArgs += @('-FederationHoldSecs', '0')
    }
    # -q | --doNotUseConsole for the back end. Off by default; see the -QuietBackend
    # note in the param block. Recorded in the manifest as inputs.quietBackend either way.
    if ($QuietBackend) { $launchArgs += '-QuietBackend' }
    # 5.2 back-end log. NOTHING is added to the launch line: -LogFileName is left off, so
    # LaunchVrf52 does not pass --logFileName to the sim (PREREG_52_CRASH_BISECT_2026-09-04
    # sec 5 - 6 startup crashes / 18 launches with it, 0 / 12 without, p = 0.031), and it
    # harvests the vendor's own log for the back-end pid instead. Said out loud because an
    # ABSENT argument is otherwise invisible in the evidence.
    if ($Is52) {
        Say '  back-end log: -LogFileName NOT passed, so the sim gets no --logFileName (PREREG_52_CRASH_BISECT_2026-09-04 sec 5:'
        Say '                6 startup crashes / 18 launches with it, 0 / 12 without, p = 0.031 - the OPTION, not the path).'
        Say '                LaunchVrf52 HARVESTS C:\MAK\logs\vrfSim*-<pid>.log for the back-end pid into runs\launch52 and'
        Say '                reports src/dst; this stage records both in the manifest (inputs.vrfProfile.vendorLog).'
        Say '                SECRETS: that copy holds the full process environment in cleartext - never attach it to a ticket'
        Say '                or mail; send the .callstack.log / .dmp instead (FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10).'
    }
    # THIS IS THE STAGE THAT DEADLOCKED THE RUNNER FOR 47 MINUTES ON 2026-07-19.
    # LaunchVrf's pwsh exits in ~35-60 s, but vrfGui and vrfSimHLA1516e are its
    # DESCENDANTS and are MEANT to keep running. Invoke-External now waits for the
    # direct child only; see its header block.
    # +120 = LaunchVrf's fixed ReadyTimeoutSec readiness poll (runner never lowers it);
    # header invariant is stage-budget + slack, and this stage's budget is that 120 s poll.
    $launchTimeoutSec = 120 + $StageTimeoutSec
    $r = Invoke-External -Name 'LaunchVrf' -File 'pwsh' -Arguments $launchArgs -Cwd $RepoRoot `
            -StdOutFile $PathLaunchOut -StdErrFile $PathLaunchErr `
            -TimeoutSec $launchTimeoutSec `
            -Note 'exit 0 READY; 1 PARTIAL (no front-end - crash risk); 2 precondition/args; 3 NOT READY within timeout; 4 BLOCKED (modal dialog). -AllowExistingVrf is deliberately NOT passed. LEAVES VR-FORCES RUNNING BY DESIGN - never waited on as a process tree.'
    if (-not $DryRun) {
        # VR-Forces may be up even if LaunchVrf itself never reported, so teardown
        # must be armed BEFORE the result is judged.
        $VrfLaunched = $true
        # OUT-OF-PROCESS TEARDOWN BACKSTOP, half 1 of 2 (2026-09-14). The finally at the
        # foot of this script is the teardown, and on 2026-09-14 it did NOT run: the runner
        # was TERMINATED from outside (TerminateProcess), which no finally, trap or
        # PowerShell.Exiting handler can intercept, and VR-Forces plus the interface stayed
        # joined for nine hours. The backstop therefore has to live OUTSIDE this process, in
        # the launching wrapper (scripts\RunScenario.sh). These two marker files are its
        # entire contract:
        #   runner.launched     - written HERE, the instant VR-Forces becomes OURS to stop.
        #                         Its ABSENCE tells the wrapper this run launched nothing, so
        #                         a validation abort can never make the wrapper tear down
        #                         someone ELSE's live session (RUNBOOK sec 0).
        #   runner.teardown-ran - written as the last statement of the finally.
        # Wrapper rule: launched AND NOT teardown-ran => the runner died; tear down for it.
        # The file CONTENTS are this runner's own PID, so an operator (or a cleanup script)
        # can name the one process that must not be swept while the window is open.
        try { Set-Content -LiteralPath (Join-Path $RunDir 'runner.launched') -Value ([string]$PID) -Encoding ascii } catch { }
        if (-not (Test-StageProduced -Result $r)) { Stop-Runner 3 (Get-StageFailureText -Name 'LaunchVrf' -Result $r) }
        switch ($r.ExitCode) {
            0 { Say-Ok 'VR-Forces READY' }
            2 { Stop-Runner 2 'LaunchVrf exited 2 (precondition/argument failure). Nothing joined.' }
            1 { Stop-Runner 3 'LaunchVrf exited 1 PARTIAL - back-end healthy but no front-end. That is the known 0xC0000005 crash-risk condition; refusing to run a scored trace on it.' }
            3 { Stop-Runner 3 ('LaunchVrf exited 3: NOT READY within timeout, or - on the 5.2 profile - the back-end CRASHED AT STARTUP (0xC0000005 in DtVrfSimOptions::parseCmdLine; its known trigger, --logFileName, is NOT passed by this profile - PREREG_52_CRASH_BISECT_2026-09-04 sec 5 - so a crash here is NEW and must be recorded, not explained away). Read {0}: a crash prints its callstack frames there. Other usual cause is an UNANSWERED RTI Assistant prompt (RUNBOOK 0.5.3). Nothing force-killed, nothing retried.' -f $PathLaunchOut) }
            4 { Stop-Runner 3 'LaunchVrf exited 4 BLOCKED - the front-end has no main window, i.e. a modal dialog is waiting. Nothing force-killed.' }
            default { Stop-Runner 3 ('LaunchVrf exited {0} - undocumented code.' -f $r.ExitCode) }
        }
        # BACK-END PID for the mid-run liveness check (2026-09-06: COA-STP1 run 2's sim died in
        # the DI-Guy controller 3 min after READY and the runner waited the full 180 s oracle gate
        # before failing for the wrong reason). LaunchVrf52 prints "back-end started (pid N)".
        $BackendPid = $null
        if (Test-Path -LiteralPath $PathLaunchOut -PathType Leaf) {
            $pm = [regex]::Match((Get-Content -LiteralPath $PathLaunchOut -Raw), 'back-end started \(pid (\d+)\)')
            if ($pm.Success) { $BackendPid = [int]$pm.Groups[1].Value; Say-Ok ('back-end pid {0} (liveness is checked while the run waits)' -f $BackendPid) }
            else { Say-Warn 'back-end pid not found in the launch output - the mid-run liveness check is OFF for this run.' }
        }
        # THE HARVESTED BACK-END LOG (5.2 only). The launcher does not pass --logFileName - it
        # copies the vendor's own log for the back-end pid instead - and says so in one marker
        # line, which is where the manifest's path comes from. Read AFTER the exit-code switch,
        # which is where the run is failed; a missing marker is recorded as null and flagged,
        # never fatal: the log is evidence, not a gate. Note Stop-Runner would already have
        # ended a crashed run above, so this fills in only on the paths that survive.
        if ($Is52 -and (Test-Path -LiteralPath $PathLaunchOut -PathType Leaf)) {
            # occasion is a single token; src runs to ' dst='; dst runs to end of line (paths
            # may contain spaces).
            $hm = [regex]::Match((Get-Content -LiteralPath $PathLaunchOut -Raw),
                                 'VENDOR LOG HARVESTED occasion=(\S+) src=(.*?) dst=(.*?)\s*$',
                                 [System.Text.RegularExpressions.RegexOptions]::Multiline)
            if ($hm.Success) {
                $Manifest.inputs.vrfProfile.vendorLog.harvestOccasion = $hm.Groups[1].Value
                $Manifest.inputs.vrfProfile.vendorLog.harvestedFrom   = $hm.Groups[2].Value
                $Manifest.inputs.vrfProfile.vendorLog.harvestedTo     = $hm.Groups[3].Value
                Say-Ok ('back-end log HARVESTED from the vendor copy ({0}) -> {1}. SECRETS: it holds the full process environment in cleartext - never attach it to a ticket or mail; send the .callstack.log / .dmp instead.' -f $hm.Groups[2].Value, $hm.Groups[3].Value)
            } else {
                Add-Flag 'WARN' ('No vendor back-end log was harvested by LaunchVrf52 (no "VENDOR LOG HARVESTED" line in {0}). The run has no back-end log; look in C:\MAK\logs by hand (the vendor stamps LOCAL time). NOT fatal and NOT a readiness verdict.' -f $PathLaunchOut)
            }
            Save-Manifest
        }
    }

    # =====================================================================
    # STAGE 3w - THE DETACHED TEARDOWN WATCHDOG (design A2, RUNNER_EXIT127 sec 3.1)
    # =====================================================================
    # ARMED HERE, immediately after runner.launched was written above, and NOT at stage 6b as
    # it was in 374ea49 (review finding F6). runner.launched is written the instant VR-Forces
    # becomes THIS run's to stop, and from that instant a death of this process leaves a
    # back-end up - which HARD-BLOCKS the next launch. Between the marker and stage 6b run
    # stage 3b's settle, the stage-4 pre-check, the observers, the pre-roll and PushInit: tens
    # of seconds to a couple of minutes, not the "sub-second" the first version assumed. The
    # watchdog needs nothing but the marker, so there is no reason to wait for the interface.
    #
    # WHY IT EXISTS AT ALL. The teardown below is a finally, and a finally is exactly what
    # TerminateProcess defeats; on 2026-09-14 that cost nine hours of a joined federate and a
    # 5.89 GB app log. The wrapper backstop (scripts\RunScenario.sh) repairs that only while
    # its bash survives. This process covers the wrapper dying too. It polls THIS pid, reads
    # nothing while we live, and tears down only on "runner.launched present AND
    # runner.teardown-ran absent" - the wrapper's rule.
    $WatchdogScript = Join-Path $PSScriptRoot 'RunnerWatchdog.ps1'
    # Stage 4's own pre-check window, named HERE because the watchdog budget below includes it
    # and the two must not drift. Stage 4 reads this same variable.
    $PreCheckSecs   = 30
    # -MaxSec. The watchdog NEVER tears down on its own timer (expiry is exit 4 and nothing
    # touched), so a generous budget is the safe direction and an under-budget one silently
    # ends the backstop mid-run. Armed at stage 3, it must now cover EVERYTHING from here to
    # this runner's last statement, which is the observation window PLUS the stages between
    # this point and the observers' start PLUS the teardown:
    #
    #   observation coverage  max(EffWatchSecs, DerivedWatchSecs)   - whichever is larger, because
    #                         an EXPLICIT -WatchSecs may be below the derived cap (it wins for the
    #                         observers and is WARNed about, but it must not shrink this budget).
    #                         derived = preRoll 20 + appJoin 180 + initDispatch 120 +
    #                         oracleGate 180 + pushOrderListen 30 + runSecs + trail 30 [+ settle]
    #   stage 3b settle       LaunchSettleSec                       (45 by default)
    #   stage 4 pre-check     PreCheckSecs + StageTimeoutSec        (30 + 600, its Invoke-External cap)
    #   stage 6 PushInit      StageTimeoutSec                       (600, its Invoke-External cap)
    #   teardown budgets      TraceStopGraceSec + AppExitTimeoutSec + StopVrfTimeoutSec (120+120+120)
    #   slack                 600
    #
    # G7 attempt 4 (-RunSecs 1200 -WatchSecs 2000 -PreOrderSettleSecs 240, defaults elsewhere):
    #   max(2000, 2000) + 45 + (30 + 600) + 600 + (120 + 120 + 120) + 600 = 4235 s = 70.6 min,
    #   against a run whose whole span from here to exit is ~1900-2100 s. Capped at 86400.
    $WatchdogCoverSecs = [Math]::Max($EffWatchSecs, $DerivedWatchSecs)
    $WatchdogMaxSec = [Math]::Min(86400, ($WatchdogCoverSecs +
                                          $LaunchSettleSec +
                                          ($PreCheckSecs + $StageTimeoutSec) +
                                          $StageTimeoutSec +
                                          $TraceStopGraceSec + $AppExitTimeoutSec + $StopVrfTimeoutSec +
                                          600))
    if ($NoWatchdog) {
        Say-Head 'Stage 3w - detached teardown watchdog: SKIPPED (-NoWatchdog)'
        Say-Warn 'no detached watchdog for this run. If this runner is killed, the ONLY backstop left is scripts\RunScenario.sh, which needs its bash to survive.'
        Add-Flag 'WARN' '-NoWatchdog was passed: this run has NO detached teardown watchdog. A kill that takes the wrapper too leaves VR-Forces and the interface joined.'
    } elseif (-not (Test-Path -LiteralPath $WatchdogScript -PathType Leaf)) {
        Say-Head 'Stage 3w - detached teardown watchdog: NOT AVAILABLE'
        Say-Warn ('{0} is missing. The run CONTINUES - a backstop must never fail a healthy run - but it is unprotected against a kill that also takes the wrapper.' -f $WatchdogScript)
        Add-Flag 'WARN' ('scripts\RunnerWatchdog.ps1 not found at {0}; no detached teardown watchdog for this run.' -f $WatchdogScript)
    } else {
        Say-Head 'Stage 3w - start the DETACHED teardown watchdog (out-of-process backstop)'
        # $PSHOME\pwsh.exe, never a bare 'pwsh': this runner is gated to a 64-bit host and the
        # watchdog must be the same one. Bare pwsh resolves to the 32-BIT build on this
        # machine (RUNBOOK 0.5.14 item 1).
        $WatchdogPwsh = Join-Path $PSHOME 'pwsh.exe'
        $WatchdogArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File', $WatchdogScript,
                          '-RunnerPid', [string]$PID,
                          '-RunDir',    $RunDir,
                          '-VrfProfile',$VrfProfile,
                          '-RestUrl',   $RestUrl,
                          '-StompUrl',  $StompUrl,
                          '-MaxSec',    [string]$WatchdogMaxSec,
                          '-PollSec',   '5')
        Say ('  watches pid {0} (this runner) every 5s for at most {1}s ({2:N1} min): cover {3} + settle {4} + preCheck {5} + pushInit {6} + teardown {7} + slack 600' -f `
                $PID, $WatchdogMaxSec, ($WatchdogMaxSec / 60.0), $WatchdogCoverSecs, $LaunchSettleSec, ($PreCheckSecs + $StageTimeoutSec), $StageTimeoutSec, ($TraceStopGraceSec + $AppExitTimeoutSec + $StopVrfTimeoutSec))
        Say  '  DETACHED: its own HIDDEN console (not ours - a shared console dies with the terminal and takes the'
        Say  '  watchdog with it), stdout/stderr to files in the run directory, stdin from an empty file there.'
        Say  '  It force-kills NOTHING and never touches rtiexec / rtiForwarder / rtiAssistant, and it acts on a'
        Say  '  death only after TWO observations 2 s apart (review of 374ea49, finding F2).'
        if (-not $DryRun) {
            # Start-Process needs a real path to redirect stdin FROM; an empty file is the
            # Windows equivalent of the wrapper's `< /dev/null`.
            try { [System.IO.File]::WriteAllText($PathWatchdogIn, '') }
            catch { Say-Warn ('could not create {0}: {1}. The watchdog will inherit this runner''s stdin instead.' -f $PathWatchdogIn, $_.Exception.Message) }
        } else {
            Say-Plan ('would create the empty stdin file {0}' -f $PathWatchdogIn)
        }
        # A BACKSTOP MUST NEVER FAIL A HEALTHY RUN. Start-External does not wrap Start-Process,
        # and $ErrorActionPreference is 'Stop', so without this try/catch a watchdog that could
        # not be started would raise a terminating error and tear down a run that is fine.
        $WatchdogProc = $null
        try {
        $WatchdogProc = Start-External -Name 'RunnerWatchdog' -File $WatchdogPwsh -Arguments $WatchdogArgs -Cwd $RepoRoot `
                -StdOutFile $PathWatchdogOut -StdErrFile $PathWatchdogErr `
                -StdInFile $(if ($DryRun -or (Test-Path -LiteralPath $PathWatchdogIn)) { $PathWatchdogIn } else { '' }) -NewConsole `
                -Note 'OUTLIVES THIS RUNNER BY DESIGN - it is never completed or waited on here, and it exits on its own the moment this pid is gone. exit 0 nothing owed / teardown done; 2 REFUSED (no runner.launched, or a pid mismatch - it touched nothing); 3 teardown ran with a failed step; 4 -MaxSec expired while the runner was still alive (nothing torn down); 5 unexpected. Its log: <runDir>\runner-watchdog.log.'
        } catch {
            Say-Warn ('the detached teardown watchdog could not be started: {0}' -f $_.Exception.Message)
            Add-Flag 'WARN' ('the detached teardown watchdog could not be started ({0}). The run CONTINUES - a backstop must never fail a healthy run - but it is unprotected against a kill that also takes the wrapper.' -f $_.Exception.Message)
        }
        if (-not $DryRun) {
            if ($null -ne $WatchdogProc) {
                try { Set-Content -LiteralPath $PathWatchdogPid -Value ([string]$WatchdogProc.Id) -Encoding ascii } catch { }
                Say-Ok ('teardown watchdog pid {0} armed (pid also in {1})' -f $WatchdogProc.Id, $PathWatchdogPid)
                $Manifest.artifacts.watchdog = [ordered]@{
                    pid     = $WatchdogProc.Id
                    pidFile = $PathWatchdogPid
                    stdout  = $PathWatchdogOut
                    stderr  = $PathWatchdogErr
                    log     = (Join-Path $RunDir 'runner-watchdog.log')
                    maxSec  = $WatchdogMaxSec
                    pollSec = 5
                    armedAtStage = '3w - immediately after runner.launched'
                }
                # DID IT SURVIVE ARMING? (review finding F5.) A watchdog that REFUSES at
                # validation exits in ~50 ms, and without this check the manifest would record
                # "armed" while the run went on unprotected. The run still continues - a
                # backstop must never fail a healthy run - but the manifest says what happened.
                Start-Sleep -Milliseconds 750
                $WatchdogGone = $false
                try { $WatchdogGone = $WatchdogProc.HasExited } catch { $WatchdogGone = $false }
                if ($WatchdogGone) {
                    $WatchdogCode = 'unknown'
                    try { $WatchdogCode = [string]$WatchdogProc.ExitCode } catch { }
                    $Manifest.artifacts.watchdog.exitedAtArming = $WatchdogCode
                    Add-Flag 'WARN' ('the detached teardown watchdog EXITED {0} within 750 ms of being started (2 = REFUSED at validation, touching nothing; 5 = unexpected error). THIS RUN IS UNPROTECTED against a kill that also takes the wrapper - the run continues anyway, because a backstop must never fail a healthy run. Its reason is the last lines of {1}.' -f $WatchdogCode, (Join-Path $RunDir 'runner-watchdog.log'))
                } else {
                    Say-Ok 'the watchdog was still alive 750 ms after arming (it did not refuse at validation).'
                }
                Save-Manifest
            } else {
                Add-Flag 'WARN' 'the detached teardown watchdog did not start (Start-External returned no process). The run CONTINUES unprotected against a kill that also takes the wrapper.'
            }
        }
    }

    # LaunchVrf's READY is thread-count + main-window only. It does NOT imply
    # scenario loaded or federation joined - the script says so itself, and
    # RUNBOOK 0.5.7 measured settle times past 50 s.
    Say-Head ('Stage 3b - settle {0}s before touching the oracle (RUNBOOK 0.5.7: READY does NOT mean joined)' -f $LaunchSettleSec)
    if ($DryRun) { Say-Plan ('would sleep {0}s' -f $LaunchSettleSec) } else { Start-Sleep -Seconds $LaunchSettleSec }

    # ---------------------------------------------------------------------
    # STAGE 4 - ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
    # ---------------------------------------------------------------------
    Say-Head 'Stage 4 - oracle pre-check, pre-init (ADVISORY - see the header block)'
    Say '  RUNBOOK 0.5.7: a stock TropicTortoise contains only POSITIONLESS control objects,'
    Say '  so a DEGENERATE result here is EXPECTED and is NOT a fault. What this stage proves'
    Say '  is that the oracle can JOIN and DISCOVER. The real coordinate criterion is applied'
    Say '  post-init at stage 7, against the scoring trace.'
    $preSecs = $PreCheckSecs
    # --report-backends: appended ONLY when stage 0b saw it advertised (an unprobed flag on the
    # live path is exit 2 and a dead stage - the -ConsoleLogDir landmine). V6c arm A0.
    $WatchBackendArgs = @(); if ($ProbeWatch.supportsReportBackends) { $WatchBackendArgs = @('--report-backends') }
    $r = Invoke-External -Name 'WatchVrf-precheck' -File $ExeWatchVrf `
            -Arguments (@([string]$AppNo['oraclePre'], [string]$preSecs, [string]$SampleSecs, $FederationArg) + $WatchBackendArgs) `
            -Cwd $Bin64 -StdOutFile $PathPreTrace -StdErrFile $PathPreTraceErr `
            -TimeoutSec ($preSecs + $StageTimeoutSec) `
            -Note 'ADVISORY. WatchVrf exit 2 = usage/arg error (the runner built bad args); exit 1 = operational. These args are generated, so treat a 2 as a runner bug. Exits on its OWN timer after seconds-to-watch.'
    if (-not $DryRun) {
        # The STAGE is advisory; a WatchVrf that will not exit is NOT. It is a
        # joined federate wedged in the federation, and everything downstream -
        # the scoring trace, the app join - runs through that same federation.
        # Recorded, never killed, and the run is failed cleanly.
        if (-not (Test-StageProduced -Result $r)) { Stop-Runner 3 (Get-StageFailureText -Name 'WatchVrf-precheck' -Result $r) }
        $preText = Read-LiveText -Path $PathPreTrace
        $pre     = Get-RealPositions -TraceText $preText
        $Manifest.oracle.preInit = [ordered]@{
            advisory        = $true
            appNumber       = $AppNo['oraclePre']
            exitCode        = $r.ExitCode
            posLines        = $pre.PosLineCount
            realCoordLines  = $pre.RealCount
            degenerateLines = $pre.DegenerateCount
            lastSummaryLine = (Get-TraceSummaryLine -TraceText $preText)
            firstRealLine   = $(if ($pre.First) { $pre.First.line } else { $null })
        }
        Say ('  POS lines={0} real={1} degenerate={2}  last: {3}' -f $pre.PosLineCount, $pre.RealCount, $pre.DegenerateCount, (Get-TraceSummaryLine -TraceText $preText))
        if ($pre.RealCount -gt 0) {
            Say-Ok ('oracle already reads a REAL coordinate pre-init: {0}' -f $pre.First.line)
        } else {
            Add-Flag 'INFO' 'Pre-init oracle pre-check saw NO real-coordinate POS line. EXPECTED on a stock TropicTortoise (RUNBOOK 0.5.7); advisory only.'
            if ($StrictPreInitOracle) {
                Stop-Runner 3 '-StrictPreInitOracle was set and the pre-init pre-check found no real coordinate.'
            }
        }
        if ($r.ExitCode -ne 0) {
            Add-Flag 'WARN' ('WatchVrf pre-check exited {0} (advisory stage). Exit 2 from WatchVrf is AMBIGUOUS - usage or operational.' -f $r.ExitCode)
        }
        Save-Manifest
    }

    # ---------------------------------------------------------------------
    # STAGE 5 - OBSERVERS START FIRST, BEFORE THE INIT (unit births in trace)
    # ---------------------------------------------------------------------
    Say-Head 'Stage 5 - start the observers BEFORE the init, so unit births are in the trace'
    # $WatchConsoleArgs is EMPTY unless -ConsoleLogDir was supplied, so the argument list
    # here is byte-identical to before that parameter existed on a default run. Only the
    # stage-5 SCORING trace is armed, never the stage-4 pre-check: the pre-check runs
    # pre-init against a stock scenario that contains no units, so there is nothing there
    # whose console is worth capturing, and arming it would raise notify levels before the
    # observation window the run is actually scored on.
    if ($PathConsoleDir) {
        Say ('  -ConsoleLogDir is IGNORED - WatchVrf has no --console-log-dir flag and emits no CONARM lines. (was {0}; the BACKEND writes the files, and it may not be this machine)' -f $PathConsoleDir)
    }
    # --stop-file is appended ONLY when stage 0b saw the deployed binary advertise it.
    # An unprobed flag on the live path is exit 2 and a dead oracle stage (the
    # -ConsoleLogDir landmine above). Without it the tool runs to its duration cap.
    $WatchStopArgs  = @(); if ($ProbeWatch.supportsStopFile)  { $WatchStopArgs  = @('--stop-file', $PathStopFile) }
    $ListenStopArgs = @(); if ($ProbeListen.supportsStopFile) { $ListenStopArgs = @('--stop-file', $PathStopFile) }
    # Second F5 guard, at the point of use: the pre-flight check above ran before the
    # run directory existed; if the file appeared since (a concurrent runner in the
    # same directory), both observers would refuse with exit 2 and the oracle stage
    # would be dead. Fail through teardown instead - nothing has joined yet.
    if (-not $DryRun -and (Test-Path -LiteralPath $PathStopFile)) {
        Stop-Runner 3 ('the observer stop file {0} appeared before the observers were started - the run directory is shared with another writer. Refusing to start the observers (they would refuse it themselves with exit 2).' -f $PathStopFile)
    }
    $WatchProc = Start-External -Name 'WatchVrf-trace' -File $ExeWatchVrf `
            -Arguments (@([string]$AppNo['oracleTrace'], [string]$EffWatchSecs, [string]$SampleSecs, $FederationArg) + $WatchConsoleArgs + $WatchStopArgs + $WatchBackendArgs) `
            -Cwd $Bin64 -StdOutFile $PathTrace -StdErrFile $PathTraceErr `
            -Note $(if ($ProbeWatch.supportsStopFile) { 'THE MOVEMENT ORACLE and the scoring input. Started before PushInit (HEADLESS_RUN_PLAN sec 2). Duration is the CAP; teardown ends it via the stop file and it resigns cleanly; never killed.' }
                    else { 'THE MOVEMENT ORACLE and the scoring input. Started before PushInit (HEADLESS_RUN_PLAN sec 2). Resigns on its own timer (deployed binary has no --stop-file); never killed.' })
    $ListenEndpointArgs = @(); if ($ProbeListen.supportsEndpoints) { $ListenEndpointArgs = @('--rest-url', $RestUrl, '--stomp-url', $StompUrl) }
    $ListenProc = Start-External -Name 'ListenReports' -File $ExeListenReports `
            -Arguments (@([string]$EffWatchSecs, $PathReports) + $ListenStopArgs + $ListenEndpointArgs) `
            -Cwd $RepoRoot -StdOutFile $PathReportsOut -StdErrFile $PathReportsErr `
            -Note 'Listens on -RestUrl/-StompUrl when the deployed binary supports --rest-url/--stomp-url (Stage 0b), else the historical 8080/61613 (already checked to match). Writes reports-captured.log INCREMENTALLY - every captured report is appended as it arrives (0999eeb), so the capture is live evidence DURING the window and not only after exit; its stdout names the server it heard.'

    Say-Head ('Stage 5b - pre-roll {0}s of trace before the init is pushed' -f $PreRollSecs)
    if ($DryRun) { Say-Plan ('would sleep {0}s' -f $PreRollSecs) } else { Start-Sleep -Seconds $PreRollSecs }

    # ---------------------------------------------------------------------
    # STAGE 6 - PushInit, THEN the app (RUNBOOK sec 3: order matters)
    # ---------------------------------------------------------------------
    Say-Head 'Stage 6 - PushInit, then start the interface (RUNBOOK sec 3: init FIRST, the app late-joins)'
    $r = Invoke-External -Name 'PushInit' -File $ExePushInit `
            -Arguments @($Init, $RestUrl, $StompUrl) -Cwd $RepoRoot `
            -StdOutFile $PathPushInitOut -StdErrFile $PathPushInitErr `
            -TimeoutSec $StageTimeoutSec `
            -Note 'exit 0 ok; 1 push rejected; 2 usage. Drives the server RESET -> INITIALIZING -> share -> RUNNING. Never run against a RUNNING interface (RUNBOOK sec 4 corollary) - none is running yet.'
    if (-not $DryRun) {
        if (-not (Test-StageProduced -Result $r)) { Stop-Runner 3 (Get-StageFailureText -Name 'PushInit' -Result $r) }
        switch ($r.ExitCode) {
            0 { Say-Ok 'init accepted and server switched to RUNNING' }
            2 { Stop-Runner 2 'PushInit exited 2 (usage error). The server was NOT touched.' }
            default { Stop-Runner 3 ('PushInit exited {0} - the init was rejected or the push failed. See {1}.' -f $r.ExitCode, $PathPushInitOut) }
        }
        $pushInitText = Read-LiveText -Path $PathPushInitOut
        $qm = [regex]::Match($pushInitText, 'QUERYINIT\s*:\s*(\d+)\s+Units')
        if ($qm.Success) {
            $Manifest.oracle.queryInitUnits = [int]$qm.Groups[1].Value
            Say-Ok ('QUERYINIT reports {0} units will be handed to a late joiner' -f $qm.Groups[1].Value)
            if ([int]$qm.Groups[1].Value -eq 0) {
                Add-Flag 'FAIL' 'QUERYINIT reports 0 Units - the interface will create nothing. Continuing to collect evidence, but this run is NOT valid (4a.6).'
            }
        } else {
            Add-Flag 'WARN' 'could not read the QUERYINIT unit count out of PushInit stdout.'
        }
    }

    Say-Head 'Stage 6b - start VrfC2SimApp with a LEDGERED ApplicationNumber'
    Say ('  Vrf__ApplicationNumber={0} comes from the Appendix B marker, NOT from appsettings.json' -f $AppNo['app'])
    Say  '  (appsettings.json carries a baked-in ApplicationNumber; hand-editing it is exactly how stale-federate hangs were created - the env override wins and is ledgered)'
    Say ('  C2SIM__RestUrl={0} C2SIM__StompUrl={1} come from -RestUrl/-StompUrl, NOT from appsettings.json (same env-override mechanism; the app must hear the SAME server every other stage talks to)' -f $RestUrl, $StompUrl)
    if ($Is52) {
        Say ('  5.2: the app joins the CONFIG-FILE way, exactly as the tools do - Vrf__Federation and')
        Say ('  Vrf__FedFileName EMPTY, Vrf__ConfigFileIdentity=true (clears the appsettings FOM module')
        Say ('  list; config modules are ADDITIVE, DIFF row A9), Vrf__ConnectionConfigFile={0}' -f $ConnConfigFile)
    }
    if (-not $DryRun) {
        $env:Vrf__ApplicationNumber = [string]$AppNo['app']
        $env:C2SIM__RestUrl  = $RestUrl
        $env:C2SIM__StompUrl = $StompUrl
        foreach ($k in $AppEnv52.Keys) { Set-Item -Path ('Env:' + $k) -Value ([string]$AppEnv52[$k]) }
    }
    else { Say-Plan ('would set env Vrf__ApplicationNumber={0}, C2SIM__RestUrl, C2SIM__StompUrl for the child, then restore them' -f $AppNo['app']) }
    $AppProc = Start-External -Name 'VrfC2SimApp' -File $ExeApp `
            -Arguments @(('--contentRoot=' + (Split-Path -Parent $ExeApp))) -Cwd $Bin64 `
            -StdOutFile $PathAppLog -StdErrFile $PathAppErr `
            -Note 'cwd MUST be VR-Forces bin64 so Legion finds vrfLegion.lua (RUNBOOK sec 7 item 3); --contentRoot keeps appsettings.json loading. ApplicationNumber and C2SIM endpoints overridden via env.'
    if (-not $DryRun) {
        $AppStarted = $true
        $env:Vrf__ApplicationNumber = $SavedVrfAppNumber
        $env:C2SIM__RestUrl  = $SavedC2SimRestUrl
        $env:C2SIM__StompUrl = $SavedC2SimStompUrl
        foreach ($k in $AppEnv52.Keys) { Set-Item -Path ('Env:' + $k) -Value ([string]$SavedAppEnv52[$k]) }
    }

    Say-Head ('Stage 6c - wait up to {0}s for the interface to connect to C2SIM' -f $AppJoinTimeoutSec)
    if ($DryRun) {
        Say-Plan ('would poll {0} for "Connected to C2SIM", with the app thread count as a backstop' -f $PathAppLog)
    } else {
        $deadline = (Get-Date).AddSeconds($AppJoinTimeoutSec)
        $connected = $false
        $threads = 0
        while ((Get-Date) -lt $deadline) {
            if ($AppProc.HasExited) {
                Stop-Runner 3 ('VrfC2SimApp exited early with code {0} - see {1} and {2}.' -f $AppProc.ExitCode, $PathAppLog, $PathAppErr)
            }
            $appText = Read-LiveText -Path $PathAppLog
            if ($appText -match 'Connected to C2SIM') { $connected = $true; break }
            try { $threads = (Get-Process -Id $AppProc.Id -ErrorAction Stop).Threads.Count } catch { $threads = 0 }
            Start-Sleep -Seconds 3
        }
        $Manifest.oracle.appConnected = $connected
        $Manifest.oracle.appThreads   = $threads
        # WHICH STACK THE APP ACTUALLY BOUND. The app logs
        #   VrfBridge native stack = <5.2|5.0.2>|<vrfcontrol.dll path>
        # right after Start(). That is the RUNTIME fact; -VrfProfile is only an intention,
        # and MAK DLLs bind by NAME on PATH, so the two CAN disagree (a 5.2 binary under a
        # 5.0.2 prefix loads 5.0.2 and says so only here). Recorded either way, flagged on
        # a mismatch - a trace from the wrong stack is not evidence about the other one.
        $stackMatch = [regex]::Match((Read-LiveText -Path $PathAppLog), 'VrfBridge native stack = (?<s>[^;]+)')
        if ($stackMatch.Success) {
            $nativeStack = $stackMatch.Groups['s'].Value.Trim()
            $Manifest.inputs.vrfProfile.nativeStack = $nativeStack
            $expectTag = $(if ($Is52) { '5.2|' } else { '5.0.2|' })
            if ($nativeStack.StartsWith($expectTag)) { Say-Ok ('app native stack = {0} (matches -VrfProfile {1})' -f $nativeStack, $VrfProfile) }
            else { Add-Flag 'FAIL' ('-VrfProfile {0} but the app BOUND {1}. The binaries and the PATH disagree; nothing this run observes belongs to the profile it claims.' -f $VrfProfile, $nativeStack) }
        } else {
            Add-Flag 'WARN' 'no "VrfBridge native stack" line in the app log yet - which MAK stack the interface bound is UNRECORDED for this run.'
        }
        if ($connected) { Say-Ok 'interface logged "Connected to C2SIM"' }
        else {
            # RUNBOOK sec 3: a redirected stdout can be block-buffered, so absence of
            # the line is not proof of absence of the connect. Thread count is the
            # independent signal (connected ~9-10, hang-at-RTI 1).
            Add-Flag 'WARN' ('did not see "Connected to C2SIM" within {0}s (thread count {1}). Redirected stdout can be block-buffered (RUNBOOK sec 3), so this is NOT proof it failed. Continuing.' -f $AppJoinTimeoutSec, $threads)
        }
    }

    Say-Head ('Stage 6d - wait up to {0}s for the interface to dispatch the init' -f $InitDispatchWaitSec)
    if ($DryRun) {
        Say-Plan ('would poll {0} for "Init dispatched: N units + M areas queued for creation"' -f $PathAppLog)
    } else {
        $deadline = (Get-Date).AddSeconds($InitDispatchWaitSec)
        $dm = $null
        while ((Get-Date) -lt $deadline) {
            if ($AppProc.HasExited) {
                Stop-Runner 3 ('VrfC2SimApp exited before dispatching the init (code {0}).' -f $AppProc.ExitCode)
            }
            $dm = [regex]::Match((Read-LiveText -Path $PathAppLog), 'Init dispatched:\s*(\d+)\s+units\s*\+\s*(\d+)\s+areas')
            if ($dm.Success) { break }
            Start-Sleep -Seconds 3
        }
        if ($dm -and $dm.Success) {
            $Manifest.oracle.initDispatchedUnits = [int]$dm.Groups[1].Value
            $Manifest.oracle.initDispatchedAreas = [int]$dm.Groups[2].Value
            Say-Ok ('interface dispatched {0} units + {1} areas for creation' -f $dm.Groups[1].Value, $dm.Groups[2].Value)
        } else {
            $Manifest.oracle.initDispatchedUnits = $null
            Add-Flag 'WARN' ('no "Init dispatched" line within {0}s. Continuing deliberately: a wasted observation window is cheaper than a lost diagnosis, and the trace + app log are the evidence either way.' -f $InitDispatchWaitSec)
        }
        Save-Manifest
    }

    # ---------------------------------------------------------------------
    # STAGE 7 - THE ORACLE GATE (RUNBOOK 0.5.7 CORRECTED criterion, for real)
    # ---------------------------------------------------------------------
    Say-Head ('Stage 7 - ORACLE GATE: a POS line with REAL lat/lon, retried up to {0}s' -f $OracleGateTimeoutSec)
    Say '  RUNBOOK 0.5.7 CORRECTED: reflected>0 is NOT sufficient (it passes on pole/NaN'
    Say '  garbage) and reflected=0 at 20 s is NOT a stop (settle time exceeded 50 s live).'
    Say '  PASS = lat/lon real, not NaN, not the 90/-90 pole. Applied to the live trace.'
    if ($DryRun) {
        Say-Plan ('would poll {0} every 5s for up to {1}s for a real-coordinate POS line; STOP the run if none appears' -f $PathTrace, $OracleGateTimeoutSec)
        Say-Plan ('ON FAILURE ONLY, would then run stage 7b below with appNumber {0} before failing the run.' -f $AppNo['createOneDiag'])
        $null = Invoke-CreateOneDiagnostic -AppNumber $AppNo['createOneDiag'] -TracePath $PathTrace `
                    -WatchProcess $WatchProc -WatchSec $CreateOneWatchSec
    } else {
        $deadline = (Get-Date).AddSeconds($OracleGateTimeoutSec)
        $gate = $null
        while ($true) {
            # The PLACEMENT lines - the start of the interval the stage-7d gate measures -
            # land HERE, during this wait, and the nav-area row can land here too on a fast
            # warm machine. Watching from this loop is what makes the delta the real one
            # instead of one measured from the gate's own start. Off unless the gate is on.
            if ($PreOrderGateOn) { Update-NavGateWatch -AppLogPath $PathAppLog }
            $traceText = Read-LiveText -Path $PathTrace
            $gate = Get-RealPositions -TraceText $traceText
            if ($gate.RealCount -gt 0) { break }
            if ((Get-Date) -ge $deadline) { break }
            # Mid-run liveness: a back-end that has DIED cannot produce a real coordinate; fail now,
            # with the right reason, instead of waiting out the gate (its crash record is the
            # newest C:\MAK\logs\vrfSimHLA1516e5.2d-*.callstack.log + .dmp - sharable; the .log is not).
            if ($BackendPid) {
                # Two forms of death: the process is gone, or it is PARKED on the MAK crash-dump prompt
                # (still a live pid, 3+ GB resident, doing nothing) - in which case its crash record
                # <build>-<pid>.callstack.log / .dmp already exists (runs L1/L2 2026-09-06 sat like that
                # for the whole 180 s gate).
                $gone = -not (Get-Process -Id $BackendPid -ErrorAction SilentlyContinue)
                $cs = Get-ChildItem -Path 'C:\MAK\logs' -Filter ('vrfSimHLA1516e5.2d-*-{0}.callstack.log' -f $BackendPid) -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($gone -or $cs) {
                    Stop-Runner 3 ('BACK-END pid {0} {1} during the oracle-gate wait (not a gate result). Crash record: {2}. Read its callstack before anything else; the harvested .log holds the environment and is NOT for sharing.' -f $BackendPid, $(if ($gone) { 'DIED' } else { 'CRASHED and is PARKED on the dump prompt' }), $(if ($cs) { $cs.FullName } else { '(none found in C:\MAK\logs)' }))
                }
            }
            Say-Info ('  no real coordinate yet - {0}' -f $(if (Get-TraceSummaryLine -TraceText $traceText) { Get-TraceSummaryLine -TraceText $traceText } else { 'no samples yet' }))
            Start-Sleep -Seconds 5
        }
        $Manifest.oracle.gate = [ordered]@{
            criterion       = 'RUNBOOK 0.5.7 CORRECTED: at least one POS line with real (non-NaN, non-pole) lat/lon. This is a RUN-VALIDITY gate, NOT a score.'
            timeoutSec      = $OracleGateTimeoutSec
            posLines        = $gate.PosLineCount
            realCoordLines  = $gate.RealCount
            degenerateLines = $gate.DegenerateCount
            distinctRealUuids = @($gate.Uuids)
            firstRealLine   = $(if ($gate.First) { $gate.First.line } else { $null })
            passed          = ($gate.RealCount -gt 0)
        }
        Save-Manifest
        if ($gate.RealCount -gt 0) {
            Say-Ok ('ORACLE GATE PASSED: {0} real-coordinate POS lines across {1} distinct uuids' -f $gate.RealCount, $gate.Uuids.Count)
            Say-Ok ('  first: {0}' -f $gate.First.line)
        } else {
            # The gate has failed and the run is now unscored. Spend the ledgered
            # diagnostic appNumber on the RUNBOOK 0.5.7 STRONGER CHECK before
            # exiting, so the failure names a LAYER instead of a symptom. The
            # verdict changes only the exit reason - the run fails either way.
            Say-Fail ('ORACLE GATE FAILED: no real-coordinate POS line within {0}s ({1} POS lines seen, all degenerate).' -f $OracleGateTimeoutSec, $gate.PosLineCount)
            # The diagnostic is BEST EFFORT and must never displace the gate's own
            # verdict. If it blows up, that is a failed diagnostic on an already
            # failed run - it must not escape into the generic catch and turn a
            # clean exit 3 into an exit 5 ("unexpected error, inspect the machine").
            $verdict = $null
            try {
                $verdict = Invoke-CreateOneDiagnostic -AppNumber $AppNo['createOneDiag'] -TracePath $PathTrace `
                                -WatchProcess $WatchProc -WatchSec $CreateOneWatchSec
            } catch {
                Say-Warn ('stage 7b diagnostic itself failed: {0}' -f $_.Exception.Message)
                $Manifest.oracle.createOneVerdict = 'INCONCLUSIVE'
                $Manifest.oracle.createOneDiagnosticError = $_.Exception.Message
            }
            if (-not $verdict) { $verdict = 'INCONCLUSIVE' }
            Stop-Runner 3 ("ORACLE GATE FAILED: no POS line with a real coordinate within {0}s (POS lines seen: {1}, all degenerate). RUNBOOK 0.5.7 STOP condition. Stage 7b disambiguation verdict: {2} - see oracle.createOneDiagnostic in the manifest, which spells out what that verdict means." -f $OracleGateTimeoutSec, $gate.PosLineCount, $verdict)
        }
    }

    # ---------------------------------------------------------------------
    # STAGE 7d - PRE-ORDER READY GATE (-PreOrderGate) or FIXED SETTLE (-PreOrderSettleSecs)
    # ---------------------------------------------------------------------
    # WHY THE STAGE EXISTS: the sectorised navigation area loads LAZILY, AFTER the entities
    # are placed, so a task issued too early is PLANNED WITHOUT THE MESH - and silently: the
    # move-to tree's "Is current point in nav area?" action fails and the whole plan drops to
    # the FEATURE planner on one straight part. Four single-variable runs on 2026-09-14
    # (docs/experiments/G7B_G8_RESULTS_2026-09-14.md sec 1.5) put the usable instant at the
    # first "New Primary nav area: | <area>" row from a placed platform, and it partitions
    # the gate outcomes PERFECTLY: every goal before that row failed, every goal after it
    # passed, resolved to 0.3 s in run D. Under CreationPolicy=AtOrder the order reached the
    # bus 4.7-7.7 s BEFORE the row in all three AtOrder runs.
    #
    # TWO WAYS TO WAIT, AND THEY ARE NOT EQUALS.
    #   -PreOrderGate NavArea   waits for the SIMULATOR'S OWN row. It is the ready SIGNAL, so
    #                           it is right at 9.1-12.1 s warm and at 236.9 s cold - a 20x
    #                           spread (sec 3b) no fixed number covers. Needs the object
    #                           consoles at >= 3 (stage 0 refuses it otherwise).
    #   -PreOrderSettleSecs N   holds a FIXED N seconds. It says how long we wait, never what
    #                           counts as loaded, and it cannot tell you the mesh arrived - a
    #                           guess against that spread, kept because it is all there is
    #                           when the consoles are shut. >= 30 s warm, 240 s+ cold.
    # GATE OR SETTLE, NEVER ONE AFTER THE OTHER. With both given the GATE is in force and the
    # settle is the TIMEOUT FALLBACK only, so a gate that never fires degrades to the old
    # behaviour instead of stopping a run that would otherwise have been fine.
    #
    # WHAT NEITHER IS: a fix. The vendor-side loadAllNavigationDataOnTerrainLoad was tested
    # and moved this row by nothing at all (sec 3a) - do not let it stand in for the wait.
    # Both budgets are already counted into the observers' duration cap ($DerivedWatchSecs).
    $RunPreOrderSettle = ($PreOrderSettleSecs -gt 0)
    if ($PreOrderGateOn) {
        Say-Head ('Stage 7d - pre-order READY GATE ({0}): hold PushOrder until the simulator acquires its nav area (timeout {1}s)' -f $PreOrderGate, $PreOrderGateTimeoutSec)
        Say  '  The signal is the simulator''s own row, not a guess: "New Primary nav area: | <area>" from ANY'
        Say ('  placed platform, at object-console level 3 (this run: {0}). Measured 2026-09-14: 9.1-12.1 s' -f $ObjectConsoleLevel)
        Say  '  after the first placement WARM, 236.9 s COLD. Until it prints, every ground-vehicle-move-to'
        Say  '  is feature-planned, silently (G7B_G8_RESULTS_2026-09-14 sec 1.5).'
        if ($DryRun) {
            Say-Plan ('would poll {0} incrementally every 2s for up to {1}s for that row, then push the order IMMEDIATELY' -f $PathAppLog, $PreOrderGateTimeoutSec)
            Say-Plan ('would log the object, the area and the delta from the first "PLACEMENT: <UNIT|PLATFORM> <name> ... created" line, and call it WARM at <= {0}s or COLD above it (the cache-state indicator)' -f $PreOrderGateWarmSecs)
            if ($PreOrderSettleSecs -gt 0) {
                Say-Plan ('on gate TIMEOUT would fall back to the {0}s -PreOrderSettleSecs hold (FALLBACK ONLY: the gate is what is in force)' -f $PreOrderSettleSecs)
            } else {
                Say-Plan 'on gate TIMEOUT would STOP the run (exit 3, NOT-READY) - no -PreOrderSettleSecs fallback was given'
            }
            $RunPreOrderSettle = $false
        } else {
            $gateStartUtc = (Get-Date).ToUniversalTime()
            $Manifest.clocks.preOrderGateStartUtc = $gateStartUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            Save-Manifest
            $gateEnd      = (Get-Date).AddSeconds($PreOrderGateTimeoutSec)
            $nextGateNote = (Get-Date).AddSeconds(15)
            while ($true) {
                # ONE incremental read per poll, shared with the stage-7 loop through the
                # 'applog-navgate' offset key: the log reaches gigabytes and a whole-file read
                # at 2 s would be the G6 cadence collapse all over again (RUNNER_HARDENING sec 5).
                Update-NavGateWatch -AppLogPath $PathAppLog
                if ($null -ne $script:NavGate.firstRow) { break }
                if ((Get-Date) -ge $gateEnd) { break }
                # LIVENESS, not a blind wait - the same three checks the settle below makes and
                # for the same reason: a 5.2 sim can die here, and the order has NOT been pushed
                # yet, so a death during the gate is a clean stop rather than a truncated window.
                if ($BackendPid) {
                    $gone = -not (Get-Process -Id $BackendPid -ErrorAction SilentlyContinue)
                    $cs = Get-ChildItem -Path 'C:\MAK\logs' -Filter ('vrfSimHLA1516e5.2d-*-{0}.callstack.log' -f $BackendPid) -ErrorAction SilentlyContinue | Select-Object -First 1
                    if ($gone -or $cs) {
                        Stop-Runner 3 ('BACK-END pid {0} {1} DURING the stage-7d READY GATE. The order was NOT pushed. Crash record: {2}. Read its callstack before anything else; the harvested .log holds the environment and is NOT for sharing.' -f $BackendPid, $(if ($gone) { 'DIED' } else { 'CRASHED and is PARKED on the dump prompt' }), $(if ($cs) { $cs.FullName } else { '(none found in C:\MAK\logs)' }))
                    }
                }
                if ($AppProc.HasExited) {
                    Stop-Runner 3 ('VrfC2SimApp EXITED with code {0} during the stage-7d READY GATE - there is nothing left to push the order into, and nothing left to print the nav-area row. See {1} and {2}.' -f $AppProc.ExitCode, $PathAppLog, $PathAppErr)
                }
                if ($null -ne $WatchProc -and $WatchProc.HasExited) {
                    Stop-Runner 3 ('the trace observer WatchVrf-trace EXITED with code {0} during the stage-7d READY GATE - THE MOVEMENT ORACLE IS GONE, so an order pushed now would be unscored. See {1}.' -f $WatchProc.ExitCode, $PathTrace)
                }
                if ((Get-Date) -ge $nextGateNote) {
                    $nextGateNote = (Get-Date).AddSeconds(15)
                    Say-Info ('  nav-area gate: {0}s of {1}s elapsed, {2} area row(s) seen, {3} placement line(s) seen{4} (back-end, interface and trace observer all alive)' -f `
                        [int]((Get-Date).ToUniversalTime() - $gateStartUtc).TotalSeconds, $PreOrderGateTimeoutSec, `
                        $script:NavGate.areaRowsSeen, $script:NavGate.placementRowsSeen, `
                        $(if ($null -ne $script:NavGate.firstPlacementUtc) { (' - {0}s since the first placement ({1})' -f [int]((Get-Date).ToUniversalTime() - $script:NavGate.firstPlacementUtc).TotalSeconds, $script:NavGate.firstPlacementName) } else { '' }))
                }
                Start-Sleep -Seconds 2
            }
            $gateNowUtc = (Get-Date).ToUniversalTime()
            $row        = $script:NavGate.firstRow
            # The cache-state indicator: first PLACEMENT -> first area row. ~10 s means the
            # terrain tiles were already in the file cache, ~240 s means they were not
            # (G7B_G8_RESULTS sec 3b; identical 2.33 GB streamed either way). It is the
            # NUMBER TO QUOTE when a demo run is slow to start, and the reason the prepare
            # step loads the scenario once beforehand.
            $deltaSecs  = $null
            $cacheState = 'UNKNOWN'
            if ($null -ne $row -and $null -ne $script:NavGate.firstPlacementUtc) {
                $deltaSecs  = [Math]::Round(($script:NavGate.firstRowUtc - $script:NavGate.firstPlacementUtc).TotalSeconds, 1)
                $cacheState = $(if ($deltaSecs -le $PreOrderGateWarmSecs) { 'WARM' } else { 'COLD' })
            }
            $Manifest.oracle.preOrderGate = [ordered]@{
                mode               = $PreOrderGate
                timeoutSec         = $PreOrderGateTimeoutSec
                warmThresholdSec   = $PreOrderGateWarmSecs
                objectConsoleLevel = $ObjectConsoleLevel
                signal             = 'the first "VRF console [N] <object> (VRF_UUID:...): New Primary nav area: | <area>" row in vrfc2simapp.log - the simulator''s own acquisition of the sectorised nav area, which partitions the move-to nav gate perfectly (G7B_G8_RESULTS_2026-09-14 sec 1.5)'
                fired              = ($null -ne $row)
                object             = $(if ($null -ne $row) { $row.object } else { $null })
                objectUuid         = $(if ($null -ne $row) { $row.uuid } else { $null })
                area               = $(if ($null -ne $row) { $row.area } else { $null })
                row                = $(if ($null -ne $row) { $row.line } else { $null })
                areaRowsSeen       = $script:NavGate.areaRowsSeen
                placementRowsSeen  = $script:NavGate.placementRowsSeen
                firstPlacement     = $(if ($script:NavGate.firstPlacementName) { ('{0} {1}' -f $script:NavGate.firstPlacementKind, $script:NavGate.firstPlacementName) } else { $null })
                firstPlacementUtc  = $(if ($null -ne $script:NavGate.firstPlacementUtc) { $script:NavGate.firstPlacementUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ') } else { $null })
                placementToAreaSec = $deltaSecs
                cacheState         = $cacheState
                deltaResolutionNote = 'both instants are the RUNNER''s observation times (vrfc2simapp.log carries no per-line timestamp); resolution is the poll interval - 5 s in the stage-7 loop, 2 s here.'
                waitedSec          = [Math]::Round(($gateNowUtc - $gateStartUtc).TotalSeconds, 1)
                fellBackToSettle   = $false
            }
            if ($null -ne $row) {
                $Manifest.clocks.preOrderGateFiredUtc = $script:NavGate.firstRowUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                $RunPreOrderSettle = $false
                Say-Ok ('NAV AREA ACQUIRED after {0}s of gate: object "{1}" took primary area "{2}"' -f $Manifest.oracle.preOrderGate.waitedSec, $row.object, $row.area)
                Say-Ok ('  {0}' -f $row.line)
                if ($null -ne $deltaSecs) {
                    Say-Ok ('  first placement ({0}) -> area row: {1}s  ->  {2} file cache (warm ~10s / cold ~240s, G7B_G8_RESULTS sec 3b; threshold {3}s)' -f `
                        $Manifest.oracle.preOrderGate.firstPlacement, $deltaSecs, $cacheState, $PreOrderGateWarmSecs)
                } else {
                    Say-Warn '  no PLACEMENT line was seen before the area row, so the warm/cold delta is UNKNOWN for this run (the gate itself is unaffected).'
                }
                if ($PreOrderSettleSecs -gt 0) {
                    Say-Info ('  -PreOrderSettleSecs {0} is NOT spent: it is the gate''s timeout fallback and the gate fired.' -f $PreOrderSettleSecs)
                }
                Say-Ok '  pushing the order NOW - this is the first instant at which a move-to can be mesh-planned.'
                Save-Manifest
            } else {
                $Manifest.clocks.preOrderGateTimedOutUtc = $gateNowUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                $notReady = ('PRE-ORDER READY GATE ({0}) NOT READY: no "New Primary nav area" row in {1} after {2}s ({3} area rows seen, {4} placement lines seen{5}). The navigation area is NOT usable, so every move-to in the order would be planned by the FEATURE planner on one straight part - the exact failure this gate exists to prevent (G7B_G8_RESULTS_2026-09-14 sec 1.5). Check the object-console level (this run: {6}; the row needs >= 3) and whether the scenario really carries a nav area.' -f `
                    $PreOrderGate, $PathAppLog, $Manifest.oracle.preOrderGate.waitedSec, `
                    $script:NavGate.areaRowsSeen, $script:NavGate.placementRowsSeen, `
                    $(if ($null -ne $script:NavGate.firstPlacementUtc) { (', the first {0}s ago' -f [int]($gateNowUtc - $script:NavGate.firstPlacementUtc).TotalSeconds) } else { '' }), `
                    $ObjectConsoleLevel)
                if ($PreOrderSettleSecs -gt 0) {
                    Say-Fail $notReady
                    Say-Warn ('FALLING BACK to the fixed -PreOrderSettleSecs {0} hold. The order WILL be pushed after it, WITHOUT a ready signal: this run''s first legs may be feature-planned and the run must be read with that in the manifest (oracle.preOrderGate.fellBackToSettle).' -f $PreOrderSettleSecs)
                    Add-Flag 'WARN' ($notReady + (' -PreOrderSettleSecs {0} was given, so the run CONTINUES on that fixed hold instead of stopping; the order is pushed WITHOUT a ready signal.' -f $PreOrderSettleSecs))
                    $Manifest.oracle.preOrderGate.fellBackToSettle = $true
                    $RunPreOrderSettle = $true
                    Save-Manifest
                } else {
                    Save-Manifest
                    Stop-Runner 3 ($notReady + ' No -PreOrderSettleSecs fallback was given, so the run STOPS here rather than spend a window on an order that cannot be planned. Pass -PreOrderSettleSecs N to fall back to a fixed hold instead.')
                }
            }
        }
    }
    if ($RunPreOrderSettle) {
        Say-Head ('Stage 7d - pre-order settle{1}: hold {0}s before PushOrder (LAZY sectorised nav-area load)' -f $PreOrderSettleSecs, $(if ($PreOrderGateOn) { ' (FALLBACK - the READY GATE timed out)' } else { '' }))
        Say  '  The nav area loads AFTER placement: run 20260914T130439Z logged "New Primary nav area" 175 s'
        Say  '  after the members were created. A task issued before that plans without the mesh. This is a'
        Say  '  measurement parameter, not a verdict - the object consoles are what say the mesh arrived.'
        if ($DryRun) {
            Say-Plan ('would hold {0}s here, printing one status line every 30s, then push the order' -f $PreOrderSettleSecs)
        } else {
            $settleStart = (Get-Date).ToUniversalTime()
            $settleEnd   = (Get-Date).AddSeconds($PreOrderSettleSecs)
            while ((Get-Date) -lt $settleEnd) {
                $remaining = [int][Math]::Ceiling(($settleEnd - (Get-Date)).TotalSeconds)
                if ($remaining -le 0) { break }
                # LIVENESS, not a blind sleep (review of 374ea49, finding F7). Stage 7 polls the
                # back-end for exactly this reason and stage 8b polls the app: a 5.2 sim can die
                # mid-run (DI-Guy, 0xC0000005). Without these three checks a death during the
                # hold is slept through, the order is pushed into a corpse, and the failure then
                # surfaces later and for the WRONG REASON. The order has NOT been pushed yet at
                # this point, so a death here is a clean stop, not a truncated window.
                if ($BackendPid) {
                    $gone = -not (Get-Process -Id $BackendPid -ErrorAction SilentlyContinue)
                    $cs = Get-ChildItem -Path 'C:\MAK\logs' -Filter ('vrfSimHLA1516e5.2d-*-{0}.callstack.log' -f $BackendPid) -ErrorAction SilentlyContinue | Select-Object -First 1
                    if ($gone -or $cs) {
                        Stop-Runner 3 ('BACK-END pid {0} {1} DURING the stage-7d pre-order hold. The order was NOT pushed. Crash record: {2}. Read its callstack before anything else; the harvested .log holds the environment and is NOT for sharing.' -f $BackendPid, $(if ($gone) { 'DIED' } else { 'CRASHED and is PARKED on the dump prompt' }), $(if ($cs) { $cs.FullName } else { '(none found in C:\MAK\logs)' }))
                    }
                }
                if ($AppProc.HasExited) {
                    Stop-Runner 3 ('VrfC2SimApp EXITED with code {0} during the stage-7d pre-order hold - there is nothing left to push the order into. See {1} and {2}.' -f $AppProc.ExitCode, $PathAppLog, $PathAppErr)
                }
                if ($null -ne $WatchProc -and $WatchProc.HasExited) {
                    Stop-Runner 3 ('the trace observer WatchVrf-trace EXITED with code {0} during the stage-7d pre-order hold - THE MOVEMENT ORACLE IS GONE, so an order pushed now would be unscored. See {1}.' -f $WatchProc.ExitCode, $PathTrace)
                }
                Say-Info ('pre-order settle: {0}s remaining (back-end, interface and trace observer all alive)' -f $remaining)
                Start-Sleep -Seconds ([Math]::Min(30, $remaining))
            }
            Say-Ok ('pre-order settle complete ({0}s)' -f $PreOrderSettleSecs)
            $Manifest.clocks.preOrderSettleStartUtc = $settleStart.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            $Manifest.clocks.preOrderSettleEndUtc   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            Save-Manifest
        }
    }

    # ---------------------------------------------------------------------
    # STAGE 8 - push the order and observe
    # ---------------------------------------------------------------------
    Say-Head 'Stage 8 - PushOrder, then observe'
    # t0 MUST be stamped BEFORE the blocking call. PushOrder issues the order at child
    # start (Program.cs:102) and THEN sleeps PushOrderListenSec (default 30 s) before
    # exiting, and Invoke-External blocks until it exits. Capturing after the call would
    # stamp "order pushed" ~30 s late - and a downstream scorer using this as movement t0
    # would be off by ~30 s, enough to flip a pass/fail near the arrival thresholds.
    $orderPushedUtc   = (Get-Date).ToUniversalTime()
    $orderPushedLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
    $r = Invoke-External -Name 'PushOrder' -File $ExePushOrder `
            -Arguments @($Order, [string]$PushOrderListenSec, $RestUrl, $StompUrl) -Cwd $RepoRoot `
            -StdOutFile $PathPushOrderOut -StdErrFile $PathPushOrderErr `
            -TimeoutSec ($PushOrderListenSec + $StageTimeoutSec) `
            -Note 'BLOCKS for seconds-to-listen. exit 0 ok; 1 order rejected; 2 usage. Writes c2sim-bus.log beside its own binary; copied into the run dir afterwards.'
    if (-not $DryRun) {
        $Manifest.clocks.orderPushedUtc   = $orderPushedUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        $Manifest.clocks.orderPushedLocal = $orderPushedLocal
        if (-not (Test-StageProduced -Result $r)) { Stop-Runner 3 (Get-StageFailureText -Name 'PushOrder' -Result $r) }
        switch ($r.ExitCode) {
            0 { Say-Ok 'order accepted by the server' }
            2 { Stop-Runner 2 'PushOrder exited 2 (usage error). Nothing was pushed.' }
            default { Stop-Runner 3 ('PushOrder exited {0} - the server REJECTED the order. See {1}.' -f $r.ExitCode, $PathPushOrderOut) }
        }
        # LIMITATION 3: copy the bus log, and record whether it is actually ours.
        $srcBus = Join-Path (Split-Path -Parent $ExePushOrder) 'c2sim-bus.log'
        if (Test-Path -LiteralPath $srcBus -PathType Leaf) {
            $busWrite = (Get-Item -LiteralPath $srcBus).LastWriteTimeUtc
            Copy-Item -LiteralPath $srcBus -Destination $PathBusLog -Force
            $fresh = ($busWrite -ge $orderPushedUtc.AddSeconds(-($PushOrderListenSec + 60)))
            $Manifest.artifacts.busLogSourceWriteUtc = $busWrite.ToString('yyyy-MM-ddTHH:mm:ssZ')
            $Manifest.artifacts.busLogIsFromThisRun  = $fresh
            # The moment the ORDER really hit the bus. The observation window's t+Ns clock
            # starts when PushOrder RETURNS, up to -PushOrderListenSec later than this, and
            # the two were being printed as if they were one clock (defect 4 of 2026-09-14).
            $script:OrderOnBusUtc = Get-BusOrderUtc -BusLogText (Read-LiveText -Path $PathBusLog) -RunStartUtc $RunStartUtc
            if ($null -ne $script:OrderOnBusUtc) { $Manifest.clocks.orderOnBusUtc = ([datetime]$script:OrderOnBusUtc).ToString('yyyy-MM-ddTHH:mm:ss.fffZ') }
            if ($fresh) { Say-Ok ('bus log copied: {0}{1}' -f $PathBusLog, $(if ($null -ne $script:OrderOnBusUtc) { ('; the ORDER reached the bus at {0}' -f $Manifest.clocks.orderOnBusUtc) } else { '' })) }
            else { Add-Flag 'WARN' ('the copied c2sim-bus.log was last written {0} - it may be from an EARLIER run (PushOrder gives no output-path override).' -f $busWrite) }
        } else {
            Add-Flag 'WARN' 'PushOrder produced no c2sim-bus.log beside its binary.'
        }
    }

    Say-Head ('Stage 8b - observation window: {0}s{1}' -f $RunSecs, $(if ($StopWhenComplete) { ' CAP (-StopWhenComplete)' } else { '' }))
    Say '  Nothing is judged here. The trace and the report capture are the evidence;'
    Say '  scoring happens later, against a ratified 4a.'
    # -StopWhenComplete bookkeeping. Written to the manifest whether or not it fires,
    # so a reader can tell "did not fire" from "was not enabled".
    $EarlyExit = [ordered]@{
        enabled        = [bool]$StopWhenComplete
        source         = 'vrfc2simapp.log: SENT TASK STATUS REPORT (TASKCMPLT|TASKABRT) taskee=<uuid> task=<uuid> - <why> and the R1 position-report round lines; watchvrf-trace.csv TSK/RPT/POS records; reports-captured.log C2SIM PositionReport / TaskStatus records (APPENDED by ListenReports as each report arrives, 0999eeb - live during the window)'
        criterion      = '(1) every distinct order taskee has >= 1 TERMINAL report (TASKCMPLT or TASKABRT); (2) every order task, keyed (taskee, task), has one - a line COUNT is not the test; (3) (1)+(2) held for settleHoldSecs (FLOOR); (4) every taskee has post-completion position evidence from ANY ONE of: RPT (a trace RPT POSITION later than its TSK and within reportToleranceMeters of its latest POS), C2SIM-capture (a PositionReport for its own uuid captured after its TASKCMPLT), R1-applog (a complete R1 round, 0 skipped, logged after its TASKCMPLT line); runSecs is the cap'
        taskees        = $OrderTaskees
        taskCount      = $OrderTasks.Count
        settleHoldSecs = $SettleHoldSecs
        reportToleranceMeters = $ReportToleranceMeters
        reportEvidence = [ordered]@{}
        evidenceSatisfiedUtc = $null
        windowSecsCap  = $RunSecs
        fired          = $false
        firstSeenUtc   = [ordered]@{}
        allCompleteUtc = $null
        closedUtc      = $null
        windowSecsUsed = $null
        completionLinesSeen = 0
        tasksClosed    = 0
        terminalByCode = [ordered]@{}
        unattributedTerminalLines = 0
        failedCondition = $null
    }
    $Manifest.oracle.earlyExit = $EarlyExit
    # THE Q5 PROBE's own record. Written whether or not it fires, so a reader can tell
    # "did not fire" from "was not armed" - the same reason $EarlyExit is written unconditionally.
    $PauseProbe = [ordered]@{
        armed        = [bool]$PauseProbeOn
        pauseAtSec   = $PauseAtSec
        resumeAtSec  = $ResumeAtSec
        whatThisIs   = 'Q5 (assessment live gate 11 / validation V5): a PAUSED scenario must not age a C2SIM task clock. tools/PauseSim pauses and later resumes the scenario from inside the observation window and records the back end''s own scenario clock either side of each call. It is a MEASUREMENT: it never fails the run, and the KILL half of the Q5 probe is a MANUAL step (RUNBOOK 0.5.14 item 14), never automated here.'
        pauseFired   = $false
        resumeFired  = $false
        pause        = $null
        resume       = $null
    }
    if ($PauseProbeOn) { $Manifest.probes = [ordered]@{ pauseResume = $PauseProbe } }
    # THE WS RUNAWAY ABORT's own record. Written whether or not it is armed, and whether or not it
    # ever fires, for the same reason $EarlyExit and $PauseProbe are: a reader must be able to tell
    # "never checked" from "checked and clean" from "checked and aborted".
    $wsAlertsPath = Join-Path $RunDir 'thread-samples.alerts.txt'
    $WsDispatchUtc = if ($null -ne $script:OrderOnBusUtc) { [datetime]$script:OrderOnBusUtc } else { $orderPushedUtc }
    $WsDispatchSource = if ($null -ne $script:OrderOnBusUtc) { 'orderOnBusUtc' } else { 'orderPushedUtc (bus log dispatch time unavailable)' }
    $Manifest.preflight.wsRunaway = [ordered]@{
        enabled             = $WsRunawayOn
        abortAfter          = $WsRunawayAbortAfter
        alertsFile          = $wsAlertsPath
        dispatchUtc         = $WsDispatchUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        dispatchSource      = $WsDispatchSource
        pollIntervalSecs    = 10
        alertsSinceDispatch = @()
        firstAlertLine      = $null
        lastAlertLine       = $null
        aborted             = $false
        abortedAtUtc        = $null
    }
    if ($DryRun) {
        if ($StopWhenComplete) {
            Say-Plan ('would poll {0} every 5s for TERMINAL task-status lines (TASKCMPLT or TASKABRT); would close the window once all {1} taskee(s) have one and all {2} task(s) are closed by one, {3}s have passed and every taskee has post-completion position evidence - a trace RPT within {5} m of its POS, OR a C2SIM PositionReport for its own uuid captured after its TASKCMPLT, OR a complete R1 round (0 skipped) logged after it; would otherwise sleep out the {4}s cap' -f $PathAppLog, $OrderTaskees.Count, $OrderTasks.Count, $SettleHoldSecs, $RunSecs, $ReportToleranceMeters)
        } else {
            Say-Plan ('would sleep {0}s while WatchVrf and ListenReports keep sampling' -f $RunSecs)
        }
        if ($PauseProbeOn) {
            if ($PauseAtSec -gt 0) {
                Say-Plan ('would run, at t+{0}s of that window, {1} pause {2} "{3}" (cwd {4}), blocking the poll loop ~20s, then record its [RESULT] line in the manifest' -f `
                    $PauseAtSec, $ExePauseSim, $AppNo['pauseSim'], $FederationArg, $Bin64)
            }
            if ($ResumeAtSec -gt 0) {
                Say-Plan ('would run, at t+{0}s of that window, {1} resume {2} "{3}" (cwd {4}) - a SEPARATE join and a SEPARATE appNumber' -f `
                    $ResumeAtSec, $ExePauseSim, $AppNo['resumeSim'], $FederationArg, $Bin64)
            }
            Say-Plan 'the KILL half of the Q5 probe is NOT automated and is not shown here: it is a manual Stop-Process on the vrfSimHLA1516e pid (RUNBOOK 0.5.14 item 14).'
        }
        if ($WsRunawayOn) {
            Say-Plan ('would poll {0} every 10s for BACK-END WS RUNAWAY alerts (scripts\SampleThreads.ps1, RUNBOOK 0.5.11 item 17) timestamped at or after the order reaching the bus; would ABORT the run (Stop-Runner 6, normal teardown, exit 6) once {1} such alert(s) have accumulated - never before -WsRunawayAbortAfter 0 disables this check' -f $wsAlertsPath, $WsRunawayAbortAfter)
        }
    } else {
        $obsStart = Get-Date
        $obsEnd = $obsStart.AddSeconds($RunSecs)
        $appDeathRecorded = $false
        # 5 s poll when the window can close early (so the hold is over-measured by
        # at most 5 s), the historical 30 s otherwise. The status line keeps its
        # 30 s cadence either way. An ARMED Q5 PROBE forces the 5 s cadence too: its
        # offsets are honoured at POLL granularity, and on a 30 s poll a pause asked
        # for at t+120 s could fire anywhere up to t+150 s. The WS RUNAWAY ABORT (armed by
        # default, -WsRunawayAbortAfter 0 turns it off) forces AT MOST a 10 s cadence for the
        # same reason: its own alerts-file poll is specified at 10 s and must not be stretched
        # to 30 s just because neither of the other two probes happens to be active.
        $pollSecs = if ($StopWhenComplete -or $PauseProbeOn) { 5 } elseif ($WsRunawayOn) { 10 } else { 30 }
        $nextStatus = (Get-Date).AddSeconds(30)
        $nextEvidenceNote = Get-Date
        # WS RUNAWAY ABORT poll state. $nextWsCheck starts in the past so the FIRST loop
        # iteration already checks (the alerts file could in principle already exist from a
        # stale prior run's sampler - see the manifest's alertsFile path, which is THIS run's
        # own run directory, so that is not actually possible, but starting armed costs nothing).
        $nextWsCheck = (Get-Date).AddSeconds(-1)
        $wsRunawayAborted = $false
        $completion = New-CompletionState
        # TERMINAL task-status lines seen in the app log for ANY taskee (the state's own
        # lineCount counts only the order's taskees). A RUNNING TOTAL now that the reader
        # is incremental: each poll sees only what was appended since the previous one.
        $completionLinesAll = 0
        # Condition (4) below reads the WHOLE app log and the WHOLE trace (Get-VrfUuidByName
        # correlates a CreateRoute line with a later Route-created line, and
        # Test-ReportEvidence needs the last RPT and the last POS - neither can be fed a
        # delta without cross-poll state of its own). It is therefore THROTTLED to once per
        # 30 s instead of once per 5 s, and these two variables persist across polls instead
        # of being recomputed on every one. It is evaluated IMMEDIATELY the first time
        # all-complete holds, so $evidence is never $null where the pending-reason message
        # below reads it.
        # HONEST COST, BOTH DIRECTIONS (do not restate this as "safe"): the decision now runs
        # on an evidence snapshot up to 30 s old. A flip from out to IN is seen up to 30 s
        # LATE - that lengthens the window, which is safe. A flip from in to OUT is also seen
        # up to 30 s late, so the window CAN close on evidence that was satisfied 30 s ago and
        # is not satisfied now. $ReportToleranceMeters is 2.0 m (:1826), so that is not
        # impossible; it is judged acceptable because condition (4) is only ever evaluated
        # AFTER every taskee has reported TASKCMPLT - the units have stopped, the RPT stream
        # converges on a static POS - and because $EarlyExit.evidenceSatisfiedUtc plus the
        # per-taskee reportEvidence written at that evaluation record exactly which snapshot
        # the close was made on. If a run ever closes on stale evidence, that pair is the
        # evidence of it. The alternative measured on 2026-09-14 was 12 whole-trace reads
        # during a 60 s hold, at ~2 GB of UTF-16 per read on a 1 GB trace.
        $evidence     = $null
        $evidenceOk   = $false
        $nextEvidence = Get-Date
        while ((Get-Date) -lt $obsEnd) {
            Start-Sleep -Seconds ([int][Math]::Min($pollSecs, [Math]::Max(1, [Math]::Ceiling(($obsEnd - (Get-Date)).TotalSeconds))))
            $remaining = [int]([Math]::Max(0, ($obsEnd - (Get-Date)).TotalSeconds))
            if ((Get-Date) -ge $nextStatus -or $remaining -eq 0) {
                $nextStatus = (Get-Date).AddSeconds(30)
                $sum = Get-LastLineWithPrefix -Path $PathTrace -Prefix '# t='
                Say-Info ('  {0}s remaining   trace: {1}' -f $remaining, $(if ($sum) { $sum } else { '(no samples)' }))
            }
            # An interface that dies mid-window is RECORDED, ONCE, and the window is
            # then RUN OUT rather than cut short: 4a.6 makes "the trace covers the
            # whole run" a validity item, and a truncated trace destroys the evidence
            # that would explain the death. Do not turn this into a break - and the
            # early exit below is disabled from here on for the same reason.
            if ($AppProc.HasExited -and -not $appDeathRecorded) {
                $appDeathRecorded = $true
                Add-Flag 'FAIL' ('VrfC2SimApp exited DURING the observation window with code {0}. The window is being RUN OUT anyway so the trace still covers it; the run is NOT valid (4a.6) but the evidence is preserved.' -f $AppProc.ExitCode)
            }
            # THE WS RUNAWAY ABORT (RUNBOOK 0.5.11 item 17 extension). Reads the SAME
            # thread-samples.alerts.txt scripts\SampleThreads.ps1 writes (armed only when the
            # wrapper's -SampleThreads/--sample-threads started that sampler against THIS run
            # directory; an absent file is silently 0 alerts, exactly like today's behaviour when
            # no sampler is running at all - this check adds nothing for a run that never samples).
            # Get-WsRunawayAlertsSinceDispatch (RunnerLib.ps1) keeps only lines timestamped at or
            # after $WsDispatchUtc; Stop-Runner below takes the SAME failure path (Add-Flag 'FAIL',
            # normal finally teardown) every other post-launch Stage 8b failure takes.
            if ($WsRunawayOn -and -not $wsRunawayAborted -and (Get-Date) -ge $nextWsCheck) {
                $nextWsCheck = (Get-Date).AddSeconds(10)
                if (Test-Path -LiteralPath $wsAlertsPath -PathType Leaf) {
                    $wsSince = @(Get-WsRunawayAlertsSinceDispatch -AlertsText (Read-LiveText -Path $wsAlertsPath) -DispatchUtc $WsDispatchUtc)
                    $Manifest.preflight.wsRunaway.alertsSinceDispatch = $wsSince
                    if ($wsSince.Count -gt 0) {
                        $Manifest.preflight.wsRunaway.firstAlertLine = $wsSince[0]
                        $Manifest.preflight.wsRunaway.lastAlertLine  = $wsSince[$wsSince.Count - 1]
                    }
                    if ($wsSince.Count -ge $WsRunawayAbortAfter) {
                        $wsRunawayAborted = $true
                        $Manifest.preflight.wsRunaway.aborted      = $true
                        $Manifest.preflight.wsRunaway.abortedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                        Save-Manifest
                        Stop-Runner 6 ('BACK-END WS RUNAWAY confirmed: {0} alert(s) since the order at {1}; last: {2}' -f `
                            $wsSince.Count, $Manifest.preflight.wsRunaway.dispatchUtc, $wsSince[$wsSince.Count - 1])
                    }
                }
            }
            # THE Q5 PROBE. Each half fires ONCE, at the first poll at or after its offset.
            # Skipped once the interface has died: the window is being RUN OUT for the trace's
            # sake at that point and pausing the scenario would only corrupt what is left of it.
            # The elapsed time is re-read between the two halves because the pause BLOCKS for
            # ~20 s - long enough for a resume offset close behind it to have come due.
            if ($PauseProbeOn -and -not $appDeathRecorded) {
                if ($PauseAtSec -gt 0 -and -not $PauseProbe.pauseFired -and ((Get-Date) - $obsStart).TotalSeconds -ge $PauseAtSec) {
                    $PauseProbe.pauseFired = $true
                    $PauseProbe.pause = Invoke-PauseSimProbe -Action 'pause' -AppNumber $AppNo['pauseSim'] `
                        -StdOutFile $PathPauseSimOut -StdErrFile $PathPauseSimErr `
                        -AtSec $PauseAtSec -ElapsedSec ([int]((Get-Date) - $obsStart).TotalSeconds)
                    Save-Manifest
                }
                if ($ResumeAtSec -gt 0 -and -not $PauseProbe.resumeFired -and ((Get-Date) - $obsStart).TotalSeconds -ge $ResumeAtSec) {
                    $PauseProbe.resumeFired = $true
                    $PauseProbe.resume = Invoke-PauseSimProbe -Action 'resume' -AppNumber $AppNo['resumeSim'] `
                        -StdOutFile $PathResumeSimOut -StdErrFile $PathResumeSimErr `
                        -AtSec $ResumeAtSec -ElapsedSec ([int]((Get-Date) - $obsStart).TotalSeconds)
                    Save-Manifest
                }
            }
            if ($StopWhenComplete -and -not $appDeathRecorded -and $OrderTaskees.Count -gt 0) {
                $nowUtc = (Get-Date).ToUniversalTime()
                # TERMINAL reports, not TASKCMPLT alone: a task VR-Forces failed, and a
                # successor skipped behind it, are TASKABRT, and both are end states of an
                # order task (run 20260914T230706Z). -OrderTasks gives the state the order's
                # own (taskee, task) keys, so coverage is asked per TASK, not per line.
                $done = @(Get-TerminalTaskReports -AppLogText (Read-LiveDelta -Path $PathAppLog -Key 'applog-completions'))
                $completionLinesAll += $done.Count
                $before       = $completion.firstSeenUtc.Count
                $beforeClosed = (Get-TaskCoverage -State $completion).Closed
                $completion = Update-CompletionState -State $completion -Taskees $OrderTaskees -TaskCount $OrderTasks.Count -Completions $done -NowUtc $nowUtc -OrderTasks $OrderTasks
                $cov = Get-TaskCoverage -State $completion
                if ($completion.firstSeenUtc.Count -gt $before -or $cov.Closed -gt $beforeClosed) {
                    Say-Info ('  terminal task reports: {0}/{1} taskee(s) covered, {2}/{3} task(s) closed [{4}]; {5} line(s) for order taskees ({6} total) (t+{7}s after PushOrder returned{8})' -f `
                        $completion.firstSeenUtc.Count, $OrderTaskees.Count, $cov.Closed, $cov.TaskCount, `
                        $cov.Text, $completion.lineCount, $completionLinesAll, `
                        [int]((Get-Date) - $obsStart).TotalSeconds, (Get-OrderClockNote))
                }
                # Condition (4): post-completion POSITION EVIDENCE, from three sources -
                # the live trace (RPT vs POS on the trace clock), the app log (a complete
                # R1 round below this taskee's TASKCMPLT line) and, when it is already on
                # disk, the C2SIM report capture. ANY ONE satisfies a taskee; RunnerLib
                # Test-ReportEvidence says which in 'via'. Only evaluated once all taskees
                # have completed - before that the answer is "not yet" by construction and
                # the parse is wasted work.
                if ($null -ne $completion.allCompleteUtc -and ($null -eq $evidence -or (Get-Date) -ge $nextEvidence)) {
                    $nextEvidence = (Get-Date).AddSeconds(30)
                    # ONE whole-file read of the app log feeds both parsers (they need
                    # cross-line and cross-poll context a delta reader can not give).
                    $appWhole  = Read-LiveText -Path $PathAppLog
                    $nameToVrf = Get-VrfUuidByName -AppLogText $appWhole
                    $appPosEv  = Get-AppLogPositionEvidence -AppLogText $appWhole
                    # reports-captured.log is APPENDED by ListenReports as each report arrives
                    # (0999eeb), so it is normally POPULATED here - before that change it
                    # existed only after the tool exited, which is why this evidence source
                    # had never fired live. An absent or still-empty file is still tolerated
                    # (Read-LiveText returns ''). It is the AUTHORITY once it carries the
                    # record - the taskee's own uuid and its completion on ONE wall clock.
                    $capEv     = Get-ReportCaptureEvidence -CaptureText (Read-LiveText -Path $PathReports) -RunStartUtc $RunStartUtc
                    $evidence  = Test-ReportEvidence -Taskees $OrderTaskees -TaskeeNames $TaskeeNames -NameToVrfUuid $nameToVrf `
                                     -TraceText (Read-LiveText -Path $PathTrace) -ToleranceMeters $ReportToleranceMeters `
                                     -CaptureEvidence $capEv -AppLogPositionEvidence $appPosEv `
                                     -CompletionUtcByTaskee $completion.firstSeenUtc -CodeByTaskee $completion.firstSeenCode
                    $evidenceOk = [bool]$evidence.AllSatisfied
                    foreach ($k in @($evidence.PerTaskee.Keys)) { $EarlyExit.reportEvidence[$k] = $evidence.PerTaskee[$k] }
                    if ($evidenceOk -and $null -eq $EarlyExit.evidenceSatisfiedUtc) {
                        $EarlyExit.evidenceSatisfiedUtc = $nowUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                        Say-Ok ('  report evidence IN for all {0} taskee(s) at t+{1}s after PushOrder returned{3}: {2}' -f `
                            $OrderTaskees.Count, [int]((Get-Date) - $obsStart).TotalSeconds, `
                            (@($evidence.PerTaskee.Values | ForEach-Object { '{0} via {1} ({2})' -f $(if ($_.name) { $_.name } else { '(unnamed)' }), $_.via, $_.reason }) -join '; '), `
                            (Get-OrderClockNote))
                    }
                }
                $verdict = Test-EarlyExit -State $completion -Taskees $OrderTaskees -SettleHoldSecs $SettleHoldSecs -NowUtc $nowUtc -ReportEvidence $evidenceOk
                if ($verdict.AllComplete -and $null -eq $EarlyExit.allCompleteUtc) {
                    $EarlyExit.allCompleteUtc = ([datetime]$completion.allCompleteUtc).ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                    Say-Ok ('  ALL taskees and ALL tasks have a TERMINAL report at t+{0}s after PushOrder returned{2} - closed: {3}; holding >= {1}s AND waiting for post-completion position evidence before closing the window' -f [int]((Get-Date) - $obsStart).TotalSeconds, $SettleHoldSecs, (Get-OrderClockNote), $verdict.TerminalSummary)
                }
                if ($verdict.HoldElapsed -and -not $verdict.EvidenceIn -and (Get-Date) -ge $nextEvidenceNote) {
                    # Report once per 30 s why the window is still open past the floor.
                    $nextEvidenceNote = (Get-Date).AddSeconds(30)
                    $pending = @($evidence.PerTaskee.GetEnumerator() | Where-Object { -not $_.Value.satisfied } | ForEach-Object { '{0}: {1}' -f $(if ($_.Value.name) { $_.Value.name } else { $_.Key }), $_.Value.reason })
                    Say-Info ('  hold floor of {0}s elapsed ({1}s); report evidence still pending - {2}' -f $SettleHoldSecs, $verdict.HoldElapsedSecs, ($pending -join ' | '))
                }
                if ($verdict.ShouldClose) {
                    $EarlyExit.fired = $true
                    Say-Ok ('  settle hold of {0}s elapsed ({1}s) and position evidence in - closing the observation window EARLY at t+{2}s after PushOrder returned{4}, of the {3}s cap; closed: {5}' -f $SettleHoldSecs, $verdict.HoldElapsedSecs, [int]((Get-Date) - $obsStart).TotalSeconds, $RunSecs, (Get-OrderClockNote), $verdict.TerminalSummary)
                    break
                }
            }
        }
        $Manifest.clocks.observationEndUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        $EarlyExit.closedUtc      = $Manifest.clocks.observationEndUtc
        $EarlyExit.windowSecsUsed = [Math]::Round(((Get-Date) - $obsStart).TotalSeconds, 1)
        $EarlyExit.completionLinesSeen = $completion.lineCount
        $covFinal = Get-TaskCoverage -State $completion
        $EarlyExit.tasksClosed    = $covFinal.Closed
        $EarlyExit.terminalByCode = $covFinal.ByCode
        $EarlyExit.unattributedTerminalLines = $covFinal.Unattributed
        foreach ($k in @($completion.firstSeenUtc.Keys)) { $EarlyExit.firstSeenUtc[$k] = ([datetime]$completion.firstSeenUtc[$k]).ToString('yyyy-MM-ddTHH:mm:ss.fffZ') }
        if ($StopWhenComplete -and -not $EarlyExit.fired) {
            # The @() MUST wrap the .Missing PROPERTY, not the call: member enumeration over a
            # one-element array unwraps to a bare [string], and under Set-StrictMode -Version
            # Latest (:314) the $missing.Count at :2171 then throws (runner EXIT=5, first hit by
            # run 20260902T143638Z - exactly one taskee missing is the only branch that reaches it).
            $missing = @( (Test-EarlyExit -State $completion -Taskees $OrderTaskees -SettleHoldSecs $SettleHoldSecs -NowUtc (Get-Date).ToUniversalTime() -ReportEvidence $false).Missing )
            # The SAME verdict, taken once more only to read its FailedCondition: the
            # $missing assignment above must keep the exact @( (call).Missing ) shape the
            # offline gate pins (tests sec 8, the run 20260902T143638Z EXIT=5 defect), and
            # $evidenceFinal aliases $evidenceOk so that gate's "exactly one call takes
            # -ReportEvidence $evidenceOk" still identifies the POLL-LOOP call.
            $evidenceFinal = $evidenceOk
            $finalVerdict  = Test-EarlyExit -State $completion -Taskees $OrderTaskees -SettleHoldSecs $SettleHoldSecs -NowUtc (Get-Date).ToUniversalTime() -ReportEvidence $evidenceFinal
            $EarlyExit.failedCondition = $finalVerdict.FailedCondition
            $pendingEv = @($EarlyExit.reportEvidence.GetEnumerator() | Where-Object { -not $_.Value.satisfied } | ForEach-Object { '{0}: {1}' -f $(if ($_.Value.name) { $_.Value.name } else { $_.Key }), $_.Value.reason })
            Say-Info ('  -StopWhenComplete did NOT fire; window ran to its {0}s cap. BLOCKED BY {1}. Terminal reports: {2}. Taskees without a terminal report: {3}. Report evidence pending: {4}' -f `
                $RunSecs, `
                $(if ($finalVerdict.FailedCondition) { $finalVerdict.FailedCondition } else { 'nothing - every condition held by the time the window ended' }), `
                $finalVerdict.TerminalSummary, `
                $(if ($missing.Count -gt 0) { $missing -join ', ' } else { '(none)' }), `
                $(if ($pendingEv.Count -gt 0) { $pendingEv -join ' | ' } else { ('(condition (4) was never evaluated - it is reached only once (1) and (2) hold; {0} of {1} task(s) closed, {2} of {3} taskee(s) covered)' -f $finalVerdict.TasksClosed, $finalVerdict.TaskCount, ($OrderTaskees.Count - $missing.Count), $OrderTaskees.Count) }))
        }
        # AN ARMED PROBE THAT NEVER FIRED IS A FLAG, NOT A FOOTNOTE: it means the window closed
        # before the offset (-StopWhenComplete firing early is the ordinary cause), so the run
        # is NOT the Q5 evidence it was launched to be, and its appNumber was burned unused.
        if ($PauseProbeOn) {
            foreach ($half in @(
                @{ n='pause';  at=$PauseAtSec;  fired=$PauseProbe.pauseFired },
                @{ n='resume'; at=$ResumeAtSec; fired=$PauseProbe.resumeFired })) {
                if ($half.at -gt 0 -and -not $half.fired) {
                    Add-Flag 'WARN' ('the Q5 {0} probe NEVER FIRED: it was asked for t+{1}s and the observation window closed at t+{2}s. Its appNumber is BURNED, not recycled, and this run carries NO Q5 evidence for that half.' -f `
                        $half.n, $half.at, $EarlyExit.windowSecsUsed)
                }
            }
            if ($PauseProbe.pauseFired -and $PauseAtSec -gt 0 -and $ResumeAtSec -gt 0 -and -not $PauseProbe.resumeFired) {
                Add-Flag 'FAIL' ('the Q5 pause FIRED but the resume did NOT - the scenario was left PAUSED when the window closed, so nothing in the rest of this run moved. Teardown runs regardless (it is a finally), but whether StopVrf brings a PAUSED back end down as cleanly as a running one is UNMEASURED - watch its exit code. To resume by hand while VR-Forces is still up: tools/PauseSim resume <freshAppNo>.')
            }
        }
        Save-Manifest
        Say-Ok ('observation window complete ({0}s used of {1}s)' -f $EarlyExit.windowSecsUsed, $RunSecs)
    }

    if ($DryRun) {
        Say-Head 'DRY RUN - teardown that WOULD follow (it also runs on every failure path)'
    }
}
catch [System.OperationCanceledException] {
    # Stop-Runner already recorded the reason and set $RunnerExit.
    if ($RunnerExit -eq 0) { $RunnerExit = 3 }
}
catch {
    $RunnerExit = 5
    Say-Fail ('unexpected terminating error: {0}' -f $_.Exception.Message)
    Say-Fail ('at: {0}' -f $_.InvocationInfo.PositionMessage)
    Add-Flag 'FAIL' ('unexpected terminating error: {0}' -f $_.Exception.Message)
}
finally {
    # =====================================================================
    # TEARDOWN - runs on EVERY path. StopIface (clean resign) THEN StopVrf.
    # Nothing here force-kills anything, ever.
    # =====================================================================
    Say-Head 'Teardown (runs on every path, success or failure)'

    $teardownOk = $true

    # 1. StopIface: drive the C2SIM server to UNINITIALIZED so the interface
    #    RESIGNS from the RTI (RUNBOOK sec 4). This is the ONLY correct way to
    #    stop the interface. Skipped when the app was never started, so a
    #    validation abort does not tear down someone else's live session.
    $StopIfaceUtc = $null
    if ($AppStarted -or $DryRun) {
        $StopIfaceUtc = (Get-Date).ToUniversalTime()
        $r = Invoke-External -Name 'StopIface' -File $ExeStopIface `
                -Arguments @($RestUrl, $StompUrl, '--yes') -Cwd $RepoRoot `
                -StdOutFile $PathStopIfaceOut -StdErrFile $PathStopIfaceErr `
                -TimeoutSec $StageTimeoutSec `
                -Note 'REQUIRES <restUrl> <stompUrl> --yes; NO defaults. exit 0 ok; 1 the server did NOT reach UNINITIALIZED (the interface MAY STILL BE JOINED); 2 usage.'
        if (-not $DryRun) {
            # A teardown stage that never returned is a teardown FAILURE, not a
            # reason to stop tearing down - StopVrf still has to run below.
            # Handled INSIDE default (which pwsh 7 does reach on a $null exit
            # code) so this is reported exactly once.
            switch ($r.ExitCode) {
                0 { Say-Ok 'server driven to UNINITIALIZED; the interface should resign' }
                default {
                    $teardownOk = $false
                    if (-not (Test-StageProduced -Result $r)) {
                        Add-Flag 'FAIL' ((Get-StageFailureText -Name 'StopIface' -Result $r) + ' The server may NOT be UNINITIALIZED and the interface may STILL BE JOINED. INSPECT BEFORE THE NEXT RUN.')
                    } else {
                        Add-Flag 'FAIL' ('StopIface exited {0}. The server may NOT be UNINITIALIZED and the interface may STILL BE JOINED. Nothing was force-killed. INSPECT BEFORE THE NEXT RUN.' -f $r.ExitCode)
                    }
                }
            }
        }
    } else {
        Say-Info 'interface was never started - StopIface skipped (it is destructive and would hit a server this run never used)'
    }

    # 2. Wait for the interface to exit ON ITS OWN. NEVER force-killed: a
    #    force-killed joined federate is a stale federate (RUNBOOK sec 0), and
    #    the next start hangs at RTI join.
    if ($AppProc -and -not $DryRun) {
        Say-Info ('waiting up to {0}s for VrfC2SimApp (pid {1}) to resign and exit' -f $AppExitTimeoutSec, $AppProc.Id)
        $null = $AppProc.WaitForExit($AppExitTimeoutSec * 1000)
        if ($AppProc.HasExited) {
            Say-Ok ('VrfC2SimApp exited with code {0} (clean resign)' -f $AppProc.ExitCode)
            foreach ($s in $Manifest.stages) {
                if ($s.name -eq 'VrfC2SimApp' -and $s.outcome -eq 'started-background') {
                    $s.exitCode = $AppProc.ExitCode
                    $s.endedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                    $s.outcome  = 'exited'
                    break
                }
            }
        } else {
            $teardownOk = $false
            Add-Flag 'FAIL' ("VrfC2SimApp (pid {0}) did NOT exit within {1}s. IT IS NOT BEING KILLED - a force-killed joined federate leaves a STALE FEDERATE and the next start hangs at RTI join (RUNBOOK sec 0). MANUAL INSPECTION REQUIRED: check whether the server actually reached UNINITIALIZED, or use tools/ResetVrf (RUNBOOK sec 8)." -f $AppProc.Id, $AppExitTimeoutSec)
        }
    } elseif ($DryRun) {
        Say-Plan 'would wait for VrfC2SimApp to exit on its own; would NEVER kill it'
    }

    # 3. End the observers WITH the window, not with their worst-case duration cap.
    #    Hold until StopIface + TrailSecs (so the trace still carries the trail and
    #    the interface's resign), then TOUCH THE STOP FILE. Each observer that was
    #    started with --stop-file sees it within ~1 s and takes its normal clean
    #    resign / disconnect path - the SAME code path its duration expiry takes.
    #    Nothing is signalled, closed or killed; a file appears and the tool reads
    #    it. An observer started without the flag (stage 0b said unsupported) is
    #    unaffected and runs to its cap; teardown waits for it as before.
    #    Reference is the StopIface moment; when StopIface never ran (validation
    #    abort, app never started) the reference is now, so the trail still applies.
    $traceRef = if ($null -ne $StopIfaceUtc) { $StopIfaceUtc } else { (Get-Date).ToUniversalTime() }
    $anyStopFile = ($ProbeWatch.supportsStopFile -or $ProbeListen.supportsStopFile)
    $stopFileTouched = $false
    if ($DryRun) {
        if ($anyStopFile) {
            Say-Plan ('would wait until StopIface + {0}s (trail), then create {1}; WatchVrf-trace/ListenReports would see it within ~1 s and resign/disconnect cleanly; would then wait up to {2}s grace for each, and if one had not exited, keep waiting up to its {3}s cap + {4}s margin (never killed)' -f $TrailSecs, $PathStopFile, $TraceStopGraceSec, $EffWatchSecs, $ObserverCapMarginSecs)
        } else {
            Say-Plan ('would wait for WatchVrf and ListenReports to finish their own {0}s timers (never killed) - neither deployed binary supports --stop-file' -f $EffWatchSecs)
        }
    } elseif ($anyStopFile -and ($null -ne $WatchProc -or $null -ne $ListenProc)) {
        $wait = Get-TraceStopWaitSecs -ReferenceUtc $traceRef -TrailSecs $TrailSecs -NowUtc (Get-Date).ToUniversalTime()
        if ($wait -gt 0) {
            Say-Info ('holding {0}s so the trace carries {1}s of trail past StopIface before the observers are told to stop' -f $wait, $TrailSecs)
            Start-Sleep -Seconds $wait
        }
        try {
            [System.IO.File]::WriteAllText($PathStopFile, ('stop requested by RunC2SimScenario.ps1 {0} at {1} (StopIface {2}, trail {3}s)' -f $RunId, (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ'), $(if ($StopIfaceUtc) { $StopIfaceUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ') } else { '(never ran)' }), $TrailSecs) + "`n")
            $stopFileTouched = $true
            $Manifest.clocks.traceStopRequestedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            Say-Ok ('stop file created: {0} - observers started with --stop-file will resign within ~1 s' -f $PathStopFile)
        } catch {
            # Not fatal: the observers still end at their duration cap. Teardown just
            # has to wait for that, as it did before the stop file existed.
            Add-Flag 'WARN' ('could not create the observer stop file {0}: {1}. The observers will run to their {2}s cap and teardown waits for them.' -f $PathStopFile, $_.Exception.Message, $EffWatchSecs)
        }
        Save-Manifest
    }
    # In stop-file mode the wait is the GRACE; if it expires, Complete-Background keeps
    # waiting (never kills) up to the observer's own cap + margin (review F1) so that
    # StopVrf below never runs under a possibly still-joined observer. Without the
    # stop file the wait is cap + 120 as in the record, and the cap fallback is moot.
    $watchStop  = ($stopFileTouched -and $ProbeWatch.supportsStopFile)
    $listenStop = ($stopFileTouched -and $ProbeListen.supportsStopFile)
    $watchWait  = if ($watchStop)  { $TraceStopGraceSec } else { $EffWatchSecs + 120 }
    $listenWait = if ($listenStop) { $TraceStopGraceSec } else { $EffWatchSecs + 120 }
    Complete-Background -Name 'WatchVrf-trace' -Process $WatchProc -TimeoutSec $watchWait `
        -CapSecs $(if ($watchStop) { $EffWatchSecs } else { 0 }) -CapMarginSecs $ObserverCapMarginSecs `
        -Note $(if ($watchStop) { 'THE MOVEMENT ORACLE trace. Told to stop via the stop file at StopIface + trail; resigned itself.' }
                else { 'THE MOVEMENT ORACLE trace. Allowed to run its full duration and resign itself.' })
    Complete-Background -Name 'ListenReports' -Process $ListenProc -TimeoutSec $listenWait `
        -CapSecs $(if ($listenStop) { $EffWatchSecs } else { 0 }) -CapMarginSecs $ObserverCapMarginSecs

    # *** ADDED 2026-07-19: THE ORACLE MUST NOT DIE SILENTLY. ***
    # Run 20260719T185814Z: WatchVrf CRASHED with 0xC0000005 after a single POS line,
    # and this runner reported "RUN COMPLETE - evidence collected" and EXIT=0. A run
    # whose movement oracle died produced NO evidence, and saying otherwise is worse
    # than failing - it invites scoring a trace that stops three seconds in.
    # 0xC0000005 arrives here as the signed int -1073741819 (unsigned 0xC0000005).
    $watchStage = $Manifest.stages | Where-Object { $_.name -eq 'WatchVrf-trace' } | Select-Object -Last 1
    if ($null -ne $watchStage -and $null -ne $watchStage.exitCode -and $watchStage.exitCode -ne 0) {
        $isAv = ($watchStage.exitCode -eq -1073741819)
        $why  = if ($isAv) { 'ACCESS VIOLATION (0xC0000005) - the native bridge faulted' }
                else       { ('exit {0}' -f $watchStage.exitCode) }
        Add-Flag 'FAIL' ('THE MOVEMENT ORACLE DIED: WatchVrf-trace {0}. The trace is TRUNCATED and MUST NOT be scored. This run collected no usable movement evidence.' -f $why)
        $script:OracleDied = $true
    }

    # 4. Bring VR-Forces down. RTI infrastructure is preserved by StopVrf itself.
    if ($VrfLaunched -or $DryRun) {
        # $PSHOME\pwsh.exe, never a bare 'pwsh' (RUNBOOK 0.5.14 item 1: bare pwsh on this
        # machine is the 32-BIT build). Harmless for StopVrf itself, but the rule is the rule
        # and the watchdog already obeys it (review of 374ea49, finding F8).
        $r = Invoke-External -Name 'StopVrf' -File (Join-Path $PSHOME 'pwsh.exe') `
                -Arguments @('-NoProfile','-File', $StopVrf, '-TimeoutSec', [string]$StopVrfTimeoutSec) `
                -Cwd $RepoRoot -StdOutFile $PathStopVrfOut -StdErrFile $PathStopVrfErr `
                -TimeoutSec ($StopVrfTimeoutSec + $StageTimeoutSec) `
                -Note $(if ($Is52) { 'StopVrf52.ps1 (5.2 profile): CloseMainWindow on vrfGui, then a NO-/F taskkill (a graceful close request) on vrfSimHLA1516e after the grace. exit 0 down/already down; 2 bad args; 3 timed out (NOTHING killed); 5 unexpected error - VR-FORCES MAY STILL BE RUNNING. rtiAssistant/rtiexec/rtiForwarder are never touched.' }
                        else { 'exit 0 down/already down; 2 bad args; 3 timed out (NOT killed); 4 confirm dialog not drivable via UIA; 5 unexpected error - VR-FORCES MAY STILL BE RUNNING. An unattended runner must branch on 5 as well as 3 (RUNBOOK 0.5.9). NOTE: this stage MASKED the -Wait defect, because StopVrf makes its own descendants exit; see the Invoke-External header.' })
        if (-not $DryRun) {
            switch ($r.ExitCode) {
                0 { Say-Ok 'VR-Forces is down (graceful; RTI infrastructure preserved)' }
                default {
                    $teardownOk = $false
                    if (-not (Test-StageProduced -Result $r)) {
                        Add-Flag 'FAIL' ((Get-StageFailureText -Name 'StopVrf' -Result $r) + ' VR-Forces MAY STILL BE RUNNING - a leftover instance HARD-BLOCKS the next launch.')
                    } else {
                        Add-Flag 'FAIL' ('StopVrf exited {0}. VR-Forces MAY STILL BE RUNNING, possibly behind an unanswered modal. NOTHING was force-killed. Inspect before the next run - a leftover instance HARD-BLOCKS the next launch.' -f $r.ExitCode)
                    }
                }
            }
        }
    } else {
        Say-Info 'VR-Forces was never launched by this run - StopVrf skipped'
    }

    # 4b. Capture the simulator's OWN logs into the run directory (P0.a,
    #     docs/RESEARCH_MECHANISMS_2026-09-01.md sec 6). bin64\vrfSim.log and
    #     vrfGui.log are OVERWRITTEN by the next launch; RUN 3's vrfSim.log held
    #     the decisive company-freeze lines and was never captured. Copied AFTER
    #     StopVrf so the files are complete. A copy failure must never affect
    #     teardown - report it as a WARN flag and move on.
    if (-not $DryRun) {
        foreach ($lg in @('vrfSim.log', 'vrfGui.log')) {
            # 5.2 writes these to C:\MAK\logs by default (DIFF row A5), so bin64 is
            # searched FIRST (5.0.2, unchanged) and C:\MAK\logs only as a fallback -
            # a READ, never a write into the vendor tree.
            $src = Join-Path $Bin64 $lg
            if ($Is52 -and -not (Test-Path -LiteralPath $src)) { $src = Join-Path 'C:\MAK\logs' $lg }
            try {
                if (Test-Path -LiteralPath $src) {
                    Copy-Item -LiteralPath $src -Destination (Join-Path $RunDir ('bin64-' + $lg)) -Force
                    Say-Ok ('captured {0} into the run directory (bin64-{0})' -f $lg)
                } else {
                    Add-Flag 'WARN' ('simulator log {0} not found at {1} - nothing captured.' -f $lg, $src)
                }
            } catch {
                Add-Flag 'WARN' ('could not capture {0} into the run directory: {1}' -f $lg, $_.Exception.Message)
            }
        }
    } elseif ($Is52) {
        Say-Plan 'would copy vrfSim.log and vrfGui.log (bin64, else C:\MAK\logs - the 5.2 default location, DIFF row A5) into the run directory (bin64-*.log)'
    } else {
        Say-Plan 'would copy bin64\vrfSim.log and bin64\vrfGui.log into the run directory (bin64-*.log)'
    }

    # 5. Post-teardown inventory: what is left, and confirm RTI survived.
    if (-not $DryRun) {
        $left = @()
        foreach ($n in @($ProcLauncher, $ProcBackend, $ProcFrontend)) {
            foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
                $left += ('{0}(pid {1})' -f $p.Name, $p.Id)
            }
        }
        $rtiLeft = @()
        foreach ($n in $RtiNames) {
            foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) { $rtiLeft += ('{0}(pid {1})' -f $p.Name, $p.Id) }
        }
        $obsLeft = @()
        foreach ($n in $ProcObservers) {
            foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) { $obsLeft += ('{0}(pid {1})' -f $p.Name, $p.Id) }
        }
        $Manifest.preflight.postRunVrf       = $left
        $Manifest.preflight.postRunObservers = $obsLeft
        $Manifest.preflight.postRunRti       = $rtiLeft
        if ($left.Count -gt 0) {
            $teardownOk = $false
            Add-Flag 'FAIL' ('VR-Forces processes still present after teardown: {0}. Not killed.' -f ($left -join ', '))
        } else { Say-Ok 'no VR-Forces processes remain' }
        if ($obsLeft.Count -gt 0) {
            Add-Flag 'WARN' ('Observer processes still present after teardown: {0}. Not killed - they end on their own duration cap. The next run REFUSES to launch (Stage 1) until they are gone.' -f ($obsLeft -join ', '))
        } else { Say-Ok 'no WatchVrf / ListenReports observer remains' }
        if ($rtiLeft.Count -gt 0) { Say-Ok ('RTI infrastructure preserved (correct): {0}' -f ($rtiLeft -join ', ')) }

        # STAGE 2h's HOLDER IS EXPECTED TO BE ALIVE HERE (STP-825). It is not a leftover and
        # not a fault: it is a joined federate on a WALL-CLOCK hold that deliberately
        # outlives this run, so the next run's sim also finds the federation already there.
        # Named in the inventory so "nothing left running" stays honest, never killed
        # (RUNBOOK sec 0) and never waited for - teardown does not block on it.
        $holdLeft = @()
        foreach ($hp in $script:FederationHolderPids) {
            $hAlive = $false
            try { $null = Get-Process -Id $hp -ErrorAction Stop; $hAlive = $true } catch { $hAlive = $false }
            if ($hAlive) { $holdLeft += ('RtiProbe-holder(pid {0})' -f $hp) }
        }
        $Manifest.preflight.postRunFederationHolder = $holdLeft
        if ($holdLeft.Count -gt 0) {
            Say-Ok ('federation holder STILL JOINED and that is EXPECTED (STP-825): {0}. It holds {1} on a {2}s wall-clock timer so the next create is an "already exists" JOIN. NOT killed, NOT waited for.' -f ($holdLeft -join ', '), $FederationHoldName, $FederationHoldSecs)
        } elseif ($FederationHoldOn) {
            Say-Ok 'the Stage 2h federation holder has already resigned (its hold expired during the run) - nothing left to leave alone.'
        }

        # Restore this process's environment.
        $env:PATH                  = $SavedPath
        $env:MAKLMGRD_LICENSE_FILE = $SavedLicense
        $env:Vrf__ApplicationNumber= $SavedVrfAppNumber
        $env:C2SIM__RestUrl        = $SavedC2SimRestUrl
        $env:C2SIM__StompUrl       = $SavedC2SimStompUrl
        # ...and the profile's own variables, back to whatever they were (no-op on 5.0.2).
        foreach ($k in $ProfileEnv.Keys) { Set-Item -Path ('Env:' + $k) -Value ([string]$SavedProfileEnv[$k]) }
        foreach ($k in $AppEnv52.Keys)   { Set-Item -Path ('Env:' + $k) -Value ([string]$SavedAppEnv52[$k]) }

        if (-not $teardownOk -and $RunnerExit -lt 4) { $RunnerExit = 4 }
        $Manifest.runnerExitCode = $RunnerExit
        Save-Manifest
    }

    Say-Head 'Result'
    if ($DryRun) {
        # *** FALSE GREEN FIXED 2026-09-04. This branch used to `exit 0` UNCONDITIONALLY, so a
        # dry run that hit the generic catch above printed "[FAIL] unexpected terminating
        # error ..." and then "DRY-RUN complete" and exited 0. It cost nothing that day only
        # because someone grepped the output; the defect that produced it (a comment between a
        # backtick continuation and its argument, breaking the Stage 2r call) would otherwise
        # have shipped. The live path below always honoured $RunnerExit - only -DryRun did not.
        # A SUCCESSFUL dry run is untouched, byte for byte: same four lines, same exit 0. ***
        if ($RunnerExit -ne 0) {
            Say-Fail ('DRY-RUN FAILED (exit {0}). Nothing was launched, but this dry run did NOT complete: a stage or the script itself raised the error(s) recorded above.' -f $RunnerExit)
            Say-Fail '  A dry run exists to prove the planned sequence is well-formed. Treat this exactly like a failed run: fix the cause, do not proceed to a live run.'
            exit $RunnerExit
        }
        Say-Ok 'DRY-RUN complete.'
        Say-Ok ('  NOTHING was launched, NO server was contacted, the Appendix B marker was NOT advanced (it still reads {0}).' -f $FirstFree)
        Say-Ok ('  No run directory was created ({0} does not exist because of this invocation).' -f $RunDir)
        Say-Ok  '  This script COLLECTS EVIDENCE and does NOT score. sec 4a is ratified; scoring is a separate program.'
        exit 0
    }

    Say ('  run directory : {0}' -f $RunDir)
    Say ('  manifest      : {0}' -f $ManifestPath)
    Say ('  appNumbers    : {0} (marker advanced {1} -> {2}, ledgered={3})' -f `
            (($Alloc | ForEach-Object { $_.appNumber }) -join ','), $FirstFree, $NextFree, $LedgerAdvanced)
    Say ('  local / UTC   : {0} / {1}' -f (Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'), (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss'))
    foreach ($f in $Manifest.validityFlags) { Say ('  [{0}] {1}' -f $f.severity, $f.text) }

    # *** ADDED 2026-07-19. A run whose ORACLE DIED is not a successful run, whatever
    # every other stage returned. Previously this could reach exit 0 and print
    # "RUN COMPLETE - evidence collected" after WatchVrf had crashed three seconds in.
    # Promote to 3 (failed after VR-Forces was up): teardown still ran, evidence is
    # partial, and the manifest names the stage. ***
    if ($script:OracleDied -and $RunnerExit -eq 0) {
        $RunnerExit = 3
        # The manifest was already written with runnerExitCode 0 above, so a later scorer
        # would read 0 for a run whose oracle died - the exact false-green shape this
        # project keeps hitting. Rewrite the field and re-save.
        $Manifest.runnerExitCode = 3
        try { Save-Manifest } catch { Say-Warn 'could not re-save the manifest after promoting the exit code to 3; the FAIL flag is still present in it.' }
    }

    switch ($RunnerExit) {
        0 { Say-Ok 'RUN COMPLETE - evidence collected. THIS IS NOT A VERDICT: sec 4a.6 makes run 1 a measurement. Score the trace separately.' }
        2 { Say-Fail 'ABORTED at validation / usage. Nothing was launched where it could be checked first.' }
        3 { Say-Fail 'RUN FAILED after VR-Forces was up. Teardown ran. Evidence is PARTIAL - the manifest names the stage.' }
        4 { Say-Fail 'TEARDOWN INCOMPLETE. VR-Forces and/or the interface MAY STILL BE RUNNING and MAY STILL BE JOINED. Nothing was force-killed. INSPECT BEFORE THE NEXT RUN.' }
        5 { Say-Fail 'UNEXPECTED TERMINATING ERROR. Same warning as exit 4 - inspect before the next run.' }
        6 { Say-Fail 'BACK-END WS RUNAWAY ABORT (RUNBOOK 0.5.11 item 17 extension). Teardown ran. Evidence is PARTIAL - see preflight.wsRunaway in the manifest for the alerts that triggered it.' }
    }
    # OUT-OF-PROCESS TEARDOWN BACKSTOP, half 2 of 2 (2026-09-14). LAST statement before the
    # exit, so its presence means the whole finally above ran. The wrapper
    # (scripts\RunScenario.sh) tears down on its own when runner.launched exists and this
    # does not. Best-effort and silent: a teardown that completed must not be turned into a
    # failure by a marker that could not be written. -DryRun never reaches here (it exits
    # earlier and creates no run directory), which is correct - it launches nothing.
    try {
        if ($RunDir -and (Test-Path -LiteralPath $RunDir)) {
            New-Item -ItemType File -Path (Join-Path $RunDir 'runner.teardown-ran') -Force | Out-Null
        }
    } catch { }
    exit $RunnerExit
}
} # closes the OUTER try opened right after Stage 1a takes the lock (see there)
finally {
    # LAUNCH LOCK RELEASE (RUNBOOK 0.5.14 item 15 addendum, 2026-09-15 - V6b LIVE
    # DEFECT). This finally is the OUTER one opened right after Stage 1a takes the
    # lock (see "try {" there) and it closes the true end of the script, so it wraps
    # EVERYTHING after the lock: Stage 1's own pre-flight checks, the C2SIM server
    # reachability probe, Stage 2 appNo allocation, the entire launch try/catch/
    # finally above (including its own exit $RunnerExit, which unwinds through here
    # on its way out) - every exit path, not only the teardown-complete one.
    #
    # WHY THE PREVIOUS PLACEMENT WAS WRONG: v1 (commit 0acd4fe) released the lock
    # INSIDE the teardown finally above, which only executes for code paths that
    # reach the main try. V6b's first live use (2026-09-15 11:37Z, merged main
    # 56f3a20) proved the gap: the runner took the lock in Stage 1a, then aborted at
    # the C2SIM REST reachability check further down Stage 1 ("[FAIL] Aborting
    # BEFORE VR-Forces is launched" - the private test server was down after a
    # reboot) via a bare `exit 2` OUTSIDE any try block, so the inner finally never
    # ran and runs/runner.lock stayed on disk naming the now-dead pid. The
    # stale-pid rule (Stage 1a) made the NEXT run self-heal, but a lock must be
    # released on every exit path, not merely be recoverable from on the next one.
    #
    # -DryRun never takes the lock (Stage 1a), so this is a no-op there. Best-effort,
    # like the teardown-ran marker inside the inner finally - a release that fails
    # must not turn a completed run into a reported failure; the next runner's
    # stale-pid check (Stage 1a) recovers it anyway.
    if ($script:RunnerLockTaken -and $script:RunnerLockPath) {
        try {
            Remove-Item -LiteralPath $script:RunnerLockPath -Force -ErrorAction Stop
            Say-Ok ('launch lock released: {0}' -f $script:RunnerLockPath)
        } catch {
            Say-Warn ('could not remove the launch lock {0}: {1}. The next runner will find pid {2} already gone and report it stale.' -f $script:RunnerLockPath, $_.Exception.Message, $PID)
        }
    }
}
